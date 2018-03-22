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

        public delegate void KeyDownHandler(object sender, KeyEventArgs e);
        public event KeyDownHandler EnterDown;

        #endregion

        #region properties

        public Password Password { get; private set; }
        public ResultBox LinkedResultBox { get; set; }

        public string PasswordString
        {
            get { return passwordbox.Password; }
        }

        public bool Edited
        {
            get; private set;
        }

        #endregion

        #region constructors

        public PasswordControl(Password password)
        {
            Password = password;
            Password.StateChanged += password_StateChanged;

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
            Password.Reset();
            Edited = true;
        }

        private void password_StateChanged(object sender, PasswordStateEventArgs e)
        {
            if (sender is Password)
            {
                Password password = (sender as Password);
                if (e.SearchState == SearchState.DoneSeeking)
                {
                    Edited = false;
                }
                this.Dispatcher.Invoke(() => this.IsEnabled = (e.SearchState != SearchState.Seeking));
            }
        }

        private void passwordbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.EnterDown?.Invoke(this, e);
            }
        }

        #endregion
    }
}
