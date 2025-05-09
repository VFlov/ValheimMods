using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000011 RID: 17
public class Character : MonoBehaviour, IDestructible, Hoverable, IWaterInteractable, IMonoUpdater
{
	// Token: 0x0600010D RID: 269 RVA: 0x0000D6E8 File Offset: 0x0000B8E8
	protected virtual void Awake()
	{
		Character.s_characters.Add(this);
		this.m_collider = base.GetComponent<CapsuleCollider>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_zanim = base.GetComponent<ZSyncAnimation>();
		this.m_nview = ((this.m_nViewOverride != null) ? this.m_nViewOverride : base.GetComponent<ZNetView>());
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_animEvent = this.m_animator.GetComponent<CharacterAnimEvent>();
		this.m_baseAI = base.GetComponent<BaseAI>();
		this.m_animator.logWarnings = false;
		this.m_visual = base.transform.Find("Visual").gameObject;
		this.m_lodGroup = this.m_visual.GetComponent<LODGroup>();
		this.m_head = Utils.GetBoneTransform(this.m_animator, HumanBodyBones.Head);
		this.m_body.maxDepenetrationVelocity = 2f;
		this.m_originalMass = this.m_body.mass;
		if (Character.s_smokeRayMask == 0)
		{
			Character.s_smokeRayMask = LayerMask.GetMask(new string[]
			{
				"smoke"
			});
			Character.s_characterLayer = LayerMask.NameToLayer("character");
			Character.s_characterNetLayer = LayerMask.NameToLayer("character_net");
			Character.s_characterGhostLayer = LayerMask.NameToLayer("character_ghost");
			Character.s_groundRayMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"terrain",
				"blocker",
				"vehicle"
			});
			Character.s_characterLayerMask = LayerMask.GetMask(new string[]
			{
				"character",
				"character_noenv"
			});
			Character.s_blockedRayMask = LayerMask.GetMask(new string[]
			{
				"piece",
				"Default",
				"static_solid",
				"Default_small",
				"terrain"
			});
		}
		if (this.m_lodGroup)
		{
			this.m_originalLocalRef = this.m_lodGroup.localReferencePoint;
		}
		this.m_seman = new SEMan(this, this.m_nview);
		if (this.m_nview.GetZDO() != null)
		{
			if (!this.IsPlayer())
			{
				this.m_tamed = this.m_nview.GetZDO().GetBool(ZDOVars.s_tamed, this.m_tamed);
				this.m_level = this.m_nview.GetZDO().GetInt(ZDOVars.s_level, 1);
				if (this.m_nview.IsOwner() && this.GetHealth() == this.GetMaxHealth())
				{
					this.SetupMaxHealth();
				}
			}
			this.m_nview.Register<HitData>("RPC_Damage", new Action<long, HitData>(this.RPC_Damage));
			this.m_nview.Register<float, bool>("RPC_Heal", new Action<long, float, bool>(this.RPC_Heal));
			this.m_nview.Register<float>("RPC_AddNoise", new Action<long, float>(this.RPC_AddNoise));
			this.m_nview.Register<Vector3>("RPC_Stagger", new Action<long, Vector3>(this.RPC_Stagger));
			this.m_nview.Register("RPC_ResetCloth", new Action<long>(this.RPC_ResetCloth));
			this.m_nview.Register<bool>("RPC_SetTamed", new Action<long, bool>(this.RPC_SetTamed));
			this.m_nview.Register<float>("RPC_FreezeFrame", new Action<long, float>(this.RPC_FreezeFrame));
			this.m_nview.Register<Vector3, Quaternion, bool>("RPC_TeleportTo", new Action<long, Vector3, Quaternion, bool>(this.RPC_TeleportTo));
		}
		if (!this.IsPlayer())
		{
			if (Game.m_enemySpeedSize != 1f && !this.InInterior())
			{
				base.transform.localScale *= Game.m_enemySpeedSize;
			}
			if (Game.m_worldLevel > 0)
			{
				base.transform.localScale *= 1f + (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyMoveSpeedMultiplier;
			}
		}
	}

	// Token: 0x0600010E RID: 270 RVA: 0x0000DABC File Offset: 0x0000BCBC
	protected virtual void OnEnable()
	{
		Character.Instances.Add(this);
	}

	// Token: 0x0600010F RID: 271 RVA: 0x0000DAC9 File Offset: 0x0000BCC9
	protected virtual void OnDisable()
	{
		Character.Instances.Remove(this);
	}

	// Token: 0x06000110 RID: 272 RVA: 0x0000DAD8 File Offset: 0x0000BCD8
	private void SetupMaxHealth()
	{
		int level = this.GetLevel();
		this.SetMaxHealth(this.GetMaxHealthBase() * (float)level);
	}

	// Token: 0x06000111 RID: 273 RVA: 0x0000DAFB File Offset: 0x0000BCFB
	protected virtual void Start()
	{
	}

	// Token: 0x06000112 RID: 274 RVA: 0x0000DAFD File Offset: 0x0000BCFD
	protected virtual void OnDestroy()
	{
		this.m_seman.OnDestroy();
		Character.s_characters.Remove(this);
		if (EnemyHud.instance != null)
		{
			EnemyHud.instance.RemoveCharacterHud(this);
		}
	}

	// Token: 0x06000113 RID: 275 RVA: 0x0000DB30 File Offset: 0x0000BD30
	public void SetLevel(int level)
	{
		if (level < 1)
		{
			return;
		}
		this.m_level = level;
		this.m_nview.GetZDO().Set(ZDOVars.s_level, level, false);
		this.SetupMaxHealth();
		if (this.m_onLevelSet != null)
		{
			this.m_onLevelSet(this.m_level);
		}
	}

	// Token: 0x06000114 RID: 276 RVA: 0x0000DB7F File Offset: 0x0000BD7F
	public int GetLevel()
	{
		return this.m_level;
	}

	// Token: 0x06000115 RID: 277 RVA: 0x0000DB87 File Offset: 0x0000BD87
	public virtual bool IsPlayer()
	{
		return false;
	}

	// Token: 0x06000116 RID: 278 RVA: 0x0000DB8A File Offset: 0x0000BD8A
	public Character.Faction GetFaction()
	{
		return this.m_faction;
	}

	// Token: 0x06000117 RID: 279 RVA: 0x0000DB92 File Offset: 0x0000BD92
	public string GetGroup()
	{
		return this.m_group;
	}

	// Token: 0x06000118 RID: 280 RVA: 0x0000DB9C File Offset: 0x0000BD9C
	public virtual void CustomFixedUpdate(float dt)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		ZDO zdo = this.m_nview.GetZDO();
		bool flag = zdo.IsOwner();
		bool visible = zdo.HasOwner();
		this.CalculateLiquidDepth();
		this.UpdateLayer();
		this.UpdateContinousEffects();
		this.UpdateWater(dt);
		this.UpdateGroundTilt(dt);
		this.SetVisible(visible);
		this.UpdateLookTransition(dt);
		if (flag)
		{
			this.UpdateGroundContact(dt);
			this.UpdateNoise(dt);
			this.m_seman.Update(zdo, dt);
			this.UpdateStagger(dt);
			this.UpdatePushback(dt);
			this.UpdateMotion(dt);
			this.UpdateSmoke(dt);
			this.UpdateLava(dt);
			this.UpdateAshlandsWater(dt);
			this.UpdateHeatDamage(dt);
			this.UnderWorldCheck(dt);
			this.UpdatePheromones(dt);
			this.SyncVelocity();
			if (this.m_groundForceTimer > 0f)
			{
				this.m_groundForceTimer -= dt;
			}
			this.CheckDeath();
		}
		this.UpdateHeatEffects(dt);
		if (this.IsPlayer() && Terminal.m_showTests)
		{
			Terminal.m_testList["Player.Zone"] = ZoneSystem.GetZone(base.transform.position).ToString();
		}
	}

	// Token: 0x06000119 RID: 281 RVA: 0x0000DCC8 File Offset: 0x0000BEC8
	private void UpdateLayer()
	{
		int layer = this.m_collider.gameObject.layer;
		if (layer == Character.s_characterLayer || layer == Character.s_characterNetLayer)
		{
			int num = Character.s_characterNetLayer;
			if (this.m_nview.IsOwner() && !this.IsAttached())
			{
				num = Character.s_characterLayer;
			}
			if (layer != num)
			{
				this.m_collider.gameObject.layer = num;
			}
		}
		if (this.m_disableWhileSleeping)
		{
			if (this.m_baseAI && this.m_baseAI.IsSleeping())
			{
				this.m_body.isKinematic = true;
				return;
			}
			this.m_body.isKinematic = false;
		}
	}

	// Token: 0x0600011A RID: 282 RVA: 0x0000DD68 File Offset: 0x0000BF68
	private void UnderWorldCheck(float dt)
	{
		if (this.IsDead())
		{
			return;
		}
		this.m_underWorldCheckTimer += dt;
		if (this.m_underWorldCheckTimer > 5f || this.IsPlayer())
		{
			this.m_underWorldCheckTimer = 0f;
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			if (base.transform.position.y < groundHeight - 1f)
			{
				Vector3 position = base.transform.position;
				position.y = groundHeight + 0.5f;
				base.transform.position = position;
				this.m_body.position = position;
				this.m_body.velocity = Vector3.zero;
			}
		}
	}

	// Token: 0x0600011B RID: 283 RVA: 0x0000DE20 File Offset: 0x0000C020
	private void UpdatePheromones(float dt)
	{
		if (this.m_pheromoneTimer >= 0f)
		{
			this.m_pheromoneTimer -= dt;
			if (this.m_pheromoneTimer <= 0f && this.m_pheromoneLoveEffect.HasEffects())
			{
				this.m_pheromoneTimer = 5f;
				foreach (Player player in Player.GetAllPlayers())
				{
					foreach (StatusEffect statusEffect in player.GetSEMan().GetStatusEffects())
					{
						SE_Stats se_Stats = statusEffect as SE_Stats;
						if (se_Stats != null && se_Stats.m_pheromoneTarget != null && se_Stats.m_pheromoneTarget.GetComponent<Character>().m_name == this.m_name)
						{
							this.m_pheromoneLoveEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
							MonsterAI monsterAI = this.GetBaseAI() as MonsterAI;
							if (monsterAI != null)
							{
								monsterAI.Alert();
							}
						}
					}
				}
			}
		}
	}

	// Token: 0x0600011C RID: 284 RVA: 0x0000DF6C File Offset: 0x0000C16C
	private void UpdateSmoke(float dt)
	{
		if (this.m_tolerateSmoke)
		{
			return;
		}
		this.m_smokeCheckTimer += dt;
		if (this.m_smokeCheckTimer > 2f)
		{
			this.m_smokeCheckTimer = 0f;
			if (Physics.CheckSphere(this.GetTopPoint() + Vector3.up * 0.1f, 0.5f, Character.s_smokeRayMask))
			{
				this.m_seman.AddStatusEffect(SEMan.s_statusEffectSmoked, true, 0, 0f);
				return;
			}
			this.m_seman.RemoveStatusEffect(SEMan.s_statusEffectSmoked, true);
		}
	}

	// Token: 0x0600011D RID: 285 RVA: 0x0000E000 File Offset: 0x0000C200
	private void UpdateLava(float dt)
	{
		this.m_lavaTimer += dt;
		this.m_aboveOrInLavaTimer += dt;
		if (!WorldGenerator.IsAshlands(base.transform.position.x, base.transform.position.z))
		{
			return;
		}
		Vector3 position = base.transform.position;
		this.m_lavaProximity = 0f;
		Vector3 vector;
		Heightmap.Biome biome;
		Heightmap.BiomeArea biomeArea;
		Heightmap heightmap;
		ZoneSystem.instance.GetGroundData(ref position, out vector, out biome, out biomeArea, out heightmap);
		if (heightmap != null)
		{
			this.m_lavaProximity = Mathf.Min(1f, Utils.SmoothStep(0.1f, 1f, heightmap.GetLava(position)));
		}
		if (this.m_lavaProximity > this.m_minLavaMaskThreshold)
		{
			this.m_aboveOrInLavaTimer = 0f;
		}
		this.m_lavaHeightFactor = base.transform.position.y - position.y;
		this.m_lavaHeightFactor = (this.m_lavaAirDamageHeight - this.m_lavaHeightFactor) / this.m_lavaAirDamageHeight;
		bool flag = false;
		RaycastHit raycastHit;
		if (this.m_lavaProximity > this.m_minLavaMaskThreshold && Physics.Raycast(base.transform.position + Vector3.up, Vector3.down, out raycastHit, 50f, Character.s_blockedRayMask) && raycastHit.collider.GetComponent<Heightmap>() == null)
		{
			flag = true;
		}
		if (!flag && this.IsRiding())
		{
			flag = true;
		}
		float num = 1f - this.GetEquipmentHeatResistanceModifier();
		if (Terminal.m_showTests && this.IsPlayer())
		{
			Terminal.m_testList["Lava/Height/Resist"] = string.Concat(new string[]
			{
				this.m_lavaProximity.ToString("0.00"),
				" / ",
				this.m_lavaHeightFactor.ToString("0.00"),
				" / ",
				num.ToString("0.00")
			});
		}
		if (this.m_lavaProximity > this.m_minLavaMaskThreshold && this.m_lavaHeightFactor > 0f && !flag)
		{
			this.m_lavaHeatLevel += this.m_lavaProximity * dt * this.m_heatBuildupBase * this.m_lavaHeightFactor * num;
			this.m_lavaTimer = 0f;
		}
		else if (biome == Heightmap.Biome.AshLands && this.m_dayHeatGainRunning != 0f && this.IsPlayer() && EnvMan.IsDay() && !this.IsUnderRoof() && this.GetEquipmentHeatResistanceModifier() < this.m_dayHeatEquipmentStop)
		{
			if (this.m_currentVel.magnitude > 0.1f && this.IsOnGround())
			{
				this.m_lavaHeatLevel += dt * this.m_dayHeatGainRunning * num;
			}
			else if (!this.InWater())
			{
				this.m_lavaHeatLevel += dt * this.m_dayHeatGainStill;
			}
			if (this.m_lavaHeatLevel > this.m_heatLevelFirstDamageThreshold)
			{
				this.m_lavaHeatLevel = this.m_heatLevelFirstDamageThreshold;
			}
		}
		else if (!this.InWater())
		{
			this.m_lavaHeatLevel -= dt * this.m_heatCooldownBase;
		}
		if (this.m_tolerateFire)
		{
			this.m_lavaHeatLevel = 0f;
			return;
		}
		this.m_lavaHeatLevel = Mathf.Clamp(this.m_lavaHeatLevel, 0f, 1f);
	}

	// Token: 0x0600011E RID: 286 RVA: 0x0000E32C File Offset: 0x0000C52C
	private void UpdateHeatEffects(float dt)
	{
		bool flag = false;
		if (Player.m_localPlayer == this)
		{
			GameCamera.instance.m_heatDistortImageEffect.m_intensity = (flag ? 0f : this.m_lavaHeatLevel);
			if (this.m_lavaHeatEffects.HasEffects())
			{
				if (this.m_lavaHeatLevel > 0f && this.m_lavaHeatParticles.Count == 0 && !this.IsDead())
				{
					GameObject[] array = this.m_lavaHeatEffects.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
					foreach (KeyValuePair<ParticleSystem, float> keyValuePair in this.m_lavaHeatParticles)
					{
						UnityEngine.Object.Destroy(keyValuePair.Key.gameObject);
					}
					foreach (ZSFX zsfx in this.m_lavaHeatAudio)
					{
						UnityEngine.Object.Destroy(zsfx.gameObject);
					}
					this.m_lavaHeatParticles.Clear();
					this.m_lavaHeatAudio.Clear();
					foreach (GameObject gameObject in array)
					{
						foreach (ParticleSystem particleSystem in gameObject.GetComponentsInChildren<ParticleSystem>())
						{
							this.m_lavaHeatParticles.Add(particleSystem, particleSystem.emissionRate);
						}
						foreach (ZSFX item in gameObject.GetComponentsInChildren<ZSFX>())
						{
							this.m_lavaHeatAudio.Add(item);
						}
					}
				}
				foreach (KeyValuePair<ParticleSystem, float> keyValuePair2 in this.m_lavaHeatParticles)
				{
					if (keyValuePair2.Key != null)
					{
						keyValuePair2.Key.emissionRate = this.m_lavaHeatLevel * keyValuePair2.Value;
					}
				}
				if (Player.m_localPlayer == this)
				{
					foreach (ZSFX zsfx2 in this.m_lavaHeatAudio)
					{
						if (zsfx2 != null)
						{
							zsfx2.SetVolumeModifier(this.IsDead() ? 0f : this.m_lavaHeatLevel);
						}
					}
				}
				if (this.IsDead())
				{
					foreach (KeyValuePair<ParticleSystem, float> keyValuePair3 in this.m_lavaHeatParticles)
					{
						ZNetView component = keyValuePair3.Key.gameObject.GetComponent<ZNetView>();
						if (component != null && component.IsValid() && component.IsOwner())
						{
							component.Destroy();
						}
					}
					foreach (ZSFX zsfx3 in this.m_lavaHeatAudio)
					{
						ZNetView component2 = zsfx3.gameObject.GetComponent<ZNetView>();
						if (component2 != null && component2.IsValid() && component2.IsOwner())
						{
							component2.Destroy();
						}
					}
					this.m_lavaHeatParticles.Clear();
					this.m_lavaHeatAudio.Clear();
				}
			}
			return;
		}
	}

	// Token: 0x0600011F RID: 287 RVA: 0x0000E6C4 File Offset: 0x0000C8C4
	private void UpdateHeatDamage(float dt)
	{
		bool flag = false;
		this.m_lavaDamageTimer += dt;
		if (this.m_lavaDamageTimer > this.m_lavaDamageTickInterval && !flag)
		{
			this.m_lavaDamageTimer = 0f;
			float num = this.InWater() ? 1f : Mathf.Max(this.m_lavaProximity, 0.05f);
			float num2 = 1f - this.GetEquipmentHeatResistanceModifier();
			if (this.m_lavaHeatLevel >= 1f)
			{
				if (!this.InWater() && this.m_lavaProximity > this.m_minLavaMaskThreshold)
				{
					this.m_seman.AddStatusEffect(SEMan.s_statusEffectBurning, true, 0, 0f);
				}
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = this.m_lavaFullDamage * num2 * num;
				hitData.m_point = this.m_lastGroundPoint;
				hitData.m_dir = this.m_lastGroundNormal;
				hitData.m_hitType = HitData.HitType.Burning;
				this.Damage(hitData);
				return;
			}
			if (this.m_lavaHeatLevel >= this.m_heatLevelFirstDamageThreshold)
			{
				if (!this.InWater() && this.m_lavaProximity > this.m_minLavaMaskThreshold)
				{
					this.m_seman.AddStatusEffect(SEMan.s_statusEffectBurning, true, 0, 0f);
				}
				HitData hitData2 = new HitData();
				hitData2.m_damage.m_damage = this.m_lavaFirstDamage * num2 * num;
				hitData2.m_point = this.m_lastGroundPoint;
				hitData2.m_dir = this.m_lastGroundNormal;
				hitData2.m_hitType = HitData.HitType.Burning;
				this.Damage(hitData2);
			}
		}
	}

	// Token: 0x06000120 RID: 288 RVA: 0x0000E830 File Offset: 0x0000CA30
	private bool IsUnderRoof()
	{
		return Physics.RaycastNonAlloc(base.transform.position + Vector3.up * 0.2f, Vector3.up, this.m_lavaRoofCheck, 20f, LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"piece"
		})) > 0;
	}

	// Token: 0x06000121 RID: 289 RVA: 0x0000E898 File Offset: 0x0000CA98
	private void UpdateAshlandsWater(float dt)
	{
		if (this.m_tolerateFire || !this.InWater())
		{
			return;
		}
		float num = WorldGenerator.GetAshlandsOceanGradient(base.transform.position);
		if (!this.IsSwimming())
		{
			num *= this.m_heatWaterTouchMultiplier;
		}
		if (num < 0f)
		{
			return;
		}
		num = Mathf.Clamp01(num);
		float num2 = 1f - this.GetEquipmentHeatResistanceModifier();
		this.m_lavaHeatLevel += num * dt * this.m_heatBuildupWater * num2;
		if (this.m_lavaHeatLevel > this.m_heatLevelFirstDamageThreshold)
		{
			this.m_lavaHeatLevel = this.m_heatLevelFirstDamageThreshold;
		}
	}

	// Token: 0x06000122 RID: 290 RVA: 0x0000E928 File Offset: 0x0000CB28
	private void UpdateContinousEffects()
	{
		Character.SetupContinuousEffect(base.transform, base.transform.position, this.m_sliding, this.m_slideEffects, ref this.m_slideEffects_instances);
		Vector3 position = base.transform.position;
		position.y = this.GetLiquidLevel() + 0.05f;
		EffectList effects = (this.InTar() && this.m_tarEffects.HasEffects()) ? this.m_tarEffects : this.m_waterEffects;
		Character.SetupContinuousEffect(base.transform, position, this.InLiquid(), effects, ref this.m_waterEffects_instances);
		Character.SetupContinuousEffect(base.transform, base.transform.position, this.IsFlying(), this.m_flyingContinuousEffect, ref this.m_flyingEffects_instances);
	}

	// Token: 0x06000123 RID: 291 RVA: 0x0000E9E4 File Offset: 0x0000CBE4
	public static void SetupContinuousEffect(Transform transform, Vector3 point, bool enabledEffect, EffectList effects, ref GameObject[] instances)
	{
		if (!effects.HasEffects())
		{
			return;
		}
		if (enabledEffect)
		{
			if (instances == null)
			{
				instances = effects.Create(point, Quaternion.identity, transform, 1f, -1);
				return;
			}
			foreach (GameObject gameObject in instances)
			{
				if (gameObject)
				{
					gameObject.transform.position = point;
				}
			}
			return;
		}
		else
		{
			if (instances == null)
			{
				return;
			}
			foreach (GameObject gameObject2 in instances)
			{
				if (gameObject2)
				{
					foreach (ParticleSystem particleSystem in gameObject2.GetComponentsInChildren<ParticleSystem>())
					{
						particleSystem.emission.enabled = false;
						particleSystem.Stop();
					}
					CamShaker componentInChildren = gameObject2.GetComponentInChildren<CamShaker>();
					if (componentInChildren)
					{
						UnityEngine.Object.Destroy(componentInChildren);
					}
					ZSFX componentInChildren2 = gameObject2.GetComponentInChildren<ZSFX>();
					if (componentInChildren2)
					{
						componentInChildren2.FadeOut();
					}
					TimedDestruction component = gameObject2.GetComponent<TimedDestruction>();
					if (component)
					{
						component.Trigger();
					}
					else
					{
						UnityEngine.Object.Destroy(gameObject2);
					}
				}
			}
			instances = null;
			return;
		}
	}

	// Token: 0x06000124 RID: 292 RVA: 0x0000EAFA File Offset: 0x0000CCFA
	protected virtual void OnSwimming(Vector3 targetVel, float dt)
	{
	}

	// Token: 0x06000125 RID: 293 RVA: 0x0000EAFC File Offset: 0x0000CCFC
	protected virtual void OnSneaking(float dt)
	{
	}

	// Token: 0x06000126 RID: 294 RVA: 0x0000EAFE File Offset: 0x0000CCFE
	protected virtual void OnJump()
	{
	}

	// Token: 0x06000127 RID: 295 RVA: 0x0000EB00 File Offset: 0x0000CD00
	protected virtual bool TakeInput()
	{
		return true;
	}

	// Token: 0x06000128 RID: 296 RVA: 0x0000EB03 File Offset: 0x0000CD03
	private float GetSlideAngle()
	{
		if (this.IsPlayer())
		{
			return 38f;
		}
		if (this.HaveRider())
		{
			return 45f;
		}
		return 90f;
	}

	// Token: 0x06000129 RID: 297 RVA: 0x0000EB26 File Offset: 0x0000CD26
	public bool HaveRider()
	{
		return this.m_baseAI && this.m_baseAI.HaveRider();
	}

	// Token: 0x0600012A RID: 298 RVA: 0x0000EB44 File Offset: 0x0000CD44
	private void ApplySlide(float dt, ref Vector3 currentVel, Vector3 bodyVel, bool running)
	{
		bool flag = this.CanWallRun();
		float num = Mathf.Clamp(Mathf.Acos(Mathf.Clamp01(((this.m_groundTilt != Character.GroundTiltType.None) ? this.m_groundTiltNormal : this.m_lastGroundNormal).y)) * 57.29578f, 0f, 90f);
		Vector3 lastGroundNormal = this.m_lastGroundNormal;
		lastGroundNormal.y = 0f;
		lastGroundNormal.Normalize();
		Vector3 velocity = this.m_body.velocity;
		Vector3 rhs = Vector3.Cross(this.m_lastGroundNormal, Vector3.up);
		Vector3 a = Vector3.Cross(this.m_lastGroundNormal, rhs);
		bool flag2 = currentVel.magnitude > 0.1f;
		if (num > this.GetSlideAngle())
		{
			if (running && flag && flag2)
			{
				this.m_slippage = 0f;
				this.m_wallRunning = true;
			}
			else
			{
				this.m_slippage = Mathf.MoveTowards(this.m_slippage, 1f, 1f * dt);
			}
			Vector3 b = a * 5f;
			currentVel = Vector3.Lerp(currentVel, b, this.m_slippage);
			this.m_sliding = (this.m_slippage > 0.5f);
			return;
		}
		this.m_slippage = 0f;
	}

	// Token: 0x0600012B RID: 299 RVA: 0x0000EC6C File Offset: 0x0000CE6C
	private void UpdateMotion(float dt)
	{
		this.UpdateBodyFriction();
		this.m_sliding = false;
		this.m_wallRunning = false;
		this.m_running = false;
		this.m_walking = false;
		if (this.IsDead())
		{
			return;
		}
		if (this.IsDebugFlying())
		{
			this.UpdateDebugFly(dt);
			return;
		}
		if (this.InIntro())
		{
			this.m_maxAirAltitude = base.transform.position.y;
			this.m_body.velocity = Vector3.zero;
			this.m_body.angularVelocity = Vector3.zero;
		}
		if (!this.InLiquidSwimDepth() && !this.IsOnGround() && !this.IsAttached())
		{
			float y = base.transform.position.y;
			this.m_maxAirAltitude = Mathf.Max(this.m_maxAirAltitude, y);
			this.m_fallTimer += dt;
			if (this.IsPlayer() && this.m_fallTimer > 0.1f)
			{
				this.m_zanim.SetBool(Character.s_animatorFalling, true);
			}
		}
		else
		{
			this.m_fallTimer = 0f;
			if (this.IsPlayer())
			{
				this.m_zanim.SetBool(Character.s_animatorFalling, false);
			}
		}
		if (this.IsSwimming())
		{
			this.UpdateSwimming(dt);
		}
		else if (this.m_flying)
		{
			this.UpdateFlying(dt);
		}
		else
		{
			if (this.m_baseAI && !this.m_baseAI.IsSleeping())
			{
				this.UpdateWalking(dt);
			}
			if (!this.m_baseAI)
			{
				this.UpdateWalking(dt);
			}
		}
		this.UpdateSinkingPlatform(dt);
		this.m_lastGroundTouch += Time.fixedDeltaTime;
		this.m_jumpTimer += Time.fixedDeltaTime;
		if (this.m_standUp > 0f)
		{
			this.m_standUp -= Time.fixedDeltaTime;
		}
	}

	// Token: 0x0600012C RID: 300 RVA: 0x0000EE28 File Offset: 0x0000D028
	private void UpdateSinkingPlatform(float dt)
	{
		if (this != Player.m_localPlayer || this.InLiquidSwimDepth() || this.IsOnGround() || this.IsAttached())
		{
			return;
		}
		if (this.m_lastGroundBody != null && this.m_lastGroundBody.GetComponent<Leviathan>() != null)
		{
			StaticPhysics component = this.m_lastGroundBody.GetComponent<StaticPhysics>();
			if (component != null && component.IsFalling)
			{
				base.transform.position = base.transform.position + this.m_lastGroundBody.velocity * dt;
				if (this.m_fallTimer > 0.1f)
				{
					this.m_zanim.SetBool(Character.s_animatorFalling, false);
				}
			}
		}
	}

	// Token: 0x0600012D RID: 301 RVA: 0x0000EEDD File Offset: 0x0000D0DD
	public static void SetTakeInputDelay(float delayInSeconds)
	{
		Character.takeInputDelay = delayInSeconds;
	}

	// Token: 0x0600012E RID: 302 RVA: 0x0000EEE8 File Offset: 0x0000D0E8
	private void UpdateDebugFly(float dt)
	{
		Character.takeInputDelay = Mathf.Max(0f, Character.takeInputDelay - dt);
		float num = this.m_run ? ((float)Character.m_debugFlySpeed * 2.5f) : ((float)Character.m_debugFlySpeed);
		Vector3 b = this.m_moveDir * num;
		if (this.TakeInput())
		{
			if ((ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump")) && Character.takeInputDelay <= 0f && !Hud.IsPieceSelectionVisible())
			{
				b.y = num;
			}
			else if (ZInput.GetKey(KeyCode.LeftControl, true) || ZInput.GetButtonPressedTimer("JoyCrouch") > 0.33f)
			{
				b.y = -num;
			}
		}
		this.m_currentVel = Vector3.Lerp(this.m_currentVel, b, 0.5f);
		this.m_body.velocity = this.m_currentVel;
		this.m_body.useGravity = false;
		this.m_lastGroundTouch = 0f;
		this.m_maxAirAltitude = base.transform.position.y;
		this.m_body.rotation = Quaternion.RotateTowards(base.transform.rotation, this.m_lookYaw, this.m_turnSpeed * dt);
		this.m_body.angularVelocity = Vector3.zero;
		this.UpdateEyeRotation();
	}

	// Token: 0x0600012F RID: 303 RVA: 0x0000F030 File Offset: 0x0000D230
	private void UpdateSwimming(float dt)
	{
		bool flag = this.IsOnGround();
		if (Mathf.Max(0f, this.m_maxAirAltitude - base.transform.position.y) > 0.5f && this.m_onLand != null)
		{
			this.m_onLand(new Vector3(base.transform.position.x, this.GetLiquidLevel(), base.transform.position.z));
		}
		this.m_maxAirAltitude = base.transform.position.y;
		float d = this.m_swimSpeed * this.GetAttackSpeedFactorMovement();
		if (this.InMinorActionSlowdown())
		{
			d = 0f;
		}
		this.m_seman.ApplyStatusEffectSpeedMods(ref d, this.m_moveDir);
		Vector3 vector = this.m_moveDir * d;
		if (this.IsPlayer())
		{
			this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, this.m_swimAcceleration);
		}
		else
		{
			float num = vector.magnitude;
			float magnitude = this.m_currentVel.magnitude;
			if (num > magnitude)
			{
				num = Mathf.MoveTowards(magnitude, num, this.m_swimAcceleration);
				vector = vector.normalized * num;
			}
			this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, 0.5f);
		}
		if (this.m_currentVel.magnitude > 0.1f)
		{
			this.AddNoise(15f);
		}
		this.AddPushbackForce(ref this.m_currentVel);
		Vector3 force = this.m_currentVel - this.m_body.velocity;
		force.y = 0f;
		if (force.magnitude > 20f)
		{
			force = force.normalized * 20f;
		}
		this.m_body.AddForce(force, ForceMode.VelocityChange);
		float num2 = this.GetLiquidLevel() - this.m_swimDepth;
		if (base.transform.position.y < num2)
		{
			float t = Mathf.Clamp01((num2 - base.transform.position.y) / 2f);
			float target = Mathf.Lerp(0f, 10f, t);
			Vector3 velocity = this.m_body.velocity;
			velocity.y = Mathf.MoveTowards(velocity.y, target, 50f * dt);
			this.m_body.velocity = velocity;
		}
		else
		{
			float t2 = Mathf.Clamp01(-(num2 - base.transform.position.y) / 1f);
			float num3 = Mathf.Lerp(0f, 10f, t2);
			Vector3 velocity2 = this.m_body.velocity;
			velocity2.y = Mathf.MoveTowards(velocity2.y, -num3, 30f * dt);
			this.m_body.velocity = velocity2;
		}
		float target2 = 0f;
		if (this.m_moveDir.magnitude > 0.1f || this.AlwaysRotateCamera())
		{
			float swimTurnSpeed = this.m_swimTurnSpeed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref swimTurnSpeed, this.m_currentVel);
			target2 = this.UpdateRotation(swimTurnSpeed, dt, false);
		}
		this.m_body.angularVelocity = Vector3.zero;
		this.UpdateEyeRotation();
		this.m_body.useGravity = true;
		float value = (this.IsPlayer() || this.HaveRider()) ? Vector3.Dot(this.m_currentVel, base.transform.forward) : Vector3.Dot(this.m_body.velocity, base.transform.forward);
		float value2 = Vector3.Dot(this.m_currentVel, base.transform.right);
		this.m_currentTurnVel = Mathf.SmoothDamp(this.m_currentTurnVel, target2, ref this.m_currentTurnVelChange, 0.5f, 99f);
		this.m_zanim.SetFloat(Character.s_forwardSpeed, value);
		this.m_zanim.SetFloat(Character.s_sidewaySpeed, value2);
		this.m_zanim.SetFloat(Character.s_turnSpeed, this.m_currentTurnVel);
		this.m_zanim.SetBool(Character.s_inWater, !flag);
		this.m_zanim.SetBool(Character.s_onGround, false);
		this.m_zanim.SetBool(Character.s_encumbered, false);
		this.m_zanim.SetBool(Character.s_flying, false);
		if (!flag)
		{
			this.OnSwimming(vector, dt);
		}
	}

	// Token: 0x06000130 RID: 304 RVA: 0x0000F45C File Offset: 0x0000D65C
	private void UpdateFlying(float dt)
	{
		float d = (this.m_run ? this.m_flyFastSpeed : this.m_flySlowSpeed) * this.GetAttackSpeedFactorMovement();
		Vector3 b = this.CanMove() ? (this.m_moveDir * d) : Vector3.zero;
		this.m_currentVel = Vector3.Lerp(this.m_currentVel, b, this.m_acceleration);
		this.m_maxAirAltitude = base.transform.position.y;
		this.ApplyRootMotion(ref this.m_currentVel);
		this.AddPushbackForce(ref this.m_currentVel);
		Vector3 force = this.m_currentVel - this.m_body.velocity;
		if (force.magnitude > 20f)
		{
			force = force.normalized * 20f;
		}
		this.m_body.AddForce(force, ForceMode.VelocityChange);
		float target = 0f;
		if ((this.m_moveDir.magnitude > 0.1f || this.AlwaysRotateCamera()) && !this.InDodge() && this.CanMove())
		{
			float flyTurnSpeed = this.m_flyTurnSpeed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref flyTurnSpeed, this.m_currentVel);
			target = this.UpdateRotation(flyTurnSpeed, dt, true);
		}
		this.m_body.angularVelocity = Vector3.zero;
		this.UpdateEyeRotation();
		this.m_body.useGravity = false;
		float num = Vector3.Dot(this.m_currentVel, base.transform.forward);
		float value = Vector3.Dot(this.m_currentVel, base.transform.right);
		float num2 = Vector3.Dot(this.m_body.velocity, base.transform.forward);
		this.m_currentTurnVel = Mathf.SmoothDamp(this.m_currentTurnVel, target, ref this.m_currentTurnVelChange, 0.5f, 99f);
		this.m_zanim.SetFloat(Character.s_forwardSpeed, this.IsPlayer() ? num : num2);
		this.m_zanim.SetFloat(Character.s_sidewaySpeed, value);
		this.m_zanim.SetFloat(Character.s_turnSpeed, this.m_currentTurnVel);
		this.m_zanim.SetBool(Character.s_inWater, false);
		this.m_zanim.SetBool(Character.s_onGround, false);
		this.m_zanim.SetBool(Character.s_encumbered, false);
		this.m_zanim.SetBool(Character.s_flying, true);
	}

	// Token: 0x06000131 RID: 305 RVA: 0x0000F6A0 File Offset: 0x0000D8A0
	private void UpdateWalking(float dt)
	{
		Vector3 moveDir = this.m_moveDir;
		bool flag = this.IsCrouching();
		this.m_running = this.CheckRun(moveDir, dt);
		float num = this.m_speed * this.GetJogSpeedFactor();
		if ((this.m_walk || this.InMinorActionSlowdown()) && !flag)
		{
			num = this.m_walkSpeed;
			this.m_walking = (moveDir.magnitude > 0.1f);
		}
		else if (this.m_running)
		{
			num = this.m_runSpeed * this.GetRunSpeedFactor();
			if (this.IsPlayer() && moveDir.magnitude > 0f)
			{
				moveDir.Normalize();
			}
		}
		else if (flag || this.IsEncumbered())
		{
			num = this.m_crouchSpeed;
		}
		if (!this.IsPlayer())
		{
			if (!this.InInterior())
			{
				num *= Game.m_enemySpeedSize;
			}
			num *= 1f + (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyMoveSpeedMultiplier;
		}
		this.ApplyLiquidResistance(ref num);
		num *= this.GetAttackSpeedFactorMovement();
		this.m_seman.ApplyStatusEffectSpeedMods(ref num, moveDir);
		if (this.m_lavaProximity > 0.33f && this.m_lavaHeightFactor > this.m_lavaSlowHeight)
		{
			float num2 = (this.m_lavaProximity - 0.33f) * 1.4925374f;
			num *= 1f - num2 * this.m_lavaSlowMax;
		}
		if (Terminal.m_showTests && Player.m_localPlayer == this)
		{
			Terminal.m_testList["Player Speed"] = num.ToString("0.000");
		}
		Vector3 vector = this.CanMove() ? (moveDir * num) : Vector3.zero;
		if (vector.magnitude > 0f && this.IsOnGround())
		{
			vector = Vector3.ProjectOnPlane(vector, this.m_lastGroundNormal).normalized * vector.magnitude;
		}
		float num3 = vector.magnitude;
		float magnitude = this.m_currentVel.magnitude;
		if (num3 > magnitude)
		{
			num3 = Mathf.MoveTowards(magnitude, num3, this.m_acceleration);
			vector = vector.normalized * num3;
		}
		else
		{
			num3 = Mathf.MoveTowards(magnitude, num3, this.m_acceleration * 2f);
			vector = ((vector.magnitude > 0f) ? (vector.normalized * num3) : (this.m_currentVel.normalized * num3));
		}
		this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, 0.5f);
		Vector3 velocity = this.m_body.velocity;
		Vector3 currentVel = this.m_currentVel;
		currentVel.y = velocity.y;
		if (this.IsOnGround() && this.m_lastAttachBody == null)
		{
			this.ApplySlide(dt, ref currentVel, velocity, this.m_running);
			currentVel.y = Mathf.Min(currentVel.y, 3f);
		}
		this.ApplyRootMotion(ref currentVel);
		this.AddPushbackForce(ref currentVel);
		this.ApplyGroundForce(ref currentVel, vector);
		Vector3 vector2 = currentVel - velocity;
		if (!this.IsOnGround())
		{
			if (vector.magnitude > 0.1f)
			{
				vector2 *= this.m_airControl;
			}
			else
			{
				vector2 = Vector3.zero;
			}
		}
		if (this.IsAttached())
		{
			vector2 = Vector3.zero;
		}
		if (vector2.magnitude > 20f)
		{
			vector2 = vector2.normalized * 20f;
		}
		if (vector2.magnitude > 0.01f)
		{
			this.m_body.AddForce(vector2, ForceMode.VelocityChange);
		}
		Vector3 velocity2 = this.m_body.velocity;
		this.m_seman.ModifyWalkVelocity(ref velocity2);
		this.m_body.velocity = velocity2;
		if (this.m_lastGroundBody && this.m_lastGroundBody.gameObject.layer != base.gameObject.layer && this.m_lastGroundBody.mass > this.m_body.mass)
		{
			float d = this.m_body.mass / this.m_lastGroundBody.mass;
			this.m_lastGroundBody.AddForceAtPosition(-vector2 * d, base.transform.position, ForceMode.VelocityChange);
		}
		float target = 0f;
		if (((moveDir.magnitude > 0.1f || this.AlwaysRotateCamera() || this.m_standUp > 0f) && !this.InDodge() && this.CanMove()) || this.m_groundContact)
		{
			float turnSpeed = this.m_run ? this.m_runTurnSpeed : this.m_turnSpeed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref turnSpeed, this.m_currentVel);
			target = this.UpdateRotation(turnSpeed, dt, false);
		}
		if (this.IsSneaking())
		{
			this.OnSneaking(dt);
		}
		this.UpdateEyeRotation();
		this.m_body.useGravity = true;
		float num4 = Vector3.Dot(this.m_currentVel, Vector3.ProjectOnPlane(base.transform.forward, this.m_lastGroundNormal).normalized);
		float num5 = Vector3.Dot(this.m_body.velocity, this.m_visual.transform.forward);
		if (this.IsRiding())
		{
			num4 = num5;
		}
		else if (!this.IsPlayer() && !this.HaveRider())
		{
			num4 = Mathf.Min(num4, num5);
		}
		float value = Vector3.Dot(this.m_currentVel, Vector3.ProjectOnPlane(base.transform.right, this.m_lastGroundNormal).normalized);
		this.m_currentTurnVel = Mathf.SmoothDamp(this.m_currentTurnVel, target, ref this.m_currentTurnVelChange, 0.5f, 99f);
		this.m_zanim.SetFloat(Character.s_forwardSpeed, num4);
		this.m_zanim.SetFloat(Character.s_sidewaySpeed, value);
		this.m_zanim.SetFloat(Character.s_turnSpeed, this.m_currentTurnVel);
		this.m_zanim.SetBool(Character.s_inWater, false);
		this.m_zanim.SetBool(Character.s_onGround, this.IsOnGround());
		this.m_zanim.SetBool(Character.s_encumbered, this.IsEncumbered());
		this.m_zanim.SetBool(Character.s_flying, false);
		if (this.m_currentVel.magnitude > 0.1f)
		{
			if (this.m_running)
			{
				this.AddNoise(30f);
				return;
			}
			if (!flag)
			{
				this.AddNoise(15f);
			}
		}
	}

	// Token: 0x06000132 RID: 306 RVA: 0x0000FCBE File Offset: 0x0000DEBE
	public bool IsSneaking()
	{
		return this.IsCrouching() && this.m_currentVel.magnitude > 0.1f && this.IsOnGround();
	}

	// Token: 0x06000133 RID: 307 RVA: 0x0000FCE4 File Offset: 0x0000DEE4
	private float GetSlopeAngle()
	{
		if (!this.IsOnGround())
		{
			return 0f;
		}
		float num = Vector3.SignedAngle(base.transform.forward, this.m_lastGroundNormal, base.transform.right);
		return -(90f - -num);
	}

	// Token: 0x06000134 RID: 308 RVA: 0x0000FD2C File Offset: 0x0000DF2C
	protected void AddPushbackForce(ref Vector3 velocity)
	{
		if (this.m_pushForce != Vector3.zero)
		{
			Vector3 normalized = this.m_pushForce.normalized;
			float num = Vector3.Dot(normalized, velocity);
			if (num < 20f)
			{
				velocity += normalized * (20f - num);
			}
			if (this.IsSwimming() || this.m_flying)
			{
				velocity *= 0.5f;
			}
		}
	}

	// Token: 0x06000135 RID: 309 RVA: 0x0000FDB0 File Offset: 0x0000DFB0
	private void ApplyPushback(HitData hit)
	{
		this.ApplyPushback(hit.m_dir, hit.m_pushForce);
	}

	// Token: 0x06000136 RID: 310 RVA: 0x0000FDC4 File Offset: 0x0000DFC4
	public void ApplyPushback(Vector3 dir, float pushForce)
	{
		if (pushForce != 0f && dir != Vector3.zero)
		{
			float d = pushForce * Mathf.Clamp01(1f + this.GetEquipmentMovementModifier()) / this.m_body.mass * 2.5f;
			dir.y = 0f;
			dir.Normalize();
			Vector3 pushForce2 = dir * d;
			if (this.m_pushForce.magnitude < pushForce2.magnitude)
			{
				this.m_pushForce = pushForce2;
			}
		}
	}

	// Token: 0x06000137 RID: 311 RVA: 0x0000FE42 File Offset: 0x0000E042
	private void UpdatePushback(float dt)
	{
		this.m_pushForce = Vector3.MoveTowards(this.m_pushForce, Vector3.zero, 100f * dt);
	}

	// Token: 0x06000138 RID: 312 RVA: 0x0000FE61 File Offset: 0x0000E061
	public void TimeoutGroundForce(float time)
	{
		this.m_groundForceTimer = time;
	}

	// Token: 0x06000139 RID: 313 RVA: 0x0000FE6C File Offset: 0x0000E06C
	private void ApplyGroundForce(ref Vector3 vel, Vector3 targetVel)
	{
		Vector3 vector = Vector3.zero;
		if (this.IsOnGround() && this.m_lastGroundBody && this.m_groundForceTimer <= 0f)
		{
			vector = this.m_lastGroundBody.GetPointVelocity(base.transform.position);
			vector.y = 0f;
		}
		Ship standingOnShip = this.GetStandingOnShip();
		if (standingOnShip != null)
		{
			if (targetVel.magnitude > 0.01f)
			{
				this.m_lastAttachBody = null;
			}
			else if (this.m_lastAttachBody != this.m_lastGroundBody)
			{
				this.m_lastAttachBody = this.m_lastGroundBody;
				this.m_lastAttachPos = this.m_lastAttachBody.transform.InverseTransformPoint(this.m_body.position);
			}
			if (this.m_lastAttachBody)
			{
				Vector3 vector2 = this.m_lastAttachBody.transform.TransformPoint(this.m_lastAttachPos);
				Vector3 a = vector2 - this.m_body.position;
				if (a.magnitude < 4f)
				{
					Vector3 position = vector2;
					position.y = this.m_body.position.y;
					if (standingOnShip.IsOwner())
					{
						a.y = 0f;
						vector += a * 10f;
					}
					else
					{
						this.m_body.position = position;
					}
				}
				else
				{
					this.m_lastAttachBody = null;
				}
			}
		}
		else
		{
			this.m_lastAttachBody = null;
		}
		vel += vector;
	}

	// Token: 0x0600013A RID: 314 RVA: 0x0000FFE8 File Offset: 0x0000E1E8
	private float UpdateRotation(float turnSpeed, float dt, bool smooth)
	{
		Quaternion quaternion = (this.AlwaysRotateCamera() || this.m_moveDir == Vector3.zero) ? this.m_lookYaw : Quaternion.LookRotation(this.m_moveDir);
		float yawDeltaAngle = Utils.GetYawDeltaAngle(base.transform.rotation, quaternion);
		float num = 1f;
		if (!this.IsPlayer())
		{
			num = Mathf.Clamp01(Mathf.Abs(yawDeltaAngle) / 90f);
			num = Mathf.Pow(num, 0.5f);
			float num2 = Mathf.Clamp01(Mathf.Abs(yawDeltaAngle) / 90f);
			num2 = Mathf.Pow(num2, 0.5f);
			if (smooth)
			{
				this.currentRotSpeedFactor = Mathf.MoveTowards(this.currentRotSpeedFactor, num2, dt);
				num = this.currentRotSpeedFactor;
			}
			else
			{
				num = num2;
			}
		}
		float num3 = turnSpeed * this.GetAttackSpeedFactorRotation() * num;
		Quaternion rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, num3 * dt);
		if (Mathf.Abs(yawDeltaAngle) > 0.001f)
		{
			base.transform.rotation = rotation;
		}
		return num3 * Mathf.Sign(yawDeltaAngle) * 0.017453292f;
	}

	// Token: 0x0600013B RID: 315 RVA: 0x000100F0 File Offset: 0x0000E2F0
	private void UpdateGroundTilt(float dt)
	{
		if (this.m_visual == null)
		{
			return;
		}
		if (this.m_baseAI && this.m_baseAI.IsSleeping())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			if (this.m_groundTilt != Character.GroundTiltType.None)
			{
				if (!this.IsFlying() && this.IsOnGround() && !this.IsAttached())
				{
					Vector3 vector = this.m_lastGroundNormal;
					if (this.m_groundTilt == Character.GroundTiltType.PitchRaycast || this.m_groundTilt == Character.GroundTiltType.FullRaycast)
					{
						Vector3 p = base.transform.position + base.transform.forward * this.m_collider.radius;
						Vector3 p2 = base.transform.position - base.transform.forward * this.m_collider.radius;
						float num;
						Vector3 b;
						this.GetGroundHeight(p, out num, out b);
						float num2;
						Vector3 b2;
						this.GetGroundHeight(p2, out num2, out b2);
						vector = (vector + b + b2).normalized;
					}
					Vector3 vector2 = base.transform.InverseTransformVector(vector);
					vector2 = Vector3.RotateTowards(Vector3.up, vector2, 0.87266463f, 1f);
					this.m_groundTiltNormal = Vector3.Lerp(this.m_groundTiltNormal, vector2, 0.05f);
					Vector3 vector3;
					if (this.m_groundTilt == Character.GroundTiltType.Pitch || this.m_groundTilt == Character.GroundTiltType.PitchRaycast)
					{
						Vector3 b3 = Vector3.Project(this.m_groundTiltNormal, Vector3.right);
						vector3 = this.m_groundTiltNormal - b3;
					}
					else
					{
						vector3 = this.m_groundTiltNormal;
					}
					Quaternion to = Quaternion.LookRotation(Vector3.Cross(vector3, Vector3.left), vector3);
					this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, to, dt * this.m_groundTiltSpeed);
				}
				else if (this.IsFlying() && !this.IsOnGround() && this.m_groundTilt == Character.GroundTiltType.Flying && this.m_currentVel.sqrMagnitude > 0f)
				{
					this.m_groundTiltNormal = Vector3.Cross(base.transform.InverseTransformVector(this.m_currentVel.normalized), Vector3.right);
					Quaternion quaternion = Quaternion.LookRotation(Vector3.Cross(this.m_groundTiltNormal, Vector3.left), this.m_groundTiltNormal);
					quaternion = Quaternion.Lerp(Quaternion.identity, quaternion, this.m_currentVel.magnitude * 0.33f);
					this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, quaternion, dt * this.m_groundTiltSpeed);
				}
				else
				{
					this.m_groundTiltNormal = Vector3.up;
					if (this.IsSwimming())
					{
						this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, Quaternion.identity, dt * this.m_groundTiltSpeed);
					}
					else
					{
						this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, Quaternion.identity, dt * this.m_groundTiltSpeed * 2f);
					}
				}
				Quaternion localRotation = this.m_visual.transform.localRotation;
				if (!localRotation.Equals(this.m_tiltRotCached))
				{
					this.m_nview.GetZDO().Set(ZDOVars.s_tiltrot, localRotation);
					this.m_tiltRotCached = localRotation;
				}
			}
			else if (this.CanWallRun())
			{
				if (this.m_wallRunning)
				{
					Vector3 vector4 = Vector3.Lerp(Vector3.up, this.m_lastGroundNormal, 0.65f);
					Vector3 forward = Vector3.ProjectOnPlane(base.transform.forward, vector4);
					forward.Normalize();
					Quaternion to2 = Quaternion.LookRotation(forward, vector4);
					this.m_visual.transform.rotation = Quaternion.RotateTowards(this.m_visual.transform.rotation, to2, 30f * dt);
				}
				else
				{
					this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, Quaternion.identity, dt * this.m_groundTiltSpeed * 2f);
				}
				Quaternion localRotation2 = this.m_visual.transform.localRotation;
				if (!localRotation2.Equals(this.m_tiltRotCached))
				{
					this.m_nview.GetZDO().Set(ZDOVars.s_tiltrot, localRotation2);
					this.m_tiltRotCached = localRotation2;
				}
			}
		}
		else if (this.m_groundTilt != Character.GroundTiltType.None || this.CanWallRun())
		{
			Quaternion quaternion2 = this.m_nview.GetZDO().GetQuaternion(ZDOVars.s_tiltrot, Quaternion.identity);
			this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, quaternion2, dt * this.m_groundTiltSpeed);
		}
		this.m_animator.SetFloat(Character.s_tilt, Vector3.Dot(this.m_visual.transform.forward, Vector3.up));
	}

	// Token: 0x0600013C RID: 316 RVA: 0x000105D8 File Offset: 0x0000E7D8
	private bool GetGroundHeight(Vector3 p, out float height, out Vector3 normal)
	{
		p.y += 10f;
		RaycastHit raycastHit;
		if (Physics.Raycast(p, Vector3.down, out raycastHit, 20f, Character.s_groundRayMask))
		{
			height = raycastHit.point.y;
			normal = raycastHit.normal;
			return true;
		}
		height = p.y;
		normal = Vector3.zero;
		return false;
	}

	// Token: 0x0600013D RID: 317 RVA: 0x0001063F File Offset: 0x0000E83F
	public bool IsWallRunning()
	{
		return this.m_wallRunning;
	}

	// Token: 0x0600013E RID: 318 RVA: 0x00010647 File Offset: 0x0000E847
	private bool IsOnSnow()
	{
		return false;
	}

	// Token: 0x0600013F RID: 319 RVA: 0x0001064C File Offset: 0x0000E84C
	public void Heal(float hp, bool showText = true)
	{
		if (hp <= 0f)
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_Heal(0L, hp, showText);
			return;
		}
		this.m_nview.InvokeRPC("RPC_Heal", new object[]
		{
			hp,
			showText
		});
	}

	// Token: 0x06000140 RID: 320 RVA: 0x000106A4 File Offset: 0x0000E8A4
	private void RPC_Heal(long sender, float hp, bool showText)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float health = this.GetHealth();
		if (health <= 0f || this.IsDead())
		{
			return;
		}
		float num = Mathf.Min(health + hp, this.GetMaxHealth());
		if (num > health)
		{
			this.SetHealth(num);
			if (showText)
			{
				Vector3 topPoint = this.GetTopPoint();
				DamageText.instance.ShowText(DamageText.TextType.Heal, topPoint, hp, this.IsPlayer());
			}
		}
	}

	// Token: 0x06000141 RID: 321 RVA: 0x00010710 File Offset: 0x0000E910
	public Vector3 GetTopPoint()
	{
		return base.transform.TransformPoint(this.m_collider.center) + this.m_visual.transform.up * (this.m_collider.height * 0.5f);
	}

	// Token: 0x06000142 RID: 322 RVA: 0x00010760 File Offset: 0x0000E960
	public float GetRadius()
	{
		if (this.IsPlayer())
		{
			return this.m_collider.radius;
		}
		return this.m_collider.radius * base.transform.localScale.magnitude;
	}

	// Token: 0x06000143 RID: 323 RVA: 0x000107A0 File Offset: 0x0000E9A0
	public float GetHeight()
	{
		return Mathf.Max(this.m_collider.height, this.m_collider.radius * 2f);
	}

	// Token: 0x06000144 RID: 324 RVA: 0x000107C3 File Offset: 0x0000E9C3
	public Vector3 GetHeadPoint()
	{
		return this.m_head.position;
	}

	// Token: 0x06000145 RID: 325 RVA: 0x000107D0 File Offset: 0x0000E9D0
	public Vector3 GetEyePoint()
	{
		return this.m_eye.position;
	}

	// Token: 0x06000146 RID: 326 RVA: 0x000107E0 File Offset: 0x0000E9E0
	public Vector3 GetCenterPoint()
	{
		return this.m_collider.bounds.center;
	}

	// Token: 0x06000147 RID: 327 RVA: 0x00010800 File Offset: 0x0000EA00
	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Character;
	}

	// Token: 0x06000148 RID: 328 RVA: 0x00010804 File Offset: 0x0000EA04
	private short FindWeakSpotIndex(Collider c)
	{
		if (c == null || this.m_weakSpots == null || this.m_weakSpots.Length == 0)
		{
			return -1;
		}
		short num = 0;
		while ((int)num < this.m_weakSpots.Length)
		{
			if (this.m_weakSpots[(int)num].m_collider == c)
			{
				return num;
			}
			num += 1;
		}
		return -1;
	}

	// Token: 0x06000149 RID: 329 RVA: 0x00010859 File Offset: 0x0000EA59
	private WeakSpot GetWeakSpot(short index)
	{
		if (index < 0 || (int)index >= this.m_weakSpots.Length)
		{
			return null;
		}
		return this.m_weakSpots[(int)index];
	}

	// Token: 0x0600014A RID: 330 RVA: 0x00010874 File Offset: 0x0000EA74
	public void Damage(HitData hit)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		hit.m_weakSpot = this.FindWeakSpotIndex(hit.m_hitCollider);
		this.m_nview.InvokeRPC("RPC_Damage", new object[]
		{
			hit
		});
	}

	// Token: 0x0600014B RID: 331 RVA: 0x000108B0 File Offset: 0x0000EAB0
	private void RPC_Damage(long sender, HitData hit)
	{
		if (this.IsDebugFlying())
		{
			return;
		}
		if (hit.GetAttacker() == Player.m_localPlayer)
		{
			Game.instance.IncrementPlayerStat(this.IsPlayer() ? PlayerStatType.PlayerHits : PlayerStatType.EnemyHits, 1f);
			this.m_localPlayerHasHit = true;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetHealth() <= 0f || this.IsDead() || this.IsTeleporting() || this.InCutscene())
		{
			return;
		}
		if (hit.m_dodgeable && this.IsDodgeInvincible())
		{
			return;
		}
		Character attacker = hit.GetAttacker();
		if (hit.HaveAttacker() && attacker == null)
		{
			return;
		}
		if (this.IsPlayer() && !this.IsPVPEnabled() && attacker != null && attacker.IsPlayer() && !hit.m_ignorePVP)
		{
			return;
		}
		if (attacker != null && !attacker.IsPlayer())
		{
			float difficultyDamageScalePlayer = Game.instance.GetDifficultyDamageScalePlayer(base.transform.position);
			hit.ApplyModifier(difficultyDamageScalePlayer);
			hit.ApplyModifier(Game.m_enemyDamageRate);
		}
		this.m_seman.OnDamaged(hit, attacker);
		if (this.m_baseAI != null && this.m_baseAI.IsAggravatable() && !this.m_baseAI.IsAggravated() && attacker && attacker.IsPlayer() && hit.GetTotalDamage() > 0f)
		{
			BaseAI.AggravateAllInArea(base.transform.position, 20f, BaseAI.AggravatedReason.Damage);
		}
		if (this.m_baseAI != null && !this.m_baseAI.IsAlerted() && hit.m_backstabBonus > 1f && Time.time - this.m_backstabTime > 300f && (!ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs) || !this.m_baseAI.CanSeeTarget(attacker)))
		{
			this.m_backstabTime = Time.time;
			hit.ApplyModifier(hit.m_backstabBonus);
			this.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
		}
		if (this.IsStaggering() && !this.IsPlayer())
		{
			hit.ApplyModifier(2f);
			this.m_critHitEffects.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
		}
		if (hit.m_blockable && this.IsBlocking())
		{
			this.BlockAttack(hit, attacker);
		}
		this.ApplyPushback(hit);
		if (hit.m_statusEffectHash != 0)
		{
			StatusEffect statusEffect = this.m_seman.GetStatusEffect(hit.m_statusEffectHash);
			if (statusEffect == null)
			{
				statusEffect = this.m_seman.AddStatusEffect(hit.m_statusEffectHash, false, (int)hit.m_itemLevel, hit.m_skillLevel);
			}
			else
			{
				statusEffect.ResetTime();
				statusEffect.SetLevel((int)hit.m_itemLevel, hit.m_skillLevel);
			}
			if (statusEffect != null && attacker != null)
			{
				statusEffect.SetAttacker(attacker);
			}
		}
		WeakSpot weakSpot = this.GetWeakSpot(hit.m_weakSpot);
		if (weakSpot != null)
		{
			ZLog.Log("HIT Weakspot:" + weakSpot.gameObject.name);
		}
		HitData.DamageModifiers damageModifiers = this.GetDamageModifiers(weakSpot);
		HitData.DamageModifier mod;
		hit.ApplyResistance(damageModifiers, out mod);
		if (this.IsPlayer())
		{
			float bodyArmor = this.GetBodyArmor();
			hit.ApplyArmor(bodyArmor);
			this.DamageArmorDurability(hit);
		}
		else if (Game.m_worldLevel > 0)
		{
			hit.ApplyArmor((float)(Game.m_worldLevel * Game.instance.m_worldLevelEnemyBaseAC));
		}
		float poison = hit.m_damage.m_poison;
		float fire = hit.m_damage.m_fire;
		float spirit = hit.m_damage.m_spirit;
		hit.m_damage.m_poison = 0f;
		hit.m_damage.m_fire = 0f;
		hit.m_damage.m_spirit = 0f;
		this.ApplyDamage(hit, true, true, mod);
		this.AddFireDamage(fire);
		this.AddSpiritDamage(spirit);
		this.AddPoisonDamage(poison);
		this.AddFrostDamage(hit.m_damage.m_frost);
		this.AddLightningDamage(hit.m_damage.m_lightning);
	}

	// Token: 0x0600014C RID: 332 RVA: 0x00010CB0 File Offset: 0x0000EEB0
	protected HitData.DamageModifier GetDamageModifier(HitData.DamageType damageType)
	{
		return this.GetDamageModifiers(null).GetModifier(damageType);
	}

	// Token: 0x0600014D RID: 333 RVA: 0x00010CD0 File Offset: 0x0000EED0
	public HitData.DamageModifiers GetDamageModifiers(WeakSpot weakspot = null)
	{
		HitData.DamageModifiers result = weakspot ? weakspot.m_damageModifiers.Clone() : this.m_damageModifiers.Clone();
		this.ApplyArmorDamageMods(ref result);
		this.m_seman.ApplyDamageMods(ref result);
		return result;
	}

	// Token: 0x0600014E RID: 334 RVA: 0x00010D14 File Offset: 0x0000EF14
	public void ApplyDamage(HitData hit, bool showDamageText, bool triggerEffects, HitData.DamageModifier mod = HitData.DamageModifier.Normal)
	{
		if (this.IsDebugFlying() || this.IsDead() || this.IsTeleporting() || this.InCutscene())
		{
			return;
		}
		float totalDamage = hit.GetTotalDamage();
		if (!this.IsPlayer())
		{
			float difficultyDamageScaleEnemy = Game.instance.GetDifficultyDamageScaleEnemy(base.transform.position);
			hit.ApplyModifier(difficultyDamageScaleEnemy);
			hit.ApplyModifier(Game.m_playerDamageRate);
		}
		else
		{
			hit.ApplyModifier(Game.m_localDamgeTakenRate);
			Game.instance.IncrementPlayerStat((hit.GetAttacker() is Player) ? PlayerStatType.HitsTakenPlayers : PlayerStatType.HitsTakenEnemies, 1f);
		}
		float totalDamage2 = hit.GetTotalDamage();
		if (totalDamage2 <= 0.1f)
		{
			return;
		}
		if (showDamageText && (totalDamage2 > 0f || !this.IsPlayer()))
		{
			DamageText.instance.ShowText(mod, hit.m_point, totalDamage, this.IsPlayer() || this.IsTamed());
		}
		this.m_lastHit = hit;
		float num = this.GetHealth();
		num -= totalDamage2;
		if (num <= 0f && (this.InGodMode() || this.InGhostMode()))
		{
			num = 1f;
		}
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("damage"))
		{
			Terminal.Log(string.Format("Damage: Character {0} took {1} damage from {2}", this.m_name, totalDamage2, hit));
		}
		this.SetHealth(num);
		float totalStaggerDamage = hit.m_damage.GetTotalStaggerDamage();
		this.AddStaggerDamage(totalStaggerDamage * hit.m_staggerMultiplier, hit.m_dir);
		if (triggerEffects && totalDamage2 > this.GetMaxHealth() / 10f)
		{
			this.DoDamageCameraShake(hit);
			if (hit.m_damage.GetTotalPhysicalDamage() > 0f)
			{
				this.m_hitEffects.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
			}
		}
		this.OnDamaged(hit);
		if (this.m_onDamaged != null)
		{
			this.m_onDamaged(totalDamage2, hit.GetAttacker());
		}
		if (Character.s_dpsDebugEnabled)
		{
			Character.AddDPS(totalDamage2, this);
		}
	}

	// Token: 0x0600014F RID: 335 RVA: 0x00010EFA File Offset: 0x0000F0FA
	protected virtual void DoDamageCameraShake(HitData hit)
	{
	}

	// Token: 0x06000150 RID: 336 RVA: 0x00010EFC File Offset: 0x0000F0FC
	protected virtual void DamageArmorDurability(HitData hit)
	{
	}

	// Token: 0x06000151 RID: 337 RVA: 0x00010F00 File Offset: 0x0000F100
	private void AddFireDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Burning se_Burning = this.m_seman.GetStatusEffect(SEMan.s_statusEffectBurning) as SE_Burning;
		if (se_Burning == null)
		{
			se_Burning = (this.m_seman.AddStatusEffect(SEMan.s_statusEffectBurning, false, 0, 0f) as SE_Burning);
		}
		if (!se_Burning.AddFireDamage(damage))
		{
			this.m_seman.RemoveStatusEffect(se_Burning, false);
		}
	}

	// Token: 0x06000152 RID: 338 RVA: 0x00010F6C File Offset: 0x0000F16C
	private void AddSpiritDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Burning se_Burning = this.m_seman.GetStatusEffect(SEMan.s_statusEffectSpirit) as SE_Burning;
		if (se_Burning == null)
		{
			se_Burning = (this.m_seman.AddStatusEffect(SEMan.s_statusEffectSpirit, false, 0, 0f) as SE_Burning);
		}
		if (!se_Burning.AddSpiritDamage(damage))
		{
			this.m_seman.RemoveStatusEffect(se_Burning, false);
		}
	}

	// Token: 0x06000153 RID: 339 RVA: 0x00010FD8 File Offset: 0x0000F1D8
	private void AddPoisonDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Poison se_Poison = this.m_seman.GetStatusEffect(SEMan.s_statusEffectPoison) as SE_Poison;
		if (se_Poison == null)
		{
			se_Poison = (this.m_seman.AddStatusEffect(SEMan.s_statusEffectPoison, false, 0, 0f) as SE_Poison);
		}
		se_Poison.AddDamage(damage);
	}

	// Token: 0x06000154 RID: 340 RVA: 0x00011034 File Offset: 0x0000F234
	private void AddFrostDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Frost se_Frost = this.m_seman.GetStatusEffect(SEMan.s_statusEffectFrost) as SE_Frost;
		if (se_Frost == null)
		{
			se_Frost = (this.m_seman.AddStatusEffect(SEMan.s_statusEffectFrost, false, 0, 0f) as SE_Frost);
		}
		se_Frost.AddDamage(damage);
	}

	// Token: 0x06000155 RID: 341 RVA: 0x0001108D File Offset: 0x0000F28D
	private void AddLightningDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		this.m_seman.AddStatusEffect(SEMan.s_statusEffectLightning, true, 0, 0f);
	}

	// Token: 0x06000156 RID: 342 RVA: 0x000110B0 File Offset: 0x0000F2B0
	private static void AddDPS(float damage, Character me)
	{
		if (me == Player.m_localPlayer)
		{
			Character.CalculateDPS("To-you ", Character.s_playerDamage, damage);
			return;
		}
		Character.CalculateDPS("To-others ", Character.s_enemyDamage, damage);
	}

	// Token: 0x06000157 RID: 343 RVA: 0x000110E0 File Offset: 0x0000F2E0
	private static void CalculateDPS(string name, List<KeyValuePair<float, float>> damages, float damage)
	{
		float time = Time.time;
		if (damages.Count > 0 && Time.time - damages[damages.Count - 1].Key > 5f)
		{
			damages.Clear();
		}
		damages.Add(new KeyValuePair<float, float>(time, damage));
		float num = Time.time - damages[0].Key;
		if (num < 0.01f)
		{
			return;
		}
		float num2 = 0f;
		foreach (KeyValuePair<float, float> keyValuePair in damages)
		{
			num2 += keyValuePair.Value;
		}
		float num3 = num2 / num;
		string text = string.Concat(new string[]
		{
			"DPS ",
			name,
			" ( ",
			damages.Count.ToString(),
			" attacks, ",
			num.ToString("0.0"),
			"s ): ",
			num3.ToString("0.0")
		});
		ZLog.Log(text);
		MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text, 0, null, false);
	}

	// Token: 0x06000158 RID: 344 RVA: 0x0001121C File Offset: 0x0000F41C
	public float GetStaggerPercentage()
	{
		return Mathf.Clamp01(this.m_staggerDamage / this.GetStaggerTreshold());
	}

	// Token: 0x06000159 RID: 345 RVA: 0x00011230 File Offset: 0x0000F430
	private float GetStaggerTreshold()
	{
		return this.GetMaxHealth() * this.m_staggerDamageFactor;
	}

	// Token: 0x0600015A RID: 346 RVA: 0x00011240 File Offset: 0x0000F440
	protected bool AddStaggerDamage(float damage, Vector3 forceDirection)
	{
		if (this.m_staggerDamageFactor <= 0f)
		{
			return false;
		}
		this.m_staggerDamage += damage;
		float staggerTreshold = this.GetStaggerTreshold();
		if (this.m_staggerDamage >= staggerTreshold)
		{
			this.m_staggerDamage = staggerTreshold;
			this.Stagger(forceDirection);
			if (this.IsPlayer())
			{
				Hud.instance.StaggerBarFlash();
			}
			return true;
		}
		return false;
	}

	// Token: 0x0600015B RID: 347 RVA: 0x000112A0 File Offset: 0x0000F4A0
	private void UpdateStagger(float dt)
	{
		if (this.m_staggerDamageFactor <= 0f && !this.IsPlayer())
		{
			return;
		}
		float num = this.GetMaxHealth() * this.m_staggerDamageFactor;
		this.m_staggerDamage -= num / 5f * dt;
		if (this.m_staggerDamage < 0f)
		{
			this.m_staggerDamage = 0f;
		}
	}

	// Token: 0x0600015C RID: 348 RVA: 0x000112FF File Offset: 0x0000F4FF
	public void Stagger(Vector3 forceDirection)
	{
		if (this.m_nview.IsOwner())
		{
			this.RPC_Stagger(0L, forceDirection);
			return;
		}
		this.m_nview.InvokeRPC("RPC_Stagger", new object[]
		{
			forceDirection
		});
	}

	// Token: 0x0600015D RID: 349 RVA: 0x00011338 File Offset: 0x0000F538
	private void RPC_Stagger(long sender, Vector3 forceDirection)
	{
		if (!this.IsStaggering())
		{
			if (forceDirection.magnitude > 0.01f)
			{
				forceDirection.y = 0f;
				base.transform.rotation = Quaternion.LookRotation(-forceDirection);
			}
			this.m_zanim.SetSpeed(1f);
			this.m_zanim.SetTrigger("stagger");
		}
	}

	// Token: 0x0600015E RID: 350 RVA: 0x0001139D File Offset: 0x0000F59D
	protected virtual void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
	}

	// Token: 0x0600015F RID: 351 RVA: 0x0001139F File Offset: 0x0000F59F
	public virtual float GetBodyArmor()
	{
		return 0f;
	}

	// Token: 0x06000160 RID: 352 RVA: 0x000113A6 File Offset: 0x0000F5A6
	protected virtual bool BlockAttack(HitData hit, Character attacker)
	{
		return false;
	}

	// Token: 0x06000161 RID: 353 RVA: 0x000113A9 File Offset: 0x0000F5A9
	protected virtual void OnDamaged(HitData hit)
	{
	}

	// Token: 0x06000162 RID: 354 RVA: 0x000113AC File Offset: 0x0000F5AC
	private void OnCollisionStay(Collision collision)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_jumpTimer < 0.1f)
		{
			return;
		}
		foreach (ContactPoint contactPoint in collision.contacts)
		{
			float num = contactPoint.point.y - base.transform.position.y;
			Vector3 normal = contactPoint.normal;
			if (normal.y < 0f)
			{
				normal.y *= -1f;
			}
			if (normal.y > 0.1f && num < this.m_collider.radius && contactPoint.point.y < this.m_collider.transform.position.y + this.m_collider.center.y)
			{
				if (normal.y > this.m_groundContactNormal.y || !this.m_groundContact)
				{
					if (this.m_standUp == -100f)
					{
						this.m_standUp = 2f;
					}
					this.m_groundContact = true;
					this.m_groundContactNormal = normal;
					this.m_groundContactPoint = contactPoint.point;
					this.m_lowestContactCollider = collision.collider;
				}
				else
				{
					Vector3 vector = Vector3.Normalize(this.m_groundContactNormal + normal);
					if (vector.y > this.m_groundContactNormal.y)
					{
						this.m_groundContactNormal = vector;
						this.m_groundContactPoint = (this.m_groundContactPoint + contactPoint.point) * 0.5f;
					}
				}
			}
		}
	}

	// Token: 0x06000163 RID: 355 RVA: 0x00011546 File Offset: 0x0000F746
	public void StandUpOnNextGround()
	{
		this.m_standUp = -100f;
	}

	// Token: 0x06000164 RID: 356 RVA: 0x00011554 File Offset: 0x0000F754
	private void UpdateGroundContact(float dt)
	{
		if (!this.m_groundContact)
		{
			return;
		}
		this.m_lastGroundCollider = this.m_lowestContactCollider;
		this.m_lastGroundNormal = this.m_groundContactNormal;
		this.m_lastGroundPoint = this.m_groundContactPoint;
		this.m_lastGroundBody = (this.m_lastGroundCollider ? this.m_lastGroundCollider.attachedRigidbody : null);
		if (!this.IsPlayer() && this.m_lastGroundBody != null && this.m_lastGroundBody.gameObject.layer == base.gameObject.layer)
		{
			this.m_lastGroundCollider = null;
			this.m_lastGroundBody = null;
		}
		float num = Mathf.Max(0f, this.m_maxAirAltitude - base.transform.position.y);
		if (num > 0.8f && this.m_onLand != null)
		{
			Vector3 lastGroundPoint = this.m_lastGroundPoint;
			if (this.InLiquid())
			{
				lastGroundPoint.y = this.GetLiquidLevel();
			}
			this.m_onLand(this.m_lastGroundPoint);
		}
		if (this.IsPlayer() && num > 4f)
		{
			float num2 = Mathf.Clamp01((num - 4f) / 16f) * 100f;
			this.m_seman.ModifyFallDamage(num2, ref num2);
			if (num2 > 0f)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = num2;
				hitData.m_point = this.m_lastGroundPoint;
				hitData.m_dir = this.m_lastGroundNormal;
				hitData.m_hitType = HitData.HitType.Fall;
				this.Damage(hitData);
			}
		}
		this.ResetGroundContact();
		this.m_lastGroundTouch = 0f;
		this.m_maxAirAltitude = base.transform.position.y;
		if (this.IsPlayer() && Terminal.m_showTests)
		{
			Dictionary<string, string> testList = Terminal.m_testList;
			string key = "Player.CollisionLayer";
			Collider lastGroundCollider = Player.m_localPlayer.GetLastGroundCollider();
			testList[key] = ((lastGroundCollider != null && lastGroundCollider) ? LayerMask.LayerToName(lastGroundCollider.gameObject.layer) : "none");
		}
	}

	// Token: 0x06000165 RID: 357 RVA: 0x0001173C File Offset: 0x0000F93C
	private void ResetGroundContact()
	{
		this.m_lowestContactCollider = null;
		this.m_groundContact = false;
		this.m_groundContactNormal = Vector3.zero;
		this.m_groundContactPoint = Vector3.zero;
	}

	// Token: 0x06000166 RID: 358 RVA: 0x00011762 File Offset: 0x0000F962
	public Ship GetStandingOnShip()
	{
		if (this.InNumShipVolumes == 0)
		{
			return null;
		}
		if (!this.IsOnGround())
		{
			return null;
		}
		if (this.m_lastGroundBody)
		{
			return this.m_lastGroundBody.GetComponent<Ship>();
		}
		return null;
	}

	// Token: 0x06000167 RID: 359 RVA: 0x00011792 File Offset: 0x0000F992
	public bool IsOnGround()
	{
		return this.m_lastGroundTouch < 0.2f || this.m_body.IsSleeping();
	}

	// Token: 0x06000168 RID: 360 RVA: 0x000117AE File Offset: 0x0000F9AE
	private void CheckDeath()
	{
		if (this.IsDead())
		{
			return;
		}
		if (this.GetHealth() <= 0f)
		{
			this.OnDeath();
		}
	}

	// Token: 0x06000169 RID: 361 RVA: 0x000117CC File Offset: 0x0000F9CC
	protected virtual void OnRagdollCreated(Ragdoll ragdoll)
	{
	}

	// Token: 0x0600016A RID: 362 RVA: 0x000117D0 File Offset: 0x0000F9D0
	protected virtual void OnDeath()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		bool flag = this.m_lastHit != null && this.m_lastHit.GetAttacker() == Player.m_localPlayer;
		if (flag && this.IsPlayer())
		{
			Player player = this as Player;
			if (player != null)
			{
				playerProfile.IncrementStat(PlayerStatType.PlayerKills, 1f);
				playerProfile.m_enemyStats.IncrementOrSet(player.GetPlayerName(), 1f);
			}
		}
		if (!this.IsPlayer())
		{
			if (this.m_localPlayerHasHit)
			{
				playerProfile.IncrementStat(this.IsBoss() ? PlayerStatType.BossKills : PlayerStatType.EnemyKills, 1f);
			}
			if (flag)
			{
				playerProfile.IncrementStat(this.IsBoss() ? PlayerStatType.BossLastHits : PlayerStatType.EnemyKillsLastHits, 1f);
			}
			playerProfile.m_enemyStats.IncrementOrSet(this.m_name, 1f);
		}
		if (!string.IsNullOrEmpty(this.m_defeatSetGlobalKey))
		{
			Player.m_addUniqueKeyQueue.Add(this.m_defeatSetGlobalKey);
		}
		if (this.m_nview && !this.m_nview.IsOwner())
		{
			return;
		}
		GameObject[] array = this.m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if (component)
			{
				CharacterDrop component2 = base.GetComponent<CharacterDrop>();
				LevelEffects componentInChildren = base.GetComponentInChildren<LevelEffects>();
				Vector3 velocity = this.m_body.velocity;
				if (this.m_pushForce.magnitude * 0.5f > velocity.magnitude)
				{
					velocity = this.m_pushForce * 0.5f;
				}
				float hue = 0f;
				float saturation = 0f;
				float value = 0f;
				if (componentInChildren)
				{
					componentInChildren.GetColorChanges(out hue, out saturation, out value);
				}
				component.Setup(velocity, hue, saturation, value, component2);
				this.OnRagdollCreated(component);
				if (component2 && component.m_dropItems)
				{
					component2.SetDropsEnabled(false);
				}
			}
		}
		if (!string.IsNullOrEmpty(this.m_defeatSetGlobalKey))
		{
			ZoneSystem.instance.SetGlobalKey(this.m_defeatSetGlobalKey);
		}
		if (this.m_onDeath != null)
		{
			this.m_onDeath();
		}
		if (this.IsBoss() && this.m_nview.GetZDO().GetBool("bosscount", false))
		{
			float num;
			ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out num);
			ZoneSystem.instance.SetGlobalKey(GlobalKeys.activeBosses, Mathf.Max(0f, num - 1f));
		}
		ZNetScene.instance.Destroy(base.gameObject);
		Gogan.LogEvent("Game", "Killed", this.m_name, 0L);
	}

	// Token: 0x0600016B RID: 363 RVA: 0x00011A7C File Offset: 0x0000FC7C
	public float GetHealth()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null)
		{
			return this.GetMaxHealth();
		}
		return zdo.GetFloat(ZDOVars.s_health, this.GetMaxHealth());
	}

	// Token: 0x0600016C RID: 364 RVA: 0x00011AB0 File Offset: 0x0000FCB0
	public void SetHealth(float health)
	{
		if (health >= this.GetMaxHealth())
		{
			this.m_localPlayerHasHit = false;
		}
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null || !this.m_nview.IsOwner())
		{
			return;
		}
		if (health < 0f)
		{
			health = 0f;
		}
		zdo.Set(ZDOVars.s_health, health);
	}

	// Token: 0x0600016D RID: 365 RVA: 0x00011B08 File Offset: 0x0000FD08
	public void UseHealth(float hp)
	{
		if (hp <= 0f)
		{
			return;
		}
		float num = this.GetHealth();
		num -= hp;
		num = Mathf.Clamp(num, 0f, this.GetMaxHealth());
		this.SetHealth(num);
		if (this.IsPlayer())
		{
			Hud.instance.DamageFlash();
		}
	}

	// Token: 0x0600016E RID: 366 RVA: 0x00011B54 File Offset: 0x0000FD54
	public float GetHealthPercentage()
	{
		return this.GetHealth() / this.GetMaxHealth();
	}

	// Token: 0x0600016F RID: 367 RVA: 0x00011B63 File Offset: 0x0000FD63
	public virtual bool IsDead()
	{
		return false;
	}

	// Token: 0x06000170 RID: 368 RVA: 0x00011B66 File Offset: 0x0000FD66
	public void SetMaxHealth(float health)
	{
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_maxHealth, health);
		}
		if (this.GetHealth() > health)
		{
			this.SetHealth(health);
		}
	}

	// Token: 0x06000171 RID: 369 RVA: 0x00011B9B File Offset: 0x0000FD9B
	public float GetMaxHealth()
	{
		if (this.m_nview.GetZDO() != null)
		{
			return this.m_nview.GetZDO().GetFloat(ZDOVars.s_maxHealth, this.m_health);
		}
		return this.GetMaxHealthBase();
	}

	// Token: 0x06000172 RID: 370 RVA: 0x00011BCC File Offset: 0x0000FDCC
	public float GetMaxHealthBase()
	{
		float num = this.m_health;
		if (!this.IsPlayer() && Game.m_worldLevel > 0)
		{
			num *= (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyHPMultiplier;
		}
		return num;
	}

	// Token: 0x06000173 RID: 371 RVA: 0x00011C05 File Offset: 0x0000FE05
	public virtual float GetMaxStamina()
	{
		return 0f;
	}

	// Token: 0x06000174 RID: 372 RVA: 0x00011C0C File Offset: 0x0000FE0C
	public virtual float GetMaxEitr()
	{
		return 0f;
	}

	// Token: 0x06000175 RID: 373 RVA: 0x00011C13 File Offset: 0x0000FE13
	public virtual float GetEitrPercentage()
	{
		return 1f;
	}

	// Token: 0x06000176 RID: 374 RVA: 0x00011C1A File Offset: 0x0000FE1A
	public virtual float GetStaminaPercentage()
	{
		return 1f;
	}

	// Token: 0x06000177 RID: 375 RVA: 0x00011C21 File Offset: 0x0000FE21
	public bool IsBoss()
	{
		return this.m_boss;
	}

	// Token: 0x06000178 RID: 376 RVA: 0x00011C2C File Offset: 0x0000FE2C
	public bool TryUseEitr(float eitrUse = 0f)
	{
		if (eitrUse == 0f)
		{
			return true;
		}
		if (this.GetMaxEitr() == 0f)
		{
			this.Message(MessageHud.MessageType.Center, "$hud_eitrrequired", 0, null);
			return false;
		}
		if (!this.HaveEitr(eitrUse + 0.1f))
		{
			if (this.IsPlayer())
			{
				Hud.instance.EitrBarEmptyFlash();
			}
			return false;
		}
		return true;
	}

	// Token: 0x06000179 RID: 377 RVA: 0x00011C84 File Offset: 0x0000FE84
	public void SetLookDir(Vector3 dir, float transitionTime = 0f)
	{
		if (transitionTime > 0f)
		{
			this.m_lookTransitionTimeTotal = transitionTime;
			this.m_lookTransitionTime = transitionTime;
			this.m_lookTransitionStart = this.GetLookDir();
			this.m_lookTransitionTarget = Vector3.Normalize(dir);
			return;
		}
		if (dir.magnitude <= Mathf.Epsilon)
		{
			dir = base.transform.forward;
		}
		else
		{
			dir.Normalize();
		}
		this.m_lookDir = dir;
		dir.y = 0f;
		this.m_lookYaw = Quaternion.LookRotation(dir);
	}

	// Token: 0x0600017A RID: 378 RVA: 0x00011D08 File Offset: 0x0000FF08
	private void UpdateLookTransition(float dt)
	{
		if (this.m_lookTransitionTime > 0f)
		{
			this.SetLookDir(Vector3.Lerp(this.m_lookTransitionTarget, this.m_lookTransitionStart, Mathf.SmoothStep(0f, 1f, this.m_lookTransitionTime / this.m_lookTransitionTimeTotal)), 0f);
			this.m_lookTransitionTime -= dt;
		}
	}

	// Token: 0x0600017B RID: 379 RVA: 0x00011D68 File Offset: 0x0000FF68
	public Vector3 GetLookDir()
	{
		return this.m_eye.forward;
	}

	// Token: 0x0600017C RID: 380 RVA: 0x00011D75 File Offset: 0x0000FF75
	public virtual void OnAttackTrigger()
	{
	}

	// Token: 0x0600017D RID: 381 RVA: 0x00011D77 File Offset: 0x0000FF77
	public virtual void OnStopMoving()
	{
	}

	// Token: 0x0600017E RID: 382 RVA: 0x00011D79 File Offset: 0x0000FF79
	public virtual void OnWeaponTrailStart()
	{
	}

	// Token: 0x0600017F RID: 383 RVA: 0x00011D7B File Offset: 0x0000FF7B
	public void SetMoveDir(Vector3 dir)
	{
		this.m_moveDir = dir;
	}

	// Token: 0x06000180 RID: 384 RVA: 0x00011D84 File Offset: 0x0000FF84
	public void SetRun(bool run)
	{
		this.m_run = run;
	}

	// Token: 0x06000181 RID: 385 RVA: 0x00011D8D File Offset: 0x0000FF8D
	public void SetWalk(bool walk)
	{
		this.m_walk = walk;
	}

	// Token: 0x06000182 RID: 386 RVA: 0x00011D96 File Offset: 0x0000FF96
	public bool GetWalk()
	{
		return this.m_walk;
	}

	// Token: 0x06000183 RID: 387 RVA: 0x00011D9E File Offset: 0x0000FF9E
	protected virtual void UpdateEyeRotation()
	{
		this.m_eye.rotation = Quaternion.LookRotation(this.m_lookDir);
	}

	// Token: 0x06000184 RID: 388 RVA: 0x00011DB8 File Offset: 0x0000FFB8
	public void OnAutoJump(Vector3 dir, float upVel, float forwardVel)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.IsOnGround() || this.IsDead() || this.InAttack() || this.InDodge() || this.IsKnockedBack())
		{
			return;
		}
		if (Time.time - this.m_lastAutoJumpTime < 0.5f)
		{
			return;
		}
		this.m_lastAutoJumpTime = Time.time;
		if (Vector3.Dot(this.m_moveDir, dir) < 0.5f)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		vector.y = upVel;
		vector += dir * forwardVel;
		this.m_body.velocity = vector;
		this.m_lastGroundTouch = 1f;
		this.m_jumpTimer = 0f;
		this.m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		this.SetCrouch(false);
		this.UpdateBodyFriction();
	}

	// Token: 0x06000185 RID: 389 RVA: 0x00011EB8 File Offset: 0x000100B8
	public void Jump(bool force = false)
	{
		if (this.IsOnGround() && !this.IsDead() && (force || !this.InAttack()) && !this.IsEncumbered() && !this.InDodge() && !this.IsKnockedBack() && !this.IsStaggering())
		{
			bool flag = false;
			if (!this.HaveStamina(this.m_jumpStaminaUsage))
			{
				if (this.IsPlayer())
				{
					Hud.instance.StaminaBarEmptyFlash();
				}
				flag = true;
			}
			float speed = this.m_speed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref speed, this.m_currentVel);
			if (speed <= 0f)
			{
				flag = true;
			}
			float num = 0f;
			Skills skills = this.GetSkills();
			if (skills != null)
			{
				num = skills.GetSkillFactor(Skills.SkillType.Jump);
				if (!flag)
				{
					this.RaiseSkill(Skills.SkillType.Jump, 1f);
				}
			}
			Vector3 vector = this.m_body.velocity;
			Mathf.Acos(Mathf.Clamp01(this.m_lastGroundNormal.y));
			Vector3 normalized = (this.m_lastGroundNormal + Vector3.up).normalized;
			float num2 = 1f + num * 0.4f;
			float num3 = this.m_jumpForce * num2;
			float num4 = Vector3.Dot(normalized, vector);
			if (num4 < num3)
			{
				vector += normalized * (num3 - num4);
			}
			if (this.IsPlayer())
			{
				vector += this.m_moveDir * this.m_jumpForceForward * num2;
			}
			else
			{
				vector += base.transform.forward * this.m_jumpForceForward * num2;
			}
			if (flag)
			{
				vector *= this.m_jumpForceTiredFactor;
			}
			this.m_seman.ApplyStatusEffectJumpMods(ref vector);
			if (vector.x <= 0f && vector.y <= 0f && vector.z <= 0f)
			{
				return;
			}
			this.ForceJump(vector, true);
		}
	}

	// Token: 0x06000186 RID: 390 RVA: 0x000120B0 File Offset: 0x000102B0
	public void ForceJump(Vector3 vel, bool effects = true)
	{
		this.m_body.WakeUp();
		this.m_body.velocity = vel;
		this.ResetGroundContact();
		this.m_lastGroundTouch = 1f;
		this.m_jumpTimer = 0f;
		this.AddNoise(30f);
		if (effects)
		{
			this.m_zanim.SetTrigger("jump");
			this.m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
			this.ResetCloth();
			this.OnJump();
			this.SetCrouch(false);
			this.UpdateBodyFriction();
		}
	}

	// Token: 0x06000187 RID: 391 RVA: 0x00012155 File Offset: 0x00010355
	public void SetTempParent(Transform t)
	{
		this.oldParent = base.transform.parent;
		base.transform.parent = t;
	}

	// Token: 0x06000188 RID: 392 RVA: 0x00012174 File Offset: 0x00010374
	public void ReleaseTempParent()
	{
		base.transform.parent = this.oldParent;
	}

	// Token: 0x06000189 RID: 393 RVA: 0x00012188 File Offset: 0x00010388
	private void UpdateBodyFriction()
	{
		this.m_collider.material.frictionCombine = PhysicMaterialCombine.Multiply;
		if (this.IsDead())
		{
			this.m_collider.material.staticFriction = 1f;
			this.m_collider.material.dynamicFriction = 1f;
			this.m_collider.material.frictionCombine = PhysicMaterialCombine.Maximum;
			return;
		}
		if (this.IsSwimming())
		{
			this.m_collider.material.staticFriction = 0.2f;
			this.m_collider.material.dynamicFriction = 0.2f;
			return;
		}
		if (!this.IsOnGround())
		{
			this.m_collider.material.staticFriction = 0f;
			this.m_collider.material.dynamicFriction = 0f;
			return;
		}
		if (this.IsFlying())
		{
			this.m_collider.material.staticFriction = 0f;
			this.m_collider.material.dynamicFriction = 0f;
			return;
		}
		if (this.m_moveDir.magnitude < 0.1f)
		{
			this.m_collider.material.staticFriction = 0.8f * (1f - this.m_slippage);
			this.m_collider.material.dynamicFriction = 0.8f * (1f - this.m_slippage);
			this.m_collider.material.frictionCombine = PhysicMaterialCombine.Maximum;
			return;
		}
		this.m_collider.material.staticFriction = 0.4f * (1f - this.m_slippage);
		this.m_collider.material.dynamicFriction = 0.4f * (1f - this.m_slippage);
	}

	// Token: 0x0600018A RID: 394 RVA: 0x0001232F File Offset: 0x0001052F
	public virtual bool StartAttack(Character target, bool charge)
	{
		return false;
	}

	// Token: 0x0600018B RID: 395 RVA: 0x00012332 File Offset: 0x00010532
	public virtual float GetTimeSinceLastAttack()
	{
		return 99999f;
	}

	// Token: 0x0600018C RID: 396 RVA: 0x00012339 File Offset: 0x00010539
	public virtual void OnNearFire(Vector3 point)
	{
	}

	// Token: 0x0600018D RID: 397 RVA: 0x0001233B File Offset: 0x0001053B
	public ZDOID GetZDOID()
	{
		if (!this.m_nview.IsValid())
		{
			return ZDOID.None;
		}
		return this.m_nview.GetZDO().m_uid;
	}

	// Token: 0x0600018E RID: 398 RVA: 0x00012360 File Offset: 0x00010560
	public bool IsOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	// Token: 0x0600018F RID: 399 RVA: 0x0001237C File Offset: 0x0001057C
	public long GetOwner()
	{
		if (!this.m_nview.IsValid())
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetOwner();
	}

	// Token: 0x06000190 RID: 400 RVA: 0x0001239E File Offset: 0x0001059E
	public virtual bool UseMeleeCamera()
	{
		return false;
	}

	// Token: 0x06000191 RID: 401 RVA: 0x000123A1 File Offset: 0x000105A1
	protected virtual bool AlwaysRotateCamera()
	{
		return true;
	}

	// Token: 0x06000192 RID: 402 RVA: 0x000123A4 File Offset: 0x000105A4
	public void SetLiquidLevel(float level, LiquidType type, Component liquidObj)
	{
		if (type != LiquidType.Water)
		{
			if (type == LiquidType.Tar)
			{
				this.m_tarLevel = level;
			}
		}
		else
		{
			this.m_waterLevel = level;
		}
		this.m_liquidLevel = Mathf.Max(this.m_waterLevel, this.m_tarLevel);
	}

	// Token: 0x06000193 RID: 403 RVA: 0x000123D6 File Offset: 0x000105D6
	public virtual bool IsPVPEnabled()
	{
		return false;
	}

	// Token: 0x06000194 RID: 404 RVA: 0x000123D9 File Offset: 0x000105D9
	public virtual bool InIntro()
	{
		return false;
	}

	// Token: 0x06000195 RID: 405 RVA: 0x000123DC File Offset: 0x000105DC
	public virtual bool InCutscene()
	{
		return false;
	}

	// Token: 0x06000196 RID: 406 RVA: 0x000123DF File Offset: 0x000105DF
	public virtual bool IsCrouching()
	{
		return false;
	}

	// Token: 0x06000197 RID: 407 RVA: 0x000123E2 File Offset: 0x000105E2
	public virtual bool InBed()
	{
		return false;
	}

	// Token: 0x06000198 RID: 408 RVA: 0x000123E5 File Offset: 0x000105E5
	public virtual bool IsAttached()
	{
		return false;
	}

	// Token: 0x06000199 RID: 409 RVA: 0x000123E8 File Offset: 0x000105E8
	public virtual bool IsAttachedToShip()
	{
		return false;
	}

	// Token: 0x0600019A RID: 410 RVA: 0x000123EB File Offset: 0x000105EB
	public virtual bool IsRiding()
	{
		return false;
	}

	// Token: 0x0600019B RID: 411 RVA: 0x000123EE File Offset: 0x000105EE
	protected virtual void SetCrouch(bool crouch)
	{
	}

	// Token: 0x0600019C RID: 412 RVA: 0x000123F0 File Offset: 0x000105F0
	public virtual void AttachStart(Transform attachPoint, GameObject colliderRoot, bool hideWeapons, bool isBed, bool onShip, string attachAnimation, Vector3 detachOffset, Transform cameraPos = null)
	{
	}

	// Token: 0x0600019D RID: 413 RVA: 0x000123F2 File Offset: 0x000105F2
	public virtual void AttachStop()
	{
	}

	// Token: 0x0600019E RID: 414 RVA: 0x000123F4 File Offset: 0x000105F4
	private void UpdateWater(float dt)
	{
		this.m_swimTimer += dt;
		float depth = this.InLiquidDepth();
		if (this.m_canSwim && this.InLiquidSwimDepth(depth))
		{
			this.m_swimTimer = 0f;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.InLiquidWetDepth(depth))
		{
			return;
		}
		if (this.m_waterLevel > this.m_tarLevel)
		{
			this.m_seman.AddStatusEffect(SEMan.s_statusEffectWet, true, 0, 0f);
			return;
		}
		if (this.m_tarLevel > this.m_waterLevel && !this.m_tolerateTar)
		{
			this.m_seman.AddStatusEffect(SEMan.s_statusEffectTared, true, 0, 0f);
		}
	}

	// Token: 0x0600019F RID: 415 RVA: 0x000124A0 File Offset: 0x000106A0
	private void ApplyLiquidResistance(ref float speed)
	{
		float num = this.InLiquidDepth();
		if (num <= 0f)
		{
			return;
		}
		if (this.m_seman.HaveStatusEffect(SEMan.s_statusEffectTared))
		{
			return;
		}
		float num2 = (this.m_tarLevel > this.m_waterLevel) ? 0.1f : 0.05f;
		float num3 = this.m_collider.height / 3f;
		float num4 = Mathf.Clamp01(num / num3);
		speed -= speed * speed * num4 * num2;
	}

	// Token: 0x060001A0 RID: 416 RVA: 0x00012514 File Offset: 0x00010714
	public bool IsSwimming()
	{
		return this.m_swimTimer < 0.5f;
	}

	// Token: 0x060001A1 RID: 417 RVA: 0x00012523 File Offset: 0x00010723
	public bool InLava()
	{
		return this.m_lavaTimer < 0.5f;
	}

	// Token: 0x060001A2 RID: 418 RVA: 0x00012532 File Offset: 0x00010732
	public bool AboveOrInLava()
	{
		return this.m_aboveOrInLavaTimer < 0.5f;
	}

	// Token: 0x060001A3 RID: 419 RVA: 0x00012541 File Offset: 0x00010741
	private bool InLiquidSwimDepth()
	{
		return this.InLiquidDepth() > Mathf.Max(0f, this.m_swimDepth - 0.4f);
	}

	// Token: 0x060001A4 RID: 420 RVA: 0x00012561 File Offset: 0x00010761
	private bool InLiquidSwimDepth(float depth)
	{
		return depth > Mathf.Max(0f, this.m_swimDepth - 0.4f);
	}

	// Token: 0x060001A5 RID: 421 RVA: 0x0001257C File Offset: 0x0001077C
	private bool InLiquidKneeDepth()
	{
		return this.InLiquidDepth() > 0.4f;
	}

	// Token: 0x060001A6 RID: 422 RVA: 0x0001258B File Offset: 0x0001078B
	private bool InLiquidKneeDepth(float depth)
	{
		return depth > 0.4f;
	}

	// Token: 0x060001A7 RID: 423 RVA: 0x00012595 File Offset: 0x00010795
	private bool InLiquidWetDepth(float depth)
	{
		return this.InLiquidSwimDepth(depth) || (this.IsSitting() && this.InLiquidKneeDepth(depth));
	}

	// Token: 0x060001A8 RID: 424 RVA: 0x000125B3 File Offset: 0x000107B3
	private float InLiquidDepth()
	{
		return this.m_cashedInLiquidDepth;
	}

	// Token: 0x060001A9 RID: 425 RVA: 0x000125BC File Offset: 0x000107BC
	private void CalculateLiquidDepth()
	{
		if (this.IsTeleporting() || this.GetStandingOnShip() != null || this.IsAttachedToShip())
		{
			this.m_cashedInLiquidDepth = 0f;
			return;
		}
		this.m_cashedInLiquidDepth = Mathf.Max(0f, this.GetLiquidLevel() - base.transform.position.y);
	}

	// Token: 0x060001AA RID: 426 RVA: 0x0001261A File Offset: 0x0001081A
	protected void InvalidateCachedLiquidDepth()
	{
		this.m_cashedInLiquidDepth = 0f;
	}

	// Token: 0x060001AB RID: 427 RVA: 0x00012627 File Offset: 0x00010827
	public float GetLiquidLevel()
	{
		return this.m_liquidLevel;
	}

	// Token: 0x060001AC RID: 428 RVA: 0x0001262F File Offset: 0x0001082F
	public bool InLiquid()
	{
		return this.InLiquidDepth() > 0f;
	}

	// Token: 0x060001AD RID: 429 RVA: 0x0001263E File Offset: 0x0001083E
	private bool InTar()
	{
		return this.m_tarLevel > this.m_waterLevel && this.InLiquid();
	}

	// Token: 0x060001AE RID: 430 RVA: 0x00012656 File Offset: 0x00010856
	public bool InWater()
	{
		return this.m_waterLevel > this.m_tarLevel && this.InLiquid();
	}

	// Token: 0x060001AF RID: 431 RVA: 0x0001266E File Offset: 0x0001086E
	protected virtual bool CheckRun(Vector3 moveDir, float dt)
	{
		return this.m_run && moveDir.magnitude >= 0.1f && !this.IsCrouching() && !this.IsEncumbered() && !this.InDodge();
	}

	// Token: 0x060001B0 RID: 432 RVA: 0x000126A7 File Offset: 0x000108A7
	public bool IsRunning()
	{
		return this.m_running;
	}

	// Token: 0x060001B1 RID: 433 RVA: 0x000126AF File Offset: 0x000108AF
	public bool IsWalking()
	{
		return this.m_walking;
	}

	// Token: 0x060001B2 RID: 434 RVA: 0x000126B7 File Offset: 0x000108B7
	public virtual bool InPlaceMode()
	{
		return false;
	}

	// Token: 0x060001B3 RID: 435 RVA: 0x000126BA File Offset: 0x000108BA
	public virtual void AddEitr(float v)
	{
	}

	// Token: 0x060001B4 RID: 436 RVA: 0x000126BC File Offset: 0x000108BC
	public virtual void UseEitr(float eitr)
	{
	}

	// Token: 0x060001B5 RID: 437 RVA: 0x000126BE File Offset: 0x000108BE
	public virtual bool HaveEitr(float amount = 0f)
	{
		return true;
	}

	// Token: 0x060001B6 RID: 438 RVA: 0x000126C1 File Offset: 0x000108C1
	public virtual bool HaveStamina(float amount = 0f)
	{
		return true;
	}

	// Token: 0x060001B7 RID: 439 RVA: 0x000126C4 File Offset: 0x000108C4
	public bool HaveHealth(float amount = 0f)
	{
		return this.GetHealth() >= amount;
	}

	// Token: 0x060001B8 RID: 440 RVA: 0x000126D2 File Offset: 0x000108D2
	public virtual void AddStamina(float v)
	{
	}

	// Token: 0x060001B9 RID: 441 RVA: 0x000126D4 File Offset: 0x000108D4
	public virtual void UseStamina(float stamina)
	{
	}

	// Token: 0x060001BA RID: 442 RVA: 0x000126D6 File Offset: 0x000108D6
	protected int GetNextOrCurrentAnimHash()
	{
		if (this.m_cachedAnimHashFrame == MonoUpdaters.UpdateCount)
		{
			return this.m_cachedNextOrCurrentAnimHash;
		}
		this.UpdateCachedAnimHashes();
		return this.m_cachedNextOrCurrentAnimHash;
	}

	// Token: 0x060001BB RID: 443 RVA: 0x000126F8 File Offset: 0x000108F8
	protected int GetCurrentAnimHash()
	{
		if (this.m_cachedAnimHashFrame == MonoUpdaters.UpdateCount)
		{
			return this.m_cachedCurrentAnimHash;
		}
		this.UpdateCachedAnimHashes();
		return this.m_cachedCurrentAnimHash;
	}

	// Token: 0x060001BC RID: 444 RVA: 0x0001271A File Offset: 0x0001091A
	protected int GetNextAnimHash()
	{
		if (this.m_cachedAnimHashFrame == MonoUpdaters.UpdateCount)
		{
			return this.m_cachedNextAnimHash;
		}
		this.UpdateCachedAnimHashes();
		return this.m_cachedNextAnimHash;
	}

	// Token: 0x060001BD RID: 445 RVA: 0x0001273C File Offset: 0x0001093C
	private void UpdateCachedAnimHashes()
	{
		this.m_cachedAnimHashFrame = MonoUpdaters.UpdateCount;
		this.m_cachedCurrentAnimHash = this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash;
		this.m_cachedNextAnimHash = 0;
		this.m_cachedNextOrCurrentAnimHash = this.m_cachedCurrentAnimHash;
		if (this.m_animator.IsInTransition(0))
		{
			this.m_cachedNextAnimHash = this.m_animator.GetNextAnimatorStateInfo(0).tagHash;
			this.m_cachedNextOrCurrentAnimHash = this.m_cachedNextAnimHash;
		}
	}

	// Token: 0x060001BE RID: 446 RVA: 0x000127B5 File Offset: 0x000109B5
	public bool IsStaggering()
	{
		return this.GetNextAnimHash() == Character.s_animatorTagStagger || this.GetCurrentAnimHash() == Character.s_animatorTagStagger;
	}

	// Token: 0x060001BF RID: 447 RVA: 0x000127D4 File Offset: 0x000109D4
	public virtual bool CanMove()
	{
		if (this.IsStaggering())
		{
			return false;
		}
		int nextOrCurrentAnimHash = this.GetNextOrCurrentAnimHash();
		return nextOrCurrentAnimHash != Character.s_animatorTagFreeze && nextOrCurrentAnimHash != Character.s_animatorTagSitting;
	}

	// Token: 0x060001C0 RID: 448 RVA: 0x00012807 File Offset: 0x00010A07
	public virtual bool IsEncumbered()
	{
		return false;
	}

	// Token: 0x060001C1 RID: 449 RVA: 0x0001280A File Offset: 0x00010A0A
	public virtual bool IsTeleporting()
	{
		return false;
	}

	// Token: 0x060001C2 RID: 450 RVA: 0x0001280D File Offset: 0x00010A0D
	private bool CanWallRun()
	{
		return this.IsPlayer();
	}

	// Token: 0x060001C3 RID: 451 RVA: 0x00012815 File Offset: 0x00010A15
	public void ShowPickupMessage(ItemDrop.ItemData item, int amount)
	{
		this.Message(MessageHud.MessageType.TopLeft, "$msg_added " + item.m_shared.m_name, amount, item.GetIcon());
	}

	// Token: 0x060001C4 RID: 452 RVA: 0x0001283A File Offset: 0x00010A3A
	public void ShowRemovedMessage(ItemDrop.ItemData item, int amount)
	{
		this.Message(MessageHud.MessageType.TopLeft, "$msg_removed " + item.m_shared.m_name, amount, item.GetIcon());
	}

	// Token: 0x060001C5 RID: 453 RVA: 0x0001285F File Offset: 0x00010A5F
	public virtual void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
	}

	// Token: 0x060001C6 RID: 454 RVA: 0x00012861 File Offset: 0x00010A61
	public CapsuleCollider GetCollider()
	{
		return this.m_collider;
	}

	// Token: 0x060001C7 RID: 455 RVA: 0x00012869 File Offset: 0x00010A69
	public virtual float GetStealthFactor()
	{
		return 1f;
	}

	// Token: 0x060001C8 RID: 456 RVA: 0x00012870 File Offset: 0x00010A70
	private void UpdateNoise(float dt)
	{
		this.m_noiseRange = Mathf.Max(0f, this.m_noiseRange - dt * 4f);
		this.m_syncNoiseTimer += dt;
		if (this.m_syncNoiseTimer > 0.5f)
		{
			this.m_syncNoiseTimer = 0f;
			this.m_nview.GetZDO().Set(ZDOVars.s_noise, this.m_noiseRange);
		}
	}

	// Token: 0x060001C9 RID: 457 RVA: 0x000128DC File Offset: 0x00010ADC
	public void AddNoise(float range)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_AddNoise(0L, range);
			return;
		}
		this.m_nview.InvokeRPC("RPC_AddNoise", new object[]
		{
			range
		});
	}

	// Token: 0x060001CA RID: 458 RVA: 0x0001292D File Offset: 0x00010B2D
	private void RPC_AddNoise(long sender, float range)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (range > this.m_noiseRange)
		{
			this.m_noiseRange = range;
			this.m_seman.ModifyNoise(this.m_noiseRange, ref this.m_noiseRange);
		}
	}

	// Token: 0x060001CB RID: 459 RVA: 0x00012964 File Offset: 0x00010B64
	public float GetNoiseRange()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_noiseRange;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_noise, 0f);
	}

	// Token: 0x060001CC RID: 460 RVA: 0x000129B2 File Offset: 0x00010BB2
	public virtual bool InGodMode()
	{
		return false;
	}

	// Token: 0x060001CD RID: 461 RVA: 0x000129B5 File Offset: 0x00010BB5
	public virtual bool InGhostMode()
	{
		return false;
	}

	// Token: 0x060001CE RID: 462 RVA: 0x000129B8 File Offset: 0x00010BB8
	public virtual bool IsDebugFlying()
	{
		return false;
	}

	// Token: 0x060001CF RID: 463 RVA: 0x000129BC File Offset: 0x00010BBC
	public virtual string GetHoverText()
	{
		Tameable component = base.GetComponent<Tameable>();
		if (component)
		{
			return component.GetHoverText();
		}
		return "";
	}

	// Token: 0x060001D0 RID: 464 RVA: 0x000129E4 File Offset: 0x00010BE4
	public virtual string GetHoverName()
	{
		Tameable component = base.GetComponent<Tameable>();
		if (component)
		{
			return component.GetHoverName();
		}
		return Localization.instance.Localize(this.m_name);
	}

	// Token: 0x060001D1 RID: 465 RVA: 0x00012A17 File Offset: 0x00010C17
	public virtual bool IsDrawingBow()
	{
		return false;
	}

	// Token: 0x060001D2 RID: 466 RVA: 0x00012A1A File Offset: 0x00010C1A
	public virtual bool InAttack()
	{
		return false;
	}

	// Token: 0x060001D3 RID: 467 RVA: 0x00012A1D File Offset: 0x00010C1D
	protected virtual void StopEmote()
	{
	}

	// Token: 0x060001D4 RID: 468 RVA: 0x00012A1F File Offset: 0x00010C1F
	public virtual bool InMinorAction()
	{
		return false;
	}

	// Token: 0x060001D5 RID: 469 RVA: 0x00012A22 File Offset: 0x00010C22
	public virtual bool InMinorActionSlowdown()
	{
		return false;
	}

	// Token: 0x060001D6 RID: 470 RVA: 0x00012A25 File Offset: 0x00010C25
	public virtual bool InDodge()
	{
		return false;
	}

	// Token: 0x060001D7 RID: 471 RVA: 0x00012A28 File Offset: 0x00010C28
	public virtual bool IsDodgeInvincible()
	{
		return false;
	}

	// Token: 0x060001D8 RID: 472 RVA: 0x00012A2B File Offset: 0x00010C2B
	public virtual bool InEmote()
	{
		return false;
	}

	// Token: 0x060001D9 RID: 473 RVA: 0x00012A2E File Offset: 0x00010C2E
	public virtual bool IsBlocking()
	{
		return false;
	}

	// Token: 0x060001DA RID: 474 RVA: 0x00012A31 File Offset: 0x00010C31
	public bool IsFlying()
	{
		return this.m_flying;
	}

	// Token: 0x060001DB RID: 475 RVA: 0x00012A39 File Offset: 0x00010C39
	public bool IsKnockedBack()
	{
		return this.m_pushForce != Vector3.zero;
	}

	// Token: 0x060001DC RID: 476 RVA: 0x00012A4C File Offset: 0x00010C4C
	private void OnDrawGizmosSelected()
	{
		if (this.m_nview != null && this.m_nview.GetZDO() != null)
		{
			float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_noise, 0f);
			Gizmos.DrawWireSphere(base.transform.position, @float);
		}
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_swimDepth, new Vector3(1f, 0.05f, 1f));
		if (this.IsOnGround())
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(this.m_lastGroundPoint, this.m_lastGroundPoint + this.m_lastGroundNormal);
		}
	}

	// Token: 0x060001DD RID: 477 RVA: 0x00012B11 File Offset: 0x00010D11
	public virtual bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		return false;
	}

	// Token: 0x060001DE RID: 478 RVA: 0x00012B14 File Offset: 0x00010D14
	protected void RPC_TeleportTo(long sender, Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.TeleportTo(pos, rot, distantTeleport);
	}

	// Token: 0x060001DF RID: 479 RVA: 0x00012B30 File Offset: 0x00010D30
	private void SyncVelocity()
	{
		Vector3 velocity = this.m_body.velocity;
		if (!velocity.Equals(this.m_bodyVelocityCached))
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_bodyVelocity, velocity);
		}
		this.m_bodyVelocityCached = velocity;
	}

	// Token: 0x060001E0 RID: 480 RVA: 0x00012B78 File Offset: 0x00010D78
	public Vector3 GetVelocity()
	{
		if (!this.m_nview.IsValid())
		{
			return Vector3.zero;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_body.velocity;
		}
		return this.m_nview.GetZDO().GetVec3(ZDOVars.s_bodyVelocity, Vector3.zero);
	}

	// Token: 0x060001E1 RID: 481 RVA: 0x00012BCB File Offset: 0x00010DCB
	public void AddRootMotion(Vector3 vel)
	{
		if (this.InDodge() || this.InAttack() || this.InEmote())
		{
			this.m_rootMotion += vel;
		}
	}

	// Token: 0x060001E2 RID: 482 RVA: 0x00012BF8 File Offset: 0x00010DF8
	private void ApplyRootMotion(ref Vector3 vel)
	{
		Vector3 vector = this.m_rootMotion * 55f;
		if (vector.magnitude > vel.magnitude)
		{
			vel = vector;
		}
		this.m_rootMotion = Vector3.zero;
	}

	// Token: 0x060001E3 RID: 483 RVA: 0x00012C38 File Offset: 0x00010E38
	public static void GetCharactersInRange(Vector3 point, float radius, List<Character> characters)
	{
		float num = radius * radius;
		foreach (Character character in Character.s_characters)
		{
			if (Utils.DistanceSqr(character.transform.position, point) < num)
			{
				characters.Add(character);
			}
		}
	}

	// Token: 0x060001E4 RID: 484 RVA: 0x00012CA4 File Offset: 0x00010EA4
	public static List<Character> GetAllCharacters()
	{
		return Character.s_characters;
	}

	// Token: 0x060001E5 RID: 485 RVA: 0x00012CAC File Offset: 0x00010EAC
	public static bool IsCharacterInRange(Vector3 point, float range)
	{
		using (List<Character>.Enumerator enumerator = Character.s_characters.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.transform.position, point) < range)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x060001E6 RID: 486 RVA: 0x00012D10 File Offset: 0x00010F10
	public virtual void OnTargeted(bool sensed, bool alerted)
	{
	}

	// Token: 0x060001E7 RID: 487 RVA: 0x00012D12 File Offset: 0x00010F12
	public GameObject GetVisual()
	{
		return this.m_visual;
	}

	// Token: 0x060001E8 RID: 488 RVA: 0x00012D1C File Offset: 0x00010F1C
	protected void UpdateLodgroup()
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		Renderer[] componentsInChildren = this.m_visual.GetComponentsInChildren<Renderer>();
		LOD[] lods = this.m_lodGroup.GetLODs();
		lods[0].renderers = componentsInChildren;
		this.m_lodGroup.SetLODs(lods);
	}

	// Token: 0x060001E9 RID: 489 RVA: 0x00012D69 File Offset: 0x00010F69
	public virtual bool IsSitting()
	{
		return false;
	}

	// Token: 0x060001EA RID: 490 RVA: 0x00012D6C File Offset: 0x00010F6C
	public virtual float GetEquipmentMovementModifier()
	{
		return 0f;
	}

	// Token: 0x060001EB RID: 491 RVA: 0x00012D73 File Offset: 0x00010F73
	public virtual float GetEquipmentHomeItemModifier()
	{
		return 0f;
	}

	// Token: 0x060001EC RID: 492 RVA: 0x00012D7A File Offset: 0x00010F7A
	public virtual float GetEquipmentHeatResistanceModifier()
	{
		return 0f;
	}

	// Token: 0x060001ED RID: 493 RVA: 0x00012D81 File Offset: 0x00010F81
	public virtual float GetEquipmentJumpStaminaModifier()
	{
		return 0f;
	}

	// Token: 0x060001EE RID: 494 RVA: 0x00012D88 File Offset: 0x00010F88
	public virtual float GetEquipmentAttackStaminaModifier()
	{
		return 0f;
	}

	// Token: 0x060001EF RID: 495 RVA: 0x00012D8F File Offset: 0x00010F8F
	public virtual float GetEquipmentBlockStaminaModifier()
	{
		return 0f;
	}

	// Token: 0x060001F0 RID: 496 RVA: 0x00012D96 File Offset: 0x00010F96
	public virtual float GetEquipmentDodgeStaminaModifier()
	{
		return 0f;
	}

	// Token: 0x060001F1 RID: 497 RVA: 0x00012D9D File Offset: 0x00010F9D
	public virtual float GetEquipmentSwimStaminaModifier()
	{
		return 0f;
	}

	// Token: 0x060001F2 RID: 498 RVA: 0x00012DA4 File Offset: 0x00010FA4
	public virtual float GetEquipmentSneakStaminaModifier()
	{
		return 0f;
	}

	// Token: 0x060001F3 RID: 499 RVA: 0x00012DAB File Offset: 0x00010FAB
	public virtual float GetEquipmentRunStaminaModifier()
	{
		return 0f;
	}

	// Token: 0x060001F4 RID: 500 RVA: 0x00012DB2 File Offset: 0x00010FB2
	protected virtual float GetJogSpeedFactor()
	{
		return 1f;
	}

	// Token: 0x060001F5 RID: 501 RVA: 0x00012DBC File Offset: 0x00010FBC
	protected virtual float GetRunSpeedFactor()
	{
		if (this.HaveRider())
		{
			float riderSkill = this.m_baseAI.GetRiderSkill();
			return 1f + riderSkill * 0.25f;
		}
		return 1f;
	}

	// Token: 0x060001F6 RID: 502 RVA: 0x00012DF0 File Offset: 0x00010FF0
	protected virtual float GetAttackSpeedFactorMovement()
	{
		return 1f;
	}

	// Token: 0x060001F7 RID: 503 RVA: 0x00012DF7 File Offset: 0x00010FF7
	protected virtual float GetAttackSpeedFactorRotation()
	{
		return 1f;
	}

	// Token: 0x060001F8 RID: 504 RVA: 0x00012E00 File Offset: 0x00011000
	public virtual void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
		if (!this.IsTamed())
		{
			return;
		}
		if (!this.m_tameable)
		{
			this.m_tameable = base.GetComponent<Tameable>();
			this.m_tameableMonsterAI = base.GetComponent<MonsterAI>();
		}
		if (!this.m_tameable || !this.m_tameableMonsterAI)
		{
			ZLog.LogWarning(this.m_name + " is tamed but missing tameable or monster AI script!");
			return;
		}
		if (this.m_tameable.m_levelUpOwnerSkill != Skills.SkillType.None)
		{
			GameObject followTarget = this.m_tameableMonsterAI.GetFollowTarget();
			if (followTarget != null && followTarget)
			{
				Character component = followTarget.GetComponent<Character>();
				if (component != null)
				{
					Skills skills = component.GetSkills();
					if (skills != null)
					{
						skills.RaiseSkill(this.m_tameable.m_levelUpOwnerSkill, value * this.m_tameable.m_levelUpFactor);
						Terminal.Log(string.Format("{0} leveling up from '{1}' to master {2} skill '{3}' at factor {4}", new object[]
						{
							base.name,
							skill,
							component.name,
							this.m_tameable.m_levelUpOwnerSkill,
							value * this.m_tameable.m_levelUpFactor
						}));
					}
				}
			}
		}
	}

	// Token: 0x060001F9 RID: 505 RVA: 0x00012F25 File Offset: 0x00011125
	public virtual Skills GetSkills()
	{
		return null;
	}

	// Token: 0x060001FA RID: 506 RVA: 0x00012F28 File Offset: 0x00011128
	public float GetSkillLevel(Skills.SkillType skillType)
	{
		Skills skills = this.GetSkills();
		if (skills != null)
		{
			return skills.GetSkillLevel(skillType);
		}
		return 0f;
	}

	// Token: 0x060001FB RID: 507 RVA: 0x00012F4C File Offset: 0x0001114C
	public virtual float GetSkillFactor(Skills.SkillType skill)
	{
		return 0f;
	}

	// Token: 0x060001FC RID: 508 RVA: 0x00012F53 File Offset: 0x00011153
	public virtual float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return Mathf.Pow(UnityEngine.Random.Range(0.75f, 1f), 0.5f) * this.m_nview.GetZDO().GetFloat(ZDOVars.s_randomSkillFactor, 1f);
	}

	// Token: 0x060001FD RID: 509 RVA: 0x00012F8C File Offset: 0x0001118C
	public bool IsMonsterFaction(float time)
	{
		return !this.IsTamed(time) && (this.m_faction == Character.Faction.ForestMonsters || this.m_faction == Character.Faction.Undead || this.m_faction == Character.Faction.Demon || this.m_faction == Character.Faction.PlainsMonsters || this.m_faction == Character.Faction.MountainMonsters || this.m_faction == Character.Faction.SeaMonsters || this.m_faction == Character.Faction.MistlandsMonsters);
	}

	// Token: 0x060001FE RID: 510 RVA: 0x00012FE6 File Offset: 0x000111E6
	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	// Token: 0x060001FF RID: 511 RVA: 0x00012FF9 File Offset: 0x000111F9
	public Collider GetLastGroundCollider()
	{
		return this.m_lastGroundCollider;
	}

	// Token: 0x06000200 RID: 512 RVA: 0x00013001 File Offset: 0x00011201
	public Vector3 GetLastGroundNormal()
	{
		return this.m_groundContactNormal;
	}

	// Token: 0x06000201 RID: 513 RVA: 0x00013009 File Offset: 0x00011209
	public void ResetCloth()
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_ResetCloth", Array.Empty<object>());
	}

	// Token: 0x06000202 RID: 514 RVA: 0x00013028 File Offset: 0x00011228
	private void RPC_ResetCloth(long sender)
	{
		foreach (Cloth cloth in base.GetComponentsInChildren<Cloth>())
		{
			if (cloth.enabled)
			{
				cloth.enabled = false;
				cloth.enabled = true;
			}
		}
	}

	// Token: 0x06000203 RID: 515 RVA: 0x00013064 File Offset: 0x00011264
	public virtual bool GetRelativePosition(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		relativeVel = Vector3.zero;
		if (this.IsOnGround() && this.m_lastGroundBody)
		{
			ZNetView component = this.m_lastGroundBody.GetComponent<ZNetView>();
			if (component && component.IsValid())
			{
				parent = component.GetZDO().m_uid;
				attachJoint = "";
				relativePos = component.transform.InverseTransformPoint(base.transform.position);
				relativeRot = Quaternion.Inverse(component.transform.rotation) * base.transform.rotation;
				relativeVel = component.transform.InverseTransformVector(this.m_body.velocity - this.m_lastGroundBody.velocity);
				return true;
			}
		}
		parent = ZDOID.None;
		attachJoint = "";
		relativePos = Vector3.zero;
		relativeRot = Quaternion.identity;
		return false;
	}

	// Token: 0x06000204 RID: 516 RVA: 0x0001316E File Offset: 0x0001136E
	public Quaternion GetLookYaw()
	{
		return this.m_lookYaw;
	}

	// Token: 0x06000205 RID: 517 RVA: 0x00013176 File Offset: 0x00011376
	public Vector3 GetMoveDir()
	{
		return this.m_moveDir;
	}

	// Token: 0x06000206 RID: 518 RVA: 0x0001317E File Offset: 0x0001137E
	public BaseAI GetBaseAI()
	{
		return this.m_baseAI;
	}

	// Token: 0x06000207 RID: 519 RVA: 0x00013186 File Offset: 0x00011386
	public float GetMass()
	{
		return this.m_body.mass;
	}

	// Token: 0x06000208 RID: 520 RVA: 0x00013194 File Offset: 0x00011394
	protected void SetVisible(bool visible)
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		if (this.m_lodVisible == visible)
		{
			return;
		}
		this.m_lodVisible = visible;
		if (this.m_lodVisible)
		{
			this.m_lodGroup.localReferencePoint = this.m_originalLocalRef;
			return;
		}
		this.m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
	}

	// Token: 0x06000209 RID: 521 RVA: 0x000131FA File Offset: 0x000113FA
	public void SetTamed(bool tamed)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_tamed == tamed)
		{
			return;
		}
		this.m_nview.InvokeRPC("RPC_SetTamed", new object[]
		{
			tamed
		});
	}

	// Token: 0x0600020A RID: 522 RVA: 0x00013233 File Offset: 0x00011433
	private void RPC_SetTamed(long sender, bool tamed)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_tamed == tamed)
		{
			return;
		}
		this.m_tamed = tamed;
		this.m_nview.GetZDO().Set(ZDOVars.s_tamed, this.m_tamed);
	}

	// Token: 0x0600020B RID: 523 RVA: 0x00013270 File Offset: 0x00011470
	private bool IsTamed(float time)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.m_nview.GetZDO().IsOwner() && time - this.m_lastTamedCheck > 1f)
		{
			this.m_lastTamedCheck = time;
			this.m_tamed = this.m_nview.GetZDO().GetBool(ZDOVars.s_tamed, this.m_tamed);
		}
		return this.m_tamed;
	}

	// Token: 0x0600020C RID: 524 RVA: 0x000132DB File Offset: 0x000114DB
	public bool IsTamed()
	{
		return this.IsTamed(Time.time);
	}

	// Token: 0x0600020D RID: 525 RVA: 0x000132E8 File Offset: 0x000114E8
	public ZSyncAnimation GetZAnim()
	{
		return this.m_zanim;
	}

	// Token: 0x0600020E RID: 526 RVA: 0x000132F0 File Offset: 0x000114F0
	public SEMan GetSEMan()
	{
		return this.m_seman;
	}

	// Token: 0x0600020F RID: 527 RVA: 0x000132F8 File Offset: 0x000114F8
	public bool InInterior()
	{
		return Character.InInterior(base.transform);
	}

	// Token: 0x06000210 RID: 528 RVA: 0x00013305 File Offset: 0x00011505
	public static bool InInterior(Transform me)
	{
		return Character.InInterior(me.position);
	}

	// Token: 0x06000211 RID: 529 RVA: 0x00013312 File Offset: 0x00011512
	public static bool InInterior(Vector3 position)
	{
		return position.y > 3000f;
	}

	// Token: 0x06000212 RID: 530 RVA: 0x00013321 File Offset: 0x00011521
	public static void SetDPSDebug(bool enabled)
	{
		Character.s_dpsDebugEnabled = enabled;
	}

	// Token: 0x06000213 RID: 531 RVA: 0x00013329 File Offset: 0x00011529
	public static bool IsDPSDebugEnabled()
	{
		return Character.s_dpsDebugEnabled;
	}

	// Token: 0x06000214 RID: 532 RVA: 0x00013330 File Offset: 0x00011530
	public void TakeOff()
	{
		this.m_flying = true;
		this.m_jumpEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.m_animator.SetTrigger("fly_takeoff");
	}

	// Token: 0x06000215 RID: 533 RVA: 0x0001336C File Offset: 0x0001156C
	public void Land()
	{
		this.m_flying = false;
		this.m_animator.SetTrigger("fly_land");
	}

	// Token: 0x06000216 RID: 534 RVA: 0x00013385 File Offset: 0x00011585
	public void FreezeFrame(float duration)
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_FreezeFrame", new object[]
		{
			duration
		});
	}

	// Token: 0x06000217 RID: 535 RVA: 0x000133AB File Offset: 0x000115AB
	private void RPC_FreezeFrame(long sender, float duration)
	{
		this.m_animEvent.FreezeFrame(duration);
	}

	// Token: 0x06000218 RID: 536 RVA: 0x000133B9 File Offset: 0x000115B9
	public void SetExtraMass(float amount)
	{
		this.m_body.mass = this.m_originalMass + amount;
	}

	// Token: 0x17000006 RID: 6
	// (get) Token: 0x06000219 RID: 537 RVA: 0x000133CE File Offset: 0x000115CE
	// (set) Token: 0x0600021A RID: 538 RVA: 0x000133D6 File Offset: 0x000115D6
	public int InNumShipVolumes { get; set; }

	// Token: 0x0600021B RID: 539 RVA: 0x000133E0 File Offset: 0x000115E0
	public int Increment(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] + 1;
		liquids[(int)type] = num;
		return num;
	}

	// Token: 0x0600021C RID: 540 RVA: 0x00013404 File Offset: 0x00011604
	public int Decrement(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] - 1;
		liquids[(int)type] = num;
		return num;
	}

	// Token: 0x17000007 RID: 7
	// (get) Token: 0x0600021D RID: 541 RVA: 0x00013425 File Offset: 0x00011625
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04000222 RID: 546
	private float m_underWorldCheckTimer;

	// Token: 0x04000223 RID: 547
	private static float takeInputDelay = 0f;

	// Token: 0x04000224 RID: 548
	private float currentRotSpeedFactor;

	// Token: 0x04000225 RID: 549
	private float m_standUp;

	// Token: 0x04000226 RID: 550
	private const float c_StandUpTime = 2f;

	// Token: 0x04000227 RID: 551
	private const float c_StandUpTriggerState = -100f;

	// Token: 0x04000228 RID: 552
	private Collider m_lowestContactCollider;

	// Token: 0x04000229 RID: 553
	private bool m_groundContact;

	// Token: 0x0400022A RID: 554
	private Vector3 m_groundContactPoint = Vector3.zero;

	// Token: 0x0400022B RID: 555
	private Vector3 m_groundContactNormal = Vector3.zero;

	// Token: 0x0400022C RID: 556
	private Transform oldParent;

	// Token: 0x0400022D RID: 557
	private int m_cachedCurrentAnimHash;

	// Token: 0x0400022E RID: 558
	private int m_cachedNextAnimHash;

	// Token: 0x0400022F RID: 559
	private int m_cachedNextOrCurrentAnimHash;

	// Token: 0x04000230 RID: 560
	private int m_cachedAnimHashFrame;

	// Token: 0x04000231 RID: 561
	public ZNetView m_nViewOverride;

	// Token: 0x04000232 RID: 562
	public Action<float, Character> m_onDamaged;

	// Token: 0x04000233 RID: 563
	public Action m_onDeath;

	// Token: 0x04000234 RID: 564
	public Action<int> m_onLevelSet;

	// Token: 0x04000235 RID: 565
	public Action<Vector3> m_onLand;

	// Token: 0x04000236 RID: 566
	[Header("Character")]
	public string m_name = "";

	// Token: 0x04000237 RID: 567
	public string m_group = "";

	// Token: 0x04000238 RID: 568
	public Character.Faction m_faction = Character.Faction.AnimalsVeg;

	// Token: 0x04000239 RID: 569
	public bool m_boss;

	// Token: 0x0400023A RID: 570
	public bool m_dontHideBossHud;

	// Token: 0x0400023B RID: 571
	public string m_bossEvent = "";

	// Token: 0x0400023C RID: 572
	[global::Tooltip("Also sets player unique key")]
	public string m_defeatSetGlobalKey = "";

	// Token: 0x0400023D RID: 573
	public bool m_aiSkipTarget;

	// Token: 0x0400023E RID: 574
	[Header("Movement & Physics")]
	public float m_crouchSpeed = 2f;

	// Token: 0x0400023F RID: 575
	public float m_walkSpeed = 5f;

	// Token: 0x04000240 RID: 576
	public float m_speed = 10f;

	// Token: 0x04000241 RID: 577
	public float m_turnSpeed = 300f;

	// Token: 0x04000242 RID: 578
	public float m_runSpeed = 20f;

	// Token: 0x04000243 RID: 579
	public float m_runTurnSpeed = 300f;

	// Token: 0x04000244 RID: 580
	public float m_flySlowSpeed = 5f;

	// Token: 0x04000245 RID: 581
	public float m_flyFastSpeed = 12f;

	// Token: 0x04000246 RID: 582
	public float m_flyTurnSpeed = 12f;

	// Token: 0x04000247 RID: 583
	public float m_acceleration = 1f;

	// Token: 0x04000248 RID: 584
	public float m_jumpForce = 10f;

	// Token: 0x04000249 RID: 585
	public float m_jumpForceForward;

	// Token: 0x0400024A RID: 586
	public float m_jumpForceTiredFactor = 0.7f;

	// Token: 0x0400024B RID: 587
	public float m_airControl = 0.1f;

	// Token: 0x0400024C RID: 588
	public bool m_canSwim = true;

	// Token: 0x0400024D RID: 589
	public float m_swimDepth = 2f;

	// Token: 0x0400024E RID: 590
	public float m_swimSpeed = 2f;

	// Token: 0x0400024F RID: 591
	public float m_swimTurnSpeed = 100f;

	// Token: 0x04000250 RID: 592
	public float m_swimAcceleration = 0.05f;

	// Token: 0x04000251 RID: 593
	public Character.GroundTiltType m_groundTilt;

	// Token: 0x04000252 RID: 594
	public float m_groundTiltSpeed = 50f;

	// Token: 0x04000253 RID: 595
	public bool m_flying;

	// Token: 0x04000254 RID: 596
	public float m_jumpStaminaUsage = 10f;

	// Token: 0x04000255 RID: 597
	public bool m_disableWhileSleeping;

	// Token: 0x04000256 RID: 598
	[Header("Bodyparts")]
	public Transform m_eye;

	// Token: 0x04000257 RID: 599
	protected Transform m_head;

	// Token: 0x04000258 RID: 600
	[Header("Effects")]
	public EffectList m_hitEffects = new EffectList();

	// Token: 0x04000259 RID: 601
	public EffectList m_critHitEffects = new EffectList();

	// Token: 0x0400025A RID: 602
	public EffectList m_backstabHitEffects = new EffectList();

	// Token: 0x0400025B RID: 603
	public EffectList m_deathEffects = new EffectList();

	// Token: 0x0400025C RID: 604
	public EffectList m_waterEffects = new EffectList();

	// Token: 0x0400025D RID: 605
	public EffectList m_tarEffects = new EffectList();

	// Token: 0x0400025E RID: 606
	public EffectList m_slideEffects = new EffectList();

	// Token: 0x0400025F RID: 607
	public EffectList m_jumpEffects = new EffectList();

	// Token: 0x04000260 RID: 608
	public EffectList m_flyingContinuousEffect = new EffectList();

	// Token: 0x04000261 RID: 609
	public EffectList m_pheromoneLoveEffect = new EffectList();

	// Token: 0x04000262 RID: 610
	[Header("Health & Damage")]
	public bool m_tolerateWater = true;

	// Token: 0x04000263 RID: 611
	public bool m_tolerateFire;

	// Token: 0x04000264 RID: 612
	public bool m_tolerateSmoke = true;

	// Token: 0x04000265 RID: 613
	public bool m_tolerateTar;

	// Token: 0x04000266 RID: 614
	public float m_health = 10f;

	// Token: 0x04000267 RID: 615
	public HitData.DamageModifiers m_damageModifiers;

	// Token: 0x04000268 RID: 616
	public WeakSpot[] m_weakSpots;

	// Token: 0x04000269 RID: 617
	public bool m_staggerWhenBlocked = true;

	// Token: 0x0400026A RID: 618
	public float m_staggerDamageFactor;

	// Token: 0x0400026B RID: 619
	private float m_staggerDamage;

	// Token: 0x0400026C RID: 620
	private float m_backstabTime = -99999f;

	// Token: 0x0400026D RID: 621
	private GameObject[] m_waterEffects_instances;

	// Token: 0x0400026E RID: 622
	private GameObject[] m_slideEffects_instances;

	// Token: 0x0400026F RID: 623
	private GameObject[] m_flyingEffects_instances;

	// Token: 0x04000270 RID: 624
	protected Vector3 m_moveDir = Vector3.zero;

	// Token: 0x04000271 RID: 625
	protected Vector3 m_lookDir = Vector3.forward;

	// Token: 0x04000272 RID: 626
	protected Quaternion m_lookYaw = Quaternion.identity;

	// Token: 0x04000273 RID: 627
	protected bool m_run;

	// Token: 0x04000274 RID: 628
	protected bool m_walk;

	// Token: 0x04000275 RID: 629
	private Vector3 m_lookTransitionStart;

	// Token: 0x04000276 RID: 630
	private Vector3 m_lookTransitionTarget;

	// Token: 0x04000277 RID: 631
	protected float m_lookTransitionTime;

	// Token: 0x04000278 RID: 632
	protected float m_lookTransitionTimeTotal;

	// Token: 0x04000279 RID: 633
	protected bool m_attack;

	// Token: 0x0400027A RID: 634
	protected bool m_attackHold;

	// Token: 0x0400027B RID: 635
	protected bool m_secondaryAttack;

	// Token: 0x0400027C RID: 636
	protected bool m_secondaryAttackHold;

	// Token: 0x0400027D RID: 637
	protected bool m_blocking;

	// Token: 0x0400027E RID: 638
	protected GameObject m_visual;

	// Token: 0x0400027F RID: 639
	protected LODGroup m_lodGroup;

	// Token: 0x04000280 RID: 640
	protected Rigidbody m_body;

	// Token: 0x04000281 RID: 641
	protected CapsuleCollider m_collider;

	// Token: 0x04000282 RID: 642
	protected ZNetView m_nview;

	// Token: 0x04000283 RID: 643
	protected ZSyncAnimation m_zanim;

	// Token: 0x04000284 RID: 644
	protected Animator m_animator;

	// Token: 0x04000285 RID: 645
	protected CharacterAnimEvent m_animEvent;

	// Token: 0x04000286 RID: 646
	protected BaseAI m_baseAI;

	// Token: 0x04000287 RID: 647
	private const float c_MaxFallHeight = 20f;

	// Token: 0x04000288 RID: 648
	private const float c_MinFallHeight = 4f;

	// Token: 0x04000289 RID: 649
	private const float c_MaxFallDamage = 100f;

	// Token: 0x0400028A RID: 650
	private const float c_StaggerDamageBonus = 2f;

	// Token: 0x0400028B RID: 651
	private const float c_AutoJumpInterval = 0.5f;

	// Token: 0x0400028C RID: 652
	private const float c_MinSlideDegreesPlayer = 38f;

	// Token: 0x0400028D RID: 653
	private const float c_MinSlideDegreesMount = 45f;

	// Token: 0x0400028E RID: 654
	private const float c_MinSlideDegreesMonster = 90f;

	// Token: 0x0400028F RID: 655
	private const float c_RootMotionMultiplier = 55f;

	// Token: 0x04000290 RID: 656
	private const float c_PushForceScale = 2.5f;

	// Token: 0x04000291 RID: 657
	private const float c_ContinuousPushForce = 20f;

	// Token: 0x04000292 RID: 658
	private const float c_PushForceDissipation = 100f;

	// Token: 0x04000293 RID: 659
	private const float c_MaxMoveForce = 20f;

	// Token: 0x04000294 RID: 660
	private const float c_StaggerResetTime = 5f;

	// Token: 0x04000295 RID: 661
	private const float c_BackstabResetTime = 300f;

	// Token: 0x04000296 RID: 662
	private const float m_slopeStaminaDrain = 10f;

	// Token: 0x04000297 RID: 663
	public const float m_minSlideDegreesPlayer = 38f;

	// Token: 0x04000298 RID: 664
	public const float m_minSlideDegreesMount = 45f;

	// Token: 0x04000299 RID: 665
	public const float m_minSlideDegreesMonster = 90f;

	// Token: 0x0400029A RID: 666
	private const float m_rootMotionMultiplier = 55f;

	// Token: 0x0400029B RID: 667
	private const float m_pushForceScale = 2.5f;

	// Token: 0x0400029C RID: 668
	private const float m_continousPushForce = 20f;

	// Token: 0x0400029D RID: 669
	private const float m_pushForcedissipation = 100f;

	// Token: 0x0400029E RID: 670
	private const float m_maxMoveForce = 20f;

	// Token: 0x0400029F RID: 671
	private const float m_staggerResetTime = 5f;

	// Token: 0x040002A0 RID: 672
	private const float m_backstabResetTime = 300f;

	// Token: 0x040002A1 RID: 673
	private float m_jumpTimer;

	// Token: 0x040002A2 RID: 674
	private float m_lastAutoJumpTime;

	// Token: 0x040002A3 RID: 675
	private float m_lastGroundTouch;

	// Token: 0x040002A4 RID: 676
	private Vector3 m_lastGroundNormal = Vector3.up;

	// Token: 0x040002A5 RID: 677
	private Vector3 m_lastGroundPoint = Vector3.up;

	// Token: 0x040002A6 RID: 678
	private Collider m_lastGroundCollider;

	// Token: 0x040002A7 RID: 679
	private Rigidbody m_lastGroundBody;

	// Token: 0x040002A8 RID: 680
	private float m_groundForceTimer;

	// Token: 0x040002A9 RID: 681
	private float m_originalMass;

	// Token: 0x040002AA RID: 682
	private Vector3 m_lastAttachPos = Vector3.zero;

	// Token: 0x040002AB RID: 683
	private Rigidbody m_lastAttachBody;

	// Token: 0x040002AC RID: 684
	protected float m_maxAirAltitude = -10000f;

	// Token: 0x040002AD RID: 685
	private float m_waterLevel = -10000f;

	// Token: 0x040002AE RID: 686
	private float m_tarLevel = -10000f;

	// Token: 0x040002AF RID: 687
	private float m_liquidLevel = -10000f;

	// Token: 0x040002B0 RID: 688
	private float m_swimTimer = 999f;

	// Token: 0x040002B1 RID: 689
	private float m_lavaTimer = 999f;

	// Token: 0x040002B2 RID: 690
	private float m_aboveOrInLavaTimer = 999f;

	// Token: 0x040002B3 RID: 691
	private float m_fallTimer;

	// Token: 0x040002B4 RID: 692
	protected SEMan m_seman;

	// Token: 0x040002B5 RID: 693
	private float m_noiseRange;

	// Token: 0x040002B6 RID: 694
	private float m_syncNoiseTimer;

	// Token: 0x040002B7 RID: 695
	private bool m_tamed;

	// Token: 0x040002B8 RID: 696
	private float m_lastTamedCheck;

	// Token: 0x040002B9 RID: 697
	private Tameable m_tameable;

	// Token: 0x040002BA RID: 698
	private MonsterAI m_tameableMonsterAI;

	// Token: 0x040002BB RID: 699
	private int m_level = 1;

	// Token: 0x040002BC RID: 700
	private RaycastHit[] m_lavaRoofCheck = new RaycastHit[1];

	// Token: 0x040002BD RID: 701
	private bool m_localPlayerHasHit;

	// Token: 0x040002BE RID: 702
	protected HitData m_lastHit;

	// Token: 0x040002BF RID: 703
	private Vector3 m_currentVel = Vector3.zero;

	// Token: 0x040002C0 RID: 704
	private float m_currentTurnVel;

	// Token: 0x040002C1 RID: 705
	private float m_currentTurnVelChange;

	// Token: 0x040002C2 RID: 706
	private Vector3 m_groundTiltNormal = Vector3.up;

	// Token: 0x040002C3 RID: 707
	protected Vector3 m_pushForce = Vector3.zero;

	// Token: 0x040002C4 RID: 708
	private Vector3 m_rootMotion = Vector3.zero;

	// Token: 0x040002C5 RID: 709
	private static readonly int s_forwardSpeed = ZSyncAnimation.GetHash("forward_speed");

	// Token: 0x040002C6 RID: 710
	private static readonly int s_sidewaySpeed = ZSyncAnimation.GetHash("sideway_speed");

	// Token: 0x040002C7 RID: 711
	private static readonly int s_turnSpeed = ZSyncAnimation.GetHash("turn_speed");

	// Token: 0x040002C8 RID: 712
	private static readonly int s_inWater = ZSyncAnimation.GetHash("inWater");

	// Token: 0x040002C9 RID: 713
	private static readonly int s_onGround = ZSyncAnimation.GetHash("onGround");

	// Token: 0x040002CA RID: 714
	private static readonly int s_encumbered = ZSyncAnimation.GetHash("encumbered");

	// Token: 0x040002CB RID: 715
	private static readonly int s_flying = ZSyncAnimation.GetHash("flying");

	// Token: 0x040002CC RID: 716
	private float m_slippage;

	// Token: 0x040002CD RID: 717
	protected bool m_wallRunning;

	// Token: 0x040002CE RID: 718
	private bool m_sliding;

	// Token: 0x040002CF RID: 719
	private bool m_running;

	// Token: 0x040002D0 RID: 720
	private bool m_walking;

	// Token: 0x040002D1 RID: 721
	private Vector3 m_originalLocalRef;

	// Token: 0x040002D2 RID: 722
	private bool m_lodVisible = true;

	// Token: 0x040002D3 RID: 723
	private static int s_smokeRayMask = 0;

	// Token: 0x040002D4 RID: 724
	private float m_smokeCheckTimer;

	// Token: 0x040002D5 RID: 725
	[Header("Heat & Lava")]
	private float m_minLavaMaskThreshold = 0.05f;

	// Token: 0x040002D6 RID: 726
	public float m_heatBuildupBase = 1.5f;

	// Token: 0x040002D7 RID: 727
	public float m_heatCooldownBase = 1f;

	// Token: 0x040002D8 RID: 728
	public float m_heatBuildupWater = 2f;

	// Token: 0x040002D9 RID: 729
	public float m_heatWaterTouchMultiplier = 0.2f;

	// Token: 0x040002DA RID: 730
	public float m_lavaDamageTickInterval = 0.2f;

	// Token: 0x040002DB RID: 731
	public float m_heatLevelFirstDamageThreshold = 0.7f;

	// Token: 0x040002DC RID: 732
	public float m_lavaFirstDamage = 10f;

	// Token: 0x040002DD RID: 733
	public float m_lavaFullDamage = 100f;

	// Token: 0x040002DE RID: 734
	public float m_lavaAirDamageHeight = 3f;

	// Token: 0x040002DF RID: 735
	public float m_dayHeatGainRunning = 0.2f;

	// Token: 0x040002E0 RID: 736
	public float m_dayHeatGainStill = -0.05f;

	// Token: 0x040002E1 RID: 737
	public float m_dayHeatEquipmentStop = 0.5f;

	// Token: 0x040002E2 RID: 738
	public float m_lavaSlowMax = 0.5f;

	// Token: 0x040002E3 RID: 739
	public float m_lavaSlowHeight = 0.8f;

	// Token: 0x040002E4 RID: 740
	public EffectList m_lavaHeatEffects = new EffectList();

	// Token: 0x040002E5 RID: 741
	private Dictionary<ParticleSystem, float> m_lavaHeatParticles = new Dictionary<ParticleSystem, float>();

	// Token: 0x040002E6 RID: 742
	private List<ZSFX> m_lavaHeatAudio = new List<ZSFX>();

	// Token: 0x040002E7 RID: 743
	private float m_lavaHeatLevel;

	// Token: 0x040002E8 RID: 744
	private float m_lavaProximity;

	// Token: 0x040002E9 RID: 745
	private float m_lavaHeightFactor;

	// Token: 0x040002EA RID: 746
	private float m_lavaDamageTimer;

	// Token: 0x040002EB RID: 747
	private static bool s_dpsDebugEnabled = false;

	// Token: 0x040002EC RID: 748
	private static readonly List<KeyValuePair<float, float>> s_enemyDamage = new List<KeyValuePair<float, float>>();

	// Token: 0x040002ED RID: 749
	private static readonly List<KeyValuePair<float, float>> s_playerDamage = new List<KeyValuePair<float, float>>();

	// Token: 0x040002EE RID: 750
	private static readonly List<Character> s_characters = new List<Character>();

	// Token: 0x040002EF RID: 751
	private static int s_characterLayer = 0;

	// Token: 0x040002F0 RID: 752
	private static int s_characterNetLayer = 0;

	// Token: 0x040002F1 RID: 753
	private static int s_characterGhostLayer = 0;

	// Token: 0x040002F2 RID: 754
	protected static int s_groundRayMask = 0;

	// Token: 0x040002F3 RID: 755
	protected static int s_characterLayerMask = 0;

	// Token: 0x040002F4 RID: 756
	private static int s_blockedRayMask;

	// Token: 0x040002F5 RID: 757
	private float m_pheromoneTimer = 5f;

	// Token: 0x040002F6 RID: 758
	private float m_cashedInLiquidDepth;

	// Token: 0x040002F7 RID: 759
	private Quaternion m_tiltRotCached = Quaternion.identity;

	// Token: 0x040002F8 RID: 760
	private Vector3 m_bodyVelocityCached = Vector3.negativeInfinity;

	// Token: 0x040002F9 RID: 761
	protected static readonly int s_animatorTagFreeze = ZSyncAnimation.GetHash("freeze");

	// Token: 0x040002FA RID: 762
	protected static readonly int s_animatorTagStagger = ZSyncAnimation.GetHash("stagger");

	// Token: 0x040002FB RID: 763
	protected static readonly int s_animatorTagSitting = ZSyncAnimation.GetHash("sitting");

	// Token: 0x040002FC RID: 764
	private static readonly int s_animatorFalling = ZSyncAnimation.GetHash("falling");

	// Token: 0x040002FD RID: 765
	private static readonly int s_tilt = ZSyncAnimation.GetHash("tilt");

	// Token: 0x040002FE RID: 766
	public static int m_debugFlySpeed = 20;

	// Token: 0x04000300 RID: 768
	private readonly int[] m_liquids = new int[2];

	// Token: 0x0200022B RID: 555
	public enum Faction
	{
		// Token: 0x04001F45 RID: 8005
		Players,
		// Token: 0x04001F46 RID: 8006
		AnimalsVeg,
		// Token: 0x04001F47 RID: 8007
		ForestMonsters,
		// Token: 0x04001F48 RID: 8008
		Undead,
		// Token: 0x04001F49 RID: 8009
		Demon,
		// Token: 0x04001F4A RID: 8010
		MountainMonsters,
		// Token: 0x04001F4B RID: 8011
		SeaMonsters,
		// Token: 0x04001F4C RID: 8012
		PlainsMonsters,
		// Token: 0x04001F4D RID: 8013
		Boss,
		// Token: 0x04001F4E RID: 8014
		MistlandsMonsters,
		// Token: 0x04001F4F RID: 8015
		Dverger,
		// Token: 0x04001F50 RID: 8016
		PlayerSpawned
	}

	// Token: 0x0200022C RID: 556
	public enum GroundTiltType
	{
		// Token: 0x04001F52 RID: 8018
		None,
		// Token: 0x04001F53 RID: 8019
		Pitch,
		// Token: 0x04001F54 RID: 8020
		Full,
		// Token: 0x04001F55 RID: 8021
		PitchRaycast,
		// Token: 0x04001F56 RID: 8022
		FullRaycast,
		// Token: 0x04001F57 RID: 8023
		Flying
	}
}
