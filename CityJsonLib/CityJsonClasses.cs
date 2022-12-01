using CityJsonLib;
using System.Text.Json;

//https://www.cityjson.org/specs/1.1.1/


public class PolygonPoint
{
    public int Index;
    public Vector3 Point;
    public PolygonPoint(int index, Vector3 point)
    {
        Index = index;
        Point = point;
    }
}

public class CityJsonUtil
{
    public static string CreateCityJsonFromSurfaces(Dictionary<string, double[][][][]> SurfaceVerticesDict)
    {
        var cityjson = new Rootobject();
        cityjson.version = "1.1";
        cityjson.type = "CityJSON";

        cityjson.metadata = new Metadata()
        {
            geographicalExtent = GetGeoGraphicalExtent(SurfaceVerticesDict)
        };

        cityjson.transform = new Transform()
        {
            scale = new float[] { 0.001f, 0.001f, 0.001f },
            translate = new float[] { 0, 0, 0 }
        };

        cityjson.extensions = new Extensions()
        {
            Generic = new Generic()
            {
                url = "https://cityjson.org/extensions/download/generic.ext.json",
                version = "1.0"
            }
        };

        List<int[][]> allvertices = new List<int[][]>();

        cityjson.CityObjects = new Dictionary<string, CityObject>();

        foreach(var surfaces in SurfaceVerticesDict)
        {
            var cityverts = CreateVertices(surfaces.Value);

            int vertcountIndex = allvertices.SelectMany(o => o).Count();

            var cityobject = CreateCityObject(surfaces.Value,vertcountIndex);
            cityjson.CityObjects.Add(surfaces.Key, cityobject);

            allvertices.Add(cityverts);
        }

        cityjson.vertices = allvertices.SelectMany(o => o).ToArray();

        //FixBoundaries(cityjson);
            
        string jsonString = JsonSerializer.Serialize(cityjson);
        return jsonString;
    }

    private static void FixBoundaries(Rootobject cityjson)
    {
        
        int vertcounter = 0;

        foreach(var cityobject in cityjson.CityObjects.Values)
        {
            for(int i=0; i<cityobject.geometry[0].boundaries.Length; i++)
            {                

                List<PolygonPoint> points = new List<PolygonPoint>();

                foreach (var vertIndex in cityobject.geometry[0].boundaries[i][0])                    
                {
                    var vert = cityjson.vertices[vertIndex];

                    points.Add(new PolygonPoint(vertcounter, new Vector3(vert[0], vert[1], vert[2])));
                    vertcounter++;
                }

                var outerAndInner = GetOuterAndInnerPolygons(points);

                if(outerAndInner.Count > 1)
                {
                    cityobject.geometry[0].boundaries[i] = new int[outerAndInner.Count][];
                    cityobject.geometry[0].boundaries[i][0] = outerAndInner[0].Select(o => o.Index).ToArray();

                    for(int j = 1; j<outerAndInner.Count; j++)
                    {
                        //gaten polygonen moeten andere kant op draaien
                        cityobject.geometry[0].boundaries[i][j] = outerAndInner[j].Select(o => o.Index).Reverse().ToArray();                        
                    }

                    
                }

            }
        }
        //cityjson.CityObjects.Values.First().geometry[0].boundaries
    }

