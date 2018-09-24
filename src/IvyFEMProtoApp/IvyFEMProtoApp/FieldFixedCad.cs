using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class FieldFixedCad
    {
        public uint CadId { get; set; } = 0;
        public CadElementType CadElemType { get; private set; } = CadElementType.NotSet;
        public FieldValueType ValueType { get; private set; } = FieldValueType.NoValue;
        public uint QuantityId { get; set; } = 0;
        public uint DofIndex { get; private set; } = 0;
        public double Value { get; set; } = 0;

        public FieldFixedCad()
        {

        }

        public FieldFixedCad(uint cadId, CadElementType cadElemType,
            FieldValueType valueType, uint quantityId, uint iDof, double value)
        {
            CadId = cadId;
            CadElemType = cadElemType;
            ValueType = valueType;
            QuantityId = quantityId;
            DofIndex = iDof;
            Value = value;
        }

        public FieldFixedCad(FieldFixedCad src)
        {
            CadId = src.CadId;
            CadElemType = src.CadElemType;
            ValueType = src.ValueType;
            QuantityId = src.QuantityId;
            DofIndex = src.DofIndex;
            Value = src.Value;
        }

        public IList<int> GetCoordIds(FEWorld world)
        {
            IList<int>coIds = world.GetCoordIdsFromCadId(QuantityId, (uint)CadId, CadElemType);
            return coIds;
        }
    }
}
