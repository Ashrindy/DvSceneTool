using DvSceneLib;
using DvSceneTool.Editors;
using System.Numerics;
using Hexa.NET.ImPlot;
using Hexa.NET.ImGui;
using MathNet.Numerics;

namespace DvSceneTool.Panels;

class NodeInspector : Panel
{
    public NodeInspector(Context ctx) : base(ctx) { }

    bool EditorCurve(string name, ref float[] rValue)
    {
        bool changed = false;
        if (ImPlot.BeginPlot(name, new Vector2(512, 256), ImPlotFlags.CanvasOnly | ImPlotFlags.NoInputs))
        {
            if (ImGui.BeginPopupContextItem("Controls"))
            {
                ImGui.SeparatorText("Curve Editing Settings");
                Basic.Editor("Falloff", ref ctx.CurveInfo.Falloff);
                ImGui.SeparatorText("Custom Curve");
                Basic.Editor("Decreasing", ref ctx.CurveInfo.Decreasing);
                ImGui.SameLine();
                int type = (int)ctx.CurveInfo.Type;
                if (ImGui.Combo("Curve Type", ref type, Enum.GetNames(typeof(Context.CurveSettings.CurveType)), 7)) 
                    ctx.CurveInfo.Type = (Context.CurveSettings.CurveType)type; 
                if (ImGui.Selectable("Generate Curve"))
                    Utilities.GenerateCurve(rValue, type, ctx.CurveInfo.Decreasing);
                ImGui.EndPopup();
            }

            Vector4 color = new Vector4(0.31f, 0.69f, 0.776f, 1.0f);
            ImPlot.SetupAxis(ImAxis.X1, "Time", ImPlotAxisFlags.NoDecorations | ImPlotAxisFlags.Lock | ImPlotAxisFlags.LockMin | ImPlotAxisFlags.LockMax);
            ImPlot.SetupAxis(ImAxis.Y1, "Value", ImPlotAxisFlags.AutoFit | ImPlotAxisFlags.NoDecorations);
            ImPlot.SetupAxisLimits(ImAxis.X1, 0, rValue.Length, ImPlotCond.Always);
            ImPlot.SetupAxisLimits(ImAxis.Y1, 0, 1, ImPlotCond.Always);
            float[] time = Enumerable.Range(0, rValue.Length).Select(i => (float)i).ToArray();
            ImPlot.SetNextFillStyle(color, 0.3f);
            unsafe
            {
                fixed (float* timeX = &time[0])
                fixed (float* valueX = &rValue[0])
                    ImPlot.PlotLine("X", timeX, valueX, rValue.Length, ImPlotLineFlags.Shaded);
            }

            for (int i = 0; i < rValue.Length; i++)
            {
                double frame = i;
                double value = rValue[i];
                bool clicked = false;
                bool hovered = false;
                bool held = false;

                if (changed |= ImPlot.DragPoint(i, ref frame, ref value, color, 4, ref clicked, ref hovered, ref held))
                {
                    value = Math.Clamp(value, 0, 1);

                    double sigma = Context.Instance.CurveInfo.Falloff;
                    double delta = value - rValue[i];

                    for (int x = 0; x < rValue.Length; x++)
                    {
                        double dist = x - i;
                        double weight = Math.Exp(-(dist * dist) / (sigma * sigma));
                        rValue[x] += (float)(weight * delta);
                        rValue[x] = Math.Clamp(rValue[x], 0, 1);
                    }
                }

                if (hovered || held)
                    ImGui.SetTooltip($"Frame: {i}\nValue: {value}");
            }

            ImPlot.EndPlot();
        }
        ImGui.SameLine();
        ImGui.Text(name);
        return changed;
    }

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
                    if(changed |= EditorCurve(name, ref rValue)) field.Value = rValue;
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
            if(changed |= Editor(key, ref value)) fields[key] = value;
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
