using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServerMods
{
    [BepInPlugin("vsp.ServerMod", "ServerMod", "1.0.0")]
    internal class ServerMod : BaseUnityPlugin
    {
        public void Awake()
        {
            PrefabManager.OnPrefabsRegistered += Items.CoinsEdit;
            PrefabManager.OnVanillaPrefabsAvailable += Items.StomEnemyAdd;
            CommandManager.Instance.AddConsoleCommand(new ClearQuestsCommand());
            CommandManager.Instance.AddConsoleCommand(new RemoveAllPins());
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public void OnDestroy()
        {
            Harmony.UnpatchAll();
        }
        public static bool DeathItemLess;
    }
    /// <summary>
    /// Disable structure system
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "UpdateWear")]
    public static class WearNTearSupportDisable
    {
        private static void Prefix(WearNTear __instance)
        {
            __instance.m_noSupportWear = false;
        }
    }
    /// <summary>
    /// Disable quit button. For smooth closing the game
    /// </summary>
    [HarmonyPatch(typeof(Menu), "OnQuit")]
    public static class DisableQuitButton
    {
        private static void Postfix(Menu __instance)
        {
            __instance.m_quitDialog.gameObject.SetActive(false);
            __instance.m_logoutDialog.gameObject.SetActive(true);
        }
    }
}
