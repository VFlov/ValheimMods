using System;
using System.Collections;
using System.Runtime.CompilerServices;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Party;
using Splatform;
using UnityEngine;

// Token: 0x020000FD RID: 253
public class PlayFabManager : MonoBehaviour
{
	// Token: 0x1700008A RID: 138
	// (get) Token: 0x06000FFF RID: 4095 RVA: 0x00076FED File Offset: 0x000751ED
	public static bool IsLoggedIn
	{
		get
		{
			return !(PlayFabManager.instance == null) && PlayFabManager.instance.m_loginState == LoginState.LoggedIn;
		}
	}

	// Token: 0x1700008B RID: 139
	// (get) Token: 0x06001000 RID: 4096 RVA: 0x0007700B File Offset: 0x0007520B
	public static LoginState CurrentLoginState
	{
		get
		{
			if (PlayFabManager.instance == null)
			{
				return LoginState.NotLoggedIn;
			}
			return PlayFabManager.instance.m_loginState;
		}
	}

	// Token: 0x1700008C RID: 140
	// (get) Token: 0x06001001 RID: 4097 RVA: 0x00077026 File Offset: 0x00075226
	public bool ShouldTryAutoLogin
	{
		get
		{
			return PlayFabManager.instance.m_shouldTryAutoLogin;
		}
	}

	// Token: 0x1700008D RID: 141
	// (get) Token: 0x06001002 RID: 4098 RVA: 0x00077032 File Offset: 0x00075232
	// (set) Token: 0x06001003 RID: 4099 RVA: 0x00077039 File Offset: 0x00075239
	public static DateTime NextRetryUtc { get; private set; } = DateTime.MinValue;

	// Token: 0x1700008E RID: 142
	// (get) Token: 0x06001004 RID: 4100 RVA: 0x00077041 File Offset: 0x00075241
	// (set) Token: 0x06001005 RID: 4101 RVA: 0x00077049 File Offset: 0x00075249
	public EntityKey Entity { get; private set; }

	// Token: 0x1400000A RID: 10
	// (add) Token: 0x06001006 RID: 4102 RVA: 0x00077054 File Offset: 0x00075254
	// (remove) Token: 0x06001007 RID: 4103 RVA: 0x0007708C File Offset: 0x0007528C
	public event LoginFinishedCallback LoginFinished;

	// Token: 0x1700008F RID: 143
	// (get) Token: 0x06001008 RID: 4104 RVA: 0x000770C1 File Offset: 0x000752C1
	// (set) Token: 0x06001009 RID: 4105 RVA: 0x000770C8 File Offset: 0x000752C8
	public static PlayFabManager instance { get; private set; }

	// Token: 0x0600100A RID: 4106 RVA: 0x000770D0 File Offset: 0x000752D0
	public void SetShouldTryAutoLogin(bool value)
	{
		PlatformPrefs.SetInt("ShouldTryAutoLogin", value ? 1 : 0);
		if (PlatformPrefs.GetInt("ShouldTryAutoLogin", 0) == 1)
		{
			PlayFabManager.instance.m_shouldTryAutoLogin = true;
			PlayFabManager.instance.Login();
			return;
		}
		PlayFabManager.instance.m_shouldTryAutoLogin = false;
	}

	// Token: 0x0600100B RID: 4107 RVA: 0x0007711D File Offset: 0x0007531D
	public static void SetCustomId(PlatformUserID id)
	{
		PlayFabManager.m_customId = id;
		ZLog.Log(string.Format("PlayFab custom ID set to \"{0}\"", PlayFabManager.m_customId));
		if (PlayFabManager.instance != null && PlayFabManager.CurrentLoginState == LoginState.NotLoggedIn)
		{
			PlayFabManager.instance.Login();
		}
	}

	// Token: 0x0600100C RID: 4108 RVA: 0x0007715C File Offset: 0x0007535C
	public static void Initialize()
	{
		if (PlayFabManager.instance == null)
		{
			Application.logMessageReceived += PlayFabManager.HandleLog;
			new GameObject("PlayFabManager").AddComponent<PlayFabManager>();
		}
	}

	// Token: 0x0600100D RID: 4109 RVA: 0x0007718C File Offset: 0x0007538C
	public void Awake()
	{
	}

