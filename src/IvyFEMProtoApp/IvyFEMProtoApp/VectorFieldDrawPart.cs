using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class VectorFieldDrawPart
    {
        public uint MeshId { get; set; } = 0;
        public ElementType Type { get; private set; } = ElementType.NOT_SET;
        public int Layer { get; set; } = 0;
        public uint ElemCount { get; set; } = 0;
        public uint ValueDof { get; private set; } = 0;
        public double[] Coords { get; set; } = null;
        public double[] Values { get; set; } = null;
        public VectorFieldDrawerType DrawerType { get; private set; } = VectorFieldDrawerType.NOT_SET;

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

        public VectorFieldDrawPart()
        {

        }

        public VectorFieldDrawPart(VectorFieldDrawPart src)
        {
            MeshId = src.MeshId;
            Type = src.Type;
            Layer = src.Layer;
            ElemCount = src.ElemCount;
            ValueDof = src.ValueDof;
            Coords = null;
            if (src.Coords != null)
            {
                Coords = new double[src.Coords.Length];
                src.Coords.CopyTo(Coords, 0);
            }
            Values = null;
            if (src.Values != null)
            {
                Values = new double[src.Values.Length];
                src.Values.CopyTo(Values, 0);
            }
            DrawerType = src.DrawerType;
        }


        public VectorFieldDrawPart(uint meshId, FEWorld world)
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
            }
            else if (meshType == MeshType.BAR)
            {
                Type = ElementType.LINE;
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

            // TODO: あとで
            //throw new NotImplementedException();
        }

        private void SetTri(FEWorld world)
        {
            System.Diagnostics.Debug.Assert(Type == ElementType.TRI);
            if (Type != ElementType.TRI)
            {
                return;
            }
            var mesh = world.Mesh;

            int elemPtCount = 3;
            MeshType meshType;
            int[] vertexs;
            mesh.GetConnectivity(MeshId, out meshType, out vertexs);
            System.Diagnostics.Debug.Assert(elemPtCount * ElemCount == vertexs.Length);

            uint dim = Dimension;
            System.Diagnostics.Debug.Assert(dim == 2);
            Coords = new double[ElemCount * dim];
            for (int iTri = 0; iTri < ElemCount; iTri++)
            {
                double[] bubbleCoord = new double[dim];
                for (int iPt = 0; iPt < elemPtCount; iPt++)
                {
                    int coId = vertexs[iTri * elemPtCount + iPt];
                    double[] coord = world.GetCoord(coId);
                    for (int iDimTmp = 0; iDimTmp < dim; iDimTmp++)
                    {
                        bubbleCoord[iDimTmp] += coord[iDimTmp];
                    }
                }
                for (int iDim = 0; iDim < dim; iDim++)
                {
                    bubbleCoord[iDim] /= elemPtCount;

                    Coords[iTri * dim + iDim] = bubbleCoord[iDim];
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

            // TODO: あとで
            //throw new NotImplementedException();
        }

        public void Update(uint valueId, FieldDerivationType dt, VectorFieldDrawerType drawerType, FEWorld world)
        {
            DrawerType = drawerType;

            if (DrawerType == VectorFieldDrawerType.VECTOR)
            {
                UpdateVector(valueId, dt, world);
            }
            else if (DrawerType == VectorFieldDrawerType.SYMMETRIC_TENSOR2)
            {
                UpdateSymmetricTensor2(valueId, dt, world);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void UpdateVector(uint valueId, FieldDerivationType dt, FEWorld world)
        {
            ValueDof = 2;

            FieldValue fv = world.GetFieldValue(valueId);
            uint dof = fv.Dof;
            System.Diagnostics.Debug.Assert(fv.IsBubble == true);
            var mesh = world.Mesh;
            MeshType meshType;
            int[] vertexs;
            mesh.GetConnectivity(MeshId, out meshType, out vertexs);

            if (Type == ElementType.TRI)
            {
                Values = new double[ElemCount * ValueDof];
                for (int iTri = 0; iTri < ElemCount; iTri++)
                {
                    // Bubble
                    uint feId = world.GetTriangleFEIdFromMesh(MeshId, (uint)iTri);
                    System.Diagnostics.Debug.Assert(feId != 0);
                    System.Diagnostics.Debug.Assert(dof >= ValueDof);
                    for (int iDof = 0; iDof < ValueDof; iDof++)
                    {
                        double u = fv.GetShowValue((int)(feId - 1), iDof, dt);
                        Values[iTri * ValueDof + iDof] = u;
                    }
                }
            }
            else if (Type == ElementType.QUAD)
            {
                // TRIと同じでよいが要素IDを取得するメソッドが現状ない
                throw new NotImplementedException();
            }
        }

        private void UpdateSymmetricTensor2(uint valueId, FieldDerivationType dt, FEWorld world)
        {
            ValueDof = 6;

            FieldValue fv = world.GetFieldValue(valueId);
            uint dof = fv.Dof;
            System.Diagnostics.Debug.Assert(fv.IsBubble == true);
            var mesh = world.Mesh;
            MeshType meshType;
            int[] vertexs;
            mesh.GetConnectivity(MeshId, out meshType, out vertexs);

            if (Type == ElementType.TRI)
            {
                Values = new double[ElemCount * ValueDof];
                for (int iTri = 0; iTri < ElemCount; iTri++)
                {
                    // Bubble
                    uint feId = world.GetTriangleFEIdFromMesh(MeshId, (uint)iTri);
                    System.Diagnostics.Debug.Assert(feId != 0);
                    double[] sigma = new double[dof];
                    for (int iDof = 0; iDof < dof; iDof++)
                    {
                        sigma[iDof] = fv.GetShowValue((int)(feId - 1), iDof, dt);
                    }

                    double[] vecs;
                    double ls;
                    double[] vecl;
                    double ll;
                    GetPrincipleVectorForSymmetricTensor2(sigma,
                        out vecs, out ls,
                        out vecl, out ll);
                    Values[iTri * ValueDof + 0] = vecs[0];
                    Values[iTri * ValueDof + 1] = vecs[1];
                    Values[iTri * ValueDof + 2] = ls;
                    Values[iTri * ValueDof + 3] = vecl[0];
                    Values[iTri * ValueDof + 4] = vecl[1];
                    Values[iTri * ValueDof + 5] = ll;
                }
            }
            else if (Type == ElementType.QUAD)
            {
                // TRIと同じでよいが要素IDを取得するメソッドが現状ない
                throw new NotImplementedException();
            }
        }

        private void GetPrincipleVectorForSymmetricTensor2(double[] sigma, 
            out double[] vecs, out double ls,
            out double[] vecl, out double ll)
        {
            vecs = new double[2];
            ls = 0;
            vecl = new double[2];
            ll = 0;
            {
                double tmp1 = Math.Sqrt((sigma[0] - sigma[1]) * (sigma[0] - sigma[1]) + 4 * sigma[2] * sigma[2]);
                double tmp2 = sigma[0] + sigma[1];
                double l1 = 0.5 * (tmp2 - tmp1);
                double l2 = 0.5 * (tmp2 + tmp1);
                if (Math.Abs(l1) > Math.Abs(l2))
                {
                    ll = l1;
                    ls = l2;
                }
                else
                {
                    ll = l2;
                    ls = l1;
                }
            }
            {
                double[] candl1 = { -sigma[2], sigma[0] - ls };
                double[] candl2 = { sigma[1] - ls, -sigma[2] };
                double sqlen1 = candl1[0] * candl1[0] + candl1[1] * candl1[1];
                double sqlen2 = candl2[0] * candl2[0] + candl2[1] * candl2[1];
                if (sqlen1 > sqlen2)
                {
                    vecs[0] = candl1[0];
                    vecs[1] = candl1[1];
                }
                else
                {
                    vecs[0] = candl2[0];
                    vecs[1] = candl2[1];
                }
                double len = Math.Sqrt(vecs[0] * vecs[0] + vecs[1] * vecs[1]);
                if (len < 1.0e-10)
                {
                    vecs[0] = 0;
                    vecs[1] = 0;
                }
                else
                {
                    double normalizer = ls / len;
                    vecs[0] *= normalizer;
                    vecs[1] *= normalizer;
                }
            }
            {
                double[] candl1 = { -sigma[2], sigma[0] - ll };
                double[] candl2 = { sigma[1] - ll, -sigma[2] };
                double sqlen1 = candl1[0] * candl1[0] + candl1[1] * candl1[1];
                double sqlen2 = candl2[0] * candl2[0] + candl2[1] * candl2[1];
                if (sqlen1 > sqlen2) { vecl[0] = candl1[0]; vecl[1] = candl1[1]; }
                else { vecl[0] = candl2[0]; vecl[1] = candl2[1]; }
                double len = Math.Sqrt(vecl[0] * vecl[0] + vecl[1] * vecl[1]);
                if (len < 1.0e-10)
                {
                    vecl[0] = 0; vecl[1] = 0;
                }
                else
                {
                    double normalizer = ll / len;
                    vecl[0] *= normalizer;
                    vecl[1] *= normalizer;
                }
            }
        }

        public void DrawElements()
        {
            if (Type != ElementType.TRI)
            {
                // TODO: あとで
                return;
            }

            uint dim = Dimension;
            if (dim == 2)
            {
                if (DrawerType == VectorFieldDrawerType.VECTOR)
                {
                    System.Diagnostics.Debug.Assert(ValueDof == 2);
                    for (int iElem = 0; iElem < ElemCount; iElem++)
                    {
                        double[] co = { Coords[iElem * dim], Coords[iElem * dim + 1] };
                        double[] va = new double[ValueDof];
                        for (int iDof = 0; iDof < ValueDof; iDof++)
                        {
                            va[iDof] = Values[iElem * ValueDof + iDof];
                        }
                        GL.Vertex2(co);
                        GL.Vertex2(co[0] + va[0], co[1] + va[1]);
                    }
                }
                else if (DrawerType == VectorFieldDrawerType.SYMMETRIC_TENSOR2)
                {
                    System.Diagnostics.Debug.Assert(ValueDof == 6);
                    for (int iElem = 0; iElem < ElemCount; iElem++)
                    {
                        double[] co = { Coords[iElem * dim], Coords[iElem * dim + 1] };
                        double[] va = new double[ValueDof];
                        for (int iDof = 0; iDof < ValueDof; iDof++)
                        {
                            va[iDof] = Values[iElem * ValueDof + iDof];
                        }

                        if (va[2] > 0)
                        {
                            GL.Color3(0.0, 0.0, 1.0);
                        }
                        else
                        {
                            GL.Color3(1.0, 0.0, 0.0);
                        }
                        GL.Vertex2(co);
                        GL.Vertex2(co[0] + va[0], co[1] + va[1]);

                        GL.Vertex2(co);
                        GL.Vertex2(co[0] - va[0], co[1] - va[1]);

                        if (va[5] > 0)
                        {
                            GL.Color3(0.0, 0.0, 1.0);
                        }
                        else
                        {
                            GL.Color3(1.0, 0.0, 0.0);
                        }
                        GL.Vertex2(co);
                        GL.Vertex2(co[0] + va[3], co[1] + va[4]);

                        GL.Vertex2(co);
                        GL.Vertex2(co[0] - va[3], co[1] - va[4]);
                    }
                }
            }
            else if (dim == 3)
            {
                // TODO: あとで
                throw new NotImplementedException();
            }
        }
    }
}
