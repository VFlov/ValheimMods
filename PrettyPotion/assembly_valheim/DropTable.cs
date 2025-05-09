using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000AD RID: 173
[Serializable]
public class DropTable
{
	// Token: 0x06000AC3 RID: 2755 RVA: 0x0005B840 File Offset: 0x00059A40
	public DropTable Clone()
	{
		return base.MemberwiseClone() as DropTable;
	}

	// Token: 0x06000AC4 RID: 2756 RVA: 0x0005B850 File Offset: 0x00059A50
	public List<ItemDrop.ItemData> GetDropListItems()
	{
		DropTable.toDrop.Clear();
		if (this.m_drops.Count == 0)
		{
			return DropTable.toDrop;
		}
		if (UnityEngine.Random.value > this.m_dropChance)
		{
			return DropTable.toDrop;
		}
		DropTable.drops.Clear();
		DropTable.drops.AddRange(this.m_drops);
		float num = 0f;
		foreach (DropTable.DropData dropData in DropTable.drops)
		{
			num += dropData.m_weight;
		}
		int num2 = UnityEngine.Random.Range(this.m_dropMin, this.m_dropMax + 1);
		for (int i = 0; i < num2; i++)
		{
			float num3 = UnityEngine.Random.Range(0f, num);
			bool flag = false;
			float num4 = 0f;
			foreach (DropTable.DropData dropData2 in DropTable.drops)
			{
				num4 += dropData2.m_weight;
				if (num3 <= num4)
				{
					flag = true;
					this.AddItemToList(DropTable.toDrop, dropData2);
					if (this.m_oneOfEach)
					{
						DropTable.drops.Remove(dropData2);
						num -= dropData2.m_weight;
						break;
					}
					break;
				}
			}
			if (!flag && DropTable.drops.Count > 0)
			{
				this.AddItemToList(DropTable.toDrop, DropTable.drops[0]);
			}
		}
		return DropTable.toDrop;
	}

	// Token: 0x06000AC5 RID: 2757 RVA: 0x0005B9E0 File Offset: 0x00059BE0
	private void AddItemToList(List<ItemDrop.ItemData> toDrop, DropTable.DropData data)
	{
		ItemDrop.ItemData itemData = data.m_item.GetComponent<ItemDrop>().m_itemData;
		ItemDrop.ItemData itemData2 = itemData.Clone();
		itemData2.m_dropPrefab = data.m_item;
		int num = Mathf.Max(1, data.m_stackMin);
		int num2 = Mathf.Min(itemData.m_shared.m_maxStackSize, data.m_stackMax);
		itemData2.m_stack = (data.m_dontScale ? UnityEngine.Random.Range(num, num2 + 1) : Game.instance.ScaleDrops(itemData2, num, num2 + 1));
		itemData2.m_worldLevel = (int)((byte)Game.m_worldLevel);
		toDrop.Add(itemData2);
	}

	// Token: 0x06000AC6 RID: 2758 RVA: 0x0005BA70 File Offset: 0x00059C70
	public List<GameObject> GetDropList()
	{
		int amount = UnityEngine.Random.Range(this.m_dropMin, this.m_dropMax + 1);
		return this.GetDropList(amount);
	}

	// Token: 0x06000AC7 RID: 2759 RVA: 0x0005BA98 File Offset: 0x00059C98
	private List<GameObject> GetDropList(int amount)
	{
		List<GameObject> list = new List<GameObject>();
		if (this.m_drops.Count == 0)
		{
			return list;
		}
		if (UnityEngine.Random.value > this.m_dropChance)
		{
			return list;
		}
		float num = Mathf.Ceil(Game.m_resourceRate);
		int num2 = 0;
		while ((float)num2 < num)
		{
			DropTable.dropsTemp.Clear();
			DropTable.dropsTemp.AddRange(this.m_drops);
			bool flag = (float)(num2 + 1) == num;
			float num3 = Game.m_resourceRate % 1f;
			if (num3 == 0f)
			{
				num3 = 1f;
			}
			float num4 = flag ? ((num3 == 0f) ? 1f : num3) : 1f;
			float num5 = 0f;
			foreach (DropTable.DropData dropData in DropTable.dropsTemp)
			{
				num5 += dropData.m_weight;
				if (dropData.m_weight <= 0f && DropTable.dropsTemp.Count > 1)
				{
					ZLog.LogWarning(string.Format("Droptable item '{0}' has a weight of 0 and will not be dropped correctly!", dropData.m_item));
				}
			}
			if (num4 < 1f && amount > DropTable.dropsTemp.Count)
			{
				amount = (int)Mathf.Max(1f, Mathf.Round((float)amount * num4));
			}
			for (int i = 0; i < amount; i++)
			{
				float num6 = UnityEngine.Random.Range(0f, num5);
				bool flag2 = false;
				float num7 = 0f;
				foreach (DropTable.DropData dropData2 in DropTable.dropsTemp)
				{
					num7 += dropData2.m_weight;
					if (num6 <= num7)
					{
						flag2 = true;
						int num8;
						if (dropData2.m_dontScale)
						{
							if (num2 == 0)
							{
								num8 = UnityEngine.Random.Range(dropData2.m_stackMin, dropData2.m_stackMax);
							}
							else
							{
								num8 = 0;
							}
						}
						else
						{
							num8 = (int)Mathf.Max(1f, Mathf.Round(UnityEngine.Random.Range(Mathf.Round((float)dropData2.m_stackMin * num4), Mathf.Round((float)dropData2.m_stackMax * num4))));
						}
						for (int j = 0; j < num8; j++)
						{
							list.Add(dropData2.m_item);
						}
						if (this.m_oneOfEach)
						{
							DropTable.dropsTemp.Remove(dropData2);
							num5 -= dropData2.m_weight;
							break;
						}
						break;
					}
				}
				if (!flag2 && DropTable.dropsTemp.Count > 0)
				{
					list.Add(DropTable.dropsTemp[0].m_item);
				}
			}
			num2++;
		}
		return list;
	}

	// Token: 0x06000AC8 RID: 2760 RVA: 0x0005BD44 File Offset: 0x00059F44
	public bool IsEmpty()
	{
		return this.m_drops.Count == 0;
	}

	// Token: 0x04000C4B RID: 3147
	private static List<DropTable.DropData> drops = new List<DropTable.DropData>();

	// Token: 0x04000C4C RID: 3148
	private static List<ItemDrop.ItemData> toDrop = new List<ItemDrop.ItemData>();

	// Token: 0x04000C4D RID: 3149
	private static List<DropTable.DropData> dropsTemp = new List<DropTable.DropData>();

	// Token: 0x04000C4E RID: 3150
	public List<DropTable.DropData> m_drops = new List<DropTable.DropData>();

	// Token: 0x04000C4F RID: 3151
	public int m_dropMin = 1;

	// Token: 0x04000C50 RID: 3152
	public int m_dropMax = 1;

	// Token: 0x04000C51 RID: 3153
	[Range(0f, 1f)]
	public float m_dropChance = 1f;

	// Token: 0x04000C52 RID: 3154
	public bool m_oneOfEach;

	// Token: 0x020002CE RID: 718
	[Serializable]
	public struct DropData
	{
		// Token: 0x040022E5 RID: 8933
		public GameObject m_item;

		// Token: 0x040022E6 RID: 8934
		public int m_stackMin;

		// Token: 0x040022E7 RID: 8935
		public int m_stackMax;

		// Token: 0x040022E8 RID: 8936
		public float m_weight;

		// Token: 0x040022E9 RID: 8937
		public bool m_dontScale;
	}
}
