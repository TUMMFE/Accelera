using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace CreateXmlForms {

    public partial class StartForm : DevComponents.DotNetBar.Metro.MetroForm {

        private string _applicationFullPath;
        private string _applicationVersion;
        private string _applicationFileName;
        private string _md5;
        private string _downloadFileFullPath;

        private readonly XmlDocument _doc = new XmlDocument();
        private XmlNode _root;
        private XmlNode _update;
        private string _xmlPath;

        public StartForm() {
            InitializeComponent();
        }

        private void SelectAppFileButton_Click(object sender, EventArgs e) {
            openFileDialog1.DefaultExt = "exe";
            DialogResult result = openFileDialog1.ShowDialog();
            if ( result == DialogResult.OK ) {
                _applicationFullPath = openFileDialog1.FileName;
                _applicationVersion = AssemblyName.GetAssemblyName(_applicationFullPath).Version.ToString();
                //applicationFileName = Path.GetFileName(applicationFullPath);
                AppFilePathTextBox.Text = _applicationFullPath;
                AppVersionTextBox.Text = _applicationVersion;
            }
        }

        private static string GetMd5(string path) {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            FileStream stream = File.Open(path, FileMode.Open);
            md5.ComputeHash(stream);
            stream.Close();

            StringBuilder sb = new StringBuilder();
            foreach ( byte byt in md5.Hash ) {
                sb.Append(byt.ToString("x2"));
            }

            return sb.ToString().ToUpper();
        }

        private void SelectDownloadFileButton_Click(object sender, EventArgs e) {
            openFileDialog1.DefaultExt = "exe";
            DialogResult result = openFileDialog1.ShowDialog();
            if ( result == DialogResult.OK ) {
                _downloadFileFullPath = openFileDialog1.FileName;
                _md5 = GetMd5(_downloadFileFullPath);
                _applicationFileName = Path.GetFileName(_downloadFileFullPath);

                DownloadFilePathTextBox.Text = _downloadFileFullPath;
                MD5TextBox.Text = _md5;
                _xmlPath = Path.GetDirectoryName(_downloadFileFullPath) + "\\update.xml";
            }
        }

        private void CreateXMLButton_Click(object sender, EventArgs e) {
            string logEn;

            _root = _doc.CreateElement("Updater");
            _doc.AppendChild(_root);

            _update = _doc.CreateElement("update");
            XmlAttribute attribute = _doc.CreateAttribute("appID");
            attribute.Value = Path.GetFileNameWithoutExtension(_applicationFullPath);
            _update.Attributes.Append(attribute);
            _root.AppendChild(_update);

            AppendNode("version", _applicationVersion);
            AppendNode("url", "na");
            AppendNode("fileName", _applicationFileName);
            AppendNode("md5", _md5);

            string[] logEnArray = UpdateDescriptionTextBox.Lines;
            if ( logEnArray.Length == 0 ) {
                logEn = "";
            } else {
                logEn = string.Join("\n    ", logEnArray);
            }

            string date = DateTime.Today.ToString("d");
            string log = string.Concat(date, " Update:\n    ", logEn, "\n");

            AppendNode("description", log);
            AppendNode("launchArgs", "");
            _doc.Save(_xmlPath);
        }

        private void AppendNode(string nodeName, string value) {
            XmlNode version = _doc.CreateElement(nodeName);
            version.InnerText = value;
            _update.AppendChild(version);
        }

    }

}