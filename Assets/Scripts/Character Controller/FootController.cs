using UnityEngine;

using System.Collections.Generic;

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

    private float smallerRadius;

	private Vector3 currentSpeed = Vector3.zero;
	private GroundType currentGroundType = GroundType.Airborn;

	public Gravity CurrentGravity { get; set; }

	private Vector3 GravityDirection
	{
		get { return (CurrentGravity.transform.position - Position).normalized; }
	}

    protected override void Awake()
    {
		base.Awake();

        // Reduce our radius by tolerance squared to avoid failing the SphereCast due to clipping with walls
        this.smallerRadius = ownCollider.radius - (tolerance * tolerance);
    }

	private void Start()
	{
		this.CurrentGravity = Parent.GetComponentInChildren<Gravity>();
	}

    private void Update()
    {
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
    }

	private void PerformGravity(RaycastHit raycastHit, Vector3 gravityDirection)
	{
		currentSpeed = nextSpeed(currentSpeed, gravityDirection);

		RaycastHit hit;
		if(Physics.SphereCast(Position, ownCollider.radius, currentSpeed, out hit, currentSpeed.magnitude + tolerance, layerMask))
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
		RaycastHit hit;
		if (Physics.SphereCast(Position - Movement.normalized * tolerance, ownCollider.radius, Movement, out hit, Movement.magnitude * Time.deltaTime + tolerance, layerMask))
		{
			RaycastHit wallHit = getWallHit(hit);
			RaycastHit floorHit = getFloorHit(wallHit);

			if((hit.point - floorHit.point).magnitude <= stepHeight + tolerance)
			{
				this.movementController.addVeto(60, getNewPosition(hit));
				return true;
			}
		}

		return false;
    }

    private void ClampPlayer(RaycastHit hit)
    {
		Vector3 newPosition = getNewPosition(hit);

		if (hit.normal != -GravityDirection)
		{
			RaycastHit floorHit = getFloorHit(getWallHit(hit));
			Vector3 newCenter = newPosition + transform.localPosition + ownCollider.center;

			Vector3 height = Vector3.Project(floorHit.point - hit.point, GravityDirection);
			Vector3 v = Vector3.Project(hit.point - newCenter, GravityDirection);

			if ((height + v).magnitude < ownCollider.radius - tinyTolerance)
			{
				newPosition -= GravityDirection * (ownCollider.radius - (height + v).magnitude);
			}
		}

		if(!(float.IsNaN(newPosition.x) && float.IsNaN(newPosition.y) && float.IsNaN(newPosition.z)))
		{
			this.movementController.addVeto(50, newPosition);
		}
    }

    private GroundType ProbeGround(out RaycastHit raycastHit, out Vector3 gravityDirection)
    {
		gravityDirection = GravityDirection;

		if(Physics.SphereCast(Position - GravityDirection * tolerance, ownCollider.radius, GravityDirection, out raycastHit, tolerance * 2 + currentSpeed.magnitude * Time.deltaTime, layerMask)) //Check if we are colliding at all
		{
			if (Vector3.Angle(raycastHit.normal, -GravityDirection) > standAngle) //check if we can stand on the surface we are on
			{
				RaycastHit hit = getWallHit(raycastHit); //Get normal of the wall we are about to slide down
				RaycastHit floorHit = getFloorHit(hit);

				Vector3 raycastCross = Vector3.Cross(GravityDirection, raycastHit.normal);
				Vector3 slideDirection = pointTowards(Vector3.Cross(raycastHit.normal, raycastCross), GravityDirection);

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

	private Vector3 nextSpeed(Vector3 currentSpeed, Vector3 gravityDirection)
	{
		Vector3 newSpeed = gravityDirection * CurrentGravity.gravity * Time.deltaTime;
		if (currentGroundType == GroundType.Sliding)
		{
			newSpeed /= SlideSlowing;
		}
		return currentSpeed + newSpeed;
	}

	private Vector3 getNewPosition(RaycastHit hit)
	{
		Vector3 newPos = Position + Movement * Time.deltaTime;
		Vector3 a = Vector3.ProjectOnPlane(hit.point - newPos, -GravityDirection);
		Vector3 f = hit.point - (a + newPos);

		float b = Mathf.Sqrt(Mathf.Pow(ownCollider.radius, 2) - a.sqrMagnitude);

		return Parent.position + Movement * Time.deltaTime - GravityDirection * (b - f.magnitude);
	}
}
