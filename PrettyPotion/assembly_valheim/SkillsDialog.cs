using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Token: 0x02000094 RID: 148
public class SkillsDialog : MonoBehaviour
{
	// Token: 0x06000A04 RID: 2564 RVA: 0x00057AB0 File Offset: 0x00055CB0
	private void Awake()
	{
		this.m_baseListSize = this.m_listRoot.rect.height;
	}

	// Token: 0x06000A05 RID: 2565 RVA: 0x00057AD6 File Offset: 0x00055CD6
	private IEnumerator SelectFirstEntry()
	{
		yield return null;
		yield return null;
		if (this.m_elements.Count > 0)
		{
			this.m_selectionIndex = 0;
			EventSystem.current.SetSelectedGameObject(this.m_elements[this.m_selectionIndex]);
			base.StartCoroutine(this.FocusOnCurrentLevel(this.m_elements[this.m_selectionIndex].transform as RectTransform));
			this.skillListScrollRect.verticalNormalizedPosition = 1f;
		}
		yield return null;
		yield break;
	}

	// Token: 0x06000A06 RID: 2566 RVA: 0x00057AE5 File Offset: 0x00055CE5
	private IEnumerator FocusOnCurrentLevel(RectTransform element)
	{
		yield return null;
		yield return null;
		Canvas.ForceUpdateCanvases();
		this.SnapTo(element);
		yield break;
	}

	// Token: 0x06000A07 RID: 2567 RVA: 0x00057AFC File Offset: 0x00055CFC
	private void SnapTo(RectTransform target)
	{
		Canvas.ForceUpdateCanvases();
		this.m_listRoot.anchoredPosition = this.skillListScrollRect.transform.InverseTransformPoint(this.m_listRoot.position) - this.skillListScrollRect.transform.InverseTransformPoint(target.position) - new Vector2(target.sizeDelta.x / 2f, 0f);
	}

	// Token: 0x06000A08 RID: 2568 RVA: 0x00057B7C File Offset: 0x00055D7C
	private void Update()
	{
		if (this.m_inputDelayTimer > 0f)
		{
			this.m_inputDelayTimer -= Time.unscaledDeltaTime;
			return;
		}
		if (ZInput.IsGamepadActive() && this.m_elements.Count > 0)
		{
			float joyRightStickY = ZInput.GetJoyRightStickY(true);
			float joyLeftStickY = ZInput.GetJoyLeftStickY(true);
			bool buttonDown = ZInput.GetButtonDown("JoyDPadUp");
			bool flag = joyLeftStickY < -0.1f || joyRightStickY < -0.1f;
			bool buttonDown2 = ZInput.GetButtonDown("JoyDPadDown");
			bool flag2 = joyLeftStickY > 0.1f || joyRightStickY > 0.1f;
			if ((flag || buttonDown) && this.m_selectionIndex > 0)
			{
				this.m_selectionIndex--;
			}
			if ((buttonDown2 || flag2) && this.m_selectionIndex < this.m_elements.Count - 1)
			{
				this.m_selectionIndex++;
			}
			GameObject gameObject = this.m_elements[this.m_selectionIndex];
			EventSystem.current.SetSelectedGameObject(gameObject);
			base.StartCoroutine(this.FocusOnCurrentLevel(gameObject.transform as RectTransform));
			gameObject.GetComponentInChildren<UITooltip>().OnHoverStart(gameObject);
			if (flag || flag2)
			{
				this.m_inputDelayTimer = this.m_inputDelay;
			}
		}
		if (this.m_elements.Count > 0)
		{
			RectTransform rectTransform = this.skillListScrollRect.transform as RectTransform;
			RectTransform listRoot = this.m_listRoot;
			this.scrollbar.size = rectTransform.rect.height / listRoot.rect.height;
		}
	}

