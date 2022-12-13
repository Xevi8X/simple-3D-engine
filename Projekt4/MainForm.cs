using System.Numerics;

namespace Projekt4
{
    public partial class MainForm : Form
    {
        const float virtualX = 1.0f;
        const float virtualY = 1.0f;

        List<Obj> objs = new List<Obj>();

        float yaw = 0.0f;
        float pitch = 0.0f;
        float roll = 0.0f;

        public MainForm()
        {
            InitializeComponent();
            objs.Add(new Obj(@"torus.obj"));
            objs.Add(new Obj(@"sfera.obj"));
            objs.Add(new Obj(@"stozek.obj"));
            //objs.Add(new Obj(@"cat.obj"));
            Render();
        }

        void Render()
        {
            Bitmap nextBitmap = new Bitmap(canva.Width, canva.Height);
            Matrix4x4 rotation = Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, roll);

            using (Graphics g = Graphics.FromImage(nextBitmap))
            {
                g.Clear(Color.Blue);
                drawCoords(g, rotation);
                foreach (var obj in objs)
                {
                    drawObj(g, rotation, obj);
                }
            }
            canva.Image?.Dispose();
            canva.Image = (Image)nextBitmap;
        }

        void drawCoords(Graphics g, Matrix4x4 rotation)
        {
            float size = 0.1f;
            Pen p = new Pen(Color.Red);
            myDrawLine(g, p, rotation, Vector3.Zero, size * Vector3.UnitX);
            myDrawLine(g, p, rotation, Vector3.Zero, size * Vector3.UnitY);
            myDrawLine(g, p, rotation, Vector3.Zero, size * Vector3.UnitZ);
        }

        void drawObj(Graphics g, Matrix4x4 rotation, Obj obj)
        {
            Pen p = new Pen(Color.White);
            foreach (int[] face in obj.faces)
            {
                for (int i = 0; i < face.Length - 1; i++)
                {
                    myDrawLine(g, p, rotation, obj.points[face[i]], obj.points[face[i + 1]]);
                }
                myDrawLine(g, p, rotation, obj.points[face.First()], obj.points[face.Last()]);
            }
        }

        void myDrawLine(Graphics g, Pen p, Matrix4x4 rotation, Vector3 from, Vector3 to)
        {
            Vector2 fromCanva = virtualToCanva(projection(Vector3.Transform(from, rotation)));
            Vector2 toCanva = virtualToCanva(projection(Vector3.Transform(to, rotation)));
            g.DrawLine(p, (PointF)fromCanva, (PointF)toCanva);
        }

        Vector2 virtualToCanva(Vector2 virtualPos)
        {
            float X = virtualPos.X;
            float Y = virtualPos.Y;

            float newX = 0.5f * canva.Width * (1.0f + X / virtualX);
            float newY = 0.5f * canva.Height * (1.0f - Y / virtualY);
            return new Vector2(newX, newY);
        }

        Vector2 canvaToVirtual(Vector2 canvaPos)
        {
            throw new NotImplementedException();
        }

        Vector2 projection(Vector3 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        private void canva_Resize(object sender, EventArgs e)
        {
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            Render();
        }

        private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case 'q':
                    yaw += 0.1f;
                    break;
                case 'a':
                    yaw -= 0.1f;
                    break;
                case 'w':
                    pitch += 0.1f;
                    break;
                case 's':
                    pitch -= 0.1f;
                    break;
                case 'e':
                    roll += 0.1f;
                    break;
                case 'd':
                    roll -= 0.1f;
                    break;
            }
            Render();
        }
    }
}