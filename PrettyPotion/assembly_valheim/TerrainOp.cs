using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001C9 RID: 457
public class TerrainOp : MonoBehaviour
{
	// Token: 0x06001A80 RID: 6784 RVA: 0x000C5468 File Offset: 0x000C3668
	private void Awake()
	{
		if (TerrainOp.m_forceDisableTerrainOps)
		{
			return;
		}
		List<Heightmap> list = new List<Heightmap>();
		Heightmap.FindHeightmap(base.transform.position, this.GetRadius(), list);
		foreach (Heightmap heightmap in list)
		{
			heightmap.GetAndCreateTerrainCompiler().ApplyOperation(this);
		}
		this.OnPlaced();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	// Token: 0x06001A81 RID: 6785 RVA: 0x000C54F0 File Offset: 0x000C36F0
	public float GetRadius()
	{
		return this.m_settings.GetRadius();
	}

	// Token: 0x06001A82 RID: 6786 RVA: 0x000C5500 File Offset: 0x000C3700
	private void OnPlaced()
	{
		this.m_onPlacedEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		if (this.m_spawnOnPlaced)
		{
			if (!this.m_spawnAtMaxLevelDepth && Heightmap.AtMaxLevelDepth(base.transform.position + Vector3.up * this.m_settings.m_levelOffset))
			{
				return;
			}
			if (UnityEngine.Random.value <= this.m_chanceToSpawn)
			{
				Vector3 b = UnityEngine.Random.insideUnitCircle * 0.2f;
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnPlaced, base.transform.position + Vector3.up * 0.5f + b, Quaternion.identity);
				gameObject.GetComponent<ItemDrop>().m_itemData.m_stack = UnityEngine.Random.Range(1, this.m_maxSpawned + 1);
				gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
			}
		}
	}

	// Token: 0x06001A83 RID: 6787 RVA: 0x000C5608 File Offset: 0x000C3808
	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + Vector3.up * this.m_settings.m_levelOffset, Quaternion.identity, new Vector3(1f, 0f, 1f));
		if (this.m_settings.m_level)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_settings.m_levelRadius);
		}
		if (this.m_settings.m_smooth)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_settings.m_smoothRadius);
		}
		if (this.m_settings.m_paintCleared)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_settings.m_paintRadius);
		}
		Gizmos.matrix = Matrix4x4.identity;
	}

	// Token: 0x04001AE6 RID: 6886
	public static bool m_forceDisableTerrainOps;

	// Token: 0x04001AE7 RID: 6887
	public TerrainOp.Settings m_settings = new TerrainOp.Settings();

	// Token: 0x04001AE8 RID: 6888
	[Header("Effects")]
	public EffectList m_onPlacedEffect = new EffectList();

	// Token: 0x04001AE9 RID: 6889
	[Header("Spawn items")]
	public GameObject m_spawnOnPlaced;

	// Token: 0x04001AEA RID: 6890
	public float m_chanceToSpawn = 1f;

	// Token: 0x04001AEB RID: 6891
	public int m_maxSpawned = 1;

	// Token: 0x04001AEC RID: 6892
	public bool m_spawnAtMaxLevelDepth = true;

	// Token: 0x02000389 RID: 905
	[Serializable]
	public class Settings
	{
		// Token: 0x06002310 RID: 8976 RVA: 0x000F10BC File Offset: 0x000EF2BC
		public void Serialize(ZPackage pkg)
		{
			pkg.Write(this.m_levelOffset);
			pkg.Write(this.m_level);
			pkg.Write(this.m_levelRadius);
			pkg.Write(this.m_square);
			pkg.Write(this.m_raise);
			pkg.Write(this.m_raiseRadius);
			pkg.Write(this.m_raisePower);
			pkg.Write(this.m_raiseDelta);
			pkg.Write(this.m_smooth);
			pkg.Write(this.m_smoothRadius);
			pkg.Write(this.m_smoothPower);
			pkg.Write(this.m_paintCleared);
			pkg.Write(this.m_paintHeightCheck);
			pkg.Write((int)this.m_paintType);
			pkg.Write(this.m_paintRadius);
		}

		// Token: 0x06002311 RID: 8977 RVA: 0x000F1180 File Offset: 0x000EF380
		public void Deserialize(ZPackage pkg)
		{
			this.m_levelOffset = pkg.ReadSingle();
			this.m_level = pkg.ReadBool();
			this.m_levelRadius = pkg.ReadSingle();
			this.m_square = pkg.ReadBool();
			this.m_raise = pkg.ReadBool();
			this.m_raiseRadius = pkg.ReadSingle();
			this.m_raisePower = pkg.ReadSingle();
			this.m_raiseDelta = pkg.ReadSingle();
			this.m_smooth = pkg.ReadBool();
			this.m_smoothRadius = pkg.ReadSingle();
			this.m_smoothPower = pkg.ReadSingle();
			this.m_paintCleared = pkg.ReadBool();
			this.m_paintHeightCheck = pkg.ReadBool();
			this.m_paintType = (TerrainModifier.PaintType)pkg.ReadInt();
			this.m_paintRadius = pkg.ReadSingle();
		}

		// Token: 0x06002312 RID: 8978 RVA: 0x000F1244 File Offset: 0x000EF444
		public float GetRadius()
		{
			float num = 0f;
			if (this.m_level && this.m_levelRadius > num)
			{
				num = this.m_levelRadius;
			}
			if (this.m_raise && this.m_raiseRadius > num)
			{
				num = this.m_raiseRadius;
			}
			if (this.m_smooth && this.m_smoothRadius > num)
			{
				num = this.m_smoothRadius;
			}
			if (this.m_paintCleared && this.m_paintRadius > num)
			{
				num = this.m_paintRadius;
			}
			return num;
		}

		// Token: 0x04002685 RID: 9861
		public float m_levelOffset;

		// Token: 0x04002686 RID: 9862
		[Header("Level")]
		public bool m_level;

		// Token: 0x04002687 RID: 9863
		public float m_levelRadius = 2f;

		// Token: 0x04002688 RID: 9864
		public bool m_square = true;

		// Token: 0x04002689 RID: 9865
		[Header("Raise")]
		public bool m_raise;

		// Token: 0x0400268A RID: 9866
		public float m_raiseRadius = 2f;

		// Token: 0x0400268B RID: 9867
		public float m_raisePower;

		// Token: 0x0400268C RID: 9868
		public float m_raiseDelta;

		// Token: 0x0400268D RID: 9869
		[Header("Smooth")]
		public bool m_smooth;

		// Token: 0x0400268E RID: 9870
		public float m_smoothRadius = 2f;

		// Token: 0x0400268F RID: 9871
		public float m_smoothPower = 3f;

		// Token: 0x04002690 RID: 9872
		[Header("Paint")]
		public bool m_paintCleared = true;

		// Token: 0x04002691 RID: 9873
		public bool m_paintHeightCheck;

		// Token: 0x04002692 RID: 9874
		public TerrainModifier.PaintType m_paintType;

		// Token: 0x04002693 RID: 9875
		public float m_paintRadius = 2f;
	}
}
