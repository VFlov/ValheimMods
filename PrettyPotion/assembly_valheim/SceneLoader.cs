using System;
using System.Collections;
using SoftReferenceableAssets.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000142 RID: 322
public class SceneLoader : MonoBehaviour
{
	// Token: 0x060013EB RID: 5099 RVA: 0x00092CD0 File Offset: 0x00090ED0
	private void Awake()
	{
		this._showLogos = true;
		this._showHealthWarning = false;
		this._showSaveNotification = false;
		this.healthWarning.gameObject.SetActive(false);
		this.savingNotification.gameObject.SetActive(false);
		this.coffeeStainLogo.SetActive(false);
		this.ironGateLogo.SetActive(false);
		this.gameLogo.SetActive(false);
		ZInput.Initialize();
	}

	// Token: 0x060013EC RID: 5100 RVA: 0x00092D3D File Offset: 0x00090F3D
	private void Start()
	{
		this.StartLoading();
	}

	// Token: 0x060013ED RID: 5101 RVA: 0x00092D48 File Offset: 0x00090F48
	private void Update()
	{
		ZInput.Update(Time.unscaledDeltaTime);
		if (this._skipEnabled && (ZInput.GetButtonDown("JoyButtonA") || ZInput.GetMouseButtonDown(0)))
		{
			this._skipped = true;
		}
		if (LoadingIndicator.s_instance.m_showProgressIndicator)
		{
			float num = (this._sceneLoadOperation == null) ? 0f : this._sceneLoadOperation.Progress;
			if (num <= 0.25f)
			{
				float num2 = num / 0.25f * 0.05f;
				if (this._fakeProgress < num2)
				{
					this._fakeProgress = num2;
				}
				else if (num == 0.25f)
				{
					this._fakeProgress = Mathf.Min(num, this._fakeProgress + Time.deltaTime * 0.01f);
				}
			}
			else
			{
				this._fakeProgress = num;
			}
			LoadingIndicator.SetProgress(this._fakeProgress);
		}
	}

	// Token: 0x060013EE RID: 5102 RVA: 0x00092E0B File Offset: 0x0009100B
	private void OnDestroy()
	{
		if (this._currentLoadingBudgetRequest != ThreadPriority.Low)
		{
			BackgroundLoadingBudgetController.ReleaseLoadingBudgetRequest(this._currentLoadingBudgetRequest);
		}
	}

	// Token: 0x060013EF RID: 5103 RVA: 0x00092E20 File Offset: 0x00091020
	private void StartLoading()
	{
		base.StartCoroutine(this.LoadSceneAsync());
	}

