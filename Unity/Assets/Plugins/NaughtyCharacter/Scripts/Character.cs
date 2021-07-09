using UnityEngine;

namespace NaughtyCharacter
{
	[System.Serializable]
	public class MovementSettings
	{
		/// 加速度
		public float Acceleration = 25.0f; // In meters/second
		///减速度
		public float Decceleration = 25.0f; // In meters/second
		///最大水平速度
		public float MaxHorizontalSpeed = 8.0f; // In meters/second
		///跳跃速度
		public float JumpSpeed = 10.0f; // In meters/second
		///跳跃中止速度
		public float JumpAbortSpeed = 10.0f; // In meters/second
	}

	[System.Serializable]
	public class GravitySettings
	{
		///当玩家在空中时施加重力
		public float Gravity = 20.0f; // Gravity applied when the player is airborne
		///当玩家落地时所施加的恒定重力
		public float GroundedGravity = 5.0f; // A constant gravity that is applied when the player is grounded
		///玩家下落的最大速度
		public float MaxFallSpeed = 40.0f; // The max speed at which the player can fall
	}

	[System.Serializable]
	public class RotationSettings
	{
		[Header("Control Rotation")]
		//最小俯仰角
		public float MinPitchAngle = -45.0f;
		///最大俯仰角
		public float MaxPitchAngle = 75.0f;

		[Header("Character Orientation")]
		//是否使用镜头旋转
		[SerializeField] private bool _useControlRotation = false;
		///是否使用运动旋转
		[SerializeField] private bool _orientRotationToMovement = true;
		//玩家速度最大时的旋转速度
		public float MinRotationSpeed = 600.0f; // The turn speed when the player is at max speed (in degrees/second)
		//玩家禁止的旋转速度
		public float MaxRotationSpeed = 1200.0f; // The turn speed when the player is stationary (in degrees/second)

		public bool UseControlRotation { get { return _useControlRotation; } set { SetUseControlRotation(value); } }
		public bool OrientRotationToMovement { get { return _orientRotationToMovement; } set { SetOrientRotationToMovement(value); } }

		private void SetUseControlRotation(bool useControlRotation)
		{
			_useControlRotation = useControlRotation;
			_orientRotationToMovement = !_useControlRotation;
		}

		private void SetOrientRotationToMovement(bool orientRotationToMovement)
		{
			_orientRotationToMovement = orientRotationToMovement;
			_useControlRotation = !_orientRotationToMovement;
		}
	}

	public class Character : MonoBehaviour
	{
		//控制器
		public Controller Controller; // The controller that controls the character
		//设置
		public MovementSettings MovementSettings;
		public GravitySettings GravitySettings;
		public RotationSettings RotationSettings;

		//unity 角色控制器组件
		private CharacterController _characterController; // The Unity's CharacterController
		//动画控制器
		private CharacterAnimator _characterAnimator;

		private float _targetHorizontalSpeed; // In meters/second
		private float _horizontalSpeed; // In meters/second
		private float _verticalSpeed; // In meters/second

		private Vector2 _controlRotation; // X (Pitch), Y (Yaw)
		private Vector3 _movementInput;
		private Vector3 _lastMovementInput;
		private bool _hasMovementInput;
		private bool _jumpInput;

		public Vector3 Velocity => _characterController.velocity;
		public Vector3 HorizontalVelocity => _characterController.velocity.SetY(0.0f);
		public Vector3 VerticalVelocity => _characterController.velocity.Multiply(0.0f, 1.0f, 0.0f);
		
		/// 是否在地面上
		public bool IsGrounded { get; private set; } 

		private void Awake()
		{
			Controller.Init();
			Controller.Character = this;

			_characterController = GetComponent<CharacterController>();
			_characterAnimator = GetComponent<CharacterAnimator>();
		}

		private void Update()
		{
			Controller.OnCharacterUpdate();
		}

		private void FixedUpdate()
		{
			UpdateState();
			Controller.OnCharacterFixedUpdate();
		}

