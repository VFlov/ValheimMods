using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

// Token: 0x0200013C RID: 316
public class SaveSystem
{
	// Token: 0x060013BA RID: 5050 RVA: 0x0009158C File Offset: 0x0008F78C
	private static void EnsureCollectionsAreCreated()
	{
		if (SaveSystem.s_saveCollections != null)
		{
			return;
		}
		SaveSystem.s_saveCollections = new Dictionary<SaveDataType, SaveCollection>();
		SaveDataType[] array = new SaveDataType[]
		{
			SaveDataType.World,
			SaveDataType.Character
		};
		for (int i = 0; i < array.Length; i++)
		{
			SaveCollection saveCollection = new SaveCollection(array[i]);
			SaveSystem.s_saveCollections.Add(saveCollection.m_dataType, saveCollection);
		}
	}

	// Token: 0x060013BB RID: 5051 RVA: 0x000915E0 File Offset: 0x0008F7E0
	public static SaveWithBackups[] GetSavesByType(SaveDataType dataType)
	{
		SaveSystem.EnsureCollectionsAreCreated();
		SaveCollection saveCollection;
		if (!SaveSystem.s_saveCollections.TryGetValue(dataType, out saveCollection))
		{
			return null;
		}
		return saveCollection.Saves;
	}

	// Token: 0x060013BC RID: 5052 RVA: 0x0009160C File Offset: 0x0008F80C
	public static bool TryGetSaveByName(string name, SaveDataType dataType, out SaveWithBackups save)
	{
		SaveSystem.EnsureCollectionsAreCreated();
		SaveCollection saveCollection;
		if (!SaveSystem.s_saveCollections.TryGetValue(dataType, out saveCollection))
		{
			ZLog.LogError(string.Format("Failed to retrieve collection of type {0}!", dataType));
			save = null;
			return false;
		}
		return saveCollection.TryGetSaveByName(name, out save);
	}

	// Token: 0x060013BD RID: 5053 RVA: 0x00091650 File Offset: 0x0008F850
	public static void ForceRefreshCache()
	{
		foreach (KeyValuePair<SaveDataType, SaveCollection> keyValuePair in SaveSystem.s_saveCollections)
		{
			keyValuePair.Value.EnsureLoadedAndSorted();
		}
	}

	// Token: 0x060013BE RID: 5054 RVA: 0x000916A8 File Offset: 0x0008F8A8
	public static void InvalidateCache()
	{
		SaveSystem.EnsureCollectionsAreCreated();
		foreach (KeyValuePair<SaveDataType, SaveCollection> keyValuePair in SaveSystem.s_saveCollections)
		{
			keyValuePair.Value.InvalidateCache();
		}
	}

	// Token: 0x060013BF RID: 5055 RVA: 0x00091704 File Offset: 0x0008F904
	public static IComparer<string> GetComparerByDataType(SaveDataType dataType)
	{
		if (dataType == SaveDataType.World)
		{
			return new WorldSaveComparer();
		}
		if (dataType != SaveDataType.Character)
		{
			return null;
		}
		return new CharacterSaveComparer();
	}

	// Token: 0x060013C0 RID: 5056 RVA: 0x0009171C File Offset: 0x0008F91C
	public static string GetSavePath(SaveDataType dataType, FileHelpers.FileSource source)
	{
		if (dataType == SaveDataType.World)
		{
			return World.GetWorldSavePath(source);
		}
		if (dataType != SaveDataType.Character)
		{
			ZLog.LogError(string.Format("Reload not implemented for save data type {0}!", dataType));
			return null;
		}
		return PlayerProfile.GetCharacterFolderPath(source);
	}

	// Token: 0x060013C1 RID: 5057 RVA: 0x0009174C File Offset: 0x0008F94C
	public static bool Delete(SaveFile file)
	{
		int num = 0;
		for (int i = 0; i < file.AllPaths.Length; i++)
		{
			if (!FileHelpers.Delete(file.AllPaths[i], file.m_source))
			{
				num++;
			}
		}
		Minimap.DeleteMapTextureData(file.FileName);
		if (num > 0)
		{
			SaveSystem.InvalidateCache();
			return false;
		}
		SaveWithBackups parentSaveWithBackups = file.ParentSaveWithBackups;
		parentSaveWithBackups.RemoveSaveFile(file);
		if (parentSaveWithBackups.AllFiles.Length == 0)
		{
			parentSaveWithBackups.ParentSaveCollection.Remove(parentSaveWithBackups);
		}
		return true;
	}

