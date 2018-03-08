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

        public ResultBox LinkedResultBox { get; set; }

        public string Password {
            get { return textbox.Text; }
            set { textbox.Text = value; }
        }

        #endregion

        #region constructors

        public PasswordControl()
        {
            InitializeComponent();
        }

        #endregion

        #region events

        private void btRemovePassword_Click(object sender, RoutedEventArgs e)
        {
            RemovePasswordClick?.Invoke(this, e);
        }

        #endregion

    }
}
