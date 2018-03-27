using System.Collections.Generic;
using System.Linq;
using Dash.I18n;
using Jil;

namespace Dash
{
    public enum TableDataType
    {
        String,
        Int,
        Date,
        Currency
    }

    public enum TableIcon
    {
        Edit,
        Trash,
        Clone,
        HeartBeat,
        Database
    }

    public class Table
    {
        public Table()
        {
        }

        public Table(string id, string url, List<TableColumn> columns)
        {
            Id = id;
            Url = url;
            Columns = columns;
        }

        public List<TableColumn> Columns { get; set; }
        public IEnumerable<object> Data { get; set; }
        public string Id { get; set; }
        public bool LoadAllData { get; set; } = true;
        public bool Searchable { get; set; } = true;

        [JilDirective(true)]
        public string ToJson { get { return JSON.Serialize(this, JilOutputFormatter.Options); } }

        public string Url { get; set; }

        public static TableLink CopyButton(string url, string controller, string body, bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return null;
            }
            return new TableLink(url, Html.Classes(DashClasses.DashPrompt, DashClasses.BtnInfo)
                .Merge("data-message", body), Core.Copy, TableIcon.Clone);
        }

        public static TableLink DeleteButton(string url, string controller, string message, bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return null;
            }
            return new TableLink(url,
                Html.Classes(DashClasses.DashConfirm, DashClasses.BtnDanger).Merge("data-title", Core.ConfirmDelete).Merge("data-message", message),
                Core.Delete, TableIcon.Trash, HttpVerbs.Delete);
        }

        public static TableLink EditButton(string url, string controller, string action = "Edit", bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return null;
            }
            return new TableLink(url, Html.Classes(DashClasses.DashDialog, DashClasses.BtnWarning), Core.Edit, TableIcon.Edit);
        }

        public static TableLink EditLink(string url, string controller, string action = "Edit", bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return null;
            }
            return new TableLink(url, Html.Classes(DashClasses.DashDialog, DashClasses.BtnLink));
        }
    }

    public class TableColumn
    {
        public TableColumn()
        {
        }

        public TableColumn(string field, string label, bool sortable = true, TableDataType dataType = TableDataType.String, List<TableLink> links = null)
        {
            Field = field;
            Label = label;
            Sortable = sortable;
            DataType = dataType;
            Links = links;
        }

        public TableColumn(string field, string label, TableLink link)
        {
            Field = field;
            Label = label;
            Links = new List<TableLink> { link };
        }

        public TableDataType DataType { get; set; } = TableDataType.String;
        public string Field { get; set; }
        public string Label { get; set; }
        public List<TableLink> Links { get; set; }
        public bool Sortable { get; set; } = true;
        public decimal Width { get; set; }
    }

    public class TableColumnWidth
    {
        public string Field { get; set; }
        public decimal Width { get; set; }
    }

    public class TableLink
    {
        public TableLink(string href, Dictionary<string, object> attributes = null, string label = null, TableIcon? icon = null, HttpVerbs method = HttpVerbs.Get)
        {
            Href = href;
            Attributes = attributes?.ToDictionary(k => k.Key, v => v.Value.ToString());
            Label = label;
            Icon = icon;
            Method = method;
        }

        public Dictionary<string, string> Attributes { get; set; }
        public string Href { get; set; }
        public TableIcon? Icon { get; set; }
        public string Label { get; set; }
        public HttpVerbs Method { get; set; } = HttpVerbs.Get;
    }

    public class TableSorting
    {
        public string DataType { get; set; }
        public string Dir { get; set; }
        public string Field { get; set; }
        public int Index { get; set; }
    }
}
