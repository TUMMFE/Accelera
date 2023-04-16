using System;
using System.Windows.Threading;

namespace Accelera.Models
{
    public class DispatcherTimerEx : DispatcherTimer
    {
        public new TimeSpan Interval
        {
            get => maxInterval;
            set => base.Interval = maxInterval = value;
        }

        public new bool IsEnabled
        {
            get => base.IsEnabled;
            set
            {
                if (value == base.IsEnabled)
                    return;
                if (value)
                    this.Start();
                else
                    this.Stop();
            }
        }

        TimeSpan maxInterval;
        DateTime startTime = DateTime.MinValue;
        DateTime stopTime = DateTime.MinValue;

        public DispatcherTimerEx()
        {
            base.Tick += OnTick;
        }

        public DispatcherTimerEx(DispatcherPriority priority) : base(priority)
        {
            base.Tick += OnTick;
        }

        public DispatcherTimerEx(DispatcherPriority priority, Dispatcher dispatcher) : base(priority, dispatcher)
        {
            base.Tick += OnTick;
        }

        public DispatcherTimerEx(TimeSpan interval, DispatcherPriority priority, EventHandler callback,
            Dispatcher dispatcher) : base(interval, priority, callback, dispatcher)
        {
            base.Tick += OnTick;
        }

        public new void Start()
        {
            base.Start();
            startTime = DateTime.Now;
            stopTime = DateTime.MinValue;
        }

        void OnTick(object sender, EventArgs e)
        {
            startTime = DateTime.Now;
            if (base.Interval == maxInterval) return;

            base.Stop();
            base.Interval = maxInterval;
            base.Start();
        }


        public void Pause()
        {
            base.Stop();
            stopTime = DateTime.Now;
        }

        public void Resume()
        {
            if (startTime == DateTime.MinValue)
                startTime = DateTime.Now;

            if (stopTime == DateTime.MinValue)
            {
                base.Interval = maxInterval;
            }
            else
            {
                base.Interval = maxInterval - (stopTime - startTime);
                stopTime = DateTime.MinValue;
            }

            base.Start();
        }
    }
}
