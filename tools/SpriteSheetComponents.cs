using System;
using System.Collections.Generic;
using System.Drawing;

// Connected alpha regions for sheets whose artwork crosses nominal grid boundaries.
public static class SpriteSheetComponents
{
    public sealed class Part
    {
        public int Id, Area;
        public Rectangle Bounds;
    }
    public sealed class Result
    {
        public int[] Labels;
        public List<Part> Parts = new List<Part>();
    }
    public static Result Find(Bitmap image, int threshold, int minimumArea)
    {
        int w = image.Width, h = image.Height;
        var result = new Result { Labels = new int[w * h] };
        var alpha = new byte[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) alpha[y*w+x] = image.GetPixel(x,y).A;
        int id = 0;
        var queue = new Queue<int>();
        for (int start = 0; start < alpha.Length; start++)
        {
            if (alpha[start] < threshold || result.Labels[start] != 0) continue;
            id++;
            result.Labels[start] = id;
            queue.Enqueue(start);
            int left = w, top = h, right = 0, bottom = 0, area = 0;
            while (queue.Count > 0)
            {
                int pos = queue.Dequeue(), x = pos % w, y = pos / w;
                area++; left = Math.Min(left,x); top = Math.Min(top,y);
                right = Math.Max(right,x); bottom = Math.Max(bottom,y);
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    int nx=x+dx, ny=y+dy;
                    if (nx<0 || ny<0 || nx>=w || ny>=h) continue;
                    int next=ny*w+nx;
                    if (alpha[next]<threshold || result.Labels[next]!=0) continue;
                    result.Labels[next]=id; queue.Enqueue(next);
                }
            }
            if (area >= minimumArea) result.Parts.Add(new Part { Id=id, Area=area, Bounds=Rectangle.FromLTRB(left,top,right+1,bottom+1) });
        }
        return result;
    }
    public static Bitmap Extract(Bitmap source, Result result, Part part)
    {
        var image = new Bitmap(part.Bounds.Width, part.Bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        for (int y=0; y<image.Height; y++) for (int x=0; x<image.Width; x++)
        {
            int sx=x+part.Bounds.X, sy=y+part.Bounds.Y;
            if (result.Labels[sy*source.Width+sx]==part.Id) image.SetPixel(x,y,source.GetPixel(sx,sy));
        }
        return image;
    }
}
