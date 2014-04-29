/* Copyright (c) 2014 Advanced Platformer 2D */
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(APCharacterMotor))]
[AddComponentMenu("Advanced Platformer 2D/CharacterController")]

public class APCharacterController : MonoBehaviour
{
	////////////////////////////////////////////////////////
	// PUBLIC/HIGH LEVEL
	[System.Serializable]
	public class Inputs
	{
		public APInput m_axisX = new APInput("Horizontal");
		public APInput m_axisY = new APInput("Vertical");
		public string m_runButton;
	}

	[System.Serializable]
	public class Settings
	{
		public bool m_alwaysRun = true;					// run always enabled
		public float m_walkSpeed = 5f;					// in m/s
		public float m_runSpeed = 8f;					// in m/s
		public float m_acceleration = 30f;				// in m/s²
		public float m_deceleration = 20f;				// in m/s²
		public float m_frictionDynamic = 1f;			// friction when accelerating
		public float m_frictionStatic = 1f;				// friction when releasing input / moving in opposite direction
		public bool m_autoRotate = true;				// enable auto rotate
		public float m_gravity = 50f;					// in m/s²
		public float m_airPower = 10f;					// acceleration when in air
		public float m_maxAirSpeed = 8f;				// max horizontal speed in air
		public float m_maxFallSpeed = 20f;				// max fall speed
		public float m_crouchSizePercent = 0.5f;		// collision box reduce size percent when crouched
		public bool m_enableCrouchedAutoRotate = true;	// enable auto rotate while crouched

		// maxspeed factor in function of current ground slope angle (negative values for down slope, positive value for up)
		// NB : factor must be in [0,1] range
		public AnimationCurve m_slopeSpeedMultiplier = new AnimationCurve(new Keyframe(-90, 1), new Keyframe(0, 1), new Keyframe(45, 1), new Keyframe(60, 0));
	}

	[System.Serializable]
	public class Animations
	{
		public string m_stand = "Stand";
		public string m_run = "Run";
		public string m_crouch = "Crouch";
		public string m_inAir = "InAir";
		public string m_wallJump = "WallJump";
		public float m_minAirTime = 0.2f;			// min time in air before launching InAir animation
		public bool m_walkAnimFromInput = false;	// use input filtered value for animation, otherwise use ground speed

		// animation speed in function of input filtered value, 0 means go to stand
		public AnimationCurve m_animFromInput = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
		// animation speed in function of ground speed
		public AnimationCurve m_animFromSpeed = new AnimationCurve(new Keyframe(0, 0), new Keyframe(8, 1));
	}

	[System.Serializable]
	public class SettingsJump
	{
		public bool m_enabled = true;		// enabled status
		public string m_button = "Jump";	// disabled if empty
		public float m_minHeight = 3f;		// min jump height
		public float m_maxHeight = 3f;		// max jump height if pushing jump input
	}

	[System.Serializable]
	public class SettingsWallJump
	{
		public bool m_enabled = true;
		public string m_button = "Jump";					// disabled if empty
		public Vector2 m_jumpPower = new Vector2(10f, 8f);	// power of jump
		public float m_timeBeforeJump = 0.1f;				// time to be snapped on wall before jumping
		public float m_timeBeforeFlip = 0.1f;				// time before flipping after jump (negative to disable)
		public float m_disableAutoRotateTime = 0.3f;		// min time after wall jump to prevent autorotate
		public int[] m_rayIndexes;							// list of ray indexes to use for wall detection, empty means all rays
	}

	[System.Serializable]
	public class SettingsMeleeAttacks
	{
		public bool m_enabled = false;
		public APMeleeAttack[] m_attacks;						// list of melee attacks
	}

	public Inputs m_inputs;						// inputs handler (common for all interactive objects, we may add one by object if needed...)
	public Settings m_basic;					// basic settings
	public Animations m_animations;				// settings for animations
	public SettingsJump m_jump;					// jump settings
	public SettingsWallJump m_wallJump;			// wall jump settings
	public APLadder.Settings m_ladder;			// settings for ladders (can be overriden for each ladder)
	public APRailings.Settings m_railings;		// settings for railings (can be overriden for each railings)
	public SettingsMeleeAttacks m_meleeAttacks; // settings for melee attacks

	// Accessors
	public APCharacterMotor GetMotor()
	{
		return m_motor;
	}

	public Animator GetAnim()
	{
		return m_anim;
	}

	public float GetGroundSpeed()
	{
		return m_groundSpeed;
	}
	
	////////////////////////////////////////////////////////
	// PRIVATE/LOW LEVEL

