using System;
using System.Collections.Generic;
using System.IO;
using GUIFramework;
using Splatform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000090 RID: 144
public class ServerList : MonoBehaviour
{
	// Token: 0x17000040 RID: 64
	// (get) Token: 0x0600098E RID: 2446 RVA: 0x0005357D File Offset: 0x0005177D
	public bool currentServerListIsLocal
	{
		get
		{
			return this.currentServerList == ServerListType.recent || this.currentServerList == ServerListType.favorite;
		}
	}

	// Token: 0x17000041 RID: 65
	// (get) Token: 0x0600098F RID: 2447 RVA: 0x00053593 File Offset: 0x00051793
	private List<ServerStatus> CurrentServerListFiltered
	{
		get
		{
			if (this.filteredListOutdated)
			{
				this.FilterList();
				this.filteredListOutdated = false;
			}
			return this.m_filteredList;
		}
	}

	// Token: 0x06000990 RID: 2448 RVA: 0x000535B0 File Offset: 0x000517B0
	private static string GetServerListFolder(FileHelpers.FileSource fileSource)
	{
		if (fileSource != FileHelpers.FileSource.Local)
		{
			return "/serverlist/";
		}
		return "/serverlist_local/";
	}

	// Token: 0x06000991 RID: 2449 RVA: 0x000535C1 File Offset: 0x000517C1
	private static string GetServerListFolderPath(FileHelpers.FileSource fileSource)
	{
		return Utils.GetSaveDataPath(fileSource) + ServerList.GetServerListFolder(fileSource);
	}

	// Token: 0x06000992 RID: 2450 RVA: 0x000535D4 File Offset: 0x000517D4
	private static string GetFavoriteListFile(FileHelpers.FileSource fileSource)
	{
		return ServerList.GetServerListFolderPath(fileSource) + "favorite";
	}

	// Token: 0x06000993 RID: 2451 RVA: 0x000535E6 File Offset: 0x000517E6
	private static string GetRecentListFile(FileHelpers.FileSource fileSource)
	{
		return ServerList.GetServerListFolderPath(fileSource) + "recent";
	}

	// Token: 0x06000994 RID: 2452 RVA: 0x000535F8 File Offset: 0x000517F8
	private void Awake()
	{
		this.InitializeIfNot();
	}

	// Token: 0x06000995 RID: 2453 RVA: 0x00053600 File Offset: 0x00051800
	private void OnEnable()
	{
		if (ServerList.instance != null && ServerList.instance != this)
		{
			ZLog.LogError("More than one instance of ServerList!");
			return;
		}
		ServerList.instance = this;
		this.OnServerListTab();
	}

	// Token: 0x06000996 RID: 2454 RVA: 0x00053633 File Offset: 0x00051833
	private void OnApplicationQuit()
	{
		UnityEngine.Object.DestroyImmediate(this);
	}

	// Token: 0x06000997 RID: 2455 RVA: 0x0005363B File Offset: 0x0005183B
	private void OnDestroy()
	{
		if (ServerList.instance != this)
		{
			return;
		}
		ServerList.instance = null;
		this.FlushLocalServerLists();
	}

	// Token: 0x06000998 RID: 2456 RVA: 0x00053658 File Offset: 0x00051858
	private void Update()
	{
		if (this.m_addServerPanel.activeInHierarchy)
		{
			this.m_addServerConfirmButton.interactable = (this.m_addServerTextInput.text.Length > 0 && !this.isAwaitingServerAdd);
			this.m_addServerCancelButton.interactable = !this.isAwaitingServerAdd;
		}
		ServerListType serverListType = this.currentServerList;
		if (serverListType - ServerListType.favorite > 1)
		{
			if (serverListType - ServerListType.friends <= 1 && Time.timeAsDouble >= this.serverListLastUpdatedTime + 0.5)
			{
				this.UpdateMatchmakingServerList();
				this.UpdateServerCount();
			}
		}
		else if (Time.timeAsDouble >= this.serverListLastUpdatedTime + 0.5)
		{
			this.UpdateLocalServerListStatus();
			this.UpdateServerCount();
		}
		if (!base.GetComponent<UIGamePad>().IsBlocked())
		{
			this.UpdateGamepad();
			this.UpdateKeyboard();
		}
		this.m_serverRefreshButton.interactable = (Time.time - this.m_lastServerListRequesTime > 1f);
		if (this.buttonsOutdated)
		{
			this.buttonsOutdated = false;
			this.UpdateButtons();
		}
	}

	// Token: 0x06000999 RID: 2457 RVA: 0x00053758 File Offset: 0x00051958
	private void InitializeIfNot()
	{
		if (this.initialized)
		{
			return;
		}
		this.initialized = true;
		this.m_favoriteButton.onClick.AddListener(delegate()
		{
			this.OnFavoriteServerButton();
		});
		this.m_removeButton.onClick.AddListener(delegate()
		{
			this.OnRemoveServerButton();
		});
		this.m_upButton.onClick.AddListener(delegate()
		{
			this.OnMoveServerUpButton();
		});
		this.m_downButton.onClick.AddListener(delegate()
		{
			this.OnMoveServerDownButton();
		});
		this.m_filterInputField.onValueChanged.AddListener(delegate(string _)
		{
			this.OnServerFilterChanged(true);
		});
		this.m_addServerButton.gameObject.SetActive(true);
		if (PlayerPrefs.HasKey("LastIPJoined"))
		{
			PlayerPrefs.DeleteKey("LastIPJoined");
		}
		this.m_serverListBaseSize = this.m_serverListRoot.rect.height;
		this.OnServerListTab();
	}

	// Token: 0x0600099A RID: 2458 RVA: 0x00053848 File Offset: 0x00051A48
	public static uint[] FairSplit(uint[] entryCounts, uint maxEntries)
	{
		uint num = 0U;
		uint num2 = 0U;
		for (int i = 0; i < entryCounts.Length; i++)
		{
			num += entryCounts[i];
			if (entryCounts[i] > 0U)
			{
				num2 += 1U;
			}
		}
		if (num <= maxEntries)
		{
			return entryCounts;
		}
		uint[] array = new uint[entryCounts.Length];
		while (num2 > 0U)
		{
			uint num3 = maxEntries / num2;
			if (num3 <= 0U)
			{
				uint num4 = 0U;
				int num5 = 0;
				while ((long)num5 < (long)((ulong)maxEntries))
				{
					if (entryCounts[(int)num4] > 0U)
					{
						array[(int)num4] += 1U;
					}
					else
					{
						num5--;
					}
					num4 += 1U;
					num5++;
				}
				maxEntries = 0U;
				break;
			}
			for (int j = 0; j < entryCounts.Length; j++)
			{
				if (entryCounts[j] > 0U)
				{
					if (entryCounts[j] > num3)
					{
						array[j] += num3;
						maxEntries -= num3;
						entryCounts[j] -= num3;
					}
					else
					{
						array[j] += entryCounts[j];
						maxEntries -= entryCounts[j];
						entryCounts[j] = 0U;
						num2 -= 1U;
					}
				}
			}
		}
		return array;
	}

	// Token: 0x0600099B RID: 2459 RVA: 0x00053944 File Offset: 0x00051B44
	public void FilterList()
	{
		if (this.currentServerListIsLocal)
		{
			List<ServerStatus> list;
			if (this.currentServerList == ServerListType.favorite)
			{
				list = this.m_favoriteServerList;
			}
			else
			{
				if (this.currentServerList != ServerListType.recent)
				{
					ZLog.LogError("Can't filter invalid server list!");
					return;
				}
				list = this.m_recentServerList;
			}
			this.m_filteredList = new List<ServerStatus>();
			for (int i = 0; i < list.Count; i++)
			{
				if (this.m_filterInputField.text.Length <= 0 || list[i].m_joinData.m_serverName.ToLowerInvariant().Contains(this.m_filterInputField.text.ToLowerInvariant()))
				{
					this.m_filteredList.Add(list[i]);
				}
			}
			return;
		}
		List<ServerStatus> list2 = new List<ServerStatus>();
		if (this.currentServerList == ServerListType.community)
		{
			for (int j = 0; j < this.m_crossplayMatchmakingServerList.Count; j++)
			{
				if (this.m_filterInputField.text.Length <= 0 || this.m_crossplayMatchmakingServerList[j].m_joinData.m_serverName.ToLowerInvariant().Contains(this.m_filterInputField.text.ToLowerInvariant()))
				{
					list2.Add(this.m_crossplayMatchmakingServerList[j]);
				}
			}
		}
		uint[] array = ServerList.FairSplit(new uint[]
		{
			(uint)list2.Count,
			(uint)this.m_steamMatchmakingServerList.Count
		}, 200U);
		this.m_filteredList = new List<ServerStatus>();
		if (array[0] > 0U)
		{
			this.m_filteredList.AddRange(list2.GetRange(0, (int)array[0]));
		}
		if (array[1] > 0U)
		{
			int num = 0;
			while (num < this.m_steamMatchmakingServerList.Count && (long)this.m_filteredList.Count < 200L)
			{
				if (this.m_steamMatchmakingServerList[num].IsCrossplay)
				{
					bool flag = false;
					for (int k = 0; k < this.m_filteredList.Count; k++)
					{
						if (this.m_steamMatchmakingServerList[num].m_joinData == this.m_filteredList[k].m_joinData)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						this.m_filteredList.Add(this.m_steamMatchmakingServerList[num]);
					}
				}
				else
				{
					this.m_filteredList.Add(this.m_steamMatchmakingServerList[num]);
				}
				num++;
			}
		}
		this.m_filteredList.Sort((ServerStatus a, ServerStatus b) => a.m_joinData.m_serverName.CompareTo(b.m_joinData.m_serverName));
	}

