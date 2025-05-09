using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Token: 0x02000084 RID: 132
public class ManageSavesMenuElement : MonoBehaviour
{
	// Token: 0x17000035 RID: 53
	// (get) Token: 0x0600089F RID: 2207 RVA: 0x0004BB6E File Offset: 0x00049D6E
	public RectTransform rectTransform
	{
		get
		{
			return base.transform as RectTransform;
		}
	}

	// Token: 0x17000036 RID: 54
	// (get) Token: 0x060008A0 RID: 2208 RVA: 0x0004BB7B File Offset: 0x00049D7B
	private RectTransform arrowRectTransform
	{
		get
		{
			return this.arrow.transform as RectTransform;
		}
	}

	// Token: 0x14000003 RID: 3
	// (add) Token: 0x060008A1 RID: 2209 RVA: 0x0004BB90 File Offset: 0x00049D90
	// (remove) Token: 0x060008A2 RID: 2210 RVA: 0x0004BBC8 File Offset: 0x00049DC8
	public event ManageSavesMenuElement.HeightChangedHandler HeightChanged;

	// Token: 0x14000004 RID: 4
	// (add) Token: 0x060008A3 RID: 2211 RVA: 0x0004BC00 File Offset: 0x00049E00
	// (remove) Token: 0x060008A4 RID: 2212 RVA: 0x0004BC38 File Offset: 0x00049E38
	public event ManageSavesMenuElement.ElementClickedHandler ElementClicked;

	// Token: 0x14000005 RID: 5
	// (add) Token: 0x060008A5 RID: 2213 RVA: 0x0004BC70 File Offset: 0x00049E70
	// (remove) Token: 0x060008A6 RID: 2214 RVA: 0x0004BCA8 File Offset: 0x00049EA8
	public event ManageSavesMenuElement.ElementExpandedChangedHandler ElementExpandedChanged;

	// Token: 0x17000037 RID: 55
	// (get) Token: 0x060008A7 RID: 2215 RVA: 0x0004BCDD File Offset: 0x00049EDD
	// (set) Token: 0x060008A8 RID: 2216 RVA: 0x0004BCE5 File Offset: 0x00049EE5
	public bool IsExpanded { get; private set; }

	// Token: 0x17000038 RID: 56
	// (get) Token: 0x060008A9 RID: 2217 RVA: 0x0004BCEE File Offset: 0x00049EEE
	public int BackupCount
	{
		get
		{
			return this.backupElements.Count;
		}
	}

	// Token: 0x17000039 RID: 57
	// (get) Token: 0x060008AA RID: 2218 RVA: 0x0004BCFB File Offset: 0x00049EFB
	// (set) Token: 0x060008AB RID: 2219 RVA: 0x0004BD03 File Offset: 0x00049F03
	public SaveWithBackups Save { get; private set; }

	// Token: 0x060008AC RID: 2220 RVA: 0x0004BD0C File Offset: 0x00049F0C
	public void SetUp(SaveWithBackups save)
	{
		this.UpdatePrimaryElement();
		for (int i = 0; i < this.Save.BackupFiles.Length; i++)
		{
			ManageSavesMenuElement.BackupElement item = this.CreateBackupElement(this.Save.BackupFiles[i], i);
			this.backupElements.Add(item);
		}
		this.UpdateElementPositions();
	}

	// Token: 0x060008AD RID: 2221 RVA: 0x0004BD5F File Offset: 0x00049F5F
	public IEnumerator SetUpEnumerator(SaveWithBackups save)
	{
		this.Save = save;
		this.UpdatePrimaryElement();
		yield return null;
		int num;
		for (int i = 0; i < this.Save.BackupFiles.Length; i = num + 1)
		{
			ManageSavesMenuElement.BackupElement item = this.CreateBackupElement(this.Save.BackupFiles[i], i);
			this.backupElements.Add(item);
			yield return null;
			num = i;
		}
		IEnumerator updateElementPositions = this.UpdateElementPositionsEnumerator();
		while (updateElementPositions.MoveNext())
		{
			yield return null;
		}
		yield break;
	}

