using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DesktopCat
{
    public partial class PetManager
    {
        public void RunProbe(string log)
        {
            var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(600)};
            int stage=0;CatWindow first=null,second=null;double firstX=0,secondX=0;
            string secondSettings=null;PetProfile survivor=null;
            timer.Tick+=delegate {
                try
                {
                    if(stage==0)
                    {
                        Require(pets.Count==1,"Initial pet count");first=pets[0];
                        first.ProbePause();AddPet();Require(pets.Count==2,"Add another pet");second=pets[1];second.ProbePause();
                        Require(tray.Visible && first.ProbeHasNoTray && second.ProbeHasNoTray,"One shared tray icon");
                        Require(Math.Abs(first.Left-second.Left)>first.Width,"Separated initial positions");
                        Require(first.ProbeSettingsPath!=second.ProbeSettingsPath,"Separate profile paths");
                        first.ProbeSaveSettings(2,1.5);second.ProbeSaveSettings(1,.75);
                        Require(first.ProbeSize==1.5 && second.ProbeSize==.75,"Independent sizes");
                        secondSettings=File.ReadAllText(second.ProbeSettingsPath);
                        Require(first.ProbeSwitchSprites("white-ascii"),"First pet sprite switch");
                        Require(first.ProbeSpriteSet=="white-ascii" && second.ProbeSpriteSet=="black-ascii-sticker","Independent sprite sets");
                        Require(File.Exists(first.ProbeSpritePath) && !File.Exists(second.ProbeSpritePath),"Separate sprite preference files");
                        trayMenu.Show(new System.Drawing.Point(20,20));
                        Require(MenuVisible,"Shared tray menu opens");trayMenu.Hide();
                        first.PetMenu.Show(new System.Drawing.Point(20,20));
                        Require(first.PetMenu.Visible,"Same pet menu opens directly");first.PetMenu.Hide();
                        var pair=LoadRoster();Require(pair.Pets.Count==2,"Both pets persist in roster");
                        var restoredFirst=new CatWindow(defaultAssets,this,pair.Pets[0]);
                        try {Require(restoredFirst.ProbeSize==1.5 && restoredFirst.ProbeSpriteSet=="white-ascii","Saved size and sprite restore together");}
                        finally {restoredFirst.Close();}
                        first.ProbeOpenEditor();
                        var source=PresentationSource.FromVisual(first);
                        var windows=NativeWindows.Read(IntPtr.Zero,source.CompositionTarget.TransformFromDevice,IsPetWindow);
                        Require(IsPetWindow(new WindowInteropHelper(first).Handle) && IsPetWindow(new WindowInteropHelper(second).Handle),"All pet handles tracked");
                        Require(IsPetWindow(first.ProbeEditorHandle),"Settings handle tracked");
                        Require(!windows.Exists(w=>IsPetWindow(w.Id)),"Pet and editor windows excluded from platform discovery");
                        first.ProbeCloseEditor();first.ProbeSaveSettings(0,1.25);
                        Require(secondSettings==File.ReadAllText(second.ProbeSettingsPath),"Saving first leaves second settings unchanged");
                        first.ProbePause();second.ProbeWalk();firstX=first.Left;secondX=second.Left;
                        entries[first].PerformClick();stage=1;
                    }
                    else
                    {
                        Require(first.PetMenu.Visible,"Tray pet entry opens that pet's controls");first.PetMenu.Hide();
                        Require(Math.Abs(first.Left-firstX)<1,"First cat remains paused");
                        Require(second.Left-secondX>5,"Second cat moves independently");
                        survivor=profiles[second];first.Close();
                        Require(pets.Count==1 && pets[0]==second && second.IsVisible && tray.Visible,"Removing one leaves survivor and tray running");
                        var saved=LoadRoster();
                        Require(saved.Pets.Count==1 && saved.Pets[0].Id==survivor.Id,"Roster restores surviving pet identity");
                        var restored=new CatWindow(defaultAssets,this,saved.Pets[0]);
                        try {Require(restored.ProbeSize==.75,"Restored pet loads its saved size");}
                        finally {restored.Close();}
                        Require(File.Exists(ProfileFile(survivor,"last-run.log")),"Per-pet diagnostics");
                        File.AppendAllText(log,"PASS multi-pet integration: add; separate positions, settings, sizes, sprites and logs; one shared tray; independent pause/movement; all pet/editor windows excluded; remove preserves survivor; roster and settings restore.\r\n");
                        timer.Stop();trayMenu.Items[trayMenu.Items.Count-1].PerformClick();
                        Require(pets.Count==0 && disposed,"Exit all closes pets and disposes shared tray");
                        Require(LoadRoster().Pets.Count==1,"Exit all preserves roster");
                    }
                }
                catch(Exception ex)
                {
                    File.AppendAllText(log,"FAIL multi-pet integration: "+ex+"\r\n");Environment.ExitCode=1;timer.Stop();Exit();
                }
            };
            timer.Start();
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    }
    public partial class CatWindow
    {
        internal bool ProbeHasNoTray {get{return tray==null;}}
        internal string ProbeSettingsPath {get{return settingsPath;}}
        internal string ProbeSpritePath {get{return spritePreference;}}
        internal string ProbeSpriteSet {get{return spriteSet;}}
        internal double ProbeSize {get{return petSize;}}
        internal IntPtr ProbeEditorHandle {get{return new WindowInteropHelper(personalityEditor).Handle;}}
        internal void ProbePause(){paused=true;personality.CursorAttention=false;}
        internal void ProbeWalk(){paused=false;personality.CursorAttention=false;PlaceOnFloor(area.Left+20);direction=1;Start("walk",30);}
        internal void ProbeSaveSettings(int preset,double size){EditPersonality();personalityEditor.SelectPresetForTest(preset);personalityEditor.SetSizeForTest(size);personalityEditor.SaveForTest();}
        internal void ProbeOpenEditor(){EditPersonality();}
        internal void ProbeCloseEditor(){personalityEditor.Close();}
        internal bool ProbeSwitchSprites(string id){return SwitchSpriteSet(id,true,false);}
    }
}
