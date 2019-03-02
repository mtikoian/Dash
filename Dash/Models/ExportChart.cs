using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using Dash.Resources;

namespace Dash.Models
{
    public class ExportChart
    {
        public string ContentType { get; } = "image/png";

        [Required, Display(Name = "ExportChartData", ResourceType = typeof(Charts))]
        public string Data { get; set; }

        [Required, StringLength(250, MinimumLength = 5)]
        [Display(Name = "ExportChartName", ResourceType = typeof(Charts))]
        public string FileName { get; set; }

        public string FormattedFileName => FileName.ToCleanFileName("png");

        [Required, Display(Name = "ExportChartWidth", ResourceType = typeof(Charts))]
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