	// Token: 0x060013C2 RID: 5058 RVA: 0x000917C4 File Offset: 0x0008F9C4
	public static bool Copy(SaveFile file, string newName, FileHelpers.FileSource destinationLocation = FileHelpers.FileSource.Auto)
	{
		if (destinationLocation == FileHelpers.FileSource.Auto)
		{
			destinationLocation = file.m_source;
		}
		string[] allPaths = file.AllPaths;
		string[] array = new string[allPaths.Length];
		for (int i = 0; i < allPaths.Length; i++)
		{
			string text;
			SaveFileType saveFileType;
			string str;
			DateTime? dateTime;
			if (!SaveSystem.GetSaveInfo(allPaths[i], out text, out saveFileType, out str, out dateTime))
			{
				ZLog.LogError("Failed to get save info for file " + allPaths[i]);
				return false;
			}
			string text2;
			if (!SaveSystem.TryConvertSource(allPaths[i], file.m_source, destinationLocation, out text2))
			{
				ZLog.LogError(string.Format("Failed to convert source from {0} to {1} for file {2}", file.m_source, destinationLocation, allPaths[i]));
				return false;
			}
			int num = text2.LastIndexOfAny(new char[]
			{
				'/',
				'\\'
			});
			string str2 = (num >= 0) ? text2.Substring(0, num + 1) : text2;
			array[i] = str2 + newName + str;
		}
		bool flag = false;
		for (int j = 0; j < allPaths.Length; j++)
		{
			if (!FileHelpers.Copy(allPaths[j], file.m_source, array[j], destinationLocation))
			{
				flag = true;
			}
		}
		if (flag)
		{
			SaveSystem.InvalidateCache();
		}
		else
		{
			file.ParentSaveWithBackups.AddSaveFile(array, destinationLocation);
		}
		return true;
	}

	// Token: 0x060013C3 RID: 5059 RVA: 0x000918E0 File Offset: 0x0008FAE0
	public static bool Rename(SaveFile file, string newName)
	{
		string[] allPaths = file.AllPaths;
		string[] array = new string[allPaths.Length];
		for (int i = 0; i < allPaths.Length; i++)
		{
			string text;
			SaveFileType saveFileType;
			string str;
			DateTime? dateTime;
			if (!SaveSystem.GetSaveInfo(allPaths[i], out text, out saveFileType, out str, out dateTime))
			{
				return false;
			}
			int num = allPaths[i].LastIndexOfAny(new char[]
			{
				'/',
				'\\'
			});
			string str2 = (num >= 0) ? allPaths[i].Substring(0, num + 1) : allPaths[i];
			array[i] = str2 + newName + str;
		}
		if (file.m_source == FileHelpers.FileSource.Cloud)
		{
			int num2 = -1;
			for (int j = 0; j < allPaths.Length; j++)
			{
				if (!FileHelpers.CloudMove(allPaths[j], array[j]))
				{
					num2 = j;
					break;
				}
			}
			if (num2 >= 0)
			{
				for (int k = 0; k < num2; k++)
				{
					FileHelpers.CloudMove(allPaths[k], array[k]);
				}
				SaveSystem.InvalidateCache();
				return false;
			}
		}
		else
		{
			for (int l = 0; l < allPaths.Length; l++)
			{
				File.Move(allPaths[l], array[l]);
			}
		}
		SaveWithBackups parentSaveWithBackups = file.ParentSaveWithBackups;
		parentSaveWithBackups.RemoveSaveFile(file);
		parentSaveWithBackups.AddSaveFile(array, file.m_source);
		return true;
	}

	// Token: 0x060013C4 RID: 5060 RVA: 0x000919F8 File Offset: 0x0008FBF8
	public static bool MoveSource(SaveFile file, bool isBackup, FileHelpers.FileSource destinationSource, out bool cloudQuotaExceeded)
	{
		cloudQuotaExceeded = false;
		string[] allPaths = file.AllPaths;
		string[] array = new string[allPaths.Length];
		for (int i = 0; i < allPaths.Length; i++)
		{
			if (!SaveSystem.TryConvertSource(allPaths[i], file.m_source, destinationSource, out array[i]))
			{
				ZLog.LogError(string.Format("Failed to convert source from {0} to {1} for file {2}", file.m_source, destinationSource, allPaths[i]));
				return false;
			}
		}
		if (destinationSource == FileHelpers.FileSource.Cloud && FileHelpers.OperationExceedsCloudCapacity(file.Size))
		{
			ZLog.LogWarning("This operation would exceed the cloud save quota and has therefore been aborted!");
			cloudQuotaExceeded = true;
			return false;
		}
		bool flag = false;
		int num = 0;
		for (int j = 0; j < allPaths.Length; j++)
		{
			if (!FileHelpers.Copy(allPaths[j], file.m_source, array[j], destinationSource))
			{
				flag = true;
				break;
			}
			num = j;
		}
		if (flag)
		{
			ZLog.LogError("Copying world into cloud failed, aborting move to cloud.");
			for (int k = 0; k < num; k++)
			{
				FileHelpers.Delete(array[k], FileHelpers.FileSource.Cloud);
			}
			SaveSystem.InvalidateCache();
			return false;
		}
		file.ParentSaveWithBackups.AddSaveFile(array, destinationSource);
		if (file.m_source != FileHelpers.FileSource.Cloud && !isBackup)
		{
			SaveSystem.MoveToBackup(file, DateTime.Now);
		}
		else
		{
			SaveSystem.Delete(file);
		}
		return true;
	}