	// list of different states
	enum State
	{
		Standard = 0,
		Crouch,
		WallJump,
		WallJumpInAir,
		MeleeAttack
	}
	// finite state machine of character controller
	class Fsm : APFsm
	{
		/// Virtual method called when state enter, update or leave occur
		public override void OnStateUpdate(APFsmStateEvent eEvent, uint a_oState)
		{
			switch ((State)a_oState)
			{
				case State.Standard:
					m_controller.StateStandard(eEvent);
					break;
				case State.Crouch: 
					m_controller.StateCrouch(eEvent); 
					break;
				case State.WallJump: 
					m_controller.StateWallJump(eEvent); 
					break;
				case State.WallJumpInAir: 
					m_controller.StateWallJumpInAir(eEvent); 
					break;
				case State.MeleeAttack: 
					m_controller.StateMeleeAttack(eEvent); 
					break;
			}
		}

		public APCharacterController m_controller;
	}
	//	private attributes
	float m_maxVerticalVelForSnap = 0.01f;	// max vertical vel for ground snapping
	float m_minJumpDuration = 0.1f;			// min time between 2 consecutive jumps
	float m_maxAirVelDamping = 0.5f;		// damping when max vel is reached
	uint m_carryRayIndex = 0;				// used only when different carriers are detected, prefer same ray each time
	float m_maxAttackDuration = 10f;		// max time an attack can occur (prevent infinite state)
	float m_downDuration = 0.1f;			// time during which down button status can lasts
	APCharacterMotor m_motor;
	Animator m_anim;
	bool m_debugDraw = false;
	Fsm m_fsm = new Fsm();

	// basics
	float m_groundSpeed;
	bool m_isControlled;
	bool m_onGround;
	float m_speedFactor;
	bool m_sliding;	 // used only for animation

	//	jump
	float m_lastJumpTime;
	float m_animAirTime;
	bool m_jumpDown;
	float m_jumpButtonTimeDown;

	// crouch
	float m_crouchBoxOriginalSize;
	float m_crouchBoxOriginalCenter;

	// carry
	APCarrier m_carrier;

	// wall jump
	bool m_wallJumpFlipDone;
	bool m_wallJumpAutoRotate;

	// attacks
	int m_curAttackId;
	int m_curAttackAnimHash;
	Collider2D[] m_attackHitResult = new Collider2D[8];
	bool m_attackCrouched;


	// Use this for initialization
	void Awake()
	{
		m_motor = GetComponent<APCharacterMotor>();
		m_anim = GetComponent<Animator>();
		m_fsm.m_controller = this;

		ClearRuntimeValues();
	}

	void OnDisable()
	{
		m_fsm.StopFsm();
	}

	void ClearRuntimeValues()
	{
		m_carrier = null;
		m_jumpDown = false;
		m_isControlled = false;
		m_onGround = false;
		m_animAirTime = m_animations.m_minAirTime;
		m_speedFactor = 1f;
		m_lastJumpTime = 0f;
		m_crouchBoxOriginalSize = 1f;
		m_crouchBoxOriginalCenter = 0f;
		m_groundSpeed = 0f;
		m_sliding = false;
		m_curAttackId = -1;
		m_attackCrouched = false;
		m_jumpButtonTimeDown = 0f;

		foreach (APMeleeAttack curAttack in m_meleeAttacks.m_attacks)
		{
			curAttack.timeDown = 0f;
			curAttack.inputStatus = false;
		}

		SetState(State.Standard);
	}

	void Start()
	{
		// collect collision info at first frame
		m_motor.Move();
		UpdateGroundStatus();
	}
	// Physic update
	void FixedUpdate()
	{
		if (APSettings.m_fixedUpdate)
		{
			UpdateController();
			UnsetInputs();
		}
	}
	// Update is called once per frame
	void Update()
	{
		if (APSettings.m_fixedUpdate)
		{
			SetInputs();
		} 
		else
		{
			SetInputs();
			UpdateController();
			UnsetInputs();
		}
	}

	void SetInputs()
	{
		if(Input.GetButtonDown(m_wallJump.m_button))
		{
			m_jumpDown = true;
			m_jumpButtonTimeDown = Time.time;
		}

		foreach (APMeleeAttack curAttack in m_meleeAttacks.m_attacks)
		{
			if (Input.GetButtonDown(curAttack.m_button))
			{
				curAttack.inputStatus = true;
				curAttack.timeDown = Time.time;
			}
		}
	}

	void UnsetInputs()
	{
		// reset inputs after amount of time
		if(m_jumpDown && (Time.time >= m_jumpButtonTimeDown + m_downDuration))
		{
			m_jumpDown = false;
		}

		foreach (APMeleeAttack curAttack in m_meleeAttacks.m_attacks)
		{
			if(curAttack.inputStatus && (Time.time >= curAttack.timeDown + m_downDuration))
			{
				curAttack.inputStatus = false;
			}
		}
	}

	void LateUpdate()
	{
		HandleAnimation();
		HandleCarry();
	}

