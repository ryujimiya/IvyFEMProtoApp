using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace IvyFEM
{
    abstract class Camera
    {
        public double WindowAspect { get; set; }

        public Vector2 WindowCenter { get; set; } = new Vector2();
        protected double InvScale;
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

        private double ObjectW;
        private double ObjectH;
        private double ObjectD;
        public Vector3 ObjectCenter { get; set; } = new Vector3();

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
        private double fovY;
        public double FovY
        {
            get
            {
                return GetFovY();
            }
            set
            {
                SetFovY(value);
            }
        }
        private double Dist;

        public Camera()
        {
            WindowAspect = 1.0;

            HalfViewHeight = 1.0;
            InvScale = 1.0;
            WindowCenter = new Vector2(0.0f, 0.0f);

            ObjectW = 1.0;
            ObjectH = 1.0;
            ObjectD = 1.0;
            ObjectCenter = Vector3.Zero;

            IsPers = false;
            fovY = 30.0 * Math.PI / 180.0;
            Dist = HalfViewHeight / Math.Tan(fovY * 0.5) + ObjectD * 0.5;
        }

        private void SetIsPers(bool value)
        {
            isPers = value;
            if (IsPers)
            {
                Dist = HalfViewHeight * InvScale / Math.Tan(fovY * 0.5) + ObjectD * 0.5;
            }
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
            Dist = HalfViewHeight * InvScale / Math.Tan(fovY * 0.5) + ObjectD * 0.5;
        }

        private double GetFovY()
        {
            return fovY * 180.0/Math.PI;
        }

        private void SetFovY(double fovY)
        {
            if (!IsPers)
            {
                return;
            }
            double _fovY = fovY;
            if (fovY < 15.0)
            {
                _fovY = 15.0;
            }
            else if (fovY > 90.0)
            {
                _fovY = 90.0;
            }
            else
            {
                _fovY = fovY;
            }
            this.fovY = _fovY * Math.PI / 180.0;
            Dist = HalfViewHeight * InvScale / Math.Tan(this.fovY * 0.5) + ObjectD * 0.5;
        }

        public void GetPerspective(out double fovY, out double aspect,
            out double clipNear, out double clipFar)
        {
            fovY = this.fovY * 180.0 / Math.PI;
            aspect = WindowAspect;
            clipNear = 0.001;
            clipFar = Dist * 2.0 + ObjectD * 20.0 + 100.0;
        }

        public void GetOrtho(out double hw, out double hh, out double hd)
        {
            hw = HalfViewHeight * InvScale * WindowAspect;
            hh = HalfViewHeight * InvScale;
            hd = (ObjectD * 20.0 > 1.0e-4) ? ObjectD * 20.0 : 1.0e-4;
            hd = (ObjectW * 20.0 > hd) ? ObjectW * 20.0 : hd;
            hd = (ObjectH * 20.0 > hd) ? ObjectH * 20.0 : hd;
        }

        public void GetCenterPosition(out double x, out double y, out double z)
        {
            if (IsPers)
            {
                System.Diagnostics.Debug.Assert(false);
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
            double x;
            double y;
            double z;
            GetCenterPosition(out x, out y, out z);
            return new Vector3((float)x, (float)y, (float)z);
        }

        public void GetObjectCenter(out double x, out double y, out double z)
    	{
            x = ObjectCenter.X;
            y = ObjectCenter.Y;
            z = ObjectCenter.Z;
        }

        public Vector3 GetObjectCenter()
        {
            return ObjectCenter;
        }

        public void SetObjectCenter(double x, double y, double z)
        {
            ObjectCenter = new Vector3((float)x, (float)y, (float)z);
        }

        public void GetObjectSize(out double w, out double h, out double d)
        {
            w = ObjectW;
            h = ObjectH;
            d = ObjectD;
        }

        public void SetObjectSize(double w, double h, double d)
        {
            ObjectW = w;
            ObjectH = h;
            ObjectD = d;
        }

        public void SetObjectBoundingBox(BoundingBox3D bb)
        {
            ObjectCenter = new Vector3(
                (float)((bb.MinX + bb.MaxX) * 0.5),
                (float)((bb.MinY + bb.MaxY) * 0.5),
                (float)((bb.MinZ + bb.MaxZ) * 0.5)
                );
            ObjectW = bb.MaxX - bb.MinX;
            ObjectH = bb.MaxY - bb.MinY;
            ObjectD = bb.MaxZ - bb.MinZ;
        }

        public void Fit()
        {
            double margin = 1.5;
            double objAspect = ObjectW / ObjectH;
            InvScale = 1.0;
            if (objAspect < WindowAspect)
            {
                HalfViewHeight = ObjectH * 0.5 * margin;
            }
            else
            {
                double tmpH = ObjectW / WindowAspect;
                HalfViewHeight = tmpH * 0.5 * margin;
            }
            Dist = HalfViewHeight * InvScale / Math.Tan(fovY * 0.5) + ObjectD * 0.5;
            WindowCenter = new Vector2(0.0f, 0.0f);
        }

        public void Fit(BoundingBox3D bb)
        {
            SetObjectBoundingBox(bb);
            Fit();
        }

        public void MousePan(double movBeginX, double movBeginY, double movEndX, double movEndY)
        {
            double x = WindowCenter.X;
            double y = WindowCenter.Y;
            
            x += (movEndX - movBeginX) * InvScale;
            y += (movEndY - movBeginY) * InvScale;

            WindowCenter = new Vector2((float)x, (float)y);
        }

        public abstract void MouseRotation(double movBeginX, double movBeginY, double movEndX, double movEndY);

        public abstract void RotMatrix33(double[] rot);

        public double[] RotMatrix33()
        {
            double[] rot = new double[9];
            RotMatrix33(rot);
            return rot;
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

    }
}
