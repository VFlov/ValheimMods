using System;
using UnityEngine;

// Token: 0x0200002A RID: 42
public class Procreation : MonoBehaviour
{
	// Token: 0x06000453 RID: 1107 RVA: 0x000277B4 File Offset: 0x000259B4
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_baseAI = base.GetComponent<BaseAI>();
		this.m_character = base.GetComponent<Character>();
		this.m_tameable = base.GetComponent<Tameable>();
		base.InvokeRepeating("Procreate", UnityEngine.Random.Range(this.m_updateInterval, this.m_updateInterval + this.m_updateInterval * 0.5f), this.m_updateInterval);
	}

	// Token: 0x06000454 RID: 1108 RVA: 0x00027820 File Offset: 0x00025A20
	private void Procreate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_tameable.IsTamed())
		{
			return;
		}
		if (this.m_offspringPrefab == null)
		{
			string prefabName = Utils.GetPrefabName(this.m_offspring);
			this.m_offspringPrefab = ZNetScene.instance.GetPrefab(prefabName);
			int prefab = this.m_nview.GetZDO().GetPrefab();
			this.m_myPrefab = ZNetScene.instance.GetPrefab(prefab);
		}
		if (this.IsPregnant())
		{
			if (this.IsDue())
			{
				this.ResetPregnancy();
				GameObject original = this.m_offspringPrefab;
				if (this.m_noPartnerOffspring)
				{
					int nrOfInstances = SpawnSystem.GetNrOfInstances(this.m_seperatePartner ? this.m_seperatePartner : this.m_myPrefab, base.transform.position, this.m_partnerCheckRange, false, true);
					if ((!this.m_seperatePartner && nrOfInstances < 2) || (this.m_seperatePartner && nrOfInstances < 1))
					{
						original = this.m_noPartnerOffspring;
					}
				}
				Vector3 forward = base.transform.forward;
				if (this.m_spawnRandomDirection)
				{
					float f = UnityEngine.Random.Range(0f, 6.2831855f);
					forward = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
				}
				float d = (this.m_spawnOffsetMax > 0f) ? UnityEngine.Random.Range(this.m_spawnOffset, this.m_spawnOffsetMax) : this.m_spawnOffset;
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, base.transform.position - forward * d, Quaternion.LookRotation(-base.transform.forward, Vector3.up));
				Character component = gameObject.GetComponent<Character>();
				if (component)
				{
					component.SetTamed(this.m_tameable.IsTamed());
					component.SetLevel(Mathf.Max(this.m_minOffspringLevel, this.m_character ? this.m_character.GetLevel() : this.m_minOffspringLevel));
				}
				else
				{
					ItemDrop component2 = gameObject.GetComponent<ItemDrop>();
					if (component2 != null)
					{
						component2.SetQuality(Mathf.Max(this.m_minOffspringLevel, this.m_character ? this.m_character.GetLevel() : this.m_minOffspringLevel));
					}
				}
				this.m_birthEffects.Create(gameObject.transform.position, Quaternion.identity, null, 1f, -1);
				return;
			}
		}
		else
		{
			if (UnityEngine.Random.value <= this.m_pregnancyChance)
			{
				return;
			}
			if (this.m_baseAI && this.m_baseAI.IsAlerted())
			{
				return;
			}
			if (this.m_tameable.IsHungry())
			{
				return;
			}
			int nrOfInstances2 = SpawnSystem.GetNrOfInstances(this.m_myPrefab, base.transform.position, this.m_totalCheckRange, false, false);
			int nrOfInstances3 = SpawnSystem.GetNrOfInstances(this.m_offspringPrefab, base.transform.position, this.m_totalCheckRange, false, false);
			if (nrOfInstances2 + nrOfInstances3 >= this.m_maxCreatures)
			{
				return;
			}
			int nrOfInstances4 = SpawnSystem.GetNrOfInstances(this.m_seperatePartner ? this.m_seperatePartner : this.m_myPrefab, base.transform.position, this.m_partnerCheckRange, false, true);
			if (!this.m_noPartnerOffspring && ((!this.m_seperatePartner && nrOfInstances4 < 2) || (this.m_seperatePartner && nrOfInstances4 < 1)))
			{
				return;
			}
			if (nrOfInstances4 > 0)
			{
				this.m_loveEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			}
			int num = this.GetLovePoints();
			num++;
			this.m_nview.GetZDO().Set(ZDOVars.s_lovePoints, num, false);
			if (num >= this.m_requiredLovePoints)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_lovePoints, 0, false);
				this.MakePregnant();
			}
		}
	}

	// Token: 0x06000455 RID: 1109 RVA: 0x00027BF3 File Offset: 0x00025DF3
	public int GetLovePoints()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_lovePoints, 0);
	}

	// Token: 0x06000456 RID: 1110 RVA: 0x00027C0B File Offset: 0x00025E0B
	public bool ReadyForProcreation()
	{
		return this.m_tameable.IsTamed() && !this.IsPregnant() && !this.m_tameable.IsHungry();
	}

	// Token: 0x06000457 RID: 1111 RVA: 0x00027C34 File Offset: 0x00025E34
	private void MakePregnant()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_pregnant, ZNet.instance.GetTime().Ticks);
	}

	// Token: 0x06000458 RID: 1112 RVA: 0x00027C68 File Offset: 0x00025E68
	private void ResetPregnancy()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_pregnant, 0L);
	}

	// Token: 0x06000459 RID: 1113 RVA: 0x00027C84 File Offset: 0x00025E84
	private bool IsDue()
	{
		long @long = this.m_nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L);
		if (@long == 0L)
		{
			return false;
		}
		DateTime d = new DateTime(@long);
		return (ZNet.instance.GetTime() - d).TotalSeconds > (double)this.m_pregnancyDuration;
	}

	// Token: 0x0600045A RID: 1114 RVA: 0x00027CD7 File Offset: 0x00025ED7
	private bool IsPregnant()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L) != 0L;
	}

	// Token: 0x040004DC RID: 1244
	public float m_updateInterval = 10f;

	// Token: 0x040004DD RID: 1245
	public float m_totalCheckRange = 10f;

	// Token: 0x040004DE RID: 1246
	public int m_maxCreatures = 4;

	// Token: 0x040004DF RID: 1247
	public float m_partnerCheckRange = 3f;

	// Token: 0x040004E0 RID: 1248
	public float m_pregnancyChance = 0.5f;

	// Token: 0x040004E1 RID: 1249
	public float m_pregnancyDuration = 10f;

	// Token: 0x040004E2 RID: 1250
	public int m_requiredLovePoints = 4;

	// Token: 0x040004E3 RID: 1251
	public GameObject m_offspring;

	// Token: 0x040004E4 RID: 1252
	public int m_minOffspringLevel;

	// Token: 0x040004E5 RID: 1253
	public float m_spawnOffset = 2f;

	// Token: 0x040004E6 RID: 1254
	public float m_spawnOffsetMax;

	// Token: 0x040004E7 RID: 1255
	public bool m_spawnRandomDirection;

	// Token: 0x040004E8 RID: 1256
	public GameObject m_seperatePartner;

	// Token: 0x040004E9 RID: 1257
	public GameObject m_noPartnerOffspring;

	// Token: 0x040004EA RID: 1258
	public EffectList m_birthEffects = new EffectList();

	// Token: 0x040004EB RID: 1259
	public EffectList m_loveEffects = new EffectList();

	// Token: 0x040004EC RID: 1260
	private GameObject m_myPrefab;

	// Token: 0x040004ED RID: 1261
	private GameObject m_offspringPrefab;

	// Token: 0x040004EE RID: 1262
	private ZNetView m_nview;

	// Token: 0x040004EF RID: 1263
	private BaseAI m_baseAI;

	// Token: 0x040004F0 RID: 1264
	private Character m_character;

	// Token: 0x040004F1 RID: 1265
	private Tameable m_tameable;
}