	void UpdateController()
	{
		// Update inputs in all case (may be used by other objects)
		HandleInputFilter();

		// Do nothing if controlled by something
		if (IsControlled)
			return;

		// Update states
		m_fsm.UpdateFsm(Time.deltaTime);

		// Finally move character with its current velocity
		m_motor.Move();

		// Update our own touch ground status
		UpdateGroundStatus();
	}

	void StateStandard(APFsmStateEvent eEvent)
	{
		if (eEvent == APFsmStateEvent.eUpdate)
		{
			ApplyGravity();
			HandleCrouch();
			HandleHorizontalMove();
			HandleJump();
			HandleWallJump();
			HandleMeleeAttack();
			HandleAutoRotate();
		}
	}

	void StateCrouch(APFsmStateEvent eEvent)
	{
		switch (eEvent)
		{
			case APFsmStateEvent.eEnter:
			{
				SendMessage("APOnCharacterCrouch", SendMessageOptions.DontRequireReceiver);
				PlayAnim(m_animations.m_crouch, GetPreviousState() == State.MeleeAttack ? 1f : 0f);
			}
			break;

			case APFsmStateEvent.eUpdate:
			{
				ApplyGravity();
				HandleCrouch();
				HandleHorizontalMove();
				HandleJump();
				HandleMeleeAttack();

				if(m_basic.m_enableCrouchedAutoRotate)
					HandleAutoRotate();
			}
			break;
		}
	}

	void StateWallJump(APFsmStateEvent eEvent)
	{
		switch (eEvent)
		{
			case APFsmStateEvent.eEnter:
			{
				SendMessage("APOnCharacterWallJump", SendMessageOptions.DontRequireReceiver);
				PlayAnim(m_animations.m_wallJump, 0f);
				m_wallJumpAutoRotate = m_basic.m_autoRotate;

				// prevent auto rotate for a while
				if (m_wallJumpAutoRotate)
					m_basic.m_autoRotate = false;
			}
			break;

			case APFsmStateEvent.eUpdate:
			{
				// cancel any velocity
				m_motor.m_velocity = Vector2.zero;

				// wait for end of timer before effective jump
				if (m_fsm.GetFsmStateTime() >= m_wallJump.m_timeBeforeJump)
				{
					m_motor.m_velocity.y = m_wallJump.m_jumpPower.y;
					m_motor.m_velocity.x = m_wallJump.m_jumpPower.x * (m_motor.FaceRight ? -1f : 1f);
					SetState(State.WallJumpInAir);
				}
			}
			break;

			case APFsmStateEvent.eLeave:
			{
				// restore initial auto rotate in all cases
				m_basic.m_autoRotate = m_wallJumpAutoRotate;				
			}
			break;
		}
	}

	void StateWallJumpInAir(APFsmStateEvent eEvent)
	{
		switch (eEvent)
		{
			case APFsmStateEvent.eEnter:
			{				
				m_wallJumpAutoRotate = m_basic.m_autoRotate;
				m_wallJumpFlipDone = false;
			
				// prevent auto rotate for a while
				if (m_wallJumpAutoRotate)
					m_basic.m_autoRotate = false;
			}
			break;
				
			case APFsmStateEvent.eUpdate:
			{
				// go back to standard state as soon as touching ground
				if (m_onGround)
				{
					SetState(State.Standard);					
				} 
				else
				{
					// flip when needed
					bool bFlipEnabled = (m_wallJump.m_timeBeforeFlip >= 0f);
					if (!m_wallJumpFlipDone && bFlipEnabled && (m_fsm.GetFsmStateTime() >= m_wallJump.m_timeBeforeFlip))
					{
						m_motor.Flip();
						m_wallJumpFlipDone = true;
					}
				
					// enable back auto rotate if needed
					if (m_wallJumpAutoRotate && !m_basic.m_autoRotate && (m_fsm.GetFsmStateTime() >= m_wallJump.m_disableAutoRotateTime) && (m_wallJumpFlipDone || !bFlipEnabled))
					{
						m_basic.m_autoRotate = true;
					}
				
					// standard process
					StateStandard(APFsmStateEvent.eUpdate);

					// leave state if work is ended and no new state is requested
					if (!IsNewStateRequested() && (!bFlipEnabled || m_wallJumpFlipDone) && (!m_wallJumpAutoRotate || m_basic.m_autoRotate))
					{
						SetState(State.Standard);
					}
				}
			}
			break;
				
			case APFsmStateEvent.eLeave:
			{
				// restore initial auto rotate in all cases
				m_basic.m_autoRotate = m_wallJumpAutoRotate;				
			}
			break;
		}
	}

