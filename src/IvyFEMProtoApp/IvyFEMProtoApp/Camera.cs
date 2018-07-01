using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    enum RotationMode
    {
        ROT_2D,     // 2dim rotation
        ROT_2DH,    // z axis is allways pararell to the upright direction of screan
        ROT_3D      // track ball rotation
    };

    class Camera
    {
        public double WindowAspect { get; set; }

        private double WinCenterX;
        private double WinCenterY;
        private double InvScale;
        public double Scale
        {
            get
            {
                return GetScale();
            }
            set
            {
                SetScale(value);
            }
        }
        public double HalfViewHeight { get; private set; }

        private double ObjW;
        private double ObjH;
        private double ObjD;
        private Vector3 ObjCenter;

        public RotationMode RotationMode { get; set; }
        private Quaternion RotQuat;
        private double Theta;
        private double Phi;

        private bool isPers;
        public bool IsPers
        {
            get
            {
                return isPers;
            }
            set
            {
                SetIsPers(value);
            }
        }
        private double FovY;
        private double Dist;

        public Camera()
        {
            WindowAspect = 1.0;

            HalfViewHeight = 1.0;
            InvScale = 1.0;
            WinCenterX = 0.0; WinCenterY = 0.0;

            ObjW = 1.0; ObjH = 1.0; ObjD = 1.0;
            ObjCenter = Vector3.Zero;

            RotationMode = RotationMode.ROT_2D;
            Theta = 0;
            Phi = -60.0 * 3.1415926 / 180.0;
            RotQuat = Quaternion.Identity;

            IsPers = false;
            FovY = 30.0 * 3.1415926 / 180.0;
            Dist = HalfViewHeight / Math.Tan(FovY * 0.5) + ObjD * 0.5;
        }

        public void RotMatrix33(double[] m)
        {
            if (RotationMode == RotationMode.ROT_2D)
            {
                double ct = Math.Cos(Theta);
                double st = Math.Sin(Theta);
                m[0] = ct;
                m[1] = -st;
                m[2] = 0.0;
                m[3] = st;
                m[4] = ct;
                m[5] = 0.0;
                m[6] = 0.0;
                m[7] = 0.0;
                m[8] = 1.0;
            }
            else if (RotationMode == RotationMode.ROT_2DH)
            {
                double ct = Math.Cos(Theta);
                double st = Math.Sin(Theta);
                double cp = Math.Cos(Phi);
                double sp = Math.Sin(Phi);
                m[0] = ct;
                m[1] = -st;
                m[2] = 0.0;
                m[3] = cp * st;
                m[4] = cp * ct;
                m[5] = -sp;
                m[6] = sp * st;
                m[7] = sp * ct;
                m[8] = cp;
            }
            else if (RotationMode == RotationMode.ROT_3D)
            {
                CadUtils.RotMatrix33(RotQuat, m);
            }
        }

        public void Fit()
        {
            const double margin = 1.5;
            double objAspect = ObjW / ObjH;
            InvScale = 1.0;
            if (objAspect < WindowAspect)
            {
                HalfViewHeight = ObjH * 0.5 * margin;
            }
            else
            {
                double tmpH = ObjW / WindowAspect;
                HalfViewHeight = tmpH * 0.5 * margin;
            }
            Dist = HalfViewHeight * InvScale / Math.Tan(FovY * 0.5) + ObjD * 0.5;
            WinCenterX = 0.0; WinCenterY = 0.0;
        }

        public void Fit(BoundingBox3D bb)
        {
            ObjCenter.X = (float)((bb.MinX + bb.MaxX) * 0.5);
            ObjCenter.Y = (float)((bb.MinY + bb.MaxY) * 0.5);
            ObjCenter.Z = (float)((bb.MinZ + bb.MaxZ) * 0.5);
            ObjW = bb.MaxX - bb.MinX;
            ObjH = bb.MaxY - bb.MinY;
            ObjD = bb.MaxZ - bb.MinZ;
            Fit();
        }

        private void SetIsPers(bool value)
        {
            if (RotationMode == RotationMode.ROT_2D)
            {
                return;
            }
            isPers = value;
            if (IsPers)
            {
                Dist = HalfViewHeight * InvScale / Math.Tan(FovY * 0.5) + ObjD * 0.5;
            }
        }

        public void GetPerspective(out double fovY, out double aspect,
            out double clipNear, out double clipFar)
        {
            fovY = this.FovY * 180.0 / 3.1415926;
            aspect = WindowAspect;
            clipNear = 0.001;
            clipFar = Dist * 2.0 + ObjD * 20.0 + 100.0;
        }

        private double GetScale()
        {
            return 1.0 / InvScale;
        }

        private void SetScale(double value)
        {
            if (value < 0.01)
            {
                InvScale = 100;
            }
            else if (value > 100.0)
            {
                InvScale = 0.01;
            }
            else
            {
                InvScale = 1.0 / value;
            }
            Dist = HalfViewHeight * InvScale / Math.Tan(FovY * 0.5) + ObjD * 0.5;
        }
    }
}
