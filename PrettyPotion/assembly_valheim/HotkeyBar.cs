using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000077 RID: 119
public class HotkeyBar : MonoBehaviour
{
	// Token: 0x06000784 RID: 1924 RVA: 0x0004045C File Offset: 0x0003E65C
	private void Update()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer && !InventoryGui.IsVisible() && !Menu.IsVisible() && !GameCamera.InFreeFly() && !Minimap.IsOpen() && !Hud.IsPieceSelectionVisible() && !StoreGui.IsVisible() && !global::Console.IsVisible() && !Chat.instance.HasFocus() && !PlayerCustomizaton.IsBarberGuiVisible() && !Hud.InRadial())
		{
			if (ZInput.GetButtonDown("JoyHotbarLeft") && !ZInput.GetButton("JoyAltKeys"))
			{
				if (this.m_selected - 1 < 0)
				{
					this.m_selected = this.m_elements.Count - 1;
				}
				else
				{
					this.m_selected--;
				}
			}
			if (ZInput.GetButtonDown("JoyHotbarRight") && !ZInput.GetButton("JoyAltKeys"))
			{
				if (this.m_selected + 1 > this.m_elements.Count - 1)
				{
					this.m_selected = 0;
				}
				else
				{
					this.m_selected++;
				}
			}
			if (ZInput.GetButtonDown("JoyHotbarUse") && !ZInput.GetButton("JoyAltKeys"))
			{
				localPlayer.UseHotbarItem(this.m_selected + 1);
			}
		}
		if (this.m_selected > this.m_elements.Count - 1)
		{
			this.m_selected = Mathf.Max(0, this.m_elements.Count - 1);
		}
		this.UpdateIcons(localPlayer);
	}

	// Token: 0x06000785 RID: 1925 RVA: 0x000405CC File Offset: 0x0003E7CC
	private void UpdateIcons(Player player)
	{
		if (!player || player.IsDead())
		{
			foreach (HotkeyBar.ElementData elementData in this.m_elements)
			{
				UnityEngine.Object.Destroy(elementData.m_go);
			}
			this.m_elements.Clear();
			return;
		}
		player.GetInventory().GetBoundItems(this.m_items);
		this.m_items.Sort((ItemDrop.ItemData x, ItemDrop.ItemData y) => x.m_gridPos.x.CompareTo(y.m_gridPos.x));
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_items)
		{
			if (itemData.m_gridPos.x + 1 > num)
			{
				num = itemData.m_gridPos.x + 1;
			}
		}
		if (this.m_elements.Count != num)
		{
			foreach (HotkeyBar.ElementData elementData2 in this.m_elements)
			{
				UnityEngine.Object.Destroy(elementData2.m_go);
			}
			this.m_elements.Clear();
			for (int i = 0; i < num; i++)
			{
				HotkeyBar.ElementData elementData3 = new HotkeyBar.ElementData();
				elementData3.m_go = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, base.transform);
				elementData3.m_go.transform.localPosition = new Vector3((float)i * this.m_elementSpace, 0f, 0f);
				TMP_Text component = elementData3.m_go.transform.Find("binding").GetComponent<TMP_Text>();
				if (ZInput.IsGamepadActive())
				{
					component.text = string.Empty;
				}
				else
				{
					component.text = (i + 1).ToString();
				}
				elementData3.m_icon = elementData3.m_go.transform.transform.Find("icon").GetComponent<Image>();
				elementData3.m_durability = elementData3.m_go.transform.Find("durability").GetComponent<GuiBar>();
				elementData3.m_amount = elementData3.m_go.transform.Find("amount").GetComponent<TMP_Text>();
				elementData3.m_equiped = elementData3.m_go.transform.Find("equiped").gameObject;
				elementData3.m_queued = elementData3.m_go.transform.Find("queued").gameObject;
				elementData3.m_selection = elementData3.m_go.transform.Find("selected").gameObject;
				this.m_elements.Add(elementData3);
			}
		}
		foreach (HotkeyBar.ElementData elementData4 in this.m_elements)
		{
			elementData4.m_used = false;
		}
		bool flag = ZInput.IsGamepadActive();
		for (int j = 0; j < this.m_items.Count; j++)
		{
			ItemDrop.ItemData itemData2 = this.m_items[j];
			HotkeyBar.ElementData elementData5 = this.m_elements[itemData2.m_gridPos.x];
			elementData5.m_used = true;
			elementData5.m_icon.gameObject.SetActive(true);
			elementData5.m_icon.sprite = itemData2.GetIcon();
			bool flag2 = itemData2.m_shared.m_useDurability && itemData2.m_durability < itemData2.GetMaxDurability();
			elementData5.m_durability.gameObject.SetActive(flag2);
			if (flag2)
			{
				if (itemData2.m_durability <= 0f)
				{
					elementData5.m_durability.SetValue(1f);
					elementData5.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
				}
				else
				{
					elementData5.m_durability.SetValue(itemData2.GetDurabilityPercentage());
					elementData5.m_durability.ResetColor();
				}
			}
			elementData5.m_equiped.SetActive(itemData2.m_equipped);
			elementData5.m_queued.SetActive(player.IsEquipActionQueued(itemData2));
			if (itemData2.m_shared.m_maxStackSize > 1)
			{
				elementData5.m_amount.gameObject.SetActive(true);
				if (elementData5.m_stackText != itemData2.m_stack)
				{
					elementData5.m_amount.text = string.Format("{0} / {1}", itemData2.m_stack, itemData2.m_shared.m_maxStackSize);
					elementData5.m_stackText = itemData2.m_stack;
				}
			}
			else
			{
				elementData5.m_amount.gameObject.SetActive(false);
			}
		}
		for (int k = 0; k < this.m_elements.Count; k++)
		{
			HotkeyBar.ElementData elementData6 = this.m_elements[k];
			elementData6.m_selection.SetActive(flag && k == this.m_selected);
			if (!elementData6.m_used)
			{
				elementData6.m_icon.gameObject.SetActive(false);
				elementData6.m_durability.gameObject.SetActive(false);
				elementData6.m_equiped.SetActive(false);
				elementData6.m_queued.SetActive(false);
				elementData6.m_amount.gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06000786 RID: 1926 RVA: 0x00040B88 File Offset: 0x0003ED88
	private void ToggleBindingHint(bool bShouldEnable)
	{
		for (int i = 0; i < this.m_elements.Count; i++)
		{
			TMP_Text component = this.m_elements[i].m_go.transform.Find("binding").GetComponent<TMP_Text>();
			if (bShouldEnable)
			{
				component.text = (i + 1).ToString();
			}
			else
			{
				component.text = string.Empty;
			}
		}
	}

	// Token: 0x06000787 RID: 1927 RVA: 0x00040BF2 File Offset: 0x0003EDF2
	private void OnEnable()
	{
		ZInput.OnInputLayoutChanged += this.OnInputLayoutChanged;
	}

	// Token: 0x06000788 RID: 1928 RVA: 0x00040C05 File Offset: 0x0003EE05
	private void OnDisable()
	{
		ZInput.OnInputLayoutChanged -= this.OnInputLayoutChanged;
	}

	// Token: 0x06000789 RID: 1929 RVA: 0x00040C18 File Offset: 0x0003EE18
	private void OnInputLayoutChanged()
	{
		this.ToggleBindingHint(!ZInput.IsGamepadActive());
	}

	// Token: 0x040008C1 RID: 2241
	public GameObject m_elementPrefab;

	// Token: 0x040008C2 RID: 2242
	public float m_elementSpace = 70f;

	// Token: 0x040008C3 RID: 2243
	private int m_selected;

	// Token: 0x040008C4 RID: 2244
	private List<HotkeyBar.ElementData> m_elements = new List<HotkeyBar.ElementData>();

	// Token: 0x040008C5 RID: 2245
	private List<ItemDrop.ItemData> m_items = new List<ItemDrop.ItemData>();

	// Token: 0x0200027B RID: 635
	private class ElementData
	{
		// Token: 0x040021A2 RID: 8610
		public bool m_used;

		// Token: 0x040021A3 RID: 8611
		public GameObject m_go;

		// Token: 0x040021A4 RID: 8612
		public Image m_icon;

		// Token: 0x040021A5 RID: 8613
		public GuiBar m_durability;

		// Token: 0x040021A6 RID: 8614
		public TMP_Text m_amount;

		// Token: 0x040021A7 RID: 8615
		public GameObject m_equiped;

		// Token: 0x040021A8 RID: 8616
		public GameObject m_queued;

		// Token: 0x040021A9 RID: 8617
		public GameObject m_selection;

		// Token: 0x040021AA RID: 8618
		public int m_stackText = -1;
	}
}
