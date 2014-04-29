/* Copyright (c) 2014 Advanced Platformer 2D */

using UnityEngine;
using System.Collections;

[AddComponentMenu("Advanced Platformer 2D/Carrier")]

public class APCarrier : MonoBehaviour
{ 
	////////////////////////////////////////////////////////
	// PUBLIC/HIGH LEVEL

	// Return last velocity, for now this must be called in your LateUpdate !
	// and carrier position must be updated by animation with "Animate Physics" unchecked
	public Vector2 GetVelocity() 
	{
		if(m_bCompute)
		{
			m_velocity = ((Vector2)transform.position - m_prevPos) / Time.deltaTime;
			m_bCompute = false;
		}

		return m_velocity;
	}

	////////////////////////////////////////////////////////
	// PRIVATE/LOW LEVEL
	void Start ()
	{
		m_prevPos = transform.position;
		m_velocity = Vector2.zero;
		m_bCompute = false;
	}

	void Update ()
	{
		m_bCompute = true;
		m_prevPos = transform.position;
	}

	void LateUpdate ()
	{
		// animation has been updated between Update & LateUpdate call
		if(m_bCompute)
		{
			m_velocity = ((Vector2)transform.position - m_prevPos) / Time.deltaTime;
			m_bCompute = false;
		}
	}

	Vector2 m_prevPos;
	Vector2 m_velocity;
	bool m_bCompute;
}

