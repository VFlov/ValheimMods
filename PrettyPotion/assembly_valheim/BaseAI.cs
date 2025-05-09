using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200000C RID: 12
public class BaseAI : MonoBehaviour, IUpdateAI
{
	// Token: 0x06000077 RID: 119 RVA: 0x00008520 File Offset: 0x00006720
	protected virtual void Awake()
	{
		BaseAI.m_instances.Add(this);
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_character = base.GetComponent<Character>();
		this.m_animator = base.GetComponent<ZSyncAnimation>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_tamable = base.GetComponent<Tameable>();
		if (BaseAI.m_solidRayMask == 0)
		{
			BaseAI.m_solidRayMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"terrain",
				"vehicle"
			});
			BaseAI.m_viewBlockMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"terrain",
				"viewblock",
				"vehicle"
			});
			BaseAI.m_monsterTargetRayMask = LayerMask.GetMask(new string[]
			{
				"piece",
				"piece_nonsolid",
				"Default",
				"static_solid",
				"Default_small",
				"vehicle"
			});
		}
		Character character = this.m_character;
		character.m_onDamaged = (Action<float, Character>)Delegate.Combine(character.m_onDamaged, new Action<float, Character>(this.OnDamaged));
		Character character2 = this.m_character;
		character2.m_onDeath = (Action)Delegate.Combine(character2.m_onDeath, new Action(this.OnDeath));
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
			if (!string.IsNullOrEmpty(this.m_spawnMessage))
			{
				MessageHud.instance.MessageAll(MessageHud.MessageType.Center, this.m_spawnMessage);
			}
		}
		this.m_randomMoveUpdateTimer = UnityEngine.Random.Range(0f, this.m_randomMoveInterval);
		this.m_nview.Register("Alert", new Action<long>(this.RPC_Alert));
		this.m_nview.Register<Vector3, float, ZDOID>("OnNearProjectileHit", new Action<long, Vector3, float, ZDOID>(this.RPC_OnNearProjectileHit));
		this.m_nview.Register<bool, int>("SetAggravated", new Action<long, bool, int>(this.RPC_SetAggravated));
		this.m_huntPlayer = this.m_nview.GetZDO().GetBool(ZDOVars.s_huntPlayer, this.m_huntPlayer);
		this.m_spawnPoint = this.m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, this.m_spawnPoint);
		}
		base.InvokeRepeating("DoIdleSound", this.m_idleSoundInterval, this.m_idleSoundInterval);
	}

	// Token: 0x06000078 RID: 120 RVA: 0x000087F2 File Offset: 0x000069F2
	private void OnDestroy()
	{
		BaseAI.m_instances.Remove(this);
	}

	// Token: 0x06000079 RID: 121 RVA: 0x00008800 File Offset: 0x00006A00
	protected virtual void OnEnable()
	{
		BaseAI.Instances.Add(this);
		BaseAI.BaseAIInstances.Add(this);
	}

	// Token: 0x0600007A RID: 122 RVA: 0x00008818 File Offset: 0x00006A18
	protected virtual void OnDisable()
	{
		BaseAI.Instances.Remove(this);
		BaseAI.BaseAIInstances.Remove(this);
	}

	// Token: 0x0600007B RID: 123 RVA: 0x00008832 File Offset: 0x00006A32
	public void SetPatrolPoint()
	{
		this.SetPatrolPoint(base.transform.position);
	}

	// Token: 0x0600007C RID: 124 RVA: 0x00008845 File Offset: 0x00006A45
	private void SetPatrolPoint(Vector3 point)
	{
		this.m_patrol = true;
		this.m_patrolPoint = point;
		this.m_nview.GetZDO().Set(ZDOVars.s_patrolPoint, point);
		this.m_nview.GetZDO().Set(ZDOVars.s_patrol, true);
	}

	// Token: 0x0600007D RID: 125 RVA: 0x00008881 File Offset: 0x00006A81
	public void ResetPatrolPoint()
	{
		this.m_patrol = false;
		this.m_nview.GetZDO().Set(ZDOVars.s_patrol, false);
	}

	// Token: 0x0600007E RID: 126 RVA: 0x000088A0 File Offset: 0x00006AA0
	protected bool GetPatrolPoint(out Vector3 point)
	{
		if (Time.time - this.m_patrolPointUpdateTime > 1f)
		{
			this.m_patrolPointUpdateTime = Time.time;
			this.m_patrol = this.m_nview.GetZDO().GetBool(ZDOVars.s_patrol, false);
			if (this.m_patrol)
			{
				this.m_patrolPoint = this.m_nview.GetZDO().GetVec3(ZDOVars.s_patrolPoint, this.m_patrolPoint);
			}
		}
		point = this.m_patrolPoint;
		return this.m_patrol;
	}

	// Token: 0x0600007F RID: 127 RVA: 0x00008924 File Offset: 0x00006B24
	public virtual bool UpdateAI(float dt)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.m_nview.IsOwner())
		{
			this.m_alerted = this.m_nview.GetZDO().GetBool(ZDOVars.s_alert, false);
			return false;
		}
		this.UpdateTakeoffLanding(dt);
		if (this.m_jumpInterval > 0f)
		{
			this.m_jumpTimer += dt;
		}
		if (this.m_randomMoveUpdateTimer > 0f)
		{
			this.m_randomMoveUpdateTimer -= dt;
		}
		this.UpdateRegeneration(dt);
		this.m_timeSinceHurt += dt;
		return true;
	}

	// Token: 0x06000080 RID: 128 RVA: 0x000089C0 File Offset: 0x00006BC0
	private void UpdateRegeneration(float dt)
	{
		this.m_regenTimer += dt;
		if (this.m_regenTimer <= 2f)
		{
			return;
		}
		this.m_regenTimer = 0f;
		if (this.m_tamable && this.m_character.IsTamed() && this.m_tamable.IsHungry())
		{
			return;
		}
		float worldTimeDelta = this.GetWorldTimeDelta();
		float num = this.m_character.GetMaxHealth() / 3600f;
		this.m_character.Heal(num * worldTimeDelta, this.m_tamable && this.m_character.IsTamed());
	}

	// Token: 0x06000081 RID: 129 RVA: 0x00008A5E File Offset: 0x00006C5E
	protected bool IsTakingOff()
	{
		return this.m_randomFly && this.m_character.IsFlying() && this.m_randomFlyTimer < this.m_takeoffTime;
	}

	// Token: 0x06000082 RID: 130 RVA: 0x00008A88 File Offset: 0x00006C88
	private void UpdateTakeoffLanding(float dt)
	{
		if (!this.m_randomFly)
		{
			return;
		}
		this.m_randomFlyTimer += dt;
		if (this.m_character.InAttack() || this.m_character.IsStaggering())
		{
			return;
		}
		if (this.m_character.IsFlying())
		{
			if (this.m_randomFlyTimer > this.m_airDuration && this.GetAltitude() < this.m_maxLandAltitude)
			{
				this.m_randomFlyTimer = 0f;
				if (UnityEngine.Random.value <= this.m_chanceToLand)
				{
					this.m_character.Land();
					return;
				}
			}
		}
		else if (this.m_randomFlyTimer > this.m_groundDuration)
		{
			this.m_randomFlyTimer = 0f;
			if (UnityEngine.Random.value <= this.m_chanceToTakeoff)
			{
				this.m_character.TakeOff();
			}
		}
	}

	// Token: 0x06000083 RID: 131 RVA: 0x00008B48 File Offset: 0x00006D48
	private float GetWorldTimeDelta()
	{
		DateTime time = ZNet.instance.GetTime();
		long @long = this.m_nview.GetZDO().GetLong(ZDOVars.s_worldTimeHash, 0L);
		if (@long == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_worldTimeHash, time.Ticks);
			return 0f;
		}
		DateTime d = new DateTime(@long);
		TimeSpan timeSpan = time - d;
		this.m_nview.GetZDO().Set(ZDOVars.s_worldTimeHash, time.Ticks);
		return (float)timeSpan.TotalSeconds;
	}

	// Token: 0x06000084 RID: 132 RVA: 0x00008BD4 File Offset: 0x00006DD4
	public TimeSpan GetTimeSinceSpawned()
	{
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return TimeSpan.Zero;
		}
		long num = this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L);
		if (num == 0L)
		{
			num = ZNet.instance.GetTime().Ticks;
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, num);
		}
		DateTime d = new DateTime(num);
		return ZNet.instance.GetTime() - d;
	}

	// Token: 0x06000085 RID: 133 RVA: 0x00008C5D File Offset: 0x00006E5D
	private void DoIdleSound()
	{
		if (this.IsSleeping())
		{
			return;
		}
		if (UnityEngine.Random.value > this.m_idleSoundChance)
		{
			return;
		}
		this.m_idleSound.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x06000086 RID: 134 RVA: 0x00008C9C File Offset: 0x00006E9C
	protected void Follow(GameObject go, float dt)
	{
		float num = Vector3.Distance(go.transform.position, base.transform.position);
		bool run = num > 10f;
		if (num < 3f)
		{
			this.StopMoving();
			return;
		}
		this.MoveTo(dt, go.transform.position, 0f, run);
	}

	// Token: 0x06000087 RID: 135 RVA: 0x00008CF4 File Offset: 0x00006EF4
	protected void MoveToWater(float dt, float maxRange)
	{
		float num = this.m_haveWaterPosition ? 2f : 0.5f;
		if (Time.time - this.m_lastMoveToWaterUpdate > num)
		{
			this.m_lastMoveToWaterUpdate = Time.time;
			Vector3 vector = base.transform.position;
			for (int i = 0; i < 10; i++)
			{
				Vector3 b = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(4f, maxRange);
				Vector3 vector2 = base.transform.position + b;
				vector2.y = ZoneSystem.instance.GetSolidHeight(vector2);
				if (vector2.y < vector.y)
				{
					vector = vector2;
				}
			}
			if (vector.y < 30f)
			{
				this.m_moveToWaterPosition = vector;
				this.m_haveWaterPosition = true;
			}
			else
			{
				this.m_haveWaterPosition = false;
			}
		}
		if (this.m_haveWaterPosition)
		{
			this.MoveTowards(this.m_moveToWaterPosition - base.transform.position, true);
		}
	}

	// Token: 0x06000088 RID: 136 RVA: 0x00008E04 File Offset: 0x00007004
	protected void MoveAwayAndDespawn(float dt, bool run)
	{
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 40f);
		if (closestPlayer != null)
		{
			Vector3 normalized = (closestPlayer.transform.position - base.transform.position).normalized;
			this.MoveTo(dt, base.transform.position - normalized * 5f, 0f, run);
			return;
		}
		this.m_nview.Destroy();
	}

	// Token: 0x06000089 RID: 137 RVA: 0x00008E8C File Offset: 0x0000708C
	protected void IdleMovement(float dt)
	{
		Vector3 centerPoint = (this.m_character.IsTamed() || this.HuntPlayer()) ? base.transform.position : this.m_spawnPoint;
		Vector3 vector;
		if (this.GetPatrolPoint(out vector))
		{
			centerPoint = vector;
		}
		this.RandomMovement(dt, centerPoint, true);
	}

	// Token: 0x0600008A RID: 138 RVA: 0x00008ED8 File Offset: 0x000070D8
	protected void RandomMovement(float dt, Vector3 centerPoint, bool snapToGround = false)
	{
		if (this.m_randomMoveUpdateTimer <= 0f)
		{
			float y;
			if (snapToGround && ZoneSystem.instance.GetSolidHeight(this.m_randomMoveTarget, out y, 1000))
			{
				centerPoint.y = y;
			}
			if (Utils.DistanceXZ(centerPoint, base.transform.position) > this.m_randomMoveRange * 2f)
			{
				Vector3 vector = centerPoint - base.transform.position;
				vector.y = 0f;
				vector.Normalize();
				vector = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(-30, 30), 0f) * vector;
				this.m_randomMoveTarget = base.transform.position + vector * this.m_randomMoveRange * 2f;
			}
			else
			{
				Vector3 b = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * base.transform.forward * UnityEngine.Random.Range(this.m_randomMoveRange * 0.7f, this.m_randomMoveRange);
				this.m_randomMoveTarget = centerPoint + b;
			}
			if (this.m_character.IsFlying())
			{
				this.m_randomMoveTarget.y = Mathf.Max(this.m_flyAbsMinAltitude, this.m_randomMoveTarget.y + UnityEngine.Random.Range(this.m_flyAltitudeMin, this.m_flyAltitudeMax));
			}
			if (!this.IsValidRandomMovePoint(this.m_randomMoveTarget))
			{
				return;
			}
			this.m_reachedRandomMoveTarget = false;
			this.m_randomMoveUpdateTimer = UnityEngine.Random.Range(this.m_randomMoveInterval, this.m_randomMoveInterval + this.m_randomMoveInterval / 2f);
			if ((this.m_avoidWater && this.m_character.IsSwimming()) || (this.m_avoidLava && this.m_character.InLava()))
			{
				this.m_randomMoveUpdateTimer /= 4f;
			}
		}
		if (!this.m_reachedRandomMoveTarget)
		{
			bool flag = this.IsAlerted() || Utils.DistanceXZ(base.transform.position, centerPoint) > this.m_randomMoveRange * 2f;
			if (this.MoveTo(dt, this.m_randomMoveTarget, 0f, flag))
			{
				this.m_reachedRandomMoveTarget = true;
				if (flag)
				{
					this.m_randomMoveUpdateTimer = 0f;
					return;
				}
			}
		}
		else
		{
			this.StopMoving();
		}
	}

	// Token: 0x0600008B RID: 139 RVA: 0x0000911A File Offset: 0x0000731A
	public void ResetRandomMovement()
	{
		this.m_reachedRandomMoveTarget = true;
		this.m_randomMoveUpdateTimer = UnityEngine.Random.Range(this.m_randomMoveInterval, this.m_randomMoveInterval + this.m_randomMoveInterval / 2f);
	}

	// Token: 0x0600008C RID: 140 RVA: 0x00009148 File Offset: 0x00007348
	protected bool Flee(float dt, Vector3 from)
	{
		float time = Time.time;
		if (time - this.m_fleeTargetUpdateTime > this.m_fleeInterval)
		{
			this.m_lastFlee = time;
			this.m_fleeTargetUpdateTime = time;
			Vector3 point = -(from - base.transform.position);
			point.y = 0f;
			point.Normalize();
			bool flag = false;
			for (int i = 0; i < 10; i++)
			{
				this.m_fleeTarget = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(-this.m_fleeAngle, this.m_fleeAngle), 0f) * point * this.m_fleeRange;
				if (this.HavePath(this.m_fleeTarget) && (!this.m_avoidWater || this.m_character.IsSwimming() || ZoneSystem.instance.GetSolidHeight(this.m_fleeTarget) >= 30f) && (!this.m_avoidLavaFlee || !ZoneSystem.instance.IsLava(this.m_fleeTarget, false)))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				this.m_fleeTarget = base.transform.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * this.m_fleeRange;
			}
		}
		return this.MoveTo(dt, this.m_fleeTarget, 1f, this.IsAlerted());
	}

	// Token: 0x0600008D RID: 141 RVA: 0x000092C0 File Offset: 0x000074C0
	protected bool AvoidFire(float dt, Character moveToTarget, bool superAfraid)
	{
		if (this.m_character.IsTamed())
		{
			return false;
		}
		if (superAfraid)
		{
			EffectArea effectArea = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Fire, 3f);
			if (effectArea)
			{
				this.m_nearFireTime = Time.time;
				this.m_nearFireArea = effectArea;
			}
			if (Time.time - this.m_nearFireTime < 6f && this.m_nearFireArea)
			{
				this.SetAlerted(true);
				this.Flee(dt, this.m_nearFireArea.transform.position);
				return true;
			}
		}
		else
		{
			EffectArea effectArea2 = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Fire, 3f);
			if (effectArea2)
			{
				if (moveToTarget != null && EffectArea.IsPointInsideArea(moveToTarget.transform.position, EffectArea.Type.Fire, 0f))
				{
					this.RandomMovementArroundPoint(dt, effectArea2.transform.position, effectArea2.GetRadius() + 3f + 1f, this.IsAlerted());
					return true;
				}
				this.RandomMovementArroundPoint(dt, effectArea2.transform.position, (effectArea2.GetRadius() + 3f) * 1.5f, this.IsAlerted());
				return true;
			}
		}
		return false;
	}

	// Token: 0x0600008E RID: 142 RVA: 0x000093F8 File Offset: 0x000075F8
	protected void RandomMovementArroundPoint(float dt, Vector3 point, float distance, bool run)
	{
		this.ChargeStop();
		float time = Time.time;
		if (time - this.aroundPointUpdateTime > this.m_randomCircleInterval)
		{
			this.aroundPointUpdateTime = time;
			Vector3 point2 = base.transform.position - point;
			point2.y = 0f;
			point2.Normalize();
			float num;
			if (Vector3.Distance(base.transform.position, point) < distance / 2f)
			{
				num = (float)(((double)UnityEngine.Random.value > 0.5) ? 90 : -90);
			}
			else
			{
				num = (float)(((double)UnityEngine.Random.value > 0.5) ? 40 : -40);
			}
			Vector3 a = Quaternion.Euler(0f, num, 0f) * point2;
			this.arroundPointTarget = point + a * distance;
			if (Vector3.Dot(base.transform.forward, this.arroundPointTarget - base.transform.position) < 0f)
			{
				a = Quaternion.Euler(0f, -num, 0f) * point2;
				this.arroundPointTarget = point + a * distance;
				if (this.m_serpentMovement && Vector3.Distance(point, base.transform.position) > distance / 2f && Vector3.Dot(base.transform.forward, this.arroundPointTarget - base.transform.position) < 0f)
				{
					this.arroundPointTarget = point - a * distance;
				}
			}
			if (this.m_character.IsFlying())
			{
				this.arroundPointTarget.y = this.arroundPointTarget.y + UnityEngine.Random.Range(this.m_flyAltitudeMin, this.m_flyAltitudeMax);
			}
		}
		if (this.MoveTo(dt, this.arroundPointTarget, 0f, run))
		{
			if (run)
			{
				this.aroundPointUpdateTime = 0f;
			}
			if (!this.m_serpentMovement && !run)
			{
				this.LookAt(point);
			}
		}
	}

	// Token: 0x0600008F RID: 143 RVA: 0x000095F4 File Offset: 0x000077F4
	private bool GetSolidHeight(Vector3 p, float maxUp, float maxDown, out float height)
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up * maxUp, Vector3.down, out raycastHit, maxDown, BaseAI.m_solidRayMask))
		{
			height = raycastHit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	// Token: 0x06000090 RID: 144 RVA: 0x00009640 File Offset: 0x00007840
	protected bool IsValidRandomMovePoint(Vector3 point)
	{
		if (this.m_character.IsFlying())
		{
			return true;
		}
		float num;
		if (this.m_avoidWater && this.GetSolidHeight(point, 20f, 100f, out num))
		{
			if (this.m_character.IsSwimming())
			{
				float num2;
				if (this.GetSolidHeight(base.transform.position, 20f, 100f, out num2) && num < num2)
				{
					return false;
				}
			}
			else if (num < 30f)
			{
				return false;
			}
		}
		return (!this.m_avoidLava || !ZoneSystem.instance.IsLava(point, false)) && ((!this.m_afraidOfFire && !this.m_avoidFire) || !EffectArea.IsPointInsideArea(point, EffectArea.Type.Fire, 0f));
	}

	// Token: 0x06000091 RID: 145 RVA: 0x000096F3 File Offset: 0x000078F3
	protected virtual void OnDamaged(float damage, Character attacker)
	{
		this.m_timeSinceHurt = 0f;
	}

	// Token: 0x06000092 RID: 146 RVA: 0x00009700 File Offset: 0x00007900
	protected virtual void OnDeath()
	{
		if (!string.IsNullOrEmpty(this.m_deathMessage))
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, this.m_deathMessage);
		}
	}

	// Token: 0x06000093 RID: 147 RVA: 0x00009720 File Offset: 0x00007920
	public bool CanSenseTarget(Character target)
	{
		return this.CanSenseTarget(target, this.m_passiveAggresive);
	}

	// Token: 0x06000094 RID: 148 RVA: 0x00009730 File Offset: 0x00007930
	public bool CanSenseTarget(Character target, bool passiveAggresive)
	{
		return BaseAI.CanSenseTarget(base.transform, this.m_character.m_eye.position, this.m_hearRange, this.m_viewRange, this.m_viewAngle, this.IsAlerted(), this.m_mistVision, target, passiveAggresive, this.m_character.IsTamed());
	}

	// Token: 0x06000095 RID: 149 RVA: 0x00009784 File Offset: 0x00007984
	public static bool CanSenseTarget(Transform me, Vector3 eyePoint, float hearRange, float viewRange, float viewAngle, bool alerted, bool mistVision, Character target, bool passiveAggresive, bool isTamed)
	{
		return (passiveAggresive || !ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs) || (isTamed && target.GetBaseAI().IsAlerted())) && (BaseAI.CanHearTarget(me, hearRange, target) || BaseAI.CanSeeTarget(me, eyePoint, viewRange, viewAngle, alerted, mistVision, target));
	}

	// Token: 0x06000096 RID: 150 RVA: 0x000097D9 File Offset: 0x000079D9
	public bool CanHearTarget(Character target)
	{
		return BaseAI.CanHearTarget(base.transform, this.m_hearRange, target);
	}

	// Token: 0x06000097 RID: 151 RVA: 0x000097F0 File Offset: 0x000079F0
	public static bool CanHearTarget(Transform me, float hearRange, Character target)
	{
		if (target.IsPlayer())
		{
			Player player = target as Player;
			if (player.InDebugFlyMode() || player.InGhostMode())
			{
				return false;
			}
		}
		float num = Vector3.Distance(target.transform.position, me.position);
		if (Character.InInterior(me))
		{
			hearRange = Mathf.Min(12f, hearRange);
		}
		return num <= hearRange && num < target.GetNoiseRange();
	}

	// Token: 0x06000098 RID: 152 RVA: 0x0000985C File Offset: 0x00007A5C
	public bool CanSeeTarget(Character target)
	{
		return BaseAI.CanSeeTarget(base.transform, this.m_character.m_eye.position, this.m_viewRange, this.m_viewAngle, this.IsAlerted(), this.m_mistVision, target);
	}

	// Token: 0x06000099 RID: 153 RVA: 0x00009894 File Offset: 0x00007A94
	public static bool CanSeeTarget(Transform me, Vector3 eyePoint, float viewRange, float viewAngle, bool alerted, bool mistVision, Character target)
	{
		if (target == null || me == null)
		{
			return false;
		}
		if (target.IsPlayer())
		{
			Player player = target as Player;
			if (player.InDebugFlyMode() || player.InGhostMode())
			{
				return false;
			}
		}
		float num = Vector3.Distance(target.transform.position, me.position);
		if (num > viewRange)
		{
			return false;
		}
		float num2 = num / viewRange;
		float stealthFactor = target.GetStealthFactor();
		float num3 = viewRange * stealthFactor;
		if (num > num3)
		{
			return false;
		}
		if (!alerted && Vector3.Angle(target.transform.position - me.position, me.forward) > viewAngle)
		{
			return false;
		}
		Vector3 vector = target.IsCrouching() ? target.GetCenterPoint() : target.m_eye.position;
		Vector3 vector2 = vector - eyePoint;
		return !Physics.Raycast(eyePoint, vector2.normalized, vector2.magnitude, BaseAI.m_viewBlockMask) && (mistVision || !ParticleMist.IsMistBlocked(eyePoint, vector));
	}

	// Token: 0x0600009A RID: 154 RVA: 0x00009990 File Offset: 0x00007B90
	protected bool CanSeeTarget(StaticTarget target)
	{
		if (target == null)
		{
			return false;
		}
		Vector3 center = target.GetCenter();
		if (Vector3.Distance(center, base.transform.position) > this.m_viewRange)
		{
			return false;
		}
		Vector3 rhs = center - this.m_character.m_eye.position;
		if (this.m_viewRange > 0f && !this.IsAlerted() && Vector3.Dot(base.transform.forward, rhs) < 0f)
		{
			return false;
		}
		List<Collider> allColliders = target.GetAllColliders();
		int num = Physics.RaycastNonAlloc(this.m_character.m_eye.position, rhs.normalized, BaseAI.s_tempRaycastHits, rhs.magnitude, BaseAI.m_viewBlockMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = BaseAI.s_tempRaycastHits[i];
			if (!allColliders.Contains(raycastHit.collider))
			{
				return false;
			}
		}
		return this.m_mistVision || !ParticleMist.IsMistBlocked(this.m_character.m_eye.position, center);
	}

	// Token: 0x0600009B RID: 155 RVA: 0x00009A98 File Offset: 0x00007C98
	private void MoveTowardsSwoop(Vector3 dir, bool run, float distance)
	{
		dir = dir.normalized;
		float num = Mathf.Clamp01(Vector3.Dot(dir, this.m_character.transform.forward));
		num *= num;
		float num2 = Mathf.Clamp01(distance / this.m_serpentTurnRadius);
		float num3 = 1f - (1f - num2) * (1f - num);
		num3 = num3 * 0.9f + 0.1f;
		Vector3 moveDir = base.transform.forward * num3;
		this.LookTowards(dir);
		this.m_character.SetMoveDir(moveDir);
		this.m_character.SetRun(run);
	}

	// Token: 0x0600009C RID: 156 RVA: 0x00009B34 File Offset: 0x00007D34
	public void MoveTowards(Vector3 dir, bool run)
	{
		dir = dir.normalized;
		this.LookTowards(dir);
		if (this.m_smoothMovement)
		{
			float num = Vector3.Angle(new Vector3(dir.x, 0f, dir.z), base.transform.forward);
			float d = 1f - Mathf.Clamp01(num / this.m_moveMinAngle);
			Vector3 moveDir = base.transform.forward * d;
			moveDir.y = dir.y;
			this.m_character.SetMoveDir(moveDir);
			this.m_character.SetRun(run);
			if (this.m_jumpInterval > 0f && this.m_jumpTimer >= this.m_jumpInterval)
			{
				this.m_jumpTimer = 0f;
				this.m_character.Jump(false);
				return;
			}
		}
		else if (this.IsLookingTowards(dir, this.m_moveMinAngle))
		{
			this.m_character.SetMoveDir(dir);
			this.m_character.SetRun(run);
			if (this.m_jumpInterval > 0f && this.m_jumpTimer >= this.m_jumpInterval)
			{
				this.m_jumpTimer = 0f;
				this.m_character.Jump(false);
				return;
			}
		}
		else
		{
			this.StopMoving();
		}
	}

	// Token: 0x0600009D RID: 157 RVA: 0x00009C64 File Offset: 0x00007E64
	protected void LookAt(Vector3 point)
	{
		Vector3 vector = point - this.m_character.m_eye.position;
		if (Utils.LengthXZ(vector) < 0.01f)
		{
			return;
		}
		vector.Normalize();
		this.LookTowards(vector);
	}

	// Token: 0x0600009E RID: 158 RVA: 0x00009CA4 File Offset: 0x00007EA4
	public void LookTowards(Vector3 dir)
	{
		this.m_character.SetLookDir(dir, 0f);
	}

	// Token: 0x0600009F RID: 159 RVA: 0x00009CB8 File Offset: 0x00007EB8
	protected bool IsLookingAt(Vector3 point, float minAngle, bool inverted = false)
	{
		return this.IsLookingTowards((point - base.transform.position).normalized, minAngle) ^ inverted;
	}

	// Token: 0x060000A0 RID: 160 RVA: 0x00009CE8 File Offset: 0x00007EE8
	public bool IsLookingTowards(Vector3 dir, float minAngle)
	{
		dir.y = 0f;
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		return Vector3.Angle(dir, forward) < minAngle;
	}

	// Token: 0x060000A1 RID: 161 RVA: 0x00009D23 File Offset: 0x00007F23
	public void StopMoving()
	{
		this.m_character.SetMoveDir(Vector3.zero);
	}

	// Token: 0x060000A2 RID: 162 RVA: 0x00009D35 File Offset: 0x00007F35
	protected bool HavePath(Vector3 target)
	{
		return this.m_character.IsFlying() || Pathfinding.instance.HavePath(base.transform.position, target, this.m_pathAgentType);
	}

	// Token: 0x060000A3 RID: 163 RVA: 0x00009D64 File Offset: 0x00007F64
	protected bool FindPath(Vector3 target)
	{
		float time = Time.time;
		float num = time - this.m_lastFindPathTime;
		if (num < 1f)
		{
			return this.m_lastFindPathResult;
		}
		if (Vector3.Distance(target, this.m_lastFindPathTarget) < 1f && num < 5f)
		{
			return this.m_lastFindPathResult;
		}
		this.m_lastFindPathTarget = target;
		this.m_lastFindPathTime = time;
		this.m_lastFindPathResult = Pathfinding.instance.GetPath(base.transform.position, target, this.m_path, this.m_pathAgentType, false, true, false);
		return this.m_lastFindPathResult;
	}

	// Token: 0x060000A4 RID: 164 RVA: 0x00009DF0 File Offset: 0x00007FF0
	protected bool FoundPath()
	{
		return this.m_lastFindPathResult;
	}

	// Token: 0x060000A5 RID: 165 RVA: 0x00009DF8 File Offset: 0x00007FF8
	protected bool MoveTo(float dt, Vector3 point, float dist, bool run)
	{
		if (this.m_character.m_flying)
		{
			dist = Mathf.Max(dist, 1f);
			float num;
			if (this.GetSolidHeight(point, 0f, this.m_flyAltitudeMin * 2f, out num))
			{
				point.y = Mathf.Max(point.y, num + this.m_flyAltitudeMin);
			}
			return this.MoveAndAvoid(dt, point, dist, run);
		}
		float num2 = run ? 1f : 0.5f;
		if (this.m_serpentMovement)
		{
			num2 = 3f;
		}
		if (Utils.DistanceXZ(point, base.transform.position) < Mathf.Max(dist, num2))
		{
			this.StopMoving();
			return true;
		}
		if (!this.FindPath(point))
		{
			this.StopMoving();
			return true;
		}
		if (this.m_path.Count == 0)
		{
			this.StopMoving();
			return true;
		}
		Vector3 vector = this.m_path[0];
		if (Utils.DistanceXZ(vector, base.transform.position) < num2)
		{
			this.m_path.RemoveAt(0);
			if (this.m_path.Count == 0)
			{
				this.StopMoving();
				return true;
			}
		}
		else if (this.m_serpentMovement)
		{
			float distance = Vector3.Distance(vector, base.transform.position);
			Vector3 normalized = (vector - base.transform.position).normalized;
			this.MoveTowardsSwoop(normalized, run, distance);
		}
		else
		{
			Vector3 normalized2 = (vector - base.transform.position).normalized;
			this.MoveTowards(normalized2, run);
		}
		return false;
	}

	// Token: 0x060000A6 RID: 166 RVA: 0x00009F74 File Offset: 0x00008174
	protected bool MoveAndAvoid(float dt, Vector3 point, float dist, bool run)
	{
		Vector3 vector = point - base.transform.position;
		if (this.m_character.IsFlying())
		{
			if (vector.magnitude < dist)
			{
				this.StopMoving();
				return true;
			}
		}
		else
		{
			vector.y = 0f;
			if (vector.magnitude < dist)
			{
				this.StopMoving();
				return true;
			}
		}
		vector.Normalize();
		float radius = this.m_character.GetRadius();
		float num = radius + 1f;
		if (!this.m_character.InAttack())
		{
			this.m_getOutOfCornerTimer -= dt;
			if (this.m_getOutOfCornerTimer > 0f)
			{
				Vector3 dir = Quaternion.Euler(0f, this.m_getOutOfCornerAngle, 0f) * -vector;
				this.MoveTowards(dir, run);
				return false;
			}
			this.m_stuckTimer += Time.fixedDeltaTime;
			if (this.m_stuckTimer > 1.5f)
			{
				if (Vector3.Distance(base.transform.position, this.m_lastPosition) < 0.2f)
				{
					this.m_getOutOfCornerTimer = 4f;
					this.m_getOutOfCornerAngle = UnityEngine.Random.Range(-20f, 20f);
					this.m_stuckTimer = 0f;
					return false;
				}
				this.m_stuckTimer = 0f;
				this.m_lastPosition = base.transform.position;
			}
		}
		if (this.CanMove(vector, radius, num))
		{
			this.MoveTowards(vector, run);
		}
		else
		{
			Vector3 forward = base.transform.forward;
			if (this.m_character.IsFlying())
			{
				forward.y = 0.2f;
				forward.Normalize();
			}
			Vector3 b = base.transform.right * radius * 0.75f;
			float num2 = num * 1.5f;
			Vector3 centerPoint = this.m_character.GetCenterPoint();
			float num3 = this.Raycast(centerPoint - b, forward, num2, 0.1f);
			float num4 = this.Raycast(centerPoint + b, forward, num2, 0.1f);
			if (num3 >= num2 && num4 >= num2)
			{
				this.MoveTowards(forward, run);
			}
			else
			{
				Vector3 dir2 = Quaternion.Euler(0f, -20f, 0f) * forward;
				Vector3 dir3 = Quaternion.Euler(0f, 20f, 0f) * forward;
				if (num3 > num4)
				{
					this.MoveTowards(dir2, run);
				}
				else
				{
					this.MoveTowards(dir3, run);
				}
			}
		}
		return false;
	}

	// Token: 0x060000A7 RID: 167 RVA: 0x0000A1E4 File Offset: 0x000083E4
	private bool CanMove(Vector3 dir, float checkRadius, float distance)
	{
		Vector3 centerPoint = this.m_character.GetCenterPoint();
		Vector3 right = base.transform.right;
		return this.Raycast(centerPoint, dir, distance, 0.1f) >= distance && this.Raycast(centerPoint - right * (checkRadius - 0.1f), dir, distance, 0.1f) >= distance && this.Raycast(centerPoint + right * (checkRadius - 0.1f), dir, distance, 0.1f) >= distance;
	}

	// Token: 0x060000A8 RID: 168 RVA: 0x0000A268 File Offset: 0x00008468
	public float Raycast(Vector3 p, Vector3 dir, float distance, float radius)
	{
		if (radius == 0f)
		{
			RaycastHit raycastHit;
			if (Physics.Raycast(p, dir, out raycastHit, distance, BaseAI.m_solidRayMask))
			{
				return raycastHit.distance;
			}
			return distance;
		}
		else
		{
			RaycastHit raycastHit2;
			if (Physics.SphereCast(p, radius, dir, out raycastHit2, distance, BaseAI.m_solidRayMask))
			{
				return raycastHit2.distance;
			}
			return distance;
		}
	}

	// Token: 0x060000A9 RID: 169 RVA: 0x0000A2B8 File Offset: 0x000084B8
	public void SetAggravated(bool aggro, BaseAI.AggravatedReason reason)
	{
		if (!this.m_aggravatable)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_aggravated == aggro)
		{
			return;
		}
		this.m_nview.InvokeRPC("SetAggravated", new object[]
		{
			aggro,
			(int)reason
		});
	}

	// Token: 0x060000AA RID: 170 RVA: 0x0000A310 File Offset: 0x00008510
	private void RPC_SetAggravated(long sender, bool aggro, int reason)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_aggravated == aggro)
		{
			return;
		}
		this.m_aggravated = aggro;
		this.m_nview.GetZDO().Set(ZDOVars.s_aggravated, this.m_aggravated);
		if (this.m_onBecameAggravated != null)
		{
			this.m_onBecameAggravated((BaseAI.AggravatedReason)reason);
		}
	}

	// Token: 0x060000AB RID: 171 RVA: 0x0000A36B File Offset: 0x0000856B
	public bool IsAggravatable()
	{
		return this.m_aggravatable;
	}

	// Token: 0x060000AC RID: 172 RVA: 0x0000A374 File Offset: 0x00008574
	public bool IsAggravated()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.m_aggravatable)
		{
			return false;
		}
		if (Time.time - this.m_lastAggravatedCheck > 1f)
		{
			this.m_lastAggravatedCheck = Time.time;
			this.m_aggravated = this.m_nview.GetZDO().GetBool(ZDOVars.s_aggravated, this.m_aggravated);
		}
		return this.m_aggravated;
	}

	// Token: 0x060000AD RID: 173 RVA: 0x0000A3DF File Offset: 0x000085DF
	public bool IsEnemy(Character other)
	{
		return BaseAI.IsEnemy(this.m_character, other);
	}

	// Token: 0x060000AE RID: 174 RVA: 0x0000A3F0 File Offset: 0x000085F0
	public static bool IsEnemy(Character a, Character b)
	{
		if (a == b)
		{
			return false;
		}
		if (!a || !b)
		{
			return false;
		}
		string group = a.GetGroup();
		if (group.Length > 0 && group == b.GetGroup())
		{
			return false;
		}
		Character.Faction faction = a.GetFaction();
		Character.Faction faction2 = b.GetFaction();
		bool flag = a.IsTamed();
		bool flag2 = b.IsTamed();
		bool flag3 = a.GetBaseAI() && a.GetBaseAI().IsAggravated();
		bool flag4 = b.GetBaseAI() && b.GetBaseAI().IsAggravated();
		if (flag || flag2)
		{
			return (!flag || !flag2) && (!flag || faction2 != Character.Faction.Players) && (!flag2 || faction != Character.Faction.Players) && (!flag || faction2 != Character.Faction.Dverger || flag4) && (!flag2 || faction != Character.Faction.Dverger || flag3);
		}
		if ((flag3 || flag4) && ((flag3 && faction2 == Character.Faction.Players) || (flag4 && faction == Character.Faction.Players)))
		{
			return true;
		}
		if (faction == faction2)
		{
			return false;
		}
		switch (faction)
		{
		case Character.Faction.Players:
			return faction2 != Character.Faction.Dverger;
		case Character.Faction.AnimalsVeg:
		case Character.Faction.PlayerSpawned:
			return true;
		case Character.Faction.ForestMonsters:
			return faction2 != Character.Faction.AnimalsVeg && faction2 != Character.Faction.Boss;
		case Character.Faction.Undead:
			return faction2 != Character.Faction.Demon && faction2 != Character.Faction.Boss;
		case Character.Faction.Demon:
			return faction2 != Character.Faction.Undead && faction2 != Character.Faction.Boss;
		case Character.Faction.MountainMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.SeaMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.PlainsMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.Boss:
			return faction2 == Character.Faction.Players || faction2 == Character.Faction.PlayerSpawned;
		case Character.Faction.MistlandsMonsters:
			return faction2 != Character.Faction.AnimalsVeg && faction2 != Character.Faction.Boss;
		case Character.Faction.Dverger:
			return faction2 != Character.Faction.AnimalsVeg && faction2 != Character.Faction.Boss && faction2 > Character.Faction.Players;
		default:
			return false;
		}
	}

	// Token: 0x060000AF RID: 175 RVA: 0x0000A594 File Offset: 0x00008794
	protected StaticTarget FindRandomStaticTarget(float maxDistance)
	{
		float radius = this.m_character.GetRadius();
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, radius + maxDistance, BaseAI.s_tempSphereOverlap);
		if (num == 0)
		{
			return null;
		}
		List<StaticTarget> list = new List<StaticTarget>();
		for (int i = 0; i < num; i++)
		{
			StaticTarget componentInParent = BaseAI.s_tempSphereOverlap[i].GetComponentInParent<StaticTarget>();
			if (!(componentInParent == null) && componentInParent.IsRandomTarget() && this.CanSeeTarget(componentInParent))
			{
				list.Add(componentInParent);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	// Token: 0x060000B0 RID: 176 RVA: 0x0000A62C File Offset: 0x0000882C
	protected StaticTarget FindClosestStaticPriorityTarget()
	{
		float num = (this.m_viewRange > 0f) ? this.m_viewRange : this.m_hearRange;
		int num2 = Physics.OverlapSphereNonAlloc(base.transform.position, num, BaseAI.s_tempSphereOverlap, BaseAI.m_monsterTargetRayMask);
		if (num2 == 0)
		{
			return null;
		}
		StaticTarget result = null;
		float num3 = num;
		for (int i = 0; i < num2; i++)
		{
			StaticTarget componentInParent = BaseAI.s_tempSphereOverlap[i].GetComponentInParent<StaticTarget>();
			if (!(componentInParent == null) && componentInParent.IsPriorityTarget())
			{
				float num4 = Vector3.Distance(base.transform.position, componentInParent.GetCenter());
				if (num4 < num3 && this.CanSeeTarget(componentInParent))
				{
					result = componentInParent;
					num3 = num4;
				}
			}
		}
		return result;
	}

	// Token: 0x060000B1 RID: 177 RVA: 0x0000A6DC File Offset: 0x000088DC
	protected void HaveFriendsInRange(float range, out Character hurtFriend, out Character friend)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		friend = this.HaveFriendInRange(allCharacters, range);
		hurtFriend = this.HaveHurtFriendInRange(allCharacters, range);
	}

	// Token: 0x060000B2 RID: 178 RVA: 0x0000A704 File Offset: 0x00008904
	private Character HaveFriendInRange(List<Character> characters, float range)
	{
		foreach (Character character in characters)
		{
			if (!(character == this.m_character) && !BaseAI.IsEnemy(this.m_character, character) && Vector3.Distance(character.transform.position, base.transform.position) <= range)
			{
				return character;
			}
		}
		return null;
	}

	// Token: 0x060000B3 RID: 179 RVA: 0x0000A78C File Offset: 0x0000898C
	protected Character HaveFriendInRange(float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		return this.HaveFriendInRange(allCharacters, range);
	}

	// Token: 0x060000B4 RID: 180 RVA: 0x0000A7A8 File Offset: 0x000089A8
	private Character HaveHurtFriendInRange(List<Character> characters, float range)
	{
		foreach (Character character in characters)
		{
			if (!BaseAI.IsEnemy(this.m_character, character) && Vector3.Distance(character.transform.position, base.transform.position) <= range && character.GetHealth() < character.GetMaxHealth())
			{
				return character;
			}
		}
		return null;
	}

	// Token: 0x060000B5 RID: 181 RVA: 0x0000A830 File Offset: 0x00008A30
	protected float StandStillDuration(float distanceTreshold)
	{
		if (Vector3.Distance(base.transform.position, this.m_lastMovementCheck) > distanceTreshold)
		{
			this.m_lastMovementCheck = base.transform.position;
			this.m_lastMoveTime = Time.time;
		}
		return Time.time - this.m_lastMoveTime;
	}

	// Token: 0x060000B6 RID: 182 RVA: 0x0000A880 File Offset: 0x00008A80
	protected Character HaveHurtFriendInRange(float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		return this.HaveHurtFriendInRange(allCharacters, range);
	}

	// Token: 0x060000B7 RID: 183 RVA: 0x0000A89C File Offset: 0x00008A9C
	protected Character FindEnemy()
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		Character character = null;
		float num = 99999f;
		foreach (Character character2 in allCharacters)
		{
			if (BaseAI.IsEnemy(this.m_character, character2) && !character2.IsDead() && !character2.m_aiSkipTarget)
			{
				BaseAI baseAI = character2.GetBaseAI();
				if ((!(baseAI != null) || !baseAI.IsSleeping()) && this.CanSenseTarget(character2))
				{
					float num2 = Vector3.Distance(character2.transform.position, base.transform.position);
					if (num2 < num || character == null)
					{
						character = character2;
						num = num2;
					}
				}
			}
		}
		if (!(character == null) || !this.HuntPlayer())
		{
			return character;
		}
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 200f);
		if (closestPlayer && (closestPlayer.InDebugFlyMode() || closestPlayer.InGhostMode()))
		{
			return null;
		}
		return closestPlayer;
	}

	// Token: 0x060000B8 RID: 184 RVA: 0x0000A9B0 File Offset: 0x00008BB0
	public static Character FindClosestCreature(Transform me, Vector3 eyePoint, float hearRange, float viewRange, float viewAngle, bool alerted, bool mistVision, bool passiveAggresive, bool includePlayers = true, bool includeTamed = true, bool includeEnemies = true, List<Character> onlyTargets = null)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		Character character = null;
		float num = 99999f;
		bool flag;
		if (!includeEnemies && ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs))
		{
			WearNTear component = me.GetComponent<WearNTear>();
			if (component != null)
			{
				flag = (component.GetHealthPercentage() == 1f);
				goto IL_3A;
			}
		}
		flag = false;
		IL_3A:
		if (flag)
		{
			return null;
		}
		foreach (Character character2 in allCharacters)
		{
			bool flag2 = character2 is Player;
			if ((includePlayers || !flag2) && (includeEnemies || flag2) && (includeTamed || !character2.IsTamed()))
			{
				if (onlyTargets != null && onlyTargets.Count > 0)
				{
					bool flag3 = false;
					foreach (Character character3 in onlyTargets)
					{
						if (character2.m_name == character3.m_name)
						{
							flag3 = true;
							break;
						}
					}
					if (!flag3)
					{
						continue;
					}
				}
				if (!character2.IsDead())
				{
					BaseAI baseAI = character2.GetBaseAI();
					if ((!(baseAI != null) || !baseAI.IsSleeping()) && BaseAI.CanSenseTarget(me, eyePoint, hearRange, viewRange, viewAngle, alerted, mistVision, character2, passiveAggresive, false))
					{
						float num2 = Vector3.Distance(character2.transform.position, me.position);
						if (num2 < num || character == null)
						{
							character = character2;
							num = num2;
						}
					}
				}
			}
		}
		return character;
	}

	// Token: 0x060000B9 RID: 185 RVA: 0x0000AB5C File Offset: 0x00008D5C
	public void SetHuntPlayer(bool hunt)
	{
		if (this.m_huntPlayer == hunt)
		{
			return;
		}
		this.m_huntPlayer = hunt;
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_huntPlayer, this.m_huntPlayer);
		}
	}

	// Token: 0x060000BA RID: 186 RVA: 0x0000AB97 File Offset: 0x00008D97
	public virtual bool HuntPlayer()
	{
		return this.m_huntPlayer;
	}

	// Token: 0x060000BB RID: 187 RVA: 0x0000ABA0 File Offset: 0x00008DA0
	protected bool HaveAlertedCreatureInRange(float range)
	{
		foreach (BaseAI baseAI in BaseAI.m_instances)
		{
			if (Vector3.Distance(base.transform.position, baseAI.transform.position) < range && baseAI.IsAlerted())
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060000BC RID: 188 RVA: 0x0000AC18 File Offset: 0x00008E18
	public static void DoProjectileHitNoise(Vector3 center, float range, Character attacker)
	{
		foreach (BaseAI baseAI in BaseAI.m_instances)
		{
			if ((!attacker || baseAI.IsEnemy(attacker)) && Vector3.Distance(baseAI.transform.position, center) < range && baseAI.m_nview && baseAI.m_nview.IsValid())
			{
				baseAI.m_nview.InvokeRPC("OnNearProjectileHit", new object[]
				{
					center,
					range,
					attacker ? attacker.GetZDOID() : ZDOID.None
				});
			}
		}
	}

	// Token: 0x060000BD RID: 189 RVA: 0x0000ACF0 File Offset: 0x00008EF0
	protected virtual void RPC_OnNearProjectileHit(long sender, Vector3 center, float range, ZDOID attacker)
	{
		if (!ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs))
		{
			this.Alert();
		}
	}

	// Token: 0x060000BE RID: 190 RVA: 0x0000AD08 File Offset: 0x00008F08
	public void Alert()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.IsAlerted())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.SetAlerted(true);
			return;
		}
		this.m_nview.InvokeRPC("Alert", Array.Empty<object>());
	}

	// Token: 0x060000BF RID: 191 RVA: 0x0000AD56 File Offset: 0x00008F56
	private void RPC_Alert(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.SetAlerted(true);
	}

	// Token: 0x060000C0 RID: 192 RVA: 0x0000AD70 File Offset: 0x00008F70
	protected virtual void SetAlerted(bool alert)
	{
		if (this.m_alerted == alert)
		{
			return;
		}
		this.m_alerted = alert;
		this.m_animator.SetBool("alert", this.m_alerted);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_alert, this.m_alerted);
		}
		if (this.m_alerted)
		{
			this.m_alertedEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
		if (this.m_character.IsBoss() && !this.m_nview.GetZDO().GetBool("bosscount", false))
		{
			float num;
			ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out num);
			ZoneSystem.instance.SetGlobalKey(GlobalKeys.activeBosses, num + 1f);
			this.m_nview.GetZDO().Set("bosscount", true);
		}
		if (alert && this.m_alertedMessage.Length > 0 && !this.m_nview.GetZDO().GetBool(ZDOVars.s_shownAlertMessage, false))
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_shownAlertMessage, true);
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, this.m_alertedMessage);
		}
	}

	// Token: 0x060000C1 RID: 193 RVA: 0x0000AEA4 File Offset: 0x000090A4
	public static bool InStealthRange(Character me)
	{
		bool result = false;
		foreach (BaseAI baseAI in BaseAI.BaseAIInstances)
		{
			if (BaseAI.IsEnemy(me, baseAI.m_character))
			{
				float num = Vector3.Distance(me.transform.position, baseAI.transform.position);
				if (num < baseAI.m_viewRange || num < 10f)
				{
					if (baseAI.IsAlerted())
					{
						return false;
					}
					result = true;
				}
			}
		}
		return result;
	}

	// Token: 0x060000C2 RID: 194 RVA: 0x0000AF40 File Offset: 0x00009140
	public static bool HaveEnemyInRange(Character me, Vector3 point, float range)
	{
		foreach (Character character in Character.GetAllCharacters())
		{
			if (BaseAI.IsEnemy(me, character) && Vector3.Distance(character.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060000C3 RID: 195 RVA: 0x0000AFB0 File Offset: 0x000091B0
	public static Character FindClosestEnemy(Character me, Vector3 point, float maxDistance)
	{
		Character character = null;
		float num = maxDistance;
		foreach (Character character2 in Character.GetAllCharacters())
		{
			if (BaseAI.IsEnemy(me, character2))
			{
				float num2 = Vector3.Distance(character2.transform.position, point);
				if (character == null || num2 < num)
				{
					character = character2;
					num = num2;
				}
			}
		}
		return character;
	}

	// Token: 0x060000C4 RID: 196 RVA: 0x0000B030 File Offset: 0x00009230
	public static Character FindRandomEnemy(Character me, Vector3 point, float maxDistance)
	{
		List<Character> list = new List<Character>();
		foreach (Character character in Character.GetAllCharacters())
		{
			if (BaseAI.IsEnemy(me, character) && Vector3.Distance(character.transform.position, point) < maxDistance)
			{
				list.Add(character);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	// Token: 0x060000C5 RID: 197 RVA: 0x0000B0C4 File Offset: 0x000092C4
	public bool IsAlerted()
	{
		return this.m_alerted;
	}

	// Token: 0x060000C6 RID: 198 RVA: 0x0000B0CC File Offset: 0x000092CC
	protected void SetTargetInfo(ZDOID targetID)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_haveTargetHash, !targetID.IsNone());
	}

	// Token: 0x060000C7 RID: 199 RVA: 0x0000B0ED File Offset: 0x000092ED
	public bool HaveTarget()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_haveTargetHash, false);
	}

	// Token: 0x060000C8 RID: 200 RVA: 0x0000B114 File Offset: 0x00009314
	private float GetAltitude()
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(base.transform.position, Vector3.down, out raycastHit, (float)BaseAI.m_solidRayMask))
		{
			return this.m_character.transform.position.y - raycastHit.point.y;
		}
		return 1000f;
	}

	// Token: 0x060000C9 RID: 201 RVA: 0x0000B168 File Offset: 0x00009368
	public static List<BaseAI> GetAllInstances()
	{
		return BaseAI.m_instances;
	}

	// Token: 0x060000CA RID: 202 RVA: 0x0000B170 File Offset: 0x00009370
	protected virtual void OnDrawGizmosSelected()
	{
		if (this.m_lastFindPathResult)
		{
			Gizmos.color = Color.yellow;
			for (int i = 0; i < this.m_path.Count - 1; i++)
			{
				Vector3 a = this.m_path[i];
				Vector3 a2 = this.m_path[i + 1];
				Gizmos.DrawLine(a + Vector3.up * 0.1f, a2 + Vector3.up * 0.1f);
			}
			Gizmos.color = Color.cyan;
			foreach (Vector3 a3 in this.m_path)
			{
				Gizmos.DrawSphere(a3 + Vector3.up * 0.1f, 0.1f);
			}
			Gizmos.color = Color.green;
			Gizmos.DrawLine(base.transform.position, this.m_lastFindPathTarget);
			Gizmos.DrawSphere(this.m_lastFindPathTarget, 0.2f);
			return;
		}
		Gizmos.color = Color.red;
		Gizmos.DrawLine(base.transform.position, this.m_lastFindPathTarget);
		Gizmos.DrawSphere(this.m_lastFindPathTarget, 0.2f);
	}

	// Token: 0x060000CB RID: 203 RVA: 0x0000B2BC File Offset: 0x000094BC
	public virtual bool IsSleeping()
	{
		return false;
	}

	// Token: 0x060000CC RID: 204 RVA: 0x0000B2BF File Offset: 0x000094BF
	public bool HasZDOOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().HasOwner();
	}

	// Token: 0x060000CD RID: 205 RVA: 0x0000B2E0 File Offset: 0x000094E0
	public bool CanUseAttack(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_aiInDungeonOnly && !this.m_character.InInterior())
		{
			return false;
		}
		if (item.m_shared.m_aiMaxHealthPercentage < 1f && this.m_character.GetHealthPercentage() > item.m_shared.m_aiMaxHealthPercentage)
		{
			return false;
		}
		if (item.m_shared.m_aiMinHealthPercentage > 0f && this.m_character.GetHealthPercentage() < item.m_shared.m_aiMinHealthPercentage)
		{
			return false;
		}
		bool flag = this.m_character.IsFlying();
		bool flag2 = this.m_character.IsSwimming();
		if (item.m_shared.m_aiWhenFlying && flag)
		{
			float altitude = this.GetAltitude();
			return altitude > item.m_shared.m_aiWhenFlyingAltitudeMin && altitude < item.m_shared.m_aiWhenFlyingAltitudeMax;
		}
		return (!item.m_shared.m_aiInMistOnly || ParticleMist.IsInMist(this.m_character.GetCenterPoint())) && ((item.m_shared.m_aiWhenWalking && !flag && !flag2) || (item.m_shared.m_aiWhenSwiming && flag2));
	}

	// Token: 0x060000CE RID: 206 RVA: 0x0000B3F6 File Offset: 0x000095F6
	public virtual Character GetTargetCreature()
	{
		return null;
	}

	// Token: 0x060000CF RID: 207 RVA: 0x0000B3F9 File Offset: 0x000095F9
	public bool HaveRider()
	{
		return this.m_tamable && this.m_tamable.HaveRider();
	}

	// Token: 0x060000D0 RID: 208 RVA: 0x0000B415 File Offset: 0x00009615
	public float GetRiderSkill()
	{
		if (this.m_tamable)
		{
			return this.m_tamable.GetRiderSkill();
		}
		return 0f;
	}

	// Token: 0x060000D1 RID: 209 RVA: 0x0000B438 File Offset: 0x00009638
	public static void AggravateAllInArea(Vector3 point, float radius, BaseAI.AggravatedReason reason)
	{
		foreach (BaseAI baseAI in BaseAI.BaseAIInstances)
		{
			if (baseAI.IsAggravatable() && Vector3.Distance(point, baseAI.transform.position) <= radius)
			{
				baseAI.SetAggravated(true, reason);
				baseAI.Alert();
			}
		}
	}

	// Token: 0x060000D2 RID: 210 RVA: 0x0000B4B0 File Offset: 0x000096B0
	public void ChargeStart(string animBool)
	{
		if (!this.IsCharging())
		{
			this.m_character.GetZAnim().SetBool(animBool, true);
			this.m_charging = animBool;
		}
	}

	// Token: 0x060000D3 RID: 211 RVA: 0x0000B4D3 File Offset: 0x000096D3
	public void ChargeStop()
	{
		if (this.IsCharging())
		{
			this.m_character.GetZAnim().SetBool(this.m_charging, false);
			this.m_charging = null;
		}
	}

	// Token: 0x060000D4 RID: 212 RVA: 0x0000B4FB File Offset: 0x000096FB
	public bool IsCharging()
	{
		return this.m_charging != null;
	}

	// Token: 0x17000004 RID: 4
	// (get) Token: 0x060000D5 RID: 213 RVA: 0x0000B506 File Offset: 0x00009706
	public static List<IUpdateAI> Instances { get; } = new List<IUpdateAI>();

	// Token: 0x17000005 RID: 5
	// (get) Token: 0x060000D6 RID: 214 RVA: 0x0000B50D File Offset: 0x0000970D
	public static List<BaseAI> BaseAIInstances { get; } = new List<BaseAI>();

	// Token: 0x0400015E RID: 350
	private float m_lastMoveToWaterUpdate;

	// Token: 0x0400015F RID: 351
	private bool m_haveWaterPosition;

	// Token: 0x04000160 RID: 352
	private Vector3 m_moveToWaterPosition = Vector3.zero;

	// Token: 0x04000161 RID: 353
	private float m_fleeTargetUpdateTime;

	// Token: 0x04000162 RID: 354
	private Vector3 m_fleeTarget = Vector3.zero;

	// Token: 0x04000163 RID: 355
	private float m_nearFireTime;

	// Token: 0x04000164 RID: 356
	private EffectArea m_nearFireArea;

	// Token: 0x04000165 RID: 357
	private float aroundPointUpdateTime;

	// Token: 0x04000166 RID: 358
	private Vector3 arroundPointTarget = Vector3.zero;

	// Token: 0x04000167 RID: 359
	private Vector3 m_lastMovementCheck;

	// Token: 0x04000168 RID: 360
	private float m_lastMoveTime;

	// Token: 0x04000169 RID: 361
	private const bool m_debugDraw = false;

	// Token: 0x0400016A RID: 362
	public Action<BaseAI.AggravatedReason> m_onBecameAggravated;

	// Token: 0x0400016B RID: 363
	public float m_viewRange = 50f;

	// Token: 0x0400016C RID: 364
	public float m_viewAngle = 90f;

	// Token: 0x0400016D RID: 365
	public float m_hearRange = 9999f;

	// Token: 0x0400016E RID: 366
	public bool m_mistVision;

	// Token: 0x0400016F RID: 367
	private const float m_interiorMaxHearRange = 12f;

	// Token: 0x04000170 RID: 368
	private const float m_despawnDistance = 80f;

	// Token: 0x04000171 RID: 369
	private const float m_regenAllHPTime = 3600f;

	// Token: 0x04000172 RID: 370
	public EffectList m_alertedEffects = new EffectList();

	// Token: 0x04000173 RID: 371
	public EffectList m_idleSound = new EffectList();

	// Token: 0x04000174 RID: 372
	public float m_idleSoundInterval = 5f;

	// Token: 0x04000175 RID: 373
	public float m_idleSoundChance = 0.5f;

	// Token: 0x04000176 RID: 374
	public Pathfinding.AgentType m_pathAgentType = Pathfinding.AgentType.Humanoid;

	// Token: 0x04000177 RID: 375
	public float m_moveMinAngle = 10f;

	// Token: 0x04000178 RID: 376
	public bool m_smoothMovement = true;

	// Token: 0x04000179 RID: 377
	public bool m_serpentMovement;

	// Token: 0x0400017A RID: 378
	public float m_serpentTurnRadius = 20f;

	// Token: 0x0400017B RID: 379
	public float m_jumpInterval;

	// Token: 0x0400017C RID: 380
	[Header("Random circle")]
	public float m_randomCircleInterval = 2f;

	// Token: 0x0400017D RID: 381
	[Header("Random movement")]
	public float m_randomMoveInterval = 5f;

	// Token: 0x0400017E RID: 382
	public float m_randomMoveRange = 4f;

	// Token: 0x0400017F RID: 383
	[Header("Fly behaviour")]
	public bool m_randomFly;

	// Token: 0x04000180 RID: 384
	public float m_chanceToTakeoff = 1f;

	// Token: 0x04000181 RID: 385
	public float m_chanceToLand = 1f;

	// Token: 0x04000182 RID: 386
	public float m_groundDuration = 10f;

	// Token: 0x04000183 RID: 387
	public float m_airDuration = 10f;

	// Token: 0x04000184 RID: 388
	public float m_maxLandAltitude = 5f;

	// Token: 0x04000185 RID: 389
	public float m_takeoffTime = 5f;

	// Token: 0x04000186 RID: 390
	public float m_flyAltitudeMin = 3f;

	// Token: 0x04000187 RID: 391
	public float m_flyAltitudeMax = 10f;

	// Token: 0x04000188 RID: 392
	public float m_flyAbsMinAltitude = 32f;

	// Token: 0x04000189 RID: 393
	[Header("Other")]
	public bool m_avoidFire;

	// Token: 0x0400018A RID: 394
	public bool m_afraidOfFire;

	// Token: 0x0400018B RID: 395
	public bool m_avoidWater = true;

	// Token: 0x0400018C RID: 396
	public bool m_avoidLava = true;

	// Token: 0x0400018D RID: 397
	public bool m_skipLavaTargets;

	// Token: 0x0400018E RID: 398
	public bool m_avoidLavaFlee = true;

	// Token: 0x0400018F RID: 399
	public bool m_aggravatable;

	// Token: 0x04000190 RID: 400
	public bool m_passiveAggresive;

	// Token: 0x04000191 RID: 401
	public string m_spawnMessage = "";

	// Token: 0x04000192 RID: 402
	public string m_deathMessage = "";

	// Token: 0x04000193 RID: 403
	public string m_alertedMessage = "";

	// Token: 0x04000194 RID: 404
	[Header("Flee")]
	public float m_fleeRange = 25f;

	// Token: 0x04000195 RID: 405
	public float m_fleeAngle = 45f;

	// Token: 0x04000196 RID: 406
	public float m_fleeInterval = 2f;

	// Token: 0x04000197 RID: 407
	private bool m_patrol;

	// Token: 0x04000198 RID: 408
	private Vector3 m_patrolPoint = Vector3.zero;

	// Token: 0x04000199 RID: 409
	private float m_patrolPointUpdateTime;

	// Token: 0x0400019A RID: 410
	protected ZNetView m_nview;

	// Token: 0x0400019B RID: 411
	protected Character m_character;

	// Token: 0x0400019C RID: 412
	protected ZSyncAnimation m_animator;

	// Token: 0x0400019D RID: 413
	protected Tameable m_tamable;

	// Token: 0x0400019E RID: 414
	protected Rigidbody m_body;

	// Token: 0x0400019F RID: 415
	private static int m_solidRayMask = 0;

	// Token: 0x040001A0 RID: 416
	private static int m_viewBlockMask = 0;

	// Token: 0x040001A1 RID: 417
	private static int m_monsterTargetRayMask = 0;

	// Token: 0x040001A2 RID: 418
	private Vector3 m_randomMoveTarget = Vector3.zero;

	// Token: 0x040001A3 RID: 419
	private float m_randomMoveUpdateTimer;

	// Token: 0x040001A4 RID: 420
	private bool m_reachedRandomMoveTarget = true;

	// Token: 0x040001A5 RID: 421
	private float m_jumpTimer;

	// Token: 0x040001A6 RID: 422
	private float m_randomFlyTimer;

	// Token: 0x040001A7 RID: 423
	private float m_regenTimer;

	// Token: 0x040001A8 RID: 424
	private bool m_alerted;

	// Token: 0x040001A9 RID: 425
	private bool m_huntPlayer;

	// Token: 0x040001AA RID: 426
	private bool m_aggravated;

	// Token: 0x040001AB RID: 427
	private float m_lastAggravatedCheck;

	// Token: 0x040001AC RID: 428
	protected Vector3 m_spawnPoint = Vector3.zero;

	// Token: 0x040001AD RID: 429
	private const float m_getOfOfCornerMaxAngle = 20f;

	// Token: 0x040001AE RID: 430
	private float m_getOutOfCornerTimer;

	// Token: 0x040001AF RID: 431
	private float m_getOutOfCornerAngle;

	// Token: 0x040001B0 RID: 432
	private Vector3 m_lastPosition = Vector3.zero;

	// Token: 0x040001B1 RID: 433
	private float m_stuckTimer;

	// Token: 0x040001B2 RID: 434
	protected float m_timeSinceHurt = 99999f;

	// Token: 0x040001B3 RID: 435
	protected float m_lastFlee;

	// Token: 0x040001B4 RID: 436
	private string m_charging;

	// Token: 0x040001B5 RID: 437
	private Vector3 m_lastFindPathTarget = new Vector3(-999999f, -999999f, -999999f);

	// Token: 0x040001B6 RID: 438
	private float m_lastFindPathTime;

	// Token: 0x040001B7 RID: 439
	private bool m_lastFindPathResult;

	// Token: 0x040001B8 RID: 440
	private readonly List<Vector3> m_path = new List<Vector3>();

	// Token: 0x040001B9 RID: 441
	private static readonly RaycastHit[] s_tempRaycastHits = new RaycastHit[128];

	// Token: 0x040001BA RID: 442
	private static readonly Collider[] s_tempSphereOverlap = new Collider[128];

	// Token: 0x040001BB RID: 443
	private static List<BaseAI> m_instances = new List<BaseAI>();

	// Token: 0x02000229 RID: 553
	public enum AggravatedReason
	{
		// Token: 0x04001F3E RID: 7998
		Damage,
		// Token: 0x04001F3F RID: 7999
		Building,
		// Token: 0x04001F40 RID: 8000
		Theif
	}
}
