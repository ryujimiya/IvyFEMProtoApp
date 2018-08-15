using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class EdgeFieldDrawer : IFieldDrawer
    {
        public uint LineWidth { get; set; } = 1;
        private VertexArray VertexArray = new VertexArray();
        private IList<LineFE> lineFEs = new List<LineFE>();
        private uint LineCount => (uint)lineFEs.Count;
        private uint ValueId = 0;
        private FieldDerivationType ValueDt = FieldDerivationType.VALUE;
        private bool IsntDisplacementValue = false;
        public RotMode SutableRotMode { get; private set; } = RotMode.ROTMODE_NOT_SET;
        public bool IsAntiAliasing { get; set; } = false;

        public EdgeFieldDrawer()
        {

        }

        public EdgeFieldDrawer(uint valueId, FieldDerivationType valueDt, bool isntDisplacementValue,
            FEWorld world)
        {
            Set(valueId, valueDt, isntDisplacementValue, world);
        }

        private void Set(uint valueId, FieldDerivationType valueDt, bool isntDisplacementValue, FEWorld world)
        {
            var mesh = world.Mesh;

            if (!world.IsFieldValueId(valueId))
            {
                throw new ArgumentException();
                return;
            }

            ValueId = valueId;
            ValueDt = valueDt;
            IsntDisplacementValue = isntDisplacementValue;

            // 線要素を生成
            lineFEs = world.MakeBoundOfElements();

            uint ptCnt = LineCount * 2;
            var fv = world.GetFieldValue(valueId);
            uint dim = world.Dimension;

            uint drawDim;
            if (!IsntDisplacementValue
                && dim == 2
                && (fv.Type == FieldValueType.SCALAR || fv.Type == FieldValueType.ZSCALAR))
            {
                drawDim = 3;
            }
            else
            {
                drawDim = dim;
            }
            VertexArray.SetSize(ptCnt, drawDim);

            if (drawDim == 2) { SutableRotMode = RotMode.ROTMODE_2D; }
            else if (dim == 3) { SutableRotMode = RotMode.ROTMODE_3D; }
            else { SutableRotMode = RotMode.ROTMODE_2DH; }


            Update(world);
        }

        public void Update(FEWorld world)
        {
            FieldValue fv = world.GetFieldValue(ValueId);
            uint dim = world.Dimension;
            uint ptCnt = LineCount * 2;
            uint lineCnt = LineCount;
            uint drawDim = VertexArray.Dimension;

            if (IsntDisplacementValue)
            {
                System.Diagnostics.Debug.Assert(drawDim == 2); // いまはそうなってる
                for (int iEdge = 0; iEdge < lineCnt; iEdge++)
                {
                    LineFE lineFE = lineFEs[iEdge];
                    int coId1 = lineFE.CoordIds[0];
                    int coId2 = lineFE.CoordIds[1];
                    double[] co1 = world.GetCoord(coId1);
                    double[] co2 = world.GetCoord(coId2);
                    VertexArray.VertexCoordArray[iEdge * 2 * drawDim] = co1[0];
                    VertexArray.VertexCoordArray[iEdge * 2 * drawDim + 1] = co1[1];
                    VertexArray.VertexCoordArray[iEdge * 2 * drawDim + drawDim] = co2[0];
                    VertexArray.VertexCoordArray[iEdge * 2 * drawDim + drawDim + 1] = co2[1];
                }
            }
            else
            {
                // 変位を伴う場合

                if (dim == 2 && drawDim == 3)
                {
                    for (int iEdge = 0; iEdge < LineCount; iEdge++)
                    {
                        LineFE lineFE = lineFEs[iEdge];
                        System.Diagnostics.Debug.Assert(lineFE.CoordIds.Length == 2);
                        for (int iPt = 0; iPt < 2; iPt++)
                        {
                            int coId = lineFE.CoordIds[iPt];
                            double[] coord = world.GetCoord(coId);
                            FieldDerivationType dt = ValueDt;
                            double value = fv.GetShowValue(coId, 0, dt);
                            VertexArray.VertexCoordArray[(iEdge * 2 + iPt) * drawDim + 0] = coord[0];
                            VertexArray.VertexCoordArray[(iEdge * 2 + iPt) * drawDim + 1] = coord[1];
                            VertexArray.VertexCoordArray[(iEdge * 2 + iPt) * drawDim + 2] = value;
                        }
                    }
                }
                else
                {
                    for (int iEdge = 0; iEdge < lineCnt; iEdge++)
                    {
                        LineFE lineFE = lineFEs[iEdge];
                        System.Diagnostics.Debug.Assert(lineFE.CoordIds.Length == 2);
                        for (int iPt = 0; iPt < 2; iPt++)
                        {
                            int coId = lineFE.CoordIds[iPt];
                            double[] coord = world.GetCoord(coId);
                            FieldDerivationType dt = ValueDt;
                            for (int iDim = 0; iDim < drawDim; iDim++)
                            {
                                double value = fv.GetShowValue(coId, iDim, dt);
                                VertexArray.VertexCoordArray[(iEdge * 2 + iPt) * drawDim + iDim] =
                                    coord[iDim] + value;
                            }
                        }
                    }
                }
            }
        }

        public void Draw()
        {
            if (LineCount == 0)
            {
                return;
            }

            bool isTexture = GL.IsEnabled(EnableCap.Texture2D);
            bool isLighting = GL.IsEnabled(EnableCap.Lighting);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);

            GL.Color3(0.0, 0.0, 0.0);
            GL.LineWidth(LineWidth);

            uint drawDim = VertexArray.Dimension;
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer((int)drawDim, VertexPointerType.Double, 0, VertexArray.VertexCoordArray);
            if (drawDim == 2)
            {
                GL.Translate(0, 0, +0.01);
            }

            GL.DrawArrays(PrimitiveType.Lines, 0, (int)LineCount * 2);
            if (drawDim == 2)
            {
                GL.Translate(0, 0, -0.01);
            }

            GL.DisableClientState(ArrayCap.VertexArray);
            if (isTexture)
            {
                GL.Enable(EnableCap.Texture2D);
            }
            if (isLighting)
            {
                GL.Enable(EnableCap.Lighting);
            }
        }

        public void DrawSelection(uint idraw)
        {
            throw new NotImplementedException();
        }

        public void AddSelected(int[] selectFlag)
        {
            throw new NotImplementedException();
        }

        public void ClearSelected()
        {
            throw new NotImplementedException();
        }

        public BoundingBox3D GetBoundingBox(OpenTK.Matrix3d rot)
        {
            return VertexArray.GetBoundingBox(rot);
        }

    }
}
