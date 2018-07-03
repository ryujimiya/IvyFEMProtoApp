using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class Mesher2DDrawPart
    {
        public bool IsSelected { get; set; } = false;
        public bool IsShown { get; set; } = true;
        public IList<uint> SelectedElems { get; set; } = new List<uint>();
        public uint MshId { get; set; } = 0;
        public uint CadId { get; set; } = 0;
        public double[] Color { get; } = new double[3] { 0.8f, 0.8f, 0.8f };
        public uint LineWidth { get; set; } = 1;
        public uint NElem { get; set; } = 0;
        public int[] ElemIndexs { get; set; } = null;
        public uint NEdge { get; set; } = 0;
        public int[] EdgeIndexs { get; set; } = null;
        public double Height { get; set; } = 0;

        private uint ElemType; // bar:1，tri:2，quad:3, tet:4, hex:5

        public Mesher2DDrawPart()
        {

        }

        public Mesher2DDrawPart(Mesher2DDrawPart src)
        {
            IsSelected = src.IsSelected;
            IsShown = src.IsShown;
            SelectedElems = new List<uint>(src.SelectedElems);
            MshId = src.MshId;
            CadId = src.CadId;
            for (int i = 0; i < 3; i++)
            {
                Color[i] = src.Color[i];
            }
            LineWidth = src.LineWidth;
            NElem = src.NElem;
            ElemIndexs = new int[src.EdgeIndexs.Length];
            src.ElemIndexs.CopyTo(ElemIndexs, 0);
            NEdge = src.NEdge;
            EdgeIndexs = new int[src.EdgeIndexs.Length];
            src.EdgeIndexs.CopyTo(EdgeIndexs, 0);
            Height = src.Height;
            ElemType = src.ElemType;
        }

        public Mesher2DDrawPart(QuadArray2D quadArray)
        {
            IsSelected = false;
            IsShown = true;
            Color[0] = 0.8;
            Color[1] = 0.8;
            Color[2] = 0.8;
            LineWidth = 1;
            Height = 0;
            CadId = quadArray.LCadId;
            MshId = quadArray.Id;
            System.Diagnostics.Debug.Assert(MshId != 0);
            ElemType = 3;
            ElemIndexs = null;
            EdgeIndexs = null;

            NElem = (uint)quadArray.Quads.Count;
            {
                // 面のセット
                ElemIndexs = new int[NElem * 4];
                for (int iquad = 0; iquad < NElem; iquad++)
                {
                    ElemIndexs[iquad * 4 + 0] = (int)quadArray.Quads[iquad].V[0];
                    ElemIndexs[iquad * 4 + 1] = (int)quadArray.Quads[iquad].V[1];
                    ElemIndexs[iquad * 4 + 2] = (int)quadArray.Quads[iquad].V[2];
                    ElemIndexs[iquad * 4 + 3] = (int)quadArray.Quads[iquad].V[3];
                }
            }
            {
                // 辺のセット
                NEdge = NElem * 4;
                EdgeIndexs = new int[NEdge * 2];
                for (int iquad = 0; iquad < NElem; iquad++)
                {
                    EdgeIndexs[(iquad * 4) * 2 + 0] = (int)quadArray.Quads[iquad].V[0];
                    EdgeIndexs[(iquad * 4) * 2 + 1] = (int)quadArray.Quads[iquad].V[1];

                    EdgeIndexs[(iquad * 4 + 1) * 2 + 0] = (int)quadArray.Quads[iquad].V[1];
                    EdgeIndexs[(iquad * 4 + 1) * 2 + 1] = (int)quadArray.Quads[iquad].V[2];

                    EdgeIndexs[(iquad * 4 + 2) * 2 + 0] = (int)quadArray.Quads[iquad].V[2];
                    EdgeIndexs[(iquad * 4 + 2) * 2 + 1] = (int)quadArray.Quads[iquad].V[3];

                    EdgeIndexs[(iquad * 4 + 3) * 2 + 0] = (int)quadArray.Quads[iquad].V[3];
                    EdgeIndexs[(iquad * 4 + 3) * 2 + 1] = (int)quadArray.Quads[iquad].V[0];
                }
            }
        }

        public Mesher2DDrawPart(TriArray2D triArray)
        {
            IsSelected = false;
            IsShown = true;
            Color[0] = 0.8;
            Color[1] = 0.8;
            Color[2] = 0.8;
            LineWidth = 1;
            Height = 0;
            ElemIndexs = null;
            EdgeIndexs = null;

            CadId = triArray.LCadId;

            MshId = triArray.Id;
            System.Diagnostics.Debug.Assert(MshId != 0);
            ElemType = 2;

            NElem = (uint)triArray.Tris.Count;

            {
                // 面のセット
                ElemIndexs = new int[NElem * 3];
                for (int itri = 0; itri < NElem; itri++)
                {
                    ElemIndexs[itri * 3 + 0] = (int)triArray.Tris[itri].V[0];
                    ElemIndexs[itri * 3 + 1] = (int)triArray.Tris[itri].V[1];
                    ElemIndexs[itri * 3 + 2] = (int)triArray.Tris[itri].V[2];
                }
            }

            {   
                // 辺のセット
                NEdge = NElem * 3;
                EdgeIndexs = new int[NEdge * 2];
                for (int itri = 0; itri < NElem; itri++)
                {
                    EdgeIndexs[(itri * 3) * 2 + 0] = (int)triArray.Tris[itri].V[0];
                    EdgeIndexs[(itri * 3) * 2 + 1] = (int)triArray.Tris[itri].V[1];

                    EdgeIndexs[(itri * 3 + 1) * 2 + 0] = (int)triArray.Tris[itri].V[1];
                    EdgeIndexs[(itri * 3 + 1) * 2 + 1] = (int)triArray.Tris[itri].V[2];

                    EdgeIndexs[(itri * 3 + 2) * 2 + 0] = (int)triArray.Tris[itri].V[2];
                    EdgeIndexs[(itri * 3 + 2) * 2 + 1] = (int)triArray.Tris[itri].V[0];
                }
            }
        }

        public Mesher2DDrawPart(BarArray barArray)
        {
            IsSelected = false;
            IsShown = true;
            Color[0] = 0.8;
            Color[1] = 0.8;
            Color[2] = 0.8;
            LineWidth = 1;
            Height = 0;
            ElemIndexs = null;
            EdgeIndexs = null;

            CadId = barArray.ECadId;

            MshId = barArray.Id;
            System.Diagnostics.Debug.Assert(MshId != 0);
            ElemType = 1;

            NElem = (uint)barArray.Bars.Count;
            ElemIndexs = new int[NElem * 2];
            for (int ibar = 0; ibar < NElem; ibar++)
            {
                ElemIndexs[ibar * 2 + 0] = (int)barArray.Bars[ibar].V[0];
                ElemIndexs[ibar * 2 + 1] = (int)barArray.Bars[ibar].V[1];
            }
        }

        public void DrawElements()
        {
            if (ElemType == 1)
            {
                DrawElementsBar();
            }
            else if (ElemType == 2)
            {
                DrawElementsTri();
            }
            else if (ElemType == 3)
            {
                DrawElementsQuad();
            }
            else if (ElemType == 4)
            {
                new NotImplementedException();
            }
            else if (ElemType == 5)
            {
                new NotImplementedException();
            }
        }

        public void DrawElementsSelection()
        {
            if (ElemType == 1)
            {
                DrawElementsSelectionBar();
            }
            else if (ElemType == 2)
            {
                DrawElementsSelectionTri();
            }
            else if (ElemType == 3)
            {
                DrawElementsSelectionQuad();
            }
            else if (ElemType == 4)
            {
                new NotImplementedException();
            }
            else if (ElemType == 5)
            {
                new NotImplementedException();
            }
        }

        public uint GetElemDim()
        {
            if (ElemType == 1)
            {
                return 1;
            }
            else if (ElemType == 2)
            {
                return 2;
            }
            else if (ElemType == 3)
            {
                return 2;
            }
            else if (ElemType == 4)
            {
                return 3;
            }
            else if (ElemType == 5)
            {
                return 3;
            }
            return 0;
        }

        private void DrawElementsQuad()
        {
            if (!IsShown)
            {
                return;
            }

            // 辺を描画
            GL.LineWidth(1);
            /*
            if (IsSelected)
            {
                GL.LineWidth(2);
                GL.Color3(1.0, 1.0, 0.0);
            }
            else
            {
                GL.LineWidth(1);
                GL.Color3(0.0, 0.0, 0.0);
            }
            */
            GL.Color3(0.0, 0.0, 0.0);
            GL.DrawElements(BeginMode.Lines, (int)NEdge * 2, DrawElementsType.UnsignedInt, EdgeIndexs);

            GL.Color3(1.0, 0.0, 0.0);
            GL.Begin(BeginMode.Quads);
            for (int iielem = 0; iielem < SelectedElems.Count; iielem++)
            {
                uint ielem0 = SelectedElems[iielem];

                GL.ArrayElement(ElemIndexs[ielem0 * 4]);
                GL.ArrayElement(ElemIndexs[ielem0 * 4 + 1]);
                GL.ArrayElement(ElemIndexs[ielem0 * 4 + 2]);
                GL.ArrayElement(ElemIndexs[ielem0 * 4 + 3]);
            }
            GL.End();

            // 面を描画
            GL.Color3(Color);
            GL.DrawElements(BeginMode.Quads, (int)NElem * 4, DrawElementsType.UnsignedInt, ElemIndexs);
        }

        private void DrawElementsSelectionQuad()
        {
            if (!IsShown)
            {
                return;
            }

            // 面を描画
            for (int iquad = 0; iquad < NElem; iquad++)
            {
                GL.PushName(iquad);
                GL.Begin(BeginMode.Quads);

                GL.ArrayElement(ElemIndexs[iquad * 4]);
                GL.ArrayElement(ElemIndexs[iquad * 4 + 1]);
                GL.ArrayElement(ElemIndexs[iquad * 4 + 2]);
                GL.ArrayElement(ElemIndexs[iquad * 4 + 3]);

                GL.End();
                GL.PopName();
            }
        }

        private void DrawElementsTri()
        {
            if (!IsShown)
            {
                return;
            }

            // 辺を描画
            /*
            if (IsSelected)
            {
                GL.LineWidth(LineWidth + 1);
                GL.Color3(1.0, 1.0, 0.0);
            }
            else
            {
                GL.LineWidth(LineWidth);
                GL.Color3(0.0, 0.0, 0.0);
            }
            */
            GL.LineWidth(LineWidth);
            GL.Color3(0.0, 0.0, 0.0);
            GL.DrawElements(BeginMode.Lines, (int)NEdge * 2, DrawElementsType.UnsignedInt, EdgeIndexs);

            GL.Color3(1.0, 0.0, 0.0);
            GL.Begin(BeginMode.Triangles);
            for (int iielem = 0; iielem < SelectedElems.Count; iielem++)
            {
                uint ielem0 = SelectedElems[iielem];

                GL.ArrayElement(ElemIndexs[ielem0 * 3]);
                GL.ArrayElement(ElemIndexs[ielem0 * 3 + 1]);
                GL.ArrayElement(ElemIndexs[ielem0 * 3 + 2]);
            }
            GL.End();

            // 面を描画
            GL.Color3(0.8, 0.8, 0.8);
            GL.DrawElements(BeginMode.Triangles, (int)NElem * 3, DrawElementsType.UnsignedInt, ElemIndexs);
        }

        private void DrawElementsSelectionTri()
        {
            if (!IsShown)
            {
                return;
            }

            // 面を描画
            for (int itri = 0; itri < NElem; itri++)
            {
                GL.PushName(itri);
                GL.Begin(BeginMode.Triangles);

                GL.ArrayElement(ElemIndexs[itri * 3]);
                GL.ArrayElement(ElemIndexs[itri * 3 + 1]);
                GL.ArrayElement(ElemIndexs[itri * 3 + 2]);

                GL.End();
                GL.PopName();
            }
        }

        private void DrawElementsBar()
        {
            if (!IsShown)
            {
                return;
            }

            GL.LineWidth(2);
            GL.Color3(1.0, 0.0, 0.0);
            GL.Begin(BeginMode.Lines);
            for (int iielem = 0; iielem < SelectedElems.Count; iielem++)
            {
                uint ielem0 = SelectedElems[iielem];

                GL.ArrayElement(ElemIndexs[ielem0 * 2]);
                GL.ArrayElement(ElemIndexs[ielem0 * 2 + 1]);
            }

            GL.End();

            /*
            if (IsSelected)
            {
                GL.Color3(1.0, 1.0, 0.0);
            }
            else
            {
                GL.Color3(0.0, 0.0, 0.0);
            }
            */
            GL.Color3(0.0, 0.0, 0.0);
            GL.DrawElements(BeginMode.Lines, (int)NElem * 2, DrawElementsType.UnsignedInt, ElemIndexs);
        }

        private void DrawElementsSelectionBar()
        {

            if (!IsShown)
            {
                return;
            }

            for (int ibar = 0; ibar < NElem; ibar++)
            {
                GL.PushName(ibar);
                GL.Begin(BeginMode.Lines);

                GL.ArrayElement(ElemIndexs[ibar * 2]);
                GL.ArrayElement(ElemIndexs[ibar * 2 + 1]);

                GL.End();
                GL.PopName();
            }
        }

    }
}
