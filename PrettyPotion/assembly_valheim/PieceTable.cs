using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000B3 RID: 179
public class PieceTable : MonoBehaviour
{
	// Token: 0x06000B5D RID: 2909 RVA: 0x000604F0 File Offset: 0x0005E6F0
	public void UpdateAvailable(HashSet<string> knownRecipies, Player player, bool hideUnavailable, bool noPlacementCost)
	{
		if (this.m_availablePieces.Count == 0)
		{
			for (int i = 0; i < 8; i++)
			{
				this.m_availablePieces.Add(new List<Piece>());
			}
		}
		foreach (List<Piece> list in this.m_availablePieces)
		{
			list.Clear();
		}
		foreach (GameObject gameObject in this.m_pieces)
		{
			Piece component = gameObject.GetComponent<Piece>();
			bool flag = player.CurrentSeason != null && player.CurrentSeason.Pieces.Contains(gameObject);
			if ((noPlacementCost && !component.m_canRockJade) || (knownRecipies.Contains(component.m_name) && (component.m_enabled || flag) && (!hideUnavailable || player.HaveRequirements(component, Player.RequirementMode.CanAlmostBuild))))
			{
				if (component.m_category == Piece.PieceCategory.All)
				{
					for (int j = 0; j < 8; j++)
					{
						this.m_availablePieces[j].Add(component);
					}
				}
				else
				{
					this.m_availablePieces[(int)component.m_category].Add(component);
				}
			}
		}
	}

	// Token: 0x06000B5E RID: 2910 RVA: 0x00060658 File Offset: 0x0005E858
	public GameObject GetSelectedPrefab()
	{
		Piece selectedPiece = this.GetSelectedPiece();
		if (selectedPiece)
		{
			return selectedPiece.gameObject;
		}
		return null;
	}

	// Token: 0x06000B5F RID: 2911 RVA: 0x0006067C File Offset: 0x0005E87C
	public Piece GetPiece(Piece.PieceCategory category, Vector2Int p)
	{
		if (this.m_availablePieces[(int)category].Count == 0)
		{
			return null;
		}
		int num = p.y * 15 + p.x;
		if (num < 0 || num >= this.m_availablePieces[(int)category].Count)
		{
			return null;
		}
		return this.m_availablePieces[(int)category][num];
	}

	// Token: 0x06000B60 RID: 2912 RVA: 0x000606DD File Offset: 0x0005E8DD
	public Piece GetPiece(Vector2Int p)
	{
		return this.GetPiece(this.GetSelectedCategory(), p);
	}

