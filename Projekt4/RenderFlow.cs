using FastBitmapLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Projekt4
{
    public partial class MainForm : Form
    {
        void Render()
        {
            Bitmap nextBitmap = new Bitmap(canva.Width, canva.Height);
            int canvaWidth = canva.Width;
            int canvaHeight = canva.Height;
            int[] tmp = new int[canvaWidth * canvaHeight];
            float[] zBuffer = Enumerable.Repeat(fogMax, canvaWidth * canvaHeight).ToArray();


            Parallel.ForEach(objs, (obj) =>
            {
                switch (shading)
                {
                    case Shading.Gouraud:
                        drawObjGouraud(obj, tmp, canvaWidth, zBuffer);
                        break;
                    case Shading.Phong:
                        drawObjPhong(obj, tmp, canvaWidth, zBuffer);
                        break;
                    case Shading.constant:
                        drawObjConst(obj, tmp, canvaWidth, zBuffer);
                        break;
                }
            });

            drawFog(tmp, zBuffer);

            using (var fastBitmap = nextBitmap.FastLock())
            {
                if (dayNight) fastBitmap.Clear(Color.White);
                else fastBitmap.Clear(Color.Black);
                fastBitmap.CopyFromArray(tmp, true);
            }

            // using (Graphics g = Graphics.FromImage(nextBitmap))
            // {
            //     drawInfo(g);
            // }

            canva.Image?.Dispose();
            canva.Image = (Image)nextBitmap;
        }

        private void drawFog(int[] tmp, float[] zBuffer)
        {
            int backColor = dayNight ? Color.White.ToArgb() : Color.Black.ToArgb();
            for (int i = 0; i < zBuffer.Length; i++)
            {
                if (zBuffer[i] < fogMax && zBuffer[i] > fogMin)
                {
                    float alpha = (zBuffer[i] - fogMin) / (fogMax - fogMin);
                    tmp[i] = blend(tmp[i], backColor, alpha);
                }
            }

            int blend(int color, int backColor, float amount)
            {
                int r = ((int)(((color & 0xFF0000) >> 16) * (1 - amount) + ((backColor & 0xFF0000) >> 16) * amount)) << 16;
                int g = ((int)(((color & 0xFF00) >> 8) * (1 - amount) + ((backColor & 0xFF00) >> 8) * amount)) << 8;
                int b = (int)((color & 0xFF) * (1 - amount) + (backColor & 0xFF) * amount);
                return (-16777216 | r | g | b);
            }
        }

        private void drawInfo(Graphics g)
        {
            string s = $"Camera pos:\nX:{String.Format("{0:0.00}", cameraPos.X)}\nY:{String.Format("{0:0.00}", cameraPos.Y)}\nZ:{String.Format("{0:0.00}", cameraPos.Z)}";
            g.DrawString(s, new Font("Arial", 8), new SolidBrush(Color.Black), canva.Width - 90, 30);
        }

        void drawObjGouraud(Obj obj, int[] tmp, int canvaWidth, float[] zBuffer)
        {
            Parallel.ForEach(obj.faces, (face) =>
            {
                Vector3[] points = new Vector3[face.Item1.Length];
                bool isOK = true;
                for (int i = 0; i < face.Item1.Length; i++)
                {
                    points[i] = projectPoint(obj.points[face.Item1[i]], obj.modelMatrix);
                    if (!IsIn(points[i]))
                    {
                        isOK = false;
                        break;
                    }
                }

                Color[] colors = new Color[3];
                Vector3[] normalVectors = new Vector3[3];
                for (int i = 0; i < 3; i++)
                {
                    normalVectors[i] = Vector3.TransformNormal(obj.normalVector[face.Item2[i]], obj.modelMatrix);
                    colors[i] = calcColor(obj.color, Vector4.Transform(obj.points[face.Item1[i]], obj.modelMatrix).toVec3(), normalVectors[i]);
                }

                if (isOK) ForEachPixel(points, (vec, x, y) =>
                {
                    Vector3 point = getPoint(vec, x, y);
                    if (point.Z < zBuffer[x + y * canvaWidth])
                    {
                        zBuffer[x + y * canvaWidth] = point.Z;
                        tmp[x + y * canvaWidth] = interpolateColor(vec, colors, point);
                    }
                });
            });
        }

        void drawObjPhong(Obj obj, int[] tmp, int canvaWidth, float[] zBuffer)
        {
            Parallel.ForEach(obj.faces, (face) =>
            {
                Vector3[] points = new Vector3[face.Item1.Length];
                bool isOK = true;
                for (int i = 0; i < face.Item1.Length; i++)
                {
                    points[i] = projectPoint(obj.points[face.Item1[i]], obj.modelMatrix);
                    if (!IsIn(points[i]))
                    {
                        isOK = false;
                        break;
                    }
                }


                Vector4[] rotatedPoints = face.Item1
                .Select((i) => obj.points[i])
                .Select((v) => Vector4.Transform(v, obj.modelMatrix))
                .ToArray();

                Vector3[] normals = face.Item2
                .Select((i) => obj.normalVector[i])
                .Select((v) => Vector3.TransformNormal(v, obj.modelMatrix))
                .ToArray();

                if (isOK) ForEachPixel(points, (vec, x, y) =>
                {
                    Console.WriteLine(points);
                    Vector3 point = getPoint(vec, x, y);
                    if (point.Z < zBuffer[x + y * canvaWidth])
                    {
                        zBuffer[x + y * canvaWidth] = point.Z;
                        Vector3 localNormal = interpolateVector(vec, normals, point);
                        Vector3 localPoint = interpolateVector(vec, rotatedPoints.Select(v => v.toVec3()).ToArray(), point);
                        tmp[x + y * canvaWidth] = calcColor(obj.color, localPoint, localNormal).ToArgb();
                    }
                });
            });
        }

        void drawObjConst(Obj obj, int[] tmp, int canvaWidth, float[] zBuffer)
        {
            Parallel.ForEach(obj.faces, (face) =>
            {
                Vector3[] points = new Vector3[face.Item1.Length];
                bool isOK = true;
                for (int i = 0; i < face.Item1.Length; i++)
                {
                    points[i] = projectPoint(obj.points[face.Item1[i]], obj.modelMatrix);
                    if (!IsIn(points[i]))
                    {
                        isOK = false;
                        break;
                    }
                }
                Vector3[] realPoints = face.Item1
                .Select((i) => obj.points[i].toVec3())
                .ToArray();
                Vector3 centralpoint = centralPoint(realPoints);
                Vector3[] normals = face.Item2
                .Select((i) => obj.normalVector[i])
                .Select((v) => Vector3.TransformNormal(v, obj.modelMatrix))
                .ToArray();

                Vector3 normalInCenter = interpolateVector(realPoints, normals, centralpoint);

                int constColor = calcColor(obj.color, Vector3.Transform(centralpoint, obj.modelMatrix), normalInCenter).ToArgb();

                if (isOK) ForEachPixel(points, (vec, x, y) =>
                {
                    Vector3 point = getPoint(vec, x, y);
                    if (point.Z < zBuffer[x + y * canvaWidth])
                    {
                        zBuffer[x + y * canvaWidth] = point.Z;
                        tmp[x + y * canvaWidth] = constColor;
                    }
                });
            });
        }

        float myCos(Vector3 v1, Vector3 v2, bool cut = true)
        {
            float cos = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z / v1.Length() / v2.Length();
            if (cut && cos < 0.0) cos = 0.0f;
            if (cos > 1.1f) throw new Exception("cos > 1");
            return cos;
        }

        private Color calcColor(Color obj_c, Vector3 point, Vector3 normalVector)
        {
            float[] objColor = { obj_c.R, obj_c.G, obj_c.B };
            int[] rgb = new int[3];
            for (int i = 0; i < 3; i++)
            {
                rgb[i] = (int)(ka * objColor[i]);
            }
            for (int i = dayNight ? 0 : 1; i < lightsSources.Count; i++)
            {
                Vector3 N = normalVector;
                (bool ok, Vector3 L, float[] color) = lightsSources[i].isInRange(point);
                if (!ok) continue;
                Vector3 R = 2 * myCos(N, L, false) * N - L;
                float first = kd * myCos(N, L, true);
                Vector3 Observer = Vector3.Normalize(getCameraPos() - point);
                float second = ks * MathF.Pow(myCos(Observer, R, true), m);
                for (int j = 0; j < 3; j++)
                {
                    rgb[j] += (int)((first + second) * objColor[j] * color[j]);
                    if (rgb[j] < 0) rgb[j] = 0;
                    if (rgb[j] > 255) rgb[j] = 255;
                }
            }

            Color res = Color.FromArgb(rgb[0], rgb[1], rgb[2]);
            return res;
        }

        private Vector3 getCameraPos()
        {
            return isMovingCamera ? movingCameras[cameraIndex].cameraPos : cameraPos;
        }

        private float[] barocentricWeigths(Vector3[] f, Vector3 point)
        {
            Vector3 A = f[1] - f[0];
            Vector3 B = f[2] - f[0];
            Vector3 C = f[2] - f[1];


            float P = Vector3.Cross(A, B).Length();
            float P0 = Vector3.Cross((point - f[1]), C).Length();
            float P1 = Vector3.Cross((point - f[0]), B).Length();
            float P2 = Vector3.Cross((point - f[0]), A).Length();
            float[] res = new float[] { P0 / P, P1 / P, P2 / P };
            float sum = res[0] + res[1] + res[2];
            if (sum < 0.8 || sum > 2)
            {
                //throw new Exception("WTF?");
            }
            return res.Select(i => i / sum).ToArray();
        }

        private int interpolateColor(Vector3[] face, Color[] colors, Vector3 point)
        {
            float[] weights = barocentricWeigths(face, point);
            int[] rgb = new int[3];
            for (int i = 0; i < 3; i++)
            {
                rgb[0] += (int)(weights[i] * colors[i].R);
                rgb[1] += (int)(weights[i] * colors[i].G);
                rgb[2] += (int)(weights[i] * colors[i].B);
            }
            return Color.FromArgb(rgb[0], rgb[1], rgb[2]).ToArgb();
        }

        private Vector3 centralPoint(Vector3[] points)
        {
            Vector3 avg = Vector3.Zero;
            foreach (var item in points)
            {
                avg += item;
            }
            return avg / points.Length;
        }

        private Vector3 interpolateVector(Vector3[] face, Vector3[] vectors, Vector3 point)
        {
            float[] weights = barocentricWeigths(face, point);

            return weights[0] * vectors[0] + weights[1] * vectors[1] + weights[2] * vectors[2];
        }

        private Vector3 getPoint(Vector3[] face, int x, int y)
        {
            float det = (face[1].Y - face[2].Y) * (face[0].X - face[2].X) + (face[2].X - face[1].X) * (face[0].Y - face[2].Y);
            float l1 = ((face[1].Y - face[2].Y) * (x - face[2].X) + (face[2].X - face[1].X) * (y - face[2].Y)) / det;
            float l2 = ((face[2].Y - face[0].Y) * (x - face[2].X) + (face[0].X - face[2].X) * (y - face[2].Y)) / det;
            float l3 = 1.0f - l1 - l2;
            float res = (l1 * face[0].Z + l2 * face[1].Z + l3 * face[2].Z);
            return new Vector3(x, y, res);
        }

        private bool IsIn(Vector3 pointF)
        {
            return pointF.X >= 0 && pointF.Y >= 0 && pointF.X < canva.Width && pointF.Y < canva.Height;
        }

        Vector3 projectPoint(Vector4 vec, Matrix4x4 modelMatrix)
        {
            Vector4 vecCanva = Vector4.Transform(
                Vector4.Transform(
                    Vector4.Transform(vec, modelMatrix),
                    view),
                perspective);
            return virtualToCanva(projection(vecCanva));
        }

        Vector3 virtualToCanva(Vector3 virtualPos)
        {
            float X = virtualPos.X;
            float Y = virtualPos.Y;

            float newX = 0.5f * canva.Width * (1.0f + X / virtualX);
            float newY = 0.5f * canva.Height * (1.0f - Y / virtualY);
            return new Vector3(newX, newY, virtualPos.Z);
        }

        Vector3 projection(Vector4 vec)
        {
            return new Vector3(vec.X / vec.W, vec.Y / vec.W, vec.Z / vec.W);
        }
    }
}
