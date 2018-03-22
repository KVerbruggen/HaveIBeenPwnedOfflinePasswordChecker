using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;

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

        private static List<Password> runningSearches = new List<Password>();

        #endregion

        #region properties

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

        private static void AddSearch(Password password)
        {
            lock (threadLock)
            {
                if (runningSearches.Count == 0)
                {
                    SearchStarted?.Invoke(password, new EventArgs());
                }
                runningSearches.Add(password);
            }
        }

        private static void RemoveSearch(Password password)
        {
            lock (threadLock)
            {
                runningSearches.Remove(password);
                if (runningSearches.Count == 0)
                {
                    ctsAllSearches.Cancel();
                    AllSearchesFinished?.Invoke(password, new EventArgs());
                }
            }
        }

        public static void CreateSearchesInFile(List<Password> passwords)
        {
            if (runningSearches.Count < 1)
            {
                ctsAllSearches = new CancellationTokenSource();
            }

            foreach(Password password in passwords)
            {
                password.StartedSeeking();
                AddSearch(password);
            }
            Task seekHashTask = Task.Run(() => SeekHashesInFile(passwords, ctsAllSearches));

            seekHashTask.ContinueWith((task) =>
            {
                foreach (Password password in passwords)
                {
                    RemoveSearch(password);
                    if (password.State == SearchState.Seeking)
                    {
                        password.Cancelled();
                    }
                }
            });

            return;
        }

        public static void StopSeeking()
        {
            ctsAllSearches.Cancel();
        }

        private static async Task SeekHashesInFile(List<Password> passwordsToFind, CancellationTokenSource cts)
        {
            await Task.Run(() =>
            {
                
                using (FileStream stream = File.Open(@Filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    ReaderWriterLockSlim passwordsLock = new ReaderWriterLockSlim();
                    Password[] allPasswords = passwordsToFind.ToArray();

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

                                            passwordsLock.EnterReadLock();
                                            Password[] passwordsCurrentLoop = passwordsToFind.ToArray();
                                            passwordsLock.ExitReadLock();
                                            Parallel.ForEach(passwordsCurrentLoop, password =>
                                            {
                                                int result = CompareHashes(currentHash, password.Hash, currentCount);
                                                if (result != -1)
                                                {
                                                    passwordsLock.EnterWriteLock();
                                                    passwordsToFind.Remove(password);
                                                    passwordsLock.ExitWriteLock();
                                                    RemoveSearch(password);
                                                    password.DoneSeeking(result);
                                                }
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

                                        passwordsLock.EnterReadLock();
                                        Password[] passwordsCurrentLoop = passwordsToFind.ToArray();
                                        passwordsLock.ExitReadLock();
                                        Parallel.ForEach(passwordsCurrentLoop, password =>
                                        {
                                            int result = CompareHashes(currentHash, password.Hash, currentCount);
                                            if (result != -1)
                                            {
                                                passwordsLock.EnterWriteLock();
                                                passwordsToFind.Remove(password);
                                                passwordsLock.ExitWriteLock();
                                                RemoveSearch(password);
                                                password.DoneSeeking(result);
                                            }
                                        });
                                    }
                                    else
                                    {
                                        cts.Cancel();
                                        return;
                                    }
                                }
                            }
                            break;
                        case SearchType.OrderedByHash:
                            // TODO
                            break;
                    }
                }
                return;
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
