using UnityEngine;

public class BodyController : Controller
{
	private void Update()
	{
		/*RaycastHit hit;
		if (Physics.SphereCast(Position - Movement.normalized * tolerance, ownCollider.radius, Movement, out hit, Movement.magnitude * Time.deltaTime + tolerance, layerMask))
		{
			int weight = Vector3.Angle(Parent.transform.up, hit.normal) >= 90 ? 90 : 40; // Higher priority if upper half of sphere

			Vector3 wallDirection = Vector3.zero;

			if(Movement.normalized + hit.normal != Vector3.zero && Movement.normalized - hit.normal != Vector3.zero)
			{
				wallDirection = pointTowards(Vector3.Cross(Parent.up, hit.normal), Movement).normalized;
			}

			this.movementController.addVeto(weight, Parent.position + wallDirection * Movement.magnitude * Time.deltaTime);
		}*/
	}
}