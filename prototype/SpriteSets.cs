using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Forms = System.Windows.Forms;

namespace DesktopCat
{
    public class SpriteSetInfo
    {
        public string Id, Name, Error;
    }

    public static class SpriteSets
    {
        public static readonly string PreferencePath = AppPaths.UserFile("sprite-set.txt");
        public static string Root { get { return Path.Combine(AppPaths.Root, "art", "sprites"); } }
        public static string Normalize(string id)
        {
            if (id == "white") return "white-ascii";
            if (id == "sticker") return "black-ascii-sticker";
            return id;
        }
        public static Assets Load(string id) { return Load(id, Root); }
        public static Assets Load(string id, string root)
        {
            if (String.IsNullOrWhiteSpace(id) || id == "." || id == ".." || Path.IsPathRooted(id) || id.IndexOfAny(new char[] {'/', '\\', ':'}) >= 0)
                throw new ArgumentException("Sprite set must be a folder name.");
            return new Assets(Path.Combine(root, id));
        }
        public static List<SpriteSetInfo> Discover(string root)
        {
            var result = new List<SpriteSetInfo>();
            if (!Directory.Exists(root)) return result;
            foreach (string directory in Directory.GetDirectories(root))
            {
                string manifestPath = Path.Combine(directory, "manifest.json");
                if (!File.Exists(manifestPath)) continue;
                var info = new SpriteSetInfo { Id = Path.GetFileName(directory), Name = Path.GetFileName(directory) };
                try
                {
                    var manifest = new JavaScriptSerializer().Deserialize<Manifest>(File.ReadAllText(manifestPath));
                    if (manifest == null || manifest.clips == null || manifest.canvas == null || manifest.canvas.Length != 2)
                        throw new InvalidDataException("Invalid manifest");
                    if (!String.IsNullOrWhiteSpace(manifest.name)) info.Name = manifest.name.Trim();
                }
                catch (Exception ex) { info.Error = ex.Message; }
                result.Add(info);
            }
            result.Sort(delegate(SpriteSetInfo a, SpriteSetInfo b) { return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name); });
            return result;
        }
        public static void Validate(Assets current, Assets next)
        {
            if (current.Clips.Count != next.Clips.Count) throw new InvalidDataException("Sprite clip counts differ.");
            foreach (var entry in current.Clips)
            {
                if (!next.Clips.ContainsKey(entry.Key) || next.Clips[entry.Key].Count != entry.Value.Count || next.Loops[entry.Key] != current.Loops[entry.Key])
                    throw new InvalidDataException("Incompatible sprite clip: " + entry.Key);
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    var a = entry.Value[i]; var b = next.Clips[entry.Key][i];
                    if (a.Width != b.Width || a.Height != b.Height || a.Duration != b.Duration)
                        throw new InvalidDataException("Incompatible sprite frame: " + entry.Key);
                }
            }
        }
    }

    public partial class CatWindow
    {
        string spriteSet = "black-ascii-sticker";
        string spriteRoot = SpriteSets.Root;
        string spritePreference = SpriteSets.PreferencePath;
        Forms.ToolStripMenuItem spriteChoices;

        void BuildSpriteMenu()
        {
            spriteChoices = new Forms.ToolStripMenuItem("Sprite set");
            spriteChoices.DropDownOpening += delegate { UpdateSpriteMenu(); };
            menu.Items.Add(spriteChoices);
            UpdateSpriteMenu();
        }
        void UpdateSpriteMenu()
        {
            while (spriteChoices.DropDownItems.Count > 0)
            {
                Forms.ToolStripItem old = spriteChoices.DropDownItems[0];
                spriteChoices.DropDownItems.RemoveAt(0);
                old.Dispose();
            }
            foreach (SpriteSetInfo info in SpriteSets.Discover(spriteRoot))
            {
                string id = info.Id;
                var choice = new Forms.ToolStripMenuItem(info.Name.Replace("&", "&&"));
                choice.Tag = id;
                choice.Checked = String.Equals(spriteSet, id, StringComparison.OrdinalIgnoreCase);
                choice.Enabled = info.Error == null;
                choice.ToolTipText = info.Error == null ? id : "Invalid manifest: " + info.Error;
                choice.Click += delegate { SwitchSpriteSet(id, true, true); };
                spriteChoices.DropDownItems.Add(choice);
            }
            spriteChoices.DropDownItems.Add(new Forms.ToolStripSeparator());
            spriteChoices.DropDownItems.Add("Open sprite folder", null, delegate { System.Diagnostics.Process.Start("explorer.exe", "\"" + spriteRoot + "\""); });
        }
        void LoadSpritePreference()
        {
            try
            {
                if (File.Exists(spritePreference))
                {
                    string id = File.ReadAllText(spritePreference).Trim();
                    if ((id == "white" || id == "sticker") && !Directory.Exists(Path.Combine(spriteRoot, id))) id = SpriteSets.Normalize(id);
                    SwitchSpriteSet(id, false, false);
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine("Could not read sprite preference: " + ex.Message); }
        }
        bool SwitchSpriteSet(string id, bool save, bool reportError)
        {
            if (String.Equals(id, spriteSet, StringComparison.OrdinalIgnoreCase)) return true;
            try
            {
                Assets next = SpriteSets.Load(id, spriteRoot);
                SpriteSets.Validate(assets, next);
                if (save)
                {
                    string temp = spritePreference + ".tmp";
                    File.WriteAllText(temp, id + Environment.NewLine);
                    if (File.Exists(spritePreference)) File.Replace(temp, spritePreference, null); else File.Move(temp, spritePreference);
                }
                assets = next;
                spriteSet = id;
                DrawFrame();
                // Update existing choices without rebuilding a dropdown during its click event.
                foreach (Forms.ToolStripItem item in spriteChoices.DropDownItems)
                {
                    var choice = item as Forms.ToolStripMenuItem;
                    if (choice != null && choice.Tag is string) choice.Checked = String.Equals((string)choice.Tag, id, StringComparison.OrdinalIgnoreCase);
                }
                return true;
            }
            catch (Exception ex)
            {
                if (reportError) Forms.MessageBox.Show("Could not switch sprite set: " + ex.Message, "Desktop Cat");
                else System.Diagnostics.Trace.WriteLine("Sprite preference unavailable; keeping default: " + ex.Message);
                return false;
            }
        }
    }
}
