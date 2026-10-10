using System;
using System.Windows;

namespace DesktopCat
{
    public partial class CatWindow
    {
        // Replays cursor samples through the real controller without moving the OS pointer.
        public string RunCursorProbe()
        {
            int checks=0;
            foreach(int hz in new int[]{30,60,120})
            {
                ResetCursorProbe();
                double startX=Left, time=0, chaseStart=-1, traveled=0;
                Point cursor=new Point(Left+Width+BodyHeight,Body.Top+BodyHeight/2);
                testCursor=delegate {return cursor;};
                for(int frame=0;frame<hz*10;frame++)
                {
                    double dt=1.0/hz;time+=dt;testNow=time;
                    // Slow wiggling: individual 60/120Hz samples are below the old .5px cutoff.
                    cursor=new Point(startX+Width+BodyHeight+Math.Sin(time*2)*12*(Scale/.58),Body.Top+BodyHeight/2);
                    UpdateAttention(dt);
                    if(action=="chase")
                    {
                        if(chaseStart<0)chaseStart=time;
                        double before=Left;AdvanceCursorMove(dt);traveled+=Math.Abs(Left-before);
                    }
                }
                if(chaseStart<0 || chaseStart>3 || traveled<20*(Scale/.58))throw new Exception("Slow nearby motion did not trigger a visible chase within 3 seconds at "+hz+"Hz");
                if(BudgetUsed()>CursorMovementBudget+.001)throw new Exception("Chase exceeded movement budget");
                checks+=2;
            }
            ResetCursorProbe();
            Point moving=new Point(Left+Width+BodyHeight,Body.Top+BodyHeight/2);
            testCursor=delegate{return moving;};
            action="chase";SetClip("run");direction=1;stateTime=2;destination=Left+100;cursorPosition=Left;lastApproachCursor=moving;lastCursor=moving;
            double old=Left;
            moving.X+=1.2*BodyHeight;lastCursor=moving;AdvanceCursorMove(.02);
            if(action!="chase" || Left<=old)throw new Exception("Moving target away immediately cancelled chase");checks++;
            moving=new Point(Body.Right,Body.Top+BodyHeight/2);lastCursor=moving;old=Left;AdvanceCursorMove(.02);
            if(action=="chase" || Left!=old)throw new Exception("Cat did not stop when pointer entered comfort gap");checks++;
            ResetCursorProbe();
            double chaseOrigin=Left;
            moving=new Point(Left+Width+2*BodyHeight,Body.Top+BodyHeight/2);
            testCursor=delegate{return moving;};
            bool pursued=false;
            for(int i=0;i<600;i++)
            {
                testNow=i/60.0;
                moving=new Point(Left+Width+2*BodyHeight+10*(Scale/.58)*Math.Sin(i/30.0),Body.Top+BodyHeight/2);
                UpdateAttention(1.0/60);
                if(action=="chase") {pursued=true;AdvanceCursorMove(1.0/60);}
                else if(pursued)break;
            }
            if(Left-chaseOrigin<4*BodyHeight || Left-chaseOrigin>ChaseReach+1.5)throw new Exception("Moving lure did not produce a longer bounded pursuit: travel="+(Left-chaseOrigin)+", limit="+ChaseReach+", action="+action);checks++;
            if(Math.Abs(cursorCooldown-PlayBreak)>.1)throw new Exception("Play cooldown did not follow personality");checks++;
            cursorBudget.Clear();cursorBudget.Enqueue(Tuple.Create(CursorNow,CursorMovementBudget));
            action="chase";stateTime=4;cursorTravel=0;old=Left;AdvanceCursorMove(.02);
            if(action=="chase" || Left!=old)throw new Exception("Movement continued after minute budget was exhausted");checks++;
            foreach(string mode in new string[]{"still","far","off","sleep"})
            {
                ResetCursorProbe();
                if(mode=="off")personality.CursorAttention=false;
                if(mode=="sleep")Start("sleep",60);
                double origin=Left;
                testCursor=delegate{return moving;};
                for(int i=0;i<600;i++)
                {
                    double t=i/60.0;testNow=t;
                    moving=new Point(origin+Width+(mode=="far"?9:1)*BodyHeight+(mode=="still"?0:12*(Scale/.58)*Math.Sin(t*2)),Body.Top+BodyHeight/2);
                    UpdateAttention(1.0/60);
                    if(action=="chase")throw new Exception("Unexpected chase with cursor mode: "+mode);
                }
                if(mode=="sleep" && action!="sleep")throw new Exception("Non-annoying motion interrupted sleep");
                checks++;
            }
            ResetCursorProbe();
            personality.Sleepiness=100;cursorCooldown=100;
            cursorBudget.Enqueue(Tuple.Create(0.0,CursorMovementBudget));
            testCursor=delegate{return moving;};
            for(int i=0;i<1200;i++)
            {
                double t=i/60.0;testNow=t;
                moving=new Point(Left+Width+BodyHeight+12*(Scale/.58)*Math.Sin(t*2),Body.Top+BodyHeight/2);
                UpdateAttention(1.0/60);
                if(i>120 && i%30==0)
                {
                    SelectAction();
                    if(action=="sleep" || !CursorKeepsAwake)throw new Exception("Engaged cat slept during chase cooldown/budget exhaustion");
                }
            }
            checks++;
            for(int i=0;i<600;i++){testNow=20+i/60.0;UpdateAttention(1.0/60);}
            if(!CursorKeepsAwake)throw new Exception("Attention wind-down ended too soon after play");checks++;
            for(int i=0;i<1800;i++){testNow=30+i/60.0;UpdateAttention(1.0/60);}
            if(CursorKeepsAwake)throw new Exception("Stationary cursor kept cat awake indefinitely");checks++;
            RememberCursorAttention();Command("sleep",60);
            if(action!="sleep" || CursorKeepsAwake)throw new Exception("Explicit Sleep did not override attention");checks++;
            Start("idle",60);RememberCursorAttention();personality.CursorAttention=false;UpdateAttention(.1);
            if(CursorKeepsAwake)throw new Exception("Disabled attention continued suppressing sleep");checks++;
            ResetCursorProbe();personality=Personality.ForPreset(2);
            moving=new Point(Left+Width+2*BodyHeight,Body.Top+BodyHeight/2);testCursor=delegate{return moving;};
            int bursts=0;string previousAction=action;double lureSide=1;
            for(int i=0;i<1200;i++)
            {
                testNow=i/60.0;
                if(Left>area.Right-Width-3*BodyHeight)lureSide=-1;
                if(Left<area.Left+3*BodyHeight)lureSide=1;
                moving=new Point((lureSide>0?Left+Width+2*BodyHeight:Left-2*BodyHeight)+5*Math.Sin(i/15.0),Body.Top+BodyHeight/2);
                UpdateAttention(1.0/60);
                if(action=="chase")
                {
                    if(previousAction!="chase")bursts++;
                    AdvanceCursorMove(1.0/60);
                }
                else {stateTime-=1.0/60;if(stateTime<=0)SelectAction();}
                previousAction=action;
            }
            if(bursts<3 || BudgetUsed()>CursorMovementBudget+.001)throw new Exception("Playful kitten did not offer repeated bounded play bouts: bursts="+bursts+", movement="+BudgetUsed());checks++;
            ResetCursorProbe();personality=Personality.ForPreset(2);
            testCursor=delegate{return moving;};int swipes=0;previousAction=action;
            for(int i=0;i<240;i++)
            {
                double t=i/60.0;testNow=t;
                moving=new Point(Body.Right+.3*BodyHeight+2*Math.Sin(t*4),Body.Top+BodyHeight/2);
                UpdateAttention(1.0/60);
                if(action=="paw_swipe" && previousAction!="paw_swipe")swipes++;
                previousAction=action;stateTime-=1.0/60;if(stateTime<=0)SelectAction();
            }
            if(swipes<3)throw new Exception("Close-range play did not produce repeated paw reactions");checks++;
            ResetCursorProbe();moving=new Point(Left-2*BodyHeight,Body.Top+BodyHeight/2);testCursor=delegate{return moving;};lastCursor=moving;
            action="chase";SetClip("run");direction=1;stateTime=4;destination=Left+100;cursorPosition=Left;retargetTime=.1;
            AdvanceCursorMove(.02);
            if(action!="chase" || direction!=-1 || clip!="stalk")throw new Exception("Changing lure side failed to turn the cat");checks++;
            old=Left;for(int i=0;i<20;i++)AdvanceCursorMove(.02);
            if(Left>=old)throw new Exception("Cat did not pursue after turning");checks++;
            testCursor=null;testNow=null;
            return "PASS cursor replay: "+checks+" checks; slow movement starts visible pursuit at 30/60/120Hz, moving target is followed, comfort gap/budget held, stationary/far/off/sleep do not chase. OS pointer untouched.";
        }
        void ResetCursorProbe()
        {
            personality=new Personality();paused=false;pressed=false;dragging=false;
            area=new Rect(0,0,1536,912);Left=300;Top=area.Bottom-FootOffset;
            support=new Platform {Left=area.Left,Right=area.Right,Y=area.Bottom};
            engagement=0;nearbyTime=0;cursorCooldown=0;glanceCooldown=0;wakeCooldown=0;disturbance=0;reactionTime=0;motionSignal=0;
            motionDistance=0;motionSeconds=0;sampledSpeed=0;chaseInterest=0;decisionTime=0;retargetTime=0;cursorTravel=0;chaseWindup=0;attentionAwakeTime=0;pawCooldown=0;pawInterest=0;
            haveCursor=false;haveApproached=false;cursorBudget.Clear();testNow=0;cursorPosition=Left;Start("idle",60);
        }
    }
}
