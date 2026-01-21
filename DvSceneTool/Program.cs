using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Utilities;
using Hexa.NET.ImNodes;
using Hexa.NET.ImPlot;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Diagnostics;

class DvSceneToolApp : GameWindow
{
    public static string Version = "0.1.1";
    static DvSceneToolApp instance;
    public static DvSceneToolApp Instance { get { return instance; } }

    ImGuiContextPtr imGuiCtx = ImGui.CreateContext();
    ImPlotContextPtr imPlotCtx = ImPlot.CreateContext();
    ImNodesContextPtr imNodesCtx = ImNodes.CreateContext();
    Stopwatch stopWatch = new();
    float fpsLimit = 60;
    
    public DvSceneToolApp(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
        InitImGUI();
        stopWatch.Start();
        FileDrop += OnFileDrop;
    }

    void OnFileDrop(FileDropEventArgs e)
    {
        foreach (var i in e.FileNames)
            DvSceneTool.Context.Instance.LoadFile(i);
    }

    void InitImGUI()
    {
        ImGui.SetCurrentContext(imGuiCtx);
        ImPlot.SetCurrentContext(imPlotCtx);
        ImPlot.SetImGuiContext(imGuiCtx);
        ImNodes.SetCurrentContext(imNodesCtx);
        ImNodes.SetImGuiContext(imGuiCtx);

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.DisplaySize = new(ClientSize.X, ClientSize.Y);

        ImGui.StyleColorsDark();
        ImPlot.GetStyle().LineWeight = 4;

        InitImGUIFont();
        InitImGUIGLFW();
        InitImGUIOpenGL();
    }

    void InitImGUIFont()
    {
        ImGuiFontBuilder builder = new();
        builder
            .AddFontFromFileTTF("res/fonts/Inter.ttf", 14);
        builder
            .Config.RasterizerMultiply = 5f;
        builder
            .AddFontFromFileTTF("res/fonts/NotoSansJP-VariableFont_wght.ttf", 20, [0x0020, 0x00FF, 0x3000, 0x30FF, 0x31F0, 0x31FF, 0xFF00, 0xFFEF, 0x4E00, 0x9FAF])
            .Build();
    }

    void InitImGUIOpenGL()
    {
        ImGuiImplOpenGL3.SetCurrentContext(ImGui.GetCurrentContext());
        ImGuiImplOpenGL3.Init((string)null!);
    }

    void InitImGUIGLFW()
    {
        ImGuiImplGLFW.SetCurrentContext(ImGui.GetCurrentContext());
        GLFWwindowPtr windowPtr = new();
        unsafe
        {
            windowPtr.Handle = (GLFWwindow*)WindowPtr;
        }
        ImGuiImplGLFW.InitForOpenGL(windowPtr, true);
    }

    protected override void OnLoad() => base.OnLoad();

    void FrameLimit()
    {
        float elapsedTime = (float)stopWatch.Elapsed.TotalSeconds;
        stopWatch.Restart();
        float frameTime = 1f / fpsLimit;
        if (elapsedTime < frameTime)
        {
            Thread.Sleep((int)((frameTime - elapsedTime) * 1000));
            elapsedTime = frameTime;
        }
    }

    void ImGuiBegin()
    {
        ImGui.SetCurrentContext(imGuiCtx);
        var color = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];
        GL.ClearColor(color.X, color.Y, color.Z, color.W);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        ImGui.SetNextFrameWantCaptureKeyboard(true);
        ImGui.SetNextFrameWantCaptureMouse(true);
        ImGuiImplOpenGL3.NewFrame();
        ImGuiImplGLFW.NewFrame();
        ImGui.NewFrame();

#if DEBUG
        ImGui.ShowDemoWindow();
        ImPlot.ShowDemoWindow();
#endif
    }

    void ImGuiEnd()
    {
        var io = ImGui.GetIO();
        ImGui.Render();
        ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

        if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }

        SwapBuffers();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        FrameLimit();
        ImGuiBegin();

        DvSceneTool.Context.Instance.Render();

        ImGui.End();

        ImGuiEnd();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        ImGui.GetIO().DisplaySize = new(e.Width, e.Height);
    }

    protected override void OnUnload() => base.OnUnload();

    public void SetTitleBarName(string name) => Title = name;

    static void Main(string[] args)
    {
        Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var gameSettings = new GameWindowSettings();
        var nativeSettings = new NativeWindowSettings
        {
            ClientSize = new(800, 600),
            Title = "DvScene Tool",
        };

        instance = new DvSceneToolApp(gameSettings, nativeSettings);
        for (int i = 0; i < args.Length; i++)
        {
            var x = args[i];
            if (x.ToLower() == "-t")
                DvSceneTool.Context.Instance.OpenDatabase(args[i++]);
            else
                DvSceneTool.Context.Instance.LoadFile(x);
        }
        instance.Run();
    }
}
