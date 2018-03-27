using Microsoft.AspNetCore.Authorization;

namespace Dash
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement()
        {
        }
    }
}
