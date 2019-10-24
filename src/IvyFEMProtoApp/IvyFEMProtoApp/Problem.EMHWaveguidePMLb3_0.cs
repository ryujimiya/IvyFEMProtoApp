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
        // Open Waveguide
        public void HWaveguidePMLProblemb3_0(MainWindow mainWindow)
        {
            double waveguideWidth = 1.0;
            //double eLen = waveguideWidth * (0.95 * 1.0 / 30.0);
            //double eLen = waveguideWidth * (0.95 * 1.0 / 30.0);
            double eLen = waveguideWidth * (0.95 * 0.75 / 30.0);

            // 誘電体スラブ導波路幅
            double coreWidth = waveguideWidth * (4.0 / 30.0);
            // 誘電体スラブ比誘電率
            double coreEps = 3.6 * 3.6;
            double claddingEps = 3.24 * 3.24;
            double replacedMu0 = Constants.Ep0; // TMモード
            bool isTMMode = true; // TMモード

            // 形状設定で使用する単位長さ
            double unitLen = waveguideWidth / 20.0;
            // PML層の厚さ
            double pmlXThickness = 10 * unitLen;
            double pmlYThickness = 5 * unitLen;
            // 導波管不連続領域の長さ
            double disconLength = 1.0 * waveguideWidth + 2.0 * pmlXThickness;
            // PML位置
            double port1PMLPosX = pmlXThickness;
            double port2PMLPosX = disconLength - pmlXThickness;

            // 計算する周波数領域
            double sFreq = 0.50;
            double eFreq = 1.00;
            int freqDiv = 25;

            // 1D固有値問題で減衰定数を使う?
            bool[] isEigen1DUseDecayParameters = {
                true,
                true,
                true
            };
            // 1D固有値問題のクラッド比誘電率
            double[] eigen1DCladdingEps = {
                claddingEps,
                claddingEps,
                claddingEps
            };

            const int portCnt = 2;
            double waveguideWidthWithPML = waveguideWidth + 2.0 * pmlYThickness;
            double posY1 = pmlYThickness;
            double posY2 = waveguideWidthWithPML - pmlYThickness;
            double coreY1 = posY1 + (waveguideWidth - coreWidth) * 0.5;
            double coreY2 = coreY1 + coreWidth;
            uint loopCnt = 15;
            uint[] pmlLIds1 = { 5, 6, 7 };
            uint[] pmlCoreLIds1 = { 6 };
            uint[] pmlCladdingLIds1 = { 5, 7 };
            uint[] pmlLIds2 = { 13, 14, 15 };
            uint[] pmlCoreLIds2 = { 14 };
            uint[] pmlCladdingLIds2 = { 13, 15 };
            uint[] pmlLIds3 = { 8 };
            uint[] pmlCoreLIds3 = { };
            uint[] pmlCladdingLIds3 = { 8 };
            uint[] pmlLIds4 = { 2 };
            uint[] pmlCoreLIds4 = { };
            uint[] pmlCladdingLIds4 = { 2 };
            uint[] pmlLIds5 = { 4 };
            uint[] pmlCoreLIds5 = { };
            uint[] pmlCladdingLIds5 = { 4 };
            uint[] pmlLIds6 = { 12 };
            uint[] pmlCoreLIds6 = { };
            uint[] pmlCladdingLIds6 = { 12 };
            uint[] pmlLIds7 = { 1 };
            uint[] pmlCoreLIds7 = { };
            uint[] pmlCladdingLIds7 = { 1 };
            uint[] pmlLIds8 = { 3 };
            uint[] pmlCoreLIds8 = { };
            uint[] pmlCladdingLIds8 = { 3 };
            uint[][] pmlLIdss = { pmlLIds1, pmlLIds2, pmlLIds3, pmlLIds4, pmlLIds5, pmlLIds6, pmlLIds7, pmlLIds8 };
            uint[][] pmlCoreLIdss = {
                pmlCoreLIds1, pmlCoreLIds2, pmlCoreLIds3, pmlCoreLIds4,
                pmlCoreLIds5, pmlCoreLIds6, pmlCoreLIds7, pmlCoreLIds8
            };
            uint[][] pmlCladdingLIdss = {
                pmlCladdingLIds1, pmlCladdingLIds2, pmlCladdingLIds3, pmlCladdingLIds4,
                pmlCladdingLIds5, pmlCladdingLIds6, pmlCladdingLIds7, pmlCladdingLIds8
            };
            uint[] coreLIds = { 10 };
            uint[] claddingLIds = { 9, 11 };
            uint[] refport1EIds = { 18, 17, 16 };
            uint[] refport1CoreEIds = { 17 };
            uint[] refport1CladdingEIds = { 18, 16 };
            uint[] refport2EIds = { 22, 21, 20 };
            uint[] refport2CoreEIds = { 21 };
            uint[] refport2CladdingEIds = { 22, 20 };
            uint[] portSrcEIds = refport1EIds;
            uint[] portSrcCoreEIds = refport1CoreEIds;
            uint[] portSrcCladdingEIds = refport1CladdingEIds;
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
            CadObject2D cad2D = new CadObject2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(port1PMLPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(port2PMLPosX, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, 0.0));
                pts.Add(new OpenTK.Vector2d(disconLength, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(port2PMLPosX, waveguideWidthWithPML));
                pts.Add(new OpenTK.Vector2d(port1PMLPosX, waveguideWidthWithPML));
                uint _lId1 = cad2D.AddPolygon(pts).AddLId;
                uint _lId2 = cad2D.ConnectVertexLine(3, 8).AddLId;
                uint _lId3 = cad2D.ConnectVertexLine(4, 7).AddLId;
            }
            // スラブ導波路と境界の交点
            {
                // 入出力面と参照面、励振面
                double[] portXs = { 0.0, port1PMLPosX, port2PMLPosX, disconLength };
                uint[] parentEIds = { 1, 9, 10, 5 };
                IList<uint[]> slabVIds = new List<uint[]>();
                for (int index = 0; index < portXs.Length; index++)
                {
                    double portX = portXs[index];
                    uint parentEId = parentEIds[index];

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
                    uint vId1 = cad2D.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY1)).AddVId;
                    uint vId2 = cad2D.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY2)).AddVId;
                    uint vId3 = cad2D.AddVertex(
                        CadElementType.Edge, parentEId, new OpenTK.Vector2d(portX, workY3)).AddVId;
                    uint vId4 = cad2D.AddVertex(
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
                    slabVIds.Add(workVIds);
                }
                // スラブ導波路
                {
                    for (int portIndex = 0; portIndex < (portXs.Length - 1); portIndex++)
                    {
                        int vIdCnt = slabVIds[portIndex].Length;
                        for (int i = 0; i < vIdCnt; i++)
                        {
                            uint workVId1 = slabVIds[portIndex][i];
                            uint workVId2 = slabVIds[portIndex + 1][i];
                            uint workEId = cad2D.ConnectVertexLine(workVId1, workVId2).AddEId;
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
                            cad2D.SetLoopColor(lId, pmlCoreColor);
                        }
                        else if (pmlCladdingLIds.Contains(lId))
                        {
                            cad2D.SetLoopColor(lId, pmlCladdingColor);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                            //cad2D.SetLoopColor(lId, pmlColor);
                        }
                    }
                }

                double[] coreColor = { 0.6, 0.0, 0.0 };
                foreach (uint lId in coreLIds)
                {
                    cad2D.SetLoopColor(lId, coreColor);
                }
                double[] edgeCoreColor = { 0.4, 0.0, 0.0 };
                uint[][] portCoreEIdss = { refport1CoreEIds, refport2CoreEIds, portSrcCoreEIds };
                foreach (uint[] eIds in portCoreEIdss)
                {
                    foreach (uint eId in eIds)
                    {
                        cad2D.SetEdgeColor(eId, edgeCoreColor);
                    }
                }
                double[] claddingColor = { 0.5, 1.0, 0.5 };
                foreach (uint lId in claddingLIds)
                {
                    cad2D.SetLoopColor(lId, claddingColor);
                }
                double[] edgeCladdingColor = { 0.3, 0.8, 0.3 };
                uint[][] portCladdingEIdss = { refport1CladdingEIds, refport2CladdingEIds, portSrcCladdingEIds };
                foreach (uint[] eIds in portCladdingEIdss)
                {
                    foreach (uint eId in eIds)
                    {
                        cad2D.SetEdgeColor(eId, edgeCladdingColor);
                    }
                }
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            //Mesher2D mesher2D = new Mesher2D(cad2D, eLen);
            Mesher2D mesher2D = new Mesher2D();
            mesher2D.SetMeshingModeElemLength();
            for (int i = 0; i < loopCnt; i++)
            {
                uint lId = (uint)(i + 1);
                double workeLen = eLens[i];
                mesher2D.AddCutMeshLoopCadId(lId, workeLen);
            }
            mesher2D.Meshing(cad2D);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            IDrawer meshDrawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 1; // 複素数
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint vacuumMaId = 0;
            uint claddingMaId = 0;
            uint coreMaId = 0;
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
                // Note: TMモード(比誘電率は比透磁率のところにセットする)
                DielectricMaterial claddingMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps
                };
                DielectricMaterial coreMa = new DielectricMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = coreEps,
                    Muyy = coreEps,
                    Muzz = coreEps
                };
                System.Diagnostics.Debug.Assert(isTMMode); // TMモード
                DielectricPMLMaterial pmlCladdingMa1 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, 0.0),
                    XThickness = pmlXThickness,
                    YThickness = 0.0,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCoreMa1 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = coreEps,
                    Muyy = coreEps,
                    Muzz = coreEps,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, 0.0),
                    XThickness = pmlXThickness,
                    YThickness = 0.0,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCladdingMa2 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLPosX, 0.0),
                    XThickness = pmlXThickness,
                    YThickness = 0.0,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCoreMa2 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = coreEps,
                    Muyy = coreEps,
                    Muzz = coreEps,
                    // X方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLPosX, 0.0),
                    XThickness = pmlXThickness,
                    YThickness = 0.0,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCladdingMa3 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps,
                    // Y方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY1),
                    XThickness = 0.0,
                    YThickness = pmlYThickness,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCoreMa3 = null;
                DielectricPMLMaterial pmlCladdingMa4 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps,
                    // Y方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY2),
                    XThickness = 0.0,
                    YThickness = pmlYThickness,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCoreMa4 = null;
                DielectricPMLMaterial pmlCladdingMa5 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps,
                    // XY方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY1),
                    XThickness = pmlXThickness,
                    YThickness = pmlYThickness,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCoreMa5 = null;
                DielectricPMLMaterial pmlCladdingMa6 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps,
                    // XY方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLPosX, posY1),
                    XThickness = pmlXThickness,
                    YThickness = pmlYThickness,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCoreMa6 = null;
                DielectricPMLMaterial pmlCladdingMa7 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps,
                    // XY方向PML
                    OriginPoint = new OpenTK.Vector2d(port1PMLPosX, posY2),
                    XThickness = pmlXThickness,
                    YThickness = pmlYThickness,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCoreMa7 = null;
                DielectricPMLMaterial pmlCladdingMa8 = new DielectricPMLMaterial
                {
                    Epxx = 1.0,
                    Epyy = 1.0,
                    Epzz = 1.0,
                    Muxx = claddingEps,
                    Muyy = claddingEps,
                    Muzz = claddingEps,
                    // XY方向PML
                    OriginPoint = new OpenTK.Vector2d(port2PMLPosX, posY2),
                    XThickness = pmlXThickness,
                    YThickness = pmlYThickness,
                    IsTMMode = isTMMode
                };
                DielectricPMLMaterial pmlCoreMa8 = null;

                vacuumMaId = world.AddMaterial(vacuumMa);
                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

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
                    var pmlCladdingMa = pmlCladdingMas[pmlIndex];
                    var pmlCoreMa = pmlCoreMas[pmlIndex];
                    uint pmlCladdingMaId = world.AddMaterial(pmlCladdingMa);
                    pmlCladdingMaIds.Add(pmlCladdingMaId);
                    uint pmlCoreMaId = world.AddMaterial(pmlCoreMa);
                    pmlCoreMaIds.Add(pmlCoreMaId);
                }

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
                                System.Diagnostics.Debug.Assert(false);
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
                            System.Diagnostics.Debug.Assert(false);
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
                            System.Diagnostics.Debug.Assert(false);
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
                    // 複素数
                    PortCondition portCondition = new PortCondition(portEIds, FieldValueType.ZScalar);
                    portConditions.Add(portCondition);
                }

                // 入射ポート、モード
                int incidentPortId = 0;
                world.SetIncidentPortId(quantityId, incidentPortId);
                world.SetIncidentModeId(quantityId, 0);
            }
            /*
            uint[] zeroEIds = { };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            */

            world.MakeElements();

            if (ChartWindow1 == null)
            {
                ChartWindow1 = new ChartWindow();
                ChartWindow1.Closed += ChartWindow1_Closed;
            }
            ChartWindow chartWin = ChartWindow1;
            chartWin.Owner = mainWindow;
            chartWin.Left = mainWindow.Left + mainWindow.Width;
            chartWin.Top = mainWindow.Top;
            chartWin.Show();
            var model = new PlotModel();
            chartWin.Plot.Model = model;
            model.Title = "Waveguide Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "2Wc√(n1^2 - n2^2)/λ",
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
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
                //mainWindow.GLControl.Invalidate();
                //mainWindow.GLControl.Update();
                //WPFUtils.DoEvents();
            }

            for (int iFreq = 0; iFreq < freqDiv + 1; iFreq++)
            {
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波長
                double waveLength = 2.0 * coreWidth * Math.Sqrt(coreEps - claddingEps) / normalizedFreq;
                // 周波数
                double freq = Constants.C0 / waveLength;
                // 角周波数
                double omega = 2.0 * Math.PI * freq;
                // 波数
                double k0 = omega / Constants.C0;
                System.Diagnostics.Debug.WriteLine("2Wc √(n1^2 - n2^2) / λ: " + normalizedFreq);

                var FEM = new EMWaveguide2DHPlanePMLFEM(world);
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
                FEM.IsEigen1DUseDecayParameters = isEigen1DUseDecayParameters.ToList();
                FEM.Eigen1DCladdingEps = eigen1DCladdingEps.ToList();
                FEM.ReplacedMu0 = replacedMu0;
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
