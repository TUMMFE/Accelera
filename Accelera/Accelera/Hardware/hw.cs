using Accelera.Models;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;


namespace Accelera.Hardware
{
    /// <summary>
    /// This class provides the interface with the hardware. This class implements the communication protocol
    /// </summary>
    public class hw
    {
        private readonly ObservableCollection<string> _possibleOutputDataRates;
        private readonly ObservableCollection<string> _possibleHighPassFilterFrequencies;
        private readonly ObservableCollection<string> _possibleRanges;
        private readonly ObservableCollection<string> _possibleDataType;
        private readonly ObservableCollection<string> _possibleTriggerType;

        /// <summary>
        /// PID and VID values are located in the source code of the firmware. The ones from STM are used due to simplification.
        /// PID and VID are defined in "usbd_desc.c" of the device source code.
        /// Leading zeros must be included and the strings are the HEX representation of the VID and PID.
        /// </summary>
        public const string VID = "0483";       //this is the STM VID as a hex value 0x0483 = 1155. The leading zero is essential.
        public const string PID = "5740";       //this is the STM PID as a hex value 0x5740 = 22336

        /// <summary>
        /// Communication protocol.
        /// The lenght of one data frame is 9 bytes. Each data frame consists of one command byte followed by 8 bytes of payload.
        /// The edinaness of the data is BIG ENDIAN, which means highest byte/bit comes first.
        /// </summary>
        public const int TxProtocolLength               = 9;  //length of data frame which will be transmitted to the device. 
        public const int RxProtocolLength               = 22;  //length of data frame which will be transmitted from the device. 
        public const byte TxCmdSetAcousticParameters    = 0x10;
        public const byte TxCmdSetTriggerParameters     = 0x20;
        public const byte TxCmdSetAdxlParameters        = 0x30;
        public const byte TxCmdSetOffset                = 0x40;
        public const byte TxCmdSetOpMode                   = 0x50;
        public const byte TxCmdGetSamples               = 0x60;
        public const byte Ack                           = 0x06;

        public const int NumberOfLsbSteps               = 1048576;
        public const int OffsetInLsbSteps               = NumberOfLsbSteps / 2;
        public const double SensitivityInLsbPerG           = 256000.0;   // valid for +/-2g; for +/-4g divide by 2, for +/-8g divide by 4


        /// <summary>
        /// Physical Parameters of the sensor.
        /// Only raw data (coded in 2's complement for acc. data) is transfered via USB. To convert this data from LSB into
        /// physical units, the following constants are needed.
        /// 
        /// Acceleration:
        /// At +/-2 g the scale factor to convert from LSB to m/^s^2 ist 3.9 µg/LSB = 3.9 x 9.80665 = 38,245935 µm/s^2
        /// At +/-4 g the scale factor will be multiplied by 2 and at +/-8 g the scale factor will be multiplied by 4.
        /// 
        /// Temperature:
        /// The nominal intercept is 1852 LSB at 25 °C and the nominal slope is -9.05 LSB/°C. Only the lowest 12 bits are valied.
        /// Thus, 
        ///         1852 LSB = -9.05 LSB/°C * 25 °C + X --> X = 1852 LSB + 226 LSB = 2078 LSB
        ///         Value = -9.05 LSB/°C * T + 2078 LSB
        ///         T = (Value - 2078 LSB)/(-9.05 LSB/°C) = -0.11 °C/LBS * Value + 229.6 °C
        /// </summary>
        public const double AccelerationScaleFactor = 0.0000382;
        public const double TemperatureSlope        = -0.11;
        public const double TemperatureOffset       = 229.6;
        public const double Gravity                 = 9.80665;

        
        public ObservableCollection<string> PossibleOutputDataRates => _possibleOutputDataRates;
               
        public ObservableCollection<string> PossibleHighPassFilterFrequencies => _possibleHighPassFilterFrequencies;
       
        public ObservableCollection<string> PossibleRanges => _possibleRanges;

        public ObservableCollection<string> PossibleDataType => _possibleDataType;

        public ObservableCollection<string> PossibleTriggerType => _possibleTriggerType;

