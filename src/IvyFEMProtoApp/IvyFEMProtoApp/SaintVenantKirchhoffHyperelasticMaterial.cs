using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class SaintVenantKirchhoffHyperelasticMaterial : ElasticBaseMaterial
    {
        public SaintVenantKirchhoffHyperelasticMaterial() : base()
        {
            MaterialType = MaterialType.SAINTVENANT_KIRCHHOFF_HYPERELASTIC;
        }
    }
}
