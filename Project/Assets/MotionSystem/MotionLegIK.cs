using UnityEngine;

namespace MotionSystem
{ 
	public class MotionLegIK
	{
		private const float m_maxDistFactor = 0.999f;
		private const float m_minDistFactor = 1.001f;

		public void Solve(Transform[] bones, Vector3 target)
		{
			var hip = bones[Int.Zero];
			var knee = bones[Int.One];
			var ankle = bones[Int.Two];

			// Calculate the direction in which the knee should be pointing
			Vector3 vKneeDir = Vector3.Cross(
				ankle.position - hip.position,
				Vector3.Cross(
					ankle.position - hip.position,
					ankle.position - knee.position
				)
			);

			// Get lengths of leg bones
			var fThighLength = (knee.position - hip.position).magnitude;
			var fShinLength = (ankle.position - knee.position).magnitude;

			// Calculate the desired new joint positions
			var pHip = hip.position;
			var pAnkle = target;
			var pKnee = FindKnee(pHip, pAnkle, fThighLength, fShinLength, vKneeDir);

			// Rotate the bone transformations to align correctly
			Quaternion hipRot = Quaternion.FromToRotation(knee.position - hip.position, pKnee - pHip) * hip.rotation;
			if (float.IsNaN(hipRot.x))
			{
#if UNITY_EDITOR
				Debug.LogWarning("hipRot=" + hipRot + " pHip=" + pHip + " pAnkle=" + pAnkle + " fThighLength=" + fThighLength + " fShinLength=" + fShinLength + " vKneeDir=" + vKneeDir);
#endif
				return;
			}

			hip.rotation = hipRot;
			knee.rotation = Quaternion.FromToRotation(ankle.position - knee.position, pAnkle - pKnee) * knee.rotation;
		}

		public Vector3 FindKnee(Vector3 pHip, Vector3 pAnkle, float fThigh, float fShin, Vector3 vKneeDir)
		{
			Vector3 vB = pAnkle - pHip;
			float LB = vB.magnitude;

			float maxDist = (fThigh + fShin) * m_maxDistFactor;
			if (LB > maxDist)
			{
				// ankle is too far away from hip - adjust ankle position
				pAnkle = pHip + (vB.normalized * maxDist);
				vB = pAnkle - pHip;
				LB = maxDist;
			}

			float minDist = Mathf.Abs(fThigh - fShin) * m_minDistFactor;
			if (LB < minDist)
			{
				// ankle is too close to hip - adjust ankle position
				pAnkle = pHip + (vB.normalized * minDist);
				vB = pAnkle - pHip;
				LB = minDist;
			}

			float aa = (LB * LB + fThigh * fThigh - fShin * fShin) / Float.Two / LB;
			float bb = Mathf.Sqrt(fThigh * fThigh - aa * aa);
			Vector3 vF = Vector3.Cross(vB, Vector3.Cross(vKneeDir, vB));
			return pHip + (aa * vB.normalized) + (bb * vF.normalized);
		}
	}
}