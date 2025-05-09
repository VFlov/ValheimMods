using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

// Token: 0x020000EB RID: 235
public class ZSocket2 : ZNetStats, IDisposable, ISocket
{
	// Token: 0x06000F16 RID: 3862 RVA: 0x00072089 File Offset: 0x00070289
	public ZSocket2()
	{
	}

	// Token: 0x06000F17 RID: 3863 RVA: 0x000720C9 File Offset: 0x000702C9
	public static TcpClient CreateSocket()
	{
		TcpClient tcpClient = new TcpClient(AddressFamily.InterNetwork);
		ZSocket2.ConfigureSocket(tcpClient);
		return tcpClient;
	}

	// Token: 0x06000F18 RID: 3864 RVA: 0x000720D7 File Offset: 0x000702D7
	private static void ConfigureSocket(TcpClient socket)
	{
		socket.NoDelay = true;
		socket.SendBufferSize = 2048;
	}

	// Token: 0x06000F19 RID: 3865 RVA: 0x000720EC File Offset: 0x000702EC
	public ZSocket2(TcpClient socket, string originalHostName = null)
	{
		this.m_socket = socket;
		this.m_originalHostName = originalHostName;
		try
		{
			this.m_endpoint = (this.m_socket.Client.RemoteEndPoint as IPEndPoint);
		}
		catch
		{
			this.Close();
			return;
		}
		this.BeginReceive();
	}

	// Token: 0x06000F1A RID: 3866 RVA: 0x00072184 File Offset: 0x00070384
	public void Dispose()
	{
		this.Close();
		this.m_mutex.Close();
		this.m_sendMutex.Close();
		this.m_recvBuffer = null;
	}

	// Token: 0x06000F1B RID: 3867 RVA: 0x000721AC File Offset: 0x000703AC
	public void Close()
	{
		ZLog.Log("Closing socket " + this.GetEndPointString());
		if (this.m_listner != null)
		{
			this.m_listner.Stop();
			this.m_listner = null;
		}
		if (this.m_socket != null)
		{
			this.m_socket.Close();
			this.m_socket = null;
		}
		this.m_endpoint = null;
	}

	// Token: 0x06000F1C RID: 3868 RVA: 0x00072209 File Offset: 0x00070409
	public static IPEndPoint GetEndPoint(string host, int port)
	{
		return new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
	}

	// Token: 0x06000F1D RID: 3869 RVA: 0x0007221E File Offset: 0x0007041E
	public bool StartHost(int port)
	{
		if (this.m_listner != null)
		{
			this.m_listner.Stop();
			this.m_listner = null;
		}
		if (!this.BindSocket(port, port + 10))
		{
			ZLog.LogWarning("Failed to bind socket");
			return false;
		}
		return true;
	}

	// Token: 0x06000F1E RID: 3870 RVA: 0x00072254 File Offset: 0x00070454
	private bool BindSocket(int startPort, int endPort)
	{
		for (int i = startPort; i <= endPort; i++)
		{
			try
			{
				this.m_listner = new TcpListener(IPAddress.Any, i);
				this.m_listner.Start();
				this.m_listenPort = i;
				ZLog.Log("Bound socket port " + i.ToString());
				return true;
			}
			catch
			{
				ZLog.Log("Failed to bind port:" + i.ToString());
				this.m_listner = null;
			}
		}
		return false;
	}

	// Token: 0x06000F1F RID: 3871 RVA: 0x000722E0 File Offset: 0x000704E0
	private void BeginReceive()
	{
		this.m_recvSizeOffset = 0;
		this.m_socket.GetStream().BeginRead(this.m_recvSizeBuffer, 0, this.m_recvSizeBuffer.Length, new AsyncCallback(this.PkgSizeReceived), this.m_socket);
	}

