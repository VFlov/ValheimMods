using System;
using System.Net;
using System.Net.Sockets;

// Token: 0x020000C2 RID: 194
public abstract class ServerJoinData
{
	// Token: 0x06000BB1 RID: 2993 RVA: 0x00061CE5 File Offset: 0x0005FEE5
	public virtual bool IsValid()
	{
		return false;
	}

	// Token: 0x06000BB2 RID: 2994 RVA: 0x00061CE8 File Offset: 0x0005FEE8
	public virtual string GetDataName()
	{
		return "";
	}

	// Token: 0x06000BB3 RID: 2995 RVA: 0x00061CEF File Offset: 0x0005FEEF
	public override bool Equals(object obj)
	{
		return obj is ServerJoinData;
	}

	// Token: 0x06000BB4 RID: 2996 RVA: 0x00061CFA File Offset: 0x0005FEFA
	public override int GetHashCode()
	{
		return 0;
	}

	// Token: 0x06000BB5 RID: 2997 RVA: 0x00061CFD File Offset: 0x0005FEFD
	public static bool operator ==(ServerJoinData left, ServerJoinData right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return left.Equals(right);
	}

	// Token: 0x06000BB6 RID: 2998 RVA: 0x00061D16 File Offset: 0x0005FF16
	public static bool operator !=(ServerJoinData left, ServerJoinData right)
	{
		return !(left == right);
	}

	// Token: 0x06000BB7 RID: 2999 RVA: 0x00061D24 File Offset: 0x0005FF24
	public static bool URLToIP(string url, out IPAddress ip)
	{
		bool result;
		try
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(url);
			if (hostAddresses.Length == 0)
			{
				ip = null;
				result = false;
			}
			else
			{
				ZLog.Log("Got dns entries: " + hostAddresses.Length.ToString());
				foreach (IPAddress ipaddress in hostAddresses)
				{
					if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
					{
						ip = ipaddress;
						return true;
					}
				}
				ip = null;
				result = false;
			}
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while finding ip:" + ex.ToString());
			ip = null;
			result = false;
		}
		return result;
	}

	// Token: 0x04000CE4 RID: 3300
	public string m_serverName;
}
