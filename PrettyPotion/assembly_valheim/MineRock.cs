using System;
using UnityEngine;

// Token: 0x02000198 RID: 408
public class MineRock : MonoBehaviour, IDestructible, Hoverable
{
	// Token: 0x06001828 RID: 6184 RVA: 0x000B4328 File Offset: 0x000B2528
	private void Start()
	{
		this.m_hitAreas = ((this.m_areaRoot != null) ? this.m_areaRoot.GetComponentsInChildren<Collider>() : base.gameObject.GetComponentsInChildren<Collider>());
		if (this.m_baseModel)
		{
			this.m_areaMeshes = new MeshRenderer[this.m_hitAreas.Length][];
			for (int i = 0; i < this.m_hitAreas.Length; i++)
			{
				this.m_areaMeshes[i] = this.m_hitAreas[i].GetComponents<MeshRenderer>();
			}
		}
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview && this.m_nview.GetZDO() != null)
		{
			this.m_nview.Register<HitData, int>("Hit", new Action<long, HitData, int>(this.RPC_Hit));
			this.m_nview.Register<int>("Hide", new Action<long, int>(this.RPC_Hide));
		}
		base.InvokeRepeating("UpdateVisability", UnityEngine.Random.Range(1f, 2f), 10f);
	}

	// Token: 0x06001829 RID: 6185 RVA: 0x000B4426 File Offset: 0x000B2626
	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name);
	}

	// Token: 0x0600182A RID: 6186 RVA: 0x000B4438 File Offset: 0x000B2638
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x0600182B RID: 6187 RVA: 0x000B4440 File Offset: 0x000B2640
	private void UpdateVisability()
	{
		bool flag = false;
		for (int i = 0; i < this.m_hitAreas.Length; i++)
		{
			Collider collider = this.m_hitAreas[i];
			if (collider)
			{
				string name = "Health" + i.ToString();
				bool flag2 = this.m_nview.GetZDO().GetFloat(name, this.GetHealth()) > 0f;
				collider.gameObject.SetActive(flag2);
				if (!flag2)
				{
					flag = true;
				}
			}
		}
		if (this.m_baseModel)
		{
			this.m_baseModel.SetActive(!flag);
			foreach (MeshRenderer[] array in this.m_areaMeshes)
			{
				for (int k = 0; k < array.Length; k++)
				{
					array[k].enabled = flag;
				}
			}
		}
	}

	// Token: 0x0600182C RID: 6188 RVA: 0x000B4514 File Offset: 0x000B2714
	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	// Token: 0x0600182D RID: 6189 RVA: 0x000B4518 File Offset: 0x000B2718
	public void Damage(HitData hit)
	{
		if (hit.m_hitCollider == null)
		{
			ZLog.Log("Minerock hit has no collider");
			return;
		}
		int areaIndex = this.GetAreaIndex(hit.m_hitCollider);
		if (areaIndex == -1)
		{
			ZLog.Log("Invalid hit area on " + base.gameObject.name);
			return;
		}
		ZLog.Log("Hit mine rock area " + areaIndex.ToString());
		this.m_nview.InvokeRPC("Hit", new object[]
		{
			hit,
			areaIndex
		});
	}

	// Token: 0x0600182E RID: 6190 RVA: 0x000B45A4 File Offset: 0x000B27A4
	private void RPC_Hit(long sender, HitData hit, int hitAreaIndex)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		Collider hitArea = this.GetHitArea(hitAreaIndex);
		if (hitArea == null)
		{
			ZLog.Log("Missing hit area " + hitAreaIndex.ToString());
			return;
		}
		string name = "Health" + hitAreaIndex.ToString();
		float num = this.m_nview.GetZDO().GetFloat(name, this.GetHealth());
		if (num <= 0f)
		{
			ZLog.Log("Already destroyed");
			return;
		}
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damageModifiers, out type);
		float totalDamage = hit.GetTotalDamage();
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
		this.m_nview.GetZDO().Set(name, num);
		this.m_hitEffect.Create(hit.m_point, Quaternion.identity, null, 1f, -1);
		if (hit.m_hitType != HitData.HitType.CinderFire)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(100f);
			}
		}
		if (this.m_onHit != null)
		{
			this.m_onHit();
		}
		if (hit.GetAttacker() == Player.m_localPlayer)
		{
			Game.instance.IncrementPlayerStat(PlayerStatType.MineHits, 1f);
		}
		if (num <= 0f)
		{
			this.m_destroyedEffect.Create(hitArea.bounds.center, Quaternion.identity, null, 1f, -1);
			this.m_nview.InvokeRPC(ZNetView.Everybody, "Hide", new object[]
			{
				hitAreaIndex
			});
			foreach (GameObject gameObject in this.m_dropItems.GetDropList())
			{
				Vector3 position = hit.m_point - hit.m_dir * 0.2f + UnityEngine.Random.insideUnitSphere * 0.3f;
				UnityEngine.Object.Instantiate<GameObject>(gameObject, position, Quaternion.identity);
				ItemDrop.OnCreateNew(gameObject);
			}
			if (this.m_removeWhenDestroyed && this.AllDestroyed())
			{
				this.m_nview.Destroy();
			}
			if (hit.GetAttacker() == Player.m_localPlayer)
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.Mines, 1f);
				switch (this.m_minToolTier)
				{
				case 0:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier0, 1f);
					return;
				case 1:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier1, 1f);
					return;
				case 2:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier2, 1f);
					return;
				case 3:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier3, 1f);
					return;
				case 4:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier4, 1f);
					return;
				case 5:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier5, 1f);
					return;
				default:
					ZLog.LogWarning("No stat for mine tier: " + this.m_minToolTier.ToString());
					break;
				}
			}
		}
	}

	// Token: 0x0600182F RID: 6191 RVA: 0x000B48EC File Offset: 0x000B2AEC
	private bool AllDestroyed()
	{
		for (int i = 0; i < this.m_hitAreas.Length; i++)
		{
			string name = "Health" + i.ToString();
			if (this.m_nview.GetZDO().GetFloat(name, this.GetHealth()) > 0f)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06001830 RID: 6192 RVA: 0x000B4940 File Offset: 0x000B2B40
	private void RPC_Hide(long sender, int index)
	{
		Collider hitArea = this.GetHitArea(index);
		if (hitArea)
		{
			hitArea.gameObject.SetActive(false);
		}
		if (this.m_baseModel && this.m_baseModel.activeSelf)
		{
			this.m_baseModel.SetActive(false);
			foreach (MeshRenderer[] array in this.m_areaMeshes)
			{
				for (int j = 0; j < array.Length; j++)
				{
					array[j].enabled = true;
				}
			}
		}
	}

	// Token: 0x06001831 RID: 6193 RVA: 0x000B49C4 File Offset: 0x000B2BC4
	private int GetAreaIndex(Collider area)
	{
		for (int i = 0; i < this.m_hitAreas.Length; i++)
		{
			if (this.m_hitAreas[i] == area)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x06001832 RID: 6194 RVA: 0x000B49F7 File Offset: 0x000B2BF7
	private Collider GetHitArea(int index)
	{
		if (index < 0 || index >= this.m_hitAreas.Length)
		{
			return null;
		}
		return this.m_hitAreas[index];
	}

	// Token: 0x06001833 RID: 6195 RVA: 0x000B4A12 File Offset: 0x000B2C12
	public float GetHealth()
	{
		return this.m_health + (float)Game.m_worldLevel * this.m_health * Game.instance.m_worldLevelMineHPMultiplier;
	}

	// Token: 0x0400181D RID: 6173
	public string m_name = "";

	// Token: 0x0400181E RID: 6174
	public float m_health = 2f;

	// Token: 0x0400181F RID: 6175
	public bool m_removeWhenDestroyed = true;

	// Token: 0x04001820 RID: 6176
	public HitData.DamageModifiers m_damageModifiers;

	// Token: 0x04001821 RID: 6177
	public int m_minToolTier;

	// Token: 0x04001822 RID: 6178
	public GameObject m_areaRoot;

	// Token: 0x04001823 RID: 6179
	public GameObject m_baseModel;

	// Token: 0x04001824 RID: 6180
	public EffectList m_destroyedEffect = new EffectList();

	// Token: 0x04001825 RID: 6181
	public EffectList m_hitEffect = new EffectList();

	// Token: 0x04001826 RID: 6182
	public DropTable m_dropItems;

	// Token: 0x04001827 RID: 6183
	public Action m_onHit;

	// Token: 0x04001828 RID: 6184
	private Collider[] m_hitAreas;

	// Token: 0x04001829 RID: 6185
	private MeshRenderer[][] m_areaMeshes;

	// Token: 0x0400182A RID: 6186
	private ZNetView m_nview;
}
