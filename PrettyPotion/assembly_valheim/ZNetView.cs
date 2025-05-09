using System;
using System.Collections.Generic;
using System.Reflection;
using SoftReferenceableAssets;
using UnityEngine;

// Token: 0x020000E5 RID: 229
public class ZNetView : MonoBehaviour, IReferenceHolder
{
	// Token: 0x06000E6F RID: 3695 RVA: 0x0006F4E4 File Offset: 0x0006D6E4
	private void Awake()
	{
		if (ZNetView.m_forceDisableInit || ZDOMan.instance == null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		this.m_body = base.GetComponent<Rigidbody>();
		if (ZNetView.m_useInitZDO && ZNetView.m_initZDO == null)
		{
			ZLog.LogWarning("Double ZNetview when initializing object " + base.gameObject.name);
		}
		if (ZNetView.m_initZDO != null)
		{
			this.m_zdo = ZNetView.m_initZDO;
			ZNetView.m_initZDO = null;
			if (this.m_zdo.Type != this.m_type && this.m_zdo.IsOwner())
			{
				this.m_zdo.SetType(this.m_type);
			}
			if (this.m_zdo.Distant != this.m_distant && this.m_zdo.IsOwner())
			{
				this.m_zdo.SetDistant(this.m_distant);
			}
			if (this.m_syncInitialScale)
			{
				Vector3 vec = this.m_zdo.GetVec3(ZDOVars.s_scaleHash, Vector3.zero);
				if (vec != Vector3.zero)
				{
					base.transform.localScale = vec;
				}
				else
				{
					float @float = this.m_zdo.GetFloat(ZDOVars.s_scaleScalarHash, base.transform.localScale.x);
					if (!base.transform.localScale.x.Equals(@float))
					{
						base.transform.localScale = new Vector3(@float, @float, @float);
					}
				}
			}
			if (this.m_body)
			{
				this.m_body.Sleep();
			}
		}
		else
		{
			string prefabName = this.GetPrefabName();
			this.m_zdo = ZDOMan.instance.CreateNewZDO(base.transform.position, prefabName.GetStableHashCode());
			this.m_zdo.Persistent = this.m_persistent;
			this.m_zdo.Type = this.m_type;
			this.m_zdo.Distant = this.m_distant;
			this.m_zdo.SetPrefab(prefabName.GetStableHashCode());
			this.m_zdo.SetRotation(base.transform.rotation);
			if (this.m_syncInitialScale)
			{
				this.SyncScale(true);
			}
			if (ZNetView.m_ghostInit)
			{
				this.m_ghost = true;
				return;
			}
		}
		this.LoadFields();
		ZNetScene.instance.AddInstance(this.m_zdo, this);
	}

	// Token: 0x06000E70 RID: 3696 RVA: 0x0006F71C File Offset: 0x0006D91C
	public void SetLocalScale(Vector3 scale)
	{
		if (base.transform.localScale == scale)
		{
			return;
		}
		base.transform.localScale = scale;
		if (this.m_zdo != null && this.m_syncInitialScale && this.IsOwner())
		{
			this.SyncScale(false);
		}
	}

	// Token: 0x06000E71 RID: 3697 RVA: 0x0006F768 File Offset: 0x0006D968
	private void SyncScale(bool skipOne = false)
	{
		if (!Mathf.Approximately(base.transform.localScale.x, base.transform.localScale.y) || !Mathf.Approximately(base.transform.localScale.x, base.transform.localScale.z))
		{
			this.m_zdo.Set(ZDOVars.s_scaleHash, base.transform.localScale);
			return;
		}
		if (skipOne && Mathf.Approximately(base.transform.localScale.x, 1f))
		{
			return;
		}
		this.m_zdo.Set(ZDOVars.s_scaleScalarHash, base.transform.localScale.x);
	}

	// Token: 0x06000E72 RID: 3698 RVA: 0x0006F820 File Offset: 0x0006DA20
	private void OnDestroy()
	{
		ZNetScene.instance;
		if (this.m_heldReferences != null)
		{
			for (int i = 0; i < this.m_heldReferences.Count; i++)
			{
				this.m_heldReferences[i].Release();
			}
			this.m_heldReferences.Clear();
		}
	}

	// Token: 0x06000E73 RID: 3699 RVA: 0x0006F874 File Offset: 0x0006DA74
	public void LoadFields()
	{
		ZDO zdo = this.GetZDO();
		if (!zdo.GetBool("HasFields", false))
		{
			return;
		}
		base.gameObject.GetComponentsInChildren<MonoBehaviour>(ZNetView.m_tempComponents);
		foreach (MonoBehaviour monoBehaviour in ZNetView.m_tempComponents)
		{
			string text = monoBehaviour.GetType().Name;
			("HasFields" + text).GetStableHashCode();
			if (zdo.GetBool("HasFields" + text, false))
			{
				foreach (FieldInfo fieldInfo in monoBehaviour.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					text = monoBehaviour.GetType().Name + "." + fieldInfo.Name;
					int num;
					float num2;
					bool flag;
					Vector3 vector;
					string value;
					string name;
					string name2;
					if (fieldInfo.FieldType == typeof(int) && zdo.GetInt(text, out num))
					{
						fieldInfo.SetValue(monoBehaviour, num);
					}
					else if (fieldInfo.FieldType == typeof(float) && zdo.GetFloat(text, out num2))
					{
						fieldInfo.SetValue(monoBehaviour, num2);
					}
					else if (fieldInfo.FieldType == typeof(bool) && zdo.GetBool(text, out flag))
					{
						fieldInfo.SetValue(monoBehaviour, flag);
					}
					else if (fieldInfo.FieldType == typeof(Vector3) && zdo.GetVec3(text, out vector))
					{
						fieldInfo.SetValue(monoBehaviour, vector);
					}
					else if (fieldInfo.FieldType == typeof(string) && zdo.GetString(text, out value))
					{
						fieldInfo.SetValue(monoBehaviour, value);
					}
					else if (fieldInfo.FieldType == typeof(GameObject) && zdo.GetString(text, out name))
					{
						GameObject prefab = ZNetScene.instance.GetPrefab(name);
						if (prefab != null)
						{
							fieldInfo.SetValue(monoBehaviour, prefab);
						}
					}
					else if (fieldInfo.FieldType == typeof(ItemDrop) && zdo.GetString(text, out name2))
					{
						GameObject prefab2 = ZNetScene.instance.GetPrefab(name2);
						if (prefab2 != null)
						{
							ItemDrop component = prefab2.GetComponent<ItemDrop>();
							if (component != null)
							{
								fieldInfo.SetValue(monoBehaviour, component);
							}
						}
					}
				}
			}
		}
	}

	// Token: 0x06000E74 RID: 3700 RVA: 0x0006FB14 File Offset: 0x0006DD14
	private string GetPrefabName()
	{
		return global::Utils.GetPrefabName(base.gameObject);
	}

	// Token: 0x06000E75 RID: 3701 RVA: 0x0006FB21 File Offset: 0x0006DD21
	public void Destroy()
	{
		ZNetScene.instance.Destroy(base.gameObject);
	}

	// Token: 0x06000E76 RID: 3702 RVA: 0x0006FB33 File Offset: 0x0006DD33
	public bool IsOwner()
	{
		return this.IsValid() && this.m_zdo.IsOwner();
	}

	// Token: 0x06000E77 RID: 3703 RVA: 0x0006FB4A File Offset: 0x0006DD4A
	public bool HasOwner()
	{
		return this.IsValid() && this.m_zdo.HasOwner();
	}

	// Token: 0x06000E78 RID: 3704 RVA: 0x0006FB61 File Offset: 0x0006DD61
	public void ClaimOwnership()
	{
		if (this.IsOwner())
		{
			return;
		}
		this.m_zdo.SetOwner(ZDOMan.GetSessionID());
	}

	// Token: 0x06000E79 RID: 3705 RVA: 0x0006FB7C File Offset: 0x0006DD7C
	public ZDO GetZDO()
	{
		return this.m_zdo;
	}

	// Token: 0x06000E7A RID: 3706 RVA: 0x0006FB84 File Offset: 0x0006DD84
	public bool IsValid()
	{
		return this.m_zdo != null && this.m_zdo.IsValid();
	}

	// Token: 0x06000E7B RID: 3707 RVA: 0x0006FB9B File Offset: 0x0006DD9B
	public void ResetZDO()
	{
		this.m_zdo.Created = false;
		this.m_zdo = null;
	}

	// Token: 0x06000E7C RID: 3708 RVA: 0x0006FBB0 File Offset: 0x0006DDB0
	public void Register(string name, Action<long> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod(f));
	}

