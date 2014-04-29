/* Copyright (c) 2014 Advanced Platformer 2D */

using UnityEngine;

[AddComponentMenu("Advanced Platformer 2D/Hitable")]
public class APHitable : MonoBehaviour 
{
    // called when we have been hit by a melee attack
	// - launcher : character controller launching the attack
	// - hitZone : hitzone detecting the hit
	virtual public void OnMeleeAttackHit(APCharacterController launcher, APHitZone hitZone) {}


	// called when character is touching us
	// - motor : character controller touching the hitable
	// - rayType : type of ray detecting the hit
	virtual public void OnCharacterTouch(APCharacterController launcher, APCharacterMotor.RayType rayType) {}
}

