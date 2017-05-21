using GalaSoft.MvvmLight;
using System;
namespace ClientApplicationWpf.ViewModel
{
    public class TraceItemViewModel : ViewModelBase
    {
        private string _text;

        public string Text
        {
            get
            {
                return _text;
            }
        }

        public TraceItemViewModel(string text)
        {
            _text = text;
        }
    }
}
