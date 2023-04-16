using Ookii.Dialogs.Wpf;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Windows;

namespace Accelera.WebUpdater
{
    public class WebUpdate
    {
        private UpdateInformation _updateInformation;
        private UpdateInformationReader _updateInformationReader;
        private BackgroundWorker _updateBackgroundWorker;
        private bool _isSearchForUpdatesFinished;
        private bool _areThereUpdates;
        private ProgressDialog _progressDialog;
        private string _updatedMd5;
        private string _downloadedFileName;
        private long _filesize;

        public WebUpdate(UpdateInformation updateInformation)
        {            
            _updateInformation = updateInformation;
            _isSearchForUpdatesFinished = true;
            _updateInformationReader = new UpdateInformationReader(updateInformation);
            _updateBackgroundWorker = new BackgroundWorker();
            _updateBackgroundWorker.WorkerSupportsCancellation = true;
            _updateBackgroundWorker.DoWork += new DoWorkEventHandler(UpdateBackgroundWorker);
            _updateBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(UpdateBackgroundWorkerCompleted);
        }

        public void DoUpdate()
        {
            if (!_updateBackgroundWorker.IsBusy)
            {
                _isSearchForUpdatesFinished = false;
                _areThereUpdates = false;
                _updateBackgroundWorker.RunWorkerAsync();
            }
        }

        public void CancelUpdate()
        {
            _updateBackgroundWorker.CancelAsync();
        }

        private void UpdateAvailable(UpdateInformation update)
        {
            TaskDialog dialog = new TaskDialog();
            {
                dialog.WindowTitle = "Update Available";
                dialog.MainInstruction = "New Version: " + update.Version + " available";
                dialog.Content = update.Description;
                dialog.ExpandedInformation = "MD5 Checksum: " + update.Md5;
                dialog.Footer = "Current Version: " + _updateInformation.ApplicationAssembly.GetName().Version;
                dialog.FooterIcon = TaskDialogIcon.Information;
                dialog.EnableHyperlinks = true;
                TaskDialogButton installButton = new TaskDialogButton("Install Update");
                TaskDialogButton cancelButton = new TaskDialogButton(ButtonType.Cancel);
                dialog.Buttons.Add(installButton);
                dialog.Buttons.Add(cancelButton);
                TaskDialogButton button = dialog.ShowDialog();
                if (button == installButton)
                {
                    DownloadUpdate(update);
                }                    
            }
        }

        private void DownloadUpdate(UpdateInformation update)
        {
            _progressDialog = new ProgressDialog();
            _progressDialog.WindowTitle = "Download";
            _progressDialog.Text = "Downloading and verifing the update package.";
            _progressDialog.Description = "downloading";
            _progressDialog.ShowTimeRemaining = true;

            _progressDialog.DoWork += new DoWorkEventHandler(DownloadingProgressWorker);
            _progressDialog.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DownloadingProgressWorkerCompleted);
            _updatedMd5 = update.Md5;
            _progressDialog.ShowDialog();

        }

        private void DownloadingProgressWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                Globals.Log.Info("Download process was cancled.");
                MessageBox.Show("There is no update available. Your software is up to date.", "Update");
            }             
            if ((int)e.Result == 8)
            {
                Globals.Log.Error("Error during download.");
                MessageBox.Show("Some error occured. Try again later and check your internet connection.", "Update");
            }
            if ((int)e.Result == 9)
            {
                Globals.Log.Error("MD5 checksum of downloaded file was wrong.");
                MessageBox.Show("Checksum of downloaded file does not fit.", "Update");
            }
            if ((int)e.Result == 3)
            {
                Globals.Log.Info("Installing update.");
                MessageBox.Show("Prepare for restart of the software.", "Update");
                ProcessStartInfo p = new ProcessStartInfo();
                p.FileName = _downloadedFileName;
                Process.Start(p);
                Environment.Exit(0); // this command kills the application      
            }
        }

        private void DownloadingProgressWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string tempFile;

            tempFile = Path.GetTempFileName();
            tempFile = Path.ChangeExtension(tempFile, "exe");

            try
            {
                using (Stream stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var client = new SftpClient(_updateInformation.Host, _updateInformation.UserName, _updateInformation.Password))
                {
                    if (_progressDialog.CancellationPending == false)
                    {
                        client.Connect();
                        SftpFileAttributes attributes = client.GetAttributes(_updateInformation.RemoteDirectory + _updateInformation.RemoteFileName);
                        _filesize = attributes.Size;
                        //download with progress callback
                        client.DownloadFile(_updateInformation.RemoteDirectory + _updateInformation.RemoteFileName, stream, DownloadProgressBar);
                        client.Disconnect();
                        stream.Flush();
                        stream.Close();
                        stream.Dispose();
                    } else
                    {
                        e.Cancel = true;
                        return;
                    }
                    // Hash the file and compare to the hash in the update xml
                    
                    var md5 = MD5.Create();
                    byte[] hash;
                    string md5str;
                    using (var fstream = File.OpenRead(tempFile))
                    {
                        hash = md5.ComputeHash(fstream);
                    }
                    string str = BitConverter.ToString(hash).Replace("-","").ToLowerInvariant();
                                       
                    if (str.ToUpper() != _updatedMd5.ToUpper())
                    {
                        e.Result = 9;  //hash is wrong
                        return;
                    } else
                    {
                        e.Result = 3; //file is correct
                        _downloadedFileName = tempFile;
                    }                    
                }
            }
            catch
            {
                e.Result = 8;
                return;
            }
        }
        private void DownloadProgressBar(ulong uploaded)
        {
            int progress = (int)((uploaded / (ulong)_filesize) * 100);
            _progressDialog.ReportProgress(progress);
        }
        private void UpdateBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateInformation update = new UpdateInformation();
            update = (UpdateInformation) e.Result;
            if (e.Cancelled == true)
            {
                Globals.Log.Info("Searching for updates was cancled.");
            }
            if (update == null)
            {
                Globals.Log.Info("No update was available.");
                MessageBox.Show("There is no update available. Your software is up to date.", "Update");
            }
            else
            {                
                //Check if the update is not null and is a newer version than the current application
                if (update.Version >_updateInformation.ApplicationAssembly.GetName().Version)
                {
                    Globals.Log.Info("Update was available and is newer than the installed one.");
                    UpdateAvailable(update);
                } else
                {
                    Globals.Log.Info("Update was available but is older than the installed one.");
                    MessageBox.Show("There is no update available. Your software is up to date.", "Update");
                }
            }
        }

        private void UpdateBackgroundWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while ((_updateBackgroundWorker.CancellationPending == false) & (_isSearchForUpdatesFinished == false))
            {
                if (!_updateInformationReader.ExistsOnServer())
                {
                    //there is no update, thus
                    _isSearchForUpdatesFinished = true;
                    _areThereUpdates = false;
                } else
                {
                    _isSearchForUpdatesFinished = true;
                    _areThereUpdates = true;
                }

            }
            if (_updateBackgroundWorker.CancellationPending == true)
            {
                _areThereUpdates = false;
                e.Cancel = true;
                return;
            }
            if ((_isSearchForUpdatesFinished == true) & (_areThereUpdates == false))
            {
                e.Result = null;       //this means no updates found
                return;
            }
            
            if ((_isSearchForUpdatesFinished == true) & (_areThereUpdates == true))
            {
                UpdateInformation result = new UpdateInformation();
                result = _updateInformationReader.Parse(_updateInformation.ApplicationId);
                e.Result = result;
                return;
            }
        }
    }
}
