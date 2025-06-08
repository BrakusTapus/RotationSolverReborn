﻿using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Data;
using RotationSolver.Helpers;


namespace RotationSolver.Updaters;

internal static class RotationUpdater
{
    internal record CustomRotationGroup(Job JobId, Job[] ClassJobIds, Type[] Rotations);
    internal static SortedList<JobRole, CustomRotationGroup[]> CustomRotationsDict { get; private set; } = [];

    internal static Dictionary<Job, Dictionary<CombatType, List<ICustomRotation>>> CustomRotationsLookup { get; private set; } = [];

    internal static CustomRotationGroup[] CustomRotations { get; set; } = [];
    internal static SortedList<uint, Type[]> DutyRotations { get; set; } = [];

    public static IAction[] CurrentRotationActions { get; private set; } = [];

    private static DateTime LastRunTime;

    private static bool _isLoading = false;
    private static string _curDutyRotationName = string.Empty;

    public static Task ResetToDefaults()
    {
        try
        {
            string relayFolder = Svc.PluginInterface.ConfigDirectory.FullName + "\\Rotations";
            string[] files = Directory.GetFiles(relayFolder);
            foreach (string file in files)
            {
                PluginLog.Information($"Deleting {file}");
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to delete the rotation files: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves custom rotations from local and/or downloads
    /// them from remote server based on DownloadOption
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    public static async Task GetAllCustomRotationsAsync(DownloadOption option)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

        try
        {
            string relayFolder = Svc.PluginInterface.ConfigDirectory.FullName + "\\Rotations";
            _ = Directory.CreateDirectory(relayFolder);

            if (option.HasFlag(DownloadOption.Local))
            {
                LoadRotationsFromLocal(relayFolder);
            }

            if (option.HasFlag(DownloadOption.Download) && Service.Config.DownloadCustomRotations)
            {
                await DownloadRotationsAsync(relayFolder, option.HasFlag(DownloadOption.MustDownload));
            }

            if (option.HasFlag(DownloadOption.ShowList))
            {
                List<string> assemblies = [];
                HashSet<string> seen = [];
                foreach (KeyValuePair<JobRole, CustomRotationGroup[]> d in CustomRotationsDict)
                {
                    foreach (CustomRotationGroup g in d.Value)
                    {
                        foreach (Type r in g.Rotations)
                        {
                            string name = r.Assembly.FullName ?? string.Empty;
                            if (seen.Add(name))
                            {
                                assemblies.Add(name);
                            }
                        }
                    }
                }
                PrintLoadedAssemblies(assemblies);
            }
        }
        catch (Exception ex)
        {
            WarningHelper.AddSystemWarning($"Failed to load rotations because: {ex.Message}");
            PluginLog.Error($"Failed to get custom rotations: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static Assembly? LoadDefaultRotationsFromLocal()
    {
        DirectoryInfo? directory = Svc.PluginInterface.AssemblyLocation.Directory;
        if (directory == null || !directory.Exists)
        {
            PluginLog.Error("Failed to find main assembly directory");
            return null;
        }
        string assemblyPath = Path.Combine(directory.ToString(),
#if DEBUG
            "net9.0-windows\\RebornRotations.dll"
#else
            "RebornRotations.dll"
#endif
        );
        return LoadOne(assemblyPath);
    }

    /// <summary>
    /// This method loads custom rotation groups from local directories and assemblies, creates a sorted list of
    /// author hashes, and creates a sorted list of custom rotations grouped by job role.
    /// </summary>
    /// <param name="relayFolder"></param>
    private static void LoadRotationsFromLocal(string relayFolder)
    {
        List<string> directories = [];
        foreach (string lib in Service.Config.RotationLibs)
        {
            if (Directory.Exists(lib))
            {
                directories.Add(lib);
            }
        }
        if (Directory.Exists(relayFolder))
        {
            directories.Add(relayFolder);
        }

        List<Assembly> assemblies = [];

        if (Service.Config.LoadDefaultRotations)
        {
            Assembly? defaultAssembly = LoadDefaultRotationsFromLocal();
            if (defaultAssembly == null)
            {
                PluginLog.Error("Failed to load default rotations from local directory");
                return;
            }
            assemblies.Add(defaultAssembly);
        }

        foreach (string dir in directories)
        {
            if (Directory.Exists(dir))
            {
                foreach (string dll in Directory.GetFiles(dir, "*.dll"))
                {
                    if (dll.Contains("RebornRotations.dll"))
                    {
                        continue;
                    }

                    Assembly? assembly = LoadOne(dll);

                    bool found = false;
                    if (assembly != null)
                    {
                        foreach (Assembly a in assemblies)
                        {
                            if (a.FullName == assembly.FullName)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            assemblies.Add(assembly);
                        }
                    }
                }
            }
        }

        DutyRotations = LoadDutyRotationGroup(assemblies);
        CustomRotations = LoadCustomRotationGroup(assemblies);

        Dictionary<JobRole, List<CustomRotationGroup>> customRotationsGroupedByJobRole = [];
        foreach (CustomRotationGroup customRotationGroup in CustomRotations)
        {
            Job job = customRotationGroup.Rotations[0].GetType().GetCustomAttribute<JobsAttribute>()?.Jobs[0] ?? Job.ADV;
            JobRole jobRole = Svc.Data.GetExcelSheet<ClassJob>()!.GetRow((uint)job)!.GetJobRole();
            if (!customRotationsGroupedByJobRole.TryGetValue(jobRole, out List<CustomRotationGroup>? value))
            {
                value = [];
                customRotationsGroupedByJobRole[jobRole] = value;
            }
            value.Add(customRotationGroup);
        }

        CustomRotationsDict = [];
        foreach (KeyValuePair<JobRole, List<CustomRotationGroup>> kvp in customRotationsGroupedByJobRole)
        {
            List<CustomRotationGroup> customRotationGroups = kvp.Value;
            // Sort by JobId
            for (int i = 0; i < customRotationGroups.Count - 1; i++)
            {
                for (int j = i + 1; j < customRotationGroups.Count; j++)
                {
                    if (customRotationGroups[i].JobId > customRotationGroups[j].JobId)
                    {
                        (customRotationGroups[j], customRotationGroups[i]) = (customRotationGroups[i], customRotationGroups[j]);
                    }
                }
            }
            CustomRotationsDict[kvp.Key] = customRotationGroups.ToArray();
        }
    }

    private static SortedList<uint, Type[]> LoadDutyRotationGroup(List<Assembly> assemblies)
    {
        List<Type> rotationList = [];
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in TryGetTypes(assembly))
            {
                if (type.IsAssignableTo(typeof(DutyRotation))
                    && !type.IsAbstract && type.GetConstructor([]) != null)
                {
                    rotationList.Add(type);
                }
            }
        }

        Dictionary<uint, List<Type>> result = [];
        foreach (Type type in rotationList)
        {
            uint[] territories = type.GetCustomAttribute<DutyTerritoryAttribute>()?.TerritoryIds ?? [];

            foreach (uint id in territories)
            {
                if (result.TryGetValue(id, out List<Type>? list))
                {
                    list.Add(type);
                }
                else
                {
                    result[id] = [type];
                }
            }
        }

        SortedList<uint, Type[]> sorted = [];
        foreach (var pair in result)
        {
            sorted.Add(pair.Key, [.. pair.Value]);
        }
        return sorted;
    }

    private static CustomRotationGroup[] LoadCustomRotationGroup(List<Assembly> assemblies)
    {
        List<Type> rotationList = [];

        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in TryGetTypes(assembly))
            {
                ApiAttribute? apiAttribute = type.GetCustomAttribute<ApiAttribute>();
                AssemblyInfo info = assembly.GetInfo();
                string authorName = info.Author ?? "Unknown Author";

                bool implementsICustomRotation = false;
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface == typeof(ICustomRotation))
                    {
                        implementsICustomRotation = true;
                        break;
                    }
                }

                if (implementsICustomRotation
                    && !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    if (apiAttribute?.ApiVersion == Service.ApiVersion)
                    {
                        rotationList.Add(type);
                    }
                    else
                    {
                        string warning = $"Failed to load rotation {type.Assembly.GetName().Name} by {authorName} due to API update";
                        WarningHelper.AddSystemWarning(warning);
                    }
                }
            }
        }

