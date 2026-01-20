using DvSceneLib;
using DvSceneTool.Editors;
using DvSceneTool.Widgets;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets;
using Hexa.NET.ImPlot;
using System.Numerics;
using System.Xml.Linq;
using static DvSceneTool.Context;

namespace DvSceneTool.Panels;

class Timeline : Panel 
{

    public Timeline(Context ctx) : base(ctx) { }

    public void RenderClipCurve()
    {
        bool isElement = ctx.Selected.GetType() == typeof(DvElementTemplate);
        bool rendered = false;
        if (isElement)
        {
            var element = (DvElementTemplate)ctx.Selected;
            for (int x = 0; x < element.ElementFields.Count; x++)
            {
                var i = element.ElementFields.Values.ToList()[x];
                if (i.DataType == DvNodeTemplate.DataType.Curve)
                {
                    rendered = true;

                    float[] rValue = (float[])i.Value;
                    var clipSize = ImTimeline.GetClipSize();

                    ImPlot.PushStyleVar(ImPlotStyleVar.Padding, new Vector2(0, 0));
                    ImPlot.PushStyleVar(ImPlotStyleVar.BorderSize, 0);

                    if (Editors.DvScene.EditorCurve("##Track", ref rValue, ctx.CurveInfo, clipSize.X, clipSize.Y))
                    {
                        i.Value = rValue;
                        element.ElementFields.Values.ToList()[x] = i;
                    }

                    ImPlot.PopStyleVar(2);

                    break;
                }
            }
        }
        if (!rendered)
        {
            Vector2 p = ImGui.GetCursorScreenPos();
            var clipSize = ImTimeline.GetClipSize();
            ImGui.AddRectFilled(ImGui.GetWindowDrawList(), p, p + clipSize, ImGui.GetColorU32(ImGuiCol.WindowBg));
        }
    }

    public bool RenderClip(ref float start, ref float end, Action action)
    {
        bool changed = false;

        bool startTimeChanged = false;
        bool endTimeChanged = false;
        bool moved = false;

        if (ImTimeline.BeginClip("", ref start, ref end, 60, ref startTimeChanged, ref endTimeChanged, ref moved))
        {
            if (ImGui.BeginPopupContextItem("Properties"))
            {
                if (changed |= Basic.Editor("Start", ref start))
                    if (start < 0)
                        start = 0;

                if (changed |= Basic.Editor("End", ref end))
                    if (end < 0)
                        end = 0;

                ImGui.EndPopup();
            }

            action();

            ImTimeline.EndClip();
        }

        changed |= startTimeChanged | endTimeChanged;

        return changed;
    }

    public override void RenderPanel()
    {
        if (ctx.LoadedScene == null)
            return;

        if (ImGui.BeginChild("Timeline", new Vector2(0, 0), 0, ImGuiWindowFlags.HorizontalScrollbar))
        {
            float currentFrame = -1;
            bool play = false;
            bool currentTimeChanged = false;
            
            if (ImTimeline.BeginTimeline("Timeline", ref currentFrame, ctx.LoadedScene.Common.End, 60, ref play, ref currentTimeChanged))
            {
                if (ImTimeline.BeginTrack(""))
                {
                    if (ctx.Selected != null &&
                        (ctx.Selected.GetType() == typeof(DvElementTemplate) ||
                        ctx.Selected.GetType() == typeof(DvNodeTemplate))
                        )
                    {
                        var selected = (DvNodeTemplate)ctx.Selected;
                        float start = 0;
                        float end = 0;
                        bool isElement = ctx.Selected.GetType() == typeof(DvElementTemplate);
                        bool ticks = false;
                        string startName = "";
                        string endName = "";

                        foreach (var i in selected.Fields)
                        {
                            string lowerKey = i.Key.ToLower();
                            var value = i.Value;
                            if (lowerKey.Contains("start"))
                            {
                                startName = i.Key;
                                if (value.DataType == DvNodeTemplate.DataType.Float)
                                    start = (float)value.Value;
                                else if (ticks = value.DataType == DvNodeTemplate.DataType.UInt)
                                    start = (uint)value.Value / 100;
                            }
                            else if (lowerKey.Contains("end"))
                            {
                                endName = i.Key;
                                if (value.DataType == DvNodeTemplate.DataType.Float)
                                    end = (float)value.Value;
                                else if (ticks = value.DataType == DvNodeTemplate.DataType.UInt)
                                    end = (uint)value.Value / 100;
                            }
                        }

                        if (startName != "" && endName != "")
                        {
                            bool changed = RenderClip(ref start, ref end, RenderClipCurve);

                            if (changed)
                            {
                                var frameStart = selected.Fields[startName];
                                var frameEnd = selected.Fields[endName];
                                if (ticks)
                                {
                                    start *= 100;
                                    end *= 100;
                                    frameStart.Value = (uint)start;
                                    frameEnd.Value = (uint)end;
                                }
                                else
                                {
                                    frameStart.Value = start;
                                    frameEnd.Value = end;
                                }
                                selected.Fields[startName] = frameStart;
                                selected.Fields[endName] = frameEnd;
                                ctx.Selected = selected;
                            }
                        }

                    }
                    ImTimeline.EndTrack();
                }

                if (ImTimeline.BeginTrack("Cuts"))
                {
                    var cutInfo = ctx.LoadedScene.Common.CutInfo;

                    for (int i = 0; i < cutInfo.FrameCut.Count; i++) {
                        var x = cutInfo.FrameCut[i];

                        bool clicked = false;
                        if (ImTimeline.Event($"cut{i}", ref x, ref clicked))
                            cutInfo.FrameCut[i] = x;
                    }

                    ImTimeline.EndTrack();
                }

                if (ImTimeline.BeginTrack("Resource Cuts"))
                {
                    var cutInfo = ctx.LoadedScene.Common.ResourceCutInfo;

                    for (int i = 0; i < cutInfo.Frames.Count; i++)
                    {
                        var x = cutInfo.Frames[i];

                        bool clicked = false;
                        if (ImTimeline.Event($"rescut{i}", ref x, ref clicked))
                            cutInfo.Frames[i] = x;
                    }

                    ImTimeline.EndTrack();
                }

                ImTimeline.EndTimeline();
            }
        }
        ImGui.EndChild();
    }

    public override Properties GetProperties() => new Properties("Timeline", new(8, 20), new(160, 500));
}
