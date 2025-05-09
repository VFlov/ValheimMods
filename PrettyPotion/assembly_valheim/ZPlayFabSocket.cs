using System;
using System.Collections.Generic;
using PlayFab.Party;
using Splatform;
using UnityEngine;

// Token: 0x02000109 RID: 265
public class ZPlayFabSocket : ZNetStats, IDisposable, ISocket
{
	// Token: 0x060010AB RID: 4267 RVA: 0x0007A210 File Offset: 0x00078410
	public ZPlayFabSocket()
	{
		this.m_state = ZPlayFabSocketState.LISTEN;
		PlayFabMultiplayerManager.Get().LogLevel = PlayFabMultiplayerManager.LogLevelType.None;
	}

	// Token: 0x060010AC RID: 4268 RVA: 0x0007A29C File Offset: 0x0007849C
	public ZPlayFabSocket(string remotePlayerId, Action<PlayFabMatchmakingServerData> serverDataFoundCallback)
	{
		PlayFabMultiplayerManager.Get().LogLevel = PlayFabMultiplayerManager.LogLevelType.None;
		this.m_state = ZPlayFabSocketState.CONNECTING;
		this.m_remotePlayerId = remotePlayerId;
		this.ClientConnect();
		PlayFabMultiplayerManager.Get().OnDataMessageReceived += this.OnDataMessageReceived;
		PlayFabMultiplayerManager.Get().OnRemotePlayerJoined += this.OnRemotePlayerJoined;
		this.m_isClient = true;
		this.m_platformPlayerId = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
		this.m_serverDataFoundCallback = serverDataFoundCallback;
		ZPackage zpackage = new ZPackage();
		zpackage.Write(1);
		zpackage.Write(this.m_platformPlayerId.ToString());
		this.Send(zpackage, 64);
		ZLog.Log("PlayFab socket with remote ID " + remotePlayerId + " sent local Platform ID " + this.GetHostName());
	}

	// Token: 0x060010AD RID: 4269 RVA: 0x0007A3C9 File Offset: 0x000785C9
	private void ClientConnect()
	{
		ZPlayFabMatchmaking.CheckHostOnlineStatus(this.m_remotePlayerId, new ZPlayFabMatchmakingSuccessCallback(this.OnRemotePlayerSessionFound), new ZPlayFabMatchmakingFailedCallback(this.OnRemotePlayerNotFound), true);
	}

	// Token: 0x060010AE RID: 4270 RVA: 0x0007A3F0 File Offset: 0x000785F0
	private ZPlayFabSocket(PlayFabPlayer remotePlayer)
	{
		this.InitRemotePlayer(remotePlayer);
		this.Connect(remotePlayer);
		this.m_isClient = false;
		this.m_remotePlayerId = remotePlayer.EntityKey.Id;
		PlayFabMultiplayerManager.Get().OnDataMessageReceived += this.OnDataMessageReceived;
		ZLog.Log("PlayFab listen socket child connected to remote player " + this.m_remotePlayerId);
	}

	// Token: 0x060010AF RID: 4271 RVA: 0x0007A4B8 File Offset: 0x000786B8
	private void InitRemotePlayer(PlayFabPlayer remotePlayer)
	{
		this.m_delayedInitActions.Add(delegate
		{
			remotePlayer.IsMuted = true;
			ZLog.Log("Muted PlayFab remote player " + remotePlayer.EntityKey.Id);
		});
	}

	// Token: 0x060010B0 RID: 4272 RVA: 0x0007A4EC File Offset: 0x000786EC
	private void OnRemotePlayerSessionFound(PlayFabMatchmakingServerData serverData)
	{
		Action<PlayFabMatchmakingServerData> serverDataFoundCallback = this.m_serverDataFoundCallback;
		if (serverDataFoundCallback != null)
		{
			serverDataFoundCallback(serverData);
		}
		if (this.m_state == ZPlayFabSocketState.CLOSED)
		{
			return;
		}
		string networkId = PlayFabMultiplayerManager.Get().NetworkId;
		this.m_lobbyId = serverData.lobbyId;
		if (this.m_state == ZPlayFabSocketState.CONNECTING)
		{
			ZLog.Log(string.Concat(new string[]
			{
				"Joining server '",
				serverData.serverName,
				"' at PlayFab network ",
				serverData.networkId,
				" from lobby ",
				serverData.lobbyId
			}));
			PlayFabMultiplayerManager.Get().JoinNetwork(serverData.networkId);
			PlayFabMultiplayerManager.Get().OnNetworkJoined += this.OnNetworkJoined;
			return;
		}
		if (networkId == null || networkId != serverData.networkId || this.m_partyNetworkLeft)
		{
			ZLog.Log("Re-joining server '" + serverData.serverName + "' at new PlayFab network " + serverData.networkId);
			PlayFabMultiplayerManager.Get().JoinNetwork(serverData.networkId);
			this.m_partyNetworkLeft = false;
			return;
		}
		if (this.PartyResetInProgress())
		{
			ZLog.Log(string.Concat(new string[]
			{
				"Leave server '",
				serverData.serverName,
				"' at new PlayFab network ",
				serverData.networkId,
				", try to re-join later"
			}));
			this.ResetPartyTimeout();
			PlayFabMultiplayerManager.Get().LeaveNetwork();
			this.m_partyNetworkLeft = true;
		}
	}

