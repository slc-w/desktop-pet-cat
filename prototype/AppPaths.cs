using System;
using System.IO;

namespace DesktopCat
{
    public static class AppPaths
    {
        public static readonly string Root = FindRoot();
        public static readonly string Data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopCat");

        static string FindRoot()
        {
            string besideExe = AppDomain.CurrentDomain.BaseDirectory;
            if (Directory.Exists(Path.Combine(besideExe, "art", "sprites", "white-ascii"))) return besideExe;
            return Path.GetFullPath(Path.Combine(besideExe, "..", ".."));
        }

        public static string UserFile(string name)
        {
            Directory.CreateDirectory(Data);
            string destination = Path.Combine(Data, name);
            string legacy = Path.Combine(Root, "prototype", name);
            if (!File.Exists(destination) && File.Exists(legacy)) File.Copy(legacy, destination);
            return destination;
        }
    }
}
