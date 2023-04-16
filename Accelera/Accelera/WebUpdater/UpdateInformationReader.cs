using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;

namespace Accelera.WebUpdater
{
    public class UpdateInformationReader
    {
        private UpdateInformation _info;

        ///=================================================================================================
        /// <summary>Constructor.</summary>
        ///
        /// <remarks>Bernhard Gleich, 10.04.2023.</remarks>
        ///
        /// <param name="version">        The version.</param>
        /// <param name="host">           The host.</param>
        /// <param name="userName">       Name of the user.</param>
        /// <param name="password">       The password.</param>
        /// <param name="fileName">       Filename of the file.</param>
        /// <param name="md5">            The fifth md.</param>
        /// <param name="remoteDirectory">Pathname of the remote directory.</param>
        /// <param name="description">    The description.</param>
        /// <param name="launchArgs">     The launch arguments.</param>
        ///=================================================================================================

        public UpdateInformationReader(UpdateInformation updateInformation)
        {
            _info = updateInformation;
        }

        ///=================================================================================================
        /// <summary>Query if 'version' is newer than the installed one</summary>
        ///
        /// <remarks>Bernhard Gleich, 10.04.2023.</remarks>
        ///
        /// <param name="version">The version of the current installed app.</param>
        ///
        /// <returns>True if newer than, false if not.</returns>
        ///=================================================================================================

        public bool IsNewerThan(Version version)
        {
            return _info.Version > version;
        }

        public bool ExistsOnServer()
        {
            try
            {
                SftpClient sftp = new SftpClient(_info.Host, _info.UserName, _info.Password);
                sftp.Connect();
                List<SftpFile> fileList = sftp.ListDirectory(_info.RemoteDirectory).ToList();
                if (fileList.Count == 0)
                {
                    //There is no file in the folder
                    return false;
                }
                foreach (SftpFile f in fileList)
                {
                    //a not case sensitive comparison is made
                    if (f.IsRegularFile && string.Equals(f.Name, _info.RemoteFileName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        sftp.Disconnect();
                        return true;
                    }
                }
                //if not found in the list, the file does not exist
                sftp.Disconnect();
                return false;
            }
            catch
            {
                return false;
            }
        }

        public UpdateInformation Parse(string appId)
        {
            try
            {
                SftpClient sftp = new SftpClient(_info.Host, _info.UserName, _info.Password);
                sftp.Connect();
                SftpFileStream fstream = sftp.Open(_info.RemoteDirectory + _info.RemoteFileName, FileMode.Open);
                ServicePointManager.ServerCertificateValidationCallback = (s, ce, ch, ssl) => true;
                XmlDocument doc = new XmlDocument();
                doc.Load(fstream);

                // Gets the appId's node with the update info
                // This allows you to store all program's update nodes in one file
                if (doc.DocumentElement != null)
                {
                    XmlNode updateNode = doc.DocumentElement.SelectSingleNode("//update[@appID='" + appId + "']");

                    // If the node doesn't exist, there is no update
                    if (updateNode == null)
                        return null;

                    // Parse data
                    _info.Version = Version.Parse(updateNode["version"]?.InnerText);
                    UpdateInformation info = new UpdateInformation();
                    _info.RemoteFileName = updateNode["fileName"]?.InnerText;
                    _info.Md5 = updateNode["md5"]?.InnerText;
                    _info.Description = updateNode["description"]?.InnerText;
                    _info.LaunchArgs = updateNode["launchArgs"]?.InnerText;
                    return _info;
                }
            }
            catch
            {
                return null;
            }
            return null;
        }
    }
}
