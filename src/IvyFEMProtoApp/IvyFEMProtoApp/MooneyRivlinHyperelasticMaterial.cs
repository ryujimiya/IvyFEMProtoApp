using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class MooneyRivlinHyperelasticMaterial : Material
    {
        public double MassDensity { get => Values[0]; set => Values[0] = value; }
        public double GravityX { get => Values[1]; set => Values[1] = value; }
        public double GravityY { get => Values[2]; set => Values[2] = value; }
        public double C1 { get => Values[3]; set => Values[3] = value; }
        public double C2 { get => Values[4]; set => Values[4] = value; }

        public MooneyRivlinHyperelasticMaterial()
        {
            int len = 5;
            Values = new double[len];

            MassDensity = 1.0;
            GravityX = 0.0;
            GravityY = 0.0;
            C1 = 0.0;
            C2 = 0.0;
        }
    }
}
