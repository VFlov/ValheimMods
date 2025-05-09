using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200012B RID: 299
public class ObjectDB : MonoBehaviour
{
	// Token: 0x170000A3 RID: 163
	// (get) Token: 0x060012D7 RID: 4823 RVA: 0x0008C5ED File Offset: 0x0008A7ED
	public static ObjectDB instance
	{
		get
		{
			return ObjectDB.m_instance;
		}
	}

	// Token: 0x060012D8 RID: 4824 RVA: 0x0008C5F4 File Offset: 0x0008A7F4
	private void Awake()
	{
		ObjectDB.m_instance = this;
		this.UpdateRegisters();
	}

	// Token: 0x060012D9 RID: 4825 RVA: 0x0008C602 File Offset: 0x0008A802
	public void CopyOtherDB(ObjectDB other)
	{
		this.m_items = other.m_items;
		this.m_recipes = other.m_recipes;
		this.m_StatusEffects = other.m_StatusEffects;
		this.UpdateRegisters();
	}

	// Token: 0x060012DA RID: 4826 RVA: 0x0008C630 File Offset: 0x0008A830
	private void UpdateRegisters()
	{
		this.m_itemByHash.Clear();
		this.m_itemByData.Clear();
		foreach (GameObject gameObject in this.m_items)
		{
			this.m_itemByHash.Add(gameObject.name.GetStableHashCode(), gameObject);
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			if (component != null)
			{
				this.m_itemByData[component.m_itemData.m_shared] = gameObject;
			}
		}
	}

	// Token: 0x060012DB RID: 4827 RVA: 0x0008C6CC File Offset: 0x0008A8CC
	public StatusEffect GetStatusEffect(int nameHash)
	{
		foreach (StatusEffect statusEffect in this.m_StatusEffects)
		{
			if (statusEffect.NameHash() == nameHash)
			{
				return statusEffect;
			}
		}
		return null;
	}

	// Token: 0x060012DC RID: 4828 RVA: 0x0008C728 File Offset: 0x0008A928
	public GameObject GetItemPrefab(string name)
	{
		return this.GetItemPrefab(name.GetStableHashCode());
	}

	// Token: 0x060012DD RID: 4829 RVA: 0x0008C738 File Offset: 0x0008A938
	public GameObject GetItemPrefab(int hash)
	{
		GameObject result;
		if (this.TryGetItemPrefab(hash, out result))
		{
			return result;
		}
		return null;
	}

	// Token: 0x060012DE RID: 4830 RVA: 0x0008C754 File Offset: 0x0008A954
	public GameObject GetItemPrefab(ItemDrop.ItemData.SharedData sharedData)
	{
		GameObject result;
		if (this.TryGetItemPrefab(sharedData, out result))
		{
			return result;
		}
		return null;
	}

	// Token: 0x060012DF RID: 4831 RVA: 0x0008C76F File Offset: 0x0008A96F
	public bool TryGetItemPrefab(string name, out GameObject prefab)
	{
		return this.TryGetItemPrefab(name.GetStableHashCode(), out prefab);
	}

	// Token: 0x060012E0 RID: 4832 RVA: 0x0008C77E File Offset: 0x0008A97E
	public bool TryGetItemPrefab(int hash, out GameObject prefab)
	{
		return this.m_itemByHash.TryGetValue(hash, out prefab);
	}

	// Token: 0x060012E1 RID: 4833 RVA: 0x0008C78D File Offset: 0x0008A98D
	public bool TryGetItemPrefab(ItemDrop.ItemData.SharedData sharedData, out GameObject prefab)
	{
		return this.m_itemByData.TryGetValue(sharedData, out prefab);
	}

	// Token: 0x060012E2 RID: 4834 RVA: 0x0008C79C File Offset: 0x0008A99C
	public int GetPrefabHash(GameObject prefab)
	{
		return prefab.name.GetStableHashCode();
	}

	// Token: 0x060012E3 RID: 4835 RVA: 0x0008C7AC File Offset: 0x0008A9AC
	public List<ItemDrop> GetAllItems(ItemDrop.ItemData.ItemType type, string startWith)
	{
		List<ItemDrop> list = new List<ItemDrop>();
		foreach (GameObject gameObject in this.m_items)
		{
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			if (component.m_itemData.m_shared.m_itemType == type && component.gameObject.name.CustomStartsWith(startWith))
			{
				list.Add(component);
			}
		}
		return list;
	}

	// Token: 0x060012E4 RID: 4836 RVA: 0x0008C834 File Offset: 0x0008AA34
	public Recipe GetRecipe(ItemDrop.ItemData item)
	{
		foreach (Recipe recipe in this.m_recipes)
		{
			if (!(recipe.m_item == null) && recipe.m_item.m_itemData.m_shared.m_name == item.m_shared.m_name)
			{
				return recipe;
			}
		}
		return null;
	}

	// Token: 0x04001294 RID: 4756
	private static ObjectDB m_instance;

	// Token: 0x04001295 RID: 4757
	public List<StatusEffect> m_StatusEffects = new List<StatusEffect>();

	// Token: 0x04001296 RID: 4758
	public List<GameObject> m_items = new List<GameObject>();

	// Token: 0x04001297 RID: 4759
	public List<Recipe> m_recipes = new List<Recipe>();

	// Token: 0x04001298 RID: 4760
	private Dictionary<int, GameObject> m_itemByHash = new Dictionary<int, GameObject>();

	// Token: 0x04001299 RID: 4761
	private Dictionary<ItemDrop.ItemData.SharedData, GameObject> m_itemByData = new Dictionary<ItemDrop.ItemData.SharedData, GameObject>();
}
