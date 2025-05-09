using System;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x0200009B RID: 155
public class ToggleImage : MonoBehaviour
{
	// Token: 0x06000A59 RID: 2649 RVA: 0x0005A0E6 File Offset: 0x000582E6
	private void Awake()
	{
		this.m_toggle = base.GetComponent<Toggle>();
	}

	// Token: 0x06000A5A RID: 2650 RVA: 0x0005A0F4 File Offset: 0x000582F4
	private void Update()
	{
		if (this.m_toggle.isOn)
		{
			this.m_targetImage.sprite = this.m_onImage;
			return;
		}
		this.m_targetImage.sprite = this.m_offImage;
	}

	// Token: 0x04000BE7 RID: 3047
	private Toggle m_toggle;

	// Token: 0x04000BE8 RID: 3048
	public Image m_targetImage;

	// Token: 0x04000BE9 RID: 3049
	public Sprite m_onImage;

	// Token: 0x04000BEA RID: 3050
	public Sprite m_offImage;
}
