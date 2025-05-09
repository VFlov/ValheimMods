using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

// Token: 0x020000CD RID: 205
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class ZDO : IEquatable<ZDO>
{
	// Token: 0x06000C31 RID: 3121 RVA: 0x000633C4 File Offset: 0x000615C4
	public void Initialize(ZDOID id, Vector3 position)
	{
		this.m_uid = id;
		this.m_position = position;
		Vector2i zone = ZoneSystem.GetZone(this.m_position);
		this.m_sector = zone.ClampToShort();
		ZDOMan.instance.AddToSector(this, zone);
		this.m_dataFlags = ZDO.DataFlags.None;
		this.Valid = true;
	}

	// Token: 0x06000C32 RID: 3122 RVA: 0x00063411 File Offset: 0x00061611
	public void Init()
	{
		this.m_dataFlags = ZDO.DataFlags.None;
		this.Valid = true;
	}

	// Token: 0x06000C33 RID: 3123 RVA: 0x00063421 File Offset: 0x00061621
	public override string ToString()
	{
		return this.m_uid.ToString();
	}

	// Token: 0x06000C34 RID: 3124 RVA: 0x00063434 File Offset: 0x00061634
	public bool IsValid()
	{
		return this.Valid;
	}

	// Token: 0x06000C35 RID: 3125 RVA: 0x0006343C File Offset: 0x0006163C
	public override int GetHashCode()
	{
		return this.m_uid.GetHashCode();
	}

	// Token: 0x06000C36 RID: 3126 RVA: 0x0006344F File Offset: 0x0006164F
	public bool Equals(ZDO other)
	{
		return this == other;
	}

	// Token: 0x06000C37 RID: 3127 RVA: 0x00063458 File Offset: 0x00061658
	public void Reset()
	{
		if (!this.SaveClone)
		{
			ZDOExtraData.Release(this, this.m_uid);
		}
		this.m_uid = ZDOID.None;
		this.m_dataFlags = ZDO.DataFlags.None;
		this.OwnerRevision = 0;
		this.DataRevision = 0U;
		this.m_tempSortValue = 0f;
		this.m_prefab = 0;
		this.m_sector = Vector2s.zero;
		this.m_position = Vector3.zero;
		this.m_rotation = Quaternion.identity.eulerAngles;
	}

	// Token: 0x06000C38 RID: 3128 RVA: 0x000634D4 File Offset: 0x000616D4
	public ZDO Clone()
	{
		ZDO zdo = base.MemberwiseClone() as ZDO;
		zdo.SaveClone = true;
		return zdo;
	}

	// Token: 0x06000C39 RID: 3129 RVA: 0x000634E8 File Offset: 0x000616E8
	public void Set(string name, ZDOID id)
	{
		this.Set(ZDO.GetHashZDOID(name), id);
	}

	// Token: 0x06000C3A RID: 3130 RVA: 0x000634F7 File Offset: 0x000616F7
	public void Set(KeyValuePair<int, int> hashPair, ZDOID id)
	{
		this.Set(hashPair.Key, id.UserID);
		this.Set(hashPair.Value, (long)((ulong)id.ID));
	}

	// Token: 0x06000C3B RID: 3131 RVA: 0x00063522 File Offset: 0x00061722
	public static KeyValuePair<int, int> GetHashZDOID(string name)
	{
		return new KeyValuePair<int, int>((name + "_u").GetStableHashCode(), (name + "_i").GetStableHashCode());
	}

	// Token: 0x06000C3C RID: 3132 RVA: 0x00063549 File Offset: 0x00061749
	public ZDOID GetZDOID(string name)
	{
		return this.GetZDOID(ZDO.GetHashZDOID(name));
	}

	// Token: 0x06000C3D RID: 3133 RVA: 0x00063558 File Offset: 0x00061758
	public ZDOID GetZDOID(KeyValuePair<int, int> hashPair)
	{
		long @long = this.GetLong(hashPair.Key, 0L);
		uint num = (uint)this.GetLong(hashPair.Value, 0L);
		if (@long == 0L || num == 0U)
		{
			return ZDOID.None;
		}
		return new ZDOID(@long, num);
	}

	// Token: 0x06000C3E RID: 3134 RVA: 0x00063599 File Offset: 0x00061799
	public void Set(string name, float value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	// Token: 0x06000C3F RID: 3135 RVA: 0x000635A8 File Offset: 0x000617A8
	public void Set(int hash, float value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C40 RID: 3136 RVA: 0x000635BF File Offset: 0x000617BF
	public void Set(string name, Vector3 value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	// Token: 0x06000C41 RID: 3137 RVA: 0x000635CE File Offset: 0x000617CE
	public void Set(int hash, Vector3 value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C42 RID: 3138 RVA: 0x000635E5 File Offset: 0x000617E5
	public void Update(int hash, Vector3 value)
	{
		if (ZDOExtraData.Update(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C43 RID: 3139 RVA: 0x000635FC File Offset: 0x000617FC
	public void Set(string name, Quaternion value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	// Token: 0x06000C44 RID: 3140 RVA: 0x0006360B File Offset: 0x0006180B
	public void Set(int hash, Quaternion value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C45 RID: 3141 RVA: 0x00063622 File Offset: 0x00061822
	public void Set(string name, int value)
	{
		this.Set(name.GetStableHashCode(), value, false);
	}

	// Token: 0x06000C46 RID: 3142 RVA: 0x00063632 File Offset: 0x00061832
	public void Set(int hash, int value, bool okForNotOwner = false)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C47 RID: 3143 RVA: 0x0006364B File Offset: 0x0006184B
	public void SetConnection(ZDOExtraData.ConnectionType connectionType, ZDOID zid)
	{
		if (ZDOExtraData.SetConnection(this.m_uid, connectionType, zid))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C48 RID: 3144 RVA: 0x00063662 File Offset: 0x00061862
	public void UpdateConnection(ZDOExtraData.ConnectionType connectionType, ZDOID zid)
	{
		if (ZDOExtraData.UpdateConnection(this.m_uid, connectionType, zid))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C49 RID: 3145 RVA: 0x00063679 File Offset: 0x00061879
	public void Set(string name, bool value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	// Token: 0x06000C4A RID: 3146 RVA: 0x00063688 File Offset: 0x00061888
	public void Set(int hash, bool value)
	{
		this.Set(hash, value ? 1 : 0, false);
	}

	// Token: 0x06000C4B RID: 3147 RVA: 0x00063699 File Offset: 0x00061899
	public void Set(string name, long value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	// Token: 0x06000C4C RID: 3148 RVA: 0x000636A8 File Offset: 0x000618A8
	public void Set(int hash, long value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C4D RID: 3149 RVA: 0x000636BF File Offset: 0x000618BF
	public void Set(string name, byte[] bytes)
	{
		this.Set(name.GetStableHashCode(), bytes);
	}

	// Token: 0x06000C4E RID: 3150 RVA: 0x000636CE File Offset: 0x000618CE
	public void Set(int hash, byte[] bytes)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, bytes))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C4F RID: 3151 RVA: 0x000636E5 File Offset: 0x000618E5
	public void Set(string name, string value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	// Token: 0x06000C50 RID: 3152 RVA: 0x000636F4 File Offset: 0x000618F4
	public void Set(int hash, string value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C51 RID: 3153 RVA: 0x0006370B File Offset: 0x0006190B
	public void SetPosition(Vector3 pos)
	{
		this.InternalSetPosition(pos);
	}

	// Token: 0x06000C52 RID: 3154 RVA: 0x00063714 File Offset: 0x00061914
	public void InternalSetPosition(Vector3 pos)
	{
		if (this.m_position == pos)
		{
			return;
		}
		this.m_position = pos;
		this.SetSector(ZoneSystem.GetZone(this.m_position));
		if (this.IsOwner())
		{
			this.IncreaseDataRevision();
		}
	}

	// Token: 0x06000C53 RID: 3155 RVA: 0x0006374B File Offset: 0x0006194B
	public void InvalidateSector()
	{
		this.SetSector(new Vector2i(int.MinValue, int.MinValue));
	}

	// Token: 0x06000C54 RID: 3156 RVA: 0x00063764 File Offset: 0x00061964
	private void SetSector(Vector2i sector)
	{
		if (this.m_sector == sector)
		{
			return;
		}
		ZDOMan.instance.RemoveFromSector(this, this.m_sector.ToVector2i());
		this.m_sector = sector.ClampToShort();
		ZDOMan.instance.AddToSector(this, sector);
		if (ZNet.instance.IsServer())
		{
			ZDOMan.instance.ZDOSectorInvalidated(this);
		}
	}

	// Token: 0x06000C55 RID: 3157 RVA: 0x000637C5 File Offset: 0x000619C5
	public Vector2i GetSector()
	{
		return this.m_sector.ToVector2i();
	}

	// Token: 0x06000C56 RID: 3158 RVA: 0x000637D2 File Offset: 0x000619D2
	public void SetRotation(Quaternion rot)
	{
		if (this.m_rotation == rot.eulerAngles)
		{
			return;
		}
		this.m_rotation = rot.eulerAngles;
		this.IncreaseDataRevision();
	}

	// Token: 0x06000C57 RID: 3159 RVA: 0x000637FC File Offset: 0x000619FC
	public void SetType(ZDO.ObjectType type)
	{
		if (this.Type == type)
		{
			return;
		}
		this.Type = type;
		this.IncreaseDataRevision();
	}

	// Token: 0x06000C58 RID: 3160 RVA: 0x00063815 File Offset: 0x00061A15
	public void SetDistant(bool distant)
	{
		if (this.Distant == distant)
		{
			return;
		}
		this.Distant = distant;
		this.IncreaseDataRevision();
	}

	// Token: 0x06000C59 RID: 3161 RVA: 0x0006382E File Offset: 0x00061A2E
	public void SetPrefab(int prefab)
	{
		if (this.m_prefab == prefab)
		{
			return;
		}
		this.m_prefab = prefab;
		this.IncreaseDataRevision();
	}

	// Token: 0x06000C5A RID: 3162 RVA: 0x00063847 File Offset: 0x00061A47
	public int GetPrefab()
	{
		return this.m_prefab;
	}

	// Token: 0x06000C5B RID: 3163 RVA: 0x0006384F File Offset: 0x00061A4F
	public Vector3 GetPosition()
	{
		return this.m_position;
	}

	// Token: 0x06000C5C RID: 3164 RVA: 0x00063857 File Offset: 0x00061A57
	public Quaternion GetRotation()
	{
		return Quaternion.Euler(this.m_rotation);
	}

	// Token: 0x06000C5D RID: 3165 RVA: 0x00063864 File Offset: 0x00061A64
	private void IncreaseDataRevision()
	{
		uint dataRevision = this.DataRevision;
		this.DataRevision = dataRevision + 1U;
		if (!ZNet.instance.IsServer())
		{
			ZDOMan.instance.ClientChanged(this.m_uid);
		}
	}

	// Token: 0x06000C5E RID: 3166 RVA: 0x000638A0 File Offset: 0x00061AA0
	private void IncreaseOwnerRevision()
	{
		ushort ownerRevision = this.OwnerRevision;
		this.OwnerRevision = ownerRevision + 1;
		if (!ZNet.instance.IsServer())
		{
			ZDOMan.instance.ClientChanged(this.m_uid);
		}
	}

	// Token: 0x06000C5F RID: 3167 RVA: 0x000638DA File Offset: 0x00061ADA
	public float GetFloat(string name, float defaultValue = 0f)
	{
		return this.GetFloat(name.GetStableHashCode(), defaultValue);
	}

	// Token: 0x06000C60 RID: 3168 RVA: 0x000638E9 File Offset: 0x00061AE9
	public float GetFloat(int hash, float defaultValue = 0f)
	{
		return ZDOExtraData.GetFloat(this.m_uid, hash, defaultValue);
	}

	// Token: 0x06000C61 RID: 3169 RVA: 0x000638F8 File Offset: 0x00061AF8
	public bool GetFloat(string name, out float value)
	{
		return this.GetFloat(name.GetStableHashCode(), out value);
	}

	// Token: 0x06000C62 RID: 3170 RVA: 0x00063907 File Offset: 0x00061B07
	public bool GetFloat(int hash, out float value)
	{
		return ZDOExtraData.GetFloat(this.m_uid, hash, out value);
	}

	// Token: 0x06000C63 RID: 3171 RVA: 0x00063916 File Offset: 0x00061B16
	public Vector3 GetVec3(string name, Vector3 defaultValue)
	{
		return this.GetVec3(name.GetStableHashCode(), defaultValue);
	}

	// Token: 0x06000C64 RID: 3172 RVA: 0x00063925 File Offset: 0x00061B25
	public Vector3 GetVec3(int hash, Vector3 defaultValue)
	{
		return ZDOExtraData.GetVec3(this.m_uid, hash, defaultValue);
	}

	// Token: 0x06000C65 RID: 3173 RVA: 0x00063934 File Offset: 0x00061B34
	public bool GetVec3(string name, out Vector3 value)
	{
		return this.GetVec3(name.GetStableHashCode(), out value);
	}

	// Token: 0x06000C66 RID: 3174 RVA: 0x00063943 File Offset: 0x00061B43
	public bool GetVec3(int hash, out Vector3 value)
	{
		return ZDOExtraData.GetVec3(this.m_uid, hash, out value);
	}

	// Token: 0x06000C67 RID: 3175 RVA: 0x00063952 File Offset: 0x00061B52
	public Quaternion GetQuaternion(string name, Quaternion defaultValue)
	{
		return this.GetQuaternion(name.GetStableHashCode(), defaultValue);
	}

	// Token: 0x06000C68 RID: 3176 RVA: 0x00063961 File Offset: 0x00061B61
	public Quaternion GetQuaternion(int hash, Quaternion defaultValue)
	{
		return ZDOExtraData.GetQuaternion(this.m_uid, hash, defaultValue);
	}

	// Token: 0x06000C69 RID: 3177 RVA: 0x00063970 File Offset: 0x00061B70
	public bool GetQuaternion(string name, out Quaternion value)
	{
		return this.GetQuaternion(name.GetStableHashCode(), out value);
	}

	// Token: 0x06000C6A RID: 3178 RVA: 0x0006397F File Offset: 0x00061B7F
	public bool GetQuaternion(int hash, out Quaternion value)
	{
		return ZDOExtraData.GetQuaternion(this.m_uid, hash, out value);
	}

	// Token: 0x06000C6B RID: 3179 RVA: 0x0006398E File Offset: 0x00061B8E
	public int GetInt(string name, int defaultValue = 0)
	{
		return this.GetInt(name.GetStableHashCode(), defaultValue);
	}

	// Token: 0x06000C6C RID: 3180 RVA: 0x0006399D File Offset: 0x00061B9D
	public int GetInt(int hash, int defaultValue = 0)
	{
		return ZDOExtraData.GetInt(this.m_uid, hash, defaultValue);
	}

	// Token: 0x06000C6D RID: 3181 RVA: 0x000639AC File Offset: 0x00061BAC
	public bool GetInt(string name, out int value)
	{
		return this.GetInt(name.GetStableHashCode(), out value);
	}

	// Token: 0x06000C6E RID: 3182 RVA: 0x000639BB File Offset: 0x00061BBB
	public bool GetInt(int hash, out int value)
	{
		return ZDOExtraData.GetInt(this.m_uid, hash, out value);
	}

	// Token: 0x06000C6F RID: 3183 RVA: 0x000639CA File Offset: 0x00061BCA
	public bool GetBool(string name, bool defaultValue = false)
	{
		return this.GetBool(name.GetStableHashCode(), defaultValue);
	}

	// Token: 0x06000C70 RID: 3184 RVA: 0x000639D9 File Offset: 0x00061BD9
	public bool GetBool(int hash, bool defaultValue = false)
	{
		return ZDOExtraData.GetInt(this.m_uid, hash, defaultValue ? 1 : 0) != 0;
	}

	// Token: 0x06000C71 RID: 3185 RVA: 0x000639F1 File Offset: 0x00061BF1
	public bool GetBool(string name, out bool value)
	{
		return this.GetBool(name.GetStableHashCode(), out value);
	}

	// Token: 0x06000C72 RID: 3186 RVA: 0x00063A00 File Offset: 0x00061C00
	public bool GetBool(int hash, out bool value)
	{
		return ZDOExtraData.GetBool(this.m_uid, hash, out value);
	}

	// Token: 0x06000C73 RID: 3187 RVA: 0x00063A0F File Offset: 0x00061C0F
	public long GetLong(string name, long defaultValue = 0L)
	{
		return this.GetLong(name.GetStableHashCode(), defaultValue);
	}

	// Token: 0x06000C74 RID: 3188 RVA: 0x00063A1E File Offset: 0x00061C1E
	public long GetLong(int hash, long defaultValue = 0L)
	{
		return ZDOExtraData.GetLong(this.m_uid, hash, defaultValue);
	}

	// Token: 0x06000C75 RID: 3189 RVA: 0x00063A2D File Offset: 0x00061C2D
	public string GetString(string name, string defaultValue = "")
	{
		return this.GetString(name.GetStableHashCode(), defaultValue);
	}

	// Token: 0x06000C76 RID: 3190 RVA: 0x00063A3C File Offset: 0x00061C3C
	public string GetString(int hash, string defaultValue = "")
	{
		return ZDOExtraData.GetString(this.m_uid, hash, defaultValue);
	}

	// Token: 0x06000C77 RID: 3191 RVA: 0x00063A4B File Offset: 0x00061C4B
	public bool GetString(string name, out string value)
	{
		return this.GetString(name.GetStableHashCode(), out value);
	}

	// Token: 0x06000C78 RID: 3192 RVA: 0x00063A5A File Offset: 0x00061C5A
	public bool GetString(int hash, out string value)
	{
		return ZDOExtraData.GetString(this.m_uid, hash, out value);
	}

	// Token: 0x06000C79 RID: 3193 RVA: 0x00063A69 File Offset: 0x00061C69
	public byte[] GetByteArray(string name, byte[] defaultValue = null)
	{
		return this.GetByteArray(name.GetStableHashCode(), defaultValue);
	}

	// Token: 0x06000C7A RID: 3194 RVA: 0x00063A78 File Offset: 0x00061C78
	public byte[] GetByteArray(int hash, byte[] defaultValue = null)
	{
		return ZDOExtraData.GetByteArray(this.m_uid, hash, defaultValue);
	}

	// Token: 0x06000C7B RID: 3195 RVA: 0x00063A87 File Offset: 0x00061C87
	public bool GetByteArray(string name, out byte[] value)
	{
		return this.GetByteArray(name.GetStableHashCode(), out value);
	}

	// Token: 0x06000C7C RID: 3196 RVA: 0x00063A96 File Offset: 0x00061C96
	public bool GetByteArray(int hash, out byte[] value)
	{
		return ZDOExtraData.GetByteArray(this.m_uid, hash, out value);
	}

	// Token: 0x06000C7D RID: 3197 RVA: 0x00063AA5 File Offset: 0x00061CA5
	public ZDOID GetConnectionZDOID(ZDOExtraData.ConnectionType type)
	{
		return ZDOExtraData.GetConnectionZDOID(this.m_uid, type);
	}

	// Token: 0x06000C7E RID: 3198 RVA: 0x00063AB3 File Offset: 0x00061CB3
	public ZDOExtraData.ConnectionType GetConnectionType()
	{
		return ZDOExtraData.GetConnectionType(this.m_uid);
	}

	// Token: 0x06000C7F RID: 3199 RVA: 0x00063AC0 File Offset: 0x00061CC0
	public ZDOConnection GetConnection()
	{
		return ZDOExtraData.GetConnection(this.m_uid);
	}

	// Token: 0x06000C80 RID: 3200 RVA: 0x00063ACD File Offset: 0x00061CCD
	public ZDOConnectionHashData GetConnectionHashData(ZDOExtraData.ConnectionType type)
	{
		return ZDOExtraData.GetConnectionHashData(this.m_uid, type);
	}

	// Token: 0x06000C81 RID: 3201 RVA: 0x00063ADB File Offset: 0x00061CDB
	public bool RemoveInt(string name)
	{
		return this.RemoveInt(name.GetStableHashCode());
	}

	// Token: 0x06000C82 RID: 3202 RVA: 0x00063AE9 File Offset: 0x00061CE9
	public bool RemoveInt(int hash)
	{
		return ZDOExtraData.RemoveInt(this.m_uid, hash);
	}

	// Token: 0x06000C83 RID: 3203 RVA: 0x00063AF7 File Offset: 0x00061CF7
	public bool RemoveLong(int hash)
	{
		return ZDOExtraData.RemoveLong(this.m_uid, hash);
	}

	// Token: 0x06000C84 RID: 3204 RVA: 0x00063B05 File Offset: 0x00061D05
	public bool RemoveFloat(string name)
	{
		return this.RemoveFloat(name.GetStableHashCode());
	}

	// Token: 0x06000C85 RID: 3205 RVA: 0x00063B13 File Offset: 0x00061D13
	public bool RemoveFloat(int hash)
	{
		return ZDOExtraData.RemoveFloat(this.m_uid, hash);
	}

	// Token: 0x06000C86 RID: 3206 RVA: 0x00063B21 File Offset: 0x00061D21
	public bool RemoveVec3(string name)
	{
		return this.RemoveVec3(name.GetStableHashCode());
	}

	// Token: 0x06000C87 RID: 3207 RVA: 0x00063B2F File Offset: 0x00061D2F
	public bool RemoveVec3(int hash)
	{
		return ZDOExtraData.RemoveVec3(this.m_uid, hash);
	}

	// Token: 0x06000C88 RID: 3208 RVA: 0x00063B3D File Offset: 0x00061D3D
	public bool RemoveQuaternion(string name)
	{
		return this.RemoveQuaternion(name.GetStableHashCode());
	}

	// Token: 0x06000C89 RID: 3209 RVA: 0x00063B4B File Offset: 0x00061D4B
	public bool RemoveQuaternion(int hash)
	{
		return ZDOExtraData.RemoveQuaternion(this.m_uid, hash);
	}

	// Token: 0x06000C8A RID: 3210 RVA: 0x00063B5C File Offset: 0x00061D5C
	public void RemoveZDOID(string name)
	{
		KeyValuePair<int, int> hashZDOID = ZDO.GetHashZDOID(name);
		ZDOExtraData.RemoveLong(this.m_uid, hashZDOID.Key);
		ZDOExtraData.RemoveLong(this.m_uid, hashZDOID.Value);
	}

	// Token: 0x06000C8B RID: 3211 RVA: 0x00063B96 File Offset: 0x00061D96
	public void RemoveZDOID(KeyValuePair<int, int> hashes)
	{
		ZDOExtraData.RemoveLong(this.m_uid, hashes.Key);
		ZDOExtraData.RemoveLong(this.m_uid, hashes.Value);
	}

	// Token: 0x06000C8C RID: 3212 RVA: 0x00063BC0 File Offset: 0x00061DC0
	public void Serialize(ZPackage pkg)
	{
		List<KeyValuePair<int, float>> floats = ZDOExtraData.GetFloats(this.m_uid);
		List<KeyValuePair<int, Vector3>> vec3s = ZDOExtraData.GetVec3s(this.m_uid);
		List<KeyValuePair<int, Quaternion>> quaternions = ZDOExtraData.GetQuaternions(this.m_uid);
		List<KeyValuePair<int, int>> ints = ZDOExtraData.GetInts(this.m_uid);
		List<KeyValuePair<int, long>> longs = ZDOExtraData.GetLongs(this.m_uid);
		List<KeyValuePair<int, string>> strings = ZDOExtraData.GetStrings(this.m_uid);
		List<KeyValuePair<int, byte[]>> byteArrays = ZDOExtraData.GetByteArrays(this.m_uid);
		ZDOConnection connection = ZDOExtraData.GetConnection(this.m_uid);
		ushort num = 0;
		if (connection != null && connection.m_type != ZDOExtraData.ConnectionType.None)
		{
			num |= 1;
		}
		if (floats.Count > 0)
		{
			num |= 2;
		}
		if (vec3s.Count > 0)
		{
			num |= 4;
		}
		if (quaternions.Count > 0)
		{
			num |= 8;
		}
		if (ints.Count > 0)
		{
			num |= 16;
		}
		if (longs.Count > 0)
		{
			num |= 32;
		}
		if (strings.Count > 0)
		{
			num |= 64;
		}
		if (byteArrays.Count > 0)
		{
			num |= 128;
		}
		bool flag = this.m_rotation != Quaternion.identity.eulerAngles;
		num |= (this.Persistent ? 256 : 0);
		num |= (this.Distant ? 512 : 0);
		num |= (ushort)(this.Type << 10);
		num |= (flag ? 4096 : 0);
		pkg.Write(num);
		pkg.Write(this.m_prefab);
		if (flag)
		{
			pkg.Write(this.m_rotation);
		}
		if ((num & 255) == 0)
		{
			return;
		}
		if ((num & 1) != 0)
		{
			pkg.Write((byte)connection.m_type);
			pkg.Write(connection.m_target);
		}
		if (floats.Count > 0)
		{
			pkg.Write((byte)floats.Count);
			foreach (KeyValuePair<int, float> keyValuePair in floats)
			{
				pkg.Write(keyValuePair.Key);
				pkg.Write(keyValuePair.Value);
			}
		}
		if (vec3s.Count > 0)
		{
			pkg.Write((byte)vec3s.Count);
			foreach (KeyValuePair<int, Vector3> keyValuePair2 in vec3s)
			{
				pkg.Write(keyValuePair2.Key);
				pkg.Write(keyValuePair2.Value);
			}
		}
		if (quaternions.Count > 0)
		{
			pkg.Write((byte)quaternions.Count);
			foreach (KeyValuePair<int, Quaternion> keyValuePair3 in quaternions)
			{
				pkg.Write(keyValuePair3.Key);
				pkg.Write(keyValuePair3.Value);
			}
		}
		if (ints.Count > 0)
		{
			pkg.Write((byte)ints.Count);
			foreach (KeyValuePair<int, int> keyValuePair4 in ints)
			{
				pkg.Write(keyValuePair4.Key);
				pkg.Write(keyValuePair4.Value);
			}
		}
		if (longs.Count > 0)
		{
			pkg.Write((byte)longs.Count);
			foreach (KeyValuePair<int, long> keyValuePair5 in longs)
			{
				pkg.Write(keyValuePair5.Key);
				pkg.Write(keyValuePair5.Value);
			}
		}
		if (strings.Count > 0)
		{
			pkg.Write((byte)strings.Count);
			foreach (KeyValuePair<int, string> keyValuePair6 in strings)
			{
				pkg.Write(keyValuePair6.Key);
				pkg.Write(keyValuePair6.Value);
			}
		}
		if (byteArrays.Count > 0)
		{
			pkg.Write((byte)byteArrays.Count);
			foreach (KeyValuePair<int, byte[]> keyValuePair7 in byteArrays)
			{
				pkg.Write(keyValuePair7.Key);
				pkg.Write(keyValuePair7.Value);
			}
		}
	}

	// Token: 0x06000C8D RID: 3213 RVA: 0x00064060 File Offset: 0x00062260
	public void Deserialize(ZPackage pkg)
	{
		ushort num = pkg.ReadUShort();
		this.Persistent = ((num & 256) > 0);
		this.Distant = ((num & 512) > 0);
		this.Type = (ZDO.ObjectType)(num >> 10 & 3);
		this.m_prefab = pkg.ReadInt();
		if ((num & 4096) > 0)
		{
			this.m_rotation = pkg.ReadVector3();
		}
		if ((num & 255) == 0)
		{
			return;
		}
		bool flag = (num & 1) > 0;
		bool flag2 = (num & 2) > 0;
		bool flag3 = (num & 4) > 0;
		bool flag4 = (num & 8) > 0;
		bool flag5 = (num & 16) > 0;
		bool flag6 = (num & 32) > 0;
		bool flag7 = (num & 64) > 0;
		bool flag8 = (num & 128) > 0;
		if (flag)
		{
			ZDOExtraData.ConnectionType connectionType = (ZDOExtraData.ConnectionType)pkg.ReadByte();
			ZDOID target = pkg.ReadZDOID();
			ZDOExtraData.SetConnection(this.m_uid, connectionType, target);
		}
		if (flag2)
		{
			int num2 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Float, num2);
			for (int i = 0; i < num2; i++)
			{
				int hash = pkg.ReadInt();
				float value = pkg.ReadSingle();
				ZDOExtraData.Set(this.m_uid, hash, value);
			}
		}
		if (flag3)
		{
			int num3 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Vec3, num3);
			for (int j = 0; j < num3; j++)
			{
				int hash2 = pkg.ReadInt();
				Vector3 value2 = pkg.ReadVector3();
				ZDOExtraData.Set(this.m_uid, hash2, value2);
			}
		}
		if (flag4)
		{
			int num4 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Quat, num4);
			for (int k = 0; k < num4; k++)
			{
				int hash3 = pkg.ReadInt();
				Quaternion value3 = pkg.ReadQuaternion();
				ZDOExtraData.Set(this.m_uid, hash3, value3);
			}
		}
		if (flag5)
		{
			int num5 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Int, num5);
			for (int l = 0; l < num5; l++)
			{
				int hash4 = pkg.ReadInt();
				int value4 = pkg.ReadInt();
				ZDOExtraData.Set(this.m_uid, hash4, value4);
			}
		}
		if (flag6)
		{
			int num6 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Long, num6);
			for (int m = 0; m < num6; m++)
			{
				int hash5 = pkg.ReadInt();
				long value5 = pkg.ReadLong();
				ZDOExtraData.Set(this.m_uid, hash5, value5);
			}
		}
		if (flag7)
		{
			int num7 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.String, num7);
			for (int n = 0; n < num7; n++)
			{
				int hash6 = pkg.ReadInt();
				string value6 = pkg.ReadString();
				ZDOExtraData.Set(this.m_uid, hash6, value6);
			}
		}
		if (flag8)
		{
			int num8 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.ByteArray, num8);
			for (int num9 = 0; num9 < num8; num9++)
			{
				int hash7 = pkg.ReadInt();
				byte[] value7 = pkg.ReadByteArray();
				ZDOExtraData.Set(this.m_uid, hash7, value7);
			}
		}
	}

	// Token: 0x06000C8E RID: 3214 RVA: 0x00064340 File Offset: 0x00062540
	public void Save(ZPackage pkg)
	{
		List<KeyValuePair<int, float>> saveFloats = ZDOExtraData.GetSaveFloats(this.m_uid);
		List<KeyValuePair<int, Vector3>> saveVec3s = ZDOExtraData.GetSaveVec3s(this.m_uid);
		List<KeyValuePair<int, Quaternion>> saveQuaternions = ZDOExtraData.GetSaveQuaternions(this.m_uid);
		List<KeyValuePair<int, int>> saveInts = ZDOExtraData.GetSaveInts(this.m_uid);
		List<KeyValuePair<int, long>> saveLongs = ZDOExtraData.GetSaveLongs(this.m_uid);
		List<KeyValuePair<int, string>> saveStrings = ZDOExtraData.GetSaveStrings(this.m_uid);
		List<KeyValuePair<int, byte[]>> saveByteArrays = ZDOExtraData.GetSaveByteArrays(this.m_uid);
		ZDOConnectionHashData saveConnections = ZDOExtraData.GetSaveConnections(this.m_uid);
		ushort num = 0;
		if (saveConnections != null && saveConnections.m_type != ZDOExtraData.ConnectionType.None)
		{
			num |= 1;
		}
		if (saveFloats.Count > 0)
		{
			num |= 2;
		}
		if (saveVec3s.Count > 0)
		{
			num |= 4;
		}
		if (saveQuaternions.Count > 0)
		{
			num |= 8;
		}
		if (saveInts.Count > 0)
		{
			num |= 16;
		}
		if (saveLongs.Count > 0)
		{
			num |= 32;
		}
		if (saveStrings.Count > 0)
		{
			num |= 64;
		}
		if (saveByteArrays.Count > 0)
		{
			num |= 128;
		}
		bool flag = this.m_rotation != Quaternion.identity.eulerAngles;
		num |= (this.Persistent ? 256 : 0);
		num |= (this.Distant ? 512 : 0);
		num |= (ushort)(this.Type << 10);
		num |= (flag ? 4096 : 0);
		pkg.Write(num);
		pkg.Write(this.m_sector);
		pkg.Write(this.m_position);
		pkg.Write(this.m_prefab);
		if (flag)
		{
			pkg.Write(this.m_rotation);
		}
		if ((num & 255) == 0)
		{
			return;
		}
		if ((num & 1) != 0)
		{
			pkg.Write((byte)saveConnections.m_type);
			pkg.Write(saveConnections.m_hash);
		}
		ZDODataHelper.WriteData<float>(pkg, saveFloats, delegate(float value)
		{
			pkg.Write(value);
		});
		ZDODataHelper.WriteData<Vector3>(pkg, saveVec3s, delegate(Vector3 value)
		{
			pkg.Write(value);
		});
		ZDODataHelper.WriteData<Quaternion>(pkg, saveQuaternions, delegate(Quaternion value)
		{
			pkg.Write(value);
		});
		ZDODataHelper.WriteData<int>(pkg, saveInts, delegate(int value)
		{
			pkg.Write(value);
		});
		ZDODataHelper.WriteData<long>(pkg, saveLongs, delegate(long value)
		{
			pkg.Write(value);
		});
		ZDODataHelper.WriteData<string>(pkg, saveStrings, delegate(string value)
		{
			pkg.Write(value);
		});
		ZDODataHelper.WriteData<byte[]>(pkg, saveByteArrays, delegate(byte[] value)
		{
			pkg.Write(value);
		});
	}

	// Token: 0x06000C8F RID: 3215 RVA: 0x000645F0 File Offset: 0x000627F0
	private static bool Strip(int key)
	{
		return ZDOHelper.s_stripOldData.Contains(key);
	}

	// Token: 0x06000C90 RID: 3216 RVA: 0x000645FD File Offset: 0x000627FD
	private static bool StripLong(int key)
	{
		return ZDOHelper.s_stripOldLongData.Contains(key);
	}

	// Token: 0x06000C91 RID: 3217 RVA: 0x0006460A File Offset: 0x0006280A
	private static bool Strip(int key, long data)
	{
		return data == 0L || ZDO.StripLong(key) || ZDO.Strip(key);
	}

	// Token: 0x06000C92 RID: 3218 RVA: 0x0006461F File Offset: 0x0006281F
	private static bool Strip(int key, int data)
	{
		return data == 0 || ZDO.Strip(key);
	}

	// Token: 0x06000C93 RID: 3219 RVA: 0x0006462C File Offset: 0x0006282C
	private static bool Strip(int key, Quaternion data)
	{
		return data == Quaternion.identity || ZDO.Strip(key);
	}

	// Token: 0x06000C94 RID: 3220 RVA: 0x00064643 File Offset: 0x00062843
	private static bool Strip(int key, string data)
	{
		return string.IsNullOrEmpty(data) || ZDO.Strip(key);
	}

	// Token: 0x06000C95 RID: 3221 RVA: 0x00064655 File Offset: 0x00062855
	private static bool Strip(int key, byte[] data)
	{
		return data.Length == 0 || ZDOHelper.s_stripOldDataByteArray.Contains(key);
	}

	// Token: 0x06000C96 RID: 3222 RVA: 0x00064668 File Offset: 0x00062868
	private static bool StripConvert(ZDOID zid, int key, long data)
	{
		if (ZDO.Strip(key))
		{
			return true;
		}
		if (key == ZDOVars.s_SpawnTime__DontUse || key == ZDOVars.s_spawn_time__DontUse)
		{
			ZDOExtraData.Set(zid, ZDOVars.s_spawnTime, data);
			return true;
		}
		return false;
	}

	// Token: 0x06000C97 RID: 3223 RVA: 0x00064694 File Offset: 0x00062894
	private static bool StripConvert(ZDOID zid, int key, Vector3 data)
	{
		if (ZDO.Strip(key))
		{
			return true;
		}
		if (key == ZDOVars.s_SpawnPoint__DontUse)
		{
			ZDOExtraData.Set(zid, ZDOVars.s_spawnPoint, data);
			return true;
		}
		if (Mathf.Approximately(data.x, data.y) && Mathf.Approximately(data.x, data.z))
		{
			if (key == ZDOVars.s_scaleHash)
			{
				if (Mathf.Approximately(data.x, 1f))
				{
					return true;
				}
				ZDOExtraData.Set(zid, ZDOVars.s_scaleScalarHash, data.x);
				return true;
			}
			else if (Mathf.Approximately(data.x, 0f))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000C98 RID: 3224 RVA: 0x0006472D File Offset: 0x0006292D
	private static bool StripConvert(ZDOID zid, int key, float data)
	{
		return ZDO.Strip(key) || (key == ZDOVars.s_scaleScalarHash && Mathf.Approximately(data, 1f));
	}

	// Token: 0x06000C99 RID: 3225 RVA: 0x00064754 File Offset: 0x00062954
	public void LoadOldFormat(ZPackage pkg, int version)
	{
		pkg.ReadUInt();
		pkg.ReadUInt();
		this.Persistent = pkg.ReadBool();
		pkg.ReadLong();
		long timeCreated = pkg.ReadLong();
		ZDOExtraData.SetTimeCreated(this.m_uid, timeCreated);
		pkg.ReadInt();
		if (version >= 16 && version < 24)
		{
			pkg.ReadInt();
		}
		if (version >= 23)
		{
			this.Type = (ZDO.ObjectType)(pkg.ReadSByte() & 3);
		}
		if (version >= 22)
		{
			this.Distant = pkg.ReadBool();
		}
		if (version < 13)
		{
			pkg.ReadChar();
			pkg.ReadChar();
		}
		if (version >= 17)
		{
			this.m_prefab = pkg.ReadInt();
		}
		this.m_sector = pkg.ReadVector2i().ClampToShort();
		this.m_position = pkg.ReadVector3();
		this.m_rotation = pkg.ReadQuaternion().eulerAngles;
		int num = (int)pkg.ReadChar();
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				int num2 = pkg.ReadInt();
				float num3 = pkg.ReadSingle();
				if (!ZDO.StripConvert(this.m_uid, num2, num3))
				{
					ZDOExtraData.Set(this.m_uid, num2, num3);
				}
			}
		}
		int num4 = (int)pkg.ReadChar();
		if (num4 > 0)
		{
			for (int j = 0; j < num4; j++)
			{
				int num5 = pkg.ReadInt();
				Vector3 vector = pkg.ReadVector3();
				if (!ZDO.StripConvert(this.m_uid, num5, vector))
				{
					ZDOExtraData.Set(this.m_uid, num5, vector);
				}
			}
		}
		int num6 = (int)pkg.ReadChar();
		if (num6 > 0)
		{
			for (int k = 0; k < num6; k++)
			{
				int num7 = pkg.ReadInt();
				Quaternion value = pkg.ReadQuaternion();
				if (!ZDO.Strip(num7))
				{
					ZDOExtraData.Set(this.m_uid, num7, value);
				}
			}
		}
		int num8 = (int)pkg.ReadChar();
		if (num8 > 0)
		{
			for (int l = 0; l < num8; l++)
			{
				int num9 = pkg.ReadInt();
				int value2 = pkg.ReadInt();
				if (!ZDO.Strip(num9))
				{
					ZDOExtraData.Set(this.m_uid, num9, value2);
				}
			}
		}
		int num10 = (int)pkg.ReadChar();
		if (num10 > 0)
		{
			for (int m = 0; m < num10; m++)
			{
				int num11 = pkg.ReadInt();
				long num12 = pkg.ReadLong();
				if (!ZDO.StripConvert(this.m_uid, num11, num12))
				{
					ZDOExtraData.Set(this.m_uid, num11, num12);
				}
			}
		}
		int num13 = (int)pkg.ReadChar();
		if (num13 > 0)
		{
			for (int n = 0; n < num13; n++)
			{
				int num14 = pkg.ReadInt();
				string value3 = pkg.ReadString();
				if (!ZDO.Strip(num14))
				{
					ZDOExtraData.Set(this.m_uid, num14, value3);
				}
			}
		}
		if (version >= 27)
		{
			int num15 = (int)pkg.ReadChar();
			if (num15 > 0)
			{
				for (int num16 = 0; num16 < num15; num16++)
				{
					int num17 = pkg.ReadInt();
					byte[] value4 = pkg.ReadByteArray();
					if (!ZDO.Strip(num17))
					{
						ZDOExtraData.Set(this.m_uid, num17, value4);
					}
				}
			}
		}
		if (version < 17)
		{
			this.m_prefab = this.GetInt("prefab", 0);
		}
	}

	// Token: 0x06000C9A RID: 3226 RVA: 0x00064A44 File Offset: 0x00062C44
	private int ReadNumItems(ZPackage pkg, int version)
	{
		if (version < 33)
		{
			return (int)pkg.ReadByte();
		}
		return pkg.ReadNumItems();
	}

	// Token: 0x06000C9B RID: 3227 RVA: 0x00064A58 File Offset: 0x00062C58
	public void Load(ZPackage pkg, int version)
	{
		this.m_uid.SetID(ZDOID.m_loadID += 1U);
		ushort num = pkg.ReadUShort();
		this.Persistent = ((num & 256) > 0);
		this.Distant = ((num & 512) > 0);
		this.Type = (ZDO.ObjectType)(num >> 10 & 3);
		this.m_sector = pkg.ReadVector2s();
		this.m_position = pkg.ReadVector3();
		this.m_prefab = pkg.ReadInt();
		this.OwnerRevision = 0;
		this.DataRevision = 0U;
		this.Owned = false;
		this.Owner = false;
		this.Valid = true;
		this.SaveClone = false;
		if ((num & 4096) > 0)
		{
			this.m_rotation = pkg.ReadVector3();
		}
		if ((num & 255) == 0)
		{
			return;
		}
		bool flag = (num & 1) > 0;
		bool flag2 = (num & 2) > 0;
		bool flag3 = (num & 4) > 0;
		bool flag4 = (num & 8) > 0;
		bool flag5 = (num & 16) > 0;
		bool flag6 = (num & 32) > 0;
		bool flag7 = (num & 64) > 0;
		bool flag8 = (num & 128) > 0;
		if (flag)
		{
			ZDOExtraData.ConnectionType connectionType = (ZDOExtraData.ConnectionType)pkg.ReadByte();
			int hash = pkg.ReadInt();
			ZDOExtraData.SetConnectionData(this.m_uid, connectionType, hash);
		}
		if (flag2)
		{
			int num2 = this.ReadNumItems(pkg, version);
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Float, num2);
			for (int i = 0; i < num2; i++)
			{
				int num3 = pkg.ReadInt();
				float num4 = pkg.ReadSingle();
				if (!ZDO.StripConvert(this.m_uid, num3, num4))
				{
					ZDOExtraData.Add(this.m_uid, num3, num4);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Float);
		}
		if (flag3)
		{
			int num5 = this.ReadNumItems(pkg, version);
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Vec3, num5);
			for (int j = 0; j < num5; j++)
			{
				int num6 = pkg.ReadInt();
				Vector3 vector = pkg.ReadVector3();
				if (!ZDO.StripConvert(this.m_uid, num6, vector))
				{
					ZDOExtraData.Add(this.m_uid, num6, vector);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Vec3);
		}
		if (flag4)
		{
			int num7 = this.ReadNumItems(pkg, version);
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Quat, num7);
			for (int k = 0; k < num7; k++)
			{
				int num8 = pkg.ReadInt();
				Quaternion quaternion = pkg.ReadQuaternion();
				if (!ZDO.Strip(num8, quaternion))
				{
					ZDOExtraData.Add(this.m_uid, num8, quaternion);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Quat);
		}
		if (flag5)
		{
			int num9 = this.ReadNumItems(pkg, version);
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Int, num9);
			for (int l = 0; l < num9; l++)
			{
				int num10 = pkg.ReadInt();
				int num11 = pkg.ReadInt();
				if (!ZDO.Strip(num10, num11))
				{
					ZDOExtraData.Add(this.m_uid, num10, num11);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Int);
		}
		if (flag6)
		{
			int num12 = this.ReadNumItems(pkg, version);
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Long, num12);
			for (int m = 0; m < num12; m++)
			{
				int num13 = pkg.ReadInt();
				long num14 = pkg.ReadLong();
				if (!ZDO.Strip(num13, num14))
				{
					ZDOExtraData.Add(this.m_uid, num13, num14);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Long);
		}
		if (flag7)
		{
			int num15 = this.ReadNumItems(pkg, version);
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.String, num15);
			for (int n = 0; n < num15; n++)
			{
				int num16 = pkg.ReadInt();
				string text = pkg.ReadString();
				if (!ZDO.Strip(num16, text))
				{
					ZDOExtraData.Add(this.m_uid, num16, text);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.String);
		}
		if (flag8)
		{
			int num17 = this.ReadNumItems(pkg, version);
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.ByteArray, num17);
			for (int num18 = 0; num18 < num17; num18++)
			{
				int num19 = pkg.ReadInt();
				byte[] array = pkg.ReadByteArray();
				if (!ZDO.Strip(num19, array))
				{
					ZDOExtraData.Add(this.m_uid, num19, array);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.ByteArray);
		}
	}

	// Token: 0x06000C9C RID: 3228 RVA: 0x00064E42 File Offset: 0x00063042
	public long GetOwner()
	{
		if (!this.Owned)
		{
			return 0L;
		}
		return ZDOExtraData.GetOwner(this.m_uid);
	}

	// Token: 0x06000C9D RID: 3229 RVA: 0x00064E5A File Offset: 0x0006305A
	public bool IsOwner()
	{
		return this.Owner;
	}

	// Token: 0x06000C9E RID: 3230 RVA: 0x00064E62 File Offset: 0x00063062
	public bool HasOwner()
	{
		return this.Owned;
	}

	// Token: 0x06000C9F RID: 3231 RVA: 0x00064E6A File Offset: 0x0006306A
	public void SetOwner(long uid)
	{
		if (ZDOExtraData.GetOwner(this.m_uid) == uid)
		{
			return;
		}
		this.SetOwnerInternal(uid);
		this.IncreaseOwnerRevision();
	}

	// Token: 0x06000CA0 RID: 3232 RVA: 0x00064E88 File Offset: 0x00063088
	public void SetOwnerInternal(long uid)
	{
		if (uid == 0L)
		{
			ZDOExtraData.ReleaseOwner(this.m_uid);
			this.Owned = false;
			this.Owner = false;
			return;
		}
		ushort ownerKey = ZDOID.AddUser(uid);
		ZDOExtraData.SetOwner(this.m_uid, ownerKey);
		this.Owned = true;
		this.Owner = (uid == ZDOMan.GetSessionID());
	}

	// Token: 0x17000068 RID: 104
	// (get) Token: 0x06000CA1 RID: 3233 RVA: 0x00064EDA File Offset: 0x000630DA
	// (set) Token: 0x06000CA2 RID: 3234 RVA: 0x00064EE7 File Offset: 0x000630E7
	public bool Persistent
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Persistent) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Persistent;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Persistent;
		}
	}

	// Token: 0x17000069 RID: 105
	// (get) Token: 0x06000CA3 RID: 3235 RVA: 0x00064F0D File Offset: 0x0006310D
	// (set) Token: 0x06000CA4 RID: 3236 RVA: 0x00064F1A File Offset: 0x0006311A
	public bool Distant
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Distant) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Distant;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Distant;
		}
	}

	// Token: 0x1700006A RID: 106
	// (get) Token: 0x06000CA5 RID: 3237 RVA: 0x00064F40 File Offset: 0x00063140
	// (set) Token: 0x06000CA6 RID: 3238 RVA: 0x00064F4E File Offset: 0x0006314E
	private bool Owner
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Owner) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Owner;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Owner;
		}
	}

	// Token: 0x1700006B RID: 107
	// (get) Token: 0x06000CA7 RID: 3239 RVA: 0x00064F75 File Offset: 0x00063175
	// (set) Token: 0x06000CA8 RID: 3240 RVA: 0x00064F83 File Offset: 0x00063183
	private bool Owned
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Owned) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Owned;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Owned;
		}
	}

	// Token: 0x1700006C RID: 108
	// (get) Token: 0x06000CA9 RID: 3241 RVA: 0x00064FAA File Offset: 0x000631AA
	// (set) Token: 0x06000CAA RID: 3242 RVA: 0x00064FBB File Offset: 0x000631BB
	private bool Valid
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Valid) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Valid;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Valid;
		}
	}

	// Token: 0x1700006D RID: 109
	// (get) Token: 0x06000CAB RID: 3243 RVA: 0x00064FE2 File Offset: 0x000631E2
	// (set) Token: 0x06000CAC RID: 3244 RVA: 0x00064FF0 File Offset: 0x000631F0
	public bool Created
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Created) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Created;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Created;
		}
	}

	// Token: 0x1700006E RID: 110
	// (get) Token: 0x06000CAD RID: 3245 RVA: 0x00065017 File Offset: 0x00063217
	// (set) Token: 0x06000CAE RID: 3246 RVA: 0x00065021 File Offset: 0x00063221
	public ZDO.ObjectType Type
	{
		get
		{
			return (ZDO.ObjectType)(this.m_dataFlags & ZDO.DataFlags.Type);
		}
		set
		{
			this.m_dataFlags = ((this.m_dataFlags & ~ZDO.DataFlags.Type) | (ZDO.DataFlags)(value & ZDO.ObjectType.Terrain));
		}
	}

	// Token: 0x1700006F RID: 111
	// (get) Token: 0x06000CAF RID: 3247 RVA: 0x00065039 File Offset: 0x00063239
	// (set) Token: 0x06000CB0 RID: 3248 RVA: 0x00065048 File Offset: 0x00063248
	private bool SaveClone
	{
		get
		{
			return this.m_tempSortValue < 0f;
		}
		set
		{
			if (value)
			{
				this.m_tempSortValue = -1f;
				return;
			}
			this.m_tempSortValue = 0f;
		}
	}

	// Token: 0x17000070 RID: 112
	// (get) Token: 0x06000CB1 RID: 3249 RVA: 0x00065064 File Offset: 0x00063264
	// (set) Token: 0x06000CB2 RID: 3250 RVA: 0x0006506C File Offset: 0x0006326C
	public byte TempRemoveEarmark
	{
		get
		{
			return this.m_tempRemoveEarmark;
		}
		set
		{
			this.m_tempRemoveEarmark = value;
		}
	}

	// Token: 0x17000071 RID: 113
	// (get) Token: 0x06000CB3 RID: 3251 RVA: 0x00065075 File Offset: 0x00063275
	// (set) Token: 0x06000CB4 RID: 3252 RVA: 0x0006507D File Offset: 0x0006327D
	public ushort OwnerRevision { get; set; }

	// Token: 0x17000072 RID: 114
	// (get) Token: 0x06000CB5 RID: 3253 RVA: 0x00065086 File Offset: 0x00063286
	// (set) Token: 0x06000CB6 RID: 3254 RVA: 0x0006508E File Offset: 0x0006328E
	public uint DataRevision { get; set; }

	// Token: 0x04000D20 RID: 3360
	public ZDOID m_uid = ZDOID.None;

	// Token: 0x04000D23 RID: 3363
	public float m_tempSortValue;

	// Token: 0x04000D24 RID: 3364
	private int m_prefab;

	// Token: 0x04000D25 RID: 3365
	private Vector2s m_sector = Vector2s.zero;

	// Token: 0x04000D26 RID: 3366
	private Vector3 m_rotation = Quaternion.identity.eulerAngles;

	// Token: 0x04000D27 RID: 3367
	private Vector3 m_position = Vector3.zero;

	// Token: 0x04000D28 RID: 3368
	private ZDO.DataFlags m_dataFlags;

	// Token: 0x04000D29 RID: 3369
	private byte m_tempRemoveEarmark = byte.MaxValue;

	// Token: 0x020002D9 RID: 729
	[Flags]
	private enum DataFlags : byte
	{
		// Token: 0x04002317 RID: 8983
		None = 0,
		// Token: 0x04002318 RID: 8984
		Type = 3,
		// Token: 0x04002319 RID: 8985
		Persistent = 4,
		// Token: 0x0400231A RID: 8986
		Distant = 8,
		// Token: 0x0400231B RID: 8987
		Created = 16,
		// Token: 0x0400231C RID: 8988
		Owner = 32,
		// Token: 0x0400231D RID: 8989
		Owned = 64,
		// Token: 0x0400231E RID: 8990
		Valid = 128
	}

	// Token: 0x020002DA RID: 730
	public enum ObjectType : byte
	{
		// Token: 0x04002320 RID: 8992
		Default,
		// Token: 0x04002321 RID: 8993
		Prioritized,
		// Token: 0x04002322 RID: 8994
		Solid,
		// Token: 0x04002323 RID: 8995
		Terrain
	}
}
