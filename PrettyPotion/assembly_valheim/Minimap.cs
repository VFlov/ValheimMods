using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GUIFramework;
using Splatform;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Token: 0x02000087 RID: 135
public class Minimap : MonoBehaviour
{
	// Token: 0x1700003D RID: 61
	// (get) Token: 0x060008F8 RID: 2296 RVA: 0x0004DDCA File Offset: 0x0004BFCA
	public static Minimap instance
	{
		get
		{
			return Minimap.m_instance;
		}
	}

	// Token: 0x060008F9 RID: 2297 RVA: 0x0004DDD1 File Offset: 0x0004BFD1
	private void Awake()
	{
		Minimap.m_instance = this;
		this.m_largeRoot.SetActive(false);
		this.m_smallRoot.SetActive(false);
		Localization.OnLanguageChange = (Action)Delegate.Combine(Localization.OnLanguageChange, new Action(this.OnLanguageChange));
	}

	// Token: 0x060008FA RID: 2298 RVA: 0x0004DE14 File Offset: 0x0004C014
	private void OnDestroy()
	{
		Localization.OnLanguageChange = (Action)Delegate.Remove(Localization.OnLanguageChange, new Action(this.OnLanguageChange));
		GuiInputField nameInput = this.m_nameInput;
		nameInput.VirtualKeyboardStateChange = (Action<VirtualKeyboardState>)Delegate.Remove(nameInput.VirtualKeyboardStateChange, new Action<VirtualKeyboardState>(this.VirtualKeyboardStateChange));
		Minimap.m_instance = null;
	}

	// Token: 0x060008FB RID: 2299 RVA: 0x0004DE6E File Offset: 0x0004C06E
	public void PauseUpdateTemporarily()
	{
		this.m_pauseUpdate = 0.1f;
	}

	// Token: 0x060008FC RID: 2300 RVA: 0x0004DE7B File Offset: 0x0004C07B
	public static bool IsOpen()
	{
		return Minimap.m_instance && (Minimap.m_instance.m_largeRoot.activeSelf || Minimap.m_instance.m_hiddenFrames <= 2);
	}

	// Token: 0x060008FD RID: 2301 RVA: 0x0004DEAE File Offset: 0x0004C0AE
	public static bool InTextInput()
	{
		return Minimap.m_instance && Minimap.m_instance.m_mode == Minimap.MapMode.Large && Minimap.m_instance.m_wasFocused;
	}