	void StateMeleeAttack(APFsmStateEvent eEvent)
	{
		switch (eEvent)
		{
			case APFsmStateEvent.eEnter:
			{
				SendMessage("APOnCharacterMeleeAttack", SendMessageOptions.DontRequireReceiver);
				PlayAnim(m_curAttackAnimHash, 0f);			

				// clear buffer of hits & disable hit zones at init
				APMeleeAttack curAttack = m_meleeAttacks.m_attacks[m_curAttackId];
				foreach (APHitZone curHitZone in curAttack.m_hitZones)
				{
					curHitZone.attackHits.Clear();
				}
			}
			break;

			case APFsmStateEvent.eUpdate:
			{
				// update state
				ApplyGravity();
				HandleHorizontalMove();	
				
				// Compute all hits for current attack
				APMeleeAttack curAttack = m_meleeAttacks.m_attacks [m_curAttackId];
				foreach (APHitZone curHitZone in curAttack.m_hitZones)
				{
					if (curHitZone.m_active && curHitZone.gameObject.activeInHierarchy)
					{
						Vector2 wsPos = curHitZone.transform.position;
						int hitCount = Physics2D.OverlapCircleNonAlloc(wsPos, curHitZone.m_radius, m_attackHitResult, m_motor.m_rayLayer);
						for (int i = 0; i < hitCount; i++)
						{
							Collider2D curHit = m_attackHitResult[i];

							// notify only hitable objects and not already hit by this hit zone
							APHitable hitable = curHit.GetComponent<APHitable>();
							if (hitable != null && !curHitZone.attackHits.Contains(hitable))
							{
								curHitZone.attackHits.Add(hitable);

								// alert hitable
								hitable.OnMeleeAttackHit(this, curHitZone);
							}
						}
					}
				}			
				
				// make sure state does not end infinitely	
				if (m_fsm.GetFsmStateTime() > m_maxAttackDuration)
				{
					SetState(GetPreviousState());
				}
			}
			break;

			case APFsmStateEvent.eLeave:
			{
				m_curAttackId = -1;
			}
			break;
		}
	}

	// called when melee attack must end and go back to previous state
	void LeaveMeleeAttack()
	{
		if(GetState() == State.MeleeAttack)
		{
			SetState(GetPreviousState());
		}
	}

	void HandleMeleeAttack()
	{
		// check if attack allowed
		State eCurState = GetState();
		if (m_meleeAttacks.m_enabled && ((eCurState == State.Crouch) || (eCurState == State.Standard)) && !IsNewStateRequested())
		{
			// parse list of attack, check if one is launched
			for (int i = 0; i < m_meleeAttacks.m_attacks.Length; i++)
			{
				APMeleeAttack curAttack = m_meleeAttacks.m_attacks [i];
				if (curAttack.inputStatus)
				{
					// make sure this attack is enabled for this state
					string sAttackAnim = (eCurState == State.Crouch) ? curAttack.m_animCrouched : (m_onGround ? curAttack.m_animStand : curAttack.m_animInAir);
					if (string.IsNullOrEmpty(sAttackAnim))
					{						
						continue;
					}

					// launch attack
					m_curAttackId = i;		
					m_curAttackAnimHash = Animator.StringToHash(sAttackAnim);
					m_attackCrouched = (eCurState == State.Crouch);

					SetState(State.MeleeAttack);
					return;
				}
			}			
		}
	}

	public bool IsAttacking()
	{
		return (GetState() == State.MeleeAttack) || (GetRequestedState() == State.MeleeAttack);
	}

	public bool IsAttackingCrouched()
	{
		return IsAttacking() && m_attackCrouched;
	}

	void SetState(State newState)
	{
		m_fsm.SetRequestedState((uint)newState);
	}

	State GetState()
	{
		return (State)m_fsm.GetState();
	}

	State GetRequestedState()
	{
		return (State)m_fsm.GetRequestedState();
	}

	State GetPreviousState()
	{
		return (State)m_fsm.GetPreviousState();
	}

	void ApplyGravity()
	{
		if (!m_onGround)
			m_motor.m_velocity.y -= m_basic.m_gravity * Time.deltaTime;
	}

	void HandleInputFilter()
	{
		m_inputs.m_axisX.Update(Time.deltaTime);
		m_inputs.m_axisY.Update(Time.deltaTime);
	}

