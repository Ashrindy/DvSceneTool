using DvSceneLib;
using DvSceneLib.IO.Template;
using Hexa.NET.ImGui;
using System.Runtime.InteropServices;

namespace DvSceneTool.Panels;

class NodeHierarchy : Panel
{
    public NodeHierarchy(Context ctx) : base(ctx) { }

    bool RenderCreateMenuNode(DiEventDataBase.Node node, ref DvNodeTemplate nodet)
    {
        bool changed = false;
        if (node.Descriptions.ContainsKey("isNodeElement") && node.Descriptions["isNodeElement"] == "true") return changed;

        if (changed |= ImGui.MenuItem(node.Name, false, !((node.Descriptions.ContainsKey("Unknown") && node.Descriptions["Unknown"] == "true"))))
        {
            if (ctx.DiEvtDB.Elements.Contains(node))
                nodet.AddChild(new DvElementTemplate(ctx.DiEvtDB.Nodes.Find(x => x.Descriptions.ContainsKey("isNodeElement") && x.Descriptions["isNodeElement"] == "true"), node));
            else
                nodet.AddChild(new DvNodeTemplate(node));
            ImGui.CloseCurrentPopup();
        }

        string[] blacklistedDescs =
        {
            "isNodeElement",
            "Unknown",
            "Category"
        };

        bool show = false;
        foreach (var i in node.Descriptions)
        {
            if (blacklistedDescs.Contains(i.Key)) continue;
            if (i.Key == "" || i.Value == "") continue;

            show |= true;
        }

        if (show && ImGui.BeginItemTooltip())
        {
            foreach (var i in node.Descriptions)
            {
                if (blacklistedDescs.Contains(i.Key)) continue;
                if (i.Key == "" || i.Value == "") continue;

                ImGui.Text($"{i.Key}: {i.Value}");
            }
            ImGui.EndTooltip();
        }

        return changed;
    }

    bool RenderCreateMenu(ref DvNodeTemplate node) 
    {
        bool changed = false;

        foreach (var i in ctx.CategorizedNodesElems)
        {
            if (i.Key == "")
            {
                foreach(var x in i.Value)
                    changed |= RenderCreateMenuNode(x, ref node);
            }
            else
            {
                if (ImGui.BeginMenu(i.Key))
                {
                    foreach (var x in i.Value)
                        changed |= RenderCreateMenuNode(x, ref node);
                    ImGui.EndMenu();
                }
            }
        }

        return changed;
    }

    bool RenderNode(ref DvNodeTemplate node)
    {
        bool changed = false;

        ImGui.PushID(node.GetHashCode());

        string nodeName = $"{node.NodeName} - ";
        if (node.GetType() == typeof(DvElementTemplate))
        {
            string elemName = ((DvElementTemplate)node).ElementName;
            nodeName += ctx.DiEvtDB.Elements.Find(x => x.FullName == elemName).Name;
        }
        else
        {
            string category = node.Category;
            nodeName += ctx.DiEvtDB.Nodes.Find(x => x.FullName == category).Name;
        }

        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (ctx.Selected == node)
            flags |= ImGuiTreeNodeFlags.Selected;

        bool isOpen = false;
        if (node.ChildNodes.Count == 0)
            ImGui.TreeNodeEx(nodeName, flags | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
        else
            isOpen = ImGui.TreeNodeEx(nodeName, flags);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            ctx.Selected = node;
        }

        if (node != ctx.LoadedScene.Common.Node && ImGui.BeginDragDropSource())
        {
            var nodes = ctx.GetNodes(ctx.LoadedScene);
            string name = node.NodeName;
            Guid guid = node.Guid;
            var payload = nodes.FindIndex(x => x.NodeName == name && x.Guid == guid);
            unsafe {
                ImGui.SetDragDropPayload("node-move", &payload, 4);
            }
            ImGui.Text(nodeName);
            ImGui.EndDragDropSource();
        }

        if (ImGui.BeginDragDropTarget())
        {
            unsafe
            {
                ImGuiPayload* payload = ImGui.AcceptDragDropPayload("node-move");
                if (payload != null && (nint)payload->Data != IntPtr.Zero)
                {
                    var dropped = Marshal.ReadInt32((nint)payload->Data);
                    DvNodeTemplate droppedNode = ctx.GetNodes(ctx.LoadedScene)[dropped];
                    if (!droppedNode.ChildNodes.Contains(node))
                    {
                        var nodeParent = droppedNode.GetParent();
                        node.AddChild(droppedNode);
                        nodeParent.ChildNodes.Remove(droppedNode);
                    }
                }
            }
            ImGui.EndDragDropTarget();
        }

        if (ImGui.BeginPopupContextItem())
        {
            if (ImGui.BeginMenu("Add"))
            {
                changed |= RenderCreateMenu(ref node);
                ImGui.EndMenu();
            }

            if (node != ctx.LoadedScene.Common.Node)
                if (ImGui.Selectable("Remove"))
                    node.GetParent().ChildNodes.Remove(node);

            ImGui.EndPopup();
        }

        if (isOpen)
        {
            for(int i = 0; i < node.ChildNodes.Count; i++)
            {
                var x = (DvNodeTemplate)node.ChildNodes[i];
                if (changed |= RenderNode(ref x)) node.ChildNodes[i] = x;
            }
            ImGui.TreePop();
        }
        
        ImGui.PopID();

        return changed;
    }

    public override void RenderPanel()
    {
        if(ctx.LoadedScene != null && ctx.LoadedScene.Common.Node != null && ctx.LoadedScene.Common.Node.GetType() == typeof(DvNodeTemplate))
        {
            DvNodeTemplate node = (DvNodeTemplate)ctx.LoadedScene.Common.Node;
            if (RenderNode(ref node)) ctx.LoadedScene.Common.Node = node;
        }
    }

    public override Properties GetProperties() => new Properties("Node Hierarchy", new(8, 20), new(160, 500));
}
