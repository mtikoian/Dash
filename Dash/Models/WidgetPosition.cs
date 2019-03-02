namespace Dash.Models
{
    public class WidgetPosition
    {
        public int Height { get; set; } = 4;
        public string Id { get; set; }
        public int Width { get; set; } = 4;
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;

        public int SanitizedId() => Id.Replace("widget_", "").ToInt();
    }
}
