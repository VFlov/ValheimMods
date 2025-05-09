using System;
using System.Collections;
using LlamAcademy.Spring;
using UnityEngine;

// Token: 0x02000164 RID: 356
public class ConditionalObject : MonoBehaviour, Hoverable
{
	// Token: 0x06001595 RID: 5525 RVA: 0x0009EF80 File Offset: 0x0009D180
	private void Awake()
	{
		this.m_startScale = this.m_enableObject.transform.localScale;
		this.m_startHeight = this.m_enableObject.transform.position.y;
		this.m_scaleSpring = new SpringVector3
		{
			Damping = this.m_springDamping,
			Stiffness = this.m_springStiffness,
			StartValue = this.m_startScale,
			EndValue = this.m_startScale,
			InitialVelocity = this.m_startSpringVelocity
		};
		if (this.ShouldBeVisible() && !string.IsNullOrEmpty(this.m_globalKeyCondition))
		{
			this.m_enableObject.SetActive(true);
			if (!string.IsNullOrEmpty(this.m_animatorBool))
			{
				Animator componentInChildren = this.m_enableObject.GetComponentInChildren<Animator>();
				if (componentInChildren != null)
				{
					componentInChildren.SetBool(this.m_animatorBool, true);
				}
			}
			this.m_springActive = false;
			this.m_dropTimer = float.PositiveInfinity;
		}
		else
		{
			this.m_enableObject.SetActive(false);
		}
		this.m_dropTimeActual = this.m_dropTime + UnityEngine.Random.Range(0f, this.m_dropTimeVariance);
	}

	// Token: 0x06001596 RID: 5526 RVA: 0x0009F08C File Offset: 0x0009D28C
	private void Update()
	{
		if (!this.m_enableObject.activeInHierarchy && this.ShouldBeVisible())
		{
			this.m_delayTimer += Time.deltaTime;
			if (this.m_delayTimer > this.m_appearDelay)
			{
				if (this.m_dropEnabled)
				{
					this.m_enableObject.transform.position = this.m_enableObject.transform.position + Vector3.up * this.m_dropHeight;
				}
				else if (this.m_springEnabled)
				{
					this.ActivateSpring();
				}
				this.m_enableObject.SetActive(true);
				this.m_showEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
				if (!string.IsNullOrEmpty(this.m_animatorBool))
				{
					Animator componentInChildren = this.m_enableObject.GetComponentInChildren<Animator>();
					if (componentInChildren != null)
					{
						componentInChildren.SetBool(this.m_animatorBool, true);
					}
					else
					{
						ZLog.LogError(string.Concat(new string[]
						{
							"Object '",
							base.name,
							"' trying to set animation trigger '",
							this.m_animatorBool,
							"' but no animator was found!"
						}));
					}
				}
			}
		}
		if (this.m_enableObject.activeInHierarchy)
		{
			if (this.m_springEnabled && this.m_springActive)
			{
				this.m_enableObject.transform.localScale = this.m_scaleSpring.Evaluate(Time.deltaTime);
			}
			if (this.m_dropEnabled)
			{
				if (this.m_dropTimer <= this.m_dropTimeActual)
				{
					this.m_dropTimer += Time.deltaTime;
					Vector3 position = this.m_enableObject.transform.position;
					float num = (1f - this.m_dropCurve.Evaluate(this.m_dropTimer / this.m_dropTimeActual)) * this.m_dropHeight;
					position.y = this.m_startHeight + num;
					this.m_enableObject.transform.position = position;
				}
				if (this.m_dropTimer > this.m_dropTimeActual && !this.m_springActive)
				{
					this.ActivateSpring();
				}
			}
		}
	}

	// Token: 0x06001597 RID: 5527 RVA: 0x0009F2A1 File Offset: 0x0009D4A1
	private bool ShouldBeVisible()
	{
		return string.IsNullOrEmpty(this.m_globalKeyCondition) || (ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(this.m_globalKeyCondition));
	}

	// Token: 0x06001598 RID: 5528 RVA: 0x0009F2D0 File Offset: 0x0009D4D0
	private void ActivateSpring()
	{
		base.StartCoroutine(this.DisableSpring());
		this.m_springActive = true;
	}

	// Token: 0x06001599 RID: 5529 RVA: 0x0009F2E6 File Offset: 0x0009D4E6
	private IEnumerator DisableSpring()
	{
		yield return new WaitForSeconds(this.m_springDisableTime);
		this.m_springActive = false;
		this.m_enableObject.transform.localScale = this.m_startScale;
		yield break;
	}

	// Token: 0x0600159A RID: 5530 RVA: 0x0009F2F5 File Offset: 0x0009D4F5
	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_hoverName);
	}

	// Token: 0x0600159B RID: 5531 RVA: 0x0009F307 File Offset: 0x0009D507
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_hoverName);
	}

	// Token: 0x0400153B RID: 5435
	private float m_delayTimer;

	// Token: 0x0400153C RID: 5436
	[NonSerialized]
	public float m_dropTimer;

	// Token: 0x0400153D RID: 5437
	private SpringVector3 m_scaleSpring;

	// Token: 0x0400153E RID: 5438
	private bool m_springActive;

	// Token: 0x0400153F RID: 5439
	private Vector3 m_startScale;

	// Token: 0x04001540 RID: 5440
	private float m_startHeight;

	// Token: 0x04001541 RID: 5441
	public GameObject m_enableObject;

	// Token: 0x04001542 RID: 5442
	public string m_hoverName = "Oddity";

	// Token: 0x04001543 RID: 5443
	public string m_globalKeyCondition = "";

	// Token: 0x04001544 RID: 5444
	public float m_appearDelay;

	// Token: 0x04001545 RID: 5445
	public string m_animatorBool;

	// Token: 0x04001546 RID: 5446
	public EffectList m_showEffects = new EffectList();

	// Token: 0x04001547 RID: 5447
	[Header("Drop Settings")]
	public bool m_dropEnabled;

	// Token: 0x04001548 RID: 5448
	public float m_dropHeight = 1f;

	// Token: 0x04001549 RID: 5449
	public float m_dropTime = 0.5f;

	// Token: 0x0400154A RID: 5450
	public float m_dropTimeVariance;

	// Token: 0x0400154B RID: 5451
	private float m_dropTimeActual;

	// Token: 0x0400154C RID: 5452
	public AnimationCurve m_dropCurve = AnimationCurve.Linear(0f, 1f, 0f, 1f);

	// Token: 0x0400154D RID: 5453
	[Header("Spring Settings")]
	public bool m_springEnabled;

	// Token: 0x0400154E RID: 5454
	public float m_springDisableTime = 3f;

	// Token: 0x0400154F RID: 5455
	[Min(0f)]
	public float m_springDamping = 8f;

	// Token: 0x04001550 RID: 5456
	[Min(0f)]
	public float m_springStiffness = 180f;

	// Token: 0x04001551 RID: 5457
	public Vector3 m_startSpringVelocity = new Vector3(1.5f, -1f, 1.5f);
}
