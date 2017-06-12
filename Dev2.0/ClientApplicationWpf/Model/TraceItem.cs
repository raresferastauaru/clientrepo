namespace ClientApplicationWpf.Model
{
    public class TraceItem
    {
        private string _text;

        public string Text
        {
            get
            {
                return _text;
            }
        }

        public TraceItem(string text)
        {
            _text = text;
        }
    }
}