	// Token: 0x0600100E RID: 4110 RVA: 0x0007718E File Offset: 0x0007538E
	private void EnsureMultiplayerManagerCreated()
	{
		if (this.m_multiplayerManager != null)
		{
			return;
		}
		this.m_multiplayerManager = new GameObject("PlayFabMultiplayerManager");
		this.m_multiplayerManager.AddComponent<PlayFabMultiplayerManager>();
	}

	// Token: 0x0600100F RID: 4111 RVA: 0x000771BB File Offset: 0x000753BB
	private void EnsureMultiplayerManagerDestroyed()
	{
		if (this.m_multiplayerManager == null)
		{
			return;
		}
		UnityEngine.Object.Destroy(this.m_multiplayerManager);
		this.m_multiplayerManager = null;
	}

	// Token: 0x06001010 RID: 4112 RVA: 0x000771E0 File Offset: 0x000753E0
	public void Start()
	{
		if (PlayFabManager.instance != null)
		{
			ZLog.LogError("Tried to create another PlayFabManager when one already exists! Ignoring and destroying the new one.");
			UnityEngine.Object.Destroy(this);
			return;
		}
		this.m_shouldTryAutoLogin = (PlatformPrefs.GetInt("ShouldTryAutoLogin", 1) == 1);
		PlayFabManager.instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		this.Login();
		base.Invoke("StopListeningToLogMsgs", 5f);
	}

	// Token: 0x06001011 RID: 4113 RVA: 0x00077248 File Offset: 0x00075448
	private void Login()
	{
		if (!this.m_shouldTryAutoLogin)
		{
			return;
		}
		if (this.m_loginState == LoginState.AttemptingLogin)
		{
			ZLog.LogError("Can't log in while in the " + this.m_loginState.ToString() + " state!");
			return;
		}
		this.m_loginAttempts++;
		ZLog.Log(string.Format("Sending PlayFab login request (attempt {0})", this.m_loginAttempts));
		this.EnsureMultiplayerManagerCreated();
		if (this.m_loginState != LoginState.LoggedIn)
		{
			this.m_loginState = LoginState.AttemptingLogin;
		}
		PlayFabAuthWithSteam.Login();
	}

	// Token: 0x06001012 RID: 4114 RVA: 0x000772D0 File Offset: 0x000754D0
	public void OnLoginSuccess(LoginResult result)
	{
		this.Entity = result.EntityToken.Entity;
		this.PlayFabUniqueId = result.PlayFabId;
		this.m_entityToken = result.EntityToken.EntityToken;
		this.m_tokenExpiration = result.EntityToken.TokenExpiration;
		this.m_authenticationContext = result.AuthenticationContext;
		if (this.m_tokenExpiration == null)
		{
			ZLog.LogError("Token expiration time was null!");
			this.m_loginState = LoginState.LoggedIn;
			return;
		}
		this.m_refreshThresh = (float)(this.m_tokenExpiration.Value - DateTime.UtcNow).TotalSeconds / 2f;
		if (PlayFabManager.IsLoggedIn)
		{
			ZLog.Log(string.Format("PlayFab local entity ID {0} lifetime extended ", this.Entity.Id));
			LoginFinishedCallback loginFinished = this.LoginFinished;
			if (loginFinished != null)
			{
				loginFinished(LoginType.Refresh);
			}
		}
		else
		{
			if (PlayFabManager.m_customId.IsValid)
			{
				ZLog.Log(string.Format("PlayFab logged in as \"{0}\"", PlayFabManager.m_customId));
			}
			ZLog.Log("PlayFab local entity ID is " + this.Entity.Id);
			this.m_loginState = LoginState.LoggedIn;
			LoginFinishedCallback loginFinished2 = this.LoginFinished;
			if (loginFinished2 != null)
			{
				loginFinished2(LoginType.Success);
			}
		}
		if (this.m_updateEntityTokenCoroutine == null)
		{
			this.m_updateEntityTokenCoroutine = base.StartCoroutine(this.UpdateEntityTokenCoroutine());
		}
		ZPlayFabMatchmaking.OnLogin();
	}

