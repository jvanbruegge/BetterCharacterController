using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class FootController : MonoBehaviour
{
    [SerializeField]
    private float standAngle = 45.0f;
    [SerializeField]
    private float stepHeight = 0.3f;

    private const float tolerance = 0.05f;
    private const float tinyTolerance = 0.01f;

    private SphereCollider ownCollider;
    private int layerMask;
    private float smallerRadius;

	private float currentSpeed = 0;

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
		bool isGrounded = ProbeGround(out hit);

        DebugDraw.DrawMarker(hit.point, 0.25f, Color.red, 1);

		if(!lifted)
		{
			if (isGrounded)
			{
				ClampPlayer(hit);
			}
			else
			{
				PerformGravity();
			}
		}

        this.gameObject.layer = 0;
    }

	private void PerformGravity()
	{
		currentSpeed += CurrentGravity.gravity * Time.deltaTime;

		RaycastHit hit;
		if (!Physics.Raycast(Position, GravityDirection, out hit, currentSpeed, layerMask))
		{
			transform.parent.position += GravityDirection * currentSpeed;
		}
		else
		{
			transform.parent.position = hit.point + transform.parent.up * ownCollider.radius - transform.localPosition;
			currentSpeed = 0;
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
        if ((hit.point - Position).magnitude * (1 - tinyTolerance) < ownCollider.radius) return;

		transform.parent.position += transform.parent.up * (hit.point.y - getY(hit.point));
    }

    private bool ProbeGround(out RaycastHit raycastHit)
    {
        RaycastHit hit;

		if (Physics.Raycast(Position, -transform.parent.up, out hit, stepHeight + tolerance, layerMask))
		{
			if ((hit.point - Position).magnitude > ownCollider.radius + stepHeight + tinyTolerance)
			{
				raycastHit = hit;
				return false;
			}
		}

		if(Physics.SphereCast(Position, smallerRadius, -transform.up, out hit, stepHeight + tolerance, layerMask))
		{
			SimulateSphereCast(hit.normal, out hit);
		}

		raycastHit = hit;

		return hit.point.y - getY(hit.point) <= stepHeight;
    }

    private float getY(Vector3 hit)
    {
        return -Mathf.Sqrt(Mathf.Pow(ownCollider.radius, 2)
                           - Mathf.Pow(hit.x - Position.x, 2)
                           - Mathf.Pow(hit.z - Position.z, 2)
                ) + Position.y + ownCollider.center.y;
    }

    /// <summary>
    /// Provides raycast data based on where a SphereCast would contact the specified normal
    /// Raycasting downwards from a point along the controller's bottom sphere, based on the provided
    /// normal
    /// </summary>
    /// <param name="groundNormal">Normal of a triangle assumed to be directly below the controller</param>
    /// <param name="hit">Simulated SphereCast data</param>
    /// <returns>True if the raycast is successful</returns>
    private bool SimulateSphereCast(Vector3 groundNormal, out RaycastHit hit)
    {
        float groundAngle = Vector3.Angle(groundNormal, transform.parent.up) * Mathf.Deg2Rad;

        Vector3 secondaryOrigin = Position + transform.parent.up * tolerance;

        if (!Mathf.Approximately(groundAngle, 0))
        {
            float horizontal = Mathf.Sin(groundAngle) * ownCollider.radius;
            float vertical = (1.0f - Mathf.Cos(groundAngle)) * ownCollider.radius;

            // Retrieve a vector pointing up the slope
            Vector3 r2 = Vector3.Cross(groundNormal, -transform.parent.up);
            Vector3 v2 = -Vector3.Cross(r2, groundNormal);

            secondaryOrigin += Math3d.ProjectVectorOnPlane(transform.parent.up, v2).normalized * horizontal + transform.parent.up * vertical;
        }

        if (Physics.Raycast(secondaryOrigin, -transform.parent.up, out hit, Mathf.Infinity, layerMask))
        {
            // Remove the tolerance from the distance travelled
            hit.distance -= tolerance;

            return true;
        }
        else
        {
            return false;
        }
    }
}
