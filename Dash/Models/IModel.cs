namespace Dash.Models
{
    public abstract class IModel
    {
        public abstract void SetDbContext(IDbContext dbContext);

        public abstract void SetRequestUserId(int? userId);
    }
}