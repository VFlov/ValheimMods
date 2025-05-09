using System;
using System.Collections.Generic;
using System.Threading;
using Steamworks;

// Token: 0x020000EE RID: 238
public class ZSteamSocketOLD : IDisposable, ISocket
{
	// Token: 0x06000F88 RID: 3976 RVA: 0x0007493C File Offset: 0x00072B3C
	public ZSteamSocketOLD()
	{
		ZSteamSocketOLD.m_sockets.Add(this);
		ZSteamSocketOLD.RegisterGlobalCallbacks();
	}

	// Token: 0x06000F89 RID: 3977 RVA: 0x0007498C File Offset: 0x00072B8C
	public ZSteamSocketOLD(CSteamID peerID)
	{
		ZSteamSocketOLD.m_sockets.Add(this);
		this.m_peerID = peerID;
		ZSteamSocketOLD.RegisterGlobalCallbacks();
	}

	// Token: 0x06000F8A RID: 3978 RVA: 0x000749E4 File Offset: 0x00072BE4
	private static void RegisterGlobalCallbacks()
	{
		if (ZSteamSocketOLD.m_connectionFailed == null)
		{
			ZLog.Log("ZSteamSocketOLD  Registering global callbacks");
			ZSteamSocketOLD.m_connectionFailed = Callback<P2PSessionConnectFail_t>.Create(new Callback<P2PSessionConnectFail_t>.DispatchDelegate(ZSteamSocketOLD.OnConnectionFailed));
		}
		if (ZSteamSocketOLD.m_SessionRequest == null)
		{
			ZSteamSocketOLD.m_SessionRequest = Callback<P2PSessionRequest_t>.Create(new Callback<P2PSessionRequest_t>.DispatchDelegate(ZSteamSocketOLD.OnSessionRequest));
		}
	}

	// Token: 0x06000F8B RID: 3979 RVA: 0x00074A38 File Offset: 0x00072C38
	private static void UnregisterGlobalCallbacks()
	{
		ZLog.Log("ZSteamSocket  UnregisterGlobalCallbacks, existing sockets:" + ZSteamSocketOLD.m_sockets.Count.ToString());
		if (ZSteamSocketOLD.m_connectionFailed != null)
		{
			ZSteamSocketOLD.m_connectionFailed.Dispose();
			ZSteamSocketOLD.m_connectionFailed = null;
		}
		if (ZSteamSocketOLD.m_SessionRequest != null)
		{
			ZSteamSocketOLD.m_SessionRequest.Dispose();
			ZSteamSocketOLD.m_SessionRequest = null;
		}
	}

	// Token: 0x06000F8C RID: 3980 RVA: 0x00074A94 File Offset: 0x00072C94
	private static void OnConnectionFailed(P2PSessionConnectFail_t data)
	{
		string str = "Got connection failed callback: ";
		CSteamID steamIDRemote = data.m_steamIDRemote;
		ZLog.Log(str + steamIDRemote.ToString());
		foreach (ZSteamSocketOLD zsteamSocketOLD in ZSteamSocketOLD.m_sockets)
		{
			if (zsteamSocketOLD.IsPeer(data.m_steamIDRemote))
			{
				zsteamSocketOLD.Close();
			}
		}
	}

	// Token: 0x06000F8D RID: 3981 RVA: 0x00074B18 File Offset: 0x00072D18
	private static void OnSessionRequest(P2PSessionRequest_t data)
	{
		string str = "Got session request from ";
		CSteamID steamIDRemote = data.m_steamIDRemote;
		ZLog.Log(str + steamIDRemote.ToString());
		if (SteamNetworking.AcceptP2PSessionWithUser(data.m_steamIDRemote))
		{
			ZSteamSocketOLD listner = ZSteamSocketOLD.GetListner();
			if (listner != null)
			{
				listner.QueuePendingConnection(data.m_steamIDRemote);
			}
		}
	}

	// Token: 0x06000F8E RID: 3982 RVA: 0x00074B6C File Offset: 0x00072D6C
	public void Dispose()
	{
		ZLog.Log("Disposing socket");
		this.Close();
		this.m_pkgQueue.Clear();
		ZSteamSocketOLD.m_sockets.Remove(this);
		if (ZSteamSocketOLD.m_sockets.Count == 0)
		{
			ZLog.Log("Last socket, unregistering callback");
			ZSteamSocketOLD.UnregisterGlobalCallbacks();
		}
	}

	// Token: 0x06000F8F RID: 3983 RVA: 0x00074BBC File Offset: 0x00072DBC
	public void Close()
	{
		ZLog.Log("Closing socket " + this.GetEndPointString());
		if (this.m_peerID != CSteamID.Nil)
		{
			this.Flush();
			ZLog.Log("  send queue size:" + this.m_sendQueue.Count.ToString());
			Thread.Sleep(100);
			P2PSessionState_t p2PSessionState_t;
			SteamNetworking.GetP2PSessionState(this.m_peerID, out p2PSessionState_t);
			ZLog.Log("  P2P state, bytes in send queue:" + p2PSessionState_t.m_nBytesQueuedForSend.ToString());
			SteamNetworking.CloseP2PSessionWithUser(this.m_peerID);
			SteamUser.EndAuthSession(this.m_peerID);
			this.m_peerID = CSteamID.Nil;
		}
		this.m_listner = false;
	}

