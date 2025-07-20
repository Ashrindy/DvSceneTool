namespace DvSceneTool.Panels;

class ResourceInspector : Panel
{
    public ResourceInspector(Context ctx) : base(ctx) { }

    public override void RenderPanel()
    {
        if (ctx.LoadedScene != null && ctx.SelectedResource != null)
            Editors.DvScene.Editor(ref ctx.SelectedResource);
    }

    public override Properties GetProperties() => new Properties("Resource Inspector", new(8, 20), new(160, 500));
}
