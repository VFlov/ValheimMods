using System;
using System.ComponentModel;
using System.Net;
using NetworkingUtils;
using UnityEngine;

// Token: 0x020000C5 RID: 197
public class ServerJoinDataDedicated : ServerJoinData
{
	// Token: 0x06000BCE RID: 3022 RVA: 0x00061FB4 File Offset: 0x000601B4
	public ServerJoinDataDedicated(string address)
	{
		ushort num = 0;
		string text;
		ServerJoinDataDedicated.GetAddressAndPortFromString(address, out text, out num);
		Debug.Log(string.Format("We passed in {0} and got back ip address: {1}, and port: {2}", address, text, num));
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		this.SetHost(text);
		if (num != 0)
		{
			this.m_port = num;
		}
		else
		{
			this.m_port = 2456;
		}
		this.m_serverName = this.ToString();
	}

	// Token: 0x06000BCF RID: 3023 RVA: 0x0006201D File Offset: 0x0006021D
	public ServerJoinDataDedicated(string host, ushort port)
	{
		this.SetHost(host);
		this.m_port = Convert.ToUInt16(port);
		this.m_serverName = this.ToString();
	}

	// Token: 0x06000BD0 RID: 3024 RVA: 0x00062044 File Offset: 0x00060244
	public ServerJoinDataDedicated(uint host, ushort port)
	{
		this.m_address = new IPv6Address?(new IPv4Address(host));
		this.m_host = this.m_address.Value.IPv4.ToString();
		this.m_port = port;
		this.m_serverName = this.ToString();
	}

	// Token: 0x06000BD1 RID: 3025 RVA: 0x000620A8 File Offset: 0x000602A8
	public static void GetAddressAndPortFromString(string address, out string ipAddress, out ushort foundPort)
	{
		if (string.IsNullOrEmpty(address))
		{
			ipAddress = string.Empty;
			foundPort = 0;
			return;
		}
		int num = address.LastIndexOf(":");
		int num2 = (num >= 0) ? num : address.Length;
		if (num < 0 || !ushort.TryParse(address.Substring(num + 1), out foundPort))
		{
			foundPort = 0;
			num2 = address.Length;
		}
		IPv6Address pv6Address;
		if (address[0] == '[' && address[num2 - 1] == ']' && IPv6Address.TryParse(address.Substring(1, num2 - 2), out pv6Address, false))
		{
			if (pv6Address.AddressRange == IPv6AddressRange.Unspecified)
			{
				ipAddress = string.Empty;
				foundPort = 0;
				return;
			}
			ipAddress = pv6Address.ToString();
			return;
		}
		else
		{
			IPv4Address pv4Address;
			if (IPv4Address.TryParse(address.Substring(0, num2), out pv4Address))
			{
				ipAddress = pv4Address.ToString();
				return;
			}
			IPv6Address pv6Address2;
			if (IPv6Address.TryParse(address, out pv6Address2, false))
			{
				if (pv6Address2.AddressRange == IPv6AddressRange.Unspecified)
				{
					ipAddress = string.Empty;
				}
				else
				{
					ipAddress = pv6Address2.ToString();
				}
				foundPort = 0;
				return;
			}
			ipAddress = address.Substring(0, num2);
			return;
		}
	}

	// Token: 0x17000055 RID: 85
	// (get) Token: 0x06000BD2 RID: 3026 RVA: 0x000621AC File Offset: 0x000603AC
	// (set) Token: 0x06000BD3 RID: 3027 RVA: 0x000621B4 File Offset: 0x000603B4
	public bool IsURL { get; private set; }

	// Token: 0x06000BD4 RID: 3028 RVA: 0x000621C0 File Offset: 0x000603C0
	public override bool IsValid()
	{
		if (this.m_address != null)
		{
			return true;
		}
		if (!this.m_dnsLookupShouldBePerformed)
		{
			return false;
		}
		this.m_dnsLookupShouldBePerformed = false;
		IPAddress ipaddress;
		if (!ServerJoinData.URLToIP(this.m_host, out ipaddress))
		{
			return false;
		}
		this.m_address = new IPv6Address?(new IPv4Address(ipaddress.GetAddressBytes()));
		return true;
	}

	// Token: 0x06000BD5 RID: 3029 RVA: 0x0006221C File Offset: 0x0006041C
	public void IsValidAsync(Action<bool> resultCallback)
	{
		bool result = false;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
		{
			result = this.IsValid();
		};
		backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
		{
			resultCallback(result);
		};
		backgroundWorker.RunWorkerAsync();
	}

	// Token: 0x06000BD6 RID: 3030 RVA: 0x00062272 File Offset: 0x00060472
	public override string GetDataName()
	{
		return "Dedicated";
	}

