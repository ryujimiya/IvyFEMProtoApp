using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class IntegrationPoints
    {
        public int PointCount { get; set; } = 0;
        public double[][] L { get; set; } = null;
        public double[] Weight { get; set; } = null;

        public IntegrationPoints()
        {

        }

        public IntegrationPoints(IntegrationPoints src)
        {
            PointCount = src.PointCount;
            L = null;
            if (src.L != null)
            {
                L = new double[src.L.Length][];
                for (int i = 0; i < src.L.Length; i++)
                {
                    double[] srcPoint = src.L[i];
                    L[i] = new double[srcPoint.Length];
                    srcPoint.CopyTo(L[i], 0);
                }
            }
            Weight = null;
            if (src.Weight != null)
            {
                Weight = new double[src.Weight.Length];
                src.Weight.CopyTo(Weight, 0);
            }
        }

        // α, β, γ
        private static double[] TriangleIP3Alpha = { 1.0 / 3.0, 0.6, 0.2 };
        // α, β, γ, δ, ε
        private static double[] TriangleIP7Alpha = 
            { 1.0 / 3.0, 0.05971587, 0.47014206, 0.79742669, 0.10128651};

        public static IntegrationPoints[] TriangleIntegrationPoints =
        {
            new IntegrationPoints{
                PointCount = 1,
                L = new double[1][]
                {
                    new double[3]{1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0}
                },
                Weight = new double[1]
                {
                    1.0
                }
            },
            new IntegrationPoints{
                PointCount = 3,
                L = new double[3][]
                {
                    new double[3] {1.0 / 2.0, 1.0 / 2.0, 0.0},
                    new double[3] {0.0, 1.0 / 2.0, 1.0 / 2.0},
                    new double[3] {1.0 / 2.0, 0.0, 1.0 / 2.0}
                },
                Weight = new double[3]
                {
                    1.0 / 3.0,
                    1.0 / 3.0,
                    1.0 / 3.0
                }
            },
            new IntegrationPoints{
                PointCount = 4,
                L = new double[4][]
                {
                    new double[3] {TriangleIP3Alpha[0], TriangleIP3Alpha[0], TriangleIP3Alpha[0]},
                    new double[3] {TriangleIP3Alpha[1], TriangleIP3Alpha[2], TriangleIP3Alpha[2]},
                    new double[3] {TriangleIP3Alpha[2], TriangleIP3Alpha[1], TriangleIP3Alpha[2]},
                    new double[3] {TriangleIP3Alpha[2], TriangleIP3Alpha[2], TriangleIP3Alpha[1]}
                },
                Weight = new double[4]
                {
                    -27.0 / 48.0,
                    25.0 / 48.0,
                    25.0 / 48.0,
                    25.0 / 48.0
                }
            },
            new IntegrationPoints{
                PointCount = 7,
                L = new double[7][]
                {
                    new double[3] {TriangleIP7Alpha[0], TriangleIP7Alpha[0], TriangleIP7Alpha[0]},
                    new double[3] {TriangleIP7Alpha[1], TriangleIP7Alpha[2], TriangleIP7Alpha[2]},
                    new double[3] {TriangleIP7Alpha[2], TriangleIP7Alpha[1], TriangleIP7Alpha[2]},
                    new double[3] {TriangleIP7Alpha[2], TriangleIP7Alpha[2], TriangleIP7Alpha[1]},
                    new double[3] {TriangleIP7Alpha[3], TriangleIP7Alpha[4], TriangleIP7Alpha[4]},
                    new double[3] {TriangleIP7Alpha[4], TriangleIP7Alpha[3], TriangleIP7Alpha[4]},
                    new double[3] {TriangleIP7Alpha[4], TriangleIP7Alpha[4], TriangleIP7Alpha[3]}
                },
                Weight = new double[7]
                {
                    0.225,
                    0.13239415,
                    0.13239415,
                    0.13239415,
                    0.12593918,
                    0.12593918,
                    0.12593918
                }
            }
        };

    }
}
