using Accelera.Models;
using MicroMvvm;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Accelera.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MFE.Communication;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MathNet.Filtering.Median;
using System.ComponentModel;
using System.Windows;
using System.Configuration;
using Accelera.Properties;

namespace Accelera.ViewModels
{
    /// <summary>
    /// https://www.mobilemotion.eu/?p=1872
    /// </summary>
    public class ConfigurationDialogViewModel : ObservableObject
    {
        #region Constants
        public const int InvalidatePlotEveryNthValue = 200;
        public const int MaximumRollover = 1000;
        public const int DecimationFactor = 5;
        public const int MinimumDataSizePrioDownsampling = 100;
        public const int MedianWindowSize = 9;
        #endregion

        #region Private Members   
        /// <summary>
        /// This property is needed to bind the DialogResult of the window. See here for details:
        /// https://stackoverflow.com/questions/501886/how-should-the-viewmodel-close-the-form/3329467#3329467
        /// </summary>
        private bool? _dialogResult;
        private BufferBlock<DataModel> _dataBufferBlock;    //used for producer/consumer model
        private List<DataModel> _storageData;
        private CancellationTokenSource _ts = new CancellationTokenSource();
        private CancellationToken _ct;
        private hw _hw;
        private Vcp _usedDeviceVcp;
        private double _dataRateForOffset;

        private ConfigurationModel _configuration;
        private ObservableCollection<string> _ranges;
        private ObservableCollection<string> _outputDataRates;
        private ObservableCollection<string> _highPassFilters;
        private ObservableCollection<string> _triggerTypes;

        private bool _isRunning = false;
        private bool _isProcessing = false;
        private bool _isConfigTransmissionFinished = true;

        private int _selectedRangeIdx = -1;
        private int _selectedOutputDataRateIdx = -1;
        private int _selectedHighPassFilterIdx = -1;
        private int _selectedTriggerTypeIdx = -1;
        private int _currentProgress;
        private int _progressBarMin;
        private int _progressBarMax;

        private bool _isActivityCountingSelected = false;
        private bool _isBeepOnTriggerSelected = false;
        private int _activityCountNumber = 0;
        private int _activityThreshold = 0;
        private int _sampleNumberExternalTrigger = 0;
        private int _sampleNumberAcousticStimulation = 0;
        private int _blockDuration = 0;
        private int _stimulusRate = 0;
        private int _xOffset = 0;
        private int _yOffset = 0;
        private int _zOffset = 0;
        private int _beepDuration = 0;

        private double _outputDataRate;
        private byte _highPassFilterFrequency;
        private double _range;
        private byte _triggerPolarity;

        #endregion

        #region Public Members 
        public ConfigurationModel Configuration => _configuration;
        public PlotModel xOffsetModel { get; set; }
        public PlotModel yOffsetModel { get; set; }
        public PlotModel zOffsetModel { get; set; }
        public string ConnectionString { get; set; }
        public ConfigurationModel Preferences { get; set; } = new ConfigurationModel();

