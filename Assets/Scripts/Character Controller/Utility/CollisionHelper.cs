using UnityEngine;

public static class CollisionHelper
{
    private const float resolution = 0.1f;

    public static Vector3 ClosestPointOnSurface(Collider collider, Vector3 to, float radius)
    {
        if (collider is BoxCollider)
        {
            return CollisionHelper.ClosestPointOnSurface((BoxCollider)collider, to);
        }
        else if (collider is SphereCollider)
        {
            return CollisionHelper.ClosestPointOnSurface((SphereCollider)collider, to);
        }
        else if (collider is CapsuleCollider)
        {
            return CollisionHelper.ClosestPointOnSurface((CapsuleCollider)collider, to);
        }
        else if (collider is TerrainCollider)
        {
            return CollisionHelper.ClosestPointOnSurface(((TerrainCollider)collider), to, radius);
        }
        else if (collider is MeshCollider)
        {
            BSPTree bsp = collider.GetComponent<BSPTree>();

            return bsp.ClosestPointOn(to, radius);
        }

        return Vector3.zero;
    }

    public static Vector3 ClosestPointOnSurface(TerrainCollider collider, Vector3 to, float radius)
    {
        float[,] values = new float[(int)(radius * 2 / resolution), (int)(radius * 2 / resolution)];
        Terrain terrain = collider.GetComponent<Terrain>();

        for (float x = to.x - radius, i = 0; x <= to.x + radius && i < values.GetLength(0); x += resolution, i++)
        {
            for (float z = to.z - radius, j = 0; z <= to.z + radius && j < values.GetLength(0); z += resolution, j++)
            {
                values[(int)i, (int)j] = terrain.SampleHeight(new Vector3(x, to.y, z));
            }
        }

        Vector3 a = Vector3.zero;
        Vector3 b = Vector3.zero;
        Vector3 c = Vector3.zero;

        Vector3 shortest = to + Vector3.up * radius * 2;

        for (int i = 0; i < values.GetLength(0); i++)
        {
            for (int j = 1; j < values.GetLength(0); j++)
            {
                a.x = to.x - radius + i * resolution;
                a.y = values[i, j - 1];
                a.z = to.z - radius + (j - 1) * resolution;

                b.x = to.x - radius + i * resolution;
                b.y = values[i, j];
                b.z = to.z - radius + j * resolution;

                if (i % 2 == 0)
                {
                    c.x = to.x - radius + (i + 1) * resolution;
                    c.y = values[i, j - 1];
                    c.z = to.z - radius + (j - 1) * resolution;
                }
                else
                {
                    c.x = to.x - radius + (i - 1) * resolution;
                    c.y = values[i, j];
                    c.z = to.z - radius + j * resolution;
                }

                Vector3 newShortest = Math3d.ClosestPointOnTriangleToPoint(ref a, ref b, ref c, ref to);

                if ((to - newShortest).magnitude < (to - shortest).magnitude)
                {
                    shortest = newShortest;
                }
            }
        }

        return shortest;
    }

    public static Vector3 ClosestPointOnSurface(SphereCollider collider, Vector3 to)
    {
        Vector3 p;

        p = to - collider.transform.position;
        p.Normalize();

        p *= collider.radius * collider.transform.localScale.x;
        p += collider.transform.position;

        return p;
    }

    public static Vector3 ClosestPointOnSurface(BoxCollider collider, Vector3 to)
    {
        // Cache the collider transform
        var ct = collider.transform;

        // Firstly, transform the point into the space of the collider
        var local = ct.InverseTransformPoint(to);

        // Now, shift it to be in the center of the box
        local -= collider.center;

        //Pre multiply to save operations.
        var halfSize = collider.size * 0.5f;

        // Clamp the points to the collider's extents
        var localNorm = new Vector3(
                Mathf.Clamp(local.x, -halfSize.x, halfSize.x),
                Mathf.Clamp(local.y, -halfSize.y, halfSize.y),
                Mathf.Clamp(local.z, -halfSize.z, halfSize.z)
            );

        //Calculate distances from each edge
        var dx = Mathf.Min(Mathf.Abs(halfSize.x - localNorm.x), Mathf.Abs(-halfSize.x - localNorm.x));
        var dy = Mathf.Min(Mathf.Abs(halfSize.y - localNorm.y), Mathf.Abs(-halfSize.y - localNorm.y));
        var dz = Mathf.Min(Mathf.Abs(halfSize.z - localNorm.z), Mathf.Abs(-halfSize.z - localNorm.z));

        // Select a face to project on
        if (dx < dy && dx < dz)
        {
            localNorm.x = Mathf.Sign(localNorm.x) * halfSize.x;
        }
        else if (dy < dx && dy < dz)
        {
            localNorm.y = Mathf.Sign(localNorm.y) * halfSize.y;
        }
        else if (dz < dx && dz < dy)
        {
            localNorm.z = Mathf.Sign(localNorm.z) * halfSize.z;
        }

        // Now we undo our transformations
        localNorm += collider.center;

        // Return resulting point
        return ct.TransformPoint(localNorm);
    }

    // Courtesy of Moodie
    public static Vector3 ClosestPointOnSurface(CapsuleCollider collider, Vector3 to)
    {
        Transform ct = collider.transform; // Transform of the collider

        float lineLength = collider.height - collider.radius * 2; // The length of the line connecting the center of both sphere
        Vector3 dir = Vector3.up;

        Vector3 upperSphere = dir * lineLength * 0.5f + collider.center; // The position of the radius of the upper sphere in local coordinates
        Vector3 lowerSphere = -dir * lineLength * 0.5f + collider.center; // The position of the radius of the lower sphere in local coordinates

        Vector3 local = ct.InverseTransformPoint(to); // The position of the controller in local coordinates

        Vector3 p = Vector3.zero; // Contact point
        Vector3 pt = Vector3.zero; // The point we need to use to get a direction vector with the controller to calculate contact point

        if (local.y < lineLength * 0.5f && local.y > -lineLength * 0.5f) // Controller is contacting with cylinder, not spheres
            pt = dir * local.y + collider.center;
        else if (local.y > lineLength * 0.5f) // Controller is contacting with the upper sphere
            pt = upperSphere;
        else if (local.y < -lineLength * 0.5f) // Controller is contacting with lower sphere
            pt = lowerSphere;

        //Calculate contact point in local coordinates and return it in world coordinates
        p = local - pt;
        p.Normalize();
        p = p * collider.radius + pt;
        return ct.TransformPoint(p);

    }
}
