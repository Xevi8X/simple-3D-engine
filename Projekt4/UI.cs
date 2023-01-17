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
                Matrix4x4.CreateRotationZ(-acctualAngle) * Matrix4x4.CreateTranslation(moveStep, 0f, 0f) * Matrix4x4.CreateRotationZ(acctualAngle)
                );
        }

        private void leftBtn_Click(object sender, EventArgs e)
        {
            movingObject.move(
                Matrix4x4.CreateRotationZ(-acctualAngle) * Matrix4x4.CreateTranslation(-moveStep, 0f, 0f) * Matrix4x4.CreateRotationZ(acctualAngle)
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
            int limit = isMovingCamera ? movingCameras.Count() - 1 : objs.Count() - 1;
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
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            fov = (int)numericUpDown1.Value;
            perspective = Matrix4x4.CreatePerspectiveFieldOfView(fov / 180.0f * MathF.PI, ((float)canva.Width) / canva.Height, 1.0f, 3.0f);
        }
    }
}
