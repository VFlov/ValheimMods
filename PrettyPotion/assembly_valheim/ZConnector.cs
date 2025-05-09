using System;
using System.Net;
using System.Net.Sockets;

// Token: 0x020000CA RID: 202
public class ZConnector : IDisposable
{
	// Token: 0x06000C15 RID: 3093 RVA: 0x00062ED8 File Offset: 0x000610D8
	public ZConnector(string host, int port)
	{
		this.m_host = host;
		this.m_port = port;
		ZLog.Log("Zconnect " + host + " " + port.ToString());
		Dns.BeginGetHostEntry(host, new AsyncCallback(this.OnHostLookupDone), null);
	}

	// Token: 0x06000C16 RID: 3094 RVA: 0x00062F29 File Offset: 0x00061129
	public void Dispose()
	{
		this.Close();
	}

	// Token: 0x06000C17 RID: 3095 RVA: 0x00062F34 File Offset: 0x00061134
	private void Close()
	{
		if (this.m_socket != null)
		{
			try
			{
				if (this.m_socket.Connected)
				{
					this.m_socket.Shutdown(SocketShutdown.Both);
				}
			}
			catch (Exception ex)
			{
				string str = "Some excepetion when shuting down ZConnector socket, ignoring:";
				Exception ex2 = ex;
				ZLog.Log(str + ((ex2 != null) ? ex2.ToString() : null));
			}
			this.m_socket.Close();
			this.m_socket = null;
		}
		this.m_abort = true;
	}

	// Token: 0x06000C18 RID: 3096 RVA: 0x00062FAC File Offset: 0x000611AC
	public bool IsPeer(string host, int port)
	{
		return this.m_host == host && this.m_port == port;
	}

	// Token: 0x06000C19 RID: 3097 RVA: 0x00062FC8 File Offset: 0x000611C8
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
			ZLog.Log("ZConnector - result completed");
			return true;
		}
		this.m_timer += dt;
		if (this.m_timer > ZConnector.m_timeout)
		{
			ZLog.Log("ZConnector - timeout");
			this.Close();
			return true;
		}
		return false;
	}

	// Token: 0x06000C1A RID: 3098 RVA: 0x0006304C File Offset: 0x0006124C
	public ZSocket Complete()
	{
		if (this.m_socket != null && this.m_socket.Connected)
		{
			ZSocket result = new ZSocket(this.m_socket, this.m_host);
			this.m_socket = null;
			return result;
		}
		this.Close();
		return null;
	}

	// Token: 0x06000C1B RID: 3099 RVA: 0x00063083 File Offset: 0x00061283
	public bool CompareEndPoint(IPEndPoint endpoint)
	{
		return this.m_endPoint.Equals(endpoint);
	}

	// Token: 0x06000C1C RID: 3100 RVA: 0x00063094 File Offset: 0x00061294
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
		ZLog.Log("Host lookup done , addresses: " + iphostEntry.AddressList.Length.ToString());
		foreach (IPAddress ipaddress in iphostEntry.AddressList)
		{
			string str = " ";
			IPAddress ipaddress2 = ipaddress;
			ZLog.Log(str + ((ipaddress2 != null) ? ipaddress2.ToString() : null));
		}
		this.m_socket = ZSocket.CreateSocket();
		this.m_result = this.m_socket.BeginConnect(iphostEntry.AddressList, this.m_port, null, null);
	}

	// Token: 0x06000C1D RID: 3101 RVA: 0x00063154 File Offset: 0x00061354
	public string GetEndPointString()
	{
		return this.m_host + ":" + this.m_port.ToString();
	}

	// Token: 0x06000C1E RID: 3102 RVA: 0x00063171 File Offset: 0x00061371
	public string GetHostName()
	{
		return this.m_host;
	}

	// Token: 0x06000C1F RID: 3103 RVA: 0x00063179 File Offset: 0x00061379
	public int GetHostPort()
	{
		return this.m_port;
	}

	// Token: 0x04000D0E RID: 3342
	private Socket m_socket;

	// Token: 0x04000D0F RID: 3343
	private IAsyncResult m_result;

	// Token: 0x04000D10 RID: 3344
	private IPEndPoint m_endPoint;

	// Token: 0x04000D11 RID: 3345
	private string m_host;

	// Token: 0x04000D12 RID: 3346
	private int m_port;

	// Token: 0x04000D13 RID: 3347
	private bool m_dnsError;

	// Token: 0x04000D14 RID: 3348
	private bool m_abort;

	// Token: 0x04000D15 RID: 3349
	private float m_timer;

	// Token: 0x04000D16 RID: 3350
	private static float m_timeout = 5f;
}
