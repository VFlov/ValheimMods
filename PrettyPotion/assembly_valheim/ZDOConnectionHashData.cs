using System;

// Token: 0x020000D1 RID: 209
public class ZDOConnectionHashData
{
	// Token: 0x06000D10 RID: 3344 RVA: 0x00065DEC File Offset: 0x00063FEC
	public ZDOConnectionHashData(ZDOExtraData.ConnectionType type, int hash)
	{
		this.m_type = type;
		this.m_hash = hash;
	}

	// Token: 0x04000D41 RID: 3393
	public readonly ZDOExtraData.ConnectionType m_type;

	// Token: 0x04000D42 RID: 3394
	public readonly int m_hash;
}
