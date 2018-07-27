using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class FieldDrawPart
    {
        public bool IsSelected { get; set; } = false;
        public uint MeshId { get; set; } = 0; 
        public ElementType Type { get; private set; } = ElementType.NOT_SET;
        public int Layer { get; set; } = 0;
        public double[] Color { get; } = new double[3] { 0.8, 0.8, 0.8 };
        public uint ElemCount { get; set; } = 0;
        public uint ElemPtCount { get; set; } = 0;
        public uint[] Indexs { get; set; } = null;
        public float[] Colors { get; set; } = null;

        public uint Dimension
        {
            get
            {
                if (Type == ElementType.LINE) { return 1; }
                if (Type == ElementType.TRI || Type == ElementType.QUAD) { return 2; }
                if (Type == ElementType.TET || Type == ElementType.HEX) { return 3; }
                return 0;
            }
        }

        public FieldDrawPart()
        {

        }

        public FieldDrawPart(FieldDrawPart src)
        {
            IsSelected = src.IsSelected;
            MeshId = src.MeshId;
            Type = src.Type;
            Layer = src.Layer;
            for (int i = 0; i < 3; i++)
            {
                Color[i] = src.Color[i];
            }
            ElemCount = src.ElemCount;
            ElemPtCount = src.ElemPtCount;
            Indexs = null;
            if (src.Indexs != null)
            {
                Indexs = new uint[src.Indexs.Length];
                src.Indexs.CopyTo(Indexs, 0);
            }
            Colors = null;
            if (src.Colors != null)
            {
                Colors = new float[src.Colors.Length];
                src.Colors.CopyTo(Colors, 0);
            }
        }

        public FieldDrawPart(uint meshId, FEWorld world)
        {
            var mesh = world.Mesh;
            if (!mesh.IsId(meshId))
            {
                return;
            }
            MeshId = meshId;

            uint cadId;
            int layer;
            uint elemCount;
            MeshType meshType;
            int loc;
            mesh.GetInfo(MeshId, out cadId, out layer);
            mesh.GetMeshInfo(MeshId, out elemCount, out meshType, out loc, out cadId);
            Layer = layer;
            ElemCount = elemCount;

            if (meshType == MeshType.VERTEX)
            {
                Type = ElementType.POINT;
                Color[0] = 0;
                Color[1] = 0;
                Color[2] = 0;
            }
            else if (meshType == MeshType.BAR)
            {
                Type = ElementType.LINE;
                Color[0] = 0;
                Color[1] = 0;
                Color[2] = 0;
                SetLine(world);
            }
            else if (meshType == MeshType.TRI)
            {
                Type = ElementType.TRI;
                SetTri(world);
            }
            else if (meshType == MeshType.QUAD)
            {
                Type = ElementType.QUAD;
                SetQuad(world);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void SetLine(FEWorld world)
        {
            System.Diagnostics.Debug.Assert(Type == ElementType.LINE);
            if (Type != ElementType.LINE)
            {
                return;
            }
            var mesh = world.Mesh;

            ElemPtCount = 2;
            MeshType meshType;
            int[] vertexs;
            mesh.GetConnectivity(MeshId, out meshType, out vertexs);
            System.Diagnostics.Debug.Assert(ElemPtCount * ElemCount == vertexs.Length);

            Indexs = new uint[ElemPtCount * ElemCount];
            for (int iEdge = 0; iEdge < ElemCount; iEdge++)
            {
                for (int iPt = 0; iPt < ElemPtCount; iPt++)
                {
                    Indexs[iEdge * ElemPtCount + iPt] = (uint)vertexs[iEdge * ElemPtCount + iPt];
                }
            }
        }

        private void SetTri(FEWorld world)
        {
            System.Diagnostics.Debug.Assert(Type == ElementType.TRI);
            if (Type != ElementType.TRI)
            {
                return;
            }
            var mesh = world.Mesh;

            ElemPtCount = 3;
            MeshType meshType;
            int[] vertexs;
            mesh.GetConnectivity(MeshId, out meshType, out vertexs);
            System.Diagnostics.Debug.Assert(ElemPtCount * ElemCount == vertexs.Length);

            Indexs = new uint[ElemPtCount * ElemCount];
            for (int iTri = 0; iTri < ElemCount; iTri++)
            {
                for (int iPt = 0; iPt < ElemPtCount; iPt++)
                {
                    Indexs[iTri * ElemPtCount + iPt] = (uint)vertexs[iTri * ElemPtCount + iPt];
                }
            }
        }

        private void SetQuad(FEWorld world)
        {
            System.Diagnostics.Debug.Assert(Type == ElementType.QUAD);
            if (Type != ElementType.QUAD)
            {
                return;
            }
            var mesh = world.Mesh;

            ElemPtCount = 4;
            MeshType meshType;
            int[] vertexs;
            mesh.GetConnectivity(MeshId, out meshType, out vertexs);
            System.Diagnostics.Debug.Assert(ElemPtCount * ElemCount == vertexs.Length);

            Indexs = new uint[ElemPtCount * ElemCount];
            for (int iQuad = 0; iQuad < ElemCount; iQuad++)
            {
                for (int iPt = 0; iPt < ElemPtCount; iPt++)
                {
                    Indexs[iQuad * ElemPtCount + iPt] = (uint)vertexs[iQuad * ElemPtCount + iPt];
                }
            }
        }

        public void SetColors(uint valueId, FEWorld world, IColorMap colorMap)
        {
            FieldValue fv = world.GetFieldValue(valueId);
            var mesh = world.Mesh;
            MeshType meshType;
            int[] vertexs;
            mesh.GetConnectivity(MeshId, out meshType, out vertexs);

            if (Type == ElementType.TRI || Type == ElementType.QUAD)
            {
                Colors = new float[ElemCount * 3];
                for (int iTri = 0; iTri < ElemCount; iTri++)
                {
                    double value = 0;
                    for (int iPt = 0; iPt < ElemPtCount; iPt++)
                    {
                        int coId = vertexs[iTri * ElemPtCount + iPt];
                        FieldDerivationType dt = FieldDerivationType.VALUE |
                            FieldDerivationType.VELOCITY |
                            FieldDerivationType.ACCELERATION;
                        double value1 = fv.GetShowValue(coId, 0, dt);
                        value += value1;
                    }
                    value /= ElemPtCount;

                    var color = colorMap.GetColor(value);
                    for (int iColor = 0; iColor < 3; iColor++)
                    {
                        Colors[iTri * 3 + iColor] = (float)color[iColor];
                    }
                }
            }
        }

        public void ClearColors()
        {
            Colors = null;
        }

        public uint[] GetVertexs(uint iElem)
        {
            uint[] vertexs = new uint[ElemPtCount]; 
            for (int iPt = 0; iPt < ElemPtCount; iPt++)
            {
                vertexs[iPt] = Indexs[iElem * ElemPtCount + iPt];
            }
            return vertexs;
        }

        public void DrawElements()
        {
            if (Colors == null)
            {
                //GL.Color3(Color[0], Color[1], Color[2]);
                if (Type == ElementType.LINE)
                {
                    GL.DrawElements(PrimitiveType.Lines, (int)(ElemCount * 2), DrawElementsType.UnsignedInt, Indexs);
                }
                else if (Type == ElementType.TRI || Type == ElementType.TET) 
                {
                    GL.DrawElements(PrimitiveType.Triangles, (int)(ElemCount * 3), DrawElementsType.UnsignedInt, Indexs);
                }
                else if (Type == ElementType.QUAD || Type == ElementType.HEX)
                {
                    GL.DrawElements(PrimitiveType.Quads, (int)(ElemCount * 4), DrawElementsType.UnsignedInt, Indexs);
                }
                return;
            }
            if (Type == ElementType.QUAD || Type == ElementType.HEX)
            {
                GL.Begin(PrimitiveType.Quads);
                for (int iQuad = 0; iQuad < ElemCount; iQuad++)
                {

                    GL.Color3(Colors[iQuad * 3], Colors[iQuad * 3 + 1], Colors[iQuad * 3 + 2]);
                    GL.ArrayElement((int)Indexs[iQuad * 4]);
                    GL.ArrayElement((int)Indexs[iQuad * 4 + 1]);
                    GL.ArrayElement((int)Indexs[iQuad * 4 + 2]);
                    GL.ArrayElement((int)Indexs[iQuad * 4 + 3]);
                }
                GL.End();
            }
            else if (Type == ElementType.TRI || Type == ElementType.TET)
            {
                GL.Begin(PrimitiveType.Triangles);
                for (int iTri = 0; iTri < ElemCount; iTri++)
                {
                    GL.Color3(Colors[iTri * 3], Colors[iTri * 3 + 1], Colors[iTri * 3 + 2]);
                    GL.ArrayElement((int)Indexs[iTri * 3]);
                    GL.ArrayElement((int)Indexs[iTri * 3 + 1]);
                    GL.ArrayElement((int)Indexs[iTri * 3 + 2]);
                }
                GL.End();
            }
        }

    }
}
