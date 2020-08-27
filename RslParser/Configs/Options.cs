using CommandLine;

namespace RslParser.Configs {
    public class Options {
        [Option("sl", Required = false, HelpText = "Стартовая буква для обработки", Default = "")]
        public string StartLetter { get; set; }
        
        [Option("sp", Required = false, HelpText = "Стартовая страница для обработки", Default = 1)]
        public int StartPage { get; set; }
        
        [Option("el", Required = false, HelpText = "Конечная буква для обработки", Default = "")]
        public string EndLetter { get; set; }
        
        [Option("ep", Required = false, HelpText = "Конечная страница для обработки", Default = 1)]
        public int EndPage { get; set; }
        
        [Option("proxy", Required = false, HelpText = "Прокси в формате <host>:<port>", Default = "")]
        public string Proxy { get; set; }
        
        [Option("pu", Required = false, HelpText = "Url для отправки данных. ", Default = null)]
        public string ProcessUrl { get; set; }
        
        [Option("error", Required = false, HelpText = "Максимальное кол-во ошибок после которых парсинг остановится", Default = int.MaxValue)]
        public int MaxErrorCount { get; set; }
    }
}
