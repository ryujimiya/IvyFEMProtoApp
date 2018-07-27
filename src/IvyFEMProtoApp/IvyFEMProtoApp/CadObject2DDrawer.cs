﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class CadObject2DDrawer : IDrawer
    {
        public RotMode SutableRotMode { get; private set; } = RotMode.ROTMODE_2D;
        public bool IsAntiAliasing { get; set; } = false;

        private double[] SelectedColor = { 1.0, 0.5, 1.0 }; //{ 1.0, 1.0, 0.0 };
        private byte[] Mask = new byte[128];
        private IList<CadObject2DDrawPart> DrawParts = new List<CadObject2DDrawPart>();
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
            SutableRotMode = RotMode.ROTMODE_2D;
            IList<CadObject2DDrawPart> oldDrawParts = new List<CadObject2DDrawPart>();
            for (int i = 0; i < DrawParts.Count; i++)
            {
                oldDrawParts.Add(new CadObject2DDrawPart(DrawParts[i]));
            }

            for (int idp = 0; idp < oldDrawParts.Count; idp++)
            {
                oldDrawParts[idp].MeshId = 0;
                oldDrawParts[idp].ShowMode = 0;
            }
            DrawParts.Clear();

            int minLayer;
            int maxLayer;
            cad2D.GetLayerMinMax(out minLayer, out maxLayer);
            double layerHeight = 1.0 / (maxLayer - minLayer + 1);

            {
                // 面をセット
                IList<uint> lIds = cad2D.GetElemIds(CadElementType.LOOP);
                for (int iLId = 0; iLId < lIds.Count; iLId++)
                {
                    uint lId = lIds[iLId];
                    double height = 0;
                    {
                        int layer = cad2D.GetLayer(CadElementType.LOOP, lId);
                        height = (layer - minLayer) * layerHeight;
                    }
                    int idp0 = 0;
                    for (; idp0 < oldDrawParts.Count; idp0++)
                    {
                        CadObject2DDrawPart olddp = oldDrawParts[idp0];
                        if (olddp.Type == CadElementType.LOOP && olddp.CadId == lId)
                        {
                            olddp.MeshId = 1;
                            olddp.Height = height;
                            double[] color = new double[3];
                            cad2D.GetLoopColor(lId, color);
                            for (int iTmp = 0; iTmp < 3; iTmp++)
                            {
                                olddp.Color[iTmp] = (float)color[iTmp];
                            }
                            DrawParts.Add(oldDrawParts[idp0]);
                            break;
                        }
                    }
                    if (idp0 == oldDrawParts.Count)
                    {
                        CadObject2DDrawPart dp = new CadObject2DDrawPart();
                        dp.CadId = lId;
                        dp.Type = CadElementType.LOOP;
                        dp.Height = height;
                        double[] color = new double[3];
                        cad2D.GetLoopColor(lId, color);
                        for (int iTmp = 0; iTmp < 3; iTmp++)
                        {
                            dp.Color[iTmp] = (float)color[iTmp];
                        }
                        DrawParts.Add(dp);
                    }
                }
            }

            {
                // set edge
                IList<uint> eIds = cad2D.GetElemIds(CadElementType.EDGE);
                for (int iEId = 0; iEId < eIds.Count; iEId++)
                {
                    uint eId = eIds[iEId];
                    double height = 0;
                    {
                        int layer = cad2D.GetLayer(CadElementType.EDGE, eId);
                        height += (layer - minLayer + 0.01) * layerHeight;
                    }
                    int idp0 = 0;
                    for (; idp0 < oldDrawParts.Count; idp0++)
                    {
                        CadObject2DDrawPart olddp = oldDrawParts[idp0];
                        if (olddp.Type == CadElementType.EDGE && olddp.CadId == eId)
                        {
                            olddp.MeshId = 1;
                            olddp.Height = height;
                            DrawParts.Add(olddp);
                            break;
                        }
                    }
                    if (idp0 == oldDrawParts.Count)
                    {
                        CadObject2DDrawPart dp = new CadObject2DDrawPart();
                        dp.CadId = eId;
                        dp.Type = CadElementType.EDGE;
                        dp.Height = height;
                        DrawParts.Add(dp);
                    }
                    {
                        CadObject2DDrawPart dp = DrawParts[DrawParts.Count - 1];
                        Edge2D edge = cad2D.GetEdge(eId);
                        dp.CtrlPoints.Clear();
                        dp.CurveType = edge.CurveType;
                        if (edge.CurveType == CurveType.CURVE_ARC)
                        {
                            Vector2 cPt;
                            double radius;
                            edge.GetCenterRadius(out cPt, out radius);
                            dp.CtrlPoints.Add(cPt);
                        }
                        else if (edge.CurveType == CurveType.CURVE_BEZIER)
                        {
                            IList<Vector2> cos = edge.GetCurvePoint();
                            dp.CtrlPoints.Add(cos[0]);
                            dp.CtrlPoints.Add(cos[1]);
                        }
                    }
                }
            }

            { 
                // set vertex
                IList<uint> vIds = cad2D.GetElemIds(CadElementType.VERTEX);
                for (int iVId = 0; iVId < vIds.Count; iVId++)
                {
                    uint vCadId = vIds[iVId];
                    int layer = cad2D.GetLayer(CadElementType.VERTEX, vCadId);
                    double height = (layer - minLayer + 0.1) * layerHeight;
                    int idp0 = 0;
                    for (; idp0 < oldDrawParts.Count; idp0++)
                    {
                        CadObject2DDrawPart olddp = oldDrawParts[idp0];
                        if (olddp.Type == CadElementType.VERTEX && olddp.CadId == vCadId)
                        {
                            olddp.MeshId = 1;
                            olddp.Height = height;
                            DrawParts.Add(olddp);
                            break;
                        }
                    }
                    if (idp0 == oldDrawParts.Count)
                    {
                        CadObject2DDrawPart dp = new CadObject2DDrawPart();
                        dp.CadId = vCadId;
                        dp.Type = CadElementType.VERTEX;
                        dp.Height = height;
                        DrawParts.Add(dp);
                    }
                }
            }

            oldDrawParts.Clear();

            UpdateCadGeometry(cad2D);
            return true;
        }

        public void UpdateCadGeometry(CadObject2D cad2D)
        {
            Mesher2D mesh = new Mesher2D(cad2D);
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                CadObject2DDrawPart dp = DrawParts[idp];
                dp.Clear();
                uint cadId = dp.CadId;
                CadElementType cadType = dp.Type;
                if (!cad2D.IsElemId(cadType, cadId))
                {
                    continue;
                }
                uint meshId = mesh.GetIdFromCadId(cadId, cadType);
                if (meshId == 0)
                {
                    continue;
                }
                MeshType meshType;
                uint elemCnt;
                int loc;
                uint cadId0;
                mesh.GetMeshInfo(meshId, out elemCnt, out meshType, out loc, out cadId0);
                System.Diagnostics.Debug.Assert(cadId0 == cadId);
                if (meshType == MeshType.TRI)
                {
                    dp.SetTriArray(mesh.GetTriArrays()[loc]);
                    double[] color = new double[3];
                    cad2D.GetLoopColor(cadId0, color);
                    for (int iTmp = 0; iTmp < 3; iTmp++)
                    {
                        dp.Color[iTmp] = (float)color[iTmp];
                    }
                }
                else if (meshType == MeshType.BAR)
                {
                    dp.SetBarArray(mesh.GetBarArrays()[loc]);
                    System.Diagnostics.Debug.Assert(cadType == CadElementType.EDGE);
                    Edge2D edge = cad2D.GetEdge(cadId);
                    dp.CurveType = edge.CurveType;
                    dp.CtrlPoints.Clear();
                    if (edge.CurveType == CurveType.CURVE_ARC)
                    {
                        Vector2 cPt;
                        double radius;
                        edge.GetCenterRadius(out cPt, out radius);
                        dp.CtrlPoints.Add(cPt);
                    }
                    else if (edge.CurveType == CurveType.CURVE_BEZIER)
                    {
                        IList<Vector2> cos = edge.GetCurvePoint();
                        dp.CtrlPoints.Add(cos[0]);
                        dp.CtrlPoints.Add(cos[1]);
                    }
                }
                else if (meshType == MeshType.VERTEX)
                {
                    dp.SetVertex(mesh.GetVertexs()[loc]);
                }
            }

            {
                // 座標をセット
                IList<Vector2> vec2Ds = mesh.GetVectors();
                uint ptCnt = (uint)vec2Ds.Count;
                uint ndim = 2;
                VertexArray.SetSize(ptCnt, ndim);
                for (int iPt = 0; iPt < ptCnt; iPt++)
                {
                    VertexArray.VertexCoordArray[iPt * ndim] = vec2Ds[iPt].X;
                    VertexArray.VertexCoordArray[iPt * ndim + 1] = vec2Ds[iPt].Y;
                }
                if (VertexArray.UVCoordArray != null)
                {
                    for (int iPt = 0; iPt < ptCnt; iPt++)
                    {
                        VertexArray.UVCoordArray[iPt * ndim] = vec2Ds[iPt].X * TexScale;
                        VertexArray.UVCoordArray[iPt * ndim + 1] = vec2Ds[iPt].Y * TexScale;
                    }
                }
            }
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

            uint ndim = VertexArray.Dimension;

            ////////////////////////////////////////////////////////////////
            // モデルの描画

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
            GL.PointSize(PointSize);
            GL.LineWidth(LineWidth);
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                CadObject2DDrawPart dp = DrawParts[idp];
                if (dp.ShowMode == -1)
                {
                    continue;
                }
                double height = dp.Height;
                double dispX = dp.DispX;
                double dispY = dp.DispY;
                if (dp.Type == CadElementType.VERTEX)
                {
                    GL.Disable(EnableCap.Texture2D);
                    if (dp.ShowMode == 1)
                    {
                        GL.Color3(1.0, 1.0, 0.0);
                    }
                    else if (dp.ShowMode == 2)
                    {
                        GL.Color3(SelectedColor);
                    }
                    else
                    {
                        GL.Color3(0.0, 0.0, 0.0);
                    }

                    GL.Translate(0.0, 0.0, height);
                    dp.DrawElements();

                    GL.Translate(0.0, 0.0, -height);
                    if (isTexture)
                    {
                        GL.Enable(EnableCap.Texture2D);
                    }
                }
                if (dp.Type == CadElementType.EDGE)
                {
                    GL.Disable(EnableCap.Texture2D);
                    GL.LineWidth(LineWidth);
                    if (IsAntiAliasing)
                    {
                        GL.Enable(EnableCap.LineSmooth);
                        GL.Enable(EnableCap.Blend);
                        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                        GL.Hint(HintTarget.LineSmoothHint, HintMode.DontCare);
                    }
                    if (dp.ShowMode == 1)
                    {
                        GL.Color3(1.0, 1.0, 0.0);
                    }
                    else if (dp.ShowMode == 2)
                    {
                        GL.Color3(SelectedColor);
                    }
                    else
                    {
                        GL.Color3(0, 0, 0);
                    }

                    GL.Translate(0.0, 0.0, height);
                    dp.DrawElements();
                    if (dp.ShowMode > 0)
                    {
                        // draw ctrl point        
                        GL.Begin(PrimitiveType.Points);
                        for (int icp = 0; icp < dp.CtrlPoints.Count; icp++)
                        {
                            Vector2 cp = dp.CtrlPoints[icp];
                            GL.Vertex3(cp.X, cp.Y, 0.0);
                        }
                        GL.End();
                        // draw line between ctrl point and point
                        GL.Enable(EnableCap.LineStipple);
                        GL.LineStipple(1, 0xF0F0);
                        GL.PolygonStipple(Mask);
                        GL.LineWidth(1);
                        GL.Begin(PrimitiveType.Lines);
                        uint sVI = dp.Indexs[0];
                        uint eVI = dp.Indexs[dp.ElemCount * dp.ElemPtCount - 1];
                        double[] va = VertexArray.VertexCoordArray;
                        Vector2 sPt = new Vector2(
                            (float)va[sVI * 2 + 0], (float)va[sVI * 2 + 1]);
                        Vector2 ePt = new Vector2(
                            (float)va[eVI * 2 + 0], (float)va[eVI * 2 + 1]);
                        if (dp.CurveType == CurveType.CURVE_ARC)
                        {
                            OpenGLUtils.GLVertex2(sPt);
                            OpenGLUtils.GLVertex2(dp.CtrlPoints[0]);
                            OpenGLUtils.GLVertex2(ePt);
                            OpenGLUtils.GLVertex2(dp.CtrlPoints[0]);
                        }
                        if (dp.CurveType == CurveType.CURVE_BEZIER)
                        {
                            OpenGLUtils.GLVertex2(sPt);
                            OpenGLUtils.GLVertex2(dp.CtrlPoints[0]);
                            OpenGLUtils.GLVertex2(ePt);
                            OpenGLUtils.GLVertex2(dp.CtrlPoints[1]);
                        }
                        GL.End();
                        GL.Disable(EnableCap.LineStipple);
                    }
                    GL.Translate(0.0, 0.0, -height);
                    GL.Disable(EnableCap.LineSmooth);
                    GL.Disable(EnableCap.Blend);
                    if (isTexture)
                    {
                        GL.Enable(EnableCap.Texture2D);
                    }
                }
                else if (dp.Type == CadElementType.LOOP)
                {
                    GL.Disable(EnableCap.Blend);
                    if (dp.ShowMode > 0)
                    {
                        GL.Enable(EnableCap.PolygonStipple);
                        GL.PolygonStipple(Mask);
                        if (dp.ShowMode == 1)
                        {
                            GL.Color3(1.0, 1.0, 0.0);
                        }
                        else if (dp.ShowMode == 2)
                        {
                            GL.Color3(SelectedColor);
                        }
                        GL.Translate(0.0, 0.0, +height + 0.001);
                        dp.DrawElements();
                        GL.Translate(0.0, 0.0, -height - 0.001);
                        GL.Disable(EnableCap.PolygonStipple);
                    }
                    if (dp.ShowMode != 0)
                    {
                        continue;
                    }
                    GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, dp.Color);
                    GL.Color3(dp.Color);
                    GL.Translate(+dispX, +dispY, +height);
                    dp.DrawElements();
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

        public void DrawSelection(uint iDraw)
        {
            bool isBlend = GL.IsEnabled(EnableCap.Blend);
            bool isLineSmooth = GL.IsEnabled(EnableCap.LineSmooth);
            bool isTexture = GL.IsEnabled(EnableCap.Texture2D);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.LineSmooth);
            GL.Disable(EnableCap.Texture2D);

            uint ndim = VertexArray.Dimension;
            GL.PushName(iDraw);
            // モデルの描画
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer((int)ndim, VertexPointerType.Double, 0, VertexArray.VertexCoordArray);
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                CadObject2DDrawPart dp = DrawParts[idp];
                double height = dp.Height;
                GL.PushName(idp);
                GL.Translate(0.0, 0.0, +height);
                dp.DrawElements();
                if (dp.Type == CadElementType.EDGE && dp.ShowMode == 2)
                {
                    for (int icp = 0; icp < dp.CtrlPoints.Count; icp++)
                    {
                        Vector2 cp = dp.CtrlPoints[icp];
                        GL.PushName(icp);
                        GL.Begin(PrimitiveType.Points);
                        OpenGLUtils.GLVertex2(cp);
                        GL.End();
                        GL.PopName();
                    }
                }
                GL.Translate(0.0, 0.0, -height);
                GL.PopName();
            }
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.PopName();

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

        public void GetPartCadId(int[] selectFlag, 
            out CadElementType partType, out uint partId, out int ctrlIndex)
        {
            uint idp = (uint)selectFlag[1];
            if (idp < DrawParts.Count)
            {
                CadObject2DDrawPart dp = DrawParts[(int)idp];
                partType = dp.Type;
                partId = dp.CadId;
                ctrlIndex = selectFlag[2];
                return;
            }
            partType = CadElementType.NOT_SET;
            partId = 0;
            ctrlIndex = 0;
        }

        public void SetShowMode(CadElementType type, uint id, int showMode)
        {
            bool flag = false;
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                if (DrawParts[idp].Type == type && DrawParts[idp].CadId == id)
                {
                    DrawParts[idp].ShowMode = showMode;
                    System.Diagnostics.Debug.Assert(flag == false);
                    flag = true;
                }
            }
        }

        public int GetShowMode(CadElementType type, uint id)
        {
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                if (DrawParts[idp].Type == type && DrawParts[idp].CadId == id)
                {
                    return DrawParts[idp].ShowMode;
                }
            }
            return 0;
        }

        public void ClearShowMode(int curShowMode)
        {
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                if (DrawParts[idp].ShowMode == curShowMode)
                {
                    DrawParts[idp].ShowMode = 0;
                }
            }
        }

        public void SetIsShow(bool isShow, CadElementType cadPartType, uint cadPartId)
        {
            int mode = (isShow) ? 0 : -1;
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                if (DrawParts[idp].CadId == cadPartId &&
                    DrawParts[idp].Type == cadPartType)
                {
                    DrawParts[idp].ShowMode = mode;
                }
            }
        }

        public void SetIsShow(bool isShow, CadElementType cadPartType, IList<uint> partIds)
        {
            for (int i = 0; i < partIds.Count; i++)
            {
                uint id = partIds[i];
                SetIsShow(isShow, cadPartType, id);
            }
        }

        public void AddSelected(int[] selectFlag)
        {
            CadElementType partType;
            uint partId;
            int ctrlIndex;
            GetPartCadId(selectFlag, out partType, out partId, out ctrlIndex);
            if (partType == CadElementType.NOT_SET)
            {
                return;
            }
            int showMode = 2;
            SetShowMode(partType, partId, showMode);
        }

        public void ClearSelected()
        {
            int curShowMode = 2;
            ClearShowMode(curShowMode);
        }
    }

}
