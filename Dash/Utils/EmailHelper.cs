using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Dash.Utils
{
    public class EmailHelper
    {
        bool _Invalid = false;
        static Regex _DomainRegex = new Regex(@"(@)(.+)$",  RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

        static Regex _EmailRegex = new Regex(@"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

        string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            var idn = new IdnMapping();
            var domainName = match.Groups[2].Value;
            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                _Invalid = true;
            }
            return match.Groups[1].Value + domainName;
        }

        public bool IsValidEmail(string strIn)
        {
            _Invalid = false;
            if (strIn.IsEmpty())
                return false;

            // Use IdnMapping class to convert Unicode domain names.
            try
            {
                strIn = _DomainRegex.Replace(strIn, DomainMapper);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            if (_Invalid)
                return false;

            // Return true if strIn is in valid email format.
            try
            {
                return _EmailRegex.IsMatch(strIn);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
