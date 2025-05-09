using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Token: 0x0200003F RID: 63
public class SE_Stats : StatusEffect
{
	// Token: 0x0600050F RID: 1295 RVA: 0x0002C4D8 File Offset: 0x0002A6D8
	public override void Setup(Character character)
	{
		base.Setup(character);
		if (this.m_healthOverTime > 0f && this.m_healthOverTimeInterval > 0f)
		{
			if (this.m_healthOverTimeDuration <= 0f)
			{
				this.m_healthOverTimeDuration = this.m_ttl;
			}
			this.m_healthOverTimeTicks = this.m_healthOverTimeDuration / this.m_healthOverTimeInterval;
			this.m_healthOverTimeTickHP = this.m_healthOverTime / this.m_healthOverTimeTicks;
		}
		if (this.m_staminaOverTime > 0f && this.m_staminaOverTimeDuration <= 0f)
		{
			this.m_staminaOverTimeDuration = this.m_ttl;
		}
		if (this.m_eitrOverTime > 0f && this.m_eitrOverTimeDuration <= 0f)
		{
			this.m_eitrOverTimeDuration = this.m_ttl;
		}
	}

	// Token: 0x06000510 RID: 1296 RVA: 0x0002C594 File Offset: 0x0002A794
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (this.m_tickInterval > 0f)
		{
			this.m_tickTimer += dt;
			if (this.m_tickTimer >= this.m_tickInterval)
			{
				this.m_tickTimer = 0f;
				if (this.m_character.GetHealthPercentage() >= this.m_healthPerTickMinHealthPercentage)
				{
					if (this.m_healthPerTick > 0f)
					{
						this.m_character.Heal(this.m_healthPerTick, true);
					}
					else
					{
						HitData hitData = new HitData();
						hitData.m_damage.m_damage = -this.m_healthPerTick;
						hitData.m_point = this.m_character.GetTopPoint();
						hitData.m_hitType = this.m_hitType;
						this.m_character.Damage(hitData);
					}
				}
			}
		}
		if (this.m_healthOverTimeTicks > 0f)
		{
			this.m_healthOverTimeTimer += dt;
			if (this.m_healthOverTimeTimer > this.m_healthOverTimeInterval)
			{
				this.m_healthOverTimeTimer = 0f;
				this.m_healthOverTimeTicks -= 1f;
				this.m_character.Heal(this.m_healthOverTimeTickHP, true);
			}
		}
		if (this.m_staminaOverTime != 0f && this.m_time <= this.m_staminaOverTimeDuration)
		{
			float num = this.m_staminaOverTimeDuration / dt;
			this.m_character.AddStamina(this.m_staminaOverTime / num);
		}
		if (this.m_eitrOverTime != 0f && this.m_time <= this.m_eitrOverTimeDuration)
		{
			float num2 = this.m_eitrOverTimeDuration / dt;
			this.m_character.AddEitr(this.m_eitrOverTime / num2);
		}
		if (this.m_staminaDrainPerSec > 0f)
		{
			this.m_character.UseStamina(this.m_staminaDrainPerSec * dt);
		}
	}

	// Token: 0x06000511 RID: 1297 RVA: 0x0002C73D File Offset: 0x0002A93D
	public override void ModifyHealthRegen(ref float regenMultiplier)
	{
		if (this.m_healthRegenMultiplier > 1f)
		{
			regenMultiplier += this.m_healthRegenMultiplier - 1f;
			return;
		}
		regenMultiplier *= this.m_healthRegenMultiplier;
	}

	// Token: 0x06000512 RID: 1298 RVA: 0x0002C769 File Offset: 0x0002A969
	public override void ModifyStaminaRegen(ref float staminaRegen)
	{
		if (this.m_staminaRegenMultiplier > 1f)
		{
			staminaRegen += this.m_staminaRegenMultiplier - 1f;
			return;
		}
		staminaRegen *= this.m_staminaRegenMultiplier;
	}

	// Token: 0x06000513 RID: 1299 RVA: 0x0002C795 File Offset: 0x0002A995
	public override void ModifyEitrRegen(ref float staminaRegen)
	{
		if (this.m_eitrRegenMultiplier > 1f)
		{
			staminaRegen += this.m_eitrRegenMultiplier - 1f;
			return;
		}
		staminaRegen *= this.m_eitrRegenMultiplier;
	}

	// Token: 0x06000514 RID: 1300 RVA: 0x0002C7C1 File Offset: 0x0002A9C1
	public override void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
	{
		modifiers.Apply(this.m_mods);
	}

	// Token: 0x06000515 RID: 1301 RVA: 0x0002C7CF File Offset: 0x0002A9CF
	public override void ModifyRaiseSkill(Skills.SkillType skill, ref float value)
	{
		if (this.m_raiseSkill == Skills.SkillType.None)
		{
			return;
		}
		if (this.m_raiseSkill == Skills.SkillType.All || this.m_raiseSkill == skill)
		{
			value += this.m_raiseSkillModifier;
		}
	}

	// Token: 0x06000516 RID: 1302 RVA: 0x0002C7FC File Offset: 0x0002A9FC
	public override void ModifySkillLevel(Skills.SkillType skill, ref float value)
	{
		if (this.m_skillLevel == Skills.SkillType.None)
		{
			return;
		}
		if (this.m_skillLevel == Skills.SkillType.All || this.m_skillLevel == skill)
		{
			value += this.m_skillLevelModifier;
		}
		if (this.m_skillLevel2 == Skills.SkillType.All || this.m_skillLevel2 == skill)
		{
			value += this.m_skillLevelModifier2;
		}
	}

	// Token: 0x06000517 RID: 1303 RVA: 0x0002C854 File Offset: 0x0002AA54
	public override void ModifyNoise(float baseNoise, ref float noise)
	{
		noise += baseNoise * this.m_noiseModifier;
	}

	// Token: 0x06000518 RID: 1304 RVA: 0x0002C863 File Offset: 0x0002AA63
	public override void ModifyStealth(float baseStealth, ref float stealth)
	{
		stealth += baseStealth * this.m_stealthModifier;
	}

	// Token: 0x06000519 RID: 1305 RVA: 0x0002C872 File Offset: 0x0002AA72
	public override void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
		limit += this.m_addMaxCarryWeight;
		if (limit < 0f)
		{
			limit = 0f;
		}
	}

	// Token: 0x0600051A RID: 1306 RVA: 0x0002C88F File Offset: 0x0002AA8F
	public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		if (skill == this.m_modifyAttackSkill || this.m_modifyAttackSkill == Skills.SkillType.All)
		{
			hitData.m_damage.Modify(this.m_damageModifier);
		}
		hitData.m_damage.Modify(this.m_percentigeDamageModifiers);
	}

	// Token: 0x0600051B RID: 1307 RVA: 0x0002C8CC File Offset: 0x0002AACC
	public override void ModifyRunStaminaDrain(float baseDrain, ref float drain, Vector3 dir)
	{
		drain += baseDrain * this.m_runStaminaDrainModifier;
		if (this.m_windRunStaminaModifier != 0f)
		{
			dir.Normalize();
			float num = (Vector3.Dot(dir, EnvMan.instance.GetWindDir()) + 1f) / 2f;
			num *= EnvMan.instance.GetWindIntensity();
			num *= this.m_windRunStaminaModifier;
			drain *= 1f + num;
		}
	}

	// Token: 0x0600051C RID: 1308 RVA: 0x0002C939 File Offset: 0x0002AB39
	public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * this.m_jumpStaminaUseModifier;
	}

	// Token: 0x0600051D RID: 1309 RVA: 0x0002C948 File Offset: 0x0002AB48
	public override void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * this.m_attackStaminaUseModifier;
	}

	// Token: 0x0600051E RID: 1310 RVA: 0x0002C957 File Offset: 0x0002AB57
	public override void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * this.m_blockStaminaUseModifier;
	}

	// Token: 0x0600051F RID: 1311 RVA: 0x0002C966 File Offset: 0x0002AB66
	public override void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * this.m_dodgeStaminaUseModifier;
	}

	// Token: 0x06000520 RID: 1312 RVA: 0x0002C975 File Offset: 0x0002AB75
	public override void ModifySwimStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * this.m_swimStaminaUseModifier;
	}

	// Token: 0x06000521 RID: 1313 RVA: 0x0002C984 File Offset: 0x0002AB84
	public override void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * this.m_homeItemStaminaUseModifier;
	}

	// Token: 0x06000522 RID: 1314 RVA: 0x0002C993 File Offset: 0x0002AB93
	public override void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * this.m_sneakStaminaUseModifier;
	}

	// Token: 0x06000523 RID: 1315 RVA: 0x0002C9A4 File Offset: 0x0002ABA4
	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
	{
		if (this.m_character.IsSwimming())
		{
			speed += baseSpeed * this.m_speedModifier * 0.5f;
		}
		else
		{
			speed += baseSpeed * this.m_speedModifier;
		}
		if (this.m_windMovementModifier != 0f)
		{
			dir.Normalize();
			float num = (Vector3.Dot(dir, EnvMan.instance.GetWindDir()) + 1f) / 2f;
			num *= EnvMan.instance.GetWindIntensity();
			num *= this.m_windMovementModifier;
			speed *= 1f + num;
		}
		if (speed < 0f)
		{
			speed = 0f;
		}
	}

	// Token: 0x06000524 RID: 1316 RVA: 0x0002CA44 File Offset: 0x0002AC44
	public override void ModifyJump(Vector3 baseJump, ref Vector3 jump)
	{
		jump += new Vector3(baseJump.x * this.m_jumpModifier.x, baseJump.y * this.m_jumpModifier.y, baseJump.z * this.m_jumpModifier.z);
	}

	// Token: 0x06000525 RID: 1317 RVA: 0x0002CA9D File Offset: 0x0002AC9D
	public override void ModifyWalkVelocity(ref Vector3 vel)
	{
		if (this.m_maxMaxFallSpeed > 0f && vel.y < -this.m_maxMaxFallSpeed)
		{
			vel.y = -this.m_maxMaxFallSpeed;
		}
	}

	// Token: 0x06000526 RID: 1318 RVA: 0x0002CAC8 File Offset: 0x0002ACC8
	public override void ModifyFallDamage(float baseDamage, ref float damage)
	{
		damage += baseDamage * this.m_fallDamageModifier;
		if (damage < 0f)
		{
			damage = 0f;
		}
	}

	// Token: 0x06000527 RID: 1319 RVA: 0x0002CAE8 File Offset: 0x0002ACE8
	public override string GetTooltipString()
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		if (this.m_tooltip.Length > 0)
		{
			stringBuilder.AppendFormat("{0}\n", this.m_tooltip);
		}
		if (this.m_runStaminaDrainModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_runstamina: <color=orange>{0}%</color>\n", (this.m_runStaminaDrainModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_healthOverTime != 0f)
		{
			stringBuilder.AppendFormat("$se_health: <color=orange>{0}</color>\n", this.m_healthOverTime.ToString());
		}
		if (this.m_staminaOverTime != 0f)
		{
			stringBuilder.AppendFormat("$se_stamina: <color=orange>{0}</color>\n", this.m_staminaOverTime.ToString());
		}
		if (this.m_eitrOverTime != 0f)
		{
			stringBuilder.AppendFormat("$se_eitr: <color=orange>{0}</color>\n", this.m_eitrOverTime.ToString());
		}
		if (this.m_healthRegenMultiplier != 1f)
		{
			stringBuilder.AppendFormat("$se_healthregen: <color=orange>{0}%</color>\n", ((this.m_healthRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
		}
		if (this.m_staminaRegenMultiplier != 1f)
		{
			stringBuilder.AppendFormat("$se_staminaregen: <color=orange>{0}%</color>\n", ((this.m_staminaRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
		}
		if (this.m_eitrRegenMultiplier != 1f)
		{
			stringBuilder.AppendFormat("$se_eitrregen: <color=orange>{0}%</color>\n", ((this.m_eitrRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
		}
		if (this.m_addMaxCarryWeight != 0f)
		{
			stringBuilder.AppendFormat("$se_max_carryweight: <color=orange>{0}</color>\n", this.m_addMaxCarryWeight.ToString("+0;-0"));
		}
		if (this.m_mods.Count > 0)
		{
			stringBuilder.Append(SE_Stats.GetDamageModifiersTooltipString(this.m_mods));
			stringBuilder.Append("\n");
		}
		if (this.m_noiseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_noisemod: <color=orange>{0}%</color>\n", (this.m_noiseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_stealthModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_sneakmod: <color=orange>{0}%</color>\n", (this.m_stealthModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_speedModifier != 0f && this.m_speedModifier > -100f)
		{
			stringBuilder.AppendFormat("$item_movement_modifier: <color=orange>{0}%</color>\n", (this.m_speedModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_maxMaxFallSpeed != 0f)
		{
			stringBuilder.AppendFormat("$item_limitfallspeed: <color=orange>{0}m/s</color>\n", this.m_maxMaxFallSpeed.ToString("0"));
		}
		if (this.m_fallDamageModifier != 0f)
		{
			stringBuilder.AppendFormat("$item_falldamage: <color=orange>{0}%</color>\n", (this.m_fallDamageModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_jumpModifier.y != 0f && this.m_jumpModifier.y != 1f && this.m_jumpModifier.y > -1f && this.m_jumpModifier.x > -1f)
		{
			stringBuilder.AppendFormat("$se_jumpheight: <color=orange>{0}%</color>\n", (this.m_jumpModifier.y * 100f).ToString("+0;-0"));
		}
		if ((this.m_jumpModifier.x != 0f || this.m_jumpModifier.z != 0f) && this.m_jumpModifier.y > -1f && this.m_jumpModifier.x > -1f)
		{
			stringBuilder.AppendFormat("$se_jumplength: <color=orange>{0}%</color>\n", (Mathf.Max(this.m_jumpModifier.x, this.m_jumpModifier.z) * 100f).ToString("+0;-0"));
		}
		if (this.m_jumpStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_jumpstamina: <color=orange>{0}%</color>\n", (this.m_jumpStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_attackStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_attackstamina: <color=orange>{0}%</color>\n", (this.m_attackStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_blockStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_blockstamina: <color=orange>{0}%</color>\n", (this.m_blockStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_dodgeStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_dodgestamina: <color=orange>{0}%</color>\n", (this.m_dodgeStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_swimStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_swimstamina: <color=orange>{0}%</color>\n", (this.m_swimStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_homeItemStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$base_item_modifier: <color=orange>{0}%</color>\n", (this.m_homeItemStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_sneakStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_sneakstamina: <color=orange>{0}%</color>\n", (this.m_sneakStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_runStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_runstamina: <color=orange>{0}%</color>\n", (this.m_runStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_skillLevel != Skills.SkillType.None)
		{
			stringBuilder.AppendFormat("{0} <color=orange>{1}</color>\n", Localization.instance.Localize("$skill_" + this.m_skillLevel.ToString().ToLower()), this.m_skillLevelModifier.ToString("+0;-0"));
		}
		if (this.m_skillLevel2 != Skills.SkillType.None)
		{
			stringBuilder.AppendFormat("{0} <color=orange>{1}</color>\n", Localization.instance.Localize("$skill_" + this.m_skillLevel2.ToString().ToLower()), this.m_skillLevelModifier2.ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_blunt != 0f)
		{
			stringBuilder.AppendFormat("$inventory_blunt: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_blunt * 100f).ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_slash != 0f)
		{
			stringBuilder.AppendFormat("$inventory_slash: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_slash * 100f).ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_pierce != 0f)
		{
			stringBuilder.AppendFormat("$inventory_pierce: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_pierce * 100f).ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_chop != 0f)
		{
			stringBuilder.AppendFormat("$inventory_chop: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_chop * 100f).ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_pickaxe != 0f)
		{
			stringBuilder.AppendFormat("$inventory_pickaxe: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_pickaxe * 100f).ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_fire != 0f)
		{
			stringBuilder.AppendFormat("$inventory_fire: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_fire * 100f).ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_frost != 0f)
		{
			stringBuilder.AppendFormat("$inventory_frost: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_frost * 100f).ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_lightning != 0f)
		{
			stringBuilder.AppendFormat("$inventory_lightning: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_lightning * 100f).ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_poison != 0f)
		{
			stringBuilder.AppendFormat("$inventory_poison: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_poison * 100f).ToString("+0;-0"));
		}
		if (this.m_percentigeDamageModifiers.m_spirit != 0f)
		{
			stringBuilder.AppendFormat("$inventory_spirit: <color=orange>{0}%</color>\n", (this.m_percentigeDamageModifiers.m_spirit * 100f).ToString("+0;-0"));
		}
		return stringBuilder.ToString();
	}

	// Token: 0x06000528 RID: 1320 RVA: 0x0002D338 File Offset: 0x0002B538
	public static string GetDamageModifiersTooltipString(List<HitData.DamageModPair> mods)
	{
		if (mods.Count == 0)
		{
			return "";
		}
		string text = "";
		foreach (HitData.DamageModPair damageModPair in mods)
		{
			if (damageModPair.m_modifier != HitData.DamageModifier.Ignore && damageModPair.m_modifier != HitData.DamageModifier.Normal)
			{
				switch (damageModPair.m_modifier)
				{
				case HitData.DamageModifier.Resistant:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_resistant</color> VS ";
					break;
				case HitData.DamageModifier.Weak:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_weak</color> VS ";
					break;
				case HitData.DamageModifier.Immune:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_immune</color> VS ";
					break;
				case HitData.DamageModifier.VeryResistant:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_veryresistant</color> VS ";
					break;
				case HitData.DamageModifier.VeryWeak:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_veryweak</color> VS ";
					break;
				}
				text += "<color=orange>";
				HitData.DamageType type = damageModPair.m_type;
				if (type <= HitData.DamageType.Fire)
				{
					if (type <= HitData.DamageType.Chop)
					{
						switch (type)
						{
						case HitData.DamageType.Blunt:
							text += "$inventory_blunt";
							break;
						case HitData.DamageType.Slash:
							text += "$inventory_slash";
							break;
						case HitData.DamageType.Blunt | HitData.DamageType.Slash:
							break;
						case HitData.DamageType.Pierce:
							text += "$inventory_pierce";
							break;
						default:
							if (type == HitData.DamageType.Chop)
							{
								text += "$inventory_chop";
							}
							break;
						}
					}
					else if (type != HitData.DamageType.Pickaxe)
					{
						if (type == HitData.DamageType.Fire)
						{
							text += "$inventory_fire";
						}
					}
					else
					{
						text += "$inventory_pickaxe";
					}
				}
				else if (type <= HitData.DamageType.Lightning)
				{
					if (type != HitData.DamageType.Frost)
					{
						if (type == HitData.DamageType.Lightning)
						{
							text += "$inventory_lightning";
						}
					}
					else
					{
						text += "$inventory_frost";
					}
				}
				else if (type != HitData.DamageType.Poison)
				{
					if (type == HitData.DamageType.Spirit)
					{
						text += "$inventory_spirit";
					}
				}
				else
				{
					text += "$inventory_poison";
				}
				text += "</color>";
			}
		}
		return text;
	}

	// Token: 0x040005AC RID: 1452
	[Header("__SE_Stats__")]
	[Header("HP per tick")]
	public float m_tickInterval;

	// Token: 0x040005AD RID: 1453
	public float m_healthPerTickMinHealthPercentage;

	// Token: 0x040005AE RID: 1454
	public float m_healthPerTick;

	// Token: 0x040005AF RID: 1455
	public HitData.HitType m_hitType;

	// Token: 0x040005B0 RID: 1456
	[Header("Health over time")]
	public float m_healthOverTime;

	// Token: 0x040005B1 RID: 1457
	public float m_healthOverTimeDuration;

	// Token: 0x040005B2 RID: 1458
	public float m_healthOverTimeInterval = 5f;

	// Token: 0x040005B3 RID: 1459
	[Header("Stamina")]
	public float m_staminaOverTime;

	// Token: 0x040005B4 RID: 1460
	public float m_staminaOverTimeDuration;

	// Token: 0x040005B5 RID: 1461
	public float m_staminaDrainPerSec;

	// Token: 0x040005B6 RID: 1462
	public float m_runStaminaDrainModifier;

	// Token: 0x040005B7 RID: 1463
	public float m_jumpStaminaUseModifier;

	// Token: 0x040005B8 RID: 1464
	public float m_attackStaminaUseModifier;

	// Token: 0x040005B9 RID: 1465
	public float m_blockStaminaUseModifier;

	// Token: 0x040005BA RID: 1466
	public float m_dodgeStaminaUseModifier;

	// Token: 0x040005BB RID: 1467
	public float m_swimStaminaUseModifier;

	// Token: 0x040005BC RID: 1468
	public float m_homeItemStaminaUseModifier;

	// Token: 0x040005BD RID: 1469
	public float m_sneakStaminaUseModifier;

	// Token: 0x040005BE RID: 1470
	public float m_runStaminaUseModifier;

	// Token: 0x040005BF RID: 1471
	[Header("Eitr")]
	public float m_eitrOverTime;

	// Token: 0x040005C0 RID: 1472
	public float m_eitrOverTimeDuration;

	// Token: 0x040005C1 RID: 1473
	[Header("Regen modifiers")]
	public float m_healthRegenMultiplier = 1f;

	// Token: 0x040005C2 RID: 1474
	public float m_staminaRegenMultiplier = 1f;

	// Token: 0x040005C3 RID: 1475
	public float m_eitrRegenMultiplier = 1f;

	// Token: 0x040005C4 RID: 1476
	[Header("Modify raise skill")]
	public Skills.SkillType m_raiseSkill;

	// Token: 0x040005C5 RID: 1477
	public float m_raiseSkillModifier;

	// Token: 0x040005C6 RID: 1478
	[Header("Modify skill level")]
	public Skills.SkillType m_skillLevel;

	// Token: 0x040005C7 RID: 1479
	public float m_skillLevelModifier;

	// Token: 0x040005C8 RID: 1480
	public Skills.SkillType m_skillLevel2;

	// Token: 0x040005C9 RID: 1481
	public float m_skillLevelModifier2;

	// Token: 0x040005CA RID: 1482
	[Header("Hit modifier")]
	public List<HitData.DamageModPair> m_mods = new List<HitData.DamageModPair>();

	// Token: 0x040005CB RID: 1483
	[Header("Attack")]
	public Skills.SkillType m_modifyAttackSkill;

	// Token: 0x040005CC RID: 1484
	public float m_damageModifier = 1f;

	// Token: 0x040005CD RID: 1485
	public HitData.DamageTypes m_percentigeDamageModifiers;

	// Token: 0x040005CE RID: 1486
	[Header("Sneak")]
	public float m_noiseModifier;

	// Token: 0x040005CF RID: 1487
	public float m_stealthModifier;

	// Token: 0x040005D0 RID: 1488
	[Header("Carry weight")]
	public float m_addMaxCarryWeight;

	// Token: 0x040005D1 RID: 1489
	[Header("Speed")]
	public float m_speedModifier;

	// Token: 0x040005D2 RID: 1490
	public Vector3 m_jumpModifier;

	// Token: 0x040005D3 RID: 1491
	[Header("Fall")]
	public float m_maxMaxFallSpeed;

	// Token: 0x040005D4 RID: 1492
	public float m_fallDamageModifier;

	// Token: 0x040005D5 RID: 1493
	[Header("Wind")]
	public float m_windMovementModifier;

	// Token: 0x040005D6 RID: 1494
	public float m_windRunStaminaModifier;

	// Token: 0x040005D7 RID: 1495
	[Header("Pheromones")]
	public GameObject m_pheromoneTarget;

	// Token: 0x040005D8 RID: 1496
	public float m_pheromoneSpawnChanceOverride;

	// Token: 0x040005D9 RID: 1497
	public int m_pheromoneSpawnMinLevel;

	// Token: 0x040005DA RID: 1498
	public float m_pheromoneLevelUpMultiplier = 1f;

	// Token: 0x040005DB RID: 1499
	public int m_pheromoneMaxInstanceOverride;

	// Token: 0x040005DC RID: 1500
	public bool m_pheromoneFlee;

	// Token: 0x040005DD RID: 1501
	private float m_tickTimer;

	// Token: 0x040005DE RID: 1502
	private float m_healthOverTimeTimer;

	// Token: 0x040005DF RID: 1503
	private float m_healthOverTimeTicks;

	// Token: 0x040005E0 RID: 1504
	private float m_healthOverTimeTickHP;
}
