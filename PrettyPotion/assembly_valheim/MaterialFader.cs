using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200005A RID: 90
public class MaterialFader : MonoBehaviour
{
	// Token: 0x06000639 RID: 1593 RVA: 0x00034888 File Offset: 0x00032A88
	private void Awake()
	{
		if (this.m_renderers == null)
		{
			this.m_renderers = new List<Renderer>();
			Renderer[] componentsInChildren = base.GetComponentsInChildren<Renderer>();
			if (componentsInChildren != null)
			{
				this.m_renderers.AddRange(componentsInChildren);
			}
			Renderer component = base.GetComponent<Renderer>();
			if (component != null)
			{
				this.m_renderers.Add(component);
			}
		}
		if (this.m_renderers.Count == 0)
		{
			ZLog.LogError("No MeshRenderer components assigned to MaterialFader!");
		}
		foreach (MaterialFader.FadeProperty fadeProperty in this.m_fadeProperties)
		{
			fadeProperty.Initalize(this.m_renderers[0].material);
		}
		this.m_propertyBlock = new MaterialPropertyBlock();
		if (this.m_renderers[0].HasPropertyBlock())
		{
			this.m_renderers[0].GetPropertyBlock(this.m_propertyBlock);
		}
		this.m_started = this.m_triggerOnAwake;
	}

	// Token: 0x0600063A RID: 1594 RVA: 0x00034988 File Offset: 0x00032B88
	public void TriggerFade()
	{
		if (this.m_started)
		{
			foreach (MaterialFader.FadeProperty fadeProperty in this.m_fadeProperties)
			{
				fadeProperty.Reset();
			}
		}
		this.m_started = true;
	}

	// Token: 0x0600063B RID: 1595 RVA: 0x000349E8 File Offset: 0x00032BE8
	private void Update()
	{
		if (!this.m_started)
		{
			return;
		}
		foreach (MaterialFader.FadeProperty fadeProperty in this.m_fadeProperties)
		{
			fadeProperty.Update(Time.deltaTime, ref this.m_propertyBlock);
		}
		foreach (Renderer renderer in this.m_renderers)
		{
			renderer.SetPropertyBlock(this.m_propertyBlock);
		}
	}

	// Token: 0x04000740 RID: 1856
	public List<Renderer> m_renderers;

	// Token: 0x04000741 RID: 1857
	public bool m_triggerOnAwake = true;

	// Token: 0x04000742 RID: 1858
	public List<MaterialFader.FadeProperty> m_fadeProperties;

	// Token: 0x04000743 RID: 1859
	private MaterialPropertyBlock m_propertyBlock;

	// Token: 0x04000744 RID: 1860
	private bool m_started;

	// Token: 0x02000253 RID: 595
	[Serializable]
	public class FadeProperty
	{
		// Token: 0x06001F05 RID: 7941 RVA: 0x000E1E2B File Offset: 0x000E002B
		public void Initalize(Material mat)
		{
			this.m_shaderID = Shader.PropertyToID(this.m_propertyName);
			this.m_originalMaterial = mat;
		}

		// Token: 0x06001F06 RID: 7942 RVA: 0x000E1E45 File Offset: 0x000E0045
		public void Reset()
		{
			this.m_fadeTimer = 0f;
			this.m_finished = false;
			this.m_startedFade = false;
		}

		// Token: 0x06001F07 RID: 7943 RVA: 0x000E1E60 File Offset: 0x000E0060
		private void GetMaterialValues(MaterialPropertyBlock propertyBlock)
		{
			this.m_startedFade = true;
			if (propertyBlock.HasProperty(this.m_shaderID))
			{
				switch (this.m_propertyType)
				{
				case MaterialFader.PropertyType.Float:
					this.m_startFloatValue = propertyBlock.GetFloat(this.m_shaderID);
					return;
				case MaterialFader.PropertyType.Color:
					this.m_startColorValue = propertyBlock.GetColor(this.m_shaderID);
					return;
				case MaterialFader.PropertyType.SingleVectorChannel:
					this.m_startVectorValue = propertyBlock.GetVector(this.m_shaderID);
					this.m_startFloatValue = this.m_startVectorValue[(int)this.m_vectorChannel];
					return;
				case MaterialFader.PropertyType.Vector3:
					this.m_startVectorValue = propertyBlock.GetVector(this.m_shaderID);
					return;
				default:
					return;
				}
			}
			else
			{
				switch (this.m_propertyType)
				{
				case MaterialFader.PropertyType.Float:
					this.m_startFloatValue = this.m_originalMaterial.GetFloat(this.m_shaderID);
					return;
				case MaterialFader.PropertyType.Color:
					this.m_startColorValue = this.m_originalMaterial.GetColor(this.m_shaderID);
					return;
				case MaterialFader.PropertyType.SingleVectorChannel:
					this.m_startVectorValue = this.m_originalMaterial.GetVector(this.m_shaderID);
					this.m_startFloatValue = this.m_startVectorValue[(int)this.m_vectorChannel];
					return;
				case MaterialFader.PropertyType.Vector3:
					this.m_startVectorValue = this.m_originalMaterial.GetVector(this.m_shaderID);
					return;
				default:
					return;
				}
			}
		}

