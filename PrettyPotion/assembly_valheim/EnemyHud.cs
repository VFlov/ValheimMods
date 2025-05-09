using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Token: 0x02000075 RID: 117
public class EnemyHud : MonoBehaviour
{
	// Token: 0x17000029 RID: 41
	// (get) Token: 0x06000770 RID: 1904 RVA: 0x0003F902 File Offset: 0x0003DB02
	public static EnemyHud instance
	{
		get
		{
			return EnemyHud.m_instance;
		}
	}

	// Token: 0x06000771 RID: 1905 RVA: 0x0003F909 File Offset: 0x0003DB09
	private void Awake()
	{
		EnemyHud.m_instance = this;
		this.m_baseHud.SetActive(false);
		this.m_baseHudBoss.SetActive(false);
		this.m_baseHudPlayer.SetActive(false);
		this.m_baseHudMount.SetActive(false);
	}

	// Token: 0x06000772 RID: 1906 RVA: 0x0003F941 File Offset: 0x0003DB41
	private void OnDestroy()
	{
		EnemyHud.m_instance = null;
	}

	// Token: 0x06000773 RID: 1907 RVA: 0x0003F94C File Offset: 0x0003DB4C
	private void LateUpdate()
	{
		this.m_hudRoot.SetActive(!Hud.IsUserHidden());
		Sadle sadle = null;
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer != null)
		{
			this.m_refPoint = localPlayer.transform.position;
			sadle = (localPlayer.GetDoodadController() as Sadle);
		}
		foreach (Character character in Character.GetAllCharacters())
		{
			if (!(character == localPlayer) && (!sadle || !(character == sadle.GetCharacter())) && this.TestShow(character, false))
			{
				bool isMount = sadle && character == sadle.GetCharacter();
				this.ShowHud(character, isMount);
			}
		}
		this.UpdateHuds(localPlayer, sadle, Time.deltaTime);
	}

	// Token: 0x06000774 RID: 1908 RVA: 0x0003FA30 File Offset: 0x0003DC30
	private bool TestShow(Character c, bool isVisible)
	{
		float num = Vector3.SqrMagnitude(c.transform.position - this.m_refPoint);
		if (c.IsBoss() && num < this.m_maxShowDistanceBoss * this.m_maxShowDistanceBoss)
		{
			if (isVisible && c.m_dontHideBossHud)
			{
				return true;
			}
			if (c.GetComponent<BaseAI>().IsAlerted())
			{
				return true;
			}
		}
		else if (num < this.m_maxShowDistance * this.m_maxShowDistance)
		{
			return !c.IsPlayer() || !c.IsCrouching();
		}
		return false;
	}

	// Token: 0x06000775 RID: 1909 RVA: 0x0003FAB4 File Offset: 0x0003DCB4
	private void ShowHud(Character c, bool isMount)
	{
		EnemyHud.HudData hudData;
		if (this.m_huds.TryGetValue(c, out hudData))
		{
			return;
		}
		GameObject original;
		if (isMount)
		{
			original = this.m_baseHudMount;
		}
		else if (c.IsPlayer())
		{
			original = this.m_baseHudPlayer;
		}
		else if (c.IsBoss())
		{
			original = this.m_baseHudBoss;
		}
		else
		{
			original = this.m_baseHud;
		}
		hudData = new EnemyHud.HudData();
		hudData.m_character = c;
		hudData.m_ai = c.GetComponent<BaseAI>();
		hudData.m_gui = UnityEngine.Object.Instantiate<GameObject>(original, this.m_hudRoot.transform);
		hudData.m_gui.SetActive(true);
		hudData.m_healthFast = hudData.m_gui.transform.Find("Health/health_fast").GetComponent<GuiBar>();
		hudData.m_healthSlow = hudData.m_gui.transform.Find("Health/health_slow").GetComponent<GuiBar>();
		Transform transform = hudData.m_gui.transform.Find("Health/health_fast_friendly");
		if (transform)
		{
			hudData.m_healthFastFriendly = transform.GetComponent<GuiBar>();
		}
		if (isMount)
		{
			hudData.m_stamina = hudData.m_gui.transform.Find("Stamina/stamina_fast").GetComponent<GuiBar>();
			hudData.m_staminaText = hudData.m_gui.transform.Find("Stamina/StaminaText").GetComponent<TextMeshProUGUI>();
			hudData.m_healthText = hudData.m_gui.transform.Find("Health/HealthText").GetComponent<TextMeshProUGUI>();
		}
		hudData.m_level2 = (hudData.m_gui.transform.Find("level_2") as RectTransform);
		hudData.m_level3 = (hudData.m_gui.transform.Find("level_3") as RectTransform);
		hudData.m_alerted = (hudData.m_gui.transform.Find("Alerted") as RectTransform);
		hudData.m_aware = (hudData.m_gui.transform.Find("Aware") as RectTransform);
		hudData.m_name = hudData.m_gui.transform.Find("Name").GetComponent<TextMeshProUGUI>();
		hudData.m_name.text = Localization.instance.Localize(c.GetHoverName());
		hudData.m_isMount = isMount;
		this.m_huds.Add(c, hudData);
	}

	// Token: 0x06000776 RID: 1910 RVA: 0x0003FCE0 File Offset: 0x0003DEE0
	private void UpdateHuds(Player player, Sadle sadle, float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!mainCamera)
		{
			return;
		}
		Character y = sadle ? sadle.GetCharacter() : null;
		Character y2 = player ? player.GetHoverCreature() : null;
		Character character = null;
		foreach (KeyValuePair<Character, EnemyHud.HudData> keyValuePair in this.m_huds)
		{
			EnemyHud.HudData value = keyValuePair.Value;
			if (!value.m_character || !this.TestShow(value.m_character, true) || value.m_character == y)
			{
				if (character == null)
				{
					character = value.m_character;
					UnityEngine.Object.Destroy(value.m_gui);
				}
			}
			else
			{
				if (value.m_character == y2)
				{
					value.m_hoverTimer = 0f;
				}
				value.m_hoverTimer += dt;
				float healthPercentage = value.m_character.GetHealthPercentage();
				if (value.m_character.IsPlayer() || value.m_character.IsBoss() || value.m_isMount || value.m_hoverTimer < this.m_hoverShowDuration)
				{
					value.m_gui.SetActive(true);
					int level = value.m_character.GetLevel();
					if (value.m_level2)
					{
						value.m_level2.gameObject.SetActive(level == 2);
					}
					if (value.m_level3)
					{
						value.m_level3.gameObject.SetActive(level == 3);
					}
					value.m_name.text = Localization.instance.Localize(value.m_character.GetHoverName());
					if (!value.m_character.IsBoss() && !value.m_character.IsPlayer())
					{
						bool flag = value.m_character.GetBaseAI().HaveTarget();
						bool flag2 = value.m_character.GetBaseAI().IsAlerted();
						value.m_alerted.gameObject.SetActive(flag2);
						value.m_aware.gameObject.SetActive(!flag2 && flag);
					}
				}
				else
				{
					value.m_gui.SetActive(false);
				}
				value.m_healthSlow.SetValue(healthPercentage);
				if (value.m_healthFastFriendly)
				{
					bool flag3 = !player || BaseAI.IsEnemy(player, value.m_character);
					value.m_healthFast.gameObject.SetActive(flag3);
					value.m_healthFastFriendly.gameObject.SetActive(!flag3);
					value.m_healthFast.SetValue(healthPercentage);
					value.m_healthFastFriendly.SetValue(healthPercentage);
				}
				else
				{
					value.m_healthFast.SetValue(healthPercentage);
				}
				if (value.m_isMount)
				{
					float stamina = sadle.GetStamina();
					float maxStamina = sadle.GetMaxStamina();
					value.m_stamina.SetValue(stamina / maxStamina);
					value.m_healthText.text = Mathf.CeilToInt(value.m_character.GetHealth()).ToString();
					value.m_staminaText.text = Mathf.CeilToInt(stamina).ToString();
				}
				if (!value.m_character.IsBoss() && value.m_gui.activeSelf)
				{
					Vector3 worldPos = Vector3.zero;
					if (value.m_character.IsPlayer())
					{
						worldPos = value.m_character.GetHeadPoint() + Vector3.up * 0.3f;
					}
					else if (value.m_isMount)
					{
						worldPos = player.transform.position - player.transform.up * 0.5f;
					}
					else
					{
						worldPos = value.m_character.GetTopPoint();
					}
					Vector3 vector = mainCamera.WorldToScreenPointScaled(worldPos);
					if (vector.x < 0f || vector.x > (float)Screen.width || vector.y < 0f || vector.y > (float)Screen.height || vector.z > 0f)
					{
						value.m_gui.transform.position = vector;
						value.m_gui.SetActive(true);
					}
					else
					{
						value.m_gui.SetActive(false);
					}
				}
			}
		}
		if (character != null)
		{
			this.m_huds.Remove(character);
		}
	}

	// Token: 0x06000777 RID: 1911 RVA: 0x0004016C File Offset: 0x0003E36C
	public bool ShowingBossHud()
	{
		foreach (KeyValuePair<Character, EnemyHud.HudData> keyValuePair in this.m_huds)
		{
			if (keyValuePair.Value.m_character && keyValuePair.Value.m_character.IsBoss())
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000778 RID: 1912 RVA: 0x000401E8 File Offset: 0x0003E3E8
	public Character GetActiveBoss()
	{
		foreach (KeyValuePair<Character, EnemyHud.HudData> keyValuePair in this.m_huds)
		{
			if (keyValuePair.Value.m_character && keyValuePair.Value.m_character.IsBoss())
			{
				return keyValuePair.Value.m_character;
			}
		}
		return null;
	}

	// Token: 0x06000779 RID: 1913 RVA: 0x0004026C File Offset: 0x0003E46C
	public void RemoveCharacterHud(Character character)
	{
		if (!this.m_huds.ContainsKey(character))
		{
			return;
		}
		if (this.m_huds[character].m_gui != null)
		{
			UnityEngine.Object.Destroy(this.m_huds[character].m_gui);
		}
		this.m_huds.Remove(character);
	}

	// Token: 0x040008AF RID: 2223
	private static EnemyHud m_instance;

	// Token: 0x040008B0 RID: 2224
	public GameObject m_hudRoot;

	// Token: 0x040008B1 RID: 2225
	public GameObject m_baseHud;

	// Token: 0x040008B2 RID: 2226
	public GameObject m_baseHudBoss;

	// Token: 0x040008B3 RID: 2227
	public GameObject m_baseHudPlayer;

	// Token: 0x040008B4 RID: 2228
	public GameObject m_baseHudMount;

	// Token: 0x040008B5 RID: 2229
	public float m_maxShowDistance = 10f;

	// Token: 0x040008B6 RID: 2230
	public float m_maxShowDistanceBoss = 100f;

	// Token: 0x040008B7 RID: 2231
	public float m_hoverShowDuration = 60f;

	// Token: 0x040008B8 RID: 2232
	private Vector3 m_refPoint = Vector3.zero;

	// Token: 0x040008B9 RID: 2233
	private Dictionary<Character, EnemyHud.HudData> m_huds = new Dictionary<Character, EnemyHud.HudData>();

	// Token: 0x0200027A RID: 634
	private class HudData
	{
		// Token: 0x04002192 RID: 8594
		public Character m_character;

		// Token: 0x04002193 RID: 8595
		public BaseAI m_ai;

		// Token: 0x04002194 RID: 8596
		public GameObject m_gui;

		// Token: 0x04002195 RID: 8597
		public RectTransform m_level2;

		// Token: 0x04002196 RID: 8598
		public RectTransform m_level3;

		// Token: 0x04002197 RID: 8599
		public RectTransform m_alerted;

		// Token: 0x04002198 RID: 8600
		public RectTransform m_aware;

		// Token: 0x04002199 RID: 8601
		public GuiBar m_healthFast;

		// Token: 0x0400219A RID: 8602
		public GuiBar m_healthFastFriendly;

		// Token: 0x0400219B RID: 8603
		public GuiBar m_healthSlow;

		// Token: 0x0400219C RID: 8604
		public TextMeshProUGUI m_healthText;

		// Token: 0x0400219D RID: 8605
		public GuiBar m_stamina;

		// Token: 0x0400219E RID: 8606
		public TextMeshProUGUI m_staminaText;

		// Token: 0x0400219F RID: 8607
		public TextMeshProUGUI m_name;

		// Token: 0x040021A0 RID: 8608
		public float m_hoverTimer = 99999f;

		// Token: 0x040021A1 RID: 8609
		public bool m_isMount;
	}
}
