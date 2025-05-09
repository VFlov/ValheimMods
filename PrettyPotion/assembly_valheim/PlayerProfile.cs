using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

// Token: 0x0200012D RID: 301
public class PlayerProfile
{
	// Token: 0x0600130D RID: 4877 RVA: 0x0008E08C File Offset: 0x0008C28C
	public PlayerProfile(string filename = null, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		this.m_filename = filename;
		if (fileSource == FileHelpers.FileSource.Auto)
		{
			this.m_fileSource = (FileHelpers.CloudStorageEnabled ? FileHelpers.FileSource.Cloud : FileHelpers.FileSource.Local);
		}
		else
		{
			this.m_fileSource = fileSource;
		}
		this.m_playerName = "Stranger";
		this.m_playerID = Utils.GenerateUID();
	}

	// Token: 0x0600130E RID: 4878 RVA: 0x0008E176 File Offset: 0x0008C376
	public bool Load()
	{
		return this.m_filename != null && this.LoadPlayerFromDisk();
	}

	// Token: 0x0600130F RID: 4879 RVA: 0x0008E188 File Offset: 0x0008C388
	public bool Save()
	{
		return this.m_filename != null && this.SavePlayerToDisk();
	}

	// Token: 0x06001310 RID: 4880 RVA: 0x0008E19C File Offset: 0x0008C39C
	public bool HaveIncompatiblPlayerData()
	{
		if (this.m_filename == null)
		{
			return false;
		}
		ZPackage zpackage = this.LoadPlayerDataFromDisk();
		if (zpackage == null)
		{
			return false;
		}
		if (!global::Version.IsPlayerVersionCompatible(zpackage.ReadInt()))
		{
			ZLog.Log("Player data is not compatible, ignoring");
			return true;
		}
		return false;
	}

	// Token: 0x06001311 RID: 4881 RVA: 0x0008E1DC File Offset: 0x0008C3DC
	public void SavePlayerData(Player player)
	{
		ZPackage zpackage = new ZPackage();
		player.Save(zpackage);
		this.m_playerData = zpackage.GetArray();
	}

	// Token: 0x06001312 RID: 4882 RVA: 0x0008E204 File Offset: 0x0008C404
	public void LoadPlayerData(Player player)
	{
		player.SetPlayerID(this.m_playerID, this.GetName());
		if (this.m_playerData != null)
		{
			ZPackage pkg = new ZPackage(this.m_playerData);
			player.Load(pkg);
			return;
		}
		player.GiveDefaultItems();
	}

	// Token: 0x06001313 RID: 4883 RVA: 0x0008E245 File Offset: 0x0008C445
	public void SaveLogoutPoint()
	{
		if (Player.m_localPlayer && !Player.m_localPlayer.IsDead() && !Player.m_localPlayer.InIntro())
		{
			this.SetLogoutPoint(Player.m_localPlayer.transform.position);
		}
	}

