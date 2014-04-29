using UnityEngine;
using System.Collections;

public class Hit : MonoBehaviour {

	float m_minTimeBetweenTwoReceivedHits = 0.3f;
	float m_lastHitTime;	


	void Start () 
	{
		// init start position
		m_lastHitTime = 0f;

	}

	void OnTriggerEnter2D (Collider2D col) 
	{
		if(Time.time < m_lastHitTime + m_minTimeBetweenTwoReceivedHits)
			return;

		// save current hit time
		m_lastHitTime = Time.time;


		// If it hits an enemy...			
		if (col.tag == "Enemy") 
		{	

			// ... find the Enemy script and call the Hurt function.
			col.gameObject.GetComponent<Enemies> ().Hurt ();
		}


	}
}