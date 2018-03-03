using Dapper;
using Dash.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;

namespace Dash
{
    public class CustomMembershipProvider : MembershipProvider
    {
        private const int _Iterations = 1000;

        public override string ApplicationName { get; set; }

        public override bool EnablePasswordReset
        {
            get { return true; }
        }

        public override bool EnablePasswordRetrieval { get; }
        public override int MaxInvalidPasswordAttempts { get; }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return 1; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return 6; }
        }

        public override int PasswordAttemptWindow { get; }

        public override MembershipPasswordFormat PasswordFormat { get; }

        public override string PasswordStrengthRegularExpression { get; }

        public override bool RequiresQuestionAndAnswer { get; }

        public override bool RequiresUniqueEmail
        {
            get { return false; }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            if (!ValidateUser(username, oldPassword))
            {
                return false;
            }

            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPassword, true);
            OnValidatingPassword(args);
            if (args.Cancel)
            {
                if (args.FailureInformation != null)
                {
                    throw args.FailureInformation;
                }
                else
                {
                    throw new MembershipPasswordException(MembershipCreateStatus.InvalidPassword.ToString());
                }
            }

            var user = GetUser(username, true);
            if (user != null)
            {
                var salt = GenerateSalt(200);
                var d = new DynamicParameters();
                d.Add("ID", null, System.Data.DbType.Int32, System.Data.ParameterDirection.InputOutput);
                d.Add("UID", username);
                d.Add("Email", user.Email);
                d.Add("Password", GenerateHash(newPassword, salt, _Iterations, 200));
                d.Add("Salt", salt);
                try
                {
                    Willow.Execute("UserMembershipSave", d);
                    return true;
                }
                catch { }
            }

            return false;
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            var args = new ValidatePasswordEventArgs(username, password, true);
            OnValidatingPassword(args);
            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (RequiresUniqueEmail && GetUserNameByEmail(email) != string.Empty)
            {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            var user = GetUser(username, true);
            if (user == null)
            {
                var salt = GenerateSalt(200);
                var d = new DynamicParameters();
                d.Add("ID", null, System.Data.DbType.Int32, System.Data.ParameterDirection.InputOutput);
                d.Add("UID", username);
                d.Add("Email", email);
                d.Add("Password", GenerateHash(password, salt, _Iterations, 200));
                d.Add("Salt", salt);
                try
                {
                    Willow.Execute("UserMembershipSave", d);
                    status = MembershipCreateStatus.Success;
                    return GetUser(username, true);
                }
                catch
                {
                    status = MembershipCreateStatus.UserRejected;
                    return null;
                }
            }
            status = MembershipCreateStatus.DuplicateUserName;

            return null;
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            var userMem = Willow.GetAll<User>(new { UID = username }).FirstOrDefault();
            if (userMem != null)
            {
                var memUser = new MembershipUser(Membership.Provider.Name, username, userMem.UID, userMem.Email,
                    string.Empty, string.Empty, true, false, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.Now, DateTime.Now);
                return memUser;
            }
            return null;
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public override string ResetPassword(string username, string answer)
        {
            if (!EnablePasswordReset)
            {
                throw new NotSupportedException("Password reset is not enabled.");
            }

            var userMem = Willow.GetAll<User>(new { UID = username }).FirstOrDefault();
            if (userMem != null)
            {
                string newPassword = Membership.GeneratePassword(Membership.MinRequiredPasswordLength, MinRequiredNonAlphanumericCharacters);

                ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPassword, true);
                OnValidatingPassword(args);
                if (args.Cancel)
                {
                    if (args.FailureInformation != null)
                    {
                        throw args.FailureInformation;
                    }
                    else
                    {
                        throw new MembershipPasswordException("Reset password canceled due to password validation failure.");
                    }
                }

                var salt = GenerateSalt(200);
                var d = new DynamicParameters();
                d.Add("ID", null, System.Data.DbType.Int32, System.Data.ParameterDirection.InputOutput);
                d.Add("UID", username);
                d.Add("Email", userMem.Email);
                d.Add("Password", GenerateHash(newPassword, salt, _Iterations, 200));
                d.Add("Salt", salt);
                try
                {
                    Willow.Execute("UserMembershipSave", d);
                    return newPassword;
                }
                catch { }
            }
            throw new MembershipPasswordException("User not found, or user is locked out. Password not Reset.");
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        public override bool ValidateUser(string username, string password)
        {
            var userMem = Willow.GetAll<User>(new { UID = username }).FirstOrDefault();
            if (userMem != null)
            {
                var hash = GenerateHash(password, userMem.Salt, _Iterations, 200);
                return hash == userMem.Password;
            }
            return false;
        }

        private string GenerateHash(string password, string salt, int iterations, int length)
        {
            byte[] saltBytes = Encoding.ASCII.GetBytes(salt);
            using (var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, iterations))
            {
                return Encoding.ASCII.GetString(deriveBytes.GetBytes(length));
            }
        }

        private string GenerateSalt(int length)
        {
            var bytes = new byte[length];

            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }

            return Encoding.ASCII.GetString(bytes);
        }
    }
}