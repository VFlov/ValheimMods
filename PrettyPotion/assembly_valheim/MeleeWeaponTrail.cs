using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Token: 0x0200005C RID: 92
public class MeleeWeaponTrail : MonoBehaviour, IMonoUpdater
{
	// Token: 0x17000015 RID: 21
	// (set) Token: 0x06000667 RID: 1639 RVA: 0x000354E6 File Offset: 0x000336E6
	public bool Emit
	{
		set
		{
			this._emit = value;
			this.m_pointsPoolIndex = 0;
		}
	}

	// Token: 0x17000016 RID: 22
	// (get) Token: 0x06000668 RID: 1640 RVA: 0x000354F6 File Offset: 0x000336F6
	// (set) Token: 0x06000669 RID: 1641 RVA: 0x000354FE File Offset: 0x000336FE
	public bool Use { get; set; }

	// Token: 0x0600066A RID: 1642 RVA: 0x00035508 File Offset: 0x00033708
	private MeleeWeaponTrail.Point GetPooledPoint()
	{
		if (this.m_pointsPoolIndex >= this.m_pointsPool.Count)
		{
			this.m_pointsPool.Capacity += 32;
			for (int i = 0; i < 32; i++)
			{
				this.m_pointsPool.Add(new MeleeWeaponTrail.Point());
			}
		}
		List<MeleeWeaponTrail.Point> pointsPool = this.m_pointsPool;
		int pointsPoolIndex = this.m_pointsPoolIndex;
		this.m_pointsPoolIndex = pointsPoolIndex + 1;
		return pointsPool[pointsPoolIndex];
	}

	// Token: 0x0600066B RID: 1643 RVA: 0x00035578 File Offset: 0x00033778
	private MeleeWeaponTrail()
	{
		this.m_minVertexDistanceSqr = this._minVertexDistance * this._minVertexDistance;
		this.m_maxVertexDistanceSqr = this._maxVertexDistance * this._maxVertexDistance;
	}

	// Token: 0x0600066C RID: 1644 RVA: 0x00035674 File Offset: 0x00033874
	private void Start()
	{
		this.m_lastPosition = base.transform.position;
		this.m_trailObject = new GameObject("Trail");
		this.m_trailObject.transform.parent = null;
		this.m_trailObject.transform.position = Vector3.zero;
		this.m_trailObject.transform.rotation = Quaternion.identity;
		this.m_trailObject.transform.localScale = Vector3.one;
		this.m_trailObject.AddComponent(typeof(MeshFilter));
		this.m_trailObject.AddComponent(typeof(MeshRenderer));
		this.m_trailObject.GetComponent<Renderer>().material = this._material;
		this.m_trailMesh = new Mesh();
		this.m_trailMesh.name = base.name + "TrailMesh";
		this.m_trailObject.GetComponent<MeshFilter>().mesh = this.m_trailMesh;
		for (int i = 0; i < 160; i++)
		{
			this.m_pointsPool.Add(new MeleeWeaponTrail.Point());
		}
		this.Use = true;
	}

	// Token: 0x0600066D RID: 1645 RVA: 0x00035797 File Offset: 0x00033997
	private void OnEnable()
	{
		MeleeWeaponTrail.Instances.Add(this);
	}

	// Token: 0x0600066E RID: 1646 RVA: 0x000357A4 File Offset: 0x000339A4
	private void OnDisable()
	{
		MeleeWeaponTrail.Instances.Remove(this);
		UnityEngine.Object.Destroy(this.m_trailObject);
	}

