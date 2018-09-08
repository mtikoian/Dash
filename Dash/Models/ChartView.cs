namespace Dash.Models
{
    public enum ChartViewTab
    {
        Edit,
        Ranges,
        ChangeType,
        Rename,
        Share,
        Export,
        Sql
    }

    public class ChartView
    {
        public ChartView(int id, bool isOwner, ChartViewTab activeTab)
        {
            Id = id;
            IsOwner = isOwner;
            ActiveTab = activeTab;
        }

        public ChartViewTab ActiveTab { get; set; }
        public int Id { get; set; }
        public bool IsOwner { get; set; }
    }
}
