using System;
using GUIFramework;
using TMPro;
using UnityEngine;

// Token: 0x02000098 RID: 152
public class TextInput : MonoBehaviour
{
	// Token: 0x06000A31 RID: 2609 RVA: 0x000592E7 File Offset: 0x000574E7
	private void Awake()
	{
		TextInput.m_instance = this;
		this.m_panel.SetActive(false);
	}

	// Token: 0x17000044 RID: 68
	// (get) Token: 0x06000A32 RID: 2610 RVA: 0x000592FB File Offset: 0x000574FB
	public static TextInput instance
	{
		get
		{
			return TextInput.m_instance;
		}
	}

	// Token: 0x06000A33 RID: 2611 RVA: 0x00059302 File Offset: 0x00057502
	private void OnDestroy()
	{
		TextInput.m_instance = null;
	}

	// Token: 0x06000A34 RID: 2612 RVA: 0x0005930A File Offset: 0x0005750A
	public static bool IsVisible()
	{
		return TextInput.m_instance && TextInput.m_instance.m_visibleFrame;
	}

	// Token: 0x06000A35 RID: 2613 RVA: 0x00059324 File Offset: 0x00057524
	private void Update()
	{
		if (this.m_bShouldHideNextFrame)
		{
			this.m_bShouldHideNextFrame = false;
			this.Hide();
			return;
		}
		this.m_visibleFrame = TextInput.m_instance.m_panel.gameObject.activeSelf;
		if (!this.m_visibleFrame)
		{
			return;
		}
		if (global::Console.IsVisible() || Chat.instance.HasFocus())
		{
			return;
		}
		if (ZInput.GetKeyDown(KeyCode.Escape, true))
		{
			this.Hide();
			return;
		}
	}

	// Token: 0x06000A36 RID: 2614 RVA: 0x0005938F File Offset: 0x0005758F
	public void OnInput()
	{
		this.setText(this.m_inputField.text.Replace("\\n", "\n").Replace("\\t", "\t"));
		this.m_bShouldHideNextFrame = true;
	}

	// Token: 0x06000A37 RID: 2615 RVA: 0x000593C7 File Offset: 0x000575C7
	public void OnCancel()
	{
		this.Hide();
	}

	// Token: 0x06000A38 RID: 2616 RVA: 0x000593CF File Offset: 0x000575CF
	public void OnEnter()
	{
		this.setText(this.m_inputField.text.Replace("\\n", "\n").Replace("\\t", "\t"));
		this.Hide();
	}

	// Token: 0x06000A39 RID: 2617 RVA: 0x00059406 File Offset: 0x00057606
	private void setText(string text)
	{
		if (this.m_queuedSign != null)
		{
			this.m_queuedSign.SetText(text);
			this.m_queuedSign = null;
		}
	}

	// Token: 0x06000A3A RID: 2618 RVA: 0x00059423 File Offset: 0x00057623
	public void RequestText(TextReceiver sign, string topic, int charLimit)
	{
		this.m_queuedSign = sign;
		this.Show(topic, sign.GetText(), charLimit);
	}

	// Token: 0x06000A3B RID: 2619 RVA: 0x0005943C File Offset: 0x0005763C
	private void Show(string topic, string text, int charLimit)
	{
		this.m_panel.SetActive(true);
		this.m_topic.text = Localization.instance.Localize(topic);
		this.m_inputField.characterLimit = charLimit;
		this.m_inputField.text = text;
		this.m_inputField.ActivateInputField();
	}

	// Token: 0x06000A3C RID: 2620 RVA: 0x0005948E File Offset: 0x0005768E
	public void Hide()
	{
		this.m_panel.SetActive(false);
	}

	// Token: 0x04000BBD RID: 3005
	private static TextInput m_instance;

	// Token: 0x04000BBE RID: 3006
	public GameObject m_panel;

	// Token: 0x04000BBF RID: 3007
	public TMP_Text m_topic;

	// Token: 0x04000BC0 RID: 3008
	public GuiInputField m_inputField;

	// Token: 0x04000BC1 RID: 3009
	private TextReceiver m_queuedSign;

	// Token: 0x04000BC2 RID: 3010
	private bool m_visibleFrame;

	// Token: 0x04000BC3 RID: 3011
	private bool m_bShouldHideNextFrame;
}
