using System;
using UnityEngine;
using MotionSystem.Data;
using System.Collections.Generic;

namespace MotionSystem
{
	[Serializable]
    public class MotionAnimator
	{
        public bool UseIK = true;
        public bool LegParking = true;
        public NormalizedTime NormalizedTimeMode;
        public BlendStatesType BlendStates;
        public LayerMask GroundLayers = 1; // Default layer per default
        [Range(0f, 90f)]
        public float FootRotationAngle = 45f;
        [Range(0f, 2f)]
        public float IKAdjustmentDistance = 0.5f;
        // Step behavior settings
        [Range(0.01f, 1f)]
        [Tooltip("Model dependent, thus no better default")]
        public float StepDistance = 0.3f; // Model dependent, thus no better default
        [Range(1f, 10f)]
        [Tooltip("Sensible for most models")]
        public float StepDuration = 1.5f; // Sensible for most models
        [Range(0f, 360f)]
        [Tooltip("Sensible for most models, must be less than 180")]
        public float StepRotation = 160; // Sensible for most models, must be less than 180
        [Range(0f, 10f)]
        [Tooltip("Model dependent, thus no better default")]
        public float StepAcceleration = 5.0f; // Model dependent, thus no better default
        [Range(0f, 5f)]
        public float MaxStepHeight = 1.0f;
        [Range(1f, 100f)]
        [Tooltip("Sensible for most models, must be less than 90")]
        public float MaxSlopeAngle = 60; // Sensible for most models, must be less than 90
        // Transition behavior settings
        [Range(0.1f, 1f)]
        public float BlendSmoothing = 0.2f;
        // Tilting settings
        [Range(0f, 1f)]
        public float GroundHugX = 0; // Sensible for humanoids
        [Range(0f, 1f)]
        [Tooltip("Sensible for humanoids")]
        public float GroundHugZ = 0; // Sensible for humanoids
        [Range(0f, 1f)]
        [Tooltip("Sensible default value")]
        public float ClimbTiltAmount = 0.5f; // Sensible default value
        [Range(0f, 1f)]
        [Tooltip("Zero as default")]
        public float ClimbTiltSensitivity = 0.0f; // None as default
        [Range(0.01f, 1f)]
        [Tooltip("Sensible default value")]
        public float AccelerateTiltAmount = 0.02f; // Sensible default value
        [Range(0f, 1f)]
        [Tooltip("Zero as default")]
        public float AccelerateTiltSensitivity = 0.0f; // None as default;
        [Range(0f, 1f)]
        public float SystemWeight = 1f;
        [HideInInspector]
        public MotionLegState[] LegStates;
        [HideInInspector]
        public Vector3 ObjectVelocity;
        [HideInInspector]
        public float TotalMotionWeight;
        [HideInInspector]
        public float TotalCycleMotionWeight;
        [HideInInspector]
        public float Scale;

        public Action<Vector3> OnFootStrike;
        private MotionData[] m_motionData;
        private int m_motionCount;
        private int m_movingCount;
        private MotionController m_controller;
        private MotionAlignment m_alig;
        private MotionLeg[] m_legs;
        private MotionLegIK m_ik;
        private int[] m_ids;
        private bool m_active;
        private float m_curTime;
        private float m_speed;
        private float m_hSpeedSmoothed;
        private Vector3 m_position;
        private Vector3 m_objectVelocity;
        private Quaternion m_rotation;
        private Vector3 m_up;
        private Vector3 m_right;
        private Vector3 m_forward;
        private Vector3 m_baseUpGround;
        private Vector3 m_bodyUp;
        private Vector3 m_legsUp;
        private float m_cycleDuration;
        private float m_cycleDistance;
        private float m_normalizedTime;
        private int[] m_movingIndexes;
        private float[] m_cycleWeights;
        private float[] m_motionWeights;
        private Vector3[] m_basePointFoot;
        private float m_accelerationTiltX;
        private float m_accelerationTiltZ;
        private const float m_min = 0.001f;
        private bool m_resetStates = true;
        private int m_currentMotion = 0;
        private float m_highestWeight = 0f;
        private bool m_velocityChanged = false;
        private const float m_scaleFactor = 0.999f;
        private readonly float[] m_lineIntersections = new float[Int.Two];
        private List<AnimatorClipInfo> m_clipsInfo = new List<AnimatorClipInfo>(Int.Ten);

        public void Setup(MotionController controller)
        {
            m_active = false;

            if (m_ik == null)
                m_ik = new MotionLegIK();
            if (m_controller == null)
                m_controller = controller;
            if (m_legs == null)
                m_legs = m_controller.Legs;
            if (m_alig == null)
                m_alig = m_controller.Alignment;

            if (controller.MotionAsset == null || controller.MotionAsset.MotionData == null)
            {
                Debug.LogError(controller.name + " Motion data is null.");
                return;
            }

            m_normalizedTime = Float.Zero;
            m_basePointFoot = new Vector3[m_legs.Length];
            LegStates = new MotionLegState[m_legs.Length];
            m_motionData = controller.MotionAsset.MotionData;
            m_motionCount = controller.MotionAsset.MotionCount;
            m_movingCount = controller.MotionAsset.MovingCount;

            for (int i = Int.Zero; i < m_legs.Length; i++)
            {
                LegStates[i] = null;
                LegStates[i] = new MotionLegState();
                LegStates[i].StepFromPosition = Vector3.zero;
                LegStates[i].StepToPosition = Vector3.zero;
                LegStates[i].StepToPositionGoal = Vector3.zero;
            }

            //m_ids = new int[m_motionCount];
            m_ids = new int[m_motionCount];
            m_motionWeights = new float[m_motionCount];
            m_cycleWeights = new float[m_motionCount];
            m_movingIndexes = new int[m_movingCount];

            var mi = Int.Zero;
            for (int i = Int.Zero; i < m_motionCount; i++)
            {
                var motion = m_motionData[i];
                m_ids[i] = motion.Clip.GetInstanceID();
                m_motionWeights[i] = Float.Zero;
                m_cycleWeights[i] = Float.Zero;

                if (!motion.Stationary)
                {
                    m_movingIndexes[mi] = i;
                    mi++;
                }
            }
            m_active = true;
        }

        public void Reset()
        {
            m_resetStates = true;
            ResetMotionStates();
            ResetSteps();
        }

        private void ResetMotionStates()
        {
            if (m_legs == null)
                return;

            for (int i = 0; i < m_legs.Length; i++)
            {
                LegStates[i] = null;
                LegStates[i] = new MotionLegState();
                LegStates[i].StepFromPosition = Vector3.zero;
                LegStates[i].StepToPosition = Vector3.zero;
                LegStates[i].StepToPositionGoal = Vector3.zero;
            }

            m_normalizedTime = Float.Zero;
            m_currentMotion = Int.Zero;
        }

