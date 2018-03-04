namespace Dash.Configuration
{
    public interface IAppConfiguration
    {
        bool IsDevelopment { get; }
        string CryptKey { get; }
        DatabaseConfiguration Database { get; }
        MembershipConfiguration Membership { get; }
        MailConfiguration Mail { get; }
    }
}
