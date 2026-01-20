using Hexa.NET.ImGui;
using System.Numerics;

namespace DvSceneTool.Widgets;

// Implementation comes from https://github.com/HE2-SDK/he2-devtools/tree/main/vendor/imtimeline
// Slightly altered for this project

public struct ImTimelineContext
{
    public float currentTime = 0;
    public float length = 0;
    public float divisionLength = 0;
    public float zoom = 1;
    public float trackHeight = 0;
    public Vector2 timelineScreenPos = new(0, 0);
    public Vector2 trackScreenPos = new(0, 0);
    public Vector2 clipDimensions = new(0, 0);

    public ImTimelineContext() { }
}

public class ImTimeline
{
    static float nameColumnWidth = 200.0f;
    static float playHeadSize = 10.0f;
    static float minZoom = 0.05f;
    static float maxZoom = 20.0f;

    static ImTimelineContext gCtx = new();

    public static bool TimeSelect(string id, ref float time, Vector2 size, float min = 0, float max = 1)
    {
        var screenPos = ImGui.GetCursorScreenPos();

        ImGui.PushID(id);
        bool clicked = ImGui.InvisibleButton("button", size);

        var s = ImGui.GetStateStorage();

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            time = Math.Max(min, Math.Min(max, (ImGui.GetMousePos().X - screenPos.X) / gCtx.zoom));
            s.SetBool(ImGui.GetID("isDragging"), true);
        }

        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            s.SetBool(ImGui.GetID("isDragging"), false);

        if (s.GetBool(ImGui.GetID("isDragging")) && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            time = Math.Max(min, Math.Min(max, (ImGui.GetMousePos().X - screenPos.X) / gCtx.zoom));

            ImGui.PopID();
            return true;
        }

