using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;

using ClientApplication.APIs; 
using ClientApplication.Models;
using ClientApplication.Processors;

namespace ClientApplication
{
    public partial class Main : Form
    {
        #region PrivateMembers  
        //private const string MyIp = "192.168.100.11";
        //private const string MyPort = "4444";

        //private const string MyIp = "193.226.9.250";
        //private const string MyPort = "4445";

        private const string MyIp = "10.6.99.254";
        private const string MyPort = "4444";

        private const bool UiConnectedState = true;
		private const bool UiDisconnectedState = false;

		private TcpCommunication _tcpCommunication;
		private CommandHandler _commandHandler;
		private SyncProcessor _syncProcessor;
		private MyFsWatcher _myFsWatcher;

		//private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
		//private CancellationToken _cancelationToken;
        #endregion PrivateMembers

        #region FormOperations
        public Main()
        {
            InitializeComponent();
        }
        private void Main_Load(object sender, EventArgs e)
        {
            var tabs = tabApplicationMode.TabPages;
            tabApplicationMode.SelectedTab = tabs[1];
            //tabApplicationMode.SelectedTab = tabs[0];
	        //btnConnectAuto_Click(null, null);
        }
        private void tabApplicationMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabApplicationMode.SelectedIndex == 0)
            {
                SwitchManualUiState(UiDisconnectedState);

                txtHost.Text = MyIp;
                txtPort.Text = MyPort;

				txtGetFileName.Text = @"Music/Flume-LeftAlone.mp3";
            }
            else if (tabApplicationMode.SelectedIndex == 1)
            {
                SwitchAutoUiState(UiDisconnectedState);

                txtHostAuto.Text = MyIp;
                txtPortAuto.Text = MyPort;

                txtDefaultFolderAuto.Text = Helper.SyncLocation;
            }
        }
        #endregion FormOperations

        #region Manual
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
				Context.InAutoMode = false;
				
				var tcpClient = new TcpClient(txtHostAuto.Text, Int32.Parse(txtPortAuto.Text));
				var tcpCommunication = new TcpCommunication(tcpClient);
				_commandHandler = new CommandHandler(tcpCommunication);

                SwitchManualUiState(UiConnectedState);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
				_commandHandler.Kill();
                SwitchManualUiState(false);
                Logger.WriteDisconnectLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnGetConfirm_Click(object sender, EventArgs e)
        {
            //if (String.IsNullOrEmpty(txtGetFileName.Text)) return;

            //try
            //{
            //    var fullPath = Helper.SyncLocation + txtGetFileName.Text.Replace('/', '\\');
            //    var relPath = Helper.GetRelativePath(fullPath);

            //    var response = await _commandHandler.Get(txtGetFileName.Text);

            //    if (response)
            //    {
            //        switch (Path.GetExtension(fullPath))
            //        {
            //            case ".exe":
            //                break;
            //            case ".txt":
            //                Process.Start("notepad++.exe", fullPath);
            //                break;
            //            case ".mp3":
            //                Process.Start("wmplayer.exe", fullPath);
            //                break;
            //            default:
            //                MessageBox.Show(Path.GetExtension(fullPath) + @" extension is not treated");
            //                break;
            //        }
            //    }
            //    else MessageBox.Show("The file was empty !");
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
            //}
        }

        private void btnPutBrowse_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                InitialDirectory = @"C:\SyncRootDirectory"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtPutPath.Text = dlg.FileName;
            }
        }

        private void btnPutConfirm_Click(object sender, EventArgs e)
        {
            //var chf = new CustomFileHash(txtPutPath.Text);
			//await _commandHandler.Put(chf);
        }

        private void btnRenameFromBrowse_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                InitialDirectory = @"C:\SyncRootDirectory"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtRenameFrom.Text = dlg.FileName;
            }
        }
        private void btnRenameConfirm_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    var oldFileName = txtRenameFrom.Text;
            //    var newFileName = txtRenameTo.Text;

            //    var success = await _commandHandler.Rename(oldFileName, newFileName);
                
            //    if (success)
            //        MessageBox.Show("File " + oldFileName + " was renamed to " + newFileName + " succesfully!", @"Success!", MessageBoxButtons.OK);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK);
            //}
        }

        private void btnDeleteConfirm_Click(object sender, EventArgs e)
        {
            //try
            //{
	           // var success = await _commandHandler.Delete(txtDeleteFileName.Text);

            //    if (success)
            //        MessageBox.Show(@"File/directory " + txtDeleteFileName.Text + @" was deleted succesfully!", @"Success!", MessageBoxButtons.OK);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
        }

        private void btnNewFolderConfirm_Click(object sender, EventArgs e)
        {
    //        try
    //        {
				//var success = await _commandHandler.Mkdir(txtNewFolderName.Text);

    //            if (success)
    //                MessageBox.Show(@"Folder " + txtDeleteFileName.Text + @" was created succesfully!", @"Success!", MessageBoxButtons.OK);

    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show(ex.Message);
    //        }
        }

        private void SwitchManualUiState(bool uiState)
        {
            btnConnect.Enabled = !uiState;
            txtHost.Enabled = !uiState;
            txtPort.Enabled = !uiState;

            btnDisconnect.Enabled = uiState;

            txtGetFileName.Enabled = uiState;
            txtPutPath.Enabled = uiState;

            btnGetConfirm.Enabled = uiState;
            btnGetFileHashes.Enabled = uiState;
            btnPutBrowse.Enabled = uiState;
            btnPutConfirm.Enabled = uiState;

            txtRenameFrom.Enabled = uiState;
            txtRenameTo.Enabled = uiState;
            btnRenameFromBrowse.Enabled = uiState;
            btnRenameConfirm.Enabled = uiState;

            txtDeleteFileName.Enabled = uiState;
            btnDeleteConfirm.Enabled = uiState;

            txtNewFolderName.Enabled = uiState;
            btnNewFolderConfirm.Enabled = uiState;
        }

        private async void btnGetFileHashes_Click(object sender, EventArgs e)
        {
            try
            {
                var str = string.Empty;
				var fileHashes = await _commandHandler.GetAllFileHashes();
                fileHashes.ForEach(fh => str+= fh.ToString() + "\n\n");

                var filePath = Application.StartupPath + "FileHashes.txt";
                File.WriteAllText(filePath, str);

                Process.Start("notepad++.exe", filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion Manual

        #region Auto
	    private Task _loggerTask;

	    private async void btnConnectAuto_Click(object sender, EventArgs e)
        {
            try
            {
                Context.InAutoMode = true;

	            if (Helper.TraceEnabled)
	            {
		            Logger.InitLogger(true);
					_loggerTask = Task.Factory.StartNew(LoggerAction);
	            }
	            else
	            {
		            Logger.InitLogger();
		            _loggerTask = null;
                }


                var changedFilesList = new ThreadSafeList<CustomFileHash>();
				var commandResponseBuffer = new BufferBlock<byte[]>();

				var tcpClient = new TcpClient(txtHostAuto.Text, Int32.Parse(txtPortAuto.Text));
				_tcpCommunication = new TcpCommunication(tcpClient, commandResponseBuffer, changedFilesList);


				var connectionBytes = Encoding.UTF8.GetBytes(txtUsername.Text + ":" + txtUserpassword.Text + ":");
				_tcpCommunication.SendCommand(connectionBytes, 0, connectionBytes.Length);
				await _tcpCommunication.CommandResponseBuffer.OutputAvailableAsync();
				var response = _tcpCommunication.CommandResponseBuffer.Receive();
				var message = Encoding.UTF8.GetString(response).Split(':');
	            if (message[0].Equals("Error"))
	            {
		            MessageBox.Show(message[1], @"Invalid user or password");
		            _tcpCommunication.Dispose();
	            }
	            else
                {
                    SwitchAutoUiState(UiConnectedState);
                    Context.CurrentUser = txtUsername.Text;
                    _commandHandler = new CommandHandler(_tcpCommunication);

					Logger.WriteInitialSyncBreakLine();
					var filesForInitialSync = await DetermineFilesForInitialSync();
					_syncProcessor = new SyncProcessor(_commandHandler, changedFilesList);
                    filesForInitialSync.ForEach(_syncProcessor.AddChangedFile);
                    await _syncProcessor.ChangedFileManager();

                    Logger.WriteSyncBreakLine();
					changedFilesList.OnAdd += changedFilesList_OnAdd;
					_myFsWatcher = new MyFsWatcher(txtDefaultFolderAuto.Text, _syncProcessor);
	            }
			}
			catch (FormatException fex)
			{
				var str = "Message: " + fex.Message +
						  "\nSource: " + fex.Source +
						  "\nStackTrace: " + fex.StackTrace;
				MessageBox.Show(str, @"Syncroniser - FormatException");
				SwitchAutoUiState(false);
				MessageBox.Show(fex.Message);
			}
			catch (Exception ex)
			{
				var str = "Message: " + ex.Message +
						  "\nSource: " + ex.Source +
						  "\nStackTrace: " + ex.StackTrace;
				MessageBox.Show(str, @"Syncroniser - Exception");
				SwitchAutoUiState(false);
				MessageBox.Show(ex.Message);
			}
        }

	    private void changedFilesList_OnAdd(object sender, EventArgs e)
        {
            if (!_syncProcessor.On)
                _syncProcessor.ChangedFileManager();
        }

	    private void LoggerAction()
		{
			while (true)
			{
				try
				{
					Invoke((MethodInvoker) delegate
					{
						try
						{
							if (lbTrace.Items.Count > 0)
								lbTrace.Items.Clear();
							Helper.TraceItems.ForEach(i => lbTrace.Items.Add(i));
						}
						catch (Exception ex)
						{
							Logger.WriteLine("Tracer error: " + ex.Message);
						}

						//if (_cancelationToken.IsCancellationRequested)
						//	_cancelationToken.ThrowIfCancellationRequested();
					});
					Thread.Sleep(1000);
				}
				catch (Exception)
				{
					return;
				}
			}
	    }

	    private async Task<List<CustomFileHash>> DetermineFilesForInitialSync()
		{
			var clientFileHashes = new List<CustomFileHash>();
			var paths = Directory.GetFiles(Helper.SyncLocation, "*", SearchOption.AllDirectories)
									.Where(p => !p.Equals(Helper.SyncLocation + "\\desktop.ini"))
									.ToList();
            paths.ForEach(path => clientFileHashes.Add(new CustomFileHash(path)));
            var serverFilesHashes = await _commandHandler.GetAllFileHashes();
            var processedFileHashes = InitialSyncHelper.GetProcessedFileHashes(clientFileHashes, serverFilesHashes);

            return processedFileHashes;
		}

        private void btnDisconnectAuto_Click(object sender, EventArgs e)
        {
            try
			{
//				if (_loggerTask != null)
//					_tokenSource.Cancel();

				_syncProcessor.Dispose();
				_syncProcessor = null;

				_commandHandler.Kill();
				_commandHandler.Dispose();
				_commandHandler = null;

				_tcpCommunication.Dispose();
				_tcpCommunication = null;

				_myFsWatcher.Dispose();
				_myFsWatcher = null;

				SwitchAutoUiState(false);
                Logger.WriteDisconnectLine();
            }
            catch (Exception ex)
			{
				SwitchAutoUiState(false);
				MessageBox.Show(ex.Message, @"Disconnect");
            }
        }

        private void btnBrowseDefFolderAuto_Click(object sender, EventArgs e)
        {
            var dlg = new FolderBrowserDialog
            {
                SelectedPath = txtDefaultFolderAuto.Text
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtDefaultFolderAuto.Text = dlg.SelectedPath;
            }
        }

        private void SwitchAutoUiState(bool uiState)
        {
			if (Helper.TraceEnabled && uiState)
			{
				Height = 745;
				lbTrace.Visible = true;
			}
			else if (Helper.TraceEnabled && !uiState)
			{
				Height = 400;
				lbTrace.Visible = false;
			}

            txtUsername.Enabled = !uiState;
            txtUserpassword.Enabled = !uiState;

            btnConnectAuto.Enabled = !uiState;
            btnDisconnectAuto.Enabled = uiState;
            btnBrowseDefFolderAuto.Enabled = !uiState;

            txtHostAuto.Enabled = !uiState;
            txtPortAuto.Enabled = !uiState;

            txtDefaultFolderAuto.Enabled = !uiState;
        }
        #endregion Auto
    }
}
