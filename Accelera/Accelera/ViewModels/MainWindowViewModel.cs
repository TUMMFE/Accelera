using Accelera.Hardware;
using Accelera.Models;
using Accelera.Views;
using CsvHelper;
using MFE.Communication;
using MicroMvvm;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using System.Windows.Markup;
using MathNet.Numerics;

namespace Accelera.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {

        #region Constants
        public const int MaximumRollover = 1000; //maximum value at which plot rollover will occur. Rollover will generate the illusion of moving time axis.
        public const int DecimationFactor = 10;  //this decimation factor is used only in the data aquisition modes
        public const int MinimumDataSizePrioDecimation = 100; //decimation will start when more data than this value is available
        public const int MaximumDataPlotOffline = 10000; //this is only used when loading large data sets from the disk to improve performance
        private const int DisposeFirstDataFrameNumbers = 50; //number of data frames which will be thrown away when stiching block wise data together
        #endregion

        #region Private Members       

        private BufferBlock<DataModel> _dataBufferBlock;    //used for producer/consumer model
        private List<DataModel> _storageData;               //used for saving data to disk
        private Vcp _usedDeviceVcp;        
        private hw _hw;
        private CancellationTokenSource _ts = new CancellationTokenSource();
        private CancellationToken _ct;
        private ConnectDialogModel _connectModel;
        private ConfigurationModel _configuration;

        private string _fileNameSave = string.Empty;
        private string _fileNameOpen = string.Empty;
        List<DataModel> _openData = new List<DataModel>();

        private bool _isRunning;
        private bool _isConnected;
        private bool _isDataAvailable;
        private bool _isRunningAcoustic;
        private bool _isRunningExternal;

        private bool _dataSavingFinished = false;

        private ProgressDialog _saveProgressDialog = new ProgressDialog() 
        {
            WindowTitle = "Save data",
            Text = "Saving aquired data on hard disk ...",
            Description = "Processing...",
            ShowTimeRemaining = true,            
            CancellationText = "Saving canceled. Datafile not written completly.",
        };

        private ProgressDialog _openProgressDialog = new ProgressDialog()
        {
            WindowTitle = "Open data file",
            Text = "Loading data from disk ...",
            Description = "Processing...",
            ShowTimeRemaining = true,
            CancellationText = "Loading canceled. Datafile will not be shown.",
        };        
        #endregion

        #region Public Members 
        public PlotModel AccelerationPlotModel { get; set; }

        #endregion

        #region Constructors

        ///=================================================================================================
        /// <summary>Constructor of the view model. The constructor will init all plot models, all UI
        /// control flags and will init a standard configuration for settings.</summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///=================================================================================================

        public MainWindowViewModel()
        {
            //This will break strict MVVM pattern but other solutions seems to be more complicated
            //https://stackoverflow.com/questions/3683450/handling-the-window-closing-event-with-wpf-mvvm-light-toolkit
            Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);

            _hw = new hw();              
            _usedDeviceVcp = new Vcp();
            _isRunning = false;
            _isConnected = false;
            _isDataAvailable = false;
            _isRunningAcoustic = false;
            _isRunningExternal = false;
            _connectModel = new ConnectDialogModel();
            _configuration = new ConfigurationModel();

            AccelerationPlotModel = new PlotModel();


            _dataBufferBlock = new BufferBlock<DataModel>();
            _storageData = new List<DataModel>();

            //here we shall load some standard settings from the disk                           
            SystemSettings systemSettings = new SystemSettings();
            _configuration = systemSettings.LoadOrCreate();

            _ct = _ts.Token;

            AccelerationPlotModel.Series.Add(new LineSeries() { Color = OxyColors.DodgerBlue, InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline });
            AccelerationPlotModel.Series.Add(new LineSeries() { Color = OxyColors.DodgerBlue, InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline });
            AccelerationPlotModel.Series.Add(new LineSeries() { Color = OxyColors.DodgerBlue, InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline });
            PlotAxisFormatting(AccelerationPlotModel, "Acceleration", "time in s", "acceleration in m/s^2");

