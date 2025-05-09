using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000DB RID: 219
public class ZNetScene : MonoBehaviour
{
	// Token: 0x1700007D RID: 125
	// (get) Token: 0x06000E36 RID: 3638 RVA: 0x0006E537 File Offset: 0x0006C737
	public static ZNetScene instance
	{
		get
		{
			return ZNetScene.s_instance;
		}
	}

	// Token: 0x06000E37 RID: 3639 RVA: 0x0006E540 File Offset: 0x0006C740
	private void Awake()
	{
		ZNetScene.s_instance = this;
		foreach (GameObject gameObject in this.m_prefabs)
		{
			this.m_namedPrefabs.Add(gameObject.name.GetStableHashCode(), gameObject);
		}
		foreach (GameObject gameObject2 in this.m_nonNetViewPrefabs)
		{
			this.m_namedPrefabs.Add(gameObject2.name.GetStableHashCode(), gameObject2);
		}
		ZDOMan instance = ZDOMan.instance;
		instance.m_onZDODestroyed = (Action<ZDO>)Delegate.Combine(instance.m_onZDODestroyed, new Action<ZDO>(this.OnZDODestroyed));
		ZRoutedRpc.instance.Register<Vector3, Quaternion, int>("SpawnObject", new Action<long, Vector3, Quaternion, int>(this.RPC_SpawnObject));
	}

	// Token: 0x06000E38 RID: 3640 RVA: 0x0006E63C File Offset: 0x0006C83C
	private void OnDestroy()
	{
		ZLog.Log("Net scene destroyed");
		if (ZNetScene.s_instance == this)
		{
			ZNetScene.s_instance = null;
		}
	}

	// Token: 0x06000E39 RID: 3641 RVA: 0x0006E65C File Offset: 0x0006C85C
	public void Shutdown()
	{
		foreach (KeyValuePair<ZDO, ZNetView> keyValuePair in this.m_instances)
		{
			if (keyValuePair.Value)
			{
				keyValuePair.Value.ResetZDO();
				UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
			}
		}
		this.m_instances.Clear();
		base.enabled = false;
	}

	// Token: 0x06000E3A RID: 3642 RVA: 0x0006E6E8 File Offset: 0x0006C8E8
	public void AddInstance(ZDO zdo, ZNetView nview)
	{
		zdo.Created = true;
		this.m_instances[zdo] = nview;
	}

	// Token: 0x06000E3B RID: 3643 RVA: 0x0006E700 File Offset: 0x0006C900
	private bool IsPrefabZDOValid(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		return prefab != 0 && this.GetPrefab(prefab) != null;
	}

