using Dapper;
using Dash.Configuration;
using FastMember;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Dash.Models
{
    public class DbContext : IDbContext
    {
        /// <summary>
        /// Specifies the type of properties that should be including when building sql parameters to save an object.
        /// </summary>
        private static readonly Type[] SavableTypes = { typeof(string), typeof(bool), typeof(int), typeof(long), typeof(DateTime), typeof(DateTimeOffset),
            typeof(decimal), typeof(int?), typeof(long?), typeof(Byte[]), typeof(Enum), typeof(double) };

        private IMemoryCache _Cache;
        private DatabaseConfiguration _Database;

        public DbContext(AppConfiguration config, IMemoryCache cache = null)
        {
            _Database = config.Database;
            _Cache = cache;
        }

        /// <summary>
        /// Delete an object from the database.
        /// </summary>
        /// <param name="id">ID of the object to delete.</param>
        /// <param name="type">Type of object to delete.</typeparam>
        public override void Delete(int id, Type type, int? requestUserId = null)
        {
            using (var conn = GetOpenConnection())
            {
                conn.Execute($"{type.Name}Delete", new { RequestUserId = requestUserId, Id = id }, commandType: CommandType.StoredProcedure);
            }
        }

        /// <summary>
        /// Delete an object from the database.
        /// </summary>
        public override void Delete<T>(T model)
        {
            var myType = model.GetType();
            using (var conn = GetOpenConnection())
            {
                conn.Execute($"{myType.Name}Delete", new { RequestUserId = model.RequestUserId, Id = myType.GetProperty("Id").GetValue(model) }, commandType: CommandType.StoredProcedure);
            }
        }

        /// <summary>
        /// Run a stored procedure that doesn't return a result.
        /// </summary>
        /// <param name="procName">Name of the stored procedure to run.</param>
        /// <param name="parameters">Parameters to pass to stored procedure.</param>
        public override void Execute(string procName, object parameters)
        {
            using (var conn = GetOpenConnection())
            {
                conn.Execute(procName, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        /// <summary>
        /// Find a record in the database by Id and load into the specified object type.
        /// </summary>
        /// <typeparam name="T">Any object type derived from this class.</typeparam>
        /// <param name="id">Id of the record to load</param>
        /// <returns>Returns a new object of type T or null.</returns>
        public override T Get<T>(int id)
        {
            if (id == 0)
            {
                return null;
            }
            using (var conn = GetOpenConnection())
            {
                var res = conn.Query<T>($"{typeof(T).Name}Get", new { Id = id }, commandType: CommandType.StoredProcedure).FirstOrDefault();
                if (res != null)
                {
                    res.DbContext = this;
                }
                return res;
            }
        }

        /// <summary>
        /// Find all the records of the requested type and return as objects. Does not load children.
        /// </summary>
        /// <typeparam name="T">Any object type derived from this class.</typeparam>
        /// <param name="parameters">Sql parameters. Each property is the parameter name and the value is the param value.</param>
        /// <returns>Return enumerable of objects.</returns>
        public override IEnumerable<T> GetAll<T>(object parameters = null)
        {
            using (var conn = GetOpenConnection())
            {
                return conn.Query<T>($"{typeof(T).Name}Get", parameters, commandType: CommandType.StoredProcedure).ToArray();
            }
        }

        /// <summary>
        /// Get an open connection.
        /// </summary>
        /// <returns>Returns an open dbConnection object.</returns>
        public override DbConnection GetOpenConnection()
        {
            var conn = new SqlConnection(_Database.ConnectionString);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// Get the schema for a table from this database.
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <returns>List of fields, else false.</returns>
        public override List<DbColumn> GetTableSchema(string tableName)
        {
            using (var conn = GetOpenConnection())
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

        /// <summary>
        /// Run a stored procedure.
        /// </summary>
        /// <typeparam name="T">Any object type derived from this class.</typeparam>
        /// <param name="procName">Name of the stored procedure to run.</param>
        /// <param name="parameters">Parameters to pass to stored procedure.</param>
        /// <param name="connectionName">Use the named connection string instead of the default one.</param>
        /// <returns>Returns a new object of type T.</returns>
        public override IEnumerable<T> Query<T>(string procName, object parameters = null)
        {
            using (var conn = GetOpenConnection())
            {
                return conn.Query<T>(procName, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        /// <summary>
        /// Saves the object to the database. Includes any children.
        /// </summary>
        /// <param name="lazySave">Save children objects if true.</param>
        /// <returns>true</returns>
        public override void Save<T>(T model, bool lazySave = true, bool forceSaveNulls = false)
        {
            // @todo modify to return a bool instead of void

            var myType = model.GetType();
            using (var conn = GetOpenConnection())
            {
                // build the parameters for saving
                var paramList = new DynamicParameters();

                var accessor = Cached(myType.Name, () => { return TypeAccessor.Create(myType); });

                // iterate through all the properties of the object adding to param list
                accessor.GetMembers().Where(x => (SavableTypes.Contains(x.Type) || SavableTypes.Contains(x.Type.GetTypeInfo().BaseType)) && !x.HasAttribute<Ignore>()).ToList().ForEach(x => {
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

        public override void SetCache(IMemoryCache cache)
        {
            _Cache = cache;
        }

        /// <summary>
        /// Gets an object from the cache. Cache persists for the lifetime of the app pool.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="key">Unique key to identify the object.</param>
        /// <param name="onCreate">If object doesn't exist in cache, use return value from this function to create the object.</param>
        /// <returns>Returns the matched object.</returns>
        protected T Cached<T>(string key, Func<T> onCreate) where T : class
        {
            if (_Cache == null)
            {
                return onCreate();
            }

            T result;
            if (!_Cache.TryGetValue<T>(key, out result))
            {
                result = onCreate();
                _Cache.Set(key, result);
            }
            return result;
        }
    }
}