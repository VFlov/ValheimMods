using System;
using System.Collections.Generic;
using System.Threading;
using NetworkingUtils;
using PartyCSharpSDK;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using PlayFab.Party;
using Splatform;
using UnityEngine;

// Token: 0x02000107 RID: 263
public class ZPlayFabMatchmaking
{
	// Token: 0x1400000B RID: 11
	// (add) Token: 0x0600105C RID: 4188 RVA: 0x000787A0 File Offset: 0x000769A0
	// (remove) Token: 0x0600105D RID: 4189 RVA: 0x000787D4 File Offset: 0x000769D4
	public static event ZPlayFabMatchmakeLobbyLeftCallback LobbyLeft;

	// Token: 0x17000091 RID: 145
	// (get) Token: 0x0600105E RID: 4190 RVA: 0x00078807 File Offset: 0x00076A07
	public static ZPlayFabMatchmaking instance
	{
		get
		{
			if (ZPlayFabMatchmaking.m_instance == null)
			{
				ZPlayFabMatchmaking.m_instance = new ZPlayFabMatchmaking();
			}
			return ZPlayFabMatchmaking.m_instance;
		}
	}

	// Token: 0x17000092 RID: 146
	// (get) Token: 0x0600105F RID: 4191 RVA: 0x0007881F File Offset: 0x00076A1F
	// (set) Token: 0x06001060 RID: 4192 RVA: 0x00078826 File Offset: 0x00076A26
	public static string JoinCode { get; internal set; }

	// Token: 0x17000093 RID: 147
	// (get) Token: 0x06001061 RID: 4193 RVA: 0x00078830 File Offset: 0x00076A30
	// (set) Token: 0x06001062 RID: 4194 RVA: 0x00078870 File Offset: 0x00076A70
	public static string PublicIP
	{
		get
		{
			object mtx = ZPlayFabMatchmaking.m_mtx;
			string publicIP;
			lock (mtx)
			{
				publicIP = ZPlayFabMatchmaking.m_publicIP;
			}
			return publicIP;
		}
		private set
		{
			object mtx = ZPlayFabMatchmaking.m_mtx;
			lock (mtx)
			{
				ZPlayFabMatchmaking.m_publicIP = value;
			}
		}
	}

	// Token: 0x06001063 RID: 4195 RVA: 0x000788B0 File Offset: 0x00076AB0
	public static void Initialize(bool isServer)
	{
		ZPlayFabMatchmaking.JoinCode = (isServer ? "" : "000000");
	}

	// Token: 0x06001064 RID: 4196 RVA: 0x000788C6 File Offset: 0x00076AC6
	public void Update(float deltaTime)
	{
		if (this.ReconnectNetwork(deltaTime))
		{
			return;
		}
		this.RefreshLobby(deltaTime);
		this.RetryJoinCodeUniquenessCheck(deltaTime);
		this.UpdateActiveLobbySearches(deltaTime);
		this.UpdateBackgroundLobbySearches(deltaTime);
	}

	// Token: 0x06001065 RID: 4197 RVA: 0x000788EE File Offset: 0x00076AEE
	private bool IsJoinedToNetwork()
	{
		return this.m_serverData != null && !string.IsNullOrEmpty(this.m_serverData.networkId);
	}

	// Token: 0x06001066 RID: 4198 RVA: 0x0007890D File Offset: 0x00076B0D
	private bool IsReconnectNetworkTimerActive()
	{
		return this.m_lostNetworkRetryIn > 0f;
	}

	// Token: 0x06001067 RID: 4199 RVA: 0x0007891C File Offset: 0x00076B1C
	private void StartReconnectNetworkTimer(int code = -1)
	{
		this.m_lostNetworkRetryIn = 30f;
		if (ZPlayFabMatchmaking.DoFastRecovery(code))
		{
			ZLog.Log("PlayFab host fast recovery");
			this.m_lostNetworkRetryIn = 12f;
		}
	}

	// Token: 0x06001068 RID: 4200 RVA: 0x00078946 File Offset: 0x00076B46
	private static bool DoFastRecovery(int code)
	{
		return code == 63 || code == 11;
	}

	// Token: 0x06001069 RID: 4201 RVA: 0x00078954 File Offset: 0x00076B54
	private void StopReconnectNetworkTimer()
	{
		this.m_isResettingNetwork = false;
		this.m_lostNetworkRetryIn = -1f;
		if (this.m_serverData != null && !this.IsJoinedToNetwork())
		{
			this.CreateAndJoinNetwork();
		}
	}

	// Token: 0x0600106A RID: 4202 RVA: 0x00078980 File Offset: 0x00076B80
	private bool ReconnectNetwork(float deltaTime)
	{
		if (!this.IsReconnectNetworkTimerActive())
		{
			if (this.IsJoinedToNetwork() && !PlayFabMultiplayerManager.Get().IsConnectedToNetworkState())
			{
				PlayFabMultiplayerManager.Get().ResetParty();
				this.StartReconnectNetworkTimer(-1);
				this.m_serverData.networkId = null;
			}
			return false;
		}
		this.m_lostNetworkRetryIn -= deltaTime;
		if (this.m_lostNetworkRetryIn <= 0f)
		{
			ZLog.Log(string.Format("PlayFab reconnect server '{0}'", this.m_serverData.serverName));
			this.m_isConnectingToNetwork = false;
			this.m_serverData.networkId = null;
			this.StopReconnectNetworkTimer();
		}
		else if (!this.m_isConnectingToNetwork && !this.m_isResettingNetwork && this.m_lostNetworkRetryIn <= 12f)
		{
			PlayFabMultiplayerManager.Get().ResetParty();
			this.m_isResettingNetwork = true;
			this.m_isConnectingToNetwork = false;
		}
		return true;
	}

	// Token: 0x0600106B RID: 4203 RVA: 0x00078A4E File Offset: 0x00076C4E
	private void StartRefreshLobbyTimer()
	{
		this.m_refreshLobbyTimer = UnityEngine.Random.Range(540f, 840f);
	}

