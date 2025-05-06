using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using static LightFlicker;
using UnityEngine;

namespace PrettyPotion
{
    public static class Patches
    {
        [HarmonyPatch(typeof(Skills), "LowerAllSkills")] // Гриб етунов - оранжевый = ModifySpeed
        private class SkillLowerOff_Update_Patch : MonoBehaviour
        {
            private static bool Prefix(Skills __instance, float factor)
            {
                //if (__instance.m_player.statu())
                factor = 1;
                return true;
            }
        }

        [HarmonyPatch(typeof(SE_Stats), "ModifyHealthRegen")] // Желтый гриб - желтый = хил
        private class ModifyHealthRegen_Update_Patch : MonoBehaviour
        {

        }
        [HarmonyPatch(typeof(SE_Stats), "ModifyStaminaRegen")] //Малина + черника - фиолетовый = стамина
        private class ModifyStaminaRegen_Update_Patch : MonoBehaviour
        {


        }
        [HarmonyPatch(typeof(SE_Stats), "ModifyEitrRegen")] // Волшебный гриб - синий = ейтр восстановление
        private class ModifyEitrRegen_Update_Patch : MonoBehaviour
        {

        }
        [HarmonyPatch(typeof(SE_Stats), "ModifyRaiseSkill")] // Гроздь - зеленый = увеличение получаемого опыта
        private class ModifyRaiseSkill_Update_Patch : MonoBehaviour
        {

            
        }
        [HarmonyPatch(typeof(SE_Stats), "ModifyDamageMods")] //  Туша - красный = урон
        private class ModifyDamageMods_Update_Patch : MonoBehaviour
        {
            private static bool Prefix(SE_Stats __instance, ref HitData.DamageModifiers modifiers)
            {
                
              
                return false;
            }
        }
        [HarmonyPatch(typeof(SE_Stats), "ModifySpeed")] // Гриб етунов - оранжевый = ModifySpeed
        private class MovementSpeed_Update_Patch : MonoBehaviour
        {
            private static bool Prefix(SE_Stats __instance, float baseSpeed, ref float speed, Character character, Vector3 dir)
            {
                __instance.m_windMovementModifier = 0.25f;
                return false;
            }
        }
        [HarmonyPatch(typeof(Player), "UpdateDodge")] // Гриб етунов - оранжевый = ModifySpeed
        private class Dodge_Update_Patch : MonoBehaviour
        {
            private static bool Prefix(Player __instance, float dt)
            {
                __instance.m_queuedDodgeTimer = dt;
                return true;
            }
        }
    }
}