	// Token: 0x060013C5 RID: 5061 RVA: 0x00091B1C File Offset: 0x0008FD1C
	public static SaveSystem.RestoreBackupResult RestoreMetaFromMostRecentBackup(SaveFile saveFile)
	{
		if (!saveFile.PathPrimary.EndsWith(".db"))
		{
			return SaveSystem.RestoreBackupResult.UnknownError;
		}
		for (int i = 0; i < saveFile.AllPaths.Length; i++)
		{
			if (saveFile.AllPaths[i].EndsWith(".fwl"))
			{
				return SaveSystem.RestoreBackupResult.AlreadyHasMeta;
			}
		}
		SaveFile saveFile2 = SaveSystem.<RestoreMetaFromMostRecentBackup>g__GetMostRecentBackupWithMeta|24_0(saveFile.ParentSaveWithBackups);
		if (saveFile2 == null)
		{
			return SaveSystem.RestoreBackupResult.NoBackup;
		}
		string text = World.GetWorldSavePath(saveFile.m_source) + "/" + saveFile.ParentSaveWithBackups.m_name + ".fwl";
		try
		{
			if (!FileHelpers.Copy(saveFile2.PathPrimary, saveFile2.m_source, text, saveFile.m_source))
			{
				SaveSystem.InvalidateCache();
				return SaveSystem.RestoreBackupResult.CopyFailed;
			}
		}
		catch (Exception ex)
		{
			ZLog.LogError("Caught exception while restoring meta from backup: " + ex.ToString());
			SaveSystem.InvalidateCache();
			return SaveSystem.RestoreBackupResult.UnknownError;
		}
		saveFile.AddAssociatedFile(text);
		return SaveSystem.RestoreBackupResult.Success;
	}