	// Token: 0x0600106C RID: 4204 RVA: 0x00078A68 File Offset: 0x00076C68
	private void RefreshLobby(float deltaTime)
	{
		if (this.m_serverData == null || this.m_serverData.networkId == null)
		{
			return;
		}
		bool flag = this.m_serverData.isDedicatedServer && string.IsNullOrEmpty(this.m_serverData.serverIp) && !string.IsNullOrEmpty(ZPlayFabMatchmaking.PublicIP);
		this.m_refreshLobbyTimer -= deltaTime;
		if (this.m_refreshLobbyTimer < 0f || flag)
		{
			this.StartRefreshLobbyTimer();
			UpdateLobbyRequest updateLobbyRequest = new UpdateLobbyRequest
			{
				LobbyId = this.m_serverData.lobbyId
			};
			if (flag)
			{
				this.m_serverData.serverIp = this.GetServerIPAndPort();
				ZLog.Log("Updating lobby with public IP " + this.m_serverData.serverIp);
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary["string_key10"] = this.m_serverData.serverIp;
				Dictionary<string, string> searchData = dictionary;
				updateLobbyRequest.SearchData = searchData;
			}
			PlayFabMultiplayerAPI.UpdateLobby(updateLobbyRequest, delegate(LobbyEmptyResult _)
			{
				ZLog.Log(string.Format("Lobby {0} for world '{1}' and network {2} refreshed", this.m_serverData.lobbyId, this.m_serverData.serverName, this.m_serverData.networkId));
			}, new Action<PlayFabError>(this.OnRefreshFailed), null, null);
		}
	}

	// Token: 0x0600106D RID: 4205 RVA: 0x00078B6B File Offset: 0x00076D6B
	private void OnRefreshFailed(PlayFabError err)
	{
		this.CreateLobby(true, delegate(CreateLobbyResult _)
		{
			ZLog.Log(string.Format("Lobby {0} for world '{1}' recreated", this.m_serverData.lobbyId, this.m_serverData.serverName));
		}, delegate(PlayFabError err)
		{
			ZLog.LogWarning(string.Format("Failed to refresh lobby {0} for world '{1}': {2}", this.m_serverData.lobbyId, this.m_serverData.serverName, err.GenerateErrorReport()));
		});
	}

	// Token: 0x0600106E RID: 4206 RVA: 0x00078B8C File Offset: 0x00076D8C
	private void RetryJoinCodeUniquenessCheck(float deltaTime)
	{
		if (this.m_retryIn > 0f)
		{
			this.m_retryIn -= deltaTime;
			if (this.m_retryIn <= 0f)
			{
				this.CheckJoinCodeIsUnique();
			}
		}
	}

	// Token: 0x0600106F RID: 4207 RVA: 0x00078BBC File Offset: 0x00076DBC
	private void UpdateActiveLobbySearches(float deltaTime)
	{
		for (int i = 0; i < this.m_activeSearches.Count; i++)
		{
			ZPlayFabLobbySearch zplayFabLobbySearch = this.m_activeSearches[i];
			if (zplayFabLobbySearch.IsDone)
			{
				this.m_activeSearches.RemoveAt(i);
				i--;
			}
			else
			{
				zplayFabLobbySearch.Update(deltaTime);
			}
		}
	}

	// Token: 0x06001070 RID: 4208 RVA: 0x00078C10 File Offset: 0x00076E10
	private void UpdateBackgroundLobbySearches(float deltaTime)
	{
		if (this.m_submitBackgroundSearchIn >= 0f)
		{
			this.m_submitBackgroundSearchIn -= deltaTime;
			return;
		}
		if (this.m_pendingSearches.Count > 0)
		{
			this.m_submitBackgroundSearchIn = 2f;
			ZPlayFabLobbySearch zplayFabLobbySearch = this.m_pendingSearches.Dequeue();
			zplayFabLobbySearch.FindLobby();
			this.m_activeSearches.Add(zplayFabLobbySearch);
		}
	}

	// Token: 0x06001071 RID: 4209 RVA: 0x00078C70 File Offset: 0x00076E70
	private void OnFailed(string what, PlayFabError error)
	{
		ZLog.LogError("PlayFab " + what + " failed: " + error.ToString());
		this.UnregisterServer();
	}

	// Token: 0x06001072 RID: 4210 RVA: 0x00078C94 File Offset: 0x00076E94
	private void OnSessionUpdated(ZPlayFabMatchmaking.State newState)
	{
		this.m_state = newState;
		switch (this.m_state)
		{
		case ZPlayFabMatchmaking.State.Creating:
			ZLog.Log(string.Format("Session \"{0}\" registered with join code {1}", this.m_serverData.serverName, ZPlayFabMatchmaking.JoinCode));
			this.m_retries = 100;
			this.CheckJoinCodeIsUnique();
			return;
		case ZPlayFabMatchmaking.State.RegenerateJoinCode:
			this.RegenerateLobbyJoinCode();
			ZLog.Log(string.Format("Created new join code {0} for session \"{1}\"", ZPlayFabMatchmaking.JoinCode, this.m_serverData.serverName));
			return;
		case ZPlayFabMatchmaking.State.Active:
			ZLog.Log(string.Format("Session \"{0}\" with join code {1} is active with {2} player(s)", this.m_serverData.serverName, ZPlayFabMatchmaking.JoinCode, this.m_serverData.numPlayers));
			return;
		default:
			return;
		}
	}

	// Token: 0x06001073 RID: 4211 RVA: 0x00078D48 File Offset: 0x00076F48
	private void SetPlatformMatchmakingData()
	{
		IMatchmakingProvider matchmakingProvider = PlatformManager.DistributionPlatform.MatchmakingProvider;
		if (matchmakingProvider != null)
		{
			MultiplayerSessionData multiplayerSession = new MultiplayerSessionData
			{
				m_connectionString = this.m_serverData.remotePlayerId,
				m_maxPlayers = 10U,
				m_currentPlayers = this.m_serverData.numPlayers,
				m_joinRestriction = MultiplayerJoinRestriction.Friends
			};
			matchmakingProvider.SetMultiplayerSession(multiplayerSession);
		}
	}

