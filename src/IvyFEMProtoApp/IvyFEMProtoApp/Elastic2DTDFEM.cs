using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class Elastic2DTDFEM : FEM
    {
        public double TimeStep { get; private set; } = 0;
        public double NewmarkBeta { get; private set; } = 1.0 / 4.0;
        public double NewmarkGamma { get; private set; } = 1.0 / 2.0;
        uint ValueId { get; set; } = 0;
        uint PrevValueId { get; set; } = 0;

        //Solve
        // Output
        public double[] U { get; private set; }

        public Elastic2DTDFEM() : base()
        {

        }

        public Elastic2DTDFEM(FEWorld world, double timeStep,
            double newmarkBeta, double newmarkGamma,
            uint valueId, uint prevValueId)
        {
            World = world;
            TimeStep = timeStep;
            NewmarkBeta = newmarkBeta;
            NewmarkGamma = newmarkGamma;
            ValueId = valueId;
            PrevValueId = prevValueId;
        }

        public override void Solve()
        {
            int nodeCnt = (int)World.GetNodeCount();
            IList<uint> feIds = World.GetTriangleFEIds();

            double dt = TimeStep;
            double beta = NewmarkBeta;
            double gamma = NewmarkGamma;
            var FV = World.GetFieldValue(ValueId);
            var prevFV = World.GetFieldValue(PrevValueId);
            prevFV.CopyValues(FV);

            int dof = 2;
            var A = new Lapack.DoubleMatrix(nodeCnt * dof, nodeCnt * dof);
            var B = new double[nodeCnt * dof];

            foreach (uint feId in feIds)
            {
                TriangleFE triFE = World.GetTriangleFE(feId);
                int[] coIds = triFE.CoordIds;
                uint elemNodeCnt = triFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = coIds[iNode];
                    int nodeId = World.Coord2Node(coId);
                    nodes[iNode] = nodeId;
                }

                Material ma0 = World.GetMaterial(triFE.MaterialId);
                System.Diagnostics.Debug.Assert(ma0.MaterialType == MaterialType.ELASTIC);
                var ma = ma0 as ElasticMaterial;
                double lambda = ma.LameLambda;
                double mu = ma.LameMu;
                double rho = ma.MassDensity;
                double gx = ma.GravityX;
                double gy = ma.GravityY;

                double[] sNN = triFE.CalcSNN();
                double[][] sNuNvs = triFE.CalcSNuNvs();
                double[] sNxNx = sNuNvs[0];
                double[] sNyNx = sNuNvs[1];
                double[] sNxNy = sNuNvs[2];
                double[] sNyNy = sNuNvs[3];
                double lumpedSNN = triFE.CalcLumpedSNN();

                for (int row = 0; row < elemNodeCnt; row++)
                {
                    int rowNodeId = nodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    int rowCoId = coIds[row];
                    double[] prevU = prevFV.GetValue(rowCoId, FieldDerivationType.VALUE);
                    double[] prevVel = prevFV.GetValue(rowCoId, FieldDerivationType.VELOCITY);
                    double[] prevAcc = prevFV.GetValue(rowCoId, FieldDerivationType.ACCELERATION);

                    for (int col = 0; col < elemNodeCnt; col++)
                    {
                        int colNodeId = nodes[col];
                        if (colNodeId == -1)
                        {
                            continue;
                        }

                        double[] k = new double[dof * dof]; // 11,21,12,22の順
                        double[] m = new double[dof * dof]; // 11,21,12,22の順
                        System.Diagnostics.Debug.Assert(k.Length == 4);
                        System.Diagnostics.Debug.Assert(m.Length == 4);
                        int index = (int)(col * elemNodeCnt + row);

                        double kroneckerDelta = mu * (sNyNy[index] + sNxNx[index]);
                        k[0] = (lambda + mu) * sNxNx[index] + kroneckerDelta;
                        k[1] = lambda * sNyNx[index] + mu * sNxNy[index];
                        k[2] = lambda * sNxNy[index] + mu * sNyNx[index];
                        k[3] = (lambda + mu) * sNyNy[index] + kroneckerDelta;

                        m[0] = rho * sNN[index];
                        m[1] = 0.0;
                        m[2] = 0.0;
                        m[3] = rho * sNN[index];

                        for (int dofRow = 0; dofRow < dof; dofRow++)
                        {
                            for (int dofCol = 0; dofCol < dof; dofCol++)
                            {
                                int dofIndex = dofCol * dof + dofRow;
                                A[rowNodeId * dof + dofRow, colNodeId * dof + dofCol] +=
                                    (1.0 / (beta * dt * dt)) * m[dofIndex] +
                                    k[dofIndex];

                                B[rowNodeId * dof + dofRow] +=
                                    m[dofIndex] * (
                                    (1.0 / (beta * dt * dt)) * prevU[dofRow] +
                                    (1.0 / (beta * dt)) * prevVel[dofRow] +
                                    ((1.0 / (2.0 * beta)) - 1.0) * prevAcc[dofRow]);
                            }
                        }
                    }
                }

                for (int row = 0; row < elemNodeCnt; row++)
                {
                    int rowNodeId = nodes[row];
                    if (rowNodeId == -1)
                    {
                        continue;
                    }
                    B[rowNodeId * dof + 0] += rho * gx * lumpedSNN;
                    B[rowNodeId * dof + 1] += rho * gy * lumpedSNN;
                }
            }

            SetFixedCadsCondtion(World, A, B, nodeCnt, dof);

            double[] X;
            int xRow;
            int xCol;
            int ret = IvyFEM.Lapack.Functions.dgesv(out X, out xRow, out xCol,
                A.Buffer, A.RowSize, A.ColumnSize,
                B, B.Length, 1);
            U = X;
        }

        public void UpdateFieldValues()
        {
            World.UpdateFieldValueValuesFromNodeValue(ValueId, FieldDerivationType.VALUE, U);

            double dt = TimeStep;
            double beta = NewmarkBeta;
            double gamma = NewmarkGamma;

            var FV = World.GetFieldValue(ValueId);
            var prevFV = World.GetFieldValue(PrevValueId);
            double[] u = FV.GetValues(FieldDerivationType.VALUE);
            double[] vel = FV.GetValues(FieldDerivationType.VELOCITY);
            double[] acc = FV.GetValues(FieldDerivationType.ACCELERATION);
            double[] prevU = prevFV.GetValues(FieldDerivationType.VALUE);
            double[] prevVel = prevFV.GetValues(FieldDerivationType.VELOCITY);
            double[] prevAcc = prevFV.GetValues(FieldDerivationType.ACCELERATION);

            uint coCnt = World.GetCoordCount();
            int dof = 2;
            System.Diagnostics.Debug.Assert(u.Length == coCnt * dof);
            for (int iPt = 0; iPt < coCnt; iPt++)
            {
                for(int iDof = 0; iDof < dof; iDof++)
                {
                    int index = iPt * dof + iDof;
                    vel[index] =
                        (gamma / (beta * dt)) * (u[index] - prevU[index]) +
                        (1.0 - gamma / beta) * prevVel[index] +
                        dt * (1.0 - gamma / (2.0 * beta)) * prevAcc[index];
                    acc[index] =
                        (1.0 / (beta * dt * dt)) * (u[index] - prevU[index]) -
                        (1.0 / (beta * dt)) * prevVel[index] -
                        (1.0 / (2.0 * beta) - 1.0) * prevAcc[index];
                }
            }

        }
    }
}
