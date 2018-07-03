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
    class Mesher2DDrawer : IDrawer
    {
        private double[] SelectedColor = { 1.0, 0.5, 1.0 }; //{ 1.0, 1.0, 0.0 };
        public uint SutableRotMode { get; private set; } = 1;
        public bool IsAntiAliasing { get; set; } = false;

        private bool IsFrontAndBack;
        private IList<Mesher2DDrawPart> DrawParts = new List<Mesher2DDrawPart>();
        private IList<Mesher2DVertexDrawPart> VertexDrawParts = new List<Mesher2DVertexDrawPart>();
        private VertexArray VertexArray = new VertexArray();
        private bool IsDrawFace;
        private uint LineWidth = 1;

        public Mesher2DDrawer()
        {

        }

        public Mesher2DDrawer(Mesher2D mesher, bool isDrawFace = true)
        {
            IsDrawFace = isDrawFace;
            Set(mesher);
        }

        private bool Set(Mesher2D mesher)
        {
            SutableRotMode = 1; // DrawMode 1 : 2D

            int layerMin = 0;
            int layerMax = 0;
            {
                bool isInited = false;
                IList<TriArray2D> triArrays = mesher.GetTriArrays();
                for (int itri = 0; itri < triArrays.Count; itri++)
                {
                    int layer = triArrays[itri].Layer;
                    if (isInited)
                    {
                        layerMin = (layer < layerMin) ? layer : layerMin;
                        layerMax = (layer > layerMax) ? layer : layerMax;
                    }
                    else
                    {
                        layerMin = layer;
                        layerMax = layer;
                        isInited = true;
                    }
                }
                IList<QuadArray2D> quadArrays = mesher.GetQuadArrays();
                for (int iquad = 0; iquad < quadArrays.Count; iquad++)
                {
                    int layer = quadArrays[iquad].Layer;
                    if (isInited)
                    {
                        layerMin = (layer < layerMin) ? layer : layerMin;
                        layerMax = (layer > layerMax) ? layer : layerMax;
                    }
                    else
                    {
                        layerMin = layer;
                        layerMax = layer;
                        isInited = true;
                    }
                }
            }
            double layer_height = 1.0 / (layerMax - layerMin + 1);

            {
                // 三角形要素をセット
                IList<TriArray2D> triArrays = mesher.GetTriArrays();
                for (int itri = 0; itri < triArrays.Count; itri++)
                {
                    Mesher2DDrawPart dp = new Mesher2DDrawPart(triArrays[itri]);
                    int layer = triArrays[itri].Layer;
                    dp.Height = (layer - layerMin) * layer_height;

                    DrawParts.Add(dp);
                }
            }

            {
                // 四角形要素をセット
                IList<QuadArray2D> quadArrays = mesher.GetQuadArrays();
                for (int iquad = 0; iquad < quadArrays.Count; iquad++)
                {
                    Mesher2DDrawPart dp = new Mesher2DDrawPart(quadArrays[iquad]);
                    int ilayer = quadArrays[iquad].Layer;
                    dp.Height = (ilayer - layerMin) * layer_height;

                    DrawParts.Add(dp);

                }
            }

            {
                // 線要素をセット
                IList<BarArray> barArrays = mesher.GetBarArrays();
                for (int ibar = 0; ibar < barArrays.Count; ibar++)
                {
                    double height = 0;
                    {
                        int layer = barArrays[ibar].Layer;
                        height += (layer - layerMin) * layer_height;
                        height += 0.01 * layer_height;
                    }
                    Mesher2DDrawPart dp = new Mesher2DDrawPart(barArrays[ibar]);
                    dp.Height = height;

                    DrawParts.Add(dp);

                }
            }

            { 
                // 頂点をセット
                IList<Vertex> vertexs = mesher.GetVertexs();
                for (int iver = 0; iver < vertexs.Count; iver++)
                {
                    double height = 0;
                    {
                        int layer = vertexs[iver].Layer;
                        height += (layer - layerMin) * layer_height;
                        height += 0.01 * layer_height;
                    }
                    Mesher2DVertexDrawPart dpv = new Mesher2DVertexDrawPart();
                    dpv.CadId = vertexs[iver].VCadId;
                    dpv.MshId = vertexs[iver].Id;
                    dpv.VId = vertexs[iver].V;
                    dpv.Height = height;
                    dpv.IsSelected = false;

                    VertexDrawParts.Add(dpv);
                }
            }

            {
                // 座標をセット
                IList<System.Numerics.Vector2> vec2Ds = mesher.GetVectors();
                uint nDim = 2;
                uint nVec = (uint)vec2Ds.Count;
                VertexArray.SetSize(nVec, nDim);
                for (int ivec = 0; ivec < nVec; ivec++)
                {
                    VertexArray.VertexCoordArray[ivec * nDim] = vec2Ds[ivec].X;
                    VertexArray.VertexCoordArray[ivec * nDim + 1] = vec2Ds[ivec].Y;
                }
            }
            return true;
        }

        public void UpdateCoord(Mesher2D mesher)
        {
            {
                // 座標をセット
                IList<System.Numerics.Vector2> vecs = mesher.GetVectors();
                uint nDim = 2;
                uint nVec = (uint)vecs.Count;
                if (VertexArray.NDim != nDim)
                {
                    return;
                }
                if (VertexArray.NPoint != nVec)
                {
                    return;
                }
                for (int ivec = 0; ivec < nVec; ivec++)
                {
                    VertexArray.VertexCoordArray[ivec * nDim] = vecs[ivec].X;
                    VertexArray.VertexCoordArray[ivec * nDim + 1] = vecs[ivec].Y;
                }
            }
        }

        public BoundingBox3D GetBoundingBox(double[] rot)
        {
            return VertexArray.GetBoundingBox(rot);
        }

        public void Draw()
        {
            // ライティングの指定
            GL.Disable(EnableCap.Lighting);
            // 色の指定
            GL.Color3(0.8, 0.8, 0.8);

            // 片面かどうかの指定
            GL.Enable(EnableCap.CullFace);

            GL.CullFace(CullFaceMode.Back);
            //GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            //GL.Disable(EnableCap.DepthTest);

            uint nDim = VertexArray.NDim;

            // 頂点配列の設定
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer((int)nDim, VertexPointerType.Double, 0, VertexArray.VertexCoordArray);
            
            // 面と辺の描画
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                Mesher2DDrawPart dp = DrawParts[idp];
                if (!IsDrawFace && (dp.GetElemDim() == 2)) { continue; }
                double height = dp.Height;

                GL.Translate(0, 0, +height);
                dp.DrawElements();
                GL.Translate(0, 0, -height);
            }
            // 点の描画
            GL.Translate(0.0, 0.0, 0.2);
            GL.PointSize(5);
            GL.Begin(BeginMode.Points);
            for (int iver = 0; iver < VertexDrawParts.Count; iver++)
            {
                Mesher2DVertexDrawPart vdp = VertexDrawParts[iver];
                if (vdp.IsSelected)
                {
                    GL.Color3(SelectedColor);
                }
                else
                {
                    GL.Color3(0.0, 0.0, 0.0);
                }
                uint ipo0 = vdp.VId;
                GL.ArrayElement((int)ipo0);
            }
            GL.End();
            GL.Translate(0.0, 0.0, -0.2);

            GL.DisableClientState(ArrayCap.VertexArray);
        }

        public void DrawSelection(uint idraw)
        {
            uint nDim = VertexArray.NDim;

            // モデルの描画
            GL.EnableClientState(ArrayCap.VertexArray);

            GL.VertexPointer((int)nDim, VertexPointerType.Double, 0, VertexArray.VertexCoordArray);


            GL.PushName(idraw);
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                GL.PushName(idp);
                DrawParts[idp].DrawElementsSelection();
                GL.PopName();
            }
            for (int iver = 0; iver < VertexDrawParts.Count; iver++)
            {
                uint ipo0 = VertexDrawParts[iver].VId;

                GL.PushName(DrawParts.Count + iver);

                GL.Translate(0.0, 0.0, 0.2);
                GL.Begin(BeginMode.Points);

                GL.ArrayElement((int)ipo0);

                GL.End();
                GL.Translate(0.0, 0.0, -0.2);

                GL.PopName();
            }

            GL.PopName();

            GL.DisableClientState(ArrayCap.VertexArray);
        }

        public void AddSelected(int[] selectFlag)
        {
            int idp0 = selectFlag[1];
            int ielem0 = selectFlag[2];
            if (idp0 < DrawParts.Count)
            {
                DrawParts[idp0].IsSelected = true;
                IList<uint> selectedElems = DrawParts[idp0].SelectedElems;
                for (int i = 0; i < selectedElems.Count; i++)
                {
                    uint selectedElem = selectedElems[i];
                    if (selectedElem == ielem0)
                    {
                        selectedElems.RemoveAt(i);
                        return;
                    }
                }
                selectedElems.Add((uint)ielem0);
            }
            else
            {
                uint iver = (uint)(idp0 - DrawParts.Count);
                VertexDrawParts[(int)iver].IsSelected = true;
            }
        }

        public void ClearSelected()
        {
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                Mesher2DDrawPart dp = DrawParts[idp];
                dp.IsSelected = false;
                dp.SelectedElems.Clear();
            }
            for (int iver = 0; iver < VertexDrawParts.Count; iver++)
            {
                Mesher2DVertexDrawPart vdp = VertexDrawParts[iver];
                vdp.IsSelected = false;
            }
        }
    }
}
