using System;
using System.Collections.Generic;
using NetworkingUtils;
using Splatform;

// Token: 0x020000C7 RID: 199
public class ServerStatus
{
	// Token: 0x17000058 RID: 88
	// (get) Token: 0x06000BE4 RID: 3044 RVA: 0x000624E3 File Offset: 0x000606E3
	// (set) Token: 0x06000BE5 RID: 3045 RVA: 0x000624EB File Offset: 0x000606EB
	public ServerJoinData m_joinData { get; private set; }

	// Token: 0x17000059 RID: 89
	// (get) Token: 0x06000BE6 RID: 3046 RVA: 0x000624F4 File Offset: 0x000606F4
	// (set) Token: 0x06000BE7 RID: 3047 RVA: 0x000624FC File Offset: 0x000606FC
	public PlatformUserID m_hostId { get; private set; }

	// Token: 0x1700005A RID: 90
	// (get) Token: 0x06000BE8 RID: 3048 RVA: 0x00062505 File Offset: 0x00060705
	// (set) Token: 0x06000BE9 RID: 3049 RVA: 0x0006250D File Offset: 0x0006070D
	public ServerPingStatus PingStatus { get; private set; }

	// Token: 0x1700005B RID: 91
	// (get) Token: 0x06000BEA RID: 3050 RVA: 0x00062516 File Offset: 0x00060716
	// (set) Token: 0x06000BEB RID: 3051 RVA: 0x0006251E File Offset: 0x0006071E
	public OnlineStatus OnlineStatus { get; private set; }

	// Token: 0x1700005C RID: 92
	// (get) Token: 0x06000BEC RID: 3052 RVA: 0x00062528 File Offset: 0x00060728
	public bool IsCrossplay
	{
		get
		{
			return this.PlatformRestriction != null && this.PlatformRestriction.Value == Platform.Unknown;
		}
	}

	// Token: 0x1700005D RID: 93
	// (get) Token: 0x06000BED RID: 3053 RVA: 0x00062560 File Offset: 0x00060760
	public bool IsRestrictedToOwnPlatform
	{
		get
		{
			return this.PlatformRestriction != null && this.PlatformRestriction.Value == PlatformManager.DistributionPlatform.Platform;
		}
	}

