namespace MotionSystem
{
	public enum YesNo
	{
		Yes,
		No,
	}

	public enum BlendStatesType
	{
		CurrentStateOnly,
		CurrentAndNextState,
	}

	public enum MotionType
	{
		Moving,
		Stationary,
	}

	public enum NormalizedTime
	{
		Calculated,
		AnimationState,
	}

	public enum UpdateType
    {
		LateUpdate,
		FixedUpdate
    }

	public enum LegCyclePhase
	{
		Stance, 
		Lift, 
		Flight, 
		Land
	}

	public enum IgnoreRootMotionOnBone
	{
		None = 0,
		Root = 1,
		Pelvis = 2
	}
}