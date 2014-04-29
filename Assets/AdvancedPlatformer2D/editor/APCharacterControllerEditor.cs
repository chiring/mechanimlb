using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(APCharacterController))]
[CanEditMultipleObjects]
public class APCharacterControllerEditor : Editor
{
	public void OnSceneGUI () 
	{
		APCharacterController oController = (APCharacterController)target;

		// draw all melee attack zones and allow to move them when selecting controller
		if (oController.m_meleeAttacks.m_enabled)
		{
			foreach(APMeleeAttack curAttack in oController.m_meleeAttacks.m_attacks)
			{
				foreach (APHitZone curZone in curAttack.m_hitZones)
				{
					if (curZone.m_active && curZone.gameObject.activeInHierarchy)
					{
						Vector3 pointPos = curZone.transform.position;
						Color color = Color.green;
						color.a = 0.5f;
						Handles.color = color;
						Vector3 newPos = Handles.FreeMoveHandle(pointPos, Quaternion.identity, curZone.m_radius * 2f, Vector3.zero, Handles.SphereCap);
						if (newPos != pointPos)
						{
							Undo.RecordObject(curZone.transform, "Move Hit Zone");
							curZone.transform.position = newPos;

							// mark object as dirty
							EditorUtility.SetDirty(curZone);
						}
					}
				}
			}
		}
	}
}
