using System;
using System.Collections.Generic;
using System.Threading;
using Ionic.Zlib;

// Token: 0x020000FE RID: 254
public class PlayFabZLibWorkQueue : IDisposable
{
	// Token: 0x06001027 RID: 4135 RVA: 0x000778CC File Offset: 0x00075ACC
	public PlayFabZLibWorkQueue()
	{
		PlayFabZLibWorkQueue.s_workersMutex.WaitOne();
		if (PlayFabZLibWorkQueue.s_thread == null)
		{
			ZLog.Log("Semaphore type: " + PlayFabZLibWorkQueue.s_workSemaphore.GetType().Name);
			PlayFabZLibWorkQueue.s_thread = new Thread(new ThreadStart(this.WorkerMain));
			PlayFabZLibWorkQueue.s_thread.Name = "PlayfabZlibThread";
			PlayFabZLibWorkQueue.s_thread.Start();
		}
		PlayFabZLibWorkQueue.s_workers.Add(this);
		PlayFabZLibWorkQueue.s_workersMutex.ReleaseMutex();
	}

	// Token: 0x06001028 RID: 4136 RVA: 0x0007798A File Offset: 0x00075B8A
	public void Compress(byte[] buffer)
	{
		this.m_buffersMutex.WaitOne();
		this.m_inCompress.Enqueue(buffer);
		this.m_buffersMutex.ReleaseMutex();
		if (PlayFabZLibWorkQueue.s_workSemaphore.CurrentCount < 1)
		{
			PlayFabZLibWorkQueue.s_workSemaphore.Release();
		}
	}

	// Token: 0x06001029 RID: 4137 RVA: 0x000779C7 File Offset: 0x00075BC7
	public void Decompress(byte[] buffer)
	{
		this.m_buffersMutex.WaitOne();
		this.m_inDecompress.Enqueue(buffer);
		this.m_buffersMutex.ReleaseMutex();
		if (PlayFabZLibWorkQueue.s_workSemaphore.CurrentCount < 1)
		{
			PlayFabZLibWorkQueue.s_workSemaphore.Release();
		}
	}

	// Token: 0x0600102A RID: 4138 RVA: 0x00077A04 File Offset: 0x00075C04
	public void Poll(out List<byte[]> compressedBuffers, out List<byte[]> decompressedBuffers)
	{
		compressedBuffers = null;
		decompressedBuffers = null;
		this.m_buffersMutex.WaitOne();
		if (this.m_outCompress.Count > 0)
		{
			compressedBuffers = new List<byte[]>();
			while (this.m_outCompress.Count > 0)
			{
				compressedBuffers.Add(this.m_outCompress.Dequeue());
			}
		}
		if (this.m_outDecompress.Count > 0)
		{
			decompressedBuffers = new List<byte[]>();
			while (this.m_outDecompress.Count > 0)
			{
				decompressedBuffers.Add(this.m_outDecompress.Dequeue());
			}
		}
		this.m_buffersMutex.ReleaseMutex();
	}

	// Token: 0x0600102B RID: 4139 RVA: 0x00077A9C File Offset: 0x00075C9C
	private void WorkerMain()
	{
		for (;;)
		{
			PlayFabZLibWorkQueue.s_workSemaphore.Wait();
			PlayFabZLibWorkQueue.s_workersMutex.WaitOne();
			foreach (PlayFabZLibWorkQueue playFabZLibWorkQueue in PlayFabZLibWorkQueue.s_workers)
			{
				playFabZLibWorkQueue.Execute();
			}
			PlayFabZLibWorkQueue.s_workersMutex.ReleaseMutex();
		}
	}

	// Token: 0x0600102C RID: 4140 RVA: 0x00077B0C File Offset: 0x00075D0C
	private void Execute()
	{
		this.m_buffersMutex.WaitOne();
		this.DoUncompress();
		this.m_buffersMutex.ReleaseMutex();
		this.m_buffersMutex.WaitOne();
		this.DoCompress();
		this.m_buffersMutex.ReleaseMutex();
	}

	// Token: 0x0600102D RID: 4141 RVA: 0x00077B48 File Offset: 0x00075D48
	private void DoUncompress()
	{
		while (this.m_inDecompress.Count > 0)
		{
			try
			{
				byte[] payload = this.m_inDecompress.Dequeue();
				byte[] item = this.UncompressOnThisThread(payload);
				this.m_outDecompress.Enqueue(item);
			}
			catch
			{
			}
		}
	}

	// Token: 0x0600102E RID: 4142 RVA: 0x00077B9C File Offset: 0x00075D9C
	private void DoCompress()
	{
		while (this.m_inCompress.Count > 0)
		{
			try
			{
				byte[] payload = this.m_inCompress.Dequeue();
				byte[] item = this.CompressOnThisThread(payload);
				this.m_outCompress.Enqueue(item);
			}
			catch
			{
			}
		}
	}

	// Token: 0x0600102F RID: 4143 RVA: 0x00077BF0 File Offset: 0x00075DF0
	public void Dispose()
	{
		PlayFabZLibWorkQueue.s_workersMutex.WaitOne();
		PlayFabZLibWorkQueue.s_workers.Remove(this);
		PlayFabZLibWorkQueue.s_workersMutex.ReleaseMutex();
	}

	// Token: 0x06001030 RID: 4144 RVA: 0x00077C13 File Offset: 0x00075E13
	internal byte[] CompressOnThisThread(byte[] payload)
	{
		return ZlibStream.CompressBuffer(payload);
	}

	// Token: 0x06001031 RID: 4145 RVA: 0x00077C1B File Offset: 0x00075E1B
	internal byte[] UncompressOnThisThread(byte[] payload)
	{
		return ZlibStream.UncompressBuffer(payload);
	}

	// Token: 0x04000F6E RID: 3950
	private static Thread s_thread;

	// Token: 0x04000F6F RID: 3951
	private static bool s_moreWork;

	// Token: 0x04000F70 RID: 3952
	private static readonly List<PlayFabZLibWorkQueue> s_workers = new List<PlayFabZLibWorkQueue>();

	// Token: 0x04000F71 RID: 3953
	private readonly Queue<byte[]> m_inCompress = new Queue<byte[]>();

	// Token: 0x04000F72 RID: 3954
	private readonly Queue<byte[]> m_outCompress = new Queue<byte[]>();

	// Token: 0x04000F73 RID: 3955
	private readonly Queue<byte[]> m_inDecompress = new Queue<byte[]>();

	// Token: 0x04000F74 RID: 3956
	private readonly Queue<byte[]> m_outDecompress = new Queue<byte[]>();

	// Token: 0x04000F75 RID: 3957
	private static Mutex s_workersMutex = new Mutex();

	// Token: 0x04000F76 RID: 3958
	private Mutex m_buffersMutex = new Mutex();

	// Token: 0x04000F77 RID: 3959
	private static SemaphoreSlim s_workSemaphore = new SemaphoreSlim(0, 1);
}
