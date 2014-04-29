/* Copyright (c) 2014 Advanced Platformer 2D */

using UnityEngine;
using System.Collections;

[AddComponentMenu("Advanced Platformer 2D/Camera")]

public class APCamera : MonoBehaviour 
{ 
	////////////////////////////////////////////////////////
	// PUBLIC/HIGH LEVEL
	public float m_marginX = 0f;				// Max distance between camera and player on X axis
	public float m_marginY = 0f;				// Max distance between camera and player on Y axis
	public float m_powerX = 100f;   			// Power for X axis follow
	public float m_powerY = 100f;				// Power for Y axis follow

	public bool m_clampEnabled = false;			// Enable position clamping
	public Vector2 m_minPos = Vector2.zero;		// Camera min position
	public Vector2 m_maxPos = Vector2.zero;		// Camera max position

	////////////////////////////////////////////////////////
	// PRIVATE/LOW LEVEL
	public Transform player;								// Reference to the player's transform.
	public APParallaxScrolling[] m_parallaxScrollings;	// list of object to handle for parallax scrolling

	void Awake ()
	{
		for(int i = 0; i < m_parallaxScrollings.Length; i++)
		{
			m_parallaxScrollings[i].Init(this);
		}
	}

	void LateUpdate ()
	{
		float camX = transform.position.x;
		float camY = transform.position.y;
		float fOffsetX = 0f;
		float fOffsetY = 0f;
		Vector2 v2Diff = player.position - transform.position;

		// handle x margin
		if(v2Diff.x > m_marginX)
		{
			fOffsetX = (v2Diff.x - m_marginX);
		}
		else if(v2Diff.x < -m_marginX)
		{
			fOffsetX = (v2Diff.x + m_marginX);
		}

		// handle y margin
		if(v2Diff.y > m_marginY)
		{
			fOffsetY = (v2Diff.y - m_marginY);
		}
		else if(v2Diff.y < -m_marginY)
		{
			fOffsetY = (v2Diff.y + m_marginY);
		}

		// update
		camX = Mathf.Lerp(camX, camX + fOffsetX, m_powerX * Time.deltaTime);
		camY = Mathf.Lerp(camY, camY + fOffsetY, m_powerY * Time.deltaTime);

		// clamp if needed
		if(m_clampEnabled)
		{
			camX = Mathf.Clamp(camX, m_minPos.x, m_maxPos.x);
			camY = Mathf.Clamp(camY, m_minPos.y, m_maxPos.y);
		}

		// Final update
		transform.position = new Vector3(camX, camY, transform.position.z);

		// Update parallax scrollings then
		for(int i = 0; i < m_parallaxScrollings.Length; i++)
		{
			m_parallaxScrollings[i].Update();
		}
	}
}
