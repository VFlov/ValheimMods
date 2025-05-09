using System;
using System.Runtime.CompilerServices;

// Token: 0x0200014F RID: 335
public struct GameVersion
{
	// Token: 0x06001452 RID: 5202 RVA: 0x00095259 File Offset: 0x00093459
	public GameVersion(int major, int minor, int patch)
	{
		this.m_major = major;
		this.m_minor = minor;
		this.m_patch = patch;
	}

	// Token: 0x06001453 RID: 5203 RVA: 0x00095270 File Offset: 0x00093470
	public static GameVersion ParseGameVersion(string versionString)
	{
		GameVersion result;
		GameVersion.TryParseGameVersion(versionString, out result);
		return result;
	}

	// Token: 0x06001454 RID: 5204 RVA: 0x00095288 File Offset: 0x00093488
	public static bool TryParseGameVersion(string versionString, out GameVersion version)
	{
		version = default(GameVersion);
		string[] array = versionString.Split('.', StringSplitOptions.None);
		if (array.Length < 2)
		{
			return false;
		}
		int major;
		int minor;
		if (!GameVersion.<TryParseGameVersion>g__TryGetFirstIntFromString|5_0(array[0], out major) || !GameVersion.<TryParseGameVersion>g__TryGetFirstIntFromString|5_0(array[1], out minor))
		{
			return false;
		}
		if (array.Length == 2)
		{
			version = new GameVersion(major, minor, 0);
			return true;
		}
		int num;
		if (array[2].StartsWith("rc"))
		{
			if (!GameVersion.<TryParseGameVersion>g__TryGetFirstIntFromString|5_0(array[2].Substring(2), out num))
			{
				return false;
			}
			num = -num;
		}
		else if (!GameVersion.<TryParseGameVersion>g__TryGetFirstIntFromString|5_0(array[2], out num))
		{
			return false;
		}
		version = new GameVersion(major, minor, num);
		return true;
	}

	// Token: 0x06001455 RID: 5205 RVA: 0x00095323 File Offset: 0x00093523
	public bool Equals(GameVersion other)
	{
		return this.m_major == other.m_major && this.m_minor == other.m_minor && this.m_patch == other.m_patch;
	}

	// Token: 0x06001456 RID: 5206 RVA: 0x00095354 File Offset: 0x00093554
	private static bool IsVersionNewer(GameVersion other, GameVersion reference)
	{
		if (other.m_major > reference.m_major)
		{
			return true;
		}
		if (other.m_major == reference.m_major && other.m_minor > reference.m_minor)
		{
			return true;
		}
		if (other.m_major != reference.m_major || other.m_minor != reference.m_minor)
		{
			return false;
		}
		if (reference.m_patch >= 0)
		{
			return other.m_patch > reference.m_patch;
		}
		return other.m_patch >= 0 || other.m_patch < reference.m_patch;
	}

	// Token: 0x06001457 RID: 5207 RVA: 0x000953E0 File Offset: 0x000935E0
	public bool IsValid()
	{
		return this != default(GameVersion);
	}

	// Token: 0x06001458 RID: 5208 RVA: 0x00095404 File Offset: 0x00093604
	public override string ToString()
	{
		if (!this.IsValid())
		{
			return "";
		}
		string result;
		if (this.m_patch == 0)
		{
			result = this.m_major.ToString() + "." + this.m_minor.ToString();
		}
		else if (this.m_patch < 0)
		{
			result = string.Concat(new string[]
			{
				this.m_major.ToString(),
				".",
				this.m_minor.ToString(),
				".rc",
				(-this.m_patch).ToString()
			});
		}
		else
		{
			result = string.Concat(new string[]
			{
				this.m_major.ToString(),
				".",
				this.m_minor.ToString(),
				".",
				this.m_patch.ToString()
			});
		}
		return result;
	}

	// Token: 0x06001459 RID: 5209 RVA: 0x000954E9 File Offset: 0x000936E9
	public override bool Equals(object other)
	{
		return other != null && other is GameVersion && this.Equals((GameVersion)other);
	}

	// Token: 0x0600145A RID: 5210 RVA: 0x00095506 File Offset: 0x00093706
	public override int GetHashCode()
	{
		return ((313811945 * -1521134295 + this.m_major.GetHashCode()) * -1521134295 + this.m_minor.GetHashCode()) * -1521134295 + this.m_patch.GetHashCode();
	}

	// Token: 0x0600145B RID: 5211 RVA: 0x00095543 File Offset: 0x00093743
	public static bool operator ==(GameVersion lhs, GameVersion rhs)
	{
		return lhs.Equals(rhs);
	}

	// Token: 0x0600145C RID: 5212 RVA: 0x0009554D File Offset: 0x0009374D
	public static bool operator !=(GameVersion lhs, GameVersion rhs)
	{
		return !(lhs == rhs);
	}

	// Token: 0x0600145D RID: 5213 RVA: 0x00095559 File Offset: 0x00093759
	public static bool operator >(GameVersion lhs, GameVersion rhs)
	{
		return GameVersion.IsVersionNewer(lhs, rhs);
	}

	// Token: 0x0600145E RID: 5214 RVA: 0x00095562 File Offset: 0x00093762
	public static bool operator <(GameVersion lhs, GameVersion rhs)
	{
		return GameVersion.IsVersionNewer(rhs, lhs);
	}

	// Token: 0x0600145F RID: 5215 RVA: 0x0009556B File Offset: 0x0009376B
	public static bool operator >=(GameVersion lhs, GameVersion rhs)
	{
		return lhs == rhs || lhs > rhs;
	}

	// Token: 0x06001460 RID: 5216 RVA: 0x0009557F File Offset: 0x0009377F
	public static bool operator <=(GameVersion lhs, GameVersion rhs)
	{
		return lhs == rhs || lhs < rhs;
	}

	// Token: 0x06001461 RID: 5217 RVA: 0x00095594 File Offset: 0x00093794
	[CompilerGenerated]
	internal static bool <TryParseGameVersion>g__TryGetFirstIntFromString|5_0(string input, out int output)
	{
		output = 0;
		char[] array = new char[input.Length];
		int num = 0;
		for (int i = 0; i < input.Length; i++)
		{
			if ((num == 0 && input[i] == '-') || char.IsNumber(input[i]))
			{
				array[num++] = input[i];
			}
			else if (num > 0)
			{
				break;
			}
		}
		return num > 0 && int.TryParse(new string(array, 0, num), out output);
	}

	// Token: 0x04001412 RID: 5138
	public int m_major;

	// Token: 0x04001413 RID: 5139
	public int m_minor;

	// Token: 0x04001414 RID: 5140
	public int m_patch;
}
