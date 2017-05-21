using ClientApplicationWpf.ViewModel;
using System.Windows.Controls;

namespace ClientApplicationWpf.View
{
    public partial class LoginControl : UserControl
    {
        public LoginControl()
        {
            InitializeComponent();
            userPassword.Password = "passw1";
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender != null)
            {
                var pwd = (PasswordBox)sender;

                if (pwd != null && pwd.Password != null)
                    ((LoginViewModel)(pwd).DataContext).UserPassword = pwd.Password;
            }
        }
    }
}