	// Token: 0x0600099C RID: 2460 RVA: 0x00053BC8 File Offset: 0x00051DC8
	private void UpdateButtons()
	{
		int selectedServer = this.GetSelectedServer();
		bool flag = selectedServer >= 0;
		bool flag2 = false;
		if (flag)
		{
			for (int i = 0; i < this.m_favoriteServerList.Count; i++)
			{
				if (this.m_favoriteServerList[i].m_joinData == this.CurrentServerListFiltered[selectedServer].m_joinData)
				{
					flag2 = true;
					break;
				}
			}
		}
		switch (this.currentServerList)
		{
		case ServerListType.favorite:
			this.m_upButton.interactable = (flag && selectedServer != 0);
			this.m_downButton.interactable = (flag && selectedServer != this.CurrentServerListFiltered.Count - 1);
			this.m_removeButton.interactable = flag;
			this.m_favoriteButton.interactable = (flag && (this.m_removeButton == null || !this.m_removeButton.gameObject.activeSelf));
			break;
		case ServerListType.recent:
			this.m_favoriteButton.interactable = (flag && !flag2);
			this.m_removeButton.interactable = flag;
			break;
		case ServerListType.friends:
		case ServerListType.community:
			this.m_favoriteButton.interactable = (flag && !flag2);
			break;
		}
		this.m_joinGameButton.interactable = flag;
	}

	// Token: 0x0600099D RID: 2461 RVA: 0x00053D18 File Offset: 0x00051F18
	public void OnFavoriteServersTab()
	{
		this.InitializeIfNot();
		if (this.currentServerList == ServerListType.favorite)
		{
			return;
		}
		this.currentServerList = ServerListType.favorite;
		this.m_filterInputField.text = "";
		this.OnServerFilterChanged(false);
		if (this.m_doneInitialServerListRequest)
		{
			PlayerPrefs.SetInt("serverListTab", this.m_serverListTabHandler.GetActiveTab());
		}
		this.ResetListManipulationButtons();
		this.m_removeButton.gameObject.SetActive(true);
		this.UpdateLocalServerListStatus();
		this.UpdateLocalServerListSelection();
	}

	// Token: 0x0600099E RID: 2462 RVA: 0x00053D94 File Offset: 0x00051F94
	public void OnRecentServersTab()
	{
		this.InitializeIfNot();
		if (this.currentServerList == ServerListType.recent)
		{
			return;
		}
		this.currentServerList = ServerListType.recent;
		this.m_filterInputField.text = "";
		this.OnServerFilterChanged(false);
		if (this.m_doneInitialServerListRequest)
		{
			PlayerPrefs.SetInt("serverListTab", this.m_serverListTabHandler.GetActiveTab());
		}
		this.ResetListManipulationButtons();
		this.m_favoriteButton.gameObject.SetActive(true);
		this.UpdateLocalServerListStatus();
		this.UpdateLocalServerListSelection();
	}

	// Token: 0x0600099F RID: 2463 RVA: 0x00053E10 File Offset: 0x00052010
	public void OnFriendsServersTab()
	{
		this.InitializeIfNot();
		if (this.currentServerList == ServerListType.friends)
		{
			return;
		}
		this.currentServerList = ServerListType.friends;
		if (this.m_doneInitialServerListRequest)
		{
			PlayerPrefs.SetInt("serverListTab", this.m_serverListTabHandler.GetActiveTab());
		}
		this.ResetListManipulationButtons();
		this.m_favoriteButton.gameObject.SetActive(true);
		this.m_filterInputField.text = "";
		this.OnServerFilterChanged(false);
		this.UpdateMatchmakingServerList();
		this.UpdateServerListGui(true);
		this.UpdateServerCount();
	}

	// Token: 0x060009A0 RID: 2464 RVA: 0x00053E94 File Offset: 0x00052094
	public void OnCommunityServersTab()
	{
		this.InitializeIfNot();
		if (this.currentServerList == ServerListType.community)
		{
			return;
		}
		this.currentServerList = ServerListType.community;
		if (this.m_doneInitialServerListRequest)
		{
			PlayerPrefs.SetInt("serverListTab", this.m_serverListTabHandler.GetActiveTab());
		}
		this.ResetListManipulationButtons();
		this.m_favoriteButton.gameObject.SetActive(true);
		this.m_filterInputField.text = "";
		this.OnServerFilterChanged(false);
		this.UpdateMatchmakingServerList();
		this.UpdateServerListGui(true);
		this.UpdateServerCount();
	}

	// Token: 0x060009A1 RID: 2465 RVA: 0x00053F18 File Offset: 0x00052118
	public void OnFavoriteServerButton()
	{
		if ((this.m_removeButton == null || !this.m_removeButton.gameObject.activeSelf) && this.currentServerList == ServerListType.favorite)
		{
			this.OnRemoveServerButton();
			return;
		}
		int selectedServer = this.GetSelectedServer();
		ServerStatus serverStatus = this.CurrentServerListFiltered[selectedServer];
		ServerStatus item;
		if (this.m_allLoadedServerData.TryGetValue(serverStatus.m_joinData, out item))
		{
			this.m_favoriteServerList.Add(item);
		}
		else
		{
			this.m_favoriteServerList.Add(serverStatus);
			this.m_allLoadedServerData.Add(serverStatus.m_joinData, serverStatus);
		}
		this.SetButtonsOutdated();
	}

