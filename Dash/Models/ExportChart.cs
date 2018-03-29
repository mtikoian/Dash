using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Web;

namespace Dash.Models
{
    public class ExportChart
    {
        public string ContentType { get; } = "image/png";

        [Required, Display(Name = "ExportChartData", ResourceType = typeof(I18n.Charts))]
        public string Data { get; set; }

        [Required, StringLength(250, MinimumLength = 5)]
        [Display(Name = "ExportChartName", ResourceType = typeof(I18n.Charts))]
        public string FileName { get; set; }

        public string FormattedFileName
        {
            get
            {
                var formattedName = FileName;
                Array.ForEach(Path.GetInvalidFileNameChars(), c => formattedName = formattedName.Replace(c.ToString(), String.Empty));
                return $"{formattedName}.png";
            }
        }

        [Required, Display(Name = "ExportChartWidth", ResourceType = typeof(I18n.Charts))]
        public int Width { get; set; }

        /// <summary>
        /// Create the chart image and stream to the response.
        /// </summary>
        public byte[] Stream()
        {
            var bytes = Convert.FromBase64String(Data.Replace("data:image/png;base64,", ""));
            Image image;
            using (var ms = new MemoryStream(bytes))
            {
                image = Image.FromStream(ms);
            }

            // Convert PNG to non-transparent background. Might make this an option later.
            using (var nonTransparent = new Bitmap(image.Width, image.Height))
            {
                nonTransparent.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                using (var g = Graphics.FromImage(nonTransparent))
                {
                    g.Clear(Color.White);
                    g.DrawImageUnscaled(image, 0, 0);
                }

                var stream = new MemoryStream();
                nonTransparent.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}