	// Token: 0x06001314 RID: 4884 RVA: 0x0008E280 File Offset: 0x0008C480
	private bool SavePlayerToDisk()
	{
		Action savingStarted = PlayerProfile.SavingStarted;
		if (savingStarted != null)
		{
			savingStarted();
		}
		DateTime now = DateTime.Now;
		bool flag = SaveSystem.CheckMove(this.m_filename, SaveDataType.Character, ref this.m_fileSource, now, 0UL, false);
		if (this.m_createBackupBeforeSaving && !flag)
		{
			SaveWithBackups saveWithBackups;
			if (SaveSystem.TryGetSaveByName(this.m_filename, SaveDataType.Character, out saveWithBackups) && !saveWithBackups.IsDeleted)
			{
				if (SaveSystem.CreateBackup(saveWithBackups.PrimaryFile, DateTime.Now, this.m_fileSource))
				{
					ZLog.Log("Migrating character save from an old save format, created backup!");
				}
				else
				{
					ZLog.LogError("Failed to create backup of character save " + this.m_filename + "!");
				}
			}
			else
			{
				ZLog.LogError("Failed to get character save " + this.m_filename + " from save system, so a backup couldn't be created!");
			}
		}
		this.m_createBackupBeforeSaving = false;
		string text = PlayerProfile.GetCharacterFolderPath(this.m_fileSource) + this.m_filename + ".fch";
		string oldFile = text + ".old";
		string text2 = text + ".new";
		string characterFolderPath = PlayerProfile.GetCharacterFolderPath(this.m_fileSource);
		if (!Directory.Exists(characterFolderPath) && this.m_fileSource != FileHelpers.FileSource.Cloud)
		{
			Directory.CreateDirectory(characterFolderPath);
		}
		ZPackage zpackage = new ZPackage();
		zpackage.Write(41);
		zpackage.Write(105);
		for (int i = 0; i < 105; i++)
		{
			zpackage.Write(this.m_playerStats.m_stats[(PlayerStatType)i]);
		}
		zpackage.Write(this.m_firstSpawn);
		zpackage.Write(this.m_worldData.Count);
		foreach (KeyValuePair<long, PlayerProfile.WorldPlayerData> keyValuePair in this.m_worldData)
		{
			zpackage.Write(keyValuePair.Key);
			zpackage.Write(keyValuePair.Value.m_haveCustomSpawnPoint);
			zpackage.Write(keyValuePair.Value.m_spawnPoint);
			zpackage.Write(keyValuePair.Value.m_haveLogoutPoint);
			zpackage.Write(keyValuePair.Value.m_logoutPoint);
			zpackage.Write(keyValuePair.Value.m_haveDeathPoint);
			zpackage.Write(keyValuePair.Value.m_deathPoint);
			zpackage.Write(keyValuePair.Value.m_homePoint);
			zpackage.Write(keyValuePair.Value.m_mapData != null);
			if (keyValuePair.Value.m_mapData != null)
			{
				zpackage.Write(keyValuePair.Value.m_mapData);
			}
		}
		zpackage.Write(this.m_playerName);
		zpackage.Write(this.m_playerID);
		zpackage.Write(this.m_startSeed);
		int num = (int)(DateTime.Now - this.m_lastSaveLoad).TotalSeconds;
		this.m_lastSaveLoad = DateTime.Now;
		zpackage.Write(this.m_usedCheats);
		zpackage.Write(new DateTimeOffset(this.m_dateCreated).ToUnixTimeSeconds());
		if (ZNet.instance && ZoneSystem.instance && ZNet.World != null)
		{
			this.m_knownWorlds.IncrementOrSet(ZNet.instance.GetWorldName(), (float)num);
			for (int j = 0; j < 42; j++)
			{
				string str;
				if (ZoneSystem.instance.GetGlobalKey((GlobalKeys)j, out str))
				{
					Dictionary<string, float> knownWorldKeys = this.m_knownWorldKeys;
					GlobalKeys globalKeys = (GlobalKeys)j;
					knownWorldKeys.IncrementOrSet(globalKeys.ToString().ToLower() + " " + str, (float)num);
				}
				else
				{
					Dictionary<string, float> knownWorldKeys2 = this.m_knownWorldKeys;
					GlobalKeys globalKeys = (GlobalKeys)j;
					knownWorldKeys2.IncrementOrSet(globalKeys.ToString().ToLower() + " default", (float)num);
				}
			}
		}
		zpackage.Write(this.m_knownWorlds.Count);
		foreach (KeyValuePair<string, float> keyValuePair2 in this.m_knownWorlds)
		{
			zpackage.Write(keyValuePair2.Key);
			zpackage.Write(keyValuePair2.Value);
		}
		zpackage.Write(this.m_knownWorldKeys.Count);
		foreach (KeyValuePair<string, float> keyValuePair3 in this.m_knownWorldKeys)
		{
			zpackage.Write(keyValuePair3.Key);
			zpackage.Write(keyValuePair3.Value);
		}
		zpackage.Write(this.m_knownCommands.Count);
		foreach (KeyValuePair<string, float> keyValuePair4 in this.m_knownCommands)
		{
			zpackage.Write(keyValuePair4.Key);
			zpackage.Write(keyValuePair4.Value);
		}
		if (this.m_playerData != null)
		{
			zpackage.Write(true);
			zpackage.Write(this.m_playerData);
		}
		else
		{
			zpackage.Write(false);
		}
		byte[] array = zpackage.GenerateHash();
		byte[] array2 = zpackage.GetArray();
		FileWriter fileWriter = new FileWriter(text2, FileHelpers.FileHelperType.Binary, this.m_fileSource);
		fileWriter.m_binary.Write(array2.Length);
		fileWriter.m_binary.Write(array2);
		fileWriter.m_binary.Write(array.Length);
		fileWriter.m_binary.Write(array);
		fileWriter.Finish();
		SaveSystem.InvalidateCache();
		if (fileWriter.Status != FileWriter.WriterStatus.CloseSucceeded && this.m_fileSource == FileHelpers.FileSource.Cloud)
		{
			string text3 = string.Concat(new string[]
			{
				PlayerProfile.GetCharacterFolderPath(FileHelpers.FileSource.Local),
				this.m_filename,
				"_backup_cloud-",
				now.ToString("yyyyMMdd-HHmmss"),
				".fch"
			});
			fileWriter.DumpCloudWriteToLocalFile(text3);
			SaveSystem.InvalidateCache();
			ZLog.LogError(string.Concat(new string[]
			{
				"Cloud save to location \"",
				text,
				"\" failed! Saved as local backup \"",
				text3,
				"\". Use the \"Manage saves\" menu to restore this backup."
			}));
		}
		else
		{
			FileHelpers.ReplaceOldFile(text, text2, oldFile, this.m_fileSource);
			SaveSystem.InvalidateCache();
			ZNet.ConsiderAutoBackup(this.m_filename, SaveDataType.Character, now);
		}
		Action savingFinished = PlayerProfile.SavingFinished;
		if (savingFinished != null)
		{
			savingFinished();
		}
		return true;
	}