	// Token: 0x06000E3C RID: 3644 RVA: 0x0006E728 File Offset: 0x0006C928
	private GameObject CreateObject(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefab == 0)
		{
			return null;
		}
		GameObject prefab2 = this.GetPrefab(prefab);
		if (prefab2 == null)
		{
			return null;
		}
		Vector3 position = zdo.GetPosition();
		Quaternion rotation = zdo.GetRotation();
		ZNetView.m_useInitZDO = true;
		ZNetView.m_initZDO = zdo;
		GameObject result = UnityEngine.Object.Instantiate<GameObject>(prefab2, position, rotation);
		if (ZNetView.m_initZDO != null)
		{
			string str = "ZDO ";
			ZDOID uid = zdo.m_uid;
			ZLog.LogWarning(str + uid.ToString() + " not used when creating object " + prefab2.name);
			ZNetView.m_initZDO = null;
		}
		ZNetView.m_useInitZDO = false;
		return result;
	}

	// Token: 0x06000E3D RID: 3645 RVA: 0x0006E7B8 File Offset: 0x0006C9B8
	public void Destroy(GameObject go)
	{
		ZNetView component = go.GetComponent<ZNetView>();
		if (component && component.GetZDO() != null)
		{
			ZDO zdo = component.GetZDO();
			component.ResetZDO();
			this.m_instances.Remove(zdo);
			if (zdo.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zdo);
			}
		}
		UnityEngine.Object.Destroy(go);
	}

	// Token: 0x06000E3E RID: 3646 RVA: 0x0006E80F File Offset: 0x0006CA0F
	public bool HasPrefab(int hash)
	{
		return this.m_namedPrefabs.ContainsKey(hash);
	}

	// Token: 0x06000E3F RID: 3647 RVA: 0x0006E820 File Offset: 0x0006CA20
	public GameObject GetPrefab(int hash)
	{
		GameObject result;
		if (this.m_namedPrefabs.TryGetValue(hash, out result))
		{
			return result;
		}
		return null;
	}

	// Token: 0x06000E40 RID: 3648 RVA: 0x0006E840 File Offset: 0x0006CA40
	public GameObject GetPrefab(string name)
	{
		return this.GetPrefab(name.GetStableHashCode());
	}

	// Token: 0x06000E41 RID: 3649 RVA: 0x0006E84E File Offset: 0x0006CA4E
	public int GetPrefabHash(GameObject go)
	{
		return go.name.GetStableHashCode();
	}

	// Token: 0x06000E42 RID: 3650 RVA: 0x0006E85C File Offset: 0x0006CA5C
	public bool IsAreaReady(Vector3 point)
	{
		Vector2i zone = ZoneSystem.GetZone(point);
		if (!ZoneSystem.instance.IsZoneLoaded(zone))
		{
			return false;
		}
		this.m_tempCurrentObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, 1, 0, this.m_tempCurrentObjects, null);
		foreach (ZDO zdo in this.m_tempCurrentObjects)
		{
			if (this.IsPrefabZDOValid(zdo) && !this.FindInstance(zdo))
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06000E43 RID: 3651 RVA: 0x0006E8FC File Offset: 0x0006CAFC
	private bool InLoadingScreen()
	{
		return Player.m_localPlayer == null || Player.m_localPlayer.IsTeleporting();
	}

	// Token: 0x06000E44 RID: 3652 RVA: 0x0006E918 File Offset: 0x0006CB18
	private void CreateObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		int maxCreatedPerFrame = 10;
		if (this.InLoadingScreen())
		{
			maxCreatedPerFrame = 100;
		}
		int num = 0;
		this.CreateObjectsSorted(currentNearObjects, maxCreatedPerFrame, ref num);
		this.CreateDistantObjects(currentDistantObjects, maxCreatedPerFrame, ref num);
	}

	// Token: 0x06000E45 RID: 3653 RVA: 0x0006E94C File Offset: 0x0006CB4C
	private void CreateObjectsSorted(List<ZDO> currentNearObjects, int maxCreatedPerFrame, ref int created)
	{
		if (!ZoneSystem.instance.IsActiveAreaLoaded())
		{
			return;
		}
		this.m_tempCurrentObjects2.Clear();
		Vector3 referencePosition = ZNet.instance.GetReferencePosition();
		foreach (ZDO zdo in currentNearObjects)
		{
			if (!zdo.Created)
			{
				zdo.m_tempSortValue = Utils.DistanceSqr(referencePosition, zdo.GetPosition());
				this.m_tempCurrentObjects2.Add(zdo);
			}
		}
		int num = Mathf.Max(this.m_tempCurrentObjects2.Count / 100, maxCreatedPerFrame);
		this.m_tempCurrentObjects2.Sort(new Comparison<ZDO>(ZNetScene.ZDOCompare));
		foreach (ZDO zdo2 in this.m_tempCurrentObjects2)
		{
			if (ZoneSystem.instance.IsZoneReadyForType(zdo2.GetSector(), zdo2.Type))
			{
				if (this.CreateObject(zdo2) != null)
				{
					created++;
					if (created > num)
					{
						break;
					}
				}
				else if (ZNet.instance.IsServer())
				{
					zdo2.SetOwner(ZDOMan.GetSessionID());
					string str = "Destroyed invalid predab ZDO:";
					ZDOID uid = zdo2.m_uid;
					ZLog.Log(str + uid.ToString());
					ZDOMan.instance.DestroyZDO(zdo2);
				}
			}
		}
	}

	// Token: 0x06000E46 RID: 3654 RVA: 0x0006EACC File Offset: 0x0006CCCC
	private static int ZDOCompare(ZDO x, ZDO y)
	{
		if (x.Type == y.Type)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		return ((int)y.Type).CompareTo((int)x.Type);
	}

	// Token: 0x06000E47 RID: 3655 RVA: 0x0006EB10 File Offset: 0x0006CD10
	private void CreateDistantObjects(List<ZDO> objects, int maxCreatedPerFrame, ref int created)
	{
		if (created > maxCreatedPerFrame)
		{
			return;
		}
		foreach (ZDO zdo in objects)
		{
			if (!zdo.Created)
			{
				if (this.CreateObject(zdo) != null)
				{
					created++;
					if (created > maxCreatedPerFrame)
					{
						break;
					}
				}
				else if (ZNet.instance.IsServer())
				{
					zdo.SetOwner(ZDOMan.GetSessionID());
					string str = "Destroyed invalid predab ZDO:";
					ZDOID uid = zdo.m_uid;
					ZLog.Log(str + uid.ToString() + "  prefab hash:" + zdo.GetPrefab().ToString());
					ZDOMan.instance.DestroyZDO(zdo);
				}
			}
		}
	}

	// Token: 0x06000E48 RID: 3656 RVA: 0x0006EBE0 File Offset: 0x0006CDE0
	private void OnZDODestroyed(ZDO zdo)
	{
		ZNetView znetView;
		if (this.m_instances.TryGetValue(zdo, out znetView))
		{
			znetView.ResetZDO();
			UnityEngine.Object.Destroy(znetView.gameObject);
			this.m_instances.Remove(zdo);
		}
	}

	// Token: 0x06000E49 RID: 3657 RVA: 0x0006EC1C File Offset: 0x0006CE1C
	private void RemoveObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		byte b = (byte)(Time.frameCount & 255);
		foreach (ZDO zdo in currentNearObjects)
		{
			zdo.TempRemoveEarmark = b;
		}
		foreach (ZDO zdo2 in currentDistantObjects)
		{
			zdo2.TempRemoveEarmark = b;
		}
		this.m_tempRemoved.Clear();
		foreach (ZNetView znetView in this.m_instances.Values)
		{
			if (znetView.GetZDO().TempRemoveEarmark != b)
			{
				this.m_tempRemoved.Add(znetView);
			}
		}
		for (int i = 0; i < this.m_tempRemoved.Count; i++)
		{
			ZNetView znetView2 = this.m_tempRemoved[i];
			ZDO zdo3 = znetView2.GetZDO();
			znetView2.ResetZDO();
			UnityEngine.Object.Destroy(znetView2.gameObject);
			if (!zdo3.Persistent && zdo3.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zdo3);
			}
			this.m_instances.Remove(zdo3);
		}
	}

	// Token: 0x06000E4A RID: 3658 RVA: 0x0006ED80 File Offset: 0x0006CF80
	public ZNetView FindInstance(ZDO zdo)
	{
		ZNetView result;
		if (this.m_instances.TryGetValue(zdo, out result))
		{
			return result;
		}
		return null;
	}

	// Token: 0x06000E4B RID: 3659 RVA: 0x0006EDA0 File Offset: 0x0006CFA0
	public bool HaveInstance(ZDO zdo)
	{
		return this.m_instances.ContainsKey(zdo);
	}

	// Token: 0x06000E4C RID: 3660 RVA: 0x0006EDB0 File Offset: 0x0006CFB0
	public GameObject FindInstance(ZDOID id)
	{
		ZDO zdo = ZDOMan.instance.GetZDO(id);
		if (zdo != null)
		{
			ZNetView znetView = this.FindInstance(zdo);
			if (znetView)
			{
				return znetView.gameObject;
			}
		}
		return null;
	}

	// Token: 0x06000E4D RID: 3661 RVA: 0x0006EDE4 File Offset: 0x0006CFE4
	private void Update()
	{
		float deltaTime = Time.deltaTime;
		this.m_createDestroyTimer += deltaTime;
		if (this.m_createDestroyTimer >= 0.033333335f)
		{
			this.m_createDestroyTimer = 0f;
			this.CreateDestroyObjects();
		}
	}

	// Token: 0x06000E4E RID: 3662 RVA: 0x0006EE24 File Offset: 0x0006D024
	private void CreateDestroyObjects()
	{
		Vector2i zone = ZoneSystem.GetZone(ZNet.instance.GetReferencePosition());
		this.m_tempCurrentObjects.Clear();
		this.m_tempCurrentDistantObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, this.m_tempCurrentObjects, this.m_tempCurrentDistantObjects);
		this.CreateObjects(this.m_tempCurrentObjects, this.m_tempCurrentDistantObjects);
		this.RemoveObjects(this.m_tempCurrentObjects, this.m_tempCurrentDistantObjects);
	}

	// Token: 0x06000E4F RID: 3663 RVA: 0x0006EEA8 File Offset: 0x0006D0A8
	public static bool InActiveArea(Vector2i zone, Vector3 refPoint)
	{
		Vector2i zone2 = ZoneSystem.GetZone(refPoint);
		return ZNetScene.InActiveArea(zone, zone2);
	}

	// Token: 0x06000E50 RID: 3664 RVA: 0x0006EEC4 File Offset: 0x0006D0C4
	public static bool InActiveArea(Vector2i zone, Vector2i refCenterZone)
	{
		int num = ZoneSystem.instance.m_activeArea - 1;
		return zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num && zone.y >= refCenterZone.y - num;
	}

	// Token: 0x06000E51 RID: 3665 RVA: 0x0006EF24 File Offset: 0x0006D124
	public static bool InActiveArea(Vector2i zone, Vector2i refCenterZone, int activatedArea)
	{
		return zone.x >= refCenterZone.x - activatedArea && zone.x <= refCenterZone.x + activatedArea && zone.y <= refCenterZone.y + activatedArea && zone.y >= refCenterZone.y - activatedArea;
	}

	// Token: 0x06000E52 RID: 3666 RVA: 0x0006EF76 File Offset: 0x0006D176
	public bool OutsideActiveArea(Vector3 point)
	{
		return ZNetScene.OutsideActiveArea(point, ZNet.instance.GetReferencePosition());
	}

	// Token: 0x06000E53 RID: 3667 RVA: 0x0006EF88 File Offset: 0x0006D188
	private static bool OutsideActiveArea(Vector3 point, Vector3 refPoint)
	{
		Vector2i zone = ZoneSystem.GetZone(refPoint);
		Vector2i zone2 = ZoneSystem.GetZone(point);
		return zone2.x <= zone.x - ZoneSystem.instance.m_activeArea || zone2.x >= zone.x + ZoneSystem.instance.m_activeArea || zone2.y >= zone.y + ZoneSystem.instance.m_activeArea || zone2.y <= zone.y - ZoneSystem.instance.m_activeArea;
	}

	// Token: 0x06000E54 RID: 3668 RVA: 0x0006F00C File Offset: 0x0006D20C
	public static bool OutsideActiveArea(Vector3 point, Vector2i centerZone, int activeArea)
	{
		Vector2i zone = ZoneSystem.GetZone(point);
		return zone.x <= centerZone.x - activeArea || zone.x >= centerZone.x + activeArea || zone.y >= centerZone.y + activeArea || zone.y <= centerZone.y - activeArea;
	}

	// Token: 0x06000E55 RID: 3669 RVA: 0x0006F068 File Offset: 0x0006D268
	public bool HaveInstanceInSector(Vector2i sector)
	{
		foreach (KeyValuePair<ZDO, ZNetView> keyValuePair in this.m_instances)
		{
			if (keyValuePair.Value && !keyValuePair.Value.m_distant && ZoneSystem.GetZone(keyValuePair.Value.transform.position) == sector)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000E56 RID: 3670 RVA: 0x0006F0F8 File Offset: 0x0006D2F8
	public int NrOfInstances()
	{
		return this.m_instances.Count;
	}

	// Token: 0x06000E57 RID: 3671 RVA: 0x0006F108 File Offset: 0x0006D308
	public void SpawnObject(Vector3 pos, Quaternion rot, GameObject prefab)
	{
		int prefabHash = this.GetPrefabHash(prefab);
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SpawnObject", new object[]
		{
			pos,
			rot,
			prefabHash
		});
	}

	// Token: 0x06000E58 RID: 3672 RVA: 0x0006F154 File Offset: 0x0006D354
	public List<string> GetPrefabNames()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<int, GameObject> keyValuePair in this.m_namedPrefabs)
		{
			list.Add(keyValuePair.Value.name);
		}
		return list;
	}

	// Token: 0x06000E59 RID: 3673 RVA: 0x0006F1BC File Offset: 0x0006D3BC
	private void RPC_SpawnObject(long spawner, Vector3 pos, Quaternion rot, int prefabHash)
	{
		GameObject prefab = this.GetPrefab(prefabHash);
		if (prefab == null)
		{
			ZLog.Log("Missing prefab " + prefabHash.ToString());
			return;
		}
		UnityEngine.Object.Instantiate<GameObject>(prefab, pos, rot);
	}

	// Token: 0x04000E64 RID: 3684
	private static ZNetScene s_instance;

	// Token: 0x04000E65 RID: 3685
	private const int m_maxCreatedPerFrame = 10;

	// Token: 0x04000E66 RID: 3686
	private const float m_createDestroyFps = 30f;

	// Token: 0x04000E67 RID: 3687
	public List<GameObject> m_prefabs = new List<GameObject>();

	// Token: 0x04000E68 RID: 3688
	public List<GameObject> m_nonNetViewPrefabs = new List<GameObject>();

	// Token: 0x04000E69 RID: 3689
	private readonly Dictionary<int, GameObject> m_namedPrefabs = new Dictionary<int, GameObject>();

	// Token: 0x04000E6A RID: 3690
	private readonly Dictionary<ZDO, ZNetView> m_instances = new Dictionary<ZDO, ZNetView>();

	// Token: 0x04000E6B RID: 3691
	private readonly List<ZDO> m_tempCurrentObjects = new List<ZDO>();

	// Token: 0x04000E6C RID: 3692
	private readonly List<ZDO> m_tempCurrentObjects2 = new List<ZDO>();

	// Token: 0x04000E6D RID: 3693
	private readonly List<ZDO> m_tempCurrentDistantObjects = new List<ZDO>();

	// Token: 0x04000E6E RID: 3694
	private readonly List<ZNetView> m_tempRemoved = new List<ZNetView>();

	// Token: 0x04000E6F RID: 3695
	private float m_createDestroyTimer;
}
