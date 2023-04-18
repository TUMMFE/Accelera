using Accelera.Hardware;
using Accelera.Models;
using Accelera.Views;
using CsvHelper;
using MFE.Communication;
using MicroMvvm;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Input;
using Windows.Media.Capture;

namespace Accelera.ViewModels
{
    public class AcousticDialogViewModel : ObservableObject
    {
        #region Constants   
        public const int InvalidatePlotEveryNthValue = 1000;
        public const int MaximumRollover = 1000;
        public const int DecimationFactor = 5;
        public const int MinimumDataSizePrioDownsampling = 1000;
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

        private CancellationTokenSource _tsStimulationTimer = new CancellationTokenSource();
        private BackgroundWorker _singleProgressBlockBackgroundWorker;
        private BackgroundWorker _pauseProgressBackgroundWorker;

        private DispatcherTimerEx _pauseTimer;
        private long _elapsedPauseTime;
            

        private hw _hw;
        private Vcp _usedDeviceVcp;

        private bool _canStart = true;
        private bool _canStop = false;
        private bool _canPause = false;
        private bool _canResume = false;
        private bool _canSave = false;
        private bool _canCancel = true;

        private bool _isStimulationPause = false;
        private bool _currentStimulationBlockFinished;


        private ConfigurationModel _configuration;


        private int _blockDuration = 0;
        private int _stimulusRate = 0;
        private int _pauseDuration = 0;
        private int _blockRepetitions = 0;

        private int _currentProgressSingleBlock;
        private int _progressBarSingleBlockMin;
        private int _progressBarSingleBlockMax;

        private int _currentProgressPause;
        private int _progressBarPauseMin;
        private int _progressBarPauseMax;


        private int _currentProgressTotal;
        private int _progressBarTotalMin;
        private int _progressBarTotalMax;

        private int _stimuliPerBlock;
        private int _stimuliTotal;

        private int _counterStimuliBlock;
        private int _counterStimuliTotal;
        private int _counterBlocks;        

        

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
        public int BlockDuration
        {
            get => _blockDuration;
            set
            {
                _blockDuration = value;
                RaisePropertyChanged("BlockDuration");
            }
        }
        public int StimulusRate
        {
            get => _stimulusRate;
            set
            {
                _stimulusRate = value;
                RaisePropertyChanged("StimulusRate");
            }
        }
        public int PauseDuration
        {
            get => _pauseDuration;
            set
            {
                _pauseDuration = value;
                RaisePropertyChanged("PauseDuration");
            }
        }
        public int BlockRepetitions
        {
            get => _blockRepetitions;
            set
            {
                _blockRepetitions = value;
                RaisePropertyChanged("BlockRepetitions");
            }
        }
        public int CurrentProgressSingleBlock
        {
            get => _currentProgressSingleBlock;
            set
            {
                _currentProgressSingleBlock = value;
                RaisePropertyChanged("CurrentProgressSingleBlock");
            }
        }
        public int ProgressBarSingleBlockMin
        {
            get => _progressBarSingleBlockMin;
            set
            {
                _progressBarSingleBlockMin = value;
                RaisePropertyChanged("ProgressBarSingleBlockMin");
            }
        }
        public int ProgressBarSingleBlockMax
        {
            get => _progressBarSingleBlockMax;
            set
            {
                _progressBarSingleBlockMax = value;
                RaisePropertyChanged("ProgressBarSingleBlockMax");
            }
        }
        public int CurrentProgressPause
        {
            get => _currentProgressPause;
            set
            {
                _currentProgressPause = value;
                RaisePropertyChanged("CurrentProgressPause");
            }
        }
        public int ProgressBarPauseMin
        {
            get => _progressBarPauseMin;
            set
            {
                _progressBarPauseMin = value;
                RaisePropertyChanged("ProgressBarPauseMin");
            }
        }
        public int ProgressBarPauseMax
        {
            get => _progressBarPauseMax;
            set
            {
                _progressBarPauseMax = value;
                RaisePropertyChanged("ProgressBarPauseMax");
            }
        }
        public int CurrentProgressTotal
        {
            get => _currentProgressTotal;
            set
            {
                _currentProgressTotal = value;
                RaisePropertyChanged("CurrentProgressTotal");
            }
        }
        public int ProgressBarTotalMin
        {
            get => _progressBarTotalMin;
            set
            {
                _progressBarTotalMin = value;
                RaisePropertyChanged("ProgressBarTotalMin");
            }
        }
        public int ProgressBarTotalMax
        {
            get => _progressBarTotalMax;
            set
            {
                _progressBarTotalMax = value;
                RaisePropertyChanged("ProgressBarTotalMax");
            }
        }

