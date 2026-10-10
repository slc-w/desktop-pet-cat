using System;
using System.IO;
using Forms = System.Windows.Forms;

namespace DesktopCat
{
    public partial class CatWindow
    {
        public string RunSpriteSetProbe()
        {
            string testRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sprite-probe-" + Guid.NewGuid().ToString("N")));
            if (!testRoot.StartsWith(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), StringComparison.OrdinalIgnoreCase)) throw new Exception("Unsafe test path");
            string oldRoot = spriteRoot, oldPreference = spritePreference;
            Assets original = assets;
            string originalId = spriteSet;
            try
            {
                string custom = Path.Combine(testRoot, "my-robot");
                string template = Path.Combine(SpriteSets.Root, "black-ascii-sticker");
                foreach (string source in Directory.GetFiles(template, "*", SearchOption.AllDirectories))
                {
                    string destination = Path.Combine(custom, source.Substring(template.Length + 1));
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(source, destination);
                }
                string customManifest = Path.Combine(custom, "manifest.json");
                string validManifest = File.ReadAllText(customManifest);
                // Replace optional bundled name or insert a custom one.
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var manifest = serializer.Deserialize<Manifest>(validManifest);
                manifest.name = "My Robot";
                validManifest = serializer.Serialize(manifest);
                File.WriteAllText(customManifest, validManifest);
                Directory.CreateDirectory(Path.Combine(testRoot, "broken"));
                File.WriteAllText(Path.Combine(testRoot, "broken", "manifest.json"), "not json");
                Directory.CreateDirectory(Path.Combine(testRoot, "no-manifest"));
                var discovered = SpriteSets.Discover(testRoot);
                if (discovered.Count != 2 || !discovered.Exists(x => x.Id == "my-robot" && x.Name == "My Robot") || !discovered.Exists(x => x.Id == "broken" && x.Error != null))
                    throw new Exception("Custom set discovery/labels failed");
                spriteRoot = testRoot; spritePreference = Path.Combine(testRoot, "choice.txt");
                UpdateSpriteMenu();
                Forms.ToolStripMenuItem robot = null, broken = null;
                foreach (Forms.ToolStripItem item in spriteChoices.DropDownItems)
                {
                    if ((string)item.Tag == "my-robot") robot = item as Forms.ToolStripMenuItem;
                    if ((string)item.Tag == "broken") broken = item as Forms.ToolStripMenuItem;
                }
                if (robot == null || robot.Text != "My Robot" || broken == null || broken.Enabled) throw new Exception("Dropdown entries failed");
                SetClip("idle"); frameIndex = 1; frameTime = 123; DrawFrame();
                string beforeAction = action; double beforeLeft = Left, beforeTop = Top;
                robot.PerformClick();
                if (spriteSet != "my-robot" || !robot.Checked || frameIndex != 1 || frameTime != 123 || action != beforeAction || Left != beforeLeft || Top != beforeTop || File.ReadAllText(spritePreference).Trim() != "my-robot")
                    throw new Exception("Dropdown selection changed motion or failed to persist");
                spriteSet = "black-ascii-sticker"; assets = original; LoadSpritePreference();
                if (spriteSet != "my-robot") throw new Exception("Saved custom selection failed to restore");
                assets = original; spriteSet = "black-ascii-sticker";
                File.WriteAllText(customManifest, validManifest.Replace("900", "901"));
                if (SwitchSpriteSet("my-robot", true, false) || assets != original) throw new Exception("Incompatible timing was accepted");
                File.WriteAllText(customManifest, validManifest);
                string missingFrame = Path.Combine(custom, manifest.clips["idle"].frames[0].file);
                File.Move(missingFrame, missingFrame + ".saved");
                if (SwitchSpriteSet("my-robot", false, false) || assets != original) throw new Exception("Missing frame changed active assets");
                File.Move(missingFrame + ".saved", missingFrame);
                manifest.clips["idle"].frames[0].file = "../outside.png";
                File.WriteAllText(customManifest, serializer.Serialize(manifest));
                if (SwitchSpriteSet("my-robot", false, false)) throw new Exception("External frame path was accepted");
                bool rejected = false;
                try { SpriteSets.Load("../escape", testRoot); } catch (ArgumentException) { rejected = true; }
                if (!rejected || SpriteSets.Normalize("white") != "white-ascii" || SpriteSets.Normalize("sticker") != "black-ascii-sticker") throw new Exception("Folder isolation/legacy choices failed");
                File.WriteAllText(spritePreference, "removed-set"); LoadSpritePreference();
                if (spriteSet != "black-ascii-sticker" || assets != original) throw new Exception("Missing saved set did not keep default");
                return "PASS sprite sets: discovery, named dropdown, malformed manifest, selection, animation continuity, persistence/restore, incompatible timing, missing frames, external paths, legacy IDs, missing saved set fallback.";
            }
            finally
            {
                spriteRoot = oldRoot; spritePreference = oldPreference; assets = original; spriteSet = originalId;
                UpdateSpriteMenu(); DrawFrame();
                if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
            }
        }
    }
}
