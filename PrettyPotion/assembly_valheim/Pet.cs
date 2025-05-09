using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Token: 0x02000026 RID: 38
public class Pet : MonoBehaviour, Hoverable, Interactable, IRemoved, IPlaced
{
	// Token: 0x060002F8 RID: 760 RVA: 0x0001A664 File Offset: 0x00018864
	private void Awake()
	{
		this.m_tameable = base.GetComponent<Tameable>();
		this.m_procreation = base.GetComponent<Procreation>();
		this.m_materialVariation = base.GetComponentInChildren<MaterialVariation>();
		this.m_itemStand = base.GetComponent<ItemStand>();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_renderer = base.GetComponentInChildren<Renderer>();
		this.m_randomSpeak = base.GetComponent<RandomSpeak>();
		if (this.m_materialVariation)
		{
			base.InvokeRepeating("UpdateMaterial", (float)UnityEngine.Random.Range(1, this.m_UpdateRate), (float)this.m_UpdateRate);
		}
		Tameable tameable = this.m_tameable;
		tameable.m_tameTextGetter = (Tameable.TextGetter)Delegate.Combine(tameable.m_tameTextGetter, new Tameable.TextGetter(this.GetStr));
	}

	// Token: 0x060002F9 RID: 761 RVA: 0x0001A718 File Offset: 0x00018918
	private void UpdateMaterial()
	{
		if (this.m_nview == null)
		{
			return;
		}
		int material = this.m_materialVariation.GetMaterial();
		if (this.m_randomSpeak != null)
		{
			this.m_randomSpeak.enabled = (material != 5 && material != 6);
		}
		if (!this.m_nview.IsOwner() || this.m_renderer.isVisible)
		{
			return;
		}
		List<Player> allPlayers = Player.GetAllPlayers();
		float num = 99999f;
		float num2 = 0.5f;
		if (this.m_materialVariation.GetMaterial() == 5)
		{
			num2 = 0.07f;
		}
		foreach (Player player in allPlayers)
		{
			float num3 = Utils.DistanceXZ(player.transform.position, base.transform.position);
			if (num > num3)
			{
				num = num3;
			}
			if (num3 <= 10f)
			{
				SEMan seman = player.GetSEMan();
				if (seman != null)
				{
					if (seman.HaveStatusEffect(SEMan.s_statusEffectSoftDeath) && UnityEngine.Random.value > num2)
					{
						this.SetFace(1);
						return;
					}
					if ((seman.HaveStatusEffect(SEMan.s_statusEffectRested) || seman.HaveStatusEffect(SEMan.s_statusEffectCampFire) || this.m_procreation.GetLovePoints() > 2) && UnityEngine.Random.value > num2)
					{
						this.SetFace(0);
						return;
					}
					if (seman.HaveStatusEffect(SEMan.s_statusEffectBurning) || seman.HaveStatusEffect(SEMan.s_statusEffectFreezing) || (seman.HaveStatusEffect(SEMan.s_statusEffectPoison) && UnityEngine.Random.value > num2))
					{
						this.SetFace(3);
						return;
					}
					if (seman.HaveStatusEffect(SEMan.s_statusEffectEncumbered) && UnityEngine.Random.value > num2)
					{
						this.SetFace(7);
						return;
					}
					if (seman.HaveStatusEffect(SEMan.s_statusEffectSmoked) && UnityEngine.Random.value > num2)
					{
						this.SetFace(5);
						return;
					}
					if (DateTime.Now - TimeSpan.FromSeconds((double)this.m_UpdateRate) < Player.LastEmoteTime)
					{
						if (Player.LastEmote == "cry" && UnityEngine.Random.value > num2)
						{
							this.SetFace((UnityEngine.Random.value > 0.5f) ? 3 : 1);
							return;
						}
						if ((Player.LastEmote == "cheer" || Player.LastEmote == "toast" || Player.LastEmote == "flex" || Player.LastEmote == "laugh") && UnityEngine.Random.value > num2)
						{
							this.SetFace((UnityEngine.Random.value > 0.5f) ? 0 : 4);
							return;
						}
						if ((Player.LastEmote == "blowkiss" || Player.LastEmote == "dance" || Player.LastEmote == "shrug" || Player.LastEmote == "roar") && UnityEngine.Random.value > num2)
						{
							this.SetFace((UnityEngine.Random.value > 0.5f) ? 5 : 7);
							return;
						}
						if ((Player.LastEmote == "kneel" || Player.LastEmote == "bow" || Player.LastEmote == "sit") && UnityEngine.Random.value > num2)
						{
							this.SetFace((UnityEngine.Random.value > 0.5f) ? 4 : 2);
							return;
						}
					}
				}
			}
		}
		if (UnityEngine.Random.value < 0.1f && (allPlayers.Count == 1 || num > 20f))
		{
			this.SetFace(UnityEngine.Random.Range(0, this.m_materialVariation.m_materials.Count));
			return;
		}
	}

	// Token: 0x060002FA RID: 762 RVA: 0x0001AAD0 File Offset: 0x00018CD0
	public void SetFace(int index)
	{
		this.m_materialVariation.SetMaterial(index);
		if (this.m_randomSpeak != null)
		{
			this.m_randomSpeak.enabled = (this.m_materialVariation.GetMaterial() != 7);
		}
	}

