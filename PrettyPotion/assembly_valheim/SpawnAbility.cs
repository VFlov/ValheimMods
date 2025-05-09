using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000008 RID: 8
public class SpawnAbility : MonoBehaviour, IProjectile
{
	// Token: 0x06000061 RID: 97 RVA: 0x00007D2B File Offset: 0x00005F2B
	public void Awake()
	{
		if (this.m_spawnOnAwake)
		{
			base.StartCoroutine("Spawn");
		}
	}

	// Token: 0x06000062 RID: 98 RVA: 0x00007D41 File Offset: 0x00005F41
	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		this.m_weapon = item;
		base.StartCoroutine("Spawn");
	}

	// Token: 0x06000063 RID: 99 RVA: 0x00007D5E File Offset: 0x00005F5E
	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	// Token: 0x06000064 RID: 100 RVA: 0x00007D65 File Offset: 0x00005F65
	private IEnumerator Spawn()
	{
		if (this.m_initialSpawnDelay > 0f)
		{
			yield return new WaitForSeconds(this.m_initialSpawnDelay);
		}
		int toSpawn = UnityEngine.Random.Range(this.m_minToSpawn, this.m_maxToSpawn);
		Skills skills = this.m_owner ? this.m_owner.GetSkills() : null;
		int num;
		for (int i = 0; i < toSpawn; i = num)
		{
			Vector3 targetPosition = base.transform.position;
			bool foundSpawnPoint = false;
			int tries = (this.m_targetType == SpawnAbility.TargetType.RandomPathfindablePosition) ? 5 : 1;
			int j = 0;
			while (j < tries && !(foundSpawnPoint = this.FindTarget(out targetPosition, i, toSpawn)))
			{
				if (this.m_targetType == SpawnAbility.TargetType.RandomPathfindablePosition)
				{
					if (j == tries - 1)
					{
						Terminal.LogWarning(string.Format("SpawnAbility failed to pathfindable target after {0} tries, defaulting to transform position.", tries));
						targetPosition = base.transform.position;
						foundSpawnPoint = true;
					}
					else
					{
						Terminal.Log("SpawnAbility failed to pathfindable target, waiting before retry.");
						yield return new WaitForSeconds(0.2f);
					}
				}
				num = j;
				j = num + 1;
			}
			if (!foundSpawnPoint)
			{
				Terminal.LogWarning("SpawnAbility failed to find spawn point, aborting spawn.");
			}
			else
			{
				Vector3 spawnPoint = targetPosition;
				if (this.m_targetType != SpawnAbility.TargetType.RandomPathfindablePosition)
				{
					Vector3 vector = this.m_spawnAtTarget ? targetPosition : base.transform.position;
					Vector2 vector2 = UnityEngine.Random.insideUnitCircle * this.m_spawnRadius;
					if (this.m_circleSpawn)
					{
						vector2 = this.GetCirclePoint(i, toSpawn) * this.m_spawnRadius;
					}
					spawnPoint = vector + new Vector3(vector2.x, 0f, vector2.y);
					if (this.m_snapToTerrain)
					{
						float y;
						ZoneSystem.instance.GetSolidHeight(spawnPoint, out y, this.m_getSolidHeightMargin);
						spawnPoint.y = y;
					}
					spawnPoint.y += this.m_spawnGroundOffset;
					if (Mathf.Abs(spawnPoint.y - vector.y) > 100f)
					{
						goto IL_645;
					}
				}
				GameObject prefab = this.m_spawnPrefab[UnityEngine.Random.Range(0, this.m_spawnPrefab.Length)];
				if (this.m_maxSpawned > 0 && SpawnSystem.GetNrOfInstances(prefab) >= this.m_maxSpawned)
				{
					Player player = this.m_owner as Player;
					if (player != null)
					{
						player.Message(MessageHud.MessageType.Center, this.m_maxSummonReached, 0, null);
					}
				}
				else
				{
					this.m_preSpawnEffects.Create(spawnPoint, Quaternion.identity, null, 1f, -1);
					if (this.m_preSpawnDelay > 0f)
					{
						yield return new WaitForSeconds(this.m_preSpawnDelay);
					}
					Terminal.Log("SpawnAbility spawning a " + prefab.name);
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, spawnPoint, Quaternion.Euler(0f, UnityEngine.Random.value * 3.1415927f * 2f, 0f));
					ZNetView component = gameObject.GetComponent<ZNetView>();
					Projectile component2 = gameObject.GetComponent<Projectile>();
					if (component2)
					{
						this.SetupProjectile(component2, targetPosition);
					}
					if (this.m_randomYRotation)
					{
						gameObject.transform.Rotate(Vector3.up, (float)UnityEngine.Random.Range(-180, 180));
					}
					if (skills)
					{
						if (this.m_copySkill != Skills.SkillType.None && this.m_copySkillToRandomFactor > 0f)
						{
							component.GetZDO().Set(ZDOVars.s_randomSkillFactor, 1f + skills.GetSkillLevel(this.m_copySkill) * this.m_copySkillToRandomFactor);
						}
						if (this.m_levelUpSettings.Count > 0)
						{
							Character component3 = gameObject.GetComponent<Character>();
							if (component3 != null)
							{
								int k = this.m_levelUpSettings.Count - 1;
								while (k >= 0)
								{
									SpawnAbility.LevelUpSettings levelUpSettings = this.m_levelUpSettings[k];
									if (skills.GetSkillLevel(levelUpSettings.m_skill) >= (float)levelUpSettings.m_skillLevel)
									{
										component3.SetLevel(levelUpSettings.m_setLevel);
										int num2 = this.m_setMaxInstancesFromWeaponLevel ? this.m_weapon.m_quality : levelUpSettings.m_maxSpawns;
										if (num2 > 0)
										{
											component.GetZDO().Set(ZDOVars.s_maxInstances, num2, false);
											break;
										}
										break;
									}
									else
									{
										k--;
									}
								}
							}
						}
					}
					if (this.m_commandOnSpawn)
					{
						Tameable component4 = gameObject.GetComponent<Tameable>();
						if (component4 != null)
						{
							Humanoid humanoid = this.m_owner as Humanoid;
							if (humanoid != null)
							{
								component4.Command(humanoid, false);
								if (humanoid == Player.m_localPlayer)
								{
									Game.instance.IncrementPlayerStat(PlayerStatType.SkeletonSummons, 1f);
								}
							}
						}
					}
					if (this.m_wakeUpAnimation)
					{
						ZSyncAnimation component5 = gameObject.GetComponent<ZSyncAnimation>();
						if (component5 != null)
						{
							component5.SetBool("wakeup", true);
						}
					}
					BaseAI component6 = gameObject.GetComponent<BaseAI>();
					if (component6 != null)
					{
						if (this.m_alertSpawnedCreature)
						{
							component6.Alert();
						}
						BaseAI baseAI = this.m_owner.GetBaseAI();
						if (component6.m_aggravatable && baseAI && baseAI.m_aggravatable)
						{
							component6.SetAggravated(baseAI.IsAggravated(), BaseAI.AggravatedReason.Damage);
						}
						if (this.m_passiveAggressive)
						{
							component6.m_passiveAggresive = true;
						}
					}
					this.SetupAoe(gameObject.GetComponent<Character>(), spawnPoint);
					this.m_spawnEffects.Create(spawnPoint, Quaternion.identity, null, 1f, -1);
					if (this.m_spawnDelay > 0f)
					{
						yield return new WaitForSeconds(this.m_spawnDelay);
					}
					targetPosition = default(Vector3);
					spawnPoint = default(Vector3);
					prefab = null;
				}
			}
			IL_645:
			num = i + 1;
		}
		if (!this.m_spawnOnAwake)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		yield break;
	}

	// Token: 0x06000065 RID: 101 RVA: 0x00007D74 File Offset: 0x00005F74
	private Vector3 GetRandomConeDirection()
	{
		float num = (float)UnityEngine.Random.Range(0, 360);
		float f = UnityEngine.Random.Range(this.m_randomAngleMin, this.m_randomAngleMax);
		return Quaternion.AngleAxis(num, Vector3.up) * new Vector3(Mathf.Sin(f), Mathf.Cos(f), 0f);
	}

	// Token: 0x06000066 RID: 102 RVA: 0x00007DC4 File Offset: 0x00005FC4
	private void SetupProjectile(Projectile projectile, Vector3 targetPoint)
	{
		Vector3 vector = this.m_randomDirection ? this.GetRandomConeDirection() : (targetPoint - projectile.transform.position).normalized;
		Vector3 axis = Vector3.Cross(vector, Vector3.up);
		Quaternion rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(-this.m_projectileAccuracy, this.m_projectileAccuracy), Vector3.up);
		vector = Quaternion.AngleAxis(UnityEngine.Random.Range(-this.m_projectileAccuracy, this.m_projectileAccuracy), axis) * vector;
		vector = rotation * vector;
		float d = (this.m_projectileVelocityMax > 0f) ? UnityEngine.Random.Range(this.m_projectileVelocity, this.m_projectileVelocityMax) : this.m_projectileVelocity;
		projectile.Setup(this.m_owner, vector * d, -1f, null, null, null);
	}

	// Token: 0x06000067 RID: 103 RVA: 0x00007E8C File Offset: 0x0000608C
	private void SetupAoe(Character owner, Vector3 targetPoint)
	{
		if (this.m_aoePrefab == null || owner == null)
		{
			return;
		}
		Aoe component = UnityEngine.Object.Instantiate<GameObject>(this.m_aoePrefab, targetPoint, Quaternion.identity).GetComponent<Aoe>();
		if (component == null)
		{
			return;
		}
		component.Setup(owner, Vector3.zero, -1f, null, null, null);
	}

	// Token: 0x06000068 RID: 104 RVA: 0x00007EE8 File Offset: 0x000060E8
	private bool FindTarget(out Vector3 point, int i, int spawnCount)
	{
		point = Vector3.zero;
		switch (this.m_targetType)
		{
		case SpawnAbility.TargetType.ClosestEnemy:
		{
			if (this.m_owner == null)
			{
				return false;
			}
			Character character = BaseAI.FindClosestEnemy(this.m_owner, base.transform.position, this.m_maxTargetRange);
			if (character != null)
			{
				point = character.transform.position;
				return true;
			}
			return false;
		}
		case SpawnAbility.TargetType.RandomEnemy:
		{
			if (this.m_owner == null)
			{
				return false;
			}
			Character character2 = BaseAI.FindRandomEnemy(this.m_owner, base.transform.position, this.m_maxTargetRange);
			if (character2 != null)
			{
				point = character2.transform.position;
				return true;
			}
			return false;
		}
		case SpawnAbility.TargetType.Caster:
			if (this.m_owner == null)
			{
				return false;
			}
			point = this.m_owner.transform.position;
			return true;
		case SpawnAbility.TargetType.Position:
			point = base.transform.position;
			return true;
		case SpawnAbility.TargetType.RandomPathfindablePosition:
		{
			if (this.m_owner == null)
			{
				return false;
			}
			List<Vector3> list = new List<Vector3>();
			Vector2 vector = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(this.m_spawnRadius / 2f, this.m_spawnRadius);
			point = base.transform.position + new Vector3(vector.x, 2f, vector.y);
			float y;
			ZoneSystem.instance.GetSolidHeight(point, out y, 2);
			point.y = y;
			if (Pathfinding.instance.GetPath(this.m_owner.transform.position, point, list, this.m_targetWhenPathfindingType, true, false, true))
			{
				Terminal.Log(string.Format("SpawnAbility found path target, distance: {0}", Vector3.Distance(base.transform.position, list[0])));
				point = list[list.Count - 1];
				return true;
			}
			return false;
		}
		default:
			return false;
		}
	}

	// Token: 0x06000069 RID: 105 RVA: 0x000080F8 File Offset: 0x000062F8
	private Vector2 GetCirclePoint(int i, int spawnCount)
	{
		float num = (float)i / (float)spawnCount;
		float x = Mathf.Sin(num * 3.1415927f * 2f);
		float y = Mathf.Cos(num * 3.1415927f * 2f);
		return new Vector2(x, y);
	}

	// Token: 0x0400012B RID: 299
	[Header("Spawn")]
	public GameObject[] m_spawnPrefab;

	// Token: 0x0400012C RID: 300
	public string m_maxSummonReached = "$hud_maxsummonsreached";

	// Token: 0x0400012D RID: 301
	public bool m_spawnOnAwake;

	// Token: 0x0400012E RID: 302
	public bool m_alertSpawnedCreature = true;

	// Token: 0x0400012F RID: 303
	public bool m_passiveAggressive;

	// Token: 0x04000130 RID: 304
	public bool m_spawnAtTarget = true;

	// Token: 0x04000131 RID: 305
	public int m_minToSpawn = 1;

	// Token: 0x04000132 RID: 306
	public int m_maxToSpawn = 1;

	// Token: 0x04000133 RID: 307
	public int m_maxSpawned = 3;

	// Token: 0x04000134 RID: 308
	public float m_spawnRadius = 3f;

	// Token: 0x04000135 RID: 309
	public bool m_circleSpawn;

	// Token: 0x04000136 RID: 310
	public bool m_snapToTerrain = true;

	// Token: 0x04000137 RID: 311
	[global::Tooltip("Used to give random Y rotations to things like AOEs that aren't circular")]
	public bool m_randomYRotation;

	// Token: 0x04000138 RID: 312
	public float m_spawnGroundOffset;

	// Token: 0x04000139 RID: 313
	public int m_getSolidHeightMargin = 1000;

	// Token: 0x0400013A RID: 314
	public float m_initialSpawnDelay;

	// Token: 0x0400013B RID: 315
	public float m_spawnDelay;

	// Token: 0x0400013C RID: 316
	public float m_preSpawnDelay;

	// Token: 0x0400013D RID: 317
	public bool m_commandOnSpawn;

	// Token: 0x0400013E RID: 318
	public bool m_wakeUpAnimation;

	// Token: 0x0400013F RID: 319
	public Skills.SkillType m_copySkill;

	// Token: 0x04000140 RID: 320
	public float m_copySkillToRandomFactor;

	// Token: 0x04000141 RID: 321
	public bool m_setMaxInstancesFromWeaponLevel;

	// Token: 0x04000142 RID: 322
	public List<SpawnAbility.LevelUpSettings> m_levelUpSettings;

	// Token: 0x04000143 RID: 323
	public SpawnAbility.TargetType m_targetType;

	// Token: 0x04000144 RID: 324
	public Pathfinding.AgentType m_targetWhenPathfindingType = Pathfinding.AgentType.Humanoid;

	// Token: 0x04000145 RID: 325
	public float m_maxTargetRange = 40f;

	// Token: 0x04000146 RID: 326
	public EffectList m_spawnEffects = new EffectList();

	// Token: 0x04000147 RID: 327
	public EffectList m_preSpawnEffects = new EffectList();

	// Token: 0x04000148 RID: 328
	[global::Tooltip("Used for the troll summoning staff, to spawn an AOE that's friendly to the spawned creature.")]
	public GameObject m_aoePrefab;

	// Token: 0x04000149 RID: 329
	[Header("Projectile")]
	public float m_projectileVelocity = 10f;

	// Token: 0x0400014A RID: 330
	public float m_projectileVelocityMax;

	// Token: 0x0400014B RID: 331
	public float m_projectileAccuracy = 10f;

	// Token: 0x0400014C RID: 332
	public bool m_randomDirection;

	// Token: 0x0400014D RID: 333
	public float m_randomAngleMin;

	// Token: 0x0400014E RID: 334
	public float m_randomAngleMax;

	// Token: 0x0400014F RID: 335
	private Character m_owner;

	// Token: 0x04000150 RID: 336
	private ItemDrop.ItemData m_weapon;

	// Token: 0x02000226 RID: 550
	public enum TargetType
	{
		// Token: 0x04001F28 RID: 7976
		ClosestEnemy,
		// Token: 0x04001F29 RID: 7977
		RandomEnemy,
		// Token: 0x04001F2A RID: 7978
		Caster,
		// Token: 0x04001F2B RID: 7979
		Position,
		// Token: 0x04001F2C RID: 7980
		RandomPathfindablePosition
	}

	// Token: 0x02000227 RID: 551
	[Serializable]
	public class LevelUpSettings
	{
		// Token: 0x04001F2D RID: 7981
		public Skills.SkillType m_skill;

		// Token: 0x04001F2E RID: 7982
		public int m_skillLevel;

		// Token: 0x04001F2F RID: 7983
		public int m_setLevel;

		// Token: 0x04001F30 RID: 7984
		public int m_maxSpawns;
	}
}
