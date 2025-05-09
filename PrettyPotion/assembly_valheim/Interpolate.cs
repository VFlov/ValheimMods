using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200005B RID: 91
public class Interpolate
{
	// Token: 0x0600063D RID: 1597 RVA: 0x00034AA3 File Offset: 0x00032CA3
	private static Vector3 Identity(Vector3 v)
	{
		return v;
	}

	// Token: 0x0600063E RID: 1598 RVA: 0x00034AA6 File Offset: 0x00032CA6
	private static Vector3 TransformDotPosition(Transform t)
	{
		return t.position;
	}

	// Token: 0x0600063F RID: 1599 RVA: 0x00034AAE File Offset: 0x00032CAE
	private static IEnumerable<float> NewTimer(float duration)
	{
		float elapsedTime = 0f;
		while (elapsedTime < duration)
		{
			yield return elapsedTime;
			elapsedTime += Time.deltaTime;
			if (elapsedTime >= duration)
			{
				yield return elapsedTime;
			}
		}
		yield break;
	}

	// Token: 0x06000640 RID: 1600 RVA: 0x00034ABE File Offset: 0x00032CBE
	private static IEnumerable<float> NewCounter(int start, int end, int step)
	{
		for (int i = start; i <= end; i += step)
		{
			yield return (float)i;
		}
		yield break;
	}

	// Token: 0x06000641 RID: 1601 RVA: 0x00034ADC File Offset: 0x00032CDC
	public static IEnumerator NewEase(Interpolate.Function ease, Vector3 start, Vector3 end, float duration)
	{
		IEnumerable<float> driver = Interpolate.NewTimer(duration);
		return Interpolate.NewEase(ease, start, end, duration, driver);
	}

	// Token: 0x06000642 RID: 1602 RVA: 0x00034AFC File Offset: 0x00032CFC
	public static IEnumerator NewEase(Interpolate.Function ease, Vector3 start, Vector3 end, int slices)
	{
		IEnumerable<float> driver = Interpolate.NewCounter(0, slices + 1, 1);
		return Interpolate.NewEase(ease, start, end, (float)(slices + 1), driver);
	}

	// Token: 0x06000643 RID: 1603 RVA: 0x00034B21 File Offset: 0x00032D21
	private static IEnumerator NewEase(Interpolate.Function ease, Vector3 start, Vector3 end, float total, IEnumerable<float> driver)
	{
		Vector3 distance = end - start;
		foreach (float elapsedTime in driver)
		{
			yield return Interpolate.Ease(ease, start, distance, elapsedTime, total);
		}
		IEnumerator<float> enumerator = null;
		yield break;
		yield break;
	}

	// Token: 0x06000644 RID: 1604 RVA: 0x00034B50 File Offset: 0x00032D50
	private static Vector3 Ease(Interpolate.Function ease, Vector3 start, Vector3 distance, float elapsedTime, float duration)
	{
		start.x = ease(start.x, distance.x, elapsedTime, duration);
		start.y = ease(start.y, distance.y, elapsedTime, duration);
		start.z = ease(start.z, distance.z, elapsedTime, duration);
		return start;
	}

