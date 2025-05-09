using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

// Token: 0x020000D3 RID: 211
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct ZDOID : IEquatable<ZDOID>, IComparable<ZDOID>
{
	// Token: 0x06000D20 RID: 3360 RVA: 0x000662D3 File Offset: 0x000644D3
	public static void Reset()
	{
		ZDOID.m_userIDs.Clear();
		ZDOID.m_userIDs.Add(ZDOID.NullUser);
		ZDOID.m_userIDs.Add(ZDOID.UnknownFormerUser);
		ZDOID.m_userIDCount = 2;
		ZDOID.m_loadID = 0U;
	}

	// Token: 0x06000D21 RID: 3361 RVA: 0x0006630C File Offset: 0x0006450C
	public static ushort AddUser(long userID)
	{
		int num = ZDOID.m_userIDs.IndexOf(userID);
		if (num < 0)
		{
			ZDOID.m_userIDs.Add(userID);
			ushort userIDCount = ZDOID.m_userIDCount;
			ZDOID.m_userIDCount = userIDCount + 1;
			return userIDCount;
		}
		if (userID == 0L)
		{
			return 0;
		}
		return (ushort)num;
	}

	// Token: 0x06000D22 RID: 3362 RVA: 0x0006634A File Offset: 0x0006454A
	public static long GetUserID(ushort userKey)
	{
		return ZDOID.m_userIDs[(int)userKey];
	}

	// Token: 0x06000D23 RID: 3363 RVA: 0x00066357 File Offset: 0x00064557
	public ZDOID(BinaryReader reader)
	{
		this.UserKey = ZDOID.AddUser(reader.ReadInt64());
		this.ID = reader.ReadUInt32();
	}

	// Token: 0x06000D24 RID: 3364 RVA: 0x00066376 File Offset: 0x00064576
	public ZDOID(long userID, uint id)
	{
		this.UserKey = ZDOID.AddUser(userID);
		this.ID = id;
	}

	// Token: 0x06000D25 RID: 3365 RVA: 0x0006638B File Offset: 0x0006458B
	public void SetID(uint id)
	{
		this.ID = id;
		this.UserKey = ZDOID.UnknownFormerUserKey;
	}

	// Token: 0x06000D26 RID: 3366 RVA: 0x000663A0 File Offset: 0x000645A0
	public override string ToString()
	{
		return ZDOID.GetUserID(this.UserKey).ToString() + ":" + this.ID.ToString();
	}

	// Token: 0x06000D27 RID: 3367 RVA: 0x000663D8 File Offset: 0x000645D8
	public static bool operator ==(ZDOID a, ZDOID b)
	{
		return a.UserKey == b.UserKey && a.ID == b.ID;
	}

	// Token: 0x06000D28 RID: 3368 RVA: 0x000663FC File Offset: 0x000645FC
	public static bool operator !=(ZDOID a, ZDOID b)
	{
		return a.UserKey != b.UserKey || a.ID != b.ID;
	}

	// Token: 0x06000D29 RID: 3369 RVA: 0x00066423 File Offset: 0x00064623
	public bool Equals(ZDOID other)
	{
		return other.UserKey == this.UserKey && other.ID == this.ID;
	}

	// Token: 0x06000D2A RID: 3370 RVA: 0x00066448 File Offset: 0x00064648
	public override bool Equals(object other)
	{
		if (other is ZDOID)
		{
			ZDOID b = (ZDOID)other;
			return this == b;
		}
		return false;
	}

	// Token: 0x06000D2B RID: 3371 RVA: 0x00066474 File Offset: 0x00064674
	public int CompareTo(ZDOID other)
	{
		if (this.UserKey != other.UserKey)
		{
			if (this.UserKey >= other.UserKey)
			{
				return 1;
			}
			return -1;
		}
		else
		{
			if (this.ID < other.ID)
			{
				return -1;
			}
			if (this.ID <= other.ID)
			{
				return 0;
			}
			return 1;
		}
	}

	// Token: 0x06000D2C RID: 3372 RVA: 0x000664C8 File Offset: 0x000646C8
	public override int GetHashCode()
	{
		return ZDOID.GetUserID(this.UserKey).GetHashCode() ^ this.ID.GetHashCode();
	}

	// Token: 0x06000D2D RID: 3373 RVA: 0x000664F7 File Offset: 0x000646F7
	public bool IsNone()
	{
		return this.UserKey == 0 && this.ID == 0U;
	}

	// Token: 0x17000073 RID: 115
	// (get) Token: 0x06000D2E RID: 3374 RVA: 0x0006650C File Offset: 0x0006470C
	public long UserID
	{
		get
		{
			return ZDOID.GetUserID(this.UserKey);
		}
	}

	// Token: 0x17000074 RID: 116
	// (get) Token: 0x06000D2F RID: 3375 RVA: 0x00066519 File Offset: 0x00064719
	// (set) Token: 0x06000D30 RID: 3376 RVA: 0x00066521 File Offset: 0x00064721
	private ushort UserKey { readonly get; set; }

	// Token: 0x17000075 RID: 117
	// (get) Token: 0x06000D31 RID: 3377 RVA: 0x0006652A File Offset: 0x0006472A
	// (set) Token: 0x06000D32 RID: 3378 RVA: 0x00066532 File Offset: 0x00064732
	public uint ID { readonly get; private set; }

	// Token: 0x04000D46 RID: 3398
	private static readonly long NullUser = 0L;

	// Token: 0x04000D47 RID: 3399
	private static readonly long UnknownFormerUser = 1L;

	// Token: 0x04000D48 RID: 3400
	private static readonly ushort UnknownFormerUserKey = 1;

	// Token: 0x04000D49 RID: 3401
	public static uint m_loadID = 0U;

	// Token: 0x04000D4A RID: 3402
	private static readonly List<long> m_userIDs = new List<long>
	{
		ZDOID.NullUser,
		ZDOID.UnknownFormerUser
	};

	// Token: 0x04000D4B RID: 3403
	public static readonly ZDOID None = new ZDOID(0L, 0U);

	// Token: 0x04000D4C RID: 3404
	private static ushort m_userIDCount = 2;
}