	void UpdateGroundStatus()
	{
		// first check if we are on ground
		bool bCurrentGroundStatus = false;
		if (m_motor.TouchGround)
		{
			// NB : prevent snapping on ground if velocity is not toward ground normal
			float fVelDotGround = Vector2.Dot(m_motor.m_velocity, m_motor.GetGroundNormal());
			if (/*m_onGround ||*/ fVelDotGround < m_maxVerticalVelForSnap)
			{
				bCurrentGroundStatus = true;
			}
		}

		// clear vertical velocity if we just touched ground
		if ((bCurrentGroundStatus != m_onGround) && bCurrentGroundStatus)
		{
			m_motor.m_velocity.y = 0f;
			m_animAirTime = 0f;

			// and snap on ground
			transform.position += new Vector3(0f, -m_motor.GetDistanceToGround(), 0f);
		}

		if (!bCurrentGroundStatus)
		{
			m_animAirTime += Time.deltaTime;
		}

		// save current status
		if (m_onGround != bCurrentGroundStatus)
		{
			m_onGround = bCurrentGroundStatus;

			if (bCurrentGroundStatus)
				SendMessage("APOnCharacterLand", SendMessageOptions.DontRequireReceiver);
			else
				SendMessage("APOnCharacterInAir", SendMessageOptions.DontRequireReceiver);
		}

		// clear last jump time as soon as we touch ground
		if (m_onGround)
		{
			m_lastJumpTime = 0f;
		}
	}

	void HandleAutoRotate()
	{
		if (m_basic.m_autoRotate)
		{
			if (m_inputs.m_axisX.GetRawValue() > 0f && !m_motor.FaceRight)
			{
				m_motor.Flip();
			} 
			else if (m_inputs.m_axisX.GetRawValue() < 0f && m_motor.FaceRight)
			{
				m_motor.Flip();
			}
		}
	}

	bool GetInputRunning()
	{
		return m_basic.m_alwaysRun || (string.IsNullOrEmpty(m_inputs.m_runButton) ? false : Input.GetButton(m_inputs.m_runButton));
	}

	public float ComputeMaxSpeed()
	{
		if (!m_onGround)
		{
			return m_basic.m_maxAirSpeed;
		} 
		else
		{
			return GetInputRunning() ? m_basic.m_runSpeed : m_basic.m_walkSpeed;
		}
	}

	void HandleHorizontalMove()
	{		
		m_sliding = false;
		float maxSpeed = ComputeMaxSpeed();

		// compute horizontal velocity from input
		float fMoveDir = m_inputs.m_axisX.GetRawValue() != 0f ? Mathf.Sign(m_inputs.m_axisX.GetRawValue()) : (m_motor.FaceRight ? 1f : -1f);

		// compute slope factor
		m_speedFactor = 1f;
		if (m_onGround)
		{
			float fGroundAngle = Mathf.Rad2Deg * Mathf.Acos(m_motor.GetGroundNormal().y);
			fGroundAngle = fMoveDir != Mathf.Sign(m_motor.GetGroundNormal().x) ? fGroundAngle : -fGroundAngle;
			m_speedFactor = Mathf.Clamp01(m_basic.m_slopeSpeedMultiplier.Evaluate(fGroundAngle));
		}
		
		// Compute move direction
		Vector2 hrzMoveDir = new Vector2(fMoveDir, 0f);
		float absAxisX = Mathf.Abs(m_inputs.m_axisX.GetRawValue());
		float hrzMoveLength = absAxisX * maxSpeed * m_speedFactor;

		// align this velocity on ground plane if we touch the ground 
		bool bCrouched = IsCrouched();
		bool bAttacking = IsAttacking();
		if (m_onGround)
		{
			float fDot = Vector2.Dot(hrzMoveDir, m_motor.GetGroundNormal());
			if (Mathf.Abs(fDot) > float.Epsilon)
			{
				Vector3 perpAxis = Vector3.Cross(hrzMoveDir, m_motor.GetGroundNormal());
				hrzMoveDir = Vector3.Cross(m_motor.GetGroundNormal(), perpAxis);
				hrzMoveDir.Normalize();
			}

			// cancel input if needed
			if (bCrouched || bAttacking)
			{
				hrzMoveLength = 0f;
			}
		}

		// handle dynamic
		if (m_onGround)
		{
			float fDynFriction, fStaticFriction;
			ComputeFrictions(out fDynFriction, out fStaticFriction);

			// update sliding status
			if (fDynFriction < 1f)
			{
				m_sliding = true;
			} 

			float fVelOnMove = Vector2.Dot(m_motor.m_velocity, hrzMoveDir);
			float fDirLength = fVelOnMove;
			if(m_sliding)
			{
				// dynamic is different while sliding
				if(!bCrouched && !bAttacking && absAxisX > 0f)
				{
					float fDiffMax = maxSpeed - fVelOnMove;
					if(fDiffMax > 0f)
					{
						fDirLength = fVelOnMove + Mathf.Min(fDiffMax, absAxisX * fDynFriction * Time.deltaTime * 20f);
					}
				}
				else
				{
					fDirLength = ApplyDamping(fVelOnMove, fStaticFriction);
				}
			}
			else
			{
				// raise deceleration for crouched/attack
				float fDecel = (bCrouched || bAttacking) ? m_basic.m_deceleration * 2f : m_basic.m_deceleration;
				fDirLength = APInput.Update(fVelOnMove, hrzMoveLength, true, m_basic.m_acceleration, fDecel, Time.deltaTime);
			}

			ClampValueWithDamping(ref fDirLength, m_maxAirVelDamping, -maxSpeed, maxSpeed);
			m_motor.m_velocity = hrzMoveDir * (fDirLength);
			m_groundSpeed = Mathf.Abs(fDirLength);
		}
		else
		{
			// in air dynamic
			float fVelOnMove = Vector2.Dot(m_motor.m_velocity, hrzMoveDir);
			float fDiffVel = (hrzMoveLength - fVelOnMove);
			float fMaxAccel = m_basic.m_airPower * Time.deltaTime;
			fDiffVel = Mathf.Clamp(fDiffVel, -fMaxAccel, fMaxAccel);

			m_motor.m_velocity += hrzMoveDir * fDiffVel;
			ClampValueWithDamping(ref m_motor.m_velocity.x, m_maxAirVelDamping, -m_basic.m_maxAirSpeed, m_basic.m_maxAirSpeed);
			ClampValueWithDamping(ref m_motor.m_velocity.y, m_maxAirVelDamping, -m_basic.m_maxFallSpeed, m_basic.m_maxFallSpeed);
			m_groundSpeed = 0f;
		}
	}

