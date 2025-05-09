using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Token: 0x020000CF RID: 207
public static class ZDOExtraData
{
	// Token: 0x06000CB9 RID: 3257 RVA: 0x00065190 File Offset: 0x00063390
	public static void Init()
	{
		ZDOExtraData.Reset();
		for (int i = 0; i < 256; i++)
		{
			ZDOHelper.s_stripOldData.Add(("room" + i.ToString() + "_seed").GetStableHashCode());
		}
	}

	// Token: 0x06000CBA RID: 3258 RVA: 0x000651D8 File Offset: 0x000633D8
	public static void Reset()
	{
		ZDOExtraData.s_floats.Clear();
		ZDOExtraData.s_vec3.Clear();
		ZDOExtraData.s_quats.Clear();
		ZDOExtraData.s_ints.Clear();
		ZDOExtraData.s_longs.Clear();
		ZDOExtraData.s_strings.Clear();
		ZDOExtraData.s_byteArrays.Clear();
		ZDOExtraData.s_connections.Clear();
		ZDOExtraData.s_connectionsHashData.Clear();
		ZDOExtraData.s_owner.Clear();
		ZDOExtraData.s_tempTimeCreated.Clear();
	}

	// Token: 0x06000CBB RID: 3259 RVA: 0x00065254 File Offset: 0x00063454
	public static void Reserve(ZDOID zid, ZDOExtraData.Type type, int size)
	{
		switch (type)
		{
		case ZDOExtraData.Type.Float:
			ZDOExtraData.s_floats.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.Vec3:
			ZDOExtraData.s_vec3.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.Quat:
			ZDOExtraData.s_quats.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.Int:
			ZDOExtraData.s_ints.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.Long:
			ZDOExtraData.s_longs.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.String:
			ZDOExtraData.s_strings.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.ByteArray:
			ZDOExtraData.s_byteArrays.InitAndReserve(zid, size);
			return;
		default:
			return;
		}
	}

	// Token: 0x06000CBC RID: 3260 RVA: 0x000652DE File Offset: 0x000634DE
	public static void Add(ZDOID zid, int hash, float value)
	{
		ZDOExtraData.s_floats[zid][hash] = value;
	}

	// Token: 0x06000CBD RID: 3261 RVA: 0x000652F2 File Offset: 0x000634F2
	public static void Add(ZDOID zid, int hash, string value)
	{
		ZDOExtraData.s_strings[zid][hash] = value;
	}

	// Token: 0x06000CBE RID: 3262 RVA: 0x00065306 File Offset: 0x00063506
	public static void Add(ZDOID zid, int hash, Vector3 value)
	{
		ZDOExtraData.s_vec3[zid][hash] = value;
	}

	// Token: 0x06000CBF RID: 3263 RVA: 0x0006531A File Offset: 0x0006351A
	public static void Add(ZDOID zid, int hash, Quaternion value)
	{
		ZDOExtraData.s_quats[zid][hash] = value;
	}

	// Token: 0x06000CC0 RID: 3264 RVA: 0x0006532E File Offset: 0x0006352E
	public static void Add(ZDOID zid, int hash, int value)
	{
		ZDOExtraData.s_ints[zid][hash] = value;
	}

	// Token: 0x06000CC1 RID: 3265 RVA: 0x00065342 File Offset: 0x00063542
	public static void Add(ZDOID zid, int hash, long value)
	{
		ZDOExtraData.s_longs[zid][hash] = value;
	}

	// Token: 0x06000CC2 RID: 3266 RVA: 0x00065356 File Offset: 0x00063556
	public static void Add(ZDOID zid, int hash, byte[] value)
	{
		ZDOExtraData.s_byteArrays[zid][hash] = value;
	}

	// Token: 0x06000CC3 RID: 3267 RVA: 0x0006536A File Offset: 0x0006356A
	public static bool Set(ZDOID zid, int hash, float value)
	{
		return ZDOExtraData.s_floats.InitAndSet(zid, hash, value);
	}

	// Token: 0x06000CC4 RID: 3268 RVA: 0x00065379 File Offset: 0x00063579
	public static bool Set(ZDOID zid, int hash, string value)
	{
		return ZDOExtraData.s_strings.InitAndSet(zid, hash, value);
	}

	// Token: 0x06000CC5 RID: 3269 RVA: 0x00065388 File Offset: 0x00063588
	public static bool Set(ZDOID zid, int hash, Vector3 value)
	{
		return ZDOExtraData.s_vec3.InitAndSet(zid, hash, value);
	}

