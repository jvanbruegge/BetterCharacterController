using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Controller : MonoBehaviour
{
	private MovementController movementController;

	protected Vector3 Movement
	{
		get { return movementController.Movement; }
	}

	protected Transform Parent
	{
		get { return movementController.transform; }
	}

	private void Awake()
	{
		this.movementController = GetComponentInParent<MovementController>();
	}
}