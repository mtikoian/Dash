namespace Dash.Configuration 
{
    public class MailConfiguration
    {
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public MailServerConfiguration Smtp { get; set; }
    }

    public class MailServerConfiguration
    {
        public string Host { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
    }
}