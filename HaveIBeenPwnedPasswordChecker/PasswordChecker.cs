using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Threading;

namespace PasswordChecker
{
    #region enums

    public enum SearchType
    {
        OrderedByHash,
        OrderedByCount
    }

    #endregion

    public static class PWC
    {
        #region event listener definitions

        public delegate void EventHandler(object sender, EventArgs e);
        public static EventHandler AllSearchesFinished;

        #endregion

        #region fields

        private static object threadLock = new object();

        private static CancellationTokenSource ctsAllSearches;

        #endregion

        #region properties

        private static List<SearchThread> runningSearches = new List<SearchThread>();

        public static string Filepath { get; set; }

        public static SearchType SearchType { get; set; } = SearchType.OrderedByCount;

        #endregion

        #region methods

        public static string Hash(string input)
        {
            // Source: https://stackoverflow.com/questions/17292366/hashing-with-sha1-algorithm-in-c-sharp
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        private static void RemoveSearch(SearchThread searchThread)
        {
            lock (threadLock)
            {
                runningSearches.Remove(searchThread);
                if (runningSearches.Count < 1)
                {
                    AllSearchesFinished?.Invoke(searchThread, new EventArgs());
                }
            }
        }

        public static void CreateSearchInFile(string inputhash, controls.PasswordControl pwc)
        {
            if (runningSearches.Count < 1)
            {
                ctsAllSearches = new CancellationTokenSource();
            }
            Search search = pwc.Search;

            CancellationTokenSource localCts = new CancellationTokenSource();

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ctsAllSearches.Token, localCts.Token);
            Task<int> seekHashTask = Task.Run(() => SeekHashInFile(search.Hash, linkedCts));
            SearchThread searchThread = new SearchThread(seekHashTask, search);
            runningSearches.Add(searchThread);
            seekHashTask.ContinueWith((task) =>
            {
                RemoveSearch(searchThread);
                if (task.Result < 0)
                {
                    pwc.Search.Cancelled();
                }
                else
                {
                    pwc.Search.DoneSeeking(task.Result);
                }
            });

            return;
        }

        public static void StopSeeking()
        {
            ctsAllSearches.Cancel();
        }

        private static async Task<int> SeekHashInFile(string hashToFind, CancellationTokenSource cts)
        {
            bool foundHash = false;
            int foundCount = -1;
            using (FileStream stream = File.Open(@Filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                await Task.Run(async () =>
                {
                    char byteRead;

                    string currentHash = String.Empty;
                    string currentHashCountString = String.Empty;
                    // phase 0 = read hash; phase 1 = read count
                    int phase = 0;

                    switch (SearchType)
                    {
                        case SearchType.OrderedByCount:
                            while (((byteRead = (char)stream.ReadByte()) != -1) && !cts.Token.IsCancellationRequested)
                            {
                                if ((int)byteRead != 65535 && (int)byteRead != -1)
                                {
                                    if (Char.IsWhiteSpace(byteRead))
                                    {
                                        if (phase == 1)
                                        {
                                            Task<(bool found, int count)> t = Task.Run(() => CompareHashes(currentHash, hashToFind, currentHashCountString));
                                            await t.ContinueWith((taskCompareHashes) =>
                                            {
                                                if (taskCompareHashes.Result.found)
                                                {
                                                    cts.Cancel();
                                                    foundHash = true;
                                                    foundCount = taskCompareHashes.Result.count;
                                                }
                                                return;
                                            });
                                            currentHash = String.Empty;
                                            currentHashCountString = String.Empty;
                                            phase = 0;
                                        }
                                    }
                                    else if (byteRead.Equals(':'))
                                    {
                                        phase = 1;
                                    }
                                    else
                                    {
                                        if (phase == 0)
                                        {
                                            currentHash += byteRead;
                                        }
                                        else
                                        {
                                            currentHashCountString += byteRead;
                                        }
                                    }
                                }
                                else
                                {
                                    if (phase == 1)
                                    {

                                        Task<(bool found, int count)> t = Task.Run(() => CompareHashes(currentHash, hashToFind, currentHashCountString));
                                        int i = await t.ContinueWith((taskCompareHashes) =>
                                        {
                                            if (taskCompareHashes.Result.found)
                                            {
                                                cts.Cancel();
                                                foundHash = true;
                                                foundCount = taskCompareHashes.Result.count;
                                            }
                                            return taskCompareHashes.Result.count;
                                        });
                                    }
                                }
                            }
                            break;
                        case SearchType.OrderedByHash:
                            // TODO
                            break;
                    }
                });
            }
            return foundCount;
        }

        private static (bool, int) CompareHashes(string hashFromFile, string hashToFind, string countString)
        {
            int count = 0;
            Int32.TryParse(countString, out count);
            return (hashFromFile.Equals(hashToFind), count);
        }

        #endregion

    }
}
