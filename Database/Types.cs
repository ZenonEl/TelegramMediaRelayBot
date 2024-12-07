

namespace DataBase;

public class ButtonData
{
    public string ButtonText { get; set; }
    public string CallbackData { get; set; }
}

public class ContactsStatus
{
    public static string WaitingForAccept = "waiting_for_accept";
    public static string Accepted = "accepted";
}