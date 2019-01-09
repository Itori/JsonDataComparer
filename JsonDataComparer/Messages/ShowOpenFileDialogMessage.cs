using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace JsonDataComparer.Messages
{
    /// <summary>
    /// Message to show a OpenFileDialog
    /// </summary>
    public class ShowOpenFileDialogMessage : MessageBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="requestChoice"></param>
        public ShowOpenFileDialogMessage(FileRequestChoiceEnum requestChoice)
        {
            RequestChoice = requestChoice;
        }

        /// <summary>
        /// Message goal
        /// </summary>
        public FileRequestChoiceEnum RequestChoice { get; }
    }
}
