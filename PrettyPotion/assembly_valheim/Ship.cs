using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Token: 0x020001B4 RID: 436
public class Ship : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06001978 RID: 6520 RVA: 0x000BE3E8 File Offset: 0x000BC5E8
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_destructible = base.GetComponent<IDestructible>();
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
		}
		this.m_body.maxDepenetrationVelocity = 2f;
		Heightmap.ForceGenerateAll();
		this.m_sailCloth = this.m_sailObject.GetComponentInChildren<Cloth>();
		if (this.m_sailCloth)
		{
			this.m_globalWind = this.m_sailCloth.gameObject.GetComponent<GlobalWind>();
		}
		if (this.m_ashdamageEffects)
		{
			this.m_ashdamageEffects.SetActive(false);
			this.m_ashlandsFxAudio = this.m_ashdamageEffects.GetComponentsInChildren<AudioSource>().ToList<AudioSource>();
		}
	}

	// Token: 0x06001979 RID: 6521 RVA: 0x000BE4D6 File Offset: 0x000BC6D6
	private void OnEnable()
	{
		Ship.Instances.Add(this);
	}

	// Token: 0x0600197A RID: 6522 RVA: 0x000BE4E3 File Offset: 0x000BC6E3
	private void OnDisable()
	{
		Ship.Instances.Remove(this);
	}

	// Token: 0x0600197B RID: 6523 RVA: 0x000BE4F1 File Offset: 0x000BC6F1
	public bool CanBeRemoved()
	{
		return this.m_players.Count == 0;
	}

	// Token: 0x0600197C RID: 6524 RVA: 0x000BE504 File Offset: 0x000BC704
	private void Start()
	{
		this.m_nview.Register("Stop", new Action<long>(this.RPC_Stop));
		this.m_nview.Register("Forward", new Action<long>(this.RPC_Forward));
		this.m_nview.Register("Backward", new Action<long>(this.RPC_Backward));
		this.m_nview.Register<float>("Rudder", new Action<long, float>(this.RPC_Rudder));
		base.InvokeRepeating("UpdateOwner", 2f, 2f);
	}

	// Token: 0x0600197D RID: 6525 RVA: 0x000BE598 File Offset: 0x000BC798
	private void PrintStats()
	{
		if (this.m_players.Count == 0)
		{
			return;
		}
		ZLog.Log("Vel:" + this.m_body.velocity.magnitude.ToString("0.0"));
	}

	// Token: 0x0600197E RID: 6526 RVA: 0x000BE5E4 File Offset: 0x000BC7E4
	public void ApplyControlls(Vector3 dir)
	{
		bool flag = (double)dir.z > 0.5;
		bool flag2 = (double)dir.z < -0.5;
		if (flag && !this.m_forwardPressed)
		{
			this.Forward();
		}
		if (flag2 && !this.m_backwardPressed)
		{
			this.Backward();
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		float num = Mathf.Lerp(0.5f, 1f, Mathf.Abs(this.m_rudderValue));
		this.m_rudder = dir.x * num;
		this.m_rudderValue += this.m_rudder * this.m_rudderSpeed * fixedDeltaTime;
		this.m_rudderValue = Utils.Clamp(this.m_rudderValue, -1f, 1f);
		if (Time.time - this.m_sendRudderTime > 0.2f)
		{
			this.m_sendRudderTime = Time.time;
			this.m_nview.InvokeRPC("Rudder", new object[]
			{
				this.m_rudderValue
			});
		}
		this.m_forwardPressed = flag;
		this.m_backwardPressed = flag2;
	}

	// Token: 0x0600197F RID: 6527 RVA: 0x000BE6EF File Offset: 0x000BC8EF
	public void Forward()
	{
		this.m_nview.InvokeRPC("Forward", Array.Empty<object>());
	}

	// Token: 0x06001980 RID: 6528 RVA: 0x000BE706 File Offset: 0x000BC906
	public void Backward()
	{
		this.m_nview.InvokeRPC("Backward", Array.Empty<object>());
	}

	// Token: 0x06001981 RID: 6529 RVA: 0x000BE71D File Offset: 0x000BC91D
	public void Rudder(float rudder)
	{
		this.m_nview.Invoke("Rudder", rudder);
	}

	// Token: 0x06001982 RID: 6530 RVA: 0x000BE730 File Offset: 0x000BC930
	private void RPC_Rudder(long sender, float value)
	{
		this.m_rudderValue = value;
	}

	// Token: 0x06001983 RID: 6531 RVA: 0x000BE739 File Offset: 0x000BC939
	public void Stop()
	{
		this.m_nview.InvokeRPC("Stop", Array.Empty<object>());
	}

	// Token: 0x06001984 RID: 6532 RVA: 0x000BE750 File Offset: 0x000BC950
	private void RPC_Stop(long sender)
	{
		this.m_speed = Ship.Speed.Stop;
	}

	// Token: 0x06001985 RID: 6533 RVA: 0x000BE75C File Offset: 0x000BC95C
	private void RPC_Forward(long sender)
	{
		switch (this.m_speed)
		{
		case Ship.Speed.Stop:
			this.m_speed = Ship.Speed.Slow;
			return;
		case Ship.Speed.Back:
			this.m_speed = Ship.Speed.Stop;
			break;
		case Ship.Speed.Slow:
			this.m_speed = Ship.Speed.Half;
			return;
		case Ship.Speed.Half:
			this.m_speed = Ship.Speed.Full;
			return;
		case Ship.Speed.Full:
			break;
		default:
			return;
		}
	}

	// Token: 0x06001986 RID: 6534 RVA: 0x000BE7AC File Offset: 0x000BC9AC
	private void RPC_Backward(long sender)
	{
		switch (this.m_speed)
		{
		case Ship.Speed.Stop:
			this.m_speed = Ship.Speed.Back;
			return;
		case Ship.Speed.Back:
			break;
		case Ship.Speed.Slow:
			this.m_speed = Ship.Speed.Stop;
			return;
		case Ship.Speed.Half:
			this.m_speed = Ship.Speed.Slow;
			return;
		case Ship.Speed.Full:
			this.m_speed = Ship.Speed.Half;
			break;
		default:
			return;
		}
	}

	// Token: 0x06001987 RID: 6535 RVA: 0x000BE7FC File Offset: 0x000BC9FC
	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		bool flag = this.HaveControllingPlayer();
		this.UpdateControlls(fixedDeltaTime);
		this.UpdateSail(fixedDeltaTime);
		this.UpdateRudder(fixedDeltaTime, flag);
		if (this.m_nview && !this.m_nview.IsOwner())
		{
			return;
		}
		this.UpdateUpsideDmg(fixedDeltaTime);
		this.TakeAshlandsDamage(fixedDeltaTime);
		if (this.m_players.Count == 0)
		{
			this.m_speed = Ship.Speed.Stop;
			this.m_rudderValue = 0f;
		}
		if (!flag && (this.m_speed == Ship.Speed.Slow || this.m_speed == Ship.Speed.Back))
		{
			this.m_speed = Ship.Speed.Stop;
		}
		Vector3 worldCenterOfMass = this.m_body.worldCenterOfMass;
		Transform transform = this.m_floatCollider.transform;
		Vector3 size = this.m_floatCollider.size;
		Vector3 position = transform.position;
		Vector3 forward = transform.forward;
		Vector3 right = transform.right;
		Vector3 vector = position + forward * size.z / 2f;
		Vector3 vector2 = position - forward * size.z / 2f;
		Vector3 vector3 = position - right * size.x / 2f;
		Vector3 vector4 = position + right * size.x / 2f;
		Transform transform2 = base.transform;
		Vector3 forward2 = transform2.forward;
		Vector3 right2 = transform2.right;
		float waterLevel = Floating.GetWaterLevel(worldCenterOfMass, ref this.m_previousCenter);
		float waterLevel2 = Floating.GetWaterLevel(vector3, ref this.m_previousLeft);
		float waterLevel3 = Floating.GetWaterLevel(vector4, ref this.m_previousRight);
		float waterLevel4 = Floating.GetWaterLevel(vector, ref this.m_previousForward);
		float waterLevel5 = Floating.GetWaterLevel(vector2, ref this.m_previousBack);
		float num = (waterLevel + waterLevel2 + waterLevel3 + waterLevel4 + waterLevel5) / 5f;
		float num2 = worldCenterOfMass.y - num - this.m_waterLevelOffset;
		if (num2 > this.m_disableLevel)
		{
			return;
		}
		this.m_body.WakeUp();
		this.UpdateWaterForce(num2, Time.time);
		ref Vector3 ptr = new Vector3(vector3.x, waterLevel2, vector3.z);
		Vector3 vector5 = new Vector3(vector4.x, waterLevel3, vector4.z);
		ref Vector3 ptr2 = new Vector3(vector.x, waterLevel4, vector.z);
		Vector3 vector6 = new Vector3(vector2.x, waterLevel5, vector2.z);
		float num3 = fixedDeltaTime * 50f;
		Vector3 vector7 = this.m_body.velocity;
		float magnitude = vector7.magnitude;
		float num4 = Utils.Clamp01(Utils.Abs(num2) / this.m_forceDistance);
		Vector3 a = this.m_force * num4 * Vector3.up;
		this.m_body.AddForceAtPosition(a * num3, worldCenterOfMass, ForceMode.VelocityChange);
		float num5 = Vector3.Dot(vector7, forward2);
		float num6 = Vector3.Dot(vector7, right2);
		float num7 = vector7.y * vector7.y * Utils.Sign(vector7.y) * this.m_damping * num4;
		float num8 = num5 * num5 * Utils.Sign(num5) * this.m_dampingForward * num4;
		float num9 = num6 * num6 * Utils.Sign(num6) * this.m_dampingSideway * num4;
		vector7.y -= Utils.Clamp(num7, -1f, 1f);
		vector7 -= base.transform.forward * Utils.Clamp(num8, -1f, 1f);
		vector7 -= base.transform.right * Utils.Clamp(num9, -1f, 1f);
		if (vector7.magnitude > this.m_body.velocity.magnitude)
		{
			vector7 = vector7.normalized * this.m_body.velocity.magnitude;
		}
		if (this.m_players.Count == 0)
		{
			vector7.x *= 0.1f;
			vector7.z *= 0.1f;
		}
		this.m_body.velocity = vector7;
		this.m_body.angularVelocity -= this.m_body.angularVelocity * (this.m_angularDamping * num4);
		float num10 = 0.15f;
		float num11 = 0.5f;
		float num12 = Utils.Clamp((ptr2.y - vector.y) * num10, -num11, num11);
		float num13 = Utils.Clamp((vector6.y - vector2.y) * num10, -num11, num11);
		float num14 = Utils.Clamp((ptr.y - vector3.y) * num10, -num11, num11);
		float num15 = Utils.Clamp((vector5.y - vector4.y) * num10, -num11, num11);
		num12 = Utils.Sign(num12) * Utils.Abs(num12 * num12);
		num13 = Utils.Sign(num13) * Utils.Abs(num13 * num13);
		num14 = Utils.Sign(num14) * Utils.Abs(num14 * num14);
		num15 = Utils.Sign(num15) * Utils.Abs(num15 * num15);
		this.m_body.AddForceAtPosition(num12 * num3 * Vector3.up, vector, ForceMode.VelocityChange);
		this.m_body.AddForceAtPosition(num13 * num3 * Vector3.up, vector2, ForceMode.VelocityChange);
		this.m_body.AddForceAtPosition(num14 * num3 * Vector3.up, vector3, ForceMode.VelocityChange);
		this.m_body.AddForceAtPosition(num15 * num3 * Vector3.up, vector4, ForceMode.VelocityChange);
		float sailSize = 0f;
		if (this.m_speed == Ship.Speed.Full)
		{
			sailSize = 1f;
		}
		else if (this.m_speed == Ship.Speed.Half)
		{
			sailSize = 0.5f;
		}
		Vector3 sailForce = this.GetSailForce(sailSize, fixedDeltaTime);
		Vector3 position2 = worldCenterOfMass + base.transform.up * this.m_sailForceOffset;
		this.m_body.AddForceAtPosition(sailForce, position2, ForceMode.VelocityChange);
		Vector3 position3 = base.transform.position + forward2 * this.m_stearForceOffset;
		float num16 = num5 * this.m_stearVelForceFactor;
		this.m_body.AddForceAtPosition(num16 * -this.m_rudderValue * fixedDeltaTime * right2, position3, ForceMode.VelocityChange);
		Vector3 a2 = Vector3.zero;
		Ship.Speed speed = this.m_speed;
		if (speed != Ship.Speed.Back)
		{
			if (speed == Ship.Speed.Slow)
			{
				a2 += forward2 * (this.m_backwardForce * (1f - Utils.Abs(this.m_rudderValue)));
			}
		}
		else
		{
			a2 += -forward2 * (this.m_backwardForce * (1f - Utils.Abs(this.m_rudderValue)));
		}
		if (this.m_speed == Ship.Speed.Back || this.m_speed == Ship.Speed.Slow)
		{
			float num17 = (float)((this.m_speed == Ship.Speed.Back) ? -1 : 1);
			a2 += base.transform.right * (this.m_stearForce * -this.m_rudderValue * num17);
		}
		this.m_body.AddForceAtPosition(a2 * fixedDeltaTime, position3, ForceMode.VelocityChange);
		this.ApplyEdgeForce(fixedDeltaTime);
	}

	// Token: 0x06001988 RID: 6536 RVA: 0x000BEEF0 File Offset: 0x000BD0F0
	private void UpdateUpsideDmg(float dt)
	{
		if (base.transform.up.y >= 0f)
		{
			return;
		}
		this.m_upsideDownDmgTimer += dt;
		if (this.m_upsideDownDmgTimer <= this.m_upsideDownDmgInterval)
		{
			return;
		}
		this.m_upsideDownDmgTimer = 0f;
		HitData hitData = new HitData();
		hitData.m_damage.m_blunt = this.m_upsideDownDmg;
		hitData.m_point = base.transform.position;
		hitData.m_dir = Vector3.up;
		this.m_destructible.Damage(hitData);
	}

	// Token: 0x06001989 RID: 6537 RVA: 0x000BEF7C File Offset: 0x000BD17C
	private void TakeAshlandsDamage(float dt)
	{
		if (this.m_ashlandsReady)
		{
			return;
		}
		float num = WorldGenerator.GetAshlandsOceanGradient(base.transform.position);
		if (this.m_ashdamageEffects)
		{
			if (num < 0f)
			{
				this.m_ashdamageEffects.SetActive(false);
				foreach (AudioSource audioSource in this.m_ashlandsFxAudio)
				{
					audioSource.Stop();
				}
				return;
			}
			this.m_ashdamageEffects.SetActive(true);
		}
		if (this.m_ashDamageMsgTimer <= 0f && ZoneSystem.instance && Player.m_localPlayer)
		{
			ZoneSystem.instance.SetGlobalKey(GlobalKeys.AshlandsOcean);
			this.m_ashDamageMsgTimer = this.m_ashDamageMsgTime;
		}
		else
		{
			this.m_ashDamageMsgTimer -= Time.fixedDeltaTime;
		}
		this.m_ashlandsDmgTimer += dt;
		if ((double)this.m_ashlandsDmgTimer <= 1.0)
		{
			return;
		}
		this.m_ashlandsDmgTimer = 0f;
		num = Utils.Clamp(num, 0f, 3f);
		HitData hitData = new HitData();
		hitData.m_damage.m_blunt = Mathf.Floor(Mathf.Lerp(1f, 30f, num));
		hitData.m_hitType = HitData.HitType.AshlandsOcean;
		hitData.m_point = base.transform.position;
		hitData.m_dir = Vector3.up;
		this.m_destructible.Damage(hitData);
	}

	// Token: 0x0600198A RID: 6538 RVA: 0x000BF0F8 File Offset: 0x000BD2F8
	private Vector3 GetSailForce(float sailSize, float dt)
	{
		Vector3 windDir = EnvMan.instance.GetWindDir();
		float windIntensity = EnvMan.instance.GetWindIntensity();
		float num = Mathf.Lerp(0.25f, 1f, windIntensity);
		float num2 = this.GetWindAngleFactor();
		num2 *= num;
		Vector3 target = Vector3.Normalize(windDir + base.transform.forward) * (num2 * this.m_sailForceFactor * sailSize);
		this.m_sailForce = Vector3.SmoothDamp(this.m_sailForce, target, ref this.m_windChangeVelocity, 1f, 99f);
		return this.m_sailForce;
	}

	// Token: 0x0600198B RID: 6539 RVA: 0x000BF184 File Offset: 0x000BD384
	public float GetWindAngleFactor()
	{
		float num = Vector3.Dot(EnvMan.instance.GetWindDir(), -base.transform.forward);
		float num2 = Mathf.Lerp(0.7f, 1f, 1f - Utils.Abs(num));
		float num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
		return num2 * num3;
	}

	// Token: 0x0600198C RID: 6540 RVA: 0x000BF1E8 File Offset: 0x000BD3E8
	private void UpdateWaterForce(float depth, float time)
	{
		float num = depth - this.m_lastDepth;
		float num2 = time - this.m_lastUpdateWaterForceTime;
		this.m_lastDepth = depth;
		this.m_lastUpdateWaterForceTime = time;
		float num3 = num / num2;
		if (num3 > 0f)
		{
			return;
		}
		if (Utils.Abs(num3) > this.m_minWaterImpactForce && time - this.m_lastWaterImpactTime > this.m_minWaterImpactInterval)
		{
			this.m_lastWaterImpactTime = time;
			this.m_waterImpactEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			if (this.m_players.Count > 0)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_blunt = this.m_waterImpactDamage;
				hitData.m_point = base.transform.position;
				hitData.m_dir = Vector3.up;
				this.m_destructible.Damage(hitData);
			}
		}
	}

	// Token: 0x0600198D RID: 6541 RVA: 0x000BF2C0 File Offset: 0x000BD4C0
	private void ApplyEdgeForce(float dt)
	{
		float magnitude = base.transform.position.magnitude;
		float num = 10420f;
		if (magnitude > num)
		{
			Vector3 a = Vector3.Normalize(base.transform.position);
			float d = Utils.LerpStep(num, 10500f, magnitude) * 8f;
			Vector3 a2 = a * d;
			this.m_body.AddForce(a2 * dt, ForceMode.VelocityChange);
		}
	}

	// Token: 0x0600198E RID: 6542 RVA: 0x000BF32C File Offset: 0x000BD52C
	private void FixTilt()
	{
		float num = Mathf.Asin(base.transform.right.y);
		float num2 = Mathf.Asin(base.transform.forward.y);
		if (Utils.Abs(num) > 0.5235988f)
		{
			if (num > 0f)
			{
				base.transform.RotateAround(base.transform.position, base.transform.forward, -Time.fixedDeltaTime * 20f);
			}
			else
			{
				base.transform.RotateAround(base.transform.position, base.transform.forward, Time.fixedDeltaTime * 20f);
			}
		}
		if (Utils.Abs(num2) > 0.5235988f)
		{
			if (num2 > 0f)
			{
				base.transform.RotateAround(base.transform.position, base.transform.right, -Time.fixedDeltaTime * 20f);
				return;
			}
			base.transform.RotateAround(base.transform.position, base.transform.right, Time.fixedDeltaTime * 20f);
		}
	}

	// Token: 0x0600198F RID: 6543 RVA: 0x000BF444 File Offset: 0x000BD644
	private void UpdateControlls(float dt)
	{
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_forward, (int)this.m_speed, false);
			this.m_nview.GetZDO().Set(ZDOVars.s_rudder, this.m_rudderValue);
			return;
		}
		this.m_speed = (Ship.Speed)this.m_nview.GetZDO().GetInt(ZDOVars.s_forward, 0);
		if (Time.time - this.m_sendRudderTime > 1f)
		{
			this.m_rudderValue = this.m_nview.GetZDO().GetFloat(ZDOVars.s_rudder, 0f);
		}
	}

	// Token: 0x06001990 RID: 6544 RVA: 0x000BF4E5 File Offset: 0x000BD6E5
	public bool IsSailUp()
	{
		return this.m_speed == Ship.Speed.Half || this.m_speed == Ship.Speed.Full;
	}

	// Token: 0x06001991 RID: 6545 RVA: 0x000BF4FC File Offset: 0x000BD6FC
	private void UpdateSail(float dt)
	{
		this.UpdateSailSize(dt);
		Vector3 vector = EnvMan.instance.GetWindDir();
		vector = Vector3.Cross(Vector3.Cross(vector, base.transform.up), base.transform.up);
		if (this.m_speed == Ship.Speed.Full || this.m_speed == Ship.Speed.Half)
		{
			float t = 0.5f + Vector3.Dot(base.transform.forward, vector) * 0.5f;
			Quaternion to = Quaternion.LookRotation(-Vector3.Lerp(vector, Vector3.Normalize(vector - base.transform.forward), t), base.transform.up);
			this.m_mastObject.transform.rotation = Quaternion.RotateTowards(this.m_mastObject.transform.rotation, to, 30f * dt);
			return;
		}
		if (this.m_speed == Ship.Speed.Back)
		{
			Quaternion from = Quaternion.LookRotation(-base.transform.forward, base.transform.up);
			Quaternion to2 = Quaternion.LookRotation(-vector, base.transform.up);
			to2 = Quaternion.RotateTowards(from, to2, 80f);
			this.m_mastObject.transform.rotation = Quaternion.RotateTowards(this.m_mastObject.transform.rotation, to2, 30f * dt);
		}
	}

	// Token: 0x06001992 RID: 6546 RVA: 0x000BF648 File Offset: 0x000BD848
	private void UpdateRudder(float dt, bool haveControllingPlayer)
	{
		if (!this.m_rudderObject)
		{
			return;
		}
		Quaternion quaternion = Quaternion.Euler(0f, this.m_rudderRotationMax * -this.m_rudderValue, 0f);
		if (haveControllingPlayer)
		{
			if (this.m_speed == Ship.Speed.Slow)
			{
				this.m_rudderPaddleTimer += dt;
				quaternion *= Quaternion.Euler(0f, Mathf.Sin(this.m_rudderPaddleTimer * 6f) * 20f, 0f);
			}
			else if (this.m_speed == Ship.Speed.Back)
			{
				this.m_rudderPaddleTimer += dt;
				quaternion *= Quaternion.Euler(0f, Mathf.Sin(this.m_rudderPaddleTimer * -3f) * 40f, 0f);
			}
		}
		this.m_rudderObject.transform.localRotation = Quaternion.Slerp(this.m_rudderObject.transform.localRotation, quaternion, 0.5f);
	}

	// Token: 0x06001993 RID: 6547 RVA: 0x000BF73C File Offset: 0x000BD93C
	private void UpdateSailSize(float dt)
	{
		float num = 0f;
		switch (this.m_speed)
		{
		case Ship.Speed.Stop:
			num = 0.1f;
			break;
		case Ship.Speed.Back:
			num = 0.1f;
			break;
		case Ship.Speed.Slow:
			num = 0.1f;
			break;
		case Ship.Speed.Half:
			num = 0.5f;
			break;
		case Ship.Speed.Full:
			num = 1f;
			break;
		}
		Vector3 localScale = this.m_sailObject.transform.localScale;
		bool flag = Utils.Abs(localScale.y - num) < 0.01f;
		if (!flag)
		{
			localScale.y = Mathf.MoveTowards(localScale.y, num, dt);
			this.m_sailObject.transform.localScale = localScale;
		}
		if (this.m_sailCloth)
		{
			if (this.m_speed == Ship.Speed.Stop || this.m_speed == Ship.Speed.Slow || this.m_speed == Ship.Speed.Back)
			{
				if (flag && this.m_sailCloth.enabled)
				{
					this.m_sailCloth.enabled = false;
				}
			}
			else if (flag)
			{
				if (!this.m_sailWasInPosition)
				{
					Utils.RecreateComponent(ref this.m_sailCloth);
					if (this.m_globalWind)
					{
						this.m_globalWind.UpdateClothReference(this.m_sailCloth);
					}
				}
			}
			else
			{
				this.m_sailCloth.enabled = true;
			}
		}
		this.m_sailWasInPosition = flag;
	}

	// Token: 0x06001994 RID: 6548 RVA: 0x000BF874 File Offset: 0x000BDA74
	private void UpdateOwner()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (Player.m_localPlayer == null)
		{
			return;
		}
		if (this.m_players.Count > 0 && !this.IsPlayerInBoat(Player.m_localPlayer))
		{
			this.RefreshPlayerList();
			long newOwnerID = this.GetNewOwnerID();
			this.m_nview.GetZDO().SetOwner(newOwnerID);
			ZLog.Log("Changing ship owner to " + newOwnerID.ToString());
		}
	}

	// Token: 0x06001995 RID: 6549 RVA: 0x000BF8FC File Offset: 0x000BDAFC
	private long GetNewOwnerID()
	{
		long num = 0L;
		for (int i = 0; i < this.m_players.Count; i++)
		{
			num = this.m_players[i].GetOwner();
			if (num != 0L)
			{
				break;
			}
		}
		if (num == 0L)
		{
			num = ZDOMan.GetSessionID();
		}
		return num;
	}

	// Token: 0x06001996 RID: 6550 RVA: 0x000BF944 File Offset: 0x000BDB44
	private void RefreshPlayerList()
	{
		for (int i = 0; i < this.m_players.Count; i++)
		{
			if (this.m_players[i].GetOwner() == 0L)
			{
				this.m_players.RemoveAt(i);
			}
		}
	}

	// Token: 0x06001997 RID: 6551 RVA: 0x000BF988 File Offset: 0x000BDB88
	private void OnTriggerEnter(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (component)
		{
			this.m_players.Add(component);
			ZLog.Log("Player onboard, total onboard " + this.m_players.Count.ToString());
			if (component == Player.m_localPlayer)
			{
				Ship.s_currentShips.Add(this);
			}
		}
		Character component2 = collider.GetComponent<Character>();
		if (component2)
		{
			Character character = component2;
			int inNumShipVolumes = character.InNumShipVolumes;
			character.InNumShipVolumes = inNumShipVolumes + 1;
		}
	}

	// Token: 0x06001998 RID: 6552 RVA: 0x000BFA0C File Offset: 0x000BDC0C
	private void OnTriggerExit(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (component)
		{
			this.m_players.Remove(component);
			ZLog.Log("Player over board, players left " + this.m_players.Count.ToString());
			if (component == Player.m_localPlayer)
			{
				Ship.s_currentShips.Remove(this);
			}
		}
		Character component2 = collider.GetComponent<Character>();
		if (component2)
		{
			Character character = component2;
			int inNumShipVolumes = character.InNumShipVolumes;
			character.InNumShipVolumes = inNumShipVolumes - 1;
		}
	}

	// Token: 0x06001999 RID: 6553 RVA: 0x000BFA90 File Offset: 0x000BDC90
	public bool IsPlayerInBoat(ZDOID zdoid)
	{
		using (List<Player>.Enumerator enumerator = this.m_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.GetZDOID() == zdoid)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x0600199A RID: 6554 RVA: 0x000BFAF0 File Offset: 0x000BDCF0
	public bool IsPlayerInBoat(Player player)
	{
		return this.m_players.Contains(player);
	}

	// Token: 0x0600199B RID: 6555 RVA: 0x000BFB00 File Offset: 0x000BDD00
	public bool IsPlayerInBoat(long playerID)
	{
		using (List<Player>.Enumerator enumerator = this.m_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.GetPlayerID() == playerID)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x0600199C RID: 6556 RVA: 0x000BFB5C File Offset: 0x000BDD5C
	public bool HasPlayerOnboard()
	{
		return this.m_players.Count > 0;
	}

	// Token: 0x0600199D RID: 6557 RVA: 0x000BFB6C File Offset: 0x000BDD6C
	private void OnDestroyed()
	{
		if (this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			Gogan.LogEvent("Game", "ShipDestroyed", base.gameObject.name, 0L);
		}
		Ship.s_currentShips.Remove(this);
	}

	// Token: 0x0600199E RID: 6558 RVA: 0x000BFBBC File Offset: 0x000BDDBC
	public bool IsWindControllActive()
	{
		using (List<Player>.Enumerator enumerator = this.m_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.SailingPower))
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x0600199F RID: 6559 RVA: 0x000BFC1C File Offset: 0x000BDE1C
	public static Ship GetLocalShip()
	{
		if (Ship.s_currentShips.Count != 0)
		{
			return Ship.s_currentShips[Ship.s_currentShips.Count - 1];
		}
		return null;
	}

	// Token: 0x060019A0 RID: 6560 RVA: 0x000BFC42 File Offset: 0x000BDE42
	private bool HaveControllingPlayer()
	{
		return this.m_players.Count != 0 && this.m_shipControlls.HaveValidUser();
	}

	// Token: 0x060019A1 RID: 6561 RVA: 0x000BFC5E File Offset: 0x000BDE5E
	public bool IsOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	// Token: 0x060019A2 RID: 6562 RVA: 0x000BFC7A File Offset: 0x000BDE7A
	public float GetSpeed()
	{
		return Vector3.Dot(this.m_body.velocity, base.transform.forward);
	}

	// Token: 0x060019A3 RID: 6563 RVA: 0x000BFC97 File Offset: 0x000BDE97
	public Ship.Speed GetSpeedSetting()
	{
		return this.m_speed;
	}

	// Token: 0x060019A4 RID: 6564 RVA: 0x000BFC9F File Offset: 0x000BDE9F
	public float GetRudder()
	{
		return this.m_rudder;
	}

	// Token: 0x060019A5 RID: 6565 RVA: 0x000BFCA7 File Offset: 0x000BDEA7
	public float GetRudderValue()
	{
		return this.m_rudderValue;
	}

	// Token: 0x060019A6 RID: 6566 RVA: 0x000BFCB0 File Offset: 0x000BDEB0
	public float GetShipYawAngle()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return 0f;
		}
		return -Utils.YawFromDirection(mainCamera.transform.InverseTransformDirection(base.transform.forward));
	}

	// Token: 0x060019A7 RID: 6567 RVA: 0x000BFCF0 File Offset: 0x000BDEF0
	public float GetWindAngle()
	{
		Vector3 windDir = EnvMan.instance.GetWindDir();
		return -Utils.YawFromDirection(base.transform.InverseTransformDirection(windDir));
	}

	// Token: 0x060019A8 RID: 6568 RVA: 0x000BFD1C File Offset: 0x000BDF1C
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position + base.transform.forward * this.m_stearForceOffset, 0.25f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position + base.transform.up * this.m_sailForceOffset, 0.25f);
	}

	// Token: 0x170000C9 RID: 201
	// (get) Token: 0x060019A9 RID: 6569 RVA: 0x000BFD9D File Offset: 0x000BDF9D
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x040019F6 RID: 6646
	private bool m_forwardPressed;

	// Token: 0x040019F7 RID: 6647
	private bool m_backwardPressed;

	// Token: 0x040019F8 RID: 6648
	private float m_sendRudderTime;

	// Token: 0x040019F9 RID: 6649
	private float m_ashDamageMsgTimer;

	// Token: 0x040019FA RID: 6650
	public float m_ashDamageMsgTime = 10f;

	// Token: 0x040019FB RID: 6651
	[Header("Objects")]
	public GameObject m_sailObject;

	// Token: 0x040019FC RID: 6652
	public GameObject m_mastObject;

	// Token: 0x040019FD RID: 6653
	public GameObject m_rudderObject;

	// Token: 0x040019FE RID: 6654
	public ShipControlls m_shipControlls;

	// Token: 0x040019FF RID: 6655
	public Transform m_controlGuiPos;

	// Token: 0x04001A00 RID: 6656
	public GameObject m_ashdamageEffects;

	// Token: 0x04001A01 RID: 6657
	[Header("Misc")]
	public BoxCollider m_floatCollider;

	// Token: 0x04001A02 RID: 6658
	public float m_waterLevelOffset;

	// Token: 0x04001A03 RID: 6659
	public float m_forceDistance = 1f;

	// Token: 0x04001A04 RID: 6660
	public float m_force = 0.5f;

	// Token: 0x04001A05 RID: 6661
	public float m_damping = 0.05f;

	// Token: 0x04001A06 RID: 6662
	public float m_dampingSideway = 0.05f;

	// Token: 0x04001A07 RID: 6663
	public float m_dampingForward = 0.01f;

	// Token: 0x04001A08 RID: 6664
	public float m_angularDamping = 0.01f;

	// Token: 0x04001A09 RID: 6665
	public float m_disableLevel = -0.5f;

	// Token: 0x04001A0A RID: 6666
	public float m_sailForceOffset;

	// Token: 0x04001A0B RID: 6667
	public float m_sailForceFactor = 0.1f;

	// Token: 0x04001A0C RID: 6668
	public float m_rudderSpeed = 0.5f;

	// Token: 0x04001A0D RID: 6669
	public float m_stearForceOffset = -10f;

	// Token: 0x04001A0E RID: 6670
	public float m_stearForce = 0.5f;

	// Token: 0x04001A0F RID: 6671
	public float m_stearVelForceFactor = 0.1f;

	// Token: 0x04001A10 RID: 6672
	public float m_backwardForce = 50f;

	// Token: 0x04001A11 RID: 6673
	public float m_rudderRotationMax = 30f;

	// Token: 0x04001A12 RID: 6674
	public float m_minWaterImpactForce = 2.5f;

	// Token: 0x04001A13 RID: 6675
	public float m_minWaterImpactInterval = 2f;

	// Token: 0x04001A14 RID: 6676
	public float m_waterImpactDamage = 10f;

	// Token: 0x04001A15 RID: 6677
	public float m_upsideDownDmgInterval = 1f;

	// Token: 0x04001A16 RID: 6678
	public float m_upsideDownDmg = 20f;

	// Token: 0x04001A17 RID: 6679
	public EffectList m_waterImpactEffect = new EffectList();

	// Token: 0x04001A18 RID: 6680
	public bool m_ashlandsReady;

	// Token: 0x04001A19 RID: 6681
	private bool m_sailWasInPosition;

	// Token: 0x04001A1A RID: 6682
	private Vector3 m_windChangeVelocity = Vector3.zero;

	// Token: 0x04001A1B RID: 6683
	private Ship.Speed m_speed;

	// Token: 0x04001A1C RID: 6684
	private float m_rudder;

	// Token: 0x04001A1D RID: 6685
	private float m_rudderValue;

	// Token: 0x04001A1E RID: 6686
	private Vector3 m_sailForce = Vector3.zero;

	// Token: 0x04001A1F RID: 6687
	private readonly List<Player> m_players = new List<Player>();

	// Token: 0x04001A20 RID: 6688
	private List<AudioSource> m_ashlandsFxAudio;

	// Token: 0x04001A21 RID: 6689
	private WaterVolume m_previousCenter;

	// Token: 0x04001A22 RID: 6690
	private WaterVolume m_previousLeft;

	// Token: 0x04001A23 RID: 6691
	private WaterVolume m_previousRight;

	// Token: 0x04001A24 RID: 6692
	private WaterVolume m_previousForward;

	// Token: 0x04001A25 RID: 6693
	private WaterVolume m_previousBack;

	// Token: 0x04001A26 RID: 6694
	private static readonly List<Ship> s_currentShips = new List<Ship>();

	// Token: 0x04001A27 RID: 6695
	private GlobalWind m_globalWind;

	// Token: 0x04001A28 RID: 6696
	private Rigidbody m_body;

	// Token: 0x04001A29 RID: 6697
	private ZNetView m_nview;

	// Token: 0x04001A2A RID: 6698
	private IDestructible m_destructible;

	// Token: 0x04001A2B RID: 6699
	private Cloth m_sailCloth;

	// Token: 0x04001A2C RID: 6700
	private float m_lastDepth = -9999f;

	// Token: 0x04001A2D RID: 6701
	private float m_lastWaterImpactTime;

	// Token: 0x04001A2E RID: 6702
	private float m_upsideDownDmgTimer;

	// Token: 0x04001A2F RID: 6703
	private float m_ashlandsDmgTimer;

	// Token: 0x04001A30 RID: 6704
	private float m_rudderPaddleTimer;

	// Token: 0x04001A31 RID: 6705
	private float m_lastUpdateWaterForceTime;

	// Token: 0x02000381 RID: 897
	public enum Speed
	{
		// Token: 0x0400266C RID: 9836
		Stop,
		// Token: 0x0400266D RID: 9837
		Back,
		// Token: 0x0400266E RID: 9838
		Slow,
		// Token: 0x0400266F RID: 9839
		Half,
		// Token: 0x04002670 RID: 9840
		Full
	}
}
