using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001C1 RID: 449
public class StationExtension : MonoBehaviour, Hoverable
{
	// Token: 0x06001A22 RID: 6690 RVA: 0x000C2B98 File Offset: 0x000C0D98
	private void Awake()
	{
		if (base.GetComponent<ZNetView>().GetZDO() == null)
		{
			return;
		}
		this.m_piece = base.GetComponent<Piece>();
		StationExtension.m_allExtensions.Add(this);
		if (this.m_continousConnection)
		{
			base.InvokeRepeating("UpdateConnection", 1f, 4f);
		}
	}

	// Token: 0x06001A23 RID: 6691 RVA: 0x000C2BE7 File Offset: 0x000C0DE7
	private void OnDestroy()
	{
		if (this.m_connection)
		{
			UnityEngine.Object.Destroy(this.m_connection);
			this.m_connection = null;
		}
		StationExtension.m_allExtensions.Remove(this);
	}

	// Token: 0x06001A24 RID: 6692 RVA: 0x000C2C14 File Offset: 0x000C0E14
	public string GetHoverText()
	{
		if (!this.m_continousConnection)
		{
			this.PokeEffect(1f);
		}
		return Localization.instance.Localize(this.m_piece.m_name);
	}

	// Token: 0x06001A25 RID: 6693 RVA: 0x000C2C3E File Offset: 0x000C0E3E
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_piece.m_name);
	}

	// Token: 0x06001A26 RID: 6694 RVA: 0x000C2C55 File Offset: 0x000C0E55
	private string GetExtensionName()
	{
		return this.m_piece.m_name;
	}

	// Token: 0x06001A27 RID: 6695 RVA: 0x000C2C64 File Offset: 0x000C0E64
	public static void FindExtensions(CraftingStation station, Vector3 pos, List<StationExtension> extensions)
	{
		foreach (StationExtension stationExtension in StationExtension.m_allExtensions)
		{
			if (Vector3.Distance(stationExtension.transform.position, pos) < stationExtension.m_maxStationDistance && stationExtension.m_craftingStation.m_name == station.m_name && (stationExtension.m_stack || !StationExtension.ExtensionInList(extensions, stationExtension)))
			{
				extensions.Add(stationExtension);
			}
		}
	}

	// Token: 0x06001A28 RID: 6696 RVA: 0x000C2CF8 File Offset: 0x000C0EF8
	private static bool ExtensionInList(List<StationExtension> extensions, StationExtension extension)
	{
		using (List<StationExtension>.Enumerator enumerator = extensions.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.GetExtensionName() == extension.GetExtensionName())
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06001A29 RID: 6697 RVA: 0x000C2D58 File Offset: 0x000C0F58
	public bool OtherExtensionInRange(float radius)
	{
		foreach (StationExtension stationExtension in StationExtension.m_allExtensions)
		{
			if (!(stationExtension == this) && Vector3.Distance(stationExtension.transform.position, base.transform.position) < radius)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001A2A RID: 6698 RVA: 0x000C2DD4 File Offset: 0x000C0FD4
	public List<CraftingStation> FindStationsInRange(Vector3 center)
	{
		List<CraftingStation> list = new List<CraftingStation>();
		CraftingStation.FindStationsInRange(this.m_craftingStation.m_name, center, this.m_maxStationDistance, list);
		return list;
	}

	// Token: 0x06001A2B RID: 6699 RVA: 0x000C2E00 File Offset: 0x000C1000
	public CraftingStation FindClosestStationInRange(Vector3 center)
	{
		return CraftingStation.FindClosestStationInRange(this.m_craftingStation.m_name, center, this.m_maxStationDistance);
	}

	// Token: 0x06001A2C RID: 6700 RVA: 0x000C2E19 File Offset: 0x000C1019
	private void UpdateConnection()
	{
		this.PokeEffect(5f);
	}

	// Token: 0x06001A2D RID: 6701 RVA: 0x000C2E28 File Offset: 0x000C1028
	private void PokeEffect(float timeout = 1f)
	{
		CraftingStation craftingStation = this.FindClosestStationInRange(base.transform.position);
		if (craftingStation)
		{
			this.StartConnectionEffect(craftingStation, timeout);
		}
	}

	// Token: 0x06001A2E RID: 6702 RVA: 0x000C2E57 File Offset: 0x000C1057
	public void StartConnectionEffect(CraftingStation station, float timeout = 1f)
	{
		this.StartConnectionEffect(station.GetConnectionEffectPoint(), timeout);
	}

	// Token: 0x06001A2F RID: 6703 RVA: 0x000C2E68 File Offset: 0x000C1068
	public void StartConnectionEffect(Vector3 targetPos, float timeout = 1f)
	{
		Vector3 connectionPoint = this.GetConnectionPoint();
		if (this.m_connection == null)
		{
			this.m_connection = UnityEngine.Object.Instantiate<GameObject>(this.m_connectionPrefab, connectionPoint, Quaternion.identity);
		}
		Vector3 vector = targetPos - connectionPoint;
		Quaternion rotation = Quaternion.LookRotation(vector.normalized);
		this.m_connection.transform.position = connectionPoint;
		this.m_connection.transform.rotation = rotation;
		this.m_connection.transform.localScale = new Vector3(1f, 1f, vector.magnitude);
		base.CancelInvoke("StopConnectionEffect");
		base.Invoke("StopConnectionEffect", timeout);
	}

	// Token: 0x06001A30 RID: 6704 RVA: 0x000C2F15 File Offset: 0x000C1115
	public void StopConnectionEffect()
	{
		if (this.m_connection)
		{
			UnityEngine.Object.Destroy(this.m_connection);
			this.m_connection = null;
		}
	}

	// Token: 0x06001A31 RID: 6705 RVA: 0x000C2F36 File Offset: 0x000C1136
	private Vector3 GetConnectionPoint()
	{
		return base.transform.TransformPoint(this.m_connectionOffset);
	}

	// Token: 0x06001A32 RID: 6706 RVA: 0x000C2F49 File Offset: 0x000C1149
	private void OnDrawGizmos()
	{
	}

	// Token: 0x04001A9C RID: 6812
	public CraftingStation m_craftingStation;

	// Token: 0x04001A9D RID: 6813
	public float m_maxStationDistance = 5f;

	// Token: 0x04001A9E RID: 6814
	public bool m_stack;

	// Token: 0x04001A9F RID: 6815
	public GameObject m_connectionPrefab;

	// Token: 0x04001AA0 RID: 6816
	public Vector3 m_connectionOffset = new Vector3(0f, 0f, 0f);

	// Token: 0x04001AA1 RID: 6817
	public bool m_continousConnection;

	// Token: 0x04001AA2 RID: 6818
	private GameObject m_connection;

	// Token: 0x04001AA3 RID: 6819
	private Piece m_piece;

	// Token: 0x04001AA4 RID: 6820
	private static List<StationExtension> m_allExtensions = new List<StationExtension>();
}
