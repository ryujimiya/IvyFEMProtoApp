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
    class CadObject2DDrawer
    {
        //private bool IsShow = true;
        public uint SutableRotMode { get; private set; } = 1;
        public bool IsAntiAliasing { get; set; } = false;

        private byte[] Mask = new byte[128];
        private IList<DrawPart> DrawParts = new List<DrawPart>();
        private IList<VertexDrawPart> VertexDrawParts = new List<VertexDrawPart>();
        private VertexArray VertexArray = new VertexArray();
        private uint LineWidth = 3;
        private uint PointSize = 5;
        private double TexCentX = 0;
        private double TexCentY = 0;
        private double TexScale = 1;

        public CadObject2DDrawer()
        {
            SetupMask();
        }

        public CadObject2DDrawer(CadObject2D cad2D)
        {
            SetupMask();
            UpdateCadTopologyGeometry(cad2D);
        }

        private void SetupMask()
        {
            for (uint j = 0; j < 4; j++)
            {
                for (uint i = 0; i < 8; i++)
                {
                    Mask[j * 32 + i] = 0x33;
                }
                for (uint i = 0; i < 8; i++)
                {
                    Mask[j * 32 + 8 + i] = 0xcc;
                }
                for (uint i = 0; i < 8; i++)
                {
                    Mask[j * 32 + 16 + i] = 0x33;
                }
                for (uint i = 0; i < 8; i++)
                {
                    Mask[j * 32 + 24 + i] = 0xcc;
                }
            }
        }

        public bool UpdateCadTopologyGeometry(CadObject2D cad2D)
        {
            SutableRotMode = 1;
            IList<DrawPart> oldDrawParts = new List<DrawPart>();
            for (int i = 0; i < DrawParts.Count; i++)
            {
                oldDrawParts.Add(new DrawPart(DrawParts[i]));
            }

            for (int idp = 0; idp < oldDrawParts.Count; idp++)
            {
                oldDrawParts[idp].MshId = 0;
                oldDrawParts[idp].IsSelected = false;
            }
            DrawParts.Clear();
            VertexDrawParts.Clear();

            Mesher2D mesh = new Mesher2D(cad2D);

            int minLayer;
            int maxLayer;
            cad2D.GetLayerMinMax(out minLayer, out maxLayer);

            double layerHeight = 1.0 / (maxLayer - minLayer + 1);

            {
                IList<TriArray2D> triArrays = mesh.GetTriArrays();
                for (int ita = 0; ita < triArrays.Count; ita++)
                {
                    uint lId = triArrays[ita].LCadId;
                    double height = 0;
                    {
                        int layer = cad2D.GetLayer(CadElemType.LOOP, lId);
                        height = (layer - minLayer) * layerHeight;
                    }
                    int idp0 = 0;
                    for (; idp0 < oldDrawParts.Count; idp0++)
                    {
                        if (oldDrawParts[idp0].Type == CadElemType.LOOP &&
                            oldDrawParts[idp0].CadId == lId)
                        {
                            oldDrawParts[idp0].Set(triArrays[ita]);
                            oldDrawParts[idp0].SetHeight(height);
                            double[] color = new double[3];
                            cad2D.GetLoopColor(lId, color);
                            for (int i = 0; i < 3; i++)
                            {
                                oldDrawParts[idp0].Color[i] = (float)color[i];
                            }
                            DrawParts.Add(oldDrawParts[idp0]);
                            break;
                        }
                    }
                    if (idp0 == oldDrawParts.Count)
                    {
                        DrawPart dp = new DrawPart();
                        dp.Set(triArrays[ita]);
                        dp.SetHeight(height);
                        double[] color = new double[3];
                        cad2D.GetLoopColor(lId, color);
                        for (int i = 0; i < 3; i++)
                        {
                            dp.Color[i] = (float)color[i];
                        }
                        DrawParts.Add(dp);
                    }
                }
            }

            {
                IList<BarArray> barArrays = mesh.GetBarArrays();
                for (int ibar = 0; ibar < barArrays.Count; ibar++)
                {
                    uint eId = barArrays[ibar].ECadId;
                    double height = 0;
                    {
                        int layer = cad2D.GetLayer(CadElemType.EDGE, eId);
                        height += (layer - minLayer + 0.01) * layerHeight;
                    }
                    int idp0 = 0;
                    for (; idp0 < oldDrawParts.Count; idp0++)
                    {
                        if (oldDrawParts[idp0].Type == CadElemType.EDGE
                           && oldDrawParts[idp0].CadId == eId)
                        {
                            oldDrawParts[idp0].Set(barArrays[ibar]);
                            oldDrawParts[idp0].SetHeight(height);
                            double[] color = new double[3];
                            cad2D.GetEdgeColor(eId, color);
                            for (int i = 0; i < 3; i++)
                            {
                                oldDrawParts[idp0].Color[i] = (float)color[i];
                            }
                            DrawParts.Add(oldDrawParts[idp0]);
                            break;
                        }
                    }
                    if (idp0 == oldDrawParts.Count)
                    {
                        DrawPart dp = new DrawPart();
                        dp.Set(barArrays[ibar]);
                        dp.SetHeight(height);
                        double[] color = new double[3];
                        cad2D.GetEdgeColor(eId, color);
                        for (int i = 0; i < 3; i++)
                        {
                            dp.Color[i] = (float)color[i];
                        }
                        DrawParts.Add(dp);
                    }
                }
            }

            oldDrawParts.Clear();

            {
                IList<Vertex> vertexs = mesh.GetVertexs();
                for (int iver = 0; iver < vertexs.Count; iver++)
                {
                    uint vCadId = vertexs[iver].VCadId;
                    int layer = cad2D.GetLayer(CadElemType.VERTEX, vCadId);
                    double height = (layer - minLayer + 0.1) * layerHeight;
                    VertexDrawPart vdp = new VertexDrawPart();
                    vdp.CadId = vCadId;
                    vdp.MshId = vertexs[iver].Id;
                    vdp.VId = vertexs[iver].V;
                    vdp.IsSelected = false;
                    vdp.IsShow = true;
                    vdp.Height = height;
                    double[] color = new double[3];
                    cad2D.GetVertexColor(vCadId, color);
                    for (int i = 0; i < 3; i++)
                    {
                        vdp.Color[i] = (float)color[i];
                    }
                    VertexDrawParts.Add(vdp);
                }
            }

            {
                IList<System.Numerics.Vector2> vecs = mesh.GetVectors();
                uint nvec = (uint)vecs.Count;
                uint ndim = 2;
                VertexArray.SetSize(nvec, ndim);
                for (int ivec = 0; ivec < nvec; ivec++)
                {
                    VertexArray.VertexCoordArray[ivec * ndim] = vecs[ivec].X;
                    VertexArray.VertexCoordArray[ivec * ndim + 1] = vecs[ivec].Y;
                }
                for (int ivec = 0; ivec < nvec; ivec++)
                {
                    VertexArray.UVCoordArray[ivec * ndim] = vecs[ivec].X * TexScale;
                    VertexArray.UVCoordArray[ivec * ndim + 1] = vecs[ivec].Y * TexScale;
                }
            }

            return true;
        }

        public BoundingBox3D GetBoundingBox(double[] rot)
        {
            return VertexArray.GetBoundingBox(rot);
        }

        public void Draw()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            bool isLighting = GL.IsEnabled(EnableCap.Lighting);
            bool isTexture = GL.IsEnabled(EnableCap.Texture2D);
            bool isBlend = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.Lighting);

            uint ndim = VertexArray.NDim;

            ////////////////////////////////////////////////////////////////
            // モデルの描画
            {
                // draw vertecies
                GL.Disable(EnableCap.Texture2D);
                ////////////////
                GL.PointSize(PointSize);
                GL.Begin(BeginMode.Points);
                for (uint iver = 0; iver < VertexDrawParts.Count; iver++)
                {
                    if (!VertexDrawParts[(int)iver].IsShow)
                    {
                        continue;
                    }
                    double height = VertexDrawParts[(int)iver].Height;
                    if (VertexDrawParts[(int)iver].IsSelected)
                    {
                        GL.Color3(1.0, 1.0, 0.0);
                    }
                    else
                    {
                        GL.Color3(VertexDrawParts[(int)iver].Color);
                    }
                    uint ipo0 = VertexDrawParts[(int)iver].VId;
                    GL.Vertex3(
                        VertexArray.VertexCoordArray[ipo0 * ndim + 0],
                        VertexArray.VertexCoordArray[ipo0 * ndim + 1],
                        height);
                }
                GL.End();
                if (isTexture)
                {
                    GL.Enable(EnableCap.Texture2D);
                }
            }
            /////////////
            // vertex arrayを登録する
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer((int)ndim, VertexPointerType.Double, 0, VertexArray.VertexCoordArray);
            if (isTexture && VertexArray.UVCoordArray != null)
            {
                GL.EnableClientState(ArrayCap.TextureCoordArray);
                GL.TexCoordPointer(2, TexCoordPointerType.Double, 0, VertexArray.UVCoordArray);
                GL.MatrixMode(MatrixMode.Texture);
                GL.LoadIdentity();
                GL.Translate(-TexCentX, -TexCentY, 0.0);
            }
            for (uint idp = 0; idp < DrawParts.Count; idp++)
            {
                DrawPart part = DrawParts[(int)idp];
                double height = part.Height;
                double dispX = part.DispX;
                double dispY = part.DispY;
                if (part.Type == CadElemType.EDGE)
                {
                    // draw edge
                    GL.Disable(EnableCap.Texture2D);
                    if (!part.IsShow)
                    {
                        continue;
                    }
                    GL.LineWidth(LineWidth);
                    if (IsAntiAliasing)
                    { 
                        // anti aliasing
                        GL.Enable(EnableCap.LineSmooth);
                        GL.Enable(EnableCap.Blend);
                        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                        GL.Hint(HintTarget.LineSmoothHint, HintMode.DontCare);
                    }
                    if (part.IsSelected)
                    {
                        GL.Color3(1.0, 1.0, 0.0);
                    }
                    else
                    {
                        GL.Color3(part.Color);
                    }
                    GL.Translate(0.0, 0.0, height);
                    part.DrawElements();
                    GL.Translate(0.0, 0.0, -height);
                    GL.Disable(EnableCap.LineSmooth);
                    GL.Disable(EnableCap.Blend);
                    if (isTexture)
                    {
                        GL.Enable(EnableCap.Texture2D);
                    }
                }
                else if (part.Type == CadElemType.LOOP)
                {
                    GL.Disable(EnableCap.Blend);
                    if (part.IsSelected)
                    {
                        GL.Enable(EnableCap.PolygonStipple);
                        GL.PolygonStipple(Mask);
                        GL.Color3(1.0, 1.0, 0.0);
                        GL.Translate(0.0, 0.0, +height + 0.001);
                        part.DrawElements();
                        GL.Translate(0.0, 0.0, -height - 0.001);
                        GL.Disable(EnableCap.PolygonStipple);
                    }
                    if (!part.IsShow)
                    {
                        continue;
                    }
                    GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, part.Color);
                    GL.Color3(part.Color);
                    GL.Translate(+dispX, +dispY, +height);
                    part.DrawElements();
                    GL.Translate(-dispX, -dispY, -height);
                }
            }
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
            if (isLighting)
            {
                GL.Enable(EnableCap.Lighting);
            }
            else
            {
                GL.Disable(EnableCap.Lighting);
            }
            if (isBlend)
            {
                GL.Enable(EnableCap.Blend);
            }
            else
            {
                GL.Disable(EnableCap.Blend);
            }
            if (isTexture)
            {
                GL.Enable(EnableCap.Texture2D);
            }
            else
            {
                GL.Disable(EnableCap.Texture2D);
            }
        }

    }
}