	// Token: 0x06001315 RID: 4885 RVA: 0x0008E8E0 File Offset: 0x0008CAE0
	private bool LoadPlayerFromDisk()
	{
		try
		{
			ZPackage zpackage = this.LoadPlayerDataFromDisk();
			if (zpackage == null)
			{
				ZLog.LogWarning("No player data");
				return false;
			}
			int num = zpackage.ReadInt();
			if (!global::Version.IsPlayerVersionCompatible(num))
			{
				ZLog.Log("Player data is not compatible, ignoring");
				return false;
			}
			if (num != 41)
			{
				this.m_createBackupBeforeSaving = true;
			}
			if (num >= 38)
			{
				int num2 = zpackage.ReadInt();
				for (int i = 0; i < num2; i++)
				{
					this.m_playerStats[(PlayerStatType)i] = zpackage.ReadSingle();
				}
			}
			else if (num >= 28)
			{
				this.m_playerStats[PlayerStatType.EnemyKills] = (float)zpackage.ReadInt();
				this.m_playerStats[PlayerStatType.Deaths] = (float)zpackage.ReadInt();
				this.m_playerStats[PlayerStatType.CraftsOrUpgrades] = (float)zpackage.ReadInt();
				this.m_playerStats[PlayerStatType.Builds] = (float)zpackage.ReadInt();
			}
			if (num >= 40)
			{
				this.m_firstSpawn = zpackage.ReadBool();
			}
			this.m_worldData.Clear();
			int num3 = zpackage.ReadInt();
			for (int j = 0; j < num3; j++)
			{
				long key = zpackage.ReadLong();
				PlayerProfile.WorldPlayerData worldPlayerData = new PlayerProfile.WorldPlayerData();
				worldPlayerData.m_haveCustomSpawnPoint = zpackage.ReadBool();
				worldPlayerData.m_spawnPoint = zpackage.ReadVector3();
				worldPlayerData.m_haveLogoutPoint = zpackage.ReadBool();
				worldPlayerData.m_logoutPoint = zpackage.ReadVector3();
				if (num >= 30)
				{
					worldPlayerData.m_haveDeathPoint = zpackage.ReadBool();
					worldPlayerData.m_deathPoint = zpackage.ReadVector3();
				}
				worldPlayerData.m_homePoint = zpackage.ReadVector3();
				if (num >= 29 && zpackage.ReadBool())
				{
					worldPlayerData.m_mapData = zpackage.ReadByteArray();
				}
				this.m_worldData.Add(key, worldPlayerData);
			}
			this.SetName(zpackage.ReadString());
			this.m_playerID = zpackage.ReadLong();
			this.m_startSeed = zpackage.ReadString();
			if (num >= 38)
			{
				this.m_usedCheats = zpackage.ReadBool();
				this.m_dateCreated = DateTimeOffset.FromUnixTimeSeconds(zpackage.ReadLong()).Date;
				int num4 = zpackage.ReadInt();
				for (int k = 0; k < num4; k++)
				{
					this.m_knownWorlds[zpackage.ReadString()] = zpackage.ReadSingle();
				}
				num4 = zpackage.ReadInt();
				for (int l = 0; l < num4; l++)
				{
					this.m_knownWorldKeys[zpackage.ReadString()] = zpackage.ReadSingle();
				}
				num4 = zpackage.ReadInt();
				for (int m = 0; m < num4; m++)
				{
					this.m_knownCommands[zpackage.ReadString()] = zpackage.ReadSingle();
				}
			}
			else
			{
				this.m_dateCreated = new DateTime(2021, 2, 2);
			}
			if (zpackage.ReadBool())
			{
				this.m_playerData = zpackage.ReadByteArray();
			}
			else
			{
				this.m_playerData = null;
			}
			if (num < 40)
			{
				this.<LoadPlayerFromDisk>g__GetFirstSpawnFromPlayerData|10_0();
			}
			this.m_lastSaveLoad = DateTime.Now;
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("Exception while loading player profile:" + this.m_filename + " , " + ex.ToString());
		}
		return true;
	}

