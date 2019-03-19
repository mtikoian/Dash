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
        public BaseModel() { }

        public BaseModel(IDbContext dbContext, IAppConfiguration appConfig)
        {
            DbContext = dbContext;
            AppConfig = appConfig;
        }

        [DbIgnore, JilDirective(true)]
        public IAppConfiguration AppConfig { get; set; }

        [DbIgnore, JilDirective(true)]
        public IMemoryCache Cache { get; set; }

        [DbIgnore, JilDirective(true)]
        public DateTimeOffset DateCreated { get; set; }

        [DbIgnore, JilDirective(true)]
        public DateTimeOffset DateUpdated { get; set; }

        [DbIgnore, JilDirective(true)]
        public IDbContext DbContext { get; set; }

        [DbIgnore, JilDirective(true)]
        public string FormAction => IsCreate ? "Create" : "Edit";

        [DbIgnore, JilDirective(true)]
        public HttpVerbs FormMethod => IsCreate ? HttpVerbs.Post : HttpVerbs.Put;

        [DbIgnore, JilDirective(true)]
        public bool ForSave { get; set; } = false;

        public int Id { get; set; }

        [DbIgnore, JilDirective(true)]
        public bool IsCreate => Id == 0;

        [JilDirective(true)]
        public int? RequestUserId { get; set; }

        public override void SetAppConfig(IAppConfiguration appConfig) => AppConfig = appConfig;

        public override void SetDbContext(IDbContext dbContext) => DbContext = dbContext;

        public override void SetForSave(bool forSave) => ForSave = forSave;

        public override void SetRequestUserId(int? userId) => RequestUserId = userId;
    }

    /// <summary>
    /// Attribute that specifies a property should be ignored when inserting into or updating the db.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class DbIgnore : Attribute
    {
        public DbIgnore() { }
    }
}
