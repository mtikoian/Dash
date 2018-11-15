using System;
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

        private IAppConfiguration _AppConfig;
        private IMemoryCache _Cache;
        private IHttpContextAccessor _HttpContextAccessor;

        public DbContext(IAppConfiguration config, IMemoryCache cache = null, IHttpContextAccessor httpContextAccessor = null)
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

        public override void Save<T>(T model)
        {
            var myType = model.GetType();
            using (var conn = GetConnection())
            {
                // build the parameters for saving
                var paramList = new DynamicParameters();
                var accessor = Cached(myType.Name, () => { return TypeAccessor.Create(myType); });

                // iterate through all the properties of the object adding to param list
                accessor.GetMembers()
                    .Where(x => (SavableTypes.Contains(x.Type) || SavableTypes.Contains(x.Type.GetTypeInfo().BaseType)) && !x.HasAttribute<DbIgnore>())
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
                model.Id = paramList.Get<int>("Id");
            }
        }

        public override void SaveMany<T, T2>(T model, List<T2> children, bool forceSaveNulls = true)
        {
            using (var conn = GetConnection())
            {
                WithTransaction(() => {
                    var parentType = typeof(T);
                    var childType = typeof(T2);
                    if (children == null && !forceSaveNulls)
                    {
                        return;
                    }

                    var existingObjs = new Dictionary<int, T2>();
                    try
                    {
                        // first lets get the full list from the db so we can figure out who to delete
                        var childParams = new DynamicParameters();
                        childParams.Add($"{parentType.Name}Id", model.Id);
                        existingObjs = conn.Query<T2>($"{childType.Name}Get", childParams, commandType: CommandType.StoredProcedure).ToList().ToDictionary(x => x.Id, x => x);
                    }
                    catch { }

                    if (children.Count > 0)
                    {
                        var childAccessor = TypeAccessor.Create(childType);
                        var saveMethod = childType.GetMethod("Save");
                        foreach (var child in children)
                        {
                            if (existingObjs.TryGetValue(child.Id, out var savedChild))
                            {
                                // remove this id from the list that has to be cleaned up later
                                existingObjs.Remove(child.Id);
                            }
                            if (savedChild == null || !child.Equals(savedChild))
                            {
                                // make sure the parent Id is set on the child object
                                childAccessor[child, $"{parentType.Name}Id"] = model.Id;
                                // now save the child
                                Save(child);
                            }
                        }
                    }

                    // delete any child ids are that are leftover
                    existingObjs.Keys.ToList().ForEach(y => Delete(y, childType));
                });
            }
        }

        public override void WithTransaction(Action commands)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        commands();
                        tran.Commit();
                        conn.Close();
                    }
                    catch (Exception)
                    {
                        try
                        {
                            tran.Rollback();
                        }
                        catch { }
                        conn.Close();
                        throw;
                    }
                }
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
