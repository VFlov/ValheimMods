using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000021 RID: 33
public class LevelEffects : MonoBehaviour
{
	// Token: 0x060002E3 RID: 739 RVA: 0x00019DE8 File Offset: 0x00017FE8
	private void Start()
	{
		this.m_character = base.GetComponentInParent<Character>();
		Character character = this.m_character;
		character.m_onLevelSet = (Action<int>)Delegate.Combine(character.m_onLevelSet, new Action<int>(this.OnLevelSet));
		this.SetupLevelVisualization(this.m_character.GetLevel());
	}

	// Token: 0x060002E4 RID: 740 RVA: 0x00019E39 File Offset: 0x00018039
	private void OnLevelSet(int level)
	{
		this.SetupLevelVisualization(level);
	}

	// Token: 0x060002E5 RID: 741 RVA: 0x00019E44 File Offset: 0x00018044
	private void SetupLevelVisualization(int level)
	{
		if (level <= 1)
		{
			return;
		}
		if (this.m_levelSetups.Count >= level - 1)
		{
			LevelEffects.LevelSetup levelSetup = this.m_levelSetups[level - 2];
			base.transform.localScale = new Vector3(levelSetup.m_scale, levelSetup.m_scale, levelSetup.m_scale);
			if (this.m_mainRender)
			{
				string key = Utils.GetPrefabName(this.m_character.gameObject) + level.ToString();
				Material material;
				if (LevelEffects.m_materials.TryGetValue(key, out material))
				{
					Material[] sharedMaterials = this.m_mainRender.sharedMaterials;
					sharedMaterials[0] = material;
					this.m_mainRender.sharedMaterials = sharedMaterials;
				}
				else
				{
					Material[] sharedMaterials2 = this.m_mainRender.sharedMaterials;
					sharedMaterials2[0] = new Material(sharedMaterials2[0]);
					sharedMaterials2[0].SetFloat("_Hue", levelSetup.m_hue);
					sharedMaterials2[0].SetFloat("_Saturation", levelSetup.m_saturation);
					sharedMaterials2[0].SetFloat("_Value", levelSetup.m_value);
					if (levelSetup.m_setEmissiveColor)
					{
						sharedMaterials2[0].SetColor("_EmissionColor", levelSetup.m_emissiveColor);
					}
					this.m_mainRender.sharedMaterials = sharedMaterials2;
					LevelEffects.m_materials[key] = sharedMaterials2[0];
				}
			}
			if (this.m_baseEnableObject)
			{
				this.m_baseEnableObject.SetActive(false);
			}
			if (levelSetup.m_enableObject)
			{
				levelSetup.m_enableObject.SetActive(true);
			}
		}
	}

	// Token: 0x060002E6 RID: 742 RVA: 0x00019FB8 File Offset: 0x000181B8
	public void GetColorChanges(out float hue, out float saturation, out float value)
	{
		int level = this.m_character.GetLevel();
		if (level > 1 && this.m_levelSetups.Count >= level - 1)
		{
			LevelEffects.LevelSetup levelSetup = this.m_levelSetups[level - 2];
			hue = levelSetup.m_hue;
			saturation = levelSetup.m_saturation;
			value = levelSetup.m_value;
			return;
		}
		hue = 0f;
		saturation = 0f;
		value = 0f;
	}

	// Token: 0x040003E0 RID: 992
	public Renderer m_mainRender;

	// Token: 0x040003E1 RID: 993
	public GameObject m_baseEnableObject;

	// Token: 0x040003E2 RID: 994
	public List<LevelEffects.LevelSetup> m_levelSetups = new List<LevelEffects.LevelSetup>();

	// Token: 0x040003E3 RID: 995
	private static Dictionary<string, Material> m_materials = new Dictionary<string, Material>();

	// Token: 0x040003E4 RID: 996
	private Character m_character;

	// Token: 0x02000237 RID: 567
	[Serializable]
	public class LevelSetup
	{
		// Token: 0x04001F8E RID: 8078
		public float m_scale = 1f;

		// Token: 0x04001F8F RID: 8079
		public float m_hue;

		// Token: 0x04001F90 RID: 8080
		public float m_saturation;

		// Token: 0x04001F91 RID: 8081
		public float m_value;

		// Token: 0x04001F92 RID: 8082
		public bool m_setEmissiveColor;

		// Token: 0x04001F93 RID: 8083
		[ColorUsage(false, true)]
		public Color m_emissiveColor = Color.white;

		// Token: 0x04001F94 RID: 8084
		public GameObject m_enableObject;
	}
}
