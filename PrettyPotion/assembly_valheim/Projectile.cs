using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000006 RID: 6
public class Projectile : MonoBehaviour, IProjectile
{
	// Token: 0x0600004A RID: 74 RVA: 0x000068D8 File Offset: 0x00004AD8
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (Projectile.s_rayMaskSolids == 0)
		{
			Projectile.s_rayMaskSolids = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid",
				"terrain",
				"character",
				"character_net",
				"character_ghost",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
		}
		if (!string.IsNullOrEmpty(this.m_statusEffect))
		{
			this.m_statusEffectHash = this.m_statusEffect.GetStableHashCode();
		}
		this.m_nview.Register("RPC_OnHit", new Action<long>(this.RPC_OnHit));
		this.m_nview.Register<ZDOID>("RPC_Attach", new Action<long, ZDOID>(this.RPC_Attach));
		this.UpdateVisual();
	}

	// Token: 0x0600004B RID: 75 RVA: 0x000069C8 File Offset: 0x00004BC8
	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	// Token: 0x0600004C RID: 76 RVA: 0x000069D0 File Offset: 0x00004BD0
	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateRotation(Time.fixedDeltaTime);
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_didHit)
		{
			Vector3 vector = base.transform.position;
			if (this.m_haveStartPoint)
			{
				vector = this.m_startPoint;
			}
			this.m_vel += Vector3.down * (this.m_gravity * Time.fixedDeltaTime);
			float d = Mathf.Pow(this.m_vel.magnitude, 2f) * this.m_drag * Time.fixedDeltaTime;
			this.m_vel += d * -this.m_vel.normalized;
			base.transform.position += this.m_vel * Time.fixedDeltaTime;
			if (this.m_rotateVisual == 0f)
			{
				base.transform.rotation = Quaternion.LookRotation(this.m_vel);
			}
			if (this.m_canHitWater)
			{
				float liquidLevel = Floating.GetLiquidLevel(base.transform.position, 1f, LiquidType.All);
				if (base.transform.position.y < liquidLevel)
				{
					this.OnHit(null, base.transform.position, true, Vector3.up);
				}
			}
			this.m_didBounce = false;
			if (!this.m_didHit)
			{
				Vector3 vector2 = base.transform.position - vector;
				if (!this.m_haveStartPoint)
				{
					vector -= vector2.normalized * (vector2.magnitude * 0.5f);
				}
				RaycastHit[] array;
				if (this.m_rayRadius == 0f)
				{
					array = Physics.RaycastAll(vector, vector2.normalized, vector2.magnitude * 1.5f, Projectile.s_rayMaskSolids);
				}
				else
				{
					array = Physics.SphereCastAll(vector, this.m_rayRadius, vector2.normalized, vector2.magnitude, Projectile.s_rayMaskSolids);
				}
				Debug.DrawLine(vector, base.transform.position, (array.Length != 0) ? Color.red : Color.yellow, 5f);
				if (array.Length != 0)
				{
					Array.Sort<RaycastHit>(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
					foreach (RaycastHit raycastHit in array)
					{
						Vector3 hitPoint = (raycastHit.distance == 0f) ? vector : raycastHit.point;
						this.OnHit(raycastHit.collider, hitPoint, false, raycastHit.normal);
						if (this.m_didHit || this.m_didBounce)
						{
							break;
						}
					}
				}
			}
			if (this.m_haveStartPoint)
			{
				this.m_haveStartPoint = false;
			}
		}
		if (this.m_ttl > 0f)
		{
			this.m_ttl -= Time.fixedDeltaTime;
			if (this.m_ttl <= 0f)
			{
				if (this.m_spawnOnTtl)
				{
					this.SpawnOnHit(null, null, -this.m_vel.normalized);
				}
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
		ShieldGenerator.CheckProjectile(this);
	}

	// Token: 0x0600004D RID: 77 RVA: 0x00006CF5 File Offset: 0x00004EF5
	private void Update()
	{
		this.UpdateVisual();
	}

	// Token: 0x0600004E RID: 78 RVA: 0x00006D00 File Offset: 0x00004F00
	private void LateUpdate()
	{
		if (this.m_attachParent)
		{
			Vector3 point = this.m_attachParent.transform.position - this.m_attachParentOffset;
			Quaternion quaternion = this.m_attachParent.transform.rotation * this.m_attachParentOffsetRot;
			base.transform.position = Utils.RotatePointAroundPivot(point, this.m_attachParent.transform.position, quaternion);
			base.transform.localRotation = quaternion;
		}
	}

	// Token: 0x0600004F RID: 79 RVA: 0x00006D80 File Offset: 0x00004F80
	private void UpdateVisual()
	{
		if (!this.m_canChangeVisuals || this.m_nview == null)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		string text;
		if (this.m_changedVisual || !this.m_nview.GetZDO().GetString(ZDOVars.s_visual, out text))
		{
			return;
		}
		ZLog.Log("Visual prefab is " + text);
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(text);
		if (itemPrefab.GetComponent<ItemDrop>() == null)
		{
			return;
		}
		GameObject gameObject = ItemStand.GetAttachPrefab(itemPrefab);
		if (gameObject == null)
		{
			return;
		}
		gameObject = ItemStand.GetAttachGameObject(gameObject);
		this.m_visual.gameObject.SetActive(false);
		this.m_visual = UnityEngine.Object.Instantiate<GameObject>(gameObject, base.transform);
		this.m_visual.transform.localPosition = Vector3.zero;
		this.m_changedVisual = true;
	}

	// Token: 0x06000050 RID: 80 RVA: 0x00006E4A File Offset: 0x0000504A
	public Vector3 GetVelocity()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return Vector3.zero;
		}
		if (this.m_didHit)
		{
			return Vector3.zero;
		}
		return this.m_vel;
	}

	// Token: 0x06000051 RID: 81 RVA: 0x00006E80 File Offset: 0x00005080
	private void UpdateRotation(float dt)
	{
		if (this.m_visual == null || ((double)this.m_rotateVisual == 0.0 && (double)this.m_rotateVisualY == 0.0 && (double)this.m_rotateVisualZ == 0.0))
		{
			return;
		}
		this.m_visual.transform.Rotate(new Vector3(this.m_rotateVisual * dt, this.m_rotateVisualY * dt, this.m_rotateVisualZ * dt));
	}

	// Token: 0x06000052 RID: 82 RVA: 0x00006F00 File Offset: 0x00005100
	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		this.m_vel = velocity;
		this.m_ammo = ammo;
		this.m_weapon = item;
		if (hitNoise >= 0f)
		{
			this.m_hitNoise = hitNoise;
		}
		if (hitData != null)
		{
			this.m_originalHitData = hitData;
			this.m_damage = hitData.m_damage;
			this.m_blockable = hitData.m_blockable;
			this.m_dodgeable = hitData.m_dodgeable;
			this.m_attackForce = hitData.m_pushForce;
			this.m_backstabBonus = hitData.m_backstabBonus;
			this.m_healthReturn = hitData.m_healthReturn;
			if (this.m_statusEffectHash != hitData.m_statusEffectHash)
			{
				this.m_statusEffectHash = hitData.m_statusEffectHash;
				this.m_statusEffect = "";
			}
			this.m_skill = hitData.m_skill;
			this.m_raiseSkillAmount = hitData.m_skillRaiseAmount;
		}
		if (this.m_spawnOnHit != null && this.m_onlySpawnedProjectilesDealDamage)
		{
			this.m_damage.Modify(0f);
		}
		if (this.m_respawnItemOnHit)
		{
			this.m_spawnItem = item;
		}
		if (this.m_doOwnerRaytest && owner)
		{
			this.m_startPoint = owner.GetCenterPoint();
			this.m_startPoint.y = base.transform.position.y;
			this.m_haveStartPoint = true;
		}
		else
		{
			this.m_startPoint = base.transform.position;
		}
		LineConnect component = base.GetComponent<LineConnect>();
		if (component && owner)
		{
			component.SetPeer(owner.GetZDOID());
		}
		this.m_hasLeftShields = !ShieldGenerator.IsInsideShield(base.transform.position);
	}

	// Token: 0x06000053 RID: 83 RVA: 0x0000709C File Offset: 0x0000529C
	private void DoAOE(Vector3 hitPoint, ref bool hitCharacter, ref bool didDamage)
	{
		Collider[] array = Physics.OverlapSphere(hitPoint, this.m_aoe, Projectile.s_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		foreach (Collider collider in array)
		{
			GameObject gameObject = Projectile.FindHitObject(collider);
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component != null && !hashSet.Contains(gameObject))
			{
				hashSet.Add(gameObject);
				if (this.IsValidTarget(component))
				{
					if (component is Character)
					{
						hitCharacter = true;
					}
					Vector3 vector = collider.ClosestPointOnBounds(hitPoint);
					Vector3 vector2 = (Vector3.Distance(vector, hitPoint) > 0.1f) ? (vector - hitPoint) : this.m_vel;
					vector2.y = 0f;
					vector2.Normalize();
					HitData hitData = new HitData();
					hitData.m_hitCollider = collider;
					hitData.m_damage = this.m_damage;
					hitData.m_pushForce = this.m_attackForce;
					hitData.m_backstabBonus = this.m_backstabBonus;
					hitData.m_ranged = true;
					hitData.m_point = vector;
					hitData.m_dir = vector2.normalized;
					hitData.m_statusEffectHash = this.m_statusEffectHash;
					hitData.m_skillLevel = (this.m_owner ? this.m_owner.GetSkillLevel(this.m_skill) : 1f);
					hitData.m_dodgeable = this.m_dodgeable;
					hitData.m_blockable = this.m_blockable;
					hitData.m_skill = this.m_skill;
					hitData.m_skillRaiseAmount = this.m_raiseSkillAmount;
					hitData.SetAttacker(this.m_owner);
					hitData.m_hitType = ((hitData.GetAttacker() is Player) ? HitData.HitType.PlayerHit : HitData.HitType.EnemyHit);
					hitData.m_healthReturn = this.m_healthReturn;
					component.Damage(hitData);
					didDamage = true;
				}
			}
		}
	}

	// Token: 0x06000054 RID: 84 RVA: 0x00007260 File Offset: 0x00005460
	private bool IsValidTarget(IDestructible destr)
	{
		Character character = destr as Character;
		if (character)
		{
			if (character == this.m_owner)
			{
				return false;
			}
			if (this.m_owner != null)
			{
				bool flag = BaseAI.IsEnemy(this.m_owner, character) || (character.GetBaseAI() && character.GetBaseAI().IsAggravatable() && this.m_owner.IsPlayer());
				if (!this.m_owner.IsPlayer() && !flag)
				{
					return false;
				}
				if (this.m_owner.IsPlayer() && !this.m_owner.IsPVPEnabled() && !flag)
				{
					return false;
				}
			}
			if (this.m_dodgeable && character.IsDodgeInvincible())
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06000055 RID: 85 RVA: 0x0000731C File Offset: 0x0000551C
	public void OnHit(Collider collider, Vector3 hitPoint, bool water, Vector3 normal)
	{
		GameObject gameObject = collider ? Projectile.FindHitObject(collider) : null;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = this.m_bounce && normal != Vector3.zero;
		if (water)
		{
			flag3 = (flag3 && this.m_bounceOnWater);
		}
		IDestructible destructible = gameObject ? gameObject.GetComponent<IDestructible>() : null;
		if (destructible != null)
		{
			flag2 = (destructible is Character);
			flag3 = (flag3 && !flag2);
			if (!this.IsValidTarget(destructible))
			{
				return;
			}
		}
		if (flag3 && this.m_bounceCount < this.m_maxBounces && this.m_vel.magnitude > this.m_minBounceVel)
		{
			Vector3 normalized = this.m_vel.normalized;
			if (this.m_bounceRoughness > 0f)
			{
				Vector3 vector = UnityEngine.Random.onUnitSphere;
				float f = Vector3.Dot(normal, vector);
				vector *= Mathf.Sign(f);
				normal = Vector3.Lerp(normal, vector, this.m_bounceRoughness).normalized;
			}
			this.m_vel = Vector3.Reflect(normalized, normal) * (this.m_vel.magnitude * this.m_bouncePower);
			this.m_bounceCount++;
			this.m_didBounce = true;
			return;
		}
		if (this.m_aoe > 0f)
		{
			this.DoAOE(hitPoint, ref flag2, ref flag);
		}
		else if (destructible != null)
		{
			HitData hitData = new HitData();
			hitData.m_hitCollider = collider;
			hitData.m_damage = this.m_damage;
			hitData.m_pushForce = this.m_attackForce;
			hitData.m_backstabBonus = this.m_backstabBonus;
			hitData.m_point = hitPoint;
			hitData.m_dir = base.transform.forward;
			hitData.m_statusEffectHash = this.m_statusEffectHash;
			hitData.m_dodgeable = this.m_dodgeable;
			hitData.m_blockable = this.m_blockable;
			hitData.m_ranged = true;
			hitData.m_skill = this.m_skill;
			hitData.m_skillRaiseAmount = this.m_raiseSkillAmount;
			hitData.SetAttacker(this.m_owner);
			hitData.m_hitType = ((hitData.GetAttacker() is Player) ? HitData.HitType.PlayerHit : HitData.HitType.EnemyHit);
			hitData.m_healthReturn = this.m_healthReturn;
			destructible.Damage(hitData);
			if (this.m_healthReturn > 0f && this.m_owner)
			{
				this.m_owner.Heal(this.m_healthReturn, true);
			}
			flag = true;
		}
		if (water)
		{
			this.m_hitWaterEffects.Create(hitPoint, Quaternion.identity, null, 1f, -1);
		}
		else
		{
			this.m_hitEffects.Create(hitPoint, Quaternion.identity, null, 1f, -1);
		}
		if (this.m_spawnOnHit != null || this.m_spawnItem != null || this.m_randomSpawnOnHit.Count > 0)
		{
			this.SpawnOnHit(gameObject, collider, normal);
		}
		OnProjectileHit onHit = this.m_onHit;
		if (onHit != null)
		{
			onHit(collider, hitPoint, water);
		}
		if (this.m_hitNoise > 0f)
		{
			BaseAI.DoProjectileHitNoise(base.transform.position, this.m_hitNoise, this.m_owner);
		}
		if (flag && this.m_owner != null && flag2)
		{
			this.m_owner.RaiseSkill(this.m_skill, this.m_raiseSkillAmount);
		}
		this.m_didHit = true;
		base.transform.position = hitPoint;
		this.m_nview.InvokeRPC("RPC_OnHit", Array.Empty<object>());
		this.m_ttl = this.m_stayTTL;
		if (collider && collider.attachedRigidbody != null)
		{
			ZNetView componentInParent = collider.gameObject.GetComponentInParent<ZNetView>();
			if (componentInParent && (this.m_attachToClosestBone || this.m_attachToRigidBody))
			{
				this.m_nview.InvokeRPC("RPC_Attach", new object[]
				{
					componentInParent.GetZDO().m_uid
				});
				return;
			}
			if (!this.m_stayAfterHitDynamic)
			{
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
		}
		else if (!this.m_stayAfterHitStatic)
		{
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}

	// Token: 0x06000056 RID: 86 RVA: 0x00007720 File Offset: 0x00005920
	private void RPC_OnHit(long sender)
	{
		if (this.m_hideOnHit)
		{
			this.m_hideOnHit.SetActive(false);
		}
		if (this.m_stopEmittersOnHit)
		{
			ParticleSystem[] componentsInChildren = base.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].emission.enabled = false;
			}
		}
	}

	// Token: 0x06000057 RID: 87 RVA: 0x00007774 File Offset: 0x00005974
	private void RPC_Attach(long sender, ZDOID parent)
	{
		this.m_attachParent = ZNetScene.instance.FindInstance(parent);
		if (this.m_attachParent)
		{
			if (this.m_attachToClosestBone)
			{
				float dist = float.MaxValue;
				Animator componentInChildren = this.m_attachParent.gameObject.GetComponentInChildren<Animator>();
				if (componentInChildren != null)
				{
					Utils.IterateHierarchy(componentInChildren.gameObject, delegate(GameObject obj)
					{
						float num = Vector3.Distance(this.transform.position, obj.transform.position);
						if (num < dist)
						{
							dist = num;
							this.m_attachParent = obj;
						}
					}, false);
				}
			}
			base.transform.position += base.transform.forward * this.m_attachPenetration;
			base.transform.position += (this.m_attachParent.transform.position - base.transform.position) * this.m_attachBoneNearify;
			this.m_attachParentOffset = this.m_attachParent.transform.position - base.transform.position;
			this.m_attachParentOffsetRot = Quaternion.Inverse(this.m_attachParent.transform.localRotation * base.transform.localRotation);
		}
	}

	// Token: 0x06000058 RID: 88 RVA: 0x000078AC File Offset: 0x00005AAC
	private void SpawnOnHit(GameObject go, Collider collider, Vector3 normal)
	{
		if (this.m_groundHitOnly && go.GetComponent<Heightmap>() == null)
		{
			return;
		}
		if (this.m_staticHitOnly)
		{
			if (collider && collider.attachedRigidbody != null)
			{
				return;
			}
			if (go && go.GetComponent<IDestructible>() != null)
			{
				return;
			}
		}
		Vector3 vector = base.transform.position + base.transform.TransformDirection(this.m_spawnOffset);
		Quaternion rotation = Quaternion.identity;
		if (this.m_copyProjectileRotation)
		{
			rotation = base.transform.rotation;
		}
		if (this.m_spawnRandomRotation)
		{
			rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		}
		if (this.m_spawnFacingRotation)
		{
			rotation = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y, 0f);
		}
		if (this.m_spawnOnHit != null && (this.m_spawnOnHitChance >= 1f || UnityEngine.Random.value < this.m_spawnOnHitChance))
		{
			for (int i = 0; i < this.m_spawnCount; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnHit, vector, rotation);
				Vector3 normalized = this.m_vel.normalized;
				Vector3 vector2 = UnityEngine.Random.onUnitSphere;
				if (this.m_spawnProjectileHemisphereDir)
				{
					vector2 *= Mathf.Sign(Vector3.Dot(normal, vector2));
				}
				normalized = Vector3.Lerp(normalized, vector2, this.m_spawnProjectileRandomDir).normalized;
				float d = this.m_vel.magnitude;
				if (this.m_spawnProjectileNewVelocity)
				{
					d = UnityEngine.Random.Range(this.m_spawnProjectileMinVel, this.m_spawnProjectileMaxVel);
				}
				IProjectile componentInChildren = gameObject.GetComponentInChildren<IProjectile>();
				if (componentInChildren != null)
				{
					gameObject.transform.position += normal * 0.25f;
					HitData hitData = null;
					if (this.m_projectilesInheritHitData)
					{
						hitData = this.m_originalHitData;
						if (this.m_divideDamageBetweenProjectiles)
						{
							hitData.m_damage.Modify(1f / (float)this.m_spawnCount);
						}
					}
					componentInChildren.Setup(this.m_owner, normalized * d, this.m_hitNoise, hitData, this.m_weapon, this.m_ammo);
				}
			}
		}
		if (this.m_spawnItem != null)
		{
			ItemDrop.DropItem(this.m_spawnItem, 1, vector, base.transform.rotation);
		}
		if (this.m_randomSpawnOnHit.Count > 0 && (!this.m_randomSpawnSkipLava || !ZoneSystem.instance.IsLava(base.transform.position, false)))
		{
			for (int j = 0; j < this.m_randomSpawnOnHitCount; j++)
			{
				GameObject gameObject2 = this.m_randomSpawnOnHit[UnityEngine.Random.Range(0, this.m_randomSpawnOnHit.Count)];
				if (gameObject2)
				{
					IProjectile component = UnityEngine.Object.Instantiate<GameObject>(gameObject2, vector, rotation).GetComponent<IProjectile>();
					if (component != null)
					{
						component.Setup(this.m_owner, this.m_vel, this.m_hitNoise, null, null, this.m_ammo);
					}
				}
			}
		}
		this.m_spawnOnHitEffects.Create(vector, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x06000059 RID: 89 RVA: 0x00007BC4 File Offset: 0x00005DC4
	public static GameObject FindHitObject(Collider collider)
	{
		IDestructible componentInParent = collider.gameObject.GetComponentInParent<IDestructible>();
		if (componentInParent != null)
		{
			return (componentInParent as MonoBehaviour).gameObject;
		}
		if (collider.attachedRigidbody)
		{
			return collider.attachedRigidbody.gameObject;
		}
		return collider.gameObject;
	}

	// Token: 0x17000003 RID: 3
	// (get) Token: 0x0600005A RID: 90 RVA: 0x00007C0B File Offset: 0x00005E0B
	public bool HasBeenOutsideShields
	{
		get
		{
			return this.m_hasLeftShields;
		}
	}

	// Token: 0x0600005B RID: 91 RVA: 0x00007C13 File Offset: 0x00005E13
	public void TriggerShieldsLeftFlag()
	{
		this.m_hasLeftShields = true;
	}

	// Token: 0x040000D8 RID: 216
	public HitData.DamageTypes m_damage;

	// Token: 0x040000D9 RID: 217
	public float m_aoe;

	// Token: 0x040000DA RID: 218
	public bool m_dodgeable;

	// Token: 0x040000DB RID: 219
	public bool m_blockable;

	// Token: 0x040000DC RID: 220
	public float m_attackForce;

	// Token: 0x040000DD RID: 221
	public float m_backstabBonus = 4f;

	// Token: 0x040000DE RID: 222
	public string m_statusEffect = "";

	// Token: 0x040000DF RID: 223
	private int m_statusEffectHash;

	// Token: 0x040000E0 RID: 224
	public float m_healthReturn;

	// Token: 0x040000E1 RID: 225
	public bool m_canHitWater;

	// Token: 0x040000E2 RID: 226
	public float m_ttl = 4f;

	// Token: 0x040000E3 RID: 227
	public float m_gravity;

	// Token: 0x040000E4 RID: 228
	public float m_drag;

	// Token: 0x040000E5 RID: 229
	public float m_rayRadius;

	// Token: 0x040000E6 RID: 230
	public float m_hitNoise = 50f;

	// Token: 0x040000E7 RID: 231
	public bool m_doOwnerRaytest;

	// Token: 0x040000E8 RID: 232
	public bool m_stayAfterHitStatic;

	// Token: 0x040000E9 RID: 233
	public bool m_stayAfterHitDynamic;

	// Token: 0x040000EA RID: 234
	public float m_stayTTL = 1f;

	// Token: 0x040000EB RID: 235
	public bool m_attachToRigidBody;

	// Token: 0x040000EC RID: 236
	public bool m_attachToClosestBone;

	// Token: 0x040000ED RID: 237
	public float m_attachPenetration;

	// Token: 0x040000EE RID: 238
	public float m_attachBoneNearify = 0.25f;

	// Token: 0x040000EF RID: 239
	public GameObject m_hideOnHit;

	// Token: 0x040000F0 RID: 240
	public bool m_stopEmittersOnHit = true;

	// Token: 0x040000F1 RID: 241
	public EffectList m_hitEffects = new EffectList();

	// Token: 0x040000F2 RID: 242
	public EffectList m_hitWaterEffects = new EffectList();

	// Token: 0x040000F3 RID: 243
	[Header("Bounce")]
	public bool m_bounce;

	// Token: 0x040000F4 RID: 244
	public bool m_bounceOnWater;

	// Token: 0x040000F5 RID: 245
	[Range(0f, 1f)]
	public float m_bouncePower = 0.85f;

	// Token: 0x040000F6 RID: 246
	[Range(0f, 1f)]
	public float m_bounceRoughness = 0.3f;

	// Token: 0x040000F7 RID: 247
	[Min(1f)]
	public int m_maxBounces = 99;

	// Token: 0x040000F8 RID: 248
	[Min(0.01f)]
	public float m_minBounceVel = 0.25f;

	// Token: 0x040000F9 RID: 249
	[Header("Spawn on hit")]
	public bool m_respawnItemOnHit;

	// Token: 0x040000FA RID: 250
	public bool m_spawnOnTtl;

	// Token: 0x040000FB RID: 251
	public GameObject m_spawnOnHit;

	// Token: 0x040000FC RID: 252
	[Range(0f, 1f)]
	public float m_spawnOnHitChance = 1f;

	// Token: 0x040000FD RID: 253
	public int m_spawnCount = 1;

	// Token: 0x040000FE RID: 254
	public List<GameObject> m_randomSpawnOnHit = new List<GameObject>();

	// Token: 0x040000FF RID: 255
	public int m_randomSpawnOnHitCount = 1;

	// Token: 0x04000100 RID: 256
	public bool m_randomSpawnSkipLava;

	// Token: 0x04000101 RID: 257
	public bool m_showBreakMessage;

	// Token: 0x04000102 RID: 258
	public bool m_staticHitOnly;

	// Token: 0x04000103 RID: 259
	public bool m_groundHitOnly;

	// Token: 0x04000104 RID: 260
	public Vector3 m_spawnOffset = Vector3.zero;

	// Token: 0x04000105 RID: 261
	public bool m_copyProjectileRotation = true;

	// Token: 0x04000106 RID: 262
	public bool m_spawnRandomRotation;

	// Token: 0x04000107 RID: 263
	public bool m_spawnFacingRotation;

	// Token: 0x04000108 RID: 264
	public EffectList m_spawnOnHitEffects = new EffectList();

	// Token: 0x04000109 RID: 265
	public OnProjectileHit m_onHit;

	// Token: 0x0400010A RID: 266
	[Header("Projectile Spawning")]
	public bool m_spawnProjectileNewVelocity;

	// Token: 0x0400010B RID: 267
	public float m_spawnProjectileMinVel = 1f;

	// Token: 0x0400010C RID: 268
	public float m_spawnProjectileMaxVel = 5f;

	// Token: 0x0400010D RID: 269
	[Range(0f, 1f)]
	public float m_spawnProjectileRandomDir;

	// Token: 0x0400010E RID: 270
	public bool m_spawnProjectileHemisphereDir;

	// Token: 0x0400010F RID: 271
	public bool m_projectilesInheritHitData;

	// Token: 0x04000110 RID: 272
	public bool m_onlySpawnedProjectilesDealDamage;

	// Token: 0x04000111 RID: 273
	public bool m_divideDamageBetweenProjectiles;

	// Token: 0x04000112 RID: 274
	[Header("Rotate projectile")]
	public float m_rotateVisual;

	// Token: 0x04000113 RID: 275
	public float m_rotateVisualY;

	// Token: 0x04000114 RID: 276
	public float m_rotateVisualZ;

	// Token: 0x04000115 RID: 277
	public GameObject m_visual;

	// Token: 0x04000116 RID: 278
	public bool m_canChangeVisuals;

	// Token: 0x04000117 RID: 279
	private ZNetView m_nview;

	// Token: 0x04000118 RID: 280
	private GameObject m_attachParent;

	// Token: 0x04000119 RID: 281
	private Vector3 m_attachParentOffset;

	// Token: 0x0400011A RID: 282
	private Quaternion m_attachParentOffsetRot;

	// Token: 0x0400011B RID: 283
	private bool m_hasLeftShields = true;

	// Token: 0x0400011C RID: 284
	private Vector3 m_vel = Vector3.zero;

	// Token: 0x0400011D RID: 285
	private Character m_owner;

	// Token: 0x0400011E RID: 286
	private Skills.SkillType m_skill;

	// Token: 0x0400011F RID: 287
	private float m_raiseSkillAmount = 1f;

	// Token: 0x04000120 RID: 288
	private ItemDrop.ItemData m_weapon;

	// Token: 0x04000121 RID: 289
	private ItemDrop.ItemData m_ammo;

	// Token: 0x04000122 RID: 290
	[NonSerialized]
	public ItemDrop.ItemData m_spawnItem;

	// Token: 0x04000123 RID: 291
	private HitData m_originalHitData;

	// Token: 0x04000124 RID: 292
	private bool m_didHit;

	// Token: 0x04000125 RID: 293
	private int m_bounceCount;

	// Token: 0x04000126 RID: 294
	private bool m_didBounce;

	// Token: 0x04000127 RID: 295
	private bool m_changedVisual;

	// Token: 0x04000128 RID: 296
	[HideInInspector]
	public Vector3 m_startPoint;

	// Token: 0x04000129 RID: 297
	private bool m_haveStartPoint;

	// Token: 0x0400012A RID: 298
	private static int s_rayMaskSolids;
}
