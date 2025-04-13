using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using System.Collections;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Header("Stamina System")]
		[Tooltip("Maximum stamina value")]
		public float MaxStamina = 10f;
		[Tooltip("Stamina consumption per second while sprinting")]
		public float StaminaConsumption = 2f;
		[Tooltip("Stamina regeneration per second when not sprinting")]
		public float StaminaRegen = 1f;
		[Tooltip("Delay before stamina starts regenerating after sprinting")]
		public float StaminaRegenDelay = 2f;

		public float _currentStamina;
		public float _staminaRegenTimer;

		[Header("Crouch")]
		[Tooltip("Height of the character when crouching")]
		public float CrouchHeight = 0.9f;
		[Tooltip("Speed multiplier while crouching")]
		public float CrouchSpeed = 2.0f;
		[Tooltip("How fast the character crouches/stands up")]
		public float CrouchTransitionSpeed = 5.0f;

		private float _originalHeight;
		private bool _isCrouching;

		[Header("Head Bob")]
		[Tooltip("Amount of head bob when walking")]
		public float WalkBobAmount = 0.05f;
		[Tooltip("Amount of head bob when sprinting")]
		public float SprintBobAmount = 0.1f;
		[Tooltip("Speed of head bob when walking")]
		public float WalkBobSpeed = 10f;
		[Tooltip("Speed of head bob when sprinting")]
		public float SprintBobSpeed = 14f;

		private float _defaultCameraYPosition;
		private float _headBobTimer;
		private float _currentBobAmount;
		private float _currentBobSpeed;

		[Header("Jump Cooldown")]
		[Tooltip("Cooldown time between jumps in seconds")]
		public float JumpCooldown = 0.5f;
		private float _jumpCooldownDelta;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		[Header("Crouch Obstacle Detection")]
		[Tooltip("Distance to check for obstacles above the player")]
		public float CeilingCheckDistance = 0.5f;
		[Tooltip("Layer mask for obstacle detection")]
		public LayerMask ObstacleLayers;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;


#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
			}
		}

		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			_currentStamina = MaxStamina; // Start with full stamina
			_staminaRegenTimer = 0f;
			_jumpCooldownDelta = 0f;
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

			// Store original height
			_originalHeight = _controller.height;

			if (CinemachineCameraTarget != null)
			{
				_defaultCameraYPosition = CinemachineCameraTarget.transform.localPosition.y;
			}

			_currentBobAmount = WalkBobAmount;
			_currentBobSpeed = WalkBobSpeed;

		}

		private void Update()
		{
			HandleStamina(); // Add this before other methods
			JumpAndGravity();
			GroundedCheck();
			HandleCrouch();
			Move();
			HandleHeadBob();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}
		private void HandleStamina()
		{
			// Consume stamina when sprinting
			if (_input.sprint && _input.move != Vector2.zero && !_isCrouching)
			{
				_currentStamina -= StaminaConsumption * Time.deltaTime;
				_staminaRegenTimer = StaminaRegenDelay; // Reset regen delay

				// Clamp stamina to minimum 0
				_currentStamina = Mathf.Max(_currentStamina, 0f);

				// Stop sprinting if out of stamina
				if (_currentStamina <= 0f)
				{
					_input.sprint = false;
				}
			}
			// Regenerate stamina when not sprinting
			else if (_currentStamina < MaxStamina)
			{
				_staminaRegenTimer -= Time.deltaTime;

				// Only start regenerating after the delay
				if (_staminaRegenTimer <= 0f)
				{
					_currentStamina += StaminaRegen * Time.deltaTime;
					_currentStamina = Mathf.Min(_currentStamina, MaxStamina);
				}
			}
		}

		private void HandleHeadBob()
		{
			if (CinemachineCameraTarget == null) return;

			// Set bob parameters based on movement state
			if (_input.sprint && _input.move != Vector2.zero)
			{
				_currentBobAmount = SprintBobAmount;
				_currentBobSpeed = SprintBobSpeed;
			}
			else if (_input.move != Vector2.zero)
			{
				_currentBobAmount = WalkBobAmount;
				_currentBobSpeed = WalkBobSpeed;
			}

			// Apply head bob when moving and grounded
			if (Grounded && _input.move != Vector2.zero)
			{
				_headBobTimer += Time.deltaTime * _currentBobSpeed;
				float bobOffset = Mathf.Sin(_headBobTimer) * _currentBobAmount;

				Vector3 cameraPos = CinemachineCameraTarget.transform.localPosition;
				cameraPos.y = _defaultCameraYPosition + bobOffset;
				CinemachineCameraTarget.transform.localPosition = cameraPos;
			}
			else
			{
				// Reset camera position when not moving
				Vector3 cameraPos = CinemachineCameraTarget.transform.localPosition;
				cameraPos.y = _defaultCameraYPosition;
				CinemachineCameraTarget.transform.localPosition = cameraPos;
				_headBobTimer = 0; // Reset timer
			}
		}

		private void HandleCrouch()
		{
			// Check for crouch input (CTRL key)
			bool wantsToCrouch = Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;

			// If player wants to stand up but there's an obstacle, force crouch
			if (_isCrouching && !wantsToCrouch && HasObstacleAbove())
			{
				// Player can't stand up because there's an obstacle
				return;
			}

			// If crouch state changed
			if (wantsToCrouch != _isCrouching)
			{
				_isCrouching = wantsToCrouch;

				// Adjust height based on crouch state
				float targetHeight = _isCrouching ? CrouchHeight : _originalHeight;

				// Smoothly transition to target height
				StartCoroutine(SmoothCrouchTransition(targetHeight));
			}
		}

		private bool HasObstacleAbove()
		{
			// Calculate the top of the character controller
			float currentHeight = _controller.height;
			Vector3 rayStart = transform.position + _controller.center + Vector3.up * (currentHeight / 2 - _controller.radius);

			// Cast a sphere to check for obstacles
			float checkRadius = _controller.radius * 0.9f; // Slightly smaller than controller radius
			return Physics.SphereCast(rayStart, checkRadius, Vector3.up, out _, CeilingCheckDistance, ObstacleLayers);
		}

		private IEnumerator SmoothCrouchTransition(float targetHeight)
		{
			float initialHeight = _controller.height;
			float t = 0f;

			while (t < 1f)
			{
				// If transitioning to standing but obstacle appears, abort and return to crouch
				if (!_isCrouching && HasObstacleAbove())
				{
					// Immediately return to crouch
					_isCrouching = true;
					targetHeight = CrouchHeight;
					initialHeight = _controller.height;
					t = 0f;
				}

				t += Time.deltaTime * CrouchTransitionSpeed;
				_controller.height = Mathf.Lerp(initialHeight, targetHeight, t);
				yield return null;
			}
		}

		private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}



		private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = (_input.sprint && _currentStamina > 0 && !_isCrouching) ? SprintSpeed : MoveSpeed;
			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon
			// Reduce speed if crouching
			if (_isCrouching)
			{
				targetSpeed = CrouchSpeed;
			}

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			if (_controller.enabled)
			{
				_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
			}

		}

		private void JumpAndGravity()
		{
			// Update cooldown timer
			if (_jumpCooldownDelta > 0)
			{
				_jumpCooldownDelta -= Time.deltaTime;
			}

			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump - now checks cooldown as well
				if (_input.jump && _jumpTimeoutDelta <= 0.0f && _jumpCooldownDelta <= 0)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

					// Reset cooldown
					_jumpCooldownDelta = JumpCooldown;
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}