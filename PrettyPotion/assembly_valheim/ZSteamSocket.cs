using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Steamworks;
using UnityEngine;

// Token: 0x020000ED RID: 237
public class ZSteamSocket : IDisposable, ISocket
{
	// Token: 0x06000F65 RID: 3941 RVA: 0x00073D8C File Offset: 0x00071F8C
	public ZSteamSocket()
	{
		ZSteamSocket.RegisterGlobalCallbacks();
		ZSteamSocket.m_sockets.Add(this);
	}

	// Token: 0x06000F66 RID: 3942 RVA: 0x00073DE8 File Offset: 0x00071FE8
	public ZSteamSocket(SteamNetworkingIPAddr host)
	{
		ZSteamSocket.RegisterGlobalCallbacks();
		string str;
		host.ToString(out str, true);
		ZLog.Log("Starting to connect to " + str);
		this.m_con = SteamNetworkingSockets.ConnectByIPAddress(ref host, 0, null);
		ZSteamSocket.m_sockets.Add(this);
	}

	// Token: 0x06000F67 RID: 3943 RVA: 0x00073E6C File Offset: 0x0007206C
	public ZSteamSocket(CSteamID peerID)
	{
		ZSteamSocket.RegisterGlobalCallbacks();
		this.m_peerID.SetSteamID(peerID);
		this.m_con = SteamNetworkingSockets.ConnectP2P(ref this.m_peerID, 0, 0, null);
		ZLog.Log("Connecting to " + this.m_peerID.GetSteamID().ToString());
		ZSteamSocket.m_sockets.Add(this);
	}

	// Token: 0x06000F68 RID: 3944 RVA: 0x00073F10 File Offset: 0x00072110
	public ZSteamSocket(HSteamNetConnection con)
	{
		ZSteamSocket.RegisterGlobalCallbacks();
		this.m_con = con;
		SteamNetConnectionInfo_t steamNetConnectionInfo_t;
		SteamNetworkingSockets.GetConnectionInfo(this.m_con, out steamNetConnectionInfo_t);
		this.m_peerID = steamNetConnectionInfo_t.m_identityRemote;
		ZLog.Log("Connecting to " + this.m_peerID.ToString());
		ZSteamSocket.m_sockets.Add(this);
	}