	// Token: 0x06000CC6 RID: 3270 RVA: 0x00065397 File Offset: 0x00063597
	public static bool Update(ZDOID zid, int hash, Vector3 value)
	{
		return ZDOExtraData.s_vec3.Update(zid, hash, value);
	}

	// Token: 0x06000CC7 RID: 3271 RVA: 0x000653A6 File Offset: 0x000635A6
	public static bool Set(ZDOID zid, int hash, Quaternion value)
	{
		return ZDOExtraData.s_quats.InitAndSet(zid, hash, value);
	}

	// Token: 0x06000CC8 RID: 3272 RVA: 0x000653B5 File Offset: 0x000635B5
	public static bool Set(ZDOID zid, int hash, int value)
	{
		return ZDOExtraData.s_ints.InitAndSet(zid, hash, value);
	}

	// Token: 0x06000CC9 RID: 3273 RVA: 0x000653C4 File Offset: 0x000635C4
	public static bool Set(ZDOID zid, int hash, long value)
	{
		return ZDOExtraData.s_longs.InitAndSet(zid, hash, value);
	}

	// Token: 0x06000CCA RID: 3274 RVA: 0x000653D3 File Offset: 0x000635D3
	public static bool Set(ZDOID zid, int hash, byte[] value)
	{
		return ZDOExtraData.s_byteArrays.InitAndSet(zid, hash, value);
	}

	// Token: 0x06000CCB RID: 3275 RVA: 0x000653E4 File Offset: 0x000635E4
	public static bool SetConnection(ZDOID zid, ZDOExtraData.ConnectionType connectionType, ZDOID target)
	{
		ZDOConnection zdoconnection = new ZDOConnection(connectionType, target);
		ZDOConnection zdoconnection2;
		if (ZDOExtraData.s_connections.TryGetValue(zid, out zdoconnection2) && zdoconnection2.m_type == zdoconnection.m_type && zdoconnection2.m_target == zdoconnection.m_target)
		{
			return false;
		}
		ZDOExtraData.s_connections[zid] = zdoconnection;
		return true;
	}

	// Token: 0x06000CCC RID: 3276 RVA: 0x00065438 File Offset: 0x00063638
	public static bool UpdateConnection(ZDOID zid, ZDOExtraData.ConnectionType connectionType, ZDOID target)
	{
		ZDOConnection zdoconnection = new ZDOConnection(connectionType, target);
		ZDOConnection zdoconnection2;
		if (!ZDOExtraData.s_connections.TryGetValue(zid, out zdoconnection2))
		{
			return false;
		}
		if (zdoconnection2.m_type == zdoconnection.m_type && zdoconnection2.m_target == zdoconnection.m_target)
		{
			return false;
		}
		ZDOExtraData.s_connections[zid] = zdoconnection;
		return true;
	}

	// Token: 0x06000CCD RID: 3277 RVA: 0x00065490 File Offset: 0x00063690
	public static void SetConnectionData(ZDOID zid, ZDOExtraData.ConnectionType connectionType, int hash)
	{
		ZDOConnectionHashData value = new ZDOConnectionHashData(connectionType, hash);
		ZDOExtraData.s_connectionsHashData[zid] = value;
	}

	// Token: 0x06000CCE RID: 3278 RVA: 0x000654B1 File Offset: 0x000636B1
	public static void SetOwner(ZDOID zid, ushort ownerKey)
	{
		if (!ZDOExtraData.s_owner.ContainsKey(zid))
		{
			ZDOExtraData.s_owner.Add(zid, ownerKey);
			return;
		}
		if (ownerKey != 0)
		{
			ZDOExtraData.s_owner[zid] = ownerKey;
			return;
		}
		ZDOExtraData.s_owner.Remove(zid);
	}

	// Token: 0x06000CCF RID: 3279 RVA: 0x000654E9 File Offset: 0x000636E9
	public static long GetOwner(ZDOID zid)
	{
		if (!ZDOExtraData.s_owner.ContainsKey(zid))
		{
			return 0L;
		}
		return ZDOID.GetUserID(ZDOExtraData.s_owner[zid]);
	}

	// Token: 0x06000CD0 RID: 3280 RVA: 0x0006550B File Offset: 0x0006370B
	public static bool GetFloat(ZDOID zid, int hash, out float value)
	{
		return ZDOExtraData.s_floats.GetValue(zid, hash, out value);
	}

