using ClientApplicationWpf.ViewModel;

namespace ClientApplicationWpf.Messages
{
    public class TraceItemAddedMsg
    {
        public TraceItemViewModel TraceItem { get; private set; }

        public TraceItemAddedMsg(string message)
        {
            TraceItem = new TraceItemViewModel(message);
        }
    }
}
