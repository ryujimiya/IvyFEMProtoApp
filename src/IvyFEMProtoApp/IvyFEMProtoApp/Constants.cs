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
        RotModeNotSet,
        RotMode2D,
        RotMode2DH,
        RotMode3D
    }

    enum CurveType
    {
        CurveEndPoint,
        CurveLine,
        CurveArc,
        CurvePolyline,
        CurveBezier
    }

    enum CadElementType
    {
        NotSet,
        Vertex,
        Edge,
        Loop
    }

    enum MeshType
    {
        NotSet,
        Vertex,
        Bar,
        Tri,
        Quad,
        Tet,
        Hex
    }

    enum ElementType
    {
        NotSet,
        Point,
        Line,
        Tri,
        Quad,
        Tet,
        Hex
    }

    enum FieldValueType
    {
        NoValue,
        Scalar,
        Vector2,
        Vector3,
        SymmetricTensor2,
        ZScalar
    }

    [Flags]
    enum FieldDerivativeType
    {
        Value = 1,
        Velocity = 2,
        Acceleration = 4
    }

    enum FieldShowType
    {
        Real,
        Abs,
        ZReal,
        ZImaginary,
        ZAbs
    }

    enum VectorFieldDrawerType
    {
        NotSet,
        Vector,
        SymmetricTensor2
    }

    enum LineIntegrationPointCount
    {
        Point1 = 1,
        Point2 = 2,
        Point3 = 3,
        Point4 = 4,
        Point5 = 5
    }

    enum TriangleIntegrationPointCount
    {
        Point1 = 1,
        Point3 = 3,
        Point4 = 4,
        Point7 = 7
    }    

    enum EqualityType
    {
        Eq,
        LessEq,
        GreaterEq
    }
}
