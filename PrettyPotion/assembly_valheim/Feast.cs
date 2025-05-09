using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000176 RID: 374
public class Feast : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001672 RID: 5746 RVA: 0x000A5F10 File Offset: 0x000A4110
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview)
		{
			this.m_nview.Register("RPC_TryEat", new Action<long>(this.RPC_TryEat));
			this.m_nview.Register("RPC_OnEat", new Action<long>(this.RPC_OnEat));
			this.m_nview.Register("RPC_EatConfirmation", new Action<long>(this.RPC_EatConfirmation));
		}
		this.UpdateVisual();
		if (!this.m_foodItem)
		{
			this.m_foodItem = base.gameObject.GetComponent<ItemDrop>();
		}
		if (!this.m_foodItem)
		{
			ZLog.LogError("Feast created without separate food item or being a food itself!");
		}
	}

	// Token: 0x06001673 RID: 5747 RVA: 0x000A5FC8 File Offset: 0x000A41C8
	public void UpdateVisual()
	{
		float stackPercentige = this.GetStackPercentige();
		for (int i = this.m_feastParts.Count - 1; i >= 0; i--)
		{
			Feast.FeastLevel feastLevel = this.m_feastParts[i];
			if (feastLevel.m_onAboveEquals)
			{
				feastLevel.m_onAboveEquals.SetActive(stackPercentige >= feastLevel.m_threshold);
			}
			if (feastLevel.m_onBelow)
			{
				feastLevel.m_onBelow.SetActive(stackPercentige < feastLevel.m_threshold && stackPercentige >= feastLevel.m_thresholdBelowMax);
			}
		}
	}

	// Token: 0x06001674 RID: 5748 RVA: 0x000A6058 File Offset: 0x000A4258
	private void RPC_TryEat(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		int stack = this.GetStack();
		ZLog.Log(string.Format("We eat a stack - starting with {0}", stack));
		if (stack <= 0)
		{
			return;
		}
		if (stack <= 1)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_value, -1, false);
		}
		else
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_value, stack - 1, false);
		}
		ZLog.Log(string.Format("Stack is now {0}", this.GetStack()));
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_OnEat", Array.Empty<object>());
		this.m_nview.InvokeRPC(sender, "RPC_EatConfirmation", Array.Empty<object>());
		this.UpdateVisual();
	}

	// Token: 0x06001675 RID: 5749 RVA: 0x000A611C File Offset: 0x000A431C
	private void RPC_EatConfirmation(long sender)
	{
		if (this.m_foodItem.m_itemData.m_shared.m_consumeStatusEffect)
		{
			Player.m_localPlayer.GetSEMan().AddStatusEffect(this.m_foodItem.m_itemData.m_shared.m_consumeStatusEffect, true, 0, 0f);
		}
		if (this.m_foodItem.m_itemData.m_shared.m_food > 0f)
		{
			Player.m_localPlayer.EatFood(this.m_foodItem.m_itemData);
		}
	}

	// Token: 0x06001676 RID: 5750 RVA: 0x000A61A3 File Offset: 0x000A43A3
	public void RPC_OnEat(long sender)
	{
		this.m_eatEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		this.UpdateVisual();
	}

	// Token: 0x06001677 RID: 5751 RVA: 0x000A61D4 File Offset: 0x000A43D4
	public int GetStack()
	{
		if (this.m_nview && this.m_nview.IsValid())
		{
			return this.m_nview.GetZDO().GetInt(ZDOVars.s_value, this.m_eatStacks);
		}
		return this.m_eatStacks;
	}

	// Token: 0x06001678 RID: 5752 RVA: 0x000A6212 File Offset: 0x000A4412
	public float GetStackPercentige()
	{
		return (float)Mathf.Max(this.GetStack(), 0) / (float)this.m_eatStacks;
	}

	// Token: 0x06001679 RID: 5753 RVA: 0x000A6229 File Offset: 0x000A4429
	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, base.transform.position) < this.m_useDistance;
	}

	// Token: 0x0600167A RID: 5754 RVA: 0x000A6250 File Offset: 0x000A4450
	public string GetHoverText()
	{
		int stack = this.GetStack();
		if (stack <= 0)
		{
			return "";
		}
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize(this.GetHoverName() + string.Format("\n[<color=yellow><b>$KEY_Use</b></color>] $item_eat ( {0}/{1} )", stack, this.m_eatStacks));
	}

	// Token: 0x0600167B RID: 5755 RVA: 0x000A62BB File Offset: 0x000A44BB
	public string GetHoverName()
	{
		return this.m_foodItem.m_itemData.m_shared.m_name;
	}

	// Token: 0x0600167C RID: 5756 RVA: 0x000A62D4 File Offset: 0x000A44D4
	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		Player player = human as Player;
		if (!player || !this.InUseDistance(player))
		{
			return false;
		}
		if (this.GetStack() <= 0)
		{
			return false;
		}
		if (!player.CanConsumeItem(this.m_foodItem.m_itemData, true))
		{
			return false;
		}
		this.m_nview.InvokeRPC("RPC_TryEat", Array.Empty<object>());
		return true;
	}

	// Token: 0x0600167D RID: 5757 RVA: 0x000A6337 File Offset: 0x000A4537
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x04001611 RID: 5649
	public int m_eatStacks = 5;

	// Token: 0x04001612 RID: 5650
	public float m_useDistance = 2f;

	// Token: 0x04001613 RID: 5651
	public ItemDrop m_foodItem;

	// Token: 0x04001614 RID: 5652
	public List<Feast.FeastLevel> m_feastParts = new List<Feast.FeastLevel>();

	// Token: 0x04001615 RID: 5653
	public EffectList m_eatEffect = new EffectList();

	// Token: 0x04001616 RID: 5654
	private ZNetView m_nview;

	// Token: 0x0200035B RID: 859
	[Serializable]
	public class FeastLevel
	{
		// Token: 0x04002592 RID: 9618
		public GameObject m_onAboveEquals;

		// Token: 0x04002593 RID: 9619
		public GameObject m_onBelow;

		// Token: 0x04002594 RID: 9620
		public float m_threshold;

		// Token: 0x04002595 RID: 9621
		public float m_thresholdBelowMax;
	}
}
