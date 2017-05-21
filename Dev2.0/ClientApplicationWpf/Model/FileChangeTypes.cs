namespace ClientApplicationWpf.Model
{
    public enum FileChangeTypes
    {
        None,

        ChangedOnClient,
        CreatedOnClient,
        DeletedOnClient,
        RenamedOnClient,

        // CreatedOnServer nu e nevoie de el pt ca rezolvam din Changed ierarhia de foldere ce nu exista
        ChangedOnServer,
        CreatedOnServer,
        DeletedOnServer,
        RenamedOnServer
    }
}