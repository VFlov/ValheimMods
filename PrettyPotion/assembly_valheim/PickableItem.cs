using System;
using UnityEngine;

// Token: 0x020000B2 RID: 178
public class PickableItem : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06000B50 RID: 2896 RVA: 0x0005FF28 File Offset: 0x0005E128
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.SetupRandomPrefab();
		this.m_nview.Register("Pick", new Action<long>(this.RPC_Pick));
		this.SetupItem(true);
	}

	// Token: 0x06000B51 RID: 2897 RVA: 0x0005FF78 File Offset: 0x0005E178
	private void SetupRandomPrefab()
	{
		if (this.m_itemPrefab == null && this.m_randomItemPrefabs.Length != 0)
		{
			int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_itemPrefab, 0);
			if (@int == 0)
			{
				if (this.m_nview.IsOwner())
				{
					PickableItem.RandomItem randomItem = this.m_randomItemPrefabs[UnityEngine.Random.Range(0, this.m_randomItemPrefabs.Length)];
					this.m_itemPrefab = randomItem.m_itemPrefab;
					this.m_stack = Game.instance.ScaleDrops(randomItem.m_itemPrefab.m_itemData, randomItem.m_stackMin, randomItem.m_stackMax + 1);
					int prefabHash = ObjectDB.instance.GetPrefabHash(this.m_itemPrefab.gameObject);
					this.m_nview.GetZDO().Set(ZDOVars.s_itemPrefab, prefabHash, false);
					this.m_nview.GetZDO().Set(ZDOVars.s_itemStack, this.m_stack, false);
					return;
				}
				return;
			}
			else
			{
				GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(@int);
				if (itemPrefab == null)
				{
					ZLog.LogError("Failed to find saved prefab " + @int.ToString() + " in PickableItem " + base.gameObject.name);
					return;
				}
				this.m_itemPrefab = itemPrefab.GetComponent<ItemDrop>();
				this.m_stack = this.m_nview.GetZDO().GetInt(ZDOVars.s_itemStack, 0);
			}
		}
	}

	// Token: 0x06000B52 RID: 2898 RVA: 0x000600CD File Offset: 0x0005E2CD
	public string GetHoverText()
	{
		if (this.m_picked)
		{
			return "";
		}
		return Localization.instance.Localize(this.GetHoverName() + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	// Token: 0x06000B53 RID: 2899 RVA: 0x000600F8 File Offset: 0x0005E2F8
	public string GetHoverName()
	{
		if (!this.m_itemPrefab)
		{
			return "None";
		}
		int stackSize = this.GetStackSize();
		if (stackSize > 1)
		{
			return this.m_itemPrefab.m_itemData.m_shared.m_name + " x " + stackSize.ToString();
		}
		return this.m_itemPrefab.m_itemData.m_shared.m_name;
	}

	// Token: 0x06000B54 RID: 2900 RVA: 0x0006015F File Offset: 0x0005E35F
	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		this.m_nview.InvokeRPC("Pick", Array.Empty<object>());
		return true;
	}

	// Token: 0x06000B55 RID: 2901 RVA: 0x00060186 File Offset: 0x0005E386
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06000B56 RID: 2902 RVA: 0x0006018C File Offset: 0x0005E38C
	private void RPC_Pick(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_picked)
		{
			return;
		}
		this.m_picked = true;
		this.m_pickEffector.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.Drop();
		this.m_nview.Destroy();
	}

	// Token: 0x06000B57 RID: 2903 RVA: 0x000601EC File Offset: 0x0005E3EC
	private void Drop()
	{
		Vector3 position = base.transform.position + Vector3.up * 0.2f;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_itemPrefab.gameObject, position, base.transform.rotation);
		ItemDrop component = gameObject.GetComponent<ItemDrop>();
		if (component != null)
		{
			component.m_itemData.m_stack = this.GetStackSize();
			ItemDrop.OnCreateNew(component);
		}
		gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
	}

	// Token: 0x06000B58 RID: 2904 RVA: 0x00060270 File Offset: 0x0005E470
	private int GetStackSize()
	{
		return Mathf.Clamp((this.m_stack > 0) ? this.m_stack : this.m_itemPrefab.m_itemData.m_stack, 1, (int)Math.Round((double)((float)this.m_itemPrefab.m_itemData.m_shared.m_maxStackSize * Game.m_resourceRate)));
	}

	// Token: 0x06000B59 RID: 2905 RVA: 0x000602C8 File Offset: 0x0005E4C8
	private GameObject GetAttachPrefab()
	{
		Transform transform = this.m_itemPrefab.transform.Find("attach");
		if (transform)
		{
			return transform.gameObject;
		}
		return null;
	}

	// Token: 0x06000B5A RID: 2906 RVA: 0x000602FC File Offset: 0x0005E4FC
	private void SetupItem(bool enabled)
	{
		if (!enabled)
		{
			if (this.m_instance)
			{
				UnityEngine.Object.Destroy(this.m_instance);
				this.m_instance = null;
			}
			return;
		}
		if (this.m_instance)
		{
			return;
		}
		if (this.m_itemPrefab == null)
		{
			return;
		}
		GameObject attachPrefab = this.GetAttachPrefab();
		if (attachPrefab == null)
		{
			ZLog.LogWarning("Failed to get attach prefab for item " + this.m_itemPrefab.name);
			return;
		}
		this.m_instance = UnityEngine.Object.Instantiate<GameObject>(attachPrefab, base.transform.position, base.transform.rotation, base.transform);
		this.m_instance.transform.localPosition = attachPrefab.transform.localPosition;
		this.m_instance.transform.localRotation = attachPrefab.transform.localRotation;
	}

	// Token: 0x06000B5B RID: 2907 RVA: 0x000603D4 File Offset: 0x0005E5D4
	private bool DrawPrefabMesh(ItemDrop prefab)
	{
		if (prefab == null)
		{
			return false;
		}
		bool result = false;
		Gizmos.color = Color.yellow;
		foreach (MeshFilter meshFilter in prefab.gameObject.GetComponentsInChildren<MeshFilter>())
		{
			if (meshFilter && meshFilter.sharedMesh)
			{
				Vector3 position = prefab.transform.position;
				Quaternion lhs = Quaternion.Inverse(prefab.transform.rotation);
				Vector3 point = meshFilter.transform.position - position;
				Vector3 position2 = base.transform.position + base.transform.rotation * point;
				Quaternion rhs = lhs * meshFilter.transform.rotation;
				Quaternion rotation = base.transform.rotation * rhs;
				Gizmos.DrawMesh(meshFilter.sharedMesh, position2, rotation, meshFilter.transform.lossyScale);
				result = true;
			}
		}
		return result;
	}

	// Token: 0x04000C9C RID: 3228
	public ItemDrop m_itemPrefab;

	// Token: 0x04000C9D RID: 3229
	public int m_stack;

	// Token: 0x04000C9E RID: 3230
	public PickableItem.RandomItem[] m_randomItemPrefabs = Array.Empty<PickableItem.RandomItem>();

	// Token: 0x04000C9F RID: 3231
	public EffectList m_pickEffector = new EffectList();

	// Token: 0x04000CA0 RID: 3232
	private ZNetView m_nview;

	// Token: 0x04000CA1 RID: 3233
	private GameObject m_instance;

	// Token: 0x04000CA2 RID: 3234
	private bool m_picked;

	// Token: 0x020002D6 RID: 726
	[Serializable]
	public struct RandomItem
	{
		// Token: 0x0400230D RID: 8973
		public ItemDrop m_itemPrefab;

		// Token: 0x0400230E RID: 8974
		public int m_stackMin;

		// Token: 0x0400230F RID: 8975
		public int m_stackMax;
	}
}
