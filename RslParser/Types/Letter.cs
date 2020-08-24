namespace RslParser.Types {
    public class SearchLetter {
        public string Letter;
        public int StartPage;
        public int EndPage;

        public SearchLetter() {
            StartPage = 1;
            EndPage = int.MaxValue;
        }
    }
}