        public int SelectedRangeIdx { 
            get => _selectedRangeIdx; 
            set 
            {
                _selectedRangeIdx = value;
                _range = _hw.ExtractDecimalFromString(_hw.PossibleRanges[_selectedRangeIdx]);
                RaisePropertyChanged("SelectedRangeIdx");
            } 
        }
        public int SelectedOutputDataRateIdx { 
            get => _selectedOutputDataRateIdx; 
            set
            {
                _selectedOutputDataRateIdx = value;
                _outputDataRate = _hw.ExtractDecimalFromString(_hw.PossibleOutputDataRates[_selectedOutputDataRateIdx]);
                RaisePropertyChanged("SelectedOutputDataRateIdx");
            }
        }
        public int SelectedHighPassFilterIdx { 
            get => _selectedHighPassFilterIdx; 
            set
            {
                _selectedHighPassFilterIdx = value;
                _highPassFilterFrequency = (byte)_selectedHighPassFilterIdx;
                RaisePropertyChanged("SelectedHighPassFilterIdx");
            }
        }
        public int SelectedTriggerTypeIdx { 
            get => _selectedTriggerTypeIdx; 
            set
            {
                _selectedTriggerTypeIdx = value;
                _triggerPolarity = (byte)_selectedTriggerTypeIdx;
                RaisePropertyChanged("SelectedTriggerTypeIdx");
            }
        }
        public int CurrentProgress
        {
            get => _currentProgress;
            set
            {
                _currentProgress = value;
                RaisePropertyChanged("CurrentProgress");
            }
        }
        public int ProgressBarMin
        {
            get => _progressBarMin;
            set
            {
                _progressBarMin = value;
                RaisePropertyChanged("ProgressBarMin");
            }
        }
        public int ProgressBarMax
        {
            get => _progressBarMax;
            set
            {
                _progressBarMax = value;
                RaisePropertyChanged("ProgressBarMax");
            }
        }
        public ObservableCollection<string> Ranges { get => _ranges; set => _ranges = value; }
        public ObservableCollection<string> OutputDataRates { get => _outputDataRates; set => _outputDataRates = value; }
        public ObservableCollection<string> HighPassFilters { get => _highPassFilters; set => _highPassFilters = value; }
        public ObservableCollection<string> TriggerTypes { get => _triggerTypes; set => _triggerTypes = value; }
        public bool IsActivityCountingSelected {
            get => _isActivityCountingSelected;
            set
            {
                _isActivityCountingSelected = value;
                RaisePropertyChanged("IsActivityCountingSelected");
            }
        }
        public bool IsBeepOnTriggerSelected {
            get => _isBeepOnTriggerSelected;
            set
            {
                _isBeepOnTriggerSelected = value;
                RaisePropertyChanged("IsBeepOnTriggerSelected");
            }
        }
        public int ActivityCountNumber { 
            get => _activityCountNumber;
            set
            {
                _activityCountNumber = value;
                RaisePropertyChanged("ActivityCountNumber");
            }
        }
        public int ActivityThreshold
        {
            get => _activityThreshold;
            set
            {
                _activityThreshold = value;
                RaisePropertyChanged("ActivityThreshold");
            }
        }
        public int SampleNumberExternalTrigger {
            get => _sampleNumberExternalTrigger;
            set
            {
                _sampleNumberExternalTrigger = value;
                RaisePropertyChanged("SampleNumberExternalTrigger");
            }
        }
        public int SampleNumberAcousticStimulation {
            get => _sampleNumberAcousticStimulation;
            set
            {
                _sampleNumberAcousticStimulation = value;
                RaisePropertyChanged("SampleNumberAcousticStimulation");
            }
        }
        public int BlockDuration {
            get => _blockDuration;
            set
            {
                _blockDuration = value;
                RaisePropertyChanged("BlockDuration");
            }
         }
        public int StimulusRate {
            get => _stimulusRate;
            set
            {
                _stimulusRate = value;
                RaisePropertyChanged("StimulusRate");
            }
        }
        public int XOffset {
            get => _xOffset;
            set
            {
                _xOffset = value;
                RaisePropertyChanged("XOffset");
            }
        }
        public int YOffset
        {
            get => _yOffset;
            set
            {
                _yOffset = value;
                RaisePropertyChanged("YOffset");
            }
        }
        public int ZOffset
        {
            get => _zOffset;
            set
            {
                _zOffset = value;
                RaisePropertyChanged("ZOffset");
            }
        }
        public int BeepDuration {
            get => _beepDuration;
            set
            {
                _beepDuration = value;
                RaisePropertyChanged("BeepDuration");
            }
        }
        public bool? DialogResult
        {
            get => _dialogResult;
            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }
        #endregion

        #region Constructors
        public ConfigurationDialogViewModel()
        {
        
            xOffsetModel = new PlotModel();
            yOffsetModel = new PlotModel();
            zOffsetModel = new PlotModel();
            _dataBufferBlock = new BufferBlock<DataModel>();
            _storageData = new List<DataModel>();

            _configuration = new ConfigurationModel();
            _hw = new hw();
            _usedDeviceVcp = new Vcp();

            _ranges = new ObservableCollection<string>();
            _outputDataRates = new ObservableCollection<string>();
            _highPassFilters = new ObservableCollection<string>();
            _triggerTypes = new ObservableCollection<string>();

            _ct = _ts.Token;

            xOffsetModel.Series.Add(new LineSeries() { Color = OxyColors.DodgerBlue, InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline });
            PlotAxisFormatting(xOffsetModel, "X", "time in s", "acceleration in LSB");

            yOffsetModel.Series.Add(new LineSeries() { Color = OxyColors.DarkGreen, InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline });
            PlotAxisFormatting(yOffsetModel, "Y", "time in s", "acceleration in LSB");

            zOffsetModel.Series.Add(new LineSeries() { Color = OxyColors.DarkOrange, InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline });
            PlotAxisFormatting(zOffsetModel, "Z", "time in s", "acceleration in LSB");

            _outputDataRates = _hw.PossibleOutputDataRates;
            _highPassFilters = _hw.PossibleHighPassFilterFrequencies;
            _triggerTypes = _hw.PossibleTriggerType;
            _ranges = _hw.PossibleRanges;

            _currentProgress = 0;
            CurrentProgress = 0;
            _progressBarMin = 0;
            _progressBarMax = 100;

            Globals.Log.Info("== ConfigurationDialogViewModel ==");

            //here we shall load some standard settings from the disk   
            SystemSettings systemSettings = new SystemSettings();
            _configuration = systemSettings.LoadOrCreate();
            _dataRateForOffset = _configuration.OutputDataRate;
            UpdateDataContext();
        }
        #endregion

