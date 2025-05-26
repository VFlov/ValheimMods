using UnityEngine;
using UnityEngine.Networking;
using Jotunn.Utils;
using System.IO;
using System.Collections;
using BepInEx;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Experimental.Audio;
using Accessibility;

namespace Tailwind
{
    [BepInPlugin("vaffle.Tailwind", "Tailwind", "1.0.2")]
    public class Class1 : BaseUnityPlugin
    {
        private static AudioSource audioSource;
        private static bool isPlaying = false;
        private static string musicFolderPath = Path.Combine(BepInEx.Paths.PluginPath, "Shanty"); // Папка с .mp3 файлами
        private static GameObject uiPanel; // Ссылка на префаб Tailwind
        private static TextMeshProUGUI musicNameText; // Текст для названия трека
        private static Button skipButton; // Кнопка Skip
        private static Button pauseButton; // Кнопка Pause
        private static string[] mp3Files; // Список всех .mp3 файлов

        private void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            // Загружаем префаб UI
        }

        [HarmonyPatch(typeof(Player), "StartDoodadControl")]
        private static class AddMusic_Ship
        {
            private static void Postfix(Player __instance, IDoodadController shipControl)
            {
                if (__instance == null)
                    return;
                
                LoadUIPrefab();
                var ship = Ship.GetLocalShip();
                if (ship == null)
                    return;
                var musicPiece = ship.transform.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "InWaterSounds")?
                    .gameObject.transform.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "Decksound").gameObject;

                if (musicPiece == null)
                {
                    Jotunn.Logger.LogWarning("Music piece (Decksound) not found!");
                    return;
                }

                if (audioSource == null)
                {
                    audioSource = musicPiece.AddComponent<AudioSource>();
                    Jotunn.Logger.LogWarning("AudioSource added");
                }
                else
                {
                    Jotunn.Logger.LogWarning("AudioSource already exists");
                }

