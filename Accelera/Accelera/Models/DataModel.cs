using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace Accelera.Models
{
    /// <summary>
    /// Data Model Class 
    /// </summary>
    public class DataModel
    {
        public bool Valid { get; set; }
        public byte ErrorCode { get; set; }
        public byte Mode { get; set; }
        public byte State { get; set; }
        public int SampleId { get; set; }
        public int EventId { get; set; }
        public int BlockId { get; set; }
        public double TimeInSec { get; set; }

        public double XAccelerationInMeterPerSecondSquare { get; set; }
        public double YAccelerationInMeterPerSecondSquare { get; set; }
        public double ZAccelerationInMeterPerSecondSquare { get; set; }

        public double TemperatureInDegreeCelsius { get; set; }
        public uint XRawData { get; set; }
        public uint YRawData { get; set; }
        public uint ZRawData { get; set; }
        public uint TempRawData { get; set; }

        
        
        

    }
}
