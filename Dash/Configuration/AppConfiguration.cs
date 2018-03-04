namespace Dash.Configuration
{
    public class AppConfiguration : IAppConfiguration
    {
        public bool IsDevelopment { get; set; }
        public string CryptKey { get; set; }
        public DatabaseConfiguration Database { get; set; }
        public MembershipConfiguration Membership { get; set; }
        public MailConfiguration Mail { get; set; }
    }
}
