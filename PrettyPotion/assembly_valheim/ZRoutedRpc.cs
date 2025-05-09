using System;
using System.Collections.Generic;

// Token: 0x020000E8 RID: 232
public class ZRoutedRpc
{
	// Token: 0x1700007F RID: 127
	// (get) Token: 0x06000ED0 RID: 3792 RVA: 0x0007085B File Offset: 0x0006EA5B
	public static ZRoutedRpc instance
	{
		get
		{
			return ZRoutedRpc.s_instance;
		}
	}

	// Token: 0x06000ED1 RID: 3793 RVA: 0x00070862 File Offset: 0x0006EA62
	public ZRoutedRpc(bool server)
	{
		ZRoutedRpc.s_instance = this;
		this.m_server = server;
	}

	// Token: 0x06000ED2 RID: 3794 RVA: 0x00070894 File Offset: 0x0006EA94
	public void SetUID(long uid)
	{
		this.m_id = uid;
	}

	// Token: 0x06000ED3 RID: 3795 RVA: 0x000708A0 File Offset: 0x0006EAA0
	public void AddPeer(ZNetPeer peer)
	{
		this.m_peers.Add(peer);
		peer.m_rpc.Register<ZPackage>("RoutedRPC", new Action<ZRpc, ZPackage>(this.RPC_RoutedRPC));
		if (this.m_onNewPeer != null)
		{
			this.m_onNewPeer(peer.m_uid);
		}
	}

	// Token: 0x06000ED4 RID: 3796 RVA: 0x000708EE File Offset: 0x0006EAEE
	public void RemovePeer(ZNetPeer peer)
	{
		this.m_peers.Remove(peer);
	}

	// Token: 0x06000ED5 RID: 3797 RVA: 0x00070900 File Offset: 0x0006EB00
	private ZNetPeer GetPeer(long uid)
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

	// Token: 0x06000ED6 RID: 3798 RVA: 0x0007095C File Offset: 0x0006EB5C
	public void InvokeRoutedRPC(long targetPeerID, string methodName, params object[] parameters)
	{
		this.InvokeRoutedRPC(targetPeerID, ZDOID.None, methodName, parameters);
	}

	// Token: 0x06000ED7 RID: 3799 RVA: 0x0007096C File Offset: 0x0006EB6C
	public void InvokeRoutedRPC(string methodName, params object[] parameters)
	{
		this.InvokeRoutedRPC(this.GetServerPeerID(), methodName, parameters);
	}

	// Token: 0x06000ED8 RID: 3800 RVA: 0x0007097C File Offset: 0x0006EB7C
	private long GetServerPeerID()
	{
		if (this.m_server)
		{
			return this.m_id;
		}
		if (this.m_peers.Count > 0)
		{
			return this.m_peers[0].m_uid;
		}
		return 0L;
	}

	// Token: 0x06000ED9 RID: 3801 RVA: 0x000709B0 File Offset: 0x0006EBB0
	public void InvokeRoutedRPC(long targetPeerID, ZDOID targetZDO, string methodName, params object[] parameters)
	{
		ZRoutedRpc.RoutedRPCData routedRPCData = new ZRoutedRpc.RoutedRPCData();
		ZRoutedRpc.RoutedRPCData routedRPCData2 = routedRPCData;
		long id = this.m_id;
		int rpcMsgID = this.m_rpcMsgID;
		this.m_rpcMsgID = rpcMsgID + 1;
		routedRPCData2.m_msgID = id + (long)rpcMsgID;
		routedRPCData.m_senderPeerID = this.m_id;
		routedRPCData.m_targetPeerID = targetPeerID;
		routedRPCData.m_targetZDO = targetZDO;
		routedRPCData.m_methodHash = methodName.GetStableHashCode();
		ZRpc.Serialize(parameters, ref routedRPCData.m_parameters);
		routedRPCData.m_parameters.SetPos(0);
		if (targetPeerID == this.m_id || targetPeerID == 0L)
		{
			this.HandleRoutedRPC(routedRPCData);
		}
		if (targetPeerID != this.m_id)
		{
			this.RouteRPC(routedRPCData);
		}
	}

