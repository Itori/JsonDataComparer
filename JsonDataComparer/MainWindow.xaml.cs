using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using JsonDataComparer.Messages;
using JsonDataComparer.ViewModel;
using Microsoft.Win32;

namespace JsonDataComparer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<FileRequestChoiceEnum, string> _fileDialogCache = new Dictionary<FileRequestChoiceEnum, string>();
        private OpenFileDialog _openFileDialog;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) =>
            {
                ViewModelLocator.Cleanup();
                Messenger.Default.Unregister<ShowOpenFileDialogMessage>(this);
            };
            Loaded += (s, e) =>
            {
                _openFileDialog = new OpenFileDialog();
                _openFileDialog.Multiselect = false;
            };

            Messenger.Default.Register<ShowOpenFileDialogMessage>(this, (msg) =>
            {
                if (_fileDialogCache.TryGetValue(msg.RequestChoice, out var initialFolder))
                    _openFileDialog.InitialDirectory = initialFolder;

                if (_openFileDialog.ShowDialog(this) != true)
                    return;
                _fileDialogCache[msg.RequestChoice] = Path.GetDirectoryName(_openFileDialog.FileName);

                Messenger.Default.Send(new FilePathChangeRequestedMessage(msg.RequestChoice, _openFileDialog.FileName));
            });
        }
    }
}