        ImGui.PopID();
        return false;
    }

    public static bool TimeDrag(string id, ref float time, Vector2 size, float min = 0, float max = 1) {

        ImGui.PushID(id);
		bool clicked = ImGui.InvisibleButton("button", size);

        var s = ImGui.GetStateStorage();

		if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
			s.SetBool(ImGui.GetID("isDragging"), true);
			s.SetFloat(ImGui.GetID("origTime"), time);
		}

		if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
			s.SetBool(ImGui.GetID("isDragging"), false);

		if (s.GetBool(ImGui.GetID("isDragging")) && ImGui.IsMouseDragging(ImGuiMouseButton.Left)) {

            time = Math.Max(min, Math.Min(max, s.GetFloat(ImGui.GetID("origTime")) + ImGui.GetMouseDragDelta().X / gCtx.zoom));

            ImGui.PopID();
			return true;
		}

		ImGui.PopID();
        return false;
	}

    public static bool PlayHead(string id, ref float currentTime, float height = 20.0f, float minTickSpacing = 10.0f) {

        var dl = ImGui.GetWindowDrawList();

        var screenPos = ImGui.GetCursorScreenPos();
        float playHeadHeight = playHeadSize * 0.86602540378443864676372317075294f;

		bool changed = gCtx.length > 0.0f ? TimeSelect(id, ref currentTime, new Vector2(gCtx.length * gCtx.zoom, height), max: gCtx.length) : false;

        var playHeadScreenPos = new Vector2(screenPos.X + currentTime * gCtx.zoom, screenPos.Y + height);

        dl.AddTriangleFilled(
            playHeadScreenPos + new Vector2(-playHeadSize / 2, -playHeadHeight),
			playHeadScreenPos + new Vector2(playHeadSize / 2, -playHeadHeight),
			playHeadScreenPos + new Vector2(0.0f, 0.0f),
			0xFFFFFFFF
		);

		return changed;
	}

    public static bool BeginTimeline(string id, ref float currentTime, float length, float divisionLength, ref bool playing, ref bool currentTimeChanged, bool showPlay = false)
	{
        ImGui.PushID(id);

        if (showPlay) ImGui.Checkbox("Play", ref playing);

		if (ImGui.IsKeyChordPressed((int)(ImGuiKey.MouseWheelY | ImGuiKey.ModCtrl)))
			gCtx.zoom = Math.Max(minZoom, Math.Min(maxZoom, gCtx.zoom* MathF.Pow(2, ImGui.GetIO().MouseWheel)));

		ImGui.SameLine();
		ImGui.Text($"Zoom: {gCtx.zoom}");

		ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 0));

		if (!ImGui.BeginTable("Timeline", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY)) {
			ImGui.PopStyleVar();
			ImGui.PopID();
			return false;
		}

        ImGui.TableSetupScrollFreeze(1, 1);
		ImGui.TableSetupColumn("Track name", ImGuiTableColumnFlags.WidthFixed, nameColumnWidth);
		ImGui.TableSetupColumn("Timeline", ImGuiTableColumnFlags.WidthFixed, length * gCtx.zoom);

		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.TableNextColumn();

		gCtx.timelineScreenPos = ImGui.GetCursorScreenPos();
		gCtx.length = length;
		gCtx.divisionLength = divisionLength;

        if (currentTime != -1)
		    if (PlayHead("playhead", ref currentTime) && currentTimeChanged)
               currentTimeChanged = true;

        gCtx.currentTime = currentTime;

		return true;
	}

    public static void EndTimeline()
    {
        var dl = ImGui.GetWindowDrawList();
        var screenPosY = ImGui.GetCursorScreenPos().Y;

        long sliceCount = (long)(gCtx.length / gCtx.divisionLength);
        float sliceRemainder = gCtx.length % gCtx.divisionLength;

        for (long i = 0; i < sliceCount; i++)
            dl.AddRectFilled(gCtx.timelineScreenPos + new Vector2(i * gCtx.divisionLength * gCtx.zoom, 20.0f), new Vector2(gCtx.timelineScreenPos.X + (i * gCtx.divisionLength + gCtx.divisionLength / 2.0f) * gCtx.zoom, screenPosY), 0x20FFFFFF);

        if (sliceRemainder > 0.0f)
            dl.AddRectFilled(gCtx.timelineScreenPos + new Vector2(sliceCount * gCtx.divisionLength * gCtx.zoom, 20.0f), new Vector2(gCtx.timelineScreenPos.X + (sliceCount * gCtx.divisionLength + Math.Min(gCtx.divisionLength / 2.0f, sliceRemainder)) * gCtx.zoom, screenPosY), 0x20FFFFFF);
        
        if (gCtx.currentTime != -1)
            dl.AddLine(gCtx.timelineScreenPos + new Vector2(gCtx.currentTime * gCtx.zoom, 20.0f), new Vector2(gCtx.timelineScreenPos.X + gCtx.currentTime * gCtx.zoom, screenPosY), 0xFFFFFFFF);

        ImGui.EndTable();
        ImGui.PopStyleVar();
        ImGui.PopID();
    }

    public static bool BeginTrackGroup(string id) {

        ImGui.TableNextRow(ImGuiTableRowFlags.None, 20.0f);
		ImGui.TableNextColumn();

		var isOpen = ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.DefaultOpen, id);

        ImGui.TableNextColumn();

		return isOpen;
	}

    public static void EndTrackGroup()
    {
        ImGui.TreePop();
    }

    public static bool BeginTrack(string id) {

        ImGui.TableNextRow(ImGuiTableRowFlags.None, 20.0f);
		ImGui.TableNextColumn();

		var isOpen = ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen, id);

        ImGui.TableNextColumn();

		gCtx.trackScreenPos = ImGui.GetCursorScreenPos();

		ImGui.PushID(id);

		return isOpen;
	}

    public static void EndTrack()
    {
        ImGui.PopID();
    }

    public static bool BeginClip(string id, ref float startTime, ref float endTime, float height, ref bool startTimeChanged, ref bool endTimeChanged, ref bool moved)
	{
        float timeDiff = endTime - startTime;

        gCtx.clipDimensions = new Vector2(timeDiff* gCtx.zoom, height);

        ImGui.SetCursorScreenPos(gCtx.trackScreenPos + new Vector2(startTime * gCtx.zoom, 0.0f));

		if (!ImGui.BeginChild(id, gCtx.clipDimensions + new Vector2(0.0f, ImGui.GetTextLineHeightWithSpacing()))) {
			ImGui.EndChild();
			return false;
		}

        var dl = ImGui.GetWindowDrawList();

        dl.AddRectFilled(ImGui.GetWindowPos(), ImGui.GetWindowPos() + new Vector2(gCtx.clipDimensions.X, ImGui.GetTextLineHeightWithSpacing()), 0xFF5A6AED);
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 1));
		ImGui.Text(id);
		ImGui.PopStyleColor();
		ImGui.SetItemTooltip(id);

		var pos = ImGui.GetCursorPos();

        ImGui.SetCursorPos(new Vector2(0.0f, 0.0f));
		if (TimeDrag("startTimeHandle", ref startTime, new Vector2(5.0f, ImGui.GetTextLineHeightWithSpacing()), max: gCtx.length)) {

            startTime = Math.Min(startTime, endTime - 1.0f);

            startTimeChanged = true;
		}

		if (ImGui.IsItemHovered())
            dl.AddRectFilled(ImGui.GetWindowPos() + new Vector2(0.0f, 0.0f), ImGui.GetWindowPos() + new Vector2(5.0f, ImGui.GetTextLineHeightWithSpacing()), ImGui.GetColorU32(ImGuiCol.SeparatorHovered));

        ImGui.SetCursorPos(new Vector2(gCtx.clipDimensions.X - 5.0f, 0.0f));
        if (TimeDrag("endTimeHandle", ref endTime, new Vector2(5.0f, ImGui.GetTextLineHeightWithSpacing()), max: gCtx.length))
        {
            endTime = Math.Max(endTime, startTime + 1.0f);

            endTimeChanged = true;
        }

        if (ImGui.IsItemHovered())
            dl.AddRectFilled(ImGui.GetWindowPos() + new Vector2(gCtx.clipDimensions.X - 5.0f, 0.0f), ImGui.GetWindowPos() + new Vector2(gCtx.clipDimensions.X, ImGui.GetTextLineHeightWithSpacing()), ImGui.GetColorU32(ImGuiCol.SeparatorHovered));

        ImGui.SetCursorPos(new Vector2(0.0f, 0.0f));
        if (TimeDrag("titlebar", ref startTime, new Vector2(gCtx.clipDimensions.X, ImGui.GetTextLineHeightWithSpacing()), 0.0f, gCtx.length - timeDiff))
        {
            endTime = startTime + timeDiff;

            moved = true;
            startTimeChanged = true;
            endTimeChanged = true;
        }

        ImGui.SetCursorPos(pos);

        return true;
	}

    public static void EndClip()
    {
        ImGui.SetCursorPos(new Vector2(0, 0));
        if (ImGui.BeginChild("overlays", gCtx.clipDimensions + new Vector2(0.0f, ImGui.GetTextLineHeightWithSpacing()), ImGuiChildFlags.None, ImGuiWindowFlags.NoInputs))
        {
            var screenPos = ImGui.GetCursorScreenPos();

            ImGui.GetWindowDrawList().AddRect(screenPos, screenPos + gCtx.clipDimensions + new Vector2(0.0f, ImGui.GetTextLineHeightWithSpacing()), 0xFF000000);
            ImGui.GetWindowDrawList().AddLine(screenPos + new Vector2(0.0f, ImGui.GetTextLineHeightWithSpacing()), screenPos + new Vector2(gCtx.clipDimensions.X, ImGui.GetTextLineHeightWithSpacing()), 0xFF000000);
        }
        ImGui.EndChild();
        ImGui.EndChild();
    }

    public static bool Event(string id, ref float time, ref bool clicked)
	{
        float radius = 5.0f;
        float spacing = 5.0f;

        ImGui.SetCursorScreenPos(gCtx.trackScreenPos + new Vector2(time * gCtx.zoom - radius - spacing, 0.0f));

		var screenPos = ImGui.GetCursorScreenPos();

        ImGui.PushID(id);

		ImGui.GetWindowDrawList().AddCircleFilled(screenPos + new Vector2(radius + spacing, radius + spacing), radius, 0xFF5A6AED);
		ImGui.GetWindowDrawList().AddCircle(screenPos + new Vector2(radius + spacing, radius + spacing), radius, 0xFF000000);

		bool changed = TimeDrag("handle", ref time, new Vector2((radius + spacing) * 2.0f, (radius + spacing) * 2.0f), max: gCtx.length);

        ImGui.PopID();

		return changed;
	}

    public static bool IsNameColumnHovered() => (ImGui.TableGetColumnFlags(0) & ImGuiTableColumnFlags.IsHovered) != 0;
    public static bool IsTimelineColumnHovered() => (ImGui.TableGetColumnFlags(1) & ImGuiTableColumnFlags.IsHovered) != 0;

    public static float GetMouseTime()
    {
        var mousePos = ImGui.GetMousePos();

        return (mousePos.X - gCtx.timelineScreenPos.X) / gCtx.zoom;
    }

    public static Vector2 GetClipSize() => gCtx.clipDimensions;
}
