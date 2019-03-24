using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    public class Help
    {
        ISession _Session;
        public const string SettingName = "ContextHelp";

        public Help(ISession session) => _Session = session;

        public bool IsEnabled => _Session.GetString(SettingName).ToBool();
    }
}