        private void ResetSteps()
        {
            if (m_legs == null)
                return;

            m_up = m_controller.Transform.up;
            m_forward = m_controller.Transform.forward;
            m_bodyUp = m_up;
            m_legsUp = m_up;
            m_baseUpGround = m_up;
            m_accelerationTiltX = Float.Zero;
            m_accelerationTiltZ = Float.Zero;
            const float min = 0.01f;

            m_alig.Reset();

            for (int leg = 0; leg < m_legs.Length; leg++)
            {
                LegStates[leg].StepFromTime = Time.time - min;
                LegStates[leg].StepToTime = Time.time;

                LegStates[leg].StepFromMatrix = FindGroundedBase(
                    m_controller.Transform.TransformPoint(LegStates[leg].StancePosition / Scale),
                    m_controller.Transform.rotation,
                    LegStates[leg].HeelToetipVector,
                    false
                );
                LegStates[leg].StepFromPosition = LegStates[leg].StepFromMatrix.GetColumn(Int.Three);
                LegStates[leg].StepToPosition = LegStates[leg].StepFromPosition;
                LegStates[leg].StepToMatrix = LegStates[leg].StepFromMatrix;
            }
            m_cycleDistance = Float.Zero;
            m_cycleDuration = StepDuration;
            m_normalizedTime = Float.Zero;
            m_currentMotion = Int.Zero;
            m_resetStates = false;
        }

        #region Update Logic

        public void Update()
        {
            if (!m_active)
                return;
            if (Time.deltaTime == Float.Zero || Time.timeScale == Float.Zero) 
                return;

            Scale = m_controller.Transform.lossyScale.z;
            UpdateVelocity();
            UpdateWeights();

            if (TotalMotionWeight == Float.Zero)
                return;

            if (m_velocityChanged || m_resetStates)
            {
                UpdateLegsCycle();
                // Get blended cycle data (based on cycle animations only)
                UpdateCycleBlend();
                // Get blended stance time (based on cycle animations only)
                // - getting the average is tricky becuase stance time is cyclic!
                UpdateCycleBlendTime();
            }

            if (m_resetStates)
                ResetSteps();

            CalculateLegsCycle();
            CheckIfAllLegsParked();

            m_resetStates = false;
            m_curTime = Time.time;
        }

        private void UpdateVelocity()
        {
            // When calculating speed, clamp vertical speed to be no longer than horizontal speed
            // to avoid sudden spikes when CharacterController walks up a step.
            Vector3 velocity = Exts.ProjectOntoPlane(m_alig.Velocity, m_up);
            
            m_speed = velocity.magnitude;
            velocity = velocity + m_up * Mathf.Clamp(Vector3.Dot(m_alig.Velocity, m_up), -m_speed, m_speed);
            m_speed = velocity.magnitude;

            m_hSpeedSmoothed = Exts.ProjectOntoPlane(m_alig.VelocitySmoothed, m_up).magnitude;
            ObjectVelocity = (
                m_controller.Transform.InverseTransformPoint(m_alig.VelocitySmoothed)
                - m_controller.Transform.InverseTransformPoint(Vector3.zero)
            );

            // Check if velocity (and turning - not implemented yet) have changed significantly
            m_velocityChanged = false;

            var resultA = (ObjectVelocity - m_objectVelocity).magnitude;
            var resultB = m_min * Mathf.Min(ObjectVelocity.magnitude, m_objectVelocity.magnitude);
            if (resultA > resultB || m_resetStates)
            {
                m_velocityChanged = true;
                m_objectVelocity = ObjectVelocity;
            }
        }

        private void UpdateWeights()
        {
            var count = m_controller.Animator.GetCurrentAnimatorClipInfoCount(Int.Zero);
            m_highestWeight = Float.Zero;

            if (count > Int.Zero)
            {
                m_clipsInfo.Clear();
                m_controller.Animator.GetCurrentAnimatorClipInfo(Int.Zero, m_clipsInfo);
                for (int an = 0; an < count; an++)
                    SetAnimationWeight(m_clipsInfo[an]);
            }

            if (BlendStates == BlendStatesType.CurrentAndNextState)
            {
                count = m_controller.Animator.GetNextAnimatorClipInfoCount(Int.Zero);
                if (count > Int.Zero)
                {
                    m_clipsInfo.Clear();
                    m_controller.Animator.GetNextAnimatorClipInfo(Int.Zero, m_clipsInfo);
                    for (int an = 0; an < count; an++)
                        SetAnimationWeight(m_clipsInfo[an]);
                }
            }

            // Get summed weights
            TotalMotionWeight = Float.Zero;
            TotalCycleMotionWeight = Float.Zero;

            for (int m = 0; m < m_motionCount; m++)
            {
                if (!m_motionData[m].Stationary)
                    m_cycleWeights[m] = m_motionWeights[m];

                TotalMotionWeight += m_motionWeights[m];
                TotalCycleMotionWeight += m_cycleWeights[m];
            }

            if (TotalMotionWeight == Float.Zero)
                return;

            if (TotalCycleMotionWeight > Float.Zero)
            {
                // Make weights sum to 1
                for (int m = Int.Zero; m < m_motionCount; m++)
                {
                    m_motionWeights[m] /= TotalMotionWeight;
                    m_cycleWeights[m] /= TotalCycleMotionWeight;
                }
            }
        }

        private void UpdateLegsCycle()
        {
            // Get blended cycle data (based on all animations)
            for (int i = Int.Zero; i < m_legs.Length; i++)
            {
                LegStates[i].StancePosition = Vector3.zero;
                LegStates[i].HeelToetipVector = Vector3.zero;
            }
            for (int i = Int.Zero; i < m_motionCount; i++)
            {
                if (m_motionWeights[i] > Float.Zero)
                {
                    var motion = m_motionData[i];
                    var weight = m_motionWeights[i];
                    for (int leg = Int.Zero; leg < m_legs.Length; leg++)
                    {
                        LegStates[leg].StancePosition += (
                            motion.Cycles[leg].StancePosition * Scale * weight
                        );
                        LegStates[leg].HeelToetipVector += (
                            motion.Cycles[leg].HeelToetipVector * Scale * weight
                        );
                    }
                }
            }
        }

        private void UpdateCycleBlend()
        {
            if (TotalCycleMotionWeight > Float.Zero)
            {
                for (int i = 0; i < m_legs.Length; i++)
                {
                    LegStates[i].LiftTime = Float.Zero;
                    LegStates[i].LiftoffTime = Float.Zero;
                    LegStates[i].PostliftTime = Float.Zero;
                    LegStates[i].PrelandTime = Float.Zero;
                    LegStates[i].StrikeTime = Float.Zero;
                    LegStates[i].LandTime = Float.Zero;
                }
                for (int m = Int.Zero; m < m_movingIndexes.Length; m++)
                {
                    var index = m_movingIndexes[m];
                    if (m_cycleWeights[index] > Float.Zero)
                    {
                        var motion = m_motionData[index];
                        var weight = m_cycleWeights[index];
                        for (int i = Int.Zero; i < m_legs.Length; i++)
                        {
                            LegStates[i].LiftTime += motion.Cycles[i].LiftTime * weight;
                            LegStates[i].LiftoffTime += motion.Cycles[i].LiftoffTime * weight;
                            LegStates[i].PostliftTime += motion.Cycles[i].PostliftTime * weight;
                            LegStates[i].PrelandTime += motion.Cycles[i].PrelandTime * weight;
                            LegStates[i].StrikeTime += motion.Cycles[i].StrikeTime * weight;
                            LegStates[i].LandTime += motion.Cycles[i].LandTime * weight;
                        }
                    }
                }
            }
        }

