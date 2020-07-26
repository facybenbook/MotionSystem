using UnityEngine;
using MotionSystem.Data;

namespace MotionSystem
{
    public class MotionAnalyzer
    {
		public string Name;
		public int Samples;
		public float NativeSpeed;
		public bool Stationary;
		public bool FixFootSkating;
		public float CycleDistance;
		public AnimationClip Clip;
		public Vector3 CycleDirection;
		public MotionLegCycle[] Cycles;
		public float CycleDuration { get; private set; }
		public float CycleSpeed { get; private set; }
		public float CycleOffset { get; private set; }
		public Vector3 CycleVector { get { return CycleDirection * CycleDistance; } }
        public Vector3 CycleVelocity { get { return CycleDirection * CycleSpeed; } }

		private GameObject m_gameObject;
		private MotionController m_controller;
		private Transform m_reference;
		private IgnoreRootMotionOnBone m_ignore;

		public void Init(MotionController controller, 
						 MotionClip source, 
						 Transform reference,
						 IgnoreRootMotionOnBone ignore,
						 int samples = 50)
        {
			Clip = source.Clip;
			Samples = samples;
			Name = Clip.name;
			Stationary = source.Stationary;
			FixFootSkating = source.FixFootSkating;
			m_controller = controller;
			m_gameObject = controller.gameObject;
			m_reference = reference;
			m_ignore = ignore;
		}

