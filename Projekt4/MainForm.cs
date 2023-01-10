using FastBitmapLib;
using System.Numerics;
using static System.Reflection.Metadata.BlobBuilder;

namespace Projekt4
{
    public partial class MainForm : Form
    {
        const float virtualX = 1.0f;
        const float virtualY = 1.0f;

        float fogMax = 1.5f;
        float fogMin = 1.42f;

        List<Obj> objs = new List<Obj>();
        float fov = 60.0f;

        Vector3 cameraPos = new Vector3(12f, 12f, 10f);
        Vector3 carCameraPos = Vector3.Zero;

        Matrix4x4 view = Matrix4x4.Identity;
        Matrix4x4 model = Matrix4x4.Identity;
        Matrix4x4 perspective = Matrix4x4.Identity;
        private bool isRunning = true;

        float ka = 0.1f;
        float kd = 0.9f;
        float ks = 0.5f;
        float m = 10;

        List<Light> lightsSources = new List<Light>();

        float time = 0.0f;
        private int cameraIndex = -1;
        private bool carCamera;

        private bool dayNight = true;

        public MainForm()
        {
            InitializeComponent();
            view = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitZ);
            perspective = Matrix4x4.CreatePerspectiveFieldOfView(fov / 180.0f * MathF.PI, ((float)canva.Width) / canva.Height, 1.0f, 3.0f);

            objs.Add(new Obj(@"Models/ground.obj", Color.DarkSlateGray, (t) =>
            {
                return Matrix4x4.Identity;
            }));

            objs.Add(new Obj(@"Models/ufo.obj", Color.FromArgb(71, 219, 63), (t) =>
            {
                return Matrix4x4.CreateRotationY(-0.2f) * Matrix4x4.CreateTranslation(6.0f, 0.0f, 7.0f) * Matrix4x4.CreateRotationZ(t);
            }));

            objs.Add(new Obj(@"Models/cybertruck.obj", Color.Silver, (t) =>
            {
                return Matrix4x4.CreateRotationY(0.2f*MathF.Sin(10*t)) * Matrix4x4.CreateTranslation(0.0f, -7.0f, 1.0f) * Matrix4x4.CreateRotationZ(-2.0f * t);
            }));

            lightsSources.Add(new Light(new Vector3(0, 0, 50), new Vector3(0, 0, -1),-1));
            lightsSources.Add(new Light(
               (t) => Vector3.Transform(new Vector3(-2, 1, 1f), Matrix4x4.CreateRotationY(0.2f * MathF.Sin(10 * t)) * Matrix4x4.CreateTranslation(0.0f, -7.0f, 1.0f) * Matrix4x4.CreateRotationZ(-2.0f * t)),
               (t) => Vector3.Transform(new Vector3(-1, 0, 0), Matrix4x4.CreateRotationY(0.2f * MathF.Sin(10 * t)) * Matrix4x4.CreateRotationZ(-2.0f * t)),
               6f));
            lightsSources.Add(new Light(
               (t) => Vector3.Transform(new Vector3(-2, -1, 1f), Matrix4x4.CreateRotationY(0.2f * MathF.Sin(10 * t)) * Matrix4x4.CreateTranslation(0.0f, -7.0f, 1.0f) * Matrix4x4.CreateRotationZ(-2.0f * t)),
               (t) => Vector3.Transform(new Vector3(-1, 0, 0), Matrix4x4.CreateRotationY(0.2f * MathF.Sin(10 * t)) * Matrix4x4.CreateRotationZ(-2.0f * t)),
               6f));

            timer.Interval = 100;
            timer.Start();

        }




