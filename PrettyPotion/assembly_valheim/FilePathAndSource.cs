using System;

// Token: 0x02000138 RID: 312
public struct FilePathAndSource
{
	// Token: 0x06001388 RID: 5000 RVA: 0x00090A7D File Offset: 0x0008EC7D
	public FilePathAndSource(string path, FileHelpers.FileSource source)
	{
		this.path = path;
		this.source = source;
	}

	// Token: 0x04001388 RID: 5000
	public string path;

	// Token: 0x04001389 RID: 5001
	public FileHelpers.FileSource source;
}
