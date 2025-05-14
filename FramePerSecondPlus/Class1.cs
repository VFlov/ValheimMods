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


namespace FramePerSecondPlus
{
    [BepInPlugin("vaffle.FramePerSecondPlus", "FramePerSecondPlus", "1.0.1")]
    public class FramePerSecondPlus : BaseUnityPlugin
    {
        private static ConfigEntry<float> spawnUpdateInterval;
        private static ConfigEntry<bool> enableMultithreading;
        private static ConfigEntry<float> spawnRadiusMin;
        private static ConfigEntry<float> spawnRadiusMax;
        private static ConfigEntry<bool> enableLogging;
        private static ConfigEntry<float> spawnDelay;
        private static ConfigEntry<float> updateInterval;
        private static ConfigEntry<float> lakeGridStep;
        private static ConfigEntry<int> maxLakeIterations;



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
            spawnRadiusMin = Config.Bind("General", "SpawnRadiusMin", 40f, "Minimum spawn radius");
            spawnRadiusMax = Config.Bind("General", "SpawnRadiusMax", 80f, "Maximum spawn radius");
            enableLogging = Config.Bind("General", "EnableLogging", false, "Enable logging for spawn events");
            spawnDelay = Config.Bind("General", "SpawnDelay", 1f, "Delay before spawning in seconds (0 for immediate spawn)");
            updateInterval = Config.Bind("General", "UpdateInterval", 0.1f, "Interval for environment updates in seconds");
            lakeGridStep = Config.Bind("General", "LakeGridStep", 256f, "Grid step for lake generation (higher = faster, lower = more precise)");
            maxLakeIterations = Config.Bind("General", "MaxLakeIterations", 1000, "Maximum iterations for lake point generation");

