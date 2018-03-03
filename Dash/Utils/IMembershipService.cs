using System.Web.Security;

namespace Dash
{
    public interface IMembershipService
    {
        int MinPasswordLength { get; }

        bool ChangePassword(string userName, string oldPassword, string newPassword);

        MembershipCreateStatus CreateUser(string userName, string password, string email);

        bool ValidateUser(string userName, string password);
    }
}