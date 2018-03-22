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

    /// <summary>
    /// Table defines the settings passed to the Table.js component to generate a table from JSON data.
    /// </summary>
    public class Table
    {
        public Table()
        {
        }

        /// <summary>
        /// Table constructor.
        /// </summary>
        /// <param name="id">Identifier for table.</param>
        /// <param name="url">Url to load data from.</param>
        /// <param name="columns">List of table columns.</param>
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

        /// <summary>
        /// Convert the model to a json string.
        /// </summary>
        [JilDirective(true)]
        public string ToJson { get { return JSON.Serialize(this, JilOutputFormatter.Options); } }

        public string Url { get; set; }

        /// <summary>
        /// Create a icon button link for an Table column to copy a model.
        /// </summary>
        /// <param name="url">URL for the new link.</param>
        /// <param name="controller">Name of controller used for copy.</param>
        /// <param name="body">Body text for copy dialog.</param>
        /// <param name="hasAccess">User has access.</param>
        /// <returns>Returns new button link.</returns>
        public static TableLink CopyButton(string url, string controller, string body, bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return null;
            }
            return new TableLink(url, Html.Classes(DashClasses.DashPrompt, DashClasses.BtnInfo)
                .Merge("data-message", body), Core.Copy, TableIcon.Clone);
        }

        /// <summary>
        /// Create a icon button link for an Table column to edit a model.
        /// </summary>
        /// <param name="url">URL for the new link.</param>
        /// <param name="controller">Name of controller used for edit.</param>
        /// <param name="message">Delete confirmation message.</param>
        /// <param name="hasAccess">User has access.</param>
        /// <returns>Returns new button link.</returns>
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

        /// <summary>
        /// Create a icon button link for an Table column to edit a model.
        /// </summary>
        /// <param name="url">URL for the new link.</param>
        /// <param name="controller">Name of controller used for edit.</param>
        /// <param name="controller">Name of action used for edit. Defaults to `Edit`.</param>
        /// <param name="hasAccess">User has access.</param>
        /// <returns>Returns new button link.</returns>
        public static TableLink EditButton(string url, string controller, string action = "Edit", bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return null;
            }
            return new TableLink(url, Html.Classes(DashClasses.DashDialog, DashClasses.BtnWarning), Core.Edit, TableIcon.Edit);
        }

        /// <summary>
        /// Create a link for an Table column to edit a model.
        /// </summary>
        /// <param name="url">URL for the new link.</param>
        /// <param name="controller">Name of controller used for edit.</param>
        /// <param name="controller">Name of action used for edit. Defaults to `Edit`.</param>
        /// <param name="hasAccess">User has access.</param>
        /// <returns>Returns new link.</returns>
        public static TableLink EditLink(string url, string controller, string action = "Edit", bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return null;
            }
            return new TableLink(url, Html.Classes(DashClasses.DashDialog, DashClasses.BtnLink));
        }
    }

    // Defines a single column in the table.
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