	// Token: 0x06000CD1 RID: 3281 RVA: 0x0006551A File Offset: 0x0006371A
	public static bool GetVec3(ZDOID zid, int hash, out Vector3 value)
	{
		return ZDOExtraData.s_vec3.GetValue(zid, hash, out value);
	}

	// Token: 0x06000CD2 RID: 3282 RVA: 0x00065529 File Offset: 0x00063729
	public static bool GetQuaternion(ZDOID zid, int hash, out Quaternion value)
	{
		return ZDOExtraData.s_quats.GetValue(zid, hash, out value);
	}

	// Token: 0x06000CD3 RID: 3283 RVA: 0x00065538 File Offset: 0x00063738
	public static bool GetInt(ZDOID zid, int hash, out int value)
	{
		return ZDOExtraData.s_ints.GetValue(zid, hash, out value);
	}

	// Token: 0x06000CD4 RID: 3284 RVA: 0x00065547 File Offset: 0x00063747
	public static bool GetLong(ZDOID zid, int hash, out long value)
	{
		return ZDOExtraData.s_longs.GetValue(zid, hash, out value);
	}

	// Token: 0x06000CD5 RID: 3285 RVA: 0x00065556 File Offset: 0x00063756
	public static bool GetString(ZDOID zid, int hash, out string value)
	{
		return ZDOExtraData.s_strings.GetValue(zid, hash, out value);
	}

	// Token: 0x06000CD6 RID: 3286 RVA: 0x00065565 File Offset: 0x00063765
	public static bool GetByteArray(ZDOID zid, int hash, out byte[] value)
	{
		return ZDOExtraData.s_byteArrays.GetValue(zid, hash, out value);
	}

	// Token: 0x06000CD7 RID: 3287 RVA: 0x00065574 File Offset: 0x00063774
	public static bool GetBool(ZDOID zid, int hash, out bool value)
	{
		int num;
		if (ZDOExtraData.s_ints.GetValue(zid, hash, out num))
		{
			value = (num != 0);
			return true;
		}
		value = false;
		return false;
	}

	// Token: 0x06000CD8 RID: 3288 RVA: 0x0006559D File Offset: 0x0006379D
	public static float GetFloat(ZDOID zid, int hash, float defaultValue = 0f)
	{
		return ZDOExtraData.s_floats.GetValueOrDefault(zid, hash, defaultValue);
	}

	// Token: 0x06000CD9 RID: 3289 RVA: 0x000655AC File Offset: 0x000637AC
	public static Vector3 GetVec3(ZDOID zid, int hash, Vector3 defaultValue)
	{
		return ZDOExtraData.s_vec3.GetValueOrDefault(zid, hash, defaultValue);
	}

	// Token: 0x06000CDA RID: 3290 RVA: 0x000655BB File Offset: 0x000637BB
	public static Quaternion GetQuaternion(ZDOID zid, int hash, Quaternion defaultValue)
	{
		return ZDOExtraData.s_quats.GetValueOrDefault(zid, hash, defaultValue);
	}

	// Token: 0x06000CDB RID: 3291 RVA: 0x000655CA File Offset: 0x000637CA
	public static int GetInt(ZDOID zid, int hash, int defaultValue = 0)
	{
		return ZDOExtraData.s_ints.GetValueOrDefault(zid, hash, defaultValue);
	}

	// Token: 0x06000CDC RID: 3292 RVA: 0x000655D9 File Offset: 0x000637D9
	public static long GetLong(ZDOID zid, int hash, long defaultValue = 0L)
	{
		return ZDOExtraData.s_longs.GetValueOrDefault(zid, hash, defaultValue);
	}

	// Token: 0x06000CDD RID: 3293 RVA: 0x000655E8 File Offset: 0x000637E8
	public static string GetString(ZDOID zid, int hash, string defaultValue = "")
	{
		return ZDOExtraData.s_strings.GetValueOrDefault(zid, hash, defaultValue);
	}

	// Token: 0x06000CDE RID: 3294 RVA: 0x000655F7 File Offset: 0x000637F7
	public static byte[] GetByteArray(ZDOID zid, int hash, byte[] defaultValue = null)
	{
		return ZDOExtraData.s_byteArrays.GetValueOrDefault(zid, hash, defaultValue);
	}

	// Token: 0x06000CDF RID: 3295 RVA: 0x00065606 File Offset: 0x00063806
	public static ZDOConnection GetConnection(ZDOID zid)
	{
		return ZDOExtraData.s_connections.GetValueOrDefaultPiktiv(zid, null);
	}

