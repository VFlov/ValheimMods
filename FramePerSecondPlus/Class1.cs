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


namespace FramePerSecondPlus
{
    [BepInPlugin("vsp.FramePerSecondPlus", "FramePerSecondPlus", "1.4.0")]
    public class FramePerSecondPlus : BaseUnityPlugin
    {
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

        [HarmonyPatch(typeof(ClutterSystem), "Awake")]
        private static class ClutterSystem_Awake_Patch
        {
            private static void Prefix(ClutterSystem __instance)
            {
                __instance.m_grassPatchSize = 20;
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
                    __instance.m_alpha = Mathf.Clamp01(__instance.m_time);
                    __instance.m_time += deltaTime;
                    __instance.m_body.mass = 0.75f;
                    Vector3 velocity = __instance.m_body.velocity;
                    Vector3 vel = __instance.m_vel;
                    vel.y *= 0.75f;
                    Vector3 a = vel - velocity;
                    __instance.m_body.AddForce(a * (__instance.m_force * deltaTime), ForceMode.VelocityChange);
                    if (__instance.m_fadeTimer >= 0f)
                    {
                        __instance.m_fadeTimer += deltaTime;
                        __instance.m_alpha *= 0.5f;
                        if (__instance.m_fadeTimer >= __instance.m_fadetime)
                        {
                            UnityEngine.Object.Destroy(__instance.gameObject);
                        }
                    }

                    if (__instance.m_added)
                    {
                        Color color = __instance.m_propertyBlock.GetColor(Smoke.m_colorProp);
                        color.a = __instance.m_alpha;
                        __instance.m_propertyBlock.SetColor(Smoke.m_colorProp, color);
                        __instance.m_mr.SetPropertyBlock(__instance.m_propertyBlock);
                    }
                return false;
            }
                
        }
        
    }
}
