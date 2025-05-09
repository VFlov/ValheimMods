using System;
using UnityEngine;

// Token: 0x02000069 RID: 105
public class Thunder : MonoBehaviour
{
	// Token: 0x060006C7 RID: 1735 RVA: 0x000387C9 File Offset: 0x000369C9
	private void Start()
	{
		this.m_strikeTimer = UnityEngine.Random.Range(this.m_strikeIntervalMin, this.m_strikeIntervalMax);
	}

	// Token: 0x060006C8 RID: 1736 RVA: 0x000387E4 File Offset: 0x000369E4
	private void Update()
	{
		if (this.m_strikeTimer > 0f)
		{
			this.m_strikeTimer -= Time.deltaTime;
			if (this.m_strikeTimer <= 0f)
			{
				this.DoFlash();
			}
		}
		if (this.m_thunderTimer > 0f)
		{
			this.m_thunderTimer -= Time.deltaTime;
			if (this.m_thunderTimer <= 0f)
			{
				this.DoThunder();
				this.m_strikeTimer = UnityEngine.Random.Range(this.m_strikeIntervalMin, this.m_strikeIntervalMax);
			}
		}
		if (this.m_spawnThor)
		{
			this.m_thorTimer += Time.deltaTime;
			if (this.m_thorTimer > this.m_thorInterval)
			{
				this.m_thorTimer = 0f;
				if (UnityEngine.Random.value <= this.m_thorChance && (this.m_requiredGlobalKey == "" || ZoneSystem.instance.GetGlobalKey(this.m_requiredGlobalKey)))
				{
					this.SpawnThor();
				}
			}
		}
	}

	// Token: 0x060006C9 RID: 1737 RVA: 0x000388D8 File Offset: 0x00036AD8
	private void SpawnThor()
	{
		float num = UnityEngine.Random.value * 6.2831855f;
		Vector3 vector = base.transform.position + new Vector3(Mathf.Sin(num), 0f, Mathf.Cos(num)) * this.m_thorSpawnDistance;
		vector.y += UnityEngine.Random.Range(this.m_thorSpawnAltitudeMin, this.m_thorSpawnAltitudeMax);
		float groundHeight = ZoneSystem.instance.GetGroundHeight(vector);
		if (vector.y < groundHeight)
		{
			vector.y = groundHeight + 50f;
		}
		float f = num + 180f + (float)UnityEngine.Random.Range(-45, 45);
		Vector3 vector2 = base.transform.position + new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * this.m_thorSpawnDistance;
		vector2.y += UnityEngine.Random.Range(this.m_thorSpawnAltitudeMin, this.m_thorSpawnAltitudeMax);
		float groundHeight2 = ZoneSystem.instance.GetGroundHeight(vector2);
		if (vector.y < groundHeight2)
		{
			vector.y = groundHeight2 + 50f;
		}
		Vector3 normalized = (vector2 - vector).normalized;
		UnityEngine.Object.Instantiate<GameObject>(this.m_thorPrefab, vector, Quaternion.LookRotation(normalized));
	}

	// Token: 0x060006CA RID: 1738 RVA: 0x00038A14 File Offset: 0x00036C14
	private void DoFlash()
	{
		float f = UnityEngine.Random.value * 6.2831855f;
		float d = UnityEngine.Random.Range(this.m_flashDistanceMin, this.m_flashDistanceMax);
		this.m_flashPos = base.transform.position + new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * d;
		this.m_flashPos.y = this.m_flashPos.y + this.m_flashAltitude;
		Quaternion rotation = Quaternion.LookRotation((base.transform.position - this.m_flashPos).normalized);
		GameObject[] array = this.m_flashEffect.Create(this.m_flashPos, Quaternion.identity, null, 1f, -1);
		for (int i = 0; i < array.Length; i++)
		{
			Light[] componentsInChildren = array[i].GetComponentsInChildren<Light>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].transform.rotation = rotation;
			}
		}
		this.m_thunderTimer = UnityEngine.Random.Range(this.m_thunderDelayMin, this.m_thunderDelayMax);
	}

	// Token: 0x060006CB RID: 1739 RVA: 0x00038B22 File Offset: 0x00036D22
	private void DoThunder()
	{
		this.m_thunderEffect.Create(this.m_flashPos, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x040007DE RID: 2014
	public float m_strikeIntervalMin = 3f;

	// Token: 0x040007DF RID: 2015
	public float m_strikeIntervalMax = 10f;

	// Token: 0x040007E0 RID: 2016
	public float m_thunderDelayMin = 3f;

	// Token: 0x040007E1 RID: 2017
	public float m_thunderDelayMax = 5f;

	// Token: 0x040007E2 RID: 2018
	public float m_flashDistanceMin = 50f;

	// Token: 0x040007E3 RID: 2019
	public float m_flashDistanceMax = 200f;

	// Token: 0x040007E4 RID: 2020
	public float m_flashAltitude = 100f;

	// Token: 0x040007E5 RID: 2021
	public EffectList m_flashEffect = new EffectList();

	// Token: 0x040007E6 RID: 2022
	public EffectList m_thunderEffect = new EffectList();

	// Token: 0x040007E7 RID: 2023
	[Header("Thor")]
	public bool m_spawnThor;

	// Token: 0x040007E8 RID: 2024
	public string m_requiredGlobalKey = "";

	// Token: 0x040007E9 RID: 2025
	public GameObject m_thorPrefab;

	// Token: 0x040007EA RID: 2026
	public float m_thorSpawnDistance = 300f;

	// Token: 0x040007EB RID: 2027
	public float m_thorSpawnAltitudeMax = 100f;

	// Token: 0x040007EC RID: 2028
	public float m_thorSpawnAltitudeMin = 100f;

	// Token: 0x040007ED RID: 2029
	public float m_thorInterval = 10f;

	// Token: 0x040007EE RID: 2030
	public float m_thorChance = 1f;

	// Token: 0x040007EF RID: 2031
	private Vector3 m_flashPos = Vector3.zero;

	// Token: 0x040007F0 RID: 2032
	private float m_strikeTimer = -1f;

	// Token: 0x040007F1 RID: 2033
	private float m_thunderTimer = -1f;

	// Token: 0x040007F2 RID: 2034
	private float m_thorTimer;
}
