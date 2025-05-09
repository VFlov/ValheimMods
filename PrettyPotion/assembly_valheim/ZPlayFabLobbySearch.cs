using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.MultiplayerModels;
using Splatform;

// Token: 0x02000102 RID: 258
internal class ZPlayFabLobbySearch
{
	// Token: 0x17000090 RID: 144
	// (get) Token: 0x0600103B RID: 4155 RVA: 0x00077C45 File Offset: 0x00075E45
	// (set) Token: 0x0600103C RID: 4156 RVA: 0x00077C4D File Offset: 0x00075E4D
	internal bool IsDone { get; private set; }

	// Token: 0x0600103D RID: 4157 RVA: 0x00077C58 File Offset: 0x00075E58
	internal ZPlayFabLobbySearch(ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, string searchFilter, string serverFilter, bool findFriendsOnly = false)
	{
		this.m_successAction = successAction;
		this.m_failedAction = failedAction;
		this.m_searchFilter = searchFilter;
		this.m_serverFilter = serverFilter;
		this.m_findFriendsOnly = findFriendsOnly;
		if (serverFilter == null)
		{
			this.FindLobby();
			this.m_retries = 1;
			return;
		}
		this.m_pages = this.CreatePages();
	}

	// Token: 0x0600103E RID: 4158 RVA: 0x00077CD0 File Offset: 0x00075ED0
	internal ZPlayFabLobbySearch(ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, string searchFilter, bool joinLobby)
	{
		this.m_successAction = successAction;
		this.m_failedAction = failedAction;
		this.m_searchFilter = searchFilter;
		this.m_joinLobby = joinLobby;
		if (joinLobby)
		{
			this.FindLobby();
			this.m_retries = 3;
		}
	}

	// Token: 0x0600103F RID: 4159 RVA: 0x00077D34 File Offset: 0x00075F34
	private Queue<int> CreatePages()
	{
		Queue<int> queue = new Queue<int>();
		for (int i = 0; i < 4; i++)
		{
			queue.Enqueue(i);
		}
		return queue;
	}

	// Token: 0x06001040 RID: 4160 RVA: 0x00077D5B File Offset: 0x00075F5B
	internal void Update(float deltaTime)
	{
		if (this.m_retryIn > 0f)
		{
			this.m_retryIn -= deltaTime;
			if (this.m_retryIn <= 0f)
			{
				this.FindLobby();
			}
		}
		this.TickAPICallRateLimiter();
	}

	// Token: 0x06001041 RID: 4161 RVA: 0x00077D94 File Offset: 0x00075F94
	internal void FindLobby()
	{
		if (this.m_serverFilter == null)
		{
			FindLobbiesRequest request = new FindLobbiesRequest
			{
				Filter = this.m_searchFilter
			};
			this.QueueAPICall(delegate
			{
				PlayFabMultiplayerAPI.FindLobbies(request, new Action<FindLobbiesResult>(this.OnFindLobbySuccess), new Action<PlayFabError>(this.OnFindLobbyFailed), null, null);
			});
			return;
		}
		this.FindLobbyWithPagination(this.m_pages.Dequeue());
	}

	// Token: 0x06001042 RID: 4162 RVA: 0x00077DF4 File Offset: 0x00075FF4
	private void FindLobbyWithPagination(int page)
	{
		FindLobbiesRequest request = new FindLobbiesRequest
		{
			Filter = this.m_searchFilter + string.Format(" and {0} eq {1}", "number_key11", page),
			Pagination = new PaginationRequest
			{
				PageSizeRequested = new uint?(50U)
			}
		};
		if (this.m_verboseLog)
		{
			ZLog.Log(string.Format("Page {0}, {1} remains: {2}", page, this.m_pages.Count, request.Filter));
		}
		this.QueueAPICall(delegate
		{
			PlayFabMultiplayerAPI.FindLobbies(request, new Action<FindLobbiesResult>(this.OnFindServersSuccess), new Action<PlayFabError>(this.OnFindLobbyFailed), null, null);
		});
	}

	// Token: 0x06001043 RID: 4163 RVA: 0x00077EA4 File Offset: 0x000760A4
	private void RetryOrFail(string error)
	{
		if (this.m_retries > 0)
		{
			this.m_retries--;
			this.m_retryIn = 1f;
			return;
		}
		ZLog.Log(string.Format("PlayFab lobby matching search filter '{0}': {1}", this.m_searchFilter, error));
		this.OnFailed(ZPLayFabMatchmakingFailReason.Unknown);
	}

	// Token: 0x06001044 RID: 4164 RVA: 0x00077EF1 File Offset: 0x000760F1
	private void OnFindLobbyFailed(PlayFabError error)
	{
		if (!this.IsDone)
		{
			this.RetryOrFail(error.ToString());
		}
	}

