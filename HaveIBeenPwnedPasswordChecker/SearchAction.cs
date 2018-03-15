using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PasswordChecker.controls;

namespace PasswordChecker
{
    public class SearchThread
    {
        public Task Task { get; }
        public List<SearchParameters> SearchParameters { get; set; }

        public SearchThread(Task task, SearchParameters searchParameters)
        {
            this.Task = task;
            this.SearchParameters = new List<SearchParameters>() { searchParameters };
        }

        public SearchThread(Task task, List<SearchParameters> searchParameters)
        {
            this.Task = task;
            this.SearchParameters = searchParameters;
        }
    }

    public struct SearchParameters
    {
        public string Hash;
        public PasswordControl LinkedPasswordControl;
        public ResultBox LinkedResultBox {
            get { return LinkedPasswordControl.LinkedResultBox; }
        }
    }
}
