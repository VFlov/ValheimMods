using System;
using Splatform;
using UserManagement;

// Token: 0x020000F6 RID: 246
public static class RelationsManager
{
	// Token: 0x06000FE8 RID: 4072 RVA: 0x0007697F File Offset: 0x00074B7F
	public static bool PlatformRequiresTextFiltering()
	{
		return PlatformManager.DistributionPlatform.Platform == "Xbox" || (PlatformManager.DistributionPlatform.HardwareInfoProvider != null && PlatformManager.DistributionPlatform.HardwareInfoProvider.HardwareInfo.m_category == HardwareCategory.Console);
	}

	// Token: 0x06000FE9 RID: 4073 RVA: 0x000769BF File Offset: 0x00074BBF
	public static bool PermissionRequiresFiltering(Permission permission)
	{
		return RelationsManager.PlatformRequiresTextFiltering() && (permission == Permission.CommunicateWithUsingText || permission == Permission.ViewUserGeneratedContent);
	}

	// Token: 0x06000FEA RID: 4074 RVA: 0x000769D4 File Offset: 0x00074BD4
	public static bool FilterTextCommunicationSentToUser(PlatformUserID recipient)
	{
		if (!RelationsManager.PermissionRequiresFiltering(Permission.CommunicateWithUsingText))
		{
			return false;
		}
		if (recipient == PlatformManager.DistributionPlatform.LocalUser.PlatformUserID)
		{
			return false;
		}
		IRelationsProvider relationsProvider = PlatformManager.DistributionPlatform.RelationsProvider;
		return relationsProvider == null || !relationsProvider.IsFriend(recipient);
	}

	// Token: 0x06000FEB RID: 4075 RVA: 0x00076A20 File Offset: 0x00074C20
	public static void CheckPermissionAsync(PlatformUserID user, Permission permission, bool isSender, CheckPermissionCompletedHandler completedHandler)
	{
		RelationsManager.<>c__DisplayClass3_0 CS$<>8__locals1 = new RelationsManager.<>c__DisplayClass3_0();
		CS$<>8__locals1.permission = permission;
		CS$<>8__locals1.user = user;
		CS$<>8__locals1.completedHandler = completedHandler;
		if (!CS$<>8__locals1.user.IsValid)
		{
			ZLog.LogError(string.Format("Failed to check permission {0}: UserID was invalid", CS$<>8__locals1.permission));
			CS$<>8__locals1.completedHandler(RelationsManagerPermissionResult.Error);
			return;
		}
		if (CS$<>8__locals1.user == PlatformManager.DistributionPlatform.LocalUser.PlatformUserID)
		{
			CS$<>8__locals1.completedHandler(RelationsManagerPermissionResult.Granted);
			return;
		}
		if (!isSender && MuteList.Contains(CS$<>8__locals1.user))
		{
			switch (CS$<>8__locals1.permission)
			{
			case Permission.CommunicateWithUsingText:
			case Permission.ViewUserGeneratedContent:
				ZLog.Log(string.Format("Permission {0} was denied for user {1}: The user is blocked locally", CS$<>8__locals1.permission, CS$<>8__locals1.user));
				CS$<>8__locals1.completedHandler(RelationsManagerPermissionResult.Denied);
				return;
			case Permission.PlayMultiplayerWith:
				break;
			default:
				throw new NotImplementedException(string.Format("Permission {0} has not been implemented!", CS$<>8__locals1.permission));
			}
		}
		bool flag;
		if (!RelationsManager.TryCheckEquivalentPrivilege(CS$<>8__locals1.permission, out flag))
		{
			ZLog.LogError(string.Format("Failed to check permission {0} for user {1}: Equivalent privilege check failed", CS$<>8__locals1.permission, CS$<>8__locals1.user));
			CS$<>8__locals1.completedHandler(RelationsManagerPermissionResult.Error);
			return;
		}
		if (!flag)
		{
			ZLog.Log(string.Format("Permission {0} was denied for user {1}: Equivalent privilege was denied", CS$<>8__locals1.permission, CS$<>8__locals1.user));
			CheckPermissionCompletedHandler completedHandler2 = CS$<>8__locals1.completedHandler;
			if (completedHandler2 == null)
			{
				return;
			}
			completedHandler2(RelationsManagerPermissionResult.Denied);
			return;
		}
		else
		{
			if (PlatformManager.DistributionPlatform.RelationsProvider != null)
			{
				PlatformManager.DistributionPlatform.RelationsProvider.GetUserProfileAsync(CS$<>8__locals1.user, new GetUserProfileCompletedHandler(CS$<>8__locals1.<CheckPermissionAsync>g__OnGetUserProfileCompleted|0), new GetUserProfileFailedHandler(CS$<>8__locals1.<CheckPermissionAsync>g__OnGetUserProfileFailed|1));
				return;
			}
			CheckPermissionCompletedHandler completedHandler3 = CS$<>8__locals1.completedHandler;
			if (completedHandler3 == null)
			{
				return;
			}
			completedHandler3(RelationsManager.PermissionRequiresFiltering(CS$<>8__locals1.permission) ? RelationsManagerPermissionResult.GrantedRequiresFiltering : RelationsManagerPermissionResult.Granted);
			return;
		}
	}

	// Token: 0x06000FEC RID: 4076 RVA: 0x00076BF4 File Offset: 0x00074DF4
	private static bool TryCheckEquivalentPrivilege(Permission permission, out bool result)
	{
		Privilege privilege;
		switch (permission)
		{
		case Permission.CommunicateWithUsingText:
			privilege = Privilege.TextCommunication;
			break;
		case Permission.PlayMultiplayerWith:
			privilege = Privilege.OnlineMultiplayer;
			break;
		case Permission.ViewUserGeneratedContent:
			privilege = Privilege.ViewUserGeneratedContent;
			break;
		default:
			ZLog.LogError(string.Format("Failed to check equivalent privilege for permission {0}: There is no equivalent privilege", permission));
			result = false;
			return false;
		}
		PrivilegeResult privilegeResult = PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(privilege);
		if (privilegeResult.IsError())
		{
			ZLog.LogError(string.Format("Failed to check privilege {0}: {1}", privilege, privilegeResult));
			result = false;
			return false;
		}
		result = privilegeResult.IsGranted();
		return true;
	}

	// Token: 0x06000FED RID: 4077 RVA: 0x00076C7D File Offset: 0x00074E7D
	public static bool IsBlocked(PlatformUserID user)
	{
		return PlatformManager.DistributionPlatform.RelationsProvider != null && PlatformManager.DistributionPlatform.RelationsProvider.IsBlocked(user);
	}
}
