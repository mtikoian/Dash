namespace Dash.Models
{
    public enum DatasetViewTab
    {
        Overview,
        Joins,
        Columns
    }

    public class DatasetView
    {
        public DatasetView(int id, bool isCreate, DatasetViewTab activeTab)
        {
            Id = id;
            IsCreate = isCreate;
            ActiveTab = activeTab;
        }

        public DatasetViewTab ActiveTab { get; set; }
        public int Id { get; set; }
        public bool IsCreate { get; set; }
    }
}