        #region Member Methods
        /// <summary>
        /// Draw, axis, grid lines, labels and titels of a plot model.
        /// </summary>
        /// <param name="pm">Plotmodel to be modified</param>
        /// <param name="title">Title of the plot</param>
        /// <param name="xaxis">Label for the x-axis</param>
        /// <param name="yaxis">Label for the y-axis</param>
        private void PlotAxisFormatting(PlotModel pm, string title, string xaxis, string yaxis)
        {
            pm.Title = title;

            var xvalueAxis = new LinearAxis();
            xvalueAxis.Position = AxisPosition.Bottom;
            xvalueAxis.MajorGridlineStyle = LineStyle.Solid;
            xvalueAxis.Title = xaxis;
            pm.Axes.Add(xvalueAxis);

            var valueAxis = new LinearAxis();
            valueAxis.Position = AxisPosition.Left;
            valueAxis.MajorGridlineStyle = LineStyle.Solid;
            valueAxis.Title = yaxis;
            pm.Axes.Add(valueAxis);
        }
        /// <summary>
        /// Prepare the plot for data aquisistion. Clears all data points and set all colors. 
        /// For performance issues, the data is ploted as is without cubic spline interpolation.
        /// </summary>
        /// <param name="pm">Plot Model</param>
        /// <param name="xaxis">Label for the x-axis</param>
        /// <param name="yaxis">Label for the y-axis</param>
        private void PreparePlotForDaq(PlotModel pm, OxyColor color, string xaxis, string yaxis)
        {
            (pm.Series[0] as LineSeries).Points.Clear();
            pm.InvalidatePlot(true);
            pm.Series.Clear();
            pm.Series.Add(new LineSeries() { Color = color });

            pm.Axes[0].Title = xaxis;
            pm.Axes[1].Title = yaxis;
        }
        /// <summary>
        /// Use the list of data tuples and perform interpolation and decimation. Add 
        /// the filtered data tuples to the plot series. Afterwards redraw the plot.
        /// </summary>
        /// <param name="pm">The plotmodel</param>
        /// <param name="dataTuples">list of data tuples</param>
        private void DrawPoints(PlotModel pm, List<Tuple<double, double>> dataTuples)
        {
            int Threshold = MinimumDataSizePrioDownsampling / DecimationFactor;

            IEnumerable<Tuple<double, double>> interpolatedData = LargestTriangleThreeBuckets(dataTuples, Threshold);
            foreach (Tuple<double, double> item in interpolatedData)
            {
                (pm.Series[0] as LineSeries).Points.Add(new DataPoint(item.Item1, item.Item2));
            }

            if ((pm.Series[0] as LineSeries).Points.Count > MaximumRollover) //show only 100 last points
            {
                (pm.Series[0] as LineSeries).Points.RemoveRange(0, interpolatedData.Count()); //remove first point
            }
            pm.InvalidatePlot(true);
        }
        /// <summary>
        /// Producer thread to collect the received and converted raw data.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="e"></param>
        private void Produce(ITargetBlock<DataModel> target, DataModel data)
        {
            target.Post(data);
        }       
        /// <summary>
        /// Prepare the plot paneel and send the start command to the device.
        /// </summary>
        private void PrepareOffsetCalibrationDataAquisition()
        {
            _ts = new CancellationTokenSource();
            _ct = _ts.Token;
            _usedDeviceVcp.Rxbuffer.Complete();
            _usedDeviceVcp.Restart();

            PreparePlotForDaq(xOffsetModel, OxyColor.FromRgb(43, 138, 128), "time in s", "acceleration in LSB");
            PreparePlotForDaq(yOffsetModel, OxyColor.FromRgb(100, 20, 120), "time in s", "acceleration in LSB");
            PreparePlotForDaq(zOffsetModel, OxyColor.FromRgb(191, 115, 28), "time in s", "acceleration in LSB");
            
            _usedDeviceVcp.Port.StartListening();
            
            //DAQ in free running mode 0x03 and start it 0x00
            _usedDeviceVcp.Port.Write(_hw.SetOpMode(0x03, 0x00), 0, hw.TxProtocolLength);
        }
        
