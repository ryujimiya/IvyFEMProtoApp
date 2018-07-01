using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class VertexArray
    {
        public double[] VertexCoordArray { get; set; } = null;
        public double[] UVCoordArray { get; set; } = null;
        public uint NPoint { get; private set; } = 0;
        public uint NDim { get; private set; } = 0;

        public VertexArray()
        {

        }

        public VertexArray(uint np, uint nd)
        {
            NPoint = np;
            NDim = nd;
            VertexCoordArray = new double[NPoint * NDim];
            UVCoordArray = null;
        }

        public void SetSize(uint nPoint, uint nDim)
        {
            if (NPoint == nPoint && NDim == nDim)
            {
                return;
            }
            NPoint = nPoint;
            NDim = nDim;
            VertexCoordArray = new double[nPoint * nDim];
            UVCoordArray = new double[nPoint * 2];
        }

        public BoundingBox3D GetBoundingBox(double[] rot)
        {
            if (VertexCoordArray == null)
            {
                return new BoundingBox3D();
            }
            if (rot == null)
            {
                if (NDim == 2)
                {
                    BoundingBox3D bb;
                    {
                        double x1 = VertexCoordArray[0];
                        double y1 = VertexCoordArray[1];
                        double z1 = 0.0;
                        bb = new BoundingBox3D(x1, x1, y1, y1, z1, z1);
                    }
                    for (uint ipoin = 1; ipoin < NPoint; ipoin++)
                    {
                        double x1 = VertexCoordArray[ipoin * 2];
                        double y1 = VertexCoordArray[ipoin * 2 + 1];
                        double z1 = 0.0;
                        bb.MaxX = (x1 > bb.MaxX) ? x1 : bb.MaxX; bb.MinX = (x1 < bb.MinX) ? x1 : bb.MinX;
                        bb.MaxY = (y1 > bb.MaxY) ? y1 : bb.MaxY; bb.MinY = (y1 < bb.MinY) ? y1 : bb.MinY;
                        bb.MaxZ = (z1 > bb.MaxZ) ? z1 : bb.MaxZ; bb.MinZ = (z1 < bb.MinZ) ? z1 : bb.MinZ;
                    }
                    return bb;
                }
                if (NDim == 3)
                {
                    BoundingBox3D bb;
                    {
                        double x1 = VertexCoordArray[0];
                        double y1 = VertexCoordArray[1];
                        double z1 = 0.0;
                        bb = new BoundingBox3D(x1, x1, y1, y1, z1, z1);
                    }
                    for (uint ipoin = 1; ipoin < NPoint; ipoin++)
                    {
                        double x1 = VertexCoordArray[ipoin * 3];
                        double y1 = VertexCoordArray[ipoin * 3 + 1];
                        double z1 = VertexCoordArray[ipoin * 3 + 2];
                        bb.MaxX = (x1 > bb.MaxX) ? x1 : bb.MaxX; bb.MinX = (x1 < bb.MinX) ? x1 : bb.MinX;
                        bb.MaxY = (y1 > bb.MaxY) ? y1 : bb.MaxY; bb.MinY = (y1 < bb.MinY) ? y1 : bb.MinY;
                        bb.MaxZ = (z1 > bb.MaxZ) ? z1 : bb.MaxZ; bb.MinZ = (z1 < bb.MinZ) ? z1 : bb.MinZ;
                    }
                    return bb;
                }
            }
            if (NDim == 2)
            {
                double minX;
                double maxX;
                double minY;
                double maxY;
                double minZ;
                double maxZ;
                {
                    double x1 = VertexCoordArray[0];
                    double y1 = VertexCoordArray[1];
                    double z1 = 0.0;
                    minX = maxX = x1 * rot[0] + y1 * rot[1] + z1 * rot[2];
                    minY = maxY = x1 * rot[3] + y1 * rot[4] + z1 * rot[5];
                    minZ = maxZ = x1 * rot[6] + y1 * rot[7] + z1 * rot[8];
                }
                for (uint ipoin = 1; ipoin < NPoint; ipoin++)
                {
                    double x1 = VertexCoordArray[ipoin * 2];
                    double y1 = VertexCoordArray[ipoin * 2 + 1];
                    double z1 = 0.0;
                    double x2 = x1 * rot[0] + y1 * rot[1] + z1 * rot[2];
                    double y2 = x1 * rot[3] + y1 * rot[4] + z1 * rot[5];
                    double z2 = x1 * rot[6] + y1 * rot[7] + z1 * rot[8];
                    maxX = (x2 > maxX) ? x2 : maxX; minX = (x2 < minX) ? x2 : minX;
                    maxY = (y2 > maxY) ? y2 : maxY; minY = (y2 < minY) ? y2 : minY;
                    maxZ = (z2 > maxZ) ? z2 : maxZ; minZ = (z2 < minZ) ? z2 : minZ;
                }

                double c1X = (minX + maxX) * 0.5;
                double c1Y = (minY + maxY) * 0.5;
                double c1Z = (minZ + maxZ) * 0.5;
                double c2X = c1X * rot[0] + c1Y * rot[3] + c1Z * rot[6];
                double c2Y = c1X * rot[1] + c1Y * rot[4] + c1Z * rot[7];
                double c2Z = c1X * rot[2] + c1Y * rot[5] + c1Z * rot[8];
                double hX = (maxX - minX) * 0.5;
                double hY = (maxY - minY) * 0.5;
                double hZ = (maxZ - minZ) * 0.5;
                BoundingBox3D bb = new BoundingBox3D(
                    c2X - hX, c2X + hX, 
                    c2Y - hY, c2Y + hY, 
                    c2Z - hZ, c2Z + hZ);
                return bb;
            }
            if (NDim == 3) // view axis alligned bounding box
            {
                double minX;
                double maxX;
                double minY;
                double maxY;
                double minZ;
                double maxZ;
                {
                    double x1 = VertexCoordArray[0];
                    double y1 = VertexCoordArray[1];
                    double z1 = VertexCoordArray[2];
                    minX = maxX = x1 * rot[0] + y1 * rot[1] + z1 * rot[2];
                    minY = maxY = x1 * rot[3] + y1 * rot[4] + z1 * rot[5];
                    minZ = maxZ = x1 * rot[6] + y1 * rot[7] + z1 * rot[8];
                }
                for (uint ipoin = 1; ipoin < NPoint; ipoin++)
                {
                    double x1 = VertexCoordArray[ipoin * 3];
                    double y1 = VertexCoordArray[ipoin * 3 + 1];
                    double z1 = VertexCoordArray[ipoin * 3 + 2];
                    double x2 = x1 * rot[0] + y1 * rot[1] + z1 * rot[2];
                    double y2 = x1 * rot[3] + y1 * rot[4] + z1 * rot[5];
                    double z2 = x1 * rot[6] + y1 * rot[7] + z1 * rot[8];
                    maxX = (x2 > maxX) ? x2 : maxX; minX = (x2 < minX) ? x2 : minX;
                    maxY = (y2 > maxY) ? y2 : maxY; minY = (y2 < minY) ? y2 : minY;
                    maxZ = (z2 > maxZ) ? z2 : maxZ; minZ = (z2 < minZ) ? z2 : minZ;
                }

                double c1X = (minX + maxX) * 0.5;
                double c1Y = (minY + maxY) * 0.5;
                double c1Z = (minZ + maxZ) * 0.5;
                double c2X = c1X * rot[0] + c1Y * rot[3] + c1Z * rot[6];
                double c2Y = c1X * rot[1] + c1Y * rot[4] + c1Z * rot[7];
                double c2Z = c1X * rot[2] + c1Y * rot[5] + c1Z * rot[8];
                double hX = (maxX - minX) * 0.5;
                double hY = (maxY - minY) * 0.5;
                double hZ = (maxZ - minZ) * 0.5;
                BoundingBox3D bb = new BoundingBox3D(
                    c2X - hX, c2X + hX,
                    c2Y - hY, c2Y + hY,
                    c2Z - hZ, c2Z + hZ);
                return bb;
            }

            return new BoundingBox3D();
        }

        public void EnableUVMap(bool isUVMap)
        {
            if ((UVCoordArray != null) == isUVMap)
            {
                return;
            }
            if (isUVMap)
            {
                uint nPt = NPoint;
                UVCoordArray = new double[nPt * 2];
                for (uint i = 0; i < nPt * 2; i++)
                {
                    UVCoordArray[i] = 0;
                }
            }
            else
            {
                UVCoordArray = null;
                UVCoordArray = null;
            }
        }

    }
}
