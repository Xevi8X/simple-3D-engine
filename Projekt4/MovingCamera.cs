using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Projekt4
{
    public class MovingCamera
    {
        Obj owner;
        Vector3 relativeCameraPos;
        Vector3 relativeLookAt;
        public Vector3 cameraPos { get; private set; }

        public Matrix4x4 View { get; private set; }


        public MovingCamera(Obj owner, Vector3 relativeCameraPos, Vector3 relativeLookAt)
        {
            this.owner = owner;
            this.relativeCameraPos = relativeCameraPos;
            this.relativeLookAt = relativeLookAt;
            update();
        }

        public void update()
        {
            cameraPos = Vector3.Transform(relativeCameraPos, owner.modelMatrix);
            Vector3 lookAt = Vector3.Transform(relativeLookAt, owner.modelMatrix);
            View = Matrix4x4.CreateLookAt(cameraPos, lookAt, Vector3.UnitZ);
        }
    }
}
