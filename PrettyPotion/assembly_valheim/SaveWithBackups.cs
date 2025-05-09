using System;
using System.Collections.Generic;

// Token: 0x0200013A RID: 314
public class SaveWithBackups
{
	// Token: 0x0600139D RID: 5021 RVA: 0x00090F3B File Offset: 0x0008F13B
	public SaveWithBackups(string name, SaveCollection parentSaveCollection, Action modifiedCallback)
	{
		this.m_name = name;
		this.ParentSaveCollection = parentSaveCollection;
		this.m_modifiedCallback = modifiedCallback;
	}

	// Token: 0x0600139E RID: 5022 RVA: 0x00090F7C File Offset: 0x0008F17C
	public SaveFile AddSaveFile(string filePath, FileHelpers.FileSource fileSource)
	{
		SaveFile saveFile = new SaveFile(filePath, fileSource, this, new Action(this.OnModified));
		string key = saveFile.FileName + "_" + saveFile.m_source.ToString();
		SaveFile saveFile2;
		if (this.m_saveFiles.Count > 0 && this.m_saveFilesByNameAndSource.TryGetValue(key, out saveFile2))
		{
			saveFile2.AddAssociatedFiles(saveFile.AllPaths);
		}
		else
		{
			this.m_saveFiles.Add(saveFile);
			this.m_saveFilesByNameAndSource.Add(key, saveFile);
		}
		this.OnModified();
		return saveFile;
	}

	// Token: 0x0600139F RID: 5023 RVA: 0x0009100C File Offset: 0x0008F20C
	public SaveFile AddSaveFile(string[] filePaths, FileHelpers.FileSource fileSource)
	{
		SaveFile saveFile = new SaveFile(filePaths, fileSource, this, new Action(this.OnModified));
		string key = saveFile.FileName + "_" + saveFile.m_source.ToString();
		SaveFile saveFile2;
		if (this.m_saveFiles.Count > 0 && this.m_saveFilesByNameAndSource.TryGetValue(key, out saveFile2))
		{
			saveFile2.AddAssociatedFiles(saveFile.AllPaths);
		}
		else
		{
			this.m_saveFiles.Add(saveFile);
			this.m_saveFilesByNameAndSource.Add(key, saveFile);
		}
		this.OnModified();
		return saveFile;
	}

	// Token: 0x060013A0 RID: 5024 RVA: 0x0009109C File Offset: 0x0008F29C
	public void RemoveSaveFile(SaveFile saveFile)
	{
		this.m_saveFiles.Remove(saveFile);
		string key = saveFile.FileName + "_" + saveFile.m_source.ToString();
		this.m_saveFilesByNameAndSource.Remove(key);
		this.OnModified();
	}

	// Token: 0x170000AF RID: 175
	// (get) Token: 0x060013A1 RID: 5025 RVA: 0x000910EB File Offset: 0x0008F2EB
	public SaveFile PrimaryFile
	{
		get
		{
			this.EnsureSortedAndPrimaryFileDetermined();
			return this.m_primaryFile;
		}
	}

	// Token: 0x170000B0 RID: 176
	// (get) Token: 0x060013A2 RID: 5026 RVA: 0x000910F9 File Offset: 0x0008F2F9
	public SaveFile[] BackupFiles
	{
		get
		{
			this.EnsureSortedAndPrimaryFileDetermined();
			return this.m_backupFiles.ToArray();
		}
	}

	// Token: 0x170000B1 RID: 177
	// (get) Token: 0x060013A3 RID: 5027 RVA: 0x0009110C File Offset: 0x0008F30C
	public SaveFile[] AllFiles
	{
		get
		{
			return this.m_saveFiles.ToArray();
		}
	}

	// Token: 0x170000B2 RID: 178
	// (get) Token: 0x060013A4 RID: 5028 RVA: 0x0009111C File Offset: 0x0008F31C
	public ulong SizeWithBackups
	{
		get
		{
			ulong num = 0UL;
			for (int i = 0; i < this.m_saveFiles.Count; i++)
			{
				num += this.m_saveFiles[i].Size;
			}
			return num;
		}
	}

	// Token: 0x170000B3 RID: 179
	// (get) Token: 0x060013A5 RID: 5029 RVA: 0x00091157 File Offset: 0x0008F357
	public bool IsDeleted
	{
		get
		{
			return this.PrimaryFile == null;
		}
	}

	// Token: 0x170000B4 RID: 180
	// (get) Token: 0x060013A6 RID: 5030 RVA: 0x00091162 File Offset: 0x0008F362
	// (set) Token: 0x060013A7 RID: 5031 RVA: 0x0009116A File Offset: 0x0008F36A
	public string m_name { get; private set; }

	// Token: 0x170000B5 RID: 181
	// (get) Token: 0x060013A8 RID: 5032 RVA: 0x00091173 File Offset: 0x0008F373
	// (set) Token: 0x060013A9 RID: 5033 RVA: 0x0009117B File Offset: 0x0008F37B
	public SaveCollection ParentSaveCollection { get; private set; }

	// Token: 0x060013AA RID: 5034 RVA: 0x00091184 File Offset: 0x0008F384
	private void EnsureSortedAndPrimaryFileDetermined()
	{
		if (!this.m_isDirty)
		{
			return;
		}
		this.m_saveFiles.Sort(new SaveFileComparer());
		this.m_primaryFile = null;
		for (int i = 0; i < this.m_saveFiles.Count; i++)
		{
			string text;
			SaveFileType saveFileType;
			string text2;
			DateTime? dateTime;
			if (SaveSystem.GetSaveInfo(this.m_saveFiles[i].PathPrimary, out text, out saveFileType, out text2, out dateTime) && saveFileType == SaveFileType.Single && (this.m_primaryFile == null || this.m_saveFiles[i].m_source == FileHelpers.FileSource.Cloud || (this.m_saveFiles[i].m_source == FileHelpers.FileSource.Local && this.m_primaryFile.m_source == FileHelpers.FileSource.Legacy)))
			{
				this.m_primaryFile = this.m_saveFiles[i];
			}
		}
		if (this.m_primaryFile != null)
		{
			this.m_name = this.m_primaryFile.FileName;
		}
		this.m_backupFiles.Clear();
		if (this.m_primaryFile == null)
		{
			this.m_backupFiles.AddRange(this.m_saveFiles);
		}
		else
		{
			for (int j = 0; j < this.m_saveFiles.Count; j++)
			{
				if (this.m_saveFiles[j] != this.m_primaryFile)
				{
					this.m_backupFiles.Add(this.m_saveFiles[j]);
				}
			}
		}
		this.m_isDirty = false;
	}

	// Token: 0x060013AB RID: 5035 RVA: 0x000912CB File Offset: 0x0008F4CB
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

	// Token: 0x060013AC RID: 5036 RVA: 0x000912E3 File Offset: 0x0008F4E3
	private void SetDirty()
	{
		this.m_isDirty = true;
	}

	// Token: 0x04001393 RID: 5011
	private List<SaveFile> m_saveFiles = new List<SaveFile>();

	// Token: 0x04001395 RID: 5013
	private Action m_modifiedCallback;

	// Token: 0x04001396 RID: 5014
	private bool m_isDirty;

	// Token: 0x04001397 RID: 5015
	private SaveFile m_primaryFile;

	// Token: 0x04001398 RID: 5016
	private List<SaveFile> m_backupFiles = new List<SaveFile>();

	// Token: 0x04001399 RID: 5017
	private Dictionary<string, SaveFile> m_saveFilesByNameAndSource = new Dictionary<string, SaveFile>();
}
