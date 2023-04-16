using Accelera.Models;
using Accelera.Properties;
using Accelera.WebUpdater;
using MicroMvvm;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace Accelera.ViewModels
{
    internal class AboutDialogViewModel : ObservableObject
    {
        #region Interface implementation for Updater (Web-Update of the Application)
        public string ApplicationName { get; } = "Accelera";

        public string ApplicationId { get; } = "Accelera";

        public Assembly ApplicationAssembly { get; } = Assembly.GetExecutingAssembly();

        
        #endregion

        #region Private Members   
        /// <summary>
        /// This property is needed to bind the DialogResult of the window. See here for details:
        /// https://stackoverflow.com/questions/501886/how-should-the-viewmodel-close-the-form/3329467#3329467
        /// </summary>
        private bool? _dialogResult;

        private bool _canExport;
        private bool _canUpdate;
        private bool _canClose;
        private SystemSettings _settings = new SystemSettings();
        #endregion

        #region Public Members
        public bool? DialogResult
        {
            get => _dialogResult;

            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }

        public string ProductName
        {
            get => _settings.AssemblyProduct;
        }

        public string Version
        {
            get => String.Format("Version {0}", _settings.AssemblyVersion);
        }

        public string Copyright
        {
            get => _settings.AssemblyCopyright;
        }

        public string Company
        {
            get => _settings.AssemblyCompany;
        }
        public string Description
        {
            get =>  _settings.AssemblyDescription;
        }
        #endregion

        #region Constructors
        public AboutDialogViewModel()
        {
            _canClose = true;
            _canExport = true;
            _canUpdate = true;
            Globals.Log.Wpf("== AboutDialogViewModel ==");
        }
        #endregion

        #region Commands

        public ICommand ExportButtonClicked { get { return new RelayCommand(OnExportButtonClicked, CanExportButtonBeExecuted); } }       
        public ICommand WebUpdateButtonClicked { get { return new RelayCommand(OnWebUpdateButtonClicked, CanWebUpdateButtonBeExecuted); } }
        public ICommand CloseButtonClicked { get { return new RelayCommand(OnCloseButtonClicked, CanCloseButtonBeExecuted); } }

        private bool CanExportButtonBeExecuted()
        {
            return _canExport;
        }

        private bool CanWebUpdateButtonBeExecuted()
        {
            return _canUpdate;
        }

        private bool CanCloseButtonBeExecuted()
        {
            return _canClose;
        }

        private void OnExportButtonClicked()
        {
            _canExport = false;
            _canUpdate = false;
            _canClose = false;
            Globals.Log.Info("Export Log Files.");
            string source = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\MIBE\\Accelera\\Logs\\";
            string[] filePaths = Directory.GetFiles(source);
            string destination;

            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true;

            if (!VistaFolderBrowserDialog.IsVistaFolderDialogSupported)
            {
                Globals.Log.Warn("Wrong Windows Version.");
            }

            if ((bool)dialog.ShowDialog())
            {
                destination = dialog.SelectedPath;
                Globals.Log.Info("Destination folder: " + destination);

                //copy log files
                foreach (var file in filePaths)
                {
                    string fileName = Path.GetFileName(file);

                    string str = destination + "\\" + fileName;
                    if (File.Exists(str))
                    {
                        File.Delete(str);
                    }
                    File.Copy(file, str);
                    Globals.Log.Info("Copied File: " + file + " to " + str);
                }
                //copy preference file
                var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SystemSettings.appDataMainFolder, SystemSettings.appDataProgrammFolder, SystemSettings.appDataPreferenceFolder);
                string preferenceFile = appDataFolder + SystemSettings.appDataPreferenceFileName; //path and filename of the preferences file
                string fileNamePrefFile = Path.GetFileName(preferenceFile);
                string strPrefFile = destination + "\\" + fileNamePrefFile;
                if (File.Exists(strPrefFile))
                {
                    File.Delete(strPrefFile);
                }
                File.Copy(preferenceFile, strPrefFile);
                Globals.Log.Info("Copied File: " + preferenceFile + " to " + strPrefFile);
            }
            _canExport = true;
            _canUpdate = true;
            _canClose = true;
        }
       
        private void OnWebUpdateButtonClicked()
        {
            _canExport = false;
            _canUpdate = false;
            _canClose = false;
            Globals.Log.Info("Start Webupdate.");

            UpdateInformation updateInfo = new UpdateInformation();
            updateInfo.Host = "home16315215.1and1-data.host";
            updateInfo.UserName = "u1878379852";
            updateInfo.Password = "vMahr2gz3dWAaV9ETXm5";
            updateInfo.RemoteDirectory = "";
            updateInfo.RemoteFileName = "update.xml";
            updateInfo.Version = ApplicationAssembly.GetName().Version;
            updateInfo.ApplicationName = ApplicationName;
            updateInfo.ApplicationAssembly = ApplicationAssembly;
            updateInfo.ApplicationId = ApplicationId; 
            WebUpdate webUpdate = new WebUpdate(updateInfo);
            webUpdate.DoUpdate(); ;



            //DialogResult = true;
            _canExport = true;
            _canUpdate = true;
            _canClose = true;
        }

        private void OnCloseButtonClicked()
        {
            DialogResult = true;
        }
        #endregion
    }
}
