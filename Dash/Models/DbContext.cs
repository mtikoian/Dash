using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Dapper;
using Dash.Configuration;
using FastMember;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Dash.Models
{
    public class DbContext : IDbContext
    {
        /// <summary>
        /// Specifies the type of properties that should be included when building sql parameters to save an object.
        /// </summary>
        private static readonly Type[] SavableTypes = { typeof(string), typeof(bool), typeof(int), typeof(long), typeof(DateTime), typeof(DateTimeOffset),
            typeof(decimal), typeof(int?), typeof(long?), typeof(byte[]), typeof(Enum), typeof(double), typeof(DateTimeOffset?) };

        private AppConfiguration _AppConfig;
        private IMemoryCache _Cache;
        private IHttpContextAccessor _HttpContextAccessor;

        public DbContext(AppConfiguration config, IMemoryCache cache = null, IHttpContextAccessor httpContextAccessor = null)
        {
            _AppConfig = config;
            _Cache = cache;
            _HttpContextAccessor = httpContextAccessor;
        }

        public override void Delete(int id, Type type, int? requestUserId = null)
        {
            using (var conn = GetConnection())
            {
                conn.Execute($"{type.Name}Delete", new {
                    RequestUserId = requestUserId ?? _HttpContextAccessor.HttpContext?.User.UserId(),
                    Id = id
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public override void Delete<T>(T model)
        {
            var myType = model.GetType();
            using (var conn = GetConnection())
            {
                conn.Execute($"{myType.Name}Delete", new {
                    RequestUserId = model.RequestUserId ?? _HttpContextAccessor.HttpContext?.User.UserId(),
                    Id = myType.GetProperty("Id").GetValue(model)
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public override void Execute(string procName, object parameters)
        {
            using (var conn = GetConnection())
            {
                conn.Execute(procName, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public override T Get<T>(int id)
        {
            if (id == 0)
            {
                return null;
            }
            using (var conn = GetConnection())
            {
                var res = conn.Query<T>($"{typeof(T).Name}Get", new { Id = id }, commandType: CommandType.StoredProcedure).FirstOrDefault();
                if (res != null)
                {
                    res.DbContext = this;
                    res.AppConfig = _AppConfig;
                    if (_HttpContextAccessor?.HttpContext != null)
                    {
                        res.RequestUserId = _HttpContextAccessor.HttpContext.User.UserId();
                    }
                }
                return res;
            }
        }

        public override IEnumerable<T> GetAll<T>(object parameters = null)
        {
            using (var conn = GetConnection())
            {
                return conn.Query<T>($"{typeof(T).Name}Get", parameters, commandType: CommandType.StoredProcedure)
                    .Each(x => {
                        x.DbContext = this;
                        x.AppConfig = _AppConfig;
                        if (_HttpContextAccessor?.HttpContext != null)
                        {
                            x.RequestUserId = _HttpContextAccessor.HttpContext.User.UserId();
                        }
                    }).ToArray();
            }
        }

        public override DbConnection GetConnection()
        {
            return new SqlConnection(_AppConfig.Database.ConnectionString);
        }

        public override List<DbColumn> GetTableSchema(string tableName)
        {
            using (var conn = GetConnection())
            {
                var cmd = new SqlCommand($"SELECT TOP 1 * FROM {tableName}", (SqlConnection)conn);
                var reader = cmd.ExecuteReader(System.Data.CommandBehavior.SchemaOnly);
                if (reader.CanGetColumnSchema())
                {
                    return reader.GetColumnSchema().ToList();
                }
            }
            return null;
        }

        public override IEnumerable<T> Query<T>(string procName, object parameters = null)
        {
            using (var conn = GetConnection())
            {
                return conn.Query<T>(procName, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public override void Save<T>(T model, bool lazySave = true, bool forceSaveNulls = false)
        {
            var myType = model.GetType();
            using (var conn = GetConnection())
            {
                // build the parameters for saving
                var paramList = new DynamicParameters();

                var accessor = Cached(myType.Name, () => { return TypeAccessor.Create(myType); });

                // iterate through all the properties of the object adding to param list
                accessor.GetMembers()
                    .Where(x => (SavableTypes.Contains(x.Type) || SavableTypes.Contains(x.Type.GetTypeInfo().BaseType)) && !x.HasAttribute<Ignore>())
                    .ToList().ForEach(x => {
                        var val = accessor[model, x.Name];
                        if (x.Type.GetTypeInfo().BaseType == typeof(Enum))
                        {
                            val = Enum.GetName(x.Type, val) ?? val.ToString();
                        }
                        paramList.Add(x.Name, val, null, x.Name.ToLower() == "id" ? ParameterDirection.InputOutput : ParameterDirection.Input);
                    });

                conn.Execute($"{myType.Name}Save", paramList, commandType: CommandType.StoredProcedure);
                var id = paramList.Get<int>("Id");
                accessor[model, "Id"] = id;

                if (!lazySave)
                {
                    return;
                }
                // process the hasMany relationships
                myType.GetTypeInfo().GetCustomAttributes(typeof(HasMany)).ToList().ForEach(x => {
                    var childType = ((HasMany)x)?.ChildType;
                    if (childType == null)
                    {
                        return;
                    }

                    var children = (IList)accessor[model, childType.Name];
                    if (children == null && !forceSaveNulls)
                    {
                        return;
                    }
                    var existingIds = new List<int>();
                    try
                    {
                        // first lets get the full list from the db so we can figure out who to delete
                        var childParams = new DynamicParameters();
                        childParams.Add($"{myType.Name}Id", id);
                        var res = conn.Query($"{childType.Name}Get", childParams, commandType: CommandType.StoredProcedure);
                        res.Select(y => y.Id).ToList().ForEach(y => existingIds.Add(y));
                    }
                    catch { }

                    if (children != null && children.Count > 0)
                    {
                        var childAccessor = TypeAccessor.Create(childType);
                        var saveMethod = childType.GetMethod("Save");
                        foreach (BaseModel child in children)
                        {
                            // remove this id from the list that has to be cleaned up later
                            existingIds.Remove(childAccessor[child, "Id"].ToInt());
                            // make sure the parent Id is set on the child object
                            childAccessor[child, $"{myType.Name}Id"] = id;
                            // now save the child
                            Save(child, lazySave, forceSaveNulls);
                        }
                    }

                    // delete any child ids are that are leftover
                    existingIds.ForEach(y => Delete(y.ToInt(), childType));
                });
                conn.Close();
            }
        }

        protected T Cached<T>(string key, Func<T> onCreate) where T : class
        {
            if (_Cache == null)
            {
                return onCreate();
            }

            if (!_Cache.TryGetValue<T>(key, out var result))
            {
                result = onCreate();
                _Cache.Set(key, result);
            }
            return result;
        }
    }
}
