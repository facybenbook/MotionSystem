using UnityEngine;

namespace MotionSystem
{ 
	public class MotionDebugger : MonoBehaviour
	{
		[Range(0.1f, 1f)]
		public float TimeScale = 1f;
		//public bool DrawMotionStates = true;
		public bool DrawMotionGraph = true;
		public bool DrawStepsPrediction = true;
		public bool DrawLegLines = true;
		public Material VertexColorMaterial;

		private MotionController m_controller;
		private Color[] m_legColors;
		private Transform m_camera;

        private void Awake()
        {
			m_controller = GetComponent<MotionController>();
			m_camera = Camera.main.transform;
		}

        private void Start()
        {
			if (m_controller == null)
				return;

			m_legColors = new Color[m_controller.Legs.Length];
			for (int i = 0; i < m_legColors.Length; i++)
            {
				Vector3 colorVect = Quaternion.AngleAxis(i * Float._180Deg / m_legColors.Length, Vector3.one) * Vector3.right;
				m_legColors[i] = new Color(colorVect.x, colorVect.y, colorVect.z);
			}
		}

        /*void OnGUI()
		{
			if (!DrawMotionStates)
				return;
			if (m_controller == null)
				return;
			if (!Application.isPlaying)
				return;
			if (m_controller.LegsAnimator == null)
				return;

			for (int i = 0; i < m_controller.MotionAsset.MotionData.Length; i++)
			{
				var state = m_controller.MotionAsset.MotionData[i];
				var str = state.Name;
				var weight = m_controller.LegsAnimator.GetMotionWeight(i);
				float v = Float.One * weight;
				GUI.color = new Color(0, 0, v, 1);

				if (weight > Float.Zero)
					GUI.color = new Color(v, v, v, 1);

				str += " " + weight.ToString("0.000");
				GUI.Label(new Rect(Screen.width - 200, 10 + 20 * i, 200, 30), str);
			}
		}*/

		void OnRenderObject()
		{
			if (m_controller == null)
				return;
			if (!DrawMotionGraph && !DrawStepsPrediction)
				return;

			VertexColorMaterial.SetPass(0);
			if (DrawMotionGraph)
				RenderBlendingGraph(); 
			if (DrawStepsPrediction)
				RenderFootMarkers();
		}

        private void LateUpdate()
		{
			if (!DrawLegLines)
				return;
			if (m_controller == null)
				return;
			if (m_controller.LegsAnimator == null)
				return;

			for (int l = 0; l < m_controller.Legs.Length; l++)
            {
                // Draw desired bone alignment lines
				var leg = m_controller.Legs[l];

				for (int i = 0; i < leg.LegChain.Length - 1; i++)
                {
                    Debug.DrawLine(
						leg.LegChain[i].position,
						leg.LegChain[i + Int.One].position,
						m_legColors[i]
					);
                }
            }
			Time.timeScale = TimeScale;
		}

		private void RenderFootMarkers()
		{
			if (m_controller.LegsAnimator == null)
				return;

			
			GL.Begin(GL.LINES);

			GL.End();
			GL.Begin(GL.QUADS);
			Vector3 heel, forward, up, right;
			Matrix4x4 m;
			const int heelValue = 20;
			for (int i = 0; i < m_controller.Legs.Length; i++)
			{
				for (int step = 0; step < Int.Three; step++)
				{
					if (m_controller.LegsAnimator.LegStates[i] == null)
						continue;

					var leg = m_controller.Legs[i];
					var sts = m_controller.LegsAnimator.LegStates[i];
					if (step == Int.Zero)
					{
						m = sts.StepFromMatrix;
						GL.Color(m_legColors[i] * Float.Half);
					}
					else if (step == Int.One)
					{
						m = sts.StepToMatrix;
						GL.Color(m_legColors[i]);
					}
					else
					{
						m = sts.StepToMatrix;
						GL.Color(m_legColors[i] * Float.DotFour);
					}

					// Draw foot marker
					heel = m.MultiplyPoint3x4(Vector3.zero);
					forward = m.MultiplyVector(sts.HeelToetipVector);
					up = m.MultiplyVector(Vector3.up);
					right = (Quaternion.AngleAxis(Float._90Deg, up) * forward).normalized * leg.FootWidth * m_controller.LegsAnimator.Scale;
					heel += up.normalized * right.magnitude / heelValue;
					if (step == Int.Two)
						heel += sts.StepToPositionGoal - sts.StepToPosition;
					GL.Vertex(heel + right / Int.Two);
					GL.Vertex(heel - right / Int.Two);
					GL.Vertex(heel - right / Int.Four + forward);
					GL.Vertex(heel + right / Int.Four + forward);
				}
			}
			GL.End();
		}

