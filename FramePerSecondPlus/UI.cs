/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

using BepInEx;
using static System.Collections.Specialized.BitVector32;
using UnityEngine.InputSystem;
using PlayFab.Internal;
using Jotunn.Utils;
using Mono.Mozilla;

using UnityEngine.SceneManagement;
using System.Threading;


namespace FramePerSecondPlus
{

    partial class FramePerSecondPlus
    {
        static bool gameStartFirstTime = true;
        private static GameObject menuPanel;
        private static GameObject menuOpenButton;
        private static GameObject canvasMain;
        private static GameObject scrollViewModsContent;
        private static GameObject scrollViewConfigsContent;
        static GameObject ModButtonPrefab;
        static GameObject ConfigButtonPrefab;
        static TextMeshProUGUI ModNameText;
        private static Button closeButton;
        private static Button saveButton;
        private static List<ConfigFileData> configFiles = new List<ConfigFileData>();
        private static Dictionary<string, List<GameObject>> configEntryObjects = new Dictionary<string, List<GameObject>>();
        private static string activeConfigFileName;

        private class ConfigFileData
        {
            public string FileName { get; set; }
            public Dictionary<string, List<ConfigEntry>> Sections { get; set; } = new Dictionary<string, List<ConfigEntry>>();
        }

        private class ConfigEntry
        {
            public string Key { get; set; }
            public string Type { get; set; }
            public string DefaultValue { get; set; }
            public string CurrentValue { get; set; }
            public string Description { get; set; }
            public string AcceptableValues { get; set; }
            public InputField InputField { get; set; }
        }



        [HarmonyPatch(typeof(MenuScene))]
        public class MenuScenePatches
        {
            [HarmonyPostfix]
            [HarmonyPatch("Awake")]
            public static void AwakePostfix(MenuScene __instance)
            {
                Debug.Log("MenuScenePatches.AwakePostfix started");

                if (!UnityEngine.Object.FindObjectOfType<EventSystem>())
                {
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<EventSystem>();
                    eventSystemObj.AddComponent<StandaloneInputModule>();
                    Debug.Log("Created new EventSystem");
                }

                try
                {
                    ParseConfigFiles();
                    PopulateModList();
                    Debug.Log("Config files parsed and mod list populated");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in ParseConfigFiles or PopulateModList: {e.Message}");
                }
            }

        }
        [HarmonyPatch(typeof(LoadingIndicator))]
        public class LoadingIndicatorEvent
        {
            [HarmonyPostfix]
            [HarmonyPatch("Awake")]
            public static void LoadingIndicatorPostfix(LoadingIndicator __instance)
            {
                if (canvasMain != null)
                {
                    if (gameStartFirstTime)
                    {
                        gameStartFirstTime = false;
                        return;
                    }
                    UnityEngine.Object.Destroy(canvasMain);
                }
            }
        }

        private void InitializeUIComponents(GameObject canvas)
        {
            canvasMain = canvas;
            Debug.Log("CanvasFPSP children: " + string.Join(", ", canvas.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray()));

            menuPanel = canvas.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "ModsPanel")?.gameObject;
            if (menuPanel == null)
            {
                Logger.LogError("ModsPanel not found in CanvasFPSP!");
                return;
            }
            Debug.Log($"ModsPanel found: {menuPanel.name}, Active: {menuPanel.activeSelf}");

            GameObject scrollViewMods = menuPanel.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "ScrollViewMods")?.gameObject;
            Debug.Log($"ScrollViewMods found: {scrollViewMods != null}");
            if (scrollViewMods == null)
            {
                Logger.LogError("ScrollViewMods not found in CanvasFPSP!");
                return;
            }
            Debug.Log($"ScrollViewMods found: {scrollViewMods.name}, Active: {scrollViewMods.activeSelf}");

            scrollViewModsContent = scrollViewMods.transform.Find("Viewport/Content")?.gameObject;
            if (scrollViewModsContent == null)
            {
                Logger.LogError("ScrollViewMods Content not found!");
                return;
            }

