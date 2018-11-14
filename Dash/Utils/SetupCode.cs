namespace Dash.Utils
{
    public class SetupCode
    {
        public SetupCode()
        {
        }

        public SetupCode(string account, string manualEntryKey, string qrCode)
        {
            Account = account;
            ManualEntryKey = manualEntryKey;
            QrCode = qrCode;
        }

        public string Account { get; internal set; }
        public string ManualEntryKey { get; internal set; }
        public string QrCode { get; internal set; }
    }
}
