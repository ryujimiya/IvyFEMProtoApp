using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class FieldFixedCad
    {
        public int CadId { get; set; } = -1;
        public CadElementType CadElemType { get; private set; } = CadElementType.NOT_SET;
        public FieldValueType ValueType { get; private set; } = FieldValueType.NO_VALUE;
        public int DofIndex { get; private set; } = -1;
        public double Value { get; set; } = 0;

        public FieldFixedCad()
        {

        }

        public FieldFixedCad(uint cadId, CadElementType cadElemType, FieldValueType valueType, int iDof, double value)
        {
            CadId = (int)cadId;
            CadElemType = cadElemType;
            ValueType = valueType;
            DofIndex = iDof;
            Value = value;
        }

        public FieldFixedCad(FieldFixedCad src)
        {
            CadId = src.CadId;
            CadElemType = src.CadElemType;
            ValueType = src.ValueType;
            DofIndex = src.DofIndex;
            Value = src.Value;
        }

        public IList<int> GetCoordIds(FEWorld world)
        {
            IList<int>coIds = world.GetCoordIdFromCadId((uint)CadId, CadElemType);
            return coIds;
        }
    }
}
