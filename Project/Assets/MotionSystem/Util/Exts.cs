using UnityEngine;

namespace MotionSystem
{
	public static class Exts
	{
		private const long m_maxLong = 1000000000000;
		private const long m_minLong = -1000000000000;

		public static bool IsSaneNumber(this float f)
		{
			if (float.IsNaN(f)) return false;
			if (f == Mathf.Infinity) return false;
			if (f == Mathf.NegativeInfinity) return false;
			if (f > m_maxLong) return false;
			if (f < m_minLong) return false;
			return true;
		}

		public static Vector3 Clamp(Vector3 v, float length)
		{
			float l = v.magnitude;
			if (l > length) return v / l * length;
			return v;
		}

		public static float Mod(float x, float period)
		{
			float r = x % period;
			return (r >= Float.Zero ? r : r + period);
		}

		public static int Mod(int x, int period)
		{
			int r = x % period;
			return (r >= Int.Zero ? r : r + period);
		}

		public static float Mod(float x)
		{
			return Mod(x, Float.One);
		}

		public static int Mod(int x)
		{
			return Mod(x, Int.One);
		}

		public static float GetFootBalance(float heelElevation, float toeElevation, float footLength)
		{
			// For any moment in time we want to know if the heel or toe is closer to the ground.
			// Rather than a binary value, we need a smooth curve with 0 = heel is closer and 1 = toe is closer.
			// We use the inverse tangens for this as it maps arbritarily large positive or negative values into a -1 to 1 range.
			const int max = 20;
			return Mathf.Atan((
				// Difference in height between heel and toe.
				heelElevation - toeElevation
			) / footLength * max) / Mathf.PI + Float.Half;
			// The 20 multiplier is found by trial and error. A rapid but still slightly smooth change of weight is wanted.
		}

		public static float CyclicDiff(float high, float low, float period, bool skipWrap)
		{
			if (!skipWrap)
			{
				high = Mod(high, period);
				low = Mod(low, period);
			}
			return (high >= low ? high - low : high + period - low);
		}

		public static int CyclicDiff(int high, int low, int period, bool skipWrap)
		{
			if (!skipWrap)
			{
				high = Mod(high, period);
				low = Mod(low, period);
			}
			return (high >= low ? high - low : high + period - low);
		}

		public static float CyclicDiff(float high, float low, float period) { return CyclicDiff(high, low, period, false); }

		public static int CyclicDiff(int high, int low, int period) { return CyclicDiff(high, low, period, false); }

		public static float CyclicDiff(float high, float low) { return CyclicDiff(high, low, Float.One, false); }

		public static int CyclicDiff(int high, int low) { return CyclicDiff(high, low, Int.One, false); }

		// Returns true is compared is lower than comparedTo relative to reference,
		// which is assumed not to lie between compared and comparedTo.
		public static bool CyclicIsLower(float compared, float comparedTo, float reference, float period)
		{
			compared = Mod(compared, period);
			comparedTo = Mod(comparedTo, period);
			if (
				CyclicDiff(compared, reference, period, true)
				<
				CyclicDiff(comparedTo, reference, period, true)
			) return true;
			return false;
		}

		public static bool CyclicIsLower(int compared, int comparedTo, int reference, int period)
		{
			compared = Mod(compared, period);
			comparedTo = Mod(comparedTo, period);
			if (
				CyclicDiff(compared, reference, period, true)
				<
				CyclicDiff(comparedTo, reference, period, true)
			) return true;
			return false;
		}

		public static bool CyclicIsLower(float compared, float comparedTo, float reference)
		{
			return CyclicIsLower(compared, comparedTo, reference, Float.One);
		}

		public static bool CyclicIsLower(int compared, int comparedTo, int reference)
		{
			return CyclicIsLower(compared, comparedTo, reference, Int.One);
		}

		public static float CyclicLerp(float a, float b, float t, float period)
		{
			if (Mathf.Abs(b - a) <= period / Int.Two) { return a * (Int.One - t) + b * t; }
			if (b < a) a -= period; else b -= period;
			return Exts.Mod(a * (Int.One - t) + b * t);
		}

		public static Vector3 ProjectOntoPlane(Vector3 v, Vector3 normal)
		{
			return v - Vector3.Project(v, normal);
		}

		public static Vector3 SetHeight(Vector3 originalVector, Vector3 referenceHeightVector, Vector3 upVector)
		{
			Vector3 originalOnPlane = ProjectOntoPlane(originalVector, upVector);
			Vector3 referenceOnAxis = Vector3.Project(referenceHeightVector, upVector);
			return originalOnPlane + referenceOnAxis;
		}

