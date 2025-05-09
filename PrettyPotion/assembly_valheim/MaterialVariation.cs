using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000022 RID: 34
public class MaterialVariation : MonoBehaviour
{
	// Token: 0x060002E9 RID: 745 RVA: 0x0001A044 File Offset: 0x00018244
	private void Awake()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		this.m_piece = base.GetComponentInParent<Piece>();
		this.m_renderer = base.GetComponent<SkinnedMeshRenderer>();
		if (!this.m_renderer)
		{
			this.m_renderer = base.GetComponent<MeshRenderer>();
		}
		if (!this.m_nview || !this.m_renderer)
		{
			ZLog.LogError("Missing nview or renderer on '" + base.transform.gameObject.name + "'");
		}
		this.m_nview.Register<int>("RPC_UpdateMaterial", new Action<long, int>(this.RPC_UpdateMaterial));
		base.InvokeRepeating("CheckMaterial", 0f, 0.2f);
	}

	// Token: 0x060002EA RID: 746 RVA: 0x0001A100 File Offset: 0x00018300
	private void CheckMaterial()
	{
		if (((!this.m_isSet && this.m_variation < 0) || (this.m_isSet && this.m_renderer.materials[this.m_materialIndex].name != this.m_matName && (!this.m_piece || !Player.IsPlacementGhost(this.m_piece.gameObject)))) && this.m_nview && this.m_nview.GetZDO() != null && this.m_renderer)
		{
			this.m_variation = this.m_nview.GetZDO().GetInt("MatVar" + this.m_materialIndex.ToString(), -1);
			if (this.m_variation < 0 && this.m_nview.IsOwner())
			{
				this.SetMaterial(this.GetWeightedVariation());
			}
			else if (this.m_variation >= 0)
			{
				this.UpdateMaterial();
			}
		}
		this.m_checks++;
		if (this.m_checks >= 5)
		{
			base.CancelInvoke("CheckMaterial");
		}
	}

	// Token: 0x060002EB RID: 747 RVA: 0x0001A21C File Offset: 0x0001841C
	public void SetMaterial(int index)
	{
		if (this.m_nview && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set("MatVar" + this.m_materialIndex.ToString(), index);
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_UpdateMaterial", new object[]
			{
				index
			});
		}
		this.m_variation = index;
		this.UpdateMaterial();
	}

	// Token: 0x060002EC RID: 748 RVA: 0x0001A29A File Offset: 0x0001849A
	public int GetMaterial()
	{
		return this.m_variation;
	}

	// Token: 0x060002ED RID: 749 RVA: 0x0001A2A2 File Offset: 0x000184A2
	private void RPC_UpdateMaterial(long sender, int index)
	{
		this.m_variation = index;
		this.UpdateMaterial();
	}

	// Token: 0x060002EE RID: 750 RVA: 0x0001A2B4 File Offset: 0x000184B4
	private void UpdateMaterial()
	{
		if (this.m_variation >= 0)
		{
			Material[] materials = this.m_renderer.materials;
			materials[this.m_materialIndex] = this.m_materials[this.m_variation].m_material;
			this.m_renderer.materials = materials;
			this.m_matName = this.m_renderer.materials[this.m_materialIndex].name;
			this.m_isSet = true;
		}
	}

	// Token: 0x060002EF RID: 751 RVA: 0x0001A324 File Offset: 0x00018524
	private int GetWeightedVariation()
	{
		float num = 0f;
		foreach (MaterialVariation.MaterialEntry materialEntry in this.m_materials)
		{
			num += materialEntry.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		for (int i = 0; i < this.m_materials.Count; i++)
		{
			num3 += this.m_materials[i].m_weight;
			if (num2 <= num3)
			{
				return i;
			}
		}
		return 0;
	}

	// Token: 0x040003E5 RID: 997
	public int m_materialIndex;

	// Token: 0x040003E6 RID: 998
	public List<MaterialVariation.MaterialEntry> m_materials = new List<MaterialVariation.MaterialEntry>();

	// Token: 0x040003E7 RID: 999
	private ZNetView m_nview;

	// Token: 0x040003E8 RID: 1000
	private Renderer m_renderer;

	// Token: 0x040003E9 RID: 1001
	private int m_variation = -1;

	// Token: 0x040003EA RID: 1002
	private string m_matName;

	// Token: 0x040003EB RID: 1003
	private bool m_isSet;

	// Token: 0x040003EC RID: 1004
	private Piece m_piece;

	// Token: 0x040003ED RID: 1005
	private int m_checks;

	// Token: 0x02000238 RID: 568
	[Serializable]
	public class MaterialEntry
	{
		// Token: 0x04001F95 RID: 8085
		public Material m_material;

		// Token: 0x04001F96 RID: 8086
		public float m_weight = 1f;
	}
}
