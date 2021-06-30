using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using IvyFEM;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Drawing;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void MakeCad3D9(MainWindow mainWindow)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));

                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 1.0));

                pts.Add(new OpenTK.Vector3d(2.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(2.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(2.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(2.0, 0.0, 1.0));

                pts.Add(new OpenTK.Vector3d(3.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(3.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(3.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(3.0, 0.0, 1.0));

                uint layerCnt = 3;
                bool isMakeRadialLoop = false;
                cad.AddCubeWithMultiLayers(pts, layerCnt, isMakeRadialLoop);
            }
            uint tmpId;
            {
                double[] xs = { 0.0, 1.0, 2.0, 3.0 };
                double y = 0.0;
                double z = 0.3;
                uint[] eIds = { 4, 12, 20, 28 };

                for (int ix = 0; ix < xs.Length; ix++)
                {
                    tmpId = cad.AddVertex(CadElementType.Edge, eIds[ix], new OpenTK.Vector3d(xs[ix], y, z)).AddVId;
                    System.Diagnostics.Debug.Assert(tmpId != 0);
                }
            }
            {
                double[] xs = { 0.0, 1.0, 2.0, 3.0 };
                double y = 1.0;
                double z = 0.3;
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
                double[] xs = { 0.0, 1.0, 2.0, 3.0 };
                double y = 0.7;
                double z = 0.3;
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
                double[] xs = { 0.5, 1.5, 2.5 };
                double y = 0.7;
                double z = 0.3;
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
        }

        public void MakeCoarseMesh3D9(MainWindow mainWindow)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));

                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 1.0));

                pts.Add(new OpenTK.Vector3d(2.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(2.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(2.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(2.0, 0.0, 1.0));

                pts.Add(new OpenTK.Vector3d(3.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(3.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(3.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(3.0, 0.0, 1.0));

                uint layerCnt = 3;
                bool isMakeRadialLoop = false;
                cad.AddCubeWithMultiLayers(pts, layerCnt, isMakeRadialLoop);
            }
            uint tmpId;
            {
                double[] xs = { 0.0, 1.0, 2.0, 3.0 };
                double y = 0.0;
                double z = 0.3;
                uint[] eIds = { 4, 12, 20, 28 };

                for (int ix = 0; ix < xs.Length; ix++)
                {
                    tmpId = cad.AddVertex(CadElementType.Edge, eIds[ix], new OpenTK.Vector3d(xs[ix], y, z)).AddVId;
                    System.Diagnostics.Debug.Assert(tmpId != 0);
                }
            }
            {
                double[] xs = { 0.0, 1.0, 2.0, 3.0 };
                double y = 1.0;
                double z = 0.3;
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
                double[] xs = { 0.0, 1.0, 2.0, 3.0 };
                double y = 0.7;
                double z = 0.3;
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
                double[] xs = { 0.5, 1.5, 2.5 };
                double y = 0.7;
                double z = 0.3;
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

            Mesher3D mesher = new Mesher3D(cad);

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Mesher3DDrawer(mesher);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
        }

        public void MakeMesh3D9(MainWindow mainWindow)
        {
            /////////////////////
            Dimension = 3; // 3次元
            var camera3D = mainWindow.Camera as Camera3D;
            OpenTK.Quaterniond q1 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(1.0, 0.0, 0.0), Math.PI * 70.0 / 180.0);
            OpenTK.Quaterniond q2 = OpenTK.Quaterniond.FromAxisAngle(
                new OpenTK.Vector3d(0.0, 1.0, 0.0), Math.PI * 10.0 / 180.0);
            camera3D.RotQuat = q1 * q2;
            /////////////////////

            Cad3D cad = new Cad3D();
            {
                IList<OpenTK.Vector3d> pts = new List<OpenTK.Vector3d>();
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(0.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(0.0, 0.0, 1.0));

                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(1.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(1.0, 0.0, 1.0));

                pts.Add(new OpenTK.Vector3d(2.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(2.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(2.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(2.0, 0.0, 1.0));

                pts.Add(new OpenTK.Vector3d(3.0, 0.0, 0.0));
                pts.Add(new OpenTK.Vector3d(3.0, 1.0, 0.0));
                pts.Add(new OpenTK.Vector3d(3.0, 1.0, 1.0));
                pts.Add(new OpenTK.Vector3d(3.0, 0.0, 1.0));

                uint layerCnt = 3;
                bool isMakeRadialLoop = false;
                cad.AddCubeWithMultiLayers(pts, layerCnt, isMakeRadialLoop);
            }
            uint tmpId;
            {
                double[] xs = { 0.0, 1.0, 2.0, 3.0 };
                double y = 0.0;
                double z = 0.3;
                uint[] eIds = { 4, 12, 20, 28 };

                for (int ix = 0; ix < xs.Length; ix++)
                {
                    tmpId = cad.AddVertex(CadElementType.Edge, eIds[ix], new OpenTK.Vector3d(xs[ix], y, z)).AddVId;
                    System.Diagnostics.Debug.Assert(tmpId != 0);
                }
            }
            {
                double[] xs = { 0.0, 1.0, 2.0, 3.0 };
                double y = 1.0;
                double z = 0.3;
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
                double[] xs = { 0.0, 1.0, 2.0, 3.0 };
                double y = 0.7;
                double z = 0.3;
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
                double[] xs = { 0.5, 1.5, 2.5 };
                double y = 0.7;
                double z = 0.3;
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

            /*
            // check
            {
                IList<uint> lIds = new List<uint> {
                    29, 32, 22, 11, 26, 17, 12
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

            //double eLen = 0.05;
            double eLen = 0.30;
            Mesher3D mesher = new Mesher3D(cad, eLen);

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            var drawer = new Mesher3DDrawer(mesher);
            drawer.IsMask = true;
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.GLControl_ResizeProc();
            mainWindow.GLControl.Invalidate();
            mainWindow.GLControl.Update();
        }
    }
}
