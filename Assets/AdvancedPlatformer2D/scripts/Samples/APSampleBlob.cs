/* Copyright (c) 2014 Advanced Platformer 2D */


using UnityEngine;
using System.Collections;

[AddComponentMenu("Advanced Platformer 2D/Samples/APSampleBlob")]

// Sample for specific NPC Blob behavior
public class APSampleBlob : APHitable 
{
	public float m_life = 2f;   							// amount of life point for this game object
	public float m_touchDamage = 1f;						// damage done when touching character
	public float m_jumpRatioInputReleased = 0.5f;			// jump ratio power when jumping on hitable while jump input is released
	public float m_jumpRatioInputPushed = 1f;				// jump ratio power when jumping on hitable while jump input is pushed

	public float m_moveLeftOffset = 5f;						// patrol start position
	public float m_moveRightOffset = 5f;					// patrol end position
	public float m_moveSpeed = 5f;							// move speed
	public bool m_faceRight = true;							// initially facing right ?


	////////////////////////////////////////////////////////
	// PRIVATE/LOW LEVEL
	float m_startPosX;
	bool m_pause;
	bool m_shouldDisable;
	Animator m_anim;
	float m_minTimeBetweenTwoReceivedHits = 0.1f;
	float m_lastHitTime;


	void Start () 
	{
		// init start position
		m_lastHitTime = 0f;
		m_startPosX = transform.position.x;
		m_anim = GetComponent<Animator>();
		m_pause = false;
		m_shouldDisable = false;
	}
	
	void FixedUpdate () 
	{
		// update position
		if(!m_pause)
		{
			Vector2 curPos = transform.position;
			curPos.x += (m_faceRight ? m_moveSpeed : -m_moveSpeed) * Time.deltaTime;
			if(curPos.x >= m_startPosX + m_moveRightOffset)
			{
				curPos.x = m_startPosX + m_moveRightOffset;// - (curPos.x - m_startPosX - m_moveLeftOffset);
				Flip();
			}
			else if(curPos.x <= m_startPosX - m_moveLeftOffset)
			{
				curPos.x = m_startPosX - m_moveLeftOffset;// - (curPos.x - m_startPosX - m_moveLeftOffset);
				Flip();
			}		
			transform.position = curPos;
		}
	}

	void LateUpdate()
	{
		if(m_shouldDisable)
		{
			gameObject.SetActive(false);
		}
	}

	// Flip horizontally
	void Flip()
	{
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
		m_faceRight = !m_faceRight;
	}

	// Pause/unpause movement
	void Pause()
	{
		m_pause = true;
	}

	void UnPause()
	{
		m_pause = false;
	}

	// return true if object is dead
	public bool IsDead() 
	{
		return m_life <= 0f;
	}

	// called when we have been hit by a melee attack
	override public void OnMeleeAttackHit(APCharacterController character, APHitZone hitZone)
	{
		AddHitDamage(hitZone.m_damage);
	}
	
	// called when character motor ray is touching us
	override public void OnCharacterTouch(APCharacterController character, APCharacterMotor.RayType rayType)
	{
		// Do nothing if dead
		if(IsDead())
			return;

		// prevent checking hits too often
		if(Time.time < m_lastHitTime + m_minTimeBetweenTwoReceivedHits)
			return;
		
		// save current hit time
		m_lastHitTime = Time.time;

		// check if jumping on us
		APSamplePlayer samplePlayer = character.GetComponent<APSamplePlayer>();
		if(rayType == APCharacterMotor.RayType.Ground)
		{
			// make character jump
			float fRatio = m_jumpRatioInputReleased;
			string sJumpButton = character.m_jump.m_button;
			if(!string.IsNullOrEmpty(sJumpButton) && Input.GetButton(sJumpButton))
			{
				fRatio = m_jumpRatioInputPushed;
			}
			
			if(fRatio > 0f)
			{
				character.Jump(fRatio);
			}
			
			// add hit to us
			AddHitDamage(samplePlayer.m_jumpDamage);
		}
		else
		{
			// notify hit to character controller as Blob makes damage by touching it
			samplePlayer.OnHit(this);
		}
	}
	
	// Add hit damage
	void AddHitDamage(float hitAmount)
	{
		// do nothing if already dead or no damage
		if (IsDead() || (hitAmount <= 0f))
			return;
		
		// reduce life point
		m_life -= hitAmount;
		
		// handle death & callbacks
		if (m_life <= 0f)
		{
			m_life = 0f;

			// launch die animation
			if(m_anim)
			{
				m_anim.Play("die", 0, 0f);
			}
		}
		else
		{
			// launch hit animation
			if(m_anim)
			{
				m_anim.Play("hit", 0, 0f);
			}
		}
	}

	// Disable object at next frame
	void Disable()
	{
		m_shouldDisable = true;
	}
}
