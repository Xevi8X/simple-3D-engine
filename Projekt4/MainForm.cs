using FastBitmapLib;
using System.Net.Http.Headers;
using System.Numerics;
using static System.Reflection.Metadata.BlobBuilder;

namespace Projekt4
{
    public partial class MainForm : Form
    {
        const float virtualX = 1.0f;
        const float virtualY = 1.0f;

        float fogMax = 1.5f;
        float fogMin = 1.4f;

        Shading shading = Shading.constant;

        List<Obj> objs = new List<Obj>();
        float fov = 60.0f;

        Vector3 cameraPos = new Vector3(12f, 12f, 10f);

        Matrix4x4 view = Matrix4x4.Identity;
        Matrix4x4 perspective = Matrix4x4.Identity;
        private bool isRunning = true;

        float ka = 0.1f;
        float kd = 0.9f;
        float ks = 0.5f;
        float m = 10;

        List<Light> lightsSources = new List<Light>();
        List<MovingCamera> movingCameras = new List<MovingCamera>();

        float time = 0.0f;
        private int cameraIndex = 0;
        private bool isMovingCamera = false;
        private Obj movingObject;
        float moveStep = 0.5f;
        float angleStep = 0.1f;
        float acctualAngle = 0.0f;

        private Light controlledLight;

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


            var cybertruck = new Obj(@"Models/cybertruck.obj", Color.Silver, (t) =>
            {
                return Matrix4x4.CreateRotationY(0.2f * MathF.Sin(10 * t)) * Matrix4x4.CreateTranslation(0.0f, -7.0f, 1.0f) * Matrix4x4.CreateRotationZ(-2.0f * t);
            });

            objs.Add(cybertruck);

            var ludzik = new Obj(@"Models/ludzik.obj", Color.Aqua, Matrix4x4.CreateTranslation(0.0f, 0.0f, 0.8f));
            objs.Add(ludzik);

            //objs.Add(new Obj(@"Models/sfera.obj", Color.White, (t) =>
            //{
            //    return Matrix4x4.CreateTranslation(9.0f,9.0f, 7.0f);
            //}));

            lightsSources.Add(new Light(new Vector3(0, 0, 50), new Vector3(0, 0, -1),-1));
            lightsSources.Add(new Light(
               (t) => new Vector3(-2, 1, 1f),
               (t) => new Vector3(-1, 0, 0),
               3f, cybertruck));
            lightsSources.Add(new Light(
               (t) => new Vector3(-2, -1, 1f),
               (t) => new Vector3(-1, 0, 0),
               3f, cybertruck));
            controlledLight = new Light(
               (t) => new Vector3(0f, 0, 1f),
               (t) => new Vector3(0f, 1f, 0f),
               7f, ludzik);

            lightsSources.Add(controlledLight);

            movingCameras.Add(new MovingCamera(cybertruck, new Vector3(10.0f, 0.0f, 5.0f), new Vector3(-30.0f, 0.0f, 0.0f)));
            movingCameras.Add(new MovingCamera(ludzik, new Vector3(0.0f, -10.0f, 5.0f), new Vector3(00.0f, 30.0f, 0.0f)));

            movingObject = ludzik;

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
                switch(shading)
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
                if(dayNight)fastBitmap.Clear(Color.White);
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

            int blend(int color,int backColor, float amount)
            {
                int r = ((int)(((color & 0xFF0000)>>16) * (1 - amount) + ((backColor & 0xFF0000) >> 16) * amount))<<16;
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
                    colors[i] = calcColor(obj.color,Vector4.Transform(obj.points[face.Item1[i]] ,obj.modelMatrix).toVec3(), normalVectors[i]);
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

                int constColor = calcColor(obj.color, Vector3.Transform(centralpoint,obj.modelMatrix), normalInCenter).ToArgb();

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
                (bool ok, Vector3 L,float[] color) = lightsSources[i].isInRange(point);
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
            return isMovingCamera ? movingCameras[cameraIndex].cameraPos: cameraPos;
        }

        private float[] barocentricWeigths(Vector3[] f, Vector3 point)
        {
            Vector3 A = f[1] - f[0];
            Vector3 B = f[2] - f[0];
            Vector3 C = f[2] - f[1];


            float P = Vector3.Cross(A,B).Length();
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

            return weights[0]*vectors[0] + weights[1]*vectors[1] + weights[2]*vectors[2];
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
                case 'g':
                    dayNight = !dayNight;
                    if(dayNight) lightsSources[0].color =new float[] {1.0f,1.0f,1.0f };
                    else lightsSources[0].color = new float[] { 0.0f, 0.0f, 0.0f };
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
                    break;
            }

            perspective = Matrix4x4.CreatePerspectiveFieldOfView(fov / 180.0f * MathF.PI, ((float)canva.Width) / canva.Height, 1.0f, 3.0f);
            Render();
        }

        private void timer_Tick(object sender, EventArgs e)
        {

            foreach (var obj in objs)
            {
                obj.update(time);
            }
            foreach (var light in lightsSources)
            {
                light.update(time);
            }
            if (isRunning) time += timer.Interval / 1000.0f;
            setViewMatrix(time);
            Render();
        }

