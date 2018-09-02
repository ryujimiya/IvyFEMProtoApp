﻿using System;
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
            int t;
            Ez = null;
            S = null;
            
            // 波数
            double k0 = 2.0 * Math.PI / WaveLength;
            // 角周波数
            double omega = k0 * Constants.C0;

            t = System.Environment.TickCount;
            int nodeCnt = (int)World.GetNodeCount();
            var A = new IvyFEM.Linear.ComplexSparseMatrix(nodeCnt, nodeCnt);
            var B = new System.Numerics.Complex[nodeCnt];
            CalcAB(k0, A, B, nodeCnt);
            System.Diagnostics.Debug.WriteLine("CalcAB t = " + (System.Environment.TickCount - t));

            t = System.Environment.TickCount;
            uint portCnt = World.GetPortCount();
            EMWaveguide1DEigenFEM[] eigenFEMs;
            SetBoundaryCondition(omega, A, B, portCnt, out eigenFEMs);
            System.Diagnostics.Debug.WriteLine("SetBoundaryCondition t = " + (System.Environment.TickCount - t));

            t = System.Environment.TickCount;
            //----------------------------------
            System.Numerics.Complex[] X;
            Solver.ComplexSolve(out X, A, B);
            Ez = X;
            //----------------------------------
            System.Diagnostics.Debug.WriteLine("Solve t = " + (System.Environment.TickCount - t));

            t = System.Environment.TickCount;
            S = CalcS(omega, Ez, portCnt, eigenFEMs);
            System.Diagnostics.Debug.WriteLine("CalcS t = " + (System.Environment.TickCount - t));
        }

        private void CalcAB(double k0,
            IvyFEM.Linear.ComplexSparseMatrix A, System.Numerics.Complex[] B, int nodeCnt)
        {
            IList<uint> feIds = World.GetTriangleFEIds();
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
                System.Diagnostics.Debug.Assert(ma0.MaterialType == MaterialType.Dielectric);
                var ma = ma0 as DielectricMaterial;

                double[,] sNN = triFE.CalcSNN();
                double[,][,] sNuNv = triFE.CalcSNuNv();
                double[,] sNxNx = sNuNv[0, 0];
                double[,] sNyNx = sNuNv[1, 0];
                double[,] sNxNy = sNuNv[0, 1];
                double[,] sNyNy = sNuNv[1, 1];
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
                        double a = (1.0 / ma.Muxx) * sNyNy[row, col] + (1.0 / ma.Muyy) * sNxNx[row, col] -
                            (k0 * k0 * ma.Epzz) * sNN[row, col];

                        A[rowNodeId, colNodeId] += (System.Numerics.Complex)a;
                    }
                }
            }
        }

        private void SetBoundaryCondition(double omega,
            IvyFEM.Linear.ComplexSparseMatrix A, System.Numerics.Complex[] B,
            uint portCnt, out EMWaveguide1DEigenFEM[] eigenFEMs)
        {
            eigenFEMs = new EMWaveguide1DEigenFEM[portCnt];
            for (uint portId = 0; portId < portCnt; portId++)
            {
                uint portNodeCnt = World.GetPortNodeCount(portId);

                var eigenFEM = new EMWaveguide1DEigenFEM(World, portId);
                eigenFEMs[portId] = eigenFEM;

                eigenFEM.WaveLength = WaveLength;
                eigenFEM.Solve();
                System.Numerics.Complex[] betas = eigenFEM.Betas;
                System.Numerics.Complex[][] ezEVecs = eigenFEM.EzEVecs;
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
                    System.Numerics.Complex[] I = eigenFEM.CalcIncidentVec(beta0, ezEVec0);
                    for (int row = 0; row < portNodeCnt; row++)
                    {
                        int rowCoId = World.PortNode2Coord(portId, row);
                        int rowNodeId = World.Coord2Node(rowCoId);

                        B[rowNodeId] += I[row];
                    }
                }
            }
        }

        private System.Numerics.Complex[][] CalcS(double omega,
            System.Numerics.Complex[] Ez,
            uint portCnt, EMWaveguide1DEigenFEM[] eigenFEMs)
        {
            var S = new System.Numerics.Complex[portCnt][];
            for (uint portId = 0; portId < portCnt; portId++)
            {
                System.Numerics.Complex[] portEz = GetPortEz(portId, Ez);
                var eigenFEM = eigenFEMs[portId];
                System.Numerics.Complex[] betas = eigenFEM.Betas;
                System.Numerics.Complex[][] ezEVecs = eigenFEM.EzEVecs;
                int incidentModeId = -1;
                if (World.IncidentPortId == portId)
                {
                    incidentModeId = (int)World.IncidentModeId;
                }
                System.Numerics.Complex[] S1 = eigenFEM.CalcSMatrix(omega, incidentModeId, betas, ezEVecs, portEz);
                S[portId] = S1;
            }
            return S;
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
