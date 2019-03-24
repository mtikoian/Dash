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
using StackExchange.Profiling;

namespace Dash.Models
{
    public class DbContext : IDbContext
    {
        /// <summary>
        /// Specifies the type of properties that should be included when building sql parameters to save an object.
        /// </summary>
        static readonly Type[] _SavableTypes = { typeof(string), typeof(bool), typeof(int), typeof(long), typeof(DateTime), typeof(DateTimeOffset),
            typeof(decimal), typeof(int?), typeof(long?), typeof(byte[]), typeof(Enum), typeof(double), typeof(DateTimeOffset?) };

        IAppConfiguration _AppConfig;
        IMemoryCache _Cache;
        IHttpContextAccessor _HttpContextAccessor;

        T Bind<T>(T model) where T : BaseModel
        {
            if (model != null)
            {
                model.DbContext = this;
                model.AppConfig = _AppConfig;
                if (_HttpContextAccessor?.HttpContext != null)
                    model.RequestUserId = _HttpContextAccessor.HttpContext.User.UserId();
            }
            return model;
        }

        public DbContext(IAppConfiguration config, IMemoryCache cache = null, IHttpContextAccessor httpContextAccessor = null)
        {
            _AppConfig = config;
            _Cache = cache;
            _HttpContextAccessor = httpContextAccessor;
        }

        public override void Delete(int id, Type type, int? requestUserId = null)
        {
            using (var conn = GetConnection())
                conn.ExecuteAsync($"{type.Name}Delete", new {
                    RequestUserId = requestUserId ?? _HttpContextAccessor.HttpContext?.User.UserId(),
                    Id = id
                }, commandType: CommandType.StoredProcedure);
        }

        public override void Delete<T>(T model)
        {
            var myType = model.GetType();
            using (var conn = GetConnection())
                conn.ExecuteAsync($"{myType.Name}Delete", new {
                    RequestUserId = model.RequestUserId ?? _HttpContextAccessor.HttpContext?.User.UserId(),
                    Id = myType.GetProperty("Id").GetValue(model)
                }, commandType: CommandType.StoredProcedure);
        }

        public override void Execute(string procName, object parameters)
        {
            using (var conn = GetConnection())
                conn.ExecuteAsync(procName, parameters, commandType: CommandType.StoredProcedure);
        }

        public override T Get<T>(int id, bool useCache = false)
        {
            if (id == 0)
                return null;

            if (useCache)
            {
                var res = _Cache.Cached<T>($"dbResult_{typeof(T).Name}_{id}", () => {
                    using (var conn = GetConnection())
                        return conn.QueryAsync<T>($"{typeof(T).Name}Get", new { Id = id }, commandType: CommandType.StoredProcedure).Result.FirstOrDefault();
                });
                return Bind(res);
            }

            using (var conn = GetConnection())
                return Bind(conn.QueryAsync<T>($"{typeof(T).Name}Get", new { Id = id }, commandType: CommandType.StoredProcedure).Result.FirstOrDefault());
        }

        public override IEnumerable<T> GetAll<T>(object parameters = null)
        {
            using (var conn = GetConnection())
                return conn.QueryAsync<T>($"{typeof(T).Name}Get", parameters, commandType: CommandType.StoredProcedure)
                    .Result.Each(x => Bind(x)).ToArray();
        }

        public override DbConnection GetConnection() => new StackExchange.Profiling.Data.ProfiledDbConnection(new SqlConnection(_AppConfig.Database.ConnectionString), MiniProfiler.Current);

        public override List<DbColumn> GetTableSchema(string tableName)
        {
            using (var conn = GetConnection())
            {
                var cmd = new SqlCommand($"SELECT TOP 1 * FROM {tableName}", (SqlConnection)conn);
                var reader = cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly).Result;
                if (reader.CanGetColumnSchema())
                    return reader.GetColumnSchema().ToList();
            }
            return null;
        }

        public override IEnumerable<T> Query<T>(string procName, object parameters = null)
        {
            using (var conn = GetConnection())
                return conn.QueryAsync<T>(procName, parameters, commandType: CommandType.StoredProcedure).Result;
        }

        public override void Save<T>(T model)
        {
            var myType = model.GetType();
            using (var conn = GetConnection())
            {
                // build the parameters for saving
                var paramList = new DynamicParameters();
                var accessor = _Cache.Cached($"typeAccessor_{myType.Name}", () => { return TypeAccessor.Create(myType); });

                // iterate through all the properties of the object adding to param list
                accessor.GetMembers()
                    .Where(x => (_SavableTypes.Contains(x.Type) || _SavableTypes.Contains(x.Type.GetTypeInfo().BaseType)) && !x.HasAttribute<DbIgnore>())
                    .ToList().ForEach(x => {
                        var val = accessor[model, x.Name];
                        if (x.Type.GetTypeInfo().BaseType == typeof(Enum))
                            val = Enum.GetName(x.Type, val) ?? val.ToString();
                        paramList.Add(x.Name, val, null, x.Name.ToLower() == "id" ? ParameterDirection.InputOutput : ParameterDirection.Input);
                    });

                var res = conn.ExecuteAsync($"{myType.Name}Save", paramList, commandType: CommandType.StoredProcedure).Result;
                var id = paramList.Get<int>("Id");
                model.Id = paramList.Get<int>("Id");
            }
        }

        public override void SaveMany<T, T2>(T model, List<T2> children)
        {
            using (var conn = GetConnection())
            {
                WithTransaction(() => {
                    var parentType = typeof(T);
                    var childType = typeof(T2);
                    var existingObjs = new Dictionary<int, T2>();

                    // first lets get the full list from the db so we can figure out who to delete
                    var childParams = new DynamicParameters();
                    childParams.Add($"{parentType.Name}Id", model.Id);
                    existingObjs = conn.QueryAsync<T2>($"{childType.Name}Get", childParams, commandType: CommandType.StoredProcedure).Result.ToList().ToDictionary(x => x.Id, x => x);

                    if (children?.Count > 0)
                    {
                        var childAccessor = TypeAccessor.Create(childType);
                        children.Each(child => {
                            if (existingObjs.TryGetValue(child.Id, out var savedChild))
                                // remove this id from the list that has to be cleaned up later
                                existingObjs.Remove(child.Id);

                            if (savedChild == null || !child.Equals(savedChild))
                            {
                                // make sure the parent Id is set on the child object
                                childAccessor[child, $"{parentType.Name}Id"] = model.Id;
                                // now save the child
                                Save(child);
                            }
                        });
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
    }
}