	// Token: 0x060009A2 RID: 2466 RVA: 0x00053FB0 File Offset: 0x000521B0
	public void OnRemoveServerButton()
	{
		int selectedServer = this.GetSelectedServer();
		UnifiedPopup.Push(new YesNoPopup("$menu_removeserver", CensorShittyWords.FilterUGC(this.CurrentServerListFiltered[selectedServer].m_joinData.m_serverName, UGCType.ServerName, default(PlatformUserID), 0L), delegate()
		{
			this.OnRemoveServerConfirm();
		}, delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x060009A3 RID: 2467 RVA: 0x00054028 File Offset: 0x00052228
	public void OnMoveServerUpButton()
	{
		List<ServerStatus> favoriteServerList = this.m_favoriteServerList;
		int selectedServer = this.GetSelectedServer();
		ServerStatus value = favoriteServerList[selectedServer - 1];
		favoriteServerList[selectedServer - 1] = favoriteServerList[selectedServer];
		favoriteServerList[selectedServer] = value;
		this.filteredListOutdated = true;
		this.UpdateServerListGui(true);
	}

	// Token: 0x060009A4 RID: 2468 RVA: 0x00054074 File Offset: 0x00052274
	public void OnMoveServerDownButton()
	{
		List<ServerStatus> favoriteServerList = this.m_favoriteServerList;
		int selectedServer = this.GetSelectedServer();
		ServerStatus value = favoriteServerList[selectedServer + 1];
		favoriteServerList[selectedServer + 1] = favoriteServerList[selectedServer];
		favoriteServerList[selectedServer] = value;
		this.filteredListOutdated = true;
		this.UpdateServerListGui(true);
	}

	// Token: 0x060009A5 RID: 2469 RVA: 0x000540C0 File Offset: 0x000522C0
	private void OnRemoveServerConfirm()
	{
		if (this.currentServerList == ServerListType.favorite)
		{
			List<ServerStatus> favoriteServerList = this.m_favoriteServerList;
			int selectedServer = this.GetSelectedServer();
			ServerStatus item = this.CurrentServerListFiltered[selectedServer];
			int index = favoriteServerList.IndexOf(item);
			favoriteServerList.RemoveAt(index);
			this.filteredListOutdated = true;
			if (this.CurrentServerListFiltered.Count <= 0 && this.m_filterInputField.text != "")
			{
				this.m_filterInputField.text = "";
				this.OnServerFilterChanged(false);
				this.m_startup.SetServerToJoin(null);
			}
			else
			{
				this.UpdateLocalServerListSelection();
				this.SetSelectedServer(selectedServer, true);
			}
			UnifiedPopup.Pop();
			return;
		}
		ZLog.LogError("Can't remove server from invalid list!");
	}

	// Token: 0x060009A6 RID: 2470 RVA: 0x00054174 File Offset: 0x00052374
	private void ResetListManipulationButtons()
	{
		this.m_favoriteButton.gameObject.SetActive(false);
		this.m_removeButton.gameObject.SetActive(false);
		this.m_favoriteButton.interactable = false;
		this.m_upButton.interactable = false;
		this.m_downButton.interactable = false;
		this.m_removeButton.interactable = false;
	}

	// Token: 0x060009A7 RID: 2471 RVA: 0x000541D3 File Offset: 0x000523D3
	private void SetButtonsOutdated()
	{
		this.buttonsOutdated = true;
	}

	// Token: 0x060009A8 RID: 2472 RVA: 0x000541DC File Offset: 0x000523DC
	private void UpdateServerListGui(bool centerSelection)
	{
		new List<ServerStatus>();
		List<ServerList.ServerListElement> list = new List<ServerList.ServerListElement>();
		Dictionary<ServerJoinData, ServerList.ServerListElement> dictionary = new Dictionary<ServerJoinData, ServerList.ServerListElement>();
		for (int i = 0; i < this.m_serverListElements.Count; i++)
		{
			ServerList.ServerListElement serverListElement;
			if (dictionary.TryGetValue(this.m_serverListElements[i].m_serverStatus.m_joinData, out serverListElement))
			{
				ZLog.LogWarning("Join data " + this.m_serverListElements[i].m_serverStatus.m_joinData.ToString() + " already has a server list element, even though duplicates are not allowed! Discarding this element.\nWhile this warning itself is fine, it might be an indication of a bug that may cause navigation issues in the server list.");
				UnityEngine.Object.Destroy(this.m_serverListElements[i].m_element);
			}
			else
			{
				dictionary.Add(this.m_serverListElements[i].m_serverStatus.m_joinData, this.m_serverListElements[i]);
			}
		}
		float num = 0f;
		for (int j = 0; j < this.CurrentServerListFiltered.Count; j++)
		{
			ServerList.ServerListElement serverListElement2;
			if (dictionary.ContainsKey(this.CurrentServerListFiltered[j].m_joinData))
			{
				serverListElement2 = dictionary[this.CurrentServerListFiltered[j].m_joinData];
				list.Add(serverListElement2);
				dictionary.Remove(this.CurrentServerListFiltered[j].m_joinData);
			}
			else
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_serverListElementSteamCrossplay, this.m_serverListRoot);
				gameObject.SetActive(true);
				serverListElement2 = new ServerList.ServerListElement(gameObject, this.CurrentServerListFiltered[j]);
				ServerStatus selectedStatus = this.CurrentServerListFiltered[j];
				serverListElement2.m_button.onClick.AddListener(delegate()
				{
					this.OnSelectedServer(selectedStatus);
				});
				list.Add(serverListElement2);
			}
			serverListElement2.m_rectTransform.anchoredPosition = new Vector2(0f, -num);
			num += serverListElement2.m_rectTransform.sizeDelta.y;
			ServerStatus serverStatus = this.CurrentServerListFiltered[j];
			serverListElement2.m_serverName.text = CensorShittyWords.FilterUGC(serverStatus.m_joinData.m_serverName, UGCType.ServerName, default(PlatformUserID), 0L);
			bool flag = serverStatus.m_modifiers != null && serverStatus.m_modifiers.Count > 0;
			serverListElement2.m_modifiers.text = (flag ? Localization.instance.Localize(ServerOptionsGUI.GetWorldModifierSummary(serverStatus.m_modifiers, true, ", ")) : "");
			string str = flag ? ServerOptionsGUI.GetWorldModifierSummary(serverStatus.m_modifiers, false, "\n") : "";
			string text = "";
			if (serverStatus.m_joinData is ServerJoinDataSteamUser)
			{
				if (flag)
				{
					text = str + "\n\n" + serverStatus.m_joinData.ToString() + "\n(Steam)";
				}
				else
				{
					text = "- \n\n" + serverStatus.m_joinData.ToString() + "\n(Steam)";
				}
			}
			if (serverStatus.m_joinData is ServerJoinDataPlayFabUser)
			{
				if (flag)
				{
					text = str + "\n\n(PlayFab)";
				}
				else
				{
					text = "- \n\n(PlayFab)";
				}
			}
			if (serverStatus.m_joinData is ServerJoinDataDedicated)
			{
				if (flag)
				{
					text = str + "\n\n" + serverStatus.m_joinData.ToString() + "\n(Dedicated)";
				}
				else
				{
					text = "- \n\n" + serverStatus.m_joinData.ToString() + "\n(Dedicated)";
				}
			}
			serverListElement2.m_tooltip.Set("$menu_serveroptions", text, this.m_tooltipAnchor, default(Vector2));
			if (serverStatus.PlatformRestriction == null || serverStatus.IsJoinable)
			{
				serverListElement2.m_version.text = serverStatus.m_gameVersion.ToString();
				if (serverStatus.OnlineStatus == OnlineStatus.Online)
				{
					serverListElement2.m_players.text = serverStatus.m_playerCount.ToString() + " / " + this.m_serverPlayerLimit.ToString();
				}
				else
				{
					serverListElement2.m_players.text = "";
				}
				switch (serverStatus.PingStatus)
				{
				case ServerPingStatus.NotStarted:
					serverListElement2.m_status.sprite = this.connectUnknown;
					break;
				case ServerPingStatus.AwaitingResponse:
					serverListElement2.m_status.sprite = this.connectTrying;
					break;
				case ServerPingStatus.Success:
					serverListElement2.m_status.sprite = this.connectSuccess;
					break;
				case ServerPingStatus.TimedOut:
				case ServerPingStatus.CouldNotReach:
				case ServerPingStatus.Unpingable:
					goto IL_451;
				default:
					goto IL_451;
				}
				IL_463:
				if (serverListElement2.m_crossplay != null)
				{
					if (serverStatus.IsCrossplay)
					{
						serverListElement2.m_crossplay.gameObject.SetActive(true);
					}
					else
					{
						serverListElement2.m_crossplay.gameObject.SetActive(false);
					}
				}
				serverListElement2.m_private.gameObject.SetActive(serverStatus.m_isPasswordProtected);
				goto IL_522;
				IL_451:
				serverListElement2.m_status.sprite = this.connectFailed;
				goto IL_463;
			}
			serverListElement2.m_version.text = "";
			serverListElement2.m_players.text = "";
			serverListElement2.m_status.sprite = this.connectFailed;
			if (serverListElement2.m_crossplay != null)
			{
				serverListElement2.m_crossplay.gameObject.SetActive(false);
			}
			serverListElement2.m_private.gameObject.SetActive(false);
			IL_522:
			bool flag2 = this.m_startup.HasServerToJoin() && this.m_startup.GetServerToJoin().Equals(serverStatus.m_joinData);
			if (flag2)
			{
				this.m_startup.SetServerToJoin(serverStatus);
			}
			serverListElement2.m_selected.gameObject.SetActive(flag2);
			if (centerSelection && flag2)
			{
				this.m_serverListEnsureVisible.CenterOnItem(serverListElement2.m_selected);
			}
		}
		foreach (KeyValuePair<ServerJoinData, ServerList.ServerListElement> keyValuePair in dictionary)
		{
			UnityEngine.Object.Destroy(keyValuePair.Value.m_element);
		}
		this.m_serverListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(num, this.m_serverListBaseSize));
		this.m_serverListElements = list;
		this.SetButtonsOutdated();
	}

	// Token: 0x060009A9 RID: 2473 RVA: 0x000547FC File Offset: 0x000529FC
	private void UpdateServerCount()
	{
		int num = 0;
		if (this.currentServerListIsLocal)
		{
			num += this.CurrentServerListFiltered.Count;
		}
		else
		{
			num += ZSteamMatchmaking.instance.GetTotalNrOfServers();
			num += this.m_crossplayMatchmakingServerList.Count;
		}
		int num2 = 0;
		for (int i = 0; i < this.CurrentServerListFiltered.Count; i++)
		{
			if (this.CurrentServerListFiltered[i].PingStatus != ServerPingStatus.NotStarted && this.CurrentServerListFiltered[i].PingStatus != ServerPingStatus.AwaitingResponse)
			{
				num2++;
			}
		}
		this.m_serverCount.text = num2.ToString() + " / " + num.ToString();
	}

	// Token: 0x060009AA RID: 2474 RVA: 0x000548A4 File Offset: 0x00052AA4
	private void OnSelectedServer(ServerStatus selected)
	{
		this.m_startup.SetServerToJoin(selected);
		this.UpdateServerListGui(false);
	}

	// Token: 0x060009AB RID: 2475 RVA: 0x000548BC File Offset: 0x00052ABC
	private void SetSelectedServer(int index, bool centerSelection)
	{
		if (this.CurrentServerListFiltered.Count == 0)
		{
			if (this.m_startup.HasServerToJoin())
			{
				ZLog.Log("Serverlist is empty, clearing selection");
			}
			this.ClearSelectedServer();
			return;
		}
		index = Mathf.Clamp(index, 0, this.CurrentServerListFiltered.Count - 1);
		this.m_startup.SetServerToJoin(this.CurrentServerListFiltered[index]);
		this.UpdateServerListGui(centerSelection);
	}

