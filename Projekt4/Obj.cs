using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Projekt4
{
    public class Obj
    {
        public string name;
        public List<Vector4> points;
        public List<Vector3> normalVector;
        public List<int[]> faces;
        public Matrix4x4 modelMatrix = Matrix4x4.Identity;
        public Color color;

        static Random random = new Random();

        public Obj(string filePath)
        {
            loadFile(filePath);
            unchecked
            {
                color = Color.FromArgb((int)0xFF000000 + (random.Next(0xFFFFFF) & 0x7F7F7F));
            }
        }

        private void loadFile(string filePath)
        {
            points = new List<Vector4>();
            faces = new List<int[]>();

            float maxCoord = 0.0f;

            foreach (string line in System.IO.File.ReadLines(filePath))
            {
                if (line.Count() == 0 || line.First() == '#') continue;
                string[] args = line.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                switch (args[0])
                {
                    case "o":
                        {
                            name = args[1];
                            break;
                        }
                    case "v":
                        {
                            Vector4 v = new Vector4(float.Parse(args[1], CultureInfo.InvariantCulture), float.Parse(args[2], CultureInfo.InvariantCulture), float.Parse(args[3], CultureInfo.InvariantCulture), 1.0f);
                            checkMax(v, ref maxCoord);
                            points.Add(v);
                            break;
                        }
                    case "f":
                        {
                            int[] tmp = new int[args.Length-1];
                            for (int i = 1; i < args.Length; i++)
                            {
                                string[] indices = args[i].Split("/");
                                tmp[i-1] = int.Parse(indices[0]) - 1;
                            }
                            faces.Add(tmp);
                            break;
                        }
                }
            }
            points = points.Select(v => rescale(0.9f / maxCoord, v)).ToList();
        }

        private void checkMax(Vector4 v, ref float maxCoord)
        {
            float max = 0.0f;
            max = Math.Max(max, v.X);
            max = Math.Max(max, v.Y);
            max = Math.Max(max, v.Z);
            maxCoord = Math.Max(maxCoord, max);
        }

        private Vector4 rescale(float alpha, Vector4 vec)
        {
            vec.X *= alpha;
            vec.Y *= alpha;
            vec.Z *= alpha;
            return vec;
        }
    }
}

