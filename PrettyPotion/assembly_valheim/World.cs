using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Token: 0x02000153 RID: 339
public class World
{
	// Token: 0x06001475 RID: 5237 RVA: 0x00095B05 File Offset: 0x00093D05
	public World()
	{
	}

	// Token: 0x06001476 RID: 5238 RVA: 0x00095B40 File Offset: 0x00093D40
	public World(SaveWithBackups save, World.SaveDataError dataError)
	{
		this.m_fileName = (this.m_name = save.m_name);
		this.m_dataError = dataError;
		this.m_fileSource = save.PrimaryFile.m_source;
	}

	// Token: 0x06001477 RID: 5239 RVA: 0x00095BB4 File Offset: 0x00093DB4
	public World(string name, string seed)
	{
		this.m_name = name;
		this.m_fileName = name;
		this.m_seedName = seed;
		this.m_seed = ((this.m_seedName == "") ? 0 : this.m_seedName.GetStableHashCode());
		this.m_uid = (long)name.GetStableHashCode() + Utils.GenerateUID();
		this.m_worldGenVersion = 2;
	}

	// Token: 0x06001478 RID: 5240 RVA: 0x00095C51 File Offset: 0x00093E51
	public static string GetWorldSavePath(FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		return Utils.GetSaveDataPath(fileSource) + ((fileSource == FileHelpers.FileSource.Local) ? "/worlds_local" : "/worlds");
	}