	// Token: 0x060010B1 RID: 4273 RVA: 0x0007A648 File Offset: 0x00078848
	private void OnRemotePlayerNotFound(ZPLayFabMatchmakingFailReason failReason)
	{
		ZLog.LogWarning("Failed to locate network session for PlayFab player " + this.m_remotePlayerId);
		switch (failReason)
		{
		case ZPLayFabMatchmakingFailReason.InvalidServerData:
			ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorVersion);
			break;
		case ZPLayFabMatchmakingFailReason.ServerFull:
			ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorFull);
			break;
		case ZPLayFabMatchmakingFailReason.APIRequestLimitExceeded:
			this.ResetPartyTimeout();
			return;
		}
		this.Close();
	}

	// Token: 0x060010B2 RID: 4274 RVA: 0x0007A6A4 File Offset: 0x000788A4
	private void CheckReestablishConnection(byte[] maybeCompressedBuffer)
	{
		try
		{
			this.OnDataMessageReceivedCont(this.m_zlibWorkQueue.UncompressOnThisThread(maybeCompressedBuffer));
			return;
		}
		catch
		{
		}
		byte msgType = this.GetMsgType(maybeCompressedBuffer);
		if (this.GetMsgId(maybeCompressedBuffer) == 0U && msgType == 64)
		{
			ZLog.Log("Assume restarted game session for remote ID " + this.GetEndPointString() + " and Platform ID " + this.GetHostName());
			this.ResetAll();
			this.OnDataMessageReceivedCont(maybeCompressedBuffer);
		}
	}

	// Token: 0x060010B3 RID: 4275 RVA: 0x0007A720 File Offset: 0x00078920
	private void ResetAll()
	{
		this.m_recvQueue.Clear();
		this.m_outOfOrderQueue.Clear();
		this.m_sendQueue.Clear();
		this.m_inFlightQueue.ResetAll();
		this.m_retransmitCache.Clear();
		List<byte[]> list;
		List<byte[]> list2;
		this.m_zlibWorkQueue.Poll(out list, out list2);
		this.m_next = 0U;
		this.m_canKickstartIn = 0f;
		this.m_useCompression = false;
		this.m_didRecover = false;
		this.CancelResetParty();
	}

	// Token: 0x060010B4 RID: 4276 RVA: 0x0007A79C File Offset: 0x0007899C
	private void OnDataMessageReceived(object sender, PlayFabPlayer from, byte[] compressedBuffer)
	{
		if (from.EntityKey.Id == this.m_remotePlayerId)
		{
			this.DelayedInit();
			if (this.m_useCompression)
			{
				if (!this.m_isClient && this.m_didRecover)
				{
					this.CheckReestablishConnection(compressedBuffer);
					return;
				}
				this.m_zlibWorkQueue.Decompress(compressedBuffer);
				return;
			}
			else
			{
				this.OnDataMessageReceivedCont(compressedBuffer);
			}
		}
	}

	// Token: 0x060010B5 RID: 4277 RVA: 0x0007A7FC File Offset: 0x000789FC
	private void OnDataMessageReceivedCont(byte[] buffer)
	{
		byte msgType = this.GetMsgType(buffer);
		uint msgId = this.GetMsgId(buffer);
		ZPlayFabSocket.s_lastReception = DateTime.UtcNow;
		base.IncRecvBytes(buffer.Length);
		if (msgType == 42)
		{
			this.ProcessAck(msgId);
			return;
		}
		if (this.m_next != msgId)
		{
			this.SendAck(this.m_next);
			if (msgId - this.m_next < 2147483647U && !this.m_outOfOrderQueue.ContainsKey(msgId))
			{
				this.m_outOfOrderQueue.Add(msgId, buffer);
			}
			return;
		}
		if (msgType != 17)
		{
			if (msgType != 64)
			{
				ZLog.LogError("Unknown message type " + msgType.ToString() + " received by socket!\nByte array:\n" + BitConverter.ToString(buffer));
				return;
			}
			this.InternalReceive(new ZPackage(buffer, buffer.Length - 5));
		}
		else
		{
			this.m_recvQueue.Enqueue(new ZPackage(buffer, buffer.Length - 5));
		}
		uint num = this.m_next + 1U;
		this.m_next = num;
		this.SendAck(num);
		if (this.m_outOfOrderQueue.Count != 0)
		{
			this.TryDeliverOutOfOrder();
		}
	}

	// Token: 0x060010B6 RID: 4278 RVA: 0x0007A8FC File Offset: 0x00078AFC
	private void ProcessAck(uint msgId)
	{
		while (this.m_inFlightQueue.Tail != msgId)
		{
			if (this.m_inFlightQueue.IsEmpty)
			{
				this.Close();
				return;
			}
			this.m_inFlightQueue.Drop();
		}
	}

	// Token: 0x060010B7 RID: 4279 RVA: 0x0007A930 File Offset: 0x00078B30
	private void TryDeliverOutOfOrder()
	{
		byte[] buffer;
		while (this.m_outOfOrderQueue.TryGetValue(this.m_next, out buffer))
		{
			this.m_outOfOrderQueue.Remove(this.m_next);
			this.OnDataMessageReceivedCont(buffer);
		}
	}

	// Token: 0x060010B8 RID: 4280 RVA: 0x0007A970 File Offset: 0x00078B70
	private void InternalReceive(ZPackage pkg)
	{
		if (pkg.ReadByte() == 1)
		{
			this.m_platformPlayerId = new PlatformUserID(pkg.ReadString());
			ZLog.Log("PlayFab socket with remote ID " + this.GetEndPointString() + " received local Platform ID " + this.GetHostName());
			return;
		}
		ZLog.LogError("Unknown data in internal receive! Ignoring");
	}

	// Token: 0x060010B9 RID: 4281 RVA: 0x0007A9C2 File Offset: 0x00078BC2
	private void SendAck(uint nextMsgId)
	{
		ZPlayFabSocket.SetMsgType(this.m_sndMsg, 42);
		ZPlayFabSocket.SetMsgId(this.m_sndMsg, nextMsgId);
		this.InternalSend(this.m_sndMsg);
	}

	// Token: 0x060010BA RID: 4282 RVA: 0x0007A9E9 File Offset: 0x00078BE9
	private static void SetMsgType(byte[] payload, byte t)
	{
		payload[4] = t;
	}

	// Token: 0x060010BB RID: 4283 RVA: 0x0007A9EF File Offset: 0x00078BEF
	private static void SetMsgId(byte[] payload, uint id)
	{
		payload[0] = (byte)id;
		payload[1] = (byte)(id >> 8);
		payload[2] = (byte)(id >> 16);
		payload[3] = (byte)(id >> 24);
	}

	// Token: 0x060010BC RID: 4284 RVA: 0x0007AA10 File Offset: 0x00078C10
	private uint GetMsgId(byte[] buffer)
	{
		uint num = 0U;
		int num2 = buffer.Length - 5;
		return num + (uint)buffer[num2] + (uint)((uint)buffer[num2 + 1] << 8) + (uint)((uint)buffer[num2 + 2] << 16) + (uint)((uint)buffer[num2 + 3] << 24);
	}

	// Token: 0x060010BD RID: 4285 RVA: 0x0007AA42 File Offset: 0x00078C42
	private byte GetMsgType(byte[] buffer)
	{
		return buffer[buffer.Length - 1];
	}

	// Token: 0x060010BE RID: 4286 RVA: 0x0007AA4C File Offset: 0x00078C4C
	private void DelayedInit()
	{
		if (this.m_delayedInitActions.Count == 0)
		{
			return;
		}
		foreach (Action action in this.m_delayedInitActions)
		{
			action();
		}
		this.m_delayedInitActions.Clear();
	}

	// Token: 0x060010BF RID: 4287 RVA: 0x0007AAB8 File Offset: 0x00078CB8
	private void OnNetworkJoined(object sender, string networkId)
	{
		ZLog.Log("PlayFab client socket to remote player " + this.m_remotePlayerId + " joined network " + networkId);
		if (this.m_isClient && this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			this.ClientConnect();
		}
		ZRpc.SetLongTimeout(true);
	}

	// Token: 0x060010C0 RID: 4288 RVA: 0x0007AAF2 File Offset: 0x00078CF2
	private void OnRemotePlayerJoined(object sender, PlayFabPlayer player)
	{
		this.InitRemotePlayer(player);
		if (player.EntityKey.Id == this.m_remotePlayerId)
		{
			ZLog.Log("PlayFab socket connected to remote player " + this.m_remotePlayerId);
			this.Connect(player);
		}
	}

	// Token: 0x060010C1 RID: 4289 RVA: 0x0007AB30 File Offset: 0x00078D30
	private void Connect(PlayFabPlayer remotePlayer)
	{
		string id = remotePlayer.EntityKey.Id;
		if (!ZPlayFabSocket.s_connectSockets.ContainsKey(id))
		{
			ZPlayFabSocket.s_connectSockets.Add(id, this);
			ZPlayFabSocket.s_lastReception = DateTime.UtcNow;
		}
		if (this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			ZLog.Log("Resume TX on " + this.GetEndPointString());
		}
		this.m_peer = new PlayFabPlayer[]
		{
			remotePlayer
		};
		this.m_state = ZPlayFabSocketState.CONNECTED;
		this.CancelResetParty();
		if (this.m_sendQueue.Count > 0)
		{
			this.m_inFlightQueue.ResetRetransTimer(false);
			while (this.m_sendQueue.Count > 0)
			{
				this.InternalSend(this.m_sendQueue.Dequeue());
			}
			return;
		}
		this.KickstartAfterRecovery();
	}

	// Token: 0x060010C2 RID: 4290 RVA: 0x0007ABE9 File Offset: 0x00078DE9
	private bool PartyResetInProgress()
	{
		return this.m_partyResetTimeout > 0f;
	}

	// Token: 0x060010C3 RID: 4291 RVA: 0x0007ABF8 File Offset: 0x00078DF8
	private void CancelResetParty()
	{
		this.m_didRecover = this.PartyResetInProgress();
		this.m_partyNetworkLeft = false;
		this.m_partyResetTimeout = 0f;
		this.m_partyResetConnectTimeout = 0f;
		ZPlayFabSocket.s_durationToPartyReset = 0f;
	}

	// Token: 0x060010C4 RID: 4292 RVA: 0x0007AC30 File Offset: 0x00078E30
	private void InternalSend(byte[] payload)
	{
		if (!this.PartyResetInProgress())
		{
			base.IncSentBytes(payload.Length);
			if (this.m_useCompression)
			{
				if (ZNet.instance != null && ZNet.instance.HaveStopped)
				{
					this.InternalSendCont(this.m_zlibWorkQueue.CompressOnThisThread(payload));
					return;
				}
				this.m_zlibWorkQueue.Compress(payload);
				return;
			}
			else
			{
				this.InternalSendCont(payload);
			}
		}
	}

	// Token: 0x060010C5 RID: 4293 RVA: 0x0007AC98 File Offset: 0x00078E98
	private void InternalSendCont(byte[] compressedPayload)
	{
		if (!this.PartyResetInProgress())
		{
			if (PlayFabMultiplayerManager.Get().SendDataMessage(compressedPayload, this.m_peer, DeliveryOption.Guaranteed))
			{
				if (!this.m_isClient)
				{
					ZPlayFabMatchmaking.ForwardProgress();
					return;
				}
			}
			else
			{
				if (this.m_isClient)
				{
					ZPlayFabSocket.ScheduleResetParty();
				}
				this.ResetPartyTimeout();
				ZLog.Log("Failed to send, suspend TX on " + this.GetEndPointString() + " while trying to reconnect");
			}
		}
	}

	// Token: 0x060010C6 RID: 4294 RVA: 0x0007ACFC File Offset: 0x00078EFC
	private void ResetPartyTimeout()
	{
		this.m_partyResetConnectTimeout = UnityEngine.Random.Range(9f, 11f) + ZPlayFabSocket.s_durationToPartyReset;
		this.m_partyResetTimeout = UnityEngine.Random.Range(18f, 22f) + ZPlayFabSocket.s_durationToPartyReset;
	}

	// Token: 0x060010C7 RID: 4295 RVA: 0x0007AD34 File Offset: 0x00078F34
	internal static void ScheduleResetParty()
	{
		if (ZPlayFabSocket.s_durationToPartyReset <= 0f)
		{
			ZPlayFabSocket.s_durationToPartyReset = UnityEngine.Random.Range(2.6999998f, 3.3000002f);
		}
	}

	// Token: 0x060010C8 RID: 4296 RVA: 0x0007AD58 File Offset: 0x00078F58
	public void Dispose()
	{
		Debug.Log("ZPlayFabSocket::Dispose. State: " + this.m_state.ToString());
		this.m_zlibWorkQueue.Dispose();
		this.ResetAll();
		if (this.m_state == ZPlayFabSocketState.CLOSED)
		{
			return;
		}
		if (this.m_state == ZPlayFabSocketState.LISTEN)
		{
			ZPlayFabSocket.s_listenSocket = null;
			using (Queue<ZPlayFabSocket>.Enumerator enumerator = this.m_backlog.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZPlayFabSocket zplayFabSocket = enumerator.Current;
					zplayFabSocket.Close();
				}
				goto IL_92;
			}
		}
		PlayFabMultiplayerManager.Get().OnDataMessageReceived -= this.OnDataMessageReceived;
		IL_92:
		if (!ZNet.instance.IsServer())
		{
			PlayFabMultiplayerManager.Get().OnRemotePlayerJoined -= this.OnRemotePlayerJoined;
			PlayFabMultiplayerManager.Get().OnNetworkJoined -= this.OnNetworkJoined;
			PlayFabMultiplayerManager.Get().LeaveNetwork();
		}
		if (this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			ZPlayFabSocket.s_connectSockets.Remove(this.m_peer[0].EntityKey.Id);
		}
		Debug.Log("ZPlayFabSocket::Dispose. leave lobby. LobbyId: " + this.m_lobbyId);
		if (this.m_lobbyId != null)
		{
			ZPlayFabMatchmaking.LeaveLobby(this.m_lobbyId);
		}
		this.m_state = ZPlayFabSocketState.CLOSED;
	}

	// Token: 0x060010C9 RID: 4297 RVA: 0x0007AEA0 File Offset: 0x000790A0
	private void Update(float dt)
	{
		if (this.m_canKickstartIn >= 0f)
		{
			this.m_canKickstartIn -= dt;
		}
		if (!this.m_isClient)
		{
			return;
		}
		if (this.PartyResetInProgress())
		{
			this.m_partyResetTimeout -= dt;
			if (this.m_partyResetConnectTimeout > 0f)
			{
				this.m_partyResetConnectTimeout -= dt;
				if (this.m_partyResetConnectTimeout <= 0f)
				{
					this.ClientConnect();
					return;
				}
			}
		}
		else if ((DateTime.UtcNow - ZPlayFabSocket.s_lastReception).TotalSeconds >= 26.0 && this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			ZLog.Log("Do a reset party as nothing seems to be received");
			this.ResetPartyTimeout();
			PlayFabMultiplayerManager.Get().ResetParty();
		}
	}

	// Token: 0x060010CA RID: 4298 RVA: 0x0007AF5C File Offset: 0x0007915C
	private void LateUpdate()
	{
		List<byte[]> list;
		List<byte[]> list2;
		this.m_zlibWorkQueue.Poll(out list, out list2);
		if (list != null)
		{
			foreach (byte[] compressedPayload in list)
			{
				this.InternalSendCont(compressedPayload);
			}
		}
		if (list2 != null)
		{
			foreach (byte[] buffer in list2)
			{
				this.OnDataMessageReceivedCont(buffer);
			}
		}
	}

	// Token: 0x060010CB RID: 4299 RVA: 0x0007B000 File Offset: 0x00079200
	public bool IsConnected()
	{
		return this.m_state == ZPlayFabSocketState.CONNECTED || this.m_state == ZPlayFabSocketState.CONNECTING;
	}

	// Token: 0x060010CC RID: 4300 RVA: 0x0007B016 File Offset: 0x00079216
	public void VersionMatch()
	{
		this.m_useCompression = true;
	}

	// Token: 0x060010CD RID: 4301 RVA: 0x0007B020 File Offset: 0x00079220
	public void Send(ZPackage pkg, byte messageType)
	{
		if (pkg.Size() == 0 || !this.IsConnected())
		{
			return;
		}
		pkg.Write(this.m_inFlightQueue.Head);
		pkg.Write(messageType);
		byte[] array = pkg.GetArray();
		this.m_inFlightQueue.Enqueue(array);
		if (this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			this.InternalSend(array);
			return;
		}
		this.m_sendQueue.Enqueue(array);
	}

	// Token: 0x060010CE RID: 4302 RVA: 0x0007B086 File Offset: 0x00079286
	public void Send(ZPackage pkg)
	{
		this.Send(pkg, 17);
	}

	// Token: 0x060010CF RID: 4303 RVA: 0x0007B091 File Offset: 0x00079291
	public ZPackage Recv()
	{
		this.CheckRetransmit();
		if (!this.GotNewData())
		{
			return null;
		}
		return this.m_recvQueue.Dequeue();
	}

	// Token: 0x060010D0 RID: 4304 RVA: 0x0007B0AE File Offset: 0x000792AE
	private void CheckRetransmit()
	{
		if (this.m_inFlightQueue.IsEmpty || this.PartyResetInProgress() || this.m_state != ZPlayFabSocketState.CONNECTED)
		{
			return;
		}
		if (Time.time < this.m_inFlightQueue.NextResend)
		{
			return;
		}
		this.DoRetransmit(true);
	}

	// Token: 0x060010D1 RID: 4305 RVA: 0x0007B0E9 File Offset: 0x000792E9
	private void DoRetransmit(bool canKickstart = true)
	{
		if (canKickstart && this.CanKickstartRatelimit())
		{
			this.KickstartAfterRecovery();
			return;
		}
		if (!this.m_inFlightQueue.IsEmpty)
		{
			this.InternalSend(this.m_inFlightQueue.Peek());
			this.m_inFlightQueue.ResetRetransTimer(true);
		}
	}

	// Token: 0x060010D2 RID: 4306 RVA: 0x0007B127 File Offset: 0x00079327
	private bool CanKickstartRatelimit()
	{
		return this.m_canKickstartIn <= 0f;
	}

	// Token: 0x060010D3 RID: 4307 RVA: 0x0007B13C File Offset: 0x0007933C
	private void KickstartAfterRecovery()
	{
		try
		{
			this.TryKickstartAfterRecovery();
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("Failed to resend data on $" + this.GetEndPointString() + ", closing socket: " + ex.Message);
			this.Close();
		}
	}

	// Token: 0x060010D4 RID: 4308 RVA: 0x0007B18C File Offset: 0x0007938C
	private void TryKickstartAfterRecovery()
	{
		if (!this.m_inFlightQueue.IsEmpty)
		{
			this.m_inFlightQueue.CopyPayloads(this.m_retransmitCache);
			foreach (byte[] payload in this.m_retransmitCache)
			{
				this.InternalSend(payload);
			}
			this.m_retransmitCache.Clear();
			this.m_inFlightQueue.ResetRetransTimer(false);
		}
		this.m_canKickstartIn = 6f;
	}

	// Token: 0x060010D5 RID: 4309 RVA: 0x0007B220 File Offset: 0x00079420
	public int GetSendQueueSize()
	{
		return (int)(this.m_inFlightQueue.Bytes * 0.25f);
	}

	// Token: 0x060010D6 RID: 4310 RVA: 0x0007B236 File Offset: 0x00079436
	public int GetCurrentSendRate()
	{
		throw new NotImplementedException();
	}

	// Token: 0x060010D7 RID: 4311 RVA: 0x0007B23D File Offset: 0x0007943D
	internal void StartHost()
	{
		if (ZPlayFabSocket.s_listenSocket != null)
		{
			ZLog.LogError("Multiple PlayFab listen sockets");
			return;
		}
		ZPlayFabSocket.s_listenSocket = this;
	}

	// Token: 0x060010D8 RID: 4312 RVA: 0x0007B257 File Offset: 0x00079457
	public bool IsHost()
	{
		return this.m_state == ZPlayFabSocketState.LISTEN;
	}

	// Token: 0x060010D9 RID: 4313 RVA: 0x0007B262 File Offset: 0x00079462
	public bool GotNewData()
	{
		return this.m_recvQueue.Count > 0;
	}

	// Token: 0x060010DA RID: 4314 RVA: 0x0007B274 File Offset: 0x00079474
	public string GetEndPointString()
	{
		string str = "";
		if (this.m_peer != null)
		{
			str = this.m_peer[0].EntityKey.Id;
		}
		return "playfab/" + str;
	}

	// Token: 0x060010DB RID: 4315 RVA: 0x0007B2AD File Offset: 0x000794AD
	public ISocket Accept()
	{
		if (this.m_backlog.Count == 0)
		{
			return null;
		}
		ZRpc.SetLongTimeout(true);
		return this.m_backlog.Dequeue();
	}

	// Token: 0x060010DC RID: 4316 RVA: 0x0007B2CF File Offset: 0x000794CF
	public int GetHostPort()
	{
		if (!this.IsHost())
		{
			return -1;
		}
		return 0;
	}

	// Token: 0x060010DD RID: 4317 RVA: 0x0007B2DC File Offset: 0x000794DC
	public bool Flush()
	{
		throw new NotImplementedException();
	}

	// Token: 0x060010DE RID: 4318 RVA: 0x0007B2E3 File Offset: 0x000794E3
	public string GetHostName()
	{
		return this.m_platformPlayerId.ToString();
	}

	// Token: 0x060010DF RID: 4319 RVA: 0x0007B2F6 File Offset: 0x000794F6
	public void Close()
	{
		this.Dispose();
	}

	// Token: 0x060010E0 RID: 4320 RVA: 0x0007B300 File Offset: 0x00079500
	internal static void LostConnection(PlayFabPlayer player)
	{
		string id = player.EntityKey.Id;
		ZPlayFabSocket zplayFabSocket;
		if (ZPlayFabSocket.s_connectSockets.TryGetValue(id, out zplayFabSocket))
		{
			ZLog.Log("Keep socket for " + zplayFabSocket.GetEndPointString() + ", try to reconnect before timeout");
		}
	}

	// Token: 0x060010E1 RID: 4321 RVA: 0x0007B344 File Offset: 0x00079544
	internal static void QueueConnection(PlayFabPlayer player)
	{
		string id = player.EntityKey.Id;
		ZPlayFabSocket zplayFabSocket;
		if (ZPlayFabSocket.s_connectSockets.TryGetValue(id, out zplayFabSocket))
		{
			ZLog.Log("Resume TX on " + zplayFabSocket.GetEndPointString());
			zplayFabSocket.Connect(player);
			return;
		}
		if (ZPlayFabSocket.s_listenSocket != null)
		{
			ZPlayFabSocket.s_listenSocket.m_backlog.Enqueue(new ZPlayFabSocket(player));
			return;
		}
		ZLog.LogError("Incoming PlayFab connection without any open listen socket");
	}

	// Token: 0x060010E2 RID: 4322 RVA: 0x0007B3B0 File Offset: 0x000795B0
	internal static void DestroyListenSocket()
	{
		while (ZPlayFabSocket.s_connectSockets.Count > 0)
		{
			Dictionary<string, ZPlayFabSocket>.Enumerator enumerator = ZPlayFabSocket.s_connectSockets.GetEnumerator();
			enumerator.MoveNext();
			KeyValuePair<string, ZPlayFabSocket> keyValuePair = enumerator.Current;
			keyValuePair.Value.Close();
		}
		ZPlayFabSocket.s_listenSocket.Close();
		ZPlayFabSocket.s_listenSocket = null;
	}

	// Token: 0x060010E3 RID: 4323 RVA: 0x0007B403 File Offset: 0x00079603
	internal static uint NumSockets()
	{
		return (uint)ZPlayFabSocket.s_connectSockets.Count;
	}

	// Token: 0x060010E4 RID: 4324 RVA: 0x0007B410 File Offset: 0x00079610
	internal static void UpdateAllSockets(float dt)
	{
		if (ZPlayFabSocket.s_durationToPartyReset > 0f)
		{
			ZPlayFabSocket.s_durationToPartyReset -= dt;
			if (ZPlayFabSocket.s_durationToPartyReset < 0f)
			{
				ZLog.Log("Reset party to clear network error");
				PlayFabMultiplayerManager.Get().ResetParty();
			}
		}
		foreach (ZPlayFabSocket zplayFabSocket in ZPlayFabSocket.s_connectSockets.Values)
		{
			zplayFabSocket.Update(dt);
		}
	}

	// Token: 0x060010E5 RID: 4325 RVA: 0x0007B4A0 File Offset: 0x000796A0
	internal static void LateUpdateAllSocket()
	{
		foreach (ZPlayFabSocket zplayFabSocket in ZPlayFabSocket.s_connectSockets.Values)
		{
			zplayFabSocket.LateUpdate();
		}
	}

	// Token: 0x04000FD3 RID: 4051
	private const byte PAYLOAD_DAT = 17;

	// Token: 0x04000FD4 RID: 4052
	private const byte PAYLOAD_ACK = 42;

	// Token: 0x04000FD5 RID: 4053
	private const byte PAYLOAD_INT = 64;

	// Token: 0x04000FD6 RID: 4054
	private const int PAYLOAD_HEADER_LEN = 5;

	// Token: 0x04000FD7 RID: 4055
	private const float PARTY_RESET_GRACE_SEC = 3f;

	// Token: 0x04000FD8 RID: 4056
	private const float PARTY_RESET_TIMEOUT_SEC = 20f;

	// Token: 0x04000FD9 RID: 4057
	private const float KICKSTART_COOLDOWN = 6f;

	// Token: 0x04000FDA RID: 4058
	private const float NETWORK_ERROR_WATCHDOG = 26f;

	// Token: 0x04000FDB RID: 4059
	private const float INFLIGHT_SCALING_FACTOR = 0.25f;

	// Token: 0x04000FDC RID: 4060
	private const byte INT_PLATFORM_ID = 1;

	// Token: 0x04000FDD RID: 4061
	private static ZPlayFabSocket s_listenSocket;

	// Token: 0x04000FDE RID: 4062
	private static readonly Dictionary<string, ZPlayFabSocket> s_connectSockets = new Dictionary<string, ZPlayFabSocket>();

	// Token: 0x04000FDF RID: 4063
	private static float s_durationToPartyReset;

	// Token: 0x04000FE0 RID: 4064
	private static DateTime s_lastReception;

	// Token: 0x04000FE1 RID: 4065
	private ZPlayFabSocketState m_state;

	// Token: 0x04000FE2 RID: 4066
	private PlayFabPlayer[] m_peer;

	// Token: 0x04000FE3 RID: 4067
	private string m_lobbyId;

	// Token: 0x04000FE4 RID: 4068
	private readonly byte[] m_sndMsg = new byte[5];

	// Token: 0x04000FE5 RID: 4069
	private readonly bool m_isClient;

	// Token: 0x04000FE6 RID: 4070
	public readonly string m_remotePlayerId;

	// Token: 0x04000FE7 RID: 4071
	private PlatformUserID m_platformPlayerId;

	// Token: 0x04000FE8 RID: 4072
	private readonly Queue<ZPackage> m_recvQueue = new Queue<ZPackage>();

	// Token: 0x04000FE9 RID: 4073
	private readonly Dictionary<uint, byte[]> m_outOfOrderQueue = new Dictionary<uint, byte[]>();

	// Token: 0x04000FEA RID: 4074
	private readonly Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	// Token: 0x04000FEB RID: 4075
	private readonly ZPlayFabSocket.InFlightQueue m_inFlightQueue = new ZPlayFabSocket.InFlightQueue();

	// Token: 0x04000FEC RID: 4076
	private readonly List<byte[]> m_retransmitCache = new List<byte[]>();

	// Token: 0x04000FED RID: 4077
	private readonly List<Action> m_delayedInitActions = new List<Action>();

	// Token: 0x04000FEE RID: 4078
	private readonly PlayFabZLibWorkQueue m_zlibWorkQueue = new PlayFabZLibWorkQueue();

	// Token: 0x04000FEF RID: 4079
	private readonly Queue<ZPlayFabSocket> m_backlog = new Queue<ZPlayFabSocket>();

	// Token: 0x04000FF0 RID: 4080
	private uint m_next;

	// Token: 0x04000FF1 RID: 4081
	private float m_partyResetTimeout;

	// Token: 0x04000FF2 RID: 4082
	private float m_partyResetConnectTimeout;

	// Token: 0x04000FF3 RID: 4083
	private bool m_partyNetworkLeft;

	// Token: 0x04000FF4 RID: 4084
	private bool m_didRecover;

	// Token: 0x04000FF5 RID: 4085
	private float m_canKickstartIn;

	// Token: 0x04000FF6 RID: 4086
	private bool m_useCompression;

	// Token: 0x04000FF7 RID: 4087
	private Action<PlayFabMatchmakingServerData> m_serverDataFoundCallback;

	// Token: 0x02000308 RID: 776
	public class InFlightQueue
	{
		// Token: 0x170001B2 RID: 434
		// (get) Token: 0x060021E3 RID: 8675 RVA: 0x000EC314 File Offset: 0x000EA514
		public uint Bytes
		{
			get
			{
				return this.m_size;
			}
		}

		// Token: 0x170001B3 RID: 435
		// (get) Token: 0x060021E4 RID: 8676 RVA: 0x000EC31C File Offset: 0x000EA51C
		public uint Head
		{
			get
			{
				return this.m_head;
			}
		}

		// Token: 0x170001B4 RID: 436
		// (get) Token: 0x060021E5 RID: 8677 RVA: 0x000EC324 File Offset: 0x000EA524
		public uint Tail
		{
			get
			{
				return this.m_tail;
			}
		}

		// Token: 0x170001B5 RID: 437
		// (get) Token: 0x060021E6 RID: 8678 RVA: 0x000EC32C File Offset: 0x000EA52C
		public bool IsEmpty
		{
			get
			{
				return this.m_payloads.Count == 0;
			}
		}

		// Token: 0x170001B6 RID: 438
		// (get) Token: 0x060021E7 RID: 8679 RVA: 0x000EC33C File Offset: 0x000EA53C
		public float NextResend
		{
			get
			{
				return this.m_nextResend;
			}
		}

		// Token: 0x060021E8 RID: 8680 RVA: 0x000EC344 File Offset: 0x000EA544
		public void Enqueue(byte[] payload)
		{
			this.m_payloads.Enqueue(payload);
			this.m_size += (uint)payload.Length;
			this.m_head += 1U;
		}

		// Token: 0x060021E9 RID: 8681 RVA: 0x000EC370 File Offset: 0x000EA570
		public void Drop()
		{
			this.m_size -= (uint)this.m_payloads.Dequeue().Length;
			this.m_tail += 1U;
			this.ResetRetransTimer(false);
		}

		// Token: 0x060021EA RID: 8682 RVA: 0x000EC3A1 File Offset: 0x000EA5A1
		public byte[] Peek()
		{
			return this.m_payloads.Peek();
		}

		// Token: 0x060021EB RID: 8683 RVA: 0x000EC3B0 File Offset: 0x000EA5B0
		public void CopyPayloads(List<byte[]> payloads)
		{
			while (this.m_payloads.Count > 0)
			{
				payloads.Add(this.m_payloads.Dequeue());
			}
			foreach (byte[] item in payloads)
			{
				this.m_payloads.Enqueue(item);
			}
		}

		// Token: 0x060021EC RID: 8684 RVA: 0x000EC424 File Offset: 0x000EA624
		public void ResetRetransTimer(bool small = false)
		{
			this.m_nextResend = Time.time + (small ? 1f : 3f);
		}

		// Token: 0x060021ED RID: 8685 RVA: 0x000EC441 File Offset: 0x000EA641
		public void ResetAll()
		{
			this.m_payloads.Clear();
			this.m_nextResend = 0f;
			this.m_size = 0U;
			this.m_head = 0U;
			this.m_tail = 0U;
		}

		// Token: 0x040023A6 RID: 9126
		private readonly Queue<byte[]> m_payloads = new Queue<byte[]>();

		// Token: 0x040023A7 RID: 9127
		private float m_nextResend;

		// Token: 0x040023A8 RID: 9128
		private uint m_size;

		// Token: 0x040023A9 RID: 9129
		private uint m_head;

		// Token: 0x040023AA RID: 9130
		private uint m_tail;
	}
}