	// Token: 0x06000F20 RID: 3872 RVA: 0x0007231C File Offset: 0x0007051C
	private void PkgSizeReceived(IAsyncResult res)
	{
		if (this.m_socket == null || !this.m_socket.Connected)
		{
			ZLog.LogWarning("PkgSizeReceived socket closed");
			this.Close();
			return;
		}
		int num;
		try
		{
			num = this.m_socket.GetStream().EndRead(res);
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("PkgSizeReceived exception " + ex.ToString());
			this.Close();
			return;
		}
		if (num == 0)
		{
			ZLog.LogWarning("PkgSizeReceived Got 0 bytes data,closing socket");
			this.Close();
			return;
		}
		this.m_gotData = true;
		this.m_recvSizeOffset += num;
		if (this.m_recvSizeOffset < this.m_recvSizeBuffer.Length)
		{
			int count = this.m_recvSizeBuffer.Length - this.m_recvOffset;
			this.m_socket.GetStream().BeginRead(this.m_recvSizeBuffer, this.m_recvSizeOffset, count, new AsyncCallback(this.PkgSizeReceived), this.m_socket);
			return;
		}
		int num2 = BitConverter.ToInt32(this.m_recvSizeBuffer, 0);
		if (num2 == 0 || num2 > 10485760)
		{
			ZLog.LogError("PkgSizeReceived Invalid pkg size " + num2.ToString());
			return;
		}
		this.m_lastRecvPkgSize = num2;
		this.m_recvOffset = 0;
		this.m_lastRecvPkgSize = num2;
		if (this.m_recvBuffer == null)
		{
			this.m_recvBuffer = new byte[ZSocket2.m_maxRecvBuffer];
		}
		this.m_socket.GetStream().BeginRead(this.m_recvBuffer, this.m_recvOffset, this.m_lastRecvPkgSize, new AsyncCallback(this.PkgReceived), this.m_socket);
	}

	// Token: 0x06000F21 RID: 3873 RVA: 0x000724A0 File Offset: 0x000706A0
	private void PkgReceived(IAsyncResult res)
	{
		int num;
		try
		{
			num = this.m_socket.GetStream().EndRead(res);
		}
		catch (Exception ex)
		{
			ZLog.Log("PkgReceived error " + ex.ToString());
			this.Close();
			return;
		}
		if (num == 0)
		{
			ZLog.LogWarning("PkgReceived: Got 0 bytes data,closing socket");
			this.Close();
			return;
		}
		this.m_gotData = true;
		this.m_totalRecv += num;
		this.m_recvOffset += num;
		base.IncRecvBytes(num);
		if (this.m_recvOffset < this.m_lastRecvPkgSize)
		{
			int count = this.m_lastRecvPkgSize - this.m_recvOffset;
			if (this.m_recvBuffer == null)
			{
				this.m_recvBuffer = new byte[ZSocket2.m_maxRecvBuffer];
			}
			this.m_socket.GetStream().BeginRead(this.m_recvBuffer, this.m_recvOffset, count, new AsyncCallback(this.PkgReceived), this.m_socket);
			return;
		}
		ZPackage item = new ZPackage(this.m_recvBuffer, this.m_lastRecvPkgSize);
		this.m_mutex.WaitOne();
		this.m_pkgQueue.Enqueue(item);
		this.m_mutex.ReleaseMutex();
		this.BeginReceive();
	}

	// Token: 0x06000F22 RID: 3874 RVA: 0x000725D0 File Offset: 0x000707D0
	public ISocket Accept()
	{
		if (this.m_listner == null)
		{
			return null;
		}
		if (!this.m_listner.Pending())
		{
			return null;
		}
		TcpClient socket = this.m_listner.AcceptTcpClient();
		ZSocket2.ConfigureSocket(socket);
		return new ZSocket2(socket, null);
	}

	// Token: 0x06000F23 RID: 3875 RVA: 0x00072602 File Offset: 0x00070802
	public bool IsConnected()
	{
		return this.m_socket != null && this.m_socket.Connected;
	}

