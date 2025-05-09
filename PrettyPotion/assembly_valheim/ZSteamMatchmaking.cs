using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Splatform;
using Steamworks;
using UnityEngine;

// Token: 0x020000EC RID: 236
public class ZSteamMatchmaking
{
	// Token: 0x17000080 RID: 128
	// (get) Token: 0x06000F35 RID: 3893 RVA: 0x00072A24 File Offset: 0x00070C24
	public static ZSteamMatchmaking instance
	{
		get
		{
			return ZSteamMatchmaking.m_instance;
		}
	}

	// Token: 0x06000F36 RID: 3894 RVA: 0x00072A2B File Offset: 0x00070C2B
	public static void Initialize()
	{
		if (ZSteamMatchmaking.m_instance == null)
		{
			ZSteamMatchmaking.m_instance = new ZSteamMatchmaking();
		}
	}

	// Token: 0x06000F37 RID: 3895 RVA: 0x00072A40 File Offset: 0x00070C40
	private ZSteamMatchmaking()
	{
		this.m_steamServerCallbackHandler = new ISteamMatchmakingServerListResponse(new ISteamMatchmakingServerListResponse.ServerResponded(this.OnServerResponded), new ISteamMatchmakingServerListResponse.ServerFailedToRespond(this.OnServerFailedToRespond), new ISteamMatchmakingServerListResponse.RefreshComplete(this.OnRefreshComplete));
		this.m_joinServerCallbackHandler = new ISteamMatchmakingPingResponse(new ISteamMatchmakingPingResponse.ServerResponded(this.OnJoinServerRespond), new ISteamMatchmakingPingResponse.ServerFailedToRespond(this.OnJoinServerFailed));
		this.m_lobbyCreated = CallResult<LobbyCreated_t>.Create(new CallResult<LobbyCreated_t>.APIDispatchDelegate(this.OnLobbyCreated));
		this.m_lobbyMatchList = CallResult<LobbyMatchList_t>.Create(new CallResult<LobbyMatchList_t>.APIDispatchDelegate(this.OnLobbyMatchList));
		this.m_changeServer = Callback<GameServerChangeRequested_t>.Create(new Callback<GameServerChangeRequested_t>.DispatchDelegate(this.OnChangeServerRequest));
		this.m_joinRequest = Callback<GameLobbyJoinRequested_t>.Create(new Callback<GameLobbyJoinRequested_t>.DispatchDelegate(this.OnJoinRequest));
		this.m_lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(new Callback<LobbyDataUpdate_t>.DispatchDelegate(this.OnLobbyDataUpdate));
		this.m_authSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(new Callback<GetAuthSessionTicketResponse_t>.DispatchDelegate(this.OnAuthSessionTicketResponse));
	}

	// Token: 0x06000F38 RID: 3896 RVA: 0x00072BA4 File Offset: 0x00070DA4
	public byte[] RequestSessionTicket(ref SteamNetworkingIdentity serverIdentity)
	{
		this.ReleaseSessionTicket();
		byte[] array = new byte[1024];
		uint num = 0U;
		SteamNetworkingIdentity steamNetworkingIdentity = default(SteamNetworkingIdentity);
		this.m_authTicket = SteamUser.GetAuthSessionTicket(array, 1024, out num, ref steamNetworkingIdentity);
		if (this.m_authTicket == HAuthTicket.Invalid)
		{
			return null;
		}
		byte[] array2 = new byte[num];
		Buffer.BlockCopy(array, 0, array2, 0, (int)num);
		return array2;
	}

	// Token: 0x06000F39 RID: 3897 RVA: 0x00072C07 File Offset: 0x00070E07
	public void ReleaseSessionTicket()
	{
		if (this.m_authTicket == HAuthTicket.Invalid)
		{
			return;
		}
		SteamUser.CancelAuthTicket(this.m_authTicket);
		this.m_authTicket = HAuthTicket.Invalid;
		ZLog.Log("Released session ticket");
	}

	// Token: 0x06000F3A RID: 3898 RVA: 0x00072C3C File Offset: 0x00070E3C
	public bool VerifySessionTicket(byte[] ticket, CSteamID steamID)
	{
		return SteamUser.BeginAuthSession(ticket, ticket.Length, steamID) == EBeginAuthSessionResult.k_EBeginAuthSessionResultOK;
	}

	// Token: 0x06000F3B RID: 3899 RVA: 0x00072C4B File Offset: 0x00070E4B
	private void OnAuthSessionTicketResponse(GetAuthSessionTicketResponse_t data)
	{
		ZLog.Log("Session auth respons callback");
		ZSteamMatchmaking.AuthSessionTicketResponseHandler authSessionTicketResponse = this.AuthSessionTicketResponse;
		if (authSessionTicketResponse == null)
		{
			return;
		}
		authSessionTicketResponse();
	}

