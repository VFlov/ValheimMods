using System;
using UnityEngine;

// Token: 0x0200018C RID: 396
public class Ladder : MonoBehaviour, Interactable, Hoverable
{
	// Token: 0x060017BD RID: 6077 RVA: 0x000B0C10 File Offset: 0x000AEE10
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!this.InUseDistance(character))
		{
			return false;
		}
		character.transform.position = this.m_targetPos.position;
		character.transform.rotation = this.m_targetPos.rotation;
		character.SetLookDir(this.m_targetPos.forward, 0f);
		Physics.SyncTransforms();
		return false;
	}

	// Token: 0x060017BE RID: 6078 RVA: 0x000B0C75 File Offset: 0x000AEE75
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x060017BF RID: 6079 RVA: 0x000B0C78 File Offset: 0x000AEE78
	public string GetHoverText()
	{
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	// Token: 0x060017C0 RID: 6080 RVA: 0x000B0CB1 File Offset: 0x000AEEB1
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x060017C1 RID: 6081 RVA: 0x000B0CB9 File Offset: 0x000AEEB9
	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, base.transform.position) < this.m_useDistance;
	}

	// Token: 0x0400178E RID: 6030
	public Transform m_targetPos;

	// Token: 0x0400178F RID: 6031
	public string m_name = "Ladder";

	// Token: 0x04001790 RID: 6032
	public float m_useDistance = 2f;
}
