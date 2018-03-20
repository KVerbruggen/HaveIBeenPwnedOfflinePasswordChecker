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
        #region enums

        public enum ResultBoxState
        {
            NotStarted,
            Seeking,
            DoneSeeking
        }

        #endregion

        #region fields

        private string hash;
        private int count = 0;
        private ResultBoxState state = ResultBoxState.NotStarted;

        #endregion

        #region properties

        private string Hash
        {
            get { return hash; }
            set
            {
                hash = value;
                txtResult.Content = hash;
            }
        }

        public ResultBoxState State
        {
            get { return state; }
            set
            {
                state = value;
                switch (value)
                {
                    case ResultBoxState.NotStarted:
                        txtCount.Foreground = new SolidColorBrush(Colors.Black);
                        txtCount.Visibility = Visibility.Hidden;
                        break;
                    case ResultBoxState.Seeking:
                        txtCount.Foreground = new SolidColorBrush(Colors.Black);
                        txtCount.Visibility = Visibility.Hidden;
                        animLoading.Visibility = Visibility.Visible;
                        break;
                    case ResultBoxState.DoneSeeking:
                        if (count > 0)
                        {
                            txtCount.Foreground = new SolidColorBrush(Colors.DarkRed);
                        }
                        else
                        {
                            txtCount.Foreground = new SolidColorBrush(Colors.DarkGreen);
                        }
                        txtCount.Visibility = Visibility.Visible;
                        animLoading.Visibility = Visibility.Hidden;
                        break;
                }
            }
        }

        public int Count
        {
            set
            {
                count = value;
                if (count > 0)
                {
                    txtCount.Content = count + " times found";
                }
                else
                {
                    txtCount.Content = "Hash not found";
                }
            }
        }

        #endregion

        #region constructors

        public ResultBox()
        {
            InitializeComponent();
            Hash = String.Empty;
        }

        #endregion

        #region methods

        public void StartedSeeking(string hash)
        {
            Hash = hash;
            State = ResultBoxState.Seeking;
        }

        public void DoneSeeking(int count)
        {
            this.Count = count;
            State = ResultBoxState.DoneSeeking;
        }

        #endregion

    }
}
