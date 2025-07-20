namespace DvSceneTool.Panels;

class DvSceneSettings : Panel
{
    public DvSceneSettings(Context ctx) : base(ctx) { }

    public override void RenderPanel()
    {
        if (ctx.LoadedScene != null && ctx.LoadedScene.Common != null)
            Editors.DvScene.Editor(ref ctx.LoadedScene.Common);
    }

    public override Properties GetProperties() => new Properties("DvScene Settings", new(8, 20), new(160, 500));
}
