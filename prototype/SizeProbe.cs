using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;

namespace DesktopCat
{
    public partial class CatWindow
    {
        public string RunSizeProbe()
        {
            var legacy=new JavaScriptSerializer().Deserialize<Personality>("{\"SchemaVersion\":1,\"Playfulness\":80}");
            if(legacy.PetSize!=1.25 || legacy.Playfulness!=80)throw new Exception("Legacy size default/migration failed");
            var invalid=new Personality {PetSize=Double.NaN};invalid.Validate();
            if(invalid.PetSize!=1.25)throw new Exception("Invalid size fallback failed");
            invalid.PetSize=9;invalid.Validate();if(invalid.PetSize!=2)throw new Exception("Size maximum not enforced");
            invalid.PetSize=0;invalid.Validate();if(invalid.PetSize!=.5)throw new Exception("Size minimum not enforced");
            ResetCursorProbe();facingRight=false;
            foreach(double size in new double[]{.5,1.25,2})
            {
                double feet=Top+FootOffset;
                ApplyPetSize(size);
                if(Math.Abs(Width-98.6*size)>.001 || Math.Abs(Height-71.92*size)>.001 || Math.Abs(Body.Height-44*size)>.001 || Math.Abs(Top+FootOffset-feet)>1 || support==null)
                    throw new Exception("Resize failed dimensions or floor anchoring at "+size);
                SetClip("idle");
                int opaque=0;
                while(opaque<current.Width*current.Height && current.Pixels[opaque*4+3]<16)opaque++;
                if(opaque==current.Width*current.Height || !Hit(new Point((opaque%current.Width+.5)*Scale,(opaque/current.Width+.5)*Scale)) || Hit(new Point(0,0)))
                    throw new Exception("Scaled alpha hit test failed at "+size);
            }
            ApplyPetSize(.5);
            var narrow=new Platform {Id=new IntPtr(79),Left=300,Right=370,Y=area.Bottom-100};
            var floor=new Platform {Left=area.Left,Right=area.Right,Y=area.Bottom};
            platforms=new List<Platform>{narrow,floor};support=narrow;Left=305;Top=narrow.Y-FootOffset;
            paused=true;ApplyPetSize(2);
            if(!paused || support!=null || action!="fall")throw new Exception("Resize did not reject a narrow support/preserve pause");
            paused=false;
            for(int i=0;i<300 && support==null;i++)AdvanceAir(.02);
            if(support==null || support.Id!=IntPtr.Zero)throw new Exception("Resized cat failed to land safely");
            ApplyPetSize(1.25);ResetCursorProbe();
            return "PASS size: legacy default 1.25x; validated range; dimensions, floor anchoring and alpha hit testing at 0.5x/1.25x/2x; narrow-platform recovery and pause preserved.";
        }
    }
}
