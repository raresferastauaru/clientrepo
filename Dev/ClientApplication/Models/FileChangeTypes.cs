namespace ClientApplication.Models
{
    public enum FileChangeTypes
    {
        None,

        ChangedOnClient,
        CreatedOnClient,
        DeletedOnClient,
        RenamedOnClient,

        // CreatedOnServer -> nu e nevoie de el pt ca rezolvam din ChangedOnClient? ierarhia de foldere ce nu exista
        ChangedOnServer,
        CreatedOnServer,
        DeletedOnServer,
        RenamedOnServer
    }
}