	// Token: 0x060009AC RID: 2476 RVA: 0x00054928 File Offset: 0x00052B28
	private int GetSelectedServer()
	{
		if (!this.m_startup.HasServerToJoin())
		{
			return -1;
		}
		for (int i = 0; i < this.CurrentServerListFiltered.Count; i++)
		{
			if (this.m_startup.GetServerToJoin() == this.CurrentServerListFiltered[i].m_joinData)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x060009AD RID: 2477 RVA: 0x00054980 File Offset: 0x00052B80
	private void ClearSelectedServer()
	{
		this.m_startup.SetServerToJoin(null);
		this.SetButtonsOutdated();
	}

	// Token: 0x060009AE RID: 2478 RVA: 0x00054994 File Offset: 0x00052B94
	private int FindSelectedServer(GameObject button)
	{
		for (int i = 0; i < this.m_serverListElements.Count; i++)
		{
			if (this.m_serverListElements[i].m_element == button)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x060009AF RID: 2479 RVA: 0x000549D4 File Offset: 0x00052BD4
	private void UpdateLocalServerListStatus()
	{
		this.serverListLastUpdatedTime = Time.timeAsDouble;
		List<ServerStatus> list;
		if (this.currentServerList == ServerListType.favorite)
		{
			list = this.m_favoriteServerList;
		}
		else
		{
			if (this.currentServerList != ServerListType.recent)
			{
				ZLog.LogError("Can't update status of invalid server list!");
				return;
			}
			list = this.m_recentServerList;
		}
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].PingStatus != ServerPingStatus.Success && list[i].PingStatus != ServerPingStatus.CouldNotReach)
			{
				if (list[i].PingStatus == ServerPingStatus.NotStarted)
				{
					list[i].Ping();
					flag = true;
				}
				if (list[i].PingStatus == ServerPingStatus.AwaitingResponse && list[i].TryGetResult())
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.UpdateServerListGui(false);
			this.UpdateServerCount();
		}
	}

	// Token: 0x060009B0 RID: 2480 RVA: 0x00054A98 File Offset: 0x00052C98
	private void UpdateMatchmakingServerList()
	{
		this.serverListLastUpdatedTime = Time.timeAsDouble;
		if (this.m_serverListRevision == ZSteamMatchmaking.instance.GetServerListRevision())
		{
			return;
		}
		this.m_serverListRevision = ZSteamMatchmaking.instance.GetServerListRevision();
		this.m_steamMatchmakingServerList.Clear();
		ZSteamMatchmaking.instance.GetServers(this.m_steamMatchmakingServerList);
		if (!this.currentServerListIsLocal && this.m_whenToSearchPlayFab >= 0f && this.m_whenToSearchPlayFab <= Time.time)
		{
			this.m_whenToSearchPlayFab = -1f;
			this.RequestPlayFabServerList();
		}
		bool flag = false;
		this.filteredListOutdated = true;
		for (int i = 0; i < this.CurrentServerListFiltered.Count; i++)
		{
			if (this.CurrentServerListFiltered[i].m_joinData == this.m_startup.GetServerToJoin())
			{
				flag = true;
				break;
			}
		}
		if (this.m_startup.HasServerToJoin() && !flag)
		{
			ZLog.Log("Serverlist does not contain selected server, clearing");
			if (this.CurrentServerListFiltered.Count > 0)
			{
				this.SetSelectedServer(0, true);
			}
			else
			{
				this.ClearSelectedServer();
			}
		}
		this.UpdateServerListGui(false);
		this.UpdateServerCount();
	}

	// Token: 0x060009B1 RID: 2481 RVA: 0x00054BAC File Offset: 0x00052DAC
	private void UpdateLocalServerListSelection()
	{
		if (this.GetSelectedServer() < 0)
		{
			this.ClearSelectedServer();
			this.UpdateServerListGui(true);
		}
	}

	// Token: 0x060009B2 RID: 2482 RVA: 0x00054BC4 File Offset: 0x00052DC4
	public void OnServerListTab()
	{
		if (PlayerPrefs.HasKey("publicfilter"))
		{
			PlayerPrefs.DeleteKey("publicfilter");
		}
		int @int = PlayerPrefs.GetInt("serverListTab", 0);
		this.m_serverListTabHandler.SetActiveTab(@int, false, true);
		if (!this.m_doneInitialServerListRequest)
		{
			this.m_doneInitialServerListRequest = true;
			this.RequestServerList();
		}
		this.UpdateServerListGui(true);
	}

	// Token: 0x060009B3 RID: 2483 RVA: 0x00054C1D File Offset: 0x00052E1D
	public void OnRefreshButton()
	{
		this.RequestServerList();
		this.UpdateServerListGui(true);
		this.UpdateServerCount();
	}

	// Token: 0x060009B4 RID: 2484 RVA: 0x00054C32 File Offset: 0x00052E32
	public static void Refresh()
	{
		if (ServerList.instance == null)
		{
			return;
		}
		ServerList.instance.OnRefreshButton();
	}

	// Token: 0x060009B5 RID: 2485 RVA: 0x00054C4C File Offset: 0x00052E4C
	public static void UpdateServerListGuiStatic()
	{
		if (ServerList.instance == null)
		{
			return;
		}
		ServerList.instance.UpdateServerListGui(false);
	}

	// Token: 0x060009B6 RID: 2486 RVA: 0x00054C67 File Offset: 0x00052E67
	private void RequestPlayFabServerListIfUnchangedIn(float time)
	{
		if (time < 0f)
		{
			this.m_whenToSearchPlayFab = -1f;
			this.RequestPlayFabServerList();
			return;
		}
		this.m_whenToSearchPlayFab = Time.time + time;
	}

	// Token: 0x060009B7 RID: 2487 RVA: 0x00054C90 File Offset: 0x00052E90
	private void RequestPlayFabServerList()
	{
		if (!PlayFabManager.IsLoggedIn)
		{
			this.m_playFabServerSearchQueued = true;
			if (PlayFabManager.instance != null)
			{
				PlayFabManager.instance.LoginFinished += delegate(LoginType loginType)
				{
					this.RequestPlayFabServerList();
				};
				return;
			}
		}
		else
		{
			if (this.m_playFabServerSearchOngoing)
			{
				this.m_playFabServerSearchQueued = true;
				return;
			}
			this.m_playFabServerSearchQueued = false;
			this.m_playFabServerSearchOngoing = true;
			this.m_crossplayMatchmakingServerList.Clear();
			ZPlayFabMatchmaking.ListServers(this.m_filterInputField.text, new ZPlayFabMatchmakingSuccessCallback(this.PlayFabServerFound), new ZPlayFabMatchmakingFailedCallback(this.PlayFabServerSearchDone), this.currentServerList == ServerListType.friends);
			ZLog.DevLog("PlayFab server search started!");
		}
	}

	// Token: 0x060009B8 RID: 2488 RVA: 0x00054D34 File Offset: 0x00052F34
	public void PlayFabServerFound(PlayFabMatchmakingServerData serverData)
	{
		MonoBehaviour.print("Found PlayFab server with name: " + serverData.serverName);
		if (this.PlayFabDisplayEntry(serverData))
		{
			PlayFabMatchmakingServerData playFabMatchmakingServerData;
			if (this.m_playFabTemporarySearchServerList.TryGetValue(serverData, out playFabMatchmakingServerData))
			{
				if (serverData.tickCreated > playFabMatchmakingServerData.tickCreated)
				{
					this.m_playFabTemporarySearchServerList.Remove(serverData);
					this.m_playFabTemporarySearchServerList.Add(serverData, serverData);
					return;
				}
			}
			else
			{
				this.m_playFabTemporarySearchServerList.Add(serverData, serverData);
			}
		}
	}

	// Token: 0x060009B9 RID: 2489 RVA: 0x00054DA5 File Offset: 0x00052FA5
	private bool PlayFabDisplayEntry(PlayFabMatchmakingServerData serverData)
	{
		return serverData != null && this.currentServerList == ServerListType.community;
	}

	// Token: 0x060009BA RID: 2490 RVA: 0x00054DB8 File Offset: 0x00052FB8
	public void PlayFabServerSearchDone(ZPLayFabMatchmakingFailReason failedReason)
	{
		ZLog.DevLog("PlayFab server search done!");
		if (this.m_playFabServerSearchQueued)
		{
			this.m_playFabServerSearchQueued = false;
			this.m_playFabServerSearchOngoing = true;
			ZPlayFabMatchmaking.ListServers(this.m_filterInputField.text, new ZPlayFabMatchmakingSuccessCallback(this.PlayFabServerFound), new ZPlayFabMatchmakingFailedCallback(this.PlayFabServerSearchDone), this.currentServerList == ServerListType.friends);
			ZLog.DevLog("PlayFab server search started!");
			return;
		}
		this.m_playFabServerSearchOngoing = false;
		if (this.currentServerList != ServerListType.friends)
		{
			this.m_crossplayMatchmakingServerList.Clear();
		}
		foreach (KeyValuePair<PlayFabMatchmakingServerData, PlayFabMatchmakingServerData> keyValuePair in this.m_playFabTemporarySearchServerList)
		{
			ServerStatus serverStatus;
			if (keyValuePair.Value.isDedicatedServer && !string.IsNullOrEmpty(keyValuePair.Value.serverIp))
			{
				ServerJoinDataDedicated serverJoinDataDedicated = new ServerJoinDataDedicated(keyValuePair.Value.serverIp);
				if (serverJoinDataDedicated.IsValid())
				{
					serverStatus = new ServerStatus(serverJoinDataDedicated);
				}
				else
				{
					ZLog.Log("Dedicated server with invalid IP address - fallback to PlayFab ID");
					serverStatus = new ServerStatus(new ServerJoinDataPlayFabUser(keyValuePair.Value.remotePlayerId));
				}
			}
			else
			{
				serverStatus = new ServerStatus(new ServerJoinDataPlayFabUser(keyValuePair.Value.remotePlayerId));
			}
			if (!keyValuePair.Value.gameVersion.IsValid())
			{
				ZLog.LogWarning("Failed to parse version string! Skipping server entry with name \"" + serverStatus.m_joinData.m_serverName + "\".");
			}
			else
			{
				Platform value = Platform.Unknown;
				if (keyValuePair.Value.gameVersion >= global::Version.FirstVersionWithPlatformRestriction)
				{
					value = keyValuePair.Value.platformRestriction;
				}
				serverStatus.UpdateStatus(OnlineStatus.Online, keyValuePair.Value.serverName, keyValuePair.Value.numPlayers, keyValuePair.Value.gameVersion, keyValuePair.Value.modifiers, keyValuePair.Value.networkVersion, keyValuePair.Value.havePassword, new Platform?(value), keyValuePair.Value.platformUserID, true);
				this.m_crossplayMatchmakingServerList.Add(serverStatus);
			}
		}
		this.m_playFabTemporarySearchServerList.Clear();
		this.filteredListOutdated = true;
	}

	// Token: 0x060009BB RID: 2491 RVA: 0x00054FF0 File Offset: 0x000531F0
	public void RequestServerList()
	{
		ZLog.DevLog("Request serverlist");
		if (!this.m_serverRefreshButton.interactable)
		{
			ZLog.DevLog("Server queue already running");
			return;
		}
		this.m_serverRefreshButton.interactable = false;
		this.m_lastServerListRequesTime = Time.time;
		this.m_steamMatchmakingServerList.Clear();
		ZSteamMatchmaking.instance.RequestServerlist();
		this.RequestPlayFabServerListIfUnchangedIn(0f);
		this.ReloadLocalServerLists();
		this.filteredListOutdated = true;
		if (this.currentServerListIsLocal)
		{
			this.UpdateLocalServerListStatus();
		}
	}

	// Token: 0x060009BC RID: 2492 RVA: 0x00055074 File Offset: 0x00053274
	private void ReloadLocalServerLists()
	{
		if (!this.m_localServerListsLoaded)
		{
			this.LoadServerListFromDisk(ServerListType.favorite, ref this.m_favoriteServerList);
			this.LoadServerListFromDisk(ServerListType.recent, ref this.m_recentServerList);
			this.m_localServerListsLoaded = true;
			return;
		}
		foreach (ServerStatus serverStatus in this.m_allLoadedServerData.Values)
		{
			serverStatus.Reset();
		}
	}

	// Token: 0x060009BD RID: 2493 RVA: 0x000550F8 File Offset: 0x000532F8
	public void FlushLocalServerLists()
	{
		if (!this.m_localServerListsLoaded)
		{
			return;
		}
		ServerList.SaveServerListToDisk(ServerListType.favorite, this.m_favoriteServerList);
		ServerList.SaveServerListToDisk(ServerListType.recent, this.m_recentServerList);
		this.m_favoriteServerList.Clear();
		this.m_recentServerList.Clear();
		this.m_allLoadedServerData.Clear();
		this.m_localServerListsLoaded = false;
		this.filteredListOutdated = true;
	}

	// Token: 0x060009BE RID: 2494 RVA: 0x00055158 File Offset: 0x00053358
	public void OnServerFilterChanged(bool isTyping = false)
	{
		ZSteamMatchmaking.instance.SetNameFilter(this.m_filterInputField.text);
		ZSteamMatchmaking.instance.SetFriendFilter(this.currentServerList == ServerListType.friends);
		if (!this.currentServerListIsLocal)
		{
			this.RequestPlayFabServerListIfUnchangedIn(isTyping ? 0.5f : 0f);
		}
		this.filteredListOutdated = true;
		if (this.currentServerListIsLocal)
		{
			this.UpdateServerListGui(false);
			this.UpdateServerCount();
		}
	}

	// Token: 0x060009BF RID: 2495 RVA: 0x000551C8 File Offset: 0x000533C8
	private void UpdateGamepad()
	{
		if (!ZInput.IsGamepadActive())
		{
			return;
		}
		if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
		{
			this.SetSelectedServer(this.GetSelectedServer() + 1, true);
		}
		if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
		{
			this.SetSelectedServer(this.GetSelectedServer() - 1, true);
		}
	}

	// Token: 0x060009C0 RID: 2496 RVA: 0x0005522C File Offset: 0x0005342C
	private void UpdateKeyboard()
	{
		if (ZInput.GetKeyDown(KeyCode.UpArrow, true))
		{
			this.SetSelectedServer(this.GetSelectedServer() - 1, true);
		}
		if (ZInput.GetKeyDown(KeyCode.DownArrow, true))
		{
			this.SetSelectedServer(this.GetSelectedServer() + 1, true);
		}
		int num = 0;
		num += (ZInput.GetKeyDown(KeyCode.W, true) ? -1 : 0);
		num += (ZInput.GetKeyDown(KeyCode.S, true) ? 1 : 0);
		int selectedServer = this.GetSelectedServer();
		if (num != 0 && !this.m_filterInputField.isFocused && this.m_favoriteServerList.Count == this.m_filteredList.Count && this.currentServerList == ServerListType.favorite && selectedServer >= 0 && selectedServer + num >= 0 && selectedServer + num < this.m_favoriteServerList.Count)
		{
			if (num > 0)
			{
				this.OnMoveServerDownButton();
				return;
			}
			this.OnMoveServerUpButton();
		}
	}

	// Token: 0x060009C1 RID: 2497 RVA: 0x000552F8 File Offset: 0x000534F8
	public static void AddToRecentServersList(ServerJoinData data)
	{
		if (ServerList.instance != null)
		{
			ServerList.instance.AddToRecentServersListCached(data);
			return;
		}
		if (data == null)
		{
			ZLog.LogError("Couldn't add server to server list, server data was null");
			return;
		}
		List<ServerJoinData> list = new List<ServerJoinData>();
		if (!ServerList.LoadServerListFromDisk(ServerListType.recent, ref list))
		{
			ZLog.Log("Server list doesn't exist yet");
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] == data)
			{
				list.RemoveAt(i);
				i--;
			}
		}
		list.Insert(0, data);
		int num = (ServerList.maxRecentServers > 0) ? Mathf.Max(list.Count - ServerList.maxRecentServers, 0) : 0;
		for (int j = 0; j < num; j++)
		{
			list.RemoveAt(list.Count - 1);
		}
		ServerList.SaveStatusCode saveStatusCode = ServerList.SaveServerListToDisk(ServerListType.recent, list);
		if (saveStatusCode == ServerList.SaveStatusCode.Succeess)
		{
			ZLog.Log("Added server with name " + data.m_serverName + " to server list");
			return;
		}
		switch (saveStatusCode)
		{
		case ServerList.SaveStatusCode.UnsupportedServerListType:
			ZLog.LogError("Couln't add server with name " + data.m_serverName + " to server list, tried to save an unsupported server list type");
			return;
		case ServerList.SaveStatusCode.UnknownServerBackend:
			ZLog.LogError("Couln't add server with name " + data.m_serverName + " to server list, tried to save a server entry with an unknown server backend");
			return;
		case ServerList.SaveStatusCode.CloudQuotaExceeded:
			ZLog.LogWarning("Couln't add server with name " + data.m_serverName + " to server list, cloud quota exceeded.");
			return;
		default:
			ZLog.LogError("Couln't add server with name " + data.m_serverName + " to server list, unknown issue when saving to disk");
			return;
		}
	}

