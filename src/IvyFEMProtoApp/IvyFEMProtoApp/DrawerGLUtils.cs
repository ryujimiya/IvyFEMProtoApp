using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace IvyFEM
{
    class DrawerGLUtils
    {
        public static void SetProjectionTransform(Camera camera)
        {
            if (camera.IsPers)
            {
                // 透視投影変換
                double fovY;
                double aspect;
                double clipNear;
                double clipFar;
                camera.GetPerspective(out fovY, out aspect, out clipNear, out clipFar);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(
                    (float)fovY, (float)aspect, (float)clipNear, (float)clipFar);
                GL.LoadMatrix(ref perspective);
            }
            else
            {
                // 正規投影変換
                double invScale = 1.0 / camera.Scale;
                double asp = camera.WindowAspect;
                double hH = camera.HalfViewHeight * invScale;
                double hW = camera.HalfViewHeight * invScale * asp;
                double depth = 2.0 * (hH + hW);

                GL.Ortho(-hW, hW, -hH, hH, -depth, depth);
            }
        }


    }
}
