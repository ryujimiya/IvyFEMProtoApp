using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class SaintVenantHyperelasticMaterial : ElasticBaseMaterial
    {
        public SaintVenantHyperelasticMaterial() : base()
        {
            MaterialType = MaterialType.SaintVenantHyperelastic;
        }
    }
}
