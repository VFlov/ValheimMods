using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

// Token: 0x02000050 RID: 80
[Serializable]
public class EffectList
{
	// Token: 0x06000607 RID: 1543 RVA: 0x00032DE0 File Offset: 0x00030FE0
	public GameObject[] Create(Vector3 basePos, Quaternion baseRot, Transform baseParent = null, float scale = 1f, int variant = -1)
	{
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < this.m_effectPrefabs.Length; i++)
		{
			EffectList.EffectData effectData = this.m_effectPrefabs[i];
			if (effectData.m_enabled && (variant < 0 || effectData.m_variant < 0 || variant == effectData.m_variant))
			{
				Transform transform = baseParent;
				Vector3 position = basePos;
				Quaternion rotation = baseRot;
				if (!string.IsNullOrEmpty(effectData.m_childTransform) && baseParent != null)
				{
					Transform transform2 = Utils.FindChild(transform, effectData.m_childTransform, Utils.IterativeSearchType.DepthFirst);
					if (transform2)
					{
						transform = transform2;
						position = transform.position;
					}
				}
				if (transform && effectData.m_inheritParentRotation)
				{
					rotation = transform.rotation;
				}
				if (effectData.m_randomRotation)
				{
					rotation = UnityEngine.Random.rotation;
				}
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(effectData.m_prefab, position, rotation);
				if (effectData.m_scale)
				{
					if (baseParent && effectData.m_inheritParentScale)
					{
						Vector3 localScale = baseParent.localScale * scale;
						gameObject.transform.localScale = localScale;
					}
					else
					{
						gameObject.transform.localScale = new Vector3(scale, scale, scale);
					}
				}
				else if (baseParent)
				{
					if (effectData.m_multiplyParentVisualScale)
					{
						Transform transform3 = baseParent.Find("Visual");
						if (transform3 != null)
						{
							gameObject.transform.localScale = Vector3.Scale(gameObject.transform.localScale, transform3.localScale);
							goto IL_166;
						}
					}
					if (effectData.m_inheritParentScale)
					{
						gameObject.transform.localScale = baseParent.localScale;
					}
				}
				IL_166:
				if (effectData.m_attach && transform != null)
				{
					gameObject.transform.SetParent(transform);
				}
				if (effectData.m_follow)
				{
					ParentConstraint parentConstraint = gameObject.AddComponent<ParentConstraint>();
					ConstraintSource source = new ConstraintSource
					{
						sourceTransform = transform,
						weight = 1f
					};
					parentConstraint.AddSource(source);
					parentConstraint.locked = true;
					parentConstraint.SetTranslationOffset(0, effectData.m_prefab.transform.position);
					parentConstraint.SetRotationOffset(0, effectData.m_prefab.transform.rotation.eulerAngles);
					parentConstraint.constraintActive = true;
				}
				list.Add(gameObject);
			}
		}
		return list.ToArray();
	}

	// Token: 0x06000608 RID: 1544 RVA: 0x00033010 File Offset: 0x00031210
	public bool HasEffects()
	{
		if (this.m_effectPrefabs == null || this.m_effectPrefabs.Length == 0)
		{
			return false;
		}
		EffectList.EffectData[] effectPrefabs = this.m_effectPrefabs;
		for (int i = 0; i < effectPrefabs.Length; i++)
		{
			if (effectPrefabs[i].m_enabled)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x040006D1 RID: 1745
	public EffectList.EffectData[] m_effectPrefabs = new EffectList.EffectData[0];

	// Token: 0x0200024F RID: 591
	[Serializable]
	public class EffectData
	{
		// Token: 0x04002009 RID: 8201
		public GameObject m_prefab;

		// Token: 0x0400200A RID: 8202
		public bool m_enabled = true;

		// Token: 0x0400200B RID: 8203
		public int m_variant = -1;

		// Token: 0x0400200C RID: 8204
		public bool m_attach;

		// Token: 0x0400200D RID: 8205
		public bool m_follow;

		// Token: 0x0400200E RID: 8206
		public bool m_inheritParentRotation;

		// Token: 0x0400200F RID: 8207
		public bool m_inheritParentScale;

		// Token: 0x04002010 RID: 8208
		public bool m_multiplyParentVisualScale;

		// Token: 0x04002011 RID: 8209
		public bool m_randomRotation;

		// Token: 0x04002012 RID: 8210
		public bool m_scale;

		// Token: 0x04002013 RID: 8211
		public string m_childTransform;
	}
}
