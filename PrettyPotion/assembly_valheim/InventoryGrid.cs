using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000079 RID: 121
public class InventoryGrid : MonoBehaviour
{
	// Token: 0x060007C7 RID: 1991 RVA: 0x000434D2 File Offset: 0x000416D2
	protected void Awake()
	{
	}

	// Token: 0x060007C8 RID: 1992 RVA: 0x000434D4 File Offset: 0x000416D4
	public void ResetView()
	{
		RectTransform rectTransform = base.transform as RectTransform;
		if (this.m_gridRoot.rect.height > rectTransform.rect.height)
		{
			this.m_gridRoot.pivot = new Vector2(this.m_gridRoot.pivot.x, 1f);
		}
		else
		{
			this.m_gridRoot.pivot = new Vector2(this.m_gridRoot.pivot.x, 0.5f);
		}
		this.m_gridRoot.anchoredPosition = new Vector2(0f, 0f);
	}

	// Token: 0x060007C9 RID: 1993 RVA: 0x00043576 File Offset: 0x00041776
	public void UpdateInventory(Inventory inventory, Player player, ItemDrop.ItemData dragItem)
	{
		this.m_inventory = inventory;
		this.UpdateGamepad();
		this.UpdateGui(player, dragItem);
	}

	// Token: 0x060007CA RID: 1994 RVA: 0x00043590 File Offset: 0x00041790
	private void UpdateGamepad()
	{
		if (!this.m_uiGroup.IsActive || global::Console.IsVisible())
		{
			return;
		}
		if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft"))
		{
			this.m_selected.x = Mathf.Max(0, this.m_selected.x - 1);
		}
		if (ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight"))
		{
			this.m_selected.x = Mathf.Min(this.m_width - 1, this.m_selected.x + 1);
		}
		if (ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp"))
		{
			if (this.m_selected.y - 1 < 0)
			{
				if (!this.jumpToNextContainer)
				{
					return;
				}
				Action<Vector2i> onMoveToUpperInventoryGrid = this.OnMoveToUpperInventoryGrid;
				if (onMoveToUpperInventoryGrid != null)
				{
					onMoveToUpperInventoryGrid(this.m_selected);
				}
			}
			else
			{
				this.m_selected.y = Mathf.Max(0, this.m_selected.y - 1);
				this.jumpToNextContainer = false;
			}
		}
		if (!ZInput.GetButton("JoyDPadUp") && !ZInput.GetButton("JoyLStickUp") && this.m_selected.y - 1 <= 0)
		{
			this.jumpToNextContainer = true;
		}
		if (ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown"))
		{
			if (this.m_selected.y + 1 > this.m_height - 1)
			{
				if (!this.jumpToNextContainer)
				{
					return;
				}
				Action<Vector2i> onMoveToLowerInventoryGrid = this.OnMoveToLowerInventoryGrid;
				if (onMoveToLowerInventoryGrid != null)
				{
					onMoveToLowerInventoryGrid(this.m_selected);
				}
			}
			else
			{
				this.m_selected.y = Mathf.Min(this.m_width - 1, this.m_selected.y + 1);
				this.jumpToNextContainer = false;
			}
		}
		if (!ZInput.GetButton("JoyDPadDown") && !ZInput.GetButton("JoyLStickDown") && this.m_selected.y + 1 >= this.m_height - 1)
		{
			this.jumpToNextContainer = true;
		}
		if (ZInput.GetButtonDown("JoyButtonA"))
		{
			InventoryGrid.Modifier arg = InventoryGrid.Modifier.Select;
			if (ZInput.GetButton("JoyLTrigger"))
			{
				arg = InventoryGrid.Modifier.Split;
			}
			if (ZInput.GetButton("JoyRTrigger"))
			{
				arg = InventoryGrid.Modifier.Drop;
			}
			ItemDrop.ItemData gamepadSelectedItem = this.GetGamepadSelectedItem();
			this.m_onSelected(this, gamepadSelectedItem, this.m_selected, arg);
		}
		if (ZInput.GetButtonDown("JoyButtonX"))
		{
			ItemDrop.ItemData gamepadSelectedItem2 = this.GetGamepadSelectedItem();
			if (ZInput.GetButton("JoyLTrigger"))
			{
				this.m_onSelected(this, gamepadSelectedItem2, this.m_selected, InventoryGrid.Modifier.Move);
				return;
			}
			this.m_onRightClick(this, gamepadSelectedItem2, this.m_selected);
		}
	}

	// Token: 0x060007CB RID: 1995 RVA: 0x00043804 File Offset: 0x00041A04
	private void UpdateGui(Player player, ItemDrop.ItemData dragItem)
	{
		RectTransform rectTransform = base.transform as RectTransform;
		int width = this.m_inventory.GetWidth();
		int height = this.m_inventory.GetHeight();
		if (this.m_selected.x >= width - 1)
		{
			this.m_selected.x = width - 1;
		}
		if (this.m_selected.y >= height - 1)
		{
			this.m_selected.y = height - 1;
		}
		if (this.m_width != width || this.m_height != height)
		{
			this.m_width = width;
			this.m_height = height;
			foreach (InventoryGrid.Element element in this.m_elements)
			{
				UnityEngine.Object.Destroy(element.m_go);
			}
			this.m_elements.Clear();
			Vector2 widgetSize = this.GetWidgetSize();
			Vector2 a = new Vector2(rectTransform.rect.width / 2f, 0f) - new Vector2(widgetSize.x, 0f) * 0.5f;
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					Vector2 b = new Vector3((float)j * this.m_elementSpace, (float)i * -this.m_elementSpace);
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, this.m_gridRoot);
					(gameObject.transform as RectTransform).anchoredPosition = a + b;
					UIInputHandler componentInChildren = gameObject.GetComponentInChildren<UIInputHandler>();
					componentInChildren.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onRightDown, new Action<UIInputHandler>(this.OnRightClick));
					componentInChildren.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onLeftDown, new Action<UIInputHandler>(this.OnLeftClick));
					TMP_Text component = gameObject.transform.Find("binding").GetComponent<TMP_Text>();
					if (player && i == 0)
					{
						component.text = (j + 1).ToString();
					}
					else
					{
						component.enabled = false;
					}
					InventoryGrid.Element element2 = new InventoryGrid.Element();
					element2.m_pos = new Vector2i(j, i);
					element2.m_go = gameObject;
					element2.m_icon = gameObject.transform.Find("icon").GetComponent<Image>();
					element2.m_amount = gameObject.transform.Find("amount").GetComponent<TMP_Text>();
					element2.m_quality = gameObject.transform.Find("quality").GetComponent<TMP_Text>();
					element2.m_equiped = gameObject.transform.Find("equiped").GetComponent<Image>();
					element2.m_queued = gameObject.transform.Find("queued").GetComponent<Image>();
					element2.m_noteleport = gameObject.transform.Find("noteleport").GetComponent<Image>();
					element2.m_food = gameObject.transform.Find("foodicon").GetComponent<Image>();
					element2.m_selected = gameObject.transform.Find("selected").gameObject;
					element2.m_tooltip = gameObject.GetComponent<UITooltip>();
					element2.m_durability = gameObject.transform.Find("durability").GetComponent<GuiBar>();
					this.m_elements.Add(element2);
				}
			}
		}
		foreach (InventoryGrid.Element element3 in this.m_elements)
		{
			element3.m_used = false;
		}
		bool flag = this.m_uiGroup.IsActive && ZInput.IsGamepadActive();
		List<ItemDrop.ItemData> allItems = this.m_inventory.GetAllItems();
		InventoryGrid.Element element4 = flag ? this.GetElement(this.m_selected.x, this.m_selected.y, width) : this.GetHoveredElement();
		foreach (ItemDrop.ItemData itemData in allItems)
		{
			InventoryGrid.Element element5 = this.GetElement(itemData.m_gridPos.x, itemData.m_gridPos.y, width);
			element5.m_used = true;
			element5.m_icon.enabled = true;
			element5.m_icon.sprite = itemData.GetIcon();
			element5.m_icon.color = ((itemData == dragItem) ? Color.grey : Color.white);
			bool flag2 = itemData.m_shared.m_useDurability && itemData.m_durability < itemData.GetMaxDurability();
			element5.m_durability.gameObject.SetActive(flag2);
			if (flag2)
			{
				if (itemData.m_durability <= 0f)
				{
					element5.m_durability.SetValue(1f);
					element5.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
				}
				else
				{
					element5.m_durability.SetValue(itemData.GetDurabilityPercentage());
					element5.m_durability.ResetColor();
				}
			}
			element5.m_equiped.enabled = (player && itemData.m_equipped);
			element5.m_queued.enabled = (player && player.IsEquipActionQueued(itemData));
			element5.m_noteleport.enabled = (!itemData.m_shared.m_teleportable && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll));
			if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && (itemData.m_shared.m_food > 0f || itemData.m_shared.m_foodStamina > 0f || itemData.m_shared.m_foodEitr > 0f))
			{
				element5.m_food.enabled = true;
				if (itemData.m_shared.m_food < itemData.m_shared.m_foodEitr / 2f && itemData.m_shared.m_foodStamina < itemData.m_shared.m_foodEitr / 2f)
				{
					element5.m_food.color = this.m_foodEitrColor;
				}
				else if (itemData.m_shared.m_foodStamina < itemData.m_shared.m_food / 2f)
				{
					element5.m_food.color = this.m_foodHealthColor;
				}
				else if (itemData.m_shared.m_food < itemData.m_shared.m_foodStamina / 2f)
				{
					element5.m_food.color = this.m_foodStaminaColor;
				}
				else
				{
					element5.m_food.color = Color.white;
				}
			}
			else
			{
				element5.m_food.enabled = false;
			}
			if (element4 == element5)
			{
				this.CreateItemTooltip(itemData, element5.m_tooltip);
			}
			element5.m_quality.enabled = (itemData.m_shared.m_maxQuality > 1);
			if (itemData.m_shared.m_maxQuality > 1)
			{
				element5.m_quality.text = itemData.m_quality.ToString();
			}
			element5.m_amount.enabled = (itemData.m_shared.m_maxStackSize > 1);
			if (itemData.m_shared.m_maxStackSize > 1)
			{
				element5.m_amount.text = string.Format("{0}/{1}", itemData.m_stack, itemData.m_shared.m_maxStackSize);
			}
		}
		foreach (InventoryGrid.Element element6 in this.m_elements)
		{
			element6.m_selected.SetActive(flag && element6.m_pos == this.m_selected);
			if (!element6.m_used)
			{
				element6.m_durability.gameObject.SetActive(false);
				element6.m_icon.enabled = false;
				element6.m_amount.enabled = false;
				element6.m_quality.enabled = false;
				element6.m_equiped.enabled = false;
				element6.m_queued.enabled = false;
				element6.m_noteleport.enabled = false;
				element6.m_food.enabled = false;
				element6.m_tooltip.m_text = "";
				element6.m_tooltip.m_topic = "";
			}
		}
		float size = (float)height * this.m_elementSpace;
		this.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
	}

	// Token: 0x060007CC RID: 1996 RVA: 0x00044108 File Offset: 0x00042308
	private void CreateItemTooltip(ItemDrop.ItemData item, UITooltip tooltip)
	{
		tooltip.Set(item.m_shared.m_name, item.GetTooltip(-1), this.m_tooltipAnchor, default(Vector2));
	}

	// Token: 0x060007CD RID: 1997 RVA: 0x0004413C File Offset: 0x0004233C
	public Vector2 GetWidgetSize()
	{
		return new Vector2((float)this.m_width * this.m_elementSpace, (float)this.m_height * this.m_elementSpace);
	}

	// Token: 0x060007CE RID: 1998 RVA: 0x00044160 File Offset: 0x00042360
	private void OnRightClick(UIInputHandler element)
	{
		GameObject gameObject = element.gameObject;
		Vector2i buttonPos = this.GetButtonPos(gameObject);
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		if (this.m_onRightClick != null)
		{
			this.m_onRightClick(this, itemAt, buttonPos);
		}
	}

	// Token: 0x060007CF RID: 1999 RVA: 0x000441AC File Offset: 0x000423AC
	private void OnLeftClick(UIInputHandler clickHandler)
	{
		GameObject gameObject = clickHandler.gameObject;
		Vector2i buttonPos = this.GetButtonPos(gameObject);
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		InventoryGrid.Modifier arg = InventoryGrid.Modifier.Select;
		if (ZInput.GetKey(KeyCode.LeftShift, true) || ZInput.GetKey(KeyCode.RightShift, true))
		{
			arg = InventoryGrid.Modifier.Split;
		}
		else if (ZInput.GetKey(KeyCode.LeftControl, true) || ZInput.GetKey(KeyCode.RightControl, true))
		{
			arg = InventoryGrid.Modifier.Move;
		}
		if (this.m_onSelected != null)
		{
			this.m_onSelected(this, itemAt, buttonPos, arg);
		}
	}

	// Token: 0x060007D0 RID: 2000 RVA: 0x00044234 File Offset: 0x00042434
	private InventoryGrid.Element GetElement(int x, int y, int width)
	{
		int index = y * width + x;
		return this.m_elements[index];
	}

	// Token: 0x060007D1 RID: 2001 RVA: 0x00044254 File Offset: 0x00042454
	private InventoryGrid.Element GetHoveredElement()
	{
		foreach (InventoryGrid.Element element in this.m_elements)
		{
			RectTransform rectTransform = element.m_go.transform as RectTransform;
			Vector2 point = rectTransform.InverseTransformPoint(ZInput.mousePosition);
			if (rectTransform.rect.Contains(point))
			{
				return element;
			}
		}
		return null;
	}

	// Token: 0x060007D2 RID: 2002 RVA: 0x000442DC File Offset: 0x000424DC
	private Vector2i GetButtonPos(GameObject go)
	{
		for (int i = 0; i < this.m_elements.Count; i++)
		{
			if (this.m_elements[i].m_go == go)
			{
				int num = i / this.m_width;
				return new Vector2i(i - num * this.m_width, num);
			}
		}
		return new Vector2i(-1, -1);
	}

	// Token: 0x060007D3 RID: 2003 RVA: 0x0004433C File Offset: 0x0004253C
	public bool DropItem(Inventory fromInventory, ItemDrop.ItemData item, int amount, Vector2i pos)
	{
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(pos.x, pos.y);
		if (itemAt == item)
		{
			return true;
		}
		if (itemAt != null && (itemAt.m_shared.m_name != item.m_shared.m_name || (item.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality) || itemAt.m_shared.m_maxStackSize == 1) && item.m_stack == amount)
		{
			fromInventory.RemoveItem(item);
			fromInventory.MoveItemToThis(this.m_inventory, itemAt, itemAt.m_stack, item.m_gridPos.x, item.m_gridPos.y);
			this.m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
			return true;
		}
		return this.m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
	}

	// Token: 0x060007D4 RID: 2004 RVA: 0x0004442C File Offset: 0x0004262C
	public ItemDrop.ItemData GetItem(Vector2i cursorPosition)
	{
		foreach (InventoryGrid.Element element in this.m_elements)
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(element.m_go.transform as RectTransform, cursorPosition.ToVector2()))
			{
				Vector2i buttonPos = this.GetButtonPos(element.m_go);
				return this.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
			}
		}
		return null;
	}

	// Token: 0x060007D5 RID: 2005 RVA: 0x000444C0 File Offset: 0x000426C0
	public Inventory GetInventory()
	{
		return this.m_inventory;
	}

	// Token: 0x060007D6 RID: 2006 RVA: 0x000444C8 File Offset: 0x000426C8
	public void SetSelection(Vector2i pos)
	{
		this.m_selected = pos;
	}

	// Token: 0x060007D7 RID: 2007 RVA: 0x000444D1 File Offset: 0x000426D1
	public ItemDrop.ItemData GetGamepadSelectedItem()
	{
		if (!this.m_uiGroup.IsActive)
		{
			return null;
		}
		if (this.m_inventory == null)
		{
			return null;
		}
		return this.m_inventory.GetItemAt(this.m_selected.x, this.m_selected.y);
	}

	// Token: 0x060007D8 RID: 2008 RVA: 0x00044510 File Offset: 0x00042710
	public RectTransform GetGamepadSelectedElement()
	{
		if (!this.m_uiGroup.IsActive)
		{
			return null;
		}
		if (this.m_selected.x < 0 || this.m_selected.x >= this.m_width || this.m_selected.y < 0 || this.m_selected.y >= this.m_height)
		{
			return null;
		}
		return this.GetElement(this.m_selected.x, this.m_selected.y, this.m_width).m_go.transform as RectTransform;
	}

	// Token: 0x1700002B RID: 43
	// (get) Token: 0x060007D9 RID: 2009 RVA: 0x000445A1 File Offset: 0x000427A1
	internal int GridWidth
	{
		get
		{
			return this.m_width;
		}
	}

	// Token: 0x1700002C RID: 44
	// (get) Token: 0x060007DA RID: 2010 RVA: 0x000445A9 File Offset: 0x000427A9
	internal Vector2i SelectionGridPosition
	{
		get
		{
			return this.m_selected;
		}
	}

	// Token: 0x0400094F RID: 2383
	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier> m_onSelected;

	// Token: 0x04000950 RID: 2384
	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i> m_onRightClick;

	// Token: 0x04000951 RID: 2385
	public RectTransform m_tooltipAnchor;

	// Token: 0x04000952 RID: 2386
	public Action<Vector2i> OnMoveToUpperInventoryGrid;

	// Token: 0x04000953 RID: 2387
	public Action<Vector2i> OnMoveToLowerInventoryGrid;

	// Token: 0x04000954 RID: 2388
	public GameObject m_elementPrefab;

	// Token: 0x04000955 RID: 2389
	public RectTransform m_gridRoot;

	// Token: 0x04000956 RID: 2390
	public Scrollbar m_scrollbar;

	// Token: 0x04000957 RID: 2391
	public UIGroupHandler m_uiGroup;

	// Token: 0x04000958 RID: 2392
	public float m_elementSpace = 10f;

	// Token: 0x04000959 RID: 2393
	private int m_width = 4;

	// Token: 0x0400095A RID: 2394
	private int m_height = 4;

	// Token: 0x0400095B RID: 2395
	private Vector2i m_selected = new Vector2i(0, 0);

	// Token: 0x0400095C RID: 2396
	private Inventory m_inventory;

	// Token: 0x0400095D RID: 2397
	private List<InventoryGrid.Element> m_elements = new List<InventoryGrid.Element>();

	// Token: 0x0400095E RID: 2398
	private bool jumpToNextContainer;

	// Token: 0x0400095F RID: 2399
	private readonly Color m_foodEitrColor = new Color(0.6f, 0.6f, 1f, 1f);

	// Token: 0x04000960 RID: 2400
	private readonly Color m_foodHealthColor = new Color(1f, 0.5f, 0.5f, 1f);

	// Token: 0x04000961 RID: 2401
	private readonly Color m_foodStaminaColor = new Color(1f, 1f, 0.5f, 1f);

	// Token: 0x0200027E RID: 638
	private class Element
	{
		// Token: 0x040021B2 RID: 8626
		public Vector2i m_pos;

		// Token: 0x040021B3 RID: 8627
		public GameObject m_go;

		// Token: 0x040021B4 RID: 8628
		public Image m_icon;

		// Token: 0x040021B5 RID: 8629
		public TMP_Text m_amount;

		// Token: 0x040021B6 RID: 8630
		public TMP_Text m_quality;

		// Token: 0x040021B7 RID: 8631
		public Image m_equiped;

		// Token: 0x040021B8 RID: 8632
		public Image m_queued;

		// Token: 0x040021B9 RID: 8633
		public GameObject m_selected;

		// Token: 0x040021BA RID: 8634
		public Image m_noteleport;

		// Token: 0x040021BB RID: 8635
		public Image m_food;

		// Token: 0x040021BC RID: 8636
		public UITooltip m_tooltip;

		// Token: 0x040021BD RID: 8637
		public GuiBar m_durability;

		// Token: 0x040021BE RID: 8638
		public bool m_used;
	}

	// Token: 0x0200027F RID: 639
	public enum Modifier
	{
		// Token: 0x040021C0 RID: 8640
		Select,
		// Token: 0x040021C1 RID: 8641
		Split,
		// Token: 0x040021C2 RID: 8642
		Move,
		// Token: 0x040021C3 RID: 8643
		Drop
	}
}
