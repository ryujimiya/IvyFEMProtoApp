using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class Material : IObject
    {
        public MaterialType MaterialType { get; protected set; } = MaterialType.NOT_SET; 
        public uint Length { get; protected set; } = 0;
        public double[] Values { get; protected set; } = null;

        public Material()
        {

        }

        public Material(Material src)
        {
            Copy(src);
        }

        public void Copy(IObject src)
        {
            Material srcMa = src as Material;
            MaterialType = srcMa.MaterialType;
            Length = srcMa.Length;
            Values = null;
            if (srcMa.Length > 0)
            {
                Values = new double[srcMa.Length];
                for (int i = 0; i < srcMa.Length; i++)
                {
                    Values[i] = srcMa.Values[i];
                }

            }
        }

    }
}