	// Token: 0x06001013 RID: 4115 RVA: 0x0007741D File Offset: 0x0007561D
	public void OnLoginFailure(PlayFabError error)
	{
		if (error == null)
		{
			ZLog.LogError("Unknown login error");
		}
		else
		{
			ZLog.LogError(error.GenerateErrorReport());
		}
		LoginFinishedCallback loginFinished = this.LoginFinished;
		if (loginFinished != null)
		{
			loginFinished(LoginType.Failed);
		}
		this.RetryLoginAfterDelay(this.GetRetryDelay(this.m_loginAttempts));
	}

	// Token: 0x06001014 RID: 4116 RVA: 0x0007745D File Offset: 0x0007565D
	private float GetRetryDelay(int attemptCount)
	{
		return Mathf.Min(1f * Mathf.Pow(2f, (float)(attemptCount - 1)), 30f) * UnityEngine.Random.Range(0.875f, 1.125f);
	}

	// Token: 0x06001015 RID: 4117 RVA: 0x0007748D File Offset: 0x0007568D
	private void RetryLoginAfterDelay(float delay)
	{
		this.m_loginState = LoginState.WaitingForRetry;
		ZLog.Log(string.Format("Retrying login in {0}s", delay));
		base.StartCoroutine(this.<RetryLoginAfterDelay>g__DelayThenLoginCoroutine|50_0(delay));
	}

	// Token: 0x06001016 RID: 4118 RVA: 0x000774B9 File Offset: 0x000756B9
	public static void CheckIfUserAuthenticated(string playfabID, PlatformUserID platformUserId, Action<bool> resultCallback)
	{
		resultCallback(true);
	}