	// Token: 0x06000CE0 RID: 3296 RVA: 0x00065614 File Offset: 0x00063814
	public static ZDOID GetConnectionZDOID(ZDOID zid, ZDOExtraData.ConnectionType type)
	{
		ZDOConnection valueOrDefaultPiktiv = ZDOExtraData.s_connections.GetValueOrDefaultPiktiv(zid, null);
		if (valueOrDefaultPiktiv != null && valueOrDefaultPiktiv.m_type == type)
		{
			return valueOrDefaultPiktiv.m_target;
		}
		return ZDOID.None;
	}

	// Token: 0x06000CE1 RID: 3297 RVA: 0x00065646 File Offset: 0x00063846
	public static ZDOExtraData.ConnectionType GetConnectionType(ZDOID zid)
	{
		ZDOConnection valueOrDefaultPiktiv = ZDOExtraData.s_connections.GetValueOrDefaultPiktiv(zid, null);
		if (valueOrDefaultPiktiv == null)
		{
			return ZDOExtraData.ConnectionType.None;
		}
		return valueOrDefaultPiktiv.m_type;
	}

	// Token: 0x06000CE2 RID: 3298 RVA: 0x0006565F File Offset: 0x0006385F
	public static List<KeyValuePair<int, float>> GetFloats(ZDOID zid)
	{
		return ZDOExtraData.s_floats.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000CE3 RID: 3299 RVA: 0x0006566C File Offset: 0x0006386C
	public static List<KeyValuePair<int, Vector3>> GetVec3s(ZDOID zid)
	{
		return ZDOExtraData.s_vec3.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000CE4 RID: 3300 RVA: 0x00065679 File Offset: 0x00063879
	public static List<KeyValuePair<int, Quaternion>> GetQuaternions(ZDOID zid)
	{
		return ZDOExtraData.s_quats.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000CE5 RID: 3301 RVA: 0x00065686 File Offset: 0x00063886
	public static List<KeyValuePair<int, int>> GetInts(ZDOID zid)
	{
		return ZDOExtraData.s_ints.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000CE6 RID: 3302 RVA: 0x00065693 File Offset: 0x00063893
	public static List<KeyValuePair<int, long>> GetLongs(ZDOID zid)
	{
		return ZDOExtraData.s_longs.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000CE7 RID: 3303 RVA: 0x000656A0 File Offset: 0x000638A0
	public static List<KeyValuePair<int, string>> GetStrings(ZDOID zid)
	{
		return ZDOExtraData.s_strings.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000CE8 RID: 3304 RVA: 0x000656AD File Offset: 0x000638AD
	public static List<KeyValuePair<int, byte[]>> GetByteArrays(ZDOID zid)
	{
		return ZDOExtraData.s_byteArrays.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000CE9 RID: 3305 RVA: 0x000656BA File Offset: 0x000638BA
	public static bool RemoveFloat(ZDOID zid, int hash)
	{
		return ZDOExtraData.s_floats.Remove(zid, hash);
	}

	// Token: 0x06000CEA RID: 3306 RVA: 0x000656C8 File Offset: 0x000638C8
	public static bool RemoveInt(ZDOID zid, int hash)
	{
		return ZDOExtraData.s_ints.Remove(zid, hash);
	}

	// Token: 0x06000CEB RID: 3307 RVA: 0x000656D6 File Offset: 0x000638D6
	public static bool RemoveLong(ZDOID zid, int hash)
	{
		return ZDOExtraData.s_longs.Remove(zid, hash);
	}

	// Token: 0x06000CEC RID: 3308 RVA: 0x000656E4 File Offset: 0x000638E4
	public static bool RemoveVec3(ZDOID zid, int hash)
	{
		return ZDOExtraData.s_vec3.Remove(zid, hash);
	}

	// Token: 0x06000CED RID: 3309 RVA: 0x000656F2 File Offset: 0x000638F2
	public static bool RemoveQuaternion(ZDOID zid, int hash)
	{
		return ZDOExtraData.s_quats.Remove(zid, hash);
	}

	// Token: 0x06000CEE RID: 3310 RVA: 0x00065700 File Offset: 0x00063900
	public static void RemoveIfEmpty(ZDOID id)
	{
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Float);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Vec3);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Quat);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Int);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Long);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.String);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.ByteArray);
	}