	// Token: 0x06001316 RID: 4886 RVA: 0x0008EBF8 File Offset: 0x0008CDF8
	private ZPackage LoadPlayerDataFromDisk()
	{
		string path = PlayerProfile.GetPath(this.m_fileSource, this.m_filename);
		FileReader fileReader;
		try
		{
			fileReader = new FileReader(path, this.m_fileSource, FileHelpers.FileHelperType.Binary);
		}
		catch (Exception ex)
		{
			ZLog.Log(string.Concat(new string[]
			{
				"  failed to load: ",
				path,
				" (",
				ex.Message,
				")"
			}));
			return null;
		}
		byte[] data;
		try
		{
			BinaryReader binary = fileReader.m_binary;
			int count = binary.ReadInt32();
			data = binary.ReadBytes(count);
			int count2 = binary.ReadInt32();
			binary.ReadBytes(count2);
		}
		catch (Exception ex2)
		{
			ZLog.LogError(string.Format("  error loading player.dat. Source: {0}, Path: {1}, Error: {2}", this.m_fileSource, path, ex2.Message));
			fileReader.Dispose();
			return null;
		}
		fileReader.Dispose();
		return new ZPackage(data);
	}

	// Token: 0x06001317 RID: 4887 RVA: 0x0008ECE8 File Offset: 0x0008CEE8
	public void SetLogoutPoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint = true;
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_logoutPoint = point;
	}

	// Token: 0x06001318 RID: 4888 RVA: 0x0008ED16 File Offset: 0x0008CF16
	public void SetDeathPoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveDeathPoint = true;
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_deathPoint = point;
	}

	// Token: 0x06001319 RID: 4889 RVA: 0x0008ED44 File Offset: 0x0008CF44
	public void SetMapData(byte[] data)
	{
		long worldUID = ZNet.instance.GetWorldUID();
		if (worldUID != 0L)
		{
			this.GetWorldData(worldUID).m_mapData = data;
		}
	}

	// Token: 0x0600131A RID: 4890 RVA: 0x0008ED6C File Offset: 0x0008CF6C
	public byte[] GetMapData()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_mapData;
	}

	// Token: 0x0600131B RID: 4891 RVA: 0x0008ED83 File Offset: 0x0008CF83
	public void ClearLoguoutPoint()
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint = false;
	}

	// Token: 0x0600131C RID: 4892 RVA: 0x0008ED9B File Offset: 0x0008CF9B
	public bool HaveLogoutPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint;
	}

	// Token: 0x0600131D RID: 4893 RVA: 0x0008EDB2 File Offset: 0x0008CFB2
	public Vector3 GetLogoutPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_logoutPoint;
	}

	// Token: 0x0600131E RID: 4894 RVA: 0x0008EDC9 File Offset: 0x0008CFC9
	public bool HaveDeathPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveDeathPoint;
	}

	// Token: 0x0600131F RID: 4895 RVA: 0x0008EDE0 File Offset: 0x0008CFE0
	public Vector3 GetDeathPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_deathPoint;
	}

	// Token: 0x06001320 RID: 4896 RVA: 0x0008EDF7 File Offset: 0x0008CFF7
	public void SetCustomSpawnPoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint = true;
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_spawnPoint = point;
	}

	// Token: 0x06001321 RID: 4897 RVA: 0x0008EE25 File Offset: 0x0008D025
	public Vector3 GetCustomSpawnPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_spawnPoint;
	}

	// Token: 0x06001322 RID: 4898 RVA: 0x0008EE3C File Offset: 0x0008D03C
	public bool HaveCustomSpawnPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint;
	}

	// Token: 0x06001323 RID: 4899 RVA: 0x0008EE53 File Offset: 0x0008D053
	public void ClearCustomSpawnPoint()
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint = false;
	}

	// Token: 0x06001324 RID: 4900 RVA: 0x0008EE6B File Offset: 0x0008D06B
	public void SetHomePoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_homePoint = point;
	}

	// Token: 0x06001325 RID: 4901 RVA: 0x0008EE83 File Offset: 0x0008D083
	public Vector3 GetHomePoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_homePoint;
	}

	// Token: 0x06001326 RID: 4902 RVA: 0x0008EE9A File Offset: 0x0008D09A
	public void SetName(string name)
	{
		this.m_playerName = name;
	}

	// Token: 0x06001327 RID: 4903 RVA: 0x0008EEA3 File Offset: 0x0008D0A3
	public string GetName()
	{
		return this.m_playerName;
	}

	// Token: 0x06001328 RID: 4904 RVA: 0x0008EEAB File Offset: 0x0008D0AB
	public long GetPlayerID()
	{
		return this.m_playerID;
	}

	// Token: 0x06001329 RID: 4905 RVA: 0x0008EEB4 File Offset: 0x0008D0B4
	public static void RemoveProfile(string name, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		SaveWithBackups saveWithBackups;
		if (SaveSystem.TryGetSaveByName(name, SaveDataType.Character, out saveWithBackups) && !saveWithBackups.IsDeleted)
		{
			SaveSystem.Delete(saveWithBackups.PrimaryFile);
		}
	}

	// Token: 0x0600132A RID: 4906 RVA: 0x0008EEE0 File Offset: 0x0008D0E0
	public static bool HaveProfile(string name)
	{
		SaveWithBackups saveWithBackups;
		return SaveSystem.TryGetSaveByName(name, SaveDataType.Character, out saveWithBackups) && !saveWithBackups.IsDeleted;
	}

	// Token: 0x0600132B RID: 4907 RVA: 0x0008EF04 File Offset: 0x0008D104
	public void IncrementStat(PlayerStatType stat, float amount = 1f)
	{
		PlayerProfile.PlayerStats playerStats = this.m_playerStats;
		playerStats[stat] += amount;
	}

	// Token: 0x0600132C RID: 4908 RVA: 0x0008EF2A File Offset: 0x0008D12A
	private static string GetCharacterFolder(FileHelpers.FileSource fileSource)
	{
		if (fileSource != FileHelpers.FileSource.Local)
		{
			return "/characters/";
		}
		return "/characters_local/";
	}

	// Token: 0x0600132D RID: 4909 RVA: 0x0008EF3B File Offset: 0x0008D13B
	public static string GetCharacterFolderPath(FileHelpers.FileSource fileSource)
	{
		return Utils.GetSaveDataPath(fileSource) + PlayerProfile.GetCharacterFolder(fileSource);
	}

	// Token: 0x0600132E RID: 4910 RVA: 0x0008EF4E File Offset: 0x0008D14E
	public string GetFilename()
	{
		return this.m_filename;
	}

	// Token: 0x0600132F RID: 4911 RVA: 0x0008EF56 File Offset: 0x0008D156
	public string GetPath()
	{
		return PlayerProfile.GetPath(this.m_fileSource, this.m_filename);
	}

	// Token: 0x06001330 RID: 4912 RVA: 0x0008EF69 File Offset: 0x0008D169
	public static string GetPath(FileHelpers.FileSource fileSource, string name)
	{
		return PlayerProfile.GetCharacterFolderPath(fileSource) + name + ".fch";
	}

	// Token: 0x06001331 RID: 4913 RVA: 0x0008EF7C File Offset: 0x0008D17C
	private PlayerProfile.WorldPlayerData GetWorldData(long worldUID)
	{
		PlayerProfile.WorldPlayerData worldPlayerData;
		if (this.m_worldData.TryGetValue(worldUID, out worldPlayerData))
		{
			return worldPlayerData;
		}
		worldPlayerData = new PlayerProfile.WorldPlayerData();
		this.m_worldData.Add(worldUID, worldPlayerData);
		return worldPlayerData;
	}

	// Token: 0x06001333 RID: 4915 RVA: 0x0008EFEC File Offset: 0x0008D1EC
	[CompilerGenerated]
	private void <LoadPlayerFromDisk>g__GetFirstSpawnFromPlayerData|10_0()
	{
		if (this.m_playerData == null)
		{
			return;
		}
		ZPackage zpackage = new ZPackage(this.m_playerData);
		int num = zpackage.ReadInt();
		if (num < 8 || num >= 28)
		{
			return;
		}
		if (num >= 7)
		{
			zpackage.ReadSingle();
		}
		zpackage.ReadSingle();
		if (num >= 10)
		{
			zpackage.ReadSingle();
		}
		this.m_firstSpawn = zpackage.ReadBool();
	}

	// Token: 0x040012B6 RID: 4790
	public static Action SavingStarted;

	// Token: 0x040012B7 RID: 4791
	public static Action SavingFinished;

	// Token: 0x040012B8 RID: 4792
	public static Vector3 m_originalSpawnPoint = new Vector3(-676f, 50f, 299f);

	// Token: 0x040012B9 RID: 4793
	public readonly PlayerProfile.PlayerStats m_playerStats = new PlayerProfile.PlayerStats();

	// Token: 0x040012BA RID: 4794
	public bool m_firstSpawn = true;

	// Token: 0x040012BB RID: 4795
	public FileHelpers.FileSource m_fileSource = FileHelpers.FileSource.Local;

	// Token: 0x040012BC RID: 4796
	public readonly string m_filename = "";

	// Token: 0x040012BD RID: 4797
	private string m_playerName = "";

	// Token: 0x040012BE RID: 4798
	private long m_playerID;

	// Token: 0x040012BF RID: 4799
	private string m_startSeed = "";

	// Token: 0x040012C0 RID: 4800
	private readonly Dictionary<long, PlayerProfile.WorldPlayerData> m_worldData = new Dictionary<long, PlayerProfile.WorldPlayerData>();

	// Token: 0x040012C1 RID: 4801
	private bool m_createBackupBeforeSaving;

	// Token: 0x040012C2 RID: 4802
	private DateTime m_lastSaveLoad = DateTime.Now;

	// Token: 0x040012C3 RID: 4803
	public Dictionary<string, float> m_knownWorlds = new Dictionary<string, float>();

	// Token: 0x040012C4 RID: 4804
	public Dictionary<string, float> m_knownWorldKeys = new Dictionary<string, float>();

	// Token: 0x040012C5 RID: 4805
	public Dictionary<string, float> m_knownCommands = new Dictionary<string, float>();

	// Token: 0x040012C6 RID: 4806
	public Dictionary<string, float> m_enemyStats = new Dictionary<string, float>();

	// Token: 0x040012C7 RID: 4807
	public Dictionary<string, float> m_itemPickupStats = new Dictionary<string, float>();

	// Token: 0x040012C8 RID: 4808
	public Dictionary<string, float> m_itemCraftStats = new Dictionary<string, float>();

	// Token: 0x040012C9 RID: 4809
	public bool m_usedCheats;

	// Token: 0x040012CA RID: 4810
	public DateTime m_dateCreated = DateTime.Now;

	// Token: 0x040012CB RID: 4811
	private byte[] m_playerData;

	// Token: 0x040012CC RID: 4812
	public static Dictionary<PlayerStatType, string> m_statTypeDates = new Dictionary<PlayerStatType, string>
	{
		{
			PlayerStatType.Deaths,
			"Since beginning"
		},
		{
			PlayerStatType.Jumps,
			"Hildirs Request (2023-06-16)"
		}
	};

	// Token: 0x02000327 RID: 807
	private class WorldPlayerData
	{
		// Token: 0x04002421 RID: 9249
		public Vector3 m_spawnPoint = Vector3.zero;

		// Token: 0x04002422 RID: 9250
		public bool m_haveCustomSpawnPoint;

		// Token: 0x04002423 RID: 9251
		public Vector3 m_logoutPoint = Vector3.zero;

		// Token: 0x04002424 RID: 9252
		public bool m_haveLogoutPoint;

		// Token: 0x04002425 RID: 9253
		public Vector3 m_deathPoint = Vector3.zero;

		// Token: 0x04002426 RID: 9254
		public bool m_haveDeathPoint;

		// Token: 0x04002427 RID: 9255
		public Vector3 m_homePoint = Vector3.zero;

		// Token: 0x04002428 RID: 9256
		public byte[] m_mapData;
	}

	// Token: 0x02000328 RID: 808
	public class PlayerStats
	{
		// Token: 0x170001BD RID: 445
		public float this[PlayerStatType type]
		{
			get
			{
				return this.m_stats[type];
			}
			set
			{
				this.m_stats[type] = value;
			}
		}

		// Token: 0x0600223C RID: 8764 RVA: 0x000ECFA4 File Offset: 0x000EB1A4
		public PlayerStats()
		{
			for (int i = 0; i < 105; i++)
			{
				this.m_stats[(PlayerStatType)i] = 0f;
			}
		}

		// Token: 0x04002429 RID: 9257
		public Dictionary<PlayerStatType, float> m_stats = new Dictionary<PlayerStatType, float>();
	}
}
