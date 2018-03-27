using Dash.Configuration;

namespace Dash.Models
{
    public abstract class IModel
    {
        public abstract void SetAppConfig(IAppConfiguration appConfig);

        public abstract void SetDbContext(IDbContext dbContext);

        public abstract void SetForSave(bool forSave);

        public abstract void SetRequestUserId(int? userId);
    }
}
