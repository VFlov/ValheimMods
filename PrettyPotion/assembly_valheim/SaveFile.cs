using System;
using System.Collections.Generic;

// Token: 0x02000139 RID: 313
public class SaveFile
{
	// Token: 0x06001389 RID: 5001 RVA: 0x00090A8D File Offset: 0x0008EC8D
	public SaveFile(string path, FileHelpers.FileSource source, SaveWithBackups parentSaveWithBackups, Action modifiedCallback)
	{
		this.m_paths = new List<string>();
		this.m_source = source;
		this.ParentSaveWithBackups = parentSaveWithBackups;
		this.m_modifiedCallback = modifiedCallback;
		this.AddAssociatedFile(path);
	}

	// Token: 0x0600138A RID: 5002 RVA: 0x00090AC8 File Offset: 0x0008ECC8
	public SaveFile(string[] paths, FileHelpers.FileSource source, SaveWithBackups parentSaveWithBackups, Action modifiedCallback)
	{
		this.m_paths = new List<string>();
		this.m_source = source;
		this.ParentSaveWithBackups = parentSaveWithBackups;
		this.m_modifiedCallback = modifiedCallback;
		this.AddAssociatedFiles(paths);
	}

	// Token: 0x0600138B RID: 5003 RVA: 0x00090B04 File Offset: 0x0008ED04
	public SaveFile(FilePathAndSource pathAndSource, SaveWithBackups inSaveFile, Action modifiedCallback)
	{
		this.m_paths = new List<string>();
		this.m_source = pathAndSource.source;
		this.Size = 0UL;
		this.ParentSaveWithBackups = inSaveFile;
		this.m_modifiedCallback = modifiedCallback;
		this.AddAssociatedFile(pathAndSource.path);
	}

	// Token: 0x0600138C RID: 5004 RVA: 0x00090B5C File Offset: 0x0008ED5C
	public void AddAssociatedFile(string path)
	{
		this.m_paths.Add(path);
		this.Size += FileHelpers.GetFileSize(path, this.m_source);
		string text;
		SaveFileType saveFileType;
		string text2;
		DateTime? dateTime;
		if (!SaveSystem.GetSaveInfo(path, out text, out saveFileType, out text2, out dateTime) || dateTime == null)
		{
			dateTime = new DateTime?(FileHelpers.GetLastWriteTime(path, this.m_source));
		}
		if (dateTime.Value > this.LastModified)
		{
			this.LastModified = dateTime.Value;
		}
		this.OnModified();
	}

	// Token: 0x0600138D RID: 5005 RVA: 0x00090BE4 File Offset: 0x0008EDE4
	public void AddAssociatedFiles(string[] paths)
	{
		this.m_paths.AddRange(paths);
		for (int i = 0; i < paths.Length; i++)
		{
			this.Size += FileHelpers.GetFileSize(paths[i], this.m_source);
			string text;
			SaveFileType saveFileType;
			string text2;
			DateTime? dateTime;
			if (!SaveSystem.GetSaveInfo(paths[i], out text, out saveFileType, out text2, out dateTime) || dateTime == null)
			{
				dateTime = new DateTime?(FileHelpers.GetLastWriteTime(paths[i], this.m_source));
			}
			if (dateTime.Value > this.LastModified)
			{
				this.LastModified = dateTime.Value;
			}
		}
		this.OnModified();
	}

	// Token: 0x170000A8 RID: 168
	// (get) Token: 0x0600138E RID: 5006 RVA: 0x00090C7D File Offset: 0x0008EE7D
	public string PathPrimary
	{
		get
		{
			this.EnsureSorted();
			return this.m_paths[0];
		}
	}

