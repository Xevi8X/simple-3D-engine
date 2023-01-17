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

            addObjLightAndCameras();

            timer.Interval = 100;
            timer.Start();
        }

        void addObjLightAndCameras()
        {
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

            lightsSources.Add(new Light(new Vector3(0, 0, 50), new Vector3(0, 0, -1), 0));
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
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
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

        enum Shading
        {
            Gouraud, Phong, constant
        }
    }
}