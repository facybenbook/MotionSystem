using UnityEngine;

namespace MotionSystem.Data
{
	public class MotionLegState
	{
        // Past and future step
        public Vector3 StepFromPosition;
        public Vector3 StepToPosition;
        public Vector3 StepToPositionGoal;
        public Matrix4x4 StepFromMatrix;
        public Matrix4x4 StepToMatrix;
        public float StepFromTime;
        public float StepToTime;

        // Continiously changing foot state
        public int Step = 0;
        public float CycleTime = 1;
        public float DesignatedCycleTimePrev = 0.9f;
        public Vector3 HipReference;
        public Vector3 AnkleReference;
        public Vector3 FootBase;
        public Quaternion FootBaseRotation;
        public Vector3 Ankle;
        // Foot cycle event time stamps
        public float StanceTime = 0;
        public float LiftTime = 0.1f;
        public float LiftoffTime = 0.2f;
        public float PostliftTime = 0.3f;
        public float PrelandTime = 0.7f;
        public float StrikeTime = 0.8f;
        public float LandTime = 0.9f;
        public LegCyclePhase Phase = LegCyclePhase.Stance;
        // Standing logic
        public bool Parked;

        // Cycle properties
        public Vector3 StancePosition;
        public Vector3 HeelToetipVector;

        public float GetFootGrounding(float time)
        {
            if ((time <= LiftTime) || (time >= LandTime)) 
                return Float.Zero;
            if ((time >= PostliftTime) && (time <= PrelandTime)) 
                return Float.One;
            if (time < PostliftTime)
                return (time - LiftTime) / (PostliftTime - LiftTime);

            return Float.One - (time - PrelandTime) / (LandTime - PrelandTime);
        }
    }
}