	// Token: 0x06000F90 RID: 3984 RVA: 0x00074C72 File Offset: 0x00072E72
	public bool StartHost()
	{
		this.m_listner = true;
		this.m_pendingConnections.Clear();
		return true;
	}

	// Token: 0x06000F91 RID: 3985 RVA: 0x00074C88 File Offset: 0x00072E88
	private ZSteamSocketOLD QueuePendingConnection(CSteamID id)
	{
		foreach (ZSteamSocketOLD zsteamSocketOLD in this.m_pendingConnections)
		{
			if (zsteamSocketOLD.IsPeer(id))
			{
				return zsteamSocketOLD;
			}
		}
		ZSteamSocketOLD zsteamSocketOLD2 = new ZSteamSocketOLD(id);
		this.m_pendingConnections.Enqueue(zsteamSocketOLD2);
		return zsteamSocketOLD2;
	}

	// Token: 0x06000F92 RID: 3986 RVA: 0x00074CF8 File Offset: 0x00072EF8
	public ISocket Accept()
	{
		if (!this.m_listner)
		{
			return null;
		}
		if (this.m_pendingConnections.Count > 0)
		{
			return this.m_pendingConnections.Dequeue();
		}
		return null;
	}

	// Token: 0x06000F93 RID: 3987 RVA: 0x00074D1F File Offset: 0x00072F1F
	public bool IsConnected()
	{
		return this.m_peerID != CSteamID.Nil;
	}

	// Token: 0x06000F94 RID: 3988 RVA: 0x00074D34 File Offset: 0x00072F34
	public void Send(ZPackage pkg)
	{
		if (pkg.Size() == 0)
		{
			return;
		}
		if (!this.IsConnected())
		{
			return;
		}
		byte[] array = pkg.GetArray();
		byte[] bytes = BitConverter.GetBytes(array.Length);
		byte[] array2 = new byte[array.Length + bytes.Length];
		bytes.CopyTo(array2, 0);
		array.CopyTo(array2, 4);
		this.m_sendQueue.Enqueue(array);
		this.SendQueuedPackages();
	}

	// Token: 0x06000F95 RID: 3989 RVA: 0x00074D92 File Offset: 0x00072F92
	public bool Flush()
	{
		this.SendQueuedPackages();
		return this.m_sendQueue.Count == 0;
	}

	// Token: 0x06000F96 RID: 3990 RVA: 0x00074DA8 File Offset: 0x00072FA8
	private void SendQueuedPackages()
	{
		if (!this.IsConnected())
		{
			return;
		}
		while (this.m_sendQueue.Count > 0)
		{
			byte[] array = this.m_sendQueue.Peek();
			EP2PSend eP2PSendType = EP2PSend.k_EP2PSendReliable;
			if (!SteamNetworking.SendP2PPacket(this.m_peerID, array, (uint)array.Length, eP2PSendType, 0))
			{
				break;
			}
			this.m_totalSent += array.Length;
			this.m_sendQueue.Dequeue();
		}
	}

	// Token: 0x06000F97 RID: 3991 RVA: 0x00074E0C File Offset: 0x0007300C
	public static void Update()
	{
		foreach (ZSteamSocketOLD zsteamSocketOLD in ZSteamSocketOLD.m_sockets)
		{
			zsteamSocketOLD.SendQueuedPackages();
		}
		ZSteamSocketOLD.ReceivePackages();
	}

	// Token: 0x06000F98 RID: 3992 RVA: 0x00074E60 File Offset: 0x00073060
	private static void ReceivePackages()
	{
		uint num;
		while (SteamNetworking.IsP2PPacketAvailable(out num, 0))
		{
			byte[] array = new byte[num];
			uint num2;
			CSteamID sender;
			if (!SteamNetworking.ReadP2PPacket(array, num, out num2, out sender, 0))
			{
				break;
			}
			ZSteamSocketOLD.QueueNewPkg(sender, array);
		}
	}

	// Token: 0x06000F99 RID: 3993 RVA: 0x00074E98 File Offset: 0x00073098
	private static void QueueNewPkg(CSteamID sender, byte[] data)
	{
		foreach (ZSteamSocketOLD zsteamSocketOLD in ZSteamSocketOLD.m_sockets)
		{
			if (zsteamSocketOLD.IsPeer(sender))
			{
				zsteamSocketOLD.QueuePackage(data);
				return;
			}
		}
		ZSteamSocketOLD listner = ZSteamSocketOLD.GetListner();
		CSteamID csteamID;
		if (listner != null)
		{
			string str = "Got package from unconnected peer ";
			csteamID = sender;
			ZLog.Log(str + csteamID.ToString());
			listner.QueuePendingConnection(sender).QueuePackage(data);
			return;
		}
		string str2 = "Got package from unkown peer ";
		csteamID = sender;
		ZLog.Log(str2 + csteamID.ToString() + " but no active listner");
	}

