using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.GUI;
using UnityEngine.SceneManagement;
using Jotunn.Utils;
using BepInEx.Logging;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace ColorfulFlame
{
    delegate Color GetterColor();
    [BepInPlugin("vsp.ColorfulFlame", "ColorfulFlame", "1.0.0")]
    public class Class1 : BaseUnityPlugin
    {
        private ConfigEntry<KeyCode> KeyConfig;
        private ConfigEntry<KeyboardShortcut> ShortcutConfig;
        private ButtonConfig GuiVisible;
        private ButtonConfig Select;
        private static Color choosedColor;
        private static Material material;

        private static ManualLogSource Log;

        void Awake()
        {
            Class1.Log = base.Logger;

            material = AssetUtils.LoadAssetBundleFromResources("flamenew").LoadAsset<Material>("Assets/Material/flameball_flipbook.mat");
            Logger.LogInfo(material.name);
            ConfigAdd();
            InputAdd();

        }
        void Update()
        {
            if (ZInput.instance != null)
            {
                if (ZInput.GetButtonDown(GuiVisible.Name))
                {
                    ChooseColor();
                }
                else if (ZInput.GetButtonDown(Select.Name))
                {
                    FixColor();
                }
            }
        }
        static void FixColor()
        {
            Transform res = null;
            try
            {
                var hovered = Player.m_localPlayer.GetHoverObject().transform.Find("../");
                if (hovered.name.Contains("groundtorch") || hovered.name.Contains("walltorch"))
                {
                    res = hovered.transform.Find("_enabled/fx_Torch_Basic");
                    if (res == null)
                    {
                        res = hovered.transform.Find("_enabled/fx_Torch_Blue");
                        if (res == null)
                        {
                            res = hovered.transform.Find("_enabled/fx_Torch_Green");
                            if (res == null)
                            {
                                //res = hovered.transform.Find("_enabled/demister_ball (1)");
                                if (res == null)
                                    return;
                                //var roundTorch = res.GetComponent<Material>();
                                //roundTorch.SetColor("MainTex", choosedColor);
                                //roundTorch.color = choosedColor;
                                //return;
                            }

                        }
                    }
                }
                /*
                else if (hovered.name.Contains("brazier"))
                {
                    res = hovered.transform.Find("_enabled_high/fx_Brazier_flames/flames (1)");
                    if (res == null)
                        return;
                    var temp = res.transform.Find("../low_flames").GetComponent<ParticleSystemRenderer>();
                    temp.material = material;
                    var temp2 = res.transform.Find("../low_flames").GetComponent<ParticleSystem>();
                    temp2.startColor = choosedColor;
                    return;
                }
                */
                else
                {
                    return;
                }
            }
            catch
            {
                Class1.Log.LogInfo("Incorrect object");
                return;
            }
            var particleSystem = res.GetComponent<ParticleSystemRenderer>();
            particleSystem.material = material;
            particleSystem.material.color = choosedColor;
        }
        void ConfigAdd()

        {
            Config.SaveOnConfigSet = false;
            KeyConfig = Config.Bind("General", "KeyCode", KeyCode.F3, new ConfigDescription("The key for displaying the menu"));
            ShortcutConfig = Config.Bind("General", "Shortcut", new KeyboardShortcut(KeyCode.E, KeyCode.LeftShift), new ConfigDescription("Shortcut combination"));
        }
        void InputAdd()
        {
            GuiVisible = new ButtonConfig
            {
                Name = "Menu",
                Key = KeyConfig.Value,
                ActiveInCustomGUI = false
            };
            InputManager.Instance.AddButton("vsp.ColorfulFlame", GuiVisible);
            Select = new ButtonConfig
            {
                Name = "Select",
                ShortcutConfig = ShortcutConfig
            };
            InputManager.Instance.AddButton("vsp.ColorfulFlame", Select);
        }

        private void ChooseColor()
        {
            if (GUIManager.Instance == null)
            {
                Logger.LogError("GUIManager instance is null");
                return;
            }

            if (!GUIManager.CustomGUIFront)
            {
                Logger.LogError("GUIManager CustomGUI is null");
                return;
            }
            if (SceneManager.GetActiveScene().name == "main" && ColorPicker.done)
            {
                GUIManager.Instance.CreateColorPicker(
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                choosedColor,
                "Choose your color",
                SetColor,
                ColorChosen,
                false
                );
                GUIManager.BlockInput(true);

            }
        }
        public void SetColor(Color currentColor)
        {
            currentColor.a = 1f;
            choosedColor = currentColor;
        }

        private void ColorChosen(Color finalColor)
        {
            finalColor.a = 1f;
            choosedColor = finalColor;
            GUIManager.BlockInput(false);
        }
    }
}

