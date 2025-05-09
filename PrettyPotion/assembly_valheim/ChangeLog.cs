using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x0200006C RID: 108
public class ChangeLog : MonoBehaviour
{
	// Token: 0x060006EA RID: 1770 RVA: 0x00039660 File Offset: 0x00037860
	private void Start()
	{
		string text = this.m_changeLog.text;
		this.m_textField.text = text;
	}

	// Token: 0x060006EB RID: 1771 RVA: 0x00039685 File Offset: 0x00037885
	private void LateUpdate()
	{
		if (!this.m_hasSetScroll)
		{
			this.m_hasSetScroll = true;
			if (this.m_scrollbar != null)
			{
				this.m_scrollbar.value = 1f;
			}
		}
	}

	// Token: 0x04000828 RID: 2088
	private bool m_hasSetScroll;

	// Token: 0x04000829 RID: 2089
	public TMP_Text m_textField;

	// Token: 0x0400082A RID: 2090
	public TextAsset m_changeLog;

	// Token: 0x0400082B RID: 2091
	public TextAsset m_xboxChangeLog;

	// Token: 0x0400082C RID: 2092
	public Scrollbar m_scrollbar;

	// Token: 0x0400082D RID: 2093
	public GameObject m_showPlayerLog;
}
