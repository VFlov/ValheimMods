using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Jotunn;
using BepInEx;
using Jotunn.Configs;
using Marketplace;

namespace ServerMods.ClientServer
{
    internal class ClientSide
    {
        public static void RPC_ClientBuyTerritory(string arg)
        {
            ZPackage zpackage = new ZPackage();
            zpackage.Write(arg);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "ServerTerritoryChanger", new object[]
            {
                zpackage
            });
        }

        // Token: 0x0600000B RID: 11 RVA: 0x00002234 File Offset: 0x00000434
        public static void RPC_RequestTerritory(long sender, ZPackage pkg)
        {
            pkg.SetPos(0);
            if (sender == ZRoutedRpc.instance.GetServerPeerID() && pkg != null && pkg.Size() > 0)
            {
                char[] array = new char[pkg.Size()];
                for (int i = 0; i < pkg.Size(); i++)
                {
                    array[i] = pkg.ReadChar();
                }
                if (array[0] == '0' && array[1] == '1')
                {
                    int num = Array.IndexOf<char>(array, 'h');
                    string text = "";
                    for (int j = num + 1; j < array.Length; j++)
                    {
                        text += array[j].ToString();
                    }
                    ClientSide.Key = array[2].ToString() + text;
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Территория №" + array[3].ToString() + " Приобретена", 0, null);
                    return;
                }
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Что-то пошло не так. Свяжитесь с администратором", 0, null);
            }
        }

        public static string Key;
    }
    [HarmonyPatch(typeof(ZoneSystem), "Update")]
    [ClientOnlyPatch]
    public static class ClientAddKey
    {
        private static void Prefix(ZoneSystem __instance)
        {
            if (ClientSide.Key != null)
            {
                __instance.SetGlobalKey(ClientSide.Key);
                ClientSide.Key = null;
            }
        }
    }
    [HarmonyPatch(typeof(Game), "Start")]
    public static class ClientPatch
    {
        private static void Prefix()
        {
            if (!ZNet.instance.IsServer())
            {
                ZRoutedRpc.instance.Register<ZPackage>("RequestTerritory", new Action<long, ZPackage>(ClientSide.RPC_RequestTerritory));
            }
        }
    }
}
