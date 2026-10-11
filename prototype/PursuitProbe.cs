using System;
using System.Windows;

namespace DesktopCat
{
    public partial class CatWindow
    {
        void StepPursuitProbe(double dt)
        {
            testNow=CursorNow+dt;
            UpdateAttention(dt);AdvanceAnimation(dt);UpdatePlayPose();
            if(action=="chase" || action=="approach_cursor")AdvanceCursorMove(dt);
            else if(action=="jump" || action=="fall")AdvanceAir(dt);
            else {stateTime-=dt;if(stateTime<=0)SelectAction();}
        }
        void PreparePursuit(Point cursor, int wiggleRoll=0)
        {
            testWiggleRoll=delegate{return wiggleRoll;};
            cursorJumpCooldown=0;lastCursor=cursor;testCursor=delegate{return cursor;};
            engagement=1;nearbyTime=1;chaseInterest=1;
            if(!TryCursorAction() || action!="chase")throw new Exception("Pursuit did not enter anticipation");
        }
        int RunPursuitProbe()
        {
            int checks=0;
            int wiggles=0;
            for(int roll=0;roll<10;roll++)
            {
                ResetCursorProbe();
                PreparePursuit(new Point(Body.Right+BodyHeight,Body.Top+BodyHeight/2),roll);
                if(chaseWiggling)wiggles++;
                else if(chaseWindup!=0 || clip!="run")throw new Exception("Ordinary chase retained a wiggle delay");
            }
            if(wiggles!=1)throw new Exception("Wiggle gate does not select one of ten outcomes");
            checks++;
            foreach(int hz in new int[]{30,60,120})
            {
                ResetCursorProbe();
                Point overhead=new Point(Body.Left+Body.Width/2,Body.Top-1.2*BodyHeight);
                double startX=Left,startY=Top;
                PreparePursuit(overhead);
                for(int i=0;i<hz*.6;i++)StepPursuitProbe(1.0/hz);
                if(action!="chase" || chaseWindup<=0 || Left!=startX || Top!=startY || Math.Abs(playSway)<.01)
                    throw new Exception("Wiggle was not sustained before launch at "+hz+"Hz");
                for(int i=0;i<hz && action=="chase";i++)StepPursuitProbe(1.0/hz);
                if(action!="jump" || !cursorJump || jump==null)throw new Exception("Aligned overhead cursor failed to trigger a jump at "+hz+"Hz");
                double minimumTop=Top;bool airAttack=false;
                for(int i=0;i<hz*3 && support==null;i++)
                {
                    StepPursuitProbe(1.0/hz);minimumTop=Math.Min(minimumTop,Top);airAttack|=clip=="paw_swipe";
                }
                if(support==null || Math.Abs(Top-startY)>1 || startY-minimumTop<BodyHeight || startY-minimumTop>=5*BodyHeight || !airAttack)
                    throw new Exception("Pounce failed height/contact/landing checks at "+hz+"Hz");
                checks+=3;
            }
            ResetCursorProbe();
            var perch=new Platform {Id=new IntPtr(42),Left=250,Right=850,Y=area.Bottom-2*BodyHeight};
            platforms.Insert(0,perch);
            PreparePursuit(new Point(Body.Left+Body.Width/2,perch.Y-BodyHeight/2));
            for(int i=0;i<100 && action=="chase";i++)StepPursuitProbe(.02);
            if(jump==null || jump.Target.Id!=perch.Id)throw new Exception("Cursor pursuit did not select a closer window platform");
            // The ballistic path must remain committed when the lure changes sides.
            double lockedVX=jump.VX;
            testCursor=delegate{return new Point(Left-3*BodyHeight,Body.Top-BodyHeight);};
            StepPursuitProbe(.02);
            if(jump==null || jump.VX!=lockedVX)throw new Exception("Cursor redirected an airborne trajectory");
            for(int i=0;i<150 && support==null;i++)StepPursuitProbe(.02);
            if(support==null || support.Id!=perch.Id)throw new Exception("Pursuit did not land on selected window");
            checks+=3;

            ResetCursorProbe();platforms.Insert(0,perch);
            PreparePursuit(new Point(Body.Left+Body.Width/2,perch.Y-BodyHeight/2));
            for(int i=0;i<100 && action=="chase";i++)StepPursuitProbe(.02);
            platforms.Remove(perch);
            for(int i=0;i<150 && support==null;i++)StepPursuitProbe(.02);
            if(support==null || support.Id!=IntPtr.Zero || cursorJump)throw new Exception("Removed cursor-jump target did not recover to floor");
            checks++;

            ResetCursorProbe();
            Point unreachable=new Point(Body.Left+Body.Width/2,area.Bottom-5.7*BodyHeight);
            PreparePursuit(unreachable);
            for(int i=0;i<100 && action=="chase";i++)StepPursuitProbe(.02);
            if(action!="look" || stateTime<1.5 || support==null || !CursorKeepsAwake)throw new Exception("Unreachable cursor was treated as caught instead of watched");
            checks++;

            ResetCursorProbe();cursorJumpCooldown=0;
            stateTime=5;lastCursor=new Point(Body.Right+BodyHeight,Body.Top+BodyHeight/2);
            testCursor=delegate{return lastCursor;};
            if(!LaunchCursorJump(lastCursor))throw new Exception("Ground chase did not offer a short pounce");
            if(jump.Target.Id!=IntPtr.Zero || Math.Abs(jump.VX)<1)throw new Exception("Ground pounce had no horizontal movement");
            checks++;

            ResetCursorProbe();cursorJumpCooldown=0;stateTime=5;
            Point nearby=new Point(Body.Left+Body.Width/2,Body.Top-BodyHeight);
            testCursor=delegate{return nearby;};
            cursorBudget.Enqueue(Tuple.Create(CursorNow,CursorMovementBudget-.01));
            if(CursorJumpPlan(nearby)!=null)throw new Exception("Jump ignored remaining movement budget");
            cursorBudget.Clear();personality.CursorAttention=false;
            if(CursorJumpPlan(nearby)!=null)throw new Exception("Jump ignored disabled cursor attention");
            checks+=2;

            ResetCursorProbe();lastCursor=new Point(Body.Right,Body.Top+BodyHeight/2);
            PlayPawSwipe();double duration=stateTime;int repeats=0;
            for(int i=0;i<60;i++)
            {
                int oldFrame=frameIndex;AdvanceAnimation(1.0/60);
                if(frameIndex<oldFrame)repeats++;
            }
            if(duration<1.25 || duration>1.91 || repeats<2 || pawCooldown<duration)throw new Exception("Long paw attack did not repeat its animation");
            checks++;
            // Cursor-height formula is the actual apex, including the physics helper.
            foreach(double heightInBodies in new double[]{.1,.5,1.5})
            {
                ResetCursorProbe();cursorJumpCooldown=0;stateTime=5;
                double cursorY=heightInBodies==.1?Body.Top+BodyHeight/2:Body.Bottom-(heightInBodies+.5)*BodyHeight;
                Point target=new Point(Body.Right+.8*BodyHeight,cursorY);
                testCursor=delegate{return target;};
                JumpPlan plan=CursorJumpPlan(target);
                if(plan==null || Math.Abs(plan.VY*plan.VY/2000-heightInBodies*BodyHeight)>.001)
                    throw new Exception("Cursor height did not produce the requested apex: "+heightInBodies+"H");
                checks++;
            }
            ResetCursorProbe();
            // The endpoint's X is correct, but this deliberately shallow arc cannot
            // reach a high cursor anywhere along the flight.
            var shallow=JumpPlan.Pursue(Left,Body.Bottom,support,Left+BodyHeight,.1*BodyHeight,Width,BodyHeight,area,FootOffset);
            if(shallow==null || JumpReachesCursor(shallow,new Point(Left+BodyHeight+Width/2,Body.Top-2*BodyHeight)))
                throw new Exception("Trajectory check accepted horizontal alignment without vertical reach");
            checks++;
            foreach(int hz in new int[]{30,60,120})
            {
                ResetCursorProbe();
                Point target=new Point(Body.Right+.2*BodyHeight,Body.Top+BodyHeight/2);
                testCursor=delegate{return target;};lastCursor=target;PlayPawSwipe();
                for(int i=0;i<hz/2;i++)StepPursuitProbe(1.0/hz);
                if(action!="paw_swipe")throw new Exception("Paw attack stopped while cursor remained in range");
                target.X=Body.Right+2*BodyHeight;StepPursuitProbe(1.0/hz);
                if(action=="paw_swipe" || clip=="paw_swipe" || pawCooldown>.26)
                    throw new Exception("Ground pawing did not cancel on cursor exit at "+hz+"Hz");
                checks++;
                ResetCursorProbe();cursorJumpCooldown=0;stateTime=5;
                target=new Point(Body.Left+Body.Width/2,Body.Top-BodyHeight);
                testCursor=delegate{return target;};lastCursor=target;
                if(!LaunchCursorJump(target))throw new Exception("Air paw cancellation fixture could not jump");
                for(int i=0;i<hz*2 && clip!="paw_swipe" && support==null;i++)StepPursuitProbe(1.0/hz);
                if(clip!="paw_swipe" || support!=null)throw new Exception("Fixture never swiped in flight");
                JumpPlan committed=jump;
                target=new Point(area.Right,area.Top);StepPursuitProbe(1.0/hz);
                if(clip=="paw_swipe" || airPawTime!=0 || action!="jump" || jump!=committed)
                    throw new Exception("Air pawing did not cancel while preserving trajectory at "+hz+"Hz");
                for(int i=0;i<hz*3 && support==null;i++)StepPursuitProbe(1.0/hz);
                if(support==null)throw new Exception("Cancelling airborne swipe prevented landing");
                checks++;
            }
            ResetCursorProbe();return checks;
        }
    }
}