	// Token: 0x06000EDA RID: 3802 RVA: 0x00070A44 File Offset: 0x0006EC44
	private void RouteRPC(ZRoutedRpc.RoutedRPCData rpcData)
	{
		ZPackage zpackage = new ZPackage();
		rpcData.Serialize(zpackage);
		if (this.m_server)
		{
			if (rpcData.m_targetPeerID != 0L)
			{
				ZNetPeer peer = this.GetPeer(rpcData.m_targetPeerID);
				if (peer != null && peer.IsReady())
				{
					peer.m_rpc.Invoke("RoutedRPC", new object[]
					{
						zpackage
					});
					return;
				}
				return;
			}
			else
			{
				using (List<ZNetPeer>.Enumerator enumerator = this.m_peers.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ZNetPeer znetPeer = enumerator.Current;
						if (rpcData.m_senderPeerID != znetPeer.m_uid && znetPeer.IsReady())
						{
							znetPeer.m_rpc.Invoke("RoutedRPC", new object[]
							{
								zpackage
							});
						}
					}
					return;
				}
			}
		}
		foreach (ZNetPeer znetPeer2 in this.m_peers)
		{
			if (znetPeer2.IsReady())
			{
				znetPeer2.m_rpc.Invoke("RoutedRPC", new object[]
				{
					zpackage
				});
			}
		}
	}

	// Token: 0x06000EDB RID: 3803 RVA: 0x00070B7C File Offset: 0x0006ED7C
	private void RPC_RoutedRPC(ZRpc rpc, ZPackage pkg)
	{
		ZRoutedRpc.RoutedRPCData routedRPCData = new ZRoutedRpc.RoutedRPCData();
		routedRPCData.Deserialize(pkg);
		if (routedRPCData.m_targetPeerID == this.m_id || routedRPCData.m_targetPeerID == 0L)
		{
			this.HandleRoutedRPC(routedRPCData);
		}
		if (this.m_server && routedRPCData.m_targetPeerID != this.m_id)
		{
			this.RouteRPC(routedRPCData);
		}
	}

	// Token: 0x06000EDC RID: 3804 RVA: 0x00070BD0 File Offset: 0x0006EDD0
	private void HandleRoutedRPC(ZRoutedRpc.RoutedRPCData data)
	{
		if (data.m_targetZDO.IsNone())
		{
			RoutedMethodBase routedMethodBase;
			if (this.m_functions.TryGetValue(data.m_methodHash, out routedMethodBase))
			{
				routedMethodBase.Invoke(data.m_senderPeerID, data.m_parameters);
				return;
			}
		}
		else
		{
			ZDO zdo = ZDOMan.instance.GetZDO(data.m_targetZDO);
			if (zdo != null)
			{
				ZNetView znetView = ZNetScene.instance.FindInstance(zdo);
				if (znetView != null)
				{
					znetView.HandleRoutedRPC(data);
				}
			}
		}
	}

	// Token: 0x06000EDD RID: 3805 RVA: 0x00070C42 File Offset: 0x0006EE42
	public void Register(string name, Action<long> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod(f));
	}

	// Token: 0x06000EDE RID: 3806 RVA: 0x00070C5B File Offset: 0x0006EE5B
	public void Register<T>(string name, Action<long, T> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T>(f));
	}

	// Token: 0x06000EDF RID: 3807 RVA: 0x00070C74 File Offset: 0x0006EE74
	public void Register<T, U>(string name, Action<long, T, U> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U>(f));
	}

	// Token: 0x06000EE0 RID: 3808 RVA: 0x00070C8D File Offset: 0x0006EE8D
	public void Register<T, U, V>(string name, Action<long, T, U, V> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V>(f));
	}

	// Token: 0x06000EE1 RID: 3809 RVA: 0x00070CA6 File Offset: 0x0006EEA6
	public void Register<T, U, V, B>(string name, RoutedMethod<T, U, V, B>.Method f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V, B>(f));
	}

	// Token: 0x06000EE2 RID: 3810 RVA: 0x00070CBF File Offset: 0x0006EEBF
	public void Register<T, U, V, B, K>(string name, RoutedMethod<T, U, V, B, K>.Method f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V, B, K>(f));
	}

	// Token: 0x06000EE3 RID: 3811 RVA: 0x00070CD8 File Offset: 0x0006EED8
	public void Register<T, U, V, B, K, M>(string name, RoutedMethod<T, U, V, B, K, M>.Method f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V, B, K, M>(f));
	}

	// Token: 0x04000E97 RID: 3735
	public static long Everybody;

	// Token: 0x04000E98 RID: 3736
	public Action<long> m_onNewPeer;

	// Token: 0x04000E99 RID: 3737
	private int m_rpcMsgID = 1;

	// Token: 0x04000E9A RID: 3738
	private bool m_server;

	// Token: 0x04000E9B RID: 3739
	private long m_id;

	// Token: 0x04000E9C RID: 3740
	private readonly List<ZNetPeer> m_peers = new List<ZNetPeer>();

	// Token: 0x04000E9D RID: 3741
	private readonly Dictionary<int, RoutedMethodBase> m_functions = new Dictionary<int, RoutedMethodBase>();

	// Token: 0x04000E9E RID: 3742
	private static ZRoutedRpc s_instance;

	// Token: 0x020002EE RID: 750
	public class RoutedRPCData
	{
		// Token: 0x0600219B RID: 8603 RVA: 0x000EBA6C File Offset: 0x000E9C6C
		public void Serialize(ZPackage pkg)
		{
			pkg.Write(this.m_msgID);
			pkg.Write(this.m_senderPeerID);
			pkg.Write(this.m_targetPeerID);
			pkg.Write(this.m_targetZDO);
			pkg.Write(this.m_methodHash);
			pkg.Write(this.m_parameters);
		}

		// Token: 0x0600219C RID: 8604 RVA: 0x000EBAC4 File Offset: 0x000E9CC4
		public void Deserialize(ZPackage pkg)
		{
			this.m_msgID = pkg.ReadLong();
			this.m_senderPeerID = pkg.ReadLong();
			this.m_targetPeerID = pkg.ReadLong();
			this.m_targetZDO = pkg.ReadZDOID();
			this.m_methodHash = pkg.ReadInt();
			this.m_parameters = pkg.ReadPackage();
		}

		// Token: 0x04002369 RID: 9065
		public long m_msgID;

		// Token: 0x0400236A RID: 9066
		public long m_senderPeerID;

		// Token: 0x0400236B RID: 9067
		public long m_targetPeerID;

		// Token: 0x0400236C RID: 9068
		public ZDOID m_targetZDO;

		// Token: 0x0400236D RID: 9069
		public int m_methodHash;

		// Token: 0x0400236E RID: 9070
		public ZPackage m_parameters = new ZPackage();
	}
}
