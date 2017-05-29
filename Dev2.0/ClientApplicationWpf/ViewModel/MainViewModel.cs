using GalaSoft.MvvmLight;
using System.Windows;
using ClientApplicationWpf.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using ClientApplicationWpf.Model;
using System.Threading.Tasks.Dataflow;
using System.Net.Sockets;
using ClientApplicationWpf.APIs;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using ClientApplicationWpf.Helpers;
using System.Linq;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Command;
using System.Threading;

namespace ClientApplicationWpf.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        #region PrivateProperties
        private LoginViewModel _loginViewModel;
        private TcpCommunication _tcpCommunication;
        #endregion PrivateProperties

        #region PublicProperties
        private ObservableCollection<TraceItemViewModel> _traceItems;
        public ObservableCollection<TraceItemViewModel> TraceItems
        {
            get
            {
                return _traceItems;
            }

            set
            {
                _traceItems = value;
                RaisePropertyChanged(() => TraceItems);
            }
        }

        private Visibility _loginVisibility = Visibility.Visible;
        public Visibility LoginVisibility
        {
            get
            {
                return _loginVisibility;
            }

            set
            {
                if (_loginVisibility == value)
                    return;
                _loginVisibility = value;
                RaisePropertyChanged(() => LoginVisibility);
            }
        }

        private Visibility _traceVisibility = Visibility.Collapsed;
        public Visibility TraceVisibility
        {
            get
            {
                return _traceVisibility;
            }

            set
            {
                if (_traceVisibility == value)
                    return;
                _traceVisibility = value;
                RaisePropertyChanged(() => TraceVisibility);
            }
        }

        private Visibility _userDetailsVisibility = Visibility.Hidden;
        public Visibility UserDetailsVisibility
        {
            get
            {
                return _userDetailsVisibility;
            }

            set
            {
                if (_userDetailsVisibility == value)
                    return;
                _userDetailsVisibility = value;
                RaisePropertyChanged(() => UserDetailsVisibility);
            }
        }

        private Visibility _pauseVisibility = Visibility.Hidden;
        public Visibility PauseVisibility
        {
            get
            {
                return _pauseVisibility;
            }

            set
            {
                if (_pauseVisibility == value)
                    return;
                _pauseVisibility = value;
                RaisePropertyChanged(() => PauseVisibility);
            }
        }

        public string ConnectedUserName
        {
            get { return _loginViewModel.UserName; }
        }

        private bool _syncOnPause;
        public bool SyncOnPause
        {
            get { return _syncOnPause; }
            set
            {
                _syncOnPause = value;
                RaisePropertyChanged(() => SyncOnPause);
            }
        }

        private bool _traceEnabled;
        public bool TraceEnabled
        {
            get { return _traceEnabled; }
            set
            {
                if (_traceEnabled == value)
                    return;

                _traceEnabled = value;
                RaisePropertyChanged(() => TraceEnabled);
            }
        }
        

        private CommandHandler _commandHandler;
        public CommandHandler CommandHandler
        {
            get
            {
                return _commandHandler;
            }

            set
            {
                _commandHandler = value;
            }
        }

        private SyncProcessor _syncProcessor;
        public SyncProcessor SyncProcessor
        {
            get
            {
                return _syncProcessor;
            }

            set
            {
                _syncProcessor = value;
            }
        }

        private MyFsWatcher _myFsWatcher;

        public MyFsWatcher MyFsWatcher
        {
            get
            {
                return _myFsWatcher;
            }

            set
            {
                _myFsWatcher = value;
            }
        }
        #endregion PublicProperties

        #region Commands
        public RelayCommand LogoutCommand { get; private set; }
        public RelayCommand PlayPauseCommand { get; private set; }

        private void DoLogout()
        {
            var result = MessageBox.Show("Are you sure you want to close logout from the application ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                TraceItems.Clear();
                ManageUserLogout();
                SetupLayout(UiState.LoggedOut);
            }
        }

        private void DoPlayPause()
        {
            if(SyncOnPause)
            {
                // OnPause
                ManageUserLogout();
                SetupLayout(UiState.OnPause);
            }
            else
            {
                // OnPlay
                SetupLayout(UiState.LoggedIn);
                ManageUserLogin();
            }
        }
        #endregion Commands

        public MainViewModel(LoginViewModel loginViewModel)
        {
            _loginViewModel = loginViewModel;
            _loginVisibility = Visibility.Visible;
            _traceVisibility = Visibility.Collapsed;

            LogoutCommand = new RelayCommand(DoLogout);
            PlayPauseCommand = new RelayCommand(DoPlayPause);

            _traceItems = new ObservableCollection<TraceItemViewModel>();

            RegisterMesseges();
        }

        private void RegisterMesseges()
        {
            Messenger.Default.Register<LoginRequestMsg>(this, msg =>
            {
                ManageUserLogin();
            });

            Messenger.Default.Register<LoginCancelingMsg>(this, msg =>
            {
                var result = MessageBox.Show("Are you sure you want to close this application ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if(result == MessageBoxResult.Yes)
                    Application.Current.Shutdown();
            });

            Messenger.Default.Register<TraceItemAddedMsg>(this, msg =>
            {
                //TraceItems.Insert(0, msg.TraceItem);

                Action<TraceItemViewModel> addMethod = TraceItems.Add;
                Application.Current.Dispatcher.BeginInvoke(addMethod, msg.TraceItem);

                RaisePropertyChanged(() => TraceItems);
            });
        }

        private async void ManageUserLogin()
        {
            try
            {
                var changedFilesList = new ThreadSafeList<CustomFileHash>();
                var commandResponseBuffer = new BufferBlock<byte[]>();

                var tcpClient = new TcpClient(Helper.HostIp, Helper.HostPort);
                _tcpCommunication = new TcpCommunication(tcpClient, commandResponseBuffer, changedFilesList);


                var connectionBytes = Encoding.UTF8.GetBytes(_loginViewModel.UserName + ":" + _loginViewModel.UserPassword + ":");
                _tcpCommunication.SendCommand(connectionBytes, 0, connectionBytes.Length);
                await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
                var response = _tcpCommunication.CommandResponseBuffer.Receive();
                var message = Encoding.UTF8.GetString(response).Split(':');
                if (message[0].Equals("Error"))
                {
                    _tcpCommunication.Dispose();
                    Messenger.Default.Send(new LoginFailedMsg());
                    MessageBox.Show(message[1], @"Invalid user or password");
                }
                else
                {
                    RaisePropertyChanged(() => ConnectedUserName);
                    Logger.InitLogger(Helper.TraceEnabled);
                    Logger.WriteInitialSyncBreakLine();

                    _commandHandler = new CommandHandler(_tcpCommunication);
                    var filesForInitialSync = await DetermineFilesForInitialSync();
                    _syncProcessor = new SyncProcessor(_commandHandler, changedFilesList);
                    filesForInitialSync.ForEach(_syncProcessor.AddChangedFile);

                    SetupLayout(UiState.LoggedIn);

                    Messenger.Default.Send(new LoginSuccededMsg());

                    await _syncProcessor.ChangedFileManager();

                    Logger.WriteSyncBreakLine();
                    changedFilesList.OnAdd += changedFilesList_OnAdd;
                    _myFsWatcher = new MyFsWatcher(Helper.SyncLocation, _syncProcessor);
                }
            }
            catch (FormatException ex)
            {
                LoginVisibility = Visibility.Visible;
                Messenger.Default.Send(new LoginFailedMsg());

                var str = "Message: " + ex.Message +
                          "\nSource: " + ex.Source +
                          "\nStackTrace: " + ex.StackTrace;
                MessageBox.Show(str, @"Synchroniser - Format Exception");
            }
            catch (SocketException ex)
            {
                LoginVisibility = Visibility.Visible;
                Messenger.Default.Send(new LoginFailedMsg());

                var str = "Message: " + ex.Message +
                          "\nSource: " + ex.Source +
                          "\nStackTrace: " + ex.StackTrace;
                MessageBox.Show(str, @"Synchroniser - Socket Exception");
            }
            catch (Exception ex)
            {
                LoginVisibility = Visibility.Visible;
                Messenger.Default.Send(new LoginFailedMsg());

                var str = "Message: " + ex.Message +
                          "\nSource: " + ex.Source +
                          "\nException Type: " + ex.GetType() +
                          "\nStackTrace: " + ex.StackTrace;
                MessageBox.Show(str, @"Synchroniser - Exception");
            }
        }

        private void ManageUserLogout()
        {
            // Wait for SyncProcessor to be EMPTY first !!!
            while (_syncProcessor.On)
                Task.Delay(100);

            _commandHandler.Kill();

            _myFsWatcher.Dispose();
            Thread.Sleep(100);
        }

        private void changedFilesList_OnAdd(object sender, EventArgs e)
        {
            if (!SyncProcessor.On)
                SyncProcessor.ChangedFileManager();
        }

        private async Task<List<CustomFileHash>> DetermineFilesForInitialSync()
        {
            var clientFileHashes = new List<CustomFileHash>();
            var paths = Directory.GetFiles(Helper.SyncLocation, "*", SearchOption.AllDirectories)
                                    .Where(p => !p.Equals(Helper.SyncLocation + "\\desktop.ini"))
                                    .ToList();
            paths.ForEach(path => clientFileHashes.Add(new CustomFileHash(path)));
            var serverFilesHashes = await CommandHandler.GetAllFileHashes();
            var processedFileHashes = InitialSyncHelper.GetProcessedFileHashes(clientFileHashes, serverFilesHashes);

            return processedFileHashes;
        }



        private void SetupLayout(UiState uiState)
        {
            switch (uiState)
            {
                case UiState.OnPause:
                    TraceEnabled = false;
                    PauseVisibility = Visibility.Visible;
                    break;

                case UiState.LoggedOut:
                    LoginVisibility = Visibility.Visible;
                    TraceVisibility = Visibility.Collapsed;
                    UserDetailsVisibility = Visibility.Collapsed;

                    TraceEnabled = true;
                    PauseVisibility = Visibility.Hidden;
                    break;

                case UiState.LoggedIn:
                    LoginVisibility = Visibility.Hidden;
                    TraceVisibility = Visibility.Visible;
                    UserDetailsVisibility = Visibility.Visible;

                    TraceEnabled = true;
                    PauseVisibility = Visibility.Hidden;
                    break;
            }
        }
    }
}