	// Token: 0x06000F24 RID: 3876 RVA: 0x0007261C File Offset: 0x0007081C
	public void Send(ZPackage pkg)
	{
		if (pkg.Size() == 0)
		{
			return;
		}
		if (this.m_socket == null || !this.m_socket.Connected)
		{
			return;
		}
		byte[] array = pkg.GetArray();
		byte[] bytes = BitConverter.GetBytes(array.Length);
		byte[] array2 = new byte[array.Length + bytes.Length];
		bytes.CopyTo(array2, 0);
		array.CopyTo(array2, 4);
		base.IncSentBytes(array.Length);
		this.m_sendMutex.WaitOne();
		if (!this.m_isSending)
		{
			if (array2.Length > 10485760)
			{
				ZLog.LogError("Too big data package: " + array2.Length.ToString());
			}
			try
			{
				this.m_totalSent += array2.Length;
				this.m_socket.GetStream().BeginWrite(array2, 0, array2.Length, new AsyncCallback(this.PkgSent), this.m_socket);
				this.m_isSending = true;
				goto IL_105;
			}
			catch (Exception ex)
			{
				string str = "Handled exception in ZSocket:Send:";
				Exception ex2 = ex;
				ZLog.Log(str + ((ex2 != null) ? ex2.ToString() : null));
				this.Close();
				goto IL_105;
			}
		}
		this.m_sendQueue.Enqueue(array2);
		IL_105:
		this.m_sendMutex.ReleaseMutex();
	}

	// Token: 0x06000F25 RID: 3877 RVA: 0x0007274C File Offset: 0x0007094C
	private void PkgSent(IAsyncResult res)
	{
		try
		{
			this.m_socket.GetStream().EndWrite(res);
		}
		catch (Exception ex)
		{
			ZLog.Log("PkgSent error " + ex.ToString());
			this.Close();
			return;
		}
		this.m_sendMutex.WaitOne();
		if (this.m_sendQueue.Count > 0 && this.IsConnected())
		{
			byte[] array = this.m_sendQueue.Dequeue();
			try
			{
				this.m_totalSent += array.Length;
				this.m_socket.GetStream().BeginWrite(array, 0, array.Length, new AsyncCallback(this.PkgSent), this.m_socket);
				goto IL_CF;
			}
			catch (Exception ex2)
			{
				string str = "Handled exception in pkgsent:";
				Exception ex3 = ex2;
				ZLog.Log(str + ((ex3 != null) ? ex3.ToString() : null));
				this.m_isSending = false;
				this.Close();
				goto IL_CF;
			}
		}
		this.m_isSending = false;
		IL_CF:
		this.m_sendMutex.ReleaseMutex();
	}

	// Token: 0x06000F26 RID: 3878 RVA: 0x00072850 File Offset: 0x00070A50
	public ZPackage Recv()
	{
		if (this.m_socket == null)
		{
			return null;
		}
		if (this.m_pkgQueue.Count == 0)
		{
			return null;
		}
		ZPackage result = null;
		this.m_mutex.WaitOne();
		if (this.m_pkgQueue.Count > 0)
		{
			result = this.m_pkgQueue.Dequeue();
		}
		this.m_mutex.ReleaseMutex();
		return result;
	}

	// Token: 0x06000F27 RID: 3879 RVA: 0x000728AA File Offset: 0x00070AAA
	public string GetEndPointString()
	{
		if (this.m_endpoint != null)
		{
			return this.m_endpoint.ToString();
		}
		return "None";
	}

	// Token: 0x06000F28 RID: 3880 RVA: 0x000728C5 File Offset: 0x00070AC5
	public string GetHostName()
	{
		if (this.m_endpoint != null)
		{
			return this.m_endpoint.Address.ToString();
		}
		return "None";
	}

	// Token: 0x06000F29 RID: 3881 RVA: 0x000728E5 File Offset: 0x00070AE5
	public IPEndPoint GetEndPoint()
	{
		return this.m_endpoint;
	}

