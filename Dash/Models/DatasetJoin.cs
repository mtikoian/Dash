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
    public enum JoinTypes
    {
        Inner = 1,
        Left = 2,
        Right = 3
    }

    public class DatasetJoin : BaseModel, IEquatable<DatasetJoin>
    {
        Dataset _Dataset;

        public DatasetJoin() { }

        public DatasetJoin(IDbContext dbContext) => DbContext = dbContext;

        public DatasetJoin(IDbContext dbContext, int datasetId)
        {
            DbContext = dbContext;
            DatasetId = datasetId;
        }

        [BindNever, ValidateNever]
        public Dataset Dataset
        {
            get => _Dataset ?? (_Dataset = DbContext.Get<Dataset>(DatasetId));
            set => _Dataset = value;
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatasetId { get; set; }

        [DbIgnore]
        public bool IsLast { get; set; }

        [DbIgnore]
        public string JoinName => ((JoinTypes)JoinTypeId).ToString();

        [Required]
        public int JoinOrder { get; set; }

        [Display(Name = "JoinType", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int JoinTypeId { get; set; }

        [DbIgnore]
        public IEnumerable<SelectListItem> JoinTypeList => typeof(JoinTypes).TranslatedSelect(new ResourceDictionary("Datasets"), "LabelJoinType_");

        [Display(Name = "JoinKeys", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(500, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Keys { get; set; }

        [Display(Name = "JoinTableName", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string TableName { get; set; }

        public bool Equals(DatasetJoin other) => other.DatasetId == DatasetId && other.TableName == TableName && other.JoinTypeId == JoinTypeId && other.Keys == Keys && other.JoinOrder == JoinOrder;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals(obj as DatasetJoin);
        }

        public override int GetHashCode() => throw new NotImplementedException();

        public bool MoveDown(out string error)
        {
            error = "";
            var joins = DbContext.GetAll<DatasetJoin>(new { DatasetId }).ToList();
            if (JoinOrder == joins.Count - 1)
            {
                // can't move any higher
                error = Datasets.ErrorAlreadyLastJoin;
                return false;
            }
            var join = joins.First(x => x.JoinOrder == JoinOrder + 1);
            DbContext.WithTransaction(() => {
                join.JoinOrder--;
                DbContext.Save(join);

                JoinOrder++;
                DbContext.Save(this);
            });
            return true;
        }

        public bool MoveUp(out string error)
        {
            error = "";
            if (JoinOrder == 0)
            {
                // can't move any lower
                error = Datasets.ErrorAlreadyFirstJoin;
                return false;
            }
            var join = DbContext.GetAll<DatasetJoin>(new { DatasetId }).First(x => x.JoinOrder == JoinOrder - 1);
            join.JoinOrder++;
            DbContext.WithTransaction(() => {
                DbContext.Save(join);
                JoinOrder--;
                DbContext.Save(this);
            });
            return true;
        }
    }
}
