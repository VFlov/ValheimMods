using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200017B RID: 379
public class FishingFloat : MonoBehaviour, IProjectile
{
	// Token: 0x060016E4 RID: 5860 RVA: 0x000AA0B0 File Offset: 0x000A82B0
	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	// Token: 0x060016E5 RID: 5861 RVA: 0x000AA0B8 File Offset: 0x000A82B8
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_floating = base.GetComponent<Floating>();
		this.m_nview.Register<ZDOID, bool>("RPC_Nibble", new Action<long, ZDOID, bool>(this.RPC_Nibble));
		FishingFloat.m_allInstances.Add(this);
	}

	// Token: 0x060016E6 RID: 5862 RVA: 0x000AA110 File Offset: 0x000A8310
	private void OnDestroy()
	{
		FishingFloat.m_allInstances.Remove(this);
	}

	// Token: 0x060016E7 RID: 5863 RVA: 0x000AA120 File Offset: 0x000A8320
	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		FishingFloat fishingFloat = FishingFloat.FindFloat(owner);
		if (fishingFloat)
		{
			ZNetScene.instance.Destroy(fishingFloat.gameObject);
		}
		long userID = owner.GetZDOID().UserID;
		this.m_nview.GetZDO().Set(ZDOVars.s_rodOwner, userID);
		this.m_nview.GetZDO().Set(ZDOVars.s_bait, ammo.m_dropPrefab.name);
		Transform rodTop = this.GetRodTop(owner);
		if (rodTop == null)
		{
			ZLog.LogWarning("Failed to find fishing rod top");
			return;
		}
		this.m_rodLine.SetPeer(owner.GetZDOID());
		this.m_lineLength = Vector3.Distance(rodTop.position, base.transform.position);
		owner.Message(MessageHud.MessageType.Center, this.m_lineLength.ToString("0m"), 0, null);
	}

	// Token: 0x060016E8 RID: 5864 RVA: 0x000AA1F4 File Offset: 0x000A83F4
	private Character GetOwner()
	{
		if (!this.m_nview.IsValid())
		{
			return null;
		}
		long @long = this.m_nview.GetZDO().GetLong(ZDOVars.s_rodOwner, 0L);
		foreach (ZNet.PlayerInfo playerInfo in ZNet.instance.GetPlayerList())
		{
			ZDOID characterID = playerInfo.m_characterID;
			if (characterID.UserID == @long)
			{
				GameObject gameObject = ZNetScene.instance.FindInstance(playerInfo.m_characterID);
				if (gameObject == null)
				{
					return null;
				}
				return gameObject.GetComponent<Character>();
			}
		}
		return null;
	}

	// Token: 0x060016E9 RID: 5865 RVA: 0x000AA2AC File Offset: 0x000A84AC
	private Transform GetRodTop(Character owner)
	{
		Transform transform = Utils.FindChild(owner.transform, "_RodTop", Utils.IterativeSearchType.DepthFirst);
		if (transform == null)
		{
			ZLog.LogWarning("Failed to find fishing rod top");
			return null;
		}
		return transform;
	}

	// Token: 0x060016EA RID: 5866 RVA: 0x000AA2E4 File Offset: 0x000A84E4
	private void FixedUpdate()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		Character owner = this.GetOwner();
		if (!owner)
		{
			ZLog.LogWarning("Fishing rod not found, destroying fishing float");
			this.m_nview.Destroy();
			return;
		}
		Transform rodTop = this.GetRodTop(owner);
		if (!rodTop)
		{
			ZLog.LogWarning("Fishing rod not found, destroying fishing float");
			this.m_nview.Destroy();
			return;
		}
		Fish fish = this.GetCatch();
		if (owner.InAttack() || owner.IsDrawingBow())
		{
			this.ReturnBait();
			if (fish)
			{
				fish.OnHooked(null);
			}
			this.m_nview.Destroy();
			return;
		}
		float magnitude = (rodTop.transform.position - base.transform.position).magnitude;
		ItemDrop itemDrop = fish ? fish.gameObject.GetComponent<ItemDrop>() : null;
		if (!owner.HaveStamina(0f) && fish != null)
		{
			this.SetCatch(null);
			fish = null;
			this.Message("$msg_fishing_lost", true);
		}
		float skillFactor = owner.GetSkillFactor(Skills.SkillType.Fishing);
		float num = Mathf.Lerp(this.m_hookedStaminaPerSec, this.m_hookedStaminaPerSecMaxSkill, skillFactor);
		if (fish)
		{
			owner.UseStamina(num * fixedDeltaTime);
		}
		if (!fish && Utils.LengthXZ(this.m_body.velocity) > 2f)
		{
			this.TryToHook();
		}
		if (owner.IsBlocking() && owner.HaveStamina(0f))
		{
			float num2 = this.m_pullStaminaUse;
			if (fish != null)
			{
				num2 += fish.GetStaminaUse() * (float)((itemDrop == null) ? 1 : itemDrop.m_itemData.m_quality);
			}
			num2 = Mathf.Lerp(num2, num2 * this.m_pullStaminaUseMaxSkillMultiplier, skillFactor);
			owner.UseStamina(num2 * fixedDeltaTime);
			if (this.m_lineLength > magnitude - 0.2f)
			{
				float lineLength = this.m_lineLength;
				float num3 = Mathf.Lerp(this.m_pullLineSpeed, this.m_pullLineSpeedMaxSkill, skillFactor);
				if (fish && fish.IsEscaping())
				{
					num3 /= 2f;
				}
				this.m_lineLength -= fixedDeltaTime * num3;
				this.m_fishingSkillImproveTimer += fixedDeltaTime * ((fish == null) ? 1f : this.m_fishingSkillImproveHookedMultiplier);
				if (this.m_fishingSkillImproveTimer > 1f)
				{
					this.m_fishingSkillImproveTimer = 0f;
					owner.RaiseSkill(Skills.SkillType.Fishing, 1f);
				}
				this.TryToHook();
				if ((int)this.m_lineLength != (int)lineLength)
				{
					this.Message(this.m_lineLength.ToString("0m"), false);
				}
			}
			if (this.m_lineLength <= 0.5f)
			{
				if (fish)
				{
					string msg = FishingFloat.Catch(fish, owner);
					this.Message(msg, true);
					this.SetCatch(null);
					fish.OnHooked(null);
					this.m_nview.Destroy();
					return;
				}
				this.ReturnBait();
				this.m_nview.Destroy();
				return;
			}
		}
		this.m_rodLine.SetSlack((1f - Utils.LerpStep(this.m_lineLength / 2f, this.m_lineLength, magnitude)) * this.m_maxLineSlack);
		if (magnitude - this.m_lineLength > this.m_breakDistance || magnitude > this.m_maxDistance)
		{
			this.Message("$msg_fishing_linebroke", true);
			if (fish)
			{
				fish.OnHooked(null);
			}
			this.m_nview.Destroy();
			this.m_lineBreakEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
			return;
		}
		if (fish)
		{
			Utils.Pull(this.m_body, fish.transform.position, 0.5f, this.m_moveForce, 0.5f, 0.3f, false, false, 1f);
		}
		Utils.Pull(this.m_body, rodTop.transform.position, this.m_lineLength, this.m_moveForce, 1f, 0.3f, false, false, 1f);
	}

	// Token: 0x060016EB RID: 5867 RVA: 0x000AA6E0 File Offset: 0x000A88E0
	public static string Catch(Fish fish, Character owner)
	{
		Humanoid humanoid = owner as Humanoid;
		ItemDrop itemDrop = fish ? fish.gameObject.GetComponent<ItemDrop>() : null;
		if (itemDrop)
		{
			itemDrop.Pickup(humanoid);
		}
		else
		{
			fish.Pickup(humanoid);
		}
		string text = "$msg_fishing_catched " + fish.GetHoverName();
		if (!fish.m_extraDrops.IsEmpty())
		{
			foreach (ItemDrop.ItemData itemData in fish.m_extraDrops.GetDropListItems())
			{
				text = text + " & " + itemData.m_shared.m_name;
				if (humanoid.GetInventory().CanAddItem(itemData.m_dropPrefab, itemData.m_stack))
				{
					ZLog.Log(string.Format("picking up {0}x {1}", itemData.m_stack, itemData.m_dropPrefab.name));
					humanoid.GetInventory().AddItem(itemData.m_dropPrefab, itemData.m_stack);
				}
				else
				{
					ZLog.Log(string.Format("no room, dropping {0}x {1}", itemData.m_stack, itemData.m_dropPrefab.name));
					UnityEngine.Object.Instantiate<GameObject>(itemData.m_dropPrefab, fish.transform.position, Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f)).GetComponent<ItemDrop>().SetStack(itemData.m_stack);
					Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$inventory_full"), 0, null);
				}
			}
		}
		return text;
	}

	// Token: 0x060016EC RID: 5868 RVA: 0x000AA89C File Offset: 0x000A8A9C
	private void ReturnBait()
	{
		if (this.m_baitConsumed)
		{
			return;
		}
		Character owner = this.GetOwner();
		string bait = this.GetBait();
		GameObject prefab = ZNetScene.instance.GetPrefab(bait);
		if (prefab)
		{
			Player player = owner as Player;
			if (player != null)
			{
				player.GetInventory().AddItem(prefab, 1);
			}
		}
	}

	// Token: 0x060016ED RID: 5869 RVA: 0x000AA8EC File Offset: 0x000A8AEC
	private void TryToHook()
	{
		if (this.m_nibbler != null && Time.time - this.m_nibbleTime < 0.5f && this.GetCatch() == null)
		{
			this.Message("$msg_fishing_hooked", true);
			this.SetCatch(this.m_nibbler);
			this.m_nibbler = null;
		}
	}

	// Token: 0x060016EE RID: 5870 RVA: 0x000AA948 File Offset: 0x000A8B48
	private void SetCatch(Fish fish)
	{
		if (fish)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_sessionCatchID, fish.GetZDOID());
			this.m_hookLine.SetPeer(fish.GetZDOID());
			fish.OnHooked(this);
			this.m_baitConsumed = true;
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_sessionCatchID, ZDOID.None);
		this.m_hookLine.SetPeer(ZDOID.None);
	}

	// Token: 0x060016EF RID: 5871 RVA: 0x000AA9C4 File Offset: 0x000A8BC4
	public Fish GetCatch()
	{
		if (!this.m_nview.IsValid())
		{
			return null;
		}
		ZDOID zdoid = this.m_nview.GetZDO().GetZDOID(ZDOVars.s_sessionCatchID);
		if (!zdoid.IsNone())
		{
			GameObject gameObject = ZNetScene.instance.FindInstance(zdoid);
			if (gameObject)
			{
				return gameObject.GetComponent<Fish>();
			}
		}
		return null;
	}

	// Token: 0x060016F0 RID: 5872 RVA: 0x000AAA1B File Offset: 0x000A8C1B
	public string GetBait()
	{
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return null;
		}
		return this.m_nview.GetZDO().GetString(ZDOVars.s_bait, "");
	}

	// Token: 0x060016F1 RID: 5873 RVA: 0x000AAA54 File Offset: 0x000A8C54
	public bool IsInWater()
	{
		return this.m_floating.HaveLiquidLevel();
	}

	// Token: 0x060016F2 RID: 5874 RVA: 0x000AAA61 File Offset: 0x000A8C61
	public void Nibble(Fish fish, bool correctBait)
	{
		this.m_nview.InvokeRPC("RPC_Nibble", new object[]
		{
			fish.GetZDOID(),
			correctBait
		});
	}

	// Token: 0x060016F3 RID: 5875 RVA: 0x000AAA90 File Offset: 0x000A8C90
	public void RPC_Nibble(long sender, ZDOID fishID, bool correctBait)
	{
		if (Time.time - this.m_nibbleTime < 1f)
		{
			return;
		}
		if (this.GetCatch() != null)
		{
			return;
		}
		if (correctBait)
		{
			this.m_nibbleEffect.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
			this.m_body.AddForce(Vector3.down * this.m_nibbleForce, ForceMode.VelocityChange);
			GameObject gameObject = ZNetScene.instance.FindInstance(fishID);
			if (gameObject)
			{
				this.m_nibbler = gameObject.GetComponent<Fish>();
				this.m_nibbleTime = Time.time;
				return;
			}
		}
		else
		{
			this.m_body.AddForce(Vector3.down * this.m_nibbleForce * 0.5f, ForceMode.VelocityChange);
			this.Message("$msg_fishing_wrongbait", true);
		}
	}

	// Token: 0x060016F4 RID: 5876 RVA: 0x000AAB65 File Offset: 0x000A8D65
	public static List<FishingFloat> GetAllInstances()
	{
		return FishingFloat.m_allInstances;
	}

	// Token: 0x060016F5 RID: 5877 RVA: 0x000AAB6C File Offset: 0x000A8D6C
	private static FishingFloat FindFloat(Character owner)
	{
		foreach (FishingFloat fishingFloat in FishingFloat.m_allInstances)
		{
			if (owner == fishingFloat.GetOwner())
			{
				return fishingFloat;
			}
		}
		return null;
	}

	// Token: 0x060016F6 RID: 5878 RVA: 0x000AABCC File Offset: 0x000A8DCC
	public static FishingFloat FindFloat(Fish fish)
	{
		foreach (FishingFloat fishingFloat in FishingFloat.m_allInstances)
		{
			if (fishingFloat.GetCatch() == fish)
			{
				return fishingFloat;
			}
		}
		return null;
	}

	// Token: 0x060016F7 RID: 5879 RVA: 0x000AAC2C File Offset: 0x000A8E2C
	private void Message(string msg, bool prioritized = false)
	{
		if (!prioritized && Time.time - this.m_msgTime < 1f)
		{
			return;
		}
		this.m_msgTime = Time.time;
		Character owner = this.GetOwner();
		if (owner)
		{
			owner.Message(MessageHud.MessageType.Center, Localization.instance.Localize(msg), 0, null);
		}
	}

	// Token: 0x040016D2 RID: 5842
	public float m_maxDistance = 30f;

	// Token: 0x040016D3 RID: 5843
	public float m_moveForce = 10f;

	// Token: 0x040016D4 RID: 5844
	public float m_pullLineSpeed = 1f;

	// Token: 0x040016D5 RID: 5845
	public float m_pullLineSpeedMaxSkill = 2f;

	// Token: 0x040016D6 RID: 5846
	public float m_pullStaminaUse = 10f;

	// Token: 0x040016D7 RID: 5847
	public float m_pullStaminaUseMaxSkillMultiplier = 0.2f;

	// Token: 0x040016D8 RID: 5848
	public float m_hookedStaminaPerSec = 1f;

	// Token: 0x040016D9 RID: 5849
	public float m_hookedStaminaPerSecMaxSkill = 0.2f;

	// Token: 0x040016DA RID: 5850
	private float m_fishingSkillImproveTimer;

	// Token: 0x040016DB RID: 5851
	private float m_fishingSkillImproveHookedMultiplier = 2f;

	// Token: 0x040016DC RID: 5852
	private bool m_baitConsumed;

	// Token: 0x040016DD RID: 5853
	public float m_breakDistance = 4f;

	// Token: 0x040016DE RID: 5854
	public float m_range = 10f;

	// Token: 0x040016DF RID: 5855
	public float m_nibbleForce = 10f;

	// Token: 0x040016E0 RID: 5856
	public EffectList m_nibbleEffect = new EffectList();

	// Token: 0x040016E1 RID: 5857
	public EffectList m_lineBreakEffect = new EffectList();

	// Token: 0x040016E2 RID: 5858
	public float m_maxLineSlack = 0.3f;

	// Token: 0x040016E3 RID: 5859
	public LineConnect m_rodLine;

	// Token: 0x040016E4 RID: 5860
	public LineConnect m_hookLine;

	// Token: 0x040016E5 RID: 5861
	private ZNetView m_nview;

	// Token: 0x040016E6 RID: 5862
	private Rigidbody m_body;

	// Token: 0x040016E7 RID: 5863
	private Floating m_floating;

	// Token: 0x040016E8 RID: 5864
	private float m_lineLength;

	// Token: 0x040016E9 RID: 5865
	private float m_msgTime;

	// Token: 0x040016EA RID: 5866
	private Fish m_nibbler;

	// Token: 0x040016EB RID: 5867
	private float m_nibbleTime;

	// Token: 0x040016EC RID: 5868
	private static List<FishingFloat> m_allInstances = new List<FishingFloat>();
}
