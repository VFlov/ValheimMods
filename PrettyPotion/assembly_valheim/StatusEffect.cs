using System;
using UnityEngine;

// Token: 0x02000041 RID: 65
public class StatusEffect : ScriptableObject
{
	// Token: 0x0600052D RID: 1325 RVA: 0x0002D6B9 File Offset: 0x0002B8B9
	public StatusEffect Clone()
	{
		return base.MemberwiseClone() as StatusEffect;
	}

	// Token: 0x0600052E RID: 1326 RVA: 0x0002D6C6 File Offset: 0x0002B8C6
	public virtual bool CanAdd(Character character)
	{
		return true;
	}

	// Token: 0x0600052F RID: 1327 RVA: 0x0002D6C9 File Offset: 0x0002B8C9
	public virtual void Setup(Character character)
	{
		this.m_character = character;
		if (!string.IsNullOrEmpty(this.m_startMessage))
		{
			this.m_character.Message(this.m_startMessageType, this.m_startMessage, 0, null);
		}
		this.TriggerStartEffects();
	}

	// Token: 0x06000530 RID: 1328 RVA: 0x0002D6FE File Offset: 0x0002B8FE
	public virtual void SetAttacker(Character attacker)
	{
	}

	// Token: 0x06000531 RID: 1329 RVA: 0x0002D700 File Offset: 0x0002B900
	public virtual string GetTooltipString()
	{
		return this.m_tooltip;
	}

	// Token: 0x06000532 RID: 1330 RVA: 0x0002D708 File Offset: 0x0002B908
	protected virtual void OnApplicationQuit()
	{
		this.m_startEffectInstances = null;
	}

	// Token: 0x06000533 RID: 1331 RVA: 0x0002D711 File Offset: 0x0002B911
	public virtual void OnDestroy()
	{
		this.RemoveStartEffects();
	}

	// Token: 0x06000534 RID: 1332 RVA: 0x0002D71C File Offset: 0x0002B91C
	protected void TriggerStartEffects()
	{
		this.RemoveStartEffects();
		float radius = this.m_character.GetRadius();
		int variant = -1;
		Player player = this.m_character as Player;
		if (player)
		{
			variant = player.GetPlayerModel();
		}
		this.m_startEffectInstances = this.m_startEffects.Create(this.m_character.GetCenterPoint(), this.m_character.transform.rotation, this.m_character.transform, radius * 2f, variant);
	}

	// Token: 0x06000535 RID: 1333 RVA: 0x0002D798 File Offset: 0x0002B998
	private void RemoveStartEffects()
	{
		if (this.m_startEffectInstances != null && ZNetScene.instance != null)
		{
			foreach (GameObject gameObject in this.m_startEffectInstances)
			{
				if (gameObject)
				{
					ZNetView component = gameObject.GetComponent<ZNetView>();
					if (component.IsValid())
					{
						component.ClaimOwnership();
						component.Destroy();
					}
				}
			}
			this.m_startEffectInstances = null;
		}
	}

	// Token: 0x06000536 RID: 1334 RVA: 0x0002D800 File Offset: 0x0002BA00
	public virtual void Stop()
	{
		this.RemoveStartEffects();
		this.m_stopEffects.Create(this.m_character.transform.position, this.m_character.transform.rotation, null, 1f, -1);
		if (!string.IsNullOrEmpty(this.m_stopMessage))
		{
			this.m_character.Message(this.m_stopMessageType, this.m_stopMessage, 0, null);
		}
	}

	// Token: 0x06000537 RID: 1335 RVA: 0x0002D86C File Offset: 0x0002BA6C
	public virtual void UpdateStatusEffect(float dt)
	{
		this.m_time += dt;
		if (this.m_repeatInterval > 0f && !string.IsNullOrEmpty(this.m_repeatMessage))
		{
			this.m_msgTimer += dt;
			if (this.m_msgTimer > this.m_repeatInterval)
			{
				this.m_msgTimer = 0f;
				this.m_character.Message(this.m_repeatMessageType, this.m_repeatMessage, 0, null);
			}
		}
	}

	// Token: 0x06000538 RID: 1336 RVA: 0x0002D8E1 File Offset: 0x0002BAE1
	public virtual bool IsDone()
	{
		return this.m_ttl > 0f && this.m_time > this.m_ttl;
	}

	// Token: 0x06000539 RID: 1337 RVA: 0x0002D901 File Offset: 0x0002BB01
	public virtual void ResetTime()
	{
		this.m_time = 0f;
	}

	// Token: 0x0600053A RID: 1338 RVA: 0x0002D90E File Offset: 0x0002BB0E
	public virtual void SetLevel(int itemLevel, float skillLevel)
	{
	}

