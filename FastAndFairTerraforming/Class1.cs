using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using ServerSync;
using UnityEngine;

//Ловит ошибки при атаке противника. Отследить
namespace FastAndFairTerraforming
{
    [BepInPlugin("vaffle.BetterAntlerPickaxe", "BetterAntlerPickaxe", "1.0.0")]
    public class Class1 : BaseUnityPlugin
    {
        private void Awake()
        {

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        /*
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
        */
        [HarmonyPatch(typeof(CharacterAnimEvent), "CustomFixedUpdate")]
        private static class CharacterAnimEvent_Awake_Patch
        {
            private static void Prefix(CharacterAnimEvent __instance)
            {
                // Получаем доступ к приватному полю через Harmony
                var character = Traverse.Create(__instance).Field("m_character").GetValue() as Character;

                if (character != Player.m_localPlayer)
                    return;

                ItemDrop.ItemData currentWeapon = (character as Humanoid).GetCurrentWeapon();
                if (currentWeapon?.m_dropPrefab?.name == "PickaxeAntler")
                {
                    var animator = Traverse.Create(__instance).Field("m_animator").GetValue() as Animator;
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName("swing_pickaxe"))
                    {
                        animator.speed = 2f;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(SEMan), "ModifyAttack")]
        private class Damage
        {
            private static void Prefix(SEMan __instance, ref HitData hitData)
            {
                // Получаем доступ к приватному полю m_character через Traverse
                var character = Traverse.Create(__instance).Field("m_character").GetValue() as Character;

                if (character != Player.m_localPlayer)
                    return;

                ItemDrop.ItemData currentWeapon = (character as Humanoid).GetCurrentWeapon();
                if (currentWeapon?.m_dropPrefab?.name == "PickaxeAntler")
                {
                    HitData hitData2 = hitData;
                    hitData2.m_damage.m_pickaxe = hitData2.m_damage.m_pickaxe / 2;
                    hitData = hitData2;
                }
            }
        }
        [HarmonyPatch(typeof(Attack), "GetAttackStamina")]
        private class Stamina
        {
            private static void Prefix(Attack __instance)
            {
                // Получаем доступ к приватному полю m_character через Traverse
                var character = Traverse.Create(__instance).Field("m_character").GetValue() as Character;

                if (character != Player.m_localPlayer)
                    return;

                ItemDrop.ItemData currentWeapon = (character as Humanoid).GetCurrentWeapon();
                if (currentWeapon?.m_dropPrefab?.name == "PickaxeAntler")
                {
                    __instance.m_attackStamina /= 2;
                }
            }
        }
    }
}