        private void UpdateCycleBlendTime()
        {
            if (TotalCycleMotionWeight > 0)
            {
                for (int i = Int.Zero; i < m_legs.Length; i++)
                {
                    Vector2 stanceTimeVector = Vector2.zero;
                    for (int m = Int.Zero; m < m_movingIndexes.Length; m++)
                    {
                        var index = m_movingIndexes[m];
                        if (m_cycleWeights[index] > Float.Zero)
                        {
                            var motion = m_motionData[index];
                            stanceTimeVector += new Vector2(
                                Mathf.Cos(motion.Cycles[i].StanceTime * Float.Two * Mathf.PI),
                                Mathf.Sin(motion.Cycles[i].StanceTime * Float.Two * Mathf.PI)
                            ) * m_cycleWeights[index];
                        }
                    }
                    LegStates[i].StanceTime = Exts.Mod(
                        Mathf.Atan2(stanceTimeVector.y, stanceTimeVector.x) / Float.Two / Mathf.PI
                    );
                }
            }
        }

        private void CalculateLegsCycle()
        {
            float cycleFrequency = 0;
            float animatedCycleSpeed = 0;
            for (int m = 0; m < m_motionCount; m++)
            {
                var motion = m_motionData[m];
                var weight = m_cycleWeights[m];
                if (weight > Float.Zero)
                {
                    if (!motion.Stationary)
                        cycleFrequency += (Float.One / motion.CycleDuration) * weight;

                    animatedCycleSpeed += motion.CycleSpeed * weight;
                }
            }
            float desiredCycleDuration = StepDuration;
            if (cycleFrequency > Float.Zero)
                desiredCycleDuration = Float.One / cycleFrequency;

            // Make the step duration / step length relation follow a sqrt curve
            float speedMultiplier = Float.One;
            if (m_speed != Float.Zero)
                speedMultiplier = animatedCycleSpeed * Scale / m_speed;
            if (speedMultiplier > Float.Zero)
                desiredCycleDuration *= Mathf.Sqrt(speedMultiplier);

            // Enforce short enough step duration while rotating
            float verticalAngularVelocity = Vector3.Project(m_alig.Rotation * m_alig.AngularVelocitySmoothed, m_up).magnitude;
            if (verticalAngularVelocity > Float.Zero)
            {
                desiredCycleDuration = Mathf.Min(
                   StepRotation / verticalAngularVelocity,
                   desiredCycleDuration
               );
            }

            // Enforce short enough step duration while accelerating
            float groundAccelerationMagnitude = Exts.ProjectOntoPlane(m_alig.AccelerationSmoothed, m_up).magnitude;
            if (groundAccelerationMagnitude > Float.Zero)
            {
                desiredCycleDuration = Mathf.Clamp(
                    StepAcceleration / groundAccelerationMagnitude,
                    desiredCycleDuration / Float.Two,
                    desiredCycleDuration
                );
            }

            // Enforce short enough step duration in general
            desiredCycleDuration = Mathf.Min(desiredCycleDuration, StepDuration);
            m_cycleDuration = desiredCycleDuration;
            // Set cycle distance
            m_cycleDistance = m_cycleDuration * m_speed;
        }

        private void CheckIfAllLegsParked()
        {
            // Check if all legs are "parked" i.e. standing still
            bool allParked = false;
            if (LegParking)
            {
                allParked = true;
                for (int leg = 0; leg < m_legs.Length; leg++)
                {
                    if (LegStates[leg].Parked == false)
                    {
                        allParked = false;
                        break;
                    }
                }
            }

            // Synchronize animations
            if (!allParked)
                UpdateNormalizedTime();
        }

        private void SetAnimationWeight(AnimatorClipInfo info)
        {
            var id = info.clip.GetInstanceID();
            for (int i = Int.Zero; i < m_ids.Length; i++)
            {
                if (m_ids[i] == id)
                {
                    //Debug.Log("index: " + i + " name: " + name + " weight: " + info.weight);
                    m_motionWeights[i] = info.weight;

                    if (!m_motionData[i].Stationary)
                        m_cycleWeights[i] = info.weight;

                    if (info.weight > m_highestWeight)
                    {
                        m_highestWeight = info.weight;
                        m_currentMotion = i;
                    }
                    break;
                }
            }
        }

        private void UpdateNormalizedTime()
        {
            if (NormalizedTimeMode == NormalizedTime.AnimationState)
            {
                m_normalizedTime = m_controller.Animator.GetCurrentAnimatorStateInfo(Int.Zero).normalizedTime;
            }
            else
            {
                m_normalizedTime = Exts.Mod(m_normalizedTime + (Float.One / m_cycleDuration) * Time.deltaTime);
                m_controller.Animator.Play(Int.Zero, Int.Zero, m_normalizedTime - m_motionData[m_currentMotion].CycleOffset);
            }
        }

        #endregion

        #region LateUpdate Logic