	// Token: 0x0600053B RID: 1339 RVA: 0x0002D910 File Offset: 0x0002BB10
	public float GetDuration()
	{
		return this.m_time;
	}

	// Token: 0x0600053C RID: 1340 RVA: 0x0002D918 File Offset: 0x0002BB18
	public float GetRemaningTime()
	{
		return this.m_ttl - this.m_time;
	}

	// Token: 0x0600053D RID: 1341 RVA: 0x0002D927 File Offset: 0x0002BB27
	public virtual string GetIconText()
	{
		if (this.m_ttl > 0f)
		{
			return StatusEffect.GetTimeString(this.m_ttl - this.GetDuration(), false, false);
		}
		return "";
	}

	// Token: 0x0600053E RID: 1342 RVA: 0x0002D950 File Offset: 0x0002BB50
	public static string GetTimeString(float time, bool sufix = false, bool alwaysShowMinutes = false)
	{
		if (time <= 0f)
		{
			return "";
		}
		int num = Mathf.CeilToInt(time);
		int num2 = (int)((float)num / 60f);
		int num3 = Mathf.Max(0, num - num2 * 60);
		if (sufix)
		{
			if (num2 > 0 || alwaysShowMinutes)
			{
				return num2.ToString() + "m:" + num3.ToString("00") + "s";
			}
			return num3.ToString() + "s";
		}
		else
		{
			if (num2 > 0 || alwaysShowMinutes)
			{
				return num2.ToString() + ":" + num3.ToString("00");
			}
			return num3.ToString();
		}
	}