	// Token: 0x060013F0 RID: 5104 RVA: 0x00092E2F File Offset: 0x0009102F
	private IEnumerator LoadSceneAsync()
	{
		string str = "Starting to load scene:";
		SceneReference scene = this.m_scene;
		ZLog.Log(str + scene.ToString());
		this._sceneLoadOperation = SceneManager.LoadSceneAsync(this.m_scene, LoadSceneMode.Single);
		this._currentLoadingBudgetRequest = BackgroundLoadingBudgetController.RequestLoadingBudget(ThreadPriority.Normal);
		this._sceneLoadOperation.AllowSceneActivation = false;
		Localization instance = Localization.instance;
		PlatformInitializer.AllowSaveDataInitialization = false;
		if (PlatformInitializer.StartedSaveDataInitialization)
		{
			while (!PlatformInitializer.SaveDataInitialized)
			{
				yield return null;
			}
		}
		if (this._showLogos)
		{
			Image componentInChildren = this.coffeeStainLogo.GetComponentInChildren<Image>();
			Image igImage = this.ironGateLogo.GetComponentInChildren<Image>();
			if (!this._skipAllAtOnce)
			{
				this._skipped = false;
			}
			if (!this._logosSkippable || !this._skipped)
			{
				yield return this.FadeLogo(this.coffeeStainLogo, componentInChildren, 2f, this.alphaCurve, this.scalingCurve);
			}
			this.coffeeStainLogo.SetActive(false);
			if (!this._skipAllAtOnce)
			{
				this._skipped = false;
			}
			if (!this._logosSkippable || !this._skipped)
			{
				yield return this.FadeLogo(this.ironGateLogo, igImage, 2f, this.alphaCurve, this.scalingCurve);
			}
			this.ironGateLogo.SetActive(false);
			igImage = null;
		}
		if (this._showSaveNotification)
		{
			if (!this._skipAllAtOnce)
			{
				this._skipped = false;
			}
			if (!this._skipped)
			{
				yield return this.ShowSaveNotification();
			}
		}
		if (this._showHealthWarning)
		{
			if (!this._skipAllAtOnce)
			{
				this._skipped = false;
			}
			if (!this._skipped)
			{
				yield return this.ShowHealthWarning();
			}
		}
		this.gameLogo.SetActive(true);
		this._currentLoadingBudgetRequest = BackgroundLoadingBudgetController.UpdateLoadingBudgetRequest(this._currentLoadingBudgetRequest, ThreadPriority.High);
		LoadingIndicator.SetVisibility(true);
		PlatformInitializer.AllowSaveDataInitialization = true;
		while (!PlatformInitializer.SaveDataInitialized)
		{
			yield return null;
		}
		PlatformInitializer.InputDeviceRequired = true;
		while (!this._sceneLoadOperation.IsLoadedButNotActivated)
		{
			yield return null;
		}
		LoadingIndicator.SetVisibility(false);
		while (!LoadingIndicator.IsCompletelyInvisible)
		{
			yield return null;
		}
		yield return null;
		while (PlatformInitializer.WaitingForInputDevice)
		{
			yield return null;
		}
		this._sceneLoadOperation.AllowSceneActivation = true;
		yield break;
	}

	// Token: 0x060013F1 RID: 5105 RVA: 0x00092E3E File Offset: 0x0009103E
	private IEnumerator ShowSaveNotification()
	{
		if (!this._skipped)
		{
			this.savingNotification.alpha = 0f;
			this.savingNotification.gameObject.SetActive(true);
			yield return null;
			LayoutRebuilder.ForceRebuildLayoutImmediate(this.healthWarning.transform as RectTransform);
			float fadeTimer = 0f;
			while (this.savingNotification.alpha < 1f)
			{
				float t = 1f - (0.5f - fadeTimer) / 0.5f;
				float alpha = Mathf.SmoothStep(0f, 1f, t);
				this.savingNotification.alpha = alpha;
				fadeTimer += Time.unscaledDeltaTime;
				if (this._skipped)
				{
					goto IL_1EC;
				}
				yield return null;
			}
			fadeTimer = 0f;
			while (fadeTimer < 5f)
			{
				fadeTimer += Time.unscaledDeltaTime;
				if (this._skipped)
				{
					goto IL_1EC;
				}
				yield return null;
			}
			fadeTimer = 0f;
			while (this.savingNotification.alpha > 0f)
			{
				float t = 1f - (0.5f - fadeTimer) / 0.5f;
				float alpha = Mathf.SmoothStep(this.savingNotification.alpha, 0f, t);
				this.savingNotification.alpha = alpha;
				fadeTimer += Time.unscaledDeltaTime;
				if (this._skipped)
				{
					break;
				}
				yield return null;
			}
		}
		IL_1EC:
		this.savingNotification.gameObject.SetActive(false);
		yield break;
	}

