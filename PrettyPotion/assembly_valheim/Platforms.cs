using System;

// Token: 0x02000150 RID: 336
[Flags]
public enum Platforms
{
	// Token: 0x04001416 RID: 5142
	None = 0,
	// Token: 0x04001417 RID: 5143
	SteamWindows = 1,
	// Token: 0x04001418 RID: 5144
	SteamLinux = 2,
	// Token: 0x04001419 RID: 5145
	SteamDeckProton = 4,
	// Token: 0x0400141A RID: 5146
	SteamDeckNative = 8,
	// Token: 0x0400141B RID: 5147
	MicrosoftStore = 16,
	// Token: 0x0400141C RID: 5148
	Xbox = 32,
	// Token: 0x0400141D RID: 5149
	All = 63
}
