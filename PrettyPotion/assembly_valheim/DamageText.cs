using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

// Token: 0x02000074 RID: 116
public class DamageText : MonoBehaviour
{
	// Token: 0x17000028 RID: 40
	// (get) Token: 0x06000766 RID: 1894 RVA: 0x0003F32C File Offset: 0x0003D52C
	public static DamageText instance
	{
		get
		{
			return DamageText.m_instance;
		}
	}

	// Token: 0x06000767 RID: 1895 RVA: 0x0003F333 File Offset: 0x0003D533
	private void Awake()
	{
		DamageText.m_instance = this;
		ZRoutedRpc.instance.Register<ZPackage>("RPC_DamageText", new Action<long, ZPackage>(this.RPC_DamageText));
	}

	// Token: 0x06000768 RID: 1896 RVA: 0x0003F356 File Offset: 0x0003D556
	private void LateUpdate()
	{
		this.UpdateWorldTexts(Time.deltaTime);
	}

	// Token: 0x06000769 RID: 1897 RVA: 0x0003F364 File Offset: 0x0003D564
	private void UpdateWorldTexts(float dt)
	{
		DamageText.WorldTextInstance worldTextInstance = null;
		Camera mainCamera = Utils.GetMainCamera();
		foreach (DamageText.WorldTextInstance worldTextInstance2 in this.m_worldTexts)
		{
			worldTextInstance2.m_timer += dt;
			if (worldTextInstance2.m_timer > this.m_textDuration && worldTextInstance == null)
			{
				worldTextInstance = worldTextInstance2;
			}
			DamageText.WorldTextInstance worldTextInstance3 = worldTextInstance2;
			worldTextInstance3.m_worldPos.y = worldTextInstance3.m_worldPos.y + dt;
			float f = Mathf.Clamp01(worldTextInstance2.m_timer / this.m_textDuration);
			Color color = worldTextInstance2.m_textField.color;
			color.a = 1f - Mathf.Pow(f, 3f);
			worldTextInstance2.m_textField.color = color;
			Vector3 vector = mainCamera.WorldToScreenPointScaled(worldTextInstance2.m_worldPos);
			if (vector.x < 0f || vector.x > (float)Screen.width || vector.y < 0f || vector.y > (float)Screen.height || vector.z < 0f)
			{
				worldTextInstance2.m_gui.SetActive(false);
			}
			else
			{
				worldTextInstance2.m_gui.SetActive(true);
				worldTextInstance2.m_gui.transform.position = vector;
			}
		}
		if (worldTextInstance != null)
		{
			UnityEngine.Object.Destroy(worldTextInstance.m_gui);
			this.m_worldTexts.Remove(worldTextInstance);
		}
	}

	// Token: 0x0600076A RID: 1898 RVA: 0x0003F4E0 File Offset: 0x0003D6E0
	private void AddInworldText(DamageText.TextType type, Vector3 pos, float distance, string text, bool mySelf)
	{
		if (text == "0" && this.m_worldTexts.Count > 200)
		{
			return;
		}
		DamageText.WorldTextInstance worldTextInstance = new DamageText.WorldTextInstance();
		worldTextInstance.m_worldPos = pos + UnityEngine.Random.insideUnitSphere * 0.5f;
		worldTextInstance.m_gui = UnityEngine.Object.Instantiate<GameObject>(this.m_worldTextBase, base.transform);
		worldTextInstance.m_textField = worldTextInstance.m_gui.GetComponent<TMP_Text>();
		this.m_worldTexts.Add(worldTextInstance);
		text = Localization.instance.Localize(text);
		Color white;
		if (mySelf && type <= DamageText.TextType.Immune)
		{
			if (text == "0")
			{
				white = new Color(0.5f, 0.5f, 0.5f, 1f);
			}
			else
			{
				white = new Color(1f, 0f, 0f, 1f);
			}
		}
		else
		{
			switch (type)
			{
			case DamageText.TextType.Normal:
				white = new Color(1f, 1f, 1f, 1f);
				goto IL_1DC;
			case DamageText.TextType.Resistant:
				white = new Color(0.6f, 0.6f, 0.6f, 1f);
				goto IL_1DC;
			case DamageText.TextType.Weak:
				white = new Color(1f, 1f, 0f, 1f);
				goto IL_1DC;
			case DamageText.TextType.Immune:
				white = new Color(0.6f, 0.6f, 0.6f, 1f);
				goto IL_1DC;
			case DamageText.TextType.Heal:
				white = new Color(0.5f, 1f, 0.5f, 0.7f);
				goto IL_1DC;
			case DamageText.TextType.TooHard:
				white = new Color(0.8f, 0.7f, 0.7f, 1f);
				goto IL_1DC;
			case DamageText.TextType.Bonus:
				white = new Color(1f, 0.63f, 0.24f, 1f);
				goto IL_1DC;
			}
			white = Color.white;
		}
		IL_1DC:
		worldTextInstance.m_textField.color = white;
		if (distance > this.m_smallFontDistance)
		{
			worldTextInstance.m_textField.fontSize = (float)this.m_smallFontSize;
		}
		else
		{
			worldTextInstance.m_textField.fontSize = (float)this.m_largeFontSize;
		}
		switch (type)
		{
		case DamageText.TextType.Heal:
			text = "+" + text;
			break;
		case DamageText.TextType.TooHard:
			text = Localization.instance.Localize("$msg_toohard");
			break;
		case DamageText.TextType.Blocked:
			text = Localization.instance.Localize("$msg_blocked: " + text);
			break;
		}
		worldTextInstance.m_textField.text = text;
		worldTextInstance.m_timer = 0f;
	}

