using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Controller : MonoBehaviour
{
	protected MovementController movementController;

	protected Vector3 Movement
	{
		get { return movementController.Movement; }
	}

	protected Transform Parent
	{
		get { return movementController.transform; }
	}

	protected virtual void Awake()
	{
		this.movementController = GetComponentInParent<MovementController>();
	}
}