	// Token: 0x060013C6 RID: 5062 RVA: 0x00091C00 File Offset: 0x0008FE00
	public static SaveSystem.RestoreBackupResult RestoreBackup(SaveFile backup)
	{
		string text;
		SaveFileType saveFileType;
		string text2;
		DateTime? dateTime;
		if (!SaveSystem.GetSaveInfo(backup.PathPrimary, out text, out saveFileType, out text2, out dateTime))
		{
			return SaveSystem.RestoreBackupResult.UnknownError;
		}
		SaveWithBackups parentSaveWithBackups = backup.ParentSaveWithBackups;
		Minimap.DeleteMapTextureData(parentSaveWithBackups.m_name);
		if (!parentSaveWithBackups.IsDeleted && !SaveSystem.Rename(parentSaveWithBackups.PrimaryFile, parentSaveWithBackups.m_name + "_backup_restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")))
		{
			return SaveSystem.RestoreBackupResult.RenameFailed;
		}
		string newName;
		bool flag;
		if (saveFileType == SaveFileType.Single)
		{
			newName = parentSaveWithBackups.m_name + "_backup_" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
			flag = false;
		}
		else
		{
			newName = parentSaveWithBackups.m_name;
			flag = (backup.m_source == FileHelpers.FileSource.Local && saveFileType == SaveFileType.CloudBackup);
		}
		if (!SaveSystem.Copy(backup, newName, flag ? FileHelpers.FileSource.Cloud : backup.m_source))
		{
			return SaveSystem.RestoreBackupResult.CopyFailed;
		}
		return SaveSystem.RestoreBackupResult.Success;
	}

	// Token: 0x060013C7 RID: 5063 RVA: 0x00091CD0 File Offset: 0x0008FED0
	public static SaveSystem.RestoreBackupResult RestoreMostRecentBackup(SaveWithBackups save)
	{
		SaveFile saveFile = SaveSystem.<RestoreMostRecentBackup>g__GetMostRecentBackup|26_0(save);
		if (saveFile == null)
		{
			return SaveSystem.RestoreBackupResult.NoBackup;
		}
		return SaveSystem.RestoreBackup(saveFile);
	}

	// Token: 0x060013C8 RID: 5064 RVA: 0x00091CF0 File Offset: 0x0008FEF0
	public static bool CheckMove(string saveName, SaveDataType dataType, ref FileHelpers.FileSource source, DateTime now, ulong opUsage = 0UL, bool copyToNewLocation = false)
	{
		SaveFile saveFile = null;
		SaveWithBackups saveWithBackups;
		if (SaveSystem.TryGetSaveByName(saveName, dataType, out saveWithBackups) && !saveWithBackups.IsDeleted && saveWithBackups.PrimaryFile.m_source == source)
		{
			saveFile = saveWithBackups.PrimaryFile;
		}
		if (source == FileHelpers.FileSource.Legacy)
		{
			if (FileHelpers.CloudStorageEnabled && !FileHelpers.OperationExceedsCloudCapacity(opUsage))
			{
				source = FileHelpers.FileSource.Cloud;
			}
			else
			{
				source = FileHelpers.FileSource.Local;
			}
			if (saveFile != null)
			{
				if (copyToNewLocation)
				{
					SaveSystem.Copy(saveFile, saveName, source);
				}
				SaveSystem.MoveToBackup(saveFile, now);
			}
			return true;
		}
		if (source == FileHelpers.FileSource.Local && FileHelpers.CloudStorageEnabled && !FileHelpers.LocalStorageSupported && !FileHelpers.OperationExceedsCloudCapacity(opUsage))
		{
			source = FileHelpers.FileSource.Cloud;
			if (saveFile != null)
			{
				if (copyToNewLocation)
				{
					SaveSystem.Copy(saveFile, saveName, source);
				}
				SaveSystem.MoveToBackup(saveFile, now);
			}
			return true;
		}
		return false;
	}

	// Token: 0x060013C9 RID: 5065 RVA: 0x00091D9B File Offset: 0x0008FF9B
	private static bool MoveToBackup(SaveFile saveFile, DateTime now)
	{
		return SaveSystem.Rename(saveFile, saveFile.ParentSaveWithBackups.m_name + "_backup_" + now.ToString("yyyyMMdd-HHmmss"));
	}

	// Token: 0x060013CA RID: 5066 RVA: 0x00091DC4 File Offset: 0x0008FFC4
	public static bool CreateBackup(SaveFile saveFile, DateTime now, FileHelpers.FileSource source = FileHelpers.FileSource.Auto)
	{
		return SaveSystem.Copy(saveFile, saveFile.ParentSaveWithBackups.m_name + "_backup_" + now.ToString("yyyyMMdd-HHmmss"), source);
	}

	// Token: 0x060013CB RID: 5067 RVA: 0x00091DF0 File Offset: 0x0008FFF0
	public static bool ConsiderBackup(string saveName, SaveDataType dataType, DateTime now, int backupCount, int backupShort, int backupLong, int waitFirstBackup, float worldTime = 0f)
	{
		ZLog.Log(string.Format("Considering autobackup. World time: {0}, short time: {1}, long time: {2}, backup count: {3}", new object[]
		{
			worldTime,
			backupShort,
			backupLong,
			backupCount
		}));
		if (worldTime > 0f && worldTime < (float)waitFirstBackup)
		{
			ZLog.Log("Skipping backup. World session not long enough.");
			return false;
		}
		if (backupCount == 1)
		{
			backupCount = 2;
		}
		SaveWithBackups saveWithBackups;
		if (!SaveSystem.TryGetSaveByName(saveName, dataType, out saveWithBackups))
		{
			ZLog.LogError("Failed to retrieve save with name " + saveName + "!");
			return false;
		}
		if (saveWithBackups.IsDeleted)
		{
			ZLog.LogError("Save with name " + saveName + " is deleted, can't manage auto-backups!");
			return false;
		}
		List<SaveFile> list = new List<SaveFile>();
		foreach (SaveFile saveFile in saveWithBackups.BackupFiles)
		{
			string text;
			SaveFileType saveFileType;
			string text2;
			DateTime? dateTime;
			if (SaveSystem.GetSaveInfo(saveFile.PathPrimary, out text, out saveFileType, out text2, out dateTime) && saveFileType == SaveFileType.AutoBackup)
			{
				list.Add(saveFile);
			}
		}
		list.Sort((SaveFile a, SaveFile b) => b.LastModified.CompareTo(a.LastModified));
		while (list.Count > backupCount)
		{
			list.RemoveAt(list.Count - 1);
		}
		SaveFile saveFile2 = null;
		if (list.Count == 0)
		{
			ZLog.Log("Creating first autobackup");
		}
		else
		{
			if (!(now - TimeSpan.FromSeconds((double)backupShort) > list[0].LastModified))
			{
				ZLog.Log("No autobackup needed yet...");
				return false;
			}
			if (list.Count == 1)
			{
				ZLog.Log("Creating second autobackup for reference");
			}
			else if (now - TimeSpan.FromSeconds((double)backupLong) > list[1].LastModified)
			{
				if (list.Count < backupCount)
				{
					ZLog.Log("Creating new backup since we haven't reached our desired amount");
				}
				else
				{
					saveFile2 = list[list.Count - 1];
					ZLog.Log("Time to overwrite our last autobackup");
				}
			}
			else
			{
				saveFile2 = list[0];
				ZLog.Log("Overwrite our newest autobackup since the second one isn't so old");
			}
		}
		if (saveFile2 != null)
		{
			ZLog.Log("Replacing backup file: " + saveFile2.FileName);
			if (!SaveSystem.Delete(saveFile2))
			{
				ZLog.LogError("Failed to delete backup " + saveFile2.FileName + "!");
				return false;
			}
		}
		string text3 = saveName + "_backup_auto-" + now.ToString("yyyyMMddHHmmss");
		ZLog.Log("Saving backup at: " + text3);
		if (!SaveSystem.Copy(saveWithBackups.PrimaryFile, text3, saveWithBackups.PrimaryFile.m_source))
		{
			ZLog.LogError("Failed to copy save with name " + saveName + " to auto-backup!");
			return false;
		}
		return true;
	}

	// Token: 0x060013CC RID: 5068 RVA: 0x0009207C File Offset: 0x0009027C
	public static bool HasBackupWithMeta(SaveWithBackups save)
	{
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (SaveSystem.IsRestorableMeta(save.BackupFiles[i]))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060013CD RID: 5069 RVA: 0x000920B0 File Offset: 0x000902B0
	public static bool HasRestorableBackup(SaveWithBackups save)
	{
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (SaveSystem.IsRestorableBackup(save.BackupFiles[i]))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060013CE RID: 5070 RVA: 0x000920E4 File Offset: 0x000902E4
	private static bool IsRestorableBackup(SaveFile backup)
	{
		SaveDataType dataType = backup.ParentSaveWithBackups.ParentSaveCollection.m_dataType;
		if (dataType != SaveDataType.World)
		{
			if (dataType != SaveDataType.Character)
			{
				ZLog.LogError(string.Format("Not implemented for {0}!", backup.ParentSaveWithBackups.ParentSaveCollection.m_dataType));
				return false;
			}
			if (!backup.PathPrimary.EndsWith(".fch"))
			{
				return false;
			}
		}
		else
		{
			if (!backup.PathPrimary.EndsWith(".fwl"))
			{
				return false;
			}
			if (backup.PathsAssociated.Length < 1 || !backup.PathsAssociated[0].EndsWith(".db"))
			{
				return false;
			}
		}
		for (int i = 0; i < backup.AllPaths.Length; i++)
		{
			if (FileHelpers.IsFileCorrupt(backup.AllPaths[i], backup.m_source))
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x060013CF RID: 5071 RVA: 0x000921A5 File Offset: 0x000903A5
	private static bool IsRestorableMeta(SaveFile backup)
	{
		return backup.PathPrimary.EndsWith(".fwl") && !FileHelpers.IsFileCorrupt(backup.PathPrimary, backup.m_source);
	}

	// Token: 0x060013D0 RID: 5072 RVA: 0x000921D4 File Offset: 0x000903D4
	public static bool IsCorrupt(SaveFile file)
	{
		for (int i = 0; i < file.AllPaths.Length; i++)
		{
			if (FileHelpers.IsFileCorrupt(file.AllPaths[i], file.m_source))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060013D1 RID: 5073 RVA: 0x0009220C File Offset: 0x0009040C
	public static bool IsWorldWithMissingMetaFile(SaveFile file)
	{
		string text;
		SaveFileType saveFileType;
		string a;
		DateTime? dateTime;
		return file.ParentSaveWithBackups.ParentSaveCollection.m_dataType == SaveDataType.World && SaveSystem.GetSaveInfo(file.PathPrimary, out text, out saveFileType, out a, out dateTime) && a != ".fwl";
	}

	// Token: 0x060013D2 RID: 5074 RVA: 0x00092250 File Offset: 0x00090450
	public static bool GetSaveInfo(string path, out string saveName, out SaveFileType saveFileType, out string actualFileEnding, out DateTime? timestamp)
	{
		string text = SaveSystem.RemoveDirectoryPart(path);
		int[] array = text.AllIndicesOf('.');
		if (array.Length == 0)
		{
			saveName = "";
			actualFileEnding = "";
			saveFileType = SaveFileType.Single;
			timestamp = null;
			return false;
		}
		if (text.EndsWith(".old"))
		{
			saveName = text.Substring(0, array[0]);
			saveFileType = SaveFileType.OldBackup;
			actualFileEnding = ((array.Length >= 2) ? text.Substring(array[0], array[array.Length - 1] - array[0]) : "");
			timestamp = null;
			return true;
		}
		string text2 = text.Substring(0, array[array.Length - 1]);
		timestamp = null;
		if (text2.Length >= 14)
		{
			char[] array2 = new char[14];
			int num = array2.Length;
			int num2 = text2.Length - 1;
			while (num2 >= 0 && num > 0)
			{
				if (text2[num2] != '-')
				{
					num--;
					array2[num] = text2[num2];
				}
				num2--;
			}
			if (num == 0)
			{
				string text3 = new string(array2);
				int year;
				int month;
				int day;
				int hour;
				int minute;
				int second;
				if (text3.Length >= 14 && int.TryParse(text3.Substring(0, 4), out year) && int.TryParse(text3.Substring(4, 2), out month) && int.TryParse(text3.Substring(6, 2), out day) && int.TryParse(text3.Substring(8, 2), out hour) && int.TryParse(text3.Substring(10, 2), out minute) && int.TryParse(text3.Substring(12, 2), out second))
				{
					try
					{
						timestamp = new DateTime?(new DateTime(year, month, day, hour, minute, second));
					}
					catch (ArgumentOutOfRangeException)
					{
						timestamp = null;
					}
				}
			}
		}
		actualFileEnding = ((array.Length != 0) ? text.Substring(array[array.Length - 1]) : "");
		if (timestamp == null)
		{
			saveFileType = SaveFileType.Single;
			saveName = text2;
			return true;
		}
		int[] array3 = text.AllIndicesOf('_');
		if (array3.Length >= 1)
		{
			if (array3.Length >= 2 && text.Length - array3[array3.Length - 2] >= "_backup_".Length && text.Substring(array3[array3.Length - 2], "_backup_".Length) == "_backup_")
			{
				if (text.Length - array3[array3.Length - 2] >= "_backup_auto-".Length && text.Substring(array3[array3.Length - 2], "_backup_auto-".Length) == "_backup_auto-")
				{
					saveFileType = SaveFileType.AutoBackup;
				}
				else if (text.Length - array3[array3.Length - 2] >= "_backup_cloud-".Length && text.Substring(array3[array3.Length - 2], "_backup_cloud-".Length) == "_backup_cloud-")
				{
					saveFileType = SaveFileType.CloudBackup;
				}
				else if (text.Length - array3[array3.Length - 2] >= "_backup_restore-".Length && text.Substring(array3[array3.Length - 2], "_backup_restore-".Length) == "_backup_restore-")
				{
					saveFileType = SaveFileType.RestoredBackup;
				}
				else
				{
					saveFileType = SaveFileType.StandardBackup;
				}
			}
			else
			{
				saveFileType = SaveFileType.Rolling;
			}
			saveName = text.Substring(0, array3[array3.Length - ((saveFileType == SaveFileType.Rolling) ? 1 : 2)]);
			if (saveName.Length == 0)
			{
				timestamp = null;
				saveFileType = SaveFileType.Single;
				saveName = text2;
			}
		}
		else
		{
			timestamp = null;
			saveFileType = SaveFileType.Single;
			saveName = text2;
		}
		return true;
	}

	// Token: 0x060013D3 RID: 5075 RVA: 0x000925A8 File Offset: 0x000907A8
	public static string RemoveDirectoryPart(string path)
	{
		int num = path.LastIndexOfAny(new char[]
		{
			'/',
			'\\'
		});
		if (num >= 0)
		{
			return path.Substring(num + 1);
		}
		return path;
	}

	// Token: 0x060013D4 RID: 5076 RVA: 0x000925DC File Offset: 0x000907DC
	public static bool TryConvertSource(string sourcePath, FileHelpers.FileSource sourceLocation, FileHelpers.FileSource destinationLocation, out string destinationPath)
	{
		string text = SaveSystem.NormalizePath(sourcePath, sourceLocation);
		if (sourceLocation == destinationLocation)
		{
			destinationPath = text;
			return true;
		}
		string text2 = SaveSystem.NormalizePath(World.GetWorldSavePath(sourceLocation), sourceLocation);
		if (text.StartsWith(text2))
		{
			destinationPath = SaveSystem.NormalizePath(World.GetWorldSavePath(destinationLocation), destinationLocation) + text.Substring(text2.Length);
			return true;
		}
		string text3 = SaveSystem.NormalizePath(PlayerProfile.GetCharacterFolderPath(sourceLocation), sourceLocation);
		if (text.StartsWith(text3))
		{
			destinationPath = SaveSystem.NormalizePath(PlayerProfile.GetCharacterFolderPath(destinationLocation), destinationLocation) + text.Substring(text3.Length);
			return true;
		}
		destinationPath = null;
		return false;
	}

	// Token: 0x060013D5 RID: 5077 RVA: 0x0009266C File Offset: 0x0009086C
	public static string NormalizePath(string path, FileHelpers.FileSource source)
	{
		char[] array = new char[path.Length];
		int num = 0;
		int i = 0;
		while (i < path.Length)
		{
			char c = path[i];
			if (c == '\\')
			{
				c = '/';
			}
			if (c != '/')
			{
				goto IL_3A;
			}
			if (num > 0)
			{
				if (array[num - 1] != '/')
				{
					goto IL_3A;
				}
			}
			else if (source != FileHelpers.FileSource.Cloud)
			{
				goto IL_3A;
			}
			IL_42:
			i++;
			continue;
			IL_3A:
			array[num++] = c;
			goto IL_42;
		}
		return new string(array, 0, num);
	}

	// Token: 0x060013D6 RID: 5078 RVA: 0x000926D0 File Offset: 0x000908D0
	public static string NormalizePath(string path)
	{
		char[] array = new char[path.Length];
		int num = 0;
		foreach (char c in path)
		{
			if (c == '\\')
			{
				c = '/';
			}
			if (c != '/' || num <= 0 || array[num - 1] != '/')
			{
				array[num++] = c;
			}
		}
		return new string(array, 0, num);
	}

	// Token: 0x060013D7 RID: 5079 RVA: 0x0009272E File Offset: 0x0009092E
	public static void ClearWorldListCache(bool reload)
	{
		SaveSystem.m_cachedWorlds.Clear();
		if (reload)
		{
			SaveSystem.GetWorldList();
		}
	}

	// Token: 0x060013D8 RID: 5080 RVA: 0x00092744 File Offset: 0x00090944
	public static List<World> GetWorldList()
	{
		SaveWithBackups[] savesByType = SaveSystem.GetSavesByType(SaveDataType.World);
		List<World> list = new List<World>();
		HashSet<FilePathAndSource> hashSet = new HashSet<FilePathAndSource>();
		for (int i = 0; i < savesByType.Length; i++)
		{
			if (!savesByType[i].IsDeleted)
			{
				if (savesByType[i].PrimaryFile.PathPrimary.EndsWith(".db"))
				{
					string text;
					SaveFileType saveFileType;
					string text2;
					DateTime? dateTime;
					if (SaveSystem.GetSaveInfo(savesByType[i].PrimaryFile.PathPrimary, out text, out saveFileType, out text2, out dateTime))
					{
						World world = new World(savesByType[i], FileHelpers.IsFileCorrupt(savesByType[i].PrimaryFile.PathPrimary, savesByType[i].PrimaryFile.m_source) ? World.SaveDataError.Corrupt : World.SaveDataError.MissingMeta);
						list.Add(world);
					}
				}
				else if (savesByType[i].PrimaryFile.PathPrimary.EndsWith(".fwl"))
				{
					FilePathAndSource filePathAndSource = new FilePathAndSource(savesByType[i].PrimaryFile.PathPrimary, savesByType[i].PrimaryFile.m_source);
					World world;
					if (SaveSystem.m_cachedWorlds.TryGetValue(filePathAndSource, out world))
					{
						list.Add(world);
						hashSet.Add(filePathAndSource);
					}
					else
					{
						world = World.LoadWorld(savesByType[i]);
						if (world != null)
						{
							list.Add(world);
							hashSet.Add(filePathAndSource);
							SaveSystem.m_cachedWorlds.Add(filePathAndSource, world);
						}
					}
				}
			}
		}
		List<FilePathAndSource> list2 = new List<FilePathAndSource>();
		foreach (KeyValuePair<FilePathAndSource, World> keyValuePair in SaveSystem.m_cachedWorlds)
		{
			FilePathAndSource key = keyValuePair.Key;
			if (!hashSet.Contains(key))
			{
				list2.Add(key);
			}
		}
		for (int j = 0; j < list2.Count; j++)
		{
			SaveSystem.m_cachedWorlds.Remove(list2[j]);
		}
		return list;
	}

	// Token: 0x060013D9 RID: 5081 RVA: 0x0009291C File Offset: 0x00090B1C
	public static List<PlayerProfile> GetAllPlayerProfiles()
	{
		SaveWithBackups[] savesByType = SaveSystem.GetSavesByType(SaveDataType.Character);
		List<PlayerProfile> list = new List<PlayerProfile>();
		for (int i = 0; i < savesByType.Length; i++)
		{
			if (!savesByType[i].IsDeleted)
			{
				PlayerProfile playerProfile = new PlayerProfile(savesByType[i].m_name, savesByType[i].PrimaryFile.m_source);
				if (!playerProfile.Load())
				{
					ZLog.Log("Failed to load " + savesByType[i].m_name);
				}
				else
				{
					list.Add(playerProfile);
				}
			}
		}
		return list;
	}

	// Token: 0x060013DA RID: 5082 RVA: 0x00092994 File Offset: 0x00090B94
	public static bool CanSaveToCloudStorage(World world, PlayerProfile playerProfile)
	{
		bool flag = world != null && (!FileHelpers.LocalStorageSupported || world.m_fileSource == FileHelpers.FileSource.Cloud || (FileHelpers.CloudStorageEnabled && world.m_fileSource == FileHelpers.FileSource.Legacy));
		bool flag2 = playerProfile != null && (!FileHelpers.LocalStorageSupported || playerProfile.m_fileSource == FileHelpers.FileSource.Cloud || (FileHelpers.CloudStorageEnabled && playerProfile.m_fileSource == FileHelpers.FileSource.Legacy));
		if (!flag && !flag2)
		{
			return true;
		}
		ulong num = 0UL;
		if (flag)
		{
			string metaPath = world.GetMetaPath(world.m_fileSource);
			string dbpath = world.GetDBPath(world.m_fileSource);
			num += 104857600UL;
			if (FileHelpers.Exists(metaPath, world.m_fileSource))
			{
				num += FileHelpers.GetFileSize(metaPath, world.m_fileSource) * 2UL;
				if (FileHelpers.Exists(dbpath, world.m_fileSource))
				{
					num += FileHelpers.GetFileSize(dbpath, world.m_fileSource) * 2UL;
				}
			}
			else
			{
				ZLog.LogError("World save file doesn't exist! Using less accurate storage usage estimate.");
			}
		}
		if (flag2)
		{
			string path = playerProfile.GetPath();
			num += 2097152UL;
			if (FileHelpers.Exists(path, playerProfile.m_fileSource))
			{
				num += FileHelpers.GetFileSize(path, playerProfile.m_fileSource) * 2UL;
			}
			else
			{
				ZLog.LogError("Player save file doesn't exist! Using less accurate storage usage estimate.");
			}
		}
		return !FileHelpers.OperationExceedsCloudCapacity(num);
	}

	// Token: 0x060013DD RID: 5085 RVA: 0x00092AE8 File Offset: 0x00090CE8
	[CompilerGenerated]
	internal static SaveFile <RestoreMetaFromMostRecentBackup>g__GetMostRecentBackupWithMeta|24_0(SaveWithBackups save)
	{
		int num = -1;
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (SaveSystem.IsRestorableMeta(save.BackupFiles[i]) && (num < 0 || !(save.BackupFiles[i].LastModified <= save.BackupFiles[num].LastModified)))
			{
				num = i;
			}
		}
		if (num < 0)
		{
			return null;
		}
		return save.BackupFiles[num];
	}

	// Token: 0x060013DE RID: 5086 RVA: 0x00092B50 File Offset: 0x00090D50
	[CompilerGenerated]
	internal static SaveFile <RestoreMostRecentBackup>g__GetMostRecentBackup|26_0(SaveWithBackups save)
	{
		int num = -1;
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (SaveSystem.IsRestorableBackup(save.BackupFiles[i]) && (num < 0 || !(save.BackupFiles[i].LastModified <= save.BackupFiles[num].LastModified)))
			{
				num = i;
			}
		}
		if (num < 0)
		{
			return null;
		}
		return save.BackupFiles[num];
	}

	// Token: 0x0400139F RID: 5023
	public const string newNaming = ".new";

	// Token: 0x040013A0 RID: 5024
	public const string oldNaming = ".old";

	// Token: 0x040013A1 RID: 5025
	public const char fileNameSplitChar = '_';

	// Token: 0x040013A2 RID: 5026
	public const string backupNaming = "_backup_";

	// Token: 0x040013A3 RID: 5027
	public const string backupAutoNaming = "_backup_auto-";

	// Token: 0x040013A4 RID: 5028
	public const string backupRestoreNaming = "_backup_restore-";

	// Token: 0x040013A5 RID: 5029
	public const string backupCloudNaming = "_backup_cloud-";

	// Token: 0x040013A6 RID: 5030
	public const string characterFileEnding = ".fch";

	// Token: 0x040013A7 RID: 5031
	public const string worldMetaFileEnding = ".fwl";

	// Token: 0x040013A8 RID: 5032
	public const string worldDbFileEnding = ".db";

	// Token: 0x040013A9 RID: 5033
	private const double maximumBackupTimestampDifference = 10.0;

	// Token: 0x040013AA RID: 5034
	private static Dictionary<SaveDataType, SaveCollection> s_saveCollections = null;

	// Token: 0x040013AB RID: 5035
	private const bool useWorldListCache = true;

	// Token: 0x040013AC RID: 5036
	private static Dictionary<FilePathAndSource, World> m_cachedWorlds = new Dictionary<FilePathAndSource, World>();

	// Token: 0x0200032C RID: 812
	public enum RestoreBackupResult
	{
		// Token: 0x04002430 RID: 9264
		Success,
		// Token: 0x04002431 RID: 9265
		UnknownError,
		// Token: 0x04002432 RID: 9266
		NoBackup,
		// Token: 0x04002433 RID: 9267
		RenameFailed,
		// Token: 0x04002434 RID: 9268
		CopyFailed,
		// Token: 0x04002435 RID: 9269
		AlreadyHasMeta
	}
}
