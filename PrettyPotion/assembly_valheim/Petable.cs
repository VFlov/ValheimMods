using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001A2 RID: 418
public class Petable : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x0600188E RID: 6286 RVA: 0x000B79D0 File Offset: 0x000B5BD0
	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $hud_pet");
	}

	// Token: 0x0600188F RID: 6287 RVA: 0x000B79EC File Offset: 0x000B5BEC
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	// Token: 0x06001890 RID: 6288 RVA: 0x000B7A00 File Offset: 0x000B5C00
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (Time.time - this.m_lastPetTime > 1f)
		{
			this.m_lastPetTime = Time.time;
			this.m_petEffect.Create(this.m_effectLocation ? this.m_effectLocation.position : base.transform.position, this.m_effectLocation ? this.m_effectLocation.rotation : base.transform.rotation, null, 1f, -1);
			user.Message(MessageHud.MessageType.Center, this.m_name + " " + this.m_randomPetTexts[UnityEngine.Random.Range(0, this.m_randomPetTexts.Count)], 0, null);
			return true;
		}
		return false;
	}

	// Token: 0x06001891 RID: 6289 RVA: 0x000B7AC3 File Offset: 0x000B5CC3
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x0400189A RID: 6298
	public string m_name = "";

	// Token: 0x0400189B RID: 6299
	public Transform m_effectLocation;

	// Token: 0x0400189C RID: 6300
	public List<string> m_randomPetTexts = new List<string>();

	// Token: 0x0400189D RID: 6301
	public EffectList m_petEffect = new EffectList();

	// Token: 0x0400189E RID: 6302
	private float m_lastPetTime;
}
