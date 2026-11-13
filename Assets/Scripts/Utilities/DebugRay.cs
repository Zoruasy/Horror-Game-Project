using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Only draws rays, it never does a raycast itself, so it can't change gameplay.
/// Visible in the Scene view, and in the Game view when the Gizmos button is on.
/// The calls are stripped from builds automatically (only run in the editor).
///
/// Examples:
///   DebugRay.Draw(eyePoint.position, eyePoint.forward, 10f, Color.yellow);
///   DebugRay.DrawLine(eyePoint.position, player.position, Color.red);
///
///   bool didHit = Physics.Raycast(origin, direction, out RaycastHit hit, length);
///   DebugRay.DrawResult(origin, direction, length, didHit, hit);
/// </summary>
public static class DebugRay
{
    /// <summary>Turn all debug rays on/off at once.</summary>
    public static bool Enabled = true;

    private const float HitMarkerSize = 0.15f;
    private const float NormalLength = 0.4f;

    /// <summary>Draws a ray from origin in a direction, with a given length.</summary>
    /// <param name="duration">How long the ray stays visible in seconds. 0 = one frame (use this when calling every frame).</param>
    [Conditional("UNITY_EDITOR")]
    public static void Draw(Vector3 origin, Vector3 direction, float length, Color color, float duration = 0f)
    {
        if (!Enabled)
            return;

        Debug.DrawRay(origin, direction.normalized * length, color, duration, false);
    }

    /// <summary>Draws a line between two points.</summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawLine(Vector3 from, Vector3 to, Color color, float duration = 0f)
    {
        if (!Enabled)
            return;

        Debug.DrawLine(from, to, color, duration, false);
    }

    /// <summary>
    /// Shows the result of a raycast you already did.
    /// Hit: line up to the hit point, a cross at the hit point, the surface normal, and the rest of the ray faded.
    /// Miss: the full ray in the miss color.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawResult(Vector3 origin, Vector3 direction, float length, bool didHit, RaycastHit hit,
        float duration = 0f)
    {
        DrawResult(origin, direction, length, didHit, hit, Color.green, Color.red, duration);
    }

    /// <summary>Same as above, but with your own colors.</summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawResult(Vector3 origin, Vector3 direction, float length, bool didHit, RaycastHit hit,
        Color hitColor, Color missColor, float duration = 0f)
    {
        if (!Enabled)
            return;

        Vector3 dir = direction.normalized;

        if (!didHit)
        {
            Debug.DrawRay(origin, dir * length, missColor, duration, false);
            return;
        }

        //line up to what it hit
        Debug.DrawLine(origin, hit.point, hitColor, duration, false);

        //the part of the ray after the hit (where it would have gone), faded
        Color fadedColor = new Color(hitColor.r, hitColor.g, hitColor.b, 0.25f);
        Debug.DrawLine(hit.point, origin + dir * length, fadedColor, duration, false);

        DrawCross(hit.point, HitMarkerSize, hitColor, duration);
        Debug.DrawRay(hit.point, hit.normal * NormalLength, Color.cyan, duration, false);
    }

    /// <summary>
    /// Shows the result of a Physics.SphereCast (a "thick" ray) you already did.
    /// Draws the swept capsule: a wire sphere at the start, a wire sphere where it stopped, and lines along the sides.
    /// Hit: capsule up to the hit position, cross at the contact point, surface normal, rest of the cast faded.
    /// Miss: the full capsule in the miss color.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawSphereCastResult(Vector3 origin, Vector3 direction, float radius, float length, bool didHit,
        RaycastHit hit, float duration = 0f)
    {
        DrawSphereCastResult(origin, direction, radius, length, didHit, hit, Color.green, Color.red, duration);
    }

    /// <summary>Same as above, but with your own colors.</summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawSphereCastResult(Vector3 origin, Vector3 direction, float radius, float length, bool didHit,
        RaycastHit hit, Color hitColor, Color missColor, float duration = 0f)
    {
        if (!Enabled)
            return;

        Vector3 dir = direction.normalized;
        Vector3 fullEnd = origin + dir * length;

        if (!didHit)
        {
            DrawCapsule(origin, fullEnd, radius, missColor, duration);
            return;
        }

        //where the center of the sphere was when it hit something
        Vector3 hitCenter = origin + dir * hit.distance;
        DrawCapsule(origin, hitCenter, radius, hitColor, duration);

        //the part of the cast after the hit, faded
        Color fadedColor = new Color(hitColor.r, hitColor.g, hitColor.b, 0.25f);
        Debug.DrawLine(hitCenter, fullEnd, fadedColor, duration, false);

        DrawCross(hit.point, HitMarkerSize, hitColor, duration);
        Debug.DrawRay(hit.point, hit.normal * NormalLength, Color.cyan, duration, false);
    }

    /// <summary>Draws a wireframe capsule between two sphere centers (the shape a SphereCast sweeps).</summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawCapsule(Vector3 start, Vector3 end, float radius, Color color, float duration = 0f)
    {
        if (!Enabled)
            return;

        DrawWireSphere(start, radius, color, duration);
        DrawWireSphere(end, radius, color, duration);

        Vector3 axis = end - start;
        if (axis.sqrMagnitude < 0.0001f)
            return;

        //4 lines along the sides of the capsule
        Vector3 forward = axis.normalized;
        Vector3 side = Vector3.Cross(forward, Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up).normalized;
        Vector3 up = Vector3.Cross(side, forward);

        Debug.DrawLine(start + side * radius, end + side * radius, color, duration, false);
        Debug.DrawLine(start - side * radius, end - side * radius, color, duration, false);
        Debug.DrawLine(start + up * radius, end + up * radius, color, duration, false);
        Debug.DrawLine(start - up * radius, end - up * radius, color, duration, false);
        Debug.DrawLine(start, end, color, duration, false); //center line
    }

    /// <summary>Draws a wireframe sphere (3 circles).</summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration = 0f)
    {
        if (!Enabled)
            return;

        DrawCircle(center, Vector3.up, radius, color, duration);
        DrawCircle(center, Vector3.right, radius, color, duration);
        DrawCircle(center, Vector3.forward, radius, color, duration);
    }

    /// <summary>Draws a circle around a center, facing the given normal.</summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawCircle(Vector3 center, Vector3 normal, float radius, Color color, float duration = 0f,
        int segments = 24)
    {
        if (!Enabled || segments < 3)
            return;

        Vector3 n = normal.normalized;
        Vector3 a = Vector3.Cross(n, Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up).normalized;
        Vector3 b = Vector3.Cross(n, a);

        Vector3 previous = center + a * radius;
        for (int s = 1; s <= segments; s++)
        {
            float angle = s * Mathf.PI * 2f / segments;
            Vector3 next = center + (a * Mathf.Cos(angle) + b * Mathf.Sin(angle)) * radius;
            Debug.DrawLine(previous, next, color, duration, false);
            previous = next;
        }
    }

    /// <summary>Draws a small 3D cross, handy to mark a point.</summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawCross(Vector3 point, float size, Color color, float duration = 0f)
    {
        if (!Enabled)
            return;

        float half = size * 0.5f;
        Debug.DrawLine(point - Vector3.right * half, point + Vector3.right * half, color, duration, false);
        Debug.DrawLine(point - Vector3.up * half, point + Vector3.up * half, color, duration, false);
        Debug.DrawLine(point - Vector3.forward * half, point + Vector3.forward * half, color, duration, false);
    }
}
