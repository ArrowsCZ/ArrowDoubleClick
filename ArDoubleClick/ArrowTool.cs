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
}
