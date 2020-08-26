namespace RslParser.Types {
    public class SearchLetter {
        public readonly string Letter;
        public int StartPage;
        public int EndPage;

        public SearchLetter(string letter) {
            Letter = letter;
            StartPage = 1;
            EndPage = int.MaxValue;
        }

        public override int GetHashCode() {
            return Letter.GetHashCode();
        }

        public override bool Equals(object? obj) {
            if (!(obj is SearchLetter)) {
                return false;
            }

            return Letter == ((SearchLetter) obj).Letter;
        }
    }
}