        /// <summary>
        /// Constructor.
        /// Creates the value lists for output data rates and highpass filter configuration.
        /// Do not change the positions in the string list - they are directly (their index) linked to the send value for the hardware
        /// </summary>
        public hw()
        {
            List<string> odr = new List<string>() { "4000 Hz", "2000 Hz", "1000 Hz", "500 Hz", "250 Hz", "125 Hz", "62.5 Hz", "31.25 Hz", "15.625 Hz", "7.813 Hz", "3.906 Hz" };
            _possibleOutputDataRates = new ObservableCollection<string>(odr);
            List<string> hpf = new List<string>() { "off", "2470 µHz x ODR", "620.84 µHz x ODR", "155.45 µHz x ODR", "38.62 µHz x ODR", "9.54 µHz x ODR", "2.387 µHz x ODR" };
            _possibleHighPassFilterFrequencies = new ObservableCollection<string>(hpf);
            List<string> ranges = new List<string>() { "2 g (+/-)", "4 g  (+/-)", "8 g  (+/-)" };
            _possibleRanges = new ObservableCollection<string>(ranges);
            List<string> triggers = new List<string>() { "Rising Edge", "Falling Edge", "Both Edges" };
            _possibleTriggerType = new ObservableCollection<string>(triggers);
            List<string> types = new List<string>() { "Status", "Temperature", "X-Acceleration", "Y-Acceleration", "Z-Acceleration"};
            _possibleDataType = new ObservableCollection<string>(types);
        }

        /// <summary>
        /// Converts the temperature given in LSB to degree celsius.
        /// Temperature:
        /// The nominal intercept is 1852 LSB at 25 °C and the nominal slope is -9.05 LSB/°C. 
        /// Only the lowest 12 bits are valied.
        /// Thus, 
        ///         1852 LSB = -9.05 LSB/°C * 25 °C + X --> X = 1852 LSB + 226 LSB = 2078 LSB
        ///         Value = -9.05 LSB/°C * T + 2078 LSB
        ///         T = (Value - 2078 LSB)/(-9.05 LSB/°C) = -0.11 °C/LBS * Value + 229.6 °C 
        /// </summary>
        /// <param name="value">temperature in LSB value</param>
        /// <returns></returns>
        public double GetTemperatureInDegreeCelsius(UInt16 value)
        {
            int val = (int) value;
            double temp;

            val &= ~((1 << 15)|(1 << 14)|(1<<13)|(1<<12)); //clear bit 12-15
                      
            temp = TemperatureSlope * (double)val + TemperatureOffset;
            return temp;            
        }

