using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace DesktopCat
{
    public class Frame { public string file { get; set; } public int duration_ms { get; set; } }
    public class Clip { public bool loop { get; set; } public Frame[] frames { get; set; } }
    public class Manifest { public string name { get; set; } public int[] canvas { get; set; } public Dictionary<string, Clip> clips { get; set; } }
    public class Sprite
    {
        public BitmapSource Image;
        public byte[] Pixels;
        public int Width, Height, Duration;
    }
    public class Assets
    {
        public Dictionary<string, List<Sprite>> Clips = new Dictionary<string, List<Sprite>>();
        public Dictionary<string, bool> Loops = new Dictionary<string, bool>();
        public Assets(string root)
        {
            root = Path.GetFullPath(root);
            Manifest manifest = new JavaScriptSerializer().Deserialize<Manifest>(File.ReadAllText(Path.Combine(root, "manifest.json")));
            if (manifest == null || manifest.canvas == null || manifest.canvas.Length != 2 || manifest.clips == null)
                throw new InvalidDataException("Invalid sprite manifest");
            foreach (var entry in manifest.clips)
            {
                if (entry.Value == null || entry.Value.frames == null || entry.Value.frames.Length == 0)
                    throw new InvalidDataException("Empty sprite clip: " + entry.Key);
                var frames = new List<Sprite>();
                foreach (Frame frame in entry.Value.frames)
                {
                    if (frame == null || String.IsNullOrWhiteSpace(frame.file)) throw new InvalidDataException("Missing frame filename");
                    string framePath = Path.GetFullPath(Path.Combine(root, frame.file));
                    if (!framePath.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Frame must be inside its sprite set: " + frame.file);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.UriSource = new Uri(framePath); bitmap.EndInit();
                    var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                    converted.Freeze();
                    if (converted.PixelWidth != manifest.canvas[0] || converted.PixelHeight != manifest.canvas[1] || frame.duration_ms <= 0)
                        throw new InvalidDataException("Invalid frame: " + frame.file);
                    byte[] pixels = new byte[converted.PixelWidth * converted.PixelHeight * 4];
                    converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
                    frames.Add(new Sprite { Image = converted, Pixels = pixels, Width = converted.PixelWidth, Height = converted.PixelHeight, Duration = frame.duration_ms });
                }
                Clips.Add(entry.Key, frames); Loops.Add(entry.Key, entry.Value.loop);
            }
        }
    }
    public static class Motion
    {
        public static double Clamp(double value, double min, double max) { return Math.Max(min, Math.Min(value, Math.Max(min, max))); }
        public static bool Fall(ref double feet, ref double velocity, double floor, double dt)
        {
            velocity += 1000 * dt; feet += velocity * dt;
            if (feet >= floor) { feet = floor; velocity = 0; return true; }
            return false;
        }
    }
    public partial class CatWindow : Window
    {
        double petSize = 1.25;
        double Scale { get { return .29*petSize; } }
        double CanvasW { get { return 340*Scale; } }
        double CanvasH { get { return 248*Scale; } }
        // Sticker backing extends below the original text-grid anchor.
        double FootOffset { get { return 243*Scale; } }
        Assets assets;
        readonly Image picture = new Image();
        readonly Random random = new Random(Guid.NewGuid().GetHashCode());
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly DispatcherTimer timer = new DispatcherTimer();
        Forms.NotifyIcon tray;
        Forms.ContextMenuStrip menu;
        Forms.ToolStripMenuItem pauseItem;
        string action = "idle", clip = "idle";
        int frameIndex;
        double frameTime, stateTime = 2, lastTime, velocity, direction = 1, refreshTime, walkPosition;
        bool paused, pressed, dragging, facingRight, closing;
        Point pressCursor, grab;
        Rect area;
        IntPtr handle;
        Sprite current;
        double playBob, playSway;
        readonly PetManager manager;
        readonly PetProfile profile;
        internal string PetName { get { return profile==null?"Desktop Cat":profile.Name; } }
        internal Forms.ContextMenuStrip PetMenu { get { return menu; } }
        internal bool OwnsWindow(IntPtr hwnd)
        {
            return hwnd==handle || (personalityEditor!=null && hwnd==new WindowInteropHelper(personalityEditor).Handle);
        }
        internal void LogPet(string text) { if(manager!=null)manager.Log(profile,text); }

        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT point);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        public CatWindow(Assets data, PetManager owner=null, PetProfile petProfile=null)
        {
            manager=owner;profile=petProfile;
            settingsPath=manager==null?AppPaths.UserFile("personality.json"):manager.ProfileFile(profile,"personality.json");
            spritePreference=manager==null?SpriteSets.PreferencePath:manager.ProfileFile(profile,"sprite-set.txt");
            assets = data;
            Title = PetName;
            Width = CanvasW; Height = CanvasH;
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true; Background = Brushes.Transparent;
            Topmost = true; ShowInTaskbar = false; ShowActivated = false;
            Content = picture; picture.Stretch = Stretch.Fill;
            RenderOptions.SetBitmapScalingMode(picture, BitmapScalingMode.HighQuality);
            area = SystemParameters.WorkArea;
            Left = area.Left + area.Width * 0.4; Top = area.Bottom - FootOffset;
            SetClip("idle");
            SourceInitialized += delegate {
                handle = new WindowInteropHelper(this).Handle;
                SetWindowLong(handle, -20, GetWindowLong(handle, -20) | 0x08000000 | 0x80);
                HwndSource.FromHwnd(handle).AddHook(WindowProc);
            };
            MouseLeftButtonDown += Press;
            MouseMove += MovePointer;
            MouseLeftButtonUp += Release;
            LostMouseCapture += delegate { if (pressed) DropPet(); };
            MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e) { ShowMenu(); e.Handled = true; };
            BuildTray();
            LoadPersonality();
            LoadSpritePreference();
            timer.Interval = TimeSpan.FromMilliseconds(16);
            timer.Tick += Tick;
            Loaded += delegate { lastTime = clock.Elapsed.TotalSeconds; timer.Start();LogPet(RuntimeSummary()); };
            Closed += delegate { closing = true; if(personalityEditor!=null)personalityEditor.Close(); timer.Stop(); if(tray!=null){tray.Visible=false;tray.Dispose();} LogPet("Closed");menu.Dispose(); };
        }

        IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == 0x21) { handled = true; return new IntPtr(3); } // MA_NOACTIVATE
            if (msg == 0x84 && !pressed)
            {
                Point position = PointFromScreen(new Point((short)(lparam.ToInt64() & 65535), (short)((lparam.ToInt64() >> 16) & 65535)));
                if (!Hit(position)) { handled = true; return new IntPtr(-1); }
            }
            return IntPtr.Zero;
        }
        bool Hit(Point point)
        {
            int x = (int)((point.X-playSway) / Scale), y = (int)((point.Y-playBob) / Scale);
            if (current == null || x < 0 || x >= current.Width || y < 0 || y >= current.Height) return false;
            if (facingRight && clip != "sleep" && clip != "sleep_twitch" && clip != "sleep_peek") x = current.Width - 1 - x;
            return current.Pixels[(y * current.Width + x) * 4 + 3] >= 16;
        }
        Point CursorPosition()
        {
            if(testCursor!=null)return testCursor();
            POINT p; GetCursorPos(out p);
            var source = PresentationSource.FromVisual(this);
            return source.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y));
        }
        void Press(object sender, MouseButtonEventArgs e)
        {
            if (!Hit(e.GetPosition(this))) return;
            pressCursor = CursorPosition(); grab = e.GetPosition(this);
            pressed = true; CaptureMouse(); e.Handled = true;
        }
        void MovePointer(object sender, MouseEventArgs e)
        {
            if (!pressed) return;
            Point cursor = CursorPosition();
            if (!dragging && (Math.Abs(cursor.X - pressCursor.X) >= SystemParameters.MinimumHorizontalDragDistance || Math.Abs(cursor.Y - pressCursor.Y) >= SystemParameters.MinimumVerticalDragDistance))
            {
                dragging = true; velocity = 0; horizontalVelocity = 0; support = null; jump = null; cursorJump=false;airPawTime=0; action = "carried"; SetClip("carried");
            }
            if (dragging)
            {
                Left = Motion.Clamp(cursor.X - grab.X, area.Left, area.Right - Width);
                Top = Motion.Clamp(cursor.Y - grab.Y, area.Top, area.Bottom - FootOffset);
            }
        }
        void Release(object sender, MouseButtonEventArgs e)
        {
            if (!pressed) return;
            bool wasDrag = dragging;
            DropPet();
            if (!wasDrag && support != null) Start("pet_happy", 0.6 + 1.2 * personality.Affection/100);
            e.Handled = true;
        }
        void DropPet()
        {
            pressed = false; dragging = false; ReleaseMouseCapture(); velocity = 0;
            horizontalVelocity = 0; jump = null;cursorJump=false;airPawTime=0;
            FindSupport();
            if (support == null) Start("fall", double.MaxValue);
            else { Top = support.Y - FootOffset; Start("land", 0.3); }
        }
        void SetClip(string name)
        {
            clip = name; frameIndex = 0; frameTime = 0; DrawFrame();
        }
        void DrawFrame()
        {
            current = assets.Clips[clip][frameIndex]; picture.Source = current.Image;
            UpdatePlayPose();
        }
        void UpdatePlayPose()
        {
            // A small visual bounce emphasizes running without altering platform physics.
            playBob=action=="chase" && clip=="run"?-Math.Abs(Math.Sin(CursorNow*14))*2.5*(Scale/.29):0;
            playSway=action=="chase" && chaseWiggling && chaseWindup>0?Math.Sin(CursorNow*28)*1.6*(Scale/.29):0;
            // Sleep symbols remain readable in the canonical orientation.
            bool mirror = facingRight && clip != "sleep" && clip != "sleep_twitch" && clip != "sleep_peek";
            picture.RenderTransformOrigin = new Point(0.5, 0.5);
            var pose=new TransformGroup();pose.Children.Add(new ScaleTransform(mirror?-1:1,1));pose.Children.Add(new TranslateTransform(playSway,playBob));
            picture.RenderTransform=pose;
        }
        void Start(string name, double seconds)
        {
            if (action == "sleep" && name != "sleep") disturbance = 0;
            action = name; stateTime = seconds; SetClip(name);
            if(name=="walk")walkPosition=Left;
        }
        void SelectAction()
        {
            if (TryCursorAction()) return;
            // Give sustained nearby play time to build interest before choosing a nap.
            if(personality.CursorAttention && engagement>.2 && nearbyTime>.5 && Personality.Distance(lastCursor,Body)<4*BodyHeight)
            { facingRight=lastCursor.X>Body.Left+Body.Width/2;Start("look",.6);return; }
            if (random.NextDouble() < .1 + .25 * personality.Curiosity/100 && TryJump()) return;
            double choice = random.NextDouble();
            if (!CursorKeepsAwake && choice < .10 + .35 * personality.Sleepiness/100) { facingRight = false; Start("sleep", 15 + personality.Sleepiness*.4 + random.Next(25)); }
            else if (choice < .72)
            {
                direction = random.Next(2) == 0 ? -1 : 1; facingRight = direction > 0;
                Start("walk", 3 + random.Next(6));
            }
            else if (choice < .80) Start("groom", 3);
            else if (choice < .90) Start("sit", 4);
            else Start("idle", 2 + random.Next(4));
        }
        void Tick(object sender, EventArgs e)
        {
            double now = clock.Elapsed.TotalSeconds, dt = Math.Min(.05, now - lastTime); lastTime = now;
            refreshTime += dt;
            if (refreshTime > .25) { refreshTime = 0; RefreshPlatforms(); }
            UpdateAttention(dt);
            if (paused || (pressed && !dragging)) return;
            AdvanceAnimation(dt);
            UpdatePlayPose();
            if (dragging) return;
            if (action == "fall" || action == "jump") { AdvanceAir(dt); return; }
            if (action == "approach_cursor" || action == "chase") { AdvanceCursorMove(dt); return; }
            if (action == "walk")
            {
                double min = support == null ? area.Left : support.Left, max = (support == null ? area.Right : support.Right) - Width;
                walkPosition += direction * WalkSpeed * personality.Speed * dt;
                Left=Motion.Clamp(walkPosition,min,max);
                if (walkPosition <= min || walkPosition >= max)
                {
                    walkPosition=Motion.Clamp(walkPosition,min,max);
                    direction *= -1; facingRight = direction > 0; DrawFrame();
                }
            }
            stateTime -= dt;
            if (stateTime <= 0)
            {
                if (action == "jump_crouch") LaunchJump();
                else if (action == "sleep") Start("wake", .7);
                else if (action == "wake") Start("stretch", .75);
                else SelectAction();
            }
        }
        void AdvanceAnimation(double dt)
        {
            frameTime += dt * 1000 * ((clip == "walk" || clip == "run") ? personality.Speed * (action == "approach_cursor" ? .6 : 1) : 1);
            var frames = assets.Clips[clip];
            while (frameTime >= frames[frameIndex].Duration)
            {
                frameTime -= frames[frameIndex].Duration;
                if (frameIndex < frames.Count - 1) frameIndex++;
                else if (assets.Loops[clip] || (clip=="paw_swipe" && (action=="paw_swipe" || (cursorJump && airPawTime>0)))) frameIndex = 0;
                else { frameTime = 0; break; }
                DrawFrame();
            }
        }
        void BuildTray()
        {
            menu = new Forms.ContextMenuStrip();
            menu.Items.Add(PetName).Enabled = false;
            pauseItem = new Forms.ToolStripMenuItem("Pause");
            pauseItem.Click += delegate { paused = !paused; pauseItem.Text = paused ? "Resume" : "Pause"; };
            menu.Items.Add(pauseItem);
            menu.Items.Add("Sleep", null, delegate { Command("sleep", 90); });
            menu.Items.Add("Wake / stretch", null, delegate { Command("wake", .7); });
            menu.Items.Add("Return to taskbar", null, delegate { ReturnToFloor(); });
            menu.Items.Add("Jump to a reachable window", null, delegate { if (!paused && !dragging && support != null && action != "jump") TryJump(); });
            menu.Items.Add("Personality & size...", null, delegate { EditPersonality(); });
            BuildSpriteMenu();
            menu.Items.Add(new Forms.ToolStripSeparator());
            if(manager!=null)menu.Items.Add("Add another pet", null, delegate { manager.AddPet(); });
            menu.Items.Add(manager==null?"Exit":"Remove this pet", null, delegate { Close(); });
            if(manager==null)
            {
                tray = new Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Application, Text = "Desktop Cat — right-click for controls", ContextMenuStrip = menu, Visible = true };
                tray.DoubleClick += delegate { ReturnToFloor(); };
            }
        }
        void Command(string name, double duration)
        {
            if(name=="sleep")attentionAwakeTime=0;
            if (support == null) ReturnToFloor();
            paused = false; pauseItem.Text = "Pause"; Start(name, duration);
        }
        void ReturnToFloor()
        {
            if (closing) return;
            cursorJump=false;airPawTime=0;chaseWindup=0;
            pressed = false; dragging = false; ReleaseMouseCapture(); velocity = 0;
            area = SystemParameters.WorkArea; Left = area.Left + area.Width * .4;
            Top = area.Bottom - FootOffset; Start("idle", 2);
            horizontalVelocity = 0; jump = null; support = new Platform { Id=IntPtr.Zero, Left=area.Left, Right=area.Right, Y=area.Bottom, WindowLeft=area.Left };
        }
        void ShowMenu() { menu.Show(Forms.Cursor.Position); }
    }
    public static class Program
    {
        [STAThread] public static void Main(string[] args)
        {
            string log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopCat", "last-run.log");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(log));
                var assets = SpriteSets.Load("black-ascii-sticker");
                if (args.Length > 0 && args[0] == "--self-test")
                {
                    int count = 0; foreach (var frames in assets.Clips.Values) count += frames.Count;
                    if (count != 54 || assets.Clips.Count != 30) throw new Exception("Unexpected asset counts");
                    SpriteSets.Validate(assets, SpriteSets.Load("white-ascii"));
                    double feet = 0, velocity = 0;
                    for (int i = 0; i < 500; i++) Motion.Fall(ref feet, ref velocity, 500, .016);
                    if (feet != 500 || velocity != 0 || Motion.Clamp(-20, 0, 100) != 0 || Motion.Clamp(200, 0, 100) != 100)
                        throw new Exception("Floor or boundary invariant failed");
                    string spriteProbe;
                    var probeWindow = new CatWindow(assets);
                    try { spriteProbe = probeWindow.RunSpriteSetProbe()+"\r\n"+probeWindow.RunSizeProbe(); } finally { probeWindow.Close(); }
                    File.WriteAllText(log, "PASS: both sprite sets loaded and compatible; 30 clips / 54 PNGs loaded; dimensions and durations validated; gravity lands exactly on floor; horizontal bounds clamp.\r\n" + BehaviorTests.Run()+"\r\n" + spriteProbe + "\r\n");
                    return;
                }
                bool created;
                using (var mutex = new System.Threading.Mutex(true, "DesktopCatPrototype-01a0f24a", out created))
                {
                    if (!created) { if(args.Length>0)throw new InvalidOperationException("Close the running application before running a probe.");return; }
                    var app = new Application();
                    app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e) { File.AppendAllText(log, e.Exception.ToString()); };
                    File.WriteAllText(log, "Starting desktop prototype " + DateTime.Now.ToString("s") + "\r\n");
                    if(args.Length==0 || args[0]=="--multi-pet-test")
                    {
                        app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
                        string managerData=args.Length==0?AppPaths.Data:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"multi-pet-probe-"+Guid.NewGuid().ToString("N"));
                        using(var pets=new PetManager(app,assets,managerData,args.Length==0))
                        {
                            pets.Start();
                            if(args.Length>0)pets.RunProbe(log);
                            app.Run();
                        }
                        return;
                    }
                    var window = new CatWindow(assets);
                    if(args.Length>0 && args[0]=="--smoke-test")
                    {
                        var smokeTimer=new DispatcherTimer { Interval=TimeSpan.FromSeconds(4) };
                        smokeTimer.Tick+=delegate { smokeTimer.Stop();File.AppendAllText(log,window.RuntimeSummary()+"\r\n");window.Close(); };
                        smokeTimer.Start();
                    }
                    if(args.Length>0 && args[0]=="--integration-test")
                        window.Loaded+=delegate {window.RunIntegrationProbe(log);};
                    if(args.Length>0 && args[0]=="--cursor-test")
                        window.Loaded+=delegate {
                            try {File.AppendAllText(log,window.RunCursorProbe()+"\r\n");}
                            catch(Exception ex){File.AppendAllText(log,"FAIL cursor probe: "+ex+"\r\n");Environment.ExitCode=1;}
                            window.Close();
                        };
                    if(args.Length>0 && args[0]=="--settings-test")
                        window.Loaded+=delegate {window.RunSettingsProbe(log);};
                    app.Run(window);
                }
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(log, ex.ToString()); } catch { }
                if (args.Length == 0) MessageBox.Show(ex.Message, "Desktop Cat could not start");
                Environment.ExitCode = 1;
            }
        }
    }
}