	// Token: 0x1700005E RID: 94
	// (get) Token: 0x06000BEE RID: 3054 RVA: 0x0006259C File Offset: 0x0006079C
	public bool IsJoinable
	{
		get
		{
			return this.IsRestrictedToOwnPlatform || (this.IsCrossplay && PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) == PrivilegeResult.Granted);
		}
	}

	// Token: 0x1700005F RID: 95
	// (get) Token: 0x06000BEF RID: 3055 RVA: 0x000625C5 File Offset: 0x000607C5
	// (set) Token: 0x06000BF0 RID: 3056 RVA: 0x000625CD File Offset: 0x000607CD
	public uint m_playerCount { get; private set; }

	// Token: 0x17000060 RID: 96
	// (get) Token: 0x06000BF1 RID: 3057 RVA: 0x000625D6 File Offset: 0x000607D6
	// (set) Token: 0x06000BF2 RID: 3058 RVA: 0x000625DE File Offset: 0x000607DE
	public List<string> m_modifiers { get; private set; } = new List<string>();

	// Token: 0x17000061 RID: 97
	// (get) Token: 0x06000BF3 RID: 3059 RVA: 0x000625E7 File Offset: 0x000607E7
	// (set) Token: 0x06000BF4 RID: 3060 RVA: 0x000625EF File Offset: 0x000607EF
	public GameVersion m_gameVersion { get; private set; }

	// Token: 0x17000062 RID: 98
	// (get) Token: 0x06000BF5 RID: 3061 RVA: 0x000625F8 File Offset: 0x000607F8
	// (set) Token: 0x06000BF6 RID: 3062 RVA: 0x00062600 File Offset: 0x00060800
	public uint m_networkVersion { get; private set; }

	// Token: 0x17000063 RID: 99
	// (get) Token: 0x06000BF7 RID: 3063 RVA: 0x00062609 File Offset: 0x00060809
	// (set) Token: 0x06000BF8 RID: 3064 RVA: 0x00062611 File Offset: 0x00060811
	public bool m_isPasswordProtected { get; private set; }

	// Token: 0x17000064 RID: 100
	// (get) Token: 0x06000BF9 RID: 3065 RVA: 0x0006261A File Offset: 0x0006081A
	// (set) Token: 0x06000BFA RID: 3066 RVA: 0x00062640 File Offset: 0x00060840
	public Platform? PlatformRestriction
	{
		get
		{
			if (this.m_joinData is ServerJoinDataSteamUser)
			{
				return new Platform?(PlatformManager.DistributionPlatform.Platform);
			}
			return this.m_platformRestriction;
		}
		private set
		{
			if (this.m_joinData is ServerJoinDataSteamUser)
			{
				Platform? platform = value;
				Platform platform2 = PlatformManager.DistributionPlatform.Platform;
				if (platform == null || (platform != null && platform.GetValueOrDefault() != platform2))
				{
					ZLog.LogError("Can't set platform restriction of Steam server to anything other than Steam - it's always restricted to Steam!");
					return;
				}
			}
			this.m_platformRestriction = value;
		}
	}

	// Token: 0x06000BFB RID: 3067 RVA: 0x000626A0 File Offset: 0x000608A0
	public ServerStatus(ServerJoinData joinData)
	{
		this.m_joinData = joinData;
		this.OnlineStatus = OnlineStatus.Unknown;
	}

	// Token: 0x06000BFC RID: 3068 RVA: 0x000626C4 File Offset: 0x000608C4
	public void UpdateStatus(OnlineStatus onlineStatus, string serverName, uint playerCount, GameVersion gameVersion, List<string> modifiers, uint networkVersion, bool isPasswordProtected, Platform? platformRestriction, PlatformUserID host, bool affectPingStatus = true)
	{
		this.PlatformRestriction = platformRestriction;
		this.OnlineStatus = onlineStatus;
		this.m_joinData.m_serverName = serverName;
		this.m_playerCount = playerCount;
		this.m_gameVersion = gameVersion;
		this.m_modifiers = modifiers;
		this.m_networkVersion = networkVersion;
		this.m_isPasswordProtected = isPasswordProtected;
		this.m_hostId = host;
		if (affectPingStatus)
		{
			switch (onlineStatus)
			{
			case OnlineStatus.Online:
				this.PingStatus = ServerPingStatus.Success;
				return;
			case OnlineStatus.Offline:
				this.PingStatus = ServerPingStatus.CouldNotReach;
				return;
			}
			this.PingStatus = ServerPingStatus.NotStarted;
		}
	}

	// Token: 0x17000065 RID: 101
	// (get) Token: 0x06000BFD RID: 3069 RVA: 0x0006274A File Offset: 0x0006094A
	private bool DoSteamPing
	{
		get
		{
			return this.m_joinData is ServerJoinDataSteamUser || this.m_joinData is ServerJoinDataDedicated;
		}
	}

	// Token: 0x17000066 RID: 102
	// (get) Token: 0x06000BFE RID: 3070 RVA: 0x00062769 File Offset: 0x00060969
	private bool DoPlayFabPing
	{
		get
		{
			return this.m_joinData is ServerJoinDataPlayFabUser || this.m_joinData is ServerJoinDataDedicated;
		}
	}

	// Token: 0x06000BFF RID: 3071 RVA: 0x00062788 File Offset: 0x00060988
	private void PlayFabPingSuccess(PlayFabMatchmakingServerData serverData)
	{
		if (this.PingStatus != ServerPingStatus.AwaitingResponse)
		{
			return;
		}
		if (this.OnlineStatus != OnlineStatus.Online)
		{
			if (serverData != null)
			{
				this.UpdateStatus(OnlineStatus.Online, serverData.serverName, serverData.numPlayers, serverData.gameVersion, serverData.modifiers, serverData.networkVersion, serverData.havePassword, new Platform?(serverData.platformRestriction), serverData.platformUserID, false);
			}
			this.m_isAwaitingPlayFabPingResponse = false;
		}
	}

	// Token: 0x06000C00 RID: 3072 RVA: 0x000627EE File Offset: 0x000609EE
	private void PlayFabPingFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		if (this.PingStatus != ServerPingStatus.AwaitingResponse)
		{
			return;
		}
		this.m_isAwaitingPlayFabPingResponse = false;
	}

	// Token: 0x06000C01 RID: 3073 RVA: 0x00062804 File Offset: 0x00060A04
	public void Ping()
	{
		this.PingStatus = ServerPingStatus.AwaitingResponse;
		if (this.DoPlayFabPing)
		{
			if (!PlayFabManager.IsLoggedIn)
			{
				return;
			}
			if (this.m_joinData is ServerJoinDataPlayFabUser)
			{
				this.m_isAwaitingPlayFabPingResponse = true;
				ZPlayFabMatchmaking.CheckHostOnlineStatus((this.m_joinData as ServerJoinDataPlayFabUser).m_remotePlayerId, new ZPlayFabMatchmakingSuccessCallback(this.PlayFabPingSuccess), new ZPlayFabMatchmakingFailedCallback(this.PlayFabPingFailed), false);
			}
			else if (this.m_joinData is ServerJoinDataDedicated)
			{
				IPEndPoint? ipendPoint = (this.m_joinData as ServerJoinDataDedicated).GetIPEndPoint();
				if (ipendPoint != null)
				{
					this.m_isAwaitingPlayFabPingResponse = true;
					ZPlayFabMatchmaking.FindHostByIp(ipendPoint.Value, new ZPlayFabMatchmakingSuccessCallback(this.PlayFabPingSuccess), new ZPlayFabMatchmakingFailedCallback(this.PlayFabPingFailed), false);
				}
			}
			else
			{
				ZLog.LogError("Tried to ping an unsupported server type with server data " + this.m_joinData.ToString());
			}
		}
		if (this.DoSteamPing)
		{
			this.m_isAwaitingSteamPingResponse = true;
		}
	}

	// Token: 0x06000C02 RID: 3074 RVA: 0x000628F0 File Offset: 0x00060AF0
	private void Update()
	{
		if (this.DoSteamPing && this.m_isAwaitingSteamPingResponse)
		{
			ServerStatus serverStatus = null;
			if (ZSteamMatchmaking.instance.CheckIfOnline(this.m_joinData, ref serverStatus))
			{
				if (serverStatus.m_joinData != null && serverStatus.OnlineStatus == OnlineStatus.Online && this.OnlineStatus != OnlineStatus.Online)
				{
					this.UpdateStatus(OnlineStatus.Online, serverStatus.m_joinData.m_serverName, serverStatus.m_playerCount, serverStatus.m_gameVersion, serverStatus.m_modifiers, serverStatus.m_networkVersion, serverStatus.m_isPasswordProtected, serverStatus.PlatformRestriction, serverStatus.m_hostId, true);
				}
				this.m_isAwaitingSteamPingResponse = false;
			}
		}
	}

	// Token: 0x06000C03 RID: 3075 RVA: 0x00062988 File Offset: 0x00060B88
	public bool TryGetResult()
	{
		this.Update();
		uint num = 0U;
		uint num2 = 0U;
		if (this.DoPlayFabPing)
		{
			num += 1U;
			if (!this.m_isAwaitingPlayFabPingResponse)
			{
				num2 += 1U;
				if (this.OnlineStatus == OnlineStatus.Online)
				{
					this.PingStatus = ServerPingStatus.Success;
					return true;
				}
			}
		}
		if (this.DoSteamPing)
		{
			num += 1U;
			if (!this.m_isAwaitingSteamPingResponse)
			{
				num2 += 1U;
				if (this.OnlineStatus == OnlineStatus.Online)
				{
					this.PingStatus = ServerPingStatus.Success;
					return true;
				}
			}
		}
		if (num == num2)
		{
			this.PingStatus = ServerPingStatus.CouldNotReach;
			this.OnlineStatus = OnlineStatus.Offline;
			return true;
		}
		return false;
	}

	// Token: 0x06000C04 RID: 3076 RVA: 0x00062A08 File Offset: 0x00060C08
	public void Reset()
	{
		this.PingStatus = ServerPingStatus.NotStarted;
		this.OnlineStatus = OnlineStatus.Unknown;
		this.m_playerCount = 0U;
		this.m_gameVersion = default(GameVersion);
		this.m_modifiers = null;
		this.m_networkVersion = 0U;
		this.m_isPasswordProtected = false;
		if (!(this.m_joinData is ServerJoinDataSteamUser))
		{
			this.PlatformRestriction = null;
		}
		this.m_isAwaitingSteamPingResponse = false;
		this.m_isAwaitingPlayFabPingResponse = false;
	}

	// Token: 0x04000CFF RID: 3327
	private Platform? m_platformRestriction;

	// Token: 0x04000D00 RID: 3328
	private bool m_isAwaitingSteamPingResponse;

	// Token: 0x04000D01 RID: 3329
	private bool m_isAwaitingPlayFabPingResponse;
}
