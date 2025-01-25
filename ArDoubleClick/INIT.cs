namespace ArrowTools;

public class Init : IExtensionApplication
{
    // todo:2025-01-25  自動註冊
    public void Initialize()
    {
        const string notice =
            "版本支持:Auto CAD 2015-2025"
            + "\n"
            + "\n使用注意：2020-2025版本初次开启文档不加载此功能，"
            + "\n需切換至其它文档后双击设置生效！";
        Acaop.ShowAlertDialog(notice);

        // Bug修復:2025-01-22  開啟、關閉文檔事件改為激活文檔事件，這樣更加方便用戶操作
        // Acaop.DocumentManager.DocumentCreated += DmClickStart; // 開啟文檔時，加載雙擊事件
        // Acaop.DocumentManager.DocumentDestroyed += DmClickEnd; // 關閉文檔時，移除雙擊事件
        Acaop.DocumentManager.DocumentActivated += DmClickActivated; // 切換文檔時，加載雙擊事件

        // 幫我寫一個切換至其它文檔的功能
        // 此處錯誤，使用LISP加載dll:INIT初始化時，無法往DocumentManager中添加文檔
        // var currentDocument = Acaop.DocumentManager.MdiActiveDocument;
    }

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