        private void setViewMatrix(float time)
        {
            if (!isMovingCamera)
            {
                Vector3 target = Vector3.Transform(Vector3.Zero, objs[cameraIndex].modelMatrix);
                view = Matrix4x4.CreateLookAt(cameraPos, target, Vector3.UnitZ);
            }
            else
            {
                movingCameras[cameraIndex].update();
                view = movingCameras[cameraIndex].View;
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

        enum Shading
        {
            Gouraud, Phong, constant
        }


        class AET
        {
            public int y_max;
            public float x;
            public float jedenprzezm;

            public AET(int y_max, float x, float jedenprzezm)
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
                        float jedenprzezm = (vectorArr[before].X - vertex.X) / (vectorArr[before].Y - vertex.Y);
                        aet.Add(
                            new AET(
                                (int)vectorArr[before].Y,
                                vertex.X,
                               jedenprzezm)
                            );
                        if (Math.Abs(vectorArr[after].Y - vertex.Y) < 1.0f)
                        {
                            int min = (int)vertex.X;
                            int max = (int)vectorArr[after].X;
                            if (min > max) (min, max) = (max, min);
                            for (; min <= max; min++) action(vectorArr, min, (int)vertex.Y);
                            for (; min <= max; min++) action(vectorArr, min, (int)vectorArr[after].Y);
                        }

                    }
                    if (vectorArr[after].Y >= y)
                    {
                        float jedenprzezm = (vectorArr[after].X - vertex.X) / (vectorArr[after].Y - vertex.Y);
                        aet.Add(
                            new AET(
                                (int)vectorArr[after].Y,
                                vertex.X,
                               jedenprzezm)
                            );
                        if(Math.Abs(vectorArr[after].Y - vertex.Y) <1.0f)
                        {
                            int min = (int)vertex.X;
                            int max = (int)vectorArr[after].X;
                            if (min > max) (min, max) = (max, min);
                            for (; min <= max; min++) action(vectorArr, min, (int)vertex.Y);
                            for (; min <= max; min++) action(vectorArr, min, (int)vectorArr[after].Y);
                        }
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
                        bool isMove = false;
                        while (x < change)
                        {
                            isMove = true;
                            if (paint) action(vectorArr, x, y);
                            x++;
                        }
                        if (isMove)
                        {
                            paint = !paint;
                            isMove = false;
                        }
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

        private void gouraud_btn_CheckedChanged(object sender, EventArgs e)
        {
            shading = Shading.Gouraud;
            Render();
        }

        private void phong_btn_CheckedChanged(object sender, EventArgs e)
        {
            shading = Shading.Phong;
            Render();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            shading = Shading.constant;
            Render();
        }

        private void dayBtn_CheckedChanged(object sender, EventArgs e)
        {
            dayNight = true;
        }

        private void nightBtn_CheckedChanged(object sender, EventArgs e)
        {
            dayNight = false;
        }

        private void fromTrackBar_Scroll(object sender, EventArgs e)
        {
            fogMin = 1.0f + fromTrackBar.Value / 100.0f;
        }

        private void toTrackBar_Scroll(object sender, EventArgs e)
        {
            fogMax = 1.0f + toTrackBar.Value / 100.0f;
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            ka = trackBar3.Value / 10f;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            kd = trackBar1.Value / 10f;
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            ks = trackBar4.Value / 10f;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            m = trackBar2.Value;
        }

        private void upBtn_Click(object sender, EventArgs e)
        {
            movingObject.move(
                Matrix4x4.CreateRotationZ(-acctualAngle) * Matrix4x4.CreateTranslation(0f, moveStep, 0f) * Matrix4x4.CreateRotationZ(acctualAngle)
                );
        }

        private void backBtn_Click(object sender, EventArgs e)
        {
            movingObject.move(
                Matrix4x4.CreateRotationZ(-acctualAngle) * Matrix4x4.CreateTranslation(0f, -moveStep, 0f) * Matrix4x4.CreateRotationZ(acctualAngle)
                );
        }

        private void rightBtn_Click(object sender, EventArgs e)
        {
            movingObject.move(
                Matrix4x4.CreateRotationZ(-acctualAngle) * Matrix4x4.CreateTranslation( moveStep,0f, 0f) * Matrix4x4.CreateRotationZ(acctualAngle)
                );
        }

        private void leftBtn_Click(object sender, EventArgs e)
        {
            movingObject.move(
                Matrix4x4.CreateRotationZ(-acctualAngle)*Matrix4x4.CreateTranslation(-moveStep, 0f, 0f)* Matrix4x4.CreateRotationZ(acctualAngle)
                );
        }

        private void ccwBtn_Click(object sender, EventArgs e)
        {
            acctualAngle -= angleStep;
            movingObject.rotate(Matrix4x4.CreateRotationZ(-angleStep));
        }

        private void cwBtn_Click(object sender, EventArgs e)
        {
            acctualAngle += angleStep;
            movingObject.rotate(Matrix4x4.CreateRotationZ(angleStep));
        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void constCam_CheckedChanged(object sender, EventArgs e)
        {
            cameraIndex = 0;
            isMovingCamera = false;
        }

        private void movingCam_CheckedChanged(object sender, EventArgs e)
        {
            cameraIndex = 0;
            isMovingCamera = true;
        }

        private void prevCam_Click(object sender, EventArgs e)
        {
            int limit = isMovingCamera ? movingCameras.Count()-1 : objs.Count() - 1;
            if (limit == 0) throw new Exception("Error!");
            cameraIndex = cameraIndex == 0 ? limit : cameraIndex - 1;
        }

        private void nextCam_Click(object sender, EventArgs e)
        {
            int limit = isMovingCamera ? movingCameras.Count() - 1 : objs.Count() - 1;
            if (limit == 0) throw new Exception("Error!");
            cameraIndex = cameraIndex == limit ? 0 : cameraIndex + 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            controlledLight.control(Matrix4x4.CreateRotationX(angleStep));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            controlledLight.control(Matrix4x4.CreateRotationX(-angleStep));
        }
    }
}