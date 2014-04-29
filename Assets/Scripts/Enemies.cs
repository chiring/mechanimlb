using UnityEngine;
using System.Collections;

public class Enemies: MonoBehaviour
{
	public int HP = 1;					// How many times the enemy can be hit before it dies.
	public float moveSpeed = 2f;		// The speed the enemy moves at.




	public Sprite deadEnemy;			// A sprite of the enemy when it's dead.
	public Sprite damagedEnemy;			// An optional sprite of the enemy when it's damaged.


	private Transform frontCheck;		// Reference to the position of the gameobject used for checking if something is in front.
	private SpriteRenderer ren;			// Reference to the sprite renderer.
	private bool dead = false;			// Whether or not the enemy is dead.

	
	void Awake()
	{
		ren = transform.Find("enem").GetComponent<SpriteRenderer>();
		frontCheck = transform.Find("frontCheck").transform;

	}
	
	void FixedUpdate ()
	{
		// Create an array of all the colliders in front of the enemy.
		Collider2D[] frontHits = Physics2D.OverlapPointAll(frontCheck.position, 1);
		
		// Check each of the colliders.
		foreach(Collider2D c in frontHits)
		{
			// If any of the colliders is an Obstacle...
			if(c.tag == "Obstacle")
			{
				// ... Flip the enemy and stop checking the other colliders.
				Flip ();
				break;
			}
		}

		// Set the enemy's velocity to moveSpeed in the x direction.
		rigidbody2D.velocity = new Vector2(transform.localScale.x * moveSpeed, rigidbody2D.velocity.y);	

		// If the enemy has zero or fewer hit points and isn't dead yet...
		if(HP <= 0 && !dead)
			// ... call the death function.
			Death ();
	}
	
	public void Hurt()
	{
		// Reduce the number of hit points by one.
		HP--;
		Debug.Log (HP);

		// ... set the sprite renderer's sprite to be the damagedEnemy sprite.
		ren.sprite = damagedEnemy;

	}
	
	void Death()
	{

		// Find all of the sprite renderers on this object and it's children.
		SpriteRenderer[] otherRenderers = GetComponentsInChildren<SpriteRenderer>();
		
		// Disable all of them sprite renderers.
		foreach(SpriteRenderer s in otherRenderers)
		{
			s.enabled = false;
		}
		
		// Set dead to true.
		dead = true;
		
		// Find all of the colliders on the gameobject and set them all to be triggers.
		Collider2D[] cols = GetComponents<Collider2D>();
		foreach(Collider2D c in cols)
		{
			c.isTrigger = true;
		}

	}
	
	
	public void Flip()
	{
		// Multiply the x component of localScale by -1.
		Vector3 enemyScale = transform.localScale;
		enemyScale.x *= -1;
		transform.localScale = enemyScale;
	}
}