	// Token: 0x06000F3C RID: 3900 RVA: 0x00072C67 File Offset: 0x00070E67
	private void OnSteamServersConnected(SteamServersConnected_t data)
	{
		ZLog.Log("Game server connected");
	}

	// Token: 0x06000F3D RID: 3901 RVA: 0x00072C73 File Offset: 0x00070E73
	private void OnSteamServersDisconnected(SteamServersDisconnected_t data)
	{
		ZLog.LogWarning("Game server disconnected");
	}

	// Token: 0x06000F3E RID: 3902 RVA: 0x00072C7F File Offset: 0x00070E7F
	private void OnSteamServersConnectFail(SteamServerConnectFailure_t data)
	{
		ZLog.LogWarning("Game server connected failed");
	}

	// Token: 0x06000F3F RID: 3903 RVA: 0x00072C8B File Offset: 0x00070E8B
	private void OnChangeServerRequest(GameServerChangeRequested_t data)
	{
		ZLog.Log("ZSteamMatchmaking got change server request to:" + data.m_rgchServer);
		this.QueueServerJoin(data.m_rgchServer);
	}

	// Token: 0x06000F40 RID: 3904 RVA: 0x00072CB0 File Offset: 0x00070EB0
	private void OnJoinRequest(GameLobbyJoinRequested_t data)
	{
		string str = "ZSteamMatchmaking got join request friend:";
		CSteamID csteamID = data.m_steamIDFriend;
		string str2 = csteamID.ToString();
		string str3 = "  lobby:";
		csteamID = data.m_steamIDLobby;
		ZLog.Log(str + str2 + str3 + csteamID.ToString());
		this.QueueLobbyJoin(data.m_steamIDLobby);
	}

	// Token: 0x06000F41 RID: 3905 RVA: 0x00072D08 File Offset: 0x00070F08
	private IPAddress FindIP(string host)
	{
		IPAddress result;
		try
		{
			IPAddress ipaddress;
			if (IPAddress.TryParse(host, out ipaddress))
			{
				result = ipaddress;
			}
			else
			{
				ZLog.Log("Not an ip address " + host + " doing dns lookup");
				IPHostEntry hostEntry = Dns.GetHostEntry(host);
				if (hostEntry.AddressList.Length == 0)
				{
					ZLog.Log("Dns lookup failed");
					result = null;
				}
				else
				{
					ZLog.Log("Got dns entries: " + hostEntry.AddressList.Length.ToString());
					foreach (IPAddress ipaddress2 in hostEntry.AddressList)
					{
						if (ipaddress2.AddressFamily == AddressFamily.InterNetwork)
						{
							return ipaddress2;
						}
					}
					result = null;
				}
			}
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while finding ip:" + ex.ToString());
			result = null;
		}
		return result;
	}

	// Token: 0x06000F42 RID: 3906 RVA: 0x00072DD8 File Offset: 0x00070FD8
	public bool ResolveIPFromAddrString(string addr, ref SteamNetworkingIPAddr destination)
	{
		bool result;
		try
		{
			string[] array = addr.Split(':', StringSplitOptions.None);
			if (array.Length < 2)
			{
				result = false;
			}
			else
			{
				IPAddress ipaddress = this.FindIP(array[0]);
				if (ipaddress == null)
				{
					ZLog.Log("Invalid address " + array[0]);
					result = false;
				}
				else
				{
					uint nIP = (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(ipaddress.GetAddressBytes(), 0));
					int num = int.Parse(array[1]);
					ZLog.Log("connect to ip:" + ipaddress.ToString() + " port:" + num.ToString());
					destination.SetIPv4(nIP, (ushort)num);
					result = true;
				}
			}
		}
		catch (Exception ex)
		{
			string str = "Exception when resolving IP address: ";
			Exception ex2 = ex;
			ZLog.Log(str + ((ex2 != null) ? ex2.ToString() : null));
			result = false;
		}
		return result;
	}

	// Token: 0x06000F43 RID: 3907 RVA: 0x00072EA4 File Offset: 0x000710A4
	public void QueueServerJoin(string addr)
	{
		SteamNetworkingIPAddr steamNetworkingIPAddr = default(SteamNetworkingIPAddr);
		if (this.ResolveIPFromAddrString(addr, ref steamNetworkingIPAddr))
		{
			this.m_joinData = new ServerJoinDataDedicated(steamNetworkingIPAddr.GetIPv4(), steamNetworkingIPAddr.m_port);
			return;
		}
		ZLog.Log("Couldn't resolve IP address.");
	}

