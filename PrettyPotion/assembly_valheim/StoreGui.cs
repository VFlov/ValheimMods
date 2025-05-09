using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000095 RID: 149
public class StoreGui : MonoBehaviour
{
	// Token: 0x17000043 RID: 67
	// (get) Token: 0x06000A0D RID: 2573 RVA: 0x000580B0 File Offset: 0x000562B0
	public static StoreGui instance
	{
		get
		{
			return StoreGui.m_instance;
		}
	}

	// Token: 0x06000A0E RID: 2574 RVA: 0x000580B8 File Offset: 0x000562B8
	private void Awake()
	{
		StoreGui.m_instance = this;
		this.m_rootPanel.SetActive(false);
		this.m_itemlistBaseSize = this.m_listRoot.rect.height;
	}

	// Token: 0x06000A0F RID: 2575 RVA: 0x000580F0 File Offset: 0x000562F0
	private void OnDestroy()
	{
		if (StoreGui.m_instance == this)
		{
			StoreGui.m_instance = null;
		}
	}

	// Token: 0x06000A10 RID: 2576 RVA: 0x00058108 File Offset: 0x00056308
	private void Update()
	{
		if (!this.m_rootPanel.activeSelf)
		{
			this.m_hiddenFrames++;
			return;
		}
		this.m_hiddenFrames = 0;
		if (!this.m_trader)
		{
			this.Hide();
			return;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene())
		{
			this.Hide();
			return;
		}
		if (Vector3.Distance(this.m_trader.transform.position, Player.m_localPlayer.transform.position) > this.m_hideDistance)
		{
			this.Hide();
			return;
		}
		if (InventoryGui.IsVisible() || Minimap.IsOpen())
		{
			this.Hide();
			return;
		}
		if ((Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !Menu.IsVisible() && TextViewer.instance && !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() && (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape, true) || ZInput.GetButtonDown("Use")))
		{
			ZInput.ResetButtonStatus("JoyButtonB");
			this.Hide();
		}
		this.UpdateBuyButton();
		this.UpdateSellButton();
		this.UpdateRecipeGamepadInput();
		this.m_coinText.text = this.GetPlayerCoins().ToString();
	}

	// Token: 0x06000A11 RID: 2577 RVA: 0x00058264 File Offset: 0x00056464
	public void Show(Trader trader)
	{
		if (this.m_trader == trader && StoreGui.IsVisible())
		{
			return;
		}
		this.m_trader = trader;
		this.m_rootPanel.SetActive(true);
		this.FillList();
	}

	// Token: 0x06000A12 RID: 2578 RVA: 0x00058295 File Offset: 0x00056495
	public void Hide()
	{
		this.m_trader = null;
		this.m_rootPanel.SetActive(false);
	}

	// Token: 0x06000A13 RID: 2579 RVA: 0x000582AA File Offset: 0x000564AA
	public static bool IsVisible()
	{
		return StoreGui.m_instance && StoreGui.m_instance.m_hiddenFrames <= 1;
	}

	// Token: 0x06000A14 RID: 2580 RVA: 0x000582CA File Offset: 0x000564CA
	public void OnBuyItem()
	{
		this.BuySelectedItem();
	}

	// Token: 0x06000A15 RID: 2581 RVA: 0x000582D4 File Offset: 0x000564D4
	private void BuySelectedItem()
	{
		if (this.m_selectedItem == null || !this.CanAfford(this.m_selectedItem))
		{
			return;
		}
		int stack = Mathf.Min(this.m_selectedItem.m_stack, this.m_selectedItem.m_prefab.m_itemData.m_shared.m_maxStackSize);
		int quality = this.m_selectedItem.m_prefab.m_itemData.m_quality;
		int variant = this.m_selectedItem.m_prefab.m_itemData.m_variant;
		if (Player.m_localPlayer.GetInventory().AddItem(this.m_selectedItem.m_prefab.name, stack, quality, variant, 0L, "", false) != null)
		{
			Player.m_localPlayer.GetInventory().RemoveItem(this.m_coinPrefab.m_itemData.m_shared.m_name, this.m_selectedItem.m_price, -1, true);
			this.m_trader.OnBought(this.m_selectedItem);
			this.m_buyEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
			Player.m_localPlayer.ShowPickupMessage(this.m_selectedItem.m_prefab.m_itemData, this.m_selectedItem.m_prefab.m_itemData.m_stack);
			this.FillList();
			Gogan.LogEvent("Game", "BoughtItem", this.m_selectedItem.m_prefab.name, 0L);
		}
	}

	// Token: 0x06000A16 RID: 2582 RVA: 0x00058439 File Offset: 0x00056639
	public void OnSellItem()
	{
		this.SellItem();
	}