	// Token: 0x06000F2A RID: 3882 RVA: 0x000728F0 File Offset: 0x00070AF0
	public bool IsPeer(string host, int port)
	{
		if (!this.IsConnected())
		{
			return false;
		}
		if (this.m_endpoint == null)
		{
			return false;
		}
		IPEndPoint endpoint = this.m_endpoint;
		return (endpoint.Address.ToString() == host && endpoint.Port == port) || (this.m_originalHostName != null && this.m_originalHostName == host && endpoint.Port == port);
	}

	// Token: 0x06000F2B RID: 3883 RVA: 0x00072958 File Offset: 0x00070B58
	public bool IsHost()
	{
		return this.m_listenPort != 0;
	}

	// Token: 0x06000F2C RID: 3884 RVA: 0x00072963 File Offset: 0x00070B63
	public int GetHostPort()
	{
		return this.m_listenPort;
	}

	// Token: 0x06000F2D RID: 3885 RVA: 0x0007296C File Offset: 0x00070B6C
	public int GetSendQueueSize()
	{
		if (!this.IsConnected())
		{
			return 0;
		}
		this.m_sendMutex.WaitOne();
		int num = 0;
		foreach (byte[] array in this.m_sendQueue)
		{
			num += array.Length;
		}
		this.m_sendMutex.ReleaseMutex();
		return num;
	}

	// Token: 0x06000F2E RID: 3886 RVA: 0x000729E4 File Offset: 0x00070BE4
	public bool IsSending()
	{
		return this.m_isSending || this.m_sendQueue.Count > 0;
	}

	// Token: 0x06000F2F RID: 3887 RVA: 0x000729FE File Offset: 0x00070BFE
	public bool GotNewData()
	{
		bool gotData = this.m_gotData;
		this.m_gotData = false;
		return gotData;
	}

	// Token: 0x06000F30 RID: 3888 RVA: 0x00072A0D File Offset: 0x00070C0D
	public bool Flush()
	{
		return true;
	}

	// Token: 0x06000F31 RID: 3889 RVA: 0x00072A10 File Offset: 0x00070C10
	public int GetCurrentSendRate()
	{
		return 0;
	}

	// Token: 0x06000F32 RID: 3890 RVA: 0x00072A13 File Offset: 0x00070C13
	public int GetAverageSendRate()
	{
		return 0;
	}

	// Token: 0x06000F33 RID: 3891 RVA: 0x00072A16 File Offset: 0x00070C16
	public void VersionMatch()
	{
	}

	// Token: 0x04000EBC RID: 3772
	private TcpListener m_listner;

	// Token: 0x04000EBD RID: 3773
	private TcpClient m_socket;

	// Token: 0x04000EBE RID: 3774
	private Mutex m_mutex = new Mutex();

	// Token: 0x04000EBF RID: 3775
	private Mutex m_sendMutex = new Mutex();

	// Token: 0x04000EC0 RID: 3776
	private static int m_maxRecvBuffer = 10485760;

	// Token: 0x04000EC1 RID: 3777
	private int m_recvOffset;

	// Token: 0x04000EC2 RID: 3778
	private byte[] m_recvBuffer;

	// Token: 0x04000EC3 RID: 3779
	private int m_recvSizeOffset;

	// Token: 0x04000EC4 RID: 3780
	private byte[] m_recvSizeBuffer = new byte[4];

	// Token: 0x04000EC5 RID: 3781
	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	// Token: 0x04000EC6 RID: 3782
	private bool m_isSending;

	// Token: 0x04000EC7 RID: 3783
	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	// Token: 0x04000EC8 RID: 3784
	private IPEndPoint m_endpoint;

	// Token: 0x04000EC9 RID: 3785
	private string m_originalHostName;

	// Token: 0x04000ECA RID: 3786
	private int m_listenPort;

	// Token: 0x04000ECB RID: 3787
	private int m_lastRecvPkgSize;

	// Token: 0x04000ECC RID: 3788
	private int m_totalSent;

	// Token: 0x04000ECD RID: 3789
	private int m_totalRecv;

	// Token: 0x04000ECE RID: 3790
	private bool m_gotData;
}
