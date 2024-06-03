using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMods
{
    internal class TombstonePatch
    {
    }
    [HarmonyPatch(typeof(Player), "CreateTombStone")]
    public static class TombstoneEmpty
    {
        // Token: 0x06000008 RID: 8 RVA: 0x00002162 File Offset: 0x00000362
        public static void Prefix(Player __instance)
        {
            if (__instance.m_inventory.NrOfItems() == 0)
            {
                ServerMod.DeathItemLess = true;
            }
        }
    }
    [HarmonyPatch(typeof(TombStone), "OnTakeAllSuccess")]
    public static class TombStone_Despawn
    {
        // Token: 0x06000007 RID: 7 RVA: 0x000020F4 File Offset: 0x000002F4
        public static void Prefix(TombStone __instance)
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
        }
    }
}
