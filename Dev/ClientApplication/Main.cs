using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using ClientApplication.APIs;
using ClientApplication.Models;
using ClientApplication.Processors;

namespace ClientApplication
{
    public partial class Main : Form
    {
        #region PrivateMembers 

		private const string MyIp = "193.226.9.250";			//10.6.99.254
		private const string MyPort = "4445";					//4444
        private TcpCommunication _myTcp;
        private MyFsWatcher _myFsWatcher;
        private const Boolean UiConnectedState = true;
        private const Boolean UiDisconnectedState = false;
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
//            tabApplicationMode.SelectedTab = tabs[0];

	        btnConnectAuto_Click(null, null);
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
                _myTcp = new TcpCommunication(txtHost.Text, Int32.Parse(txtPort.Text));
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
                _myTcp.Kill();
                _myTcp = null;
                _myFsWatcher = null;
                SwitchManualUiState(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnGetConfirm_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtGetFileName.Text)) return;
            
            try
            {
                var fullPath = Helper.SyncLocation + txtGetFileName.Text.Replace('/', '\\');
	            var relPath = Helper.GetRelativePath(fullPath);

				var response = _myTcp.Get(txtGetFileName.Text);
                    
                if (response)
                {
                    switch (Path.GetExtension(fullPath))
                    {
                        case ".exe":
                            break;
                        case ".txt":
                            Process.Start("notepad++.exe", fullPath);
                            break;
                        case ".mp3":
                            Process.Start("wmplayer.exe", fullPath);
                            break;
                        default:
                            MessageBox.Show(Path.GetExtension(fullPath) + @" extension is not treated");
                            break;
                    }
                }
                else MessageBox.Show("The file was empty !");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
            }
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
            var chf = new CustomFileHash(txtPutPath.Text);
            _myTcp.Put(chf);
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
            try
            {
                var oldFileName = txtRenameFrom.Text;
                var newFileName = txtRenameTo.Text;

                var success = _myTcp.Rename(oldFileName, newFileName);
                
                if (success)
                    MessageBox.Show("File " + oldFileName + " was renamed to " + newFileName + " succesfully!", @"Success!", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK);
            }
        }

        private void btnDeleteConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                var success = _myTcp.Delete(txtDeleteFileName.Text);

                if (success)
                    MessageBox.Show(@"File/directory " + txtDeleteFileName.Text + @" was deleted succesfully!", @"Success!", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnNewFolderConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                var success = _myTcp.Mkdir(txtNewFolderName.Text);

                if (success)
                    MessageBox.Show(@"Folder " + txtDeleteFileName.Text + @" was created succesfully!", @"Success!", MessageBoxButtons.OK);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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

        private void btnGetFileHashes_Click(object sender, EventArgs e)
        {
            try
            {
                var str = string.Empty;
                var fileHashes = _myTcp.GetAllFileHashes();
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
        private void btnConnectAuto_Click(object sender, EventArgs e)
        {
            try
            {
                Context.InAutoMode = true;

				_myTcp = new TcpCommunication(txtHostAuto.Text, Int32.Parse(txtPortAuto.Text));
				StartLogger();
				Task.Factory.StartNew(StartSynchroniser);
                
				SwitchAutoUiState(UiConnectedState);
            }
            catch (Exception ex)
			{
				SwitchAutoUiState(false);
	            MessageBox.Show(ex.Message);
            }
        }

		private void StartSynchroniser()
	    {
		    var syncProcessor = new SyncProcessor(_myTcp);

			Logger.WriteInitialSyncBreakLine();
		    DetermineFilesForInitialSync().ForEach(syncProcessor.AddChangedFile);
			syncProcessor.ChangedFileManager();
//			var initialSyncTask = Task.Factory.StartNew();
//			initialSyncTask.Wait();

			Logger.WriteSyncBreakLine();
			_myFsWatcher = new MyFsWatcher(txtDefaultFolderAuto.Text, syncProcessor);
	    }


		// Pune-l ca thread separat de la apel
	    private void StartLogger()
	    {
		    if (Helper.TraceEnabled)
		    {
			    Logger.InitLogger(true);
			    Task.Factory.StartNew(() =>
			    {
				    while (true)
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
					    });
					    Thread.Sleep(1000);
				    }
			    });
		    }
		    else Logger.InitLogger();
	    }

	    private List<CustomFileHash> DetermineFilesForInitialSync()
		{
			var clientFileHashes = new List<CustomFileHash>();
			var paths = Directory.GetFiles(Helper.SyncLocation, "*", SearchOption.AllDirectories)
									.Where(p => !p.Equals(Helper.SyncLocation + "\\desktop.ini"))
									.ToList();
			paths.ForEach(path => clientFileHashes.Add(new CustomFileHash(path)));

			var serverFilesHashes = _myTcp.GetAllFileHashes();
			var processedFileHashes = InitialSyncHelper.GetProcessedFileHashes(clientFileHashes, serverFilesHashes);

			return processedFileHashes;
		}

        private void btnDisconnectAuto_Click(object sender, EventArgs e)
        {
            try
            {
                _myTcp.Kill();

                _myTcp.Dispose();
                _myFsWatcher.Dispose();

				SwitchAutoUiState(false);
            }
            catch (Exception ex)
			{
				SwitchAutoUiState(false);
                MessageBox.Show(ex.Message);
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