	// Token: 0x06000F44 RID: 3908 RVA: 0x00072EE8 File Offset: 0x000710E8
	private void OnJoinServerRespond(gameserveritem_t serverData)
	{
		string str = "Got join server data ";
		string serverName = serverData.GetServerName();
		string str2 = "  ";
		CSteamID steamID = serverData.m_steamID;
		ZLog.Log(str + serverName + str2 + steamID.ToString());
		SteamNetworkingIPAddr steamNetworkingIPAddr = default(SteamNetworkingIPAddr);
		steamNetworkingIPAddr.SetIPv4(serverData.m_NetAdr.GetIP(), serverData.m_NetAdr.GetConnectionPort());
		this.m_joinData = new ServerJoinDataDedicated(steamNetworkingIPAddr.GetIPv4(), steamNetworkingIPAddr.m_port);
	}

	// Token: 0x06000F45 RID: 3909 RVA: 0x00072F60 File Offset: 0x00071160
	private void OnJoinServerFailed()
	{
		ZLog.Log("Failed to get join server data");
	}

	// Token: 0x06000F46 RID: 3910 RVA: 0x00072F6C File Offset: 0x0007116C
	private bool TryGetLobbyData(CSteamID lobbyID)
	{
		uint num;
		ushort num2;
		CSteamID csteamID;
		if (!SteamMatchmaking.GetLobbyGameServer(lobbyID, out num, out num2, out csteamID))
		{
			return false;
		}
		string str = "  hostid: ";
		CSteamID csteamID2 = csteamID;
		ZLog.Log(str + csteamID2.ToString());
		this.m_queuedJoinLobby = CSteamID.Nil;
		ServerStatus lobbyServerData = this.GetLobbyServerData(lobbyID);
		this.m_joinData = lobbyServerData.m_joinData;
		return true;
	}

	// Token: 0x06000F47 RID: 3911 RVA: 0x00072FC8 File Offset: 0x000711C8
	public void QueueLobbyJoin(CSteamID lobbyID)
	{
		if (!this.TryGetLobbyData(lobbyID))
		{
			string str = "Failed to get lobby data for lobby ";
			CSteamID csteamID = lobbyID;
			ZLog.Log(str + csteamID.ToString() + ", requesting lobby data");
			this.m_queuedJoinLobby = lobbyID;
			SteamMatchmaking.RequestLobbyData(lobbyID);
		}
		if (FejdStartup.instance == null)
		{
			if (UnifiedPopup.IsAvailable() && Menu.instance != null)
			{
				UnifiedPopup.Push(new YesNoPopup("$menu_joindifferentserver", "$menu_logoutprompt", delegate()
				{
					UnifiedPopup.Pop();
					if (Menu.instance != null)
					{
						Menu.instance.OnLogoutYes();
					}
				}, delegate()
				{
					UnifiedPopup.Pop();
					this.m_queuedJoinLobby = CSteamID.Nil;
					this.m_joinData = null;
				}, true));
				return;
			}
			Debug.LogWarning("Couldn't handle invite appropriately! Ignoring.");
			this.m_queuedJoinLobby = CSteamID.Nil;
			this.m_joinData = null;
		}
	}

	// Token: 0x06000F48 RID: 3912 RVA: 0x00073090 File Offset: 0x00071290
	private void OnLobbyDataUpdate(LobbyDataUpdate_t data)
	{
		CSteamID csteamID = new CSteamID(data.m_ulSteamIDLobby);
		if (csteamID == this.m_queuedJoinLobby)
		{
			if (this.TryGetLobbyData(csteamID))
			{
				ZLog.Log("Got lobby data, for queued lobby");
				return;
			}
		}
		else
		{
			ZLog.Log("Got requested lobby data");
			foreach (KeyValuePair<CSteamID, string> keyValuePair in this.m_requestedFriendGames)
			{
				if (keyValuePair.Key == csteamID)
				{
					ServerStatus lobbyServerData = this.GetLobbyServerData(csteamID);
					if (lobbyServerData != null)
					{
						lobbyServerData.m_joinData.m_serverName = keyValuePair.Value + " [" + lobbyServerData.m_joinData.m_serverName + "]";
						this.m_friendServers.Add(lobbyServerData);
						this.m_serverListRevision++;
					}
				}
			}
		}
	}

	// Token: 0x06000F49 RID: 3913 RVA: 0x00073178 File Offset: 0x00071378
	public void RegisterServer(string name, bool password, GameVersion gameVersion, List<string> modifiers, uint networkVersion, bool publicServer, string worldName, ZSteamMatchmaking.ServerRegistered serverRegisteredCallback)
	{
		this.UnregisterServer();
		this.serverRegisteredCallback = serverRegisteredCallback;
		SteamAPICall_t hAPICall = SteamMatchmaking.CreateLobby(publicServer ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly, 32);
		this.m_lobbyCreated.Set(hAPICall, null);
		this.m_registerServerName = name;
		this.m_registerPassword = password;
		this.m_registerGameVerson = gameVersion;
		this.m_registerNetworkVerson = networkVersion;
		this.m_registerModifiers = modifiers;
		ZLog.Log("Registering lobby");
	}

