using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DesktopCat
{
    public partial class CatWindow
    {
        const double BodyHeight = 88 * (Scale / 0.58);
        const double WalkSpeed = 48 * (Scale / 0.58);
        double CursorMovementBudget { get { return 18+42*personality.Playfulness/100; } }
        double ChaseDuration { get { return 3+3*personality.Playfulness/100; } }
        double ChaseReach { get { return (6+6*personality.Playfulness/100)*BodyHeight; } }
        double PlayBreak { get { return 1+2*(1-personality.Playfulness/100); } }
        Personality personality = new Personality();
        Platform support;
        List<Platform> platforms = new List<Platform>();
        JumpPlan jump;
        double horizontalVelocity, jumpElapsed;
        double engagement, nearbyTime, cursorCooldown, glanceCooldown, wakeCooldown, disturbance, reactionTime, motionSignal;
        double destination, cursorTravel, cursorPosition;
        double motionDistance, motionSeconds, sampledSpeed, chaseInterest, decisionTime, retargetTime;
        double chaseWindup;
        double pawCooldown, pawInterest;
        double attentionAwakeTime;
        bool CursorKeepsAwake { get { return personality.CursorAttention && attentionAwakeTime>0; } }
        void RememberCursorAttention()
        {
            // Playful cats linger longer; even sleepy cats get a substantial wind-down.
            attentionAwakeTime=15+15*(personality.Playfulness/100)*(1-.5*personality.Sleepiness/100);
        }
        Func<Point> testCursor;
        double? testNow;
        bool ButtonsHeld { get { return testCursor==null && NativeWindows.ButtonHeld(); } }
        double CursorNow { get { return testNow.HasValue?testNow.Value:clock.Elapsed.TotalSeconds; } }
        Point lastCursor, lastApproachCursor;
        bool haveCursor, haveApproached, settingsOpen;
        PersonalityWindow personalityEditor;
        readonly Queue<Tuple<double,double>> cursorBudget = new Queue<Tuple<double,double>>();
        string settingsPath = AppPaths.UserFile("personality.json");
        bool badSettings;

        void LoadPersonality()
        {
            if (File.Exists(settingsPath))
            {
                try
                {
                    Personality saved = new JavaScriptSerializer().Deserialize<Personality>(File.ReadAllText(settingsPath));
                    if(saved == null || saved.SchemaVersion != 1) throw new InvalidDataException("Unsupported personality file");
                    saved.Validate();personality=saved;
                }
                catch { badSettings = true; }
            }
            support = new Platform { Id=IntPtr.Zero, Left=area.Left,Right=area.Right,Y=area.Bottom,WindowLeft=area.Left };
        }
        void EditPersonality()
        {
            if(personalityEditor!=null)
            {
                if(personalityEditor.WindowState==WindowState.Minimized)personalityEditor.WindowState=WindowState.Normal;
                personalityEditor.Activate();return;
            }
            // A modeless WPF window uses the same dispatcher as the pet and does not
            // pause its timer or disable its overlay/tray as a modal Forms dialog did.
            personalityEditor=new PersonalityWindow(personality,SavePersonality,badSettings);
            settingsOpen=true;
            personalityEditor.Closed+=delegate {personalityEditor=null;settingsOpen=false;};
            personalityEditor.Show();
            personalityEditor.Activate();
        }
        void SavePersonality(Personality value)
        {
            personality=value;
            try
            {
                if(badSettings && File.Exists(settingsPath))File.Copy(settingsPath,settingsPath+".invalid-"+DateTime.Now.ToString("yyyyMMddHHmmss"),false);
                string temp=settingsPath+".tmp";
                File.WriteAllText(temp,new JavaScriptSerializer().Serialize(personality));
                if(File.Exists(settingsPath))File.Replace(temp,settingsPath,null);else File.Move(temp,settingsPath);
                badSettings=false;
            }
            catch(Exception ex){MessageBox.Show(personalityEditor,"Settings apply for this session but could not be saved: "+ex.Message,"Cat personality");}
            disturbance=0;engagement=0;nearbyTime=0;chaseInterest=0;
        }
        void RefreshPlatforms()
        {
            var source=PresentationSource.FromVisual(this);
            if(source==null)return;
            area=SystemParameters.WorkArea;
            var windows=NativeWindows.Read(handle,source.CompositionTarget.TransformFromDevice);
            if(personalityEditor!=null)
            {
                IntPtr editorId=new System.Windows.Interop.WindowInteropHelper(personalityEditor).Handle;
                windows.RemoveAll(w=>w.Id==editorId);
            }
            platforms=Platforms.Build(windows,area,FootOffset,Width);
            if(paused || dragging)return;
            Left=Motion.Clamp(Left,area.Left,area.Right-Width);
            if(support!=null)
            {
                Platform replacement=null;double nextX=Left;
                foreach(Platform p in platforms)
                {
                    if(p.Id!=support.Id)continue;
                    double movedX=Left+p.WindowLeft-support.WindowLeft;
                    if(p.Id==IntPtr.Zero)movedX=Motion.Clamp(Left,p.Left,p.Right-Width);
                    if(p.Fits(movedX,Width)){replacement=p;nextX=movedX;break;}
                }
                if(replacement!=null)
                {
                    if(action=="walk")walkPosition+=nextX-Left;
                    if(action=="chase" || action=="approach_cursor")cursorPosition+=nextX-Left;
                    Left=nextX;Top=replacement.Y-FootOffset;support=replacement;
                }
                else
                {
                    support=null;jump=null;velocity=0;horizontalVelocity=0;Start("fall",double.MaxValue);
                }
            }
            else if(action!="fall" && action!="jump" && !pressed)
            {
                FindSupport();if(support==null)Start("fall",double.MaxValue);
            }
            if(Top+FootOffset>area.Bottom){Top=area.Bottom-FootOffset;FindSupport();velocity=0;jump=null;Start("land",.3);}
        }
        void FindSupport()
        {
            support=null;
            foreach(Platform p in platforms)
                if(Math.Abs(p.Y-(Top+FootOffset))<3 && p.Fits(Left,Width)){support=p;break;}
            if(support==null && Math.Abs(area.Bottom-(Top+FootOffset))<3)
                support=new Platform {Id=IntPtr.Zero,Left=area.Left,Right=area.Right,Y=area.Bottom,WindowLeft=area.Left};
        }
        bool TryJump()
        {
            if(support==null)return false;
            var options=new List<JumpPlan>();
            foreach(Platform p in platforms)
            {
                if(p.Id==support.Id)continue;
                JumpPlan plan=JumpPlan.Create(Left,Top+FootOffset,p,Width,BodyHeight,area);
                // Full canvas must stay on-screen throughout the arc.
                if(plan!=null && plan.Y-plan.VY*plan.VY/2000-FootOffset>=area.Top)options.Add(plan);
            }
            if(options.Count==0)return false;
            jump=options[random.Next(options.Count)];facingRight=jump.VX>0;Start("jump_crouch",.18);return true;
        }
        void LaunchJump()
        {
            JumpPlan fresh=null;
            if(jump!=null)
                foreach(Platform p in platforms)
                    if(p.Id==jump.Target.Id && Math.Abs(p.Y-jump.Target.Y)<1 && p.Fits(jump.X+jump.VX*jump.Duration,Width))
                        fresh=JumpPlan.Create(Left,Top+FootOffset,p,Width,BodyHeight,area);
            if(fresh==null || fresh.Y-fresh.VY*fresh.VY/2000-FootOffset<area.Top){jump=null;Start("idle",1);return;}
            jump=fresh;velocity=jump.VY;horizontalVelocity=jump.VX;jumpElapsed=0;support=null;
            action="jump";SetClip("jump_rise");
        }
        void AdvanceAir(double dt)
        {
            double oldFeet=Top+FootOffset,oldX=Left,newFeet,newX;
            if(jump!=null)
            {
                bool valid=false;
                foreach(Platform p in platforms)
                    if(p.Id==jump.Target.Id && Math.Abs(p.Y-jump.Target.Y)<1 && p.Fits(jump.X+jump.VX*jump.Duration,Width)){valid=true;break;}
                if(!valid){jump=null;horizontalVelocity=0;action="fall";SetClip("fall");}
            }
            if(jump!=null)
            {
                jumpElapsed+=dt;
                newFeet=jump.Y+jump.VY*jumpElapsed+500*jumpElapsed*jumpElapsed;
                newX=jump.X+jump.VX*jumpElapsed;
                velocity=jump.VY+1000*jumpElapsed;
                if(velocity>=0 && clip!="jump_fall")SetClip("jump_fall");
            }
            else { newFeet=oldFeet+velocity*dt+500*dt*dt;velocity+=1000*dt;newX=oldX+horizontalVelocity*dt; }
            newX=Motion.Clamp(newX,area.Left,area.Right-Width);
            Platform landing=velocity>=0?Platforms.Crossed(platforms,oldFeet,newFeet,oldX,newX,Width):null;
            if(landing!=null)
            {
                Left=Motion.Clamp(newX,landing.Left,landing.Right-Width);Top=landing.Y-FootOffset;support=landing;
                velocity=0;horizontalVelocity=0;jump=null;Start("land",.3);
            }
            else if(newFeet>=area.Bottom)
            {
                Left=newX;Top=area.Bottom-FootOffset;FindSupport();velocity=0;horizontalVelocity=0;jump=null;Start("land",.3);
            }
            else {Left=newX;Top=Math.Max(area.Top,newFeet-FootOffset);}
        }
        Rect Body { get { double inset=8*(Scale/.58);return new Rect(Left+inset,Top+FootOffset-BodyHeight,Width-2*inset,BodyHeight); } }
        void UpdateAttention(double dt)
        {
            if(!paused)attentionAwakeTime=Math.Max(0,attentionAwakeTime-dt);
            if(!personality.CursorAttention)attentionAwakeTime=0;
            cursorCooldown=Math.Max(0,cursorCooldown-dt);glanceCooldown=Math.Max(0,glanceCooldown-dt);wakeCooldown=Math.Max(0,wakeCooldown-dt);reactionTime=Math.Max(0,reactionTime-dt);
            pawCooldown=Math.Max(0,pawCooldown-dt);
            Point cursor=CursorPosition();double traveled=haveCursor?(cursor-lastCursor).Length:0;haveCursor=true;lastCursor=cursor;
            double distance=Personality.Distance(cursor,Body);
            bool suppressed=paused || dragging || pressed || settingsOpen || menu.Visible || !personality.CursorAttention || ButtonsHeld;
            if(suppressed){engagement=0;nearbyTime=0;disturbance=0;motionSignal=0;motionDistance=0;motionSeconds=0;sampledSpeed=0;chaseInterest=0;pawInterest=0;cursorCooldown=Math.Max(cursorCooldown,.5);return;}
            if(traveled>4*BodyHeight){motionDistance=0;motionSeconds=0;sampledSpeed=0;engagement=0;chaseInterest=0;}
            else {motionDistance+=traveled;motionSeconds+=dt;}
            if(motionSeconds>=.1)
            {
                sampledSpeed=motionDistance>=.5?motionDistance/motionSeconds:0;
                motionDistance=0;motionSeconds=0;
            }
            double stimulus=Math.Min(1,sampledSpeed/(2*BodyHeight));
            motionSignal+=(stimulus-motionSignal)*(1-Math.Exp(-dt/.5));
            nearbyTime=distance<6*BodyHeight?nearbyTime+dt:0;
            engagement=Personality.Engage(engagement,sampledSpeed,distance,BodyHeight,dt);
            if(pawCooldown<=0 && engagement>.15 && distance<.6*BodyHeight && sampledSpeed>=.06*BodyHeight)
                pawInterest+=dt*(.3+personality.Playfulness/100);
            else pawInterest=Math.Max(0,pawInterest-dt*.5);
            // Remember active interest across chase cooldowns and exhausted movement budgets.
            // Cursor motion alone must not renew this timer once the cat is asleep.
            if(action!="sleep" && engagement>.25 && sampledSpeed>=.06*BodyHeight && distance<4*BodyHeight)
                RememberCursorAttention();
            bool available=action=="idle" || action=="sit" || action=="look" || action=="walk";
            if(available && cursorCooldown<=0 && engagement>.2 && sampledSpeed>=.06*BodyHeight)
                chaseInterest=Math.Min(2,chaseInterest+dt*personality.ChaseInterest(distance,BodyHeight,engagement));
            else chaseInterest=Math.Max(0,chaseInterest-dt*.25);
            decisionTime+=dt;
            // Notice play while idling/walking; do not wait for a long action to end.
            if(available && support!=null && decisionTime>=.15)
            {
                decisionTime=0;
                TryCursorAction();
            }
            if(action=="sleep")
            {
                if(wakeCooldown>0)disturbance=0;
                else disturbance=personality.Disturb(disturbance,distance,BodyHeight,motionSignal,dt);
                if(disturbance>=personality.WakeThreshold-.001)
                {
                    wakeCooldown=30;Start("annoyed_look",1.1);return;
                }
                if(reactionTime==0 && disturbance>personality.WakeThreshold*.35)
                {
                    SetClip(disturbance>personality.WakeThreshold*.7?"sleep_peek":"sleep_twitch");reactionTime=5;
                }
                else if(clip!="sleep" && reactionTime<4)SetClip("sleep");
            }
        }
        double BudgetUsed()
        {
            double now=CursorNow;
            while(cursorBudget.Count>0 && cursorBudget.Peek().Item1<now-60)cursorBudget.Dequeue();
            double total=0;foreach(var item in cursorBudget)total+=item.Item2;return total;
        }
        bool TryCursorAction()
        {
            if(!personality.CursorAttention || nearbyTime<.5 || pressed || settingsOpen || ButtonsHeld || menu.Visible)return false;
            Point cursor=lastCursor;double d=Personality.Distance(cursor,Body),g=Personality.Proximity(d,BodyHeight);
            double p=personality.Playfulness/100,c=personality.Curiosity/100,a=personality.Affection/100,b=personality.Boldness/100,s=personality.Sleepiness/100;
            if(pawCooldown<=0 && engagement>.15 && d<.6*BodyHeight && pawInterest>=.15)
            {
                PlayPawSwipe();return true;
            }
            bool novel=!haveApproached || (cursor-lastApproachCursor).Length>.5*BodyHeight;
            bool chasing=engagement>.2 && chaseInterest>=.08+.12*(1-p);
            double gap=BodyHeight*(chasing?.35:.65);
            if(support!=null && cursorCooldown<=0 && nearbyTime>=.5+.4*(1-b) && (novel || chasing) && BudgetUsed()<CursorMovementBudget && d>=gap)
            {
                double approach=g*(.1+.45*c+.35*a+.1*b)*(1-.4*s);
                // Active play gets a short accumulation window before a passive
                // approach can consume its cooldown. Quiet hovering can still attract.
                if(chasing || (sampledSpeed<.06*BodyHeight && engagement<.2 && random.NextDouble()<approach*.25))
                {
                    direction=cursor.X>Body.Left+Body.Width/2?1:-1;
                    double desired=direction>0?cursor.X-Width-gap:cursor.X+gap;
                    destination=Motion.Clamp(desired,support.Left,support.Right-Width);
                    if(Math.Abs(destination-Left)>5 && Math.Sign(destination-Left)==direction)
                    {
                        facingRight=direction>0;action=chasing?"chase":"approach_cursor";stateTime=chasing?ChaseDuration:2;cursorTravel=0;cursorPosition=Left;
                        chaseWindup=chasing?.22:0;
                        SetClip(chasing?"stalk":"walk");RememberCursorAttention();lastApproachCursor=cursor;haveApproached=true;chaseInterest=0;retargetTime=0;return true;
                    }
                }
            }
            if(glanceCooldown<=0 && random.NextDouble()<g*(.25+.6*c+.15*p))
            {
                facingRight=cursor.X>Body.Left+Body.Width/2;Start("look",.5+random.NextDouble()*1.5);if(sampledSpeed>=.06*BodyHeight)RememberCursorAttention();glanceCooldown=5+random.Next(6);return true;
            }
            return false;
        }
        void AdvanceCursorMove(double dt)
        {
            Point cursor=lastCursor;
            double dist=Personality.Distance(cursor,Body);
            double gap=BodyHeight*(action=="chase"?.35:.65);
            double travelLimit=action=="chase"?ChaseReach:2*BodyHeight;
            bool stop=support==null || !personality.CursorAttention || settingsOpen || ButtonsHeld || menu.Visible || BudgetUsed()>=CursorMovementBudget || stateTime<=0 || cursorTravel>=travelLimit || dist<gap || dist>6*BodyHeight;
            if(!stop && chaseWindup>0)
            {
                chaseWindup=Math.Max(0,chaseWindup-dt);
                if(chaseWindup==0)SetClip("run");
                return;
            }
            retargetTime+=dt;
            if(!stop && action=="chase" && retargetTime>=.08)
            {
                retargetTime=0;
                double newDirection=cursor.X>Body.Left+Body.Width/2?1:-1;
                // Follow a lure that changes sides, with a brief turn/crouch.
                if(newDirection!=direction)
                {
                    direction=newDirection;facingRight=direction>0;chaseWindup=.12;SetClip("stalk");
                    destination=Motion.Clamp(direction>0?cursor.X-Width-gap:cursor.X+gap,support.Left,support.Right-Width);
                    return;
                }
                else destination=Motion.Clamp(direction>0?cursor.X-Width-gap:cursor.X+gap,support.Left,support.Right-Width);
            }
            if(!stop && Math.Sign(destination-cursorPosition)!=direction)stop=true;
            if(!stop)
            {
                dt=Math.Min(dt,Math.Max(0,CursorMovementBudget-BudgetUsed()));
                double speed=(action=="chase"?3.5*BodyHeight*(.6+.8*personality.Playfulness/100):WalkSpeed*.8)*personality.Speed;
                double amount=Math.Min(Math.Abs(destination-cursorPosition),Math.Min(speed*dt,travelLimit-cursorTravel));
                double x=Motion.Clamp(cursorPosition+direction*amount,support.Left,support.Right-Width);
                Rect next=new Rect(x+(Body.Left-Left),Body.Top,Body.Width,BodyHeight);
                if(Personality.Distance(cursor,next)<gap)stop=true;
                else
                {
                    cursorTravel+=Math.Abs(x-cursorPosition);cursorPosition=x;
                    Left=x;stateTime-=dt;cursorBudget.Enqueue(Tuple.Create(CursorNow,dt));
                    stop=Math.Abs(destination-cursorPosition)<1;
                }
            }
            if(stop)
            {
                bool caught=action=="chase" && support!=null && dist<.6*BodyHeight && !ButtonsHeld && !settingsOpen && !menu.Visible && personality.CursorAttention && pawCooldown<=0;
                cursorCooldown=PlayBreak;chaseInterest=0;chaseWindup=0;
                if(caught)PlayPawSwipe();else Start("look",.6);
            }
        }
        void PlayPawSwipe()
        {
            facingRight=lastCursor.X>Body.Left+Body.Width/2;
            Start("paw_swipe",.46);RememberCursorAttention();pawInterest=0;
            pawCooldown=.6+1.2*(1-personality.Playfulness/100);
            // Close-range pawing does not consume the whole movement cooldown.
            cursorCooldown=Math.Max(cursorCooldown,.35);
        }
        public string RuntimeSummary()
        {
            return "Runtime OK: state="+action+", exposed platforms="+platforms.Count+", support="+(support==null?"airborne":support.Id==IntPtr.Zero?"floor":"window")+", profile="+personality.Preset+", area="+area.ToString();
        }
    }
}
