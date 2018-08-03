using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class EMWaveguide2DHPlaneFEM : FEM
    {
        // Solve
        // input
        public double WaveLength { get; set; }
        // output
        public System.Numerics.Complex[] Ez { get; private set; }
        public System.Numerics.Complex[][] S { get; private set; }

        public EMWaveguide2DHPlaneFEM() : base()
        {

        }

        public EMWaveguide2DHPlaneFEM(FEWorld world)
        {
            World = world;
        }

        public override void Solve()
        {
            Ez = null;
            S = null;
            
            // 波数
            double k0 = 2.0 * Math.PI / WaveLength;
            // 角周波数
            double omega = k0 * Constants.C0;

            int nodeCnt = (int)World.GetNodeCount();
            IList<uint> feIds = World.GetTriangleFEIds();

            var A = new Lapack.ComplexMatrix(nodeCnt, nodeCnt);
            var B = new System.Numerics.Complex[nodeCnt];

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
                double[][] sNuNvs = triFE.CalcSNuNvs();
                double[] sNxNx = sNuNvs[0];
                double[] sNyNx = sNuNvs[1];
                double[] sNxNy = sNuNvs[2];
                double[] sNyNy = sNuNvs[3];
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
                        int index = (int)(col * elemNodeCnt + row);
                        double a = (1.0 / ma.Muxx) * sNyNy[index] + (1.0 / ma.Muyy) * sNxNx[index] -
                            (k0 * k0 * ma.Epzz) * sNN[index];

                        A[rowNodeId, colNodeId] += (System.Numerics.Complex)a;
                    }
                }
            }

            uint portCnt = World.GetPortCount();
            var eigenFEMs = new EMWaveguide1DEigenFEM[portCnt];
            var betass = new System.Numerics.Complex[2][];
            var ezEVecss = new System.Numerics.Complex[2][][];
            for (uint portId = 0; portId < portCnt; portId++)
            {
                uint portNodeCnt = World.GetPortNodeCount(portId);

                var eigenFEM = new EMWaveguide1DEigenFEM(World, portId);
                eigenFEMs[portId] = eigenFEM;

                eigenFEM.WaveLength = WaveLength;
                eigenFEM.Solve();
                System.Numerics.Complex[] betas = eigenFEM.Betas;
                System.Numerics.Complex[][] ezEVecs = eigenFEM.EzEVecs;

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

                bool isIncidentPort = (portId == World.IncidentPortId);
                if (isIncidentPort)
                {
                    int incidentModeId = World.IncidentModeId;
                    System.Diagnostics.Debug.Assert(incidentModeId != -1);
                    System.Numerics.Complex beta0 = betas[incidentModeId];
                    System.Numerics.Complex[] ezEVec0 = ezEVecs[incidentModeId];
                    System.Numerics.Complex[] I = eigenFEM.CalcIncidentResidualVec(beta0, ezEVec0);
                    for (int row = 0; row < portNodeCnt; row++)
                    {
                        int rowCoId = World.PortNode2Coord(portId, row);
                        int rowNodeId = World.Coord2Node(rowCoId);

                        B[rowNodeId] += I[row];
                    }
                }
            }

            System.Numerics.Complex[] X;
            int xRow;
            int xCol;
            int ret = IvyFEM.Lapack.Functions.zgesv(out X, out xRow, out xCol,
                A.Buffer, A.RowSize, A.ColumnSize,
                B, B.Length, 1);
            Ez = X;

            S = new System.Numerics.Complex[portCnt][];
            for (uint portId = 0; portId < portCnt; portId++)
            {
                System.Numerics.Complex[] portEz = GetPortEz(portId, Ez);
                var eigenFEM = eigenFEMs[portId];
                var betas = betass[portId];
                var ezEVecs = ezEVecss[portId];
                int incidentModeId = -1;
                if (World.IncidentPortId == portId)
                {
                    incidentModeId = (int)World.IncidentModeId;
                }
                System.Numerics.Complex[] S1 = eigenFEM.CalcSMatrix(omega, incidentModeId, betas, ezEVecs, portEz);
                S[portId] = S1;
            }
        }

        private System.Numerics.Complex[] GetPortEz(uint portId, System.Numerics.Complex[] Ez)
        {
            int nodeCnt = (int)World.GetPortNodeCount(portId);
            System.Numerics.Complex[] portEz= new System.Numerics.Complex[nodeCnt];
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
