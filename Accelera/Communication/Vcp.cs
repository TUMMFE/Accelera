using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Microsoft.Win32;
using FTD2XX_NET;

namespace MFE.Communication
{
    /// <summary>
    /// Communication with an USB device using a virtual com port (VCP). The class provides functions to get a list of all com ports
    /// with a known VID and PID or a list of all FTDI devices using the D2XX drivers from FTDI.
    /// A communication port can be opened using either a unique FTDI serial number (when using a FTDI interface IC) or the com port name
    /// (e.g. COM3).
    /// </summary>
    public class Vcp
    {
        #region Private variables

        #endregion

        #region Properties
        private bool _isOpen;
        private ReliableSerialPort _port;
        private BufferBlock<byte[]> _rxbuffer;

        #endregion

        #region Constructors and destructors, getter and setter
      
        public bool IsOpen => _isOpen;

        public ReliableSerialPort Port { get => _port; set => _port = value; }
        public BufferBlock<byte[]> Rxbuffer { get => _rxbuffer; set => _rxbuffer = value; }


        /// <summary>
        /// Standard construction which can be used if the comport is already known.
        /// </summary>
        public Vcp()
        {
            _rxbuffer = new BufferBlock<byte[]>();
        }

        ~Vcp()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _port.DataReceived -= OnData;
            }

            // Releasing serial port (and other unmanaged objects)
            if (_port != null)
            {
                if (_port.IsOpen)
                    _port.Close();
                _port.Dispose();
            }
        }

        #endregion

        #region Events
        public event EventHandler DataReceivedComplete;

        /// <summary>
        /// Data reception event. Event will write all received data into a buffer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnData(object sender, DataReceivedArgs e)
        {
            Produce(_rxbuffer, e);
        }


        #endregion

        #region Private functions
        
        /// <summary>
        /// Producer thread to collect the received amount of data.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="e"></param>
        private void Produce(ITargetBlock<byte[]> target, DataReceivedArgs e)
        {
            target.Post(e.Data);
        }
        #endregion

        #region Member functions

        /// <summary>
        /// Open the port with the given communication parameters and hook up the DataReceived event.
        /// </summary>
        /// <param name="comPort">can be the os name of the com port (e.g. COM7)</param>
        /// <param name="baudrate">baudrate</param>
        /// <param name="parity">Parity</param>
        /// <param name="dataBits"># of data bits</param>
        /// <param name="stopBits"># of stop bits</param>
        public void Open(string comPort, int baudrate, Parity parity, int dataBits, StopBits stopBits)
        {
            try
            {
                
                _port = new ReliableSerialPort(comPort, baudrate, parity, dataBits, stopBits);
                _port.DataReceived += OnData;
                _port.Open();                
                _isOpen = true;
            }
            catch
            {
                _isOpen = false;
                throw new IOException("Can not open the device.");
            }
        }

        /// <summary>
        /// Close the used com port and release the date receive event.
        /// </summary>
        public void Close()
        {
            if (_isOpen == true)
            {
                _port.Close();
                _port.DataReceived -= OnData;
                _isOpen = false;
            }
        }
        public void Restart()
        {
            _rxbuffer = new BufferBlock<byte[]>();
        }

        /// <summary>
        /// The function look up a given vendor ID and product ID in the windows registry.
        /// Only com ports which are available at the moment are found.
        /// </summary>
        /// <param name="vid">Vendor ID like "0430" - do not miss the leading zero.</param>
        /// <param name="pid">Product ID lile "1570"</param>
        /// <returns>a list of valid com port names which are connected to the computer with the given VID and PID.</returns>
        public List<ComPortList> GetComPortList(string vid, string pid)
        {
            String pattern = String.Format("^VID_{0}.PID_{1}", vid, pid);
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
            List<ComPortList> comports = new List<ComPortList>();

            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");

            foreach (String s3 in rk2.GetSubKeyNames())
            {
                RegistryKey rk3 = rk2.OpenSubKey(s3);
                foreach (String s in rk3.GetSubKeyNames())
                {
                    if (_rx.Match(s).Success)
                    {
                        RegistryKey rk4 = rk3.OpenSubKey(s);
                        foreach (String s2 in rk4.GetSubKeyNames())
                        {
                            RegistryKey rk5 = rk4.OpenSubKey(s2);
                            string location = (string)rk5.GetValue("LocationInformation");
                            string friendlyName = (string)rk5.GetValue("FriendlyName");
                            string deviceDesc = (string)rk5.GetValue("DeviceDesc");
                            RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
                            string portName = (string)rk6.GetValue("PortName");
                            if (!String.IsNullOrEmpty(portName) && SerialPort.GetPortNames().Contains(portName))
                            {
                                ComPortList portItem = new ComPortList(portName, vid, pid, deviceDesc, friendlyName);
                                comports.Add(portItem);
                            }
                        }
                    }
                }
            }
            return comports;
        }

        /// <summary>
        /// The function will look up all available and connected FTDI devices.
        /// </summary>
        /// <returns></returns>
        public List<ComPortList> GetComPortList()
        {
            uint count = 0;
            FTDI ftdiChip = new FTDI();
            List<ComPortList> ports = new List<ComPortList>();

            FTDI.FT_STATUS status;
            ftdiChip.GetNumberOfDevices(ref count);
            FTDI.FT_DEVICE_INFO_NODE[] list = new FTDI.FT_DEVICE_INFO_NODE[count];
            ports.Clear();

            status = ftdiChip.GetDeviceList(list);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                return null;
            }

            foreach (FTDI.FT_DEVICE_INFO_NODE node in list)
            {
                if (ftdiChip.OpenByLocation(node.LocId) == FTDI.FT_STATUS.FT_OK)
                {
                    try
                    {
                        string comport;
                        ftdiChip.GetCOMPort(out comport);

                        if (comport != null && comport.Length > 0)
                        {
                            ports.Add(new ComPortList(comport, "none", "none", node.Description, node.SerialNumber));
                        }
                    }
                    finally
                    {
                        ftdiChip.Close();
                    }
                }
            } //foreach
            return ports;
        }

        /// <summary>
        /// Get the com port name of a connected FTDI device by its serial number
        /// </summary>
        /// <param name="serialNumber">serial number ob the FTDI device</param>
        /// <returns>string of the com port name e.g. COM3. If no device is found retval contains an empty string</returns>
        public string GetComPortBySerialNumber(string serialNumber)
        {
            List<ComPortList> ports = new List<ComPortList>(); 
            ports = GetComPortList();
            string retval = string.Empty;
            if (ports != null)
            {
                IEnumerable<ComPortList> temp = ports;
                temp = from v in temp orderby v.ComPortName ascending select v;
                foreach (ComPortList item in temp)
                {
                    if (item.SerialNumber == serialNumber)
                    {
                        retval = Convert.ToString(item.ComPortName);
                        break;
                    }
                }
            }
            return retval;
        }
        #endregion
    }
}
