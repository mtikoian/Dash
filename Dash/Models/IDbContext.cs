using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Dash.Models
{
    public abstract class IDbContext
    {
        public abstract void Delete(int id, Type type, int? requestUserId);

        public abstract void Delete<T>(T model) where T : BaseModel;

        public abstract void Execute(string procName, object parameters);

        public abstract T Get<T>(int id, bool useCache = false) where T : BaseModel;

        public abstract IEnumerable<T> GetAll<T>(object parameters = null) where T : BaseModel;

        public abstract DbConnection GetConnection();

        public abstract List<DbColumn> GetTableSchema(string tableName);

        public abstract IEnumerable<T> Query<T>(string procName, object parameters = null);

        public abstract void Save<T>(T model) where T : BaseModel;

        public abstract void SaveMany<T, T2>(T model, List<T2> children) where T : BaseModel where T2 : BaseModel;

        public abstract void WithTransaction(Action commands);
    }
}
