using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000030 RID: 48
public class Skills : MonoBehaviour
{
	// Token: 0x06000495 RID: 1173 RVA: 0x00029560 File Offset: 0x00027760
	public void Awake()
	{
		this.m_player = base.GetComponent<Player>();
	}

	// Token: 0x06000496 RID: 1174 RVA: 0x00029570 File Offset: 0x00027770
	public void Save(ZPackage pkg)
	{
		pkg.Write(2);
		pkg.Write(this.m_skillData.Count);
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			pkg.Write((int)keyValuePair.Value.m_info.m_skill);
			pkg.Write(keyValuePair.Value.m_level);
			pkg.Write(keyValuePair.Value.m_accumulator);
		}
	}

	// Token: 0x06000497 RID: 1175 RVA: 0x00029610 File Offset: 0x00027810
	public void Load(ZPackage pkg)
	{
		int num = pkg.ReadInt();
		this.m_skillData.Clear();
		int num2 = pkg.ReadInt();
		for (int i = 0; i < num2; i++)
		{
			Skills.SkillType skillType = (Skills.SkillType)pkg.ReadInt();
			float level = pkg.ReadSingle();
			float accumulator = (num >= 2) ? pkg.ReadSingle() : 0f;
			if (Skills.IsSkillValid(skillType))
			{
				Skills.Skill skill = this.GetSkill(skillType);
				skill.m_level = level;
				skill.m_accumulator = accumulator;
			}
		}
	}

	// Token: 0x06000498 RID: 1176 RVA: 0x00029682 File Offset: 0x00027882
	private static bool IsSkillValid(Skills.SkillType type)
	{
		return Enum.IsDefined(typeof(Skills.SkillType), type);
	}

	// Token: 0x06000499 RID: 1177 RVA: 0x00029699 File Offset: 0x00027899
	public float GetSkillFactor(Skills.SkillType skillType)
	{
		if (skillType == Skills.SkillType.None)
		{
			return 0f;
		}
		return Mathf.Clamp01(this.GetSkillLevel(skillType) / 100f);
	}

	// Token: 0x0600049A RID: 1178 RVA: 0x000296B8 File Offset: 0x000278B8
	public float GetSkillLevel(Skills.SkillType skillType)
	{
		if (skillType == Skills.SkillType.None)
		{
			return 0f;
		}
		float level = this.GetSkill(skillType).m_level;
		this.m_player.GetSEMan().ModifySkillLevel(skillType, ref level);
		return Mathf.Floor(level);
	}

	// Token: 0x0600049B RID: 1179 RVA: 0x000296F4 File Offset: 0x000278F4
	public void GetRandomSkillRange(out float min, out float max, Skills.SkillType skillType)
	{
		float skillFactor = this.GetSkillFactor(skillType);
		float num = Mathf.Lerp(0.4f, 1f, skillFactor);
		min = Mathf.Clamp01(num - 0.15f);
		max = Mathf.Clamp01(num + 0.15f);
	}

	// Token: 0x0600049C RID: 1180 RVA: 0x00029738 File Offset: 0x00027938
	public float GetRandomSkillFactor(Skills.SkillType skillType)
	{
		float skillFactor = this.GetSkillFactor(skillType);
		float num = Mathf.Lerp(0.4f, 1f, skillFactor);
		float a = Mathf.Clamp01(num - 0.15f);
		float b = Mathf.Clamp01(num + 0.15f);
		return Mathf.Lerp(a, b, UnityEngine.Random.value);
	}

	// Token: 0x0600049D RID: 1181 RVA: 0x00029784 File Offset: 0x00027984
	public void CheatRaiseSkill(string name, float value, bool showMessage = true)
	{
		if (name.ToLower() == "all")
		{
			foreach (Skills.SkillType skillType in Skills.s_allSkills)
			{
				if (skillType != Skills.SkillType.All)
				{
					this.CheatRaiseSkill(skillType.ToString(), value, false);
				}
			}
			if (showMessage)
			{
				this.m_player.Message(MessageHud.MessageType.TopLeft, string.Format("All skills increased by {0}", value), 0, null);
				global::Console.instance.Print(string.Format("All skills increased by {0}", value));
			}
			return;
		}
		Skills.SkillType[] array = Skills.s_allSkills;
		int i = 0;
		while (i < array.Length)
		{
			Skills.SkillType skillType2 = array[i];
			if (skillType2.ToString().ToLower() == name.ToLower() && skillType2 != Skills.SkillType.All && skillType2 != Skills.SkillType.None)
			{
				Skills.Skill skill = this.GetSkill(skillType2);
				skill.m_level += value;
				skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
				if (this.m_useSkillCap)
				{
					this.RebalanceSkills(skillType2);
				}
				if (skill.m_info == null)
				{
					return;
				}
				if (showMessage)
				{
					this.m_player.Message(MessageHud.MessageType.TopLeft, string.Format("Skill increased {0}: {1}", skill.m_info.m_skill, (int)skill.m_level), 0, skill.m_info.m_icon);
					global::Console.instance.Print(string.Format("Skill {0} = {1}", skillType2, skill.m_level));
				}
				return;
			}
			else
			{
				i++;
			}
		}
		global::Console.instance.Print("Skill not found " + name);
	}

	// Token: 0x0600049E RID: 1182 RVA: 0x00029934 File Offset: 0x00027B34
	public void CheatResetSkill(string name)
	{
		if (name.ToLower() == "all")
		{
			foreach (Skills.SkillType skillType in Skills.s_allSkills)
			{
				if (skillType != Skills.SkillType.All)
				{
					this.ResetSkill(skillType);
				}
			}
			this.m_player.Message(MessageHud.MessageType.TopLeft, "All skills reset", 0, null);
			global::Console.instance.Print("All skills reset");
			return;
		}
		foreach (Skills.SkillType skillType2 in Skills.s_allSkills)
		{
			if (skillType2.ToString().ToLower() == name.ToLower())
			{
				this.ResetSkill(skillType2);
				global::Console.instance.Print(string.Format("Skill {0} reset", skillType2));
				return;
			}
		}
		global::Console.instance.Print("Skill not found " + name);
	}

	// Token: 0x0600049F RID: 1183 RVA: 0x00029A0B File Offset: 0x00027C0B
	public void ResetSkill(Skills.SkillType skillType)
	{
		this.m_skillData.Remove(skillType);
	}

	// Token: 0x060004A0 RID: 1184 RVA: 0x00029A1C File Offset: 0x00027C1C
	public void RaiseSkill(Skills.SkillType skillType, float factor = 1f)
	{
		if (skillType == Skills.SkillType.None)
		{
			return;
		}
		Skills.Skill skill = this.GetSkill(skillType);
		float level = skill.m_level;
		if (skill.Raise(factor))
		{
			if (this.m_useSkillCap)
			{
				this.RebalanceSkills(skillType);
			}
			this.m_player.OnSkillLevelup(skillType, skill.m_level);
			MessageHud.MessageType type = ((int)level == 0) ? MessageHud.MessageType.Center : MessageHud.MessageType.TopLeft;
			this.m_player.Message(type, "$msg_skillup $skill_" + skill.m_info.m_skill.ToString().ToLower() + ": " + ((int)skill.m_level).ToString(), 0, skill.m_info.m_icon);
			Gogan.LogEvent("Game", "Levelup", skillType.ToString(), (long)((int)skill.m_level));
		}
	}

	// Token: 0x060004A1 RID: 1185 RVA: 0x00029AE8 File Offset: 0x00027CE8
	private void RebalanceSkills(Skills.SkillType skillType)
	{
		if (this.GetTotalSkill() < this.m_totalSkillCap)
		{
			return;
		}
		float level = this.GetSkill(skillType).m_level;
		float num = this.m_totalSkillCap - level;
		float num2 = 0f;
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			if (keyValuePair.Key != skillType)
			{
				num2 += keyValuePair.Value.m_level;
			}
		}
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair2 in this.m_skillData)
		{
			if (keyValuePair2.Key != skillType)
			{
				keyValuePair2.Value.m_level = keyValuePair2.Value.m_level / num2 * num;
			}
		}
	}

	// Token: 0x060004A2 RID: 1186 RVA: 0x00029BDC File Offset: 0x00027DDC
	public void Clear()
	{
		this.m_skillData.Clear();
	}

	// Token: 0x060004A3 RID: 1187 RVA: 0x00029BE9 File Offset: 0x00027DE9
	public void OnDeath()
	{
		this.LowerAllSkills(this.m_DeathLowerFactor * Game.m_skillReductionRate);
	}

	// Token: 0x060004A4 RID: 1188 RVA: 0x00029C00 File Offset: 0x00027E00
	public void LowerAllSkills(float factor)
	{
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			float num = keyValuePair.Value.m_level * factor;
			keyValuePair.Value.m_level -= num;
			keyValuePair.Value.m_accumulator = 0f;
		}
		this.m_player.Message(MessageHud.MessageType.TopLeft, "$msg_skills_lowered", 0, null);
	}

	// Token: 0x060004A5 RID: 1189 RVA: 0x00029C94 File Offset: 0x00027E94
	private Skills.Skill GetSkill(Skills.SkillType skillType)
	{
		Skills.Skill skill;
		if (this.m_skillData.TryGetValue(skillType, out skill))
		{
			return skill;
		}
		skill = new Skills.Skill(this.GetSkillDef(skillType));
		this.m_skillData.Add(skillType, skill);
		return skill;
	}

	// Token: 0x060004A6 RID: 1190 RVA: 0x00029CD0 File Offset: 0x00027ED0
	private Skills.SkillDef GetSkillDef(Skills.SkillType type)
	{
		foreach (Skills.SkillDef skillDef in this.m_skills)
		{
			if (skillDef.m_skill == type)
			{
				return skillDef;
			}
		}
		return null;
	}

	// Token: 0x060004A7 RID: 1191 RVA: 0x00029D2C File Offset: 0x00027F2C
	public List<Skills.Skill> GetSkillList()
	{
		List<Skills.Skill> list = new List<Skills.Skill>();
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			list.Add(keyValuePair.Value);
		}
		return list;
	}

	// Token: 0x060004A8 RID: 1192 RVA: 0x00029D8C File Offset: 0x00027F8C
	public float GetTotalSkill()
	{
		float num = 0f;
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			num += keyValuePair.Value.m_level;
		}
		return num;
	}

	// Token: 0x060004A9 RID: 1193 RVA: 0x00029DF0 File Offset: 0x00027FF0
	public float GetTotalSkillCap()
	{
		return this.m_totalSkillCap;
	}

	// Token: 0x0400052D RID: 1325
	private const int c_SaveFileDataVersion = 2;

	// Token: 0x0400052E RID: 1326
	private const float c_RandomSkillRange = 0.15f;

	// Token: 0x0400052F RID: 1327
	private const float c_RandomSkillMin = 0.4f;

	// Token: 0x04000530 RID: 1328
	public const float c_MaxSkillLevel = 100f;

	// Token: 0x04000531 RID: 1329
	public float m_DeathLowerFactor = 0.25f;

	// Token: 0x04000532 RID: 1330
	public bool m_useSkillCap;

	// Token: 0x04000533 RID: 1331
	public float m_totalSkillCap = 600f;

	// Token: 0x04000534 RID: 1332
	public List<Skills.SkillDef> m_skills = new List<Skills.SkillDef>();

	// Token: 0x04000535 RID: 1333
	private readonly Dictionary<Skills.SkillType, Skills.Skill> m_skillData = new Dictionary<Skills.SkillType, Skills.Skill>();

	// Token: 0x04000536 RID: 1334
	private Player m_player;

	// Token: 0x04000537 RID: 1335
	private static readonly Skills.SkillType[] s_allSkills = (Skills.SkillType[])Enum.GetValues(typeof(Skills.SkillType));

	// Token: 0x02000243 RID: 579
	public enum SkillType
	{
		// Token: 0x04001FCE RID: 8142
		None,
		// Token: 0x04001FCF RID: 8143
		Swords,
		// Token: 0x04001FD0 RID: 8144
		Knives,
		// Token: 0x04001FD1 RID: 8145
		Clubs,
		// Token: 0x04001FD2 RID: 8146
		Polearms,
		// Token: 0x04001FD3 RID: 8147
		Spears,
		// Token: 0x04001FD4 RID: 8148
		Blocking,
		// Token: 0x04001FD5 RID: 8149
		Axes,
		// Token: 0x04001FD6 RID: 8150
		Bows,
		// Token: 0x04001FD7 RID: 8151
		ElementalMagic,
		// Token: 0x04001FD8 RID: 8152
		BloodMagic,
		// Token: 0x04001FD9 RID: 8153
		Unarmed,
		// Token: 0x04001FDA RID: 8154
		Pickaxes,
		// Token: 0x04001FDB RID: 8155
		WoodCutting,
		// Token: 0x04001FDC RID: 8156
		Crossbows,
		// Token: 0x04001FDD RID: 8157
		Jump = 100,
		// Token: 0x04001FDE RID: 8158
		Sneak,
		// Token: 0x04001FDF RID: 8159
		Run,
		// Token: 0x04001FE0 RID: 8160
		Swim,
		// Token: 0x04001FE1 RID: 8161
		Fishing,
		// Token: 0x04001FE2 RID: 8162
		Cooking,
		// Token: 0x04001FE3 RID: 8163
		Farming,
		// Token: 0x04001FE4 RID: 8164
		Crafting,
		// Token: 0x04001FE5 RID: 8165
		Ride = 110,
		// Token: 0x04001FE6 RID: 8166
		All = 999
	}

	// Token: 0x02000244 RID: 580
	[Serializable]
	public class SkillDef
	{
		// Token: 0x04001FE7 RID: 8167
		public Skills.SkillType m_skill = Skills.SkillType.Swords;

		// Token: 0x04001FE8 RID: 8168
		public Sprite m_icon;

		// Token: 0x04001FE9 RID: 8169
		public string m_description = "";

		// Token: 0x04001FEA RID: 8170
		public float m_increseStep = 1f;
	}

	// Token: 0x02000245 RID: 581
	public class Skill
	{
		// Token: 0x06001EEC RID: 7916 RVA: 0x000E1BA7 File Offset: 0x000DFDA7
		public Skill(Skills.SkillDef info)
		{
			this.m_info = info;
		}

		// Token: 0x06001EED RID: 7917 RVA: 0x000E1BB8 File Offset: 0x000DFDB8
		public bool Raise(float factor)
		{
			if (this.m_level >= 100f)
			{
				return false;
			}
			float num = this.m_info.m_increseStep * factor * Game.m_skillGainRate;
			this.m_accumulator += num;
			float nextLevelRequirement = this.GetNextLevelRequirement();
			if (this.m_accumulator >= nextLevelRequirement)
			{
				this.m_level += 1f;
				this.m_level = Mathf.Clamp(this.m_level, 0f, 100f);
				this.m_accumulator = 0f;
				return true;
			}
			return false;
		}

		// Token: 0x06001EEE RID: 7918 RVA: 0x000E1C41 File Offset: 0x000DFE41
		private float GetNextLevelRequirement()
		{
			return Mathf.Pow(Mathf.Floor(this.m_level + 1f), 1.5f) * 0.5f + 0.5f;
		}

		// Token: 0x06001EEF RID: 7919 RVA: 0x000E1C6C File Offset: 0x000DFE6C
		public float GetLevelPercentage()
		{
			if (this.m_level >= 100f)
			{
				return 0f;
			}
			float nextLevelRequirement = this.GetNextLevelRequirement();
			return Mathf.Clamp01(this.m_accumulator / nextLevelRequirement);
		}

		// Token: 0x04001FEB RID: 8171
		public Skills.SkillDef m_info;

		// Token: 0x04001FEC RID: 8172
		public float m_level;

		// Token: 0x04001FED RID: 8173
		public float m_accumulator;
	}
}
