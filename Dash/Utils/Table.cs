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
        Database,
        Unlock,
        Up,
        Down
    }

    public class Table
    {
        public Table()
        {
        }

        public Table(string id, string url, IEnumerable<TableColumn> columns)
        {
            Id = id;
            Url = url;
            Columns = columns;
        }

        public IEnumerable<TableColumn> Columns { get; set; }
        public IEnumerable<object> Data { get; set; }
        public string Id { get; set; }
        public bool LoadAllData { get; set; } = true;
        public bool Searchable { get; set; } = true;

        public string Url { get; set; }

        [JilDirective(true)]
        public string ToJson { get { return JSON.SerializeDynamic(this, JilOutputFormatter.Options); } }

        public static TableLink CopyButton(string href, string prompt, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href, Html.Classes(DashClasses.DashPrompt, DashClasses.BtnInfo)
                .Merge("data-prompt", prompt), Core.Copy, TableIcon.Clone);
        }

        public static TableLink DeleteButton(string href, string confirm, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href,
                Html.Classes(DashClasses.DashConfirm, DashClasses.BtnError).Merge("data-confirm", confirm),
                Core.Delete, TableIcon.Trash, HttpVerbs.Delete);
        }

        public static TableLink EditButton(string href, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href, Html.Classes(DashClasses.BtnWarning), Core.Edit, TableIcon.Edit);
        }

        public static TableLink EditLink(string href, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href);
        }

        public static TableLink UpButton(string href, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href, Html.Classes(DashClasses.BtnInfo), Core.MoveUp, TableIcon.Up);
        }

        public static TableLink DownButton(string href, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href, Html.Classes(DashClasses.BtnInfo), Core.MoveDown, TableIcon.Down);
        }
    }

    public class TableColumn
    {
        public TableColumn()
        {
        }

        public TableColumn(string field, string label, bool sortable = true, TableDataType dataType = TableDataType.String, IEnumerable<TableLink> links = null)
        {
            Field = field;
            Label = label;
            Sortable = sortable;
            DataType = dataType;
            Links = links;
        }

        public TableColumn(string field, string label, TableLink link, bool sortable = true)
        {
            Field = field;
            Label = label;
            Links = new List<TableLink> { link };
        }

        public TableDataType DataType { get; set; } = TableDataType.String;
        public string Field { get; set; }
        public string Label { get; set; }
        public IEnumerable<TableLink> Links { get; set; }
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
        public TableLink(string href, Dictionary<string, object> attributes = null, string label = null, TableIcon? icon = null, HttpVerbs method = HttpVerbs.Get, object jsonLogic = null)
        {
            Href = href;
            Attributes = attributes?.ToDictionary(k => k.Key, v => v.Value.ToString());
            Label = label;
            Icon = icon;
            Method = method;
            JsonLogic = jsonLogic;
        }

        public Dictionary<string, string> Attributes { get; set; }
        public string Href { get; set; }
        public TableIcon? Icon { get; set; }
        public object JsonLogic { get; set; }
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