		public static Vector3 GetHighest(Vector3 a, Vector3 b, Vector3 upVector)
		{
			if (Vector3.Dot(a, upVector) >= Vector3.Dot(b, upVector)) return a;
			return b;
		}

		public static Vector3 GetLowest(Vector3 a, Vector3 b, Vector3 upVector)
		{
			if (Vector3.Dot(a, upVector) <= Vector3.Dot(b, upVector)) return a;
			return b;
		}

		public static Matrix4x4 RelativeMatrix(Transform t, Transform relativeTo)
		{
			return relativeTo.worldToLocalMatrix * t.localToWorldMatrix;
		}

		public static Vector3 TransformVector(Matrix4x4 m, Vector3 v)
		{
			return m.MultiplyPoint(v) - m.MultiplyPoint(Vector3.zero);
		}

		public static Vector3 TransformVector(Transform t, Vector3 v)
		{
			return TransformVector(t.localToWorldMatrix, v);
		}

		public static void TransformFromMatrix(Matrix4x4 matrix, Transform trans)
		{
			trans.rotation = Exts.QuaternionFromMatrix(matrix);
			trans.position = matrix.GetColumn(Int.Three); // uses implicit conversion from Vector4 to Vector3
		}

		public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
		{
			// Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
			Quaternion q = new Quaternion();
			q.w = Mathf.Sqrt(Mathf.Max(0, Int.One + m[0, 0] + m[Int.One, Int.One] + m[Int.Two, Int.Two])) / Int.Two;
			q.x = Mathf.Sqrt(Mathf.Max(0, Int.One + m[0, 0] - m[Int.One, Int.One] - m[Int.Two, Int.Two])) / Int.Two;
			q.y = Mathf.Sqrt(Mathf.Max(0, Int.One - m[0, 0] + m[Int.One, Int.One] - m[Int.Two, Int.Two])) / Int.Two;
			q.z = Mathf.Sqrt(Mathf.Max(0, Int.One - m[0, 0] - m[Int.One, Int.One] + m[Int.Two, Int.Two])) / Int.Two;
			q.x *= Mathf.Sign(q.x * (m[Int.Two, Int.One] - m[Int.One, Int.Two]));
			q.y *= Mathf.Sign(q.y * (m[0, Int.Two] - m[Int.Two, 0]));
			q.z *= Mathf.Sign(q.z * (m[Int.One, 0] - m[0, Int.One]));
			return q;
		}

		public static Matrix4x4 MatrixFromQuaternion(Quaternion q)
		{
			return CreateMatrix(q * Vector3.right, q * Vector3.up, q * Vector3.forward, Vector3.zero);
		}

		public static Matrix4x4 MatrixFromQuaternionPosition(Quaternion q, Vector3 p)
		{
			Matrix4x4 m = MatrixFromQuaternion(q);
			m.SetColumn(Int.Three, p);
			m[Int.Three, Int.Three] = Int.One;
			return m;
		}

		public static Matrix4x4 MatrixSlerp(Matrix4x4 a, Matrix4x4 b, float t)
		{
			t = Mathf.Clamp01(t);
			Matrix4x4 m = MatrixFromQuaternion(Quaternion.Slerp(QuaternionFromMatrix(a), QuaternionFromMatrix(b), t));
			m.SetColumn(Int.Three, a.GetColumn(Int.Three) * (Int.One - t) + b.GetColumn(Int.Three) * t);
			m[Int.Three, Int.Three] = Int.One;
			return m;
		}

		public static Matrix4x4 CreateMatrix(Vector3 right, Vector3 up, Vector3 forward, Vector3 position)
		{
			Matrix4x4 m = Matrix4x4.identity;
			m.SetColumn(0, right);
			m.SetColumn(Int.One, up);
			m.SetColumn(Int.Two, forward);
			m.SetColumn(Int.Three, position);
			m[Int.Three, Int.Three] = Int.One;
			return m;
		}

		public static Matrix4x4 CreateMatrixPosition(Vector3 position)
		{
			Matrix4x4 m = Matrix4x4.identity;
			m.SetColumn(Int.Three, position);
			m[Int.Three, Int.Three] = Int.One;
			return m;
		}

