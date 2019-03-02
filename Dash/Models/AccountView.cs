namespace Dash.Models
{
    public enum AccountViewTab
    {
        Account,
        Password
    }

    public class AccountView
    {
        public AccountView(AccountViewTab activeTab) => ActiveTab = activeTab;

        public AccountViewTab ActiveTab { get; set; }
    }
}
