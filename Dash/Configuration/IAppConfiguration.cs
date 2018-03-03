﻿namespace Dash.Configuration
{
    public interface IAppConfiguration
    {
        bool IsDevelopment { get; }
        DatabaseConfiguration Database { get; }
        MembershipConfiguration Membership { get; }
        MailConfiguration Mail { get; }
    }
}
