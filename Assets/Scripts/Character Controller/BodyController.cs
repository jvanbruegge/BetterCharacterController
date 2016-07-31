using System.Linq;
using UnityEngine;

public class BodyController : Controller
{
	private float smallerRadius;

	protected override void Awake()
	{
		base.Awake();
		this.smallerRadius = ownCollider.radius - tolerance * tolerance;
	}

	private void Update()
	{
		Pushback();
		SweepTest();
	}

	private void Pushback() //TODO: Refine, remove if possible
	{
		Collider[] colliders = Physics.OverlapSphere(Position, ownCollider.radius - tolerance, layerMask);

		if(colliders.Length > 0)
		{
			Vector3 newPosition = Position + Movement * Time.deltaTime;

			Vector3 closestPoint = colliders.Aggregate(newPosition + transform.up * ownCollider.radius * 2, (acc, curr) =>	{
				Vector3 newPoint = CollisionHelper.ClosestPointOnSurface(curr, newPosition, ownCollider.radius);
				return (acc - newPosition).magnitude > (newPoint - newPosition).magnitude ? newPoint : acc;
			});

			this.movementController.addVeto(100, closestPoint + (newPosition - closestPoint).normalized * ownCollider.radius - transform.localPosition - ownCollider.center);
		}
	}

	private void SweepTest()
	{
		RaycastHit hit;
		if (Physics.SphereCast(Position - Movement.normalized * tolerance, smallerRadius, Movement, out hit, Movement.magnitude * Time.deltaTime + tolerance, layerMask))
		{
			int weight = Vector3.Angle(Parent.transform.up, hit.normal) >= 90 - tinyTolerance ? 90 : 40; // Higher priority if upper half of sphere

			RaycastHit wallHit = getWallHit(hit);

			Vector3 wallDirection = Vector3.zero;

			if (Vector3.Angle(wallHit.normal, -Movement.normalized) > 10)
			{
				wallDirection = pointTowards(Vector3.Cross(Parent.up, wallHit.normal), Movement).normalized;
			}

			this.movementController.addVeto(weight, Parent.position + wallDirection * Movement.magnitude * Time.deltaTime);
		}
	}
}