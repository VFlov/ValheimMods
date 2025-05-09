using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001D3 RID: 467
public class TreeBase : MonoBehaviour, IDestructible
{
	// Token: 0x06001AC3 RID: 6851 RVA: 0x000C6D70 File Offset: 0x000C4F70
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_nview.Register<HitData>("RPC_Damage", new Action<long, HitData>(this.RPC_Damage));
		this.m_nview.Register("RPC_Grow", new Action<long>(this.RPC_Grow));
		this.m_nview.Register("RPC_Shake", new Action<long>(this.RPC_Shake));
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health + (float)Game.m_worldLevel * this.m_health * Game.instance.m_worldLevelMineHPMultiplier) <= 0f)
		{
			this.m_nview.Destroy();
		}
	}

	// Token: 0x06001AC4 RID: 6852 RVA: 0x000C6E30 File Offset: 0x000C5030
	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Tree;
	}

	// Token: 0x06001AC5 RID: 6853 RVA: 0x000C6E33 File Offset: 0x000C5033
	public void Damage(HitData hit)
	{
		this.m_nview.InvokeRPC("RPC_Damage", new object[]
		{
			hit
		});
	}

	// Token: 0x06001AC6 RID: 6854 RVA: 0x000C6E4F File Offset: 0x000C504F
	public void Grow()
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_Grow", Array.Empty<object>());
	}

	// Token: 0x06001AC7 RID: 6855 RVA: 0x000C6E6B File Offset: 0x000C506B
	private void RPC_Grow(long uid)
	{
		base.StartCoroutine("GrowAnimation");
	}

	// Token: 0x06001AC8 RID: 6856 RVA: 0x000C6E79 File Offset: 0x000C5079
	private IEnumerator GrowAnimation()
	{
		GameObject animatedTrunk = UnityEngine.Object.Instantiate<GameObject>(this.m_trunk, this.m_trunk.transform.position, this.m_trunk.transform.rotation, base.transform);
		animatedTrunk.isStatic = false;
		LODGroup component = base.transform.GetComponent<LODGroup>();
		if (component)
		{
			component.fadeMode = LODFadeMode.None;
		}
		this.m_trunk.SetActive(false);
		for (float t = 0f; t < 0.3f; t += Time.deltaTime)
		{
			float d = Mathf.Clamp01(t / 0.3f);
			animatedTrunk.transform.localScale = this.m_trunk.transform.localScale * d;
			yield return null;
		}
		UnityEngine.Object.Destroy(animatedTrunk);
		this.m_trunk.SetActive(true);
		if (this.m_nview.IsOwner())
		{
			this.m_respawnEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		}
		yield break;
	}

	// Token: 0x06001AC9 RID: 6857 RVA: 0x000C6E88 File Offset: 0x000C5088
	private void RPC_Damage(long sender, HitData hit)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_health, this.m_health);
		if (num <= 0f)
		{
			this.m_nview.Destroy();
			return;
		}
		bool flag = hit.m_damage.GetMajorityDamageType() == HitData.DamageType.Fire;
		bool flag2 = hit.m_hitType == HitData.HitType.CinderFire;
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damageModifiers, out type);
		float totalDamage = hit.GetTotalDamage();
		if (!hit.CheckToolTier(this.m_minToolTier, true))
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
		if (!flag && !flag2)
		{
			this.Shake();
		}
		if (!flag2)
		{
			this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(100f);
			}
		}
		if (hit.GetAttacker() == Player.m_localPlayer)
		{
			Game.instance.IncrementPlayerStat(PlayerStatType.TreeChops, 1f);
		}
		if (num <= 0f)
		{
			this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
			this.SpawnLog(hit.m_dir);
			List<GameObject> dropList = this.m_dropWhenDestroyed.GetDropList();
			for (int i = 0; i < dropList.Count; i++)
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
				Vector3 position = base.transform.position + Vector3.up * this.m_spawnYOffset + new Vector3(vector.x, this.m_spawnYStep * (float)i, vector.y);
				Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate<GameObject>(dropList[i], position, rotation);
			}
			base.gameObject.SetActive(false);
			this.m_nview.Destroy();
			if (hit.GetAttacker() == Player.m_localPlayer)
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.Tree, 1f);
				switch (this.m_minToolTier)
				{
				case 0:
					Game.instance.IncrementPlayerStat(PlayerStatType.TreeTier0, 1f);
					return;
				case 1:
					Game.instance.IncrementPlayerStat(PlayerStatType.TreeTier1, 1f);
					return;
				case 2:
					Game.instance.IncrementPlayerStat(PlayerStatType.TreeTier2, 1f);
					return;
				case 3:
					Game.instance.IncrementPlayerStat(PlayerStatType.TreeTier3, 1f);
					return;
				case 4:
					Game.instance.IncrementPlayerStat(PlayerStatType.TreeTier4, 1f);
					return;
				case 5:
					Game.instance.IncrementPlayerStat(PlayerStatType.TreeTier5, 1f);
					return;
				default:
					ZLog.LogWarning("No stat for tree tier: " + this.m_minToolTier.ToString());
					break;
				}
			}
		}
	}

	// Token: 0x06001ACA RID: 6858 RVA: 0x000C71C4 File Offset: 0x000C53C4
	private void Shake()
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_Shake", Array.Empty<object>());
	}

	// Token: 0x06001ACB RID: 6859 RVA: 0x000C71E0 File Offset: 0x000C53E0
	private void RPC_Shake(long uid)
	{
		base.StopCoroutine("ShakeAnimation");
		base.StartCoroutine("ShakeAnimation");
	}

	// Token: 0x06001ACC RID: 6860 RVA: 0x000C71F9 File Offset: 0x000C53F9
	private IEnumerator ShakeAnimation()
	{
		this.m_trunk.gameObject.isStatic = false;
		float t = Time.time;
		while (Time.time - t < 1f)
		{
			float time = Time.time;
			float num = 1f - Mathf.Clamp01((time - t) / 1f);
			float num2 = num * num * num * 1.5f;
			Quaternion localRotation = Quaternion.Euler(Mathf.Sin(time * 40f) * num2, 0f, Mathf.Cos(time * 0.9f * 40f) * num2);
			this.m_trunk.transform.localRotation = localRotation;
			yield return null;
		}
		this.m_trunk.transform.localRotation = Quaternion.identity;
		this.m_trunk.gameObject.isStatic = true;
		yield break;
	}

	// Token: 0x06001ACD RID: 6861 RVA: 0x000C7208 File Offset: 0x000C5408
	private void SpawnLog(Vector3 hitDir)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_logPrefab, this.m_logSpawnPoint.position, this.m_logSpawnPoint.rotation);
		gameObject.GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
		Rigidbody component = gameObject.GetComponent<Rigidbody>();
		component.mass *= base.transform.localScale.x;
		component.ResetInertiaTensor();
		component.AddForceAtPosition(hitDir * 0.2f, gameObject.transform.position + Vector3.up * 4f * base.transform.localScale.y, ForceMode.VelocityChange);
		if (this.m_stubPrefab)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_stubPrefab, base.transform.position, base.transform.rotation).GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
		}
	}

	// Token: 0x04001B2F RID: 6959
	private ZNetView m_nview;

	// Token: 0x04001B30 RID: 6960
	public float m_health = 1f;

	// Token: 0x04001B31 RID: 6961
	public HitData.DamageModifiers m_damageModifiers;

	// Token: 0x04001B32 RID: 6962
	public int m_minToolTier;

	// Token: 0x04001B33 RID: 6963
	public EffectList m_destroyedEffect = new EffectList();

	// Token: 0x04001B34 RID: 6964
	public EffectList m_hitEffect = new EffectList();

	// Token: 0x04001B35 RID: 6965
	public EffectList m_respawnEffect = new EffectList();

	// Token: 0x04001B36 RID: 6966
	public GameObject m_trunk;

	// Token: 0x04001B37 RID: 6967
	public GameObject m_stubPrefab;

	// Token: 0x04001B38 RID: 6968
	public GameObject m_logPrefab;

	// Token: 0x04001B39 RID: 6969
	public Transform m_logSpawnPoint;

	// Token: 0x04001B3A RID: 6970
	[Header("Drops")]
	public DropTable m_dropWhenDestroyed = new DropTable();

	// Token: 0x04001B3B RID: 6971
	public float m_spawnYOffset = 0.5f;

	// Token: 0x04001B3C RID: 6972
	public float m_spawnYStep = 0.3f;
}
