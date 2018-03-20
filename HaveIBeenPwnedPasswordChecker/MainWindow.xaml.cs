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

        private string Filepath {
            get { return String.IsNullOrEmpty(PWC.Filepath) ? "[None]" : PWC.Filepath; }
            set { PWC.Filepath = value; }
        }


        public MainWindow()
        {
            PWC.MainWindow = this;
            InitializeComponent();
            defaultPasswordControl.LinkedResultBox = defaultResultBox;
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
            btCheck.Content = "Check passwords";
        }

        public bool SetFilepath()
        {
            bool b = false;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (b = (openFileDialog.ShowDialog() == true))
            {
                Filepath = openFileDialog.FileName;
            }
            txt_filepath.Text = Filepath;
            return b;
        }

        public void AllSearchesFinished()
        {
            EnableSettingsControls();
        }

        #endregion

        #region events

        private void btAddPassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordControl newPassword = new PasswordControl();
            newPassword.RemovePasswordClick += RemovePassword_Click;
            // TODO - generate hash
            ResultBox newResultBox = new ResultBox();
            newPassword.LinkedResultBox = newResultBox;
            passwords.Children.Insert(passwords.Children.Count - 1, newPassword);
            results.Children.Insert(results.Children.Count - 1, newResultBox);
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
            if (String.IsNullOrEmpty(PWC.Filepath))
            {
                MessageBox.Show("Please point to the database file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!SetFilepath())
                {
                    return;
                }
            }
            DisableSettingsControls();
            if (sender is Button)
            {
                (sender as Button).Content = "Add passwords to check";
            }

            Queue<string> hashes = new Queue<string>();
            foreach(Control c in passwords.Children)
            {
                if (c is PasswordControl)
                {
                    PasswordControl pwc = (c as PasswordControl);
                    if (pwc.LinkedResultBox.State != ResultBox.ResultBoxState.Seeking)
                    {
                        string hash = PWC.Hash(pwc.Password);
                        hashes.Enqueue(hash);
                        pwc.LinkedResultBox.StartedSeeking(hash);
                        PWC.CreateSearchInFile(hash, pwc);
                    }
                }
            }
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
            PWC.SearchType = SearchType.OrderedByHash;
        }

        private void setSearchType_OrderByCount(object sender, RoutedEventArgs e)
        {
            PWC.SearchType = SearchType.OrderedByCount;
        }

        private void btStop_Click(object sender, RoutedEventArgs e)
        {
            PWC.StopSeeking();
        }

        #endregion

    }
}
