using System;

// Token: 0x020000BF RID: 191
public static class OnlineBackendTypeExtentions
{
	// Token: 0x06000BAF RID: 2991 RVA: 0x00061C50 File Offset: 0x0005FE50
	public static string ConvertToString(this OnlineBackendType backend)
	{
		switch (backend)
		{
		case OnlineBackendType.Steamworks:
			return "steamworks";
		case OnlineBackendType.PlayFab:
			return "playfab";
		case OnlineBackendType.EOS:
			return "eos";
		case OnlineBackendType.CustomSocket:
			return "socket";
		}
		return "none";
	}

	// Token: 0x06000BB0 RID: 2992 RVA: 0x00061C8C File Offset: 0x0005FE8C
	public static OnlineBackendType ConvertFromString(string backend)
	{
		if (backend == "steamworks")
		{
			return OnlineBackendType.Steamworks;
		}
		if (backend == "eos")
		{
			return OnlineBackendType.EOS;
		}
		if (backend == "playfab")
		{
			return OnlineBackendType.PlayFab;
		}
		if (!(backend == "socket"))
		{
			if (!(backend == "none"))
			{
			}
			return OnlineBackendType.None;
		}
		return OnlineBackendType.CustomSocket;
	}
}
