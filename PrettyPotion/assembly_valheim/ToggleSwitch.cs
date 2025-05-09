using System;
using UnityEngine;

// Token: 0x020001CF RID: 463
public class ToggleSwitch : MonoBehaviour, Interactable, Hoverable
{
	// Token: 0x06001A98 RID: 6808 RVA: 0x000C5BBC File Offset: 0x000C3DBC
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_onUse != null)
		{
			this.m_onUse(this, character);
		}
		return true;
	}

	// Token: 0x06001A99 RID: 6809 RVA: 0x000C5BD9 File Offset: 0x000C3DD9
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001A9A RID: 6810 RVA: 0x000C5BDC File Offset: 0x000C3DDC
	public string GetHoverText()
	{
		return this.m_hoverText;
	}

	// Token: 0x06001A9B RID: 6811 RVA: 0x000C5BE4 File Offset: 0x000C3DE4
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001A9C RID: 6812 RVA: 0x000C5BEC File Offset: 0x000C3DEC
	public void SetState(bool enabled)
	{
		this.m_state = enabled;
		this.m_renderer.material = (this.m_state ? this.m_enableMaterial : this.m_disableMaterial);
	}

	// Token: 0x04001AF9 RID: 6905
	public MeshRenderer m_renderer;

	// Token: 0x04001AFA RID: 6906
	public Material m_enableMaterial;

	// Token: 0x04001AFB RID: 6907
	public Material m_disableMaterial;

	// Token: 0x04001AFC RID: 6908
	public Action<ToggleSwitch, Humanoid> m_onUse;

	// Token: 0x04001AFD RID: 6909
	public string m_hoverText = "";

	// Token: 0x04001AFE RID: 6910
	public string m_name = "";

	// Token: 0x04001AFF RID: 6911
	private bool m_state;
}
