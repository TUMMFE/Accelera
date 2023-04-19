using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace Accelera.Models
{
    /// <summary>
    /// The Configuration class is used as a data model for the ConfigWindowViewModel. The class is a data template for
    /// passing information between the MainWindowView and the ConfigWindowView.
    /// </summary>
    public class ConfigurationModel
    {
        private double _outputDataRate;
        private byte _highpassFilterFrequency;
        private int _numberOfSamplesPerTriggerEvent;
        private int _numberOfSamplesPerAcousticStimulus;
        private int _frequencyOfAcousticStimulusInMillihertz;
        private int _durationOfEventInSeconds;
        private int _durationOfAcousticStimulusInMilliSeconds;
        private byte _triggerPolarity;
        private double _range;
        private byte _activityCounts;
        private int _xOffset;
        private int _yOffset;
        private int _zOffset;
        private int _activityThreshold;
        private bool _beepOnExternalTrigger;
        private string _appDataFolder;
        private string _preferencesFileName;
        private int _pauseTimeinSeconds;
        private int _blockRepetitions;
        private string _nameOfSubject;
        private string _sexOfSubject;
        private string _handednessOfSubject;
        private string _placeOfExperiment;
        private string _typeOfExperiment;
        private string _nameOfExaminer;
        private DateTime _dateTimeOfExperiment;
        private string _comments;
        private int _totalNumberOfAquirecDataFrames;
        private int _totalNumberOfAquirecEvents;
        private int _totalNumberOfAquirecBlocks;
        private int _setValueSamplesPerDataFrame;
        private List<string> _namesOfExaminers;
        private List<string> _placesOfExperiments;
        private List<string> _typesOfExperiments;
        private List<TimeMarks> _absoluteTimeMarks;


        public double OutputDataRate { get => _outputDataRate; set => _outputDataRate = value; }
        public byte HighpassFilterFrequency { get => _highpassFilterFrequency; set => _highpassFilterFrequency = value; }
        public int NumberOfSamplesPerTriggerEvent { get => _numberOfSamplesPerTriggerEvent; set => _numberOfSamplesPerTriggerEvent = value; }
        public int NumberOfSamplesPerAcousticStimulus { get => _numberOfSamplesPerAcousticStimulus; set => _numberOfSamplesPerAcousticStimulus = value; }
        public int FrequencyOfAcousticStimulusInMillihertz { get => _frequencyOfAcousticStimulusInMillihertz; set => _frequencyOfAcousticStimulusInMillihertz = value; }
        public int DurationOfEventInSeconds { get => _durationOfEventInSeconds; set => _durationOfEventInSeconds = value; }
        public int DurationOfAcousticStimulusInMilliSeconds { get => _durationOfAcousticStimulusInMilliSeconds; set => _durationOfAcousticStimulusInMilliSeconds = value; }
        public byte TriggerPolarity { get => _triggerPolarity; set => _triggerPolarity = value; }
        public double Range { get => _range; set => _range = value; }
        public byte ActivityCounts { get => _activityCounts; set => _activityCounts = value; }
        public int XOffset { get => _xOffset; set => _xOffset = value; }
        public int YOffset { get => _yOffset; set => _yOffset = value; }
        public int ZOffset { get => _zOffset; set => _zOffset = value; }
        public int ActivityThreshold { get => _activityThreshold; set => _activityThreshold = value; }
        public bool BeepOnExternalTrigger { get => _beepOnExternalTrigger; set => _beepOnExternalTrigger = value; }
        public string AppDataFolder { get => _appDataFolder; set => _appDataFolder = value; }
        public string PreferencesFileName { get => _preferencesFileName; set => _preferencesFileName = value; }
        public int PauseTimeinSeconds { get => _pauseTimeinSeconds; set => _pauseTimeinSeconds = value; }
        public int BlockRepetitions { get => _blockRepetitions; set => _blockRepetitions = value; }
        public string NameOfSubject { get => _nameOfSubject; set => _nameOfSubject = value; }
        public string SexOfSubject { get => _sexOfSubject; set => _sexOfSubject = value; }
        public string HandednessOfSubject { get => _handednessOfSubject; set => _handednessOfSubject = value; }
        public string PlaceOfExperiment { get => _placeOfExperiment; set => _placeOfExperiment = value; }
        public string TypeOfExperiment { get => _typeOfExperiment; set => _typeOfExperiment = value; }
        public string NameOfExaminer { get => _nameOfExaminer; set => _nameOfExaminer = value; }
        public DateTime DateTimeOfExperiment { get => _dateTimeOfExperiment; set => _dateTimeOfExperiment = value; }
        public string Comments { get => _comments; set => _comments = value; }
        public int TotalNumberOfAquiredDataFrames { get => _totalNumberOfAquirecDataFrames; set => _totalNumberOfAquirecDataFrames = value; }
        public int TotalNumberOfAquiredEvents { get => _totalNumberOfAquirecEvents; set => _totalNumberOfAquirecEvents = value; }
        public int TotalNumberOfAquiredBlocks { get => _totalNumberOfAquirecBlocks; set => _totalNumberOfAquirecBlocks = value; }
        public int SetValueSamplesPerDataFrame { get => _setValueSamplesPerDataFrame; set => _setValueSamplesPerDataFrame = value; }
        public List<string> NamesOfExaminers { get => _namesOfExaminers; set => _namesOfExaminers = value; }
        public List<string> PlacesOfExperiments { get => _placesOfExperiments; set => _placesOfExperiments = value; }
        public List<string> TypesOfExperiments { get => _typesOfExperiments; set => _typesOfExperiments = value; }
        public List<TimeMarks> AbsoluteTimeMarks { get => _absoluteTimeMarks; set => _absoluteTimeMarks = value; }


        

        public ConfigurationModel()
        {
            _namesOfExaminers = new List<string>();
            _placesOfExperiments = new List<string>();
            _typesOfExperiments = new List<string>();
            _absoluteTimeMarks = new List<TimeMarks>();
        }

        public ConfigurationModel(bool createStandardValues)
        {
            if (createStandardValues == true)
            {
                _namesOfExaminers = new List<string>();
                _placesOfExperiments = new List<string>();
                _typesOfExperiments= new List<string>();    
                _outputDataRate = 1000;
                _highpassFilterFrequency = 0;
                _numberOfSamplesPerTriggerEvent = 1000;
                _numberOfSamplesPerAcousticStimulus = 1000;
                _frequencyOfAcousticStimulusInMillihertz = 250; ;
                _durationOfEventInSeconds = 300; ;
                _durationOfAcousticStimulusInMilliSeconds = 200;
                _triggerPolarity = 0;
                _range = 8;
                _activityCounts = 0;
                _xOffset = 0;
                _yOffset = 0;
                _zOffset = 0;
                _activityThreshold = 0;
                _beepOnExternalTrigger = false;
                _pauseTimeinSeconds = 600;
                _blockRepetitions = 10;
                _namesOfExaminers.Add("Peter Venkman");
                _namesOfExaminers.Add("Raymond Stantz");
                _namesOfExaminers.Add("Egon Spengler");
                _placesOfExperiments.Add("Thumb, left");
                _placesOfExperiments.Add("Thumb, right");
                _typesOfExperiments.Add("ballistic thumb flexion");

            }
        }

        public void SaveAsFile(string fileName)
        {
            using (var sw = new StreamWriter(fileName))
            {
                sw.WriteLine("=== PERSONAL INFORMATION ===");
                sw.WriteLine("Subject Name: " + _nameOfSubject);
                sw.WriteLine("Examiner Name: " + _nameOfExaminer);
                sw.WriteLine("Subject Sex: " + _sexOfSubject);
                sw.WriteLine("Handedness: " + _handednessOfSubject);
                sw.WriteLine("Type of Experiment: " + _typeOfExperiment);
                sw.WriteLine("Place of Experiment: " + _placeOfExperiment);
                sw.WriteLine("Start Date/Time: " + _dateTimeOfExperiment.ToString());                
                sw.WriteLine("=== SUMMARY ===");
                sw.WriteLine("Total Number of Aquired Data Frames: " + _totalNumberOfAquirecDataFrames);
                sw.WriteLine("Total Number of Aquired Events: "+_totalNumberOfAquirecEvents);
                sw.WriteLine("Total Number of Aquired Blocks: " + _totalNumberOfAquirecBlocks);
                sw.WriteLine("=== SET VALUES ===");
                sw.WriteLine("SET VALUE - Total Number of Data Frames: "+ ((_frequencyOfAcousticStimulusInMillihertz * _durationOfEventInSeconds) / 1000) * _blockRepetitions * _setValueSamplesPerDataFrame);
                sw.WriteLine("SET VALUE - Total Number of Events: " + ((_frequencyOfAcousticStimulusInMillihertz * _durationOfEventInSeconds)/1000)* _blockRepetitions);
                sw.WriteLine("SET VALUE - Total Number of Blocks: " + _blockRepetitions);
                sw.WriteLine("=== CONFIGURATION DATA ===");
                sw.WriteLine("High Pass Filter Frequency: " + _highpassFilterFrequency);
                sw.WriteLine("Number of Samples/Trigger Event: " + _numberOfSamplesPerTriggerEvent);
                sw.WriteLine("Number of Samples/Acoustic Event: " + _numberOfSamplesPerAcousticStimulus);
                sw.WriteLine("Acoustic Stimulation rate (mHz): " + _frequencyOfAcousticStimulusInMillihertz);
                sw.WriteLine("Duration of Event (s): " + _durationOfEventInSeconds);
                sw.WriteLine("Duration of Beep (ms): " + _durationOfAcousticStimulusInMilliSeconds);
                sw.WriteLine("Trigger Polarity: " + _triggerPolarity);
                sw.WriteLine("Range: " + _range);
                sw.WriteLine("Activity Count Threshold: " + _activityCounts);
                sw.WriteLine("X-Offset (LSB): " + _xOffset);
                sw.WriteLine("Y-Offset (LSB): " + _yOffset);
                sw.WriteLine("Z-Offset (LSB): " + _zOffset);                
                sw.WriteLine("Beep on Tigger: " + _beepOnExternalTrigger);
                sw.WriteLine("Pause Time (s): " + _pauseTimeinSeconds);
                sw.WriteLine("=== COMMENTS ===");
                sw.WriteLine("Comments: " + _comments);
            }

        }

        public void SaveTimeSteps(string filename)
        {
            List<TimeMarks> time = new List<TimeMarks>();

            foreach (var t in _absoluteTimeMarks)
            {
                TimeMarks tm = new TimeMarks();
                tm.Type = t.Type;
                tm.AbsoluteTimeStamp = t.AbsoluteTimeStamp;
                tm.RelativeTimeStamp = t.AbsoluteTimeStamp - _absoluteTimeMarks[0].AbsoluteTimeStamp;
                time.Add(tm);
            }
            
            using (var writer = new StreamWriter(filename))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(time);
            }
        }
    }
}