        /// <summary>
        /// Update the UI with the values from the _data
        /// </summary>
        private void UpdateDataContext()
        {
            //update UI from _data
            _outputDataRate = _configuration.OutputDataRate;
            for (int i = 0; i < _outputDataRates.Count; i++)
            {
                if ((int)_hw.ExtractDecimalFromString(_outputDataRates[i]) == (int)_outputDataRate)
                {
                    _selectedOutputDataRateIdx = i;
                }
            }
            _range = _configuration.Range;

            for (int i = 0; i < _ranges.Count; i++)
            {
                if ((int)_hw.ExtractDecimalFromString(_ranges[i]) == _range)
                {
                    _selectedRangeIdx = i;
                }
            }
            _selectedHighPassFilterIdx = _configuration.HighpassFilterFrequency;
            _selectedTriggerTypeIdx = _configuration.TriggerPolarity;
            _beepDuration = _configuration.DurationOfAcousticStimulusInMilliSeconds;            
            _activityCountNumber = _configuration.ActivityCounts;
            
            if (_activityCountNumber == 0)
            {
                _isActivityCountingSelected = false;
            } else
            {
                _isActivityCountingSelected = true;
            }
            _activityThreshold = _configuration.ActivityThreshold;
            _sampleNumberExternalTrigger = _configuration.NumberOfSamplesPerTriggerEvent;
            _sampleNumberAcousticStimulation = _configuration.NumberOfSamplesPerAcousticStimulus;
            _isBeepOnTriggerSelected = _configuration.BeepOnExternalTrigger;
            _blockDuration = _configuration.DurationOfEventInSeconds;
            _stimulusRate = _configuration.FrequencyOfAcousticStimulusInMillihertz;
            _xOffset = _configuration.XOffset;
            _yOffset = _configuration.YOffset;
            _zOffset = _configuration.ZOffset;
        }
        /// <summary>
        /// Save the _data to the preference file
        /// </summary>
        private void SaveDataContext()
        {
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SystemSettings.appDataMainFolder, SystemSettings.appDataProgrammFolder, SystemSettings.appDataPreferenceFolder);
            string preferenceFile = appDataFolder + SystemSettings.appDataPreferenceFileName; //path and filename of the preferences file

            _configuration.OutputDataRate = _outputDataRate;
            _configuration.Range = _range;
            _configuration.HighpassFilterFrequency = (byte)_selectedHighPassFilterIdx;
            _configuration.TriggerPolarity = (byte)_selectedTriggerTypeIdx;
            _configuration.DurationOfAcousticStimulusInMilliSeconds = _beepDuration;
            _configuration.ActivityCounts = (byte)_activityCountNumber;  
            _configuration.ActivityThreshold = _activityThreshold;
            _configuration.NumberOfSamplesPerTriggerEvent = _sampleNumberExternalTrigger;
            _configuration.NumberOfSamplesPerAcousticStimulus = _sampleNumberAcousticStimulation;
            _configuration.BeepOnExternalTrigger = _isBeepOnTriggerSelected;
            _configuration.DurationOfEventInSeconds = _blockDuration;
            _configuration.FrequencyOfAcousticStimulusInMillihertz = _stimulusRate;
            _configuration.XOffset = _xOffset;
            _configuration.YOffset = _yOffset;
            _configuration.ZOffset = _zOffset;

            // safe preference file
            SystemSettings settings = new SystemSettings();
            settings.SaveOrCreate(_configuration);
        }        
        #endregion

        #region Commands
        public ICommand StartOffsetCalibrationClicked { get { return new RelayCommand(OnStartOffsetCalibrationButtonClicked, CanStartOffsetCalibrationButtonBeExecuted); } }
        public ICommand StopOffsetCalibrationClicked { get { return new RelayCommand(OnStopOffsetCalibrationButtonClicked, CanStopOffsetCalibrationButtonBeExecuted); } }
        public ICommand WriteToHardwareClicked { get { return new RelayCommand(OnWriteToHardwareButtonClicked, CanWriteToHardwareButtonBeExecuted); } }
        public ICommand CancelButtonClicked { get { return new RelayCommand(OnCancelButtonClicked, CanCancelButtonBeExecuted); } }
      
