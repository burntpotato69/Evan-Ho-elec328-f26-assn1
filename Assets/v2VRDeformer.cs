using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(MeshFilter))]
public class v2VRDeformer : MonoBehaviour
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
    // NOD SETTINGS
    // =========================================================

    // Speed of the nodding motion
    public float nodSpeed = 2f;

    // Maximum bend angle in degrees
    public float nodAngle = 35f;

    Vector3 meshCenter;
    float meshTop;

    // =========================================================
    // VR CONTROLLERS
    // =========================================================

    InputDevice leftController;
    InputDevice rightController;

    // 1 = wave
    // 2 = twist
    // 3 = nod
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

        meshTop =
            mesh.bounds.max.y;

        for (int i = 0;
             i < originalVertices.Length;
             i++)
        {
            vertices[i] =
                originalVertices[i];
        }

        FindControllers();
    }

    // =========================================================
    // FIND VR CONTROLLERS
    // =========================================================

    void FindControllers()
    {
        leftController =
            InputDevices.GetDeviceAtXRNode(
                XRNode.LeftHand
            );

        rightController =
            InputDevices.GetDeviceAtXRNode(
                XRNode.RightHand
            );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        // Reconnect if controllers aren't ready yet
        if (!leftController.isValid ||
            !rightController.isValid)
        {
            FindControllers();
        }

        // ----------------------------
        // BUTTON INPUT
        // ----------------------------

        bool rightA = false;
        bool rightB = false;
        bool leftX = false;

        rightController.TryGetFeatureValue(
            CommonUsages.primaryButton,
            out rightA
        );

        rightController.TryGetFeatureValue(
            CommonUsages.secondaryButton,
            out rightB
        );

        leftController.TryGetFeatureValue(
            CommonUsages.primaryButton,
            out leftX
        );

        // A = Wave
        if (rightA)
        {
            deformationMode = 1;
        }

        // B = Twist
        if (rightB)
        {
            deformationMode = 2;
        }

        // X = Nod
        if (leftX)
        {
            deformationMode = 3;
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
            ApplyNod();
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
        float leftTrigger = 0f;
        float rightTrigger = 0f;

        leftController.TryGetFeatureValue(
            CommonUsages.trigger,
            out leftTrigger
        );

        rightController.TryGetFeatureValue(
            CommonUsages.trigger,
            out rightTrigger
        );

        twistAmount +=
            (rightTrigger - leftTrigger)
            * twistSpeed
            * Time.deltaTime;

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
    // MODE 3 - NOD
    // =========================================================

    void ApplyNod()
    {
        // Continuous nodding motion
        float nod =
            Mathf.Sin(Time.time * nodSpeed);

        // Maximum nod angle converted to radians
        float maxAngle =
            nod *
            nodAngle *
            Mathf.Deg2Rad;

        // Distance from center to top of mesh
        float upperHeight =
            meshTop -
            meshCenter.y;

        for (int i = 0;
             i < originalVertices.Length;
             i++)
        {
            Vector3 v =
                originalVertices[i];

            // Only deform upper half
            if (v.y > meshCenter.y)
            {
                // 0 at center
                // 1 at top of mesh
                float influence =
                    (v.y - meshCenter.y)
                    / upperHeight;

                influence =
                    Mathf.Clamp01(influence);

                // Higher vertices bend more
                float angle =
                    maxAngle *
                    influence;

                float cos =
                    Mathf.Cos(angle);

                float sin =
                    Mathf.Sin(angle);

                // Position relative to bend pivot
                float relativeY =
                    v.y -
                    meshCenter.y;

                float relativeZ =
                    v.z -
                    meshCenter.z;

                // Rotate around X axis
                float newY =
                    relativeY * cos -
                    relativeZ * sin;

                float newZ =
                    relativeY * sin +
                    relativeZ * cos;

                v.y =
                    meshCenter.y +
                    newY;

                v.z =
                    meshCenter.z +
                    newZ;
            }

            vertices[i] = v;
        }
    }
}