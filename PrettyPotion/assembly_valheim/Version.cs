using System;
using UnityEngine;

// Token: 0x0200014E RID: 334
internal class Version
{
	// Token: 0x170000BB RID: 187
	// (get) Token: 0x06001449 RID: 5193 RVA: 0x000950F7 File Offset: 0x000932F7
	public static GameVersion CurrentVersion { get; } = new GameVersion(0, 220, 5);

	// Token: 0x0600144A RID: 5194 RVA: 0x00095100 File Offset: 0x00093300
	public static string GetVersionString(bool includeMercurialHash = false)
	{
		string text = global::Version.CurrentVersion.ToString();
		string platformPrefix = global::Version.GetPlatformPrefix("");
		if (platformPrefix.Length > 0)
		{
			text = string.Format("{0}-{1}", platformPrefix, global::Version.CurrentVersion);
		}
		if (includeMercurialHash)
		{
			TextAsset textAsset = Resources.Load<TextAsset>("clientVersion");
			if (textAsset != null)
			{
				text = text + "\n" + textAsset.text;
			}
		}
		return text;
	}

	// Token: 0x0600144B RID: 5195 RVA: 0x00095175 File Offset: 0x00093375
	public static bool IsWorldVersionCompatible(int version)
	{
		return version <= 35 && version >= 9;
	}

	// Token: 0x0600144C RID: 5196 RVA: 0x00095186 File Offset: 0x00093386
	public static bool IsPlayerVersionCompatible(int version)
	{
		return version <= 41 && version >= 27;
	}

	// Token: 0x0600144D RID: 5197 RVA: 0x00095197 File Offset: 0x00093397
	public static Platforms GetPlatform()
	{
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			return Platforms.SteamDeckProton;
		}
		return Platforms.SteamWindows;
	}

	// Token: 0x0600144E RID: 5198 RVA: 0x000951A4 File Offset: 0x000933A4
	public static string GetPlatformPrefix(string Default = "")
	{
		Platforms platform = global::Version.GetPlatform();
		switch (platform)
		{
		case Platforms.SteamWindows:
		case Platforms.SteamWindows | Platforms.SteamLinux:
			break;
		case Platforms.SteamLinux:
			return "l";
		case Platforms.SteamDeckProton:
			return "dw";
		default:
			if (platform == Platforms.SteamDeckNative)
			{
				return "dl";
			}
			if (platform == Platforms.MicrosoftStore)
			{
				return "ms";
			}
			break;
		}
		return Default;
	}

	// Token: 0x0600144F RID: 5199 RVA: 0x000951F3 File Offset: 0x000933F3
	public static string GetHardwarePrefix()
	{
		return "";
	}

	// Token: 0x04001402 RID: 5122
	public const uint m_networkVersion = 34U;

	// Token: 0x04001403 RID: 5123
	public const int m_playerVersion = 41;

	// Token: 0x04001404 RID: 5124
	public const int m_oldestForwardCompatiblePlayerVersion = 27;

	// Token: 0x04001405 RID: 5125
	public const int m_worldVersion = 35;

	// Token: 0x04001406 RID: 5126
	public const int m_oldestForwardCompatibleWorldVersion = 9;

	// Token: 0x04001407 RID: 5127
	public const int c_WorldVersionNewSaveFormat = 31;

	// Token: 0x04001408 RID: 5128
	public const int c_WorldVersionGlobalKeys = 32;

	// Token: 0x04001409 RID: 5129
	public const int c_WorldVersionNumItems = 33;

	// Token: 0x0400140A RID: 5130
	public const int c_PlayerVersionMovedFirstSpawn = 40;

	// Token: 0x0400140B RID: 5131
	public const int c_PlayerDataVersionMovedFirstSpawn = 28;

	// Token: 0x0400140C RID: 5132
	public const int m_worldGenVersion = 2;

	// Token: 0x0400140D RID: 5133
	public const int m_itemDataVersion = 106;

	// Token: 0x0400140E RID: 5134
	public const int m_playerDataVersion = 29;

	// Token: 0x0400140F RID: 5135
	public static readonly GameVersion FirstVersionWithNetworkVersion = new GameVersion(0, 214, 301);

	// Token: 0x04001410 RID: 5136
	public static readonly GameVersion FirstVersionWithPlatformRestriction = new GameVersion(0, 213, 3);

	// Token: 0x04001411 RID: 5137
	public static readonly GameVersion FirstVersionWithModifiers = new GameVersion(0, 217, 8);
}