            GameObject scrollViewConfigs = menuPanel.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "ScrollViewConfigs")?.gameObject;
            Debug.Log($"ScrollViewConfigs found: {scrollViewConfigs != null}");
            if (scrollViewConfigs == null)
            {
                Logger.LogError("ScrollViewConfigs not found in CanvasFPSP!");
                return;
            }
            Debug.Log($"ScrollViewConfigs found: {scrollViewConfigs.name}, Active: {scrollViewConfigs.activeSelf}");

            scrollViewConfigsContent = scrollViewConfigs.transform.Find("Viewport/Content")?.gameObject;
            if (scrollViewConfigsContent == null)
            {
                Logger.LogError("ScrollViewConfigs Content not found!");
                return;
            }

            ConfigButtonPrefab = scrollViewConfigsContent.transform.Find("ModConfigEntry")?.gameObject;
            if (ConfigButtonPrefab == null)
            {
                Log.LogError("ModConfigEntry prefab not found in ScrollViewConfigs Content!");
                return;
            }
            ModButtonPrefab = scrollViewModsContent.transform.Find("ModButton")?.gameObject;
            if (ModButtonPrefab == null)
            {
                Log.LogError("ModButton prefab not found in ScrollViewMods Content!");
                return;
            }
            closeButton = canvas.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Close")?.GetComponent<Button>();
            if (closeButton == null)
            {
                Logger.LogError("Close button not found in CanvasFPSP!");
                return;
            }

            saveButton = canvas.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Save")?.GetComponent<Button>();
            if (saveButton == null)
            {
                Logger.LogError("Save button not found in CanvasFPSP!");
                return;
            }
            menuPanel.SetActive(false);
            ModNameText = canvas.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "ModNameText")?.GetComponent<TextMeshProUGUI>();
            if (saveButton == null)
            {
                Logger.LogError("Mod Name Text not found in CanvasFPSP!");
                return;
            }
            Button avatarButton = canvas.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Avatar")?.GetComponent<Button>();
            Button iconButton = canvas.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Icon")?.GetComponent<Button>();
            Button menuButton = canvas.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "ButtonFPSP")?.GetComponent<Button>();
            menuOpenButton = menuButton.gameObject;
            if (menuButton != null)
            {
                menuButton.onClick.RemoveAllListeners();
                menuButton.onClick.AddListener(() =>
                {
                    Debug.Log("Menu button clicked!");
                    menuPanel.SetActive(!menuPanel.activeSelf);
                });
                Debug.Log("Menu button click handler registered");
            }
            else
            {
                Logger.LogWarning("ButtonFPSP not found, menu button functionality skipped.");
            }
            avatarButton.onClick.RemoveAllListeners();
            avatarButton.onClick.AddListener(() =>
            {
                Application.OpenURL("https://discord.com/channels/me/1310563441978773514");
            });
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(() =>
            {
                Application.OpenURL("https://thunderstore.io/c/valheim/p/vaffle1/FPSPlus/");
            });
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                Debug.Log("Close button clicked!");
                menuPanel.SetActive(false);
            });

            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() =>
            {
                Debug.Log("Save button clicked!");
                SaveConfigChanges();
            });
        }

        private static void ParseConfigFiles()
        {

            Debug.Log("Parsing config files...");
            string configPath = Path.Combine(BepInEx.Paths.ConfigPath);
            if (!Directory.Exists(configPath))
            {
                Debug.LogError($"Config path not found: {configPath}");
                return;
            }

            foreach (string file in Directory.GetFiles(configPath, "*.cfg"))
            {

                ConfigFileData configData = new ConfigFileData { FileName = Path.GetFileName(file) };
                string[] lines = File.ReadAllLines(file).Skip(2).ToArray();
                string currentSection = null;
                List<string> descriptionLines = new List<string>();
                Dictionary<string, string> metadata = new Dictionary<string, string>();

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine)) continue;

                    // Обработка секций
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        if (currentSection != null && configData.Sections.ContainsKey(currentSection))
                        {
                            descriptionLines.Clear();
                            metadata.Clear();
                        }
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        configData.Sections[currentSection] = new List<ConfigEntry>();
                        continue;
                    }

                    // Обработка комментариев
                    if (trimmedLine.StartsWith("#"))
                    {
                        string comment = trimmedLine.Substring(1).Trim();
                        if (comment.StartsWith("Setting type:"))
                        {
                            metadata["type"] = comment.Replace("Setting type:", "").Trim();
                        }
                        else if (comment.StartsWith("Default value:"))
                        {
                            metadata["default"] = comment.Replace("Default value:", "").Trim();
                        }
                        else if (comment.StartsWith("Acceptable values:"))
                        {
                            metadata["acceptable"] = comment.Replace("Acceptable values:", "").Trim();
                        }
                        else
                        {
                            descriptionLines.Add(comment);
                        }
                        continue;
                    }

                    // Обработка значений
                    if (trimmedLine.Contains("=") && currentSection != null)
                    {
                        var match = Regex.Match(trimmedLine, @"^([^=]+)\s*=\s*([^=]+)$");
                        if (match.Success)
                        {
                            string key = match.Groups[1].Value.Trim();
                            string value = match.Groups[2].Value.Trim();
                            string type = metadata.ContainsKey("type") ? metadata["type"] : "Unknown";
                            string defaultValue = metadata.ContainsKey("default") ? metadata["default"] : "";
                            string acceptableValues = metadata.ContainsKey("acceptable") ? metadata["acceptable"] : "";
                            string description = string.Join(" ", descriptionLines);

                            configData.Sections[currentSection].Add(new ConfigEntry
                            {
                                Key = key,
                                Type = type,
                                DefaultValue = defaultValue,
                                CurrentValue = value,
                                Description = description,
                                AcceptableValues = acceptableValues
                            });
                            descriptionLines.Clear();
                            metadata.Clear();
                        }
                    }
                }

                if (configData.Sections.Any())
                {
                    configFiles.Add(configData);
                    Debug.Log($"Successfully parsed config file: {configData.FileName}");
                }
                else
                {
                    Debug.LogWarning($"No valid sections found in config file: {configData.FileName}");
                }
            }
            Debug.Log($"Total parsed config files: {configFiles.Count}");
        }

        private static void PopulateModList()
        {


            configEntryObjects.Clear();

            GameObject modButtonPrefab = ModButtonPrefab;

            for (int i = 0; i < configFiles.Count; i++)
            {
                var config = configFiles[i];
                GameObject modButtonObj = UnityEngine.Object.Instantiate(modButtonPrefab, scrollViewModsContent.transform);
                //modButtonObj.SetActive(true);
                modButtonObj.name = $"Mod_{config.FileName}";

                TextMeshProUGUI modNameText = modButtonObj.transform.Find("ModName")?.GetComponent<TextMeshProUGUI>();
                if (modNameText != null)
                {
                    modNameText.text = Path.GetFileNameWithoutExtension(config.FileName);
                }

                Button modButton = modButtonObj.GetComponent<Button>();
                if (modButton != null)
                {
                    modButton.onClick.RemoveAllListeners();
                    int index = i;
                    modButton.onClick.AddListener(() =>
                    {
                        Debug.Log($"Mod {config.FileName} clicked");
                        activeConfigFileName = config.FileName;
                        ModNameText.text = config.FileName;
                        PopulateConfigDetails(config);
                    });
                }
                Image modImage = modButtonObj.GetComponent<Image>();


                if (i == 0)
                {
                    activeConfigFileName = config.FileName;
                    PopulateConfigDetails(config);
                }
            }
            //UnityEngine.Object.Destroy(modButtonPrefab);
            ModButtonPrefab.SetActive(false);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewModsContent.GetComponent<RectTransform>());
        }

        private static void PopulateConfigDetails(ConfigFileData config)
        {

            foreach (Transform child in scrollViewConfigsContent.transform)
            {
                if (child.gameObject != ConfigButtonPrefab)
                    UnityEngine.Object.Destroy(child.gameObject);
            }

            List<GameObject> entryObjects = new List<GameObject>();
            configEntryObjects[config.FileName] = entryObjects;

            GameObject configEntryPrefab = ConfigButtonPrefab;

            foreach (var section in config.Sections)
            {
                foreach (var entry in section.Value)
                {
                    GameObject configEntryObj = UnityEngine.Object.Instantiate(configEntryPrefab, scrollViewConfigsContent.transform);
                    configEntryObj.SetActive(true);
                    configEntryObj.name = $"ConfigEntry_{entry.Key}";

                    // Поле Key
                    TextMeshProUGUI keyText = configEntryObj.transform.Find("Key&Type")?.GetComponent<TextMeshProUGUI>();
                    if (keyText != null)
                    {
                        keyText.text = $"Key: {entry.Key}";
                    }

                    // Поле Type
                    TextMeshProUGUI typeText = configEntryObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
                    if (typeText != null)
                    {
                        //typeText.text = $"Type: {entry.Type}";
                        typeText.text = entry.Description;
                    }

                    // Поле DefaultValue и CurrentValue
                    TextMeshProUGUI defaultValueText = configEntryObj.transform.Find("DefaultValues&CurrentValue")?.GetComponent<TextMeshProUGUI>();
                    if (defaultValueText != null)
                    {
                        defaultValueText.text = $"Default: {entry.DefaultValue}, Current: {entry.CurrentValue}, Type: {entry.Type}";
                    }

                    // Поле ввода InputField
                    TMP_InputField inputField = configEntryObj.transform.Find("InputField")?.GetComponent<TMP_InputField>();
                    if (inputField != null)
                    {
                        inputField.text = entry.CurrentValue;
                        inputField.pointSize = 20;
                        //entry.InputField = inputField;
                    }

                    entryObjects.Add(configEntryObj);
                }
            }

            configEntryPrefab.SetActive(false);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewConfigsContent.GetComponent<RectTransform>());
        }

        private static void SaveConfigChanges()
        {
            ModNameText.text = "While not working";
            ModNameText.color = Color.red;
            return;
            if (string.IsNullOrEmpty(activeConfigFileName))
            {
                Debug.LogError("No config file selected to save!");
                return;
            }

            ConfigFileData config = configFiles.Find(c => c.FileName == activeConfigFileName);
            if (config == null)
            {
                Debug.LogError($"Config file {activeConfigFileName} not found!");
                return;
            }

            string configPath = Path.Combine(BepInEx.Paths.ConfigPath, config.FileName);
            if (!File.Exists(configPath))
            {
                Debug.LogError($"Config file path {configPath} does not exist!");
                return;
            }

            StringBuilder configContent = new StringBuilder();
            string[] lines = File.ReadAllLines(configPath);
            string currentSection = null;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    configContent.AppendLine(line);
                    continue;
                }

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    configContent.AppendLine(line);
                    continue;
                }

                if (trimmedLine.Contains("=") && currentSection != null)
                {
                    var match = Regex.Match(trimmedLine, @"^([^=]+)\s*=\s*([^=]+)$");
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value.Trim();
                        ConfigEntry entry = config.Sections[currentSection].Find(e => e.Key == key);
                        if (entry != null && entry.InputField != null)
                        {
                            string newValue = entry.InputField.text;
                            entry.CurrentValue = newValue;
                            // Сохраняем оригинальное форматирование строки, заменяя только значение после =
                            Log.LogWarning(newValue);
                            string updatedLine = line.Substring(0, line.IndexOf('=') + 1) + " " + newValue;
                            Log.LogWarning(updatedLine);
                            configContent.AppendLine(updatedLine);
                        }
                        else
                        {
                            configContent.AppendLine(line);
                        }
                    }
                    else
                    {
                        configContent.AppendLine(line);
                    }
                }
                else
                {
                    configContent.AppendLine(line);
                }
            }

            File.WriteAllText(configPath, configContent.ToString());
            Debug.Log($"Config file {configPath} saved successfully");

            PopulateConfigDetails(config);



        }
    }
}
*/