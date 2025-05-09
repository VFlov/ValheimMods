using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

// Token: 0x0200013B RID: 315
public class SaveCollection
{
	// Token: 0x060013AD RID: 5037 RVA: 0x000912EC File Offset: 0x0008F4EC
	public SaveCollection(SaveDataType dataType)
	{
		this.m_dataType = dataType;
	}

	// Token: 0x170000B6 RID: 182
	// (get) Token: 0x060013AE RID: 5038 RVA: 0x0009131D File Offset: 0x0008F51D
	public SaveWithBackups[] Saves
	{
		get
		{
			this.EnsureLoadedAndSorted();
			return this.m_saves.ToArray();
		}
	}

	// Token: 0x060013AF RID: 5039 RVA: 0x00091330 File Offset: 0x0008F530
	public void Add(SaveWithBackups save)
	{
		this.m_saves.Add(save);
		this.SetNeedsSort();
	}

	// Token: 0x060013B0 RID: 5040 RVA: 0x00091344 File Offset: 0x0008F544
	public void Remove(SaveWithBackups save)
	{
		this.m_saves.Remove(save);
		this.SetNeedsSort();
	}

	// Token: 0x060013B1 RID: 5041 RVA: 0x00091359 File Offset: 0x0008F559
	public void EnsureLoadedAndSorted()
	{
		this.EnsureLoaded();
		if (this.m_needsSort)
		{
			this.Sort();
		}
	}

	// Token: 0x060013B2 RID: 5042 RVA: 0x0009136F File Offset: 0x0008F56F
	private void EnsureLoaded()
	{
		if (this.m_needsReload)
		{
			this.Reload();
		}
	}

	// Token: 0x060013B3 RID: 5043 RVA: 0x0009137F File Offset: 0x0008F57F
	public void InvalidateCache()
	{
		this.m_needsReload = true;
	}

	// Token: 0x060013B4 RID: 5044 RVA: 0x00091388 File Offset: 0x0008F588
	public bool TryGetSaveByName(string name, out SaveWithBackups save)
	{
		this.EnsureLoaded();
		return this.m_savesByName.TryGetValue(name, out save);
	}

	// Token: 0x060013B5 RID: 5045 RVA: 0x000913A0 File Offset: 0x0008F5A0
	private void Reload()
	{
		this.m_saves.Clear();
		this.m_savesByName.Clear();
		List<string> list = new List<string>();
		if (FileHelpers.CloudStorageEnabled)
		{
			SaveCollection.<Reload>g__GetAllFilesInSource|14_0(this.m_dataType, FileHelpers.FileSource.Cloud, ref list);
		}
		int count = list.Count;
		if (Directory.Exists(SaveSystem.GetSavePath(this.m_dataType, FileHelpers.FileSource.Local)))
		{
			SaveCollection.<Reload>g__GetAllFilesInSource|14_0(this.m_dataType, FileHelpers.FileSource.Local, ref list);
		}
		int count2 = list.Count;
		if (Directory.Exists(SaveSystem.GetSavePath(this.m_dataType, FileHelpers.FileSource.Legacy)))
		{
			SaveCollection.<Reload>g__GetAllFilesInSource|14_0(this.m_dataType, FileHelpers.FileSource.Legacy, ref list);
		}
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			string text2;
			SaveFileType saveFileType;
			string a;
			DateTime? dateTime;
			if (SaveSystem.GetSaveInfo(text, out text2, out saveFileType, out a, out dateTime))
			{
				FileHelpers.FileSource fileSource = SaveCollection.<Reload>g__SourceByIndexAndEntryCount|14_1(count, count2, i);
				if (fileSource != FileHelpers.FileSource.Cloud)
				{
					SaveDataType dataType = this.m_dataType;
					if (dataType != SaveDataType.World)
					{
						if (dataType != SaveDataType.Character)
						{
							ZLog.LogError(string.Format("File type filter not implemented for data type {0}!", this.m_dataType));
						}
						else if (a != ".fch")
						{
							goto IL_161;
						}
					}
					else if (a != ".fwl" && a != ".db")
					{
						goto IL_161;
					}
				}
				SaveWithBackups saveWithBackups;
				if (!this.m_savesByName.TryGetValue(text2, out saveWithBackups))
				{
					saveWithBackups = new SaveWithBackups(text2, this, new Action(this.SetNeedsSort));
					this.m_saves.Add(saveWithBackups);
					this.m_savesByName.Add(text2, saveWithBackups);
				}
				saveWithBackups.AddSaveFile(text, fileSource);
			}
			IL_161:;
		}
		this.m_needsReload = false;
		this.SetNeedsSort();
	}

	// Token: 0x060013B6 RID: 5046 RVA: 0x0009152B File Offset: 0x0008F72B
	private void Sort()
	{
		this.m_saves.Sort(new SaveWithBackupsComparer());
		this.m_needsSort = false;
	}

	// Token: 0x060013B7 RID: 5047 RVA: 0x00091544 File Offset: 0x0008F744
	private void SetNeedsSort()
	{
		this.m_needsSort = true;
	}

	// Token: 0x060013B8 RID: 5048 RVA: 0x00091550 File Offset: 0x0008F750
	[CompilerGenerated]
	internal static bool <Reload>g__GetAllFilesInSource|14_0(SaveDataType dataType, FileHelpers.FileSource source, ref List<string> listToAddTo)
	{
		string savePath = SaveSystem.GetSavePath(dataType, source);
		string[] files = FileHelpers.GetFiles(source, savePath, null, null);
		if (files == null)
		{
			return false;
		}
		listToAddTo.AddRange(files);
		return true;
	}

	// Token: 0x060013B9 RID: 5049 RVA: 0x0009157D File Offset: 0x0008F77D
	[CompilerGenerated]
	internal static FileHelpers.FileSource <Reload>g__SourceByIndexAndEntryCount|14_1(int cloudEntries, int localEntries, int i)
	{
		if (i < cloudEntries)
		{
			return FileHelpers.FileSource.Cloud;
		}
		if (i < localEntries)
		{
			return FileHelpers.FileSource.Local;
		}
		return FileHelpers.FileSource.Legacy;
	}

	// Token: 0x0400139A RID: 5018
	public readonly SaveDataType m_dataType;

	// Token: 0x0400139B RID: 5019
	private List<SaveWithBackups> m_saves = new List<SaveWithBackups>();

	// Token: 0x0400139C RID: 5020
	private Dictionary<string, SaveWithBackups> m_savesByName = new Dictionary<string, SaveWithBackups>(StringComparer.OrdinalIgnoreCase);

	// Token: 0x0400139D RID: 5021
	private bool m_needsSort;

	// Token: 0x0400139E RID: 5022
	private bool m_needsReload = true;
}