        void Render()
        {
            Bitmap nextBitmap = new Bitmap(canva.Width, canva.Height);
            int canvaWidth = canva.Width;
            int canvaHeight = canva.Height;
            int[] tmp = new int[canvaWidth * canvaHeight];
            float[] zBuffer = Enumerable.Repeat(fogMax, canvaWidth * canvaHeight).ToArray();


            Parallel.ForEach(objs, (obj) =>
            {
                drawObj(obj, tmp, canvaWidth, zBuffer);
            });

            drawFog(tmp, zBuffer);

            using (var fastBitmap = nextBitmap.FastLock())
            {
                fastBitmap.Clear(Color.White);
                fastBitmap.CopyFromArray(tmp, true);
            }

            using (Graphics g = Graphics.FromImage(nextBitmap))
            {
                drawInfo(g);
            }

            canva.Image?.Dispose();
            canva.Image = (Image)nextBitmap;
        }

        private void drawFog(int[] tmp, float[] zBuffer)
        {
            for (int i = 0; i < zBuffer.Length; i++)
            {
                if (zBuffer[i] < fogMax && zBuffer[i] > fogMin)
                {
                    int alpha = 255 - (int) (255.0 * (zBuffer[i] - fogMin) / (fogMax - fogMin));
                    tmp[i] += alpha << 24;
                }
            }
        }

        private void drawInfo(Graphics g)
        {
            string s = $"Camera pos:\nX:{String.Format("{0:0.00}", cameraPos.X)}\nY:{String.Format("{0:0.00}", cameraPos.Y)}\nZ:{String.Format("{0:0.00}", cameraPos.Z)}";
            g.DrawString(s, new Font("Arial", 8), new SolidBrush(Color.Black), canva.Width - 90, 30);
        }

