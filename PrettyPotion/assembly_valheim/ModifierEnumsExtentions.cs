using System;

// Token: 0x02000120 RID: 288
public static class ModifierEnumsExtentions
{
	// Token: 0x06001269 RID: 4713 RVA: 0x000891A4 File Offset: 0x000873A4
	public static string GetDisplayString(this WorldModifiers modifiers)
	{
		switch (modifiers)
		{
		case WorldModifiers.Default:
			return "$menu_default";
		case WorldModifiers.Combat:
			return "$menu_combat";
		case WorldModifiers.DeathPenalty:
			return "$menu_deathpenalty";
		case WorldModifiers.Resources:
			return "$menu_resources";
		case WorldModifiers.Raids:
			return "$menu_events";
		case WorldModifiers.Portals:
			return "$menu_portals";
		default:
			return "$menu_unknown";
		}
	}

	// Token: 0x0600126A RID: 4714 RVA: 0x000891FC File Offset: 0x000873FC
	public static string GetDisplayString(this WorldModifierOption modifiers)
	{
		switch (modifiers)
		{
		case WorldModifierOption.Default:
			return "$menu_default";
		case WorldModifierOption.None:
			return "$menu_none";
		case WorldModifierOption.Less:
			return "$menu_less";
		case WorldModifierOption.MuchLess:
			return "$menu_muchless";
		case WorldModifierOption.More:
			return "$menu_more";
		case WorldModifierOption.MuchMore:
			return "$menu_muchmore";
		case WorldModifierOption.Casual:
			return "$menu_modifier_casual";
		case WorldModifierOption.VeryEasy:
			return "$menu_modifier_veryeasy";
		case WorldModifierOption.Easy:
			return "$menu_modifier_easy";
		case WorldModifierOption.Hard:
			return "$menu_modifier_hard";
		case WorldModifierOption.VeryHard:
			return "$menu_modifier_veryhard";
		case WorldModifierOption.Hardcore:
			return "$menu_modifier_hardcore";
		case WorldModifierOption.Most:
			return "$menu_modifier_most";
		default:
			return "$menu_unknown";
		}
	}

	// Token: 0x0600126B RID: 4715 RVA: 0x00089298 File Offset: 0x00087498
	public static string GetDisplayString(this WorldPresets preset)
	{
		switch (preset)
		{
		case WorldPresets.Default:
			return "$menu_default";
		case WorldPresets.Custom:
			return "$menu_modifier_custom";
		case WorldPresets.Normal:
			return "$menu_modifier_normal";
		case WorldPresets.Casual:
			return "$menu_modifier_casual";
		case WorldPresets.Easy:
			return "$menu_modifier_easy";
		case WorldPresets.Hard:
			return "$menu_modifier_hard";
		case WorldPresets.Hardcore:
			return "$menu_modifier_hardcore";
		case WorldPresets.Immersive:
			return "$menu_modifier_immersive";
		case WorldPresets.Hammer:
			return "$menu_modifier_hammer";
		default:
			return "$menu_unknown";
		}
	}
}
