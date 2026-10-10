using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace DesktopCat
{
    public class Platform
    {
        public IntPtr Id;
        public double Left, Right, Y, WindowLeft;
        public bool Fits(double x, double width) { return x >= Left - .1 && x + width <= Right + .1; }
    }
    public class WindowSurface
    {
        public IntPtr Id;
        public Rect Bounds;
        public bool Candidate;
    }
    public static class Platforms
    {
        public static List<Platform> Build(List<WindowSurface> windows, Rect area, double headroom, double width)
        {
            var result = new List<Platform>();
            var covers = new List<Rect>();
            foreach (var window in windows) // front to back
            {
                Rect r = window.Bounds;
                if (window.Candidate && r.Top >= area.Top + headroom && r.Top < area.Bottom)
                {
                    var spans = new List<Tuple<double, double>>();
                    spans.Add(Tuple.Create(Math.Max(area.Left, r.Left), Math.Min(area.Right, r.Right)));
                    foreach (Rect cover in covers)
                    {
                        // Reserve space for the entire cat above the top edge, not just its feet.
                        if (cover.Bottom <= r.Top - headroom || cover.Top > r.Top + 1) continue;
                        var next = new List<Tuple<double, double>>();
                        foreach (var span in spans)
                        {
                            if (cover.Right <= span.Item1 || cover.Left >= span.Item2) next.Add(span);
                            else
                            {
                                if (cover.Left > span.Item1) next.Add(Tuple.Create(span.Item1, cover.Left));
                                if (cover.Right < span.Item2) next.Add(Tuple.Create(cover.Right, span.Item2));
                            }
                        }
                        spans = next;
                    }
                    foreach (var span in spans)
                        if (span.Item2 - span.Item1 >= width + 8)
                            result.Add(new Platform { Id = window.Id, Left = span.Item1 + 4, Right = span.Item2 - 4, Y = r.Top, WindowLeft = r.Left });
                }
                covers.Add(r);
            }
            result.Add(new Platform { Id = IntPtr.Zero, Left = area.Left, Right = area.Right, Y = area.Bottom, WindowLeft = area.Left });
            return result;
        }
        public static Platform Crossed(List<Platform> platforms, double oldFeet, double newFeet, double oldX, double newX, double width)
        {
            if (newFeet < oldFeet) return null;
            Platform best = null;
            foreach (Platform p in platforms)
            {
                if (p.Y < oldFeet - .01 || p.Y > newFeet + .01) continue;
                double t = newFeet == oldFeet ? 1 : Motion.Clamp((p.Y - oldFeet) / (newFeet - oldFeet), 0, 1);
                double x = oldX + (newX - oldX) * t;
                if (p.Fits(x, width) && (best == null || p.Y < best.Y)) best = p;
            }
            return best;
        }
    }
    public class JumpPlan
    {
        public double X, Y, VX, VY, Duration;
        public Platform Target;
        public static JumpPlan Create(double x, double feet, Platform target, double width, double height, Rect area)
        {
            double rise = feet - target.Y;
            if (rise <= 0 || rise >= 5 * height || target.Right - target.Left < width) return null;
            double inset = Math.Min(2, (target.Right - target.Left - width)/2);
            double toX = Motion.Clamp(x, target.Left + inset, target.Right - width - inset);
            if (Math.Abs(toX - x) > 4 * height) return null;
            double apex = target.Y - Math.Min(25, (5 * height - rise) * .5);
            if (apex - height < area.Top) return null;
            double vy = -Math.Sqrt(2 * 1000 * (feet - apex));
            double time = (-vy + Math.Sqrt(vy * vy - 2 * 1000 * rise)) / 1000;
            if (time <= 0) return null;
            return new JumpPlan { X = x, Y = feet, VX = (toX - x) / time, VY = vy, Duration = time, Target = target };
        }
    }
    public static class NativeWindows
    {
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
        delegate bool EnumProc(IntPtr hwnd, IntPtr param);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback, IntPtr param);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd, StringBuilder text, int count);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] static extern int GetBounds(IntPtr hwnd, int attr, out RECT rect, int size);
        [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] static extern int GetCloaked(IntPtr hwnd, int attr, out int value, int size);
        public static List<WindowSurface> Read(IntPtr own, Matrix fromDevice)
        {
            var windows = new List<WindowSurface>();
            EnumWindows(delegate(IntPtr hwnd, IntPtr unused) {
                if (hwnd == own || !IsWindowVisible(hwnd) || IsIconic(hwnd)) return true;
                int cloaked;
                if (GetCloaked(hwnd, 14, out cloaked, 4) == 0 && cloaked != 0) return true;
                var name = new StringBuilder(256); GetClassName(hwnd, name, name.Capacity);
                string cls = name.ToString();
                if (cls == "Progman" || cls == "WorkerW" || cls.StartsWith("Shell_")) return true;
                RECT r;
                if (GetBounds(hwnd, 9, out r, 16) != 0 && !GetWindowRect(hwnd, out r)) return true;
                if (r.Right <= r.Left || r.Bottom <= r.Top) return true;
                var bounds = new Rect(fromDevice.Transform(new Point(r.Left, r.Top)), fromDevice.Transform(new Point(r.Right, r.Bottom)));
                int ex = GetWindowLong(hwnd, -20);
                // Popups still occlude windows behind them, but are not platforms themselves.
                bool candidate = (ex & 0x80) == 0 && cls != "#32768" && !cls.ToLowerInvariant().Contains("tooltip") && bounds.Height > 80;
                windows.Add(new WindowSurface { Id = hwnd, Bounds = bounds, Candidate = candidate });
                return true;
            }, IntPtr.Zero);
            return windows;
        }
        public static bool ButtonHeld()
        {
            return GetAsyncKeyState(1) < 0 || GetAsyncKeyState(2) < 0 || GetAsyncKeyState(4) < 0;
        }
    }
}