	// Token: 0x060009C2 RID: 2498 RVA: 0x00055468 File Offset: 0x00053668
	private void AddToRecentServersListCached(ServerJoinData data)
	{
		if (data == null)
		{
			ZLog.LogError("Couldn't add server to server list, server data was null");
			return;
		}
		ServerStatus serverStatus = null;
		for (int i = 0; i < this.m_recentServerList.Count; i++)
		{
			if (this.m_recentServerList[i].m_joinData == data)
			{
				serverStatus = this.m_recentServerList[i];
				this.m_recentServerList.RemoveAt(i);
				i--;
			}
		}
		if (serverStatus == null)
		{
			ServerStatus item;
			if (this.m_allLoadedServerData.TryGetValue(data, out item))
			{
				this.m_recentServerList.Insert(0, item);
			}
			else
			{
				ServerStatus serverStatus2 = new ServerStatus(data);
				this.m_allLoadedServerData.Add(data, serverStatus2);
				this.m_recentServerList.Insert(0, serverStatus2);
			}
		}
		else
		{
			this.m_recentServerList.Insert(0, serverStatus);
		}
		int num = (ServerList.maxRecentServers > 0) ? Mathf.Max(this.m_recentServerList.Count - ServerList.maxRecentServers, 0) : 0;
		for (int j = 0; j < num; j++)
		{
			this.m_recentServerList.RemoveAt(this.m_recentServerList.Count - 1);
		}
		ZLog.Log("Added server with name " + data.m_serverName + " to server list");
	}

