

namespace DataBase.Types;

public class ButtonData
{
    public required string ButtonText { get; set; }
    public required string CallbackData { get; set; }
}

public class ContactsStatus
{
    public const string WAITING_FOR_ACCEPT = "waiting_for_accept";
    public const string ACCEPTED = "accepted";
    public const string DECLINED = "declined";
}