        Dictionary<Job, List<Type>> rotationGroups = [];
        foreach (Type rotation in rotationList)
        {
            JobsAttribute? attr = rotation.GetCustomAttribute<JobsAttribute>();
            if (attr == null)
            {
                continue;
            }

            Job jobId = attr.Jobs[0];
            if (!rotationGroups.TryGetValue(jobId, out List<Type>? value))
            {
                value = [];
                rotationGroups.Add(jobId, value);
            }

            value.Add(rotation);
        }

        List<CustomRotationGroup> result = [];
        foreach (KeyValuePair<Job, List<Type>> kvp in rotationGroups)
        {
            Job jobId = kvp.Key;
            Type[] rotations = [.. kvp.Value];

            result.Add(new CustomRotationGroup(jobId, rotations[0].GetCustomAttribute<JobsAttribute>()!.Jobs,
                rotations));
        }

        CustomRotationsLookup = []; // CustomRotations aren't disposed or we'd want to loop through and dispose them
        return [.. result];
    }


    /// <summary>
    /// Downloads rotation files from a remote server and saves them to a local folder.
    /// The download list is obtained from a JSON file on the remote server.
    /// If mustDownload is set to true, it will always download the files, otherwise it will only download if the file doesn't exist locally. 
    /// </summary>
    /// <param name="relayFolder"></param>
    /// <param name="mustDownload"></param>
    /// <returns></returns>
    private static async Task DownloadRotationsAsync(string relayFolder, bool mustDownload)
    {
        // Code to download rotations from remote server
        bool hasDownload = false;

        using (HttpClient client = new())
        {
            foreach (string url in Service.Config.RotationLibs)
            {
                hasDownload |= await DownloadOneUrlAsync(url, relayFolder, client, mustDownload);
                string pdbUrl = Path.ChangeExtension(url, ".pdb");
                _ = await DownloadOneUrlAsync(pdbUrl, relayFolder, client, mustDownload);
            }
        }
        if (hasDownload)
        {
            LoadRotationsFromLocal(relayFolder);
        }
    }

    private static string Convert(string value)
    {
        string[] split = value.Split('|');
        if (split == null || split.Length < 2)
        {
            return value;
        }

        string username = split[0];
        string repo = split[1];
        string file = split.Last();
        return string.IsNullOrEmpty(username) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(file)
            ? value
            : $"https://GitHub.com/{username}/{repo}/releases/latest/download/{file}.dll";
    }

    private static async Task<bool> DownloadOneUrlAsync(string url, string relayFolder, HttpClient client, bool mustDownload)
    {
        try
        {
            bool valid = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uriResult)
                 && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!valid)
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
        try
        {
            string? fileName = url.Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            string filePath = Path.Combine(relayFolder, fileName);

            // Check if the file needs to be downloaded
            HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            FileInfo fileInfo = new(filePath);
            System.Net.Http.Headers.HttpContentHeaders header = response.Content.Headers;
            bool shouldDownload = mustDownload || !File.Exists(filePath) ||
                                  !header.LastModified.HasValue ||
                                  header.LastModified.Value.UtcDateTime >= fileInfo.LastWriteTimeUtc ||
                                  fileInfo.Length != header.ContentLength;

            if (!shouldDownload)
            {
                return false; // No need to download
            }

            // If reaching here, either the local file doesn't exist, or it's outdated. Proceed to download.
            if (File.Exists(filePath))
            {
                File.Delete(filePath); // Delete the old local file
            }

            using (FileStream stream = new(filePath, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(stream);
            }

            PluginLog.Information($"Successfully downloaded {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            WarningHelper.AddSystemWarning($"Failed to download from {url} Please check VPN");
            PluginLog.Error($"Failed to download from {url}: {ex.Message}");
        }
        return false;
    }


    private static void PrintLoadedAssemblies(IEnumerable<string>? assemblies)
    {
        if (assemblies == null)
        {
            return;
        }

        foreach (string assembly in assemblies)
        {
            Svc.Chat.Print("Loaded: " + assembly);
        }
    }

    private static Assembly? LoadOne(string filePath)
    {
        try
        {
            return RotationHelper.LoadCustomRotationAssembly(filePath);
        }
        catch (Exception ex)
        {
            WarningHelper.AddSystemWarning("Failed to load " + filePath);
            PluginLog.Warning($"Failed to load {filePath}: {ex.Message}");
        }
        return null;
    }

    // This method watches for changes in local rotation files by checking the
    // last modified time of the files in the directories specified in the configuration.
    // If there are new changes, it triggers a reload of the custom rotation.
    // This method uses Parallel.ForEach to improve performance.
    // It also has a check to ensure it's not running too frequently, to avoid hurting the FPS of the game.
    public static void LocalRotationWatcher()
    {
        if (DateTime.Now < LastRunTime.AddSeconds(2))
        {
            return;
        }

        string[] dirs = Service.Config.RotationLibs;

        foreach (string dir in dirs)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                continue;
            }

            string[] dlls = Directory.GetFiles(dir, "*.dll");

            // There may be many files in these directories,
            // so we opt to use Parallel.ForEach for performance.
            _ = Parallel.ForEach(dlls, async dll =>
            {
                LoadedAssembly loadedAssembly = new(
                    dll,
                    File.GetLastWriteTimeUtc(dll).ToString());

                int index = RotationHelper.LoadedCustomRotations.FindIndex(item => item.LastModified == loadedAssembly.LastModified);

                if (index == -1)
                {
                    await GetAllCustomRotationsAsync(DownloadOption.Local);
                }
            });
        }

        LastRunTime = DateTime.Now;
    }

    public static Type[] TryGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"Failed to load the types from {assembly.FullName}: {ex.Message}");
            return [];
        }
    }

    public static IEnumerable<IGrouping<string, IAction>>? AllGroupedActions
        => GroupActions([
            .. DataCenter.CurrentRotation?.AllActions ?? [],
            .. DataCenter.CurrentDutyRotation?.AllActions ?? []]);

    public static IEnumerable<IGrouping<string, IAction>>? GroupActions(IEnumerable<IAction> actions)
    {
        if (actions == null)
        {
            return null;
        }

        Dictionary<string, List<IAction>> groups = [];
        foreach (IAction a in actions)
        {
            string key = string.Empty;
            if (a is IBaseAction act)
            {
                if (!act.Info.IsOnSlot)
                {
                    key = string.Empty;
                }
                else if (act.Action.ActionCategory.RowId is 10 or 11)
                {
                    key = "System Action";
                }
                else if (act.Action.IsRoleAction)
                {
                    key = "Role Action";
                }
                else if (act.Info.IsLimitBreak)
                {
                    key = "Limit Break";
                }
                else if (act.Info.IsDutyAction)
                {
                    key = "Duty Action";
                }
                else
                {
                    key = act.Info.IsRealGCD ? "GCD" : "oGCD";
                    if (act.Setting.IsFriendly)
                    {
                        key += "-Friendly";
                    }
                    else
                    {
                        key += "-Attack";
                    }
                }
            }
            else if (a is IBaseItem)
            {
                key = "Item";
            }

            if (!string.IsNullOrEmpty(key))
            {
                if (!groups.TryGetValue(key, out List<IAction>? list))
                {
                    list = [];
                    groups[key] = list;
                }
                list.Add(a);
            }
        }

        // Sort groups by key
        List<string> sortedKeys = [.. groups.Keys];
        sortedKeys.Sort(StringComparer.Ordinal);

        List<IGrouping<string, IAction>> result = [];
        foreach (string key in sortedKeys)
        {
            result.Add(new SimpleGrouping<string, IAction>(key, groups[key]));
        }
        return result;
    }

    public static ICustomRotation[] GetRotations(Job playerJob, CombatType combatType)
    {
        if (Player.Object == null || CustomRotations.Length < 22) // If we haven't loaded rotations, we don't have anything to return
        {
            return [];
        }

        if (!CustomRotationsLookup.TryGetValue(playerJob, out Dictionary<CombatType, List<ICustomRotation>>? validCustomRotations))
        {
            InitReferenceDict(playerJob);
        }

        if (CustomRotationsLookup.TryGetValue(playerJob, out validCustomRotations))
        {
            if (validCustomRotations.Count == 0)
            {
                PluginLog.Warning($"No valid rotations found for {playerJob}");
                return [];
            }

            if (validCustomRotations.TryGetValue(combatType, out List<ICustomRotation>? validCustomRotationsList))
            {
                return [.. validCustomRotationsList];
            }
        }

        return [];
    }

    // Helper class for grouping (since LINQ's Grouping is not available)
    private class SimpleGrouping<TKey, TElement>(TKey key, IEnumerable<TElement> elements) : IGrouping<TKey, TElement>
    {
        private readonly IEnumerable<TElement> _elements = elements;

        public TKey Key { get; } = key;
        public IEnumerator<TElement> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _elements.GetEnumerator();
        }
    }

    public static void UpdateRotation()
    {
        UpdateCustomRotation();
        UpdateDutyRotation();
    }

    private static void UpdateDutyRotation()
    {
        if (!DutyRotations.TryGetValue(Svc.ClientState.TerritoryType, out Type[]? rotations))
        {
            return;
        }

        _ = Service.Config.DutyRotationChoice.TryGetValue(Svc.ClientState.TerritoryType, out string? value);
        string name = value ?? string.Empty;
        if (name == _curDutyRotationName && DataCenter.CurrentDutyRotation != null)
        {
            return; // No change, so we don't need to update
        }

        Type? type = GetChosenType(rotations, name);
        if (type != DataCenter.CurrentDutyRotation?.GetType())
        {
            DataCenter.CurrentDutyRotation?.Dispose();
            DataCenter.CurrentDutyRotation = GetRotation(type);
            _curDutyRotationName = name;
        }

        static DutyRotation? GetRotation(Type? t)
        {
            if (t == null)
            {
                return null;
            }

            try
            {
                return (DutyRotation?)Activator.CreateInstance(t);
            }
            catch (Exception ex)
            {
                WarningHelper.AddSystemWarning($"Failed to create the rotation: {t.Name}");
                PluginLog.Error($"Failed to create the rotation: {t.Name}: {ex.Message}");
                return null;
            }
        }
    }

    private static void UpdateCustomRotation()
    {
        if (Player.Object == null || CustomRotations.Length < 22) // If we haven't loaded rotations, don't bother calling this and ultimately clearing everything; wait for it to load and try again
        {
            return;
        }

        Job nowJob = Player.Job;
        CombatType curCombatType = DataCenter.IsPvP ? CombatType.PvP : CombatType.PvE;

        if (DataCenter.CurrentRotation?.Job == nowJob && DataCenter.CurrentRotation?.GetAttributes()?.Type == curCombatType)
        {
            return; // Nothing has changed, so we don't need to try and find a new rotation
        }

        if (!CustomRotationsLookup.TryGetValue(nowJob, out _))
        {
            InitReferenceDict(nowJob);
        }

        if (CustomRotationsLookup.TryGetValue(nowJob, out Dictionary<CombatType, List<ICustomRotation>>? validCustomRotations)) // Because default rotations exist, this *should* always have something; if not, no rotations
        {
            if (validCustomRotations.Count == 0) // We successfully got something, but we'll still check if there are any valid rotations
            {
                PluginLog.Warning($"No valid rotations found for {nowJob}");
                return;
            }

            if (validCustomRotations.TryGetValue(curCombatType, out List<ICustomRotation>? validCustomRotationsList))
            {
                string desiredRotationName = DataCenter.IsPvP ? Service.Config.PvPRotationChoice : Service.Config.RotationChoice;

                // Check if we have a matching rotation for the config, or use the first rotation which should be our default
                ICustomRotation rotation = validCustomRotationsList[0];
                foreach (var possibleRotation in validCustomRotationsList)
                {
                    if (possibleRotation.GetType().FullName == desiredRotationName)
                    {
                        rotation = possibleRotation;
                        break;
                    }
                }

                //If the rotation has changed, perform a clear
                if (rotation != DataCenter.CurrentRotation)
                {
                    rotation.OnTerritoryChanged();
                    DataCenter.CurrentRotation = rotation;
                    CurrentRotationActions = DataCenter.CurrentRotation?.AllActions ?? [];
                }

                return;
            }
        }

        DataCenter.CurrentRotation = null;
        CurrentRotationActions = [];
    }

    private static ICustomRotation? GetRotation(Type? t)
    {
        if (t == null)
        {
            return null;
        }

        try
        {
            return (ICustomRotation?)Activator.CreateInstance(t);
        }
        catch (Exception)
        {
#if DEBUG
            PluginLog.Error($"Failed to create the rotation: {t.Name}");
#endif
            return null;
        }
    }

    private static void InitReferenceDict(Job currentJob)
    {
        foreach (CustomRotationGroup customRotationGroup in CustomRotations)
        {
            if (!customRotationGroup.ClassJobIds.Contains(currentJob))
            {
                continue;
            }

            // Add this group for every job in ClassJobIds
            foreach (var job in customRotationGroup.ClassJobIds)
            {
                if (!CustomRotationsLookup.TryGetValue(job, out var rotationsListByType))
                {
                    rotationsListByType = new Dictionary<CombatType, List<ICustomRotation>>
                {
                    { CombatType.PvE, new List<ICustomRotation>() },
                    { CombatType.PvP, new List<ICustomRotation>() }
                };
                    CustomRotationsLookup[job] = rotationsListByType;
                }

                foreach (Type rotationType in customRotationGroup.Rotations)
                {
                    CombatType? comType = rotationType.GetCustomAttribute<RotationAttribute>()?.Type;
                    if (comType == null)
                    {
                        continue;
                    }
                    var possibleRotation = GetRotation(rotationType);
                    if (possibleRotation == null)
                    {
                        continue;
                    }

                    rotationsListByType[(CombatType)comType].Add(possibleRotation);
                }
            }
        }
    }

    private static Type? GetChosenType(IEnumerable<Type> types, string name)
    {
        Type? rotation = null;
        foreach (var r in types)
        {
            if (r.FullName == name)
            {
                rotation = r;
                break;
            }
        }

        if (rotation == null)
        {
            foreach (var r in types)
            {
                if (r.Assembly.FullName != null && r.Assembly.FullName.Contains("DefaultRotations", StringComparison.OrdinalIgnoreCase))
                {
                    rotation = r;
                    break;
                }
            }
        }

        if (rotation == null)
        {
            foreach (var r in types)
            {
                rotation = r;
                break;
            }
        }

        return rotation;
    }
}
