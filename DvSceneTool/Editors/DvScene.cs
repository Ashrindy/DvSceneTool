using DvSceneLib;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using System.Numerics;
using static DvSceneTool.Context;

namespace DvSceneTool.Editors;

public static class DvScene
{
    public static bool EditorCurve(string name, ref float[] rValue, CurveSettings curveInfo, float width = 512, float higth = 256)
    {
        bool changed = false;
        if (ImPlot.BeginPlot(name, new Vector2(width, higth), ImPlotFlags.CanvasOnly | ImPlotFlags.NoInputs))
        {
            if (ImGui.BeginPopupContextItem("Controls"))
            {
                ImGui.SeparatorText("Curve Editing Settings");
                Basic.Editor("Falloff", ref curveInfo.Falloff);
                ImGui.SeparatorText("Custom Curve");
                Basic.Editor("Decreasing", ref curveInfo.Decreasing);
                ImGui.SameLine();
                int type = (int)curveInfo.Type;
                if (ImGui.Combo("Curve Type", ref type, Enum.GetNames(typeof(CurveSettings.CurveType)), 7))
                    curveInfo.Type = (CurveSettings.CurveType)type;
                if (ImGui.Selectable("Generate Curve"))
                    Utilities.GenerateCurve(rValue, type, curveInfo.Decreasing);
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

                    double sigma = curveInfo.Falloff;
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

    public static bool Editor(string label, ref DvCutInfo cutInfo)
    {
        bool changed = false;

        bool isOpen = ImGui.TreeNode(label);

        if (ImGui.BeginPopupContextItem())
        {
            if (changed |= ImGui.Selectable("Add"))
                cutInfo.FrameCut.Add(0);
            ImGui.EndPopup();
        }

        if (isOpen)
        {

            for (int i = 0; i < cutInfo.FrameCut.Count; i++)
            {
                ImGui.PushID(i);

                var x = cutInfo.FrameCut[i];
                if (changed |= Basic.Editor("", ref x)) cutInfo.FrameCut[i] = x;

                if (ImGui.BeginPopupContextItem())
                {
                    if (changed |= ImGui.Selectable("Remove"))
                        cutInfo.FrameCut.RemoveAt(i);
                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }

            ImGui.TreePop();
        }

        return changed;
    }

    public static bool Editor(string label, ref DvResourceCutInfo cutInfo)
    {
        bool changed = false;

        bool isOpen = ImGui.TreeNode(label);

        if (ImGui.BeginPopupContextItem())
        {
            if (changed |= ImGui.Selectable("Add"))
                cutInfo.Frames.Add(0);
            ImGui.EndPopup();
        }

        if (isOpen)
        {

            for (int i = 0; i < cutInfo.Frames.Count; i++)
            {
                ImGui.PushID(i);

                var x = cutInfo.Frames[i];
                if (changed |= Basic.Editor("", ref x)) cutInfo.Frames[i] = x;

                if (ImGui.BeginPopupContextItem())
                {
                    if (changed |= ImGui.Selectable("Remove"))
                        cutInfo.Frames.RemoveAt(i);
                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }

            ImGui.TreePop();
        }

        return changed;
    }

    public static bool Editor(ref DvCommon value)
    {
        bool changed = false;

        float start = value.Start;
        if (changed |= Basic.Editor("Frame Start", ref start)) value.Start = start;

        float end = value.End;
        if (changed |= Basic.Editor("Frame End", ref end)) value.End = end;

        DvCutInfo cutInfo = value.CutInfo;
        if (changed |= Editor("Cuts", ref cutInfo)) value.CutInfo = cutInfo;

        DvResourceCutInfo resourceCutInfo = value.ResourceCutInfo;
        if (changed |= Editor("Resource Cuts", ref resourceCutInfo)) value.ResourceCutInfo = resourceCutInfo;

        return changed;
    }

    public static bool Editor(ref ResourceEntry value)
    {
        bool changed = false;
        string name = value.Name;
        var guid = value.Guid;
        if (changed |= Basic.Editor("GUID", ref guid)) value.Guid = guid;
        if (changed |= Basic.Editor("Name", ref name, 0x300)) value.Name = name;
        int type = 0;
        switch (value.Type)
        {
            case ResourceEntry.DvResourceType.Character:
                type = 0;
                break;

            case ResourceEntry.DvResourceType.CameraMotion:
                type = 1;
                break;

            case ResourceEntry.DvResourceType.ModelMotion:
                type = 2;
                break;

            case ResourceEntry.DvResourceType.CharacterMotion:
                type = 3;
                break;

            case ResourceEntry.DvResourceType.Model:
                type = 4;
                break;
        }
        if (changed |= ImGui.Combo("Type", ref type, Enum.GetNames(typeof(ResourceEntry.DvResourceType)), 5))
        {
            switch (type)
            {
                case 0:
                    value.Type = ResourceEntry.DvResourceType.Character;
                    break;

                case 1:
                    value.Type = ResourceEntry.DvResourceType.CameraMotion;
                    break;

                case 2:
                    value.Type = ResourceEntry.DvResourceType.ModelMotion;
                    break;

                case 3:
                    value.Type = ResourceEntry.DvResourceType.CharacterMotion;
                    break;

                case 4:
                    value.Type = ResourceEntry.DvResourceType.Model;
                    break;
            }
        }

        int field14 = value.Field14;
        if (changed |= Basic.Editor("Unk0", ref field14)) value.Field14 = field14;

        int field18 = value.Field18;
        if (changed |= Basic.Editor("Unk1", ref field18)) value.Field18 = field18;

        int unk0 = value.Unk0;
        if (changed |= Basic.Editor("Unk2", ref unk0)) value.Unk0 = unk0;

        int unk1 = value.Unk1;
        if (changed |= Basic.Editor("Unk3", ref unk1)) value.Unk1 = unk1;

        return changed;
    }
}
