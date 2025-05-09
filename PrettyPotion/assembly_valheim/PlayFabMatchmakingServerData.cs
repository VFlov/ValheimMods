using System;
using System.Collections.Generic;
using Splatform;

// Token: 0x02000103 RID: 259
public class PlayFabMatchmakingServerData
{
	// Token: 0x06001053 RID: 4179 RVA: 0x000785E4 File Offset: 0x000767E4
	public override bool Equals(object obj)
	{
		PlayFabMatchmakingServerData playFabMatchmakingServerData = obj as PlayFabMatchmakingServerData;
		return playFabMatchmakingServerData != null && this.remotePlayerId == playFabMatchmakingServerData.remotePlayerId && this.serverIp == playFabMatchmakingServerData.serverIp && this.isDedicatedServer == playFabMatchmakingServerData.isDedicatedServer;
	}

	// Token: 0x06001054 RID: 4180 RVA: 0x00078634 File Offset: 0x00076834
	public override int GetHashCode()
	{
		return ((1416698207 * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.remotePlayerId)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.serverIp)) * -1521134295 + this.isDedicatedServer.GetHashCode();
	}

	// Token: 0x06001055 RID: 4181 RVA: 0x00078688 File Offset: 0x00076888
	public override string ToString()
	{
		return string.Format("Server Name : {0}\nServer IP : {1}\nGame Version : {2}\nNetwork Version : {3}\nPlayer ID : {4}\nPlayers : {5}\nLobby ID : {6}\nNetwork ID : {7}\nJoin Code : {8}\nPlatform Restriction : {9}\nDedicated : {10}\nCommunity : {11}\nTickCreated : {12}\nModifiers : {13}\n", new object[]
		{
			this.serverName,
			this.serverIp,
			this.gameVersion,
			this.networkVersion,
			this.remotePlayerId,
			this.numPlayers,
			this.lobbyId,
			this.networkId,
			this.joinCode,
			this.platformRestriction,
			this.isDedicatedServer,
			this.isCommunityServer,
			this.tickCreated,
			this.modifiers
		});
	}

	// Token: 0x04000F8F RID: 3983
	public string serverName;

	// Token: 0x04000F90 RID: 3984
	public string worldName;

	// Token: 0x04000F91 RID: 3985
	public GameVersion gameVersion;

	// Token: 0x04000F92 RID: 3986
	public List<string> modifiers;

	// Token: 0x04000F93 RID: 3987
	public uint networkVersion;

	// Token: 0x04000F94 RID: 3988
	public string networkId = "";

	// Token: 0x04000F95 RID: 3989
	public string joinCode;

	// Token: 0x04000F96 RID: 3990
	public string remotePlayerId;

	// Token: 0x04000F97 RID: 3991
	public string lobbyId;

	// Token: 0x04000F98 RID: 3992
	public PlatformUserID platformUserID;

	// Token: 0x04000F99 RID: 3993
	public string serverIp = "";

	// Token: 0x04000F9A RID: 3994
	public Platform platformRestriction = Platform.Unknown;

	// Token: 0x04000F9B RID: 3995
	public bool isDedicatedServer;

	// Token: 0x04000F9C RID: 3996
	public bool isCommunityServer;

	// Token: 0x04000F9D RID: 3997
	public bool havePassword;

	// Token: 0x04000F9E RID: 3998
	public uint numPlayers;

	// Token: 0x04000F9F RID: 3999
	public long tickCreated;
}