        public bool CanEdit { get; set; }

        public PlotModel AccelerationPlotModel { get; set; }
        public string ConnectionString { get; set; }
        #endregion

        #region Constructors

        ///=================================================================================================
        /// <summary>Default constructor.
        ///          The constructor will generate a bunch of objects, initialize the plot panels and 
        ///          fill the UI with standard settings from the preference file.
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
        ///=================================================================================================
        public AcousticDialogViewModel()
        {
            AccelerationPlotModel = new PlotModel();
            _dataBufferBlock = new BufferBlock<DataModel>();
            _storageData = new List<DataModel>();
            
            _hw = new hw();
            _usedDeviceVcp = new Vcp();

            _ct = _ts.Token;

            _currentProgressSingleBlock = 0;
            _progressBarSingleBlockMin = 0;
            _progressBarSingleBlockMax = 0;
            _currentProgressPause = 0;
            _progressBarPauseMin = 0;
            _progressBarTotalMax = 0;
            _currentProgressTotal = 0;
            _progressBarTotalMin = 0;
            _progressBarTotalMax = 0;
            CanEdit = true;

            AccelerationPlotModel.Series.Add(new LineSeries() { Color = OxyColor.FromRgb(43,138,128), InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline });
            PlotAxisFormatting(AccelerationPlotModel, "Acceleration (|a|)", "time in s", "acceleration in m/s^2");

            Globals.Log.Wpf("== AcousticDialogViewModel ==");


            //here we shall load some standard settings from the disk                           
            SystemSettings systemSettings = new SystemSettings();
            _configuration = systemSettings.LoadOrCreate();
            UpdateDataContext();
          


        }
        #endregion

        #region Member Methods