		// Token: 0x06001F08 RID: 7944 RVA: 0x000E1F9C File Offset: 0x000E019C
		public void Update(float delta, ref MaterialPropertyBlock propertyBlock)
		{
			this.m_fadeTimer += delta;
			if (this.m_finished || this.m_fadeTimer < this.m_delay)
			{
				return;
			}
			if (!this.m_startedFade)
			{
				this.GetMaterialValues(propertyBlock);
			}
			float num = Mathf.Clamp01((this.m_fadeTimer - this.m_delay) / this.m_fadeTime);
			num = this.m_animationCurve.Evaluate(num);
			switch (this.m_propertyType)
			{
			case MaterialFader.PropertyType.Float:
				propertyBlock.SetFloat(this.m_shaderID, Mathf.Lerp(this.m_startFloatValue, this.m_finalFloatValue, num));
				break;
			case MaterialFader.PropertyType.Color:
				propertyBlock.SetColor(this.m_shaderID, Color.Lerp(this.m_startColorValue, this.m_finalColorValue, num));
				break;
			case MaterialFader.PropertyType.SingleVectorChannel:
			{
				Vector4 startVectorValue = this.m_startVectorValue;
				startVectorValue[(int)this.m_vectorChannel] = Mathf.Lerp(this.m_startFloatValue, this.m_finalFloatValue, num);
				propertyBlock.SetVector(this.m_shaderID, startVectorValue);
				break;
			}
			case MaterialFader.PropertyType.Vector3:
				propertyBlock.SetVector(this.m_shaderID, Vector3.Lerp(this.m_startVectorValue, this.m_finalVectorValue, num));
				break;
			}
			if (num == 1f)
			{
				this.m_finished = true;
			}
		}

		// Token: 0x04002020 RID: 8224
		[Header("Settings")]
		public string m_propertyName;

		// Token: 0x04002021 RID: 8225
		public AnimationCurve m_animationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		// Token: 0x04002022 RID: 8226
		[global::Tooltip("Single Vector Channel mode will work on Colors too, if you just want to affect alpha color for example.")]
		public MaterialFader.PropertyType m_propertyType;

		// Token: 0x04002023 RID: 8227
		[Min(0.1f)]
		public float m_fadeTime = 5f;

		// Token: 0x04002024 RID: 8228
		[Min(0f)]
		public float m_delay;

		// Token: 0x04002025 RID: 8229
		[NonSerialized]
		public int m_shaderID;

		// Token: 0x04002026 RID: 8230
		[Header("Values")]
		[global::Tooltip("Used for floats and single vector channels")]
		public float m_finalFloatValue;

		// Token: 0x04002027 RID: 8231
		public Color m_finalColorValue;

		// Token: 0x04002028 RID: 8232
		public Vector3 m_finalVectorValue;

		// Token: 0x04002029 RID: 8233
		[global::Tooltip("Only used for single vector channel mode.")]
		public MaterialFader.VectorChannel m_vectorChannel;

		// Token: 0x0400202A RID: 8234
		[NonSerialized]
		public float m_startFloatValue;

		// Token: 0x0400202B RID: 8235
		[NonSerialized]
		public Color m_startColorValue;

		// Token: 0x0400202C RID: 8236
		[NonSerialized]
		public Vector4 m_startVectorValue;

		// Token: 0x0400202D RID: 8237
		private float m_fadeTimer;

		// Token: 0x0400202E RID: 8238
		private bool m_startedFade;

		// Token: 0x0400202F RID: 8239
		private bool m_finished;

		// Token: 0x04002030 RID: 8240
		private Material m_originalMaterial;
	}

	// Token: 0x02000254 RID: 596
	[Serializable]
	public enum PropertyType
	{
		// Token: 0x04002032 RID: 8242
		Float,
		// Token: 0x04002033 RID: 8243
		Color,
		// Token: 0x04002034 RID: 8244
		SingleVectorChannel,
		// Token: 0x04002035 RID: 8245
		Vector3
	}

	// Token: 0x02000255 RID: 597
	[Serializable]
	public enum VectorChannel
	{
		// Token: 0x04002037 RID: 8247
		X,
		// Token: 0x04002038 RID: 8248
		Y,
		// Token: 0x04002039 RID: 8249
		Z,
		// Token: 0x0400203A RID: 8250
		W
	}
}
