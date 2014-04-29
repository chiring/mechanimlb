/* Copyright (c) 2014 Advanced Platformer 2D */


using UnityEngine;
using System.Collections;

[AddComponentMenu("Advanced Platformer 2D/Samples/APSampleCrate")]

// Sample for specific explodable Crate behavior
public class APSampleCrate : APHitable 
{
	////////////////////////////////////////////////////////
	// PRIVATE/LOW LEVEL
	int m_hitCount;
	bool m_shouldDisable;
	Animator m_anim;
	float m_minTimeBetweenTwoReceivedHits = 0.1f;
	float m_lastHitTime;	
	
	void Start () 
	{
		// init start position
		m_hitCount = 2;
		m_lastHitTime = 0f;
		m_anim = GetComponent<Animator>();
		m_shouldDisable = false;
	}
	
	void LateUpdate()
	{
		if(m_shouldDisable)
		{
			gameObject.SetActive(false);
		}
	}

	// return true if object is dead
	bool IsDead() 
	{
		return m_hitCount <= 0;
	}
	
	// called when we have been hit by a melee attack
	override public void OnMeleeAttackHit(APCharacterController character, APHitZone hitZone)
	{
		// do nothing if already dead
		if (IsDead())
			return;

		// prevent checking hits too often
		if(Time.time < m_lastHitTime + m_minTimeBetweenTwoReceivedHits)
			return;
		
		// save current hit time
		m_lastHitTime = Time.time;
		
		// reduce hit count
		m_hitCount -= 1;
		
		// handle death & callbacks
		if (m_hitCount <= 0)
		{
			m_hitCount = 0;
			
			// launch die animation
			if(m_anim)
			{
				m_anim.Play("explode", 0, 0f);
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
