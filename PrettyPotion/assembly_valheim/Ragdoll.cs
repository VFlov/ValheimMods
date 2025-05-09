using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200002B RID: 43
public class Ragdoll : MonoBehaviour
{
	// Token: 0x0600045C RID: 1116 RVA: 0x00027D80 File Offset: 0x00025F80
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_bodies = base.GetComponentsInChildren<Rigidbody>();
		base.Invoke("RemoveInitVel", 2f);
		if (this.m_mainModel)
		{
			float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_hue, 0f);
			float float2 = this.m_nview.GetZDO().GetFloat(ZDOVars.s_saturation, 0f);
			float float3 = this.m_nview.GetZDO().GetFloat(ZDOVars.s_value, 0f);
			this.m_mainModel.material.SetFloat("_Hue", @float);
			this.m_mainModel.material.SetFloat("_Saturation", float2);
			this.m_mainModel.material.SetFloat("_Value", float3);
		}
		base.InvokeRepeating("DestroyNow", this.m_ttl, 1f);
	}

	// Token: 0x0600045D RID: 1117 RVA: 0x00027E70 File Offset: 0x00026070
	public Vector3 GetAverageBodyPosition()
	{
		if (this.m_bodies.Length == 0)
		{
			return base.transform.position;
		}
		Vector3 a = Vector3.zero;
		foreach (Rigidbody rigidbody in this.m_bodies)
		{
			a += rigidbody.position;
		}
		return a / (float)this.m_bodies.Length;
	}

	// Token: 0x0600045E RID: 1118 RVA: 0x00027ED0 File Offset: 0x000260D0
	private void DestroyNow()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		Vector3 vector = this.GetAverageBodyPosition();
		Quaternion identity = Quaternion.identity;
		this.m_removeEffect.Create(vector, Quaternion.identity, null, 1f, -1);
		if (this.m_lootSpawnJoint != null)
		{
			vector = this.m_lootSpawnJoint.transform.position;
		}
		this.SpawnLoot(vector);
		ZNetScene.instance.Destroy(base.gameObject);
	}

	// Token: 0x0600045F RID: 1119 RVA: 0x00027F54 File Offset: 0x00026154
	private void RemoveInitVel()
	{
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_initVel, Vector3.zero);
		}
	}

	// Token: 0x06000460 RID: 1120 RVA: 0x00027F80 File Offset: 0x00026180
	private void Start()
	{
		Vector3 vec = this.m_nview.GetZDO().GetVec3(ZDOVars.s_initVel, Vector3.zero);
		if (vec != Vector3.zero)
		{
			vec.y = Mathf.Min(vec.y, 4f);
			Rigidbody[] bodies = this.m_bodies;
			for (int i = 0; i < bodies.Length; i++)
			{
				bodies[i].velocity = vec * UnityEngine.Random.value;
			}
		}
	}

	// Token: 0x06000461 RID: 1121 RVA: 0x00027FF4 File Offset: 0x000261F4
	public void Setup(Vector3 velocity, float hue, float saturation, float value, CharacterDrop characterDrop)
	{
		velocity.x *= this.m_velMultiplier;
		velocity.z *= this.m_velMultiplier;
		this.m_nview.GetZDO().Set(ZDOVars.s_initVel, velocity);
		this.m_nview.GetZDO().Set(ZDOVars.s_hue, hue);
		this.m_nview.GetZDO().Set(ZDOVars.s_saturation, saturation);
		this.m_nview.GetZDO().Set(ZDOVars.s_value, value);
		if (this.m_mainModel)
		{
			this.m_mainModel.material.SetFloat("_Hue", hue);
			this.m_mainModel.material.SetFloat("_Saturation", saturation);
			this.m_mainModel.material.SetFloat("_Value", value);
		}
		if (characterDrop && this.m_dropItems)
		{
			this.SaveLootList(characterDrop);
		}
	}

	// Token: 0x06000462 RID: 1122 RVA: 0x000280E8 File Offset: 0x000262E8
	private void SaveLootList(CharacterDrop characterDrop)
	{
		List<KeyValuePair<GameObject, int>> list = characterDrop.GenerateDropList();
		if (list.Count > 0)
		{
			ZDO zdo = this.m_nview.GetZDO();
			zdo.Set(ZDOVars.s_drops, list.Count, false);
			for (int i = 0; i < list.Count; i++)
			{
				KeyValuePair<GameObject, int> keyValuePair = list[i];
				int prefabHash = ZNetScene.instance.GetPrefabHash(keyValuePair.Key);
				zdo.Set("drop_hash" + i.ToString(), prefabHash);
				zdo.Set("drop_amount" + i.ToString(), keyValuePair.Value);
			}
		}
	}

	// Token: 0x06000463 RID: 1123 RVA: 0x0002818C File Offset: 0x0002638C
	private void SpawnLoot(Vector3 center)
	{
		ZDO zdo = this.m_nview.GetZDO();
		int @int = zdo.GetInt(ZDOVars.s_drops, 0);
		if (@int <= 0)
		{
			return;
		}
		List<KeyValuePair<GameObject, int>> list = new List<KeyValuePair<GameObject, int>>();
		for (int i = 0; i < @int; i++)
		{
			int int2 = zdo.GetInt("drop_hash" + i.ToString(), 0);
			int int3 = zdo.GetInt("drop_amount" + i.ToString(), 0);
			GameObject prefab = ZNetScene.instance.GetPrefab(int2);
			if (prefab == null)
			{
				ZLog.LogWarning("Ragdoll: Missing prefab:" + int2.ToString() + " when dropping loot");
			}
			else
			{
				list.Add(new KeyValuePair<GameObject, int>(prefab, int3));
			}
		}
		CharacterDrop.DropItems(list, center + Vector3.up * 0.75f, 0.5f);
	}

	// Token: 0x06000464 RID: 1124 RVA: 0x00028261 File Offset: 0x00026461
	private void FixedUpdate()
	{
		if (this.m_float)
		{
			this.UpdateFloating(Time.fixedDeltaTime);
		}
	}

	// Token: 0x06000465 RID: 1125 RVA: 0x00028278 File Offset: 0x00026478
	private void UpdateFloating(float dt)
	{
		foreach (Rigidbody rigidbody in this.m_bodies)
		{
			Vector3 worldCenterOfMass = rigidbody.worldCenterOfMass;
			worldCenterOfMass.y += this.m_floatOffset;
			float liquidLevel = Floating.GetLiquidLevel(worldCenterOfMass, 1f, LiquidType.All);
			if (worldCenterOfMass.y < liquidLevel)
			{
				float d = (liquidLevel - worldCenterOfMass.y) / 0.5f;
				Vector3 a = Vector3.up * 20f * d;
				rigidbody.AddForce(a * dt, ForceMode.VelocityChange);
				rigidbody.velocity -= rigidbody.velocity * 0.05f * d;
			}
		}
	}

	// Token: 0x040004F2 RID: 1266
	public float m_velMultiplier = 1f;

	// Token: 0x040004F3 RID: 1267
	public float m_ttl;

	// Token: 0x040004F4 RID: 1268
	public Renderer m_mainModel;

	// Token: 0x040004F5 RID: 1269
	public EffectList m_removeEffect = new EffectList();

	// Token: 0x040004F6 RID: 1270
	public Action<Vector3> m_onDestroyed;

	// Token: 0x040004F7 RID: 1271
	public bool m_float;

	// Token: 0x040004F8 RID: 1272
	public float m_floatOffset = -0.1f;

	// Token: 0x040004F9 RID: 1273
	public bool m_dropItems = true;

	// Token: 0x040004FA RID: 1274
	public GameObject m_lootSpawnJoint;

	// Token: 0x040004FB RID: 1275
	private const float m_floatForce = 20f;

	// Token: 0x040004FC RID: 1276
	private const float m_damping = 0.05f;

	// Token: 0x040004FD RID: 1277
	private ZNetView m_nview;

	// Token: 0x040004FE RID: 1278
	private Rigidbody[] m_bodies;

	// Token: 0x040004FF RID: 1279
	private const float m_dropOffset = 0.75f;

	// Token: 0x04000500 RID: 1280
	private const float m_dropArea = 0.5f;
}
