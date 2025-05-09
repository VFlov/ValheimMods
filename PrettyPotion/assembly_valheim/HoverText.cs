using System;
using UnityEngine;

// Token: 0x02000187 RID: 391
public class HoverText : MonoBehaviour, Hoverable
{
	// Token: 0x0600178E RID: 6030 RVA: 0x000AF751 File Offset: 0x000AD951
	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_text);
	}

	// Token: 0x0600178F RID: 6031 RVA: 0x000AF763 File Offset: 0x000AD963
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_text);
	}

	// Token: 0x04001769 RID: 5993
	public string m_text = "";
}
