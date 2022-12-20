using FastBitmapLib;
using System.Numerics;
using static System.Reflection.Metadata.BlobBuilder;

namespace Projekt4
{
    public partial class MainForm : Form
    {
        const float virtualX = 1.0f;
        const float virtualY = 1.0f;

        List<Obj> objs = new List<Obj>();
        float fov = 60.0f;

        Vector3 cameraPos = new Vector3(1.5f, 1.5f, 1.5f);

        Matrix4x4 view = Matrix4x4.Identity;
        Matrix4x4 model = Matrix4x4.Identity;
        Matrix4x4 perspective = Matrix4x4.Identity;
        private bool isRunning = true;


        double kd = 0.9;
        double ks = 0.5;
        double m = 10;


        Vector3 sun = new Vector3(0, 0, 200);


        public MainForm()
        {
            InitializeComponent();
            view = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitZ);
            perspective = Matrix4x4.CreatePerspectiveFieldOfView(fov / 180.0f * MathF.PI, ((float)canva.Width) / canva.Height, 1.0f, 3.0f);
            objs.Add(new Obj(@"torus2.obj"));
            objs.Add(new Obj(@"torus2.obj"));
            //objs.Add(new Obj(@"stozek.obj"));
            //objs.Add(new Obj(@"cat.obj"));
            Render();

            timer.Interval = 100;
            timer.Start();

        }

        


        void Render()
        {
            Bitmap nextBitmap = new Bitmap(canva.Width, canva.Height);
            int canvaWidth = canva.Width;
            int canvaHeight = canva.Height;
            int[] tmp = new int[canvaWidth * canvaHeight];
            float[] zBuffer = Enumerable.Repeat(float.MaxValue, canvaWidth * canvaHeight).ToArray();

            
            foreach (var obj in objs)
            {
                drawObj( obj, tmp, canvaWidth, zBuffer);
            }
            using (var fastBitmap = nextBitmap.FastLock())
            {
                fastBitmap.Clear(Color.Blue);
                fastBitmap.CopyFromArray(tmp,true);
            }

            using (Graphics g = Graphics.FromImage(nextBitmap))
            {
                drawCoords(g);
                drawInfo(g);

            }

            canva.Image?.Dispose();
            canva.Image = (Image)nextBitmap;
        }

        private void drawInfo(Graphics g)
        {
            string s = $"Camera pos:\nX:{String.Format("{0:0.00}", cameraPos.X)}\nY:{String.Format("{0:0.00}", cameraPos.Y)}\nZ:{String.Format("{0:0.00}", cameraPos.Z)}";
            g.DrawString(s, new Font("Arial", 8), new SolidBrush(Color.White), canva.Width - 90, 30);
        }

        void drawCoords(Graphics g)
        {
            float size = 0.2f;
            Pen p = new Pen(Color.Red);
            /*
            PointF O = projectPoint(new Vector4(0,0,0,1), Matrix4x4.Identity);
            PointF OX = projectPoint(new Vector4(size, 0, 0, 1), Matrix4x4.Identity);
            PointF OY = projectPoint( new Vector4(0, size, 0, 1), Matrix4x4.Identity);
            PointF OZ = projectPoint( new Vector4(0, 0, size, 1), Matrix4x4.Identity);

            g.DrawLine(point, O, OX);
            g.DrawLine(point, O, OY);
            g.DrawLine(point, O, OZ);
            */
        }

        void drawObj( Obj obj, int[] tmp, int canvaWidth, float[] zBuffer)
        {
            

            Pen p = new Pen(Color.White);
            foreach ((int[],int[]) face in obj.faces)
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
                    colors[i] = calcColor(obj.color, new Vector3(obj.points[face.Item1[i]].X, obj.points[face.Item1[i]].Y, obj.points[face.Item1[i]].Z), normalVectors[i]);
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
            }
        }

        float myCos(Vector3 v1, Vector3 v2, bool cut = true)
        {
            float cos = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z / v1.Length() / v2.Length();
            if (cut && cos < 0.0) cos = 0.0f;
            return cos;
        }

        private Color calcColor(Color c,Vector3 point, Vector3 normalVector)
        {
            double[] objColor = { c.R, c.G, c.B };
            Vector3 N = normalVector;
            Vector3 L = sun - N;
            L = Vector3.Normalize(L);
            Vector3 R = 2 * myCos(N, L, false) * N - L;
            double first = kd * myCos(N, L, true);
            double second = ks * Math.Pow(myCos(N, R, true), m);
            int[] rgb = new int[3];
            for (int i = 0; i < 3; i++)
            {
                rgb[i] = (int)((first + second) * objColor[i]);
                if (rgb[i] < 0) rgb[i] = 0;
                if (rgb[i] > 255) rgb[i] = 255;
            }
            Color res = Color.FromArgb(rgb[0], rgb[1], rgb[2]);
            return res;
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
            return new Vector3(x, y,res);
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
            return new Vector3(newX, newY,virtualPos.Z);
        }

        Vector2 canvaToVirtual(Vector2 canvaPos)
        {
            throw new NotImplementedException();
        }

        Vector3 projection(Vector4 vec)
        {
            return new Vector3(vec.X / vec.W, vec.Y / vec.W,vec.Z/vec.W);
        }

        private void canva_Resize(object sender, EventArgs e)
        {
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            perspective = Matrix4x4.CreatePerspectiveFieldOfView(fov / 180.0f * MathF.PI, ((float)canva.Width)/ canva.Height, 1.0f, 3.0f);
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
                    cameraPos = new Vector3(1.5f, 1.5f, 1.5f);
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
            view = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitZ);
            perspective = Matrix4x4.CreatePerspectiveFieldOfView(fov / 180.0f * MathF.PI, ((float)canva.Width) / canva.Height, 1.0f, 3.0f);
            Render();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < objs.Count; i++)
            {
                if(i%3 == 0) objs[i].modelMatrix = Matrix4x4.Multiply(Matrix4x4.CreateRotationX(0.2f), objs[i].modelMatrix);
                if (i % 3 == 1) objs[i].modelMatrix = Matrix4x4.Multiply(Matrix4x4.CreateRotationY(0.2f), objs[i].modelMatrix);
                if (i % 3 == 2) objs[i].modelMatrix = Matrix4x4.Multiply(Matrix4x4.CreateRotationZ(0.2f), objs[i].modelMatrix);
            }

            Render();
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
            int y = (int) et.First().Y;

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