            Logger.LogEvent += OnLogEvent;
            FramePerSecondPlus.Log = base.Logger;
            AddConfiguration();
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.particleRaycastBudget = 1024;
            QualitySettings.softVegetation = false;
            PrefabManager.OnPrefabsRegistered += CustomAwake;
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
        [HarmonyPatch(typeof(ClutterSystem), "Awake")]
        private static class ClutterSystem_Awake_Patch
        {
            private static void Prefix(ClutterSystem __instance)
            {
                __instance.m_grassPatchSize = 20;
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
                Debug.Log($"FastSceneLoader: Starting to load scene: {sceneName}");

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

                Debug.Log("FastSceneLoader: Scene loading completed.");
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

        [HarmonyPatch(typeof(SpawnSystem), "UpdateSpawning")]
        class SpawnSystemUpdatePatch
        {
            static bool Prefix(SpawnSystem __instance)
            {
                if (!__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner() || Player.m_localPlayer == null)
                {
                    return false; // Пропускаем, если нет игрока или ZNetView не валиден
                }

                // Кэшируем список игроков в зоне
                var tempNearPlayers = AccessTools.StaticFieldRefAccess<List<Player>>(typeof(SpawnSystem), "m_tempNearPlayers");
                tempNearPlayers.Clear();
                __instance.GetPlayersInZone(tempNearPlayers);
                if (tempNearPlayers.Count == 0)
                {
                    return false; // Пропускаем, если нет игроков в зоне
                }

                // Запускаем обновление спавна
                DateTime currentTime = ZNet.instance.GetTime();
                var spawnLists = __instance.m_spawnLists;

                // Если включена многопоточность
                var plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["vaffle.FramePerSecondPlus"].Instance as FramePerSecondPlus;
                if (enableMultithreading.Value)
                {
                    Task.Run(() =>
                    {
                        foreach (var spawnSystemList in spawnLists)
                        {
                            OptimizedUpdateSpawnList(__instance, spawnSystemList.m_spawners, currentTime, false);
                        }
                        var currentSpawners = RandEventSystem.instance?.GetCurrentSpawners();
                        if (currentSpawners != null)
                        {
                            OptimizedUpdateSpawnList(__instance, currentSpawners, currentTime, true);
                        }
                    });
                }
                else
                {
                    foreach (var spawnSystemList in spawnLists)
                    {
                        OptimizedUpdateSpawnList(__instance, spawnSystemList.m_spawners, currentTime, false);
                    }
                    var currentSpawners = RandEventSystem.instance?.GetCurrentSpawners();
                    if (currentSpawners != null)
                    {
                        OptimizedUpdateSpawnList(__instance, currentSpawners, currentTime, true);
                    }
                }

                return false; // Пропускаем оригинальный метод
            }

            static void OptimizedUpdateSpawnList(SpawnSystem instance, List<SpawnSystem.SpawnData> spawners, DateTime currentTime, bool eventSpawners)
            {
                var pheromoneList = AccessTools.Field(typeof(SpawnSystem), "m_pheromoneList").GetValue(instance) as List<SE_Stats>;
                pheromoneList.Clear();

                // Кэшируем статусные эффекты с феромонами
                foreach (Player player in Player.GetAllPlayers())
                {
                    foreach (StatusEffect statusEffect in player.GetSEMan().GetStatusEffects())
                    {
                        if (statusEffect is SE_Stats se_Stats && se_Stats.m_pheromoneTarget != null)
                        {
                            pheromoneList.Add(se_Stats);
                        }
                    }
                }

                string str = eventSpawners ? "e_" : "b_";
                int num = 0;

                foreach (var spawnData in spawners)
                {
                    num++;
                    if (!spawnData.m_enabled || !instance.m_heightmap.HaveBiome(spawnData.m_biome))
                    {
                        continue;
                    }

                    // Оптимизированный расчет времени спавна
                    int stableHashCode = (str + spawnData.m_prefab.name + num.ToString()).GetStableHashCode();
                    long lastSpawnTicks = instance.m_nview.GetZDO().GetLong(stableHashCode, 0L);
                    TimeSpan timeSpan = currentTime - new DateTime(lastSpawnTicks);
                    int spawnCount = Mathf.Min(spawnData.m_maxSpawned == 0 ? 1 : spawnData.m_maxSpawned,
                        (int)(timeSpan.TotalSeconds / spawnData.m_spawnInterval));

                    if (spawnCount <= 0)
                    {
                        continue;
                    }

                    instance.m_nview.GetZDO().Set(stableHashCode, currentTime.Ticks);

                    // Оптимизированный поиск точки спавна
                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPoint;
                        Player targetPlayer;

                        if (OptimizedFindBaseSpawnPoint(instance, spawnData,  out spawnPoint, out targetPlayer))
                        {
                            int maxSpawned = spawnData.m_maxSpawned;
                            float spawnChance = spawnData.m_spawnChance;
                            int minLevelOverride = -1;
                            float levelUpMultiplier = 1f;

                            // Применяем феромоны
                            foreach (var se_Stats in pheromoneList)
                            {
                                if (se_Stats.m_pheromoneTarget == spawnData.m_prefab && se_Stats.m_character != null &&
                                    Vector3.Distance(spawnPoint, se_Stats.m_character.transform.position) < 100f)
                                {
                                    if (se_Stats.m_pheromoneSpawnChanceOverride > 0f)
                                        spawnChance = se_Stats.m_pheromoneSpawnChanceOverride;
                                    if (se_Stats.m_pheromoneMaxInstanceOverride > 0)
                                        maxSpawned = se_Stats.m_pheromoneMaxInstanceOverride;
                                    if (se_Stats.m_pheromoneSpawnMinLevel > 0)
                                        minLevelOverride = se_Stats.m_pheromoneSpawnMinLevel;
                                    if (se_Stats.m_pheromoneLevelUpMultiplier != 1f)
                                        levelUpMultiplier *= se_Stats.m_pheromoneLevelUpMultiplier;
                                }
                            }

                            if (UnityEngine.Random.Range(0f, 100f) > spawnChance)
                            {
                                continue;
                            }

                            if (!string.IsNullOrEmpty(spawnData.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(spawnData.m_requiredGlobalKey) ||
                                (spawnData.m_requiredEnvironments.Count > 0 && !EnvMan.instance.IsEnvironment(spawnData.m_requiredEnvironments)) ||
                                (!spawnData.m_spawnAtDay && EnvMan.IsDay()) || (!spawnData.m_spawnAtNight && EnvMan.IsNight()))
                            {
                                continue;
                            }

                            int currentInstances = SpawnSystem.GetNrOfInstances(spawnData.m_prefab, Vector3.zero, 0f, eventSpawners, false);
                            if (maxSpawned > 0 && currentInstances >= maxSpawned)
                            {
                                continue;
                            }

                            int groupSize = Mathf.Min(UnityEngine.Random.Range(spawnData.m_groupSizeMin, spawnData.m_groupSizeMax + 1),
                                maxSpawned > 0 ? (maxSpawned - currentInstances) : 100);
                            float groupRadius = groupSize > 1 ? spawnData.m_groupRadius : 0f;
                            int spawnedCount = 0;

                            for (int j = 0; j < groupSize * 2; j++)
                            {
                                Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
                                Vector3 groupSpawnPoint = spawnPoint + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * groupRadius;
                                if (instance.IsSpawnPointGood(spawnData, ref groupSpawnPoint))
                                {
                                    instance.Spawn(spawnData, groupSpawnPoint + Vector3.up * (spawnData.m_groundOffset + UnityEngine.Random.Range(0f, spawnData.m_groundOffsetRandom)),
                                        eventSpawners, minLevelOverride, levelUpMultiplier);
                                    spawnedCount++;
                                    if (spawnedCount >= groupSize)
                                    {
                                        break;
                                    }
                                }
                            }
                            ZLog.Log($"Spawned {spawnData.m_prefab.name} x {spawnedCount}");
                        }
                    }
                }
            }

            static bool OptimizedFindBaseSpawnPoint(SpawnSystem instance, SpawnSystem.SpawnData spawn,  out Vector3 spawnCenter, out Player targetPlayer)
            {
                var tempNearPlayers = AccessTools.StaticFieldRefAccess<List<Player>>(typeof(SpawnSystem), "m_tempNearPlayers");
                float minInclusive = spawn.m_spawnRadiusMin > 0f ? spawn.m_spawnRadiusMin : spawnRadiusMin.Value;
                float maxInclusive = spawn.m_spawnRadiusMax > 0f ? spawn.m_spawnRadiusMax : spawnRadiusMax.Value;

                // Уменьшаем количество попыток для оптимизации
                for (int i = 0; i < 10; i++) // Снижено с 20 до 10
                {
                    Player player = tempNearPlayers[UnityEngine.Random.Range(0, tempNearPlayers.Count)];
                    Vector3 direction = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward;
                    Vector3 point = player.transform.position + direction * UnityEngine.Random.Range(minInclusive, maxInclusive);
                    if (instance.IsSpawnPointGood(spawn, ref point))
                    {
                        spawnCenter = point;
                        targetPlayer = player;
                        return true;
                    }
                }

                spawnCenter = Vector3.zero;
                targetPlayer = null;
                return false;
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
                    return false; // Дальнейшие вычисления не нужны
                }

                __instance.m_time += deltaTime;

                // Проверка на необходимость начать исчезновение
                if (__instance.m_time > __instance.m_ttl)
                {
                    __instance.StartFadeOut();
                    return false;
                }

                float num = 1f - (__instance.m_time / __instance.m_ttl); // Mathf.Clamp01 не нужен, если m_time гарантированно <= m_ttl
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