	// Token: 0x060008AE RID: 2222 RVA: 0x0004BD78 File Offset: 0x00049F78
	public void UpdateElement(SaveWithBackups save)
	{
		this.Save = save;
		this.UpdatePrimaryElement();
		List<ManageSavesMenuElement.BackupElement> list = new List<ManageSavesMenuElement.BackupElement>();
		Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>> dictionary = new Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>>();
		for (int i = 0; i < this.backupElements.Count; i++)
		{
			if (!dictionary.ContainsKey(this.backupElements[i].File.FileName))
			{
				dictionary.Add(this.backupElements[i].File.FileName, new Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>());
			}
			dictionary[this.backupElements[i].File.FileName].Add(this.backupElements[i].File.m_source, this.backupElements[i]);
		}
		for (int j = 0; j < this.Save.BackupFiles.Length; j++)
		{
			SaveFile saveFile = this.Save.BackupFiles[j];
			if (dictionary.ContainsKey(saveFile.FileName) && dictionary[saveFile.FileName].ContainsKey(saveFile.m_source))
			{
				int currentIndex = j;
				dictionary[saveFile.FileName][saveFile.m_source].UpdateElement(saveFile, delegate
				{
					this.OnBackupElementClicked(currentIndex);
				});
				list.Add(dictionary[saveFile.FileName][saveFile.m_source]);
				dictionary[saveFile.FileName].Remove(saveFile.m_source);
				if (dictionary.Count <= 0)
				{
					dictionary.Remove(saveFile.FileName);
				}
			}
			else
			{
				ManageSavesMenuElement.BackupElement item = this.CreateBackupElement(saveFile, j);
				list.Add(item);
			}
		}
		foreach (KeyValuePair<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>> keyValuePair in dictionary)
		{
			foreach (KeyValuePair<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement> keyValuePair2 in keyValuePair.Value)
			{
				UnityEngine.Object.Destroy(keyValuePair2.Value.GuiInstance);
			}
		}
		this.backupElements = list;
		float num = this.UpdateElementPositions();
		this.rectTransform.sizeDelta = new Vector2(this.rectTransform.sizeDelta.x, this.IsExpanded ? num : this.elementHeight);
	}

	// Token: 0x060008AF RID: 2223 RVA: 0x0004C014 File Offset: 0x0004A214
	public IEnumerator UpdateElementEnumerator(SaveWithBackups save)
	{
		this.Save = save;
		this.UpdatePrimaryElement();
		List<ManageSavesMenuElement.BackupElement> newBackupElementsList = new List<ManageSavesMenuElement.BackupElement>();
		Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>> backupNameToElementMap = new Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>>();
		int num;
		for (int i = 0; i < this.backupElements.Count; i = num + 1)
		{
			if (!backupNameToElementMap.ContainsKey(this.backupElements[i].File.FileName))
			{
				backupNameToElementMap.Add(this.backupElements[i].File.FileName, new Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>());
			}
			backupNameToElementMap[this.backupElements[i].File.FileName].Add(this.backupElements[i].File.m_source, this.backupElements[i]);
			yield return null;
			num = i;
		}
		for (int i = 0; i < this.Save.BackupFiles.Length; i = num + 1)
		{
			SaveFile saveFile = this.Save.BackupFiles[i];
			if (backupNameToElementMap.ContainsKey(saveFile.FileName) && backupNameToElementMap[saveFile.FileName].ContainsKey(saveFile.m_source))
			{
				int currentIndex = i;
				backupNameToElementMap[saveFile.FileName][saveFile.m_source].UpdateElement(saveFile, delegate
				{
					this.OnBackupElementClicked(currentIndex);
				});
				newBackupElementsList.Add(backupNameToElementMap[saveFile.FileName][saveFile.m_source]);
				backupNameToElementMap[saveFile.FileName].Remove(saveFile.m_source);
				if (backupNameToElementMap.Count <= 0)
				{
					backupNameToElementMap.Remove(saveFile.FileName);
				}
			}
			else
			{
				ManageSavesMenuElement.BackupElement item = this.CreateBackupElement(saveFile, i);
				newBackupElementsList.Add(item);
			}
			yield return null;
			num = i;
		}
		foreach (KeyValuePair<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>> keyValuePair in backupNameToElementMap)
		{
			foreach (KeyValuePair<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement> keyValuePair2 in keyValuePair.Value)
			{
				UnityEngine.Object.Destroy(keyValuePair2.Value.GuiInstance);
				yield return null;
			}
			Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>.Enumerator enumerator2 = default(Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>.Enumerator);
		}
		Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>>.Enumerator enumerator = default(Dictionary<string, Dictionary<FileHelpers.FileSource, ManageSavesMenuElement.BackupElement>>.Enumerator);
		this.backupElements = newBackupElementsList;
		float num2 = this.UpdateElementPositions();
		this.rectTransform.sizeDelta = new Vector2(this.rectTransform.sizeDelta.x, this.IsExpanded ? num2 : this.elementHeight);
		yield break;
		yield break;
	}

