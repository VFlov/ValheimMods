using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000188 RID: 392
public class Incinerator : MonoBehaviour
{
	// Token: 0x06001791 RID: 6033 RVA: 0x000AF788 File Offset: 0x000AD988
	private void Awake()
	{
		Switch incinerateSwitch = this.m_incinerateSwitch;
		incinerateSwitch.m_onUse = (Switch.Callback)Delegate.Combine(incinerateSwitch.m_onUse, new Switch.Callback(this.OnIncinerate));
		Switch incinerateSwitch2 = this.m_incinerateSwitch;
		incinerateSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(incinerateSwitch2.m_onHover, new Switch.TooltipCallback(this.GetLeverHoverText));
		this.m_conversions.Sort((Incinerator.IncineratorConversion a, Incinerator.IncineratorConversion b) => b.m_priority.CompareTo(a.m_priority));
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<long>("RPC_RequestIncinerate", new Action<long, long>(this.RPC_RequestIncinerate));
		this.m_nview.Register<int>("RPC_IncinerateRespons", new Action<long, int>(this.RPC_IncinerateRespons));
		this.m_nview.Register("RPC_AnimateLever", new Action<long>(this.RPC_AnimateLever));
		this.m_nview.Register("RPC_AnimateLeverReturn", new Action<long>(this.RPC_AnimateLeverReturn));
	}

	// Token: 0x06001792 RID: 6034 RVA: 0x000AF8A5 File Offset: 0x000ADAA5
	private void StopAOE()
	{
		this.isInUse = false;
	}

