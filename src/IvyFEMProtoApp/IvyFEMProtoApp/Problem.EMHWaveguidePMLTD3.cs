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
        public void HWaveguidePMLTDProblem3(MainWindow mainWindow)
        {
            // 直線導波路を解く
            double[] freqs;
            System.Numerics.Complex[] freqDomainAmpsInc;
            SolveHWaveguidePMLTDProblem3_0(
                mainWindow, out freqs, out freqDomainAmpsInc);

            WPFUtils.DoEvents(10 * 1000);

            // 対象導波路を解く(直線導波路と同じ導波路、同じ計算条件である必要がある)
            SolveHWaveguidePMLTDProblem3(mainWindow, freqDomainAmpsInc);
        }

        // Open Waveguide
        public void SolveHWaveguidePMLTDProblem3(
            MainWindow mainWindow, System.Numerics.Complex[] freqDomainAmpsInc)
        {
            double waveguideWidth = 1.0;
            //double eLen = waveguideWidth * (0.95 * 1.0 / 30.0);
            double eLen = waveguideWidth * (0.95 * 1.0 / 30.0);
            //double eLen = waveguideWidth * (0.95 * 1.25 / 30.0); // 計算時間緩和
            // 誘電体スラブ導波路幅
            double coreWidth = waveguideWidth * (4.0 / 30.0);
            // 誘電体スラブ比誘電率
            double coreEp = 3.6 * 3.6;
            double claddingEp = 3.24 * 3.24;
            bool isTMMode = true; // TMモード

            // 時間刻み幅の算出
            double courantNumber = 0.5;
            // Note: timeLoopCnt は 2^mでなければならない
            //int timeLoopCnt = 4096;
            int timeLoopCnt = 2048;
            double timeStep = courantNumber * eLen / (Constants.C0 * Math.Sqrt(2.0));
            // 励振源
            // 規格化周波数
            // 2Wc √(n1^2 - n2^2) / λ (Wc:コアの幅)
            //double srcNormalizedFreq = 0.10;
            double srcNormalizedFreq = 0.75;
            // 波長
            double srcWaveLength = 2.0 * coreWidth * Math.Sqrt(coreEp - claddingEp) / srcNormalizedFreq;
            // 周波数
            double srcFreq = Constants.C0 / srcWaveLength;
            // 角周波数
            double srcOmega = 2.0 * Math.PI * srcFreq;
            // 計算する周波数領域
            double normalizedFreq1 = 0.50;
            double normalizedFreq2 = 1.00;
            double waveLength1 = 2.0 * coreWidth * Math.Sqrt(coreEp - claddingEp) / normalizedFreq1;
            double waveLength2 = 2.0 * coreWidth * Math.Sqrt(coreEp - claddingEp) / normalizedFreq2;
            double freq1 = Constants.C0 / waveLength1;
            double freq2 = Constants.C0 / waveLength2;
            // 規格化周波数変換
            Func<double, double> toNormalizedFreq =
                waveLength => 2.0 * coreWidth * Math.Sqrt(coreEp - claddingEp) / waveLength;
            // 1D固有値問題で減衰定数を使う?
            bool[] isEigen1DUseDecayParameters = {
                true,
                false,
                true
            };
            // 1D固有値問題のクラッド比誘電率
            double[] eigen1DCladdingEp = {
                claddingEp,
                0.0,
                claddingEp
            };

            // ガウシアンパルス
            GaussianType gaussianType = GaussianType.SinModulation; // 正弦波変調
            // 搬送波の振動回数
            int nCycle = 5;
            double gaussianT0 = 1.00 * (1.0 / srcFreq) * nCycle / 2.0;
            double gaussianTp = gaussianT0 / (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));

            const int portCnt = 2;
            // 形状設定で使用する単位長さ
            double unitLen = waveguideWidth / 20.0;
            // PML層の厚さ
            double pmlXThickness = 10 * unitLen;
            double pmlYThickness = 5 * unitLen;
            // 導波管不連続領域の長さ
            double disconLength = 2.0 * waveguideWidth + 2.0 * pmlXThickness;
            // PML位置
            double port1PMLPosX = pmlXThickness;
            double port2PMLPosX = disconLength - pmlXThickness;
            // 励振位置
            double srcPosX = port1PMLPosX + 5 * unitLen;
            // 誘電体終端
            double terminalPosX = port1PMLPosX + 1.0 * waveguideWidth;
            System.Diagnostics.Debug.Assert(terminalPosX > srcPosX);
            // 観測点
            int port1OfsX = 5;
            int port2OfsX = 5;
            double port1PosX = srcPosX + port1OfsX * unitLen;
            double port2PosX = port2PMLPosX - port2OfsX * unitLen;
            double waveguideWidthWithPML = waveguideWidth + 2.0 * pmlYThickness;
            double posY1 = pmlYThickness;
            double posY2 = waveguideWidthWithPML - pmlYThickness;
            double coreY1 = posY1 + (waveguideWidth - coreWidth) * 0.5;
            double coreY2 = coreY1 + coreWidth;
            uint loopCnt = 29;
            uint[] pmlLIds1 = { 9, 10, 11 };
            uint[] pmlCoreLIds1 = { 10 };
            uint[] pmlCladdingLIds1 = { 9, 11 };
            uint[] pmlLIds2 = { 29 };
            uint[] pmlCoreLIds2 = { };
            uint[] pmlCladdingLIds2 = { };
            uint[] pmlLIds3 = { 12, 16, 20, 24, 26 };
            uint[] pmlCoreLIds3 = { };
            uint[] pmlCladdingLIds3 = { 12, 16, 20 };
            uint[] pmlLIds4 = { 2, 3, 4, 5, 6 };
            uint[] pmlCoreLIds4 = { };
            uint[] pmlCladdingLIds4 = { 2, 3, 4 };
            uint[] pmlLIds5 = { 8 };
            uint[] pmlCoreLIds5 = { };
            uint[] pmlCladdingLIds5 = { 8 };
            uint[] pmlLIds6 = { 28 };
            uint[] pmlCoreLIds6 = { };
            uint[] pmlCladdingLIds6 = { };
            uint[] pmlLIds7 = { 1 };
            uint[] pmlCoreLIds7 = { };
            uint[] pmlCladdingLIds7 = { 1 };
            uint[] pmlLIds8 = { 7 };
            uint[] pmlCoreLIds8 = { };
            uint[] pmlCladdingLIds8 = { };
            uint[][] pmlLIdss = { pmlLIds1, pmlLIds2, pmlLIds3, pmlLIds4, pmlLIds5, pmlLIds6, pmlLIds7, pmlLIds8 };
            uint[][] pmlCoreLIdss = {
                pmlCoreLIds1, pmlCoreLIds2, pmlCoreLIds3, pmlCoreLIds4,
                pmlCoreLIds5, pmlCoreLIds6, pmlCoreLIds7, pmlCoreLIds8
            };
            uint[][] pmlCladdingLIdss = {
                pmlCladdingLIds1, pmlCladdingLIds2, pmlCladdingLIds3, pmlCladdingLIds4,
                pmlCladdingLIds5, pmlCladdingLIds6, pmlCladdingLIds7, pmlCladdingLIds8
            };
            uint[] coreLIds = { 14, 18, 22 };
            uint[] claddingLIds = { 15, 19, 23, 13, 17, 21 };
            uint[] refport1EIds = { 38, 37, 36 };
            uint[] refport1CoreEIds = { 37 };
            uint[] refport1CladdingEIds = { 38, 36 };
            uint[] refport2EIds = { 44 };
            uint[] refport2CoreEIds = { };
            uint[] refport2CladdingEIds = { };
            uint[] portSrcEIds = { 34, 33, 32 };
            uint[] portSrcCoreEIds = { 33 };
            uint[] portSrcCladdingEIds = { 34, 32 };
            // メッシュの長さ
            double[] eLens = new double[loopCnt];
            for (int i = 0; i < loopCnt; i++)
            {
                uint lId = (uint)(i + 1);
                //double workeLen = eLen;
                double workeLen = eLen;
                int hitPMLIndex = -1;
                for (int pmlIndex = 0; pmlIndex < pmlCoreLIdss.Length; pmlIndex++)
                {
                    uint[] pmlCoreLIds = pmlCoreLIdss[pmlIndex];
                    if (pmlCoreLIds.Contains(lId))
                    {
                        hitPMLIndex = pmlIndex;
                        break;
                    }
                }
                if (hitPMLIndex != -1 || coreLIds.Contains(lId))
                {
                    workeLen = eLen * 0.5;
                }
                else
                {
                    workeLen = eLen;
                }
                eLens[i] = workeLen;
            }
            Cad2D cad = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PMLPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(srcPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PosX, 0.0));
                pts.Add(new OpenTK.Vector2d(terminalPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PMLPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(port2PMLPosX, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(port2PosX, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(terminalPosX, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(port1PosX, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(srcPosX, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(port1PMLPosX, waveguideWidthWithPML));
                uint _lId1 = cad.AddPolygon(pts).AddLId;
                uint _lId2 = cad.ConnectVertexLine(3, 16).AddLId;
                uint _lId3 = cad.ConnectVertexLine(4, 15).AddLId;
                uint _lId4 = cad.ConnectVertexLine(5, 14).AddLId;
                uint _lId5 = cad.ConnectVertexLine(6, 13).AddLId;
                uint _lId6 = cad.ConnectVertexLine(7, 12).AddLId;
                uint _lId7 = cad.ConnectVertexLine(8, 11).AddLId;
            }
            // スラブ導波路と境界の交点
            {
                // 入力面と終端と参照面1、励振面
                double[] portXs1 = { 0.0, port1PMLPosX, srcPosX, port1PosX, terminalPosX };
                uint[] parentEIds1 = { 1, 17, 18, 19, 20 };
                IList<uint[]> slabVIds1 = new List<uint[]>();
                for (int index = 0; index < portXs1.Length; index++)
                {
                    double portX = portXs1[index];
                    uint parentEId = parentEIds1[index];

                    double workY1 = 0.0;
                    double workY2 = 0.0;
                    double workY3 = 0.0;
                    double workY4 = 0.0;
                    if (index == 0)
                    {
                        // 入力面
                        workY1 = posY1;
                        workY2 = coreY1;
                        workY3 = coreY2;
                        workY4 = posY2;
                    }
                    else
                    {
                        workY1 = posY2;
                        workY2 = coreY2;
                        workY3 = coreY1;
                        workY4 = posY1;
                    }
                    uint vId1 = cad.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY1)).AddVId;
                    uint vId2 = cad.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY2)).AddVId;
                    uint vId3 = cad.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY3)).AddVId;
                    uint vId4 = cad.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY4)).AddVId;
                    uint[] workVIds = new uint[4];
                    if (index == 0)
                    {
                        // 入力面
                        workVIds[0] = vId1;
                        workVIds[1] = vId2;
                        workVIds[2] = vId3;
                        workVIds[3] = vId4;
                    }
                    else
                    {
                        workVIds[0] = vId4;
                        workVIds[1] = vId3;
                        workVIds[2] = vId2;
                        workVIds[3] = vId1;
                    }
                    slabVIds1.Add(workVIds);
                }

                // 出力面、参照面2
                double[] portXs2 = { port2PMLPosX, port2PosX, disconLength };
                uint[] parentEIds2 = { 21, 22, 9 };
                IList<uint[]> slabVIds2 = new List<uint[]>();
                for (int index = 0; index < portXs2.Length; index++)
                {
                    double portX = portXs2[index];
                    uint parentEId = parentEIds2[index];

                    double workY1 = 0.0;
                    double workY2 = 0.0;
                    {
                        workY1 = posY2;
                        workY2 = posY1;
                    }
                    uint vId1 = cad.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY1)).AddVId;
                    uint vId2 = cad.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY2)).AddVId;
                    uint[] workVIds = new uint[2];
                    {
                        workVIds[0] = vId2;
                        workVIds[1] = vId1;
                    }
                    slabVIds2.Add(workVIds);
                }

                // スラブ導波路
                {
                    for (int portIndex = 0; portIndex < (portXs1.Length - 1); portIndex++)
                    {
                        int vIdCnt = slabVIds1[portIndex].Length;
                        for (int i = 0; i < vIdCnt; i++)
                        {
                            uint workVId1 = slabVIds1[portIndex][i];
                            uint workVId2 = slabVIds1[portIndex + 1][i];
                            uint workEId = cad.ConnectVertexLine(workVId1, workVId2).AddEId;
                        }
                    }
                    // 誘電体終端とポート2参照面間
                    {
                        {
                            uint workVId1 = slabVIds1[slabVIds1.Count - 1][0];
                            uint workVId2 = slabVIds2[0][0];
                            uint workEId = cad.ConnectVertexLine(workVId1, workVId2).AddEId;
                        }
                        {
                            uint workVId1 = slabVIds1[slabVIds1.Count - 1][3];
                            uint workVId2 = slabVIds2[0][1];
                            uint workEId = cad.ConnectVertexLine(workVId1, workVId2).AddEId;
                        }
                    }
                    // ポート2参照面以降
                    for (int portIndex = 0; portIndex < (portXs2.Length - 1); portIndex++)
                    {
                        int vIdCnt = slabVIds2[portIndex].Length;
                        for (int i = 0; i < vIdCnt; i++)
                        {
                            uint workVId1 = slabVIds2[portIndex][i];
                            uint workVId2 = slabVIds2[portIndex + 1][i];
                            uint workEId = cad.ConnectVertexLine(workVId1, workVId2).AddEId;
                        }
                    }
                }
            }

            // check
            {
                double[] pmlColor = { 0.5, 0.5, 0.5 };
                double[] pmlCoreColor = { 0.6, 0.3, 0.3 };
                double[] pmlCladdingColor = { 0.8, 1.0, 0.8 };
                for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
                {
                    uint[] pmlLIds = pmlLIdss[pmlIndex];
                    uint[] pmlCoreLIds = pmlCoreLIdss[pmlIndex];
                    uint[] pmlCladdingLIds = pmlCladdingLIdss[pmlIndex];
                    foreach (uint lId in pmlLIds)
                    {
                        if (pmlCoreLIds.Contains(lId))
                        {
                            cad.SetLoopColor(lId, pmlCoreColor);
                        }
                        else if (pmlCladdingLIds.Contains(lId))
                        {
                            cad.SetLoopColor(lId, pmlCladdingColor);
                        }
                        else
                        {
                            cad.SetLoopColor(lId, pmlColor);
                        }
                    }
                }

                double[] coreColor = { 0.6, 0.0, 0.0 };
                foreach (uint lId in coreLIds)
                {
                    cad.SetLoopColor(lId, coreColor);
                }
                double[] edgeCoreColor = { 0.4, 0.0, 0.0 };
                uint[][] portCoreEIdss = { refport1CoreEIds, refport2CoreEIds, portSrcCoreEIds };
                foreach (uint[] eIds in portCoreEIdss)
                {
                    foreach (uint eId in eIds)
                    {
                        cad.SetEdgeColor(eId, edgeCoreColor);
                    }
                }
                double[] claddingColor = { 0.5, 1.0, 0.5 };
                foreach (uint lId in claddingLIds)
                {
                    cad.SetLoopColor(lId, claddingColor);
                }
                double[] edgeCladdingColor = { 0.3, 0.8, 0.3 };
                uint[][] portCladdingEIdss = { refport1CladdingEIds, refport2CladdingEIds, portSrcCladdingEIds };
                foreach (uint[] eIds in portCladdingEIdss)
                {
                    foreach (uint eId in eIds)
                    {
                        cad.SetEdgeColor(eId, edgeCladdingColor);
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

            //Mesher2D mesher = new Mesher2D(cad, eLen);
            Mesher2D mesher = new Mesher2D();
            mesher.SetMeshingModeElemLength();
            for (int i = 0; i < loopCnt; i++)
            {
                uint lId = (uint)(i + 1);
                double workeLen = eLens[i];
                mesher.AddMeshingLoopCadId(lId, workeLen);
            }
            mesher.MakeMesh(cad);

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
                uint dof = 1; // スカラー
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint vacuumMaId = 0;
            uint claddingMaId = 0;
            uint coreMaId = 0;
            IList<uint> pmlVacuumMaIds = new List<uint>();
            IList<uint> pmlCoreMaIds = new List<uint>();
            IList<uint> pmlCladdingMaIds = new List<uint>();
            {
                world.ClearMaterial();
                DielectricMaterial vacuumMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = coreEp,
                    Epyy = coreEp,
                    Epzz = coreEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricPMLMaterial pmlVacuumMa1 = null;
                DielectricPMLMaterial pmlCladdingMa1 = new DielectricPMLMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, 0.0),
                    XThickness = pmlXThickness,
                    YThickness = 0.0
                };
                DielectricPMLMaterial pmlCoreMa1 = new DielectricPMLMaterial
                {
                    Epxx = coreEp,
                    Epyy = coreEp,
                    Epzz = coreEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, 0.0),
                    XThickness = pmlXThickness,
                    YThickness = 0.0
                };
                DielectricPMLMaterial pmlVacuumMa2 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLPosX, 0.0),
                    XThickness = pmlXThickness,
                    YThickness = 0.0
                };
                DielectricPMLMaterial pmlCladdingMa2 = null;
                DielectricPMLMaterial pmlCoreMa2 = null;
                DielectricPMLMaterial pmlVacuumMa3 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // Y方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY1),
                    XThickness = 0.0,
                    YThickness = pmlYThickness
                };
                DielectricPMLMaterial pmlCladdingMa3 = new DielectricPMLMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // Y方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY1),
                    XThickness = 0.0,
                    YThickness = pmlYThickness
                };
                DielectricPMLMaterial pmlCoreMa3 = null;
                DielectricPMLMaterial pmlVacuumMa4 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // Y方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY2),
                    XThickness = 0.0,
                    YThickness = pmlYThickness
                };
                DielectricPMLMaterial pmlCladdingMa4 = new DielectricPMLMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // Y方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY2),
                    XThickness = 0.0,
                    YThickness = pmlYThickness
                };
                DielectricPMLMaterial pmlCoreMa4 = null;
                DielectricPMLMaterial pmlVacuumMa5 = null;
                DielectricPMLMaterial pmlCladdingMa5 = new DielectricPMLMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // XY方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY1),
                    XThickness = pmlXThickness,
                    YThickness = pmlYThickness
                };
                DielectricPMLMaterial pmlCoreMa5 = null;
                DielectricPMLMaterial pmlVacuumMa6 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // XY方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLPosX, posY1),
                    XThickness = pmlXThickness,
                    YThickness = pmlYThickness
                };
                DielectricPMLMaterial pmlCladdingMa6 = null;
                DielectricPMLMaterial pmlCoreMa6 = null;
                DielectricPMLMaterial pmlVacuumMa7 = null;
                DielectricPMLMaterial pmlCladdingMa7 = new DielectricPMLMaterial
                {
                    Epxx = claddingEp,
                    Epyy = claddingEp,
                    Epzz = claddingEp,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // XY方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY2),
                    XThickness = pmlXThickness,
                    YThickness = pmlYThickness
                };
                DielectricPMLMaterial pmlCoreMa7 = null;
                DielectricPMLMaterial pmlVacuumMa8 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0,
                    // XY方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLPosX, posY2),
                    XThickness = pmlXThickness,
                    YThickness = pmlYThickness
                };
                DielectricPMLMaterial pmlCladdingMa8 = null;
                DielectricPMLMaterial pmlCoreMa8 = null;

                vacuumMaId = world.AddMaterial(vacuumMa);
                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

                DielectricPMLMaterial[] pmlVacuumMas = {
                    pmlVacuumMa1, pmlVacuumMa2, pmlVacuumMa3, pmlVacuumMa4,
                    pmlVacuumMa5, pmlVacuumMa6, pmlVacuumMa7, pmlVacuumMa8
                };
                DielectricPMLMaterial[] pmlCladdingMas = {
                    pmlCladdingMa1, pmlCladdingMa2, pmlCladdingMa3, pmlCladdingMa4,
                    pmlCladdingMa5, pmlCladdingMa6, pmlCladdingMa7, pmlCladdingMa8
                };
                DielectricPMLMaterial[] pmlCoreMas = {
                    pmlCoreMa1, pmlCoreMa2, pmlCoreMa3, pmlCoreMa4,
                    pmlCoreMa5, pmlCoreMa6, pmlCoreMa7, pmlCoreMa8
                };
                for (int pmlIndex = 0; pmlIndex < pmlCladdingMas.Length; pmlIndex++)
                {
                    var pmlVacuumMa = pmlVacuumMas[pmlIndex];
                    var pmlCladdingMa = pmlCladdingMas[pmlIndex];
                    var pmlCoreMa = pmlCoreMas[pmlIndex];
                    uint pmlVacuumMaId = world.AddMaterial(pmlVacuumMa);
                    pmlVacuumMaIds.Add(pmlVacuumMaId);
                    uint pmlCladdingMaId = world.AddMaterial(pmlCladdingMa);
                    pmlCladdingMaIds.Add(pmlCladdingMaId);
                    uint pmlCoreMaId = world.AddMaterial(pmlCoreMa);
                    pmlCoreMaIds.Add(pmlCoreMaId);
                }

                System.Diagnostics.Debug.Assert(pmlLIdss.Length == pmlVacuumMaIds.Count);
                System.Diagnostics.Debug.Assert(pmlLIdss.Length == pmlCladdingMaIds.Count);
                System.Diagnostics.Debug.Assert(pmlLIdss.Length == pmlCoreMaIds.Count);
            }
            {
                uint[] lIds = new uint[loopCnt];
                for (int i = 0; i < loopCnt; i++)
                {
                    lIds[i] = (uint)(i + 1);
                }
                uint[] maIds = new uint[lIds.Length];
                for (int i = 0; i < loopCnt; i++)
                {
                    uint lId = lIds[i];
                    uint maId = vacuumMaId;
                    int hitPMLIndex = -1;
                    for (int pmlIndex = 0; pmlIndex < pmlLIdss.Length; pmlIndex++)
                    {
                        uint[] pmlLIds = pmlLIdss[pmlIndex];
                        uint[] pmlCoreLIds = pmlCoreLIdss[pmlIndex];
                        uint[] pmlCladdingLIds = pmlCladdingLIdss[pmlIndex];
                        if (pmlLIds.Contains(lId))
                        {
                            hitPMLIndex = pmlIndex;
                            if (pmlCoreLIds.Contains(lId))
                            {
                                maId = pmlCoreMaIds[hitPMLIndex];
                            }
                            else if (pmlCladdingLIds.Contains(lId))
                            {
                                maId = pmlCladdingMaIds[hitPMLIndex];
                            }
                            else
                            {
                                maId = pmlVacuumMaIds[hitPMLIndex];
                            }
                            break;
                        }
                    }
                    if (hitPMLIndex == -1)
                    {
                        if (coreLIds.Contains(lId))
                        {
                            maId = coreMaId;
                        }
                        else if (claddingLIds.Contains(lId))
                        {
                            maId = claddingMaId;
                        }
                        else
                        {
                            maId = vacuumMaId;
                        }
                    }

                    maIds[i] = maId;
                }
                for (int i = 0; i < loopCnt; i++)
                {
                    uint lId = lIds[i];
                    uint maId = maIds[i];
                    world.SetCadLoopMaterial(lId, maId);
                }
            }
            {
                // 入出力面、励振面
                uint[][] portEIdss = { refport1EIds, refport2EIds, portSrcEIds };
                uint[][] portCoreEIdss = { refport1CoreEIds, refport2CoreEIds, portSrcCoreEIds };
                uint[][] portCladdingEIdss = { refport1CladdingEIds, refport2CladdingEIds, portSrcCladdingEIds };
                for (int eIdIndex = 0; eIdIndex < portEIdss.Length; eIdIndex++)
                {
                    uint[] eIds = portEIdss[eIdIndex];
                    IList<uint> portCoreEIds = portCoreEIdss[eIdIndex].ToList();
                    IList<uint> portCladdingEIds = portCladdingEIdss[eIdIndex].ToList();
                    int edgeCnt = eIds.Length;
                    uint[] maIds = new uint[edgeCnt];
                    for (int i = 0; i < edgeCnt; i++)
                    {
                        uint eId = eIds[i];
                        uint maId = vacuumMaId;
                        if (portCoreEIds.Contains(eId))
                        {
                            maId = coreMaId;
                        }
                        else if (portCladdingEIds.Contains(eId))
                        {
                            maId = claddingMaId;
                        }
                        else
                        {
                            maId = vacuumMaId;
                        }
                        maIds[i] = maId;
                    }
                    for (int i = 0; i < edgeCnt; i++)
                    {
                        uint eId = eIds[i];
                        uint maId = maIds[i];
                        world.SetCadEdgeMaterial(eId, maId);
                    }
                }
            }

            {
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                uint[][] _portEIdss = { refport1EIds, refport2EIds, portSrcEIds };
                IList<IList<uint>> portEIdss = new List<IList<uint>>();
                foreach (uint[] _portEIds in _portEIdss)
                {
                    IList<uint> __portEIds = _portEIds.ToList();
                    portEIdss.Add(__portEIds);

                }
                foreach (IList<uint> portEIds in portEIdss)
                {
                    // スカラー
                    PortCondition portCondition = new PortCondition(portEIds, CadElementType.Edge, FieldValueType.Scalar);
                    portConditions.Add(portCondition);
                }
            }
            /*
            uint[] zeroEIds = { };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // スカラー
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.Scalar);
                zeroFixedCads.Add(fixedCad);
            }
            */

            world.MakeElements();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // スカラー
                valueId = world.AddFieldValue(FieldValueType.Scalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
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

            var FEM = new EMWaveguide2DHPlanePMLTDFEM(world);
            FEM.TimeLoopCnt = timeLoopCnt;
            FEM.TimeIndex = 0;
            FEM.TimeStep = timeStep;
            FEM.GaussianType = gaussianType;
            FEM.GaussianT0 = gaussianT0;
            FEM.GaussianTp = gaussianTp;
            FEM.SrcFrequency = srcFreq;
            FEM.StartFrequencyForSMatrix = freq1;
            FEM.EndFrequencyForSMatrix = freq2;
            FEM.IsEigen1DUseDecayParameters = isEigen1DUseDecayParameters.ToList();
            FEM.Eigen1DCladdingEp = eigen1DCladdingEp.ToList();
            FEM.IsTMMode = isTMMode;

            {
                var solver = new IvyFEM.Linear.LapackEquationSolver();
                //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                solver.IsOrderingToBandMatrix = true;
                solver.IsRepeatSolve = true;
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
                //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCG;
                //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.CG;
                //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCG;
                //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                //FEM.Solver = solver;
            }

            if (ChartWindow2 == null)
            {
                ChartWindow2 = new ChartWindow();
                ChartWindow2.Closing += ChartWindow2_Closing;
            }
            {
                ChartWindow chartWin = ChartWindow2;
                chartWin.Owner = mainWindow;
                chartWin.Left = mainWindow.Left + mainWindow.Width;
                chartWin.Top = mainWindow.Top;
                chartWin.Show();
                chartWin.TextBox1.Text = "";
                var model = new PlotModel();
                chartWin.Plot.Model = model;
                model.Title = "hz(t): Time Domain";
                var axis1 = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "t",
                    //Minimum = 0,
                    //Maximum = timeLoopCnt
                };
                var axis2 = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "hz(t)"
                };
                model.Axes.Add(axis1);
                model.Axes.Add(axis2);
                var series1 = new LineSeries
                {
                    Title = "Port1"
                };
                var series2 = new LineSeries
                {
                    Title = "Port2"
                };
                model.Series.Add(series1);
                model.Series.Add(series2);
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();
            }

            for (int iTime = 0; iTime < timeLoopCnt; iTime++)
            {
                // 解く
                FEM.Solve();
                // 時間領域のEz
                double[] Ez = FEM.Ez;

                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, Ez);

                {
                    int timeIndex = FEM.TimeIndex;
                    int nodeCntB1 = FEM.RefTimeEzsss[0][timeIndex].Length;
                    int refNodeIdB1 = nodeCntB1 / 2;
                    int nodeCntB2 = FEM.RefTimeEzsss[1][timeIndex].Length;
                    int refNodeIdB2 = nodeCntB2 / 2;
                    double ezPort1 = FEM.RefTimeEzsss[0][timeIndex][refNodeIdB1];
                    double ezPort2 = FEM.RefTimeEzsss[1][timeIndex][refNodeIdB2];
                    var chartWin = ChartWindow2;
                    var model = chartWin.Plot.Model;
                    var series = model.Series;
                    var series1 = series[0] as LineSeries;
                    var series2 = series[1] as LineSeries;
                    series1.Points.Add(new DataPoint(timeIndex, ezPort1));
                    series2.Points.Add(new DataPoint(timeIndex, ezPort2));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                FEM.TimeIndex++;
            }

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closing += ChartWindow1_Closing;
            }
            {
                ChartWindow chartWin = ChartWindow1;
                chartWin.Owner = mainWindow;
                chartWin.Left = mainWindow.Left + mainWindow.Width;
                chartWin.Top = mainWindow.Top + ChartWindow2.Height;
                chartWin.Show();
                chartWin.TextBox1.Text = "";
                var model = new PlotModel();
                chartWin.Plot.Model = model;
                model.Title = "Waveguide Example";
                var axis1 = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "2Wc√(n1^2 - n2^2) /λ",
                    Minimum = normalizedFreq1,
                    Maximum = normalizedFreq2
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
            }

            // S11、S21の周波数特性
            double[] freqs;
            System.Numerics.Complex[] _freqDomainAmpsInc = freqDomainAmpsInc;
            IList<System.Numerics.Complex[]> freqDomainAmpss;
            IList<System.Numerics.Complex[]> Sss;
            FEM.CalcSParameter(_freqDomainAmpsInc, out freqs, out freqDomainAmpss, out Sss);
            int freqCnt = freqs.Length;
            for (int iFreq = 0; iFreq < freqCnt; iFreq++)
            {
                // 周波数
                double freq = freqs[iFreq];
                // 波長
                double waveLength = Constants.C0 / freq;
                // 規格化周波数
                // 2Wc √(n1^2 - n2^2) / λ
                double normalizedFreq = toNormalizedFreq(waveLength);
                if (normalizedFreq < normalizedFreq1)
                {
                    continue;
                }
                if (normalizedFreq > normalizedFreq2)
                {
                    break;
                }
                // S
                System.Numerics.Complex S11 = Sss[0][iFreq];
                System.Numerics.Complex S21 = Sss[1][iFreq];
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
                var chartWin = ChartWindow1;
                var model = chartWin.Plot.Model;
                var series = model.Series;
                var series1 = series[0] as LineSeries;
                var series2 = series[1] as LineSeries;
                series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();
            }
        }
    }
}
