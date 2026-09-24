using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MeshFilter))]
public class testVRDeformer : MonoBehaviour
{
    Mesh mesh;

    Vector3[] originalVertices;
    Vector3[] vertices;

    // =========================================================
    // WAVE SETTINGS
    // =========================================================

    public float amplitude = 0.2f;
    public float speed = 2f;
    public float frequency = 3f;

    // =========================================================
    // TWIST SETTINGS
    // =========================================================

    public float twistAmount = 0f;
    public float twistSpeed = 5f;
    public float twistStrength = 5f;

    // =========================================================
    // PROXIMITY SETTINGS
    // =========================================================

    // How far the controller must move closer
    // to reach maximum enlargement.
    public float proximityTravel = 0.5f;

    // Maximum fractional enlargement.
    public float maxScaleIncrease = 0.25f;

    // Drag DebugController into this field.
    public Transform debugController;

    float startingControllerDistance;
    Vector3 meshCenter;

    // 1 = wave
    // 2 = twist
    // 3 = proximity
    public int deformationMode = 1;

    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        originalVertices = mesh.vertices;

        vertices =
            new Vector3[originalVertices.Length];

        meshCenter =
            mesh.bounds.center;

        // Copy original mesh
        for (int i = 0;
             i < originalVertices.Length;
             i++)
        {
            vertices[i] =
                originalVertices[i];
        }

        // Record starting controller distance.
        // This becomes the "no deformation" baseline.
        if (debugController != null)
        {
            Vector3 starCenterWorld =
                transform.TransformPoint(
                    meshCenter
                );

            startingControllerDistance =
                Vector3.Distance(
                    debugController.position,
                    starCenterWorld
                );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        // ----------------------------
        // MODE SWITCHING
        // ----------------------------

        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                deformationMode = 1;
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                deformationMode = 2;
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                deformationMode = 3;
            }
        }

        // ----------------------------
        // APPLY DEFORMATION
        // ----------------------------

        if (deformationMode == 1)
        {
            ApplyWave();
        }
        else if (deformationMode == 2)
        {
            ApplyTwist();
        }
        else if (deformationMode == 3)
        {
            ApplyProximityDeformation();
        }

        mesh.vertices = vertices;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // =========================================================
    // MODE 1 - WAVE
    // =========================================================

    void ApplyWave()
    {
        float[] waves =
            new float[originalVertices.Length];

        float averageWave = 0f;

        for (int i = 0;
             i < originalVertices.Length;
             i++)
        {
            Vector3 v =
                originalVertices[i];

            waves[i] =
                Mathf.Sin(
                    v.x * frequency +
                    Time.time * speed
                )
                * amplitude;

            averageWave += waves[i];
        }

        averageWave /=
            originalVertices.Length;

        for (int i = 0;
             i < originalVertices.Length;
             i++)
        {
            Vector3 v =
                originalVertices[i];

            v.y +=
                waves[i] -
                averageWave;

            vertices[i] = v;
        }
    }

    // =========================================================
    // MODE 2 - TWIST
    // =========================================================

    void ApplyTwist()
    {
        if (Keyboard.current != null)
        {
            // E twists one direction
            if (Keyboard.current.eKey.isPressed)
            {
                twistAmount +=
                    twistSpeed *
                    Time.deltaTime;
            }

            // Q twists the opposite direction
            if (Keyboard.current.qKey.isPressed)
            {
                twistAmount -=
                    twistSpeed *
                    Time.deltaTime;
            }
        }

        for (int i = 0;
             i < originalVertices.Length;
             i++)
        {
            Vector3 v =
                originalVertices[i];

            float angle =
                v.y *
                twistAmount *
                twistStrength;

            float cos =
                Mathf.Cos(angle);

            float sin =
                Mathf.Sin(angle);

            float newX =
                v.x * cos -
                v.y * sin;

            float newY =
                v.x * sin +
                v.y * cos;

            v.x = newX;
            v.y = newY;

            vertices[i] = v;
        }
    }

    // =========================================================
    // MODE 3 - PROXIMITY
    // =========================================================

    void ApplyProximityDeformation()
    {
        if (debugController == null)
        {
            for (int i = 0;
                 i < originalVertices.Length;
                 i++)
            {
                vertices[i] =
                    originalVertices[i];
            }

            return;
        }

        // World-space center of the star
        Vector3 starCenterWorld =
            transform.TransformPoint(
                meshCenter
            );

        // Current distance from controller
        // to star center
        float currentDistance =
            Vector3.Distance(
                debugController.position,
                starCenterWorld
            );

        // How much closer has the controller moved
        // compared with its starting position?
        float distanceMovedCloser =
            startingControllerDistance -
            currentDistance;

        // Convert movement into a 0-1 value
        float influence =
            Mathf.Max(
                0f,
                distanceMovedCloser /
                proximityTravel
            );
        // 1.0 = original size
        // 1.15 = 15% larger, etc.
        float scale =
            1f +
            influence *
            maxScaleIncrease;

        for (int i = 0;
             i < originalVertices.Length;
             i++)
        {
            Vector3 v =
                originalVertices[i];

            // Scale every vertex outward
            // from the center of the mesh
            v =
                meshCenter +
                (v - meshCenter) *
                scale;

            vertices[i] = v;
        }
    }
}