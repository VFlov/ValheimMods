using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000127 RID: 295
public class MaterialMan : MonoBehaviour
{
	// Token: 0x170000A1 RID: 161
	// (get) Token: 0x060012A9 RID: 4777 RVA: 0x0008B2EC File Offset: 0x000894EC
	public static MaterialMan instance
	{
		get
		{
			return MaterialMan.s_instance;
		}
	}

	// Token: 0x060012AA RID: 4778 RVA: 0x0008B2F3 File Offset: 0x000894F3
	private void Awake()
	{
		if (MaterialMan.s_instance == null)
		{
			MaterialMan.s_instance = this;
			return;
		}
		UnityEngine.Object.Destroy(this);
	}

	// Token: 0x060012AB RID: 4779 RVA: 0x0008B30F File Offset: 0x0008950F
	private void Start()
	{
		this.m_propertyBlock = new MaterialPropertyBlock();
	}

	// Token: 0x060012AC RID: 4780 RVA: 0x0008B31C File Offset: 0x0008951C
	private void Update()
	{
		for (int i = this.m_containersToUpdate.Count - 1; i >= 0; i--)
		{
			this.m_containersToUpdate[i].UpdateBlock();
			this.m_containersToUpdate.RemoveAt(i);
		}
	}

	// Token: 0x060012AD RID: 4781 RVA: 0x0008B360 File Offset: 0x00089560
	private void RegisterRenderers(GameObject gameObject)
	{
		if (this.m_blocks.ContainsKey(gameObject.GetInstanceID()))
		{
			return;
		}
		gameObject.AddComponent<MaterialManNotifier>();
		MaterialMan.PropertyContainer propertyContainer = new MaterialMan.PropertyContainer(gameObject, this.m_propertyBlock);
		MaterialMan.PropertyContainer propertyContainer2 = propertyContainer;
		propertyContainer2.MarkDirty = (Action<MaterialMan.PropertyContainer>)Delegate.Combine(propertyContainer2.MarkDirty, new Action<MaterialMan.PropertyContainer>(this.QueuePropertyUpdate));
		this.m_blocks.Add(gameObject.GetInstanceID(), propertyContainer);
	}

	// Token: 0x060012AE RID: 4782 RVA: 0x0008B3CC File Offset: 0x000895CC
	public void UnregisterRenderers(GameObject gameObject)
	{
		MaterialMan.PropertyContainer propertyContainer;
		if (!this.m_blocks.TryGetValue(gameObject.GetInstanceID(), out propertyContainer))
		{
			ZLog.LogError("Can't unregister renderer for " + gameObject.name);
			return;
		}
		MaterialMan.PropertyContainer propertyContainer2 = propertyContainer;
		propertyContainer2.MarkDirty = (Action<MaterialMan.PropertyContainer>)Delegate.Remove(propertyContainer2.MarkDirty, new Action<MaterialMan.PropertyContainer>(this.QueuePropertyUpdate));
		if (this.m_containersToUpdate.Contains(propertyContainer))
		{
			this.m_containersToUpdate.Remove(propertyContainer);
		}
		this.m_blocks.Remove(gameObject.GetInstanceID());
	}

	// Token: 0x060012AF RID: 4783 RVA: 0x0008B453 File Offset: 0x00089653
	private void QueuePropertyUpdate(MaterialMan.PropertyContainer p)
	{
		if (!this.m_containersToUpdate.Contains(p))
		{
			this.m_containersToUpdate.Add(p);
		}
	}

	// Token: 0x060012B0 RID: 4784 RVA: 0x0008B46F File Offset: 0x0008966F
	public void SetValue<T>(GameObject go, int nameID, T value)
	{
		this.RegisterRenderers(go);
		this.m_blocks[go.GetInstanceID()].SetValue<T>(nameID, value);
	}

	// Token: 0x060012B1 RID: 4785 RVA: 0x0008B490 File Offset: 0x00089690
	public void ResetValue(GameObject go, int nameID)
	{
		if (!this.m_blocks.ContainsKey(go.GetInstanceID()))
		{
			return;
		}
		this.m_blocks[go.GetInstanceID()].ResetValue(nameID);
	}

	// Token: 0x04001267 RID: 4711
	private static MaterialMan s_instance;

	// Token: 0x04001268 RID: 4712
	private Dictionary<int, MaterialMan.PropertyContainer> m_blocks = new Dictionary<int, MaterialMan.PropertyContainer>();

	// Token: 0x04001269 RID: 4713
	private List<MaterialMan.PropertyContainer> m_containersToUpdate = new List<MaterialMan.PropertyContainer>();

	// Token: 0x0400126A RID: 4714
	private MaterialPropertyBlock m_propertyBlock;

	// Token: 0x0200031F RID: 799
	private class PropertyContainer
	{
		// Token: 0x0600222C RID: 8748 RVA: 0x000ECA80 File Offset: 0x000EAC80
		public PropertyContainer(GameObject go, MaterialPropertyBlock block)
		{
			MeshRenderer[] componentsInChildren = go.GetComponentsInChildren<MeshRenderer>(true);
			if (componentsInChildren != null)
			{
				this.m_assignedRenderers.AddRange(componentsInChildren);
			}
			SkinnedMeshRenderer[] componentsInChildren2 = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			if (componentsInChildren2 != null)
			{
				this.m_assignedRenderers.AddRange(componentsInChildren2);
			}
			this.m_propertyBlock = block;
		}