	// Token: 0x170000A9 RID: 169
	// (get) Token: 0x0600138F RID: 5007 RVA: 0x00090C94 File Offset: 0x0008EE94
	public string[] PathsAssociated
	{
		get
		{
			this.EnsureSorted();
			string[] array = new string[this.m_paths.Count - 1];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = this.m_paths[i + 1];
			}
			return array;
		}
	}

	// Token: 0x170000AA RID: 170
	// (get) Token: 0x06001390 RID: 5008 RVA: 0x00090CDA File Offset: 0x0008EEDA
	public string[] AllPaths
	{
		get
		{
			this.EnsureSorted();
			return this.m_paths.ToArray();
		}
	}

	// Token: 0x170000AB RID: 171
	// (get) Token: 0x06001391 RID: 5009 RVA: 0x00090CF0 File Offset: 0x0008EEF0
	public string FileName
	{
		get
		{
			if (this.m_fileName == null)
			{
				string pathPrimary = this.PathPrimary;
				string text;
				SaveFileType saveFileType;
				string text2;
				DateTime? dateTime;
				if (!SaveSystem.GetSaveInfo(pathPrimary, out text, out saveFileType, out text2, out dateTime))
				{
					this.m_fileName = SaveSystem.RemoveDirectoryPart(pathPrimary);
					return this.m_fileName;
				}
				SaveDataType dataType = this.ParentSaveWithBackups.ParentSaveCollection.m_dataType;
				if (dataType != SaveDataType.World)
				{
					if (dataType == SaveDataType.Character)
					{
						if (text2 != ".fch")
						{
							this.m_fileName = SaveSystem.RemoveDirectoryPart(pathPrimary);
							return this.m_fileName;
						}
					}
				}
				else if (text2 != ".fwl" && text2 != ".db")
				{
					this.m_fileName = SaveSystem.RemoveDirectoryPart(pathPrimary);
					return this.m_fileName;
				}
				string text3 = SaveSystem.RemoveDirectoryPart(pathPrimary);
				int num = text3.LastIndexOf(text2);
				if (num < 0)
				{
					this.m_fileName = text3;
				}
				else
				{
					this.m_fileName = text3.Remove(num, text2.Length);
				}
			}
			return this.m_fileName;
		}
	}

	// Token: 0x06001392 RID: 5010 RVA: 0x00090DD4 File Offset: 0x0008EFD4
	public override bool Equals(object obj)
	{
		SaveFile saveFile = obj as SaveFile;
		if (saveFile == null)
		{
			return false;
		}
		if (this.m_source != saveFile.m_source)
		{
			return false;
		}
		string[] allPaths = this.AllPaths;
		string[] allPaths2 = saveFile.AllPaths;
		if (allPaths.Length != allPaths2.Length)
		{
			return false;
		}
		for (int i = 0; i < allPaths.Length; i++)
		{
			if (allPaths[i] != allPaths2[i])
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06001393 RID: 5011 RVA: 0x00090E34 File Offset: 0x0008F034
	public override int GetHashCode()
	{
		string[] allPaths = this.AllPaths;
		int num = 878520832;
		num = num * -1521134295 + allPaths.Length.GetHashCode();
		for (int i = 0; i < allPaths.Length; i++)
		{
			num = num * -1521134295 + EqualityComparer<string>.Default.GetHashCode(allPaths[i]);
		}
		return num * -1521134295 + this.m_source.GetHashCode();
	}

	// Token: 0x170000AC RID: 172
	// (get) Token: 0x06001394 RID: 5012 RVA: 0x00090EA1 File Offset: 0x0008F0A1
	// (set) Token: 0x06001395 RID: 5013 RVA: 0x00090EA9 File Offset: 0x0008F0A9
	public DateTime LastModified { get; private set; } = DateTime.MinValue;

	// Token: 0x170000AD RID: 173
	// (get) Token: 0x06001396 RID: 5014 RVA: 0x00090EB2 File Offset: 0x0008F0B2
	// (set) Token: 0x06001397 RID: 5015 RVA: 0x00090EBA File Offset: 0x0008F0BA
	public ulong Size { get; private set; }

	// Token: 0x170000AE RID: 174
	// (get) Token: 0x06001398 RID: 5016 RVA: 0x00090EC3 File Offset: 0x0008F0C3
	// (set) Token: 0x06001399 RID: 5017 RVA: 0x00090ECB File Offset: 0x0008F0CB
	public SaveWithBackups ParentSaveWithBackups { get; private set; }

	// Token: 0x0600139A RID: 5018 RVA: 0x00090ED4 File Offset: 0x0008F0D4
	private void EnsureSorted()
	{
		if (!this.m_isDirty)
		{
			return;
		}
		this.m_paths.Sort(SaveSystem.GetComparerByDataType(this.ParentSaveWithBackups.ParentSaveCollection.m_dataType));
		this.m_isDirty = false;
	}

	// Token: 0x0600139B RID: 5019 RVA: 0x00090F06 File Offset: 0x0008F106
	private void OnModified()
	{
		this.SetDirty();
		Action modifiedCallback = this.m_modifiedCallback;
		if (modifiedCallback == null)
		{
			return;
		}
		modifiedCallback();
	}

	// Token: 0x0600139C RID: 5020 RVA: 0x00090F1E File Offset: 0x0008F11E
	private void SetDirty()
	{
		this.m_isDirty = (this.m_paths.Count > 1);
		this.m_fileName = null;
	}

	// Token: 0x0400138A RID: 5002
	private List<string> m_paths;

	// Token: 0x0400138B RID: 5003
	public readonly FileHelpers.FileSource m_source;

	// Token: 0x0400138F RID: 5007
	private Action m_modifiedCallback;

	// Token: 0x04001390 RID: 5008
	private bool m_isDirty;

	// Token: 0x04001391 RID: 5009
	private string m_fileName;
}
