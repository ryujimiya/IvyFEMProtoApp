﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
//using OpenTK; // System.Numericsと衝突するので注意
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.IO;

namespace IvyFEM
{
    class OpenGLUtils
    {
        public static void GLVertex2(Vector2 p)
        {
            GL.Vertex2(p.X, p.Y);
        }

        public static void SetProjectionTransform(Camera camera)
        {
            //System.Diagnostics.Debug.WriteLine("SetProjectionTransform");
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
                OpenTK.Matrix4 perspective = OpenTK.Matrix4.CreatePerspectiveFieldOfView(
                    (float)fovY, (float)aspect, (float)clipNear, (float)clipFar);
                GL.LoadMatrix(ref perspective);
            }
            else
            {
                // 正規投影変換
                //System.Diagnostics.Debug.WriteLine("正規投影変換");
                //System.Diagnostics.Debug.WriteLine("scale = " + camera.Scale +
                //    " windowAspect = " + camera.WindowAspect +
                //    " halfViewHeight = " + camera.HalfViewHeight);
                double invScale = 1.0 / camera.Scale;
                double asp = camera.WindowAspect;
                double hH = camera.HalfViewHeight * invScale;
                double hW = camera.HalfViewHeight * invScale * asp;
                double depth = 2.0 * (hH + hW);

                GL.Ortho(-hW, hW, -hH, hH, -depth, depth);
            }
        }

        public static void SetModelViewTransform(Camera camera)
        {
            //System.Diagnostics.Debug.WriteLine("SetModelViewTransform");
            {
                // pan the object
                double x;
                double y;
                double z;
                camera.GetCenterPosition(out x, out y, out z);
                //System.Diagnostics.Debug.WriteLine("CenterPosition = " + x + ", " + y + ", " + z);

                GL.Translate(x, y, z);
            }
            {
                // rotate
                double[] rot = new double[16];
                camera.RotMatrix44Trans(rot);
                /*
                System.Diagnostics.Debug.WriteLine("RotMatrix44");
                for (int i = 0; i < 16; i++)
                {
                    System.Diagnostics.Debug.Write(rot[i] + " ");
                }
                System.Diagnostics.Debug.WriteLine("");
                */

                GL.MultMatrix(rot);
            }
            {
                // put the origin at the center of object
                double x;
                double y;
                double z;
                Vector3 objCenter = camera.ObjectCenter;
                x = objCenter.X;
                y = objCenter.Y;
                z = objCenter.Z;
                //System.Diagnostics.Debug.WriteLine("objCenter = " + x + ", " + y + ", " + z);

                GL.Translate(-x, -y, -z);
            }
        }

        public static void GluPickMatrix(double x, double y, double deltax, double deltay, int[] viewport)
        {
            if (deltax <= 0 || deltay <= 0)
            {
                return;
            }

            GL.Translate((viewport[2] - 2 * (x - viewport[0])) / deltax, (viewport[3] - 2 * (y - viewport[1])) / deltay, 0);
            GL.Scale(viewport[2] / deltax, viewport[3] / deltay, 1.0);
        }