		// Token: 0x0600222D RID: 8749 RVA: 0x000ECAE0 File Offset: 0x000EACE0
		public void UpdateBlock()
		{
			this.m_propertyBlock.Clear();
			foreach (KeyValuePair<int, MaterialMan.ShaderPropertyBase> keyValuePair in this.m_shaderProperties)
			{
				if (keyValuePair.Value.PropertyType == typeof(int))
				{
					this.m_propertyBlock.SetInt(keyValuePair.Key, (keyValuePair.Value as MaterialMan.ShaderProperty<int>).Get());
				}
				else if (keyValuePair.Value.PropertyType == typeof(float))
				{
					this.m_propertyBlock.SetFloat(keyValuePair.Key, (keyValuePair.Value as MaterialMan.ShaderProperty<float>).Get());
				}
				else if (keyValuePair.Value.PropertyType == typeof(float[]))
				{
					this.m_propertyBlock.SetFloatArray(keyValuePair.Key, (keyValuePair.Value as MaterialMan.ShaderProperty<float[]>).Get());
				}
				else if (keyValuePair.Value.PropertyType == typeof(Color))
				{
					this.m_propertyBlock.SetColor(keyValuePair.Key, (keyValuePair.Value as MaterialMan.ShaderProperty<Color>).Get());
				}
				else if (keyValuePair.Value.PropertyType == typeof(Vector4))
				{
					this.m_propertyBlock.SetVector(keyValuePair.Key, (keyValuePair.Value as MaterialMan.ShaderProperty<Vector4>).Get());
				}
				else if (keyValuePair.Value.PropertyType == typeof(Vector4[]))
				{
					this.m_propertyBlock.SetVectorArray(keyValuePair.Key, (keyValuePair.Value as MaterialMan.ShaderProperty<Vector4[]>).Get());
				}
				else if (keyValuePair.Value.PropertyType == typeof(Vector3))
				{
					this.m_propertyBlock.SetVector(keyValuePair.Key, (keyValuePair.Value as MaterialMan.ShaderProperty<Vector3>).Get());
				}
				else if (keyValuePair.Value.PropertyType == typeof(Vector2))
				{
					this.m_propertyBlock.SetVector(keyValuePair.Key, (keyValuePair.Value as MaterialMan.ShaderProperty<Vector2>).Get());
				}
				else if (keyValuePair.Value.PropertyType == typeof(ComputeBuffer))
				{
					this.m_propertyBlock.SetBuffer(keyValuePair.Key, (keyValuePair.Value as MaterialMan.ShaderProperty<ComputeBuffer>).Get());
				}
			}
			for (int i = this.m_assignedRenderers.Count - 1; i >= 0; i--)
			{
				if (this.m_assignedRenderers[i] == null)
				{
					this.m_assignedRenderers.RemoveAt(i);
				}
				else
				{
					this.m_assignedRenderers[i].SetPropertyBlock(this.m_propertyBlock);
				}
			}
		}

		// Token: 0x0600222E RID: 8750 RVA: 0x000ECE0C File Offset: 0x000EB00C
		public void ResetValue(int nameID)
		{
			this.m_shaderProperties.Remove(nameID);
			Action<MaterialMan.PropertyContainer> markDirty = this.MarkDirty;
			if (markDirty == null)
			{
				return;
			}
			markDirty(this);
		}

		// Token: 0x0600222F RID: 8751 RVA: 0x000ECE2C File Offset: 0x000EB02C
		public void SetValue<T>(int nameID, T value)
		{
			if (this.m_shaderProperties.ContainsKey(nameID))
			{
				(this.m_shaderProperties[nameID] as MaterialMan.ShaderProperty<T>).Set(value);
			}
			else
			{
				this.m_shaderProperties.Add(nameID, new MaterialMan.ShaderProperty<T>(nameID, value));
			}
			Action<MaterialMan.PropertyContainer> markDirty = this.MarkDirty;
			if (markDirty == null)
			{
				return;
			}
			markDirty(this);
		}

		// Token: 0x040023EE RID: 9198
		private List<Renderer> m_assignedRenderers = new List<Renderer>();

		// Token: 0x040023EF RID: 9199
		private Dictionary<int, MaterialMan.ShaderPropertyBase> m_shaderProperties = new Dictionary<int, MaterialMan.ShaderPropertyBase>();

		// Token: 0x040023F0 RID: 9200
		private MaterialPropertyBlock m_propertyBlock;

		// Token: 0x040023F1 RID: 9201
		public Action<MaterialMan.PropertyContainer> MarkDirty;
	}

	// Token: 0x02000320 RID: 800
	private abstract class ShaderPropertyBase
	{
		// Token: 0x170001BB RID: 443
		// (get) Token: 0x06002230 RID: 8752
		public abstract Type PropertyType { get; }

		// Token: 0x06002231 RID: 8753 RVA: 0x000ECE84 File Offset: 0x000EB084
		protected ShaderPropertyBase(int nameID)
		{
			this.NameID = nameID;
		}

		// Token: 0x040023F2 RID: 9202
		public readonly int NameID;
	}

	// Token: 0x02000321 RID: 801
	private class ShaderProperty<T> : MaterialMan.ShaderPropertyBase
	{
		// Token: 0x170001BC RID: 444
		// (get) Token: 0x06002232 RID: 8754 RVA: 0x000ECE93 File Offset: 0x000EB093
		public override Type PropertyType
		{
			get
			{
				return typeof(T);
			}
		}

		// Token: 0x06002233 RID: 8755 RVA: 0x000ECE9F File Offset: 0x000EB09F
		public ShaderProperty(int nameID, T value) : base(nameID)
		{
			this._value = value;
		}

		// Token: 0x06002234 RID: 8756 RVA: 0x000ECEAF File Offset: 0x000EB0AF
		public void Set(T value)
		{
			this._value = value;
		}

		// Token: 0x06002235 RID: 8757 RVA: 0x000ECEB8 File Offset: 0x000EB0B8
		public T Get()
		{
			return this._value;
		}

		// Token: 0x040023F3 RID: 9203
		private T _value;
	}
}
