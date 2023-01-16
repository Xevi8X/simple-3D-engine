using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Projekt4
{
    public static class Extentions
    {
        public static Vector3 toVec3(this Vector4 v)
        {
            return new Vector3(v.X/v.W, v.Y / v.W, v.Z / v.W);
        }
    }
}