	// Token: 0x06001045 RID: 4165 RVA: 0x00077F08 File Offset: 0x00076108
	private void OnFindLobbySuccess(FindLobbiesResult result)
	{
		if (this.IsDone)
		{
			return;
		}
		if (result.Lobbies.Count == 0)
		{
			this.RetryOrFail("Got back zero lobbies");
			return;
		}
		LobbySummary lobbySummary = result.Lobbies[0];
		if (result.Lobbies.Count > 1)
		{
			ZLog.LogWarning(string.Format("Expected zero or one lobby got {0} matching lobbies, returning newest lobby", result.Lobbies.Count));
			long num = long.Parse(lobbySummary.SearchData["string_key9"]);
			foreach (LobbySummary lobbySummary2 in result.Lobbies)
			{
				long num2 = long.Parse(lobbySummary2.SearchData["string_key9"]);
				if (num < num2)
				{
					lobbySummary = lobbySummary2;
					num = num2;
				}
			}
		}
		if (this.m_joinLobby)
		{
			this.JoinLobby(lobbySummary.LobbyId, lobbySummary.ConnectionString);
			ZPlayFabMatchmaking.JoinCode = lobbySummary.SearchData["string_key4"];
			return;
		}
		this.DeliverLobby(lobbySummary);
		this.IsDone = true;
	}

	// Token: 0x06001046 RID: 4166 RVA: 0x00078028 File Offset: 0x00076228
	private void JoinLobby(string lobbyId, string connectionString)
	{
		JoinLobbyRequest request = new JoinLobbyRequest
		{
			ConnectionString = connectionString,
			MemberEntity = ZPlayFabMatchmaking.GetEntityKeyForLocalUser()
		};
		Action<JoinLobbyResult> <>9__1;
		Action<PlayFabError> <>9__2;
		this.QueueAPICall(delegate
		{
			JoinLobbyRequest request = request;
			Action<JoinLobbyResult> resultCallback;
			if ((resultCallback = <>9__1) == null)
			{
				resultCallback = (<>9__1 = delegate(JoinLobbyResult result)
				{
					this.OnJoinLobbySuccess(result.LobbyId);
				});
			}
			Action<PlayFabError> errorCallback;
			if ((errorCallback = <>9__2) == null)
			{
				errorCallback = (<>9__2 = delegate(PlayFabError error)
				{
					this.OnJoinLobbyFailed(error, lobbyId);
				});
			}
			PlayFabMultiplayerAPI.JoinLobby(request, resultCallback, errorCallback, null, null);
		});
	}

	// Token: 0x06001047 RID: 4167 RVA: 0x00078078 File Offset: 0x00076278
	private void OnJoinLobbySuccess(string lobbyId)
	{
		if (this.IsDone)
		{
			return;
		}
		GetLobbyRequest request = new GetLobbyRequest
		{
			LobbyId = lobbyId
		};
		this.QueueAPICall(delegate
		{
			PlayFabMultiplayerAPI.GetLobby(request, new Action<GetLobbyResult>(this.OnGetLobbySuccess), new Action<PlayFabError>(this.OnGetLobbyFailed), null, null);
		});
	}

	// Token: 0x06001048 RID: 4168 RVA: 0x000780C0 File Offset: 0x000762C0
	private void OnJoinLobbyFailed(PlayFabError error, string lobbyId)
	{
		PlayFabErrorCode error2 = error.Error;
		if (error2 <= PlayFabErrorCode.APIClientRequestRateLimitExceeded)
		{
			if (error2 != PlayFabErrorCode.APIRequestLimitExceeded && error2 != PlayFabErrorCode.APIClientRequestRateLimitExceeded)
			{
				goto IL_5D;
			}
		}
		else
		{
			if (error2 == PlayFabErrorCode.LobbyPlayerAlreadyJoined)
			{
				this.OnJoinLobbySuccess(lobbyId);
				return;
			}
			if (error2 == PlayFabErrorCode.LobbyNotJoinable)
			{
				ZLog.Log("Can't join lobby because it's not joinable, likely because it's full.");
				this.OnFailed(ZPLayFabMatchmakingFailReason.ServerFull);
				return;
			}
			if (error2 != PlayFabErrorCode.LobbyPlayerMaxLobbyLimitExceeded)
			{
				goto IL_5D;
			}
		}
		this.OnFailed(ZPLayFabMatchmakingFailReason.APIRequestLimitExceeded);
		return;
		IL_5D:
		ZLog.LogError("Failed to get lobby: " + error.ToString());
		this.OnFailed(ZPLayFabMatchmakingFailReason.Unknown);
	}