	// Token: 0x060002FB RID: 763 RVA: 0x0001AB08 File Offset: 0x00018D08
	private string GetStr()
	{
		EnvMan instance = EnvMan.instance;
		float dayFraction = instance.GetDayFraction();
		if (this.m_deepKnowledge.Count > 0 && UnityEngine.Random.value < 0.1f && (dayFraction <= 0.15f || dayFraction >= 0.85f))
		{
			return this.m_deepKnowledge[instance.GetDay() % this.m_deepKnowledge.Count];
		}
		Vector3 position = base.transform.position;
		string name = EnvMan.instance.GetCurrentEnvironment().m_name;
		if (position.x * position.x > 106300000f - position.z * position.z)
		{
			return Encoding.UTF8.GetString(new byte[]
			{
				102,
				97,
				114,
				32,
				111,
				117,
				116,
				32,
				100,
				117,
				100,
				101
			});
		}
		if (position.y > 4000f && position.y < 5090f && (name.GetHashCode() & 16777215) == 16001704)
		{
			return Encoding.UTF8.GetString(new byte[]
			{
				100,
				101,
				101,
				112,
				32,
				114,
				111,
				99,
				107
			});
		}
		return null;
	}

	// Token: 0x060002FC RID: 764 RVA: 0x0001AC10 File Offset: 0x00018E10
	public string GetHoverName()
	{
		return this.m_tameable.GetHoverName();
	}

	// Token: 0x060002FD RID: 765 RVA: 0x0001AC20 File Offset: 0x00018E20
	public string GetHoverText()
	{
		string text = this.m_tameable.GetHoverText();
		if (this.m_itemStand)
		{
			if (this.m_itemStand.HaveAttachment())
			{
				if (ZInput.IsGamepadActive())
				{
					text += Localization.instance.Localize("\n<b>$ui_hold $KEY_Use</b> $piece_itemstand_take ( " + this.m_itemStand.m_currentItemName + " )");
				}
				else
				{
					text += Localization.instance.Localize("\n[<color=yellow><b>$ui_hold $KEY_Use</b></color>] $piece_itemstand_take ( " + this.m_itemStand.m_currentItemName + " )");
				}
			}
			text += Localization.instance.Localize("\n[<color=yellow><b>1-8</b></color>] $piece_itemstand_attach");
		}
		return text;
	}

	// Token: 0x060002FE RID: 766 RVA: 0x0001ACCC File Offset: 0x00018ECC
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (Terminal.m_showTests)
		{
			this.SetFace(UnityEngine.Random.Range(0, this.m_materialVariation.m_materials.Count));
		}
		if (hold && this.m_itemStand && this.m_itemStand.HaveAttachment())
		{
			return this.m_itemStand.Interact(user, false, alt);
		}
		return this.m_tameable.Interact(user, hold, alt);
	}

	// Token: 0x060002FF RID: 767 RVA: 0x0001AD38 File Offset: 0x00018F38
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.m_FeedItem != null && item.m_shared.m_name == this.m_FeedItem.m_itemData.m_shared.m_name)
		{
			if (this.m_materialVariation.GetMaterial() == 7)
			{
				this.SetFace(2);
			}
			else if (Terminal.m_showTests)
			{
				this.SetFace(UnityEngine.Random.Range(0, this.m_materialVariation.m_materials.Count));
			}
			else if (UnityEngine.Random.value < 0.02f)
			{
				this.SetFace(4);
			}
			user.GetInventory().RemoveItem(item, 1);
			return true;
		}
		if (this.m_itemStand && this.m_itemStand.HaveAttachment())
		{
			this.m_itemStand.Interact(user, false, false);
		}
		ItemStand itemStand = this.m_itemStand;
		return itemStand != null && itemStand.UseItem(user, item);
	}

	// Token: 0x06000300 RID: 768 RVA: 0x0001AE18 File Offset: 0x00019018
	public void OnPlaced()
	{
		string s;
		int num;
		if (Player.m_localPlayer && Player.m_localPlayer.TryGetUniqueKeyValue("Pet", out s) && int.TryParse(s, out num) && num >= 0)
		{
			this.SetFace(num);
			Player.m_localPlayer.RemoveUniqueKeyValue("Pet");
		}
	}

	// Token: 0x06000301 RID: 769 RVA: 0x0001AE6C File Offset: 0x0001906C
	public void OnRemoved()
	{
		if (Player.m_localPlayer && this.m_materialVariation.GetMaterial() != 7)
		{
			Player.m_localPlayer.AddUniqueKeyValue("Pet", this.m_materialVariation.GetMaterial().ToString());
		}
	}

	// Token: 0x040003F8 RID: 1016
	public ItemDrop m_FeedItem;

	// Token: 0x040003F9 RID: 1017
	public int m_UpdateRate = 10;

	// Token: 0x040003FA RID: 1018
	public List<string> m_deepKnowledge = new List<string>();

	// Token: 0x040003FB RID: 1019
	private ItemStand m_itemStand;

	// Token: 0x040003FC RID: 1020
	private Tameable m_tameable;

	// Token: 0x040003FD RID: 1021
	private Procreation m_procreation;

	// Token: 0x040003FE RID: 1022
	private RandomSpeak m_randomSpeak;

	// Token: 0x040003FF RID: 1023
	private MaterialVariation m_materialVariation;

	// Token: 0x04000400 RID: 1024
	private ZNetView m_nview;

	// Token: 0x04000401 RID: 1025
	private Renderer m_renderer;
}
