using System;

// Token: 0x020000C6 RID: 198
public enum ServerPingStatus
{
	// Token: 0x04000CF0 RID: 3312
	NotStarted,
	// Token: 0x04000CF1 RID: 3313
	AwaitingResponse,
	// Token: 0x04000CF2 RID: 3314
	Success,
	// Token: 0x04000CF3 RID: 3315
	TimedOut,
	// Token: 0x04000CF4 RID: 3316
	CouldNotReach,
	// Token: 0x04000CF5 RID: 3317
	Unpingable
}
