using System;

// Token: 0x020000D0 RID: 208
public class ZDOConnection
{
	// Token: 0x06000D0F RID: 3343 RVA: 0x00065DCB File Offset: 0x00063FCB
	public ZDOConnection(ZDOExtraData.ConnectionType type, ZDOID target)
	{
		this.m_type = type;
		this.m_target = target;
	}

	// Token: 0x04000D3F RID: 3391
	public readonly ZDOExtraData.ConnectionType m_type;

	// Token: 0x04000D40 RID: 3392
	public readonly ZDOID m_target = ZDOID.None;
}
