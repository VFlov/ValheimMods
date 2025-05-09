using System;

// Token: 0x02000105 RID: 261
public static class PlayFabAttrKeyExtension
{
	// Token: 0x06001057 RID: 4183 RVA: 0x00078775 File Offset: 0x00076975
	public static string ToKeyString(this PlayFabAttrKey key)
	{
		switch (key)
		{
		case PlayFabAttrKey.WorldName:
			return "WORLD";
		case PlayFabAttrKey.NetworkId:
			return "NETWORK";
		case PlayFabAttrKey.HavePassword:
			return "PASSWORD";
		default:
			return null;
		}
	}
}
