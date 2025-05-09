using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200000D RID: 13
public class MonsterAI : BaseAI
{
	// Token: 0x060000D9 RID: 217 RVA: 0x0000B754 File Offset: 0x00009954
	protected override void Awake()
	{
		base.Awake();
		this.m_despawnInDay = this.m_nview.GetZDO().GetBool(ZDOVars.s_despawnInDay, this.m_despawnInDay);
		this.m_eventCreature = this.m_nview.GetZDO().GetBool(ZDOVars.s_eventCreature, this.m_eventCreature);
		this.m_sleeping = this.m_nview.GetZDO().GetBool(ZDOVars.s_sleeping, this.m_sleeping);
		this.m_animator.SetBool(MonsterAI.s_sleeping, this.IsSleeping());
		this.m_interceptTime = UnityEngine.Random.Range(this.m_interceptTimeMin, this.m_interceptTimeMax);
		this.m_pauseTimer = UnityEngine.Random.Range(0f, this.m_circleTargetInterval);
		this.m_updateTargetTimer = UnityEngine.Random.Range(0f, 2f);
		if (this.m_wakeUpDelayMin > 0f || this.m_wakeUpDelayMax > 0f)
		{
			this.m_sleepDelay = UnityEngine.Random.Range(this.m_wakeUpDelayMin, this.m_wakeUpDelayMax);
		}
		if (this.m_enableHuntPlayer)
		{
			base.SetHuntPlayer(true);
		}
		this.m_nview.Register("RPC_Wakeup", new Action<long>(this.RPC_Wakeup));
	}

	// Token: 0x060000DA RID: 218 RVA: 0x0000B880 File Offset: 0x00009A80
	private void Start()
	{
		if (this.m_nview && this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			Humanoid humanoid = this.m_character as Humanoid;
			if (humanoid)
			{
				humanoid.EquipBestWeapon(null, null, null, null);
			}
		}
	}

	// Token: 0x060000DB RID: 219 RVA: 0x0000B8D2 File Offset: 0x00009AD2
	protected override void OnDamaged(float damage, Character attacker)
	{
		base.OnDamaged(damage, attacker);
		this.Wakeup();
		this.SetAlerted(true);
		this.SetTarget(attacker);
	}

	// Token: 0x060000DC RID: 220 RVA: 0x0000B8F0 File Offset: 0x00009AF0
	private void SetTarget(Character attacker)
	{
		if (attacker != null && this.m_targetCreature == null)
		{
			if (attacker.IsPlayer() && this.m_character.IsTamed())
			{
				return;
			}
			this.m_targetCreature = attacker;
			this.m_lastKnownTargetPos = attacker.transform.position;
			this.m_beenAtLastPos = false;
			this.m_targetStatic = null;
		}
	}

