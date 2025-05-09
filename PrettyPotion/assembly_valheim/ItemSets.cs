using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Token: 0x0200018A RID: 394
public class ItemSets : MonoBehaviour
{
	// Token: 0x170000C7 RID: 199
	// (get) Token: 0x0600179D RID: 6045 RVA: 0x000AFB3E File Offset: 0x000ADD3E
	public static ItemSets instance
	{
		get
		{
			return ItemSets.m_instance;
		}
	}

	// Token: 0x0600179E RID: 6046 RVA: 0x000AFB45 File Offset: 0x000ADD45
	public void Awake()
	{
		ItemSets.m_instance = this;
	}

	// Token: 0x0600179F RID: 6047 RVA: 0x000AFB50 File Offset: 0x000ADD50
	public bool TryGetSet(string name, bool dropCurrentItems = false, int itemLevelOverride = -1, int worldLevel = -1)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		ItemSets.ItemSet itemSet;
		if (this.GetSetDictionary().TryGetValue(name, out itemSet))
		{
			Skills skills = Player.m_localPlayer.GetSkills();
			if (dropCurrentItems)
			{
				Player.m_localPlayer.CreateTombStone();
				Player.m_localPlayer.ClearFood();
				Player.m_localPlayer.ClearHardDeath();
				Player.m_localPlayer.GetSEMan().RemoveAllStatusEffects(false);
				foreach (Skills.SkillDef skillDef in skills.m_skills)
				{
					skills.CheatResetSkill(skillDef.m_skill.ToString());
				}
			}
			Inventory inventory = Player.m_localPlayer.GetInventory();
			InventoryGui.instance.m_playerGrid.UpdateInventory(inventory, Player.m_localPlayer, null);
			foreach (ItemSets.SetItem setItem in itemSet.m_items)
			{
				if (!(setItem.m_item == null))
				{
					int amount = Math.Max(1, setItem.m_stack);
					int num = Math.Max(1, (itemLevelOverride >= 0) ? itemLevelOverride : setItem.m_quality);
					if (num > 4)
					{
						num = 4;
					}
					ItemDrop.ItemData itemData = inventory.AddItem(setItem.m_item.gameObject.name, Math.Max(1, setItem.m_stack), num, 0, 0L, "Thor", true);
					if (worldLevel >= 0)
					{
						itemData.m_worldLevel = worldLevel;
					}
					if (itemData != null)
					{
						if (setItem.m_use)
						{
							Player.m_localPlayer.UseItem(inventory, itemData, false);
						}
						if (setItem.m_hotbarSlot > 0)
						{
							InventoryGui.instance.m_playerGrid.DropItem(inventory, itemData, amount, new Vector2i(setItem.m_hotbarSlot - 1, 0));
						}
					}
				}
			}
			foreach (ItemSets.SetSkill setSkill in itemSet.m_skills)
			{
				skills.CheatResetSkill(setSkill.m_skill.ToString());
				Player.m_localPlayer.GetSkills().CheatRaiseSkill(setSkill.m_skill.ToString(), (float)setSkill.m_level, true);
			}
			return true;
		}
		return false;
	}

	// Token: 0x060017A0 RID: 6048 RVA: 0x000AFDC0 File Offset: 0x000ADFC0
	public List<string> GetSetNames()
	{
		return this.GetSetDictionary().Keys.ToList<string>();
	}

	// Token: 0x060017A1 RID: 6049 RVA: 0x000AFDD4 File Offset: 0x000ADFD4
	public Dictionary<string, ItemSets.ItemSet> GetSetDictionary()
	{
		Dictionary<string, ItemSets.ItemSet> dictionary = new Dictionary<string, ItemSets.ItemSet>();
		foreach (ItemSets.ItemSet itemSet in this.m_sets)
		{
			dictionary[itemSet.m_name] = itemSet;
		}
		return dictionary;
	}

	// Token: 0x04001776 RID: 6006
	private static ItemSets m_instance;

	// Token: 0x04001777 RID: 6007
	public List<ItemSets.ItemSet> m_sets = new List<ItemSets.ItemSet>();

	// Token: 0x0200036F RID: 879
	[Serializable]
	public class ItemSet
	{
		// Token: 0x04002619 RID: 9753
		public string m_name;

		// Token: 0x0400261A RID: 9754
		public List<ItemSets.SetItem> m_items = new List<ItemSets.SetItem>();

		// Token: 0x0400261B RID: 9755
		public List<ItemSets.SetSkill> m_skills = new List<ItemSets.SetSkill>();
	}

	// Token: 0x02000370 RID: 880
	[Serializable]
	public class SetItem
	{
		// Token: 0x0400261C RID: 9756
		public ItemDrop m_item;

		// Token: 0x0400261D RID: 9757
		public int m_quality = 1;

		// Token: 0x0400261E RID: 9758
		public int m_stack = 1;

		// Token: 0x0400261F RID: 9759
		public bool m_use = true;

		// Token: 0x04002620 RID: 9760
		public int m_hotbarSlot;
	}

	// Token: 0x02000371 RID: 881
	[Serializable]
	public class SetSkill
	{
		// Token: 0x04002621 RID: 9761
		public Skills.SkillType m_skill;

		// Token: 0x04002622 RID: 9762
		public int m_level;
	}
}
