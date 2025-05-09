using System;
using System.IO;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;

// Token: 0x0200010A RID: 266
[DisallowMultipleComponent]
public class SteamManager : MonoBehaviour
{
	// Token: 0x17000094 RID: 148
	// (get) Token: 0x060010E7 RID: 4327 RVA: 0x0007B500 File Offset: 0x00079700
	public static SteamManager instance
	{
		get
		{
			return SteamManager.s_instance;
		}
	}

	// Token: 0x060010E8 RID: 4328 RVA: 0x0007B507 File Offset: 0x00079707
	public static bool Initialize()
	{
		if (SteamManager.s_instance == null)
		{
			new GameObject("SteamManager").AddComponent<SteamManager>();
		}
		return SteamManager.Initialized;
	}

	// Token: 0x17000095 RID: 149
	// (get) Token: 0x060010E9 RID: 4329 RVA: 0x0007B52B File Offset: 0x0007972B
	public static bool Initialized
	{
		get
		{
			return SteamManager.s_instance != null && SteamManager.s_instance.m_bInitialized;
		}
	}

	// Token: 0x060010EA RID: 4330 RVA: 0x0007B546 File Offset: 0x00079746
	private static void SteamAPIDebugTextHook(int nSeverity, StringBuilder pchDebugText)
	{
		Debug.LogWarning(pchDebugText);
	}

	// Token: 0x060010EB RID: 4331 RVA: 0x0007B54E File Offset: 0x0007974E
	public static void SetServerPort(int port)
	{
		SteamManager.m_serverPort = port;
	}

	// Token: 0x060010EC RID: 4332 RVA: 0x0007B558 File Offset: 0x00079758
	private uint LoadAPPID()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("SteamAppId");
		if (environmentVariable != null)
		{
			ZLog.Log("Using environment steamid " + environmentVariable);
			return uint.Parse(environmentVariable);
		}
		try
		{
			string s = File.ReadAllText("steam_appid.txt");
			ZLog.Log("Using steam_appid.txt");
			return uint.Parse(s);
		}
		catch
		{
		}
		ZLog.LogWarning("Failed to find APPID");
		return 0U;
	}

	// Token: 0x060010ED RID: 4333 RVA: 0x0007B5C8 File Offset: 0x000797C8
	private void Awake()
	{
		if (SteamManager.s_instance != null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		SteamManager.s_instance = this;
		SteamManager.APP_ID = this.LoadAPPID();
		ZLog.Log("Using steam APPID:" + SteamManager.APP_ID.ToString());
		if (!SteamManager.ACCEPTED_APPIDs.Contains(SteamManager.APP_ID))
		{
			ZLog.Log("Invalid APPID");
			Application.Quit();
			return;
		}
		if (SteamManager.s_EverInialized)
		{
			throw new Exception("Tried to Initialize the SteamAPI twice in one session!");
		}
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		if (!Packsize.Test())
		{
			Debug.LogError("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.", this);
		}
		if (!DllCheck.Test())
		{
			Debug.LogError("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.", this);
		}
		try
		{
			if (SteamAPI.RestartAppIfNecessary((AppId_t)SteamManager.APP_ID))
			{
				Application.Quit();
				return;
			}
		}
		catch (DllNotFoundException ex)
		{
			string str = "[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n";
			DllNotFoundException ex2 = ex;
			Debug.LogError(str + ((ex2 != null) ? ex2.ToString() : null), this);
			Application.Quit();
			return;
		}
		this.m_bInitialized = SteamAPI.Init();
		if (!this.m_bInitialized)
		{
			Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.", this);
			return;
		}
		ZLog.Log("Authentication:" + SteamNetworkingSockets.InitAuthentication().ToString());
		SteamManager.s_EverInialized = true;
	}

	// Token: 0x060010EE RID: 4334 RVA: 0x0007B714 File Offset: 0x00079914
	private void OnEnable()
	{
		if (SteamManager.s_instance == null)
		{
			SteamManager.s_instance = this;
		}
		if (!this.m_bInitialized)
		{
			return;
		}
		if (this.m_SteamAPIWarningMessageHook == null)
		{
			this.m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamManager.SteamAPIDebugTextHook);
			SteamClient.SetWarningMessageHook(this.m_SteamAPIWarningMessageHook);
		}
	}

	// Token: 0x060010EF RID: 4335 RVA: 0x0007B762 File Offset: 0x00079962
	private void OnDestroy()
	{
		ZLog.Log("Steam manager on destroy");
		if (SteamManager.s_instance != this)
		{
			return;
		}
		SteamManager.s_instance = null;
		if (!this.m_bInitialized)
		{
			return;
		}
		SteamAPI.Shutdown();
	}

	// Token: 0x060010F0 RID: 4336 RVA: 0x0007B790 File Offset: 0x00079990
	private void Update()
	{
		if (!this.m_bInitialized)
		{
			return;
		}
		SteamAPI.RunCallbacks();
	}

	// Token: 0x04000FF8 RID: 4088
	public static uint[] ACCEPTED_APPIDs = new uint[]
	{
		1223920U,
		892970U
	};

	// Token: 0x04000FF9 RID: 4089
	public static uint APP_ID = 0U;

	// Token: 0x04000FFA RID: 4090
	private static int m_serverPort = 2456;

	// Token: 0x04000FFB RID: 4091
	private static SteamManager s_instance;

	// Token: 0x04000FFC RID: 4092
	private static bool s_EverInialized;

	// Token: 0x04000FFD RID: 4093
	private bool m_bInitialized;

	// Token: 0x04000FFE RID: 4094
	private SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;
}