        public void Analyze()
        {
			int legs = m_controller.Legs.Length;
			Cycles = new MotionLegCycle[legs];

			for (int leg = Int.Zero; leg < legs; leg++)
			{
				Cycles[leg] = new MotionLegCycle();
				Cycles[leg].Samples = new MotionLegSample[Samples + Int.One];
				for (int i = 0; i < Samples + Int.One; i++)
				{
					Cycles[leg].Samples[i] = new MotionLegSample();
				}
			}

			for (int leg = 0; leg < legs; leg++)
			{
				// Sample ankle, heel, toe, and toetip positions over the length of the animation.
				Transform ankleT = m_controller.Legs[leg].Ankle;
				Transform toeT = m_controller.Legs[leg].Toe;

				float rangeMax = 0;
				float ankleMin; float ankleMax; float toeMin; float toeMax;
				ankleMin = 1000;
				ankleMax = -1000;
				toeMin = 1000;
				toeMax = -1000;
				for (int i = Int.Zero; i < Samples + Int.One; i++)
				{
					var time = i * Float.One / Samples * Clip.length;
					Clip.SampleAnimation(m_gameObject, time);
					SetReferencePosition();
					Cycles[leg].Samples[i].AnkleMatrix = Exts.RelativeMatrix(ankleT, m_reference);
					Cycles[leg].Samples[i].ToeMatrix = Exts.RelativeMatrix(toeT, m_reference);
					Cycles[leg].Samples[i].Heel = Cycles[leg].Samples[i].AnkleMatrix.MultiplyPoint(m_controller.Legs[leg].AnkleHeelVector);
					Cycles[leg].Samples[i].Toetip = Cycles[leg].Samples[i].ToeMatrix.MultiplyPoint(m_controller.Legs[leg].ToeToetipVector);
					Cycles[leg].Samples[i].Middle = (Cycles[leg].Samples[i].Heel + Cycles[leg].Samples[i].Toetip) / Float.Two;

					// For each sample in time we want to know if the heel or toetip is closer to the ground.
					// We need a smooth curve with 0 = ankle is closer and 1 = toe is closer.
					Cycles[leg].Samples[i].Balance = Exts.GetFootBalance(Cycles[leg].Samples[i].Heel.y, Cycles[leg].Samples[i].Toetip.y, m_controller.Legs[leg].FootLength);

					// Find the minimum and maximum extends on all axes of the ankle and toe positions.
					ankleMin = Mathf.Min(ankleMin, Cycles[leg].Samples[i].Heel.y);
					toeMin = Mathf.Min(toeMin, Cycles[leg].Samples[i].Toetip.y);
					ankleMax = Mathf.Max(ankleMax, Cycles[leg].Samples[i].Heel.y);
					toeMax = Mathf.Max(toeMax, Cycles[leg].Samples[i].Toetip.y);
				}
				rangeMax = Mathf.Max(ankleMax - ankleMin, toeMax - toeMin);

				if (!Stationary)
				{
					FindCycleAxis(leg);

					// Foot stance time
					// Find the time when the foot stands most firmly on the ground.
					float stanceValue = Mathf.Infinity;
					for (int i = Int.Zero; i < Samples + Int.One; i++)
					{
						//var s = Cycles[leg].Samples[i];
						float sampleValue =
						// We want the point in time when the max of the heel height and the toe height is lowest
						Mathf.Max(Cycles[leg].Samples[i].Heel.y, Cycles[leg].Samples[i].Toetip.y) / rangeMax
						// Add some bias to poses where the leg is in the middle of the swing
						// i.e. the foot position is close to the middle of the foot curve
						+ Mathf.Abs(
							Exts.ProjectOntoPlane(Cycles[leg].Samples[i].Middle - Cycles[leg].CycleCenter, Vector3.up).magnitude
						) / Cycles[leg].CycleScaling;

						// Use the new value if it is lower (better).
						if (sampleValue < stanceValue)
						{
							Cycles[leg].StanceIndex = i;
							stanceValue = sampleValue;
						}
					}
				}
				else
				{
					Cycles[leg].CycleDirection = Vector3.forward;
					Cycles[leg].CycleScaling = Float.Zero;
					Cycles[leg].StanceIndex = Int.Zero;
				}
				// The stance time
				Cycles[leg].StanceTime = GetTimeFromIndex(Cycles[leg].StanceIndex);

				// The stance index sample
				var ss = Cycles[leg].Samples[Cycles[leg].StanceIndex];
				// Sample the animation at stance time
				//await Task.Delay(100);
				Clip.SampleAnimation(m_gameObject, Cycles[leg].StanceTime * Clip.length);
				SetReferencePosition();
				//RestoreBodyPosRot(m_motionSamples[Cycles[leg].StanceIndex]);
				// Using the stance sample as reference we can now determine:

				// The vector from heel to toetip at the stance pose 
				Cycles[leg].HeelToetipVector = (
					Cycles[leg].Samples[Cycles[leg].StanceIndex].ToeMatrix.MultiplyPoint(m_controller.Legs[leg].ToeToetipVector)
					- Cycles[leg].Samples[Cycles[leg].StanceIndex].AnkleMatrix.MultiplyPoint(m_controller.Legs[leg].AnkleHeelVector)
				);
				Cycles[leg].HeelToetipVector = Exts.ProjectOntoPlane(Cycles[leg].HeelToetipVector, Vector3.up);
				Cycles[leg].HeelToetipVector = Cycles[leg].HeelToetipVector.normalized * m_controller.Legs[leg].FootLength;

				// Calculate foot flight path based on weighted average between ankle flight path and toe flight path,
				// using foot balance as weight.
				// The distance between ankle and toe is accounted for, using the stance pose for reference.
				for (int i = 0; i < Samples + 1; i++)
				{
					//var s = Cycles[leg].Samples[i];
					Cycles[leg].Samples[i].FootBase = (
						(Cycles[leg].Samples[i].Heel) * (Float.One - Cycles[leg].Samples[i].Balance)
						+ (Cycles[leg].Samples[i].Toetip - Cycles[leg].HeelToetipVector) * (Cycles[leg].Samples[i].Balance)
					);
				}

				// The position of the footbase in the stance pose
				Cycles[leg].StancePosition = Cycles[leg].Samples[Cycles[leg].StanceIndex].FootBase;
				Cycles[leg].StancePosition.y = m_controller.GroundPlaneHeight;

				if (!Stationary)
				{
					// Find contact times:
					// Strike time: foot first touches the ground (0% grounding)
					// Down time: all of the foot touches the ground (100% grounding)
					// Lift time: all of the foot still touches the ground but begins to lift (100% grounding)
					// Liftoff time: last part of the foot leaves the ground (0% grounding)
					float timeA;
					float timeB;

					// Find upwards contact times for projected ankle and toe
					// Use the first occurance as lift time and the second as liftoff time
					timeA = FindContactTime(Cycles[leg], false, +Int.One, rangeMax, Float.DotOne);
					//Cycles[leg].DebugInfo.AnkleLiftTime = timeA;
					timeB = FindContactTime(Cycles[leg], true, +Int.One, rangeMax, Float.DotOne);
					//Cycles[leg].DebugInfo.ToeLiftTime = timeB;
					if (timeA < timeB)
					{
						Cycles[leg].LiftTime = timeA;
						Cycles[leg].LiftoffTime = timeB;
					}
					else
					{
						Cycles[leg].LiftTime = timeB;
						Cycles[leg].LiftoffTime = timeA;
					}

					// Find time where swing direction and speed changes significantly.
					// If this happens sooner than the found liftoff time,
					// then the liftoff time must be overwritten with this value.
					timeA = FindSwingChangeTime(Cycles[leg], +1, 0.5f);
					//Cycles[leg].DebugInfo.FootLiftTime = timeA;
					if (Cycles[leg].LiftoffTime > timeA)
					{
						Cycles[leg].LiftoffTime = timeA;
						if (Cycles[leg].LiftTime > Cycles[leg].LiftoffTime)
							Cycles[leg].LiftTime = Cycles[leg].LiftoffTime;
					}

					// Find downwards contact times for projected ankle and toe
					// Use the first occurance as strike time and the second as down time
					timeA = FindContactTime(Cycles[leg], false, -1, rangeMax, Float.DotOne);
					timeB = FindContactTime(Cycles[leg], true, -1, rangeMax, Float.DotOne);
					if (timeA < timeB)
					{
						Cycles[leg].StrikeTime = timeA;
						Cycles[leg].LandTime = timeB;
					}
					else
					{
						Cycles[leg].StrikeTime = timeB;
						Cycles[leg].LandTime = timeA;
					}

					// Find time where swing direction and speed changes significantly.
					// If this happens later than the found strike time,
					// then the strike time must be overwritten with this value.
					timeA = FindSwingChangeTime(Cycles[leg], -1, Float.Half);
					//Cycles[leg].DebugInfo.FootLandTime = timeA;
					if (Cycles[leg].StrikeTime < timeA)
					{
						Cycles[leg].StrikeTime = timeA;
						if (Cycles[leg].LandTime < Cycles[leg].StrikeTime)
							Cycles[leg].LandTime = Cycles[leg].StrikeTime;
					}

					// Set postliftTime and prelandTime
					float softening = 0.2f;

					Cycles[leg].PostliftTime = Cycles[leg].LiftoffTime;
					if (Cycles[leg].PostliftTime < Cycles[leg].LiftTime + softening)
					{
						Cycles[leg].PostliftTime = Cycles[leg].LiftTime + softening;
					}

					Cycles[leg].PrelandTime = Cycles[leg].StrikeTime;
					if (Cycles[leg].PrelandTime > Cycles[leg].LandTime - softening)
					{
						Cycles[leg].PrelandTime = Cycles[leg].LandTime - softening;
					}

					// Calculate the distance traveled during one cycle (for this foot).
					Vector3 stanceSlideVector = (
						Cycles[leg].Samples[GetIndexFromTime(Exts.Mod(Cycles[leg].LiftoffTime + Cycles[leg].StanceTime))].FootBase
						- Cycles[leg].Samples[GetIndexFromTime(Exts.Mod(Cycles[leg].StrikeTime + Cycles[leg].StanceTime))].FootBase
					);
					// FIXME: Assumes horizontal ground plane
					stanceSlideVector.y = 0;
					Cycles[leg].CycleDistance = stanceSlideVector.magnitude / (Cycles[leg].LiftoffTime - Cycles[leg].StrikeTime + Float.One);
					Cycles[leg].CycleDirection = -(stanceSlideVector.normalized);
				}
				else
				{
					Cycles[leg].CycleDirection = Vector3.zero;
					Cycles[leg].CycleDistance = Float.Zero;
				}
			}

			// Find the overall speed and direction traveled during one cycle,
			// based on average of speed values for each individual foot.
			// (They should be very close, but animations are often imperfect,
			// leading to different speeds for different legs.)
			CycleDistance = 0;
			CycleDirection = Vector3.zero;
			for (int leg = 0; leg < legs; leg++)
			{
				CycleDistance += Cycles[leg].CycleDistance;
				CycleDirection += Cycles[leg].CycleDirection;
				//Debug.Log("Cycle direction of leg " + leg + " is " + Cycles[leg].CycleDirection + " with step distance " + Cycles[leg].CycleDistance);
			}
			CycleDistance /= legs;
			CycleDirection /= legs;
			CycleDuration = Clip.length;
			CycleSpeed = CycleDistance / CycleDuration;
			//Debug.Log("Overall cycle direction is " + CycleDirection + " with step distance " + CycleDistance + " and speed " + CycleSpeed);
			NativeSpeed = CycleSpeed * m_gameObject.transform.localScale.x;

			// Calculate normalized foot flight path
			for (int leg = 0; leg < m_controller.Legs.Length; leg++)
			{
				if (!Stationary)
				{
					for (int j = 0; j < Samples; j++)
					{
						int i = Exts.Mod(j + Cycles[leg].StanceIndex, Samples);
						//var s = Cycles[leg].Samples[i];
						float time = GetTimeFromIndex(j);
						Cycles[leg].Samples[i].FootBaseNormalized = Cycles[leg].Samples[i].FootBase;

						if (FixFootSkating)
						{
							// Calculate normalized foot flight path
							// based on the calculated cycle distance of each individual foot
							Vector3 reference = (
								-Cycles[leg].CycleDistance * Cycles[leg].CycleDirection * (time - Cycles[leg].LiftoffTime)
								+ Cycles[leg].Samples[
									GetIndexFromTime(Cycles[leg].LiftoffTime + Cycles[leg].StanceTime)
								].FootBase
							);

							Cycles[leg].Samples[i].FootBaseNormalized = (Cycles[leg].Samples[i].FootBaseNormalized - reference);
							if (Cycles[leg].CycleDirection != Vector3.zero)
							{
								Cycles[leg].Samples[i].FootBaseNormalized = Quaternion.Inverse(
									Quaternion.LookRotation(Cycles[leg].CycleDirection)
								) * Cycles[leg].Samples[i].FootBaseNormalized;
							}

							Cycles[leg].Samples[i].FootBaseNormalized.z /= Cycles[leg].CycleDistance;
							if (time <= Cycles[leg].LiftoffTime) { Cycles[leg].Samples[i].FootBaseNormalized.z = Float.Zero; }
							if (time >= Cycles[leg].StrikeTime) { Cycles[leg].Samples[i].FootBaseNormalized.z = Float.One; }

							Cycles[leg].Samples[i].FootBaseNormalized.y = Cycles[leg].Samples[i].FootBase.y - m_controller.GroundPlaneHeight;
						}
						else
						{
							// Calculate normalized foot flight path
							// based on the cycle distance of the whole motion
							// (the calculated average cycle distance)
							Vector3 reference = (
								-CycleDistance * CycleDirection * (time - Cycles[leg].LiftoffTime * Float.Zero)
								// FIXME: Is same as stance position:
								+ Cycles[leg].Samples[
									GetIndexFromTime(Cycles[leg].LiftoffTime * Float.Zero + Cycles[leg].StanceTime)
								].FootBase
							);

							Cycles[leg].Samples[i].FootBaseNormalized = (Cycles[leg].Samples[i].FootBaseNormalized - reference);
							if (Cycles[leg].CycleDirection != Vector3.zero)
							{
								Cycles[leg].Samples[i].FootBaseNormalized = Quaternion.Inverse(
									Quaternion.LookRotation(CycleDirection)
								) * Cycles[leg].Samples[i].FootBaseNormalized;
							}

							Cycles[leg].Samples[i].FootBaseNormalized.z /= CycleDistance;

							Cycles[leg].Samples[i].FootBaseNormalized.y = Cycles[leg].Samples[i].FootBase.y - m_controller.GroundPlaneHeight;
						}
					}
					Cycles[leg].Samples[Samples] = Cycles[leg].Samples[Int.Zero];
				}
				else
				{
					for (int j = 0; j < Samples; j++)
					{
						int i = Exts.Mod(j + Cycles[leg].StanceIndex, Samples);
						Cycles[leg].Samples[i].FootBaseNormalized = Cycles[leg].Samples[i].FootBase - Cycles[leg].StancePosition;
					}
				}
			}
		}

