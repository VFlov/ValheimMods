using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001D4 RID: 468
public class TreeLog : MonoBehaviour, IDestructible
{
	// Token: 0x06001ACF RID: 6863 RVA: 0x000C7360 File Offset: 0x000C5560
	private void Awake()
	{
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_body.maxDepenetrationVelocity = 1f;
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_nview.Register<HitData>("RPC_Damage", new Action<long, HitData>(this.RPC_Damage));
		if (this.m_nview.IsOwner())
		{
			float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, -1f);
			if (@float == -1f)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_health, this.m_health + (float)Game.m_worldLevel * this.m_health * Game.instance.m_worldLevelMineHPMultiplier);
			}
			else if (@float <= 0f)
			{
				this.m_nview.Destroy();
			}
		}
		base.Invoke("EnableDamage", 0.2f);
	}

	// Token: 0x06001AD0 RID: 6864 RVA: 0x000C743A File Offset: 0x000C563A
	private void EnableDamage()
	{
		this.m_firstFrame = false;
	}

	// Token: 0x06001AD1 RID: 6865 RVA: 0x000C7443 File Offset: 0x000C5643
	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Tree;
	}

	// Token: 0x06001AD2 RID: 6866 RVA: 0x000C7446 File Offset: 0x000C5646
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

	// Token: 0x06001AD3 RID: 6867 RVA: 0x000C747C File Offset: 0x000C567C
	private void RPC_Damage(long sender, HitData hit)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, 0f);
		if (num <= 0f)
		{
			return;
		}
		HitData hitData = hit.Clone();
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damages, out type);
		float totalDamage = hit.GetTotalDamage();
		if (!hit.CheckToolTier(this.m_minToolTier, true))
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f, false);
			return;
		}
		if (this.m_body)
		{
			this.m_body.AddForceAtPosition(hit.m_dir * hit.m_pushForce * 2f, hit.m_point, ForceMode.Impulse);
		}
		DamageText.instance.ShowText(type, hit.m_point, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return;
		}
		num -= totalDamage;
		if (num < 0f)
		{
			num = 0f;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_health, num);
		if (hit.m_hitType != HitData.HitType.CinderFire)
		{
			this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
			if (this.m_hitNoise > 0f)
			{
				Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
				if (closestPlayer)
				{
					closestPlayer.AddNoise(this.m_hitNoise);
				}
			}
		}
		if (hit.GetAttacker() == Player.m_localPlayer)
		{
			Game.instance.IncrementPlayerStat(PlayerStatType.LogChops, 1f);
		}
		if (num <= 0f)
		{
			this.Destroy(hitData);
			if (hit.GetAttacker() == Player.m_localPlayer)
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.Logs, 1f);
			}
		}
	}

	// Token: 0x06001AD4 RID: 6868 RVA: 0x000C7638 File Offset: 0x000C5838
	private void Destroy(HitData hitData = null)
	{
		ZNetScene.instance.Destroy(base.gameObject);
		this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		List<GameObject> dropList = this.m_dropWhenDestroyed.GetDropList();
		for (int i = 0; i < dropList.Count; i++)
		{
			Vector3 position = base.transform.position + base.transform.up * UnityEngine.Random.Range(-this.m_spawnDistance, this.m_spawnDistance) + Vector3.up * 0.3f * (float)i;
			Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
			GameObject gameObject = dropList[i];
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			int num = 1;
			if (component)
			{
				gameObject = Game.instance.CheckDropConversion(hitData, component, gameObject, ref num);
			}
			for (int j = 0; j < num; j++)
			{
				ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate<GameObject>(gameObject, position, rotation));
			}
		}
		if (this.m_subLogPrefab != null)
		{
			foreach (Transform transform in this.m_subLogPoints)
			{
				Quaternion rotation2 = this.m_useSubLogPointRotation ? transform.rotation : base.transform.rotation;
				UnityEngine.Object.Instantiate<GameObject>(this.m_subLogPrefab, transform.position, rotation2).GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
			}
		}
	}

	// Token: 0x04001B3D RID: 6973
	public float m_health = 60f;

	// Token: 0x04001B3E RID: 6974
	public HitData.DamageModifiers m_damages;

	// Token: 0x04001B3F RID: 6975
	public int m_minToolTier;

	// Token: 0x04001B40 RID: 6976
	public EffectList m_destroyedEffect = new EffectList();

	// Token: 0x04001B41 RID: 6977
	public EffectList m_hitEffect = new EffectList();

	// Token: 0x04001B42 RID: 6978
	public DropTable m_dropWhenDestroyed = new DropTable();

	// Token: 0x04001B43 RID: 6979
	public GameObject m_subLogPrefab;

	// Token: 0x04001B44 RID: 6980
	public Transform[] m_subLogPoints = Array.Empty<Transform>();

	// Token: 0x04001B45 RID: 6981
	public bool m_useSubLogPointRotation;

	// Token: 0x04001B46 RID: 6982
	public float m_spawnDistance = 2f;

	// Token: 0x04001B47 RID: 6983
	public float m_hitNoise = 100f;

	// Token: 0x04001B48 RID: 6984
	private Rigidbody m_body;

	// Token: 0x04001B49 RID: 6985
	private ZNetView m_nview;

	// Token: 0x04001B4A RID: 6986
	private bool m_firstFrame = true;
}