	// Token: 0x06000BD7 RID: 3031 RVA: 0x0006227C File Offset: 0x0006047C
	public override bool Equals(object obj)
	{
		ServerJoinDataDedicated serverJoinDataDedicated = obj as ServerJoinDataDedicated;
		return serverJoinDataDedicated != null && base.Equals(obj) && this.m_host == serverJoinDataDedicated.m_host && this.m_port == serverJoinDataDedicated.m_port;
	}

	// Token: 0x06000BD8 RID: 3032 RVA: 0x000622C0 File Offset: 0x000604C0
	public override int GetHashCode()
	{
		int num = -468063053;
		num = num * -1521134295 + base.GetHashCode();
		if (!string.IsNullOrEmpty(this.m_host))
		{
			num = num * -1521134295 + this.m_host.GetHashCode();
		}
		else
		{
			ZLog.LogWarning("m_host was null or empty when trying to get hash code!");
		}
		return num * -1521134295 + this.m_port.GetHashCode();
	}

	// Token: 0x06000BD9 RID: 3033 RVA: 0x00062327 File Offset: 0x00060527
	public static bool operator ==(ServerJoinDataDedicated left, ServerJoinDataDedicated right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return left.Equals(right);
	}

	// Token: 0x06000BDA RID: 3034 RVA: 0x00062340 File Offset: 0x00060540
	public static bool operator !=(ServerJoinDataDedicated left, ServerJoinDataDedicated right)
	{
		return !(left == right);
	}

	// Token: 0x06000BDB RID: 3035 RVA: 0x0006234C File Offset: 0x0006054C
	private void SetHost(string host)
	{
		IPv6Address value;
		if (IPv6Address.TryParse(host, out value, true))
		{
			this.m_host = value.ToString();
			this.m_address = new IPv6Address?(value);
			return;
		}
		string text = host;
		if (!host.StartsWith("http://") && !host.StartsWith("https://"))
		{
			text = "http://" + host;
		}
		if (!host.EndsWith("/"))
		{
			text += "/";
		}
		Uri uri;
		if (Uri.TryCreate(text, UriKind.Absolute, out uri))
		{
			this.m_host = host;
			this.IsURL = true;
			this.m_dnsLookupShouldBePerformed = true;
			return;
		}
		this.m_host = host;
	}

	// Token: 0x06000BDC RID: 3036 RVA: 0x000623ED File Offset: 0x000605ED
	public string GetHost()
	{
		return this.m_host;
	}

	// Token: 0x06000BDD RID: 3037 RVA: 0x000623F5 File Offset: 0x000605F5
	public bool TryGetIPAddress(out IPv6Address address)
	{
		if (!this.IsValid())
		{
			ZLog.LogError("Can't get IP from invalid server data");
			address = default(IPv6Address);
			return false;
		}
		address = this.m_address.Value;
		return true;
	}

	// Token: 0x06000BDE RID: 3038 RVA: 0x00062424 File Offset: 0x00060624
	public NetworkingUtils.IPEndPoint? GetIPEndPoint()
	{
		if (!this.IsValid())
		{
			return null;
		}
		return new NetworkingUtils.IPEndPoint?(new NetworkingUtils.IPEndPoint(this.m_address.Value, this.m_port));
	}

	// Token: 0x06000BDF RID: 3039 RVA: 0x00062460 File Offset: 0x00060660
	public override string ToString()
	{
		string host = this.GetHost();
		int port = (int)this.m_port;
		if (this.m_address != null && this.m_address.Value.AddressRange != IPv6AddressRange.IPv4Mapped)
		{
			return string.Format("[{0}]:{1}", host, port);
		}
		return string.Format("{0}:{1}", host, port);
	}

	// Token: 0x17000056 RID: 86
	// (get) Token: 0x06000BE0 RID: 3040 RVA: 0x000624C1 File Offset: 0x000606C1
	// (set) Token: 0x06000BE1 RID: 3041 RVA: 0x000624C9 File Offset: 0x000606C9
	public string m_host { get; private set; }

	// Token: 0x17000057 RID: 87
	// (get) Token: 0x06000BE2 RID: 3042 RVA: 0x000624D2 File Offset: 0x000606D2
	// (set) Token: 0x06000BE3 RID: 3043 RVA: 0x000624DA File Offset: 0x000606DA
	public ushort m_port { get; private set; }

	// Token: 0x04000CE9 RID: 3305
	public const string typeName = "Dedicated";

	// Token: 0x04000CEB RID: 3307
	private bool m_dnsLookupShouldBePerformed;

	// Token: 0x04000CEE RID: 3310
	private IPv6Address? m_address;
}