		public MotionData ToAnalisisData()
		{
			var data = new MotionData()
			{
				Index = 0,
				Name = this.Name,
				Samples = this.Samples,
				Stationary = this.Stationary,
				CycleDirection = this.CycleDirection,
				CycleDistance = this.CycleDistance,
				Cycles = this.Cycles,
				CycleVector = this.CycleVector,
				CycleDuration = this.CycleDuration,
				CycleSpeed = this.CycleSpeed,
				CycleVelocity = this.CycleVelocity,
				CycleOffset = this.CycleOffset
			};
			return data;
		}

		private void SetReferencePosition()
		{
			if (m_ignore == IgnoreRootMotionOnBone.None)
				return;

            Vector3 pos;
            if (m_ignore == IgnoreRootMotionOnBone.Pelvis)
                pos = m_controller.PelvisBone.position;
            else
                pos = m_controller.RootBone.position;

            pos.y = m_reference.position.y;
			m_reference.position = pos;
		}

		private int GetIndexFromTime(float time)
		{
			return Exts.Mod((int)(time * Samples + Float.Half), Samples);
		}

		private float GetTimeFromIndex(int index)
		{
			return index * Float.One / Samples;
		}

		public Vector3 GetFlightFootPosition(int leg, float flightTime, int phase)
        {
			if (Stationary)
			{
				if (phase == Int.Zero) return Vector3.zero;
				if (phase == Int.One) return (-Mathf.Cos(flightTime * Mathf.PI) / Int.Two + Float.Half) * Vector3.forward;
				if (phase == Int.Two) return Vector3.forward;
			}

            float cycleTime;
            if (phase == Int.Zero) 
				cycleTime = Mathf.Lerp(Float.Zero, Cycles[leg].LiftoffTime, flightTime);
			else if (phase == Int.One) 
				cycleTime = Mathf.Lerp(Cycles[leg].LiftoffTime, Cycles[leg].StrikeTime, flightTime);
			else 
				cycleTime = Mathf.Lerp(Cycles[leg].StrikeTime, Float.One, flightTime);

			int index = (int)(cycleTime * Samples);
			float weight = cycleTime * Samples - index;
			if (index >= Samples - Int.One)
			{
				index = Samples - Int.One;
				weight = Float.Zero;
			}
			index = Exts.Mod(index + Cycles[leg].StanceIndex, Samples);
			return (
				Cycles[leg].Samples[index].FootBaseNormalized * (Int.One - weight)
				+ Cycles[leg].Samples[Exts.Mod(index + Int.One, Samples)].FootBaseNormalized * (weight)
			);
		}

