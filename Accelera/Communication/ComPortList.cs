namespace MFE.Communication
{
    /// <summary>
    /// This is a helper class to store some information about the used virtual comport. 
    /// </summary>
    public class ComPortList
    {
        private string _comPortName;
        private string _description;
        private string _vid;
        private string _pid;
        private string _serialNumber;

        public string ComPortName
        {
            get => _comPortName;
            set => _comPortName = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public string Vid
        {
            get => _vid;
            set => _vid = value;
        }

        public string Pid
        {
            get => _pid;
            set => _pid = value;
        }

        public string SerialNumber
        {
            get => _serialNumber;
            set => _serialNumber = value;
        }

        /// <summary>
        /// Create a object of type ComPortList
        /// </summary>
        /// <param name="cName">Name of the port e.g. COM1</param>
        /// <param name="vid">Vendor ID like "0430" - do not miss the leading zero.</param>
        /// <param name="pid">Product ID lile "1570"</param>
        /// <param name="desc">Description string of the port</param>
        /// <param name="serial">Serial number of the used FTDI chip or the used device</param>
        public ComPortList(string cName, string vid, string pid, string desc, string serial)
        {
            _comPortName = cName;
            _vid = vid;
            _pid = pid;
            _description = desc;
            _serialNumber = serial;
        }
    }
}
