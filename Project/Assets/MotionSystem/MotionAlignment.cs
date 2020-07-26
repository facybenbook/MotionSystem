using System;
using UnityEngine;

namespace MotionSystem
{
    [Serializable]
    public class MotionAlignment
    {
		public UpdateType UpdateMethod = UpdateType.LateUpdate;
		public Vector3 Position { get { return m_position; } }
		public Quaternion Rotation { get { return m_rotation; } }
		public Vector3 Velocity { get { return m_velocity; } }
		public Vector3 Acceleration { get { return m_acceleration; } }
		public Vector3 AngularVelocity { get { return m_angularVelocity; } }
		public Vector3 VelocitySmoothed { get { return m_velocitySmoothed; } }
		public Vector3 AccelerationSmoothed { get { return m_accelerationSmoothed; } }
		public Vector3 AngularVelocitySmoothed { get { return m_angularVelocitySmoothed; } }

		private Rigidbody m_rigidbody;
		private Transform m_transform;
		private float m_currentLateTime;
		private float m_currentFixedTime;
		private MotionController m_controller;
		private Vector3 m_position = Vector3.zero;
		private Vector3 m_positionPrev = Vector3.zero;
		private Vector3 m_velocity = Vector3.zero;
		private Vector3 m_velocityPrev = Vector3.zero;
		private Vector3 m_velocitySmoothed = Vector3.zero;
		private Vector3 m_acceleration = Vector3.zero;
		private Vector3 m_accelerationSmoothed = Vector3.zero;
		private Quaternion m_rotation = Quaternion.identity;
		private Quaternion m_rotationPrev = Quaternion.identity;	
		private Vector3 m_angularVelocity = Vector3.zero;		
		private Vector3 m_angularVelocitySmoothed = Vector3.zero;

		public void Setup(MotionController controller)
        {
			m_controller = controller;

			if (m_rigidbody == null)
				m_rigidbody = m_controller.GetComponent<Rigidbody>();
			if (m_transform == null)
				m_transform = m_controller.Transform;
		}

		public void Reset()
		{
			m_currentLateTime = -Float.One;
			m_currentFixedTime = -Float.One;
			m_position = m_positionPrev = m_transform.position;
			m_rotation = m_rotationPrev = m_transform.rotation;
			m_velocity = Vector3.zero;
			m_velocityPrev = Vector3.zero;
			m_velocitySmoothed = Vector3.zero;
			m_acceleration = Vector3.zero;
			m_accelerationSmoothed = Vector3.zero;
			m_angularVelocity = Vector3.zero;
			m_angularVelocitySmoothed = Vector3.zero;
		}

		private Vector3 CalculateAngularVelocity(Quaternion prev, Quaternion current)
		{
			var deltaRotation = Quaternion.Inverse(prev) * current;
			var angle = Float.Zero;
			var axis = Vector3.zero;
			deltaRotation.ToAngleAxis(out angle, out axis);

			if (axis == Vector3.zero || axis.x == Mathf.Infinity || axis.x == Mathf.NegativeInfinity)
				return Vector3.zero;

			if (angle > Float._180Deg) 
				angle -= Float._360Deg;

			angle /= Time.deltaTime;
			return axis.normalized * angle;
		}

		private void UpdateTracking()
		{
			m_position = m_transform.position;
			m_rotation = m_transform.rotation;

			if (m_rigidbody != null)
			{
				// Rigidbody velocity is not reliable, so we calculate our own
				m_velocity = (m_position - m_positionPrev) / Time.deltaTime;

				// Rigidbody angularVelocity is not reliable, so we calculate out own
				m_angularVelocity = CalculateAngularVelocity(m_rotationPrev, m_rotation);
			}
			else
			{
				m_velocity = (m_position - m_positionPrev) / Time.deltaTime;
				m_angularVelocity = CalculateAngularVelocity(m_rotationPrev, m_rotation);
			}

			m_acceleration = (m_velocity - m_velocityPrev) / Time.deltaTime;
			m_positionPrev = m_position;
			m_rotationPrev = m_rotation;
			m_velocityPrev = m_velocity;
		}

		public void FixedUpdate()
		{
			if (Time.deltaTime == Float.Zero || Time.timeScale == Float.Zero) 
				return;
			if (m_currentFixedTime == Time.time) 
				return;

			m_currentFixedTime = Time.time;

			if (UpdateMethod == UpdateType.FixedUpdate) 
				UpdateTracking();
		}

		public void LateUpdate()
		{
			if (Time.deltaTime == Float.Zero || Time.timeScale == Float.Zero) 
				return;
			if (m_currentLateTime == Time.time) 
				return;

			m_currentLateTime = Time.time;

			if (UpdateMethod == UpdateType.LateUpdate)
				UpdateTracking();

			m_velocitySmoothed = Vector3.Lerp(
				m_velocitySmoothed, m_velocity, Time.deltaTime * Float.Ten
			);

			m_accelerationSmoothed = Vector3.Lerp(
				m_accelerationSmoothed, m_acceleration, Time.deltaTime * Float.Three
			);

			m_angularVelocitySmoothed = Vector3.Lerp(
				m_angularVelocitySmoothed, m_angularVelocity, Time.deltaTime * Float.Three
			);

			if (UpdateMethod == UpdateType.FixedUpdate)
				m_position += m_velocity * Time.deltaTime;
		}
	}
}