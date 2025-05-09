using System;
using UnityEngine;

// Token: 0x020000DC RID: 220
public class ZNetStats
{
	// Token: 0x06000E5C RID: 3676 RVA: 0x0006F27A File Offset: 0x0006D47A
	internal void IncRecvBytes(int count)
	{
		this.m_recvBytes += count;
	}

	// Token: 0x06000E5D RID: 3677 RVA: 0x0006F28A File Offset: 0x0006D48A
	internal void IncSentBytes(int count)
	{
		this.m_sentBytes += count;
	}

	// Token: 0x06000E5E RID: 3678 RVA: 0x0006F29A File Offset: 0x0006D49A
	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = this.m_sentBytes;
		totalRecv = this.m_recvBytes;
		this.m_sentBytes = 0;
		this.m_statSentBytes = 0;
		this.m_recvBytes = 0;
		this.m_statRecvBytes = 0;
		this.m_statStart = Time.time;
	}

	// Token: 0x06000E5F RID: 3679 RVA: 0x0006F2D4 File Offset: 0x0006D4D4
	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		float num = Time.time - this.m_statStart;
		if (num >= 1f)
		{
			this.m_sendRate = ((float)(this.m_sentBytes - this.m_statSentBytes) / num * 2f + this.m_sendRate) / 3f;
			this.m_recvRate = ((float)(this.m_recvBytes - this.m_statRecvBytes) / num * 2f + this.m_recvRate) / 3f;
			this.m_statSentBytes = this.m_sentBytes;
			this.m_statRecvBytes = this.m_recvBytes;
			this.m_statStart = Time.time;
		}
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = this.m_sendRate;
		inByteSec = this.m_recvRate;
	}

	// Token: 0x04000E70 RID: 3696
	private int m_recvBytes;

	// Token: 0x04000E71 RID: 3697
	private int m_statRecvBytes;

	// Token: 0x04000E72 RID: 3698
	private int m_sentBytes;

	// Token: 0x04000E73 RID: 3699
	private int m_statSentBytes;

	// Token: 0x04000E74 RID: 3700
	private float m_recvRate;

	// Token: 0x04000E75 RID: 3701
	private float m_sendRate;

	// Token: 0x04000E76 RID: 3702
	private float m_statStart = Time.time;
}
