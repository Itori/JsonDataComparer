namespace JsonDataComparer.Messages
{
    public class FilePathChangeRequestedMessage
    {
        public FileRequestChoiceEnum RequestChoice { get; }
        public string FileName { get; }

        public FilePathChangeRequestedMessage(FileRequestChoiceEnum requestChoice, string fileName)
        {
            this.RequestChoice = requestChoice;
            this.FileName = fileName;
        }
    }
}