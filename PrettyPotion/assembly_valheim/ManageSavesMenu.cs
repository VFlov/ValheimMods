using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Token: 0x02000083 RID: 131
public class ManageSavesMenu : MonoBehaviour
{
	// Token: 0x0600086D RID: 2157 RVA: 0x0004A3C0 File Offset: 0x000485C0
	private void Update()
	{
		bool flag = false;
		if (!this.blockerInfo.IsBlocked())
		{
			bool flag2 = true;
			if (ZInput.GetKeyDown(KeyCode.LeftArrow, true) && this.IsSelectedExpanded())
			{
				this.CollapseSelected();
				flag = true;
				flag2 = false;
			}
			if (ZInput.GetKeyDown(KeyCode.RightArrow, true) && !this.IsSelectedExpanded())
			{
				this.ExpandSelected();
				flag = true;
			}
			if (flag2)
			{
				if (ZInput.GetKeyDown(KeyCode.DownArrow, true))
				{
					this.SelectRelative(1);
					flag = true;
				}
				if (ZInput.GetKeyDown(KeyCode.UpArrow, true))
				{
					this.SelectRelative(-1);
					flag = true;
				}
			}
			if (ZInput.IsGamepadActive())
			{
				if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
				{
					this.SelectRelative(1);
					flag = true;
				}
				if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
				{
					this.SelectRelative(-1);
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.UpdateButtons();
			this.CenterSelected();
			return;
		}
		this.UpdateButtonsInteractable();
	}

	// Token: 0x0600086E RID: 2158 RVA: 0x0004A4AB File Offset: 0x000486AB
	private void LateUpdate()
	{
		if (this.elementHeightChanged)
		{
			this.elementHeightChanged = false;
			this.UpdateElementPositions();
		}
	}

	// Token: 0x0600086F RID: 2159 RVA: 0x0004A4C4 File Offset: 0x000486C4
	private void UpdateButtons()
	{
		this.moveButton.gameObject.SetActive(FileHelpers.CloudStorageEnabled && FileHelpers.LocalStorageSupported);
		if (this.selectedSaveIndex < 0)
		{
			this.actionButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize("$menu_expand");
		}
		else
		{
			if (this.selectedBackupIndex < 0)
			{
				if (this.listElements[this.selectedSaveIndex].BackupCount > 0)
				{
					this.actionButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize(this.listElements[this.selectedSaveIndex].IsExpanded ? "$menu_collapse" : "$menu_expand");
				}
			}
			else
			{
				this.actionButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize("$menu_restorebackup");
			}
			if (this.selectedBackupIndex < 0)
			{
				if (!this.currentList[this.selectedSaveIndex].IsDeleted)
				{
					this.moveButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize((this.currentList[this.selectedSaveIndex].PrimaryFile.m_source != FileHelpers.FileSource.Cloud) ? "$menu_movetocloud" : "$menu_movetolocal");
				}
			}
			else
			{
				this.moveButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize((this.currentList[this.selectedSaveIndex].BackupFiles[this.selectedBackupIndex].m_source != FileHelpers.FileSource.Cloud) ? "$menu_movetocloud" : "$menu_movetolocal");
			}
		}
		this.UpdateButtonsInteractable();
	}

	// Token: 0x06000870 RID: 2160 RVA: 0x0004A650 File Offset: 0x00048850
	private void UpdateButtonsInteractable()
	{
		bool flag = (DateTime.Now - this.mostRecentBackupCreatedTime).TotalSeconds >= 1.0;
		bool flag2 = this.selectedSaveIndex >= 0 && this.selectedSaveIndex < this.listElements.Count;
		bool flag3 = flag2 && this.selectedBackupIndex >= 0;
		bool flag4 = flag2 && this.listElements[this.selectedSaveIndex].BackupCount > 0 && this.selectedBackupIndex < 0;
		this.actionButton.interactable = (flag4 || (flag3 && flag));
		bool flag5 = flag2 && (this.selectedBackupIndex >= 0 || !this.currentList[this.selectedSaveIndex].IsDeleted);
		this.removeButton.interactable = flag5;
		this.moveButton.interactable = (flag5 && flag);
	}

	// Token: 0x06000871 RID: 2161 RVA: 0x0004A735 File Offset: 0x00048935
	private void OnSaveElementHeighChanged()
	{
		this.elementHeightChanged = true;
	}

	// Token: 0x06000872 RID: 2162 RVA: 0x0004A740 File Offset: 0x00048940
	private void UpdateCloudUsageAsync(ManageSavesMenu.UpdateCloudUsageFinishedCallback callback = null)
	{
		if (FileHelpers.CloudStorageEnabled)
		{
			this.PushPleaseWait();
			BackgroundWorker backgroundWorker = new BackgroundWorker();
			ulong usedBytes = 0UL;
			ulong capacityBytes = 0UL;
			backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
			{
				usedBytes = FileHelpers.GetTotalCloudUsage();
				capacityBytes = FileHelpers.GetTotalCloudCapacity();
			};
			backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
			{
				this.storageUsed.gameObject.SetActive(true);
				this.storageBar.parent.gameObject.SetActive(true);
				this.storageUsed.text = Localization.instance.Localize("$menu_cloudstorageused", new string[]
				{
					FileHelpers.BytesAsNumberString(usedBytes, 1U),
					FileHelpers.BytesAsNumberString(capacityBytes, 1U)
				});
				this.storageBar.localScale = new Vector3(usedBytes / capacityBytes, this.storageBar.localScale.y, this.storageBar.localScale.z);
				this.PopPleaseWait();
				ManageSavesMenu.UpdateCloudUsageFinishedCallback callback3 = callback;
				if (callback3 == null)
				{
					return;
				}
				callback3();
			};
			backgroundWorker.RunWorkerAsync();
			return;
		}
		this.storageUsed.gameObject.SetActive(false);
		this.storageBar.parent.gameObject.SetActive(false);
		ManageSavesMenu.UpdateCloudUsageFinishedCallback callback2 = callback;
		if (callback2 == null)
		{
			return;
		}
		callback2();
	}

	// Token: 0x06000873 RID: 2163 RVA: 0x0004A7E4 File Offset: 0x000489E4
	private void OnBackButton()
	{
		this.Close();
	}

	// Token: 0x06000874 RID: 2164 RVA: 0x0004A7EC File Offset: 0x000489EC
	private void OnRemoveButton()
	{
		if (this.selectedSaveIndex < 0)
		{
			return;
		}
		bool isBackup = this.selectedBackupIndex >= 0;
		string text;
		if (isBackup)
		{
			text = "$menu_removebackup";
		}
		else
		{
			int activeTab = this.tabHandler.GetActiveTab();
			if (activeTab != 0)
			{
				if (activeTab != 1)
				{
					text = "Remove?";
				}
				else
				{
					text = "$menu_removecharacter";
				}
			}
			else
			{
				text = "$menu_removeworld";
			}
		}
		SaveFile toDelete = isBackup ? this.currentList[this.selectedSaveIndex].BackupFiles[this.selectedBackupIndex] : this.currentList[this.selectedSaveIndex].PrimaryFile;
		UnifiedPopup.Push(new YesNoPopup(Localization.instance.Localize(text), isBackup ? toDelete.FileName : this.currentList[this.selectedSaveIndex].m_name, delegate()
		{
			UnifiedPopup.Pop();
			this.DeleteSaveFile(toDelete, isBackup);
		}, delegate()
		{
			UnifiedPopup.Pop();
		}, false));
	}

	// Token: 0x06000875 RID: 2165 RVA: 0x0004A900 File Offset: 0x00048B00
	private void OnMoveButton()
	{
		if (this.selectedSaveIndex < 0)
		{
			return;
		}
		bool flag = this.selectedBackupIndex >= 0;
		SaveFile saveFile = flag ? this.currentList[this.selectedSaveIndex].BackupFiles[this.selectedBackupIndex] : this.currentList[this.selectedSaveIndex].PrimaryFile;
		FileHelpers.FileSource fileSource = (saveFile.m_source != FileHelpers.FileSource.Cloud) ? FileHelpers.FileSource.Cloud : FileHelpers.FileSource.Local;
		SaveFile saveFile2 = null;
		for (int i = 0; i < this.currentList[this.selectedSaveIndex].BackupFiles.Length; i++)
		{
			if (i != this.selectedBackupIndex && this.currentList[this.selectedSaveIndex].BackupFiles[i].m_source == fileSource && this.currentList[this.selectedSaveIndex].BackupFiles[i].FileName == saveFile.FileName)
			{
				saveFile2 = this.currentList[this.selectedSaveIndex].BackupFiles[i];
				break;
			}
		}
		if (saveFile2 == null && flag && !this.currentList[this.selectedSaveIndex].IsDeleted && this.currentList[this.selectedSaveIndex].PrimaryFile.m_source == fileSource && this.currentList[this.selectedSaveIndex].PrimaryFile.FileName == saveFile.FileName)
		{
			saveFile2 = this.currentList[this.selectedSaveIndex].PrimaryFile;
		}
		if (saveFile2 != null)
		{
			UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$menu_cantmovesave"), Localization.instance.Localize("$menu_duplicatefileprompttext", new string[]
			{
				saveFile.FileName
			}), delegate()
			{
				UnifiedPopup.Pop();
			}, false));
			return;
		}
		if (SaveSystem.IsCorrupt(saveFile))
		{
			UnifiedPopup.Push(new WarningPopup("$menu_cantmovesave", "$menu_savefilecorrupt", delegate()
			{
				UnifiedPopup.Pop();
			}, true));
			return;
		}
		this.MoveSource(saveFile, flag, fileSource);
	}

	// Token: 0x06000876 RID: 2166 RVA: 0x0004AB00 File Offset: 0x00048D00
	private void OnPrimaryActionButton()
	{
		if (this.selectedSaveIndex < 0)
		{
			return;
		}
		if (this.selectedBackupIndex >= 0)
		{
			this.RestoreBackup();
			return;
		}
		if (this.listElements[this.selectedSaveIndex].BackupCount > 0)
		{
			this.listElements[this.selectedSaveIndex].SetExpanded(!this.listElements[this.selectedSaveIndex].IsExpanded, true);
			this.UpdateButtons();
		}
	}

	// Token: 0x06000877 RID: 2167 RVA: 0x0004AB7C File Offset: 0x00048D7C
	private void RestoreBackup()
	{
		SaveWithBackups saveWithBackups = this.currentList[this.selectedSaveIndex];
		SaveFile backup = this.currentList[this.selectedSaveIndex].BackupFiles[this.selectedBackupIndex];
		UnifiedPopup.Push(new YesNoPopup(Localization.instance.Localize("$menu_backuprestorepromptheader"), saveWithBackups.IsDeleted ? Localization.instance.Localize("$menu_backuprestorepromptrecover", new string[]
		{
			saveWithBackups.m_name,
			backup.FileName
		}) : Localization.instance.Localize("$menu_backuprestorepromptreplace", new string[]
		{
			saveWithBackups.m_name,
			backup.FileName
		}), delegate()
		{
			UnifiedPopup.Pop();
			base.<RestoreBackup>g__RestoreBackupAsync|2();
		}, delegate()
		{
			UnifiedPopup.Pop();
		}, false));
	}

	// Token: 0x06000878 RID: 2168 RVA: 0x0004AC6C File Offset: 0x00048E6C
	private void UpdateGuiAfterFileModification(bool alwaysSelectSave = false)
	{
		string saveName = (this.selectedSaveIndex >= 0) ? this.listElements[this.selectedSaveIndex].Save.m_name : "";
		string backupName = (this.selectedSaveIndex >= 0 && this.selectedBackupIndex >= 0 && this.selectedBackupIndex < this.listElements[this.selectedSaveIndex].Save.BackupFiles.Length) ? this.listElements[this.selectedSaveIndex].Save.BackupFiles[this.selectedBackupIndex].FileName : "";
		int saveIndex = this.selectedSaveIndex;
		int backupIndex = this.selectedBackupIndex;
		this.DeselectCurrent();
		this.UpdateCloudUsageAsync(null);
		this.ReloadSavesAsync(delegate(bool success)
		{
			if (success)
			{
				base.<UpdateGuiAfterFileModification>g__UpdateGuiAsync|1();
				return;
			}
			this.ShowReloadError();
		});
	}

	// Token: 0x06000879 RID: 2169 RVA: 0x0004AD60 File Offset: 0x00048F60
	public void OnWorldTab()
	{
		if (this.pleaseWaitCount > 0)
		{
			return;
		}
		this.ChangeList(SaveDataType.World);
	}

	// Token: 0x0600087A RID: 2170 RVA: 0x0004AD73 File Offset: 0x00048F73
	public void OnCharacterTab()
	{
		if (this.pleaseWaitCount > 0)
		{
			return;
		}
		this.ChangeList(SaveDataType.Character);
	}

	// Token: 0x0600087B RID: 2171 RVA: 0x0004AD86 File Offset: 0x00048F86
	private void ChangeList(SaveDataType dataType)
	{
		this.DeselectCurrent();
		this.currentList = SaveSystem.GetSavesByType(dataType);
		this.currentListType = dataType;
		this.UpdateSavesListGuiAsync(delegate
		{
			bool flag = false;
			if (!string.IsNullOrEmpty(this.m_queuedNameToSelect))
			{
				for (int i = 0; i < this.currentList.Length; i++)
				{
					if (!this.currentList[i].IsDeleted && this.currentList[i].PrimaryFile.FileName == this.m_queuedNameToSelect)
					{
						this.SelectByIndex(i, -1);
						flag = true;
						break;
					}
				}
				this.m_queuedNameToSelect = null;
			}
			if (!flag || this.listElements.Count <= 0)
			{
				this.SelectByIndex(0, -1);
			}
			if (this.selectedSaveIndex >= 0)
			{
				this.CenterSelected();
			}
			this.UpdateButtons();
		});
	}

	// Token: 0x0600087C RID: 2172 RVA: 0x0004ADB4 File Offset: 0x00048FB4
	private void DeleteSaveFile(SaveFile file, bool isBackup)
	{
		this.PushPleaseWait();
		bool success = false;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
		{
			success = SaveSystem.Delete(file);
		};
		backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
		{
			this.PopPleaseWait();
			if (!success)
			{
				ManageSavesMenu.<DeleteSaveFile>g__DeleteSaveFailed|37_2();
				ZLog.LogError("Failed to delete save " + file.FileName);
			}
			this.mostRecentBackupCreatedTime = DateTime.Now;
			ManageSavesMenu.SavesModifiedCallback savesModifiedCallback = this.savesModifiedCallback;
			if (savesModifiedCallback != null)
			{
				savesModifiedCallback(this.GetCurrentListType());
			}
			this.UpdateGuiAfterFileModification(false);
		};
		backgroundWorker.RunWorkerAsync();
	}

	// Token: 0x0600087D RID: 2173 RVA: 0x0004AE10 File Offset: 0x00049010
	private void MoveSource(SaveFile file, bool isBackup, FileHelpers.FileSource destinationSource)
	{
		this.PushPleaseWait();
		bool cloudQuotaExceeded = false;
		bool success = false;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
		{
			success = SaveSystem.MoveSource(file, isBackup, destinationSource, out cloudQuotaExceeded);
		};
		backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
		{
			this.PopPleaseWait();
			if (cloudQuotaExceeded)
			{
				this.ShowCloudQuotaWarning();
			}
			else if (!success)
			{
				ManageSavesMenu.<MoveSource>g__MoveSourceFailed|38_2();
			}
			this.mostRecentBackupCreatedTime = DateTime.Now;
			ManageSavesMenu.SavesModifiedCallback savesModifiedCallback = this.savesModifiedCallback;
			if (savesModifiedCallback != null)
			{
				savesModifiedCallback(this.GetCurrentListType());
			}
			this.UpdateGuiAfterFileModification(false);
		};
		backgroundWorker.RunWorkerAsync();
	}

	// Token: 0x0600087E RID: 2174 RVA: 0x0004AE81 File Offset: 0x00049081
	private SaveDataType GetCurrentListType()
	{
		return this.currentListType;
	}

	// Token: 0x0600087F RID: 2175 RVA: 0x0004AE8C File Offset: 0x0004908C
	private void ReloadSavesAsync(ManageSavesMenu.ReloadSavesFinishedCallback callback)
	{
		this.PushPleaseWait();
		Exception reloadException = null;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
		{
			try
			{
				SaveSystem.ForceRefreshCache();
			}
			catch (Exception reloadException)
			{
				reloadException = reloadException;
			}
		};
		backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
		{
			this.currentList = SaveSystem.GetSavesByType(this.currentListType);
			this.PopPleaseWait();
			if (reloadException != null)
			{
				ZLog.LogError(reloadException.ToString());
			}
			ManageSavesMenu.ReloadSavesFinishedCallback callback2 = callback;
			if (callback2 == null)
			{
				return;
			}
			callback2(reloadException == null);
		};
		backgroundWorker.RunWorkerAsync();
	}

	// Token: 0x06000880 RID: 2176 RVA: 0x0004AEE8 File Offset: 0x000490E8
	private void UpdateElementPositions()
	{
		float num = 0f;
		for (int i = 0; i < this.listElements.Count; i++)
		{
			this.listElements[i].rectTransform.anchoredPosition = new Vector2(this.listElements[i].rectTransform.anchoredPosition.x, -num);
			num += this.listElements[i].rectTransform.sizeDelta.y;
		}
		this.listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
	}

	// Token: 0x06000881 RID: 2177 RVA: 0x0004AF74 File Offset: 0x00049174
	private IEnumerator UpdateElementPositionsEnumerator()
	{
		float pos = 0f;
		int num;
		for (int i = 0; i < this.listElements.Count; i = num + 1)
		{
			this.listElements[i].rectTransform.anchoredPosition = new Vector2(this.listElements[i].rectTransform.anchoredPosition.x, -pos);
			pos += this.listElements[i].rectTransform.sizeDelta.y;
			yield return null;
			num = i;
		}
		this.listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pos);
		yield break;
	}

	// Token: 0x06000882 RID: 2178 RVA: 0x0004AF84 File Offset: 0x00049184
	private ManageSavesMenuElement CreateElement()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.saveElement, this.listRoot);
		ManageSavesMenuElement component = gameObject.GetComponent<ManageSavesMenuElement>();
		gameObject.SetActive(true);
		component.HeightChanged += this.OnSaveElementHeighChanged;
		component.ElementClicked += this.OnElementClicked;
		component.ElementExpandedChanged += this.OnElementExpandedChanged;
		return component;
	}

	// Token: 0x06000883 RID: 2179 RVA: 0x0004AFE8 File Offset: 0x000491E8
	private void UpdateSavesListGui()
	{
		List<ManageSavesMenuElement> list = new List<ManageSavesMenuElement>();
		Dictionary<string, ManageSavesMenuElement> dictionary = new Dictionary<string, ManageSavesMenuElement>();
		for (int i = 0; i < this.listElements.Count; i++)
		{
			dictionary.Add(this.listElements[i].Save.m_name, this.listElements[i]);
		}
		for (int j = 0; j < this.currentList.Length; j++)
		{
			if (dictionary.ContainsKey(this.currentList[j].m_name))
			{
				dictionary[this.currentList[j].m_name].UpdateElement(this.currentList[j]);
				list.Add(dictionary[this.currentList[j].m_name]);
				dictionary.Remove(this.currentList[j].m_name);
			}
			else
			{
				ManageSavesMenuElement manageSavesMenuElement = this.CreateElement();
				manageSavesMenuElement.SetUp(manageSavesMenuElement.Save);
				list.Add(manageSavesMenuElement);
			}
		}
		foreach (KeyValuePair<string, ManageSavesMenuElement> keyValuePair in dictionary)
		{
			UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
		}
		this.listElements = list;
		this.UpdateElementPositions();
	}

	// Token: 0x06000884 RID: 2180 RVA: 0x0004B134 File Offset: 0x00049334
	private IEnumerator UpdateSaveListGuiAsyncCoroutine(ManageSavesMenu.UpdateGuiListFinishedCallback callback)
	{
		this.PushPleaseWait();
		float timeBudget = 0.25f / (float)Application.targetFrameRate;
		DateTime now = DateTime.Now;
		int num;
		for (int i = this.listElements.Count - 1; i >= 0; i = num - 1)
		{
			this.listElements[i].rectTransform.anchoredPosition = new Vector2(this.listElements[i].rectTransform.anchoredPosition.x, 1000000f);
			if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
			num = i;
		}
		if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
		{
			yield return null;
			now = DateTime.Now;
		}
		List<ManageSavesMenuElement> newSaveElementsList = new List<ManageSavesMenuElement>();
		Dictionary<string, ManageSavesMenuElement> saveNameToElementMap = new Dictionary<string, ManageSavesMenuElement>();
		for (int i = 0; i < this.listElements.Count; i = num + 1)
		{
			saveNameToElementMap.Add(this.listElements[i].Save.m_name, this.listElements[i]);
			if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
			num = i;
		}
		for (int i = 0; i < this.currentList.Length; i = num + 1)
		{
			if (saveNameToElementMap.ContainsKey(this.currentList[i].m_name))
			{
				IEnumerator updateElementEnumerator = saveNameToElementMap[this.currentList[i].m_name].UpdateElementEnumerator(this.currentList[i]);
				while (updateElementEnumerator.MoveNext())
				{
					if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
					{
						yield return null;
						now = DateTime.Now;
					}
				}
				newSaveElementsList.Add(saveNameToElementMap[this.currentList[i].m_name]);
				saveNameToElementMap.Remove(this.currentList[i].m_name);
				updateElementEnumerator = null;
			}
			else
			{
				ManageSavesMenuElement manageSavesMenuElement = this.CreateElement();
				newSaveElementsList.Add(manageSavesMenuElement);
				newSaveElementsList[newSaveElementsList.Count - 1].rectTransform.anchoredPosition = new Vector2(newSaveElementsList[newSaveElementsList.Count - 1].rectTransform.anchoredPosition.x, 1000000f);
				IEnumerator updateElementEnumerator = manageSavesMenuElement.SetUpEnumerator(this.currentList[i]);
				while (updateElementEnumerator.MoveNext())
				{
					if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
					{
						yield return null;
						now = DateTime.Now;
					}
				}
				updateElementEnumerator = null;
			}
			if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
			num = i;
		}
		foreach (KeyValuePair<string, ManageSavesMenuElement> keyValuePair in saveNameToElementMap)
		{
			UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
			if ((DateTime.Now - now).TotalSeconds > (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
		}
		Dictionary<string, ManageSavesMenuElement>.Enumerator enumerator = default(Dictionary<string, ManageSavesMenuElement>.Enumerator);
		this.listElements = newSaveElementsList;
		if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
		{
			yield return null;
			now = DateTime.Now;
		}
		IEnumerator updateElementPositionsEnumerator = this.UpdateElementPositionsEnumerator();
		while (updateElementPositionsEnumerator.MoveNext())
		{
			if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
		}
		this.PopPleaseWait();
		if (callback != null)
		{
			callback();
		}
		yield break;
		yield break;
	}

	// Token: 0x06000885 RID: 2181 RVA: 0x0004B14A File Offset: 0x0004934A
	private void UpdateSavesListGuiAsync(ManageSavesMenu.UpdateGuiListFinishedCallback callback)
	{
		base.StartCoroutine(this.UpdateSaveListGuiAsyncCoroutine(callback));
	}

	// Token: 0x06000886 RID: 2182 RVA: 0x0004B15C File Offset: 0x0004935C
	private void DestroyGui()
	{
		for (int i = 0; i < this.listElements.Count; i++)
		{
			UnityEngine.Object.Destroy(this.listElements[i].gameObject);
		}
		this.listElements.Clear();
	}

	// Token: 0x06000887 RID: 2183 RVA: 0x0004B1A0 File Offset: 0x000493A0
	public void Open(SaveDataType dataType, string selectedSaveName, ManageSavesMenu.ClosedCallback closedCallback, ManageSavesMenu.SavesModifiedCallback savesModifiedCallback)
	{
		this.QueueSelectByName(selectedSaveName);
		this.Open(dataType, closedCallback, savesModifiedCallback);
	}

	// Token: 0x06000888 RID: 2184 RVA: 0x0004B1B4 File Offset: 0x000493B4
	public void Open(SaveDataType dataType, ManageSavesMenu.ClosedCallback closedCallback, ManageSavesMenu.SavesModifiedCallback savesModifiedCallback)
	{
		this.closedCallback = closedCallback;
		this.savesModifiedCallback = savesModifiedCallback;
		if (base.gameObject.activeSelf && this.tabHandler.GetActiveTab() == this.GetTabIndexFromSaveDataType(dataType))
		{
			return;
		}
		this.backButton.onClick.AddListener(new UnityAction(this.OnBackButton));
		this.removeButton.onClick.AddListener(new UnityAction(this.OnRemoveButton));
		this.moveButton.onClick.AddListener(new UnityAction(this.OnMoveButton));
		this.actionButton.onClick.AddListener(new UnityAction(this.OnPrimaryActionButton));
		this.storageUsed.gameObject.SetActive(false);
		this.storageBar.parent.gameObject.SetActive(false);
		base.gameObject.SetActive(true);
		this.UpdateCloudUsageAsync(null);
		this.ReloadSavesAsync(delegate(bool success)
		{
			if (!success)
			{
				this.ShowReloadError();
			}
			this.tabHandler.SetActiveTabWithoutInvokingOnClick(this.GetTabIndexFromSaveDataType(dataType));
			this.ChangeList(dataType);
		});
	}

	// Token: 0x06000889 RID: 2185 RVA: 0x0004B2C6 File Offset: 0x000494C6
	private void QueueSelectByName(string name)
	{
		this.m_queuedNameToSelect = name;
	}

	// Token: 0x0600088A RID: 2186 RVA: 0x0004B2CF File Offset: 0x000494CF
	private int GetTabIndexFromSaveDataType(SaveDataType dataType)
	{
		if (dataType == SaveDataType.World)
		{
			return 0;
		}
		if (dataType != SaveDataType.Character)
		{
			throw new ArgumentException(string.Format("{0} does not have a tab!", dataType));
		}
		return 1;
	}

	// Token: 0x0600088B RID: 2187 RVA: 0x0004B2F4 File Offset: 0x000494F4
	public void Close()
	{
		this.DestroyGui();
		this.backButton.onClick.RemoveListener(new UnityAction(this.OnBackButton));
		this.removeButton.onClick.RemoveListener(new UnityAction(this.OnRemoveButton));
		this.moveButton.onClick.RemoveListener(new UnityAction(this.OnMoveButton));
		this.actionButton.onClick.RemoveListener(new UnityAction(this.OnPrimaryActionButton));
		base.gameObject.SetActive(false);
		ManageSavesMenu.ClosedCallback closedCallback = this.closedCallback;
		if (closedCallback == null)
		{
			return;
		}
		closedCallback();
	}

	// Token: 0x0600088C RID: 2188 RVA: 0x0004B393 File Offset: 0x00049593
	public bool IsVisible()
	{
		return base.gameObject.activeInHierarchy;
	}

	// Token: 0x0600088D RID: 2189 RVA: 0x0004B3A0 File Offset: 0x000495A0
	private void SelectByIndex(int saveIndex, int backupIndex = -1)
	{
		this.DeselectCurrent();
		this.selectedSaveIndex = saveIndex;
		this.selectedBackupIndex = backupIndex;
		if (this.listElements.Count <= 0)
		{
			this.selectedSaveIndex = -1;
			this.selectedBackupIndex = -1;
			return;
		}
		this.selectedSaveIndex = Mathf.Clamp(this.selectedSaveIndex, 0, this.listElements.Count - 1);
		this.listElements[this.selectedSaveIndex].Select(ref this.selectedBackupIndex);
	}

	// Token: 0x0600088E RID: 2190 RVA: 0x0004B41C File Offset: 0x0004961C
	private void SelectRelative(int offset)
	{
		int num = this.selectedSaveIndex;
		int num2 = this.selectedBackupIndex;
		this.DeselectCurrent();
		if (this.listElements.Count <= 0)
		{
			this.selectedSaveIndex = -1;
			this.selectedBackupIndex = -1;
			return;
		}
		if (num < 0)
		{
			num = 0;
			num2 = -1;
		}
		else if (num > this.listElements.Count - 1)
		{
			num = this.listElements.Count - 1;
			num2 = (this.listElements[num].IsExpanded ? this.listElements[num].BackupCount : -1);
		}
		int num4;
		for (int num3 = offset; num3 != 0; num3 -= num4)
		{
			num4 = Math.Sign(num3);
			if (this.listElements[num].IsExpanded)
			{
				if (num2 + num4 < -1 || num2 + num4 > this.listElements[num].BackupCount - 1)
				{
					if (num + num4 >= 0 && num + num4 <= this.listElements.Count - 1)
					{
						num += num4;
						num2 = ((num4 < 0 && this.listElements[num].IsExpanded) ? (this.listElements[num].BackupCount - 1) : -1);
					}
				}
				else
				{
					num2 += num4;
				}
			}
			else if (num2 >= 0)
			{
				if (num + num4 >= 0 && num + num4 <= this.listElements.Count - 1 && num4 > 0)
				{
					num += num4;
				}
				num2 = -1;
			}
			else if (num + num4 >= 0 && num + num4 <= this.listElements.Count - 1)
			{
				num += num4;
				num2 = ((num4 < 0 && this.listElements[num].IsExpanded) ? (this.listElements[num].BackupCount - 1) : -1);
			}
		}
		this.SelectByIndex(num, num2);
	}

	// Token: 0x0600088F RID: 2191 RVA: 0x0004B5C8 File Offset: 0x000497C8
	private void DeselectCurrent()
	{
		if (this.selectedSaveIndex >= 0 && this.selectedSaveIndex <= this.listElements.Count - 1)
		{
			this.listElements[this.selectedSaveIndex].Deselect(this.selectedBackupIndex);
		}
		this.selectedSaveIndex = -1;
		this.selectedBackupIndex = -1;
	}

	// Token: 0x06000890 RID: 2192 RVA: 0x0004B620 File Offset: 0x00049820
	private bool IsSelectedExpanded()
	{
		if (this.selectedSaveIndex < 0 || this.selectedSaveIndex > this.listElements.Count - 1)
		{
			ZLog.LogError(string.Concat(new string[]
			{
				"Failed to expand save: Index ",
				this.selectedSaveIndex.ToString(),
				" was outside of the valid range 0-",
				(this.listElements.Count - 1).ToString(),
				"."
			}));
			return false;
		}
		return this.listElements[this.selectedSaveIndex].IsExpanded;
	}

	// Token: 0x06000891 RID: 2193 RVA: 0x0004B6B4 File Offset: 0x000498B4
	private void ExpandSelected()
	{
		if (this.selectedSaveIndex < 0 || this.selectedSaveIndex > this.listElements.Count - 1)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Failed to expand save: Index ",
				this.selectedSaveIndex.ToString(),
				" was outside of the valid range 0-",
				(this.listElements.Count - 1).ToString(),
				". Ignoring."
			}));
			return;
		}
		this.listElements[this.selectedSaveIndex].SetExpanded(true, true);
	}

	// Token: 0x06000892 RID: 2194 RVA: 0x0004B748 File Offset: 0x00049948
	private void CollapseSelected()
	{
		if (this.selectedSaveIndex < 0 || this.selectedSaveIndex > this.listElements.Count - 1)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Failed to collapse save: Index ",
				this.selectedSaveIndex.ToString(),
				" was outside of the valid range 0-",
				(this.listElements.Count - 1).ToString(),
				". Ignoring."
			}));
			return;
		}
		this.listElements[this.selectedSaveIndex].SetExpanded(false, true);
	}

	// Token: 0x06000893 RID: 2195 RVA: 0x0004B7DC File Offset: 0x000499DC
	private void CenterSelected()
	{
		if (this.selectedSaveIndex < 0 || this.selectedSaveIndex > this.listElements.Count - 1)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Failed to center save: Index ",
				this.selectedSaveIndex.ToString(),
				" was outside of the valid range 0-",
				(this.listElements.Count - 1).ToString(),
				". Ignoring."
			}));
			return;
		}
		this.scrollRectEnsureVisible.CenterOnItem(this.listElements[this.selectedSaveIndex].GetTransform(this.selectedBackupIndex));
	}

	// Token: 0x06000894 RID: 2196 RVA: 0x0004B880 File Offset: 0x00049A80
	private void OnElementClicked(ManageSavesMenuElement element, int backupElementIndex)
	{
		int num = this.selectedSaveIndex;
		int num2 = this.selectedBackupIndex;
		int saveIndex = this.listElements.IndexOf(element);
		this.DeselectCurrent();
		this.SelectByIndex(saveIndex, backupElementIndex);
		if (this.selectedSaveIndex == num && this.selectedBackupIndex == num2 && Time.time < this.timeClicked + 0.5f)
		{
			this.OnPrimaryActionButton();
			this.timeClicked = Time.time - 0.5f;
		}
		else
		{
			this.timeClicked = Time.time;
		}
		this.UpdateButtons();
	}

	// Token: 0x06000895 RID: 2197 RVA: 0x0004B908 File Offset: 0x00049B08
	private void OnElementExpandedChanged(ManageSavesMenuElement element, bool isExpanded)
	{
		int num = this.listElements.IndexOf(element);
		if (this.selectedSaveIndex == num)
		{
			if (!isExpanded && this.selectedBackupIndex >= 0)
			{
				this.DeselectCurrent();
				this.SelectByIndex(num, -1);
			}
			this.UpdateButtons();
		}
	}

	// Token: 0x06000896 RID: 2198 RVA: 0x0004B94B File Offset: 0x00049B4B
	public void ShowCloudQuotaWarning()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_cloudstoragefull", "$menu_cloudstoragefulloperationfailed", delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x06000897 RID: 2199 RVA: 0x0004B981 File Offset: 0x00049B81
	public void ShowReloadError()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_reloadfailed", "$menu_checklogfile", delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x06000898 RID: 2200 RVA: 0x0004B9B7 File Offset: 0x00049BB7
	private void PushPleaseWait()
	{
		if (this.pleaseWaitCount == 0)
		{
			this.pleaseWait.SetActive(true);
		}
		this.pleaseWaitCount++;
	}

	// Token: 0x06000899 RID: 2201 RVA: 0x0004B9DB File Offset: 0x00049BDB
	private void PopPleaseWait()
	{
		this.pleaseWaitCount--;
		if (this.pleaseWaitCount == 0)
		{
			this.pleaseWait.SetActive(false);
		}
	}

	// Token: 0x0600089B RID: 2203 RVA: 0x0004BA2B File Offset: 0x00049C2B
	[CompilerGenerated]
	internal static void <RestoreBackup>g__RestoreBackupFailed|32_3()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_backuprestorefailedheader", "$menu_tryagainorrestart", delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x0600089D RID: 2205 RVA: 0x0004BB02 File Offset: 0x00049D02
	[CompilerGenerated]
	internal static void <DeleteSaveFile>g__DeleteSaveFailed|37_2()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_deletefailedheader", "$menu_tryagainorrestart", delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x0600089E RID: 2206 RVA: 0x0004BB38 File Offset: 0x00049D38
	[CompilerGenerated]
	internal static void <MoveSource>g__MoveSourceFailed|38_2()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_movefailedheader", "$menu_tryagainorrestart", delegate()
		{
			UnifiedPopup.Pop();
		}, true));
	}

	// Token: 0x04000A37 RID: 2615
	[SerializeField]
	private Button backButton;

	// Token: 0x04000A38 RID: 2616
	[SerializeField]
	private Button removeButton;

	// Token: 0x04000A39 RID: 2617
	[SerializeField]
	private Button moveButton;

	// Token: 0x04000A3A RID: 2618
	[SerializeField]
	private Button actionButton;

	// Token: 0x04000A3B RID: 2619
	[SerializeField]
	private GameObject saveElement;

	// Token: 0x04000A3C RID: 2620
	[SerializeField]
	private TMP_Text storageUsed;

	// Token: 0x04000A3D RID: 2621
	[SerializeField]
	private TabHandler tabHandler;

	// Token: 0x04000A3E RID: 2622
	[SerializeField]
	private RectTransform storageBar;

	// Token: 0x04000A3F RID: 2623
	[SerializeField]
	private RectTransform listRoot;

	// Token: 0x04000A40 RID: 2624
	[SerializeField]
	private ScrollRectEnsureVisible scrollRectEnsureVisible;

	// Token: 0x04000A41 RID: 2625
	[SerializeField]
	private UIGamePad blockerInfo;

	// Token: 0x04000A42 RID: 2626
	[SerializeField]
	private GameObject pleaseWait;

	// Token: 0x04000A43 RID: 2627
	private SaveWithBackups[] currentList;

	// Token: 0x04000A44 RID: 2628
	private SaveDataType currentListType;

	// Token: 0x04000A45 RID: 2629
	private DateTime mostRecentBackupCreatedTime = DateTime.MinValue;

	// Token: 0x04000A46 RID: 2630
	private List<ManageSavesMenuElement> listElements = new List<ManageSavesMenuElement>();

	// Token: 0x04000A47 RID: 2631
	private bool elementHeightChanged;

	// Token: 0x04000A48 RID: 2632
	private ManageSavesMenu.ClosedCallback closedCallback;

	// Token: 0x04000A49 RID: 2633
	private ManageSavesMenu.SavesModifiedCallback savesModifiedCallback;

	// Token: 0x04000A4A RID: 2634
	private string m_queuedNameToSelect;

	// Token: 0x04000A4B RID: 2635
	private int selectedSaveIndex = -1;

	// Token: 0x04000A4C RID: 2636
	private int selectedBackupIndex = -1;

	// Token: 0x04000A4D RID: 2637
	private float timeClicked;

	// Token: 0x04000A4E RID: 2638
	private const float doubleClickTime = 0.5f;

	// Token: 0x04000A4F RID: 2639
	private int pleaseWaitCount;

	// Token: 0x02000285 RID: 645
	// (Invoke) Token: 0x06002019 RID: 8217
	public delegate void ClosedCallback();

	// Token: 0x02000286 RID: 646
	// (Invoke) Token: 0x0600201D RID: 8221
	public delegate void SavesModifiedCallback(SaveDataType list);

	// Token: 0x02000287 RID: 647
	// (Invoke) Token: 0x06002021 RID: 8225
	private delegate void UpdateCloudUsageFinishedCallback();

	// Token: 0x02000288 RID: 648
	// (Invoke) Token: 0x06002025 RID: 8229
	private delegate void ReloadSavesFinishedCallback(bool success);

	// Token: 0x02000289 RID: 649
	// (Invoke) Token: 0x06002029 RID: 8233
	private delegate void UpdateGuiListFinishedCallback();
}
