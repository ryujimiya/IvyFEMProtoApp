using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class DrawerArray
    {
        public IList<CadObject2DDrawer> Drawers { get; } = new List<CadObject2DDrawer>();

        public DrawerArray()
        {

        }

        public void Add(CadObject2DDrawer drawer)
        {
            System.Diagnostics.Debug.Assert(drawer != null);
            Drawers.Add(drawer);
        }

        public void Clear()
        {
            Drawers.Clear();
        }

        public void Draw()
        {
            for (int idraw = 0; idraw < Drawers.Count; idraw++)
            {
                Drawers[idraw].Draw();
            }
        }

        /*
        public void DrawSelection()
        {
            for (int idraw = 0; idraw < Drawers.Count; idraw++)
            {
                Drawers[idraw].DrawSelection(idraw);
            }
        }
        */

        /*
        public void AddSelected(int[] selectFlg)
        {
            for (int idraw = 0; idraw < Drawers.Count; idraw++)
            {
                Drawers[idraw].AddSelected(selectFlg);
            }
        }
        */

        /*
        public void ClearSelected()
        {
            for (int idraw = 0; idraw < Drawers.Count; idraw++)
            {
                Drawers[idraw].ClearSelected();
            }
        }
        */

        public BoundingBox3D GetBoundingBox(double[] rot)
        {
            if (Drawers.Count == 0)
            {
                return new BoundingBox3D(-0.5, 0.5, -0.5, 0.5, -0.5, 0.5);
            }
            BoundingBox3D bb = Drawers[0].GetBoundingBox(rot);
            for (int idraw = 1; idraw < Drawers.Count; idraw++)
            {
                bb += Drawers[idraw].GetBoundingBox(rot);
            }
            return bb;
        }

        public void InitTransform(Camera camera)
        {
            {
                uint rotMode = 0;
                for (int idraw = 0; idraw < Drawers.Count; idraw++)
                {
                    uint rotMode0 = Drawers[idraw].SutableRotMode;
                    rotMode = (rotMode0 > rotMode) ? rotMode0 : rotMode;
                }
                if (rotMode == 1)
                {
                    camera.RotationMode = RotationMode.ROT_2D;
                }
                else if (rotMode == 2)
                {
                    camera.RotationMode = RotationMode.ROT_2DH;
                }
                else if (rotMode == 3)
                {
                    camera.RotationMode = RotationMode.ROT_3D;
                }
            }

            double[] rot = new double[9];
            camera.RotMatrix33(rot);
            BoundingBox3D bb = GetBoundingBox(rot);
            camera.Fit(bb);
        }

    }
}
