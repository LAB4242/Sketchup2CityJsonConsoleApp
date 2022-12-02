// See https://aka.ms/new-console-template for more information
using T3D.CityJsonLib;

var apppath = AppDomain.CurrentDomain.BaseDirectory;
string srcpath = string.Empty;
string dstpath = string.Empty;


if(args.Length > 0)
{
    if(args[0].ToLower().EndsWith("skp") == false)
    {
        Console.Error.WriteLine($"File is not a Sketchup file: {args[0]}");
        Environment.Exit(-1);
    }

    srcpath = Path.Combine(apppath,args[0]);
    
    if (File.Exists(srcpath))
    {        
        dstpath = GetDestinationPath(apppath, srcpath);
    }
    else
    {
        Console.Error.WriteLine($"Can't find file: {srcpath}");
        Environment.Exit(-2);
    }    
}
else
{
    srcpath = Path.Combine(apppath, "Shed-2019.skp");
    dstpath = GetDestinationPath(apppath, srcpath);

    //srcpath = Path.Combine(apppath, "ShedOuter.skp");
    //dstpath = GetDestinationPath(apppath, srcpath);

    //srcpath = Path.Combine(apppath, "ShedInner.skp");
    //dstpath = GetDestinationPath(apppath, srcpath);
}


var surfaceVertices = SketchupUtilities.SketchupUtility.GetSurfaceVerticesFromSketchupFile(srcpath);
var cityjson = CityJsonUtil.CreateCityJsonFromSurfaces(surfaceVertices);

File.WriteAllText(dstpath, cityjson);
Console.WriteLine($"Sketchup file converted to: {dstpath}");


string GetDestinationPath(string apppath, string srcpath)
{
    FileInfo finfo = new FileInfo(srcpath);
    dstpath = Path.Combine(apppath, finfo.Name.Replace(".skp", ".json"));
    return dstpath;
}