        public void LateUpdate()
        {
            if (Time.deltaTime == Float.Zero || Time.timeScale == Float.Zero) 
                return;

            MonitorFootsteps();

            m_position = m_alig.Position;
            m_rotation = m_alig.Rotation;

            m_up = m_rotation * Vector3.up;
            m_right = m_rotation * Vector3.right;
            m_forward = m_rotation * Vector3.forward;

            // Do not run locomotion system in this frame if locomotion weights are all zero
            if (!m_active) 
                return;
            if (m_curTime != Time.time) 
                return;
            if (!UseIK) 
                return;

            for (int i = 0; i < m_legs.Length; i++)
            {
                var leg = m_legs[i];
                var legState = LegStates[i];
                m_basePointFoot[i] = Vector3.zero;
                // Calculate current time in foot cycle
                float designatedCycleTime = Exts.CyclicDiff(m_normalizedTime, legState.StanceTime);
                // See if this time is beginning of a new step
                bool newStep = false;
                if (designatedCycleTime < legState.DesignatedCycleTimePrev - Float.Half)
                {
                    newStep = true;
                    legState.Step++;
                    if (!legState.Parked)
                    {
                        legState.StepFromTime = legState.StepToTime;
                        legState.StepFromPosition = legState.StepToPosition;
                        legState.StepFromMatrix = legState.StepToMatrix;
                        legState.CycleTime = designatedCycleTime;
                    }
                    legState.Parked = false;

                }
                legState.DesignatedCycleTimePrev = designatedCycleTime;

                // Find future step time	
                legState.StepToTime = (
                    Time.time
                    + (Float.One - designatedCycleTime) * m_cycleDuration
                );

                float predictedStrikeTime = (legState.StrikeTime - designatedCycleTime) * m_cycleDuration;

                if (designatedCycleTime >= legState.StrikeTime)
                    legState.CycleTime = designatedCycleTime;
                else
                {
                    // Calculate how fast cycle must go to catch up from a possible parked state
                    legState.CycleTime += (
                        (legState.StrikeTime - legState.CycleTime)
                         * Time.deltaTime / predictedStrikeTime);
                }

                if (legState.CycleTime >= designatedCycleTime)
                    legState.CycleTime = designatedCycleTime;

                // Find future step position and alignment
                if (legState.CycleTime < legState.StrikeTime)
                {
                    // Value from 0.0 at liftoff time to 1.0 at strike time
                    float flightTime = Mathf.InverseLerp(
                        legState.LiftoffTime, legState.StrikeTime, legState.CycleTime);

                    // Find future step alignment
                    Quaternion newPredictedRotation = Quaternion.AngleAxis(
                        m_alig.AngularVelocitySmoothed.magnitude * (legState.StepToTime - Time.time),
                        m_alig.AngularVelocitySmoothed
                    ) * m_alig.Rotation;

                    // Apply smoothing of predicted step rotation
                    Quaternion predictedRotation;
                    if (legState.CycleTime <= legState.LiftoffTime)
                    {
                        // No smoothing if foot hasn't lifted off the ground yet
                        predictedRotation = newPredictedRotation;
                    }
                    else
                    {
                        Quaternion oldPredictedRotation = Exts.QuaternionFromMatrix(legState.StepToMatrix);
                        oldPredictedRotation =
                            Quaternion.FromToRotation(oldPredictedRotation * Vector3.up, m_up)
                            * oldPredictedRotation;

                        float rotationSeekSpeed = Mathf.Max(
                            m_alig.AngularVelocitySmoothed.magnitude * Float.Three,
                            StepRotation / StepDuration
                        );
                        float maxRotateAngle = rotationSeekSpeed / flightTime * Time.deltaTime;
                        predictedRotation = Exts.ConstantSlerp(
                            oldPredictedRotation, newPredictedRotation, maxRotateAngle);
                    }

                    // Find future step position (prior to raycast)
                    Vector3 newStepPosition;

                    // Find out how much the character is turning
                    float turnSpeed = Vector3.Dot(m_alig.AngularVelocitySmoothed, m_up);

                    if (turnSpeed * m_cycleDuration < Float.Five)
                    {
                        // Linear prediction if no turning
                        newStepPosition = (
                            m_alig.Position
                            + predictedRotation * legState.StancePosition
                            + m_alig.Velocity * (legState.StepToTime - Time.time)
                        );
                    }
                    else
                    {
                        // If character is turning, assume constant turning
                        // and do circle-based prediction
                        Vector3 turnCenter = Vector3.Cross(m_up, m_alig.Velocity) / (turnSpeed * Mathf.PI / Float._180Deg);
                        Vector3 predPos = turnCenter + Quaternion.AngleAxis(
                            turnSpeed * (legState.StepToTime - Time.time),
                            m_up
                        ) * -turnCenter;

                        newStepPosition = (
                            m_alig.Position
                            + predictedRotation * legState.StancePosition
                            + predPos
                        );
                    }

                    newStepPosition = Exts.SetHeight(
                        newStepPosition, m_position + m_controller.GroundPlaneHeight * m_up * Scale, m_up
                    );

                    // Get position and orientation projected onto the ground
                    Matrix4x4 groundedBase = FindGroundedBase(
                        newStepPosition,
                        predictedRotation,
                        legState.HeelToetipVector,
                        true
                    );
                    newStepPosition = groundedBase.GetColumn(3);

                    // Apply smoothing of predicted step position
                    if (newStep)
                    {
                        // No smoothing if foot hasn't lifted off the ground yet
                        legState.StepToPosition = newStepPosition;
                        legState.StepToPositionGoal = newStepPosition;
                    }
                    else
                    {
                        float stepSeekSpeed = Mathf.Max(
                            m_speed * Float.Three + m_alig.AccelerationSmoothed.magnitude / Float.Ten,
                            leg.FootLength * Scale * Float.Three
                        );

                        float towardStrike = legState.CycleTime / legState.StrikeTime;

                        // Evaluate if new potential goal is within reach
                        if ((newStepPosition - legState.StepToPosition).sqrMagnitude
                            < Mathf.Pow(stepSeekSpeed * ((Float.One / towardStrike) - Float.One), Float.Two))
                        {
                            legState.StepToPositionGoal = newStepPosition;
                        }

                        // Move towards goal - faster initially, then slower
                        Vector3 moveVector = legState.StepToPositionGoal - legState.StepToPosition;
                        if (moveVector != Vector3.zero && predictedStrikeTime > Float.Zero)
                        {
                            float moveVectorMag = moveVector.magnitude;
                            float moveDist = Mathf.Min(
                                moveVectorMag,
                                Mathf.Max(
                                    stepSeekSpeed / Mathf.Max(Float.DotOne, flightTime) * Time.deltaTime,
                                    (Float.One + Float.Two * Mathf.Pow(towardStrike - Float.One, Float.Two))
                                        * (Time.deltaTime / predictedStrikeTime)
                                        * moveVectorMag
                                )
                            );
                            legState.StepToPosition += (
                                (legState.StepToPositionGoal - legState.StepToPosition)
                                / moveVectorMag * moveDist
                            );
                        }
                    }

                    groundedBase.SetColumn(Int.Three, legState.StepToPosition);
                    groundedBase[Int.Three, Int.Three] = Int.One;
                    legState.StepToMatrix = groundedBase;
                }

                if (LegParking)
                {
                    // Check if old and new footstep has
                    // significant difference in position or rotation
                    float distToNextStep = Exts.ProjectOntoPlane(
                        legState.StepToPosition - legState.StepFromPosition, m_up
                    ).magnitude;

                    bool significantStepDifference = (
                        distToNextStep > StepDistance ||
                        Vector3.Angle(
                            legState.StepToMatrix.GetColumn(Int.Two),
                            legState.StepFromMatrix.GetColumn(Int.Two)
                        ) > StepRotation / Float.Two
                    );

                    // Park foot's cycle if the step length/rotation is below threshold
                    if (newStep && !significantStepDifference)
                        legState.Parked = true;

                    // Allow unparking during first part of cycle if the
                    // step length/rotation is now above threshold
                    const float acc = 0.67f;
                    if (legState.Parked && (designatedCycleTime < acc) && significantStepDifference)
                        legState.Parked = false;

                    if (legState.Parked) 
                        legState.CycleTime = Float.Zero;
                }
            }

            // Calculate base point
            Vector3 tangentDir = Quaternion.Inverse(m_alig.Rotation) * m_alig.Velocity;
            // This is in object space, so OK to set y to 0
            tangentDir.y = 0;
            if (tangentDir.sqrMagnitude > 0)
                tangentDir = tangentDir.normalized;

            Vector3 basePoint = Vector3.zero;
            Vector3 baseVel = Vector3.zero;
            Vector3 avgFootPoint = Vector3.zero;
            float baseSummedWeight = Float.Zero;

            for (int leg = 0; leg < m_legs.Length; leg++)
            {
                m_basePointFoot[leg] = Vector3.zero;
                // Calculate base position (starts and ends in tangent to surface)
                // weight goes 1 -> 0 -> 1 as cycleTime goes from 0 to 1
                float weight = Mathf.Cos(LegStates[leg].CycleTime * Float.Two * Mathf.PI) / Float.Two + Float.Half;
                baseSummedWeight += weight + m_min;

                // Value from 0.0 at lift time to 1.0 at land time
                float strideTime = Mathf.InverseLerp(
                    LegStates[leg].LiftTime, LegStates[leg].LandTime, LegStates[leg].CycleTime);
                float strideSCurve = -Mathf.Cos(strideTime * Mathf.PI) / Float.Two + Float.Half;

                Vector3 stepBodyPoint = m_controller.Transform.TransformDirection(-LegStates[leg].StancePosition) * Scale;

                m_basePointFoot[leg] = ((
                        LegStates[leg].StepFromPosition
                        + LegStates[leg].StepFromMatrix.MultiplyVector(tangentDir)
                            * m_cycleDistance * LegStates[leg].CycleTime
                    ) * (Float.One - strideSCurve)
                    + (LegStates[leg].StepToPosition
                        + LegStates[leg].StepToMatrix.MultiplyVector(tangentDir)
                            * m_cycleDistance * (LegStates[leg].CycleTime - Float.One)
                    ) * strideSCurve
                );
                
                if (float.IsNaN(m_basePointFoot[leg].x) || float.IsNaN(m_basePointFoot[leg].y) || float.IsNaN(m_basePointFoot[leg].z))
                    Debug.LogError("legStates[leg].cycleTime=" + LegStates[leg].CycleTime + ", strideSCurve=" + strideSCurve + ", tangentDir=" + tangentDir + ", cycleDistance=" + m_cycleDistance + ", legStates[leg].stepFromPosition=" + LegStates[leg].StepFromPosition + ", legStates[leg].stepToPosition=" + LegStates[leg].StepToPosition + ", legStates[leg].stepToMatrix.MultiplyVector(tangentDir)=" + LegStates[leg].StepToMatrix.MultiplyVector(tangentDir) + ", legStates[leg].stepFromMatrix.MultiplyVector(tangentDir)=" + LegStates[leg].StepFromMatrix.MultiplyVector(tangentDir));

                avgFootPoint += m_basePointFoot[leg];
                basePoint += (m_basePointFoot[leg] + stepBodyPoint) * (weight + m_min);
                baseVel += (LegStates[leg].StepToPosition - LegStates[leg].StepFromPosition) * (Float.One - weight + m_min);
            }

            avgFootPoint /= m_legs.Length;
            basePoint /= baseSummedWeight;

            if (float.IsNaN(basePoint.x) || 
                float.IsNaN(basePoint.y) || 
                float.IsNaN(basePoint.z)) 
                basePoint = m_position;

            Vector3 groundBasePoint = basePoint + m_up * m_controller.GroundPlaneHeight;

            // Calculate base up vector
            Vector3 baseUp = m_up;
            if (GroundHugX >= Float.Zero || GroundHugZ >= Float.Zero)
            {
                // Ground-based Base Up Vector
                Vector3 baseUpGroundNew = m_up * Float.DotOne;
                for (int leg = 0; leg < m_legs.Length; leg++)
                {
                    Vector3 vec = (m_basePointFoot[leg] - avgFootPoint);
                    baseUpGroundNew += Vector3.Cross(Vector3.Cross(vec, m_baseUpGround), vec);
                }

                float baseUpGroundNewUpPart = Vector3.Dot(baseUpGroundNew, m_up);
                if (baseUpGroundNewUpPart > Float.Zero)
                {
                    // Scale vector such that vertical element has length of 1
                    baseUpGroundNew /= baseUpGroundNewUpPart;
                    m_baseUpGround = baseUpGroundNew;
                }

                if (GroundHugX >= Float.One && GroundHugZ >= Float.One)
                {
                    baseUp = m_baseUpGround.normalized;
                }
                else
                {
                    baseUp = (
                        m_up
                        + GroundHugX * Vector3.Project(m_baseUpGround, m_right)
                        + GroundHugZ * Vector3.Project(m_baseUpGround, m_forward)
                    ).normalized;
                }
            }
            // Velocity-based Base Up Vector
            Vector3 baseUpVel = m_up;
            if (baseVel != Vector3.zero) 
                baseUpVel = Vector3.Cross(baseVel, Vector3.Cross(m_up, baseVel));
            // Scale vector such that vertical element has length of 1
            baseUpVel /= Vector3.Dot(baseUpVel, m_up);
            // Calculate acceleration direction in local XZ plane
            Vector3 accelerationDir = Vector3.zero;
            if (AccelerateTiltAmount * AccelerateTiltSensitivity != Float.Zero)
            {
                float accelX = Vector3.Dot(
                    m_alig.AccelerationSmoothed * AccelerateTiltSensitivity * AccelerateTiltAmount,
                    m_right
                ) * (Float.One - GroundHugX);
                float accelZ = Vector3.Dot(
                    m_alig.AccelerationSmoothed * AccelerateTiltSensitivity * AccelerateTiltAmount,
                    m_forward
                ) * (Float.One - GroundHugZ);
                m_accelerationTiltX = Mathf.Lerp(m_accelerationTiltX, accelX, Time.deltaTime * Float.Ten);
                m_accelerationTiltZ = Mathf.Lerp(m_accelerationTiltZ, accelZ, Time.deltaTime * Float.Ten);
                accelerationDir = (
                    (m_accelerationTiltX * m_right + m_accelerationTiltZ * m_forward)
                    // a curve that goes towards 1 as speed goes towards infinity:
                    * (Float.One - Float.One / (m_hSpeedSmoothed * AccelerateTiltSensitivity + Float.One))
                );
            }

            // Calculate tilting direction in local XZ plane
            Vector3 tiltDir = Vector3.zero;
            if (ClimbTiltAmount * ClimbTiltAmount != Float.Zero)
            {
                tiltDir = (
                    (
                        Vector3.Project(baseUpVel, m_right) * (Float.One - GroundHugX)
                        + Vector3.Project(baseUpVel, m_forward) * (Float.One - GroundHugZ)
                    ) * -ClimbTiltAmount
                    // a curve that goes towards 1 as speed goes towards infinity:
                    * (Float.One - Float.One / (m_hSpeedSmoothed * ClimbTiltSensitivity + Float.One))
                );
            }

            // Up vector and rotations for the torso
            m_bodyUp = (baseUp + accelerationDir + tiltDir).normalized;
            Quaternion bodyRotation = Quaternion.AngleAxis(
                Vector3.Angle(m_up, m_bodyUp),
                Vector3.Cross(m_up, m_bodyUp)
            );

            // Up vector and rotation for the legs
            m_legsUp = (m_up + accelerationDir).normalized;
            Quaternion legsRotation = Quaternion.AngleAxis(
                Vector3.Angle(m_up, m_legsUp),
                Vector3.Cross(m_up, m_legsUp)
            );

            for (int leg = 0; leg < m_legs.Length; leg++)
            {
                // Value from 0.0 at liftoff time to 1.0 at strike time
                float flightTime = Mathf.InverseLerp(
                    LegStates[leg].LiftoffTime, LegStates[leg].StrikeTime, LegStates[leg].CycleTime);

                // Value from 0.0 at lift time to 1.0 at land time
                float strideTime = Mathf.InverseLerp(
                    LegStates[leg].LiftTime, LegStates[leg].LandTime, LegStates[leg].CycleTime);

                int phase;
                float phaseTime = 0;
                if (LegStates[leg].CycleTime < LegStates[leg].LiftoffTime)
                {
                    phase = 0; 
                    phaseTime = Mathf.InverseLerp(
                        0, LegStates[leg].LiftoffTime, LegStates[leg].CycleTime
                    );
                }
                else if (LegStates[leg].CycleTime > LegStates[leg].StrikeTime)
                {
                    phase = Int.Two; 
                    phaseTime = Mathf.InverseLerp(
                        LegStates[leg].StrikeTime, 1, LegStates[leg].CycleTime
                    );
                }
                else
                {
                    phase = Int.One; 
                    phaseTime = flightTime;
                }

                // Calculate foot position on foot flight path from old to new step
                Vector3 flightPos = Vector3.zero;
                for (int m = 0; m < m_motionCount; m++)
                {
                    var motion = m_motionData[m];
                    float weight = m_motionWeights[m];
                    if (weight > Float.Zero)
                        flightPos += motion.GetFlightFootPosition(leg, phaseTime, phase) * weight;
                }

                // Start and end point at step from and step to positions
                Vector3 pointFrom = LegStates[leg].StepFromPosition;
                Vector3 pointTo = LegStates[leg].StepToPosition;
                Vector3 normalFrom = LegStates[leg].StepFromMatrix.MultiplyVector(Vector3.up);
                Vector3 normalTo = LegStates[leg].StepToMatrix.MultiplyVector(Vector3.up);

                float flightProgressionLift = Mathf.Sin(flightPos.z * Mathf.PI);
                float flightTimeLift = Mathf.Sin(flightTime * Mathf.PI);

                // Calculate horizontal part of flight paths
                LegStates[leg].FootBase = pointFrom * (Float.One - flightPos.z) + pointTo * flightPos.z;

                Vector3 offset =
                    m_alig.Position + m_alig.Rotation * LegStates[leg].StancePosition
                    - Vector3.Lerp(pointFrom, pointTo, LegStates[leg].CycleTime);

                LegStates[leg].FootBase += Exts.ProjectOntoPlane(offset * flightProgressionLift, m_legsUp);

                // Calculate vertical part of flight paths
                Vector3 midPoint = (pointFrom + pointTo) / Float.Two;
                float tangentHeightFrom = (
                    Vector3.Dot(normalFrom, pointFrom - midPoint) / Vector3.Dot(normalFrom, m_legsUp)
                );
                float tangentHeightTo = (
                    Vector3.Dot(normalTo, pointTo - midPoint) / Vector3.Dot(normalTo, m_legsUp)
                );
                float heightMidOffset = Mathf.Max(tangentHeightFrom, tangentHeightTo) * Float.Two / Mathf.PI;

                LegStates[leg].FootBase += Mathf.Max(0, heightMidOffset * flightProgressionLift - flightPos.y * Scale) * m_legsUp;

                // Footbase rotation
                Quaternion footBaseRotationFromSteps = Quaternion.Slerp(
                    Exts.QuaternionFromMatrix(LegStates[leg].StepFromMatrix),
                    Exts.QuaternionFromMatrix(LegStates[leg].StepToMatrix),
                    flightTime
                );

                if (strideTime < Float.Half)
                {
                    LegStates[leg].FootBaseRotation = Quaternion.Slerp(
                        Exts.QuaternionFromMatrix(LegStates[leg].StepFromMatrix),
                        m_rotation,
                        strideTime * Float.Two
                    );
                }
                else
                {
                    LegStates[leg].FootBaseRotation = Quaternion.Slerp(
                        m_rotation,
                        Exts.QuaternionFromMatrix(LegStates[leg].StepToMatrix),
                        strideTime * Float.Two - Float.One
                    );
                }

                float footRotationAngle = Quaternion.Angle(m_rotation, LegStates[leg].FootBaseRotation);
                if (footRotationAngle > FootRotationAngle)
                {
                    LegStates[leg].FootBaseRotation = Quaternion.Slerp(
                        m_rotation,
                        LegStates[leg].FootBaseRotation,
                        FootRotationAngle / footRotationAngle
                    );
                }

                LegStates[leg].FootBaseRotation = Quaternion.FromToRotation(
                    LegStates[leg].FootBaseRotation * Vector3.up,
                    footBaseRotationFromSteps * Vector3.up
                ) * LegStates[leg].FootBaseRotation;

                // Elevate feet according to flight pas from keyframed animation
                LegStates[leg].FootBase += flightPos.y * m_legsUp * Scale;
                // Offset feet sideways according to flight pas from keyframed animation
                Vector3 stepRight = Vector3.Cross(m_legsUp, pointTo - pointFrom).normalized;
                LegStates[leg].FootBase += flightPos.x * stepRight * Scale;
                // Smooth lift that elevates feet in the air based on height of feet on the ground.
                Vector3 footBaseElevated = Vector3.Lerp(
                    LegStates[leg].FootBase,
                    Exts.SetHeight(LegStates[leg].FootBase, groundBasePoint, m_legsUp),
                    flightTimeLift
                );

                if (Vector3.Dot(footBaseElevated, m_legsUp) > Vector3.Dot(LegStates[leg].FootBase, m_legsUp))
                    LegStates[leg].FootBase = footBaseElevated;
            }

            BlendLegsMotionSystem();
            AdjustHipAndLegs(bodyRotation, legsRotation);
        }

