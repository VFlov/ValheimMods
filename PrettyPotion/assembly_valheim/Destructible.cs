using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200016A RID: 362
public class Destructible : MonoBehaviour, IDestructible
{
	// Token: 0x060015EF RID: 5615 RVA: 0x000A1A54 File Offset: 0x0009FC54
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		if (this.m_nview && this.m_nview.GetZDO() != null)
		{
			this.m_nview.Register<HitData>("RPC_Damage", new Action<long, HitData>(this.RPC_Damage));
			if (this.m_autoCreateFragments)
			{
				this.m_nview.Register("RPC_CreateFragments", new Action<long>(this.RPC_CreateFragments));
			}
			if (this.m_ttl > 0f)
			{
				base.InvokeRepeating("DestroyNow", this.m_ttl, 1f);
			}
		}
	}

	// Token: 0x060015F0 RID: 5616 RVA: 0x000A1AF6 File Offset: 0x0009FCF6
	private void Start()
	{
		this.m_firstFrame = false;
	}

	// Token: 0x060015F1 RID: 5617 RVA: 0x000A1AFF File Offset: 0x0009FCFF
	public GameObject GetParentObject()
	{
		return null;
	}

	// Token: 0x060015F2 RID: 5618 RVA: 0x000A1B02 File Offset: 0x0009FD02
	public DestructibleType GetDestructibleType()
	{
		return this.m_destructibleType;
	}

	// Token: 0x060015F3 RID: 5619 RVA: 0x000A1B0A File Offset: 0x0009FD0A
	public void Damage(HitData hit)
	{
		if (this.m_firstFrame)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("RPC_Damage", new object[]
		{
			hit
		});
	}

	// Token: 0x060015F4 RID: 5620 RVA: 0x000A1B40 File Offset: 0x0009FD40
	private void RPC_Damage(long sender, HitData hit)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_destroyed)
		{
			return;
		}
		float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health + (float)Game.m_worldLevel * this.m_health * Game.instance.m_worldLevelMineHPMultiplier);
		if (num <= 0f || this.m_destroyed)
		{
			return;
		}
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damages, out type);
		float totalDamage = hit.GetTotalDamage();
		if (this.m_body)
		{
			this.m_body.AddForceAtPosition(hit.m_dir * hit.m_pushForce, hit.m_point, ForceMode.Impulse);
		}
		if (!hit.CheckToolTier(this.m_minToolTier, false))
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f, false);
			return;
		}
		DamageText.instance.ShowText(type, hit.m_point, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return;
		}
		num -= totalDamage;
		this.m_nview.GetZDO().Set(ZDOVars.s_health, num);
		if (this.m_triggerPrivateArea)
		{
			Character attacker = hit.GetAttacker();
			if (attacker)
			{
				bool destroyed = num <= 0f;
				PrivateArea.OnObjectDamaged(base.transform.position, attacker, destroyed);
			}
		}
		this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
		if (this.m_onDamaged != null)
		{
			this.m_onDamaged();
		}
		if (this.m_hitNoise > 0f && hit.m_hitType != HitData.HitType.CinderFire)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_hitNoise);
			}
		}
		if (num <= 0f)
		{
			this.Destroy(hit);
		}
	}

	// Token: 0x060015F5 RID: 5621 RVA: 0x000A1D15 File Offset: 0x0009FF15
	public void DestroyNow()
	{
		if (this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			this.Destroy(null);
		}
	}

	// Token: 0x060015F6 RID: 5622 RVA: 0x000A1D38 File Offset: 0x0009FF38
	public void Destroy(HitData hit = null)
	{
		Vector3 hitPoint = (hit == null) ? Vector3.zero : hit.m_point;
		Vector3 hitDir = (hit == null) ? Vector3.zero : hit.m_dir;
		this.CreateDestructionEffects(hitPoint, hitDir);
		if (this.m_destroyNoise > 0f && (hit == null || hit.m_hitType != HitData.HitType.CinderFire))
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_destroyNoise);
			}
		}
		if (this.m_spawnWhenDestroyed)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnWhenDestroyed, base.transform.position, base.transform.rotation);
			gameObject.GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
			Gibber component = gameObject.GetComponent<Gibber>();
			if (component != null)
			{
				component.Setup(hitPoint, hitDir);
			}
			if (hit != null)
			{
				MineRock5 component2 = gameObject.GetComponent<MineRock5>();
				if (component2 != null)
				{
					component2.Damage(hit);
				}
			}
		}
		if (this.m_onDestroyed != null)
		{
			this.m_onDestroyed();
		}
		ZNetScene.instance.Destroy(base.gameObject);
		this.m_destroyed = true;
	}

	// Token: 0x060015F7 RID: 5623 RVA: 0x000A1E50 File Offset: 0x000A0050
	private void CreateDestructionEffects(Vector3 hitPoint, Vector3 hitDir)
	{
		GameObject[] array = this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		for (int i = 0; i < array.Length; i++)
		{
			Gibber component = array[i].GetComponent<Gibber>();
			if (component)
			{
				component.Setup(hitPoint, hitDir);
			}
		}
		if (this.m_autoCreateFragments)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_CreateFragments", Array.Empty<object>());
		}
	}

	// Token: 0x060015F8 RID: 5624 RVA: 0x000A1ED4 File Offset: 0x000A00D4
	private void RPC_CreateFragments(long peer)
	{
		Destructible.CreateFragments(base.gameObject, true);
	}

	// Token: 0x060015F9 RID: 5625 RVA: 0x000A1EE4 File Offset: 0x000A00E4
	public static void CreateFragments(GameObject rootObject, bool visibleOnly = true)
	{
		MeshRenderer[] componentsInChildren = rootObject.GetComponentsInChildren<MeshRenderer>(true);
		int layer = LayerMask.NameToLayer("effect");
		List<Rigidbody> list = new List<Rigidbody>();
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			if (meshRenderer.gameObject.activeInHierarchy && (!visibleOnly || meshRenderer.isVisible))
			{
				MeshFilter component = meshRenderer.gameObject.GetComponent<MeshFilter>();
				if (!(component == null))
				{
					if (component.sharedMesh == null)
					{
						ZLog.Log("Meshfilter missing mesh " + component.gameObject.name);
					}
					else
					{
						GameObject gameObject = new GameObject();
						gameObject.layer = layer;
						gameObject.transform.position = component.gameObject.transform.position;
						gameObject.transform.rotation = component.gameObject.transform.rotation;
						gameObject.transform.localScale = component.gameObject.transform.lossyScale * 0.9f;
						gameObject.AddComponent<MeshFilter>().sharedMesh = component.sharedMesh;
						gameObject.AddComponent<MeshRenderer>().sharedMaterials = meshRenderer.sharedMaterials;
						MaterialMan.instance.SetValue<float>(gameObject, ShaderProps._RippleDistance, 0f);
						MaterialMan.instance.SetValue<float>(gameObject, ShaderProps._ValueNoise, 0f);
						Rigidbody item = gameObject.AddComponent<Rigidbody>();
						gameObject.AddComponent<BoxCollider>();
						list.Add(item);
						gameObject.AddComponent<TimedDestruction>().Trigger((float)UnityEngine.Random.Range(2, 4));
					}
				}
			}
		}
		if (list.Count > 0)
		{
			Vector3 vector = Vector3.zero;
			int num = 0;
			foreach (Rigidbody rigidbody in list)
			{
				vector += rigidbody.worldCenterOfMass;
				num++;
			}
			vector /= (float)num;
			foreach (Rigidbody rigidbody2 in list)
			{
				Vector3 vector2 = (rigidbody2.worldCenterOfMass - vector).normalized * 4f;
				vector2 += UnityEngine.Random.onUnitSphere * 1f;
				rigidbody2.AddForce(vector2, ForceMode.VelocityChange);
			}
		}
	}

	// Token: 0x04001592 RID: 5522
	public Action m_onDestroyed;

	// Token: 0x04001593 RID: 5523
	public Action m_onDamaged;

	// Token: 0x04001594 RID: 5524
	[Header("Destruction")]
	public DestructibleType m_destructibleType = DestructibleType.Default;

	// Token: 0x04001595 RID: 5525
	public float m_health = 1f;

	// Token: 0x04001596 RID: 5526
	public HitData.DamageModifiers m_damages;

	// Token: 0x04001597 RID: 5527
	public float m_minDamageTreshold;

	// Token: 0x04001598 RID: 5528
	public int m_minToolTier;

	// Token: 0x04001599 RID: 5529
	public float m_hitNoise;

	// Token: 0x0400159A RID: 5530
	public float m_destroyNoise;

	// Token: 0x0400159B RID: 5531
	public bool m_triggerPrivateArea;

	// Token: 0x0400159C RID: 5532
	public float m_ttl;

	// Token: 0x0400159D RID: 5533
	public GameObject m_spawnWhenDestroyed;

	// Token: 0x0400159E RID: 5534
	[Header("Effects")]
	public EffectList m_destroyedEffect = new EffectList();

	// Token: 0x0400159F RID: 5535
	public EffectList m_hitEffect = new EffectList();

	// Token: 0x040015A0 RID: 5536
	public bool m_autoCreateFragments;

	// Token: 0x040015A1 RID: 5537
	private ZNetView m_nview;

	// Token: 0x040015A2 RID: 5538
	private Rigidbody m_body;

	// Token: 0x040015A3 RID: 5539
	private bool m_firstFrame = true;

	// Token: 0x040015A4 RID: 5540
	private bool m_destroyed;
}
