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
                        if (Math.Abs(vectorArr[after].Y - vertex.Y) < 1.0f)
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
    }
}
