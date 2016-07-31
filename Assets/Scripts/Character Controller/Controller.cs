using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Controller : MonoBehaviour
{
	protected const float tolerance = 0.05f;
	protected const float tinyTolerance = 0.01f;

	protected SphereCollider ownCollider;
	protected int layerMask;
	protected MovementController movementController;

	protected float SlideSlowing
	{
		get { return movementController.SlideSlowing; }
	}

	protected Vector3 Movement
	{
		get { return movementController.Movement; }
	}

	protected Transform Parent
	{
		get { return movementController.transform; }
	}

	protected Vector3 Position
	{
		get { return Parent.position + transform.localPosition + ownCollider.center; }
	}

	protected virtual void Awake()
	{
		this.layerMask = ~(1 << 8);

		this.ownCollider = GetComponent<SphereCollider>();
		this.movementController = GetComponentInParent<MovementController>();
	}

	protected Vector3 pointTowards(Vector3 vector, Vector3 direction)
	{
		return (vector + direction).magnitude > (-vector + direction).magnitude ? vector : -vector;
	}

	protected RaycastHit getWallHit(RaycastHit originalHit)
	{
		RaycastHit hit;
		Vector3 normalPit = originalHit.point + originalHit.normal;
		Physics.Raycast(normalPit, originalHit.point - Parent.up * tinyTolerance - normalPit, out hit, 2, layerMask);
		return hit;
	}

	protected RaycastHit getFloorHit(RaycastHit wallHit)
	{
		Vector3 cross = Vector3.Cross(-Parent.up, wallHit.normal);
		Vector3 floorDirection = pointTowards(Vector3.Cross(wallHit.normal, cross), -Parent.up);
		RaycastHit floorHit;
		Physics.Raycast(wallHit.point + wallHit.normal * tinyTolerance, floorDirection, out floorHit, Mathf.Infinity, layerMask);
		return floorHit;
	}
}