		public static void TranslateMatrix(ref Matrix4x4 m, Vector3 position)
		{
			m.SetColumn(Int.Three, (Vector3)(m.GetColumn(Int.Three)) + position);
			m[Int.Three, Int.Three] = Int.One;
		}

		public static Vector3 ConstantSlerp(Vector3 from, Vector3 to, float angle)
		{
			float value = Mathf.Min(Int.One, angle / Vector3.Angle(from, to));
			return Vector3.Slerp(from, to, value);
		}

		public static Quaternion ConstantSlerp(Quaternion from, Quaternion to, float angle)
		{
			float value = Mathf.Min(Int.One, angle / Quaternion.Angle(from, to));
			return Quaternion.Slerp(from, to, value);
		}

		public static Vector3 ConstantLerp(Vector3 from, Vector3 to, float length)
		{
			return from + Clamp(to - from, length);
		}

		public static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
		{
			Vector3 ab = Vector3.Lerp(a, b, t);
			Vector3 bc = Vector3.Lerp(b, c, t);
			Vector3 cd = Vector3.Lerp(c, d, t);
			Vector3 abc = Vector3.Lerp(ab, bc, t);
			Vector3 bcd = Vector3.Lerp(bc, cd, t);
			return Vector3.Lerp(abc, bcd, t);
		}

		public static bool GetLineSphereIntersections(float[] data, Vector3 lineStart, Vector3 lineDir, Vector3 sphereCenter, float sphereRadius)
		{
			float a = lineDir.sqrMagnitude;
			float b = Int.Two * (Vector3.Dot(lineStart, lineDir) - Vector3.Dot(lineDir, sphereCenter));
			float dot = Vector3.Dot(lineStart, sphereCenter);
			float c = lineStart.sqrMagnitude + sphereCenter.sqrMagnitude - Int.Two * dot - sphereRadius * sphereRadius;
			float d = b * b - Float.Four * a * c;

			if (d < Float.Zero)
				return false;

			float i1 = (-b - Mathf.Sqrt(d)) / (Int.Two * a);
			float i2 = (-b + Mathf.Sqrt(d)) / (Int.Two * a);

			if (i1 < i2)
			{
				data[Int.Zero] = i1;
				data[Int.One] = i2;
				return true;
			}

			data[Int.Zero] = i2;
			data[Int.One] = i1;
			return true;
		}

		public static Transform[] GetTransformChain(Transform upper, Transform lower)
		{
			Transform t = lower;
			int chainLength = Int.One;
			while (t != upper)
			{
				t = t.parent;
				chainLength++;
			}
			Transform[] chain = new Transform[chainLength];
			t = lower;
			for (int j = Int.Zero; j < chainLength; j++)
			{
				chain[chainLength - Int.One - j] = t;
				t = t.parent;
			}
			return chain;
		}

		public static Vector3 GetHeelOffset(Transform ankleT,
									Vector3 ankleHeelVector,
									Transform toeT, Vector3 toeToetipVector,
									Vector3 stanceFootVector,
									Quaternion footBaseRotation)
		{
			// Given the ankle and toe transforms,
			// the heel and toetip positions are calculated.
			Vector3 heel = ankleT.localToWorldMatrix.MultiplyPoint(ankleHeelVector);
			Vector3 toetip = toeT.localToWorldMatrix.MultiplyPoint(toeToetipVector);

			// From this the balance is calculated,
			// relative to the current orientation of the foot base.
			float balance = GetFootBalance(
				(Quaternion.Inverse(footBaseRotation) * heel).y,
				(Quaternion.Inverse(footBaseRotation) * toetip).y,
				stanceFootVector.magnitude
			);

			// From the balance, the heel offset can be calculated.
			Vector3 heelOffset = balance * ((footBaseRotation * stanceFootVector) + (heel - toetip));

			return heelOffset;
		}

		public static Vector3 GetAnklePosition(Transform ankleT, Vector3 ankleHeelVector,
											   Transform toeT, Vector3 toeToetipVector,
											   Vector3 stanceFootVector,
											   Vector3 footBasePosition,
											   Quaternion footBaseRotation)
		{
			// Get the heel offset
			Vector3 heelOffset = GetHeelOffset(
				ankleT, ankleHeelVector, toeT, toeToetipVector,
				stanceFootVector, footBaseRotation
			);

			// Then calculate the ankle position.
			Vector3 anklePosition = (
				footBasePosition
				+ heelOffset
				+ ankleT.localToWorldMatrix.MultiplyVector(ankleHeelVector * -Int.One)
			);

			return anklePosition;
		}
	}
}