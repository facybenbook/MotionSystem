using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class DemoController : MonoBehaviour
{
	public string VerticalParam = "vertical";
	public string HorizontalParam = "horizontal";
	[Range(0f, 180f)]
	public float Gravity = 10.0f;
	public float MovingTurnSpeed = 360;
	public float StationaryTurnSpeed = 180;
	public float MoveSpeedMultiplier = 1f;
	public float GroundCheckDistance = 0.1f;
	public LayerMask GroundLayer;

	private Animator m_Animator;
	//private bool m_IsGrounded;
	private float m_TurnAmount;
	private float m_ForwardAmount;
	private Vector3 m_GroundNormal;
	private Transform m_camera;
	private Transform m_transform;
	private CharacterController m_charController;
    private Vector3 m_camForward;             // The current forward direction of the camera
    private Vector3 m_move;
    private const string m_vertical = "Vertical";
	private const string m_horizontal = "Horizontal";

    // Use this for initialization
    private void Start()
	{
		m_camera = Camera.main.transform;
		m_Animator = GetComponent<Animator>();
		m_transform = GetComponent<Transform>();
		m_charController = GetComponent<CharacterController>();
	}

	public void OnAnimatorMove()
	{
		// we implement this function to override the default root motion.
		// this allows us to modify the positional speed before it's applied.
		if (Time.deltaTime > 0)
		{
			Vector3 v = (m_Animator.deltaPosition * MoveSpeedMultiplier) / Time.deltaTime;
			v += m_transform.up * -Gravity;
			// Apply movement
			CollisionFlags flags = m_charController.Move(v * Time.deltaTime);
			//m_IsGrounded = (flags & CollisionFlags.CollidedBelow) != 0;
		}
	}

	private void FixedUpdate()
    {
        // read inputs
        float h = Input.GetAxis(m_horizontal);
        float v = Input.GetAxis(m_vertical);
        bool crouch = Input.GetKey(KeyCode.C);

        // calculate move direction to pass to character
        if (m_camera != null)
        {
            // calculate camera relative direction to move:
            m_camForward = Vector3.Scale(m_camera.forward, new Vector3(1f, 0f, 1f)).normalized;
            m_move = v * m_camForward + h * m_camera.right;
        }
        else
        {
            // we use world-relative directions in the case of no main camera
            m_move = v * Vector3.forward + h * Vector3.right;
        }
#if !MOBILE_INPUT
        // walk speed multiplier
        if (!Input.GetKey(KeyCode.LeftShift)) 
			m_move *= 0.5f;
#endif

        // pass all parameters to the character control script
        Move(m_move);
    }

	public void Move(Vector3 move)
	{
		// convert the world relative moveInput vector into a local-relative
		// turn amount and forward amount required to head in the desired
		// direction.
		if (move.magnitude > 1f) 
			move.Normalize();

		move = m_transform.InverseTransformDirection(move);
		CheckGroundStatus();
		move = Vector3.ProjectOnPlane(move, m_GroundNormal);
		m_TurnAmount = Mathf.Atan2(move.x, move.z);
		m_ForwardAmount = move.z;

		ApplyExtraTurnRotation();
		// send input and other state parameters to the animator
		UpdateAnimator(move);
	}

	void UpdateAnimator(Vector3 move)
	{
		// update the animator parameters
		m_Animator.SetFloat(VerticalParam, m_ForwardAmount, 0.1f, Time.deltaTime);
		m_Animator.SetFloat(HorizontalParam, m_TurnAmount, 0.1f, Time.deltaTime);
	}

	void ApplyExtraTurnRotation()
	{
		// help the character turn faster (this is in addition to root rotation in the animation)
		float turnSpeed = Mathf.Lerp(StationaryTurnSpeed, MovingTurnSpeed, m_ForwardAmount);
		m_transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
	}

	void CheckGroundStatus()
	{
		RaycastHit hitInfo;
#if UNITY_EDITOR
		// helper to visualise the ground check ray in the scene view
		Debug.DrawLine(m_transform.position + (Vector3.up * 0.1f), m_transform.position + (Vector3.up * 0.1f) + (Vector3.down * GroundCheckDistance));
#endif
		// 0.1f is a small offset to start the ray from inside the character
		// it is also good to note that the transform position in the sample assets is at the base of the character
		if (Physics.Raycast(m_transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, GroundCheckDistance))
		{
			m_GroundNormal = hitInfo.normal;
			m_Animator.applyRootMotion = true;
		}
		else
		{
			m_GroundNormal = Vector3.up;
			m_Animator.applyRootMotion = false;
		}
	}
}