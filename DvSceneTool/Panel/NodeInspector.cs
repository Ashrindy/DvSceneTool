using DvSceneLib;
using DvSceneTool.Editors;
using System.Numerics;
using Hexa.NET.ImGui;

namespace DvSceneTool.Panels;

class NodeInspector : Panel
{
    public NodeInspector(Context ctx) : base(ctx) { }

    bool Editor(string name, ref DvNodeTemplate.Field field)
    {
        bool changed = false;
        if (field.Descriptions.ContainsKey("visibleEditor") && field.Descriptions["visibleEditor"] == "false") return false;

        switch (field.DataType)
        {
            case DvNodeTemplate.DataType.UByte:
                {
                    byte ubyteValue = (byte)field.Value;
                    unsafe { if(changed |= ImGui.DragScalar(name, ImGuiDataType.U8, &ubyteValue)) field.Value = ubyteValue; }
                    break;
                }

            case DvNodeTemplate.DataType.Byte:
                {
                    byte rValue = (byte)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.UShort:
                {
                    ushort rValue = (ushort)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Short:
                {
                    short rValue = (short)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.UInt:
                {
                    uint rValue = (uint)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Int:
                {
                    int rValue = (int)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Float:
                {
                    float rValue = (float)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Vector2:
                {
                    Vector2 rValue = (Vector2)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Vector3:
                {
                    Vector3 rValue = (Vector3)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Vector4:
                {
                    Vector4 rValue = (Vector4)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Matrix4x4:
                {
                    Matrix4x4 rValue = (Matrix4x4)field.Value;
                    Vector3 position;
                    Quaternion rotationQ;
                    Vector3 scale;
                    Matrix4x4.Decompose(rValue, out scale, out rotationQ, out position);
                    Vector3 rotationE = Utils.ToEulerAngles(rotationQ);

                    ImGui.BeginGroup();
                    changed |= Basic.Editor($"{name} Position", ref position);
                    changed |= Basic.Editor($"{name} Rotation", ref rotationE);
                    changed |= Basic.Editor($"{name} Scale", ref scale);
                    ImGui.EndGroup();

                    if(changed)
                    {
                        rotationQ = Utils.ToQuaternion(rotationE);
                        field.Value = Utils.ComposeMatrix(position, scale, rotationQ);
                    }
                    break;
                }

            case DvNodeTemplate.DataType.Curve:
                {
                    float[] rValue = (float[])field.Value;
                    if(changed |= Editors.DvScene.EditorCurve(name, ref rValue, ctx.CurveInfo)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.String:
                {
                    DvNodeTemplate.String rValue = (DvNodeTemplate.String)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Enum:
                {
                    DvNodeTemplate.Enum rValue = (DvNodeTemplate.Enum)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Struct:
                {
                    if (ImGui.TreeNode(name))
                    {
                        Dictionary<string, DvNodeTemplate.Field> fields = (Dictionary<string, DvNodeTemplate.Field>)field.Value;
                        if (changed |= Editor(ref fields)) field.Value = fields;
                        ImGui.TreePop();
                    }
                    break;
                }

            case DvNodeTemplate.DataType.Guid:
                {
                    Guid rValue = (Guid)field.Value;

                    var nodes = Context.Instance.GetNodes(Context.Instance.LoadedScene);
                    DvNodeTemplate selectedNode = null;
                    if(rValue != Guid.Empty)
                        foreach(var i in nodes) {
                            if(i.Guid == rValue) selectedNode = i; break;
                        }

                    if(ImGui.BeginCombo(name, selectedNode == null ? "None" : selectedNode.ToString()))
                    {
                        if (changed |= ImGui.Selectable("None", selectedNode == null)) field.Value = Guid.Empty;
                        foreach(var x in nodes)
                        {
                            bool isSelected = x == selectedNode;
                            if (changed |= ImGui.Selectable(x.ToString(), isSelected))
                                field.Value = x.Guid;
                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }

                    break;
                }

            case DvNodeTemplate.DataType.Array:
                {
                    List<DvNodeTemplate.Field> rValue;
                    bool resizable = false;
                    if (resizable = field.Value.GetType() == typeof(List<DvNodeTemplate.Field>))
                        rValue = (List<DvNodeTemplate.Field>)field.Value;
                    else
                        rValue = [.. (DvNodeTemplate.Field[])field.Value];

                    bool isOpen = ImGui.TreeNode(name);

                    if (resizable && ImGui.BeginPopupContextItem())
                    {
                        if (changed |= ImGui.Selectable("Add"))
                            ((List<DvNodeTemplate.Field>)field.Value).Add(new() { DataType = field.SubDataType, Value = DvNodeTemplate.GetDefaultByType(field.SubDataType), Descriptions = new() });
                        ImGui.EndPopup();
                    }

                    if (isOpen)
                    {
                        for(int i = 0; i < rValue.Count; i++)
                        {
                            var x = rValue[i];

                            ImGui.PushID(i);
                            if (changed |= Editor("", ref x))
                            {
                                if (resizable)
                                    ((List<DvNodeTemplate.Field>)field.Value)[i] = x;
                                else
                                    ((DvNodeTemplate.Field[])field.Value)[i] = x;
                            }

                            if (resizable)
                            {
                                if (resizable && ImGui.BeginPopupContextItem())
                                {
                                    if (changed |= ImGui.Selectable("Remove"))
                                        ((List<DvNodeTemplate.Field>)field.Value).RemoveAt(i);
                                    ImGui.EndPopup();
                                }
                            }

                            ImGui.PopID();
                        }
                        ImGui.TreePop();
                    }
                    break;
                }

            case DvNodeTemplate.DataType.Boolean:
                {
                    bool rValue = (bool)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.RGBA:
                {
                    RGBA8 rValue = (RGBA8)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.RGB32:
                {
                    RGB32 rValue = (RGB32)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.RGB32F:
                {
                    RGB32F rValue = (RGB32F)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.RGBA32:
                {
                    RGBA32 rValue = (RGBA32)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }

            case DvNodeTemplate.DataType.Flag:
                {
                    DvNodeTemplate.Flag rValue = (DvNodeTemplate.Flag)field.Value;
                    if (changed |= Basic.Editor(name, ref rValue)) field.Value = rValue;
                    break;
                }
        }

        string[] blacklistedDescs =
        {
            "visibleEditor"
        };

        bool show = false;
        foreach (var i in field.Descriptions)
        {
            if (blacklistedDescs.Contains(i.Key)) continue;
            if (i.Key == "" || i.Value == "") continue;

            show |= true;
        }

        if (show && ImGui.BeginItemTooltip())
        {
            foreach (var i in field.Descriptions)
            {
                if (blacklistedDescs.Contains(i.Key)) continue;
                if (i.Key == "" || i.Value == "") continue;

                ImGui.Text($"{i.Key}: {i.Value}");
            }
            ImGui.EndTooltip();
        }

        return changed;
    }

    bool Editor(ref Dictionary<string, DvNodeTemplate.Field> fields)
    {
        bool changed = false;
        for (int i = 0; i < fields.Count; i++)
        {
            var key = fields.Keys.ToList()[i];
            var value = fields.Values.ToList()[i];

            bool ticks = false;
            var keyLower = key.ToLower();
            if (keyLower.Contains("start") || keyLower.Contains("end"))
            {
                if (ticks = value.DataType == DvNodeTemplate.DataType.UInt)
                {
                    value.Value = ((float)(uint)value.Value) / 100;
                    value.DataType = DvNodeTemplate.DataType.Float;
                }
            }

            if(changed |= Editor(key, ref value))
            {
                if (ticks)
                {
                    value.Value = (uint)((float)value.Value * 100);
                    value.DataType = DvNodeTemplate.DataType.UInt;
                }
                fields[key] = value;
            }
        }
        return changed;
    }

    bool Editor(ref DvNodeTemplate node)
    {
        bool changed = false;

        var guid = node.Guid;
        if (changed |= Basic.Editor("GUID", ref guid)) node.Guid = guid;
        string name = node.NodeName;
        if (changed |= Basic.Editor("Name", ref name, 64)) node.NodeName = name;
        int priority = node.Priority;
        if (changed |= Basic.Editor("Priority", ref priority)) node.Priority = priority;
        int flags = node.NodeFlags;
        if (changed |= Basic.Editor("Flags", ref flags)) node.NodeFlags = flags;

        ImGui.SeparatorText("Node Properties");
        ImGui.PushID($"{node.NodeName}NodeProperties");
        changed |= Editor(ref node.Fields);
        ImGui.PopID();

        return changed;
    }

    bool Editor(ref DvElementTemplate node)
    {
        bool changed = false;
        DvNodeTemplate nodeTemp = node;
        if (changed |= Editor(ref nodeTemp)) node = (DvElementTemplate)nodeTemp;
        if (node.ElementFields.Count == 0) return changed;

        ImGui.SeparatorText("Element Properties");
        ImGui.PushID($"{node.NodeName}ElementProperties");
        Editor(ref node.ElementFields);
        ImGui.PopID();
        return changed;
    }

    public override void RenderPanel()
    {
        if(ctx.LoadedScene != null && ctx.Selected != null)
        {
            if(ctx.Selected.GetType() == typeof(DvNodeTemplate))
            {
                var node = ctx.Selected as DvNodeTemplate;
                Editor(ref node);
            }
            else if(ctx.Selected.GetType() == typeof(DvElementTemplate))
            {
                var elem = ctx.Selected as DvElementTemplate;
                Editor(ref elem);
            }
        }
    }

    public override Properties GetProperties() => new Properties("Node Inspector", new(8, 20), new(160, 500));
}
