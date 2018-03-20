using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PasswordChecker.controls
{
    /// <summary>
    /// Interaction logic for PasswordControl.xaml
    /// </summary>
    public partial class PasswordControl : UserControl
    {
        #region event handlers
        public delegate void ButtonClickHandler(object sender, RoutedEventArgs e);
        public event ButtonClickHandler RemovePasswordClick;

        #endregion

        #region properties

        public Search Search { get; private set; }
        public ResultBox LinkedResultBox { get; set; }

        public string Password
        {
            get { return passwordbox.Password; }
        }

        public bool Edited
        {
            get; private set;
        }

        #endregion

        #region constructors

        public PasswordControl(Search search)
        {
            Search = search;
            search.StateChanged += search_StateChanged;

            InitializeComponent();
        }

        #endregion

        #region events

        private void btRemovePassword_Click(object sender, RoutedEventArgs e)
        {
            RemovePasswordClick?.Invoke(this, e);
        }

        private void passwordbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Edited = true;
        }

        private void search_StateChanged(object sender, SearchStateEventArgs e)
        {
            if (sender is Search)
            {
                Search search = (sender as Search);
                if (e.SearchState == SearchState.DoneSeeking)
                {
                    Edited = false;
                }
            }
        }

        #endregion
    }
}
