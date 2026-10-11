using System;
using System.Windows;

namespace DesktopCat
{
    public partial class CatWindow
    {
        void ApplyPetSize(double multiplier)
        {
            if(Math.Abs(petSize-multiplier)<.0001)return;
            double feet=Top+FootOffset,center=Left+Width/2;
            bool wasAirborne=support==null || jump!=null;
            petSize=multiplier;
            Width=CanvasW;Height=CanvasH;
            Left=Motion.Clamp(center-Width/2,area.Left,area.Right-Width);
            Top=Motion.Clamp(feet-FootOffset,area.Top,area.Bottom-FootOffset);
            // An old trajectory was calculated for another body size. Re-evaluate
            // the support and let gravity take over if it no longer fits.
            jump=null;cursorJump=false;airPawTime=0;horizontalVelocity=0;velocity=0;
            if(PresentationSource.FromVisual(this)!=null)RefreshPlatforms();
            FindSupport();
            if(support==null)Start("fall",double.MaxValue);
            else if(wasAirborne || action=="jump_crouch" || action=="chase" || action=="approach_cursor")Start("idle",.5);
            walkPosition=Left;cursorPosition=Left;chaseWindup=0;chaseWiggling=false;
            DrawFrame();
        }
    }
}
