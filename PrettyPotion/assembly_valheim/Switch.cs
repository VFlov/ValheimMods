using System;
using UnityEngine;

// Token: 0x020001C2 RID: 450
public class Switch : MonoBehaviour, Interactable, Hoverable
{
	// Token: 0x06001A35 RID: 6709 RVA: 0x000C2F84 File Offset: 0x000C1184
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			if (this.m_holdRepeatInterval <= 0f)
			{
				return false;
			}
			if (Time.time - this.m_lastUseTime < this.m_holdRepeatInterval)
			{
				return false;
			}
		}
		this.m_lastUseTime = Time.time;
		return this.m_onUse != null && this.m_onUse(this, character, null);
	}

	// Token: 0x06001A36 RID: 6710 RVA: 0x000C2FDC File Offset: 0x000C11DC
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return this.m_onUse != null && this.m_onUse(this, user, item);
	}

	// Token: 0x06001A37 RID: 6711 RVA: 0x000C2FF6 File Offset: 0x000C11F6
	public string GetHoverText()
	{
		if (this.m_onHover != null)
		{
			return this.m_onHover();
		}
		return Localization.instance.Localize(this.m_hoverText);
	}

	// Token: 0x06001A38 RID: 6712 RVA: 0x000C301C File Offset: 0x000C121C
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	// Token: 0x04001AA5 RID: 6821
	public Switch.Callback m_onUse;

	// Token: 0x04001AA6 RID: 6822
	public Switch.TooltipCallback m_onHover;

	// Token: 0x04001AA7 RID: 6823
	[TextArea(3, 20)]
	public string m_hoverText = "";

	// Token: 0x04001AA8 RID: 6824
	public string m_name = "";

	// Token: 0x04001AA9 RID: 6825
	public float m_holdRepeatInterval = -1f;

	// Token: 0x04001AAA RID: 6826
	private float m_lastUseTime;

	// Token: 0x02000386 RID: 902
	// (Invoke) Token: 0x06002309 RID: 8969
	public delegate bool Callback(Switch caller, Humanoid user, ItemDrop.ItemData item);

	// Token: 0x02000387 RID: 903
	// (Invoke) Token: 0x0600230D RID: 8973
	public delegate string TooltipCallback();
}
