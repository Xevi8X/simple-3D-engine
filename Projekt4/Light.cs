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
        private float cosAngle;


        public Light(Vector3 source, Vector3 directory, float cosAngle)
        {
            this.source = source;
            this.directory = directory;
            this.cosAngle = cosAngle;
            color = new float[3] { 1.0f, 1.0f, 1.0f };
        }

        public Light(Vector3 source, Vector3 directory, float cosAngle,float R,float G,float B)
        {
            this.source = source;
            this.directory = directory;
            this.cosAngle = cosAngle;
            color = new float[3] { R, G, B };
        }

        public (bool,Vector3) isInRange(Vector3 point)
        {
            Vector3 L = source - point;
            L = Vector3.Normalize(L);
            if (Vector3.Dot(-L, directory) > cosAngle) return (true, L);
            return (false, Vector3.Zero);
        }
    }
}