	// Token: 0x06000CEF RID: 3311 RVA: 0x00065734 File Offset: 0x00063934
	public static void RemoveIfEmpty(ZDOID id, ZDOExtraData.Type type)
	{
		switch (type)
		{
		case ZDOExtraData.Type.Float:
			if (ZDOExtraData.s_floats.ContainsKey(id) && ZDOExtraData.s_floats[id].Count == 0)
			{
				ZDOExtraData.ReleaseFloats(id);
				return;
			}
			break;
		case ZDOExtraData.Type.Vec3:
			if (ZDOExtraData.s_vec3.ContainsKey(id) && ZDOExtraData.s_vec3[id].Count == 0)
			{
				ZDOExtraData.ReleaseVec3(id);
				return;
			}
			break;
		case ZDOExtraData.Type.Quat:
			if (ZDOExtraData.s_quats.ContainsKey(id) && ZDOExtraData.s_quats[id].Count == 0)
			{
				ZDOExtraData.ReleaseQuats(id);
				return;
			}
			break;
		case ZDOExtraData.Type.Int:
			if (ZDOExtraData.s_ints.ContainsKey(id) && ZDOExtraData.s_ints[id].Count == 0)
			{
				ZDOExtraData.ReleaseInts(id);
				return;
			}
			break;
		case ZDOExtraData.Type.Long:
			if (ZDOExtraData.s_longs.ContainsKey(id) && ZDOExtraData.s_longs[id].Count == 0)
			{
				ZDOExtraData.ReleaseLongs(id);
				return;
			}
			break;
		case ZDOExtraData.Type.String:
			if (ZDOExtraData.s_strings.ContainsKey(id) && ZDOExtraData.s_strings[id].Count == 0)
			{
				ZDOExtraData.ReleaseStrings(id);
				return;
			}
			break;
		case ZDOExtraData.Type.ByteArray:
			if (ZDOExtraData.s_byteArrays.ContainsKey(id) && ZDOExtraData.s_byteArrays[id].Count == 0)
			{
				ZDOExtraData.ReleaseByteArrays(id);
			}
			break;
		default:
			return;
		}
	}

	// Token: 0x06000CF0 RID: 3312 RVA: 0x00065882 File Offset: 0x00063A82
	public static void Release(ZDO zdo, ZDOID zid)
	{
		ZDOExtraData.ReleaseFloats(zid);
		ZDOExtraData.ReleaseVec3(zid);
		ZDOExtraData.ReleaseQuats(zid);
		ZDOExtraData.ReleaseInts(zid);
		ZDOExtraData.ReleaseLongs(zid);
		ZDOExtraData.ReleaseStrings(zid);
		ZDOExtraData.ReleaseByteArrays(zid);
		ZDOExtraData.ReleaseOwner(zid);
		ZDOExtraData.ReleaseConnection(zid);
	}

	// Token: 0x06000CF1 RID: 3313 RVA: 0x000658BA File Offset: 0x00063ABA
	private static void ReleaseFloats(ZDOID zid)
	{
		ZDOExtraData.s_floats.Release(zid);
	}

	// Token: 0x06000CF2 RID: 3314 RVA: 0x000658C7 File Offset: 0x00063AC7
	private static void ReleaseVec3(ZDOID zid)
	{
		ZDOExtraData.s_vec3.Release(zid);
	}

	// Token: 0x06000CF3 RID: 3315 RVA: 0x000658D4 File Offset: 0x00063AD4
	private static void ReleaseQuats(ZDOID zid)
	{
		ZDOExtraData.s_quats.Release(zid);
	}

	// Token: 0x06000CF4 RID: 3316 RVA: 0x000658E1 File Offset: 0x00063AE1
	private static void ReleaseInts(ZDOID zid)
	{
		ZDOExtraData.s_ints.Release(zid);
	}

	// Token: 0x06000CF5 RID: 3317 RVA: 0x000658EE File Offset: 0x00063AEE
	private static void ReleaseLongs(ZDOID zid)
	{
		ZDOExtraData.s_longs.Release(zid);
	}

	// Token: 0x06000CF6 RID: 3318 RVA: 0x000658FB File Offset: 0x00063AFB
	private static void ReleaseStrings(ZDOID zid)
	{
		ZDOExtraData.s_strings.Release(zid);
	}

	// Token: 0x06000CF7 RID: 3319 RVA: 0x00065908 File Offset: 0x00063B08
	private static void ReleaseByteArrays(ZDOID zid)
	{
		ZDOExtraData.s_byteArrays.Release(zid);
	}

	// Token: 0x06000CF8 RID: 3320 RVA: 0x00065915 File Offset: 0x00063B15
	public static void ReleaseOwner(ZDOID zid)
	{
		ZDOExtraData.s_owner.Remove(zid);
	}

