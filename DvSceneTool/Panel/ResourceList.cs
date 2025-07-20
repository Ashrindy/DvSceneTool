using Hexa.NET.ImGui;

namespace DvSceneTool.Panels;

class ResourceList : Panel
{
    public ResourceList(Context ctx) : base(ctx) { }

    public void AddNewResource() => ctx.LoadedScene.Resource.Entries.Add(new() { Guid = Guid.NewGuid(), Name = "New Resource" });

    public override void RenderPanel()
    {
        if (ctx.LoadedScene != null)
        {
            if (ImGui.BeginPopupContextWindow())
            {
                if (ImGui.Selectable("Add")) AddNewResource();

                ImGui.EndPopup();
            }

            if(ImGui.Button("Add")) AddNewResource();

            for (int x = 0; x < ctx.LoadedScene.Resource.Entries.Count; x++)
            {
                var i = ctx.LoadedScene.Resource.Entries[x];
                if (ImGui.Selectable($"{i.Name} - {i.Type.ToString()}", ctx.SelectedResource == i))
                    ctx.SelectedResource = i;

                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.Selectable("Remove"))
                        ctx.LoadedScene.Resource.Entries.RemoveAt(x);

                    ImGui.EndPopup();
                }
            }
        }
    }

    public override Properties GetProperties() => new Properties("Resource List", new(8, 20), new(160, 500));
}
