using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

// Token: 0x02000185 RID: 389
public class HitData
{
	// Token: 0x06001778 RID: 6008 RVA: 0x000AE8D8 File Offset: 0x000ACAD8
	public HitData()
	{
	}

	// Token: 0x06001779 RID: 6009 RVA: 0x000AE934 File Offset: 0x000ACB34
	public HitData(float damage)
	{
		this.m_damage.m_damage = damage;
	}

	// Token: 0x0600177A RID: 6010 RVA: 0x000AE99C File Offset: 0x000ACB9C
	public HitData Clone()
	{
		return (HitData)base.MemberwiseClone();
	}

	// Token: 0x0600177B RID: 6011 RVA: 0x000AE9AC File Offset: 0x000ACBAC
	public void Serialize(ref ZPackage pkg)
	{
		HitData.HitDefaults.SerializeFlags serializeFlags = HitData.HitDefaults.SerializeFlags.None;
		serializeFlags |= ((!this.m_damage.m_damage.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.Damage : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_blunt.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageBlunt : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_slash.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageSlash : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_pierce.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamagePierce : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_chop.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageChop : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_pickaxe.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamagePickaxe : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_fire.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageFire : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_frost.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageFrost : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_lightning.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageLightning : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_poison.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamagePoison : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_spirit.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageSpirit : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_pushForce.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.PushForce : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_backstabBonus.Equals(1f)) ? HitData.HitDefaults.SerializeFlags.BackstabBonus : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_staggerMultiplier.Equals(1f)) ? HitData.HitDefaults.SerializeFlags.StaggerMultiplier : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((this.m_attacker != ZDOID.None) ? HitData.HitDefaults.SerializeFlags.Attacker : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_skillRaiseAmount.Equals(1f)) ? HitData.HitDefaults.SerializeFlags.SkillRaiseAmount : HitData.HitDefaults.SerializeFlags.None);
		pkg.Write((ushort)serializeFlags);
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.Damage) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_damage);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageBlunt) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_blunt);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageSlash) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_slash);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePierce) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_pierce);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageChop) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_chop);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePickaxe) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_pickaxe);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageFire) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_fire);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageFrost) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_frost);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageLightning) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_lightning);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePoison) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_poison);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageSpirit) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_spirit);
		}
		pkg.Write(this.m_toolTier);
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.PushForce) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_pushForce);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.BackstabBonus) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_backstabBonus);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.StaggerMultiplier) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_staggerMultiplier);
		}
		byte b = 0;
		if (this.m_dodgeable)
		{
			b |= 1;
		}
		if (this.m_blockable)
		{
			b |= 2;
		}
		if (this.m_ranged)
		{
			b |= 4;
		}
		if (this.m_ignorePVP)
		{
			b |= 8;
		}
		pkg.Write(b);
		pkg.Write(this.m_point);
		pkg.Write(this.m_dir);
		pkg.Write(this.m_statusEffectHash);
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.Attacker) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_attacker);
		}
		pkg.Write((short)this.m_skill);
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.SkillRaiseAmount) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_skillRaiseAmount);
		}
		pkg.Write((char)this.m_weakSpot);
		pkg.Write(this.m_skillLevel);
		pkg.Write(this.m_itemLevel);
		pkg.Write(this.m_itemWorldLevel);
		pkg.Write((byte)this.m_hitType);
		pkg.Write(this.m_healthReturn);
		pkg.Write(this.m_radius);
	}

	// Token: 0x0600177C RID: 6012 RVA: 0x000AEE0C File Offset: 0x000AD00C
	public void Deserialize(ref ZPackage pkg)
	{
		HitData.HitDefaults.SerializeFlags serializeFlags = (HitData.HitDefaults.SerializeFlags)pkg.ReadUShort();
		this.m_damage.m_damage = (((serializeFlags & HitData.HitDefaults.SerializeFlags.Damage) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_blunt = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageBlunt) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_slash = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageSlash) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_pierce = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePierce) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_chop = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageChop) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_pickaxe = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePickaxe) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_fire = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageFire) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_frost = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageFrost) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_lightning = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageLightning) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_poison = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePoison) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_spirit = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageSpirit) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_toolTier = pkg.ReadShort();
		this.m_pushForce = (((serializeFlags & HitData.HitDefaults.SerializeFlags.PushForce) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_backstabBonus = (((serializeFlags & HitData.HitDefaults.SerializeFlags.BackstabBonus) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 1f);
		this.m_staggerMultiplier = (((serializeFlags & HitData.HitDefaults.SerializeFlags.StaggerMultiplier) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 1f);
		byte b = pkg.ReadByte();
		this.m_dodgeable = ((b & 1) > 0);
		this.m_blockable = ((b & 2) > 0);
		this.m_ranged = ((b & 4) > 0);
		this.m_ignorePVP = ((b & 8) > 0);
		this.m_point = pkg.ReadVector3();
		this.m_dir = pkg.ReadVector3();
		this.m_statusEffectHash = pkg.ReadInt();
		this.m_attacker = (((serializeFlags & HitData.HitDefaults.SerializeFlags.Attacker) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadZDOID() : HitData.HitDefaults.s_attackerDefault);
		this.m_skill = (Skills.SkillType)pkg.ReadShort();
		this.m_skillRaiseAmount = (((serializeFlags & HitData.HitDefaults.SerializeFlags.SkillRaiseAmount) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 1f);
		this.m_weakSpot = (short)pkg.ReadChar();
		this.m_skillLevel = pkg.ReadSingle();
		this.m_itemLevel = pkg.ReadShort();
		this.m_itemWorldLevel = pkg.ReadByte();
		this.m_hitType = (HitData.HitType)pkg.ReadByte();
		this.m_healthReturn = pkg.ReadSingle();
		this.m_radius = pkg.ReadSingle();
	}

	// Token: 0x0600177D RID: 6013 RVA: 0x000AF0E6 File Offset: 0x000AD2E6
	public float GetTotalPhysicalDamage()
	{
		return this.m_damage.GetTotalPhysicalDamage();
	}

	// Token: 0x0600177E RID: 6014 RVA: 0x000AF0F3 File Offset: 0x000AD2F3
	public float GetTotalElementalDamage()
	{
		return this.m_damage.GetTotalElementalDamage();
	}

	// Token: 0x0600177F RID: 6015 RVA: 0x000AF100 File Offset: 0x000AD300
	public float GetTotalDamage()
	{
		Character attacker = this.GetAttacker();
		if (attacker != null && Game.m_worldLevel > 0 && !attacker.IsPlayer())
		{
			return this.m_damage.GetTotalDamage() + (float)(Game.m_worldLevel * Game.instance.m_worldLevelEnemyBaseDamage);
		}
		return this.m_damage.GetTotalDamage();
	}

	// Token: 0x06001780 RID: 6016 RVA: 0x000AF158 File Offset: 0x000AD358
	private float ApplyModifier(float baseDamage, HitData.DamageModifier mod, ref float normalDmg, ref float resistantDmg, ref float weakDmg, ref float immuneDmg)
	{
		if (mod == HitData.DamageModifier.Ignore)
		{
			return 0f;
		}
		float num = baseDamage;
		switch (mod)
		{
		case HitData.DamageModifier.Resistant:
			num /= 2f;
			resistantDmg += baseDamage;
			return num;
		case HitData.DamageModifier.Weak:
			num *= 1.5f;
			weakDmg += baseDamage;
			return num;
		case HitData.DamageModifier.Immune:
			num = 0f;
			immuneDmg += baseDamage;
			return num;
		case HitData.DamageModifier.VeryResistant:
			num /= 4f;
			resistantDmg += baseDamage;
			return num;
		case HitData.DamageModifier.VeryWeak:
			num *= 2f;
			weakDmg += baseDamage;
			return num;
		}
		normalDmg += baseDamage;
		return num;
	}

	// Token: 0x06001781 RID: 6017 RVA: 0x000AF1F4 File Offset: 0x000AD3F4
	public void ApplyResistance(HitData.DamageModifiers modifiers, out HitData.DamageModifier significantModifier)
	{
		float damage = this.m_damage.m_damage;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		this.m_damage.m_blunt = this.ApplyModifier(this.m_damage.m_blunt, modifiers.m_blunt, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_slash = this.ApplyModifier(this.m_damage.m_slash, modifiers.m_slash, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_pierce = this.ApplyModifier(this.m_damage.m_pierce, modifiers.m_pierce, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_chop = this.ApplyModifier(this.m_damage.m_chop, modifiers.m_chop, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_pickaxe = this.ApplyModifier(this.m_damage.m_pickaxe, modifiers.m_pickaxe, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_fire = this.ApplyModifier(this.m_damage.m_fire, modifiers.m_fire, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_frost = this.ApplyModifier(this.m_damage.m_frost, modifiers.m_frost, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_lightning = this.ApplyModifier(this.m_damage.m_lightning, modifiers.m_lightning, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_poison = this.ApplyModifier(this.m_damage.m_poison, modifiers.m_poison, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_spirit = this.ApplyModifier(this.m_damage.m_spirit, modifiers.m_spirit, ref damage, ref num, ref num2, ref num3);
		significantModifier = HitData.DamageModifier.Immune;
		if (num3 >= num && num3 >= num2 && num3 >= damage)
		{
			significantModifier = HitData.DamageModifier.Immune;
		}
		if (damage >= num && damage >= num2 && damage >= num3)
		{
			significantModifier = HitData.DamageModifier.Normal;
		}
		if (num >= num2 && num >= num3 && num >= damage)
		{
			significantModifier = HitData.DamageModifier.Resistant;
		}
		if (num2 >= num && num2 >= num3 && num2 >= damage)
		{
			significantModifier = HitData.DamageModifier.Weak;
		}
	}

	// Token: 0x06001782 RID: 6018 RVA: 0x000AF402 File Offset: 0x000AD602
	public void ApplyArmor(float ac)
	{
		this.m_damage.ApplyArmor(ac);
	}

	// Token: 0x06001783 RID: 6019 RVA: 0x000AF410 File Offset: 0x000AD610
	public void ApplyModifier(float multiplier)
	{
		this.m_damage.m_blunt = this.m_damage.m_blunt * multiplier;
		this.m_damage.m_slash = this.m_damage.m_slash * multiplier;
		this.m_damage.m_pierce = this.m_damage.m_pierce * multiplier;
		this.m_damage.m_chop = this.m_damage.m_chop * multiplier;
		this.m_damage.m_pickaxe = this.m_damage.m_pickaxe * multiplier;
		this.m_damage.m_fire = this.m_damage.m_fire * multiplier;
		this.m_damage.m_frost = this.m_damage.m_frost * multiplier;
		this.m_damage.m_lightning = this.m_damage.m_lightning * multiplier;
		this.m_damage.m_poison = this.m_damage.m_poison * multiplier;
		this.m_damage.m_spirit = this.m_damage.m_spirit * multiplier;
	}

	// Token: 0x06001784 RID: 6020 RVA: 0x000AF4BD File Offset: 0x000AD6BD
	public float GetTotalBlockableDamage()
	{
		return this.m_damage.GetTotalBlockableDamage();
	}

	// Token: 0x06001785 RID: 6021 RVA: 0x000AF4CC File Offset: 0x000AD6CC
	public void BlockDamage(float damage)
	{
		float totalBlockableDamage = this.GetTotalBlockableDamage();
		float num = Mathf.Max(0f, totalBlockableDamage - damage);
		if (totalBlockableDamage <= 0f)
		{
			return;
		}
		float num2 = num / totalBlockableDamage;
		this.m_damage.m_blunt = this.m_damage.m_blunt * num2;
		this.m_damage.m_slash = this.m_damage.m_slash * num2;
		this.m_damage.m_pierce = this.m_damage.m_pierce * num2;
		this.m_damage.m_fire = this.m_damage.m_fire * num2;
		this.m_damage.m_frost = this.m_damage.m_frost * num2;
		this.m_damage.m_lightning = this.m_damage.m_lightning * num2;
		this.m_damage.m_poison = this.m_damage.m_poison * num2;
		this.m_damage.m_spirit = this.m_damage.m_spirit * num2;
	}

	// Token: 0x06001786 RID: 6022 RVA: 0x000AF57B File Offset: 0x000AD77B
	public bool HaveAttacker()
	{
		return !this.m_attacker.IsNone();
	}

	// Token: 0x06001787 RID: 6023 RVA: 0x000AF58C File Offset: 0x000AD78C
	public Character GetAttacker()
	{
		if (this.m_attacker.IsNone())
		{
			return null;
		}
		if (ZNetScene.instance == null)
		{
			return null;
		}
		GameObject gameObject = ZNetScene.instance.FindInstance(this.m_attacker);
		if (gameObject == null)
		{
			return null;
		}
		return gameObject.GetComponent<Character>();
	}

	// Token: 0x06001788 RID: 6024 RVA: 0x000AF5D9 File Offset: 0x000AD7D9
	public void SetAttacker(Character attacker)
	{
		if (attacker)
		{
			this.m_attacker = attacker.GetZDOID();
			return;
		}
		this.m_attacker = ZDOID.None;
	}

	// Token: 0x06001789 RID: 6025 RVA: 0x000AF5FB File Offset: 0x000AD7FB
	public bool CheckToolTier(int minToolTier, bool alwaysAllowTierZero = false)
	{
		return ((int)this.m_itemWorldLevel >= Game.m_worldLevel || !ZoneSystem.instance.GetGlobalKey(GlobalKeys.WorldLevelLockedTools) || (minToolTier <= 0 && alwaysAllowTierZero)) && (int)this.m_toolTier >= minToolTier;
	}

	// Token: 0x0600178A RID: 6026 RVA: 0x000AF630 File Offset: 0x000AD830
	public override string ToString()
	{
		HitData.m_sb.Clear();
		HitData.m_sb.Append(string.Format("Hit: {0}, {1}", this.m_hitType, this.m_damage));
		if (this.m_toolTier > 0)
		{
			HitData.m_sb.Append(string.Format(", Tooltier: {0}", this.m_toolTier));
		}
		if (this.m_itemLevel > 0)
		{
			HitData.m_sb.Append(string.Format(", ItemLevel: {0}", this.m_itemLevel));
		}
		if (this.m_skill != Skills.SkillType.None)
		{
			HitData.m_sb.Append(string.Format(", Skill: {0}", this.m_skill));
		}
		if (this.m_statusEffectHash > 0)
		{
			HitData.m_sb.Append(string.Format(", Statushash: {0}", this.m_statusEffectHash));
		}
		Character attacker = this.GetAttacker();
		if (attacker != null)
		{
			HitData.m_sb.Append(", Attacker: " + attacker.m_name);
		}
		return HitData.m_sb.ToString();
	}

	// Token: 0x04001751 RID: 5969
	private static StringBuilder m_sb = new StringBuilder();

	// Token: 0x04001752 RID: 5970
	public HitData.DamageTypes m_damage;

	// Token: 0x04001753 RID: 5971
	public bool m_dodgeable;

	// Token: 0x04001754 RID: 5972
	public bool m_blockable;

	// Token: 0x04001755 RID: 5973
	public bool m_ranged;

	// Token: 0x04001756 RID: 5974
	public bool m_ignorePVP;

	// Token: 0x04001757 RID: 5975
	public short m_toolTier;

	// Token: 0x04001758 RID: 5976
	public float m_pushForce;

	// Token: 0x04001759 RID: 5977
	public float m_backstabBonus = 1f;

	// Token: 0x0400175A RID: 5978
	public float m_staggerMultiplier = 1f;

	// Token: 0x0400175B RID: 5979
	public Vector3 m_point = Vector3.zero;

	// Token: 0x0400175C RID: 5980
	public Vector3 m_dir = Vector3.zero;

	// Token: 0x0400175D RID: 5981
	public int m_statusEffectHash;

	// Token: 0x0400175E RID: 5982
	public ZDOID m_attacker = ZDOID.None;

	// Token: 0x0400175F RID: 5983
	public Skills.SkillType m_skill;

	// Token: 0x04001760 RID: 5984
	public float m_skillRaiseAmount = 1f;

	// Token: 0x04001761 RID: 5985
	public float m_skillLevel;

	// Token: 0x04001762 RID: 5986
	public short m_itemLevel;

	// Token: 0x04001763 RID: 5987
	public byte m_itemWorldLevel;

	// Token: 0x04001764 RID: 5988
	public HitData.HitType m_hitType;

	// Token: 0x04001765 RID: 5989
	public float m_healthReturn;

	// Token: 0x04001766 RID: 5990
	public float m_radius;

	// Token: 0x04001767 RID: 5991
	public short m_weakSpot = -1;

	// Token: 0x04001768 RID: 5992
	public Collider m_hitCollider;

	// Token: 0x02000363 RID: 867
	private struct HitDefaults
	{
		// Token: 0x040025BC RID: 9660
		public const float c_DamageDefault = 0f;

		// Token: 0x040025BD RID: 9661
		public const float c_PushForceDefault = 0f;

		// Token: 0x040025BE RID: 9662
		public const float c_BackstabBonusDefault = 1f;

		// Token: 0x040025BF RID: 9663
		public const float c_StaggerMultiplierDefault = 1f;

		// Token: 0x040025C0 RID: 9664
		public static readonly ZDOID s_attackerDefault = ZDOID.None;

		// Token: 0x040025C1 RID: 9665
		public const float c_SkillRaiseAmountDefault = 1f;

		// Token: 0x020003E6 RID: 998
		[Flags]
		public enum SerializeFlags
		{
			// Token: 0x04002826 RID: 10278
			None = 0,
			// Token: 0x04002827 RID: 10279
			Damage = 1,
			// Token: 0x04002828 RID: 10280
			DamageBlunt = 2,
			// Token: 0x04002829 RID: 10281
			DamageSlash = 4,
			// Token: 0x0400282A RID: 10282
			DamagePierce = 8,
			// Token: 0x0400282B RID: 10283
			DamageChop = 16,
			// Token: 0x0400282C RID: 10284
			DamagePickaxe = 32,
			// Token: 0x0400282D RID: 10285
			DamageFire = 64,
			// Token: 0x0400282E RID: 10286
			DamageFrost = 128,
			// Token: 0x0400282F RID: 10287
			DamageLightning = 256,
			// Token: 0x04002830 RID: 10288
			DamagePoison = 512,
			// Token: 0x04002831 RID: 10289
			DamageSpirit = 1024,
			// Token: 0x04002832 RID: 10290
			PushForce = 2048,
			// Token: 0x04002833 RID: 10291
			BackstabBonus = 4096,
			// Token: 0x04002834 RID: 10292
			StaggerMultiplier = 8192,
			// Token: 0x04002835 RID: 10293
			Attacker = 16384,
			// Token: 0x04002836 RID: 10294
			SkillRaiseAmount = 32768
		}
	}

	// Token: 0x02000364 RID: 868
	[Flags]
	public enum DamageType
	{
		// Token: 0x040025C3 RID: 9667
		Blunt = 1,
		// Token: 0x040025C4 RID: 9668
		Slash = 2,
		// Token: 0x040025C5 RID: 9669
		Pierce = 4,
		// Token: 0x040025C6 RID: 9670
		Chop = 8,
		// Token: 0x040025C7 RID: 9671
		Pickaxe = 16,
		// Token: 0x040025C8 RID: 9672
		Fire = 32,
		// Token: 0x040025C9 RID: 9673
		Frost = 64,
		// Token: 0x040025CA RID: 9674
		Lightning = 128,
		// Token: 0x040025CB RID: 9675
		Poison = 256,
		// Token: 0x040025CC RID: 9676
		Spirit = 512,
		// Token: 0x040025CD RID: 9677
		Damage = 1024,
		// Token: 0x040025CE RID: 9678
		Physical = 31,
		// Token: 0x040025CF RID: 9679
		Elemental = 224
	}

	// Token: 0x02000365 RID: 869
	public enum DamageModifier
	{
		// Token: 0x040025D1 RID: 9681
		Normal,
		// Token: 0x040025D2 RID: 9682
		Resistant,
		// Token: 0x040025D3 RID: 9683
		Weak,
		// Token: 0x040025D4 RID: 9684
		Immune,
		// Token: 0x040025D5 RID: 9685
		Ignore,
		// Token: 0x040025D6 RID: 9686
		VeryResistant,
		// Token: 0x040025D7 RID: 9687
		VeryWeak
	}

	// Token: 0x02000366 RID: 870
	public enum HitType : byte
	{
		// Token: 0x040025D9 RID: 9689
		Undefined,
		// Token: 0x040025DA RID: 9690
		EnemyHit,
		// Token: 0x040025DB RID: 9691
		PlayerHit,
		// Token: 0x040025DC RID: 9692
		Fall,
		// Token: 0x040025DD RID: 9693
		Drowning,
		// Token: 0x040025DE RID: 9694
		Burning,
		// Token: 0x040025DF RID: 9695
		Freezing,
		// Token: 0x040025E0 RID: 9696
		Poisoned,
		// Token: 0x040025E1 RID: 9697
		Water,
		// Token: 0x040025E2 RID: 9698
		Smoke,
		// Token: 0x040025E3 RID: 9699
		EdgeOfWorld,
		// Token: 0x040025E4 RID: 9700
		Impact,
		// Token: 0x040025E5 RID: 9701
		Cart,
		// Token: 0x040025E6 RID: 9702
		Tree,
		// Token: 0x040025E7 RID: 9703
		Self,
		// Token: 0x040025E8 RID: 9704
		Structural,
		// Token: 0x040025E9 RID: 9705
		Turret,
		// Token: 0x040025EA RID: 9706
		Boat,
		// Token: 0x040025EB RID: 9707
		Stalagtite,
		// Token: 0x040025EC RID: 9708
		Catapult,
		// Token: 0x040025ED RID: 9709
		CinderFire,
		// Token: 0x040025EE RID: 9710
		AshlandsOcean
	}

	// Token: 0x02000367 RID: 871
	[Serializable]
	public struct DamageModPair
	{
		// Token: 0x040025EF RID: 9711
		public HitData.DamageType m_type;

		// Token: 0x040025F0 RID: 9712
		public HitData.DamageModifier m_modifier;
	}

	// Token: 0x02000368 RID: 872
	[Serializable]
	public struct DamageModifiers
	{
		// Token: 0x060022BA RID: 8890 RVA: 0x000EF32F File Offset: 0x000ED52F
		public HitData.DamageModifiers Clone()
		{
			return (HitData.DamageModifiers)base.MemberwiseClone();
		}

		// Token: 0x060022BB RID: 8891 RVA: 0x000EF348 File Offset: 0x000ED548
		public void Apply(List<HitData.DamageModPair> modifiers)
		{
			foreach (HitData.DamageModPair damageModPair in modifiers)
			{
				HitData.DamageType type = damageModPair.m_type;
				if (type <= HitData.DamageType.Fire)
				{
					if (type <= HitData.DamageType.Chop)
					{
						switch (type)
						{
						case HitData.DamageType.Blunt:
							this.ApplyIfBetter(ref this.m_blunt, damageModPair.m_modifier);
							break;
						case HitData.DamageType.Slash:
							this.ApplyIfBetter(ref this.m_slash, damageModPair.m_modifier);
							break;
						case HitData.DamageType.Blunt | HitData.DamageType.Slash:
							break;
						case HitData.DamageType.Pierce:
							this.ApplyIfBetter(ref this.m_pierce, damageModPair.m_modifier);
							break;
						default:
							if (type == HitData.DamageType.Chop)
							{
								this.ApplyIfBetter(ref this.m_chop, damageModPair.m_modifier);
							}
							break;
						}
					}
					else if (type != HitData.DamageType.Pickaxe)
					{
						if (type == HitData.DamageType.Fire)
						{
							this.ApplyIfBetter(ref this.m_fire, damageModPair.m_modifier);
						}
					}
					else
					{
						this.ApplyIfBetter(ref this.m_pickaxe, damageModPair.m_modifier);
					}
				}
				else if (type <= HitData.DamageType.Lightning)
				{
					if (type != HitData.DamageType.Frost)
					{
						if (type == HitData.DamageType.Lightning)
						{
							this.ApplyIfBetter(ref this.m_lightning, damageModPair.m_modifier);
						}
					}
					else
					{
						this.ApplyIfBetter(ref this.m_frost, damageModPair.m_modifier);
					}
				}
				else if (type != HitData.DamageType.Poison)
				{
					if (type == HitData.DamageType.Spirit)
					{
						this.ApplyIfBetter(ref this.m_spirit, damageModPair.m_modifier);
					}
				}
				else
				{
					this.ApplyIfBetter(ref this.m_poison, damageModPair.m_modifier);
				}
			}
		}

		// Token: 0x060022BC RID: 8892 RVA: 0x000EF4F4 File Offset: 0x000ED6F4
		public HitData.DamageModifier GetModifier(HitData.DamageType type)
		{
			if (type <= HitData.DamageType.Fire)
			{
				if (type <= HitData.DamageType.Chop)
				{
					switch (type)
					{
					case HitData.DamageType.Blunt:
						return this.m_blunt;
					case HitData.DamageType.Slash:
						return this.m_slash;
					case HitData.DamageType.Blunt | HitData.DamageType.Slash:
						break;
					case HitData.DamageType.Pierce:
						return this.m_pierce;
					default:
						if (type == HitData.DamageType.Chop)
						{
							return this.m_chop;
						}
						break;
					}
				}
				else
				{
					if (type == HitData.DamageType.Pickaxe)
					{
						return this.m_pickaxe;
					}
					if (type == HitData.DamageType.Fire)
					{
						return this.m_fire;
					}
				}
			}
			else if (type <= HitData.DamageType.Lightning)
			{
				if (type == HitData.DamageType.Frost)
				{
					return this.m_frost;
				}
				if (type == HitData.DamageType.Lightning)
				{
					return this.m_lightning;
				}
			}
			else
			{
				if (type == HitData.DamageType.Poison)
				{
					return this.m_poison;
				}
				if (type == HitData.DamageType.Spirit)
				{
					return this.m_spirit;
				}
			}
			return HitData.DamageModifier.Normal;
		}

		// Token: 0x060022BD RID: 8893 RVA: 0x000EF5A4 File Offset: 0x000ED7A4
		private void ApplyIfBetter(ref HitData.DamageModifier original, HitData.DamageModifier mod)
		{
			if (this.ShouldOverride(original, mod))
			{
				original = mod;
			}
		}

		// Token: 0x060022BE RID: 8894 RVA: 0x000EF5B4 File Offset: 0x000ED7B4
		private bool ShouldOverride(HitData.DamageModifier a, HitData.DamageModifier b)
		{
			return a != HitData.DamageModifier.Ignore && (b == HitData.DamageModifier.Immune || ((a != HitData.DamageModifier.VeryResistant || b != HitData.DamageModifier.Resistant) && (a != HitData.DamageModifier.VeryWeak || b != HitData.DamageModifier.Weak) && ((a != HitData.DamageModifier.Resistant && a != HitData.DamageModifier.VeryResistant && a != HitData.DamageModifier.Immune) || (b != HitData.DamageModifier.Weak && b != HitData.DamageModifier.VeryWeak))));
		}

		// Token: 0x060022BF RID: 8895 RVA: 0x000EF5F0 File Offset: 0x000ED7F0
		public void Print()
		{
			ZLog.Log("m_blunt " + this.m_blunt.ToString());
			ZLog.Log("m_slash " + this.m_slash.ToString());
			ZLog.Log("m_pierce " + this.m_pierce.ToString());
			ZLog.Log("m_chop " + this.m_chop.ToString());
			ZLog.Log("m_pickaxe " + this.m_pickaxe.ToString());
			ZLog.Log("m_fire " + this.m_fire.ToString());
			ZLog.Log("m_frost " + this.m_frost.ToString());
			ZLog.Log("m_lightning " + this.m_lightning.ToString());
			ZLog.Log("m_poison " + this.m_poison.ToString());
			ZLog.Log("m_spirit " + this.m_spirit.ToString());
		}

		// Token: 0x040025F1 RID: 9713
		public HitData.DamageModifier m_blunt;

		// Token: 0x040025F2 RID: 9714
		public HitData.DamageModifier m_slash;

		// Token: 0x040025F3 RID: 9715
		public HitData.DamageModifier m_pierce;

		// Token: 0x040025F4 RID: 9716
		public HitData.DamageModifier m_chop;

		// Token: 0x040025F5 RID: 9717
		public HitData.DamageModifier m_pickaxe;

		// Token: 0x040025F6 RID: 9718
		public HitData.DamageModifier m_fire;

		// Token: 0x040025F7 RID: 9719
		public HitData.DamageModifier m_frost;

		// Token: 0x040025F8 RID: 9720
		public HitData.DamageModifier m_lightning;

		// Token: 0x040025F9 RID: 9721
		public HitData.DamageModifier m_poison;

		// Token: 0x040025FA RID: 9722
		public HitData.DamageModifier m_spirit;
	}

	// Token: 0x02000369 RID: 873
	[Serializable]
	public struct DamageTypes
	{
		// Token: 0x060022C0 RID: 8896 RVA: 0x000EF740 File Offset: 0x000ED940
		public bool HaveDamage()
		{
			return this.m_damage > 0f || this.m_blunt > 0f || this.m_slash > 0f || this.m_pierce > 0f || this.m_chop > 0f || this.m_pickaxe > 0f || this.m_fire > 0f || this.m_frost > 0f || this.m_lightning > 0f || this.m_poison > 0f || this.m_spirit > 0f;
		}

		// Token: 0x060022C1 RID: 8897 RVA: 0x000EF7E1 File Offset: 0x000ED9E1
		public float GetTotalPhysicalDamage()
		{
			return this.m_blunt + this.m_slash + this.m_pierce;
		}

		// Token: 0x060022C2 RID: 8898 RVA: 0x000EF7F7 File Offset: 0x000ED9F7
		public float GetTotalStaggerDamage()
		{
			return this.m_blunt + this.m_slash + this.m_pierce + this.m_lightning;
		}

		// Token: 0x060022C3 RID: 8899 RVA: 0x000EF814 File Offset: 0x000EDA14
		public float GetTotalBlockableDamage()
		{
			return this.m_blunt + this.m_slash + this.m_pierce + this.m_fire + this.m_frost + this.m_lightning + this.m_poison + this.m_spirit;
		}

		// Token: 0x060022C4 RID: 8900 RVA: 0x000EF84D File Offset: 0x000EDA4D
		public float GetTotalElementalDamage()
		{
			return this.m_fire + this.m_frost + this.m_lightning;
		}

		// Token: 0x060022C5 RID: 8901 RVA: 0x000EF864 File Offset: 0x000EDA64
		public float GetTotalDamage()
		{
			return this.m_damage + this.m_blunt + this.m_slash + this.m_pierce + this.m_chop + this.m_pickaxe + this.m_fire + this.m_frost + this.m_lightning + this.m_poison + this.m_spirit;
		}

		// Token: 0x060022C6 RID: 8902 RVA: 0x000EF8BD File Offset: 0x000EDABD
		public HitData.DamageTypes Clone()
		{
			return (HitData.DamageTypes)base.MemberwiseClone();
		}

		// Token: 0x060022C7 RID: 8903 RVA: 0x000EF8D4 File Offset: 0x000EDAD4
		public void Add(HitData.DamageTypes other, int multiplier = 1)
		{
			this.m_damage += other.m_damage * (float)multiplier;
			this.m_blunt += other.m_blunt * (float)multiplier;
			this.m_slash += other.m_slash * (float)multiplier;
			this.m_pierce += other.m_pierce * (float)multiplier;
			this.m_chop += other.m_chop * (float)multiplier;
			this.m_pickaxe += other.m_pickaxe * (float)multiplier;
			this.m_fire += other.m_fire * (float)multiplier;
			this.m_frost += other.m_frost * (float)multiplier;
			this.m_lightning += other.m_lightning * (float)multiplier;
			this.m_poison += other.m_poison * (float)multiplier;
			this.m_spirit += other.m_spirit * (float)multiplier;
		}

		// Token: 0x060022C8 RID: 8904 RVA: 0x000EF9D4 File Offset: 0x000EDBD4
		public void Modify(float multiplier)
		{
			this.m_damage *= multiplier;
			this.m_blunt *= multiplier;
			this.m_slash *= multiplier;
			this.m_pierce *= multiplier;
			this.m_chop *= multiplier;
			this.m_pickaxe *= multiplier;
			this.m_fire *= multiplier;
			this.m_frost *= multiplier;
			this.m_lightning *= multiplier;
			this.m_poison *= multiplier;
			this.m_spirit *= multiplier;
		}

		// Token: 0x060022C9 RID: 8905 RVA: 0x000EFA7C File Offset: 0x000EDC7C
		public void Modify(HitData.DamageTypes multipliers)
		{
			this.m_damage *= 1f + multipliers.m_damage;
			this.m_blunt *= 1f + multipliers.m_blunt;
			this.m_slash *= 1f + multipliers.m_slash;
			this.m_pierce *= 1f + multipliers.m_pierce;
			this.m_chop *= 1f + multipliers.m_chop;
			this.m_pickaxe *= 1f + multipliers.m_pickaxe;
			this.m_fire *= 1f + multipliers.m_fire;
			this.m_frost *= 1f + multipliers.m_frost;
			this.m_lightning *= 1f + multipliers.m_lightning;
			this.m_poison *= 1f + multipliers.m_poison;
			this.m_spirit *= 1f + multipliers.m_spirit;
		}

		// Token: 0x060022CA RID: 8906 RVA: 0x000EFB9C File Offset: 0x000EDD9C
		public void IncreaseEqually(float totalDamageIncrease, bool seperateUtilityDamage = false)
		{
			HitData.DamageTypes.<>c__DisplayClass21_0 CS$<>8__locals1;
			CS$<>8__locals1.totalDamageIncrease = totalDamageIncrease;
			CS$<>8__locals1.total = this.GetTotalDamage();
			if (CS$<>8__locals1.total <= 0f)
			{
				return;
			}
			if (seperateUtilityDamage)
			{
				float chop = this.m_chop;
				this.m_chop += this.m_chop / CS$<>8__locals1.total * CS$<>8__locals1.totalDamageIncrease;
				CS$<>8__locals1.total -= chop;
			}
			else
			{
				HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_chop, ref CS$<>8__locals1);
			}
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_damage, ref CS$<>8__locals1);
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_blunt, ref CS$<>8__locals1);
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_slash, ref CS$<>8__locals1);
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_pierce, ref CS$<>8__locals1);
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_pickaxe, ref CS$<>8__locals1);
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_fire, ref CS$<>8__locals1);
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_frost, ref CS$<>8__locals1);
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_lightning, ref CS$<>8__locals1);
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_poison, ref CS$<>8__locals1);
			HitData.DamageTypes.<IncreaseEqually>g__increase|21_0(ref this.m_spirit, ref CS$<>8__locals1);
		}

		// Token: 0x060022CB RID: 8907 RVA: 0x000EFC98 File Offset: 0x000EDE98
		public static float ApplyArmor(float dmg, float ac)
		{
			float result = Mathf.Clamp01(dmg / (ac * 4f)) * dmg;
			if (ac < dmg / 2f)
			{
				result = dmg - ac;
			}
			return result;
		}

		// Token: 0x060022CC RID: 8908 RVA: 0x000EFCC8 File Offset: 0x000EDEC8
		public void ApplyArmor(float ac)
		{
			if (ac <= 0f)
			{
				return;
			}
			float num = this.m_blunt + this.m_slash + this.m_pierce + this.m_fire + this.m_frost + this.m_lightning + this.m_poison + this.m_spirit;
			if (num <= 0f)
			{
				return;
			}
			float num2 = HitData.DamageTypes.ApplyArmor(num, ac) / num;
			this.m_blunt *= num2;
			this.m_slash *= num2;
			this.m_pierce *= num2;
			this.m_fire *= num2;
			this.m_frost *= num2;
			this.m_lightning *= num2;
			this.m_poison *= num2;
			this.m_spirit *= num2;
		}

		// Token: 0x060022CD RID: 8909 RVA: 0x000EFD9C File Offset: 0x000EDF9C
		public HitData.DamageType GetMajorityDamageType()
		{
			float num;
			return this.GetMajorityDamageType(out num);
		}

		// Token: 0x060022CE RID: 8910 RVA: 0x000EFDB4 File Offset: 0x000EDFB4
		public HitData.DamageType GetMajorityDamageType(out float damage)
		{
			damage = this.m_damage;
			HitData.DamageType result = HitData.DamageType.Damage;
			if (this.m_slash > damage)
			{
				damage = this.m_slash;
				result = HitData.DamageType.Slash;
			}
			if (this.m_pierce > damage)
			{
				damage = this.m_pierce;
				result = HitData.DamageType.Pierce;
			}
			if (this.m_chop > damage)
			{
				damage = this.m_chop;
				result = HitData.DamageType.Chop;
			}
			if (this.m_pickaxe > damage)
			{
				damage = this.m_pickaxe;
				result = HitData.DamageType.Pickaxe;
			}
			if (this.m_fire > damage)
			{
				damage = this.m_fire;
				result = HitData.DamageType.Fire;
			}
			if (this.m_frost > damage)
			{
				damage = this.m_frost;
				result = HitData.DamageType.Frost;
			}
			if (this.m_lightning > damage)
			{
				damage = this.m_lightning;
				result = HitData.DamageType.Lightning;
			}
			if (this.m_poison > damage)
			{
				damage = this.m_poison;
				result = HitData.DamageType.Poison;
			}
			if (this.m_spirit > damage)
			{
				damage = this.m_spirit;
				result = HitData.DamageType.Spirit;
			}
			return result;
		}

		// Token: 0x060022CF RID: 8911 RVA: 0x000EFE94 File Offset: 0x000EE094
		public string GetTooltipString(Skills.SkillType skillType = Skills.SkillType.None)
		{
			if (Player.m_localPlayer == null)
			{
				return "";
			}
			float num;
			float num2;
			Player.m_localPlayer.GetSkills().GetRandomSkillRange(out num, out num2, skillType);
			HitData.DamageTypes.m_sb.Clear();
			if (this.m_damage != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_damage: <color=orange>{0}</color> <color=yellow>({1}-{2}) </color>", Mathf.RoundToInt(this.m_damage), Mathf.RoundToInt(this.m_damage * num), Mathf.RoundToInt(this.m_damage * num2)));
			}
			if (this.m_blunt != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_blunt: <color=orange>{0}</color> <color=yellow>({1}-{2}) </color>", Mathf.RoundToInt(this.m_blunt), Mathf.RoundToInt(this.m_blunt * num), Mathf.RoundToInt(this.m_blunt * num2)));
			}
			if (this.m_slash != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_slash: <color=orange>{0}</color> <color=yellow>({1}-{2}) </color>", Mathf.RoundToInt(this.m_slash), Mathf.RoundToInt(this.m_slash * num), Mathf.RoundToInt(this.m_slash * num2)));
			}
			if (this.m_pierce != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_pierce: <color=orange>{0}</color> <color=yellow>({1}-{2}) </color>", Mathf.RoundToInt(this.m_pierce), Mathf.RoundToInt(this.m_pierce * num), Mathf.RoundToInt(this.m_pierce * num2)));
			}
			if (this.m_fire != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_fire: <color=orange>{0}</color> <color=yellow>({1}-{2}) </color>", Mathf.RoundToInt(this.m_fire), Mathf.RoundToInt(this.m_fire * num), Mathf.RoundToInt(this.m_fire * num2)));
			}
			if (this.m_frost != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_frost: <color=orange>{0}</color> <color=yellow>({1}-{2}) </color>", Mathf.RoundToInt(this.m_frost), Mathf.RoundToInt(this.m_frost * num), Mathf.RoundToInt(this.m_frost * num2)));
			}
			if (this.m_lightning != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_lightning: <color=orange>{0}</color> <color=yellow>({1}-{2}) </color>", Mathf.RoundToInt(this.m_lightning), Mathf.RoundToInt(this.m_lightning * num), Mathf.RoundToInt(this.m_lightning * num2)));
			}
			if (this.m_poison != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_poison: <color=orange>{0}</color> <color=yellow>({1}-{2}) </color>", Mathf.RoundToInt(this.m_poison), Mathf.RoundToInt(this.m_poison * num), Mathf.RoundToInt(this.m_poison * num2)));
			}
			if (this.m_spirit != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_spirit: <color=orange>{0}</color> <color=yellow>({1}-{2}) </color>", Mathf.RoundToInt(this.m_spirit), Mathf.RoundToInt(this.m_spirit * num), Mathf.RoundToInt(this.m_spirit * num2)));
			}
			return HitData.DamageTypes.m_sb.ToString();
		}

		// Token: 0x060022D0 RID: 8912 RVA: 0x000F01E4 File Offset: 0x000EE3E4
		public string GetTooltipString()
		{
			HitData.DamageTypes.m_sb.Clear();
			if (this.m_damage != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_damage: <color=yellow>{0}</color>", this.m_damage));
			}
			if (this.m_blunt != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_blunt: <color=yellow>{0}</color>", this.m_blunt));
			}
			if (this.m_slash != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_slash: <color=yellow>{0}</color>", this.m_slash));
			}
			if (this.m_pierce != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_pierce: <color=yellow>{0}</color>", this.m_pierce));
			}
			if (this.m_fire != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_fire: <color=yellow>{0}</color>", this.m_fire));
			}
			if (this.m_frost != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_frost: <color=yellow>{0}</color>", this.m_frost));
			}
			if (this.m_lightning != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_lightning: <color=yellow>{0}</color>", this.m_frost));
			}
			if (this.m_poison != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_poison: <color=yellow>{0}</color>", this.m_poison));
			}
			if (this.m_spirit != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("\n$inventory_spirit: <color=yellow>{0}</color>", this.m_spirit));
			}
			return HitData.DamageTypes.m_sb.ToString();
		}

		// Token: 0x060022D1 RID: 8913 RVA: 0x000F039C File Offset: 0x000EE59C
		public override string ToString()
		{
			HitData.DamageTypes.m_sb.Clear();
			if (this.m_damage != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Damage: {0} ", this.m_damage));
			}
			if (this.m_blunt != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Blunt: {0} ", this.m_blunt));
			}
			if (this.m_slash != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Slash: {0} ", this.m_slash));
			}
			if (this.m_pierce != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Pierce: {0} ", this.m_pierce));
			}
			if (this.m_fire != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Fire: {0} ", this.m_fire));
			}
			if (this.m_frost != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Frost: {0} ", this.m_frost));
			}
			if (this.m_lightning != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Lightning: {0} ", this.m_frost));
			}
			if (this.m_poison != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Poison: {0} ", this.m_poison));
			}
			if (this.m_spirit != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Spirit: {0} ", this.m_spirit));
			}
			if (this.m_chop != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Chop: {0} ", this.m_chop));
			}
			if (this.m_pickaxe != 0f)
			{
				HitData.DamageTypes.m_sb.Append(string.Format("Pickaxe: {0} ", this.m_pickaxe));
			}
			return HitData.DamageTypes.m_sb.ToString();
		}

		// Token: 0x060022D3 RID: 8915 RVA: 0x000F05B9 File Offset: 0x000EE7B9
		[CompilerGenerated]
		internal static void <IncreaseEqually>g__increase|21_0(ref float damage, ref HitData.DamageTypes.<>c__DisplayClass21_0 A_1)
		{
			damage += damage / A_1.total * A_1.totalDamageIncrease;
		}

		// Token: 0x040025FB RID: 9723
		public float m_damage;

		// Token: 0x040025FC RID: 9724
		public float m_blunt;

		// Token: 0x040025FD RID: 9725
		public float m_slash;

		// Token: 0x040025FE RID: 9726
		public float m_pierce;

		// Token: 0x040025FF RID: 9727
		public float m_chop;

		// Token: 0x04002600 RID: 9728
		public float m_pickaxe;

		// Token: 0x04002601 RID: 9729
		public float m_fire;

		// Token: 0x04002602 RID: 9730
		public float m_frost;

		// Token: 0x04002603 RID: 9731
		public float m_lightning;

		// Token: 0x04002604 RID: 9732
		public float m_poison;

		// Token: 0x04002605 RID: 9733
		public float m_spirit;

		// Token: 0x04002606 RID: 9734
		private static StringBuilder m_sb = new StringBuilder();
	}
}
