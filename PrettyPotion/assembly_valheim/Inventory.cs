using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Token: 0x020000AE RID: 174
public class Inventory
{
	// Token: 0x06000ACB RID: 2763 RVA: 0x0005BDA0 File Offset: 0x00059FA0
	public Inventory(string name, Sprite bkg, int w, int h)
	{
		this.m_bkg = bkg;
		this.m_name = name;
		this.m_width = w;
		this.m_height = h;
	}

	// Token: 0x06000ACC RID: 2764 RVA: 0x0005BDF4 File Offset: 0x00059FF4
	private bool AddItem(ItemDrop.ItemData item, int amount, int x, int y)
	{
		amount = Mathf.Min(amount, item.m_stack);
		if (x < 0 || y < 0 || x >= this.m_width || y >= this.m_height)
		{
			return false;
		}
		ItemDrop.ItemData itemAt = this.GetItemAt(x, y);
		bool result;
		if (itemAt != null)
		{
			if (itemAt.m_shared.m_name != item.m_shared.m_name || itemAt.m_worldLevel != item.m_worldLevel || (itemAt.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality))
			{
				return false;
			}
			int num = itemAt.m_shared.m_maxStackSize - itemAt.m_stack;
			if (num <= 0)
			{
				return false;
			}
			int num2 = Mathf.Min(num, amount);
			itemAt.m_stack += num2;
			item.m_stack -= num2;
			result = (num2 == amount);
			ZLog.Log("Added to stack" + itemAt.m_stack.ToString() + " " + item.m_stack.ToString());
		}
		else
		{
			ItemDrop.ItemData itemData = item.Clone();
			itemData.m_stack = amount;
			itemData.m_gridPos = new Vector2i(x, y);
			this.m_inventory.Add(itemData);
			item.m_stack -= amount;
			result = true;
		}
		this.Changed();
		return result;
	}

	// Token: 0x06000ACD RID: 2765 RVA: 0x0005BF3C File Offset: 0x0005A13C
	public bool CanAddItem(GameObject prefab, int stack = -1)
	{
		ItemDrop component = prefab.GetComponent<ItemDrop>();
		return !(component == null) && this.CanAddItem(component.m_itemData, stack);
	}

	// Token: 0x06000ACE RID: 2766 RVA: 0x0005BF68 File Offset: 0x0005A168
	public bool CanAddItem(ItemDrop.ItemData item, int stack = -1)
	{
		if (stack <= 0)
		{
			stack = item.m_stack;
		}
		return this.FindFreeStackSpace(item.m_shared.m_name, (float)item.m_worldLevel) + (this.m_width * this.m_height - this.m_inventory.Count) * item.m_shared.m_maxStackSize >= stack;
	}

	// Token: 0x06000ACF RID: 2767 RVA: 0x0005BFC8 File Offset: 0x0005A1C8
	public bool AddItem(GameObject prefab, int amount)
	{
		ItemDrop.ItemData itemData = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
		itemData.m_dropPrefab = prefab;
		itemData.m_stack = Mathf.Min(amount, itemData.m_shared.m_maxStackSize);
		itemData.m_worldLevel = (int)((byte)Game.m_worldLevel);
		ZLog.Log("adding " + prefab.name + "  " + itemData.m_stack.ToString());
		return this.AddItem(itemData);
	}

	// Token: 0x06000AD0 RID: 2768 RVA: 0x0005C03C File Offset: 0x0005A23C
	public bool AddItem(ItemDrop.ItemData item)
	{
		bool result = true;
		if (item.m_shared.m_maxStackSize > 1)
		{
			int i = 0;
			while (i < item.m_stack)
			{
				ItemDrop.ItemData itemData = this.FindFreeStackItem(item.m_shared.m_name, item.m_quality, (float)item.m_worldLevel);
				if (itemData != null)
				{
					itemData.m_stack++;
					i++;
				}
				else
				{
					int stack = item.m_stack - i;
					item.m_stack = stack;
					Vector2i vector2i = this.FindEmptySlot(this.TopFirst(item));
					if (vector2i.x >= 0)
					{
						item.m_gridPos = vector2i;
						this.m_inventory.Add(item);
						break;
					}
					result = false;
					break;
				}
			}
		}
		else
		{
			Vector2i vector2i2 = this.FindEmptySlot(this.TopFirst(item));
			if (vector2i2.x >= 0)
			{
				item.m_gridPos = vector2i2;
				this.m_inventory.Add(item);
			}
			else
			{
				result = false;
			}
		}
		this.Changed();
		return result;
	}