	// Token: 0x0600076B RID: 1899 RVA: 0x0003F770 File Offset: 0x0003D970
	public void ShowText(HitData.DamageModifier type, Vector3 pos, float dmg, bool player = false)
	{
		DamageText.TextType type2 = DamageText.TextType.Normal;
		switch (type)
		{
		case HitData.DamageModifier.Normal:
			type2 = DamageText.TextType.Normal;
			break;
		case HitData.DamageModifier.Resistant:
			type2 = DamageText.TextType.Resistant;
			break;
		case HitData.DamageModifier.Weak:
			type2 = DamageText.TextType.Weak;
			break;
		case HitData.DamageModifier.Immune:
			type2 = DamageText.TextType.Immune;
			break;
		case HitData.DamageModifier.VeryResistant:
			type2 = DamageText.TextType.Resistant;
			break;
		case HitData.DamageModifier.VeryWeak:
			type2 = DamageText.TextType.Weak;
			break;
		}
		this.ShowText(type2, pos, dmg, player);
	}

	// Token: 0x0600076C RID: 1900 RVA: 0x0003F7C4 File Offset: 0x0003D9C4
	public void ShowText(DamageText.TextType type, Vector3 pos, float dmg, bool player = false)
	{
		this.ShowText(type, pos, dmg.ToString("0.#", CultureInfo.InvariantCulture), player);
	}

	// Token: 0x0600076D RID: 1901 RVA: 0x0003F7E4 File Offset: 0x0003D9E4
	public void ShowText(DamageText.TextType type, Vector3 pos, string text, bool player = false)
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write((int)type);
		zpackage.Write(pos);
		zpackage.Write(text);
		zpackage.Write(player);
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_DamageText", new object[]
		{
			zpackage
		});
	}

	// Token: 0x0600076E RID: 1902 RVA: 0x0003F834 File Offset: 0x0003DA34
	private void RPC_DamageText(long sender, ZPackage pkg)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!mainCamera)
		{
			return;
		}
		if (Hud.IsUserHidden())
		{
			return;
		}
		DamageText.TextType type = (DamageText.TextType)pkg.ReadInt();
		Vector3 vector = pkg.ReadVector3();
		string text = pkg.ReadString();
		bool flag = pkg.ReadBool();
		float num = Vector3.Distance(mainCamera.transform.position, vector);
		if (num > this.m_maxTextDistance)
		{
			return;
		}
		bool mySelf = flag && sender == ZNet.GetUID();
		this.AddInworldText(type, vector, num, text, mySelf);
	}

	// Token: 0x040008A7 RID: 2215
	private static DamageText m_instance;

	// Token: 0x040008A8 RID: 2216
	public float m_textDuration = 1.5f;

	// Token: 0x040008A9 RID: 2217
	public float m_maxTextDistance = 30f;

	// Token: 0x040008AA RID: 2218
	public int m_largeFontSize = 16;

	// Token: 0x040008AB RID: 2219
	public int m_smallFontSize = 8;

	// Token: 0x040008AC RID: 2220
	public float m_smallFontDistance = 10f;

	// Token: 0x040008AD RID: 2221
	public GameObject m_worldTextBase;

	// Token: 0x040008AE RID: 2222
	private List<DamageText.WorldTextInstance> m_worldTexts = new List<DamageText.WorldTextInstance>();

	// Token: 0x02000278 RID: 632
	public enum TextType
	{
		// Token: 0x04002186 RID: 8582
		Normal,
		// Token: 0x04002187 RID: 8583
		Resistant,
		// Token: 0x04002188 RID: 8584
		Weak,
		// Token: 0x04002189 RID: 8585
		Immune,
		// Token: 0x0400218A RID: 8586
		Heal,
		// Token: 0x0400218B RID: 8587
		TooHard,
		// Token: 0x0400218C RID: 8588
		Blocked,
		// Token: 0x0400218D RID: 8589
		Bonus
	}

	// Token: 0x02000279 RID: 633
	private class WorldTextInstance
	{
		// Token: 0x0400218E RID: 8590
		public Vector3 m_worldPos;

		// Token: 0x0400218F RID: 8591
		public GameObject m_gui;

		// Token: 0x04002190 RID: 8592
		public float m_timer;

		// Token: 0x04002191 RID: 8593
		public TMP_Text m_textField;
	}
}
