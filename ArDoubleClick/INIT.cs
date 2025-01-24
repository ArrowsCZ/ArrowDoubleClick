namespace ArrowTools;

public class Init : IExtensionApplication
{
    // todo:2025-01-25  自動註冊
    public void Initialize()
    {
        const string notice =
            "版本支持:Auto CAD 2015-2025"
            + "\n"
            + "\n"
            + "使用注意：2020-2025版本初次开启文档不加载此功能，需关闭初开文档后双击设置生效！";
        Acaop.ShowAlertDialog(notice);

        Acaop.DocumentManager.DocumentCreated += DmClickStart; // 開啟文檔時，加載雙擊事件
        Acaop.DocumentManager.DocumentDestroyed += DmClickEnd; // 關閉文檔時，移除雙擊事件
    }

    private static void DmClickStart(object sender, DocumentCollectionEventArgs e)
    {
#if Debug
        Acaop.ShowAlertDialog("開啟了當前！");
#endif
        ArDoubleClick.ArDoubleClickStart();
    }

    private static void DmClickEnd(object sender, DocumentDestroyedEventArgs e)
    {
#if Debug
        Acaop.ShowAlertDialog("關閉了當前！");
#endif
        ArDoubleClick.ArDoubleClickEnd();
    }

    public void Terminate() { }
}
