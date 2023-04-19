using Accelera.Models;
using CsvHelper;
using MicroMvvm;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Accelera.ViewModels
{
    public class SaveAsViewModel : ObservableObject
    {
      
        #region Private Members   
        /// <summary>
        /// This property is needed to bind the DialogResult of the window. See here for details:
        /// https://stackoverflow.com/questions/501886/how-should-the-viewmodel-close-the-form/3329467#3329467
        /// </summary>
        private bool? _dialogResult;

        private ConfigurationModel _configuration;
        private ObservableCollection<string> _sex;
        private ObservableCollection<string> _handedness;
        private int _selectedSexIdx;
        private int _selectedHandednessIdx;
        private string _nameOfSubject = string.Empty;
        private string _typeOfExperiment = string.Empty;
        private string _placeOfExperiment = string.Empty;
        private string _nameOfExaminer = string.Empty;
        private string _comment = string.Empty;
        private ObservableCollection<string> _typesOfExperiments;
        private ObservableCollection<string> _placesOfExperiments;
        private ObservableCollection<string> _namesOfExaminers;
        private int _selectedTypesOfExperimentsIdx;
        private int _selectedPlacesOfExperimentsIdx;
        private int _selectedNamesOfExaminersIdx;

        private bool _canAddTypeOfExperiment;
        private bool _canRemoveTypeOfExperiment;
        private bool _canAddPlaceOfExperiment;
        private bool _canRemovePlaceOfExperiment;
        private bool _canAddNamesOfExaminer;
        private bool _canRemoveNamesOfExaminer;

        private ProgressDialog _saveProgressDialog = new ProgressDialog()
        {
            WindowTitle = "Save data",
            Text = "Saving aquired data on hard disk ...",
            Description = "Processing...",
            ShowTimeRemaining = true,
            CancellationText = "Saving canceled. Datafile not written completly.",
        };
        private string _fileNameSave = string.Empty;
        private bool _dataSavingFinished;

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

        public List<DataModel> StorageData { get; set; }
        public ConfigurationModel ExperimentConfig { get; set; }
        public ObservableCollection<string> Sex { get => _sex; set => _sex = value; }
        public ObservableCollection<string> Handedness { get => _handedness; set => _handedness = value; }
        public int SelectedSexIdx
        {
            get => _selectedSexIdx;
            set
            {
                _selectedSexIdx = value;
                RaisePropertyChanged("SelectedSexIdx");
            }
        }
        public int SelectedHandednessIdx
        {
            get => _selectedHandednessIdx;
            set
            {
                _selectedHandednessIdx = value;
                RaisePropertyChanged("SelectedHandednessIdx");
            }
        }

        public string NameOfSubject
        {
            get => _nameOfSubject;
            set
            {
                _nameOfSubject = value;
                RaisePropertyChanged("NameOfSubject");
            }
        }

        public string TypeOfExperiment
        {
            get => _typeOfExperiment;
            set
            {
                _typeOfExperiment = value;
                RaisePropertyChanged("TypeOfExperiment");
            }
        }
        public string PlaceOfExperiment
        {
            get => _placeOfExperiment;
            set
            {
                _placeOfExperiment = value;
                RaisePropertyChanged("PlaceOfExperiment");
            }
        }
        public string NameOfExaminer
        {
            get => _nameOfExaminer;
            set
            {
                _nameOfExaminer = value;
                RaisePropertyChanged("NameOfExaminer");
            }
        }
        public string Comment
        {
            get => _comment;
            set
            {
                _comment = value;
                RaisePropertyChanged("Comment");
            }
        }
        public ObservableCollection<string> TypesOfExperiments { get => _typesOfExperiments; set => _typesOfExperiments = value; }
        public ObservableCollection<string> PlacesOfExperiments { get => _placesOfExperiments; set => _placesOfExperiments = value; }
        public ObservableCollection<string> NamesOfExaminers { get => _namesOfExaminers; set => _namesOfExaminers = value; }
        public int SelectedTypesOfExperimentsIdx
        {
            get => _selectedTypesOfExperimentsIdx;
            set
            {
                _selectedTypesOfExperimentsIdx = value;
                RaisePropertyChanged("SelectedTypesOfExperimentsIdx");
            }
        }

        public int SelectedPlacesOfExperimentsIdx
        {
            get => _selectedPlacesOfExperimentsIdx;
            set
            {
                _selectedPlacesOfExperimentsIdx = value;
                RaisePropertyChanged("SelectedPlacesOfExperimentsIdx");
            }
        }
        public int SelectedNamesOfExaminersIdx
        {
            get => _selectedNamesOfExaminersIdx;
            set
            {
                _selectedNamesOfExaminersIdx = value;
                RaisePropertyChanged("SelectedNamesOfExaminersIdx");
            }
        }

        #endregion 

        #region Constructors
        public SaveAsViewModel()
        {
            StorageData = new List<DataModel>();
            ExperimentConfig = new ConfigurationModel();

            _configuration = new ConfigurationModel();
            SystemSettings settings = new SystemSettings();
            _configuration = settings.LoadOrCreate();
            _sex = new ObservableCollection<string>();
            _handedness = new ObservableCollection<string>();   
            _typesOfExperiments = new ObservableCollection<string>(_configuration.TypesOfExperiments);
            _placesOfExperiments = new ObservableCollection<string>(_configuration.PlacesOfExperiments);
            _namesOfExaminers = new ObservableCollection<string>(_configuration.NamesOfExaminers);
            List<string> sex = new List<string>() { "female", "male" }; 
            List<string> handedness = new List<string>() { "right handed", "left handed", "both handed" };
            _sex = new ObservableCollection<string>(sex); 
            _handedness = new ObservableCollection<string>(handedness);
            _selectedSexIdx = 0;
            _selectedHandednessIdx = 0;

            _saveProgressDialog.DoWork += new DoWorkEventHandler(SaveProgressDialogDoWork);
            _saveProgressDialog.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SaveProgressDialogCompleted);

            Globals.Log.Wpf("== SaveAsViewModel ==");
        }
        #endregion

        #region Relay Commands
        public ICommand SaveButtonClicked { get { return new RelayCommand(OnSaveButtonClicked, CanSaveButtonBeExecuted); } }
        public ICommand CancelButtonClicked { get { return new RelayCommand(OnCancelButtonClicked, CanCancelButtonBeExecuted); } }
        public ICommand AddTypeOfExperimentClicked { get { return new RelayCommand(OnAddTypeOfExperimentClicked, CanAddTypeOfExperimentBeExecuted); } }
        public ICommand DeleteTypeOfExperimentClicked { get { return new RelayCommand(OnDeleteTypeOfExperimentClicked, CanDeleteTypeOfExperimentBeExecuted); } }
        public ICommand AddPlaceOfExperimentClicked { get { return new RelayCommand(OnAddPlaceOfExperimentClicked, CanAddPlaceOfExperimentBeExecuted); } }
        public ICommand DeletePlaceOfExperimentClicked { get { return new RelayCommand(OnDeletePlaceOfExperimentClicked, CanDeletePlaceOfExperimentBeExecuted); } }
        public ICommand AddExaminerClicked { get { return new RelayCommand(OnAddExaminerClicked, CanAddExaminerBeExecuted); } }

        public ICommand DeleteExaminerClicked { get { return new RelayCommand(OnDeleteExaminerClicked, CanDeleteExaminerBeExecuted); } }

        #endregion

        #region Enable Control of Buttons

        ///=================================================================================================
        /// <summary>Determine if we can save button be executed.
        ///          The save button can be used at any time.
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 16.04.2023.</remarks>
        ///
        /// <returns>True if we can save button be executed, false if not.</returns>
        ///=================================================================================================

        private bool CanSaveButtonBeExecuted()
        {
            return true;
        }

        ///=================================================================================================
        /// <summary>Determine if we can cancel button be executed.
        ///          The cancel button can be used at any time.
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 16.04.2023.</remarks>
        ///
        /// <returns>True if we can cancel button be executed, false if not.</returns>
        ///=================================================================================================

        private bool CanCancelButtonBeExecuted()
        {
            return true;
        }

        ///=================================================================================================
        /// <summary>Determine if we can add type of experiment.
        ///          A type of Experiment can only be added to the list if the edit box is not empty.
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 16.04.2023.</remarks>
        ///
        /// <returns>True if we can add type of experiment be executed, false if not.</returns>
        ///=================================================================================================

        private bool CanAddTypeOfExperimentBeExecuted()
        {
            bool retval;
            if (_typeOfExperiment == string.Empty)
            {
                retval = false;
            }
            else
            {
                retval = true;
            }
            return retval;
        }
        private bool CanDeleteTypeOfExperimentBeExecuted()
        {
            bool retval;
            if ((_typesOfExperiments.Count == 0) || (_selectedTypesOfExperimentsIdx == -1))
            {
                retval = false;
            }
            else
            {
                retval = true;
            }
            return retval;
        }
        private bool CanAddPlaceOfExperimentBeExecuted()
        {
            bool retval;
            if (_placeOfExperiment == string.Empty)
            {
                retval = false;
            }
            else
            {
                retval = true;
            }
            return retval;
        }
        private bool CanDeletePlaceOfExperimentBeExecuted()
        {
            bool retval;
            if ((_placesOfExperiments.Count == 0) || (_selectedPlacesOfExperimentsIdx == -1))
            {
                retval = false;
            }
            else
            {
                retval = true;
            }
            return retval;
        }
        private bool CanAddExaminerBeExecuted()
        {
            bool retval;
            if (_nameOfExaminer == string.Empty)
            {
                retval = false;
            }
            else
            {
                retval = true;
            }
            return retval;
        }
        private bool CanDeleteExaminerBeExecuted()
        {
            bool retval;
            if ((_namesOfExaminers.Count == 0) || (_selectedNamesOfExaminersIdx == -1))
            {
                retval = false;
            }
            else
            {
                retval = true;
            }
            return retval;
        }
        #endregion

        #region Background Worker Tasks
        private void SaveProgressDialogDoWork(object sender, DoWorkEventArgs e)
        {
            double progress = 0;
            int percent = 0;
            int previous = 0;

            using (var writer = new StreamWriter(_fileNameSave))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<DataModel>();
                csv.NextRecord();
                int cnt = 0;
                foreach (var item in StorageData)
                {
                    if (_saveProgressDialog.CancellationPending == true)
                    {
                        e.Cancel = true;
                        return;
                    }
                    else
                    {
                        csv.WriteRecord(item);
                        csv.NextRecord();
                        cnt++;
                        progress = cnt / StorageData.Count;

                        percent = (int)(progress * 100.0);
                        if (percent > previous)
                        {
                            // slow down the report progress to see animation bar
                            _saveProgressDialog.ReportProgress(percent, null, string.Format(System.Globalization.CultureInfo.CurrentCulture, "Processing: {0}%", percent));
                            previous = percent;
                        }
                    }
                }
                _dataSavingFinished = true;
                e.Result = _dataSavingFinished;
            }
        }
        private void SaveProgressDialogCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                Globals.Log.Info("Saving data file cancelled.");
                MessageBox.Show("Saving of data file was cancelled. Data might be corrupt or not complete.", "Information");
            }
            else if ((bool)e.Result == _dataSavingFinished)
            {
                //save summary file
                _configuration.TotalNumberOfAquiredBlocks = ExperimentConfig.TotalNumberOfAquiredBlocks;
                _configuration.TotalNumberOfAquiredDataFrames = StorageData.Count();
                _configuration.TotalNumberOfAquiredEvents = StorageData[StorageData.Count() - 1].EventId + 1;
                _configuration.SetValueSamplesPerDataFrame = ExperimentConfig.NumberOfSamplesPerAcousticStimulus;

                var infoFile = Path.ChangeExtension(_fileNameSave, ".info");
                _configuration.SaveAsFile(infoFile);

                //save timestep file
                var timestepFile = Path.ChangeExtension(_fileNameSave, ".time");
                _configuration.SaveTimeSteps(timestepFile);

                Globals.Log.Info("Saving data file finished.");
                MessageBox.Show("Saving of data file finished.", "Information");

                DialogResult = true;
            }

        }
        #endregion

        #region Button Clicked Methods

        private void OnSaveButtonClicked()
        {
            Globals.Log.Info("Save button clicked.");
            SaveFileDialog sdialog = new SaveFileDialog();


            _configuration.NameOfSubject = _nameOfSubject;
            _configuration.SexOfSubject = _sex[_selectedSexIdx];
            _configuration.HandednessOfSubject = _handedness[_selectedHandednessIdx];
            _configuration.TypeOfExperiment = _typesOfExperiments[_selectedTypesOfExperimentsIdx];
            _configuration.PlaceOfExperiment = _placesOfExperiments[_selectedPlacesOfExperimentsIdx];
            _configuration.NameOfExaminer = _namesOfExaminers[_selectedNamesOfExaminersIdx];
            _configuration.Comments = _comment;

            // safe preference file
            SystemSettings settings = new SystemSettings();
            settings.SaveOrCreate(_configuration);


            sdialog.Filter = "CSV File (*.csv)|*.csv";
            if (sdialog.ShowDialog() == true)
            {
                _fileNameSave = sdialog.FileName;
                _saveProgressDialog.Show();                
            }
        }

        private void OnCancelButtonClicked()
        {
            Globals.Log.Info("Save As Dialog Cancled");
            DialogResult = false;
        }

        private void OnAddTypeOfExperimentClicked()
        {
            _configuration.TypesOfExperiments.Add(_typeOfExperiment);
            _typesOfExperiments.Add(_typeOfExperiment);
            _selectedTypesOfExperimentsIdx = _typesOfExperiments.IndexOf(_typeOfExperiment);
            SelectedTypesOfExperimentsIdx = _selectedTypesOfExperimentsIdx;
        }

        private void OnDeleteTypeOfExperimentClicked()
        {
            int del = _selectedTypesOfExperimentsIdx;
            _configuration.TypesOfExperiments.RemoveAt(del);
            _typesOfExperiments.RemoveAt(del);
            _selectedTypesOfExperimentsIdx = -1;
            SelectedTypesOfExperimentsIdx = _selectedTypesOfExperimentsIdx;
        }

        private void OnAddPlaceOfExperimentClicked()
        {
            _configuration.PlacesOfExperiments.Add(_placeOfExperiment);
            _placesOfExperiments.Add(_placeOfExperiment);
            _selectedPlacesOfExperimentsIdx = _placesOfExperiments.IndexOf(_placeOfExperiment);
            SelectedPlacesOfExperimentsIdx = _selectedPlacesOfExperimentsIdx;
        }

        private void OnDeletePlaceOfExperimentClicked()
        {
            int del = _selectedPlacesOfExperimentsIdx;
            _configuration.PlacesOfExperiments.RemoveAt(del);
            _placesOfExperiments.RemoveAt(del);
            _selectedPlacesOfExperimentsIdx = -1;
            SelectedPlacesOfExperimentsIdx = _selectedPlacesOfExperimentsIdx;
        }

        private void OnAddExaminerClicked()
        {
            _configuration.NamesOfExaminers.Add(_nameOfExaminer);
            _namesOfExaminers.Add(_nameOfExaminer);
            _selectedNamesOfExaminersIdx = _namesOfExaminers.IndexOf(_nameOfExaminer);
            SelectedNamesOfExaminersIdx = _selectedNamesOfExaminersIdx;
        }
        
        private void OnDeleteExaminerClicked()
        {
           int del = _selectedNamesOfExaminersIdx;
            _configuration.NamesOfExaminers.RemoveAt(del);
            _namesOfExaminers.RemoveAt(del);
            _selectedNamesOfExaminersIdx = -1;
            SelectedNamesOfExaminersIdx = _selectedNamesOfExaminersIdx;
        }
        #endregion
    }
}