		private void FindCycleAxis(int leg)
		{
			// Find axis that feet are moving back and forth along
			// (i.e. Z for characters facing Z, that are walking forward, but could be any direction)
			// FIXME
			// First find the average point of all the points in the foot motion curve
			// (projeted onto the ground plane). This gives us a center.
			Cycles[leg].CycleCenter = Vector3.zero;
			for (int i = 0; i < Samples; i++)
			{
				//var s = Cycles[leg].Samples[i];
				// FIXME: Assumes horizontal ground plane
				Cycles[leg].CycleCenter += Exts.ProjectOntoPlane(Cycles[leg].Samples[i].Middle, Vector3.up);
			}
			Cycles[leg].CycleCenter /= Samples;

			float maxlength;
			// Then find the point furthest away from this center point
			Vector3 footCurvePointA = Cycles[leg].CycleCenter;
			maxlength = Float.Zero;
			for (int i = 0; i < Samples; i++)
			{
				// TODO: Assumes horizontal ground plane
				Vector3 curvePoint = Exts.ProjectOntoPlane(Cycles[leg].Samples[i].Middle, Vector3.up);
				float curLength = (curvePoint - Cycles[leg].CycleCenter).magnitude;
				if (curLength > maxlength)
				{
					footCurvePointA = curvePoint;
					maxlength = curLength;
				}
			}

			// Lastly find the point furthest away from the point we found before
			Vector3 footCurvePointB = footCurvePointA;
			maxlength = Float.Zero;
			for (int i = 0; i < Samples; i++)
			{
				// TODO: Assumes horizontal ground plane
				Vector3 curvePoint = Exts.ProjectOntoPlane(Cycles[leg].Samples[i].Middle, Vector3.up);
				float curLength = (curvePoint - footCurvePointA).magnitude;
				if (curLength > maxlength)
				{
					footCurvePointB = curvePoint;
					maxlength = curLength;
				}
			}

			Cycles[leg].CycleDirection = (footCurvePointB - footCurvePointA).normalized;
			Cycles[leg].CycleScaling = (footCurvePointB - footCurvePointA).magnitude;
		}