        ///=================================================================================================
        /// <summary>   Updates the data context. 
        ///             The UI bindings will be updated with current _configuration object. 
        ///             The maximum values of the progress bars will be calculated and set</summary>
        ///
        /// <remarks>   Berng, 12.04.2023. </remarks>
        ///=================================================================================================
        private void UpdateDataContext()
        {
            _blockDuration = _configuration.DurationOfEventInSeconds;
            _stimulusRate = _configuration.FrequencyOfAcousticStimulusInMillihertz;
            _pauseDuration = _configuration.PauseTimeinSeconds;
            _blockRepetitions = _configuration.BlockRepetitions;

            _stimuliPerBlock = (_configuration.DurationOfEventInSeconds * _configuration.FrequencyOfAcousticStimulusInMillihertz) / 1000;
            _stimuliTotal = _stimuliPerBlock * _blockRepetitions;

            _progressBarTotalMax = _stimuliTotal;
            ProgressBarTotalMax  = _progressBarTotalMax;

            _progressBarSingleBlockMax = _stimuliPerBlock;
            ProgressBarSingleBlockMax = _progressBarSingleBlockMax;

            _progressBarPauseMax = _pauseDuration;
            ProgressBarPauseMax = _progressBarPauseMax;
        }   
        /// <summary>
        /// Load the settings from the settings file. If this file does not exist, 
        /// than generate a standard setting file. 
        /// If the folder does not exist, the method will try to create this folder. 
        /// If the folder could not be created, the application will be terminated.
        /// </summary>
        private void CheckApplicationFiles()
        {
            SystemSettings systemSettings = new SystemSettings();
            _configuration = systemSettings.LoadOrCreate();            
        }
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
        private void ClearPlotSeries(PlotModel pm, OxyColor color, string xaxis, string yaxis)
        {
            (pm.Series[0] as LineSeries).Points.Clear();
            pm.InvalidatePlot(true);
            pm.Series.Clear();
            pm.Series.Add(new LineSeries() { Color = color });
            pm.InvalidatePlot(true);
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
            int Threshold = _configuration.NumberOfSamplesPerAcousticStimulus  / DecimationFactor; //MinimumDataSizePrioDownsampling
            (pm.Series[0] as LineSeries).Points.Clear();
            IEnumerable<Tuple<double, double>> interpolatedData = LargestTriangleThreeBuckets(dataTuples, Threshold);
         
            foreach (Tuple<double, double> item in interpolatedData)
            {
                (pm.Series[0] as LineSeries).Points.Add(new DataPoint(item.Item1, item.Item2));
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
        ///=================================================================================================
        /// <summary>Prepare data aquisition.
        ///          the cancellation token for the data Rx thread will be generated, the plot panels will
        ///          be initialized with title, colors, axis labels. The com port will be cleared and 
        ///          the Rx buffers will be completed. The com port will be opened and but into the 
        ///          listening mode.</summary>
        ///
        /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
        ///=================================================================================================
        private void PrepareDataAquisition()
        {
            _ts = new CancellationTokenSource();
            _ct = _ts.Token;
            if (_usedDeviceVcp.IsOpen == false)
            {
                _usedDeviceVcp.Open(ConnectionString, 921600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            }
            _usedDeviceVcp.Rxbuffer.Complete();
            _usedDeviceVcp.Restart();

            ClearPlotSeries(AccelerationPlotModel, OxyColor.FromRgb(43, 138, 128), "time in s", "acceleration in m/s^2");

            _usedDeviceVcp.Port.StartListening();
        }
        ///=================================================================================================
        /// <summary>Update configuration model from view.
        ///          The method will updatae the _configuration object from the UI values and will set
        ///          the maximum values of the progressbar. The method will calculate the total number of
        ///          stimuli as well as the number of stimuli per block (event)
        /// </summary>
        ///
        /// <remarks>Bernhard Gleich, 08.04.2023.</remarks>
        ///=================================================================================================
        private void UpDateConfigModelFromView()
        {
            //set limits for the progress bars
            _configuration.DurationOfEventInSeconds = _blockDuration;
            _configuration.FrequencyOfAcousticStimulusInMillihertz = _stimulusRate;
            _configuration.PauseTimeinSeconds = _pauseDuration;
            _configuration.BlockRepetitions = _blockRepetitions;

            _stimuliPerBlock = (_configuration.DurationOfEventInSeconds * _configuration.FrequencyOfAcousticStimulusInMillihertz) / 1000;
            _stimuliTotal = _stimuliPerBlock * _blockRepetitions;

            _progressBarTotalMax = _stimuliTotal;
            ProgressBarTotalMax = _progressBarTotalMax;

            _progressBarSingleBlockMax = _stimuliPerBlock;
            ProgressBarSingleBlockMax = _progressBarSingleBlockMax;

            _progressBarPauseMax = _pauseDuration;
            ProgressBarPauseMax = _progressBarPauseMax;
        }
        #endregion

        #region Commands

        public ICommand StartButtonClicked { get { return new RelayCommand(OnStartButtonClicked, CanStartButtonBeExecuted); } }
        public ICommand StopButtonClicked { get { return new RelayCommand(OnStopButtonClicked, CanStopButtonBeExecuted); } }
        public ICommand PauseButtonClicked { get { return new RelayCommand(OnPauseButtonClicked, CanPauseButtonBeExecuted); } }
        public ICommand ResumeButtonClicked { get { return new RelayCommand(OnResumeButtonClicked, CanResumeButtonBeExecuted); } }
        public ICommand SaveButtonClicked { get { return new RelayCommand(OnSaveButtonClicked, CanSaveButtonBeExecuted); } }
        public ICommand SaveAsButtonClicked { get { return new RelayCommand(OnSaveAsButtonClicked, CanSaveAsButtonBeExecuted); } }
        public ICommand CancelButtonClicked { get { return new RelayCommand(OnCancelButtonClicked, CanCancelButtonBeExecuted); } }

        /// <summary>
        /// Can only start if the system is not already running or is not paused or it is not resumed
        /// </summary>
        /// <returns></returns>
        bool CanStartButtonBeExecuted()
        {            
            return _canStart;
        }
        /// <summary>
        /// Stop is possible if the system is running or resumed
        /// </summary>
        /// <returns></returns>
        bool CanStopButtonBeExecuted()
        {
            return _canStop; 
        }
        /// <summary>
        /// Pause is possible if the system is running or resumed
        /// </summary>
        /// <returns></returns>
        bool CanPauseButtonBeExecuted()
        {
            return _canPause;
        }
        /// <summary>
        /// Resume is possible if the system is paused
        /// </summary>
        /// <returns></returns>
        bool CanResumeButtonBeExecuted()
        {
            return _canResume;
        }
        /// <summary>
        /// Save is possible if the system is finished
        /// </summary>
        /// <returns></returns>
        bool CanSaveButtonBeExecuted()
        {
            return _canSave;
        }
        bool CanSaveAsButtonBeExecuted()
        {
            return _canSave;
        }
        /// <summary>
        /// Cancel is possible if the system is paused or finished
        /// </summary>
        /// <returns></returns>
        bool CanCancelButtonBeExecuted()
        {
            return _canCancel;
        }
        ///=================================================================================================
        /// <summary>Executes a task periodically.</summary>
        ///
        /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
        ///
        /// <param name="onTick">Method/Action which should be executed periodically</param>
        /// <param name="dueTime">Due time/wait time before the periodic start is initiated.</param>
        /// <param name="interval">The periodic interval.</param>
        /// <param name="token">A token that allows processing to be cancelled.</param>
        ///
        /// <returns>A Task.</returns>
        ///=================================================================================================
        private static async Task RunPeriodicAsync(Action onTick, TimeSpan dueTime, TimeSpan interval, CancellationToken token)
        {
            if (dueTime > TimeSpan.Zero)
            {
                await Task.Delay(dueTime, token);
            }
            while (!token.IsCancellationRequested)
            {
                onTick?.Invoke();
                if (interval > TimeSpan.Zero)
                {
                    await Task.Delay(interval, token);
                }
            }
        }
        ///=================================================================================================
        /// <summary>Executes the 'stimulus tick' action.
        ///          On every tick the method will start a single shot measurement from the hardware device.
        ///          The number of samples per acoustic stimulus is given in the _configuration object. 
        ///          The hardware will execute a 'beep' and will not wait for an external trigger
        ///          event.</summary>
        ///
        /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
        ///=================================================================================================
        private void OnStimulusTick()
        {
            _usedDeviceVcp.Port.Write(_hw.SetSamples((short)_configuration.NumberOfSamplesPerAcousticStimulus, true, false), 0, hw.TxProtocolLength);         
            _counterStimuliBlock++;
            _counterStimuliTotal++;
            CurrentProgressSingleBlock = _counterStimuliBlock;
            CurrentProgressTotal = _counterStimuliTotal;
        }
        ///=================================================================================================
        /// <summary>Executes the 'start button clicked' action.
        ///          The com port will be opened, the _configuration object will be updated from the UI, the
        ///          counters of stimuli and blocks will be reseted and the _isStimulationPause flag will be
        ///          set to false. 
        ///          The progress of all progressbars will be set to 0. The background worker thread which
        ///          indicates the progress on the progressbar will be generated and started. 
        ///          The stimulus timer task which starts the single shot measurements will be started as
        ///          well as the data Rx and signal processing task. The _storage object will be cleared
        ///          and the plot panels will be prepared for data aquisition-          
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
        ///=================================================================================================
        private void OnStartButtonClicked()
        {
            Globals.Log.Info("START BUTTON CLICKED.");
            _canStart = false;
            _canStop = true;
            _canPause = true;
            _canResume = false;
            _canSave = false;            
            _canCancel = false;
            CanEdit = false;
            
            if (_usedDeviceVcp.IsOpen == false)
            {
                _usedDeviceVcp.Open(ConnectionString, 921600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            }

            UpDateConfigModelFromView();
            _counterStimuliBlock = 0;
            _counterStimuliTotal = 0;
            _counterBlocks = 0;
            _isStimulationPause = false;
            _currentStimulationBlockFinished = false;


            CurrentProgressSingleBlock = 0;
            CurrentProgressTotal = 0;
            CurrentProgressPause = 0;

            var dueTime = TimeSpan.FromSeconds(0);
            var interval = TimeSpan.FromSeconds(1000 / _configuration.FrequencyOfAcousticStimulusInMillihertz);
            _tsStimulationTimer = new CancellationTokenSource();

            _pauseTimer = new DispatcherTimerEx();
            _pauseTimer.Interval = TimeSpan.FromSeconds(1);
            _pauseTimer.Tick += PauseTimerTick;
            
            _singleProgressBlockBackgroundWorker = new BackgroundWorker();
            _singleProgressBlockBackgroundWorker.WorkerSupportsCancellation = true;
            _singleProgressBlockBackgroundWorker.DoWork += new DoWorkEventHandler(SingleProgressBlockBackgroundWorker);
            _singleProgressBlockBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SingleProgressBlockBackgroundWorkerCompleted);
            
            _singleProgressBlockBackgroundWorker.RunWorkerAsync();

            PrepareDataAquisition();                    //prepare plots and start the data aquisistion
            DataCollectionTask();                       //start data collection task
            SignalProcessingTask();                     //start signal processing
            
            _storageData.Clear();                       //clear the storage list
            RunPeriodicAsync(OnStimulusTick, dueTime, interval, _tsStimulationTimer.Token);               
        }
        private void PauseProgressBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
            }
            if ((int)e.Result == 15)
            {                                                
                CurrentProgressSingleBlock = 0;
                _counterStimuliBlock = 0;
                _isStimulationPause = false;
                _pauseTimer.Stop();
                _tsStimulationTimer = new CancellationTokenSource();
                var dueTime = TimeSpan.FromSeconds(0);
                var interval = TimeSpan.FromSeconds(1000 / _configuration.FrequencyOfAcousticStimulusInMillihertz);
                RunPeriodicAsync(OnStimulusTick, dueTime, interval, _tsStimulationTimer.Token);
                _singleProgressBlockBackgroundWorker = new BackgroundWorker();
                _singleProgressBlockBackgroundWorker.WorkerSupportsCancellation = true;
                _singleProgressBlockBackgroundWorker.DoWork += new DoWorkEventHandler(SingleProgressBlockBackgroundWorker);
                _singleProgressBlockBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SingleProgressBlockBackgroundWorkerCompleted);
                _singleProgressBlockBackgroundWorker.RunWorkerAsync();
                CurrentProgressPause = 0;
            }
        }
        private void PauseProgressBackgroundWorker(object sender, DoWorkEventArgs e)
        {
            while ((_elapsedPauseTime < _configuration.DurationOfEventInSeconds) & (_pauseProgressBackgroundWorker.CancellationPending == false))
            {

            }
            if (_pauseProgressBackgroundWorker.CancellationPending == true)
            {
                e.Cancel = true;
                return;
            }
            if (_elapsedPauseTime == _configuration.DurationOfEventInSeconds)
            {
                e.Result = 15;
            }            
        }
        private void PauseTimerTick(object sender, EventArgs e)
        {
            CurrentProgressPause++;
            _elapsedPauseTime++;
        }
        