	// Token: 0x06000CF9 RID: 3321 RVA: 0x00065923 File Offset: 0x00063B23
	private static void ReleaseConnection(ZDOID zid)
	{
		ZDOExtraData.s_connections.Remove(zid);
	}

	// Token: 0x06000CFA RID: 3322 RVA: 0x00065931 File Offset: 0x00063B31
	public static void SetTimeCreated(ZDOID zid, long timeCreated)
	{
		ZDOExtraData.s_tempTimeCreated.Add(zid, timeCreated);
	}

	// Token: 0x06000CFB RID: 3323 RVA: 0x00065940 File Offset: 0x00063B40
	public static long GetTimeCreated(ZDOID zid)
	{
		long result;
		if (ZDOExtraData.s_tempTimeCreated.TryGetValue(zid, out result))
		{
			return result;
		}
		return 0L;
	}

	// Token: 0x06000CFC RID: 3324 RVA: 0x00065960 File Offset: 0x00063B60
	public static void ClearTimeCreated()
	{
		ZDOExtraData.s_tempTimeCreated.Clear();
	}

	// Token: 0x06000CFD RID: 3325 RVA: 0x0006596C File Offset: 0x00063B6C
	public static bool HasTimeCreated()
	{
		return ZDOExtraData.s_tempTimeCreated.Count != 0;
	}

	// Token: 0x06000CFE RID: 3326 RVA: 0x0006597B File Offset: 0x00063B7B
	public static List<ZDOID> GetAllZDOIDsWithHash(ZDOExtraData.Type type, int hash)
	{
		if (type == ZDOExtraData.Type.Long)
		{
			return ZDOExtraData.s_longs.GetAllZDOIDsWithHash(hash);
		}
		if (type == ZDOExtraData.Type.Int)
		{
			return ZDOExtraData.s_ints.GetAllZDOIDsWithHash(hash);
		}
		Debug.LogError("This type isn't supported, yet.");
		return Array.Empty<ZDOID>().ToList<ZDOID>();
	}

	// Token: 0x06000CFF RID: 3327 RVA: 0x000659B1 File Offset: 0x00063BB1
	public static List<ZDOID> GetAllConnectionZDOIDs()
	{
		return ZDOExtraData.s_connections.Keys.ToList<ZDOID>();
	}

