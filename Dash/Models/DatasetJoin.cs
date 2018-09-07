﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Dash.Utils;
using Jil;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public enum JoinTypes
    {
        Inner = 1,
        Left = 2,
        Right = 3
    }

    public class DatasetJoin : BaseModel
    {
        public DatasetJoin()
        {
        }

        public DatasetJoin(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public DatasetJoin(IDbContext dbContext, int datasetId)
        {
            DbContext = dbContext;
            DatasetId = datasetId;
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatasetId { get; set; }

        [Ignore]
        public bool IsLast { get; set; }

        [Required]
        public int JoinOrder { get; set; }

        [Display(Name = "JoinType", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int JoinTypeId { get; set; }

        [Ignore, JilDirective(true)]
        public IEnumerable<SelectListItem> JoinTypeList
        {
            get
            {
                return typeof(JoinTypes).TranslatedSelect(new ResourceDictionary("Datasets"), "LabelJoinType_");
            }
        }

        [Ignore]
        public string JoinName
        {
            get
            {
                return ((JoinTypes)JoinTypeId).ToString();
            }
        }

        [Display(Name = "JoinKeys", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(500, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Keys { get; set; }

        [Display(Name = "JoinTableName", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string TableName { get; set; }

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
                return this;
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
                return this;
            });
            return true;
        }
    }
}