	// Token: 0x06001074 RID: 4212 RVA: 0x00078DAC File Offset: 0x00076FAC
	private void ClearPlatformMatchmakingData()
	{
		IMatchmakingProvider matchmakingProvider = PlatformManager.DistributionPlatform.MatchmakingProvider;
		if (matchmakingProvider != null)
		{
			matchmakingProvider.ClearMultiplayerSession();
		}
	}

	// Token: 0x06001075 RID: 4213 RVA: 0x00078DD0 File Offset: 0x00076FD0
	private void UpdateNumPlayers(string info)
	{
		this.m_serverData.numPlayers = ZPlayFabSocket.NumSockets();
		if (!this.m_serverData.isDedicatedServer)
		{
			this.m_serverData.numPlayers += 1U;
		}
		ZLog.Log(string.Format("{0} server \"{1}\" that has join code {2}, now {3} player(s)", new object[]
		{
			info,
			this.m_serverData.serverName,
			ZPlayFabMatchmaking.JoinCode,
			this.m_serverData.numPlayers
		}));
	}

	// Token: 0x06001076 RID: 4214 RVA: 0x00078E4F File Offset: 0x0007704F
	private void OnRemotePlayerLeft(object sender, PlayFabPlayer player)
	{
		if (player == null)
		{
			ZLog.LogWarning("Player that left was null! Ignoring.");
			return;
		}
		ZPlayFabSocket.LostConnection(player);
		this.UpdateNumPlayers("Player connection lost");
	}

	// Token: 0x06001077 RID: 4215 RVA: 0x00078E70 File Offset: 0x00077070
	private void OnRemotePlayerJoined(object sender, PlayFabPlayer player)
	{
		this.StopReconnectNetworkTimer();
		ZPlayFabSocket.QueueConnection(player);
		this.UpdateNumPlayers("Player joined");
	}

	// Token: 0x06001078 RID: 4216 RVA: 0x00078E8C File Offset: 0x0007708C
	private void OnNetworkJoined(object sender, string networkId)
	{
		ZLog.Log(string.Format("Joined PlayFab Party network with ID \"{0}\"", networkId));
		if (this.m_serverData.networkId == null || this.m_serverData.networkId != networkId)
		{
			this.m_serverData.networkId = networkId;
			this.CreateLobby(false, new Action<CreateLobbyResult>(this.OnCreateLobbySuccess), delegate(PlayFabError error)
			{
				this.OnFailed("create lobby", error);
			});
		}
		this.m_isConnectingToNetwork = false;
		this.m_isResettingNetwork = false;
		this.StopReconnectNetworkTimer();
		this.StartRefreshLobbyTimer();
	}

