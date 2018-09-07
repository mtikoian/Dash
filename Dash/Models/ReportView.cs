namespace Dash.Models
{
    public enum ReportViewTab
    {
        Edit,
        Filters,
        Groups,
        Columns,
        Rename,
        Share,
        Export,
        Sql
    }

    public class ReportView
    {
        public ReportView(int id, bool isOwner, ReportViewTab activeTab)
        {
            Id = id;
            IsOwner = isOwner;
            ActiveTab = activeTab;
        }

        public ReportViewTab ActiveTab { get; set; }
        public int Id { get; set; }
        public bool IsOwner { get; set; }
    }
}
