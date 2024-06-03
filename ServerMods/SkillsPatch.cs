using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMods
{
    internal class SkillsPatch
    {
    }
    [HarmonyPatch(typeof(Skills), "RaiseSkill")]
    internal static class MovementSkillPatch
    {
        private static bool Prefix(Skills.SkillType skillType)
        {
            return skillType != Skills.SkillType.Run && skillType != Skills.SkillType.Jump;
        }
    }
}