	// Token: 0x06000F9A RID: 3994 RVA: 0x00074F50 File Offset: 0x00073150
	private static ZSteamSocketOLD GetListner()
	{
		foreach (ZSteamSocketOLD zsteamSocketOLD in ZSteamSocketOLD.m_sockets)
		{
			if (zsteamSocketOLD.IsHost())
			{
				return zsteamSocketOLD;
			}
		}
		return null;
	}

	// Token: 0x06000F9B RID: 3995 RVA: 0x00074FAC File Offset: 0x000731AC
	private void QueuePackage(byte[] data)
	{
		ZPackage item = new ZPackage(data);
		this.m_pkgQueue.Enqueue(item);
		this.m_gotData = true;
		this.m_totalRecv += data.Length;
	}

	// Token: 0x06000F9C RID: 3996 RVA: 0x00074FE3 File Offset: 0x000731E3
	public ZPackage Recv()
	{
		if (!this.IsConnected())
		{
			return null;
		}
		if (this.m_pkgQueue.Count > 0)
		{
			return this.m_pkgQueue.Dequeue();
		}
		return null;
	}

	// Token: 0x06000F9D RID: 3997 RVA: 0x0007500A File Offset: 0x0007320A
	public string GetEndPointString()
	{
		return this.m_peerID.ToString();
	}

	// Token: 0x06000F9E RID: 3998 RVA: 0x0007501D File Offset: 0x0007321D
	public string GetHostName()
	{
		return this.m_peerID.ToString();
	}

	// Token: 0x06000F9F RID: 3999 RVA: 0x00075030 File Offset: 0x00073230
	public CSteamID GetPeerID()
	{
		return this.m_peerID;
	}

	// Token: 0x06000FA0 RID: 4000 RVA: 0x00075038 File Offset: 0x00073238
	public bool IsPeer(CSteamID peer)
	{
		return this.IsConnected() && peer == this.m_peerID;
	}

	// Token: 0x06000FA1 RID: 4001 RVA: 0x00075050 File Offset: 0x00073250
	public bool IsHost()
	{
		return this.m_listner;
	}

	// Token: 0x06000FA2 RID: 4002 RVA: 0x00075058 File Offset: 0x00073258
	public int GetSendQueueSize()
	{
		if (!this.IsConnected())
		{
			return 0;
		}
		int num = 0;
		foreach (byte[] array in this.m_sendQueue)
		{
			num += array.Length;
		}
		return num;
	}

	// Token: 0x06000FA3 RID: 4003 RVA: 0x000750B8 File Offset: 0x000732B8
	public bool IsSending()
	{
		return this.IsConnected() && this.m_sendQueue.Count > 0;
	}

	// Token: 0x06000FA4 RID: 4004 RVA: 0x000750D2 File Offset: 0x000732D2
	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
	}

	// Token: 0x06000FA5 RID: 4005 RVA: 0x000750F5 File Offset: 0x000732F5
	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = this.m_totalSent;
		totalRecv = this.m_totalRecv;
		this.m_totalSent = 0;
		this.m_totalRecv = 0;
	}

	// Token: 0x06000FA6 RID: 4006 RVA: 0x00075115 File Offset: 0x00073315
	public bool GotNewData()
	{
		bool gotData = this.m_gotData;
		this.m_gotData = false;
		return gotData;
	}

	// Token: 0x06000FA7 RID: 4007 RVA: 0x00075124 File Offset: 0x00073324
	public int GetCurrentSendRate()
	{
		return 0;
	}

	// Token: 0x06000FA8 RID: 4008 RVA: 0x00075127 File Offset: 0x00073327
	public int GetAverageSendRate()
	{
		return 0;
	}

	// Token: 0x06000FA9 RID: 4009 RVA: 0x0007512A File Offset: 0x0007332A
	public int GetHostPort()
	{
		if (this.IsHost())
		{
			return 1;
		}
		return -1;
	}

	// Token: 0x06000FAA RID: 4010 RVA: 0x00075137 File Offset: 0x00073337
	public void VersionMatch()
	{
	}

	// Token: 0x04000F04 RID: 3844
	private static List<ZSteamSocketOLD> m_sockets = new List<ZSteamSocketOLD>();

	// Token: 0x04000F05 RID: 3845
	private static Callback<P2PSessionRequest_t> m_SessionRequest;

	// Token: 0x04000F06 RID: 3846
	private static Callback<P2PSessionConnectFail_t> m_connectionFailed;

	// Token: 0x04000F07 RID: 3847
	private Queue<ZSteamSocketOLD> m_pendingConnections = new Queue<ZSteamSocketOLD>();

	// Token: 0x04000F08 RID: 3848
	private CSteamID m_peerID = CSteamID.Nil;

	// Token: 0x04000F09 RID: 3849
	private bool m_listner;

	// Token: 0x04000F0A RID: 3850
	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	// Token: 0x04000F0B RID: 3851
	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	// Token: 0x04000F0C RID: 3852
	private int m_totalSent;

	// Token: 0x04000F0D RID: 3853
	private int m_totalRecv;

	// Token: 0x04000F0E RID: 3854
	private bool m_gotData;
}