	// Token: 0x06000D00 RID: 3328 RVA: 0x000659C4 File Offset: 0x00063BC4
	public static List<ZDOID> GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType connectionType)
	{
		List<ZDOID> list = new List<ZDOID>();
		foreach (KeyValuePair<ZDOID, ZDOConnectionHashData> keyValuePair in ZDOExtraData.s_connectionsHashData)
		{
			if (keyValuePair.Value.m_type == connectionType)
			{
				list.Add(keyValuePair.Key);
			}
		}
		return list;
	}

	// Token: 0x06000D01 RID: 3329 RVA: 0x00065A34 File Offset: 0x00063C34
	public static ZDOConnectionHashData GetConnectionHashData(ZDOID zid, ZDOExtraData.ConnectionType type)
	{
		ZDOConnectionHashData valueOrDefaultPiktiv = ZDOExtraData.s_connectionsHashData.GetValueOrDefaultPiktiv(zid, null);
		if (valueOrDefaultPiktiv != null && valueOrDefaultPiktiv.m_type == type)
		{
			return valueOrDefaultPiktiv;
		}
		return null;
	}

	// Token: 0x06000D02 RID: 3330 RVA: 0x00065A60 File Offset: 0x00063C60
	private static int GetUniqueHash(string name)
	{
		int num = ZDOMan.GetSessionID().GetHashCode() + ZDOExtraData.s_uniqueHashes;
		int num2 = 0;
		int num3;
		do
		{
			num2++;
			num3 = (num ^ (name + "_" + num2.ToString()).GetHashCode());
		}
		while (ZDOExtraData.s_usedHashes.Contains(num3));
		ZDOExtraData.s_usedHashes.Add(num3);
		ZDOExtraData.s_uniqueHashes++;
		return num3;
	}

	// Token: 0x06000D03 RID: 3331 RVA: 0x00065AC8 File Offset: 0x00063CC8
	private static void RegenerateConnectionHashData()
	{
		ZDOExtraData.s_usedHashes.Clear();
		ZDOExtraData.s_connectionsHashData.Clear();
		foreach (KeyValuePair<ZDOID, ZDOConnection> keyValuePair in ZDOExtraData.s_connections)
		{
			ZDOExtraData.ConnectionType type = keyValuePair.Value.m_type;
			if (type != ZDOExtraData.ConnectionType.None && (!(keyValuePair.Key == ZDOID.None) || type == ZDOExtraData.ConnectionType.Spawned) && ZDOMan.instance.GetZDO(keyValuePair.Key) != null && (ZDOMan.instance.GetZDO(keyValuePair.Value.m_target) != null || type == ZDOExtraData.ConnectionType.Spawned))
			{
				int uniqueHash = ZDOExtraData.GetUniqueHash(type.ToStringFast());
				ZDOExtraData.s_connectionsHashData[keyValuePair.Key] = new ZDOConnectionHashData(type, uniqueHash);
				if (keyValuePair.Value.m_target != ZDOID.None)
				{
					ZDOExtraData.s_connectionsHashData[keyValuePair.Value.m_target] = new ZDOConnectionHashData(type | ZDOExtraData.ConnectionType.Target, uniqueHash);
				}
			}
		}
	}

	// Token: 0x06000D04 RID: 3332 RVA: 0x00065BE8 File Offset: 0x00063DE8
	public static void PrepareSave()
	{
		ZDOExtraData.RegenerateConnectionHashData();
		ZDOExtraData.s_saveFloats = ZDOExtraData.s_floats.Clone<float>();
		ZDOExtraData.s_saveVec3s = ZDOExtraData.s_vec3.Clone<Vector3>();
		ZDOExtraData.s_saveQuats = ZDOExtraData.s_quats.Clone<Quaternion>();
		ZDOExtraData.s_saveInts = ZDOExtraData.s_ints.Clone<int>();
		ZDOExtraData.s_saveLongs = ZDOExtraData.s_longs.Clone<long>();
		ZDOExtraData.s_saveStrings = ZDOExtraData.s_strings.Clone<string>();
		ZDOExtraData.s_saveByteArrays = ZDOExtraData.s_byteArrays.Clone<byte[]>();
		ZDOExtraData.s_saveConnections = ZDOExtraData.s_connectionsHashData.Clone();
	}

	// Token: 0x06000D05 RID: 3333 RVA: 0x00065C72 File Offset: 0x00063E72
	public static void ClearSave()
	{
		ZDOExtraData.s_saveFloats = null;
		ZDOExtraData.s_saveVec3s = null;
		ZDOExtraData.s_saveQuats = null;
		ZDOExtraData.s_saveInts = null;
		ZDOExtraData.s_saveLongs = null;
		ZDOExtraData.s_saveStrings = null;
		ZDOExtraData.s_saveByteArrays = null;
		ZDOExtraData.s_saveConnections = null;
	}

	// Token: 0x06000D06 RID: 3334 RVA: 0x00065CA4 File Offset: 0x00063EA4
	public static List<KeyValuePair<int, float>> GetSaveFloats(ZDOID zid)
	{
		return ZDOExtraData.s_saveFloats.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000D07 RID: 3335 RVA: 0x00065CB1 File Offset: 0x00063EB1
	public static List<KeyValuePair<int, Vector3>> GetSaveVec3s(ZDOID zid)
	{
		return ZDOExtraData.s_saveVec3s.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000D08 RID: 3336 RVA: 0x00065CBE File Offset: 0x00063EBE
	public static List<KeyValuePair<int, Quaternion>> GetSaveQuaternions(ZDOID zid)
	{
		return ZDOExtraData.s_saveQuats.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000D09 RID: 3337 RVA: 0x00065CCB File Offset: 0x00063ECB
	public static List<KeyValuePair<int, int>> GetSaveInts(ZDOID zid)
	{
		return ZDOExtraData.s_saveInts.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000D0A RID: 3338 RVA: 0x00065CD8 File Offset: 0x00063ED8
	public static List<KeyValuePair<int, long>> GetSaveLongs(ZDOID zid)
	{
		return ZDOExtraData.s_saveLongs.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000D0B RID: 3339 RVA: 0x00065CE5 File Offset: 0x00063EE5
	public static List<KeyValuePair<int, string>> GetSaveStrings(ZDOID zid)
	{
		return ZDOExtraData.s_saveStrings.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000D0C RID: 3340 RVA: 0x00065CF2 File Offset: 0x00063EF2
	public static List<KeyValuePair<int, byte[]>> GetSaveByteArrays(ZDOID zid)
	{
		return ZDOExtraData.s_saveByteArrays.GetValuesOrEmpty(zid);
	}

	// Token: 0x06000D0D RID: 3341 RVA: 0x00065CFF File Offset: 0x00063EFF
	public static ZDOConnectionHashData GetSaveConnections(ZDOID zid)
	{
		return ZDOExtraData.s_saveConnections.GetValueOrDefaultPiktiv(zid, null);
	}

	// Token: 0x04000D2A RID: 3370
	private static readonly Dictionary<ZDOID, long> s_tempTimeCreated = new Dictionary<ZDOID, long>();

	// Token: 0x04000D2B RID: 3371
	private static int s_uniqueHashes = 0;

	// Token: 0x04000D2C RID: 3372
	private static readonly HashSet<int> s_usedHashes = new HashSet<int>();

	// Token: 0x04000D2D RID: 3373
	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, float>> s_floats = new Dictionary<ZDOID, BinarySearchDictionary<int, float>>();

	// Token: 0x04000D2E RID: 3374
	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, Vector3>> s_vec3 = new Dictionary<ZDOID, BinarySearchDictionary<int, Vector3>>();

	// Token: 0x04000D2F RID: 3375
	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, Quaternion>> s_quats = new Dictionary<ZDOID, BinarySearchDictionary<int, Quaternion>>();

	// Token: 0x04000D30 RID: 3376
	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, int>> s_ints = new Dictionary<ZDOID, BinarySearchDictionary<int, int>>();

	// Token: 0x04000D31 RID: 3377
	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, long>> s_longs = new Dictionary<ZDOID, BinarySearchDictionary<int, long>>();

	// Token: 0x04000D32 RID: 3378
	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, string>> s_strings = new Dictionary<ZDOID, BinarySearchDictionary<int, string>>();

	// Token: 0x04000D33 RID: 3379
	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, byte[]>> s_byteArrays = new Dictionary<ZDOID, BinarySearchDictionary<int, byte[]>>();

	// Token: 0x04000D34 RID: 3380
	private static readonly Dictionary<ZDOID, ZDOConnectionHashData> s_connectionsHashData = new Dictionary<ZDOID, ZDOConnectionHashData>();

	// Token: 0x04000D35 RID: 3381
	private static readonly Dictionary<ZDOID, ZDOConnection> s_connections = new Dictionary<ZDOID, ZDOConnection>();

	// Token: 0x04000D36 RID: 3382
	private static readonly Dictionary<ZDOID, ushort> s_owner = new Dictionary<ZDOID, ushort>();

	// Token: 0x04000D37 RID: 3383
	private static Dictionary<ZDOID, BinarySearchDictionary<int, float>> s_saveFloats = null;

	// Token: 0x04000D38 RID: 3384
	private static Dictionary<ZDOID, BinarySearchDictionary<int, Vector3>> s_saveVec3s = null;

	// Token: 0x04000D39 RID: 3385
	private static Dictionary<ZDOID, BinarySearchDictionary<int, Quaternion>> s_saveQuats = null;

	// Token: 0x04000D3A RID: 3386
	private static Dictionary<ZDOID, BinarySearchDictionary<int, int>> s_saveInts = null;

	// Token: 0x04000D3B RID: 3387
	private static Dictionary<ZDOID, BinarySearchDictionary<int, long>> s_saveLongs = null;

	// Token: 0x04000D3C RID: 3388
	private static Dictionary<ZDOID, BinarySearchDictionary<int, string>> s_saveStrings = null;

	// Token: 0x04000D3D RID: 3389
	private static Dictionary<ZDOID, BinarySearchDictionary<int, byte[]>> s_saveByteArrays = null;

	// Token: 0x04000D3E RID: 3390
	private static Dictionary<ZDOID, ZDOConnectionHashData> s_saveConnections = null;

	// Token: 0x020002DC RID: 732
	public enum Type
	{
		// Token: 0x04002326 RID: 8998
		Float,
		// Token: 0x04002327 RID: 8999
		Vec3,
		// Token: 0x04002328 RID: 9000
		Quat,
		// Token: 0x04002329 RID: 9001
		Int,
		// Token: 0x0400232A RID: 9002
		Long,
		// Token: 0x0400232B RID: 9003
		String,
		// Token: 0x0400232C RID: 9004
		ByteArray
	}

	// Token: 0x020002DD RID: 733
	[Flags]
	public enum ConnectionType : byte
	{
		// Token: 0x0400232E RID: 9006
		None = 0,
		// Token: 0x0400232F RID: 9007
		Portal = 1,
		// Token: 0x04002330 RID: 9008
		SyncTransform = 2,
		// Token: 0x04002331 RID: 9009
		Spawned = 3,
		// Token: 0x04002332 RID: 9010
		Target = 16
	}
}
