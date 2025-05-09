using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Token: 0x020001DD RID: 477
public class WearNTear : MonoBehaviour, IDestructible
{
	// Token: 0x06001B59 RID: 7001 RVA: 0x000CC654 File Offset: 0x000CA854
	private void Awake()
	{
		int? num = WearNTear.s_terrainLayer;
		int num2 = 0;
		if (num.GetValueOrDefault() < num2 & num != null)
		{
			WearNTear.s_terrainLayer = new int?(LayerMask.NameToLayer("terrain"));
		}
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<HitData>("RPC_Damage", new Action<long, HitData>(this.RPC_Damage));
		this.m_nview.Register<bool>("RPC_Remove", new Action<long, bool>(this.RPC_Remove));
		this.m_nview.Register("RPC_Repair", new Action<long>(this.RPC_Repair));
		this.m_nview.Register<float>("RPC_HealthChanged", new Action<long, float>(this.RPC_HealthChanged));
		this.m_nview.Register("RPC_ClearCachedSupport", new Action<long>(this.RPC_ClearCachedSupport));
		if (this.m_autoCreateFragments)
		{
			this.m_nview.Register("RPC_CreateFragments", new Action<long>(this.RPC_CreateFragments));
		}
		if (WearNTear.s_rayMask == 0)
		{
			WearNTear.s_rayMask = LayerMask.GetMask(new string[]
			{
				"piece",
				"Default",
				"static_solid",
				"Default_small",
				"terrain"
			});
		}
		WearNTear.s_allInstances.Add(this);
		this.m_myIndex = WearNTear.s_allInstances.Count - 1;
		this.m_createTime = Time.time;
		this.m_support = this.GetMaxSupport();
		float num3 = this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health);
		if (num3.Equals(this.m_health) && WearNTear.m_randomInitialDamage)
		{
			num3 = UnityEngine.Random.Range(0.1f * this.m_health, this.m_health * 0.6f);
			this.m_nview.GetZDO().Set(ZDOVars.s_health, num3);
		}
		if (Game.m_worldLevel > 0)
		{
			this.m_health += (float)Game.m_worldLevel * Game.instance.m_worldLevelPieceHPMultiplier * this.m_health;
		}
		this.m_updateCoverTimer = UnityEngine.Random.Range(0f, 4f);
		this.m_healthPercentage = Mathf.Clamp01(num3 / this.m_health);
		this.m_propertyBlock = new MaterialPropertyBlock();
		this.m_renderers = this.GetHighlightRenderers();
		this.SetAshlandsMaterialValue(0f);
		this.UpdateVisual(false);
	}

	// Token: 0x06001B5A RID: 7002 RVA: 0x000CC8BE File Offset: 0x000CAABE
	private void Start()
	{
		this.m_connectedHeightMap = Heightmap.FindHeightmap(base.transform.position);
		if (this.m_connectedHeightMap != null)
		{
			this.m_connectedHeightMap.m_clearConnectedWearNTearCache += this.ClearCachedSupport;
		}
	}

	// Token: 0x06001B5B RID: 7003 RVA: 0x000CC8FC File Offset: 0x000CAAFC
	public void UpdateAshlandsMaterialValues(float time)
	{
		this.SetAshlandsMaterialValue(Mathf.Max(this.m_lavaTimer, Mathf.Max(this.m_ashDamageTime, this.m_burnDamageTime)));
		float num = time - this.m_lastMaterialValueTimeCheck;
		this.m_lastMaterialValueTimeCheck = time;
		if (this.m_ashDamageTime > 0f)
		{
			this.m_ashDamageTime -= num;
		}
		if (this.m_burnDamageTime > 0f)
		{
			this.m_burnDamageTime -= num;
		}
	}

	// Token: 0x06001B5C RID: 7004 RVA: 0x000CC974 File Offset: 0x000CAB74
	private void OnDestroy()
	{
		if (this.m_myIndex != -1)
		{
			WearNTear.s_allInstances[this.m_myIndex] = WearNTear.s_allInstances[WearNTear.s_allInstances.Count - 1];
			WearNTear.s_allInstances[this.m_myIndex].m_myIndex = this.m_myIndex;
			WearNTear.s_allInstances.RemoveAt(WearNTear.s_allInstances.Count - 1);
		}
		if (this.m_connectedHeightMap != null)
		{
			this.m_connectedHeightMap.m_clearConnectedWearNTearCache -= this.ClearCachedSupport;
		}
	}

	// Token: 0x06001B5D RID: 7005 RVA: 0x000CCA06 File Offset: 0x000CAC06
	private void SetAshlandsMaterialValue(float v)
	{
		if (this.m_ashMaterialValue == v)
		{
			return;
		}
		this.m_ashMaterialValue = v;
		MaterialMan.instance.SetValue<float>(base.gameObject, WearNTear.s_AshlandsDamageShaderID, v);
	}

	// Token: 0x06001B5E RID: 7006 RVA: 0x000CCA30 File Offset: 0x000CAC30
	public bool Repair()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health) >= this.m_health)
		{
			return false;
		}
		if (Time.time - this.m_lastRepair < 1f)
		{
			return false;
		}
		this.m_lastRepair = Time.time;
		this.m_nview.InvokeRPC("RPC_Repair", Array.Empty<object>());
		return true;
	}

	// Token: 0x06001B5F RID: 7007 RVA: 0x000CCAA8 File Offset: 0x000CACA8
	private void RPC_Repair(long sender)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_health, this.m_health);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_HealthChanged", new object[]
		{
			this.m_health
		});
	}

	// Token: 0x06001B60 RID: 7008 RVA: 0x000CCB14 File Offset: 0x000CAD14
	private float GetSupport()
	{
		if (!this.m_nview.IsValid())
		{
			return this.GetMaxSupport();
		}
		if (!this.m_nview.HasOwner())
		{
			return this.GetMaxSupport();
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_support;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_support, this.GetMaxSupport());
	}

	// Token: 0x06001B61 RID: 7009 RVA: 0x000CCB78 File Offset: 0x000CAD78
	private float GetSupportColorValue()
	{
		float num = this.GetSupport();
		float num2;
		float num3;
		float num4;
		float num5;
		this.GetMaterialProperties(out num2, out num3, out num4, out num5);
		if (num >= num2)
		{
			return -1f;
		}
		num -= num3;
		return Mathf.Clamp01(num / (num2 * 0.5f - num3));
	}

	// Token: 0x06001B62 RID: 7010 RVA: 0x000CCBB8 File Offset: 0x000CADB8
	public void OnPlaced()
	{
		this.m_createTime = -1f;
		this.m_clearCachedSupport = true;
	}

	// Token: 0x06001B63 RID: 7011 RVA: 0x000CCBCC File Offset: 0x000CADCC
	private List<Renderer> GetHighlightRenderers()
	{
		MeshRenderer[] componentsInChildren = base.GetComponentsInChildren<MeshRenderer>(true);
		SkinnedMeshRenderer[] componentsInChildren2 = base.GetComponentsInChildren<SkinnedMeshRenderer>(true);
		List<Renderer> list = new List<Renderer>();
		list.AddRange(componentsInChildren);
		list.AddRange(componentsInChildren2);
		return list;
	}

	// Token: 0x06001B64 RID: 7012 RVA: 0x000CCBFC File Offset: 0x000CADFC
	public void Highlight()
	{
		float supportColorValue = this.GetSupportColorValue();
		Color color = new Color(0.6f, 0.8f, 1f);
		if (supportColorValue >= 0f)
		{
			color = Color.Lerp(new Color(1f, 0f, 0f), new Color(0f, 1f, 0f), supportColorValue);
			float h;
			float s;
			float v;
			Color.RGBToHSV(color, out h, out s, out v);
			s = Mathf.Lerp(1f, 0.5f, supportColorValue);
			v = Mathf.Lerp(1.2f, 0.9f, supportColorValue);
			color = Color.HSVToRGB(h, s, v);
		}
		MaterialMan.instance.SetValue<Color>(base.gameObject, ShaderProps._EmissionColor, color * 0.4f);
		MaterialMan.instance.SetValue<Color>(base.gameObject, ShaderProps._Color, color);
		base.CancelInvoke("ResetHighlight");
		base.Invoke("ResetHighlight", 0.2f);
	}

	// Token: 0x06001B65 RID: 7013 RVA: 0x000CCCE7 File Offset: 0x000CAEE7
	private void ResetHighlight()
	{
		MaterialMan.instance.ResetValue(base.gameObject, ShaderProps._Color);
		MaterialMan.instance.ResetValue(base.gameObject, ShaderProps._EmissionColor);
	}

	// Token: 0x06001B66 RID: 7014 RVA: 0x000CCD14 File Offset: 0x000CAF14
	private void SetupColliders()
	{
		this.m_colliders = base.GetComponentsInChildren<Collider>(true);
		this.m_bounds = new List<WearNTear.BoundData>();
		foreach (Collider collider in this.m_colliders)
		{
			if (!collider.isTrigger && !(collider.attachedRigidbody != null))
			{
				WearNTear.BoundData item = default(WearNTear.BoundData);
				if (collider is BoxCollider)
				{
					BoxCollider boxCollider = collider as BoxCollider;
					item.m_rot = boxCollider.transform.rotation;
					item.m_pos = boxCollider.transform.position + boxCollider.transform.TransformVector(boxCollider.center);
					item.m_size = new Vector3(boxCollider.transform.lossyScale.x * boxCollider.size.x, boxCollider.transform.lossyScale.y * boxCollider.size.y, boxCollider.transform.lossyScale.z * boxCollider.size.z);
				}
				else
				{
					item.m_rot = Quaternion.identity;
					item.m_pos = collider.bounds.center;
					item.m_size = collider.bounds.size;
				}
				item.m_size.x = item.m_size.x + 0.3f;
				item.m_size.y = item.m_size.y + 0.3f;
				item.m_size.z = item.m_size.z + 0.3f;
				item.m_size *= 0.5f;
				this.m_bounds.Add(item);
			}
		}
	}

	// Token: 0x06001B67 RID: 7015 RVA: 0x000CCED4 File Offset: 0x000CB0D4
	private bool ShouldUpdate(float time)
	{
		return this.m_createTime < 0f || time - this.m_createTime > 30f;
	}

	// Token: 0x06001B68 RID: 7016 RVA: 0x000CCEF4 File Offset: 0x000CB0F4
	public void UpdateWear(float time)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.ShouldUpdate(time))
		{
			if (ZNetScene.instance.OutsideActiveArea(base.transform.position))
			{
				this.m_support = this.GetMaxSupport();
				this.m_nview.GetZDO().Set(ZDOVars.s_support, this.m_support);
				return;
			}
			bool flag = ShieldGenerator.IsInsideShieldCached(base.transform.position, ref this.m_shieldChangeID);
			float num = 0f;
			this.m_rainWet = (!flag && !this.m_haveRoof && EnvMan.IsWet());
			if (this.m_wet)
			{
				this.m_wet.SetActive(this.m_rainWet);
			}
			if (this.m_noRoofWear && !flag && this.GetHealthPercentage() > 0.5f)
			{
				if (this.IsWet())
				{
					if (this.m_rainTimer == 0f)
					{
						this.m_rainTimer = time;
					}
					else if (time - this.m_rainTimer > 60f)
					{
						this.m_rainTimer = time;
						num += 5f;
					}
				}
				else
				{
					this.m_rainTimer = 0f;
				}
			}
			if (this.m_noSupportWear)
			{
				this.UpdateSupport();
				if (!this.HaveSupport())
				{
					num = 100f;
				}
			}
			bool flag2 = false;
			if (this.m_biome == Heightmap.Biome.None || !this.m_heightmap)
			{
				Vector3 position = base.transform.position;
				Vector3 vector;
				Heightmap.Biome biome;
				Heightmap.BiomeArea biomeArea;
				ZoneSystem.instance.GetGroundData(ref position, out vector, out biome, out biomeArea, out this.m_heightmap);
				if (this.m_heightmap != null)
				{
					this.m_biome = this.m_heightmap.GetBiome(base.transform.position, 0.02f, false);
					float num2 = 9999f;
					foreach (Renderer renderer in this.m_renderers)
					{
						if (!this.m_nonSolidRenderers.Contains(renderer))
						{
							float y = renderer.bounds.min.y;
							if (y < num2)
							{
								num2 = y;
							}
						}
					}
					this.m_groundDist = num2 - position.y;
					if (this.m_staticPosition)
					{
						this.m_lavaValue = this.m_heightmap.GetLava(base.transform.position);
					}
				}
			}
			if (this.m_biome == Heightmap.Biome.AshLands)
			{
				this.m_inAshlands = true;
				if (Game.instance.m_ashDamage > 0f && !flag && !this.m_ashDamageImmune)
				{
					flag2 = (!this.m_haveAshRoof && (!this.m_ashDamageResist || this.GetHealthPercentage() > 0.1f));
					if (flag2)
					{
						if (this.m_ashTimer == 0f)
						{
							this.m_ashTimer = time;
						}
						else if (time - this.m_ashTimer > 5f)
						{
							this.m_ashTimer = time;
							num += Game.instance.m_ashDamage;
						}
					}
					else
					{
						this.m_ashTimer = 0f;
					}
				}
				if (!this.m_staticPosition)
				{
					this.m_lavaValue = this.m_heightmap.GetLava(base.transform.position);
				}
				if (this.m_lavaValue > 0.2f && this.m_groundDist < 1.5f && !this.m_ashDamageImmune)
				{
					if (this.m_lavaTimer == 0f)
					{
						this.m_lavaTimer = time;
					}
					else if (time - this.m_lavaTimer > 2f)
					{
						this.m_lavaTimer = time;
						float num3 = (flag ? 30f : 70f) * this.m_lavaValue;
						num += num3 * (this.m_ashDamageResist ? 0.33f : 1f);
					}
				}
				else
				{
					this.m_lavaTimer = 0f;
				}
			}
			this.m_ashDamageTime = (float)(flag2 ? 5 : 0);
			if (num > 0f && !this.CanBeRemoved())
			{
				num = 0f;
			}
			if (num > 0f)
			{
				float damage = num / 100f * this.m_health;
				this.ApplyDamage(damage, null);
			}
		}
		this.UpdateVisual(true);
	}

	// Token: 0x06001B69 RID: 7017 RVA: 0x000CD2F4 File Offset: 0x000CB4F4
	private Vector3 GetCOM()
	{
		return base.transform.position + base.transform.rotation * this.m_comOffset;
	}

	// Token: 0x06001B6A RID: 7018 RVA: 0x000CD31C File Offset: 0x000CB51C
	public bool IsWet()
	{
		return this.m_rainWet || this.IsUnderWater();
	}

	// Token: 0x06001B6B RID: 7019 RVA: 0x000CD32E File Offset: 0x000CB52E
	private void ClearCachedSupport()
	{
		this.m_supportColliders.Clear();
		this.m_supportPositions.Clear();
		this.m_supportValue.Clear();
	}

	// Token: 0x06001B6C RID: 7020 RVA: 0x000CD351 File Offset: 0x000CB551
	private void RPC_ClearCachedSupport(long sender)
	{
		this.ClearCachedSupport();
	}

	// Token: 0x06001B6D RID: 7021 RVA: 0x000CD35C File Offset: 0x000CB55C
	private void UpdateSupport()
	{
		int count = this.m_supportColliders.Count;
		if (count > 0)
		{
			int num = 0;
			float num2 = 0f;
			for (int i = 0; i < count; i++)
			{
				Collider collider = this.m_supportColliders[i];
				if (collider == null)
				{
					break;
				}
				WearNTear componentInParent = collider.GetComponentInParent<WearNTear>();
				if (componentInParent == null || !componentInParent.m_supports)
				{
					break;
				}
				if (collider != null && collider.transform.position == this.m_supportPositions[i])
				{
					float support = componentInParent.GetSupport();
					if (support > num2)
					{
						num2 = support;
					}
					if (support.Equals(this.m_supportValue[i]))
					{
						num++;
					}
				}
			}
			if (num == this.m_supportPositions.Count && num2 > this.m_support)
			{
				return;
			}
			this.ClearCachedSupport();
		}
		if (this.m_colliders == null)
		{
			this.SetupColliders();
		}
		float num3;
		float num4;
		float num5;
		float num6;
		this.GetMaterialProperties(out num3, out num4, out num5, out num6);
		WearNTear.s_tempSupportPoints.Clear();
		WearNTear.s_tempSupportPointValues.Clear();
		Vector3 com = this.GetCOM();
		bool flag = false;
		float num7 = 0f;
		foreach (WearNTear.BoundData boundData in this.m_bounds)
		{
			int num8 = Physics.OverlapBoxNonAlloc(boundData.m_pos, boundData.m_size, WearNTear.s_tempColliders, boundData.m_rot, WearNTear.s_rayMask);
			if (this.m_clearCachedSupport)
			{
				for (int j = 0; j < num8; j++)
				{
					Collider collider2 = WearNTear.s_tempColliders[j];
					if (!(collider2.attachedRigidbody != null) && !collider2.isTrigger && !this.m_colliders.Contains(collider2))
					{
						WearNTear componentInParent2 = collider2.GetComponentInParent<WearNTear>();
						if (!(componentInParent2 == null))
						{
							if (componentInParent2.m_nview.IsOwner())
							{
								componentInParent2.ClearCachedSupport();
							}
							else if (componentInParent2.m_nview.IsValid())
							{
								componentInParent2.m_nview.InvokeRPC(componentInParent2.m_nview.GetZDO().GetOwner(), "RPC_ClearCachedSupport", Array.Empty<object>());
							}
						}
					}
				}
				this.m_clearCachedSupport = false;
			}
			for (int k = 0; k < num8; k++)
			{
				Collider collider3 = WearNTear.s_tempColliders[k];
				if (!(collider3.attachedRigidbody != null) && !collider3.isTrigger && !this.m_colliders.Contains(collider3))
				{
					int layer = collider3.gameObject.layer;
					int? num9 = WearNTear.s_terrainLayer;
					if (layer == num9.GetValueOrDefault() & num9 != null)
					{
						flag = true;
					}
					else
					{
						WearNTear componentInParent3 = collider3.GetComponentInParent<WearNTear>();
						if (componentInParent3 == null)
						{
							this.m_support = num3;
							this.ClearCachedSupport();
							this.m_nview.GetZDO().Set(ZDOVars.s_support, this.m_support);
							return;
						}
						if (componentInParent3.m_supports)
						{
							float num10 = Vector3.Distance(com, componentInParent3.GetCOM()) + 0.1f;
							float num11 = Vector3.Distance(com, componentInParent3.transform.position) + 0.1f;
							if (num11 < num10 && !this.m_forceCorrectCOMCalculation)
							{
								num10 = num11;
							}
							float support2 = componentInParent3.GetSupport();
							num7 = Mathf.Max(num7, support2 - num5 * num10 * support2);
							Vector3 vector = WearNTear.FindSupportPoint(com, componentInParent3, collider3);
							if (vector.y < com.y + 0.05f)
							{
								Vector3 normalized = (vector - com).normalized;
								if (normalized.y < 0f)
								{
									float t = Mathf.Acos(1f - Mathf.Abs(normalized.y)) / 1.5707964f;
									float num12 = Mathf.Lerp(num5, num6, t);
									float b = support2 - num12 * num10 * support2;
									num7 = Mathf.Max(num7, b);
								}
								float item = support2 - num6 * num10 * support2;
								WearNTear.s_tempSupportPoints.Add(vector);
								WearNTear.s_tempSupportPointValues.Add(item);
								this.m_supportColliders.Add(collider3);
								this.m_supportPositions.Add(collider3.transform.position);
								this.m_supportValue.Add(componentInParent3.GetSupport());
							}
						}
					}
				}
			}
		}
		if (flag)
		{
			this.m_support = num3;
			this.m_nview.GetZDO().Set(ZDOVars.s_support, this.m_support);
			return;
		}
		if (WearNTear.s_tempSupportPoints.Count > 0)
		{
			int count2 = WearNTear.s_tempSupportPoints.Count;
			for (int l = 0; l < count2 - 1; l++)
			{
				Vector3 from = WearNTear.s_tempSupportPoints[l] - com;
				from.y = 0f;
				for (int m = l + 1; m < count2; m++)
				{
					float num13 = (WearNTear.s_tempSupportPointValues[l] + WearNTear.s_tempSupportPointValues[m]) * 0.5f;
					if (num13 > num7)
					{
						Vector3 to = WearNTear.s_tempSupportPoints[m] - com;
						to.y = 0f;
						if (Vector3.Angle(from, to) >= 100f)
						{
							num7 = num13;
						}
					}
				}
			}
		}
		this.m_support = Mathf.Min(num7, num3);
		this.m_nview.GetZDO().Set(ZDOVars.s_support, this.m_support);
		if (!this.HaveSupport())
		{
			this.ClearCachedSupport();
		}
	}

	// Token: 0x06001B6E RID: 7022 RVA: 0x000CD8F8 File Offset: 0x000CBAF8
	private static Vector3 FindSupportPoint(Vector3 com, WearNTear wnt, Collider otherCollider)
	{
		MeshCollider meshCollider = otherCollider as MeshCollider;
		if (!(meshCollider != null) || meshCollider.convex)
		{
			return otherCollider.ClosestPoint(com);
		}
		RaycastHit raycastHit;
		if (meshCollider.Raycast(new Ray(com, Vector3.down), out raycastHit, 10f))
		{
			return raycastHit.point;
		}
		return (com + wnt.GetCOM()) * 0.5f;
	}

	// Token: 0x06001B6F RID: 7023 RVA: 0x000CD95D File Offset: 0x000CBB5D
	private bool HaveSupport()
	{
		return this.m_support >= this.GetMinSupport();
	}

	// Token: 0x06001B70 RID: 7024 RVA: 0x000CD970 File Offset: 0x000CBB70
	private bool IsUnderWater()
	{
		return Floating.IsUnderWater(base.transform.position, ref this.m_previousWaterVolume);
	}

	// Token: 0x06001B71 RID: 7025 RVA: 0x000CD988 File Offset: 0x000CBB88
	public void UpdateCover(float dt)
	{
		this.m_updateCoverTimer += dt;
		if (this.m_updateCoverTimer <= 4f)
		{
			return;
		}
		if (EnvMan.IsWet())
		{
			this.m_haveRoof = this.HaveRoof();
		}
		if (this.m_inAshlands)
		{
			this.m_haveAshRoof = this.HaveAshRoof();
		}
		this.m_updateCoverTimer = 0f;
	}

	// Token: 0x06001B72 RID: 7026 RVA: 0x000CD9E3 File Offset: 0x000CBBE3
	private bool HaveRoof()
	{
		return this.m_roof || WearNTear.RoofCheck(base.transform.position, out this.m_roof);
	}

	// Token: 0x06001B73 RID: 7027 RVA: 0x000CDA0C File Offset: 0x000CBC0C
	public static bool RoofCheck(Vector3 position, out GameObject roofObject)
	{
		int num = Physics.SphereCastNonAlloc(position, 0.1f, Vector3.up, WearNTear.s_raycastHits, 100f, WearNTear.s_rayMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = WearNTear.s_raycastHits[i];
			if (!raycastHit.collider.gameObject.CompareTag("leaky"))
			{
				roofObject = raycastHit.collider.gameObject;
				return true;
			}
		}
		roofObject = null;
		return false;
	}

	// Token: 0x06001B74 RID: 7028 RVA: 0x000CDA80 File Offset: 0x000CBC80
	private bool HaveAshRoof()
	{
		if (this.m_ashroof)
		{
			return true;
		}
		int num = Physics.SphereCastNonAlloc(base.transform.position, 0.1f, Vector3.up, WearNTear.s_raycastHits, 100f, WearNTear.s_rayMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = WearNTear.s_raycastHits[i];
			if (raycastHit.collider.gameObject != base.gameObject && (raycastHit.collider.transform.parent == null || raycastHit.collider.transform.parent.gameObject != base.gameObject))
			{
				this.m_ashroof = raycastHit.collider.gameObject;
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001B75 RID: 7029 RVA: 0x000CDB48 File Offset: 0x000CBD48
	private void RPC_HealthChanged(long peer, float health)
	{
		float health2 = health / this.m_health;
		this.m_healthPercentage = Mathf.Clamp01(health / this.m_health);
		this.ClearCachedSupport();
		this.SetHealthVisual(health2, true);
	}

	// Token: 0x06001B76 RID: 7030 RVA: 0x000CDB7F File Offset: 0x000CBD7F
	private void UpdateVisual(bool triggerEffects)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.SetHealthVisual(this.GetHealthPercentage(), triggerEffects);
	}

	// Token: 0x06001B77 RID: 7031 RVA: 0x000CDB9C File Offset: 0x000CBD9C
	private void SetHealthVisual(float health, bool triggerEffects)
	{
		if (this.m_worn == null && this.m_broken == null && this.m_new == null)
		{
			return;
		}
		if (health > 0.75f)
		{
			if (this.m_worn != this.m_new)
			{
				this.m_worn.SetActive(false);
			}
			if (this.m_broken != this.m_new)
			{
				this.m_broken.SetActive(false);
			}
			this.m_new.SetActive(true);
			return;
		}
		if (health > 0.25f)
		{
			if (triggerEffects && !this.m_worn.activeSelf)
			{
				this.m_switchEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
			}
			if (this.m_new != this.m_worn)
			{
				this.m_new.SetActive(false);
			}
			if (this.m_broken != this.m_worn)
			{
				this.m_broken.SetActive(false);
			}
			this.m_worn.SetActive(true);
			return;
		}
		if (triggerEffects && !this.m_broken.activeSelf)
		{
			this.m_switchEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		}
		if (this.m_new != this.m_broken)
		{
			this.m_new.SetActive(false);
		}
		if (this.m_worn != this.m_broken)
		{
			this.m_worn.SetActive(false);
		}
		this.m_broken.SetActive(true);
	}

	// Token: 0x06001B78 RID: 7032 RVA: 0x000CDD43 File Offset: 0x000CBF43
	public float GetHealthPercentage()
	{
		if (!this.m_nview.IsValid())
		{
			return 1f;
		}
		return this.m_healthPercentage;
	}

	// Token: 0x06001B79 RID: 7033 RVA: 0x000CDD5E File Offset: 0x000CBF5E
	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	// Token: 0x06001B7A RID: 7034 RVA: 0x000CDD61 File Offset: 0x000CBF61
	public void Damage(HitData hit)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("RPC_Damage", new object[]
		{
			hit
		});
	}

	// Token: 0x06001B7B RID: 7035 RVA: 0x000CDD8B File Offset: 0x000CBF8B
	private bool CanBeRemoved()
	{
		return !this.m_piece || this.m_piece.CanBeRemoved();
	}

	// Token: 0x06001B7C RID: 7036 RVA: 0x000CDDA8 File Offset: 0x000CBFA8
	private void RPC_Damage(long sender, HitData hit)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health) <= 0f)
		{
			return;
		}
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damages, out type);
		float totalDamage = hit.GetTotalDamage();
		DamageText.instance.ShowText(type, hit.m_point, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return;
		}
		if (this.m_triggerPrivateArea)
		{
			Character attacker = hit.GetAttacker();
			if (attacker)
			{
				bool destroyed = totalDamage >= this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health);
				PrivateArea.OnObjectDamaged(base.transform.position, attacker, destroyed);
			}
		}
		if (!hit.CheckToolTier(this.m_minToolTier, true))
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f, false);
			return;
		}
		this.ApplyDamage(totalDamage, hit);
		if (hit.m_hitType != HitData.HitType.CinderFire && hit.m_hitType != HitData.HitType.AshlandsOcean)
		{
			this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
			if (hit.GetTotalPhysicalDamage() > 0f)
			{
				this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
			}
		}
		if (this.m_hitNoise > 0f && hit.m_hitType != HitData.HitType.CinderFire)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_hitNoise);
			}
		}
		if (this.m_onDamaged != null)
		{
			this.m_onDamaged();
		}
		if (hit.m_damage.m_fire > 3f && !this.IsWet())
		{
			this.m_burnDamageTime = 3f;
		}
	}

	// Token: 0x06001B7D RID: 7037 RVA: 0x000CDF80 File Offset: 0x000CC180
	public bool ApplyDamage(float damage, HitData hitData = null)
	{
		float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health);
		if (num <= 0f)
		{
			return false;
		}
		num -= damage;
		this.m_nview.GetZDO().Set(ZDOVars.s_health, num);
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("damage"))
		{
			Terminal.Log(string.Format("Damage WNT: {0} took {1} damage from {2}", base.gameObject.name, damage, (hitData == null) ? "UNKNOWN" : hitData));
		}
		if (num <= 0f)
		{
			this.Destroy(hitData, false);
		}
		else
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_HealthChanged", new object[]
			{
				num
			});
		}
		return true;
	}

	// Token: 0x06001B7E RID: 7038 RVA: 0x000CE047 File Offset: 0x000CC247
	public void Remove(bool blockDrop = false)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("RPC_Remove", new object[]
		{
			blockDrop
		});
	}

	// Token: 0x06001B7F RID: 7039 RVA: 0x000CE076 File Offset: 0x000CC276
	private void RPC_Remove(long sender, bool blockDrop)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.Destroy(null, blockDrop);
	}

	// Token: 0x06001B80 RID: 7040 RVA: 0x000CE09C File Offset: 0x000CC29C
	private void Destroy(HitData hitData = null, bool blockDrop = false)
	{
		Bed component = base.GetComponent<Bed>();
		if (component != null && this.m_nview.IsOwner() && Game.instance != null)
		{
			Game.instance.RemoveCustomSpawnPoint(component.GetSpawnPoint());
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_health, 0f);
		this.m_nview.GetZDO().Set(ZDOVars.s_support, 0f);
		this.m_support = 0f;
		this.m_health = 0f;
		this.ClearCachedSupport();
		if (this.m_piece && !blockDrop)
		{
			this.m_piece.DropResources(hitData);
		}
		if (this.m_onDestroyed != null)
		{
			this.m_onDestroyed();
		}
		if (this.m_destroyNoise > 0f && (hitData == null || hitData.m_hitType != HitData.HitType.CinderFire))
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_destroyNoise);
			}
		}
		this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		if (this.m_autoCreateFragments)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_CreateFragments", Array.Empty<object>());
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	// Token: 0x06001B81 RID: 7041 RVA: 0x000CE208 File Offset: 0x000CC408
	private void RPC_CreateFragments(long peer)
	{
		this.ResetHighlight();
		if (this.m_fragmentRoots != null && this.m_fragmentRoots.Length != 0)
		{
			foreach (GameObject gameObject in this.m_fragmentRoots)
			{
				gameObject.SetActive(true);
				Destructible.CreateFragments(gameObject, false);
			}
			return;
		}
		Destructible.CreateFragments(base.gameObject, true);
	}

	// Token: 0x06001B82 RID: 7042 RVA: 0x000CE260 File Offset: 0x000CC460
	private float GetMaxSupport()
	{
		float result;
		float num;
		float num2;
		float num3;
		this.GetMaterialProperties(out result, out num, out num2, out num3);
		return result;
	}

	// Token: 0x06001B83 RID: 7043 RVA: 0x000CE27C File Offset: 0x000CC47C
	private float GetMinSupport()
	{
		float num;
		float result;
		float num2;
		float num3;
		this.GetMaterialProperties(out num, out result, out num2, out num3);
		return result;
	}

	// Token: 0x06001B84 RID: 7044 RVA: 0x000CE298 File Offset: 0x000CC498
	private void GetMaterialProperties(out float maxSupport, out float minSupport, out float horizontalLoss, out float verticalLoss)
	{
		switch (this.m_materialType)
		{
		case WearNTear.MaterialType.Wood:
			maxSupport = 100f;
			minSupport = 10f;
			verticalLoss = 0.125f;
			horizontalLoss = 0.2f;
			return;
		case WearNTear.MaterialType.Stone:
			maxSupport = 1000f;
			minSupport = 100f;
			verticalLoss = 0.125f;
			horizontalLoss = 1f;
			return;
		case WearNTear.MaterialType.Iron:
			maxSupport = 1500f;
			minSupport = 20f;
			verticalLoss = 0.07692308f;
			horizontalLoss = 0.07692308f;
			return;
		case WearNTear.MaterialType.HardWood:
			maxSupport = 140f;
			minSupport = 10f;
			verticalLoss = 0.1f;
			horizontalLoss = 0.16666667f;
			return;
		case WearNTear.MaterialType.Marble:
			maxSupport = 1500f;
			minSupport = 100f;
			verticalLoss = 0.125f;
			horizontalLoss = 0.5f;
			return;
		case WearNTear.MaterialType.Ashstone:
			maxSupport = 2000f;
			minSupport = 100f;
			verticalLoss = 0.1f;
			horizontalLoss = 0.33333334f;
			return;
		case WearNTear.MaterialType.Ancient:
			maxSupport = 5000f;
			minSupport = 100f;
			verticalLoss = 0.06666667f;
			horizontalLoss = 0.25f;
			return;
		default:
			maxSupport = 0f;
			minSupport = 0f;
			verticalLoss = 0f;
			horizontalLoss = 0f;
			return;
		}
	}

	// Token: 0x06001B85 RID: 7045 RVA: 0x000CE3C2 File Offset: 0x000CC5C2
	public static List<WearNTear> GetAllInstances()
	{
		return WearNTear.s_allInstances;
	}

	// Token: 0x04001C15 RID: 7189
	public static bool m_randomInitialDamage = false;

	// Token: 0x04001C16 RID: 7190
	public Action m_onDestroyed;

	// Token: 0x04001C17 RID: 7191
	public Action m_onDamaged;

	// Token: 0x04001C18 RID: 7192
	[Header("Wear")]
	public GameObject m_new;

	// Token: 0x04001C19 RID: 7193
	public GameObject m_worn;

	// Token: 0x04001C1A RID: 7194
	public GameObject m_broken;

	// Token: 0x04001C1B RID: 7195
	public GameObject m_wet;

	// Token: 0x04001C1C RID: 7196
	public bool m_noRoofWear = true;

	// Token: 0x04001C1D RID: 7197
	public bool m_noSupportWear = true;

	// Token: 0x04001C1E RID: 7198
	[global::Tooltip("'Ash Damage' covers both lava and ambient ashlands damage (currently 0... but server modifiers??)")]
	public bool m_ashDamageImmune;

	// Token: 0x04001C1F RID: 7199
	public bool m_ashDamageResist;

	// Token: 0x04001C20 RID: 7200
	public bool m_burnable = true;

	// Token: 0x04001C21 RID: 7201
	public WearNTear.MaterialType m_materialType;

	// Token: 0x04001C22 RID: 7202
	public bool m_supports = true;

	// Token: 0x04001C23 RID: 7203
	public Vector3 m_comOffset = Vector3.zero;

	// Token: 0x04001C24 RID: 7204
	public bool m_forceCorrectCOMCalculation;

	// Token: 0x04001C25 RID: 7205
	public bool m_staticPosition = true;

	// Token: 0x04001C26 RID: 7206
	public List<Renderer> m_nonSolidRenderers = new List<Renderer>();

	// Token: 0x04001C27 RID: 7207
	[Header("Destruction")]
	public float m_health = 100f;

	// Token: 0x04001C28 RID: 7208
	public HitData.DamageModifiers m_damages;

	// Token: 0x04001C29 RID: 7209
	public int m_minToolTier;

	// Token: 0x04001C2A RID: 7210
	public float m_hitNoise;

	// Token: 0x04001C2B RID: 7211
	public float m_destroyNoise;

	// Token: 0x04001C2C RID: 7212
	public bool m_triggerPrivateArea = true;

	// Token: 0x04001C2D RID: 7213
	[Header("Effects")]
	public EffectList m_destroyedEffect = new EffectList();

	// Token: 0x04001C2E RID: 7214
	public EffectList m_hitEffect = new EffectList();

	// Token: 0x04001C2F RID: 7215
	public EffectList m_switchEffect = new EffectList();

	// Token: 0x04001C30 RID: 7216
	public bool m_autoCreateFragments = true;

	// Token: 0x04001C31 RID: 7217
	public GameObject[] m_fragmentRoots;

	// Token: 0x04001C32 RID: 7218
	private const float c_RainDamageTime = 60f;

	// Token: 0x04001C33 RID: 7219
	private const float c_RainDamage = 5f;

	// Token: 0x04001C34 RID: 7220
	private const float c_RainDamageMax = 0.5f;

	// Token: 0x04001C35 RID: 7221
	private const float c_AshDamageTime = 5f;

	// Token: 0x04001C36 RID: 7222
	private const float c_AshDamageMaxResist = 0.1f;

	// Token: 0x04001C37 RID: 7223
	private const float c_LavaDamageTime = 2f;

	// Token: 0x04001C38 RID: 7224
	private const float c_LavaDamage = 70f;

	// Token: 0x04001C39 RID: 7225
	private const float c_LavaDamageShielded = 30f;

	// Token: 0x04001C3A RID: 7226
	private const float c_ComTestWidth = 0.2f;

	// Token: 0x04001C3B RID: 7227
	private const float c_ComMinAngle = 100f;

	// Token: 0x04001C3C RID: 7228
	private static readonly RaycastHit[] s_raycastHits = new RaycastHit[128];

	// Token: 0x04001C3D RID: 7229
	private static readonly Collider[] s_tempColliders = new Collider[128];

	// Token: 0x04001C3E RID: 7230
	private static int s_rayMask = 0;

	// Token: 0x04001C3F RID: 7231
	private static readonly List<WearNTear> s_allInstances = new List<WearNTear>();

	// Token: 0x04001C40 RID: 7232
	private static readonly List<Vector3> s_tempSupportPoints = new List<Vector3>();

	// Token: 0x04001C41 RID: 7233
	private static readonly List<float> s_tempSupportPointValues = new List<float>();

	// Token: 0x04001C42 RID: 7234
	private static readonly int s_AshlandsDamageShaderID = Shader.PropertyToID("_TakingAshlandsDamage");

	// Token: 0x04001C43 RID: 7235
	private MaterialPropertyBlock m_propertyBlock;

	// Token: 0x04001C44 RID: 7236
	private List<Renderer> m_renderers;

	// Token: 0x04001C45 RID: 7237
	private ZNetView m_nview;

	// Token: 0x04001C46 RID: 7238
	private Collider[] m_colliders;

	// Token: 0x04001C47 RID: 7239
	private float m_support = 1f;

	// Token: 0x04001C48 RID: 7240
	private float m_createTime;

	// Token: 0x04001C49 RID: 7241
	private int m_myIndex = -1;

	// Token: 0x04001C4A RID: 7242
	private float m_rainTimer;

	// Token: 0x04001C4B RID: 7243
	private float m_lastRepair;

	// Token: 0x04001C4C RID: 7244
	private Piece m_piece;

	// Token: 0x04001C4D RID: 7245
	private GameObject m_roof;

	// Token: 0x04001C4E RID: 7246
	private float m_healthPercentage = 100f;

	// Token: 0x04001C4F RID: 7247
	private bool m_clearCachedSupport;

	// Token: 0x04001C50 RID: 7248
	private Heightmap m_connectedHeightMap;

	// Token: 0x04001C51 RID: 7249
	private float m_burnDamageTime;

	// Token: 0x04001C52 RID: 7250
	private float m_ashDamageTime;

	// Token: 0x04001C53 RID: 7251
	private float m_ashMaterialValue;

	// Token: 0x04001C54 RID: 7252
	private float m_lastBurnDamageTime;

	// Token: 0x04001C55 RID: 7253
	private float m_lastMaterialValueTimeCheck;

	// Token: 0x04001C56 RID: 7254
	private readonly List<Collider> m_supportColliders = new List<Collider>();

	// Token: 0x04001C57 RID: 7255
	private readonly List<Vector3> m_supportPositions = new List<Vector3>();

	// Token: 0x04001C58 RID: 7256
	private readonly List<float> m_supportValue = new List<float>();

	// Token: 0x04001C59 RID: 7257
	private WaterVolume m_previousWaterVolume;

	// Token: 0x04001C5A RID: 7258
	private GameObject m_ashroof;

	// Token: 0x04001C5B RID: 7259
	private Heightmap.Biome m_biome;

	// Token: 0x04001C5C RID: 7260
	private Heightmap m_heightmap;

	// Token: 0x04001C5D RID: 7261
	private float m_groundDist;

	// Token: 0x04001C5E RID: 7262
	private float m_ashTimer;

	// Token: 0x04001C5F RID: 7263
	private bool m_inAshlands;

	// Token: 0x04001C60 RID: 7264
	private int m_shieldChangeID;

	// Token: 0x04001C61 RID: 7265
	private float m_lavaTimer;

	// Token: 0x04001C62 RID: 7266
	private float m_lavaValue;

	// Token: 0x04001C63 RID: 7267
	private bool m_rainWet;

	// Token: 0x04001C64 RID: 7268
	private List<WearNTear.BoundData> m_bounds;

	// Token: 0x04001C65 RID: 7269
	private List<WearNTear.OldMeshData> m_oldMaterials;

	// Token: 0x04001C66 RID: 7270
	private float m_updateCoverTimer;

	// Token: 0x04001C67 RID: 7271
	private bool m_haveRoof = true;

	// Token: 0x04001C68 RID: 7272
	private bool m_haveAshRoof = true;

	// Token: 0x04001C69 RID: 7273
	private const float c_UpdateCoverFrequency = 4f;

	// Token: 0x04001C6A RID: 7274
	private static int? s_terrainLayer = new int?(-1);

	// Token: 0x02000399 RID: 921
	public enum MaterialType
	{
		// Token: 0x040026D4 RID: 9940
		Wood,
		// Token: 0x040026D5 RID: 9941
		Stone,
		// Token: 0x040026D6 RID: 9942
		Iron,
		// Token: 0x040026D7 RID: 9943
		HardWood,
		// Token: 0x040026D8 RID: 9944
		Marble,
		// Token: 0x040026D9 RID: 9945
		Ashstone,
		// Token: 0x040026DA RID: 9946
		Ancient
	}

	// Token: 0x0200039A RID: 922
	private struct BoundData
	{
		// Token: 0x040026DB RID: 9947
		public Vector3 m_pos;

		// Token: 0x040026DC RID: 9948
		public Quaternion m_rot;

		// Token: 0x040026DD RID: 9949
		public Vector3 m_size;
	}

	// Token: 0x0200039B RID: 923
	private struct OldMeshData
	{
		// Token: 0x040026DE RID: 9950
		public Renderer m_renderer;

		// Token: 0x040026DF RID: 9951
		public Material[] m_materials;

		// Token: 0x040026E0 RID: 9952
		public Color[] m_color;

		// Token: 0x040026E1 RID: 9953
		public Color[] m_emissiveColor;
	}
}