    private static int[][] CreateVertices(double[][][][] SurfaceVertices)
    {
        var totalvertices = SurfaceVertices.SelectMany(l => l).SelectMany(m=>m)
                                            .Select(o => o.Select(v => (int)(v * 1000))
                                                .ToArray()
                                            )
                                            .ToArray();
        return totalvertices;              
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="SurfaceVertices">
    /// - An array of surfaces. 
    /// - A surface had multiple points. 
    /// - A point is an array of 3 vertices</param>
    /// <param name="lastVertIndex"></param>
    /// <returns></returns>
    private static CityObject CreateCityObject(double[][][][] SurfaceVertices, int lastVertIndex)
    {
        var cityObject = new CityObject();

        var list = new List<Geometry>();
        Geometry geom = new Geometry();
        geom.lod = "1";
        geom.type = "MultiSurface";

        var surfaceCount = SurfaceVertices.Length;

        var boundaries = new int[surfaceCount][][];

        

        int vertcounter = lastVertIndex;

        for (int i = 0; i < surfaceCount; i++)
        {
            var boundaryCount = SurfaceVertices[i].Length;
            boundaries[i] = new int[boundaryCount][];

            //elke surface heeft 1 of meerdere boudaries(1 outer en mogelijke inners)
            for (int j=0;j< boundaryCount; j++)
            {

                var vertcount = SurfaceVertices[i][j].Length;
                boundaries[i][j] = new int[vertcount];

                for (int t = 0; t < vertcount; t++)
                {
                    boundaries[i][j][t] = vertcounter;
                    vertcounter++;
                }

            }

         
        }
        //TODO reuse vertices
        geom.boundaries = boundaries;
        list.Add(geom);

        cityObject.geometry = list.ToArray();
        cityObject.attributes = new Attributes() { function = "something" };
        cityObject.type = "GenericCityObject";

        return cityObject;

    }

    /// <summary>
    /// geographicalExtent
    /// While this can be extracted from the dataset itself, it is often useful to store it. 
    /// It may be stored as an array with 6 values: [minx, miny, minz, maxx, maxy, maxz]. 
    /// Notice that these are in the real coordinates of the dataset (based on § 5.5 referenceSystem (CRS)) 
    /// and not after the coordinates have been compressed with the "transform" property (§ 4 Transform Object).
    /// </summary>
    /// <returns>a float array with 6 values [minx, miny, minz, maxx, maxy, maxz].</returns>
    public static float[] GetGeoGraphicalExtent(Dictionary<string, double[][][][]> surfaceVertices)
    {
        var vertices = surfaceVertices.Values
            .SelectMany(x => x)
            .SelectMany(o => o)
            .SelectMany(v => v).ToArray();

        var xarray = vertices.Select(x => x[0]).ToArray();
        var yarray = vertices.Select(x => x[1]).ToArray();
        var zarray = vertices.Select(x => x[2]).ToArray();

        return new float[6] { 
                (float)xarray.Min(), 
                (float)yarray.Min(),
                (float)zarray.Min(),
                (float)xarray.Max(),
                (float)yarray.Max(),
                (float)zarray.Max()
        };
    }

    /// <summary>
    /// https://www.geeksforgeeks.org/program-find-mid-point-line/
    /// https://stackoverflow.com/questions/2034540/calculating-area-of-irregular-polygon-in-c-sharp
    /// https://web.archive.org/web/20120410040052/http://blog.csharphelper.com/2010/01/04/calculate-a-polygons-area-in-c.aspx
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static float GetPolygonArea(List<Vector2> points)
    {
        var area = points.Take(points.Count - 1)
        .Select((p, i) => (points[i + 1].x - p.x) * (points[i + 1].y + p.y))
        .Sum() / 2;

        return area;
    }

    public static List<List<PolygonPoint>> GetOuterAndInnerPolygons(List<PolygonPoint> outer)
    {
        List<List<PolygonPoint>> inners = new List<List<PolygonPoint>>();
        List<List<PolygonPoint>> outerAndInners = new List<List<PolygonPoint>>();


        GetInner(false, inners, outer);

        if (inners.Any())
        {
            var removedPointIndices = inners.SelectMany(points => points).Select(o => o.Index);
            var strippedOuter = outer.Where(o => removedPointIndices.Contains(o.Index) == false).ToList();

            //outer strippen van de gevonden inners
            inners.Insert(0, strippedOuter);

            //alle inners toevoegen, dit zijn de gaten
            foreach (var points in inners)
            {
                outerAndInners.Add(points);
            }
        }
        else
        {
            //wanneer geen inners dan outer weer als enige
            outerAndInners.Add(outer);
        }

        //outerAndInner[0] = RemoveEmptyInners(outerAndInner[0]);
        //outerAndInner[0] = RemoveDoubleInner(outerAndInner[0]);

        return outerAndInners;
    }

    /// <summary>
    /// Alleen toevoegen aan lijst als binnenkomende outer list geen zelfde begin en eindpunt hebben
    /// </summary>
    /// <param name="result"></param>
    private static void GetInner(bool fromInnerRing,  List<List<PolygonPoint>> result, List<PolygonPoint> outer)
    {
        bool found = false;

        for (int i = 1; i < outer.Count; i++)
        {
            var startPoint = outer[i];

            List<PolygonPoint> list = new List<PolygonPoint>();
            list.Add(startPoint);

            for (int j = i + 1; j < outer.Count; j++)
            {
                var testpoint = outer[j];
                list.Add(testpoint);

                if (startPoint.Point.Equals(testpoint.Point))
                {
                    found = true;
                    i = j;
                    GetInner(true, result, list);
                    break;
                }
            }

        }

        if (fromInnerRing && found == false)
        {
            result.Add(outer);
        }
    }

}

public class Rootobject
{

    public Dictionary<string, CityObject> CityObjects { get; set; }
    public string type { get; set; }
    public string version { get; set; }
    public int[][] vertices { get; set; }
    public Metadata metadata { get; set; }
    public Transform transform { get; set; }
    public Extensions extensions { get; set; }
}

public class CityObject
{
    public Geometry[] geometry { get; set; }
    public Attributes attributes { get; set; }
    public string type { get; set; }
}

public class Attributes
{
    public string function { get; set; }
}

public class Geometry
{
    public int[][][] boundaries { get; set; }
    public string lod { get; set; }
    public string type { get; set; }
}

public class Metadata
{
    public float[] geographicalExtent { get; set; }
}

public class Transform
{
    public float[] scale { get; set; }
    public float[] translate { get; set; }
}

public class Extensions
{
    public Generic Generic { get; set; }
}

public class Generic
{
    public string url { get; set; }
    public string version { get; set; }
}
