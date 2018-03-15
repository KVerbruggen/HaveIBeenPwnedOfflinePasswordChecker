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

        public static void CreateSearchInFile(string inputhash, controls.PasswordControl pwc)
        {
            SearchParameters searchParameters = new SearchParameters {
                Hash = inputhash,
                LinkedPasswordControl = pwc
            };

            CancellationTokenSource cts = new CancellationTokenSource();

            Task<int> seekHashTask = Task.Run(() => SeekHashInFile(searchParameters.Hash, cts));
            runningSearches.Add(new SearchThread(seekHashTask, searchParameters));
            seekHashTask.ContinueWith((task) => {
                searchParameters.LinkedResultBox.Dispatcher.Invoke(() => searchParameters.LinkedResultBox.DoneSeeking(task.Result));
            });

            return;
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
                                if ((int)byteRead != -1)
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
