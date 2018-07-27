using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class EMWaveguide2DHPlane
    {
        public FEWorld World { get; private set; } = null;

        public EMWaveguide2DHPlane()
        {

        }

        public EMWaveguide2DHPlane(FEWorld world)
        {
            World = world;
        }

        public void Solve(double waveLength,
            out IvyFEM.Lapack.Complex[] Ez,
            out IvyFEM.Lapack.Complex[][] S)
        {
            Ez = null;
            S = null;
            
            // 波数
            double k0 = 2.0 * Math.PI / waveLength;
            // 角周波数
            double omega = k0 * Constants.C0;

            int nodeCnt = (int)World.GetNodeCount();
            IList<uint> feIds = World.GetTriangleFEIds();

            var A = new Lapack.ComplexMatrix(nodeCnt, nodeCnt);
            var B = new Lapack.Complex[nodeCnt];

            foreach (uint feId in feIds)
            {
                TriangleFE triFE = World.GetTriangleFE(feId);
                uint elemNodeCnt = triFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = triFE.CoordIds[iNode];
                    int nodeId = World.Coord2Node(coId);
                    nodes[iNode] = nodeId;
                }
                Material ma0 = World.GetMaterial(triFE.MaterialId);
                System.Diagnostics.Debug.Assert(ma0.MaterialType == MaterialType.DIELECTRIC);
                var ma = ma0 as DielectricMaterial;

                double[] sNN = triFE.CalcSNN();
                double[][] sNxNxs = triFE.CalcSNxNxs();
                double[] sNxNx = sNxNxs[0];
                double[] sNyNy = sNxNxs[1];
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
                        double a = (1.0 / ma.Muxx) * sNyNy[col * elemNodeCnt + row] +
                            (1.0 / ma.Muyy) * sNxNx[col * elemNodeCnt + row] -
                            (k0 * k0 * ma.Epzz) * sNN[col * elemNodeCnt + row];

                        A[rowNodeId, colNodeId] += (IvyFEM.Lapack.Complex)a;
                    }
                }
            }

            uint portCnt = World.GetPortCount();
            var eigenFEMs = new EMWaveguide1DEigenFEM[portCnt];
            var betass = new IvyFEM.Lapack.Complex[2][];
            var ezEVecss = new IvyFEM.Lapack.Complex[2][][];
            for (uint portId = 0; portId < portCnt; portId++)
            {
                uint portNodeCnt = World.GetPortNodeCount(portId);

                var eigenFEM = new EMWaveguide1DEigenFEM(World, portId);
                eigenFEMs[portId] = eigenFEM;

                IvyFEM.Lapack.Complex[] betas;
                IvyFEM.Lapack.Complex[][] ezEVecs;
                eigenFEM.Solve(waveLength, out betas, out ezEVecs);
                betass[portId] = betas;
                ezEVecss[portId] = ezEVecs;

                IvyFEM.Lapack.ComplexMatrix b = eigenFEM.CalcBoundaryMatrix(omega, betas, ezEVecs);
                for (int col = 0; col < portNodeCnt; col++)
                {
                    int colCoId = World.PortNode2Coord(portId, col);
                    int colNodeId = World.Coord2Node(colCoId);
                    for (int row = 0; row < portNodeCnt; row++)
                    {
                        int rowCoId = World.PortNode2Coord(portId, row);
                        int rowNodeId = World.Coord2Node(rowCoId);

                        A[rowNodeId, colNodeId] += b[row, col];
                    }
                }

                bool isIncidentPort = (portId == World.IncidentPotId);
                if (isIncidentPort)
                {
                    uint incidentModeId = World.IncidentModeId;
                    IvyFEM.Lapack.Complex beta0 = betas[incidentModeId];
                    IvyFEM.Lapack.Complex[] ezEVec0 = ezEVecs[incidentModeId];
                    IvyFEM.Lapack.Complex[] I = eigenFEM.CalcIncidentResidualVec(beta0, ezEVec0);
                    for (int row = 0; row < portNodeCnt; row++)
                    {
                        int rowCoId = World.PortNode2Coord(portId, row);
                        int rowNodeId = World.Coord2Node(rowCoId);

                        B[rowNodeId] += I[row];
                    }
                }
            }

            IvyFEM.Lapack.Complex[] X;
            int xRow;
            int xCol;
            int ret = IvyFEM.Lapack.Functions.zgesv(out X, out xRow, out xCol,
                A.Buffer, A.RowSize, A.ColumnSize,
                B, B.Length, 1);
            Ez = X;

            S = new IvyFEM.Lapack.Complex[portCnt][];
            for (uint portId = 0; portId < portCnt; portId++)
            {
                IvyFEM.Lapack.Complex[] portEz = GetPortEz(portId, Ez);
                var eigenFEM = eigenFEMs[portId];
                var betas = betass[portId];
                var ezEVecs = ezEVecss[portId];
                int incidentModeId = -1;
                if (World.IncidentPotId == portId)
                {
                    incidentModeId = (int)World.IncidentModeId;
                }
                IvyFEM.Lapack.Complex[] S1 = eigenFEM.CalcSMatrix(omega, incidentModeId, betas, ezEVecs, portEz);
                S[portId] = S1;
            }
        }

        private IvyFEM.Lapack.Complex[] GetPortEz(uint portId, IvyFEM.Lapack.Complex[] Ez)
        {
            int nodeCnt = (int)World.GetPortNodeCount(portId);
            IvyFEM.Lapack.Complex[] portEz= new IvyFEM.Lapack.Complex[nodeCnt];
            for (int row = 0; row < nodeCnt; row++)
            {
                int coId = World.PortNode2Coord(portId, row);
                int nodeId = World.Coord2Node(coId);
                portEz[row] = Ez[nodeId];
            }
            return portEz;
        }

    }
}
