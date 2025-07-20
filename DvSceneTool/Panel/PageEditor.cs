using DvSceneLib;
using DvSceneTool.Editors;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;

namespace DvSceneTool.Panels;

class PageEditor : Panel
{
    public PageEditor(Context ctx) : base(ctx) { }

    public static bool RenderCondition(ref Condition cond, int condId, int pageId)
    {
        bool changed = false;

        int nodeId = condId * 100 * pageId;
        ImNodes.BeginNode(nodeId);
        ImGui.PushID(nodeId);
        ImGui.PushItemWidth(100);

        ImNodes.BeginOutputAttribute(pageId * 1000 * condId + 1);
        ImGui.Text("Output");
        ImNodes.EndOutputAttribute();

        ImNodes.BeginInputAttribute(pageId * 1000 * condId + 2);
        ImGui.Text("Input");
        ImNodes.EndInputAttribute();

        ImGui.Text($"Type: {cond.ConditionType}");

        ImGui.PopItemWidth();
        ImGui.PopID();
        ImNodes.EndNode();

        return changed;
    }

    public static bool RenderPage(ref DvPage page)
    {
        bool changed = false;

        int nodeId = (int)page.Index+1;
        ImNodes.BeginNode(nodeId);
        ImGui.PushID(nodeId);
        ImGui.PushItemWidth(150);

        ImNodes.BeginNodeTitleBar();
        ImGui.Text(page.Name);
        ImNodes.EndNodeTitleBar();

        ImNodes.BeginOutputAttribute(nodeId * 100 + 1);
        ImGui.Text("Output");
        ImNodes.EndOutputAttribute();

        ImNodes.BeginInputAttribute(nodeId * 100 + 2);
        ImGui.Text("Input");
        ImNodes.EndInputAttribute();

        var start = page.Start;
        if(changed |= Basic.Editor("Frame Start", ref start)) page.Start = start;

        var end = page.End;
        if (changed |= Basic.Editor("Frame End", ref end)) page.End = end;

        ImGui.PopItemWidth();
        ImGui.PopID();
        ImNodes.EndNode();

        return changed;
    }

    public override void RenderPanel()
    {
        if (ctx.LoadedScene != null)
        {
            ImNodes.BeginNodeEditor();

            for(int i = 0; i < ctx.LoadedScene.Common.PageInfo.Entries.Count; i++)
            {
                DvPage page = ctx.LoadedScene.Common.PageInfo.Entries[i];
                if (RenderPage(ref page)) ctx.LoadedScene.Common.PageInfo.Entries[i] = page;
            }

            foreach (var i in ctx.LoadedScene.Common.PageInfo.Entries)
            {
                foreach (var x in i.Transitions)
                {
                    for(int y = 0; y < x.Conditions.Count; y++)
                    {
                        Condition cond = x.Conditions[y];
                        if (RenderCondition(ref cond, y+1, ((int)i.Index+1))) x.Conditions[y] = cond;
                    }
                }
            }

            foreach (var i in ctx.LoadedScene.Common.PageInfo.Entries)
            {
                foreach(var x in i.Transitions)
                {
                    int condId = 1;
                    foreach(var y in x.Conditions)
                    {
                        ImNodes.Link(((int)i.Index+1) * 10000 + condId+1, ((int)i.Index+1) * 100 + 1, ((int)i.Index+1) * 1000 * condId + 2);
                        if (x.DestPageIndex != -1)
                            ImNodes.Link(((int)i.Index + 1) * 10000 + condId+2, ((int)i.Index+1) * 1000 * condId + 1, (x.DestPageIndex+1) * 100 + 2);
                        condId++;
                    }
                }
            }

            ImNodes.EndNodeEditor();
        }
    }

    public override Properties GetProperties() => new Properties("Page Editor", new(8, 20), new(160, 500));
}
