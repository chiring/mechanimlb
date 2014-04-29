/* Copyright (c) 2014 Advanced Platformer 2D */

using UnityEngine;

[System.Serializable]
public class APMeleeAttack 
{
    ////////////////////////////////////////////////////////
    // PUBLIC/HIGH LEVEL
	public string m_button = "Attack";					// button to use for this attack
	public string m_animStand = "AttackStand"; 			// animation to use when attacking in stand position
	public string m_animCrouched = "AttackCrouched"; 	// animation to use when attacking in crouched position
	public string m_animInAir = "AttackInAir"; 			// animation to use when attacking while in air

	public APHitZone[] m_hitZones;						// list of hit zones for hit detection


	////////////////////////////////////////////////////////
	// PRIVATE/HIGH LEVEL
	bool m_inputStatus;							// runtime variable for handling status of input
	float m_timeDown;							// time at which last down occured

	public bool inputStatus
	{
		get
		{
			return m_inputStatus;
		}
		set
		{
			m_inputStatus = value;
		}
	}

	public float timeDown
	{
		get
		{
			return m_timeDown;
		}
		set
		{
			m_timeDown = value;
		}
	}
}