	// Token: 0x06001793 RID: 6035 RVA: 0x000AF8AE File Offset: 0x000ADAAE
	public string GetLeverHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return Localization.instance.Localize("$piece_incinerator\n$piece_noaccess");
		}
		return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $piece_pulllever");
	}

	// Token: 0x06001794 RID: 6036 RVA: 0x000AF8E8 File Offset: 0x000ADAE8
	private bool OnIncinerate(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.HasOwner())
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		this.m_nview.InvokeRPC("RPC_RequestIncinerate", new object[]
		{
			playerID
		});
		return true;
	}

	// Token: 0x06001795 RID: 6037 RVA: 0x000AF95C File Offset: 0x000ADB5C
	private void RPC_RequestIncinerate(long uid, long playerID)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Player ",
			uid.ToString(),
			" wants to incinerate ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.GetSessionID().ToString()
		}));
		if (!this.m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
			return;
		}
		if (this.m_container.IsInUse() || this.isInUse)
		{
			this.m_nview.InvokeRPC(uid, "RPC_IncinerateRespons", new object[]
			{
				0
			});
			ZLog.Log("  but it's in use");
			return;
		}
		if (this.m_container.GetInventory().NrOfItems() == 0)
		{
			this.m_nview.InvokeRPC(uid, "RPC_IncinerateRespons", new object[]
			{
				3
			});
			ZLog.Log("  but it's empty");
			return;
		}
		base.StartCoroutine(this.Incinerate(uid));
	}

	// Token: 0x06001796 RID: 6038 RVA: 0x000AFA5C File Offset: 0x000ADC5C
	private IEnumerator Incinerate(long uid)
	{
		this.isInUse = true;
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_AnimateLever", Array.Empty<object>());
		this.m_leverEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		yield return new WaitForSeconds(UnityEngine.Random.Range(this.m_effectDelayMin, this.m_effectDelayMax));
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_AnimateLeverReturn", Array.Empty<object>());
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner() || this.m_container.IsInUse())
		{
			this.isInUse = false;
			yield break;
		}
		base.Invoke("StopAOE", 4f);
		UnityEngine.Object.Instantiate<GameObject>(this.m_lightingAOEs, base.transform.position, base.transform.rotation);
		Inventory inventory = this.m_container.GetInventory();
		List<ItemDrop> list = new List<ItemDrop>();
		int num = 0;
		foreach (Incinerator.IncineratorConversion incineratorConversion in this.m_conversions)
		{
			num += incineratorConversion.AttemptCraft(inventory, list);
		}
		if (this.m_defaultResult != null && this.m_defaultCost > 0)
		{
			int num2 = inventory.NrOfItemsIncludingStacks() / this.m_defaultCost;
			num += num2;
			for (int i = 0; i < num2; i++)
			{
				list.Add(this.m_defaultResult);
			}
		}
		inventory.RemoveAll();
		foreach (ItemDrop itemDrop in list)
		{
			inventory.AddItem(itemDrop.gameObject, 1);
		}
		this.m_nview.InvokeRPC(uid, "RPC_IncinerateRespons", new object[]
		{
			(num > 0) ? 2 : 1
		});
		yield break;
	}

	// Token: 0x06001797 RID: 6039 RVA: 0x000AFA74 File Offset: 0x000ADC74
	private void RPC_IncinerateRespons(long uid, int r)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		string msg;
		switch (r)
		{
		default:
			msg = "$piece_incinerator_fail";
			break;
		case 1:
			msg = "$piece_incinerator_success";
			break;
		case 2:
			msg = "$piece_incinerator_conversion";
			break;
		case 3:
			msg = "$piece_incinerator_empty";
			break;
		}
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg, 0, null);
	}

	// Token: 0x06001798 RID: 6040 RVA: 0x000AFAD4 File Offset: 0x000ADCD4
	private void RPC_AnimateLever(long uid)
	{
		ZLog.Log("DO THE THING WITH THE LEVER!");
		this.m_leverAnim.SetBool("Pulled", true);
	}

	// Token: 0x06001799 RID: 6041 RVA: 0x000AFAF1 File Offset: 0x000ADCF1
	private void RPC_AnimateLeverReturn(long uid)
	{
		ZLog.Log("Lever return");
		this.m_leverAnim.SetBool("Pulled", false);
	}

	// Token: 0x0400176A RID: 5994
	public Switch m_incinerateSwitch;

	// Token: 0x0400176B RID: 5995
	public Container m_container;

	// Token: 0x0400176C RID: 5996
	public Animator m_leverAnim;

	// Token: 0x0400176D RID: 5997
	public GameObject m_lightingAOEs;

	// Token: 0x0400176E RID: 5998
	public EffectList m_leverEffects = new EffectList();

	// Token: 0x0400176F RID: 5999
	public float m_effectDelayMin = 5f;

	// Token: 0x04001770 RID: 6000
	public float m_effectDelayMax = 7f;

	// Token: 0x04001771 RID: 6001
	[Header("Conversion")]
	public List<Incinerator.IncineratorConversion> m_conversions;

	// Token: 0x04001772 RID: 6002
	public ItemDrop m_defaultResult;

	// Token: 0x04001773 RID: 6003
	public int m_defaultCost = 1;

	// Token: 0x04001774 RID: 6004
	private ZNetView m_nview;

	// Token: 0x04001775 RID: 6005
	private bool isInUse;

	// Token: 0x0200036A RID: 874
	[Serializable]
	public class IncineratorConversion
	{
		// Token: 0x060022D4 RID: 8916 RVA: 0x000F05D0 File Offset: 0x000EE7D0
		public int AttemptCraft(Inventory inv, List<ItemDrop> toAdd)
		{
			int num = int.MaxValue;
			int num2 = 0;
			Incinerator.Requirement requirement = null;
			foreach (Incinerator.Requirement requirement2 in this.m_requirements)
			{
				int num3 = inv.CountItems(requirement2.m_resItem.m_itemData.m_shared.m_name, -1, true) / requirement2.m_amount;
				if (num3 == 0 && !this.m_requireOnlyOneIngredient)
				{
					return 0;
				}
				if (num3 > num2)
				{
					num2 = num3;
					requirement = requirement2;
				}
				if (num3 < num)
				{
					num = num3;
				}
			}
			int num4 = this.m_requireOnlyOneIngredient ? num2 : num;
			if (num4 == 0)
			{
				return 0;
			}
			if (this.m_requireOnlyOneIngredient)
			{
				inv.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, requirement.m_amount * num4, -1, true);
			}
			else
			{
				foreach (Incinerator.Requirement requirement3 in this.m_requirements)
				{
					inv.RemoveItem(requirement3.m_resItem.m_itemData.m_shared.m_name, requirement3.m_amount * num4, -1, true);
				}
			}
			num4 *= this.m_resultAmount;
			for (int i = 0; i < num4; i++)
			{
				toAdd.Add(this.m_result);
			}
			return num4;
		}

		// Token: 0x04002607 RID: 9735
		public List<Incinerator.Requirement> m_requirements;

		// Token: 0x04002608 RID: 9736
		public ItemDrop m_result;

		// Token: 0x04002609 RID: 9737
		public int m_resultAmount = 1;

		// Token: 0x0400260A RID: 9738
		public int m_priority;

		// Token: 0x0400260B RID: 9739
		[global::Tooltip("True: Requires only one of the list of ingredients to be able to produce the result. False: All of the ingredients are required.")]
		public bool m_requireOnlyOneIngredient;
	}

	// Token: 0x0200036B RID: 875
	[Serializable]
	public class Requirement
	{
		// Token: 0x0400260C RID: 9740
		public ItemDrop m_resItem;

		// Token: 0x0400260D RID: 9741
		public int m_amount = 1;
	}

	// Token: 0x0200036C RID: 876
	public enum Response
	{
		// Token: 0x0400260F RID: 9743
		Fail,
		// Token: 0x04002610 RID: 9744
		Success,
		// Token: 0x04002611 RID: 9745
		Conversion,
		// Token: 0x04002612 RID: 9746
		Empty
	}
}
