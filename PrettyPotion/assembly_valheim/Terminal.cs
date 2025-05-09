using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using GUIFramework;
using Splatform;
using TMPro;
using UnityEngine;

// Token: 0x02000073 RID: 115
public abstract class Terminal : MonoBehaviour
{
	// Token: 0x0600073F RID: 1855 RVA: 0x0003BC04 File Offset: 0x00039E04
	private static void InitTerminal()
	{
		if (Terminal.m_terminalInitialized)
		{
			return;
		}
		Terminal.m_terminalInitialized = true;
		Terminal.AddConsoleCheatCommands();
		new Terminal.ConsoleCommand("help", "Shows a list of console commands (optional: help 2 4 shows the second quarter)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (ZNet.instance && ZNet.instance.IsServer())
			{
				Player.m_localPlayer;
			}
			args.Context.IsCheatsEnabled();
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, Terminal.ConsoleCommand> keyValuePair in Terminal.commands)
			{
				if (keyValuePair.Value.ShowCommand(args.Context))
				{
					list.Add(keyValuePair.Value.Command + " - " + keyValuePair.Value.Description);
				}
			}
			list.Sort();
			if (args.Context != null)
			{
				int num = args.TryParameterInt(2, 5);
				int num2;
				if (args.TryParameterInt(1, out num2))
				{
					int num3 = list.Count / num;
					for (int j = num3 * (num2 - 1); j < Mathf.Min(list.Count, num3 * (num2 - 1) + num3); j++)
					{
						args.Context.AddString(list[j]);
					}
					return;
				}
				foreach (string text in list)
				{
					args.Context.AddString(text);
				}
			}
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("devcommands", "enables cheats", delegate(Terminal.ConsoleEventArgs args)
		{
			if (ZNet.instance && !ZNet.instance.IsServer())
			{
				ZNet.instance.RemoteCommand("devcommands");
			}
			Terminal.m_cheat = !Terminal.m_cheat;
			Terminal context = args.Context;
			if (context != null)
			{
				context.AddString("Dev commands: " + Terminal.m_cheat.ToString());
			}
			Terminal context2 = args.Context;
			if (context2 != null)
			{
				context2.AddString("WARNING: using any dev commands is not recommended and is done at your own risk.");
			}
			Gogan.LogEvent("Cheat", "CheatsEnabled", Terminal.m_cheat.ToString(), 0L);
			args.Context.updateCommandList();
		}, false, false, false, true, false, null, false, false, false);
		new Terminal.ConsoleCommand("hidebetatext", "", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Hud.instance)
			{
				Hud.instance.ToggleBetaTextVisible();
			}
		}, false, false, false, true, false, null, false, false, false);
		new Terminal.ConsoleCommand("ping", "ping server", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Game.instance)
			{
				Game.instance.Ping();
			}
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("dpsdebug", "toggle dps debug print", delegate(Terminal.ConsoleEventArgs args)
		{
			Character.SetDPSDebug(!Character.IsDPSDebugEnabled());
			Terminal context = args.Context;
			if (context == null)
			{
				return;
			}
			context.AddString("DPS debug " + Character.IsDPSDebugEnabled().ToString());
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("lodbias", "set distance lod bias", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length == 1)
			{
				args.Context.AddString("Lod bias:" + QualitySettings.lodBias.ToString());
				return;
			}
			float lodBias;
			if (args.TryParameterFloat(1, out lodBias))
			{
				args.Context.AddString("Setting lod bias:" + lodBias.ToString());
				QualitySettings.lodBias = lodBias;
			}
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("info", "print system info", delegate(Terminal.ConsoleEventArgs args)
		{
			args.Context.AddString("Render threading mode:" + SystemInfo.renderingThreadingMode.ToString());
			long totalMemory = GC.GetTotalMemory(false);
			args.Context.AddString("Total allocated mem: " + (totalMemory / 1048576L).ToString("0") + "mb");
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("gc", "shows garbage collector information", delegate(Terminal.ConsoleEventArgs args)
		{
			long totalMemory = GC.GetTotalMemory(false);
			GC.Collect();
			long totalMemory2 = GC.GetTotalMemory(true);
			long num = totalMemory2 - totalMemory;
			args.Context.AddString(string.Concat(new string[]
			{
				"GC collect, Delta: ",
				(num / 1048576L).ToString("0"),
				"mb   Total left:",
				(totalMemory2 / 1048576L).ToString("0"),
				"mb"
			}));
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("cr", "unloads unused assets", delegate(Terminal.ConsoleEventArgs args)
		{
			args.Context.AddString("Unloading unused assets");
			Game.instance.CollectResources(true);
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("fov", "changes camera field of view", delegate(Terminal.ConsoleEventArgs args)
		{
			Camera mainCamera = Utils.GetMainCamera();
			if (mainCamera)
			{
				if (args.Length == 1)
				{
					args.Context.AddString("Fov:" + mainCamera.fieldOfView.ToString());
					return;
				}
				float num;
				if (args.TryParameterFloat(1, out num) && num > 5f)
				{
					args.Context.AddString("Setting fov to " + num.ToString());
					Camera[] componentsInChildren = mainCamera.GetComponentsInChildren<Camera>();
					for (int j = 0; j < componentsInChildren.Length; j++)
					{
						componentsInChildren[j].fieldOfView = num;
					}
				}
			}
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("kick", "[name/ip/userID] - kick user", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string user = args[1];
			ZNet.instance.Kick(user);
			return true;
		}, false, true, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("ban", "[name/ip/userID] - ban user", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string user = args[1];
			ZNet.instance.Ban(user);
			return true;
		}, false, true, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("unban", "[ip/userID] - unban user", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string user = args[1];
			ZNet.instance.Unban(user);
			return true;
		}, false, true, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("banned", "list banned users", delegate(Terminal.ConsoleEventArgs args)
		{
			ZNet.instance.PrintBanned();
		}, false, true, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("save", "force saving of world and resets world save interval", delegate(Terminal.ConsoleEventArgs args)
		{
			ZNet.instance.SaveWorldAndPlayerProfiles();
		}, false, true, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("optterrain", "optimize old terrain modifications", delegate(Terminal.ConsoleEventArgs args)
		{
			TerrainComp.UpgradeTerrain();
			Heightmap.UpdateTerrainAlpha();
		}, false, true, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("genloc", "regenerate all locations.", delegate(Terminal.ConsoleEventArgs args)
		{
			ZoneSystem.instance.GenerateLocations();
		}, false, false, true, false, false, null, false, true, false);
		new Terminal.ConsoleCommand("players", "[nr] - force diffuculty scale ( 0 = reset)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			int forcePlayerDifficulty;
			if (args.TryParameterInt(1, out forcePlayerDifficulty))
			{
				Game.instance.SetForcePlayerDifficulty(forcePlayerDifficulty);
				args.Context.AddString("Setting players to " + forcePlayerDifficulty.ToString());
			}
			return true;
		}, true, false, true, false, false, null, false, true, false);
		new Terminal.ConsoleCommand("exclusivefullscreen", "changes window mode to exclusive fullscreen, or back to borderless", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen)
			{
				Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
				return;
			}
			Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("setkey", "[name]", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				ZoneSystem.instance.SetGlobalKey(args.FullLine.Substring(args[0].Length + 1));
				args.Context.AddString("Setting global key " + args[1]);
				return;
			}
			args.Context.AddString("Syntax: setkey [key]");
		}, true, false, true, false, false, delegate()
		{
			List<string> list = Enum.GetNames(typeof(GlobalKeys)).ToList<string>();
			list.Remove(GlobalKeys.NonServerOption.ToString());
			return list;
		}, false, true, false);
		new Terminal.ConsoleCommand("removekey", "[name]", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				ZoneSystem.instance.RemoveGlobalKey(args.FullLine.Substring(args[0].Length + 1));
				args.Context.AddString("Removing global key " + args[1]);
				return;
			}
			args.Context.AddString("Syntax: setkey [key]");
		}, true, false, true, false, false, delegate()
		{
			if (!ZoneSystem.instance)
			{
				return null;
			}
			return ZoneSystem.instance.GetGlobalKeys();
		}, true, true, false);
		new Terminal.ConsoleCommand("resetkeys", "[name]", delegate(Terminal.ConsoleEventArgs args)
		{
			ZoneSystem.instance.ResetGlobalKeys();
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer != null)
			{
				localPlayer.ResetUniqueKeys();
			}
			args.Context.AddString("Global and player keys cleared");
		}, true, false, true, false, false, null, false, true, false);
		new Terminal.ConsoleCommand("resetworldkeys", "[name] Resets all world modifiers to default", delegate(Terminal.ConsoleEventArgs args)
		{
			ZoneSystem.instance.ResetWorldKeys();
			args.Context.AddString("Server keys cleared");
		}, false, false, true, false, false, null, false, true, false);
		new Terminal.ConsoleCommand("setworldpreset", "[name] Resets all world modifiers to a named preset", delegate(Terminal.ConsoleEventArgs args)
		{
			WorldPresets preset;
			if (Enum.TryParse<WorldPresets>(args[1], true, out preset))
			{
				ZoneSystem.instance.ResetWorldKeys();
				ServerOptionsGUI.m_instance.ReadKeys(ZNet.World);
				ServerOptionsGUI.m_instance.SetPreset(ZNet.World, preset);
				ServerOptionsGUI.m_instance.SetKeys(ZNet.World);
				return true;
			}
			return "Invalid preset";
		}, false, false, true, false, false, () => Enum.GetNames(typeof(WorldPresets)).ToList<string>(), false, true, false);
		new Terminal.ConsoleCommand("setworldmodifier", "[name] [value] Sets a world modifier value", delegate(Terminal.ConsoleEventArgs args)
		{
			WorldModifiers preset;
			WorldModifierOption value;
			if (Enum.TryParse<WorldModifiers>(args[1], true, out preset) && Enum.TryParse<WorldModifierOption>(args[2], true, out value))
			{
				ServerOptionsGUI.m_instance.ReadKeys(ZNet.World);
				ServerOptionsGUI.m_instance.SetPreset(ZNet.World, preset, value);
				ServerOptionsGUI.m_instance.SetKeys(ZNet.World);
				return true;
			}
			return "Invalid input, possible valid values are: " + string.Join(", ", Enum.GetNames(typeof(WorldModifierOption)));
		}, false, false, true, false, false, () => Enum.GetNames(typeof(WorldModifiers)).ToList<string>(), false, true, false);
		new Terminal.ConsoleCommand("setkeyplayer", "[name]", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				Player.m_localPlayer.AddUniqueKey(args.FullLine.Substring(args[0].Length + 1));
				args.Context.AddString("Setting player key " + args[1]);
				return;
			}
			args.Context.AddString("Syntax: setkey [key]");
		}, true, false, true, false, false, () => Enum.GetNames(typeof(PlayerKeys)).ToList<string>(), false, false, false);
		new Terminal.ConsoleCommand("removekeyplayer", "[name]", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				Player.m_localPlayer.RemoveUniqueKey(args.FullLine.Substring(args[0].Length + 1));
				args.Context.AddString("Removing player key " + args[1]);
				return;
			}
			args.Context.AddString("Syntax: setkey [key]");
		}, true, false, true, false, false, delegate()
		{
			if (!Player.m_localPlayer)
			{
				return null;
			}
			return Player.m_localPlayer.GetUniqueKeys();
		}, true, false, false);
		new Terminal.ConsoleCommand("listkeys", "", delegate(Terminal.ConsoleEventArgs args)
		{
			List<string> list = ZoneSystem.instance.GetGlobalKeys();
			args.Context.AddString(string.Format("Current Keys: {0}", list.Count));
			foreach (string str in list)
			{
				args.Context.AddString("  " + str);
			}
			args.Context.AddString(string.Format("Server Option Keys: {0}", ZNet.World.m_startingGlobalKeys.Count));
			foreach (string str2 in ZNet.World.m_startingGlobalKeys)
			{
				args.Context.AddString("  " + str2);
			}
			if (args.Length > 2)
			{
				args.Context.AddString(string.Format("Current Keys Values: {0}", list.Count));
				foreach (KeyValuePair<string, string> keyValuePair in ZoneSystem.instance.m_globalKeysValues)
				{
					args.Context.AddString("  " + keyValuePair.Key + ": " + keyValuePair.Value);
				}
				args.Context.AddString(string.Format("Current Keys Enums: {0}", list.Count));
				foreach (GlobalKeys globalKeys in ZoneSystem.instance.m_globalKeysEnums)
				{
					args.Context.AddString(string.Format("  {0}", globalKeys));
				}
			}
			if (Player.m_localPlayer)
			{
				list = Player.m_localPlayer.GetUniqueKeys();
				args.Context.AddString(string.Format("Player Keys: {0}", list.Count));
				foreach (string str3 in list)
				{
					args.Context.AddString("  " + str3);
				}
			}
		}, true, false, true, false, false, null, false, true, false);
		new Terminal.ConsoleCommand("sortcraft", "[type] sorts crafting lists according to setting", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.RemoveUniqueKeyValue("sortcraft");
			if (args.Length >= 2 && args[1].Length > 0)
			{
				Player.m_localPlayer.AddUniqueKeyValue("sortcraft", args[1]);
				args.Context.AddString("List sorting set to: " + args[1]);
				return;
			}
			args.Context.AddString("List sorting reset");
		}, false, false, false, false, false, () => Enum.GetNames(typeof(InventoryGui.SortMethod)).ToList<string>(), false, false, false);
		new Terminal.ConsoleCommand("debugmode", "fly mode", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_debugMode = !Player.m_debugMode;
			args.Context.AddString("Debugmode " + Player.m_debugMode.ToString());
		}, true, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("fly", "fly mode", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.ToggleDebugFly();
			int debugFlySpeed;
			if (args.TryParameterInt(1, out debugFlySpeed))
			{
				Character.m_debugFlySpeed = debugFlySpeed;
			}
		}, true, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("nocost", "no build cost", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.HasArgumentAnywhere("on", 0, true))
			{
				Player.m_localPlayer.SetNoPlacementCost(true);
				return;
			}
			if (args.HasArgumentAnywhere("off", 0, true))
			{
				Player.m_localPlayer.SetNoPlacementCost(false);
				return;
			}
			Player.m_localPlayer.ToggleNoPlacementCost();
		}, true, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("raiseskill", "[skill] [amount]", delegate(Terminal.ConsoleEventArgs args)
		{
			int num;
			if (args.TryParameterInt(2, out num))
			{
				Player.m_localPlayer.GetSkills().CheatRaiseSkill(args[1], (float)num, true);
				return;
			}
			args.Context.AddString("Syntax: raiseskill [skill] [amount]");
		}, true, false, true, false, false, delegate()
		{
			List<string> list = Enum.GetNames(typeof(Skills.SkillType)).ToList<string>();
			list.Remove(Skills.SkillType.None.ToString());
			return list;
		}, false, false, false);
		new Terminal.ConsoleCommand("resetskill", "[skill]", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length > 1)
			{
				string name = args[1];
				Player.m_localPlayer.GetSkills().CheatResetSkill(name);
				return;
			}
			args.Context.AddString("Syntax: resetskill [skill]");
		}, true, false, true, false, false, delegate()
		{
			List<string> list = Enum.GetNames(typeof(Skills.SkillType)).ToList<string>();
			list.Remove(Skills.SkillType.None.ToString());
			return list;
		}, false, false, false);
		new Terminal.ConsoleCommand("sleep", "skips to next morning", delegate(Terminal.ConsoleEventArgs args)
		{
			EnvMan.instance.SkipToMorning();
		}, true, false, true, false, false, null, false, true, false);
		new Terminal.ConsoleCommand("stats", "shows player stats", delegate(Terminal.ConsoleEventArgs args)
		{
			if (!Game.instance)
			{
				return;
			}
			PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
			args.Context.AddString("Player stats");
			if (playerProfile.m_usedCheats)
			{
				args.Context.AddString("Cheater!");
			}
			foreach (KeyValuePair<PlayerStatType, float> keyValuePair in playerProfile.m_playerStats.m_stats)
			{
				string str;
				if (PlayerProfile.m_statTypeDates.TryGetValue(keyValuePair.Key, out str))
				{
					args.Context.AddString("  " + str);
				}
				args.Context.AddString(string.Format("    {0}: {1}", keyValuePair.Key, keyValuePair.Value));
			}
			args.Context.AddString("Known worlds:");
			foreach (KeyValuePair<string, float> keyValuePair2 in playerProfile.m_knownWorlds)
			{
				args.Context.AddString("  " + keyValuePair2.Key + ": " + TimeSpan.FromSeconds((double)keyValuePair2.Value).ToString("c"));
			}
			args.Context.AddString("Enemies:");
			foreach (KeyValuePair<string, float> keyValuePair3 in playerProfile.m_enemyStats)
			{
				args.Context.AddString(string.Format("  {0}: {1}", Localization.instance.Localize(keyValuePair3.Key), keyValuePair3.Value));
			}
			args.Context.AddString("Items found:");
			foreach (KeyValuePair<string, float> keyValuePair4 in playerProfile.m_itemPickupStats)
			{
				args.Context.AddString(string.Format("  {0}: {1}", Localization.instance.Localize(keyValuePair4.Key), keyValuePair4.Value));
			}
			args.Context.AddString("Crafts:");
			foreach (KeyValuePair<string, float> keyValuePair5 in playerProfile.m_itemCraftStats)
			{
				args.Context.AddString(string.Format("  {0}: {1}", Localization.instance.Localize(keyValuePair5.Key), keyValuePair5.Value));
			}
			if (args.Length > 1)
			{
				args.Context.AddString("Known world keys:");
				foreach (KeyValuePair<string, float> keyValuePair6 in playerProfile.m_knownWorldKeys)
				{
					args.Context.AddString("  " + keyValuePair6.Key + ": " + TimeSpan.FromSeconds((double)keyValuePair6.Value).ToString("c"));
				}
				args.Context.AddString("Used commands:");
				foreach (KeyValuePair<string, float> keyValuePair7 in playerProfile.m_knownCommands)
				{
					args.Context.AddString(string.Format("  {0}: {1}", keyValuePair7.Key, keyValuePair7.Value));
				}
			}
		}, false, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("skiptime", "[gameseconds] skips head in seconds", delegate(Terminal.ConsoleEventArgs args)
		{
			double num = ZNet.instance.GetTimeSeconds();
			float num2 = args.TryParameterFloat(1, 240f);
			num += (double)num2;
			ZNet.instance.SetNetTime(num);
			args.Context.AddString("Skipping " + num2.ToString("0") + "s , Day:" + EnvMan.instance.GetDay(num).ToString());
		}, true, false, true, false, false, null, false, true, false);
		new Terminal.ConsoleCommand("time", "shows current time", delegate(Terminal.ConsoleEventArgs args)
		{
			double timeSeconds = ZNet.instance.GetTimeSeconds();
			bool flag = EnvMan.CanSleep();
			args.Context.AddString(string.Format("{0} sec, Day: {1} ({2}), {3}, Session start: {4}", new object[]
			{
				timeSeconds.ToString("0.00"),
				EnvMan.instance.GetDay(timeSeconds),
				EnvMan.instance.GetDayFraction().ToString("0.00"),
				flag ? "Can sleep" : "Can NOT sleep",
				ZoneSystem.instance.TimeSinceStart()
			}));
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("maxfps", "[FPS] sets fps limit", delegate(Terminal.ConsoleEventArgs args)
		{
			int num;
			if (args.TryParameterInt(1, out num))
			{
				Settings.FPSLimit = num;
				PlatformPrefs.SetInt("FPSLimit", num);
				return true;
			}
			return false;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("resetcharacter", "reset character data", delegate(Terminal.ConsoleEventArgs args)
		{
			Terminal context = args.Context;
			if (context != null)
			{
				context.AddString("Reseting character");
			}
			Player.m_localPlayer.ResetCharacter();
		}, true, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("resetknownitems", "reset character known items & recipes", delegate(Terminal.ConsoleEventArgs args)
		{
			Terminal context = args.Context;
			if (context != null)
			{
				context.AddString("Reseting known items for character");
			}
			Player.m_localPlayer.ResetCharacterKnownItems();
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("tutorialreset", "reset tutorial data", delegate(Terminal.ConsoleEventArgs args)
		{
			Terminal context = args.Context;
			if (context != null)
			{
				context.AddString("Reseting tutorials");
			}
			Player.ResetSeenTutorials();
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("timescale", "[target] [fadetime, default: 1, max: 3] sets timescale", delegate(Terminal.ConsoleEventArgs args)
		{
			float b;
			if (args.TryParameterFloat(1, out b))
			{
				Game.FadeTimeScale(Mathf.Min(5f, b), args.TryParameterFloat(2, 0f));
				return true;
			}
			return false;
		}, true, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("randomevent", "start a random event", delegate(Terminal.ConsoleEventArgs args)
		{
			RandEventSystem.instance.StartRandomEvent();
		}, true, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("event", "[name] - start event", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text = args[1];
			if (!RandEventSystem.instance.HaveEvent(text))
			{
				args.Context.AddString("Random event not found:" + text);
				return true;
			}
			RandEventSystem.instance.SetRandomEventByName(text, Player.m_localPlayer.transform.position);
			return true;
		}, true, false, true, false, false, delegate()
		{
			List<string> list = new List<string>();
			foreach (RandomEvent randomEvent in RandEventSystem.instance.m_events)
			{
				list.Add(randomEvent.m_name);
			}
			return list;
		}, false, false, false);
		new Terminal.ConsoleCommand("stopevent", "stop current event", delegate(Terminal.ConsoleEventArgs args)
		{
			RandEventSystem.instance.ResetRandomEvent();
		}, true, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("removedrops", "remove all item-drops in area", delegate(Terminal.ConsoleEventArgs args)
		{
			int num = 0;
			foreach (ItemDrop itemDrop in UnityEngine.Object.FindObjectsOfType<ItemDrop>())
			{
				Fish component = itemDrop.gameObject.GetComponent<Fish>();
				if ((!component || component.IsOutOfWater()) && !itemDrop.IsPiece())
				{
					ZNetView component2 = itemDrop.GetComponent<ZNetView>();
					if (component2 && component2.IsValid() && component2.IsOwner())
					{
						component2.Destroy();
						num++;
					}
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Removed item drops: " + num.ToString(), 0, null);
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("removefish", "remove all fish", delegate(Terminal.ConsoleEventArgs args)
		{
			int num = 0;
			Fish[] array = UnityEngine.Object.FindObjectsOfType<Fish>();
			for (int j = 0; j < array.Length; j++)
			{
				ZNetView component = array[j].GetComponent<ZNetView>();
				if (component && component.IsValid() && component.IsOwner())
				{
					component.Destroy();
					num++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Removed fish: " + num.ToString(), 0, null);
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("printcreatures", "shows counts and levels of active creatures", delegate(Terminal.ConsoleEventArgs args)
		{
			Terminal.<>c__DisplayClass7_0 CS$<>8__locals2;
			CS$<>8__locals2.args = args;
			CS$<>8__locals2.counts = new Dictionary<string, Dictionary<int, int>>();
			Terminal.<InitTerminal>g__GetInfo|7_135(Character.GetAllCharacters(), ref CS$<>8__locals2);
			Terminal.<InitTerminal>g__GetInfo|7_135(UnityEngine.Object.FindObjectsOfType<RandomFlyingBird>(), ref CS$<>8__locals2);
			Terminal.<InitTerminal>g__GetInfo|7_135(UnityEngine.Object.FindObjectsOfType<Fish>(), ref CS$<>8__locals2);
			foreach (KeyValuePair<string, Dictionary<int, int>> keyValuePair in CS$<>8__locals2.counts)
			{
				string text = Localization.instance.Localize(keyValuePair.Key) + ": ";
				foreach (KeyValuePair<int, int> keyValuePair2 in keyValuePair.Value)
				{
					text += string.Format("Level {0}: {1}, ", keyValuePair2.Key, keyValuePair2.Value);
				}
				CS$<>8__locals2.args.Context.AddString(text);
			}
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("printnetobj", "[radius = 5] lists number of network objects by name surrounding the player", delegate(Terminal.ConsoleEventArgs args)
		{
			float num = args.TryParameterFloat(1, 5f);
			ZNetView[] array = UnityEngine.Object.FindObjectsOfType<ZNetView>();
			Terminal.<>c__DisplayClass7_1 CS$<>8__locals2;
			CS$<>8__locals2.counts = new Dictionary<string, int>();
			CS$<>8__locals2.total = 0;
			foreach (ZNetView znetView in array)
			{
				Transform transform = (znetView.transform.parent != null) ? znetView.transform.parent : znetView.transform;
				if (num <= 0f || Vector3.Distance(transform.position, Player.m_localPlayer.transform.position) <= num)
				{
					string name = transform.name;
					int num2 = name.IndexOf('(');
					if (num2 > 0)
					{
						Terminal.<InitTerminal>g__add|7_137(name.Substring(0, num2), ref CS$<>8__locals2);
					}
					else
					{
						Terminal.<InitTerminal>g__add|7_137("Other", ref CS$<>8__locals2);
					}
				}
			}
			args.Context.AddString(string.Format("Total network objects found: {0}", CS$<>8__locals2.total));
			foreach (KeyValuePair<string, int> keyValuePair in CS$<>8__locals2.counts)
			{
				args.Context.AddString(string.Format("   {0}: {1}", keyValuePair.Key, keyValuePair.Value));
			}
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("removebirds", "remove all birds", delegate(Terminal.ConsoleEventArgs args)
		{
			int num = 0;
			RandomFlyingBird[] array = UnityEngine.Object.FindObjectsOfType<RandomFlyingBird>();
			for (int j = 0; j < array.Length; j++)
			{
				ZNetView component = array[j].GetComponent<ZNetView>();
				if (component && component.IsValid() && component.IsOwner())
				{
					component.Destroy();
					num++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Removed birds: " + num.ToString(), 0, null);
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("printlocations", "shows counts of loaded locations", delegate(Terminal.ConsoleEventArgs args)
		{
			new Dictionary<string, Dictionary<int, int>>();
			foreach (Location location in UnityEngine.Object.FindObjectsOfType<Location>())
			{
				args.Context.AddString(string.Format("   {0}, Dist: {1}, Offset: {2}", location.name, Vector3.Distance(Player.m_localPlayer.transform.position, location.transform.position).ToString("0.0"), location.transform.position - Player.m_localPlayer.transform.position));
			}
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("find", "[text] [pingmax] searches loaded objects and location list matching name and pings them on the map. pingmax defaults to 1, if more will place pins on map instead", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text = args[1].ToLower();
			List<Tuple<object, Vector3>> list = Terminal.<InitTerminal>g__find|7_65(text);
			list.Sort((Tuple<object, Vector3> a, Tuple<object, Vector3> b) => Vector3.Distance(a.Item2, Player.m_localPlayer.transform.position).CompareTo(Vector3.Distance(b.Item2, Player.m_localPlayer.transform.position)));
			foreach (Tuple<object, Vector3> tuple in list)
			{
				Terminal context = args.Context;
				string format = "   {0}, Dist: {1}, Pos: {2}";
				GameObject gameObject = tuple.Item1 as GameObject;
				object arg;
				if (gameObject == null)
				{
					object item = tuple.Item1;
					if (item is ZoneSystem.LocationInstance)
					{
						ZoneSystem.LocationInstance locationInstance = (ZoneSystem.LocationInstance)item;
						arg = locationInstance.m_location.m_prefab.Name;
					}
					else
					{
						arg = "unknown";
					}
				}
				else
				{
					arg = gameObject.name.ToString();
				}
				context.AddString(string.Format(format, arg, Vector3.Distance(Player.m_localPlayer.transform.position, tuple.Item2).ToString("0.0"), tuple.Item2));
			}
			foreach (Minimap.PinData pin in args.Context.m_findPins)
			{
				Minimap.instance.RemovePin(pin);
			}
			args.Context.m_findPins.Clear();
			int num = Math.Min(list.Count, args.TryParameterInt(2, 1));
			if (num == 1)
			{
				Chat.instance.SendPing(list[0].Item2);
			}
			else
			{
				for (int j = 0; j < num; j++)
				{
					List<Minimap.PinData> findPins = args.Context.m_findPins;
					Minimap instance = Minimap.instance;
					Vector3 item2 = list[j].Item2;
					Minimap.PinType type = (list[j].Item1 is ZDO) ? Minimap.PinType.Icon2 : ((list[j].Item1 is ZoneSystem.LocationInstance) ? Minimap.PinType.Icon1 : Minimap.PinType.Icon3);
					ZDO zdo = list[j].Item1 as ZDO;
					findPins.Add(instance.AddPin(item2, type, (zdo != null) ? zdo.GetString("tag", "") : "", false, true, Player.m_localPlayer.GetPlayerID(), default(PlatformUserID)));
				}
			}
			args.Context.AddString(string.Format("Found {0} objects containing '{1}'", list.Count, text));
			return true;
		}, true, false, false, false, false, new Terminal.ConsoleOptionsFetcher(Terminal.<InitTerminal>g__findOpt|7_64), false, false, false);
		new Terminal.ConsoleCommand("findtp", "[text] [index=-1] [closerange=30] searches loaded objects and location list matching name and teleports you to the closest one outside of closerange. Specify an index to tp to any other in the found list, a minus value means index by closest.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2 || Player.m_localPlayer == null)
			{
				return false;
			}
			string text = args[1].ToLower();
			if (text.Length < 1)
			{
				args.Context.AddString("You must specify a search query");
				return false;
			}
			List<Tuple<object, Vector3>> list = Terminal.<InitTerminal>g__find|7_65(text);
			int num = args.TryParameterInt(2, -1);
			if (num < 0)
			{
				list.Sort((Tuple<object, Vector3> a, Tuple<object, Vector3> b) => Vector3.Distance(a.Item2, Player.m_localPlayer.transform.position).CompareTo(Vector3.Distance(b.Item2, Player.m_localPlayer.transform.position)));
				num *= -1;
				num--;
			}
			num = Math.Min(list.Count - 1, num);
			if (list.Count > 0)
			{
				int num2 = args.TryParameterInt(3, 30);
				for (int j = num; j < list.Count; j++)
				{
					if (Vector3.Distance(Player.m_localPlayer.transform.position, list[j].Item2) >= (float)num2)
					{
						Player.m_localPlayer.TeleportTo(list[j].Item2, Player.m_localPlayer.transform.rotation, true);
					}
				}
			}
			args.Context.AddString(string.Format("Found {0} objects containing '{1}'", list.Count, text));
			return true;
		}, true, false, false, false, false, new Terminal.ConsoleOptionsFetcher(Terminal.<InitTerminal>g__findOpt|7_64), false, false, false);
		new Terminal.ConsoleCommand("setfuel", "[amount=10] Sets all light fuel to specified amount", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			UnityEngine.Object[] array = UnityEngine.Object.FindObjectsOfType(typeof(Fireplace));
			int num = args.TryParameterInt(1, 10);
			UnityEngine.Object[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				((Fireplace)array2[j]).SetFuel((float)num);
			}
			return true;
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("freefly", "freefly photo mode", delegate(Terminal.ConsoleEventArgs args)
		{
			args.Context.AddString("Toggling free fly camera");
			GameCamera.instance.ToggleFreeFly();
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("ffsmooth", "freefly smoothness", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length <= 1)
			{
				args.Context.AddString(GameCamera.instance.GetFreeFlySmoothness().ToString());
				return true;
			}
			float freeFlySmoothness;
			if (args.TryParameterFloat(1, out freeFlySmoothness))
			{
				args.Context.AddString("Setting free fly camera smoothing:" + freeFlySmoothness.ToString());
				GameCamera.instance.SetFreeFlySmoothness(freeFlySmoothness);
				return true;
			}
			return false;
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("location", "[SAVE*] spawn location (CAUTION: saving permanently disabled, *unless you specify SAVE)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string name = args[1];
			Vector3 pos = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 10f;
			ZoneSystem.instance.TestSpawnLocation(name, pos, args.Length < 3 || args[2] != "SAVE");
			return true;
		}, true, false, false, false, false, delegate()
		{
			List<string> list = new List<string>();
			foreach (ZoneSystem.ZoneLocation zoneLocation in ZoneSystem.instance.m_locations)
			{
				if (zoneLocation.m_prefab.IsValid)
				{
					list.Add(zoneLocation.m_prefab.Name);
				}
			}
			return list;
		}, false, false, true);
		new Terminal.ConsoleCommand("vegetation", "spawn vegetation", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			Vector3 vector = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f;
			string b = args[1].ToLower();
			foreach (ZoneSystem.ZoneVegetation zoneVegetation in ZoneSystem.instance.m_vegetation)
			{
				if (zoneVegetation.m_prefab.name.ToLower() == b)
				{
					float y = (float)UnityEngine.Random.Range(0, 360);
					float num = UnityEngine.Random.Range(zoneVegetation.m_scaleMin, zoneVegetation.m_scaleMax);
					float x = UnityEngine.Random.Range(-zoneVegetation.m_randTilt, zoneVegetation.m_randTilt);
					float z = UnityEngine.Random.Range(-zoneVegetation.m_randTilt, zoneVegetation.m_randTilt);
					Vector3 vector2;
					Heightmap.Biome biome;
					Heightmap.BiomeArea biomeArea;
					Heightmap heightmap;
					ZoneSystem.instance.GetGroundData(ref vector, out vector2, out biome, out biomeArea, out heightmap);
					float y2;
					Vector3 vector3;
					if (zoneVegetation.m_snapToStaticSolid && ZoneSystem.instance.GetStaticSolidHeight(vector, out y2, out vector3))
					{
						vector.y = y2;
						vector2 = vector3;
					}
					if (zoneVegetation.m_snapToWater)
					{
						vector.y = 30f;
					}
					vector.y += zoneVegetation.m_groundOffset;
					Quaternion rotation = Quaternion.identity;
					if (zoneVegetation.m_chanceToUseGroundTilt > 0f && UnityEngine.Random.value <= zoneVegetation.m_chanceToUseGroundTilt)
					{
						Quaternion rotation2 = Quaternion.Euler(0f, y, 0f);
						rotation = Quaternion.LookRotation(Vector3.Cross(vector2, rotation2 * Vector3.forward), vector2);
					}
					else
					{
						rotation = Quaternion.Euler(x, y, z);
					}
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(zoneVegetation.m_prefab, vector, rotation);
					gameObject.GetComponent<ZNetView>().SetLocalScale(new Vector3(num, num, num));
					foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>())
					{
						collider.enabled = false;
						collider.enabled = true;
					}
					return true;
				}
			}
			return "No vegeration prefab named '" + args[1] + "' found";
		}, true, false, false, false, false, delegate()
		{
			List<string> list = new List<string>();
			foreach (ZoneSystem.ZoneVegetation zoneVegetation in ZoneSystem.instance.m_vegetation)
			{
				if (zoneVegetation.m_prefab != null)
				{
					list.Add(zoneVegetation.m_prefab.name);
				}
			}
			return list;
		}, false, false, true);
		new Terminal.ConsoleCommand("nextseed", "forces the next dungeon to a seed (CAUTION: saving permanently disabled)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return true;
			}
			int forceSeed;
			if (args.TryParameterInt(1, out forceSeed))
			{
				DungeonGenerator.m_forceSeed = forceSeed;
				ZoneSystem.instance.m_didZoneTest = true;
				MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Location seed set, world saving DISABLED until restart", 0, null, false);
			}
			return true;
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("spawn", "[amount] [level] [radius] [p/e/i] - spawn something. (End word with a star (*) to create each object containing that word.) Add a 'p' after to try to pick up the spawned items, adding 'e' will try to use/equip, 'i' will only spawn and pickup if you don't have one in your inventory.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length <= 1 || !ZNetScene.instance)
			{
				return false;
			}
			string text = args[1];
			Terminal.<>c__DisplayClass7_2 CS$<>8__locals2;
			CS$<>8__locals2.count = args.TryParameterInt(2, 1);
			CS$<>8__locals2.level = args.TryParameterInt(3, 1);
			CS$<>8__locals2.radius = args.TryParameterFloat(4, 0.5f);
			args.TryParameterInt(5, -1);
			CS$<>8__locals2.pickup = args.HasArgumentAnywhere("p", 2, true);
			CS$<>8__locals2.use = args.HasArgumentAnywhere("e", 2, true);
			CS$<>8__locals2.onlyIfMissing = args.HasArgumentAnywhere("i", 2, true);
			CS$<>8__locals2.vals = null;
			foreach (string text2 in args.Args)
			{
				if (text2.Contains("::"))
				{
					string[] array = text2.Split(new string[]
					{
						"::"
					}, StringSplitOptions.None);
					string[] array2 = array[0].Split('.', StringSplitOptions.None);
					if (array.Length >= 2 && array2.Length >= 2)
					{
						if (CS$<>8__locals2.vals == null)
						{
							CS$<>8__locals2.vals = new Dictionary<string, object>();
						}
						int num;
						bool flag;
						float num2;
						float x;
						float y;
						float z;
						if (int.TryParse(array[1], out num))
						{
							CS$<>8__locals2.vals[array[0]] = num;
						}
						else if (bool.TryParse(array[1], out flag))
						{
							CS$<>8__locals2.vals[array[0]] = flag;
						}
						else if (float.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out num2))
						{
							CS$<>8__locals2.vals[array[0]] = num2;
						}
						else if (array.Length >= 4 && float.TryParse(array[1], out x) && float.TryParse(array[2], out y) && float.TryParse(array[3], out z))
						{
							CS$<>8__locals2.vals[array[0]] = new Vector3(x, y, z);
						}
						else
						{
							CS$<>8__locals2.vals[array[0]] = array[1];
						}
					}
				}
			}
			DateTime now = DateTime.Now;
			if (text.Length >= 2 && text[text.Length - 1] == '*')
			{
				text = text.Substring(0, text.Length - 1).ToLower();
				using (List<string>.Enumerator enumerator = ZNetScene.instance.GetPrefabNames().GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						string text3 = enumerator.Current;
						string text4 = text3.ToLower();
						if (text4.Contains(text) && (text.Contains("fx") || !text4.Contains("fx")))
						{
							Terminal.<InitTerminal>g__spawn|7_140(text3, ref CS$<>8__locals2);
						}
					}
					goto IL_2BA;
				}
			}
			Terminal.<InitTerminal>g__spawn|7_140(text, ref CS$<>8__locals2);
			IL_2BA:
			ZLog.Log("Spawn time :" + (DateTime.Now - now).TotalMilliseconds.ToString() + " ms");
			Gogan.LogEvent("Cheat", "Spawn", text, (long)CS$<>8__locals2.count);
			return true;
		}, true, false, false, false, false, delegate()
		{
			if (!ZNetScene.instance)
			{
				return new List<string>();
			}
			return ZNetScene.instance.GetPrefabNames();
		}, false, false, true);
		new Terminal.ConsoleCommand("catch", "[fishname] [level] simulates catching a fish", delegate(Terminal.ConsoleEventArgs args)
		{
			string text = args[1];
			int num = args.TryParameterInt(2, 1);
			num = Mathf.Min(num, 4);
			GameObject prefab = ZNetScene.instance.GetPrefab(text);
			if (!prefab)
			{
				return "No prefab named: " + text;
			}
			Fish componentInChildren = prefab.GetComponentInChildren<Fish>();
			if (!componentInChildren)
			{
				return "No fish prefab named: " + text;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, Player.m_localPlayer.transform.position, Quaternion.identity);
			componentInChildren = gameObject.GetComponentInChildren<Fish>();
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			if (component)
			{
				component.SetQuality(num);
			}
			string msg = FishingFloat.Catch(componentInChildren, Player.m_localPlayer);
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg, 0, null);
			return true;
		}, true, false, false, false, false, () => new List<string>
		{
			"Fish1",
			"Fish2",
			"Fish3",
			"Fish4_cave",
			"Fish5",
			"Fish6",
			"Fish7",
			"Fish8",
			"Fish9",
			"Fish10",
			"Fish11",
			"Fish12"
		}, false, false, false);
		new Terminal.ConsoleCommand("itemset", "[name] [item level override] [keep] - spawn a premade named set, add 'keep' to not drop current items.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				ItemSets.instance.TryGetSet(args.Args[1], !args.HasArgumentAnywhere("keep", 0, true), args.TryParameterInt(2, -1), args.TryParameterInt(3, -1));
				return true;
			}
			return "Specify name of itemset.";
		}, true, false, false, false, false, () => ItemSets.instance.GetSetNames(), false, false, true);
		new Terminal.ConsoleCommand("pos", "print current player position", delegate(Terminal.ConsoleEventArgs args)
		{
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer && ZoneSystem.instance)
			{
				Terminal context = args.Context;
				if (context == null)
				{
					return;
				}
				context.AddString(string.Format("Player position (X,Y,Z): {0} (Zone: {1})", localPlayer.transform.position.ToString("F0"), ZoneSystem.GetZone(localPlayer.transform.position)));
			}
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("recall", "[*name] recalls players to you, optionally that match given name", delegate(Terminal.ConsoleEventArgs args)
		{
			foreach (ZNetPeer znetPeer in ZNet.instance.GetPeers())
			{
				if (znetPeer.m_playerName != Player.m_localPlayer.GetPlayerName() && (args.Length < 2 || znetPeer.m_playerName.ToLower().Contains(args[1].ToLower())))
				{
					Chat.instance.TeleportPlayer(znetPeer.m_uid, Player.m_localPlayer.transform.position, Player.m_localPlayer.transform.rotation, true);
				}
			}
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("goto", "[x,z] - teleport", delegate(Terminal.ConsoleEventArgs args)
		{
			int num;
			int num2;
			if (args.Length < 3 || !args.TryParameterInt(1, out num) || !args.TryParameterInt(2, out num2))
			{
				return false;
			}
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer)
			{
				Vector3 vector = new Vector3((float)num, localPlayer.transform.position.y, (float)num2);
				float max = localPlayer.IsDebugFlying() ? 400f : 30f;
				vector.y = Mathf.Clamp(vector.y, 30f, max);
				localPlayer.TeleportTo(vector, localPlayer.transform.rotation, true);
			}
			Gogan.LogEvent("Cheat", "Goto", "", 0L);
			return true;
		}, true, false, false, false, true, null, false, false, true);
		new Terminal.ConsoleCommand("exploremap", "explore entire map", delegate(Terminal.ConsoleEventArgs args)
		{
			Minimap.instance.ExploreAll();
		}, true, false, true, false, true, null, false, false, false);
		new Terminal.ConsoleCommand("resetmap", "reset map exploration", delegate(Terminal.ConsoleEventArgs args)
		{
			Minimap.instance.Reset();
		}, true, false, true, false, true, null, false, false, false);
		new Terminal.ConsoleCommand("resetsharedmap", "removes any shared map data from cartography table", delegate(Terminal.ConsoleEventArgs args)
		{
			Minimap.instance.ResetSharedMapData();
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("restartparty", "restart playfab party network", delegate(Terminal.ConsoleEventArgs args)
		{
			if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
			{
				if (ZNet.instance.IsServer())
				{
					ZPlayFabMatchmaking.ResetParty();
					return;
				}
				ZPlayFabSocket.ScheduleResetParty();
			}
		}, false, false, false, false, false, null, false, true, false);
		new Terminal.ConsoleCommand("puke", "empties your stomach of food", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer)
			{
				Player.m_localPlayer.ClearFood();
			}
		}, true, false, false, false, true, null, false, false, true);
		new Terminal.ConsoleCommand("tame", "tame all nearby tameable creatures", delegate(Terminal.ConsoleEventArgs args)
		{
			Tameable.TameAllInArea(Player.m_localPlayer.transform.position, 20f);
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("aggravate", "aggravated all nearby neutrals", delegate(Terminal.ConsoleEventArgs args)
		{
			BaseAI.AggravateAllInArea(Player.m_localPlayer.transform.position, 20f, BaseAI.AggravatedReason.Damage);
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("killall", "kill nearby creatures", delegate(Terminal.ConsoleEventArgs args)
		{
			List<Character> allCharacters = Character.GetAllCharacters();
			int num = 0;
			int num2 = 0;
			foreach (Character character in allCharacters)
			{
				if (!character.IsPlayer())
				{
					character.Damage(new HitData(1E+10f));
					num++;
				}
			}
			SpawnArea[] array = UnityEngine.Object.FindObjectsByType<SpawnArea>(FindObjectsSortMode.None);
			for (int j = 0; j < array.Length; j++)
			{
				Destructible component = array[j].gameObject.GetComponent<Destructible>();
				if (component != null)
				{
					component.Damage(new HitData(1E+10f));
					num2++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format("Killed {0} monsters{1}", num, (num2 > 0) ? string.Format(" & {0} spawners.", num2) : "."), 0, null);
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("killenemycreatures", "kill nearby enemies", delegate(Terminal.ConsoleEventArgs args)
		{
			List<Character> allCharacters = Character.GetAllCharacters();
			int num = 0;
			foreach (Character character in allCharacters)
			{
				if (!character.IsPlayer() && !character.IsTamed())
				{
					character.Damage(new HitData(1E+10f));
					num++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format("Killed {0} monsters.", num), 0, null);
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("killenemies", "kill nearby enemies", delegate(Terminal.ConsoleEventArgs args)
		{
			List<Character> allCharacters = Character.GetAllCharacters();
			int num = 0;
			int num2 = 0;
			foreach (Character character in allCharacters)
			{
				if (!character.IsPlayer() && !character.IsTamed())
				{
					character.Damage(new HitData(1E+10f));
					num++;
				}
			}
			SpawnArea[] array = UnityEngine.Object.FindObjectsByType<SpawnArea>(FindObjectsSortMode.None);
			for (int j = 0; j < array.Length; j++)
			{
				Destructible component = array[j].gameObject.GetComponent<Destructible>();
				if (component != null)
				{
					component.Damage(new HitData(1E+10f));
					num2++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format("Killed {0} monsters{1}", num, (num2 > 0) ? string.Format(" & {0} spawners.", num2) : "."), 0, null);
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("killtame", "kill nearby tame creatures.", delegate(Terminal.ConsoleEventArgs args)
		{
			List<Character> allCharacters = Character.GetAllCharacters();
			int num = 0;
			foreach (Character character in allCharacters)
			{
				if (!character.IsPlayer() && character.IsTamed())
				{
					HitData hitData = new HitData();
					hitData.m_damage.m_damage = 1E+10f;
					character.Damage(hitData);
					num++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Killing all tame creatures:" + num.ToString(), 0, null);
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("heal", "heal to full health & stamina", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return;
			}
			Player.m_localPlayer.Heal(Player.m_localPlayer.GetMaxHealth(), true);
			Player.m_localPlayer.AddStamina(Player.m_localPlayer.GetMaxStamina());
			Player.m_localPlayer.AddEitr(Player.m_localPlayer.GetMaxEitr());
		}, true, false, false, false, true, null, false, false, true);
		new Terminal.ConsoleCommand("god", "invincible mode", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.SetGodMode(args.HasArgumentAnywhere("on", 0, true) || (!args.HasArgumentAnywhere("off", 0, true) && !Player.m_localPlayer.InGodMode()));
			args.Context.AddString("God mode:" + Player.m_localPlayer.InGodMode().ToString());
			Gogan.LogEvent("Cheat", "God", Player.m_localPlayer.InGodMode().ToString(), 0L);
		}, true, false, false, false, true, null, false, false, true);
		new Terminal.ConsoleCommand("ghost", "", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.SetGhostMode(args.HasArgumentAnywhere("on", 0, true) || (!args.HasArgumentAnywhere("off", 0, true) && !Player.m_localPlayer.InGhostMode()));
			args.Context.AddString("Ghost mode:" + Player.m_localPlayer.InGhostMode().ToString());
			Gogan.LogEvent("Cheat", "Ghost", Player.m_localPlayer.InGhostMode().ToString(), 0L);
		}, true, false, false, false, true, null, false, false, true);
		new Terminal.ConsoleCommand("beard", "change beard", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			if (Player.m_localPlayer)
			{
				Player.m_localPlayer.SetBeard(args[1]);
			}
			return true;
		}, true, false, false, false, false, delegate()
		{
			List<string> list = new List<string>();
			foreach (ItemDrop itemDrop in ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard"))
			{
				list.Add(itemDrop.name);
			}
			return list;
		}, false, false, false);
		new Terminal.ConsoleCommand("hair", "change hair", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			if (Player.m_localPlayer)
			{
				Player.m_localPlayer.SetHair(args[1]);
			}
			return true;
		}, true, false, false, false, false, delegate()
		{
			List<string> list = new List<string>();
			foreach (ItemDrop itemDrop in ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair"))
			{
				list.Add(itemDrop.name);
			}
			return list;
		}, false, false, false);
		new Terminal.ConsoleCommand("model", "change player model", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			int playerModel;
			if (Player.m_localPlayer && args.TryParameterInt(1, out playerModel))
			{
				Player.m_localPlayer.SetPlayerModel(playerModel);
			}
			return true;
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("tod", "-1 OR [0-1]", delegate(Terminal.ConsoleEventArgs args)
		{
			float num;
			if (EnvMan.instance == null || args.Length < 2 || !args.TryParameterFloat(1, out num))
			{
				return false;
			}
			args.Context.AddString("Setting time of day:" + num.ToString());
			if (num < 0f)
			{
				EnvMan.instance.m_debugTimeOfDay = false;
			}
			else
			{
				EnvMan.instance.m_debugTimeOfDay = true;
				EnvMan.instance.m_debugTime = Mathf.Clamp01(num);
			}
			return true;
		}, true, false, true, false, true, null, false, false, false);
		new Terminal.ConsoleCommand("env", "[env] override environment", delegate(Terminal.ConsoleEventArgs args)
		{
			if (EnvMan.instance == null || args.Length < 2)
			{
				return false;
			}
			string text = string.Join(" ", args.Args, 1, args.Args.Length - 1);
			args.Context.AddString("Setting debug enviornment:" + text);
			EnvMan.instance.m_debugEnv = text;
			return true;
		}, true, false, true, false, true, delegate()
		{
			List<string> list = new List<string>();
			foreach (EnvSetup envSetup in EnvMan.instance.m_environments)
			{
				list.Add(envSetup.m_name);
			}
			return list;
		}, false, false, false);
		new Terminal.ConsoleCommand("resetenv", "disables environment override", delegate(Terminal.ConsoleEventArgs args)
		{
			if (EnvMan.instance == null)
			{
				return false;
			}
			args.Context.AddString("Resetting debug environment");
			EnvMan.instance.m_debugEnv = "";
			return true;
		}, true, false, true, false, true, null, false, false, false);
		new Terminal.ConsoleCommand("wind", "[angle] [intensity]", delegate(Terminal.ConsoleEventArgs args)
		{
			float angle;
			float intensity;
			if (args.TryParameterFloat(1, out angle) && args.TryParameterFloat(2, out intensity))
			{
				EnvMan.instance.SetDebugWind(angle, intensity);
				return true;
			}
			return false;
		}, true, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("resetwind", "", delegate(Terminal.ConsoleEventArgs args)
		{
			EnvMan.instance.ResetDebugWind();
		}, true, false, true, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("clear", "clear the console window", delegate(Terminal.ConsoleEventArgs args)
		{
			args.Context.m_chatBuffer.Clear();
			args.Context.UpdateChat();
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("filtercraft", "[name] filters crafting list to contain part of text", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length <= 1)
			{
				Player.s_FilterCraft.Clear();
				return;
			}
			Player.s_FilterCraft = args.ArgsAll.Split(' ', StringSplitOptions.None).ToList<string>();
		}, false, false, false, false, true, null, false, false, false);
		new Terminal.ConsoleCommand("clearstatus", "clear any status modifiers", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.ClearHardDeath();
			Player.m_localPlayer.GetSEMan().RemoveAllStatusEffects(false);
		}, true, false, false, false, true, null, false, false, true);
		new Terminal.ConsoleCommand("addstatus", "[name] adds a status effect (ex: Rested, Burning, SoftDeath, Wet, etc)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			Player.m_localPlayer.GetSEMan().AddStatusEffect(args[1].GetStableHashCode(), true, 0, 0f);
			return true;
		}, true, false, false, false, true, delegate()
		{
			List<StatusEffect> statusEffects = ObjectDB.instance.m_StatusEffects;
			List<string> list = new List<string>();
			foreach (StatusEffect statusEffect in statusEffects)
			{
				list.Add(statusEffect.name);
			}
			return list;
		}, false, false, true);
		new Terminal.ConsoleCommand("setpower", "[name] sets your current guardian power and resets cooldown (ex: GP_Eikthyr, GP_TheElder, etc)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			Player.m_localPlayer.SetGuardianPower(args[1]);
			Player.m_localPlayer.m_guardianPowerCooldown = 0f;
			return true;
		}, true, false, false, false, true, delegate()
		{
			List<StatusEffect> statusEffects = ObjectDB.instance.m_StatusEffects;
			List<string> list = new List<string>();
			foreach (StatusEffect statusEffect in statusEffects)
			{
				list.Add(statusEffect.name);
			}
			return list;
		}, false, false, true);
		new Terminal.ConsoleCommand("bind", "[keycode] [command and parameters] bind a key to a console command. note: may cause conflicts with game controls", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			KeyCode keyCode;
			if (!Enum.TryParse<KeyCode>(args[1], true, out keyCode))
			{
				args.Context.AddString("'" + args[1] + "' is not a valid UnityEngine.KeyCode.");
			}
			else
			{
				string item = string.Join(" ", args.Args, 1, args.Length - 1);
				Terminal.m_bindList.Add(item);
				Terminal.updateBinds();
			}
			return true;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("unbind", "[keycode] clears all binds connected to keycode", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			for (int j = Terminal.m_bindList.Count - 1; j >= 0; j--)
			{
				if (Terminal.m_bindList[j].Split(' ', StringSplitOptions.None)[0].ToLower() == args[1].ToLower())
				{
					Terminal.m_bindList.RemoveAt(j);
				}
			}
			Terminal.updateBinds();
			return true;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("printbinds", "prints current binds", delegate(Terminal.ConsoleEventArgs args)
		{
			foreach (string text in Terminal.m_bindList)
			{
				args.Context.AddString(text);
			}
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("resetbinds", "resets all custom binds to default dev commands", delegate(Terminal.ConsoleEventArgs args)
		{
			for (int j = Terminal.m_bindList.Count - 1; j >= 0; j--)
			{
				Terminal.m_bindList.Remove(Terminal.m_bindList[j]);
			}
			Terminal.updateBinds();
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("tombstone", "[name] creates a tombstone with given name", delegate(Terminal.ConsoleEventArgs args)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(Player.m_localPlayer.m_tombstone, Player.m_localPlayer.GetCenterPoint(), Player.m_localPlayer.transform.rotation);
			Container component = gameObject.GetComponent<Container>();
			ItemDrop coinPrefab = StoreGui.instance.m_coinPrefab;
			component.GetInventory().AddItem(coinPrefab.gameObject.name, 1, coinPrefab.m_itemData.m_quality, coinPrefab.m_itemData.m_variant, 0L, "", true);
			TombStone component2 = gameObject.GetComponent<TombStone>();
			PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
			string ownerName = (args.Args.Length >= 2) ? args.Args[1] : playerProfile.GetName();
			component2.Setup(ownerName, playerProfile.GetPlayerID());
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("test", "[key] [value] set test string, with optional value. set empty existing key to remove", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				Terminal.m_showTests = !Terminal.m_showTests;
				return true;
			}
			string text = (args.Length >= 3) ? args[2] : "";
			if (Terminal.m_testList.ContainsKey(args[1]) && text.Length == 0)
			{
				Terminal.m_testList.Remove(args[1]);
				Terminal context = args.Context;
				if (context != null)
				{
					context.AddString("'" + args[1] + "' removed");
				}
			}
			else
			{
				Terminal.m_testList[args[1]] = text;
				Terminal context2 = args.Context;
				if (context2 != null)
				{
					context2.AddString(string.Concat(new string[]
					{
						"'",
						args[1],
						"' added with value '",
						text,
						"'"
					}));
				}
			}
			string a = args[1].ToLower();
			if (!(a == "ngenemyac"))
			{
				if (!(a == "ngenemyhp"))
				{
					if (!(a == "ngenemydamage"))
					{
						if (!(a == "ngplayerac"))
						{
							if (a == "ngplayerdamage")
							{
								Game.instance.m_worldLevelGearBaseDamage = int.Parse(args[2]);
							}
						}
						else
						{
							Game.instance.m_worldLevelGearBaseAC = int.Parse(args[2]);
						}
					}
					else
					{
						Game.instance.m_worldLevelEnemyBaseDamage = int.Parse(args[2]);
					}
				}
				else
				{
					Game.instance.m_worldLevelEnemyHPMultiplier = float.Parse(args[2]);
				}
			}
			else
			{
				Game.instance.m_worldLevelEnemyBaseAC = int.Parse(args[2]);
			}
			return true;
		}, true, false, false, true, false, null, false, false, false);
		new Terminal.ConsoleCommand("forcedelete", "[radius] [*name] force remove all objects within given radius. If name is entered, only deletes items with matching names. Caution! Use at your own risk. Make backups! Radius default: 5, max: 50.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			float num = Math.Min(50f, args.TryParameterFloat(1, 5f));
			foreach (GameObject gameObject in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
			{
				if (Vector3.Distance(gameObject.transform.position, Player.m_localPlayer.transform.position) < num)
				{
					string path = gameObject.gameObject.transform.GetPath();
					if (!(gameObject.GetComponentInParent<Game>() != null) && !(gameObject.GetComponentInParent<Player>() != null) && !(gameObject.GetComponentInParent<Valkyrie>() != null) && !(gameObject.GetComponentInParent<LocationProxy>() != null) && !(gameObject.GetComponentInParent<Room>() != null) && !(gameObject.GetComponentInParent<Vegvisir>() != null) && !(gameObject.GetComponentInParent<DungeonGenerator>() != null) && !path.Contains("StartTemple") && !path.Contains("BossStone") && (args.Length <= 2 || gameObject.name.ToLower().Contains(args[2].ToLower())))
					{
						Destructible component = gameObject.GetComponent<Destructible>();
						ZNetView component2 = gameObject.GetComponent<ZNetView>();
						if (component != null)
						{
							component.DestroyNow();
						}
						else if (component2 != null && ZNetScene.instance)
						{
							ZNetScene.instance.Destroy(gameObject);
						}
					}
				}
			}
			return true;
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("stopfire", "Puts out all spreading fires and smoke", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			Terminal.RemoveObj(UnityEngine.Object.FindObjectsOfType(typeof(Fire)));
			Terminal.RemoveObj(UnityEngine.Object.FindObjectsOfType(typeof(Smoke)));
			return true;
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("stopsmoke", "Puts out all spreading fires", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			foreach (Fire fire in UnityEngine.Object.FindObjectsOfType(typeof(Fire)))
			{
				Destructible component = fire.GetComponent<Destructible>();
				ZNetView component2 = fire.GetComponent<ZNetView>();
				if (component != null)
				{
					component.DestroyNow();
				}
				else if (component2 != null && ZNetScene.instance)
				{
					ZNetScene.instance.Destroy(fire.gameObject);
				}
			}
			return true;
		}, true, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("printseeds", "print seeds of loaded dungeons", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			Math.Min(20f, args.TryParameterFloat(1, 5f));
			UnityEngine.Object[] array = UnityEngine.Object.FindObjectsOfType(typeof(DungeonGenerator));
			args.Context.AddString(string.Format("{0} version {1}, world seed: {2}/{3}", new object[]
			{
				(ZNet.instance && ZNet.instance.IsServer()) ? "Server" : "Client",
				global::Version.GetVersionString(false),
				ZNet.World.m_seed,
				ZNet.World.m_seedName
			}));
			foreach (DungeonGenerator dungeonGenerator in array)
			{
				args.Context.AddString(string.Format("  {0}: Seed: {1}/{2}, Distance: {3}", new object[]
				{
					dungeonGenerator.name,
					dungeonGenerator.m_generatedSeed,
					dungeonGenerator.GetSeed(),
					Utils.DistanceXZ(Player.m_localPlayer.transform.position, dungeonGenerator.transform.position).ToString("0.0")
				}));
			}
			return true;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("nomap", "disables map for this character. If used as host, will disable for all joining players from now on.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer != null)
			{
				string key = "mapenabled_" + Player.m_localPlayer.GetPlayerName();
				bool flag = PlayerPrefs.GetFloat(key, 1f) == 1f;
				PlayerPrefs.SetFloat(key, (float)(flag ? 0 : 1));
				Minimap.instance.SetMapMode(Minimap.MapMode.None);
				Terminal context = args.Context;
				if (context != null)
				{
					context.AddString("Map " + (flag ? "disabled" : "enabled"));
				}
				if (ZNet.instance && ZNet.instance.IsServer())
				{
					if (flag)
					{
						ZoneSystem.instance.SetGlobalKey(GlobalKeys.NoMap);
						return;
					}
					ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoMap);
				}
			}
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("noportals", "disables portals for server.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer != null)
			{
				bool globalKey = ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals);
				if (globalKey)
				{
					ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoPortals);
				}
				else
				{
					ZoneSystem.instance.SetGlobalKey(GlobalKeys.NoPortals);
				}
				Terminal context = args.Context;
				if (context == null)
				{
					return;
				}
				context.AddString("Portals " + (globalKey ? "enabled" : "disabled"));
			}
		}, false, false, false, false, false, null, false, false, true);
		new Terminal.ConsoleCommand("resetspawn", "resets spawn location", delegate(Terminal.ConsoleEventArgs args)
		{
			if (!Game.instance)
			{
				return false;
			}
			PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
			if (playerProfile != null)
			{
				playerProfile.ClearCustomSpawnPoint();
			}
			Terminal context = args.Context;
			if (context != null)
			{
				context.AddString("Reseting spawn point");
			}
			return true;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("respawntime", "sets respawntime", delegate(Terminal.ConsoleEventArgs args)
		{
			if (!Game.instance)
			{
				return false;
			}
			float fadeTimeDeath;
			if (args.TryParameterFloat(1, out fadeTimeDeath))
			{
				Game.instance.m_respawnLoadDuration = (Game.instance.m_fadeTimeDeath = fadeTimeDeath);
			}
			return true;
		}, true, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("die", "kill yourself", delegate(Terminal.ConsoleEventArgs args)
		{
			if (!Player.m_localPlayer)
			{
				return false;
			}
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = 99999f;
			hitData.m_hitType = HitData.HitType.Self;
			Player.m_localPlayer.Damage(hitData);
			return true;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("say", "chat message", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.FullLine.Length < 5 || Chat.instance == null)
			{
				return false;
			}
			Chat.instance.SendText(Talker.Type.Normal, args.FullLine.Substring(4));
			return true;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("s", "shout message", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.FullLine.Length < 3 || Chat.instance == null)
			{
				return false;
			}
			Chat.instance.SendText(Talker.Type.Shout, args.FullLine.Substring(2));
			return true;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("w", "[playername] whispers a private message to a player", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.FullLine.Length < 3 || Chat.instance == null)
			{
				return false;
			}
			Chat.instance.SendText(Talker.Type.Whisper, args.FullLine.Substring(2));
			return true;
		}, false, false, false, false, false, null, false, false, false);
		new Terminal.ConsoleCommand("resetplayerprefs", "Resets any saved settings and variables (not the save game)", delegate(Terminal.ConsoleEventArgs args)
		{
			PlayerPrefs.DeleteAll();
			Terminal context = args.Context;
			if (context == null)
			{
				return;
			}
			context.AddString("Reset saved player preferences");
		}, false, false, false, true, true, null, false, false, false);
		for (int i = 0; i < 23; i++)
		{
			Emotes emote = (Emotes)i;
			new Terminal.ConsoleCommand(emote.ToString().ToLower(), string.Format("emote: {0}", emote), delegate(Terminal.ConsoleEventArgs args)
			{
				Emote.DoEmote(emote);
			}, false, false, false, false, false, null, false, false, false);
		}
		new Terminal.ConsoleCommand("resetplayerprefs", "Resets any saved settings and variables (not the save game)", delegate(Terminal.ConsoleEventArgs args)
		{
			PlayerPrefs.DeleteAll();
			Terminal context = args.Context;
			if (context == null)
			{
				return;
			}
			context.AddString("Reset saved player preferences");
		}, false, false, false, true, true, null, false, false, false);
	}

	// Token: 0x06000740 RID: 1856 RVA: 0x0003D7AC File Offset: 0x0003B9AC
	private static void RemoveObj(UnityEngine.Object[] objs)
	{
		foreach (MonoBehaviour monoBehaviour in objs)
		{
			Destructible component = monoBehaviour.GetComponent<Destructible>();
			ZNetView component2 = monoBehaviour.GetComponent<ZNetView>();
			if (component != null)
			{
				component.DestroyNow();
			}
			else if (component2 != null && ZNetScene.instance)
			{
				ZNetScene.instance.Destroy(monoBehaviour.gameObject);
			}
			else
			{
				UnityEngine.Object.Destroy(monoBehaviour.gameObject);
			}
		}
	}

	// Token: 0x06000741 RID: 1857 RVA: 0x0003D828 File Offset: 0x0003BA28
	private static void AddConsoleCheatCommands()
	{
		new Terminal.ConsoleCommand("xb:version", "Prints mercurial hashset used for this build", delegate(Terminal.ConsoleEventArgs args)
		{
			Terminal context = args.Context;
			if (context == null)
			{
				return;
			}
			context.AddString("Buildhash: " + global::Version.GetVersionString(true));
		}, false, false, false, false, false, null, false, false, false);
	}

	// Token: 0x06000742 RID: 1858 RVA: 0x0003D870 File Offset: 0x0003BA70
	protected static void updateBinds()
	{
		Terminal.m_binds.Clear();
		foreach (string text in Terminal.m_bindList)
		{
			string[] array = text.Split(' ', StringSplitOptions.None);
			string item = string.Join(" ", array, 1, array.Length - 1);
			KeyCode key;
			if (Enum.TryParse<KeyCode>(array[0], true, out key))
			{
				List<string> list;
				if (Terminal.m_binds.TryGetValue(key, out list))
				{
					list.Add(item);
				}
				else
				{
					Terminal.m_binds[key] = new List<string>
					{
						item
					};
				}
			}
		}
		PlayerPrefs.SetString("ConsoleBindings", string.Join("\n", Terminal.m_bindList));
	}

	// Token: 0x06000743 RID: 1859 RVA: 0x0003D938 File Offset: 0x0003BB38
	private void updateCommandList()
	{
		this.m_commandList.Clear();
		foreach (KeyValuePair<string, Terminal.ConsoleCommand> keyValuePair in Terminal.commands)
		{
			if (keyValuePair.Value.ShowCommand(this) && (this.m_autoCompleteSecrets || !keyValuePair.Value.IsSecret))
			{
				this.m_commandList.Add(keyValuePair.Key);
			}
		}
	}

	// Token: 0x06000744 RID: 1860 RVA: 0x0003D9C8 File Offset: 0x0003BBC8
	public bool IsCheatsEnabled()
	{
		return Terminal.m_cheat && ZNet.instance && ZNet.instance.IsServer();
	}

	// Token: 0x06000745 RID: 1861 RVA: 0x0003D9EC File Offset: 0x0003BBEC
	public void TryRunCommand(string text, bool silentFail = false, bool skipAllowedCheck = false)
	{
		string[] array = text.Split(' ', StringSplitOptions.None);
		Terminal.ConsoleCommand consoleCommand;
		if (Terminal.commands.TryGetValue(array[0].ToLower(), out consoleCommand))
		{
			if (consoleCommand.IsValid(this, skipAllowedCheck))
			{
				consoleCommand.RunAction(new Terminal.ConsoleEventArgs(text, this));
				return;
			}
			if (consoleCommand.RemoteCommand && ZNet.instance && !ZNet.instance.IsServer())
			{
				ZNet.instance.RemoteCommand(text);
				return;
			}
			if (!silentFail)
			{
				this.AddString("'" + text.Split(' ', StringSplitOptions.None)[0] + "' is not valid in the current context.");
				return;
			}
		}
		else if (!silentFail)
		{
			this.AddString("'" + array[0] + "' is not a recognized command. Type 'help' to see a list of valid commands.");
		}
	}

	// Token: 0x06000746 RID: 1862 RVA: 0x0003DA9C File Offset: 0x0003BC9C
	public virtual void Awake()
	{
		Terminal.InitTerminal();
	}

	// Token: 0x06000747 RID: 1863 RVA: 0x0003DAA3 File Offset: 0x0003BCA3
	public virtual void Update()
	{
		if (this.m_focused)
		{
			this.UpdateInput();
		}
	}

	// Token: 0x06000748 RID: 1864 RVA: 0x0003DAB4 File Offset: 0x0003BCB4
	private void UpdateInput()
	{
		if (ZInput.GetButton("JoyButtonX"))
		{
			if (ZInput.GetButtonDown("JoyDPadLeft"))
			{
				this.m_quickSelect[0] = this.m_input.text;
				PlatformPrefs.SetString("quick_save_left", this.m_quickSelect[0]);
				PlayerPrefs.Save();
			}
			if (ZInput.GetButtonDown("JoyDPadRight"))
			{
				this.m_quickSelect[1] = this.m_input.text;
				PlatformPrefs.SetString("quick_save_right", this.m_quickSelect[1]);
				PlayerPrefs.Save();
			}
			if (ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.m_quickSelect[2] = this.m_input.text;
				PlatformPrefs.SetString("quick_save_up", this.m_quickSelect[2]);
				PlayerPrefs.Save();
			}
			if (ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.m_quickSelect[3] = this.m_input.text;
				PlatformPrefs.SetString("quick_save_down", this.m_quickSelect[3]);
				PlayerPrefs.Save();
			}
		}
		else if (ZInput.GetButton("JoyButtonY"))
		{
			if (ZInput.GetButtonDown("JoyDPadLeft"))
			{
				this.m_input.text = this.m_quickSelect[0];
				this.m_input.caretPosition = this.m_input.text.Length;
			}
			if (ZInput.GetButtonDown("JoyDPadRight"))
			{
				this.m_input.text = this.m_quickSelect[1];
				this.m_input.caretPosition = this.m_input.text.Length;
			}
			if (ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.m_input.caretPosition = this.m_input.text.Length;
				this.m_input.text = this.m_quickSelect[2];
			}
			if (ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.m_input.caretPosition = this.m_input.text.Length;
				this.m_input.text = this.m_quickSelect[3];
			}
		}
		else if ((ZInput.GetButtonDown("ChatUp") || ZInput.GetButtonDown("JoyDPadUp")) && !this.m_input.IsCompositionActive())
		{
			if (this.m_historyPosition > 0)
			{
				this.m_historyPosition--;
			}
			this.m_input.text = ((this.m_history.Count > 0) ? this.m_history[this.m_historyPosition] : "");
			this.m_input.caretPosition = this.m_input.text.Length;
		}
		else if ((ZInput.GetButtonDown("ChatDown") || ZInput.GetButtonDown("JoyDPadDown")) && !this.m_input.IsCompositionActive())
		{
			if (this.m_historyPosition < this.m_history.Count)
			{
				this.m_historyPosition++;
			}
			this.m_input.text = ((this.m_historyPosition < this.m_history.Count) ? this.m_history[this.m_historyPosition] : "");
			this.m_input.caretPosition = this.m_input.text.Length;
		}
		else if (ZInput.GetKeyDown(KeyCode.Tab, true) || ZInput.GetButtonDown("JoyDPadRight"))
		{
			if (this.m_commandList.Count == 0)
			{
				this.updateCommandList();
			}
			string[] array = this.m_input.text.Split(' ', StringSplitOptions.None);
			if (array.Length == 1)
			{
				this.tabCycle(array[0], this.m_commandList, true);
			}
			else
			{
				string key = (this.m_tabPrefix == '\0') ? array[0] : array[0].Substring(1);
				Terminal.ConsoleCommand consoleCommand;
				if (Terminal.commands.TryGetValue(key, out consoleCommand))
				{
					this.tabCycle(array[1], consoleCommand.GetTabOptions(), false);
				}
			}
		}
		if ((ZInput.GetButtonDown("ScrollChatUp") || ZInput.GetButtonDown("JoyScrollChatUp")) && this.m_scrollHeight < this.m_chatBuffer.Count - 5)
		{
			this.m_scrollHeight++;
			this.UpdateChat();
		}
		if ((ZInput.GetButtonDown("ScrollChatDown") || ZInput.GetButtonDown("JoyScrollChatDown")) && this.m_scrollHeight > 0)
		{
			this.m_scrollHeight--;
			this.UpdateChat();
		}
		if (this.m_input.caretPosition != this.m_tabCaretPositionEnd)
		{
			this.m_tabCaretPosition = -1;
		}
		if (this.m_lastSearchLength != this.m_input.text.Length)
		{
			this.m_lastSearchLength = this.m_input.text.Length;
			if (this.m_commandList.Count == 0)
			{
				this.updateCommandList();
			}
			string[] array2 = this.m_input.text.Split(' ', StringSplitOptions.None);
			if (array2.Length == 1)
			{
				this.updateSearch(array2[0], this.m_commandList, true);
				return;
			}
			string key2 = (this.m_tabPrefix == '\0') ? array2[0] : ((array2[0].Length == 0) ? "" : array2[0].Substring(1));
			Terminal.ConsoleCommand consoleCommand2;
			if (Terminal.commands.TryGetValue(key2, out consoleCommand2))
			{
				this.updateSearch(array2[1], consoleCommand2.GetTabOptions(), false);
			}
		}
	}

	// Token: 0x06000749 RID: 1865 RVA: 0x0003DFB0 File Offset: 0x0003C1B0
	protected void SendInput()
	{
		if (string.IsNullOrEmpty(this.m_input.text))
		{
			return;
		}
		this.InputText();
		if (this.m_history.Count == 0 || this.m_history[this.m_history.Count - 1] != this.m_input.text)
		{
			this.m_history.Add(this.m_input.text);
		}
		this.m_historyPosition = this.m_history.Count;
		this.m_input.text = "";
		this.m_scrollHeight = 0;
		this.UpdateChat();
		if (!Application.isConsolePlatform && !Application.isMobilePlatform)
		{
			this.m_input.ActivateInputField();
		}
	}

	// Token: 0x0600074A RID: 1866 RVA: 0x0003E06C File Offset: 0x0003C26C
	protected virtual void InputText()
	{
		string text = this.m_input.text;
		this.AddString(text);
		this.TryRunCommand(text, false, false);
	}

	// Token: 0x0600074B RID: 1867 RVA: 0x0003E095 File Offset: 0x0003C295
	protected virtual bool isAllowedCommand(Terminal.ConsoleCommand cmd)
	{
		return true;
	}

	// Token: 0x0600074C RID: 1868 RVA: 0x0003E098 File Offset: 0x0003C298
	public void AddString(PlatformUserID user, string text, Talker.Type type, bool timestamp = false)
	{
		Color color = Color.white;
		if (type != Talker.Type.Whisper)
		{
			if (type == Talker.Type.Shout)
			{
				color = Color.yellow;
				text = text.ToUpper();
			}
			else
			{
				color = Color.white;
			}
		}
		else
		{
			color = new Color(1f, 1f, 1f, 0.75f);
			text = text.ToLowerInvariant();
		}
		ZNet.PlayerInfo playerInfo;
		if (!ZNet.TryGetPlayerByPlatformUserID(user, out playerInfo))
		{
			ZLog.LogError(string.Format("Failed to get player info for player {0} who sent the chat message \"{1}\"!", user, text));
			return;
		}
		string text2 = CensorShittyWords.FilterUGC(playerInfo.m_name, UGCType.CharacterName, user, 0L);
		if (PlatformManager.DistributionPlatform.Platform == "Xbox")
		{
			IRelationsProvider relationsProvider = PlatformManager.DistributionPlatform.RelationsProvider;
			if (relationsProvider == null)
			{
				ZLog.LogError(string.Format("Relations provider was unavailable when user {0} ({1}) sent the chat message \"{2}\"! This should never happen!", text2, user, text));
			}
			IUserProfile userProfile;
			string text3;
			if (relationsProvider != null && PlatformManager.DistributionPlatform.Platform == user.m_platform && relationsProvider.TryGetUserProfile(user, out userProfile) && userProfile.DisplayName != null)
			{
				if (userProfile.DisplayName.Length > 0)
				{
					text2 = text2 + " [" + userProfile.DisplayName + "]";
				}
			}
			else if (ZNet.TryGetServerAssignedDisplayName(user, out text3))
			{
				if (text3.Length > 0)
				{
					text2 = text2 + " [" + text3 + "]";
				}
			}
			else
			{
				ZLog.LogWarning(string.Format("Failed to get display name for player {0} ({1}) who sent the chat message \"{2}\"!", text2, user, text));
			}
		}
		string text4 = timestamp ? ("[" + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss") + "] ") : "";
		text4 = string.Concat(new string[]
		{
			text4,
			"<color=orange>",
			text2,
			"</color>: <color=#",
			ColorUtility.ToHtmlStringRGBA(color),
			">",
			text,
			"</color>"
		});
		this.AddString(text4);
	}

	// Token: 0x0600074D RID: 1869 RVA: 0x0003E270 File Offset: 0x0003C470
	public void AddString(string title, string text, Talker.Type type, bool timestamp = false)
	{
		Color color = Color.white;
		if (type != Talker.Type.Whisper)
		{
			if (type == Talker.Type.Shout)
			{
				color = Color.yellow;
				text = text.ToUpper();
			}
			else
			{
				color = Color.white;
			}
		}
		else
		{
			color = new Color(1f, 1f, 1f, 0.75f);
			text = text.ToLowerInvariant();
		}
		string text2 = timestamp ? ("[" + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss") + "] ") : "";
		text2 = string.Concat(new string[]
		{
			text2,
			"<color=orange>",
			title,
			"</color>: <color=#",
			ColorUtility.ToHtmlStringRGBA(color),
			">",
			text,
			"</color>"
		});
		this.AddString(text2);
	}

	// Token: 0x0600074E RID: 1870 RVA: 0x0003E33C File Offset: 0x0003C53C
	public void AddString(string text)
	{
		while (this.m_maxVisibleBufferLength > 1)
		{
			try
			{
				this.m_chatBuffer.Add(text);
				while (this.m_chatBuffer.Count > 300)
				{
					this.m_chatBuffer.RemoveAt(0);
				}
				this.UpdateChat();
				break;
			}
			catch (Exception)
			{
				this.m_maxVisibleBufferLength--;
			}
		}
	}

	// Token: 0x0600074F RID: 1871 RVA: 0x0003E3AC File Offset: 0x0003C5AC
	public void UpdateDisplayName(string oldName, string newName)
	{
		if (string.IsNullOrEmpty(oldName))
		{
			Debug.LogError(string.Concat(new string[]
			{
				"Failed to update display to \"",
				newName,
				"\"! oldName was ",
				(oldName == null) ? "null" : "empty",
				" "
			}));
			return;
		}
		for (int i = 0; i < this.m_chatBuffer.Count; i++)
		{
			this.m_chatBuffer[i] = this.m_chatBuffer[i].Replace(oldName, newName);
		}
		this.UpdateChat();
	}

	// Token: 0x06000750 RID: 1872 RVA: 0x0003E43C File Offset: 0x0003C63C
	private void UpdateChat()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = Mathf.Min(this.m_chatBuffer.Count, Mathf.Max(5, this.m_chatBuffer.Count - this.m_scrollHeight));
		for (int i = Mathf.Max(0, num - this.m_maxVisibleBufferLength); i < num; i++)
		{
			stringBuilder.Append(this.m_chatBuffer[i]);
			stringBuilder.Append("\n");
		}
		this.m_output.text = stringBuilder.ToString();
	}

	// Token: 0x06000751 RID: 1873 RVA: 0x0003E4C4 File Offset: 0x0003C6C4
	public static float GetTestValue(string key, float defaultIfMissing = 0f)
	{
		string s;
		float result;
		if (Terminal.m_testList.TryGetValue(key, out s) && float.TryParse(s, out result))
		{
			return result;
		}
		return defaultIfMissing;
	}

	// Token: 0x06000752 RID: 1874 RVA: 0x0003E4F0 File Offset: 0x0003C6F0
	private void tabCycle(string word, List<string> options, bool usePrefix)
	{
		if (options == null || options.Count == 0)
		{
			return;
		}
		usePrefix = (usePrefix && this.m_tabPrefix > '\0');
		if (usePrefix)
		{
			if (word.Length < 1 || word[0] != this.m_tabPrefix)
			{
				return;
			}
			word = word.Substring(1);
		}
		if (this.m_tabCaretPosition == -1)
		{
			this.m_tabOptions.Clear();
			this.m_tabCaretPosition = this.m_input.caretPosition;
			word = word.ToLower();
			this.m_tabLength = word.Length;
			if (this.m_tabLength == 0)
			{
				this.m_tabOptions.AddRange(options);
			}
			else
			{
				foreach (string text in options)
				{
					if (text != null && text.Length > this.m_tabLength && this.safeSubstring(text, 0, this.m_tabLength).ToLower() == word)
					{
						this.m_tabOptions.Add(text);
					}
				}
			}
			this.m_tabOptions.Sort();
			this.m_tabIndex = -1;
		}
		if (this.m_tabOptions.Count == 0)
		{
			this.m_tabOptions.AddRange(this.m_lastSearch);
		}
		if (this.m_tabOptions.Count == 0)
		{
			return;
		}
		int num = this.m_tabIndex + 1;
		this.m_tabIndex = num;
		if (num >= this.m_tabOptions.Count)
		{
			this.m_tabIndex = 0;
		}
		if (this.m_tabCaretPosition - this.m_tabLength >= 0)
		{
			this.m_input.text = this.safeSubstring(this.m_input.text, 0, this.m_tabCaretPosition - this.m_tabLength) + this.m_tabOptions[this.m_tabIndex];
		}
		this.m_tabCaretPositionEnd = (this.m_input.caretPosition = this.m_input.text.Length);
	}

	// Token: 0x06000753 RID: 1875 RVA: 0x0003E6DC File Offset: 0x0003C8DC
	private void updateSearch(string word, List<string> options, bool usePrefix)
	{
		if (this.m_search == null)
		{
			return;
		}
		this.m_search.text = "";
		if (options == null || options.Count == 0)
		{
			return;
		}
		usePrefix = (usePrefix && this.m_tabPrefix > '\0');
		if (usePrefix)
		{
			if (word.Length < 1 || word[0] != this.m_tabPrefix)
			{
				return;
			}
			word = word.Substring(1);
		}
		this.m_lastSearch.Clear();
		foreach (string text in options)
		{
			if (text != null)
			{
				string text2 = text.ToLower();
				if (text2.Contains(word.ToLower()) && (word.Contains("fx") || !text2.Contains("fx")))
				{
					this.m_lastSearch.Add(text);
				}
			}
		}
		int num = 10;
		for (int i = 0; i < Math.Min(this.m_lastSearch.Count, num); i++)
		{
			string text3 = this.m_lastSearch[i];
			int num2 = text3.ToLower().IndexOf(word.ToLower());
			TMP_Text search = this.m_search;
			search.text += this.safeSubstring(text3, 0, num2);
			TMP_Text search2 = this.m_search;
			search2.text = search2.text + "<color=white>" + this.safeSubstring(text3, num2, word.Length) + "</color>";
			TMP_Text search3 = this.m_search;
			search3.text = search3.text + this.safeSubstring(text3, num2 + word.Length, -1) + " ";
		}
		if (this.m_lastSearch.Count > num)
		{
			TMP_Text search4 = this.m_search;
			search4.text += string.Format("... {0} more.", this.m_lastSearch.Count - num);
		}
	}

	// Token: 0x06000754 RID: 1876 RVA: 0x0003E8D8 File Offset: 0x0003CAD8
	private string safeSubstring(string text, int start, int length = -1)
	{
		if (text.Length == 0)
		{
			return text;
		}
		if (start < 0)
		{
			start = 0;
		}
		if (start + length >= text.Length)
		{
			length = text.Length - start;
		}
		if (length >= 0)
		{
			return text.Substring(start, length);
		}
		return text.Substring(start);
	}

	// Token: 0x06000755 RID: 1877 RVA: 0x0003E914 File Offset: 0x0003CB14
	protected void LoadQuickSelect()
	{
		this.m_quickSelect[0] = PlatformPrefs.GetString("quick_save_left", "");
		this.m_quickSelect[1] = PlatformPrefs.GetString("quick_save_right", "");
		this.m_quickSelect[2] = PlatformPrefs.GetString("quick_save_up", "");
		this.m_quickSelect[3] = PlatformPrefs.GetString("quick_save_down", "");
	}

	// Token: 0x06000756 RID: 1878 RVA: 0x0003E980 File Offset: 0x0003CB80
	public static float TryTestFloat(string key, float defaultValue = 1f)
	{
		string s;
		float result;
		if (Terminal.m_testList.TryGetValue(key, out s) && float.TryParse(s, out result))
		{
			return result;
		}
		return defaultValue;
	}

	// Token: 0x06000757 RID: 1879 RVA: 0x0003E9AC File Offset: 0x0003CBAC
	public static int TryTestInt(string key, int defaultValue = 1)
	{
		string s;
		int result;
		if (Terminal.m_testList.TryGetValue(key, out s) && int.TryParse(s, out result))
		{
			return result;
		}
		return defaultValue;
	}

	// Token: 0x06000758 RID: 1880 RVA: 0x0003E9D8 File Offset: 0x0003CBD8
	public static string TryTest(string key, string defaultValue = "")
	{
		string result;
		if (Terminal.m_testList.TryGetValue(key, out result))
		{
			return result;
		}
		return defaultValue;
	}

	// Token: 0x06000759 RID: 1881 RVA: 0x0003E9F8 File Offset: 0x0003CBF8
	public static int Increment(string key, int by = 1)
	{
		string s;
		if (Terminal.m_testList.TryGetValue(key, out s))
		{
			Terminal.m_testList[key] = (int.Parse(s) + by).ToString();
		}
		else
		{
			Terminal.m_testList[key] = by.ToString();
		}
		return int.Parse(Terminal.m_testList[key]);
	}

	// Token: 0x0600075A RID: 1882 RVA: 0x0003EA53 File Offset: 0x0003CC53
	public static void Log(object obj)
	{
		if (Terminal.m_showTests)
		{
			ZLog.Log(obj);
			if (global::Console.instance)
			{
				global::Console.instance.AddString("Log", obj.ToString(), Talker.Type.Whisper, true);
			}
		}
	}

	// Token: 0x0600075B RID: 1883 RVA: 0x0003EA85 File Offset: 0x0003CC85
	public static void LogWarning(object obj)
	{
		if (Terminal.m_showTests)
		{
			ZLog.LogWarning(obj);
			if (global::Console.instance)
			{
				global::Console.instance.AddString("Warning", obj.ToString(), Talker.Type.Whisper, true);
			}
		}
	}

	// Token: 0x0600075C RID: 1884 RVA: 0x0003EAB7 File Offset: 0x0003CCB7
	public static void LogError(object obj)
	{
		if (Terminal.m_showTests)
		{
			ZLog.LogError(obj);
			if (global::Console.instance)
			{
				global::Console.instance.AddString("Warning", obj.ToString(), Talker.Type.Whisper, true);
			}
		}
	}

	// Token: 0x17000027 RID: 39
	// (get) Token: 0x0600075D RID: 1885
	protected abstract Terminal m_terminalInstance { get; }

	// Token: 0x06000760 RID: 1888 RVA: 0x0003EB98 File Offset: 0x0003CD98
	[CompilerGenerated]
	internal static void <InitTerminal>g__GetInfo|7_135(IEnumerable collection, ref Terminal.<>c__DisplayClass7_0 A_1)
	{
		foreach (object obj in collection)
		{
			Character character = obj as Character;
			if (character != null)
			{
				Terminal.<InitTerminal>g__count|7_136(character.m_name, character.GetLevel(), 1, ref A_1);
			}
			else if (obj is RandomFlyingBird)
			{
				Terminal.<InitTerminal>g__count|7_136("Bird", 1, 1, ref A_1);
			}
			else
			{
				Fish fish = obj as Fish;
				if (fish != null)
				{
					ItemDrop component = fish.GetComponent<ItemDrop>();
					if (component != null)
					{
						Terminal.<InitTerminal>g__count|7_136(component.m_itemData.m_shared.m_name, component.m_itemData.m_quality, component.m_itemData.m_stack, ref A_1);
					}
				}
			}
		}
		foreach (object obj2 in collection)
		{
			MonoBehaviour monoBehaviour = obj2 as MonoBehaviour;
			if (monoBehaviour != null)
			{
				A_1.args.Context.AddString(string.Format("   {0}, Dist: {1}, Offset: {2}", monoBehaviour.name, Vector3.Distance(Player.m_localPlayer.transform.position, monoBehaviour.transform.position).ToString("0.0"), monoBehaviour.transform.position - Player.m_localPlayer.transform.position));
			}
		}
	}

	// Token: 0x06000761 RID: 1889 RVA: 0x0003ED24 File Offset: 0x0003CF24
	[CompilerGenerated]
	internal static void <InitTerminal>g__count|7_136(string key, int level, int increment = 1, ref Terminal.<>c__DisplayClass7_0 A_3)
	{
		Dictionary<int, int> dictionary;
		if (!A_3.counts.TryGetValue(key, out dictionary))
		{
			dictionary = (A_3.counts[key] = new Dictionary<int, int>());
		}
		int num;
		if (dictionary.TryGetValue(level, out num))
		{
			dictionary[level] = num + increment;
			return;
		}
		dictionary[level] = increment;
	}

	// Token: 0x06000762 RID: 1890 RVA: 0x0003ED74 File Offset: 0x0003CF74
	[CompilerGenerated]
	internal static void <InitTerminal>g__add|7_137(string key, ref Terminal.<>c__DisplayClass7_1 A_1)
	{
		int total = A_1.total;
		A_1.total = total + 1;
		int num;
		if (A_1.counts.TryGetValue(key, out num))
		{
			A_1.counts[key] = num + 1;
			return;
		}
		A_1.counts[key] = 1;
	}

	// Token: 0x06000763 RID: 1891 RVA: 0x0003EDC0 File Offset: 0x0003CFC0
	[CompilerGenerated]
	internal static List<string> <InitTerminal>g__findOpt|7_64()
	{
		if (!ZNetScene.instance)
		{
			return null;
		}
		List<string> list = new List<string>(ZNetScene.instance.GetPrefabNames());
		foreach (ZoneSystem.ZoneLocation zoneLocation in ZoneSystem.instance.m_locations)
		{
			if (zoneLocation.m_enable || zoneLocation.m_prefab.IsValid)
			{
				list.Add(zoneLocation.m_prefab.Name);
			}
		}
		return list;
	}

	// Token: 0x06000764 RID: 1892 RVA: 0x0003EE58 File Offset: 0x0003D058
	[CompilerGenerated]
	internal static List<Tuple<object, Vector3>> <InitTerminal>g__find|7_65(string q)
	{
		new Dictionary<string, Dictionary<int, int>>();
		GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
		List<Tuple<object, Vector3>> list = new List<Tuple<object, Vector3>>();
		foreach (GameObject gameObject in array)
		{
			if (gameObject.name.ToLower().Contains(q))
			{
				list.Add(new Tuple<object, Vector3>(gameObject, gameObject.transform.position));
			}
		}
		foreach (ZoneSystem.LocationInstance locationInstance in ZoneSystem.instance.GetLocationList())
		{
			if (locationInstance.m_location.m_prefab.Name.ToLower().Contains(q))
			{
				list.Add(new Tuple<object, Vector3>(locationInstance, locationInstance.m_position));
			}
		}
		List<ZDO> list2 = new List<ZDO>();
		int num = 0;
		while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(q, list2, ref num))
		{
		}
		foreach (ZDO zdo in list2)
		{
			list.Add(new Tuple<object, Vector3>(zdo, zdo.GetPosition()));
		}
		return list;
	}

	// Token: 0x06000765 RID: 1893 RVA: 0x0003EFA0 File Offset: 0x0003D1A0
	[CompilerGenerated]
	internal static void <InitTerminal>g__spawn|7_140(string name, ref Terminal.<>c__DisplayClass7_2 A_1)
	{
		GameObject prefab = ZNetScene.instance.GetPrefab(name);
		if (!prefab)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Missing object " + name, 0, null);
			return;
		}
		for (int i = 0; i < A_1.count; i++)
		{
			Vector3 b = UnityEngine.Random.insideUnitSphere * ((A_1.count == 1) ? 0f : A_1.radius);
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Spawning object " + name, 0, null);
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up + b, Quaternion.identity);
			if (A_1.vals != null)
			{
				ZNetView component = gameObject.GetComponent<ZNetView>();
				if (component != null && component.IsValid())
				{
					component.GetZDO().Set("HasFields", true);
					foreach (KeyValuePair<string, object> keyValuePair in A_1.vals)
					{
						string[] array = keyValuePair.Key.Split('.', StringSplitOptions.None);
						if (array.Length >= 2)
						{
							("HasFields" + array[0]).GetStableHashCode();
							component.GetZDO().Set("HasFields" + array[0], true);
							keyValuePair.Value.GetType();
							if (keyValuePair.Value is float)
							{
								component.GetZDO().Set(keyValuePair.Key, (float)keyValuePair.Value);
							}
							else if (keyValuePair.Value is int)
							{
								component.GetZDO().Set(keyValuePair.Key, (int)keyValuePair.Value);
							}
							else if (keyValuePair.Value is bool)
							{
								component.GetZDO().Set(keyValuePair.Key, (bool)keyValuePair.Value);
							}
							else
							{
								component.GetZDO().Set(keyValuePair.Key, keyValuePair.Value.ToString());
							}
						}
					}
					component.LoadFields();
				}
			}
			ItemDrop component2 = gameObject.GetComponent<ItemDrop>();
			ItemDrop.OnCreateNew(gameObject);
			if (A_1.level > 1)
			{
				if (component2)
				{
					A_1.level = Mathf.Min(A_1.level, 4);
				}
				else
				{
					A_1.level = Mathf.Min(A_1.level, 9);
				}
				Character component3 = gameObject.GetComponent<Character>();
				if (component3 != null)
				{
					component3.SetLevel(A_1.level);
				}
				if (A_1.level > 4)
				{
					A_1.level = 4;
				}
				if (component2)
				{
					component2.SetQuality(A_1.level);
				}
			}
			if (A_1.pickup | A_1.use | A_1.onlyIfMissing)
			{
				if (A_1.onlyIfMissing && component2 && Player.m_localPlayer.GetInventory().HaveItem(component2.m_itemData.m_shared.m_name, true))
				{
					ZNetView component4 = gameObject.GetComponent<ZNetView>();
					if (component4 != null)
					{
						component4.Destroy();
						goto IL_352;
					}
				}
				if ((Player.m_localPlayer.Pickup(gameObject, false, false) & A_1.use) && component2)
				{
					Player.m_localPlayer.UseItem(Player.m_localPlayer.GetInventory(), component2.m_itemData, false);
				}
			}
			IL_352:;
		}
	}

	// Token: 0x04000885 RID: 2181
	private static bool m_terminalInitialized;

	// Token: 0x04000886 RID: 2182
	protected static List<string> m_bindList;

	// Token: 0x04000887 RID: 2183
	public static Dictionary<string, string> m_testList = new Dictionary<string, string>();

	// Token: 0x04000888 RID: 2184
	protected static Dictionary<KeyCode, List<string>> m_binds = new Dictionary<KeyCode, List<string>>();

	// Token: 0x04000889 RID: 2185
	private static bool m_cheat = false;

	// Token: 0x0400088A RID: 2186
	public static bool m_showTests;

	// Token: 0x0400088B RID: 2187
	protected float m_lastDebugUpdate;

	// Token: 0x0400088C RID: 2188
	protected static Dictionary<string, Terminal.ConsoleCommand> commands = new Dictionary<string, Terminal.ConsoleCommand>();

	// Token: 0x0400088D RID: 2189
	public static ConcurrentQueue<string> m_threadSafeMessages = new ConcurrentQueue<string>();

	// Token: 0x0400088E RID: 2190
	public static ConcurrentQueue<string> m_threadSafeConsoleLog = new ConcurrentQueue<string>();

	// Token: 0x0400088F RID: 2191
	protected char m_tabPrefix;

	// Token: 0x04000890 RID: 2192
	protected bool m_autoCompleteSecrets;

	// Token: 0x04000891 RID: 2193
	private List<string> m_history = new List<string>();

	// Token: 0x04000892 RID: 2194
	protected string[] m_quickSelect = new string[4];

	// Token: 0x04000893 RID: 2195
	private List<string> m_tabOptions = new List<string>();

	// Token: 0x04000894 RID: 2196
	private int m_historyPosition;

	// Token: 0x04000895 RID: 2197
	private int m_tabCaretPosition = -1;

	// Token: 0x04000896 RID: 2198
	private int m_tabCaretPositionEnd;

	// Token: 0x04000897 RID: 2199
	private int m_tabLength;

	// Token: 0x04000898 RID: 2200
	private int m_tabIndex;

	// Token: 0x04000899 RID: 2201
	private List<string> m_commandList = new List<string>();

	// Token: 0x0400089A RID: 2202
	private List<Minimap.PinData> m_findPins = new List<Minimap.PinData>();

	// Token: 0x0400089B RID: 2203
	protected bool m_focused;

	// Token: 0x0400089C RID: 2204
	public RectTransform m_chatWindow;

	// Token: 0x0400089D RID: 2205
	public TextMeshProUGUI m_output;

	// Token: 0x0400089E RID: 2206
	public GuiInputField m_input;

	// Token: 0x0400089F RID: 2207
	public TMP_Text m_search;

	// Token: 0x040008A0 RID: 2208
	private int m_lastSearchLength;

	// Token: 0x040008A1 RID: 2209
	private List<string> m_lastSearch = new List<string>();

	// Token: 0x040008A2 RID: 2210
	protected List<string> m_chatBuffer = new List<string>();

	// Token: 0x040008A3 RID: 2211
	protected const int m_maxBufferLength = 300;

	// Token: 0x040008A4 RID: 2212
	public int m_maxVisibleBufferLength = 30;

	// Token: 0x040008A5 RID: 2213
	private const int m_maxScrollHeight = 5;

	// Token: 0x040008A6 RID: 2214
	private int m_scrollHeight;

	// Token: 0x0200026E RID: 622
	public class ConsoleEventArgs
	{
		// Token: 0x17000183 RID: 387
		// (get) Token: 0x06001F57 RID: 8023 RVA: 0x000E3090 File Offset: 0x000E1290
		public int Length
		{
			get
			{
				return this.Args.Length;
			}
		}

		// Token: 0x17000184 RID: 388
		public string this[int i]
		{
			get
			{
				return this.Args[i];
			}
		}

		// Token: 0x06001F59 RID: 8025 RVA: 0x000E30A4 File Offset: 0x000E12A4
		public ConsoleEventArgs(string line, Terminal context)
		{
			this.Context = context;
			this.FullLine = line;
			int num = line.IndexOf(' ');
			this.ArgsAll = ((num > 0) ? line.Substring(num + 1) : "");
			this.Args = line.Split(' ', StringSplitOptions.None);
		}

		// Token: 0x06001F5A RID: 8026 RVA: 0x000E30F8 File Offset: 0x000E12F8
		public int TryParameterInt(int parameterIndex, int defaultValue = 1)
		{
			int result;
			if (this.TryParameterInt(parameterIndex, out result))
			{
				return result;
			}
			return defaultValue;
		}

		// Token: 0x06001F5B RID: 8027 RVA: 0x000E3113 File Offset: 0x000E1313
		public bool TryParameterInt(int parameterIndex, out int value)
		{
			if (this.Args.Length <= parameterIndex || !int.TryParse(this.Args[parameterIndex], out value))
			{
				value = 0;
				return false;
			}
			return true;
		}

		// Token: 0x06001F5C RID: 8028 RVA: 0x000E3136 File Offset: 0x000E1336
		public bool TryParameterLong(int parameterIndex, out long value)
		{
			if (this.Args.Length <= parameterIndex || !long.TryParse(this.Args[parameterIndex], out value))
			{
				value = 0L;
				return false;
			}
			return true;
		}

		// Token: 0x06001F5D RID: 8029 RVA: 0x000E315C File Offset: 0x000E135C
		public float TryParameterFloat(int parameterIndex, float defaultValue = 1f)
		{
			float result;
			if (this.TryParameterFloat(parameterIndex, out result))
			{
				return result;
			}
			return defaultValue;
		}

		// Token: 0x06001F5E RID: 8030 RVA: 0x000E3177 File Offset: 0x000E1377
		public bool TryParameterFloat(int parameterIndex, out float value)
		{
			if (this.Args.Length <= parameterIndex || !float.TryParse(this.Args[parameterIndex].Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
			{
				value = 0f;
				return false;
			}
			return true;
		}

		// Token: 0x06001F5F RID: 8031 RVA: 0x000E31B8 File Offset: 0x000E13B8
		public bool HasArgumentAnywhere(string value, int firstIndexToCheck = 0, bool toLower = true)
		{
			for (int i = firstIndexToCheck; i < this.Args.Length; i++)
			{
				if ((toLower && this.Args[i].ToLower() == value) || (!toLower && this.Args[i] == value))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x040020DE RID: 8414
		public string[] Args;

		// Token: 0x040020DF RID: 8415
		public string ArgsAll;

		// Token: 0x040020E0 RID: 8416
		public string FullLine;

		// Token: 0x040020E1 RID: 8417
		public Terminal Context;
	}

	// Token: 0x0200026F RID: 623
	public class ConsoleCommand
	{
		// Token: 0x06001F60 RID: 8032 RVA: 0x000E3208 File Offset: 0x000E1408
		public ConsoleCommand(string command, string description, Terminal.ConsoleEventFailable action, bool isCheat = false, bool isNetwork = false, bool onlyServer = false, bool isSecret = false, bool allowInDevBuild = false, Terminal.ConsoleOptionsFetcher optionsFetcher = null, bool alwaysRefreshTabOptions = false, bool remoteCommand = false, bool onlyAdmin = false)
		{
			Terminal.commands[command.ToLower()] = this;
			this.Command = command;
			this.Description = description;
			this.actionFailable = action;
			this.IsCheat = isCheat;
			this.OnlyServer = (onlyServer || onlyAdmin);
			this.IsSecret = isSecret;
			this.IsNetwork = isNetwork;
			this.AllowInDevBuild = allowInDevBuild;
			this.m_tabOptionsFetcher = optionsFetcher;
			this.m_alwaysRefreshTabOptions = alwaysRefreshTabOptions;
			this.RemoteCommand = remoteCommand;
			this.OnlyAdmin = onlyAdmin;
		}

		// Token: 0x06001F61 RID: 8033 RVA: 0x000E328C File Offset: 0x000E148C
		public ConsoleCommand(string command, string description, Terminal.ConsoleEvent action, bool isCheat = false, bool isNetwork = false, bool onlyServer = false, bool isSecret = false, bool allowInDevBuild = false, Terminal.ConsoleOptionsFetcher optionsFetcher = null, bool alwaysRefreshTabOptions = false, bool remoteCommand = false, bool onlyAdmin = false)
		{
			Terminal.commands[command.ToLower()] = this;
			this.Command = command;
			this.Description = description;
			this.action = action;
			this.IsCheat = isCheat;
			this.OnlyServer = onlyServer;
			this.IsSecret = isSecret;
			this.IsNetwork = isNetwork;
			this.AllowInDevBuild = allowInDevBuild;
			this.m_tabOptionsFetcher = optionsFetcher;
			this.m_alwaysRefreshTabOptions = alwaysRefreshTabOptions;
			this.RemoteCommand = remoteCommand;
			this.OnlyAdmin = onlyAdmin;
		}

		// Token: 0x06001F62 RID: 8034 RVA: 0x000E330D File Offset: 0x000E150D
		public List<string> GetTabOptions()
		{
			if (this.m_tabOptionsFetcher != null && (this.m_tabOptions == null || this.m_alwaysRefreshTabOptions))
			{
				this.m_tabOptions = this.m_tabOptionsFetcher();
			}
			return this.m_tabOptions;
		}

		// Token: 0x06001F63 RID: 8035 RVA: 0x000E3340 File Offset: 0x000E1540
		public void RunAction(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				List<string> tabOptions = this.GetTabOptions();
				if (tabOptions != null)
				{
					foreach (string text in tabOptions)
					{
						if (text != null && args[1].ToLower() == text.ToLower())
						{
							args.Args[1] = text;
							break;
						}
					}
				}
			}
			if (this.action != null)
			{
				this.action(args);
			}
			else
			{
				object obj = this.actionFailable(args);
				if (obj is bool && !(bool)obj)
				{
					args.Context.AddString(string.Concat(new string[]
					{
						"<color=#8b0000>Error executing command. Check parameters and context.</color>\n   <color=#888888>",
						this.Command,
						" - ",
						this.Description,
						"</color>"
					}));
				}
				string text2 = obj as string;
				if (text2 != null)
				{
					args.Context.AddString(string.Concat(new string[]
					{
						"<color=#8b0000>Error executing command: ",
						text2,
						"</color>\n   <color=#888888>",
						this.Command,
						" - ",
						this.Description,
						"</color>"
					}));
				}
			}
			if (Game.instance)
			{
				PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
				if (this.IsCheat)
				{
					playerProfile.m_usedCheats = true;
					playerProfile.IncrementStat(PlayerStatType.Cheats, 1f);
				}
				playerProfile.m_knownCommands.IncrementOrSet(args[0].ToLower(), 1f);
			}
		}

		// Token: 0x06001F64 RID: 8036 RVA: 0x000E34E8 File Offset: 0x000E16E8
		public bool ShowCommand(Terminal context)
		{
			return !this.IsSecret && (this.IsValid(context, false) || (ZNet.instance && !ZNet.instance.IsServer() && this.RemoteCommand));
		}

		// Token: 0x06001F65 RID: 8037 RVA: 0x000E3520 File Offset: 0x000E1720
		public bool IsValid(Terminal context, bool skipAllowedCheck = false)
		{
			return (!this.IsCheat || context.IsCheatsEnabled()) && (context.isAllowedCommand(this) || skipAllowedCheck) && (!this.IsNetwork || ZNet.instance) && (!this.OnlyServer || (ZNet.instance && ZNet.instance.IsServer()));
		}

		// Token: 0x040020E2 RID: 8418
		public string Command;

		// Token: 0x040020E3 RID: 8419
		public string Description;

		// Token: 0x040020E4 RID: 8420
		public bool IsCheat;

		// Token: 0x040020E5 RID: 8421
		public bool IsNetwork;

		// Token: 0x040020E6 RID: 8422
		public bool OnlyServer;

		// Token: 0x040020E7 RID: 8423
		public bool IsSecret;

		// Token: 0x040020E8 RID: 8424
		public bool AllowInDevBuild;

		// Token: 0x040020E9 RID: 8425
		public bool RemoteCommand;

		// Token: 0x040020EA RID: 8426
		public bool OnlyAdmin;

		// Token: 0x040020EB RID: 8427
		private Terminal.ConsoleEventFailable actionFailable;

		// Token: 0x040020EC RID: 8428
		private Terminal.ConsoleEvent action;

		// Token: 0x040020ED RID: 8429
		private Terminal.ConsoleOptionsFetcher m_tabOptionsFetcher;

		// Token: 0x040020EE RID: 8430
		private List<string> m_tabOptions;

		// Token: 0x040020EF RID: 8431
		private bool m_alwaysRefreshTabOptions;
	}

	// Token: 0x02000270 RID: 624
	// (Invoke) Token: 0x06001F67 RID: 8039
	public delegate object ConsoleEventFailable(Terminal.ConsoleEventArgs args);

	// Token: 0x02000271 RID: 625
	// (Invoke) Token: 0x06001F6B RID: 8043
	public delegate void ConsoleEvent(Terminal.ConsoleEventArgs args);

	// Token: 0x02000272 RID: 626
	// (Invoke) Token: 0x06001F6F RID: 8047
	public delegate List<string> ConsoleOptionsFetcher();
}
