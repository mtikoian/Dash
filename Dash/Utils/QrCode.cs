using System;
using System.Drawing;

// https://github.com/codebude/QRCoder/blob/master/QRCoder/QRCode.cs
namespace QRCoder
{
    public class DashQRCode : AbstractQRCode, IDisposable
    {
        public DashQRCode(QRCodeData data) : base(data) { }

        public Bitmap GetGraphic(int pixelsPerModule) => GetGraphic(pixelsPerModule, Color.Black, Color.White, true);

        public Bitmap GetGraphic(int pixelsPerModule, Color darkColor, Color lightColor, bool drawQuietZones = true)
        {
            var size = (QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8)) * pixelsPerModule;
            var offset = drawQuietZones ? 0 : 4 * pixelsPerModule;
            var bmp = new Bitmap(size, size);
            var gfx = Graphics.FromImage(bmp);
            for (var x = 0; x < size + offset; x = x + pixelsPerModule)
                for (var y = 0; y < size + offset; y = y + pixelsPerModule)
                    gfx.FillRectangle(new SolidBrush(QrCodeData.ModuleMatrix[(y + pixelsPerModule) / pixelsPerModule - 1][(x + pixelsPerModule) / pixelsPerModule - 1] ? darkColor : lightColor), new Rectangle(x - offset, y - offset, pixelsPerModule, pixelsPerModule));
            gfx.Save();
            return bmp;
        }
    }
}
