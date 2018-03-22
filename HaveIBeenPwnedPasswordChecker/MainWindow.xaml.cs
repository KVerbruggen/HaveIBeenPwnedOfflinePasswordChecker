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
using Microsoft.Win32;
using PasswordChecker.controls;

namespace PasswordChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string filepath = String.Empty;

        private SearchType searchType = SearchType.OrderedByCount;

        private string Filepath {
            get { return String.IsNullOrEmpty(filepath) ? "[None]" : filepath; }
            set { filepath = value; }
        }

        public MainWindow()
        {
            PWC.AllSearchesFinished += this.AllSearchesFinished;
            PWC.SearchStarted += this.SearchStarted;

            InitializeComponent();

            AddPassword();
        }

        #region methods

        public void DisableSettingsControls()
        {
            rbOrderByHash.IsEnabled = false;
            rbOrderByCount.IsEnabled = false;
            btFilepath.IsEnabled = false;
            btStop.IsEnabled = true;
        }

        public void EnableSettingsControls()
        {
            rbOrderByHash.IsEnabled = true;
            rbOrderByCount.IsEnabled = true;
            btFilepath.IsEnabled = true;
            btStop.IsEnabled = false;
        }

        public void SetFilepath()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                Filepath = openFileDialog.FileName;
                txt_filepath.Text = Filepath;
            }
        }

        public PasswordControl AddPassword()
        {
            Password newPassword = new Password();

            PasswordControl newPasswordControl = new PasswordControl(newPassword);
            newPasswordControl.RemovePasswordClick += RemovePassword_Click;
            newPasswordControl.EnterDown += pwc_EnterDown;

            ResultBox newResultBox = new ResultBox(newPassword);
            newPasswordControl.LinkedResultBox = newResultBox;

            passwords.Children.Insert(passwords.Children.Count - 1, newPasswordControl);
            results.Children.Insert(results.Children.Count - 1, newResultBox);

            svPasswords.ScrollToBottom();
            svHashes.ScrollToBottom();

            return newPasswordControl;
        }

        #endregion

        #region events

        public void AllSearchesFinished(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                EnableSettingsControls();
                btCheck.Content = "Check passwords";
            });
        }

        public void SearchStarted(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                DisableSettingsControls();
                btCheck.Content = "Add passwords to check";
            });
        }

        private void pwc_EnterDown(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordControl)
            {
                PasswordControl pwc = (sender as PasswordControl);
                
                if (pwc == passwords.Children[passwords.Children.Count - 2])
                {
                    AddPassword().passwordbox.Focus();
                }
                else
                {
                    int index = passwords.Children.IndexOf(pwc);
                    PasswordControl pwcNewFocus = passwords.Children[index + 1] as PasswordControl;
                    pwcNewFocus.passwordbox.Focus();
                }
            }
        }

        private void btAddPassword_Click(object sender, RoutedEventArgs e)
        {
            AddPassword();
        }

        private void RemovePassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordControl pwc_sender = (sender as PasswordControl);
            if (passwords.Children.Count > 2)
            {
                if (pwc_sender.LinkedResultBox != null)
                {
                    results.Children.Remove(pwc_sender.LinkedResultBox);
                }
                passwords.Children.Remove(pwc_sender);
            }
        }

        private void btFilepath_Click(object sender, RoutedEventArgs e)
        {
            SetFilepath();
        }

        private void btCheck_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(filepath))
            {
                MessageBox.Show("No file selected. Please select a passwordhash database and indicate whether it is ordered by count or by hash.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<Password> searches = new List<Password>();
            foreach(Control c in passwords.Children)
            {
                if (c is PasswordControl)
                {
                    PasswordControl pwc = (c as PasswordControl);
                    if (!pwc.Password.IsLocked && pwc.Edited)
                    {
                        pwc.Password.Hash = PWC.Hash(pwc.PasswordString);
                        searches.Add(pwc.Password);
                    }
                }
            }
            PWC.CreateSearchesInFile(filepath, searches, searchType);
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Source: https://stackoverflow.com/questions/15151974/synchronized-scrolling-of-two-scrollviewers-whenever-any-one-is-scrolled-in-wpf
            if (sender == svPasswords)
            {
                svHashes.ScrollToVerticalOffset(e.VerticalOffset);
                svHashes.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            else
            {
                svPasswords.ScrollToVerticalOffset(e.VerticalOffset);
                svPasswords.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void setSearchType_OrderByHash(object sender, RoutedEventArgs e)
        {
            searchType = SearchType.OrderedByHash;
        }

        private void setSearchType_OrderByCount(object sender, RoutedEventArgs e)
        {
            searchType = SearchType.OrderedByCount;
        }

        private void btStop_Click(object sender, RoutedEventArgs e)
        {
            PWC.StopSeeking();
        }

        #endregion
    }
}