	// Token: 0x06001049 RID: 4169 RVA: 0x00078148 File Offset: 0x00076348
	private void DeliverLobby(LobbySummary lobbySummary)
	{
		PlayFabMatchmakingServerData playFabMatchmakingServerData = ZPlayFabLobbySearch.ToServerData(lobbySummary.LobbyId, lobbySummary.CurrentPlayers, lobbySummary.SearchData, null, true);
		if (this.m_verboseLog && playFabMatchmakingServerData != null)
		{
			ZLog.Log("Deliver server data\n" + playFabMatchmakingServerData.ToString());
		}
		this.m_successAction(playFabMatchmakingServerData);
		if (this.m_findFriendsOnly)
		{
			this.m_failedAction(ZPLayFabMatchmakingFailReason.None);
		}
	}

	// Token: 0x0600104A RID: 4170 RVA: 0x000781B0 File Offset: 0x000763B0
	private void OnFindServersSuccess(FindLobbiesResult result)
	{
		if (this.IsDone)
		{
			return;
		}
		foreach (LobbySummary lobbySummary in result.Lobbies)
		{
			if (lobbySummary.SearchData["string_key5"].ToLowerInvariant().Contains(this.m_serverFilter.ToLowerInvariant()))
			{
				this.DeliverLobby(lobbySummary);
			}
		}
		if (this.m_pages.Count == 0)
		{
			this.OnFailed(ZPLayFabMatchmakingFailReason.None);
			return;
		}
		this.FindLobbyWithPagination(this.m_pages.Dequeue());
	}

	// Token: 0x0600104B RID: 4171 RVA: 0x0007825C File Offset: 0x0007645C
	private void OnGetLobbySuccess(GetLobbyResult result)
	{
		PlayFabMatchmakingServerData playFabMatchmakingServerData = ZPlayFabLobbySearch.ToServerData(result);
		if (this.IsDone)
		{
			this.OnFailed(ZPLayFabMatchmakingFailReason.Cancelled);
			return;
		}
		if (playFabMatchmakingServerData == null)
		{
			this.OnFailed(ZPLayFabMatchmakingFailReason.InvalidServerData);
			return;
		}
		this.IsDone = true;
		ZLog.Log("Get Lobby\n" + playFabMatchmakingServerData.ToString());
		this.m_successAction(playFabMatchmakingServerData);
	}

	// Token: 0x0600104C RID: 4172 RVA: 0x000782B3 File Offset: 0x000764B3
	private void OnGetLobbyFailed(PlayFabError error)
	{
		ZLog.LogError("Failed to get lobby: " + error.ToString());
		this.OnFailed(ZPLayFabMatchmakingFailReason.Unknown);
	}

	// Token: 0x0600104D RID: 4173 RVA: 0x000782D4 File Offset: 0x000764D4
	private static PlayFabMatchmakingServerData ToServerData(string lobbyID, uint playerCount, Dictionary<string, string> searchData, Dictionary<string, string> lobbyData = null, bool subtractOneFromPlayerCountIfDedicated = false)
	{
		PlayFabMatchmakingServerData result;
		try
		{
			bool isCommunityServer;
			bool flag;
			long tickCreated;
			if (!bool.TryParse(searchData["string_key3"], out isCommunityServer) || !bool.TryParse(searchData["string_key7"], out flag) || !long.TryParse(searchData["string_key9"], out tickCreated))
			{
				ZLog.LogWarning("Got PlayFab lobby entry with invalid data");
				result = null;
			}
			else
			{
				string versionString = searchData["string_key6"];
				uint num = uint.Parse(searchData["number_key13"]);
				GameVersion gameVersion;
				if (!GameVersion.TryParseGameVersion(versionString, out gameVersion) || gameVersion < global::Version.FirstVersionWithNetworkVersion)
				{
					num = 0U;
				}
				string encodedString;
				Dictionary<string, string> kvps;
				List<string> modifiers;
				if (num != 34U || !searchData.TryGetValue("string_key14", out encodedString) || !StringUtils.TryDecodeStringAsIDictionary<Dictionary<string, string>>(encodedString, out kvps) || !ServerOptionsGUI.TryConvertCompactKVPToModifierKeys<List<string>>(kvps, out modifiers))
				{
					modifiers = new List<string>();
				}
				string text = searchData["string_key8"];
				PlatformUserID platformUserID;
				if (text.Contains('_'))
				{
					platformUserID = new PlatformUserID(text);
				}
				else
				{
					platformUserID = new PlatformUserID(new Platform("Steam"), text);
				}
				PlayFabMatchmakingServerData playFabMatchmakingServerData = new PlayFabMatchmakingServerData
				{
					isCommunityServer = isCommunityServer,
					isDedicatedServer = flag,
					joinCode = searchData["string_key4"],
					lobbyId = lobbyID,
					numPlayers = ((flag && subtractOneFromPlayerCountIfDedicated) ? (playerCount - 1U) : playerCount),
					remotePlayerId = searchData["string_key1"],
					serverIp = searchData["string_key10"],
					serverName = searchData["string_key5"],
					tickCreated = tickCreated,
					gameVersion = gameVersion,
					modifiers = modifiers,
					networkVersion = num,
					platformUserID = platformUserID,
					platformRestriction = new Platform(searchData["string_key12"])
				};
				if (lobbyData != null)
				{
					playFabMatchmakingServerData.havePassword = bool.Parse(lobbyData[PlayFabAttrKey.HavePassword.ToKeyString()]);
					playFabMatchmakingServerData.networkId = lobbyData[PlayFabAttrKey.NetworkId.ToKeyString()];
					playFabMatchmakingServerData.worldName = lobbyData[PlayFabAttrKey.WorldName.ToKeyString()];
				}
				result = playFabMatchmakingServerData;
			}
		}
		catch (KeyNotFoundException)
		{
			ZLog.LogWarning("Got PlayFab lobby entry with missing key(s)");
			result = null;
		}
		catch
		{
			result = null;
		}
		return result;
	}

