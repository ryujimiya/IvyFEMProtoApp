﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    partial class Elastic2DTDFEM
    {
        protected void CalcSaintVenantHyperelasticElementAB(
            uint feId, IvyFEM.Linear.DoubleSparseMatrix A, double[] B)
        {
            uint quantityId = QuantityIds[0];
            int nodeCnt = NodeCounts[0];
            int dof = Dofs[0];

            double dt = TimeStep;
            double beta = NewmarkBeta;
            double gamma = NewmarkGamma;
            var FV = World.GetFieldValue(ValueId);

            TriangleFE triFE = World.GetTriangleFE(quantityId, feId);
            Material ma0 = World.GetMaterial(triFE.MaterialId);
            if (!(ma0 is SaintVenantHyperelasticMaterial))
            {
                return;
            }
            int[] coIds = triFE.NodeCoordIds;
            uint elemNodeCnt = triFE.NodeCount;
            int[] nodes = new int[elemNodeCnt];
            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
            {
                int coId = coIds[iNode];
                int nodeId = World.Coord2Node(quantityId, coId);
                nodes[iNode] = nodeId;
            }

            var ma = ma0 as SaintVenantHyperelasticMaterial;
            double lambda = ma.LameLambda;
            double mu = ma.LameMu;
            double rho = ma.MassDensity;
            double[] g = { ma.GravityX, ma.GravityY };

            double[] sN = triFE.CalcSN();
            double[,] sNN = triFE.CalcSNN();
            IntegrationPoints ip = triFE.GetIntegrationPoints(TriangleIntegrationPointCount.Point1);
            System.Diagnostics.Debug.Assert(ip.Ls.Length == 1);
            double[] L = ip.Ls[0];
            double[][] Nu = triFE.CalcNu(L);
            double detJ = triFE.GetDetJacobian(L);
            double weight = ip.Weights[0];
            double detJWeight = (1.0 / 2.0) * weight * detJ;

            double[,] uu = new double[dof, dof];
            for (int iDof = 0; iDof < dof; iDof++)
            {
                for (int jDof = 0; jDof < dof; jDof++)
                {
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        int iNodeId = nodes[iNode];
                        if (iNodeId == -1)
                        {
                            continue;
                        }
                        uu[iDof, jDof] += U[iNodeId * dof + iDof] * Nu[jDof][iNode];
                    }
                }
            }

            double[,] e = new double[dof, dof];
            for (int iDof = 0; iDof < dof; iDof++)
            {
                for (int jDof = 0; jDof < dof; jDof++)
                {
                    e[iDof, jDof] = (1.0 / 2.0) * (uu[iDof, jDof] + uu[jDof, iDof]);
                    for (int kDof = 0; kDof < dof; kDof++)
                    {
                        e[iDof, jDof] += (1.0 / 2.0) * uu[kDof, iDof] * uu[kDof, jDof];
                    }
                }
            }

            double[,,,] b = new double[elemNodeCnt, dof, dof, dof];
            {
                double[,] f = new double[dof, dof];
                for (int iDof = 0; iDof < dof; iDof++)
                {
                    for (int jDof = 0; jDof < dof; jDof++)
                    {
                        f[iDof, jDof] = uu[iDof, jDof];
                    }
                    f[iDof, iDof] += 1.0;
                }
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    for (int iDof = 0; iDof < dof; iDof++)
                    {
                        for (int gDof = 0; gDof < dof; gDof++)
                        {
                            for (int hDof = 0; hDof < dof; hDof++)
                            {
                                b[iNode, iDof, gDof, hDof] = Nu[hDof][iNode] * f[iDof, gDof];
                            }
                        }
                    }
                }
            }

            double[,] s = new double[dof, dof];
            {
                double tmp = 0.0;
                for (int iDof = 0; iDof < dof; iDof++)
                {
                    for (int jDof = 0; jDof < dof; jDof++)
                    {
                        s[iDof, jDof] = 2.0 * mu * e[iDof, jDof];
                    }
                    tmp += e[iDof, iDof];
                }
                for (int iDof = 0; iDof < dof; iDof++)
                {
                    s[iDof, iDof] += lambda * tmp;
                }
            }

            double[,] q = new double[elemNodeCnt, dof];
            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
            {
                for (int iDof = 0; iDof < dof; iDof++)
                {
                    for (int gDof = 0; gDof < dof; gDof++)
                    {
                        for (int hDof = 0; hDof < dof; hDof++)
                        {
                            q[iNode, iDof] +=
                                detJWeight * s[gDof, hDof] * b[iNode, iDof, gDof, hDof];
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
                for (int col = 0; col < elemNodeCnt; col++)
                {
                    int colNodeId = nodes[col];
                    if (colNodeId == -1)
                    {
                        continue;
                    }

                    int colCoId = coIds[col];
                    double[] u = FV.GetDoubleValue(colCoId, FieldDerivationType.Value);
                    double[] vel = FV.GetDoubleValue(colCoId, FieldDerivationType.Velocity);
                    double[] acc = FV.GetDoubleValue(colCoId, FieldDerivationType.Acceleration);

                    double[,] k = new double[dof, dof];
                    double[,] m = new double[dof, dof];
                    for (int rowDof = 0; rowDof < dof; rowDof++)
                    {
                        for (int colDof = 0; colDof < dof; colDof++)
                        {
                            {
                                double tmp1 = 0.0;
                                double tmp2 = 0.0;
                                for (int gDof = 0; gDof < dof; gDof++)
                                {
                                    tmp1 += b[row, rowDof, gDof, gDof];
                                    tmp2 += b[col, colDof, gDof, gDof];
                                }
                                k[rowDof, colDof] += detJWeight * lambda * tmp1 * tmp2;
                            }
                            {
                                double tmp = 0.0;
                                for (int gDof = 0; gDof < dof; gDof++)
                                {
                                    for (int hDof = 0; hDof < dof; hDof++)
                                    {
                                        tmp +=
                                            b[row, rowDof, gDof, hDof] * b[col, colDof, gDof, hDof] +
                                            b[row, rowDof, gDof, hDof] * b[col, colDof, hDof, gDof];
                                    }
                                }
                                k[rowDof, colDof] += detJWeight * mu * tmp;
                            }
                        }
                    }

                    {
                        double tmp = 0.0;
                        for (int gDof = 0; gDof < dof; gDof++)
                        {
                            for (int hDof = 0; hDof < dof; hDof++)
                            {
                                tmp += s[gDof, hDof] * Nu[hDof][row] * Nu[gDof][col];
                            }
                        }
                        for (int rowDof = 0; rowDof < dof; rowDof++)
                        {
                            k[rowDof, rowDof] += detJWeight * tmp;
                        }
                    }

                    m[0, 0] = rho * sNN[row, col];
                    m[1, 0] = 0.0;
                    m[0, 1] = 0.0;
                    m[1, 1] = rho * sNN[row, col];

                    for (int rowDof = 0; rowDof < dof; rowDof++)
                    {
                        for (int colDof = 0; colDof < dof; colDof++)
                        {
                            A[rowNodeId * dof + rowDof, colNodeId * dof + colDof] +=
                                (1.0 / (beta * dt * dt)) * m[rowDof, colDof] +
                                k[rowDof, colDof];
                            B[rowNodeId * dof + rowDof] +=
                                k[rowDof, colDof] * U[colNodeId * dof + colDof] +
                                m[rowDof, colDof] * (
                                (1.0 / (beta * dt * dt)) * u[colDof] +
                                (1.0 / (beta * dt)) * vel[colDof] +
                                (1.0 / (2.0 * beta) - 1.0) * acc[colDof]);
                        }
                    }
                }
            }

            double[,] fg = new double[elemNodeCnt, dof];
            for (int iNode = 0; iNode < elemNodeCnt; iNode++)
            {
                for (int iDof = 0; iDof < dof; iDof++)
                {
                    fg[iNode, iDof] += rho * g[iDof] * sN[iNode];
                }
            }

            for (int row = 0; row < elemNodeCnt; row++)
            {
                int rowNodeId = nodes[row];
                if (rowNodeId == -1)
                {
                    continue;
                }
                for (int rowDof = 0; rowDof < dof; rowDof++)
                {
                    B[rowNodeId * dof + rowDof] += fg[row, rowDof] - q[row, rowDof];
                }
            }
        }
    }
}
