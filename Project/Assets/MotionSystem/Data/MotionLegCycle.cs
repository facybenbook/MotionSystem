using UnityEngine;

namespace MotionSystem.Data
{
	[System.Serializable]
	public struct MotionLegCycle
	{
		[ReadOnly]
		public int StanceIndex;
		[ReadOnly]
		public float StanceTime;
		[ReadOnly]
		public float LiftTime;
		[ReadOnly]
		public float LiftoffTime;
		[ReadOnly]
		public float PostliftTime;
		[ReadOnly]
		public float PrelandTime;
		[ReadOnly]
		public float StrikeTime;
		[ReadOnly]
		public float LandTime;
		[ReadOnly]
		public float CycleScaling;
		[ReadOnly]
		public float CycleDistance;
		[ReadOnly]
		public Vector3 CycleCenter;
		[ReadOnly]
		public Vector3 CycleDirection;
		[ReadOnly]
		public Vector3 StancePosition;
		[ReadOnly]
		public Vector3 HeelToetipVector;
		public MotionLegSample[] Samples;
	}
}