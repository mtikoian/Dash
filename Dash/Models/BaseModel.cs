using System;
using Dash.Configuration;
using Jil;
using Microsoft.Extensions.Caching.Memory;

namespace Dash.Models
{
    /// <summary>
    /// Base for all other models.
    /// </summary>
    public class BaseModel
    {
        public BaseModel()
        {
        }

        public BaseModel(IDbContext dbContext, IMemoryCache cache, IAppConfiguration appConfig)
        {
            DbContext = dbContext;
            Cache = cache;
            AppConfig = appConfig;
        }

        [JilDirective(true)]
        public IAppConfiguration AppConfig { get; set; }

        [Ignore, JilDirective(true)]
        public IMemoryCache Cache { get; set; }

        [Ignore, JilDirective(true)]
        public DateTimeOffset DateCreated { get; set; }

        [Ignore, JilDirective(true)]
        public DateTimeOffset DateUpdated { get; set; }

        [JilDirective(true)]
        public IDbContext DbContext { get; set; }

        [Ignore, JilDirective(true)]
        public string FormAction { get { return IsCreate ? "Create" : "Edit"; } }

        [Ignore, JilDirective(true)]
        public HttpVerbs FormMethod { get { return IsCreate ? HttpVerbs.Post : HttpVerbs.Put; } }

        public int Id { get; set; }

        [Ignore, JilDirective(true)]
        public bool IsCreate { get { return Id == 0; } }

        public int? RequestUserId { get; set; }

        /// <summary>
        /// Gets an object from the cache. Cache persists for the lifetime of the app pool.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="key">Unique key to identify the object.</param>
        /// <param name="onCreate">If object doesn't exist in cache, use return value from this function to create the object.</param>
        /// <returns>Returns the matched object.</returns>
        public T Cached<T>(string key, Func<T> onCreate) where T : class {
            if (Cache == null) {
                return onCreate();
            }

            T result;
            if (!Cache.TryGetValue<T>(key, out result)) {
                result = onCreate();
                Cache.Set(key, result);
            }
            return result;
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