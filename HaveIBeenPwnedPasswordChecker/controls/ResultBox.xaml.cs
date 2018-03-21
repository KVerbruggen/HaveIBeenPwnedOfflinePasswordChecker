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
    /// Interaction logic for ResultBox.xaml
    /// </summary>
    public partial class ResultBox : UserControl
    {

        #region fields

        private Password search;

        #endregion

        #region properties

        public int Count
        {
            set
            {
                search.Count = value;
                if (search.Count > 0)
                {
                    txtCount.Content = search.Count + " times found";
                }
                else if (search.Count < 0)
                {
                    txtCount.Content = "Cancelled";
                }
                else
                {
                    txtCount.Content = "Hash not found";
                }
            }
        }

        #endregion

        #region constructors

        public ResultBox(Password search)
        {
            this.search = search;
            search.StateChanged += search_StateChanged;

            InitializeComponent();
        }

        #endregion

        #region methods

        private void SearchStarted()
        {
            txtResult.Content = search.Hash;
            txtCount.Foreground = new SolidColorBrush(Colors.Black);
            txtCount.Visibility = Visibility.Hidden;
            animLoading.Visibility = Visibility.Visible;
        }

        private void SearchFinished()
        {
            if (search.Count > 0)
            {
                txtCount.Foreground = new SolidColorBrush(Colors.DarkRed);
                txtCount.Content = search.Count + " times found";
            }
            else if (search.Count < 0)
            {
                SearchCancelled();
            }
            else
            {
                txtCount.Foreground = new SolidColorBrush(Colors.DarkGreen);
                txtCount.Content = "Hash not found";
            }
            txtCount.Visibility = Visibility.Visible;
            animLoading.Visibility = Visibility.Hidden;
        }

        private void SearchCancelled()
        {
            txtCount.Foreground = new SolidColorBrush(Colors.Orange);
            txtCount.Visibility = Visibility.Visible;
            Count = -1;
            animLoading.Visibility = Visibility.Hidden;
        }

        private void ResetCount()
        {
            txtCount.Foreground = new SolidColorBrush(Colors.Black);
            txtCount.Visibility = Visibility.Hidden;
        }

        #endregion

        #region events

        private void search_StateChanged(object sender, PasswordStateEventArgs e)
        {
            if (sender is Password)
            {
                Password search = (sender as Password);
                switch (e.SearchState)
                {
                    case SearchState.NotStarted:
                        this.Dispatcher.Invoke(() => ResetCount());
                        break;
                    case SearchState.Seeking:
                        this.Dispatcher.Invoke(() => SearchStarted());
                        break;
                    case SearchState.DoneSeeking:
                        this.Dispatcher.Invoke(() => SearchFinished());
                        break;
                    case SearchState.Cancelled:
                        this.Dispatcher.Invoke(() => SearchCancelled());
                        break;
                }
            }
        }

        #endregion

    }
}
