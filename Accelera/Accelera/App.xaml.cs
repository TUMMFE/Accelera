using Accelera.Properties;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Accelera
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs ev)
        {
            Globals.Log.Fatal(ev.Exception.Message + ev.Exception);
            Current.Shutdown();
        }
    }
    public static class Globals
    {
        public static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    // Group: Log4Net

    public class DebugTraceListener : TraceListener
    {
        public override void Write(string message)
        {
        }
        public override void WriteLine(string message)
        {
            Debugger.Break();
        }

    }

    public class Log4NetTraceListener : TraceListener
    {
        public override void Write(string message)
        {
        }
        public override void WriteLine(string message)
        {
            Globals.Log.Wpf(message);
        }

    }

    public static class LoggingExtensions
    {
        private static readonly Level _plotLevel = new Level(50000, "PLOT");
        private static readonly Level _wpfLevel = new Level(45000, "WPF");
        private static readonly Level _comLevel = new Level(42000, "COM");

        public static void Plot(this ILog log, string message, params object[] args)
        {
            string formattedMessage = string.Format(message, args);
            log.Logger.Log(MethodBase.GetCurrentMethod().DeclaringType, _plotLevel, formattedMessage, null);
        }

        public static void Wpf(this ILog log, string message, params object[] args)
        {
            string formattedMessage = string.Format(message, args);
            log.Logger.Log(MethodBase.GetCurrentMethod().DeclaringType, _wpfLevel, formattedMessage, null);
        }

        public static void Com(this ILog log, string message, params object[] args)
        {
            string formattedMessage = string.Format(message, args);
            log.Logger.Log(MethodBase.GetCurrentMethod().DeclaringType, _comLevel, formattedMessage, null);
        }

    }

}