	// Token: 0x06000645 RID: 1605 RVA: 0x00034BB4 File Offset: 0x00032DB4
	public static Interpolate.Function Ease(Interpolate.EaseType type)
	{
		Interpolate.Function result = null;
		switch (type)
		{
		case Interpolate.EaseType.Linear:
			result = new Interpolate.Function(Interpolate.Linear);
			break;
		case Interpolate.EaseType.EaseInQuad:
			result = new Interpolate.Function(Interpolate.EaseInQuad);
			break;
		case Interpolate.EaseType.EaseOutQuad:
			result = new Interpolate.Function(Interpolate.EaseOutQuad);
			break;
		case Interpolate.EaseType.EaseInOutQuad:
			result = new Interpolate.Function(Interpolate.EaseInOutQuad);
			break;
		case Interpolate.EaseType.EaseInCubic:
			result = new Interpolate.Function(Interpolate.EaseInCubic);
			break;
		case Interpolate.EaseType.EaseOutCubic:
			result = new Interpolate.Function(Interpolate.EaseOutCubic);
			break;
		case Interpolate.EaseType.EaseInOutCubic:
			result = new Interpolate.Function(Interpolate.EaseInOutCubic);
			break;
		case Interpolate.EaseType.EaseInQuart:
			result = new Interpolate.Function(Interpolate.EaseInQuart);
			break;
		case Interpolate.EaseType.EaseOutQuart:
			result = new Interpolate.Function(Interpolate.EaseOutQuart);
			break;
		case Interpolate.EaseType.EaseInOutQuart:
			result = new Interpolate.Function(Interpolate.EaseInOutQuart);
			break;
		case Interpolate.EaseType.EaseInQuint:
			result = new Interpolate.Function(Interpolate.EaseInQuint);
			break;
		case Interpolate.EaseType.EaseOutQuint:
			result = new Interpolate.Function(Interpolate.EaseOutQuint);
			break;
		case Interpolate.EaseType.EaseInOutQuint:
			result = new Interpolate.Function(Interpolate.EaseInOutQuint);
			break;
		case Interpolate.EaseType.EaseInSine:
			result = new Interpolate.Function(Interpolate.EaseInSine);
			break;
		case Interpolate.EaseType.EaseOutSine:
			result = new Interpolate.Function(Interpolate.EaseOutSine);
			break;
		case Interpolate.EaseType.EaseInOutSine:
			result = new Interpolate.Function(Interpolate.EaseInOutSine);
			break;
		case Interpolate.EaseType.EaseInExpo:
			result = new Interpolate.Function(Interpolate.EaseInExpo);
			break;
		case Interpolate.EaseType.EaseOutExpo:
			result = new Interpolate.Function(Interpolate.EaseOutExpo);
			break;
		case Interpolate.EaseType.EaseInOutExpo:
			result = new Interpolate.Function(Interpolate.EaseInOutExpo);
			break;
		case Interpolate.EaseType.EaseInCirc:
			result = new Interpolate.Function(Interpolate.EaseInCirc);
			break;
		case Interpolate.EaseType.EaseOutCirc:
			result = new Interpolate.Function(Interpolate.EaseOutCirc);
			break;
		case Interpolate.EaseType.EaseInOutCirc:
			result = new Interpolate.Function(Interpolate.EaseInOutCirc);
			break;
		}
		return result;
	}

	// Token: 0x06000646 RID: 1606 RVA: 0x00034D98 File Offset: 0x00032F98
	public static IEnumerable<Vector3> NewBezier(Interpolate.Function ease, Transform[] nodes, float duration)
	{
		IEnumerable<float> steps = Interpolate.NewTimer(duration);
		return Interpolate.NewBezier<Transform>(ease, nodes, new Interpolate.ToVector3<Transform>(Interpolate.TransformDotPosition), duration, steps);
	}

	// Token: 0x06000647 RID: 1607 RVA: 0x00034DC4 File Offset: 0x00032FC4
	public static IEnumerable<Vector3> NewBezier(Interpolate.Function ease, Transform[] nodes, int slices)
	{
		IEnumerable<float> steps = Interpolate.NewCounter(0, slices + 1, 1);
		return Interpolate.NewBezier<Transform>(ease, nodes, new Interpolate.ToVector3<Transform>(Interpolate.TransformDotPosition), (float)(slices + 1), steps);
	}

	// Token: 0x06000648 RID: 1608 RVA: 0x00034DF4 File Offset: 0x00032FF4
	public static IEnumerable<Vector3> NewBezier(Interpolate.Function ease, Vector3[] points, float duration)
	{
		IEnumerable<float> steps = Interpolate.NewTimer(duration);
		return Interpolate.NewBezier<Vector3>(ease, points, new Interpolate.ToVector3<Vector3>(Interpolate.Identity), duration, steps);
	}

	// Token: 0x06000649 RID: 1609 RVA: 0x00034E20 File Offset: 0x00033020
	public static IEnumerable<Vector3> NewBezier(Interpolate.Function ease, Vector3[] points, int slices)
	{
		IEnumerable<float> steps = Interpolate.NewCounter(0, slices + 1, 1);
		return Interpolate.NewBezier<Vector3>(ease, points, new Interpolate.ToVector3<Vector3>(Interpolate.Identity), (float)(slices + 1), steps);
	}

	// Token: 0x0600064A RID: 1610 RVA: 0x00034E50 File Offset: 0x00033050
	private static IEnumerable<Vector3> NewBezier<T>(Interpolate.Function ease, IList nodes, Interpolate.ToVector3<T> toVector3, float maxStep, IEnumerable<float> steps)
	{
		if (nodes.Count >= 2)
		{
			Vector3[] points = new Vector3[nodes.Count];
			foreach (float elapsedTime in steps)
			{
				for (int i = 0; i < nodes.Count; i++)
				{
					points[i] = toVector3((T)((object)nodes[i]));
				}
				yield return Interpolate.Bezier(ease, points, elapsedTime, maxStep);
			}
			IEnumerator<float> enumerator = null;
			points = null;
		}
		yield break;
		yield break;
	}