		private float FindContactTime(MotionLegCycle data, bool useToe, int searchDirection, float yRange, float threshold)
		{
			// Find the contact time on the height curve, where the (projected ankle or toe)
			// hits or leaves the ground (depending on search direction in time).
			const int spread = Int.Five;
			float curvatureMax = 0;
			int curvatureMaxIndex = data.StanceIndex;
			for (int i = 0; i < Samples && i > -Samples; i += searchDirection)
			{
				// Find second derived by sampling three positions on curve.
				// Spred samples a bit to ignore high frequency noise.
				int[] j = new int[Int.Three];
				float[] value = new float[Int.Three];
				for (int s = 0; s < Int.Three; s++)
				{
					j[s] = Exts.Mod(i + data.StanceIndex - spread + spread * s, Samples);
					if (useToe) value[s] = data.Samples[j[s]].Toetip.y;
					else value[s] = data.Samples[j[s]].Heel.y;
				}

				float curvatureCurrent = Mathf.Atan((value[Int.Two] - value[Int.One]) * Int.Ten / yRange) - Mathf.Atan((value[Int.One] - value[Int.Zero]) * Int.Ten / yRange);
				if (
					// Sample must be above the ground
					(value[Int.One] > m_controller.GroundPlaneHeight)
					// Sample must have highest curvature
					&& (curvatureCurrent > curvatureMax)
					// Slope at sample must go upwards (when going in search direction)
					&& (Mathf.Sign(value[2] - value[0]) == Mathf.Sign(searchDirection))
				)
				{
					curvatureMax = curvatureCurrent;
					curvatureMaxIndex = j[Int.One];
				}
				// Terminate search when foot height is above threshold height above ground
				if (value[Int.One] > m_controller.GroundPlaneHeight + yRange * threshold)
					break;
			}
			return GetTimeFromIndex(Exts.Mod(curvatureMaxIndex - data.StanceIndex, Samples));
		}