	// Token: 0x06001017 RID: 4119 RVA: 0x000774C2 File Offset: 0x000756C2
	private IEnumerator UpdateEntityTokenCoroutine()
	{
		for (;;)
		{
			yield return new WaitForSecondsRealtime(420f);
			ZLog.Log("Update PlayFab entity token");
			PlayFabMultiplayerManager.Get().UpdateEntityToken(this.m_entityToken);
			if (this.m_tokenExpiration == null)
			{
				break;
			}
			if ((float)(this.m_tokenExpiration.Value - DateTime.UtcNow).TotalSeconds <= this.m_refreshThresh)
			{
				ZLog.Log("Renew PlayFab entity token");
				this.m_refreshThresh /= 1.5f;
				this.Login();
			}
			yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(420f, 840f));
		}
		ZLog.LogError("Token expiration time was null!");
		this.m_updateEntityTokenCoroutine = null;
		yield break;
		yield break;
	}

	// Token: 0x06001018 RID: 4120 RVA: 0x000774D1 File Offset: 0x000756D1
	private static void HandleLog(string logString, string stackTrace, LogType type)
	{
		if (type == LogType.Exception && logString.ToLower().Contains("DllNotFoundException: Party", StringComparison.InvariantCultureIgnoreCase))
		{
			ZLog.LogError("DLL Not Found: This error usually occurs when you do not have the correct dependencies installed, and will prevent crossplay from working. The dependencies are different depending on which platform you play on.\n For windows: You need VC++ Redistributables. https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170Linux: You need Pulse Audio. https://learn.microsoft.com/it-it/gaming/playfab/features/multiplayer/networking/linux-specific-requirementsSteam deck: Try using Proton Compatability Layer.Other platforms: If the issue persists, please report it as a bug.");
			UnityEngine.Object.FindObjectOfType<PlayFabManager>().WaitForPopupEnabled();
		}
	}

	// Token: 0x06001019 RID: 4121 RVA: 0x000774FE File Offset: 0x000756FE
	private void StopListeningToLogMsgs()
	{
		Application.logMessageReceived -= PlayFabManager.HandleLog;
	}

	// Token: 0x0600101A RID: 4122 RVA: 0x00077511 File Offset: 0x00075711
	private void WaitForPopupEnabled()
	{
		if (UnifiedPopup.IsAvailable())
		{
			this.DelayedVCRedistWarningPopup();
			return;
		}
		UnifiedPopup.OnPopupEnabled += this.DelayedVCRedistWarningPopup;
	}

	// Token: 0x0600101B RID: 4123 RVA: 0x00077534 File Offset: 0x00075734
	private void DelayedVCRedistWarningPopup()
	{
		string header = "$playfab_couldnotloadplayfabparty_header";
		string playFabErrorBodyText = this.GetPlayFabErrorBodyText();
		UnifiedPopup.Push(new WarningPopup(header, playFabErrorBodyText, delegate()
		{
			UnifiedPopup.Pop();
		}, true));
		UnifiedPopup.OnPopupEnabled -= this.DelayedVCRedistWarningPopup;
		Application.logMessageReceived -= PlayFabManager.HandleLog;
	}

	// Token: 0x0600101C RID: 4124 RVA: 0x0007759C File Offset: 0x0007579C
	private string GetPlayFabErrorBodyText()
	{
		string result;
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			result = "$playfab_couldnotloadplayfabparty_text_linux_steamdeck";
		}
		else if (!Settings.IsSteamRunningOnSteamDeck() && (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.WindowsServer || Application.platform == RuntimePlatform.WindowsEditor))
		{
			result = "$playfab_couldnotloadplayfabparty_text_linux";
		}
		else if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer || Application.platform == RuntimePlatform.WindowsEditor)
		{
			result = "$playfab_couldnotloadplayfabparty_text_windows";
		}
		else
		{
			result = "$playfab_couldnotloadplayfabparty_text_otherplatforms";
		}
		return result;
	}

	// Token: 0x0600101D RID: 4125 RVA: 0x00077609 File Offset: 0x00075809
	public void LoginFailed()
	{
		this.OnPlayFabRespondRemoveUIBlock(LoginType.Refresh);
		this.RetryLoginAfterDelay(this.GetRetryDelay(this.m_loginAttempts));
	}

	// Token: 0x0600101E RID: 4126 RVA: 0x00077624 File Offset: 0x00075824
	private void Update()
	{
		ZPlayFabMatchmaking instance = ZPlayFabMatchmaking.instance;
		if (instance == null)
		{
			return;
		}
		instance.Update(Time.unscaledDeltaTime);
	}

	// Token: 0x0600101F RID: 4127 RVA: 0x0007763C File Offset: 0x0007583C
	public void DeletePlayerTitleAccount()
	{
		if (string.IsNullOrEmpty(this.PlayFabUniqueId))
		{
			ZLog.LogError("No associated PlayFab ID found. Cannot delete account.");
			return;
		}
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
		{
			FunctionName = "deletePlayerAccount",
			FunctionParameter = new
			{
				this.PlayFabUniqueId
			},
			GeneratePlayStreamEvent = new bool?(true)
		}, new Action<ExecuteCloudScriptResult>(this.OnCloudDeletePlayerResult), new Action<PlayFabError>(this.OnDeletePlayerFailed), null, null);
		UnifiedPopup.Push(new TaskPopup("$settings_deleteplayfab_task_header", "", true));
	}

	// Token: 0x06001020 RID: 4128 RVA: 0x000776C4 File Offset: 0x000758C4
	public void OnPlayFabRespondRemoveUIBlock(LoginType loginType = LoginType.Success)
	{
		if (UnifiedPopup.IsAvailable())
		{
			if (!UnifiedPopup.IsVisible())
			{
				return;
			}
			UnifiedPopup.Pop();
		}
		if (loginType == LoginType.Failed || loginType == LoginType.Refresh)
		{
			ZLog.LogWarning("Could not log in to PlayFab. ");
			this.ResetMainMenuButtons();
			this.LoginFinished -= this.OnPlayFabRespondRemoveUIBlock;
			UnifiedPopup.Push(new WarningPopup("$menu_logging_in_playfab_failed_header", "", new PopupButtonCallback(UnifiedPopup.Pop), true));
		}
	}

	// Token: 0x06001021 RID: 4129 RVA: 0x00077734 File Offset: 0x00075934
	public void ResetMainMenuButtons()
	{
		if (FejdStartup.instance == null)
		{
			return;
		}
		TabHandler[] componentsInChildren = FejdStartup.instance.transform.GetComponentsInChildren<TabHandler>(true);
		int num = 0;
		if (num < componentsInChildren.Length)
		{
			TabHandler tabHandler = componentsInChildren[num];
			if (tabHandler.m_tabs.Count == 2)
			{
				tabHandler.SetActiveTab(0, false, true);
			}
		}
		FejdStartup.instance.m_openServerToggle.isOn = false;
	}

	// Token: 0x06001022 RID: 4130 RVA: 0x00077798 File Offset: 0x00075998
	private void OnCloudDeletePlayerResult(ExecuteCloudScriptResult obj)
	{
		bool flag = false;
		string str = "";
		if (obj.FunctionResult != null)
		{
			string text = obj.FunctionResult.ToString();
			flag = text.Contains("\"success\":true,\"");
			str = text;
		}
		else
		{
			Debug.LogError("Result of PlayFab API is null or invalid.");
		}
		this.m_loginState = LoginState.NotLoggedIn;
		this.SetShouldTryAutoLogin(false);
		this.EnsureMultiplayerManagerDestroyed();
		this.OnPlayFabRespondRemoveUIBlock(LoginType.Success);
		ZLog.Log("Delete Player Result: " + obj.ToJson());
		this.ResetMainMenuButtons();
		string text2 = flag ? "$settings_deleteplayfabaccount_success" : ("$settings_deleteplayfabaccount_failure" + str);
		UnifiedPopup.Push(new WarningPopup("", text2, new PopupButtonCallback(UnifiedPopup.Pop), true));
	}

	// Token: 0x06001023 RID: 4131 RVA: 0x00077844 File Offset: 0x00075A44
	private void OnDeletePlayerFailed(PlayFabError error)
	{
		this.OnPlayFabRespondRemoveUIBlock(LoginType.Success);
		string str = error.GenerateErrorReport();
		ZLog.LogError("Could not remove player account: " + str);
		UnifiedPopup.Push(new WarningPopup("", "$settings_deleteplayfabaccount_failure" + str, new PopupButtonCallback(UnifiedPopup.Pop), true));
	}

	// Token: 0x06001026 RID: 4134 RVA: 0x000778B5 File Offset: 0x00075AB5
	[CompilerGenerated]
	private IEnumerator <RetryLoginAfterDelay>g__DelayThenLoginCoroutine|50_0(float delay)
	{
		ZLog.Log(string.Format("PlayFab login failed! Retrying in {0}s, total attempts: {1}", delay, this.m_loginAttempts));
		PlayFabManager.NextRetryUtc = DateTime.UtcNow + TimeSpan.FromSeconds((double)delay);
		while (DateTime.UtcNow < PlayFabManager.NextRetryUtc)
		{
			yield return null;
		}
		this.Login();
		yield break;
	}

	// Token: 0x04000F57 RID: 3927
	public const string TitleId = "6E223";

	// Token: 0x04000F58 RID: 3928
	private LoginState m_loginState;

	// Token: 0x04000F59 RID: 3929
	private string m_entityToken;

	// Token: 0x04000F5A RID: 3930
	private DateTime? m_tokenExpiration;

	// Token: 0x04000F5B RID: 3931
	private PlayFabAuthenticationContext m_authenticationContext;

	// Token: 0x04000F5C RID: 3932
	private float m_refreshThresh;

	// Token: 0x04000F5D RID: 3933
	private int m_loginAttempts;

	// Token: 0x04000F5E RID: 3934
	private bool m_shouldTryAutoLogin;

	// Token: 0x04000F5F RID: 3935
	private bool m_deletionRequestDoneOrTimedOut;

	// Token: 0x04000F60 RID: 3936
	private PlayFabMultiplayerManager m_playFabMultiplayerManager;

	// Token: 0x04000F61 RID: 3937
	private const float EntityTokenUpdateDurationMin = 420f;

	// Token: 0x04000F62 RID: 3938
	private const float EntityTokenUpdateDurationMax = 840f;

	// Token: 0x04000F63 RID: 3939
	private const float LoginRetryDelay = 1f;

	// Token: 0x04000F64 RID: 3940
	private const float LoginRetryDelayMax = 30f;

	// Token: 0x04000F65 RID: 3941
	private const float LoginRetryJitterFactor = 0.125f;

	// Token: 0x04000F68 RID: 3944
	public string PlayFabUniqueId = "";

	// Token: 0x04000F6A RID: 3946
	private static PlatformUserID m_customId;

	// Token: 0x04000F6B RID: 3947
	private GameObject m_multiplayerManager;

	// Token: 0x04000F6D RID: 3949
	private Coroutine m_updateEntityTokenCoroutine;
}
