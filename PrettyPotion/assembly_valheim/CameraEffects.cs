using System;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityStandardAssets.ImageEffects;

// Token: 0x0200004B RID: 75
public class CameraEffects : MonoBehaviour
{
	// Token: 0x17000012 RID: 18
	// (get) Token: 0x060005E4 RID: 1508 RVA: 0x000320F5 File Offset: 0x000302F5
	public static CameraEffects instance
	{
		get
		{
			return CameraEffects.m_instance;
		}
	}

	// Token: 0x060005E5 RID: 1509 RVA: 0x000320FC File Offset: 0x000302FC
	private void Awake()
	{
		CameraEffects.m_instance = this;
		this.m_postProcessing = base.GetComponent<PostProcessingBehaviour>();
		this.m_dof = base.GetComponent<DepthOfField>();
		this.ApplySettings();
	}

	// Token: 0x060005E6 RID: 1510 RVA: 0x00032122 File Offset: 0x00030322
	private void OnDestroy()
	{
		if (CameraEffects.m_instance == this)
		{
			CameraEffects.m_instance = null;
		}
	}

	// Token: 0x060005E7 RID: 1511 RVA: 0x00032138 File Offset: 0x00030338
	public void ApplySettings()
	{
		this.SetDof(PlatformPrefs.GetInt("DOF", 1) == 1);
		this.SetBloom(PlatformPrefs.GetInt("Bloom", 1) == 1);
		this.SetSSAO(PlatformPrefs.GetInt("SSAO", 1) == 1);
		this.SetSunShafts(PlatformPrefs.GetInt("SunShafts", 1) == 1);
		this.SetAntiAliasing(PlatformPrefs.GetInt("AntiAliasing", 1) == 1);
		this.SetCA(PlatformPrefs.GetInt("ChromaticAberration", 1) == 1);
		this.SetMotionBlur(PlatformPrefs.GetInt("MotionBlur", 1) == 1);
	}

	// Token: 0x060005E8 RID: 1512 RVA: 0x000321F0 File Offset: 0x000303F0
	public void SetSunShafts(bool enabled)
	{
		SunShafts component = base.GetComponent<SunShafts>();
		if (component != null)
		{
			component.enabled = enabled;
		}
	}

	// Token: 0x060005E9 RID: 1513 RVA: 0x00032214 File Offset: 0x00030414
	private void SetBloom(bool enabled)
	{
		this.m_postProcessing.profile.bloom.enabled = enabled;
	}

	// Token: 0x060005EA RID: 1514 RVA: 0x0003222C File Offset: 0x0003042C
	private void SetSSAO(bool enabled)
	{
		this.m_postProcessing.profile.ambientOcclusion.enabled = enabled;
	}

	// Token: 0x060005EB RID: 1515 RVA: 0x00032244 File Offset: 0x00030444
	private void SetMotionBlur(bool enabled)
	{
		this.m_postProcessing.profile.motionBlur.enabled = enabled;
	}

	// Token: 0x060005EC RID: 1516 RVA: 0x0003225C File Offset: 0x0003045C
	private void SetAntiAliasing(bool enabled)
	{
		this.m_postProcessing.profile.antialiasing.enabled = enabled;
	}

	// Token: 0x060005ED RID: 1517 RVA: 0x00032274 File Offset: 0x00030474
	private void SetCA(bool enabled)
	{
		this.m_postProcessing.profile.chromaticAberration.enabled = enabled;
	}

	// Token: 0x060005EE RID: 1518 RVA: 0x0003228C File Offset: 0x0003048C
	private void SetDof(bool enabled)
	{
		this.m_dof.enabled = (enabled || this.m_forceDof);
	}

	// Token: 0x060005EF RID: 1519 RVA: 0x000322A5 File Offset: 0x000304A5
	private void LateUpdate()
	{
		this.UpdateDOF();
	}

	// Token: 0x060005F0 RID: 1520 RVA: 0x000322AD File Offset: 0x000304AD
	private bool ControllingShip()
	{
		return Player.m_localPlayer == null || Player.m_localPlayer.GetControlledShip() != null;
	}

	// Token: 0x060005F1 RID: 1521 RVA: 0x000322D4 File Offset: 0x000304D4
	private void UpdateDOF()
	{
		if (!this.m_dof.enabled || !this.m_dofAutoFocus)
		{
			return;
		}
		float num = this.m_dofMaxDistance;
		RaycastHit raycastHit;
		if (Physics.Raycast(base.transform.position, base.transform.forward, out raycastHit, this.m_dofMaxDistance, this.m_dofRayMask))
		{
			num = raycastHit.distance;
		}
		if (this.ControllingShip() && num < this.m_dofMinDistanceShip)
		{
			num = this.m_dofMinDistanceShip;
		}
		if (num < this.m_dofMinDistance)
		{
			num = this.m_dofMinDistance;
		}
		this.m_dof.focalLength = Mathf.Lerp(this.m_dof.focalLength, num, 0.2f);
	}

	// Token: 0x0400069F RID: 1695
	private static CameraEffects m_instance;

	// Token: 0x040006A0 RID: 1696
	public bool m_forceDof;

	// Token: 0x040006A1 RID: 1697
	public LayerMask m_dofRayMask;

	// Token: 0x040006A2 RID: 1698
	public bool m_dofAutoFocus;

	// Token: 0x040006A3 RID: 1699
	public float m_dofMinDistance = 50f;

	// Token: 0x040006A4 RID: 1700
	public float m_dofMinDistanceShip = 50f;

	// Token: 0x040006A5 RID: 1701
	public float m_dofMaxDistance = 3000f;

	// Token: 0x040006A6 RID: 1702
	private PostProcessingBehaviour m_postProcessing;

	// Token: 0x040006A7 RID: 1703
	private DepthOfField m_dof;
}
