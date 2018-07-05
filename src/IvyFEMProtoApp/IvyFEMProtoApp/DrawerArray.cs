using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class DrawerArray
    {
        public IList<IDrawer> Drawers { get; } = new List<IDrawer>();

        public DrawerArray()
        {

        }

        public void Add(IDrawer drawer)
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

        public void DrawSelection()
        {
            for (uint idraw = 0; idraw < Drawers.Count; idraw++)
            {
                Drawers[(int)idraw].DrawSelection(idraw);
            }
        }

        public void AddSelected(int[] selectFlg)
        {
            for (uint idraw = 0; idraw < Drawers.Count; idraw++)
            {
                Drawers[(int)idraw].AddSelected(selectFlg);
            }
        }

        public void ClearSelected()
        {
            for (uint idraw = 0; idraw < Drawers.Count; idraw++)
            {
                Drawers[(int)idraw].ClearSelected();
            }
        }

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

    }
}
