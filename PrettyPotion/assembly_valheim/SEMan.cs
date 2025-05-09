using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000031 RID: 49
public class SEMan
{
	// Token: 0x060004AC RID: 1196 RVA: 0x00029E48 File Offset: 0x00028048
	public SEMan(Character character, ZNetView nview)
	{
		this.m_character = character;
		this.m_nview = nview;
		this.m_nview.Register<int, bool, int, float>("RPC_AddStatusEffect", new RoutedMethod<int, bool, int, float>.Method(this.RPC_AddStatusEffect));
	}

	// Token: 0x060004AD RID: 1197 RVA: 0x00029EB0 File Offset: 0x000280B0
	public void OnDestroy()
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.OnDestroy();
		}
		this.m_statusEffects.Clear();
		this.m_statusEffectsHashSet.Clear();
	}

	// Token: 0x060004AE RID: 1198 RVA: 0x00029F18 File Offset: 0x00028118
	public void ApplyStatusEffectSpeedMods(ref float speed, Vector3 dir)
	{
		float baseSpeed = speed;
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifySpeed(baseSpeed, ref speed, this.m_character, dir);
		}
	}

	// Token: 0x060004AF RID: 1199 RVA: 0x00029F74 File Offset: 0x00028174
	public void ApplyStatusEffectJumpMods(ref Vector3 jump)
	{
		Vector3 baseJump = jump;
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyJump(baseJump, ref jump);
		}
	}

	// Token: 0x060004B0 RID: 1200 RVA: 0x00029FD0 File Offset: 0x000281D0
	public void ApplyDamageMods(ref HitData.DamageModifiers mods)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyDamageMods(ref mods);
		}
	}

	// Token: 0x060004B1 RID: 1201 RVA: 0x0002A024 File Offset: 0x00028224
	public void Update(ZDO zdo, float dt)
	{
		this.m_statusEffectAttributes = 0;
		int count = this.m_statusEffects.Count;
		for (int i = 0; i < count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			statusEffect.UpdateStatusEffect(dt);
			if (statusEffect.IsDone())
			{
				this.m_removeStatusEffects.Add(statusEffect);
			}
			else
			{
				this.m_statusEffectAttributes |= (int)statusEffect.m_attributes;
			}
		}
		if (this.m_removeStatusEffects.Count > 0)
		{
			foreach (StatusEffect statusEffect2 in this.m_removeStatusEffects)
			{
				statusEffect2.Stop();
				this.m_statusEffects.Remove(statusEffect2);
				this.m_statusEffectsHashSet.Remove(statusEffect2.NameHash());
			}
			this.m_removeStatusEffects.Clear();
		}
		if (this.m_statusEffectAttributes != this.m_statusEffectAttributesOld)
		{
			zdo.Set(ZDOVars.s_seAttrib, this.m_statusEffectAttributes, false);
			this.m_statusEffectAttributesOld = this.m_statusEffectAttributes;
		}
	}

	// Token: 0x060004B2 RID: 1202 RVA: 0x0002A13C File Offset: 0x0002833C
	public StatusEffect AddStatusEffect(int nameHash, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f)
	{
		if (nameHash == 0)
		{
			return null;
		}
		if (this.m_nview.IsOwner())
		{
			return this.Internal_AddStatusEffect(nameHash, resetTime, itemLevel, skillLevel);
		}
		this.m_nview.InvokeRPC("RPC_AddStatusEffect", new object[]
		{
			nameHash,
			resetTime,
			itemLevel,
			skillLevel
		});
		return null;
	}

	// Token: 0x060004B3 RID: 1203 RVA: 0x0002A1A3 File Offset: 0x000283A3
	private void RPC_AddStatusEffect(long sender, int nameHash, bool resetTime, int itemLevel, float skillLevel)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.Internal_AddStatusEffect(nameHash, resetTime, itemLevel, skillLevel);
	}

	// Token: 0x060004B4 RID: 1204 RVA: 0x0002A1C0 File Offset: 0x000283C0
	private StatusEffect Internal_AddStatusEffect(int nameHash, bool resetTime, int itemLevel, float skillLevel)
	{
		StatusEffect statusEffect = this.GetStatusEffect(nameHash);
		if (statusEffect)
		{
			if (resetTime)
			{
				statusEffect.ResetTime();
				statusEffect.SetLevel(itemLevel, skillLevel);
			}
			return null;
		}
		StatusEffect statusEffect2 = ObjectDB.instance.GetStatusEffect(nameHash);
		if (statusEffect2 == null)
		{
			return null;
		}
		return this.AddStatusEffect(statusEffect2, false, itemLevel, skillLevel);
	}

	// Token: 0x060004B5 RID: 1205 RVA: 0x0002A214 File Offset: 0x00028414
	public StatusEffect AddStatusEffect(StatusEffect statusEffect, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f)
	{
		StatusEffect statusEffect2 = this.GetStatusEffect(statusEffect.NameHash());
		if (statusEffect2)
		{
			if (resetTime)
			{
				statusEffect2.ResetTime();
				statusEffect2.SetLevel(itemLevel, skillLevel);
			}
			return null;
		}
		if (!statusEffect.CanAdd(this.m_character))
		{
			return null;
		}
		StatusEffect statusEffect3 = statusEffect.Clone();
		this.m_statusEffects.Add(statusEffect3);
		this.m_statusEffectsHashSet.Add(statusEffect3.NameHash());
		statusEffect3.Setup(this.m_character);
		statusEffect3.SetLevel(itemLevel, skillLevel);
		if (this.m_character.IsPlayer())
		{
			Gogan.LogEvent("Game", "StatusEffect", statusEffect.name, 0L);
		}
		return statusEffect3;
	}

	// Token: 0x060004B6 RID: 1206 RVA: 0x0002A2B9 File Offset: 0x000284B9
	public bool RemoveStatusEffect(StatusEffect se, bool quiet = false)
	{
		return this.RemoveStatusEffect(se.NameHash(), quiet);
	}

	// Token: 0x060004B7 RID: 1207 RVA: 0x0002A2C8 File Offset: 0x000284C8
	public bool RemoveStatusEffect(int nameHash, bool quiet = false)
	{
		if (nameHash == 0)
		{
			return false;
		}
		for (int i = 0; i < this.m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (statusEffect.NameHash() == nameHash)
			{
				if (quiet)
				{
					statusEffect.m_stopMessage = "";
				}
				statusEffect.Stop();
				this.m_statusEffects.Remove(statusEffect);
				this.m_statusEffectsHashSet.Remove(nameHash);
				return true;
			}
		}
		return false;
	}

	// Token: 0x060004B8 RID: 1208 RVA: 0x0002A338 File Offset: 0x00028538
	public void RemoveAllStatusEffects(bool quiet = false)
	{
		for (int i = this.m_statusEffects.Count - 1; i >= 0; i--)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (quiet)
			{
				statusEffect.m_stopMessage = "";
			}
			statusEffect.Stop();
			this.m_statusEffects.Remove(statusEffect);
		}
		this.m_statusEffectsHashSet.Clear();
	}

	// Token: 0x060004B9 RID: 1209 RVA: 0x0002A398 File Offset: 0x00028598
	public bool HaveStatusEffectCategory(string cat)
	{
		if (cat.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < this.m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (statusEffect.m_category.Length > 0 && statusEffect.m_category == cat)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060004BA RID: 1210 RVA: 0x0002A3F4 File Offset: 0x000285F4
	public bool HaveStatusAttribute(StatusEffect.StatusAttribute value)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			return (this.m_statusEffectAttributes & (int)value) != 0;
		}
		return (this.m_nview.GetZDO().GetInt(ZDOVars.s_seAttrib, 0) & (int)value) != 0;
	}

	// Token: 0x060004BB RID: 1211 RVA: 0x0002A444 File Offset: 0x00028644
	public bool HaveStatusEffect(int nameHash)
	{
		return this.m_statusEffectsHashSet.Contains(nameHash);
	}

	// Token: 0x060004BC RID: 1212 RVA: 0x0002A452 File Offset: 0x00028652
	public List<StatusEffect> GetStatusEffects()
	{
		return this.m_statusEffects;
	}

	// Token: 0x060004BD RID: 1213 RVA: 0x0002A45C File Offset: 0x0002865C
	public StatusEffect GetStatusEffect(int nameHash)
	{
		if (nameHash == 0)
		{
			return null;
		}
		if (!this.m_statusEffectsHashSet.Contains(nameHash))
		{
			return null;
		}
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			if (statusEffect.NameHash() == nameHash)
			{
				return statusEffect;
			}
		}
		return null;
	}

	// Token: 0x060004BE RID: 1214 RVA: 0x0002A4D0 File Offset: 0x000286D0
	public void GetHUDStatusEffects(List<StatusEffect> effects)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			if (statusEffect.m_icon)
			{
				effects.Add(statusEffect);
			}
		}
	}

	// Token: 0x060004BF RID: 1215 RVA: 0x0002A530 File Offset: 0x00028730
	public void ModifyFallDamage(float baseDamage, ref float damage)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyFallDamage(baseDamage, ref damage);
		}
	}

	// Token: 0x060004C0 RID: 1216 RVA: 0x0002A584 File Offset: 0x00028784
	public void ModifyWalkVelocity(ref Vector3 vel)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyWalkVelocity(ref vel);
		}
	}

	// Token: 0x060004C1 RID: 1217 RVA: 0x0002A5D8 File Offset: 0x000287D8
	public void ModifyNoise(float baseNoise, ref float noise)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyNoise(baseNoise, ref noise);
		}
	}

	// Token: 0x060004C2 RID: 1218 RVA: 0x0002A62C File Offset: 0x0002882C
	public void ModifySkillLevel(Skills.SkillType skill, ref float level)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifySkillLevel(skill, ref level);
		}
	}

	// Token: 0x060004C3 RID: 1219 RVA: 0x0002A680 File Offset: 0x00028880
	public void ModifyRaiseSkill(Skills.SkillType skill, ref float multiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyRaiseSkill(skill, ref multiplier);
		}
	}

	// Token: 0x060004C4 RID: 1220 RVA: 0x0002A6D4 File Offset: 0x000288D4
	public void ModifyStaminaRegen(ref float staminaMultiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyStaminaRegen(ref staminaMultiplier);
		}
	}

	// Token: 0x060004C5 RID: 1221 RVA: 0x0002A728 File Offset: 0x00028928
	public void ModifyEitrRegen(ref float eitrMultiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyEitrRegen(ref eitrMultiplier);
		}
	}

	// Token: 0x060004C6 RID: 1222 RVA: 0x0002A77C File Offset: 0x0002897C
	public void ModifyHealthRegen(ref float regenMultiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyHealthRegen(ref regenMultiplier);
		}
	}

	// Token: 0x060004C7 RID: 1223 RVA: 0x0002A7D0 File Offset: 0x000289D0
	public void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyMaxCarryWeight(baseLimit, ref limit);
		}
	}

	// Token: 0x060004C8 RID: 1224 RVA: 0x0002A824 File Offset: 0x00028A24
	public void ModifyStealth(float baseStealth, ref float stealth)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyStealth(baseStealth, ref stealth);
		}
	}

	// Token: 0x060004C9 RID: 1225 RVA: 0x0002A878 File Offset: 0x00028A78
	public void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyAttack(skill, ref hitData);
		}
	}

	// Token: 0x060004CA RID: 1226 RVA: 0x0002A8CC File Offset: 0x00028ACC
	public void ModifyRunStaminaDrain(float baseDrain, ref float drain, Vector3 dir, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyRunStaminaDrain(baseDrain, ref drain, dir);
		}
		if (minZero && drain < 0f)
		{
			drain = 0f;
		}
	}

	// Token: 0x060004CB RID: 1227 RVA: 0x0002A934 File Offset: 0x00028B34
	public void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyJumpStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	// Token: 0x060004CC RID: 1228 RVA: 0x0002A99C File Offset: 0x00028B9C
	public void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyAttackStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	// Token: 0x060004CD RID: 1229 RVA: 0x0002AA04 File Offset: 0x00028C04
	public void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyBlockStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	// Token: 0x060004CE RID: 1230 RVA: 0x0002AA6C File Offset: 0x00028C6C
	public void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyDodgeStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	// Token: 0x060004CF RID: 1231 RVA: 0x0002AAD4 File Offset: 0x00028CD4
	public void ModifySwimStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifySwimStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	// Token: 0x060004D0 RID: 1232 RVA: 0x0002AB3C File Offset: 0x00028D3C
	public void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyHomeItemStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	// Token: 0x060004D1 RID: 1233 RVA: 0x0002ABA4 File Offset: 0x00028DA4
	public void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifySneakStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	// Token: 0x060004D2 RID: 1234 RVA: 0x0002AC0C File Offset: 0x00028E0C
	public void OnDamaged(HitData hit, Character attacker)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.OnDamaged(hit, attacker);
		}
	}

	// Token: 0x04000538 RID: 1336
	private readonly HashSet<int> m_statusEffectsHashSet = new HashSet<int>();

	// Token: 0x04000539 RID: 1337
	private readonly List<StatusEffect> m_statusEffects = new List<StatusEffect>();

	// Token: 0x0400053A RID: 1338
	private readonly List<StatusEffect> m_removeStatusEffects = new List<StatusEffect>();

	// Token: 0x0400053B RID: 1339
	private int m_statusEffectAttributes;

	// Token: 0x0400053C RID: 1340
	private int m_statusEffectAttributesOld = -1;

	// Token: 0x0400053D RID: 1341
	private Character m_character;

	// Token: 0x0400053E RID: 1342
	private ZNetView m_nview;

	// Token: 0x0400053F RID: 1343
	public static readonly int s_statusEffectRested = "Rested".GetStableHashCode();

	// Token: 0x04000540 RID: 1344
	public static readonly int s_statusEffectEncumbered = "Encumbered".GetStableHashCode();

	// Token: 0x04000541 RID: 1345
	public static readonly int s_statusEffectSoftDeath = "SoftDeath".GetStableHashCode();

	// Token: 0x04000542 RID: 1346
	public static readonly int s_statusEffectWet = "Wet".GetStableHashCode();

	// Token: 0x04000543 RID: 1347
	public static readonly int s_statusEffectShelter = "Shelter".GetStableHashCode();

	// Token: 0x04000544 RID: 1348
	public static readonly int s_statusEffectCampFire = "CampFire".GetStableHashCode();

	// Token: 0x04000545 RID: 1349
	public static readonly int s_statusEffectResting = "Resting".GetStableHashCode();

	// Token: 0x04000546 RID: 1350
	public static readonly int s_statusEffectCold = "Cold".GetStableHashCode();

	// Token: 0x04000547 RID: 1351
	public static readonly int s_statusEffectFreezing = "Freezing".GetStableHashCode();

	// Token: 0x04000548 RID: 1352
	public static readonly int s_statusEffectBurning = "Burning".GetStableHashCode();

	// Token: 0x04000549 RID: 1353
	public static readonly int s_statusEffectFrost = "Frost".GetStableHashCode();

	// Token: 0x0400054A RID: 1354
	public static readonly int s_statusEffectLightning = "Lightning".GetStableHashCode();

	// Token: 0x0400054B RID: 1355
	public static readonly int s_statusEffectPoison = "Poison".GetStableHashCode();

	// Token: 0x0400054C RID: 1356
	public static readonly int s_statusEffectSmoked = "Smoked".GetStableHashCode();

	// Token: 0x0400054D RID: 1357
	public static readonly int s_statusEffectSpirit = "Spirit".GetStableHashCode();

	// Token: 0x0400054E RID: 1358
	public static readonly int s_statusEffectTared = "Tared".GetStableHashCode();
}
