using System;
using Dash.Configuration;
using Jil;
using Microsoft.Extensions.Caching.Memory;

namespace Dash.Models
{
    /// <summary>
    /// Base for all other models.
    /// </summary>
    public class BaseModel : IModel
    {
        public BaseModel()
        {
        }

        public BaseModel(IDbContext dbContext, IAppConfiguration appConfig)
        {
            DbContext = dbContext;
            AppConfig = appConfig;
        }

        [Ignore, JilDirective(true)]
        public IAppConfiguration AppConfig { get; set; }

        [Ignore, JilDirective(true)]
        public IMemoryCache Cache { get; set; }

        [Ignore, JilDirective(true)]
        public DateTimeOffset DateCreated { get; set; }

        [Ignore, JilDirective(true)]
        public DateTimeOffset DateUpdated { get; set; }

        [Ignore, JilDirective(true)]
        public IDbContext DbContext { get; set; }

        [Ignore, JilDirective(true)]
        public string FormAction { get { return IsCreate ? "Create" : "Edit"; } }

        [Ignore, JilDirective(true)]
        public HttpVerbs FormMethod { get { return IsCreate ? HttpVerbs.Post : HttpVerbs.Put; } }

        [Ignore, JilDirective(true)]
        public Table Table { get; set; }

        [Ignore, JilDirective(true)]
        public bool ForSave { get; set; } = false;

        public int Id { get; set; }

        [Ignore, JilDirective(true)]
        public bool IsCreate { get { return Id == 0; } }

        [JilDirective(true)]
        public int? RequestUserId { get; set; }

        public override void SetAppConfig(IAppConfiguration appConfig)
        {
            AppConfig = appConfig;
        }

        public override void SetDbContext(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public override void SetForSave(bool forSave)
        {
            ForSave = forSave;
        }

        public override void SetRequestUserId(int? userId)
        {
            RequestUserId = userId;
        }
    }

    /// <summary>
    /// Attribute for specifying has many relationships.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class HasMany : Attribute
    {
        public HasMany(Type childType)
        {
            ChildType = childType;
        }

        public Type ChildType { get; set; }
    }

    /// <summary>
    /// Attribute that specifies a property should be ignored when inserting into or updating the db.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class Ignore : Attribute
    {
        public Ignore()
        {
        }
    }
}