        private void SingleProgressBlockBackgroundWorker(object sender, DoWorkEventArgs e)
        {
            while ((_counterStimuliBlock < _stimuliPerBlock) & (_counterBlocks < _configuration.BlockRepetitions) & (_singleProgressBlockBackgroundWorker.CancellationPending == false))
            {
                               
            }
            if (_singleProgressBlockBackgroundWorker.CancellationPending == true)
            {                
                e.Cancel = true;
                return;
            } 
            if (_counterStimuliBlock == _stimuliPerBlock) 
            {
                _currentStimulationBlockFinished = true;
                e.Result = _currentStimulationBlockFinished;
            }           
        }
        private void SingleProgressBlockBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {            
            if (e.Cancelled == true)
            {
                Globals.Log.Info("Block Backgroundworker canceled." + _counterStimuliBlock.ToString());
            }
            else if ((bool)e.Result == _currentStimulationBlockFinished)
            {
                _tsStimulationTimer.Cancel();
                _counterBlocks++;
                if (_counterBlocks < _configuration.BlockRepetitions)
                {                                        
                    CurrentProgressPause = 0;
                    _isStimulationPause = true;
                    _pauseTimer = new DispatcherTimerEx();
                    _pauseTimer.Interval = TimeSpan.FromSeconds(1);
                    _pauseTimer.Tick += PauseTimerTick;
                    _elapsedPauseTime = 0;

                    _pauseProgressBackgroundWorker = new BackgroundWorker();
                    _pauseProgressBackgroundWorker.WorkerSupportsCancellation = true;
                    _pauseProgressBackgroundWorker.DoWork += new DoWorkEventHandler(PauseProgressBackgroundWorker);
                    _pauseProgressBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PauseProgressBackgroundWorkerCompleted);
                    _pauseProgressBackgroundWorker.RunWorkerAsync();
                    _pauseTimer.Start();
                } else
                {
                    _canPause = false;                   
                    Globals.Log.Info("Acoustic stimulation experiment finished.");
                    MessageBox.Show("Stimulation protocol finished. Press STOP to finalize the data aquisition.", "Information");
                }
            }
        }

