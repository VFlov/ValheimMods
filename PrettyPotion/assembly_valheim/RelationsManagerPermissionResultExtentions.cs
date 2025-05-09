using System;

// Token: 0x020000F4 RID: 244
public static class RelationsManagerPermissionResultExtentions
{
	// Token: 0x06000FE3 RID: 4067 RVA: 0x00076974 File Offset: 0x00074B74
	public static bool IsGranted(this RelationsManagerPermissionResult result)
	{
		return result == RelationsManagerPermissionResult.Granted || result == RelationsManagerPermissionResult.GrantedRequiresFiltering;
	}
}
