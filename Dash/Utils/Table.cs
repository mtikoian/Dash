using System.Collections.Generic;
using System.Linq;
using Dash.I18n;

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
        Unlock
    }

    public class Table
    {
        public Table()
        {
        }

        public Table(string id, string url, IEnumerable<TableColumn> columns, IEnumerable<TableHeaderButton> headerButtons = null, IEnumerable<object> data = null)
        {
            Id = id;
            Url = url;
            Columns = columns;
            HeaderButtons = headerButtons;
            Data = data;
        }

        public IEnumerable<TableColumn> Columns { get; set; }
        public IEnumerable<object> Data { get; set; }
        public IEnumerable<TableHeaderButton> HeaderButtons { get; set; }
        public string Id { get; set; }
        public bool LoadAllData { get; set; } = true;
        public bool Searchable { get; set; } = true;

        public string Url { get; set; }

        public static TableLink CopyButton(string href, string message, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href, Html.Classes(DashClasses.DashPrompt, DashClasses.BtnInfo)
                .Merge("data-message", message), Core.Copy, TableIcon.Clone);
        }

        public static TableHeaderButton CreateButton(string href, string label, bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return null;
            }
            var attr = Html.Classes(DashClasses.BtnPrimary, DashClasses.DashAjax);
            attr["type"] = "button";
            attr["role"] = "button";
            attr["data-href"] = href;
            attr["data-method"] = "GET";
            return new TableHeaderButton(attr, label);
        }

        public static TableLink DeleteButton(string href, string message, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href,
                Html.Classes(DashClasses.DashConfirm, DashClasses.BtnError).Merge("data-title", Core.ConfirmDelete).Merge("data-message", message),
                Core.Delete, TableIcon.Trash, HttpVerbs.Delete);
        }

        public static TableLink EditButton(string href, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href, Html.Classes(DashClasses.DashAjax, DashClasses.BtnWarning), Core.Edit, TableIcon.Edit);
        }

        public static TableLink EditLink(string href, bool hasAccess = true)
        {
            return !hasAccess ? null : new TableLink(href, Html.Classes(DashClasses.DashAjax));
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

        public TableColumn(string field, string label, TableLink link)
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

    public class TableHeaderButton
    {
        public TableHeaderButton(Dictionary<string, object> attributes = null, string label = null)
        {
            Attributes = attributes?.ToDictionary(k => k.Key, v => v.Value.ToString());
            Label = label;
        }

        public Dictionary<string, string> Attributes { get; set; }
        public string Label { get; set; }
        public string Type { get; set; } = "button";
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
