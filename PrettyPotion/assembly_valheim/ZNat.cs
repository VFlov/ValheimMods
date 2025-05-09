using System;
using System.IO;

// Token: 0x020000D7 RID: 215
public class ZNat : IDisposable
{
	// Token: 0x06000D89 RID: 3465 RVA: 0x0006A121 File Offset: 0x00068321
	public void Dispose()
	{
	}

	// Token: 0x06000D8A RID: 3466 RVA: 0x0006A123 File Offset: 0x00068323
	public void SetPort(int port)
	{
		if (this.m_port == port)
		{
			return;
		}
		this.m_port = port;
	}

	// Token: 0x06000D8B RID: 3467 RVA: 0x0006A136 File Offset: 0x00068336
	public void Update(float dt)
	{
	}

	// Token: 0x06000D8C RID: 3468 RVA: 0x0006A138 File Offset: 0x00068338
	public bool GetStatus()
	{
		return this.m_mappingOK;
	}

	// Token: 0x04000E1D RID: 3613
	private FileStream m_output;

	// Token: 0x04000E1E RID: 3614
	private bool m_mappingOK;

	// Token: 0x04000E1F RID: 3615
	private int m_port;
}
