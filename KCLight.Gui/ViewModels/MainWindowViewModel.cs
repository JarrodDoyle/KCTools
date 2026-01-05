using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeepersCompound.Dark;
using KeepersCompound.Dark.Database;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Resources;
using KeepersCompound.Lighting;
using Serilog;
using Serilog.Events;

namespace KCLight.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IObserver<LogEvent>
{
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _installPath = "";

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _campaignName = "";

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _missionName = "";

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _outputName = "kc_lit";

    [ObservableProperty] private bool _validInstallPath;
    [ObservableProperty] private bool _validCampaignName;
    [ObservableProperty] private bool _validMissionName;
    [ObservableProperty] private ObservableCollection<string> _campaignNames = [];
    [ObservableProperty] private ObservableCollection<string> _missionNames = [];
    [ObservableProperty] private ObservableCollection<string> _logLines = [];

    private InstallContext _context = null!;
    private ResourceManager _resources = null!;

    partial void OnInstallPathChanged(string value)
    {
        if (InitialiseContext())
        {
            InitialiseCampaignResources();
        }
    }

    partial void OnCampaignNameChanged(string value)
    {
        InitialiseCampaignResources();
    }

    partial void OnMissionNameChanged(string value)
    {
        ValidateMissionName();
    }

    private void ValidateCampaignName()
    {
        ValidCampaignName = ValidInstallPath && CampaignNames.Contains(CampaignName);
    }

    private void ValidateMissionName()
    {
        ValidMissionName = ValidInstallPath && ValidCampaignName && MissionNames.Contains(MissionName.ToLower());
    }

    private void UpdateMissionNames()
    {
        if (!ValidCampaignName)
        {
            return;
        }

        var dbNames = _resources.GetDbFileNames().ToList();
        dbNames.Sort();

        MissionNames.Clear();
        foreach (var dbName in dbNames)
        {
            var fileName = Path.GetFileName(dbName);
            var ext = Path.GetExtension(dbName).ToLower();
            if (ext is ".mis" or ".cow")
            {
                MissionNames.Add(fileName.ToLower());
            }
        }
    }

    private void InitialiseCampaignResources()
    {
        ValidateCampaignName();
        if (ValidCampaignName)
        {
            _resources.SetActiveCampaign(CampaignName);
        }
        UpdateMissionNames();
        ValidateMissionName();
    }

    private bool InitialiseContext()
    {
        _context = new InstallContext(InstallPath);
        _resources = new ResourceManager(_context);
        CampaignNames = new ObservableCollection<string>(_context.Fms);
        ValidInstallPath = _context.Valid;
        if (!ValidInstallPath)
        {
            Log.Error("Invalid install context");
            return false;
        }

        return true;
    }

    [RelayCommand(CanExecute = nameof(ValidMissionName))]
    private async Task RunAsync()
    {
        await Task.Run(() =>
        {
            Timing.Reset();
            Timing.TimeStage("Total", () =>
            {
                var (loaded, mission) = Timing.TimeStage("Load Mission File", () =>
                {
                    var loaded = _resources.TryGetDbFile(MissionName, out var mission);
                    return (loaded, mission);
                });

                if (!loaded || mission == null)
                {
                    return;
                }

                if (!LightMapper.RequiredChunksExist(mission) ||
                    !mission.TryGetChunk<WorldRep>("WREXT", out var worldRep) ||
                    !mission.TryGetChunk<RendParams>("RENDPARAMS", out var rendParams) ||
                    !mission.TryGetChunk<LmParams>("LM_PARAM", out var lmParams) ||
                    !mission.TryGetChunk<BrList>("BRLIST", out var brList) ||
                    !mission.TryGetChunk<PropertyChunk<PropAnimLight>>("P$AnimLight", out var animLights))
                {
                    Log.Error("Failed to load required chunk.");
                    return;
                }

                var hierarchy = new ObjectHierarchy(_resources, mission);
                var settings = LightMapperSettingsBuilder.FromChunks(worldRep, rendParams, lmParams);
                var lightMapper = new LightMapper(settings);
                lightMapper.Light(_resources, hierarchy, worldRep, brList, rendParams, lmParams, animLights);
                if (_resources.TryGetDbFileVirtualPath(MissionName, out var virtualMisPath) &&
                    _resources.TryGetFilePath(virtualMisPath, out var misPath))
                {
                    var folder = Path.GetDirectoryName(misPath);
                    var misName = OutputName + Path.GetExtension(misPath);
                    var savePath = Path.Join(folder, misName);
                    Timing.TimeStage("Save Mission File", () => mission.Save(savePath));
                }
            });
            Timing.LogAll();
        });
    }

    [RelayCommand]
    private async Task SelectGameDirectory()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            throw new NullReferenceException("Missing StorageProvider instance.");
        }

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Game Directory",
            AllowMultiple = false,
            SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(InstallPath)
        };

        var folders = await provider.OpenFolderPickerAsync(options);
        if (folders.Count > 0)
        {
            InstallPath = folders[0].Path.LocalPath;
        }
    }

    [RelayCommand]
    private void ReloadGameDirectory()
    {
        if (InitialiseContext())
        {
            InitialiseCampaignResources();
        }
    }

    public void Close()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(LogEvent value)
    {
        var message = value.RenderMessage();
        LogLines.Add(message);
    }
}