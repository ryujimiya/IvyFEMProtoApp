using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class MultipointConstraint
    {
        public FieldFixedCad FixedCad { get; set; } = null;
        public Constraint Constraint { get; set; } = null;

        public MultipointConstraint()
        {

        }

        public MultipointConstraint(uint cadId, CadElementType cadElemType, Constraint constraint)
        {
            uint dof = 1;
            FixedCad = new FieldFixedCad(cadId, cadElemType, FieldValueType.Scalar, dof);
            Constraint = constraint;
        }
    }
}