	// Token: 0x060008B0 RID: 2224 RVA: 0x0004C02C File Offset: 0x0004A22C
	private ManageSavesMenuElement.BackupElement CreateBackupElement(SaveFile backup, int index)
	{
		return new ManageSavesMenuElement.BackupElement(UnityEngine.Object.Instantiate<GameObject>(this.backupElement.gameObject, this.rectTransform), backup, delegate()
		{
			this.OnBackupElementClicked(index);
		});
	}

	// Token: 0x060008B1 RID: 2225 RVA: 0x0004C078 File Offset: 0x0004A278
	private float UpdateElementPositions()
	{
		float num = this.elementHeight;
		for (int i = 0; i < this.backupElements.Count; i++)
		{
			this.backupElements[i].rectTransform.anchoredPosition = new Vector2(this.backupElements[i].rectTransform.anchoredPosition.x, -num);
			num += this.backupElements[i].rectTransform.sizeDelta.y;
		}
		return num;
	}

	// Token: 0x060008B2 RID: 2226 RVA: 0x0004C0F9 File Offset: 0x0004A2F9
	private IEnumerator UpdateElementPositionsEnumerator()
	{
		float pos = this.elementHeight;
		int num;
		for (int i = 0; i < this.backupElements.Count; i = num + 1)
		{
			this.backupElements[i].rectTransform.anchoredPosition = new Vector2(this.backupElements[i].rectTransform.anchoredPosition.x, -pos);
			pos += this.backupElements[i].rectTransform.sizeDelta.y;
			yield return null;
			num = i;
		}
		yield break;
	}

	// Token: 0x060008B3 RID: 2227 RVA: 0x0004C108 File Offset: 0x0004A308
	private void UpdatePrimaryElement()
	{
		this.arrow.gameObject.SetActive(this.Save.BackupFiles.Length != 0);
		string text = this.Save.m_name;
		if (!this.Save.IsDeleted)
		{
			text = this.Save.PrimaryFile.FileName;
			if (SaveSystem.IsCorrupt(this.Save.PrimaryFile))
			{
				text += " [CORRUPT]";
			}
			if (SaveSystem.IsWorldWithMissingMetaFile(this.Save.PrimaryFile))
			{
				text += " [MISSING META]";
			}
		}
		this.nameText.text = text;
		this.sizeText.text = FileHelpers.BytesAsNumberString(this.Save.IsDeleted ? 0UL : this.Save.PrimaryFile.Size, 1U) + "/" + FileHelpers.BytesAsNumberString(this.Save.SizeWithBackups, 1U);
		this.backupCountText.text = Localization.instance.Localize("$menu_backupcount", new string[]
		{
			this.Save.BackupFiles.Length.ToString()
		});
		this.dateText.text = (this.Save.IsDeleted ? Localization.instance.Localize("$menu_deleted") : (this.Save.PrimaryFile.LastModified.ToShortDateString() + " " + this.Save.PrimaryFile.LastModified.ToShortTimeString()));
		Transform transform = this.sourceParent.Find("source_cloud");
		if (transform != null)
		{
			transform.gameObject.SetActive(!this.Save.IsDeleted && this.Save.PrimaryFile.m_source == FileHelpers.FileSource.Cloud);
		}
		Transform transform2 = this.sourceParent.Find("source_local");
		if (transform2 != null)
		{
			transform2.gameObject.SetActive(!this.Save.IsDeleted && this.Save.PrimaryFile.m_source == FileHelpers.FileSource.Local);
		}
		Transform transform3 = this.sourceParent.Find("source_legacy");
		if (transform3 != null)
		{
			transform3.gameObject.SetActive(!this.Save.IsDeleted && this.Save.PrimaryFile.m_source == FileHelpers.FileSource.Legacy);
		}
		if (this.IsExpanded && this.Save.BackupFiles.Length == 0)
		{
			this.SetExpanded(false, false);
		}
	}

