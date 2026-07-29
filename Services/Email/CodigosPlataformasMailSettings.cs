namespace Tienda_Streaming.Services.Email
{
    public class CodigosPlataformasMailSettings
    {
        public bool Enabled { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 993;
        public bool UseSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Folder { get; set; } = "INBOX";
        public int PollIntervalSeconds { get; set; } = 60;
        public int MaxMessagesPerSync { get; set; } = 80;
        public int RetentionHours { get; set; } = 24;
        public int CleanupHourLocal { get; set; } = 3;
        public bool DeleteFromMailboxAfterImport { get; set; } = true;
    }
}