	// Token: 0x060009C3 RID: 2499 RVA: 0x00055594 File Offset: 0x00053794
	public bool LoadServerListFromDisk(ServerListType listType, ref List<ServerStatus> list)
	{
		List<ServerJoinData> list2 = new List<ServerJoinData>();
		if (!ServerList.LoadServerListFromDisk(listType, ref list2))
		{
			return false;
		}
		list.Clear();
		for (int i = 0; i < list2.Count; i++)
		{
			ServerStatus item;
			if (this.m_allLoadedServerData.TryGetValue(list2[i], out item))
			{
				list.Add(item);
			}
			else
			{
				ServerStatus serverStatus = new ServerStatus(list2[i]);
				this.m_allLoadedServerData.Add(list2[i], serverStatus);
				list.Add(serverStatus);
			}
		}
		return true;
	}

	// Token: 0x060009C4 RID: 2500 RVA: 0x00055614 File Offset: 0x00053814
	private static List<ServerList.StorageLocation> GetServerListFileLocations(ServerListType listType)
	{
		List<ServerList.StorageLocation> list = new List<ServerList.StorageLocation>();
		switch (listType)
		{
		case ServerListType.favorite:
			list.Add(new ServerList.StorageLocation(ServerList.GetFavoriteListFile(FileHelpers.FileSource.Local), FileHelpers.FileSource.Local));
			if (FileHelpers.CloudStorageEnabled)
			{
				list.Add(new ServerList.StorageLocation(ServerList.GetFavoriteListFile(FileHelpers.FileSource.Cloud), FileHelpers.FileSource.Cloud));
				return list;
			}
			return list;
		case ServerListType.recent:
			list.Add(new ServerList.StorageLocation(ServerList.GetRecentListFile(FileHelpers.FileSource.Local), FileHelpers.FileSource.Local));
			if (FileHelpers.CloudStorageEnabled)
			{
				list.Add(new ServerList.StorageLocation(ServerList.GetRecentListFile(FileHelpers.FileSource.Cloud), FileHelpers.FileSource.Cloud));
				return list;
			}
			return list;
		}
		return null;
	}

	// Token: 0x060009C5 RID: 2501 RVA: 0x000556A0 File Offset: 0x000538A0
	private static bool LoadUniqueServerListEntriesIntoList(ServerList.StorageLocation location, ref List<ServerJoinData> joinData)
	{
		HashSet<ServerJoinData> hashSet = new HashSet<ServerJoinData>();
		for (int i = 0; i < joinData.Count; i++)
		{
			hashSet.Add(joinData[i]);
		}
		FileReader fileReader;
		try
		{
			fileReader = new FileReader(location.path, location.source, FileHelpers.FileHelperType.Binary);
		}
		catch (Exception ex)
		{
			ZLog.Log(string.Concat(new string[]
			{
				"Failed to load: ",
				location.path,
				" (",
				ex.Message,
				")"
			}));
			return false;
		}
		byte[] data;
		try
		{
			BinaryReader binary = fileReader.m_binary;
			int count = binary.ReadInt32();
			data = binary.ReadBytes(count);
		}
		catch (Exception ex2)
		{
			ZLog.LogError(string.Format("error loading player.dat. Source: {0}, Path: {1}, Error: {2}", location.source, location.path, ex2.Message));
			fileReader.Dispose();
			return false;
		}
		fileReader.Dispose();
		ZPackage zpackage = new ZPackage(data);
		try
		{
			uint num = zpackage.ReadUInt();
			if (num != 0U && num != 1U)
			{
				ZLog.LogError("Couldn't read list of version " + num.ToString());
				return false;
			}
			int num2 = zpackage.ReadInt();
			for (int j = 0; j < num2; j++)
			{
				string a = zpackage.ReadString();
				string serverName = zpackage.ReadString();
				ServerJoinData serverJoinData;
				if (!(a == "Steam user"))
				{
					if (!(a == "PlayFab user"))
					{
						if (!(a == "Dedicated"))
						{
							ZLog.LogError("Unsupported backend! This should be an impossible code path if the server list was saved and loaded properly.");
							return false;
						}
						serverJoinData = ((num == 0U) ? new ServerJoinDataDedicated(zpackage.ReadUInt(), (ushort)zpackage.ReadUInt()) : new ServerJoinDataDedicated(zpackage.ReadString(), (ushort)zpackage.ReadUInt()));
					}
					else
					{
						serverJoinData = new ServerJoinDataPlayFabUser(zpackage.ReadString());
					}
				}
				else
				{
					serverJoinData = new ServerJoinDataSteamUser(zpackage.ReadULong());
				}
				if (serverJoinData != null)
				{
					serverJoinData.m_serverName = serverName;
					if (!hashSet.Contains(serverJoinData))
					{
						joinData.Add(serverJoinData);
					}
				}
			}
		}
		catch (EndOfStreamException ex3)
		{
			ZLog.LogWarning(string.Format("Something is wrong with the server list at path {0} and source {1}, reached the end of the stream unexpectedly! Entries that have successfully been read so far have been added to the server list. \n", location.path, location.source) + ex3.StackTrace);
		}
		return true;
	}

	// Token: 0x060009C6 RID: 2502 RVA: 0x00055928 File Offset: 0x00053B28
	public static bool LoadServerListFromDisk(ServerListType listType, ref List<ServerJoinData> destination)
	{
		List<ServerList.StorageLocation> serverListFileLocations = ServerList.GetServerListFileLocations(listType);
		if (serverListFileLocations == null)
		{
			ZLog.LogError("Can't load a server list of unsupported type");
			return false;
		}
		for (int i = 0; i < serverListFileLocations.Count; i++)
		{
			if (!FileHelpers.Exists(serverListFileLocations[i].path, serverListFileLocations[i].source))
			{
				serverListFileLocations.RemoveAt(i);
				i--;
			}
		}
		if (serverListFileLocations.Count <= 0)
		{
			ZLog.Log("No list saved! Aborting load operation");
			return false;
		}
		SortedList<DateTime, List<ServerList.StorageLocation>> sortedList = new SortedList<DateTime, List<ServerList.StorageLocation>>();
		for (int j = 0; j < serverListFileLocations.Count; j++)
		{
			DateTime lastWriteTime = FileHelpers.GetLastWriteTime(serverListFileLocations[j].path, serverListFileLocations[j].source);
			if (sortedList.ContainsKey(lastWriteTime))
			{
				sortedList[lastWriteTime].Add(serverListFileLocations[j]);
			}
			else
			{
				sortedList.Add(lastWriteTime, new List<ServerList.StorageLocation>
				{
					serverListFileLocations[j]
				});
			}
		}
		List<ServerJoinData> list = new List<ServerJoinData>();
		for (int k = sortedList.Count - 1; k >= 0; k--)
		{
			for (int l = 0; l < sortedList.Values[k].Count; l++)
			{
				if (!ServerList.LoadUniqueServerListEntriesIntoList(sortedList.Values[k][l], ref list))
				{
					ZLog.Log("Failed to load list entries! Aborting load operation.");
					return false;
				}
			}
		}
		destination = list;
		return true;
	}