            _saveProgressDialog.DoWork += new DoWorkEventHandler(SaveProgressDialogDoWork);
            _saveProgressDialog.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SaveProgressDialogCompleted);
            _openProgressDialog.DoWork += new DoWorkEventHandler(OpenProgressDialogDoWork);
            _openProgressDialog.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OpenProgressDialogCompleted);
            Globals.Log.Wpf("== MainWindowViewModel ==");
        }
       
        #endregion

        #region Member Methods

        ///=================================================================================================
        /// <summary>This method will be executed when the OnClosing event happen although it breaks the
        /// strict MVVM pattern.</summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///
        /// <param name="sender">.</param>
        /// <param name="e">     .</param>
        ///=================================================================================================

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //If your app is a "portable app" without an installer, consider calling this method upon app
            //exit unless you have notifications that are meant to persist after your app is closed.
            //The uninstall method will clean up any scheduled and current notifications, remove any associated
            //registry values, and remove any associated temporary files that were created by the library.
            //https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop
            ToastNotificationManagerCompat.Uninstall();
            _usedDeviceVcp.Close();
            Globals.Log.Info("Application closed.");
        }

        ///=================================================================================================
        /// <summary>Draw, axis, grid lines, labels and titels of a plot model.</summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///
        /// <param name="pm">   Plotmodel to be modified.</param>
        /// <param name="title">Title of the plot.</param>
        /// <param name="xaxis">Label for the x-axis.</param>
        /// <param name="yaxis">Label for the y-axis.</param>
        ///=================================================================================================

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

        ///=================================================================================================
        /// <summary>Send the system configuration and the experimental setup data from the settings panel.</summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///=================================================================================================

        private void SendSystemConfiguration()
        {
            if (_usedDeviceVcp.IsOpen == false)
            {
                _usedDeviceVcp.Open(_connectModel.ConnectionString, 921600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

            }

            _usedDeviceVcp.Port.Write(_hw.SetAcousticParameters((short)_configuration.NumberOfSamplesPerAcousticStimulus,
                                                                (short)_configuration.FrequencyOfAcousticStimulusInMillihertz,
                                                                (short)_configuration.DurationOfEventInSeconds,
                                                                (short)_configuration.DurationOfAcousticStimulusInMilliSeconds), 0, hw.TxProtocolLength);
            Thread.Sleep(300);

            _usedDeviceVcp.Port.Write(_hw.SetTriggerModeParameters((short)_configuration.NumberOfSamplesPerTriggerEvent,
                                                                   (byte)_configuration.TriggerPolarity), 0, hw.TxProtocolLength);
            Thread.Sleep(300);

            byte selectedOutputDataRateIdx = 0;
            byte selectedRangeIdx = 0;
            for (int i = 0; i < _hw.PossibleOutputDataRates.Count; i++)
            {
                if ((int)_hw.ExtractDecimalFromString(_hw.PossibleOutputDataRates[i]) == (int)_configuration.OutputDataRate)
                {
                    selectedOutputDataRateIdx = (byte)i;
                }
            }

            for (int i = 0; i < _hw.PossibleRanges.Count; i++)
            {
                if ((int)_hw.ExtractDecimalFromString(_hw.PossibleRanges[i]) == _configuration.Range)
                {
                    selectedRangeIdx = (byte)i;
                }
            }



            _usedDeviceVcp.Port.Write(_hw.SetAdxlParameters(_configuration.HighpassFilterFrequency,
                                                            selectedOutputDataRateIdx,
                                                            selectedRangeIdx,
                                                            _configuration.ActivityCounts), 0, hw.TxProtocolLength);
            Thread.Sleep(300);
            
            _usedDeviceVcp.Port.Write(_hw.SetAdxlOffset((short)_configuration.XOffset,
                                                        (short)_configuration.YOffset,
                                                        (short)_configuration.ZOffset,
                                                        (short)_configuration.ActivityThreshold), 0, hw.TxProtocolLength);
            Thread.Sleep(300);


        }

        ///=================================================================================================
        /// <summary>Prepare the plot panels for the free running data aquisition and send the start
        /// command to the device.</summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///=================================================================================================

        private void PrepareFreeRunningDataAquisition()
        {
            _ts = new CancellationTokenSource();
            _ct = _ts.Token;
            _usedDeviceVcp.Rxbuffer.Complete();
            _usedDeviceVcp.Restart();

            PreparePlot(AccelerationPlotModel, "time in s", "acceleration in m/s^2");

            _usedDeviceVcp.Port.StartListening();           

            //DAQ in free running mode 0x03 and start it 0x00
            _usedDeviceVcp.Port.Write(_hw.SetOpMode(0x03, 0x00), 0, hw.TxProtocolLength);
        }

        ///=================================================================================================
        /// <summary>Prepare plot.
        ///          This method will remove all previous data series, add the legends and three new data 
        ///          series for the three acceleration directions. The method will add the labes for the 
        ///          two axes of the plot.</summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///
        /// <param name="pm">   Plotmodel to be modified.</param>
        /// <param name="xaxis">Label for the x-axis.</param>
        /// <param name="yaxis">Label for the y-axis.</param>
        ///=================================================================================================

        private void PreparePlot(PlotModel pm, string xaxis, string yaxis)
        {
            for (int i = 0; i< pm.Series.Count;i++)
            {
                (pm.Series[i] as LineSeries).Points.Clear();
            }
            pm.Series.Clear();
            pm.InvalidatePlot(true);
            
            pm.Legends.Add(new Legend()
            {
                LegendTitle = "Direction",
                LegendPosition = LegendPosition.LeftBottom                                
            });

            pm.Series.Add(new LineSeries() { Color = OxyColor.FromRgb(43, 138, 128), Title = "x-direction" });
            pm.Series.Add(new LineSeries() { Color = OxyColor.FromRgb(100, 20, 120), Title = "y-direction" });
            pm.Series.Add(new LineSeries() { Color = OxyColor.FromRgb(191, 115, 28), Title = "z-direction" });

            pm.Axes[0].Title = xaxis;
            pm.Axes[1].Title = yaxis;
        }

        ///=================================================================================================
        /// <summary>Producer thread to collect the received and converted raw data.</summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///
        /// <param name="target">.</param>
        /// <param name="data">  data set which should be plotted.</param>
        ///
        /// ### <param name="e">.</param>
        ///=================================================================================================

        private void Produce(ITargetBlock<DataModel> target, DataModel data)
        {
            target.Post(data);
        }

        ///=================================================================================================
        /// <summary>Plot data.
        ///          The method is used in free running mode only. The method will be called by the
        ///          'SignalProcessingTask'. The method will interpolate and decimate the data using
        ///          'LargestTriangleThreeBuckets' method, add the data points to the corresponding
        ///          series. In case of a rollover, the previous data will be removed from the plot 
        ///          series and finally the plot will be invalidted. </summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///
        /// <param name="pm">   Plotmodel to be modified.</param>
        /// <param name="xData">The data.</param>
        /// <param name="yData">The data.</param>
        /// <param name="zData">The data.</param>
        ///=================================================================================================

        public void PlotData(PlotModel pm, List<Tuple<double, double>> xData, List<Tuple<double, double>> yData, List<Tuple<double, double>> zData)
        {
            int Threshold = MinimumDataSizePrioDecimation / DecimationFactor;
            IEnumerable<Tuple<double, double>> xInterpolated = LargestTriangleThreeBuckets(xData, Threshold);
            IEnumerable<Tuple<double, double>> yInterpolated = LargestTriangleThreeBuckets(yData, Threshold);
            IEnumerable<Tuple<double, double>> zInterpolated = LargestTriangleThreeBuckets(zData, Threshold);
            foreach (Tuple<double, double> item in xInterpolated)
            {
                (pm.Series[0] as LineSeries).Points.Add(new DataPoint(item.Item1, item.Item2));
            }
            foreach (Tuple<double, double> item in yInterpolated)
            {
                (pm.Series[1] as LineSeries).Points.Add(new DataPoint(item.Item1, item.Item2));
            }
            foreach (Tuple<double, double> item in zInterpolated)
            {
                (pm.Series[2] as LineSeries).Points.Add(new DataPoint(item.Item1, item.Item2));
            }

            if ((pm.Series[0] as LineSeries).Points.Count > MaximumRollover) //show only some last points
            {
                (pm.Series[0] as LineSeries).Points.RemoveRange(0, xInterpolated.Count()); //remove first point
                (pm.Series[1] as LineSeries).Points.RemoveRange(0, yInterpolated.Count()); //remove first point
                (pm.Series[2] as LineSeries).Points.RemoveRange(0, zInterpolated.Count()); //remove first point
            }
            pm.InvalidatePlot(true);
        }

        ///=================================================================================================
        /// <summary>Reads additional information.
        ///          Check if the *.info file exists. If the file exists read all necessary information to
        ///          convert the time vector (which is event wise saved in the data file, which means that
        ///          it will start at zero for each event) into a continous time vector with increasing
        ///          times. Thus, a full plot with all pauses and events can be plotted. The method will 
        ///          also call 'ReadTimestampFile' to get pause times. The method will remove all data 
        ///          frames where the raw values of x,y,z acceleration are 0. Before stiching the method 
        ///          will throw the first 'DisposeFirstDataFrameNumbers' data frames away (to avoid some
        ///          transients).
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///=================================================================================================

        private void ReadAdditonalInformation()
        {
            string infoFileName = Path.ChangeExtension(_fileNameOpen, ".info");
            if (File.Exists(infoFileName) == false)
            {

                Globals.Log.Info("No info file available: " + infoFileName);
            }
            else
            {
                //a info file is available 
                Globals.Log.Info("Info file available: " + infoFileName);
            }
            try
            {
                //look for a time stamp file to create a real time vector of the experiment
                List<TimeMarks> tm = new List<TimeMarks>();
                tm = ReadTimestampFile();
                if (tm.Count > 0)
                {
                //1. find number of samples of the first event
                //start with the first sample number 0 and count until the next sample number is zero again
                // thus, find the index of the second 0 in the sample id column, we expect the second zero at a
                // index of a few thousend, thus sorting like in 
                // https://stackoverflow.com/questions/52446285/get-indexof-second-int-record-in-a-sorted-list-in-c-sharp
                // might be too slow.
                
                    int cnt = 1;
                    while (cnt < _openData.Count)
                    {
                        if (_openData[cnt].SampleId != 0)
                        {
                            cnt++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    int numberOfSamplesPerEvent = cnt;
                    double timestep = _openData[1].TimeInSec;       //time step which is 1/output datarate
                    List<TimeMarks> daqTimeMarks = tm.Where(x => x.Type == "DAQ Started").ToList();
                    daqTimeMarks.RemoveAt(0); //delete first element because this is always at time 0

                    for (int i = 0; i < _openData.Count; i++)
                    {
                        _openData[i].TimeInSec = _openData[i].TimeInSec + _openData[i].EventId * timestep;
                    }

                    //2. include the DAQ start times
                    for (int i = 0; i < daqTimeMarks.Count; i++)
                    {
                        for (int j = 0; j < _openData.Count; j++)
                        {
                            if (j / numberOfSamplesPerEvent > i)
                            {
                                _openData[j].TimeInSec = _openData[j].TimeInSec + daqTimeMarks[i].RelativeTimeStamp - _openData[i * numberOfSamplesPerEvent].TimeInSec;
                            }
                        }
                    }
                    _openData.RemoveAll(s => s.SampleId <= DisposeFirstDataFrameNumbers);

                } else
                {
                    //there are not saved time marks so we need to create continous time vector to improve plotting performance
                    double timestep = _openData[1].TimeInSec;       //time step which is 1/output datarate
                    for (int i =0; i< _openData.Count; i++)
                    {
                        _openData[i].TimeInSec =  i*timestep;
                    }

                }
                
                //remove all useles data values
                _openData.RemoveAll(s => (s.XRawData == 0 || s.YRawData == 0 || s.ZRawData == 0 || s.TempRawData == 0));
            }
            catch
            {
                MessageBox.Show("Wrong file format of info file.", "Error");
                Globals.Log.Warn("Wrong info file format.");
            }                       
        }

        ///=================================================================================================
        /// <summary>Reads time stamp file.
        ///          Check if the *.time file exists. If the file exists read the content into a list object
        ///          of type TimeMark.
        ///          </summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///
        /// <returns>A list of time marks, which will be used to convert the time vector of the data
        ///          file into a continous time frame to see all pauses, block and events when plotting
        ///          the data.
        ///          </returns>
        ///=================================================================================================

        private List<TimeMarks> ReadTimestampFile()
        {
            List<TimeMarks> timeMarks = new List<TimeMarks>();
            string timestepsFileName = Path.ChangeExtension(_fileNameOpen, ".time");
            if (File.Exists(timestepsFileName) == false)
            {

                Globals.Log.Info("No time vector file available: " + timestepsFileName);
            }
            else
            {
                using (var reader = new StreamReader(timestepsFileName))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<TimeMarks>();
                    timeMarks.AddRange(records);
                }
            }
            return timeMarks;
        }

        ///=================================================================================================
        /// <summary>Decimate and plot data file.
        ///          The method creats lists of data tuples for the interpolation method 
        ///          ('LargestTriangleThreeBuckets') and calculates the absolute value of the acceleration
        ///          as well. The method will set the labeling of the axes (depending on which data to be 
        ///          ploted), add the line series to the plot model and finally will add the data points
        ///          to the corresponding series. 
        ///          At the end the method will invalidate the plotmodel.</summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///
        /// <param name="data">data set which should be plotted</param>
        /// <param name="decimationFactor">Decimation factor of the data set</param>
        /// <param name="plotAbsoluteValueOnly">True to plot absolute value only (false will plot all 
        ///                                      three axes but not the absolute value.</param>
        ///=================================================================================================

        private void DecimateAndPlotDataFile(List<DataModel> data, int decimationFactor, bool plotAbsoluteValueOnly)
        {
            int thr = data.Count / decimationFactor; //this is the number of date which shall be returned by the interpolation

            //1. create data tuples

            List<Tuple<double, double>> tupleListX = new List<Tuple<double, double>>();
            List<Tuple<double, double>> tupleListY = new List<Tuple<double, double>>();
            List<Tuple<double, double>> tupleListZ = new List<Tuple<double, double>>();
            List<Tuple<double, double>> tupleListAbs = new List<Tuple<double, double>>();
            Tuple<double, double> tupleX;
            Tuple<double, double> tupleY;
            Tuple<double, double> tupleZ;
            Tuple<double, double> tupleAbs;

            foreach (var d in data)
            {
                tupleListX.Add(Tuple.Create(d.TimeInSec, d.XAccelerationInMeterPerSecondSquare));
                tupleListY.Add(Tuple.Create(d.TimeInSec, d.YAccelerationInMeterPerSecondSquare));
                tupleListZ.Add(Tuple.Create(d.TimeInSec, d.ZAccelerationInMeterPerSecondSquare));
                double abs = d.XAccelerationInMeterPerSecondSquare * d.XAccelerationInMeterPerSecondSquare + d.YAccelerationInMeterPerSecondSquare * d.YAccelerationInMeterPerSecondSquare + d.ZAccelerationInMeterPerSecondSquare * d.ZAccelerationInMeterPerSecondSquare;
                abs = Math.Sqrt(abs);
                tupleListAbs.Add(Tuple.Create(d.TimeInSec, abs));
            }

            //2. delete and clear all existing data points and line series
            for (int i = 0; i < AccelerationPlotModel.Series.Count; i++)
            {
                (AccelerationPlotModel.Series[i] as LineSeries).Points.Clear();
            }
            AccelerationPlotModel.Series.Clear();
            AccelerationPlotModel.InvalidatePlot(true);

            if (plotAbsoluteValueOnly == true)
            {
                AccelerationPlotModel.Legends.Add(new Legend()
                {
                    LegendTitle = "Direction",
                    LegendPosition = LegendPosition.LeftBottom
                });

                AccelerationPlotModel.Axes[0].Title = "time in s";
                AccelerationPlotModel.Axes[1].Title = "acceleration in m/s^2";
                AccelerationPlotModel.Series.Add(new LineSeries() { Color = OxyColor.FromRgb(43, 138, 128), Title = "absolut value (|a|)" });

                IEnumerable<Tuple<double, double>> absInterpolated = LargestTriangleThreeBuckets(tupleListAbs, thr);
                foreach (var item in absInterpolated)
                {
                    (AccelerationPlotModel.Series[0] as LineSeries).Points.Add(new DataPoint(item.Item1, item.Item2));
                }
            }
            else
            {
                PreparePlot(AccelerationPlotModel, "time in s", "acceleration in m/s^2");

                IEnumerable<Tuple<double, double>> xInterpolated = LargestTriangleThreeBuckets(tupleListX, thr);
                foreach (var item in xInterpolated)
                {
                    (AccelerationPlotModel.Series[0] as LineSeries).Points.Add(new DataPoint(item.Item1, item.Item2));
                }

                IEnumerable<Tuple<double, double>> yInterpolated = LargestTriangleThreeBuckets(tupleListY, thr);
                foreach (var item in yInterpolated)
                {
                    (AccelerationPlotModel.Series[1] as LineSeries).Points.Add(new DataPoint(item.Item1, item.Item2));
                }

                IEnumerable<Tuple<double, double>> zInterpolated = LargestTriangleThreeBuckets(tupleListZ, thr);
                foreach (var item in zInterpolated)
                {
                    (AccelerationPlotModel.Series[2] as LineSeries).Points.Add(new DataPoint(item.Item1, item.Item2));
                }
            }
            AccelerationPlotModel.InvalidatePlot(true);
        }

        ///=================================================================================================
        /// <summary>Plot existing data.
        ///          This method is used for plotting the loaded data. It will open a dialog window in which
        ///          the use can chose either to plot all 3-directions or only the absolut value of the 
        ///          acceleration. The method will call the 'DecimateAndPlotDataFile' method which does all
        ///          the plotting.</summary>
        ///
        /// <remarks>Bernhard Gleich, 19.04.2023.</remarks>
        ///=================================================================================================

        private void PlotExistingData()
        {
            int decimationFactor = _openData.Count() / MaximumDataPlotOffline + 1;
            using (TaskDialog dialog = new TaskDialog())
            {
                dialog.WindowTitle = "Open data file";
                dialog.MainInstruction = "What do you want to see?";
                dialog.Content = "You can chose between the plot of all three different acceleration axes or the absolute acceleration. The absolut value of the acceleration will give you some information about the value, where as the 3-axis plot will show the direction.";
                dialog.ExpandedInformation = "All plots will show all raw data points without any signal processing except for the first 50 data points of each event which will be deleted to avoid transients. Also, all data points with zero acceleration will be removed for plotting.";
                dialog.Footer = "User our Python scripts for detailed data anaylsis. You know who you gonna call...";
                dialog.FooterIcon = TaskDialogIcon.Information;
                dialog.EnableHyperlinks = true;
                TaskDialogButton threeAxes = new TaskDialogButton("3-Axes");
                TaskDialogButton absolutValue = new TaskDialogButton("|a| only");
                dialog.Buttons.Add(absolutValue);
                dialog.Buttons.Add(threeAxes);
                TaskDialogButton button = dialog.ShowDialog();
                if (button == threeAxes)
                {
                    Globals.Log.Info("Plot all three axes.");
                    DecimateAndPlotDataFile(_openData, decimationFactor, false);
                }
                else if (button == absolutValue)
                {
                    Globals.Log.Info("Plot absolut value only.");
                    DecimateAndPlotDataFile(_openData, decimationFactor, true);
                }
            }
        }

        #endregion

        #region Relay Commands
        /// <summary>
        /// The following ICommand link the UI with the ViewModel.
        /// </summary>
        public ICommand SaveButtonClicked { get { return new RelayCommand(OnSaveButtonClicked, CanSaveBeExecuted); } }
        public ICommand OpenButtonClicked { get { return new RelayCommand(OnOpenButtonClicked, CanOpenBeExecuted); } }
        public ICommand ConnectButtonClicked { get { return new RelayCommand(OnConnectButtonClicked, CanConnectBeExecuted); } }
        public ICommand SetupButtonClicked { get { return new RelayCommand(OnSetupButtonClicked, CanSetupBeExecuted); } }
        public ICommand StartFreeRunningModeButtonClicked { get { return new RelayCommand(OnRunFreeButtonClicked, CanRunFreeBeExecuted); } }
        public ICommand StartAcousticModeButtonClicked { get { return new RelayCommand(OnRunAcousticButtonClicked, CanRunAcousticBeExecuted); } }
        public ICommand StartExternalTriggerModeButtonClicked { get { return new RelayCommand(OnRunExtiButtonClicked, CanRunExtiBeExecuted); } }
        public ICommand StopButtonClicked { get { return new RelayCommand(OnStopButtonClicked, CanStopBeExecuted); } }

        public ICommand AboutButtonClicked { get { return new RelayCommand(OnAboutButtonClicked, CanAboutBeExecuted); } }

        #endregion

        #region Enable Control of Buttons
        /// <summary>
        /// The save button can only be used if there is some data available to be saved
        /// </summary>
        /// <returns></returns>
        bool CanSaveBeExecuted()
        {
            return (_isDataAvailable & !_isRunning & !_isRunningAcoustic &!_isRunningExternal);
        }
        /// <summary>
        /// The open button can only be used if the data aquisistion is not running
        /// </summary>
        /// <returns></returns>
        bool CanOpenBeExecuted()
        {
            return (!_isRunning & !_isRunningAcoustic & !_isRunningExternal);
        }
        /// <summary>
        /// The connection button can only be used if the data aquisistion is not running
        /// </summary>
        /// <returns></returns>
        bool CanConnectBeExecuted()
        {
            return (!_isRunning & !_isRunningAcoustic & !_isRunningExternal);
        }
        /// <summary>
        /// The setup button can only be used if the data aquisistion is not running and if
        /// a device is connected.
        /// </summary>
        /// <returns></returns>
        bool CanSetupBeExecuted()
        {
            return (!_isRunning & !_isRunningAcoustic & !_isRunningExternal & _isConnected);
        }
        /// <summary>
        /// The free running button can only be used if the data aquisistion is not running and if
        /// a device is connected.
        /// </summary>
        /// <returns></returns>
        bool CanRunFreeBeExecuted()
        {
            return (!_isRunning & !_isRunningAcoustic & !_isRunningExternal & _isConnected);
        }
        /// <summary>
        /// The acoustic stimulation button can only be used if the data aquisistion is not running and if
        /// a device is connected.
        /// </summary>
        /// <returns></returns>
        bool CanRunAcousticBeExecuted()
        {
            return (!_isRunning & !_isRunningAcoustic & !_isRunningExternal & _isConnected);
        }
        /// <summary>
        /// The external trigger button can only be used if the data aquisistion is not running and if
        /// a device is connected.
        /// </summary>
        /// <returns></returns>
        bool CanRunExtiBeExecuted()
        {
            return (!_isRunning & !_isRunningAcoustic & !_isRunningExternal & _isConnected);
        }
        /// <summary>
        /// The setup button can only be used if the data aquisistion is running and if
        /// a device is connected.
        /// </summary>
        /// <returns></returns>
        bool CanStopBeExecuted()
        {
            return ((_isRunning | _isRunningAcoustic | _isRunningExternal) & _isConnected);
        }

        private bool CanAboutBeExecuted()
        {
            return (!_isRunning & !_isRunningAcoustic & !_isRunningExternal);
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
                foreach (var item in _storageData)
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
                        progress = cnt / _storageData.Count;

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
                Globals.Log.Info("Saving data file finished.");
                MessageBox.Show("Saving of data file finished.", "Information");
            }

        }
        private void OpenProgressDialogDoWork(object sender, DoWorkEventArgs e)
        {
            int percent = 0;
            int previous = 0;

            using (var reader = new StreamReader(_fileNameOpen))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                while (csv.Read() && (_openProgressDialog.CancellationPending == false))
                {
                    _openData.Add(csv.GetRecord<DataModel>());
                    double progress = (double)reader.BaseStream.Position / reader.BaseStream.Length;
                    percent = (int)(progress * 100.0);
                    if (percent > previous)
                    {
                        // slow down the report progress to see animation bar
                        _openProgressDialog.ReportProgress(percent, null, string.Format(System.Globalization.CultureInfo.CurrentCulture, "Processing: {0}%", percent));
                        previous = percent;
                    }
                }
                if (_openProgressDialog.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }
        private void OpenProgressDialogCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                Globals.Log.Info("Open data file cancelled.");
                MessageBox.Show("Open data file was cancelled.", "Information");
            }
            else
            {
                ReadAdditonalInformation();                
                PlotExistingData();
            }
        }
        #endregion

        #region Button Clicked Methods
        /// <summary>
        /// When the save button was clicked, the user will be asked for
        /// a filename (using the SaveFileDialog), and afterwards the 
        /// data will be written into that CSV file. Data can only be saved
        /// when some data is available.
        /// </summary>
        private void OnSaveButtonClicked() 
        {
            Globals.Log.Wpf("SAVE BUTTON CLICKED.");
            SaveFileDialog sdialog = new SaveFileDialog();
           

            //remove not valid data
            _storageData.RemoveAll(s => (s.XRawData == 0 || s.YRawData == 0 || s.ZRawData == 0 || s.TempRawData == 0));

            sdialog.Filter = "CSV File (*.csv)|*.csv";
            if (sdialog.ShowDialog() == true)
            {                
                _fileNameSave = sdialog.FileName;
                _saveProgressDialog.Show();
            }
        }
        
        private void OnOpenButtonClicked()
        {                       
            Globals.Log.Wpf("OPEN BUTTON CLICKED.");
            OpenFileDialog sdialog = new OpenFileDialog();

            sdialog.Filter = "CSV File (*.csv)|*.csv";
            if (sdialog.ShowDialog() == true)
            {
                _openData.Clear();
                _fileNameOpen = sdialog.FileName;
                //1. Read the data file
                Globals.Log.Info("Reading File: " + _fileNameOpen);
                
                _openProgressDialog.Show();                                
            }
        }
      
       
        /// <summary>
        /// When the user has clicked the connect button a dialog apears. In this dialog
        /// the user can select a device from a list and connect to this device or cancel
        /// the dialog. In either case a toast notification is shown.
        /// In case of success the configuration from the preference file will be send
        /// to the device.
        /// 
        /// Toast Notifications:
        /// https://stackoverflow.com/questions/35910332/windows-10-notification-wpf
        /// https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=uwp
        /// </summary>
        private void OnConnectButtonClicked()
        {
            Globals.Log.Wpf("CONNECT BUTTON CLICKED.");
            if (_isRunning == false)
            {
                ///not data aquistion is running --> show the configuration dialog
                ConnectDialogView connectDialog = new ConnectDialogView();
                bool? res = connectDialog.ShowDialog();
                if (res ?? false)
                {
                    //the dialog was closed with OK
                    //_config = ((ConfigWindowViewModel)cfg.DataContext).Config;
                    _connectModel = ((ConnectDialogViewModel)connectDialog.DataContext).Data;                  
                    SendSystemConfiguration();
                    Globals.Log.Info("Standard Configuration was sent to the device.");
                    new ToastContentBuilder()
                        .AddText("Device connected to Port " + _connectModel.ConnectionString+".")
                        .Show();

                    
                    _isConnected = true;
                }
                else
                {
                    //the dialog was canceled thus, we leave the method without any action
                    new ToastContentBuilder()
                        .AddText("No device connected.")
                        .Show();

                    _isConnected = false;
                    return;
                }
            }
            else
            {
                new ToastContentBuilder()
                    .AddText("You have to stop data aquisition first.")
                    .Show();
            }
        }
        /// <summary>
        /// Will open the ConfigurationDialogView.
        /// </summary>
        private void OnSetupButtonClicked()
        {
            Globals.Log.Wpf("SETUP BUTTON CLICKED.");
            if (_usedDeviceVcp.IsOpen == true) {             
                _usedDeviceVcp.Port.CloseIt();
                _usedDeviceVcp.Port.DiscardInBuffer();
                _usedDeviceVcp.Rxbuffer.Complete();
                _usedDeviceVcp.Close();
                _usedDeviceVcp.Port.Dispose();
            }            
            
            ConfigurationDialogView configurationDialog = new ConfigurationDialogView();
            ((ConfigurationDialogViewModel)configurationDialog.DataContext).ConnectionString = _connectModel.ConnectionString;
            bool? res = configurationDialog.ShowDialog();
            if (res ?? false)
            {
                _configuration = ((ConfigurationDialogViewModel)configurationDialog.DataContext).Configuration;
                Globals.Log.Info("Preference File was changed.");
                new ToastContentBuilder()
                        .AddText("Configuration was changed!")
                        .Show();
            }
            else
            {
                new ToastContentBuilder()
                        .AddText("Configuration was not changed!")
                        .Show();
            }
            _usedDeviceVcp = new Vcp();
        }
                                   
        /// <summary>
        /// A click on this button starts the free running mode of the device. In this device
        /// data is collected continously and displayed on the plot paneel. The function will 
        /// open the COM port if necessary, prepare the plot views, starts the raw data collection task and 
        /// the signal processing task.
        /// </summary>
        private void OnRunFreeButtonClicked() 
        {
            Globals.Log.Wpf("RUN FREE BUTTON CLICKED.");
            if (_usedDeviceVcp.IsOpen == false)
            {
                _usedDeviceVcp.Open(_connectModel.ConnectionString, 921600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            }
            Globals.Log.Info("Free running mode started.");
            PrepareFreeRunningDataAquisition(); //prepare plots and start the data aquisistion
            _isRunning = true;                  //set control flag for UI binding 
            _storageData.Clear();               //clear the storage list
            DataCollectionTask();               //start data collection task
            SignalProcessingTask();             //start signal processing
        }
        private void OnRunAcousticButtonClicked() 
        {
            Globals.Log.Wpf("RUN ACOUSTIC BUTTON CLICKED.");
            if (_usedDeviceVcp.IsOpen == true)
            {
                _usedDeviceVcp.Port.CloseIt();
                _usedDeviceVcp.Port.DiscardInBuffer();
                _usedDeviceVcp.Rxbuffer.Complete();
                _usedDeviceVcp.Close();
                _usedDeviceVcp.Port.Dispose();
            }

            AcousticDialogView acousticDialog = new AcousticDialogView();
            ((AcousticDialogViewModel) acousticDialog.DataContext).ConnectionString = _connectModel.ConnectionString;
            _isRunningAcoustic = true;

            bool? res = acousticDialog.ShowDialog();
            
            _isRunningAcoustic = false;
            _usedDeviceVcp = new Vcp();

            
        }
        private void OnRunExtiButtonClicked() 
        {
            Globals.Log.Wpf("RUN EXTI BUTTON CLICKED.");
            _isRunningExternal = true;
            MessageBox.Show("Curiosity kill the cat.", "Do not do that");
            _isRunningExternal = false;            
        }
        /// <summary>
        /// A click on the stop button will stop the data aquisition. The method will send the
        /// stop command to the device, close the COM port, cancel the task and clear the RX buffers.
        /// If there was some data stored in the _storageData List, the _isDataAvailable flag will 
        /// be set, thus the UI will enable the SAVE button.
        /// </summary>
        private void OnStopButtonClicked() 
        {
             Globals.Log.Wpf("STOP BUTTON CLICKED.");
            //DAQ in free running mode 0x03 and stop it 0x01
            _usedDeviceVcp.Port.Write(_hw.SetOpMode(0x03, 0x01), 0, hw.TxProtocolLength);
            _usedDeviceVcp.Port.CloseIt();            
            _ts.Cancel();
            _ts.Dispose();
            _usedDeviceVcp.Port.DiscardInBuffer();
            _usedDeviceVcp.Rxbuffer.Complete();
            _isRunning = false;
            _isRunningAcoustic = false;
            _isRunningExternal = false;
            if (_storageData.Count != 0)
            {
                _isDataAvailable = true;
            } else
            {
                _isDataAvailable = false;
            }
        }

        private void OnAboutButtonClicked()
        {
            Globals.Log.Wpf("ABOUT BUTTON CLICKED.");
            AboutDialogView aboutDialog = new AboutDialogView();
            bool? res = aboutDialog.ShowDialog();            
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
                    byte[] v = new byte[] {0, 0, dataStream[4], dataStream[5] };
                    Array.Reverse(v);
                    dataModel.SampleId = BitConverter.ToUInt16(v, 0);
                                        
                    v = new byte[] { 0, 0, dataStream[6], dataStream[7] };
                    Array.Reverse(v);
                    dataModel.EventId = BitConverter.ToUInt16(v, 0);                   
       
                    v = new byte[] { 0, 0, dataStream[8], dataStream[9] };
                    Array.Reverse(v);
                    val = BitConverter.ToUInt16(v, 0);
                    dataModel.TempRawData = val;
                    dataModel.TemperatureInDegreeCelsius = _hw.GetTemperatureInDegreeCelsius((ushort) val);
           
                   
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

                    timestep = timestep + (double)(1.0 / (double)_configuration.OutputDataRate);
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
            List<Tuple<double, double>> tupleListTemperature = new List<Tuple<double, double>>();
            List<Tuple<double, double>> tupleListX = new List<Tuple<double, double>>();
            List<Tuple<double, double>> tupleListY = new List<Tuple<double, double>>();
            List<Tuple<double, double>> tupleListZ = new List<Tuple<double, double>>();
            Tuple<double, double> tupleTemp;
            Tuple<double, double> tupleX;
            Tuple<double, double> tupleY;
            Tuple<double, double> tupleZ;
         
            int counter = 0;
            while (await dataSourceBlock.OutputAvailableAsync(_ct))
            {
                DataModel processed = new DataModel();
                DataModel acq = await dataSourceBlock.ReceiveAsync(_ct);
                _storageData.Add(acq);
                               
                tupleX = Tuple.Create(acq.TimeInSec, acq.XAccelerationInMeterPerSecondSquare);
                tupleListX.Add(tupleX);

                tupleY = Tuple.Create(acq.TimeInSec, acq.YAccelerationInMeterPerSecondSquare);
                tupleListY.Add(tupleY);

                tupleZ = Tuple.Create(acq.TimeInSec, acq.ZAccelerationInMeterPerSecondSquare);
                tupleListZ.Add(tupleZ);

                tupleTemp = Tuple.Create(acq.TimeInSec, acq.TemperatureInDegreeCelsius);
                tupleListTemperature.Add(tupleTemp);

                counter++;

               if (counter % MinimumDataSizePrioDecimation == 0)
                {
                    PlotData(AccelerationPlotModel, tupleListX, tupleListY, tupleListZ);
                    tupleListX.Clear();
                    tupleListY.Clear();
                    tupleListZ.Clear();
                    tupleListTemperature.Clear();
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
        /// <param name="threshold">number of datapoints to be returned</param>
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

    } // end of class
} // end of namespace
