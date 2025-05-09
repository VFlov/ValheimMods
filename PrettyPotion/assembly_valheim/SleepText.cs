using System;
using TMPro;
using UnityEngine;

// Token: 0x020001BB RID: 443
public class SleepText : MonoBehaviour
{
	// Token: 0x060019DF RID: 6623 RVA: 0x000C10D8 File Offset: 0x000BF2D8
	private void OnEnable()
	{
		this.m_textField.CrossFadeAlpha(0f, 0f, true);
		this.m_textField.CrossFadeAlpha(1f, 1f, true);
		this.m_dreamField.enabled = false;
		base.Invoke("CollectResources", 5f);
		base.Invoke("HideZZZ", 2f);
		base.Invoke("ShowDreamText", 4f);
	}

	// Token: 0x060019E0 RID: 6624 RVA: 0x000C114D File Offset: 0x000BF34D
	private void HideZZZ()
	{
		this.m_textField.CrossFadeAlpha(0f, 2f, true);
	}

	// Token: 0x060019E1 RID: 6625 RVA: 0x000C1165 File Offset: 0x000BF365
	private void CollectResources()
	{
		Game.instance.CollectResourcesCheck();
	}

	// Token: 0x060019E2 RID: 6626 RVA: 0x000C1174 File Offset: 0x000BF374
	private void ShowDreamText()
	{
		DreamTexts.DreamText randomDreamText = this.m_dreamTexts.GetRandomDreamText();
		if (randomDreamText == null)
		{
			return;
		}
		this.m_dreamField.text = Localization.instance.Localize(randomDreamText.m_text);
		this.m_dreamField.enabled = true;
		base.Invoke("DelayedCrossFadeStart", 0.1f);
		base.Invoke("HideDreamText", 6.5f);
	}

	// Token: 0x060019E3 RID: 6627 RVA: 0x000C11D8 File Offset: 0x000BF3D8
	private void DelayedCrossFadeStart()
	{
		this.m_dreamField.CrossFadeAlpha(0f, 0f, true);
		this.m_dreamField.CrossFadeAlpha(1f, 1.5f, true);
	}

	// Token: 0x060019E4 RID: 6628 RVA: 0x000C1206 File Offset: 0x000BF406
	private void HideDreamText()
	{
		this.m_dreamField.CrossFadeAlpha(0f, 1.5f, true);
	}

	// Token: 0x04001A6D RID: 6765
	public TMP_Text m_textField;

	// Token: 0x04001A6E RID: 6766
	public TMP_Text m_dreamField;

	// Token: 0x04001A6F RID: 6767
	public DreamTexts m_dreamTexts;
}
