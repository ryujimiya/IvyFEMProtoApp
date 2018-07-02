using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    interface IDrawer
    {
        uint SutableRotMode { get; }
        bool IsAntiAliasing { get; set; }

        BoundingBox3D GetBoundingBox(double[] rot);

        void DrawSelection(uint idraw);
        void Draw();
        void AddSelected(int[] selectFlag);
        void ClearSelected();
    }
}
