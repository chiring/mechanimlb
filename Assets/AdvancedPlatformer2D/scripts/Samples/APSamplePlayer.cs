/* Copyright (c) 2014 Advanced Platformer 2D */

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(APCharacterController))]
[AddComponentMenu("Advanced Platformer 2D/Samples/APSamplePlayer")]

// Sample for handling Player behavior when being hit
public class APSamplePlayer : MonoBehaviour 
{
	public float m_life = 10f;   							// total life of player
	public float m_jumpDamage = 1f;							// ammount of damage done when jumping on Blob
	public float m_godModeTimeWhenHit = 3f;					// time of god mode (i.e invincible mode when beeing hit)
	public LayerMask m_godModeLayerMask;					// collision layer while in god mode
	public float m_hitImpulse = 5f;							// impulse when beeing hit by Blob

	////////////////////////////////////////////////////////
	// PRIVATE/LOW LEVEL
	APCharacterController m_player;
	Animator m_anim;
	bool m_godMode;
	float m_godModeTime;
	LayerMask m_prevCollisionLayer;

	// Use this for initialization
	void Start () 
	{
		m_player = GetComponent<APCharacterController>();
		m_anim = GetComponent<Animator>();

		m_godMode = false;
		m_godModeTime = 0f;
	}
	
	// Update is called once per frame
	void Update () 
	{
		// handle god mode here
		if(m_godMode && Time.time > m_godModeTime + m_godModeTimeWhenHit)
		{
			if(m_anim)
			{
				m_anim.SetBool("GodMode", false);
			}
			m_godMode = false;

			// restore previous layer mask
			m_player.GetMotor().m_rayLayer = m_prevCollisionLayer;
		}
	}

	// return true if character is dead
	public bool IsDead() 
	{
		return m_life <= 0f;
	}
	
	// called when hit by NPC object
	public void OnHit(APSampleBlob blob)
	{
		// do nothing if already dead or no damage
		if (IsDead() || (blob.m_touchDamage <= 0f))
			return;

		// reduce life point
		m_life -= blob.m_touchDamage;
		
		// handle death & callbacks
		if (m_life <= 0f)
		{
			// die !
			m_life = 0f;
		}
		else
		{
			// hit!
			// enable god mode if not already done
			if(!m_godMode)
			{
				m_godMode = true;
				m_godModeTime = Time.time;
				if(m_anim)
				{
					m_anim.SetBool("GodMode", true);
				}
				
				// disable collisions with npcs for a while
				APCharacterMotor motor = m_player.GetMotor();
				m_prevCollisionLayer = motor.m_rayLayer;
				motor.m_rayLayer = m_godModeLayerMask;
				
				// add small impulse in opposite direction
				Vector2 v2Dir = motor.transform.position - blob.transform.position;
				motor.m_velocity.x += (v2Dir.x > 0f ? 1f : -1f) * m_hitImpulse;
				m_player.Jump(0.75f);
			}
		}
	}
}