		public void RenderBlendingGraph()
		{
			Matrix4x4 matrix = Exts.CreateMatrix(
				m_controller.Transform.right, m_controller.Transform.forward, transform.up,
				m_controller.Transform.TransformPoint(m_controller.HipAverage)
			);
			float size = (m_camera.position - m_controller.Transform.TransformPoint(m_controller.HipAverage)).magnitude / Float.Two;
			DrawArea graph = new DrawArea3D(new Vector3(-size, -size, 0), new Vector3(size, size, 0), matrix);

			GL.Begin(GL.QUADS);
			graph.DrawRect(new Vector3(0, 0, 0), new Vector3(1, 1, 0), new Color(0, 0, 0, 0.2f));
			GL.End();

			//Color strongColor = new Color(0, 0, 0, 1);
			Color weakColor = new Color(0.7f, 0.7f, 0.7f, 1);

			float range = 0;
			for (int i = 0; i < m_controller.MotionAsset.MotionData.Length; i++)
			{
				var m = m_controller.MotionAsset.MotionData[i];
				range = Mathf.Max(range, Mathf.Abs(m.CycleVelocity.x));
				range = Mathf.Max(range, Mathf.Abs(m.CycleVelocity.z));
			}
			if (range == 0)
				range = Float.One;
			else
				range *= 1.2f;

			GL.Begin(GL.LINES);
			graph.DrawLine(new Vector3(Float.Half, 0, 0), new Vector3(Float.Half, Float.One, 0), weakColor);
			graph.DrawLine(new Vector3(0, Float.Half, 0), new Vector3(Float.One, Float.Half, 0), weakColor);
			graph.DrawLine(new Vector3(0, 0, 0), new Vector3(Float.One, 0, 0), weakColor);
			graph.DrawLine(new Vector3(Float.One, 0, 0), new Vector3(1, Float.One, 0), weakColor);
			graph.DrawLine(new Vector3(Float.One, Float.One, 0), new Vector3(0, Float.One, 0), weakColor);
			graph.DrawLine(new Vector3(0, Float.One, 0), new Vector3(0, 0, 0), weakColor);
			GL.End();

			float mX, mY;
			Vector3 colorVect = Quaternion.AngleAxis(
				Float.Half * Float._180Deg / Float.One, Vector3.one
			) * Vector3.right;
			Color color = new Color(colorVect.x, colorVect.y, colorVect.z);

			// Draw weights
			GL.Begin(GL.QUADS);
			Color colorTemp = color * Float.Half;
			colorTemp.a = 0.9f;
			for (int i = 0; i < m_controller.MotionAsset.MotionData.Length; i++)
			{
				var m = m_controller.MotionAsset.MotionData[i];
				mX = (m.CycleVelocity.x) / range / Float.Two + Float.Half;
				mY = (m.CycleVelocity.z) / range / Float.Two + Float.Half;
				float s = 0.02f;
				graph.DrawDiamond(
					new Vector3(mX - s, mY - s, 0),
					new Vector3(mX + s, mY + s, 0),
					colorTemp
				);
			}
			GL.End();

			GL.Begin(GL.QUADS);
			// Draw marker
			mX = (m_controller.LegsAnimator.ObjectVelocity.x) / range / Float.Two + Float.Half;
			mY = (m_controller.LegsAnimator.ObjectVelocity.z) / range / Float.Two + Float.Half;
			float t = 0.02f;
			graph.DrawRect(new Vector3(mX - t, mY - t, 0), new Vector3(mX + t, mY + t, 0), new Color(0, 0, 0, 1));
			t /= Float.Two;
			graph.DrawRect(new Vector3(mX - t, mY - t, 0), new Vector3(mX + t, mY + t, 0), new Color(1, 1, 1, 1));
			GL.End();
		}
	}
}