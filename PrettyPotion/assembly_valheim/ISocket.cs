using System;

// Token: 0x020000B9 RID: 185
public interface ISocket
{
	// Token: 0x06000B80 RID: 2944
	bool IsConnected();

	// Token: 0x06000B81 RID: 2945
	void Send(ZPackage pkg);

	// Token: 0x06000B82 RID: 2946
	ZPackage Recv();

	// Token: 0x06000B83 RID: 2947
	int GetSendQueueSize();

	// Token: 0x06000B84 RID: 2948
	int GetCurrentSendRate();

	// Token: 0x06000B85 RID: 2949
	bool IsHost();

	// Token: 0x06000B86 RID: 2950
	void Dispose();

	// Token: 0x06000B87 RID: 2951
	bool GotNewData();

	// Token: 0x06000B88 RID: 2952
	void Close();

	// Token: 0x06000B89 RID: 2953
	string GetEndPointString();

	// Token: 0x06000B8A RID: 2954
	void GetAndResetStats(out int totalSent, out int totalRecv);

	// Token: 0x06000B8B RID: 2955
	void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec);

	// Token: 0x06000B8C RID: 2956
	ISocket Accept();

	// Token: 0x06000B8D RID: 2957
	int GetHostPort();

	// Token: 0x06000B8E RID: 2958
	bool Flush();

	// Token: 0x06000B8F RID: 2959
	string GetHostName();

	// Token: 0x06000B90 RID: 2960
	void VersionMatch();
}