	void ComputeFrictions(out float fDynFriction, out float fStaticFriction)
	{
		fDynFriction = 0f;
		fStaticFriction = 0f;

		// compute material override friction, keep highest value if different grounds are touched
		bool bOverride = false;
		for (int i = 0; i < m_motor.m_RaysGround.Length; i++)
		{
			if (m_motor.m_RaysGround [i].m_collider)
			{
				APMaterial groundMat = m_motor.m_RaysGround [i].m_collider.GetComponent<APMaterial>();
				if (groundMat != null && groundMat.m_overrideFriction)
				{
					bOverride = true;
					fDynFriction = Mathf.Max(fDynFriction, groundMat.m_dynFriction);
					fStaticFriction = Mathf.Max(fStaticFriction, groundMat.m_staticFriction);
				}
			}
		}
		
		// keep default value if no override
		if (!bOverride)
		{		
			fDynFriction = m_basic.m_frictionDynamic;
			fStaticFriction = m_basic.m_frictionStatic;
		}
	}

	void ClampValueWithDamping(ref float fValue, float fDamping, float fMin, float fMax)
	{
		if (fValue > fMax)
		{
			fValue = Mathf.Max(fMax, ApplyDamping(fValue, fDamping));
		} 
		else if (fValue < fMin)
		{
			fValue = Mathf.Min(fMin, ApplyDamping(fValue, fDamping));
		}
	}

	float ApplyDamping(float fValue, float fDampingValue)
	{
		float fDamping = Mathf.Exp(-fDampingValue * Time.deltaTime);
		return fValue * fDamping;
	}

	void HandleCarry()
	{
		m_carrier = null;
		if (m_onGround)
		{
			for (int i = 0; i < m_motor.m_RaysGround.Length; i++)
			{
				if (m_motor.m_RaysGround [i].m_collider)
				{
					APCarrier curCarrier = m_motor.m_RaysGround [i].m_collider.GetComponent<APCarrier>();
					if (curCarrier != null)
					{
						// use prefer index if carrier is already assigned
						if ((m_carrier == null) || (i == m_carryRayIndex))
						{
							m_carrier = curCarrier;
						}
					}
				}
			}
			
			// for now move player by position
			// todo : we should move by velocity properly and detect collision by the way, must be in future release !
			if (m_carrier != null)
			{
				Vector2 carrierOffset = m_carrier.GetVelocity() * Time.deltaTime;
				transform.position += new Vector3(carrierOffset.x, carrierOffset.y, 0f);
			}
		}
	}

	void HandleJump()
	{
		if (string.IsNullOrEmpty(m_jump.m_button))
			return;

		// check if we should jump
		if (m_jump.m_enabled && m_jumpDown && m_onGround && (Time.time - m_lastJumpTime > m_minJumpDuration))
		{ 
			// make sure we can jump if crouched
			if (!IsCrouched() || CanUncrouch())
			{ 
				Uncrouch();
				Jump(1f);

				// to prevent jumping too fast when current slope is limiting speed, we may add a separate curve...
				m_motor.m_velocity.x *= m_speedFactor; 		

				SendMessage("APOnCharacterJump", SendMessageOptions.DontRequireReceiver);
				return;
			}
		}

		// handle extra jumping
		if (!m_onGround && (m_lastJumpTime > 0f) && Input.GetButton(m_jump.m_button) && !m_motor.TouchHead)
		{
			if (Time.time < m_lastJumpTime + (m_jump.m_maxHeight - m_jump.m_minHeight) / ComputeJumpVerticalSpeed(m_jump.m_minHeight))
			{
				// remove gravity
				m_motor.m_velocity.y += m_basic.m_gravity * Time.deltaTime;
			}
		}
	}

