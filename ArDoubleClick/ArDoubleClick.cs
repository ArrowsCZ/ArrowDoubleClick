using System.Xml.Linq;
using Autodesk.AutoCAD.Geometry;

namespace ArrowTools;

public static class ArDoubleClick
{
    // todo:2025-01-22  使用類結構及xml反序列化重構代碼
    static ArDoubleClick()
    {
        if (!Directory.Exists(Path.GetDirectoryName(SettingsFile)))
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!); // 在指定的路径下创建一个新的文件夹。
        if (!File.Exists(SettingsFile))
        {
            File.WriteAllBytes(SettingsFile, ResourceTools.ClickSettings); // 在指定的路径下写入一个新的配置。
#if Debug
            Acaop.ShowAlertDialog($"配置文件缺失，已从资源中写入文件:{SettingsFile}！");
#endif
        }

        InitClick();
    }

    private static Database Database => HostApplicationServices.WorkingDatabase;
    private static Document Document => Acaop.DocumentManager.MdiActiveDocument;
    private static Editor Editor => Document.Editor;

    /// <summary>
    /// 初始化命令設置，從 XML 文件中讀取配置並設置相應的變量。
    /// </summary>
    private static void InitClick()
    {
        XDocument doc = XDocument.Load(SettingsFile);

        // 初始化 IsOn
        var isOnElement = doc.Root?.Element("IsOn");
        if (isOnElement != null && bool.TryParse(isOnElement.Value, out bool isOn))
            _isOn = isOn;

        // 初始化 IsQuiescentCommand
        var isQuiescentCommandElements = doc.Root?.Element("IsQuiescentCommand")?.Elements("Command");
        if (isQuiescentCommandElements != null)
            _isQuiescentCommand = [.. isQuiescentCommandElements.Select(element => element.Value)];

        // 初始化 VoteCommand
        var voteCommandElements = doc.Root?.Element("VoteCommand")?.Elements("Command");
        if (voteCommandElements != null)
        {
            _voteCommand = [];
            foreach (var element in voteCommandElements)
            {
                string? type = element.Attribute("Type")?.Value;
                string command = element.Value;
                if (type != null)
                    _voteCommand[type] = command;
            }
        }

        // 初始化 MultiSelectCmd
        var multiSelectCmdElement = doc.Root?.Element("MultiSelectCmd");
        if (multiSelectCmdElement != null)
            _multiSelectCmd = multiSelectCmdElement.Value;
    }

    // todo:2025-01-16  如果雙擊時是多選對象，那麼邏輯如何重新構建
    private static string FilePath =>
        Path.GetDirectoryName(Path.GetDirectoryName(GetAssemblyFullPath())) ?? string.Empty;

    /// <summary>
    /// 配置類文件路徑。
    /// </summary>
    private static readonly string SettingsFile = Path.Combine(FilePath, "ClickSettings.xml");

    /// <summary>
    /// 存储在不同情况下双击时要执行的命令。
    /// <para>0x01 模型空间：启动 ** 命令。</para>
    /// <para>0x02 布局空间（默认视口）：启动 ** 命令。</para>
    /// <para>0x03 布局空间（活动视口）：启动 ** 命令。</para>
    /// </summary>
    private static string[] _isQuiescentCommand = [];

    /// <summary>
    /// 当双击特定类型的对象时，将触发相应的命令。
    /// </summary>
    private static Dictionary<string, string> _voteCommand = [];

    private static readonly Dictionary<string, string> CmdClassMap = new() { { "VPMAX", "VIEWPORT" } };
    private static string _multiSelectCmd = string.Empty; // 多選發送的命令

    private static bool _isOn = true; // 是否開啟雙擊
    private static bool _isClick; // 是否是雙擊操作

    // 2025-01-23  Bug修復：打開多個文檔時，雙擊多次觸發雙擊事件，導致命令重複執行
    //private static int _isCurrent = int.MaxValue; // 是否是当前文档

    [CommandMethod(nameof(ArS_Double_Click_Init))]
    public static void ArS_Double_Click_Init()
    {
        // 更新 XML 文件中的 IsOn 值
        if (!File.Exists(SettingsFile))
            File.WriteAllBytes(SettingsFile, ResourceTools.ClickSettings); // 在指定的路径下写入一个新的配置。

        PromptKeywordOptions options = new(string.Empty);
        options.Keywords.Add("S", "S", "更新");
        options.Keywords.Add("Y", "Y", "开启");
        options.Keywords.Add("N", "N", "关闭");
        options.Keywords.Default = _isOn ? "Y" : "N";
        options.AppendKeywordsToMessage = false; //不将关键字列表添加到提示信息中
        options.Message = $"\n配置更新或双击设置：" + $"\n[更新(S)/开启(Y)/关闭(N)]<{(_isOn ? "Y" : "N")}>";
        PromptResult pr = Editor.GetKeywords(options);
        if (pr.Status != PromptStatus.OK)
        {
            Editor.WriteMessage("\n*取消*");
            return;
        }

        bool isOnTemp = _isOn;
        switch (pr.StringResult.ToUpper())
        {
            case "S":
                try
                {
                    InitClick();
                    Editor.WriteMessage($"\n配置文件已更新：{SettingsFile}");
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"配置文件出现错误，需更正：{ex}");
                }
                break;
            case "Y":
                _isOn = true;
                break;
            case "N":
                _isOn = false;
                break;
        }

        // 更新xml中 IsOn 值
        if (isOnTemp == _isOn)
            return;
        XDocument doc = XDocument.Load(SettingsFile);
        var isOnElement = doc.Root?.Element("IsOn");
        if (isOnElement != null)
            isOnElement.Value = _isOn.ToString().ToLower();
        else
            doc.Root?.Add(new XElement("IsOn", _isOn.ToString().ToLower()));
        doc.Save(SettingsFile);

        Editor.WriteMessage($"\n双击设置已：{(_isOn ? "开启" : "关闭")}！");
    }

    /// <summary>
    /// 啟動雙擊功能，通過啟用雙擊事件和否決命令事件。
    /// </summary>
    public static void ArDoubleClickStart()
    {
        Acaop.BeginDoubleClick += Acaop_BeginDoubleClick; // 啟用雙擊事件
        Acaop.DocumentManager.DocumentLockModeChanged += Dm_VetoCommand; // 啟用否決命令事件

        //if ((short)Env.GetVar("DBLCLKEDIT") == 1) // 禁用绘图区域中的双击编辑操作
        //    Env.SetVar("DBLCLKEDIT", 0);
    }

    /// <summary>
    /// 關閉雙擊功能，通過關閉雙擊事件和否決命令事件。
    /// </summary>
    public static void ArDoubleClickEnd()
    {
        Acaop.BeginDoubleClick -= Acaop_BeginDoubleClick; // 關閉雙擊事件
        Acaop.DocumentManager.DocumentLockModeChanged -= Dm_VetoCommand; // 關閉否決命令事件
    }

    /// <summary>
    /// 處理雙擊事件，判斷是否選中對象並設置相關標誌。
    /// </summary>
    private static void Acaop_BeginDoubleClick(object sender, BeginDoubleClickEventArgs e)
    {
        if (!_isOn)
            return;

        // 更新加载方式，取消：打開幾個文檔，雙擊操作就會觸發幾次，所以需要限定為當前文檔時才執行，否則會多次執行
        //int docCount = Acaop.DocumentManager.Count;
        //if (_isCurrent > docCount)
        //    _isCurrent = docCount;
#if Debug
        //Editor.WriteMessage($"\n命令執行次數：★ {_isCurrent} ★ 无活动命令或选择集：{Editor.IsQuiescent}"); // 无活动命令或选择集
#endif
        if (Editor.IsQuiescent) // 判斷是否處於選中ent的狀態
        {
            //_isCurrent = int.MaxValue; // 重置
            _isClick = true;
        }
        else
        {
            //if (_isCurrent == 1)
            DoubleClickIsQuiescent(); // 静止状态
            //else
            //_isCurrent--;
        }
    }

    /// <summary>
    /// 反应器->命令否决触发命令前(不可锁文档)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private static void Dm_VetoCommand(object sender, DocumentLockModeChangedEventArgs e)
    {
        if (!_isClick || !_isOn)
            return;

        if (string.IsNullOrEmpty(e.GlobalCommandName) || e.GlobalCommandName == "#")
            return;

        // Bug修復: 2025-01-22  屬性塊雙擊無選擇集對象，ppr.Status==PromptStatus.error。
        // 故雙擊切換至其它doc再切換回當前doc強讀錯誤的BUG
        //if (e.GlobalCommandName == "EATTEDIT")
        //{
        //    _isClick = false; // 還原至默認
        //    return;
        //}

        try
        {
            _isClick = false; // 還原至默認
            // 判斷是否選中對象
            var ppr = Editor.SelectImplied();
            if (ppr.Status != PromptStatus.OK)
                return;
            var ids = ppr.Value.GetObjectIds();
#if Debug
            foreach (var id in ids)
            {
                Editor.WriteMessage($"\n選中ObjectId：{id}");
                Editor.WriteMessage($"\n選中類別：{id.ObjectClass.DxfName}");
                Editor.WriteMessage($"\n----------------");
            }
#endif
            // 判斷單選對象還是多選對象
            if (ids.Length == 1)
            {
                // 判斷是否是要處理的類型
                string classType = ids[0].ObjectClass.DxfName;
                if (!_voteCommand.TryGetValue(classType, out string? value))
                    return;

                string cmd = value + " ";
                if (string.IsNullOrWhiteSpace(cmd))
                    return;

                e.Veto();
#if Debug
                Editor.WriteMessage($"\n快捷面板命令：{ids[0].ObjectClass.DxfName}");
                Editor.WriteMessage($"\nDm_VetoCommand {e.GlobalCommandName} 被否决了");
#endif
                Document.SendStringToExecute(cmd, true, false, false);
            }
            else
            {
                if (CmdClassMap.ContainsKey(e.GlobalCommandName.ToUpper())) // 判斷多選是非矩形視口
                {
                    var classType = CmdClassMap[e.GlobalCommandName.ToUpper()];
                    if (!_voteCommand.TryGetValue(classType, out string? value))
                        return;

                    var cmd = value + " ";
                    if (string.IsNullOrWhiteSpace(cmd))
                        return;

                    e.Veto();
#if Debug
                    Editor.WriteMessage($"\n快捷面板命令：{ids[0].ObjectClass.DxfName}");
                    Editor.WriteMessage($"\nDm_VetoCommand {e.GlobalCommandName} 被否决了");
#endif
                    Document.SendStringToExecute(cmd, true, false, false);
                }
                else
                {
                    e.Veto();
#if Debug
                    Editor.WriteMessage($"\nDm_VetoCommand {e.GlobalCommandName} 被否决了");
                    Editor.WriteMessage($"\n多選了對象");
#endif
                    string cmd = _multiSelectCmd + " ";
                    Document.SendStringToExecute(cmd, true, false, false);
                }
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"双击功能出现错误：{ex}");
        }
    }

    /// <summary>
    /// 处理双击事件时未選擇圖元時的静止状态。
    /// <para> 0x01 模型空间：则启动 ** 命令；</para>
    /// <para> 0x02 佈局空間(默認視口)：启动相应的命令。</para>
    /// <para> 0x03 佈局空間(活動視口)：启动相应的命令。</para>
    /// </summary>
    private static void DoubleClickIsQuiescent()
    {
        // Bug修復：2025-01-27  佈局切換至視口空間時雙擊，否決命令事件不觸發
        bool isMsClick = false; // 鼠標位置是否在視口內
        using Transaction tr = Database.TransactionManager.StartTransaction();
        if (Database.TileMode)
        {
            if (!string.IsNullOrWhiteSpace(_isQuiescentCommand[0]))
                Document.SendStringToExecute($"\u0003_{_isQuiescentCommand[0]} ", true, false, false);
        }
        else
        {
            var vpActive = (Viewport)tr.GetObject(Editor.ActiveViewportId, OpenMode.ForRead);
            if (vpActive.Number == 1)
            {
                // 判斷當前鼠標位置是否在視口內
                Point3d mpt = (Point3d)Acaop.GetSystemVariable("LASTPOINT");
#if Debug
                Editor.WriteMessage($"\n鼠標位置：{mpt}");
                Editor.WriteMessage($"\n空間名稱：{LayoutManager.Current.CurrentLayout}");
#endif
                // 通過過濾器選擇排除佈局的所有視口
                TypedValue[] values =
                [
                    new((int)DxfCode.LayoutName, LayoutManager.Current.CurrentLayout),
                    new((int)DxfCode.Start, nameof(Viewport)),
                ];
                SelectionFilter filter = new(values);
                var psr = Editor.SelectAll(filter);
                if (psr.Status == PromptStatus.OK && psr.Value.Count != 1)
                {
                    List<Polyline> plines = [];
                    foreach (var id in psr.Value.GetObjectIds())
                    {
                        var vp = (Viewport)tr.GetObject(id, OpenMode.ForRead);
                        if (vp.Number == 1)
                            continue;

                        // 添加視口邊界框
                        // 判斷是否是非矩形視口
                        Polyline addBoundary;
                        if (vp.NonRectClipOn)
                        {
                            var boundary = tr.GetObject(vp.NonRectClipEntityId, OpenMode.ForRead);
                            switch (boundary)
                            {
                                case Polyline pline:
                                    addBoundary = pline;
                                    break;
                                case Circle circle:
                                {
                                    using Polyline pl = circle.ToPolyline();
                                    addBoundary = (Polyline)pl.Clone();
                                    break;
                                }
                                default:
                                    continue;
                            }
                        }
                        else
                        {
                            var geoEx = vp.GeometricExtents;
                            using Polyline pline = geoEx.ToPolyline();
                            addBoundary = (Polyline)pline.Clone();
                        }
                        plines.Add(addBoundary);
                    }

                    // 查找鼠標位置是否在視口內
                    var plineInside = plines.FirstOrDefault(pl => mpt.IsPointInside(pl));
                    if (plineInside != null)
                        isMsClick = true;
                }
            }
            tr.Commit();

            // 發送相應的命令
            var cmd = (vpActive.Number == 1 ? _isQuiescentCommand[1] : _isQuiescentCommand[2]) + " ";
            if (!isMsClick && !string.IsNullOrWhiteSpace(cmd))
                Document.SendStringToExecute(cmd, true, false, false);
        }

        //_isCurrent = int.MaxValue; // 重置
    }
}
