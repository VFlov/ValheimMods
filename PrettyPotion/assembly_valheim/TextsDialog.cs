using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000099 RID: 153
public class TextsDialog : MonoBehaviour
{
	// Token: 0x06000A3E RID: 2622 RVA: 0x000594A4 File Offset: 0x000576A4
	private void Awake()
	{
		this.m_baseListSize = this.m_listRoot.rect.height;
	}

	// Token: 0x06000A3F RID: 2623 RVA: 0x000594CC File Offset: 0x000576CC
	public void Setup(Player player)
	{
		base.gameObject.SetActive(true);
		this.FillTextList();
		if (this.m_texts.Count > 0)
		{
			this.ShowText(0);
			return;
		}
		this.m_textAreaTopic.text = "";
		this.m_textArea.text = "";
	}

	// Token: 0x06000A40 RID: 2624 RVA: 0x00059524 File Offset: 0x00057724
	private void Update()
	{
		this.UpdateGamepadInput();
		if (this.m_texts.Count > 0)
		{
			RectTransform rectTransform = this.m_leftScrollRect.transform as RectTransform;
			RectTransform listRoot = this.m_listRoot;
			this.m_leftScrollbar.size = rectTransform.rect.height / listRoot.rect.height;
		}
	}

	// Token: 0x06000A41 RID: 2625 RVA: 0x00059585 File Offset: 0x00057785
	private IEnumerator FocusOnCurrentLevel(ScrollRect scrollRect, RectTransform listRoot, RectTransform element)
	{
		yield return null;
		yield return null;
		Canvas.ForceUpdateCanvases();
		this.SnapTo(scrollRect, this.m_listRoot, element);
		yield break;
	}

	// Token: 0x06000A42 RID: 2626 RVA: 0x000595A4 File Offset: 0x000577A4
	private void SnapTo(ScrollRect scrollRect, RectTransform listRoot, RectTransform target)
	{
		Canvas.ForceUpdateCanvases();
		listRoot.anchoredPosition = scrollRect.transform.InverseTransformPoint(listRoot.position) - scrollRect.transform.InverseTransformPoint(target.position) - new Vector2(target.sizeDelta.x / 2f, 0f);
	}

