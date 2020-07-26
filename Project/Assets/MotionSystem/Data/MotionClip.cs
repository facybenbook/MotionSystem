using UnityEngine;

namespace MotionSystem.Data
{
	[System.Serializable]
	public struct MotionClip
	{
		[ReadOnly]
		public int Id;
		[ReadOnly]
		public string Name;
		[ReadOnly]
		public AnimationClip Clip;
		public bool Stationary;
		public bool FixFootSkating;
	}
}