	// Token: 0x060008B4 RID: 2228 RVA: 0x0004C378 File Offset: 0x0004A578
	private void OnDestroy()
	{
		foreach (ManageSavesMenuElement.BackupElement backupElement in this.backupElements)
		{
			UnityEngine.Object.Destroy(backupElement.GuiInstance);
		}
		this.backupElements.Clear();
	}

	// Token: 0x060008B5 RID: 2229 RVA: 0x0004C3D8 File Offset: 0x0004A5D8
	private void Start()
	{
		this.elementHeight = this.rectTransform.sizeDelta.y;
	}

	// Token: 0x060008B6 RID: 2230 RVA: 0x0004C3F0 File Offset: 0x0004A5F0
	private void OnEnable()
	{
		this.primaryElement.onClick.AddListener(new UnityAction(this.OnElementClicked));
		this.arrow.onClick.AddListener(new UnityAction(this.OnArrowClicked));
	}

	// Token: 0x060008B7 RID: 2231 RVA: 0x0004C42A File Offset: 0x0004A62A
	private void OnDisable()
	{
		this.primaryElement.onClick.RemoveListener(new UnityAction(this.OnElementClicked));
		this.arrow.onClick.RemoveListener(new UnityAction(this.OnArrowClicked));
	}

	// Token: 0x060008B8 RID: 2232 RVA: 0x0004C464 File Offset: 0x0004A664
	private void OnElementClicked()
	{
		ManageSavesMenuElement.ElementClickedHandler elementClicked = this.ElementClicked;
		if (elementClicked == null)
		{
			return;
		}
		elementClicked(this, -1);
	}

	// Token: 0x060008B9 RID: 2233 RVA: 0x0004C478 File Offset: 0x0004A678
	private void OnBackupElementClicked(int index)
	{
		ManageSavesMenuElement.ElementClickedHandler elementClicked = this.ElementClicked;
		if (elementClicked == null)
		{
			return;
		}
		elementClicked(this, index);
	}

	// Token: 0x060008BA RID: 2234 RVA: 0x0004C48C File Offset: 0x0004A68C
	private void OnArrowClicked()
	{
		this.SetExpanded(!this.IsExpanded, true);
	}

	// Token: 0x060008BB RID: 2235 RVA: 0x0004C4A0 File Offset: 0x0004A6A0
	public void SetExpanded(bool value, bool animated = true)
	{
		if (this.IsExpanded == value)
		{
			return;
		}
		this.IsExpanded = value;
		ManageSavesMenuElement.ElementExpandedChangedHandler elementExpandedChanged = this.ElementExpandedChanged;
		if (elementExpandedChanged != null)
		{
			elementExpandedChanged(this, this.IsExpanded);
		}
		if (this.arrowAnimationCoroutine != null)
		{
			base.StopCoroutine(this.arrowAnimationCoroutine);
		}
		if (this.listAnimationCoroutine != null)
		{
			base.StopCoroutine(this.listAnimationCoroutine);
		}
		if (animated)
		{
			this.arrowAnimationCoroutine = base.StartCoroutine(this.AnimateArrow());
			this.listAnimationCoroutine = base.StartCoroutine(this.AnimateList());
			return;
		}
		float z = (float)(this.IsExpanded ? 0 : 90);
		this.arrowRectTransform.rotation = Quaternion.Euler(0f, 0f, z);
		float y = this.IsExpanded ? (this.elementHeight * (float)(this.backupElements.Count + 1)) : this.elementHeight;
		this.rectTransform.sizeDelta = new Vector2(this.rectTransform.sizeDelta.x, y);
		ManageSavesMenuElement.HeightChangedHandler heightChanged = this.HeightChanged;
		if (heightChanged == null)
		{
			return;
		}
		heightChanged();
	}

	// Token: 0x060008BC RID: 2236 RVA: 0x0004C5A8 File Offset: 0x0004A7A8
	public void Select(ref int backupIndex)
	{
		if (backupIndex < 0 || this.BackupCount <= 0)
		{
			this.selectedBackground.gameObject.SetActive(true);
			backupIndex = -1;
			return;
		}
		backupIndex = Mathf.Clamp(backupIndex, 0, this.BackupCount - 1);
		this.backupElements[backupIndex].rectTransform.Find("selected").gameObject.SetActive(true);
	}

