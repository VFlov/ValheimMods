using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000BA RID: 186
public class MasterClient
{
	// Token: 0x17000051 RID: 81
	// (get) Token: 0x06000B91 RID: 2961 RVA: 0x00060FFD File Offset: 0x0005F1FD
	public static MasterClient instance
	{
		get
		{
			return MasterClient.m_instance;
		}
	}

	// Token: 0x06000B92 RID: 2962 RVA: 0x00061004 File Offset: 0x0005F204
	public static void Initialize()
	{
		if (MasterClient.m_instance == null)
		{
			MasterClient.m_instance = new MasterClient();
		}
	}

	// Token: 0x06000B93 RID: 2963 RVA: 0x00061017 File Offset: 0x0005F217
	public MasterClient()
	{
		this.m_sessionUID = Utils.GenerateUID();
	}

	// Token: 0x06000B94 RID: 2964 RVA: 0x00061058 File Offset: 0x0005F258
	public void Dispose()
	{
		if (this.m_socket != null)
		{
			this.m_socket.Dispose();
		}
		if (this.m_connector != null)
		{
			this.m_connector.Dispose();
		}
		if (this.m_rpc != null)
		{
			this.m_rpc.Dispose();
		}
		if (MasterClient.m_instance == this)
		{
			MasterClient.m_instance = null;
		}
	}

	// Token: 0x06000B95 RID: 2965 RVA: 0x000610AC File Offset: 0x0005F2AC
	public void Update(float dt)
	{
	}

