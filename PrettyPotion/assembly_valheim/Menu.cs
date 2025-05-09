using System;
using System.Collections;
using System.Collections.Generic;
using SoftReferenceableAssets.SceneManagement;
using Splatform;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valheim.SettingsGui;

// Token: 0x02000085 RID: 133
public class Menu : MonoBehaviour
{
	// Token: 0x1700003A RID: 58
	// (get) Token: 0x060008C2 RID: 2242 RVA: 0x0004C71C File Offset: 0x0004A91C
	public static Menu instance
	{
		get
		{
			return Menu.m_instance;
		}
	}

	// Token: 0x060008C3 RID: 2243 RVA: 0x0004C724 File Offset: 0x0004A924
	private void Start()
	{
		Menu.m_instance = this;
		this.Hide();
		this.UpdateNavigation();
		this.m_rebuildLayout = true;
		if (ZNet.GetWorldIfIsHost() == null)
		{
			PlayerProfile.SavingFinished = (Action)Delegate.Combine(PlayerProfile.SavingFinished, new Action(this.SaveFinished));
			return;
		}
		ZNet.WorldSaveFinished = (Action)Delegate.Combine(ZNet.WorldSaveFinished, new Action(this.SaveFinished));
	}

	// Token: 0x060008C4 RID: 2244 RVA: 0x0004C794 File Offset: 0x0004A994
	private void HandleInputLayoutChanged()
	{
		this.UpdateCursor();
		if (!ZInput.IsGamepadActive())
		{
			this.m_gamepadRoot.gameObject.SetActive(false);
			return;
		}
		this.m_gamepadRoot.gameObject.SetActive(true);
		this.m_gamepadMapController.Show(ZInput.InputLayout, GamepadMapController.GetType(ZInput.CurrentGlyph, Settings.IsSteamRunningOnSteamDeck()));
	}

	// Token: 0x060008C5 RID: 2245 RVA: 0x0004C7F0 File Offset: 0x0004A9F0
	private void UpdateNavigation()
	{
		Button component = this.m_menuDialog.Find("MenuEntries/Logout").GetComponent<Button>();
		Button component2 = this.m_menuDialog.Find("MenuEntries/Exit").GetComponent<Button>();
		Button component3 = this.m_menuDialog.Find("MenuEntries/Continue").GetComponent<Button>();
		Button component4 = this.m_menuDialog.Find("MenuEntries/Settings").GetComponent<Button>();
		Button component5 = this.m_menuDialog.Find("MenuEntries/SkipIntro").GetComponent<Button>();
		this.m_firstMenuButton = component3;
		List<Button> list = new List<Button>();
		list.Add(component3);
		if (component5.gameObject.activeSelf)
		{
			list.Add(component5);
		}
		if (this.saveButton.interactable)
		{
			list.Add(this.saveButton);
		}
		if (this.menuCurrentPlayersListButton.gameObject.activeSelf)
		{
			list.Add(this.menuCurrentPlayersListButton);
		}
		if (this.menuInviteFriendsButton.gameObject.activeSelf)
		{
			list.Add(this.menuInviteFriendsButton);
		}
		list.Add(component4);
		list.Add(component);
		if (component2.gameObject.activeSelf)
		{
			list.Add(component2);
		}
		for (int i = 0; i < list.Count; i++)
		{
			Navigation navigation = list[i].navigation;
			if (i > 0)
			{
				navigation.selectOnUp = list[i - 1];
			}
			else
			{
				navigation.selectOnUp = list[list.Count - 1];
			}
			if (i < list.Count - 1)
			{
				navigation.selectOnDown = list[i + 1];
			}
			else
			{
				navigation.selectOnDown = list[0];
			}
			navigation.mode = Navigation.Mode.Explicit;
			list[i].navigation = navigation;
		}
	}

	// Token: 0x060008C6 RID: 2246 RVA: 0x0004C9BC File Offset: 0x0004ABBC
	private void OnDestroy()
	{
		PlayerProfile.SavingFinished = (Action)Delegate.Remove(PlayerProfile.SavingFinished, new Action(this.SaveFinished));
		ZNet.WorldSaveFinished = (Action)Delegate.Remove(ZNet.WorldSaveFinished, new Action(this.SaveFinished));
		ZInput.OnInputLayoutChanged -= this.HandleInputLayoutChanged;
	}

