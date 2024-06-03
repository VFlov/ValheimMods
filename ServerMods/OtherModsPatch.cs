using HarmonyLib;
using ServerMods.ClientServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jotunn.Entities;

namespace ServerMods
{
    [HarmonyPatch(typeof(Quests_DataTypes.Quest), "RemoveQuestComplete")]
    public static class OnComplitedQuestPatch
    {
        // Token: 0x0600000E RID: 14 RVA: 0x00002354 File Offset: 0x00000554
        private static void Prefix(int UID)
        {
            if (!Player.m_localPlayer)
            {
                return;
            }
            if (!Quests_DataTypes.AcceptedQuests.ContainsKey(UID))
            {
                return;
            }
            Quests_DataTypes.Quest quest = Quests_DataTypes.AcceptedQuests[UID];
            if (quest.Name.Contains("Дом№"))
            {
                ClientSide.RPC_ClientBuyTerritory(quest.Name);
            }
            for (int i = 0; i < quest.RewardCount.Length; i++)
            {
                if (quest.RewardType[i] == 6)
                {
                    API.AddExperience(quest.RewardCount[i]);
                }
            }
        }
    }
}
