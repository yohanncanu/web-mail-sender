namespace MailLib.Configuration;
public class MailSettings
{
    public MessageSettings MessageSettings { get; set; }
    public ServerConnection ServerConnection { get; set; } = new ServerConnection();
    public SendingLogic SendingLogic { get; set; } = new SendingLogic();

}
