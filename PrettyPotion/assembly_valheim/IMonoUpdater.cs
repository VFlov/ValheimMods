using System;

// Token: 0x020000BD RID: 189
public interface IMonoUpdater
{
	// Token: 0x06000BA8 RID: 2984 RVA: 0x00061AD9 File Offset: 0x0005FCD9
	void CustomFixedUpdate(float deltaTime)
	{
	}

	// Token: 0x06000BA9 RID: 2985 RVA: 0x00061ADB File Offset: 0x0005FCDB
	void CustomUpdate(float deltaTime, float time)
	{
	}

	// Token: 0x06000BAA RID: 2986 RVA: 0x00061ADD File Offset: 0x0005FCDD
	void CustomLateUpdate(float deltaTime)
	{
	}
}