		/// <summary>
		/// 更新状态
		/// </summary>
		private void UpdateState()
		{
			UpdateHorizontalSpeed();
			UpdateVerticalSpeed();

			//运动
			Vector3 movement = _horizontalSpeed * GetMovementDirection() + _verticalSpeed * Vector3.up;
			_characterController.Move(movement * Time.deltaTime);

			//旋转
			OrientToTargetRotation(movement.SetY(0.0f));

			IsGrounded = _characterController.isGrounded;

			_characterAnimator.UpdateState();
		}

		
		public void SetMovementInput(Vector3 movementInput)
		{
			bool hasMovementInput = movementInput.sqrMagnitude > 0.0f;

			//如果上次有输入 本次无输入 记录上一次的输入
			if (_hasMovementInput && !hasMovementInput)
			{
				_lastMovementInput = _movementInput;
			}

			_movementInput = movementInput;
			_hasMovementInput = hasMovementInput;
		}

		public void SetJumpInput(bool jumpInput)
		{
			_jumpInput = jumpInput;
		}

		public Vector2 GetControlRotation()
		{
			return _controlRotation;
		}

		/// <summary>
		/// 设置旋转
		/// </summary>
		public void SetControlRotation(Vector2 controlRotation)
		{
			//俯仰角
			// Adjust the pitch angle (X Rotation)
			float pitchAngle = controlRotation.x;
			pitchAngle %= 360.0f;
			pitchAngle = Mathf.Clamp(pitchAngle, RotationSettings.MinPitchAngle, RotationSettings.MaxPitchAngle);

			//偏航角
			// Adjust the yaw angle (Y Rotation)
			float yawAngle = controlRotation.y;
			yawAngle %= 360.0f;

			_controlRotation = new Vector2(pitchAngle, yawAngle);
		}

		
		/// <summary>
		/// 刷新水平速度
		/// </summary>
		private void UpdateHorizontalSpeed()
		{
			Vector3 movementInput = _movementInput;
			if (movementInput.sqrMagnitude > 1.0f)
			{
				movementInput.Normalize();
			}

			_targetHorizontalSpeed = movementInput.magnitude * MovementSettings.MaxHorizontalSpeed;
			float acceleration = _hasMovementInput ? MovementSettings.Acceleration : MovementSettings.Decceleration;

			_horizontalSpeed = Mathf.MoveTowards(_horizontalSpeed, _targetHorizontalSpeed, acceleration * Time.deltaTime);
		}

		
		/// <summary>
		/// 刷新垂直速度
		/// </summary>
		private void UpdateVerticalSpeed()
		{
			if (IsGrounded)
			{
				_verticalSpeed = -GravitySettings.GroundedGravity;

				if (_jumpInput)
				{
					_verticalSpeed = MovementSettings.JumpSpeed;
					IsGrounded = false;
				}
			}
			else
			{
				//轻拍跳
				if (!_jumpInput && _verticalSpeed > 0.0f)
				{
					// This is what causes holding jump to jump higher than tapping jump.
					_verticalSpeed = Mathf.MoveTowards(_verticalSpeed, -GravitySettings.MaxFallSpeed, MovementSettings.JumpAbortSpeed * Time.deltaTime);
				}

				//持续跳
				_verticalSpeed = Mathf.MoveTowards(_verticalSpeed, -GravitySettings.MaxFallSpeed, GravitySettings.Gravity * Time.deltaTime);
			}
		}

		private Vector3 GetMovementDirection()
		{
			Vector3 moveDir = _hasMovementInput ? _movementInput : _lastMovementInput;
			if (moveDir.sqrMagnitude > 1f)
			{
				moveDir.Normalize();
			}

			return moveDir;
		}

		/// <summary>
		/// 面向目标旋转
		/// </summary>
		/// <param name="horizontalMovement"></param>
		private void OrientToTargetRotation(Vector3 horizontalMovement)
		{
			if (RotationSettings.OrientRotationToMovement && horizontalMovement.sqrMagnitude > 0.0f)
			{
				float rotationSpeed = Mathf.Lerp(
					RotationSettings.MaxRotationSpeed, RotationSettings.MinRotationSpeed, _horizontalSpeed / _targetHorizontalSpeed);

				Quaternion targetRotation = Quaternion.LookRotation(horizontalMovement, Vector3.up);

				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
			else if (RotationSettings.UseControlRotation)
			{
				Quaternion targetRotation = Quaternion.Euler(0.0f, _controlRotation.y, 0.0f);
				transform.rotation = targetRotation;
			}
		}
	}
}
