using System;
using System.Collections.Generic;
using System.IO;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using JsonDataComparer.Localization;
using JsonDataComparer.Messages;
using JsonDataComparer.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonDataComparer.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;
        private string _file2Path;
        private string _file1Path;
        private string _comparisonRules = string.Empty;

        private string _welcomeTitle = string.Empty;

        /// <summary>
        /// Gets the WelcomeTitle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string WelcomeTitle
        {
            get
            {
                return _welcomeTitle;
            }
            set
            {
                Set(ref _welcomeTitle, value);
            }
        }

        public Strings Strings { get; } = new Strings();

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }

                    WelcomeTitle = item.Title;
                });

            CompareCommand = new RelayCommand(Compare, () =>
            {
                if (string.IsNullOrEmpty(File1Path) || string.IsNullOrEmpty(File2Path))
                    return false;
                return true;
            });
            BrowseJsonFilePathCommand = new GalaSoft.MvvmLight.Command.RelayCommand<FileRequestChoiceEnum>((en) => Messenger.Default.Send(new ShowOpenFileDialogMessage(en)));

            Messenger.Default.Register<FilePathChangeRequestedMessage>(this, (msg) =>
            {
                switch (msg.RequestChoice)
                {
                    case FileRequestChoiceEnum.File1:
                        File1Path = msg.FileName;
                        break;
                    case FileRequestChoiceEnum.File2:
                        File2Path = msg.FileName;
                        break;
                    case FileRequestChoiceEnum.SaveRules:
                        break;
                    case FileRequestChoiceEnum.LoadRules:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}

        public string File1Path
        {
            get => _file1Path;
            set => Set(ref _file1Path, value);
        }

        public string File2Path
        {
            get => _file2Path;
            set => Set(ref _file2Path, value);
        }

        public string ComparisonRules
        {
            get => _comparisonRules;
            set => Set(ref _comparisonRules, value);
        }

        public GalaSoft.MvvmLight.Command.RelayCommand<FileRequestChoiceEnum> BrowseJsonFilePathCommand { get; }

        public RelayCommand CompareCommand { get; }

        private void Compare()
        {
            var rules = new Dictionary<string, string>();
            var rulesStrings = ComparisonRules.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var ruleString in rulesStrings)
            {
                var indexDot = ruleString.LastIndexOf('.');
                var xpath = ruleString.Substring(0, indexDot);
                var key = ruleString.Substring(indexDot+1);

                if (rules.ContainsKey(xpath))
                    continue;
                rules.Add(xpath, key);            
            }

            using (StreamReader reader = File.OpenText(File1Path))
            {
                using (StreamReader reader2 = File.OpenText(File2Path))
                {
                    var json1 = JToken.ReadFrom(new JsonTextReader(reader));
                    var json2 = JToken.ReadFrom(new JsonTextReader(reader2));

                    var comparer = new JTokenComparer(rules);
                    comparer.Equals(json1, json2);


                }
            }



        }
    }
}