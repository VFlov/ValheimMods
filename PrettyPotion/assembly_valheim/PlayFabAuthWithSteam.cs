using System;
using System.Text;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;

// Token: 0x020000F9 RID: 249
public static class PlayFabAuthWithSteam
{
	// Token: 0x06000FF7 RID: 4087 RVA: 0x00076EC0 File Offset: 0x000750C0
	public static void Login()
	{
		SteamNetworkingIdentity steamNetworkingIdentity = default(SteamNetworkingIdentity);
		byte[] array = ZSteamMatchmaking.instance.RequestSessionTicket(ref steamNetworkingIdentity);
		if (array == null)
		{
			PlayFabManager.instance.OnLoginFailure(null);
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.AppendFormat("{0:x2}", array[i]);
		}
		PlayFabAuthWithSteam.m_steamTicket = stringBuilder.ToString();
		ZSteamMatchmaking.instance.AuthSessionTicketResponse += PlayFabAuthWithSteam.OnAuthSessionTicketResponse;
	}

	// Token: 0x06000FF8 RID: 4088 RVA: 0x00076F3C File Offset: 0x0007513C
	private static void OnAuthSessionTicketResponse()
	{
		ZSteamMatchmaking.instance.AuthSessionTicketResponse -= PlayFabAuthWithSteam.OnAuthSessionTicketResponse;
		LoginWithSteamRequest loginWithSteamRequest = new LoginWithSteamRequest();
		loginWithSteamRequest.CreateAccount = new bool?(true);
		loginWithSteamRequest.SteamTicket = PlayFabAuthWithSteam.m_steamTicket;
		PlayFabAuthWithSteam.m_steamTicket = null;
		PlayFabClientAPI.LoginWithSteam(loginWithSteamRequest, new Action<LoginResult>(PlayFabAuthWithSteam.OnSteamLoginSuccess), new Action<PlayFabError>(PlayFabAuthWithSteam.OnSteamLoginFailed), null, null);
	}

	// Token: 0x06000FF9 RID: 4089 RVA: 0x00076FA0 File Offset: 0x000751A0
	private static void OnSteamLoginSuccess(LoginResult result)
	{
		ZLog.Log("Logged in PlayFab user via Steam auth session ticket");
		PlayFabManager.instance.OnLoginSuccess(result);
		ZSteamMatchmaking.instance.ReleaseSessionTicket();
	}

	// Token: 0x06000FFA RID: 4090 RVA: 0x00076FC1 File Offset: 0x000751C1
	private static void OnSteamLoginFailed(PlayFabError error)
	{
		ZLog.LogError("Failed to logged in PlayFab user via Steam auth session ticket: " + error.GenerateErrorReport());
		PlayFabManager.instance.OnLoginFailure(error);
		ZSteamMatchmaking.instance.ReleaseSessionTicket();
	}

	// Token: 0x04000F4D RID: 3917
	private static string m_steamTicket;
}