        private void BlendLegsMotionSystem()
        {
            // Blend motion system effect in and out according to its weight
            for (int leg = 0; leg < m_legs.Length; leg++)
            {
                Vector3 footBaseReference = (
                    -Exts.GetHeelOffset(
                        m_legs[leg].Ankle, m_legs[leg].AnkleHeelVector,
                        m_legs[leg].Toe, m_legs[leg].ToeToetipVector,
                        LegStates[leg].HeelToetipVector,
                        LegStates[leg].FootBaseRotation
                    )
                    + m_legs[leg].Ankle.TransformPoint(m_legs[leg].AnkleHeelVector)
                );

                if (SystemWeight < Float.One)
                {
                    LegStates[leg].FootBase = Vector3.Lerp(
                        footBaseReference,
                        LegStates[leg].FootBase,
                        SystemWeight
                    );
                    LegStates[leg].FootBaseRotation = Quaternion.Slerp(
                        m_rotation,
                        LegStates[leg].FootBaseRotation,
                        SystemWeight
                    );
                }

                LegStates[leg].FootBase = Vector3.MoveTowards(
                    footBaseReference,
                    LegStates[leg].FootBase,
                    IKAdjustmentDistance
                );
            }
        }

        private void AdjustHipAndLegs(Quaternion bodyRotation, Quaternion legsRotation)
        {
            // Apply body rotation
            m_controller.RootBone.rotation = (
                m_alig.Rotation * Quaternion.Inverse(m_controller.Transform.rotation)
                * bodyRotation
                * m_controller.RootBone.rotation);
            for (int leg = 0; leg < m_legs.Length; leg++)
            {
                m_legs[leg].Hip.rotation = legsRotation * Quaternion.Inverse(bodyRotation) * m_legs[leg].Hip.rotation;
            }

            // Apply root offset based on body rotation
            Vector3 rootPoint = m_controller.RootBone.position;
            Vector3 hipAverage = m_controller.Transform.TransformPoint(m_controller.HipAverage);
            Vector3 hipAverageGround = m_controller.Transform.TransformPoint(m_controller.HipAverageGround);
            Vector3 rootPointAdjusted = rootPoint;
            rootPointAdjusted += bodyRotation * (rootPoint - hipAverage) - (rootPoint - hipAverage);
            rootPointAdjusted += legsRotation * (hipAverage - hipAverageGround) - (hipAverage - hipAverageGround);
            m_controller.RootBone.position = rootPointAdjusted + m_position - m_controller.Transform.position;

            for (int leg = 0; leg < m_legs.Length; leg++)
            {
                LegStates[leg].HipReference = m_legs[leg].Hip.position;
                LegStates[leg].AnkleReference = m_legs[leg].Ankle.position;
            }

            // Adjust legs in two passes
            // First pass is to find approximate place of hips and ankles
            // Second pass is to adjust ankles based on local angles found in first pass
            for (int pass = Int.One; pass <= Int.Two; pass++)
            {
                // Find the ankle position for each leg
                for (int leg = Int.Zero; leg < m_legs.Length; leg++)
                {
                    LegStates[leg].Ankle = Exts.GetAnklePosition(
                        m_legs[leg].Ankle, m_legs[leg].AnkleHeelVector,
                        m_legs[leg].Toe, m_legs[leg].ToeToetipVector,
                        LegStates[leg].HeelToetipVector,
                        LegStates[leg].FootBase, LegStates[leg].FootBaseRotation
                    );
                }

                // Find and apply the hip offset
                AdjustHipOffset();

                // Adjust the legs according to the found ankle and hip positions
                for (int leg = 0; leg < m_legs.Length; leg++)
                    AdjustLeg(leg, LegStates[leg].Ankle, pass == Int.Two);
            }
        }

