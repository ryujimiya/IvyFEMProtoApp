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

        public Vector2 WindowCenter { get; set; } = new Vector2();
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
        public Vector3 ObjectCenter { get; set; } = new Vector3();

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
            WindowCenter = new Vector2(0.0f, 0.0f);

            ObjW = 1.0;
            ObjH = 1.0;
            ObjD = 1.0;
            ObjectCenter = Vector3.Zero;

            RotationMode = RotationMode.ROT_2D;
            Theta = 0;
            Phi = -60.0 * 3.1415926 / 180.0;
            RotQuat = Quaternion.Identity;

            IsPers = false;
            FovY = 30.0 * 3.1415926 / 180.0;
            Dist = HalfViewHeight / Math.Tan(FovY * 0.5) + ObjD * 0.5;
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

        public void RotMatrix33(double[] rot)
        {
            if (RotationMode == RotationMode.ROT_2D)
            {
                double ct = Math.Cos(Theta);
                double st = Math.Sin(Theta);
                rot[0] = ct;
                rot[1] = -st;
                rot[2] = 0.0;
                rot[3] = st;
                rot[4] = ct;
                rot[5] = 0.0;
                rot[6] = 0.0;
                rot[7] = 0.0;
                rot[8] = 1.0;
            }
            else if (RotationMode == RotationMode.ROT_2DH)
            {
                double ct = Math.Cos(Theta);
                double st = Math.Sin(Theta);
                double cp = Math.Cos(Phi);
                double sp = Math.Sin(Phi);
                rot[0] = ct;
                rot[1] = -st;
                rot[2] = 0.0;
                rot[3] = cp * st;
                rot[4] = cp * ct;
                rot[5] = -sp;
                rot[6] = sp * st;
                rot[7] = sp * ct;
                rot[8] = cp;
            }
            else if (RotationMode == RotationMode.ROT_3D)
            {
                CadUtils.RotMatrix33(RotQuat, rot);
            }
        }

        public void RotMatrix44Trans(double[] rot)
        {
            double[] rot1 = new double[9];
            RotMatrix33(rot1);

            rot[0] = rot1[0];
            rot[1] = rot1[3];
            rot[2] = rot1[6];
            rot[3] = 0.0;
            rot[4] = rot1[1];
            rot[5] = rot1[4];
            rot[6] = rot1[7];
            rot[7] = 0.0;
            rot[8] = rot1[2];
            rot[9] = rot1[5];
            rot[10] = rot1[8];
            rot[11] = 0.0;
            rot[12] = 0.0;
            rot[13] = 0.0;
            rot[14] = 0.0;
            rot[15] = 1.0;
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
            WindowCenter = new Vector2(0.0f, 0.0f);
        }

        public void Fit(BoundingBox3D bb)
        {
            ObjectCenter = new Vector3(
                (float)((bb.MinX + bb.MaxX) * 0.5),
                (float)((bb.MinY + bb.MaxY) * 0.5),
                (float)((bb.MinZ + bb.MaxZ) * 0.5));
            ObjW = bb.MaxX - bb.MinX;
            ObjH = bb.MaxY - bb.MinY;
            ObjD = bb.MaxZ - bb.MinZ;
            Fit();
        }

        public void GetCenterPosition(out double x, out double y, out double z)
        {
            if (IsPers)
            {
                x = 0.0;
                y = 0.0;
                z = -Dist;
            }
            else
            {
                x = HalfViewHeight * WindowCenter.X * WindowAspect;
                y = HalfViewHeight * WindowCenter.Y;
                z = 0.0;
            }
        }

        public Vector3 GetCenterPosition()
        {
            double x, y, z;
            GetCenterPosition(out x, out y, out z);
            return new Vector3((float)x, (float)y, (float)z);
        }

        public void MousePan(double mov_begin_x, double mov_begin_y, double mov_end_x, double mov_end_y)
        {
            Vector2 prev = new Vector2(WindowCenter.X, WindowCenter.Y);
            WindowCenter = new Vector2(
                (float)(prev.X + (mov_end_x - mov_begin_x) * InvScale),
                (float)(prev.Y + (mov_end_y - mov_begin_y) * InvScale));
        }

    }
}
