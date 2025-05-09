using System;
using UnityEngine;

// Token: 0x02000056 RID: 86
public class ImpactEffect : MonoBehaviour
{
	// Token: 0x0600061E RID: 1566 RVA: 0x00033AE4 File Offset: 0x00031CE4
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		if (this.m_maxVelocity < this.m_minVelocity)
		{
			this.m_maxVelocity = this.m_minVelocity;
		}
	}

	// Token: 0x0600061F RID: 1567 RVA: 0x00033B18 File Offset: 0x00031D18
	public void OnCollisionEnter(Collision info)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview && !this.m_nview.IsOwner())
		{
			return;
		}
		if (info.contacts.Length == 0)
		{
			return;
		}
		if (!this.m_hitEffectEnabled)
		{
			return;
		}
		if ((this.m_triggerMask.value & 1 << info.collider.gameObject.layer) == 0)
		{
			return;
		}
		float magnitude = info.relativeVelocity.magnitude;
		if (magnitude < this.m_minVelocity)
		{
			return;
		}
		ContactPoint contactPoint = info.contacts[0];
		Vector3 point = contactPoint.point;
		Vector3 pointVelocity = this.m_body.GetPointVelocity(point);
		this.m_hitEffectEnabled = false;
		base.Invoke("ResetHitTimer", this.m_interval);
		if (this.m_damages.HaveDamage())
		{
			GameObject gameObject = Projectile.FindHitObject(contactPoint.otherCollider);
			float num = Utils.LerpStep(this.m_minVelocity, this.m_maxVelocity, magnitude);
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component != null)
			{
				Character character = component as Character;
				if (character)
				{
					if (!this.m_damagePlayers && character.IsPlayer())
					{
						return;
					}
					float num2 = Vector3.Dot(-info.relativeVelocity.normalized, pointVelocity);
					if (num2 < this.m_minVelocity)
					{
						return;
					}
					ZLog.Log("Rel vel " + num2.ToString());
					num = Utils.LerpStep(this.m_minVelocity, this.m_maxVelocity, num2);
					if (character.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.DoubleImpactDamage))
					{
						num *= 2f;
					}
				}
				if (!this.m_damageFish && gameObject.GetComponent<Fish>())
				{
					return;
				}
				HitData hitData = new HitData();
				hitData.m_point = point;
				hitData.m_dir = pointVelocity.normalized;
				hitData.m_hitCollider = info.collider;
				hitData.m_toolTier = (short)this.m_toolTier;
				hitData.m_hitType = this.m_hitType;
				hitData.m_damage = this.m_damages.Clone();
				hitData.m_damage.Modify(num);
				component.Damage(hitData);
			}
			if (this.m_damageToSelf)
			{
				IDestructible component2 = base.GetComponent<IDestructible>();
				if (component2 != null)
				{
					HitData hitData2 = new HitData();
					hitData2.m_point = point;
					hitData2.m_dir = -pointVelocity.normalized;
					hitData2.m_toolTier = (short)this.m_toolTier;
					hitData2.m_hitType = this.m_hitType;
					hitData2.m_damage = this.m_damages.Clone();
					hitData2.m_damage.Modify(num);
					component2.Damage(hitData2);
				}
			}
		}
		Vector3 rhs = Vector3.Cross(-Vector3.Normalize(info.relativeVelocity), contactPoint.normal);
		Vector3 vector = Vector3.Cross(contactPoint.normal, rhs);
		Quaternion baseRot = Quaternion.identity;
		if (vector != Vector3.zero && contactPoint.normal != Vector3.zero)
		{
			baseRot = Quaternion.LookRotation(vector, contactPoint.normal);
		}
		this.m_hitEffect.Create(point, baseRot, null, 1f, -1);
		if (this.m_firstHit && this.m_hitDestroyChance > 0f && UnityEngine.Random.value <= this.m_hitDestroyChance)
		{
			this.m_destroyEffect.Create(point, baseRot, null, 1f, -1);
			GameObject gameObject2 = base.gameObject;
			if (base.transform.parent)
			{
				Animator componentInParent = base.transform.GetComponentInParent<Animator>();
				if (componentInParent)
				{
					gameObject2 = componentInParent.gameObject;
				}
			}
			UnityEngine.Object.Destroy(gameObject2);
		}
		this.m_firstHit = false;
	}

	// Token: 0x06000620 RID: 1568 RVA: 0x00033EA8 File Offset: 0x000320A8
	private Vector3 GetAVGPos(ContactPoint[] points)
	{
		ZLog.Log("Pooints " + points.Length.ToString());
		Vector3 vector = Vector3.zero;
		foreach (ContactPoint contactPoint in points)
		{
			ZLog.Log("P " + contactPoint.otherCollider.gameObject.name);
			vector += contactPoint.point;
		}
		return vector;
	}

	// Token: 0x06000621 RID: 1569 RVA: 0x00033F1C File Offset: 0x0003211C
	private void ResetHitTimer()
	{
		this.m_hitEffectEnabled = true;
	}

	// Token: 0x04000707 RID: 1799
	public EffectList m_hitEffect = new EffectList();

	// Token: 0x04000708 RID: 1800
	public EffectList m_destroyEffect = new EffectList();

	// Token: 0x04000709 RID: 1801
	public float m_hitDestroyChance;

	// Token: 0x0400070A RID: 1802
	public float m_minVelocity;

	// Token: 0x0400070B RID: 1803
	public float m_maxVelocity;

	// Token: 0x0400070C RID: 1804
	public bool m_damageToSelf;

	// Token: 0x0400070D RID: 1805
	public bool m_damagePlayers = true;

	// Token: 0x0400070E RID: 1806
	public bool m_damageFish;

	// Token: 0x0400070F RID: 1807
	public HitData.HitType m_hitType = HitData.HitType.Impact;

	// Token: 0x04000710 RID: 1808
	public int m_toolTier;

	// Token: 0x04000711 RID: 1809
	public HitData.DamageTypes m_damages;

	// Token: 0x04000712 RID: 1810
	public LayerMask m_triggerMask;

	// Token: 0x04000713 RID: 1811
	public float m_interval = 0.5f;

	// Token: 0x04000714 RID: 1812
	private bool m_firstHit = true;

	// Token: 0x04000715 RID: 1813
	private bool m_hitEffectEnabled = true;

	// Token: 0x04000716 RID: 1814
	private ZNetView m_nview;

	// Token: 0x04000717 RID: 1815
	private Rigidbody m_body;
}