        /// <summary>
        /// Offset calibration can only be started when the system is not running and when 
        /// no data is processing at the moment.
        /// </summary>
        /// <returns></returns>
        bool CanStartOffsetCalibrationButtonBeExecuted()
        {
            return (!_isRunning && !_isProcessing && _isConfigTransmissionFinished);
        }
        /// <summary>
        /// Calibration can only be stopped if the system is running
        /// </summary>
        /// <returns></returns>
        bool CanStopOffsetCalibrationButtonBeExecuted()
        {
            return (_isRunning && _isConfigTransmissionFinished);
        }
        /// <summary>
        /// Hardware re-configuration can only be done when the system is not running and when 
        /// no data is processing at the moment.
        /// </summary>
        /// <returns></returns>
        bool CanWriteToHardwareButtonBeExecuted()
        {
            return (!_isRunning && !_isProcessing && _isConfigTransmissionFinished);
        }
        /// <summary>
        /// The re-configuration dialog can only be canceled if the system is not running. In 
        /// case the system is running the user must stop the calibration first.
        /// </summary>
        /// <returns></returns>
        bool CanCancelButtonBeExecuted()
        {
            return (!_isRunning && _isConfigTransmissionFinished);
        }
        /// <summary>
        /// By clicking on the start button the system will start the data aquisition and collect
        /// acceleration data from the device. Data aquisition will be stopped by the clicking the
        /// stop button. Collected data will be shown on the plot paneel in real time. This method 
        /// will not change the hardware settings in advance. It will use the standard settings.
        /// </summary>
        private void OnStartOffsetCalibrationButtonClicked()
        {
            //reset progress bar
            ProgressBarMin = 0;
            CurrentProgress = ProgressBarMin;

            if (_usedDeviceVcp.IsOpen == false)
            {
                _usedDeviceVcp.Open(ConnectionString, 921600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);                
            }
            
            PrepareOffsetCalibrationDataAquisition();   //prepare plots and start the data aquisistion
            _isRunning = true;                          //set control flag for UI binding 
            _storageData.Clear();                       //clear the storage list
            DataCollectionTask();                       //start data collection task
            SignalProcessingTask();                     //start signal processing
        }
        /// <summary>
        /// When the stop button was clicked, the stop command will be sent to the device.
        /// Afterwards the data collection and signal processing tasks will be canceled. 
        /// For data processing the method starts a background worker thread which performs 
        /// filtering (median filter) as well as the calculation of the average offset. These
        /// values will be written into the textboxes on the view (only the lower 16-bit values).
        /// </summary>
        private void OnStopOffsetCalibrationButtonClicked()
        {
            BackgroundWorker worker = new BackgroundWorker();
            //DAQ in free running mode 0x03 and stop it 0x01
            _usedDeviceVcp.Port.Write(_hw.SetOpMode(0x03, 0x01), 0, hw.TxProtocolLength);
            _usedDeviceVcp.Port.CloseIt();
            _ts.Cancel();
            _ts.Dispose();
            _usedDeviceVcp.Port.DiscardInBuffer();
            _usedDeviceVcp.Rxbuffer.Complete();

            if ((_storageData.Count != 0) && (_isProcessing == false))
            {
                worker.WorkerSupportsCancellation = true;
                // Configure the function that will run when started
                worker.DoWork += new DoWorkEventHandler(DataProcessingBackgroundWorker);
                // Configure the function to run when completed
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DataProcessingBackgroundWorkerCompleted);
                _isProcessing = true;
                // Launch the worker
                worker.RunWorkerAsync();
            }
            else
            {
                if (worker.IsBusy == true) 
                {
                    worker.CancelAsync();
                    _isProcessing = false;
                }                
            }
            _isRunning = false;
            //reset progress bar
            ProgressBarMin = 0;
            CurrentProgress = ProgressBarMin;
        }
        /// <summary>
        /// When the write to hardware button was clicked, the new configuration data will
        /// be transmitted to the device and the settings will be saved on harddisk as new 
        /// standard settings.
        /// </summary>
        private void OnWriteToHardwareButtonClicked()
        {
            CurrentProgress = 0;
            ProgressBarMin = 0;
            ProgressBarMax = 100;
            CurrentProgress = ProgressBarMin;
            if (_isConfigTransmissionFinished == true)
            {
                _isConfigTransmissionFinished = false;

                BackgroundWorker worker = new BackgroundWorker();
                // Configure the function that will run when started
                worker.DoWork += new DoWorkEventHandler(ConfigurationTransmissionBackgroundWorker);

                // Configure the function to run when completed
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ConfigurationTransmissionBackgroundWorkerCompleted);

                
                // Launch the worker
                worker.RunWorkerAsync();
                
            }
            Globals.Log.Info("Configuration Dialog Closed. Data was written to the device.");
        }            
        /// <summary>
        /// Cancel button was clicked. The configuration object _data is set to null, 
        /// the window will be closed without writing any data to the device. Thread 
        /// will be set to sleep for 300 ms due to some issues when disposing a COM
        /// port object.
        /// </summary>
        private void OnCancelButtonClicked()
        {
            _configuration = null;
            try {
                _usedDeviceVcp.Port.CloseIt();
                _usedDeviceVcp.Port.DiscardInBuffer();
                _usedDeviceVcp.Rxbuffer.Complete();
                _usedDeviceVcp.Close();
                _usedDeviceVcp.Port.Dispose();
                Thread.Sleep(300);
            } catch { }
            Globals.Log.Info("Configuration Dialog Cancled");
            DialogResult = false;
        }
        #endregion

        #region Data Collection and Signal Processing       
        /// <summary>
        /// Task which collects the data from the COM port. In this method the 
        /// incoming data stream will be analized. The length of one data frame is 
        /// defined in the "hw" class. Each data frame will be analyzed and the data 
        /// will be converted from raw data to data values in SI units.
        /// Each data frame will be stored using a producer/consumer model. The producer
        /// is the DataCollectionTask and the consumer will be the signal processing task.
        /// </summary>
        /// <returns></returns>
        private async Task DataCollectionTask()
        {
            List<Byte> dataStream = new List<Byte>();
            DataModel dataModel;
            double timestep = 0.0;
            
            while (await _usedDeviceVcp.Rxbuffer.OutputAvailableAsync(_ct))
            {
                byte[] data = await _usedDeviceVcp.Rxbuffer.ReceiveAsync(_ct);

                dataStream.AddRange(data);
                if (dataStream.Count > hw.RxProtocolLength)
                {
                    uint val;

                    //convert data frame information
                    dataModel = new DataModel();
                    if (dataStream[0] == hw.Ack)
                    {
                        dataModel.Valid = true;
                    }
                    else
                    {
                        dataModel.Valid = false;
                    }
                    dataModel.ErrorCode = dataStream[1];
                    dataModel.Mode = dataStream[2];
                    dataModel.State = dataStream[3];
                    byte[] v = new byte[] { 0, 0, dataStream[4], dataStream[5] };
                    Array.Reverse(v);
                    dataModel.SampleId = BitConverter.ToUInt16(v, 0);

                    v = new byte[] { 0, 0, dataStream[6], dataStream[7] };
                    Array.Reverse(v);
                    dataModel.EventId = BitConverter.ToUInt16(v, 0);

                    v = new byte[] { 0, 0, dataStream[8], dataStream[9] };
                    Array.Reverse(v);
                    val = BitConverter.ToUInt16(v, 0);
                    dataModel.TempRawData = val;
                    dataModel.TemperatureInDegreeCelsius = _hw.GetTemperatureInDegreeCelsius((ushort)val);


                    v = new byte[] { dataStream[10], dataStream[11], dataStream[12], dataStream[13] };
                    Array.Reverse(v);
                    val = BitConverter.ToUInt32(v, 0);
                    dataModel.XRawData = val;
                    dataModel.XAccelerationInMeterPerSecondSquare = _hw.GetAccerlerationInMeterPerSecondSquare(val, _range);

                    v = new byte[] { dataStream[14], dataStream[15], dataStream[16], dataStream[17] };
                    Array.Reverse(v);
                    val = BitConverter.ToUInt32(v, 0);
                    dataModel.YRawData = val;
                    dataModel.YAccelerationInMeterPerSecondSquare = _hw.GetAccerlerationInMeterPerSecondSquare(val, _range);

                    v = new byte[] { dataStream[18], dataStream[19], dataStream[20], dataStream[21] };
                    Array.Reverse(v);
                    val = BitConverter.ToUInt32(v, 0);
                    dataModel.ZRawData = val;
                    dataModel.ZAccelerationInMeterPerSecondSquare = _hw.GetAccerlerationInMeterPerSecondSquare(val, _range);

                    dataModel.TimeInSec = timestep;

                    Produce(_dataBufferBlock, dataModel);

                    timestep = timestep + (double)(1.0 / _dataRateForOffset);
                    dataStream.RemoveRange(0, hw.RxProtocolLength);
                }
            }
        }
        /// <summary>
        /// This task consumes the received data from the COM port. The method will pack the data
        /// into data tuples and store each tuple in a list object. The data will also be stored in the 
        /// "_storageData" List for the possibility of saving the data to the disk.
        /// Once there are enough data points in the list (the number is defined by the constant value
        /// "MinimumDataSizePrioDecimation" the method will invalidate the plots and redraw them. After 
        /// this, the tuple lists will be cleared again for the next bunch of data sets.
        /// </summary>
        /// <returns></returns>
        private async Task SignalProcessingTask()
        {
            ISourceBlock<DataModel> dataSourceBlock = _dataBufferBlock;
            List<Tuple<double, double>> tupleListX = new List<Tuple<double, double>>();
            List<Tuple<double, double>> tupleListY = new List<Tuple<double, double>>();
            List<Tuple<double, double>> tupleListZ = new List<Tuple<double, double>>();
            Tuple<double, double> tupleX;
            Tuple<double, double> tupleY;
            Tuple<double, double> tupleZ;

            int counter = 0;
            while (await dataSourceBlock.OutputAvailableAsync(_ct))
            {
                DataModel processed = new DataModel();
                DataModel acq = await dataSourceBlock.ReceiveAsync(_ct);
                _storageData.Add(acq);

                tupleX = Tuple.Create(acq.TimeInSec, (double)acq.XRawData);
                tupleListX.Add(tupleX);

                tupleY = Tuple.Create(acq.TimeInSec, (double)acq.YRawData);
                tupleListY.Add(tupleY);

                tupleZ = Tuple.Create(acq.TimeInSec, (double)acq.ZRawData);
                tupleListZ.Add(tupleZ);

                counter++;

                if (counter % MinimumDataSizePrioDownsampling == 0)
                {
                    DrawPoints(xOffsetModel, tupleListX);
                    DrawPoints(yOffsetModel, tupleListY);
                    DrawPoints(zOffsetModel, tupleListZ);
                    tupleListX.Clear();
                    tupleListY.Clear();
                    tupleListZ.Clear();
                }
            }
        }
        /// <summary>
        /// Largest-Triangle-Three Bucket Downsampling. When graphing large sets of data, plotting fewer points but still
        /// representing the data visually can be challenging. Loading 500 vs. 5000 points can help improve load time and
        /// reduce system resources. A co-worker pointed me in the direction of a downsampling plugin for flot charts.
        /// The author, Svein Steinarsson, published a paper detailing several downlsampling algorithms for graphs.
        /// https://www.danielwjudge.com/largest-triangle-three-bucket-downsampling-c/
        /// https://skemman.is/bitstream/1946/15343/3/SS_MSthesis.pdf
        /// https://github.com/sveinn-steinarsson
        /// </summary>
        /// <param name="data"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private IEnumerable<Tuple<double, double>> LargestTriangleThreeBuckets(List<Tuple<double, double>> data, int threshold)
        {
            int dataLength = data.Count;
            if (threshold >= dataLength || threshold == 0)
                return data; // Nothing to do

            List<Tuple<double, double>> sampled = new List<Tuple<double, double>>(threshold);

            // Bucket size. Leave room for start and end data points
            double every = (double)(dataLength - 2) / (threshold - 2);

            int a = 0;
            Tuple<double, double> maxAreaPoint = new Tuple<double, double>(0, 0);
            int nextA = 0;

            sampled.Add(data[a]); // Always add the first point

            for (int i = 0; i < threshold - 2; i++)
            {
                // Calculate point average for next bucket (containing c)
                double avgX = 0;
                double avgY = 0;
                int avgRangeStart = (int)(Math.Floor((i + 1) * every) + 1);
                int avgRangeEnd = (int)(Math.Floor((i + 2) * every) + 1);
                avgRangeEnd = avgRangeEnd < dataLength ? avgRangeEnd : dataLength;

                int avgRangeLength = avgRangeEnd - avgRangeStart;

                for (; avgRangeStart < avgRangeEnd; avgRangeStart++)
                {
                    avgX += data[avgRangeStart].Item1; // * 1 enforces Number (value may be Date)
                    avgY += data[avgRangeStart].Item2;
                }
                avgX /= avgRangeLength;

                avgY /= avgRangeLength;

                // Get the range for this bucket
                int rangeOffs = (int)(Math.Floor((i + 0) * every) + 1);
                int rangeTo = (int)(Math.Floor((i + 1) * every) + 1);

                // Point a
                double pointAx = data[a].Item1; // enforce Number (value may be Date)
                double pointAy = data[a].Item2;

                double maxArea = -1;

                for (; rangeOffs < rangeTo; rangeOffs++)
                {
                    // Calculate triangle area over three buckets
                    double area = Math.Abs((pointAx - avgX) * (data[rangeOffs].Item2 - pointAy) -
                                           (pointAx - data[rangeOffs].Item1) * (avgY - pointAy)
                                      ) * 0.5;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        maxAreaPoint = data[rangeOffs];
                        nextA = rangeOffs; // Next a is this b
                    }
                }

                sampled.Add(maxAreaPoint); // Pick this point from the bucket
                a = nextA; // This a is the next a (chosen b)
            }

            sampled.Add(data[dataLength - 1]); // Always add last

            return sampled;
        }
        /// <summary>
        /// This is the background worker thread for the processing of the data. 
        /// First, data will be  filtered using a median filter. Second, the mean value 
        /// of the filtered data will be calculated and converted to a unsigned 16-bit 
        /// integer. The registers on the sensor are only 16-bit wide, thus the lower
        /// 16-bit will be written into the textboxes on the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataProcessingBackgroundWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<double> xFilteredValues = new List<double>();
            List<double> yFilteredValues = new List<double>();
            List<double> zFilteredValues = new List<double>();

            OnlineMedianFilter xMedianFilter = new OnlineMedianFilter(MedianWindowSize);
            OnlineMedianFilter yMedianFilter = new OnlineMedianFilter(MedianWindowSize);
            OnlineMedianFilter zMedianFilter = new OnlineMedianFilter(MedianWindowSize);

            ProgressBarMax = _storageData.Count;
            for (int i = 0; i < _storageData.Count; i++)
            {
                xFilteredValues.Add(xMedianFilter.ProcessSample(_storageData[i].XRawData));
                yFilteredValues.Add(yMedianFilter.ProcessSample(_storageData[i].YRawData));
                zFilteredValues.Add(zMedianFilter.ProcessSample(_storageData[i].ZRawData));
                CurrentProgress = i;
            }
            _xOffset = (UInt16)xFilteredValues.Average();
            _yOffset = (UInt16)yFilteredValues.Average();
            _zOffset = (UInt16)zFilteredValues.Average();
            XOffset = _xOffset;
            YOffset = _yOffset;
            ZOffset = _zOffset;
        }
        /// <summary>
        /// When the background worker task for data processing has completed, this method will
        /// set _isProcessing to false because data processing has finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataProcessingBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _isProcessing = false;
            
        }
        /// <summary>
        /// This is the background worker thread for the transmission of the configuration data
        /// to the hardware.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ConfigurationTransmissionBackgroundWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            CurrentProgress = 0;
            ProgressBarMin = 0;
            ProgressBarMax = 4;

            if (_usedDeviceVcp.IsOpen == false)
            {
                _usedDeviceVcp.Open(ConnectionString, 921600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

            }
                      
            _usedDeviceVcp.Port.Write(_hw.SetAcousticParameters((short)_configuration.NumberOfSamplesPerAcousticStimulus,
                                                                (short)_configuration.FrequencyOfAcousticStimulusInMillihertz,
                                                                (short)_configuration.DurationOfEventInSeconds,
                                                                (short)_configuration.DurationOfAcousticStimulusInMilliSeconds), 0, hw.TxProtocolLength);
            Thread.Sleep(300);
            CurrentProgress++;

            _usedDeviceVcp.Port.Write(_hw.SetTriggerModeParameters((short)_configuration.NumberOfSamplesPerTriggerEvent,
                                                                   (byte)_configuration.TriggerPolarity), 0, hw.TxProtocolLength);
            Thread.Sleep(300);
            CurrentProgress++;

            _usedDeviceVcp.Port.Write(_hw.SetAdxlParameters(_configuration.HighpassFilterFrequency,
                                                            (byte)_selectedOutputDataRateIdx,
                                                            (byte)_selectedRangeIdx,
                                                            _configuration.ActivityCounts), 0, hw.TxProtocolLength);
            Thread.Sleep(300);
            CurrentProgress++;

            _usedDeviceVcp.Port.Write(_hw.SetAdxlOffset((short)_configuration.XOffset,
                                                        (short)_configuration.YOffset,
                                                        (short)_configuration.ZOffset,
                                                        (short)_configuration.ActivityThreshold), 0, hw.TxProtocolLength);
            Thread.Sleep(300);
            CurrentProgress++;
        }
        /// <summary>
        /// When the background worker task for the configuration has completed, this method
        /// will save the prefences to the hard disk and set the _isConfigTransmissionFinished 
        /// flag to true.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ConfigurationTransmissionBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SaveDataContext();
            _isConfigTransmissionFinished = true;
            if (_usedDeviceVcp.IsOpen == true)
            {
                _usedDeviceVcp.Port.CloseIt();
                _usedDeviceVcp.Port.DiscardInBuffer();
                _usedDeviceVcp.Rxbuffer.Complete();
                _usedDeviceVcp.Close();
            }

            try
            {
                _usedDeviceVcp.Port.Dispose();
            }
            catch { }
            DialogResult = true;
        }
        #endregion

    }
}
