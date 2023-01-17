using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Projekt4
{
    internal class Light
    {
        public float[] color;
        private Vector3 source;
        private Vector3 directory;
        Func<float, Vector3>? sourceFunc;
        Func<float, Vector3>? directoryFunc;
        Obj? owner;
        Matrix4x4 controlable;
        private float m;


        public Light(Vector3 source, Vector3 directory, float m)
        {
            this.source = source;
            this.directory = directory;
            this.m = m;
            color = new float[3] { 1.0f, 1.0f, 1.0f };
        }

        public Light(Func<float,Vector3> sourceFunc, Func<float, Vector3> directoryFunc, float m, Obj? owner = null, Matrix4x4? controlable = null)
        {
            this.source = sourceFunc(0.0f);
            this.directory = directoryFunc(0.0f);
            this.sourceFunc = sourceFunc;
            this.directoryFunc = directoryFunc;
            this.m = m;
            color = new float[3] { 1.0f, 1.0f, 1.0f };
            this.owner = owner;
            this.controlable = controlable == null ? Matrix4x4.Identity : controlable.Value;
        }

        public Light(Vector3 source, Vector3 directory, float m,float R,float G,float B)
        {
            this.source = source;
            this.directory = directory;
            this.sourceFunc = (_) => source;
            this.directoryFunc = (_) => directory;
            this.m = m;
            color = new float[3] { R, G, B };
        }

        public (bool, Vector3, float[]) isInRange(Vector3 point)
        {
            Vector3 L = source - point;
            L = Vector3.Normalize(L);
            float cos = Vector3.Dot(-L, directory);
            if (cos > 0)
            {
                cos = MathF.Pow(cos, m);
                float[] res = color.Select(x => x * cos).ToArray();
                return (true, L, res);
            }
                
            return (false, Vector3.Zero, new float[3]);
        }
        public void update(float time)
        {
            if(sourceFunc != null) this.source = sourceFunc(time);
            if(directoryFunc != null) this.directory = directoryFunc(time);
            if (owner != null)
            {
                this.source = Vector3.Transform(this.source, owner.modelMatrix);
                this.directory = Vector3.Transform(Vector3.TransformNormal(this.directory, owner.modelMatrix),controlable);
            }
        }

        public void control(Matrix4x4 transform)
        {
            controlable = transform * controlable;
        }
    }
}