	// Token: 0x0600053F RID: 1343 RVA: 0x0002D9F9 File Offset: 0x0002BBF9
	public virtual void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
	}

	// Token: 0x06000540 RID: 1344 RVA: 0x0002D9FB File Offset: 0x0002BBFB
	public virtual void ModifyHealthRegen(ref float regenMultiplier)
	{
	}

	// Token: 0x06000541 RID: 1345 RVA: 0x0002D9FD File Offset: 0x0002BBFD
	public virtual void ModifyStaminaRegen(ref float staminaRegen)
	{
	}

	// Token: 0x06000542 RID: 1346 RVA: 0x0002D9FF File Offset: 0x0002BBFF
	public virtual void ModifyEitrRegen(ref float eitrRegen)
	{
	}

	// Token: 0x06000543 RID: 1347 RVA: 0x0002DA01 File Offset: 0x0002BC01
	public virtual void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
	{
	}

	// Token: 0x06000544 RID: 1348 RVA: 0x0002DA03 File Offset: 0x0002BC03
	public virtual void ModifyRaiseSkill(Skills.SkillType skill, ref float value)
	{
	}

	// Token: 0x06000545 RID: 1349 RVA: 0x0002DA05 File Offset: 0x0002BC05
	public virtual void ModifySkillLevel(Skills.SkillType skill, ref float level)
	{
	}

	// Token: 0x06000546 RID: 1350 RVA: 0x0002DA07 File Offset: 0x0002BC07
	public virtual void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
	{
	}

	// Token: 0x06000547 RID: 1351 RVA: 0x0002DA09 File Offset: 0x0002BC09
	public virtual void ModifyJump(Vector3 baseJump, ref Vector3 jump)
	{
	}

	// Token: 0x06000548 RID: 1352 RVA: 0x0002DA0B File Offset: 0x0002BC0B
	public virtual void ModifyWalkVelocity(ref Vector3 vel)
	{
	}

	// Token: 0x06000549 RID: 1353 RVA: 0x0002DA0D File Offset: 0x0002BC0D
	public virtual void ModifyFallDamage(float baseDamage, ref float damage)
	{
	}

	// Token: 0x0600054A RID: 1354 RVA: 0x0002DA0F File Offset: 0x0002BC0F
	public virtual void ModifyNoise(float baseNoise, ref float noise)
	{
	}

	// Token: 0x0600054B RID: 1355 RVA: 0x0002DA11 File Offset: 0x0002BC11
	public virtual void ModifyStealth(float baseStealth, ref float stealth)
	{
	}

	// Token: 0x0600054C RID: 1356 RVA: 0x0002DA13 File Offset: 0x0002BC13
	public virtual void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
	}

	// Token: 0x0600054D RID: 1357 RVA: 0x0002DA15 File Offset: 0x0002BC15
	public virtual void ModifyRunStaminaDrain(float baseDrain, ref float drain, Vector3 dir)
	{
	}

	// Token: 0x0600054E RID: 1358 RVA: 0x0002DA17 File Offset: 0x0002BC17
	public virtual void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	// Token: 0x0600054F RID: 1359 RVA: 0x0002DA19 File Offset: 0x0002BC19
	public virtual void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	// Token: 0x06000550 RID: 1360 RVA: 0x0002DA1B File Offset: 0x0002BC1B
	public virtual void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	// Token: 0x06000551 RID: 1361 RVA: 0x0002DA1D File Offset: 0x0002BC1D
	public virtual void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	// Token: 0x06000552 RID: 1362 RVA: 0x0002DA1F File Offset: 0x0002BC1F
	public virtual void ModifySwimStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	// Token: 0x06000553 RID: 1363 RVA: 0x0002DA21 File Offset: 0x0002BC21
	public virtual void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	// Token: 0x06000554 RID: 1364 RVA: 0x0002DA23 File Offset: 0x0002BC23
	public virtual void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	// Token: 0x06000555 RID: 1365 RVA: 0x0002DA25 File Offset: 0x0002BC25
	public virtual void OnDamaged(HitData hit, Character attacker)
	{
	}

	// Token: 0x06000556 RID: 1366 RVA: 0x0002DA27 File Offset: 0x0002BC27
	public bool HaveAttribute(StatusEffect.StatusAttribute value)
	{
		return (this.m_attributes & value) > StatusEffect.StatusAttribute.None;
	}

	// Token: 0x06000557 RID: 1367 RVA: 0x0002DA34 File Offset: 0x0002BC34
	public int NameHash()
	{
		if (this.m_nameHash == 0)
		{
			this.m_nameHash = base.name.GetStableHashCode();
		}
		return this.m_nameHash;
	}

	// Token: 0x040005E4 RID: 1508
	[Header("__Common__")]
	public string m_name = "";

	// Token: 0x040005E5 RID: 1509
	public string m_category = "";

	// Token: 0x040005E6 RID: 1510
	public Sprite m_icon;

	// Token: 0x040005E7 RID: 1511
	public bool m_flashIcon;

	// Token: 0x040005E8 RID: 1512
	public bool m_cooldownIcon;

	// Token: 0x040005E9 RID: 1513
	[TextArea]
	public string m_tooltip = "";

	// Token: 0x040005EA RID: 1514
	[BitMask(typeof(StatusEffect.StatusAttribute))]
	public StatusEffect.StatusAttribute m_attributes;

	// Token: 0x040005EB RID: 1515
	public MessageHud.MessageType m_startMessageType = MessageHud.MessageType.TopLeft;

	// Token: 0x040005EC RID: 1516
	public string m_startMessage = "";

	// Token: 0x040005ED RID: 1517
	public MessageHud.MessageType m_stopMessageType = MessageHud.MessageType.TopLeft;

	// Token: 0x040005EE RID: 1518
	public string m_stopMessage = "";

	// Token: 0x040005EF RID: 1519
	public MessageHud.MessageType m_repeatMessageType = MessageHud.MessageType.TopLeft;

	// Token: 0x040005F0 RID: 1520
	public string m_repeatMessage = "";

	// Token: 0x040005F1 RID: 1521
	public float m_repeatInterval;

	// Token: 0x040005F2 RID: 1522
	public float m_ttl;

	// Token: 0x040005F3 RID: 1523
	public EffectList m_startEffects = new EffectList();

	// Token: 0x040005F4 RID: 1524
	public EffectList m_stopEffects = new EffectList();

	// Token: 0x040005F5 RID: 1525
	[Header("__Guardian power__")]
	public float m_cooldown;

	// Token: 0x040005F6 RID: 1526
	public string m_activationAnimation = "gpower";

	// Token: 0x040005F7 RID: 1527
	[NonSerialized]
	public bool m_isNew = true;

	// Token: 0x040005F8 RID: 1528
	private float m_msgTimer;

	// Token: 0x040005F9 RID: 1529
	public Character m_character;

	// Token: 0x040005FA RID: 1530
	protected float m_time;

	// Token: 0x040005FB RID: 1531
	protected GameObject[] m_startEffectInstances;

	// Token: 0x040005FC RID: 1532
	private int m_nameHash;

	// Token: 0x02000246 RID: 582
	public enum StatusAttribute
	{
		// Token: 0x04001FEF RID: 8175
		None,
		// Token: 0x04001FF0 RID: 8176
		ColdResistance,
		// Token: 0x04001FF1 RID: 8177
		DoubleImpactDamage,
		// Token: 0x04001FF2 RID: 8178
		SailingPower = 4,
		// Token: 0x04001FF3 RID: 8179
		TamingBoost = 8
	}
}
