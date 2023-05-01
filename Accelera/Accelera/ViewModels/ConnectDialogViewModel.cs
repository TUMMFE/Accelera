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
        #region Private Members
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
        private bool _isEnabledConnectButton;
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
                if (_selectedDeviceIdx > -1)
                {
                    _isEnabledConnectButton = true;
                }
                else
                {
                    _isEnabledConnectButton = false;
                }
                RaisePropertyChanged("SelectedDeviceIdx");
            }
        }

        public bool IsEnabledConnectButton
        {
            get => _isEnabledConnectButton;
            set
            {
                _isEnabledConnectButton = value;
                RaisePropertyChanged("IsEnabledConnectButton");
            }
        }
        #endregion
        #region Constructors

        ///=================================================================================================
        /// <summary>Constructor of the View Model
        ///          Create a list of all relevant com ports used on the machine. A relevant com port
        ///          is defined by the correct USB vendor ID (VID) and the correct USB product ID (PID).
        ///          These values are stored in the HW class.</summary>
        ///
        /// <remarks>Bernhard Gleich, 01.05.2023.</remarks>
        ///=================================================================================================

        public ConnectDialogViewModel()
        {
            _data = new ConnectDialogModel();
            _hw = new hw();
            _devices = new ObservableCollection<string>();
            _portList = new List<ComPortList>();
            Globals.Log.Wpf("== ConnectDialogViewModel ==");
            CreatePortList();
        }
        #endregion

        #region Private methods

        ///=================================================================================================
        /// <summary>Creates port list.
        ///          Get the com ports used on the machine sorted by VID and PID. Add relevant com ports
        ///          to a list, show that list in a drop down menu and select the first entry. Than,
        ///          enable the 'connect' button.</summary>
        ///
        /// <remarks>Bernhard Gleich, 01.05.2023.</remarks>
        ///=================================================================================================

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
                SelectedDeviceIdx = 0; //select the first entry
                _isEnabledConnectButton = true; //enable the connect button
            }
        }
        #endregion

        #region Commands
        public ICommand ConnectButtonClicked { get { return new RelayCommand(OnConnectButtonClicked, CanConnectButtonBeExecuted); } }
        public ICommand CancelButtonClicked { get { return new RelayCommand(OnCancelButtonClicked, CanCancelButtonBeExecuted); } }

        #endregion

        #region Enable Control of Buttons
        bool CanConnectButtonBeExecuted()
        {
            return _isEnabledConnectButton;
        }
        bool CanCancelButtonBeExecuted()
        {
            return true;
        }

        #endregion

        #region Button Click Methods
        /// <summary>
        /// Ok button was clicked. All relevant data will be collected and passed to the configuration object Config.
        /// The window will be closed.
        /// </summary>
        private void OnConnectButtonClicked()
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

      
    }
}