	// Token: 0x060008BD RID: 2237 RVA: 0x0004C614 File Offset: 0x0004A814
	public void Deselect(int backupIndex = -1)
	{
		if (backupIndex < 0)
		{
			this.selectedBackground.gameObject.SetActive(false);
			return;
		}
		if (backupIndex > this.backupElements.Count - 1)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Failed to deselect backup: Index ",
				backupIndex.ToString(),
				" was outside of the valid range -1-",
				(this.backupElements.Count - 1).ToString(),
				". Ignoring."
			}));
			return;
		}
		this.backupElements[backupIndex].rectTransform.Find("selected").gameObject.SetActive(false);
	}

	// Token: 0x060008BE RID: 2238 RVA: 0x0004C6B8 File Offset: 0x0004A8B8
	public RectTransform GetTransform(int backupIndex = -1)
	{
		if (backupIndex < 0)
		{
			return this.primaryElement.transform as RectTransform;
		}
		return this.backupElements[backupIndex].rectTransform;
	}

	// Token: 0x060008BF RID: 2239 RVA: 0x0004C6E0 File Offset: 0x0004A8E0
	private IEnumerator AnimateArrow()
	{
		float currentRotation = this.arrowRectTransform.rotation.eulerAngles.z;
		float targetRotation = (float)(this.IsExpanded ? 0 : 90);
		float sign = Mathf.Sign(targetRotation - currentRotation);
		for (;;)
		{
			currentRotation += sign * 90f * 10f * Time.deltaTime;
			if (currentRotation * sign > targetRotation * sign)
			{
				currentRotation = targetRotation;
			}
			this.arrowRectTransform.rotation = Quaternion.Euler(0f, 0f, currentRotation);
			if (currentRotation == targetRotation)
			{
				break;
			}
			yield return null;
		}
		this.arrowAnimationCoroutine = null;
		yield break;
	}

	// Token: 0x060008C0 RID: 2240 RVA: 0x0004C6EF File Offset: 0x0004A8EF
	private IEnumerator AnimateList()
	{
		float currentSize = this.rectTransform.sizeDelta.y;
		float targetSize = this.IsExpanded ? (this.elementHeight * (float)(this.backupElements.Count + 1)) : this.elementHeight;
		float sign = Mathf.Sign(targetSize - currentSize);
		float velocity = 0f;
		for (;;)
		{
			currentSize = Mathf.SmoothDamp(currentSize, targetSize, ref velocity, 0.06f);
			if (currentSize * sign + 0.1f > targetSize * sign)
			{
				currentSize = targetSize;
			}
			this.rectTransform.sizeDelta = new Vector2(this.rectTransform.sizeDelta.x, currentSize);
			ManageSavesMenuElement.HeightChangedHandler heightChanged = this.HeightChanged;
			if (heightChanged != null)
			{
				heightChanged();
			}
			if (currentSize == targetSize)
			{
				break;
			}
			yield return null;
		}
		this.listAnimationCoroutine = null;
		yield break;
	}

	// Token: 0x04000A50 RID: 2640
	[SerializeField]
	private Button primaryElement;

	// Token: 0x04000A51 RID: 2641
	[SerializeField]
	private Button backupElement;

	// Token: 0x04000A52 RID: 2642
	[SerializeField]
	private GameObject selectedBackground;

	// Token: 0x04000A53 RID: 2643
	[SerializeField]
	private Button arrow;

	// Token: 0x04000A54 RID: 2644
	[SerializeField]
	private TMP_Text nameText;

	// Token: 0x04000A55 RID: 2645
	[SerializeField]
	private TMP_Text sizeText;

	// Token: 0x04000A56 RID: 2646
	[SerializeField]
	private TMP_Text backupCountText;

	// Token: 0x04000A57 RID: 2647
	[SerializeField]
	private TMP_Text dateText;

	// Token: 0x04000A58 RID: 2648
	[SerializeField]
	private RectTransform sourceParent;

	// Token: 0x04000A5C RID: 2652
	private float elementHeight = 32f;

	// Token: 0x04000A5D RID: 2653
	private List<ManageSavesMenuElement.BackupElement> backupElements = new List<ManageSavesMenuElement.BackupElement>();

	// Token: 0x04000A60 RID: 2656
	private Coroutine arrowAnimationCoroutine;

	// Token: 0x04000A61 RID: 2657
	private Coroutine listAnimationCoroutine;

	// Token: 0x02000296 RID: 662
	// (Invoke) Token: 0x0600205F RID: 8287
	public delegate void BackupElementClickedHandler();

	// Token: 0x02000297 RID: 663
	private class BackupElement
	{
		// Token: 0x06002062 RID: 8290 RVA: 0x000E8589 File Offset: 0x000E6789
		public BackupElement(GameObject guiInstance, SaveFile backup, ManageSavesMenuElement.BackupElementClickedHandler clickedCallback)
		{
			this.GuiInstance = guiInstance;
			this.GuiInstance.SetActive(true);
			this.Button = this.GuiInstance.GetComponent<Button>();
			this.UpdateElement(backup, clickedCallback);
		}

		// Token: 0x06002063 RID: 8291 RVA: 0x000E85C0 File Offset: 0x000E67C0
		public void UpdateElement(SaveFile backup, ManageSavesMenuElement.BackupElementClickedHandler clickedCallback)
		{
			this.File = backup;
			this.Button.onClick.RemoveAllListeners();
			this.Button.onClick.AddListener(delegate()
			{
				ManageSavesMenuElement.BackupElementClickedHandler clickedCallback2 = clickedCallback;
				if (clickedCallback2 == null)
				{
					return;
				}
				clickedCallback2();
			});
			string text = backup.FileName;
			if (SaveSystem.IsCorrupt(backup))
			{
				text += " [CORRUPT]";
			}
			if (SaveSystem.IsWorldWithMissingMetaFile(backup))
			{
				text += " [MISSING META FILE]";
			}
			this.rectTransform.Find("name").GetComponent<TMP_Text>().text = text;
			this.rectTransform.Find("size").GetComponent<TMP_Text>().text = FileHelpers.BytesAsNumberString(backup.Size, 1U);
			this.rectTransform.Find("date").GetComponent<TMP_Text>().text = backup.LastModified.ToShortDateString() + " " + backup.LastModified.ToShortTimeString();
			Transform transform = this.rectTransform.Find("source");
			Transform transform2 = transform.Find("source_cloud");
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(backup.m_source == FileHelpers.FileSource.Cloud);
			}
			Transform transform3 = transform.Find("source_local");
			if (transform3 != null)
			{
				transform3.gameObject.SetActive(backup.m_source == FileHelpers.FileSource.Local);
			}
			Transform transform4 = transform.Find("source_legacy");
			if (transform4 == null)
			{
				return;
			}
			transform4.gameObject.SetActive(backup.m_source == FileHelpers.FileSource.Legacy);
		}

		// Token: 0x1700018D RID: 397
		// (get) Token: 0x06002064 RID: 8292 RVA: 0x000E8737 File Offset: 0x000E6937
		// (set) Token: 0x06002065 RID: 8293 RVA: 0x000E873F File Offset: 0x000E693F
		public SaveFile File { get; private set; }

		// Token: 0x1700018E RID: 398
		// (get) Token: 0x06002066 RID: 8294 RVA: 0x000E8748 File Offset: 0x000E6948
		// (set) Token: 0x06002067 RID: 8295 RVA: 0x000E8750 File Offset: 0x000E6950
		public GameObject GuiInstance { get; private set; }

		// Token: 0x1700018F RID: 399
		// (get) Token: 0x06002068 RID: 8296 RVA: 0x000E8759 File Offset: 0x000E6959
		// (set) Token: 0x06002069 RID: 8297 RVA: 0x000E8761 File Offset: 0x000E6961
		public Button Button { get; private set; }

		// Token: 0x17000190 RID: 400
		// (get) Token: 0x0600206A RID: 8298 RVA: 0x000E876A File Offset: 0x000E696A
		public RectTransform rectTransform
		{
			get
			{
				return this.GuiInstance.transform as RectTransform;
			}
		}
	}

	// Token: 0x02000298 RID: 664
	// (Invoke) Token: 0x0600206C RID: 8300
	public delegate void HeightChangedHandler();

	// Token: 0x02000299 RID: 665
	// (Invoke) Token: 0x06002070 RID: 8304
	public delegate void ElementClickedHandler(ManageSavesMenuElement element, int backupElementIndex);

	// Token: 0x0200029A RID: 666
	// (Invoke) Token: 0x06002074 RID: 8308
	public delegate void ElementExpandedChangedHandler(ManageSavesMenuElement element, bool isExpanded);
}
