using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DesktopCat
{
    public class PetProfile
    {
        public string Id, Name;
    }
    public class PetRoster
    {
        public int SchemaVersion=1;
        public List<PetProfile> Pets=new List<PetProfile>();
    }
    public partial class PetManager : IDisposable
    {
        readonly Application app;
        readonly Assets defaultAssets;
        readonly string dataRoot;
        readonly bool migrate;
        readonly List<CatWindow> pets=new List<CatWindow>();
        readonly Dictionary<CatWindow,PetProfile> profiles=new Dictionary<CatWindow,PetProfile>();
        readonly Dictionary<CatWindow,Forms.ToolStripMenuItem> entries=new Dictionary<CatWindow,Forms.ToolStripMenuItem>();
        readonly Forms.ContextMenuStrip trayMenu=new Forms.ContextMenuStrip();
        readonly Forms.NotifyIcon tray;
        PetRoster roster;
        bool stopping,disposed,badRoster;
        string RosterPath { get { return Path.Combine(dataRoot,"pets.json"); } }
        internal bool MenuVisible { get { return trayMenu.Visible || pets.Exists(p=>p.PetMenu.Visible); } }

        public PetManager(Application application,Assets assets,string directory,bool migrateLegacy)
        {
            app=application;defaultAssets=assets;dataRoot=Path.GetFullPath(directory);migrate=migrateLegacy;
            Directory.CreateDirectory(dataRoot);
            roster=LoadRoster();
            trayMenu.Items.Add("Desktop Cat").Enabled=false;
            trayMenu.Items.Add("Add another pet",null,delegate {AddPet();});
            trayMenu.Items.Add("Return all to taskbar",null,delegate {foreach(var pet in pets)pet.PlaceOnFloor(pet.Left);});
            trayMenu.Items.Add(new Forms.ToolStripSeparator());
            trayMenu.Items.Add("Exit all pets",null,delegate {Exit();});
            tray=new Forms.NotifyIcon {Icon=System.Drawing.SystemIcons.Application,Text="Desktop Cat — manage your pets",ContextMenuStrip=trayMenu,Visible=true};
            tray.DoubleClick+=delegate {foreach(var pet in pets)pet.PlaceOnFloor(pet.Left);};
        }
        PetRoster LoadRoster()
        {
            try
            {
                if(File.Exists(RosterPath))
                {
                    var saved=new JavaScriptSerializer().Deserialize<PetRoster>(File.ReadAllText(RosterPath));
                    if(saved==null || saved.SchemaVersion!=1 || saved.Pets==null)throw new InvalidDataException("Invalid pet roster");
                    var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach(var profile in saved.Pets)
                        if(profile==null || !ValidId(profile.Id) || String.IsNullOrWhiteSpace(profile.Name) || !ids.Add(profile.Id))throw new InvalidDataException("Invalid pet profile");
                    if(saved.Pets.Count>0)return saved;
                }
            }
            catch(Exception ex){badRoster=true;System.Diagnostics.Trace.WriteLine("Could not read pet roster: "+ex.Message);}
            var result=new PetRoster();result.Pets.Add(new PetProfile {Id="cat-1",Name="Cat 1"});return result;
        }
        static bool ValidId(string id){return id!=null && Regex.IsMatch(id,"^[A-Za-z0-9_-]{1,64}$");}
        public string ProfileFile(PetProfile profile,string file)
        {
            if(profile==null || !ValidId(profile.Id) || Path.GetFileName(file)!=file)throw new ArgumentException("Invalid profile path");
            string directory=Path.Combine(dataRoot,"pets",profile.Id);Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,file);
            if(migrate && profile.Id=="cat-1" && (file=="personality.json" || file=="sprite-set.txt") && !File.Exists(path))
            {
                string legacy=AppPaths.UserFile(file);
                if(File.Exists(legacy))File.Copy(legacy,path);
            }
            return path;
        }
        void SaveRoster()
        {
            if(badRoster && File.Exists(RosterPath))
                File.Copy(RosterPath,RosterPath+".invalid-"+Guid.NewGuid().ToString("N"));
            string temp=RosterPath+".tmp";
            File.WriteAllText(temp,new JavaScriptSerializer().Serialize(roster));
            if(File.Exists(RosterPath))File.Replace(temp,RosterPath,null);else File.Move(temp,RosterPath);
            badRoster=false;
        }
        void PersistRoster()
        {
            try {SaveRoster();}
            catch(Exception ex){Forms.MessageBox.Show("Pets are running, but the pet list could not be saved: "+ex.Message,"Desktop Cat");}
        }
        public void Start()
        {
            foreach(var profile in new List<PetProfile>(roster.Pets))CreatePet(profile);
            PersistRoster();
        }
        public void AddPet()
        {
            int number=1;
            while(roster.Pets.Exists(p=>p.Name=="Cat "+number))number++;
            var profile=new PetProfile {Id=Guid.NewGuid().ToString("N"),Name="Cat "+number};
            try
            {
                CreatePet(profile);roster.Pets.Add(profile);PersistRoster();
            }
            catch(Exception ex){Forms.MessageBox.Show("Could not add a pet: "+ex.Message,"Desktop Cat");}
        }
        void CreatePet(PetProfile profile)
        {
            var pet=new CatWindow(defaultAssets,this,profile);
            double x=SpawnX(pet);
            pet.PlaceOnFloor(x);
            pets.Add(pet);profiles.Add(pet,profile);
            var entry=new Forms.ToolStripMenuItem(profile.Name.Replace("&","&&")+" controls...");
            // A context menu has one owner. Open it after the tray menu closes,
            // rather than also attaching it as a submenu (which breaks disposal).
            entry.Click+=delegate {app.Dispatcher.BeginInvoke(new Action(delegate {
                if(pets.Contains(pet))pet.PetMenu.Show(Forms.Cursor.Position);
            }));};
            entries.Add(pet,entry);trayMenu.Items.Insert(trayMenu.Items.Count-2,entry);
            bool shown=false;
            pet.Closed+=delegate {
                pets.Remove(pet);profiles.Remove(pet);
                trayMenu.Items.Remove(entry);entries.Remove(pet);entry.Dispose();
                if(shown && !stopping)
                {
                    roster.Pets.Remove(profile);PersistRoster();
                    if(pets.Count==0)Exit();
                }
            };
            try {pet.Show();shown=true;}
            catch {pet.Close();throw;}
        }
        double SpawnX(CatWindow pet)
        {
            Rect area=SystemParameters.WorkArea;
            double preferred=Motion.Clamp(pet.Left,area.Left,area.Right-pet.Width);
            // Search the floor for a free footprint rather than stacking new pets.
            for(int i=0;i<200;i++)
            {
                double x=i==0?preferred:area.Left+(i-1)*Math.Max(20,pet.Width+24);
                if(x+pet.Width>area.Right)break;
                Rect bounds=new Rect(x,area.Bottom-pet.Height,pet.Width,pet.Height);
                bounds.Inflate(12,12);
                if(!pets.Exists(other=>bounds.IntersectsWith(new Rect(other.Left,other.Top,other.Width,other.Height))))return x;
            }
            return preferred;
        }
        public bool IsPetWindow(IntPtr hwnd){return pets.Exists(p=>p.OwnsWindow(hwnd));}
        public void Log(PetProfile profile,string text)
        {
            try {File.AppendAllText(ProfileFile(profile,"last-run.log"),DateTime.Now.ToString("s")+" "+text+Environment.NewLine);}
            catch(Exception ex){System.Diagnostics.Trace.WriteLine("Could not write pet log: "+ex.Message);}
        }
        public void Exit()
        {
            if(stopping)return;stopping=true;
            // Exiting the application preserves the roster; Remove this pet edits it.
            foreach(var pet in new List<CatWindow>(pets))pet.Close();
            // Dispose native tray/menu windows while the WPF dispatcher is alive.
            Dispose();
            app.Shutdown();
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;stopping=true;
            foreach(var pet in new List<CatWindow>(pets))pet.Close();
            tray.Visible=false;tray.Dispose();trayMenu.Dispose();
        }
    }
    public partial class CatWindow
    {
        internal void PlaceOnFloor(double x)
        {
            ReturnToFloor();Left=Motion.Clamp(x,area.Left,area.Right-Width);walkPosition=Left;cursorPosition=Left;
        }
    }
}