                MonoBehaviour instance = __instance as MonoBehaviour;
                instance.StartCoroutine(LoadAndPlayMusicFromFolder());
            }

            private static IEnumerator LoadAndPlayMusicFromFolder()
            {
                if (!Directory.Exists(musicFolderPath))
                {
                    Jotunn.Logger.LogError("Music folder not found at: " + musicFolderPath);
                    yield break;
                }

                mp3Files = Directory.GetFiles(musicFolderPath, "*.mp3");
                Jotunn.Logger.LogWarning($"Found {mp3Files.Length} .mp3 files");

                if (mp3Files.Length == 0)
                {
                    Jotunn.Logger.LogError("No .mp3 files found in " + musicFolderPath);
                    yield break;
                }

                string randomMp3Path = mp3Files[UnityEngine.Random.Range(0, mp3Files.Length)];
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + randomMp3Path, AudioType.MPEG))
                {
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        clip.name = Path.GetFileNameWithoutExtension(randomMp3Path);
                        audioSource.clip = clip;
                        audioSource.Play();
                        isPlaying = true;
                        UpdateMusicNameDisplay(clip.name); // Обновляем название трека
                        Jotunn.Logger.LogWarning("Now playing: " + clip.name);
                    }
                    else
                    {
                        Jotunn.Logger.LogError("Failed to load audio clip: " + www.error);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Player), "StopDoodadControl")]
        private static class RemoveUI_ShipEffects
        {
            private static void Postfix(Player __instance)
            {
                if (__instance == null)
                    return;
                if (uiPanel == null)
                    return;
                if (audioSource == null)
                    return;
                Destroy(uiPanel);
                audioSource.Stop();
                Destroy(audioSource);
            }
        }
        private void Update()
        {
            // Обработка нажатий клавиш
            if (Input.GetKeyDown(KeyCode.F3) && audioSource != null)
            {
                if (isPlaying)
                {
                    PauseMusic();
                    Jotunn.Logger.LogWarning("Music paused with F3");
                }
                else
                {
                    ResumeMusic();
                }
            }

            if (Input.GetKeyDown(KeyCode.F4) && audioSource != null)
            {
                SwitchTrack();
                Jotunn.Logger.LogWarning("Track switched with F4");
            }
        }

        private static void LoadUIPrefab()
        {
            AssetBundle bundle = AssetUtils.LoadAssetBundleFromResources("tailwind", Assembly.GetExecutingAssembly());
            if (bundle == null)
            {
                Jotunn.Logger.LogError("Failed to load tailwindbundle AssetBundle!");
                return;
            }

            GameObject prefab = bundle.LoadAsset<GameObject>("Tailwind");
            if (prefab == null)
            {
                Jotunn.Logger.LogError("Tailwind prefab not found in AssetBundle!");
                bundle.Unload(false);
                return;
            }

            uiPanel = Instantiate(prefab);
            uiPanel.name = "TailwindUI";
            DontDestroyOnLoad(uiPanel); // Сохранение UI при смене сцен

            
            musicNameText = uiPanel.transform.Find("MusicNameBackground/MusicName")?.GetComponent<TextMeshProUGUI>();
            skipButton = uiPanel.transform.Find("SkipButton")?.GetComponent<Button>();
            pauseButton = uiPanel.transform.Find("PauseButton")?.GetComponent<Button>();

            if (musicNameText == null) Jotunn.Logger.LogWarning("MusicName text not found!");
            if (skipButton == null) Jotunn.Logger.LogWarning("SkipButton not found!");
            if (pauseButton == null) Jotunn.Logger.LogWarning("PauseButton not found!");

            
            if (skipButton != null)
                skipButton.onClick.AddListener(SwitchTrack);
            if (pauseButton != null)
                pauseButton.onClick.AddListener(PauseMusic);

            bundle.Unload(false);
        }

        private static void UpdateMusicNameDisplay(string trackName)
        {
            if (musicNameText != null)
            {
                
                musicNameText.text = trackName;
                musicNameText.alpha = 1f; // Полная видимость
                MonoBehaviour instance = FindObjectOfType<MonoBehaviour>();
                instance.StartCoroutine(FadeOutText(musicNameText, 3f)); // 3 секунды
            }
        }

        private static IEnumerator FadeOutText(TextMeshProUGUI text, float duration)
        {
            float elapsedTime = 0f;
            Color originalColor = text.color;
            originalColor.a = 1f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = 1f - (elapsedTime / duration);
                text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f); // Полное исчезновение
        }

        private static void SwitchTrack()
        {
            if (mp3Files == null || mp3Files.Length == 0)
            {
                Jotunn.Logger.LogError("No tracks available to switch!");
                return;
            }

            string currentPath = audioSource.clip != null ? audioSource.clip.name : "";
            string newMp3Path;
            do
            {
                newMp3Path = mp3Files[UnityEngine.Random.Range(0, mp3Files.Length)];
            } while (Path.GetFileNameWithoutExtension(newMp3Path) == currentPath && mp3Files.Length > 1);

            MonoBehaviour instance = FindObjectOfType<MonoBehaviour>();
            instance.StartCoroutine(LoadAndSwitchTrack(newMp3Path));
        }

        private static System.Collections.IEnumerator LoadAndSwitchTrack(string mp3Path)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + mp3Path, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    clip.name = Path.GetFileNameWithoutExtension(mp3Path);
                    audioSource.clip = clip;
                    audioSource.Play();
                    isPlaying = true;
                    UpdateMusicNameDisplay(clip.name);
                    Jotunn.Logger.LogWarning("Now playing: " + clip.name);
                }
                else
                {
                    Jotunn.Logger.LogError("Failed to load audio clip: " + www.error);
                }
            }
        }

        public static void PauseMusic()
        {
            if (audioSource != null && isPlaying)
            {
                audioSource.Pause();
                isPlaying = false;
                Jotunn.Logger.LogWarning("Music paused");
            }
        }

        public static void ResumeMusic()
        {
            if (audioSource != null && !isPlaying)
            {
                audioSource.UnPause();
                isPlaying = true;
                Jotunn.Logger.LogWarning("Music resumed");
            }
        }

        public static void StopMusic()
        {
            if (audioSource != null && isPlaying)
            {
                audioSource.Stop();
                isPlaying = false;
                Jotunn.Logger.LogWarning("Music stopped");
            }
        }
    }
}