	// Token: 0x060008FE RID: 2302 RVA: 0x0004DED8 File Offset: 0x0004C0D8
	private void OnLanguageChange()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		this.m_biomeNameSmall.text = Localization.instance.Localize("$biome_" + localPlayer.GetCurrentBiome().ToString().ToLower());
	}

	// Token: 0x060008FF RID: 2303 RVA: 0x0004DF30 File Offset: 0x0004C130
	private void Start()
	{
		this.m_mapTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RGB24, false);
		this.m_mapTexture.name = "_Minimap m_mapTexture";
		this.m_mapTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_forestMaskTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RGBA32, false);
		this.m_forestMaskTexture.name = "_Minimap m_forestMaskTexture";
		this.m_forestMaskTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_heightTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RFloat, false);
		this.m_heightTexture.name = "_Minimap m_heightTexture";
		this.m_heightTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_fogTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RGBA32, false);
		this.m_fogTexture.name = "_Minimap m_fogTexture";
		this.m_fogTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_explored = new bool[this.m_textureSize * this.m_textureSize];
		this.m_exploredOthers = new bool[this.m_textureSize * this.m_textureSize];
		this.m_mapImageLarge.material = UnityEngine.Object.Instantiate<Material>(this.m_mapImageLarge.material);
		this.m_mapImageSmall.material = UnityEngine.Object.Instantiate<Material>(this.m_mapImageSmall.material);
		this.m_mapSmallShader = this.m_mapImageSmall.material;
		this.m_mapLargeShader = this.m_mapImageLarge.material;
		this.m_mapLargeShader.SetTexture("_MainTex", this.m_mapTexture);
		this.m_mapLargeShader.SetTexture("_MaskTex", this.m_forestMaskTexture);
		this.m_mapLargeShader.SetTexture("_HeightTex", this.m_heightTexture);
		this.m_mapLargeShader.SetTexture("_FogTex", this.m_fogTexture);
		this.m_mapSmallShader.SetTexture("_MainTex", this.m_mapTexture);
		this.m_mapSmallShader.SetTexture("_MaskTex", this.m_forestMaskTexture);
		this.m_mapSmallShader.SetTexture("_HeightTex", this.m_heightTexture);
		this.m_mapSmallShader.SetTexture("_FogTex", this.m_fogTexture);
		this.m_nameInput.gameObject.SetActive(false);
		UIInputHandler component = this.m_mapImageLarge.GetComponent<UIInputHandler>();
		component.m_onRightClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onRightClick, new Action<UIInputHandler>(this.OnMapRightClick));
		component.m_onMiddleClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onMiddleClick, new Action<UIInputHandler>(this.OnMapMiddleClick));
		component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(this.OnMapLeftDown));
		component.m_onLeftUp = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftUp, new Action<UIInputHandler>(this.OnMapLeftUp));
		this.m_visibleIconTypes = new bool[Enum.GetValues(typeof(Minimap.PinType)).Length];
		for (int i = 0; i < this.m_visibleIconTypes.Length; i++)
		{
			this.m_visibleIconTypes[i] = true;
		}
		this.m_selectedIcons[Minimap.PinType.Death] = this.m_selectedIconDeath;
		this.m_selectedIcons[Minimap.PinType.Boss] = this.m_selectedIconBoss;
		this.m_selectedIcons[Minimap.PinType.Icon0] = this.m_selectedIcon0;
		this.m_selectedIcons[Minimap.PinType.Icon1] = this.m_selectedIcon1;
		this.m_selectedIcons[Minimap.PinType.Icon2] = this.m_selectedIcon2;
		this.m_selectedIcons[Minimap.PinType.Icon3] = this.m_selectedIcon3;
		this.m_selectedIcons[Minimap.PinType.Icon4] = this.m_selectedIcon4;
		this.SelectIcon(Minimap.PinType.Icon0);
		this.Reset();
		this.m_pingImageObject.sprite = this.m_pingIcon;
		if (ZNet.World == null)
		{
			return;
		}
		World world = ZNet.World;
		string rootPath = ZNet.World.GetRootPath(FileHelpers.FileSource.Local);
		if (FileHelpers.LocalStorageSupport == LocalStorageSupport.Supported)
		{
			Directory.CreateDirectory(World.GetWorldSavePath(FileHelpers.FileSource.Local));
		}
		this.m_forestMaskTexturePath = Minimap.GetCompleteTexturePath(rootPath, "forestMaskTexCache");
		this.m_mapTexturePath = Minimap.GetCompleteTexturePath(rootPath, "mapTexCache");
		this.m_heightTexturePath = Minimap.GetCompleteTexturePath(rootPath, "heightTexCache");
	}

	// Token: 0x06000900 RID: 2304 RVA: 0x0004E328 File Offset: 0x0004C528
	public void Reset()
	{
		Color32[] array = new Color32[this.m_textureSize * this.m_textureSize];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		}
		this.m_fogTexture.SetPixels32(array);
		this.m_fogTexture.Apply();
		for (int j = 0; j < this.m_explored.Length; j++)
		{
			this.m_explored[j] = false;
			this.m_exploredOthers[j] = false;
		}
		this.m_sharedMapHint.gameObject.SetActive(false);
	}

	// Token: 0x06000901 RID: 2305 RVA: 0x0004E3C4 File Offset: 0x0004C5C4
	public void ResetSharedMapData()
	{
		Color[] pixels = this.m_fogTexture.GetPixels();
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i].g = 255f;
		}
		this.m_fogTexture.SetPixels(pixels);
		this.m_fogTexture.Apply();
		for (int j = 0; j < this.m_exploredOthers.Length; j++)
		{
			this.m_exploredOthers[j] = false;
		}
		for (int k = this.m_pins.Count - 1; k >= 0; k--)
		{
			Minimap.PinData pinData = this.m_pins[k];
			if (pinData.m_ownerID != 0L)
			{
				this.DestroyPinMarker(pinData);
				this.m_pins.RemoveAt(k);
			}
		}
		this.m_sharedMapHint.gameObject.SetActive(false);
	}

	// Token: 0x06000902 RID: 2306 RVA: 0x0004E483 File Offset: 0x0004C683
	public void ForceRegen()
	{
		if (WorldGenerator.instance != null)
		{
			this.GenerateWorldMap();
		}
	}

	// Token: 0x06000903 RID: 2307 RVA: 0x0004E494 File Offset: 0x0004C694
	private void Update()
	{
		if (ZInput.VirtualKeyboardOpen)
		{
			return;
		}
		if (this.m_pauseUpdate > 0f)
		{
			this.m_pauseUpdate -= Time.deltaTime;
			return;
		}
		this.inputDelay = Mathf.Max(0f, this.inputDelay - Time.deltaTime);
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			return;
		}
		if (Utils.GetMainCamera() == null)
		{
			return;
		}
		if (!this.m_hasGenerated)
		{
			if (WorldGenerator.instance == null)
			{
				return;
			}
			if (!this.TryLoadMinimapTextureData())
			{
				this.GenerateWorldMap();
			}
			this.LoadMapData();
			this.m_hasGenerated = true;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		this.UpdateExplore(deltaTime, localPlayer);
		if (localPlayer.IsDead())
		{
			this.SetMapMode(Minimap.MapMode.None);
			return;
		}
		if (this.m_mode == Minimap.MapMode.None)
		{
			this.SetMapMode(Minimap.MapMode.Small);
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			this.m_hiddenFrames = 0;
		}
		else
		{
			this.m_hiddenFrames++;
		}
		bool flag = (Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !TextInput.IsVisible() && !Menu.IsActive() && !InventoryGui.IsVisible();
		if (flag)
		{
			if (Minimap.InTextInput())
			{
				if ((ZInput.GetKeyDown(KeyCode.Escape, true) || ZInput.GetButton("JoyButtonB")) && this.m_namePin != null)
				{
					this.m_nameInput.text = "";
					this.OnPinTextEntered("");
				}
			}
			else if (ZInput.GetButtonDown("Map") || (ZInput.GetButtonDown("JoyMap") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")) && !ZInput.GetButton("JoyAltKeys")) || (this.m_mode == Minimap.MapMode.Large && (ZInput.GetKeyDown(KeyCode.Escape, true) || (ZInput.GetButtonDown("JoyMap") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper"))) || ZInput.GetButtonDown("JoyButtonB"))))
			{
				switch (this.m_mode)
				{
				case Minimap.MapMode.None:
					this.SetMapMode(Minimap.MapMode.Small);
					break;
				case Minimap.MapMode.Small:
					this.SetMapMode(Minimap.MapMode.Large);
					break;
				case Minimap.MapMode.Large:
					this.SetMapMode(Minimap.MapMode.Small);
					break;
				}
			}
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			this.m_publicPosition.isOn = ZNet.instance.IsReferencePositionPublic();
			this.m_gamepadCrosshair.gameObject.SetActive(ZInput.IsGamepadActive());
		}
		if (this.m_showSharedMapData && this.m_sharedMapDataFade < 1f)
		{
			this.m_sharedMapDataFade = Mathf.Min(1f, this.m_sharedMapDataFade + this.m_sharedMapDataFadeRate * deltaTime);
			this.m_mapSmallShader.SetFloat("_SharedFade", this.m_sharedMapDataFade);
			this.m_mapLargeShader.SetFloat("_SharedFade", this.m_sharedMapDataFade);
			if (this.m_sharedMapDataFade == 1f)
			{
				this.m_pinUpdateRequired = true;
			}
		}
		else if (!this.m_showSharedMapData && this.m_sharedMapDataFade > 0f)
		{
			this.m_sharedMapDataFade = Mathf.Max(0f, this.m_sharedMapDataFade - this.m_sharedMapDataFadeRate * deltaTime);
			this.m_mapSmallShader.SetFloat("_SharedFade", this.m_sharedMapDataFade);
			this.m_mapLargeShader.SetFloat("_SharedFade", this.m_sharedMapDataFade);
			if (this.m_sharedMapDataFade == 0f)
			{
				this.m_pinUpdateRequired = true;
			}
		}
		this.UpdateMap(localPlayer, deltaTime, flag);
		this.UpdateDynamicPins(deltaTime);
		if (this.m_pinUpdateRequired)
		{
			this.m_pinUpdateRequired = false;
			this.UpdatePins();
		}
		this.UpdateBiome(localPlayer);
		this.UpdateNameInput();
	}

	// Token: 0x06000904 RID: 2308 RVA: 0x0004E814 File Offset: 0x0004CA14
	private bool TryLoadMinimapTextureData()
	{
		if (string.IsNullOrEmpty(this.m_forestMaskTexturePath) || !File.Exists(this.m_forestMaskTexturePath) || !File.Exists(this.m_mapTexturePath) || !File.Exists(this.m_heightTexturePath) || 35 != ZNet.World.m_worldVersion)
		{
			return false;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		Texture2D texture2D = new Texture2D(this.m_forestMaskTexture.width, this.m_forestMaskTexture.height, TextureFormat.ARGB32, false);
		if (!texture2D.LoadImage(File.ReadAllBytes(this.m_forestMaskTexturePath)))
		{
			return false;
		}
		this.m_forestMaskTexture.SetPixels(texture2D.GetPixels());
		this.m_forestMaskTexture.Apply();
		if (!texture2D.LoadImage(File.ReadAllBytes(this.m_mapTexturePath)))
		{
			return false;
		}
		this.m_mapTexture.SetPixels(texture2D.GetPixels());
		this.m_mapTexture.Apply();
		if (!texture2D.LoadImage(File.ReadAllBytes(this.m_heightTexturePath)))
		{
			return false;
		}
		Color[] pixels = texture2D.GetPixels();
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				int num = i * this.m_textureSize + j;
				int num2 = (int)(pixels[num].r * 255f);
				int num3 = (int)(pixels[num].g * 255f);
				int num4 = (num2 << 8) + num3;
				float num5 = 127.5f;
				pixels[num].r = (float)num4 / num5;
			}
		}
		this.m_heightTexture.SetPixels(pixels);
		this.m_heightTexture.Apply();
		ZLog.Log("Loading minimap textures done [" + stopwatch.ElapsedMilliseconds.ToString() + "ms]");
		return true;
	}

	// Token: 0x06000905 RID: 2309 RVA: 0x0004E9C0 File Offset: 0x0004CBC0
	private void ShowPinNameInput(Vector3 pos)
	{
		this.m_namePin = this.AddPin(pos, this.m_selectedType, "", true, false, 0L, default(PlatformUserID));
		this.m_nameInput.text = "";
		this.m_nameInput.gameObject.SetActive(true);
		if (ZInput.IsGamepadActive())
		{
			this.m_nameInput.gameObject.transform.localPosition = new Vector3(0f, -30f, 0f);
		}
		else
		{
			Vector2 vector;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.m_nameInput.gameObject.transform.parent.GetComponent<RectTransform>(), ZInput.mousePosition, null, out vector);
			this.m_nameInput.gameObject.transform.localPosition = new Vector3(vector.x, vector.y - 30f);
		}
		if (Application.isConsolePlatform && ZInput.IsGamepadActive())
		{
			this.m_nameInput.Select();
		}
		else
		{
			this.m_nameInput.ActivateInputField();
		}
		GuiInputField nameInput = this.m_nameInput;
		nameInput.VirtualKeyboardStateChange = (Action<VirtualKeyboardState>)Delegate.Remove(nameInput.VirtualKeyboardStateChange, new Action<VirtualKeyboardState>(this.VirtualKeyboardStateChange));
		GuiInputField nameInput2 = this.m_nameInput;
		nameInput2.VirtualKeyboardStateChange = (Action<VirtualKeyboardState>)Delegate.Combine(nameInput2.VirtualKeyboardStateChange, new Action<VirtualKeyboardState>(this.VirtualKeyboardStateChange));
		this.m_wasFocused = true;
	}

	// Token: 0x06000906 RID: 2310 RVA: 0x0004EB19 File Offset: 0x0004CD19
	private void UpdateNameInput()
	{
		if (this.m_delayTextInput < 0f)
		{
			return;
		}
		this.m_delayTextInput -= Time.deltaTime;
		this.m_wasFocused = (this.m_delayTextInput > 0f);
	}

	// Token: 0x06000907 RID: 2311 RVA: 0x0004EB4E File Offset: 0x0004CD4E
	private void VirtualKeyboardStateChange(VirtualKeyboardState state)
	{
		if (state != VirtualKeyboardState.Cancel)
		{
			return;
		}
		this.HidePinTextInput(true);
	}

	// Token: 0x06000908 RID: 2312 RVA: 0x0004EB5C File Offset: 0x0004CD5C
	private void CreateMapNamePin(Minimap.PinData namePin, RectTransform root)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_pinNamePrefab, root);
		namePin.m_NamePinData.SetTextAndGameObject(gameObject);
		namePin.m_NamePinData.PinNameRectTransform.SetParent(root);
		this.m_pinUpdateRequired = true;
		base.StartCoroutine(this.DelayActivation(gameObject, 1f));
	}

	// Token: 0x06000909 RID: 2313 RVA: 0x0004EBAD File Offset: 0x0004CDAD
	private IEnumerator DelayActivation(GameObject go, float delay)
	{
		go.SetActive(false);
		yield return new WaitForSeconds(delay);
		if (this == null || go == null || this.m_mode != Minimap.MapMode.Large)
		{
			yield break;
		}
		go.SetActive(true);
		yield break;
	}

	// Token: 0x0600090A RID: 2314 RVA: 0x0004EBCC File Offset: 0x0004CDCC
	public void OnPinTextEntered(string t)
	{
		string text = this.m_nameInput.text;
		if (text.Length > 0 && this.m_namePin != null)
		{
			text = text.Replace('$', ' ');
			text = text.Replace('<', ' ');
			text = text.Replace('>', ' ');
			this.m_namePin.m_name = text;
			if (!string.IsNullOrEmpty(text) && this.m_namePin.m_NamePinData == null)
			{
				this.m_namePin.m_NamePinData = new Minimap.PinNameData(this.m_namePin);
				if (this.m_namePin.m_NamePinData.PinNameGameObject == null)
				{
					this.CreateMapNamePin(this.m_namePin, this.m_pinNameRootLarge);
				}
			}
		}
		this.HidePinTextInput(true);
	}

	// Token: 0x0600090B RID: 2315 RVA: 0x0004EC88 File Offset: 0x0004CE88
	private void HidePinTextInput(bool delayTextInput = false)
	{
		GuiInputField nameInput = this.m_nameInput;
		nameInput.VirtualKeyboardStateChange = (Action<VirtualKeyboardState>)Delegate.Remove(nameInput.VirtualKeyboardStateChange, new Action<VirtualKeyboardState>(this.VirtualKeyboardStateChange));
		this.m_namePin = null;
		this.m_nameInput.text = "";
		this.m_nameInput.gameObject.SetActive(false);
		if (delayTextInput)
		{
			this.m_delayTextInput = 0.1f;
			this.m_wasFocused = true;
			return;
		}
		this.m_wasFocused = false;
	}

	// Token: 0x0600090C RID: 2316 RVA: 0x0004ED04 File Offset: 0x0004CF04
	private void UpdateMap(Player player, float dt, bool takeInput)
	{
		if (takeInput)
		{
			if (this.m_mode == Minimap.MapMode.Large)
			{
				float num = 0f;
				num += ZInput.GetMouseScrollWheel();
				num = Mathf.Clamp(num, -0.05f, 0.05f) * this.m_largeZoom * 2f;
				if (ZInput.GetButton("JoyButtonX") && this.inputDelay <= 0f)
				{
					Vector3 viewCenterWorldPoint = this.GetViewCenterWorldPoint();
					Chat.instance.SendPing(viewCenterWorldPoint);
				}
				if (ZInput.GetButton("JoyLTrigger") && !this.m_nameInput.gameObject.activeSelf)
				{
					num -= this.m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButton("JoyRTrigger") && !this.m_nameInput.gameObject.activeSelf)
				{
					num += this.m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButtonDown("JoyDPadUp"))
				{
					Minimap.PinType pinType = Minimap.PinType.None;
					using (Dictionary<Minimap.PinType, Image>.Enumerator enumerator = this.m_selectedIcons.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							KeyValuePair<Minimap.PinType, Image> keyValuePair = enumerator.Current;
							if (keyValuePair.Key == this.m_selectedType && pinType != Minimap.PinType.None)
							{
								this.SelectIcon(pinType);
								break;
							}
							pinType = keyValuePair.Key;
						}
						goto IL_190;
					}
				}
				if (ZInput.GetButtonDown("JoyDPadDown"))
				{
					bool flag = false;
					foreach (KeyValuePair<Minimap.PinType, Image> keyValuePair2 in this.m_selectedIcons)
					{
						if (flag)
						{
							this.SelectIcon(keyValuePair2.Key);
							break;
						}
						if (keyValuePair2.Key == this.m_selectedType)
						{
							flag = true;
						}
					}
				}
				IL_190:
				if (ZInput.GetButtonDown("JoyDPadRight"))
				{
					this.ToggleIconFilter(this.m_selectedType);
				}
				if (this.m_selectedType != Minimap.PinType.Boss && this.m_selectedType != Minimap.PinType.Death && ZInput.GetButtonUp("JoyButtonA") && !Minimap.InTextInput() && this.inputDelay <= 0f)
				{
					this.ShowPinNameInput(this.ScreenToWorldPoint(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2))));
				}
				if (ZInput.GetButtonDown("JoyTabRight"))
				{
					Vector3 pos = this.ScreenToWorldPoint(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2)));
					this.RemovePin(pos, this.m_removeRadius * (this.m_largeZoom * 2f));
					this.HidePinTextInput(false);
				}
				if (ZInput.GetButtonDown("JoyTabLeft"))
				{
					Vector3 pos2 = this.ScreenToWorldPoint(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2)));
					Minimap.PinData closestPin = this.GetClosestPin(pos2, this.m_removeRadius * (this.m_largeZoom * 2f), true);
					if (closestPin != null)
					{
						if (closestPin.m_ownerID != 0L)
						{
							closestPin.m_ownerID = 0L;
						}
						else
						{
							closestPin.m_checked = !closestPin.m_checked;
						}
					}
					this.HidePinTextInput(false);
				}
				if (ZInput.GetButtonDown("MapZoomOut") && !Minimap.InTextInput() && !this.m_nameInput.gameObject.activeSelf)
				{
					num -= this.m_largeZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn") && !Minimap.InTextInput() && !this.m_nameInput.gameObject.activeSelf)
				{
					num += this.m_largeZoom * 0.5f;
				}
				if (!Minimap.InTextInput())
				{
					this.m_largeZoom = Mathf.Clamp(this.m_largeZoom - num, this.m_minZoom, this.m_maxZoom);
				}
			}
			else
			{
				float num2 = 0f;
				if (ZInput.GetButtonDown("MapZoomOut") && !this.m_nameInput.gameObject.activeSelf)
				{
					num2 -= this.m_smallZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn") && !this.m_nameInput.gameObject.activeSelf)
				{
					num2 += this.m_smallZoom * 0.5f;
				}
				if (ZInput.GetButton("JoyAltKeys") && ZInput.GetButtonDown("JoyMiniMapZoomOut") && !this.m_nameInput.gameObject.activeSelf)
				{
					num2 -= this.m_smallZoom * 0.5f;
				}
				if (ZInput.GetButton("JoyAltKeys") && ZInput.GetButtonDown("JoyMiniMapZoomIn") && !this.m_nameInput.gameObject.activeSelf)
				{
					num2 += this.m_smallZoom * 0.5f;
				}
				if (!Minimap.InTextInput())
				{
					this.m_smallZoom = Mathf.Clamp(this.m_smallZoom - num2, this.m_minZoom, this.m_maxZoom);
				}
			}
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			if (this.m_leftDownTime != 0f && this.m_leftDownTime > this.m_clickDuration && !this.m_dragView)
			{
				this.m_dragWorldPos = this.ScreenToWorldPoint(ZInput.mousePosition);
				this.m_dragView = true;
				this.HidePinTextInput(false);
			}
			if (!this.m_nameInput.gameObject.activeSelf)
			{
				this.m_mapOffset.x = this.m_mapOffset.x + ZInput.GetJoyLeftStickX(true) * dt * 50000f * this.m_largeZoom * this.m_gamepadMoveSpeed;
				this.m_mapOffset.z = this.m_mapOffset.z - ZInput.GetJoyLeftStickY(true) * dt * 50000f * this.m_largeZoom * this.m_gamepadMoveSpeed;
			}
			if (this.m_dragView)
			{
				Vector3 b = this.ScreenToWorldPoint(ZInput.mousePosition) - this.m_dragWorldPos;
				this.m_mapOffset -= b;
				this.m_pinUpdateRequired = true;
				this.CenterMap(player.transform.position + this.m_mapOffset);
				this.m_dragWorldPos = this.ScreenToWorldPoint(ZInput.mousePosition);
			}
			else
			{
				this.CenterMap(player.transform.position + this.m_mapOffset);
			}
		}
		else
		{
			this.CenterMap(player.transform.position);
		}
		this.UpdateWindMarker();
		this.UpdatePlayerMarker(player, Utils.GetMainCamera().transform.rotation);
	}

	// Token: 0x0600090D RID: 2317 RVA: 0x0004F2F8 File Offset: 0x0004D4F8
	public void SetMapMode(Minimap.MapMode mode)
	{
		if (Game.m_noMap)
		{
			mode = Minimap.MapMode.None;
		}
		if (mode == this.m_mode)
		{
			return;
		}
		this.m_pinUpdateRequired = true;
		this.m_mode = mode;
		switch (mode)
		{
		case Minimap.MapMode.None:
			this.m_largeRoot.SetActive(false);
			this.m_smallRoot.SetActive(false);
			this.HidePinTextInput(false);
			return;
		case Minimap.MapMode.Small:
			this.m_largeRoot.SetActive(false);
			this.m_smallRoot.SetActive(true);
			this.HidePinTextInput(false);
			return;
		case Minimap.MapMode.Large:
		{
			this.m_largeRoot.SetActive(true);
			this.m_smallRoot.SetActive(false);
			bool active = PlayerPrefs.GetInt("KeyHints", 1) == 1;
			foreach (GameObject gameObject in this.m_hints)
			{
				gameObject.SetActive(active);
			}
			this.m_dragView = false;
			this.m_mapOffset = Vector3.zero;
			this.m_namePin = null;
			return;
		}
		default:
			return;
		}
	}

	// Token: 0x0600090E RID: 2318 RVA: 0x0004F400 File Offset: 0x0004D600
	private void CenterMap(Vector3 centerPoint)
	{
		float x;
		float y;
		this.WorldToMapPoint(centerPoint, out x, out y);
		if (this.m_mode == Minimap.MapMode.Small)
		{
			Rect uvRect = this.m_mapImageSmall.uvRect;
			uvRect.width = this.m_smallZoom;
			uvRect.height = this.m_smallZoom;
			uvRect.center = new Vector2(x, y);
			this.m_mapImageSmall.uvRect = uvRect;
		}
		else if (this.m_mode == Minimap.MapMode.Large)
		{
			RectTransform rectTransform = this.m_mapImageLarge.transform as RectTransform;
			float num = rectTransform.rect.width / rectTransform.rect.height;
			Rect uvRect2 = this.m_mapImageSmall.uvRect;
			uvRect2.width = this.m_largeZoom * num;
			uvRect2.height = this.m_largeZoom;
			uvRect2.center = new Vector2(x, y);
			this.m_mapImageLarge.uvRect = uvRect2;
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			this.m_mapLargeShader.SetFloat("_zoom", this.m_largeZoom);
			this.m_mapLargeShader.SetFloat("_pixelSize", 200f / this.m_largeZoom);
			this.m_mapLargeShader.SetVector("_mapCenter", centerPoint);
		}
		else
		{
			this.m_mapSmallShader.SetFloat("_zoom", this.m_smallZoom);
			this.m_mapSmallShader.SetFloat("_pixelSize", 200f / this.m_smallZoom);
			this.m_mapSmallShader.SetVector("_mapCenter", centerPoint);
		}
		if (this.UpdatedMap(centerPoint))
		{
			this.m_pinUpdateRequired = true;
		}
	}

	// Token: 0x0600090F RID: 2319 RVA: 0x0004F594 File Offset: 0x0004D794
	private bool UpdatedMap(Vector3 centerPoint)
	{
		float num = this.m_previousMapCenter.magnitude - centerPoint.magnitude;
		if (num > 0.01f || num < -0.01f)
		{
			this.m_previousMapCenter = centerPoint;
			return true;
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			if (this.m_previousLargeZoom != this.m_largeZoom)
			{
				this.m_previousLargeZoom = this.m_largeZoom;
				return true;
			}
		}
		else if (this.m_previousSmallZoom != this.m_smallZoom)
		{
			this.m_previousSmallZoom = this.m_smallZoom;
			return true;
		}
		return false;
	}

	// Token: 0x06000910 RID: 2320 RVA: 0x0004F610 File Offset: 0x0004D810
	private void UpdateDynamicPins(float dt)
	{
		this.UpdateProfilePins();
		this.UpdateShoutPins();
		this.UpdatePingPins();
		this.UpdatePlayerPins(dt);
		this.UpdateLocationPins(dt);
		this.UpdateEventPin(dt);
	}

	// Token: 0x06000911 RID: 2321 RVA: 0x0004F63C File Offset: 0x0004D83C
	private void UpdateProfilePins()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		playerProfile.HaveDeathPoint();
		if (this.m_deathPin != null)
		{
			this.RemovePin(this.m_deathPin);
			this.m_deathPin = null;
		}
		if (playerProfile.HaveCustomSpawnPoint())
		{
			if (this.m_spawnPointPin == null)
			{
				this.m_spawnPointPin = this.AddPin(playerProfile.GetCustomSpawnPoint(), Minimap.PinType.Bed, "", false, false, 0L, default(PlatformUserID));
			}
			this.m_spawnPointPin.m_pos = playerProfile.GetCustomSpawnPoint();
			return;
		}
		if (this.m_spawnPointPin != null)
		{
			this.RemovePin(this.m_spawnPointPin);
			this.m_spawnPointPin = null;
		}
	}

	// Token: 0x06000912 RID: 2322 RVA: 0x0004F6D8 File Offset: 0x0004D8D8
	private void UpdateEventPin(float dt)
	{
		if (Time.time - this.m_updateEventTime < 1f)
		{
			return;
		}
		this.m_updateEventTime = Time.time;
		RandomEvent currentRandomEvent = RandEventSystem.instance.GetCurrentRandomEvent();
		if (currentRandomEvent != null)
		{
			if (this.m_randEventAreaPin == null)
			{
				this.m_randEventAreaPin = this.AddPin(currentRandomEvent.m_pos, Minimap.PinType.EventArea, "", false, false, 0L, default(PlatformUserID));
				this.m_randEventAreaPin.m_worldSize = currentRandomEvent.m_eventRange * 2f;
				this.m_randEventAreaPin.m_worldSize *= 0.9f;
			}
			if (this.m_randEventPin == null)
			{
				this.m_randEventPin = this.AddPin(currentRandomEvent.m_pos, Minimap.PinType.RandomEvent, "", false, false, 0L, default(PlatformUserID));
				this.m_randEventPin.m_animate = true;
				this.m_randEventPin.m_doubleSize = true;
			}
			this.m_randEventAreaPin.m_pos = currentRandomEvent.m_pos;
			this.m_randEventPin.m_pos = currentRandomEvent.m_pos;
			this.m_randEventPin.m_name = Localization.instance.Localize(currentRandomEvent.GetHudText());
			return;
		}
		if (this.m_randEventPin != null)
		{
			this.RemovePin(this.m_randEventPin);
			this.m_randEventPin = null;
		}
		if (this.m_randEventAreaPin != null)
		{
			this.RemovePin(this.m_randEventAreaPin);
			this.m_randEventAreaPin = null;
		}
	}

	// Token: 0x06000913 RID: 2323 RVA: 0x0004F82C File Offset: 0x0004DA2C
	private void UpdateLocationPins(float dt)
	{
		this.m_updateLocationsTimer -= dt;
		if (this.m_updateLocationsTimer <= 0f)
		{
			this.m_updateLocationsTimer = 5f;
			Dictionary<Vector3, string> dictionary = new Dictionary<Vector3, string>();
			ZoneSystem.instance.GetLocationIcons(dictionary);
			bool flag = false;
			while (!flag)
			{
				flag = true;
				foreach (KeyValuePair<Vector3, Minimap.PinData> keyValuePair in this.m_locationPins)
				{
					if (!dictionary.ContainsKey(keyValuePair.Key))
					{
						ZLog.DevLog("Minimap: Removing location " + keyValuePair.Value.m_name);
						this.RemovePin(keyValuePair.Value);
						this.m_locationPins.Remove(keyValuePair.Key);
						flag = false;
						break;
					}
				}
			}
			foreach (KeyValuePair<Vector3, string> keyValuePair2 in dictionary)
			{
				if (!this.m_locationPins.ContainsKey(keyValuePair2.Key))
				{
					Sprite locationIcon = this.GetLocationIcon(keyValuePair2.Value);
					if (locationIcon)
					{
						Minimap.PinData pinData = this.AddPin(keyValuePair2.Key, Minimap.PinType.None, "", false, false, 0L, default(PlatformUserID));
						pinData.m_icon = locationIcon;
						pinData.m_doubleSize = true;
						this.m_locationPins.Add(keyValuePair2.Key, pinData);
						ZLog.Log("Minimap: Adding unique location " + keyValuePair2.Key.ToString());
					}
				}
			}
		}
	}

	// Token: 0x06000914 RID: 2324 RVA: 0x0004F9E8 File Offset: 0x0004DBE8
	private Sprite GetLocationIcon(string name)
	{
		foreach (Minimap.LocationSpriteData locationSpriteData in this.m_locationIcons)
		{
			if (locationSpriteData.m_name == name)
			{
				return locationSpriteData.m_icon;
			}
		}
		return null;
	}

	// Token: 0x06000915 RID: 2325 RVA: 0x0004FA50 File Offset: 0x0004DC50
	private void UpdatePlayerPins(float dt)
	{
		this.m_tempPlayerInfo.Clear();
		ZNet.instance.GetOtherPublicPlayers(this.m_tempPlayerInfo);
		if (this.m_playerPins.Count != this.m_tempPlayerInfo.Count)
		{
			foreach (Minimap.PinData pin in this.m_playerPins)
			{
				this.RemovePin(pin);
			}
			this.m_playerPins.Clear();
			foreach (ZNet.PlayerInfo playerInfo in this.m_tempPlayerInfo)
			{
				Minimap.PinData item = this.AddPin(Vector3.zero, Minimap.PinType.Player, "", false, false, 0L, default(PlatformUserID));
				this.m_playerPins.Add(item);
			}
		}
		for (int i = 0; i < this.m_tempPlayerInfo.Count; i++)
		{
			Minimap.PinData pinData = this.m_playerPins[i];
			ZNet.PlayerInfo playerInfo2 = this.m_tempPlayerInfo[i];
			if (pinData.m_name == playerInfo2.m_name)
			{
				Vector3 vector = Vector3.MoveTowards(pinData.m_pos, playerInfo2.m_position, 200f * dt);
				if (vector != pinData.m_pos)
				{
					this.m_pinUpdateRequired = true;
				}
				pinData.m_pos = vector;
			}
			else
			{
				pinData.m_name = CensorShittyWords.FilterUGC(playerInfo2.m_name, UGCType.CharacterName, playerInfo2.m_characterID.UserID);
				if (playerInfo2.m_position != pinData.m_pos)
				{
					this.m_pinUpdateRequired = true;
				}
				pinData.m_pos = playerInfo2.m_position;
				if (pinData.m_NamePinData == null)
				{
					pinData.m_NamePinData = new Minimap.PinNameData(pinData);
					this.CreateMapNamePin(pinData, this.m_pinNameRootLarge);
				}
			}
		}
	}

	// Token: 0x06000916 RID: 2326 RVA: 0x0004FC4C File Offset: 0x0004DE4C
	private void UpdatePingPins()
	{
		this.m_tempShouts.Clear();
		Chat.instance.GetPingWorldTexts(this.m_tempShouts);
		if (this.m_pingPins.Count != this.m_tempShouts.Count)
		{
			foreach (Minimap.PinData pin in this.m_pingPins)
			{
				this.RemovePin(pin);
			}
			this.m_pingPins.Clear();
			foreach (Chat.WorldTextInstance worldTextInstance in this.m_tempShouts)
			{
				Minimap.PinData pinData = this.AddPin(Vector3.zero, Minimap.PinType.Ping, worldTextInstance.m_name + ": " + worldTextInstance.m_text, false, false, 0L, default(PlatformUserID));
				pinData.m_doubleSize = true;
				pinData.m_animate = true;
				this.m_pingPins.Add(pinData);
			}
		}
		if (this.m_pingPins.Count > 0)
		{
			this.m_pinUpdateRequired = true;
		}
		for (int i = 0; i < this.m_tempShouts.Count; i++)
		{
			Minimap.PinData pinData2 = this.m_pingPins[i];
			Chat.WorldTextInstance worldTextInstance2 = this.m_tempShouts[i];
			pinData2.m_pos = worldTextInstance2.m_position;
			pinData2.m_name = worldTextInstance2.m_name + ": " + worldTextInstance2.m_text;
		}
	}

	// Token: 0x06000917 RID: 2327 RVA: 0x0004FDE0 File Offset: 0x0004DFE0
	private void UpdateShoutPins()
	{
		this.m_tempShouts.Clear();
		Chat.instance.GetShoutWorldTexts(this.m_tempShouts);
		if (this.m_shoutPins.Count != this.m_tempShouts.Count)
		{
			foreach (Minimap.PinData pin in this.m_shoutPins)
			{
				this.RemovePin(pin);
			}
			this.m_shoutPins.Clear();
			foreach (Chat.WorldTextInstance worldTextInstance in this.m_tempShouts)
			{
				Minimap.PinData pinData = this.AddPin(Vector3.zero, Minimap.PinType.Shout, worldTextInstance.m_name + ": " + worldTextInstance.m_text, false, false, 0L, default(PlatformUserID));
				pinData.m_doubleSize = true;
				pinData.m_animate = true;
				this.m_shoutPins.Add(pinData);
			}
		}
		if (this.m_shoutPins.Count > 0)
		{
			this.m_pinUpdateRequired = true;
		}
		for (int i = 0; i < this.m_tempShouts.Count; i++)
		{
			Minimap.PinData pinData2 = this.m_shoutPins[i];
			Chat.WorldTextInstance worldTextInstance2 = this.m_tempShouts[i];
			pinData2.m_pos = worldTextInstance2.m_position;
			pinData2.m_name = worldTextInstance2.m_name + ": " + worldTextInstance2.m_text;
		}
	}

	// Token: 0x06000918 RID: 2328 RVA: 0x0004FF74 File Offset: 0x0004E174
	private void UpdatePins()
	{
		RawImage rawImage = (this.m_mode == Minimap.MapMode.Large) ? this.m_mapImageLarge : this.m_mapImageSmall;
		Rect uvRect = rawImage.uvRect;
		Rect rect = rawImage.rectTransform.rect;
		float num = (this.m_mode == Minimap.MapMode.Large) ? this.m_pinSizeLarge : this.m_pinSizeSmall;
		if (this.m_mode != Minimap.MapMode.Large)
		{
			float smallZoom = this.m_smallZoom;
		}
		else
		{
			float largeZoom = this.m_largeZoom;
		}
		Color color = new Color(0.7f, 0.7f, 0.7f, 0.8f * this.m_sharedMapDataFade);
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			RectTransform rectTransform = (this.m_mode == Minimap.MapMode.Large) ? this.m_pinRootLarge : this.m_pinRootSmall;
			RectTransform root = (this.m_mode == Minimap.MapMode.Large) ? this.m_pinNameRootLarge : this.m_pinNameRootSmall;
			if (!this.IsPointVisible(pinData.m_pos, rawImage) || !this.m_visibleIconTypes[(int)pinData.m_type] || (this.m_sharedMapDataFade <= 0f && pinData.m_ownerID != 0L))
			{
				this.DestroyPinMarker(pinData);
			}
			else
			{
				if (pinData.m_uiElement == null || pinData.m_uiElement.parent != rectTransform)
				{
					this.DestroyPinMarker(pinData);
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_pinPrefab, rectTransform);
					pinData.m_iconElement = gameObject.GetComponent<Image>();
					pinData.m_iconElement.sprite = pinData.m_icon;
					pinData.m_uiElement = (gameObject.transform as RectTransform);
					float size = pinData.m_doubleSize ? (num * 2f) : num;
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
					pinData.m_checkedElement = gameObject.transform.Find("Checked").gameObject;
				}
				if (pinData.m_NamePinData != null && pinData.m_NamePinData.PinNameGameObject == null)
				{
					this.CreateMapNamePin(pinData, root);
				}
				if (pinData.m_ownerID != 0L && this.m_sharedMapHint != null)
				{
					this.m_sharedMapHint.gameObject.SetActive(true);
				}
				Color color2 = (pinData.m_ownerID != 0L) ? color : Color.white;
				pinData.m_iconElement.color = color2;
				if (pinData.m_NamePinData != null && pinData.m_NamePinData.PinNameText.color != color2)
				{
					pinData.m_NamePinData.PinNameText.color = color2;
				}
				float mx;
				float my;
				this.WorldToMapPoint(pinData.m_pos, out mx, out my);
				Vector2 anchoredPosition = this.MapPointToLocalGuiPos(mx, my, uvRect, rect);
				pinData.m_uiElement.anchoredPosition = anchoredPosition;
				if (pinData.m_NamePinData != null)
				{
					pinData.m_NamePinData.PinNameRectTransform.anchoredPosition = anchoredPosition;
				}
				if (pinData.m_animate)
				{
					float num2 = pinData.m_doubleSize ? (num * 2f) : num;
					num2 *= 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num2);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
				}
				if (pinData.m_worldSize > 0f)
				{
					Vector2 size2 = new Vector2(pinData.m_worldSize / this.m_pixelSize / (float)this.m_textureSize, pinData.m_worldSize / this.m_pixelSize / (float)this.m_textureSize);
					Vector2 vector = this.MapSizeToLocalGuiSize(size2, rawImage);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vector.x);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, vector.y);
				}
				if (pinData.m_checkedElement.activeInHierarchy != pinData.m_checked)
				{
					pinData.m_checkedElement.SetActive(pinData.m_checked);
				}
				if (pinData.m_name.Length > 0 && this.m_mode == Minimap.MapMode.Large && this.m_largeZoom < this.m_showNamesZoom && pinData.m_NamePinData != null)
				{
					pinData.m_NamePinData.PinNameGameObject.SetActive(true);
				}
				else if (pinData.m_NamePinData != null)
				{
					pinData.m_NamePinData.PinNameGameObject.SetActive(false);
				}
			}
		}
	}

	// Token: 0x06000919 RID: 2329 RVA: 0x000503D8 File Offset: 0x0004E5D8
	private void DestroyPinMarker(Minimap.PinData pin)
	{
		if (pin.m_uiElement != null)
		{
			UnityEngine.Object.Destroy(pin.m_uiElement.gameObject);
			pin.m_uiElement = null;
		}
		if (pin.m_NamePinData != null)
		{
			pin.m_NamePinData.DestroyMapMarker();
		}
	}

	// Token: 0x0600091A RID: 2330 RVA: 0x00050414 File Offset: 0x0004E614
	private void UpdateWindMarker()
	{
		Quaternion quaternion = Quaternion.LookRotation(EnvMan.instance.GetWindDir());
		this.m_windMarker.rotation = Quaternion.Euler(0f, 0f, -quaternion.eulerAngles.y);
	}

	// Token: 0x0600091B RID: 2331 RVA: 0x00050458 File Offset: 0x0004E658
	private void UpdatePlayerMarker(Player player, Quaternion playerRot)
	{
		Vector3 position = player.transform.position;
		Vector3 eulerAngles = playerRot.eulerAngles;
		this.m_smallMarker.rotation = Quaternion.Euler(0f, 0f, -eulerAngles.y);
		if (this.m_mode == Minimap.MapMode.Large && this.IsPointVisible(position, this.m_mapImageLarge))
		{
			this.m_largeMarker.gameObject.SetActive(true);
			this.m_largeMarker.rotation = this.m_smallMarker.rotation;
			float mx;
			float my;
			this.WorldToMapPoint(position, out mx, out my);
			Vector2 anchoredPosition = this.MapPointToLocalGuiPos(mx, my, this.m_mapImageLarge);
			this.m_largeMarker.anchoredPosition = anchoredPosition;
		}
		else
		{
			this.m_largeMarker.gameObject.SetActive(false);
		}
		Ship controlledShip = player.GetControlledShip();
		if (controlledShip)
		{
			this.m_smallShipMarker.gameObject.SetActive(true);
			Vector3 eulerAngles2 = controlledShip.transform.rotation.eulerAngles;
			this.m_smallShipMarker.rotation = Quaternion.Euler(0f, 0f, -eulerAngles2.y);
			if (this.m_mode == Minimap.MapMode.Large)
			{
				this.m_largeShipMarker.gameObject.SetActive(true);
				Vector3 position2 = controlledShip.transform.position;
				float mx2;
				float my2;
				this.WorldToMapPoint(position2, out mx2, out my2);
				Vector2 anchoredPosition2 = this.MapPointToLocalGuiPos(mx2, my2, this.m_mapImageLarge);
				this.m_largeShipMarker.anchoredPosition = anchoredPosition2;
				this.m_largeShipMarker.rotation = this.m_smallShipMarker.rotation;
				return;
			}
		}
		else
		{
			this.m_smallShipMarker.gameObject.SetActive(false);
			this.m_largeShipMarker.gameObject.SetActive(false);
		}
	}

	// Token: 0x0600091C RID: 2332 RVA: 0x000505FE File Offset: 0x0004E7FE
	private Vector2 MapPointToLocalGuiPos(float mx, float my, RawImage img)
	{
		return this.MapPointToLocalGuiPos(mx, my, img.uvRect, img.rectTransform.rect);
	}

	// Token: 0x0600091D RID: 2333 RVA: 0x0005061C File Offset: 0x0004E81C
	private Vector2 MapPointToLocalGuiPos(float mx, float my, Rect uvRect, Rect transformRect)
	{
		Vector2 result = default(Vector2);
		result.x = (mx - uvRect.xMin) / uvRect.width;
		result.y = (my - uvRect.yMin) / uvRect.height;
		result.x *= transformRect.width;
		result.y *= transformRect.height;
		return result;
	}

	// Token: 0x0600091E RID: 2334 RVA: 0x00050688 File Offset: 0x0004E888
	private Vector2 MapSizeToLocalGuiSize(Vector2 size, RawImage img)
	{
		size.x /= img.uvRect.width;
		size.y /= img.uvRect.height;
		return new Vector2(size.x * img.rectTransform.rect.width, size.y * img.rectTransform.rect.height);
	}

	// Token: 0x0600091F RID: 2335 RVA: 0x00050700 File Offset: 0x0004E900
	private bool IsPointVisible(Vector3 p, RawImage map)
	{
		float num;
		float num2;
		this.WorldToMapPoint(p, out num, out num2);
		return num > map.uvRect.xMin && num < map.uvRect.xMax && num2 > map.uvRect.yMin && num2 < map.uvRect.yMax;
	}

	// Token: 0x06000920 RID: 2336 RVA: 0x00050760 File Offset: 0x0004E960
	public void ExploreAll()
	{
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				this.Explore(j, i);
			}
		}
		this.m_fogTexture.Apply();
	}

	// Token: 0x06000921 RID: 2337 RVA: 0x000507A4 File Offset: 0x0004E9A4
	private void WorldToMapPoint(Vector3 p, out float mx, out float my)
	{
		int num = this.m_textureSize / 2;
		mx = p.x / this.m_pixelSize + (float)num;
		my = p.z / this.m_pixelSize + (float)num;
		mx /= (float)this.m_textureSize;
		my /= (float)this.m_textureSize;
	}

	// Token: 0x06000922 RID: 2338 RVA: 0x000507F8 File Offset: 0x0004E9F8
	private Vector3 MapPointToWorld(float mx, float my)
	{
		int num = this.m_textureSize / 2;
		mx *= (float)this.m_textureSize;
		my *= (float)this.m_textureSize;
		mx -= (float)num;
		my -= (float)num;
		mx *= this.m_pixelSize;
		my *= this.m_pixelSize;
		return new Vector3(mx, 0f, my);
	}

	// Token: 0x06000923 RID: 2339 RVA: 0x00050850 File Offset: 0x0004EA50
	private void WorldToPixel(Vector3 p, out int px, out int py)
	{
		int num = this.m_textureSize / 2;
		px = Mathf.RoundToInt(p.x / this.m_pixelSize + (float)num);
		py = Mathf.RoundToInt(p.z / this.m_pixelSize + (float)num);
	}

	// Token: 0x06000924 RID: 2340 RVA: 0x00050894 File Offset: 0x0004EA94
	private void UpdateExplore(float dt, Player player)
	{
		this.m_exploreTimer += Time.deltaTime;
		if (this.m_exploreTimer > this.m_exploreInterval)
		{
			this.m_exploreTimer = 0f;
			this.Explore(player.transform.position, this.m_exploreRadius);
		}
	}

	// Token: 0x06000925 RID: 2341 RVA: 0x000508E4 File Offset: 0x0004EAE4
	private void Explore(Vector3 p, float radius)
	{
		int num = (int)Mathf.Ceil(radius / this.m_pixelSize);
		bool flag = false;
		int num2;
		int num3;
		this.WorldToPixel(p, out num2, out num3);
		for (int i = num3 - num; i <= num3 + num; i++)
		{
			for (int j = num2 - num; j <= num2 + num; j++)
			{
				if (j >= 0 && i >= 0 && j < this.m_textureSize && i < this.m_textureSize && new Vector2((float)(j - num2), (float)(i - num3)).magnitude <= (float)num && this.Explore(j, i))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.m_fogTexture.Apply();
		}
	}

	// Token: 0x06000926 RID: 2342 RVA: 0x0005098C File Offset: 0x0004EB8C
	private bool Explore(int x, int y)
	{
		if (this.m_explored[y * this.m_textureSize + x])
		{
			return false;
		}
		Color pixel = this.m_fogTexture.GetPixel(x, y);
		pixel.r = 0f;
		this.m_fogTexture.SetPixel(x, y, pixel);
		this.m_explored[y * this.m_textureSize + x] = true;
		return true;
	}

	// Token: 0x06000927 RID: 2343 RVA: 0x000509EC File Offset: 0x0004EBEC
	private void ResetAndExplore(byte[] explored, byte[] exploredOthers)
	{
		this.m_sharedMapHint.gameObject.SetActive(false);
		int num = explored.Length;
		Color[] pixels = this.m_fogTexture.GetPixels();
		if (num != pixels.Length || num != exploredOthers.Length)
		{
			ZLog.LogError("Dimension mismatch for exploring mipmap");
			return;
		}
		for (int i = 0; i < num; i++)
		{
			pixels[i] = Color.white;
			if (explored[i] != 0)
			{
				pixels[i].r = 0f;
				this.m_explored[i] = true;
			}
			else
			{
				this.m_explored[i] = false;
			}
			if (exploredOthers[i] != 0)
			{
				pixels[i].g = 0f;
				this.m_exploredOthers[i] = true;
			}
			else
			{
				this.m_exploredOthers[i] = false;
			}
		}
		this.m_fogTexture.SetPixels(pixels);
	}

	// Token: 0x06000928 RID: 2344 RVA: 0x00050AAC File Offset: 0x0004ECAC
	private bool ExploreOthers(int x, int y)
	{
		if (this.m_exploredOthers[y * this.m_textureSize + x])
		{
			return false;
		}
		Color pixel = this.m_fogTexture.GetPixel(x, y);
		pixel.g = 0f;
		this.m_fogTexture.SetPixel(x, y, pixel);
		this.m_exploredOthers[y * this.m_textureSize + x] = true;
		if (this.m_sharedMapHint != null)
		{
			this.m_sharedMapHint.gameObject.SetActive(true);
		}
		return true;
	}

	// Token: 0x06000929 RID: 2345 RVA: 0x00050B28 File Offset: 0x0004ED28
	private bool IsExplored(Vector3 worldPos)
	{
		int num;
		int num2;
		this.WorldToPixel(worldPos, out num, out num2);
		return num >= 0 && num < this.m_textureSize && num2 >= 0 && num2 < this.m_textureSize && (this.m_explored[num2 * this.m_textureSize + num] || this.m_exploredOthers[num2 * this.m_textureSize + num]);
	}

	// Token: 0x0600092A RID: 2346 RVA: 0x00050B82 File Offset: 0x0004ED82
	private float GetHeight(int x, int y)
	{
		return this.m_heightTexture.GetPixel(x, y).r;
	}

	// Token: 0x0600092B RID: 2347 RVA: 0x00050B98 File Offset: 0x0004ED98
	private void GenerateWorldMap()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		Minimap.DeleteMapTextureData(ZNet.World.m_name);
		int num = this.m_textureSize / 2;
		float num2 = this.m_pixelSize / 2f;
		Color32[] array = new Color32[this.m_textureSize * this.m_textureSize];
		Color32[] array2 = new Color32[this.m_textureSize * this.m_textureSize];
		Color[] array3 = new Color[this.m_textureSize * this.m_textureSize];
		Color32[] array4 = new Color32[this.m_textureSize * this.m_textureSize];
		float num3 = 127.5f;
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				float wx = (float)(j - num) * this.m_pixelSize + num2;
				float wy = (float)(i - num) * this.m_pixelSize + num2;
				Heightmap.Biome biome = WorldGenerator.instance.GetBiome(wx, wy, 0.02f, false);
				Color color;
				float biomeHeight = WorldGenerator.instance.GetBiomeHeight(biome, wx, wy, out color, false);
				array[i * this.m_textureSize + j] = this.GetPixelColor(biome);
				array2[i * this.m_textureSize + j] = this.GetMaskColor(wx, wy, biomeHeight, biome);
				array3[i * this.m_textureSize + j].r = biomeHeight;
				int num4 = Mathf.Clamp((int)(biomeHeight * num3), 0, 65025);
				byte r = (byte)(num4 >> 8);
				byte g = (byte)(num4 & 255);
				array4[i * this.m_textureSize + j] = new Color32(r, g, 0, byte.MaxValue);
			}
		}
		this.m_forestMaskTexture.SetPixels32(array2);
		this.m_forestMaskTexture.Apply();
		this.m_mapTexture.SetPixels32(array);
		this.m_mapTexture.Apply();
		this.m_heightTexture.SetPixels(array3);
		this.m_heightTexture.Apply();
		Texture2D texture2D = new Texture2D(this.m_textureSize, this.m_textureSize);
		texture2D.SetPixels32(array4);
		texture2D.Apply();
		ZLog.Log("Generating new world minimap done [" + stopwatch.ElapsedMilliseconds.ToString() + "ms]");
		if (FileHelpers.LocalStorageSupport == LocalStorageSupport.Supported)
		{
			this.SaveMapTextureDataToDisk(this.m_forestMaskTexture, this.m_mapTexture, texture2D);
		}
	}

	// Token: 0x0600092C RID: 2348 RVA: 0x00050DF4 File Offset: 0x0004EFF4
	public static void DeleteMapTextureData(string worldName)
	{
		string rootPath = World.GetWorldSavePath(FileHelpers.FileSource.Local) + "/" + worldName;
		string completeTexturePath = Minimap.GetCompleteTexturePath(rootPath, "forestMaskTexCache");
		string completeTexturePath2 = Minimap.GetCompleteTexturePath(rootPath, "mapTexCache");
		string completeTexturePath3 = Minimap.GetCompleteTexturePath(rootPath, "heightTexCache");
		if (File.Exists(completeTexturePath))
		{
			File.Delete(completeTexturePath);
		}
		if (File.Exists(completeTexturePath2))
		{
			File.Delete(completeTexturePath2);
		}
		if (File.Exists(completeTexturePath3))
		{
			File.Delete(completeTexturePath3);
		}
	}

	// Token: 0x0600092D RID: 2349 RVA: 0x00050E5F File Offset: 0x0004F05F
	private static string GetCompleteTexturePath(string rootPath, string maskTextureName)
	{
		return rootPath + "_" + maskTextureName;
	}

	// Token: 0x0600092E RID: 2350 RVA: 0x00050E70 File Offset: 0x0004F070
	private void SaveMapTextureDataToDisk(Texture2D forestMaskTexture, Texture2D mapTexture, Texture2D heightTexture)
	{
		if (string.IsNullOrEmpty(this.m_forestMaskTexturePath))
		{
			return;
		}
		File.WriteAllBytes(this.m_forestMaskTexturePath, forestMaskTexture.EncodeToPNG());
		File.WriteAllBytes(this.m_mapTexturePath, mapTexture.EncodeToPNG());
		File.WriteAllBytes(this.m_heightTexturePath, heightTexture.EncodeToPNG());
	}

	// Token: 0x0600092F RID: 2351 RVA: 0x00050EC0 File Offset: 0x0004F0C0
	private Color GetMaskColor(float wx, float wy, float height, Heightmap.Biome biome)
	{
		Color result = new Color(0f, 0f, 0f, 0f);
		if (height < 30f)
		{
			result.b = Mathf.Clamp01(WorldGenerator.GetAshlandsOceanGradient(wx, wy));
			return result;
		}
		if (biome == Heightmap.Biome.Meadows)
		{
			result.r = (float)(WorldGenerator.InForest(new Vector3(wx, 0f, wy)) ? 1 : 0);
		}
		else if (biome == Heightmap.Biome.Plains)
		{
			result.r = (float)((WorldGenerator.GetForestFactor(new Vector3(wx, 0f, wy)) < 0.8f) ? 1 : 0);
		}
		else if (biome == Heightmap.Biome.BlackForest)
		{
			result.r = 1f;
		}
		else if (biome == Heightmap.Biome.Mistlands)
		{
			float forestFactor = WorldGenerator.GetForestFactor(new Vector3(wx, 0f, wy));
			result.g = 1f - Utils.SmoothStep(1.1f, 1.3f, forestFactor);
		}
		else if (biome == Heightmap.Biome.AshLands)
		{
			Color color;
			WorldGenerator.instance.GetAshlandsHeight(wx, wy, out color, true);
			result.b = color.a;
		}
		return result;
	}

	// Token: 0x06000930 RID: 2352 RVA: 0x00050FCC File Offset: 0x0004F1CC
	private Color GetPixelColor(Heightmap.Biome biome)
	{
		if (biome <= Heightmap.Biome.Plains)
		{
			switch (biome)
			{
			case Heightmap.Biome.Meadows:
				return this.m_meadowsColor;
			case Heightmap.Biome.Swamp:
				return this.m_swampColor;
			case Heightmap.Biome.Meadows | Heightmap.Biome.Swamp:
				break;
			case Heightmap.Biome.Mountain:
				return this.m_mountainColor;
			default:
				if (biome == Heightmap.Biome.BlackForest)
				{
					return this.m_blackforestColor;
				}
				if (biome == Heightmap.Biome.Plains)
				{
					return this.m_heathColor;
				}
				break;
			}
		}
		else if (biome <= Heightmap.Biome.DeepNorth)
		{
			if (biome == Heightmap.Biome.AshLands)
			{
				return this.m_ashlandsColor;
			}
			if (biome == Heightmap.Biome.DeepNorth)
			{
				return this.m_deepnorthColor;
			}
		}
		else
		{
			if (biome == Heightmap.Biome.Ocean)
			{
				return Color.white;
			}
			if (biome == Heightmap.Biome.Mistlands)
			{
				return this.m_mistlandsColor;
			}
		}
		return Color.white;
	}

	// Token: 0x06000931 RID: 2353 RVA: 0x0005107C File Offset: 0x0004F27C
	private void LoadMapData()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		if (playerProfile.GetMapData() != null)
		{
			this.SetMapData(playerProfile.GetMapData());
		}
	}

	// Token: 0x06000932 RID: 2354 RVA: 0x000510A8 File Offset: 0x0004F2A8
	public void SaveMapData()
	{
		Game.instance.GetPlayerProfile().SetMapData(this.GetMapData());
	}

	// Token: 0x06000933 RID: 2355 RVA: 0x000510C0 File Offset: 0x0004F2C0
	private byte[] GetMapData()
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(Minimap.MAPVERSION);
		ZPackage zpackage2 = new ZPackage();
		zpackage2.Write(this.m_textureSize);
		for (int i = 0; i < this.m_explored.Length; i++)
		{
			zpackage2.Write(this.m_explored[i]);
		}
		for (int j = 0; j < this.m_explored.Length; j++)
		{
			zpackage2.Write(this.m_exploredOthers[j]);
		}
		int num = 0;
		using (List<Minimap.PinData>.Enumerator enumerator = this.m_pins.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_save)
				{
					num++;
				}
			}
		}
		zpackage2.Write(num);
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_save)
			{
				zpackage2.Write(pinData.m_name);
				zpackage2.Write(pinData.m_pos);
				zpackage2.Write((int)pinData.m_type);
				zpackage2.Write(pinData.m_checked);
				zpackage2.Write(pinData.m_ownerID);
				zpackage2.Write(pinData.m_author.ToString());
			}
		}
		zpackage2.Write(ZNet.instance.IsReferencePositionPublic());
		int num2 = zpackage2.Size();
		zpackage.WriteCompressed(zpackage2);
		ZLog.Log(string.Concat(new string[]
		{
			"Minimap: compressed mapData ",
			num2.ToString(),
			" => ",
			zpackage.Size().ToString(),
			" bytes"
		}));
		return zpackage.GetArray();
	}

	// Token: 0x06000934 RID: 2356 RVA: 0x00051298 File Offset: 0x0004F498
	private void SetMapData(byte[] data)
	{
		ZPackage zpackage = new ZPackage(data);
		int num = zpackage.ReadInt();
		if (num >= 7)
		{
			int num2 = zpackage.Size();
			zpackage = zpackage.ReadCompressedPackage();
			ZLog.Log(string.Concat(new string[]
			{
				"Minimap: unpacking compressed mapData ",
				num2.ToString(),
				" => ",
				zpackage.Size().ToString(),
				" bytes"
			}));
		}
		int num3 = zpackage.ReadInt();
		if (this.m_textureSize != num3)
		{
			string str = "Missmatching mapsize ";
			Texture2D mapTexture = this.m_mapTexture;
			ZLog.LogWarning(str + ((mapTexture != null) ? mapTexture.ToString() : null) + " vs " + num3.ToString());
			return;
		}
		if (num >= 5)
		{
			byte[] explored = zpackage.ReadByteArray(this.m_explored.Length);
			byte[] exploredOthers = zpackage.ReadByteArray(this.m_exploredOthers.Length);
			this.ResetAndExplore(explored, exploredOthers);
		}
		else
		{
			this.Reset();
			for (int i = 0; i < this.m_explored.Length; i++)
			{
				if (zpackage.ReadBool())
				{
					int x = i % num3;
					int y = i / num3;
					this.Explore(x, y);
				}
			}
		}
		if (num >= 2)
		{
			int num4 = zpackage.ReadInt();
			this.ClearPins();
			for (int j = 0; j < num4; j++)
			{
				string name = zpackage.ReadString();
				Vector3 pos = zpackage.ReadVector3();
				Minimap.PinType type = (Minimap.PinType)zpackage.ReadInt();
				bool isChecked = num >= 3 && zpackage.ReadBool();
				long ownerID = (num >= 6) ? zpackage.ReadLong() : 0L;
				string text = (num >= 8) ? zpackage.ReadString() : "";
				this.AddPin(pos, type, name, true, isChecked, ownerID, string.IsNullOrEmpty(text) ? PlatformUserID.None : new PlatformUserID(text));
			}
		}
		if (num >= 4)
		{
			bool publicReferencePosition = zpackage.ReadBool();
			ZNet.instance.SetPublicReferencePosition(publicReferencePosition);
		}
		this.m_fogTexture.Apply();
	}

	// Token: 0x06000935 RID: 2357 RVA: 0x00051470 File Offset: 0x0004F670
	public bool RemovePin(Vector3 pos, float radius)
	{
		Minimap.PinData closestPin = this.GetClosestPin(pos, radius, true);
		if (closestPin != null)
		{
			this.RemovePin(closestPin);
			return true;
		}
		return false;
	}

	// Token: 0x06000936 RID: 2358 RVA: 0x00051494 File Offset: 0x0004F694
	private bool HavePinInRange(Vector3 pos, float radius)
	{
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_save && Utils.DistanceXZ(pos, pinData.m_pos) < radius)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000937 RID: 2359 RVA: 0x00051500 File Offset: 0x0004F700
	private Minimap.PinData GetClosestPin(Vector3 pos, float radius, bool mustBeVisible = true)
	{
		Minimap.PinData pinData = null;
		float num = 999999f;
		foreach (Minimap.PinData pinData2 in this.m_pins)
		{
			if (pinData2.m_save && ((pinData2.m_uiElement && pinData2.m_uiElement.gameObject.activeInHierarchy) || !mustBeVisible))
			{
				float num2 = Utils.DistanceXZ(pos, pinData2.m_pos);
				if (num2 < radius && (num2 < num || pinData == null))
				{
					pinData = pinData2;
					num = num2;
				}
			}
		}
		return pinData;
	}

	// Token: 0x06000938 RID: 2360 RVA: 0x000515A4 File Offset: 0x0004F7A4
	public void RemovePin(Minimap.PinData pin)
	{
		this.m_pinUpdateRequired = true;
		this.DestroyPinMarker(pin);
		this.m_pins.Remove(pin);
	}

	// Token: 0x06000939 RID: 2361 RVA: 0x000515C1 File Offset: 0x0004F7C1
	public void ShowPointOnMap(Vector3 point)
	{
		this.inputDelay = 0.5f;
		if (Player.m_localPlayer == null)
		{
			return;
		}
		this.SetMapMode(Minimap.MapMode.Large);
		this.m_mapOffset = point - Player.m_localPlayer.transform.position;
	}

	// Token: 0x0600093A RID: 2362 RVA: 0x00051600 File Offset: 0x0004F800
	public bool DiscoverLocation(Vector3 pos, Minimap.PinType type, string name, bool showMap)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		if (this.HaveSimilarPin(pos, type, name, true))
		{
			if (showMap)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_pin_exist", 0, null);
				this.ShowPointOnMap(pos);
			}
			return false;
		}
		Sprite sprite = this.GetSprite(type);
		this.AddPin(pos, type, name, true, false, 0L, default(PlatformUserID));
		if (showMap)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_pin_added: " + name, 0, sprite);
			this.ShowPointOnMap(pos);
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_pin_added: " + name, 0, sprite);
		}
		return true;
	}

	// Token: 0x0600093B RID: 2363 RVA: 0x000516A4 File Offset: 0x0004F8A4
	private bool HaveSimilarPin(Vector3 pos, Minimap.PinType type, string name, bool save)
	{
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_name == name && pinData.m_type == type && pinData.m_save == save && Utils.DistanceXZ(pos, pinData.m_pos) < 1f)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x0600093C RID: 2364 RVA: 0x0005172C File Offset: 0x0004F92C
	public Minimap.PinData AddPin(Vector3 pos, Minimap.PinType type, string name, bool save, bool isChecked, long ownerID = 0L, PlatformUserID author = default(PlatformUserID))
	{
		if (type >= (Minimap.PinType)this.m_visibleIconTypes.Length || type < Minimap.PinType.Icon0)
		{
			ZLog.LogWarning(string.Format("Trying to add invalid pin type: {0}", type));
			type = Minimap.PinType.Icon3;
		}
		if (name == null)
		{
			name = "";
		}
		Minimap.PinData pinData = new Minimap.PinData();
		pinData.m_type = type;
		pinData.m_name = name;
		pinData.m_pos = pos;
		pinData.m_icon = this.GetSprite(type);
		pinData.m_save = save;
		pinData.m_checked = isChecked;
		pinData.m_ownerID = ownerID;
		pinData.m_author = author;
		if (!string.IsNullOrEmpty(pinData.m_name))
		{
			pinData.m_NamePinData = new Minimap.PinNameData(pinData);
		}
		this.m_pins.Add(pinData);
		if (type < (Minimap.PinType)this.m_visibleIconTypes.Length && !this.m_visibleIconTypes[(int)type])
		{
			this.ToggleIconFilter(type);
		}
		this.m_pinUpdateRequired = true;
		return pinData;
	}

	// Token: 0x0600093D RID: 2365 RVA: 0x000517FC File Offset: 0x0004F9FC
	private Sprite GetSprite(Minimap.PinType type)
	{
		if (type == Minimap.PinType.None)
		{
			return null;
		}
		return this.m_icons.Find((Minimap.SpriteData x) => x.m_name == type).m_icon;
	}

	// Token: 0x0600093E RID: 2366 RVA: 0x00051840 File Offset: 0x0004FA40
	private Vector3 GetViewCenterWorldPoint()
	{
		Rect uvRect = this.m_mapImageLarge.uvRect;
		float mx = uvRect.xMin + 0.5f * uvRect.width;
		float my = uvRect.yMin + 0.5f * uvRect.height;
		return this.MapPointToWorld(mx, my);
	}

	// Token: 0x0600093F RID: 2367 RVA: 0x00051890 File Offset: 0x0004FA90
	private Vector3 ScreenToWorldPoint(Vector3 mousePos)
	{
		Vector2 screenPoint = mousePos;
		RectTransform rectTransform = this.m_mapImageLarge.transform as RectTransform;
		Vector2 point;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out point))
		{
			Vector2 vector = Rect.PointToNormalized(rectTransform.rect, point);
			Rect uvRect = this.m_mapImageLarge.uvRect;
			float mx = uvRect.xMin + vector.x * uvRect.width;
			float my = uvRect.yMin + vector.y * uvRect.height;
			return this.MapPointToWorld(mx, my);
		}
		return Vector3.zero;
	}

	// Token: 0x06000940 RID: 2368 RVA: 0x0005191B File Offset: 0x0004FB1B
	private void OnMapLeftDown(UIInputHandler handler)
	{
		this.m_leftDownTime = Time.time;
	}

	// Token: 0x06000941 RID: 2369 RVA: 0x00051928 File Offset: 0x0004FB28
	private void OnMapLeftUp(UIInputHandler handler)
	{
		if (this.m_leftDownTime != 0f)
		{
			if (Time.time - this.m_leftDownTime < this.m_clickDuration)
			{
				this.OnMapLeftClick();
			}
			this.m_leftDownTime = 0f;
		}
		this.m_dragView = false;
		if (Time.time - this.m_leftClickTime < 0.3f)
		{
			this.OnMapDblClick();
			this.m_leftClickTime = 0f;
			return;
		}
		this.m_leftClickTime = Time.time;
	}

	// Token: 0x06000942 RID: 2370 RVA: 0x0005199E File Offset: 0x0004FB9E
	public void OnMapDblClick()
	{
		if (this.m_selectedType == Minimap.PinType.Death)
		{
			return;
		}
		this.ShowPinNameInput(this.ScreenToWorldPoint(ZInput.mousePosition));
	}

	// Token: 0x06000943 RID: 2371 RVA: 0x000519BC File Offset: 0x0004FBBC
	public void OnMapLeftClick()
	{
		ZLog.Log("Left click");
		this.HidePinTextInput(false);
		Vector3 pos = this.ScreenToWorldPoint(ZInput.mousePosition);
		Minimap.PinData closestPin = this.GetClosestPin(pos, this.m_removeRadius * (this.m_largeZoom * 2f), true);
		if (closestPin != null)
		{
			if (closestPin.m_ownerID != 0L)
			{
				closestPin.m_ownerID = 0L;
			}
			else
			{
				closestPin.m_checked = !closestPin.m_checked;
			}
		}
		this.m_pinUpdateRequired = true;
	}

	// Token: 0x06000944 RID: 2372 RVA: 0x00051A30 File Offset: 0x0004FC30
	public void OnMapMiddleClick(UIInputHandler handler)
	{
		this.HidePinTextInput(false);
		Vector3 vector = this.ScreenToWorldPoint(ZInput.mousePosition);
		Chat.instance.SendPing(vector);
		if (Player.m_debugMode && global::Console.instance != null && global::Console.instance.IsCheatsEnabled() && ZInput.GetKey(KeyCode.LeftControl, true))
		{
			Vector3 vector2 = new Vector3(vector.x, Player.m_localPlayer.transform.position.y, vector.z);
			float val;
			Heightmap.GetHeight(vector2, out val);
			vector2.y = Math.Max(0f, val);
			Player.m_localPlayer.TeleportTo(vector2, Player.m_localPlayer.transform.rotation, true);
			Minimap.instance.SetMapMode(Minimap.MapMode.Small);
		}
	}

	// Token: 0x06000945 RID: 2373 RVA: 0x00051AF8 File Offset: 0x0004FCF8
	public void OnMapRightClick(UIInputHandler handler)
	{
		ZLog.Log("Right click");
		this.HidePinTextInput(false);
		Vector3 pos = this.ScreenToWorldPoint(ZInput.mousePosition);
		this.RemovePin(pos, this.m_removeRadius * (this.m_largeZoom * 2f));
		this.m_namePin = null;
	}

	// Token: 0x06000946 RID: 2374 RVA: 0x00051B44 File Offset: 0x0004FD44
	public void OnPressedIcon0()
	{
		this.SelectIcon(Minimap.PinType.Icon0);
	}

	// Token: 0x06000947 RID: 2375 RVA: 0x00051B4D File Offset: 0x0004FD4D
	public void OnPressedIcon1()
	{
		this.SelectIcon(Minimap.PinType.Icon1);
	}

	// Token: 0x06000948 RID: 2376 RVA: 0x00051B56 File Offset: 0x0004FD56
	public void OnPressedIcon2()
	{
		this.SelectIcon(Minimap.PinType.Icon2);
	}

	// Token: 0x06000949 RID: 2377 RVA: 0x00051B5F File Offset: 0x0004FD5F
	public void OnPressedIcon3()
	{
		this.SelectIcon(Minimap.PinType.Icon3);
	}

	// Token: 0x0600094A RID: 2378 RVA: 0x00051B68 File Offset: 0x0004FD68
	public void OnPressedIcon4()
	{
		this.SelectIcon(Minimap.PinType.Icon4);
	}

	// Token: 0x0600094B RID: 2379 RVA: 0x00051B71 File Offset: 0x0004FD71
	public void OnPressedIconDeath()
	{
	}

	// Token: 0x0600094C RID: 2380 RVA: 0x00051B73 File Offset: 0x0004FD73
	public void OnPressedIconBoss()
	{
	}

	// Token: 0x0600094D RID: 2381 RVA: 0x00051B75 File Offset: 0x0004FD75
	public void OnAltPressedIcon0()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon0);
	}

	// Token: 0x0600094E RID: 2382 RVA: 0x00051B7E File Offset: 0x0004FD7E
	public void OnAltPressedIcon1()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon1);
	}

	// Token: 0x0600094F RID: 2383 RVA: 0x00051B87 File Offset: 0x0004FD87
	public void OnAltPressedIcon2()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon2);
	}

	// Token: 0x06000950 RID: 2384 RVA: 0x00051B90 File Offset: 0x0004FD90
	public void OnAltPressedIcon3()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon3);
	}

	// Token: 0x06000951 RID: 2385 RVA: 0x00051B99 File Offset: 0x0004FD99
	public void OnAltPressedIcon4()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon4);
	}

	// Token: 0x06000952 RID: 2386 RVA: 0x00051BA2 File Offset: 0x0004FDA2
	public void OnAltPressedIconDeath()
	{
		this.ToggleIconFilter(Minimap.PinType.Death);
	}

	// Token: 0x06000953 RID: 2387 RVA: 0x00051BAB File Offset: 0x0004FDAB
	public void OnAltPressedIconBoss()
	{
		this.ToggleIconFilter(Minimap.PinType.Boss);
	}

	// Token: 0x06000954 RID: 2388 RVA: 0x00051BB5 File Offset: 0x0004FDB5
	public void OnTogglePublicPosition()
	{
		if (ZNet.instance)
		{
			ZNet.instance.SetPublicReferencePosition(this.m_publicPosition.isOn);
		}
	}

	// Token: 0x06000955 RID: 2389 RVA: 0x00051BD8 File Offset: 0x0004FDD8
	public void OnToggleSharedMapData()
	{
		this.m_showSharedMapData = !this.m_showSharedMapData;
	}

	// Token: 0x06000956 RID: 2390 RVA: 0x00051BEC File Offset: 0x0004FDEC
	private void SelectIcon(Minimap.PinType type)
	{
		this.m_selectedType = type;
		this.m_pinUpdateRequired = true;
		foreach (KeyValuePair<Minimap.PinType, Image> keyValuePair in this.m_selectedIcons)
		{
			keyValuePair.Value.enabled = (keyValuePair.Key == type);
		}
	}

	// Token: 0x06000957 RID: 2391 RVA: 0x00051C5C File Offset: 0x0004FE5C
	private void ToggleIconFilter(Minimap.PinType type)
	{
		this.m_visibleIconTypes[(int)type] = !this.m_visibleIconTypes[(int)type];
		this.m_pinUpdateRequired = true;
		foreach (KeyValuePair<Minimap.PinType, Image> keyValuePair in this.m_selectedIcons)
		{
			keyValuePair.Value.transform.parent.GetComponent<Image>().color = (this.m_visibleIconTypes[(int)keyValuePair.Key] ? Color.white : Color.gray);
		}
	}

	// Token: 0x06000958 RID: 2392 RVA: 0x00051CFC File Offset: 0x0004FEFC
	private void ClearPins()
	{
		foreach (Minimap.PinData pin in this.m_pins)
		{
			this.DestroyPinMarker(pin);
		}
		this.m_pins.Clear();
		this.m_deathPin = null;
	}

	// Token: 0x06000959 RID: 2393 RVA: 0x00051D64 File Offset: 0x0004FF64
	private void UpdateBiome(Player player)
	{
		if (this.m_mode != Minimap.MapMode.Large)
		{
			Heightmap.Biome currentBiome = player.GetCurrentBiome();
			if (currentBiome != this.m_biome)
			{
				this.m_biome = currentBiome;
				string text = Localization.instance.Localize("$biome_" + currentBiome.ToString().ToLower());
				this.m_biomeNameSmall.text = text;
				this.m_biomeNameLarge.text = text;
				this.m_biomeNameSmall.GetComponent<Animator>().SetTrigger("pulse");
			}
			return;
		}
		Vector3 vector = this.ScreenToWorldPoint(ZInput.IsMouseActive() ? ZInput.mousePosition : new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2)));
		if (this.IsExplored(vector))
		{
			Heightmap.Biome biome = WorldGenerator.instance.GetBiome(vector);
			string text2 = Localization.instance.Localize("$biome_" + biome.ToString().ToLower());
			this.m_biomeNameLarge.text = text2;
			return;
		}
		this.m_biomeNameLarge.text = "";
	}

	// Token: 0x0600095A RID: 2394 RVA: 0x00051E70 File Offset: 0x00050070
	public byte[] GetSharedMapData(byte[] oldMapData)
	{
		List<bool> list = null;
		if (oldMapData != null)
		{
			ZPackage zpackage = new ZPackage(oldMapData);
			int version = zpackage.ReadInt();
			list = this.ReadExploredArray(zpackage, version);
		}
		ZPackage zpackage2 = new ZPackage();
		zpackage2.Write(3);
		zpackage2.Write(this.m_explored.Length);
		for (int i = 0; i < this.m_explored.Length; i++)
		{
			bool flag = this.m_exploredOthers[i] || this.m_explored[i];
			if (list != null)
			{
				flag |= list[i];
			}
			zpackage2.Write(flag);
		}
		int num = 0;
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_save && pinData.m_type != Minimap.PinType.Death)
			{
				num++;
			}
		}
		long playerID = Player.m_localPlayer.GetPlayerID();
		PlatformUserID platformUserID = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
		zpackage2.Write(num);
		foreach (Minimap.PinData pinData2 in this.m_pins)
		{
			if (pinData2.m_save && pinData2.m_type != Minimap.PinType.Death)
			{
				long num2 = (pinData2.m_ownerID != 0L) ? pinData2.m_ownerID : playerID;
				PlatformUserID platformUserID2 = (!pinData2.m_author.IsValid && num2 == playerID) ? platformUserID : pinData2.m_author;
				zpackage2.Write(num2);
				zpackage2.Write(pinData2.m_name);
				zpackage2.Write(pinData2.m_pos);
				zpackage2.Write((int)pinData2.m_type);
				zpackage2.Write(pinData2.m_checked);
				zpackage2.Write(platformUserID2.ToString());
			}
		}
		return zpackage2.GetArray();
	}

	// Token: 0x0600095B RID: 2395 RVA: 0x00052064 File Offset: 0x00050264
	private List<bool> ReadExploredArray(ZPackage pkg, int version)
	{
		int num = pkg.ReadInt();
		if (num != this.m_explored.Length)
		{
			ZLog.LogWarning("Map exploration array size missmatch:" + num.ToString() + " VS " + this.m_explored.Length.ToString());
			return null;
		}
		List<bool> list = new List<bool>();
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				bool item = pkg.ReadBool();
				list.Add(item);
			}
		}
		return list;
	}

	// Token: 0x0600095C RID: 2396 RVA: 0x000520F0 File Offset: 0x000502F0
	public bool AddSharedMapData(byte[] dataArray)
	{
		ZPackage zpackage = new ZPackage(dataArray);
		int num = zpackage.ReadInt();
		List<bool> list = this.ReadExploredArray(zpackage, num);
		if (list == null)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				int num2 = i * this.m_textureSize + j;
				bool flag2 = list[num2];
				bool flag3 = this.m_exploredOthers[num2] || this.m_explored[num2];
				if (flag2 != flag3 && flag2 && this.ExploreOthers(j, i))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.m_fogTexture.Apply();
		}
		bool flag4 = false;
		if (num >= 2)
		{
			long playerID = Player.m_localPlayer.GetPlayerID();
			bool flag5 = false;
			for (int k = this.m_pins.Count - 1; k >= 0; k--)
			{
				Minimap.PinData pinData = this.m_pins[k];
				if (pinData.m_ownerID != 0L && pinData.m_ownerID != playerID)
				{
					pinData.m_shouldDelete = true;
					flag5 = true;
				}
			}
			int num3 = zpackage.ReadInt();
			for (int l = 0; l < num3; l++)
			{
				long num4 = zpackage.ReadLong();
				string name = zpackage.ReadString();
				Vector3 pos = zpackage.ReadVector3();
				Minimap.PinType type = (Minimap.PinType)zpackage.ReadInt();
				bool isChecked = zpackage.ReadBool();
				string platformUserID = (num >= 3) ? zpackage.ReadString() : "";
				if (this.HavePinInRange(pos, 1f))
				{
					this.GetClosestPin(pos, 1f, false).m_shouldDelete = false;
				}
				else if (num4 != playerID)
				{
					if (num4 == playerID)
					{
						num4 = 0L;
					}
					this.AddPin(pos, type, name, true, isChecked, num4, new PlatformUserID(platformUserID));
					flag4 = true;
				}
			}
			if (flag5)
			{
				for (int m = this.m_pins.Count - 1; m >= 0; m--)
				{
					Minimap.PinData pinData2 = this.m_pins[m];
					if (pinData2.m_ownerID != 0L && pinData2.m_ownerID != playerID && pinData2.m_shouldDelete)
					{
						this.RemovePin(pinData2);
						flag4 = true;
					}
				}
			}
		}
		return flag || flag4;
	}

	// Token: 0x04000A99 RID: 2713
	private Color forest = new Color(1f, 0f, 0f, 0f);

	// Token: 0x04000A9A RID: 2714
	private Color noForest = new Color(0f, 0f, 0f, 0f);

	// Token: 0x04000A9B RID: 2715
	private static int MAPVERSION = 8;

	// Token: 0x04000A9C RID: 2716
	private float inputDelay;

	// Token: 0x04000A9D RID: 2717
	private const int sharedMapDataVersion = 3;

	// Token: 0x04000A9E RID: 2718
	private static Minimap m_instance;

	// Token: 0x04000A9F RID: 2719
	public GameObject m_smallRoot;

	// Token: 0x04000AA0 RID: 2720
	public GameObject m_largeRoot;

	// Token: 0x04000AA1 RID: 2721
	public RawImage m_mapImageSmall;

	// Token: 0x04000AA2 RID: 2722
	public RawImage m_mapImageLarge;

	// Token: 0x04000AA3 RID: 2723
	public RectTransform m_pinRootSmall;

	// Token: 0x04000AA4 RID: 2724
	public RectTransform m_pinRootLarge;

	// Token: 0x04000AA5 RID: 2725
	public RectTransform m_pinNameRootSmall;

	// Token: 0x04000AA6 RID: 2726
	public RectTransform m_pinNameRootLarge;

	// Token: 0x04000AA7 RID: 2727
	public TMP_Text m_biomeNameSmall;

	// Token: 0x04000AA8 RID: 2728
	public TMP_Text m_biomeNameLarge;

	// Token: 0x04000AA9 RID: 2729
	public RectTransform m_smallShipMarker;

	// Token: 0x04000AAA RID: 2730
	public RectTransform m_largeShipMarker;

	// Token: 0x04000AAB RID: 2731
	public RectTransform m_smallMarker;

	// Token: 0x04000AAC RID: 2732
	public RectTransform m_largeMarker;

	// Token: 0x04000AAD RID: 2733
	public RectTransform m_windMarker;

	// Token: 0x04000AAE RID: 2734
	public RectTransform m_gamepadCrosshair;

	// Token: 0x04000AAF RID: 2735
	public Toggle m_publicPosition;

	// Token: 0x04000AB0 RID: 2736
	public Image m_selectedIcon0;

	// Token: 0x04000AB1 RID: 2737
	public Image m_selectedIcon1;

	// Token: 0x04000AB2 RID: 2738
	public Image m_selectedIcon2;

	// Token: 0x04000AB3 RID: 2739
	public Image m_selectedIcon3;

	// Token: 0x04000AB4 RID: 2740
	public Image m_selectedIcon4;

	// Token: 0x04000AB5 RID: 2741
	public Image m_selectedIconDeath;

	// Token: 0x04000AB6 RID: 2742
	public Image m_selectedIconBoss;

	// Token: 0x04000AB7 RID: 2743
	private Dictionary<Minimap.PinType, Image> m_selectedIcons = new Dictionary<Minimap.PinType, Image>();

	// Token: 0x04000AB8 RID: 2744
	public Sprite m_pingIcon;

	// Token: 0x04000AB9 RID: 2745
	public Sprite m_pingIconMac;

	// Token: 0x04000ABA RID: 2746
	public Image m_pingImageObject;

	// Token: 0x04000ABB RID: 2747
	private bool[] m_visibleIconTypes;

	// Token: 0x04000ABC RID: 2748
	private bool m_showSharedMapData = true;

	// Token: 0x04000ABD RID: 2749
	public float m_sharedMapDataFadeRate = 2f;

	// Token: 0x04000ABE RID: 2750
	private float m_sharedMapDataFade;

	// Token: 0x04000ABF RID: 2751
	public GameObject m_mapSmall;

	// Token: 0x04000AC0 RID: 2752
	public GameObject m_mapLarge;

	// Token: 0x04000AC1 RID: 2753
	private Material m_mapSmallShader;

	// Token: 0x04000AC2 RID: 2754
	private Material m_mapLargeShader;

	// Token: 0x04000AC3 RID: 2755
	public GameObject m_pinPrefab;

	// Token: 0x04000AC4 RID: 2756
	[SerializeField]
	private GameObject m_pinNamePrefab;

	// Token: 0x04000AC5 RID: 2757
	public GuiInputField m_nameInput;

	// Token: 0x04000AC6 RID: 2758
	public int m_textureSize = 256;

	// Token: 0x04000AC7 RID: 2759
	public float m_pixelSize = 64f;

	// Token: 0x04000AC8 RID: 2760
	public float m_minZoom = 0.01f;

	// Token: 0x04000AC9 RID: 2761
	public float m_maxZoom = 1f;

	// Token: 0x04000ACA RID: 2762
	public float m_showNamesZoom = 0.5f;

	// Token: 0x04000ACB RID: 2763
	public float m_exploreInterval = 2f;

	// Token: 0x04000ACC RID: 2764
	public float m_exploreRadius = 100f;

	// Token: 0x04000ACD RID: 2765
	public float m_removeRadius = 128f;

	// Token: 0x04000ACE RID: 2766
	public float m_pinSizeSmall = 32f;

	// Token: 0x04000ACF RID: 2767
	public float m_pinSizeLarge = 48f;

	// Token: 0x04000AD0 RID: 2768
	public float m_clickDuration = 0.25f;

	// Token: 0x04000AD1 RID: 2769
	public List<Minimap.SpriteData> m_icons = new List<Minimap.SpriteData>();

	// Token: 0x04000AD2 RID: 2770
	public List<Minimap.LocationSpriteData> m_locationIcons = new List<Minimap.LocationSpriteData>();

	// Token: 0x04000AD3 RID: 2771
	public Color m_meadowsColor = new Color(0.45f, 1f, 0.43f);

	// Token: 0x04000AD4 RID: 2772
	public Color m_ashlandsColor = new Color(1f, 0.2f, 0.2f);

	// Token: 0x04000AD5 RID: 2773
	public Color m_blackforestColor = new Color(0f, 0.7f, 0f);

	// Token: 0x04000AD6 RID: 2774
	public Color m_deepnorthColor = new Color(1f, 1f, 1f);

	// Token: 0x04000AD7 RID: 2775
	public Color m_heathColor = new Color(1f, 1f, 0.2f);

	// Token: 0x04000AD8 RID: 2776
	public Color m_swampColor = new Color(0.6f, 0.5f, 0.5f);

	// Token: 0x04000AD9 RID: 2777
	public Color m_mountainColor = new Color(1f, 1f, 1f);

	// Token: 0x04000ADA RID: 2778
	private Color m_mistlandsColor = new Color(0.2f, 0.2f, 0.2f);

	// Token: 0x04000ADB RID: 2779
	private Minimap.PinData m_namePin;

	// Token: 0x04000ADC RID: 2780
	private Minimap.PinType m_selectedType;

	// Token: 0x04000ADD RID: 2781
	private Minimap.PinData m_deathPin;

	// Token: 0x04000ADE RID: 2782
	private Minimap.PinData m_spawnPointPin;

	// Token: 0x04000ADF RID: 2783
	private Dictionary<Vector3, Minimap.PinData> m_locationPins = new Dictionary<Vector3, Minimap.PinData>();

	// Token: 0x04000AE0 RID: 2784
	private float m_updateLocationsTimer;

	// Token: 0x04000AE1 RID: 2785
	private List<Minimap.PinData> m_pingPins = new List<Minimap.PinData>();

	// Token: 0x04000AE2 RID: 2786
	private List<Minimap.PinData> m_shoutPins = new List<Minimap.PinData>();

	// Token: 0x04000AE3 RID: 2787
	private List<Chat.WorldTextInstance> m_tempShouts = new List<Chat.WorldTextInstance>();

	// Token: 0x04000AE4 RID: 2788
	private List<Minimap.PinData> m_playerPins = new List<Minimap.PinData>();

	// Token: 0x04000AE5 RID: 2789
	private List<ZNet.PlayerInfo> m_tempPlayerInfo = new List<ZNet.PlayerInfo>();

	// Token: 0x04000AE6 RID: 2790
	private Minimap.PinData m_randEventPin;

	// Token: 0x04000AE7 RID: 2791
	private Minimap.PinData m_randEventAreaPin;

	// Token: 0x04000AE8 RID: 2792
	private float m_updateEventTime;

	// Token: 0x04000AE9 RID: 2793
	private bool[] m_explored;

	// Token: 0x04000AEA RID: 2794
	private bool[] m_exploredOthers;

	// Token: 0x04000AEB RID: 2795
	public GameObject m_sharedMapHint;

	// Token: 0x04000AEC RID: 2796
	public List<GameObject> m_hints;

	// Token: 0x04000AED RID: 2797
	private List<Minimap.PinData> m_pins = new List<Minimap.PinData>();

	// Token: 0x04000AEE RID: 2798
	private bool m_pinUpdateRequired;

	// Token: 0x04000AEF RID: 2799
	private Vector3 m_previousMapCenter = Vector3.zero;

	// Token: 0x04000AF0 RID: 2800
	private float m_previousLargeZoom = 0.1f;

	// Token: 0x04000AF1 RID: 2801
	private float m_previousSmallZoom = 0.01f;

	// Token: 0x04000AF2 RID: 2802
	private Texture2D m_forestMaskTexture;

	// Token: 0x04000AF3 RID: 2803
	private Texture2D m_mapTexture;

	// Token: 0x04000AF4 RID: 2804
	private Texture2D m_heightTexture;

	// Token: 0x04000AF5 RID: 2805
	private Texture2D m_fogTexture;

	// Token: 0x04000AF6 RID: 2806
	private float m_largeZoom = 0.1f;

	// Token: 0x04000AF7 RID: 2807
	private float m_smallZoom = 0.01f;

	// Token: 0x04000AF8 RID: 2808
	private Heightmap.Biome m_biome;

	// Token: 0x04000AF9 RID: 2809
	[HideInInspector]
	public Minimap.MapMode m_mode;

	// Token: 0x04000AFA RID: 2810
	public float m_nomapPingDistance = 50f;

	// Token: 0x04000AFB RID: 2811
	private float m_exploreTimer;

	// Token: 0x04000AFC RID: 2812
	private bool m_hasGenerated;

	// Token: 0x04000AFD RID: 2813
	private bool m_dragView = true;

	// Token: 0x04000AFE RID: 2814
	private Vector3 m_mapOffset = Vector3.zero;

	// Token: 0x04000AFF RID: 2815
	private float m_leftDownTime;

	// Token: 0x04000B00 RID: 2816
	private float m_leftClickTime;

	// Token: 0x04000B01 RID: 2817
	private Vector3 m_dragWorldPos = Vector3.zero;

	// Token: 0x04000B02 RID: 2818
	private bool m_wasFocused;

	// Token: 0x04000B03 RID: 2819
	private float m_delayTextInput;

	// Token: 0x04000B04 RID: 2820
	private float m_pauseUpdate;

	// Token: 0x04000B05 RID: 2821
	private const bool m_enableLastDeathAutoPin = false;

	// Token: 0x04000B06 RID: 2822
	private int m_hiddenFrames = 9999;

	// Token: 0x04000B07 RID: 2823
	[SerializeField]
	private float m_gamepadMoveSpeed = 0.33f;

	// Token: 0x04000B08 RID: 2824
	private string m_forestMaskTexturePath;

	// Token: 0x04000B09 RID: 2825
	private const string c_forestMaskTextureName = "forestMaskTexCache";

	// Token: 0x04000B0A RID: 2826
	private string m_mapTexturePath;

	// Token: 0x04000B0B RID: 2827
	private const string c_mapTextureName = "mapTexCache";

	// Token: 0x04000B0C RID: 2828
	private string m_heightTexturePath;

	// Token: 0x04000B0D RID: 2829
	private const string c_heightTextureName = "heightTexCache";

	// Token: 0x020002AB RID: 683
	public enum MapMode
	{
		// Token: 0x04002253 RID: 8787
		None,
		// Token: 0x04002254 RID: 8788
		Small,
		// Token: 0x04002255 RID: 8789
		Large
	}

	// Token: 0x020002AC RID: 684
	public enum PinType
	{
		// Token: 0x04002257 RID: 8791
		Icon0,
		// Token: 0x04002258 RID: 8792
		Icon1,
		// Token: 0x04002259 RID: 8793
		Icon2,
		// Token: 0x0400225A RID: 8794
		Icon3,
		// Token: 0x0400225B RID: 8795
		Death,
		// Token: 0x0400225C RID: 8796
		Bed,
		// Token: 0x0400225D RID: 8797
		Icon4,
		// Token: 0x0400225E RID: 8798
		Shout,
		// Token: 0x0400225F RID: 8799
		None,
		// Token: 0x04002260 RID: 8800
		Boss,
		// Token: 0x04002261 RID: 8801
		Player,
		// Token: 0x04002262 RID: 8802
		RandomEvent,
		// Token: 0x04002263 RID: 8803
		Ping,
		// Token: 0x04002264 RID: 8804
		EventArea,
		// Token: 0x04002265 RID: 8805
		Hildir1,
		// Token: 0x04002266 RID: 8806
		Hildir2,
		// Token: 0x04002267 RID: 8807
		Hildir3
	}

	// Token: 0x020002AD RID: 685
	public class PinData
	{
		// Token: 0x04002268 RID: 8808
		public string m_name;

		// Token: 0x04002269 RID: 8809
		public Minimap.PinType m_type;

		// Token: 0x0400226A RID: 8810
		public Sprite m_icon;

		// Token: 0x0400226B RID: 8811
		public Vector3 m_pos;

		// Token: 0x0400226C RID: 8812
		public bool m_save;

		// Token: 0x0400226D RID: 8813
		public long m_ownerID;

		// Token: 0x0400226E RID: 8814
		public PlatformUserID m_author;

		// Token: 0x0400226F RID: 8815
		public bool m_shouldDelete;

		// Token: 0x04002270 RID: 8816
		public bool m_checked;

		// Token: 0x04002271 RID: 8817
		public bool m_doubleSize;

		// Token: 0x04002272 RID: 8818
		public bool m_animate;

		// Token: 0x04002273 RID: 8819
		public float m_worldSize;

		// Token: 0x04002274 RID: 8820
		public RectTransform m_uiElement;

		// Token: 0x04002275 RID: 8821
		public GameObject m_checkedElement;

		// Token: 0x04002276 RID: 8822
		public Image m_iconElement;

		// Token: 0x04002277 RID: 8823
		public Minimap.PinNameData m_NamePinData;
	}

	// Token: 0x020002AE RID: 686
	public class PinNameData
	{
		// Token: 0x1700019D RID: 413
		// (get) Token: 0x060020AC RID: 8364 RVA: 0x000E925B File Offset: 0x000E745B
		// (set) Token: 0x060020AB RID: 8363 RVA: 0x000E9252 File Offset: 0x000E7452
		public TMP_Text PinNameText { get; private set; }

		// Token: 0x1700019E RID: 414
		// (get) Token: 0x060020AE RID: 8366 RVA: 0x000E926C File Offset: 0x000E746C
		// (set) Token: 0x060020AD RID: 8365 RVA: 0x000E9263 File Offset: 0x000E7463
		public GameObject PinNameGameObject { get; private set; }

		// Token: 0x1700019F RID: 415
		// (get) Token: 0x060020B0 RID: 8368 RVA: 0x000E927D File Offset: 0x000E747D
		// (set) Token: 0x060020AF RID: 8367 RVA: 0x000E9274 File Offset: 0x000E7474
		public RectTransform PinNameRectTransform { get; private set; }

		// Token: 0x060020B1 RID: 8369 RVA: 0x000E9285 File Offset: 0x000E7485
		public PinNameData(Minimap.PinData pin)
		{
			this.ParentPin = pin;
		}

		// Token: 0x060020B2 RID: 8370 RVA: 0x000E9294 File Offset: 0x000E7494
		internal void SetTextAndGameObject(GameObject text)
		{
			this.PinNameGameObject = text;
			this.PinNameText = this.PinNameGameObject.GetComponentInChildren<TMP_Text>();
			if (!this.ParentPin.m_author.IsValid || this.ParentPin.m_author == PlatformManager.DistributionPlatform.LocalUser.PlatformUserID)
			{
				this.PinNameText.text = Localization.instance.Localize(this.ParentPin.m_name);
			}
			else
			{
				this.PinNameText.text = CensorShittyWords.FilterUGC(Localization.instance.Localize(this.ParentPin.m_name), UGCType.Text, this.ParentPin.m_author, 0L);
			}
			this.PinNameRectTransform = text.GetComponent<RectTransform>();
		}

		// Token: 0x060020B3 RID: 8371 RVA: 0x000E934D File Offset: 0x000E754D
		internal void DestroyMapMarker()
		{
			UnityEngine.Object.Destroy(this.PinNameGameObject);
			this.PinNameGameObject = null;
		}

		// Token: 0x04002278 RID: 8824
		public readonly Minimap.PinData ParentPin;
	}

	// Token: 0x020002AF RID: 687
	[Serializable]
	public struct SpriteData
	{
		// Token: 0x0400227C RID: 8828
		public Minimap.PinType m_name;

		// Token: 0x0400227D RID: 8829
		public Sprite m_icon;
	}

	// Token: 0x020002B0 RID: 688
	[Serializable]
	public struct LocationSpriteData
	{
		// Token: 0x0400227E RID: 8830
		public string m_name;

		// Token: 0x0400227F RID: 8831
		public Sprite m_icon;
	}
}
