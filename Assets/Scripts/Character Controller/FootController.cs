using UnityEngine;

public enum GroundType
{
	Airborn,
	Sliding,
	Grounded
}

public class FootController : Controller
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
		get { return Parent.position + transform.localPosition + ownCollider.center;  }
	}

	private Vector3 GravityDirection
	{
		get { return (CurrentGravity.transform.position - Position).normalized; }
	}

    protected override void Awake()
    {
		base.Awake();
        this.ownCollider = GetComponent<SphereCollider>();

        this.layerMask = ~(1 << 8);

        // Reduce our radius by tolerance squared to avoid failing the SphereCast due to clipping with walls
        this.smallerRadius = ownCollider.radius - (tolerance * tolerance);
    }

	private void Start()
	{
		this.CurrentGravity = Parent.GetComponentInChildren<Gravity>();
	}

    private void Update()
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
		currentSpeed = nextSpeed(currentSpeed, gravityDirection);

		RaycastHit hit;
		if(Physics.SphereCast(Position, ownCollider.radius, currentSpeed, out hit, currentSpeed.magnitude * Time.deltaTime + tolerance, layerMask))
		{
			Vector3 middle = hit.point + hit.normal * ownCollider.radius;
			Vector3 newPosition = middle - transform.localPosition - ownCollider.center;

			this.movementController.addVeto(50, newPosition);
		}
		else
		{
			this.movementController.addVeto(20, currentSpeed + Parent.position);
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

			if(closestPoint.y - (Position - Parent.up * ownCollider.radius).y <= stepHeight + tinyTolerance)
			{
				this.movementController.addVeto(60, Parent.up * (closestPoint.y - getY(closestPoint)) + Movement * Time.deltaTime + Parent.position);
				return true;
			}
        }

		return false;
    }

    private void ClampPlayer(RaycastHit hit)
    {
		this.movementController.addVeto(10, Parent.up * (hit.point.y - getY(hit.point)) + Movement * Time.deltaTime + Parent.position);
    }

    private GroundType ProbeGround(out RaycastHit raycastHit, out Vector3 gravityDirection)
    {
		gravityDirection = GravityDirection;

		if(Physics.SphereCast(
			Position - GravityDirection * tolerance, ownCollider.radius, GravityDirection, out raycastHit, nextSpeed(currentSpeed, GravityDirection).magnitude * Time.deltaTime + tolerance * 2, layerMask)
		) //Check if we are colliding at all
		{
			DebugDraw.DrawVector(raycastHit.point, -GravityDirection, 1, 0.25f, Color.cyan, 1);
			DebugDraw.DrawVector(raycastHit.point, raycastHit.normal, 1, 0.25f, Color.blue, 1);

			if (Vector3.Angle(raycastHit.normal, -GravityDirection) > standAngle) //check if we can stand on the surface we are on
			{
				RaycastHit hit;
				Vector3 normalPit = raycastHit.point + raycastHit.normal;
				Physics.Raycast(normalPit, raycastHit.point + GravityDirection * tinyTolerance - normalPit, out hit, 2, layerMask); //Get normal of the wall we are about to slide down
				Vector3 cross = Vector3.Cross(GravityDirection, hit.normal);
				Vector3 wallDirection = pointDown(Vector3.Cross(hit.normal, cross), GravityDirection);

				DebugDraw.DrawVector(hit.point, hit.normal, 1, 0.25f, Color.red, 1);
				DebugDraw.DrawVector(hit.point, wallDirection, 1, 0.25f, Color.green, 1);

				RaycastHit floorHit;
				Physics.Raycast(hit.point + hit.normal * tinyTolerance, wallDirection, out floorHit, Mathf.Infinity, layerMask);

				Vector3 raycastCross = Vector3.Cross(GravityDirection, raycastHit.normal);
				Vector3 slideDirection = pointDown(Vector3.Cross(raycastHit.normal, raycastCross), GravityDirection);

				if ((floorHit.point - raycastHit.point).magnitude > stepHeight + tolerance)
				{
					gravityDirection = slideDirection.normalized;
					return GroundType.Sliding;
				}
			}
			return GroundType.Grounded;
		}
		return GroundType.Airborn;
    }

	private Vector3 pointDown(Vector3 vector, Vector3 gravityDirection)
	{
		return (vector + gravityDirection).magnitude > (-vector + gravityDirection).magnitude ? vector : -vector;
	}

	private Vector3 nextSpeed(Vector3 currentSpeed, Vector3 gravityDirection)
	{
		Vector3 newSpeed = gravityDirection * CurrentGravity.gravity * Time.deltaTime;
		if (currentGroundType == GroundType.Sliding)
		{
			newSpeed /= slideSlowing;
		}
		return currentSpeed + newSpeed;
	}

    private float getY(Vector3 hit)
    {
        return -Mathf.Sqrt(Mathf.Pow(ownCollider.radius, 2)
                           - Mathf.Pow(hit.x - Position.x, 2)
                           - Mathf.Pow(hit.z - Position.z, 2)
                ) + Position.y + ownCollider.center.y;
    }
}
