using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DesktopCat
{
    public class Personality
    {
        public int SchemaVersion = 1;
        public string Preset = "Balanced Cat";
        public double Playfulness = 55, Sleepiness = 70, Curiosity = 50, Affection = 65, Boldness = 50, Sensitivity = 50, Patience = 65;
        public double Speed = 1;
        public double PetSize = 1.25;
        public bool CursorAttention = true;
        public static readonly string[] Names = { "Balanced Cat", "Sleepy Lap Cat", "Playful Kitten", "Curious Explorer", "Gentle Observer" };
        public static Personality ForPreset(int index)
        {
            double[][] values = { new double[] {55,70,50,65,50,50,65,1}, new double[] {25,95,20,90,35,25,85,.7}, new double[] {95,55,90,75,80,75,40,1.3}, new double[] {65,60,95,45,85,60,55,1.1}, new double[] {40,75,60,60,20,80,70,.8} };
            var v = values[index];
            return new Personality { Preset = Names[index], Playfulness = v[0], Sleepiness = v[1], Curiosity = v[2], Affection = v[3], Boldness = v[4], Sensitivity = v[5], Patience = v[6], Speed = v[7] };
        }
        public void Validate()
        {
            PetSize=Safe(PetSize,1.25,.5,2);
            Playfulness = Safe(Playfulness,55,0,100); Sleepiness=Safe(Sleepiness,70,0,100); Curiosity=Safe(Curiosity,50,0,100);
            Affection=Safe(Affection,65,0,100); Boldness=Safe(Boldness,50,0,100); Sensitivity=Safe(Sensitivity,50,0,100); Patience=Safe(Patience,65,0,100); Speed=Safe(Speed,1,.5,1.5);
        }
        static double Safe(double value, double fallback, double min, double max) { return double.IsNaN(value) || double.IsInfinity(value) ? fallback : Motion.Clamp(value,min,max); }
        public static double Proximity(double distance, double height)
        {
            double q = Math.Max(0,distance)/height, t = Motion.Clamp((q-6)/2,0,1);
            return (1-3*t*t+2*t*t*t)/(1+q*q/4);
        }
        public double WakeThreshold { get { return 3 + 4*Patience/100 + 3*Sleepiness/100; } }
        public double Disturb(double value, double distance, double height, double motion, double dt)
        {
            double n = Math.Pow(Math.Max(0,1-distance/(1.5*height)),2);
            return Motion.Clamp(value + dt*(n*motion*(.5+Sensitivity/100)-.25),0,WakeThreshold);
        }
        public static double Distance(Point point, Rect body)
        {
            double dx = Math.Max(0,Math.Max(body.Left-point.X,point.X-body.Right));
            double dy = Math.Max(0,Math.Max(body.Top-point.Y,point.Y-body.Bottom));
            return Math.Sqrt(dx*dx+dy*dy);
        }
        public double ChaseInterest(double distance, double height, double engagement)
        {
            return Proximity(distance,height)*(.05+.70*Playfulness/100+.25*Boldness/100)*(1-.6*Sleepiness/100)*engagement;
        }
        public static double Engage(double current, double movementSpeed, double distance, double height, double dt)
        {
            // Speeds are sampled over 100ms, not per-frame pixel displacements.
            bool active=movementSpeed>=.06*height && distance<6*height;
            return Motion.Clamp(current+dt*(active?1.0/.7:-.35),0,1);
        }
    }
}