	// Token: 0x06000A17 RID: 2583 RVA: 0x00058444 File Offset: 0x00056644
	private void SellItem()
	{
		ItemDrop.ItemData sellableItem = this.GetSellableItem();
		if (sellableItem == null)
		{
			return;
		}
		int stack = sellableItem.m_shared.m_value * sellableItem.m_stack;
		Player.m_localPlayer.GetInventory().RemoveItem(sellableItem);
		Player.m_localPlayer.GetInventory().AddItem(this.m_coinPrefab.gameObject.name, stack, this.m_coinPrefab.m_itemData.m_quality, this.m_coinPrefab.m_itemData.m_variant, 0L, "", false);
		string text;
		if (sellableItem.m_stack > 1)
		{
			text = sellableItem.m_stack.ToString() + "x" + sellableItem.m_shared.m_name;
		}
		else
		{
			text = sellableItem.m_shared.m_name;
		}
		this.m_sellEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_sold", new string[]
		{
			text,
			stack.ToString()
		}), 0, sellableItem.m_shared.m_icons[0]);
		this.m_trader.OnSold();
		this.FillList();
		Gogan.LogEvent("Game", "SoldItem", text, 0L);
	}

	// Token: 0x06000A18 RID: 2584 RVA: 0x00058589 File Offset: 0x00056789
	private int GetPlayerCoins()
	{
		return Player.m_localPlayer.GetInventory().CountItems(this.m_coinPrefab.m_itemData.m_shared.m_name, -1, true);
	}

	// Token: 0x06000A19 RID: 2585 RVA: 0x000585B4 File Offset: 0x000567B4
	private bool CanAfford(Trader.TradeItem item)
	{
		int playerCoins = this.GetPlayerCoins();
		return item.m_price <= playerCoins;
	}

	// Token: 0x06000A1A RID: 2586 RVA: 0x000585D4 File Offset: 0x000567D4
	private void FillList()
	{
		int playerCoins = this.GetPlayerCoins();
		int num = this.GetSelectedItemIndex();
		List<Trader.TradeItem> availableItems = this.m_trader.GetAvailableItems();
		foreach (GameObject obj in this.m_itemList)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_itemList.Clear();
		float num2 = (float)availableItems.Count * this.m_itemSpacing;
		num2 = Mathf.Max(this.m_itemlistBaseSize, num2);
		this.m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
		for (int i = 0; i < availableItems.Count; i++)
		{
			Trader.TradeItem tradeItem = availableItems[i];
			GameObject element = UnityEngine.Object.Instantiate<GameObject>(this.m_listElement, this.m_listRoot);
			element.SetActive(true);
			RectTransform rectTransform = element.transform as RectTransform;
			float num3 = (this.m_listRoot.rect.width - rectTransform.rect.width) / 2f;
			rectTransform.anchoredPosition = new Vector2(num3, (float)i * -this.m_itemSpacing - num3);
			bool flag = tradeItem.m_price <= playerCoins;
			Image component = element.transform.Find("icon").GetComponent<Image>();
			component.sprite = tradeItem.m_prefab.m_itemData.m_shared.m_icons[0];
			component.color = (flag ? Color.white : new Color(1f, 0f, 1f, 0f));
			string text = Localization.instance.Localize(tradeItem.m_prefab.m_itemData.m_shared.m_name);
			if (tradeItem.m_stack > 1)
			{
				text = text + " x" + tradeItem.m_stack.ToString();
			}
			TMP_Text component2 = element.transform.Find("name").GetComponent<TMP_Text>();
			component2.text = text;
			component2.color = (flag ? Color.white : Color.grey);
			element.GetComponent<UITooltip>().Set(tradeItem.m_prefab.m_itemData.m_shared.m_name, tradeItem.m_prefab.m_itemData.GetTooltip(tradeItem.m_stack), this.m_tooltipAnchor, default(Vector2));
			TMP_Text component3 = Utils.FindChild(element.transform, "price", Utils.IterativeSearchType.DepthFirst).GetComponent<TMP_Text>();
			component3.text = tradeItem.m_price.ToString();
			if (!flag)
			{
				component3.color = Color.grey;
			}
			element.GetComponent<Button>().onClick.AddListener(delegate()
			{
				this.OnSelectedItem(element);
			});
			this.m_itemList.Add(element);
		}
		if (num < 0)
		{
			num = 0;
		}
		this.SelectItem(num, false);
	}

	// Token: 0x06000A1B RID: 2587 RVA: 0x000588EC File Offset: 0x00056AEC
	private void OnSelectedItem(GameObject button)
	{
		int index = this.FindSelectedRecipe(button);
		this.SelectItem(index, false);
	}

	// Token: 0x06000A1C RID: 2588 RVA: 0x0005890C File Offset: 0x00056B0C
	private int FindSelectedRecipe(GameObject button)
	{
		for (int i = 0; i < this.m_itemList.Count; i++)
		{
			if (this.m_itemList[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x06000A1D RID: 2589 RVA: 0x00058948 File Offset: 0x00056B48
	private void SelectItem(int index, bool center)
	{
		ZLog.Log("Setting selected recipe " + index.ToString());
		for (int i = 0; i < this.m_itemList.Count; i++)
		{
			bool active = i == index;
			this.m_itemList[i].transform.Find("selected").gameObject.SetActive(active);
		}
		if (center && index >= 0)
		{
			this.m_itemEnsureVisible.CenterOnItem(this.m_itemList[index].transform as RectTransform);
		}
		if (index < 0)
		{
			this.m_selectedItem = null;
			return;
		}
		this.m_selectedItem = this.m_trader.GetAvailableItems()[index];
	}

	// Token: 0x06000A1E RID: 2590 RVA: 0x000589F7 File Offset: 0x00056BF7
	private void UpdateSellButton()
	{
		this.m_sellButton.interactable = (this.GetSellableItem() != null);
	}

	// Token: 0x06000A1F RID: 2591 RVA: 0x00058A10 File Offset: 0x00056C10
	private ItemDrop.ItemData GetSellableItem()
	{
		this.m_tempItems.Clear();
		Player.m_localPlayer.GetInventory().GetValuableItems(this.m_tempItems);
		foreach (ItemDrop.ItemData itemData in this.m_tempItems)
		{
			if (itemData.m_shared.m_name != this.m_coinPrefab.m_itemData.m_shared.m_name)
			{
				return itemData;
			}
		}
		return null;
	}

	// Token: 0x06000A20 RID: 2592 RVA: 0x00058AAC File Offset: 0x00056CAC
	private int GetSelectedItemIndex()
	{
		int result = 0;
		List<Trader.TradeItem> availableItems = this.m_trader.GetAvailableItems();
		for (int i = 0; i < availableItems.Count; i++)
		{
			if (availableItems[i] == this.m_selectedItem)
			{
				result = i;
			}
		}
		return result;
	}

	// Token: 0x06000A21 RID: 2593 RVA: 0x00058AEC File Offset: 0x00056CEC
	private void UpdateBuyButton()
	{
		UITooltip component = this.m_buyButton.GetComponent<UITooltip>();
		if (this.m_selectedItem == null)
		{
			this.m_buyButton.interactable = false;
			component.m_text = "";
			return;
		}
		bool flag = this.CanAfford(this.m_selectedItem);
		bool flag2 = Player.m_localPlayer.GetInventory().HaveEmptySlot();
		this.m_buyButton.interactable = (flag && flag2);
		if (!flag)
		{
			component.m_text = Localization.instance.Localize("$msg_missingrequirement");
			return;
		}
		if (!flag2)
		{
			component.m_text = Localization.instance.Localize("$inventory_full");
			return;
		}
		component.m_text = "";
	}

	// Token: 0x06000A22 RID: 2594 RVA: 0x00058B90 File Offset: 0x00056D90
	private void UpdateRecipeGamepadInput()
	{
		if (this.m_itemList.Count > 0)
		{
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.SelectItem(Mathf.Min(this.m_itemList.Count - 1, this.GetSelectedItemIndex() + 1), true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.SelectItem(Mathf.Max(0, this.GetSelectedItemIndex() - 1), true);
			}
		}
	}

	// Token: 0x04000B9B RID: 2971
	private static StoreGui m_instance;

	// Token: 0x04000B9C RID: 2972
	public GameObject m_rootPanel;

	// Token: 0x04000B9D RID: 2973
	public Button m_buyButton;

	// Token: 0x04000B9E RID: 2974
	public Button m_sellButton;

	// Token: 0x04000B9F RID: 2975
	public RectTransform m_listRoot;

	// Token: 0x04000BA0 RID: 2976
	public GameObject m_listElement;

	// Token: 0x04000BA1 RID: 2977
	public Scrollbar m_listScroll;

	// Token: 0x04000BA2 RID: 2978
	public ScrollRectEnsureVisible m_itemEnsureVisible;

	// Token: 0x04000BA3 RID: 2979
	public TMP_Text m_coinText;

	// Token: 0x04000BA4 RID: 2980
	public EffectList m_buyEffects = new EffectList();

	// Token: 0x04000BA5 RID: 2981
	public EffectList m_sellEffects = new EffectList();

	// Token: 0x04000BA6 RID: 2982
	public float m_hideDistance = 5f;

	// Token: 0x04000BA7 RID: 2983
	public float m_itemSpacing = 64f;

	// Token: 0x04000BA8 RID: 2984
	public ItemDrop m_coinPrefab;

	// Token: 0x04000BA9 RID: 2985
	private List<GameObject> m_itemList = new List<GameObject>();

	// Token: 0x04000BAA RID: 2986
	private Trader.TradeItem m_selectedItem;

	// Token: 0x04000BAB RID: 2987
	private Trader m_trader;

	// Token: 0x04000BAC RID: 2988
	private float m_itemlistBaseSize;

	// Token: 0x04000BAD RID: 2989
	private int m_hiddenFrames;

	// Token: 0x04000BAE RID: 2990
	private List<ItemDrop.ItemData> m_tempItems = new List<ItemDrop.ItemData>();

	// Token: 0x04000BAF RID: 2991
	public RectTransform m_tooltipAnchor;
}
