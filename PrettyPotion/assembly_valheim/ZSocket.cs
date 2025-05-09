using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

// Token: 0x020000EA RID: 234
public class ZSocket : IDisposable
{
	// Token: 0x06000EFA RID: 3834 RVA: 0x000716F8 File Offset: 0x0006F8F8
	public ZSocket()
	{
		this.m_socket = ZSocket.CreateSocket();
	}

	// Token: 0x06000EFB RID: 3835 RVA: 0x00071759 File Offset: 0x0006F959
	public static Socket CreateSocket()
	{
		return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
		{
			NoDelay = true
		};
	}

	// Token: 0x06000EFC RID: 3836 RVA: 0x0007176C File Offset: 0x0006F96C
	public ZSocket(Socket socket, string originalHostName = null)
	{
		this.m_socket = socket;
		this.m_originalHostName = originalHostName;
		try
		{
			this.m_endpoint = (this.m_socket.RemoteEndPoint as IPEndPoint);
		}
		catch
		{
			this.Close();
			return;
		}
		this.BeginReceive();
	}

	// Token: 0x06000EFD RID: 3837 RVA: 0x00071808 File Offset: 0x0006FA08
	public void Dispose()
	{
		this.Close();
		this.m_mutex.Close();
		this.m_sendMutex.Close();
		this.m_recvBuffer = null;
	}

