using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200003B RID: 59
public class SE_Rested : SE_Stats
{
	// Token: 0x060004FA RID: 1274 RVA: 0x0002BEBC File Offset: 0x0002A0BC
	public override void Setup(Character character)
	{
		base.Setup(character);
		this.UpdateTTL();
		Player player = this.m_character as Player;
		this.m_character.Message(MessageHud.MessageType.Center, "$se_rested_start ($se_rested_comfort:" + player.GetComfortLevel().ToString() + ")", 0, null);
	}

	// Token: 0x060004FB RID: 1275 RVA: 0x0002BF0D File Offset: 0x0002A10D
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		this.m_timeSinceComfortUpdate -= dt;
	}

	// Token: 0x060004FC RID: 1276 RVA: 0x0002BF24 File Offset: 0x0002A124
	public override void ResetTime()
	{
		this.UpdateTTL();
	}

	// Token: 0x060004FD RID: 1277 RVA: 0x0002BF2C File Offset: 0x0002A12C
	private void UpdateTTL()
	{
		Player player = this.m_character as Player;
		float num = this.m_baseTTL + (float)(player.GetComfortLevel() - 1) * this.m_TTLPerComfortLevel;
		float num2 = this.m_ttl - this.m_time;
		if (num > num2)
		{
			this.m_ttl = num;
			this.m_time = 0f;
		}
	}

	// Token: 0x060004FE RID: 1278 RVA: 0x0002BF84 File Offset: 0x0002A184
	private static int PieceComfortSort(Piece x, Piece y)
	{
		if (x.m_comfortGroup != y.m_comfortGroup)
		{
			return x.m_comfortGroup.CompareTo(y.m_comfortGroup);
		}
		float num = (float)x.GetComfort();
		float num2 = (float)y.GetComfort();
		if (num != num2)
		{
			return num2.CompareTo(num);
		}
		return y.m_name.CompareTo(x.m_name);
	}

	// Token: 0x060004FF RID: 1279 RVA: 0x0002BFEA File Offset: 0x0002A1EA
	public static int CalculateComfortLevel(Player player)
	{
		return SE_Rested.CalculateComfortLevel(player.InShelter(), player.transform.position);
	}

	// Token: 0x06000500 RID: 1280 RVA: 0x0002C004 File Offset: 0x0002A204
	public static int CalculateComfortLevel(bool inShelter, Vector3 position)
	{
		int num = 1;
		if (inShelter)
		{
			num++;
			List<Piece> nearbyComfortPieces = SE_Rested.GetNearbyComfortPieces(position);
			nearbyComfortPieces.Sort(new Comparison<Piece>(SE_Rested.PieceComfortSort));
			int i = 0;
			while (i < nearbyComfortPieces.Count)
			{
				Piece piece = nearbyComfortPieces[i];
				if (i <= 0)
				{
					goto IL_68;
				}
				Piece piece2 = nearbyComfortPieces[i - 1];
				if ((piece.m_comfortGroup == Piece.ComfortGroup.None || piece.m_comfortGroup != piece2.m_comfortGroup) && !(piece.m_name == piece2.m_name))
				{
					goto IL_68;
				}
				IL_71:
				i++;
				continue;
				IL_68:
				num += piece.GetComfort();
				goto IL_71;
			}
		}
		return num;
	}

	// Token: 0x06000501 RID: 1281 RVA: 0x0002C090 File Offset: 0x0002A290
	private static List<Piece> GetNearbyComfortPieces(Vector3 point)
	{
		SE_Rested.s_tempPieces.Clear();
		Piece.GetAllComfortPiecesInRadius(point, 10f, SE_Rested.s_tempPieces);
		return SE_Rested.s_tempPieces;
	}

	// Token: 0x04000595 RID: 1429
	[Header("__SE_Rested__")]
	public float m_baseTTL = 300f;

	// Token: 0x04000596 RID: 1430
	public float m_TTLPerComfortLevel = 60f;

	// Token: 0x04000597 RID: 1431
	private const float c_ComfortRadius = 10f;

	// Token: 0x04000598 RID: 1432
	private float m_timeSinceComfortUpdate;

	// Token: 0x04000599 RID: 1433
	private static readonly List<Piece> s_tempPieces = new List<Piece>();
}