	// Token: 0x060009C7 RID: 2503 RVA: 0x00055A84 File Offset: 0x00053C84
	public static ServerList.SaveStatusCode SaveServerListToDisk(ServerListType listType, List<ServerStatus> list)
	{
		List<ServerJoinData> list2 = new List<ServerJoinData>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			list2.Add(list[i].m_joinData);
		}
		return ServerList.SaveServerListToDisk(listType, list2);
	}

	// Token: 0x060009C8 RID: 2504 RVA: 0x00055AC8 File Offset: 0x00053CC8
	private static ServerList.SaveStatusCode SaveServerListEntries(ServerList.StorageLocation location, List<ServerJoinData> list)
	{
		string oldFile = location.path + ".old";
		string text = location.path + ".new";
		ZPackage zpackage = new ZPackage();
		zpackage.Write(1U);
		zpackage.Write(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			ServerJoinData serverJoinData = list[i];
			zpackage.Write(serverJoinData.GetDataName());
			zpackage.Write(serverJoinData.m_serverName);
			string dataName = serverJoinData.GetDataName();
			if (!(dataName == "Steam user"))
			{
				if (!(dataName == "PlayFab user"))
				{
					if (!(dataName == "Dedicated"))
					{
						ZLog.LogError("Unsupported backend! Aborting save operation.");
						return ServerList.SaveStatusCode.UnknownServerBackend;
					}
					zpackage.Write(((serverJoinData as ServerJoinDataDedicated).m_host == null) ? "" : (serverJoinData as ServerJoinDataDedicated).m_host);
					zpackage.Write((uint)(serverJoinData as ServerJoinDataDedicated).m_port);
				}
				else
				{
					zpackage.Write((serverJoinData as ServerJoinDataPlayFabUser).m_remotePlayerId.ToString());
				}
			}
			else
			{
				zpackage.Write((ulong)(serverJoinData as ServerJoinDataSteamUser).m_joinUserID);
			}
		}
		if (FileHelpers.CloudStorageEnabled && location.source == FileHelpers.FileSource.Cloud)
		{
			ulong num = 0UL;
			if (FileHelpers.FileExistsCloud(location.path))
			{
				num += FileHelpers.GetFileSize(location.path, location.source);
			}
			num = Math.Max((ulong)(4L + (long)zpackage.Size()), num);
			num *= 2UL;
			if (FileHelpers.OperationExceedsCloudCapacity(num))
			{
				ZLog.LogWarning("Saving server list to cloud would exceed the cloud storage quota. Therefore the operation has been aborted!");
				return ServerList.SaveStatusCode.CloudQuotaExceeded;
			}
		}
		byte[] array = zpackage.GetArray();
		FileWriter fileWriter = new FileWriter(text, FileHelpers.FileHelperType.Binary, location.source);
		fileWriter.m_binary.Write(array.Length);
		fileWriter.m_binary.Write(array);
		fileWriter.Finish();
		FileHelpers.ReplaceOldFile(location.path, text, oldFile, location.source);
		return ServerList.SaveStatusCode.Succeess;
	}

	// Token: 0x060009C9 RID: 2505 RVA: 0x00055CAC File Offset: 0x00053EAC
	public static ServerList.SaveStatusCode SaveServerListToDisk(ServerListType listType, List<ServerJoinData> list)
	{
		List<ServerList.StorageLocation> serverListFileLocations = ServerList.GetServerListFileLocations(listType);
		if (serverListFileLocations == null)
		{
			ZLog.LogError("Can't save a server list of unsupported type");
			return ServerList.SaveStatusCode.UnsupportedServerListType;
		}
		bool flag = false;
		bool flag2 = false;
		int i = 0;
		while (i < serverListFileLocations.Count)
		{
			switch (ServerList.SaveServerListEntries(serverListFileLocations[i], list))
			{
			case ServerList.SaveStatusCode.Succeess:
				flag = true;
				break;
			case ServerList.SaveStatusCode.UnsupportedServerListType:
				goto IL_4E;
			case ServerList.SaveStatusCode.UnknownServerBackend:
				break;
			case ServerList.SaveStatusCode.CloudQuotaExceeded:
				flag2 = true;
				break;
			default:
				goto IL_4E;
			}
			IL_58:
			i++;
			continue;
			IL_4E:
			ZLog.LogError("Unknown error when saving server list");
			goto IL_58;
		}
		if (flag)
		{
			return ServerList.SaveStatusCode.Succeess;
		}
		if (flag2)
		{
			return ServerList.SaveStatusCode.CloudQuotaExceeded;
		}
		return ServerList.SaveStatusCode.FailedUnknownReason;
	}

	// Token: 0x060009CA RID: 2506 RVA: 0x00055D29 File Offset: 0x00053F29
	public void OnAddServerOpen()
	{
		if (this.m_filterInputField.isFocused)
		{
			return;
		}
		this.m_addServerPanel.SetActive(true);
	}

	// Token: 0x060009CB RID: 2507 RVA: 0x00055D45 File Offset: 0x00053F45
	public void OnAddServerClose()
	{
		this.m_addServerPanel.SetActive(false);
	}

	// Token: 0x060009CC RID: 2508 RVA: 0x00055D54 File Offset: 0x00053F54
	public void OnAddServer()
	{
		this.m_addServerPanel.SetActive(true);
		string text = this.m_addServerTextInput.text;
		string[] array = text.Split(':', StringSplitOptions.None);
		if (array.Length == 0)
		{
			return;
		}
		if (array.Length == 1)
		{
			string text2 = array[0];
			if (ZPlayFabMatchmaking.IsJoinCode(text2))
			{
				if (PlayFabManager.IsLoggedIn)
				{
					this.OnManualAddToFavoritesStart();
					ZPlayFabMatchmaking.ResolveJoinCode(text2, new ZPlayFabMatchmakingSuccessCallback(this.OnPlayFabJoinCodeSuccess), new ZPlayFabMatchmakingFailedCallback(this.OnJoinCodeFailed));
					return;
				}
				this.OnJoinCodeFailed(ZPLayFabMatchmakingFailReason.NotLoggedIn);
				return;
			}
		}
		string value;
		ushort num;
		ServerJoinDataDedicated.GetAddressAndPortFromString(text, out value, out num);
		if (!string.IsNullOrEmpty(value))
		{
			ServerJoinDataDedicated newServerListEntryDedicated = new ServerJoinDataDedicated(text);
			this.OnManualAddToFavoritesStart();
			newServerListEntryDedicated.IsValidAsync(delegate(bool result)
			{
				if (result)
				{
					this.OnManualAddToFavoritesSuccess(new ServerStatus(newServerListEntryDedicated));
					return;
				}
				if (newServerListEntryDedicated.IsURL)
				{
					UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfaileddnslookup", delegate()
					{
						UnifiedPopup.Pop();
					}, true));
				}
				else
				{
					UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedincorrectformatting", delegate()
					{
						UnifiedPopup.Pop();
					}, true));
				}
				this.isAwaitingServerAdd = false;
			});
			return;
		}
		UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedincorrectformatting", delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x060009CD RID: 2509 RVA: 0x00055E4E File Offset: 0x0005404E
	private void OnManualAddToFavoritesStart()
	{
		this.isAwaitingServerAdd = true;
	}

	// Token: 0x060009CE RID: 2510 RVA: 0x00055E58 File Offset: 0x00054058
	private void OnManualAddToFavoritesSuccess(ServerStatus newServerListEntry)
	{
		ServerStatus serverStatus = null;
		for (int i = 0; i < this.m_favoriteServerList.Count; i++)
		{
			if (this.m_favoriteServerList[i].m_joinData == newServerListEntry.m_joinData)
			{
				serverStatus = this.m_favoriteServerList[i];
				break;
			}
		}
		if (serverStatus == null)
		{
			serverStatus = newServerListEntry;
			ServerStatus item;
			if (this.m_allLoadedServerData.TryGetValue(serverStatus.m_joinData, out item))
			{
				this.m_favoriteServerList.Add(item);
			}
			else
			{
				this.m_favoriteServerList.Add(serverStatus);
				this.m_allLoadedServerData.Add(serverStatus.m_joinData, serverStatus);
			}
			this.filteredListOutdated = true;
		}
		this.m_serverListTabHandler.SetActiveTab(0, false, true);
		this.m_startup.SetServerToJoin(serverStatus);
		this.SetSelectedServer(this.GetSelectedServer(), true);
		this.OnAddServerClose();
		this.m_addServerTextInput.text = "";
		this.isAwaitingServerAdd = false;
	}

	// Token: 0x060009CF RID: 2511 RVA: 0x00055F3C File Offset: 0x0005413C
	private void OnPlayFabJoinCodeSuccess(PlayFabMatchmakingServerData serverData)
	{
		if (serverData == null || serverData.networkVersion != 34U)
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$error_incompatibleversion", delegate()
			{
				UnifiedPopup.Pop();
			}, true));
			this.isAwaitingServerAdd = false;
			return;
		}
		if (serverData.platformRestriction != Platform.Unknown && serverData.platformRestriction != PlatformManager.DistributionPlatform.Platform)
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$error_platformexcluded", delegate()
			{
				UnifiedPopup.Pop();
			}, true));
			this.isAwaitingServerAdd = false;
			return;
		}
		if (serverData.platformRestriction != PlatformManager.DistributionPlatform.Platform && PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) != PrivilegeResult.Granted)
		{
			if (PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege != null)
			{
				PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.CrossPlatformMultiplayer);
			}
			else
			{
				UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$xbox_error_crossplayprivilege", delegate()
				{
					UnifiedPopup.Pop();
				}, true));
			}
			this.isAwaitingServerAdd = false;
			return;
		}
		ZPlayFabMatchmaking.JoinCode = serverData.joinCode;
		ServerJoinData joinData;
		if (serverData.isDedicatedServer && !string.IsNullOrEmpty(serverData.serverIp))
		{
			joinData = new ServerJoinDataDedicated(serverData.serverIp);
		}
		else
		{
			joinData = new ServerJoinDataPlayFabUser(serverData.remotePlayerId);
		}
		ServerStatus serverStatus = new ServerStatus(joinData);
		serverStatus.UpdateStatus(OnlineStatus.Online, serverData.serverName, serverData.numPlayers, serverData.gameVersion, serverData.modifiers, serverData.networkVersion, serverData.havePassword, new Platform?(serverData.platformRestriction), serverData.platformUserID, true);
		this.OnManualAddToFavoritesSuccess(serverStatus);
	}

	// Token: 0x060009D0 RID: 2512 RVA: 0x0005610C File Offset: 0x0005430C
	private void OnJoinCodeFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		ZLog.Log("Failed to resolve join code for the following reason: " + failReason.ToString());
		this.isAwaitingServerAdd = false;
		UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedresolvejoincode", delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x04000B36 RID: 2870
	private static ServerList instance = null;

	// Token: 0x04000B37 RID: 2871
	private ServerListType currentServerList;

	// Token: 0x04000B38 RID: 2872
	[SerializeField]
	private Button m_favoriteButton;

	// Token: 0x04000B39 RID: 2873
	[SerializeField]
	private Button m_removeButton;

	// Token: 0x04000B3A RID: 2874
	[SerializeField]
	private Button m_upButton;

	// Token: 0x04000B3B RID: 2875
	[SerializeField]
	private Button m_downButton;

	// Token: 0x04000B3C RID: 2876
	[SerializeField]
	private FejdStartup m_startup;

	// Token: 0x04000B3D RID: 2877
	[SerializeField]
	private Sprite connectUnknown;

	// Token: 0x04000B3E RID: 2878
	[SerializeField]
	private Sprite connectTrying;

	// Token: 0x04000B3F RID: 2879
	[SerializeField]
	private Sprite connectSuccess;

	// Token: 0x04000B40 RID: 2880
	[SerializeField]
	private Sprite connectFailed;

	// Token: 0x04000B41 RID: 2881
	[Header("Join")]
	public float m_serverListElementStep = 32f;

	// Token: 0x04000B42 RID: 2882
	public RectTransform m_serverListRoot;

	// Token: 0x04000B43 RID: 2883
	public GameObject m_serverListElementSteamCrossplay;

	// Token: 0x04000B44 RID: 2884
	public GameObject m_serverListElement;

	// Token: 0x04000B45 RID: 2885
	public ScrollRectEnsureVisible m_serverListEnsureVisible;

	// Token: 0x04000B46 RID: 2886
	public Button m_serverRefreshButton;

	// Token: 0x04000B47 RID: 2887
	public TextMeshProUGUI m_serverCount;

	// Token: 0x04000B48 RID: 2888
	public int m_serverPlayerLimit = 10;

	// Token: 0x04000B49 RID: 2889
	public GuiInputField m_filterInputField;

	// Token: 0x04000B4A RID: 2890
	public RectTransform m_tooltipAnchor;

	// Token: 0x04000B4B RID: 2891
	public Button m_addServerButton;

	// Token: 0x04000B4C RID: 2892
	public GameObject m_addServerPanel;

	// Token: 0x04000B4D RID: 2893
	public Button m_addServerConfirmButton;

	// Token: 0x04000B4E RID: 2894
	public Button m_addServerCancelButton;

	// Token: 0x04000B4F RID: 2895
	public GuiInputField m_addServerTextInput;

	// Token: 0x04000B50 RID: 2896
	public TabHandler m_serverListTabHandler;

	// Token: 0x04000B51 RID: 2897
	private bool isAwaitingServerAdd;

	// Token: 0x04000B52 RID: 2898
	public Button m_joinGameButton;

	// Token: 0x04000B53 RID: 2899
	private float m_serverListBaseSize;

	// Token: 0x04000B54 RID: 2900
	private int m_serverListRevision = -1;

	// Token: 0x04000B55 RID: 2901
	private float m_lastServerListRequesTime = -999f;

	// Token: 0x04000B56 RID: 2902
	private bool m_doneInitialServerListRequest;

	// Token: 0x04000B57 RID: 2903
	private bool buttonsOutdated = true;

	// Token: 0x04000B58 RID: 2904
	private bool initialized;

	// Token: 0x04000B59 RID: 2905
	private static int maxRecentServers = 11;

	// Token: 0x04000B5A RID: 2906
	private List<ServerStatus> m_steamMatchmakingServerList = new List<ServerStatus>();

	// Token: 0x04000B5B RID: 2907
	private readonly List<ServerStatus> m_crossplayMatchmakingServerList = new List<ServerStatus>();

	// Token: 0x04000B5C RID: 2908
	private bool m_localServerListsLoaded;

	// Token: 0x04000B5D RID: 2909
	private Dictionary<ServerJoinData, ServerStatus> m_allLoadedServerData = new Dictionary<ServerJoinData, ServerStatus>();

	// Token: 0x04000B5E RID: 2910
	private List<ServerStatus> m_recentServerList = new List<ServerStatus>();

	// Token: 0x04000B5F RID: 2911
	private List<ServerStatus> m_favoriteServerList = new List<ServerStatus>();

	// Token: 0x04000B60 RID: 2912
	private bool filteredListOutdated;

	// Token: 0x04000B61 RID: 2913
	private List<ServerStatus> m_filteredList = new List<ServerStatus>();

	// Token: 0x04000B62 RID: 2914
	private List<ServerList.ServerListElement> m_serverListElements = new List<ServerList.ServerListElement>();

	// Token: 0x04000B63 RID: 2915
	private double serverListLastUpdatedTime;

	// Token: 0x04000B64 RID: 2916
	private bool m_playFabServerSearchOngoing;

	// Token: 0x04000B65 RID: 2917
	private bool m_playFabServerSearchQueued;

	// Token: 0x04000B66 RID: 2918
	private readonly Dictionary<PlayFabMatchmakingServerData, PlayFabMatchmakingServerData> m_playFabTemporarySearchServerList = new Dictionary<PlayFabMatchmakingServerData, PlayFabMatchmakingServerData>();

	// Token: 0x04000B67 RID: 2919
	private float m_whenToSearchPlayFab = -1f;

	// Token: 0x04000B68 RID: 2920
	private const uint serverListVersion = 1U;

	// Token: 0x020002B5 RID: 693
	private class ServerListElement
	{
		// Token: 0x060020C7 RID: 8391 RVA: 0x000E9514 File Offset: 0x000E7714
		public ServerListElement(GameObject element, ServerStatus serverStatus)
		{
			this.m_element = element;
			this.m_serverStatus = serverStatus;
			this.m_button = this.m_element.GetComponent<Button>();
			this.m_rectTransform = (this.m_element.transform as RectTransform);
			this.m_serverName = this.m_element.GetComponentInChildren<TMP_Text>();
			this.m_modifiers = this.m_element.transform.Find("modifiers").GetComponent<TMP_Text>();
			this.m_tooltip = this.m_element.GetComponentInChildren<UITooltip>();
			this.m_version = this.m_element.transform.Find("version").GetComponent<TMP_Text>();
			this.m_players = this.m_element.transform.Find("players").GetComponent<TMP_Text>();
			this.m_status = this.m_element.transform.Find("status").GetComponent<Image>();
			this.m_crossplay = this.m_element.transform.Find("crossplay");
			this.m_private = this.m_element.transform.Find("Private");
			this.m_selected = (this.m_element.transform.Find("selected") as RectTransform);
		}

		// Token: 0x0400228F RID: 8847
		public GameObject m_element;

		// Token: 0x04002290 RID: 8848
		public ServerStatus m_serverStatus;

		// Token: 0x04002291 RID: 8849
		public Button m_button;

		// Token: 0x04002292 RID: 8850
		public RectTransform m_rectTransform;

		// Token: 0x04002293 RID: 8851
		public TMP_Text m_serverName;

		// Token: 0x04002294 RID: 8852
		public TMP_Text m_modifiers;

		// Token: 0x04002295 RID: 8853
		public UITooltip m_tooltip;

		// Token: 0x04002296 RID: 8854
		public TMP_Text m_version;

		// Token: 0x04002297 RID: 8855
		public TMP_Text m_players;

		// Token: 0x04002298 RID: 8856
		public Image m_status;

		// Token: 0x04002299 RID: 8857
		public Transform m_crossplay;

		// Token: 0x0400229A RID: 8858
		public Transform m_private;

		// Token: 0x0400229B RID: 8859
		public RectTransform m_selected;
	}

	// Token: 0x020002B6 RID: 694
	private struct StorageLocation
	{
		// Token: 0x060020C8 RID: 8392 RVA: 0x000E9654 File Offset: 0x000E7854
		public StorageLocation(string path, FileHelpers.FileSource source)
		{
			this.path = path;
			this.source = source;
		}

		// Token: 0x0400229C RID: 8860
		public string path;

		// Token: 0x0400229D RID: 8861
		public FileHelpers.FileSource source;
	}

	// Token: 0x020002B7 RID: 695
	public enum SaveStatusCode
	{
		// Token: 0x0400229F RID: 8863
		Succeess,
		// Token: 0x040022A0 RID: 8864
		UnsupportedServerListType,
		// Token: 0x040022A1 RID: 8865
		UnknownServerBackend,
		// Token: 0x040022A2 RID: 8866
		CloudQuotaExceeded,
		// Token: 0x040022A3 RID: 8867
		FailedUnknownReason
	}
}
