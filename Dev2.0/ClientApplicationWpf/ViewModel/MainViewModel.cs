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

        public MainViewModel(LoginViewModel loginViewModel)
        {
            //InitializeHelper();

            _loginViewModel = loginViewModel;
            _loginVisibility = Visibility.Visible;
            _traceVisibility = Visibility.Collapsed;

            _traceItems = new ObservableCollection<TraceItemViewModel>();

            RegisterMesseges();
        }

        //private void InitializeHelper()
        //{
        //    var type = typeof(Helper);
        //    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        //}

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
                    LoginVisibility = Visibility.Visible;
                    Messenger.Default.Send(new LoginFailedMsg());
                    MessageBox.Show(message[1], @"Invalid user or password");
                }
                else
                {
                    Logger.InitLogger(Helper.TraceEnabled);
                    Logger.WriteInitialSyncBreakLine();

                    _commandHandler = new CommandHandler(_tcpCommunication);
                    var filesForInitialSync = await DetermineFilesForInitialSync();
                    _syncProcessor = new SyncProcessor(_commandHandler, changedFilesList);
                    filesForInitialSync.ForEach(_syncProcessor.AddChangedFile);

                    LoginVisibility = Visibility.Hidden;
                    TraceVisibility = Visibility.Visible;
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
                MessageBox.Show(str, @"Syncroniser - FormatException");
            }
            catch (SocketException ex)
            {
                LoginVisibility = Visibility.Visible;
                Messenger.Default.Send(new LoginFailedMsg());

                var str = "Message: " + ex.Message +
                          "\nSource: " + ex.Source +
                          "\nStackTrace: " + ex.StackTrace;
                MessageBox.Show(str, @"Syncroniser - SocketException");
            }
            catch (Exception ex)
            {
                LoginVisibility = Visibility.Visible;
                Messenger.Default.Send(new LoginFailedMsg());

                var str = "Message: " + ex.Message +
                          "\nSource: " + ex.Source +
                          "\nException Type: " + ex.GetType() +
                          "\nStackTrace: " + ex.StackTrace;
                MessageBox.Show(str, @"Syncroniser - Exception\nMessage: " + ex.Message);
            }
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
    }
}