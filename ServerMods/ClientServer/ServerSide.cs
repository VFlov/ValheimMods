using HarmonyLib;
using Marketplace;
using Marketplace.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMods.ClientServer
{
    public class ServerSide
    {
        public static void RPC_ServerTerritoryChanger(long sender, ZPackage pkg)
        {
            ZPackage zpackage = new ZPackage();
            zpackage.Write('0');
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            if (peer == null)
            {
                return;
            }
            string hostName = peer.m_socket.GetHostName();
            if (pkg != null && pkg.Size() > 0)
            {
                string text = pkg.ReadString();
                string[] files = Directory.GetFiles(Market_Paths.TerritoriesFolder, "*.cfg", SearchOption.AllDirectories); //MarketplacePath needed
                string value = "[" + text + "]";
                for (int i = 0; i < files.Count<string>(); i++)
                {
                    string[] array = File.ReadAllLines(files[i]);
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (array[j].Contains(value))
                        {
                            array[j + 5] = hostName;
                            File.Delete(files[i]);
                            File.WriteAllLines(files[i], array);
                            zpackage.Write('1');
                            ServerSide.Key = "h" + text.Split(new char[]
                            {
                                '№'
                            })[1];
                            zpackage.Write(ServerSide.Key);
                        }
                    }
                }
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(0L, "RequestTerritory", new object[]
            {
                zpackage
            });
        }

        // Token: 0x04000003 RID: 3
        public static string Key;
    }
    [HarmonyPatch(typeof(ZoneSystem), "Update")]
    [ServerOnlyPatch]
    public static class ServerAddKey
    {
        // Token: 0x0600001E RID: 30 RVA: 0x00002863 File Offset: 0x00000A63
        private static void Prefix(ZoneSystem __instance)
        {
            if (ServerSide.Key != null)
            {
                __instance.GlobalKeyAdd(ServerSide.Key, true);
                ServerSide.Key = null;
            }
        }
    }
    [HarmonyPatch(typeof(Game), "Start")]
    public static class ServerPatch
    {
        // Token: 0x0600001D RID: 29 RVA: 0x0000283A File Offset: 0x00000A3A
        private static void Prefix()
        {
            if (ZNet.instance.IsServer())
            {
                ZRoutedRpc.instance.Register<ZPackage>("ServerTerritoryChanger", new Action<long, ZPackage>(ServerSide.RPC_ServerTerritoryChanger));
            }
        }
    }
}
