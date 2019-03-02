using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    public class Profiling
    {
        private ISession _Session;
        public const string SettingName = "Profiling";

        public Profiling(ISession session) => _Session = session;

        public bool IsEnabled => _Session.GetString(SettingName).ToBool();
    }
}
