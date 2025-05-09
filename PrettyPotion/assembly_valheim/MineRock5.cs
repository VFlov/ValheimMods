using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000199 RID: 409
public class MineRock5 : MonoBehaviour, IDestructible, Hoverable
{
	// Token: 0x06001835 RID: 6197 RVA: 0x000B4A70 File Offset: 0x000B2C70
	private void Awake()
	{
		Collider[] componentsInChildren = base.gameObject.GetComponentsInChildren<Collider>();
		this.m_hitAreas = new List<MineRock5.HitArea>(componentsInChildren.Length);
		this.m_extraRenderers = new List<Renderer>();
		foreach (Collider collider in componentsInChildren)
		{
			MineRock5.HitArea hitArea = new MineRock5.HitArea();
			hitArea.m_collider = collider;
			hitArea.m_meshFilter = collider.GetComponent<MeshFilter>();
			hitArea.m_meshRenderer = collider.GetComponent<MeshRenderer>();
			hitArea.m_physics = collider.GetComponent<StaticPhysics>();
			hitArea.m_health = this.m_health + (float)Game.m_worldLevel * this.m_health * Game.instance.m_worldLevelMineHPMultiplier;
			hitArea.m_baseScale = hitArea.m_collider.transform.localScale.x;
			for (int j = 0; j < collider.transform.childCount; j++)
			{
				Renderer[] componentsInChildren2 = collider.transform.GetChild(j).GetComponentsInChildren<Renderer>();
				this.m_extraRenderers.AddRange(componentsInChildren2);
			}
			this.m_hitAreas.Add(hitArea);
		}
		if (MineRock5.m_rayMask == 0)
		{
			MineRock5.m_rayMask = LayerMask.GetMask(new string[]
			{
				"piece",
				"Default",
				"static_solid",
				"Default_small",
				"terrain"
			});
		}
		if (MineRock5.m_groundLayer == 0)
		{
			MineRock5.m_groundLayer = LayerMask.NameToLayer("terrain");
		}
		Material[] array = null;
		foreach (MineRock5.HitArea hitArea2 in this.m_hitAreas)
		{
			if (array == null || hitArea2.m_meshRenderer.sharedMaterials.Length > array.Length)
			{
				array = hitArea2.m_meshRenderer.sharedMaterials;
			}
		}
		this.m_meshFilter = base.gameObject.AddComponent<MeshFilter>();
		this.m_meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
		this.m_meshRenderer.sharedMaterials = array;
		this.m_meshFilter.mesh = new Mesh();
		this.m_meshFilter.name = "___MineRock5 m_meshFilter";
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview && this.m_nview.GetZDO() != null)
		{
			this.m_nview.Register<HitData, int>("RPC_Damage", new Action<long, HitData, int>(this.RPC_Damage));
			this.m_nview.Register<int, float>("RPC_SetAreaHealth", new Action<long, int, float>(this.RPC_SetAreaHealth));
		}
		this.CheckForUpdate();
		base.InvokeRepeating("CheckForUpdate", UnityEngine.Random.Range(5f, 10f), 10f);
	}

	// Token: 0x06001836 RID: 6198 RVA: 0x000B4D08 File Offset: 0x000B2F08
	private void CheckSupport()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.UpdateSupport();
		for (int i = 0; i < this.m_hitAreas.Count; i++)
		{
			MineRock5.HitArea hitArea = this.m_hitAreas[i];
			if (hitArea.m_health > 0f && !hitArea.m_supported)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = this.m_health;
				hitData.m_point = hitArea.m_collider.bounds.center;
				hitData.m_toolTier = 100;
				hitData.m_hitType = HitData.HitType.Structural;
				this.DamageArea(i, hitData);
			}
		}
	}

	// Token: 0x06001837 RID: 6199 RVA: 0x000B4DB7 File Offset: 0x000B2FB7
	private void CheckForUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.GetZDO().DataRevision != this.m_lastDataRevision)
		{
			this.LoadHealth();
			this.UpdateMesh();
		}
	}

	// Token: 0x06001838 RID: 6200 RVA: 0x000B4DEC File Offset: 0x000B2FEC
	private void LoadHealth()
	{
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_health, "");
		if (@string.Length > 0)
		{
			ZPackage zpackage = new ZPackage(Convert.FromBase64String(@string));
			int num = zpackage.ReadInt();
			for (int i = 0; i < num; i++)
			{
				float health = zpackage.ReadSingle();
				MineRock5.HitArea hitArea = this.GetHitArea(i);
				if (hitArea != null)
				{
					hitArea.m_health = health;
				}
			}
		}
		this.m_lastDataRevision = this.m_nview.GetZDO().DataRevision;
	}

	// Token: 0x06001839 RID: 6201 RVA: 0x000B4E70 File Offset: 0x000B3070
	private void SaveHealth()
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(this.m_hitAreas.Count);
		foreach (MineRock5.HitArea hitArea in this.m_hitAreas)
		{
			zpackage.Write(hitArea.m_health);
		}
		string value = Convert.ToBase64String(zpackage.GetArray());
		this.m_nview.GetZDO().Set(ZDOVars.s_health, value);
		this.m_lastDataRevision = this.m_nview.GetZDO().DataRevision;
	}

	// Token: 0x0600183A RID: 6202 RVA: 0x000B4F18 File Offset: 0x000B3118
	private void UpdateMesh()
	{
		MineRock5.m_tempInstancesA.Clear();
		MineRock5.m_tempInstancesB.Clear();
		Material y = this.m_meshRenderer.sharedMaterials[0];
		Matrix4x4 inverse = base.transform.localToWorldMatrix.inverse;
		for (int i = 0; i < this.m_hitAreas.Count; i++)
		{
			MineRock5.HitArea hitArea = this.m_hitAreas[i];
			if (hitArea.m_health > 0f)
			{
				CombineInstance item = default(CombineInstance);
				item.mesh = hitArea.m_meshFilter.sharedMesh;
				item.transform = inverse * hitArea.m_meshFilter.transform.localToWorldMatrix;
				for (int j = 0; j < hitArea.m_meshFilter.sharedMesh.subMeshCount; j++)
				{
					item.subMeshIndex = j;
					if (hitArea.m_meshRenderer.sharedMaterials[j] == y)
					{
						MineRock5.m_tempInstancesA.Add(item);
					}
					else
					{
						MineRock5.m_tempInstancesB.Add(item);
					}
				}
				hitArea.m_meshRenderer.enabled = false;
				hitArea.m_collider.gameObject.SetActive(true);
			}
			else
			{
				hitArea.m_collider.gameObject.SetActive(false);
			}
		}
		if (MineRock5.m_tempMeshA == null)
		{
			MineRock5.m_tempMeshA = new Mesh();
			MineRock5.m_tempMeshB = new Mesh();
			MineRock5.m_tempMeshA.name = "___MineRock5 m_tempMeshA";
			MineRock5.m_tempMeshB.name = "___MineRock5 m_tempMeshB";
		}
		MineRock5.m_tempMeshA.CombineMeshes(MineRock5.m_tempInstancesA.ToArray());
		MineRock5.m_tempMeshB.CombineMeshes(MineRock5.m_tempInstancesB.ToArray());
		CombineInstance combineInstance = default(CombineInstance);
		combineInstance.mesh = MineRock5.m_tempMeshA;
		CombineInstance combineInstance2 = default(CombineInstance);
		combineInstance2.mesh = MineRock5.m_tempMeshB;
		this.m_meshFilter.mesh.CombineMeshes(new CombineInstance[]
		{
			combineInstance,
			combineInstance2
		}, false, false);
		this.m_meshRenderer.enabled = true;
		Renderer[] array = new Renderer[this.m_extraRenderers.Count + 1];
		this.m_extraRenderers.CopyTo(0, array, 0, this.m_extraRenderers.Count);
		array[array.Length - 1] = this.m_meshRenderer;
		LODGroup component = base.gameObject.GetComponent<LODGroup>();
		LOD[] lods = component.GetLODs();
		lods[0].renderers = array;
		component.SetLODs(lods);
	}

	// Token: 0x0600183B RID: 6203 RVA: 0x000B518B File Offset: 0x000B338B
	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name);
	}

	// Token: 0x0600183C RID: 6204 RVA: 0x000B519D File Offset: 0x000B339D
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x0600183D RID: 6205 RVA: 0x000B51A5 File Offset: 0x000B33A5
	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	// Token: 0x0600183E RID: 6206 RVA: 0x000B51A8 File Offset: 0x000B33A8
	public void Damage(HitData hit)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_hitAreas == null)
		{
			return;
		}
		if (hit.m_hitCollider == null || hit.m_radius > 0f)
		{
			int num = 0;
			MineRock5.m_tempColliderSet.Clear();
			int num2 = Physics.OverlapSphereNonAlloc(hit.m_point, (hit.m_radius > 0f) ? hit.m_radius : 0.05f, MineRock5.m_tempColliders, MineRock5.m_rayMask);
			for (int i = 0; i < num2; i++)
			{
				if (MineRock5.m_tempColliders[i].transform.parent == base.transform || MineRock5.m_tempColliders[i].transform.parent.parent == base.transform)
				{
					MineRock5.m_tempColliderSet.Add(MineRock5.m_tempColliders[i]);
				}
			}
			if (MineRock5.m_tempColliderSet.Count > 0)
			{
				foreach (Collider area in MineRock5.m_tempColliderSet)
				{
					int areaIndex = this.GetAreaIndex(area);
					if (areaIndex >= 0)
					{
						num++;
						this.m_nview.InvokeRPC("RPC_Damage", new object[]
						{
							hit,
							areaIndex
						});
						if (this.m_allDestroyed)
						{
							return;
						}
					}
				}
			}
			if (num == 0)
			{
				ZLog.Log("Minerock hit has no collider or invalid hit area on " + base.gameObject.name);
			}
			return;
		}
		int areaIndex2 = this.GetAreaIndex(hit.m_hitCollider);
		if (areaIndex2 < 0)
		{
			ZLog.Log("Invalid hit area on " + base.gameObject.name);
			return;
		}
		this.m_nview.InvokeRPC("RPC_Damage", new object[]
		{
			hit,
			areaIndex2
		});
	}

	// Token: 0x0600183F RID: 6207 RVA: 0x000B5394 File Offset: 0x000B3594
	private void RPC_Damage(long sender, HitData hit, int hitAreaIndex)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		bool flag = this.DamageArea(hitAreaIndex, hit);
		if (flag && this.m_supportCheck)
		{
			this.CheckSupport();
		}
		if (this.m_triggerPrivateArea)
		{
			Character attacker = hit.GetAttacker();
			if (attacker != null)
			{
				PrivateArea.OnObjectDamaged(base.transform.position, attacker, flag);
			}
		}
	}

	// Token: 0x06001840 RID: 6208 RVA: 0x000B540C File Offset: 0x000B360C
	private bool DamageArea(int hitAreaIndex, HitData hit)
	{
		ZLog.Log("hit mine rock " + hitAreaIndex.ToString());
		MineRock5.HitArea hitArea = this.GetHitArea(hitAreaIndex);
		if (hitArea == null)
		{
			ZLog.Log("Missing hit area " + hitAreaIndex.ToString());
			return false;
		}
		this.LoadHealth();
		if (hitArea.m_health <= 0f)
		{
			ZLog.Log("Already destroyed");
			return false;
		}
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damageModifiers, out type);
		float totalDamage = hit.GetTotalDamage();
		Vector3 vector = (this.m_hitEffectAreaCenter && hitArea.m_collider != null) ? hitArea.m_collider.bounds.center : hit.m_point;
		if (!hit.CheckToolTier(this.m_minToolTier, false))
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, vector, 0f, false);
			return false;
		}
		DamageText.instance.ShowText(type, vector, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return false;
		}
		hitArea.m_health -= totalDamage;
		this.SaveHealth();
		this.m_hitEffect.Create(vector, Quaternion.identity, null, 1f, -1);
		if (hit.m_hitType != HitData.HitType.CinderFire)
		{
			Player closestPlayer = Player.GetClosestPlayer(vector, 10f);
			if (closestPlayer != null)
			{
				closestPlayer.AddNoise(100f);
			}
		}
		if (hit.GetAttacker() == Player.m_localPlayer)
		{
			Game.instance.IncrementPlayerStat(PlayerStatType.MineHits, 1f);
		}
		if (hitArea.m_health <= 0f)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetAreaHealth", new object[]
			{
				hitAreaIndex,
				hitArea.m_health
			});
			this.m_destroyedEffect.Create(vector, Quaternion.identity, null, 1f, -1);
			foreach (GameObject gameObject in this.m_dropItems.GetDropList())
			{
				Vector3 position = vector + UnityEngine.Random.insideUnitSphere * 0.3f;
				UnityEngine.Object.Instantiate<GameObject>(gameObject, position, Quaternion.identity);
				ItemDrop.OnCreateNew(gameObject);
			}
			if (this.AllDestroyed())
			{
				this.m_nview.Destroy();
				this.m_allDestroyed = true;
			}
			if (hit.GetAttacker() == Player.m_localPlayer)
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.Mines, 1f);
				switch (this.m_minToolTier)
				{
				case 0:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier0, 1f);
					break;
				case 1:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier1, 1f);
					break;
				case 2:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier2, 1f);
					break;
				case 3:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier3, 1f);
					break;
				case 4:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier4, 1f);
					break;
				case 5:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier5, 1f);
					break;
				default:
					ZLog.LogWarning("No stat for mine tier: " + this.m_minToolTier.ToString());
					break;
				}
			}
			return true;
		}
		return false;
	}

	// Token: 0x06001841 RID: 6209 RVA: 0x000B572C File Offset: 0x000B392C
	private bool AllDestroyed()
	{
		for (int i = 0; i < this.m_hitAreas.Count; i++)
		{
			if (this.m_hitAreas[i].m_health > 0f)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06001842 RID: 6210 RVA: 0x000B576C File Offset: 0x000B396C
	private bool NonDestroyed()
	{
		for (int i = 0; i < this.m_hitAreas.Count; i++)
		{
			if (this.m_hitAreas[i].m_health <= 0f)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06001843 RID: 6211 RVA: 0x000B57AC File Offset: 0x000B39AC
	private void RPC_SetAreaHealth(long sender, int index, float health)
	{
		MineRock5.HitArea hitArea = this.GetHitArea(index);
		if (hitArea != null)
		{
			hitArea.m_health = health;
		}
		this.UpdateMesh();
	}

	// Token: 0x06001844 RID: 6212 RVA: 0x000B57D4 File Offset: 0x000B39D4
	private int GetAreaIndex(Collider area)
	{
		for (int i = 0; i < this.m_hitAreas.Count; i++)
		{
			if (this.m_hitAreas[i].m_collider == area)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x06001845 RID: 6213 RVA: 0x000B5813 File Offset: 0x000B3A13
	private MineRock5.HitArea GetHitArea(int index)
	{
		if (index < 0 || index >= this.m_hitAreas.Count)
		{
			return null;
		}
		return this.m_hitAreas[index];
	}

	// Token: 0x06001846 RID: 6214 RVA: 0x000B5838 File Offset: 0x000B3A38
	private void UpdateSupport()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (!this.m_haveSetupBounds)
		{
			this.SetupColliders();
			this.m_haveSetupBounds = true;
		}
		foreach (MineRock5.HitArea hitArea in this.m_hitAreas)
		{
			hitArea.m_supported = false;
		}
		Vector3 position = base.transform.position;
		for (int i = 0; i < 3; i++)
		{
			foreach (MineRock5.HitArea hitArea2 in this.m_hitAreas)
			{
				if (!hitArea2.m_supported)
				{
					int num = Physics.OverlapBoxNonAlloc(position + hitArea2.m_bound.m_pos, hitArea2.m_bound.m_size, MineRock5.m_tempColliders, hitArea2.m_bound.m_rot, MineRock5.m_rayMask);
					for (int j = 0; j < num; j++)
					{
						Collider collider = MineRock5.m_tempColliders[j];
						if (!(collider == hitArea2.m_collider) && !(collider.attachedRigidbody != null) && !collider.isTrigger)
						{
							hitArea2.m_supported = (hitArea2.m_supported || this.GetSupport(collider));
							if (hitArea2.m_supported)
							{
								break;
							}
						}
					}
				}
			}
		}
		ZLog.Log("Suport time " + ((Time.realtimeSinceStartup - realtimeSinceStartup) * 1000f).ToString());
	}

	// Token: 0x06001847 RID: 6215 RVA: 0x000B59D8 File Offset: 0x000B3BD8
	private bool GetSupport(Collider c)
	{
		if (c.gameObject.layer == MineRock5.m_groundLayer)
		{
			return true;
		}
		IDestructible componentInParent = c.gameObject.GetComponentInParent<IDestructible>();
		if (componentInParent != null)
		{
			if (componentInParent == this)
			{
				foreach (MineRock5.HitArea hitArea in this.m_hitAreas)
				{
					if (hitArea.m_collider == c)
					{
						return hitArea.m_supported;
					}
				}
			}
			return c.transform.position.y < base.transform.position.y;
		}
		return true;
	}

	// Token: 0x06001848 RID: 6216 RVA: 0x000B5A88 File Offset: 0x000B3C88
	private void SetupColliders()
	{
		Vector3 position = base.transform.position;
		foreach (MineRock5.HitArea hitArea in this.m_hitAreas)
		{
			hitArea.m_bound.m_rot = Quaternion.identity;
			hitArea.m_bound.m_pos = hitArea.m_collider.bounds.center - position;
			hitArea.m_bound.m_size = hitArea.m_collider.bounds.size * 0.5f;
		}
	}

	// Token: 0x0400182B RID: 6187
	private static Mesh m_tempMeshA;

	// Token: 0x0400182C RID: 6188
	private static Mesh m_tempMeshB;

	// Token: 0x0400182D RID: 6189
	private static List<CombineInstance> m_tempInstancesA = new List<CombineInstance>();

	// Token: 0x0400182E RID: 6190
	private static List<CombineInstance> m_tempInstancesB = new List<CombineInstance>();

	// Token: 0x0400182F RID: 6191
	public string m_name = "";

	// Token: 0x04001830 RID: 6192
	public float m_health = 2f;

	// Token: 0x04001831 RID: 6193
	public HitData.DamageModifiers m_damageModifiers;

	// Token: 0x04001832 RID: 6194
	public int m_minToolTier;

	// Token: 0x04001833 RID: 6195
	public bool m_supportCheck = true;

	// Token: 0x04001834 RID: 6196
	public bool m_triggerPrivateArea;

	// Token: 0x04001835 RID: 6197
	public EffectList m_destroyedEffect = new EffectList();

	// Token: 0x04001836 RID: 6198
	public EffectList m_hitEffect = new EffectList();

	// Token: 0x04001837 RID: 6199
	public DropTable m_dropItems;

	// Token: 0x04001838 RID: 6200
	public bool m_hitEffectAreaCenter = true;

	// Token: 0x04001839 RID: 6201
	private List<MineRock5.HitArea> m_hitAreas;

	// Token: 0x0400183A RID: 6202
	private List<Renderer> m_extraRenderers;

	// Token: 0x0400183B RID: 6203
	private bool m_haveSetupBounds;

	// Token: 0x0400183C RID: 6204
	private ZNetView m_nview;

	// Token: 0x0400183D RID: 6205
	private MeshFilter m_meshFilter;

	// Token: 0x0400183E RID: 6206
	private MeshRenderer m_meshRenderer;

	// Token: 0x0400183F RID: 6207
	private uint m_lastDataRevision = uint.MaxValue;

	// Token: 0x04001840 RID: 6208
	private const int m_supportIterations = 3;

	// Token: 0x04001841 RID: 6209
	private bool m_allDestroyed;

	// Token: 0x04001842 RID: 6210
	private static int m_rayMask = 0;

	// Token: 0x04001843 RID: 6211
	private static int m_groundLayer = 0;

	// Token: 0x04001844 RID: 6212
	private static Collider[] m_tempColliders = new Collider[128];

	// Token: 0x04001845 RID: 6213
	private static HashSet<Collider> m_tempColliderSet = new HashSet<Collider>();

	// Token: 0x02000374 RID: 884
	private struct BoundData
	{
		// Token: 0x04002629 RID: 9769
		public Vector3 m_pos;

		// Token: 0x0400262A RID: 9770
		public Quaternion m_rot;

		// Token: 0x0400262B RID: 9771
		public Vector3 m_size;
	}

	// Token: 0x02000375 RID: 885
	private class HitArea
	{
		// Token: 0x0400262C RID: 9772
		public Collider m_collider;

		// Token: 0x0400262D RID: 9773
		public MeshRenderer m_meshRenderer;

		// Token: 0x0400262E RID: 9774
		public MeshFilter m_meshFilter;

		// Token: 0x0400262F RID: 9775
		public StaticPhysics m_physics;

		// Token: 0x04002630 RID: 9776
		public float m_health;

		// Token: 0x04002631 RID: 9777
		public MineRock5.BoundData m_bound;

		// Token: 0x04002632 RID: 9778
		public bool m_supported;

		// Token: 0x04002633 RID: 9779
		public float m_baseScale;
	}
}