        void drawObj(Obj obj, int[] tmp, int canvaWidth, float[] zBuffer)
        {


            Pen p = new Pen(Color.White);
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

                Color[] colors = { Color.Magenta, Color.Cyan, Color.Yellow };
                Vector3[] normalVectors = new Vector3[3];
                for (int i = 0; i < 3; i++)
                {
                    normalVectors[i] = Vector3.TransformNormal(obj.normalVector[face.Item2[i]], obj.modelMatrix);
                    colors[i] = calcColor(obj.color,Vector3.Transform( new Vector3(obj.points[face.Item1[i]].X, obj.points[face.Item1[i]].Y, obj.points[face.Item1[i]].Z),obj.modelMatrix), normalVectors[i]);
                }




                if (isOK) ForEachPixel(points, (vec, x, y) =>
                {
                    Vector3 point = getPoint(vec, x, y);
                    if (point.Z < zBuffer[x + y * canvaWidth])
                    {
                        zBuffer[x + y * canvaWidth] = point.Z;
                        //tmp[x + y * canvaWidth] = obj.color.ToArgb();
                        tmp[x + y * canvaWidth] = interpolateColor(vec, colors, point);
                    }
                });
            });
        }

        float myCos(Vector3 v1, Vector3 v2, bool cut = true)
        {
            float cos = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z / v1.Length() / v2.Length();
            if (cut && cos < 0.0) cos = 0.0f;
            return cos;
        }

        private Color calcColor(Color c, Vector3 point, Vector3 normalVector)
        {
            float[] objColor = { c.R, c.G, c.B };
            int[] rgb = new int[3];
            for (int i = 0; i < 3; i++)
            {
                rgb[i] = (int)(ka * objColor[i]);
            }
            foreach (var lights in lightsSources)
            {
                Vector3 N = normalVector;
                (bool ok, Vector3 L,float[] color) = lights.isInRange(point);
                if (!ok) continue;
                Vector3 R = 2 * myCos(N, L, false) * N - L;
                float first = kd * myCos(N, L, true);
                Vector3 Observer = Vector3.Normalize(getCameraPos() - point);
                float second = ks * MathF.Pow(myCos(Observer, R, true), m);
                for (int i = 0; i < 3; i++)
                {
                    rgb[i] += (int)((first + second) * objColor[i] * color[i]);
                    if (rgb[i] < 0) rgb[i] = 0;
                    if (rgb[i] > 255) rgb[i] = 255;
                }
            }

            Color res = Color.FromArgb(rgb[0], rgb[1], rgb[2]);
            return res;
        }

        private Vector3 getCameraPos()
        {
            return carCamera ? carCameraPos: cameraPos;
        }

        private double[] barocentricWeigths(Vector3[] f, Vector3 point)
        {
            Vector3 A = f[1] - f[0];
            Vector3 B = f[2] - f[0];
            Vector3 C = f[2] - f[1];
            Vector3 D = f[0] - f[2];

            double P = (A * B).Length();
            double P0 = ((point - f[1]) * C).Length();
            double P1 = ((point - f[2]) * C).Length();
            double P2 = ((point - f[0]) * C).Length();
            double[] res = new double[] { P0 / P, P1 / P, P2 / P };
            double sum = res[0] + res[1] + res[2];
            return res.Select(i => i / sum).ToArray();
        }

        private int interpolateColor(Vector3[] face, Color[] colors, Vector3 point)
        {
            double[] weights = barocentricWeigths(face, point);
            int[] rgb = new int[3];
            for (int i = 0; i < 3; i++)
            {
                rgb[0] += (int)(weights[i] * colors[i].R);
                rgb[1] += (int)(weights[i] * colors[i].G);
                rgb[2] += (int)(weights[i] * colors[i].B);
            }
            return Color.FromArgb(rgb[0], rgb[1], rgb[2]).ToArgb();
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

        //bool inBound nie rysowac ca³ego trojkata

        Vector3 virtualToCanva(Vector3 virtualPos)
        {
            float X = virtualPos.X;
            float Y = virtualPos.Y;

            float newX = 0.5f * canva.Width * (1.0f + X / virtualX);
            float newY = 0.5f * canva.Height * (1.0f - Y / virtualY);
            return new Vector3(newX, newY, virtualPos.Z);
        }

        Vector2 canvaToVirtual(Vector2 canvaPos)
        {
            throw new NotImplementedException();
        }

        Vector3 projection(Vector4 vec)
        {
            return new Vector3(vec.X / vec.W, vec.Y / vec.W, vec.Z / vec.W);
        }

        private void canva_Resize(object sender, EventArgs e)
        {
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            perspective = Matrix4x4.CreatePerspectiveFieldOfView(fov / 180.0f * MathF.PI, ((float)canva.Width) / canva.Height, 1.0f, 3.0f);
            Render();
        }

        private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            const float step = 0.1f;
            switch (e.KeyChar)
            {
                case 'q':
                    cameraPos.Z += step;
                    break;
                case 'a':
                    cameraPos.Y -= step;
                    break;
                case 'w':
                    cameraPos.X += step;
                    break;
                case 's':
                    cameraPos.X -= step;
                    break;
                case 'e':
                    cameraPos.Z -= step;
                    break;
                case 'd':
                    cameraPos.Y += step;
                    break;
                case 'r':
                    cameraIndex++;
                    if (cameraIndex == objs.Count) cameraIndex = -1;
                    break;
                case 'f':
                    cameraIndex--;
                    if (cameraIndex < -1) cameraIndex = objs.Count-1;
                    break;
                case 't':
                    carCamera = !carCamera;
                    break;
                case 'g':
                    dayNight = !dayNight;
                    if(dayNight) lightsSources[0].color =new float[] {1.0f,1.0f,1.0f };
                    else lightsSources[0].color = new float[] { 0.3f, 0.3f, 0.3f };
                    break;
                case 'z':
                    fov -= 10.0f;
                    if (fov < 10.0f) fov = 10.0f;
                    break;
                case 'x':
                    fov += 10.0f;
                    if (fov > 170.0f) fov = 170.0f;
                    break;
                case 'p':
                    isRunning = !isRunning;
                    if (isRunning) timer.Start();
                    else timer.Stop();
                    break;
            }

            perspective = Matrix4x4.CreatePerspectiveFieldOfView(fov / 180.0f * MathF.PI, ((float)canva.Width) / canva.Height, 1.0f, 3.0f);
            Render();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            foreach (var light in lightsSources)
            {
                light.update(time);
            }
            foreach (var obj in objs)
            {
                obj.modelMatrix = obj.modelMatrixFunc(time);
            }
            time += timer.Interval / 1000.0f;
            setViewMatrix(time);
            Render();
        }

        private void setViewMatrix(float time)
        {
            if (!carCamera)
            {
                Vector3 target = Vector3.Zero;
                if (cameraIndex >= 0)
                {
                    target = Vector3.Transform(Vector3.Zero, objs[cameraIndex].modelMatrix);
                }
                view = Matrix4x4.CreateLookAt(cameraPos, target, Vector3.UnitZ);
            }
            else
            {
                carCameraPos = Vector3.Transform(new Vector3(10.0f, 0.0f, 5.0f), objs[2].modelMatrix);
                Vector3 lookAt = Vector3.Transform(new Vector3(-30.0f, 0.0f, 0.0f), objs[2].modelMatrix);
                view = Matrix4x4.CreateLookAt(carCameraPos, lookAt, Vector3.UnitZ);
            }
        }

        private void otwórzToolStripMenuItem_Click(object sender, EventArgs e)
        {

            var d = new OpenFileDialog();
            d.Filter = "Obj Files|*.obj";
            if (d.ShowDialog() == DialogResult.OK)
            {
                var obj = new Obj(d.FileName);
                objs.Add(obj);
                Render();
            }
        }

        class AET
        {
            public int y_max;
            public double x;
            public double jedenprzezm;

            public AET(int y_max, double x, double jedenprzezm)
            {
                this.y_max = y_max;
                this.x = x;
                this.jedenprzezm = jedenprzezm;
            }
        }

        void ForEachPixel(Vector3[] vectorArr, Action<Vector3[], int, int> action)
        {
            Vector3[] v2 = (Vector3[])vectorArr.Clone();
            Array.Sort(v2, (x, y) => (int)(x.Y - y.Y));
            Queue<Vector3> et = new Queue<Vector3>(v2);
            List<AET> aet = new List<AET>();
            int y = (int)et.First().Y;

            while (aet.Count != 0 || et.Count != 0)
            {
                while (et.Count > 0 && et.First().Y < y)
                {
                    Vector3 vertex = et.Dequeue();
                    int index = Array.IndexOf(vectorArr, vertex);
                    int before = index == 0 ? vectorArr.Length - 1 : index - 1;
                    int after = (index + 1) % vectorArr.Length;
                    if (vectorArr[before].Y >= y)
                    {
                        aet.Add(
                            new AET(
                                (int)vectorArr[before].Y,
                                vertex.X,
                                ((double)(vectorArr[before].X - vertex.X)) / (vectorArr[before].Y - vertex.Y)
                                )
                            );
                    }
                    if (vectorArr[after].Y >= y)
                    {
                        aet.Add(
                            new AET(
                                (int)vectorArr[after].Y,
                                vertex.X,
                                ((double)(vectorArr[after].X - vertex.X)) / (vectorArr[after].Y - vertex.Y)
                                )
                            );
                    }
                }

                if (aet.Count > 0)
                {
                    aet.Sort((x, y) => (int)(x.x - y.x));
                    bool paint = true;
                    int[] changePoints = (int[])aet.Select(x => (int)x.x).Distinct().ToArray();
                    int x = changePoints[0];
                    foreach (var change in changePoints)
                    {
                        int ago = 0;
                        while (x < change)
                        {
                            ago++;
                            if (paint) action(vectorArr, x, y);
                            x++;
                        }
                        if (ago > 1) paint = !paint;
                    }
                    aet.RemoveAll(x => x.y_max <= y);
                    for (int i = 0; i < aet.Count; i++)
                    {
                        aet[i].x += aet[i].jedenprzezm;
                    }
                }
                y++;
            }
        }
    }
}