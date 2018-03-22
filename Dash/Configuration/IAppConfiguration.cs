namespace Dash.Configuration
{
    public interface IAppConfiguration
    {
        string CryptKey { get; }
        DatabaseConfiguration Database { get; }
        bool IsDevelopment { get; }
        MailConfiguration Mail { get; }
        MembershipConfiguration Membership { get; }
    }
}
