using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GUIFramework;
using SoftReferenceableAssets.SceneManagement;
using Splatform;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Token: 0x02000119 RID: 281
public class FejdStartup : MonoBehaviour
{
	// Token: 0x1700009B RID: 155
	// (get) Token: 0x06001195 RID: 4501 RVA: 0x00082F7E File Offset: 0x0008117E
	public static FejdStartup instance
	{
		get
		{
			return FejdStartup.m_instance;
		}
	}

	// Token: 0x06001196 RID: 4502 RVA: 0x00082F88 File Offset: 0x00081188
	private void Awake()
	{
		FejdStartup.m_instance = this;
		this.ParseArguments();
		this.m_crossplayServerToggle.gameObject.SetActive(true);
		if (!FejdStartup.AwakePlatforms())
		{
			return;
		}
		Settings.SetPlatformDefaultPrefs();
		QualitySettings.maxQueuedFrames = 2;
		ZLog.Log(string.Concat(new string[]
		{
			"Valheim version: ",
			global::Version.GetVersionString(false),
			" (network version ",
			34U.ToString(),
			")"
		}));
		Settings.ApplyStartupSettings();
		WorldGenerator.Initialize(World.GetMenuWorld());
		if (!global::Console.instance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_consolePrefab);
		}
		this.m_mainCamera.transform.position = this.m_cameraMarkerMain.transform.position;
		this.m_mainCamera.transform.rotation = this.m_cameraMarkerMain.transform.rotation;
		ZLog.Log("Render threading mode:" + SystemInfo.renderingThreadingMode.ToString());
		Gogan.StartSession();
		Gogan.LogEvent("Game", "Version", global::Version.GetVersionString(false), 0L);
		Gogan.LogEvent("Game", "SteamID", SteamManager.APP_ID.ToString(), 0L);
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			Transform transform = this.m_mainMenu.transform.Find("showlog");
			if (transform != null)
			{
				transform.gameObject.SetActive(false);
			}
		}
		this.m_menuButtons = this.m_menuList.GetComponentsInChildren<Button>();
		TabHandler[] enabledComponentsInChildren = Utils.GetEnabledComponentsInChildren<TabHandler>(this.m_startGamePanel.gameObject);
		TabHandler[] array = enabledComponentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		this.m_startGamePanel.gameObject.SetActive(true);
		this.m_serverOptions.gameObject.SetActive(true);
		this.m_serverOptions.gameObject.SetActive(false);
		this.m_startGamePanel.gameObject.SetActive(false);
		array = enabledComponentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = true;
		}
		Game.Unpause();
		Time.timeScale = 1f;
		ZInput.Initialize();
		ZInput.WorkaroundEnabled = false;
		ZInput.OnInputLayoutChanged += this.UpdateCursor;
		this.UpdateCursor();
	}

	// Token: 0x06001197 RID: 4503 RVA: 0x000831E0 File Offset: 0x000813E0
	public static bool AwakePlatforms()
	{
		if (FejdStartup.s_monoUpdaters == null)
		{
			FejdStartup.s_monoUpdaters = new GameObject();
			FejdStartup.s_monoUpdaters.AddComponent<MonoUpdaters>();
			UnityEngine.Object.DontDestroyOnLoad(FejdStartup.s_monoUpdaters);
		}
		if (!FejdStartup.AwakeSteam() || !FejdStartup.AwakePlayFab())
		{
			ZLog.LogError("Awake of network backend failed");
			return false;
		}
		return true;
	}

	// Token: 0x06001198 RID: 4504 RVA: 0x00083234 File Offset: 0x00081434
	private static bool AwakePlayFab()
	{
		PlayFabManager.Initialize();
		return true;
	}

	// Token: 0x06001199 RID: 4505 RVA: 0x0008323C File Offset: 0x0008143C
	private static bool AwakeSteam()
	{
		return FejdStartup.InitializeSteam();
	}

	// Token: 0x0600119A RID: 4506 RVA: 0x00083248 File Offset: 0x00081448
	private void OnDestroy()
	{
		SaveSystem.ClearWorldListCache(false);
		FejdStartup.m_instance = null;
		ZInput.OnInputLayoutChanged -= this.UpdateCursor;
		Localization.OnLanguageChange = (Action)Delegate.Remove(Localization.OnLanguageChange, new Action(this.OnLanguageChange));
	}

	// Token: 0x0600119B RID: 4507 RVA: 0x00083287 File Offset: 0x00081487
	private void OnApplicationQuit()
	{
		HeightmapBuilder.instance.Dispose();
	}

	// Token: 0x0600119C RID: 4508 RVA: 0x00083293 File Offset: 0x00081493
	private void OnEnable()
	{
		FejdStartup.startGameEvent += this.AddToServerList;
	}

	// Token: 0x0600119D RID: 4509 RVA: 0x000832A6 File Offset: 0x000814A6
	private void OnDisable()
	{
		FejdStartup.startGameEvent -= this.AddToServerList;
	}

	// Token: 0x0600119E RID: 4510 RVA: 0x000832B9 File Offset: 0x000814B9
	private void AddToServerList(object sender, FejdStartup.StartGameEventArgs e)
	{
		if (!e.isHost)
		{
			ServerList.AddToRecentServersList(this.GetServerToJoin());
		}
	}

	// Token: 0x0600119F RID: 4511 RVA: 0x000832D0 File Offset: 0x000814D0
	private void Start()
	{
		this.SetupGui();
		this.SetupObjectDB();
		this.m_openServerToggle.onValueChanged.AddListener(new UnityAction<bool>(this.OnOpenServerToggleClicked));
		MusicMan.instance.Reset();
		MusicMan.instance.TriggerMusic("menu");
		this.ShowConnectError(ZNet.ConnectionStatus.None);
		ZSteamMatchmaking.Initialize();
		if (FejdStartup.m_firstStartup)
		{
			this.HandleStartupJoin();
		}
		this.m_menuAnimator.SetBool("FirstStartup", FejdStartup.m_firstStartup);
		FejdStartup.m_firstStartup = false;
		string @string = PlayerPrefs.GetString("profile");
		if (@string.Length > 0)
		{
			this.SetSelectedProfile(@string);
		}
		else
		{
			this.m_profiles = SaveSystem.GetAllPlayerProfiles();
			if (this.m_profiles.Count > 0)
			{
				this.SetSelectedProfile(this.m_profiles[0].GetFilename());
			}
			else
			{
				this.UpdateCharacterList();
			}
		}
		CensorShittyWords.UGCPopupShown = (Action)Delegate.Remove(CensorShittyWords.UGCPopupShown, new Action(this.OnUGCPopupShown));
		CensorShittyWords.UGCPopupShown = (Action)Delegate.Combine(CensorShittyWords.UGCPopupShown, new Action(this.OnUGCPopupShown));
		SaveSystem.ClearWorldListCache(true);
		Player.m_debugMode = false;
	}

	// Token: 0x060011A0 RID: 4512 RVA: 0x000833F4 File Offset: 0x000815F4
	private void SetupGui()
	{
		this.HideAll();
		this.m_mainMenu.SetActive(true);
		if (SteamManager.APP_ID == 1223920U)
		{
			this.m_betaText.SetActive(true);
			if (!Debug.isDebugBuild && !this.AcceptedNDA())
			{
				this.m_ndaPanel.SetActive(true);
				this.m_mainMenu.SetActive(false);
			}
		}
		this.m_moddedText.SetActive(Game.isModded);
		this.m_worldListBaseSize = this.m_worldListRoot.rect.height;
		this.m_versionLabel.text = string.Format("Version {0} (n-{1})", global::Version.GetVersionString(false), 34U);
		Localization.instance.Localize(base.transform);
		Localization.OnLanguageChange = (Action)Delegate.Combine(Localization.OnLanguageChange, new Action(this.OnLanguageChange));
	}

	// Token: 0x060011A1 RID: 4513 RVA: 0x000834D0 File Offset: 0x000816D0
	private void HideAll()
	{
		this.m_worldVersionPanel.SetActive(false);
		this.m_playerVersionPanel.SetActive(false);
		this.m_newGameVersionPanel.SetActive(false);
		this.m_loading.SetActive(false);
		this.m_pleaseWait.SetActive(false);
		this.m_characterSelectScreen.SetActive(false);
		this.m_creditsPanel.SetActive(false);
		this.m_startGamePanel.SetActive(false);
		this.m_createWorldPanel.SetActive(false);
		this.m_serverOptions.gameObject.SetActive(false);
		this.m_mainMenu.SetActive(false);
		this.m_ndaPanel.SetActive(false);
		this.m_betaText.SetActive(false);
	}

	// Token: 0x060011A2 RID: 4514 RVA: 0x00083580 File Offset: 0x00081780
	public static bool InitializeSteam()
	{
		if (SteamManager.Initialize())
		{
			string personaName = SteamFriends.GetPersonaName();
			ZLog.Log("Steam initialized, persona:" + personaName);
			return true;
		}
		ZLog.LogError("Steam is not initialized");
		Application.Quit();
		return false;
	}

	// Token: 0x060011A3 RID: 4515 RVA: 0x000835BC File Offset: 0x000817BC
	private void HandleStartupJoin()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string a = commandLineArgs[i];
			if (a == "+connect" && i < commandLineArgs.Length - 1)
			{
				string text = commandLineArgs[i + 1];
				ZLog.Log("JOIN " + text);
				ZSteamMatchmaking.instance.QueueServerJoin(text);
			}
			else if (a == "+connect_lobby" && i < commandLineArgs.Length - 1)
			{
				string s = commandLineArgs[i + 1];
				CSteamID lobbyID = new CSteamID(ulong.Parse(s));
				ZSteamMatchmaking.instance.QueueLobbyJoin(lobbyID);
			}
		}
	}

	// Token: 0x060011A4 RID: 4516 RVA: 0x00083650 File Offset: 0x00081850
	private void ParseArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (commandLineArgs[i] == "-console")
			{
				global::Console.SetConsoleEnabled(true);
			}
		}
	}

	// Token: 0x060011A5 RID: 4517 RVA: 0x00083688 File Offset: 0x00081888
	private bool ParseServerArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string text = "Dedicated";
		string password = "";
		string text2 = "";
		int num = 2456;
		bool flag = true;
		ZNet.m_backupCount = 4;
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string text3 = commandLineArgs[i].ToLower();
			int backupCount;
			int b;
			int b2;
			int b3;
			if (text3 == "-world")
			{
				string text4 = commandLineArgs[i + 1];
				if (text4 != "")
				{
					text = text4;
				}
				i++;
			}
			else if (text3 == "-name")
			{
				string text5 = commandLineArgs[i + 1];
				if (text5 != "")
				{
					text2 = text5;
				}
				i++;
			}
			else if (text3 == "-port")
			{
				string text6 = commandLineArgs[i + 1];
				if (text6 != "")
				{
					num = int.Parse(text6);
				}
				i++;
			}
			else if (text3 == "-password")
			{
				password = commandLineArgs[i + 1];
				i++;
			}
			else if (text3 == "-savedir")
			{
				string text7 = commandLineArgs[i + 1];
				Utils.SetSaveDataPath(text7);
				ZLog.Log("Setting -savedir to: " + text7);
				i++;
			}
			else if (text3 == "-public")
			{
				string a = commandLineArgs[i + 1];
				if (a != "")
				{
					flag = (a == "1");
				}
				i++;
			}
			else if (text3.ToLower() == "-logfile")
			{
				ZLog.Log("Setting -logfile to: " + commandLineArgs[i + 1]);
			}
			else if (text3 == "-crossplay")
			{
				ZNet.m_onlineBackend = OnlineBackendType.PlayFab;
			}
			else if (text3 == "-instanceid" && commandLineArgs.Length > i + 1)
			{
				FejdStartup.InstanceId = commandLineArgs[i + 1];
				i++;
			}
			else if (text3.ToLower() == "-backups" && int.TryParse(commandLineArgs[i + 1], out backupCount))
			{
				ZNet.m_backupCount = backupCount;
			}
			else if (text3 == "-backupshort" && int.TryParse(commandLineArgs[i + 1], out b))
			{
				ZNet.m_backupShort = Mathf.Max(5, b);
			}
			else if (text3 == "-backuplong" && int.TryParse(commandLineArgs[i + 1], out b2))
			{
				ZNet.m_backupLong = Mathf.Max(5, b2);
			}
			else if (text3 == "-saveinterval" && int.TryParse(commandLineArgs[i + 1], out b3))
			{
				Game.m_saveInterval = (float)Mathf.Max(5, b3);
			}
		}
		if (text2 == "")
		{
			text2 = text;
		}
		World createWorld = World.GetCreateWorld(text, FileHelpers.FileSource.Local);
		if (!ServerOptionsGUI.m_instance)
		{
			UnityEngine.Object.Instantiate<ServerOptionsGUI>(this.m_serverOptions).gameObject.SetActive(true);
		}
		for (int j = 0; j < commandLineArgs.Length; j++)
		{
			string a2 = commandLineArgs[j].ToLower();
			if (a2 == "-resetmodifiers")
			{
				createWorld.m_startingGlobalKeys.Clear();
				createWorld.m_startingKeysChanged = true;
				ZLog.Log("Resetting world modifiers");
			}
			else if (a2 == "-preset" && commandLineArgs.Length > j + 1)
			{
				string text8 = commandLineArgs[j + 1];
				WorldPresets preset;
				if (Enum.TryParse<WorldPresets>(text8, true, out preset))
				{
					createWorld.m_startingGlobalKeys.Clear();
					createWorld.m_startingKeysChanged = true;
					ServerOptionsGUI.m_instance.ReadKeys(createWorld);
					ServerOptionsGUI.m_instance.SetPreset(createWorld, preset);
					ServerOptionsGUI.m_instance.SetKeys(createWorld);
					ZLog.Log("Setting world modifier preset: " + text8);
				}
				else
				{
					ZLog.LogError("Could not parse '" + text8 + "' as a world modifier preset.");
				}
			}
			else if (a2 == "-modifier" && commandLineArgs.Length > j + 2)
			{
				string text9 = commandLineArgs[j + 1];
				string text10 = commandLineArgs[j + 2];
				WorldModifiers preset2;
				WorldModifierOption value;
				if (Enum.TryParse<WorldModifiers>(text9, true, out preset2) && Enum.TryParse<WorldModifierOption>(text10, true, out value))
				{
					ServerOptionsGUI.m_instance.ReadKeys(createWorld);
					ServerOptionsGUI.m_instance.SetPreset(createWorld, preset2, value);
					ServerOptionsGUI.m_instance.SetKeys(createWorld);
					ZLog.Log("Setting world modifier: " + text9 + "->" + text10);
				}
				else
				{
					ZLog.LogError(string.Concat(new string[]
					{
						"Could not parse '",
						text9,
						"' with a value of '",
						text10,
						"' as a world modifier."
					}));
				}
			}
			else if (a2 == "-setkey" && commandLineArgs.Length > j + 1)
			{
				string text11 = commandLineArgs[j + 1];
				if (!createWorld.m_startingGlobalKeys.Contains(text11))
				{
					createWorld.m_startingGlobalKeys.Add(text11.ToLower());
				}
			}
		}
		if (flag && !this.IsPublicPasswordValid(password, createWorld))
		{
			string publicPasswordError = this.GetPublicPasswordError(password, createWorld);
			ZLog.LogError("Error bad password:" + publicPasswordError);
			Application.Quit();
			return false;
		}
		ZNet.SetServer(true, true, flag, text2, password, createWorld);
		ZNet.ResetServerHost();
		SteamManager.SetServerPort(num);
		ZSteamSocket.SetDataPort(num);
		ZPlayFabMatchmaking.SetDataPort(num);
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			ZPlayFabMatchmaking.LookupPublicIP();
		}
		return true;
	}

	// Token: 0x060011A6 RID: 4518 RVA: 0x00083BC4 File Offset: 0x00081DC4
	private void SetupObjectDB()
	{
		ObjectDB objectDB = base.gameObject.AddComponent<ObjectDB>();
		ObjectDB component = this.m_objectDBPrefab.GetComponent<ObjectDB>();
		objectDB.CopyOtherDB(component);
	}

	// Token: 0x060011A7 RID: 4519 RVA: 0x00083BF0 File Offset: 0x00081DF0
	private void ShowConnectError(ZNet.ConnectionStatus statusOverride = ZNet.ConnectionStatus.None)
	{
		ZNet.ConnectionStatus connectionStatus = (statusOverride == ZNet.ConnectionStatus.None) ? ZNet.GetConnectionStatus() : statusOverride;
		if (ZNet.m_loadError)
		{
			this.m_connectionFailedPanel.SetActive(true);
			this.m_connectionFailedError.text = Localization.instance.Localize("$error_worldfileload");
		}
		if (ZNet.m_loadError)
		{
			this.m_connectionFailedPanel.SetActive(true);
			this.m_connectionFailedError.text = Localization.instance.Localize("$error_worldfileload");
		}
		if (connectionStatus != ZNet.ConnectionStatus.Connected && connectionStatus != ZNet.ConnectionStatus.Connecting && connectionStatus != ZNet.ConnectionStatus.None)
		{
			this.m_connectionFailedPanel.SetActive(true);
			switch (connectionStatus)
			{
			case ZNet.ConnectionStatus.ErrorVersion:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_incompatibleversion");
				return;
			case ZNet.ConnectionStatus.ErrorDisconnected:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_disconnected");
				return;
			case ZNet.ConnectionStatus.ErrorConnectFailed:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_failedconnect");
				return;
			case ZNet.ConnectionStatus.ErrorPassword:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_password");
				return;
			case ZNet.ConnectionStatus.ErrorAlreadyConnected:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_alreadyconnected");
				return;
			case ZNet.ConnectionStatus.ErrorBanned:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_banned");
				return;
			case ZNet.ConnectionStatus.ErrorFull:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_serverfull");
				return;
			case ZNet.ConnectionStatus.ErrorPlatformExcluded:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_platformexcluded");
				return;
			case ZNet.ConnectionStatus.ErrorCrossplayPrivilege:
				this.m_connectionFailedError.text = Localization.instance.Localize("$xbox_error_crossplayprivilege");
				return;
			case ZNet.ConnectionStatus.ErrorKicked:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_kicked");
				break;
			default:
				return;
			}
		}
	}

	// Token: 0x060011A8 RID: 4520 RVA: 0x00083DC1 File Offset: 0x00081FC1
	public void OnNewVersionButtonDownload()
	{
		Application.OpenURL(this.m_downloadUrl);
		Application.Quit();
	}

	// Token: 0x060011A9 RID: 4521 RVA: 0x00083DD3 File Offset: 0x00081FD3
	public void OnNewVersionButtonContinue()
	{
		this.m_newGameVersionPanel.SetActive(false);
	}

	// Token: 0x060011AA RID: 4522 RVA: 0x00083DE4 File Offset: 0x00081FE4
	public void OnStartGame()
	{
		Gogan.LogEvent("Screen", "Enter", "StartGame", 0L);
		this.m_mainMenu.SetActive(false);
		if (SaveSystem.GetAllPlayerProfiles().Count == 0)
		{
			this.ShowCharacterSelection();
			this.OnCharacterNew();
			return;
		}
		this.ShowCharacterSelection();
	}

	// Token: 0x060011AB RID: 4523 RVA: 0x00083E32 File Offset: 0x00082032
	private void ShowStartGame()
	{
		this.m_mainMenu.SetActive(false);
		this.m_createWorldPanel.SetActive(false);
		this.m_serverOptions.gameObject.SetActive(false);
		this.m_startGamePanel.SetActive(true);
		this.RefreshWorldSelection();
	}

	// Token: 0x060011AC RID: 4524 RVA: 0x00083E6F File Offset: 0x0008206F
	public void OnSelectWorldTab()
	{
		this.RefreshWorldSelection();
	}

	// Token: 0x060011AD RID: 4525 RVA: 0x00083E78 File Offset: 0x00082078
	private void RefreshWorldSelection()
	{
		this.UpdateWorldList(true);
		if (this.m_world != null)
		{
			this.m_world = this.FindWorld(this.m_world.m_name);
			if (this.m_world != null)
			{
				this.UpdateWorldList(true);
			}
		}
		if (this.m_world == null)
		{
			string @string = PlayerPrefs.GetString("world");
			if (@string.Length > 0)
			{
				this.m_world = this.FindWorld(@string);
			}
			if (this.m_world == null)
			{
				this.m_world = ((this.m_worlds.Count > 0) ? this.m_worlds[0] : null);
			}
			if (this.m_world != null)
			{
				this.UpdateWorldList(true);
			}
			this.m_crossplayServerToggle.isOn = (PlayerPrefs.GetInt("crossplay", 1) == 1);
		}
	}

	// Token: 0x060011AE RID: 4526 RVA: 0x00083F3C File Offset: 0x0008213C
	public void OnServerListTab()
	{
		if (!PlayFabManager.IsLoggedIn && PlayFabManager.CurrentLoginState != LoginState.AttemptingLogin)
		{
			PlayFabManager.instance.SetShouldTryAutoLogin(true);
		}
		if (PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.OnlineMultiplayer) != PrivilegeResult.Granted)
		{
			this.m_startGamePanel.transform.GetChild(0).GetComponent<TabHandler>().SetActiveTab(0, false, true);
			this.ShowOnlineMultiplayerPrivilegeWarning();
		}
	}

	// Token: 0x060011AF RID: 4527 RVA: 0x00083F9C File Offset: 0x0008219C
	private void OnOpenServerToggleClicked(bool value)
	{
		if (!PlayFabManager.IsLoggedIn && PlayFabManager.CurrentLoginState != LoginState.AttemptingLogin)
		{
			PlayFabManager.instance.SetShouldTryAutoLogin(value);
		}
		if (!value)
		{
			return;
		}
		if (PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.OnlineMultiplayer) != PrivilegeResult.Granted)
		{
			this.m_openServerToggle.isOn = false;
			this.ShowOnlineMultiplayerPrivilegeWarning();
		}
	}

	// Token: 0x060011B0 RID: 4528 RVA: 0x00083FEC File Offset: 0x000821EC
	private void ShowLogInWithPlayFabWindow(bool openServerToggleValue = true)
	{
		if (openServerToggleValue && !PlatformManager.DistributionPlatform.LocalUser.IsSignedIn)
		{
			if (PlatformManager.DistributionPlatform.UIProvider.SignInLocalUser != null)
			{
				PlatformManager.DistributionPlatform.UIProvider.SignInLocalUser.Open();
				return;
			}
		}
		else if (openServerToggleValue && !PlayFabManager.IsLoggedIn)
		{
			UnifiedPopup.Push(new YesNoPopup("$menu_loginwithplayfab_header", "$menu_loginwithplayfab_text", delegate()
			{
				PlayFabManager.instance.SetShouldTryAutoLogin(true);
				UnifiedPopup.Pop();
				UnifiedPopup.Push(new TaskPopup("$menu_logging_in_playfab_task_header", "", true));
				PlayFabManager.instance.LoginFinished -= PlayFabManager.instance.OnPlayFabRespondRemoveUIBlock;
				PlayFabManager.instance.LoginFinished += PlayFabManager.instance.OnPlayFabRespondRemoveUIBlock;
			}, delegate()
			{
				PlayFabManager.instance.SetShouldTryAutoLogin(false);
				UnifiedPopup.Pop();
				PlayFabManager.instance.ResetMainMenuButtons();
			}, true));
		}
	}

	// Token: 0x060011B1 RID: 4529 RVA: 0x00084090 File Offset: 0x00082290
	private void ShowOnlineMultiplayerPrivilegeWarning()
	{
		if (PlayFabManager.CurrentLoginState != LoginState.LoggedIn)
		{
			string str = " Steam";
			UnifiedPopup.Push(new WarningPopup("$menu_logintext", "$menu_loginfailedtext" + str, delegate()
			{
				this.RefreshWorldSelection();
				UnifiedPopup.Pop();
			}, true));
			return;
		}
		if (PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege != null)
		{
			PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.OnlineMultiplayer);
			return;
		}
		UnifiedPopup.Push(new WarningPopup("$menu_privilegerequiredheader", "$menu_onlineprivilegetext", delegate()
		{
			this.RefreshWorldSelection();
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x060011B2 RID: 4530 RVA: 0x00084121 File Offset: 0x00082321
	private void OnUGCPopupShown()
	{
		this.RefreshWorldSelection();
	}

	// Token: 0x060011B3 RID: 4531 RVA: 0x0008412C File Offset: 0x0008232C
	private World FindWorld(string name)
	{
		foreach (World world in this.m_worlds)
		{
			if (world.m_name == name)
			{
				return world;
			}
		}
		return null;
	}

	// Token: 0x060011B4 RID: 4532 RVA: 0x00084190 File Offset: 0x00082390
	private void UpdateWorldList(bool centerSelection)
	{
		this.m_worlds = SaveSystem.GetWorldList();
		float num = (float)this.m_worlds.Count * this.m_worldListElementStep;
		num = Mathf.Max(this.m_worldListBaseSize, num);
		this.m_worldListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		for (int i = 0; i < this.m_worlds.Count; i++)
		{
			World world = this.m_worlds[i];
			GameObject gameObject;
			if (i < this.m_worldListElements.Count)
			{
				gameObject = this.m_worldListElements[i];
			}
			else
			{
				gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_worldListElement, this.m_worldListRoot);
				this.m_worldListElements.Add(gameObject);
				gameObject.SetActive(true);
			}
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * -this.m_worldListElementStep);
			Button component = gameObject.GetComponent<Button>();
			component.onClick.RemoveAllListeners();
			int index = i;
			component.onClick.AddListener(delegate()
			{
				this.OnSelectWorld(index);
			});
			TMP_Text component2 = gameObject.transform.Find("seed").GetComponent<TMP_Text>();
			component2.text = world.m_seedName;
			gameObject.transform.Find("modifiers").GetComponent<TMP_Text>().text = Localization.instance.Localize(ServerOptionsGUI.GetWorldModifierSummary(world.m_startingGlobalKeys, true, ", "));
			TMP_Text component3 = gameObject.transform.Find("name").GetComponent<TMP_Text>();
			if (world.m_name == world.m_fileName)
			{
				component3.text = world.m_name;
			}
			else
			{
				component3.text = world.m_name + " (" + world.m_fileName + ")";
			}
			Transform transform = gameObject.transform.Find("source_cloud");
			if (transform != null)
			{
				transform.gameObject.SetActive(world.m_fileSource == FileHelpers.FileSource.Cloud);
			}
			Transform transform2 = gameObject.transform.Find("source_local");
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(world.m_fileSource == FileHelpers.FileSource.Local);
			}
			Transform transform3 = gameObject.transform.Find("source_legacy");
			if (transform3 != null)
			{
				transform3.gameObject.SetActive(world.m_fileSource == FileHelpers.FileSource.Legacy);
			}
			switch (world.m_dataError)
			{
			case World.SaveDataError.None:
				break;
			case World.SaveDataError.BadVersion:
				component2.text = " [BAD VERSION]";
				break;
			case World.SaveDataError.LoadError:
				component2.text = " [LOAD ERROR]";
				break;
			case World.SaveDataError.Corrupt:
				component2.text = " [CORRUPT]";
				break;
			case World.SaveDataError.MissingMeta:
				component2.text = " [MISSING META]";
				break;
			case World.SaveDataError.MissingDB:
				component2.text = " [MISSING DB]";
				break;
			default:
				component2.text = string.Format(" [{0}]", world.m_dataError);
				break;
			}
			RectTransform rectTransform = gameObject.transform.Find("selected") as RectTransform;
			bool flag = this.m_world != null && world.m_fileName == this.m_world.m_fileName;
			if (flag && this.m_world != world)
			{
				this.m_world = world;
			}
			rectTransform.gameObject.SetActive(flag);
			if (flag)
			{
				component.Select();
			}
			if (flag && centerSelection)
			{
				this.m_worldListEnsureVisible.CenterOnItem(rectTransform);
			}
		}
		for (int j = this.m_worldListElements.Count - 1; j >= this.m_worlds.Count; j--)
		{
			UnityEngine.Object.Destroy(this.m_worldListElements[j]);
			this.m_worldListElements.RemoveAt(j);
		}
		this.m_worldSourceInfo.text = "";
		this.m_worldSourceInfoPanel.SetActive(false);
		if (this.m_world != null)
		{
			this.m_worldSourceInfo.text = Localization.instance.Localize(((this.m_world.m_fileSource == FileHelpers.FileSource.Legacy) ? "$menu_legacynotice \n\n$menu_legacynotice_worlds \n\n" : "") + ((!FileHelpers.CloudStorageEnabled) ? "$menu_cloudsavesdisabled" : ""));
			this.m_worldSourceInfoPanel.SetActive(this.m_worldSourceInfo.text.Length > 0);
		}
		for (int k = 0; k < this.m_worlds.Count; k++)
		{
			World world2 = this.m_worlds[k];
			UITooltip componentInChildren = this.m_worldListElements[k].GetComponentInChildren<UITooltip>();
			if (componentInChildren != null)
			{
				string worldModifierSummary = ServerOptionsGUI.GetWorldModifierSummary(world2.m_startingGlobalKeys, false, "\n");
				componentInChildren.Set(string.IsNullOrEmpty(worldModifierSummary) ? "" : "$menu_serveroptions", worldModifierSummary, this.m_worldSourceInfoPanel.activeSelf ? this.m_tooltipSecondaryAnchor : this.m_tooltipAnchor, default(Vector2));
			}
		}
	}

	// Token: 0x060011B5 RID: 4533 RVA: 0x00084652 File Offset: 0x00082852
	public void OnWorldRemove()
	{
		if (this.m_world == null)
		{
			return;
		}
		this.m_removeWorldName.text = this.m_world.m_fileName;
		this.m_removeWorldDialog.SetActive(true);
	}

	// Token: 0x060011B6 RID: 4534 RVA: 0x00084680 File Offset: 0x00082880
	public void OnButtonRemoveWorldYes()
	{
		World.RemoveWorld(this.m_world.m_fileName, this.m_world.m_fileSource);
		this.m_world = null;
		this.m_worlds = SaveSystem.GetWorldList();
		this.SetSelectedWorld(0, true);
		this.m_removeWorldDialog.SetActive(false);
	}

	// Token: 0x060011B7 RID: 4535 RVA: 0x000846CE File Offset: 0x000828CE
	public void OnButtonRemoveWorldNo()
	{
		this.m_removeWorldDialog.SetActive(false);
	}

	// Token: 0x060011B8 RID: 4536 RVA: 0x000846DC File Offset: 0x000828DC
	private void OnSelectWorld(int index)
	{
		this.SetSelectedWorld(index, false);
	}

	// Token: 0x060011B9 RID: 4537 RVA: 0x000846E6 File Offset: 0x000828E6
	private void SetSelectedWorld(int index, bool centerSelection)
	{
		if (this.m_worlds.Count != 0)
		{
			index = Mathf.Clamp(index, 0, this.m_worlds.Count - 1);
			this.m_world = this.m_worlds[index];
		}
		this.UpdateWorldList(centerSelection);
	}

	// Token: 0x060011BA RID: 4538 RVA: 0x00084724 File Offset: 0x00082924
	private int GetSelectedWorld()
	{
		if (this.m_world == null)
		{
			return -1;
		}
		for (int i = 0; i < this.m_worlds.Count; i++)
		{
			if (this.m_worlds[i].m_fileName == this.m_world.m_fileName)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x060011BB RID: 4539 RVA: 0x00084778 File Offset: 0x00082978
	private int FindSelectedWorld(GameObject button)
	{
		for (int i = 0; i < this.m_worldListElements.Count; i++)
		{
			if (this.m_worldListElements[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x060011BC RID: 4540 RVA: 0x000847B2 File Offset: 0x000829B2
	private FileHelpers.FileSource GetMoveTarget(FileHelpers.FileSource source)
	{
		if (source == FileHelpers.FileSource.Cloud)
		{
			return FileHelpers.FileSource.Local;
		}
		return FileHelpers.FileSource.Cloud;
	}

	// Token: 0x060011BD RID: 4541 RVA: 0x000847BB File Offset: 0x000829BB
	public void OnWorldNew()
	{
		this.m_createWorldPanel.SetActive(true);
		this.m_newWorldName.text = "";
		this.m_newWorldSeed.text = World.GenerateSeed();
	}

	// Token: 0x060011BE RID: 4542 RVA: 0x000847EC File Offset: 0x000829EC
	public void OnNewWorldDone(bool forceLocal)
	{
		string text = this.m_newWorldName.text;
		string text2 = this.m_newWorldSeed.text;
		if (World.HaveWorld(text))
		{
			UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$menu_newworldalreadyexists"), Localization.instance.Localize("$menu_newworldalreadyexistsmessage", new string[]
			{
				text
			}), delegate()
			{
				UnifiedPopup.Pop();
			}, false));
			return;
		}
		this.m_world = new World(text, text2);
		this.m_world.m_fileSource = ((FileHelpers.CloudStorageEnabled && !forceLocal) ? FileHelpers.FileSource.Cloud : FileHelpers.FileSource.Local);
		this.m_world.m_needsDB = false;
		if (this.m_world.m_fileSource == FileHelpers.FileSource.Cloud && FileHelpers.OperationExceedsCloudCapacity(2097152UL))
		{
			this.ShowCloudQuotaWorldDialog();
			ZLog.LogWarning("This operation may exceed the cloud save quota and has therefore been aborted! Prompt shown to user.");
			return;
		}
		this.m_world.SaveWorldMetaData(DateTime.Now);
		this.UpdateWorldList(true);
		this.ShowStartGame();
		Gogan.LogEvent("Menu", "NewWorld", text, 0L);
	}

	// Token: 0x060011BF RID: 4543 RVA: 0x000848F9 File Offset: 0x00082AF9
	public void OnNewWorldBack()
	{
		this.ShowStartGame();
	}

	// Token: 0x060011C0 RID: 4544 RVA: 0x00084904 File Offset: 0x00082B04
	public void OnServerOptions()
	{
		this.RefreshWorldSelection();
		this.m_serverOptions.gameObject.SetActive(true);
		this.m_serverOptions.ReadKeys(this.m_world);
		EventSystem.current.SetSelectedGameObject(this.m_serverOptions.m_doneButton);
		if (PlatformPrefs.GetInt("ServerOptionsDisclaimer", 0) == 0)
		{
			UnifiedPopup.Push(new WarningPopup("$menu_modifier_popup_title", "$menu_modifier_popup_text", delegate()
			{
				UnifiedPopup.Pop();
			}, true));
			PlatformPrefs.SetInt("ServerOptionsDisclaimer", 1);
		}
	}

	// Token: 0x060011C1 RID: 4545 RVA: 0x0008499C File Offset: 0x00082B9C
	public void OnServerOptionsDone()
	{
		this.m_world.m_startingGlobalKeys.Clear();
		this.m_world.m_startingKeysChanged = true;
		this.m_serverOptions.SetKeys(this.m_world);
		DateTime now = DateTime.Now;
		SaveWithBackups saveWithBackups;
		if (!SaveSystem.TryGetSaveByName(this.m_world.m_fileName, SaveDataType.World, out saveWithBackups) || saveWithBackups.IsDeleted)
		{
			ZLog.LogError("Failed to retrieve world save " + this.m_world.m_fileName + " by name when modifying server options!");
			this.ShowStartGame();
			return;
		}
		SaveSystem.CheckMove(this.m_world.m_fileName, SaveDataType.World, ref this.m_world.m_fileSource, now, saveWithBackups.PrimaryFile.Size, true);
		this.m_world.SaveWorldMetaData(now);
		this.UpdateWorldList(true);
		this.ShowStartGame();
	}

	// Token: 0x060011C2 RID: 4546 RVA: 0x00084A62 File Offset: 0x00082C62
	public void OnServerOptionsCancel()
	{
		this.ShowStartGame();
	}

	// Token: 0x060011C3 RID: 4547 RVA: 0x00084A6A File Offset: 0x00082C6A
	public void OnMerchStoreButton()
	{
		Application.OpenURL("http://valheim.shop/?game_" + global::Version.GetPlatformPrefix("win"));
	}

	// Token: 0x060011C4 RID: 4548 RVA: 0x00084A85 File Offset: 0x00082C85
	public void OnBoardGameButton()
	{
		Application.OpenURL("http://bit.ly/valheimtheboardgame");
	}

	// Token: 0x060011C5 RID: 4549 RVA: 0x00084A91 File Offset: 0x00082C91
	public void OnCloudStorageLowNextSaveWarningOk()
	{
		this.m_cloudStorageWarningNextSave.SetActive(false);
		this.RefreshWorldSelection();
	}

	// Token: 0x060011C6 RID: 4550 RVA: 0x00084AA8 File Offset: 0x00082CA8
	public void OnWorldStart()
	{
		if (!SaveSystem.CanSaveToCloudStorage(this.m_world, this.m_profiles[this.m_profileIndex]) || Menu.ExceedCloudStorageTest)
		{
			this.m_cloudStorageWarningNextSave.SetActive(true);
			return;
		}
		if (this.m_world == null || this.m_startingWorld)
		{
			return;
		}
		Game.m_serverOptionsSummary = "";
		switch (this.m_world.m_dataError)
		{
		case World.SaveDataError.None:
		{
			PlayerPrefs.SetString("world", this.m_world.m_name);
			if (this.m_crossplayServerToggle.IsInteractable())
			{
				PlayerPrefs.SetInt("crossplay", this.m_crossplayServerToggle.isOn ? 1 : 0);
			}
			bool isOn = this.m_publicServerToggle.isOn;
			bool isOn2 = this.m_openServerToggle.isOn;
			bool isOn3 = this.m_crossplayServerToggle.isOn;
			string text = this.m_serverPassword.text;
			OnlineBackendType onlineBackend = this.GetOnlineBackend(isOn3);
			if (isOn2 && onlineBackend == OnlineBackendType.PlayFab && !PlayFabManager.IsLoggedIn)
			{
				this.ContinueWhenLoggedInPopup(new FejdStartup.ContinueAction(this.OnWorldStart));
				return;
			}
			ZNet.m_onlineBackend = onlineBackend;
			ZSteamMatchmaking.instance.StopServerListing();
			this.m_startingWorld = true;
			ZNet.SetServer(true, isOn2, isOn, this.m_world.m_name, text, this.m_world);
			ZNet.ResetServerHost();
			string eventLabel = "open:" + isOn2.ToString() + ",public:" + isOn.ToString();
			Gogan.LogEvent("Menu", "WorldStart", eventLabel, 0L);
			FejdStartup.StartGameEventHandler startGameEventHandler = FejdStartup.startGameEvent;
			if (startGameEventHandler != null)
			{
				startGameEventHandler(this, new FejdStartup.StartGameEventArgs(true));
			}
			this.TransitionToMainScene();
			return;
		}
		case World.SaveDataError.BadVersion:
			return;
		case World.SaveDataError.LoadError:
		case World.SaveDataError.Corrupt:
		{
			SaveWithBackups saveWithBackups;
			if (!SaveSystem.TryGetSaveByName(this.m_world.m_name, SaveDataType.World, out saveWithBackups))
			{
				UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
				ZLog.LogError("Failed to restore backup! Couldn't get world " + this.m_world.m_name + " by name from save system.");
				return;
			}
			if (saveWithBackups.IsDeleted)
			{
				UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
				ZLog.LogError("Failed to restore backup! World " + this.m_world.m_name + " retrieved from save system was deleted.");
				return;
			}
			if (SaveSystem.HasRestorableBackup(saveWithBackups))
			{
				this.<OnWorldStart>g__RestoreBackupPrompt|50_1(saveWithBackups);
				return;
			}
			UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$error_nosuitablebackupfound", new PopupButtonCallback(UnifiedPopup.Pop), true));
			return;
		}
		case World.SaveDataError.MissingMeta:
		{
			SaveWithBackups saveWithBackups2;
			if (!SaveSystem.TryGetSaveByName(this.m_world.m_name, SaveDataType.World, out saveWithBackups2))
			{
				UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
				ZLog.LogError("Failed to restore meta file! Couldn't get world " + this.m_world.m_name + " by name from save system.");
				return;
			}
			if (saveWithBackups2.IsDeleted)
			{
				UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
				ZLog.LogError("Failed to restore meta file! World " + this.m_world.m_name + " retrieved from save system was deleted.");
				return;
			}
			if (SaveSystem.HasBackupWithMeta(saveWithBackups2))
			{
				this.<OnWorldStart>g__RestoreMetaFromBackupPrompt|50_0(saveWithBackups2);
				return;
			}
			UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$error_nosuitablebackupfound", new PopupButtonCallback(UnifiedPopup.Pop), true));
			return;
		}
		default:
			return;
		}
	}

	// Token: 0x060011C7 RID: 4551 RVA: 0x00084DF4 File Offset: 0x00082FF4
	private void ContinueWhenLoggedInPopup(FejdStartup.ContinueAction continueAction)
	{
		string headerText = Localization.instance.Localize("$menu_loginheader");
		string loggingInText = Localization.instance.Localize("$menu_logintext");
		string retryText = "";
		int previousRetryCountdown = -1;
		PlayFabManager.instance.SetShouldTryAutoLogin(true);
		UnifiedPopup.Push(new CancelableTaskPopup(() => headerText, delegate()
		{
			if (PlayFabManager.CurrentLoginState == LoginState.WaitingForRetry)
			{
				int num = Mathf.CeilToInt((float)(PlayFabManager.NextRetryUtc - DateTime.UtcNow).TotalSeconds);
				if (previousRetryCountdown != num)
				{
					previousRetryCountdown = num;
					retryText = Localization.instance.Localize("$menu_loginfailedtext") + "\n" + Localization.instance.Localize("$menu_loginretrycountdowntext", new string[]
					{
						num.ToString()
					});
				}
				return retryText;
			}
			return loggingInText;
		}, delegate()
		{
			if (PlayFabManager.IsLoggedIn)
			{
				FejdStartup.ContinueAction continueAction2 = continueAction;
				if (continueAction2 != null)
				{
					continueAction2();
				}
			}
			return PlayFabManager.IsLoggedIn;
		}, delegate()
		{
			UnifiedPopup.Pop();
		}));
	}

	// Token: 0x060011C8 RID: 4552 RVA: 0x00084EA4 File Offset: 0x000830A4
	private OnlineBackendType GetOnlineBackend(bool crossplayServer)
	{
		OnlineBackendType result = OnlineBackendType.Steamworks;
		if (crossplayServer)
		{
			result = OnlineBackendType.PlayFab;
		}
		return result;
	}

	// Token: 0x060011C9 RID: 4553 RVA: 0x00084EBC File Offset: 0x000830BC
	private void ShowCharacterSelection()
	{
		Gogan.LogEvent("Screen", "Enter", "CharacterSelection", 0L);
		ZLog.Log("show character selection");
		this.m_characterSelectScreen.SetActive(true);
		this.m_selectCharacterPanel.SetActive(true);
		this.m_newCharacterPanel.SetActive(false);
		if (this.m_profiles == null)
		{
			this.m_profiles = SaveSystem.GetAllPlayerProfiles();
		}
		if (this.m_profileIndex >= this.m_profiles.Count)
		{
			this.m_profileIndex = this.m_profiles.Count - 1;
		}
		if (this.m_profileIndex >= 0 && this.m_profileIndex < this.m_profiles.Count)
		{
			PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
			this.m_csFileSource.text = Localization.instance.Localize(FileHelpers.GetSourceString(playerProfile.m_fileSource));
		}
	}

	// Token: 0x060011CA RID: 4554 RVA: 0x00084F94 File Offset: 0x00083194
	public void OnJoinStart()
	{
		this.JoinServer();
	}

	// Token: 0x060011CB RID: 4555 RVA: 0x00084F9C File Offset: 0x0008319C
	public void JoinServer()
	{
		if (!PlayFabManager.IsLoggedIn && this.m_joinServer.m_joinData is ServerJoinDataPlayFabUser)
		{
			this.ContinueWhenLoggedInPopup(new FejdStartup.ContinueAction(this.JoinServer));
			return;
		}
		if (PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.OnlineMultiplayer) != PrivilegeResult.Granted)
		{
			ZLog.LogWarning("You should always prevent JoinServer() from being called when user does not have online multiplayer privilege!");
			this.HideAll();
			this.m_mainMenu.SetActive(true);
			this.ShowOnlineMultiplayerPrivilegeWarning();
			return;
		}
		if (this.m_joinServer.OnlineStatus == OnlineStatus.Online && this.m_joinServer.m_networkVersion != 34U)
		{
			UnifiedPopup.Push(new WarningPopup("$error_incompatibleversion", (34U < this.m_joinServer.m_networkVersion) ? "$error_needslocalupdatetojoin" : "$error_needsserverupdatetojoin", delegate()
			{
				UnifiedPopup.Pop();
			}, true));
			return;
		}
		if (this.m_joinServer.PlatformRestriction != null && !this.m_joinServer.IsRestrictedToOwnPlatform)
		{
			if (!this.m_joinServer.IsCrossplay)
			{
				UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$error_failedconnect"), Localization.instance.Localize("$xbox_error_crossplayprivilege"), delegate()
				{
					UnifiedPopup.Pop();
				}, false));
				return;
			}
			if (PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) != PrivilegeResult.Granted)
			{
				if (PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege != null)
				{
					PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.CrossPlatformMultiplayer);
					return;
				}
				UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$error_failedconnect"), Localization.instance.Localize("$xbox_error_crossplayprivilege"), delegate()
				{
					UnifiedPopup.Pop();
				}, false));
				return;
			}
		}
		ZNet.SetServer(false, false, false, "", "", null);
		FejdStartup.retries = 0;
		bool flag = false;
		if (this.m_joinServer.m_joinData is ServerJoinDataSteamUser)
		{
			ZNet.SetServerHost((ulong)(this.m_joinServer.m_joinData as ServerJoinDataSteamUser).m_joinUserID);
			flag = true;
		}
		if (this.m_joinServer.m_joinData is ServerJoinDataPlayFabUser)
		{
			ZNet.SetServerHost((this.m_joinServer.m_joinData as ServerJoinDataPlayFabUser).m_remotePlayerId);
			flag = true;
		}
		if (this.m_joinServer.m_joinData is ServerJoinDataDedicated)
		{
			ServerJoinDataDedicated serverJoin = this.m_joinServer.m_joinData as ServerJoinDataDedicated;
			if (serverJoin.IsValid())
			{
				if (PlayFabManager.IsLoggedIn)
				{
					ZNet.ResetServerHost();
					ZPlayFabMatchmaking.FindHostByIp(serverJoin.GetIPEndPoint().Value, delegate(PlayFabMatchmakingServerData result)
					{
						if (result != null)
						{
							ZNet.SetServerHost(result.remotePlayerId);
							ZLog.Log("Determined backend of dedicated server to be PlayFab");
							return;
						}
						FejdStartup.retries = 50;
					}, delegate(ZPLayFabMatchmakingFailReason failReason)
					{
						ZNet.SetServerHost(serverJoin.GetIPEndPoint().Value.ToString(), (int)serverJoin.m_port, OnlineBackendType.Steamworks);
						ZLog.Log("Determined backend of dedicated server to be Steamworks");
					}, true);
				}
				else
				{
					ZNet.SetServerHost(serverJoin.GetIPEndPoint().Value.ToString(), (int)serverJoin.m_port, OnlineBackendType.Steamworks);
					ZLog.Log("Determined backend of dedicated server to be Steamworks");
				}
				flag = true;
			}
			else
			{
				flag = false;
			}
		}
		if (!flag)
		{
			Debug.LogError("Couldn't set the server host!");
			return;
		}
		Gogan.LogEvent("Menu", "JoinServer", "", 0L);
		FejdStartup.StartGameEventHandler startGameEventHandler = FejdStartup.startGameEvent;
		if (startGameEventHandler != null)
		{
			startGameEventHandler(this, new FejdStartup.StartGameEventArgs(false));
		}
		this.TransitionToMainScene();
	}

	// Token: 0x060011CC RID: 4556 RVA: 0x0008530A File Offset: 0x0008350A
	public void OnStartGameBack()
	{
		this.m_startGamePanel.SetActive(false);
		this.ShowCharacterSelection();
	}

	// Token: 0x060011CD RID: 4557 RVA: 0x00085320 File Offset: 0x00083520
	public void OnCredits()
	{
		this.m_creditsPanel.SetActive(true);
		this.m_mainMenu.SetActive(false);
		Gogan.LogEvent("Screen", "Enter", "Credits", 0L);
		this.m_creditsList.anchoredPosition = new Vector2(0f, 0f);
	}

	// Token: 0x060011CE RID: 4558 RVA: 0x00085375 File Offset: 0x00083575
	public void OnCreditsBack()
	{
		this.m_mainMenu.SetActive(true);
		this.m_creditsPanel.SetActive(false);
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	// Token: 0x060011CF RID: 4559 RVA: 0x000853A5 File Offset: 0x000835A5
	public void OnSelelectCharacterBack()
	{
		this.m_characterSelectScreen.SetActive(false);
		this.m_mainMenu.SetActive(true);
		this.m_queuedJoinServer = null;
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	// Token: 0x060011D0 RID: 4560 RVA: 0x000853DC File Offset: 0x000835DC
	public void OnAbort()
	{
		Application.Quit();
	}

	// Token: 0x060011D1 RID: 4561 RVA: 0x000853E3 File Offset: 0x000835E3
	public void OnWorldVersionYes()
	{
		this.m_worldVersionPanel.SetActive(false);
	}

	// Token: 0x060011D2 RID: 4562 RVA: 0x000853F1 File Offset: 0x000835F1
	public void OnPlayerVersionOk()
	{
		this.m_playerVersionPanel.SetActive(false);
	}

	// Token: 0x060011D3 RID: 4563 RVA: 0x000853FF File Offset: 0x000835FF
	private void FixedUpdate()
	{
		ZInput.FixedUpdate(Time.fixedDeltaTime);
	}

	// Token: 0x060011D4 RID: 4564 RVA: 0x0008540B File Offset: 0x0008360B
	private void UpdateCursor()
	{
		Cursor.lockState = (ZInput.IsMouseActive() ? CursorLockMode.None : CursorLockMode.Locked);
		Cursor.visible = ZInput.IsMouseActive();
	}

	// Token: 0x060011D5 RID: 4565 RVA: 0x00085427 File Offset: 0x00083627
	private void OnLanguageChange()
	{
		this.UpdateCharacterList();
	}

	// Token: 0x060011D6 RID: 4566 RVA: 0x00085430 File Offset: 0x00083630
	private void Update()
	{
		int num = (Settings.FPSLimit != 29) ? Mathf.Min(Settings.FPSLimit, 60) : 60;
		Application.targetFrameRate = ((Settings.ReduceBackgroundUsage && !Application.isFocused) ? Mathf.Min(30, num) : num);
		if (Terminal.m_showTests)
		{
			Terminal.m_testList["fps limit"] = Application.targetFrameRate.ToString();
		}
		ZInput.Update(Time.deltaTime);
		Localization.instance.ReLocalizeVisible(base.transform);
		this.UpdateGamepad();
		this.UpdateKeyboard();
		this.CheckPendingJoinRequest();
		if (MasterClient.instance != null)
		{
			MasterClient.instance.Update(Time.deltaTime);
		}
		if (ZBroastcast.instance != null)
		{
			ZBroastcast.instance.Update(Time.deltaTime);
		}
		this.UpdateCharacterRotation(Time.deltaTime);
		this.UpdateCamera(Time.deltaTime);
		if (this.m_newCharacterPanel.activeInHierarchy)
		{
			this.m_csNewCharacterDone.interactable = (this.m_csNewCharacterName.text.Length >= 3);
			Navigation navigation = this.m_csNewCharacterName.navigation;
			navigation.selectOnDown = (this.m_csNewCharacterDone.interactable ? this.m_csNewCharacterDone : this.m_csNewCharacterCancel);
			this.m_csNewCharacterName.navigation = navigation;
		}
		if (this.m_newCharacterPanel.activeInHierarchy)
		{
			this.m_csNewCharacterDone.interactable = (this.m_csNewCharacterName.text.Length >= 3);
		}
		if (this.m_serverOptionsButton.gameObject.activeInHierarchy)
		{
			this.m_serverOptionsButton.interactable = (this.m_world != null);
		}
		if (this.m_createWorldPanel.activeInHierarchy)
		{
			this.m_newWorldDone.interactable = (this.m_newWorldName.text.Length >= 5);
		}
		if (this.m_startGamePanel.activeInHierarchy)
		{
			this.m_worldStart.interactable = this.CanStartServer();
			this.m_worldRemove.interactable = (this.m_world != null);
			this.UpdatePasswordError();
		}
		if (this.m_startGamePanel.activeInHierarchy)
		{
			bool flag = this.m_openServerToggle.isOn && this.m_openServerToggle.interactable;
			this.SetToggleState(this.m_publicServerToggle, flag);
			this.SetToggleState(this.m_crossplayServerToggle, flag);
			this.m_serverPassword.interactable = flag;
		}
		if (this.m_creditsPanel.activeInHierarchy)
		{
			RectTransform rectTransform = this.m_creditsList.parent as RectTransform;
			Vector3[] array = new Vector3[4];
			this.m_creditsList.GetWorldCorners(array);
			Vector3[] array2 = new Vector3[4];
			rectTransform.GetWorldCorners(array2);
			float num2 = array2[1].y - array2[0].y;
			if ((double)array[3].y < (double)num2 * 0.5)
			{
				Vector3 position = this.m_creditsList.position;
				position.y += Time.deltaTime * this.m_creditsSpeed * num2;
				this.m_creditsList.position = position;
			}
		}
	}

	// Token: 0x060011D7 RID: 4567 RVA: 0x00085729 File Offset: 0x00083929
	private void OnGUI()
	{
		ZInput.OnGUI();
	}

	// Token: 0x060011D8 RID: 4568 RVA: 0x00085730 File Offset: 0x00083930
	private void SetToggleState(Toggle toggle, bool active)
	{
		toggle.interactable = active;
		Color toggleColor = this.m_toggleColor;
		Graphic componentInChildren = toggle.GetComponentInChildren<TMP_Text>();
		if (!active)
		{
			float num = 0.5f;
			float num2 = toggleColor.linear.r * 0.2126f + toggleColor.linear.g * 0.7152f + toggleColor.linear.b * 0.0722f;
			num2 *= num;
			toggleColor.r = (toggleColor.g = (toggleColor.b = Mathf.LinearToGammaSpace(num2)));
		}
		componentInChildren.color = toggleColor;
	}

	// Token: 0x060011D9 RID: 4569 RVA: 0x000857BE File Offset: 0x000839BE
	private void LateUpdate()
	{
		if (ZInput.GetKeyDown(KeyCode.F11, true))
		{
			GameCamera.ScreenShot();
		}
	}

	// Token: 0x060011DA RID: 4570 RVA: 0x000857D4 File Offset: 0x000839D4
	private void UpdateKeyboard()
	{
		if (ZInput.GetKeyDown(KeyCode.Return, true) && this.m_menuList.activeInHierarchy && !this.m_passwordError.gameObject.activeInHierarchy)
		{
			if (this.m_menuSelectedButton != null)
			{
				this.m_menuSelectedButton.OnSubmit(null);
			}
			else
			{
				this.OnStartGame();
			}
		}
		if (this.m_worldListPanel.GetComponent<UIGamePad>().IsBlocked())
		{
			return;
		}
		if (ZInput.GetKeyDown(KeyCode.UpArrow, true))
		{
			if (this.m_worldListPanel.activeInHierarchy)
			{
				this.SetSelectedWorld(this.GetSelectedWorld() - 1, true);
			}
			if (this.m_menuList.activeInHierarchy)
			{
				if (this.m_menuSelectedButton == null)
				{
					this.m_menuSelectedButton = this.m_menuButtons[0];
					this.m_menuSelectedButton.Select();
				}
				else
				{
					for (int i = 1; i < this.m_menuButtons.Length; i++)
					{
						if (this.m_menuButtons[i] == this.m_menuSelectedButton)
						{
							this.m_menuSelectedButton = this.m_menuButtons[i - 1];
							this.m_menuSelectedButton.Select();
							break;
						}
					}
				}
			}
		}
		if (ZInput.GetKeyDown(KeyCode.DownArrow, true))
		{
			if (this.m_worldListPanel.activeInHierarchy)
			{
				this.SetSelectedWorld(this.GetSelectedWorld() + 1, true);
			}
			if (this.m_menuList.activeInHierarchy)
			{
				if (this.m_menuSelectedButton == null)
				{
					this.m_menuSelectedButton = this.m_menuButtons[0];
					this.m_menuSelectedButton.Select();
					return;
				}
				for (int j = 0; j < this.m_menuButtons.Length - 1; j++)
				{
					if (this.m_menuButtons[j] == this.m_menuSelectedButton)
					{
						this.m_menuSelectedButton = this.m_menuButtons[j + 1];
						this.m_menuSelectedButton.Select();
						return;
					}
				}
			}
		}
	}

	// Token: 0x060011DB RID: 4571 RVA: 0x00085990 File Offset: 0x00083B90
	private void UpdateGamepad()
	{
		if (ZInput.IsGamepadActive() && this.m_menuList.activeInHierarchy && EventSystem.current.currentSelectedGameObject == null && this.m_menuButtons != null && this.m_menuButtons.Length != 0)
		{
			base.StartCoroutine(this.SelectFirstMenuEntry(this.m_menuButtons[0]));
		}
		if (!ZInput.IsGamepadActive() || this.m_worldListPanel.GetComponent<UIGamePad>().IsBlocked())
		{
			return;
		}
		if (this.m_worldListPanel.activeInHierarchy)
		{
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.SetSelectedWorld(this.GetSelectedWorld() + 1, true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.SetSelectedWorld(this.GetSelectedWorld() - 1, true);
			}
			if (EventSystem.current.currentSelectedGameObject == null)
			{
				this.RefreshWorldSelection();
			}
		}
		if (this.m_characterSelectScreen.activeInHierarchy && !this.m_newCharacterPanel.activeInHierarchy && this.m_csLeftButton.interactable && ZInput.GetButtonDown("JoyDPadLeft"))
		{
			this.OnCharacterLeft();
		}
		if (this.m_characterSelectScreen.activeInHierarchy && !this.m_newCharacterPanel.activeInHierarchy && this.m_csRightButton.interactable && ZInput.GetButtonDown("JoyDPadRight"))
		{
			this.OnCharacterRight();
		}
		if (this.m_patchLogScroll.gameObject.activeInHierarchy)
		{
			this.m_patchLogScroll.value -= ZInput.GetJoyRightStickY(true) * 0.02f;
		}
	}

	// Token: 0x060011DC RID: 4572 RVA: 0x00085B18 File Offset: 0x00083D18
	private IEnumerator SelectFirstMenuEntry(Button button)
	{
		if (!this.m_menuList.activeInHierarchy)
		{
			yield break;
		}
		if (Event.current != null)
		{
			Event.current.Use();
		}
		yield return null;
		yield return null;
		if (UnifiedPopup.IsVisible())
		{
			UnifiedPopup.SetFocus();
			yield break;
		}
		this.m_menuSelectedButton = button;
		this.m_menuSelectedButton.Select();
		yield break;
	}

	// Token: 0x060011DD RID: 4573 RVA: 0x00085B30 File Offset: 0x00083D30
	public void AcceptInviteMacAppStore(string serverId)
	{
		Action resetPendingInvite = FejdStartup.ResetPendingInvite;
		if (resetPendingInvite != null)
		{
			resetPendingInvite();
		}
		this.m_queuedJoinServer = new ServerJoinDataPlayFabUser(serverId);
		if (this.m_settingsPopup != null)
		{
			UnityEngine.Object.DestroyImmediate(this.m_settingsPopup);
			this.m_settingsPopup = null;
		}
		if (this.m_serverListPanel.activeInHierarchy)
		{
			this.m_joinServer = new ServerStatus(this.m_queuedJoinServer);
			this.m_queuedJoinServer = null;
			this.JoinServer();
			return;
		}
		this.HideAll();
		this.ShowCharacterSelection();
	}

	// Token: 0x060011DE RID: 4574 RVA: 0x00085BB4 File Offset: 0x00083DB4
	private void CheckPendingJoinRequest()
	{
		if (ZSteamMatchmaking.instance == null)
		{
			return;
		}
		ServerJoinData queuedJoinServer;
		if (!ZSteamMatchmaking.instance.GetJoinHost(out queuedJoinServer))
		{
			return;
		}
		if (PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.OnlineMultiplayer) != PrivilegeResult.Granted)
		{
			this.ShowOnlineMultiplayerPrivilegeWarning();
			return;
		}
		this.m_queuedJoinServer = queuedJoinServer;
		if (this.m_serverListPanel.activeInHierarchy)
		{
			this.m_joinServer = new ServerStatus(this.m_queuedJoinServer);
			this.m_queuedJoinServer = null;
			this.JoinServer();
			return;
		}
		this.HideAll();
		this.ShowCharacterSelection();
	}

	// Token: 0x060011DF RID: 4575 RVA: 0x00085C30 File Offset: 0x00083E30
	private void UpdateCharacterRotation(float dt)
	{
		if (this.m_playerInstance == null)
		{
			return;
		}
		if (!this.m_characterSelectScreen.activeInHierarchy)
		{
			return;
		}
		if (ZInput.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			float x = ZInput.GetMouseDelta().x;
			this.m_playerInstance.transform.Rotate(0f, -x * this.m_characterRotateSpeed, 0f);
		}
		float joyRightStickX = ZInput.GetJoyRightStickX(true);
		if (joyRightStickX != 0f)
		{
			this.m_playerInstance.transform.Rotate(0f, -joyRightStickX * this.m_characterRotateSpeedGamepad * dt, 0f);
		}
	}

	// Token: 0x060011E0 RID: 4576 RVA: 0x00085CD0 File Offset: 0x00083ED0
	private void UpdatePasswordError()
	{
		string text = "";
		if (this.NeedPassword())
		{
			text = this.GetPublicPasswordError(this.m_serverPassword.text, this.m_world);
		}
		this.m_passwordError.text = text;
	}

	// Token: 0x060011E1 RID: 4577 RVA: 0x00085D0F File Offset: 0x00083F0F
	private bool NeedPassword()
	{
		return (this.m_publicServerToggle.isOn | this.m_crossplayServerToggle.isOn) & this.m_openServerToggle.isOn;
	}

	// Token: 0x060011E2 RID: 4578 RVA: 0x00085D34 File Offset: 0x00083F34
	private string GetPublicPasswordError(string password, World world)
	{
		if (password.Length < this.m_minimumPasswordLength)
		{
			return Localization.instance.Localize("$menu_passwordshort");
		}
		if (world != null && (world.m_name.Contains(password) || world.m_seedName.Contains(password)))
		{
			return Localization.instance.Localize("$menu_passwordinvalid");
		}
		return "";
	}

	// Token: 0x060011E3 RID: 4579 RVA: 0x00085D93 File Offset: 0x00083F93
	private bool IsPublicPasswordValid(string password, World world)
	{
		return password.Length >= this.m_minimumPasswordLength && !world.m_name.Contains(password) && !world.m_seedName.Contains(password);
	}

	// Token: 0x060011E4 RID: 4580 RVA: 0x00085DC8 File Offset: 0x00083FC8
	private bool CanStartServer()
	{
		if (this.m_world == null)
		{
			return false;
		}
		switch (this.m_world.m_dataError)
		{
		case World.SaveDataError.None:
		case World.SaveDataError.LoadError:
		case World.SaveDataError.Corrupt:
		case World.SaveDataError.MissingMeta:
			return !this.NeedPassword() || this.IsPublicPasswordValid(this.m_serverPassword.text, this.m_world);
		default:
			return false;
		}
	}

	// Token: 0x060011E5 RID: 4581 RVA: 0x00085E2C File Offset: 0x0008402C
	private void UpdateCamera(float dt)
	{
		Transform transform = this.m_cameraMarkerMain;
		if (this.m_characterSelectScreen.activeSelf)
		{
			transform = this.m_cameraMarkerCharacter;
		}
		else if (this.m_creditsPanel.activeSelf)
		{
			transform = this.m_cameraMarkerCredits;
		}
		else if (this.m_startGamePanel.activeSelf)
		{
			transform = this.m_cameraMarkerGame;
		}
		else if (this.m_manageSavesMenu.IsVisible())
		{
			transform = this.m_cameraMarkerSaves;
		}
		this.m_mainCamera.transform.position = Vector3.SmoothDamp(this.m_mainCamera.transform.position, transform.position, ref this.camSpeed, 1.5f, 1000f, dt);
		Vector3 forward = Vector3.SmoothDamp(this.m_mainCamera.transform.forward, transform.forward, ref this.camRotSpeed, 1.5f, 1000f, dt);
		forward.Normalize();
		this.m_mainCamera.transform.rotation = Quaternion.LookRotation(forward);
	}

	// Token: 0x060011E6 RID: 4582 RVA: 0x00085F1C File Offset: 0x0008411C
	public void ShowCloudQuotaWarning()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_cloudstoragefull", "$menu_cloudstoragefulloperationfailed", delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x060011E7 RID: 4583 RVA: 0x00085F54 File Offset: 0x00084154
	public void ShowCloudQuotaWorldDialog()
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_cloudstoragefull", "$menu_cloudstoragefullworldprompt", delegate()
		{
			UnifiedPopup.Pop();
			this.OnNewWorldDone(true);
		}, delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x060011E8 RID: 4584 RVA: 0x00085FA4 File Offset: 0x000841A4
	public void ShowCloudQuotaCharacterDialog()
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_cloudstoragefull", "$menu_cloudstoragefullcharacterprompt", delegate()
		{
			UnifiedPopup.Pop();
			this.OnNewCharacterDone(true);
		}, delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x060011E9 RID: 4585 RVA: 0x00085FF4 File Offset: 0x000841F4
	public void OnManageSaves(int index)
	{
		this.HideAll();
		if (index == 0)
		{
			this.m_manageSavesMenu.Open(SaveDataType.World, (this.m_world != null) ? this.m_world.m_fileName : null, new ManageSavesMenu.ClosedCallback(this.ShowStartGame), new ManageSavesMenu.SavesModifiedCallback(this.OnSavesModified));
			return;
		}
		if (index != 1)
		{
			return;
		}
		this.m_manageSavesMenu.Open(SaveDataType.Character, (this.m_profileIndex >= 0 && this.m_profileIndex < this.m_profiles.Count && this.m_profiles[this.m_profileIndex] != null) ? this.m_profiles[this.m_profileIndex].m_filename : null, new ManageSavesMenu.ClosedCallback(this.ShowCharacterSelection), new ManageSavesMenu.SavesModifiedCallback(this.OnSavesModified));
	}

	// Token: 0x060011EA RID: 4586 RVA: 0x000860B8 File Offset: 0x000842B8
	private void OnSavesModified(SaveDataType dataType)
	{
		if (dataType == SaveDataType.World)
		{
			SaveSystem.ClearWorldListCache(true);
			this.RefreshWorldSelection();
			return;
		}
		if (dataType != SaveDataType.Character)
		{
			return;
		}
		string selectedProfile = null;
		if (this.m_profileIndex < this.m_profiles.Count && this.m_profileIndex >= 0)
		{
			selectedProfile = this.m_profiles[this.m_profileIndex].GetFilename();
		}
		this.m_profiles = SaveSystem.GetAllPlayerProfiles();
		this.SetSelectedProfile(selectedProfile);
		this.m_manageSavesMenu.Open(dataType, new ManageSavesMenu.ClosedCallback(this.ShowCharacterSelection), new ManageSavesMenu.SavesModifiedCallback(this.OnSavesModified));
	}

	// Token: 0x060011EB RID: 4587 RVA: 0x00086148 File Offset: 0x00084348
	private void UpdateCharacterList()
	{
		if (this.m_profiles == null)
		{
			this.m_profiles = SaveSystem.GetAllPlayerProfiles();
		}
		if (this.m_profileIndex >= this.m_profiles.Count)
		{
			this.m_profileIndex = this.m_profiles.Count - 1;
		}
		this.m_csRemoveButton.gameObject.SetActive(this.m_profiles.Count > 0);
		this.m_csStartButton.gameObject.SetActive(this.m_profiles.Count > 0);
		this.m_csNewButton.gameObject.SetActive(this.m_profiles.Count > 0);
		this.m_csNewBigButton.gameObject.SetActive(this.m_profiles.Count == 0);
		this.m_csLeftButton.interactable = (this.m_profileIndex > 0);
		this.m_csRightButton.interactable = (this.m_profileIndex < this.m_profiles.Count - 1);
		if (this.m_profileIndex >= 0 && this.m_profileIndex < this.m_profiles.Count)
		{
			PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
			if (playerProfile.GetName().ToLower() == playerProfile.m_filename.ToLower())
			{
				this.m_csName.text = playerProfile.GetName();
			}
			else
			{
				this.m_csName.text = playerProfile.GetName() + " (" + playerProfile.m_filename + ")";
			}
			this.m_csName.gameObject.SetActive(true);
			this.m_csFileSource.gameObject.SetActive(true);
			this.m_csFileSource.text = Localization.instance.Localize(FileHelpers.GetSourceString(playerProfile.m_fileSource));
			this.m_csSourceInfo.text = Localization.instance.Localize(((playerProfile.m_fileSource == FileHelpers.FileSource.Legacy) ? "$menu_legacynotice \n\n" : "") + ((!FileHelpers.CloudStorageEnabled) ? "$menu_cloudsavesdisabled" : ""));
			Transform transform = this.m_csFileSource.transform.Find("source_cloud");
			if (transform != null)
			{
				transform.gameObject.SetActive(playerProfile.m_fileSource == FileHelpers.FileSource.Cloud);
			}
			Transform transform2 = this.m_csFileSource.transform.Find("source_local");
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(playerProfile.m_fileSource == FileHelpers.FileSource.Local);
			}
			Transform transform3 = this.m_csFileSource.transform.Find("source_legacy");
			if (transform3 != null)
			{
				transform3.gameObject.SetActive(playerProfile.m_fileSource == FileHelpers.FileSource.Legacy);
			}
			this.SetupCharacterPreview(playerProfile);
			return;
		}
		this.m_csName.gameObject.SetActive(false);
		this.m_csFileSource.gameObject.SetActive(false);
		this.ClearCharacterPreview();
	}

	// Token: 0x060011EC RID: 4588 RVA: 0x00086404 File Offset: 0x00084604
	private void SetSelectedProfile(string filename)
	{
		if (this.m_profiles == null)
		{
			this.m_profiles = SaveSystem.GetAllPlayerProfiles();
		}
		this.m_profileIndex = 0;
		if (filename != null)
		{
			for (int i = 0; i < this.m_profiles.Count; i++)
			{
				if (this.m_profiles[i].GetFilename() == filename)
				{
					this.m_profileIndex = i;
					break;
				}
			}
		}
		this.UpdateCharacterList();
	}

	// Token: 0x060011ED RID: 4589 RVA: 0x0008646C File Offset: 0x0008466C
	public void OnNewCharacterDone(bool forceLocal)
	{
		string text = this.m_csNewCharacterName.text;
		string text2 = text.ToLower();
		PlayerProfile playerProfile = new PlayerProfile(text2, FileHelpers.FileSource.Auto);
		if (forceLocal)
		{
			playerProfile.m_fileSource = FileHelpers.FileSource.Local;
		}
		if (playerProfile.m_fileSource == FileHelpers.FileSource.Cloud && FileHelpers.OperationExceedsCloudCapacity(1048576UL * 3UL))
		{
			this.ShowCloudQuotaCharacterDialog();
			ZLog.LogWarning("The character save operation may exceed the cloud save quota and has therefore been aborted! Prompt shown to user.");
			return;
		}
		if (PlayerProfile.HaveProfile(text2))
		{
			this.m_newCharacterError.SetActive(true);
			return;
		}
		Player component = this.m_playerInstance.GetComponent<Player>();
		component.GiveDefaultItems();
		playerProfile.SetName(text);
		playerProfile.SavePlayerData(component);
		playerProfile.Save();
		this.m_selectCharacterPanel.SetActive(true);
		this.m_newCharacterPanel.SetActive(false);
		this.m_profiles = null;
		this.SetSelectedProfile(text2);
		this.m_csNewCharacterName.text = "";
		Gogan.LogEvent("Menu", "NewCharacter", text, 0L);
	}

	// Token: 0x060011EE RID: 4590 RVA: 0x0008654C File Offset: 0x0008474C
	public void OnNewCharacterCancel()
	{
		this.m_selectCharacterPanel.SetActive(true);
		this.m_newCharacterPanel.SetActive(false);
		this.UpdateCharacterList();
	}

	// Token: 0x060011EF RID: 4591 RVA: 0x0008656C File Offset: 0x0008476C
	public void OnCharacterNew()
	{
		this.m_newCharacterPanel.SetActive(true);
		this.m_selectCharacterPanel.SetActive(false);
		this.m_newCharacterError.SetActive(false);
		this.SetupCharacterPreview(null);
		Gogan.LogEvent("Screen", "Enter", "CreateCharacter", 0L);
	}

	// Token: 0x060011F0 RID: 4592 RVA: 0x000865BC File Offset: 0x000847BC
	public void OnCharacterRemove()
	{
		if (this.m_profileIndex < 0 || this.m_profileIndex >= this.m_profiles.Count)
		{
			return;
		}
		PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
		this.m_removeCharacterName.text = playerProfile.GetName() + " (" + Localization.instance.Localize(FileHelpers.GetSourceString(playerProfile.m_fileSource)) + ")";
		this.m_tempRemoveCharacterName = playerProfile.GetFilename();
		this.m_tempRemoveCharacterSource = playerProfile.m_fileSource;
		this.m_tempRemoveCharacterIndex = this.m_profileIndex;
		this.m_removeCharacterDialog.SetActive(true);
	}

	// Token: 0x060011F1 RID: 4593 RVA: 0x0008665D File Offset: 0x0008485D
	public void OnButtonRemoveCharacterYes()
	{
		ZLog.Log("Remove character");
		PlayerProfile.RemoveProfile(this.m_tempRemoveCharacterName, this.m_tempRemoveCharacterSource);
		this.m_profiles.RemoveAt(this.m_tempRemoveCharacterIndex);
		this.UpdateCharacterList();
		this.m_removeCharacterDialog.SetActive(false);
	}

	// Token: 0x060011F2 RID: 4594 RVA: 0x0008669D File Offset: 0x0008489D
	public void OnButtonRemoveCharacterNo()
	{
		this.m_removeCharacterDialog.SetActive(false);
	}

	// Token: 0x060011F3 RID: 4595 RVA: 0x000866AB File Offset: 0x000848AB
	public void OnCharacterLeft()
	{
		if (this.m_profileIndex > 0)
		{
			this.m_profileIndex--;
		}
		this.UpdateCharacterList();
	}

	// Token: 0x060011F4 RID: 4596 RVA: 0x000866CA File Offset: 0x000848CA
	public void OnCharacterRight()
	{
		if (this.m_profileIndex < this.m_profiles.Count - 1)
		{
			this.m_profileIndex++;
		}
		this.UpdateCharacterList();
	}

	// Token: 0x060011F5 RID: 4597 RVA: 0x000866F8 File Offset: 0x000848F8
	public void OnCharacterStart()
	{
		ZLog.Log("OnCharacterStart");
		if (this.m_profileIndex < 0 || this.m_profileIndex >= this.m_profiles.Count)
		{
			return;
		}
		PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
		PlayerPrefs.SetString("profile", playerProfile.GetFilename());
		Game.SetProfile(playerProfile.GetFilename(), playerProfile.m_fileSource);
		this.m_characterSelectScreen.SetActive(false);
		if (this.m_queuedJoinServer != null)
		{
			this.m_joinServer = new ServerStatus(this.m_queuedJoinServer);
			this.m_queuedJoinServer = null;
			this.JoinServer();
			return;
		}
		this.ShowStartGame();
		if (this.m_worlds.Count == 0)
		{
			this.OnWorldNew();
		}
	}

	// Token: 0x060011F6 RID: 4598 RVA: 0x000867B1 File Offset: 0x000849B1
	private void TransitionToMainScene()
	{
		this.m_menuAnimator.SetTrigger("FadeOut");
		base.Invoke("LoadMainSceneIfBackendSelected", 1.5f);
	}

	// Token: 0x060011F7 RID: 4599 RVA: 0x000867D4 File Offset: 0x000849D4
	private void LoadMainSceneIfBackendSelected()
	{
		if (this.m_startingWorld || ZNet.HasServerHost())
		{
			ZLog.Log("Loading main scene");
			this.LoadMainScene();
			return;
		}
		FejdStartup.retries++;
		if (FejdStartup.retries > 50)
		{
			ZLog.Log("Max retries reached, reloading startup scene with connection error");
			ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorConnectFailed);
			this.m_menuAnimator.SetTrigger("FadeIn");
			this.ShowConnectError(ZNet.ConnectionStatus.ErrorConnectFailed);
			return;
		}
		base.Invoke("LoadMainSceneIfBackendSelected", 0.25f);
		ZLog.Log("Backend not retreived yet, checking again in 0.25 seconds...");
	}

	// Token: 0x060011F8 RID: 4600 RVA: 0x00086858 File Offset: 0x00084A58
	private void LoadMainScene()
	{
		this.m_loading.SetActive(true);
		SceneManager.LoadScene(this.m_mainScene, LoadSceneMode.Single);
		this.m_startingWorld = false;
	}

	// Token: 0x060011F9 RID: 4601 RVA: 0x0008687C File Offset: 0x00084A7C
	public void OnButtonSettings()
	{
		this.m_mainMenu.SetActive(false);
		this.m_settingsPopup = UnityEngine.Object.Instantiate<GameObject>(this.m_settingsPrefab, base.transform);
		this.m_settingsPopup.GetComponent<Settings>().SettingsPopupDestroyed += delegate()
		{
			GameObject mainMenu = this.m_mainMenu;
			if (mainMenu == null)
			{
				return;
			}
			mainMenu.SetActive(true);
		};
	}

	// Token: 0x060011FA RID: 4602 RVA: 0x000868C8 File Offset: 0x00084AC8
	public void OnButtonFeedback()
	{
		UnityEngine.Object.Instantiate<GameObject>(this.m_feedbackPrefab, base.transform);
	}

	// Token: 0x060011FB RID: 4603 RVA: 0x000868DC File Offset: 0x00084ADC
	public void OnButtonTwitter()
	{
		Application.OpenURL("https://twitter.com/valheimgame");
	}

	// Token: 0x060011FC RID: 4604 RVA: 0x000868E8 File Offset: 0x00084AE8
	public void OnButtonWebPage()
	{
		Application.OpenURL("http://valheimgame.com/");
	}

	// Token: 0x060011FD RID: 4605 RVA: 0x000868F4 File Offset: 0x00084AF4
	public void OnButtonDiscord()
	{
		Application.OpenURL("https://discord.gg/44qXMJH");
	}

	// Token: 0x060011FE RID: 4606 RVA: 0x00086900 File Offset: 0x00084B00
	public void OnButtonFacebook()
	{
		Application.OpenURL("https://www.facebook.com/valheimgame/");
	}

	// Token: 0x060011FF RID: 4607 RVA: 0x0008690C File Offset: 0x00084B0C
	public void OnButtonShowLog()
	{
		Application.OpenURL(Application.persistentDataPath + "/");
	}

	// Token: 0x06001200 RID: 4608 RVA: 0x00086922 File Offset: 0x00084B22
	private bool AcceptedNDA()
	{
		return PlayerPrefs.GetInt("accepted_nda", 0) == 1;
	}

	// Token: 0x06001201 RID: 4609 RVA: 0x00086932 File Offset: 0x00084B32
	public void OnButtonNDAAccept()
	{
		PlayerPrefs.SetInt("accepted_nda", 1);
		this.m_ndaPanel.SetActive(false);
		this.m_mainMenu.SetActive(true);
	}

	// Token: 0x06001202 RID: 4610 RVA: 0x00086957 File Offset: 0x00084B57
	public void OnButtonNDADecline()
	{
		Application.Quit();
	}

	// Token: 0x06001203 RID: 4611 RVA: 0x0008695E File Offset: 0x00084B5E
	public void OnConnectionFailedOk()
	{
		this.m_connectionFailedPanel.SetActive(false);
	}

	// Token: 0x06001204 RID: 4612 RVA: 0x0008696C File Offset: 0x00084B6C
	public Player GetPreviewPlayer()
	{
		if (this.m_playerInstance != null)
		{
			return this.m_playerInstance.GetComponent<Player>();
		}
		return null;
	}

	// Token: 0x06001205 RID: 4613 RVA: 0x0008698C File Offset: 0x00084B8C
	private void ClearCharacterPreview()
	{
		if (this.m_playerInstance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_changeEffectPrefab, this.m_characterPreviewPoint.position, this.m_characterPreviewPoint.rotation);
			UnityEngine.Object.Destroy(this.m_playerInstance);
			this.m_playerInstance = null;
		}
	}

	// Token: 0x06001206 RID: 4614 RVA: 0x000869DC File Offset: 0x00084BDC
	private void SetupCharacterPreview(PlayerProfile profile)
	{
		this.ClearCharacterPreview();
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_playerPrefab, this.m_characterPreviewPoint.position, this.m_characterPreviewPoint.rotation);
		ZNetView.m_forceDisableInit = false;
		UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
		Animator[] componentsInChildren = gameObject.GetComponentsInChildren<Animator>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].updateMode = AnimatorUpdateMode.Normal;
		}
		Player component = gameObject.GetComponent<Player>();
		if (profile != null)
		{
			try
			{
				profile.LoadPlayerData(component);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Error loading player data: " + profile.GetPath() + ", error: " + ex.Message);
			}
		}
		this.m_playerInstance = gameObject;
	}

	// Token: 0x06001207 RID: 4615 RVA: 0x00086A98 File Offset: 0x00084C98
	public void SetServerToJoin(ServerStatus serverData)
	{
		this.m_joinServer = serverData;
	}

	// Token: 0x06001208 RID: 4616 RVA: 0x00086AA1 File Offset: 0x00084CA1
	public bool HasServerToJoin()
	{
		return this.m_joinServer != null;
	}

	// Token: 0x06001209 RID: 4617 RVA: 0x00086AAC File Offset: 0x00084CAC
	public ServerJoinData GetServerToJoin()
	{
		if (this.m_joinServer == null)
		{
			return null;
		}
		return this.m_joinServer.m_joinData;
	}

	// Token: 0x1400000C RID: 12
	// (add) Token: 0x0600120A RID: 4618 RVA: 0x00086AC4 File Offset: 0x00084CC4
	// (remove) Token: 0x0600120B RID: 4619 RVA: 0x00086AF8 File Offset: 0x00084CF8
	public static event FejdStartup.StartGameEventHandler startGameEvent;

	// Token: 0x1700009C RID: 156
	// (get) Token: 0x0600120C RID: 4620 RVA: 0x00086B2B File Offset: 0x00084D2B
	// (set) Token: 0x0600120D RID: 4621 RVA: 0x00086B32 File Offset: 0x00084D32
	public static string InstanceId { get; private set; } = null;

	// Token: 0x06001212 RID: 4626 RVA: 0x00086C3C File Offset: 0x00084E3C
	[CompilerGenerated]
	private void <OnWorldStart>g__RestoreMetaFromBackupPrompt|50_0(SaveWithBackups saveToRestore)
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_restorebackup", "$menu_missingmetarestore", delegate()
		{
			UnifiedPopup.Pop();
			SaveSystem.RestoreBackupResult restoreBackupResult = SaveSystem.RestoreMetaFromMostRecentBackup(saveToRestore.PrimaryFile);
			switch (restoreBackupResult)
			{
			case SaveSystem.RestoreBackupResult.Success:
				this.RefreshWorldSelection();
				return;
			case SaveSystem.RestoreBackupResult.NoBackup:
				UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$error_nosuitablebackupfound", new PopupButtonCallback(UnifiedPopup.Pop), true));
				return;
			}
			UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
			ZLog.LogError(string.Format("Failed to restore meta file! Result: {0}", restoreBackupResult));
		}, new PopupButtonCallback(UnifiedPopup.Pop), true));
	}

	// Token: 0x06001213 RID: 4627 RVA: 0x00086C8C File Offset: 0x00084E8C
	[CompilerGenerated]
	private void <OnWorldStart>g__RestoreBackupPrompt|50_1(SaveWithBackups saveToRestore)
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_restorebackup", "$menu_corruptsaverestore", delegate()
		{
			UnifiedPopup.Pop();
			SaveSystem.RestoreBackupResult restoreBackupResult = SaveSystem.RestoreMostRecentBackup(saveToRestore);
			switch (restoreBackupResult)
			{
			case SaveSystem.RestoreBackupResult.Success:
				SaveSystem.ClearWorldListCache(true);
				this.RefreshWorldSelection();
				return;
			case SaveSystem.RestoreBackupResult.NoBackup:
				UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$error_nosuitablebackupfound", new PopupButtonCallback(UnifiedPopup.Pop), true));
				return;
			}
			UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
			ZLog.LogError(string.Format("Failed to restore backup! Result: {0}", restoreBackupResult));
		}, new PopupButtonCallback(UnifiedPopup.Pop), true));
	}

	// Token: 0x040010F0 RID: 4336
	private Vector3 camSpeed = Vector3.zero;

	// Token: 0x040010F1 RID: 4337
	private Vector3 camRotSpeed = Vector3.zero;

	// Token: 0x040010F2 RID: 4338
	private const int maxRetries = 50;

	// Token: 0x040010F3 RID: 4339
	private static int retries = 0;

	// Token: 0x040010F4 RID: 4340
	private static FejdStartup m_instance;

	// Token: 0x040010F5 RID: 4341
	[Header("Start")]
	public Animator m_menuAnimator;

	// Token: 0x040010F6 RID: 4342
	public GameObject m_worldVersionPanel;

	// Token: 0x040010F7 RID: 4343
	public GameObject m_playerVersionPanel;

	// Token: 0x040010F8 RID: 4344
	public GameObject m_newGameVersionPanel;

	// Token: 0x040010F9 RID: 4345
	public GameObject m_connectionFailedPanel;

	// Token: 0x040010FA RID: 4346
	public TMP_Text m_connectionFailedError;

	// Token: 0x040010FB RID: 4347
	public TMP_Text m_newVersionName;

	// Token: 0x040010FC RID: 4348
	public GameObject m_loading;

	// Token: 0x040010FD RID: 4349
	public GameObject m_pleaseWait;

	// Token: 0x040010FE RID: 4350
	public TMP_Text m_versionLabel;

	// Token: 0x040010FF RID: 4351
	public GameObject m_mainMenu;

	// Token: 0x04001100 RID: 4352
	public GameObject m_ndaPanel;

	// Token: 0x04001101 RID: 4353
	public GameObject m_betaText;

	// Token: 0x04001102 RID: 4354
	public GameObject m_moddedText;

	// Token: 0x04001103 RID: 4355
	public Scrollbar m_patchLogScroll;

	// Token: 0x04001104 RID: 4356
	public GameObject m_characterSelectScreen;

	// Token: 0x04001105 RID: 4357
	public GameObject m_selectCharacterPanel;

	// Token: 0x04001106 RID: 4358
	public GameObject m_newCharacterPanel;

	// Token: 0x04001107 RID: 4359
	public GameObject m_creditsPanel;

	// Token: 0x04001108 RID: 4360
	public GameObject m_startGamePanel;

	// Token: 0x04001109 RID: 4361
	public GameObject m_createWorldPanel;

	// Token: 0x0400110A RID: 4362
	public ServerOptionsGUI m_serverOptions;

	// Token: 0x0400110B RID: 4363
	public Button m_serverOptionsButton;

	// Token: 0x0400110C RID: 4364
	public GameObject m_menuList;

	// Token: 0x0400110D RID: 4365
	private Button[] m_menuButtons;

	// Token: 0x0400110E RID: 4366
	private Button m_menuSelectedButton;

	// Token: 0x0400110F RID: 4367
	public RectTransform m_creditsList;

	// Token: 0x04001110 RID: 4368
	public float m_creditsSpeed = 100f;

	// Token: 0x04001111 RID: 4369
	public SceneReference m_startScene;

	// Token: 0x04001112 RID: 4370
	public SceneReference m_mainScene;

	// Token: 0x04001113 RID: 4371
	[Header("Camera")]
	public GameObject m_mainCamera;

	// Token: 0x04001114 RID: 4372
	public Transform m_cameraMarkerStart;

	// Token: 0x04001115 RID: 4373
	public Transform m_cameraMarkerMain;

	// Token: 0x04001116 RID: 4374
	public Transform m_cameraMarkerCharacter;

	// Token: 0x04001117 RID: 4375
	public Transform m_cameraMarkerCredits;

	// Token: 0x04001118 RID: 4376
	public Transform m_cameraMarkerGame;

	// Token: 0x04001119 RID: 4377
	public Transform m_cameraMarkerSaves;

	// Token: 0x0400111A RID: 4378
	public float m_cameraMoveSpeed = 1.5f;

	// Token: 0x0400111B RID: 4379
	public float m_cameraMoveSpeedStart = 1.5f;

	// Token: 0x0400111C RID: 4380
	[Header("Join")]
	public GameObject m_serverListPanel;

	// Token: 0x0400111D RID: 4381
	public Toggle m_publicServerToggle;

	// Token: 0x0400111E RID: 4382
	public Toggle m_openServerToggle;

	// Token: 0x0400111F RID: 4383
	public Toggle m_crossplayServerToggle;

	// Token: 0x04001120 RID: 4384
	public Color m_toggleColor = new Color(1f, 0.6308316f, 0.2352941f);

	// Token: 0x04001121 RID: 4385
	public GuiInputField m_serverPassword;

	// Token: 0x04001122 RID: 4386
	public TMP_Text m_passwordError;

	// Token: 0x04001123 RID: 4387
	public int m_minimumPasswordLength = 5;

	// Token: 0x04001124 RID: 4388
	public float m_characterRotateSpeed = 4f;

	// Token: 0x04001125 RID: 4389
	public float m_characterRotateSpeedGamepad = 200f;

	// Token: 0x04001126 RID: 4390
	public int m_joinHostPort = 2456;

	// Token: 0x04001127 RID: 4391
	[Header("World")]
	public GameObject m_worldListPanel;

	// Token: 0x04001128 RID: 4392
	public RectTransform m_worldListRoot;

	// Token: 0x04001129 RID: 4393
	public GameObject m_worldListElement;

	// Token: 0x0400112A RID: 4394
	public ScrollRectEnsureVisible m_worldListEnsureVisible;

	// Token: 0x0400112B RID: 4395
	public float m_worldListElementStep = 28f;

	// Token: 0x0400112C RID: 4396
	public TextMeshProUGUI m_worldSourceInfo;

	// Token: 0x0400112D RID: 4397
	public GameObject m_worldSourceInfoPanel;

	// Token: 0x0400112E RID: 4398
	public GuiInputField m_newWorldName;

	// Token: 0x0400112F RID: 4399
	public GuiInputField m_newWorldSeed;

	// Token: 0x04001130 RID: 4400
	public Button m_newWorldDone;

	// Token: 0x04001131 RID: 4401
	public Button m_worldStart;

	// Token: 0x04001132 RID: 4402
	public Button m_worldRemove;

	// Token: 0x04001133 RID: 4403
	public GameObject m_removeWorldDialog;

	// Token: 0x04001134 RID: 4404
	public TMP_Text m_removeWorldName;

	// Token: 0x04001135 RID: 4405
	public GameObject m_removeCharacterDialog;

	// Token: 0x04001136 RID: 4406
	public TMP_Text m_removeCharacterName;

	// Token: 0x04001137 RID: 4407
	public RectTransform m_tooltipAnchor;

	// Token: 0x04001138 RID: 4408
	public RectTransform m_tooltipSecondaryAnchor;

	// Token: 0x04001139 RID: 4409
	[Header("Character selection")]
	public Button m_csStartButton;

	// Token: 0x0400113A RID: 4410
	public Button m_csNewBigButton;

	// Token: 0x0400113B RID: 4411
	public Button m_csNewButton;

	// Token: 0x0400113C RID: 4412
	public Button m_csRemoveButton;

	// Token: 0x0400113D RID: 4413
	public Button m_csLeftButton;

	// Token: 0x0400113E RID: 4414
	public Button m_csRightButton;

	// Token: 0x0400113F RID: 4415
	public Button m_csNewCharacterDone;

	// Token: 0x04001140 RID: 4416
	public Button m_csNewCharacterCancel;

	// Token: 0x04001141 RID: 4417
	public GameObject m_newCharacterError;

	// Token: 0x04001142 RID: 4418
	public TMP_Text m_csName;

	// Token: 0x04001143 RID: 4419
	public TMP_Text m_csFileSource;

	// Token: 0x04001144 RID: 4420
	public TMP_Text m_csSourceInfo;

	// Token: 0x04001145 RID: 4421
	public GuiInputField m_csNewCharacterName;

	// Token: 0x04001146 RID: 4422
	[Header("Misc")]
	public Transform m_characterPreviewPoint;

	// Token: 0x04001147 RID: 4423
	public GameObject m_playerPrefab;

	// Token: 0x04001148 RID: 4424
	public GameObject m_objectDBPrefab;

	// Token: 0x04001149 RID: 4425
	public GameObject m_settingsPrefab;

	// Token: 0x0400114A RID: 4426
	public GameObject m_consolePrefab;

	// Token: 0x0400114B RID: 4427
	public GameObject m_feedbackPrefab;

	// Token: 0x0400114C RID: 4428
	public GameObject m_changeEffectPrefab;

	// Token: 0x0400114D RID: 4429
	public ManageSavesMenu m_manageSavesMenu;

	// Token: 0x0400114E RID: 4430
	public GameObject m_cloudStorageWarningNextSave;

	// Token: 0x0400114F RID: 4431
	private GameObject m_settingsPopup;

	// Token: 0x04001150 RID: 4432
	private string m_downloadUrl = "";

	// Token: 0x04001151 RID: 4433
	[TextArea]
	public string m_versionXmlUrl = "https://dl.dropboxusercontent.com/s/5ibm05oelbqt8zq/fejdversion.xml?dl=0";

	// Token: 0x04001152 RID: 4434
	private World m_world;

	// Token: 0x04001153 RID: 4435
	private bool m_startingWorld;

	// Token: 0x04001154 RID: 4436
	private ServerStatus m_joinServer;

	// Token: 0x04001155 RID: 4437
	private ServerJoinData m_queuedJoinServer;

	// Token: 0x04001156 RID: 4438
	private float m_worldListBaseSize;

	// Token: 0x04001157 RID: 4439
	private List<PlayerProfile> m_profiles;

	// Token: 0x04001158 RID: 4440
	private int m_profileIndex;

	// Token: 0x04001159 RID: 4441
	private string m_tempRemoveCharacterName = "";

	// Token: 0x0400115A RID: 4442
	private FileHelpers.FileSource m_tempRemoveCharacterSource;

	// Token: 0x0400115B RID: 4443
	private int m_tempRemoveCharacterIndex = -1;

	// Token: 0x0400115C RID: 4444
	private BackgroundWorker m_moveFileWorker;

	// Token: 0x0400115D RID: 4445
	private List<GameObject> m_worldListElements = new List<GameObject>();

	// Token: 0x0400115E RID: 4446
	private List<World> m_worlds;

	// Token: 0x0400115F RID: 4447
	private GameObject m_playerInstance;

	// Token: 0x04001160 RID: 4448
	private static bool m_firstStartup = true;

	// Token: 0x04001163 RID: 4451
	public static Action HandlePendingInvite;

	// Token: 0x04001164 RID: 4452
	public static Action ResetPendingInvite;

	// Token: 0x04001165 RID: 4453
	public static Action<Privilege> ResolvePrivilege;

	// Token: 0x04001166 RID: 4454
	private static GameObject s_monoUpdaters = null;

	// Token: 0x02000310 RID: 784
	// (Invoke) Token: 0x060021FB RID: 8699
	private delegate void ContinueAction();

	// Token: 0x02000311 RID: 785
	public struct StartGameEventArgs
	{
		// Token: 0x060021FE RID: 8702 RVA: 0x000EC568 File Offset: 0x000EA768
		public StartGameEventArgs(bool isHost)
		{
			this.isHost = isHost;
		}

		// Token: 0x040023C0 RID: 9152
		public bool isHost;
	}

	// Token: 0x02000312 RID: 786
	// (Invoke) Token: 0x06002200 RID: 8704
	public delegate void StartGameEventHandler(object sender, FejdStartup.StartGameEventArgs e);
}
