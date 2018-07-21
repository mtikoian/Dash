using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    public class Help
    {
        private ISession _Session;

        public Help(ISession session)
        {
            _Session = session;
        }

        public bool IsEnabled
        {
            get
            {
                return _Session.GetString("ContextHelp").ToBool();
            }
        }
    }
}
