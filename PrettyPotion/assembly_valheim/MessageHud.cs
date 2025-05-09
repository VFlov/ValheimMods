using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000086 RID: 134
public class MessageHud : MonoBehaviour
{
	// Token: 0x060008E6 RID: 2278 RVA: 0x0004D4C9 File Offset: 0x0004B6C9
	private void Awake()
	{
		MessageHud.m_instance = this;
	}

	// Token: 0x060008E7 RID: 2279 RVA: 0x0004D4D1 File Offset: 0x0004B6D1
	private void OnDestroy()
	{
		MessageHud.m_instance = null;
	}

	// Token: 0x1700003C RID: 60
	// (get) Token: 0x060008E8 RID: 2280 RVA: 0x0004D4D9 File Offset: 0x0004B6D9
	public static MessageHud instance
	{
		get
		{
			return MessageHud.m_instance;
		}
	}

	// Token: 0x060008E9 RID: 2281 RVA: 0x0004D4E0 File Offset: 0x0004B6E0
	private void Start()
	{
		this.m_messageText.CrossFadeAlpha(0f, 0f, true);
		this.m_messageIcon.canvasRenderer.SetAlpha(0f);
		this.m_messageCenterText.CrossFadeAlpha(0f, 0f, true);
		for (int i = 0; i < this.m_maxUnlockMessages; i++)
		{
			this.m_unlockMessages.Add(null);
		}
		ZRoutedRpc.instance.Register<int, string>("ShowMessage", new Action<long, int, string>(this.RPC_ShowMessage));
	}

	// Token: 0x060008EA RID: 2282 RVA: 0x0004D566 File Offset: 0x0004B766
	private void Update()
	{
		if (Hud.IsUserHidden() && !this.m_showDespiteHiddenHUD)
		{
			this.HideAll();
			return;
		}
		this.UpdateUnlockMsg(Time.deltaTime);
		this.UpdateMessage(Time.deltaTime);
		this.UpdateBiomeFound(Time.deltaTime);
	}

	// Token: 0x060008EB RID: 2283 RVA: 0x0004D5A0 File Offset: 0x0004B7A0
	private void HideAll()
	{
		for (int i = 0; i < this.m_maxUnlockMessages; i++)
		{
			if (this.m_unlockMessages[i] != null)
			{
				UnityEngine.Object.Destroy(this.m_unlockMessages[i]);
				this.m_unlockMessages[i] = null;
			}
		}
		this.m_messageText.CrossFadeAlpha(0f, 0f, true);
		this.m_messageIcon.canvasRenderer.SetAlpha(0f);
		this.m_messageCenterText.CrossFadeAlpha(0f, 0f, true);
		if (this.m_biomeMsgInstance)
		{
			UnityEngine.Object.Destroy(this.m_biomeMsgInstance);
			this.m_biomeMsgInstance = null;
		}
	}