        public static int GluProject(float objx, float objy, float objz,
            float[] modelview, float[] projection,
            int[] viewport, float[] windowCoordinate)
        {
            // Transformation vectors
            float[] fTempo = new float[8];
            // Modelview transform
            fTempo[0] = modelview[0] * objx + modelview[4] * objy + modelview[8] * objz + modelview[12]; // w is always 1
            fTempo[1] = modelview[1] * objx + modelview[5] * objy + modelview[9] * objz + modelview[13];
            fTempo[2] = modelview[2] * objx + modelview[6] * objy + modelview[10] * objz + modelview[14];
            fTempo[3] = modelview[3] * objx + modelview[7] * objy + modelview[11] * objz + modelview[15];
            // Projection transform, the final row of projection matrix is always [0 0 -1 0]
            // so we optimize for that.
            fTempo[4] = projection[0] * fTempo[0] + projection[4] * fTempo[1] + projection[8] * fTempo[2] +
                projection[12] * fTempo[3];
            fTempo[5] = projection[1] * fTempo[0] + projection[5] * fTempo[1] + projection[9] * fTempo[2] +
                projection[13] * fTempo[3];
            fTempo[6] = projection[2] * fTempo[0] + projection[6] * fTempo[1] + projection[10] * fTempo[2] +
                projection[14] * fTempo[3];
            fTempo[7] = -fTempo[2];
            // The result normalizes between -1 and 1
            if (fTempo[7] == 0.0)
            {
                // The w value
                return 0;
            }
            fTempo[7] = 1.0f / fTempo[7];
            // Perspective division
            fTempo[4] *= fTempo[7];
            fTempo[5] *= fTempo[7];
            fTempo[6] *= fTempo[7];
            // Window coordinates
            // Map x, y to range 0-1
            windowCoordinate[0] = (fTempo[4] * 0.5f + 0.5f) * viewport[2] + viewport[0];
            windowCoordinate[1] = (fTempo[5] * 0.5f + 0.5f) * viewport[3] + viewport[1];
            // This is only correct when glDepthRange(0.0, 1.0)
            windowCoordinate[2] = (1.0f + fTempo[6]) * 0.5f;  // Between 0 and 1
            return 1;
        }

        public static int GluUnProject(double winx, double winy, double winz,
            double[] modelview, double[] projection,  int[] viewport,
            out double objectX, out double objectY, out double objectZ)
        {
            objectX = 0;
            objectY = 0;
            objectZ = 0;

            // Transformation matrices
            Matrix4x4 projectionM = new Matrix4x4(
                (float)projection[0], (float)projection[4], (float)projection[8], (float)projection[12],
                (float)projection[1], (float)projection[5], (float)projection[9], (float)projection[13],
                (float)projection[2], (float)projection[6], (float)projection[10], (float)projection[14],
                (float)projection[3], (float)projection[7], (float)projection[11], (float)projection[15]);
            Matrix4x4 modelviewM = new Matrix4x4(
                (float)modelview[0], (float)modelview[4], (float)modelview[8], (float)modelview[12],
                (float)modelview[1], (float)modelview[5], (float)modelview[9], (float)modelview[13],
                (float)modelview[2], (float)modelview[6], (float)modelview[10], (float)modelview[14],
                (float)modelview[3], (float)modelview[7], (float)modelview[11], (float)modelview[15]);
            // Calculation for inverting a matrix, compute projection x modelview
            // and store in A[16]
            Matrix4x4 AM = projectionM * modelviewM;
            // Now compute the inverse of matrix A
            Matrix4x4 mM;
            bool invRet = Matrix4x4.Invert(AM, out mM);
            if (!invRet)
            {
                return 0;
            }

            // Transformation of normalized coordinates between -1 and 1
            Vector4 inV = new Vector4();
            inV.W = (float)((winx - viewport[0]) / viewport[2] * 2.0 - 1.0);
            inV.X = (float)((winy - viewport[1]) / viewport[3] * 2.0 - 1.0);
            inV.Y = (float)(2.0 * winz - 1.0);
            inV.Z = (float)1.0;
            Vector4 outV;
            // Objects coordinates
            MultiplyMatrix4x4ByVector4(out outV, mM, inV);
            if (outV.Z == 0.0)
            {
                return 0;

            }
            outV.Z = (float)(1.0 / outV.Z);
            objectX = outV.W * outV.Z;
            objectY = outV.X * outV.Z;
            objectZ = outV.Y * outV.Z;

            /*
            System.Diagnostics.Debug.WriteLine("GluUnProject");
            System.Diagnostics.Debug.WriteLine("objectX = " + objectX);
            System.Diagnostics.Debug.WriteLine("objectY = " + objectY);
            System.Diagnostics.Debug.WriteLine("objectZ = " + objectZ);
            */
            return 1;
        }

