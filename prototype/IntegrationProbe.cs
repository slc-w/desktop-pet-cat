using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DesktopCat
{
    public partial class CatWindow
    {
        // Opt-in native integration fixture. It never reads or changes another app.
        public void RunIntegrationProbe(string log)
        {
            var fixture=new Window { Title="Desktop Cat temporary platform test", Width=650, Height=120, Left=area.Left+80, Top=area.Bottom-3*BodyHeight, Topmost=true, ShowActivated=false, Content=new TextBlock {Text="Temporary cat platform test — closes automatically",Margin=new Thickness(20)} };
            fixture.Show();
            IntPtr fixtureId=new WindowInteropHelper(fixture).Handle;
            var probe=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(350)};
            int stage=0,ticks=0;
            double oldX=0,oldY=0;
            bool savedAttention=personality.CursorAttention;
            personality.CursorAttention=false;
            Start("idle",60);
            probe.Tick+=delegate {
                try
                {
                    if(++ticks>45)throw new Exception("Integration probe timed out at stage "+stage);
                    if(stage==0)
                    {
                        RefreshPlatforms();
                        Platform p=platforms.Find(item=>item.Id==fixtureId);
                        if(p==null)throw new Exception("Native test window was not discovered as an exposed platform");
                        jump=JumpPlan.Create(Left,Top+FootOffset,p,Width,BodyHeight,area);
                        if(jump==null)throw new Exception("Native test window was not reachable");
                        Start("jump_crouch",.18);stage=1;
                    }
                    else if(stage==1 && support!=null && support.Id==fixtureId)
                    {
                        Start("idle",60);oldX=Left;oldY=Top;
                        fixture.Left+=20;fixture.Top-=10;stage=2;
                    }
                    else if(stage==2)
                    {
                        RefreshPlatforms();
                        if(support==null || support.Id!=fixtureId || Math.Abs(Left-oldX-20)>2 || Math.Abs(Top-oldY+10)>2)
                            throw new Exception("Cat failed to track its moving support");
                        fixture.Close();stage=3;
                    }
                    else if(stage==3 && support!=null && support.Id==IntPtr.Zero)
                    {
                        File.AppendAllText(log,"PASS native integration: discovered real window; crouched/jumped/landed; rode moved support; recovered to floor after close.\r\n");
                        probe.Stop();personality.CursorAttention=savedAttention;Close();
                    }
                }
                catch(Exception ex)
                {
                    File.AppendAllText(log,"FAIL native integration: "+ex+"\r\n");Environment.ExitCode=1;
                    probe.Stop();fixture.Close();personality.CursorAttention=savedAttention;Close();
                }
            };
            probe.Start();
        }
    }
}
