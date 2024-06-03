using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using ServerSync;


namespace FastAndFairTerraforming
{
    [BepInPlugin("vsp.FastAndFairTerraforming", "FastAndFairTerraforming", "1.1.0")]
    public class Class1 : BaseUnityPlugin
    {
        private void Awake()
        {

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        //[HarmonyEmitIL]
        [HarmonyPatch(typeof(CharacterAnimEvent), "CustomFixedUpdate")]
        private static class CharacterAnimEvent_Awake_Patch
        {
            private static void Prefix(CharacterAnimEvent __instance)
            {
                if (__instance.m_character != Player.m_localPlayer)
                    return;
                ItemDrop.ItemData currentWeapon = (__instance.m_character as Humanoid).GetCurrentWeapon();
                if (currentWeapon.m_dropPrefab.name == "PickaxeAntler")
                {
                    if (__instance.m_animator.GetCurrentAnimatorStateInfo(0).IsName("swing_pickaxe"))
                    {
                        __instance.m_animator.speed = 2f;

                    }
                }
            }
        }
        [HarmonyPatch(typeof(SEMan), "ModifyAttack")]
        private class Damage
        {
            private static void Prefix(SEMan __instance, ref HitData hitData)
            {
                if (__instance.m_character == Player.m_localPlayer && (__instance.m_character as Humanoid).GetCurrentWeapon().m_dropPrefab.name == "PickaxeAntler")
                {
                    HitData hitData2 = hitData;
                    hitData2.m_damage.m_pickaxe = hitData2.m_damage.m_pickaxe / 2;
                }
            }
        }
        [HarmonyPatch(typeof(Attack), "GetAttackStamina")]
        private class Stamina
        {
            private static void Prefix(Attack __instance)
            {
                if (__instance.m_character != Player.m_localPlayer)
                    return;
                if ((__instance.m_character as Humanoid).GetCurrentWeapon().m_dropPrefab.name == "PickaxeAntler")
                    __instance.m_attackStamina /= 2;
            }
        }
        /*
        [HarmonyPatch(typeof(Attack), "ProjectileAttackTriggered")]
        private class Durability
        {
            private static void Prefix(Attack __instance)
            {
                if (__instance.m_character != Player.m_localPlayer)
                    return;
                if ((__instance.m_character as Humanoid).GetCurrentWeapon().m_dropPrefab.name == "PickaxeAntler")
                    __instance.m_weapon.m_shared.m_useDurabilityDrain /= 2;
            }
        }
        */
    }
}