	// Token: 0x060008C7 RID: 2247 RVA: 0x0004CA1C File Offset: 0x0004AC1C
	private void SaveFinished()
	{
		this.m_lastSavedDate = DateTime.Now;
		this.m_rebuildLayout = true;
		if (ZNet.instance != null && !ZNet.instance.IsSaving() && (!Menu.CanSaveToCloudStorage() || Menu.ExceedCloudStorageTest))
		{
			this.ShowCloudStorageLowNextSaveWarning();
		}
	}

	// Token: 0x060008C8 RID: 2248 RVA: 0x0004CA68 File Offset: 0x0004AC68
	private static bool CanSaveToCloudStorage()
	{
		return SaveSystem.CanSaveToCloudStorage(ZNet.GetWorldIfIsHost(), Game.instance.GetPlayerProfile());
	}

	// Token: 0x060008C9 RID: 2249 RVA: 0x0004CA80 File Offset: 0x0004AC80
	public void Show()
	{
		Gogan.LogEvent("Screen", "Enter", "Menu", 0L);
		this.m_root.gameObject.SetActive(true);
		this.m_menuDialog.gameObject.SetActive(true);
		this.m_skipButton.gameObject.SetActive(Game.instance.InIntro(true) || (Player.m_localPlayer && Player.m_localPlayer.InIntro()));
		this.m_logoutDialog.gameObject.SetActive(false);
		this.m_quitDialog.gameObject.SetActive(false);
		bool active = !ZNet.IsSinglePlayer && ZNet.instance.IsServer() && PlatformManager.DistributionPlatform.UIProvider.InviteUsers != null && PlatformManager.DistributionPlatform.UIProvider.InviteUsers.CanInviteUsers;
		this.menuCurrentPlayersListButton.gameObject.SetActive(!ZNet.IsSinglePlayer);
		this.menuInviteFriendsButton.gameObject.SetActive(active);
		this.saveButton.gameObject.SetActive(true);
		this.lastSaveText.gameObject.SetActive(this.m_lastSavedDate > DateTime.MinValue);
		if (Player.m_localPlayer != null)
		{
			Game.Pause();
		}
		if (Chat.instance.IsChatDialogWindowVisible())
		{
			Chat.instance.Hide();
		}
		JoinCode.Show(false);
		this.UpdateNavigation();
		this.m_rebuildLayout = true;
		this.m_saveOnLogout = true;
		this.m_loadStartSceneOnLogout = true;
		ZInput.WorkaroundEnabled = false;
		ZInput.OnInputLayoutChanged -= this.HandleInputLayoutChanged;
		ZInput.OnInputLayoutChanged += this.HandleInputLayoutChanged;
		this.HandleInputLayoutChanged();
	}

	// Token: 0x060008CA RID: 2250 RVA: 0x0004CC2D File Offset: 0x0004AE2D
	private IEnumerator SelectEntry(GameObject entry)
	{
		yield return null;
		yield return null;
		EventSystem.current.SetSelectedGameObject(entry);
		this.UpdateCursor();
		yield break;
	}

	// Token: 0x060008CB RID: 2251 RVA: 0x0004CC44 File Offset: 0x0004AE44
	public void Hide()
	{
		this.m_root.gameObject.SetActive(false);
		JoinCode.Hide();
		Game.Unpause();
		if (ZInput.IsGamepadActive())
		{
			PlayerController.SetTakeInputDelay(0.1f);
		}
		ZInput.OnInputLayoutChanged -= this.UpdateCursor;
		ZInput.WorkaroundEnabled = true;
	}

	// Token: 0x060008CC RID: 2252 RVA: 0x0004CC94 File Offset: 0x0004AE94
	private void UpdateCursor()
	{
		Cursor.lockState = (ZInput.IsMouseActive() ? CursorLockMode.None : CursorLockMode.Locked);
		Cursor.visible = ZInput.IsMouseActive();
	}

