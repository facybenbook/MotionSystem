using System;
using UnityEngine;

namespace MotionSystem.Data
{
	[Serializable]
	public class MotionAsset : ScriptableObject
	{
		[ReadOnly]
		public int PoseBaseIndex;
		[ReadOnly]
		public int MotionCount;
		[ReadOnly]
		public int MovingCount;
		[ReadOnly]
		public int StationaryCount;
		public Vector3[] AnkleHeelVector;
		public Vector3[] ToeToetipVector;
		public MotionData[] MotionData;
		public int[] MovingIndexes;
	}
}