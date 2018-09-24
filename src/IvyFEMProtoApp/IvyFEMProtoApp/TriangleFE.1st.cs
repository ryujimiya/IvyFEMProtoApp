﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    partial class TriangleFE
    {
        public double[] Calc1stN(double[] L)
        {
            double[] N = new double[3];

            // N = L
            L.CopyTo(N, 0);

            return N;
        }

        public double[][] Calc1stNu(double[] L)
        {
            double[][] Nu = new double[2][];
            double[] a;
            double[] b;
            double[] c;
            CalcTransMatrix(out a, out b, out c);

            // dN/dx
            Nu[0] = b;

            // dN/dy
            Nu[1] = c;
            return Nu;
        }

        public double[] Calc1stSN()
        {
            double A = GetArea();
            double[] sN = new double[3]
            {
                A / 3.0,
                A / 3.0,
                A / 3.0
            };
            return sN;
        }

        public double[,] Calc1stSNN()
        {
            double A = GetArea();
            double[,] sNN = new double[3, 3]
            {
                { 2.0, 1.0, 1.0 },
                { 1.0, 2.0, 1.0 },
                { 1.0, 1.0, 2.0 }
            };
            for (int i = 0; i < sNN.GetLength(0); i++)
            {
                for (int j = 0; j < sNN.GetLength(1); j++)
                {
                    sNN[i, j] *= A / 12.0;
                }
            }
            return sNN;
        }

        public double[,][,] Calc1stSNuNv()
        {
            double A = GetArea();
            double[] a;
            double[] b;
            double[] c;
            CalcTransMatrix(out a, out b, out c);

            double[,][,] sNuNv = new double[2, 2][,];

            // sNxNx
            sNuNv[0, 0] = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[0, 0][i, j] = A * b[i] * b[j];
                }
            }

            // sNxNy
            sNuNv[0, 1] = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[0, 1][i, j] = A * b[i] * c[j];
                }
            }

            // sNyNx
            sNuNv[1, 0] = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[1, 0][i, j] = A * c[i] * b[j];
                }
            }

            // sNyNy
            sNuNv[1, 1] = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sNuNv[1, 1][i, j] = A * c[i] * c[j];
                }
            }
            return sNuNv;
        }
    }
}