	// Token: 0x0600064B RID: 1611 RVA: 0x00034E80 File Offset: 0x00033080
	private static Vector3 Bezier(Interpolate.Function ease, Vector3[] points, float elapsedTime, float duration)
	{
		for (int i = points.Length - 1; i > 0; i--)
		{
			for (int j = 0; j < i; j++)
			{
				points[j].x = ease(points[j].x, points[j + 1].x - points[j].x, elapsedTime, duration);
				points[j].y = ease(points[j].y, points[j + 1].y - points[j].y, elapsedTime, duration);
				points[j].z = ease(points[j].z, points[j + 1].z - points[j].z, elapsedTime, duration);
			}
		}
		return points[0];
	}

	// Token: 0x0600064C RID: 1612 RVA: 0x00034F6D File Offset: 0x0003316D
	public static IEnumerable<Vector3> NewCatmullRom(Transform[] nodes, int slices, bool loop)
	{
		return Interpolate.NewCatmullRom<Transform>(nodes, new Interpolate.ToVector3<Transform>(Interpolate.TransformDotPosition), slices, loop);
	}

	// Token: 0x0600064D RID: 1613 RVA: 0x00034F83 File Offset: 0x00033183
	public static IEnumerable<Vector3> NewCatmullRom(Vector3[] points, int slices, bool loop)
	{
		return Interpolate.NewCatmullRom<Vector3>(points, new Interpolate.ToVector3<Vector3>(Interpolate.Identity), slices, loop);
	}

	// Token: 0x0600064E RID: 1614 RVA: 0x00034F99 File Offset: 0x00033199
	private static IEnumerable<Vector3> NewCatmullRom<T>(IList nodes, Interpolate.ToVector3<T> toVector3, int slices, bool loop)
	{
		if (nodes.Count >= 2)
		{
			yield return toVector3((T)((object)nodes[0]));
			int last = nodes.Count - 1;
			int current = 0;
			while (loop || current < last)
			{
				if (loop && current > last)
				{
					current = 0;
				}
				int previous = (current == 0) ? (loop ? last : current) : (current - 1);
				int start = current;
				int end = (current == last) ? (loop ? 0 : current) : (current + 1);
				int next = (end == last) ? (loop ? 0 : end) : (end + 1);
				int stepCount = slices + 1;
				int num;
				for (int step = 1; step <= stepCount; step = num + 1)
				{
					yield return Interpolate.CatmullRom(toVector3((T)((object)nodes[previous])), toVector3((T)((object)nodes[start])), toVector3((T)((object)nodes[end])), toVector3((T)((object)nodes[next])), (float)step, (float)stepCount);
					num = step;
				}
				num = current;
				current = num + 1;
			}
		}
		yield break;
	}

	// Token: 0x0600064F RID: 1615 RVA: 0x00034FC0 File Offset: 0x000331C0
	private static Vector3 CatmullRom(Vector3 previous, Vector3 start, Vector3 end, Vector3 next, float elapsedTime, float duration)
	{
		float num = elapsedTime / duration;
		float num2 = num * num;
		float num3 = num2 * num;
		return previous * (-0.5f * num3 + num2 - 0.5f * num) + start * (1.5f * num3 + -2.5f * num2 + 1f) + end * (-1.5f * num3 + 2f * num2 + 0.5f * num) + next * (0.5f * num3 - 0.5f * num2);
	}

	// Token: 0x06000650 RID: 1616 RVA: 0x0003504E File Offset: 0x0003324E
	private static float Linear(float start, float distance, float elapsedTime, float duration)
	{
		if (elapsedTime > duration)
		{
			elapsedTime = duration;
		}
		return distance * (elapsedTime / duration) + start;
	}

	// Token: 0x06000651 RID: 1617 RVA: 0x0003505E File Offset: 0x0003325E
	private static float EaseInQuad(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		return distance * elapsedTime * elapsedTime + start;
	}

	// Token: 0x06000652 RID: 1618 RVA: 0x00035077 File Offset: 0x00033277
	private static float EaseOutQuad(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		return -distance * elapsedTime * (elapsedTime - 2f) + start;
	}

