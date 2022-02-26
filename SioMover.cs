using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Other players movements
public class SioMover : MonoBehaviour
{
	public GameObject realLocation;
    public CharacterController _controller;
	public Animator _animator;
	
	public float fixLocationSteps;
	public float distanceToFixLocation;
	// player
	private Vector2 _inputMove;
	private float _speed;
	private float _animationBlend;
	private float _targetRotation = 0.0f;
	private float _rotationVelocity;
	private float _verticalVelocity;

	// animation IDs
	private int _animIDSpeed;
	private int _animIDGrounded;
	private int _animIDJump;
	private int _animIDFreeFall;
	private int _animIDMotionSpeed;

	public string currentAnimState;
	bool firstSend = true;

	void Start()
    {
		AssignAnimationIDs();
	}

    public void MoveTo(Vector2 inputMove, float speed, float verticalVelocity, Vector3 position, float targetRotation)
    {
		_inputMove = inputMove;
		_speed = speed;
		_targetRotation = targetRotation;
		_verticalVelocity = verticalVelocity;

		Vector3 currentLocation = gameObject.transform.position;
		float distance = Vector3.Distance(currentLocation, position);
		if (firstSend)
        {
			transform.position = position;
			firstSend = false;
		}
		if (distance > distanceToFixLocation)
		{
			float steps = fixLocationSteps * distance;// Time.deltaTime; // calculate distance to move
			transform.position = Vector3.MoveTowards(transform.position, position, steps);
		}
		realLocation.transform.position = position;	
    }
	private void AssignAnimationIDs()
	{
		_animIDSpeed = Animator.StringToHash("Speed");
		_animIDGrounded = Animator.StringToHash("Grounded");
		_animIDJump = Animator.StringToHash("Jump");
		_animIDFreeFall = Animator.StringToHash("FreeFall");
		_animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
		_animator.SetBool(_animIDJump, false);
		_animator.SetBool(_animIDFreeFall, false);
	}
	private void Move()
	{
		float targetSpeed = _speed;
		if (_inputMove == Vector2.zero) targetSpeed = 0.0f;
		
		float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

		float inputMagnitude = 1F;
		float SpeedChangeRate = 10f;
		
		_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

		float RotationSmoothTime = 0.12f;
		float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

		transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
		Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

		// move the player
		_controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

		_animator.SetFloat(_animIDSpeed, _animationBlend);
		_animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
		
	}

	private void JumpAndGravity()
	{	
			_animator.SetBool(_animIDJump, false);
			_animator.SetBool(_animIDFreeFall, false);
			
			if (_verticalVelocity > 0.0f)
			{
				_animator.SetBool(_animIDJump, true);
			}
				
	}

	private void Update()
    {
        JumpAndGravity();
        Move();
    }

	public void ChangeAnimationState(string newState)
	{
		if (currentAnimState == newState) return;
		
		_animator.Play(newState);
		currentAnimState = newState;
	}
}
