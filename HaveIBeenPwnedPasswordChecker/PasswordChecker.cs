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
        public static EventHandler SearchStarted;
        public static EventHandler AllSearchesFinished;

        #endregion

        #region fields

        private static object threadLock = new object();

        private static CancellationTokenSource ctsAllSearches;

        #endregion

        #region properties

        private static List<Search> runningSearches = new List<Search>();

        public static string Filepath { get; set; }

        public static SearchType SearchType { get; set; } = SearchType.OrderedByCount;

        #endregion

        #region constructors

        static PWC()
        {
            // AllSearchesFinished += CollectGarbage;
        }

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

        private static void AddSearch(Search search)
        {
            lock (threadLock)
            {
                if (runningSearches.Count == 0)
                {
                    SearchStarted?.Invoke(search, new EventArgs());
                }
            }
            runningSearches.Add(search);
        }

        private static void RemoveSearch(Search search)
        {
            runningSearches.Remove(search);
            lock (threadLock)
            {
                if (runningSearches.Count < 1)
                {
                    AllSearchesFinished?.Invoke(search, new EventArgs());
                }
            }
        }

        public static void CreateSearchInFile(string inputhash, Search search)
        {
            if (runningSearches.Count < 1)
            {
                ctsAllSearches = new CancellationTokenSource();
            }

            CancellationTokenSource localCts = new CancellationTokenSource();

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ctsAllSearches.Token, localCts.Token);

            Task<int> seekHashTask = Task.Run(() => SeekHashInFile(search.Hash, linkedCts));

            AddSearch(search);

            seekHashTask.ContinueWith((task) =>
            {
                RemoveSearch(search);
                if (task.Result < 0)
                {
                    search.Cancelled();
                }
                else
                {
                    search.DoneSeeking(task.Result);
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
            return await Task<int>.Run(() =>
            {
                int foundCount = -1;

                using (FileStream stream = File.Open(@Filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    char byteRead;

                    StringBuilder HashBuilder = new StringBuilder();
                    StringBuilder CountBuilder = new StringBuilder();
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
                                        string currentHash = HashBuilder.ToString();
                                        string currentCount = CountBuilder.ToString();
                                            Task.Run(() => CompareHashes(currentHash, hashToFind, currentCount))
                                            .ContinueWith((taskCompare) =>
                                            {
                                                if (taskCompare.Result != -1)
                                                {
                                                    cts.Cancel();
                                                    foundCount = taskCompare.Result;
                                                }
                                                return;
                                            });
                                            HashBuilder.Clear();
                                            CountBuilder.Clear();
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
                                            HashBuilder.Append(byteRead);
                                        }
                                        else
                                        {
                                            CountBuilder.Append(byteRead);
                                        }
                                    }
                                }
                                else
                                {
                                    if (phase == 1)
                                    {
                                        string currentHash = HashBuilder.ToString();
                                        string currentCount = CountBuilder.ToString();
                                        Task.Run(() => CompareHashes(currentHash, hashToFind, currentCount))
                                        .ContinueWith((taskCompare) =>
                                        {
                                            if (taskCompare.Result != -1)
                                            {
                                                cts.Cancel();
                                                foundCount = taskCompare.Result;
                                            }
                                            else
                                            {
                                                cts.Cancel();
                                                foundCount = 0;
                                            }
                                            return;
                                        });
                                    }
                                    else
                                    {
                                        cts.Cancel();
                                        return 0;
                                    }
                                }
                            }
                            break;
                        case SearchType.OrderedByHash:
                            // TODO
                            break;
                    }
                }
                return foundCount;
            }, cts.Token);
        }

        private static int CompareHashes(string hashFromFile, string hashToFind, string countString)
        {
            int count = 0;
            if (hashFromFile.Equals(hashToFind))
            {
                Int32.TryParse(countString, out count);
                return count;
            }
            else
            {
                return -1;
            }
        }

        #endregion

        #region events

        private static void CollectGarbage(object sender, EventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


        #endregion

    }
}
