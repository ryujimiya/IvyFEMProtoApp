﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class EMWaveguide1DEigenFEM : FEM
    {
        public uint PortId { get; private set; } = 0;
        public IvyFEM.Lapack.DoubleMatrix Txx { get; private set; } = null;
        public IvyFEM.Lapack.DoubleMatrix Ryy { get; private set; } = null;
        public IvyFEM.Lapack.DoubleMatrix Uzz { get; private set; } = null;

        // Solve
        // Input
        public double WaveLength { get; set; }
        // Output
        public System.Numerics.Complex[] Betas { get; private set; }
        public System.Numerics.Complex[][] EzEVecs { get; private set; }

        public EMWaveguide1DEigenFEM() : base()
        {

        }

        public EMWaveguide1DEigenFEM(FEWorld world, uint portId)
        {
            World = world;
            PortId = portId;
            CalcMatrixs();
        }

        private void CalcMatrixs()
        {
            int nodeCnt = (int)World.GetPortNodeCount(PortId);
            IList<uint> feIds = World.GetPortLineFEIds(PortId);

            Txx = new IvyFEM.Lapack.DoubleMatrix(nodeCnt, nodeCnt);
            Ryy = new IvyFEM.Lapack.DoubleMatrix(nodeCnt, nodeCnt);
            Uzz = new IvyFEM.Lapack.DoubleMatrix(nodeCnt, nodeCnt);

            foreach (uint feId in feIds)
            {
                LineFE lineFE = World.GetPortLineFE(PortId, feId);
                uint elemNodeCnt = lineFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = lineFE.CoordIds[iNode];
                    int nodeId = World.PortCoord2Node(PortId, coId);
                    nodes[iNode] = nodeId;
                }

                Material ma0 = World.GetMaterial(lineFE.MaterialId);
                System.Diagnostics.Debug.Assert(ma0.MaterialType == MaterialType.DIELECTRIC);
                var ma = ma0 as DielectricMaterial;

                double[] sNN = lineFE.CalcSNN();
                double[] sNyNy = lineFE.CalcSNxNx();
                for (int row = 0; row < elemNodeCnt; row++)
                {
                    int rowNodeId = nodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    for (int col = 0; col < elemNodeCnt; col++)
                    {
                        int colNodeId = nodes[col];
                        if (colNodeId == -1)
                        {
                            continue;
                        }
                        double txxVal = (1.0 / ma.Muxx) * sNyNy[col * elemNodeCnt + row];
                        double ryyVal = (1.0 / ma.Muyy) * sNN[col * elemNodeCnt + row];
                        double uzzVal = ma.Epzz * sNN[col * elemNodeCnt + row];

                        Txx[rowNodeId, colNodeId] += txxVal;
                        Ryy[rowNodeId, colNodeId] += ryyVal;
                        Uzz[rowNodeId, colNodeId] += uzzVal;
                    }
                }
            }
        }

        public override void Solve()
        {
            Betas = null;
            EzEVecs = null;

            // 波数
            double k0 = 2.0 * Math.PI / WaveLength;
            // 角周波数
            double omega = k0 * Constants.C0;

            int nodeCnt = (int)World.GetPortNodeCount(PortId);
            var A = new IvyFEM.Lapack.DoubleMatrix(nodeCnt, nodeCnt);
            var B = new IvyFEM.Lapack.DoubleMatrix(nodeCnt, nodeCnt);
            for (int col = 0; col < nodeCnt; col++)
            {
                for (int row = 0; row < nodeCnt; row++)
                {
                    A[row, col] = (k0 * k0) * Uzz[row, col] - Txx[row, col];
                    B[row, col] = Ryy[row, col];
                }
            }

            System.Numerics.Complex[] eVals;
            System.Numerics.Complex[][] eVecs;
            int ret = IvyFEM.Lapack.Functions.dggev(A.Buffer, A.RowSize, A.ColumnSize,
                B.Buffer, B.RowSize, B.ColumnSize,
                out eVals, out eVecs);

            SortEVals(eVals, eVecs);
            AdjustPhaseEVecs(eVecs);
            GetBetasEzVecs(omega, eVals, eVecs);
            Betas = eVals;
            EzEVecs = eVecs;
        }

        private void SortEVals(System.Numerics.Complex[] eVals, System.Numerics.Complex[][] eVecs)
        {
            int modeCnt = eVals.Length;
            var eValEVecs = new List<KeyValuePair<System.Numerics.Complex, System.Numerics.Complex[]>>();
            for (int i = 0; i < modeCnt;  i++)
            {
                eValEVecs.Add(new KeyValuePair<System.Numerics.Complex, System.Numerics.Complex[]>(eVals[i], eVecs[i]));
            }
            eValEVecs.Sort((a, b) => 
            {
                // eVal(β^2) の実部を比較
                double diff = a.Key.Real - b.Key.Real;
                // 降順
                if (diff > 0)
                {
                    return -1;
                }
                else if (diff < 0)
                {
                    return 1;
                }
                return 0;
            });

            for (int i = 0; i < modeCnt; i++)
            {
                eVals[i] = eValEVecs[i].Key;
                eVecs[i] = eValEVecs[i].Value;
            }
        }

        private void AdjustPhaseEVecs(System.Numerics.Complex[][] eVecs)
        {
            int modeCnt = eVecs.Length;
            for (int iMode = 0; iMode < modeCnt; iMode++)
            {
                var eVec = eVecs[iMode];
                int nodeCnt = eVec.Length;
                System.Numerics.Complex maxValue = new System.Numerics.Complex(0, 0);
                double maxAbs = 0;
                for (int iNode = 0; iNode < nodeCnt; iNode++)
                {
                    System.Numerics.Complex value = eVec[iNode];
                    double abs = value.Magnitude;
                    if (abs > maxAbs)
                    {
                        maxAbs = abs;
                        maxValue = value;
                    }
                }
                System.Numerics.Complex phase = maxValue / maxAbs;

                for (int iNode = 0; iNode < nodeCnt; iNode++)
                {
                    eVec[iNode] /= phase;
                }
            }
        }

        private void GetBetasEzVecs(
            double omega, System.Numerics.Complex[] eVals, System.Numerics.Complex[][] eVecs)
        {
            int modeCnt = eVals.Length;
            for (int iMode = 0; iMode < modeCnt; iMode++)
            {
                var eVal = eVals[iMode];
                var beta = System.Numerics.Complex.Sqrt(eVal);
                if (beta.Imaginary > 0)
                {
                    beta = System.Numerics.Complex.Conjugate(beta);
                }
                eVals[iMode] = beta;

                var eVec = eVecs[iMode];
                var e = new IvyFEM.Lapack.ComplexMatrix(eVec, eVec.Length, 1);
                var RyyZ = (IvyFEM.Lapack.ComplexMatrix)Ryy;
                var work = RyyZ * e;
                var work2 = IvyFEM.Lapack.Functions.zdotc(eVec, work.Buffer);
                var d = System.Numerics.Complex.Sqrt(
                    (System.Numerics.Complex)(omega * Constants.Mu0) /
                    (((System.Numerics.Complex)beta.Magnitude) * work2));
                IvyFEM.Lapack.Functions.zscal(eVec, d);
            }
        }

        public IvyFEM.Lapack.ComplexMatrix CalcBoundaryMatrix(
            double omega, System.Numerics.Complex[] betas, System.Numerics.Complex[][] ezEVecs)
        {
            int nodeCnt = ezEVecs[0].Length;
            IvyFEM.Lapack.ComplexMatrix X = new Lapack.ComplexMatrix(nodeCnt, nodeCnt);

            int modeCnt = betas.Length;
            for (int iMode = 0; iMode < modeCnt; iMode++)
            {
                var beta = betas[iMode];
                var ezEVec = ezEVecs[iMode];
                var ez = new IvyFEM.Lapack.ComplexMatrix(ezEVec, nodeCnt, 1);
                var RyyZ = (IvyFEM.Lapack.ComplexMatrix)Ryy;
                var vec1 = RyyZ * ez;
                var vec2 = RyyZ * IvyFEM.Lapack.ComplexMatrix.Conjugate(ez);
                System.Diagnostics.Debug.Assert(vec1.RowSize == nodeCnt && vec1.ColumnSize == 1);
                System.Diagnostics.Debug.Assert(vec2.RowSize == nodeCnt && vec2.ColumnSize == 1);

                for (int col = 0; col < nodeCnt; col++)
                {
                    for (int row = 0; row < nodeCnt; row++)
                    {
                        System.Numerics.Complex value = (System.Numerics.Complex.ImaginaryOne /
                            (omega * Constants.Mu0)) *
                            beta * beta.Magnitude *
                            vec1.Buffer[col] * vec2.Buffer[row];
                        X[row, col] += value;
                    }
                }
            }
            return X;
        }

        public System.Numerics.Complex[] CalcIncidentResidualVec(
            System.Numerics.Complex beta0, System.Numerics.Complex[] ezEVec0)
        {
            System.Numerics.Complex[] I = null;

            int nodeCnt = ezEVec0.Length;
            var ez = new IvyFEM.Lapack.ComplexMatrix(ezEVec0, nodeCnt, 1);
            var RyyZ = (IvyFEM.Lapack.ComplexMatrix)Ryy;
            var vec1 = RyyZ * ez;
            var a1 = System.Numerics.Complex.ImaginaryOne * 2.0 * beta0;
            IvyFEM.Lapack.Functions.zscal(vec1.Buffer, a1);
            I = vec1.Buffer;
            return I;
        }

        public System.Numerics.Complex[] CalcSMatrix(double omega, int incidentModeId,
            System.Numerics.Complex[] betas, System.Numerics.Complex[][] ezEVecs,
            System.Numerics.Complex[] Ez)
        {
            int modeCnt = betas.Length;
            int nodeCnt = ezEVecs[0].Length;
            System.Numerics.Complex[] S = new System.Numerics.Complex[modeCnt];

            for (int iMode = 0; iMode < modeCnt; iMode++)
            {
                var beta = betas[iMode];
                var ezEVec = ezEVecs[iMode];
                var ez = new IvyFEM.Lapack.ComplexMatrix(ezEVec, (int)nodeCnt, 1);
                var RyyZ = (IvyFEM.Lapack.ComplexMatrix)Ryy;
                var vec1 = RyyZ * IvyFEM.Lapack.ComplexMatrix.Conjugate(ez);
                System.Numerics.Complex work1 = IvyFEM.Lapack.Functions.zdotu(vec1.Buffer, Ez);
                var b = (System.Numerics.Complex)(beta.Magnitude / (omega * Constants.Mu0)) * work1;
                if (incidentModeId != -1 && incidentModeId == iMode)
                {
                    b = (System.Numerics.Complex)(-1.0) + b;
                }
                S[iMode] = b;
            }

            return S;
        }
    }
}
