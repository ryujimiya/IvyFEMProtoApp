using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    abstract partial class Elastic2DBaseFEM
    {
        protected void CalcTwoBodyContactAB(IvyFEM.Linear.DoubleSparseMatrix A, double[] B)
        {
            for (uint quantityId = 0; quantityId < World.GetQuantityCount(); quantityId++)
            {
                int slaveCnt = World.GetContactSlaveEIds(quantityId).Count;
                int masterCnt = World.GetContactMasterEIds(quantityId).Count;
                if (slaveCnt > 0 && masterCnt > 0)
                {
                    CalcTwoBodyContactQuantityAB(quantityId, A, B);
                }
            }
        }

        private void CalcTwoBodyContactQuantityAB(
            uint cQuantityId, IvyFEM.Linear.DoubleSparseMatrix A, double[] B)
        {
            uint uQuantityId = 0;
            System.Diagnostics.Debug.Assert(World.GetCoordCount(uQuantityId) ==
                World.GetCoordCount(cQuantityId));
            int coCnt = (int)World.GetCoordCount(cQuantityId);
            int uNodeCnt = NodeCounts[uQuantityId];
            int cNodeCnt = NodeCounts[cQuantityId];
            int uDof = Dofs[uQuantityId];
            int cDof = Dofs[cQuantityId];
            System.Diagnostics.Debug.Assert(cDof == 1);
            int offset = GetOffset(cQuantityId);

            // 線要素の変位を更新
            UpdateLineFEDisplacements(uQuantityId, uDof, cQuantityId);

            // 節点法線ベクトルの計算
            Dictionary<int, double[]> co2Normal = GetSlaveLineFECo2Normal(uQuantityId, uDof, cQuantityId);

            bool[] lConstraintNodeIds = new bool[cNodeCnt];
            IList<uint> slaveFEIds = World.GetContactSlaveLineFEIds(cQuantityId);
            foreach (uint slaveFEId in slaveFEIds)
            {
                LineFE lineFE = World.GetLineFE(uQuantityId, slaveFEId);
                uint elemNodeCnt = lineFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = lineFE.NodeCoordIds[iNode];
                    int nodeId = World.Coord2Node(uQuantityId, coId);
                    nodes[iNode] = nodeId;
                }

                LineFE lLineFE = World.GetLineFE(cQuantityId, slaveFEId);
                int[] lNodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = lLineFE.NodeCoordIds[iNode];
                    int lNodeId = World.Coord2Node(cQuantityId, coId);
                    lNodes[iNode] = lNodeId;
                }
                //IntegrationPoints ip = lineFE.GetIntegrationPoints(LineIntegrationPointCount.Point3);
                //System.Diagnostics.Debug.Assert(ip.Ls.Length == 3);
                IntegrationPoints ip = lineFE.GetIntegrationPoints(LineIntegrationPointCount.Point5);
                System.Diagnostics.Debug.Assert(ip.Ls.Length == 5);

                for (int ipPt = 0; ipPt < ip.PointCount; ipPt++)
                {
                    double[] L = ip.Ls[ipPt];
                    double[] N = lineFE.CalcN(L);
                    double[][] Nu = lineFE.CalcNu(L);
                    double[] lN = lLineFE.CalcN(L);
                    double lineLen = lineFE.GetLineLength();
                    //double[] normal = lineFE.GetNormal();
                    double weight = ip.Weights[ipPt];
                    double detJWeight = (lineLen / 2.0) * weight;

                    // 現在の位置
                    double[] curCoord = new double[uDof];
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        int coId = lineFE.NodeCoordIds[iNode];
                        double[] coord = World.GetCoord(uQuantityId, coId);
                        int iNodeId = nodes[iNode];
                        if (iNodeId == -1)
                        {
                            for (int iDof = 0; iDof < uDof; iDof++)
                            {
                                curCoord[iDof] += coord[iDof] * N[iNode];
                            }
                        }
                        else
                        {
                            for (int iDof = 0; iDof < uDof; iDof++)
                            {
                                curCoord[iDof] += (coord[iDof] + U[iNodeId * uDof + iDof]) * N[iNode];
                            }
                        }
                    }

                    // 連続な近似法線ベクトルを計算する
                    double[] normal = new double[uDof];
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        int coId = lineFE.NodeCoordIds[iNode];
                        double[] nodeNormal = co2Normal[coId];
                        for (int iDof = 0; iDof < uDof; iDof++)
                        {
                            normal[iDof] += nodeNormal[iDof] * N[iNode];
                        }
                    }
                    normal = IvyFEM.Lapack.Utils.NormalizeDoubleVector(normal);

                    // 対応するMasterの点を取得する
                    uint masterFEId;
                    double[] masterN;
                    double[][] masterNu;
                    GetMasterLineFEPoint(
                        curCoord, normal,
                        uQuantityId, uDof, cQuantityId,
                        out masterFEId, out masterN, out masterNu);
                    if (masterFEId == 0)
                    {
                        // 対応するMasterの点がない
                        continue;
                    }
                    LineFE masterLineFE = World.GetLineFE(cQuantityId, masterFEId);
                    System.Diagnostics.Debug.Assert(masterLineFE.NodeCount == elemNodeCnt);
                    int[] masterNodes = new int[elemNodeCnt];
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        int coId = masterLineFE.NodeCoordIds[iNode];
                        int nodeId = World.Coord2Node(uQuantityId, coId);
                        masterNodes[iNode] = nodeId;
                    }
                    // 現在の位置
                    double[] masterCurCoord = new double[uDof];
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        int coId = masterLineFE.NodeCoordIds[iNode];
                        double[] coord = World.GetCoord(uQuantityId, coId);
                        int iNodeId = masterNodes[iNode];
                        if (iNodeId == -1)
                        {
                            for (int iDof = 0; iDof < uDof; iDof++)
                            {
                                masterCurCoord[iDof] += coord[iDof] * masterN[iNode];
                            }
                        }
                        else
                        {
                            for (int iDof = 0; iDof < uDof; iDof++)
                            {
                                masterCurCoord[iDof] += (coord[iDof] + U[iNodeId * uDof + iDof]) * masterN[iNode];
                            }
                        }
                    }

                    // ギャップの計算
                    double gap = 0;
                    for (int iDof = 0; iDof < uDof; iDof++)
                    {
                        gap += -normal[iDof] * (curCoord[iDof] - masterCurCoord[iDof]);
                    }

                    // ラグランジュの未定乗数
                    double l = 0;
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        int iNodeId = lNodes[iNode];
                        if (iNodeId == -1)
                        {
                            continue;
                        }
                        l += U[offset + iNodeId] * lN[iNode];
                    }

                    // Karush-Kuhn-Tucker条件
                    double tolerance = IvyFEM.Linear.Constants.ConvRatioTolerance;
                    if (l <= tolerance && gap >= -tolerance)
                    {
                        // 拘束しない
                        continue;
                    }

                    ////////////////////////////////////////
                    // これ以降、条件を付加する処理
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        int iNodeId = lNodes[iNode];
                        if (iNodeId == -1)
                        {
                            continue;
                        }
                        lConstraintNodeIds[iNodeId] = true;
                    }

                    // Slave
                    for (int row = 0; row < elemNodeCnt; row++)
                    {
                        int rowNodeId = nodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int col = 0; col < elemNodeCnt; col++)
                        {
                            int colNodeId = lNodes[col];
                            if (colNodeId == -1)
                            {
                                continue;
                            }

                            double[,] kul = new double[uDof, cDof];
                            double[,] klu = new double[cDof, uDof];
                            for (int rowDof = 0; rowDof < uDof; rowDof++)
                            {
                                kul[rowDof, 0] +=
                                    detJWeight * normal[rowDof] * N[row] * lN[col];
                                klu[0, rowDof] +=
                                    detJWeight * normal[rowDof] * N[row] * lN[col];
                            }

                            for (int rowDof = 0; rowDof < uDof; rowDof++)
                            {
                                A[rowNodeId * uDof + rowDof, offset + colNodeId] += kul[rowDof, 0];
                                A[offset + colNodeId, rowNodeId * uDof + rowDof] += klu[0, rowDof];
                                B[rowNodeId * uDof + rowDof] +=
                                    kul[rowDof, 0] * U[offset + colNodeId];
                                B[offset + colNodeId] +=
                                    klu[0, rowDof] * U[rowNodeId * uDof + rowDof];
                            }
                        }
                    }

                    // Master
                    for (int row = 0; row < elemNodeCnt; row++)
                    {
                        int rowNodeId = masterNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int col = 0; col < elemNodeCnt; col++)
                        {
                            int colNodeId = lNodes[col];
                            if (colNodeId == -1)
                            {
                                continue;
                            }

                            double[,] kul = new double[uDof, cDof];
                            double[,] klu = new double[cDof, uDof];
                            for (int rowDof = 0; rowDof < uDof; rowDof++)
                            {
                                kul[rowDof, 0] +=
                                    -detJWeight * normal[rowDof] * masterN[row] * lN[col];
                                klu[0, rowDof] +=
                                    -detJWeight * normal[rowDof] * masterN[row] * lN[col];
                            }

                            for (int rowDof = 0; rowDof < uDof; rowDof++)
                            {
                                A[rowNodeId * uDof + rowDof, offset + colNodeId] += kul[rowDof, 0];
                                A[offset + colNodeId, rowNodeId * uDof + rowDof] += klu[0, rowDof];
                                B[rowNodeId * uDof + rowDof] +=
                                    kul[rowDof, 0] * U[offset + colNodeId];
                                B[offset + colNodeId] +=
                                    klu[0, rowDof] * U[rowNodeId * uDof + rowDof];
                            }
                        }
                    }

                    // Slave
                    double[,] qu = new double[elemNodeCnt, uDof];
                    double[] ql = new double[elemNodeCnt];
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        for (int iDof = 0; iDof < uDof; iDof++)
                        {
                            qu[iNode, iDof] += detJWeight * l * normal[iDof] * N[iNode];
                        }
                    }
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        for (int jDof = 0; jDof < uDof; jDof++)
                        {
                            ql[iNode] += detJWeight * lN[iNode] * normal[jDof] * curCoord[jDof];
                        }
                    }

                    for (int row = 0; row < elemNodeCnt; row++)
                    {
                        int rowNodeId = nodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int rowDof = 0; rowDof < uDof; rowDof++)
                        {
                            B[rowNodeId * uDof + rowDof] += -qu[row, rowDof];
                        }
                    }
                    for (int row = 0; row < elemNodeCnt; row++)
                    {
                        int rowNodeId = lNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        B[offset + rowNodeId] += -ql[row];
                    }

                    // Master
                    double[,] masterQu = new double[elemNodeCnt, uDof];
                    double[] masterQl = new double[elemNodeCnt];
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        for (int iDof = 0; iDof < uDof; iDof++)
                        {
                            masterQu[iNode, iDof] += -detJWeight * l * normal[iDof] * masterN[iNode];
                        }
                    }
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        for (int jDof = 0; jDof < uDof; jDof++)
                        {
                            masterQl[iNode] += -detJWeight * lN[iNode] * normal[jDof] * masterCurCoord[jDof];
                        }
                    }

                    for (int row = 0; row < elemNodeCnt; row++)
                    {
                        int rowNodeId = masterNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        for (int rowDof = 0; rowDof < uDof; rowDof++)
                        {
                            B[rowNodeId * uDof + rowDof] += -masterQu[row, rowDof];
                        }
                    }
                    for (int row = 0; row < elemNodeCnt; row++)
                    {
                        int rowNodeId = lNodes[row];
                        if (rowNodeId == -1)
                        {
                            continue;
                        }
                        B[offset + rowNodeId] += -masterQl[row];
                    }
                }
            }

            // 条件をセットしなかった節点
            for (int iNodeId = 0; iNodeId < cNodeCnt; iNodeId++)
            {
                if (lConstraintNodeIds[iNodeId])
                {
                    continue;
                }
                A[offset + iNodeId, offset + iNodeId] = 1.0;
                B[offset + iNodeId] = 0;
            }
        }

        private void UpdateLineFEDisplacements(uint uQuantityId, int uDof, uint cQuantityId)
        {
            IList<uint> slaveFEIds = World.GetContactSlaveLineFEIds(cQuantityId);
            IList<uint> masterFEIds = World.GetContactMasterLineFEIds(cQuantityId);
            IList<uint>[] feIdss = { slaveFEIds, masterFEIds };

            foreach (IList<uint> feIds in feIdss)
            {
                foreach (uint feId in feIds)
                {
                    LineFE lineFE = World.GetLineFE(uQuantityId, feId);
                    uint elemNodeCnt = lineFE.NodeCount;
                    int[] nodes = new int[elemNodeCnt];
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        int coId = lineFE.NodeCoordIds[iNode];
                        int nodeId = World.Coord2Node(uQuantityId, coId);
                        nodes[iNode] = nodeId;
                    }
                    double[][] displacements = new double[elemNodeCnt][];
                    for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                    {
                        double[] u = new double[uDof];
                        int nodeId = nodes[iNode];
                        if (nodeId == -1)
                        {
                            for (int iDof = 0; iDof < uDof; iDof++)
                            {
                                u[iDof] = 0;
                            }
                        }
                        else
                        {
                            for (int iDof = 0; iDof < uDof; iDof++)
                            {
                                u[iDof] = U[nodeId * uDof + iDof];
                            }
                        }
                        displacements[iNode] = u;
                    }
                    lineFE.SetDisplacements(displacements);
                }
            }
        }

        private Dictionary<int, double[]> GetSlaveLineFECo2Normal(uint uQuantityId, int uDof, uint cQuantityId)
        {
            Dictionary<int, double[]> co2Normal = new Dictionary<int, double[]>();
            IList<uint> slaveFEIds = World.GetContactSlaveLineFEIds(cQuantityId);
            Dictionary<int, IList<double[]>> co2NormalList = new Dictionary<int, IList<double[]>>();
            foreach (uint slaveFEId in slaveFEIds)
            {
                LineFE lineFE = World.GetLineFE(uQuantityId, slaveFEId);
                uint elemNodeCnt = lineFE.NodeCount;
                double lineLen = lineFE.GetLineLength();
                double[] normal = lineFE.GetNormal();
                // 法線ベクトルに重みを付ける
                int dim = normal.Length;
                for (int iDim = 0; iDim < dim; iDim++)
                {
                    normal[iDim] /= lineLen; // Yangの方法
                }

                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = lineFE.NodeCoordIds[iNode];
                    if (!co2NormalList.ContainsKey(coId))
                    {
                        co2NormalList[coId] = new List<double[]>();
                    }
                    co2NormalList[coId].Add(normal);
                }
            }
            foreach (var pair in co2NormalList)
            {
                int coId = pair.Key;
                IList<double[]> normalList = pair.Value;
                OpenTK.Vector2d av = new OpenTK.Vector2d();
                foreach (double[] normal in normalList)
                {
                    av.X += normal[0];
                    av.Y += normal[1];
                }
                av = CadUtils.Normalize(av);
                co2Normal[coId] = new double[] { av.X, av.Y };
            }
            return co2Normal;
        }

        private void GetMasterLineFEPoint(
            double[] curCoord, double[] normal,
            uint uQuantityId, int uDof, uint cQuantityId,
            out uint masterFEId, out double[] masterN, out double[][] masterNu)
        {
            masterFEId = 0;
            masterN = null;
            masterNu = null;
            IList<uint> masterFEIds = World.GetContactMasterLineFEIds(cQuantityId);
            OpenTK.Vector2d slaveX = new OpenTK.Vector2d(curCoord[0], curCoord[1]);
            // t = e3 x n
            OpenTK.Vector2d t = new OpenTK.Vector2d(-normal[1], normal[0]);

            foreach (uint feId in masterFEIds)
            {
                LineFE lineFE = World.GetLineFE(uQuantityId, feId);
                uint elemNodeCnt = lineFE.NodeCount;
                int[] nodes = new int[elemNodeCnt];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    int coId = lineFE.NodeCoordIds[iNode];
                    int nodeId = World.Coord2Node(uQuantityId, coId);
                    nodes[iNode] = nodeId;
                }

                // 現在の頂点の位置
                double[][] masterCurNodeCoords = new double[elemNodeCnt][];
                for (int iNode = 0; iNode < elemNodeCnt; iNode++)
                {
                    masterCurNodeCoords[iNode] = new double[uDof];
                    int coId = lineFE.NodeCoordIds[iNode];
                    double[] coord = World.GetCoord(uQuantityId, coId);
                    int iNodeId = nodes[iNode];
                    if (iNodeId == -1)
                    {
                        for (int iDof = 0; iDof < uDof; iDof++)
                        {
                            masterCurNodeCoords[iNode][iDof] = coord[iDof];
                        }
                    }
                    else
                    {
                        for (int iDof = 0; iDof < uDof; iDof++)
                        {
                            masterCurNodeCoords[iNode][iDof] = coord[iDof] + U[iNodeId * uDof + iDof];
                        }
                    }
                }
                OpenTK.Vector2d masterX1 = new OpenTK.Vector2d(masterCurNodeCoords[0][0], masterCurNodeCoords[0][1]);
                OpenTK.Vector2d masterX2 = new OpenTK.Vector2d(masterCurNodeCoords[1][0], masterCurNodeCoords[1][1]);
                var v1 = masterX2 - masterX1;
                var v2 = slaveX - masterX1;
                double L2 = OpenTK.Vector2d.Dot(v2, t) / OpenTK.Vector2d.Dot(v1, t);
                if (L2 >= 0.0 && L2 <= 1.0)
                {
                    // 対象となる要素
                    double[] L = { 1.0 - L2, L2 };
                    masterFEId = feId;
                    masterN = lineFE.CalcN(L);
                    masterNu = lineFE.CalcNu(L);
                    break;
                }
            }
        }
    }
}
