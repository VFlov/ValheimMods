using System;
using PlayFab;
using PlayFab.ClientModels;
using Splatform;

// Token: 0x020000F8 RID: 248
public static class PlayFabAuthWithGameCenter
{
	// Token: 0x06000FF4 RID: 4084 RVA: 0x00076E04 File Offset: 0x00075004
	public static void Login()
	{
		if (PlayFabManager.instance == null)
		{
			return;
		}
		PlayFabClientAPI.LoginWithGameCenter(new LoginWithGameCenterRequest
		{
			TitleId = "6E223",
			CreateAccount = new bool?(true),
			PlayerId = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID.m_userID
		}, new Action<LoginResult>(PlayFabAuthWithGameCenter.OnLoginSuccess), new Action<PlayFabError>(PlayFabAuthWithGameCenter.OnLoginFailure), null, null);
	}

	// Token: 0x06000FF5 RID: 4085 RVA: 0x00076E74 File Offset: 0x00075074
	private static void OnLoginSuccess(LoginResult result)
	{
		ZLog.Log("PlayFab logged in via Game Center with ID " + result.PlayFabId);
		PlayFabManager.instance.OnLoginSuccess(result);
	}

	// Token: 0x06000FF6 RID: 4086 RVA: 0x00076E96 File Offset: 0x00075096
	private static void OnLoginFailure(PlayFabError error)
	{
		ZLog.LogWarning(string.Format("PlayFab failed to login via Game Center with error code {0}", error.Error));
		PlayFabManager.instance.OnLoginFailure(error);
	}
}
