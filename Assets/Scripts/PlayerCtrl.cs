using UnityEngine;
using System.Collections;

public class PlayerCtrl : MonoBehaviour 
{

	[HideInInspector]
	public bool facingRight = true;			// For determining which way the player is currently facing.
	public bool run = false;
	public bool jump = false;


	public float moveForce = 100f;			// Amount of force added to move the player left and right.
	public float runForce = 400f;			// Amount of force added to move the player left and right.
	public float maxSpeed = 3f;				// The fastest the player can travel in the x axis.
	public float jumpForce = 600f;			// Amount of force added when the player jumps.

	public Animator m_anim;
	private AnimatorStateInfo currentBaseState;

	private Transform groundCheck;			// A position marking where to check if the player is grounded.
	private bool ground = false;

	void Awake ()
	{
		groundCheck = transform.Find("groundCheck");
		m_anim = GetComponent<Animator> ();
	}

	// Update is called once per frame
	void Update () 
	{

		// The player is grounded if a linecast to the groundcheck position hits anything on the ground layer.
		ground = Physics2D.Linecast(transform.position, groundCheck.position, 1 << LayerMask.NameToLayer("Ground"));  

		if (ground) 
		{
			if (Input.GetButtonDown ("Jump")) 
			{
				jump = true;
			}

			if (Input.GetButtonDown ("a_attack")) 
			{
				m_anim.SetTrigger ("t_a_atk");
			}
			if (Input.GetButtonDown ("b_attack"))
			{
				m_anim.SetTrigger ("t_b_atk");

			}
			if (Input.GetButtonDown ("c_attack"))
			{
				run = true;
			}
		}else 
		{
			if (Input.GetButtonDown ("a_attack"))
			{
				m_anim.SetTrigger ("t_jump_a_atk");

			}
			if (Input.GetButtonDown ("b_attack")) 
			{
				m_anim.SetTrigger ("t_jump_b_atk");

			}
		}
	}

	void FixedUpdate ()
	{
		// Cache the horizontal input.
		float h = Input.GetAxis("Horizontal");
		currentBaseState = m_anim.GetCurrentAnimatorStateInfo(0);

		if (!currentBaseState.IsTag ("atk")) 
		{

			// If the player is changing direction (h has a different sign to velocity.x) or hasn't reached maxSpeed yet...
			if (h * rigidbody2D.velocity.x < maxSpeed)
				// ... add a force to the player.
				rigidbody2D.AddForce (Vector2.right * h * moveForce);

			// If the player's horizontal velocity is greater than the maxSpeed...
			if (Mathf.Abs (rigidbody2D.velocity.x) > maxSpeed)
				// ... set the player's velocity to the maxSpeed in the x axis.
				rigidbody2D.velocity = new Vector2 (Mathf.Sign (rigidbody2D.velocity.x) * maxSpeed, rigidbody2D.velocity.y);
		
			// If the input is moving the player right and the player is facing left...
			if (h > 0 && !facingRight)
				// ... flip the player.
				Flip ();
				// Otherwise if the input is moving the player left and the player is facing right...
			else if (h < 0 && facingRight)
				// ... flip the player.
				Flip ();
		
		}
			
		if (run && currentBaseState.IsName("walk"))
		{
			m_anim.SetTrigger("t_run");
			rigidbody2D.AddForce(Vector2.right * h * runForce);
			run = false;
		}

		// If the player should jump...
		if(jump)
		{
			// Set the Jump animator trigger parameter.
			m_anim.SetTrigger("t_jump");

			// Add a vertical force to the player.
			rigidbody2D.AddForce(new Vector2(0f, jumpForce));

			// Make sure the player can't jump again until the jump conditions from Update are satisfied.
			jump = false;
		}
	}
	
	
	void Flip ()
	{
		// Switch the way the player is labelled as facing.
		facingRight = !facingRight;

		// Multiply the player's x local scale by -1.

		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
