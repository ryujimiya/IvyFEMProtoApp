﻿using System;
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
        public void PCWaveguidePMLSquareLatticeProblem1(MainWindow mainWindow)
        {
            double waveguideWidth = 1.0;

            // フォトニック導波路
            // ロッドの数（半分）
            //const int rodCntHalf = 5;
            const int rodCntHalf = 5;
            // 欠陥ロッド数
            const int defectRodCnt = 1;
            // 格子数
            const int latticeCnt = rodCntHalf * 2 + defectRodCnt;
            // 格子定数
            double latticeA = waveguideWidth / (double)latticeCnt;
            // 周期構造距離
            double periodicDistance = latticeA;
            // ロッドの半径
            double rodRadius = 0.18 * latticeA;
            // ロッドの比誘電率
            double rodEp = 3.4 * 3.4;
            // 格子１辺の分割数
            //const int divForOneLatticeCnt = 6;
            const int divForOneLatticeCnt = 6;
            // 境界の総分割数
            const int divCnt = latticeCnt * divForOneLatticeCnt;
            // ロッドの円周分割数
            const int rodCircleDiv = 8;
            // ロッドの半径の分割数
            const int rodRadiusDiv = 1;
            // メッシュサイズ
            double eLen = 1.05 * waveguideWidth / divCnt;
            // 最小屈折率
            double minEffN = 0.0;
            // 最大屈折率
            double maxEffN = 1.0;

            // 計算する周波数領域
            double sFreq = 0.330;
            double eFreq = 0.440;
            int freqDiv = 25;

            // 形状設定で使用する単位長さ
            double unitLen = periodicDistance;
            // PML層の厚さ
            //int pmlRodCnt = 6;
            int pmlRodCnt = 10;
            double pmlThickness = pmlRodCnt * unitLen;
            // ベンド部
            int bendRodCnt = rodCntHalf * 2 + defectRodCnt;
            double bendLength = bendRodCnt * latticeA;
            // 導波路導入部の長さ
            // 最低3必要
            //int introRodCnt1 = 3;
            int introRodCnt1 = 3;
            double introLength1 = latticeA * introRodCnt1;
            // 最低3必要
            //int introRodCnt2 = 3;
            int introRodCnt2 = 3;
            double introLength2 = latticeA * introRodCnt2;
            // ベンド下側コーナー
            double bendX1 = pmlThickness + introLength1 + bendLength;
            double bendY1 = 0.0;
            // ベンド上側コーナー
            double bendX3 = pmlThickness + introLength1;
            double bendY3 = waveguideWidth;

            // 観測ポート数
            int refPortCnt = 2;
            double port1PMLPosXB1 = 0;
            double port1PMLPosXB2 = pmlThickness;
            double port2PMLPosYB1 = pmlThickness + introLength2 + waveguideWidth;
            double port2PMLPosYB2 = port2PMLPosYB1 - pmlThickness;
            // 観測点
            int port1OfsX = 1;
            int port2OfsX = 1;
            double port1PosXB1 = port1PMLPosXB2 + port1OfsX * unitLen;
            double port1PosXB2 = port1PosXB1 + periodicDistance;
            double port2PosYB1 = port2PMLPosYB2 - port2OfsX * unitLen;
            double port2PosYB2 = port2PosYB1 - periodicDistance;
            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> pmlRodLoopIdsPort1 = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort1 = new List<uint>();
            IList<uint> inputWgRodLoopIdsPort2 = new List<uint>();
            IList<uint> pmlRodLoopIdsPort2 = new List<uint>();
            IList<uint>[] inputWgRodLoopIdss = {
                pmlRodLoopIdsPort1, pmlRodLoopIdsPort2,
                inputWgRodLoopIdsPort1, inputWgRodLoopIdsPort2
            };
            uint[] inputWgBaseLoopIds = { 1, 7, 3, 5 };
            IList<uint> pmlLIds1 = new List<uint>();
            IList<uint> pmlLIds2 = new List<uint>();
            uint[] pmlBaseLoopIds = { 1, 7 };
            IList<uint>[] pmlLIdss = { pmlLIds1, pmlLIds2 };
            IList<uint>[] pmlRodLIdss = { pmlRodLoopIdsPort1, pmlRodLoopIdsPort2 };
            uint[] portInputWgBaseLoopIds = {
                inputWgBaseLoopIds[2], inputWgBaseLoopIds[3] };
            IList<uint>[] portInputWgRodLoopIdss = {
                inputWgRodLoopIdss[2], inputWgRodLoopIdss[3] };

            // Cad
            Cad2D cad = new Cad2D();
            cad.IsSkipAssertValid = true; // AssertValidを無視する
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PMLPosXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PosXB1, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PosXB2, 0.0));
                pts.Add(new OpenTK.Vector2d(bendX1, bendY1));
                pts.Add(new OpenTK.Vector2d(bendX1, port2PosYB2));
                pts.Add(new OpenTK.Vector2d(bendX1, port2PosYB1));
                pts.Add(new OpenTK.Vector2d(bendX1, port2PMLPosYB2));
                pts.Add(new OpenTK.Vector2d(bendX1, port2PMLPosYB1));
                pts.Add(new OpenTK.Vector2d(bendX3, port2PMLPosYB1));
                pts.Add(new OpenTK.Vector2d(bendX3, port2PMLPosYB2));
                pts.Add(new OpenTK.Vector2d(bendX3, port2PosYB1));
                pts.Add(new OpenTK.Vector2d(bendX3, port2PosYB2));
                pts.Add(new OpenTK.Vector2d(bendX3, bendY3));
                pts.Add(new OpenTK.Vector2d(port1PosXB2, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PosXB1, waveguideWidth));
                pts.Add(new OpenTK.Vector2d(port1PMLPosXB2, waveguideWidth));
                uint _lId1 = cad.AddPolygon(pts).AddLId;
                uint _lId2 = cad.ConnectVertexLine(3, 18).AddLId;
                uint _lId3 = cad.ConnectVertexLine(4, 17).AddLId;
                uint _lId4 = cad.ConnectVertexLine(5, 16).AddLId;
                uint _lId5 = cad.ConnectVertexLine(7, 14).AddLId;
                uint _lId6 = cad.ConnectVertexLine(8, 13).AddLId;
                uint _lId7 = cad.ConnectVertexLine(9, 12).AddLId;
            }
            uint port1B1EId = 20;
            uint port1B2EId = 21;
            uint port2B1EId = 23;
            uint port2B2EId = 22;
            uint[] port1EIds = { port1B1EId, port1B2EId };
            uint[] port2EIds = { port2B1EId, port2B2EId };
            uint[][] portEIdss = { port1EIds, port2EIds };
            double[] port1PosXs = { port1PosXB1, port1PosXB2 };
            double[] port2PosYs = { port2PosYB1, port2PosYB2 };
            double[][] portPosXss = { port1PosXs, port2PosYs };
            System.Diagnostics.Debug.Assert(portEIdss.Length == portPosXss.Length);
            // 入出力導波路の周期構造境界上の頂点を追加
            //  逆から追加しているのは、頂点によって新たに生成される辺に頂点を追加しないようにするため
            for (int portId = 0; portId < portEIdss.Length; portId++)
            {
                uint[] _portEIds = portEIdss[portId];
                double[] _portPosXs = portPosXss[portId];
                System.Diagnostics.Debug.Assert(_portEIds.Length == 2);
                System.Diagnostics.Debug.Assert(_portPosXs.Length == 2);
                for (int bcIndex = 0; bcIndex < _portEIds.Length; bcIndex++)
                {
                    uint eId = _portEIds[bcIndex];
                    if (portId == 0)
                    {
                        // X方向周期構造
                        double x1 = _portPosXs[bcIndex];
                        double y1 = 0.0;
                        double y2 = waveguideWidth;
                        PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x1, y2);
                    }
                    else if (portId == 1)
                    {
                        // Y方向周期構造
                        double y1 = _portPosXs[bcIndex];
                        double x1 = bendX1;
                        double x2 = bendX3;
                        PCWaveguideUtils.DivideBoundary(cad, eId, divCnt, x1, y1, x2, y1);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                }
            }

            // ロッドを追加
            System.Diagnostics.Debug.Assert(inputWgRodLoopIdss.Length == inputWgBaseLoopIds.Length);
            // ポート1側直線導波路のロッドを追加
            {
                int rodCntPMLPort1 = pmlRodCnt;
                int rodCntPort1Side = rodCntPMLPort1 + introRodCnt1;
                for (int col = 0; col < rodCntPort1Side; col++)
                {
                    double centerX = latticeA * 0.5 + col * latticeA;
                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    if (centerX >= 0 && centerX < port1PMLPosXB2)
                    {
                        baseLoopId = 1;
                    }
                    else if (centerX >= port1PMLPosXB2 && centerX < port1PosXB1)
                    {
                        baseLoopId = 2;
                    }
                    else if (centerX >= port1PosXB1 && centerX < port1PosXB2)
                    {
                        baseLoopId = 3;
                    }
                    else if (centerX >= port1PosXB2 && centerX < bendX3)
                    {
                        baseLoopId = 4;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                    {
                        int index = Array.IndexOf(inputWgBaseLoopIds, baseLoopId);
                        if (index != -1)
                        {
                            inputWgNo = index + 1;
                        }
                        else
                        {
                            inputWgNo = 0;
                        }
                    }

                    for (int row = 0; row < rodCntHalf; row++)
                    {
                        double x0 = centerX;
                        double y0 = waveguideWidth - row * latticeA - latticeA * 0.5;
                        uint lId = PCWaveguideUtils.AddRod(
                            cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                        rodLoopIds.Add(lId);
                        if (inputWgNo != 0)
                        {
                            inputWgRodLoopIdss[inputWgNo - 1].Add(lId);
                        }
                    }
                    for (int row = 0; row < rodCntHalf; row++)
                    {
                        double x0 = centerX;
                        double y0 = latticeA * rodCntHalf - row * latticeA - latticeA * 0.5;
                        uint lId = PCWaveguideUtils.AddRod(
                            cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                        rodLoopIds.Add(lId);
                        if (inputWgNo != 0)
                        {
                            inputWgRodLoopIdss[inputWgNo - 1].Add(lId);
                        }
                    }
                }
            }

            // ポート2側直線導波路のロッドを追加
            {
                int rodCntPMLPort2 = pmlRodCnt;
                int rodCntPort2Side = rodCntPMLPort2 + introRodCnt2;
                for (int col = 0; col < rodCntPort2Side; col++)
                {
                    double centerY = port2PMLPosYB1 - latticeA * 0.5 - col * latticeA;
                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    if (centerY <= port2PMLPosYB1 && centerY > port2PMLPosYB2)
                    {
                        baseLoopId = 7;
                    }
                    else if (centerY <= port2PMLPosYB2 && centerY > port2PosYB1)
                    {
                        baseLoopId = 6;
                    }
                    else if (centerY <= port2PosYB1 && centerY > port2PosYB2)
                    {
                        baseLoopId = 5;
                    }
                    else if (centerY <= port2PosYB2 && centerY > bendY3)
                    {
                        baseLoopId = 4;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                    {
                        int index = Array.IndexOf(inputWgBaseLoopIds, baseLoopId);
                        if (index != -1)
                        {
                            inputWgNo = index + 1;
                        }
                        else
                        {
                            inputWgNo = 0;
                        }
                    }

                    for (int row = 0; row < rodCntHalf; row++)
                    {
                        double y0 = centerY;
                        double x0 = bendX3 + row * latticeA + latticeA * 0.5;
                        uint lId = PCWaveguideUtils.AddRod(
                            cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                        rodLoopIds.Add(lId);
                        if (inputWgNo != 0)
                        {
                            inputWgRodLoopIdss[inputWgNo - 1].Add(lId);
                        }
                    }
                    for (int row = 0; row < rodCntHalf; row++)
                    {
                        double y0 = centerY;
                        double x0 = bendX3 + latticeA * (rodCntHalf + defectRodCnt) + row * latticeA + latticeA * 0.5;
                        uint lId = PCWaveguideUtils.AddRod(
                            cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                        rodLoopIds.Add(lId);
                        if (inputWgNo != 0)
                        {
                            inputWgRodLoopIdss[inputWgNo - 1].Add(lId);
                        }
                    }
                }
            }

            // コーナーのロッドを追加
            {
                System.Diagnostics.Debug.Assert(defectRodCnt == 1);
                int rowcolRodCnt = rodCntHalf * 2 + defectRodCnt;
                for (int col = 0; col < rowcolRodCnt; col++)
                {
                    uint baseLoopId = 4;
                    for (int row = 0; row < rowcolRodCnt; row++)
                    {
                        // 直角ベンド
                        if (row == rodCntHalf && col < rowcolRodCnt - rodCntHalf) continue;
                        if (col == rodCntHalf && row < rowcolRodCnt - rodCntHalf) continue;

                        double x0 = bendX3 + latticeA * 0.5 + col * latticeA;
                        double y0 = waveguideWidth - latticeA * 0.5 - row * latticeA;
                        uint lId = PCWaveguideUtils.AddRod(
                            cad, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                        rodLoopIds.Add(lId);
                    }
                }
            }

            // pml
            for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
            {
                IList<uint> pmlLIds = pmlLIdss[pmlIndex];
                uint lId0 = pmlBaseLoopIds[pmlIndex];
                pmlLIds.Add(lId0);

                IList<uint> workrodLIds = pmlRodLIdss[pmlIndex];
                foreach (uint lId in workrodLIds)
                {
                    pmlLIds.Add(lId);
                }
            }

            // check
            {
                double[] rodColor = { 1.0, 0.6, 0.6 };
                double[] pmlColor = { 0.5, 0.5, 0.5 };
                double[] pmlCoreColor = { 0.6, 0.3, 0.3 };
                for (int iL = 0; iL < (7 + rodLoopIds.Count); iL++)
                {
                    uint lId = (uint)(iL + 1);
                    int hitPMLIndex = -1;
                    for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
                    {
                        IList<uint> pmlLIds = pmlLIdss[pmlIndex];
                        IList<uint> pmlRodLIds = pmlRodLIdss[pmlIndex];
                        if (pmlLIds.Contains(lId))
                        {
                            hitPMLIndex = pmlIndex;
                            if (pmlRodLIds.Contains(lId))
                            {
                                cad.SetLoopColor(lId, pmlCoreColor);
                            }
                            else
                            {
                                cad.SetLoopColor(lId, pmlColor);
                            }
                            break;
                        }
                    }
                    if (hitPMLIndex == -1)
                    {
                        if (rodLoopIds.Contains(lId))
                        {
                            cad.SetLoopColor(lId, rodColor);
                        }
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
            IList<uint> pmlCoreMaIds = new List<uint>();
            IList<uint> pmlCladdingMaIds = new List<uint>();
            {
                world.ClearMaterial();

                // 媒質リスト作成
                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = rodEp,
                    Epyy = rodEp,
                    Epzz = rodEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricPMLMaterial pmlCladdingMa1 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosXB2, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0
                };
                DielectricPMLMaterial pmlCoreMa1 = new DielectricPMLMaterial
                {
                    Epxx = rodEp,
                    Epyy = rodEp,
                    Epzz = rodEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosXB2, 0.0),
                    XThickness = pmlThickness,
                    YThickness = 0.0
                };
                DielectricPMLMaterial pmlCladdingMa2 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // Y方向PML
                    OriginPoint = new OpenTK.Vector2d(bendX3, port2PMLPosYB2),
                    XThickness = 0.0,
                    YThickness = pmlThickness
                };
                DielectricPMLMaterial pmlCoreMa2 = new DielectricPMLMaterial
                {
                    Epxx = rodEp,
                    Epyy = rodEp,
                    Epzz = rodEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // Y方向PML
                    OriginPoint = new OpenTK.Vector2d(bendX3, port2PMLPosYB2),
                    XThickness = 0.0,
                    YThickness = pmlThickness
                };

                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

                DielectricPMLMaterial[] pmlCladdingMas = { pmlCladdingMa1, pmlCladdingMa2 };
                DielectricPMLMaterial[] pmlCoreMas = { pmlCoreMa1, pmlCoreMa2 };
                for (int pmlIndex = 0; pmlIndex < pmlCladdingMas.Length; pmlIndex++)
                {
                    var pmlCladdingMa = pmlCladdingMas[pmlIndex];
                    var pmlCoreMa = pmlCoreMas[pmlIndex];
                    uint pmlCladdingMaId = world.AddMaterial(pmlCladdingMa);
                    pmlCladdingMaIds.Add(pmlCladdingMaId);
                    uint pmlCoreMaId = world.AddMaterial(pmlCoreMa);
                    pmlCoreMaIds.Add(pmlCoreMaId);
                }

                System.Diagnostics.Debug.Assert(pmlLIdss.Length == pmlCladdingMaIds.Count);
                System.Diagnostics.Debug.Assert(pmlLIdss.Length == pmlCoreMaIds.Count);

                uint[] lIds = new uint[7 + rodLoopIds.Count];
                for (int i = 0; i < 7; i++)
                {
                    lIds[i] = (uint)(i + 1);
                }
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 7] = rodLoopIds[i];
                }
                uint[] maIds = new uint[lIds.Length];
                for (int i = 0; i < lIds.Length; i++)
                {
                    uint lId = lIds[i];
                    uint maId = claddingMaId;
                    int hitPMLIndex = -1;
                    for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
                    {
                        IList<uint> pmlLIds = pmlLIdss[pmlIndex];
                        IList<uint> pmlRodLIds = pmlRodLIdss[pmlIndex];
                        if (pmlLIds.Contains(lId))
                        {
                            hitPMLIndex = pmlIndex;
                            if (pmlRodLIds.Contains(lId))
                            {
                                maId = pmlCoreMaIds[hitPMLIndex];
                            }
                            else
                            {
                                maId = pmlCladdingMaIds[hitPMLIndex];
                            }
                            break;
                        }
                    }
                    if (hitPMLIndex == -1)
                    {
                        if (rodLoopIds.Contains(lId))
                        {
                            maId = coreMaId;
                        }
                        else
                        {
                            maId = claddingMaId;
                        }
                    }

                    maIds[i] = maId;
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
            bool[] isPortBc2Reverse = { false, false, false };
            bool[] isYDirectionPeriodic = { false, true, false };
            System.Diagnostics.Debug.Assert(isPortBc2Reverse.Length == (refPortCnt + 1));
            for (int portId = 0; portId < (refPortCnt + 1); portId++)
            {
                var wgPortInfo = new PCWaveguidePortInfo();
                wgPortInfos.Add(wgPortInfo);
                System.Diagnostics.Debug.Assert(wgPortInfos.Count == (portId + 1));
                wgPortInfo.IsSVEA = true; // 緩慢変化包絡線近似
                wgPortInfo.IsPortBc2Reverse = isPortBc2Reverse[portId];
                wgPortInfo.IsYDirectionPeriodic = isYDirectionPeriodic[portId];
                wgPortInfo.LatticeA = latticeA;
                wgPortInfo.PeriodicDistanceX = periodicDistance;
                wgPortInfo.MinEffN = minEffN;
                wgPortInfo.MaxEffN = maxEffN;
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // 周期構造入出力導波路
            for (int portId = 0; portId < refPortCnt; portId++)
            {
                uint lId0 = portInputWgBaseLoopIds[portId];
                var inputWgRodLoopIds = portInputWgRodLoopIdss[portId];
                // ワールド座標系のループIDを取得
                uint[] lIds = null;

                lIds = new uint[1 + inputWgRodLoopIds.Count];
                lIds[0] = lId0;
                for (int i = 0; i < inputWgRodLoopIds.Count; i++)
                {
                    lIds[i + 1] = inputWgRodLoopIds[i];
                }

                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.LoopIds = new List<uint>(lIds);
            }
            {
                int srcPortId = refPortCnt;
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[srcPortId];
                int port1PortId = 0;
                PCWaveguidePortInfo wgPortInfoPort1 = wgPortInfos[port1PortId];
                wgPortInfo.LoopIds = new List<uint>(wgPortInfoPort1.LoopIds);
            }
            // 周期構造境界1
            for (int portId = 0; portId < refPortCnt; portId++)
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];

                int bcIndex = 0; // B1
                uint eId0 = portEIdss[portId][bcIndex];
                eIds[0] = eId0;
                maIds[0] = claddingMaId;

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    eIds[i] = (uint)(24 + (divCnt - 1) * (1 + 2 * portId) - (i - 1));
                    maIds[i] = claddingMaId;
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
            {
                int srcPortId = refPortCnt;
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[srcPortId];
                int port1PortId = 0;
                PCWaveguidePortInfo wgPortInfoPort1 = wgPortInfos[port1PortId];
                wgPortInfo.BcEdgeIds1 = new List<uint>(wgPortInfoPort1.BcEdgeIds1);
            }
            // 周期構造境界2
            for (int portId = 0; portId < refPortCnt; portId++)
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];

                int bcIndex = 1; // B2
                uint eId0 = portEIdss[portId][bcIndex];
                eIds[0] = eId0;
                maIds[0] = claddingMaId;

                for (int i = 1; i <= (divCnt - 1); i++)
                {
                    eIds[i] = (uint)(24 + (divCnt - 1) * (2 * (portId + 1)) - (i - 1));
                    maIds[i] = claddingMaId;
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
            {
                int srcPortId = refPortCnt;
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[srcPortId];
                int port1PortId = 0;
                PCWaveguidePortInfo wgPortInfoPort1 = wgPortInfos[port1PortId];
                wgPortInfo.BcEdgeIds2 = new List<uint>(wgPortInfoPort1.BcEdgeIds2);
            }

            {
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                for (int portId = 0; portId < (refPortCnt + 1); portId++)
                {
                    PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                    IList<uint> lIds = wgPortInfo.LoopIds;
                    IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                    IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                    // 複素数
                    PortCondition portCondition = new PortCondition(
                        lIds, bcEIds1, bcEIds2, FieldValueType.ZScalar, new List<uint> { 0 }, 0);
                    portConditions.Add(portCondition);
                }
            }

            uint[] zeroEIds = { 2, 3, 4, 5, 6, 7, 8, 9, 11, 12, 13, 14, 15, 16, 17, 18 };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            // ポートの境界上の節点を準備する
            for (int portId = 0; portId < (refPortCnt + 1); portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.SetupAfterMakeElements(world, quantityId, (uint)portId);
            }
            // フォトニック結晶導波路チャンネル上節点を取得する
            for (int portId = 0; portId < (refPortCnt + 1); portId++)
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
                    foreach (int coId in coIds)
                    {
                        // 座標からチャンネル(欠陥部)を判定する
                        double[] coord = world.GetCoord(quantityId, coId);
                        if (wgPortInfo.IsYDirectionPeriodic)
                        {
                            // Y方向周期構造
                            double x = Math.Abs(coord[0] - coord0[0]); // X座標
                            if (x >= (waveguideWidth - latticeA * (rodCntHalf + defectRodCnt)) &&
                                x <= (waveguideWidth - latticeA * (rodCntHalf)))
                            {
                                channelCoIds.Add(coId);
                            }
                        }
                        else
                        {
                            // X方向周期構造
                            double y = Math.Abs(coord[1] - coord0[1]); // Y座標
                            if (y >= (waveguideWidth - latticeA * (rodCntHalf + defectRodCnt)) &&
                                y <= (waveguideWidth - latticeA * (rodCntHalf)))
                            {
                                channelCoIds.Add(coId);
                            }
                        }
                    }
                }
            }

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            ChartWindow chartWin = ChartWindow1;
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
                Maximum = 1.0
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            var series1 = new LineSeries
            {
                Title = "|S11|"
            };
            var series2 = new LineSeries
            {
                Title = "|S21|"
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
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
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波長
                double waveLength = latticeA / normalizedFreq;
                // 周波数
                double freq = Constants.C0 / waveLength;
                // 角周波数
                double omega = 2.0 * Math.PI * freq;
                // 波数
                double k0 = omega / Constants.C0;
                System.Diagnostics.Debug.WriteLine("a / λ: " + normalizedFreq);

                var FEM = new PCWaveguide2DPMLFEM(world);
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
                FEM.WgPortInfos = wgPortInfos;
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] Ez = FEM.Ez;
                System.Numerics.Complex[][] S = FEM.S;

                System.Numerics.Complex S11 = S[0][0];
                System.Numerics.Complex S21 = S[1][0];
                double S11Abs = S11.Magnitude;
                double S21Abs = S21.Magnitude;
                double total = S11Abs * S11Abs + S21Abs * S21Abs;

                string ret;
                string CRLF = System.Environment.NewLine;
                ret = "2W/λ: " + normalizedFreq + CRLF;
                ret += "|S11| = " + S11Abs + CRLF +
                      "|S21| = " + S21Abs + CRLF +
                      "|S11|^2 + |S21|^2 = " + total + CRLF;
                System.Diagnostics.Debug.WriteLine(ret);
                //AlertWindow.ShowDialog(ret, "");
                series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, Ez);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
