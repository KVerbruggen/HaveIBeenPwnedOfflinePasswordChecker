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
        public List<Search> Searches { get; set; }

        public SearchThread(Task task, Search search)
        {
            this.Task = task;
            this.Searches = new List<Search>() { search };
        }

        public SearchThread(Task task, List<Search> searches)
        {
            this.Task = task;
            this.Searches = searches;
        }
    }
}