	// force character to jump immediately at minimum height * specified ratio
	public void Jump(float fRatio)
	{
		m_lastJumpTime = Time.time;
		m_animAirTime = m_animations.m_minAirTime;		
		m_motor.m_velocity.y = ComputeJumpVerticalSpeed(m_jump.m_minHeight * fRatio);
		
		// inject carrier horizontal velocity
		if (m_carrier)
		{
			m_motor.m_velocity.x += m_carrier.GetVelocity().x;
		}
	}

	void HandleWallJump()
	{
		// early exit
		if (string.IsNullOrEmpty(m_jump.m_button) || !m_jumpDown || IsCrouched() || m_onGround || IsNewStateRequested())
			return;
		
		// check if touching wall
		bool bHit = false;
		uint iHitCount = 0;
		APMaterial.BoolValue bMaterialSnap = APMaterial.BoolValue.Default;

		for (int i = 0; i < m_motor.m_RaysFront.Length; i++)
		{
			// check if this ray should be tested
			bool bTestRay = (m_wallJump.m_rayIndexes.Length == 0);
			for (int j = 0; j < m_wallJump.m_rayIndexes.Length; j++)
			{
				if (m_wallJump.m_rayIndexes [j] == i)
				{
					bTestRay = true;
					break;
				}
			}

			if (bTestRay && m_motor.m_RaysFront [i].m_collider)
			{
				iHitCount++;

				// use hit material value, true has priority
				APMaterial hitMat = m_motor.m_RaysFront [i].m_collider.GetComponent<APMaterial>();
				if (hitMat != null)
				{
					if (hitMat.m_wallJump == APMaterial.BoolValue.True)
					{
						bMaterialSnap = APMaterial.BoolValue.True;
					} 
					else if (hitMat.m_wallJump == APMaterial.BoolValue.False && bMaterialSnap != APMaterial.BoolValue.True)
					{
						bMaterialSnap = APMaterial.BoolValue.False;
					}
				}
			}
		}

		if (m_wallJump.m_rayIndexes.Length == 0)
			bHit = (iHitCount == m_motor.m_RaysFront.Length);
		else
			bHit = (iHitCount == m_wallJump.m_rayIndexes.Length);


		// effective jump
		bool bCanJump = bHit && (bMaterialSnap != APMaterial.BoolValue.False) && (bMaterialSnap == APMaterial.BoolValue.True || m_wallJump.m_enabled);
		if (bCanJump && (Time.time - m_lastJumpTime > m_minJumpDuration))
		{			
			m_lastJumpTime = Time.time;
			m_animAirTime = m_animations.m_minAirTime;

			// put in wall jump state
			SetState(State.WallJump);
		}
	}

	bool GetInputCrouch()
	{
		return m_inputs.m_axisY.GetRawValue() < -0.5f;
	}

	public bool IsCrouched()
	{
		return (GetState() == State.Crouch) || (GetRequestedState() == State.Crouch) || IsAttackingCrouched();
	}

	void HandleCrouch()
	{
		// Do not change state if currently switching
		if(IsNewStateRequested())
		   return;

		// crouch if needed
		if (!IsCrouched() && m_onGround && GetInputCrouch())
		{
			Crouch();
		}
		// uncrouch as soon as we can
		else if (IsCrouched() && !GetInputCrouch() && CanUncrouch())
		{
			Uncrouch();
		}
	}

	bool CanUncrouch()
	{
		// make sure expanded box does not collide
		Vector2 orgSize = new Vector2(m_motor.GetBoxCollider().size.x, m_crouchBoxOriginalSize);
		Vector2 orgCenter = new Vector2(m_motor.GetBoxCollider().center.x, m_crouchBoxOriginalCenter);
		
		float fScale = 0.9f;
		float fBoxTop = (orgCenter + 0.5f * orgSize).y;
		Vector2 boxA = m_motor.GetBoxCollider().center + Vector2.Scale(m_motor.GetBoxCollider().size * fScale, new Vector2(-0.5f, 0.5f));
		Vector2 boxB = new Vector2(boxA.x + m_motor.GetBoxCollider().size.x * fScale, fBoxTop);
		boxA = transform.TransformPoint(boxA);
		boxB = transform.TransformPoint(boxB);
		
		if (m_debugDraw)
		{
			Debug.DrawLine(boxA, boxB);
		}
		
		return (Physics2D.OverlapArea(boxA, boxB, m_motor.m_rayLayer) == null);
	}