        public static void MultiplyMatrix4x4ByVector4(out Vector4 resultvector,
            Matrix4x4 matrix, Vector4 pvector)
        {
            resultvector.W = matrix.M11 * pvector.W + matrix.M12 * pvector.X +
                matrix.M13 * pvector.Y + matrix.M14 * pvector.Z;
            resultvector.X = matrix.M21 * pvector.W + matrix.M22 * pvector.X +
                matrix.M23 * pvector.Y + matrix.M24 * pvector.Z;
            resultvector.Y = matrix.M31 * pvector.W + matrix.M32 * pvector.X +
                matrix.M33 * pvector.Y + matrix.M34 * pvector.Z;
            resultvector.Z = matrix.M41 * pvector.W + matrix.M42 * pvector.X +
                matrix.M43 * pvector.Y + matrix.M44 * pvector.Z;
        }

        public static void PickPre(
                int sizeBuffer, int[] selectBuffer,
                uint pointX, uint pointY,
                uint delX, uint delY,
                Camera camera)
        {
            // Selection初期化
            GL.SelectBuffer(sizeBuffer, selectBuffer);
            GL.RenderMode(RenderingMode.Select);

            // View Port取得
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);

            GL.InitNames();

            // Projection Transform From Here
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            //Tao.OpenGl.Glu.gluPickMatrix(pointX, viewport[3] - pointY, delX, delY, viewport);
            GluPickMatrix(pointX, viewport[3] - pointY, delX, delY, viewport);
            SetProjectionTransform(camera);