	// Token: 0x060008EC RID: 2284 RVA: 0x0004D650 File Offset: 0x0004B850
	public void MessageAll(MessageHud.MessageType type, string text)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", new object[]
		{
			(int)type,
			text
		});
	}

	// Token: 0x060008ED RID: 2285 RVA: 0x0004D679 File Offset: 0x0004B879
	private void RPC_ShowMessage(long sender, int type, string text)
	{
		this.ShowMessage((MessageHud.MessageType)type, text, 0, null, false);
	}

	// Token: 0x060008EE RID: 2286 RVA: 0x0004D688 File Offset: 0x0004B888
	public void ShowMessage(MessageHud.MessageType type, string text, int amount = 0, Sprite icon = null, bool showDespiteHiddenHUD = false)
	{
		this.m_showDespiteHiddenHUD = showDespiteHiddenHUD;
		if (Hud.IsUserHidden() && !showDespiteHiddenHUD)
		{
			return;
		}
		text = Localization.instance.Localize(text);
		if (type == MessageHud.MessageType.TopLeft)
		{
			MessageHud.MsgData msgData = new MessageHud.MsgData();
			msgData.m_icon = icon;
			msgData.m_text = text;
			msgData.m_amount = amount;
			this.m_msgQeue.Enqueue(msgData);
			this.AddLog(text);
			return;
		}
		if (type != MessageHud.MessageType.Center)
		{
			return;
		}
		this.m_messageCenterText.text = text;
		this._crossFadeTextBuffer.Add(new MessageHud.CrossFadeText
		{
			text = this.m_messageCenterText,
			alpha = 1f,
			time = 0f
		});
		this._crossFadeTextBuffer.Add(new MessageHud.CrossFadeText
		{
			text = this.m_messageCenterText,
			alpha = 0f,
			time = 4f
		});
	}

	// Token: 0x060008EF RID: 2287 RVA: 0x0004D770 File Offset: 0x0004B970
	private void UpdateMessage(float dt)
	{
		if ((double)dt > 0.5)
		{
			return;
		}
		if (this._crossFadeTextBuffer.Count > 0)
		{
			MessageHud.CrossFadeText crossFadeText = this._crossFadeTextBuffer[0];
			this._crossFadeTextBuffer.RemoveAt(0);
			crossFadeText.text.CrossFadeAlpha(crossFadeText.alpha, crossFadeText.time, true);
		}
		this.m_msgQueueTimer += dt;
		if (this.m_msgQeue.Count > 0)
		{
			MessageHud.MsgData msgData = this.m_msgQeue.Peek();
			bool flag = this.m_msgQueueTimer < 4f && msgData.m_text == this.currentMsg.m_text && msgData.m_icon == this.currentMsg.m_icon;
			if (this.m_msgQueueTimer >= 1f || flag)
			{
				MessageHud.MsgData msgData2 = this.m_msgQeue.Dequeue();
				this.m_messageText.text = msgData2.m_text;
				if (flag)
				{
					msgData2.m_amount += this.currentMsg.m_amount;
				}
				if (msgData2.m_amount > 1)
				{
					TMP_Text messageText = this.m_messageText;
					messageText.text = messageText.text + " x" + msgData2.m_amount.ToString();
				}
				this._crossFadeTextBuffer.Add(new MessageHud.CrossFadeText
				{
					text = this.m_messageText,
					alpha = 1f,
					time = 0f
				});
				this._crossFadeTextBuffer.Add(new MessageHud.CrossFadeText
				{
					text = this.m_messageText,
					alpha = 0f,
					time = 4f
				});
				if (msgData2.m_icon != null)
				{
					this.m_messageIcon.sprite = msgData2.m_icon;
					this.m_messageIcon.canvasRenderer.SetAlpha(1f);
					this.m_messageIcon.CrossFadeAlpha(0f, 4f, true);
				}
				else
				{
					this.m_messageIcon.canvasRenderer.SetAlpha(0f);
				}
				this.currentMsg = msgData2;
				this.m_msgQueueTimer = 0f;
			}
		}
	}

	// Token: 0x060008F0 RID: 2288 RVA: 0x0004D99C File Offset: 0x0004BB9C
	private void UpdateBiomeFound(float dt)
	{
		if (this.m_biomeMsgInstance != null && this.m_biomeMsgInstance.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsTag("done"))
		{
			UnityEngine.Object.Destroy(this.m_biomeMsgInstance);
			this.m_biomeMsgInstance = null;
		}
		if (this.m_biomeFoundQueue.Count > 0 && this.m_biomeMsgInstance == null && this.m_msgQeue.Count == 0 && this.m_msgQueueTimer > 2f)
		{
			MessageHud.BiomeMessage biomeMessage = this.m_biomeFoundQueue.Dequeue();
			this.m_biomeMsgInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_biomeFoundPrefab, base.transform);
			TMP_Text component = Utils.FindChild(this.m_biomeMsgInstance.transform, "Title", Utils.IterativeSearchType.DepthFirst).GetComponent<TMP_Text>();
			string text = Localization.instance.Localize(biomeMessage.m_text);
			component.text = text;
			if (biomeMessage.m_playStinger && this.m_biomeFoundStinger)
			{
				UnityEngine.Object.Instantiate<GameObject>(this.m_biomeFoundStinger);
			}
		}
	}

	// Token: 0x060008F1 RID: 2289 RVA: 0x0004DAA0 File Offset: 0x0004BCA0
	public void ShowBiomeFoundMsg(string text, bool playStinger)
	{
		MessageHud.BiomeMessage biomeMessage = new MessageHud.BiomeMessage();
		biomeMessage.m_text = text;
		biomeMessage.m_playStinger = playStinger;
		this.m_biomeFoundQueue.Enqueue(biomeMessage);
	}

	// Token: 0x060008F2 RID: 2290 RVA: 0x0004DAD0 File Offset: 0x0004BCD0
	public void QueueUnlockMsg(Sprite icon, string topic, string description)
	{
		MessageHud.UnlockMsg unlockMsg = new MessageHud.UnlockMsg();
		unlockMsg.m_icon = icon;
		unlockMsg.m_topic = Localization.instance.Localize(topic);
		unlockMsg.m_description = Localization.instance.Localize(description);
		this.m_unlockMsgQueue.Enqueue(unlockMsg);
		this.m_unlockMsgCount++;
		this.AddLog(topic + ": " + description);
		ZLog.Log("Queue unlock msg:" + topic + ":" + description);
	}

	// Token: 0x060008F3 RID: 2291 RVA: 0x0004DB50 File Offset: 0x0004BD50
	private int GetFreeUnlockMsgSlot()
	{
		for (int i = 0; i < this.m_unlockMessages.Count; i++)
		{
			if (this.m_unlockMessages[i] == null)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x060008F4 RID: 2292 RVA: 0x0004DB8C File Offset: 0x0004BD8C
	private void UpdateUnlockMsg(float dt)
	{
		for (int i = 0; i < this.m_unlockMessages.Count; i++)
		{
			GameObject gameObject = this.m_unlockMessages[i];
			if (!(gameObject == null) && gameObject.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsTag("done"))
			{
				UnityEngine.Object.Destroy(gameObject);
				this.m_unlockMessages[i] = null;
				break;
			}
		}
		if (this.m_unlockMsgQueue.Count > 0)
		{
			int freeUnlockMsgSlot = this.GetFreeUnlockMsgSlot();
			if (freeUnlockMsgSlot != -1)
			{
				Transform transform = base.transform;
				GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.m_unlockMsgPrefab, transform);
				this.m_unlockMessages[freeUnlockMsgSlot] = gameObject2;
				RectTransform rectTransform = gameObject2.transform as RectTransform;
				Vector3 v = rectTransform.anchoredPosition;
				v.y -= (float)(this.m_maxUnlockMsgSpace * freeUnlockMsgSlot);
				rectTransform.anchoredPosition = v;
				MessageHud.UnlockMsg unlockMsg = this.m_unlockMsgQueue.Dequeue();
				Image component = rectTransform.Find("UnlockMessage/icon_bkg/UnlockIcon").GetComponent<Image>();
				TMP_Text component2 = rectTransform.Find("UnlockMessage/UnlockTitle").GetComponent<TMP_Text>();
				TMP_Text component3 = rectTransform.Find("UnlockMessage/UnlockDescription").GetComponent<TMP_Text>();
				component.sprite = unlockMsg.m_icon;
				component2.text = unlockMsg.m_topic;
				component3.text = unlockMsg.m_description;
				return;
			}
		}
		else if (this.m_unlockMsgCount > 0)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format("{0} $inventory_logs_new", this.m_unlockMsgCount), 0, null);
			this.m_unlockMsgCount = 0;
		}
	}

	// Token: 0x060008F5 RID: 2293 RVA: 0x0004DD0E File Offset: 0x0004BF0E
	private void AddLog(string logText)
	{
		this.m_messageLog.Add(logText);
		while (this.m_messageLog.Count > this.m_maxLogMessages)
		{
			this.m_messageLog.RemoveAt(0);
		}
	}

	// Token: 0x060008F6 RID: 2294 RVA: 0x0004DD3D File Offset: 0x0004BF3D
	public List<string> GetLog()
	{
		return this.m_messageLog;
	}

	// Token: 0x04000A84 RID: 2692
	private MessageHud.MsgData currentMsg = new MessageHud.MsgData();

	// Token: 0x04000A85 RID: 2693
	private static MessageHud m_instance;

	// Token: 0x04000A86 RID: 2694
	public TMP_Text m_messageText;

	// Token: 0x04000A87 RID: 2695
	public Image m_messageIcon;

	// Token: 0x04000A88 RID: 2696
	public TMP_Text m_messageCenterText;

	// Token: 0x04000A89 RID: 2697
	public GameObject m_unlockMsgPrefab;

	// Token: 0x04000A8A RID: 2698
	public int m_maxUnlockMsgSpace = 110;

	// Token: 0x04000A8B RID: 2699
	public int m_maxUnlockMessages = 4;

	// Token: 0x04000A8C RID: 2700
	public int m_maxLogMessages = 50;

	// Token: 0x04000A8D RID: 2701
	public GameObject m_biomeFoundPrefab;

	// Token: 0x04000A8E RID: 2702
	public GameObject m_biomeFoundStinger;

	// Token: 0x04000A8F RID: 2703
	private Queue<MessageHud.BiomeMessage> m_biomeFoundQueue = new Queue<MessageHud.BiomeMessage>();

	// Token: 0x04000A90 RID: 2704
	private List<string> m_messageLog = new List<string>();

	// Token: 0x04000A91 RID: 2705
	private List<GameObject> m_unlockMessages = new List<GameObject>();

	// Token: 0x04000A92 RID: 2706
	private Queue<MessageHud.UnlockMsg> m_unlockMsgQueue = new Queue<MessageHud.UnlockMsg>();

	// Token: 0x04000A93 RID: 2707
	private Queue<MessageHud.MsgData> m_msgQeue = new Queue<MessageHud.MsgData>();

	// Token: 0x04000A94 RID: 2708
	private float m_msgQueueTimer = -1f;

	// Token: 0x04000A95 RID: 2709
	private int m_unlockMsgCount;

	// Token: 0x04000A96 RID: 2710
	private bool m_showDespiteHiddenHUD;

	// Token: 0x04000A97 RID: 2711
	private GameObject m_biomeMsgInstance;

	// Token: 0x04000A98 RID: 2712
	private List<MessageHud.CrossFadeText> _crossFadeTextBuffer = new List<MessageHud.CrossFadeText>();

	// Token: 0x020002A6 RID: 678
	public enum MessageType
	{
		// Token: 0x04002245 RID: 8773
		TopLeft = 1,
		// Token: 0x04002246 RID: 8774
		Center
	}

	// Token: 0x020002A7 RID: 679
	private class UnlockMsg
	{
		// Token: 0x04002247 RID: 8775
		public Sprite m_icon;

		// Token: 0x04002248 RID: 8776
		public string m_topic;

		// Token: 0x04002249 RID: 8777
		public string m_description;
	}

	// Token: 0x020002A8 RID: 680
	private class MsgData
	{
		// Token: 0x0400224A RID: 8778
		public Sprite m_icon;

		// Token: 0x0400224B RID: 8779
		public string m_text;

		// Token: 0x0400224C RID: 8780
		public int m_amount;
	}

	// Token: 0x020002A9 RID: 681
	private class BiomeMessage
	{
		// Token: 0x0400224D RID: 8781
		public string m_text;

		// Token: 0x0400224E RID: 8782
		public bool m_playStinger;
	}

	// Token: 0x020002AA RID: 682
	private struct CrossFadeText
	{
		// Token: 0x0400224F RID: 8783
		public TMP_Text text;

		// Token: 0x04002250 RID: 8784
		public float alpha;

		// Token: 0x04002251 RID: 8785
		public float time;
	}
}