	// Token: 0x060008CD RID: 2253 RVA: 0x0004CCB0 File Offset: 0x0004AEB0
	public static bool IsVisible()
	{
		return !(Menu.m_instance == null) && (Menu.m_instance.m_hiddenFrames <= 2 || UnifiedPopup.WasVisibleThisFrame());
	}

	// Token: 0x060008CE RID: 2254 RVA: 0x0004CCD5 File Offset: 0x0004AED5
	public static bool IsActive()
	{
		return !(Menu.m_instance == null) && (Menu.m_instance.m_root.gameObject.activeSelf || UnifiedPopup.WasVisibleThisFrame());
	}

	// Token: 0x060008CF RID: 2255 RVA: 0x0004CD04 File Offset: 0x0004AF04
	private void Update()
	{
		if (Game.instance.IsShuttingDown())
		{
			this.Hide();
			return;
		}
		if (this.m_root.gameObject.activeSelf)
		{
			this.m_hiddenFrames = 0;
			if ((ZInput.GetKeyDown(KeyCode.Escape, true) || (ZInput.GetButtonDown("JoyMenu") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")) && !ZInput.GetButton("JoyAltKeys")) || ZInput.GetButtonDown("JoyButtonB")) && !this.m_settingsInstance && !this.m_currentPlayersInstance && !Feedback.IsVisible() && !UnifiedPopup.IsVisible())
			{
				if (this.m_quitDialog.gameObject.activeSelf)
				{
					this.OnQuitNo();
				}
				else if (this.m_logoutDialog.gameObject.activeSelf)
				{
					this.OnLogoutNo();
				}
				else
				{
					if (this.m_closeMenuState == Menu.CloseMenuState.SettingsOpen && ZInput.GetButtonDown("JoyButtonB"))
					{
						this.m_closeMenuState = Menu.CloseMenuState.Blocked;
					}
					if (this.m_closeMenuState != Menu.CloseMenuState.Blocked)
					{
						this.Hide();
					}
				}
			}
			if (this.m_closeMenuState == Menu.CloseMenuState.Blocked && ZInput.GetButtonUp("JoyButtonB"))
			{
				this.m_closeMenuState = Menu.CloseMenuState.CanBeClosed;
			}
			if (ZInput.IsGamepadActive() && base.gameObject.activeInHierarchy && EventSystem.current.currentSelectedGameObject == null && this.m_firstMenuButton != null)
			{
				base.StartCoroutine(this.SelectEntry(this.m_firstMenuButton.gameObject));
			}
			if (this.m_lastSavedDate > DateTime.MinValue)
			{
				int minutes = (DateTime.Now - this.m_lastSavedDate).Minutes;
				string text = minutes.ToString();
				if (minutes < 1)
				{
					text = "<1";
				}
				this.lastSaveText.text = Localization.instance.Localize("$menu_manualsavetime", new string[]
				{
					text
				});
			}
			if ((this.saveButton.interactable && (float)this.m_manualSaveCooldownUntil > Time.unscaledTime) || (!this.saveButton.interactable && (float)this.m_manualSaveCooldownUntil < Time.unscaledTime))
			{
				this.saveButton.interactable = ((float)this.m_manualSaveCooldownUntil < Time.unscaledTime);
				this.UpdateNavigation();
			}
			if (this.m_rebuildLayout)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(this.menuEntriesParent);
				this.lastSaveText.gameObject.SetActive(this.m_lastSavedDate > DateTime.MinValue);
				this.m_rebuildLayout = false;
				base.StartCoroutine(this.SelectEntry(this.m_firstMenuButton.gameObject));
			}
		}
		else
		{
			this.m_hiddenFrames++;
			bool flag = !InventoryGui.IsVisible() && !Minimap.IsOpen() && !global::Console.IsVisible() && !TextInput.IsVisible() && !ZNet.instance.InPasswordDialog() && !ZNet.instance.InConnectingScreen() && !StoreGui.IsVisible() && !Hud.IsPieceSelectionVisible() && !UnifiedPopup.IsVisible() && !PlayerCustomizaton.IsBarberGuiVisible() && !Hud.InRadial();
			if ((ZInput.GetKeyDown(KeyCode.Escape, true) || (ZInput.GetButtonDown("JoyMenu") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")) && !ZInput.GetButton("JoyAltKeys"))) && flag && !Chat.instance.m_wasFocused)
			{
				this.Show();
			}
		}
		if (this.m_updateLocalizationTimer > 30)
		{
			Localization.instance.ReLocalizeVisible(base.transform);
			this.m_updateLocalizationTimer = 0;
			return;
		}
		this.m_updateLocalizationTimer++;
	}

	// Token: 0x060008D0 RID: 2256 RVA: 0x0004D078 File Offset: 0x0004B278
	public void OnSkip()
	{
		Game.instance.SkipIntro();
		this.Hide();
	}

	// Token: 0x060008D1 RID: 2257 RVA: 0x0004D08A File Offset: 0x0004B28A
	public void OnSettings()
	{
		Gogan.LogEvent("Screen", "Enter", "Settings", 0L);
		this.m_settingsInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_settingsPrefab, base.transform);
		this.m_closeMenuState = Menu.CloseMenuState.SettingsOpen;
	}

	// Token: 0x060008D2 RID: 2258 RVA: 0x0004D0C0 File Offset: 0x0004B2C0
	public void OnQuit()
	{
		this.m_quitDialog.gameObject.SetActive(true);
		this.m_menuDialog.gameObject.SetActive(false);
	}

	// Token: 0x060008D3 RID: 2259 RVA: 0x0004D0E4 File Offset: 0x0004B2E4
	public void OnCurrentPlayers()
	{
		if (this.m_currentPlayersInstance == null)
		{
			this.m_currentPlayersInstance = UnityEngine.Object.Instantiate<GameObject>(this.CurrentPlayersPrefab, base.transform);
			return;
		}
		this.m_currentPlayersInstance.SetActive(true);
	}

	// Token: 0x060008D4 RID: 2260 RVA: 0x0004D118 File Offset: 0x0004B318
	public void InviteFriends()
	{
		InviteUsersUI inviteUsers = PlatformManager.DistributionPlatform.UIProvider.InviteUsers;
		if (inviteUsers == null || !inviteUsers.CanInviteUsers)
		{
			return;
		}
		inviteUsers.Open();
	}

	// Token: 0x060008D5 RID: 2261 RVA: 0x0004D148 File Offset: 0x0004B348
	public void OnManualSave()
	{
		if ((float)this.m_manualSaveCooldownUntil >= Time.unscaledTime)
		{
			return;
		}
		if (!Menu.CanSaveToCloudStorage())
		{
			this.m_logoutDialog.gameObject.SetActive(false);
			this.ShowCloudStorageFullWarning(new Menu.CloudStorageFullOkCallback(this.Logout));
			return;
		}
		if (ZNet.instance != null)
		{
			if (ZNet.IsSinglePlayer || ZNet.instance.GetPeerConnections() < 1)
			{
				bool flag;
				if (!ZNet.instance.EnoughDiskSpaceAvailable(out flag, false, null))
				{
					return;
				}
				Game.instance.SavePlayerProfile(true);
				ZNet.instance.Save(true, false, true);
			}
			else
			{
				ZNet.instance.SaveWorldAndPlayerProfiles();
			}
			this.m_manualSaveCooldownUntil = (int)Time.unscaledTime + 60;
			this.m_saveOnLogout = (ZNet.instance != null && ZNet.instance.IsServer());
		}
	}

	// Token: 0x060008D6 RID: 2262 RVA: 0x0004D214 File Offset: 0x0004B414
	public void OnQuitYes()
	{
		bool flag;
		ZNet.instance.EnoughDiskSpaceAvailable(out flag, true, delegate(bool exit)
		{
			if (exit)
			{
				this.QuitGame();
			}
		});
		if (!flag)
		{
			if (!Menu.CanSaveToCloudStorage())
			{
				this.m_quitDialog.gameObject.SetActive(false);
				if (!FileHelpers.LocalStorageSupported)
				{
					this.m_saveOnLogout = false;
				}
				this.ShowCloudStorageFullWarning(new Menu.CloudStorageFullOkCallback(this.QuitGame));
				return;
			}
			this.QuitGame();
		}
	}

	// Token: 0x060008D7 RID: 2263 RVA: 0x0004D27D File Offset: 0x0004B47D
	private void QuitGame()
	{
		Gogan.LogEvent("Game", "Quit", "", 0L);
		Application.Quit();
	}

	// Token: 0x060008D8 RID: 2264 RVA: 0x0004D29A File Offset: 0x0004B49A
	public void OnQuitNo()
	{
		this.m_quitDialog.gameObject.SetActive(false);
		this.m_menuDialog.gameObject.SetActive(true);
	}

	// Token: 0x060008D9 RID: 2265 RVA: 0x0004D2BE File Offset: 0x0004B4BE
	public void OnLogout()
	{
		this.m_menuDialog.gameObject.SetActive(false);
		this.m_logoutDialog.gameObject.SetActive(true);
	}

	// Token: 0x060008DA RID: 2266 RVA: 0x0004D2E4 File Offset: 0x0004B4E4
	public void OnLogoutYes()
	{
		if (this.m_saveOnLogout && !Menu.CanSaveToCloudStorage())
		{
			this.m_logoutDialog.gameObject.SetActive(false);
			if (!FileHelpers.LocalStorageSupported)
			{
				this.m_saveOnLogout = false;
			}
			this.ShowCloudStorageFullWarning(new Menu.CloudStorageFullOkCallback(this.Logout));
			return;
		}
		this.Logout();
	}

	// Token: 0x060008DB RID: 2267 RVA: 0x0004D338 File Offset: 0x0004B538
	public void Logout()
	{
		Gogan.LogEvent("Game", "LogOut", "", 0L);
		Game.instance.Logout(this.m_saveOnLogout, this.m_loadStartSceneOnLogout);
	}

	// Token: 0x060008DC RID: 2268 RVA: 0x0004D366 File Offset: 0x0004B566
	public void OnLogoutNo()
	{
		this.m_logoutDialog.gameObject.SetActive(false);
		this.m_menuDialog.gameObject.SetActive(true);
	}

	// Token: 0x060008DD RID: 2269 RVA: 0x0004D38A File Offset: 0x0004B58A
	public void OnClose()
	{
		Gogan.LogEvent("Screen", "Exit", "Menu", 0L);
		this.Hide();
	}

	// Token: 0x060008DE RID: 2270 RVA: 0x0004D3A8 File Offset: 0x0004B5A8
	public void OnButtonFeedback()
	{
		UnityEngine.Object.Instantiate<GameObject>(this.m_feedbackPrefab, base.transform);
	}

	// Token: 0x060008DF RID: 2271 RVA: 0x0004D3BC File Offset: 0x0004B5BC
	public void ShowCloudStorageFullWarning(Menu.CloudStorageFullOkCallback okCallback)
	{
		if (this.m_cloudStorageWarningShown)
		{
			if (okCallback != null)
			{
				okCallback();
			}
			return;
		}
		if (okCallback != null)
		{
			this.cloudStorageFullOkCallbackList.Add(okCallback);
		}
		this.m_cloudStorageWarning.SetActive(true);
	}

	// Token: 0x060008E0 RID: 2272 RVA: 0x0004D3EC File Offset: 0x0004B5EC
	public void OnCloudStorageFullWarningOk()
	{
		int count = this.cloudStorageFullOkCallbackList.Count;
		while (count-- > 0)
		{
			this.cloudStorageFullOkCallbackList[count]();
		}
		this.cloudStorageFullOkCallbackList.Clear();
		this.m_cloudStorageWarningShown = true;
		this.m_cloudStorageWarning.SetActive(false);
	}

	// Token: 0x060008E1 RID: 2273 RVA: 0x0004D43E File Offset: 0x0004B63E
	public void ShowCloudStorageLowNextSaveWarning()
	{
		this.m_saveOnLogout = false;
		this.m_loadStartSceneOnLogout = false;
		this.Logout();
		this.m_cloudStorageWarningNextSave.SetActive(true);
	}

	// Token: 0x060008E2 RID: 2274 RVA: 0x0004D460 File Offset: 0x0004B660
	public void OnCloudStorageLowNextSaveWarningOk()
	{
		SceneManager.LoadScene(this.m_startScene, LoadSceneMode.Single);
	}

	// Token: 0x1700003B RID: 59
	// (get) Token: 0x060008E3 RID: 2275 RVA: 0x0004D46E File Offset: 0x0004B66E
	public bool PlayerListActive
	{
		get
		{
			return this.m_currentPlayersInstance != null && this.m_currentPlayersInstance.activeSelf;
		}
	}

	// Token: 0x04000A62 RID: 2658
	private bool m_cloudStorageWarningShown;

	// Token: 0x04000A63 RID: 2659
	private List<Menu.CloudStorageFullOkCallback> cloudStorageFullOkCallbackList = new List<Menu.CloudStorageFullOkCallback>();

	// Token: 0x04000A64 RID: 2660
	[SerializeField]
	private GameObject CurrentPlayersPrefab;

	// Token: 0x04000A65 RID: 2661
	private GameObject m_currentPlayersInstance;

	// Token: 0x04000A66 RID: 2662
	public Button menuCurrentPlayersListButton;

	// Token: 0x04000A67 RID: 2663
	public Button menuInviteFriendsButton;

	// Token: 0x04000A68 RID: 2664
	private GameObject m_settingsInstance;

	// Token: 0x04000A69 RID: 2665
	public Button saveButton;

	// Token: 0x04000A6A RID: 2666
	public TMP_Text lastSaveText;

	// Token: 0x04000A6B RID: 2667
	private DateTime m_lastSavedDate = DateTime.MinValue;

	// Token: 0x04000A6C RID: 2668
	public RectTransform menuEntriesParent;

	// Token: 0x04000A6D RID: 2669
	private static Menu m_instance;

	// Token: 0x04000A6E RID: 2670
	public Transform m_root;

	// Token: 0x04000A6F RID: 2671
	public Transform m_menuDialog;

	// Token: 0x04000A70 RID: 2672
	public Transform m_quitDialog;

	// Token: 0x04000A71 RID: 2673
	public Transform m_logoutDialog;

	// Token: 0x04000A72 RID: 2674
	public GameObject m_cloudStorageWarning;

	// Token: 0x04000A73 RID: 2675
	public GameObject m_cloudStorageWarningNextSave;

	// Token: 0x04000A74 RID: 2676
	public GameObject m_settingsPrefab;

	// Token: 0x04000A75 RID: 2677
	public GameObject m_feedbackPrefab;

	// Token: 0x04000A76 RID: 2678
	public GameObject m_gamepadRoot;

	// Token: 0x04000A77 RID: 2679
	public GamepadMapController m_gamepadMapController;

	// Token: 0x04000A78 RID: 2680
	public SceneReference m_startScene;

	// Token: 0x04000A79 RID: 2681
	private int m_hiddenFrames;

	// Token: 0x04000A7A RID: 2682
	public GameObject m_skipButton;

	// Token: 0x04000A7B RID: 2683
	private int m_updateLocalizationTimer;

	// Token: 0x04000A7C RID: 2684
	private int m_manualSaveCooldownUntil;

	// Token: 0x04000A7D RID: 2685
	private const int ManualSavingCooldownTime = 60;

	// Token: 0x04000A7E RID: 2686
	private bool m_rebuildLayout;

	// Token: 0x04000A7F RID: 2687
	private bool m_saveOnLogout = true;

	// Token: 0x04000A80 RID: 2688
	private bool m_loadStartSceneOnLogout = true;

	// Token: 0x04000A81 RID: 2689
	private Menu.CloseMenuState m_closeMenuState = Menu.CloseMenuState.CanBeClosed;

	// Token: 0x04000A82 RID: 2690
	private Button m_firstMenuButton;

	// Token: 0x04000A83 RID: 2691
	public static bool ExceedCloudStorageTest;

	// Token: 0x020002A3 RID: 675
	// (Invoke) Token: 0x0600209E RID: 8350
	public delegate void CloudStorageFullOkCallback();

	// Token: 0x020002A4 RID: 676
	private enum CloseMenuState : byte
	{
		// Token: 0x0400223D RID: 8765
		SettingsOpen,
		// Token: 0x0400223E RID: 8766
		Blocked,
		// Token: 0x0400223F RID: 8767
		CanBeClosed
	}
}
