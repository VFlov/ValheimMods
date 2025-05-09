using System;
using System.Text;
using Steamworks;
using UnityEngine;

// Token: 0x0200010B RID: 267
[DisallowMultipleComponent]
public class SteamManager2 : MonoBehaviour
{
	// Token: 0x17000096 RID: 150
	// (get) Token: 0x060010F3 RID: 4339 RVA: 0x0007B7D5 File Offset: 0x000799D5
	protected static SteamManager2 Instance
	{
		get
		{
			if (SteamManager2.s_instance == null)
			{
				return new GameObject("SteamManager").AddComponent<SteamManager2>();
			}
			return SteamManager2.s_instance;
		}
	}

	// Token: 0x17000097 RID: 151
	// (get) Token: 0x060010F4 RID: 4340 RVA: 0x0007B7F9 File Offset: 0x000799F9
	public static bool Initialized
	{
		get
		{
			return SteamManager2.Instance.m_bInitialized;
		}
	}

	// Token: 0x060010F5 RID: 4341 RVA: 0x0007B805 File Offset: 0x00079A05
	protected static void SteamAPIDebugTextHook(int nSeverity, StringBuilder pchDebugText)
	{
		Debug.LogWarning(pchDebugText);
	}

	// Token: 0x060010F6 RID: 4342 RVA: 0x0007B810 File Offset: 0x00079A10
	protected virtual void Awake()
	{
		if (SteamManager2.s_instance != null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		SteamManager2.s_instance = this;
		if (SteamManager2.s_EverInitialized)
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
			if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
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
		SteamManager2.s_EverInitialized = true;
	}

	// Token: 0x060010F7 RID: 4343 RVA: 0x0007B8F0 File Offset: 0x00079AF0
	protected virtual void OnEnable()
	{
		if (SteamManager2.s_instance == null)
		{
			SteamManager2.s_instance = this;
		}
		if (!this.m_bInitialized)
		{
			return;
		}
		if (this.m_SteamAPIWarningMessageHook == null)
		{
			this.m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamManager2.SteamAPIDebugTextHook);
			SteamClient.SetWarningMessageHook(this.m_SteamAPIWarningMessageHook);
		}
	}

	// Token: 0x060010F8 RID: 4344 RVA: 0x0007B93E File Offset: 0x00079B3E
	protected virtual void OnDestroy()
	{
		if (SteamManager2.s_instance != this)
		{
			return;
		}
		SteamManager2.s_instance = null;
		if (!this.m_bInitialized)
		{
			return;
		}
		SteamAPI.Shutdown();
	}

	// Token: 0x060010F9 RID: 4345 RVA: 0x0007B962 File Offset: 0x00079B62
	protected virtual void Update()
	{
		if (!this.m_bInitialized)
		{
			return;
		}
		SteamAPI.RunCallbacks();
	}

	// Token: 0x04000FFF RID: 4095
	protected static bool s_EverInitialized;

	// Token: 0x04001000 RID: 4096
	protected static SteamManager2 s_instance;

	// Token: 0x04001001 RID: 4097
	protected bool m_bInitialized;

	// Token: 0x04001002 RID: 4098
	protected SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;
}
