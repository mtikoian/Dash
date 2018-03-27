namespace Dash.Models
{
    public class ModelWrapper<T> : BaseModel
    {
        public T Model { get; set; }

        public T GetModel()
        {
            return Model;
        }
    }
}
