using System;
using TMPro;
using UnityEngine;

// Token: 0x0200009A RID: 154
public class TextViewer : MonoBehaviour
{
	// Token: 0x06000A4E RID: 2638 RVA: 0x00059D70 File Offset: 0x00057F70
	private void Awake()
	{
		TextViewer.m_instance = this;
		this.m_root.SetActive(true);
		this.m_introRoot.SetActive(true);
		this.m_ravenRoot.SetActive(true);
		this.m_animator = this.m_root.GetComponent<Animator>();
		this.m_animatorIntro = this.m_introRoot.GetComponent<Animator>();
		this.m_animatorRaven = this.m_ravenRoot.GetComponent<Animator>();
	}

	// Token: 0x06000A4F RID: 2639 RVA: 0x00059DDA File Offset: 0x00057FDA
	private void OnDestroy()
	{
		TextViewer.m_instance = null;
	}

	// Token: 0x17000045 RID: 69
	// (get) Token: 0x06000A50 RID: 2640 RVA: 0x00059DE2 File Offset: 0x00057FE2
	public static TextViewer instance
	{
		get
		{
			return TextViewer.m_instance;
		}
	}

	// Token: 0x06000A51 RID: 2641 RVA: 0x00059DEC File Offset: 0x00057FEC
	private void LateUpdate()
	{
		if (!this.IsVisible())
		{
			return;
		}
		this.m_showTime += Time.deltaTime;
		if (this.m_showTime > 0.2f)
		{
			if (this.m_autoHide && Player.m_localPlayer && Vector3.Distance(Player.m_localPlayer.transform.position, this.m_openPlayerPos) > 3f)
			{
				this.Hide();
			}
			if (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse") || ZInput.GetKeyDown(KeyCode.Escape, true))
			{
				this.Hide();
			}
		}
	}

	// Token: 0x06000A52 RID: 2642 RVA: 0x00059E84 File Offset: 0x00058084
	public void ShowText(TextViewer.Style style, string topic, string textId, bool autoHide)
	{
		if (Player.m_localPlayer == null && autoHide)
		{
			return;
		}
		topic = Localization.instance.Localize(topic);
		string text = Localization.instance.Localize(textId);
		if (style == TextViewer.Style.Rune)
		{
			this.m_topic.text = topic;
			this.m_text.text = text;
			this.m_runeText.text = Localization.instance.TranslateSingleId(textId, "English");
			this.m_animator.SetBool(TextViewer.s_visibleID, true);
		}
		else if (style == TextViewer.Style.Intro)
		{
			this.m_introTopic.text = topic;
			this.m_introText.text = text;
			this.m_animatorIntro.gameObject.SetActive(true);
			this.m_animatorIntro.SetTrigger("play");
			ZLog.Log("Show intro " + Time.frameCount.ToString());
		}
		else if (style == TextViewer.Style.Raven)
		{
			this.m_ravenTopic.text = topic;
			this.m_ravenText.text = text;
			this.m_animatorRaven.SetBool(TextViewer.s_visibleID, true);
		}
		this.m_autoHide = autoHide;
		if (this.m_autoHide)
		{
			this.m_openPlayerPos = Player.m_localPlayer.transform.position;
		}
		this.m_showTime = 0f;
		ZLog.Log("Show text " + topic + ":" + text);
	}

	// Token: 0x06000A53 RID: 2643 RVA: 0x00059FD5 File Offset: 0x000581D5
	public void Hide()
	{
		this.m_autoHide = false;
		this.m_animator.SetBool(TextViewer.s_visibleID, false);
		this.m_animatorRaven.SetBool(TextViewer.s_visibleID, false);
	}

	// Token: 0x06000A54 RID: 2644 RVA: 0x0005A000 File Offset: 0x00058200
	public void HideIntro()
	{
		this.m_animatorIntro.gameObject.SetActive(false);
	}

	// Token: 0x06000A55 RID: 2645 RVA: 0x0005A014 File Offset: 0x00058214
	public bool IsVisible()
	{
		return TextViewer.m_instance.m_animatorIntro.GetCurrentAnimatorStateInfo(0).tagHash == TextViewer.s_animatorTagVisible || this.m_animator.GetBool(TextViewer.s_visibleID) || this.m_animatorIntro.GetBool(TextViewer.s_visibleID) || this.m_animatorRaven.GetBool(TextViewer.s_visibleID);
	}

	// Token: 0x06000A56 RID: 2646 RVA: 0x0005A078 File Offset: 0x00058278
	public static bool IsShowingIntro()
	{
		return TextViewer.m_instance != null && TextViewer.m_instance.m_animatorIntro.GetCurrentAnimatorStateInfo(0).tagHash == TextViewer.s_animatorTagVisible;
	}

	// Token: 0x04000BD3 RID: 3027
	private static TextViewer m_instance;

	// Token: 0x04000BD4 RID: 3028
	private Animator m_animator;

	// Token: 0x04000BD5 RID: 3029
	private Animator m_animatorIntro;

	// Token: 0x04000BD6 RID: 3030
	private Animator m_animatorRaven;

	// Token: 0x04000BD7 RID: 3031
	[Header("Rune")]
	public GameObject m_root;

	// Token: 0x04000BD8 RID: 3032
	public TMP_Text m_topic;

	// Token: 0x04000BD9 RID: 3033
	public TMP_Text m_text;

	// Token: 0x04000BDA RID: 3034
	public TMP_Text m_runeText;

	// Token: 0x04000BDB RID: 3035
	public GameObject m_closeText;

	// Token: 0x04000BDC RID: 3036
	[Header("Intro")]
	public GameObject m_introRoot;

	// Token: 0x04000BDD RID: 3037
	public TMP_Text m_introTopic;

	// Token: 0x04000BDE RID: 3038
	public TMP_Text m_introText;

	// Token: 0x04000BDF RID: 3039
	[Header("Raven")]
	public GameObject m_ravenRoot;

	// Token: 0x04000BE0 RID: 3040
	public TMP_Text m_ravenTopic;

	// Token: 0x04000BE1 RID: 3041
	public TMP_Text m_ravenText;

	// Token: 0x04000BE2 RID: 3042
	private static readonly int s_visibleID = ZSyncAnimation.GetHash("visible");

	// Token: 0x04000BE3 RID: 3043
	private static readonly int s_animatorTagVisible = ZSyncAnimation.GetHash("visible");

	// Token: 0x04000BE4 RID: 3044
	private float m_showTime;

	// Token: 0x04000BE5 RID: 3045
	private bool m_autoHide;

	// Token: 0x04000BE6 RID: 3046
	private Vector3 m_openPlayerPos = Vector3.zero;

	// Token: 0x020002C5 RID: 709
	public enum Style
	{
		// Token: 0x040022D1 RID: 8913
		Rune,
		// Token: 0x040022D2 RID: 8914
		Intro,
		// Token: 0x040022D3 RID: 8915
		Raven
	}
}
