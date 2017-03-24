namespace ClientApplication
{
    public class Context
    {
        public static string CurrentUser
        {
            get { return "rares"; }
        }

        public static bool InAutoMode { get; set; }
    }
}