	// Token: 0x06001479 RID: 5241 RVA: 0x00095C70 File Offset: 0x00093E70
	public static void RemoveWorld(string name, FileHelpers.FileSource fileSource)
	{
		SaveWithBackups saveWithBackups;
		if (SaveSystem.TryGetSaveByName(name, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted)
		{
			SaveSystem.Delete(saveWithBackups.PrimaryFile);
		}
	}

	// Token: 0x0600147A RID: 5242 RVA: 0x00095C9C File Offset: 0x00093E9C
	public string GetRootPath(FileHelpers.FileSource fileSource)
	{
		return World.GetWorldSavePath(fileSource) + "/" + this.m_fileName;
	}

	// Token: 0x0600147B RID: 5243 RVA: 0x00095CB4 File Offset: 0x00093EB4
	public string GetDBPath()
	{
		return this.GetDBPath(this.m_fileSource);
	}

	// Token: 0x0600147C RID: 5244 RVA: 0x00095CC2 File Offset: 0x00093EC2
	public string GetDBPath(FileHelpers.FileSource fileSource)
	{
		return World.GetWorldSavePath(fileSource) + "/" + this.m_fileName + ".db";
	}

	// Token: 0x0600147D RID: 5245 RVA: 0x00095CDF File Offset: 0x00093EDF
	public static string GetDBPath(string name, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		return World.GetWorldSavePath(fileSource) + "/" + name + ".db";
	}

	// Token: 0x0600147E RID: 5246 RVA: 0x00095CF7 File Offset: 0x00093EF7
	public string GetMetaPath()
	{
		return this.GetMetaPath(this.m_fileSource);
	}

	// Token: 0x0600147F RID: 5247 RVA: 0x00095D05 File Offset: 0x00093F05
	public string GetMetaPath(FileHelpers.FileSource fileSource)
	{
		return World.GetWorldSavePath(fileSource) + "/" + this.m_fileName + ".fwl";
	}

	// Token: 0x06001480 RID: 5248 RVA: 0x00095D22 File Offset: 0x00093F22
	public static string GetMetaPath(string name, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		return World.GetWorldSavePath(fileSource) + "/" + name + ".fwl";
	}

	// Token: 0x06001481 RID: 5249 RVA: 0x00095D3C File Offset: 0x00093F3C
	public static bool HaveWorld(string name)
	{
		SaveWithBackups saveWithBackups;
		return SaveSystem.TryGetSaveByName(name, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted;
	}

	// Token: 0x06001482 RID: 5250 RVA: 0x00095D5F File Offset: 0x00093F5F
	public static World GetMenuWorld()
	{
		return new World("menu", "")
		{
			m_menu = true
		};
	}

	// Token: 0x06001483 RID: 5251 RVA: 0x00095D77 File Offset: 0x00093F77
	public static World GetEditorWorld()
	{
		return new World("editor", "");
	}

	// Token: 0x06001484 RID: 5252 RVA: 0x00095D88 File Offset: 0x00093F88
	public static string GenerateSeed()
	{
		string text = "";
		for (int i = 0; i < 10; i++)
		{
			text += "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789"[UnityEngine.Random.Range(0, "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789".Length)].ToString();
		}
		return text;
	}

	// Token: 0x06001485 RID: 5253 RVA: 0x00095DD4 File Offset: 0x00093FD4
	public static World GetCreateWorld(string name, FileHelpers.FileSource source)
	{
		ZLog.Log("Get create world " + name);
		SaveWithBackups saveWithBackups;
		World world;
		if (SaveSystem.TryGetSaveByName(name, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted)
		{
			world = World.LoadWorld(saveWithBackups);
			if (world.m_dataError == World.SaveDataError.None)
			{
				return world;
			}
			ZLog.LogError(string.Format("Failed to load world with name \"{0}\", data error {1}.", name, world.m_dataError));
		}
		ZLog.Log(" creating");
		world = new World(name, World.GenerateSeed());
		world.m_fileSource = source;
		world.SaveWorldMetaData(DateTime.Now);
		return world;
	}

	// Token: 0x06001486 RID: 5254 RVA: 0x00095E5C File Offset: 0x0009405C
	public static World GetDevWorld()
	{
		SaveWithBackups saveWithBackups;
		World world;
		if (SaveSystem.TryGetSaveByName(Game.instance.m_devWorldName, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted)
		{
			world = World.LoadWorld(saveWithBackups);
			if (world.m_dataError == World.SaveDataError.None)
			{
				return world;
			}
			ZLog.Log(string.Format("Failed to load dev world, data error {0}. Creating...", world.m_dataError));
		}
		world = new World(Game.instance.m_devWorldName, Game.instance.m_devWorldSeed);
		world.m_fileSource = FileHelpers.FileSource.Local;
		world.SaveWorldMetaData(DateTime.Now);
		return world;
	}

	// Token: 0x06001487 RID: 5255 RVA: 0x00095EE0 File Offset: 0x000940E0
	public void SaveWorldMetaData(DateTime backupTimestamp)
	{
		bool flag;
		FileWriter fileWriter;
		this.SaveWorldMetaData(backupTimestamp, true, out flag, out fileWriter);
	}

	// Token: 0x06001488 RID: 5256 RVA: 0x00095EFC File Offset: 0x000940FC
	public void SaveWorldMetaData(DateTime now, bool considerBackup, out bool cloudSaveFailed, out FileWriter metaWriter)
	{
		this.GetDBPath();
		SaveSystem.CheckMove(this.m_fileName, SaveDataType.World, ref this.m_fileSource, now, 0UL, false);
		ZPackage zpackage = new ZPackage();
		zpackage.Write(35);
		zpackage.Write(this.m_name);
		zpackage.Write(this.m_seedName);
		zpackage.Write(this.m_seed);
		zpackage.Write(this.m_uid);
		zpackage.Write(this.m_worldGenVersion);
		zpackage.Write(this.m_needsDB);
		zpackage.Write(this.m_startingGlobalKeys.Count);
		for (int i = 0; i < this.m_startingGlobalKeys.Count; i++)
		{
			zpackage.Write(this.m_startingGlobalKeys[i]);
		}
		if (this.m_fileSource != FileHelpers.FileSource.Cloud)
		{
			Directory.CreateDirectory(World.GetWorldSavePath(this.m_fileSource));
		}
		string metaPath = this.GetMetaPath();
		string text = metaPath + ".new";
		string oldFile = metaPath + ".old";
		byte[] array = zpackage.GetArray();
		bool flag = this.m_fileSource == FileHelpers.FileSource.Cloud;
		FileWriter fileWriter = new FileWriter(flag ? metaPath : text, FileHelpers.FileHelperType.Binary, this.m_fileSource);
		fileWriter.m_binary.Write(array.Length);
		fileWriter.m_binary.Write(array);
		fileWriter.Finish();
		SaveSystem.InvalidateCache();
		cloudSaveFailed = (fileWriter.Status != FileWriter.WriterStatus.CloseSucceeded && this.m_fileSource == FileHelpers.FileSource.Cloud);
		if (!cloudSaveFailed)
		{
			if (!flag)
			{
				FileHelpers.ReplaceOldFile(metaPath, text, oldFile, this.m_fileSource);
				SaveSystem.InvalidateCache();
			}
			if (considerBackup)
			{
				ZNet.ConsiderAutoBackup(this.m_fileName, SaveDataType.World, now);
			}
		}
		metaWriter = fileWriter;
	}

	// Token: 0x06001489 RID: 5257 RVA: 0x00096090 File Offset: 0x00094290
	public static World LoadWorld(SaveWithBackups saveFile)
	{
		FileReader fileReader = null;
		if (saveFile.IsDeleted)
		{
			ZLog.Log("save deleted " + saveFile.m_name);
			return new World(saveFile, World.SaveDataError.LoadError);
		}
		FileHelpers.FileSource source = saveFile.PrimaryFile.m_source;
		string pathPrimary = saveFile.PrimaryFile.PathPrimary;
		string text = (saveFile.PrimaryFile.PathsAssociated.Length != 0) ? saveFile.PrimaryFile.PathsAssociated[0] : null;
		if (FileHelpers.IsFileCorrupt(pathPrimary, source) || (text != null && FileHelpers.IsFileCorrupt(text, source)))
		{
			ZLog.Log("  corrupt save " + saveFile.m_name);
			return new World(saveFile, World.SaveDataError.Corrupt);
		}
		try
		{
			fileReader = new FileReader(pathPrimary, source, FileHelpers.FileHelperType.Binary);
		}
		catch (Exception ex)
		{
			if (fileReader != null)
			{
				fileReader.Dispose();
			}
			string str = "  failed to load ";
			string name = saveFile.m_name;
			string str2 = " Exception: ";
			Exception ex2 = ex;
			ZLog.Log(str + name + str2 + ((ex2 != null) ? ex2.ToString() : null));
			return new World(saveFile, World.SaveDataError.LoadError);
		}
		World result;
		try
		{
			BinaryReader binary = fileReader.m_binary;
			int count = binary.ReadInt32();
			ZPackage zpackage = new ZPackage(binary.ReadBytes(count));
			int num = zpackage.ReadInt();
			if (!global::Version.IsWorldVersionCompatible(num))
			{
				ZLog.Log("incompatible world version " + num.ToString());
				result = new World(saveFile, World.SaveDataError.BadVersion);
			}
			else
			{
				World world = new World();
				world.m_fileSource = source;
				world.m_fileName = saveFile.m_name;
				world.m_name = zpackage.ReadString();
				world.m_seedName = zpackage.ReadString();
				world.m_seed = zpackage.ReadInt();
				world.m_uid = zpackage.ReadLong();
				world.m_worldVersion = num;
				if (num >= 26)
				{
					world.m_worldGenVersion = zpackage.ReadInt();
				}
				world.m_needsDB = (num >= 30 && zpackage.ReadBool());
				if (num != 35)
				{
					world.m_createBackupBeforeSaving = true;
				}
				if (world.CheckDbFile())
				{
					world.m_dataError = World.SaveDataError.MissingDB;
				}
				if (num >= 32)
				{
					int num2 = zpackage.ReadInt();
					for (int i = 0; i < num2; i++)
					{
						world.m_startingGlobalKeys.Add(zpackage.ReadString());
					}
				}
				result = world;
			}
		}
		catch
		{
			ZLog.LogWarning("  error loading world " + saveFile.m_name);
			result = new World(saveFile, World.SaveDataError.LoadError);
		}
		finally
		{
			if (fileReader != null)
			{
				fileReader.Dispose();
			}
		}
		return result;
	}

	// Token: 0x0600148A RID: 5258 RVA: 0x00096328 File Offset: 0x00094528
	private bool CheckDbFile()
	{
		return this.m_needsDB && !FileHelpers.Exists(this.GetDBPath(), this.m_fileSource);
	}

	// Token: 0x04001427 RID: 5159
	public string m_fileName = "";

	// Token: 0x04001428 RID: 5160
	public string m_name = "";

	// Token: 0x04001429 RID: 5161
	public string m_seedName = "";

	// Token: 0x0400142A RID: 5162
	public int m_seed;

	// Token: 0x0400142B RID: 5163
	public long m_uid;

	// Token: 0x0400142C RID: 5164
	public List<string> m_startingGlobalKeys = new List<string>();

	// Token: 0x0400142D RID: 5165
	public bool m_startingKeysChanged;

	// Token: 0x0400142E RID: 5166
	public int m_worldGenVersion;

	// Token: 0x0400142F RID: 5167
	public int m_worldVersion;

	// Token: 0x04001430 RID: 5168
	public bool m_menu;

	// Token: 0x04001431 RID: 5169
	public bool m_needsDB;

	// Token: 0x04001432 RID: 5170
	public bool m_createBackupBeforeSaving;

	// Token: 0x04001433 RID: 5171
	public SaveWithBackups saves;

	// Token: 0x04001434 RID: 5172
	public World.SaveDataError m_dataError;

	// Token: 0x04001435 RID: 5173
	public FileHelpers.FileSource m_fileSource = FileHelpers.FileSource.Local;

	// Token: 0x02000337 RID: 823
	public enum SaveDataError
	{
		// Token: 0x04002487 RID: 9351
		None,
		// Token: 0x04002488 RID: 9352
		BadVersion,
		// Token: 0x04002489 RID: 9353
		LoadError,
		// Token: 0x0400248A RID: 9354
		Corrupt,
		// Token: 0x0400248B RID: 9355
		MissingMeta,
		// Token: 0x0400248C RID: 9356
		MissingDB
	}
}
