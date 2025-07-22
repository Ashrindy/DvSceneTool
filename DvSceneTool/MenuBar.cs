using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets.Dialogs;
using System.Text.Json;
using System.Threading.Channels;

namespace DvSceneTool;

public class MenuBar
{
    Context ctx { get; }
    public OpenFileDialog ofd = new() { OnlyAllowFilteredExtensions = true, AllowedExtensions = { ".dvscene" } };
    public SaveFileDialog sfd = new() { OnlyAllowFilteredExtensions = true, AllowedExtensions = { ".dvscene" } };

    void New() => ctx.CreateFile();
    void Open() => ofd.Show();
    void Save() => ctx.SaveFile(ctx.LoadedScenePath);
    void SaveAs() => sfd.Show();

    bool CanNew() => ctx.DiEvtDB != null;
    bool CanOpen() => ctx.DiEvtDB != null;
    bool CanSave() => ctx.LoadedScenePath != "" || ctx.LoadedScene != null;
    bool CanSaveAs() => ctx.LoadedScene != null;

    public MenuBar(Context ctx) => this.ctx = ctx;

    public void Render()
    {
        if (Utilities.IsControlDown() && ImGui.IsKeyPressed(ImGuiKey.N) && CanNew()) New();

        if (Utilities.IsControlDown() && ImGui.IsKeyPressed(ImGuiKey.O) && CanOpen()) Open();

        if (Utilities.IsControlDown() && ImGui.IsKeyPressed(ImGuiKey.S) && CanSave()) Save();

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New", "Ctrl+N", false, CanNew())) New();

                if (ImGui.MenuItem("Open", "Ctrl+O", false, CanOpen())) Open();

                if (ImGui.MenuItem("Save", "Ctrl+S", false, CanSave())) Save();

                if (ImGui.MenuItem("Save As", "", false, CanSaveAs())) SaveAs();

                ImGui.EndMenu();
            }
            /*if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Undo"))
                    DvSceneToolApp.UndoCommand();

                if (ImGui.MenuItem("Redo"))
                    DvSceneToolApp.RedoCommand();

                ImGui.EndMenu();
            }*/
            if (ImGui.BeginMenu("Options"))
            {
                var settingsMgr = SettingsManager.Instance;
                ref var settings = ref settingsMgr.settings;

                if (ImGui.BeginMenu("Template", Directory.Exists("templates")))
                {
                    string[] templates = Directory.GetFiles("templates");
                    string pureName = settings.SelectedTemplateName;
                    if (ImGui.BeginCombo("###template", pureName))
                    {
                        foreach (var item in templates)
                        {
                            bool selected = (pureName == Path.GetFileNameWithoutExtension(item));
                            if (ImGui.Selectable(Path.GetFileNameWithoutExtension(item), selected))
                            {
                                settings.SelectedTemplateName = Path.GetFileNameWithoutExtension(item);
                                settingsMgr.Save();
                                ctx.OpenDatabase(item);
                            }

                            if (selected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Theme", Directory.Exists("themes")))
                {
                    string pureName = settings.SelectedTheme;
                    if (ImGui.BeginCombo("###theme", pureName))
                    {
                        foreach (var item in ThemesManager.Instance.Themes)
                        {
                            bool selected = (pureName == item.Key);
                            if (ImGui.Selectable(item.Key, selected))
                            {
                                settings.SelectedTheme = item.Key;
                                settingsMgr.Save();
                                ThemesManager.Instance.SetTheme(item.Key);
                            }

                            var rootElement = item.Value.RootElement;
                            JsonElement metadata;
                            bool hasMetadata = rootElement.TryGetProperty("Metadata", out metadata);

                            if (hasMetadata && ImGui.BeginItemTooltip())
                            {
                                foreach(var i in metadata.EnumerateObject())
                                    ImGui.Text($"{i.Name}: {i.Value}");
                                ImGui.EndTooltip();
                            }

                            if (selected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                foreach (var i in ctx.panels)
                    ImGui.MenuItem(i.GetProperties().Name, "", ref i.Visible);

                ImGui.EndMenu();
            }

            string versionText = $"v{DvSceneToolApp.Version}";
            float right_text_width = ImGui.CalcTextSize(versionText).X;
            float menu_bar_width = ImGui.GetContentRegionAvail().X;
            float padding = 10;

            ImGui.SameLine(ImGui.GetWindowPos().X + ImGui.GetWindowSize().X - right_text_width - ImGui.GetStyle().ItemSpacing.X - padding);
            ImGui.Text(versionText);

            ImGui.EndMainMenuBar();
        }

        ofd.Draw(0);
        sfd.Draw(0);

        if(ofd.Result == DialogResult.Yes && ofd.SelectedFile != null)
        {
            foreach (var file in ofd.Selection)
                ctx.LoadFile(file);
            ofd.Reset();
            ofd.Close();
        }

        if (sfd.Result == DialogResult.Yes && sfd.SelectedFile != null)
        {
            if(ctx.LoadedScenePath == "")
                ctx.LoadedScenePath = sfd.SelectedFile;
            ctx.SaveFile(sfd.SelectedFile);
            sfd.Reset();
            sfd.Close();
        }
    }
}
