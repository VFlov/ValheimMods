using BepInEx;
using HarmonyLib;
using UnityEngine;
using Jotunn;
using Jotunn.Managers;
using System.Reflection;
using static LightFlicker;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Runtime;
using System.Threading;
using System;
using UnityEngine.UI;
using TMPro;
using static Minimap;
using SoftReferenceableAssets.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
using System.Text;
using Jotunn.Utils;


namespace FramePerSecondPlus
{
    [BepInPlugin("vaffle.FramePerSecondPlus", "FramePerSecondPlus", "1.1.3")]
    public partial class FramePerSecondPlus : BaseUnityPlugin
    {
        private static ConfigEntry<float> spawnUpdateInterval;
        private static ConfigEntry<bool> enableMultithreading;

        private static ConfigEntry<bool> enableLogging;
        private static ConfigEntry<float> spawnDelay;

        private static ConfigEntry<float> generationTimeBudget;
        private static ConfigEntry<int> maxLocationAttempts;

        private static ConfigEntry<bool> enableHeightmapCaching;
        private static ConfigEntry<float> grassDistance;
        private static ConfigEntry<int> particleRaycastBudget;


        private static ConfigEntry<bool> shadowDistanceToggle;
        private static ConfigEntry<float> shadowDistance;
        private static ConfigEntry<bool> terrainTreeDistanceToggle;
        private static ConfigEntry<float> terrainTreeDistance;



        private static int warningCount = 0;
        private static int errorCount = 0;
        private static ManualLogSource Log;
        string[] PrefabLightNames = new string[] { /*"CastleKit_groundtorch", "CastleKit_groundtorch_blue", "CastleKit_groundtorch_green", "CastleKit_groundtorch_unlit", "CastleKit_metal_groundtorch_unlit",*/ "piece_groundtorch", "piece_groundtorch_blue", "piece_groundtorch_green", "piece_groundtorch_mist", "piece_groundtorch_wood", "piece_walltorch"/*, "piece_brazierfloor01", "piece_brazierfloor02" */};

