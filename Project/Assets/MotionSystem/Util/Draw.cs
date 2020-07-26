using UnityEngine;

namespace MotionSystem
{
	public class DrawArea
	{
		public Vector3 min;
		public Vector3 max;
		public Vector3 canvasMin = new Vector3(0, 0, 0);
		public Vector3 canvasMax = new Vector3(1, 1, 1);
		//public float drawDistance = 1;

		public DrawArea(Vector3 min, Vector3 max)
		{
			this.min = min;
			this.max = max;
		}

		public virtual Vector3 Point(Vector3 p)
		{
			return Camera.main.ScreenToWorldPoint(
				Vector3.Scale(
					new Vector3(
						(p.x - canvasMin.x) / (canvasMax.x - canvasMin.x),
						(p.y - canvasMin.y) / (canvasMax.y - canvasMin.y),
						0
					),
					max - min
				) + min
				+ Vector3.forward * Camera.main.nearClipPlane * 1.1f
			);
		}

		public void DrawLine(Vector3 a, Vector3 b, Color c)
		{
			GL.Color(c);
			GL.Vertex(Point(a));
			GL.Vertex(Point(b));
		}

		public void DrawRay(Vector3 start, Vector3 dir, Color c)
		{
			DrawLine(start, start + dir, c);
		}

		public void DrawRect(Vector3 a, Vector3 b, Color c)
		{
			GL.Color(c);
			GL.Vertex(Point(new Vector3(a.x, a.y, 0)));
			GL.Vertex(Point(new Vector3(a.x, b.y, 0)));
			GL.Vertex(Point(new Vector3(b.x, b.y, 0)));
			GL.Vertex(Point(new Vector3(b.x, a.y, 0)));
		}

		public void DrawDiamond(Vector3 a, Vector3 b, Color c)
		{
			GL.Color(c);
			GL.Vertex(Point(new Vector3(a.x, (a.y + b.y) / 2, 0)));
			GL.Vertex(Point(new Vector3((a.x + b.x) / 2, b.y, 0)));
			GL.Vertex(Point(new Vector3(b.x, (a.y + b.y) / 2, 0)));
			GL.Vertex(Point(new Vector3((a.x + b.x) / 2, a.y, 0)));
		}

		public void DrawRect(Vector3 corner, Vector3 dirA, Vector3 dirB, Color c)
		{
			GL.Color(c);
			Vector3[] dirs = new Vector3[] { dirA, dirB };
			for (int i = 0; i < 2; i++)
			{
				for (int dir = 0; dir < 2; dir++)
				{
					Vector3 start = corner + dirs[(dir + 1) % 2] * i;
					GL.Vertex(Point(start));
					GL.Vertex(Point(start + dirs[dir]));
				}
			}
		}

		public void DrawCube(Vector3 corner, Vector3 dirA, Vector3 dirB, Vector3 dirC, Color c)
		{
			GL.Color(c);
			Vector3[] dirs = new Vector3[] { dirA, dirB, dirC };
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					for (int dir = 0; dir < 3; dir++)
					{
						Vector3 start = corner + dirs[(dir + 1) % 3] * i + dirs[(dir + 2) % 3] * j;
						GL.Vertex(Point(start));
						GL.Vertex(Point(start + dirs[dir]));
					}
				}
			}
		}
	}

	public class DrawArea3D : DrawArea
	{
		public Matrix4x4 matrix;

		public DrawArea3D(Vector3 min, Vector3 max, Matrix4x4 matrix) : base(min, max)
		{
			this.matrix = matrix;
		}

		public override Vector3 Point(Vector3 p)
		{
			return matrix.MultiplyPoint3x4(
				Vector3.Scale(
					new Vector3(
						(p.x - canvasMin.x) / (canvasMax.x - canvasMin.x),
						(p.y - canvasMin.y) / (canvasMax.y - canvasMin.y),
						p.z
					),
					max - min
				) + min
			);
		}
	}
}