	// Token: 0x06000E7D RID: 3709 RVA: 0x0006FBC9 File Offset: 0x0006DDC9
	public void Register<T>(string name, Action<long, T> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T>(f));
	}

	// Token: 0x06000E7E RID: 3710 RVA: 0x0006FBE2 File Offset: 0x0006DDE2
	public void Register<T, U>(string name, Action<long, T, U> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U>(f));
	}

	// Token: 0x06000E7F RID: 3711 RVA: 0x0006FBFB File Offset: 0x0006DDFB
	public void Register<T, U, V>(string name, Action<long, T, U, V> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V>(f));
	}

	// Token: 0x06000E80 RID: 3712 RVA: 0x0006FC14 File Offset: 0x0006DE14
	public void Register<T, U, V, B>(string name, RoutedMethod<T, U, V, B>.Method f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V, B>(f));
	}

	// Token: 0x06000E81 RID: 3713 RVA: 0x0006FC30 File Offset: 0x0006DE30
	public void Unregister(string name)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
	}

	// Token: 0x06000E82 RID: 3714 RVA: 0x0006FC54 File Offset: 0x0006DE54
	public void HandleRoutedRPC(ZRoutedRpc.RoutedRPCData rpcData)
	{
		RoutedMethodBase routedMethodBase;
		if (this.m_functions.TryGetValue(rpcData.m_methodHash, out routedMethodBase))
		{
			routedMethodBase.Invoke(rpcData.m_senderPeerID, rpcData.m_parameters);
			return;
		}
		ZLog.LogWarning("Failed to find rpc method " + rpcData.m_methodHash.ToString());
	}

	// Token: 0x06000E83 RID: 3715 RVA: 0x0006FCA3 File Offset: 0x0006DEA3
	public void InvokeRPC(long targetID, string method, params object[] parameters)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(targetID, this.m_zdo.m_uid, method, parameters);
	}

	// Token: 0x06000E84 RID: 3716 RVA: 0x0006FCBD File Offset: 0x0006DEBD
	public void InvokeRPC(string method, params object[] parameters)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(this.m_zdo.GetOwner(), this.m_zdo.m_uid, method, parameters);
	}

	// Token: 0x06000E85 RID: 3717 RVA: 0x0006FCE4 File Offset: 0x0006DEE4
	public static object[] Deserialize(long callerID, ParameterInfo[] paramInfo, ZPackage pkg)
	{
		List<object> list = new List<object>();
		list.Add(callerID);
		ZRpc.Deserialize(paramInfo, pkg, ref list);
		return list.ToArray();
	}

	// Token: 0x06000E86 RID: 3718 RVA: 0x0006FD12 File Offset: 0x0006DF12
	public static void StartGhostInit()
	{
		ZNetView.m_ghostInit = true;
	}

	// Token: 0x06000E87 RID: 3719 RVA: 0x0006FD1A File Offset: 0x0006DF1A
	public static void FinishGhostInit()
	{
		ZNetView.m_ghostInit = false;
	}

	// Token: 0x06000E88 RID: 3720 RVA: 0x0006FD22 File Offset: 0x0006DF22
	public void HoldReferenceTo(IReferenceCounted reference)
	{
		if (this.m_heldReferences == null)
		{
			this.m_heldReferences = new List<IReferenceCounted>(1);
		}
		reference.HoldReference();
		this.m_heldReferences.Add(reference);
	}

	// Token: 0x06000E89 RID: 3721 RVA: 0x0006FD4A File Offset: 0x0006DF4A
	public void ReleaseReferenceTo(IReferenceCounted reference)
	{
		if (this.m_heldReferences == null)
		{
			return;
		}
		if (this.m_heldReferences.Remove(reference))
		{
			reference.Release();
		}
	}

	// Token: 0x04000E7E RID: 3710
	public const string CustomFieldsStr = "HasFields";

	// Token: 0x04000E7F RID: 3711
	public static long Everybody = 0L;

	// Token: 0x04000E80 RID: 3712
	public bool m_persistent;

	// Token: 0x04000E81 RID: 3713
	public bool m_distant;

	// Token: 0x04000E82 RID: 3714
	public ZDO.ObjectType m_type;

	// Token: 0x04000E83 RID: 3715
	public bool m_syncInitialScale;

	// Token: 0x04000E84 RID: 3716
	public static bool m_useInitZDO = false;

	// Token: 0x04000E85 RID: 3717
	public static ZDO m_initZDO = null;

	// Token: 0x04000E86 RID: 3718
	public static bool m_forceDisableInit = false;

	// Token: 0x04000E87 RID: 3719
	private ZDO m_zdo;

	// Token: 0x04000E88 RID: 3720
	private Rigidbody m_body;

	// Token: 0x04000E89 RID: 3721
	private Dictionary<int, RoutedMethodBase> m_functions = new Dictionary<int, RoutedMethodBase>();

	// Token: 0x04000E8A RID: 3722
	private bool m_ghost;

	// Token: 0x04000E8B RID: 3723
	private static List<MonoBehaviour> m_tempComponents = new List<MonoBehaviour>();

	// Token: 0x04000E8C RID: 3724
	private static bool m_ghostInit = false;

	// Token: 0x04000E8D RID: 3725
	private List<IReferenceCounted> m_heldReferences;
}
