using System;
using System.Windows;

namespace DesktopCat
{
    public partial class CatWindow
    {
        double ChaseWiggleDuration { get { return .9+.3*(1-personality.Playfulness/100); } }
        double PawAttackDuration { get { return 1.25+.65*personality.Playfulness/100; } }
        double cursorJumpCooldown, airPawTime;
        bool cursorJump;
        bool chaseWiggling;
        Func<int> testWiggleRoll;
        void ChooseChaseWiggle()
        {
            // One independent 10% roll per new chase; turning never rolls again.
            chaseWiggling=(testWiggleRoll==null?random.Next(10):testWiggleRoll())==0;
            chaseWindup=chaseWiggling?ChaseWiggleDuration:0;
        }
        bool CanPlayWithCursor { get { return personality.CursorAttention && !paused && !pressed && !dragging && !settingsOpen && !InteractionMenuVisible && !ButtonsHeld; } }

        double CursorDestination(Point cursor, double gap)
        {
            // Align under/over elevated targets; a horizontal comfort gap only makes
            // sense when the cursor is at body level.
            bool vertical = cursor.Y < Body.Top-.25*BodyHeight || cursor.Y > Body.Bottom+.25*BodyHeight;
            return Motion.Clamp(vertical ? cursor.X-Width/2 : direction>0 ? cursor.X-Width-gap : cursor.X+gap, support.Left, support.Right-Width);
        }

        JumpPlan CursorJumpPlan(Point cursor)
        {
            if (support == null || cursorJumpCooldown>0 || !CanPlayWithCursor) return null;
            double distance = Personality.Distance(cursor,Body), feet=Top+FootOffset;
            double desiredRise=feet-(cursor.Y+BodyHeight/2);
            double cursorHeight=Math.Max(.1*BodyHeight,desiredRise);
            JumpPlan best=null;
            double bestDistance=distance-.3*BodyHeight;
            foreach (Platform target in platforms)
            {
                if (target.Id==support.Id && Math.Abs(target.Y-support.Y)<1) continue;
                double x=Motion.Clamp(cursor.X-Width/2,target.Left,target.Right-Width);
                Rect body=new Rect(x+(Body.Left-Left),target.Y-BodyHeight,Body.Width,BodyHeight);
                double landingDistance=Personality.Distance(cursor,body);
                if (landingDistance>=bestDistance) continue;
                double rise=feet-target.Y;
                double apexRise=Math.Max(cursorHeight,rise+Math.Min(.3*BodyHeight,(5*BodyHeight-Math.Max(0,rise))*.5));
                JumpPlan plan=JumpPlan.Pursue(Left,feet,target,x,apexRise,Width,BodyHeight,area,FootOffset);
                if (AffordableJump(plan) && JumpReachesCursor(plan,cursor)) {best=plan;bestDistance=landingDistance;}
            }
            if (best!=null) return best;
            // A short pounce can land back on the current platform. An unreachable
            // overhead target gets a watching pose instead of repeated futile leaps.
            if (cursor.Y>Body.Bottom+.25*BodyHeight || desiredRise>=5*BodyHeight || (cursor.Y>=Body.Top && distance>2*BodyHeight)) return null;
            double landingX=Motion.Clamp(cursor.X-Width/2,support.Left,support.Right-Width);
            if (Math.Abs(landingX-Left)<.2*BodyHeight && cursor.Y>=Body.Top) return null;
            JumpPlan hop=JumpPlan.Pursue(Left,feet,support,landingX,cursorHeight,Width,BodyHeight,area,FootOffset);
            return AffordableJump(hop) && JumpReachesCursor(hop,cursor) ? hop : null;
        }

        bool JumpReachesCursor(JumpPlan plan, Point cursor)
        {
            // Check the body along the arc, including any earlier one-way landing.
            // At least 120 samples per second keeps even low, fast hops well sampled.
            int samples=Math.Max(16,(int)Math.Ceiling(plan.Duration*120));
            double oldX=plan.X,oldFeet=plan.Y;
            for(int i=1;i<=samples;i++)
            {
                double t=plan.Duration*i/samples;
                double x=plan.X+plan.VX*t, feet=plan.Y+plan.VY*t+500*t*t;
                Platform landing=Platforms.Crossed(platforms,oldFeet,feet,oldX,x,Width);
                if(landing!=null)
                {
                    double fraction=feet==oldFeet?1:Motion.Clamp((landing.Y-oldFeet)/(feet-oldFeet),0,1);
                    x=oldX+(x-oldX)*fraction;feet=landing.Y;
                }
                Rect body=new Rect(x+(Body.Left-Left),feet-BodyHeight,Body.Width,BodyHeight);
                if(Personality.Distance(cursor,body)<.6*BodyHeight)return true;
                if(landing!=null)return false;
                oldX=x;oldFeet=feet;
            }
            return false;
        }

        void UpdatePawContact(double distance)
        {
            if(paused)return;
            if(CanPlayWithCursor && distance<.6*BodyHeight)return;
            if(action!="paw_swipe" && airPawTime<=0)return;
            airPawTime=0;pawInterest=0;pawCooldown=.25;cursorCooldown=.25;
            if(action=="paw_swipe")Start("look",.25);
            else if(action=="jump" || action=="fall")SetClip(jump==null?"fall":velocity>=0?"jump_fall":"jump_rise");
        }

        bool AffordableJump(JumpPlan plan)
        {
            return plan!=null && BudgetUsed()+plan.Duration<=CursorMovementBudget && Math.Abs(plan.VX*plan.Duration)+cursorTravel<=ChaseReach && plan.Duration<=stateTime;
        }

        bool LaunchCursorJump(Point cursor)
        {
            JumpPlan plan=CursorJumpPlan(cursor);
            if (plan==null) return false;
            jump=plan;cursorJump=true;airPawTime=0;cursorJumpCooldown=2.5+2*(1-personality.Boldness/100);
            velocity=plan.VY;horizontalVelocity=plan.VX;jumpElapsed=0;support=null;
            if (Math.Abs(plan.VX)>.1) facingRight=plan.VX>0;
            action="jump";SetClip("jump_rise");RememberCursorAttention();
            return true;
        }

        void FinishCursorLanding()
        {
            bool wasPursuit=cursorJump;
            cursorJump=false;airPawTime=0;
            if (!wasPursuit) {Start("land",.3);return;}
            cursorCooldown=PlayBreak;chaseInterest=0;chaseWindup=0;
            if (CanPlayWithCursor && Personality.Distance(lastCursor,Body)<.6*BodyHeight) PlayPawSwipe();
            else {Start("land",.3);RememberCursorAttention();}
        }
    }
}
