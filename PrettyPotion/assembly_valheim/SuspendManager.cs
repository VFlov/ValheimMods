using System;
using PlayFab.Party;
using Splatform;
using Valheim.SettingsGui;

// Token: 0x020000F7 RID: 247
public class SuspendManager
{
	// Token: 0x06000FEE RID: 4078 RVA: 0x00076C9D File Offset: 0x00074E9D
	public static void Initialize()
	{
		if (SuspendManager.s_instance != null)
		{
			ZLog.LogError("SuspendManager already initialized!");
			return;
		}
		SuspendManager.s_instance = new SuspendManager();
	}

	// Token: 0x06000FEF RID: 4079 RVA: 0x00076CBC File Offset: 0x00074EBC
	private SuspendManager()
	{
		IPLMProvider plmprovider = PlatformManager.DistributionPlatform.PLMProvider;
		if (plmprovider == null)
		{
			ZLog.Log("Platform doesn't implement Process Lifetime Management! Don't initialize suspend manager.");
			SuspendManager.s_instance = null;
			return;
		}
		plmprovider.EnteringSuspend += this.OnEnteringSuspend;
		plmprovider.LeavingSuspend += this.OnLeavingSuspend;
		plmprovider.ResumedFromSuspend += this.OnResumedFromSuspend;
		plmprovider.IsRunningInBackgroundChanged += this.OnIsRunningInBackgroundChanged;
	}

	// Token: 0x06000FF0 RID: 4080 RVA: 0x00076D36 File Offset: 0x00074F36
	private void OnEnteringSuspend(DateTime deadlineUtc)
	{
		if (Game.instance != null && !ZNet.IsSinglePlayer)
		{
			ZNetScene.instance.Shutdown();
			ZNet.instance.ShutdownWithoutSave(true);
		}
		PlayFabMultiplayerManager.Get().Suspend();
	}

	// Token: 0x06000FF1 RID: 4081 RVA: 0x00076D6C File Offset: 0x00074F6C
	private void OnLeavingSuspend()
	{
		if (Game.instance == null)
		{
			return;
		}
		bool flag = ZNet.instance != null && ZNet.instance.IsServer();
		bool flag2 = ZNet.IsOpenServer();
		if (flag == flag2)
		{
			Game.instance.Logout(true, true);
		}
	}

	// Token: 0x06000FF2 RID: 4082 RVA: 0x00076DB8 File Offset: 0x00074FB8
	private void OnResumedFromSuspend()
	{
		if (PlatformManager.DistributionPlatform.PLMProvider.SupportedSuspendEvents.HasFlag(SuspendEvents.EnteringSuspend))
		{
			PlayFabMultiplayerManager.Get().Resume();
		}
	}

	// Token: 0x06000FF3 RID: 4083 RVA: 0x00076DE5 File Offset: 0x00074FE5
	private void OnIsRunningInBackgroundChanged(bool isRunningInBackground)
	{
		GraphicsModeManager.OnConstrainedModeActivated(isRunningInBackground);
		if (Minimap.instance != null)
		{
			Minimap.instance.PauseUpdateTemporarily();
		}
	}

	// Token: 0x04000F4C RID: 3916
	private static SuspendManager s_instance;
}