	// Token: 0x06000653 RID: 1619 RVA: 0x00035098 File Offset: 0x00033298
	private static float EaseInOutQuad(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 2f : (elapsedTime / (duration / 2f)));
		if (elapsedTime < 1f)
		{
			return distance / 2f * elapsedTime * elapsedTime + start;
		}
		elapsedTime -= 1f;
		return -distance / 2f * (elapsedTime * (elapsedTime - 2f) - 1f) + start;
	}

	// Token: 0x06000654 RID: 1620 RVA: 0x000350F4 File Offset: 0x000332F4
	private static float EaseInCubic(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		return distance * elapsedTime * elapsedTime * elapsedTime + start;
	}

	// Token: 0x06000655 RID: 1621 RVA: 0x0003510F File Offset: 0x0003330F
	private static float EaseOutCubic(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		elapsedTime -= 1f;
		return distance * (elapsedTime * elapsedTime * elapsedTime + 1f) + start;
	}

	// Token: 0x06000656 RID: 1622 RVA: 0x0003513C File Offset: 0x0003333C
	private static float EaseInOutCubic(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 2f : (elapsedTime / (duration / 2f)));
		if (elapsedTime < 1f)
		{
			return distance / 2f * elapsedTime * elapsedTime * elapsedTime + start;
		}
		elapsedTime -= 2f;
		return distance / 2f * (elapsedTime * elapsedTime * elapsedTime + 2f) + start;
	}

	// Token: 0x06000657 RID: 1623 RVA: 0x00035195 File Offset: 0x00033395
	private static float EaseInQuart(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		return distance * elapsedTime * elapsedTime * elapsedTime * elapsedTime + start;
	}

	// Token: 0x06000658 RID: 1624 RVA: 0x000351B2 File Offset: 0x000333B2
	private static float EaseOutQuart(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		elapsedTime -= 1f;
		return -distance * (elapsedTime * elapsedTime * elapsedTime * elapsedTime - 1f) + start;
	}

	// Token: 0x06000659 RID: 1625 RVA: 0x000351E0 File Offset: 0x000333E0
	private static float EaseInOutQuart(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 2f : (elapsedTime / (duration / 2f)));
		if (elapsedTime < 1f)
		{
			return distance / 2f * elapsedTime * elapsedTime * elapsedTime * elapsedTime + start;
		}
		elapsedTime -= 2f;
		return -distance / 2f * (elapsedTime * elapsedTime * elapsedTime * elapsedTime - 2f) + start;
	}

	// Token: 0x0600065A RID: 1626 RVA: 0x0003523E File Offset: 0x0003343E
	private static float EaseInQuint(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		return distance * elapsedTime * elapsedTime * elapsedTime * elapsedTime * elapsedTime + start;
	}

	// Token: 0x0600065B RID: 1627 RVA: 0x0003525D File Offset: 0x0003345D
	private static float EaseOutQuint(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		elapsedTime -= 1f;
		return distance * (elapsedTime * elapsedTime * elapsedTime * elapsedTime * elapsedTime + 1f) + start;
	}

	// Token: 0x0600065C RID: 1628 RVA: 0x0003528C File Offset: 0x0003348C
	private static float EaseInOutQuint(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 2f : (elapsedTime / (duration / 2f)));
		if (elapsedTime < 1f)
		{
			return distance / 2f * elapsedTime * elapsedTime * elapsedTime * elapsedTime * elapsedTime + start;
		}
		elapsedTime -= 2f;
		return distance / 2f * (elapsedTime * elapsedTime * elapsedTime * elapsedTime * elapsedTime + 2f) + start;
	}

	// Token: 0x0600065D RID: 1629 RVA: 0x000352ED File Offset: 0x000334ED
	private static float EaseInSine(float start, float distance, float elapsedTime, float duration)
	{
		if (elapsedTime > duration)
		{
			elapsedTime = duration;
		}
		return -distance * Mathf.Cos(elapsedTime / duration * 1.5707964f) + distance + start;
	}

	// Token: 0x0600065E RID: 1630 RVA: 0x0003530B File Offset: 0x0003350B
	private static float EaseOutSine(float start, float distance, float elapsedTime, float duration)
	{
		if (elapsedTime > duration)
		{
			elapsedTime = duration;
		}
		return distance * Mathf.Sin(elapsedTime / duration * 1.5707964f) + start;
	}

	// Token: 0x0600065F RID: 1631 RVA: 0x00035326 File Offset: 0x00033526
	private static float EaseInOutSine(float start, float distance, float elapsedTime, float duration)
	{
		if (elapsedTime > duration)
		{
			elapsedTime = duration;
		}
		return -distance / 2f * (Mathf.Cos(3.1415927f * elapsedTime / duration) - 1f) + start;
	}

	// Token: 0x06000660 RID: 1632 RVA: 0x0003534E File Offset: 0x0003354E
	private static float EaseInExpo(float start, float distance, float elapsedTime, float duration)
	{
		if (elapsedTime > duration)
		{
			elapsedTime = duration;
		}
		return distance * Mathf.Pow(2f, 10f * (elapsedTime / duration - 1f)) + start;
	}

	// Token: 0x06000661 RID: 1633 RVA: 0x00035374 File Offset: 0x00033574
	private static float EaseOutExpo(float start, float distance, float elapsedTime, float duration)
	{
		if (elapsedTime > duration)
		{
			elapsedTime = duration;
		}
		return distance * (-Mathf.Pow(2f, -10f * elapsedTime / duration) + 1f) + start;
	}

	// Token: 0x06000662 RID: 1634 RVA: 0x0003539C File Offset: 0x0003359C
	private static float EaseInOutExpo(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 2f : (elapsedTime / (duration / 2f)));
		if (elapsedTime < 1f)
		{
			return distance / 2f * Mathf.Pow(2f, 10f * (elapsedTime - 1f)) + start;
		}
		elapsedTime -= 1f;
		return distance / 2f * (-Mathf.Pow(2f, -10f * elapsedTime) + 2f) + start;
	}

	// Token: 0x06000663 RID: 1635 RVA: 0x00035414 File Offset: 0x00033614
	private static float EaseInCirc(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		return -distance * (Mathf.Sqrt(1f - elapsedTime * elapsedTime) - 1f) + start;
	}

	// Token: 0x06000664 RID: 1636 RVA: 0x0003543F File Offset: 0x0003363F
	private static float EaseOutCirc(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 1f : (elapsedTime / duration));
		elapsedTime -= 1f;
		return distance * Mathf.Sqrt(1f - elapsedTime * elapsedTime) + start;
	}

	// Token: 0x06000665 RID: 1637 RVA: 0x0003546C File Offset: 0x0003366C
	private static float EaseInOutCirc(float start, float distance, float elapsedTime, float duration)
	{
		elapsedTime = ((elapsedTime > duration) ? 2f : (elapsedTime / (duration / 2f)));
		if (elapsedTime < 1f)
		{
			return -distance / 2f * (Mathf.Sqrt(1f - elapsedTime * elapsedTime) - 1f) + start;
		}
		elapsedTime -= 2f;
		return distance / 2f * (Mathf.Sqrt(1f - elapsedTime * elapsedTime) + 1f) + start;
	}

	// Token: 0x02000256 RID: 598
	public enum EaseType
	{
		// Token: 0x0400203C RID: 8252
		Linear,
		// Token: 0x0400203D RID: 8253
		EaseInQuad,
		// Token: 0x0400203E RID: 8254
		EaseOutQuad,
		// Token: 0x0400203F RID: 8255
		EaseInOutQuad,
		// Token: 0x04002040 RID: 8256
		EaseInCubic,
		// Token: 0x04002041 RID: 8257
		EaseOutCubic,
		// Token: 0x04002042 RID: 8258
		EaseInOutCubic,
		// Token: 0x04002043 RID: 8259
		EaseInQuart,
		// Token: 0x04002044 RID: 8260
		EaseOutQuart,
		// Token: 0x04002045 RID: 8261
		EaseInOutQuart,
		// Token: 0x04002046 RID: 8262
		EaseInQuint,
		// Token: 0x04002047 RID: 8263
		EaseOutQuint,
		// Token: 0x04002048 RID: 8264
		EaseInOutQuint,
		// Token: 0x04002049 RID: 8265
		EaseInSine,
		// Token: 0x0400204A RID: 8266
		EaseOutSine,
		// Token: 0x0400204B RID: 8267
		EaseInOutSine,
		// Token: 0x0400204C RID: 8268
		EaseInExpo,
		// Token: 0x0400204D RID: 8269
		EaseOutExpo,
		// Token: 0x0400204E RID: 8270
		EaseInOutExpo,
		// Token: 0x0400204F RID: 8271
		EaseInCirc,
		// Token: 0x04002050 RID: 8272
		EaseOutCirc,
		// Token: 0x04002051 RID: 8273
		EaseInOutCirc
	}

	// Token: 0x02000257 RID: 599
	// (Invoke) Token: 0x06001F0B RID: 7947
	public delegate Vector3 ToVector3<T>(T v);

	// Token: 0x02000258 RID: 600
	// (Invoke) Token: 0x06001F0F RID: 7951
	public delegate float Function(float a, float b, float c, float d);
}
