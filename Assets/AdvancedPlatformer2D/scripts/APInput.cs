/* Copyright (c) 2014 Advanced Platformer 2D */

using UnityEngine;

[System.Serializable]
public class APInput
{
	////////////////////////////////////////////////////////
	// PUBLIC/HIGH LEVEL
	public string m_name = "axis";
	public float m_acceleration = 10f;
	public float m_deceleration = 10f;
	public bool m_snap = true;

	////////////////////////////////////////////////////////
	// PRIVATE/LOW LEVEL
	float m_value = 0f;
	
	public APInput(string sName) 
	{
		m_name = sName; 
	}

	public void Reset() 
	{
		m_value = 0f;
	}

	public void Update(float dt)
	{
		m_value = Update(m_value, Input.GetAxisRaw(m_name), m_snap, m_acceleration, m_deceleration, dt);
		m_value = Mathf.Clamp(m_value, -1f, 1f);
	}

	static public float Update(float fCurValue, float fNewVal, bool bSnap, float fAccel, float fDecel, float dt)
	{
		float fSignNew = Mathf.Sign(fNewVal);
		float fSignCur = Mathf.Sign(fCurValue);
		
		// handle snap
		if(bSnap && (fNewVal != 0f) && (fSignNew != fSignCur))
		{
			fCurValue = 0f;
		}

		// handle deceleration first
		float fDiffValue = fNewVal - fCurValue;
		if(fCurValue >= 0f)
		{
			if(fDiffValue >= 0f)
			{
				// accel
				fDiffValue = Mathf.Min(fDiffValue, fAccel * dt);
			}
			else
			{
				if(fDiffValue < -fCurValue)
				{
					// we are changing sign in one frame, handle this special case
					if(fCurValue > fDecel * dt)
					{
						fDiffValue = -fDecel * dt;

					}
					else
					{
						fDiffValue = Mathf.Max(fDiffValue + fCurValue, -fAccel * dt) - fCurValue;
					}
				}
				else
				{
					// decel
					fDiffValue = Mathf.Max(fDiffValue, -fDecel * dt);
				}
			}
		}
		else
		{
			if(fDiffValue <= 0f)
			{
				// accel
				fDiffValue = Mathf.Max(fDiffValue, -fAccel * dt);
			}
			else
			{
				if(fDiffValue > -fCurValue)
				{
					// we are changing sign in one frame, handle this special case
					if(fCurValue < -fDecel * dt)
					{
						fDiffValue = fDecel * dt;

					}
					else
					{
						fDiffValue = Mathf.Min(fDiffValue + fCurValue, fAccel * dt) - fCurValue;
					}
				}
				else
				{
					// decel
					fDiffValue = Mathf.Min(fDiffValue, fDecel * dt);
				}
			}
		}

		// final update
		return fCurValue + fDiffValue;
	}
	
	public float GetValue()
	{
		return m_value;
	}

	public float GetRawValue()
	{
		return Input.GetAxisRaw(m_name);
	}

	public void SetValue(float fValue)
	{
		m_value = fValue;
	}
}

