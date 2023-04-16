﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace Accelera.WebUpdater
{
    public class UpdateInformation
    {
        public Version Version { get; set; }
        public string Md5 { get; set; }
        public string Description { get; set; }
        public string LaunchArgs { get; set; }
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   The name of your application as you want it displayed on the update form. </summary>
        ///
        /// <value> The name of the application. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string ApplicationName { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   An identifier string to use to identify your application in the "update.xml"
        ///             Should be the same as your appId in the "update.xml" </summary>
        ///
        /// <value> The identifier of the application. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string ApplicationId { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   The current assembly. </summary>
        ///
        /// <value> The application assembly. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public Assembly ApplicationAssembly { get; set; }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   The adress of the SFTP host. </summary>
        ///
        /// <value> The host. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string Host { get; set;}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   The user name used to log on the SFTP server </summary>
        ///
        /// <value> The name of the user. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string UserName { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   The password needed to log on the SFTP server </summary>
        ///
        /// <value> The password. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string Password { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   The path of the remote directory on the SFTP server </summary>
        ///
        /// <value> The pathname of the remote directory. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string RemoteDirectory { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   This is the file name of the remote "update.xml" file which stores the
        ///             actual update information. This is not the downloades update file! </summary>
        ///
        /// <value> The filename of the remote file. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string RemoteFileName { get; set; }
    }
}