	// Token: 0x06000AD1 RID: 2769 RVA: 0x0005C120 File Offset: 0x0005A320
	public bool AddItem(ItemDrop.ItemData item, Vector2i pos)
	{
		bool result = true;
		if (item.m_shared.m_maxStackSize > 1)
		{
			int i = 0;
			while (i < item.m_stack)
			{
				ItemDrop.ItemData itemData = this.FindFreeStackItem(item.m_shared.m_name, item.m_quality, (float)item.m_worldLevel);
				if (itemData != null)
				{
					itemData.m_stack++;
					i++;
				}
				else
				{
					int stack = item.m_stack - i;
					item.m_stack = stack;
					if (this.GetItemAt(pos.x, pos.y) == null)
					{
						item.m_gridPos = pos;
						this.m_inventory.Add(item);
						break;
					}
					result = false;
					break;
				}
			}
		}
		else if (this.GetItemAt(pos.x, pos.y) == null)
		{
			item.m_gridPos = pos;
			this.m_inventory.Add(item);
		}
		else
		{
			result = false;
		}
		this.Changed();
		return result;
	}

	// Token: 0x06000AD2 RID: 2770 RVA: 0x0005C1F4 File Offset: 0x0005A3F4
	private bool TopFirst(ItemDrop.ItemData item)
	{
		return item.IsWeapon() || (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Misc);
	}

