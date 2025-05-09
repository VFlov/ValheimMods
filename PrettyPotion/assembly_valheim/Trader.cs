using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001D1 RID: 465
public class Trader : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001AA3 RID: 6819 RVA: 0x000C5CB0 File Offset: 0x000C3EB0
	private void Start()
	{
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_lookAt = base.GetComponentInChildren<LookAt>();
		SnapToGround component = base.GetComponent<SnapToGround>();
		if (component)
		{
			component.Snap();
		}
		base.InvokeRepeating("RandomTalk", this.m_randomTalkInterval, this.m_randomTalkInterval);
	}

	// Token: 0x06001AA4 RID: 6820 RVA: 0x000C5D04 File Offset: 0x000C3F04
	private void Update()
	{
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, Mathf.Max(this.m_byeRange + 3f, this.m_standRange));
		if (closestPlayer)
		{
			float num = Vector3.Distance(closestPlayer.transform.position, base.transform.position);
			if (num < this.m_standRange)
			{
				this.m_animator.SetBool("Stand", true);
				this.m_lookAt.SetLoockAtTarget(closestPlayer.GetHeadPoint());
			}
			if (!this.m_didGreet && num < this.m_greetRange)
			{
				this.m_didGreet = true;
				List<string> texts = this.CheckConditionals(this.m_randomGreets, true);
				this.Say(texts, "Greet");
				this.m_randomGreetFX.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
			}
			if (this.m_didGreet && !this.m_didGoodbye && num > this.m_byeRange)
			{
				this.m_didGoodbye = true;
				this.Say(this.m_randomGoodbye, "Greet");
				this.m_randomGoodbyeFX.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
				return;
			}
		}
		else
		{
			this.m_animator.SetBool("Stand", false);
			this.m_lookAt.ResetTarget();
		}
	}

	// Token: 0x06001AA5 RID: 6821 RVA: 0x000C5E54 File Offset: 0x000C4054
	private void RandomTalk()
	{
		if (this.m_animator.GetBool("Stand") && !StoreGui.IsVisible() && Player.IsPlayerInRange(base.transform.position, this.m_greetRange))
		{
			List<string> texts = this.CheckConditionals(this.m_randomTalk, false);
			this.Say(texts, "Talk");
			this.m_randomTalkFX.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
	}

	// Token: 0x06001AA6 RID: 6822 RVA: 0x000C5ED0 File Offset: 0x000C40D0
	private List<string> CheckConditionals(List<string> defaultList, bool isGreet)
	{
		foreach (Trader.ConditionalDialog conditionalDialog in this.m_randomTalkConditionals)
		{
			if ((!isGreet || conditionalDialog.m_textPlacement != Trader.TalkPlacement.ReplaceRandomTalk) && (isGreet || conditionalDialog.m_textPlacement != Trader.TalkPlacement.ReplaceGreet))
			{
				if (conditionalDialog.m_keyCheck == KeySetType.All)
				{
					bool flag = true;
					foreach (string key in conditionalDialog.m_keyConditions)
					{
						if (!ZoneSystem.instance.CheckKey(key, conditionalDialog.m_keyType, !conditionalDialog.m_whenKeyNotSet))
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						return conditionalDialog.m_dialog;
					}
				}
				else if (conditionalDialog.m_keyCheck == KeySetType.Exlusive)
				{
					bool flag2 = false;
					bool flag3 = false;
					foreach (string key2 in conditionalDialog.m_keyConditions)
					{
						if (ZoneSystem.instance.CheckKey(key2, conditionalDialog.m_keyType, !conditionalDialog.m_whenKeyNotSet))
						{
							flag2 = true;
						}
						else
						{
							flag3 = true;
						}
					}
					if (flag2 && flag3)
					{
						return conditionalDialog.m_dialog;
					}
				}
				else
				{
					bool flag4 = false;
					foreach (string key3 in conditionalDialog.m_keyConditions)
					{
						if (ZoneSystem.instance.CheckKey(key3, conditionalDialog.m_keyType, !conditionalDialog.m_whenKeyNotSet))
						{
							flag4 = true;
							break;
						}
					}
					if ((flag4 && conditionalDialog.m_keyCheck == KeySetType.Any) || (!flag4 && conditionalDialog.m_keyCheck == KeySetType.None))
					{
						return conditionalDialog.m_dialog;
					}
				}
			}
		}
		return defaultList;
	}

	// Token: 0x06001AA7 RID: 6823 RVA: 0x000C60FC File Offset: 0x000C42FC
	public string GetHoverText()
	{
		string text = this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact";
		if (this.m_useItems.Count > 0)
		{
			text += "\n[<color=yellow><b>1-8</b></color>] $npc_giveitem";
		}
		return Localization.instance.Localize(text);
	}

	// Token: 0x06001AA8 RID: 6824 RVA: 0x000C613F File Offset: 0x000C433F
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	// Token: 0x06001AA9 RID: 6825 RVA: 0x000C6154 File Offset: 0x000C4354
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		StoreGui.instance.Show(this);
		this.Say(this.m_randomStartTrade, "Talk");
		this.m_randomStartTradeFX.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		return false;
	}

	// Token: 0x06001AAA RID: 6826 RVA: 0x000C61A8 File Offset: 0x000C43A8
	private void DiscoverItems(Player player)
	{
		foreach (Trader.TradeItem tradeItem in this.GetAvailableItems())
		{
			player.AddKnownItem(tradeItem.m_prefab.m_itemData);
		}
	}

	// Token: 0x06001AAB RID: 6827 RVA: 0x000C6208 File Offset: 0x000C4408
	private void Say(List<string> texts, string trigger)
	{
		this.Say(texts[UnityEngine.Random.Range(0, texts.Count)], trigger);
	}

	// Token: 0x06001AAC RID: 6828 RVA: 0x000C6224 File Offset: 0x000C4424
	private void Say(string text, string trigger)
	{
		Chat.instance.SetNpcText(base.gameObject, Vector3.up * this.m_dialogHeight, 20f, this.m_hideDialogDelay, "", text, false);
		if (trigger.Length > 0)
		{
			this.m_animator.SetTrigger(trigger);
		}
	}

	// Token: 0x06001AAD RID: 6829 RVA: 0x000C6278 File Offset: 0x000C4478
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.m_useItems.Count > 0)
		{
			foreach (Trader.TraderUseItem traderUseItem in this.m_useItems)
			{
				if (item.m_shared.m_name == traderUseItem.m_prefab.m_itemData.m_shared.m_name)
				{
					if (!string.IsNullOrEmpty(traderUseItem.m_setsGlobalKey) && ZoneSystem.instance.GetGlobalKey(traderUseItem.m_setsGlobalKey))
					{
						this.Say(this.m_randomUseItemAlreadyRecieved, "Talk");
						return true;
					}
					if (!string.IsNullOrEmpty(traderUseItem.m_dialog))
					{
						this.Say(traderUseItem.m_dialog, "Talk");
					}
					if (!string.IsNullOrEmpty(traderUseItem.m_setsGlobalKey))
					{
						ZoneSystem.instance.SetGlobalKey(traderUseItem.m_setsGlobalKey);
					}
					if (traderUseItem.m_removesItem)
					{
						user.GetInventory().RemoveItem(item, 1);
						user.ShowRemovedMessage(item, 1);
					}
					return true;
				}
			}
			this.Say(this.m_randomGiveItemNo, "Talk");
			return true;
		}
		return false;
	}

	// Token: 0x06001AAE RID: 6830 RVA: 0x000C63B0 File Offset: 0x000C45B0
	public void OnBought(Trader.TradeItem item)
	{
		this.Say(this.m_randomBuy, "Buy");
		this.m_randomBuyFX.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x06001AAF RID: 6831 RVA: 0x000C63E6 File Offset: 0x000C45E6
	public void OnSold()
	{
		this.Say(this.m_randomSell, "Sell");
		this.m_randomSellFX.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x06001AB0 RID: 6832 RVA: 0x000C641C File Offset: 0x000C461C
	public List<Trader.TradeItem> GetAvailableItems()
	{
		List<Trader.TradeItem> list = new List<Trader.TradeItem>();
		foreach (Trader.TradeItem tradeItem in this.m_items)
		{
			if (string.IsNullOrEmpty(tradeItem.m_requiredGlobalKey) || ZoneSystem.instance.GetGlobalKey(tradeItem.m_requiredGlobalKey))
			{
				list.Add(tradeItem);
			}
		}
		return list;
	}

	// Token: 0x04001B01 RID: 6913
	public string m_name = "Haldor";

	// Token: 0x04001B02 RID: 6914
	public float m_standRange = 15f;

	// Token: 0x04001B03 RID: 6915
	public float m_greetRange = 5f;

	// Token: 0x04001B04 RID: 6916
	public float m_byeRange = 5f;

	// Token: 0x04001B05 RID: 6917
	public List<Trader.TradeItem> m_items = new List<Trader.TradeItem>();

	// Token: 0x04001B06 RID: 6918
	public List<Trader.TraderUseItem> m_useItems = new List<Trader.TraderUseItem>();

	// Token: 0x04001B07 RID: 6919
	[Header("Dialog")]
	public float m_hideDialogDelay = 5f;

	// Token: 0x04001B08 RID: 6920
	public float m_randomTalkInterval = 30f;

	// Token: 0x04001B09 RID: 6921
	public float m_dialogHeight = 1.5f;

	// Token: 0x04001B0A RID: 6922
	public List<string> m_randomTalk = new List<string>();

	// Token: 0x04001B0B RID: 6923
	public List<string> m_randomGreets = new List<string>();

	// Token: 0x04001B0C RID: 6924
	public List<string> m_randomGoodbye = new List<string>();

	// Token: 0x04001B0D RID: 6925
	public List<string> m_randomStartTrade = new List<string>();

	// Token: 0x04001B0E RID: 6926
	public List<string> m_randomBuy = new List<string>();

	// Token: 0x04001B0F RID: 6927
	public List<string> m_randomSell = new List<string>();

	// Token: 0x04001B10 RID: 6928
	public List<string> m_randomGiveItemNo = new List<string>();

	// Token: 0x04001B11 RID: 6929
	public List<string> m_randomUseItemAlreadyRecieved = new List<string>();

	// Token: 0x04001B12 RID: 6930
	[global::Tooltip("These will be used instead of random talk if any of the conditions are met")]
	public List<Trader.ConditionalDialog> m_randomTalkConditionals = new List<Trader.ConditionalDialog>();

	// Token: 0x04001B13 RID: 6931
	public EffectList m_randomTalkFX = new EffectList();

	// Token: 0x04001B14 RID: 6932
	public EffectList m_randomGreetFX = new EffectList();

	// Token: 0x04001B15 RID: 6933
	public EffectList m_randomGoodbyeFX = new EffectList();

	// Token: 0x04001B16 RID: 6934
	public EffectList m_randomStartTradeFX = new EffectList();

	// Token: 0x04001B17 RID: 6935
	public EffectList m_randomBuyFX = new EffectList();

	// Token: 0x04001B18 RID: 6936
	public EffectList m_randomSellFX = new EffectList();

	// Token: 0x04001B19 RID: 6937
	private bool m_didGreet;

	// Token: 0x04001B1A RID: 6938
	private bool m_didGoodbye;

	// Token: 0x04001B1B RID: 6939
	private Animator m_animator;

	// Token: 0x04001B1C RID: 6940
	private LookAt m_lookAt;

	// Token: 0x0200038A RID: 906
	[Serializable]
	public class TradeItem
	{
		// Token: 0x04002694 RID: 9876
		public ItemDrop m_prefab;

		// Token: 0x04002695 RID: 9877
		public int m_stack = 1;

		// Token: 0x04002696 RID: 9878
		public int m_price = 100;

		// Token: 0x04002697 RID: 9879
		public string m_requiredGlobalKey;
	}

	// Token: 0x0200038B RID: 907
	[Serializable]
	public class TraderUseItem
	{
		// Token: 0x04002698 RID: 9880
		public ItemDrop m_prefab;

		// Token: 0x04002699 RID: 9881
		public string m_setsGlobalKey;

		// Token: 0x0400269A RID: 9882
		public bool m_removesItem;

		// Token: 0x0400269B RID: 9883
		public string m_dialog;
	}

	// Token: 0x0200038C RID: 908
	[Serializable]
	public class ConditionalDialog
	{
		// Token: 0x0400269C RID: 9884
		public List<string> m_keyConditions = new List<string>();

		// Token: 0x0400269D RID: 9885
		[global::Tooltip("Default unchecked will run when they keys are set in the world, check this to run when keys are NOT set.")]
		public bool m_whenKeyNotSet;

		// Token: 0x0400269E RID: 9886
		[global::Tooltip("Which places this text will be used.")]
		public Trader.TalkPlacement m_textPlacement;

		// Token: 0x0400269F RID: 9887
		public KeySetType m_keyCheck;

		// Token: 0x040026A0 RID: 9888
		public GameKeyType m_keyType;

		// Token: 0x040026A1 RID: 9889
		public List<string> m_dialog;
	}

	// Token: 0x0200038D RID: 909
	public enum TalkPlacement
	{
		// Token: 0x040026A3 RID: 9891
		ReplaceRandomTalk,
		// Token: 0x040026A4 RID: 9892
		ReplaceGreetAndRandomTalk,
		// Token: 0x040026A5 RID: 9893
		ReplaceGreet
	}
}
