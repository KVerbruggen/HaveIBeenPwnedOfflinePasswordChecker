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
    public enum SearchType
    {
        OrderedByHash,
        OrderedByCount
    }

    public static class PWC
    {
        private static object threadLock = new object();

        private static CancellationTokenSource cts;

        public static MainWindow MainWindow
        {
            private get;
            set;
        }

        private static List<SearchThread> runningSearches = new List<SearchThread>();

        public static string Filepath { get; set; }

        public static SearchType SearchType { get; set; } = SearchType.OrderedByCount;

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
                    MainWindow.Dispatcher.Invoke(() => MainWindow.AllSearchesFinished());
                }
            }
        }

        public static void CreateSearchInFile(string inputhash, controls.PasswordControl pwc)
        {
            if (runningSearches.Count < 1)
            {
                cts = new CancellationTokenSource();
            }
            SearchParameters searchParameters = new SearchParameters {
                Hash = inputhash,
                LinkedPasswordControl = pwc
            };

            CancellationTokenSource localCts = new CancellationTokenSource();

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, localCts.Token);
            Task<int> seekHashTask = Task.Run(() => SeekHashInFile(searchParameters.Hash, linkedCts));
            SearchThread searchThread = new SearchThread(seekHashTask, searchParameters);
            lock (threadLock)
            {
                runningSearches.Add(searchThread);
            }
            seekHashTask.ContinueWith((task) =>
            {
                RemoveSearch(searchThread);
                searchParameters.LinkedResultBox.Dispatcher.Invoke(() => searchParameters.LinkedResultBox.DoneSeeking(task.Result));
            });

            return;
        }

        public static void StopSeeking()
        {
            cts.Cancel();
        }

        private static async Task<int> SeekHashInFile(string hashToFind, CancellationTokenSource cts)
        {
            bool foundHash = false;
            int foundCount = 0;
            using (FileStream stream = File.Open(@Filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                await Task.Run(() =>
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
                                if ((int)byteRead != 65535)
                                {
                                    switch (byteRead)
                                    {
                                        default:
                                            if (phase == 0)
                                            {
                                                currentHash += byteRead;
                                            }
                                            else
                                            {
                                                currentHashCountString += byteRead;
                                            }
                                            break;
                                        case ':':
                                            phase = 1;
                                            break;
                                        case ' ':
                                            if (phase == 1)
                                            {

                                                Task<(bool found, int count)> t = Task.Run(() => CompareHashes(currentHash, hashToFind, currentHashCountString));
                                                t.ContinueWith((taskCompareHashes) =>
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
                                            currentHash = String.Empty;
                                            currentHashCountString = String.Empty;
                                            phase = 0;
                                            break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            break;
                        case SearchType.OrderedByHash:
                            // TODO
                            break;
                    }
                });
            }
            lock (threadLock)
            {

            }
            return foundCount;
        }

        private static (bool, int) CompareHashes(string hashFromFile, string hashToFind, string countString)
        {
            int count = 0;
            Int32.TryParse(countString, out count);
            return (hashFromFile.Equals(hashToFind), count);
        }
    }

}
