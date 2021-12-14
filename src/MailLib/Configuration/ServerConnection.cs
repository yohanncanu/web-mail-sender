namespace MailLib.Configuration
{
    public class ServerConnection
    {
        public string SmtpServer { get; init; } = "localhost";
        public int SmtpPort { get; init; } = 587;
        public string? Username { get; init; }
        public string? Password { get; init; }
        public int Timeout { get; init; } = 10000;
        public bool NoSsl { get; init; }
        public bool Debug { get; init; }

    }
}