using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001A5 RID: 421
public class PointGenerator
{
	// Token: 0x060018BB RID: 6331 RVA: 0x000B8F9C File Offset: 0x000B719C
	public PointGenerator(int amount, float gridSize)
	{
		this.m_amount = amount;
		this.m_gridSize = gridSize;
	}

	// Token: 0x060018BC RID: 6332 RVA: 0x000B8FE8 File Offset: 0x000B71E8
	public void Update(Vector3 center, float radius, List<Vector3> newPoints, List<Vector3> removedPoints)
	{
		Vector2Int grid = this.GetGrid(center);
		if (this.m_currentCenterGrid == grid)
		{
			newPoints.Clear();
			removedPoints.Clear();
			return;
		}
		int num = Mathf.CeilToInt(radius / this.m_gridSize);
		if (this.m_currentCenterGrid != grid || this.m_currentGridWith != num)
		{
			this.RegeneratePoints(grid, num);
		}
	}

	// Token: 0x060018BD RID: 6333 RVA: 0x000B9048 File Offset: 0x000B7248
	private void RegeneratePoints(Vector2Int centerGrid, int gridWith)
	{
		this.m_currentCenterGrid = centerGrid;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		this.m_points.Clear();
		for (int i = centerGrid.y - gridWith; i <= centerGrid.y + gridWith; i++)
		{
			for (int j = centerGrid.x - gridWith; j <= centerGrid.x + gridWith; j++)
			{
				UnityEngine.Random.InitState(j + i * 100);
				Vector3 gridPos = this.GetGridPos(new Vector2Int(j, i));
				for (int k = 0; k < this.m_amount; k++)
				{
					Vector3 item = new Vector3(UnityEngine.Random.Range(gridPos.x - this.m_gridSize, gridPos.x + this.m_gridSize), UnityEngine.Random.Range(gridPos.z - this.m_gridSize, gridPos.z + this.m_gridSize));
					this.m_points.Add(item);
				}
			}
		}
		UnityEngine.Random.state = state;
	}

	// Token: 0x060018BE RID: 6334 RVA: 0x000B9138 File Offset: 0x000B7338
	public Vector2Int GetGrid(Vector3 point)
	{
		int x = Mathf.FloorToInt((point.x + this.m_gridSize / 2f) / this.m_gridSize);
		int y = Mathf.FloorToInt((point.z + this.m_gridSize / 2f) / this.m_gridSize);
		return new Vector2Int(x, y);
	}

	// Token: 0x060018BF RID: 6335 RVA: 0x000B918A File Offset: 0x000B738A
	public Vector3 GetGridPos(Vector2Int grid)
	{
		return new Vector3((float)grid.x * this.m_gridSize, 0f, (float)grid.y * this.m_gridSize);
	}

	// Token: 0x040018FC RID: 6396
	private int m_amount;

	// Token: 0x040018FD RID: 6397
	private float m_gridSize = 8f;

	// Token: 0x040018FE RID: 6398
	private Vector2Int m_currentCenterGrid = new Vector2Int(99999, 99999);

	// Token: 0x040018FF RID: 6399
	private int m_currentGridWith;

	// Token: 0x04001900 RID: 6400
	private List<Vector3> m_points = new List<Vector3>();
}
