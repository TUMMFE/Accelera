using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace MFE.Communication
{
    public class ReliableSerialPort:SerialPort
    {
        private bool stop;
        public ReliableSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            PortName = portName;
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
            Handshake = Handshake.None;
            DtrEnable = true;
            NewLine = Environment.NewLine;
            ReceivedBytesThreshold = 1024;
            stop = false;;
        }

        new public void Open()
        {
            base.Open();
        }

        private void ContinuousRead()
        {
            byte[] buffer = new byte[4096];            
            Action kickoffRead = null;
            kickoffRead = (Action)(() => BaseStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar)
            { 
                if (stop == false)
                {
                    try
                    {
                        int count = BaseStream.EndRead(ar);
                        byte[] dst = new byte[count];
                        Buffer.BlockCopy(buffer, 0, dst, 0, count);
                        OnDataReceived(dst);
                    }
                    catch { }
                    kickoffRead();
                } else
                {
                    BaseStream.Flush();
                }
            }, null)); kickoffRead();
        }

        public new event EventHandler<DataReceivedArgs> DataReceived;
        public virtual void OnDataReceived(byte[] data)
        {
            var handler = DataReceived;
            if (handler != null)
            {
                handler(this, new DataReceivedArgs { Data = data });
            }
        }

        public void CloseIt()
        {            
            stop = true;
        }

        public void StartListening()
        {
            stop = false;
            ContinuousRead();

        }
    }

    public class DataReceivedArgs : EventArgs
    {
        public byte[] Data { get; set; }
    }
}
