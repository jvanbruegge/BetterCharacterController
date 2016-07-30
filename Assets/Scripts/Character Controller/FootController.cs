using UnityEngine;

public enum GroundType
{
	Airborn,
	Sliding,
	Grounded
}

[RequireComponent(typeof(SphereCollider))]
public class FootController : MonoBehaviour
{
    [SerializeField]
    private float standAngle = 30.0f;
    [SerializeField]
    private float stepHeight = 0.3f;
	[SerializeField]
	private float slideSlowing = 10;

    private const float tolerance = 0.05f;
    private const float tinyTolerance = 0.01f;

    private SphereCollider ownCollider;
    private int layerMask;
    private float smallerRadius;

	private Vector3 currentSpeed = Vector3.zero;
	private GroundType currentGroundType = GroundType.Airborn;

	public Gravity CurrentGravity { get; set; }

	private Vector3 Position
	{
		get { return transform.parent.position + transform.localPosition + ownCollider.center;  }
	}

	private Vector3 GravityDirection
	{
		get { return (CurrentGravity.transform.position - Position).normalized; }
	}

    private void Awake()
    {
        this.ownCollider = GetComponent<SphereCollider>();
		this.CurrentGravity = transform.parent.GetComponentInChildren<Gravity>();

        this.layerMask = ~(1 << 8);

        // Reduce our radius by tolerance squared to avoid failing the SphereCast due to clipping with walls
        this.smallerRadius = ownCollider.radius - (tolerance * tolerance);
    }

    private void FixedUpdate()
    {
        this.gameObject.layer = 8;

		bool lifted = LiftPlayer();

		RaycastHit hit;
		Vector3 gravityDirection;
		GroundType groundType = ProbeGround(out hit, out gravityDirection);

		if(currentGroundType == GroundType.Airborn && groundType != GroundType.Airborn)
		{
			currentSpeed = gravityDirection * 0.1f;
		}
		currentGroundType = groundType;

		if(!lifted)
		{
			if (groundType == GroundType.Grounded)
			{
				ClampPlayer(hit);
			}
			else
			{
				PerformGravity(hit, gravityDirection);
			}
		}

        this.gameObject.layer = 0;
    }

	private void PerformGravity(RaycastHit raycastHit, Vector3 gravityDirection)
	{
		Vector3 newSpeed = gravityDirection * CurrentGravity.gravity * Time.deltaTime;
		if(currentGroundType == GroundType.Sliding)
		{
			newSpeed /= slideSlowing;
		}
		currentSpeed += newSpeed;

		RaycastHit hit;
		if(Physics.SphereCast(Position, ownCollider.radius, currentSpeed, out hit, currentSpeed.magnitude + tolerance, layerMask))
		{
			Vector3 middle = hit.point + hit.normal * ownCollider.radius;

			transform.parent.position = middle - transform.localPosition - ownCollider.center;
		}
		else
		{
			transform.parent.position += currentSpeed;
		}
	}

    private bool LiftPlayer()
    {
        Collider[] colliders = Physics.OverlapSphere(Position, smallerRadius, layerMask);

        if (colliders.Length > 0)
        {
            Vector3 closestPoint = CollisionHelper.ClosestPointOnSurface(colliders[0], Position, smallerRadius);

            for (int i = 1; i < colliders.Length; i++)
            {
                Vector3 newPoint = CollisionHelper.ClosestPointOnSurface(colliders[i], Position, smallerRadius);
                if ((newPoint - Position).magnitude < (Position - closestPoint).magnitude)
                {
                    closestPoint = newPoint;
                }
            }

			if(closestPoint.y - (Position - transform.parent.up * ownCollider.radius).y <= stepHeight + tinyTolerance)
			{
				transform.parent.position += transform.parent.up * (closestPoint.y - getY(closestPoint));
				return true;
			}
        }

		return false;
    }

    private void ClampPlayer(RaycastHit hit)
    {
		transform.parent.position += transform.parent.up * (hit.point.y - getY(hit.point));
    }

    private GroundType ProbeGround(out RaycastHit raycastHit, out Vector3 gravityDirection)
    {
		gravityDirection = GravityDirection;

		if(Physics.SphereCast(Position - GravityDirection * tolerance, ownCollider.radius, GravityDirection, out raycastHit, currentSpeed.magnitude + tolerance, layerMask)) //Check if we are colliding at all
		{
			DebugDraw.DrawVector(raycastHit.point, -GravityDirection, 1, 0.25f, Color.cyan, 1);
			DebugDraw.DrawVector(raycastHit.point, raycastHit.normal, 1, 0.25f, Color.blue, 1);

			if (Vector3.Angle(raycastHit.normal, -GravityDirection) > standAngle) //check if we can stand on the surface we are on
			{
				RaycastHit hit;
				Vector3 normalPit = raycastHit.point + raycastHit.normal;
				Physics.Raycast(normalPit, raycastHit.point + GravityDirection * tinyTolerance - normalPit, out hit, 2, layerMask); //Get normal of the wall we are about to slide down
				Vector3 cross = Vector3.Cross(GravityDirection, hit.normal);
				Vector3 wallDirection = Vector3.Cross(hit.normal, cross);
				wallDirection = (wallDirection + GravityDirection).magnitude > (-wallDirection + GravityDirection).magnitude ? wallDirection : -wallDirection; //Make sure we point downwards

				DebugDraw.DrawVector(hit.point, hit.normal, 1, 0.25f, Color.red, 1);
				DebugDraw.DrawVector(hit.point, wallDirection, 1, 0.25f, Color.green, 1);

				RaycastHit floorHit;
				Physics.Raycast(hit.point + hit.normal * tinyTolerance, wallDirection, out floorHit, Mathf.Infinity, layerMask);

				DebugDraw.DrawMarker(floorHit.point, 0.25f, Color.red, 1);
				DebugDraw.DrawMarker(raycastHit.point, 0.25f, Color.black, 1);

				if ((floorHit.point - raycastHit.point).magnitude > stepHeight + tolerance)
				{
					gravityDirection = wallDirection.normalized;
					return GroundType.Sliding;
				}
			}
			return GroundType.Grounded;
		}
		return GroundType.Airborn;
    }

    private float getY(Vector3 hit)
    {
        return -Mathf.Sqrt(Mathf.Pow(ownCollider.radius, 2)
                           - Mathf.Pow(hit.x - Position.x, 2)
                           - Mathf.Pow(hit.z - Position.z, 2)
                ) + Position.y + ownCollider.center.y;
    }
}