	// Token: 0x0600104E RID: 4174 RVA: 0x0007850C File Offset: 0x0007670C
	private static PlayFabMatchmakingServerData ToServerData(GetLobbyResult result)
	{
		return ZPlayFabLobbySearch.ToServerData(result.Lobby.LobbyId, (uint)result.Lobby.Members.Count, result.Lobby.SearchData, result.Lobby.LobbyData, false);
	}

	// Token: 0x0600104F RID: 4175 RVA: 0x00078545 File Offset: 0x00076745
	private void OnFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		if (!this.IsDone)
		{
			this.IsDone = true;
			if (this.m_failedAction != null)
			{
				this.m_failedAction(failReason);
			}
		}
	}

	// Token: 0x06001050 RID: 4176 RVA: 0x0007856A File Offset: 0x0007676A
	internal void Cancel()
	{
		this.IsDone = true;
	}

	// Token: 0x06001051 RID: 4177 RVA: 0x00078573 File Offset: 0x00076773
	private void QueueAPICall(ZPlayFabLobbySearch.QueueableAPICall apiCallDelegate)
	{
		this.m_APICallQueue.Enqueue(apiCallDelegate);
		this.TickAPICallRateLimiter();
	}

	// Token: 0x06001052 RID: 4178 RVA: 0x00078588 File Offset: 0x00076788
	private void TickAPICallRateLimiter()
	{
		if (this.m_APICallQueue.Count <= 0)
		{
			return;
		}
		if ((DateTime.UtcNow - this.m_previousAPICallTime).TotalSeconds >= 2.0)
		{
			this.m_APICallQueue.Dequeue()();
			this.m_previousAPICallTime = DateTime.UtcNow;
		}
	}

	// Token: 0x04000F81 RID: 3969
	private readonly ZPlayFabMatchmakingSuccessCallback m_successAction;

	// Token: 0x04000F82 RID: 3970
	private readonly ZPlayFabMatchmakingFailedCallback m_failedAction;

	// Token: 0x04000F83 RID: 3971
	private readonly string m_searchFilter;

	// Token: 0x04000F84 RID: 3972
	private readonly string m_serverFilter;

	// Token: 0x04000F85 RID: 3973
	private readonly Queue<int> m_pages;

	// Token: 0x04000F86 RID: 3974
	private readonly bool m_joinLobby;

	// Token: 0x04000F87 RID: 3975
	private readonly bool m_verboseLog;

	// Token: 0x04000F88 RID: 3976
	private readonly bool m_findFriendsOnly;

	// Token: 0x04000F89 RID: 3977
	private int m_retries;

	// Token: 0x04000F8A RID: 3978
	private float m_retryIn = -1f;

	// Token: 0x04000F8C RID: 3980
	private const float rateLimit = 2f;

	// Token: 0x04000F8D RID: 3981
	private DateTime m_previousAPICallTime = DateTime.MinValue;

	// Token: 0x04000F8E RID: 3982
	private Queue<ZPlayFabLobbySearch.QueueableAPICall> m_APICallQueue = new Queue<ZPlayFabLobbySearch.QueueableAPICall>();

	// Token: 0x020002FF RID: 767
	// (Invoke) Token: 0x060021CE RID: 8654
	private delegate void QueueableAPICall();
}
