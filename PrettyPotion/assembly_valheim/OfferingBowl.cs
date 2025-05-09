using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200019F RID: 415
public class OfferingBowl : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001877 RID: 6263 RVA: 0x000B6F4C File Offset: 0x000B514C
	private void Awake()
	{
		this.m_solidRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece"
		});
	}

	// Token: 0x06001878 RID: 6264 RVA: 0x000B6F80 File Offset: 0x000B5180
	private void Start()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		if (!this.m_nview)
		{
			return;
		}
		this.m_nview.Register<Vector3, bool>("RPC_SpawnBoss", new Action<long, Vector3, bool>(this.RPC_SpawnBoss));
		this.m_nview.Register("RPC_BossSpawnInitiated", new Action<long>(this.RPC_BossSpawnInitiated));
		this.m_nview.Register("RPC_RemoveBossSpawnInventoryItems", new Action<long>(this.RPC_RemoveBossSpawnInventoryItems));
	}

	// Token: 0x06001879 RID: 6265 RVA: 0x000B6FFC File Offset: 0x000B51FC
	public string GetHoverText()
	{
		if (this.m_useItemStands)
		{
			return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + this.m_useItemText);
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>1-8</b></color>] " + this.m_useItemText);
	}

	// Token: 0x0600187A RID: 6266 RVA: 0x000B7052 File Offset: 0x000B5252
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x0600187B RID: 6267 RVA: 0x000B705C File Offset: 0x000B525C
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold || this.IsBossSpawnQueued() || !this.m_useItemStands)
		{
			return false;
		}
		using (List<ItemStand>.Enumerator enumerator = this.FindItemStands().GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.HaveAttachment())
				{
					user.Message(MessageHud.MessageType.Center, this.m_incompleteOfferText, 0, null);
					return false;
				}
			}
		}
		this.m_interactUser = user;
		this.InitiateSpawnBoss(this.GetSpawnPosition(), false);
		return true;
	}

	// Token: 0x0600187C RID: 6268 RVA: 0x000B70F0 File Offset: 0x000B52F0
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.m_useItemStands)
		{
			return false;
		}
		if (this.IsBossSpawnQueued())
		{
			return true;
		}
		if (!(this.m_bossItem != null))
		{
			return false;
		}
		if (!(item.m_shared.m_name == this.m_bossItem.m_itemData.m_shared.m_name))
		{
			user.Message(MessageHud.MessageType.Center, this.m_wrongOfferText, 0, null);
			return true;
		}
		int num = user.GetInventory().CountItems(this.m_bossItem.m_itemData.m_shared.m_name, -1, true);
		if (num < this.m_bossItems)
		{
			if (num == 0 && Game.m_worldLevel > 0 && user.GetInventory().CountItems(this.m_bossItem.m_itemData.m_shared.m_name, -1, false) > 0)
			{
				user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_ng_the_x") + item.m_shared.m_name + Localization.instance.Localize("$msg_ng_x_is_too_low"), 0, null);
			}
			else
			{
				user.Message(MessageHud.MessageType.Center, string.Format("{0}: {1} {2} / {3}", new object[]
				{
					this.m_incompleteOfferText,
					this.m_bossItem.m_itemData.m_shared.m_name,
					num.ToString(),
					this.m_bossItems
				}), 0, null);
			}
			return true;
		}
		if (this.m_bossPrefab != null)
		{
			this.m_usedSpawnItem = item;
			this.m_interactUser = user;
			this.InitiateSpawnBoss(this.GetSpawnPosition(), true);
		}
		else if (this.m_itemPrefab != null && this.SpawnItem(this.m_itemPrefab, user as Player))
		{
			user.GetInventory().RemoveItem(item.m_shared.m_name, this.m_bossItems, -1, true);
			user.ShowRemovedMessage(this.m_bossItem.m_itemData, this.m_bossItems);
			user.Message(MessageHud.MessageType.Center, this.m_usedAltarText, 0, null);
			this.m_fuelAddedEffects.Create(this.m_itemSpawnPoint.position, base.transform.rotation, null, 1f, -1);
		}
		if (!string.IsNullOrEmpty(this.m_setGlobalKey))
		{
			ZoneSystem.instance.SetGlobalKey(this.m_setGlobalKey);
		}
		return true;
	}

	// Token: 0x0600187D RID: 6269 RVA: 0x000B732C File Offset: 0x000B552C
	private bool SpawnItem(ItemDrop item, Player player)
	{
		if (item.m_itemData.m_shared.m_questItem && player.HaveUniqueKey(item.m_itemData.m_shared.m_name))
		{
			player.Message(MessageHud.MessageType.Center, this.m_cantOfferText, 0, null);
			return false;
		}
		UnityEngine.Object.Instantiate<ItemDrop>(item, this.m_itemSpawnPoint.position, Quaternion.identity);
		return true;
	}

	// Token: 0x0600187E RID: 6270 RVA: 0x000B738C File Offset: 0x000B558C
	private Vector3 GetSpawnPosition()
	{
		if (this.m_spawnPoints.Count > 0)
		{
			return this.m_spawnPoints[UnityEngine.Random.Range(0, this.m_spawnPoints.Count)].transform.position;
		}
		Vector3 b = base.transform.localToWorldMatrix * this.m_spawnAreaOffset;
		return base.transform.position + b;
	}

	// Token: 0x0600187F RID: 6271 RVA: 0x000B7400 File Offset: 0x000B5600
	private void InitiateSpawnBoss(Vector3 point, bool removeItemsFromInventory)
	{
		this.m_nview.InvokeRPC("RPC_SpawnBoss", new object[]
		{
			point,
			removeItemsFromInventory
		});
	}

	// Token: 0x06001880 RID: 6272 RVA: 0x000B742C File Offset: 0x000B562C
	private void RPC_SpawnBoss(long senderId, Vector3 point, bool removeItemsFromInventory)
	{
		if (!this.m_nview.IsOwner() || this.IsBossSpawnQueued())
		{
			return;
		}
		Vector3 spawnPoint;
		if (this.CanSpawnBoss(point, out spawnPoint))
		{
			if (!this.m_nview && this.m_nview.IsValid())
			{
				return;
			}
			this.SpawnBoss(spawnPoint);
			this.m_nview.InvokeRPC(senderId, "RPC_BossSpawnInitiated", Array.Empty<object>());
			if (removeItemsFromInventory)
			{
				this.m_nview.InvokeRPC(senderId, "RPC_RemoveBossSpawnInventoryItems", Array.Empty<object>());
				return;
			}
			this.RemoveAltarItems();
		}
	}

	// Token: 0x06001881 RID: 6273 RVA: 0x000B74B2 File Offset: 0x000B56B2
	private void SpawnBoss(Vector3 spawnPoint)
	{
		base.Invoke("DelayedSpawnBoss", this.m_spawnBossDelay);
		this.m_spawnBossStartEffects.Create(spawnPoint, Quaternion.identity, null, 1f, -1);
		this.m_bossSpawnPoint = spawnPoint;
	}

	// Token: 0x06001882 RID: 6274 RVA: 0x000B74E8 File Offset: 0x000B56E8
	private void RPC_RemoveBossSpawnInventoryItems(long senderId)
	{
		this.m_interactUser.GetInventory().RemoveItem(this.m_usedSpawnItem.m_shared.m_name, this.m_bossItems, -1, true);
		this.m_interactUser.ShowRemovedMessage(this.m_bossItem.m_itemData, this.m_bossItems);
		this.m_interactUser.Message(MessageHud.MessageType.Center, this.m_usedAltarText, 0, null);
		if (this.m_itemSpawnPoint)
		{
			this.m_fuelAddedEffects.Create(this.m_itemSpawnPoint.position, base.transform.rotation, null, 1f, -1);
		}
	}

	// Token: 0x06001883 RID: 6275 RVA: 0x000B7584 File Offset: 0x000B5784
	private void RemoveAltarItems()
	{
		foreach (ItemStand itemStand in this.FindItemStands())
		{
			itemStand.DestroyAttachment();
		}
		if (this.m_itemSpawnPoint)
		{
			this.m_fuelAddedEffects.Create(this.m_itemSpawnPoint.position, base.transform.rotation, null, 1f, -1);
		}
	}

	// Token: 0x06001884 RID: 6276 RVA: 0x000B760C File Offset: 0x000B580C
	private void RPC_BossSpawnInitiated(long senderId)
	{
		this.m_interactUser.Message(MessageHud.MessageType.Center, this.m_usedAltarText, 0, null);
	}

	// Token: 0x06001885 RID: 6277 RVA: 0x000B7624 File Offset: 0x000B5824
	private bool CanSpawnBoss(Vector3 point, out Vector3 spawnPoint)
	{
		spawnPoint = Vector3.zero;
		int i = 0;
		while (i < 100)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * this.m_spawnBossMaxDistance;
			spawnPoint = point + new Vector3(vector.x, 0f, vector.y);
			if (this.m_enableSolidHeightCheck)
			{
				float num;
				ZoneSystem.instance.GetSolidHeight(spawnPoint, out num, this.m_getSolidHeightMargin);
				if (num >= 0f && Mathf.Abs(num - base.transform.position.y) <= this.m_spawnBossMaxYDistance && Vector3.Distance(spawnPoint, point) >= this.m_spawnBossMinDistance)
				{
					if (this.m_spawnPointClearingRadius > 0f)
					{
						spawnPoint.y = num + this.m_spawnYOffset;
						int num2 = Physics.OverlapSphereNonAlloc(spawnPoint, this.m_spawnPointClearingRadius, null, this.m_solidRayMask);
						if (num2 > 0)
						{
							ZLog.Log(num2);
							goto IL_FC;
						}
					}
					spawnPoint.y = num + this.m_spawnYOffset;
					return true;
				}
				IL_FC:
				i++;
				continue;
			}
			return true;
		}
		return false;
	}

	// Token: 0x06001886 RID: 6278 RVA: 0x000B773A File Offset: 0x000B593A
	private bool IsBossSpawnQueued()
	{
		return base.IsInvoking("DelayedSpawnBoss");
	}

	// Token: 0x06001887 RID: 6279 RVA: 0x000B7748 File Offset: 0x000B5948
	private void DelayedSpawnBoss()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_bossPrefab, this.m_bossSpawnPoint, Quaternion.identity);
		BaseAI component = gameObject.GetComponent<BaseAI>();
		if (component != null)
		{
			component.SetPatrolPoint();
			if (this.m_alertOnSpawn)
			{
				component.Alert();
			}
		}
		GameObject[] array = this.m_spawnBossDoneffects.Create(this.m_bossSpawnPoint, Quaternion.identity, null, 1f, -1);
		for (int i = 0; i < array.Length; i++)
		{
			IProjectile[] componentsInChildren = array[i].GetComponentsInChildren<IProjectile>();
			if (componentsInChildren.Length != 0)
			{
				IProjectile[] array2 = componentsInChildren;
				for (int j = 0; j < array2.Length; j++)
				{
					array2[j].Setup(gameObject.GetComponent<Character>(), Vector3.zero, -1f, null, null, null);
				}
			}
		}
	}

	// Token: 0x06001888 RID: 6280 RVA: 0x000B7800 File Offset: 0x000B5A00
	private List<ItemStand> FindItemStands()
	{
		List<ItemStand> list = new List<ItemStand>();
		foreach (ItemStand itemStand in UnityEngine.Object.FindObjectsOfType<ItemStand>())
		{
			if (Vector3.Distance(base.transform.position, itemStand.transform.position) <= this.m_itemstandMaxRange && itemStand.gameObject.name.CustomStartsWith(this.m_itemStandPrefix))
			{
				list.Add(itemStand);
			}
		}
		return list;
	}

	// Token: 0x06001889 RID: 6281 RVA: 0x000B7870 File Offset: 0x000B5A70
	private void OnDrawGizmosSelected()
	{
		if (!this.m_renderSpawnAreaGizmos)
		{
			return;
		}
		Gizmos.color = Color.green;
		Utils.DrawGizmoCylinder(this.GetSpawnPosition(), this.m_spawnBossMaxDistance, this.m_spawnBossMaxYDistance, 32);
		Gizmos.color = Color.red;
		if (this.m_spawnBossMinDistance > 0f)
		{
			Utils.DrawGizmoCylinder(this.GetSpawnPosition(), this.m_spawnBossMinDistance, this.m_spawnBossMaxYDistance, 32);
		}
	}

	// Token: 0x04001876 RID: 6262
	[Header("Tokens")]
	public string m_name = "$piece_offerbowl";

	// Token: 0x04001877 RID: 6263
	public string m_useItemText = "$piece_offerbowl_offeritem";

	// Token: 0x04001878 RID: 6264
	public string m_usedAltarText = "$msg_offerdone";

	// Token: 0x04001879 RID: 6265
	public string m_cantOfferText = "$msg_cantoffer";

	// Token: 0x0400187A RID: 6266
	public string m_wrongOfferText = "$msg_offerwrong";

	// Token: 0x0400187B RID: 6267
	public string m_incompleteOfferText = "$msg_incompleteoffering";

	// Token: 0x0400187C RID: 6268
	[Header("Settings")]
	public ItemDrop m_bossItem;

	// Token: 0x0400187D RID: 6269
	public int m_bossItems = 1;

	// Token: 0x0400187E RID: 6270
	public GameObject m_bossPrefab;

	// Token: 0x0400187F RID: 6271
	public ItemDrop m_itemPrefab;

	// Token: 0x04001880 RID: 6272
	public Transform m_itemSpawnPoint;

	// Token: 0x04001881 RID: 6273
	public string m_setGlobalKey = "";

	// Token: 0x04001882 RID: 6274
	public bool m_renderSpawnAreaGizmos;

	// Token: 0x04001883 RID: 6275
	public bool m_alertOnSpawn;

	// Token: 0x04001884 RID: 6276
	[Header("Boss")]
	public float m_spawnBossDelay = 5f;

	// Token: 0x04001885 RID: 6277
	public float m_spawnBossMaxDistance = 40f;

	// Token: 0x04001886 RID: 6278
	public float m_spawnBossMinDistance;

	// Token: 0x04001887 RID: 6279
	public float m_spawnBossMaxYDistance = 9999f;

	// Token: 0x04001888 RID: 6280
	public int m_getSolidHeightMargin = 1000;

	// Token: 0x04001889 RID: 6281
	public bool m_enableSolidHeightCheck = true;

	// Token: 0x0400188A RID: 6282
	public float m_spawnPointClearingRadius;

	// Token: 0x0400188B RID: 6283
	public float m_spawnYOffset = 1f;

	// Token: 0x0400188C RID: 6284
	public Vector3 m_spawnAreaOffset;

	// Token: 0x0400188D RID: 6285
	public List<GameObject> m_spawnPoints = new List<GameObject>();

	// Token: 0x0400188E RID: 6286
	[Header("Use itemstands")]
	public bool m_useItemStands;

	// Token: 0x0400188F RID: 6287
	public string m_itemStandPrefix = "";

	// Token: 0x04001890 RID: 6288
	public float m_itemstandMaxRange = 20f;

	// Token: 0x04001891 RID: 6289
	[Header("Effects")]
	public EffectList m_fuelAddedEffects = new EffectList();

	// Token: 0x04001892 RID: 6290
	public EffectList m_spawnBossStartEffects = new EffectList();

	// Token: 0x04001893 RID: 6291
	public EffectList m_spawnBossDoneffects = new EffectList();

	// Token: 0x04001894 RID: 6292
	private Vector3 m_bossSpawnPoint;

	// Token: 0x04001895 RID: 6293
	private int m_solidRayMask;

	// Token: 0x04001896 RID: 6294
	private static readonly Collider[] s_tempColliders = new Collider[1];

	// Token: 0x04001897 RID: 6295
	private ZNetView m_nview;

	// Token: 0x04001898 RID: 6296
	private Humanoid m_interactUser;

	// Token: 0x04001899 RID: 6297
	private ItemDrop.ItemData m_usedSpawnItem;
}