        private static ConfigEntry<bool> skipIntro;
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);
            return configEntry;
        }
        private void AddConfiguration()
        {
            skipIntro = config<bool>("General", "SkipIntro", true, new ConfigDescription("Skip the game logo to speed up the loading of the game"));
        }
        

        
        void Awake()
        {

            spawnUpdateInterval = Config.Bind("General", "SpawnUpdateInterval", 2f, "Interval between spawn updates in seconds");
            enableMultithreading = Config.Bind("General", "EnableMultithreading", true, "Enable multithreaded spawn calculations");
            enableLogging = Config.Bind("General", "EnableLogging", false, "Enable logging for spawn events");
            spawnDelay = Config.Bind("General", "SpawnDelay", 1f, "Delay before spawning in seconds (0 for immediate spawn)");
            generationTimeBudget = Config.Bind("General", "GenerationTimeBudget", 0.1f, "Time budget per frame for location generation (seconds)");
            maxLocationAttempts = Config.Bind("General", "MaxLocationAttempts", 50000, "Max attempts per location (lower to speed up)");
            enableHeightmapCaching = Config.Bind("General", "EnableHeightmapCaching", true, "Cache heightmap data for zones");
            grassDistance = Config.Bind("General", "GrassDistance", 20f, "Distance for render grass");
            particleRaycastBudget = Config.Bind("General", "ParticleRaycastBudget", 1024, "Affects the quality of the light emitted by light sources");

            shadowDistanceToggle = Config.Bind("Warning", "ShadowDistance", false, "If enabled, the shadow drawing distance setting will be available for editing.");
            shadowDistance = Config.Bind("Warning", "ShadowDistanceToggle", 150f, "Shadow drawing distance");
            terrainTreeDistanceToggle = Config.Bind("Warning", "TerrainTreeDistanceToggle", false, "If enabled, the tree drawing distance setting will be available for editing.");
            terrainTreeDistance = Config.Bind("Warning", "TerrainTreeDistance", 5000f, "Trees drawing distance");


            Logger.LogInfo(spawnUpdateInterval.Value);
            Logger.LogInfo(enableMultithreading.Value);
            Logger.LogInfo(enableLogging.Value);
            Logger.LogInfo(spawnDelay.Value);
            Logger.LogInfo(generationTimeBudget.Value);
            Logger.LogInfo(maxLocationAttempts.Value);
            Logger.LogInfo(enableHeightmapCaching.Value);
            Logger.LogEvent += OnLogEvent;
            FramePerSecondPlus.Log = base.Logger;
            AddConfiguration();
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.shadowCascades = 0;
            QualitySettings.shadowNearPlaneOffset = 0;
            QualitySettings.particleRaycastBudget = particleRaycastBudget.Value;
            QualitySettings.softVegetation = false;
           
            if (shadowDistanceToggle.Value)
                QualitySettings.shadowDistance = shadowDistance.Value;
            if (terrainTreeDistanceToggle.Value)
                QualitySettings.terrainTreeDistance = terrainTreeDistance.Value;
            //QualitySettings.shadowResolution = ShadowResolution.Low;

            PrefabManager.OnPrefabsRegistered += CustomAwake;



            // Загрузка AssetBundle из внедренного ресурса
            /*(
            AssetBundle bundle = AssetUtils.LoadAssetBundleFromResources("fpsplus", typeof(FramePerSecondPlus).Assembly);
            if (bundle == null)
            {
                Logger.LogError("Failed to load AssetBundle 'fpsplus' from resources!");
                return;
            }

            GameObject canvasPrefab = bundle.LoadAsset<GameObject>("CanvasFPSP");
            if (canvasPrefab == null)
            {
                Logger.LogError("CanvasFPSP prefab not found in AssetBundle!");
                bundle.Unload(false);
                return;
            }

            GameObject canvasInstance = Instantiate(canvasPrefab);
            canvasInstance.name = "CanvasFPSP_Instance";
            DontDestroyOnLoad(canvasInstance);

            bundle.Unload(false);

            // Инициализация элементов
            //InitializeUIComponents(canvasInstance);
            */
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        void CustomAwake()
        {
            for (int i = 0; i < PrefabLightNames.Length; i++)
                TorchParticles(PrefabManager.Instance.GetPrefab(PrefabLightNames[i]));
        }
        void TorchParticles(GameObject gameObject)
        {

            string childName = "fx_Torch_Basic";
            if (gameObject.name == "piece_groundtorch_blue")
                childName = "fx_Torch_Blue";
            else if (gameObject.name == "piece_groundtorch_green")
                childName = "fx_Torch_Green";
            else if (gameObject.name == "piece_groundtorch_mist")
                childName = "sparcs_front";
            //else if (gameObject.name == "MountainKit_brazier_blue" || gameObject.name == "MountainKit_brazier")
            //    childName = "fx_Brazier_flames";
            var fx = gameObject.FindDeepChild(childName).GetComponent<ParticleSystem>();
            fx.startLifetime = 0.2f;
            fx.gravityModifier = -0.3f;
        }

        private void OnLogEvent(object sender, LogEventArgs eventArgs)
        {
            // Фильтруем по уровню логирования
            switch (eventArgs.Level)
            {
                case LogLevel.Error:
                    errorCount++;
                    break;
                case LogLevel.Warning:
                    warningCount++;
                    break;
                case LogLevel.Message:
                    break;
                case LogLevel.Info:
                    break;
                    // Другие уровни (Warning, Debug и т.д.) можно игнорировать или учитывать отдельно
            }
        }

        private void OnDestroy()
        {
            // Отписываемся от события при выгрузке плагина
            Logger.LogEvent -= OnLogEvent;
        }
        //===============================================================================================

        //======================================================================================================
        [HarmonyPatch(typeof(ClutterSystem), "Awake")]
        private static class ClutterSystem_Awake_Patch
        {
            private static void Prefix(ClutterSystem __instance)
            {
                __instance.m_grassPatchSize = grassDistance.Value;
            }
        }
        [HarmonyPatch(typeof(SceneLoader), "LoadSceneAsync")]
        class SceneLoaderPatch
        {
            static bool Prefix(SceneLoader __instance, ref IEnumerator __result)
            {
                // Replace the original LoadSceneAsync with our optimized version
                __result = FastLoadSceneAsync(__instance);
                return false; // Skip the original method
            }

            static IEnumerator FastLoadSceneAsync(SceneLoader instance)
            {
                // Log the start of the scene loading
                string sceneName = AccessTools.Field(typeof(SceneLoader), "m_scene").GetValue(instance)?.ToString() ?? "Unknown";
                UnityEngine.Debug.Log($"FastSceneLoader: Starting to load scene: {sceneName}");

                // Start the async scene loading
                var sceneField = AccessTools.Field(typeof(SceneLoader), "m_scene");
                var scene = (SceneReference)sceneField.GetValue(instance);
                var loadOperationField = AccessTools.Field(typeof(SceneLoader), "_sceneLoadOperation");
                var loadOperation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
                loadOperationField.SetValue(instance, loadOperation);

                // Request high-priority loading budget immediately
                var budgetRequestField = AccessTools.Field(typeof(SceneLoader), "_currentLoadingBudgetRequest");
                budgetRequestField.SetValue(instance, BackgroundLoadingBudgetController.RequestLoadingBudget(UnityEngine.ThreadPriority.High));

                // Allow scene activation immediately to reduce delays
                loadOperation.AllowSceneActivation = true;

                // Initialize platform save data
                PlatformInitializer.AllowSaveDataInitialization = true;
                while (!PlatformInitializer.SaveDataInitialized)
                {
                    yield return null;
                }

                // Show loading indicator
                LoadingIndicator.SetVisibility(true);

                // Wait for scene loading to complete
                while (!loadOperation.IsDone)
                {
                    // Update fake progress for smooth UI feedback
                    float progress = loadOperation.Progress;
                    AccessTools.Field(typeof(SceneLoader), "_fakeProgress").SetValue(instance, progress);
                    LoadingIndicator.SetProgress(progress);
                    yield return null;
                }

                // Ensure input device is ready
                PlatformInitializer.InputDeviceRequired = true;
                while (PlatformInitializer.WaitingForInputDevice)
                {
                    yield return null;
                }

                // Hide loading indicator
                LoadingIndicator.SetVisibility(false);
                while (!LoadingIndicator.IsCompletelyInvisible)
                {
                    yield return null;
                }

                UnityEngine.Debug.Log("FastSceneLoader: Scene loading completed.");
                yield break;
            }
        }
        [HarmonyPatch(typeof(SpawnSystem), "Awake")]
        class SpawnSystemAwakePatch
        {
            static void Postfix(SpawnSystem __instance)
            {
                // Заменяем InvokeRepeating на более редкие обновления
                var plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["vaffle.FramePerSecondPlus"].Instance as FramePerSecondPlus;
                __instance.CancelInvoke("UpdateSpawning");
                __instance.InvokeRepeating("UpdateSpawning", 10f, spawnUpdateInterval.Value);
            }
        }


        [HarmonyPatch(typeof(SpawnPrefab), "Start")]
        class SpawnPrefabStartPatch
        {
            static bool Prefix(SpawnPrefab __instance)
            {
                // Кэшируем ZNetView
                __instance.m_nview = __instance.GetComponentInParent<ZNetView>();
                if (__instance.m_nview == null)
                {

                    if (enableLogging.Value)
                    {
                        ZLog.LogWarning("SpawnerPrefab cant find netview " + __instance.gameObject.name);
                    }
                    return false; // Пропускаем оригинальный Start
                }

                // Запускаем оптимизированный спавн
                float delay = spawnDelay.Value;
                if (delay <= 0f)
                {
                    OptimizedTrySpawn(__instance);
                }
                else
                {
                    __instance.StartCoroutine(DelayedSpawn(__instance, delay));
                }

                return false; // Пропускаем оригинальный Start
            }

            static IEnumerator DelayedSpawn(SpawnPrefab instance, float delay)
            {
                yield return new WaitForSeconds(delay);
                OptimizedTrySpawn(instance);
            }

            static void OptimizedTrySpawn(SpawnPrefab instance)
            {
                if (!instance.m_nview.IsValid() || !instance.m_nview.IsOwner())
                {
                    return;
                }

                string name = "HasSpawned_" + instance.gameObject.name;
                ZDO zdo = instance.m_nview.GetZDO();
                if (!zdo.GetBool(name, false))
                {

                    if (enableLogging.Value)
                    {
                        ZLog.Log($"SpawnPrefab {instance.gameObject.name} SPAWNING {instance.m_prefab.name}");
                    }

                    // Используем пул объектов (если настроен) или Instantiate
                    GameObject spawnedObject = UnityEngine.Object.Instantiate<GameObject>(
                        instance.m_prefab,
                        instance.transform.position,
                        instance.transform.rotation
                    );
                    zdo.Set(name, true);
                }
            }
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocationsTimeSliced), new Type[0])]
        class GenerateLocationsTimeSlicedPatch
        {
            static bool Prefix(ZoneSystem __instance, ref IEnumerator __result)
            {

                float timeBudget = generationTimeBudget.Value;
                int maxAttempts = maxLocationAttempts.Value;
                bool multithreading = enableMultithreading.Value;
                bool logging = enableLogging.Value;

                if (logging)
                    ZLog.Log("Applying optimized GenerateLocationsTimeSliced patch.");

                __result = OptimizedGenerateLocationsTimeSliced(__instance, timeBudget, maxAttempts, multithreading, logging);
                return false; // Skip original method
            }

            static IEnumerator OptimizedGenerateLocationsTimeSliced(ZoneSystem instance, float timeBudget, int maxAttempts, bool multithreading, bool logging)
            {
                instance.m_estimatedGenerateLocationsCompletionTime = DateTime.MaxValue;
                yield return null;

                LoadingIndicator.SetProgress(0f);
                LoadingIndicator.SetProgressVisibility(true);
                LoadingIndicator.SetText("$menu_generating");

                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                DateTime startTime = DateTime.UtcNow;
                instance.ClearNonPlacedLocations();

                var ordered = (from a in instance.m_locations
                               orderby a.m_prioritized descending
                               select a).ToList();
                ordered.RemoveAll(l => !l.m_enable || l.m_quantity == 0);

                ConcurrentBag<Task<int>> locationTasks = new ConcurrentBag<Task<int>>();
                ConcurrentDictionary<Vector2i, ZoneSystem.LocationInstance> newLocations = new ConcurrentDictionary<Vector2i, ZoneSystem.LocationInstance>();

                for (int i = 0; i < ordered.Count; i++)
                {
                    var location = ordered[i];
                    if (multithreading)
                    {
                        locationTasks.Add(Task.Run(() => ProcessLocation(instance, location, maxAttempts, WorldGenerator.instance.GetSeed(), newLocations, logging)));
                    }
                    else
                    {
                        int iterations = ProcessLocation(instance, location, maxAttempts, WorldGenerator.instance.GetSeed(), newLocations, logging);
                    }

                    if (stopwatch.Elapsed.TotalSeconds >= timeBudget)
                    {
                        yield return null;
                        stopwatch.Restart();
                    }

                    LoadingIndicator.SetProgress((float)(i + 1) / ordered.Count);
                }

                if (multithreading)
                {
                    while (!locationTasks.IsEmpty)
                    {
                        yield return null;
                        stopwatch.Restart();
                    }
                }

                foreach (var kvp in newLocations)
                {
                    instance.m_locationInstances[kvp.Key] = kvp.Value;
                }

                LoadingIndicator.SetProgress(1f);
                LoadingIndicator.SetProgressVisibility(false);
                instance.LocationsGenerated = true;

                if (logging)
                    ZLog.Log($"Done generating locations, duration: {(DateTime.UtcNow - startTime).TotalMilliseconds} ms");
            }

            static int ProcessLocation(ZoneSystem instance, ZoneSystem.ZoneLocation location, int maxAttempts, int seed, ConcurrentDictionary<Vector2i, ZoneSystem.LocationInstance> newLocations, bool logging)
            {
                int iterations = 0;
                int placed = instance.CountNrOfLocation(location);
                if (location.m_unique && placed > 0)
                    return iterations;

                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState(seed + location.m_prefab.Name.GetStableHashCode());

                int errorLocationInZone = 0, errorCenterDistance = 0, errorBiome = 0, errorBiomeArea = 0;
                int errorAlt = 0, errorForest = 0, errorSimilar = 0, errorNotSimilar = 0, errorTerrainDelta = 0, errorVegetation = 0;

                float maxRange = 10000f;
                if (location.m_centerFirst)
                    maxRange = location.m_minDistance;

                for (int i = 0; i < maxAttempts && placed < location.m_quantity; i++)
                {
                    Vector2i zoneID = ZoneSystem.GetRandomZone(maxRange);
                    if (location.m_centerFirst)
                        maxRange += 1f;

                    if (instance.m_locationInstances.ContainsKey(zoneID) || newLocations.ContainsKey(zoneID))
                    {
                        errorLocationInZone++;
                        continue;
                    }

                    if (!instance.IsZoneGenerated(zoneID))
                    {
                        Vector3 zonePos = ZoneSystem.GetZonePos(zoneID);
                        Heightmap.BiomeArea biomeArea = WorldGenerator.instance.GetBiomeArea(zonePos);
                        if ((location.m_biomeArea & biomeArea) == 0)
                        {
                            errorBiomeArea++;
                            continue;
                        }

                        for (int j = 0; j < 20; j++)
                        {
                            iterations++;
                            Vector3 point = ZoneSystem.GetRandomPointInZone(zoneID, Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius));
                            float magnitude = point.magnitude;

                            if ((location.m_minDistance != 0f && magnitude < location.m_minDistance) ||
                                (location.m_maxDistance != 0f && magnitude > location.m_maxDistance))
                            {
                                errorCenterDistance++;
                                continue;
                            }

                            Heightmap.Biome biome = WorldGenerator.instance.GetBiome(point);
                            if ((location.m_biome & biome) == Heightmap.Biome.None)
                            {
                                errorBiome++;
                                continue;
                            }

                            Color color;
                            point.y = WorldGenerator.instance.GetHeight(point.x, point.z, out color);
                            float altitude = point.y - 30f;
                            if (altitude < location.m_minAltitude || altitude > location.m_maxAltitude)
                            {
                                errorAlt++;
                                continue;
                            }

                            if (location.m_inForest)
                            {
                                float forestFactor = WorldGenerator.instance.GetForestHeight(point.x, point.y);
                                if (forestFactor < location.m_forestTresholdMin || forestFactor > location.m_forestTresholdMax)
                                {
                                    errorForest++;
                                    continue;
                                }
                            }

                            float delta;
                            Vector3 slope;
                            WorldGenerator.instance.GetTerrainDelta(point, location.m_exteriorRadius, out delta, out slope);
                            if (delta > location.m_maxTerrainDelta || delta < location.m_minTerrainDelta)
                            {
                                errorTerrainDelta++;
                                continue;
                            }

                            if (location.m_minDistanceFromSimilar > 0f && instance.HaveLocationInRange(location.m_prefab.Name, location.m_group, point, location.m_minDistanceFromSimilar, false))
                            {
                                errorSimilar++;
                                continue;
                            }

                            if (location.m_maxDistanceFromSimilar > 0f && !instance.HaveLocationInRange(location.m_prefabName, location.m_groupMax, point, location.m_maxDistanceFromSimilar, true))
                            {
                                errorNotSimilar++;
                                continue;
                            }

                            float vegetation = color.a;
                            if (location.m_minimumVegetation > 0f && vegetation <= location.m_minimumVegetation)
                            {
                                errorVegetation++;
                                continue;
                            }

                            if (location.m_maximumVegetation < 1f && vegetation >= location.m_maximumVegetation)
                            {
                                errorVegetation++;
                                continue;
                            }

                            if (location.m_surroundCheckVegetation)
                            {
                                float totalVegetation = 0f;
                                for (int k = 0; k < location.m_surroundCheckLayers; k++)
                                {
                                    float distance = (k + 1f) / location.m_surroundCheckLayers * location.m_surroundCheckDistance;
                                    for (int l = 0; l < 6; l++)
                                    {
                                        float angle = l / 6f * Mathf.PI * 2f;
                                        Vector3 samplePoint = point + new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
                                        Color sampleColor;
                                        WorldGenerator.instance.GetHeight(samplePoint.x, samplePoint.z, out sampleColor);
                                        float weight = (location.m_surroundCheckDistance - distance) / (location.m_surroundCheckDistance * 2f);
                                        totalVegetation += sampleColor.a * weight;
                                    }
                                }

                                instance.s_tempVeg.Add(totalVegetation);
                                if (instance.s_tempVeg.Count < 10)
                                    continue;

                                float max = instance.s_tempVeg.Max();
                                float avg = instance.s_tempVeg.Average();
                                float threshold = avg + (max - avg) * location.m_surroundBetterThanAverage;
                                if (totalVegetation < threshold)
                                    continue;
                            }

                            ZoneSystem.LocationInstance instanceData = new ZoneSystem.LocationInstance
                            {
                                m_location = location,
                                m_position = point,
                                m_placed = false
                            };
                            newLocations.TryAdd(zoneID, instanceData);
                            placed++;
                            break;
                        }
                    }
                }

                if (logging && placed < location.m_quantity)
                {
                    ZLog.LogWarning($"Failed to place all {location.m_prefab.Name}, placed {placed}/{location.m_quantity}");
                    ZLog.DevLog($"Errors: LocationInZone={errorLocationInZone}, CenterDistance={errorCenterDistance}, Biome={errorBiome}, BiomeArea={errorBiomeArea}, Alt={errorAlt}, Forest={errorForest}, Similar={errorSimilar}, NotSimilar={errorNotSimilar}, TerrainDelta={errorTerrainDelta}, Vegetation={errorVegetation}");
                }

                UnityEngine.Random.state = state;
                return iterations;
            }
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.PlaceVegetation))]
        class PlaceVegetationPatch
        {
            static bool Prefix(ZoneSystem __instance, Vector2i zoneID, Vector3 zoneCenterPos, Transform parent, Heightmap hmap, List<ZoneSystem.ClearArea> clearAreas, ZoneSystem.SpawnMode mode, List<GameObject> spawnedObjects)
            {
                bool caching = enableHeightmapCaching.Value;
                bool logging = enableLogging.Value;

                UnityEngine.Random.State state = UnityEngine.Random.state;
                int seed = WorldGenerator.instance.GetSeed();
                ConcurrentDictionary<Vector3, (float height, Heightmap.Biome biome, Heightmap.BiomeArea area)> heightCache = caching ? new ConcurrentDictionary<Vector3, (float, Heightmap.Biome, Heightmap.BiomeArea)>() : null;

                foreach (ZoneSystem.ZoneVegetation veg in __instance.m_vegetation)
                {
                    if (!veg.m_enable || !hmap.HaveBiome(veg.m_biome))
                        continue;

                    UnityEngine.Random.InitState(seed + zoneID.x * 4271 + zoneID.y * 9187 + veg.m_prefab.name.GetStableHashCode());
                    int count = UnityEngine.Random.Range((int)veg.m_min, (int)veg.m_max + 1);
                    if (veg.m_max < 1f && UnityEngine.Random.value > veg.m_max)
                        continue;

                    bool hasZNetView = veg.m_prefab.GetComponent<ZNetView>() != null;
                    float minCosTilt = Mathf.Cos(Mathf.Deg2Rad * veg.m_maxTilt);
                    float maxCosTilt = Mathf.Cos(Mathf.Deg2Rad * veg.m_minTilt);
                    float maxRadius = 32f - veg.m_groupRadius;

                    int placed = 0;
                    int maxTries = veg.m_forcePlacement ? (count * 50) : count;

                    for (int i = 0; i < maxTries && placed < count; i++)
                    {
                        Vector3 groupCenter = new Vector3(
                            UnityEngine.Random.Range(zoneCenterPos.x - maxRadius, zoneCenterPos.x + maxRadius),
                            0f,
                            UnityEngine.Random.Range(zoneCenterPos.z - maxRadius, zoneCenterPos.z + maxRadius)
                        );
                        int groupSize = UnityEngine.Random.Range(veg.m_groupSizeMin, veg.m_groupSizeMax + 1);
                        bool placedInGroup = false;

                        for (int j = 0; j < groupSize; j++)
                        {
                            Vector3 pos = j == 0 ? groupCenter : __instance.GetRandomPointInRadius(groupCenter, veg.m_groupRadius);
                            if (veg.m_blockCheck && __instance.IsBlocked(pos))
                                continue;

                            Vector3 normal;
                            Heightmap.Biome biome;
                            Heightmap.BiomeArea biomeArea;
                            Heightmap heightmap;

                            if (caching && heightCache.TryGetValue(pos, out var cached))
                            {
                                pos.y = cached.height;
                                biome = cached.biome;
                                biomeArea = cached.area;
                                heightmap = hmap;
                                normal = Vector3.up; // Approximate, refine if needed
                            }
                            else
                            {
                                __instance.GetGroundData(ref pos, out normal, out biome, out biomeArea, out heightmap);
                                if (caching)
                                    heightCache.TryAdd(pos, (pos.y, biome, biomeArea));
                            }

                            if ((veg.m_biome & biome) == 0 || (veg.m_biomeArea & biomeArea) == 0)
                                continue;

                            float altitude = pos.y - 30f;
                            if (altitude < veg.m_minAltitude || altitude > veg.m_maxAltitude)
                                continue;

                            if (veg.m_minVegetation != veg.m_maxVegetation)
                            {
                                float vegetation = heightmap.GetVegetationMask(pos);
                                if (vegetation < veg.m_minVegetation || vegetation > veg.m_maxVegetation)
                                    continue;
                            }

                            if (veg.m_minOceanDepth != veg.m_maxOceanDepth)
                            {
                                float oceanDepth = heightmap.GetOceanDepth(pos);
                                if (oceanDepth < veg.m_minOceanDepth || oceanDepth > veg.m_maxOceanDepth)
                                    continue;
                            }

                            if (normal.y < minCosTilt || normal.y > maxCosTilt)
                                continue;

                            if (veg.m_terrainDeltaRadius > 0f)
                            {
                                float delta;
                                Vector3 slope;
                                __instance.GetTerrainDelta(pos, veg.m_terrainDeltaRadius, out delta, out slope);
                                if (delta < veg.m_minTerrainDelta || delta > veg.m_maxTerrainDelta)
                                    continue;
                            }

                            if (veg.m_inForest)
                            {
                                float forestFactor = WorldGenerator.instance.GetForestHeight(pos.x, pos.y);
                                if (forestFactor < veg.m_forestTresholdMin || forestFactor > veg.m_forestTresholdMax)
                                    continue;
                            }

                            if (veg.m_surroundCheckVegetation)
                            {
                                float totalVegetation = 0f;
                                int samples = 4; // Reduced from 6 for performance
                                for (int k = 0; k < veg.m_surroundCheckLayers; k++)
                                {
                                    float distance = (k + 1f) / veg.m_surroundCheckLayers * veg.m_surroundCheckDistance;
                                    for (int l = 0; l < samples; l++)
                                    {
                                        float angle = l / (float)samples * Mathf.PI * 2f;
                                        Vector3 samplePos = pos + new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
                                        float vegetation = heightmap.GetVegetationMask(samplePos);
                                        float weight = (veg.m_surroundCheckDistance - distance) / (veg.m_surroundCheckDistance * 2f);
                                        totalVegetation += vegetation * weight;
                                    }
                                }

                                __instance.s_tempVeg.Add(totalVegetation);
                                if (__instance.s_tempVeg.Count < 5) // Reduced from 10
                                    continue;

                                float max = __instance.s_tempVeg.Max();
                                float avg = __instance.s_tempVeg.Average();
                                float threshold = avg + (max - avg) * veg.m_surroundBetterThanAverage;
                                if (totalVegetation < threshold)
                                    continue;
                            }

                            if (__instance.InsideClearArea(clearAreas, pos))
                                continue;

                            float scale = UnityEngine.Random.Range(veg.m_scaleMin, veg.m_scaleMax);
                            float yRot = UnityEngine.Random.Range(0f, 360f);
                            float xTilt = UnityEngine.Random.Range(-veg.m_randTilt, veg.m_randTilt);
                            float zTilt = UnityEngine.Random.Range(-veg.m_randTilt, veg.m_randTilt);

                            if (veg.m_snapToWater)
                                pos.y = 30f;
                            pos.y += veg.m_groundOffset;

                            Quaternion rotation = veg.m_chanceToUseGroundTilt > 0f && UnityEngine.Random.value <= veg.m_chanceToUseGroundTilt
                                ? Quaternion.LookRotation(Vector3.Cross(normal, Quaternion.Euler(0f, yRot, 0f) * Vector3.forward), normal)
                                : Quaternion.Euler(xTilt, yRot, zTilt);

                            if (hasZNetView && (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost))
                            {
                                if (mode == ZoneSystem.SpawnMode.Ghost)
                                    ZNetView.StartGhostInit();

                                GameObject obj = UnityEngine.Object.Instantiate<GameObject>(veg.m_prefab, pos, rotation);
                                ZNetView view = obj.GetComponent<ZNetView>();
                                view.SetLocalScale(new Vector3(scale, scale, scale));

                                if (mode == ZoneSystem.SpawnMode.Ghost)
                                {
                                    spawnedObjects.Add(obj);
                                    ZNetView.FinishGhostInit();
                                }
                            }
                            else
                            {
                                GameObject obj = UnityEngine.Object.Instantiate<GameObject>(veg.m_prefab, pos, rotation);
                                obj.transform.localScale = new Vector3(scale, scale, scale);
                                obj.transform.SetParent(parent, true);
                            }

                            placedInGroup = true;
                        }

                        if (placedInGroup)
                            placed++;
                    }
                    UnityEngine.Random.state = state;
                    if (logging)
                        ZLog.Log($"Placed {placed} vegetation objects in zone {zoneID}");
                }



                return false; // Skip original method
            }
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GetGroundHeight), new[] { typeof(Vector3) })]
        class GetGroundHeightPatch
        {
            static bool Prefix(ZoneSystem __instance, Vector3 p, ref float __result)
            {

                bool caching = enableHeightmapCaching.Value;

                if (caching && heightCache.TryGetValue(p, out float height))
                {
                    __result = height;
                    return false;
                }

                p.y = 6000f;
                RaycastHit hit;
                if (Physics.Raycast(p, Vector3.down, out hit, 7000f, __instance.m_terrainRayMask)) // Reduced from 10000f
                {
                    __result = hit.point.y;
                    if (caching)
                        heightCache[p] = hit.point.y;
                    return false;
                }

                __result = p.y;
                return false;
            }

            static readonly ConcurrentDictionary<Vector3, float> heightCache = new ConcurrentDictionary<Vector3, float>();
        }






        //Настройка прорисовки травы. Удалить со следующим патчем 1.4+
        /*
        [HarmonyPatch(typeof(Terminal), "InputText")]
        private static class InputText_Patch
        {
            private static bool Prefix(Terminal __instance)
            {
                string text = __instance.m_input.text;
                if (text.Equals("gr scalep"))
                    ClutterSystem.instance.m_amountScale++;
                else if (text.Equals("gr scalem"))
                    ClutterSystem.instance.m_amountScale--;
                else if (text.Equals("gr distancep"))
                    ClutterSystem.instance.m_distance++;
                else if (text.Equals("gr distancem"))
                    ClutterSystem.instance.m_distance--;
                else if (text.Equals("gr sizep"))
                    ClutterSystem.instance.m_grassPatchSize++;
                else if (text.Equals("gr sizem"))
                    ClutterSystem.instance.m_grassPatchSize--;
                else if (text.Equals("gr playerp"))
                    ClutterSystem.instance.m_playerPushFade++;
                else if (text.Equals("gr playerm"))
                    ClutterSystem.instance.m_playerPushFade--;
                else if (text.Equals("gr info"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[]
                        {
                            "Values:" +"\n"+ ClutterSystem.instance.m_amountScale + "\n" + ClutterSystem.instance.m_distance + "\n" + ClutterSystem.instance.m_grassPatchSize + "\n" +  ClutterSystem.instance.m_playerPushFade
                        }).GetValue();
                }
                else
                    return true;
                return false;

            }
        */


        [HarmonyPatch(typeof(LightFlicker), "CustomUpdate")]
        private class LightFlicker_Update_Patch : MonoBehaviour
        {
            private static bool Prefix(LightFlicker __instance)
            {
                if (!__instance.m_light)
                {
                    return false;
                }

                if (Settings.ReduceFlashingLights)
                {
                    if (__instance.m_flashingLightsSetting == LightFlashSettings.Off)
                    {
                        __instance.m_light.intensity = 0f;
                        return false;
                    }

                    if (__instance.m_flashingLightsSetting == LightFlashSettings.AlwaysOn)
                    {
                        __instance.m_light.intensity = 1f;
                        return false;
                    }
                }
                __instance.m_light.intensity = __instance.m_baseIntensity;
                return false;
            }
        }

        [HarmonyPatch(typeof(SceneLoader), "Start")]
        private class SceneLoaderOff
        {
            unsafe static void Prefix(SceneLoader __instance)
            {
                __instance._showLogos = !skipIntro.Value;
            }
        }
        [HarmonyPatch(typeof(Player), "Start")]
        private class BeforeSpawnPlayer
        {
            unsafe static void Prefix(Player __instance)
            {
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.softParticles = false;
                QualitySettings.shadowCascades = 0;
                QualitySettings.shadowNearPlaneOffset = 0;
                QualitySettings.particleRaycastBudget = particleRaycastBudget.Value;
                QualitySettings.softVegetation = false;

                if (shadowDistanceToggle.Value)
                    QualitySettings.shadowDistance = shadowDistance.Value;
                if (terrainTreeDistanceToggle.Value)
                    QualitySettings.terrainTreeDistance = terrainTreeDistance.Value;
                //QualitySettings.shadowResolution = ShadowResolution.Low;

            }
        }

        [HarmonyPatch(typeof(Smoke), "CustomUpdate")]
        private class SlowUpdaterFix
        {
            private static bool Prefix(Smoke __instance, float deltaTime, float time)
            {

                // Ранний выход, если объект в процессе удаления
                if (__instance.m_fadeTimer >= 0f)
                {
                    __instance.m_fadeTimer += deltaTime;
                    if (__instance.m_fadeTimer >= __instance.m_fadetime)
                    {
                        UnityEngine.Object.Destroy(__instance.gameObject);
                    }
                    return false; 
                }

                __instance.m_time += deltaTime;

                // Проверка на необходимость начать исчезновение
                if (__instance.m_time > __instance.m_ttl)
                {
                    __instance.StartFadeOut();
                    return false;
                }

                float num = 1f - (__instance.m_time / __instance.m_ttl);
                float mass = num * num;
                __instance.m_body.mass = mass;

                Vector3 velocity = __instance.m_body.velocity;
                Vector3 vel = __instance.m_vel;
                vel.y *= num;

                Vector3 force = (vel - velocity) * (__instance.m_force * deltaTime);
                __instance.m_body.AddForce(force, ForceMode.VelocityChange);
                return false;
            }

        }

    }
}