            // Model-View  Transform From Here
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            SetModelViewTransform(camera);

        }

        public static IList<SelectedObject> PickPost(
            int[] selectBuffer, uint pointX, uint pointY, Camera camera)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);

            IList<SelectedObject> selectedObjs = new List<SelectedObject>();

            int nHits = GL.RenderMode(RenderingMode.Render);    // return value is number of hits
            if (nHits <= 0)
            {
                return selectedObjs;
            }
            System.Diagnostics.Debug.WriteLine("Picked Object nHits = " + nHits);

            IList<PickedObject> pickedObjs = new List<PickedObject>();
            {
                // get picked_object name and its depth
                for (int i = 0; i < nHits; i++)
                {
                    pickedObjs.Add(new PickedObject());
                }
                int iSel = 0;
                for (int i = 0; i < pickedObjs.Count; i++)
                {
                    uint nameDepth = (uint)selectBuffer[iSel];
                    System.Diagnostics.Debug.Assert(nameDepth <= 4);
                    pickedObjs[i].NameDepth = nameDepth;
                    iSel++;
                    pickedObjs[i].MinDepth = (float)selectBuffer[iSel] / 0x7fffffff;
                    iSel++;
                    pickedObjs[i].MaxDepth = (float)selectBuffer[iSel] / 0x7fffffff;
                    iSel++;
                    for (int j = 0; j < nameDepth; j++)
                    {
                        pickedObjs[i].Name[j] = selectBuffer[iSel];
                        iSel++;
                    }
                }
                // sort picked object in the order of min depth
                for (int i = 0; i < pickedObjs.Count; i++)
                {
                    for (int j = i + 1; j < pickedObjs.Count; j++)
                    {
                        if (pickedObjs[i].MinDepth > pickedObjs[j].MinDepth)
                        {
                            PickedObject tmp = pickedObjs[i];
                            pickedObjs[i] = pickedObjs[j];
                            pickedObjs[j] = tmp;
                        }
                    }
                }
            }
            /*
            // DEBUG
            for (int i = 0; i < pickedObjs.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine("pickedObjs[" + i + "]");
                System.Diagnostics.Debug.WriteLine("NameDepth = " + pickedObjs[i].NameDepth + " " +
                    "MinDepth = " + pickedObjs[i].MinDepth + " " +
                    "MaxDepth = " +pickedObjs[i].MaxDepth);
                for (int j = 0; j < pickedObjs[i].NameDepth; j++)
                {
                    System.Diagnostics.Debug.Write("Name[" + j + "] = " + pickedObjs[i].Name[j] + " ");
                }
                System.Diagnostics.Debug.WriteLine("");
            }
            */

            selectedObjs.Clear();
            for (int i = 0; i < pickedObjs.Count; i++)
            {
                System.Diagnostics.Debug.Assert(pickedObjs[i].NameDepth <= 4);
                SelectedObject selectedObj = new SelectedObject();
                selectedObj.NameDepth = 3;
                for (int itmp = 0; itmp < 3; itmp++)
                {
                    selectedObj.Name[itmp] = pickedObjs[i].Name[itmp];
                }
                selectedObjs.Add(selectedObj);

                double ox, oy, oz;
                {
                    double[] mvMatrix = new double[16];
                    double[] pjMatrix = new double[16];
                    int[] viewport = new int[4];

                    GL.GetInteger(GetPName.Viewport, viewport);

                    GL.GetDouble(GetPName.ModelviewMatrix, mvMatrix);

                    GL.GetDouble(GetPName.ProjectionMatrix, pjMatrix);

                    //Tao.OpenGl.Glu.gluUnProject(
                    //    (double)pointX,
                    //    (double)viewport[3] - pointY,
                    //    pickedObjs[i].MinDepth * 0.5,
                    //    mvMatrix, pjMatrix, viewport,
                    //    out ox, out oy, out oz);
                    GluUnProject(
                        pointX,
                        viewport[3] - pointY,
                        pickedObjs[i].MinDepth * 0.5,
                        mvMatrix, pjMatrix, viewport,
                        out ox, out oy, out oz);
                }
                selectedObj.PickedPos = new Vector3((float)ox, (float)oy, (float)oz);
            }
            return selectedObjs;
        }

        // DrawStringの直書き部分
        public static double FontScale { get; set; } = 1.0 / 300.0;

        /*
        // Windows Forms版 TODO: 直書きあり
        public static void DrawString(string str, double fontSize)
        {
            var window = System.Windows.Application.Current.MainWindow;

            double[] glDoubleColor = new double[4];
            GL.GetDouble(GetPName.CurrentColor, glDoubleColor);
            byte glR = (byte)(glDoubleColor[0] * 255);
            byte glG = (byte)(glDoubleColor[1] * 255);
            byte glB = (byte)(glDoubleColor[2] * 255);
            byte glA = (byte)(glDoubleColor[3] * 255); //255;
            var glColor = System.Drawing.Color.FromArgb(glA, glR, glG, glB);
            System.Drawing.Brush foreground = new System.Drawing.SolidBrush(glColor);

            //var font = new System.Drawing.Font("Arial", 12);
            //var font = new System.Drawing.Font(window.FontFamily.Source, (int)window.FontSize);
            // 1 point  =  1 / 72.0 inch = (1 / 72.0) * 96  = 1.333333 pixel
            var font = new System.Drawing.Font(window.FontFamily.Source, (int)(fontSize / 1.333333));
            // サイズ取得用に生成
            var bmp = new System.Drawing.Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Drawing.SizeF size;
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                size = g.MeasureString(str, font);
            }

            // 再生成
            {
                int width = (int)size.Width;
                //int height = (int)size.Height;
                // TODO: テキストのY座標がずれている : 画像の高さが大きすぎる
                int height = (int)(size.Height * 0.75);
                bmp = new System.Drawing.Bitmap(width, height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }

            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                g.DrawString(str, font, foreground, new System.Drawing.Point(0, 0));
            }

            // check
            //bmp.Save("1.bmp");

            // 上下反転する
            bmp.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);

            var bitmapData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int bmpWidth = bitmapData.Width;
            int bmpHeight = bitmapData.Height;

            bool isTexture = GL.IsEnabled(EnableCap.Texture2D);
            bool isBlend = GL.IsEnabled(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.Rgba,
                bmpWidth, bmpHeight, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);

            GL.PushMatrix();
            OpenTK.Matrix4d m;
            GL.GetDouble(GetPName.ModelviewMatrix, out m);
            //GL.Translate(0, 0, -m.M14);
            GL.Scale(1.0 / m.M11, 1.0 / m.M22, 1.0 / m.M33);
            GL.Scale(FontScale, FontScale, 1.0); // TODO: これを計算で求める必要がある

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(0, 0);

            GL.TexCoord2(1, 0);
            GL.Vertex2(bmpWidth, 0);

            GL.TexCoord2(1, 1);
            GL.Vertex2(bmpWidth, bmpHeight);

            GL.TexCoord2(0, 1);
            GL.Vertex2(0, bmpHeight);
            GL.End();
            GL.PopMatrix();

            bmp.UnlockBits(bitmapData);

            if (!isTexture)
            {
                GL.Disable(EnableCap.Texture2D);
            }
            if (!isBlend)
            {
                GL.Disable(EnableCap.Blend);
            }
        }
        */

        // WPF版 TODO: 直書きあり
        public static void DrawString(string str, double fontSize)
        {
            var window = System.Windows.Application.Current.MainWindow;

            double[] glDoubleColor = new double[4];
            GL.GetDouble(GetPName.CurrentColor, glDoubleColor);
            var glColor = new System.Windows.Media.Color();
            glColor.R = (byte)(glDoubleColor[0] * 255);
            glColor.G = (byte)(glDoubleColor[1] * 255);
            glColor.B = (byte)(glDoubleColor[2] * 255);
            glColor.A = (byte)(glDoubleColor[3] * 255); //255;
            System.Windows.Media.Brush foreground = new System.Windows.Media.SolidColorBrush(glColor);
            //System.Windows.Media.Brush foreground = window.Foreground;

            var text = new System.Windows.Media.FormattedText(
                str,
                new System.Globalization.CultureInfo("en-us"),
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface(
                    window.FontFamily,
                    System.Windows.FontStyles.Normal,
                    System.Windows.FontWeights.Normal,
                    new System.Windows.FontStretch()),
                fontSize, //window.FontSize,
                foreground);
            var drawingVisual = new System.Windows.Media.DrawingVisual();
            using (System.Windows.Media.DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawText(text, new System.Windows.Point(0, 0));
            }

            System.Windows.Media.Imaging.RenderTargetBitmap bmp = null;
            {
                int width = (int)text.Width;
                int height = (int)text.Height;
                int dpiX = 96;
                int dpiY = 96;
                bmp = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    width, height, dpiX, dpiY, System.Windows.Media.PixelFormats.Pbgra32);
            }
            
            bmp.Render(drawingVisual);

            int bmpWidth = bmp.PixelWidth;
            int bmpHeight = bmp.PixelHeight;
            int stride = bmpWidth * 4;
            byte[] tmpbits = new byte[stride * bmpHeight];
            var rectangle = new System.Windows.Int32Rect(0, 0, bmpWidth, bmpHeight);
            bmp.CopyPixels(rectangle, tmpbits, stride, 0);
            // 上下反転する
            byte[] bits = new byte[stride * bmpHeight];
            for (int h = 0; h < bmpHeight; h++)
            {
                for (int w = 0; w < stride; w++)
                {
                    bits[h * stride + w] = tmpbits[(bmpHeight - 1 - h) * stride + w];
                }
            }

            // check
            //var png = new System.Windows.Media.Imaging.PngBitmapEncoder();
            //png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
            //using (var fs = new FileStream("1.png", FileMode.Create))
            //{
            //    png.Save(fs);
            //}

            bool isTexture = GL.IsEnabled(EnableCap.Texture2D);
            bool isBlend = GL.IsEnabled(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.Rgba,
                bmpWidth, bmpHeight, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, bits);

            GL.PushMatrix();
            OpenTK.Matrix4d m;
            GL.GetDouble(GetPName.ModelviewMatrix, out m);
            GL.Scale(1.0 / m.M11, 1.0 / m.M22, 1.0 / m.M33);
            GL.Scale(FontScale, FontScale, 1.0); // TODO: これを計算で求める必要がある

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(0, 0);

            GL.TexCoord2(1, 0);
            GL.Vertex2(bmpWidth, 0);

            GL.TexCoord2(1, 1);
            GL.Vertex2(bmpWidth, bmpHeight);

            GL.TexCoord2(0, 1);
            GL.Vertex2(0, bmpHeight);
            GL.End();
            GL.PopMatrix();

            if (!isTexture)
            {
                GL.Disable(EnableCap.Texture2D);
            }
            if (!isBlend)
            {
                GL.Disable(EnableCap.Blend);
            }
        }

    }
}