	// Token: 0x0600066F RID: 1647 RVA: 0x000357C0 File Offset: 0x000339C0
	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		float time = Time.time;
		if (!this.Use)
		{
			return;
		}
		if (this._emit && this._emitTime != 0f)
		{
			this._emitTime -= fixedDeltaTime;
			if (this._emitTime == 0f)
			{
				this._emitTime = -1f;
			}
			if (this._emitTime < 0f)
			{
				this._emit = false;
			}
		}
		if (!this._emit && this.m_points.Count == 0 && this._autoDestruct)
		{
			UnityEngine.Object.Destroy(this.m_trailObject);
			UnityEngine.Object.Destroy(base.gameObject);
		}
		if (Utils.GetMainCamera() == null)
		{
			return;
		}
		if (this._emit)
		{
			float sqrMagnitude = (this.m_lastPosition - base.transform.position).sqrMagnitude;
			if (sqrMagnitude > this.m_minVertexDistanceSqr)
			{
				bool flag = false;
				if (this.m_points.Count < 3)
				{
					flag = true;
				}
				else
				{
					List<MeleeWeaponTrail.Point> points = this.m_points;
					Vector3 tipPosition = points[points.Count - 2].tipPosition;
					List<MeleeWeaponTrail.Point> points2 = this.m_points;
					Vector3 from = tipPosition - points2[points2.Count - 3].tipPosition;
					List<MeleeWeaponTrail.Point> points3 = this.m_points;
					Vector3 tipPosition2 = points3[points3.Count - 1].tipPosition;
					List<MeleeWeaponTrail.Point> points4 = this.m_points;
					Vector3 to = tipPosition2 - points4[points4.Count - 2].tipPosition;
					if (Vector3.Angle(from, to) > this._maxAngle || sqrMagnitude > this.m_maxVertexDistanceSqr)
					{
						flag = true;
					}
				}
				if (flag)
				{
					MeleeWeaponTrail.Point pooledPoint = this.GetPooledPoint();
					pooledPoint.endTime = time + this._lifeTime;
					pooledPoint.basePosition = this._base.position;
					pooledPoint.tipPosition = this._tip.position;
					this.m_points.Add(pooledPoint);
					this.m_lastPosition = base.transform.position;
					if (this.m_points.Count == 1)
					{
						this.m_smoothedPoints.Add(pooledPoint);
					}
					else if (this.m_points.Count > 1)
					{
						for (int i = 0; i < 1 + this.subdivisions; i++)
						{
							this.m_smoothedPoints.Add(pooledPoint);
						}
					}
					if (this.m_points.Count >= 4)
					{
						Vector3[] tipPoints = this.m_tipPoints;
						int num = 0;
						List<MeleeWeaponTrail.Point> points5 = this.m_points;
						tipPoints[num] = points5[points5.Count - 4].tipPosition;
						Vector3[] tipPoints2 = this.m_tipPoints;
						int num2 = 1;
						List<MeleeWeaponTrail.Point> points6 = this.m_points;
						tipPoints2[num2] = points6[points6.Count - 3].tipPosition;
						Vector3[] tipPoints3 = this.m_tipPoints;
						int num3 = 2;
						List<MeleeWeaponTrail.Point> points7 = this.m_points;
						tipPoints3[num3] = points7[points7.Count - 2].tipPosition;
						Vector3[] tipPoints4 = this.m_tipPoints;
						int num4 = 3;
						List<MeleeWeaponTrail.Point> points8 = this.m_points;
						tipPoints4[num4] = points8[points8.Count - 1].tipPosition;
						IEnumerable<Vector3> source = Interpolate.NewCatmullRom(this.m_tipPoints, this.subdivisions, false);
						Vector3[] basePoints = this.m_basePoints;
						int num5 = 0;
						List<MeleeWeaponTrail.Point> points9 = this.m_points;
						basePoints[num5] = points9[points9.Count - 4].basePosition;
						Vector3[] basePoints2 = this.m_basePoints;
						int num6 = 1;
						List<MeleeWeaponTrail.Point> points10 = this.m_points;
						basePoints2[num6] = points10[points10.Count - 3].basePosition;
						Vector3[] basePoints3 = this.m_basePoints;
						int num7 = 2;
						List<MeleeWeaponTrail.Point> points11 = this.m_points;
						basePoints3[num7] = points11[points11.Count - 2].basePosition;
						Vector3[] basePoints4 = this.m_basePoints;
						int num8 = 3;
						List<MeleeWeaponTrail.Point> points12 = this.m_points;
						basePoints4[num8] = points12[points12.Count - 1].basePosition;
						IEnumerable<Vector3> source2 = Interpolate.NewCatmullRom(this.m_basePoints, this.subdivisions, false);
						this.m_smoothTipList = source.ToList<Vector3>();
						this.m_smoothBaseList = source2.ToList<Vector3>();
						List<MeleeWeaponTrail.Point> points13 = this.m_points;
						float endTime = points13[points13.Count - 4].endTime;
						List<MeleeWeaponTrail.Point> points14 = this.m_points;
						float endTime2 = points14[points14.Count - 1].endTime;
						for (int j = 0; j < this.m_smoothTipList.Count; j++)
						{
							int num9 = this.m_smoothedPoints.Count - (this.m_smoothTipList.Count - j);
							if (num9 > -1 && num9 < this.m_smoothedPoints.Count)
							{
								MeleeWeaponTrail.Point pooledPoint2 = this.GetPooledPoint();
								pooledPoint2.basePosition = this.m_smoothBaseList[j];
								pooledPoint2.tipPosition = this.m_smoothTipList[j];
								pooledPoint2.endTime = Utils.Lerp(endTime, endTime2, (float)j / (float)this.m_smoothTipList.Count);
								this.m_smoothedPoints[num9] = pooledPoint2;
							}
						}
					}
				}
				else
				{
					List<MeleeWeaponTrail.Point> points15 = this.m_points;
					points15[points15.Count - 1].basePosition = this._base.position;
					List<MeleeWeaponTrail.Point> points16 = this.m_points;
					points16[points16.Count - 1].tipPosition = this._tip.position;
					List<MeleeWeaponTrail.Point> smoothedPoints = this.m_smoothedPoints;
					smoothedPoints[smoothedPoints.Count - 1].basePosition = this._base.position;
					List<MeleeWeaponTrail.Point> smoothedPoints2 = this.m_smoothedPoints;
					smoothedPoints2[smoothedPoints2.Count - 1].tipPosition = this._tip.position;
				}
			}
			else
			{
				if (this.m_points.Count > 0)
				{
					List<MeleeWeaponTrail.Point> points17 = this.m_points;
					points17[points17.Count - 1].basePosition = this._base.position;
					List<MeleeWeaponTrail.Point> points18 = this.m_points;
					points18[points18.Count - 1].tipPosition = this._tip.position;
				}
				if (this.m_smoothedPoints.Count > 0)
				{
					List<MeleeWeaponTrail.Point> smoothedPoints3 = this.m_smoothedPoints;
					smoothedPoints3[smoothedPoints3.Count - 1].basePosition = this._base.position;
					List<MeleeWeaponTrail.Point> smoothedPoints4 = this.m_smoothedPoints;
					smoothedPoints4[smoothedPoints4.Count - 1].tipPosition = this._tip.position;
				}
			}
		}
		this.RemoveOldPoints(this.m_points, time);
		if (this.m_points.Count == 0)
		{
			this.m_trailMesh.Clear();
		}
		this.RemoveOldPoints(this.m_smoothedPoints, time);
		if (this.m_smoothedPoints.Count == 0)
		{
			this.m_trailMesh.Clear();
		}
		List<MeleeWeaponTrail.Point> smoothedPoints5 = this.m_smoothedPoints;
		if (smoothedPoints5.Count <= 1)
		{
			return;
		}
		int num10 = smoothedPoints5.Count * 2;
		int num11 = (smoothedPoints5.Count - 1) * 6;
		if (this.m_newVertices.Capacity < num10)
		{
			this.m_newVertices.Capacity = num10;
			this.m_newUV.Capacity = num10;
			this.m_newColors.Capacity = num10;
		}
		if (this.m_newTriangles.Capacity < num11)
		{
			this.m_newTriangles.Capacity = num11;
		}
		this.m_newVertices.Resize(num10);
		this.m_newUV.Resize(num10);
		this.m_newColors.Resize(num10);
		this.m_newTriangles.Resize(num11);
		Vector3[] array = this.m_newVertices.ToArray();
		Vector2[] array2 = this.m_newUV.ToArray();
		Color[] array3 = this.m_newColors.ToArray();
		int[] array4 = this.m_newTriangles.ToArray();
		float num12 = time + this._lifeTime;
		for (int k = 0; k < smoothedPoints5.Count; k++)
		{
			MeleeWeaponTrail.Point point = smoothedPoints5[k];
			float num13 = (num12 - point.endTime) / this._lifeTime;
			Color color = Color.Lerp(Color.white, Color.clear, num13);
			Color[] colors = this._colors;
			int num14 = (colors != null) ? colors.Length : 0;
			if (num14 > 0)
			{
				float num15 = num13 * (float)(num14 - 1);
				float num16 = Mathf.Floor(num15);
				float num17 = Utils.Clamp(Mathf.Ceil(num15), 1f, (float)(num14 - 1));
				float t = Mathf.InverseLerp(num16, num17, num15);
				if (num16 >= (float)num14)
				{
					num16 = (float)(num14 - 1);
				}
				if (num16 < 0f)
				{
					num16 = 0f;
				}
				if (num17 >= (float)num14)
				{
					num17 = (float)(num14 - 1);
				}
				if (num17 < 0f)
				{
					num17 = 0f;
				}
				color = Color.Lerp(this._colors[(int)num16], this._colors[(int)num17], t);
			}
			float num18 = 0f;
			float[] sizes = this._sizes;
			int num19 = (sizes != null) ? sizes.Length : 0;
			if (num19 > 0)
			{
				float num20 = num13 * (float)(num19 - 1);
				float num21 = Mathf.Floor(num20);
				float num22 = Utils.Clamp(Mathf.Ceil(num20), 1f, (float)(num19 - 1));
				float t2 = Mathf.InverseLerp(num21, num22, num20);
				if (num21 >= (float)num19)
				{
					num21 = (float)(num19 - 1);
				}
				if (num21 < 0f)
				{
					num21 = 0f;
				}
				if (num22 >= (float)num19)
				{
					num22 = (float)(num19 - 1);
				}
				if (num22 < 0f)
				{
					num22 = 0f;
				}
				num18 = Mathf.Lerp(this._sizes[(int)num21], this._sizes[(int)num22], t2);
			}
			Vector3 a = point.tipPosition - point.basePosition;
			array[k * 2] = point.basePosition - a * (num18 * 0.5f);
			array[k * 2 + 1] = point.tipPosition + a * (num18 * 0.5f);
			array3[k * 2] = (array3[k * 2 + 1] = color);
			float x = (float)k / (float)smoothedPoints5.Count;
			array2[k * 2].x = x;
			array2[k * 2].y = 0f;
			array2[k * 2 + 1].x = x;
			array2[k * 2 + 1].y = 1f;
			if (k > 0)
			{
				array4[(k - 1) * 6] = k * 2 - 2;
				array4[(k - 1) * 6 + 1] = k * 2 - 1;
				array4[(k - 1) * 6 + 2] = k * 2;
				array4[(k - 1) * 6 + 3] = k * 2 + 1;
				array4[(k - 1) * 6 + 4] = k * 2;
				array4[(k - 1) * 6 + 5] = k * 2 - 1;
			}
		}
		this.m_trailMesh.Clear();
		this.m_trailMesh.vertices = array;
		this.m_trailMesh.colors = array3;
		this.m_trailMesh.uv = array2;
		this.m_trailMesh.triangles = array4;
	}

	// Token: 0x06000670 RID: 1648 RVA: 0x000361E8 File Offset: 0x000343E8
	private void RemoveOldPoints(List<MeleeWeaponTrail.Point> pointList, float time)
	{
		for (int i = pointList.Count - 1; i >= 0; i--)
		{
			MeleeWeaponTrail.Point point = pointList[i];
			if (time > point.endTime)
			{
				pointList.RemoveAt(i);
			}
		}
	}

	// Token: 0x17000017 RID: 23
	// (get) Token: 0x06000671 RID: 1649 RVA: 0x00036220 File Offset: 0x00034420
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04000745 RID: 1861
	[SerializeField]
	private bool _emit = true;

	// Token: 0x04000747 RID: 1863
	[SerializeField]
	private float _emitTime;

	// Token: 0x04000748 RID: 1864
	[SerializeField]
	private Material _material;

	// Token: 0x04000749 RID: 1865
	[SerializeField]
	private float _lifeTime = 1f;

	// Token: 0x0400074A RID: 1866
	[SerializeField]
	private Color[] _colors;

	// Token: 0x0400074B RID: 1867
	[SerializeField]
	private float[] _sizes;

	// Token: 0x0400074C RID: 1868
	[SerializeField]
	private float _minVertexDistance = 0.1f;

	// Token: 0x0400074D RID: 1869
	[SerializeField]
	private float _maxVertexDistance = 10f;

	// Token: 0x0400074E RID: 1870
	private readonly float m_minVertexDistanceSqr;

	// Token: 0x0400074F RID: 1871
	private readonly float m_maxVertexDistanceSqr;

	// Token: 0x04000750 RID: 1872
	[SerializeField]
	private float _maxAngle = 3f;

	// Token: 0x04000751 RID: 1873
	[SerializeField]
	private bool _autoDestruct;

	// Token: 0x04000752 RID: 1874
	[SerializeField]
	private int subdivisions = 4;

	// Token: 0x04000753 RID: 1875
	[SerializeField]
	private Transform _base;

	// Token: 0x04000754 RID: 1876
	[SerializeField]
	private Transform _tip;

	// Token: 0x04000755 RID: 1877
	private readonly List<MeleeWeaponTrail.Point> m_points = new List<MeleeWeaponTrail.Point>();

	// Token: 0x04000756 RID: 1878
	private readonly List<MeleeWeaponTrail.Point> m_smoothedPoints = new List<MeleeWeaponTrail.Point>();

	// Token: 0x04000757 RID: 1879
	private const int c_PointsPoolSize = 160;

	// Token: 0x04000758 RID: 1880
	private const int c_PointsPoolSizeGrow = 32;

	// Token: 0x04000759 RID: 1881
	private readonly List<MeleeWeaponTrail.Point> m_pointsPool = new List<MeleeWeaponTrail.Point>(160);

	// Token: 0x0400075A RID: 1882
	private int m_pointsPoolIndex;

	// Token: 0x0400075B RID: 1883
	private GameObject m_trailObject;

	// Token: 0x0400075C RID: 1884
	private Mesh m_trailMesh;

	// Token: 0x0400075D RID: 1885
	private Vector3 m_lastPosition;

	// Token: 0x0400075E RID: 1886
	private readonly List<Vector3> m_newVertices = new List<Vector3>(64);

	// Token: 0x0400075F RID: 1887
	private readonly List<Vector2> m_newUV = new List<Vector2>(64);

	// Token: 0x04000760 RID: 1888
	private readonly List<Color> m_newColors = new List<Color>(64);

	// Token: 0x04000761 RID: 1889
	private readonly List<int> m_newTriangles = new List<int>(64);

	// Token: 0x04000762 RID: 1890
	private List<Vector3> m_smoothTipList = new List<Vector3>();

	// Token: 0x04000763 RID: 1891
	private List<Vector3> m_smoothBaseList = new List<Vector3>();

	// Token: 0x04000764 RID: 1892
	private readonly Vector3[] m_tipPoints = new Vector3[4];

	// Token: 0x04000765 RID: 1893
	private readonly Vector3[] m_basePoints = new Vector3[4];

	// Token: 0x0200025E RID: 606
	[Serializable]
	public class Point
	{
		// Token: 0x0400208D RID: 8333
		public float endTime;

		// Token: 0x0400208E RID: 8334
		public Vector3 basePosition;

		// Token: 0x0400208F RID: 8335
		public Vector3 tipPosition;
	}
}
