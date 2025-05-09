using System;
using System.Collections.Generic;
using System.Text;
using Splatform;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Token: 0x0200006D RID: 109
public class Chat : Terminal
{
	// Token: 0x1700001D RID: 29
	// (get) Token: 0x060006ED RID: 1773 RVA: 0x000396BC File Offset: 0x000378BC
	public static Chat instance
	{
		get
		{
			return Chat.m_instance;
		}
	}

	// Token: 0x060006EE RID: 1774 RVA: 0x000396C3 File Offset: 0x000378C3
	private void OnDestroy()
	{
		Localization.OnLanguageChange = (Action)Delegate.Remove(Localization.OnLanguageChange, new Action(this.OnLanguageChanged));
	}

	// Token: 0x060006EF RID: 1775 RVA: 0x000396E8 File Offset: 0x000378E8
	public override void Awake()
	{
		base.Awake();
		Chat.m_instance = this;
		ZRoutedRpc.instance.Register<Vector3, int, UserInfo, string>("ChatMessage", new RoutedMethod<Vector3, int, UserInfo, string>.Method(this.RPC_ChatMessage));
		ZRoutedRpc.instance.Register<Vector3, Quaternion, bool>("RPC_TeleportPlayer", new Action<long, Vector3, Quaternion, bool>(this.RPC_TeleportPlayer));
		base.AddString(Localization.instance.Localize("/w [text] - $chat_whisper"));
		base.AddString(Localization.instance.Localize("/s [text] - $chat_shout"));
		base.AddString(Localization.instance.Localize("/die - $chat_kill"));
		base.AddString(Localization.instance.Localize("/resetspawn - $chat_resetspawn"));
		base.AddString(Localization.instance.Localize("/[emote]"));
		StringBuilder stringBuilder = new StringBuilder("Emotes: ");
		for (int i = 0; i < 23; i++)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			Emotes emotes = (Emotes)i;
			stringBuilder2.Append(emotes.ToString().ToLower());
			if (i + 1 < 23)
			{
				stringBuilder.Append(", ");
			}
		}
		base.AddString(Localization.instance.Localize(stringBuilder.ToString()));
		base.AddString("");
		this.m_input.gameObject.SetActive(false);
		this.m_worldTextBase.SetActive(false);
		this.m_tabPrefix = '/';
		this.m_maxVisibleBufferLength = 20;
		Terminal.m_bindList = new List<string>(PlayerPrefs.GetString("ConsoleBindings", "").Split('\n', StringSplitOptions.None));
		if (Terminal.m_bindList.Count == 0)
		{
			base.TryRunCommand("resetbinds", false, false);
		}
		Terminal.updateBinds();
		this.m_autoCompleteSecrets = true;
		Localization.OnLanguageChange = (Action)Delegate.Combine(Localization.OnLanguageChange, new Action(this.OnLanguageChanged));
	}

	// Token: 0x060006F0 RID: 1776 RVA: 0x0003989C File Offset: 0x00037A9C
	private void OnLanguageChanged()
	{
		foreach (Chat.NpcText npcText in this.m_npcTexts)
		{
			npcText.UpdateText();
		}
	}

	// Token: 0x060006F1 RID: 1777 RVA: 0x000398EC File Offset: 0x00037AEC
	public bool HasFocus()
	{
		return this.m_chatWindow != null && this.m_chatWindow.gameObject.activeInHierarchy && this.m_input.isFocused;
	}

	// Token: 0x060006F2 RID: 1778 RVA: 0x0003991B File Offset: 0x00037B1B
	public bool IsChatDialogWindowVisible()
	{
		return this.m_chatWindow.gameObject.activeSelf;
	}

	// Token: 0x060006F3 RID: 1779 RVA: 0x00039930 File Offset: 0x00037B30
	public override void Update()
	{
		this.m_focused = false;
		this.m_hideTimer += Time.deltaTime;
		this.m_chatWindow.gameObject.SetActive(this.m_hideTimer < this.m_hideDelay);
		if (!this.m_wasFocused)
		{
			if (Player.m_localPlayer != null && !global::Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && !Menu.IsVisible() && !InventoryGui.IsVisible())
			{
				bool flag = ZInput.InputLayout == InputLayout.Alternative1;
				bool button = ZInput.GetButton("JoyLBumper");
				bool button2 = ZInput.GetButton("JoyLTrigger");
				if (ZInput.GetButtonDown("Chat") || (ZInput.GetButtonDown("JoyChat") && ZInput.GetButton("JoyAltKeys") && (!flag || !button2) && (flag || !button)))
				{
					this.m_hideTimer = 0f;
					this.m_chatWindow.gameObject.SetActive(true);
					this.m_input.gameObject.SetActive(true);
					this.TryShowTextCommunicationRestrictedSystemPopup();
					if (this.m_doubleOpenForVirtualKeyboard && Application.isConsolePlatform)
					{
						this.m_input.Select();
					}
					else
					{
						this.m_input.ActivateInputField();
					}
				}
			}
		}
		else if (this.m_wasFocused)
		{
			this.m_hideTimer = 0f;
			this.m_focused = true;
			if (ZInput.GetKeyDown(KeyCode.Mouse0, true) || ZInput.GetKey(KeyCode.Mouse1, true) || ZInput.GetKeyDown(KeyCode.Escape, true) || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyLStickDown"))
			{
				EventSystem.current.SetSelectedGameObject(null);
				this.m_input.gameObject.SetActive(false);
				this.m_focused = false;
			}
		}
		this.m_wasFocused = this.m_input.isFocused;
		if (!this.m_input.isFocused && (global::Console.instance == null || !global::Console.instance.m_chatWindow.gameObject.activeInHierarchy))
		{
			foreach (KeyValuePair<KeyCode, List<string>> keyValuePair in Terminal.m_binds)
			{
				if (ZInput.GetKeyDown(keyValuePair.Key, true))
				{
					foreach (string text in keyValuePair.Value)
					{
						base.TryRunCommand(text, true, true);
					}
				}
			}
		}
		base.Update();
	}

	// Token: 0x060006F4 RID: 1780 RVA: 0x00039C00 File Offset: 0x00037E00
	private void TryShowTextCommunicationRestrictedSystemPopup()
	{
		if (!PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.TextCommunication).IsGranted())
		{
			if (!this.m_socialRestrictionNotificationShown)
			{
				PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.TextCommunication);
				this.m_socialRestrictionNotificationShown = true;
				return;
			}
		}
		else
		{
			this.m_socialRestrictionNotificationShown = false;
		}
	}

	// Token: 0x060006F5 RID: 1781 RVA: 0x00039C53 File Offset: 0x00037E53
	public new void SendInput()
	{
		base.SendInput();
		this.m_input.gameObject.SetActive(false);
	}

	// Token: 0x060006F6 RID: 1782 RVA: 0x00039C6C File Offset: 0x00037E6C
	public void Hide()
	{
		this.m_hideTimer = this.m_hideDelay;
	}

	// Token: 0x060006F7 RID: 1783 RVA: 0x00039C7A File Offset: 0x00037E7A
	private void LateUpdate()
	{
		this.UpdateWorldTexts(Time.deltaTime);
		this.UpdateNpcTexts(Time.deltaTime);
	}

	// Token: 0x060006F8 RID: 1784 RVA: 0x00039C94 File Offset: 0x00037E94
	public void OnNewChatMessage(GameObject go, long senderID, Vector3 pos, Talker.Type type, UserInfo sender, string text)
	{
		RelationsManager.CheckPermissionAsync(sender.UserId, Permission.CommunicateWithUsingText, false, delegate(RelationsManagerPermissionResult result)
		{
			if (!result.IsGranted())
			{
				return;
			}
			if (this == null)
			{
				Debug.LogError("Chat has already been destroyed!");
				return;
			}
			text = text.Replace('<', ' ');
			text = text.Replace('>', ' ');
			if (result == RelationsManagerPermissionResult.GrantedRequiresFiltering)
			{
				CensorShittyWords.Filter(text, out text);
			}
			if (type != Talker.Type.Ping)
			{
				this.m_hideTimer = 0f;
				this.AddString(sender.UserId, text, type, false);
			}
			if (Minimap.instance && Player.m_localPlayer && Minimap.instance.m_mode == Minimap.MapMode.None && Vector3.Distance(Player.m_localPlayer.transform.position, pos) > Minimap.instance.m_nomapPingDistance)
			{
				return;
			}
			this.AddInworldText(go, senderID, pos, type, sender, text);
		});
	}

	// Token: 0x060006F9 RID: 1785 RVA: 0x00039CFC File Offset: 0x00037EFC
	private void UpdateWorldTexts(float dt)
	{
		Chat.WorldTextInstance worldTextInstance = null;
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		foreach (Chat.WorldTextInstance worldTextInstance2 in this.m_worldTexts)
		{
			worldTextInstance2.m_timer += dt;
			if (worldTextInstance2.m_timer > this.m_worldTextTTL && worldTextInstance == null)
			{
				worldTextInstance = worldTextInstance2;
			}
			Chat.WorldTextInstance worldTextInstance3 = worldTextInstance2;
			worldTextInstance3.m_position.y = worldTextInstance3.m_position.y + dt * 0.15f;
			Vector3 vector = Vector3.zero;
			if (worldTextInstance2.m_go)
			{
				Character component = worldTextInstance2.m_go.GetComponent<Character>();
				if (component)
				{
					vector = component.GetHeadPoint() + Vector3.up * 0.3f;
				}
				else
				{
					vector = worldTextInstance2.m_go.transform.position + Vector3.up * 0.3f;
				}
			}
			else
			{
				vector = worldTextInstance2.m_position + Vector3.up * 0.3f;
			}
			Vector3 vector2 = mainCamera.WorldToScreenPointScaled(vector);
			if (vector2.x < 0f || vector2.x > (float)Screen.width || vector2.y < 0f || vector2.y > (float)Screen.height || vector2.z < 0f)
			{
				Vector3 vector3 = vector - mainCamera.transform.position;
				bool flag = Vector3.Dot(mainCamera.transform.right, vector3) < 0f;
				Vector3 vector4 = vector3;
				vector4.y = 0f;
				float magnitude = vector4.magnitude;
				float y = vector3.y;
				Vector3 a = mainCamera.transform.forward;
				a.y = 0f;
				a.Normalize();
				a *= magnitude;
				Vector3 b = a + Vector3.up * y;
				vector2 = mainCamera.WorldToScreenPointScaled(mainCamera.transform.position + b);
				vector2.x = (float)(flag ? 0 : Screen.width);
			}
			RectTransform rt = worldTextInstance2.m_gui.transform as RectTransform;
			vector2 = this.ClampToScreenEdge(vector2, rt, true);
			vector2.z = Mathf.Min(vector2.z, 100f);
			worldTextInstance2.m_gui.transform.position = vector2;
		}
		if (worldTextInstance != null)
		{
			UnityEngine.Object.Destroy(worldTextInstance.m_gui);
			this.m_worldTexts.Remove(worldTextInstance);
		}
	}

	// Token: 0x060006FA RID: 1786 RVA: 0x00039FA8 File Offset: 0x000381A8
	private void AddInworldText(GameObject go, long senderID, Vector3 position, Talker.Type type, UserInfo user, string text)
	{
		Chat.WorldTextInstance worldTextInstance = this.FindExistingWorldText(senderID);
		if (worldTextInstance == null)
		{
			worldTextInstance = new Chat.WorldTextInstance();
			worldTextInstance.m_talkerID = senderID;
			worldTextInstance.m_gui = UnityEngine.Object.Instantiate<GameObject>(this.m_worldTextBase, base.transform);
			worldTextInstance.m_gui.gameObject.SetActive(true);
			Transform transform = worldTextInstance.m_gui.transform.Find("Text");
			worldTextInstance.m_textMeshField = transform.GetComponent<TextMeshProUGUI>();
			this.m_worldTexts.Add(worldTextInstance);
		}
		worldTextInstance.m_userInfo = user;
		worldTextInstance.m_type = type;
		worldTextInstance.m_go = go;
		worldTextInstance.m_position = position;
		Color color;
		switch (type)
		{
		case Talker.Type.Whisper:
			color = new Color(1f, 1f, 1f, 0.75f);
			text = text.ToLowerInvariant();
			goto IL_106;
		case Talker.Type.Shout:
			color = Color.yellow;
			text = text.ToUpper();
			goto IL_106;
		case Talker.Type.Ping:
			color = new Color(0.6f, 0.7f, 1f, 1f);
			text = "PING";
			goto IL_106;
		}
		color = Color.white;
		IL_106:
		worldTextInstance.m_textMeshField.color = color;
		worldTextInstance.m_timer = 0f;
		worldTextInstance.m_text = text;
		this.UpdateWorldTextField(worldTextInstance);
	}

	// Token: 0x060006FB RID: 1787 RVA: 0x0003A0E4 File Offset: 0x000382E4
	private void UpdateWorldTextField(Chat.WorldTextInstance wt)
	{
		string text = "";
		if (wt.m_type == Talker.Type.Shout || wt.m_type == Talker.Type.Ping)
		{
			text = wt.m_name + ": ";
		}
		text += wt.m_text;
		wt.m_textMeshField.text = text;
	}

	// Token: 0x060006FC RID: 1788 RVA: 0x0003A134 File Offset: 0x00038334
	private Chat.WorldTextInstance FindExistingWorldText(long senderID)
	{
		foreach (Chat.WorldTextInstance worldTextInstance in this.m_worldTexts)
		{
			if (worldTextInstance.m_talkerID == senderID)
			{
				return worldTextInstance;
			}
		}
		return null;
	}

	// Token: 0x060006FD RID: 1789 RVA: 0x0003A190 File Offset: 0x00038390
	protected override bool isAllowedCommand(Terminal.ConsoleCommand cmd)
	{
		return !cmd.IsCheat && base.isAllowedCommand(cmd);
	}

	// Token: 0x060006FE RID: 1790 RVA: 0x0003A1A4 File Offset: 0x000383A4
	protected override void InputText()
	{
		string text = this.m_input.text;
		if (text.Length == 0)
		{
			return;
		}
		if (text[0] == '/')
		{
			text = text.Substring(1);
		}
		else
		{
			text = "say " + text;
		}
		base.TryRunCommand(text, this, false);
	}

	// Token: 0x060006FF RID: 1791 RVA: 0x0003A1F5 File Offset: 0x000383F5
	public void TeleportPlayer(long targetPeerID, Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerID, "RPC_TeleportPlayer", new object[]
		{
			pos,
			rot,
			distantTeleport
		});
	}

	// Token: 0x06000700 RID: 1792 RVA: 0x0003A229 File Offset: 0x00038429
	private void RPC_TeleportPlayer(long sender, Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (Player.m_localPlayer != null)
		{
			Player.m_localPlayer.TeleportTo(pos, rot, distantTeleport);
		}
	}

	// Token: 0x06000701 RID: 1793 RVA: 0x0003A247 File Offset: 0x00038447
	private void RPC_ChatMessage(long sender, Vector3 position, int type, UserInfo userInfo, string text)
	{
		this.OnNewChatMessage(null, sender, position, (Talker.Type)type, userInfo, text);
	}

	// Token: 0x06000702 RID: 1794 RVA: 0x0003A258 File Offset: 0x00038458
	public void SendText(Talker.Type type, string text)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			if (type == Talker.Type.Shout)
			{
				Chat.CheckPermissionsAndSendChatMessageRPCsAsync(delegate(long user, bool filterText)
				{
					UserInfo userInfo;
					string text2;
					Chat.GetChatMessageData(text, filterText, out userInfo, out text2);
					ZRoutedRpc.instance.InvokeRoutedRPC(user, "ChatMessage", new object[]
					{
						Player.m_localPlayer.GetHeadPoint(),
						2,
						userInfo,
						text2
					});
				});
				return;
			}
			localPlayer.GetComponent<Talker>().Say(type, text);
		}
	}

	// Token: 0x06000703 RID: 1795 RVA: 0x0003A2A8 File Offset: 0x000384A8
	public static void CheckPermissionsAndSendChatMessageRPCsAsync(Chat.SendChatMessageRPCHandler sendMessageHandler)
	{
		if (!PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.TextCommunication).IsGranted())
		{
			return;
		}
		if (PlatformManager.DistributionPlatform.RelationsProvider == null)
		{
			sendMessageHandler(0L, RelationsManager.PermissionRequiresFiltering(Permission.CommunicateWithUsingText));
			return;
		}
		Chat.SendChatMessageRPCHandler sendMessageHandler2 = sendMessageHandler;
		if (sendMessageHandler2 != null)
		{
			sendMessageHandler2(ZNet.instance.LocalPlayerCharacterID.UserID, false);
		}
		List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
		for (int i = 0; i < playerList.Count; i++)
		{
			ZNet.PlayerInfo playerInfo = playerList[i];
			if (playerInfo.m_characterID == ZDOID.None)
			{
				ZLog.LogWarning(string.Format("Character ID for player {0} was {1}. Skipping.", playerInfo, ZDOID.None));
			}
			else if (!(playerInfo.m_characterID == ZNet.instance.LocalPlayerCharacterID))
			{
				RelationsManager.CheckPermissionAsync(playerList[i].m_userInfo.m_id, Permission.CommunicateWithUsingText, true, delegate(RelationsManagerPermissionResult result)
				{
					switch (result)
					{
					case RelationsManagerPermissionResult.Granted:
						sendMessageHandler(playerInfo.m_characterID.UserID, false);
						return;
					case RelationsManagerPermissionResult.GrantedRequiresFiltering:
						sendMessageHandler(playerInfo.m_characterID.UserID, true);
						return;
					case RelationsManagerPermissionResult.Denied:
						ZLog.Log(string.Format("Withholding chat message for user {0} because the {1} permission was denied.", playerInfo, Permission.CommunicateWithUsingText));
						return;
					}
					ZLog.LogError(string.Format("Failed to send chat message to user {0}: {1}", playerInfo, result));
				});
			}
		}
	}

	// Token: 0x06000704 RID: 1796 RVA: 0x0003A3DE File Offset: 0x000385DE
	public static void GetChatMessageData(string text, bool filterText, out UserInfo userInfoToSend, out string textToSend)
	{
		userInfoToSend = UserInfo.GetLocalUser();
		if (filterText)
		{
			CensorShittyWords.Filter(userInfoToSend.Name, out userInfoToSend.Name);
			CensorShittyWords.Filter(text, out textToSend);
			return;
		}
		textToSend = text;
	}

	// Token: 0x06000705 RID: 1797 RVA: 0x0003A40C File Offset: 0x0003860C
	public void SendPing(Vector3 position)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			Vector3 vector = position;
			vector.y = localPlayer.transform.position.y;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", new object[]
			{
				vector,
				3,
				UserInfo.GetLocalUser(),
				""
			});
			if (Player.m_debugMode && global::Console.instance != null && global::Console.instance.IsCheatsEnabled() && global::Console.instance != null)
			{
				global::Console.instance.AddString(string.Format("Pinged at: {0}, {1}", vector.x, vector.z));
			}
		}
	}

	// Token: 0x1700001E RID: 30
	// (get) Token: 0x06000706 RID: 1798 RVA: 0x0003A4D6 File Offset: 0x000386D6
	public List<Chat.WorldTextInstance> WorldTexts
	{
		get
		{
			return this.m_worldTexts;
		}
	}

	// Token: 0x06000707 RID: 1799 RVA: 0x0003A4E0 File Offset: 0x000386E0
	public void GetShoutWorldTexts(List<Chat.WorldTextInstance> texts)
	{
		foreach (Chat.WorldTextInstance worldTextInstance in this.m_worldTexts)
		{
			if (worldTextInstance.m_type == Talker.Type.Shout)
			{
				texts.Add(worldTextInstance);
			}
		}
	}

	// Token: 0x06000708 RID: 1800 RVA: 0x0003A53C File Offset: 0x0003873C
	public void GetPingWorldTexts(List<Chat.WorldTextInstance> texts)
	{
		foreach (Chat.WorldTextInstance worldTextInstance in this.m_worldTexts)
		{
			if (worldTextInstance.m_type == Talker.Type.Ping)
			{
				texts.Add(worldTextInstance);
			}
		}
	}

	// Token: 0x06000709 RID: 1801 RVA: 0x0003A598 File Offset: 0x00038798
	private void UpdateNpcTexts(float dt)
	{
		Chat.NpcText npcText = null;
		Camera mainCamera = Utils.GetMainCamera();
		foreach (Chat.NpcText npcText2 in this.m_npcTexts)
		{
			if (!npcText2.m_go)
			{
				npcText2.m_gui.SetActive(false);
				if (npcText == null)
				{
					npcText = npcText2;
				}
			}
			else
			{
				if (npcText2.m_timeout)
				{
					npcText2.m_ttl -= dt;
					if (npcText2.m_ttl <= 0f)
					{
						npcText2.SetVisible(false);
						if (!npcText2.IsVisible())
						{
							npcText = npcText2;
							continue;
						}
						continue;
					}
				}
				Vector3 vector = npcText2.m_go.transform.position + npcText2.m_offset;
				Vector3 vector2 = mainCamera.WorldToScreenPointScaled(vector);
				if (vector2.x < 0f || vector2.x > (float)Screen.width || vector2.y < 0f || vector2.y > (float)Screen.height || vector2.z < 0f)
				{
					npcText2.SetVisible(false);
				}
				else
				{
					npcText2.SetVisible(true);
					RectTransform rt = npcText2.m_gui.transform as RectTransform;
					vector2 = this.ClampToScreenEdge(vector2, rt, false);
					npcText2.m_gui.transform.position = vector2;
				}
				if (Vector3.Distance(mainCamera.transform.position, vector) > npcText2.m_cullDistance)
				{
					npcText2.SetVisible(false);
					if (npcText == null && !npcText2.IsVisible())
					{
						npcText = npcText2;
					}
				}
			}
		}
		if (npcText != null)
		{
			this.ClearNpcText(npcText);
		}
		if (Hud.instance.m_userHidden && this.m_npcTexts.Count > 0)
		{
			this.HideAllNpcTexts();
		}
	}

	// Token: 0x0600070A RID: 1802 RVA: 0x0003A768 File Offset: 0x00038968
	public Vector3 ClampToScreenEdge(Vector3 screenPos, RectTransform rt, bool isChatMessage)
	{
		CanvasScaler componentInParent = base.GetComponentInParent<CanvasScaler>();
		float num = componentInParent ? componentInParent.scaleFactor : 1f;
		float num2 = rt.rect.width * num;
		float num3 = rt.rect.height * num;
		int num4 = isChatMessage ? 2 : 1;
		screenPos.x = Mathf.Clamp(screenPos.x, num2 / 2f, (float)Screen.width - num2 / 2f);
		screenPos.y = Mathf.Clamp(screenPos.y, num3 / 2f, (float)Screen.height - num3 * (float)num4);
		return screenPos;
	}

	// Token: 0x0600070B RID: 1803 RVA: 0x0003A80C File Offset: 0x00038A0C
	public void HideAllNpcTexts()
	{
		for (int i = this.m_npcTexts.Count - 1; i >= 0; i--)
		{
			this.m_npcTexts[i].SetVisible(false);
			this.ClearNpcText(this.m_npcTexts[i]);
		}
	}

	// Token: 0x0600070C RID: 1804 RVA: 0x0003A858 File Offset: 0x00038A58
	public void SetNpcText(GameObject talker, Vector3 offset, float cullDistance, float ttl, string topic, string text, bool large)
	{
		if (Hud.instance.m_userHidden)
		{
			return;
		}
		Chat.NpcText npcText = this.FindNpcText(talker);
		if (npcText != null)
		{
			this.ClearNpcText(npcText);
		}
		npcText = new Chat.NpcText();
		npcText.m_topic = topic;
		npcText.m_text = text;
		npcText.m_go = talker;
		npcText.m_gui = UnityEngine.Object.Instantiate<GameObject>(large ? this.m_npcTextBaseLarge : this.m_npcTextBase, base.transform);
		npcText.m_gui.SetActive(true);
		npcText.m_animator = npcText.m_gui.GetComponent<Animator>();
		npcText.m_topicField = npcText.m_gui.transform.Find("Topic").GetComponent<TextMeshProUGUI>();
		npcText.m_textField = npcText.m_gui.transform.Find("Text").GetComponent<TextMeshProUGUI>();
		npcText.m_ttl = ttl;
		npcText.m_timeout = (ttl > 0f);
		npcText.m_offset = offset;
		npcText.m_cullDistance = cullDistance;
		npcText.UpdateText();
		this.m_npcTexts.Add(npcText);
	}

	// Token: 0x0600070D RID: 1805 RVA: 0x0003A958 File Offset: 0x00038B58
	public int CurrentNpcTexts()
	{
		return this.m_npcTexts.Count;
	}

	// Token: 0x0600070E RID: 1806 RVA: 0x0003A968 File Offset: 0x00038B68
	public bool IsDialogVisible(GameObject talker)
	{
		Chat.NpcText npcText = this.FindNpcText(talker);
		return npcText != null && npcText.IsVisible();
	}

	// Token: 0x0600070F RID: 1807 RVA: 0x0003A988 File Offset: 0x00038B88
	public void ClearNpcText(GameObject talker)
	{
		Chat.NpcText npcText = this.FindNpcText(talker);
		if (npcText != null)
		{
			this.ClearNpcText(npcText);
		}
	}

	// Token: 0x06000710 RID: 1808 RVA: 0x0003A9A7 File Offset: 0x00038BA7
	private void ClearNpcText(Chat.NpcText npcText)
	{
		UnityEngine.Object.Destroy(npcText.m_gui);
		this.m_npcTexts.Remove(npcText);
	}

	// Token: 0x06000711 RID: 1809 RVA: 0x0003A9C4 File Offset: 0x00038BC4
	private Chat.NpcText FindNpcText(GameObject go)
	{
		foreach (Chat.NpcText npcText in this.m_npcTexts)
		{
			if (npcText.m_go == go)
			{
				return npcText;
			}
		}
		return null;
	}

	// Token: 0x1700001F RID: 31
	// (get) Token: 0x06000712 RID: 1810 RVA: 0x0003AA28 File Offset: 0x00038C28
	protected override Terminal m_terminalInstance
	{
		get
		{
			return Chat.m_instance;
		}
	}

	// Token: 0x0400082E RID: 2094
	private static Chat m_instance;

	// Token: 0x0400082F RID: 2095
	public float m_hideDelay = 10f;

	// Token: 0x04000830 RID: 2096
	public float m_worldTextTTL = 5f;

	// Token: 0x04000831 RID: 2097
	public GameObject m_worldTextBase;

	// Token: 0x04000832 RID: 2098
	public GameObject m_npcTextBase;

	// Token: 0x04000833 RID: 2099
	public GameObject m_npcTextBaseLarge;

	// Token: 0x04000834 RID: 2100
	[global::Tooltip("If true the player has to open chat twice to enter input mode.")]
	[SerializeField]
	protected bool m_doubleOpenForVirtualKeyboard = true;

	// Token: 0x04000835 RID: 2101
	private List<Chat.WorldTextInstance> m_worldTexts = new List<Chat.WorldTextInstance>();

	// Token: 0x04000836 RID: 2102
	private List<Chat.NpcText> m_npcTexts = new List<Chat.NpcText>();

	// Token: 0x04000837 RID: 2103
	private float m_hideTimer = 9999f;

	// Token: 0x04000838 RID: 2104
	public bool m_wasFocused;

	// Token: 0x04000839 RID: 2105
	private bool m_socialRestrictionNotificationShown;

	// Token: 0x02000265 RID: 613
	// (Invoke) Token: 0x06001F44 RID: 8004
	public delegate void SendChatMessageRPCHandler(long user, bool filterText);

	// Token: 0x02000266 RID: 614
	public class WorldTextInstance
	{
		// Token: 0x17000182 RID: 386
		// (get) Token: 0x06001F47 RID: 8007 RVA: 0x000E2D2F File Offset: 0x000E0F2F
		public string m_name
		{
			get
			{
				return this.m_userInfo.GetDisplayName();
			}
		}

		// Token: 0x040020B8 RID: 8376
		public UserInfo m_userInfo;

		// Token: 0x040020B9 RID: 8377
		public long m_talkerID;

		// Token: 0x040020BA RID: 8378
		public GameObject m_go;

		// Token: 0x040020BB RID: 8379
		public Vector3 m_position;

		// Token: 0x040020BC RID: 8380
		public float m_timer;

		// Token: 0x040020BD RID: 8381
		public GameObject m_gui;

		// Token: 0x040020BE RID: 8382
		public TextMeshProUGUI m_textMeshField;

		// Token: 0x040020BF RID: 8383
		public Talker.Type m_type;

		// Token: 0x040020C0 RID: 8384
		public string m_text = "";
	}

	// Token: 0x02000267 RID: 615
	public class NpcText
	{
		// Token: 0x06001F49 RID: 8009 RVA: 0x000E2D4F File Offset: 0x000E0F4F
		public void SetVisible(bool visible)
		{
			this.m_animator.SetBool("visible", visible);
		}

		// Token: 0x06001F4A RID: 8010 RVA: 0x000E2D64 File Offset: 0x000E0F64
		public bool IsVisible()
		{
			return this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("visible") || this.m_animator.GetBool("visible");
		}

		// Token: 0x06001F4B RID: 8011 RVA: 0x000E2DA0 File Offset: 0x000E0FA0
		public void UpdateText()
		{
			if (this.m_topic.Length > 0)
			{
				this.m_textField.text = "<color=orange>" + Localization.instance.Localize(this.m_topic) + "</color>\n" + Localization.instance.Localize(this.m_text);
				return;
			}
			this.m_textField.text = Localization.instance.Localize(this.m_text);
		}

		// Token: 0x040020C1 RID: 8385
		public string m_topic;

		// Token: 0x040020C2 RID: 8386
		public string m_text;

		// Token: 0x040020C3 RID: 8387
		public GameObject m_go;

		// Token: 0x040020C4 RID: 8388
		public Vector3 m_offset = Vector3.zero;

		// Token: 0x040020C5 RID: 8389
		public float m_cullDistance = 20f;

		// Token: 0x040020C6 RID: 8390
		public GameObject m_gui;

		// Token: 0x040020C7 RID: 8391
		public Animator m_animator;

		// Token: 0x040020C8 RID: 8392
		public TextMeshProUGUI m_textField;

		// Token: 0x040020C9 RID: 8393
		public TextMeshProUGUI m_topicField;

		// Token: 0x040020CA RID: 8394
		public float m_ttl;

		// Token: 0x040020CB RID: 8395
		public bool m_timeout;
	}
}