	// Token: 0x060013F2 RID: 5106 RVA: 0x00092E4D File Offset: 0x0009104D
	private IEnumerator ShowHealthWarning()
	{
		if (!this._skipped)
		{
			this.healthWarning.alpha = 0f;
			this.healthWarning.gameObject.SetActive(true);
			yield return null;
			LayoutRebuilder.ForceRebuildLayoutImmediate(this.healthWarning.transform as RectTransform);
			float fadeTimer = 0f;
			while (this.healthWarning.alpha < 1f)
			{
				float t = 1f - (0.5f - fadeTimer) / 0.5f;
				float alpha = Mathf.SmoothStep(0f, 1f, t);
				this.healthWarning.alpha = alpha;
				fadeTimer += Time.unscaledDeltaTime;
				if (this._skipped)
				{
					goto IL_1EC;
				}
				yield return null;
			}
			fadeTimer = 0f;
			while (fadeTimer < 5f)
			{
				fadeTimer += Time.unscaledDeltaTime;
				if (this._skipped)
				{
					goto IL_1EC;
				}
				yield return null;
			}
			fadeTimer = 0f;
			while (this.healthWarning.alpha > 0f)
			{
				float t = 1f - (0.5f - fadeTimer) / 0.5f;
				float alpha = Mathf.SmoothStep(this.healthWarning.alpha, 0f, t);
				this.healthWarning.alpha = alpha;
				fadeTimer += Time.unscaledDeltaTime;
				if (this._skipped)
				{
					break;
				}
				yield return null;
			}
		}
		IL_1EC:
		this.healthWarning.gameObject.SetActive(false);
		yield break;
	}

	// Token: 0x060013F3 RID: 5107 RVA: 0x00092E5C File Offset: 0x0009105C
	private IEnumerator FadeLogo(GameObject parentGameObject, Image logo, float duration, AnimationCurve alpha, AnimationCurve scale)
	{
		Color spriteColor = logo.color;
		float timer = 0f;
		parentGameObject.SetActive(true);
		while (timer < duration && (!this._logosSkippable || !this._skipped))
		{
			float num = alpha.Evaluate(timer);
			spriteColor.a = num;
			logo.color = spriteColor;
			num = scale.Evaluate(timer);
			logo.transform.localScale = Vector3.one * num;
			timer += Time.deltaTime;
			yield return null;
		}
		yield break;
	}

	// Token: 0x040013AD RID: 5037
	public SceneReference m_scene;

	// Token: 0x040013AE RID: 5038
	private bool _showLogos = true;

	// Token: 0x040013AF RID: 5039
	private bool _showHealthWarning;

	// Token: 0x040013B0 RID: 5040
	private bool _showSaveNotification;

	// Token: 0x040013B1 RID: 5041
	private bool _skipEnabled;

	// Token: 0x040013B2 RID: 5042
	private bool _logosSkippable;

	// Token: 0x040013B3 RID: 5043
	private bool _skipAllAtOnce;

	// Token: 0x040013B4 RID: 5044
	private bool _skipped;

	// Token: 0x040013B5 RID: 5045
	private ILoadSceneAsyncOperation _sceneLoadOperation;

	// Token: 0x040013B6 RID: 5046
	private ThreadPriority _currentLoadingBudgetRequest;

	// Token: 0x040013B7 RID: 5047
	private float _fakeProgress;

	// Token: 0x040013B8 RID: 5048
	[SerializeField]
	private GameObject gameLogo;

	// Token: 0x040013B9 RID: 5049
	[SerializeField]
	private GameObject coffeeStainLogo;

	// Token: 0x040013BA RID: 5050
	[SerializeField]
	private GameObject ironGateLogo;

	// Token: 0x040013BB RID: 5051
	[SerializeField]
	private CanvasGroup savingNotification;

	// Token: 0x040013BC RID: 5052
	[SerializeField]
	private CanvasGroup healthWarning;

	// Token: 0x040013BD RID: 5053
	public AnimationCurve alphaCurve;

	// Token: 0x040013BE RID: 5054
	public AnimationCurve scalingCurve;

	// Token: 0x040013BF RID: 5055
	private const float LogoDisplayTime = 2f;

	// Token: 0x040013C0 RID: 5056
	private const float SaveNotificationDisplayTime = 5f;

	// Token: 0x040013C1 RID: 5057
	private const float HealthWarningDisplayTime = 5f;

	// Token: 0x040013C2 RID: 5058
	private const float FadeInOutTime = 0.5f;
}
