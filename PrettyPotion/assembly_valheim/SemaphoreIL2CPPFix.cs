using System;
using System.Threading;

// Token: 0x02000143 RID: 323
public class SemaphoreIL2CPPFix
{
	// Token: 0x060013F5 RID: 5109 RVA: 0x00092EA0 File Offset: 0x000910A0
	public SemaphoreIL2CPPFix(int initialCount, int maxCount, bool allowContextSwitch = false)
	{
		if (initialCount < 0)
		{
			throw new InvalidOperationException("initialCount must be greater than or equal to 0!");
		}
		if (maxCount <= 0)
		{
			throw new InvalidOperationException("maxCount must be greater than 0!");
		}
		this.m_count = initialCount;
		this.m_allowContextSwitch = allowContextSwitch;
		this.m_maxCount = maxCount;
	}

	// Token: 0x170000B7 RID: 183
	// (get) Token: 0x060013F6 RID: 5110 RVA: 0x00092EF1 File Offset: 0x000910F1
	public int CurrentCount
	{
		get
		{
			return this.m_count;
		}
	}

	// Token: 0x060013F7 RID: 5111 RVA: 0x00092EF9 File Offset: 0x000910F9
	public void Release()
	{
		this.m_countLock.WaitOne();
		if (this.m_count >= this.m_maxCount)
		{
			throw new InvalidOperationException("Can't increment semaphore when it's already at its max value!");
		}
		this.m_count++;
		this.m_countLock.ReleaseMutex();
	}

	// Token: 0x060013F8 RID: 5112 RVA: 0x00092F3C File Offset: 0x0009113C
	public void Wait()
	{
		for (;;)
		{
			this.m_countLock.WaitOne();
			if (this.m_count > 0)
			{
				break;
			}
			this.m_countLock.ReleaseMutex();
			if (this.m_allowContextSwitch)
			{
				Thread.Sleep(1);
			}
		}
		this.m_count--;
		this.m_countLock.ReleaseMutex();
	}

	// Token: 0x040013C3 RID: 5059
	private Mutex m_countLock = new Mutex();

	// Token: 0x040013C4 RID: 5060
	private int m_count;

	// Token: 0x040013C5 RID: 5061
	private readonly int m_maxCount;

	// Token: 0x040013C6 RID: 5062
	private readonly bool m_allowContextSwitch;
}
