namespace ArrowTools;

public static class ArrowTool
{
    /// <summary>
    /// 获取dll运行路径,并添加入注册表供lisp调用
    /// </summary>
    /// <param name="full">true是含有文件名</param>
    /// <returns>当前dll路径</returns>
    public static string GetAssemblyPath(bool full)
    {
        //string assemblyPath = Assembly.GetExecutingAssembly().CodeBase;
        // 获取当前程序集的路径
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        /*
         * 似乎直接切割比较快
         * path = path.Replace("file:/// ", string.Empty).Replace("/", "\\");
         */
        var url = new UriBuilder(assemblyPath);
        assemblyPath = Uri.UnescapeDataString(url.Path); // 这里路径是/

        if (full)
            return Path.GetFullPath(assemblyPath); // 这里是\\

        assemblyPath = Path.GetDirectoryName(assemblyPath); // 这里是\\
        if (assemblyPath == null)
            throw new Exception("获取路径失败");

        return assemblyPath;
    }
}
