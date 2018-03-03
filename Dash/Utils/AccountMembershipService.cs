using Dash.I18n;
using System;
using System.Web.Security;

namespace Dash
{
    public class AccountMembershipService : IMembershipService
    {
        private readonly MembershipProvider _provider;

        public AccountMembershipService()
            : this(null)
        {
        }

        public AccountMembershipService(MembershipProvider provider)
        {
            _provider = provider ?? Membership.Provider;
        }

        public int MinPasswordLength
        {
            get
            {
                return _provider.MinRequiredPasswordLength;
            }
        }

        public static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return Account.ErrorDuplicateUserName;
                case MembershipCreateStatus.DuplicateEmail:
                    return Account.ErrorDuplicateEmail;
                case MembershipCreateStatus.InvalidPassword:
                    return Account.ErrorInvalidPassword;
                case MembershipCreateStatus.InvalidEmail:
                    return Account.ErrorInvalidEmail;
                case MembershipCreateStatus.InvalidAnswer:
                    return Account.ErrorInvalidAnswer;
                case MembershipCreateStatus.InvalidQuestion:
                    return Account.ErrorInvalidQuestion;
                case MembershipCreateStatus.InvalidUserName:
                    return Account.ErrorInvalidUserName;
                case MembershipCreateStatus.ProviderError:
                    return Account.ErrorProviderError;
                case MembershipCreateStatus.UserRejected:
                    return Account.ErrorUserRejected;
                default:
                    return Account.ErrorGeneric;
            }
        }

        public bool ChangePassword(string userName, string oldPassword, string newPassword)
        {
            if (userName.IsEmpty()) throw new ArgumentException("Value cannot be null or empty.", "userName");
            if (oldPassword.IsEmpty()) throw new ArgumentException("Value cannot be null or empty.", "oldPassword");
            if (newPassword.IsEmpty()) throw new ArgumentException("Value cannot be null or empty.", "newPassword");

            // The underlying ChangePassword() will throw an exception rather
            // than return false in certain failure scenarios.
            try
            {
                MembershipUser currentUser = _provider.GetUser(userName, true /* userIsOnline */);
                return currentUser.ChangePassword(oldPassword, newPassword);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (MembershipPasswordException)
            {
                return false;
            }
        }

        public MembershipCreateStatus CreateUser(string userName, string password, string email)
        {
            if (userName.IsEmpty()) throw new ArgumentException("Value cannot be null or empty.", "userName");
            if (password.IsEmpty()) throw new ArgumentException("Value cannot be null or empty.", "password");
            if (email.IsEmpty()) throw new ArgumentException("Value cannot be null or empty.", "email");

            MembershipCreateStatus status;
            _provider.CreateUser(userName, password, email, null, null, true, null, out status);
            return status;
        }

        public string ResetPassword(string username, string answer)
        {
            if (username.IsEmpty()) throw new ArgumentException("Value cannot be null or empty.", "userName");
            return _provider.ResetPassword(username, answer);
        }

        public bool ValidateUser(string userName, string password)
        {
            if (userName.IsEmpty()) throw new ArgumentException("Value cannot be null or empty.", "userName");
            if (password.IsEmpty()) throw new ArgumentException("Value cannot be null or empty.", "password");
            return _provider.ValidateUser(userName, password);
        }
    }
}