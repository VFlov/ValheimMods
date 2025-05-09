using System;
using System.Collections.Generic;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000071 RID: 113
public class ConnectPanel : MonoBehaviour
{
	// Token: 0x17000024 RID: 36
	// (get) Token: 0x0600072F RID: 1839 RVA: 0x0003B1B1 File Offset: 0x000393B1
	public static ConnectPanel instance
	{
		get
		{
			return ConnectPanel.m_instance;
		}
	}

	// Token: 0x06000730 RID: 1840 RVA: 0x0003B1B8 File Offset: 0x000393B8
	private void Start()
	{
		ConnectPanel.m_instance = this;
		this.m_root.gameObject.SetActive(false);
		this.m_playerListBaseSize = this.m_playerList.rect.height;
	}

	// Token: 0x06000731 RID: 1841 RVA: 0x0003B1F5 File Offset: 0x000393F5
	public static bool IsVisible()
	{
		return ConnectPanel.m_instance && ConnectPanel.m_instance.m_root.gameObject.activeSelf;
	}

	// Token: 0x06000732 RID: 1842 RVA: 0x0003B21C File Offset: 0x0003941C
	private void Update()
	{
		if (ZInput.GetKeyDown(KeyCode.F2, true) || (ZInput.GetButton("JoyLTrigger") && ZInput.GetButton("JoyLBumper") && ZInput.GetButtonDown("JoyBack")))
		{
			this.m_root.gameObject.SetActive(!this.m_root.gameObject.activeSelf);
		}
		if (this.m_root.gameObject.activeInHierarchy)
		{
			if (!ZNet.instance.IsServer() && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
			{
				this.m_serverField.gameObject.SetActive(true);
				this.m_serverField.text = ZNet.GetServerString(true);
			}
			else
			{
				this.m_serverField.gameObject.SetActive(false);
			}
			this.m_worldField.text = ZNet.instance.GetWorldName();
			this.UpdateFps();
			this.m_serverOptions.text = Localization.instance.Localize(Game.m_serverOptionsSummary);
			this.m_myPort.gameObject.SetActive(ZNet.instance.IsServer());
			this.m_myPort.text = ZNet.instance.GetHostPort().ToString();
			this.m_myUID.text = ZNet.GetUID().ToString();
			if (ZDOMan.instance != null)
			{
				this.m_zdos.text = ZDOMan.instance.NrOfObjects().ToString();
				float num;
				float num2;
				ZDOMan.instance.GetAverageStats(out num, out num2);
				this.m_zdosSent.text = num.ToString("0.0");
				this.m_zdosRecv.text = num2.ToString("0.0");
				this.m_activePeers.text = ZNet.instance.GetNrOfPlayers().ToString();
			}
			this.m_zdosPool.text = string.Concat(new string[]
			{
				ZDOPool.GetPoolActive().ToString(),
				" / ",
				ZDOPool.GetPoolSize().ToString(),
				" / ",
				ZDOPool.GetPoolTotal().ToString()
			});
			if (ZNetScene.instance)
			{
				this.m_zdosInstances.text = ZNetScene.instance.NrOfInstances().ToString();
			}
			float num3;
			float num4;
			int num5;
			float num6;
			float num7;
			ZNet.instance.GetNetStats(out num3, out num4, out num5, out num6, out num7);
			this.m_dataSent.text = (num6 / 1024f).ToString("0.0") + "kb/s";
			this.m_dataRecv.text = (num7 / 1024f).ToString("0.0") + "kb/s";
			this.m_ping.text = num5.ToString("0") + "ms";
			this.m_quality.text = ((int)(num3 * 100f)).ToString() + "% / " + ((int)(num4 * 100f)).ToString() + "%";
			this.m_clientSendQueue.text = ZDOMan.instance.GetClientChangeQueue().ToString();
			this.m_nrOfConnections.text = ZNet.instance.GetPeerConnections().ToString();
			string text = "";
			foreach (ZNetPeer znetPeer in ZNet.instance.GetConnectedPeers())
			{
				if (znetPeer.IsReady())
				{
					text = string.Concat(new string[]
					{
						text,
						znetPeer.m_socket.GetEndPointString(),
						" UID: ",
						znetPeer.m_uid.ToString(),
						"\n"
					});
				}
				else
				{
					text = text + znetPeer.m_socket.GetEndPointString() + " connecting \n";
				}
			}
			this.m_connections.text = text;
			List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
			float num8 = 16f;
			if (playerList.Count != this.m_playerListElements.Count)
			{
				foreach (GameObject obj in this.m_playerListElements)
				{
					UnityEngine.Object.Destroy(obj);
				}
				this.m_playerListElements.Clear();
				for (int i = 0; i < playerList.Count; i++)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_playerElement, this.m_playerList);
					(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * -num8);
					this.m_playerListElements.Add(gameObject);
				}
				float num9 = (float)playerList.Count * num8;
				num9 = Mathf.Max(this.m_playerListBaseSize, num9);
				this.m_playerList.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num9);
				this.m_playerListScroll.value = 1f;
			}
			for (int j = 0; j < playerList.Count; j++)
			{
				ZNet.PlayerInfo playerInfo = playerList[j];
				TMP_Text component = this.m_playerListElements[j].transform.Find("name").GetComponent<TMP_Text>();
				TMP_Text component2 = this.m_playerListElements[j].transform.Find("hostname").GetComponent<TMP_Text>();
				Component component3 = this.m_playerListElements[j].transform.Find("KickButton").GetComponent<Button>();
				component.text = CensorShittyWords.FilterUGC(playerInfo.m_name, UGCType.CharacterName, playerInfo.m_userInfo.m_id, 0L);
				component2.text = playerInfo.m_userInfo.m_id.ToString();
				component3.gameObject.SetActive(false);
			}
			this.m_connectButton.interactable = this.ValidHost();
		}
	}

	// Token: 0x06000733 RID: 1843 RVA: 0x0003B814 File Offset: 0x00039A14
	private void UpdateFps()
	{
		this.m_frameTimer += Time.deltaTime;
		this.m_frameSamples++;
		if (this.m_frameTimer > 1f)
		{
			float num = this.m_frameTimer / (float)this.m_frameSamples;
			this.m_fps.text = (1f / num).ToString("0.0");
			this.m_frameTime.text = "( " + (num * 1000f).ToString("00.0") + "ms )";
			this.m_frameSamples = 0;
			this.m_frameTimer = 0f;
		}
	}

	// Token: 0x06000734 RID: 1844 RVA: 0x0003B8BC File Offset: 0x00039ABC
	private bool ValidHost()
	{
		int num = 0;
		try
		{
			num = int.Parse(this.m_hostPort.text);
		}
		catch
		{
			return false;
		}
		return !string.IsNullOrEmpty(this.m_hostName.text) && num != 0;
	}

	// Token: 0x0400085C RID: 2140
	private static ConnectPanel m_instance;

	// Token: 0x0400085D RID: 2141
	public Transform m_root;

	// Token: 0x0400085E RID: 2142
	public TMP_Text m_serverField;

	// Token: 0x0400085F RID: 2143
	public TMP_Text m_worldField;

	// Token: 0x04000860 RID: 2144
	public TMP_Text m_statusField;

	// Token: 0x04000861 RID: 2145
	public TMP_Text m_connections;

	// Token: 0x04000862 RID: 2146
	public RectTransform m_playerList;

	// Token: 0x04000863 RID: 2147
	public Scrollbar m_playerListScroll;

	// Token: 0x04000864 RID: 2148
	public GameObject m_playerElement;

	// Token: 0x04000865 RID: 2149
	public GuiInputField m_hostName;

	// Token: 0x04000866 RID: 2150
	public GuiInputField m_hostPort;

	// Token: 0x04000867 RID: 2151
	public Button m_connectButton;

	// Token: 0x04000868 RID: 2152
	public TMP_Text m_myPort;

	// Token: 0x04000869 RID: 2153
	public TMP_Text m_myUID;

	// Token: 0x0400086A RID: 2154
	public TMP_Text m_knownHosts;

	// Token: 0x0400086B RID: 2155
	public TMP_Text m_nrOfConnections;

	// Token: 0x0400086C RID: 2156
	public TMP_Text m_pendingConnections;

	// Token: 0x0400086D RID: 2157
	public Toggle m_autoConnect;

	// Token: 0x0400086E RID: 2158
	public TMP_Text m_zdos;

	// Token: 0x0400086F RID: 2159
	public TMP_Text m_zdosPool;

	// Token: 0x04000870 RID: 2160
	public TMP_Text m_zdosSent;

	// Token: 0x04000871 RID: 2161
	public TMP_Text m_zdosRecv;

	// Token: 0x04000872 RID: 2162
	public TMP_Text m_zdosInstances;

	// Token: 0x04000873 RID: 2163
	public TMP_Text m_activePeers;

	// Token: 0x04000874 RID: 2164
	public TMP_Text m_ntp;

	// Token: 0x04000875 RID: 2165
	public TMP_Text m_upnp;

	// Token: 0x04000876 RID: 2166
	public TMP_Text m_dataSent;

	// Token: 0x04000877 RID: 2167
	public TMP_Text m_dataRecv;

	// Token: 0x04000878 RID: 2168
	public TMP_Text m_clientSendQueue;

	// Token: 0x04000879 RID: 2169
	public TMP_Text m_fps;

	// Token: 0x0400087A RID: 2170
	public TMP_Text m_frameTime;

	// Token: 0x0400087B RID: 2171
	public TMP_Text m_ping;

	// Token: 0x0400087C RID: 2172
	public TMP_Text m_quality;

	// Token: 0x0400087D RID: 2173
	private float m_playerListBaseSize;

	// Token: 0x0400087E RID: 2174
	private List<GameObject> m_playerListElements = new List<GameObject>();

	// Token: 0x0400087F RID: 2175
	public TMP_Text m_serverOptions;

	// Token: 0x04000880 RID: 2176
	private int m_frameSamples;

	// Token: 0x04000881 RID: 2177
	private float m_frameTimer;
}
