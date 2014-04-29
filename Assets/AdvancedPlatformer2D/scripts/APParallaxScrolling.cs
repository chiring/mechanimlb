/* Copyright (c) 2014 Advanced Platformer 2D */

using UnityEngine;

[System.Serializable]
public class APParallaxScrolling 
{
	////////////////////////////////////////////////////////
	// PUBLIC/HIGH LEVEL
	public float m_Ratio = 1f;		// speed ratio in [0,1] range
	public Transform m_Target;		// object to update

	////////////////////////////////////////////////////////
	// PRIVATE/LOW LEVEL
	float m_StartPosX;
	float m_initOffsetX;
	APCamera m_Camera;	

	public void Init (APCamera oCamera)
	{
		m_Camera = oCamera;
		m_StartPosX = m_Target != null ? m_Target.transform.position.x : 0f;
		m_initOffsetX = m_Camera.transform.position.x - m_StartPosX;
	}

	public void Update()
	{
		if(m_Camera && m_Target)
		{
			// update final position according to camera pos
			Vector3 curPos = m_Target.transform.position;
			float fNewX = Mathf.Lerp(m_StartPosX, m_Camera.transform.position.x - m_initOffsetX, m_Ratio) ;
			m_Target.transform.position = new Vector3(fNewX, curPos.y, curPos.z);
		}
	}
}