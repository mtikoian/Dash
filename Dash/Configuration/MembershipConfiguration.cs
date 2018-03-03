﻿using Dash.Utils;
using System.Linq;
using System;

namespace Dash.Configuration
{
    public class MembershipConfiguration
    {
        public string Scheme { get; set; }
        public int MinRequiredPasswordLength { get; set; }
        public int MinRequiredNonAlphanumericCharacters { get; set; }
        public int MaxLoginAttempts { get; set; }
        public int LoginAttemptsLockDuration { get; set; }
        public string AuthenticatorAppName { get; set; }
        public string AuthenticatorKey { get; set; }

        public bool IsValidPassword(string password)
        {
            return !(password.IsEmpty() || password.Length < MinRequiredPasswordLength || password.ToCharArray().Count(c => !Char.IsLetterOrDigit(c)) < MinRequiredNonAlphanumericCharacters);
        }
    }
}
