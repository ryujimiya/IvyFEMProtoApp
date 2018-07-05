using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    enum CurveType
    {
        CURVE_END_POINT,
        CURVE_LINE,
        CURVE_ARC,
        CURVE_POLYLINE,
        CURVE_BEZIER
    }

    enum CadElemType
    {
        NOT_SET,
        VERTEX,
        EDGE,
        LOOP,
        SOLID,
    }

    /*
    enum RotationMode
    {
        ROT_2D,     // 2dim rotation
        ROT_2DH,    // z axis is allways pararell to the upright direction of screan
        ROT_3D      // track ball rotation
    }
    */

    enum MeshType
    {
        VERTEX,
        BAR,
        TRI,
        QUAD,
        TET,
        HEX
    }

    enum ElemType
    {
        NOT_SET = 0,
        POINT = 1,
        LINE = 2,
        TRI = 3,
        QUAD = 4,
        TET = 5,
        HEX = 6
    }
}
