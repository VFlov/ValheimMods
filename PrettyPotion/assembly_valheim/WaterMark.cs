using System;
using TMPro;
using UnityEngine;

// Token: 0x020000AB RID: 171
public class WaterMark : MonoBehaviour
{
	// Token: 0x06000AA4 RID: 2724 RVA: 0x0005B033 File Offset: 0x00059233
	private void Awake()
	{
		this.m_text.text = "Version: " + global::Version.GetVersionString(false);
	}

	// Token: 0x04000C2A RID: 3114
	public TMP_Text m_text;
}
