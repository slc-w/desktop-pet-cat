using System;
using System.Collections.Generic;
using System.Windows;

namespace DesktopCat
{
    public static class BehaviorTests
    {
        static int checks;
        static void Check(bool condition,string message){checks++;if(!condition)throw new Exception("Self-test: "+message);}
        public static string Run()
        {
            checks=0;
            var area=new Rect(0,0,1400,1000);
            var target=new Platform {Left=0,Right=1000,Y=560};
            Check(JumpPlan.Create(100,1000,target,180,88,area)==null,"exactly 5H rejected");
            target.Y=568.8;
            var plan=JumpPlan.Create(100,1000,target,180,88,area);
            Check(plan!=null,"4.9H permitted when clear");
            Check(Math.Abs(plan.Y+plan.VY*plan.Duration+500*plan.Duration*plan.Duration-target.Y)<.001,"trajectory reaches target");
            target.Y=551.2;Check(JumpPlan.Create(100,1000,target,180,88,area)==null,"5.1H rejected");
            target.Y=800;target.Left=900;target.Right=1300;Check(JumpPlan.Create(0,1000,target,180,88,area)==null,"horizontal reach enforced");
            target.Left=0;target.Right=100;Check(JumpPlan.Create(0,1000,target,180,88,area)==null,"narrow platform rejected");
            target=new Platform {Left=0,Right=1000,Y=1000};
            plan=JumpPlan.Pursue(100,1000,target,200,88,180,88,area,140);
            Check(plan!=null && Math.Abs(plan.Y+plan.VY*plan.Duration+500*plan.Duration*plan.Duration-target.Y)<.001,"same-platform pursuit arc lands exactly");
            Check(JumpPlan.Pursue(100,1000,target,200,440,180,88,area,140)==null,"pursuit apex at exactly 5H rejected");
            Check(JumpPlan.Pursue(100,1000,target,500,88,180,88,area,140)==null,"pursuit horizontal reach enforced");
            Check(JumpPlan.Pursue(100,160,target,200,88,180,88,area,140)==null,"unsafe pursuit drop rejected");
            target.Y=160;
            Check(JumpPlan.Pursue(100,160,target,200,88,180,88,area,140)==null,"pursuit full-canvas headroom enforced");
            target.Y=568.8;
            Check(JumpPlan.Pursue(100,1000,target,200,435.6,180,88,area,140)!=null,"4.9H pursuit platform remains reachable");
            var windows=new List<WindowSurface> {
                new WindowSurface {Id=new IntPtr(1),Bounds=new Rect(0,200,500,500),Candidate=true},
                new WindowSurface {Id=new IntPtr(2),Bounds=new Rect(0,400,1000,500),Candidate=true}
            };
            var surfaces=Platforms.Build(windows,area,140,180);
            Platform lower=surfaces.Find(p=>p.Id==new IntPtr(2));
            Check(lower!=null && lower.Left>=500,"occluded part removed");
            windows[0].Bounds=new Rect(0,200,1100,700);
            Check(!Platforms.Build(windows,area,140,180).Exists(p=>p.Id==new IntPtr(2)),"fully covered target excluded");
            windows[0].Bounds=new Rect(0,0,1400,1000);
            Check(Platforms.Build(windows,area,140,180).Count==1,"maximized leaves floor only");
            var levels=new List<Platform>{new Platform{Left=0,Right=1000,Y=400},new Platform{Left=0,Right=1000,Y=600}};
            Check(Platforms.Crossed(levels,300,700,100,100,180).Y==400,"fast fall lands first crossed surface");
            Check(Platforms.Crossed(levels,700,300,100,100,180)==null,"one-way platform ascent");
            Check(Platforms.Crossed(levels,300,700,1100,1100,180)==null,"fall misses out-of-range platform");
            Check(Personality.Proximity(0,88)==1 && Personality.Proximity(88,88)>Personality.Proximity(176,88),"gravity-like interest");
            Check(Personality.Proximity(704,88)==0,"far cursor ignored");
            var cat=new Personality();
            Check(cat.Disturb(1,0,88,0,1)<1,"stationary pointer disturbance decays");
            Check(cat.Disturb(0,200,88,1,10)==0,"distant movement cannot disturb sleep");
            double a=0,b=0;
            for(int i=0;i<300;i++)a=cat.Disturb(a,0,88,1,1.0/60);
            for(int i=0;i<150;i++)b=cat.Disturb(b,0,88,1,1.0/30);
            Check(Math.Abs(a-b)<.001,"disturbance frame-rate independence");
            double baseRate=cat.Disturb(0,0,88,1,1);cat.Sensitivity=100;
            Check(cat.Disturb(0,0,88,1,1)>baseRate,"sensitivity increases buildup");
            double threshold=cat.WakeThreshold;cat.Patience=100;Check(cat.WakeThreshold>threshold,"patience raises wake threshold");
            cat.Speed=100;cat.Playfulness=-4;cat.Validate();Check(cat.Speed==1.5 && cat.Playfulness==0,"settings clamp");
            return checks+" behavior checks passed: platform occlusion, jump bounds/trajectory, swept landing, proximity, disturbance and settings.";
        }
    }
}
