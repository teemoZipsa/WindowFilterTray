using System.Drawing;
using System.Drawing.Imaging;
using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class ThumbnailService
{
    private readonly AppPaths _paths;

    public ThumbnailService(AppPaths paths)
    {
        _paths = paths;
    }

    public string? Capture(WindowSnapshot snapshot, string ruleId)
    {
        if (snapshot.Rect.Width <= 0 || snapshot.Rect.Height <= 0)
        {
            return null;
        }

        try
        {
            var width = Math.Min(snapshot.Rect.Width, 640);
            var height = Math.Max(1, (int)(snapshot.Rect.Height * (width / (double)snapshot.Rect.Width)));
            using var full = new Bitmap(snapshot.Rect.Width, snapshot.Rect.Height);
            using (var graphics = Graphics.FromImage(full))
            {
                graphics.CopyFromScreen(snapshot.Rect.Left, snapshot.Rect.Top, 0, 0, full.Size);
            }

            using var thumb = new Bitmap(full, width, height);
            var fileName = $"{ruleId}.png";
            var path = Path.Combine(_paths.Thumbnails, fileName);
            thumb.Save(path, ImageFormat.Png);
            return Path.Combine("thumbs", fileName);
        }
        catch
        {
            return null;
        }
    }
}