	// Token: 0x06000A09 RID: 2569 RVA: 0x00057D00 File Offset: 0x00055F00
	public void Setup(Player player)
	{
		base.gameObject.SetActive(true);
		List<Skills.Skill> skillList = player.GetSkills().GetSkillList();
		int num = skillList.Count - this.m_elements.Count;
		for (int i = 0; i < num; i++)
		{
			GameObject item = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, Vector3.zero, Quaternion.identity, this.m_listRoot);
			this.m_elements.Add(item);
		}
		for (int j = 0; j < skillList.Count; j++)
		{
			Skills.Skill skill = skillList[j];
			GameObject gameObject = this.m_elements[j];
			gameObject.SetActive(true);
			RectTransform rectTransform = gameObject.transform as RectTransform;
			rectTransform.anchoredPosition = new Vector2(0f, (float)(-(float)j) * this.m_spacing);
			gameObject.GetComponentInChildren<UITooltip>().Set("", skill.m_info.m_description, this.m_tooltipAnchor, new Vector2(0f, Math.Min(255f, rectTransform.localPosition.y + 10f)));
			Utils.FindChild(gameObject.transform, "icon", Utils.IterativeSearchType.DepthFirst).GetComponent<Image>().sprite = skill.m_info.m_icon;
			Utils.FindChild(gameObject.transform, "name", Utils.IterativeSearchType.DepthFirst).GetComponent<TMP_Text>().text = Localization.instance.Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower());
			float skillLevel = player.GetSkills().GetSkillLevel(skill.m_info.m_skill);
			Utils.FindChild(gameObject.transform, "leveltext", Utils.IterativeSearchType.DepthFirst).GetComponent<TMP_Text>().text = ((int)skill.m_level).ToString();
			TMP_Text component = Utils.FindChild(gameObject.transform, "bonustext", Utils.IterativeSearchType.DepthFirst).GetComponent<TMP_Text>();
			bool flag = skillLevel != Mathf.Floor(skill.m_level);
			component.gameObject.SetActive(flag);
			if (flag)
			{
				component.text = (skillLevel - skill.m_level).ToString("+0");
			}
			Utils.FindChild(gameObject.transform, "levelbar_total", Utils.IterativeSearchType.DepthFirst).GetComponent<GuiBar>().SetValue(skillLevel / 100f);
			Utils.FindChild(gameObject.transform, "levelbar", Utils.IterativeSearchType.DepthFirst).GetComponent<GuiBar>().SetValue(skill.m_level / 100f);
			Utils.FindChild(gameObject.transform, "currentlevel", Utils.IterativeSearchType.DepthFirst).GetComponent<GuiBar>().SetValue(skill.GetLevelPercentage());
		}
		for (int k = skillList.Count; k < this.m_elements.Count; k++)
		{
			this.m_elements[k].SetActive(false);
		}
		float size = Mathf.Max(this.m_baseListSize, (float)skillList.Count * this.m_spacing);
		this.m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		this.m_totalSkillText.text = string.Concat(new string[]
		{
			"<color=orange>",
			player.GetSkills().GetTotalSkill().ToString("0"),
			"</color><color=white> / </color><color=orange>",
			player.GetSkills().GetTotalSkillCap().ToString("0"),
			"</color>"
		});
		base.StartCoroutine(this.SelectFirstEntry());
	}

	// Token: 0x06000A0A RID: 2570 RVA: 0x00058065 File Offset: 0x00056265
	public void OnClose()
	{
		base.gameObject.SetActive(false);
	}

	// Token: 0x06000A0B RID: 2571 RVA: 0x00058073 File Offset: 0x00056273
	public void SkillClicked(GameObject selectedObject)
	{
		this.m_selectionIndex = this.m_elements.IndexOf(selectedObject);
	}

	// Token: 0x04000B8F RID: 2959
	public RectTransform m_listRoot;

	// Token: 0x04000B90 RID: 2960
	[SerializeField]
	private ScrollRect skillListScrollRect;

	// Token: 0x04000B91 RID: 2961
	[SerializeField]
	private Scrollbar scrollbar;

	// Token: 0x04000B92 RID: 2962
	public RectTransform m_tooltipAnchor;

	// Token: 0x04000B93 RID: 2963
	public GameObject m_elementPrefab;

	// Token: 0x04000B94 RID: 2964
	public TMP_Text m_totalSkillText;

	// Token: 0x04000B95 RID: 2965
	public float m_spacing = 80f;

	// Token: 0x04000B96 RID: 2966
	public float m_inputDelay = 0.1f;

	// Token: 0x04000B97 RID: 2967
	private int m_selectionIndex;

	// Token: 0x04000B98 RID: 2968
	private float m_inputDelayTimer;

	// Token: 0x04000B99 RID: 2969
	private float m_baseListSize;

	// Token: 0x04000B9A RID: 2970
	private readonly List<GameObject> m_elements = new List<GameObject>();
}
