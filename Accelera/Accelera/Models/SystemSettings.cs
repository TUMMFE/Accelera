using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Windows.Devices.Bluetooth.Advertisement;
using System.Configuration;

namespace Accelera.Models
{
    [Serializable]
    public class SystemSettings
    {
        public const string appDataMainFolder = "MIBE";
        public const string appDataProgrammFolder = "Accelera";
        public const string appDataPreferenceFolder = "Prefs";
        public const string appDataPreferenceFileName = "\\accelera.pref";

        private string _appDataFolder;
        private string _systemSettingsFileName;
        private ConfigurationModel _settings;


        public string ApplicationName { get; } = "Accelera";
        public string ApplicationId { get; } = "Accelera";
        public Assembly ApplicationAssembly { get; } = Assembly.GetExecutingAssembly();

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }
        public string AssemblyVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }
        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }
        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }
        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        public string AppDataFolder
        {
            get { return _appDataFolder; }
            set { _appDataFolder = value; }
        }
        public string SystemSettingsFileName
        {
            get { return _systemSettingsFileName; }
            set { _systemSettingsFileName = value; }
        }
        public ConfigurationModel Settings
        {
            get { return _settings; }
            set { _settings = value; }
        }
        #endregion

        #region Constructors
        public SystemSettings(string appDataFolder, string systemSettingsFileName, ConfigurationModel settings)
        {
            _appDataFolder = appDataFolder;
            _systemSettingsFileName = systemSettingsFileName;
            _settings = settings;
        }

        public SystemSettings(ConfigurationModel settings)
        {
            _appDataFolder = settings.AppDataFolder;
            _systemSettingsFileName = settings.PreferencesFileName;
            _settings = settings;
        }

        public SystemSettings()
        {

        }
        #endregion

        #region Private Methods
        private void Load()
        {
            string fullFileName = Path.Combine(_appDataFolder, path2: Path.GetFileName(_systemSettingsFileName));
            _settings = Serializing.ReadFromXmlFile<ConfigurationModel>(fullFileName);
        }

        private void Load(string appDataFolder, string systemSettingsFileName)
        {
            string fullFileName = Path.Combine(appDataFolder, path2: Path.GetFileName(systemSettingsFileName));
            _settings = Serializing.ReadFromXmlFile<ConfigurationModel>(fullFileName);
        }

        private ConfigurationModel LoadNewData(string appDataFolder, string systemSettingsFileName)
        {
            ConfigurationModel retVal = new ConfigurationModel(false);
            string fullFileName = Path.Combine(appDataFolder, Path.GetFileName(systemSettingsFileName));
            retVal = Serializing.ReadFromXmlFile<ConfigurationModel>(fullFileName);
            retVal.AppDataFolder = appDataFolder;
            retVal.PreferencesFileName = systemSettingsFileName;
            return retVal;
        }
        private void Save()
        {
            string fullFileName = Path.Combine(_appDataFolder, Path.GetFileName(_systemSettingsFileName));
            Serializing.WriteToXmlFile(fullFileName, _settings);
        }

        private void Save(ConfigurationModel settings)
        {
            string fullFileName = Path.Combine(settings.AppDataFolder, path2: Path.GetFileName(settings.PreferencesFileName));
            Serializing.WriteToXmlFile(fullFileName, settings);
        }

        private void Reset()
        {
            ConfigurationModel newSettings = new ConfigurationModel(true);
            newSettings.AppDataFolder = _appDataFolder;
            newSettings.PreferencesFileName = _systemSettingsFileName;
            _settings = newSettings;
        }
        #endregion

        #region Public Methods

        ///=================================================================================================
        /// <summary>Loads or create.
        ///          Load the settings from the settings file. If this file does not exist,
        ///          than generate a standard setting file.
        ///          If the folder does not exist, the method will try to create this folder.
        ///          If the folder could not be created, the application will be terminated.
        /// </summary>
        ///
        /// <remarks>Bernhard Gleich, 16.04.2023.</remarks>
        ///
        /// <returns>The system settings/configuration model</returns>
        ///=================================================================================================
        public ConfigurationModel LoadOrCreate()
        {
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SystemSettings.appDataMainFolder, SystemSettings.appDataProgrammFolder, SystemSettings.appDataPreferenceFolder);
            string preferenceFile = appDataFolder + SystemSettings.appDataPreferenceFileName; //path and filename of the preferences file
            ConfigurationModel retVal = new ConfigurationModel();

            if (!Directory.Exists(appDataFolder))
            {
                Globals.Log.Info("APP DATA FOLDER does not exist.");
                try
                {
                    Directory.CreateDirectory(appDataFolder);
                    Globals.Log.Info("APP DATA FOLDER created.");
                }
                catch (Exception)
                {

                    System.Windows.MessageBox.Show("Application data folder does not exist and could not be created. Application terminated.", "Error");
                    Globals.Log.Fatal("Cannot create APP DATA FOLDER.");
                    Environment.Exit(0); // this command kills the application
                    return null;                  
                }
            }

            if (!File.Exists(preferenceFile))
            {
                Globals.Log.Info("Prefrence file does not exist.");
                retVal = new ConfigurationModel(true);
                retVal.AppDataFolder = appDataFolder;
                retVal.PreferencesFileName = ConfigurationManager.AppSettings["FileNameOfPreferenceFile"];                
                SystemSettings settings = new SystemSettings(retVal);
                settings.Save();
                Globals.Log.Info("Standard preference file generated and saved.");

            }
            else
            {
                SystemSettings settings = new SystemSettings();
                settings.AppDataFolder = appDataFolder;
                settings.SystemSettingsFileName = ConfigurationManager.AppSettings["FileNameOfPreferenceFile"];
                settings.Load();
                Globals.Log.Info("Preference file loaded.");
                retVal = settings.Settings;
            }
            return retVal;
        }

        ///=================================================================================================
        /// <summary>Saves an or create.
        ///          Save the settings file. If this file does not exist,
        ///          than generate a standard setting file.
        ///          If the folder does not exist, the method will try to create this folder.
        ///          If the folder could not be created, the application will be terminated.
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 16.04.2023.</remarks>
        ///
        /// <param name="configuration">The configuration.</param>
        ///=================================================================================================

        public void SaveOrCreate(ConfigurationModel configuration)
        {
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SystemSettings.appDataMainFolder, SystemSettings.appDataProgrammFolder, SystemSettings.appDataPreferenceFolder);
            string preferenceFile = appDataFolder + SystemSettings.appDataPreferenceFileName; //path and filename of the preferences file
            if (!Directory.Exists(appDataFolder))
            {
                Globals.Log.Info("APP DATA FOLDER does not exist.");
                try
                {
                    Directory.CreateDirectory(appDataFolder);
                }
                catch (Exception)
                {

                    System.Windows.MessageBox.Show("Application data folder does not exist and could not be created. Application terminated.", "Error");
                    Globals.Log.Fatal("Cannot create APP DATA FOLDER.");
                    Environment.Exit(0); // this command kills the application        
                }
            }
            try
            {
                ConfigurationModel preferences = new ConfigurationModel(true);
                preferences.AppDataFolder = appDataFolder;
                preferences.PreferencesFileName = ConfigurationManager.AppSettings["FileNameOfPreferenceFile"];
                configuration.AppDataFolder = preferences.AppDataFolder;
                configuration.PreferencesFileName = preferences.PreferencesFileName;
                SystemSettings settings = new SystemSettings(configuration);
                settings.Save();
                Globals.Log.Info("Preference file generated and saved.");
            }
            catch (Exception)
            {

                System.Windows.MessageBox.Show("Application data cannot be written. Application terminated.", "Error");
                Globals.Log.Fatal("Application data cannot be written.");
                Environment.Exit(0); // this command kills the application        
            }

        }
        #endregion
    }
}
