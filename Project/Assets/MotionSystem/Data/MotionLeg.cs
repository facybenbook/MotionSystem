using UnityEngine;

namespace MotionSystem.Data
{
	[System.Serializable]
	public class MotionLeg
	{
		public Transform Hip;
		public Transform Ankle;
		public Transform Toe;
		[Range(0f, 2f)]
		public float FootWidth = 0.15f;
		[Range(0f, 2f)]
		public float FootLength = 0.3f;
		public Vector2 FootOffset;
		public Transform[] LegChain;
		public Transform[] FootChain;
		//[ReadOnly]
		public float LegLength;
		//[ReadOnly]
		public Vector3 AnkleHeelVector;
		//[ReadOnly]
		public Vector3 ToeToetipVector;
	}
}