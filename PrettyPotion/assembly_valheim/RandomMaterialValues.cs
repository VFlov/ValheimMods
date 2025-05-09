using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200002E RID: 46
public class RandomMaterialValues : MonoBehaviour
{
	// Token: 0x0600046E RID: 1134 RVA: 0x000287A0 File Offset: 0x000269A0
	private void Start()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		this.m_piece = base.GetComponentInParent<Piece>();
		if (!this.m_nview)
		{
			ZLog.LogError("Missing nview on '" + base.transform.gameObject.name + "'");
		}
		base.InvokeRepeating("CheckMaterial", 0f, 0.2f);
	}

	// Token: 0x0600046F RID: 1135 RVA: 0x0002880C File Offset: 0x00026A0C
	private void CheckMaterial()
	{
		if (((!this.m_isSet && this.m_randomSeed < 0) || (this.m_isSet && (!this.m_piece || !Player.IsPlacementGhost(this.m_piece.gameObject)))) && this.m_nview && this.m_nview.GetZDO() != null)
		{
			this.m_randomSeed = this.m_nview.GetZDO().GetInt(RandomMaterialValues.s_randSeedString, -1);
			if (this.m_randomSeed < 0 && this.m_nview.IsOwner())
			{
				this.m_nview.GetZDO().Set(RandomMaterialValues.s_randSeedString, UnityEngine.Random.Range(0, 12345));
			}
			if (this.m_randomSeed >= 0)
			{
				for (int i = 0; i < this.m_vectorProperties.Count; i++)
				{
					RandomMaterialValues.VectorVariationProperty vectorVariationProperty = this.m_vectorProperties[i];
					foreach (string name in vectorVariationProperty.m_propertyNames)
					{
						MaterialMan.instance.SetValue<Vector4>(base.gameObject, Shader.PropertyToID(name), vectorVariationProperty.GetValue(this.m_randomSeed + i));
					}
				}
				this.m_isSet = true;
			}
		}
		this.m_checks++;
		if (this.m_checks >= 5)
		{
			base.CancelInvoke("CheckMaterial");
		}
	}

	// Token: 0x0400050D RID: 1293
	public List<RandomMaterialValues.VectorVariationProperty> m_vectorProperties = new List<RandomMaterialValues.VectorVariationProperty>();

	// Token: 0x0400050E RID: 1294
	private ZNetView m_nview;

	// Token: 0x0400050F RID: 1295
	private int m_randomSeed = -1;

	// Token: 0x04000510 RID: 1296
	private string m_matName;

	// Token: 0x04000511 RID: 1297
	private bool m_isSet;

	// Token: 0x04000512 RID: 1298
	private Piece m_piece;

	// Token: 0x04000513 RID: 1299
	private int m_checks;

	// Token: 0x04000514 RID: 1300
	private static readonly string s_randSeedString = "RandMatSeed";

	// Token: 0x02000240 RID: 576
	[Serializable]
	public abstract class MaterialVariationProperty<T>
	{
		// Token: 0x06001EE7 RID: 7911
		public abstract T GetValue(int seed);

		// Token: 0x04001FC3 RID: 8131
		public List<string> m_propertyNames;

		// Token: 0x04001FC4 RID: 8132
		protected System.Random m_random;
	}

	// Token: 0x02000241 RID: 577
	[Serializable]
	public class VectorVariationProperty : RandomMaterialValues.MaterialVariationProperty<Vector4>
	{
		// Token: 0x06001EE9 RID: 7913 RVA: 0x000E1AA0 File Offset: 0x000DFCA0
		public override Vector4 GetValue(int seed)
		{
			this.m_random = new System.Random(seed);
			return new Vector4
			{
				x = Mathf.Lerp(this.m_minimum.x, this.m_maximum.x, (float)this.m_random.NextDouble()),
				y = Mathf.Lerp(this.m_minimum.y, this.m_maximum.y, (float)this.m_random.NextDouble()),
				z = Mathf.Lerp(this.m_minimum.z, this.m_maximum.z, (float)this.m_random.NextDouble()),
				w = Mathf.Lerp(this.m_minimum.w, this.m_maximum.w, (float)this.m_random.NextDouble())
			};
		}

		// Token: 0x04001FC5 RID: 8133
		public Vector4 m_minimum;

		// Token: 0x04001FC6 RID: 8134
		public Vector4 m_maximum;
	}
}
