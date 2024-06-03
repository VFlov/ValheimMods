using HarmonyLib;
using Jotunn.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMods
{
    [HarmonyPatch(typeof(Player), "OnDeath")]
    public static class DeadNoItemRemovePin
    {
        public static void Postfix(Player __instance)
        {
            if (ServerMod.DeathItemLess)
            {
                Minimap.PinData pinData = (from a in Minimap.instance.m_pins
                                           where a.m_type == Minimap.PinType.Death 
                                           orderby Utils.DistanceXZ(__instance.transform.position, a.m_pos)
                                           select a).FirstOrDefault<Minimap.PinData>();
                if (pinData == null)
                {
                    return;
                }
                Minimap.instance.RemovePin(pinData);
                ServerMod.DeathItemLess = false;
            }
        }
    }
}
