using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Threading;

namespace DesktopCat
{
    public partial class CatWindow
    {
        public void RunSettingsProbe(string log)
        {
            // Use a dedicated profile so validation cannot change the user's settings.
            settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings-probe-"+Guid.NewGuid().ToString("N")+".json");
            personality=new Personality {CursorAttention=false};
            ReturnToFloor();direction=1;Start("walk",30);
            var probe=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(600)};
            int stage=0;double previousX=Left;
            EditPersonality();
            probe.Tick+=delegate {
                try
                {
                    if(stage==0)
                    {
                        if(personalityEditor==null || !personalityEditor.IsVisible || paused || !settingsOpen)throw new Exception("Editor is not visible/modeless");
                        if(Left-previousX<5)throw new Exception("Pet movement stalled while editor was open");
                        var first=personalityEditor;EditPersonality();
                        if(personalityEditor!=first)throw new Exception("Opening settings twice created duplicate windows");
                        double beforeSize=petSize;
                        personalityEditor.SetSizeForTest(2);personalityEditor.Close();
                        if(petSize!=beforeSize)throw new Exception("Cancel applied the size slider");
                        if(settingsOpen || personalityEditor!=null || personality.Playfulness!=55 || paused)throw new Exception("Cancel changed profile or left pet paused");
                        EditPersonality();personalityEditor.SetSizeForTest(1.5);personalityEditor.SelectPresetForTest(2);
                        if(personalityEditor.SizeForTest!=1.5)throw new Exception("Personality preset reset the size slider");
                        personalityEditor.SaveForTest();
                        var saved=new JavaScriptSerializer().Deserialize<Personality>(File.ReadAllText(settingsPath));
                        if(saved.Playfulness!=95 || personality.Playfulness!=95 || settingsOpen)throw new Exception("Save did not apply/persist/close");
                        // A visible native window rounds its bounds to device pixels.
                        if(Math.Abs(saved.PetSize-1.5)>.001 || Math.Abs(petSize-1.5)>.001 || Math.Abs(Width-98.6*1.5)>1.5)
                            throw new Exception("Size did not apply and persist: saved="+saved.PetSize+", applied="+petSize+", width="+Width);
                        paused=true;EditPersonality();
                        if(personalityEditor.SizeForTest!=1.5)throw new Exception("Reopened editor lost saved size");
                        personalityEditor.Close();
                        if(!paused)throw new Exception("Settings overrode an existing pause");
                        paused=false;previousX=Left;stage=1;
                    }
                    else
                    {
                        if(Left-previousX<5 || paused)throw new Exception("Cat failed to continue moving after settings closed");
                        File.AppendAllText(log,"PASS settings integration: editor visible/modeless; live cat keeps moving; duplicate-open reuses window; Cancel preserves profile; Save applies/persists; existing pause preserved; movement continues after close.\r\n");
                        probe.Stop();Close();
                    }
                }
                catch(Exception ex)
                {
                    File.AppendAllText(log,"FAIL settings integration: "+ex+"\r\n");Environment.ExitCode=1;probe.Stop();Close();
                }
            };
            probe.Start();
        }
    }
}
