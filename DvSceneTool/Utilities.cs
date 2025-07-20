using DvSceneLib;
using Hexa.NET.ImGui;
using MathNet.Numerics;
using System.Text.Json;

namespace DvSceneTool;

public static class Utilities
{
    public static T Clone<T>(this T source)
    {
        var serialized = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<T>(serialized);
    }

    public static void GetParent(this DvNode node, DvNode target, ref DvNode result)
    {
        if (node.ChildNodes.Contains(target))
        {
            result = node;
            return;
        }

        foreach (var i in node.ChildNodes)
            i.GetParent(target, ref result);
    }

    public static DvNode GetParent(this DvNode node)
    {
        DvNode parent = new();
        Context.Instance.LoadedScene.Common.Node.GetParent(node, ref parent);
        return parent;
    }

    public static void GenerateCurve(float[] curve, int type, bool decreasing)
    {
        for (int x = 0; x < curve.Length; x++)
        {
            switch (type)
            {
                case 0:
                    curve[x] = ((float)x) / (float)(curve.Length - 1);
                    if (decreasing)
                        curve[x] = 1 - curve[x];
                    break;
                case 1:
                    {
                        float sqr = ((float)x) / ((float)(curve.Length - 1));
                        if (decreasing)
                            curve[x] = (1 - sqr) * (1 - sqr);
                        else
                            curve[x] = sqr * sqr;
                        break;
                    }
                case 2:
                    {
                        float sqr = ((float)x) / ((float)(curve.Length - 1));
                        if (decreasing)
                            curve[x] = 1 - sqr * sqr;
                        else
                            curve[x] = 1 - (1 - sqr) * (1 - sqr);
                        break;
                    }
                case 3:
                    {
                        float t = ((float)x) / ((float)(curve.Length - 1));
                        curve[x] = t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
                        if (decreasing)
                            curve[x] = 1 - curve[x];
                        break;
                    }
                case 4:
                    {
                        curve[x] = 0.5f * (1 - MathF.Cos(((float)x) / ((float)(curve.Length - 1)) * MathF.PI));
                        if (decreasing)
                            curve[x] = 1 - curve[x];
                        break;
                    }
                case 5:
                    {
                        float t = ((float)x) / ((float)(curve.Length - 1));
                        if (!decreasing)
                            t = 1 - t;
                        curve[x] = 1 - (float)SpecialFunctions.Log1p(t * 9) / (float)SpecialFunctions.Log1p(9);
                        break;
                    }
                case 6:
                    {
                        float t = ((float)x) / ((float)(curve.Length - 1));
                        if (decreasing)
                            t = 1 - t;
                        curve[x] = (float)SpecialFunctions.Log1p(t * 9) / (float)SpecialFunctions.Log1p(9);
                        break;
                    }
            }
        }
    }

    public static bool IsControlDown() => ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl);
}
