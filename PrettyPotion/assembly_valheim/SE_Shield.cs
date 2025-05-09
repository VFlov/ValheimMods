using System;
using UnityEngine;

// Token: 0x0200003C RID: 60
public class SE_Shield : StatusEffect
{
	// Token: 0x06000504 RID: 1284 RVA: 0x0002C0DB File Offset: 0x0002A2DB
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	// Token: 0x06000505 RID: 1285 RVA: 0x0002C0E4 File Offset: 0x0002A2E4
	public override bool IsDone()
	{
		if (this.m_damage > this.m_totalAbsorbDamage)
		{
			this.m_breakEffects.Create(this.m_character.GetCenterPoint(), this.m_character.transform.rotation, this.m_character.transform, this.m_character.GetRadius() * 2f, -1);
			if (this.m_levelUpSkillOnBreak != Skills.SkillType.None)
			{
				Skills skills = this.m_character.GetSkills();
				if (skills != null && skills)
				{
					skills.RaiseSkill(this.m_levelUpSkillOnBreak, this.m_levelUpSkillFactor);
					Terminal.Log(string.Format("{0} is leveling up {1} at factor {2}", this.m_name, this.m_levelUpSkillOnBreak, this.m_levelUpSkillFactor));
				}
			}
			return true;
		}
		return base.IsDone();
	}

	// Token: 0x06000506 RID: 1286 RVA: 0x0002C1AC File Offset: 0x0002A3AC
	public override void OnDamaged(HitData hit, Character attacker)
	{
		float totalDamage = hit.GetTotalDamage();
		this.m_damage += totalDamage;
		hit.ApplyModifier(0f);
		this.m_hitEffects.Create(hit.m_point, Quaternion.LookRotation(-hit.m_dir), this.m_character.transform, 1f, -1);
	}

	// Token: 0x06000507 RID: 1287 RVA: 0x0002C20C File Offset: 0x0002A40C
	public override void SetLevel(int itemLevel, float skillLevel)
	{
		if (this.m_ttlPerItemLevel > 0)
		{
			this.m_ttl = (float)(this.m_ttlPerItemLevel * itemLevel);
		}
		this.m_totalAbsorbDamage = this.m_absorbDamage + this.m_absorbDamagePerSkillLevel * skillLevel;
		if (Game.m_worldLevel > 0)
		{
			this.m_totalAbsorbDamage += this.m_absorbDamageWorldLevel * (float)Game.m_worldLevel;
		}
		Terminal.Log(string.Format("Shield setting itemlevel: {0} = ttl: {1}, skilllevel: {2} = absorb: {3}", new object[]
		{
			itemLevel,
			this.m_ttl,
			skillLevel,
			this.m_totalAbsorbDamage
		}));
		base.SetLevel(itemLevel, skillLevel);
	}

	// Token: 0x06000508 RID: 1288 RVA: 0x0002C2B4 File Offset: 0x0002A4B4
	public override string GetTooltipString()
	{
		return string.Concat(new string[]
		{
			base.GetTooltipString(),
			"\n$se_shield_ttl <color=orange>",
			this.m_ttl.ToString("0"),
			"</color>\n$se_shield_damage <color=orange>",
			this.m_totalAbsorbDamage.ToString("0"),
			"</color>"
		});
	}

	// Token: 0x0400059A RID: 1434
	[Header("__SE_Shield__")]
	public float m_absorbDamage = 100f;

	// Token: 0x0400059B RID: 1435
	public float m_absorbDamageWorldLevel = 100f;

	// Token: 0x0400059C RID: 1436
	public Skills.SkillType m_levelUpSkillOnBreak;

	// Token: 0x0400059D RID: 1437
	public float m_levelUpSkillFactor = 1f;

	// Token: 0x0400059E RID: 1438
	public int m_ttlPerItemLevel;

	// Token: 0x0400059F RID: 1439
	public float m_absorbDamagePerSkillLevel;

	// Token: 0x040005A0 RID: 1440
	public EffectList m_breakEffects = new EffectList();

	// Token: 0x040005A1 RID: 1441
	public EffectList m_hitEffects = new EffectList();

	// Token: 0x040005A2 RID: 1442
	private float m_totalAbsorbDamage;

	// Token: 0x040005A3 RID: 1443
	private float m_damage;
}
