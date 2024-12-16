

namespace DataBase;

public class ButtonData
{
    public required string ButtonText { get; set; }
    public required string CallbackData { get; set; }
}

public class ContactsStatus
{
    public static string WaitingForAccept = "waiting_for_accept";
    public static string Accepted = "accepted";
}