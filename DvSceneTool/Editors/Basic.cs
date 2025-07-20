using DvSceneLib;
using Hexa.NET.ImGui;
using System.Numerics;

namespace DvSceneTool.Editors;

static class Basic
{
    public static bool Editor(string label, ref Guid value)
    {
        bool changed = false;
        string guid = value.ToString();
        if (changed |= Editor(label, ref guid, (ulong)37))
        {
            try { value = new Guid(guid); }
            catch { }
        }
        if (ImGui.BeginPopupContextItem())
        {
            if (changed |= ImGui.Selectable("Generate GUID"))
                value = Guid.NewGuid();
            ImGui.EndPopup();
        }
        return changed;
    }

    public static bool Editor(string label, ref RGB32F value)
    {
        bool changed = false;
        float[] color =
        {
            value.R, value.G, value.B
        };
        unsafe
        {
            fixed (float* x = &color[0])
            {
                if (changed |= ImGui.ColorEdit3(label, x))
                {
                    value.R = color[0];
                    value.G = color[1];
                    value.B = color[2];
                }
            }
        }
        return changed;
    }

    public static bool Editor(string label, ref RGB32 value)
    {
        bool changed = false;
        float[] color =
        {
            ((float)value.R) / 255, ((float)value.G) / 255, ((float)value.B) / 255
        };
        unsafe
        {
            fixed (float* x = &color[0])
            {
                if (changed |= ImGui.ColorEdit3(label, x))
                {
                    value.R = (uint)(color[0] * 255);
                    value.G = (uint)(color[1] * 255);
                    value.B = (uint)(color[2] * 255);
                }
            }
        }
        return changed;
    }

    public static bool Editor(string label, ref RGBA32 value)
    {
        bool changed = false;
        float[] color =
        {
            ((float)value.R) / 255, ((float)value.G) / 255, ((float)value.B) / 255, ((float)value.A) / 255
        };
        unsafe
        {
            fixed (float* x = &color[0])
            {
                if (changed |= ImGui.ColorEdit4(label, x))
                {
                    value.R = (uint)(color[0] * 255);
                    value.G = (uint)(color[1] * 255);
                    value.B = (uint)(color[2] * 255);
                    value.A = (uint)(color[3] * 255);
                }
            }
        }
        return changed;
    }

    public static bool Editor(string label, ref RGBA8 value)
    {
        bool changed = false;
        float[] color =
        {
            ((float)value.R) / 255, ((float)value.G) / 255, ((float)value.B) / 255, ((float)value.A) / 255
        };
        unsafe
        {
            fixed (float* x = &color[0])
            {
                if(changed |= ImGui.ColorEdit4(label, x))
                {
                    value.R = (byte)(color[0] * 255);
                    value.G = (byte)(color[1] * 255);
                    value.B = (byte)(color[2] * 255);
                    value.A = (byte)(color[3] * 255);
                }
            }
        }
        return changed;
    }

    public static bool Editor(string label, ref DvNodeTemplate.String value)
    {
        return Editor(label, ref value.Value, (ulong)value.Length);
    }

    public static bool Editor(string label, ref DvNodeTemplate.Enum value)
    {
        bool changed = false;
        ImGui.BeginGroup();
        if(ImGui.BeginCombo(label, value.Values.ToArray()[value.Value].Key))
        {
            foreach (var item in value.Values) {
                bool selected = (value.Value == item.Value);
                if (changed |= ImGui.Selectable(item.Key, selected))
                    value.Value = item.Value;

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        ImGui.EndGroup();
        return changed;
    }

    public static bool Editor(string label, ref int value, int bit)
    {
        bool changed = false;
        bool flag = (value & (1 << bit)) != 0;
        if(changed |= Editor(label, ref flag))
        {
            if (flag) value |= (1 << bit);
            else value &= ~(1 << bit);
        }
        return changed;
    }

    public static bool Editor(string label, ref DvNodeTemplate.Flag value)
    {
        bool changed = false;
        ImGui.BeginGroup();
        foreach(var i in value.Values) changed |= Editor(i, ref value.Value, value.Values.IndexOf(i));
        ImGui.EndGroup();
        return changed;
    }

    public unsafe static bool Editor(string label, ref byte value)
    {
        fixed (byte* x = &value) return ImGui.DragScalar(label, ImGuiDataType.S8, x);
    }

    public unsafe static bool Editor(string label, ref ushort value)
    {
        fixed (ushort* x = &value) return ImGui.DragScalar(label, ImGuiDataType.U16, x);
    }

    public unsafe static bool Editor(string label, ref short value)
    {
        fixed (short* x = &value) return ImGui.DragScalar(label, ImGuiDataType.S16, x);
    }

    public unsafe static bool Editor(string label, ref uint value)
    {
        fixed(uint* x = &value) return ImGui.DragScalar(label, ImGuiDataType.U32, x);
    }

    public static bool Editor(string label, ref int value)
    {
        return ImGui.DragInt(label, ref value);
    }

    public static bool Editor(string label, ref Vector4 value)
    {
        return ImGui.DragFloat4(label, ref value);
    }

    public static bool Editor(string label, ref Vector3 value)
    {
        return ImGui.DragFloat3(label, ref value);
    }

    public static bool Editor(string label, ref Vector2 value)
    {
        return ImGui.DragFloat2(label, ref value);
    }

    public static bool Editor(string label, ref int? value)
    {
        bool changed = false;
        if (value.HasValue)
        {
            int x = value.Value;
            changed |= ImGui.DragInt(label, ref x);
            value = x;
        }
        return changed;
    }

    public static bool Editor(string label, ref float? value)
    {
        bool changed = false;
        if (value.HasValue)
        {
            float x = value.Value;
            changed |= ImGui.DragFloat(label, ref x);
            value = x;
        }
        return changed;
    }

    public static bool Editor(string label, ref float value)
    {
        return ImGui.DragFloat(label, ref value);
    }

    public static bool Editor(string label, ref bool value)
    {
        return ImGui.Checkbox(label, ref value);
    }

    public unsafe static bool Editor(string label, ref Matrix4x4 value)
    {
        bool changed = false;
        ImGui.BeginGroup();
        fixed (float* x = &value.M11) changed |= ImGui.DragFloat4(label + " 0", x);
        fixed (float* x = &value.M21) changed |= ImGui.DragFloat4(label + " 1", x);
        fixed (float* x = &value.M31) changed |= ImGui.DragFloat4(label + " 2", x);
        fixed (float* x = &value.M41) changed |= ImGui.DragFloat4(label + " 3", x);
        ImGui.EndGroup();
        return changed;
    }

    public static bool Editor(string label, ref string value, ulong valueLength)
    {
        return ImGui.InputText(label, ref value, (nuint)valueLength);
    }
}
