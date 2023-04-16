using Accelera.Hardware;
using Accelera.Models;
using MFE.Communication;
using MicroMvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Accelera.ViewModels
{
    /// <summary>
    /// Viewmodel for the connection dialog. 
    /// </summary>
    public class ConnectDialogViewModel : ObservableObject
    {
        private hw _hw;
        private List<ComPortList> _portList;


        /// <summary>
        /// This property is needed to bind the DialogResult of the window. See here for details:
        /// https://stackoverflow.com/questions/501886/how-should-the-viewmodel-close-the-form/3329467#3329467
        /// </summary>
        private bool? _dialogResult;
        private ObservableCollection<string> _devices;
        private ConnectDialogModel _data;
        private int _selectedDeviceIdx = -1;
        private bool _isEnabledOkButton;

        public bool? DialogResult
        {
            get => _dialogResult;
            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }

        public ObservableCollection<string> Devices
        {
            get => _devices;
            set => _devices = value;
        }

        public ConnectDialogModel Data => _data;

        public int SelectedDeviceIdx
        {
            get => _selectedDeviceIdx;
            set
            {
                _selectedDeviceIdx = value;
                _selectedDeviceIdx = value;
                if (_selectedDeviceIdx > -1)
                {
                    _isEnabledOkButton = true;
                }
                else
                {
                    _isEnabledOkButton = false;
                }
                RaisePropertyChanged("SelectedDeviceIdx");
            }
        }

        public bool IsEnabledOkButton
        {
            get => _isEnabledOkButton;
            set
            {
                _isEnabledOkButton = value;
                RaisePropertyChanged("IsEnabledOkButton");
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectDialogViewModel()
        {
            _data = new ConnectDialogModel();
            _hw = new hw();
            _devices = new ObservableCollection<string>();
            _portList = new List<ComPortList>();
            Globals.Log.Wpf("== ConnectDialogViewModel ==");
            CreatePortList();
        }

        #region Commands
        public ICommand OkButtonClicked { get { return new RelayCommand(OnOkButtonClicked, CanOkButtonBeExecuted); } }
        public ICommand CancelButtonClicked { get { return new RelayCommand(OnCancelButtonClicked, CanCancelButtonBeExecuted); } }

        bool CanOkButtonBeExecuted()
        {
            return _isEnabledOkButton;
        }
        bool CanCancelButtonBeExecuted()
        {
            return true;
        }

        /// <summary>
        /// Ok button was clicked. All relevant data will be collected and passed to the configuration object Config.
        /// The window will be closed.
        /// </summary>
        private void OnOkButtonClicked()
        {
            _data.ConnectionString = _portList[_selectedDeviceIdx].ComPortName;
            Globals.Log.Info("Connected to "+_data.ConnectionString);
            DialogResult = true;
        }


        /// <summary>
        /// Cancel button was clicked. The configuration object Config is set to null, the window will be closed.
        /// </summary>
        private void OnCancelButtonClicked()
        {
            _data = null;
            Globals.Log.Info("Connect Dialog Cancled");
            DialogResult = false;
        }
#endregion

        #region Private methods

        private void CreatePortList()
        {
            Vcp comport = new Vcp();

            _portList.Clear();
            _devices.Clear();
            SelectedDeviceIdx = -1;
            //search for existing devices
            _portList = comport.GetComPortList(hw.VID, hw.PID);
            if (_portList.Count > 0)
            {
                foreach (var c in _portList)
                {
                    _devices.Add(c.ComPortName + " / S/N: " + c.SerialNumber + " / DESC: " + c.Description);
                }
                SelectedDeviceIdx = 0;
                _isEnabledOkButton = true;
            }           
        }
        #endregion
    }
}
