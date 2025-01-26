namespace ArrowTools;

public class Init : IExtensionApplication
{
    public void Initialize()
    {
        // const string notice =
        //     "版本支持:Auto CAD 2015-2025"
        //     + "\n"
        //     + "\n使用注意：初次开启文档不加载此功能，"
        //     + "\n需切換至其它文档后双击设置生效！";
        // Acaop.ShowAlertDialog(notice);

        // Bug修復:2025-01-22  開啟、關閉文檔事件改為激活文檔事件，這樣更加方便用戶操作
        // Acaop.DocumentManager.DocumentCreated += DmClickStart; // 開啟文檔時，加載雙擊事件
        // Acaop.DocumentManager.DocumentDestroyed += DmClickEnd; // 關閉文檔時，移除雙擊事件
        Acaop.DocumentManager.DocumentActivated += DmClickActivated; // 切換文檔時，加載雙擊事件

        AutoReg.RegApp(); // 自動註冊

        // Bug修復：cad各版本中初次打開CAD無法正常運行，需要關閉dwg重新打開命令才會生效。
        Acaop.Idle += OnIdle;
    }

    /// <summary>
    /// 處理空閒事件，檢查活動文檔並初始化雙擊功能。
    /// </summary>
    private static void OnIdle(object? sender, EventArgs e)
    {
        var doc = Acaop.DocumentManager.MdiActiveDocument;
        if (doc == null)
            return;
        Acaop.Idle -= OnIdle;

        ArrowDoubleClickStart();
    }

    /// <summary>
    /// 初始化雙擊功能，通過創建一個新文檔並立即關閉它。
    /// </summary>
    private static void ArrowDoubleClickStart()
    {
        // 新建模板文件，并打开
        const string strTemplatePath = "acadiso.dwt";
        Document acDoc = Acaop.DocumentManager.Add(strTemplatePath);
        Acaop.DocumentManager.MdiActiveDocument = acDoc;
        acDoc.CloseAndDiscard(); // 調用函數關掉當前文檔
    }

    /// <summary>
    /// 當前文檔被激活時觸發，結束並重新開始雙擊功能。
    /// </summary>
    private static void DmClickActivated(object sender, DocumentCollectionEventArgs e)
    {
#if Debug
        Acaop.ShowAlertDialog("激活了當前！");
#endif
        ArDoubleClick.ArDoubleClickEnd();
        ArDoubleClick.ArDoubleClickStart();
    }

    //     private static void DmClickStart(object sender, DocumentCollectionEventArgs e)
    //     {
    // #if Debug
    //         Acaop.ShowAlertDialog("開啟了當前！");
    // #endif
    //         ArDoubleClick.ArDoubleClickStart();
    //     }
    //
    //     private static void DmClickEnd(object sender, DocumentDestroyedEventArgs e)
    //     {
    // #if Debug
    //         Acaop.ShowAlertDialog("關閉了當前！");
    // #endif
    //         ArDoubleClick.ArDoubleClickEnd();
    //     }

    public void Terminate() { }
}
