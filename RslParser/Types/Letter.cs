namespace RslParser.Types {
    public class SearchLetter {
        public readonly string Letter;
        public int StartPage;
        public int EndPage;
        public string Lang;

        public SearchLetter(string letter, string lang) {
            Letter = letter;
            Lang = lang;
            StartPage = 1;
            EndPage = int.MaxValue;
        }

        public override int GetHashCode() {
            return Letter.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (!(obj is SearchLetter)) {
                return false;
            }

            return Letter == ((SearchLetter) obj).Letter;
        }
    }
}