	// Token: 0x06000EFE RID: 3838 RVA: 0x00071830 File Offset: 0x0006FA30
	public void Close()
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
			catch (Exception)
			{
			}
			this.m_socket.Close();
		}
		this.m_socket = null;
		this.m_endpoint = null;
	}

	// Token: 0x06000EFF RID: 3839 RVA: 0x0007188C File Offset: 0x0006FA8C
	public static IPEndPoint GetEndPoint(string host, int port)
	{
		return new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
	}

	// Token: 0x06000F00 RID: 3840 RVA: 0x000718A4 File Offset: 0x0006FAA4
	public bool Connect(string host, int port)
	{
		ZLog.Log("Connecting to " + host + " : " + port.ToString());
		IPEndPoint endPoint = ZSocket.GetEndPoint(host, port);
		this.m_socket.BeginConnect(endPoint, null, null).AsyncWaitHandle.WaitOne(3000, true);
		if (!this.m_socket.Connected)
		{
			return false;
		}
		try
		{
			this.m_endpoint = (this.m_socket.RemoteEndPoint as IPEndPoint);
		}
		catch
		{
			this.Close();
			return false;
		}
		this.BeginReceive();
		ZLog.Log(" connected");
		return true;
	}

	// Token: 0x06000F01 RID: 3841 RVA: 0x0007194C File Offset: 0x0006FB4C
	public bool StartHost(int port)
	{
		if (this.m_listenPort != 0)
		{
			this.Close();
		}
		if (!this.BindSocket(this.m_socket, IPAddress.Any, port, port + 10))
		{
			ZLog.LogWarning("Failed to bind socket");
			return false;
		}
		this.m_socket.Listen(100);
		this.m_socket.BeginAccept(new AsyncCallback(this.AcceptCallback), this.m_socket);
		return true;
	}

	// Token: 0x06000F02 RID: 3842 RVA: 0x000719B8 File Offset: 0x0006FBB8
	private bool BindSocket(Socket socket, IPAddress ipAddress, int startPort, int endPort)
	{
		for (int i = startPort; i <= endPort; i++)
		{
			try
			{
				IPEndPoint localEP = new IPEndPoint(ipAddress, i);
				this.m_socket.Bind(localEP);
				this.m_listenPort = i;
				ZLog.Log("Bound socket port " + i.ToString());
				return true;
			}
			catch
			{
				ZLog.Log("Failed to bind port:" + i.ToString());
			}
		}
		return false;
	}

	// Token: 0x06000F03 RID: 3843 RVA: 0x00071A34 File Offset: 0x0006FC34
	private void BeginReceive()
	{
		this.m_socket.BeginReceive(this.m_recvSizeBuffer, 0, this.m_recvSizeBuffer.Length, SocketFlags.None, new AsyncCallback(this.PkgSizeReceived), this.m_socket);
	}

	// Token: 0x06000F04 RID: 3844 RVA: 0x00071A64 File Offset: 0x0006FC64
	private void PkgSizeReceived(IAsyncResult res)
	{
		int num;
		try
		{
			num = this.m_socket.EndReceive(res);
		}
		catch (Exception)
		{
			this.Disconnect();
			return;
		}
		this.m_totalRecv += num;
		if (num != 4)
		{
			this.Disconnect();
			return;
		}
		int num2 = BitConverter.ToInt32(this.m_recvSizeBuffer, 0);
		if (num2 == 0 || num2 > 10485760)
		{
			ZLog.LogError("Invalid pkg size " + num2.ToString());
			return;
		}
		this.m_lastRecvPkgSize = num2;
		this.m_recvOffset = 0;
		this.m_lastRecvPkgSize = num2;
		if (this.m_recvBuffer == null)
		{
			this.m_recvBuffer = new byte[ZSocket.m_maxRecvBuffer];
		}
		this.m_socket.BeginReceive(this.m_recvBuffer, this.m_recvOffset, this.m_lastRecvPkgSize, SocketFlags.None, new AsyncCallback(this.PkgReceived), this.m_socket);
	}

	// Token: 0x06000F05 RID: 3845 RVA: 0x00071B44 File Offset: 0x0006FD44
	private void Disconnect()
	{
		if (this.m_socket != null)
		{
			try
			{
				this.m_socket.Disconnect(true);
			}
			catch
			{
			}
		}
	}

	// Token: 0x06000F06 RID: 3846 RVA: 0x00071B7C File Offset: 0x0006FD7C
	private void PkgReceived(IAsyncResult res)
	{
		int num;
		try
		{
			num = this.m_socket.EndReceive(res);
		}
		catch (Exception)
		{
			this.Disconnect();
			return;
		}
		this.m_totalRecv += num;
		this.m_recvOffset += num;
		if (this.m_recvOffset < this.m_lastRecvPkgSize)
		{
			int size = this.m_lastRecvPkgSize - this.m_recvOffset;
			if (this.m_recvBuffer == null)
			{
				this.m_recvBuffer = new byte[ZSocket.m_maxRecvBuffer];
			}
			this.m_socket.BeginReceive(this.m_recvBuffer, this.m_recvOffset, size, SocketFlags.None, new AsyncCallback(this.PkgReceived), this.m_socket);
			return;
		}
		ZPackage item = new ZPackage(this.m_recvBuffer, this.m_lastRecvPkgSize);
		this.m_mutex.WaitOne();
		this.m_pkgQueue.Enqueue(item);
		this.m_mutex.ReleaseMutex();
		this.BeginReceive();
	}

	// Token: 0x06000F07 RID: 3847 RVA: 0x00071C6C File Offset: 0x0006FE6C
	private void AcceptCallback(IAsyncResult res)
	{
		Socket item;
		try
		{
			item = this.m_socket.EndAccept(res);
		}
		catch
		{
			this.Disconnect();
			return;
		}
		this.m_mutex.WaitOne();
		this.m_newConnections.Enqueue(item);
		this.m_mutex.ReleaseMutex();
		this.m_socket.BeginAccept(new AsyncCallback(this.AcceptCallback), this.m_socket);
	}

	// Token: 0x06000F08 RID: 3848 RVA: 0x00071CE4 File Offset: 0x0006FEE4
	public ZSocket Accept()
	{
		if (this.m_newConnections.Count == 0)
		{
			return null;
		}
		Socket socket = null;
		this.m_mutex.WaitOne();
		if (this.m_newConnections.Count > 0)
		{
			socket = this.m_newConnections.Dequeue();
		}
		this.m_mutex.ReleaseMutex();
		if (socket != null)
		{
			return new ZSocket(socket, null);
		}
		return null;
	}

	// Token: 0x06000F09 RID: 3849 RVA: 0x00071D3F File Offset: 0x0006FF3F
	public bool IsConnected()
	{
		return this.m_socket != null && this.m_socket.Connected;
	}

	// Token: 0x06000F0A RID: 3850 RVA: 0x00071D58 File Offset: 0x0006FF58
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
		this.m_sendMutex.WaitOne();
		if (!this.m_isSending)
		{
			if (array.Length > 10485760)
			{
				ZLog.LogError("Too big data package: " + array.Length.ToString());
			}
			try
			{
				this.m_totalSent += bytes.Length;
				this.m_socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(this.PkgSent), null);
				this.m_isSending = true;
				this.m_sendQueue.Enqueue(array);
				goto IL_EC;
			}
			catch (Exception ex)
			{
				string str = "Handled exception in ZSocket:Send:";
				Exception ex2 = ex;
				ZLog.Log(str + ((ex2 != null) ? ex2.ToString() : null));
				this.Disconnect();
				goto IL_EC;
			}
		}
		this.m_sendQueue.Enqueue(bytes);
		this.m_sendQueue.Enqueue(array);
		IL_EC:
		this.m_sendMutex.ReleaseMutex();
	}

	// Token: 0x06000F0B RID: 3851 RVA: 0x00071E6C File Offset: 0x0007006C
	private void PkgSent(IAsyncResult res)
	{
		this.m_sendMutex.WaitOne();
		if (this.m_sendQueue.Count > 0 && this.IsConnected())
		{
			byte[] array = this.m_sendQueue.Dequeue();
			try
			{
				this.m_totalSent += array.Length;
				this.m_socket.BeginSend(array, 0, array.Length, SocketFlags.None, new AsyncCallback(this.PkgSent), null);
				goto IL_92;
			}
			catch (Exception ex)
			{
				string str = "Handled exception in pkgsent:";
				Exception ex2 = ex;
				ZLog.Log(str + ((ex2 != null) ? ex2.ToString() : null));
				this.m_isSending = false;
				this.Disconnect();
				goto IL_92;
			}
		}
		this.m_isSending = false;
		IL_92:
		this.m_sendMutex.ReleaseMutex();
	}

	// Token: 0x06000F0C RID: 3852 RVA: 0x00071F28 File Offset: 0x00070128
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

	// Token: 0x06000F0D RID: 3853 RVA: 0x00071F82 File Offset: 0x00070182
	public string GetEndPointString()
	{
		if (this.m_endpoint != null)
		{
			return this.m_endpoint.ToString();
		}
		return "None";
	}

	// Token: 0x06000F0E RID: 3854 RVA: 0x00071F9D File Offset: 0x0007019D
	public string GetEndPointHost()
	{
		if (this.m_endpoint != null)
		{
			return this.m_endpoint.Address.ToString();
		}
		return "None";
	}

	// Token: 0x06000F0F RID: 3855 RVA: 0x00071FBD File Offset: 0x000701BD
	public IPEndPoint GetEndPoint()
	{
		return this.m_endpoint;
	}

	// Token: 0x06000F10 RID: 3856 RVA: 0x00071FC8 File Offset: 0x000701C8
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

	// Token: 0x06000F11 RID: 3857 RVA: 0x00072030 File Offset: 0x00070230
	public bool IsHost()
	{
		return this.m_listenPort != 0;
	}

	// Token: 0x06000F12 RID: 3858 RVA: 0x0007203B File Offset: 0x0007023B
	public int GetHostPort()
	{
		return this.m_listenPort;
	}

	// Token: 0x06000F13 RID: 3859 RVA: 0x00072043 File Offset: 0x00070243
	public bool IsSending()
	{
		return this.m_isSending || this.m_sendQueue.Count > 0;
	}

	// Token: 0x06000F14 RID: 3860 RVA: 0x0007205D File Offset: 0x0007025D
	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = this.m_totalSent;
		totalRecv = this.m_totalRecv;
		this.m_totalSent = 0;
		this.m_totalRecv = 0;
	}

	// Token: 0x04000EAB RID: 3755
	private Socket m_socket;

	// Token: 0x04000EAC RID: 3756
	private Mutex m_mutex = new Mutex();

	// Token: 0x04000EAD RID: 3757
	private Mutex m_sendMutex = new Mutex();

	// Token: 0x04000EAE RID: 3758
	private Queue<Socket> m_newConnections = new Queue<Socket>();

	// Token: 0x04000EAF RID: 3759
	private static int m_maxRecvBuffer = 10485760;

	// Token: 0x04000EB0 RID: 3760
	private int m_recvOffset;

	// Token: 0x04000EB1 RID: 3761
	private byte[] m_recvBuffer;

	// Token: 0x04000EB2 RID: 3762
	private byte[] m_recvSizeBuffer = new byte[4];

	// Token: 0x04000EB3 RID: 3763
	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	// Token: 0x04000EB4 RID: 3764
	private bool m_isSending;

	// Token: 0x04000EB5 RID: 3765
	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	// Token: 0x04000EB6 RID: 3766
	private IPEndPoint m_endpoint;

	// Token: 0x04000EB7 RID: 3767
	private string m_originalHostName;

	// Token: 0x04000EB8 RID: 3768
	private int m_listenPort;

	// Token: 0x04000EB9 RID: 3769
	private int m_lastRecvPkgSize;

	// Token: 0x04000EBA RID: 3770
	private int m_totalSent;

	// Token: 0x04000EBB RID: 3771
	private int m_totalRecv;
}