        ///=================================================================================================
        /// <summary>Executes the 'stop button clicked' action.
        ///          The method will cancel all tasks like
        ///           - background worker task for progress bar indication  
        ///           - stimulus timer task which starts the single shot measurements  
        ///          The method will close the used com port and stops its listening to the
        ///          Rx buffers.
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
        ///=================================================================================================
        private async void OnStopButtonClicked()
        {
            Globals.Log.Info("STOP BUTTON CLICKED.");
            _canStart = true;
            _canStop = false;
            _canPause = false;
            _canResume = false;
            _canSave = (_storageData.Count > 0) ? true : false;
            _canCancel = true;
            CanEdit = true;

            _ts.Cancel();                       //cancel the data reception task
            _ts.Dispose();                      //destroy the cancellation token of the data reception task
            _tsStimulationTimer.Cancel();       //cancel the stimulation time task 

            //wait at least one period intervall second, thus the task has a chance to be cancled.
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait; 
            var sleepTime = TimeSpan.FromSeconds((1000 / _configuration.FrequencyOfAcousticStimulusInMillihertz));
            Thread.Sleep(sleepTime);            
            Mouse.OverrideCursor = null;

            _tsStimulationTimer.Dispose();      //destroy the cancellation token of the stimulation timer task 

            //cancel the background worker for the progress bar update of the single block progress bar
            _singleProgressBlockBackgroundWorker.CancelAsync();

            _pauseTimer.Stop();

            //go back to standard mode to reset internal device counters
            _usedDeviceVcp.Port.Write(_hw.SetOpMode(0x03, 0x01), 0, hw.TxProtocolLength);
            //close the com port and stop listening to this port
            _usedDeviceVcp.Port.CloseIt();          //stop listening to the port
            _usedDeviceVcp.Port.DiscardInBuffer();  //discard all input buffer
            _usedDeviceVcp.Port.DiscardOutBuffer(); //discard all output buffer
            _usedDeviceVcp.Rxbuffer.Complete();     //the rx producer/consumer model will not accept data any longer
        }

