using ClientApplicationWpf.Model;

namespace ClientApplicationWpf.Messages
{
    public class TraceItemAddedMsg
    {
        public TraceItem TraceItem { get; private set; }

        public TraceItemAddedMsg(string message)
        {
            TraceItem = new TraceItem(message);
        }
    }
}
