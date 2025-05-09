using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001A3 RID: 419
public class Piece : StaticTarget, IPlaced
{
	// Token: 0x06001893 RID: 6291 RVA: 0x000B7AF0 File Offset: 0x000B5CF0
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		Piece.s_allPieces.Add(this);
		this.m_myListIndex = Piece.s_allPieces.Count - 1;
		if (this.m_comfort > 0)
		{
			Piece.s_allComfortPieces.Add(this);
		}
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.m_creator = this.m_nview.GetZDO().GetLong(ZDOVars.s_creator, 0L);
		}
		if (Piece.s_ghostLayer == 0)
		{
			Piece.s_ghostLayer = LayerMask.NameToLayer("ghost");
		}
		if (Piece.s_pieceRayMask == 0)
		{
			Piece.s_pieceRayMask = LayerMask.GetMask(new string[]
			{
				"piece",
				"piece_nonsolid"
			});
		}
		if (Piece.s_harvestRayMask == 0)
		{
			Piece.s_harvestRayMask = LayerMask.GetMask(new string[]
			{
				"piece",
				"piece_nonsolid",
				"item"
			});
		}
		if (this.m_harvest)
		{
			Transform transform = base.transform.Find("_GhostOnly");
			if (transform != null)
			{
				float skillFactor = Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Farming);
				float num = Mathf.Lerp(this.m_harvestRadius, this.m_harvestRadiusMaxLevel, skillFactor);
				transform.localScale = new Vector3(num, num, num);
			}
		}
	}

	// Token: 0x06001894 RID: 6292 RVA: 0x000B7C28 File Offset: 0x000B5E28
	public void OnPlaced()
	{
		if (this.m_harvest)
		{
			float skillFactor = Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Farming);
			float radius = Mathf.Lerp(this.m_harvestRadius, this.m_harvestRadiusMaxLevel, skillFactor);
			int num = Physics.OverlapSphereNonAlloc(base.transform.position, radius, Piece.s_pieceColliders, Piece.s_harvestRayMask);
			for (int i = 0; i < num; i++)
			{
				Pickable component = Piece.s_pieceColliders[i].gameObject.GetComponent<Pickable>();
				if (component != null && component.m_harvestable && component.CanBePicked())
				{
					component.Interact(Player.m_localPlayer, false, false);
				}
			}
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}

	// Token: 0x06001895 RID: 6293 RVA: 0x000B7CD0 File Offset: 0x000B5ED0
	private void OnDestroy()
	{
		if (this.m_myListIndex >= 0)
		{
			Piece.s_allPieces[this.m_myListIndex] = Piece.s_allPieces[Piece.s_allPieces.Count - 1];
			Piece.s_allPieces[this.m_myListIndex].m_myListIndex = this.m_myListIndex;
			Piece.s_allPieces.RemoveAt(Piece.s_allPieces.Count - 1);
			this.m_myListIndex = -1;
		}
		if (this.m_comfort > 0)
		{
			Piece.s_allComfortPieces.Remove(this);
		}
	}

	// Token: 0x06001896 RID: 6294 RVA: 0x000B7D5C File Offset: 0x000B5F5C
	public bool CanBeRemoved()
	{
		Container componentInChildren = base.GetComponentInChildren<Container>();
		if (componentInChildren != null)
		{
			return componentInChildren.CanBeRemoved();
		}
		Ship componentInChildren2 = base.GetComponentInChildren<Ship>();
		return !(componentInChildren2 != null) || componentInChildren2.CanBeRemoved();
	}

	// Token: 0x06001897 RID: 6295 RVA: 0x000B7D98 File Offset: 0x000B5F98
	public void DropResources(HitData hitData = null)
	{
		if (ZoneSystem.instance.GetGlobalKey(this.FreeBuildKey()))
		{
			return;
		}
		Container container = null;
		Feast component = base.gameObject.GetComponent<Feast>();
		foreach (Piece.Requirement requirement in this.m_resources)
		{
			if (!(requirement.m_resItem == null) && requirement.m_recover)
			{
				GameObject gameObject = ObjectDB.instance.GetItemPrefab(Utils.GetPrefabName(requirement.m_resItem.name));
				int j = requirement.m_amount;
				if (component)
				{
					j = (int)Math.Floor((double)((float)j * component.GetStackPercentige()));
				}
				if (j > 0)
				{
					gameObject = Game.instance.CheckDropConversion(hitData, requirement.m_resItem, gameObject, ref j);
					if (!this.IsPlacedByPlayer())
					{
						j = Mathf.Max(1, j / 3);
					}
					if (this.m_destroyedLootPrefab)
					{
						while (j > 0)
						{
							ItemDrop.ItemData itemData = gameObject.GetComponent<ItemDrop>().m_itemData.Clone();
							itemData.m_dropPrefab = gameObject;
							itemData.m_stack = Mathf.Min(j, itemData.m_shared.m_maxStackSize);
							j -= itemData.m_stack;
							if (container == null || !container.GetInventory().HaveEmptySlot())
							{
								container = UnityEngine.Object.Instantiate<GameObject>(this.m_destroyedLootPrefab, base.transform.position + Vector3.up * this.m_returnResourceHeightOffset, Quaternion.identity).GetComponent<Container>();
							}
							container.GetInventory().AddItem(itemData);
						}
					}
					else
					{
						while (j > 0)
						{
							ItemDrop component2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, base.transform.position + Vector3.up * this.m_returnResourceHeightOffset, Quaternion.identity).GetComponent<ItemDrop>();
							component2.SetStack(Mathf.Min(j, component2.m_itemData.m_shared.m_maxStackSize));
							ItemDrop.OnCreateNew(component2);
							j -= component2.m_itemData.m_stack;
						}
					}
				}
			}
		}
	}

	// Token: 0x06001898 RID: 6296 RVA: 0x000B7FA3 File Offset: 0x000B61A3
	public override bool IsPriorityTarget()
	{
		return base.IsPriorityTarget() && (this.m_targetNonPlayerBuilt || this.IsPlacedByPlayer());
	}

	// Token: 0x06001899 RID: 6297 RVA: 0x000B7FBF File Offset: 0x000B61BF
	public override bool IsRandomTarget()
	{
		return base.IsRandomTarget() && (this.m_targetNonPlayerBuilt || this.IsPlacedByPlayer());
	}

	// Token: 0x0600189A RID: 6298 RVA: 0x000B7FDC File Offset: 0x000B61DC
	public void SetCreator(long uid)
	{
		if (this.m_nview == null)
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			if (this.GetCreator() != 0L)
			{
				return;
			}
			this.m_creator = uid;
			this.m_nview.GetZDO().Set(ZDOVars.s_creator, uid);
		}
	}

	// Token: 0x0600189B RID: 6299 RVA: 0x000B802B File Offset: 0x000B622B
	public long GetCreator()
	{
		return this.m_creator;
	}

	// Token: 0x0600189C RID: 6300 RVA: 0x000B8034 File Offset: 0x000B6234
	public bool IsCreator()
	{
		long creator = this.GetCreator();
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		return creator == playerID;
	}

	// Token: 0x0600189D RID: 6301 RVA: 0x000B805A File Offset: 0x000B625A
	public bool IsPlacedByPlayer()
	{
		return this.GetCreator() != 0L;
	}

	// Token: 0x0600189E RID: 6302 RVA: 0x000B8068 File Offset: 0x000B6268
	public void SetInvalidPlacementHeightlight(bool enabled)
	{
		if (enabled)
		{
			MaterialMan.instance.SetValue<Color>(base.gameObject, ShaderProps._Color, Color.red);
			MaterialMan.instance.SetValue<Color>(base.gameObject, ShaderProps._EmissionColor, Color.red * 0.7f);
			return;
		}
		MaterialMan.instance.ResetValue(base.gameObject, ShaderProps._Color);
		MaterialMan.instance.ResetValue(base.gameObject, ShaderProps._EmissionColor);
	}

	// Token: 0x0600189F RID: 6303 RVA: 0x000B80E4 File Offset: 0x000B62E4
	public static void GetSnapPoints(Vector3 point, float radius, List<Transform> points, List<Piece> pieces)
	{
		int num = Physics.OverlapSphereNonAlloc(point, radius, Piece.s_pieceColliders, Piece.s_pieceRayMask);
		for (int i = 0; i < num; i++)
		{
			Piece componentInParent = Piece.s_pieceColliders[i].GetComponentInParent<Piece>();
			if (componentInParent != null)
			{
				componentInParent.GetSnapPoints(points);
				pieces.Add(componentInParent);
			}
		}
	}

	// Token: 0x060018A0 RID: 6304 RVA: 0x000B8134 File Offset: 0x000B6334
	public static void GetAllPiecesInRadius(Vector3 p, float radius, List<Piece> pieces)
	{
		foreach (Piece piece in Piece.s_allPieces)
		{
			if (piece.gameObject.layer != Piece.s_ghostLayer && Vector3.Distance(p, piece.transform.position) < radius)
			{
				pieces.Add(piece);
			}
		}
	}

	// Token: 0x060018A1 RID: 6305 RVA: 0x000B81AC File Offset: 0x000B63AC
	public static void GetAllComfortPiecesInRadius(Vector3 p, float radius, List<Piece> pieces)
	{
		foreach (Piece piece in Piece.s_allComfortPieces)
		{
			if (piece.gameObject.layer != Piece.s_ghostLayer && Vector3.Distance(p, piece.transform.position) < radius)
			{
				pieces.Add(piece);
			}
		}
	}

	// Token: 0x060018A2 RID: 6306 RVA: 0x000B8224 File Offset: 0x000B6424
	public void GetSnapPoints(List<Transform> points)
	{
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			if (child.CompareTag("snappoint"))
			{
				points.Add(child);
			}
		}
	}

	// Token: 0x060018A3 RID: 6307 RVA: 0x000B8268 File Offset: 0x000B6468
	public GlobalKeys FreeBuildKey()
	{
		if (base.GetComponent<ItemDrop>() != null || base.GetComponent<Feast>() != null)
		{
			return GlobalKeys.NoCraftCost;
		}
		return GlobalKeys.NoBuildCost;
	}

	// Token: 0x060018A4 RID: 6308 RVA: 0x000B828B File Offset: 0x000B648B
	public int GetComfort()
	{
		if (this.m_comfortObject != null && !this.m_comfortObject.activeInHierarchy)
		{
			return 0;
		}
		return this.m_comfort;
	}

	// Token: 0x0400189F RID: 6303
	public bool m_targetNonPlayerBuilt = true;

	// Token: 0x040018A0 RID: 6304
	[Header("Basic stuffs")]
	public Sprite m_icon;

	// Token: 0x040018A1 RID: 6305
	public string m_name = "";

	// Token: 0x040018A2 RID: 6306
	public string m_description = "";

	// Token: 0x040018A3 RID: 6307
	public bool m_enabled = true;

	// Token: 0x040018A4 RID: 6308
	public Piece.PieceCategory m_category;

	// Token: 0x040018A5 RID: 6309
	public bool m_isUpgrade;

	// Token: 0x040018A6 RID: 6310
	[Header("Comfort")]
	public int m_comfort;

	// Token: 0x040018A7 RID: 6311
	public Piece.ComfortGroup m_comfortGroup;

	// Token: 0x040018A8 RID: 6312
	public GameObject m_comfortObject;

	// Token: 0x040018A9 RID: 6313
	[Header("Placement rules")]
	public bool m_groundPiece;

	// Token: 0x040018AA RID: 6314
	public bool m_allowAltGroundPlacement;

	// Token: 0x040018AB RID: 6315
	public bool m_groundOnly;

	// Token: 0x040018AC RID: 6316
	public bool m_cultivatedGroundOnly;

	// Token: 0x040018AD RID: 6317
	public bool m_waterPiece;

	// Token: 0x040018AE RID: 6318
	public bool m_clipGround;

	// Token: 0x040018AF RID: 6319
	public bool m_clipEverything;

	// Token: 0x040018B0 RID: 6320
	public bool m_noInWater;

	// Token: 0x040018B1 RID: 6321
	public bool m_notOnWood;

	// Token: 0x040018B2 RID: 6322
	public bool m_notOnTiltingSurface;

	// Token: 0x040018B3 RID: 6323
	public bool m_inCeilingOnly;

	// Token: 0x040018B4 RID: 6324
	public bool m_notOnFloor;

	// Token: 0x040018B5 RID: 6325
	public bool m_noClipping;

	// Token: 0x040018B6 RID: 6326
	public bool m_onlyInTeleportArea;

	// Token: 0x040018B7 RID: 6327
	public bool m_allowedInDungeons;

	// Token: 0x040018B8 RID: 6328
	public float m_spaceRequirement;

	// Token: 0x040018B9 RID: 6329
	public bool m_repairPiece;

	// Token: 0x040018BA RID: 6330
	public bool m_removePiece;

	// Token: 0x040018BB RID: 6331
	public bool m_canRotate = true;

	// Token: 0x040018BC RID: 6332
	public bool m_randomInitBuildRotation;

	// Token: 0x040018BD RID: 6333
	public bool m_canBeRemoved = true;

	// Token: 0x040018BE RID: 6334
	public bool m_canRockJade;

	// Token: 0x040018BF RID: 6335
	public bool m_allowRotatedOverlap;

	// Token: 0x040018C0 RID: 6336
	public bool m_vegetationGroundOnly;

	// Token: 0x040018C1 RID: 6337
	public List<Piece> m_blockingPieces = new List<Piece>();

	// Token: 0x040018C2 RID: 6338
	public float m_blockRadius;

	// Token: 0x040018C3 RID: 6339
	public ZNetView m_mustConnectTo;

	// Token: 0x040018C4 RID: 6340
	public float m_connectRadius;

	// Token: 0x040018C5 RID: 6341
	public bool m_mustBeAboveConnected;

	// Token: 0x040018C6 RID: 6342
	public bool m_noVines;

	// Token: 0x040018C7 RID: 6343
	public int m_extraPlacementDistance;

	// Token: 0x040018C8 RID: 6344
	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_onlyInBiome;

	// Token: 0x040018C9 RID: 6345
	[Header("Harvest")]
	public bool m_harvest;

	// Token: 0x040018CA RID: 6346
	public float m_harvestRadius;

	// Token: 0x040018CB RID: 6347
	public float m_harvestRadiusMaxLevel;

	// Token: 0x040018CC RID: 6348
	[Header("Effects")]
	public EffectList m_placeEffect = new EffectList();

	// Token: 0x040018CD RID: 6349
	[Header("Requirements")]
	public string m_dlc = "";

	// Token: 0x040018CE RID: 6350
	public CraftingStation m_craftingStation;

	// Token: 0x040018CF RID: 6351
	public float m_returnResourceHeightOffset = 1f;

	// Token: 0x040018D0 RID: 6352
	public Piece.Requirement[] m_resources = Array.Empty<Piece.Requirement>();

	// Token: 0x040018D1 RID: 6353
	public GameObject m_destroyedLootPrefab;

	// Token: 0x040018D2 RID: 6354
	private ZNetView m_nview;

	// Token: 0x040018D3 RID: 6355
	private long m_creator;

	// Token: 0x040018D4 RID: 6356
	private int m_myListIndex = -1;

	// Token: 0x040018D5 RID: 6357
	private static int s_ghostLayer = 0;

	// Token: 0x040018D6 RID: 6358
	private static int s_pieceRayMask = 0;

	// Token: 0x040018D7 RID: 6359
	private static int s_harvestRayMask = 0;

	// Token: 0x040018D8 RID: 6360
	private static readonly Collider[] s_pieceColliders = new Collider[2000];

	// Token: 0x040018D9 RID: 6361
	private static readonly List<Piece> s_allPieces = new List<Piece>();

	// Token: 0x040018DA RID: 6362
	private static readonly HashSet<Piece> s_allComfortPieces = new HashSet<Piece>();

	// Token: 0x02000377 RID: 887
	public enum PieceCategory
	{
		// Token: 0x04002637 RID: 9783
		Misc,
		// Token: 0x04002638 RID: 9784
		Crafting,
		// Token: 0x04002639 RID: 9785
		BuildingWorkbench,
		// Token: 0x0400263A RID: 9786
		BuildingStonecutter,
		// Token: 0x0400263B RID: 9787
		Furniture,
		// Token: 0x0400263C RID: 9788
		Feasts,
		// Token: 0x0400263D RID: 9789
		Food,
		// Token: 0x0400263E RID: 9790
		Meads,
		// Token: 0x0400263F RID: 9791
		Max,
		// Token: 0x04002640 RID: 9792
		All = 100
	}

	// Token: 0x02000378 RID: 888
	public enum ComfortGroup
	{
		// Token: 0x04002642 RID: 9794
		None,
		// Token: 0x04002643 RID: 9795
		Fire,
		// Token: 0x04002644 RID: 9796
		Bed,
		// Token: 0x04002645 RID: 9797
		Banner,
		// Token: 0x04002646 RID: 9798
		Chair,
		// Token: 0x04002647 RID: 9799
		Table,
		// Token: 0x04002648 RID: 9800
		Carpet
	}

	// Token: 0x02000379 RID: 889
	[Serializable]
	public class Requirement
	{
		// Token: 0x060022F0 RID: 8944 RVA: 0x000F0E00 File Offset: 0x000EF000
		public int GetAmount(int qualityLevel)
		{
			if (qualityLevel <= 1)
			{
				return this.m_amount;
			}
			return (qualityLevel - 1) * this.m_amountPerLevel;
		}

		// Token: 0x04002649 RID: 9801
		[Header("Resource")]
		public ItemDrop m_resItem;

		// Token: 0x0400264A RID: 9802
		public int m_amount = 1;

		// Token: 0x0400264B RID: 9803
		public int m_extraAmountOnlyOneIngredient;

		// Token: 0x0400264C RID: 9804
		[Header("Item")]
		public int m_amountPerLevel = 1;

		// Token: 0x0400264D RID: 9805
		[Header("Piece")]
		public bool m_recover = true;
	}
}