        ///=================================================================================================
        /// <summary>Executes the 'pause button clicked' action.
        ///          If the button was pressed during a stimulation block, the stimulus timer which
        ///          starts the single shot measurements will be cancled. If a pause request happed 
        ///          during the pause time, the pause dispatcher timer is paused. 
        ///          In all cases the the background worker task for the single block progress will
        ///          be cancled also.</summary>
        ///
        /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
        ///=================================================================================================
        private void OnPauseButtonClicked()
        {
            Globals.Log.Info("PAUSE BUTTON CLICKED.");
            _canStart = false;
            _canStop = false;
            _canPause = false;
            _canResume = true;
            _canSave = (_storageData.Count > 0) ? true : false;
            _canCancel = true;

            _singleProgressBlockBackgroundWorker.CancelAsync();
            //A pause request must be treated differently depending on the state of the stimulation protocoll
            // - if a stimulation block is currently running, then the stimulation timer must be cancled to stop further stimuli
            // - if a stimulation pause is active, then the pause timer must be stopped and the already elapsed pause time must be saved
            if (_isStimulationPause == false)
            {
                _tsStimulationTimer.Cancel();       //cancel the stimulation time task 
                //wait at least one period intervall second, thus the task has a chance to be cancled.
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                var sleepTime = TimeSpan.FromSeconds((1000 / _configuration.FrequencyOfAcousticStimulusInMillihertz));
                Thread.Sleep(sleepTime);
                Mouse.OverrideCursor = null;
                _tsStimulationTimer.Dispose();      //destroy the cancellation token of the stimulation timer task 

            }
            else
            {
                _pauseTimer.Pause();               
            }
        }

