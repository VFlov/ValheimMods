using System;
using TMPro;
using UnityEngine;
using Valheim.UI;

// Token: 0x0200007E RID: 126
public class KeyHintsRadial : MonoBehaviour
{
	// Token: 0x06000844 RID: 2116 RVA: 0x0004944C File Offset: 0x0004764C
	public void UpdateGamepadHints()
	{
		if (this.m_gamepadInteract != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_gamepadInteract);
			this.m_gamepadInteract.text = "$radial_interact  <mspace=0.6em>$KEY_RadialInteract</mspace>";
			Localization.instance.Localize(this.m_gamepadInteract.transform);
		}
		if (this.m_gamepadBack != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_gamepadBack);
			this.m_gamepadBack.text = "$radial_back  <mspace=0.6em>$KEY_RadialClose</mspace>  /  <mspace=0.6em>$KEY_RadialBack</mspace>";
			Localization.instance.Localize(this.m_gamepadBack.transform);
		}
		if (this.m_gamepadClose != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_gamepadClose);
			this.m_gamepadClose.text = "$radial_close  <mspace=0.6em>$KEY_Radial</mspace>";
			Localization.instance.Localize(this.m_gamepadClose.transform);
		}
		if (this.m_gamepadCloseTopLevel != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_gamepadCloseTopLevel);
			this.m_gamepadCloseTopLevel.text = "$radial_close  <mspace=0.6em>$KEY_RadialClose</mspace>  /  <mspace=0.6em>$KEY_RadialBack</mspace>  /  <mspace=0.6em>$KEY_Radial</mspace>";
			Localization.instance.Localize(this.m_gamepadCloseTopLevel.transform);
		}
		if (this.m_gamepadDrop != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_gamepadDrop);
			this.m_gamepadDrop.text = "$radial_drop  <mspace=0.6em>$KEY_RadialSecondaryInteract</mspace>";
			Localization.instance.Localize(this.m_gamepadDrop.transform);
		}
		if (this.m_gamepadDropMulti != null)
		{
			Localization.instance.RemoveTextFromCache(this.m_gamepadDropMulti);
			this.m_gamepadDropMulti.text = "$radial_drop_multiple  <mspace=0.6em>$KEY_RadialSecondaryInteract</mspace>  $radial_hold";
			Localization.instance.Localize(this.m_gamepadDropMulti.transform);
		}
	}

	// Token: 0x06000845 RID: 2117 RVA: 0x000495EC File Offset: 0x000477EC
	public void UpdateRadialHints(RadialBase radial)
	{
		bool isTopLevel = radial.IsTopLevel;
		this.m_gamepadCloseTopLevel.gameObject.SetActive(isTopLevel);
		this.m_kbCloseTopLevel.SetActive(isTopLevel);
		this.m_gamepadClose.gameObject.SetActive(!isTopLevel);
		this.m_kbClose.gameObject.SetActive(!isTopLevel);
		this.m_gamepadBack.gameObject.SetActive(!isTopLevel);
		this.m_kbBack.SetActive(!isTopLevel);
		bool showThrowHint = radial.ShowThrowHint;
		this.m_gamepadDrop.gameObject.SetActive(showThrowHint);
		this.m_gamepadDropMulti.gameObject.SetActive(showThrowHint);
		this.m_kbDrop.SetActive(showThrowHint);
		this.m_kbDropMulti.SetActive(showThrowHint);
	}

	// Token: 0x04000A09 RID: 2569
	public TextMeshProUGUI m_gamepadInteract;

	// Token: 0x04000A0A RID: 2570
	public TextMeshProUGUI m_gamepadBack;

	// Token: 0x04000A0B RID: 2571
	public TextMeshProUGUI m_gamepadDrop;

	// Token: 0x04000A0C RID: 2572
	public TextMeshProUGUI m_gamepadDropMulti;

	// Token: 0x04000A0D RID: 2573
	public TextMeshProUGUI m_gamepadClose;

	// Token: 0x04000A0E RID: 2574
	public TextMeshProUGUI m_gamepadCloseTopLevel;

	// Token: 0x04000A0F RID: 2575
	public GameObject m_kbInteract;

	// Token: 0x04000A10 RID: 2576
	public GameObject m_kbBack;

	// Token: 0x04000A11 RID: 2577
	public GameObject m_kbDrop;

	// Token: 0x04000A12 RID: 2578
	public GameObject m_kbDropMulti;

	// Token: 0x04000A13 RID: 2579
	public GameObject m_kbClose;

	// Token: 0x04000A14 RID: 2580
	public GameObject m_kbCloseTopLevel;
}