	// Token: 0x06001079 RID: 4217 RVA: 0x00078F10 File Offset: 0x00077110
	private void CreateLobby(bool refresh, Action<CreateLobbyResult> resultCallback, Action<PlayFabError> errorCallback)
	{
		PlayFab.MultiplayerModels.EntityKey entityKeyForLocalUser = ZPlayFabMatchmaking.GetEntityKeyForLocalUser();
		List<Member> members = new List<Member>
		{
			new Member
			{
				MemberEntity = entityKeyForLocalUser
			}
		};
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string key = PlayFabAttrKey.HavePassword.ToKeyString();
		dictionary[key] = this.m_serverData.havePassword.ToString();
		string key2 = PlayFabAttrKey.WorldName.ToKeyString();
		dictionary[key2] = this.m_serverData.worldName;
		string key3 = PlayFabAttrKey.NetworkId.ToKeyString();
		dictionary[key3] = this.m_serverData.networkId;
		Dictionary<string, string> lobbyData = dictionary;
		string value = "";
		Dictionary<string, string> dictionaryToEncode;
		if (ServerOptionsGUI.TryConvertModifierKeysToCompactKVP<Dictionary<string, string>>(this.m_serverData.modifiers, out dictionaryToEncode))
		{
			value = StringUtils.EncodeDictionaryAsString(dictionaryToEncode, false);
		}
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2["string_key9"] = DateTime.UtcNow.Ticks.ToString();
		dictionary2["string_key5"] = this.m_serverData.serverName;
		dictionary2["string_key3"] = this.m_serverData.isCommunityServer.ToString();
		dictionary2["string_key4"] = this.m_serverData.joinCode;
		dictionary2["string_key2"] = refresh.ToString();
		dictionary2["string_key1"] = this.m_serverData.remotePlayerId;
		dictionary2["string_key6"] = this.m_serverData.gameVersion.ToString();
		dictionary2["string_key14"] = value;
		dictionary2["number_key13"] = this.m_serverData.networkVersion.ToString();
		dictionary2["string_key7"] = this.m_serverData.isDedicatedServer.ToString();
		dictionary2["string_key8"] = this.m_serverData.platformUserID.ToString();
		dictionary2["string_key10"] = this.m_serverData.serverIp;
		dictionary2["number_key11"] = ZPlayFabMatchmaking.GetSearchPage().ToString();
		dictionary2["string_key12"] = ((PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) == PrivilegeResult.Granted) ? Platform.Unknown : PlatformManager.DistributionPlatform.Platform).ToString();
		Dictionary<string, string> searchData = dictionary2;
		Debug.Log("This is the serverIP used to register the server: " + this.m_serverData.serverIp);
		CreateLobbyRequest createLobbyRequest = new CreateLobbyRequest();
		createLobbyRequest.AccessPolicy = new AccessPolicy?(AccessPolicy.Public);
		createLobbyRequest.MaxPlayers = 10U;
		createLobbyRequest.Members = members;
		createLobbyRequest.Owner = entityKeyForLocalUser;
		createLobbyRequest.LobbyData = lobbyData;
		createLobbyRequest.SearchData = searchData;
		if (this.m_serverData.isCommunityServer)
		{
			ZPlayFabMatchmaking.AddNameSearchFilter(searchData, this.m_serverData.serverName);
		}
		PlayFabMultiplayerAPI.CreateLobby(createLobbyRequest, resultCallback, errorCallback, null, null);
	}

	// Token: 0x0600107A RID: 4218 RVA: 0x000791BA File Offset: 0x000773BA
	private static int GetSearchPage()
	{
		return UnityEngine.Random.Range(0, 4);
	}

	// Token: 0x0600107B RID: 4219 RVA: 0x000791C4 File Offset: 0x000773C4
	internal static PlayFab.MultiplayerModels.EntityKey GetEntityKeyForLocalUser()
	{
		PlayFab.ClientModels.EntityKey entity = PlayFabManager.instance.Entity;
		return new PlayFab.MultiplayerModels.EntityKey
		{
			Id = entity.Id,
			Type = entity.Type
		};
	}

	// Token: 0x0600107C RID: 4220 RVA: 0x000791F9 File Offset: 0x000773F9
	private void OnCreateLobbySuccess(CreateLobbyResult result)
	{
		ZLog.Log(string.Format("Created PlayFab lobby with ID \"{0}\", ConnectionString \"{1}\" and owned by \"{2}\"", result.LobbyId, result.ConnectionString, this.m_serverData.remotePlayerId));
		this.m_serverData.lobbyId = result.LobbyId;
		this.OnSessionUpdated(ZPlayFabMatchmaking.State.Creating);
	}

	// Token: 0x0600107D RID: 4221 RVA: 0x0007923C File Offset: 0x0007743C
	private void GenerateJoinCode()
	{
		ZPlayFabMatchmaking.JoinCode = UnityEngine.Random.Range(0, (int)Math.Pow(10.0, 6.0)).ToString("D" + 6U.ToString());
		this.m_serverData.joinCode = ZPlayFabMatchmaking.JoinCode;
	}

	// Token: 0x0600107E RID: 4222 RVA: 0x00079298 File Offset: 0x00077498
	private void RegenerateLobbyJoinCode()
	{
		this.GenerateJoinCode();
		UpdateLobbyRequest updateLobbyRequest = new UpdateLobbyRequest();
		updateLobbyRequest.LobbyId = this.m_serverData.lobbyId;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary["string_key4"] = ZPlayFabMatchmaking.JoinCode;
		updateLobbyRequest.SearchData = dictionary;
		PlayFabMultiplayerAPI.UpdateLobby(updateLobbyRequest, new Action<LobbyEmptyResult>(this.OnSetLobbyJoinCodeSuccess), delegate(PlayFabError error)
		{
			this.OnFailed("set lobby join-code", error);
		}, null, null);
	}

	// Token: 0x0600107F RID: 4223 RVA: 0x000792FB File Offset: 0x000774FB
	private void OnSetLobbyJoinCodeSuccess(LobbyEmptyResult _)
	{
		this.CheckJoinCodeIsUnique();
	}

	// Token: 0x06001080 RID: 4224 RVA: 0x00079303 File Offset: 0x00077503
	private void CheckJoinCodeIsUnique()
	{
		PlayFabMultiplayerAPI.FindLobbies(new FindLobbiesRequest
		{
			Filter = string.Format("{0} eq '{1}'", "string_key4", ZPlayFabMatchmaking.JoinCode)
		}, new Action<FindLobbiesResult>(this.OnCheckJoinCodeSuccess), delegate(PlayFabError error)
		{
			this.OnFailed("find lobbies", error);
		}, null, null);
	}

	// Token: 0x06001081 RID: 4225 RVA: 0x00079343 File Offset: 0x00077543
	private void ScheduleJoinCodeCheck()
	{
		this.m_retryIn = 1f;
	}

	// Token: 0x06001082 RID: 4226 RVA: 0x00079350 File Offset: 0x00077550
	private void OnCheckJoinCodeSuccess(FindLobbiesResult result)
	{
		if (result.Lobbies.Count == 0)
		{
			if (this.m_retries > 0)
			{
				this.m_retries--;
				ZLog.Log("Retry join-code check " + this.m_retries.ToString());
				this.ScheduleJoinCodeCheck();
				return;
			}
			ZLog.LogWarning("Zero lobbies returned, should be at least one");
			this.UnregisterServer();
			return;
		}
		else
		{
			if (result.Lobbies.Count == 1 && result.Lobbies[0].Owner.Id == ZPlayFabMatchmaking.GetEntityKeyForLocalUser().Id)
			{
				this.ActivateSession();
				return;
			}
			this.OnSessionUpdated(ZPlayFabMatchmaking.State.RegenerateJoinCode);
			return;
		}
	}

	// Token: 0x06001083 RID: 4227 RVA: 0x000793F8 File Offset: 0x000775F8
	private void ActivateSession()
	{
		UpdateLobbyRequest updateLobbyRequest = new UpdateLobbyRequest();
		updateLobbyRequest.LobbyId = this.m_serverData.lobbyId;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary["string_key2"] = true.ToString();
		updateLobbyRequest.SearchData = dictionary;
		PlayFabMultiplayerAPI.UpdateLobby(updateLobbyRequest, new Action<LobbyEmptyResult>(this.OnActivateLobbySuccess), delegate(PlayFabError error)
		{
			this.OnFailed("activate lobby", error);
		}, null, null);
		this.SetPlatformMatchmakingData();
	}

	// Token: 0x06001084 RID: 4228 RVA: 0x0007945F File Offset: 0x0007765F
	private void OnActivateLobbySuccess(LobbyEmptyResult _)
	{
		this.OnSessionUpdated(ZPlayFabMatchmaking.State.Active);
	}

	// Token: 0x06001085 RID: 4229 RVA: 0x00079468 File Offset: 0x00077668
	public void RegisterServer(string name, bool havePassword, bool isCommunityServer, GameVersion gameVersion, List<string> modifiers, uint networkVersion, string worldName, bool needServerAccount = true)
	{
		bool flag = false;
		if (!PlayFabMultiplayerAPI.IsEntityLoggedIn())
		{
			ZLog.LogWarning("Calling ZPlayFabMatchmaking.RegisterServer() without logged in user");
			this.m_pendingRegisterServer = delegate()
			{
				this.RegisterServer(name, havePassword, isCommunityServer, gameVersion, modifiers, networkVersion, worldName, needServerAccount);
			};
			return;
		}
		this.m_serverData = new PlayFabMatchmakingServerData
		{
			havePassword = havePassword,
			isCommunityServer = isCommunityServer,
			isDedicatedServer = flag,
			remotePlayerId = PlayFabManager.instance.Entity.Id,
			serverName = name,
			gameVersion = gameVersion,
			modifiers = modifiers,
			networkVersion = networkVersion,
			worldName = worldName
		};
		this.m_serverData.serverIp = this.GetServerIPAndPort();
		this.UpdateNumPlayers("New session");
		ZLog.Log(string.Format("Register PlayFab server \"{0}\"{1}", name, flag ? (" with IP " + this.m_serverData.serverIp) : ""));
		this.m_serverData.platformUserID = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
		this.GenerateJoinCode();
		this.CreateAndJoinNetwork();
		PlayFabMultiplayerManager playFabMultiplayerManager = PlayFabMultiplayerManager.Get();
		playFabMultiplayerManager.OnNetworkJoined -= this.OnNetworkJoined;
		playFabMultiplayerManager.OnNetworkJoined += this.OnNetworkJoined;
		playFabMultiplayerManager.OnNetworkChanged -= this.OnNetworkChanged;
		playFabMultiplayerManager.OnNetworkChanged += this.OnNetworkChanged;
		playFabMultiplayerManager.OnError -= this.OnNetworkError;
		playFabMultiplayerManager.OnError += this.OnNetworkError;
		playFabMultiplayerManager.OnRemotePlayerJoined -= this.OnRemotePlayerJoined;
		playFabMultiplayerManager.OnRemotePlayerJoined += this.OnRemotePlayerJoined;
		playFabMultiplayerManager.OnRemotePlayerLeft -= this.OnRemotePlayerLeft;
		playFabMultiplayerManager.OnRemotePlayerLeft += this.OnRemotePlayerLeft;
	}

	// Token: 0x06001086 RID: 4230 RVA: 0x00079690 File Offset: 0x00077890
	private string GetServerIPAndPort()
	{
		if (!this.m_serverData.isDedicatedServer || string.IsNullOrEmpty(ZPlayFabMatchmaking.PublicIP))
		{
			return "";
		}
		if (ZPlayFabMatchmaking.PublicIP.Contains(":"))
		{
			Debug.Log(string.Format("Likely an IPV6 address, returning [{0}]:{1}", ZPlayFabMatchmaking.PublicIP, this.m_serverPort));
			return string.Format("[{0}]:{1}", ZPlayFabMatchmaking.PublicIP, this.m_serverPort);
		}
		Debug.Log(string.Format("IPv4, returning {0}:{1}", ZPlayFabMatchmaking.PublicIP, this.m_serverPort));
		return string.Format("{0}:{1}", ZPlayFabMatchmaking.PublicIP, this.m_serverPort);
	}

	// Token: 0x06001087 RID: 4231 RVA: 0x00079740 File Offset: 0x00077940
	private bool IsIPv6(string address)
	{
		return true;
	}

	// Token: 0x06001088 RID: 4232 RVA: 0x00079744 File Offset: 0x00077944
	public static void LookupPublicIP()
	{
		if (string.IsNullOrEmpty(ZPlayFabMatchmaking.PublicIP) && ZPlayFabMatchmaking.m_publicIpLookupThread == null)
		{
			ZPlayFabMatchmaking.m_publicIpLookupThread = new Thread(new ParameterizedThreadStart(ZPlayFabMatchmaking.BackgroundLookupPublicIP));
			ZPlayFabMatchmaking.m_publicIpLookupThread.Name = "PlayfabLooupThread";
			ZPlayFabMatchmaking.m_publicIpLookupThread.Start();
		}
	}

	// Token: 0x06001089 RID: 4233 RVA: 0x00079793 File Offset: 0x00077993
	private static void BackgroundLookupPublicIP(object obj)
	{
		while (string.IsNullOrEmpty(ZPlayFabMatchmaking.PublicIP))
		{
			ZPlayFabMatchmaking.PublicIP = ZNet.GetPublicIP(ZPlayFabMatchmaking.m_getPublicIpAttempts++);
			Thread.Sleep(10);
		}
	}

	// Token: 0x0600108A RID: 4234 RVA: 0x000797C4 File Offset: 0x000779C4
	private void CreateAndJoinNetwork()
	{
		PlayFabNetworkConfiguration networkConfiguration = new PlayFabNetworkConfiguration
		{
			MaxPlayerCount = 10U,
			DirectPeerConnectivityOptions = (PARTY_DIRECT_PEER_CONNECTIVITY_OPTIONS)15U
		};
		ZLog.Log(string.Format("Server '{0}' begin PlayFab create and join network for server ", this.m_serverData.serverName));
		PlayFabMultiplayerManager.Get().CreateAndJoinNetwork(networkConfiguration);
		this.m_isConnectingToNetwork = true;
		this.StartReconnectNetworkTimer(-1);
	}

	// Token: 0x0600108B RID: 4235 RVA: 0x0007981C File Offset: 0x00077A1C
	public void UnregisterServer()
	{
		Debug.Log("ZPlayFabMatchmaking::UnregisterServer - unregistering server now. State: " + this.m_state.ToString());
		if (this.m_state != ZPlayFabMatchmaking.State.Uninitialized)
		{
			ZLog.Log(string.Format("Unregister PlayFab server \"{0}\" and leaving network \"{1}\"", this.m_serverData.serverName, this.m_serverData.networkId));
			this.DeleteLobby(this.m_serverData.lobbyId);
			ZPlayFabSocket.DestroyListenSocket();
			PlayFabMultiplayerManager.Get().LeaveNetwork();
			PlayFabMultiplayerManager.Get().OnNetworkJoined -= this.OnNetworkJoined;
			PlayFabMultiplayerManager.Get().OnNetworkChanged -= this.OnNetworkChanged;
			PlayFabMultiplayerManager.Get().OnError -= this.OnNetworkError;
			PlayFabMultiplayerManager.Get().OnRemotePlayerJoined -= this.OnRemotePlayerJoined;
			PlayFabMultiplayerManager.Get().OnRemotePlayerLeft -= this.OnRemotePlayerLeft;
			this.m_serverData = null;
			this.m_retries = 0;
			this.m_state = ZPlayFabMatchmaking.State.Uninitialized;
			this.StopReconnectNetworkTimer();
			return;
		}
		ZPlayFabMatchmakeLobbyLeftCallback lobbyLeft = ZPlayFabMatchmaking.LobbyLeft;
		if (lobbyLeft == null)
		{
			return;
		}
		lobbyLeft(true);
	}

	// Token: 0x0600108C RID: 4236 RVA: 0x00079933 File Offset: 0x00077B33
	internal static void ResetParty()
	{
		if (ZPlayFabMatchmaking.instance != null && ZPlayFabMatchmaking.instance.IsJoinedToNetwork())
		{
			ZPlayFabMatchmaking.instance.OnNetworkError(null, new PlayFabMultiplayerManagerErrorArgs(9999, "Forced ResetParty", PlayFabMultiplayerManagerErrorType.Error));
			return;
		}
		ZLog.Log("No active PlayFab Party to reset");
	}

	// Token: 0x0600108D RID: 4237 RVA: 0x00079970 File Offset: 0x00077B70
	private void OnNetworkError(object sender, PlayFabMultiplayerManagerErrorArgs args)
	{
		if (this.IsReconnectNetworkTimerActive())
		{
			return;
		}
		ZLog.LogWarning(string.Format("PlayFab network error in session '{0}' and network {1} with type '{2}' and code '{3}': {4}", new object[]
		{
			this.m_serverData.serverName,
			this.m_serverData.networkId,
			args.Type,
			args.Code,
			args.Message
		}));
		this.StartReconnectNetworkTimer(args.Code);
	}

	// Token: 0x0600108E RID: 4238 RVA: 0x000799E8 File Offset: 0x00077BE8
	private void OnNetworkChanged(object sender, string newNetworkId)
	{
		ZLog.LogWarning(string.Format("PlayFab network session '{0}' and network {1} changed to network {2}", this.m_serverData.serverName, this.m_serverData.networkId, newNetworkId));
		this.m_serverData.networkId = newNetworkId;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string key = PlayFabAttrKey.NetworkId.ToKeyString();
		dictionary[key] = this.m_serverData.networkId;
		Dictionary<string, string> lobbyData = dictionary;
		PlayFabMultiplayerAPI.UpdateLobby(new UpdateLobbyRequest
		{
			LobbyId = this.m_serverData.lobbyId,
			LobbyData = lobbyData
		}, delegate(LobbyEmptyResult _)
		{
			ZLog.Log(string.Format("Lobby {0} for world '{1}' change to network {2}", this.m_serverData.lobbyId, this.m_serverData.serverName, this.m_serverData.networkId));
		}, new Action<PlayFabError>(this.OnRefreshFailed), null, null);
	}

	// Token: 0x0600108F RID: 4239 RVA: 0x00079A84 File Offset: 0x00077C84
	private void DeleteLobby(string lobbyId)
	{
		UpdateLobbyRequest updateLobbyRequest = new UpdateLobbyRequest();
		updateLobbyRequest.LobbyId = lobbyId;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary["string_key2"] = false.ToString();
		updateLobbyRequest.SearchData = dictionary;
		PlayFabMultiplayerAPI.UpdateLobby(updateLobbyRequest, delegate(LobbyEmptyResult _)
		{
			ZLog.Log("Deactivated PlayFab lobby " + lobbyId);
		}, delegate(PlayFabError error)
		{
			ZLog.LogWarning(string.Format("Failed to deactive lobby '{0}': {1}", lobbyId, error.GenerateErrorReport()));
		}, null, null);
		ZPlayFabMatchmaking.LeaveLobby(lobbyId);
		this.ClearPlatformMatchmakingData();
	}

	// Token: 0x06001090 RID: 4240 RVA: 0x00079B00 File Offset: 0x00077D00
	public static void LeaveLobby(string lobbyId)
	{
		PlayFabMultiplayerAPI.LeaveLobby(new LeaveLobbyRequest
		{
			LobbyId = lobbyId,
			MemberEntity = ZPlayFabMatchmaking.GetEntityKeyForLocalUser()
		}, delegate(LobbyEmptyResult _)
		{
			ZLog.Log("Left PlayFab lobby " + lobbyId);
			ZPlayFabMatchmakeLobbyLeftCallback lobbyLeft = ZPlayFabMatchmaking.LobbyLeft;
			if (lobbyLeft == null)
			{
				return;
			}
			lobbyLeft(true);
		}, delegate(PlayFabError error)
		{
			ZLog.LogError(string.Format("Failed to leave lobby '{0}': {1}", lobbyId, error.GenerateErrorReport()));
			ZPlayFabMatchmakeLobbyLeftCallback lobbyLeft = ZPlayFabMatchmaking.LobbyLeft;
			if (lobbyLeft == null)
			{
				return;
			}
			lobbyLeft(false);
		}, null, null);
	}

	// Token: 0x06001091 RID: 4241 RVA: 0x00079B55 File Offset: 0x00077D55
	public static void LeaveEmptyLobby()
	{
		ZPlayFabMatchmakeLobbyLeftCallback lobbyLeft = ZPlayFabMatchmaking.LobbyLeft;
		if (lobbyLeft == null)
		{
			return;
		}
		lobbyLeft(true);
	}

	// Token: 0x06001092 RID: 4242 RVA: 0x00079B68 File Offset: 0x00077D68
	public static void ResolveJoinCode(string joinCode, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction)
	{
		string searchFilter = string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key4",
			joinCode,
			"string_key2",
			true.ToString()
		});
		ZPlayFabMatchmaking.instance.m_activeSearches.Add(new ZPlayFabLobbySearch(successAction, failedAction, searchFilter, null, false));
	}

	// Token: 0x06001093 RID: 4243 RVA: 0x00079BC0 File Offset: 0x00077DC0
	public static void CheckHostOnlineStatus(string hostName, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, bool joinLobby = false)
	{
		ZPlayFabMatchmaking.FindHostSession(string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key1",
			hostName,
			"string_key2",
			true.ToString()
		}), successAction, failedAction, joinLobby);
	}

	// Token: 0x06001094 RID: 4244 RVA: 0x00079C08 File Offset: 0x00077E08
	public static void FindHostByIp(IPEndPoint hostIp, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, bool joinLobby = false)
	{
		if (!hostIp.IsPublic)
		{
			if (failedAction != null)
			{
				failedAction(ZPLayFabMatchmakingFailReason.EndPointNotOnInternet);
			}
			return;
		}
		ZPlayFabMatchmaking.FindHostSession(string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key10",
			hostIp,
			"string_key2",
			true.ToString()
		}), successAction, failedAction, joinLobby);
	}

	// Token: 0x06001095 RID: 4245 RVA: 0x00079C68 File Offset: 0x00077E68
	private static Dictionary<char, int> CreateCharHistogram(string str)
	{
		Dictionary<char, int> dictionary = new Dictionary<char, int>();
		foreach (char c in str.ToLowerInvariant())
		{
			if (dictionary.ContainsKey(c))
			{
				Dictionary<char, int> dictionary2 = dictionary;
				char key = c;
				int num = dictionary2[key];
				dictionary2[key] = num + 1;
			}
			else
			{
				dictionary.Add(c, 1);
			}
		}
		return dictionary;
	}

	// Token: 0x06001096 RID: 4246 RVA: 0x00079CC8 File Offset: 0x00077EC8
	private static void AddNameSearchFilter(Dictionary<string, string> searchData, string serverName)
	{
		Dictionary<char, int> dictionary = ZPlayFabMatchmaking.CreateCharHistogram(serverName);
		for (char c = 'a'; c <= 'z'; c += '\u0001')
		{
			string key;
			if (ZPlayFabMatchmaking.CharToKeyName(c, out key))
			{
				int num;
				dictionary.TryGetValue(c, out num);
				searchData.Add(key, num.ToString());
			}
		}
	}

	// Token: 0x06001097 RID: 4247 RVA: 0x00079D10 File Offset: 0x00077F10
	private static string CreateNameSearchFilter(string name)
	{
		Dictionary<char, int> dictionary = ZPlayFabMatchmaking.CreateCharHistogram(name);
		string text = "";
		foreach (char c in name.ToLowerInvariant())
		{
			string arg;
			int num;
			if (ZPlayFabMatchmaking.CharToKeyName(c, out arg) && dictionary.TryGetValue(c, out num))
			{
				text += string.Format(" and {0} ge {1}", arg, num);
			}
		}
		return text;
	}

	// Token: 0x06001098 RID: 4248 RVA: 0x00079D80 File Offset: 0x00077F80
	private static bool CharToKeyName(char ch, out string key)
	{
		int num = "eariotnslcudpmhgbfywkvxzjq".IndexOf(ch);
		if (num < 0 || num >= 16)
		{
			key = null;
			return false;
		}
		key = string.Format("number_key{0}", num + 14 + 1);
		return true;
	}

	// Token: 0x06001099 RID: 4249 RVA: 0x00079DC0 File Offset: 0x00077FC0
	private void CancelPendingSearches()
	{
		foreach (ZPlayFabLobbySearch zplayFabLobbySearch in ZPlayFabMatchmaking.instance.m_activeSearches)
		{
			zplayFabLobbySearch.Cancel();
		}
		this.m_pendingSearches.Clear();
	}

	// Token: 0x0600109A RID: 4250 RVA: 0x00079E20 File Offset: 0x00078020
	private static void FindHostSession(string searchFilter, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, bool joinLobby)
	{
		if (joinLobby)
		{
			ZPlayFabMatchmaking.instance.CancelPendingSearches();
			ZPlayFabMatchmaking.instance.m_activeSearches.Add(new ZPlayFabLobbySearch(successAction, failedAction, searchFilter, true));
			return;
		}
		ZPlayFabMatchmaking.instance.m_pendingSearches.Enqueue(new ZPlayFabLobbySearch(successAction, failedAction, searchFilter, false));
	}

	// Token: 0x0600109B RID: 4251 RVA: 0x00079E60 File Offset: 0x00078060
	public static void ListServers(string nameFilter, ZPlayFabMatchmakingSuccessCallback serverFoundAction, ZPlayFabMatchmakingFailedCallback listDone, bool listP2P = false)
	{
		ZPlayFabMatchmaking.instance.CancelPendingSearches();
		string text = listP2P ? string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key7",
			false.ToString(),
			"string_key2",
			true.ToString()
		}) : string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key3",
			true.ToString(),
			"string_key2",
			true.ToString()
		});
		if (string.IsNullOrEmpty(nameFilter))
		{
			text += string.Format(" and {0} eq {1}", "number_key13", 34U);
		}
		else
		{
			text += ZPlayFabMatchmaking.CreateNameSearchFilter(nameFilter);
		}
		if (listP2P)
		{
			foreach (PlatformUserID friend in PlatformManager.DistributionPlatform.RelationsProvider.GetFriends())
			{
				string searchFilter = ZPlayFabMatchmaking.CreateSearchFilter(text, friend, PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) == PrivilegeResult.Granted);
				ZPlayFabMatchmaking.instance.m_pendingSearches.Enqueue(new ZPlayFabLobbySearch(serverFoundAction, listDone, searchFilter, null, true));
			}
			return;
		}
		string searchFilter2 = ZPlayFabMatchmaking.CreateSearchFilter(text, default(PlatformUserID), PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) == PrivilegeResult.Granted);
		ZPlayFabMatchmaking.instance.m_pendingSearches.Enqueue(new ZPlayFabLobbySearch(serverFoundAction, listDone, searchFilter2, nameFilter, false));
	}

	// Token: 0x0600109C RID: 4252 RVA: 0x00079FC4 File Offset: 0x000781C4
	private static string CreateSearchFilter(string baseFilter, PlatformUserID friend = default(PlatformUserID), bool isCrossplay = false)
	{
		string text = isCrossplay ? "None" : PlatformManager.DistributionPlatform.Platform.ToString();
		return string.Concat(new string[]
		{
			baseFilter,
			friend.IsValid ? string.Format(" and {0} eq '{1}'", "string_key8", friend) : "",
			" and string_key12 eq '",
			text,
			"'"
		});
	}

	// Token: 0x0600109D RID: 4253 RVA: 0x0007A040 File Offset: 0x00078240
	public static bool IsJoinCode(string joinString)
	{
		int num;
		return (long)joinString.Length == 6L && int.TryParse(joinString, out num);
	}

	// Token: 0x0600109E RID: 4254 RVA: 0x0007A062 File Offset: 0x00078262
	public static void SetDataPort(int serverPort)
	{
		if (ZPlayFabMatchmaking.instance != null)
		{
			ZPlayFabMatchmaking.instance.m_serverPort = serverPort;
		}
	}

	// Token: 0x0600109F RID: 4255 RVA: 0x0007A076 File Offset: 0x00078276
	public static void OnLogin()
	{
		if (ZPlayFabMatchmaking.instance != null && ZPlayFabMatchmaking.instance.m_pendingRegisterServer != null)
		{
			ZPlayFabMatchmaking.instance.m_pendingRegisterServer();
			ZPlayFabMatchmaking.instance.m_pendingRegisterServer = null;
		}
	}

	// Token: 0x060010A0 RID: 4256 RVA: 0x0007A0A5 File Offset: 0x000782A5
	internal static void ForwardProgress()
	{
		if (ZPlayFabMatchmaking.instance != null)
		{
			ZPlayFabMatchmaking.instance.StopReconnectNetworkTimer();
		}
	}

	// Token: 0x04000FA5 RID: 4005
	private static ZPlayFabMatchmaking m_instance;

	// Token: 0x04000FA7 RID: 4007
	private static string m_publicIP = "";

	// Token: 0x04000FA8 RID: 4008
	private static readonly object m_mtx = new object();

	// Token: 0x04000FA9 RID: 4009
	private static Thread m_publicIpLookupThread;

	// Token: 0x04000FAA RID: 4010
	private static int m_getPublicIpAttempts;

	// Token: 0x04000FAB RID: 4011
	public const uint JoinStringLength = 6U;

	// Token: 0x04000FAC RID: 4012
	public const uint MaxPlayers = 10U;

	// Token: 0x04000FAD RID: 4013
	internal const int NumSearchPages = 4;

	// Token: 0x04000FAE RID: 4014
	public const string RemotePlayerIdSearchKey = "string_key1";

	// Token: 0x04000FAF RID: 4015
	public const string IsActiveSearchKey = "string_key2";

	// Token: 0x04000FB0 RID: 4016
	public const string IsCommunityServerSearchKey = "string_key3";

	// Token: 0x04000FB1 RID: 4017
	public const string JoinCodeSearchKey = "string_key4";

	// Token: 0x04000FB2 RID: 4018
	public const string ServerNameSearchKey = "string_key5";

	// Token: 0x04000FB3 RID: 4019
	public const string GameVersionSearchKey = "string_key6";

	// Token: 0x04000FB4 RID: 4020
	public const string IsDedicatedServerSearchKey = "string_key7";

	// Token: 0x04000FB5 RID: 4021
	public const string PlatformUserIdSearchKey = "string_key8";

	// Token: 0x04000FB6 RID: 4022
	public const string CreatedSearchKey = "string_key9";

	// Token: 0x04000FB7 RID: 4023
	public const string ServerIpSearchKey = "string_key10";

	// Token: 0x04000FB8 RID: 4024
	public const string PageSearchKey = "number_key11";

	// Token: 0x04000FB9 RID: 4025
	public const string PlatformRestrictionKey = "string_key12";

	// Token: 0x04000FBA RID: 4026
	public const string NetworkVersionSearchKey = "number_key13";

	// Token: 0x04000FBB RID: 4027
	public const string ModifiersSearchKey = "string_key14";

	// Token: 0x04000FBC RID: 4028
	private const int NumStringSearchKeys = 14;

	// Token: 0x04000FBD RID: 4029
	private ZPlayFabMatchmaking.State m_state;

	// Token: 0x04000FBE RID: 4030
	private PlayFabMatchmakingServerData m_serverData;

	// Token: 0x04000FBF RID: 4031
	private int m_retries;

	// Token: 0x04000FC0 RID: 4032
	private float m_retryIn = -1f;

	// Token: 0x04000FC1 RID: 4033
	private const float LostNetworkRetryDuration = 30f;

	// Token: 0x04000FC2 RID: 4034
	private float m_lostNetworkRetryIn = -1f;

	// Token: 0x04000FC3 RID: 4035
	private bool m_isConnectingToNetwork;

	// Token: 0x04000FC4 RID: 4036
	private bool m_isResettingNetwork;

	// Token: 0x04000FC5 RID: 4037
	private float m_submitBackgroundSearchIn = -1f;

	// Token: 0x04000FC6 RID: 4038
	private int m_serverPort = -1;

	// Token: 0x04000FC7 RID: 4039
	private float m_refreshLobbyTimer;

	// Token: 0x04000FC8 RID: 4040
	private const float RefreshLobbyDurationMin = 540f;

	// Token: 0x04000FC9 RID: 4041
	private const float RefreshLobbyDurationMax = 840f;

	// Token: 0x04000FCA RID: 4042
	private const float DurationBetwenBackgroundSearches = 2f;

	// Token: 0x04000FCB RID: 4043
	private readonly List<ZPlayFabLobbySearch> m_activeSearches = new List<ZPlayFabLobbySearch>();

	// Token: 0x04000FCC RID: 4044
	private readonly Queue<ZPlayFabLobbySearch> m_pendingSearches = new Queue<ZPlayFabLobbySearch>();

	// Token: 0x04000FCD RID: 4045
	private Action m_pendingRegisterServer;

	// Token: 0x02000304 RID: 772
	private enum State
	{
		// Token: 0x04002397 RID: 9111
		Uninitialized,
		// Token: 0x04002398 RID: 9112
		Creating,
		// Token: 0x04002399 RID: 9113
		RegenerateJoinCode,
		// Token: 0x0400239A RID: 9114
		Active
	}
}
