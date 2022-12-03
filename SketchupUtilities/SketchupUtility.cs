using System.Linq;
using SketchUpNET;

namespace SketchupUtilities
{

    public class SketchupUtility
    {
        static Dictionary<string, double[][][][]> surfaceVertices = new Dictionary<string, double[][][][]>();
        static int surfaceCounter = 0;

        static SketchUpNET.Transform transformOne = new SketchUpNET.Transform() { Data = new double[] {
                                                                                  1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1},
                                                                                  X = 0, 
                                                                                  Y = 0,
                                                                                  Z = 0,
                                                                                  Scale = 1};


        public static Dictionary<string, double[][][][]>   GetSurfaceVerticesFromSketchupFile(string path)
        {
            surfaceVertices = new Dictionary<string, double[][][][]>();

            SketchUpNET.SketchUp skp = new SketchUpNET.SketchUp();
            skp.LoadModel(path, false);

            //if (skp.Surfaces.Any())
            //{
            //    surfaceVertices.Add($"Surfaces:", GetSurfaceVertices(skp.Surfaces, transformOne));
            //    surfaceCounter++;
            //}

            //if (skp.Instances.Any())
            //{
            //    var instances = skp.Instances.Where(o => ((SketchUpNET.Component)o.Parent).Name != "Marc"
            //                                          && ((SketchUpNET.Component)o.Parent).Name != "Chris"
            //                                          && ((SketchUpNET.Component)o.Parent).Surfaces.Any()
            //    );

            //    //var sur = skp.Components.Where(o => o.Value != null && o.Value.Name != "Marc" && o.Value.Name != "Chris" && o.Value.Surfaces.Any());
            //    foreach (var instance in instances)
            //    {
            //        SketchUpNET.Component comp = (SketchUpNET.Component)instance.Parent;
            //        surfaceVertices.Add($"Instances", GetSurfaceVertices(comp.Surfaces, instance.Transformation));
            //        surfaceCounter++;
            //    }


            //}

            //if (skp.Components.Any())
            //{
            //    var components = skp.Components.Where(o => o.Value.Name != "Marc"
            //                                          && o.Value.Name != "Chris"
            //                                          && o.Value.Surfaces.Any()
            //    );

            //    foreach (var component in components)
            //    {
            //        var comp = component.Value;
            //        surfaceVertices.Add($"Components", GetSurfaceVertices(comp.Surfaces, transformOne));
            //        surfaceCounter++;
            //    }
            //}

            int groupIdx = 0;
            foreach (var group in skp.Groups)
            {
                var transforms = new List<Transform>();
                transforms.Add(group.Transformation);


                //ParseGroup($"Group{groupIdx}",group);
                ParseGroup(group, transforms);
                groupIdx++;
            }

            return surfaceVertices;
        }

        //public static void ParseGroup(string groupPrefix, SketchUpNET.Group group)
        public static void ParseGroup(Group group, List<Transform> transforms)
        {
            if (group.Surfaces.Any() && transforms.Count == 3)
            {
                //surfaceVertices.Add(GetGroupString(groupPrefix, group), GetSurfaceVertices(group.Surfaces, group.Transformation  ));
                surfaceVertices.Add($"{surfaceCounter}-group.Name", GetSurfaceVertices(group.Surfaces, transforms));
                surfaceCounter++;
            }

            if (group.Groups.Any())
            {

                foreach (var group2 in group.Groups)
                {
                    List<Transform> transforms2 = new List<Transform>();
                    transforms2.AddRange(transforms);
                    transforms2.Add(group2.Transformation);

                    //group2.Transformation.X += group.Transformation.X;
                    //group2.Transformation.Y += group.Transformation.Y;
                    //group2.Transformation.Z += group.Transformation.Z;

                    //group2.Transformation.Data[12] += group.Transformation.Data[12]; //X
                    //group2.Transformation.Data[13] += group.Transformation.Data[13]; //Y
                    //group2.Transformation.Data[14] += group.Transformation.Data[14]; //Y

                    //group2.Transformation.Scale *= group.Transformation.Scale;//TODO scale zit niet hier maar in Data array..

                    //ParseGroup(GetGroupString(groupPrefix, group),group2);
                    ParseGroup(group2, transforms2);
                }
            }
        }

        static string GetGroupString(string groupPrefix, SketchUpNET.Group group)
        {
            var data1 = group.Transformation.Data[0];
            var data2 = group.Transformation.Data[1];
            var data3 = group.Transformation.Data[2];
            var data4 = group.Transformation.Data[3]; //not used
            var data5 = group.Transformation.Data[4];
            var data6 = group.Transformation.Data[5];
            var data7 = group.Transformation.Data[6];
            var data8 = group.Transformation.Data[7]; //not used
            var data9 = group.Transformation.Data[8];
            var data10 = group.Transformation.Data[9];
            var data11 = group.Transformation.Data[10];
            var data12 = group.Transformation.Data[11]; //not used
            var data13 = group.Transformation.Data[12]; //X
            var data14 = group.Transformation.Data[13]; //Y
            var data15 = group.Transformation.Data[14]; //Z
            var data16 = group.Transformation.Data[15]; //Scale

            return $"{groupPrefix}/{data1:0.##}:{data2:0.##}:{data4:0.##}:{data5:0.##}:{data6:0.##}:{data8:0.##}:{data9:0.##}:{data10:0.##}:{data12:0.##}:{data13:0.##}:{data14:0.##}:{data15:0.##}:{data16:0.##}";


        }