		private float FindSwingChangeTime(MotionLegCycle data, int searchDirection, float threshold)
		{
			// Find the contact time on the height curve, where the (projected ankle or toe)
			// hits or leaves the ground (depending on search direction in time).
			const float mult = -0.01f;
			int spread = Samples / Int.Five; // FIXME magic number for spread value
			float stanceSpeed = 0;
			for (int i = Int.Zero; i < Samples && i > -Samples; i += searchDirection)
			{
				// Find speed by sampling curve value ahead and behind.
				int[] j = new int[Int.Three];
				float[] value = new float[Int.Three];
				for (int s = Int.Zero; s < Int.Three; s++)
				{
					j[s] = Exts.Mod(i + data.StanceIndex - spread + spread * s, Samples);
					value[s] = Vector3.Dot(data.Samples[j[s]].FootBase, data.CycleDirection);
				}
				float currentSpeed = value[Int.Two] - value[0];
				if (i == Int.Zero) 
					stanceSpeed = currentSpeed;
				// If speed is too different from speed at stance time,
				// the current time is determined as the swing change time
				if (Mathf.Abs((currentSpeed - stanceSpeed) / stanceSpeed) > threshold)
					return GetTimeFromIndex(Exts.Mod(j[Int.One] - data.StanceIndex, Samples));
			}
			return Exts.Mod(searchDirection * mult);
		}
	}
}