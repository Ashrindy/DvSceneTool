using DvSceneLib.IO.Template;
using DvSceneLib;
using Hexa.NET.ImGui;
using System.Numerics;
using DvSceneTool.Panels;

namespace DvSceneTool;

public sealed class Context
{
    static readonly Context instance = new Context();
    public static Context Instance { get { return instance; } }

    public struct CurveSettings
    {
        public enum CurveType : int
        {
            Linear,
            QuadraticIn,
            QuadraticOut,
            Cubic,
            Sine,
            LogarithmicIn,
            LogarithmicOut,
        }

        public float Falloff = 3;
        public bool Decreasing = false;
        public CurveType Type = CurveType.Linear;

        public CurveSettings()
        {
        }
    }

    public List<Panel> panels = new();
    public MenuBar menuBar;

    public DvScene LoadedScene;
    public string LoadedScenePath = "";

    public DiEventDataBase DiEvtDB;
    public Dictionary<string, List<DiEventDataBase.Node>> CategorizedNodesElems = new();

    public object Selected;
    public ResourceEntry SelectedResource;

    public CurveSettings CurveInfo = new();

    public Stack<ICommand> UndoStack = new();
    public Stack<ICommand> RedoStack = new();

    public void AddPanel<T>() where T : Panel
    {
        var panel = (T)Activator.CreateInstance(typeof(T), this);
        panels.Add(panel);
    }

    public T GetPanel<T>() where T : Panel
    {
        foreach (var panel in panels)
            if (typeof(T) == panel.GetType())
                return (T)panel;
        return null;
    }

    public Context()
    {
        _ = SettingsManager.Instance;
        _ = ThemesManager.Instance;

        menuBar = new(this);
        AddPanel<NodeHierarchy>();
        AddPanel<NodeInspector>();
        AddPanel<ResourceList>();
        AddPanel<ResourceInspector>();
        AddPanel<DvSceneSettings>();
        AddPanel<PageEditor>();
        AddPanel<Timeline>();

        if (Directory.Exists("templates"))
            foreach(var i in Directory.GetFiles("templates"))
                if(Path.GetFileNameWithoutExtension(i) == SettingsManager.Instance.settings.SelectedTemplateName)
                {
                    OpenDatabase(i);
                    break;
                }
    }

    #region UndoRedo
    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        UndoStack.Push(command);
        RedoStack.Clear();
    }

    public void AddCommand(ICommand command)
    {
        UndoStack.Push(command);
        RedoStack.Clear();
    }

    public void UndoCommand()
    {
        if (UndoStack.Count > 0)
        {
            var cmd = UndoStack.Pop();
            cmd.Undo();
            RedoStack.Push(cmd);
        }
    }

    public void RedoCommand()
    {
        if (RedoStack.Count > 0)
        {
            var cmd = RedoStack.Pop();
            cmd.Execute();
            UndoStack.Push(cmd);
        }
    }
    #endregion

    #region Rendering
    void RenderDockSpace()
    {
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar |
                                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize |
                                    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus |
                                    ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.Begin("DockSpace Window", windowFlags);
        ImGui.PopStyleVar(3);

        ImGui.SetCursorPosY(ImGui.GetFrameHeight());

        ImGui.DockSpace(ImGui.GetID("DockSpace"), new Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode);
    }

    public void Render()
    {
        if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) && ImGui.IsKeyDown(ImGuiKey.Z))
            UndoCommand();

        if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) && ImGui.IsKeyDown(ImGuiKey.Y))
            RedoCommand();

        menuBar.Render();

        RenderDockSpace();

        foreach (var x in panels)
            x.Render();
    }
    #endregion

    #region Node Editing Essentials
    void CategorizeNode(DiEventDataBase.Node node)
    {
        string category = node.Descriptions.ContainsKey("Category") ? node.Descriptions["Category"] : "";
        if (CategorizedNodesElems.ContainsKey(category))
            CategorizedNodesElems[category].Add(node);
        else
            CategorizedNodesElems.Add(category, new() { node });
    }

    public void CategorizeNodes()
    {
        CategorizedNodesElems = new();
        foreach (var i in DiEvtDB.Nodes) CategorizeNode(i);
        foreach (var i in DiEvtDB.Elements) CategorizeNode(i);
    }

    public void GetNodes(DvNodeTemplate node, ref List<DvNodeTemplate> nodes)
    {
        nodes.Add(node);
        foreach (var i in node.ChildNodes)
            GetNodes((DvNodeTemplate)i, ref nodes);
    }

    public List<DvNodeTemplate> GetNodes(DvScene dvscene)
    {
        List<DvNodeTemplate> nodes = new();
        GetNodes((DvNodeTemplate)dvscene.Common.Node, ref nodes);
        return nodes;
    }

    public DvNode GetNodeByNameGUID(string nameguid)
    {
        var nodes = GetNodes(LoadedScene);
        return nodes.Find(x => nameguid == x.NodeName + x.Guid);
    }
    #endregion

    #region Database
    public void OpenDatabase(string path)
    {
        if (!File.Exists(path)) return;

        DiEvtDB = new();
        DiEvtDB.Open(path);
        CategorizeNodes();
    }

    public void SetDatabase(DiEventDataBase db)
    {
        DiEvtDB = db;
        CategorizeNodes();
    }
    #endregion

    #region Loaded Scene Handling
    public void LoadFile(string file)
    {
        if (Path.GetExtension(file) != ".dvscene")
            return;

        Selected = null;
        SelectedResource = null;
        LoadedScene = new();
        LoadedScene.Open(file, DiEvtDB);
        LoadedScenePath = file;

        DvSceneToolApp.Instance.SetTitleBarName($"DvScene Tool - {Path.GetFileNameWithoutExtension(LoadedScenePath)}");
    }

    public void SaveFile(string file)
    {
        if (LoadedScene != null && file != "")
            LoadedScene.Save(file, DiEvtDB);
    }

    public void CreateFile()
    {
        Close();

        LoadedScene.Common.Node = new DvNodeTemplate(DiEvtDB.Nodes.Find(x => x.Descriptions.ContainsKey("rootNode") == true && x.Descriptions["rootNode"] == "true"));

        DvSceneToolApp.Instance.SetTitleBarName($"DvScene Tool - New DvScene");
    }

    public void Close()
    {
        LoadedScene = new();
        LoadedScenePath = "";
    }
    #endregion
}
