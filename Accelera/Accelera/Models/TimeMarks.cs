using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace Accelera.Models
{
    public class TimeMarks
    {
        private string _type;
        private long _absoluteTimeStamp;
        private long _relativeTimeStamp;

        public string Type { get => _type; set => _type = value; }
        public long AbsoluteTimeStamp { get => _absoluteTimeStamp; set => _absoluteTimeStamp = value; }
        public long RelativeTimeStamp { get => _relativeTimeStamp; set => _relativeTimeStamp = value; }
    }
}
