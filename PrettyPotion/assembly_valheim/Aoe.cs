using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Token: 0x02000003 RID: 3
public class Aoe : MonoBehaviour, IProjectile, IMonoUpdater
{
	// Token: 0x06000006 RID: 6 RVA: 0x00002104 File Offset: 0x00000304
	private void Awake()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		this.m_rayMask = 0;
		if (this.m_hitCharacters)
		{
			this.m_rayMask |= LayerMask.GetMask(new string[]
			{
				"character",
				"character_net",
				"character_ghost"
			});
		}
		if (this.m_hitProps)
		{
			this.m_rayMask |= LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
		}
		if (this.m_hitTerrain)
		{
			this.m_rayMask |= LayerMask.GetMask(new string[]
			{
				"terrain"
			});
		}
		if (!string.IsNullOrEmpty(this.m_statusEffect))
		{
			this.m_statusEffectHash = this.m_statusEffect.GetStableHashCode();
		}
		if (!string.IsNullOrEmpty(this.m_statusEffectIfBoss))
		{
			this.m_statusEffectIfBossHash = this.m_statusEffectIfBoss.GetStableHashCode();
		}
		if (!string.IsNullOrEmpty(this.m_statusEffectIfPlayer))
		{
			this.m_statusEffectIfPlayerHash = this.m_statusEffectIfPlayer.GetStableHashCode();
		}
		this.m_activationTimer = this.m_activationDelay;
		if (this.m_ttlMax > 0f)
		{
			this.m_ttl = UnityEngine.Random.Range(this.m_ttl, this.m_ttlMax);
		}
		this.m_chainDelay = this.m_chainStartDelay;
		if (this.m_chainChance == 0f)
		{
			this.m_chainChance = this.m_chainStartChance;
		}
	}

	// Token: 0x06000007 RID: 7 RVA: 0x0000228B File Offset: 0x0000048B
	protected virtual void OnEnable()
	{
		this.m_initRun = true;
		Aoe.Instances.Add(this);
	}

	// Token: 0x06000008 RID: 8 RVA: 0x0000229F File Offset: 0x0000049F
	protected virtual void OnDisable()
	{
		Aoe.Instances.Remove(this);
	}

	// Token: 0x06000009 RID: 9 RVA: 0x000022AD File Offset: 0x000004AD
	private HitData.DamageTypes GetDamage()
	{
		return this.GetDamage(this.m_level);
	}

	// Token: 0x0600000A RID: 10 RVA: 0x000022BC File Offset: 0x000004BC
	private HitData.DamageTypes GetDamage(int itemQuality)
	{
		if (itemQuality <= 1)
		{
			return this.m_damage;
		}
		HitData.DamageTypes damage = this.m_damage;
		int num = (this.m_worldLevel >= 0) ? this.m_worldLevel : Game.m_worldLevel;
		if (num > 0)
		{
			damage.IncreaseEqually((float)(num * Game.instance.m_worldLevelGearBaseDamage), true);
		}
		damage.Add(this.m_damagePerLevel, itemQuality - 1);
		return damage;
	}

	// Token: 0x0600000B RID: 11 RVA: 0x0000231C File Offset: 0x0000051C
	public string GetTooltipString(int itemQuality)
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		stringBuilder.Append("AOE");
		stringBuilder.Append(this.GetDamage(itemQuality).GetTooltipString());
		stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", this.m_attackForce);
		stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", this.m_backstabBonus);
		return stringBuilder.ToString();
	}

	// Token: 0x0600000C RID: 12 RVA: 0x00002388 File Offset: 0x00000588
	private void Update()
	{
		if (this.m_activationTimer > 0f)
		{
			this.m_activationTimer -= Time.deltaTime;
		}
		if (this.m_hitInterval > 0f && this.m_useTriggers)
		{
			this.m_hitTimer -= Time.deltaTime;
			if (this.m_hitTimer <= 0f)
			{
				this.m_hitTimer = this.m_hitInterval;
				this.m_hitList.Clear();
			}
		}
	}

	// Token: 0x0600000D RID: 13 RVA: 0x00002400 File Offset: 0x00000600
	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (this.m_nview != null && !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_initRun && !this.m_useTriggers && !this.m_hitAfterTtl && this.m_activationTimer <= 0f)
		{
			this.m_initRun = false;
			if (this.m_hitInterval <= 0f)
			{
				this.Initiate();
			}
		}
		if (this.m_owner != null && this.m_attachToCaster)
		{
			base.transform.position = this.m_owner.transform.TransformPoint(this.m_offset);
			base.transform.rotation = this.m_owner.transform.rotation * this.m_localRot;
		}
		if (this.m_activationTimer > 0f)
		{
			return;
		}
		if (this.m_hitInterval > 0f && !this.m_useTriggers)
		{
			this.m_hitTimer -= fixedDeltaTime;
			if (this.m_hitTimer <= 0f)
			{
				this.m_hitTimer = this.m_hitInterval;
				this.Initiate();
			}
		}
		if (this.m_chainStartChance > 0f && this.m_chainDelay >= 0f)
		{
			this.m_chainDelay -= fixedDeltaTime;
			if (this.m_chainDelay <= 0f && UnityEngine.Random.value < this.m_chainStartChance)
			{
				Vector3 position = base.transform.position;
				this.FindHits();
				this.SortHits();
				int num = UnityEngine.Random.Range(this.m_chainMinTargets, this.m_chainMaxTargets + 1);
				foreach (Collider collider in Aoe.s_hitList)
				{
					if (UnityEngine.Random.value < this.m_chainChancePerTarget)
					{
						Vector3 position2 = collider.gameObject.transform.position;
						bool flag = false;
						for (int i = 0; i < Aoe.s_chainObjs.Count; i++)
						{
							if (Aoe.s_chainObjs[i])
							{
								if (Vector3.Distance(Aoe.s_chainObjs[i].transform.position, position2) < 0.1f)
								{
									flag = true;
									break;
								}
							}
							else
							{
								Aoe.s_chainObjs.RemoveAt(i);
							}
						}
						if (!flag)
						{
							GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_chainObj, position2, collider.gameObject.transform.rotation);
							Aoe.s_chainObjs.Add(gameObject);
							IProjectile componentInChildren = gameObject.GetComponentInChildren<IProjectile>();
							if (componentInChildren != null)
							{
								componentInChildren.Setup(this.m_owner, position.DirTo(position2), -1f, this.m_hitData, this.m_itemData, this.m_ammo);
								Aoe aoe = componentInChildren as Aoe;
								if (aoe != null)
								{
									aoe.m_chainChance = this.m_chainChance * this.m_chainStartChanceFalloff;
								}
							}
							num--;
							float d = Vector3.Distance(position2, base.transform.position);
							GameObject[] array = this.m_chainEffects.Create(position + Vector3.up, Quaternion.LookRotation(position.DirTo(position2 + Vector3.up)), null, 1f, -1);
							for (int j = 0; j < array.Length; j++)
							{
								array[j].transform.localScale = Vector3.one * d;
							}
						}
					}
					if (num <= 0)
					{
						break;
					}
				}
			}
		}
		if (this.m_ttl > 0f)
		{
			this.m_ttl -= fixedDeltaTime;
			if (this.m_ttl <= 0f)
			{
				if (this.m_hitAfterTtl)
				{
					this.Initiate();
				}
				if (ZNetScene.instance)
				{
					ZNetScene.instance.Destroy(base.gameObject);
				}
			}
		}
	}

	// Token: 0x0600000E RID: 14 RVA: 0x000027C8 File Offset: 0x000009C8
	public void Initiate()
	{
		this.m_initiateEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.CheckHits();
	}

	// Token: 0x0600000F RID: 15 RVA: 0x000027F4 File Offset: 0x000009F4
	private void CheckHits()
	{
		this.FindHits();
		if (this.m_maxTargetsFromCenter > 0)
		{
			this.SortHits();
			int num = this.m_maxTargetsFromCenter;
			using (List<Collider>.Enumerator enumerator = Aoe.s_hitList.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Collider collider = enumerator.Current;
					if (this.OnHit(collider, collider.transform.position))
					{
						num--;
					}
					if (num <= 0)
					{
						break;
					}
				}
				return;
			}
		}
		for (int i = 0; i < Aoe.s_hitList.Count; i++)
		{
			this.OnHit(Aoe.s_hitList[i], Aoe.s_hitList[i].transform.position);
		}
	}

	// Token: 0x06000010 RID: 16 RVA: 0x000028B8 File Offset: 0x00000AB8
	private void FindHits()
	{
		this.m_hitList.Clear();
		int num = (this.m_useCollider != null) ? Physics.OverlapBoxNonAlloc(base.transform.position + this.m_useCollider.center, this.m_useCollider.size / 2f, Aoe.s_hits, base.transform.rotation, this.m_rayMask) : Physics.OverlapSphereNonAlloc(base.transform.position, this.m_radius, Aoe.s_hits, this.m_rayMask);
		Aoe.s_hitList.Clear();
		for (int i = 0; i < num; i++)
		{
			Collider collider = Aoe.s_hits[i];
			if (this.ShouldHit(collider))
			{
				Aoe.s_hitList.Add(collider);
			}
		}
	}

	// Token: 0x06000011 RID: 17 RVA: 0x00002980 File Offset: 0x00000B80
	private bool ShouldHit(Collider collider)
	{
		GameObject gameObject = Projectile.FindHitObject(collider);
		if (gameObject)
		{
			Character component = gameObject.GetComponent<Character>();
			if (component != null)
			{
				if (this.m_nview == null && !component.IsOwner())
				{
					return false;
				}
				if (this.m_owner != null)
				{
					if (!this.m_hitOwner && component == this.m_owner)
					{
						return false;
					}
					if (!this.m_hitSame && component.m_name == this.m_owner.m_name)
					{
						return false;
					}
					bool flag = BaseAI.IsEnemy(this.m_owner, component) || (component.GetBaseAI() && component.GetBaseAI().IsAggravatable() && this.m_owner.IsPlayer());
					if (!this.m_hitFriendly && !flag)
					{
						return false;
					}
					if (!this.m_hitEnemy && flag)
					{
						return false;
					}
				}
				if (!this.m_hitCharacters)
				{
					return false;
				}
				if (this.m_dodgeable && component.IsDodgeInvincible())
				{
					return false;
				}
			}
		}
		return true;
	}

	// Token: 0x06000012 RID: 18 RVA: 0x00002A82 File Offset: 0x00000C82
	private void SortHits()
	{
		Aoe.s_hitList.Sort((Collider a, Collider b) => Vector3.Distance(a.transform.position, base.transform.position).CompareTo(Vector3.Distance(b.transform.position, base.transform.position)));
	}

	// Token: 0x06000013 RID: 19 RVA: 0x00002A9C File Offset: 0x00000C9C
	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		if (item != null)
		{
			this.m_level = item.m_quality;
			this.m_worldLevel = item.m_worldLevel;
			this.m_itemData = item;
		}
		if (this.m_attachToCaster && owner != null)
		{
			this.m_offset = owner.transform.InverseTransformPoint(base.transform.position);
			this.m_localRot = Quaternion.Inverse(owner.transform.rotation) * base.transform.rotation;
		}
		if (hitData != null && this.m_useAttackSettings)
		{
			this.m_damage = hitData.m_damage;
			this.m_blockable = hitData.m_blockable;
			this.m_dodgeable = hitData.m_dodgeable;
			this.m_attackForce = hitData.m_pushForce;
			this.m_backstabBonus = hitData.m_backstabBonus;
			if (this.m_statusEffectHash != hitData.m_statusEffectHash)
			{
				this.m_statusEffectHash = hitData.m_statusEffectHash;
				this.m_statusEffect = "<changed>";
			}
			this.m_toolTier = (int)hitData.m_toolTier;
			this.m_skill = hitData.m_skill;
		}
		this.m_ammo = ammo;
		this.m_hitData = hitData;
	}

	// Token: 0x06000014 RID: 20 RVA: 0x00002BCD File Offset: 0x00000DCD
	private void OnCollisionEnter(Collision collision)
	{
		this.CauseTriggerDamage(collision.collider, true);
	}

	// Token: 0x06000015 RID: 21 RVA: 0x00002BDC File Offset: 0x00000DDC
	private void OnCollisionStay(Collision collision)
	{
		this.CauseTriggerDamage(collision.collider, false);
	}

	// Token: 0x06000016 RID: 22 RVA: 0x00002BEB File Offset: 0x00000DEB
	private void OnTriggerEnter(Collider collider)
	{
		this.CauseTriggerDamage(collider, true);
	}

	// Token: 0x06000017 RID: 23 RVA: 0x00002BF5 File Offset: 0x00000DF5
	private void OnTriggerStay(Collider collider)
	{
		this.CauseTriggerDamage(collider, false);
	}

	// Token: 0x06000018 RID: 24 RVA: 0x00002C00 File Offset: 0x00000E00
	private void CauseTriggerDamage(Collider collider, bool onTriggerEnter)
	{
		if ((this.m_triggerEnterOnly && onTriggerEnter) || this.m_activationTimer > 0f)
		{
			return;
		}
		if (!this.m_useTriggers)
		{
			ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			return;
		}
		if (!this.ShouldHit(collider))
		{
			return;
		}
		this.OnHit(collider, collider.bounds.center);
	}

	// Token: 0x06000019 RID: 25 RVA: 0x00002C68 File Offset: 0x00000E68
	private bool OnHit(Collider collider, Vector3 hitPoint)
	{
		GameObject gameObject = Projectile.FindHitObject(collider);
		if (this.m_hitList.Contains(gameObject))
		{
			return false;
		}
		this.m_hitList.Add(gameObject);
		float num = 1f;
		if (this.m_owner && this.m_owner.IsPlayer() && this.m_skill != Skills.SkillType.None)
		{
			num = this.m_owner.GetRandomSkillFactor(this.m_skill);
		}
		bool result = false;
		bool flag = false;
		float num2 = 1f;
		if (this.m_scaleDamageByDistance)
		{
			num2 = this.m_distanceScaleCurve.Evaluate(Mathf.Clamp01(Vector3.Distance(gameObject.transform.position, base.transform.position) / this.m_radius));
		}
		IDestructible component = gameObject.GetComponent<IDestructible>();
		if (component != null)
		{
			if (!this.m_hitParent)
			{
				if (!(base.gameObject.transform.parent != null) || !(gameObject == base.gameObject.transform.parent.gameObject))
				{
					IDestructible componentInParent = base.gameObject.GetComponentInParent<IDestructible>();
					if (componentInParent == null || componentInParent != component)
					{
						goto IL_109;
					}
				}
				return false;
			}
			IL_109:
			Character character = component as Character;
			if (character)
			{
				if (this.m_useTriggers && !character.IsOwner())
				{
					return false;
				}
				if (this.m_launchCharacters)
				{
					float num3 = UnityEngine.Random.Range(this.m_launchForceMinMax.x, this.m_launchForceMinMax.y);
					num3 *= num2;
					Vector3 a = hitPoint.DirTo(base.transform.position);
					if (this.m_launchForceUpFactor > 0f)
					{
						a = Vector3.Slerp(a, Vector3.up, this.m_launchForceUpFactor);
					}
					character.ForceJump(a.normalized * num3, true);
				}
				flag = true;
			}
			else if (!this.m_hitProps)
			{
				return false;
			}
			Destructible destructible = component as Destructible;
			bool flag2 = (destructible != null && destructible.m_spawnWhenDestroyed != null) || gameObject.GetComponent<MineRock5>() != null;
			Vector3 dir = this.m_attackForceForward ? base.transform.forward : (hitPoint - base.transform.position).normalized;
			HitData hitData = new HitData();
			hitData.m_hitCollider = collider;
			hitData.m_damage = this.GetDamage();
			hitData.m_pushForce = this.m_attackForce * num * num2;
			hitData.m_backstabBonus = this.m_backstabBonus;
			hitData.m_point = (flag2 ? base.transform.position : hitPoint);
			hitData.m_dir = dir;
			hitData.m_statusEffectHash = this.GetStatusEffect(character);
			HitData hitData2 = hitData;
			Character owner = this.m_owner;
			hitData2.m_skillLevel = ((owner != null) ? owner.GetSkillLevel(this.m_skill) : 0f);
			hitData.m_itemLevel = (short)this.m_level;
			hitData.m_itemWorldLevel = (byte)((this.m_worldLevel >= 0) ? this.m_worldLevel : Game.m_worldLevel);
			hitData.m_dodgeable = this.m_dodgeable;
			hitData.m_blockable = this.m_blockable;
			hitData.m_ranged = true;
			hitData.m_ignorePVP = (this.m_owner == character || this.m_ignorePVP);
			hitData.m_toolTier = (short)this.m_toolTier;
			hitData.SetAttacker(this.m_owner);
			hitData.m_damage.Modify(num);
			hitData.m_damage.Modify(num2);
			hitData.m_hitType = ((hitData.GetAttacker() is Player) ? HitData.HitType.PlayerHit : HitData.HitType.EnemyHit);
			hitData.m_radius = this.m_radius;
			component.Damage(hitData);
			if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("damage"))
			{
				string str = "Damage AOE: hitting target";
				string str3;
				if (!(this.m_owner == null))
				{
					string str2 = " with owner: ";
					Character owner2 = this.m_owner;
					str3 = str2 + ((owner2 != null) ? owner2.ToString() : null);
				}
				else
				{
					str3 = " without owner";
				}
				Terminal.Log(str + str3);
			}
			if (this.m_damageSelf > 0f)
			{
				IDestructible componentInParent2 = base.GetComponentInParent<IDestructible>();
				if (componentInParent2 != null)
				{
					HitData hitData3 = new HitData();
					hitData3.m_damage.m_damage = this.m_damageSelf;
					hitData3.m_point = hitPoint;
					hitData3.m_blockable = false;
					hitData3.m_dodgeable = false;
					hitData3.m_hitType = HitData.HitType.Self;
					componentInParent2.Damage(hitData3);
				}
			}
			result = true;
		}
		else
		{
			Heightmap component2 = gameObject.GetComponent<Heightmap>();
			if (component2 != null)
			{
				FootStep.GroundMaterial groundMaterial = component2.GetGroundMaterial(Vector3.up, base.transform.position, this.m_groundLavaValue);
				FootStep.GroundMaterial groundMaterial2 = component2.GetGroundMaterial(Vector3.up, base.transform.position, 0.6f);
				FootStep.GroundMaterial groundMaterial3 = (this.m_groundLavaValue >= 0f) ? groundMaterial : groundMaterial2;
				if (this.m_spawnOnHitTerrain && (this.m_spawnOnGroundType == FootStep.GroundMaterial.Everything || this.m_spawnOnGroundType.HasFlag(groundMaterial3)) && (!this.m_hitTerrainOnlyOnce || !this.m_hasHitTerrain))
				{
					this.m_hasHitTerrain = true;
					int num4 = (this.m_multiSpawnMin == 0) ? 1 : UnityEngine.Random.Range(this.m_multiSpawnMin, this.m_multiSpawnMax);
					Vector3 vector = base.transform.position;
					for (int i = 0; i < num4; i++)
					{
						GameObject gameObject2 = Attack.SpawnOnHitTerrain(vector, this.m_spawnOnHitTerrain, this.m_owner, this.m_hitNoise, null, null, this.m_randomRotation);
						float num5 = (num4 == 1) ? 0f : ((float)i / (float)(num4 - 1));
						float num6 = UnityEngine.Random.Range(this.m_multiSpawnDistanceMin, this.m_multiSpawnDistanceMax);
						Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
						vector += new Vector3(insideUnitCircle.x * num6, 0f, insideUnitCircle.y * num6);
						if (gameObject2 && i > 0)
						{
							gameObject2.transform.localScale = Utils.Vec3((1f - num5) * (this.m_multiSpawnScaleMax - this.m_multiSpawnScaleMin) + this.m_multiSpawnScaleMin);
						}
						if (this.m_multiSpawnSpringDelayMax > 0f)
						{
							ConditionalObject componentInChildren = gameObject2.GetComponentInChildren<ConditionalObject>();
							if (componentInChildren != null)
							{
								componentInChildren.m_appearDelay = num5 * this.m_multiSpawnSpringDelayMax;
							}
						}
						if (this.m_placeOnGround)
						{
							gameObject2.transform.position = new Vector3(gameObject2.transform.position.x, ZoneSystem.instance.GetGroundHeight(gameObject2.transform.position), gameObject2.transform.position.z);
						}
					}
				}
				result = true;
			}
		}
		if (gameObject.GetComponent<MineRock5>() == null)
		{
			this.m_hitEffects.Create(hitPoint, Quaternion.identity, null, 1f, -1);
		}
		if (!this.m_gaveSkill && this.m_owner && this.m_skill > Skills.SkillType.None && flag && this.m_canRaiseSkill)
		{
			this.m_owner.RaiseSkill(this.m_skill, 1f);
			this.m_gaveSkill = true;
		}
		return result;
	}

	// Token: 0x0600001A RID: 26 RVA: 0x0000333B File Offset: 0x0000153B
	private int GetStatusEffect(Character character)
	{
		if (character)
		{
			if (character.IsBoss() && this.m_statusEffectIfBossHash != 0)
			{
				return this.m_statusEffectIfBossHash;
			}
			if (character.IsPlayer() && this.m_statusEffectIfPlayerHash != 0)
			{
				return this.m_statusEffectIfPlayerHash;
			}
		}
		return this.m_statusEffectHash;
	}

	// Token: 0x0600001B RID: 27 RVA: 0x00003379 File Offset: 0x00001579
	private void OnDrawGizmos()
	{
		bool useTriggers = this.m_useTriggers;
	}

	// Token: 0x17000002 RID: 2
	// (get) Token: 0x0600001C RID: 28 RVA: 0x00003382 File Offset: 0x00001582
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04000002 RID: 2
	public string m_name = "";

	// Token: 0x04000003 RID: 3
	[Header("Attack (overridden by item )")]
	public bool m_useAttackSettings = true;

	// Token: 0x04000004 RID: 4
	public HitData.DamageTypes m_damage;

	// Token: 0x04000005 RID: 5
	public bool m_scaleDamageByDistance;

	// Token: 0x04000006 RID: 6
	public AnimationCurve m_distanceScaleCurve = AnimationCurve.Linear(1f, 1f, 0f, 0f);

	// Token: 0x04000007 RID: 7
	public bool m_dodgeable;

	// Token: 0x04000008 RID: 8
	public bool m_blockable;

	// Token: 0x04000009 RID: 9
	public int m_toolTier;

	// Token: 0x0400000A RID: 10
	public float m_attackForce;

	// Token: 0x0400000B RID: 11
	public float m_backstabBonus = 4f;

	// Token: 0x0400000C RID: 12
	public string m_statusEffect = "";

	// Token: 0x0400000D RID: 13
	public string m_statusEffectIfBoss = "";

	// Token: 0x0400000E RID: 14
	public string m_statusEffectIfPlayer = "";

	// Token: 0x0400000F RID: 15
	private int m_statusEffectHash;

	// Token: 0x04000010 RID: 16
	private int m_statusEffectIfBossHash;

	// Token: 0x04000011 RID: 17
	private int m_statusEffectIfPlayerHash;

	// Token: 0x04000012 RID: 18
	[Header("Attack (other)")]
	public HitData.DamageTypes m_damagePerLevel;

	// Token: 0x04000013 RID: 19
	public bool m_attackForceForward;

	// Token: 0x04000014 RID: 20
	public GameObject m_spawnOnHitTerrain;

	// Token: 0x04000015 RID: 21
	public bool m_hitTerrainOnlyOnce;

	// Token: 0x04000016 RID: 22
	public FootStep.GroundMaterial m_spawnOnGroundType = FootStep.GroundMaterial.Everything;

	// Token: 0x04000017 RID: 23
	public float m_groundLavaValue = -1f;

	// Token: 0x04000018 RID: 24
	public float m_hitNoise;

	// Token: 0x04000019 RID: 25
	public bool m_placeOnGround;

	// Token: 0x0400001A RID: 26
	public bool m_randomRotation;

	// Token: 0x0400001B RID: 27
	public int m_maxTargetsFromCenter;

	// Token: 0x0400001C RID: 28
	[Header("Multi Spawn (Lava Bomb)")]
	public int m_multiSpawnMin;

	// Token: 0x0400001D RID: 29
	public int m_multiSpawnMax;

	// Token: 0x0400001E RID: 30
	public float m_multiSpawnDistanceMin;

	// Token: 0x0400001F RID: 31
	public float m_multiSpawnDistanceMax;

	// Token: 0x04000020 RID: 32
	public float m_multiSpawnScaleMin;

	// Token: 0x04000021 RID: 33
	public float m_multiSpawnScaleMax;

	// Token: 0x04000022 RID: 34
	public float m_multiSpawnSpringDelayMax;

	// Token: 0x04000023 RID: 35
	[Header("Chain Spawn")]
	public float m_chainStartChance;

	// Token: 0x04000024 RID: 36
	public float m_chainStartChanceFalloff = 0.8f;

	// Token: 0x04000025 RID: 37
	public float m_chainChancePerTarget;

	// Token: 0x04000026 RID: 38
	public GameObject m_chainObj;

	// Token: 0x04000027 RID: 39
	public float m_chainStartDelay;

	// Token: 0x04000028 RID: 40
	public int m_chainMinTargets;

	// Token: 0x04000029 RID: 41
	public int m_chainMaxTargets;

	// Token: 0x0400002A RID: 42
	public EffectList m_chainEffects = new EffectList();

	// Token: 0x0400002B RID: 43
	private float m_chainDelay;

	// Token: 0x0400002C RID: 44
	private float m_chainChance;

	// Token: 0x0400002D RID: 45
	[Header("Damage self")]
	public float m_damageSelf;

	// Token: 0x0400002E RID: 46
	[Header("Ignore targets")]
	public bool m_hitOwner;

	// Token: 0x0400002F RID: 47
	public bool m_hitParent = true;

	// Token: 0x04000030 RID: 48
	public bool m_hitSame;

	// Token: 0x04000031 RID: 49
	public bool m_hitFriendly = true;

	// Token: 0x04000032 RID: 50
	public bool m_hitEnemy = true;

	// Token: 0x04000033 RID: 51
	public bool m_hitCharacters = true;

	// Token: 0x04000034 RID: 52
	public bool m_hitProps = true;

	// Token: 0x04000035 RID: 53
	public bool m_hitTerrain;

	// Token: 0x04000036 RID: 54
	public bool m_ignorePVP;

	// Token: 0x04000037 RID: 55
	[Header("Launch Characters")]
	public bool m_launchCharacters;

	// Token: 0x04000038 RID: 56
	public Vector2 m_launchForceMinMax = Vector2.up;

	// Token: 0x04000039 RID: 57
	[Range(0f, 1f)]
	public float m_launchForceUpFactor = 0.5f;

	// Token: 0x0400003A RID: 58
	[Header("Other")]
	public Skills.SkillType m_skill;

	// Token: 0x0400003B RID: 59
	public bool m_canRaiseSkill = true;

	// Token: 0x0400003C RID: 60
	public bool m_useTriggers;

	// Token: 0x0400003D RID: 61
	public bool m_triggerEnterOnly;

	// Token: 0x0400003E RID: 62
	public BoxCollider m_useCollider;

	// Token: 0x0400003F RID: 63
	public float m_radius = 4f;

	// Token: 0x04000040 RID: 64
	[global::Tooltip("Wait this long before we start doing any damage")]
	public float m_activationDelay;

	// Token: 0x04000041 RID: 65
	public float m_ttl = 4f;

	// Token: 0x04000042 RID: 66
	[global::Tooltip("When set, ttl will be a random value between ttl and ttlMax")]
	public float m_ttlMax;

	// Token: 0x04000043 RID: 67
	public bool m_hitAfterTtl;

	// Token: 0x04000044 RID: 68
	public float m_hitInterval = 1f;

	// Token: 0x04000045 RID: 69
	public bool m_hitOnEnable;

	// Token: 0x04000046 RID: 70
	public bool m_attachToCaster;

	// Token: 0x04000047 RID: 71
	public EffectList m_hitEffects = new EffectList();

	// Token: 0x04000048 RID: 72
	public EffectList m_initiateEffect = new EffectList();

	// Token: 0x04000049 RID: 73
	private static Collider[] s_hits = new Collider[100];

	// Token: 0x0400004A RID: 74
	private static List<Collider> s_hitList = new List<Collider>();

	// Token: 0x0400004B RID: 75
	private static int s_hitListCount;

	// Token: 0x0400004C RID: 76
	private static List<GameObject> s_chainObjs = new List<GameObject>();

	// Token: 0x0400004D RID: 77
	private ZNetView m_nview;

	// Token: 0x0400004E RID: 78
	private Character m_owner;

	// Token: 0x0400004F RID: 79
	private readonly List<GameObject> m_hitList = new List<GameObject>();

	// Token: 0x04000050 RID: 80
	private float m_hitTimer;

	// Token: 0x04000051 RID: 81
	private float m_activationTimer;

	// Token: 0x04000052 RID: 82
	private Vector3 m_offset = Vector3.zero;

	// Token: 0x04000053 RID: 83
	private Quaternion m_localRot = Quaternion.identity;

	// Token: 0x04000054 RID: 84
	private int m_level;

	// Token: 0x04000055 RID: 85
	private int m_worldLevel = -1;

	// Token: 0x04000056 RID: 86
	private int m_rayMask;

	// Token: 0x04000057 RID: 87
	private bool m_gaveSkill;

	// Token: 0x04000058 RID: 88
	private bool m_hasHitTerrain;

	// Token: 0x04000059 RID: 89
	private bool m_initRun = true;

	// Token: 0x0400005A RID: 90
	private HitData m_hitData;

	// Token: 0x0400005B RID: 91
	private ItemDrop.ItemData m_itemData;

	// Token: 0x0400005C RID: 92
	private ItemDrop.ItemData m_ammo;
}