        private void MonitorFootsteps()
        {
            for (int i = 0; i < LegStates.Length; i++)
            {
                var state = LegStates[i];
                
                switch (state.Phase)
                {
                    case LegCyclePhase.Stance:
                        if (state.CycleTime >= state.LiftTime && state.CycleTime < state.LandTime)
                        {
                            state.Phase = LegCyclePhase.Lift;
                        }
                        break;
                    case LegCyclePhase.Lift:
                        if (state.CycleTime >= state.LiftoffTime || state.CycleTime < state.LiftTime)
                        {
                            state.Phase = LegCyclePhase.Flight;
                        }
                        break;
                    case LegCyclePhase.Flight:
                        if (state.CycleTime >= state.StrikeTime || state.CycleTime < state.LiftoffTime)
                        {
                            state.Phase = LegCyclePhase.Land;
                            OnFootStrike?.Invoke(m_legs[i].Ankle.position);
                        }
                        break;
                    case LegCyclePhase.Land:
                        if (state.CycleTime >= state.LandTime || state.CycleTime < state.StrikeTime)
                        {
                            state.Phase = LegCyclePhase.Stance;
                        }
                        break;
                }
            }
        }

        public void AdjustLeg(int leg, Vector3 desiredAnklePosition, bool secondPass)
        {
            var legInfo = m_legs[leg];
            var legState = LegStates[leg];

            // Store original foot alignment
            Quaternion qAnkleOrigRotation;
            if (!secondPass)
            {
                // Footbase rotation in character space
                Quaternion objectToFootBaseRotation = LegStates[leg].FootBaseRotation * Quaternion.Inverse(m_rotation);
                qAnkleOrigRotation = objectToFootBaseRotation * legInfo.Ankle.rotation;
            }
            else
            {
                qAnkleOrigRotation = legInfo.Ankle.rotation;
            }

            // Solve the inverse kinematics
            if (legInfo.LegChain.Length == Int.Three)
            {
                // Solve the inverse kinematics
                m_ik.Solve(legInfo.LegChain, desiredAnklePosition);
            }

            // Calculate the desired new joint positions
            Vector3 pHip = legInfo.Hip.position;
            Vector3 pAnkle = legInfo.Ankle.position;

            if (!secondPass)
            {
                // Find alignment that is only rotates in horizontal plane
                // and keeps local ankle angle
                Quaternion horizontalRotation = Quaternion.FromToRotation(
                    m_forward,
                    Exts.ProjectOntoPlane(LegStates[leg].FootBaseRotation * Vector3.forward, m_up)
                ) * legInfo.Ankle.rotation;

                // Apply original foot alignment when foot is grounded
                legInfo.Ankle.rotation = Quaternion.Slerp(
                    horizontalRotation, // only horizontal rotation (keep local angle)
                    qAnkleOrigRotation, // rotates to slope of ground
                    Float.One - legState.GetFootGrounding(legState.CycleTime)
                );
            }
            else
            {
                // Rotate leg around hip-ankle axis by half amount of what the foot is rotated
                Vector3 hipAnkleVector = pAnkle - pHip;
                Quaternion legAxisRotate = Quaternion.Slerp(
                    Quaternion.identity,
                    Quaternion.FromToRotation(
                        Exts.ProjectOntoPlane(m_forward, hipAnkleVector),
                        Exts.ProjectOntoPlane(LegStates[leg].FootBaseRotation * Vector3.forward, hipAnkleVector)
                    ),
                    Float.Half
                );
                legInfo.Hip.rotation = legAxisRotate * legInfo.Hip.rotation;
                // Apply foot alignment found in first pass
                legInfo.Ankle.rotation = qAnkleOrigRotation;
            }
        }

