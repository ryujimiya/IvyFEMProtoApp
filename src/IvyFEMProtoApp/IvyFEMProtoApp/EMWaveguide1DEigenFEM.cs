using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class EMWaveguide1DEigenFEM
    {
        public FEWorld World { get; private set; } = null;
        public uint PortId { get; private set; } = 0;
        public IvyFEM.Lapack.DoubleMatrix Txx { get; private set; } = null;
        public IvyFEM.Lapack.DoubleMatrix Ryy { get; private set; } = null;
        public IvyFEM.Lapack.DoubleMatrix Uzz { get; private set; } = null;

        public EMWaveguide1DEigenFEM()
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
                for (int col = 0; col < elemNodeCnt; col++)
                {
                    int colNodeId = nodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }
                    for (int row = 0; row < elemNodeCnt; row++)
                    {
                        int rowNodeId = nodes[row];
                        if (rowNodeId == -1)
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

        public void Solve(double waveLength, 
            out IvyFEM.Lapack.Complex[] betas, out IvyFEM.Lapack.Complex[][] ezEVecs)
        {
            betas = null;
            ezEVecs = null;

            // 波数
            double k0 = 2.0 * Math.PI / waveLength;
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

            IvyFEM.Lapack.Complex[] eVals;
            IvyFEM.Lapack.Complex[][] eVecs;
            int ret = IvyFEM.Lapack.Functions.dggev(A.Buffer, A.RowSize, A.ColumnSize,
                B.Buffer, B.RowSize, B.ColumnSize,
                out eVals, out eVecs);

            SortEVals(eVals, eVecs);
            AdjustPhaseEVecs(eVecs);
            GetBetasEzVecs(omega, eVals, eVecs);
            betas = eVals;
            ezEVecs = eVecs;
        }

        private void SortEVals(IvyFEM.Lapack.Complex[] eVals, IvyFEM.Lapack.Complex[][] eVecs)
        {
            int modeCnt = eVals.Length;
            var eValEVecs = new List<KeyValuePair<IvyFEM.Lapack.Complex, IvyFEM.Lapack.Complex[]>>();
            for (int i = 0; i < modeCnt;  i++)
            {
                eValEVecs.Add(new KeyValuePair<Lapack.Complex, Lapack.Complex[]>(eVals[i], eVecs[i]));
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

        private void AdjustPhaseEVecs(IvyFEM.Lapack.Complex[][] eVecs)
        {
            int modeCnt = eVecs.Length;
            for (int iMode = 0; iMode < modeCnt; iMode++)
            {
                var eVec = eVecs[iMode];
                int nodeCnt = eVec.Length;
                IvyFEM.Lapack.Complex maxValue = new Lapack.Complex(0, 0);
                double maxAbs = 0;
                for (int iNode = 0; iNode < nodeCnt; iNode++)
                {
                    IvyFEM.Lapack.Complex value = eVec[iNode];
                    double abs = value.Magnitude;
                    if (abs > maxAbs)
                    {
                        maxAbs = abs;
                        maxValue = value;
                    }
                }
                IvyFEM.Lapack.Complex phase = maxValue / (IvyFEM.Lapack.Complex)maxAbs;

                for (int iNode = 0; iNode < nodeCnt; iNode++)
                {
                    eVec[iNode] /= phase;
                }
            }
        }

        private void GetBetasEzVecs(
            double omega, IvyFEM.Lapack.Complex[] eVals, IvyFEM.Lapack.Complex[][] eVecs)
        {
            int modeCnt = eVals.Length;
            for (int iMode = 0; iMode < modeCnt; iMode++)
            {
                var eVal = eVals[iMode];
                var beta = IvyFEM.Lapack.Complex.Sqrt(eVal);
                if (beta.Imaginary > 0)
                {
                    beta.Imaginary = -beta.Imaginary;
                }
                eVals[iMode] = beta;

                var eVec = eVecs[iMode];
                var e = new IvyFEM.Lapack.ComplexMatrix(eVec, eVec.Length, 1);
                var RyyZ = (IvyFEM.Lapack.ComplexMatrix)Ryy;
                var work = RyyZ * e;
                var work2 = IvyFEM.Lapack.Functions.zdotc(eVec, work.Buffer);
                var d = IvyFEM.Lapack.Complex.Sqrt(
                    (IvyFEM.Lapack.Complex)(omega * Constants.Mu0) /
                    (((IvyFEM.Lapack.Complex)beta.Magnitude) * work2));
                IvyFEM.Lapack.Functions.zscal(eVec, d);
            }
        }

        public IvyFEM.Lapack.ComplexMatrix CalcBoundaryMatrix(
            double omega, IvyFEM.Lapack.Complex[] betas, IvyFEM.Lapack.Complex[][] ezEVecs)
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
                        IvyFEM.Lapack.Complex value = (IvyFEM.Lapack.Complex.ImaginaryOne /
                            (IvyFEM.Lapack.Complex)(omega * Constants.Mu0)) *
                            beta * ((IvyFEM.Lapack.Complex)beta.Magnitude) *
                            vec1.Buffer[col] * vec2.Buffer[row];
                        X[row, col] += value;
                    }
                }
            }
            return X;
        }

        public IvyFEM.Lapack.Complex[] CalcIncidentResidualVec(
            IvyFEM.Lapack.Complex beta0, IvyFEM.Lapack.Complex[] ezEVec0)
        {
            IvyFEM.Lapack.Complex[] I = null;

            int nodeCnt = ezEVec0.Length;
            var ez = new IvyFEM.Lapack.ComplexMatrix(ezEVec0, nodeCnt, 1);
            var RyyZ = (IvyFEM.Lapack.ComplexMatrix)Ryy;
            var vec1 = RyyZ * ez;
            var a1 = IvyFEM.Lapack.Complex.ImaginaryOne * ((IvyFEM.Lapack.Complex)2.0) * beta0;
            IvyFEM.Lapack.Functions.zscal(vec1.Buffer, a1);
            I = vec1.Buffer;
            return I;
        }

        public IvyFEM.Lapack.Complex[] CalcSMatrix(double omega, int incidentModeId,
            IvyFEM.Lapack.Complex[] betas, IvyFEM.Lapack.Complex[][] ezEVecs,
            IvyFEM.Lapack.Complex[] Ez)
        {
            int modeCnt = betas.Length;
            int nodeCnt = ezEVecs[0].Length;
            IvyFEM.Lapack.Complex[] S = new IvyFEM.Lapack.Complex[modeCnt];

            for (int iMode = 0; iMode < modeCnt; iMode++)
            {
                var beta = betas[iMode];
                var ezEVec = ezEVecs[iMode];
                var ez = new IvyFEM.Lapack.ComplexMatrix(ezEVec, (int)nodeCnt, 1);
                var RyyZ = (IvyFEM.Lapack.ComplexMatrix)Ryy;
                var vec1 = RyyZ * IvyFEM.Lapack.ComplexMatrix.Conjugate(ez);
                IvyFEM.Lapack.Complex work1 = IvyFEM.Lapack.Functions.zdotu(vec1.Buffer, Ez);
                var b = (IvyFEM.Lapack.Complex)(beta.Magnitude / (omega * Constants.Mu0)) * work1;
                if (incidentModeId != -1 && incidentModeId == iMode)
                {
                    b = (IvyFEM.Lapack.Complex)(-1.0) + b;
                }
                S[iMode] = b;
            }

            return S;
        }
    }
}
