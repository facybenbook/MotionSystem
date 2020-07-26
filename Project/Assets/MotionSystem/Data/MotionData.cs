using System;
using UnityEngine;

namespace MotionSystem.Data
{
    [Serializable]
    public struct MotionData
    {
		[ReadOnly]
		public string Name;
		[ReadOnly]
		public AnimationClip Clip;
		[ReadOnly]
		public int Index;
		[ReadOnly]
		public bool Stationary;
		[ReadOnly]
		public bool FixFootSkating;
		[ReadOnly]
		public int Samples;
		[ReadOnly]
		public float CycleSpeed;
		[ReadOnly]
		public float CycleOffset;
		[ReadOnly]
		public float CycleDistance;
		[ReadOnly]
		public float CycleDuration;
		[ReadOnly]
		public Vector3 CycleVector;
		[ReadOnly]
		public Vector3 CycleVelocity;
		[ReadOnly]
		public Vector3 CycleDirection;
		public MotionLegCycle[] Cycles;

		public Vector3 GetFlightFootPosition(int leg, float flightTime, int phase)
		{
			if (Stationary)
			{
				if (phase == Int.Zero) 
					return Vector3.zero;
				if (phase == Int.One) 
					return (-Mathf.Cos(flightTime * Mathf.PI) / Int.Two + Float.Half) * Vector3.forward;
				if (phase == Int.Two) 
					return Vector3.forward;
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

		public int GetIndexFromTime(float time)
		{
			return Exts.Mod((int)(time * Samples + Float.Half), Samples);
		}

		public float GetTimeFromIndex(int index)
		{
			return index * Float.One / Samples;
		}
	}
}