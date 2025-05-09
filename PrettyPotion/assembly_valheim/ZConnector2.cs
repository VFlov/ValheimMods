using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

// Token: 0x020000CB RID: 203
public class ZConnector2 : IDisposable
{
	// Token: 0x06000C21 RID: 3105 RVA: 0x0006318D File Offset: 0x0006138D
	public ZConnector2(string host, int port)
	{
		this.m_host = host;
		this.m_port = port;
		Dns.BeginGetHostEntry(host, new AsyncCallback(this.OnHostLookupDone), null);
	}

	// Token: 0x06000C22 RID: 3106 RVA: 0x000631B7 File Offset: 0x000613B7
	public void Dispose()
	{
		this.Close();
	}

	// Token: 0x06000C23 RID: 3107 RVA: 0x000631BF File Offset: 0x000613BF
	private void Close()
	{
		if (this.m_socket != null)
		{
			this.m_socket.Close();
			this.m_socket = null;
		}
		this.m_abort = true;
	}

	// Token: 0x06000C24 RID: 3108 RVA: 0x000631E2 File Offset: 0x000613E2
	public bool IsPeer(string host, int port)
	{
		return this.m_host == host && this.m_port == port;
	}

	// Token: 0x06000C25 RID: 3109 RVA: 0x00063200 File Offset: 0x00061400
	public bool UpdateStatus(float dt, bool logErrors = false)
	{
		if (this.m_abort)
		{
			ZLog.Log("ZConnector - Abort");
			return true;
		}
		if (this.m_dnsError)
		{
			ZLog.Log("ZConnector - dns error");
			return true;
		}
		if (this.m_result != null && this.m_result.IsCompleted)
		{
			return true;
		}
		this.m_timer += dt;
		if (this.m_timer > ZConnector2.m_timeout)
		{
			this.Close();
			return true;
		}
		return false;
	}

	// Token: 0x06000C26 RID: 3110 RVA: 0x00063270 File Offset: 0x00061470
	public ZSocket2 Complete()
	{
		if (this.m_socket != null && this.m_socket.Connected)
		{
			ZSocket2 result = new ZSocket2(this.m_socket, this.m_host);
			this.m_socket = null;
			return result;
		}
		this.Close();
		return null;
	}

	// Token: 0x06000C27 RID: 3111 RVA: 0x000632A7 File Offset: 0x000614A7
	public bool CompareEndPoint(IPEndPoint endpoint)
	{
		return this.m_endPoint.Equals(endpoint);
	}

	// Token: 0x06000C28 RID: 3112 RVA: 0x000632B8 File Offset: 0x000614B8
	private void OnHostLookupDone(IAsyncResult res)
	{
		IPHostEntry iphostEntry = Dns.EndGetHostEntry(res);
		if (this.m_abort)
		{
			ZLog.Log("Host lookup abort");
			return;
		}
		if (iphostEntry.AddressList.Length == 0)
		{
			this.m_dnsError = true;
			ZLog.Log("Host lookup adress list empty");
			return;
		}
		iphostEntry.AddressList = this.KeepInetAddrs(iphostEntry.AddressList);
		this.m_socket = ZSocket2.CreateSocket();
		this.m_result = this.m_socket.BeginConnect(iphostEntry.AddressList, this.m_port, null, null);
	}

	// Token: 0x06000C29 RID: 3113 RVA: 0x00063338 File Offset: 0x00061538
	private IPAddress[] KeepInetAddrs(IPAddress[] inetAddrs)
	{
		List<IPAddress> list = new List<IPAddress>();
		foreach (IPAddress ipaddress in inetAddrs)
		{
			if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
			{
				list.Add(ipaddress);
			}
		}
		return list.ToArray();
	}

	// Token: 0x06000C2A RID: 3114 RVA: 0x00063375 File Offset: 0x00061575
	public string GetEndPointString()
	{
		return this.m_host + ":" + this.m_port.ToString();
	}

	// Token: 0x06000C2B RID: 3115 RVA: 0x00063392 File Offset: 0x00061592
	public string GetHostName()
	{
		return this.m_host;
	}

	// Token: 0x06000C2C RID: 3116 RVA: 0x0006339A File Offset: 0x0006159A
	public int GetHostPort()
	{
		return this.m_port;
	}

	// Token: 0x04000D17 RID: 3351
	private TcpClient m_socket;

	// Token: 0x04000D18 RID: 3352
	private IAsyncResult m_result;

	// Token: 0x04000D19 RID: 3353
	private IPEndPoint m_endPoint;

	// Token: 0x04000D1A RID: 3354
	private string m_host;

	// Token: 0x04000D1B RID: 3355
	private int m_port;

	// Token: 0x04000D1C RID: 3356
	private bool m_dnsError;

	// Token: 0x04000D1D RID: 3357
	private bool m_abort;

	// Token: 0x04000D1E RID: 3358
	private float m_timer;

	// Token: 0x04000D1F RID: 3359
	private static float m_timeout = 5f;
}
