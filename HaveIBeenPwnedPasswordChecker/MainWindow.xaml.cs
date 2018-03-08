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

        public string Filepath {
            get { return String.IsNullOrEmpty(filepath) ? "[..]" : filepath; }
            private set { filepath = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
            defaultPasswordControl.LinkedResultBox = defaultResultBox;
        }

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
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                this.filepath = openFileDialog.FileName;
            }
        }

        private void btCheck_Click(object sender, RoutedEventArgs e)
        {
            foreach(Control c in passwords.Children)
            {
                if (c is PasswordControl)
                {
                    PasswordControl pwc = (c as PasswordControl);
                    string hash = PWC.Hash(pwc.Password);
                    pwc.LinkedResultBox.StartedSeeking(hash);
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

        #endregion

    }
}
