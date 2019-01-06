using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    public class Help
    {
        public const string SettingName = "ContextHelp";

        private ISession _Session;

        public Help(ISession session)
        {
            _Session = session;
        }

        public bool IsEnabled
        {
            get
            {
                return _Session.GetString(SettingName).ToBool();
            }
        }
    }
}
