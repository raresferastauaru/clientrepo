using ClientApplicationWpf.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Threading;
using System.Windows;
using System;
using System.Windows.Forms;

namespace ClientApplicationWpf.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        #region Properties
        private string _userName = string.Empty;
        public string UserName
        {
            get
            {
                return _userName;
            }

            set
            {
                if (_userName == value)
                    return;
                _userName = value;
                RaisePropertyChanged(() => UserName);
            }
        }

        private string _userPassword = string.Empty;
        public string UserPassword
        {
            get
            {
                return _userPassword;
            }

            set
            {
                if (_userPassword == value)
                    return;
                _userPassword = value;
                RaisePropertyChanged(() => UserPassword);
            }
        }

        private string _syncFolderLocation = string.Empty;
        public string SyncFolderLocation
        {
            get { return _syncFolderLocation; }
            set
            { 
                _syncFolderLocation = value;
                if (!_syncFolderLocation[_syncFolderLocation.Length - 1].Equals('\\'))
                    _syncFolderLocation += "\\";

                RaisePropertyChanged(() => SyncFolderLocation);
            }
        }

        private string _loggerFolderLocation = string.Empty;
        public string LoggerFolderLocation
        {
            get { return _loggerFolderLocation; }
            set
            {
                _loggerFolderLocation = value;
                if (!_loggerFolderLocation[_loggerFolderLocation.Length - 1].Equals('\\'))
                    _loggerFolderLocation += "\\";

                RaisePropertyChanged(() => LoggerFolderLocation);
            }
        }

        private Visibility _loading = Visibility.Hidden;
        public Visibility Loading
        {
            get { return _loading; }
            set
            {
                _loading = value;
                RaisePropertyChanged(() => Loading);
            }
        }

        private bool _boxesEnabled = true;
        public bool BoxesEnabled
        {
            get { return _boxesEnabled; }
            set
            {
                _boxesEnabled = value;
                RaisePropertyChanged(() => BoxesEnabled);
            }
        }

        private bool _rememberUser;
        public bool RememberUserDetails
        {
            get { return _rememberUser; }
            set
            {
                if (_rememberUser == value)
                    return;
                _rememberUser = value;
                RaisePropertyChanged(() => RememberUserDetails);
            }
        }

        private bool _configureApp = false;
        public bool ConfigureApp
        {
            get { return _configureApp; }
            set
            {
                _configureApp = value;

                if (_configureApp)
                    _configuringApp = Visibility.Visible;
                else
                    _configuringApp = Visibility.Collapsed;

                RaisePropertyChanged(() => ConfigureApp);
                RaisePropertyChanged(() => ConfiguringApp);
            }
        }

        private Visibility _configuringApp = Visibility.Collapsed;
        public Visibility ConfiguringApp
        {
            get { return _configuringApp; }
        }
        #endregion Properties

        #region Commands
        public RelayCommand LoginCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand BrowseSyncFolderCommand { get; private set; }
        public RelayCommand BrowseLoggingFolderCommand { get; private set; }

        private void RegisterMesseges()
        {
            Messenger.Default.Register<LoginSuccededMsg>(this, msg =>
            {
                BoxesEnabled = true;
                Loading = Visibility.Hidden;
            });

            Messenger.Default.Register<LoginFailedMsg>(this, msg =>
            {
                BoxesEnabled = true;
                Loading = Visibility.Hidden;
            });
        }

        public void DoLogin()
        {
            BoxesEnabled = false;
            Loading = Visibility.Visible;

            if (ConfigureApp)
            {
                Helper.SyncLocation = SyncFolderLocation;
                Helper.LoggerLocation = LoggerFolderLocation;
            }

            if (RememberUserDetails)
            {
                Helper.UserName = UserName;
                Helper.UserPassword = UserPassword;
            }
            else
            {
                Helper.UserName = "";
                Helper.UserPassword = "";
            }
            Helper.RememberUserDetails = RememberUserDetails;

            RaisePropertyChanged(() => UserName);
            Messenger.Default.Send(new LoginRequestMsg());
        }

        public void DoCancel()
        {
            Messenger.Default.Send(new LoginCancelingMsg());
        }

        private void DoBrowseSyncFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    SyncFolderLocation = dialog.SelectedPath;
                }
            }
        }

        private void DoBrowseLoggingFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    LoggerFolderLocation = dialog.SelectedPath;
                }
            }

        }
        #endregion Commands

        public LoginViewModel()
        {
            UserName = Helper.UserName;
            RememberUserDetails = Helper.RememberUserDetails;
            SyncFolderLocation = Helper.SyncLocation;
            LoggerFolderLocation = Helper.LoggerLocation;
            ConfigureApp = false;

            LoginCommand = new RelayCommand(DoLogin);
            CancelCommand = new RelayCommand(DoCancel);
            BrowseSyncFolderCommand = new RelayCommand(DoBrowseSyncFolder);
            BrowseLoggingFolderCommand = new RelayCommand(DoBrowseLoggingFolder);

            BoxesEnabled = true;
            Loading = Visibility.Hidden;

            RegisterMesseges();
        }
    }
}