	void Crouch()
	{
		if (!IsCrouched())
		{
			// reduce collision box (and stay on same base plane)
			Vector2 curBoxSize = m_motor.GetBoxCollider().size;
			Vector2 curBoxCenter = m_motor.GetBoxCollider().center;
			m_crouchBoxOriginalSize = curBoxSize.y;
			m_crouchBoxOriginalCenter = curBoxCenter.y;
			
			Vector2 newSize = new Vector2(curBoxSize.x, curBoxSize.y * m_basic.m_crouchSizePercent);
			m_motor.GetBoxCollider().size = newSize;
			
			float fVertOffset = (m_basic.m_crouchSizePercent - 1) * curBoxSize.y * 0.5f;
			Vector2 newCenter = new Vector2(curBoxCenter.x, curBoxCenter.y + fVertOffset);
			m_motor.GetBoxCollider().center = newCenter;
			
			// do the same for the rays
			m_motor.Scale = new Vector2(1f, m_basic.m_crouchSizePercent);
			m_motor.Offset = new Vector2(0f, fVertOffset);

			// play anim
			SetState(State.Crouch);
		}
	}

	void Uncrouch()
	{
		if (IsCrouched())
		{
			// restore collision box
			Vector2 orgSize = new Vector2(m_motor.GetBoxCollider().size.x, m_crouchBoxOriginalSize);
			Vector2 orgCenter = new Vector2(m_motor.GetBoxCollider().center.x, m_crouchBoxOriginalCenter);
			m_motor.GetBoxCollider().size = orgSize;
			m_motor.GetBoxCollider().center = orgCenter;
			
			// and motor
			m_motor.Scale = Vector2.one;
			m_motor.Offset = Vector2.zero;

			SendMessage("APOnCharacterUncrouch", SendMessageOptions.DontRequireReceiver);

			SetState(State.Standard);
		}
	}

	float ComputeJumpVerticalSpeed(float targetJumpHeight)
	{
		// estimation of speed for required height
		return Mathf.Sqrt(2 * targetJumpHeight * m_basic.m_gravity);
	}

	public bool IsControlled
	{
		get
		{
			return m_isControlled;
		}
		set
		{

			// reset runtime values when no more controlled
			if (m_isControlled != value && !value)
			{
				ClearRuntimeValues();
			}

			m_isControlled = value;
		}
	}

	void HandleAnimation()
	{
		if (m_isControlled)
			return;

		// reset animation speed
		m_anim.speed = 1f;

		// handle standard state
		if (GetState() == State.Standard)
		{
			if (m_onGround || m_animAirTime < m_animations.m_minAirTime)
			{
				// mode from input
				float fFilteredInput = Mathf.Abs(m_inputs.m_axisX.GetValue());
				if (m_animations.m_walkAnimFromInput)
				{
					float fSpeed = m_animations.m_animFromInput.Evaluate(fFilteredInput);
					if (fSpeed < 1e-3f)
					{
						PlayAnim(m_animations.m_stand);
					} 
					else
					{					
						PlayAnim(m_animations.m_run);
						m_anim.speed = Mathf.Abs(fSpeed); 
					}
				} 
				else
				{
					// compute speed on ground/in air
					float fGroundSpeed = m_onGround ? m_motor.ComputeVelocityOnGround().magnitude : Mathf.Abs(m_motor.m_velocity.x);

					float fSpeedFromInput = m_animations.m_animFromInput.Evaluate(fFilteredInput);
					float fSpeedFromGround = m_animations.m_animFromSpeed.Evaluate(fGroundSpeed);
					if ((fSpeedFromInput < 1e-3f && m_sliding) || (!m_sliding && fSpeedFromGround < 1e-3f))
					{
						PlayAnim(m_animations.m_stand);
					} 
					else
					{
						float fSpeed = 0f;
						if (m_sliding)
						{
							fSpeed = fSpeedFromInput;
						} 
						else
						{
							fSpeed = fSpeedFromGround;
						}

						PlayAnim(m_animations.m_run);
						m_anim.speed = Mathf.Abs(fSpeed); 
					}
				}
			} 
			else
			{
				PlayAnim(m_animations.m_inAir);
			}
		}
	}

	public void PlayAnim(string sAnim)
	{
		if (!string.IsNullOrEmpty(sAnim))
		{
			m_anim.Play(sAnim);
		}
	}

	public void PlayAnim(string sAnim, float fNormalizedTime)
	{
		if (!string.IsNullOrEmpty(sAnim))
		{
			m_anim.Play(sAnim, 0, fNormalizedTime);
		}
	}
	
	public void PlayAnim(int iAnimHash, float fNormalizedTime)
	{
		if (iAnimHash != 0)
		{
			m_anim.Play(iAnimHash, 0, fNormalizedTime);
		}
	}

	bool IsNewStateRequested()
	{
		return GetState() != GetRequestedState();
	}
}