	// Token: 0x06000F4A RID: 3914 RVA: 0x000731E0 File Offset: 0x000713E0
	private void OnLobbyCreated(LobbyCreated_t data, bool ioError)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Lobby was created ",
			data.m_eResult.ToString(),
			"  ",
			data.m_ulSteamIDLobby.ToString(),
			"  error:",
			ioError.ToString()
		}));
		if (ioError)
		{
			ZSteamMatchmaking.ServerRegistered serverRegistered = this.serverRegisteredCallback;
			if (serverRegistered == null)
			{
				return;
			}
			serverRegistered(false);
			return;
		}
		else if (data.m_eResult == EResult.k_EResultNoConnection)
		{
			ZLog.LogWarning("Failed to connect to Steam to register the server!");
			ZSteamMatchmaking.ServerRegistered serverRegistered2 = this.serverRegisteredCallback;
			if (serverRegistered2 == null)
			{
				return;
			}
			serverRegistered2(false);
			return;
		}
		else
		{
			this.m_myLobby = new CSteamID(data.m_ulSteamIDLobby);
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "name", this.m_registerServerName))
			{
				Debug.LogError("Couldn't set name in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "password", this.m_registerPassword ? "1" : "0"))
			{
				Debug.LogError("Couldn't set password in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "version", this.m_registerGameVerson.ToString()))
			{
				Debug.LogError("Couldn't set game version in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "networkversion", this.m_registerNetworkVerson.ToString()))
			{
				Debug.LogError("Couldn't set network version in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "modifiers", StringUtils.EncodeStringListAsString(this.m_registerModifiers, true)))
			{
				Debug.LogError("Couldn't set modifiers in lobby");
			}
			OnlineBackendType onlineBackend = ZNet.m_onlineBackend;
			string pchValue;
			string pchValue2;
			string pchValue3;
			if (onlineBackend == OnlineBackendType.CustomSocket)
			{
				pchValue = "Dedicated";
				pchValue2 = ZNet.GetServerString(false);
				pchValue3 = "1";
			}
			else if (onlineBackend == OnlineBackendType.Steamworks)
			{
				pchValue = "Steam user";
				pchValue2 = "";
				pchValue3 = "0";
			}
			else if (onlineBackend == OnlineBackendType.PlayFab)
			{
				pchValue = "PlayFab user";
				pchValue2 = PlayFabManager.instance.Entity.Id;
				pchValue3 = "1";
			}
			else
			{
				Debug.LogError("Can't create lobby for server with unknown or unsupported backend");
				pchValue = "";
				pchValue2 = "";
				pchValue3 = "";
			}
			if (PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) != PrivilegeResult.Granted)
			{
				pchValue3 = "0";
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "serverType", pchValue))
			{
				Debug.LogError("Couldn't set backend in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "hostID", pchValue2))
			{
				Debug.LogError("Couldn't set host in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "isCrossplay", pchValue3))
			{
				Debug.LogError("Couldn't set crossplay in lobby");
			}
			SteamMatchmaking.SetLobbyGameServer(this.m_myLobby, 0U, 0, SteamUser.GetSteamID());
			ZSteamMatchmaking.ServerRegistered serverRegistered3 = this.serverRegisteredCallback;
			if (serverRegistered3 == null)
			{
				return;
			}
			serverRegistered3(true);
			return;
		}
	}

	// Token: 0x06000F4B RID: 3915 RVA: 0x00073462 File Offset: 0x00071662
	private void OnLobbyEnter(LobbyEnter_t data, bool ioError)
	{
		ZLog.LogWarning("Entering lobby " + data.m_ulSteamIDLobby.ToString());
	}

	// Token: 0x06000F4C RID: 3916 RVA: 0x0007347F File Offset: 0x0007167F
	public void UnregisterServer()
	{
		if (this.m_myLobby != CSteamID.Nil)
		{
			SteamMatchmaking.SetLobbyJoinable(this.m_myLobby, false);
			SteamMatchmaking.LeaveLobby(this.m_myLobby);
			this.m_myLobby = CSteamID.Nil;
		}
	}

	// Token: 0x06000F4D RID: 3917 RVA: 0x000734B6 File Offset: 0x000716B6
	public void RequestServerlist()
	{
		this.IsRefreshing = true;
		this.RequestFriendGames();
		this.RequestPublicLobbies();
		this.RequestDedicatedServers();
	}

	// Token: 0x06000F4E RID: 3918 RVA: 0x000734D1 File Offset: 0x000716D1
	public void StopServerListing()
	{
		if (this.m_haveListRequest)
		{
			SteamMatchmakingServers.ReleaseRequest(this.m_serverListRequest);
			this.m_haveListRequest = false;
			this.IsRefreshing = false;
		}
	}

	// Token: 0x06000F4F RID: 3919 RVA: 0x000734F4 File Offset: 0x000716F4
	private void RequestFriendGames()
	{
		this.m_friendServers.Clear();
		this.m_requestedFriendGames.Clear();
		int num = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		if (num == -1)
		{
			ZLog.Log("GetFriendCount returned -1, the current user is not logged in.");
			num = 0;
		}
		for (int i = 0; i < num; i++)
		{
			CSteamID friendByIndex = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendPersonaName = SteamFriends.GetFriendPersonaName(friendByIndex);
			FriendGameInfo_t friendGameInfo_t;
			if (SteamFriends.GetFriendGamePlayed(friendByIndex, out friendGameInfo_t) && friendGameInfo_t.m_gameID == (CGameID)((ulong)SteamManager.APP_ID) && friendGameInfo_t.m_steamIDLobby != CSteamID.Nil)
			{
				ZLog.Log("Friend is in our game");
				this.m_requestedFriendGames.Add(new KeyValuePair<CSteamID, string>(friendGameInfo_t.m_steamIDLobby, friendPersonaName));
				SteamMatchmaking.RequestLobbyData(friendGameInfo_t.m_steamIDLobby);
			}
		}
		this.m_serverListRevision++;
	}

	// Token: 0x06000F50 RID: 3920 RVA: 0x000735B8 File Offset: 0x000717B8
	private void RequestPublicLobbies()
	{
		SteamAPICall_t hAPICall = SteamMatchmaking.RequestLobbyList();
		this.m_lobbyMatchList.Set(hAPICall, null);
		this.m_refreshingPublicGames = true;
	}

	// Token: 0x06000F51 RID: 3921 RVA: 0x000735E0 File Offset: 0x000717E0
	private void RequestDedicatedServers()
	{
		if (this.m_haveListRequest)
		{
			SteamMatchmakingServers.ReleaseRequest(this.m_serverListRequest);
			this.m_haveListRequest = false;
		}
		this.m_dedicatedServers.Clear();
		this.m_serverListRequest = SteamMatchmakingServers.RequestInternetServerList(SteamUtils.GetAppID(), new MatchMakingKeyValuePair_t[0], 0U, this.m_steamServerCallbackHandler);
		this.m_haveListRequest = true;
	}

	// Token: 0x06000F52 RID: 3922 RVA: 0x00073638 File Offset: 0x00071838
	private void OnLobbyMatchList(LobbyMatchList_t data, bool ioError)
	{
		this.m_refreshingPublicGames = false;
		this.m_matchmakingServers.Clear();
		int num = 0;
		while ((long)num < (long)((ulong)data.m_nLobbiesMatching))
		{
			CSteamID lobbyByIndex = SteamMatchmaking.GetLobbyByIndex(num);
			ServerStatus lobbyServerData = this.GetLobbyServerData(lobbyByIndex);
			if (lobbyServerData != null)
			{
				this.m_matchmakingServers.Add(lobbyServerData);
			}
			num++;
		}
		this.m_serverListRevision++;
	}

	// Token: 0x06000F53 RID: 3923 RVA: 0x00073698 File Offset: 0x00071898
	private ServerStatus GetLobbyServerData(CSteamID lobbyID)
	{
		string lobbyData = SteamMatchmaking.GetLobbyData(lobbyID, "name");
		bool isPasswordProtected = SteamMatchmaking.GetLobbyData(lobbyID, "password") == "1";
		GameVersion gameVersion = GameVersion.ParseGameVersion(SteamMatchmaking.GetLobbyData(lobbyID, "version"));
		List<string> modifiers;
		StringUtils.TryDecodeStringAsICollection<List<string>>(SteamMatchmaking.GetLobbyData(lobbyID, "modifiers"), out modifiers);
		uint num = uint.TryParse(SteamMatchmaking.GetLobbyData(lobbyID, "networkversion"), out num) ? num : 0U;
		int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
		uint num2;
		ushort num3;
		CSteamID joinUserID;
		if (SteamMatchmaking.GetLobbyGameServer(lobbyID, out num2, out num3, out joinUserID))
		{
			string lobbyData2 = SteamMatchmaking.GetLobbyData(lobbyID, "hostID");
			string lobbyData3 = SteamMatchmaking.GetLobbyData(lobbyID, "serverType");
			string lobbyData4 = SteamMatchmaking.GetLobbyData(lobbyID, "isCrossplay");
			ServerStatus serverStatus;
			if (lobbyData3 == null || lobbyData3.Length != 0)
			{
				if (!(lobbyData3 == "Steam user"))
				{
					if (!(lobbyData3 == "PlayFab user"))
					{
						if (!(lobbyData3 == "Dedicated"))
						{
							ZLog.LogError("Couldn't get lobby data for unknown backend \"" + lobbyData3 + "\"! " + this.KnownBackendsString());
							return null;
						}
						ServerJoinDataDedicated serverJoinDataDedicated = new ServerJoinDataDedicated(lobbyData2);
						if (!serverJoinDataDedicated.IsValid())
						{
							return null;
						}
						serverStatus = new ServerStatus(serverJoinDataDedicated);
					}
					else
					{
						serverStatus = new ServerStatus(new ServerJoinDataPlayFabUser(lobbyData2));
						if (!serverStatus.m_joinData.IsValid())
						{
							return null;
						}
					}
				}
				else
				{
					serverStatus = new ServerStatus(new ServerJoinDataSteamUser(joinUserID));
				}
			}
			else
			{
				serverStatus = new ServerStatus(new ServerJoinDataSteamUser(joinUserID));
			}
			serverStatus.UpdateStatus(OnlineStatus.Online, lobbyData, (uint)numLobbyMembers, gameVersion, modifiers, num, isPasswordProtected, new Platform?((lobbyData4 == "1") ? Platform.Unknown : PlatformManager.DistributionPlatform.Platform), serverStatus.m_hostId, true);
			return serverStatus;
		}
		ZLog.Log("Failed to get lobby gameserver");
		return null;
	}

	// Token: 0x06000F54 RID: 3924 RVA: 0x00073844 File Offset: 0x00071A44
	public string KnownBackendsString()
	{
		List<string> list = new List<string>();
		list.Add("Steam user");
		list.Add("PlayFab user");
		list.Add("Dedicated");
		return "Known backends: " + string.Join(", ", from s in list
		select "\"" + s + "\"");
	}

	// Token: 0x06000F55 RID: 3925 RVA: 0x000738B1 File Offset: 0x00071AB1
	public void GetServers(List<ServerStatus> allServers)
	{
		if (this.m_friendsFilter)
		{
			this.FilterServers(this.m_friendServers, allServers);
			return;
		}
		this.FilterServers(this.m_matchmakingServers, allServers);
		this.FilterServers(this.m_dedicatedServers, allServers);
	}

	// Token: 0x06000F56 RID: 3926 RVA: 0x000738E4 File Offset: 0x00071AE4
	private void FilterServers(List<ServerStatus> input, List<ServerStatus> allServers)
	{
		string text = this.m_nameFilter.ToLowerInvariant();
		foreach (ServerStatus serverStatus in input)
		{
			if (text.Length == 0 || serverStatus.m_joinData.m_serverName.ToLowerInvariant().Contains(text))
			{
				allServers.Add(serverStatus);
			}
			if (allServers.Count >= 200)
			{
				break;
			}
		}
	}

	// Token: 0x06000F57 RID: 3927 RVA: 0x00073970 File Offset: 0x00071B70
	public bool CheckIfOnline(ServerJoinData dataToMatchAgainst, ref ServerStatus status)
	{
		for (int i = 0; i < this.m_friendServers.Count; i++)
		{
			if (this.m_friendServers[i].m_joinData.Equals(dataToMatchAgainst))
			{
				status = this.m_friendServers[i];
				return true;
			}
		}
		for (int j = 0; j < this.m_matchmakingServers.Count; j++)
		{
			if (this.m_matchmakingServers[j].m_joinData.Equals(dataToMatchAgainst))
			{
				status = this.m_matchmakingServers[j];
				return true;
			}
		}
		for (int k = 0; k < this.m_dedicatedServers.Count; k++)
		{
			if (this.m_dedicatedServers[k].m_joinData.Equals(dataToMatchAgainst))
			{
				status = this.m_dedicatedServers[k];
				return true;
			}
		}
		if (!this.IsRefreshing)
		{
			status = new ServerStatus(dataToMatchAgainst);
			status.UpdateStatus(OnlineStatus.Offline, dataToMatchAgainst.m_serverName, 0U, default(GameVersion), new List<string>(), 0U, false, null, status.m_hostId, true);
			return true;
		}
		return false;
	}

	// Token: 0x06000F58 RID: 3928 RVA: 0x00073A7E File Offset: 0x00071C7E
	public bool GetJoinHost(out ServerJoinData joinData)
	{
		joinData = this.m_joinData;
		if (this.m_joinData == null)
		{
			return false;
		}
		if (!this.m_joinData.IsValid())
		{
			return false;
		}
		this.m_joinData = null;
		return true;
	}

	// Token: 0x06000F59 RID: 3929 RVA: 0x00073AB0 File Offset: 0x00071CB0
	private void OnServerResponded(HServerListRequest request, int iServer)
	{
		gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(request, iServer);
		string serverName = serverDetails.GetServerName();
		SteamNetworkingIPAddr steamNetworkingIPAddr = default(SteamNetworkingIPAddr);
		steamNetworkingIPAddr.SetIPv4(serverDetails.m_NetAdr.GetIP(), serverDetails.m_NetAdr.GetConnectionPort());
		ServerStatus serverStatus = new ServerStatus(new ServerJoinDataDedicated(steamNetworkingIPAddr.GetIPv4(), steamNetworkingIPAddr.m_port));
		Dictionary<string, string> dictionary;
		string gameTags;
		uint num;
		List<string> modifiers;
		if (!StringUtils.TryDecodeStringAsIDictionary<Dictionary<string, string>>(serverDetails.GetGameTags(), out dictionary))
		{
			gameTags = serverDetails.GetGameTags();
			num = 0U;
			modifiers = new List<string>();
		}
		else
		{
			string s;
			if ((!dictionary.TryGetValue("g", out gameTags) && !dictionary.TryGetValue("gameversion", out gameTags)) || (!dictionary.TryGetValue("n", out s) && !dictionary.TryGetValue("networkversion", out s)) || !uint.TryParse(s, out num))
			{
				gameTags = serverDetails.GetGameTags();
				num = 0U;
			}
			string encodedString;
			Dictionary<string, string> kvps;
			if (num != 34U || !dictionary.TryGetValue("m", out encodedString) || !StringUtils.TryDecodeStringAsIDictionary<Dictionary<string, string>>(encodedString, out kvps) || !ServerOptionsGUI.TryConvertCompactKVPToModifierKeys<List<string>>(kvps, out modifiers))
			{
				modifiers = new List<string>();
			}
		}
		serverStatus.UpdateStatus(OnlineStatus.Online, serverName, (uint)serverDetails.m_nPlayers, GameVersion.ParseGameVersion(gameTags), modifiers, num, serverDetails.m_bPassword, new Platform?(PlatformManager.DistributionPlatform.Platform), serverStatus.m_hostId, true);
		this.m_dedicatedServers.Add(serverStatus);
		this.m_updateTriggerAccumulator++;
		if (this.m_updateTriggerAccumulator > 100)
		{
			this.m_updateTriggerAccumulator = 0;
			this.m_serverListRevision++;
		}
	}

	// Token: 0x06000F5A RID: 3930 RVA: 0x00073C26 File Offset: 0x00071E26
	private void OnServerFailedToRespond(HServerListRequest request, int iServer)
	{
	}

	// Token: 0x06000F5B RID: 3931 RVA: 0x00073C28 File Offset: 0x00071E28
	private void OnRefreshComplete(HServerListRequest request, EMatchMakingServerResponse response)
	{
		ZLog.Log("Refresh complete " + this.m_dedicatedServers.Count.ToString() + "  " + response.ToString());
		this.IsRefreshing = false;
		this.m_serverListRevision++;
	}

	// Token: 0x06000F5C RID: 3932 RVA: 0x00073C7E File Offset: 0x00071E7E
	public void SetNameFilter(string filter)
	{
		if (this.m_nameFilter == filter)
		{
			return;
		}
		this.m_nameFilter = filter;
		this.m_serverListRevision++;
	}

	// Token: 0x06000F5D RID: 3933 RVA: 0x00073CA4 File Offset: 0x00071EA4
	public void SetFriendFilter(bool enabled)
	{
		if (this.m_friendsFilter == enabled)
		{
			return;
		}
		this.m_friendsFilter = enabled;
		this.m_serverListRevision++;
	}

	// Token: 0x06000F5E RID: 3934 RVA: 0x00073CC5 File Offset: 0x00071EC5
	public int GetServerListRevision()
	{
		return this.m_serverListRevision;
	}

	// Token: 0x06000F5F RID: 3935 RVA: 0x00073CCD File Offset: 0x00071ECD
	public int GetTotalNrOfServers()
	{
		return this.m_matchmakingServers.Count + this.m_dedicatedServers.Count + this.m_friendServers.Count;
	}

	// Token: 0x14000009 RID: 9
	// (add) Token: 0x06000F60 RID: 3936 RVA: 0x00073CF4 File Offset: 0x00071EF4
	// (remove) Token: 0x06000F61 RID: 3937 RVA: 0x00073D2C File Offset: 0x00071F2C
	public event ZSteamMatchmaking.AuthSessionTicketResponseHandler AuthSessionTicketResponse;

	// Token: 0x17000081 RID: 129
	// (get) Token: 0x06000F62 RID: 3938 RVA: 0x00073D61 File Offset: 0x00071F61
	// (set) Token: 0x06000F63 RID: 3939 RVA: 0x00073D69 File Offset: 0x00071F69
	public bool IsRefreshing { get; private set; }

	// Token: 0x04000ED0 RID: 3792
	private static ZSteamMatchmaking m_instance;

	// Token: 0x04000ED1 RID: 3793
	private const int maxServers = 200;

	// Token: 0x04000ED2 RID: 3794
	private List<ServerStatus> m_matchmakingServers = new List<ServerStatus>();

	// Token: 0x04000ED3 RID: 3795
	private List<ServerStatus> m_dedicatedServers = new List<ServerStatus>();

	// Token: 0x04000ED4 RID: 3796
	private List<ServerStatus> m_friendServers = new List<ServerStatus>();

	// Token: 0x04000ED5 RID: 3797
	private int m_serverListRevision;

	// Token: 0x04000ED6 RID: 3798
	private int m_updateTriggerAccumulator;

	// Token: 0x04000ED7 RID: 3799
	private CallResult<LobbyCreated_t> m_lobbyCreated;

	// Token: 0x04000ED8 RID: 3800
	private CallResult<LobbyMatchList_t> m_lobbyMatchList;

	// Token: 0x04000ED9 RID: 3801
	private CallResult<LobbyEnter_t> m_lobbyEntered;

	// Token: 0x04000EDA RID: 3802
	private Callback<GameServerChangeRequested_t> m_changeServer;

	// Token: 0x04000EDB RID: 3803
	private Callback<GameLobbyJoinRequested_t> m_joinRequest;

	// Token: 0x04000EDC RID: 3804
	private Callback<LobbyDataUpdate_t> m_lobbyDataUpdate;

	// Token: 0x04000EDD RID: 3805
	private Callback<GetAuthSessionTicketResponse_t> m_authSessionTicketResponse;

	// Token: 0x04000EDE RID: 3806
	private Callback<SteamServerConnectFailure_t> m_steamServerConnectFailure;

	// Token: 0x04000EDF RID: 3807
	private Callback<SteamServersConnected_t> m_steamServersConnected;

	// Token: 0x04000EE0 RID: 3808
	private Callback<SteamServersDisconnected_t> m_steamServersDisconnected;

	// Token: 0x04000EE1 RID: 3809
	private ZSteamMatchmaking.ServerRegistered serverRegisteredCallback;

	// Token: 0x04000EE2 RID: 3810
	private CSteamID m_myLobby = CSteamID.Nil;

	// Token: 0x04000EE3 RID: 3811
	private CSteamID m_queuedJoinLobby = CSteamID.Nil;

	// Token: 0x04000EE4 RID: 3812
	private ServerJoinData m_joinData;

	// Token: 0x04000EE5 RID: 3813
	private List<KeyValuePair<CSteamID, string>> m_requestedFriendGames = new List<KeyValuePair<CSteamID, string>>();

	// Token: 0x04000EE6 RID: 3814
	private ISteamMatchmakingServerListResponse m_steamServerCallbackHandler;

	// Token: 0x04000EE7 RID: 3815
	private ISteamMatchmakingPingResponse m_joinServerCallbackHandler;

	// Token: 0x04000EE8 RID: 3816
	private HServerQuery m_joinQuery;

	// Token: 0x04000EE9 RID: 3817
	private HServerListRequest m_serverListRequest;

	// Token: 0x04000EEA RID: 3818
	private bool m_haveListRequest;

	// Token: 0x04000EEB RID: 3819
	private bool m_refreshingDedicatedServers;

	// Token: 0x04000EEC RID: 3820
	private bool m_refreshingPublicGames;

	// Token: 0x04000EEE RID: 3822
	private string m_registerServerName = "";

	// Token: 0x04000EEF RID: 3823
	private bool m_registerPassword;

	// Token: 0x04000EF0 RID: 3824
	private GameVersion m_registerGameVerson;

	// Token: 0x04000EF1 RID: 3825
	private uint m_registerNetworkVerson;

	// Token: 0x04000EF2 RID: 3826
	private List<string> m_registerModifiers = new List<string>();

	// Token: 0x04000EF3 RID: 3827
	private string m_nameFilter = "";

	// Token: 0x04000EF4 RID: 3828
	private bool m_friendsFilter = true;

	// Token: 0x04000EF5 RID: 3829
	private HAuthTicket m_authTicket = HAuthTicket.Invalid;

	// Token: 0x020002F6 RID: 758
	// (Invoke) Token: 0x060021AA RID: 8618
	public delegate void AuthSessionTicketResponseHandler();

	// Token: 0x020002F7 RID: 759
	// (Invoke) Token: 0x060021AE RID: 8622
	public delegate void ServerRegistered(bool success);
}
