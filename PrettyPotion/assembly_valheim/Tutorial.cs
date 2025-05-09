using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Token: 0x0200009C RID: 156
public class Tutorial : MonoBehaviour
{
	// Token: 0x17000046 RID: 70
	// (get) Token: 0x06000A5C RID: 2652 RVA: 0x0005A12E File Offset: 0x0005832E
	public static Tutorial instance
	{
		get
		{
			return Tutorial.m_instance;
		}
	}

	// Token: 0x06000A5D RID: 2653 RVA: 0x0005A135 File Offset: 0x00058335
	private void Awake()
	{
		Tutorial.m_instance = this;
		this.m_windowRoot.gameObject.SetActive(false);
	}

	// Token: 0x06000A5E RID: 2654 RVA: 0x0005A150 File Offset: 0x00058350
	private void Update()
	{
		double timeSeconds = ZNet.instance.GetTimeSeconds();
		if (ZoneSystem.instance && Player.m_localPlayer && timeSeconds > this.m_lastGlobalKeyCheck + (double)this.m_GlobalKeyCheckRateSec)
		{
			this.m_lastGlobalKeyCheck = timeSeconds;
			foreach (Tutorial.TutorialText tutorialText in this.m_texts)
			{
				if (!string.IsNullOrEmpty(tutorialText.m_globalKeyTrigger) && ZoneSystem.instance.GetGlobalKey(tutorialText.m_globalKeyTrigger))
				{
					Player.m_localPlayer.ShowTutorial(tutorialText.m_globalKeyTrigger, false);
				}
				if (!string.IsNullOrEmpty(tutorialText.m_tutorialTrigger) && Player.m_localPlayer.HaveSeenTutorial(tutorialText.m_tutorialTrigger))
				{
					Player.m_localPlayer.ShowTutorial(tutorialText.m_name, false);
				}
			}
		}
	}

	// Token: 0x06000A5F RID: 2655 RVA: 0x0005A240 File Offset: 0x00058440
	public void ShowText(string name, bool force)
	{
		Tutorial.TutorialText tutorialText = this.m_texts.Find((Tutorial.TutorialText x) => x.m_name == name);
		if (tutorialText != null)
		{
			this.SpawnRaven(tutorialText.m_name, tutorialText.m_topic, tutorialText.m_text, tutorialText.m_label, tutorialText.m_isMunin);
			return;
		}
		Debug.Log("Missing tutorial text for: " + name);
	}

	// Token: 0x06000A60 RID: 2656 RVA: 0x0005A2AF File Offset: 0x000584AF
	private void SpawnRaven(string key, string topic, string text, string label, bool munin)
	{
		if (!Raven.IsInstantiated())
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_ravenPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
		}
		Raven.AddTempText(key, topic, text, label, munin);
	}

	// Token: 0x04000BEB RID: 3051
	public List<Tutorial.TutorialText> m_texts = new List<Tutorial.TutorialText>();

	// Token: 0x04000BEC RID: 3052
	public int m_GlobalKeyCheckRateSec = 10;

	// Token: 0x04000BED RID: 3053
	public RectTransform m_windowRoot;

	// Token: 0x04000BEE RID: 3054
	public TMP_Text m_topic;

	// Token: 0x04000BEF RID: 3055
	public TMP_Text m_text;

	// Token: 0x04000BF0 RID: 3056
	public GameObject m_ravenPrefab;

	// Token: 0x04000BF1 RID: 3057
	private static Tutorial m_instance;

	// Token: 0x04000BF2 RID: 3058
	private Queue<string> m_tutQueue = new Queue<string>();

	// Token: 0x04000BF3 RID: 3059
	private double m_lastGlobalKeyCheck;

	// Token: 0x020002C6 RID: 710
	[Serializable]
	public class TutorialText
	{
		// Token: 0x040022D4 RID: 8916
		public string m_name;

		// Token: 0x040022D5 RID: 8917
		[global::Tooltip("If this global key is set, this tutorial will be shown (is saved in knowntutorials as this global key name as well)")]
		public string m_globalKeyTrigger;

		// Token: 0x040022D6 RID: 8918
		[global::Tooltip("If the specified tutorial has been seen, will trigger this tutorial. (You could chain multiple birds like this, or use together with a location discoverLabel when the exact location cant be set, like for the hildir tower)")]
		public string m_tutorialTrigger;

		// Token: 0x040022D7 RID: 8919
		public string m_topic = "";

		// Token: 0x040022D8 RID: 8920
		public string m_label = "";

		// Token: 0x040022D9 RID: 8921
		public bool m_isMunin;

		// Token: 0x040022DA RID: 8922
		[TextArea]
		public string m_text = "";
	}
}
