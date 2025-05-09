using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

// Token: 0x020000C9 RID: 201
public class ZBroastcast : IDisposable
{
	// Token: 0x17000067 RID: 103
	// (get) Token: 0x06000C0A RID: 3082 RVA: 0x00062B0F File Offset: 0x00060D0F
	public static ZBroastcast instance
	{
		get
		{
			return ZBroastcast.m_instance;
		}
	}

	// Token: 0x06000C0B RID: 3083 RVA: 0x00062B16 File Offset: 0x00060D16
	public static void Initialize()
	{
		if (ZBroastcast.m_instance == null)
		{
			ZBroastcast.m_instance = new ZBroastcast();
		}
	}

	// Token: 0x06000C0C RID: 3084 RVA: 0x00062B2C File Offset: 0x00060D2C
	private ZBroastcast()
	{
		ZLog.Log("opening zbroadcast");
		this.m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		this.m_socket.EnableBroadcast = true;
		try
		{
			this.m_listner = new UdpClient(6542);
			this.m_listner.EnableBroadcast = true;
			this.m_listner.BeginReceive(new AsyncCallback(this.GotPackage), null);
		}
		catch (Exception ex)
		{
			this.m_listner = null;
			ZLog.Log("Error creating zbroadcast socket " + ex.ToString());
		}
	}

	// Token: 0x06000C0D RID: 3085 RVA: 0x00062BE0 File Offset: 0x00060DE0
	public void SetServerPort(int port)
	{
		this.m_myPort = port;
	}

	// Token: 0x06000C0E RID: 3086 RVA: 0x00062BEC File Offset: 0x00060DEC
	public void Dispose()
	{
		ZLog.Log("Clozing zbroadcast");
		if (this.m_listner != null)
		{
			this.m_listner.Close();
		}
		this.m_socket.Close();
		this.m_lock.Close();
		if (ZBroastcast.m_instance == this)
		{
			ZBroastcast.m_instance = null;
		}
	}

	// Token: 0x06000C0F RID: 3087 RVA: 0x00062C3A File Offset: 0x00060E3A
	public void Update(float dt)
	{
		this.m_timer -= dt;
		if (this.m_timer <= 0f)
		{
			this.m_timer = 5f;
			if (this.m_myPort != 0)
			{
				this.Ping();
			}
		}
		this.TimeoutHosts(dt);
	}

	// Token: 0x06000C10 RID: 3088 RVA: 0x00062C78 File Offset: 0x00060E78
	private void GotPackage(IAsyncResult ar)
	{
		IPEndPoint ipendPoint = new IPEndPoint(0L, 0);
		byte[] array;
		try
		{
			array = this.m_listner.EndReceive(ar, ref ipendPoint);
		}
		catch (ObjectDisposedException)
		{
			return;
		}
		if (array.Length < 5)
		{
			return;
		}
		ZPackage zpackage = new ZPackage(array);
		if (zpackage.ReadChar() != 'F')
		{
			return;
		}
		if (zpackage.ReadChar() != 'E')
		{
			return;
		}
		if (zpackage.ReadChar() != 'J')
		{
			return;
		}
		if (zpackage.ReadChar() != 'D')
		{
			return;
		}
		int port = zpackage.ReadInt();
		this.m_lock.WaitOne();
		this.AddHost(ipendPoint.Address.ToString(), port);
		this.m_lock.ReleaseMutex();
		this.m_listner.BeginReceive(new AsyncCallback(this.GotPackage), null);
	}

	// Token: 0x06000C11 RID: 3089 RVA: 0x00062D38 File Offset: 0x00060F38
	private void Ping()
	{
		IPEndPoint remoteEP = new IPEndPoint(IPAddress.Broadcast, 6542);
		ZPackage zpackage = new ZPackage();
		zpackage.Write('F');
		zpackage.Write('E');
		zpackage.Write('J');
		zpackage.Write('D');
		zpackage.Write(this.m_myPort);
		this.m_socket.SendTo(zpackage.GetArray(), remoteEP);
	}

	// Token: 0x06000C12 RID: 3090 RVA: 0x00062D9C File Offset: 0x00060F9C
	private void AddHost(string host, int port)
	{
		foreach (ZBroastcast.HostData hostData in this.m_hosts)
		{
			if (hostData.m_port == port && hostData.m_host == host)
			{
				hostData.m_timeout = 0f;
				return;
			}
		}
		ZBroastcast.HostData hostData2 = new ZBroastcast.HostData();
		hostData2.m_host = host;
		hostData2.m_port = port;
		hostData2.m_timeout = 0f;
		this.m_hosts.Add(hostData2);
	}

	// Token: 0x06000C13 RID: 3091 RVA: 0x00062E38 File Offset: 0x00061038
	private void TimeoutHosts(float dt)
	{
		this.m_lock.WaitOne();
		foreach (ZBroastcast.HostData hostData in this.m_hosts)
		{
			hostData.m_timeout += dt;
			if (hostData.m_timeout > 10f)
			{
				this.m_hosts.Remove(hostData);
				return;
			}
		}
		this.m_lock.ReleaseMutex();
	}

	// Token: 0x06000C14 RID: 3092 RVA: 0x00062EC8 File Offset: 0x000610C8
	public void GetHostList(List<ZBroastcast.HostData> hosts)
	{
		hosts.AddRange(this.m_hosts);
	}

	// Token: 0x04000D04 RID: 3332
	private List<ZBroastcast.HostData> m_hosts = new List<ZBroastcast.HostData>();

	// Token: 0x04000D05 RID: 3333
	private static ZBroastcast m_instance;

	// Token: 0x04000D06 RID: 3334
	private const int m_port = 6542;

	// Token: 0x04000D07 RID: 3335
	private const float m_pingInterval = 5f;

	// Token: 0x04000D08 RID: 3336
	private const float m_hostTimeout = 10f;

	// Token: 0x04000D09 RID: 3337
	private float m_timer;

	// Token: 0x04000D0A RID: 3338
	private int m_myPort;

	// Token: 0x04000D0B RID: 3339
	private Socket m_socket;

	// Token: 0x04000D0C RID: 3340
	private UdpClient m_listner;

	// Token: 0x04000D0D RID: 3341
	private Mutex m_lock = new Mutex();

	// Token: 0x020002D8 RID: 728
	public class HostData
	{
		// Token: 0x04002313 RID: 8979
		public string m_host;

		// Token: 0x04002314 RID: 8980
		public int m_port;

		// Token: 0x04002315 RID: 8981
		public float m_timeout;
	}
}
