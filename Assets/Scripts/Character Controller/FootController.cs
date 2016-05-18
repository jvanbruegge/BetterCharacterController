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

    private void Awake()
    {
        this.ownCollider = GetComponent<SphereCollider>();
        this.layerMask = ~(1 << 8);

        // Reduce our radius by tolerance squared to avoid failing the SphereCast due to clipping with walls
        this.smallerRadius = ownCollider.radius - (tolerance * tolerance);
    }

    private void LateUpdate()
    {
        this.gameObject.layer = 8;

        LiftPlayer();

        RaycastHit hit = ProbeGround();

        DebugDraw.DrawMarker(hit.point, 0.25f, Color.red, 1);

        bool isGrounded = ClampPlayer(hit);

        this.gameObject.layer = 0;
    }

    private void LiftPlayer()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position + ownCollider.center, smallerRadius, layerMask);

        if (colliders.Length > 0)
        {
            Vector3 closestPoint = CollisionHelper.ClosestPointOnSurface(colliders[0], transform.position + ownCollider.center, smallerRadius);

            for (int i = 1; i < colliders.Length; i++)
            {
                Vector3 newPoint = CollisionHelper.ClosestPointOnSurface(colliders[i], transform.position + ownCollider.center, smallerRadius);
                if ((newPoint - transform.position).magnitude < (transform.position - closestPoint).magnitude)
                {
                    closestPoint = newPoint;
                }
            }

            transform.position += transform.up * (closestPoint.y - getY(closestPoint));
        }
    }

    private bool ClampPlayer(RaycastHit hit)
    {
        if ((hit.point - transform.position - ownCollider.center).magnitude * (1 - tinyTolerance) < ownCollider.radius) return true;

        

        transform.position += transform.up * (hit.point.y - getY(hit.point));

        return true;
    }

    private RaycastHit ProbeGround()
    {
        RaycastHit hit;

        Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity);

        return hit;
    }

    private float getY(Vector3 hit)
    {
        return -Mathf.Sqrt(Mathf.Pow(ownCollider.radius, 2)
                           - Mathf.Pow(hit.x - transform.position.x - ownCollider.center.x, 2)
                           - Mathf.Pow(hit.z - transform.position.z - ownCollider.center.z, 2)
                ) + transform.position.y + ownCollider.center.y;
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
        float groundAngle = Vector3.Angle(groundNormal, transform.up) * Mathf.Deg2Rad;

        Vector3 secondaryOrigin = transform.position + transform.up * tolerance;

        if (!Mathf.Approximately(groundAngle, 0))
        {
            float horizontal = Mathf.Sin(groundAngle) * ownCollider.radius;
            float vertical = (1.0f - Mathf.Cos(groundAngle)) * ownCollider.radius;

            // Retrieve a vector pointing up the slope
            Vector3 r2 = Vector3.Cross(groundNormal, -transform.up);
            Vector3 v2 = -Vector3.Cross(r2, groundNormal);

            secondaryOrigin += Math3d.ProjectVectorOnPlane(transform.up, v2).normalized * horizontal + transform.up * vertical;
        }

        if (Physics.Raycast(secondaryOrigin, -transform.up, out hit, Mathf.Infinity, layerMask))
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
