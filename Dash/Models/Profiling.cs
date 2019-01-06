using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    public class Profiling
    {
        public const string SettingName = "Profiling";

        private ISession _Session;

        public Profiling(ISession session)
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
