using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accelera.Models
{
    /// <summary>
    /// The class is used as a data model for the ConnectDialogViewModel. The class is a data template for
    /// passing information between the MainWindowView and the ConnectDialogView.
    /// </summary>
    public class ConnectDialogModel
    {
        private string _connectionString;

        // <summary>
        /// The connenction string contains the name of the used comport (e.g. "COM3").
        /// With the connection string the virtual comport can be opened.
        /// </summary>
        public string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value;
        }

    }
}
