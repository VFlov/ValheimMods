using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000D8 RID: 216
public class ZNetPeer : IDisposable
{
	// Token: 0x06000D8D RID: 3469 RVA: 0x0006A140 File Offset: 0x00068340
	public ZNetPeer(ISocket socket, bool server)
	{
		this.m_socket = socket;
		this.m_rpc = new ZRpc(this.m_socket);
		this.m_server = server;
	}

	// Token: 0x06000D8E RID: 3470 RVA: 0x0006A19E File Offset: 0x0006839E
	public void Dispose()
	{
		this.m_socket.Dispose();
		this.m_rpc.Dispose();
	}

	// Token: 0x06000D8F RID: 3471 RVA: 0x0006A1B6 File Offset: 0x000683B6
	public bool IsReady()
	{
		return this.m_uid != 0L;
	}

	// Token: 0x06000D90 RID: 3472 RVA: 0x0006A1C2 File Offset: 0x000683C2
	public Vector3 GetRefPos()
	{
		return this.m_refPos;
	}

	// Token: 0x04000E20 RID: 3616
	public ZRpc m_rpc;

	// Token: 0x04000E21 RID: 3617
	public ISocket m_socket;

	// Token: 0x04000E22 RID: 3618
	public long m_uid;

	// Token: 0x04000E23 RID: 3619
	public bool m_server;

	// Token: 0x04000E24 RID: 3620
	public Vector3 m_refPos = Vector3.zero;

	// Token: 0x04000E25 RID: 3621
	public bool m_publicRefPos;

	// Token: 0x04000E26 RID: 3622
	public ZDOID m_characterID = ZDOID.None;

	// Token: 0x04000E27 RID: 3623
	public Dictionary<string, string> m_serverSyncedPlayerData = new Dictionary<string, string>();

	// Token: 0x04000E28 RID: 3624
	public string m_playerName = "";
}