	// Token: 0x06000B96 RID: 2966 RVA: 0x000610BC File Offset: 0x0005F2BC
	private void SendStats(float duration)
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(2);
		zpackage.Write(this.m_sessionUID);
		zpackage.Write(Time.time);
		bool flag = Player.m_localPlayer != null;
		zpackage.Write(flag ? duration : 0f);
		bool flag2 = ZNet.instance && !ZNet.instance.IsServer();
		zpackage.Write(flag2 ? duration : 0f);
		zpackage.Write(global::Version.CurrentVersion.ToString());
		zpackage.Write(34U);
		bool flag3 = ZNet.instance && ZNet.instance.IsServer();
		zpackage.Write(flag3);
		if (flag3)
		{
			zpackage.Write(ZNet.instance.GetWorldUID());
			zpackage.Write(duration);
			int num = ZNet.instance.GetPeerConnections();
			if (Player.m_localPlayer != null)
			{
				num++;
			}
			zpackage.Write(num);
			bool data = ZNet.instance.GetZNat() != null && ZNet.instance.GetZNat().GetStatus();
			zpackage.Write(data);
		}
		PlayerProfile playerProfile = (Game.instance != null) ? Game.instance.GetPlayerProfile() : null;
		if (playerProfile != null)
		{
			zpackage.Write(true);
			zpackage.Write(playerProfile.GetPlayerID());
			zpackage.Write(105);
			for (int i = 0; i < 105; i++)
			{
				zpackage.Write(playerProfile.m_playerStats.m_stats[(PlayerStatType)i]);
			}
			zpackage.Write(playerProfile.m_usedCheats);
			zpackage.Write(new DateTimeOffset(playerProfile.m_dateCreated).ToUnixTimeSeconds());
			zpackage.Write(playerProfile.m_knownWorlds.Count);
			foreach (KeyValuePair<string, float> keyValuePair in playerProfile.m_knownWorlds)
			{
				zpackage.Write(keyValuePair.Key);
				zpackage.Write(keyValuePair.Value);
			}
			zpackage.Write(playerProfile.m_knownWorldKeys.Count);
			foreach (KeyValuePair<string, float> keyValuePair2 in playerProfile.m_knownWorldKeys)
			{
				zpackage.Write(keyValuePair2.Key);
				zpackage.Write(keyValuePair2.Value);
			}
			zpackage.Write(playerProfile.m_knownCommands.Count);
			using (Dictionary<string, float>.Enumerator enumerator = playerProfile.m_knownCommands.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<string, float> keyValuePair3 = enumerator.Current;
					zpackage.Write(keyValuePair3.Key);
					zpackage.Write(keyValuePair3.Value);
				}
				goto IL_2CD;
			}
		}
		zpackage.Write(false);
		IL_2CD:
		this.m_rpc.Invoke("Stats", new object[]
		{
			zpackage
		});
	}

	// Token: 0x06000B97 RID: 2967 RVA: 0x000613D8 File Offset: 0x0005F5D8
	public void RegisterServer(string name, string host, int port, bool password, bool upnp, long worldUID, GameVersion gameVersion, uint networkVersion, List<string> modifiers)
	{
		this.m_registerPkg = new ZPackage();
		this.m_registerPkg.Write(1);
		this.m_registerPkg.Write(name);
		this.m_registerPkg.Write(host);
		this.m_registerPkg.Write(port);
		this.m_registerPkg.Write(password);
		this.m_registerPkg.Write(upnp);
		this.m_registerPkg.Write(worldUID);
		this.m_registerPkg.Write(gameVersion.ToString());
		this.m_registerPkg.Write(networkVersion);
		this.m_registerPkg.Write(StringUtils.EncodeStringListAsString(modifiers, true));
		if (this.m_rpc != null)
		{
			this.m_rpc.Invoke("RegisterServer2", new object[]
			{
				this.m_registerPkg
			});
		}
		ZLog.Log(string.Concat(new string[]
		{
			"Registering server ",
			name,
			"  ",
			host,
			":",
			port.ToString()
		}));
	}

	// Token: 0x06000B98 RID: 2968 RVA: 0x000614E0 File Offset: 0x0005F6E0
	public void UnregisterServer()
	{
		if (this.m_registerPkg == null)
		{
			return;
		}
		if (this.m_rpc != null)
		{
			this.m_rpc.Invoke("UnregisterServer", Array.Empty<object>());
		}
		this.m_registerPkg = null;
	}

	// Token: 0x06000B99 RID: 2969 RVA: 0x0006150F File Offset: 0x0005F70F
	public List<ServerStatus> GetServers()
	{
		return this.m_servers;
	}

	// Token: 0x06000B9A RID: 2970 RVA: 0x00061517 File Offset: 0x0005F717
	public bool GetServers(List<ServerStatus> servers)
	{
		if (!this.m_haveServerlist)
		{
			return false;
		}
		servers.Clear();
		servers.AddRange(this.m_servers);
		return true;
	}

	// Token: 0x06000B9B RID: 2971 RVA: 0x00061536 File Offset: 0x0005F736
	public void RequestServerlist()
	{
		if (this.m_rpc != null)
		{
			this.m_rpc.Invoke("RequestServerlist2", Array.Empty<object>());
		}
	}

	// Token: 0x06000B9C RID: 2972 RVA: 0x00061558 File Offset: 0x0005F758
	private void RPC_ServerList(ZRpc rpc, ZPackage pkg)
	{
		this.m_haveServerlist = true;
		this.m_serverListRevision++;
		pkg.ReadInt();
		int num = pkg.ReadInt();
		this.m_servers.Clear();
		for (int i = 0; i < num; i++)
		{
			string serverName = pkg.ReadString();
			string str = pkg.ReadString();
			int num2 = pkg.ReadInt();
			bool isPasswordProtected = pkg.ReadBool();
			pkg.ReadBool();
			pkg.ReadLong();
			string versionString = pkg.ReadString();
			uint networkVersion = 0U;
			GameVersion gameVersion;
			if (GameVersion.TryParseGameVersion(versionString, out gameVersion) && gameVersion >= global::Version.FirstVersionWithNetworkVersion)
			{
				networkVersion = pkg.ReadUInt();
			}
			int playerCount = pkg.ReadInt();
			List<string> modifiers = new List<string>();
			if (gameVersion >= global::Version.FirstVersionWithModifiers)
			{
				StringUtils.TryDecodeStringAsICollection<List<string>>(pkg.ReadString(), out modifiers);
			}
			ServerStatus serverStatus = new ServerStatus(new ServerJoinDataDedicated(str + ":" + num2.ToString()));
			serverStatus.UpdateStatus(OnlineStatus.Online, serverName, (uint)playerCount, gameVersion, modifiers, networkVersion, isPasswordProtected, null, serverStatus.m_hostId, true);
			if (this.m_nameFilter.Length <= 0 || serverStatus.m_joinData.m_serverName.Contains(this.m_nameFilter))
			{
				this.m_servers.Add(serverStatus);
			}
		}
		if (this.m_onServerList != null)
		{
			this.m_onServerList(this.m_servers);
		}
	}

	// Token: 0x06000B9D RID: 2973 RVA: 0x000616B1 File Offset: 0x0005F8B1
	public int GetServerListRevision()
	{
		return this.m_serverListRevision;
	}

	// Token: 0x06000B9E RID: 2974 RVA: 0x000616B9 File Offset: 0x0005F8B9
	public bool IsConnected()
	{
		return this.m_rpc != null;
	}

	// Token: 0x06000B9F RID: 2975 RVA: 0x000616C4 File Offset: 0x0005F8C4
	public void SetNameFilter(string filter)
	{
		this.m_nameFilter = filter;
		ZLog.Log("filter is " + filter);
	}

	// Token: 0x04000CC5 RID: 3269
	private const int statVersion = 2;

	// Token: 0x04000CC6 RID: 3270
	public Action<List<ServerStatus>> m_onServerList;

	// Token: 0x04000CC7 RID: 3271
	private string m_msHost = "dvoid.noip.me";

	// Token: 0x04000CC8 RID: 3272
	private int m_msPort = 9983;

	// Token: 0x04000CC9 RID: 3273
	private long m_sessionUID;

	// Token: 0x04000CCA RID: 3274
	private ZConnector2 m_connector;

	// Token: 0x04000CCB RID: 3275
	private ZSocket2 m_socket;

	// Token: 0x04000CCC RID: 3276
	private ZRpc m_rpc;

	// Token: 0x04000CCD RID: 3277
	private bool m_haveServerlist;

	// Token: 0x04000CCE RID: 3278
	private List<ServerStatus> m_servers = new List<ServerStatus>();

	// Token: 0x04000CCF RID: 3279
	private ZPackage m_registerPkg;

	// Token: 0x04000CD0 RID: 3280
	private float m_sendStatsTimer;

	// Token: 0x04000CD1 RID: 3281
	private int m_serverListRevision;

	// Token: 0x04000CD2 RID: 3282
	private string m_nameFilter = "";

	// Token: 0x04000CD3 RID: 3283
	private static MasterClient m_instance;
}