	// Token: 0x06000A43 RID: 2627 RVA: 0x00059610 File Offset: 0x00057810
	private void FillTextList()
	{
		foreach (TextsDialog.TextInfo textInfo in this.m_texts)
		{
			UnityEngine.Object.Destroy(textInfo.m_listElement);
		}
		this.m_texts.Clear();
		this.UpdateTextsList();
		for (int i = 0; i < this.m_texts.Count; i++)
		{
			TextsDialog.TextInfo text = this.m_texts[i];
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, Vector3.zero, Quaternion.identity, this.m_listRoot);
			gameObject.SetActive(true);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)(-(float)i) * this.m_spacing);
			Utils.FindChild(gameObject.transform, "name", Utils.IterativeSearchType.DepthFirst).GetComponent<TMP_Text>().text = Localization.instance.Localize(text.m_topic);
			text.m_listElement = gameObject;
			text.m_selected = Utils.FindChild(gameObject.transform, "selected", Utils.IterativeSearchType.DepthFirst).gameObject;
			text.m_selected.SetActive(false);
			gameObject.GetComponent<Button>().onClick.AddListener(delegate()
			{
				this.OnSelectText(text);
			});
		}
		float size = Mathf.Max(this.m_baseListSize, (float)this.m_texts.Count * this.m_spacing);
		this.m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		if (this.m_texts.Count > 0)
		{
			this.m_recipeEnsureVisible.CenterOnItem(this.m_texts[0].m_listElement.transform as RectTransform);
		}
	}

	// Token: 0x06000A44 RID: 2628 RVA: 0x000597EC File Offset: 0x000579EC
	private void UpdateGamepadInput()
	{
		if (this.m_inputDelayTimer > 0f)
		{
			this.m_inputDelayTimer -= Time.unscaledDeltaTime;
			return;
		}
		if (ZInput.IsGamepadActive() && this.m_texts.Count > 0)
		{
			float joyRightStickY = ZInput.GetJoyRightStickY(true);
			float joyLeftStickY = ZInput.GetJoyLeftStickY(true);
			bool buttonDown = ZInput.GetButtonDown("JoyDPadUp");
			bool flag = joyLeftStickY < -0.1f;
			bool buttonDown2 = ZInput.GetButtonDown("JoyDPadDown");
			bool flag2 = joyLeftStickY > 0.1f;
			if ((buttonDown2 || flag2) && this.m_selectionIndex < this.m_texts.Count - 1)
			{
				this.ShowText(Mathf.Min(this.m_texts.Count - 1, this.GetSelectedText() + 1));
				this.m_inputDelayTimer = 0.1f;
			}
			if ((flag || buttonDown) && this.m_selectionIndex > 0)
			{
				this.ShowText(Mathf.Max(0, this.GetSelectedText() - 1));
				this.m_inputDelayTimer = 0.1f;
			}
			if (this.m_rightScrollbar.gameObject.activeSelf && (joyRightStickY < -0.1f || joyRightStickY > 0.1f))
			{
				this.m_rightScrollbar.value = Mathf.Clamp01(this.m_rightScrollbar.value - joyRightStickY * 10f * Time.deltaTime * (1f - this.m_rightScrollbar.size));
				this.m_inputDelayTimer = 0.1f;
			}
		}
	}

	// Token: 0x06000A45 RID: 2629 RVA: 0x00059940 File Offset: 0x00057B40
	private void OnSelectText(TextsDialog.TextInfo text)
	{
		this.ShowText(text);
	}

	// Token: 0x06000A46 RID: 2630 RVA: 0x0005994C File Offset: 0x00057B4C
	private int GetSelectedText()
	{
		for (int i = 0; i < this.m_texts.Count; i++)
		{
			if (this.m_texts[i].m_selected.activeSelf)
			{
				return i;
			}
		}
		return 0;
	}

	// Token: 0x06000A47 RID: 2631 RVA: 0x0005998A File Offset: 0x00057B8A
	private void ShowText(int i)
	{
		this.m_selectionIndex = i;
		this.ShowText(this.m_texts[i]);
	}

	// Token: 0x06000A48 RID: 2632 RVA: 0x000599A8 File Offset: 0x00057BA8
	private void ShowText(TextsDialog.TextInfo text)
	{
		this.m_textAreaTopic.text = Localization.instance.Localize(text.m_topic);
		this.m_textArea.text = Localization.instance.Localize(text.m_text);
		foreach (TextsDialog.TextInfo textInfo in this.m_texts)
		{
			textInfo.m_selected.SetActive(false);
		}
		text.m_selected.SetActive(true);
		base.StartCoroutine(this.FocusOnCurrentLevel(this.m_leftScrollRect, this.m_listRoot, text.m_selected.transform as RectTransform));
	}

	// Token: 0x06000A49 RID: 2633 RVA: 0x00059A6C File Offset: 0x00057C6C
	public void OnClose()
	{
		base.gameObject.SetActive(false);
	}

	// Token: 0x06000A4A RID: 2634 RVA: 0x00059A7C File Offset: 0x00057C7C
	private void UpdateTextsList()
	{
		this.m_texts.Clear();
		foreach (KeyValuePair<string, string> keyValuePair in Player.m_localPlayer.GetKnownTexts())
		{
			this.m_texts.Add(new TextsDialog.TextInfo(Localization.instance.Localize(keyValuePair.Key.Replace("\u0016", "")), Localization.instance.Localize(keyValuePair.Value.Replace("\u0016", ""))));
		}
		this.m_texts.Sort((TextsDialog.TextInfo a, TextsDialog.TextInfo b) => a.m_topic.CompareTo(b.m_topic));
		this.AddLog();
		this.AddActiveEffects();
	}

	// Token: 0x06000A4B RID: 2635 RVA: 0x00059B60 File Offset: 0x00057D60
	private void AddLog()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string str in MessageHud.instance.GetLog())
		{
			stringBuilder.Append(str + "\n\n");
		}
		this.m_texts.Insert(0, new TextsDialog.TextInfo(Localization.instance.Localize("$inventory_logs"), stringBuilder.ToString()));
	}

	// Token: 0x06000A4C RID: 2636 RVA: 0x00059BF0 File Offset: 0x00057DF0
	private void AddActiveEffects()
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		List<StatusEffect> list = new List<StatusEffect>();
		Player.m_localPlayer.GetSEMan().GetHUDStatusEffects(list);
		StringBuilder stringBuilder = new StringBuilder(256);
		foreach (StatusEffect statusEffect in list)
		{
			stringBuilder.Append("<color=orange>" + Localization.instance.Localize(statusEffect.m_name) + "</color>\n");
			stringBuilder.Append(Localization.instance.Localize(statusEffect.GetTooltipString()));
			stringBuilder.Append("\n\n");
		}
		StatusEffect statusEffect2;
		float num;
		Player.m_localPlayer.GetGuardianPowerHUD(out statusEffect2, out num);
		if (statusEffect2)
		{
			stringBuilder.Append("<color=yellow>" + Localization.instance.Localize("$inventory_selectedgp") + "</color>\n");
			stringBuilder.Append("<color=orange>" + Localization.instance.Localize(statusEffect2.m_name) + "</color>\n");
			stringBuilder.Append(Localization.instance.Localize(statusEffect2.GetTooltipString()));
		}
		this.m_texts.Insert(0, new TextsDialog.TextInfo(Localization.instance.Localize("$inventory_activeeffects"), stringBuilder.ToString()));
	}

	// Token: 0x04000BC4 RID: 3012
	public RectTransform m_listRoot;

	// Token: 0x04000BC5 RID: 3013
	public ScrollRect m_leftScrollRect;

	// Token: 0x04000BC6 RID: 3014
	public Scrollbar m_leftScrollbar;

	// Token: 0x04000BC7 RID: 3015
	public Scrollbar m_rightScrollbar;

	// Token: 0x04000BC8 RID: 3016
	public GameObject m_elementPrefab;

	// Token: 0x04000BC9 RID: 3017
	public TMP_Text m_totalSkillText;

	// Token: 0x04000BCA RID: 3018
	public float m_spacing = 80f;

	// Token: 0x04000BCB RID: 3019
	public TMP_Text m_textAreaTopic;

	// Token: 0x04000BCC RID: 3020
	public TMP_Text m_textArea;

	// Token: 0x04000BCD RID: 3021
	public ScrollRectEnsureVisible m_recipeEnsureVisible;

	// Token: 0x04000BCE RID: 3022
	private List<TextsDialog.TextInfo> m_texts = new List<TextsDialog.TextInfo>();

	// Token: 0x04000BCF RID: 3023
	private float m_baseListSize;

	// Token: 0x04000BD0 RID: 3024
	private int m_selectionIndex;

	// Token: 0x04000BD1 RID: 3025
	private float m_inputDelayTimer;

	// Token: 0x04000BD2 RID: 3026
	private const float InputDelay = 0.1f;

	// Token: 0x020002C1 RID: 705
	public class TextInfo
	{
		// Token: 0x060020EC RID: 8428 RVA: 0x000E99AE File Offset: 0x000E7BAE
		public TextInfo(string topic, string text)
		{
			this.m_topic = topic;
			this.m_text = text;
		}

		// Token: 0x040022C3 RID: 8899
		public string m_topic;

		// Token: 0x040022C4 RID: 8900
		public string m_text;

		// Token: 0x040022C5 RID: 8901
		public GameObject m_listElement;

		// Token: 0x040022C6 RID: 8902
		public GameObject m_selected;
	}
}