        ///=================================================================================================
        /// <summary>Executes the 'resume button clicked' action.
        ///          If prior to the pause request a stimulation block was active, this block will be 
        ///          resumed by restarting the stimulation timer and its background worker thread. 
        ///          The counters will not be reseted.
        ///          If a pause was active befor, the pause timer will be resumed.
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
        ///=================================================================================================
        private void OnResumeButtonClicked()
        {
            Globals.Log.Info("RESUME BUTTON CLICKED.");
            _canStart = false;
            _canStop = true;
            _canPause = true;
            _canResume = false;
            _canSave = false;
            _canCancel = false;
            
            //A resume request must be treated differently depending on the state of the stimulation protocoll
            // - if a stimulation block was running before, then the stimulation timer must be started again
            // - if a stimulation pause was active before, then the pause timer must be resumed
            if (_isStimulationPause == false)
            {
                var dueTime = TimeSpan.FromSeconds(0);
                var interval = TimeSpan.FromSeconds(1000 / _configuration.FrequencyOfAcousticStimulusInMillihertz);
                _tsStimulationTimer = new CancellationTokenSource();

                _singleProgressBlockBackgroundWorker = new BackgroundWorker();
                _singleProgressBlockBackgroundWorker.WorkerSupportsCancellation = true;
                _singleProgressBlockBackgroundWorker.DoWork += new DoWorkEventHandler(SingleProgressBlockBackgroundWorker);
                _singleProgressBlockBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SingleProgressBlockBackgroundWorkerCompleted);
                RunPeriodicAsync(OnStimulusTick, dueTime, interval, _tsStimulationTimer.Token);                
            } else
            {
                _pauseTimer.Resume();
            }
        }

        ///=================================================================================================
        /// <summary>Executes the 'save button clicked' action.
        ///          The entered data will be stored in the preference file. Afterwards a file open dialog
        ///          appear and the user can save the experimental data. At the end the dialog window will 
        ///          be closed when storage of data was successfull. If not, the window will be kept open
        ///          and the dialog will not be closed.
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
        ///=================================================================================================

        private void OnSaveButtonClicked()
        {
            //show save as dialog
            Globals.Log.Info("SAVE BUTTON CLICKED.");
            SaveFileDialog sdialog = new SaveFileDialog();
            string fileName = string.Empty;

            //link blockId in storage data to each record.

            int dataPointsPerBlock = _stimuliPerBlock * _configuration.NumberOfSamplesPerAcousticStimulus;
            int id = 0;
            for (int i = 0; i<_storageData.Count; i++)
            {
                _storageData[i].BlockId = id;
                if ((i % dataPointsPerBlock == 0) && (i>0))
                { 
                    id++;
                }
            }
            
            sdialog.Filter = "CSV File (*.csv)|*.csv";
            if (sdialog.ShowDialog() == true)
            {
                fileName = sdialog.FileName;
                using (var writer = new StreamWriter(fileName))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(_storageData);
                }

                //save summary file
                _configuration.TotalNumberOfAquiredBlocks = _counterBlocks;
                _configuration.TotalNumberOfAquiredDataFrames = _storageData.Count();
                _configuration.TotalNumberOfAquiredEvents = _storageData[_storageData.Count()-1].EventId+1;
                _configuration.SetValueSamplesPerDataFrame = _configuration.NumberOfSamplesPerAcousticStimulus;
                _configuration.DateTimeOfExperiment = DateTime.Now;
                var infoFile = Path.ChangeExtension(fileName, ".info");
                _configuration.SaveAsFile(infoFile);

                // safe preference file
                SystemSettings settings = new SystemSettings();
                settings.SaveOrCreate(_configuration);

                try
                {
                    _usedDeviceVcp.Port.CloseIt();
                    _usedDeviceVcp.Port.DiscardInBuffer();
                    _usedDeviceVcp.Rxbuffer.Complete();
                    _usedDeviceVcp.Close();
                    _usedDeviceVcp.Port.Dispose();
                    Thread.Sleep(300);
                }
                catch { }
                DialogResult = true;
                }
        }
        private void OnSaveAsButtonClicked()
        {
            Globals.Log.Info("SAVE AS BUTTON CLICKED.");
            

            //link blockId in storage data to each record.
            int dataPointsPerBlock = _stimuliPerBlock * _configuration.NumberOfSamplesPerAcousticStimulus;
            int id = 0;
            for (int i = 0; i < _storageData.Count; i++)
            {
                _storageData[i].BlockId = id;
                if ((i % dataPointsPerBlock == 0) && (i > 0))
                {
                    id++;
                }
            }

            //save summary file
            _configuration.TotalNumberOfAquiredBlocks = _counterBlocks;
            _configuration.TotalNumberOfAquiredDataFrames = _storageData.Count();
            _configuration.TotalNumberOfAquiredEvents = _storageData[_storageData.Count() - 1].EventId + 1;
            _configuration.SetValueSamplesPerDataFrame = _configuration.NumberOfSamplesPerAcousticStimulus;
            _configuration.DateTimeOfExperiment = DateTime.Now;

            // safe preference file
            SystemSettings settings = new SystemSettings();
            settings.SaveOrCreate(_configuration);

            SaveAsView saveAsDialog = new SaveAsView();
            ((SaveAsViewModel)saveAsDialog.DataContext).StorageData = _storageData;
            ((SaveAsViewModel)saveAsDialog.DataContext).ExperimentConfig = _configuration;
            bool? res = saveAsDialog.ShowDialog();
            if (res ?? false)
            {
                DialogResult = true;
            }
        }

            ///=================================================================================================
            /// <summary>Executes the 'cancel button clicked' action.
            ///          Cancel button was clicked. The data will disgarded,
            ///          the window will be closed without writing any data to the device,
            ///          the preference file or aquired data. Thread
            ///          will be set to sleep for 300 ms due to some issues when disposing a COM
            ///          port object.</summary>
            ///
            /// <remarks>Bernhard Gleich, 09.04.2023.</remarks>
            ///=================================================================================================
            private void OnCancelButtonClicked()
             {
            Globals.Log.Info("CANCEL BUTTON CLICKED.");
            try
            {
                if (_usedDeviceVcp.IsOpen == true)
                {
                    _usedDeviceVcp.Port.CloseIt();
                    _usedDeviceVcp.Port.DiscardInBuffer();
                    _usedDeviceVcp.Rxbuffer.Complete();
                    _usedDeviceVcp.Close();
                    _usedDeviceVcp.Port.Dispose();
                    Thread.Sleep(300);
                }
            }
            catch { }
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
            int counter = 0;
            
            while (await _usedDeviceVcp.Rxbuffer.OutputAvailableAsync(_ct))
            {
                byte[] data = await _usedDeviceVcp.Rxbuffer.ReceiveAsync(_ct);
                if (counter == _configuration.NumberOfSamplesPerAcousticStimulus)
                {
                    timestep = 0.0;
                    counter = 0;
                }
                dataStream.AddRange(data);
                if (dataStream.Count >= hw.RxProtocolLength)
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
                    dataModel.XAccelerationInMeterPerSecondSquare = _hw.GetAccerlerationInMeterPerSecondSquare(val, _configuration.Range);

                    v = new byte[] { dataStream[14], dataStream[15], dataStream[16], dataStream[17] };
                    Array.Reverse(v);
                    val = BitConverter.ToUInt32(v, 0);
                    dataModel.YRawData = val;
                    dataModel.YAccelerationInMeterPerSecondSquare = _hw.GetAccerlerationInMeterPerSecondSquare(val, _configuration.Range);

                    v = new byte[] { dataStream[18], dataStream[19], dataStream[20], dataStream[21] };
                    Array.Reverse(v);
                    val = BitConverter.ToUInt32(v, 0);
                    dataModel.ZRawData = val;
                    dataModel.ZAccelerationInMeterPerSecondSquare = _hw.GetAccerlerationInMeterPerSecondSquare(val, _configuration.Range);

                    dataModel.TimeInSec = timestep;

                    Produce(_dataBufferBlock, dataModel);
                    counter++;
                    timestep = timestep + (double)(1.0 / _configuration.OutputDataRate);
                    dataStream.RemoveRange(0, hw.RxProtocolLength);
                }
            }
        }
        /// <summary>
        /// This task consumes the received data from the COM port. The method will pack the data
        /// into data tuples and store each tuple in a list object. The data will also be stored in the 
        /// "_storageData" List for the possibility of saving the data to the disk.
        /// Once there are enough data points in the list (the number is defined by the constant value
        /// "MinimumDataSizePrioDownsampling" the method will invalidate the plots and redraw them. After 
        /// this, the tuple lists will be cleared again for the next bunch of data sets.
        /// </summary>
        /// <returns></returns>
        private async Task SignalProcessingTask()
        {
            ISourceBlock<DataModel> dataSourceBlock = _dataBufferBlock;
            List<Tuple<double, double>> absAccelerationTuples = new List<Tuple<double, double>>();
            Tuple<double, double> absTuple;

            int counter = 0;
            int blockcounter = 0;
            int bhelpercounter = 0;
            int dataPointsPerBlock = _stimuliPerBlock * _configuration.NumberOfSamplesPerAcousticStimulus;
            double abs = 0;
            while (await dataSourceBlock.OutputAvailableAsync(_ct))
            {               
                DataModel acq = await dataSourceBlock.ReceiveAsync(_ct);
                bhelpercounter++;
                if ((bhelpercounter % dataPointsPerBlock) == 0)
                {
                    blockcounter++;
                    bhelpercounter = 0;
                }
                counter++;
                acq.BlockId = blockcounter;
                _storageData.Add(acq);

                abs = acq.XAccelerationInMeterPerSecondSquare * acq.XAccelerationInMeterPerSecondSquare + 
                    acq.YAccelerationInMeterPerSecondSquare * acq.YAccelerationInMeterPerSecondSquare + 
                    acq.ZAccelerationInMeterPerSecondSquare * acq.ZAccelerationInMeterPerSecondSquare;
                abs = Math.Sqrt(abs);

                absTuple = Tuple.Create(acq.TimeInSec, abs);
                absAccelerationTuples.Add(absTuple);


                //show the complete dataset at once
                if ((counter % _configuration.NumberOfSamplesPerAcousticStimulus) == 0)  //MinimumDataSizePrioDownsampling
                {
                    DrawPoints(AccelerationPlotModel, absAccelerationTuples);
                    absAccelerationTuples.Clear();
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


        #endregion

    }

}