	// Token: 0x060000DD RID: 221 RVA: 0x0000B950 File Offset: 0x00009B50
	protected override void RPC_OnNearProjectileHit(long sender, Vector3 center, float range, ZDOID attackerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs))
		{
			return;
		}
		this.SetAlerted(true);
		if (this.m_fleeIfNotAlerted)
		{
			return;
		}
		GameObject gameObject = ZNetScene.instance.FindInstance(attackerID);
		if (gameObject != null)
		{
			Character component = gameObject.GetComponent<Character>();
			if (component)
			{
				this.SetTarget(component);
			}
		}
	}

	// Token: 0x060000DE RID: 222 RVA: 0x0000B9B6 File Offset: 0x00009BB6
	public void MakeTame()
	{
		this.m_character.SetTamed(true);
		this.SetAlerted(false);
		this.m_targetCreature = null;
		this.m_targetStatic = null;
	}

	// Token: 0x060000DF RID: 223 RVA: 0x0000B9DC File Offset: 0x00009BDC
	private void UpdateTarget(Humanoid humanoid, float dt, out bool canHearTarget, out bool canSeeTarget)
	{
		this.m_unableToAttackTargetTimer -= dt;
		this.m_updateTargetTimer -= dt;
		if (this.m_updateTargetTimer <= 0f && !this.m_character.InAttack())
		{
			this.m_updateTargetTimer = (Player.IsPlayerInRange(base.transform.position, 50f) ? 2f : 6f);
			Character character = base.FindEnemy();
			if (character)
			{
				this.m_targetCreature = character;
				this.m_targetStatic = null;
			}
			bool flag = this.m_targetCreature != null && this.m_targetCreature.IsPlayer();
			bool flag2 = this.m_targetCreature != null && this.m_unableToAttackTargetTimer > 0f && !base.HavePath(this.m_targetCreature.transform.position);
			if (this.m_attackPlayerObjects && (!this.m_aggravatable || base.IsAggravated()) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs) && (this.m_targetCreature == null || flag2) && !this.m_character.IsTamed())
			{
				StaticTarget staticTarget = base.FindClosestStaticPriorityTarget();
				if (staticTarget)
				{
					this.m_targetStatic = staticTarget;
					this.m_targetCreature = null;
				}
				bool flag3 = false;
				if (this.m_targetStatic != null)
				{
					Vector3 target = this.m_targetStatic.FindClosestPoint(this.m_character.transform.position);
					flag3 = base.HavePath(target);
				}
				if ((this.m_targetStatic == null || !flag3) && base.IsAlerted() && flag)
				{
					StaticTarget staticTarget2 = base.FindRandomStaticTarget(10f);
					if (staticTarget2)
					{
						this.m_targetStatic = staticTarget2;
						this.m_targetCreature = null;
					}
				}
			}
		}
		if (this.m_targetCreature && this.m_character.IsTamed())
		{
			Vector3 b;
			if (base.GetPatrolPoint(out b))
			{
				if (Vector3.Distance(this.m_targetCreature.transform.position, b) > this.m_alertRange)
				{
					this.m_targetCreature = null;
				}
			}
			else if (this.m_follow && Vector3.Distance(this.m_targetCreature.transform.position, this.m_follow.transform.position) > this.m_alertRange)
			{
				this.m_targetCreature = null;
			}
		}
		if (this.m_targetCreature)
		{
			if (this.m_targetCreature.IsDead())
			{
				this.m_targetCreature = null;
			}
			else if (!base.IsEnemy(this.m_targetCreature))
			{
				this.m_targetCreature = null;
			}
			else if (this.m_skipLavaTargets && this.m_targetCreature.AboveOrInLava())
			{
				this.m_targetCreature = null;
			}
		}
		canHearTarget = false;
		canSeeTarget = false;
		if (this.m_targetCreature)
		{
			canHearTarget = base.CanHearTarget(this.m_targetCreature);
			canSeeTarget = base.CanSeeTarget(this.m_targetCreature);
			if (canSeeTarget | canHearTarget)
			{
				this.m_timeSinceSensedTargetCreature = 0f;
			}
			if (this.m_targetCreature.IsPlayer())
			{
				this.m_targetCreature.OnTargeted(canSeeTarget | canHearTarget, base.IsAlerted());
			}
			base.SetTargetInfo(this.m_targetCreature.GetZDOID());
		}
		else
		{
			base.SetTargetInfo(ZDOID.None);
		}
		this.m_timeSinceSensedTargetCreature += dt;
		if (base.IsAlerted() || this.m_targetCreature != null)
		{
			this.m_timeSinceAttacking += dt;
			float num = 60f;
			float num2 = Vector3.Distance(this.m_spawnPoint, base.transform.position);
			bool flag4 = this.HuntPlayer() && this.m_targetCreature && this.m_targetCreature.IsPlayer();
			if (this.m_timeSinceSensedTargetCreature > 30f || (!flag4 && (this.m_timeSinceAttacking > num || (this.m_maxChaseDistance > 0f && this.m_timeSinceSensedTargetCreature > 1f && num2 > this.m_maxChaseDistance))))
			{
				this.SetAlerted(false);
				this.m_targetCreature = null;
				this.m_targetStatic = null;
				this.m_timeSinceAttacking = 0f;
				this.m_updateTargetTimer = 5f;
			}
		}
	}

	// Token: 0x060000E0 RID: 224 RVA: 0x0000BE04 File Offset: 0x0000A004
	public override bool UpdateAI(float dt)
	{
		if (!base.UpdateAI(dt))
		{
			return false;
		}
		if (this.IsSleeping())
		{
			this.UpdateSleep(dt);
			return true;
		}
		Humanoid humanoid = this.m_character as Humanoid;
		if (this.HuntPlayer())
		{
			this.SetAlerted(true);
		}
		bool flag;
		bool flag2;
		this.UpdateTarget(humanoid, dt, out flag, out flag2);
		if (this.m_tamable && this.m_tamable.m_saddle && this.m_tamable.m_saddle.UpdateRiding(dt))
		{
			return true;
		}
		if (this.m_avoidLand && !this.m_character.IsSwimming())
		{
			base.MoveToWater(dt, 20f);
			return true;
		}
		if (this.DespawnInDay() && EnvMan.IsDay() && (this.m_targetCreature == null || !flag2))
		{
			base.MoveAwayAndDespawn(dt, true);
			return true;
		}
		if (this.IsEventCreature() && !RandEventSystem.HaveActiveEvent())
		{
			base.SetHuntPlayer(false);
			if (this.m_targetCreature == null && !base.IsAlerted())
			{
				base.MoveAwayAndDespawn(dt, false);
				return true;
			}
		}
		if (this.m_fleeIfNotAlerted && !this.HuntPlayer() && this.m_targetCreature && !base.IsAlerted() && Vector3.Distance(this.m_targetCreature.transform.position, base.transform.position) - this.m_targetCreature.GetRadius() > this.m_alertRange)
		{
			base.Flee(dt, this.m_targetCreature.transform.position);
			return true;
		}
		if (this.m_fleeIfLowHealth > 0f && this.m_timeSinceHurt < this.m_fleeTimeSinceHurt && this.m_targetCreature != null && this.m_character.GetHealthPercentage() < this.m_fleeIfLowHealth)
		{
			base.Flee(dt, this.m_targetCreature.transform.position);
			return true;
		}
		if (this.m_fleeInLava && this.m_character.InLava() && (this.m_targetCreature == null || this.m_targetCreature.AboveOrInLava()))
		{
			base.Flee(dt, this.m_character.transform.position - this.m_character.transform.forward);
			return true;
		}
		if ((this.m_afraidOfFire || this.m_avoidFire) && base.AvoidFire(dt, this.m_targetCreature, this.m_afraidOfFire))
		{
			if (this.m_afraidOfFire)
			{
				this.m_targetStatic = null;
				this.m_targetCreature = null;
			}
			return true;
		}
		if (!this.m_character.IsTamed())
		{
			if (this.m_targetCreature != null)
			{
				if (EffectArea.IsPointInsideNoMonsterArea(this.m_targetCreature.transform.position))
				{
					base.Flee(dt, this.m_targetCreature.transform.position);
					return true;
				}
			}
			else
			{
				EffectArea effectArea = EffectArea.IsPointCloseToNoMonsterArea(base.transform.position);
				if (effectArea != null)
				{
					base.Flee(dt, effectArea.transform.position);
					return true;
				}
			}
		}
		if (this.m_fleeIfHurtWhenTargetCantBeReached && this.m_targetCreature != null && this.m_timeSinceAttacking > 30f && this.m_timeSinceHurt < 20f)
		{
			base.Flee(dt, this.m_targetCreature.transform.position);
			this.m_lastKnownTargetPos = base.transform.position;
			this.m_updateTargetTimer = 1f;
			return true;
		}
		if ((!base.IsAlerted() || (this.m_targetStatic == null && this.m_targetCreature == null)) && this.UpdateConsumeItem(humanoid, dt))
		{
			return true;
		}
		if (this.m_circleTargetInterval > 0f && this.m_targetCreature)
		{
			this.m_pauseTimer += dt;
			if (this.m_pauseTimer > this.m_circleTargetInterval)
			{
				if (this.m_pauseTimer > this.m_circleTargetInterval + this.m_circleTargetDuration)
				{
					this.m_pauseTimer = UnityEngine.Random.Range(0f, this.m_circleTargetInterval / 10f);
				}
				base.RandomMovementArroundPoint(dt, this.m_targetCreature.transform.position, this.m_circleTargetDistance, base.IsAlerted());
				return true;
			}
		}
		ItemDrop.ItemData itemData = this.SelectBestAttack(humanoid, dt);
		bool flag3 = itemData != null && Time.time - itemData.m_lastAttackTime > itemData.m_shared.m_aiAttackInterval && this.m_character.GetTimeSinceLastAttack() >= this.m_minAttackInterval && !base.IsTakingOff();
		if (!base.IsCharging() && (this.m_targetStatic != null || this.m_targetCreature != null) && itemData != null && flag3 && !this.m_character.InAttack() && itemData.m_shared.m_attack != null && !itemData.m_shared.m_attack.IsDone() && !string.IsNullOrEmpty(itemData.m_shared.m_attack.m_chargeAnimationBool))
		{
			base.ChargeStart(itemData.m_shared.m_attack.m_chargeAnimationBool);
		}
		if ((this.m_character.IsFlying() ? this.m_circulateWhileChargingFlying : this.m_circulateWhileCharging) && (this.m_targetStatic != null || this.m_targetCreature != null) && itemData != null && !flag3 && !this.m_character.InAttack())
		{
			Vector3 point = this.m_targetCreature ? this.m_targetCreature.transform.position : this.m_targetStatic.transform.position;
			base.RandomMovementArroundPoint(dt, point, this.m_randomMoveRange, base.IsAlerted());
			return true;
		}
		if ((this.m_targetStatic == null && this.m_targetCreature == null) || itemData == null)
		{
			if (this.m_follow)
			{
				base.Follow(this.m_follow, dt);
			}
			else
			{
				base.IdleMovement(dt);
			}
			base.ChargeStop();
			return true;
		}
		if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
		{
			if (this.m_targetStatic)
			{
				Vector3 vector = this.m_targetStatic.FindClosestPoint(base.transform.position);
				if (Vector3.Distance(vector, base.transform.position) < itemData.m_shared.m_aiAttackRange && base.CanSeeTarget(this.m_targetStatic))
				{
					base.LookAt(this.m_targetStatic.GetCenter());
					if (itemData.m_shared.m_aiAttackMaxAngle == 0f)
					{
						ZLog.LogError("AI Attack Max Angle for " + itemData.m_shared.m_name + " is 0!");
					}
					if (base.IsLookingAt(this.m_targetStatic.GetCenter(), itemData.m_shared.m_aiAttackMaxAngle, itemData.m_shared.m_aiInvertAngleCheck) && flag3)
					{
						this.DoAttack(null, false);
					}
					else
					{
						base.StopMoving();
					}
				}
				else
				{
					base.MoveTo(dt, vector, 0f, base.IsAlerted());
					base.ChargeStop();
				}
			}
			else if (this.m_targetCreature)
			{
				if (flag || flag2 || (this.HuntPlayer() && this.m_targetCreature.IsPlayer()))
				{
					this.m_beenAtLastPos = false;
					this.m_lastKnownTargetPos = this.m_targetCreature.transform.position;
					float num = Vector3.Distance(this.m_lastKnownTargetPos, base.transform.position) - this.m_targetCreature.GetRadius();
					float num2 = this.m_alertRange * this.m_targetCreature.GetStealthFactor();
					if (flag2 && num < num2)
					{
						this.SetAlerted(true);
					}
					bool flag4 = num < itemData.m_shared.m_aiAttackRange;
					if (!flag4 || !flag2 || itemData.m_shared.m_aiAttackRangeMin < 0f || !base.IsAlerted())
					{
						Vector3 velocity = this.m_targetCreature.GetVelocity();
						Vector3 vector2 = velocity * this.m_interceptTime;
						Vector3 vector3 = this.m_lastKnownTargetPos;
						if (num > vector2.magnitude / 4f)
						{
							vector3 += velocity * this.m_interceptTime;
						}
						base.MoveTo(dt, vector3, 0f, base.IsAlerted());
						if (this.m_timeSinceAttacking > 15f)
						{
							this.m_unableToAttackTargetTimer = 15f;
						}
					}
					else
					{
						base.StopMoving();
					}
					if (flag4 && flag2 && base.IsAlerted())
					{
						if (this.PheromoneFleeCheck(this.m_targetCreature))
						{
							base.Flee(dt, this.m_targetCreature.transform.position);
							this.m_updateTargetTimer = UnityEngine.Random.Range(this.m_fleePheromoneMin, this.m_fleePheromoneMax);
							this.m_targetCreature = null;
						}
						else
						{
							base.LookAt(this.m_targetCreature.GetTopPoint());
							if (flag3 && base.IsLookingAt(this.m_lastKnownTargetPos, itemData.m_shared.m_aiAttackMaxAngle, itemData.m_shared.m_aiInvertAngleCheck))
							{
								this.DoAttack(this.m_targetCreature, false);
							}
						}
					}
				}
				else
				{
					base.ChargeStop();
					if (this.m_beenAtLastPos)
					{
						base.RandomMovement(dt, this.m_lastKnownTargetPos, false);
						if (this.m_timeSinceAttacking > 15f)
						{
							this.m_unableToAttackTargetTimer = 15f;
						}
					}
					else if (base.MoveTo(dt, this.m_lastKnownTargetPos, 0f, base.IsAlerted()))
					{
						this.m_beenAtLastPos = true;
					}
				}
			}
		}
		else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt || itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Friend)
		{
			Character character = (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt) ? base.HaveHurtFriendInRange(this.m_viewRange) : base.HaveFriendInRange(this.m_viewRange);
			if (character)
			{
				if (Vector3.Distance(character.transform.position, base.transform.position) < itemData.m_shared.m_aiAttackRange)
				{
					if (flag3)
					{
						base.StopMoving();
						base.LookAt(character.transform.position);
						this.DoAttack(character, true);
					}
					else
					{
						base.RandomMovement(dt, character.transform.position, false);
					}
				}
				else
				{
					base.MoveTo(dt, character.transform.position, 0f, base.IsAlerted());
				}
			}
			else
			{
				base.RandomMovement(dt, base.transform.position, true);
			}
		}
		return true;
	}

	// Token: 0x060000E1 RID: 225 RVA: 0x0000C820 File Offset: 0x0000AA20
	private bool PheromoneFleeCheck(Character target)
	{
		Player player = target as Player;
		if (player != null)
		{
			foreach (StatusEffect statusEffect in player.GetSEMan().GetStatusEffects())
			{
				SE_Stats se_Stats = statusEffect as SE_Stats;
				if (se_Stats != null && se_Stats.m_pheromoneFlee)
				{
					Character component = se_Stats.m_pheromoneTarget.GetComponent<Character>();
					if (component != null && component.m_name == this.m_character.m_name)
					{
						return true;
					}
				}
			}
			return false;
		}
		return false;
	}

	// Token: 0x060000E2 RID: 226 RVA: 0x0000C8BC File Offset: 0x0000AABC
	private bool UpdateConsumeItem(Humanoid humanoid, float dt)
	{
		if (this.m_consumeItems == null || this.m_consumeItems.Count == 0)
		{
			return false;
		}
		this.m_consumeSearchTimer += dt;
		if (this.m_consumeSearchTimer > this.m_consumeSearchInterval)
		{
			this.m_consumeSearchTimer = 0f;
			if (this.m_tamable && !this.m_tamable.IsHungry())
			{
				return false;
			}
			this.m_consumeTarget = this.FindClosestConsumableItem(this.m_consumeSearchRange);
		}
		if (this.m_consumeTarget)
		{
			if (base.MoveTo(dt, this.m_consumeTarget.transform.position, this.m_consumeRange, false))
			{
				base.LookAt(this.m_consumeTarget.transform.position);
				if (base.IsLookingAt(this.m_consumeTarget.transform.position, 20f, false) && this.m_consumeTarget.RemoveOne())
				{
					if (this.m_onConsumedItem != null)
					{
						this.m_onConsumedItem(this.m_consumeTarget);
					}
					humanoid.m_consumeItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
					this.m_animator.SetTrigger("consume");
					this.m_consumeTarget = null;
				}
			}
			return true;
		}
		return false;
	}

	// Token: 0x060000E3 RID: 227 RVA: 0x0000CA00 File Offset: 0x0000AC00
	private ItemDrop FindClosestConsumableItem(float maxRange)
	{
		if (MonsterAI.m_itemMask == 0)
		{
			MonsterAI.m_itemMask = LayerMask.GetMask(new string[]
			{
				"item"
			});
		}
		Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, MonsterAI.m_itemMask);
		ItemDrop itemDrop = null;
		float num = 999999f;
		foreach (Collider collider in array)
		{
			if (collider.attachedRigidbody)
			{
				ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
				if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && this.CanConsume(component.m_itemData))
				{
					float num2 = Vector3.Distance(component.transform.position, base.transform.position);
					if (itemDrop == null || num2 < num)
					{
						itemDrop = component;
						num = num2;
					}
				}
			}
		}
		if (itemDrop && base.HavePath(itemDrop.transform.position))
		{
			return itemDrop;
		}
		return null;
	}

	// Token: 0x060000E4 RID: 228 RVA: 0x0000CAF4 File Offset: 0x0000ACF4
	private bool CanConsume(ItemDrop.ItemData item)
	{
		using (List<ItemDrop>.Enumerator enumerator = this.m_consumeItems.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_itemData.m_shared.m_name == item.m_shared.m_name)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x060000E5 RID: 229 RVA: 0x0000CB68 File Offset: 0x0000AD68
	private ItemDrop.ItemData SelectBestAttack(Humanoid humanoid, float dt)
	{
		if (this.m_targetCreature || this.m_targetStatic)
		{
			this.m_updateWeaponTimer -= dt;
			if (this.m_updateWeaponTimer <= 0f && !this.m_character.InAttack())
			{
				this.m_updateWeaponTimer = 1f;
				Character hurtFriend;
				Character friend;
				base.HaveFriendsInRange(this.m_viewRange, out hurtFriend, out friend);
				humanoid.EquipBestWeapon(this.m_targetCreature, this.m_targetStatic, hurtFriend, friend);
			}
		}
		return humanoid.GetCurrentWeapon();
	}

	// Token: 0x060000E6 RID: 230 RVA: 0x0000CBEC File Offset: 0x0000ADEC
	private bool DoAttack(Character target, bool isFriend)
	{
		ItemDrop.ItemData currentWeapon = (this.m_character as Humanoid).GetCurrentWeapon();
		if (currentWeapon == null)
		{
			return false;
		}
		if (!base.CanUseAttack(currentWeapon))
		{
			return false;
		}
		bool flag = this.m_character.StartAttack(target, false);
		if (flag)
		{
			this.m_timeSinceAttacking = 0f;
		}
		return flag;
	}

	// Token: 0x060000E7 RID: 231 RVA: 0x0000CC35 File Offset: 0x0000AE35
	public void SetDespawnInDay(bool despawn)
	{
		this.m_despawnInDay = despawn;
		this.m_nview.GetZDO().Set(ZDOVars.s_despawnInDay, despawn);
	}

	// Token: 0x060000E8 RID: 232 RVA: 0x0000CC54 File Offset: 0x0000AE54
	public bool DespawnInDay()
	{
		if (Time.time - this.m_lastDespawnInDayCheck > 4f)
		{
			this.m_lastDespawnInDayCheck = Time.time;
			this.m_despawnInDay = this.m_nview.GetZDO().GetBool(ZDOVars.s_despawnInDay, this.m_despawnInDay);
		}
		return this.m_despawnInDay;
	}

	// Token: 0x060000E9 RID: 233 RVA: 0x0000CCA6 File Offset: 0x0000AEA6
	public void SetEventCreature(bool despawn)
	{
		this.m_eventCreature = despawn;
		this.m_nview.GetZDO().Set(ZDOVars.s_eventCreature, despawn);
	}

	// Token: 0x060000EA RID: 234 RVA: 0x0000CCC8 File Offset: 0x0000AEC8
	public bool IsEventCreature()
	{
		if (Time.time - this.m_lastEventCreatureCheck > 4f)
		{
			this.m_lastEventCreatureCheck = Time.time;
			this.m_eventCreature = this.m_nview.GetZDO().GetBool(ZDOVars.s_eventCreature, this.m_eventCreature);
		}
		return this.m_eventCreature;
	}

	// Token: 0x060000EB RID: 235 RVA: 0x0000CD1A File Offset: 0x0000AF1A
	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		this.DrawAILabel();
	}

	// Token: 0x060000EC RID: 236 RVA: 0x0000CD28 File Offset: 0x0000AF28
	private void OnDrawGizmos()
	{
		if (Terminal.m_showTests)
		{
			this.DrawAILabel();
		}
	}

	// Token: 0x060000ED RID: 237 RVA: 0x0000CD37 File Offset: 0x0000AF37
	private void DrawAILabel()
	{
	}

	// Token: 0x060000EE RID: 238 RVA: 0x0000CD39 File Offset: 0x0000AF39
	public override Character GetTargetCreature()
	{
		return this.m_targetCreature;
	}

	// Token: 0x060000EF RID: 239 RVA: 0x0000CD41 File Offset: 0x0000AF41
	public StaticTarget GetStaticTarget()
	{
		return this.m_targetStatic;
	}

	// Token: 0x060000F0 RID: 240 RVA: 0x0000CD4C File Offset: 0x0000AF4C
	private void UpdateSleep(float dt)
	{
		if (!this.IsSleeping())
		{
			return;
		}
		this.m_sleepTimer += dt;
		if (this.m_sleepTimer < this.m_sleepDelay)
		{
			return;
		}
		if (this.HuntPlayer())
		{
			this.Wakeup();
			return;
		}
		if (this.m_wakeupRange > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, this.m_wakeupRange);
			if (closestPlayer && !closestPlayer.InGhostMode() && !closestPlayer.IsDebugFlying())
			{
				this.Wakeup();
				return;
			}
		}
		if (this.m_noiseWakeup)
		{
			Player playerNoiseRange = Player.GetPlayerNoiseRange(base.transform.position, this.m_maxNoiseWakeupRange);
			if (playerNoiseRange && !playerNoiseRange.InGhostMode() && !playerNoiseRange.IsDebugFlying())
			{
				this.Wakeup();
				return;
			}
		}
	}

	// Token: 0x060000F1 RID: 241 RVA: 0x0000CE10 File Offset: 0x0000B010
	public void OnPrivateAreaAttacked(Character attacker, bool destroyed)
	{
		if (attacker.IsPlayer() && base.IsAggravatable() && !base.IsAggravated())
		{
			this.m_privateAreaAttacks++;
			if (this.m_privateAreaAttacks > this.m_privateAreaTriggerTreshold || destroyed)
			{
				base.SetAggravated(true, BaseAI.AggravatedReason.Damage);
			}
		}
	}

	// Token: 0x060000F2 RID: 242 RVA: 0x0000CE5D File Offset: 0x0000B05D
	private void RPC_Wakeup(long sender)
	{
		if (this.m_nview.GetZDO().IsOwner())
		{
			return;
		}
		this.m_sleeping = false;
	}

	// Token: 0x060000F3 RID: 243 RVA: 0x0000CE7C File Offset: 0x0000B07C
	private void Wakeup()
	{
		if (!this.IsSleeping())
		{
			return;
		}
		this.m_animator.SetBool(MonsterAI.s_sleeping, false);
		this.m_nview.GetZDO().Set(ZDOVars.s_sleeping, false);
		this.m_wakeupEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		this.m_sleeping = false;
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_Wakeup", Array.Empty<object>());
	}

	// Token: 0x060000F4 RID: 244 RVA: 0x0000CF03 File Offset: 0x0000B103
	public override bool IsSleeping()
	{
		return this.m_sleeping;
	}

	// Token: 0x060000F5 RID: 245 RVA: 0x0000CF0B File Offset: 0x0000B10B
	protected override void SetAlerted(bool alert)
	{
		if (alert)
		{
			this.m_timeSinceSensedTargetCreature = 0f;
		}
		base.SetAlerted(alert);
	}

	// Token: 0x060000F6 RID: 246 RVA: 0x0000CF22 File Offset: 0x0000B122
	public override bool HuntPlayer()
	{
		return base.HuntPlayer() && (!this.IsEventCreature() || RandEventSystem.InEvent()) && (!this.DespawnInDay() || !EnvMan.IsDay());
	}

	// Token: 0x060000F7 RID: 247 RVA: 0x0000CF51 File Offset: 0x0000B151
	public GameObject GetFollowTarget()
	{
		return this.m_follow;
	}

	// Token: 0x060000F8 RID: 248 RVA: 0x0000CF59 File Offset: 0x0000B159
	public void SetFollowTarget(GameObject go)
	{
		this.m_follow = go;
	}

	// Token: 0x040001BE RID: 446
	private float m_lastDespawnInDayCheck = -9999f;

	// Token: 0x040001BF RID: 447
	private float m_lastEventCreatureCheck = -9999f;

	// Token: 0x040001C0 RID: 448
	public Action<ItemDrop> m_onConsumedItem;

	// Token: 0x040001C1 RID: 449
	private const float m_giveUpTime = 30f;

	// Token: 0x040001C2 RID: 450
	private const float m_updateTargetFarRange = 50f;

	// Token: 0x040001C3 RID: 451
	private const float m_updateTargetIntervalNear = 2f;

	// Token: 0x040001C4 RID: 452
	private const float m_updateTargetIntervalFar = 6f;

	// Token: 0x040001C5 RID: 453
	private const float m_updateWeaponInterval = 1f;

	// Token: 0x040001C6 RID: 454
	private const float m_unableToAttackTargetDuration = 15f;

	// Token: 0x040001C7 RID: 455
	[Header("Monster AI")]
	public float m_alertRange = 9999f;

	// Token: 0x040001C8 RID: 456
	public bool m_fleeIfHurtWhenTargetCantBeReached = true;

	// Token: 0x040001C9 RID: 457
	public float m_fleeUnreachableSinceAttacking = 30f;

	// Token: 0x040001CA RID: 458
	public float m_fleeUnreachableSinceHurt = 20f;

	// Token: 0x040001CB RID: 459
	public bool m_fleeIfNotAlerted;

	// Token: 0x040001CC RID: 460
	public float m_fleeIfLowHealth;

	// Token: 0x040001CD RID: 461
	public float m_fleeTimeSinceHurt = 20f;

	// Token: 0x040001CE RID: 462
	public bool m_fleeInLava = true;

	// Token: 0x040001CF RID: 463
	public float m_fleePheromoneMin = 3f;

	// Token: 0x040001D0 RID: 464
	public float m_fleePheromoneMax = 8f;

	// Token: 0x040001D1 RID: 465
	public bool m_circulateWhileCharging;

	// Token: 0x040001D2 RID: 466
	public bool m_circulateWhileChargingFlying;

	// Token: 0x040001D3 RID: 467
	public bool m_enableHuntPlayer;

	// Token: 0x040001D4 RID: 468
	public bool m_attackPlayerObjects = true;

	// Token: 0x040001D5 RID: 469
	public int m_privateAreaTriggerTreshold = 4;

	// Token: 0x040001D6 RID: 470
	public float m_interceptTimeMax;

	// Token: 0x040001D7 RID: 471
	public float m_interceptTimeMin;

	// Token: 0x040001D8 RID: 472
	public float m_maxChaseDistance;

	// Token: 0x040001D9 RID: 473
	public float m_minAttackInterval;

	// Token: 0x040001DA RID: 474
	[Header("Circle target")]
	public float m_circleTargetInterval;

	// Token: 0x040001DB RID: 475
	public float m_circleTargetDuration = 5f;

	// Token: 0x040001DC RID: 476
	public float m_circleTargetDistance = 10f;

	// Token: 0x040001DD RID: 477
	[Header("Sleep")]
	public bool m_sleeping;

	// Token: 0x040001DE RID: 478
	public float m_wakeupRange = 5f;

	// Token: 0x040001DF RID: 479
	public bool m_noiseWakeup;

	// Token: 0x040001E0 RID: 480
	public float m_maxNoiseWakeupRange = 50f;

	// Token: 0x040001E1 RID: 481
	public EffectList m_wakeupEffects = new EffectList();

	// Token: 0x040001E2 RID: 482
	public float m_wakeUpDelayMin;

	// Token: 0x040001E3 RID: 483
	public float m_wakeUpDelayMax;

	// Token: 0x040001E4 RID: 484
	[Header("Other")]
	public bool m_avoidLand;

	// Token: 0x040001E5 RID: 485
	[Header("Consume items")]
	public List<ItemDrop> m_consumeItems;

	// Token: 0x040001E6 RID: 486
	public float m_consumeRange = 2f;

	// Token: 0x040001E7 RID: 487
	public float m_consumeSearchRange = 5f;

	// Token: 0x040001E8 RID: 488
	public float m_consumeSearchInterval = 10f;

	// Token: 0x040001E9 RID: 489
	private ItemDrop m_consumeTarget;

	// Token: 0x040001EA RID: 490
	private float m_consumeSearchTimer;

	// Token: 0x040001EB RID: 491
	private static int m_itemMask = 0;

	// Token: 0x040001EC RID: 492
	private bool m_despawnInDay;

	// Token: 0x040001ED RID: 493
	private bool m_eventCreature;

	// Token: 0x040001EE RID: 494
	private Character m_targetCreature;

	// Token: 0x040001EF RID: 495
	private Vector3 m_lastKnownTargetPos = Vector3.zero;

	// Token: 0x040001F0 RID: 496
	private bool m_beenAtLastPos;

	// Token: 0x040001F1 RID: 497
	private StaticTarget m_targetStatic;

	// Token: 0x040001F2 RID: 498
	private float m_timeSinceAttacking;

	// Token: 0x040001F3 RID: 499
	private float m_timeSinceSensedTargetCreature;

	// Token: 0x040001F4 RID: 500
	private float m_updateTargetTimer;

	// Token: 0x040001F5 RID: 501
	private float m_updateWeaponTimer;

	// Token: 0x040001F6 RID: 502
	private float m_interceptTime;

	// Token: 0x040001F7 RID: 503
	private float m_sleepDelay = 0.5f;

	// Token: 0x040001F8 RID: 504
	private float m_pauseTimer;

	// Token: 0x040001F9 RID: 505
	private float m_sleepTimer;

	// Token: 0x040001FA RID: 506
	private float m_unableToAttackTargetTimer;

	// Token: 0x040001FB RID: 507
	private GameObject m_follow;

	// Token: 0x040001FC RID: 508
	private int m_privateAreaAttacks;

	// Token: 0x040001FD RID: 509
	private static readonly int s_sleeping = ZSyncAnimation.GetHash("sleeping");
}
