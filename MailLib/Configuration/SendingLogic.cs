namespace MailLib.Configuration;
public class SendingLogic
{

    public int MaxFailures { get; set; } = 10;
    public int MaxNrDays { get; set; } = 5;
    public bool DoNotSendEmail { get; set; }
    public bool SaveEmail { get; set; }

}
