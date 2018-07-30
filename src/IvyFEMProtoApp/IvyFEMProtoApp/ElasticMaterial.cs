﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class ElasticMaterial : Material
    {
        public double MassDensity { get => Values[0]; set => Values[0] = value; }
        public double LameLambda { get => Values[1]; set => Values[1] = value; }
        public double LameMu { get => Values[2]; set => Values[2] = value; }
        public double GravityX { get => Values[3]; set => Values[3] = value; }
        public double GravityY { get => Values[4]; set => Values[4] = value; }

        public ElasticMaterial()
        {
            MaterialType = MaterialType.ELASTIC;
            int len = 5;
            Values = new double[len];

            MassDensity = 1.0;
            LameLambda = 0.0;
            LameMu = 1.0;
            GravityX = 0.0;
            GravityY = 0.0;
        }

        public void SetYoungPoisson(double young, double poisson/*, bool isPlaneStress = true*/)
        {
            LameLambda = young * poisson / ((1.0 + poisson) * (1 - 2.0 * poisson));
            LameMu = young / (2.0 * (1.0 + poisson));
            /*
            if (!isPlaneStress)
            {
                LameLambda = 2 *LameLambda * LameMu / (LameLambda + 2 * LameMu);
            }
            */
        }

        public void GetYoungPoisson(out double young, out double poisson)
        {
            poisson = LameLambda * 0.5 / (LameLambda + LameMu);
            young = 2 * LameMu * (1 + poisson);
        }

    }
}
