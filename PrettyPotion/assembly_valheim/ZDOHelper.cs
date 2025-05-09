using System;
using System.Collections.Generic;
using System.Linq;

// Token: 0x020000D2 RID: 210
public static class ZDOHelper
{
	// Token: 0x06000D11 RID: 3345 RVA: 0x00065E04 File Offset: 0x00064004
	public static string ToStringFast(this ZDOExtraData.ConnectionType value)
	{
		switch (value & ~ZDOExtraData.ConnectionType.Target)
		{
		case ZDOExtraData.ConnectionType.Portal:
			return "Portal";
		case ZDOExtraData.ConnectionType.SyncTransform:
			return "SyncTransform";
		case ZDOExtraData.ConnectionType.Spawned:
			return "Spawned";
		default:
			return value.ToString();
		}
	}

	// Token: 0x06000D12 RID: 3346 RVA: 0x00065E4E File Offset: 0x0006404E
	public static TValue GetValueOrDefaultPiktiv<TKey, TValue>(this IDictionary<TKey, TValue> container, TKey zid, TValue defaultValue)
	{
		if (!container.ContainsKey(zid))
		{
			return defaultValue;
		}
		return container[zid];
	}

	// Token: 0x06000D13 RID: 3347 RVA: 0x00065E62 File Offset: 0x00064062
	public static bool InitAndSet<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, TType value)
	{
		container.Init(zid);
		return container[zid].SetValue(hash, value);
	}

	// Token: 0x06000D14 RID: 3348 RVA: 0x00065E79 File Offset: 0x00064079
	public static bool Update<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, TType value)
	{
		return container[zid].SetValue(hash, value);
	}

	// Token: 0x06000D15 RID: 3349 RVA: 0x00065E89 File Offset: 0x00064089
	public static void InitAndReserve<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int size)
	{
		container.Init(zid);
		container[zid].Reserve(size);
	}

	// Token: 0x06000D16 RID: 3350 RVA: 0x00065EA0 File Offset: 0x000640A0
	public static List<ZDOID> GetAllZDOIDsWithHash<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, int hash)
	{
		List<ZDOID> list = new List<ZDOID>();
		foreach (KeyValuePair<ZDOID, BinarySearchDictionary<int, TType>> keyValuePair in container)
		{
			foreach (KeyValuePair<int, TType> keyValuePair2 in keyValuePair.Value)
			{
				if (keyValuePair2.Key == hash)
				{
					list.Add(keyValuePair.Key);
					break;
				}
			}
		}
		return list;
	}

	// Token: 0x06000D17 RID: 3351 RVA: 0x00065F40 File Offset: 0x00064140
	public static List<KeyValuePair<int, TType>> GetValuesOrEmpty<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid)
	{
		if (!container.ContainsKey(zid))
		{
			return Array.Empty<KeyValuePair<int, TType>>().ToList<KeyValuePair<int, TType>>();
		}
		return container[zid].ToList<KeyValuePair<int, TType>>();
	}

	// Token: 0x06000D18 RID: 3352 RVA: 0x00065F62 File Offset: 0x00064162
	public static bool GetValue<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, out TType value)
	{
		if (!container.ContainsKey(zid))
		{
			value = default(TType);
			return false;
		}
		return container[zid].TryGetValue(hash, out value);
	}

	// Token: 0x06000D19 RID: 3353 RVA: 0x00065F84 File Offset: 0x00064184
	public static TType GetValueOrDefault<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, TType defaultValue)
	{
		if (!container.ContainsKey(zid))
		{
			return defaultValue;
		}
		return container[zid].GetValueOrDefault(hash, defaultValue);
	}

	// Token: 0x06000D1A RID: 3354 RVA: 0x00065F9F File Offset: 0x0006419F
	public static void Release<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid)
	{
		if (!container.ContainsKey(zid))
		{
			return;
		}
		container[zid].Clear();
		Pool<BinarySearchDictionary<int, TType>>.Release(container[zid]);
		container[zid] = null;
		container.Remove(zid);
	}

	// Token: 0x06000D1B RID: 3355 RVA: 0x00065FD3 File Offset: 0x000641D3
	private static void Init<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid)
	{
		if (!container.ContainsKey(zid))
		{
			container.Add(zid, Pool<BinarySearchDictionary<int, TType>>.Create());
		}
	}

	// Token: 0x06000D1C RID: 3356 RVA: 0x00065FEC File Offset: 0x000641EC
	public static bool Remove<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID id, int hash)
	{
		if (!container.ContainsKey(id) || !container[id].ContainsKey(hash))
		{
			return false;
		}
		container[id].Remove(hash);
		if (container[id].Count == 0)
		{
			Pool<BinarySearchDictionary<int, TType>>.Release(container[id]);
			container[id] = null;
			container.Remove(id);
		}
		return true;
	}

	// Token: 0x06000D1D RID: 3357 RVA: 0x0006604C File Offset: 0x0006424C
	public static Dictionary<ZDOID, BinarySearchDictionary<int, TType>> Clone<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container)
	{
		return container.ToDictionary((KeyValuePair<ZDOID, BinarySearchDictionary<int, TType>> entry) => entry.Key, (KeyValuePair<ZDOID, BinarySearchDictionary<int, TType>> entry) => (BinarySearchDictionary<int, TType>)entry.Value.Clone());
	}

	// Token: 0x06000D1E RID: 3358 RVA: 0x000660A0 File Offset: 0x000642A0
	public static Dictionary<ZDOID, ZDOConnectionHashData> Clone(this Dictionary<ZDOID, ZDOConnectionHashData> container)
	{
		return container.ToDictionary((KeyValuePair<ZDOID, ZDOConnectionHashData> entry) => entry.Key, (KeyValuePair<ZDOID, ZDOConnectionHashData> entry) => entry.Value);
	}

	// Token: 0x04000D43 RID: 3395
	public static readonly HashSet<int> s_stripOldData = new HashSet<int>
	{
		"generated".GetStableHashCode(),
		"patrolSpawnPoint".GetStableHashCode(),
		"autoDespawn".GetStableHashCode(),
		"targetHear".GetStableHashCode(),
		"targetSee".GetStableHashCode(),
		"burnt0".GetStableHashCode(),
		"burnt1".GetStableHashCode(),
		"burnt2".GetStableHashCode(),
		"burnt3".GetStableHashCode(),
		"burnt4".GetStableHashCode(),
		"burnt5".GetStableHashCode(),
		"burnt6".GetStableHashCode(),
		"burnt7".GetStableHashCode(),
		"burnt8".GetStableHashCode(),
		"burnt9".GetStableHashCode(),
		"burnt10".GetStableHashCode(),
		"LookDir".GetStableHashCode(),
		"RideSpeed".GetStableHashCode()
	};

	// Token: 0x04000D44 RID: 3396
	public static readonly List<int> s_stripOldLongData = new List<int>
	{
		ZDOVars.s_zdoidUser.Key,
		ZDOVars.s_zdoidUser.Value,
		ZDOVars.s_zdoidRodOwner.Key,
		ZDOVars.s_zdoidRodOwner.Value,
		ZDOVars.s_sessionCatchID.Key,
		ZDOVars.s_sessionCatchID.Value
	};

	// Token: 0x04000D45 RID: 3397
	public static readonly List<int> s_stripOldDataByteArray = new List<int>
	{
		"health".GetStableHashCode()
	};
}
