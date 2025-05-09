using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using GUIFramework;
using Splatform;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UserManagement;

// Token: 0x020000DA RID: 218
public class ZNet : MonoBehaviour
{
	// Token: 0x17000077 RID: 119
	// (get) Token: 0x06000D93 RID: 3475 RVA: 0x0006A22E File Offset: 0x0006842E
	public static ZNet instance
	{
		get
		{
			return ZNet.m_instance;
		}
	}

	// Token: 0x06000D94 RID: 3476 RVA: 0x0006A238 File Offset: 0x00068438
	private void Awake()
	{
		ZNet.m_instance = this;
		ZNet.m_loadError = false;
		this.m_routedRpc = new ZRoutedRpc(ZNet.m_isServer);
		this.m_zdoMan = new ZDOMan(this.m_zdoSectorsWidth);
		this.m_passwordDialog.gameObject.SetActive(false);
		this.m_connectingDialog.gameObject.SetActive(false);
		WorldGenerator.Deitialize();
		if (SteamManager.Initialize())
		{
			string personaName = SteamFriends.GetPersonaName();
			ZLog.Log("Steam initialized, persona:" + personaName);
			ZSteamMatchmaking.Initialize();
			ZPlayFabMatchmaking.Initialize(ZNet.m_isServer);
			ZNet.m_backupCount = PlatformPrefs.GetInt("AutoBackups", ZNet.m_backupCount);
			ZNet.m_backupShort = PlatformPrefs.GetInt("AutoBackups_short", ZNet.m_backupShort);
			ZNet.m_backupLong = PlatformPrefs.GetInt("AutoBackups_long", ZNet.m_backupLong);
			if (ZNet.m_isServer)
			{
				FileHelpers.MigrateLocalSyncedListsToCloud();
				if (FileHelpers.LocalStorageSupport == LocalStorageSupport.Supported)
				{
					this.m_adminList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Local, Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/adminlist.txt"), "List admin players ID  ONE per line");
					this.m_bannedList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Local, Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/bannedlist.txt"), "List banned players ID  ONE per line");
					this.m_permittedList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Local, Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/permittedlist.txt"), "List permitted players ID ONE per line");
				}
				else if (FileHelpers.CloudStorageEnabled)
				{
					this.m_adminList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Cloud, Utils.GetSaveDataPath(FileHelpers.FileSource.Cloud) + "/adminlist.txt"), "List admin players ID  ONE per line");
					this.m_bannedList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Cloud, Utils.GetSaveDataPath(FileHelpers.FileSource.Cloud) + "/bannedlist.txt"), "List banned players ID  ONE per line");
					this.m_permittedList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Cloud, Utils.GetSaveDataPath(FileHelpers.FileSource.Cloud) + "/permittedlist.txt"), "List permitted players ID ONE per line");
				}
				else
				{
					ZLog.LogError("Neither Local nor Cloud/Platform storage is enabled on this platform!");
				}
				this.m_adminListForRpc = this.m_adminList.GetList();
				if (ZNet.m_world == null)
				{
					ZNet.m_publicServer = false;
					ZNet.m_world = World.GetDevWorld();
				}
				WorldGenerator.Initialize(ZNet.m_world);
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connected;
				ZNet.m_externalError = ZNet.ConnectionStatus.None;
			}
			this.m_routedRpc.SetUID(ZDOMan.GetSessionID());
			if (this.IsServer())
			{
				this.SendPlayerList();
			}
			if (!this.IsDedicated())
			{
				this.m_serverSyncedPlayerData["platformDisplayName"] = PlatformManager.DistributionPlatform.LocalUser.DisplayName;
			}
			return;
		}
	}

	// Token: 0x06000D95 RID: 3477 RVA: 0x0006A49A File Offset: 0x0006869A
	private void OnGenerationFinished()
	{
		if (!ZNet.m_openServer)
		{
			return;
		}
		this.OpenServer();
	}

	// Token: 0x06000D96 RID: 3478 RVA: 0x0006A4AC File Offset: 0x000686AC
	public void OpenServer()
	{
		if (!ZNet.m_isServer)
		{
			return;
		}
		ZNet.m_openServer = true;
		bool flag = ZNet.m_serverPassword != "";
		GameVersion currentVersion = global::Version.CurrentVersion;
		uint networkVersion = 34U;
		List<string> startingGlobalKeys = ZNet.m_world.m_startingGlobalKeys;
		ZSteamMatchmaking.instance.RegisterServer(ZNet.m_ServerName, flag, currentVersion, startingGlobalKeys, networkVersion, ZNet.m_publicServer, ZNet.m_world.m_seedName, new ZSteamMatchmaking.ServerRegistered(this.OnSteamServerRegistered));
		if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
		{
			ZSteamSocket zsteamSocket = new ZSteamSocket();
			zsteamSocket.StartHost();
			this.m_hostSocket = zsteamSocket;
			ZLog.Log("Opened Steam server");
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			ZPlayFabMatchmaking.instance.RegisterServer(ZNet.m_ServerName, flag, ZNet.m_publicServer, currentVersion, startingGlobalKeys, networkVersion, ZNet.m_world.m_seedName, true);
			ZPlayFabSocket zplayFabSocket = new ZPlayFabSocket();
			zplayFabSocket.StartHost();
			this.m_hostSocket = zplayFabSocket;
			ZLog.Log("Opened PlayFab server");
		}
	}

	// Token: 0x06000D97 RID: 3479 RVA: 0x0006A58C File Offset: 0x0006878C
	private void Start()
	{
		ZRpc.SetLongTimeout(false);
		ZLog.Log("ZNET START");
		MuteList.Load(null);
		if (ZNet.m_isServer)
		{
			this.ServerLoadWorld();
			return;
		}
		this.ClientConnect();
	}

	// Token: 0x06000D98 RID: 3480 RVA: 0x0006A5B8 File Offset: 0x000687B8
	private void ServerLoadWorld()
	{
		this.LoadWorld();
		ZoneSystem.instance.GenerateLocationsIfNeeded();
		ZoneSystem.instance.GenerateLocationsCompleted += this.OnGenerationFinished;
		if (ZNet.m_loadError)
		{
			ZLog.LogError("World db couldn't load correctly, saving has been disabled to prevent .old file from being overwritten.");
		}
	}

	// Token: 0x06000D99 RID: 3481 RVA: 0x0006A5F4 File Offset: 0x000687F4
	private void ClientConnect()
	{
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			ZLog.Log("Connecting to server with PlayFab-backend " + ZNet.m_serverPlayFabPlayerId);
			this.Connect(ZNet.m_serverPlayFabPlayerId);
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
		{
			if (ZNet.m_serverSteamID == 0UL)
			{
				ZLog.Log("Connecting to server with Steam-backend " + ZNet.m_serverHost + ":" + ZNet.m_serverHostPort.ToString());
				SteamNetworkingIPAddr host = default(SteamNetworkingIPAddr);
				host.ParseString(ZNet.m_serverHost + ":" + ZNet.m_serverHostPort.ToString());
				this.Connect(host);
				return;
			}
			ZLog.Log("Connecting to server with Steam-backend " + ZNet.m_serverSteamID.ToString());
			this.Connect(new CSteamID(ZNet.m_serverSteamID));
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.CustomSocket)
		{
			ZLog.Log("Connecting to server with socket-backend " + ZNet.m_serverHost + "  " + ZNet.m_serverHostPort.ToString());
			this.Connect(ZNet.m_serverHost, ZNet.m_serverHostPort);
		}
	}

	// Token: 0x06000D9A RID: 3482 RVA: 0x0006A6F3 File Offset: 0x000688F3
	private string GetServerIP()
	{
		return ZNet.GetPublicIP(0);
	}

	// Token: 0x06000D9B RID: 3483 RVA: 0x0006A6FC File Offset: 0x000688FC
	private string LocalIPAddress()
	{
		string text = IPAddress.Loopback.ToString();
		try
		{
			foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
			{
				if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
				{
					text = ipaddress.ToString();
					break;
				}
			}
		}
		catch (Exception ex)
		{
			ZLog.Log(string.Format("Failed to get local address, using {0}: {1}", text, ex.Message));
		}
		return text;
	}

	// Token: 0x06000D9C RID: 3484 RVA: 0x0006A778 File Offset: 0x00068978
	public static bool ContainsValidIP(string containsIPAddress, out string ipAddress)
	{
		string text;
		if (ZNet.ContainsValidIPv4(containsIPAddress, out text))
		{
			Debug.Log("Found ipv4 address to be " + text);
			ipAddress = text;
			return true;
		}
		ipAddress = "";
		return false;
	}

	// Token: 0x06000D9D RID: 3485 RVA: 0x0006A7AC File Offset: 0x000689AC
	private static bool ContainsValidIPv6(string potentialIPv6Address, out string ipAddress)
	{
		IPAddress ipaddress;
		if (IPAddress.TryParse(potentialIPv6Address, out ipaddress))
		{
			ipAddress = ipaddress.ToString();
			ZLog.Log("Found IPv6 address! Using " + ipAddress + ".");
			return true;
		}
		ipAddress = "";
		return false;
	}

	// Token: 0x06000D9E RID: 3486 RVA: 0x0006A7EC File Offset: 0x000689EC
	private static bool ContainsValidIPv4(string containsIPAddress, out string ipAddress)
	{
		MatchCollection matchCollection = new Regex("\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}").Matches(containsIPAddress);
		if (matchCollection.Count > 0)
		{
			ipAddress = matchCollection[0].ToString();
			return true;
		}
		ipAddress = "";
		return false;
	}

	// Token: 0x06000D9F RID: 3487 RVA: 0x0006A82C File Offset: 0x00068A2C
	public static string GetPublicIP(int ipGetAttempts)
	{
		string result;
		try
		{
			string[] array = new string[]
			{
				"https://ipv4.icanhazip.com/",
				"https://api.ipify.org",
				"https://ipv4.myip.wtf/text",
				"https://checkip.amazonaws.com/",
				"https://ipinfo.io/ip/"
			};
			string[] array2 = new string[]
			{
				"https://ipv6.icanhazip.com/",
				"https://api6.ipify.org",
				"https://ipv6.myip.wtf/text"
			};
			System.Random random = new System.Random();
			string text = ZNet.<GetPublicIP>g__DownloadString|15_0((ipGetAttempts < 5) ? array[random.Next(array.Length)] : array2[random.Next(array2.Length)], 5000);
			string text2;
			if (!ZNet.ContainsValidIP(text, out text2))
			{
				throw new Exception("Could not extract valid IP address from externalIP download string.");
			}
			text = text2;
			result = text;
		}
		catch (Exception ex)
		{
			ZLog.LogError(ex.Message);
			result = "";
		}
		return result;
	}

	// Token: 0x06000DA0 RID: 3488 RVA: 0x0006A8F8 File Offset: 0x00068AF8
	private void OnSteamServerRegistered(bool success)
	{
		if (!success)
		{
			this.m_registerAttempts++;
			float num = 1f * Mathf.Pow(2f, (float)(this.m_registerAttempts - 1));
			num = Mathf.Min(num, 30f);
			num *= UnityEngine.Random.Range(0.875f, 1.125f);
			this.<OnSteamServerRegistered>g__RetryRegisterAfterDelay|16_0(num);
		}
	}

	// Token: 0x06000DA1 RID: 3489 RVA: 0x0006A955 File Offset: 0x00068B55
	public void Shutdown(bool save = true)
	{
		ZLog.Log("ZNet Shutdown");
		if (save)
		{
			this.Save(true, false, false);
		}
		this.StopAll(false);
		base.enabled = false;
	}

	// Token: 0x06000DA2 RID: 3490 RVA: 0x0006A97B File Offset: 0x00068B7B
	public void ShutdownWithoutSave(bool suspending)
	{
		ZLog.Log("ZNet Shutdown without save");
		this.StopAll(suspending);
		base.enabled = false;
	}

	// Token: 0x06000DA3 RID: 3491 RVA: 0x0006A998 File Offset: 0x00068B98
	private void StopAll(bool suspending = false)
	{
		if (this.m_haveStoped)
		{
			return;
		}
		this.m_haveStoped = true;
		if (this.m_saveThread != null && this.m_saveThread.IsAlive)
		{
			this.m_saveThread.Join();
			this.m_saveThread = null;
		}
		if (!suspending)
		{
			this.m_zdoMan.ShutDown();
		}
		this.SendDisconnect();
		ZSteamMatchmaking.instance.ReleaseSessionTicket();
		ZSteamMatchmaking.instance.UnregisterServer();
		ZPlayFabMatchmaking instance = ZPlayFabMatchmaking.instance;
		if (instance != null)
		{
			instance.UnregisterServer();
		}
		if (this.m_hostSocket != null)
		{
			this.m_hostSocket.Dispose();
		}
		if (this.m_serverConnector != null)
		{
			this.m_serverConnector.Dispose();
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			znetPeer.Dispose();
		}
		this.m_peers.Clear();
	}

	// Token: 0x06000DA4 RID: 3492 RVA: 0x0006AA88 File Offset: 0x00068C88
	private void OnDestroy()
	{
		ZLog.Log("ZNet OnDestroy");
		if (ZNet.m_instance == this)
		{
			ZNet.m_instance = null;
		}
	}

	// Token: 0x06000DA5 RID: 3493 RVA: 0x0006AAA8 File Offset: 0x00068CA8
	private ZNetPeer Connect(ISocket socket)
	{
		ZNetPeer znetPeer = new ZNetPeer(socket, true);
		this.OnNewConnection(znetPeer);
		ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connecting;
		ZNet.m_externalError = ZNet.ConnectionStatus.None;
		this.m_connectingDialog.gameObject.SetActive(true);
		return znetPeer;
	}

	// Token: 0x06000DA6 RID: 3494 RVA: 0x0006AAE4 File Offset: 0x00068CE4
	public void Connect(string remotePlayerId)
	{
		ZNet.<>c__DisplayClass22_0 CS$<>8__locals1 = new ZNet.<>c__DisplayClass22_0();
		CS$<>8__locals1.<>4__this = this;
		CS$<>8__locals1.socket = null;
		CS$<>8__locals1.peer = null;
		CS$<>8__locals1.socket = new ZPlayFabSocket(remotePlayerId, new Action<PlayFabMatchmakingServerData>(CS$<>8__locals1.<Connect>g__CheckServerData|0));
		CS$<>8__locals1.peer = this.Connect(CS$<>8__locals1.socket);
	}

	// Token: 0x06000DA7 RID: 3495 RVA: 0x0006AB36 File Offset: 0x00068D36
	public void Connect(CSteamID hostID)
	{
		this.Connect(new ZSteamSocket(hostID));
	}

	// Token: 0x06000DA8 RID: 3496 RVA: 0x0006AB45 File Offset: 0x00068D45
	public void Connect(SteamNetworkingIPAddr host)
	{
		this.Connect(new ZSteamSocket(host));
	}

	// Token: 0x06000DA9 RID: 3497 RVA: 0x0006AB54 File Offset: 0x00068D54
	public void Connect(string host, int port)
	{
		this.m_serverConnector = new ZConnector2(host, port);
		ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connecting;
		ZNet.m_externalError = ZNet.ConnectionStatus.None;
		this.m_connectingDialog.gameObject.SetActive(true);
	}

	// Token: 0x06000DAA RID: 3498 RVA: 0x0006AB80 File Offset: 0x00068D80
	private void UpdateClientConnector(float dt)
	{
		if (this.m_serverConnector != null && this.m_serverConnector.UpdateStatus(dt, true))
		{
			ZSocket2 zsocket = this.m_serverConnector.Complete();
			if (zsocket != null)
			{
				ZLog.Log("Connection established to " + this.m_serverConnector.GetEndPointString());
				ZNetPeer peer = new ZNetPeer(zsocket, true);
				this.OnNewConnection(peer);
			}
			else
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorConnectFailed;
				ZLog.Log("Failed to connect to server");
			}
			this.m_serverConnector.Dispose();
			this.m_serverConnector = null;
		}
	}

	// Token: 0x06000DAB RID: 3499 RVA: 0x0006AC00 File Offset: 0x00068E00
	private void OnNewConnection(ZNetPeer peer)
	{
		this.m_peers.Add(peer);
		peer.m_rpc.Register<ZPackage>("PeerInfo", new Action<ZRpc, ZPackage>(this.RPC_PeerInfo));
		peer.m_rpc.Register("Disconnect", new ZRpc.RpcMethod.Method(this.RPC_Disconnect));
		peer.m_rpc.Register("SavePlayerProfile", new ZRpc.RpcMethod.Method(this.RPC_SavePlayerProfile));
		if (ZNet.m_isServer)
		{
			peer.m_rpc.Register("ServerHandshake", new ZRpc.RpcMethod.Method(this.RPC_ServerHandshake));
			return;
		}
		peer.m_rpc.Register("Kicked", new ZRpc.RpcMethod.Method(this.RPC_Kicked));
		peer.m_rpc.Register<int>("Error", new Action<ZRpc, int>(this.RPC_Error));
		peer.m_rpc.Register<bool, string>("ClientHandshake", new Action<ZRpc, bool, string>(this.RPC_ClientHandshake));
		peer.m_rpc.Invoke("ServerHandshake", Array.Empty<object>());
	}

	// Token: 0x06000DAC RID: 3500 RVA: 0x0006ACFC File Offset: 0x00068EFC
	public void SaveOtherPlayerProfiles()
	{
		ZLog.Log("Sending message to save player profiles");
		if (!this.IsServer())
		{
			ZLog.Log("Only server can save the player profiles");
			return;
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.m_rpc != null)
			{
				ZLog.Log("Sent to " + znetPeer.m_socket.GetEndPointString());
				znetPeer.m_rpc.Invoke("SavePlayerProfile", Array.Empty<object>());
			}
		}
	}

	// Token: 0x06000DAD RID: 3501 RVA: 0x0006AD9C File Offset: 0x00068F9C
	private void RPC_SavePlayerProfile(ZRpc rpc)
	{
		Game.instance.SavePlayerProfile(true);
	}

	// Token: 0x06000DAE RID: 3502 RVA: 0x0006ADAC File Offset: 0x00068FAC
	private void RPC_ServerHandshake(ZRpc rpc)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer == null)
		{
			return;
		}
		ZLog.Log("Got handshake from client " + peer.m_socket.GetEndPointString());
		this.ClearPlayerData(peer);
		bool flag = !string.IsNullOrEmpty(ZNet.m_serverPassword);
		peer.m_rpc.Invoke("ClientHandshake", new object[]
		{
			flag,
			ZNet.ServerPasswordSalt()
		});
	}

	// Token: 0x06000DAF RID: 3503 RVA: 0x0006AE1B File Offset: 0x0006901B
	public bool InPasswordDialog()
	{
		return this.m_passwordDialog.gameObject.activeSelf;
	}

	// Token: 0x06000DB0 RID: 3504 RVA: 0x0006AE2D File Offset: 0x0006902D
	public bool InConnectingScreen()
	{
		return this.m_connectingDialog.gameObject.activeSelf;
	}

	// Token: 0x06000DB1 RID: 3505 RVA: 0x0006AE40 File Offset: 0x00069040
	private void RPC_ClientHandshake(ZRpc rpc, bool needPassword, string serverPasswordSalt)
	{
		this.m_connectingDialog.gameObject.SetActive(false);
		ZNet.m_serverPasswordSalt = serverPasswordSalt;
		if (needPassword)
		{
			this.m_passwordDialog.gameObject.SetActive(true);
			GuiInputField componentInChildren = this.m_passwordDialog.GetComponentInChildren<GuiInputField>();
			componentInChildren.text = "";
			componentInChildren.ActivateInputField();
			componentInChildren.OnInputSubmit.AddListener(new UnityAction<string>(this.OnPasswordEntered));
			this.m_tempPasswordRPC = rpc;
			return;
		}
		this.SendPeerInfo(rpc, "");
	}

	// Token: 0x06000DB2 RID: 3506 RVA: 0x0006AEC0 File Offset: 0x000690C0
	private void OnPasswordEntered(string pwd)
	{
		if (!this.m_tempPasswordRPC.IsConnected())
		{
			return;
		}
		if (string.IsNullOrEmpty(pwd))
		{
			return;
		}
		this.m_passwordDialog.GetComponentInChildren<GuiInputField>().OnInputSubmit.RemoveListener(new UnityAction<string>(this.OnPasswordEntered));
		this.m_passwordDialog.gameObject.SetActive(false);
		this.SendPeerInfo(this.m_tempPasswordRPC, pwd);
		this.m_tempPasswordRPC = null;
	}

	// Token: 0x06000DB3 RID: 3507 RVA: 0x0006AF2C File Offset: 0x0006912C
	private void SendPeerInfo(ZRpc rpc, string password = "")
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(ZNet.GetUID());
		zpackage.Write(global::Version.CurrentVersion.ToString());
		zpackage.Write(34U);
		zpackage.Write(this.m_referencePosition);
		zpackage.Write(Game.instance.GetPlayerProfile().GetName());
		if (this.IsServer())
		{
			zpackage.Write(ZNet.m_world.m_name);
			zpackage.Write(ZNet.m_world.m_seed);
			zpackage.Write(ZNet.m_world.m_seedName);
			zpackage.Write(ZNet.m_world.m_uid);
			zpackage.Write(ZNet.m_world.m_worldGenVersion);
			zpackage.Write(this.m_netTime);
		}
		else
		{
			string data = string.IsNullOrEmpty(password) ? "" : ZNet.HashPassword(password, ZNet.ServerPasswordSalt());
			zpackage.Write(data);
			rpc.GetSocket().GetHostName();
			SteamNetworkingIdentity steamNetworkingIdentity = default(SteamNetworkingIdentity);
			steamNetworkingIdentity.SetSteamID(new CSteamID(ZNet.m_serverSteamID));
			byte[] array = ZSteamMatchmaking.instance.RequestSessionTicket(ref steamNetworkingIdentity);
			if (array == null)
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorConnectFailed;
				return;
			}
			zpackage.Write(array);
		}
		rpc.Invoke("PeerInfo", new object[]
		{
			zpackage
		});
	}

	// Token: 0x06000DB4 RID: 3508 RVA: 0x0006B070 File Offset: 0x00069270
	private void RPC_PeerInfo(ZRpc rpc, ZPackage pkg)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer == null)
		{
			return;
		}
		long uid = pkg.ReadLong();
		string versionString = pkg.ReadString();
		uint num = 0U;
		GameVersion gameVersion;
		if (GameVersion.TryParseGameVersion(versionString, out gameVersion) && gameVersion >= global::Version.FirstVersionWithNetworkVersion)
		{
			num = pkg.ReadUInt();
		}
		string name = peer.m_socket.GetEndPointString();
		string hostName = peer.m_socket.GetHostName();
		ZLog.Log("Network version check, their:" + num.ToString() + ", mine:" + 34U.ToString());
		if (num != 34U)
		{
			if (ZNet.m_isServer)
			{
				rpc.Invoke("Error", new object[]
				{
					3
				});
			}
			else
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorVersion;
			}
			string[] array = new string[11];
			array[0] = "Peer ";
			array[1] = name;
			array[2] = " has incompatible version, mine:";
			array[3] = global::Version.CurrentVersion.ToString();
			array[4] = " (network version ";
			array[5] = 34U.ToString();
			array[6] = ")   remote ";
			int num2 = 7;
			GameVersion gameVersion2 = gameVersion;
			array[num2] = gameVersion2.ToString();
			array[8] = " (network version ";
			array[9] = ((num == uint.MaxValue) ? "unknown" : num.ToString());
			array[10] = ")";
			ZLog.Log(string.Concat(array));
			return;
		}
		Vector3 refPos = pkg.ReadVector3();
		string text = pkg.ReadString();
		if (ZNet.m_isServer)
		{
			if (!this.IsAllowed(hostName, text))
			{
				rpc.Invoke("Error", new object[]
				{
					8
				});
				ZLog.Log(string.Concat(new string[]
				{
					"Player ",
					text,
					" : ",
					hostName,
					" is blacklisted or not in whitelist."
				}));
				return;
			}
			string b = pkg.ReadString();
			if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
			{
				ZSteamSocket zsteamSocket = peer.m_socket as ZSteamSocket;
				byte[] ticket = pkg.ReadByteArray();
				if (!ZSteamMatchmaking.instance.VerifySessionTicket(ticket, zsteamSocket.GetPeerID()))
				{
					ZLog.Log("Peer " + name + " has invalid session ticket");
					rpc.Invoke("Error", new object[]
					{
						8
					});
					return;
				}
			}
			if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
			{
				PlatformUserID platformUserID = new PlatformUserID(peer.m_socket.GetHostName());
				if (!platformUserID.IsValid)
				{
					ZLog.LogError("Failed to parse peer id! Using blank ID with unknown platform.");
					platformUserID = default(PlatformUserID);
				}
				if (PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) != PrivilegeResult.Granted && PlatformManager.DistributionPlatform.Platform != platformUserID.m_platform)
				{
					rpc.Invoke("Error", new object[]
					{
						10
					});
					ZLog.Log("Peer diconnected due to server platform privileges disallowing crossplay. Server platform: " + PlatformManager.DistributionPlatform.Platform.ToString() + "   Peer platform: " + platformUserID.m_platform.ToString());
					return;
				}
				PlayFabManager.CheckIfUserAuthenticated((peer.m_socket as ZPlayFabSocket).m_remotePlayerId, platformUserID, delegate(bool isAuthenticated)
				{
					if (!isAuthenticated)
					{
						rpc.Invoke("Error", new object[]
						{
							5
						});
						ZLog.Log("Peer " + name + " disconnected because they were not authenticated!");
					}
				});
			}
			if (this.GetNrOfPlayers() >= 10)
			{
				rpc.Invoke("Error", new object[]
				{
					9
				});
				ZLog.Log("Peer " + name + " disconnected due to server is full");
				return;
			}
			if (ZNet.m_serverPassword != b)
			{
				rpc.Invoke("Error", new object[]
				{
					6
				});
				ZLog.Log("Peer " + name + " has wrong password");
				return;
			}
			if (this.IsConnected(uid))
			{
				rpc.Invoke("Error", new object[]
				{
					7
				});
				ZLog.Log("Already connected to peer with UID:" + uid.ToString() + "  " + name);
				return;
			}
		}
		else
		{
			ZNet.m_world = new World();
			ZNet.m_world.m_name = pkg.ReadString();
			ZNet.m_world.m_seed = pkg.ReadInt();
			ZNet.m_world.m_seedName = pkg.ReadString();
			ZNet.m_world.m_uid = pkg.ReadLong();
			ZNet.m_world.m_worldGenVersion = pkg.ReadInt();
			WorldGenerator.Initialize(ZNet.m_world);
			this.m_netTime = pkg.ReadDouble();
		}
		peer.m_refPos = refPos;
		peer.m_uid = uid;
		peer.m_playerName = text;
		rpc.Register<ZPackage>("ServerSyncedPlayerData", new Action<ZRpc, ZPackage>(this.RPC_ServerSyncedPlayerData));
		rpc.Register<ZPackage>("PlayerList", new Action<ZRpc, ZPackage>(this.RPC_PlayerList));
		rpc.Register<ZPackage>("AdminList", new Action<ZRpc, ZPackage>(this.RPC_AdminList));
		rpc.Register<string>("RemotePrint", new Action<ZRpc, string>(this.RPC_RemotePrint));
		if (ZNet.m_isServer)
		{
			rpc.Register<ZDOID>("CharacterID", new Action<ZRpc, ZDOID>(this.RPC_CharacterID));
			rpc.Register<string>("Kick", new Action<ZRpc, string>(this.RPC_Kick));
			rpc.Register<string>("Ban", new Action<ZRpc, string>(this.RPC_Ban));
			rpc.Register<string>("Unban", new Action<ZRpc, string>(this.RPC_Unban));
			rpc.Register<string>("RPC_RemoteCommand", new Action<ZRpc, string>(this.RPC_RemoteCommand));
			rpc.Register("Save", new ZRpc.RpcMethod.Method(this.RPC_Save));
			rpc.Register("PrintBanned", new ZRpc.RpcMethod.Method(this.RPC_PrintBanned));
		}
		else
		{
			rpc.Register<double>("NetTime", new Action<ZRpc, double>(this.RPC_NetTime));
		}
		if (ZNet.m_isServer)
		{
			this.SendPeerInfo(rpc, "");
			peer.m_socket.VersionMatch();
			this.SendPlayerList();
			this.SendAdminList();
		}
		else
		{
			peer.m_socket.VersionMatch();
			ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connected;
		}
		this.m_zdoMan.AddPeer(peer);
		this.m_routedRpc.AddPeer(peer);
	}

	// Token: 0x06000DB5 RID: 3509 RVA: 0x0006B6C8 File Offset: 0x000698C8
	private void SendDisconnect()
	{
		ZLog.Log("Sending disconnect msg");
		foreach (ZNetPeer peer in this.m_peers)
		{
			this.SendDisconnect(peer);
		}
	}

	// Token: 0x06000DB6 RID: 3510 RVA: 0x0006B728 File Offset: 0x00069928
	private void SendDisconnect(ZNetPeer peer)
	{
		if (peer.m_rpc != null)
		{
			ZLog.Log("Sent to " + peer.m_socket.GetEndPointString());
			peer.m_rpc.Invoke("Disconnect", Array.Empty<object>());
		}
	}

	// Token: 0x06000DB7 RID: 3511 RVA: 0x0006B764 File Offset: 0x00069964
	private void RPC_Disconnect(ZRpc rpc)
	{
		ZLog.Log("RPC_Disconnect");
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer != null)
		{
			if (peer.m_server)
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorDisconnected;
			}
			this.Disconnect(peer);
		}
	}

	// Token: 0x06000DB8 RID: 3512 RVA: 0x0006B79C File Offset: 0x0006999C
	private void RPC_Error(ZRpc rpc, int error)
	{
		ZNet.ConnectionStatus connectionStatus = (ZNet.ConnectionStatus)error;
		ZNet.m_connectionStatus = connectionStatus;
		ZLog.Log("Got connectoin error msg " + connectionStatus.ToString());
	}

	// Token: 0x06000DB9 RID: 3513 RVA: 0x0006B7D0 File Offset: 0x000699D0
	public bool IsConnected(long uid)
	{
		if (uid == ZNet.GetUID())
		{
			return true;
		}
		using (List<ZNetPeer>.Enumerator enumerator = this.m_peers.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_uid == uid)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06000DBA RID: 3514 RVA: 0x0006B834 File Offset: 0x00069A34
	private void ClearPlayerData(ZNetPeer peer)
	{
		this.m_routedRpc.RemovePeer(peer);
		this.m_zdoMan.RemovePeer(peer);
	}

	// Token: 0x06000DBB RID: 3515 RVA: 0x0006B84E File Offset: 0x00069A4E
	public void Disconnect(ZNetPeer peer)
	{
		this.ClearPlayerData(peer);
		this.m_peers.Remove(peer);
		peer.Dispose();
		if (ZNet.m_isServer)
		{
			this.SendPlayerList();
		}
	}

	// Token: 0x06000DBC RID: 3516 RVA: 0x0006B877 File Offset: 0x00069A77
	private void FixedUpdate()
	{
		this.UpdateNetTime(Time.fixedDeltaTime);
	}

	// Token: 0x06000DBD RID: 3517 RVA: 0x0006B884 File Offset: 0x00069A84
	private void Update()
	{
		float deltaTime = Time.deltaTime;
		ZSteamSocket.UpdateAllSockets(deltaTime);
		ZPlayFabSocket.UpdateAllSockets(deltaTime);
		if (this.IsServer())
		{
			this.UpdateBanList(deltaTime);
		}
		this.CheckForIncommingServerConnections();
		this.UpdatePeers(deltaTime);
		this.SendPeriodicData(deltaTime);
		this.m_zdoMan.Update(deltaTime);
		this.UpdateSave();
		if (ZNet.PeersToDisconnectAfterKick.Count < 1)
		{
			return;
		}
		foreach (ZNetPeer znetPeer in ZNet.PeersToDisconnectAfterKick.Keys.ToArray<ZNetPeer>())
		{
			if (Time.time >= ZNet.PeersToDisconnectAfterKick[znetPeer])
			{
				this.Disconnect(znetPeer);
				ZNet.PeersToDisconnectAfterKick.Remove(znetPeer);
			}
		}
	}

	// Token: 0x06000DBE RID: 3518 RVA: 0x0006B92D File Offset: 0x00069B2D
	private void LateUpdate()
	{
		ZPlayFabSocket.LateUpdateAllSocket();
	}

	// Token: 0x06000DBF RID: 3519 RVA: 0x0006B934 File Offset: 0x00069B34
	private void UpdateNetTime(float dt)
	{
		if (this.IsServer())
		{
			if (this.GetNrOfPlayers() > 0)
			{
				this.m_netTime += (double)dt;
				return;
			}
		}
		else
		{
			this.m_netTime += (double)dt;
		}
	}

	// Token: 0x06000DC0 RID: 3520 RVA: 0x0006B968 File Offset: 0x00069B68
	private void UpdateBanList(float dt)
	{
		this.m_banlistTimer += dt;
		if (this.m_banlistTimer > 5f)
		{
			this.m_banlistTimer = 0f;
			this.CheckWhiteList();
			foreach (string user in this.m_bannedList.GetList())
			{
				this.InternalKick(user);
			}
		}
	}

	// Token: 0x06000DC1 RID: 3521 RVA: 0x0006B9EC File Offset: 0x00069BEC
	private void CheckWhiteList()
	{
		if (this.m_permittedList.Count() == 0)
		{
			return;
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && !ZNet.PeersToDisconnectAfterKick.ContainsKey(znetPeer))
			{
				string hostName = znetPeer.m_socket.GetHostName();
				if (!this.ListContainsId(this.m_permittedList, hostName))
				{
					ZLog.Log("Kicking player not in permitted list " + znetPeer.m_playerName + " host: " + hostName);
					this.InternalKick(znetPeer);
				}
			}
		}
	}

	// Token: 0x06000DC2 RID: 3522 RVA: 0x0006BA98 File Offset: 0x00069C98
	public bool IsSaving()
	{
		return this.m_saveThread != null;
	}

	// Token: 0x06000DC3 RID: 3523 RVA: 0x0006BAA4 File Offset: 0x00069CA4
	public void SaveWorldAndPlayerProfiles()
	{
		if (this.IsServer())
		{
			this.RPC_Save(null);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Save", Array.Empty<object>());
		}
	}

	// Token: 0x06000DC4 RID: 3524 RVA: 0x0006BADC File Offset: 0x00069CDC
	private void RPC_Save(ZRpc rpc)
	{
		if (!base.enabled)
		{
			return;
		}
		if (rpc != null && !this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		bool flag;
		if (!this.EnoughDiskSpaceAvailable(out flag, false, null))
		{
			return;
		}
		this.RemotePrint(rpc, "Saving..");
		Game.instance.SavePlayerProfile(true);
		this.Save(false, true, !this.IsDedicated());
	}

	// Token: 0x06000DC5 RID: 3525 RVA: 0x0006BB50 File Offset: 0x00069D50
	private bool ListContainsId(SyncedList list, string idString)
	{
		PlatformUserID platformUserID;
		if (!PlatformUserID.TryParse(idString, out platformUserID))
		{
			platformUserID = new PlatformUserID(this.m_steamPlatform, idString);
		}
		if (platformUserID.m_platform == this.m_steamPlatform)
		{
			return list.Contains(platformUserID.ToString()) || list.Contains(platformUserID.m_userID.ToString());
		}
		return list.Contains(platformUserID.ToString());
	}

	// Token: 0x06000DC6 RID: 3526 RVA: 0x0006BBC4 File Offset: 0x00069DC4
	public void Save(bool sync, bool saveOtherPlayerProfiles = false, bool waitForNextFrame = false)
	{
		Game.instance.m_saveTimer = 0f;
		if (ZNet.m_loadError || ZoneSystem.instance.SkipSaving() || DungeonDB.instance.SkipSaving())
		{
			ZLog.LogWarning("Skipping world save");
			return;
		}
		if (!ZNet.m_isServer || ZNet.m_world == null)
		{
			return;
		}
		if (saveOtherPlayerProfiles)
		{
			this.SaveOtherPlayerProfiles();
		}
		if (!waitForNextFrame)
		{
			this.SaveWorld(sync);
			return;
		}
		base.StartCoroutine(this.DelayedSave(sync));
	}

	// Token: 0x06000DC7 RID: 3527 RVA: 0x0006BC3B File Offset: 0x00069E3B
	private IEnumerator DelayedSave(bool sync)
	{
		yield return null;
		this.SaveWorld(sync);
		yield break;
	}

	// Token: 0x06000DC8 RID: 3528 RVA: 0x0006BC54 File Offset: 0x00069E54
	public bool EnoughDiskSpaceAvailable(out bool exitGamePopupShown, bool exitGamePrompt = false, Action<bool> onDecisionMade = null)
	{
		exitGamePopupShown = false;
		string worldSavePath = "";
		World worldIfIsHost = ZNet.GetWorldIfIsHost();
		FileHelpers.FileSource worldFileSource = FileHelpers.FileSource.Cloud;
		if (worldIfIsHost != null)
		{
			worldSavePath = worldIfIsHost.GetDBPath();
			worldFileSource = worldIfIsHost.m_fileSource;
		}
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		ulong num;
		ulong num2;
		ulong num3;
		FileHelpers.CheckDiskSpace(worldSavePath, playerProfile.GetPath(), worldFileSource, playerProfile.m_fileSource, out num, out num2, out num3);
		if (num <= num3 || num <= num2)
		{
			this.LowDiskLeftInformer(num, num2, num3, exitGamePrompt, onDecisionMade);
		}
		if (num <= num3)
		{
			if (exitGamePrompt)
			{
				exitGamePopupShown = true;
			}
			ZLog.LogWarning("Not enough space left to save. ");
			return false;
		}
		return true;
	}

	// Token: 0x06000DC9 RID: 3529 RVA: 0x0006BCE4 File Offset: 0x00069EE4
	private void LowDiskLeftInformer(ulong availableFreeSpace, ulong byteLimitWarning, ulong byteLimitBlock, bool exitGamePrompt, Action<bool> onDecisionMade)
	{
		if (availableFreeSpace <= byteLimitBlock)
		{
			if (this.IsDedicated())
			{
				MessageHud.instance.MessageAll(MessageHud.MessageType.Center, "$msg_worldsaveblockedonserver");
			}
			else if (exitGamePrompt)
			{
				string text = "$menu_lowdisk_block_exitanyway_prompt";
				UnifiedPopup.Push(new YesNoPopup("$menu_lowdisk_block_exitanyway_header", text, delegate()
				{
					Action<bool> onDecisionMade2 = onDecisionMade;
					if (onDecisionMade2 != null)
					{
						onDecisionMade2(true);
					}
					UnifiedPopup.Pop();
				}, delegate()
				{
					Action<bool> onDecisionMade2 = onDecisionMade;
					if (onDecisionMade2 != null)
					{
						onDecisionMade2(false);
					}
					UnifiedPopup.Pop();
				}, true));
			}
			else
			{
				this.SavingBlockedPopup();
			}
		}
		else if (this.IsDedicated())
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, "$msg_worldsavewarningonserver");
		}
		else
		{
			this.SaveLowDiskWarningPopup();
		}
		ZLog.LogWarning(string.Format("Running low on disk space... Available space: {0} bytes.", availableFreeSpace));
	}

	// Token: 0x06000DCA RID: 3530 RVA: 0x0006BD90 File Offset: 0x00069F90
	private void SavingBlockedPopup()
	{
		string text = "$menu_lowdisk_message_block";
		UnifiedPopup.Push(new WarningPopup("$menu_lowdisk_header_block", text, delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x06000DCB RID: 3531 RVA: 0x0006BDD4 File Offset: 0x00069FD4
	private void SaveLowDiskWarningPopup()
	{
		string text = "$menu_lowdisk_message_warn";
		UnifiedPopup.Push(new WarningPopup("$menu_lowdisk_header_warn", text, delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x06000DCC RID: 3532 RVA: 0x0006BE17 File Offset: 0x0006A017
	public bool LocalPlayerIsAdminOrHost()
	{
		return this.IsServer() || this.PlayerIsAdmin(UserInfo.GetLocalUser().UserId);
	}

	// Token: 0x06000DCD RID: 3533 RVA: 0x0006BE34 File Offset: 0x0006A034
	public bool PlayerIsAdmin(PlatformUserID networkUserId)
	{
		List<string> adminList = this.GetAdminList();
		return networkUserId.IsValid && adminList != null && adminList.Contains(networkUserId.ToString());
	}

	// Token: 0x06000DCE RID: 3534 RVA: 0x0006BE6C File Offset: 0x0006A06C
	public static World GetWorldIfIsHost()
	{
		Debug.Log(string.Format("Am I Host? {0}", ZNet.m_isServer));
		if (ZNet.m_isServer)
		{
			return ZNet.m_world;
		}
		return null;
	}

	// Token: 0x06000DCF RID: 3535 RVA: 0x0006BE98 File Offset: 0x0006A098
	private void SendPeriodicData(float dt)
	{
		this.m_periodicSendTimer += dt;
		if (this.m_periodicSendTimer >= 2f)
		{
			this.m_periodicSendTimer = 0f;
			if (this.IsServer())
			{
				this.SendNetTime();
				this.SendPlayerList();
				return;
			}
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					this.SendServerSyncPlayerData(znetPeer);
				}
			}
		}
	}

	// Token: 0x06000DD0 RID: 3536 RVA: 0x0006BF30 File Offset: 0x0006A130
	private void SendServerSyncPlayerData(ZNetPeer peer)
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(this.m_referencePosition);
		zpackage.Write(this.m_publicReferencePosition);
		zpackage.Write(this.m_serverSyncedPlayerData.Count);
		foreach (KeyValuePair<string, string> keyValuePair in this.m_serverSyncedPlayerData)
		{
			zpackage.Write(keyValuePair.Key);
			zpackage.Write(keyValuePair.Value);
		}
		peer.m_rpc.Invoke("ServerSyncedPlayerData", new object[]
		{
			zpackage
		});
	}

	// Token: 0x06000DD1 RID: 3537 RVA: 0x0006BFE0 File Offset: 0x0006A1E0
	private void SendNetTime()
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady())
			{
				znetPeer.m_rpc.Invoke("NetTime", new object[]
				{
					this.m_netTime
				});
			}
		}
	}

	// Token: 0x06000DD2 RID: 3538 RVA: 0x0006C058 File Offset: 0x0006A258
	private void RPC_NetTime(ZRpc rpc, double time)
	{
		this.m_netTime = time;
	}

	// Token: 0x06000DD3 RID: 3539 RVA: 0x0006C064 File Offset: 0x0006A264
	private void RPC_ServerSyncedPlayerData(ZRpc rpc, ZPackage data)
	{
		RandEventSystem.SetRandomEventsNeedsRefresh();
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer != null)
		{
			peer.m_refPos = data.ReadVector3();
			peer.m_publicRefPos = data.ReadBool();
			peer.m_serverSyncedPlayerData.Clear();
			int num = data.ReadInt();
			for (int i = 0; i < num; i++)
			{
				peer.m_serverSyncedPlayerData.Add(data.ReadString(), data.ReadString());
			}
		}
	}

	// Token: 0x06000DD4 RID: 3540 RVA: 0x0006C0D0 File Offset: 0x0006A2D0
	private void UpdatePeers(float dt)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (!znetPeer.m_rpc.IsConnected())
			{
				if (znetPeer.m_server)
				{
					if (ZNet.m_externalError != ZNet.ConnectionStatus.None)
					{
						ZNet.m_connectionStatus = ZNet.m_externalError;
					}
					else if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connecting)
					{
						ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorConnectFailed;
					}
					else
					{
						ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorDisconnected;
					}
				}
				this.Disconnect(znetPeer);
				break;
			}
		}
		this.m_peersCopy.Clear();
		this.m_peersCopy.AddRange(this.m_peers);
		using (List<ZNetPeer>.Enumerator enumerator = this.m_peersCopy.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_rpc.Update(dt) == ZRpc.ErrorCode.IncompatibleVersion)
				{
					ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorVersion;
				}
			}
		}
	}

	// Token: 0x06000DD5 RID: 3541 RVA: 0x0006C1D0 File Offset: 0x0006A3D0
	private void CheckForIncommingServerConnections()
	{
		if (this.m_hostSocket == null)
		{
			return;
		}
		ISocket socket = this.m_hostSocket.Accept();
		if (socket != null)
		{
			if (!socket.IsConnected())
			{
				socket.Dispose();
				return;
			}
			ZNetPeer peer = new ZNetPeer(socket, false);
			this.OnNewConnection(peer);
		}
	}

	// Token: 0x06000DD6 RID: 3542 RVA: 0x0006C214 File Offset: 0x0006A414
	public ZNetPeer GetPeerByPlayerName(string name)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && znetPeer.m_playerName == name)
			{
				return znetPeer;
			}
		}
		return null;
	}

	// Token: 0x06000DD7 RID: 3543 RVA: 0x0006C280 File Offset: 0x0006A480
	public ZNetPeer GetPeerByHostName(string endpoint)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && znetPeer.m_socket.GetHostName() == endpoint)
			{
				return znetPeer;
			}
		}
		return null;
	}

	// Token: 0x06000DD8 RID: 3544 RVA: 0x0006C2F0 File Offset: 0x0006A4F0
	public ZNetPeer GetPeer(long uid)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.m_uid == uid)
			{
				return znetPeer;
			}
		}
		return null;
	}

	// Token: 0x06000DD9 RID: 3545 RVA: 0x0006C34C File Offset: 0x0006A54C
	private ZNetPeer GetPeer(ZRpc rpc)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.m_rpc == rpc)
			{
				return znetPeer;
			}
		}
		return null;
	}

	// Token: 0x06000DDA RID: 3546 RVA: 0x0006C3A8 File Offset: 0x0006A5A8
	public List<ZNetPeer> GetConnectedPeers()
	{
		return new List<ZNetPeer>(this.m_peers);
	}

	// Token: 0x06000DDB RID: 3547 RVA: 0x0006C3B8 File Offset: 0x0006A5B8
	private void SaveWorld(bool sync)
	{
		Action worldSaveStarted = ZNet.WorldSaveStarted;
		if (worldSaveStarted != null)
		{
			worldSaveStarted();
		}
		if (this.m_saveThread != null && this.m_saveThread.IsAlive)
		{
			this.m_saveThread.Join();
			this.m_saveThread = null;
		}
		this.m_saveStartTime = Time.realtimeSinceStartup;
		this.m_zdoMan.PrepareSave();
		ZoneSystem.instance.PrepareSave();
		RandEventSystem.instance.PrepareSave();
		ZNet.m_backupCount = PlatformPrefs.GetInt("AutoBackups", ZNet.m_backupCount);
		this.m_saveThreadStartTime = Time.realtimeSinceStartup;
		this.m_saveThread = new Thread(new ThreadStart(this.SaveWorldThread));
		this.m_saveThread.Start();
		if (sync)
		{
			this.m_saveThread.Join();
			this.m_saveThread = null;
			this.m_sendSaveMessage = 0.5f;
		}
	}

	// Token: 0x06000DDC RID: 3548 RVA: 0x0006C488 File Offset: 0x0006A688
	private void UpdateSave()
	{
		if (this.m_sendSaveMessage > 0f)
		{
			this.m_sendSaveMessage -= Time.fixedDeltaTime;
			if (this.m_sendSaveMessage < 0f)
			{
				this.PrintWorldSaveMessage();
				this.m_sendSaveMessage = 0f;
			}
		}
		if (this.m_saveThread != null && !this.m_saveThread.IsAlive)
		{
			this.m_saveThread = null;
			this.m_sendSaveMessage = 0.5f;
		}
	}

	// Token: 0x06000DDD RID: 3549 RVA: 0x0006C4FC File Offset: 0x0006A6FC
	private void PrintWorldSaveMessage()
	{
		float num = this.m_saveThreadStartTime - this.m_saveStartTime;
		float num2 = Time.realtimeSinceStartup - this.m_saveThreadStartTime;
		if (this.m_saveExceededCloudQuota)
		{
			this.m_saveExceededCloudQuota = false;
			MessageHud.instance.MessageAll(MessageHud.MessageType.TopLeft, string.Concat(new string[]
			{
				"$msg_worldsavedcloudstoragefull ( ",
				num.ToString("0.00"),
				"+",
				num2.ToString("0.00"),
				"s )"
			}));
		}
		else
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.TopLeft, string.Concat(new string[]
			{
				"$msg_worldsaved ( ",
				num.ToString("0.00"),
				"+",
				num2.ToString("0.00"),
				"s )"
			}));
		}
		Action worldSaveFinished = ZNet.WorldSaveFinished;
		if (worldSaveFinished == null)
		{
			return;
		}
		worldSaveFinished();
	}

	// Token: 0x06000DDE RID: 3550 RVA: 0x0006C5DC File Offset: 0x0006A7DC
	private void SaveWorldThread()
	{
		DateTime now = DateTime.Now;
		try
		{
			ulong num = 52428800UL;
			num += FileHelpers.GetFileSize(ZNet.m_world.GetMetaPath(), ZNet.m_world.m_fileSource);
			if (FileHelpers.Exists(ZNet.m_world.GetDBPath(), ZNet.m_world.m_fileSource))
			{
				num += FileHelpers.GetFileSize(ZNet.m_world.GetDBPath(), ZNet.m_world.m_fileSource);
			}
			bool flag = SaveSystem.CheckMove(ZNet.m_world.m_fileName, SaveDataType.World, ref ZNet.m_world.m_fileSource, now, num, false);
			bool flag2 = ZNet.m_world.m_createBackupBeforeSaving && !flag;
			if (FileHelpers.CloudStorageEnabled && ZNet.m_world.m_fileSource == FileHelpers.FileSource.Cloud)
			{
				num *= (flag2 ? 3UL : 2UL);
				if (FileHelpers.OperationExceedsCloudCapacity(num))
				{
					if (!FileHelpers.LocalStorageSupported)
					{
						throw new Exception("The world save operation may exceed the cloud save quota and was therefore not performed!");
					}
					string metaPath = ZNet.m_world.GetMetaPath();
					string dbpath = ZNet.m_world.GetDBPath();
					ZNet.m_world.m_fileSource = FileHelpers.FileSource.Local;
					string metaPath2 = ZNet.m_world.GetMetaPath();
					string dbpath2 = ZNet.m_world.GetDBPath();
					FileHelpers.FileCopyOutFromCloud(metaPath, metaPath2, true);
					if (FileHelpers.FileExistsCloud(dbpath))
					{
						FileHelpers.FileCopyOutFromCloud(dbpath, dbpath2, true);
					}
					SaveSystem.InvalidateCache();
					ZLog.LogWarning("The world save operation may exceed the cloud save quota and it has therefore been moved to local storage!");
					this.m_saveExceededCloudQuota = true;
				}
			}
			if (flag2)
			{
				SaveWithBackups saveWithBackups;
				if (SaveSystem.TryGetSaveByName(ZNet.m_world.m_fileName, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted)
				{
					if (SaveSystem.CreateBackup(saveWithBackups.PrimaryFile, DateTime.Now, ZNet.m_world.m_fileSource))
					{
						ZLog.Log("Migrating world save from an old save format, created backup!");
					}
					else
					{
						ZLog.LogError("Failed to create backup of world save " + ZNet.m_world.m_fileName + "!");
					}
				}
				else
				{
					ZLog.LogError("Failed to get world save " + ZNet.m_world.m_fileName + " from save system, so a backup couldn't be created!");
				}
			}
			ZNet.m_world.m_createBackupBeforeSaving = false;
			DateTime now2 = DateTime.Now;
			bool flag3 = ZNet.m_world.m_fileSource != FileHelpers.FileSource.Cloud;
			string dbpath3 = ZNet.m_world.GetDBPath();
			string text = flag3 ? (dbpath3 + ".new") : dbpath3;
			string oldFile = dbpath3 + ".old";
			ZLog.Log("World save writing starting");
			FileWriter fileWriter = new FileWriter(text, FileHelpers.FileHelperType.Binary, ZNet.m_world.m_fileSource);
			ZLog.Log("World save writing started");
			BinaryWriter binary = fileWriter.m_binary;
			binary.Write(35);
			binary.Write(this.m_netTime);
			this.m_zdoMan.SaveAsync(binary);
			ZoneSystem.instance.SaveASync(binary);
			RandEventSystem.instance.SaveAsync(binary);
			ZLog.Log("World save writing finishing");
			fileWriter.Finish();
			SaveSystem.InvalidateCache();
			ZLog.Log("World save writing finished");
			ZNet.m_world.m_needsDB = true;
			bool flag4;
			FileWriter fileWriter2;
			ZNet.m_world.SaveWorldMetaData(now, false, out flag4, out fileWriter2);
			if (ZNet.m_world.m_fileSource == FileHelpers.FileSource.Cloud && (fileWriter2.Status == FileWriter.WriterStatus.CloseFailed || fileWriter.Status == FileWriter.WriterStatus.CloseFailed))
			{
				string text2 = ZNet.<SaveWorldThread>g__GetBackupPath|79_0(ZNet.m_world.GetMetaPath(FileHelpers.FileSource.Local), now);
				string text3 = ZNet.<SaveWorldThread>g__GetBackupPath|79_0(ZNet.m_world.GetDBPath(FileHelpers.FileSource.Local), now);
				fileWriter2.DumpCloudWriteToLocalFile(text2);
				fileWriter.DumpCloudWriteToLocalFile(text3);
				SaveSystem.InvalidateCache();
				string text4 = "";
				if (fileWriter2.Status == FileWriter.WriterStatus.CloseFailed)
				{
					text4 = text4 + "Cloud save to location \"" + ZNet.m_world.GetMetaPath() + "\" failed!\n";
				}
				if (fileWriter.Status == FileWriter.WriterStatus.CloseFailed)
				{
					text4 = text4 + "Cloud save to location \"" + dbpath3 + "\" failed!\n ";
				}
				text4 = string.Concat(new string[]
				{
					text4,
					"Saved world as local backup \"",
					text2,
					"\" and \"",
					text3,
					"\". Use the \"Manage saves\" menu to restore this backup."
				});
				ZLog.LogError(text4);
			}
			else
			{
				if (flag3)
				{
					FileHelpers.ReplaceOldFile(dbpath3, text, oldFile, ZNet.m_world.m_fileSource);
					SaveSystem.InvalidateCache();
				}
				ZLog.Log("World saved ( " + (DateTime.Now - now2).TotalMilliseconds.ToString() + "ms )");
				now2 = DateTime.Now;
				if (ZNet.ConsiderAutoBackup(ZNet.m_world.m_fileName, SaveDataType.World, now))
				{
					ZLog.Log("World auto backup saved ( " + (DateTime.Now - now2).ToString() + "ms )");
				}
			}
		}
		catch (Exception ex)
		{
			ZLog.LogError("Error saving world! " + ex.Message);
			Terminal.m_threadSafeMessages.Enqueue("Error saving world! See log or console.");
			Terminal.m_threadSafeConsoleLog.Enqueue("Error saving world! " + ex.Message);
		}
	}

	// Token: 0x06000DDF RID: 3551 RVA: 0x0006CA98 File Offset: 0x0006AC98
	public static bool ConsiderAutoBackup(string saveName, SaveDataType dataType, DateTime now)
	{
		int num = 1200;
		int num2 = (ZNet.m_backupCount == 1) ? 0 : ZNet.m_backupCount;
		string s;
		int num3;
		string s2;
		int num4;
		string s3;
		int num5;
		return num2 > 0 && SaveSystem.ConsiderBackup(saveName, dataType, now, num2, (Terminal.m_testList.TryGetValue("autoshort", out s) && int.TryParse(s, out num3)) ? num3 : ZNet.m_backupShort, (Terminal.m_testList.TryGetValue("autolong", out s2) && int.TryParse(s2, out num4)) ? num4 : ZNet.m_backupLong, (Terminal.m_testList.TryGetValue("autowait", out s3) && int.TryParse(s3, out num5)) ? num5 : num, ZoneSystem.instance ? ZoneSystem.instance.TimeSinceStart() : 0f);
	}

	// Token: 0x06000DE0 RID: 3552 RVA: 0x0006CB5C File Offset: 0x0006AD5C
	private void LoadWorld()
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Load world: ",
			ZNet.m_world.m_name,
			" (",
			ZNet.m_world.m_fileName,
			")"
		}));
		string dbpath = ZNet.m_world.GetDBPath();
		FileReader fileReader;
		try
		{
			fileReader = new FileReader(dbpath, ZNet.m_world.m_fileSource, FileHelpers.FileHelperType.Binary);
		}
		catch
		{
			ZLog.Log("  missing " + dbpath);
			this.WorldSetup();
			return;
		}
		BinaryReader binary = fileReader.m_binary;
		try
		{
			int num;
			if (!this.CheckDataVersion(binary, out num))
			{
				ZLog.Log("  incompatible data version " + num.ToString());
				ZNet.m_loadError = true;
				binary.Close();
				fileReader.Dispose();
				this.WorldSetup();
				return;
			}
			if (num >= 4)
			{
				this.m_netTime = binary.ReadDouble();
			}
			this.m_zdoMan.Load(binary, num);
			if (num >= 12)
			{
				ZoneSystem.instance.Load(binary, num);
			}
			if (num >= 15)
			{
				RandEventSystem.instance.Load(binary, num);
			}
			fileReader.Dispose();
			this.WorldSetup();
		}
		catch (Exception ex)
		{
			ZLog.LogError("Exception while loading world " + dbpath + ":" + ex.ToString());
			ZNet.m_loadError = true;
		}
		Game.instance.CollectResources(false);
	}

	// Token: 0x06000DE1 RID: 3553 RVA: 0x0006CCC4 File Offset: 0x0006AEC4
	private bool CheckDataVersion(BinaryReader reader, out int version)
	{
		version = reader.ReadInt32();
		return global::Version.IsWorldVersionCompatible(version);
	}

	// Token: 0x06000DE2 RID: 3554 RVA: 0x0006CCDA File Offset: 0x0006AEDA
	private void WorldSetup()
	{
		ZoneSystem.instance.SetStartingGlobalKeys(true);
		ZNet.m_world.m_startingKeysChanged = false;
	}

	// Token: 0x06000DE3 RID: 3555 RVA: 0x0006CCF2 File Offset: 0x0006AEF2
	public int GetHostPort()
	{
		if (this.m_hostSocket != null)
		{
			return this.m_hostSocket.GetHostPort();
		}
		return 0;
	}

	// Token: 0x06000DE4 RID: 3556 RVA: 0x0006CD09 File Offset: 0x0006AF09
	public static long GetUID()
	{
		return ZDOMan.GetSessionID();
	}

	// Token: 0x06000DE5 RID: 3557 RVA: 0x0006CD10 File Offset: 0x0006AF10
	public long GetWorldUID()
	{
		return ZNet.m_world.m_uid;
	}

	// Token: 0x06000DE6 RID: 3558 RVA: 0x0006CD1C File Offset: 0x0006AF1C
	public string GetWorldName()
	{
		if (ZNet.m_world != null)
		{
			return ZNet.m_world.m_name;
		}
		return null;
	}

	// Token: 0x06000DE7 RID: 3559 RVA: 0x0006CD31 File Offset: 0x0006AF31
	public void SetCharacterID(ZDOID id)
	{
		this.m_characterID = id;
		if (!ZNet.m_isServer)
		{
			this.m_peers[0].m_rpc.Invoke("CharacterID", new object[]
			{
				id
			});
		}
	}

	// Token: 0x06000DE8 RID: 3560 RVA: 0x0006CD6C File Offset: 0x0006AF6C
	private void RPC_CharacterID(ZRpc rpc, ZDOID characterID)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer != null)
		{
			peer.m_characterID = characterID;
			string str = "Got character ZDOID from ";
			string playerName = peer.m_playerName;
			string str2 = " : ";
			ZDOID zdoid = characterID;
			ZLog.Log(str + playerName + str2 + zdoid.ToString());
		}
	}

	// Token: 0x06000DE9 RID: 3561 RVA: 0x0006CDB4 File Offset: 0x0006AFB4
	public void SetPublicReferencePosition(bool pub)
	{
		this.m_publicReferencePosition = pub;
	}

	// Token: 0x06000DEA RID: 3562 RVA: 0x0006CDBD File Offset: 0x0006AFBD
	public bool IsReferencePositionPublic()
	{
		return this.m_publicReferencePosition;
	}

	// Token: 0x06000DEB RID: 3563 RVA: 0x0006CDC5 File Offset: 0x0006AFC5
	public void SetReferencePosition(Vector3 pos)
	{
		this.m_referencePosition = pos;
	}

	// Token: 0x06000DEC RID: 3564 RVA: 0x0006CDCE File Offset: 0x0006AFCE
	public Vector3 GetReferencePosition()
	{
		return this.m_referencePosition;
	}

	// Token: 0x06000DED RID: 3565 RVA: 0x0006CDD8 File Offset: 0x0006AFD8
	public List<ZDO> GetAllCharacterZDOS()
	{
		List<ZDO> list = new List<ZDO>();
		ZDO zdo = this.m_zdoMan.GetZDO(this.m_characterID);
		if (zdo != null)
		{
			list.Add(zdo);
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && !znetPeer.m_characterID.IsNone())
			{
				ZDO zdo2 = this.m_zdoMan.GetZDO(znetPeer.m_characterID);
				if (zdo2 != null)
				{
					list.Add(zdo2);
				}
			}
		}
		return list;
	}

	// Token: 0x06000DEE RID: 3566 RVA: 0x0006CE7C File Offset: 0x0006B07C
	public int GetPeerConnections()
	{
		int num = 0;
		for (int i = 0; i < this.m_peers.Count; i++)
		{
			if (this.m_peers[i].IsReady())
			{
				num++;
			}
		}
		return num;
	}

	// Token: 0x06000DEF RID: 3567 RVA: 0x0006CEB9 File Offset: 0x0006B0B9
	public ZNat GetZNat()
	{
		return this.m_nat;
	}

	// Token: 0x06000DF0 RID: 3568 RVA: 0x0006CEC4 File Offset: 0x0006B0C4
	public static void SetServer(bool server, bool openServer, bool publicServer, string serverName, string password, World world)
	{
		ZNet.m_isServer = server;
		ZNet.m_openServer = openServer;
		ZNet.m_publicServer = publicServer;
		ZNet.m_serverPassword = (string.IsNullOrEmpty(password) ? "" : ZNet.HashPassword(password, ZNet.ServerPasswordSalt()));
		ZNet.m_ServerName = serverName;
		ZNet.m_world = world;
	}

	// Token: 0x06000DF1 RID: 3569 RVA: 0x0006CF14 File Offset: 0x0006B114
	private static string HashPassword(string password, string salt)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(password + salt);
		byte[] bytes2 = new MD5CryptoServiceProvider().ComputeHash(bytes);
		return Encoding.ASCII.GetString(bytes2);
	}

	// Token: 0x06000DF2 RID: 3570 RVA: 0x0006CF4A File Offset: 0x0006B14A
	public static void ResetServerHost()
	{
		ZNet.m_serverPlayFabPlayerId = null;
		ZNet.m_serverSteamID = 0UL;
		ZNet.m_serverHost = "";
		ZNet.m_serverHostPort = 0;
	}

	// Token: 0x06000DF3 RID: 3571 RVA: 0x0006CF69 File Offset: 0x0006B169
	public static bool HasServerHost()
	{
		return ZNet.m_serverHost != "" || ZNet.m_serverPlayFabPlayerId != null || ZNet.m_serverSteamID > 0UL;
	}

	// Token: 0x06000DF4 RID: 3572 RVA: 0x0006CF94 File Offset: 0x0006B194
	public static void SetServerHost(string remotePlayerId)
	{
		ZNet.ResetServerHost();
		ZNet.m_serverPlayFabPlayerId = remotePlayerId;
		ZNet.m_onlineBackend = OnlineBackendType.PlayFab;
	}

	// Token: 0x06000DF5 RID: 3573 RVA: 0x0006CFA7 File Offset: 0x0006B1A7
	public static void SetServerHost(ulong serverID)
	{
		ZNet.ResetServerHost();
		ZNet.m_serverSteamID = serverID;
		ZNet.m_onlineBackend = OnlineBackendType.Steamworks;
	}

	// Token: 0x06000DF6 RID: 3574 RVA: 0x0006CFBA File Offset: 0x0006B1BA
	public static void SetServerHost(string host, int port, OnlineBackendType backend)
	{
		ZNet.ResetServerHost();
		ZNet.m_serverHost = host;
		ZNet.m_serverHostPort = port;
		ZNet.m_onlineBackend = backend;
	}

	// Token: 0x06000DF7 RID: 3575 RVA: 0x0006CFD4 File Offset: 0x0006B1D4
	public static string GetServerString(bool includeBackend = true)
	{
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			return (includeBackend ? "playfab/" : "") + ZNet.m_serverPlayFabPlayerId;
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
		{
			return string.Concat(new string[]
			{
				includeBackend ? "steam/" : "",
				ZNet.m_serverSteamID.ToString(),
				"/",
				ZNet.m_serverHost,
				":",
				ZNet.m_serverHostPort.ToString()
			});
		}
		return (includeBackend ? "socket/" : "") + ZNet.m_serverHost + ":" + ZNet.m_serverHostPort.ToString();
	}

	// Token: 0x06000DF8 RID: 3576 RVA: 0x0006D082 File Offset: 0x0006B282
	public bool IsServer()
	{
		return ZNet.m_isServer;
	}

	// Token: 0x06000DF9 RID: 3577 RVA: 0x0006D089 File Offset: 0x0006B289
	public static bool IsOpenServer()
	{
		return ZNet.m_openServer;
	}

	// Token: 0x06000DFA RID: 3578 RVA: 0x0006D090 File Offset: 0x0006B290
	public bool IsDedicated()
	{
		return false;
	}

	// Token: 0x06000DFB RID: 3579 RVA: 0x0006D093 File Offset: 0x0006B293
	public static bool IsPasswordDialogShowing()
	{
		return !(ZNet.m_instance == null) && ZNet.m_instance.m_passwordDialog.gameObject.activeInHierarchy;
	}

	// Token: 0x17000078 RID: 120
	// (get) Token: 0x06000DFC RID: 3580 RVA: 0x0006D0B8 File Offset: 0x0006B2B8
	public static bool IsSinglePlayer
	{
		get
		{
			return ZNet.m_isServer && !ZNet.m_openServer;
		}
	}

	// Token: 0x06000DFD RID: 3581 RVA: 0x0006D0CC File Offset: 0x0006B2CC
	public static bool TryGetServerAssignedDisplayName(PlatformUserID userId, out string displayName)
	{
		if (ZNet.instance == null)
		{
			displayName = null;
			return false;
		}
		for (int i = 0; i < ZNet.instance.m_players.Count; i++)
		{
			if (ZNet.instance.m_players[i].m_userInfo.m_id == userId && ZNet.instance.m_players[i].m_serverAssignedDisplayName != null)
			{
				displayName = ZNet.instance.m_players[i].m_serverAssignedDisplayName;
				return true;
			}
		}
		displayName = null;
		return false;
	}

	// Token: 0x06000DFE RID: 3582 RVA: 0x0006D15C File Offset: 0x0006B35C
	private string GetUniqueDisplayName(ZNet.CrossNetworkUserInfo userInfo)
	{
		bool flag = false;
		int num = 1;
		int num2 = 0;
		for (int i = 0; i < this.m_playerHistory.Count; i++)
		{
			if (!(this.m_playerHistory[i].m_displayName != userInfo.m_displayName))
			{
				num2++;
				if (!flag)
				{
					if (this.m_playerHistory[i].m_id == userInfo.m_id)
					{
						flag = true;
					}
					else
					{
						num++;
					}
				}
			}
		}
		if (!flag)
		{
			ZLog.LogError(string.Format("Couldn't find matching ID to user {0} in player history!", userInfo));
		}
		if (num2 > 1)
		{
			return string.Format("{0}#{1}", userInfo.m_displayName, num);
		}
		return userInfo.m_displayName;
	}

	// Token: 0x06000DFF RID: 3583 RVA: 0x0006D20C File Offset: 0x0006B40C
	private void UpdatePlayerList()
	{
		this.m_players.Clear();
		if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
		{
			ZNet.PlayerInfo playerInfo = default(ZNet.PlayerInfo);
			playerInfo.m_name = Game.instance.GetPlayerProfile().GetName();
			playerInfo.m_userInfo.m_id = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
			playerInfo.m_userInfo.m_displayName = PlatformManager.DistributionPlatform.LocalUser.DisplayName;
			playerInfo.m_characterID = this.m_characterID;
			playerInfo.m_publicPosition = this.m_publicReferencePosition;
			if (playerInfo.m_publicPosition)
			{
				playerInfo.m_position = this.m_referencePosition;
			}
			this.m_players.Add(playerInfo);
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady())
			{
				ZNet.PlayerInfo playerInfo2 = default(ZNet.PlayerInfo);
				playerInfo2.m_name = znetPeer.m_playerName;
				playerInfo2.m_characterID = znetPeer.m_characterID;
				if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
				{
					playerInfo2.m_userInfo.m_id = new PlatformUserID(this.m_steamPlatform, znetPeer.m_socket.GetHostName());
				}
				else
				{
					playerInfo2.m_userInfo.m_id = new PlatformUserID(znetPeer.m_socket.GetHostName());
				}
				playerInfo2.m_userInfo.m_displayName = (znetPeer.m_serverSyncedPlayerData.ContainsKey("platformDisplayName") ? znetPeer.m_serverSyncedPlayerData["platformDisplayName"] : "");
				playerInfo2.m_publicPosition = znetPeer.m_publicRefPos;
				if (playerInfo2.m_publicPosition)
				{
					playerInfo2.m_position = znetPeer.m_refPos;
				}
				this.m_players.Add(playerInfo2);
			}
		}
		this.UpdatePlayerHistory();
		for (int i = 0; i < this.m_players.Count; i++)
		{
			ZNet.PlayerInfo playerInfo3 = this.m_players[i];
			playerInfo3.m_serverAssignedDisplayName = this.GetUniqueDisplayName(playerInfo3.m_userInfo);
			this.m_players[i] = playerInfo3;
		}
	}

	// Token: 0x06000E00 RID: 3584 RVA: 0x0006D42C File Offset: 0x0006B62C
	private void SendPlayerList()
	{
		this.UpdatePlayerList();
		if (this.m_peers.Count > 0)
		{
			ZPackage zpackage = new ZPackage();
			zpackage.Write(this.m_players.Count);
			foreach (ZNet.PlayerInfo playerInfo in this.m_players)
			{
				zpackage.Write(playerInfo.m_name);
				zpackage.Write(playerInfo.m_characterID);
				ZPackage zpackage2 = zpackage;
				PlatformUserID id = playerInfo.m_userInfo.m_id;
				zpackage2.Write(id.ToString());
				zpackage.Write(playerInfo.m_userInfo.m_displayName);
				zpackage.Write(playerInfo.m_serverAssignedDisplayName);
				zpackage.Write(playerInfo.m_publicPosition);
				if (playerInfo.m_publicPosition)
				{
					zpackage.Write(playerInfo.m_position);
				}
			}
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					znetPeer.m_rpc.Invoke("PlayerList", new object[]
					{
						zpackage
					});
				}
			}
			this.UpdatePlayerHistory();
		}
	}

	// Token: 0x06000E01 RID: 3585 RVA: 0x0006D584 File Offset: 0x0006B784
	private void SendAdminList()
	{
		if (this.m_peers.Count > 0)
		{
			ZPackage zpackage = new ZPackage();
			zpackage.Write(this.m_adminList.Count());
			foreach (string data in this.m_adminList.GetList())
			{
				zpackage.Write(data);
			}
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					znetPeer.m_rpc.Invoke("AdminList", new object[]
					{
						zpackage
					});
				}
			}
		}
	}

	// Token: 0x06000E02 RID: 3586 RVA: 0x0006D664 File Offset: 0x0006B864
	private void RPC_AdminList(ZRpc rpc, ZPackage pkg)
	{
		this.m_adminListForRpc.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string item = pkg.ReadString();
			this.m_adminListForRpc.Add(item);
		}
	}

	// Token: 0x06000E03 RID: 3587 RVA: 0x0006D6A4 File Offset: 0x0006B8A4
	private void RPC_PlayerList(ZRpc rpc, ZPackage pkg)
	{
		this.m_players.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			ZNet.PlayerInfo playerInfo = default(ZNet.PlayerInfo);
			playerInfo.m_name = pkg.ReadString();
			playerInfo.m_characterID = pkg.ReadZDOID();
			playerInfo.m_userInfo.m_id = new PlatformUserID(pkg.ReadString());
			playerInfo.m_userInfo.m_displayName = pkg.ReadString();
			playerInfo.m_serverAssignedDisplayName = pkg.ReadString();
			playerInfo.m_publicPosition = pkg.ReadBool();
			if (playerInfo.m_publicPosition)
			{
				playerInfo.m_position = pkg.ReadVector3();
			}
			this.m_players.Add(playerInfo);
		}
		this.UpdatePlayerHistory();
	}

	// Token: 0x06000E04 RID: 3588 RVA: 0x0006D764 File Offset: 0x0006B964
	private void UpdatePlayerHistory()
	{
		List<PlatformUserID> list = new List<PlatformUserID>();
		PlatformUserID platformUserID = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
		foreach (ZNet.PlayerInfo playerInfo in this.m_players)
		{
			int num = 0;
			while (num < this.m_playerHistory.Count && !(this.m_playerHistory[num].m_id == playerInfo.m_userInfo.m_id))
			{
				num++;
			}
			if (num < this.m_playerHistory.Count)
			{
				this.m_playerHistory[num] = playerInfo.m_userInfo;
			}
			else
			{
				this.m_playerHistory.Add(playerInfo.m_userInfo);
				if (!(playerInfo.m_userInfo.m_id == platformUserID))
				{
					list.Add(playerInfo.m_userInfo.m_id);
				}
			}
		}
		IMatchmakingProvider matchmakingProvider = PlatformManager.DistributionPlatform.MatchmakingProvider;
		if (matchmakingProvider == null)
		{
			return;
		}
		if (list.Count > 0)
		{
			matchmakingProvider.AddRecentPlayers(list.ToArray());
		}
	}

	// Token: 0x06000E05 RID: 3589 RVA: 0x0006D890 File Offset: 0x0006BA90
	public List<ZNet.PlayerInfo> GetPlayerList()
	{
		return this.m_players;
	}

	// Token: 0x06000E06 RID: 3590 RVA: 0x0006D898 File Offset: 0x0006BA98
	public static bool TryGetPlayerByPlatformUserID(PlatformUserID platformUserID, out ZNet.PlayerInfo playerInfo)
	{
		if (ZNet.instance == null)
		{
			playerInfo = default(ZNet.PlayerInfo);
			return false;
		}
		for (int i = 0; i < ZNet.instance.m_players.Count; i++)
		{
			if (ZNet.instance.m_players[i].m_userInfo.m_id == platformUserID)
			{
				playerInfo = ZNet.instance.m_players[i];
				return true;
			}
		}
		playerInfo = default(ZNet.PlayerInfo);
		return false;
	}

	// Token: 0x06000E07 RID: 3591 RVA: 0x0006D917 File Offset: 0x0006BB17
	public List<string> GetAdminList()
	{
		return this.m_adminListForRpc;
	}

	// Token: 0x17000079 RID: 121
	// (get) Token: 0x06000E08 RID: 3592 RVA: 0x0006D91F File Offset: 0x0006BB1F
	public ZDOID LocalPlayerCharacterID
	{
		get
		{
			return this.m_characterID;
		}
	}

	// Token: 0x06000E09 RID: 3593 RVA: 0x0006D928 File Offset: 0x0006BB28
	public void GetOtherPublicPlayers(List<ZNet.PlayerInfo> playerList)
	{
		foreach (ZNet.PlayerInfo playerInfo in this.m_players)
		{
			if (playerInfo.m_publicPosition)
			{
				ZDOID characterID = playerInfo.m_characterID;
				if (!characterID.IsNone() && !(playerInfo.m_characterID == this.m_characterID))
				{
					playerList.Add(playerInfo);
				}
			}
		}
	}

	// Token: 0x06000E0A RID: 3594 RVA: 0x0006D9A8 File Offset: 0x0006BBA8
	public int GetNrOfPlayers()
	{
		return this.m_players.Count;
	}

	// Token: 0x06000E0B RID: 3595 RVA: 0x0006D9B8 File Offset: 0x0006BBB8
	public void GetNetStats(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
		if (this.IsServer())
		{
			int num = 0;
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					num++;
					float num2;
					float num3;
					int num4;
					float num5;
					float num6;
					znetPeer.m_socket.GetConnectionQuality(out num2, out num3, out num4, out num5, out num6);
					localQuality += num2;
					remoteQuality += num3;
					ping += num4;
					outByteSec += num5;
					inByteSec += num6;
				}
			}
			if (num > 0)
			{
				localQuality /= (float)num;
				remoteQuality /= (float)num;
				ping /= num;
			}
			return;
		}
		if (ZNet.m_connectionStatus != ZNet.ConnectionStatus.Connected)
		{
			return;
		}
		foreach (ZNetPeer znetPeer2 in this.m_peers)
		{
			if (znetPeer2.IsReady())
			{
				znetPeer2.m_socket.GetConnectionQuality(out localQuality, out remoteQuality, out ping, out outByteSec, out inByteSec);
				break;
			}
		}
	}

	// Token: 0x06000E0C RID: 3596 RVA: 0x0006DAF8 File Offset: 0x0006BCF8
	public void SetNetTime(double time)
	{
		this.m_netTime = time;
	}

	// Token: 0x06000E0D RID: 3597 RVA: 0x0006DB04 File Offset: 0x0006BD04
	public DateTime GetTime()
	{
		long ticks = (long)(this.m_netTime * 1000.0 * 10000.0);
		return new DateTime(ticks);
	}

	// Token: 0x06000E0E RID: 3598 RVA: 0x0006DB33 File Offset: 0x0006BD33
	public float GetWrappedDayTimeSeconds()
	{
		return (float)(this.m_netTime % 86400.0);
	}

	// Token: 0x06000E0F RID: 3599 RVA: 0x0006DB46 File Offset: 0x0006BD46
	public double GetTimeSeconds()
	{
		return this.m_netTime;
	}

	// Token: 0x06000E10 RID: 3600 RVA: 0x0006DB4E File Offset: 0x0006BD4E
	public static ZNet.ConnectionStatus GetConnectionStatus()
	{
		if (ZNet.m_instance != null && ZNet.m_instance.IsServer())
		{
			return ZNet.ConnectionStatus.Connected;
		}
		if (ZNet.m_externalError != ZNet.ConnectionStatus.None)
		{
			ZNet.m_connectionStatus = ZNet.m_externalError;
		}
		return ZNet.m_connectionStatus;
	}

	// Token: 0x06000E11 RID: 3601 RVA: 0x0006DB81 File Offset: 0x0006BD81
	public bool HasBadConnection()
	{
		return this.GetServerPing() > this.m_badConnectionPing;
	}

	// Token: 0x06000E12 RID: 3602 RVA: 0x0006DB94 File Offset: 0x0006BD94
	public float GetServerPing()
	{
		if (this.IsServer())
		{
			return 0f;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connecting || ZNet.m_connectionStatus == ZNet.ConnectionStatus.None)
		{
			return 0f;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
		{
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					return znetPeer.m_rpc.GetTimeSinceLastPing();
				}
			}
		}
		return 0f;
	}

	// Token: 0x06000E13 RID: 3603 RVA: 0x0006DC28 File Offset: 0x0006BE28
	public ZNetPeer GetServerPeer()
	{
		if (this.IsServer())
		{
			return null;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connecting || ZNet.m_connectionStatus == ZNet.ConnectionStatus.None)
		{
			return null;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
		{
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					return znetPeer;
				}
			}
		}
		return null;
	}

	// Token: 0x06000E14 RID: 3604 RVA: 0x0006DCA8 File Offset: 0x0006BEA8
	public ZRpc GetServerRPC()
	{
		ZNetPeer serverPeer = this.GetServerPeer();
		if (serverPeer != null)
		{
			return serverPeer.m_rpc;
		}
		return null;
	}

	// Token: 0x06000E15 RID: 3605 RVA: 0x0006DCC7 File Offset: 0x0006BEC7
	public List<ZNetPeer> GetPeers()
	{
		return this.m_peers;
	}

	// Token: 0x06000E16 RID: 3606 RVA: 0x0006DCCF File Offset: 0x0006BECF
	public void RemotePrint(ZRpc rpc, string text)
	{
		if (rpc == null)
		{
			if (global::Console.instance)
			{
				global::Console.instance.Print(text);
				return;
			}
		}
		else
		{
			rpc.Invoke("RemotePrint", new object[]
			{
				text
			});
		}
	}

	// Token: 0x06000E17 RID: 3607 RVA: 0x0006DD01 File Offset: 0x0006BF01
	private void RPC_RemotePrint(ZRpc rpc, string text)
	{
		if (global::Console.instance)
		{
			global::Console.instance.Print(text);
		}
	}

	// Token: 0x06000E18 RID: 3608 RVA: 0x0006DD1C File Offset: 0x0006BF1C
	public void Kick(string user)
	{
		if (this.IsServer())
		{
			this.InternalKick(user);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Kick", new object[]
			{
				user
			});
		}
	}

	// Token: 0x06000E19 RID: 3609 RVA: 0x0006DD58 File Offset: 0x0006BF58
	private void RPC_Kick(ZRpc rpc, string user)
	{
		if (!this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.RemotePrint(rpc, "Kicking user " + user);
		this.InternalKick(user);
	}

	// Token: 0x06000E1A RID: 3610 RVA: 0x0006DDA4 File Offset: 0x0006BFA4
	private void RPC_Kicked(ZRpc rpc)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer == null || !peer.m_server)
		{
			return;
		}
		ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorKicked;
		this.Disconnect(peer);
	}

	// Token: 0x06000E1B RID: 3611 RVA: 0x0006DDD4 File Offset: 0x0006BFD4
	private void InternalKick(string user)
	{
		if (user == "")
		{
			return;
		}
		ZNetPeer znetPeer = null;
		PlatformUserID platformUserID;
		if (!PlatformUserID.TryParse(user, out platformUserID))
		{
			platformUserID = new PlatformUserID(this.m_steamPlatform, user);
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
		{
			if (platformUserID.m_platform == this.m_steamPlatform)
			{
				znetPeer = this.GetPeerByHostName(platformUserID.m_userID);
			}
		}
		else
		{
			znetPeer = this.GetPeerByHostName(platformUserID.ToString());
		}
		if (znetPeer == null)
		{
			znetPeer = this.GetPeerByPlayerName(user);
		}
		if (znetPeer != null)
		{
			this.InternalKick(znetPeer);
		}
	}

	// Token: 0x06000E1C RID: 3612 RVA: 0x0006DE5C File Offset: 0x0006C05C
	private void InternalKick(ZNetPeer peer)
	{
		if (!this.IsServer() || peer == null || ZNet.PeersToDisconnectAfterKick.ContainsKey(peer))
		{
			return;
		}
		ZLog.Log("Kicking " + peer.m_playerName);
		peer.m_rpc.Invoke("Kicked", Array.Empty<object>());
		ZNet.PeersToDisconnectAfterKick[peer] = Time.time + 1f;
	}

	// Token: 0x06000E1D RID: 3613 RVA: 0x0006DEC4 File Offset: 0x0006C0C4
	private bool IsAllowed(string hostName, string playerName)
	{
		return !this.ListContainsId(this.m_bannedList, hostName) && !this.m_bannedList.Contains(playerName) && (this.m_permittedList.Count() <= 0 || this.ListContainsId(this.m_permittedList, hostName));
	}

	// Token: 0x06000E1E RID: 3614 RVA: 0x0006DF10 File Offset: 0x0006C110
	public void Ban(string user)
	{
		if (this.IsServer())
		{
			this.InternalBan(null, user);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Ban", new object[]
			{
				user
			});
		}
	}

	// Token: 0x06000E1F RID: 3615 RVA: 0x0006DF4D File Offset: 0x0006C14D
	private void RPC_Ban(ZRpc rpc, string user)
	{
		if (!this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalBan(rpc, user);
	}

	// Token: 0x06000E20 RID: 3616 RVA: 0x0006DF80 File Offset: 0x0006C180
	private void InternalBan(ZRpc rpc, string user)
	{
		if (!this.IsServer())
		{
			return;
		}
		if (user == "")
		{
			return;
		}
		ZNetPeer peerByPlayerName = this.GetPeerByPlayerName(user);
		if (peerByPlayerName != null)
		{
			user = peerByPlayerName.m_socket.GetHostName();
		}
		this.RemotePrint(rpc, "Banning user " + user);
		this.m_bannedList.Add(user);
	}

	// Token: 0x06000E21 RID: 3617 RVA: 0x0006DFDC File Offset: 0x0006C1DC
	public void Unban(string user)
	{
		if (this.IsServer())
		{
			this.InternalUnban(null, user);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Unban", new object[]
			{
				user
			});
		}
	}

	// Token: 0x06000E22 RID: 3618 RVA: 0x0006E019 File Offset: 0x0006C219
	private void RPC_Unban(ZRpc rpc, string user)
	{
		if (!this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalUnban(rpc, user);
	}

	// Token: 0x06000E23 RID: 3619 RVA: 0x0006E049 File Offset: 0x0006C249
	private void InternalUnban(ZRpc rpc, string user)
	{
		if (!this.IsServer())
		{
			return;
		}
		if (user == "")
		{
			return;
		}
		this.RemotePrint(rpc, "Unbanning user " + user);
		this.m_bannedList.Remove(user);
	}

	// Token: 0x06000E24 RID: 3620 RVA: 0x0006E080 File Offset: 0x0006C280
	public bool IsAdmin(string hostName)
	{
		return this.ListContainsId(this.m_adminList, hostName);
	}

	// Token: 0x1700007A RID: 122
	// (get) Token: 0x06000E25 RID: 3621 RVA: 0x0006E08F File Offset: 0x0006C28F
	public List<string> Banned
	{
		get
		{
			return this.m_bannedList.GetList();
		}
	}

	// Token: 0x06000E26 RID: 3622 RVA: 0x0006E09C File Offset: 0x0006C29C
	public void PrintBanned()
	{
		if (this.IsServer())
		{
			this.InternalPrintBanned(null);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("PrintBanned", Array.Empty<object>());
		}
	}

	// Token: 0x06000E27 RID: 3623 RVA: 0x0006E0D3 File Offset: 0x0006C2D3
	private void RPC_PrintBanned(ZRpc rpc)
	{
		if (!this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalPrintBanned(rpc);
	}

	// Token: 0x06000E28 RID: 3624 RVA: 0x0006E104 File Offset: 0x0006C304
	private void InternalPrintBanned(ZRpc rpc)
	{
		this.RemotePrint(rpc, "Banned users");
		List<string> list = this.m_bannedList.GetList();
		if (list.Count == 0)
		{
			this.RemotePrint(rpc, "-");
		}
		else
		{
			for (int i = 0; i < list.Count; i++)
			{
				this.RemotePrint(rpc, i.ToString() + ": " + list[i]);
			}
		}
		this.RemotePrint(rpc, "");
		this.RemotePrint(rpc, "Permitted users");
		List<string> list2 = this.m_permittedList.GetList();
		if (list2.Count == 0)
		{
			this.RemotePrint(rpc, "All");
			return;
		}
		for (int j = 0; j < list2.Count; j++)
		{
			this.RemotePrint(rpc, j.ToString() + ": " + list2[j]);
		}
	}

	// Token: 0x06000E29 RID: 3625 RVA: 0x0006E1D8 File Offset: 0x0006C3D8
	public void RemoteCommand(string command)
	{
		if (this.IsServer())
		{
			this.InternalCommand(null, command);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("RPC_RemoteCommand", new object[]
			{
				command
			});
		}
	}

	// Token: 0x06000E2A RID: 3626 RVA: 0x0006E215 File Offset: 0x0006C415
	private void RPC_RemoteCommand(ZRpc rpc, string command)
	{
		if (!this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalCommand(rpc, command);
	}

	// Token: 0x06000E2B RID: 3627 RVA: 0x0006E248 File Offset: 0x0006C448
	private void InternalCommand(ZRpc rpc, string command)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Remote admin '",
			rpc.GetSocket().GetHostName(),
			"' executed command '",
			command,
			"' remotely."
		}));
		global::Console.instance.TryRunCommand(command, false, false);
	}

	// Token: 0x06000E2C RID: 3628 RVA: 0x0006E29C File Offset: 0x0006C49C
	private static string ServerPasswordSalt()
	{
		if (ZNet.m_serverPasswordSalt.Length == 0)
		{
			byte[] array = new byte[16];
			RandomNumberGenerator.Create().GetBytes(array);
			ZNet.m_serverPasswordSalt = Encoding.ASCII.GetString(array);
		}
		return ZNet.m_serverPasswordSalt;
	}

	// Token: 0x06000E2D RID: 3629 RVA: 0x0006E2DD File Offset: 0x0006C4DD
	public static void SetExternalError(ZNet.ConnectionStatus error)
	{
		ZNet.m_externalError = error;
	}

	// Token: 0x1700007B RID: 123
	// (get) Token: 0x06000E2E RID: 3630 RVA: 0x0006E2E5 File Offset: 0x0006C4E5
	public bool HaveStopped
	{
		get
		{
			return this.m_haveStoped;
		}
	}

	// Token: 0x1700007C RID: 124
	// (get) Token: 0x06000E2F RID: 3631 RVA: 0x0006E2ED File Offset: 0x0006C4ED
	public static World World
	{
		get
		{
			return ZNet.m_world;
		}
	}

	// Token: 0x06000E32 RID: 3634 RVA: 0x0006E43C File Offset: 0x0006C63C
	[CompilerGenerated]
	internal static string <GetPublicIP>g__DownloadString|15_0(string downloadUrl, int timeoutMS = 5000)
	{
		Debug.Log("now checking " + downloadUrl);
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(downloadUrl);
		httpWebRequest.Timeout = timeoutMS;
		httpWebRequest.ReadWriteTimeout = timeoutMS;
		string result;
		try
		{
			result = new StreamReader(((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream()).ReadToEnd();
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while waiting for respons from " + downloadUrl + " -> " + ex.ToString());
			result = "";
		}
		return result;
	}

	// Token: 0x06000E33 RID: 3635 RVA: 0x0006E4C8 File Offset: 0x0006C6C8
	[CompilerGenerated]
	private void <OnSteamServerRegistered>g__RetryRegisterAfterDelay|16_0(float delay)
	{
		base.StartCoroutine(this.<OnSteamServerRegistered>g__DelayThenRegisterCoroutine|16_1(delay));
	}

	// Token: 0x06000E34 RID: 3636 RVA: 0x0006E4D8 File Offset: 0x0006C6D8
	[CompilerGenerated]
	private IEnumerator <OnSteamServerRegistered>g__DelayThenRegisterCoroutine|16_1(float delay)
	{
		ZLog.Log(string.Format("Steam register server failed! Retrying in {0}s, total attempts: {1}", delay, this.m_registerAttempts));
		DateTime NextRetryUtc = DateTime.UtcNow + TimeSpan.FromSeconds((double)delay);
		while (DateTime.UtcNow < NextRetryUtc)
		{
			yield return null;
		}
		bool password = ZNet.m_serverPassword != "";
		GameVersion currentVersion = global::Version.CurrentVersion;
		uint networkVersion = 34U;
		List<string> startingGlobalKeys = ZNet.m_world.m_startingGlobalKeys;
		ZSteamMatchmaking.instance.RegisterServer(ZNet.m_ServerName, password, currentVersion, startingGlobalKeys, networkVersion, ZNet.m_publicServer, ZNet.m_world.m_seedName, new ZSteamMatchmaking.ServerRegistered(this.OnSteamServerRegistered));
		yield break;
	}

	// Token: 0x06000E35 RID: 3637 RVA: 0x0006E4F0 File Offset: 0x0006C6F0
	[CompilerGenerated]
	internal static string <SaveWorldThread>g__GetBackupPath|79_0(string filePath, DateTime now)
	{
		string text;
		string text2;
		string text3;
		FileHelpers.SplitFilePath(filePath, out text, out text2, out text3);
		return string.Concat(new string[]
		{
			text,
			text2,
			"_backup_cloud-",
			now.ToString("yyyyMMdd-HHmmss"),
			text3
		});
	}

	// Token: 0x04000E2A RID: 3626
	public static Action WorldSaveStarted;

	// Token: 0x04000E2B RID: 3627
	public static Action WorldSaveFinished;

	// Token: 0x04000E2C RID: 3628
	private float m_banlistTimer;

	// Token: 0x04000E2D RID: 3629
	private static ZNet m_instance;

	// Token: 0x04000E2E RID: 3630
	public const int ServerPlayerLimit = 10;

	// Token: 0x04000E2F RID: 3631
	public int m_hostPort = 2456;

	// Token: 0x04000E30 RID: 3632
	public RectTransform m_passwordDialog;

	// Token: 0x04000E31 RID: 3633
	public RectTransform m_connectingDialog;

	// Token: 0x04000E32 RID: 3634
	public float m_badConnectionPing = 5f;

	// Token: 0x04000E33 RID: 3635
	public int m_zdoSectorsWidth = 512;

	// Token: 0x04000E34 RID: 3636
	private ZConnector2 m_serverConnector;

	// Token: 0x04000E35 RID: 3637
	private ISocket m_hostSocket;

	// Token: 0x04000E36 RID: 3638
	private readonly List<ZNetPeer> m_peers = new List<ZNetPeer>();

	// Token: 0x04000E37 RID: 3639
	private readonly List<ZNetPeer> m_peersCopy = new List<ZNetPeer>();

	// Token: 0x04000E38 RID: 3640
	private Thread m_saveThread;

	// Token: 0x04000E39 RID: 3641
	private bool m_saveExceededCloudQuota;

	// Token: 0x04000E3A RID: 3642
	private float m_saveStartTime;

	// Token: 0x04000E3B RID: 3643
	private float m_saveThreadStartTime;

	// Token: 0x04000E3C RID: 3644
	public static bool m_loadError = false;

	// Token: 0x04000E3D RID: 3645
	private float m_sendSaveMessage;

	// Token: 0x04000E3E RID: 3646
	private ZDOMan m_zdoMan;

	// Token: 0x04000E3F RID: 3647
	private ZRoutedRpc m_routedRpc;

	// Token: 0x04000E40 RID: 3648
	private ZNat m_nat;

	// Token: 0x04000E41 RID: 3649
	private double m_netTime = 2040.0;

	// Token: 0x04000E42 RID: 3650
	private ZDOID m_characterID = ZDOID.None;

	// Token: 0x04000E43 RID: 3651
	private Vector3 m_referencePosition = Vector3.zero;

	// Token: 0x04000E44 RID: 3652
	private bool m_publicReferencePosition;

	// Token: 0x04000E45 RID: 3653
	private float m_periodicSendTimer;

	// Token: 0x04000E46 RID: 3654
	public Dictionary<string, string> m_serverSyncedPlayerData = new Dictionary<string, string>();

	// Token: 0x04000E47 RID: 3655
	public static int m_backupCount = 2;

	// Token: 0x04000E48 RID: 3656
	public static int m_backupShort = 7200;

	// Token: 0x04000E49 RID: 3657
	public static int m_backupLong = 43200;

	// Token: 0x04000E4A RID: 3658
	private bool m_haveStoped;

	// Token: 0x04000E4B RID: 3659
	private static bool m_isServer = true;

	// Token: 0x04000E4C RID: 3660
	private static World m_world = null;

	// Token: 0x04000E4D RID: 3661
	private int m_registerAttempts;

	// Token: 0x04000E4E RID: 3662
	public static OnlineBackendType m_onlineBackend = OnlineBackendType.Steamworks;

	// Token: 0x04000E4F RID: 3663
	private static string m_serverPlayFabPlayerId = null;

	// Token: 0x04000E50 RID: 3664
	private static ulong m_serverSteamID = 0UL;

	// Token: 0x04000E51 RID: 3665
	private static string m_serverHost = "";

	// Token: 0x04000E52 RID: 3666
	private static int m_serverHostPort = 0;

	// Token: 0x04000E53 RID: 3667
	private static bool m_openServer = true;

	// Token: 0x04000E54 RID: 3668
	private static bool m_publicServer = true;

	// Token: 0x04000E55 RID: 3669
	private static string m_serverPassword = "";

	// Token: 0x04000E56 RID: 3670
	private static string m_serverPasswordSalt = "";

	// Token: 0x04000E57 RID: 3671
	private static string m_ServerName = "";

	// Token: 0x04000E58 RID: 3672
	private static ZNet.ConnectionStatus m_connectionStatus = ZNet.ConnectionStatus.None;

	// Token: 0x04000E59 RID: 3673
	private static ZNet.ConnectionStatus m_externalError = ZNet.ConnectionStatus.None;

	// Token: 0x04000E5A RID: 3674
	private SyncedList m_adminList;

	// Token: 0x04000E5B RID: 3675
	private SyncedList m_bannedList;

	// Token: 0x04000E5C RID: 3676
	private SyncedList m_permittedList;

	// Token: 0x04000E5D RID: 3677
	private List<ZNet.PlayerInfo> m_players = new List<ZNet.PlayerInfo>();

	// Token: 0x04000E5E RID: 3678
	private List<string> m_adminListForRpc = new List<string>();

	// Token: 0x04000E5F RID: 3679
	private ZRpc m_tempPasswordRPC;

	// Token: 0x04000E60 RID: 3680
	private List<ZNet.CrossNetworkUserInfo> m_playerHistory = new List<ZNet.CrossNetworkUserInfo>();

	// Token: 0x04000E61 RID: 3681
	private static readonly Dictionary<ZNetPeer, float> PeersToDisconnectAfterKick = new Dictionary<ZNetPeer, float>();

	// Token: 0x04000E62 RID: 3682
	private const string PlatformDisplayNameKey = "platformDisplayName";

	// Token: 0x04000E63 RID: 3683
	private readonly Platform m_steamPlatform = new Platform("Steam");

	// Token: 0x020002E2 RID: 738
	public enum ConnectionStatus
	{
		// Token: 0x04002342 RID: 9026
		None,
		// Token: 0x04002343 RID: 9027
		Connecting,
		// Token: 0x04002344 RID: 9028
		Connected,
		// Token: 0x04002345 RID: 9029
		ErrorVersion,
		// Token: 0x04002346 RID: 9030
		ErrorDisconnected,
		// Token: 0x04002347 RID: 9031
		ErrorConnectFailed,
		// Token: 0x04002348 RID: 9032
		ErrorPassword,
		// Token: 0x04002349 RID: 9033
		ErrorAlreadyConnected,
		// Token: 0x0400234A RID: 9034
		ErrorBanned,
		// Token: 0x0400234B RID: 9035
		ErrorFull,
		// Token: 0x0400234C RID: 9036
		ErrorPlatformExcluded,
		// Token: 0x0400234D RID: 9037
		ErrorCrossplayPrivilege,
		// Token: 0x0400234E RID: 9038
		ErrorKicked
	}

	// Token: 0x020002E3 RID: 739
	public struct CrossNetworkUserInfo : IEquatable<ZNet.CrossNetworkUserInfo>
	{
		// Token: 0x06002171 RID: 8561 RVA: 0x000EB5F6 File Offset: 0x000E97F6
		public override bool Equals(object other)
		{
			return other is ZNet.CrossNetworkUserInfo && this.Equals((ZNet.CrossNetworkUserInfo)other);
		}

		// Token: 0x06002172 RID: 8562 RVA: 0x000EB60E File Offset: 0x000E980E
		public bool Equals(ZNet.CrossNetworkUserInfo other)
		{
			return this.m_id == other.m_id && this.m_displayName == other.m_displayName;
		}

		// Token: 0x06002173 RID: 8563 RVA: 0x000EB636 File Offset: 0x000E9836
		public override int GetHashCode()
		{
			return HashCode.Combine<PlatformUserID, string>(this.m_id, this.m_displayName);
		}

		// Token: 0x06002174 RID: 8564 RVA: 0x000EB649 File Offset: 0x000E9849
		public static bool operator ==(ZNet.CrossNetworkUserInfo lhs, ZNet.CrossNetworkUserInfo rhs)
		{
			return lhs.Equals(rhs);
		}

		// Token: 0x06002175 RID: 8565 RVA: 0x000EB653 File Offset: 0x000E9853
		public static bool operator !=(ZNet.CrossNetworkUserInfo lhs, ZNet.CrossNetworkUserInfo rhs)
		{
			return !lhs.Equals(rhs);
		}

		// Token: 0x06002176 RID: 8566 RVA: 0x000EB660 File Offset: 0x000E9860
		public override string ToString()
		{
			return string.Format("{0} ({1})", this.m_displayName, this.m_id);
		}

		// Token: 0x0400234F RID: 9039
		public PlatformUserID m_id;

		// Token: 0x04002350 RID: 9040
		public string m_displayName;
	}

	// Token: 0x020002E4 RID: 740
	public struct PlayerInfo
	{
		// Token: 0x06002177 RID: 8567 RVA: 0x000EB680 File Offset: 0x000E9880
		public override string ToString()
		{
			string str = "([";
			if (string.IsNullOrEmpty(this.m_name))
			{
				str += "-";
			}
			else
			{
				str += this.m_name;
			}
			str += string.Format(", {0}], [", this.m_characterID);
			if (string.IsNullOrEmpty(this.m_userInfo.m_displayName))
			{
				str += "-";
			}
			else
			{
				str += this.m_userInfo.m_displayName;
			}
			str += string.Format(", {0}], ", this.m_characterID);
			if (string.IsNullOrEmpty(this.m_serverAssignedDisplayName))
			{
				str += "-";
			}
			else
			{
				str += this.m_serverAssignedDisplayName;
			}
			return str + ")";
		}

		// Token: 0x04002351 RID: 9041
		public string m_name;

		// Token: 0x04002352 RID: 9042
		public ZDOID m_characterID;

		// Token: 0x04002353 RID: 9043
		public ZNet.CrossNetworkUserInfo m_userInfo;

		// Token: 0x04002354 RID: 9044
		public string m_serverAssignedDisplayName;

		// Token: 0x04002355 RID: 9045
		public bool m_publicPosition;

		// Token: 0x04002356 RID: 9046
		public Vector3 m_position;
	}
}
