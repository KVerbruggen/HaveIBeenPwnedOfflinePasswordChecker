using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PasswordChecker.controls;

namespace PasswordChecker
{
    #region enums

    public enum SearchState
    {
        NotStarted,
        Seeking,
        DoneSeeking,
        Cancelled
    }

    #endregion

    #region event listener definitions

    public class PasswordStateEventArgs : EventArgs
    {
        private readonly SearchState searchState;

        public PasswordStateEventArgs(SearchState searchState)
        {
            this.searchState = searchState;
        }

        public SearchState SearchState
        {
            get { return searchState; }
        }
    }

    #endregion

    public class Password
    {
        #region event listeners

        public delegate void EventHandler(object sender, PasswordStateEventArgs e);
        public EventHandler StateChanged;

        #endregion

        #region fields

        private SearchState state;
        private string hash;

        #endregion

        #region properties

        public bool IsLocked
        {
            get { return (State == SearchState.Seeking) ; }
        }

        public string Hash { get { return hash; }
            set
            {
                if (!IsLocked)
                {
                    hash = value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public int Count { get; set; }

        public SearchState State
        {
            get { return state; }
            set
            {
                state = value;
                StateChanged?.Invoke(this, new PasswordStateEventArgs(value));
            }
        }

        #endregion

        #region constructors

        public Password()
        {
        }

        public Password(string hash)
        {
            this.Hash = hash;
        }

        #endregion

        #region methods

        public void StartedSeeking()
        {
            State = SearchState.Seeking;
        }

        public void DoneSeeking(int count)
        {
            this.Count = count;
            State = SearchState.DoneSeeking;
        }

        public void Cancelled()
        {
            State = SearchState.Cancelled;
        }

        #endregion

    }
}
