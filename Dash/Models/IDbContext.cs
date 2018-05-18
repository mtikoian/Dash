using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Dash.Models
{
    public abstract class IDbContext
    {
        public abstract void Delete(int id, Type type, int? requestUserId);

        public abstract void Delete<T>(T model) where T : BaseModel;

        public abstract void Execute(string procName, object parameters);

        public abstract T Get<T>(int id) where T : BaseModel;

        public abstract IEnumerable<T> GetAll<T>(object parameters = null) where T : BaseModel;

        public abstract DbConnection GetConnection();

        public abstract List<DbColumn> GetTableSchema(string tableName);

        public abstract IEnumerable<T> Query<T>(string procName, object parameters = null);

        public abstract void Save<T>(T model, bool lazySave = true, bool forceSaveNulls = false) where T : BaseModel;
    }
}
