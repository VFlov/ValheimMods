using System;

// Token: 0x02000093 RID: 147
[Flags]
public enum AssetMemoryUsagePolicy
{
	// Token: 0x04000B8A RID: 2954
	KeepAllLoaded = 7,
	// Token: 0x04000B8B RID: 2955
	KeepSynchronousOnlyLoaded = 3,
	// Token: 0x04000B8C RID: 2956
	KeepNoneLoaded = 1,
	// Token: 0x04000B8D RID: 2957
	KeepSynchronousLoadedBit = 2,
	// Token: 0x04000B8E RID: 2958
	KeepAsynchronousLoadedBit = 4
}
