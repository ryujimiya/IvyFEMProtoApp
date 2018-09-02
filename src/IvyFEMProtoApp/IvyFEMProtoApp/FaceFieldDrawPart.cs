using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class FaceFieldDrawPart
    {
        public bool IsSelected { get; set; } = false;
        public uint MeshId { get; set; } = 0; 
        public ElementType Type { get; private set; } = ElementType.NotSet;
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
                if (Type == ElementType.Line) { return 1; }
                if (Type == ElementType.Tri || Type == ElementType.Quad) { return 2; }
                if (Type == ElementType.Tet || Type == ElementType.Hex) { return 3; }
                return 0;
            }
        }

        public FaceFieldDrawPart()
        {

        }

        public FaceFieldDrawPart(FaceFieldDrawPart src)
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

        public FaceFieldDrawPart(uint meshId, FEWorld world)
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

            if (meshType == MeshType.Vertex)
            {
                Type = ElementType.Point;
                Color[0] = 0;
                Color[1] = 0;
                Color[2] = 0;
            }
            else if (meshType == MeshType.Bar)
            {
                Type = ElementType.Line;
                Color[0] = 0;
                Color[1] = 0;
                Color[2] = 0;
                SetLine(world);
            }
            else if (meshType == MeshType.Tri)
            {
                Type = ElementType.Tri;
                SetTri(world);
            }
            else if (meshType == MeshType.Quad)
            {
                Type = ElementType.Quad;
                SetQuad(world);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void SetLine(FEWorld world)
        {
            System.Diagnostics.Debug.Assert(Type == ElementType.Line);
            if (Type != ElementType.Line)
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
            System.Diagnostics.Debug.Assert(Type == ElementType.Tri);
            if (Type != ElementType.Tri)
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
            System.Diagnostics.Debug.Assert(Type == ElementType.Quad);
            if (Type != ElementType.Quad)
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

        public void SetColors(uint valueId, FieldDerivationType dt, FEWorld world, IColorMap colorMap)
        {
            FieldValue fv = world.GetFieldValue(valueId);
            System.Diagnostics.Debug.Assert(fv.IsBubble == true);
            var mesh = world.Mesh;
            MeshType meshType;
            int[] vertexs;
            mesh.GetConnectivity(MeshId, out meshType, out vertexs);

            if (Type == ElementType.Tri)
            {
                Colors = new float[ElemCount * 3];
                for (int iTri = 0; iTri < ElemCount; iTri++)
                {
                    // Bubble
                    uint feId = world.GetTriangleFEIdFromMesh(MeshId, (uint)iTri);
                    System.Diagnostics.Debug.Assert(feId != 0);
                    double value = fv.GetShowValue((int)(feId - 1), 0, dt);
                    var color = colorMap.GetColor(value);
                    for (int iColor = 0; iColor < 3; iColor++)
                    {
                        Colors[iTri * 3 + iColor] = (float)color[iColor];
                    }
                }
            }
            else if (Type == ElementType.Quad)
            {
                // TRIと同じでよいが要素IDを取得するメソッドが現状ない
                throw new NotImplementedException();
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
                if (Type == ElementType.Line)
                {
                    GL.DrawElements(PrimitiveType.Lines, (int)(ElemCount * 2), DrawElementsType.UnsignedInt, Indexs);
                }
                else if (Type == ElementType.Tri || Type == ElementType.Tet) 
                {
                    GL.DrawElements(PrimitiveType.Triangles, (int)(ElemCount * 3), DrawElementsType.UnsignedInt, Indexs);
                }
                else if (Type == ElementType.Quad || Type == ElementType.Hex)
                {
                    GL.DrawElements(PrimitiveType.Quads, (int)(ElemCount * 4), DrawElementsType.UnsignedInt, Indexs);
                }
                return;
            }
            if (Type == ElementType.Quad || Type == ElementType.Hex)
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
            else if (Type == ElementType.Tri || Type == ElementType.Tet)
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