        #endregion

        #region Util

        public float GetSpeed()
        {
            return m_speed;
        }

        public float GetMotionWeight(int index)
        {
            return m_motionWeights[index];
        }

        public float GetCycleWeight(int index)
        {
            return m_cycleWeights[index];
        }

        private Matrix4x4 FindGroundedBase(Vector3 pos, Quaternion rot, Vector3 heelToetip, bool avoidLedges)
        {
            RaycastHit hit;
            // Trace rays
            Vector3 hitAPoint = new Vector3();
            Vector3 hitBPoint = new Vector3();
            Vector3 hitANormal = new Vector3();
            Vector3 hitBNormal = new Vector3();
            bool hitA = false;
            bool hitB = false;
            bool valid = false;

            if (Physics.Raycast(pos + m_up * MaxStepHeight, -m_up, out hit, MaxStepHeight * Float.Two, GroundLayers))
            {
                valid = true;
                hitAPoint = hit.point;
                // Ignore surface normal if it deviates too much
                if (Vector3.Angle(hit.normal, m_up) < MaxSlopeAngle)
                    hitANormal = hit.normal; 
                
                hitA = true;
            }

            var heelToToetip = rot * heelToetip;
            float footLength = heelToToetip.magnitude;

            if (Physics.Raycast(pos + m_up * MaxStepHeight + heelToToetip, -m_up, out hit, MaxStepHeight * Float.Two, GroundLayers))
            {
                valid = true;
                hitBPoint = hit.point;
                // Ignore surface normal if it deviates too much
                if (Vector3.Angle(hit.normal, m_up) < MaxSlopeAngle)
                    hitBNormal = hit.normal; 
                
                hitB = true;
            }

            if (!valid)
            {
                Matrix4x4 m = Matrix4x4.identity;
                m.SetTRS(pos, rot, Vector3.one);
                return m;
            }

            // Choose which raycast result to use
            bool exclusive = false;
            if (avoidLedges)
            {
                if (!hitA && !hitB) 
                    hitA = true;
                else if (hitA && hitB)
                {
                    Vector3 avgNormal = (hitANormal + hitBNormal).normalized;
                    float hA = Vector3.Dot(hitAPoint, avgNormal);
                    float hB = Vector3.Dot(hitBPoint, avgNormal);
                    if (hA >= hB) 
                        hitB = false;
                    else 
                        hitA = false;
                    if (Mathf.Abs(hA - hB) > footLength / Float.Four) 
                        exclusive = true;
                }
                else 
                    exclusive = true;
            }

            Vector3 newStepPosition;
            Vector3 stepUp = rot * Vector3.up;

            // Apply result of raycast
            if (hitA)
            {
                if (hitANormal != Vector3.zero)
                    rot = Quaternion.FromToRotation(stepUp, hitANormal) * rot;

                newStepPosition = hitAPoint;
                if (exclusive)
                {
                    heelToToetip = rot * heelToetip;
                    newStepPosition -= heelToToetip * 0.5f;
                }
            }
            else
            {
                if (hitBNormal != Vector3.zero)
                    rot = Quaternion.FromToRotation(stepUp, hitBNormal) * rot;

                heelToToetip = rot * heelToetip;
                newStepPosition = hitBPoint - heelToToetip;
                if (exclusive) 
                    newStepPosition += heelToToetip * Float.Half; 
            }

            return Exts.MatrixFromQuaternionPosition(rot, newStepPosition);
        }

