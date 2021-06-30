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
        public void EMWaveguide3DProblem6(MainWindow mainWindow, uint feOrder)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            /*
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            */
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 40.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            double wa = 1.0;
            double stripWidth = 1.0 * wa;
            double dielectricHeight = 1.0 * wa;
            double waveguideWidth = 5.0 * wa;
            double waveguideHeight = 3.0 * wa;
            double inputLength = 10.0 * wa;
            double disconLength = 12.7 * wa;
            double x1 = inputLength;
            double x2 = x1 + disconLength;
            double x3 = x2 + inputLength;
            double y1 = (waveguideWidth - stripWidth) / 2.0;
            double z1 = dielectricHeight;
            //--------------------------
            double halfW = waveguideWidth / 2.0;
            //--------------------------
            double dielectricEp1 = 12.9;
            double dielectricEp2 = 1.0;
            double sFreq = 0.01;
            double eFreq = 0.3;
            int freqDiv = 50;

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, halfW, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, halfW, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(x1, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(x1, halfW, 0.0));
                pts.Add(new OpenTK.Vector3d(x1, halfW, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(x1, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(x2, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(x2, halfW, 0.0));
                pts.Add(new OpenTK.Vector3d(x2, halfW, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(x2, 0.0, waveguideHeight));

                pts.Add(new OpenTK.Vector3d(x3, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(x3, halfW, 0.0));
                pts.Add(new OpenTK.Vector3d(x3, halfW, waveguideHeight));
                pts.Add(new OpenTK.Vector3d(x3, 0.0, waveguideHeight));

                uint layerCnt = 3;
                bool isMakeRadialLoop = false;
                cad.AddCubeWithMultiLayers(pts, layerCnt, isMakeRadialLoop);
            }
            uint tmpId;
            {
                double[] xs = { 0.0, x1, x2, x3 };
                double y = 0.0;
                double z = z1;
                uint[] eIds = { 4, 12, 20, 28 };

                for (int ix = 0; ix < xs.Length; ix++)
                {
                    tmpId = cad.AddVertex(CadElementType.Edge, eIds[ix], new OpenTK.Vector3d(xs[ix], y, z)).AddVId;
                    System.Diagnostics.Debug.Assert(tmpId != 0);
                }
            }
            {
                double[] xs = { 0.0, x1, x2, x3 };
                double y = halfW;
                double z = z1;
                uint[] eIds = { 2, 10, 18, 26 };

                for (int ix = 0; ix < xs.Length; ix++)
                {
                    tmpId = cad.AddVertex(CadElementType.Edge, eIds[ix], new OpenTK.Vector3d(xs[ix], y, z)).AddVId;
                    System.Diagnostics.Debug.Assert(tmpId != 0);
                }
            }
            ConnectVertexRes tmpCVRes;
            {
                uint[][] vIdss =
                {
                    new uint[] { 17, 18 },
                    new uint[] { 18, 19 },
                    new uint[] { 19, 20 },
                    //
                    new uint[] { 21, 22 },
                    new uint[] { 22, 23 },
                    new uint[] { 23, 24 }
                };
                for (int i = 0; i < vIdss.Length; i++)
                {
                    tmpCVRes = cad.ConnectVertexLine(vIdss[i][0], vIdss[i][1]);
                    System.Diagnostics.Debug.Assert(tmpCVRes.AddEId != 0);
                    System.Diagnostics.Debug.Assert(tmpCVRes.AddLId != 0);
                }
            }
            {
                var eIds = new List<uint> { 9, 10, 34, 11, 12, 30 };
                tmpId = cad.MakeRadialLoop(eIds);
                System.Diagnostics.Debug.Assert(tmpId != 0);
            }
            {
                var eIds = new List<uint> { 17, 18, 35, 19, 20, 31 };
                tmpId = cad.MakeRadialLoop(eIds);
                System.Diagnostics.Debug.Assert(tmpId != 0);
            }
            {
                double[] xs = { 0.0, x1, x2, x3 };
                double y = y1;
                double z = z1;
                uint[] lIds = { 1, 21, 22, 14 };
                for (int ix = 0; ix < xs.Length; ix++)
                {
                    tmpId = cad.AddVertex(CadElementType.Loop, lIds[ix], new OpenTK.Vector3d(xs[ix], y, z)).AddVId;
                    System.Diagnostics.Debug.Assert(tmpId != 0);
                }
            }
            {
                uint[][] vIdss =
                {
                    new uint[] { 17, 25, 21 },
                    new uint[] { 18, 26, 22 },
                    new uint[] { 19, 27, 23 },
                    new uint[] { 20, 28, 24 },
                };
                for (int i = 0; i < vIdss.Length; i++)
                {
                    tmpCVRes = cad.ConnectVertexLine(vIdss[i][0], vIdss[i][1]);
                    System.Diagnostics.Debug.Assert(tmpCVRes.AddEId != 0);

                    tmpCVRes = cad.ConnectVertexLine(vIdss[i][1], vIdss[i][2]);
                    System.Diagnostics.Debug.Assert(tmpCVRes.AddEId != 0);
                    System.Diagnostics.Debug.Assert(tmpCVRes.AddLId != 0);
                }
            }
            {
                var eIds = new List<uint> { 43, 44, 40, 46, 45, 37 };
                tmpId = cad.MakeRadialLoop(eIds);
                System.Diagnostics.Debug.Assert(tmpId != 0);
            }
            {
                var eIds = new List<uint> { 45, 46, 41, 48, 47, 38 };
                tmpId = cad.MakeRadialLoop(eIds);
                System.Diagnostics.Debug.Assert(tmpId != 0);
            }
            {
                var eIds = new List<uint> { 47, 48, 42, 50, 49, 39 };
                tmpId = cad.MakeRadialLoop(eIds);
                System.Diagnostics.Debug.Assert(tmpId != 0);
            }
            {
                double[] xs = { x1 / 2.0, (x1 + x2) / 2.0, (x2 + x3) / 2.0 };
                double y = y1;
                double z = z1;
                uint[] lIds = { 27, 28, 29 };
                for (int ix = 0; ix < xs.Length; ix++)
                {
                    tmpId = cad.AddVertex(CadElementType.Loop, lIds[ix], new OpenTK.Vector3d(xs[ix], y, z)).AddVId;
                    System.Diagnostics.Debug.Assert(tmpId != 0);
                }

            }
            {
                uint[][] vIdss =
                {
                    new uint[] { 25, 29, 26 },
                    new uint[] { 26, 30, 27 },
                    new uint[] { 27, 31, 28 },
                };
                for (int i = 0; i < vIdss.Length; i++)
                {
                    tmpCVRes = cad.ConnectVertexLine(vIdss[i][0], vIdss[i][1]);
                    System.Diagnostics.Debug.Assert(tmpCVRes.AddEId != 0);

                    tmpCVRes = cad.ConnectVertexLine(vIdss[i][1], vIdss[i][2]);
                    System.Diagnostics.Debug.Assert(tmpCVRes.AddEId != 0);
                    System.Diagnostics.Debug.Assert(tmpCVRes.AddLId != 0);
                }
            }

            {
                IList<uint> lIds1 = new List<uint> {
                    2, 23, 18, 24, 5, 27, 30
                };
                IList<OpenTK.Vector3d> holes1 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds1 = new List<uint>();
                uint sId1 = cad.AddSolid(lIds1, holes1, insideVIds1);

                IList<uint> lIds2 = new List<uint> {
                    6, 24, 19, 25, 9, 28, 31
                };
                IList<OpenTK.Vector3d> holes2 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds2 = new List<uint>();
                uint sId2 = cad.AddSolid(lIds2, holes2, insideVIds2);

                IList<uint> lIds3 = new List<uint> {
                    10, 25, 20, 14, 13, 29, 32
                };
                IList<OpenTK.Vector3d> holes3 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds3 = new List<uint>();
                uint sId3 = cad.AddSolid(lIds3, holes3, insideVIds3);

                //
                IList<uint> lIds4 = new List<uint> {
                    27, 30, 1, 3, 21, 15, 4
                };
                IList<OpenTK.Vector3d> holes4 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds4 = new List<uint>();
                uint sId4 = cad.AddSolid(lIds4, holes4, insideVIds4);

                IList<uint> lIds5 = new List<uint> {
                    28, 31, 21, 7, 22, 16, 8
                };
                IList<OpenTK.Vector3d> holes5 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds5 = new List<uint>();
                uint sId5 = cad.AddSolid(lIds5, holes5, insideVIds5);

                IList<uint> lIds6 = new List<uint> {
                    29, 32, 22, 11, 26, 17, 12
                };
                IList<OpenTK.Vector3d> holes6 = new List<OpenTK.Vector3d>();
                IList<uint> insideVIds6 = new List<uint>();
                uint sId6 = cad.AddSolid(lIds6, holes6, insideVIds6);
            }

            /*
            // 電気壁
            // Notice:対称面には磁気壁を置く
            cad.SetLoopColor(2, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(4, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(15, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(5, new double[] { 0.0, 0.0, 0.0 });

            cad.SetLoopColor(6, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(8, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(16, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(9, new double[] { 0.0, 0.0, 0.0 });

            cad.SetLoopColor(10, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(12, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(17, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(13, new double[] { 0.0, 0.0, 0.0 });
            // strip
            cad.SetLoopColor(30, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(31, new double[] { 0.0, 0.0, 0.0 });
            cad.SetLoopColor(32, new double[] { 0.0, 0.0, 0.0 });
            // ポート
            cad.SetLoopColor(23, new double[] { 1.0, 0.0, 0.0 });
            cad.SetLoopColor(1, new double[] { 1.0, 0.0, 0.0 });

            cad.SetEdgeColor(1, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(2, new double[] { 0.0, 0.0, 1.0 });

            cad.SetLoopColor(14, new double[] { 1.0, 0.0, 0.0 });
            cad.SetLoopColor(26, new double[] { 1.0, 0.0, 0.0 });

            cad.SetEdgeColor(25, new double[] { 0.0, 0.0, 1.0 });
            cad.SetEdgeColor(26, new double[] { 0.0, 0.0, 1.0 });
            // check
            {
                IList<uint> lIds = new List<uint> {
                    30, 31, 32
                };
                foreach (uint lId in lIds)
                {
                    System.Diagnostics.Debug.WriteLine("##{0}", lId);
                    for (LoopEdgeItr itr = cad.GetLoopEdgeItr(lId); !itr.IsChildEnd; itr.ShiftChildLoop())
                    {
                        for (itr.Begin(); !itr.IsEnd(); itr.Next())
                        {
                            uint vId = itr.GetVertexId();
                            uint eId0;
                            bool isSameDir0;
                            itr.GetEdgeId(out eId0, out isSameDir0);
                            System.Diagnostics.Debug.WriteLine("vId:{0} eId0:{1}", vId, eId0);
                        }
                    }
                }
            }
            */

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Cad3DDrawer(cad);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            WPFUtils.DoEvents();

            double eLen = 0.0;
            if (feOrder == 1)
            {
                //eLen = 0.15 * waveguideWidth;
                eLen = 0.15 * waveguideWidth;
            }
            else if (feOrder == 2)
            {
                eLen = 0.30 * waveguideWidth;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            Mesher3D mesher = new Mesher3D(cad, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            var drawerArray1 = mainWindow.DrawerArray;
            drawerArray1.Clear();
            var drawer1 = new Mesher3DDrawer(mesher);
            drawer1.IsMask = true;
            mainWindow.DrawerArray.Add(drawer1);
            mainWindow.Camera.Fit(drawerArray1.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher;
            uint quantityId;
            uint scalarQuantityId; // ポートの固有値問題にスカラー節点が必要
            {
                uint dof1 = 1; // スカラー
                uint dof2 = 1;
                uint feOrder1 = feOrder;
                uint feOrder2 = feOrder;
                quantityId = world.AddQuantity(dof1, feOrder1, FiniteElementType.Edge);
                scalarQuantityId = world.AddQuantity(dof2, feOrder2, FiniteElementType.ScalarLagrange);
            }

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
                DielectricMaterial dielectricMa1 = new DielectricMaterial
                {
                    Epxx = dielectricEp1,
                    Epyy = dielectricEp1,
                    Epzz = dielectricEp1,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                DielectricMaterial dielectricMa2 = new DielectricMaterial
                {
                    Epxx = dielectricEp2,
                    Epyy = dielectricEp2,
                    Epzz = dielectricEp2,
                    Muxx = 1.0,
                    Muyy = 1.0,
                    Muzz = 1.0
                };
                uint maId1 = world.AddMaterial(vacuumMa);
                uint maId2 = world.AddMaterial(dielectricMa1);
                uint maId3 = world.AddMaterial(dielectricMa2);

                // solid
                uint sId1 = 1;
                world.SetCadSolidMaterial(sId1, maId2);
                uint sId2 = 2;
                world.SetCadSolidMaterial(sId2, maId3);
                uint sId3 = 3;
                world.SetCadSolidMaterial(sId3, maId2);
                uint sId4 = 4;
                world.SetCadSolidMaterial(sId4, maId1);
                uint sId5 = 5;
                world.SetCadSolidMaterial(sId5, maId3);
                uint sId6 = 6;
                world.SetCadSolidMaterial(sId6, maId1);

                // port1
                uint lId11 = 23;
                uint lId12 = 1;
                world.SetCadLoopMaterial(lId11, maId2);
                world.SetCadLoopMaterial(lId12, maId1);

                // port2
                uint lId21 = 14;
                uint lId22 = 26;
                world.SetCadLoopMaterial(lId21, maId2);
                world.SetCadLoopMaterial(lId22, maId1);
            }

            {
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
                uint[][] lIdss = { new uint[] { 23, 1 }, new uint[] { 14, 26 } };
                uint[][] dirEIdss = { new uint[] { 1, 2 }, new uint[] { 25, 26 } };
                IList<IList<uint>> portLIdss = new List<IList<uint>>();
                foreach (uint[] lIds in lIdss)
                {
                    IList<uint> portLIds = new List<uint>();
                    foreach (uint lId in lIds)
                    {
                        portLIds.Add(lId);
                    }
                    portLIdss.Add(portLIds);
                }
                for (int portId = 0; portId < portLIdss.Count; portId++)
                {
                    IList<uint> portLIds = portLIdss[portId];
                    uint[] dirEIds = dirEIdss[portId];
                    PortCondition portCondition = new PortCondition(portLIds, CadElementType.Loop, FieldValueType.ZScalar);
                    portCondition.IntAdditionalParameters = new int[] { (int)dirEIds[0], (int)dirEIds[1] };
                    portConditions.Add(portCondition);
                }
            }
            {
                IList<PortCondition> portConditions = world.GetPortConditions(scalarQuantityId);
                uint[][] lIdss = { new uint[] { 23, 1 }, new uint[] { 14, 26 } };
                IList<IList<uint>> portLIdss = new List<IList<uint>>();
                foreach (uint[] lIds in lIdss)
                {
                    IList<uint> portLIds = new List<uint>();
                    foreach (uint lId in lIds)
                    {
                        portLIds.Add(lId);
                    }
                    portLIdss.Add(portLIds);
                }
                foreach (IList<uint> portLIds in portLIdss)
                {
                    PortCondition portCondition = new PortCondition(portLIds, CadElementType.Loop, FieldValueType.ZScalar);
                    portConditions.Add(portCondition);
                }
            }

            uint[] zeroLIds = 
            {
                2, 4, 15, 5,
                6, 8, 16, 9,
                10, 12, 17, 13,
                30, 31, 32
            };
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            foreach (uint lId in zeroLIds)
            {
                // 複素数(辺方向成分)
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            uint[] scalarZeroLIds =
            {
                2, 4, 15, 5,
                6, 8, 16, 9,
                10, 12, 17, 13,
                30, 31, 32
            };
            var scalarZeroFixedCads = world.GetZeroFieldFixedCads(scalarQuantityId);
            foreach (uint lId in scalarZeroLIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(lId, CadElementType.Loop, FieldValueType.ZScalar);
                scalarZeroFixedCads.Add(fixedCad);
            }

            world.MakeElements();

            uint valueId = 0;
            uint vecValueId = 0;
            VectorFieldDrawer vectorDrawer;
            EdgeFieldDrawer edgeDrawer;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数(辺方向成分)
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.Real);
                // Vector3
                vecValueId = world.AddFieldValue(FieldValueType.ZVector3, FieldDerivativeType.Value,
                    quantityId, true, FieldShowType.ZReal);
                vectorDrawer = new VectorFieldDrawer(
                    vecValueId, FieldDerivativeType.Value, world);
                fieldDrawerArray.Add(vectorDrawer);
                edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
            }
            /*
            ////////////////////////////////////////////////////////////////////////////////////////////////
            // 断面の分布表示
            Cad2D cadA = new Cad2D();
            {
                IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                pts.Add(new OpenTK.Vector2d(halfW, 0.0));
                pts.Add(new OpenTK.Vector2d(halfW, waveguideHeight));
                pts.Add(new OpenTK.Vector2d(0.0, waveguideHeight));
                uint lIdA1 = cadA.AddPolygon(pts).AddLId;
            }
            double eLenA = eLen;
            Mesher2D mesherA = new Mesher2D(cadA, eLenA);

            FEWorld worldA = new FEWorld();
            worldA.Mesh = mesherA;
            uint quantityIdA;
            {
                uint dofA = 1; // スカラー
                uint feOrderA = 1;
                quantityIdA = worldA.AddQuantity(dofA, feOrderA, FiniteElementType.ScalarLagrange);
            }
            {
                // dummy
                DielectricMaterial maA1 = new DielectricMaterial();
                uint maIdA1 = worldA.AddMaterial(maA1);

                uint lIdA1 = 1;
                worldA.SetCadLoopMaterial(lIdA1, maIdA1);
            }

            worldA.MakeElements();

            uint valueIdA = 0;
            uint vecValueIdA = 0;
            VectorFieldDrawer vectorDrawerA;
            FaceFieldDrawer faceDrawerA;
            EdgeFieldDrawer edgeDrawerA;
            var fieldDrawerArrayA = mainWindow.FieldDrawerArrayA;
            {
                worldA.ClearFieldValue();
                // 複素数
                valueIdA = worldA.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityIdA, false, FieldShowType.ZReal);
                // Vector2
                vecValueIdA = worldA.AddFieldValue(FieldValueType.ZVector2, FieldDerivativeType.Value,
                    quantityIdA, true, FieldShowType.ZReal);
                faceDrawerA = new FaceFieldDrawer(valueIdA, FieldDerivativeType.Value, true, worldA,
                    valueIdA, FieldDerivativeType.Value);
                vectorDrawerA = new VectorFieldDrawer(
                    vecValueIdA, FieldDerivativeType.Value, worldA);
                edgeDrawerA = new EdgeFieldDrawer(
                    valueIdA, FieldDerivativeType.Value, true, false, worldA);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////
            */

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
            model.Title = "Waveguide Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "k0 a",
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

            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                fieldDrawerArray.Add(vectorDrawer);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.DoZoom(10, true); //!!!!
                mainWindow.GLControl_ResizeProc();
            }
            /*
            //------------------------------------------
            // 断面
            {
                mainWindow.IsFieldDraw = true;
                fieldDrawerArrayA.Clear();
                fieldDrawerArrayA.Add(faceDrawerA);
                fieldDrawerArrayA.Add(vectorDrawerA);
                fieldDrawerArrayA.Add(edgeDrawerA);
                mainWindow.Camera.Fit(fieldDrawerArrayA.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.GLControl_ResizeProc();
            }
            //------------------------------------------
            */

            for (int iFreq = 0; iFreq < (freqDiv + 1); iFreq++)
            {
                double normalFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                double k0 = normalFreq / wa;
                double omega = k0 * Constants.C0;
                double freq = omega / (2.0 * Math.PI);
                System.Diagnostics.Debug.WriteLine("k0 a: " + normalFreq);

                var FEM = new EMWaveguide3DFEM(world);
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
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] E = FEM.E;
                System.Numerics.Complex[] coordExyz = FEM.CoordExyz;
                System.Numerics.Complex[][] S = FEM.S;

                System.Numerics.Complex S11 = S[0][0];
                System.Numerics.Complex S21 = S[1][0];
                double S11Abs = S11.Magnitude;
                double S21Abs = S21.Magnitude;
                double total = S11Abs * S11Abs + S21Abs * S21Abs;

                string ret;
                string CRLF = System.Environment.NewLine;
                ret = "k0 a: " + normalFreq + CRLF;
                ret += "|S11| = " + S11Abs + CRLF +
                      "|S21| = " + S21Abs + CRLF +
                      "|S11|^2 + |S21|^2 = " + total + CRLF;
                System.Diagnostics.Debug.WriteLine(ret);
                //AlertWindow.ShowDialog(ret, "");
                series1.Points.Add(new DataPoint(normalFreq, S11Abs));
                series2.Points.Add(new DataPoint(normalFreq, S21Abs));
                model.InvalidatePlot(true);
                WPFUtils.DoEvents();

                // Exyzを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = coordExyz.Length;
                    foreach (System.Numerics.Complex value in coordExyz)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 1.0;//0.4;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            coordExyz[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                world.UpdateBubbleFieldValueValuesFromCoordValues(vecValueId, FieldDerivativeType.Value, coordExyz);

                fieldDrawerArray.Update(world);
                mainWindow.GLControl.Invalidate();
                mainWindow.GLControl.Update();
                WPFUtils.DoEvents();

                /*
                ////////////////////////////////////////////////////////////////////////////////////////////////
                // 断面の分布表示
                // 平面の分布を取得する
                System.Numerics.Complex[] UtA;
                System.Numerics.Complex[] UzA;
                int portIdA = 0;
                //int portIdA = 1;
                double sectionXA = 0.0; // 断面の位置
                //double sectionXA = x3; // 断面の位置
                {
                    var eigenFEMA = FEM.EigenFEMs[portIdA];
                    int iModeA = 0;
                    var betaA = eigenFEMA.Betas[iModeA];
                    var etEVecA = eigenFEMA.EtEVecs[iModeA];
                    var ezEVecA = eigenFEMA.EzEVecs[iModeA];

                    uint coCntA = worldA.GetCoordCount(quantityIdA);
                    uint dofA = 2;
                    UtA = new System.Numerics.Complex[coCntA * dofA];
                    UzA = new System.Numerics.Complex[coCntA];
                    for (int coIdA = 0; coIdA < coCntA; coIdA++)
                    {
                        double[] co2DA = worldA.GetCoord(quantityIdA, coIdA);
                        double[] coA = { sectionXA, co2DA[0], co2DA[1] }; // YZ断面
                        System.Numerics.Complex[] value =
                            eigenFEMA.CalcModeCoordExyzByCoord(coA, betaA, etEVecA, ezEVecA);
                        System.Diagnostics.Debug.Assert(value.Length == 3);
                        // x,yのみ抽出
                        for (int iDofA = 0; iDofA < dofA; iDofA++)
                        {
                            UtA[coIdA * dofA + iDofA] = value[iDofA];
                        }
                        // z
                        UzA[coIdA] = (1.0 / System.Numerics.Complex.ImaginaryOne) * value[2];
                    }
                }
                // UAを表示用にスケーリングする
                {
                    double maxValue = 0;
                    int cnt = UtA.Length;
                    foreach (System.Numerics.Complex value in UtA)
                    {
                        double abs = value.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    double maxShowValue = 0.5;//0.2;
                    if (maxValue >= 1.0e-30)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            UtA[i] *= (maxShowValue / maxValue);
                        }
                    }
                }
                {
                    worldA.UpdateBubbleFieldValueValuesFromCoordValues(vecValueIdA, FieldDerivativeType.Value, UtA);
                    worldA.UpdateFieldValueValuesFromCoordValues(valueIdA, FieldDerivativeType.Value, UzA);

                    fieldDrawerArrayA.Update(worldA);
                    mainWindow.GLControl.Invalidate();
                    mainWindow.GLControl.Update();
                    WPFUtils.DoEvents();
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////
                */
            }
        }
    }
}
