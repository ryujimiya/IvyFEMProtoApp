using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class Constants
    {
        public const double C0 = 2.99792458e+8;
        public const double Mu0 = 4.0e-7 * Math.PI;
        public const double Ep0 = 8.85418782e-12;//1.0 / (Mu0 * C0 * C0);

        public const double PrecisionLowerLimit = 1.0e-12;
    }

    enum RotMode
    {
        ROTMODE_NOT_SET,
        ROTMODE_2D,
        ROTMODE_2DH,
        ROTMODE_3D
    }

    enum CurveType
    {
        CURVE_END_POINT,
        CURVE_LINE,
        CURVE_ARC,
        CURVE_POLYLINE,
        CURVE_BEZIER
    }

    enum CadElementType
    {
        NOT_SET,
        VERTEX,
        EDGE,
        LOOP,
        SOLID,
    }

    enum MeshType
    {
        NOT_SET,
        VERTEX,
        BAR,
        TRI,
        QUAD,
        TET,
        HEX
    }

    enum ElementType
    {
        NOT_SET,
        POINT,
        LINE,
        TRI,
        QUAD,
        TET,
        HEX
    }

    enum MaterialType
    {
        NOT_SET,
        ELASTIC,
        DIELECTRIC
    }

    enum FieldValueType
    {
        NO_VALUE,
        SCALAR,
        VECTOR2,
        VECTOR3,
        // 2D symmetrical tensor
        STSR2,
        ZSCALAR
    }

    [Flags]
    enum FieldDerivationType
    {
        VALUE = 1,
        VELOCITY = 2,
        ACCELERATION = 4
    }

    enum FieldShowType
    {
        SCALAR,
        ABS,
        ZREAL,
        ZIMAGINARIY,
        ZABS
    }

}