	// Token: 0x06000F69 RID: 3945 RVA: 0x00073FAC File Offset: 0x000721AC
	private static void RegisterGlobalCallbacks()
	{
		if (ZSteamSocket.m_statusChanged == null)
		{
			ZSteamSocket.m_statusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(new Callback<SteamNetConnectionStatusChangedCallback_t>.DispatchDelegate(ZSteamSocket.OnStatusChanged));
			GCHandle gchandle = GCHandle.Alloc(30000f, GCHandleType.Pinned);
			GCHandle gchandle2 = GCHandle.Alloc(1, GCHandleType.Pinned);
			GCHandle gchandle3 = GCHandle.Alloc(153600, GCHandleType.Pinned);
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float, gchandle.AddrOfPinnedObject());
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_IP_AllowWithoutAuth, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gchandle2.AddrOfPinnedObject());
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gchandle3.AddrOfPinnedObject());
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gchandle3.AddrOfPinnedObject());
			gchandle.Free();
			gchandle2.Free();
			gchandle3.Free();
		}
	}

	// Token: 0x06000F6A RID: 3946 RVA: 0x00074078 File Offset: 0x00072278
	private static void UnregisterGlobalCallbacks()
	{
		ZLog.Log("ZSteamSocket  UnregisterGlobalCallbacks, existing sockets:" + ZSteamSocket.m_sockets.Count.ToString());
		if (ZSteamSocket.m_statusChanged != null)
		{
			ZSteamSocket.m_statusChanged.Dispose();
			ZSteamSocket.m_statusChanged = null;
		}
	}

	// Token: 0x06000F6B RID: 3947 RVA: 0x000740C0 File Offset: 0x000722C0
	private static void OnStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
	{
		ZLog.Log("Got status changed msg " + data.m_info.m_eState.ToString());
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected && data.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
		{
			ZLog.Log("Connected");
			ZSteamSocket zsteamSocket = ZSteamSocket.FindSocket(data.m_hConn);
			if (zsteamSocket != null)
			{
				SteamNetConnectionInfo_t steamNetConnectionInfo_t;
				if (SteamNetworkingSockets.GetConnectionInfo(data.m_hConn, out steamNetConnectionInfo_t))
				{
					zsteamSocket.m_peerID = steamNetConnectionInfo_t.m_identityRemote;
				}
				ZLog.Log("Got connection SteamID " + zsteamSocket.m_peerID.GetSteamID().ToString());
			}
		}
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting && data.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None)
		{
			ZLog.Log("New connection");
			ZSteamSocket listner = ZSteamSocket.GetListner();
			if (listner != null)
			{
				listner.OnNewConnection(data.m_hConn);
			}
		}
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
		{
			ZLog.Log("Got problem " + data.m_info.m_eEndReason.ToString() + ":" + data.m_info.m_szEndDebug);
			ZSteamSocket zsteamSocket2 = ZSteamSocket.FindSocket(data.m_hConn);
			if (zsteamSocket2 != null)
			{
				ZLog.Log("  Closing socket " + zsteamSocket2.GetHostName());
				zsteamSocket2.Close();
			}
		}
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer)
		{
			ZLog.Log("Socket closed by peer " + data.ToString());
			ZSteamSocket zsteamSocket3 = ZSteamSocket.FindSocket(data.m_hConn);
			if (zsteamSocket3 != null)
			{
				ZLog.Log("  Closing socket " + zsteamSocket3.GetHostName());
				zsteamSocket3.Close();
			}
		}
	}

	// Token: 0x06000F6C RID: 3948 RVA: 0x00074260 File Offset: 0x00072460
	private static ZSteamSocket FindSocket(HSteamNetConnection con)
	{
		foreach (ZSteamSocket zsteamSocket in ZSteamSocket.m_sockets)
		{
			if (zsteamSocket.m_con == con)
			{
				return zsteamSocket;
			}
		}
		return null;
	}

	// Token: 0x06000F6D RID: 3949 RVA: 0x000742C0 File Offset: 0x000724C0
	public void Dispose()
	{
		ZLog.Log("Disposing socket");
		this.Close();
		this.m_pkgQueue.Clear();
		ZSteamSocket.m_sockets.Remove(this);
		if (ZSteamSocket.m_sockets.Count == 0)
		{
			ZLog.Log("Last socket, unregistering callback");
			ZSteamSocket.UnregisterGlobalCallbacks();
		}
	}

	// Token: 0x06000F6E RID: 3950 RVA: 0x00074310 File Offset: 0x00072510
	public void Close()
	{
		if (this.m_con != HSteamNetConnection.Invalid)
		{
			ZLog.Log("Closing socket " + this.GetEndPointString());
			this.Flush();
			ZLog.Log("  send queue size:" + this.m_sendQueue.Count.ToString());
			Thread.Sleep(100);
			CSteamID steamID = this.m_peerID.GetSteamID();
			SteamNetworkingSockets.CloseConnection(this.m_con, 0, "", false);
			SteamUser.EndAuthSession(steamID);
			this.m_con = HSteamNetConnection.Invalid;
		}
		if (this.m_listenSocket != HSteamListenSocket.Invalid)
		{
			ZLog.Log("Stopping listening socket");
			SteamNetworkingSockets.CloseListenSocket(this.m_listenSocket);
			this.m_listenSocket = HSteamListenSocket.Invalid;
		}
		if (ZSteamSocket.m_hostSocket == this)
		{
			ZSteamSocket.m_hostSocket = null;
		}
		this.m_peerID.Clear();
	}

	// Token: 0x06000F6F RID: 3951 RVA: 0x000743EE File Offset: 0x000725EE
	public bool StartHost()
	{
		if (ZSteamSocket.m_hostSocket != null)
		{
			ZLog.Log("Listen socket already started");
			return false;
		}
		this.m_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
		ZSteamSocket.m_hostSocket = this;
		this.m_pendingConnections.Clear();
		return true;
	}

	// Token: 0x06000F70 RID: 3952 RVA: 0x00074424 File Offset: 0x00072624
	private void OnNewConnection(HSteamNetConnection con)
	{
		EResult eresult = SteamNetworkingSockets.AcceptConnection(con);
		ZLog.Log("Accepting connection " + eresult.ToString());
		if (eresult == EResult.k_EResultOK)
		{
			this.QueuePendingConnection(con);
		}
	}

	// Token: 0x06000F71 RID: 3953 RVA: 0x00074460 File Offset: 0x00072660
	private void QueuePendingConnection(HSteamNetConnection con)
	{
		ZSteamSocket item = new ZSteamSocket(con);
		this.m_pendingConnections.Enqueue(item);
	}

	// Token: 0x06000F72 RID: 3954 RVA: 0x00074480 File Offset: 0x00072680
	public ISocket Accept()
	{
		if (this.m_listenSocket == HSteamListenSocket.Invalid)
		{
			return null;
		}
		if (this.m_pendingConnections.Count > 0)
		{
			return this.m_pendingConnections.Dequeue();
		}
		return null;
	}

	// Token: 0x06000F73 RID: 3955 RVA: 0x000744B1 File Offset: 0x000726B1
	public bool IsConnected()
	{
		return this.m_con != HSteamNetConnection.Invalid;
	}

	// Token: 0x06000F74 RID: 3956 RVA: 0x000744C4 File Offset: 0x000726C4
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
		this.m_sendQueue.Enqueue(array);
		this.SendQueuedPackages();
	}

	// Token: 0x06000F75 RID: 3957 RVA: 0x000744FC File Offset: 0x000726FC
	public bool Flush()
	{
		this.SendQueuedPackages();
		HSteamNetConnection con = this.m_con;
		SteamNetworkingSockets.FlushMessagesOnConnection(this.m_con);
		return this.m_sendQueue.Count == 0;
	}

	// Token: 0x06000F76 RID: 3958 RVA: 0x00074528 File Offset: 0x00072728
	private void SendQueuedPackages()
	{
		if (!this.IsConnected())
		{
			return;
		}
		while (this.m_sendQueue.Count > 0)
		{
			byte[] array = this.m_sendQueue.Peek();
			IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
			Marshal.Copy(array, 0, intPtr, array.Length);
			long num;
			EResult eresult = SteamNetworkingSockets.SendMessageToConnection(this.m_con, intPtr, (uint)array.Length, 8, out num);
			Marshal.FreeHGlobal(intPtr);
			if (eresult != EResult.k_EResultOK)
			{
				ZLog.Log("Failed to send data " + eresult.ToString());
				return;
			}
			this.m_totalSent += array.Length;
			this.m_sendQueue.Dequeue();
		}
	}

	// Token: 0x06000F77 RID: 3959 RVA: 0x000745C8 File Offset: 0x000727C8
	public static void UpdateAllSockets(float dt)
	{
		foreach (ZSteamSocket zsteamSocket in ZSteamSocket.m_sockets)
		{
			zsteamSocket.Update(dt);
		}
	}

	// Token: 0x06000F78 RID: 3960 RVA: 0x00074618 File Offset: 0x00072818
	private void Update(float dt)
	{
		this.SendQueuedPackages();
	}

	// Token: 0x06000F79 RID: 3961 RVA: 0x00074620 File Offset: 0x00072820
	private static ZSteamSocket GetListner()
	{
		return ZSteamSocket.m_hostSocket;
	}

	// Token: 0x06000F7A RID: 3962 RVA: 0x00074628 File Offset: 0x00072828
	public ZPackage Recv()
	{
		if (!this.IsConnected())
		{
			return null;
		}
		IntPtr[] array = new IntPtr[1];
		if (SteamNetworkingSockets.ReceiveMessagesOnConnection(this.m_con, array, 1) == 1)
		{
			SteamNetworkingMessage_t steamNetworkingMessage_t = Marshal.PtrToStructure<SteamNetworkingMessage_t>(array[0]);
			byte[] array2 = new byte[steamNetworkingMessage_t.m_cbSize];
			Marshal.Copy(steamNetworkingMessage_t.m_pData, array2, 0, steamNetworkingMessage_t.m_cbSize);
			ZPackage zpackage = new ZPackage(array2);
			SteamNetworkingMessage_t.Release(array[0]);
			this.m_totalRecv += zpackage.Size();
			this.m_gotData = true;
			return zpackage;
		}
		return null;
	}

	// Token: 0x06000F7B RID: 3963 RVA: 0x000746AC File Offset: 0x000728AC
	public string GetEndPointString()
	{
		return this.m_peerID.GetSteamID().ToString();
	}

	// Token: 0x06000F7C RID: 3964 RVA: 0x000746D4 File Offset: 0x000728D4
	public string GetHostName()
	{
		return this.m_peerID.GetSteamID().ToString();
	}

	// Token: 0x06000F7D RID: 3965 RVA: 0x000746FA File Offset: 0x000728FA
	public CSteamID GetPeerID()
	{
		return this.m_peerID.GetSteamID();
	}

	// Token: 0x06000F7E RID: 3966 RVA: 0x00074707 File Offset: 0x00072907
	public bool IsHost()
	{
		return ZSteamSocket.m_hostSocket != null;
	}

	// Token: 0x06000F7F RID: 3967 RVA: 0x00074714 File Offset: 0x00072914
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
		SteamNetConnectionRealTimeStatus_t steamNetConnectionRealTimeStatus_t = default(SteamNetConnectionRealTimeStatus_t);
		SteamNetConnectionRealTimeLaneStatus_t steamNetConnectionRealTimeLaneStatus_t = default(SteamNetConnectionRealTimeLaneStatus_t);
		if (SteamNetworkingSockets.GetConnectionRealTimeStatus(this.m_con, ref steamNetConnectionRealTimeStatus_t, 0, ref steamNetConnectionRealTimeLaneStatus_t) == EResult.k_EResultOK)
		{
			num += steamNetConnectionRealTimeStatus_t.m_cbPendingReliable + steamNetConnectionRealTimeStatus_t.m_cbPendingUnreliable + steamNetConnectionRealTimeStatus_t.m_cbSentUnackedReliable;
		}
		return num;
	}

	// Token: 0x06000F80 RID: 3968 RVA: 0x000747B0 File Offset: 0x000729B0
	public int GetCurrentSendRate()
	{
		SteamNetConnectionRealTimeStatus_t steamNetConnectionRealTimeStatus_t = default(SteamNetConnectionRealTimeStatus_t);
		SteamNetConnectionRealTimeLaneStatus_t steamNetConnectionRealTimeLaneStatus_t = default(SteamNetConnectionRealTimeLaneStatus_t);
		if (SteamNetworkingSockets.GetConnectionRealTimeStatus(this.m_con, ref steamNetConnectionRealTimeStatus_t, 0, ref steamNetConnectionRealTimeLaneStatus_t) != EResult.k_EResultOK)
		{
			return 0;
		}
		int num = steamNetConnectionRealTimeStatus_t.m_cbPendingReliable + steamNetConnectionRealTimeStatus_t.m_cbPendingUnreliable + steamNetConnectionRealTimeStatus_t.m_cbSentUnackedReliable;
		foreach (byte[] array in this.m_sendQueue)
		{
			num += array.Length;
		}
		return num / Mathf.Clamp(steamNetConnectionRealTimeStatus_t.m_nPing, 5, 250) * 1000;
	}

	// Token: 0x06000F81 RID: 3969 RVA: 0x00074858 File Offset: 0x00072A58
	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		SteamNetConnectionRealTimeStatus_t steamNetConnectionRealTimeStatus_t = default(SteamNetConnectionRealTimeStatus_t);
		SteamNetConnectionRealTimeLaneStatus_t steamNetConnectionRealTimeLaneStatus_t = default(SteamNetConnectionRealTimeLaneStatus_t);
		if (SteamNetworkingSockets.GetConnectionRealTimeStatus(this.m_con, ref steamNetConnectionRealTimeStatus_t, 0, ref steamNetConnectionRealTimeLaneStatus_t) == EResult.k_EResultOK)
		{
			localQuality = steamNetConnectionRealTimeStatus_t.m_flConnectionQualityLocal;
			remoteQuality = steamNetConnectionRealTimeStatus_t.m_flConnectionQualityRemote;
			ping = steamNetConnectionRealTimeStatus_t.m_nPing;
			outByteSec = steamNetConnectionRealTimeStatus_t.m_flOutBytesPerSec;
			inByteSec = steamNetConnectionRealTimeStatus_t.m_flInBytesPerSec;
			return;
		}
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
	}

	// Token: 0x06000F82 RID: 3970 RVA: 0x000748D4 File Offset: 0x00072AD4
	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = this.m_totalSent;
		totalRecv = this.m_totalRecv;
		this.m_totalSent = 0;
		this.m_totalRecv = 0;
	}

	// Token: 0x06000F83 RID: 3971 RVA: 0x000748F4 File Offset: 0x00072AF4
	public bool GotNewData()
	{
		bool gotData = this.m_gotData;
		this.m_gotData = false;
		return gotData;
	}

	// Token: 0x06000F84 RID: 3972 RVA: 0x00074903 File Offset: 0x00072B03
	public int GetHostPort()
	{
		if (this.IsHost())
		{
			return 1;
		}
		return -1;
	}

	// Token: 0x06000F85 RID: 3973 RVA: 0x00074910 File Offset: 0x00072B10
	public static void SetDataPort(int port)
	{
		ZSteamSocket.m_steamDataPort = port;
	}

	// Token: 0x06000F86 RID: 3974 RVA: 0x00074918 File Offset: 0x00072B18
	public void VersionMatch()
	{
	}

	// Token: 0x04000EF6 RID: 3830
	private static List<ZSteamSocket> m_sockets = new List<ZSteamSocket>();

	// Token: 0x04000EF7 RID: 3831
	private static Callback<SteamNetConnectionStatusChangedCallback_t> m_statusChanged;

	// Token: 0x04000EF8 RID: 3832
	private static int m_steamDataPort = 2459;

	// Token: 0x04000EF9 RID: 3833
	private Queue<ZSteamSocket> m_pendingConnections = new Queue<ZSteamSocket>();

	// Token: 0x04000EFA RID: 3834
	private HSteamNetConnection m_con = HSteamNetConnection.Invalid;

	// Token: 0x04000EFB RID: 3835
	private SteamNetworkingIdentity m_peerID;

	// Token: 0x04000EFC RID: 3836
	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	// Token: 0x04000EFD RID: 3837
	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	// Token: 0x04000EFE RID: 3838
	private int m_totalSent;

	// Token: 0x04000EFF RID: 3839
	private int m_totalRecv;

	// Token: 0x04000F00 RID: 3840
	private bool m_gotData;

	// Token: 0x04000F01 RID: 3841
	private HSteamListenSocket m_listenSocket = HSteamListenSocket.Invalid;

	// Token: 0x04000F02 RID: 3842
	private static ZSteamSocket m_hostSocket;

	// Token: 0x04000F03 RID: 3843
	private static ESteamNetworkingConfigValue[] m_configValues = new ESteamNetworkingConfigValue[1];
}
