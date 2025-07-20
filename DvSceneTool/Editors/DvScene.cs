using DvSceneLib;
using Hexa.NET.ImGui;

namespace DvSceneTool.Editors;

public static class DvScene
{
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
