using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void PCWaveguideTriangleLatticeProblem2_0(MainWindow mainWindow)
        {
            double waveguideWidth = 1.0;

            // フォトニック導波路
            // 考慮する波数ベクトルの最小値
            double minWaveNum = 0.0;
            // 考慮する波数ベクトルの最大値
            double maxWaveNum = 0.5;

            // 空孔？
            //bool isAirHole = false; // 誘電体ロッド
            //bool isAirHole = true; //エアホール
            bool isAirHole = true;
            //bool isTMMode = true;  // TMモード
            //bool isTMMode = false;  // TEモード
            bool isTMMode = true;
            // ロッドのカラムをシフトする？
            bool isColumnShift180 = false;
            // ロッドの数(半分)
            //const int rodCntHalf = 4;
            const int rodCntHalf = 4;
            // 欠陥ロッド数
            const int defectRodCnt = 1;
            // 三角形格子の内角
            double latticeTheta = 30.0;
            // ロッドの半径
            double rodRadiusRatio = 0.30;
            // ロッドの比誘電率
            double rodEp = 3.40 * 3.40;
            // 1格子当たりの分割点の数
            //const int divCntForOneLattice = 9;
            //const int divCntForOneLattice = 6;
            const int divCntForOneLattice = 9;
            // ロッド円周の分割数
            const int rodCircleDiv = 12;
            // ロッドの半径の分割数
            const int rodRadiusDiv = 4;

            // 格子の数
            int latticeCnt = rodCntHalf * 2 + defectRodCnt;
            // ロッドから上下の電気壁までの距離
            double electricWallDistance = 0.1 * waveguideWidth / (rodCntHalf * 2 + defectRodCnt);
            // ロッド間の距離(Y方向)
            double rodDistanceY = 
                (waveguideWidth - 2.0 * electricWallDistance) /
                (latticeCnt - 1 + 2.0 * rodRadiusRatio / Math.Sin(latticeTheta * Math.PI / 180.0));
            // 格子定数
            double latticeA = rodDistanceY / Math.Sin(latticeTheta * Math.PI / 180.0);
            // ロッド間の距離(X方向)
            double rodDistanceX = rodDistanceY * 2.0 / Math.Tan(latticeTheta * Math.PI / 180.0);
            // 周期構造距離
            double periodicDistance = rodDistanceX;
            // ロッドの半径
            double rodRadius = rodRadiusRatio * latticeA;
            // ロッド中心と電気壁間の距離
            double electricWallDelta = electricWallDistance + rodRadius;
            // メッシュのサイズ
            double eLen = 1.05 * waveguideWidth / (latticeCnt * divCntForOneLattice);

            // フォトニック結晶導波路の場合、a/λを規格化周波数とする
            double sFreq = 0.230;
            double eFreq = 0.256;
            int freqDiv = 20;

            // 最小屈折率
            double minEffN = 0.0;
            // 最大屈折率
            double maxEffN = 1.0;
            if (isAirHole)
            {
                // air hole
                minEffN = 0.0;
                maxEffN = Math.Sqrt(rodEp);
            }

            const uint portCnt = 2;
            // 導波路不連続領域の長さ
            //const int disconRodCnt = 2;
            const int disconRodCnt = 2;
            // 導波路不連続領域の長さ
            double disconLength = rodDistanceX * disconRodCnt;
            // 入出力導波路の周期構造部分の長さ
            double inputWgLength = rodDistanceX;
            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> inputWgRodLoopIds1 = new List<uint>();
            IList<uint> inputWgRodLoopIds2 = new List<uint>();
            IList<uint> rodEIdsB1 = new List<uint>();
            IList<uint> rodEIdsB2 = new List<uint>();
            IList<uint> rodEIdsB3 = new List<uint>();
            IList<uint> rodEIdsB4 = new List<uint>();
            IList<uint> rodVIdsB1 = new List<uint>();
            IList<uint> rodVIdsB2 = new List<uint>();
            IList<uint> rodVIdsB3 = new List<uint>();
            IList<uint> rodVIdsB4 = new List<uint>();

            // Cad
            Cad2D cad = new Cad2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                //------------------------------------------------------------------
                // 図面作成
                //------------------------------------------------------------------
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    // 領域追加
                    pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength + disconLength, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength * 2 + disconLength, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength * 2 + disconLength, waveguideWidth));
                    pts.Add(new OpenTK.Vector2d(inputWgLength + disconLength, waveguideWidth));
                    pts.Add(new OpenTK.Vector2d(inputWgLength, waveguideWidth));
                    uint lId1 = cad.AddPolygon(pts).AddLId;
                    // 入出力領域を分離
                    uint eIdAdd1 = cad.ConnectVertexLine(3, 8).AddEId;
                    uint eIdAdd2 = cad.ConnectVertexLine(4, 7).AddEId;
                }

                //////////////////////////////////////////
                // ロッドの中心Y座標(偶数/奇数カラム)
                IList<double> rodY0sX1 = new List<double>();
                IList<double> rodY0sX2 = new List<double>();
                for (int i = 0; i < latticeCnt; i++)
                {
                    if (i >= rodCntHalf && i < (rodCntHalf + defectRodCnt))
                    {
                        // 欠陥部
                        continue;
                    }
                    double topRodPosY = waveguideWidth - electricWallDistance - rodRadius;
                    if (i % 2 == 0)
                    {
                        double y0 = topRodPosY - i * rodDistanceY;
                        if ((y0 + rodRadius) > waveguideWidth || (y0 - rodRadius) < 0.0)
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        var workrodY0s = isColumnShift180 ? rodY0sX1 : rodY0sX2;
                        workrodY0s.Add(y0);
                    }
                    else
                    {
                        double y0 = topRodPosY - rodDistanceY - (i - 1) * rodDistanceY;
                        if ((y0 + rodRadius) > waveguideWidth || (y0 - rodRadius) < 0.0)
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        var workrodY0s = isColumnShift180 ? rodY0sX2 : rodY0sX1;
                        workrodY0s.Add(y0);
                    }
                }

                ///////////////////////////////////////////
                // 境界上のロッドの頂点を追加
                // 入力導波路 外側境界
                // 入力導波路 内部側境界
                // 出力導波路 外側境界
                // 出力導波路 内部側境界
                uint[] eIdsB = { 1, 9, 5, 10 };
                IList<uint>[] rodVIdsB = { rodVIdsB1, rodVIdsB2, rodVIdsB3, rodVIdsB4 };
                IList<uint>[] rodEIdsB = { rodEIdsB1, rodEIdsB2, rodEIdsB3, rodEIdsB4 };
                double[] ptXsB = {
                    0.0, inputWgLength,
                    inputWgLength * 2 + disconLength,  inputWgLength + disconLength
                };
                for (int boundaryIndex = 0; boundaryIndex < eIdsB.Length; boundaryIndex++)
                {
                    uint eId = eIdsB[boundaryIndex];
                    double ptXB = ptXsB[boundaryIndex];
                    IList<uint> workrodEIdsB = rodEIdsB[boundaryIndex];
                    IList<uint> workrodVIdsB = rodVIdsB[boundaryIndex];
                    int rodCntX1 = rodY0sX1.Count;
                    for (int i = 0; i < rodCntX1; i++)
                    {
                        double ptY0 = 0.0;
                        double ptY1 = 0.0;
                        double ptY2 = 0.0;
                        if (boundaryIndex == 0)
                        {
                            // 入力導波路 外側境界
                            // 下から順に追加
                            ptY0 = rodY0sX1[rodCntX1 - 1 - i];
                            ptY1 = ptY0 - rodRadius;
                            ptY2 = ptY0 + rodRadius;
                        }
                        else
                        {
                            // 上から順に追加
                            ptY0 = rodY0sX1[i];
                            ptY1 = ptY0 + rodRadius;
                            ptY2 = ptY0 - rodRadius;
                        }

                        double[] ptYs = { ptY1, ptY0, ptY2 };
                        for (int iY = 0; iY < ptYs.Length; iY++)
                        {
                            var res = cad.AddVertex(
                                CadElementType.Edge, eId, new OpenTK.Vector2d(ptXB, ptYs[iY]));
                            uint addVId = res.AddVId;
                            uint addEId = res.AddEId;
                            System.Diagnostics.Debug.Assert(addVId != 0);
                            System.Diagnostics.Debug.Assert(addEId != 0);
                            workrodVIdsB.Add(addVId);
                            if (iY != 0) // iY == 0のときはロッドの外の辺が生成される
                            {
                                workrodEIdsB.Add(addEId);
                            }
                        }
                    }
                    System.Diagnostics.Debug.Assert(workrodVIdsB.Count % 3 == 0);
                    System.Diagnostics.Debug.Assert(workrodEIdsB.Count % 2 == 0);
                }

                /////////////////////////////////////////////////////////////////////////////
                // 左のロッドを追加
                uint[] leftRodContainsLIds = { 1, 2, 3 };
                IList<uint>[] leftRodVIdsB = { rodVIdsB[0], rodVIdsB[1], rodVIdsB[3] };
                IList<uint>[] leftRodContainsInputWgRodLoopIdss = { inputWgRodLoopIds1, null, inputWgRodLoopIds2 };
                for (int index = 0; index < leftRodContainsLIds.Length; index++)
                {
                    IList<uint> workrodVIdsB = leftRodVIdsB[index];
                    uint baseLoopId = leftRodContainsLIds[index];
                    IList<uint> workInputWgRodLoopIds = leftRodContainsInputWgRodLoopIdss[index];

                    // 始点、終点が逆？
                    bool isReverse = false;
                    if (index == 0)
                    {
                        // 入力境界 外側
                        isReverse = false;
                    }
                    else
                    {
                        isReverse = true;
                    }

                    int rodCnt = workrodVIdsB.Count / 3;

                    for (int i = 0; i < rodCnt; i++)
                    {
                        uint vId0 = workrodVIdsB[i * 3];
                        uint vId1 = workrodVIdsB[i * 3 + 1]; // 中心はこれ
                        uint vId2 = workrodVIdsB[i * 3 + 2];
                        // 中心
                        OpenTK.Vector2d cPt = cad.GetVertexCoord(vId1);
                        double x0 = cPt.X;
                        double y0 = cPt.Y;

                        uint workVId0 = 0;
                        uint workVId2 = 0;
                        if (isReverse)
                        {
                            workVId0 = vId2;
                            workVId2 = vId0;
                        }
                        else
                        {
                            workVId0 = vId0;
                            workVId2 = vId2;
                        }
                        // 左のロッド
                        uint lId = PCWaveguideUtils.AddLeftRod(
                            cad,
                            baseLoopId,
                            workVId0,
                            vId1,
                            workVId2,
                            x0,
                            y0,
                            rodRadius,
                            rodCircleDiv,
                            rodRadiusDiv);

                        rodLoopIds.Add(lId);

                        if (workInputWgRodLoopIds != null)
                        {
                            workInputWgRodLoopIds.Add(lId);
                        }
                    }
                }

                /////////////////////////////////////////////////////////////////////////////
                // 右のロッドを追加
                uint[] rightRodContainsLIds = { 1, 3, 2 };
                IList<uint>[] rightRodVIdsB = { rodVIdsB[1], rodVIdsB[2], rodVIdsB[3] };
                IList<uint>[] rightRodContainsInputWgRodLoopIdss = { inputWgRodLoopIds1, inputWgRodLoopIds2, null };
                for (int index = 0; index < rightRodContainsLIds.Length; index++)
                {
                    IList<uint> workrodVIdsB = rightRodVIdsB[index];
                    uint baseLoopId = rightRodContainsLIds[index];
                    IList<uint> workInputWgRodLoopIds = rightRodContainsInputWgRodLoopIdss[index];

                    // 始点、終点が逆？
                    bool isReverse = false;

                    int rodCnt = workrodVIdsB.Count / 3;

                    for (int i = 0; i < rodCnt; i++)
                    {
                        uint vId0 = workrodVIdsB[i * 3];
                        uint vId1 = workrodVIdsB[i * 3 + 1]; // 中心はこれ
                        uint vId2 = workrodVIdsB[i * 3 + 2];
                        // 中心
                        OpenTK.Vector2d cPt = cad.GetVertexCoord(vId1);
                        double x0 = cPt.X;
                        double y0 = cPt.Y;

                        uint workVId0 = 0;
                        uint workVId2 = 0;
                        if (isReverse)
                        {
                            workVId0 = vId2;
                            workVId2 = vId0;
                        }
                        else
                        {
                            workVId0 = vId0;
                            workVId2 = vId2;
                        }
                        // 右のロッド
                        uint lId = PCWaveguideUtils.AddRightRod(
                            cad,
                            baseLoopId,
                            workVId0,
                            vId1,
                            workVId2,
                            x0,
                            y0,
                            rodRadius,
                            rodCircleDiv,
                            rodRadiusDiv);

                        rodLoopIds.Add(lId);

                        if (workInputWgRodLoopIds != null)
                        {
                            workInputWgRodLoopIds.Add(lId);
                        }
                    }
                }

                /////////////////////////////////////////////////////////////////////////
                // 領域のロッド
                int periodicCntInputWg1 = 1;
                int periodicCntInputWg2 = 1;
                int periodicCntX = periodicCntInputWg1 + disconRodCnt + periodicCntInputWg2;

                for (int iX = 0; iX < periodicCntX; iX++)
                {
                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    if (iX >= 0 && iX < periodicCntInputWg1)
                    {
                        baseLoopId = 1;
                        inputWgNo = 1;
                    }
                    else if (iX >= periodicCntInputWg1 &&
                        iX < (periodicCntInputWg1 + disconRodCnt))
                    {
                        baseLoopId = 2;
                        inputWgNo = 0;
                    }
                    else if (iX >= (periodicCntInputWg1 + disconRodCnt) &&
                        iX < (periodicCntInputWg1 + disconRodCnt + periodicCntInputWg2))
                    {
                        baseLoopId = 3;
                        inputWgNo = 2;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    for (int iSubX = 0; iSubX < 2; iSubX++)
                    {
                        IList<double> workRodY0s = null;
                        double x0 = 0.0;
                        if (iSubX % 2 == 0)
                        {
                            if (iX == 0 || iX == periodicCntInputWg1)
                            {
                                // 追加済み
                                continue;
                            }
                            if (iX == (periodicCntX - 1))
                            {
                                // 追加済み
                                continue;
                            }
                            workRodY0s = rodY0sX1;
                            x0 = iX * periodicDistance;
                        }
                        else
                        {
                            workRodY0s = rodY0sX2;
                            x0 = iX * periodicDistance + 0.5 * periodicDistance;
                        }
                        int rodCnt = workRodY0s.Count;
                        for (int i = 0; i < rodCnt; i++)
                        {
                            double y0 = workRodY0s[i];
                            uint lId = PCWaveguideUtils.AddRod(
                                cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                            rodLoopIds.Add(lId);
                            if (inputWgNo == 1)
                            {
                                inputWgRodLoopIds1.Add(lId);
                            }
                            else if (inputWgNo == 2)
                            {
                                inputWgRodLoopIds2.Add(lId);
                            }
                            else
                            {
                                // なにもしない
                            }
                        }
                    }
                }
            }

            // check
            {
                foreach (uint lId in rodLoopIds)
                {
                    cad.SetLoopColor(lId, new double[] { 1.0, 0.6, 0.6 });
                }
                IList<uint>[] rodEIdss = { rodEIdsB1, rodEIdsB2, rodEIdsB3, rodEIdsB4 };
                foreach (var rodEIds in rodEIdss)
                {
                    foreach (uint eId in rodEIds)
                    {
                        cad.SetEdgeColor(eId, new double[] { 1.0, 0.4, 0.4 });
                    }
                }
            }
            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad2DDrawer(cad);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            Mesher2D mesher = new Mesher2D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            var meshDrawer = new Mesher2DDrawer(mesher);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            {
                uint dof = 1; // 複素数
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint claddingMaId = 0;
            uint coreMaId = 0;
            {
                world.ClearMaterial();

                // 媒質リスト作成
                double claddingMu = 1.0;
                double claddingEp = 1.0;
                double coreMu = 1.0;
                double coreEp = 1.0;
                if (isAirHole)
                {
                    // 誘電体基盤 + 空孔(air hole)
                    claddingMu = 1.0;
                    claddingEp = rodEp;
                    coreMu = 1.0;
                    coreEp = 1.0;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = claddingMu,
                    Muyy = claddingMu,
                    Muzz = claddingMu
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = coreEp,
                    Epyy = coreEp,
                    Epzz = coreEp,
                    Muxx = coreMu,
                    Muyy = coreMu,
                    Muzz = coreMu
                };

                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

                uint[] lIds = new uint[3 + rodLoopIds.Count];
                lIds[0] = 1;
                lIds[1] = 2;
                lIds[2] = 3;
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 3] = rodLoopIds[i];
                }
                uint[] maIds = new uint[3 + rodLoopIds.Count];
                for (int i = 0; i < 3; i++)
                {
                    maIds[i] = claddingMaId;
                }
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    maIds[i + 3] = coreMaId;
                }

                for (int i = 0; i < lIds.Length; i++)
                {
                    uint lId = lIds[i];
                    uint maId = maIds[i];
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            IList<PCWaveguidePortInfo> wgPortInfos = new List<PCWaveguidePortInfo>();
            // ポート情報リスト作成
            bool[] isPortBc2Reverse = { true, false };
            for (int portId = 0; portId < portCnt; portId++)
            {
                var wgPortInfo = new PCWaveguidePortInfo();
                wgPortInfos.Add(wgPortInfo);
                System.Diagnostics.Debug.Assert(wgPortInfos.Count == (portId + 1));
                //wgPortInfo.IsSVEA = true; // 緩慢変化包絡線近似
                wgPortInfo.IsSVEA = false; // Φを直接解く
                wgPortInfo.IsPortBc2Reverse = isPortBc2Reverse[portId];
                wgPortInfo.LatticeA = latticeA;
                wgPortInfo.PeriodicDistanceX = periodicDistance;
                wgPortInfo.MinEffN = minEffN;
                wgPortInfo.MaxEffN = maxEffN;
                wgPortInfo.MinWaveNum = minWaveNum;
                wgPortInfo.MaxWaveNum = maxWaveNum;
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // 周期構造入出力導波路
            for (int portId = 0; portId < portCnt; portId++)
            {
                // ワールド座標系のループIDを取得
                uint[] lIds = null;
                if (portId == 0)
                {
                    
                    lIds = new uint[1 + inputWgRodLoopIds1.Count];
                    lIds[0] = 1;
                    for (int i = 0; i < inputWgRodLoopIds1.Count; i++)
                    {
                        lIds[i + 1] = inputWgRodLoopIds1[i];
                    }
                }
                else if (portId == 1)
                {
                    lIds = new uint[1 + inputWgRodLoopIds2.Count];
                    lIds[0] = 3;
                    for (int i = 0; i < inputWgRodLoopIds2.Count; i++)
                    {
                        lIds[i + 1] = inputWgRodLoopIds2[i];
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.LoopIds = new List<uint>(lIds);
            }
            // 周期構造境界1
            for (int portId = 0; portId < portCnt; portId++)
            {
                int rodCnt = 0;
                int divCnt = 0;
                uint eId0 = 0;
                IList<uint> workrodEIdsB = null;
                if (portId == 0)
                {
                    workrodEIdsB = rodEIdsB1;
                    eId0 = 1;
                }
                else if (portId == 1)
                {
                    workrodEIdsB = rodEIdsB3;
                    eId0 = 5;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                rodCnt = workrodEIdsB.Count / 2;
                divCnt = 3 * rodCnt + 1;

                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                eIds[0] = eId0;

                if (workrodEIdsB.Contains(eIds[0]))
                {
                    maIds[0] = coreMaId;
                }
                else
                {
                    maIds[0] = claddingMaId;
                }

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    if (portId == 0)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) - (i - 1));
                    }
                    else if (portId == 1)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) * 3 - (i - 1));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    if (workrodEIdsB.Contains(eIds[i]))
                    {
                        maIds[i] = coreMaId;
                    }
                    else
                    {
                        maIds[i] = claddingMaId;
                    }
                }

                for (int i = 0; i < eIds.Length; i++)
                {
                    uint eId = eIds[i];
                    uint maId = maIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }

                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.BcEdgeIds1 = new List<uint>(eIds);
            }
            // 周期構造境界2
            for (int portId = 0; portId < portCnt; portId++)
            {
                int rodCnt = 0;
                int divCnt = 0;
                uint eId0 = 0;
                IList<uint> workrodEIdsB = null;
                if (portId == 0)
                {
                    workrodEIdsB = rodEIdsB2;
                    eId0 = 9;
                }
                else if (portId == 1)
                {
                    workrodEIdsB = rodEIdsB4;
                    eId0 = 10;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                rodCnt = workrodEIdsB.Count / 2;
                divCnt = 3 * rodCnt + 1;

                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                eIds[0] = eId0;

                if (workrodEIdsB.Contains(eIds[0]))
                {
                    maIds[0] = coreMaId;
                }
                else
                {
                   maIds[0] = claddingMaId;
                }

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    if (portId == 0)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) * 2 - (i - 1));
                    }
                    else if (portId == 1)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) * 4 - (i - 1));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    if (workrodEIdsB.Contains(eIds[i]))
                    {
                        maIds[i] = coreMaId;
                    }
                    else
                    {
                        maIds[i] = claddingMaId;
                    }
                }

                for (int i = 0; i < eIds.Length; i++)
                {
                    uint eId = eIds[i];
                    uint maId = maIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }

                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.BcEdgeIds2 = new List<uint>(eIds);
            }

            // ポート条件
            world.SetIncidentPortId(quantityId, 0);
            world.SetIncidentModeId(quantityId, 0);
            IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
            {
                for (int portId = 0; portId < portCnt; portId++)
                {
                    PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                    IList<uint> lIds = wgPortInfo.LoopIds;
                    IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                    IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                    PortCondition portCondition = new PortCondition(
                        CadElementType.Edge,
                        lIds, bcEIds1, bcEIds2, FieldValueType.ZScalar, new List<uint> { 0 }, 0);
                    portConditions.Add(portCondition);
                }
            }

            /*
            // 強制境界
            // TMモードの場合磁気壁
            uint[] zeroEIds = {};
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            */

            world.MakeElements();

            // ポートの境界上の節点を準備する
            for (int portId = 0; portId < portCnt; portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.SetupAfterMakeElements(world, quantityId, (uint)portId);
            }
            // フォトニック結晶導波路チャンネル上節点を取得する
            for (int portId = 0; portId < portCnt; portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.PCChannelCoIds = new List<IList<int>>();

                // 全座標IDの取得
                IList<int> coIds = new List<int>();
                IList<uint> lIds = wgPortInfo.LoopIds;
                foreach (uint lId in lIds)
                {
                    IList<int> tmpCoIds = world.GetCoordIdsFromCadId(quantityId, lId, CadElementType.Loop);
                    foreach (int coId in tmpCoIds)
                    {
                        if (coIds.IndexOf(coId) == -1)
                        {
                            coIds.Add(coId);
                        }
                    }
                }
                // チャンネル
                {
                    IList<int> channelCoIds = new List<int>();
                    wgPortInfo.PCChannelCoIds.Add(channelCoIds);

                    int nodeId0 = wgPortInfo.BcNodess[0][0];
                    int coId0 = world.PortNode2Coord(quantityId, (uint)portId, nodeId0);
                    double[] coord0 = world.GetCoord(quantityId, coId0);
                    // 以下の処理はX方向周期構造の場合
                    System.Diagnostics.Debug.Assert(!wgPortInfo.IsYDirectionPeriodic);
                    foreach (int coId in coIds)
                    {
                        // 座標からチャンネル(欠陥部)を判定する
                        double[] coord = world.GetCoord(quantityId, coId);
                        // X方向周期構造
                        double y = Math.Abs(coord[1] - coord0[1]); // Y座標
                        double topRodY = waveguideWidth - electricWallDelta;
                        double defectY1 =
                            topRodY - rodDistanceY * (rodCntHalf - 1) - rodDistanceY * (defectRodCnt + 1) - rodRadius;
                        double defectY2 = topRodY - rodDistanceY * (rodCntHalf - 1) + rodRadius;
                        // air hole
                        if (y >= defectY1 && y <= defectY2)
                        {
                            channelCoIds.Add(coId);
                        }
                    }
                }
            }

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            var chartWin = ChartWindow1;
            chartWin.Owner = mainWindow;
            chartWin.Left = mainWindow.Left + mainWindow.Width;
            chartWin.Top = mainWindow.Top;
            chartWin.Show();
            chartWin.TextBox1.Text = "";
            var model = new PlotModel();
            chartWin.Plot.Model = model;
            model.Title = "PCWaveguide Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "a/λ",
                Minimum = sFreq,
                Maximum = eFreq
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "|S|",
                Minimum = 0.0,
                //Maximum = 1.0
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            var series1 = new LineSeries
            {
                Title = "|S1(1)1|"
            };
            var series2 = new LineSeries
            {
                Title = "|S2(1)1|"
            };
            var series3 = new LineSeries
            {
                Title = "|S1(2)1|"
            };
            var series4 = new LineSeries
            {
                Title = "|S2(2)1|"
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
            model.Series.Add(series3);
            model.Series.Add(series4);
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.ZAbs);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                var faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                var edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            for (int iFreq = 0; iFreq < (freqDiv + 1); iFreq++)
            {
                // a/λ
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波長
                double waveLength = latticeA / normalizedFreq;
                // 周波数
                double freq = Constants.C0 / waveLength;
                System.Diagnostics.Debug.WriteLine("a/λ: " + normalizedFreq);

                var FEM = new PCWaveguide2DFEM(world);
                {
                    var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    solver.IsOrderingToBandMatrix = true;
                    solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.LisEquationSolver();
                    //solver.Method = IvyFEM.Linear.LisEquationSolverMethod.Default;
                    //FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.IvyFEMEquationSolver();
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.COCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    //FEM.Solver = solver;
                }
                FEM.IsTMMode = isTMMode;
                FEM.WgPortInfos = wgPortInfos;
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] Ez = FEM.Ez;
                System.Numerics.Complex[][] S = FEM.S;

                // eigen
                System.Numerics.Complex[][] portBetas = new System.Numerics.Complex[portCnt][];
                System.Numerics.Complex[][][] portEVecs = new System.Numerics.Complex[portCnt][][];
                for (int portId = 0; portId < portCnt; portId++)
                {
                    var eigenFEM = FEM.EigenFEMs[portId];
                    portBetas[portId] = eigenFEM.Betas;
                    portEVecs[portId] = eigenFEM.EVecs;
                }

                // eigen
                int targetModeId = 0;
                int nodeCnt = (int)world.GetNodeCount(quantityId);
                System.Numerics.Complex[] eigenEz = new System.Numerics.Complex[nodeCnt];
                for (int portId = 0; portId < portCnt; portId++)
                {
                    int modeCnt = portBetas[portId].Length;
                    if (targetModeId >= modeCnt)
                    {
                        System.Diagnostics.Debug.WriteLine("No defect mode found at port: " + portId);
                        continue;
                    }
                    System.Numerics.Complex beta = portBetas[portId][targetModeId];
                    System.Numerics.Complex[] eVec = portEVecs[portId][targetModeId];
                    int portNodeCnt = (int)world.GetPortNodeCount(quantityId, (uint)portId);
                    System.Diagnostics.Debug.Assert(portNodeCnt == eVec.Length);
                    for (int portNodeId = 0; portNodeId < portNodeCnt; portNodeId++)
                    {
                        int coId = world.PortNode2Coord(quantityId, (uint)portId, portNodeId);
                        int nodeId = world.Coord2Node(quantityId, coId);
                        System.Numerics.Complex value = eVec[portNodeId];
                        eigenEz[nodeId] = value;
                    }
                }

                if (S[0].Length > 0 && S[1].Length > 0)
                {
                    // Spmq (p:出力ポート, m:出力モード, q:入力ポート)
                    System.Numerics.Complex S111 = S[0][0];
                    System.Numerics.Complex S211 = S[1][0];
                    System.Numerics.Complex S121 = S[0].Length > 1 ? S[0][1] : 0.0;
                    System.Numerics.Complex S221 = S[1].Length > 1 ? S[1][1] : 0.0;
                    double S111Abs = S111.Magnitude;
                    double S211Abs = S211.Magnitude;
                    double S121Abs = S121.Magnitude;
                    double S221Abs = S221.Magnitude;
                    double total =
                        S111Abs * S111Abs + S211Abs * S211Abs +
                        S121Abs * S121Abs + S221Abs * S221Abs;

                    string ret;
                    string CRLF = System.Environment.NewLine;
                    ret = "2W/λ: " + normalizedFreq + CRLF;
                    ret += "|S1(1)1| = " + S111Abs + CRLF +
                          "|S2(1)1| = " + S211Abs + CRLF +
                          "|S1(2)1| = " + S121Abs + CRLF +
                          "|S2(2)1| = " + S221Abs + CRLF +
                          "|S1(1)1|^2 + |S2(1)1|^2 + ... = " + total + CRLF;
                    System.Diagnostics.Debug.WriteLine(ret);
                    series1.Points.Add(new DataPoint(normalizedFreq, S111Abs));
                    series2.Points.Add(new DataPoint(normalizedFreq, S211Abs));
                    series3.Points.Add(new DataPoint(normalizedFreq, S121Abs));
                    series4.Points.Add(new DataPoint(normalizedFreq, S221Abs));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }

                // eigen
                //world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, eigenEz);
                // Ez
                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, Ez);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
