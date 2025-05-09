using System;
using Splatform;

// Token: 0x020000F1 RID: 241
public class MatchmakingManager
{
	// Token: 0x06000FCE RID: 4046 RVA: 0x000764BB File Offset: 0x000746BB
	public static void Initialize()
	{
		if (MatchmakingManager.s_instance != null)
		{
			ZLog.LogError("MatchmakingManager already initialized!");
			return;
		}
		MatchmakingManager.s_instance = new MatchmakingManager();
	}

	// Token: 0x06000FCF RID: 4047 RVA: 0x000764DC File Offset: 0x000746DC
	private MatchmakingManager()
	{
		IMatchmakingProvider matchmakingProvider = PlatformManager.DistributionPlatform.MatchmakingProvider;
		if (matchmakingProvider == null)
		{
			ZLog.Log("Platform doesn't implement matchmaking! Don't initialize matchmaking manager.");
			MatchmakingManager.s_instance = null;
			return;
		}
		matchmakingProvider.AcceptMultiplayerSessionInvite += this.OnAcceptMultiplayerSessionInvite;
	}

	// Token: 0x06000FD0 RID: 4048 RVA: 0x00076520 File Offset: 0x00074720
	private void OnAcceptMultiplayerSessionInvite(Invite invite)
	{
		if (this.m_pendingInvite != null)
		{
			ZLog.Log("Existing pending invite was reset");
		}
		this.m_pendingInvite = null;
		FejdStartup.instance != null;
		if (Game.instance != null && !Game.instance.IsShuttingDown() && UnifiedPopup.IsAvailable() && Menu.instance != null)
		{
			InviteType inviteType = invite.m_inviteType;
			string header;
			string text;
			if (inviteType != InviteType.Invite)
			{
				if (inviteType != InviteType.JoinSession)
				{
					ZLog.LogError("This part of the code should be unreachable - can't join a game via the invite/join system without having been invited or joined!");
					return;
				}
				header = "$menu_joindifferentserver";
				text = "$menu_logoutprompt";
			}
			else
			{
				header = "$menu_acceptedinvite";
				text = "$menu_logoutprompt";
			}
			this.m_pendingInvite = new Invite?(invite);
			UnifiedPopup.Push(new YesNoPopup(header, text, delegate()
			{
				UnifiedPopup.Pop();
				if (Menu.instance != null)
				{
					Menu.instance.OnLogoutYes();
				}
			}, delegate()
			{
				UnifiedPopup.Pop();
				this.m_pendingInvite = null;
			}, true));
			return;
		}
		this.m_pendingInvite = new Invite?(invite);
	}

	// Token: 0x06000FD1 RID: 4049 RVA: 0x00076620 File Offset: 0x00074820
	public static bool TryConsumePendingInvite(out Invite invite)
	{
		if (MatchmakingManager.s_instance == null)
		{
			invite = default(Invite);
			return false;
		}
		if (MatchmakingManager.s_instance.m_pendingInvite == null)
		{
			invite = default(Invite);
			return false;
		}
		invite = MatchmakingManager.s_instance.m_pendingInvite.Value;
		MatchmakingManager.s_instance.m_pendingInvite = null;
		return true;
	}

	// Token: 0x04000F40 RID: 3904
	private static MatchmakingManager s_instance;

	// Token: 0x04000F41 RID: 3905
	private Invite? m_pendingInvite;
}
