using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class DrawPart
    {
        public bool IsShow { get; set; } = true;
        public bool IsSelected { get; set; } = false;
        public float[] Color { get; } = new float[3] { 0, 0, 0 };

        public CadElemType Type { get; set; } = CadElemType.VERTEX;
        public uint CadId { get; set; } = 0;
        public uint MshId { get; set; } = 0;

        public uint NElem { get; set; } = 0;
        public uint NPtElem { get; set; } = 0;
        public uint[] Indexs { get; set; } = null;

        public double Height { get; set; } = 0;
        public double DispX { get; set; } = 0;
        public double DispY { get; set; } = 0;

        public DrawPart()
        {

        }

        public DrawPart(DrawPart src)
        {
            IsShow = src.IsShow;
            IsSelected = src.IsSelected;
            for (int i = 0; i < 3; i++)
            {
                Color[i] = src.Color[i];
            }
            Type = src.Type;
            CadId = src.CadId;
            MshId = src.MshId;
            NElem = src.NElem;
            NPtElem = src.NPtElem;
            uint indexsCnt = src.NElem * src.NPtElem;
            Indexs = new uint[indexsCnt];
            for (int i = 0; i < indexsCnt; i++)
            {
                Indexs[i] = src.Indexs[i];
            }
            Height = src.Height;
            DispX = src.DispX;
            DispY = src.DispY;
        }

        public void Clear()
        {
            Indexs = null;
            NElem = 0;
            NPtElem = 0;
        }

        public bool Set(TriArray2D TriAry)
        {
            CadId = TriAry.LCadId;
            System.Diagnostics.Debug.Assert(CadId != 0);
            MshId = TriAry.Id;
            System.Diagnostics.Debug.Assert(MshId != 0);
            Type = CadElemType.LOOP;

            NPtElem = 3;
            NElem = (uint)TriAry.Tris.Count;
            Indexs = null;
            Indexs = new uint[NElem * NPtElem];
            for (uint ielem = 0; ielem < NElem; ielem++)
            {
                for (uint ipoel = 0; ipoel < NPtElem; ipoel++)
                {
                    Indexs[ielem * NPtElem + ipoel] = TriAry.Tris[(int)ielem].V[ipoel];
                }
            }

            Color[0] = 0.8f;
            Color[1] = 0.8f;
            Color[2] = 0.8f;

            return true;
        }

        public bool Set(BarArray BarArray)
        {
            CadId = BarArray.ECadId;
            System.Diagnostics.Debug.Assert(CadId != 0);
            MshId = BarArray.Id;
            System.Diagnostics.Debug.Assert(MshId != 0);
            Type = CadElemType.EDGE;

            NPtElem = 2;
            NElem = (uint)BarArray.Bars.Count;
            Indexs = new uint[NElem * NPtElem];
            for (uint ielem = 0; ielem < NElem; ielem++)
            {
                for (uint ipoel = 0; ipoel < NPtElem; ipoel++)
                {
                    Indexs[ielem * NPtElem + ipoel] = BarArray.Bars[(int)ielem].V[ipoel];
                }
            }

            Color[0] = 0.0f;
            Color[1] = 0.0f;
            Color[2] = 0.0f;

            return true;
        }

        public void SetHeight(double h)
        {
            Height = h;
        }

        public void DrawElements()
        {
            if (NPtElem == 2)
            {
                GL.DrawElements(BeginMode.Lines, (int)(NElem * NPtElem), DrawElementsType.UnsignedInt, Indexs);
                return;
            }
            else if (NPtElem == 3)
            {
                GL.DrawElements(BeginMode.Triangles, (int)(NElem * NPtElem), DrawElementsType.UnsignedInt, Indexs);
                return;
            }
        }

    }
}
