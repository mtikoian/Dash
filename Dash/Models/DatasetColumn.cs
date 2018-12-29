﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Dash.Utils;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public class DatasetColumn : BaseModel, IEquatable<DatasetColumn>
    {
        private Dataset _Dataset;
        private DataType _DataType;

        public DatasetColumn()
        {
        }

        public DatasetColumn(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public DatasetColumn(IDbContext dbContext, int datasetid)
        {
            DbContext = dbContext;
            DatasetId = datasetid;
        }

        [DbIgnore]
        public string Alias { get { return $"column{Id}"; } }

        [Display(Name = "ColumnName", ResourceType = typeof(Datasets))]
        [StringLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public string ColumnName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatasetId { get; set; }

        [BindNever, ValidateNever]
        public Dataset Dataset
        {
            get { return _Dataset ?? (_Dataset = DbContext.Get<Dataset>(DatasetId)); }
            set { _Dataset = value; }
        }

        [BindNever, ValidateNever]
        public DataType DataType
        {
            get { return _DataType ?? (_DataType = DbContext.Get<DataType>(DataTypeId)); }
            set { _DataType = value; }
        }

        [Display(Name = "ColumnDataType", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DataTypeId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<SelectListItem> DataTypeList
        {
            get { return DbContext.GetAll<DataType>().OrderBy(d => d.Name).ToSelectList(x => x.Name, x => x.Id.ToString()); }
        }

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<SelectListItem> FilterTypeList
        {
            get { return typeof(FilterTypes).TranslatedSelect(new ResourceDictionary("Filters"), "LabelType_"); }
        }

        [DbIgnore, BindNever, ValidateNever]
        public string DataTypeName
        {
            get { return DataType?.Name; }
        }

        [Display(Name = "ColumnTransform", ResourceType = typeof(Datasets))]
        [StringLength(500, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Derived { get; set; }

        [DbIgnore]
        public int DisplayOrder { get; set; }

        [Display(Name = "ColumnQuery", ResourceType = typeof(Datasets))]
        [StringLength(500, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string FilterQuery { get; set; }

        [Display(Name = "ColumnFilterType", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int FilterTypeId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsBinary { get { return DataType.IsBinary; } }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsBool { get { return DataType.IsBool; } }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsCurrency { get { return DataType.IsCurrency; } }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsDateTime { get { return DataType.IsDateTime; } }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsDecimal { get { return DataType.IsDecimal; } }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsInteger { get { return DataType.IsInteger; } }

        [Display(Name = "ColumnIsParam", ResourceType = typeof(Datasets))]
        public bool IsParam { get; set; }

        [DbIgnore]
        public bool IsSelect { get { return FilterTypeId == (int)FilterTypes.Select; } }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsText { get { return DataType.IsText; } }

        [Display(Name = "ColumnLink", ResourceType = typeof(Datasets))]
        [StringLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Link { get; set; }

        [DbIgnore]
        public int ReportColumnId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public TableDataType TableDataType
        {
            get
            {
                if ((FilterTypes)FilterTypeId == FilterTypes.Select)
                {
                    return TableDataType.String;
                }
                if (IsCurrency)
                {
                    return TableDataType.Currency;
                }
                if (IsInteger || IsDecimal)
                {
                    return TableDataType.Int;
                }
                if (IsDateTime)
                {
                    return TableDataType.Date;
                }
                return TableDataType.String;
            }
        }

        [DbIgnore, BindNever, ValidateNever]
        public string TableName
        {
            get
            {
                var loc = ColumnName.LastIndexOf(".");
                return loc > -1 ? ColumnName.Substring(0, loc) : "";
            }
        }

        [Display(Name = "ColumnTitle", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Title { get; set; }

        [DbIgnore]
        public int Width { get; set; }

        public string BuildSql(bool includeAlias = true, int aggregatorId = 0)
        {
            var columnSql = Derived.IsEmpty() ? ColumnName : Derived;
            if (columnSql.Length == 0)
            {
                return columnSql;
            }

            columnSql = $"({columnSql})";
            if (aggregatorId > 0)
            {
                if (IsBinary && DataType.Name.ToLower() != "binary")
                {
                    // handling for guids
                    columnSql = $"(CAST({columnSql} AS NVARCHAR(1000)))";
                }
                columnSql = $"({(IsText || IsDateTime ? "MAX" : ((Aggregators)aggregatorId).ToString().ToUpper())}{columnSql})";
            }
            return includeAlias ? $"{columnSql} AS {Alias}" : columnSql;
        }

        public bool Equals(DatasetColumn other)
        {
            return other.DatasetId == DatasetId && other.Title == Title && other.ColumnName == ColumnName && other.Derived == Derived
                && other.FilterTypeId == FilterTypeId && other.FilterQuery == FilterQuery && other.IsParam == IsParam && other.DataTypeId == DataTypeId
                && other.Link == Link;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(obj as DatasetColumn);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
