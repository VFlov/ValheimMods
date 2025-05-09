using System;
using UnityEngine;

// Token: 0x0200005E RID: 94
public class MeteorSmash : MonoBehaviour
{
	// Token: 0x06000674 RID: 1652 RVA: 0x00036270 File Offset: 0x00034470
	private void Start()
	{
		Vector3 vector = Vector3.RotateTowards(Vector3.forward, Vector3.up, 0.017453292f * this.m_spawnAngle, 0f);
		vector = (Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f) * vector).normalized * this.m_spawnDistance;
		this.m_startPos = base.transform.position + vector;
		this.m_originalScale = this.m_meteorObject.transform.localScale;
		this.m_meteorObject.SetActive(true);
		this.m_landingEffect.SetActive(false);
		this.m_meteorObject.transform.position = Vector3.Lerp(this.m_startPos, base.transform.position, this.m_speedCurve.Evaluate(0f));
		this.m_meteorObject.transform.localScale = Vector3.Lerp(Vector3.zero, this.m_originalScale, this.m_scaleCurve.Evaluate(0f));
		this.m_meteorObject.transform.LookAt(base.transform.position);
	}

	// Token: 0x06000675 RID: 1653 RVA: 0x00036398 File Offset: 0x00034598
	private void Update()
	{
		if (this.m_crashed)
		{
			return;
		}
		this.m_timer += Time.deltaTime;
		float time = this.m_timer / this.m_timeToLand;
		this.m_meteorObject.transform.position = Vector3.Lerp(this.m_startPos, base.transform.position, this.m_speedCurve.Evaluate(time));
		this.m_meteorObject.transform.localScale = Vector3.Lerp(Vector3.zero, this.m_originalScale, this.m_scaleCurve.Evaluate(time));
		if (this.m_timer < this.m_timeToLand && !this.m_crashed)
		{
			return;
		}
		this.m_crashed = true;
		this.m_landingEffect.SetActive(true);
	}

	// Token: 0x04000767 RID: 1895
	[global::Tooltip("Should be a child of this object.")]
	public GameObject m_meteorObject;

	// Token: 0x04000768 RID: 1896
	[global::Tooltip("Should be a child of this object.")]
	public GameObject m_landingEffect;

	// Token: 0x04000769 RID: 1897
	[Header("Timing")]
	public AnimationCurve m_speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	// Token: 0x0400076A RID: 1898
	public AnimationCurve m_scaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	// Token: 0x0400076B RID: 1899
	public float m_timeToLand = 10f;

	// Token: 0x0400076C RID: 1900
	[Header("Spawn Position")]
	public float m_spawnDistance = 500f;

	// Token: 0x0400076D RID: 1901
	public float m_spawnAngle = 45f;

	// Token: 0x0400076E RID: 1902
	private float m_timer;

	// Token: 0x0400076F RID: 1903
	private bool m_crashed;

	// Token: 0x04000770 RID: 1904
	private Vector3 m_startPos;

	// Token: 0x04000771 RID: 1905
	private Vector3 m_originalScale;
}