	// Token: 0x06000B61 RID: 2913 RVA: 0x000606EC File Offset: 0x0005E8EC
	public bool IsPieceAvailable(Piece piece)
	{
		using (List<Piece>.Enumerator enumerator = this.m_availablePieces[(int)this.GetSelectedCategory()].GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current == piece)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06000B62 RID: 2914 RVA: 0x00060754 File Offset: 0x0005E954
	public Piece.PieceCategory GetSelectedCategory()
	{
		if (this.m_selectedCategory == Piece.PieceCategory.Max)
		{
			if (this.m_categories.Count == 0)
			{
				return Piece.PieceCategory.Misc;
			}
			this.m_selectedCategory = this.m_categories[0];
		}
		return this.m_selectedCategory;
	}

	// Token: 0x06000B63 RID: 2915 RVA: 0x00060788 File Offset: 0x0005E988
	public Piece GetSelectedPiece()
	{
		Vector2Int selectedIndex = this.GetSelectedIndex();
		return this.GetPiece(this.GetSelectedCategory(), selectedIndex);
	}

	// Token: 0x06000B64 RID: 2916 RVA: 0x000607A9 File Offset: 0x0005E9A9
	public int GetAvailablePiecesInCategory(Piece.PieceCategory cat)
	{
		return this.m_availablePieces[(int)cat].Count;
	}

	// Token: 0x06000B65 RID: 2917 RVA: 0x000607BC File Offset: 0x0005E9BC
	public List<Piece> GetPiecesInSelectedCategory()
	{
		return this.m_availablePieces[(int)this.GetSelectedCategory()];
	}

	// Token: 0x06000B66 RID: 2918 RVA: 0x000607CF File Offset: 0x0005E9CF
	public int GetAvailablePiecesInSelectedCategory()
	{
		return this.GetAvailablePiecesInCategory(this.GetSelectedCategory());
	}

	// Token: 0x06000B67 RID: 2919 RVA: 0x000607DD File Offset: 0x0005E9DD
	public Vector2Int GetSelectedIndex()
	{
		return this.m_selectedPiece[Mathf.Min((int)this.GetSelectedCategory(), this.m_selectedPiece.Length - 1)];
	}

	// Token: 0x06000B68 RID: 2920 RVA: 0x00060800 File Offset: 0x0005EA00
	public bool GetPieceIndex(Piece p, out Vector2Int index, out int category)
	{
		string prefabName = Utils.GetPrefabName(p.gameObject);
		for (int i = 0; i < this.m_availablePieces.Count; i++)
		{
			int j = 0;
			while (j < this.m_availablePieces[i].Count)
			{
				Piece piece = this.m_availablePieces[i][j];
				if (Utils.GetPrefabName(piece.gameObject) == prefabName)
				{
					category = -1;
					for (int k = 0; k < this.m_categories.Count; k++)
					{
						if (piece.m_category == this.m_categories[k])
						{
							category = k;
							break;
						}
					}
					if (category >= 0)
					{
						index = new Vector2Int(j % 15, (j - j % 15) / 15);
						return true;
					}
					index = Vector2Int.zero;
					return false;
				}
				else
				{
					j++;
				}
			}
		}
		index = Vector2Int.zero;
		category = -1;
		return false;
	}

	// Token: 0x06000B69 RID: 2921 RVA: 0x000608F0 File Offset: 0x0005EAF0
	public void SetSelected(Vector2Int p)
	{
		this.m_selectedPiece[(int)this.GetSelectedCategory()] = p;
	}

	// Token: 0x06000B6A RID: 2922 RVA: 0x00060904 File Offset: 0x0005EB04
	public void LeftPiece()
	{
		if (this.m_availablePieces[(int)this.GetSelectedCategory()].Count <= 1)
		{
			return;
		}
		Vector2Int vector2Int = this.m_selectedPiece[(int)this.GetSelectedCategory()];
		int x = vector2Int.x - 1;
		vector2Int.x = x;
		if (vector2Int.x < 0)
		{
			vector2Int.x = 14;
		}
		this.m_selectedPiece[(int)this.GetSelectedCategory()] = vector2Int;
	}

	// Token: 0x06000B6B RID: 2923 RVA: 0x00060974 File Offset: 0x0005EB74
	public void RightPiece()
	{
		if (this.m_availablePieces[(int)this.GetSelectedCategory()].Count <= 1)
		{
			return;
		}
		Vector2Int vector2Int = this.m_selectedPiece[(int)this.GetSelectedCategory()];
		int x = vector2Int.x + 1;
		vector2Int.x = x;
		if (vector2Int.x >= 15)
		{
			vector2Int.x = 0;
		}
		this.m_selectedPiece[(int)this.GetSelectedCategory()] = vector2Int;
	}

	// Token: 0x06000B6C RID: 2924 RVA: 0x000609E4 File Offset: 0x0005EBE4
	public void DownPiece()
	{
		if (this.m_availablePieces[(int)this.GetSelectedCategory()].Count <= 1)
		{
			return;
		}
		Vector2Int vector2Int = this.m_selectedPiece[(int)this.GetSelectedCategory()];
		int y = vector2Int.y + 1;
		vector2Int.y = y;
		if (vector2Int.y >= 6)
		{
			vector2Int.y = 0;
		}
		this.m_selectedPiece[(int)this.GetSelectedCategory()] = vector2Int;
	}

	// Token: 0x06000B6D RID: 2925 RVA: 0x00060A54 File Offset: 0x0005EC54
	public void UpPiece()
	{
		if (this.m_availablePieces[(int)this.GetSelectedCategory()].Count <= 1)
		{
			return;
		}
		Vector2Int vector2Int = this.m_selectedPiece[(int)this.GetSelectedCategory()];
		int y = vector2Int.y - 1;
		vector2Int.y = y;
		if (vector2Int.y < 0)
		{
			vector2Int.y = 5;
		}
		this.m_selectedPiece[(int)this.GetSelectedCategory()] = vector2Int;
	}

	// Token: 0x06000B6E RID: 2926 RVA: 0x00060AC4 File Offset: 0x0005ECC4
	public void NextCategory()
	{
		if (this.m_categories.Count == 0)
		{
			return;
		}
		int i = this.m_categories.Count - 1;
		while (i >= 0)
		{
			if (this.m_categories[i] == this.GetSelectedCategory())
			{
				if (i + 1 == this.m_categories.Count)
				{
					this.m_selectedCategory = this.m_categories[0];
					return;
				}
				this.m_selectedCategory = this.m_categories[i + 1];
				return;
			}
			else
			{
				i--;
			}
		}
	}

	// Token: 0x06000B6F RID: 2927 RVA: 0x00060B44 File Offset: 0x0005ED44
	public void PrevCategory()
	{
		if (this.m_categories.Count == 0)
		{
			return;
		}
		int i = 0;
		while (i < this.m_categories.Count)
		{
			if (this.m_categories[i] == this.GetSelectedCategory())
			{
				if (i - 1 < 0)
				{
					this.m_selectedCategory = this.m_categories[this.m_categories.Count - 1];
					return;
				}
				this.m_selectedCategory = this.m_categories[i - 1];
				return;
			}
			else
			{
				i++;
			}
		}
	}

	// Token: 0x06000B70 RID: 2928 RVA: 0x00060BC3 File Offset: 0x0005EDC3
	public void SetCategory(int index)
	{
		if (this.m_categories.Count == 0)
		{
			return;
		}
		this.m_selectedCategory = this.m_categories[index];
	}

	// Token: 0x04000CA3 RID: 3235
	public const int m_gridWidth = 15;

	// Token: 0x04000CA4 RID: 3236
	public const int m_gridHeight = 6;

	// Token: 0x04000CA5 RID: 3237
	public List<GameObject> m_pieces = new List<GameObject>();

	// Token: 0x04000CA6 RID: 3238
	public List<Piece.PieceCategory> m_categories = new List<Piece.PieceCategory>();

	// Token: 0x04000CA7 RID: 3239
	public List<string> m_categoryLabels = new List<string>();

	// Token: 0x04000CA8 RID: 3240
	public bool m_canRemovePieces = true;

	// Token: 0x04000CA9 RID: 3241
	public bool m_canRemoveFeasts;

	// Token: 0x04000CAA RID: 3242
	public Skills.SkillType m_skill;

	// Token: 0x04000CAB RID: 3243
	[NonSerialized]
	private List<List<Piece>> m_availablePieces = new List<List<Piece>>();

	// Token: 0x04000CAC RID: 3244
	private Piece.PieceCategory m_selectedCategory = Piece.PieceCategory.Max;

	// Token: 0x04000CAD RID: 3245
	[NonSerialized]
	public Vector2Int[] m_selectedPiece = new Vector2Int[8];

	// Token: 0x04000CAE RID: 3246
	[NonSerialized]
	public Vector2Int[] m_lastSelectedPiece = new Vector2Int[8];

	// Token: 0x04000CAF RID: 3247
	[HideInInspector]
	public List<Piece.PieceCategory> m_categoriesFolded = new List<Piece.PieceCategory>();
}
