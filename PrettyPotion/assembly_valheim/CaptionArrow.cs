using System;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x0200006E RID: 110
public class CaptionArrow : MonoBehaviour
{
	// Token: 0x06000714 RID: 1812 RVA: 0x0003AA84 File Offset: 0x00038C84
	public void Setup(ClosedCaptions.CaptionType type, Vector3 position, float distanceFactor = 0f)
	{
		this.m_alpha = this.m_imageComponent.color.a;
		this.m_color = ClosedCaptions.Instance.GetCaptionColor(type);
		this.m_color.a = this.m_alpha;
		this.m_imageComponent.color = this.m_color;
		this.m_timer = this.m_fadeTime;
		this.m_sfxPosition = position;
		this.RotateArrow();
		this.m_imageComponent.material = new Material(this.m_imageComponent.material);
		this.m_imageComponent.material.SetFloat(CaptionArrow.s_CaptionDistance, this.m_distanceCurve.Evaluate(distanceFactor));
	}

	// Token: 0x06000715 RID: 1813 RVA: 0x0003AB30 File Offset: 0x00038D30
	private void Update()
	{
		this.m_timer -= Time.deltaTime;
		if (this.m_timer <= 0f)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		this.m_color.a = this.m_alpha * Mathf.Clamp01(this.m_timer / this.m_fadeTime);
		this.m_color.a = Mathf.SmoothStep(0f, 1f, this.m_color.a);
		this.m_imageComponent.color = this.m_color;
		this.RotateArrow();
	}

	// Token: 0x06000716 RID: 1814 RVA: 0x0003ABC8 File Offset: 0x00038DC8
	public void RotateArrow()
	{
		Vector3 position = AudioMan.instance.GetActiveAudioListener().transform.position;
		position.y = this.m_sfxPosition.y;
		Vector3 normalized = Vector3.ProjectOnPlane(Utils.GetMainCamera().transform.forward, Vector3.up).normalized;
		Vector3 to = position.DirTo(this.m_sfxPosition);
		float num = Vector3.SignedAngle(normalized, to, Vector3.up);
		base.transform.localEulerAngles = new Vector3(0f, 0f, -num);
	}

	// Token: 0x0400083A RID: 2106
	public float m_fadeTime = 1.5f;

	// Token: 0x0400083B RID: 2107
	private Vector3 m_sfxPosition;

	// Token: 0x0400083C RID: 2108
	private float m_timer;

	// Token: 0x0400083D RID: 2109
	public RawImage m_imageComponent;

	// Token: 0x0400083E RID: 2110
	private Color m_color;

	// Token: 0x0400083F RID: 2111
	private ClosedCaptions.CaptionType m_type;

	// Token: 0x04000840 RID: 2112
	private float m_alpha;

	// Token: 0x04000841 RID: 2113
	public AnimationCurve m_distanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	// Token: 0x04000842 RID: 2114
	private static readonly int s_CaptionDistance = Shader.PropertyToID("_CaptionDistance");
}