        /// <summary>
        /// Convert acceleration values from raw data to m/s^2. Data is coded as
        /// unsigned integer using 2s complement. Thus, first convert it to signed
        /// data while taking into account, that the ADC is 20-bit wide and thus bit
        /// number 19 will mark the sign.
        /// Second, convert the result to acceleration in m/s^2 by using the range
        /// dependant scale factor.
        /// 
        /// Acceleration:
        /// At +/-2 g the scale factor to convert from LSB to m/^s^2 ist 3.9 µg/LSB = 3.9 x 9.80665 = 38,245935 µm/s^2
        /// At +/-4 g the scale factor will be multiplied by 2 and at +/-8 g the scale factor will be multiplied by 4.
        /// The sensitivity at +/- 2 g ist 235,520 LSB/g to 276,480 LSB/g (mean 256,000 LSB/g), at +/- 4 g it is 
        /// 117,760 LSB/g to 138,240 LSB/g (mean 128,000 LSB/g) and at +/- 8 g it is 58,880 LSB/g to 69,120 LSB/g 
        /// (mean 64,000 LSB/g).
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public double GetAccerlerationInMeterPerSecondSquare(UInt32 value, double range)
        {
            int val = 0;
            double acc;
            double factor = range / 2.0; //factor is always half the range.;

            //convert from 2s complement to signed integer)           
            /*
            if ((value & (1<<19)) == 0) //it is a 20-bit ADC, thus bit no.19 defines the sign
            {
                val = (int)value;
            }
            else
            {
                val = (int)(~(value - 0x01)) * (-1);
            }
            */

            val = (int)(value ^ (1<< 19)-1) - (hw.OffsetInLsbSteps);
            acc = (val / (hw.SensitivityInLsbPerG / factor))*hw.Gravity;

            //acc = AccelerationScaleFactor * (double)val;




            return acc;
    }
    /// <summary>
    /// Extract a decimal number from a string
    /// e.g. "620.84 µH" --> 620.84
    /// Will not work in this case
    /// +/- 2 g --> not decimals in front, function will crash an return -1
    /// </summary>
    /// <param name="str"> string which should be analyzed</param>
    /// <returns></returns>
    public double ExtractDecimalFromString(string str)
        {
            double i = 0;
            double retval = -1;
            string[] numbers = Regex.Split(str, @"[^.\d]\D+"); //find text inside a decimal number "-4.51 kHz" --> " kHz"

            if (Double.TryParse(numbers[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out i))
            {
                retval = i;  // if you are here, you were able to parse the string
            }
            return retval;
        }

        /// <summary>
        /// Create the send buffer to configure the acoustic stimulation parameters.
        /// Set Acoustic Stimulation Parameters
        /// 0x10	CNT[1:2]    FSTIM[3:4]        BSTIM[5:6]       DUR[7:8]	
        /// The number of trigger/stimulation events in on block is given by 
        ///     Total Number of Events = FSTIM x BSTIM
        /// MSB first
        /// </summary>
        /// <param name="cnt"> number of samples at each trigger/stimulation event</param>
        /// <param name="fstim">frequency of stimulation event in mHz</param>
        /// <param name="bstim">duration of one stimulation block in s</param>
        /// <param name="duration">duration of one acoustic stimulus in ms</param>
        /// <returns>Send buffer to be transmitted via USB to the device</returns>
        public byte[] SetAcousticParameters(short cnt, short fstim, short bstim, short duration)
        {
            byte[] buffer = new byte[TxProtocolLength];
            byte[] val16 = new byte[2];
            buffer.Populate((byte) 0);

            buffer[0] = TxCmdSetAcousticParameters;

            val16 = BitConverter.GetBytes(cnt);
            Array.Reverse(val16);
            buffer[1] = val16[0];
            buffer[2] = val16[1];

            val16 = BitConverter.GetBytes(fstim);
            Array.Reverse(val16);
            buffer[3] = val16[0];
            buffer[4] = val16[1];

            val16 = BitConverter.GetBytes(bstim);
            Array.Reverse(val16);
            buffer[5] = val16[0];
            buffer[6] = val16[1];

            val16 = BitConverter.GetBytes(duration);
            Array.Reverse(val16);
            buffer[7] = val16[0];
            buffer[8] = val16[1];
            return buffer;
        }

        /// <summary>
        /// Create the send buffer to configure the trigger stimulation mode.
        /// Set Trigger Mode Parameters
        /// 0x20	CNT[1:2]    POL[3]     DC[4:8]
        /// MSB first
        /// </summary>
        /// <param name="cnt">number of samples at each trigger/stimulation event</param>
        /// <param name="polarity">0x00 for falling edges, 0x01 for both edges, anything else for rising edges</param>
        /// <returns>Send buffer to be transmitted via USB to the device</returns>
        public byte[] SetTriggerModeParameters(short cnt, byte polarity)
        {
            byte[] buffer = new byte[TxProtocolLength];
            byte[] val16 = new byte[2];
            buffer.Populate((byte)0);

            buffer[0] = TxCmdSetTriggerParameters;

            val16 = BitConverter.GetBytes(cnt);
            Array.Reverse(val16);
            buffer[1] = val16[0];
            buffer[2] = val16[1];
            buffer[3] = polarity;
            return buffer;
        }

        /// <summary>
        /// Create the send buffer to configure the ADXL sensor parameters.
        /// ADXL355 Control
        /// 0x30    HPF[1]							LPF[2]					RANGE[3]			ACT_COUNT[4]    DC[5:8]
        ///         ------							------					--------    		------------
        ///         |0x00 = off						|						|					|
        ///         |0x01 = 24.7 x 10^-4 x ODR		|						|					|
        ///         |0x02 = 6.2084 x 10^-4 x ODR	|						|					|
        ///         |0x03 = 1.5545 x 10^-4 x ODR	|						|					|
        ///         |0x04 = 0.3862 x 10^-4 x ODR	|						|					|
        ///         |0x05 = 0.0954 x 10^-4 x ODR	|						|					|
        ///         |0x06 = 0.02387 x 10^-4 x ODR	|						|					|
        ///									        |0x00 = 4 kHz ODR		|					|
        ///         								|0x01 = 2 kHz ODR		|					|
        ///			            					|0x02 = 1 kHz ODR		|					|
        ///						            		|0x03 = 500 Hz ODR		|					|
        ///         								|0x04 = 250 Hz ODR		|					|
        ///			            					|0x05 = 125 Hz ODR		|					|
        ///						            		|0x06 = 62.5 Hz ODR		|					|
        ///         								|0x07 = 31.25 Hz ODR	|					|
        ///			            					|0x07 = 15.625 Hz ODR	|					|
        ///						            		|0x09 = 7.813 Hz ODR	|					|
        ///			            					|0x0A = 3.906 Hz ODR	|					|
        ///						            								|0x01 = +/- 2g		|
        ///									            					|0x02 = +/- 4g		|
        ///												            		|0x03 = +/- 8g		|
        ///															            				|activity counts
        ///																		            	|0 = deactivated
        /// </summary>
        /// <param name="hpf">the byte value corresponds to the index of the string list</param>
        /// <param name="odr"></param>
        /// <param name="range"></param>
        /// <param name="act_cnt"></param>
        /// <returns>Send buffer to be transmitted via USB to the device</returns>
        public byte[] SetAdxlParameters(byte hpf, byte odr, byte range, byte act_cnt)
        {
            byte[] buffer = new byte[TxProtocolLength];
            buffer.Populate((byte)0);
            buffer[0] = TxCmdSetAdxlParameters;
            buffer[1] = hpf;
            buffer[2] = odr;
            buffer[3] = (byte)(range + 1);
            buffer[4] = act_cnt;
            return buffer;
        }

        /// <summary>
        /// Create the send buffer to configure the offset parameters for the ADXL sensor.
        /// ADXL355 Offset
        /// 0x40    X[1:2]  Y[3:4]  Z[5:6]  ACT_THRES[7:8]
        /// MSB first and all values in 2's complement represenation
        /// 
        /// </summary>
        /// <param name="x">offset in x-direction</param>
        /// <param name="y">offset in y-direction</param>
        /// <param name="z">offset in z-direction</param>
        /// <param name="act_thr">activity threshold level</param>
        /// <returns>Send buffer to be transmitted via USB to the device</returns>
        public byte[] SetAdxlOffset(short x, short y, short z, short act_thr)
        {
            byte[] buffer = new byte[TxProtocolLength];
            byte[] val16 = new byte[2];

            buffer.Populate((byte)0);
            buffer[0] = TxCmdSetOffset;

            val16 = BitConverter.GetBytes(x);
            Array.Reverse(val16);
            buffer[1] = val16[0];
            buffer[2] = val16[1];

            val16 = BitConverter.GetBytes(y);
            Array.Reverse(val16);
            buffer[3] = val16[0];
            buffer[4] = val16[1];

            val16 = BitConverter.GetBytes(z);
            Array.Reverse(val16);
            buffer[5] = val16[0];
            buffer[6] = val16[1];

            val16 = BitConverter.GetBytes(act_thr);
            Array.Reverse(val16);
            buffer[7] = val16[0];
            buffer[8] = val16[1];

            return buffer;
        }

        /// <summary>
        /// Create the send buffer to set the operation mode
        /// Set Mode/Start/Stop
        /// 0x50	MODE[1]                             SSPR[2]    DC[3:8]
		///         -------								----
		///         |0x00 = acoustic stimulation		|
		///         |0x01 = external trigger, beep on	|
		///         |0x02 = external trigger, beep off	|
		///         |0x03 = free running mode			|
		///								            	|0x00 = start
		///         									|0x01 = stop
		///			            						|0x02 = pause
		///						            			|0x03 = resume
        /// </summary>
        /// <param name="mode">Operation Mode:  0x00 = acoustic stimulation, 0x01 = external trigger with beep, 0x02 = external trigger without beep, 0x03 = free running mode</param>
        /// <param name="sspr">Operation State: 0x00 = start, 0x01 = stop, 0x02 = pause, 0x03 = resume</param>
        /// <returns>Send buffer to be transmitted via USB to the device</returns>
        public byte[] SetOpMode(byte mode, byte sspr)
        {
            byte[] buffer = new byte[TxProtocolLength];

            buffer.Populate((byte)0);
            buffer[0] = TxCmdSetOpMode;
            buffer[1] = mode;
            buffer[2] = sspr;
            return buffer;
        }

        public byte[] SetSamples(short cnt, bool beep, bool waitForExti)
        {
            byte[] buffer = new byte[TxProtocolLength];
            byte[] val16 = new byte[2];

            buffer.Populate((byte)0);
            buffer[0] = TxCmdGetSamples;
            val16 = BitConverter.GetBytes(cnt);
            Array.Reverse(val16);
            buffer[1] = val16[0];
            buffer[2] = val16[1];
            if (beep == true)
            {
                buffer[3] = 0x01;
            } else
            {
                buffer[3] = 0x00;
            }
            if (waitForExti == true)
            {
                buffer[4] = 0x01;
            } else
            {
                buffer[4] = 0x00;
            };
            return buffer;
        }
    }
}
