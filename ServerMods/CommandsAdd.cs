using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Jotunn.Entities;
using Marketplace.Modules.Quests;

namespace ServerMods
{
    internal class RemoveAllPins : ConsoleCommand
    {
        public override string Name
        {
            get
            {
                return "removeallpins";
            }
        }

        public override string Help
        {
            get
            {
                return "Clear all pins on map";
            }
        }

        public override void Run(string[] args)
        {
            IEnumerable<Minimap.PinData> enumerable = from a in Minimap.instance.m_pins
                                                      where a.m_type == Minimap.PinType.Death
                                                      select a;
            if (enumerable == null)
            {
                return;
            }
            foreach (Minimap.PinData pinData in enumerable)
            {
                Minimap.instance.RemovePin(pinData);
            }
        }
    }
    public class ClearQuestsCommand : ConsoleCommand
    {
        public override string Name
        {
            get
            {
                return "mclearquest";
            }
        }

        public override string Help
        {
            get
            {
                return "Clear complited quest";
            }
        }

        public override void Run(string[] args)
        {
            if (args.Length == 0 || !Player.m_localPlayer.m_debugFly)
            {
                return;
            }
            int hashCode = args[0].ToLower().GetHashCode();
            Player.m_localPlayer.m_customData.Remove(string.Format("[MPASN]quest={0}", hashCode));
            Player.m_localPlayer.m_customData.Remove(string.Format("[MPASN]questCD={0}", hashCode));
            Quests_DataTypes.AcceptedQuests.Remove(hashCode);
            Quests_UIs.AcceptedQuestsUI.CheckQuests();
        }
    }
}
