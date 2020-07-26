using UnityEngine;
using MotionSystem.Data;

namespace MotionSystem
{
	[RequireComponent(typeof(Animator))]
    public class MotionController : MonoBehaviour
    {
		[HideInInspector]
		public Animator Animator;
		[HideInInspector]
		public Transform Transform;
		[Range(-2f, 2f)]
        public float GroundPlaneHeight;
		public Transform RootBone;
		public Transform PelvisBone;
		public MotionLeg[] Legs;
		public MotionAsset MotionAsset;
		public MotionAlignment Alignment;
		public MotionAnimator LegsAnimator;
		[ReadOnly]
		public bool Ready = false;
		[ReadOnly]
		public bool CanEditSkeleton = false;
		[ReadOnly]
		public bool IsAnalisisRunning = false;
		[ReadOnly]
		public Vector3 HipAverage;
		[ReadOnly]
		public Vector3 HipAverageGround;

		private void OnEnable()
        {
			if (Animator == null)
				Animator = GetComponent<Animator>();

			if (Alignment != null)
				Alignment.Reset();

			if (LegsAnimator != null)
				LegsAnimator.Reset();
		}

        private void Awake()
        {
			if (!Ready) 
			{ 
				Debug.LogError(name + ": Motion System is not ready.", this); 
				return; 
			}

			Transform = gameObject.GetComponent<Transform>();
			Animator = GetComponent<Animator>();
			Alignment.Setup(this);
		}

        private void Start()
        {
			if (!Ready)
				return;

			LegsAnimator.Setup(this);
		}

        private void Update()
        {
			if (!Ready)
				return;

			if (LegsAnimator != null)
				LegsAnimator.Update();
		}

        private void LateUpdate()
        {
			if (!Ready)
				return;

			if (Alignment != null)
				Alignment.LateUpdate();

			if (LegsAnimator != null)
				LegsAnimator.LateUpdate();
		}

		private void FixedUpdate()
		{
			if (!Ready)
				return;

			if (Alignment != null)
				Alignment.FixedUpdate();
		}
	}
}