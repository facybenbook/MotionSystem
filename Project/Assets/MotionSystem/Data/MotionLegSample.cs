using UnityEngine;

namespace MotionSystem.Data
{
	[System.Serializable]
	public struct MotionLegSample
	{
		[ReadOnly]
		public float Balance;
		[ReadOnly]
		public Vector3 Heel;
		[ReadOnly]
		public Vector3 Toetip;
		[ReadOnly]
		public Vector3 Middle;
		[ReadOnly]
		public Vector3 FootBase;
		[ReadOnly]
		public Matrix4x4 ToeMatrix;
		[ReadOnly]
		public Matrix4x4 AnkleMatrix;
		[ReadOnly]
		public Vector3 FootBaseNormalized;
	}
}