        private void AdjustHipOffset()
        {
            float lowestDesiredHeight = Mathf.Infinity;
            float lowestMaxHeight = Mathf.Infinity;
            float averageDesiredHeight = 0;

            for (int i = 0; i < m_legs.Length; i++)
            {
                m_lineIntersections[Int.Zero] = Float.Zero;
                m_lineIntersections[Int.One] = Float.Zero;
                var leg = m_legs[i];
                var stt = LegStates[i];
                // Calculate desired distance between original foot base position and original hip position
                Vector3 desiredVector = (stt.AnkleReference - stt.HipReference);
                float desiredDistance = desiredVector.magnitude;
                float desiredDistanceGround = Exts.ProjectOntoPlane(desiredVector, m_legsUp).magnitude;

                // Move closer if too far away
                Vector3 ankleVectorGround = Exts.ProjectOntoPlane(
                    stt.Ankle - leg.Hip.position, m_legsUp
                );
                float excess = ankleVectorGround.magnitude - desiredDistanceGround;
                if (excess > Float.Zero)
                {
                    float bufferDistance = (leg.LegLength * Scale * m_scaleFactor) - desiredDistanceGround;
                    stt.Ankle = (
                        stt.Ankle - ankleVectorGround
                        + ankleVectorGround.normalized
                        * (
                            desiredDistanceGround
                            + (Float.One - (Float.One / (excess / bufferDistance + Float.One))) * bufferDistance
                        )
                    );
                }

                // Find the desired hip height (relative to the current hip height)
                // such that the original distance between ankle and hip is preserved.
                // (Move line start and sphere center by minus line start to avoid precision errors)
                var ok = Exts.GetLineSphereIntersections(m_lineIntersections,
                    Vector3.zero, m_legsUp,
                    stt.Ankle - leg.Hip.position,
                    desiredDistance
                );
                float hipDesiredHeight;
                if (ok)
                    hipDesiredHeight = m_lineIntersections[Int.One];
                else
                    hipDesiredHeight = Vector3.Dot(stt.FootBase - leg.Hip.position, m_legsUp);

                // Find the maximum hip height (relative to the current hip height) such that the
                // distance between the ankle and hip is no longer than the length of the leg bones combined.
                // (Move line start and sphere center by minus line start to avoid precision errors)
                Exts.GetLineSphereIntersections(m_lineIntersections,
                    Vector3.zero, m_legsUp,
                    stt.Ankle - leg.Hip.position,
                    (leg.LegLength * Scale * m_scaleFactor)
                );

                float hipMaxHeight;
                if (ok) 
                    hipMaxHeight = m_lineIntersections[Int.One];
                else
                {
                    hipMaxHeight = Vector3.Dot(stt.Ankle - leg.Hip.position, m_legsUp);
                    Debug.LogWarning(m_controller.gameObject.name
                        + ": Line-sphere intersection failed for leg " + leg + ", hipMaxHeight."
                    );
                }

                // Find the lowest (and average) heights
                if (hipDesiredHeight < lowestDesiredHeight) 
                    lowestDesiredHeight = hipDesiredHeight;

                if (hipMaxHeight < lowestMaxHeight) 
                    lowestMaxHeight = hipMaxHeight;

                averageDesiredHeight += hipDesiredHeight / m_legs.Length;
            }

            // Find offset that is in between lowest desired, average desired, and lowest max
            if (lowestDesiredHeight > lowestMaxHeight) 
                lowestDesiredHeight = lowestMaxHeight;

            float minToAvg = averageDesiredHeight - lowestDesiredHeight;
            float minToMax = lowestMaxHeight - lowestDesiredHeight;

            float hipHeight = lowestDesiredHeight;

            // make sure we don't divide by zero
            if (minToAvg + minToMax > Float.Zero)
                hipHeight += minToAvg * minToMax / (minToAvg + minToMax);

            // Translate the root by this offset           
            m_controller.RootBone.position += hipHeight * m_legsUp;
        }

        #endregion
    }
}