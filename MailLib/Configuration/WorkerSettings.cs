namespace MailLib.Configuration;
public class WorkerSettings
{
    public int IdleTimeInMinutes { get; set; } = 60;
    public int MaxNumberToProcess { get; set; } = 100;
}
