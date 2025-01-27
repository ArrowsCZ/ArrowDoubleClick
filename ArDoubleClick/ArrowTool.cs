using Autodesk.AutoCAD.Geometry;

namespace ArrowTools;

public static class ArrowTool
{
    /// <summary>
    /// 獲取當前dll的完整運行路徑。
    /// </summary>
    /// <returns>當前dll的完整路徑</returns>
    public static string GetAssemblyFullPath()
    {
        // 此方法高版本.net已棄用
        //string assemblyPath = Assembly.GetExecutingAssembly().CodeBase;
        // 获取当前程序集的路径
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var url = new UriBuilder(assemblyPath);
        assemblyPath = Uri.UnescapeDataString(url.Path);

        return Path.GetFullPath(assemblyPath); // 这里是\\
    }

    /// <summary>
    /// 检查给定的点是否在多边形内部.
    /// </summary>
    /// <param name="point">要检查的三维点.</param>
    /// <param name="pline">多边形的边界线.</param>
    /// <returns>如果点在多边形内部返回真，反之返回假.</returns>
    /// <remarks>
    /// <list type="number">
    ///     <item>
    ///         <param name="point">要检查的三维点.</param>
    ///     </item>
    ///     <item>
    ///         <param name="pline">多边形的边界线.</param>
    ///     </item>
    /// </list>
    /// </remarks>
    public static bool IsPointInside(this Point3d point, Polyline pline)
    {
        double tolerance = Tolerance.Global.EqualPoint;
        using MPolygon mpg = new();
        mpg.AppendLoopFromBoundary(pline, true, tolerance);

        return mpg.IsPointInsideMPolygon(point, tolerance).Count == 1;
    }

    /// <summary>
    /// 圆转换为多段线。
    /// </summary>
    /// <param name="cir">要转换的圆。</param>
    /// <returns>转换后的多段线。</returns>
    public static Polyline ToPolyline(this Circle cir)
    {
        Polyline poly = new();
        poly.SetPropertiesFrom(cir);

        double r = cir.Radius;
        Point3d cc = cir.Center;
        Point2d p1 = new(cc.X + r, cc.Y);
        Point2d p2 = new(cc.X - r, cc.Y);

        poly.AddVertexAt(0, p1, 1, 0, 0);
        poly.AddVertexAt(1, p2, 1, 0, 0);
        poly.AddVertexAt(2, p1, 1, 0, 0);
        poly.Closed = true;

        return poly;
    }

    public static Polyline ToPolyline(this Extents3d geoEx)
    {
        var pts = new List<Point2d>()
        {
            new(geoEx.MinPoint.X, geoEx.MinPoint.Y),
            new(geoEx.MinPoint.X, geoEx.MaxPoint.Y),
            new(geoEx.MaxPoint.X, geoEx.MaxPoint.Y),
            new(geoEx.MaxPoint.X, geoEx.MinPoint.Y),
        };
        Polyline pline = new();
        pline.AddVertexAt(0, pts[0], 0, 0, 0);
        pline.AddVertexAt(1, pts[1], 0, 0, 0);
        pline.AddVertexAt(2, pts[2], 0, 0, 0);
        pline.AddVertexAt(3, pts[3], 0, 0, 0);
        pline.Closed = true;
        return pline;
    }
}
