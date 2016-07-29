using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class FootController : MonoBehaviour
{
    [SerializeField]
    private float standAngle = 30.0f;
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
		Debug.DrawRay(hit.point, hit.normal, Color.blue, 1);

		if(!lifted)
		{
			if (isGrounded)
			{
				ClampPlayer(hit);
			}
			else
			{
				PerformGravity(hit);
			}
		}

        this.gameObject.layer = 0;
    }

	private void PerformGravity(RaycastHit raycastHit)
	{
		currentSpeed += CurrentGravity.gravity * Time.deltaTime;

		if(Vector3.Angle(raycastHit.normal, -GravityDirection) > standAngle)
		{
			Vector3 planeNormal = Vector3.Cross(raycastHit.normal, -GravityDirection);
			Vector3 pDirection = Vector3.Cross(planeNormal, raycastHit.normal);
			Vector3 nDirection = -pDirection;
			Vector3 direction = (GravityDirection + pDirection).magnitude < (GravityDirection + nDirection).magnitude ? nDirection : pDirection;

			transform.parent.position += direction.normalized * currentSpeed;
		}
		else
		{
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
		if(Physics.SphereCast(Position, ownCollider.radius, GravityDirection, out raycastHit, currentSpeed + tolerance, layerMask)) //Check if we are colliding at all
		{
			if(Vector3.Angle(raycastHit.normal, -GravityDirection) > standAngle) //check if we can stand on the surface we are on
			{
				RaycastHit hit;
				Physics.Raycast(raycastHit.point + raycastHit.normal, raycastHit.point - GravityDirection * tinyTolerance, out hit, 2, layerMask); //Get normal of the wall we are about to slide down
				Vector3 cross = Vector3.Cross(raycastHit.normal, hit.normal);
				Vector3 wallDirection = Vector3.Cross(hit.normal, cross);
				wallDirection = (wallDirection + GravityDirection).magnitude > (-wallDirection + GravityDirection).magnitude ? wallDirection : -wallDirection; //Make sure we point downwards

				RaycastHit floorHit;
				Physics.Raycast(hit.point + hit.normal * tinyTolerance, wallDirection, out floorHit, Mathf.Infinity, layerMask);
				if((floorHit.point - raycastHit.point).magnitude > stepHeight)
				{
					return false;
				}
			}
			return true;
		}
		return false;
    }

    private float getY(Vector3 hit)
    {
        return -Mathf.Sqrt(Mathf.Pow(ownCollider.radius, 2)
                           - Mathf.Pow(hit.x - Position.x, 2)
                           - Mathf.Pow(hit.z - Position.z, 2)
                ) + Position.y + ownCollider.center.y;
    }
}