        public static bool VertInList(List<SketchUpNET.Vertex> list, SketchUpNET.Vertex vert)
        {
            foreach (var vertex in list)
            {
                if (VertEquals(vert, vertex)) return true;
            }
            return false;
        }

        public static bool VertEquals(SketchUpNET.Vertex a, SketchUpNET.Vertex b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        
        /// <summary>
        /// List of Sufacevertices where each surface has an outer ring and possible inner ring(holes)
        /// </summary>                
        //public static double[][][][] GetSurfaceVertices(List<SketchUpNET.Surface> surfaces, SketchUpNET.Transform transform)
        public static double[][][][] GetSurfaceVertices(List<SketchUpNET.Surface> surfaces, List<Transform> transforms)        
        {
            var surfaceVertices = new double[surfaces.Count][][][];

            for (int i = 0; i < surfaces.Count; i++)
            {
                var outerstarts = surfaces[i].OuterEdges.Edges.Select(o => o.Start).ToList();
                var outerends = surfaces[i].OuterEdges.Edges.Select(o => o.End).ToList();

                var innerstarts = from o in surfaces[i].InnerEdges
                                  select o.Edges.Select(o => o.Start).ToList();

                var innerends = from o in surfaces[i].InnerEdges
                                  select o.Edges.Select(o => o.End).ToList();


                List<SketchUpNET.Vertex> outerPoints = new List<SketchUpNET.Vertex>();
                Dictionary<int, List<SketchUpNET.Vertex>> innerPoints = new Dictionary<int, List<SketchUpNET.Vertex>>();

                //we gaan ervan uit dat de volgorde van de polygoonpunten in de lijst van vertices zitten.
                foreach (var vert in surfaces[i].Vertices)
                {
                    if (VertInList(outerstarts, vert) || VertInList(outerends, vert))
                    {
                        outerPoints.Add(vert);
                    }
                    else
                    {
                        //welke inner is het?
                        for (int j = 0; j < innerstarts.Count(); j++)
                        {
                            var list = innerstarts.ToList()[j];

                            if (VertInList(list, vert))
                            {
                                if (innerPoints.ContainsKey(j) == false)
                                {
                                    innerPoints.Add(j,new List<SketchUpNET.Vertex>() );                                    
                                }

                                innerPoints[j].Add(vert);
                                break;
                            }
                        }
                    }
                    
                }

                surfaceVertices[i] = new double[innerPoints.Count + 1][][];
                //surfaceVertices[i][0] = outerPoints.Select(o => transform.GetTransformed(o)).Select( t=>  new double[3] { t.X,t.Y,t.Z  }).ToArray();
                //surfaceVertices[i][0] = outerPoints.Select(o => GetTransformed(o, transform.Data, transform.Scale)).Select(t => new double[3] { t.X, t.Y, t.Z }).ToArray();
                surfaceVertices[i][0] = outerPoints.Select(o => GetTransformedByTransforms(o, transforms)).Select(t => new double[3] { t.X, t.Y, t.Z }).ToArray();

                for (int t = 0; t < innerPoints.Count; t++)
                {
                    //surfaceVertices[i][t + 1] = innerPoints[t].Select(o => transform.GetTransformed(o)).Select(t => new double[3] { t.X,t.Y, t.Z }).ToArray();
                    surfaceVertices[i][t + 1] = innerPoints[t].Select(o => GetTransformedByTransforms(o, transforms)).Select(t => new double[3] { t.X, t.Y, t.Z }).ToArray();
                }

            }
            return surfaceVertices;
        }

        static Vertex GetTransformedByTransforms(Vertex point, List<Transform> transforms)
        {
            return transforms.Last().GetTransformed(point);

            ////transform vertex from  transform hierarchy
            //transforms.Reverse();

            //foreach(var transform in transforms)
            //{
            //    point = GetTransformed(point, transform.Data);
            //}

            //return point;
        }
        

        static Vertex GetTransformed(Vertex point, double[] Data)
        {
            double Scale = Data[15];

            Vertex vertex = new Vertex(point.X, point.Y, point.Z);
            double num = 1.0 / Scale;
            vertex.X = (Data[0] * point.X) + (point.Y * Data[4]) + (point.Z * Data[8]) + Data[12];
            vertex.Y = (Data[1] * point.X) + (point.Y * Data[5]) + (point.Z * Data[9]) + Data[13];
            vertex.Z = (Data[2] * point.X) + (point.Y * Data[6]) + (point.Z * Data[10]) + Data[14];
            vertex.X *= num;
            vertex.Y *= num;
            vertex.Z *= num;
            return vertex;
        }

        public static double[][] GetVertices(double[][][] verts)
        {
            return verts.SelectMany(v => v).ToArray();
        }

    }
}