	// Token: 0x06000AD3 RID: 2771 RVA: 0x0005C24C File Offset: 0x0005A44C
	public void MoveAll(Inventory fromInventory)
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>(fromInventory.GetAllItems());
		List<ItemDrop.ItemData> list2 = new List<ItemDrop.ItemData>();
		foreach (ItemDrop.ItemData itemData in list)
		{
			if (this.AddItem(itemData, itemData.m_stack, itemData.m_gridPos.x, itemData.m_gridPos.y))
			{
				fromInventory.RemoveItem(itemData);
			}
			else
			{
				list2.Add(itemData);
			}
		}
		foreach (ItemDrop.ItemData item in list2)
		{
			if (this.AddItem(item))
			{
				fromInventory.RemoveItem(item);
			}
		}
		this.Changed();
		fromInventory.Changed();
	}

	// Token: 0x06000AD4 RID: 2772 RVA: 0x0005C32C File Offset: 0x0005A52C
	public int StackAll(Inventory fromInventory, bool message = false)
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>(fromInventory.GetAllItems());
		int num = message ? this.CountItems(null, -1, true) : 0;
		foreach (ItemDrop.ItemData itemData in list)
		{
			if (this.ContainsItemByName(itemData.m_shared.m_name) && !Player.m_localPlayer.IsItemEquiped(itemData) && this.AddItem(itemData))
			{
				fromInventory.RemoveItem(itemData);
			}
		}
		int num2 = this.CountItems(null, -1, true) - num;
		if (message)
		{
			if (num2 > 0)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_stackall " + num2.ToString(), 0, null);
			}
			else
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_stackall_none", 0, null);
			}
		}
		this.Changed();
		fromInventory.Changed();
		Game.instance.IncrementPlayerStat(PlayerStatType.PlaceStacks, 1f);
		return num2;
	}

	// Token: 0x06000AD5 RID: 2773 RVA: 0x0005C424 File Offset: 0x0005A624
	public bool ContainsItemByName(string name)
	{
		using (List<ItemDrop.ItemData>.Enumerator enumerator = this.m_inventory.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_shared.m_name == name)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06000AD6 RID: 2774 RVA: 0x0005C488 File Offset: 0x0005A688
	public void MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item)
	{
		if (this.AddItem(item))
		{
			fromInventory.RemoveItem(item);
		}
		this.Changed();
		fromInventory.Changed();
	}

	// Token: 0x06000AD7 RID: 2775 RVA: 0x0005C4A7 File Offset: 0x0005A6A7
	public bool MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item, int amount, int x, int y)
	{
		bool result = this.AddItem(item, amount, x, y);
		if (item.m_stack == 0)
		{
			fromInventory.RemoveItem(item);
			return result;
		}
		fromInventory.Changed();
		return result;
	}

	// Token: 0x06000AD8 RID: 2776 RVA: 0x0005C4CC File Offset: 0x0005A6CC
	public bool RemoveItem(int index)
	{
		if (index < 0 || index >= this.m_inventory.Count)
		{
			return false;
		}
		this.m_inventory.RemoveAt(index);
		this.Changed();
		return true;
	}

	// Token: 0x06000AD9 RID: 2777 RVA: 0x0005C4F5 File Offset: 0x0005A6F5
	public bool ContainsItem(ItemDrop.ItemData item)
	{
		return this.m_inventory.Contains(item);
	}

	// Token: 0x06000ADA RID: 2778 RVA: 0x0005C504 File Offset: 0x0005A704
	public bool RemoveOneItem(ItemDrop.ItemData item)
	{
		if (!this.m_inventory.Contains(item))
		{
			return false;
		}
		if (item.m_stack > 1)
		{
			item.m_stack--;
			this.Changed();
		}
		else
		{
			this.m_inventory.Remove(item);
			this.Changed();
		}
		return true;
	}

	// Token: 0x06000ADB RID: 2779 RVA: 0x0005C554 File Offset: 0x0005A754
	public bool RemoveItem(ItemDrop.ItemData item)
	{
		if (!this.m_inventory.Contains(item))
		{
			ZLog.Log("Item is not in this container");
			return false;
		}
		this.m_inventory.Remove(item);
		this.Changed();
		return true;
	}

	// Token: 0x06000ADC RID: 2780 RVA: 0x0005C584 File Offset: 0x0005A784
	public bool RemoveItem(ItemDrop.ItemData item, int amount)
	{
		amount = Mathf.Min(item.m_stack, amount);
		if (amount == item.m_stack)
		{
			return this.RemoveItem(item);
		}
		if (!this.m_inventory.Contains(item))
		{
			return false;
		}
		item.m_stack -= amount;
		this.Changed();
		return true;
	}

	// Token: 0x06000ADD RID: 2781 RVA: 0x0005C5D8 File Offset: 0x0005A7D8
	public void RemoveItem(string name, int amount, int itemQuality = -1, bool worldLevelBased = true)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && (itemQuality < 0 || itemData.m_quality == itemQuality) && (!worldLevelBased || itemData.m_worldLevel >= Game.m_worldLevel))
			{
				int num = Mathf.Min(itemData.m_stack, amount);
				itemData.m_stack -= num;
				amount -= num;
				if (amount <= 0)
				{
					break;
				}
			}
		}
		this.m_inventory.RemoveAll((ItemDrop.ItemData x) => x.m_stack <= 0);
		this.Changed();
	}

	// Token: 0x06000ADE RID: 2782 RVA: 0x0005C6AC File Offset: 0x0005A8AC
	public bool HaveItem(string name, bool matchWorldLevel = true)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && (!matchWorldLevel || itemData.m_worldLevel >= Game.m_worldLevel))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000ADF RID: 2783 RVA: 0x0005C724 File Offset: 0x0005A924
	public bool HaveItem(ItemDrop.ItemData.ItemType type, bool matchWorldLevel = true)
	{
		return this.m_inventory.Any((ItemDrop.ItemData i) => i.m_shared.m_itemType == type && (!matchWorldLevel || i.m_worldLevel >= Game.m_worldLevel));
	}

	// Token: 0x06000AE0 RID: 2784 RVA: 0x0005C75C File Offset: 0x0005A95C
	public void GetAllPieceTables(List<PieceTable> tables)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_buildPieces != null && !tables.Contains(itemData.m_shared.m_buildPieces))
			{
				tables.Add(itemData.m_shared.m_buildPieces);
			}
		}
	}

	// Token: 0x06000AE1 RID: 2785 RVA: 0x0005C7E0 File Offset: 0x0005A9E0
	public int CountItems(string name, int quality = -1, bool matchWorldLevel = true)
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if ((name == null || itemData.m_shared.m_name == name) && (quality < 0 || quality == itemData.m_quality) && (!matchWorldLevel || itemData.m_worldLevel >= Game.m_worldLevel))
			{
				num += itemData.m_stack;
			}
		}
		return num;
	}

	// Token: 0x06000AE2 RID: 2786 RVA: 0x0005C86C File Offset: 0x0005AA6C
	public int CountItemsByName(string[] names, int quality = -1, bool matchWorldLevel = true, bool stacksOnly = false)
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if ((names == null || names.Contains(itemData.m_shared.m_name)) && (quality < 0 || quality == itemData.m_quality) && (!matchWorldLevel || itemData.m_worldLevel >= Game.m_worldLevel))
			{
				num += (stacksOnly ? 1 : itemData.m_stack);
			}
		}
		return num;
	}

	// Token: 0x06000AE3 RID: 2787 RVA: 0x0005C900 File Offset: 0x0005AB00
	public int CountItemsByType(ItemDrop.ItemData.ItemType type, int quality = -1, bool matchWorldLevel = true, bool stacksOnly = false)
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if ((type == ItemDrop.ItemData.ItemType.None || itemData.m_shared.m_itemType == type) && (quality < 0 || quality == itemData.m_quality) && (!matchWorldLevel || itemData.m_worldLevel >= Game.m_worldLevel))
			{
				num += (stacksOnly ? 1 : itemData.m_stack);
			}
		}
		return num;
	}

	// Token: 0x06000AE4 RID: 2788 RVA: 0x0005C990 File Offset: 0x0005AB90
	public int CountItemsByType(ItemDrop.ItemData.ItemType[] types, int quality = -1, bool matchWorldLevel = true, bool stacksOnly = false)
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if ((itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.None || types.Contains(itemData.m_shared.m_itemType)) && (quality < 0 || quality == itemData.m_quality) && (!matchWorldLevel || itemData.m_worldLevel >= Game.m_worldLevel))
			{
				num += (stacksOnly ? 1 : itemData.m_stack);
			}
		}
		return num;
	}

	// Token: 0x06000AE5 RID: 2789 RVA: 0x0005CA2C File Offset: 0x0005AC2C
	public ItemDrop.ItemData GetItem(int index)
	{
		return this.m_inventory[index];
	}

	// Token: 0x06000AE6 RID: 2790 RVA: 0x0005CA3C File Offset: 0x0005AC3C
	public ItemDrop.ItemData GetItem(string name, int quality = -1, bool isPrefabName = false)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (((isPrefabName && itemData.m_dropPrefab.name == name) || (!isPrefabName && itemData.m_shared.m_name == name)) && (quality < 0 || quality == itemData.m_quality) && itemData.m_worldLevel >= Game.m_worldLevel)
			{
				return itemData;
			}
		}
		return null;
	}

	// Token: 0x06000AE7 RID: 2791 RVA: 0x0005CAD8 File Offset: 0x0005ACD8
	public ItemDrop.ItemData GetAmmoItem(string ammoName, string matchPrefabName = null)
	{
		int num = 0;
		ItemDrop.ItemData itemData = null;
		foreach (ItemDrop.ItemData itemData2 in this.m_inventory)
		{
			if ((itemData2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || itemData2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable || itemData2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable) && itemData2.m_shared.m_ammoType == ammoName && (matchPrefabName == null || itemData2.m_dropPrefab.name == matchPrefabName))
			{
				int num2 = itemData2.m_gridPos.y * this.m_width + itemData2.m_gridPos.x;
				if (num2 < num || itemData == null)
				{
					num = num2;
					itemData = itemData2;
				}
			}
		}
		return itemData;
	}

	// Token: 0x06000AE8 RID: 2792 RVA: 0x0005CBB4 File Offset: 0x0005ADB4
	public int FindFreeStackSpace(string name, float worldLevel)
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && itemData.m_stack < itemData.m_shared.m_maxStackSize && (float)itemData.m_worldLevel == worldLevel)
			{
				num += itemData.m_shared.m_maxStackSize - itemData.m_stack;
			}
		}
		return num;
	}

	// Token: 0x06000AE9 RID: 2793 RVA: 0x0005CC48 File Offset: 0x0005AE48
	private ItemDrop.ItemData FindFreeStackItem(string name, int quality, float worldLevel)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && itemData.m_quality == quality && itemData.m_stack < itemData.m_shared.m_maxStackSize && (float)itemData.m_worldLevel == worldLevel)
			{
				return itemData;
			}
		}
		return null;
	}

	// Token: 0x06000AEA RID: 2794 RVA: 0x0005CCD4 File Offset: 0x0005AED4
	public int NrOfItems()
	{
		return this.m_inventory.Count;
	}

	// Token: 0x06000AEB RID: 2795 RVA: 0x0005CCE4 File Offset: 0x0005AEE4
	public int NrOfItemsIncludingStacks()
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			num += itemData.m_stack;
		}
		return num;
	}

	// Token: 0x06000AEC RID: 2796 RVA: 0x0005CD3C File Offset: 0x0005AF3C
	public float SlotsUsedPercentage()
	{
		return (float)this.m_inventory.Count / (float)(this.m_width * this.m_height) * 100f;
	}

	// Token: 0x06000AED RID: 2797 RVA: 0x0005CD60 File Offset: 0x0005AF60
	public void Print()
	{
		for (int i = 0; i < this.m_inventory.Count; i++)
		{
			ItemDrop.ItemData itemData = this.m_inventory[i];
			ZLog.Log(string.Concat(new string[]
			{
				i.ToString(),
				": ",
				itemData.m_shared.m_name,
				"  ",
				itemData.m_stack.ToString(),
				" / ",
				itemData.m_shared.m_maxStackSize.ToString()
			}));
		}
	}

	// Token: 0x06000AEE RID: 2798 RVA: 0x0005CDF1 File Offset: 0x0005AFF1
	public int GetEmptySlots()
	{
		return this.m_height * this.m_width - this.m_inventory.Count;
	}

	// Token: 0x06000AEF RID: 2799 RVA: 0x0005CE0C File Offset: 0x0005B00C
	public bool HaveEmptySlot()
	{
		return this.m_inventory.Count < this.m_width * this.m_height;
	}

	// Token: 0x06000AF0 RID: 2800 RVA: 0x0005CE28 File Offset: 0x0005B028
	private Vector2i FindEmptySlot(bool topFirst)
	{
		if (topFirst)
		{
			for (int i = 0; i < this.m_height; i++)
			{
				for (int j = 0; j < this.m_width; j++)
				{
					if (this.GetItemAt(j, i) == null)
					{
						return new Vector2i(j, i);
					}
				}
			}
		}
		else
		{
			for (int k = this.m_height - 1; k >= 0; k--)
			{
				for (int l = 0; l < this.m_width; l++)
				{
					if (this.GetItemAt(l, k) == null)
					{
						return new Vector2i(l, k);
					}
				}
			}
		}
		return new Vector2i(-1, -1);
	}

	// Token: 0x06000AF1 RID: 2801 RVA: 0x0005CEAC File Offset: 0x0005B0AC
	public ItemDrop.ItemData GetOtherItemAt(int x, int y, ItemDrop.ItemData oldItem)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData != oldItem && itemData.m_gridPos.x == x && itemData.m_gridPos.y == y)
			{
				return itemData;
			}
		}
		return null;
	}

	// Token: 0x06000AF2 RID: 2802 RVA: 0x0005CF20 File Offset: 0x0005B120
	public ItemDrop.ItemData GetItemAt(int x, int y)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_gridPos.x == x && itemData.m_gridPos.y == y)
			{
				return itemData;
			}
		}
		return null;
	}

	// Token: 0x06000AF3 RID: 2803 RVA: 0x0005CF90 File Offset: 0x0005B190
	public List<ItemDrop.ItemData> GetEquippedItems()
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_equipped)
			{
				list.Add(itemData);
			}
		}
		return list;
	}

	// Token: 0x06000AF4 RID: 2804 RVA: 0x0005CFF4 File Offset: 0x0005B1F4
	public void GetWornItems(List<ItemDrop.ItemData> worn)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_useDurability && itemData.m_durability < itemData.GetMaxDurability())
			{
				worn.Add(itemData);
			}
		}
	}

	// Token: 0x06000AF5 RID: 2805 RVA: 0x0005D064 File Offset: 0x0005B264
	public void GetValuableItems(List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_value > 0)
			{
				items.Add(itemData);
			}
		}
	}

	// Token: 0x06000AF6 RID: 2806 RVA: 0x0005D0C8 File Offset: 0x0005B2C8
	public List<ItemDrop.ItemData> GetAllItems()
	{
		return this.m_inventory;
	}

	// Token: 0x06000AF7 RID: 2807 RVA: 0x0005D0D0 File Offset: 0x0005B2D0
	public List<ItemDrop.ItemData> GetAllItemsSortedByName()
	{
		return (from item in this.m_inventory
		orderby item.m_shared.m_name
		select item).ToList<ItemDrop.ItemData>();
	}

	// Token: 0x06000AF8 RID: 2808 RVA: 0x0005D104 File Offset: 0x0005B304
	public List<ItemDrop.ItemData> GetAllItemsInGridOrder()
	{
		return (from i in this.m_inventory
		orderby i.m_gridPos.y, i.m_gridPos.x
		select i).ToList<ItemDrop.ItemData>();
	}

	// Token: 0x06000AF9 RID: 2809 RVA: 0x0005D164 File Offset: 0x0005B364
	public List<ItemDrop.ItemData> GetAllItemsOfType(ItemDrop.ItemData.ItemType type, bool sortByGridOrder = false)
	{
		if (sortByGridOrder)
		{
			return (from i in this.m_inventory
			where i.m_shared.m_itemType == type
			orderby i.m_gridPos.y, i.m_gridPos.x
			select i).ToList<ItemDrop.ItemData>();
		}
		return (from i in this.m_inventory
		where i.m_shared.m_itemType == type
		select i).ToList<ItemDrop.ItemData>();
	}

	// Token: 0x06000AFA RID: 2810 RVA: 0x0005D204 File Offset: 0x0005B404
	public List<ItemDrop.ItemData> GetAllItemsOfType(ItemDrop.ItemData.ItemType[] types, bool sortByGridOrder = false)
	{
		if (sortByGridOrder)
		{
			return (from i in this.m_inventory
			where types.Contains(i.m_shared.m_itemType)
			orderby i.m_gridPos.y, i.m_gridPos.x
			select i).ToList<ItemDrop.ItemData>();
		}
		return (from i in this.m_inventory
		where types.Contains(i.m_shared.m_itemType)
		select i).ToList<ItemDrop.ItemData>();
	}

	// Token: 0x06000AFB RID: 2811 RVA: 0x0005D2A4 File Offset: 0x0005B4A4
	public void GetAllItems(string name, List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && itemData.m_worldLevel >= Game.m_worldLevel)
			{
				items.Add(itemData);
			}
		}
	}

	// Token: 0x06000AFC RID: 2812 RVA: 0x0005D318 File Offset: 0x0005B518
	public void GetAllItems(ItemDrop.ItemData.ItemType type, List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_itemType == type)
			{
				items.Add(itemData);
			}
		}
	}

	// Token: 0x06000AFD RID: 2813 RVA: 0x0005D37C File Offset: 0x0005B57C
	public int GetWidth()
	{
		return this.m_width;
	}

	// Token: 0x06000AFE RID: 2814 RVA: 0x0005D384 File Offset: 0x0005B584
	public int GetHeight()
	{
		return this.m_height;
	}

	// Token: 0x06000AFF RID: 2815 RVA: 0x0005D38C File Offset: 0x0005B58C
	public string GetName()
	{
		return this.m_name;
	}

	// Token: 0x06000B00 RID: 2816 RVA: 0x0005D394 File Offset: 0x0005B594
	public Sprite GetBkg()
	{
		return this.m_bkg;
	}

	// Token: 0x06000B01 RID: 2817 RVA: 0x0005D39C File Offset: 0x0005B59C
	public void Save(ZPackage pkg)
	{
		pkg.Write(106);
		pkg.Write(this.m_inventory.Count);
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_dropPrefab == null)
			{
				ZLog.Log("Item missing prefab " + itemData.m_shared.m_name);
				pkg.Write("");
			}
			else
			{
				pkg.Write(itemData.m_dropPrefab.name);
			}
			pkg.Write(itemData.m_stack);
			pkg.Write(itemData.m_durability);
			pkg.Write(itemData.m_gridPos);
			pkg.Write(itemData.m_equipped);
			pkg.Write(itemData.m_quality);
			pkg.Write(itemData.m_variant);
			pkg.Write(itemData.m_crafterID);
			pkg.Write(itemData.m_crafterName);
			pkg.Write(itemData.m_customData.Count);
			foreach (KeyValuePair<string, string> keyValuePair in itemData.m_customData)
			{
				pkg.Write(keyValuePair.Key);
				pkg.Write(keyValuePair.Value);
			}
			pkg.Write(itemData.m_worldLevel);
			pkg.Write(itemData.m_pickedUp);
		}
	}

	// Token: 0x06000B02 RID: 2818 RVA: 0x0005D544 File Offset: 0x0005B744
	public void Load(ZPackage pkg)
	{
		int num = pkg.ReadInt();
		int num2 = pkg.ReadInt();
		this.m_inventory.Clear();
		if (num == 106)
		{
			for (int i = 0; i < num2; i++)
			{
				string text = pkg.ReadString();
				int stack = pkg.ReadInt();
				float durability = pkg.ReadSingle();
				Vector2i pos = pkg.ReadVector2i();
				bool equipped = pkg.ReadBool();
				int quality = pkg.ReadInt();
				int variant = pkg.ReadInt();
				long crafterID = pkg.ReadLong();
				string crafterName = pkg.ReadString();
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				int num3 = pkg.ReadInt();
				for (int j = 0; j < num3; j++)
				{
					dictionary[pkg.ReadString()] = pkg.ReadString();
				}
				int worldLevel = pkg.ReadInt();
				bool pickedUp = pkg.ReadBool();
				if (text != "")
				{
					this.AddItem(text, stack, durability, pos, equipped, quality, variant, crafterID, crafterName, dictionary, worldLevel, pickedUp);
				}
			}
		}
		else
		{
			for (int k = 0; k < num2; k++)
			{
				string text2 = pkg.ReadString();
				int stack2 = pkg.ReadInt();
				float durability2 = pkg.ReadSingle();
				Vector2i pos2 = pkg.ReadVector2i();
				bool equipped2 = pkg.ReadBool();
				int quality2 = 1;
				if (num >= 101)
				{
					quality2 = pkg.ReadInt();
				}
				int variant2 = 0;
				if (num >= 102)
				{
					variant2 = pkg.ReadInt();
				}
				long crafterID2 = 0L;
				string crafterName2 = "";
				if (num >= 103)
				{
					crafterID2 = pkg.ReadLong();
					crafterName2 = pkg.ReadString();
				}
				Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
				if (num >= 104)
				{
					int num4 = pkg.ReadInt();
					for (int l = 0; l < num4; l++)
					{
						string key = pkg.ReadString();
						string value = pkg.ReadString();
						dictionary2[key] = value;
					}
				}
				int worldLevel2 = 0;
				if (num >= 105)
				{
					worldLevel2 = pkg.ReadInt();
				}
				bool pickedUp2 = false;
				if (num >= 106)
				{
					pickedUp2 = pkg.ReadBool();
				}
				if (text2 != "")
				{
					this.AddItem(text2, stack2, durability2, pos2, equipped2, quality2, variant2, crafterID2, crafterName2, dictionary2, worldLevel2, pickedUp2);
				}
			}
		}
		this.Changed();
	}

	// Token: 0x06000B03 RID: 2819 RVA: 0x0005D750 File Offset: 0x0005B950
	public ItemDrop.ItemData AddItem(string name, int stack, int quality, int variant, long crafterID, string crafterName, bool pickedUp = false)
	{
		return this.AddItem(name, stack, quality, variant, crafterID, crafterName, new Vector2i(-1, -1), pickedUp);
	}

	// Token: 0x06000B04 RID: 2820 RVA: 0x0005D778 File Offset: 0x0005B978
	public ItemDrop.ItemData AddItem(string name, int stack, int quality, int variant, long crafterID, string crafterName, Vector2i position, bool pickedUp = false)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		if (itemPrefab == null)
		{
			ZLog.Log("Failed to find item prefab " + name);
			return null;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component == null)
		{
			ZLog.Log("Invalid item " + name);
			return null;
		}
		if (component.m_itemData.m_shared.m_maxStackSize <= 1 && this.FindEmptySlot(this.TopFirst(component.m_itemData)).x == -1)
		{
			return null;
		}
		ItemDrop.ItemData result = null;
		int i = stack;
		while (i > 0)
		{
			ZNetView.m_forceDisableInit = true;
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab);
			ZNetView.m_forceDisableInit = false;
			ItemDrop component2 = gameObject.GetComponent<ItemDrop>();
			if (component2 == null)
			{
				ZLog.Log("Missing itemdrop in " + name);
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
			int num = Mathf.Min(i, component2.m_itemData.m_shared.m_maxStackSize);
			i -= num;
			component2.m_itemData.m_stack = num;
			component2.SetQuality(quality);
			component2.m_itemData.m_variant = variant;
			component2.m_itemData.m_durability = component2.m_itemData.GetMaxDurability();
			component2.m_itemData.m_crafterID = crafterID;
			component2.m_itemData.m_crafterName = crafterName;
			component2.m_itemData.m_worldLevel = (int)((byte)Game.m_worldLevel);
			component2.m_itemData.m_pickedUp = pickedUp;
			bool flag;
			if (position.x < 0 || position.y < 0 || position.x >= this.m_width || position.y >= this.m_height)
			{
				flag = this.AddItem(component2.m_itemData);
			}
			else
			{
				flag = this.AddItem(component2.m_itemData, position);
			}
			if (!flag)
			{
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
			result = component2.m_itemData;
			UnityEngine.Object.Destroy(gameObject);
		}
		return result;
	}

	// Token: 0x06000B05 RID: 2821 RVA: 0x0005D954 File Offset: 0x0005BB54
	private bool AddItem(string name, int stack, float durability, Vector2i pos, bool equipped, int quality, int variant, long crafterID, string crafterName, Dictionary<string, string> customData, int worldLevel, bool pickedUp)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		if (itemPrefab == null)
		{
			ZLog.Log("Failed to find item prefab " + name);
			return false;
		}
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab);
		ZNetView.m_forceDisableInit = false;
		ItemDrop component = gameObject.GetComponent<ItemDrop>();
		if (component == null)
		{
			ZLog.Log("Missing itemdrop in " + name);
			UnityEngine.Object.Destroy(gameObject);
			return false;
		}
		component.m_itemData.m_stack = Mathf.Min(stack, component.m_itemData.m_shared.m_maxStackSize);
		component.m_itemData.m_durability = durability;
		component.m_itemData.m_equipped = equipped;
		component.SetQuality(quality);
		component.m_itemData.m_variant = variant;
		component.m_itemData.m_crafterID = crafterID;
		component.m_itemData.m_crafterName = crafterName;
		component.m_itemData.m_customData = customData;
		component.m_itemData.m_worldLevel = (int)((byte)worldLevel);
		component.m_itemData.m_pickedUp = pickedUp;
		this.AddItem(component.m_itemData, component.m_itemData.m_stack, pos.x, pos.y);
		UnityEngine.Object.Destroy(gameObject);
		return true;
	}

	// Token: 0x06000B06 RID: 2822 RVA: 0x0005DA84 File Offset: 0x0005BC84
	public void MoveInventoryToGrave(Inventory original)
	{
		this.m_inventory.Clear();
		this.m_width = original.m_width;
		this.m_height = original.m_height;
		foreach (ItemDrop.ItemData itemData in original.m_inventory)
		{
			if (!itemData.m_shared.m_questItem && !itemData.m_equipped)
			{
				this.m_inventory.Add(itemData);
			}
		}
		original.m_inventory.RemoveAll((ItemDrop.ItemData x) => !x.m_shared.m_questItem && !x.m_equipped);
		original.Changed();
		this.Changed();
	}

	// Token: 0x06000B07 RID: 2823 RVA: 0x0005DB4C File Offset: 0x0005BD4C
	private void Changed()
	{
		this.UpdateTotalWeight();
		if (this.m_onChanged != null)
		{
			this.m_onChanged();
		}
	}

	// Token: 0x06000B08 RID: 2824 RVA: 0x0005DB67 File Offset: 0x0005BD67
	public void RemoveAll()
	{
		this.m_inventory.Clear();
		this.Changed();
	}

	// Token: 0x06000B09 RID: 2825 RVA: 0x0005DB7A File Offset: 0x0005BD7A
	public void RemoveUnequipped()
	{
		this.m_inventory.RemoveAll((ItemDrop.ItemData x) => !x.m_shared.m_questItem && !x.m_equipped);
		this.Changed();
	}

	// Token: 0x06000B0A RID: 2826 RVA: 0x0005DBB0 File Offset: 0x0005BDB0
	private void UpdateTotalWeight()
	{
		this.m_totalWeight = 0f;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			this.m_totalWeight += itemData.GetWeight(-1);
		}
	}

	// Token: 0x06000B0B RID: 2827 RVA: 0x0005DC1C File Offset: 0x0005BE1C
	public float GetTotalWeight()
	{
		return this.m_totalWeight;
	}

	// Token: 0x06000B0C RID: 2828 RVA: 0x0005DC24 File Offset: 0x0005BE24
	public void GetBoundItems(List<ItemDrop.ItemData> bound)
	{
		bound.Clear();
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_gridPos.y == 0)
			{
				bound.Add(itemData);
			}
		}
	}

	// Token: 0x06000B0D RID: 2829 RVA: 0x0005DC8C File Offset: 0x0005BE8C
	public List<ItemDrop.ItemData> GetHotbar(bool includeEmpty = false)
	{
		int i;
		if (!includeEmpty)
		{
			return (from i in this.m_inventory
			where i.m_gridPos.y == 0
			orderby i.m_gridPos.x
			select i).ToList<ItemDrop.ItemData>();
		}
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		List<ItemDrop.ItemData> source = (from i in this.m_inventory
		where i.m_gridPos.y == 0
		select i).ToList<ItemDrop.ItemData>();
		int j;
		for (i = 0; i < 8; i = j + 1)
		{
			list.Add(source.FirstOrDefault((ItemDrop.ItemData item) => item.m_gridPos.x == i));
			j = i;
		}
		return list;
	}

	// Token: 0x06000B0E RID: 2830 RVA: 0x0005DD68 File Offset: 0x0005BF68
	public bool IsTeleportable()
	{
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll))
		{
			return true;
		}
		using (List<ItemDrop.ItemData>.Enumerator enumerator = this.m_inventory.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.m_shared.m_teleportable)
				{
					return false;
				}
			}
		}
		return true;
	}

	// Token: 0x04000C53 RID: 3155
	public Action m_onChanged;

	// Token: 0x04000C54 RID: 3156
	private string m_name = "";

	// Token: 0x04000C55 RID: 3157
	private Sprite m_bkg;

	// Token: 0x04000C56 RID: 3158
	private List<ItemDrop.ItemData> m_inventory = new List<ItemDrop.ItemData>();

	// Token: 0x04000C57 RID: 3159
	private int m_width = 4;

	// Token: 0x04000C58 RID: 3160
	private int m_height = 4;

	// Token: 0x04000C59 RID: 3161
	private float m_totalWeight;
}
