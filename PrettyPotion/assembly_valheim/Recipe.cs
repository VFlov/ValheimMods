using System;
using UnityEngine;

// Token: 0x020000B4 RID: 180
public class Recipe : ScriptableObject
{
	// Token: 0x06000B72 RID: 2930 RVA: 0x00060C58 File Offset: 0x0005EE58
	public int GetRequiredStationLevel(int quality)
	{
		return Mathf.Max(1, this.m_minStationLevel) + (quality - 1);
	}

	// Token: 0x06000B73 RID: 2931 RVA: 0x00060C6A File Offset: 0x0005EE6A
	public CraftingStation GetRequiredStation(int quality)
	{
		if (this.m_craftingStation)
		{
			return this.m_craftingStation;
		}
		if (quality > 1)
		{
			return this.m_repairStation;
		}
		return null;
	}

	// Token: 0x06000B74 RID: 2932 RVA: 0x00060C8C File Offset: 0x0005EE8C
	public int GetAmount(int quality, out int need, out ItemDrop.ItemData singleReqItem, int craftMultiplier = 1)
	{
		int num = this.m_amount;
		need = 0;
		singleReqItem = null;
		if (this.m_requireOnlyOneIngredient)
		{
			int num2;
			singleReqItem = Player.m_localPlayer.GetFirstRequiredItem(Player.m_localPlayer.GetInventory(), this, quality, out need, out num2, craftMultiplier);
			num += (int)Mathf.Ceil((float)((singleReqItem.m_quality - 1) * this.m_amount) * this.m_qualityResultAmountMultiplier) + num2;
		}
		return num * craftMultiplier;
	}

	// Token: 0x04000CB0 RID: 3248
	public ItemDrop m_item;

	// Token: 0x04000CB1 RID: 3249
	public int m_amount = 1;

	// Token: 0x04000CB2 RID: 3250
	public bool m_enabled = true;

	// Token: 0x04000CB3 RID: 3251
	[global::Tooltip("Only supported when using m_requireOnlyOneIngredient")]
	public float m_qualityResultAmountMultiplier = 1f;

	// Token: 0x04000CB4 RID: 3252
	public int m_listSortWeight = 100;

	// Token: 0x04000CB5 RID: 3253
	[Header("Requirements")]
	public CraftingStation m_craftingStation;

	// Token: 0x04000CB6 RID: 3254
	public CraftingStation m_repairStation;

	// Token: 0x04000CB7 RID: 3255
	public int m_minStationLevel = 1;

	// Token: 0x04000CB8 RID: 3256
	public bool m_requireOnlyOneIngredient;

	// Token: 0x04000CB9 RID: 3257
	public Piece.Requirement[] m_resources = new Piece.Requirement[0];
}
