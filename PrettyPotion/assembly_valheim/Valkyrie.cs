using System;
using UnityEngine;

// Token: 0x020001D7 RID: 471
public class Valkyrie : MonoBehaviour
{
	// Token: 0x06001B0C RID: 6924 RVA: 0x000C9BEC File Offset: 0x000C7DEC
	private void Awake()
	{
		Valkyrie.m_instance = this;
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_animator = base.GetComponentInChildren<Animator>();
		if (!this.m_nview.IsOwner())
		{
			base.enabled = false;
			return;
		}
		ZLog.Log("Setting up valkyrie ");
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		Vector3 vector = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f));
		Vector3 a = Vector3.Cross(vector, Vector3.up);
		this.m_targetPoint = Player.m_localPlayer.transform.position + new Vector3(0f, this.m_dropHeight, 0f);
		Vector3 position = this.m_targetPoint + vector * this.m_startDistance;
		position.y = this.m_startAltitude;
		base.transform.position = position;
		this.m_descentStart = this.m_targetPoint + vector * this.m_startDescentDistance + a * 200f;
		this.m_descentStart.y = this.m_descentAltitude;
		Vector3 a2 = this.m_targetPoint - this.m_descentStart;
		a2.y = 0f;
		a2.Normalize();
		this.m_flyAwayPoint = this.m_targetPoint + a2 * this.m_startDescentDistance;
		this.m_flyAwayPoint.y = this.m_startAltitude;
		this.SyncPlayer(true);
		ZLog.Log("World pos " + base.transform.position.ToString() + "   " + ZNet.instance.GetReferencePosition().ToString());
	}

	// Token: 0x06001B0D RID: 6925 RVA: 0x000C9DAD File Offset: 0x000C7FAD
	private void HideText()
	{
	}

	// Token: 0x06001B0E RID: 6926 RVA: 0x000C9DAF File Offset: 0x000C7FAF
	private void OnDestroy()
	{
		ZLog.Log("Destroying valkyrie");
	}

	// Token: 0x06001B0F RID: 6927 RVA: 0x000C9DBB File Offset: 0x000C7FBB
	private void FixedUpdate()
	{
		this.UpdateValkyrie(Time.fixedDeltaTime);
		if (!this.m_droppedPlayer)
		{
			this.SyncPlayer(true);
		}
	}

	// Token: 0x06001B10 RID: 6928 RVA: 0x000C9DD7 File Offset: 0x000C7FD7
	private void LateUpdate()
	{
		if (!this.m_droppedPlayer)
		{
			this.SyncPlayer(false);
		}
	}

	// Token: 0x06001B11 RID: 6929 RVA: 0x000C9DE8 File Offset: 0x000C7FE8
	private void UpdateValkyrie(float dt)
	{
		this.m_timer += dt;
		if (TextViewer.IsShowingIntro())
		{
			return;
		}
		Vector3 vector;
		if (this.m_droppedPlayer)
		{
			vector = this.m_flyAwayPoint;
		}
		else if (this.m_descent)
		{
			vector = this.m_targetPoint;
		}
		else
		{
			vector = this.m_descentStart;
		}
		if (Utils.DistanceXZ(vector, base.transform.position) < 0.5f)
		{
			if (!this.m_descent)
			{
				this.m_descent = true;
				ZLog.Log("Starting descent");
			}
			else if (!this.m_droppedPlayer)
			{
				ZLog.Log("We are here");
				this.DropPlayer(false);
			}
			else
			{
				this.m_nview.Destroy();
			}
		}
		Vector3 normalized = (vector - base.transform.position).normalized;
		Vector3 vector2 = base.transform.position + normalized * 25f;
		float num;
		if (ZoneSystem.instance.GetGroundHeight(vector2, out num))
		{
			vector2.y = Mathf.Max(vector2.y, num + this.m_dropHeight);
		}
		Vector3 normalized2 = (vector2 - base.transform.position).normalized;
		Quaternion quaternion = Quaternion.LookRotation(normalized2);
		Vector3 to = normalized2;
		to.y = 0f;
		to.Normalize();
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		float num2 = Mathf.Clamp(Vector3.SignedAngle(forward, to, Vector3.up), -30f, 30f) / 30f;
		quaternion = Quaternion.Euler(0f, 0f, num2 * 45f) * quaternion;
		float num3 = this.m_droppedPlayer ? (this.m_turnRate * 4f) : this.m_turnRate;
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, num3 * dt);
		Vector3 a = base.transform.forward * this.m_speed;
		Vector3 vector3 = base.transform.position + a * dt;
		float num4;
		if (ZoneSystem.instance.GetGroundHeight(vector3, out num4))
		{
			vector3.y = Mathf.Max(vector3.y, num4 + this.m_dropHeight);
		}
		base.transform.position = vector3;
	}

	// Token: 0x06001B12 RID: 6930 RVA: 0x000CA034 File Offset: 0x000C8234
	public void DropPlayer(bool destroy = false)
	{
		ZLog.Log("We are here");
		this.m_droppedPlayer = true;
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Player.m_localPlayer.transform.rotation = Quaternion.LookRotation(forward);
		Player.m_localPlayer.SetIntro(false);
		this.m_animator.SetBool("dropped", true);
		if (destroy)
		{
			this.m_nview.Destroy();
		}
	}

	// Token: 0x06001B13 RID: 6931 RVA: 0x000CA0B0 File Offset: 0x000C82B0
	private void SyncPlayer(bool doNetworkSync)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			ZLog.LogWarning("No local player");
			return;
		}
		localPlayer.transform.rotation = this.m_attachPoint.rotation;
		localPlayer.transform.position = this.m_attachPoint.position - localPlayer.transform.TransformVector(this.m_attachOffset);
		localPlayer.GetComponent<Rigidbody>().position = localPlayer.transform.position;
		if (doNetworkSync)
		{
			ZNet.instance.SetReferencePosition(localPlayer.transform.position);
			localPlayer.GetComponent<ZSyncTransform>().SyncNow();
			base.GetComponent<ZSyncTransform>().SyncNow();
		}
	}

	// Token: 0x04001BA5 RID: 7077
	public static Valkyrie m_instance;

	// Token: 0x04001BA6 RID: 7078
	public float m_speed = 10f;

	// Token: 0x04001BA7 RID: 7079
	public float m_turnRate = 5f;

	// Token: 0x04001BA8 RID: 7080
	public float m_dropHeight = 10f;

	// Token: 0x04001BA9 RID: 7081
	public float m_startAltitude = 500f;

	// Token: 0x04001BAA RID: 7082
	public float m_descentAltitude = 100f;

	// Token: 0x04001BAB RID: 7083
	public float m_startDistance = 500f;

	// Token: 0x04001BAC RID: 7084
	public float m_startDescentDistance = 200f;

	// Token: 0x04001BAD RID: 7085
	public Vector3 m_attachOffset = new Vector3(0f, 0f, 1f);

	// Token: 0x04001BAE RID: 7086
	public float m_textDuration = 5f;

	// Token: 0x04001BAF RID: 7087
	public Transform m_attachPoint;

	// Token: 0x04001BB0 RID: 7088
	private Vector3 m_targetPoint;

	// Token: 0x04001BB1 RID: 7089
	private Vector3 m_descentStart;

	// Token: 0x04001BB2 RID: 7090
	private Vector3 m_flyAwayPoint;

	// Token: 0x04001BB3 RID: 7091
	private bool m_descent;

	// Token: 0x04001BB4 RID: 7092
	private bool m_droppedPlayer;

	// Token: 0x04001BB5 RID: 7093
	private Animator m_animator;

	// Token: 0x04001BB6 RID: 7094
	private ZNetView m_nview;

	// Token: 0x04001BB7 RID: 7095
	private float m_timer;
}
