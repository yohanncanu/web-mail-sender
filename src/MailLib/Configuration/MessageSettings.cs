namespace MailLib.Configuration;
public class MessageSettings
{
    public string TenantId { get; set; }
    public string Sender { get; set; }
    public bool AddPlainTextVersion { get; set; }
    public string? ForceRecipientForDemo { get; set; }
}
