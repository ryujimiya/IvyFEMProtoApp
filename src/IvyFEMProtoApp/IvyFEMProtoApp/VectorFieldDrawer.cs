﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class VectorFieldDrawer : IFieldDrawer
    {
        private IList<VectorFieldDrawPart> DrawParts = new List<VectorFieldDrawPart>();
        private uint ValueId = 0;
        private FieldDerivationType ValueDt = FieldDerivationType.VALUE;

        public RotMode SutableRotMode { get; private set; } = RotMode.ROTMODE_NOT_SET;
        public bool IsAntiAliasing { get; set; } = false;

        public VectorFieldDrawerType Type { get; private set; } = VectorFieldDrawerType.NOT_SET;

        public VectorFieldDrawer() : base()
        {

        }

        public VectorFieldDrawer(uint valueId, FieldDerivationType valueDt, FEWorld world)
        {
            Set(valueId, valueDt, world);
        }

        private void Set(uint valueId, FieldDerivationType valueDt, FEWorld world)
        {
            System.Diagnostics.Debug.Assert(world.IsFieldValueId(valueId));
            ValueId = valueId;
            var mesh = world.Mesh;

            uint dim = world.Dimension;
            {
                if (dim == 2)
                {
                    SutableRotMode = RotMode.ROTMODE_2D;
                }
                else if (dim == 3)
                {
                    SutableRotMode = RotMode.ROTMODE_3D;
                }
            }
            FieldValue fv = world.GetFieldValue(valueId);
            if (fv.Type == FieldValueType.VECTOR2 || fv.Type == FieldValueType.VECTOR3)
            {
                Type = VectorFieldDrawerType.VECTOR;
            }
            else if (fv.Type == FieldValueType.SYMMETRICAL_TENSOR2)
            {
                Type = VectorFieldDrawerType.SYMMETRIC_TENSOR2;
            }

            {
                DrawParts.Clear();
                IList<uint> meshIds = mesh.GetIds();
                foreach (uint meshId in meshIds)
                {
                    VectorFieldDrawPart dp = new VectorFieldDrawPart(meshId, world);
                    DrawParts.Add(dp);
                }
            }

            Update(world);
        }

        public void Update(FEWorld world)
        {
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                VectorFieldDrawPart dp = DrawParts[idp];
                dp.Update(ValueId, ValueDt, Type, world);
            }
        }

        public void Draw()
        {
            bool isTexture = GL.IsEnabled(EnableCap.Texture2D);
            //GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Texture2D);

            int minLayer;
            int maxLayer;
            {
                if (DrawParts.Count > 0)
                {
                    minLayer = DrawParts[0].Layer;
                    maxLayer = minLayer;
                }
                else
                {
                    minLayer = 0; maxLayer = 0;
                }
                for (int idp = 1; idp < DrawParts.Count; idp++)
                {
                    int layer = DrawParts[idp].Layer;
                    minLayer = (layer < minLayer) ? layer : minLayer;
                    maxLayer = (layer > maxLayer) ? layer : maxLayer;
                }
            }
            double layerHeight = 1.0 / (maxLayer - minLayer + 1);

            GL.LineWidth(2);
            GL.Begin(PrimitiveType.Lines);
            for (int idp = 0; idp < DrawParts.Count; idp++)
            {
                VectorFieldDrawPart dp = DrawParts[idp];
                int layer = dp.Layer;
                double height = (layer - minLayer) * layerHeight;
                GL.Translate(0, 0, +height);
                dp.DrawElements();
                GL.Translate(0, 0, -height);
            }
            GL.End();

            if (isTexture)
            {
                GL.Enable(EnableCap.Texture2D);
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

        public BoundingBox3D GetBoundingBox(double[] rot)
        {
            //throw new NotImplementedException();
            return new BoundingBox3D();
        }

    }
}
