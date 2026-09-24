using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(AudioSource))]
public class VRDeformer : MonoBehaviour
{
    Mesh mesh;

    Vector3[] originalVertices;
    Vector3[] vertices;

    // Wave settings
    public float amplitude = 0.2f;
    public float speed = 2f;
    public float frequency = 3f;

    // Twist settings
    public float twistAmount = 0f;
    public float twistSpeed = 5f;
    public float twistStrength = 5f;

    // Audio settings
    public float audioStrength = 0.3f;
    public int bassBins = 4;
    public float bassMin = 0.001f;
    public float bassMax = 0.03f;
    public float bassPower = 2.5f;

    float[] spectrum = new float[64];

    AudioSource audioSource;

    // VR controllers
    InputDevice leftController;
    InputDevice rightController;

    // 1 = wave
    // 2 = twist
    // 3 = audio
    public int deformationMode = 1;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        originalVertices = mesh.vertices;
        vertices = new Vector3[originalVertices.Length];

        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.Stop();

        FindControllers();
    }

    void FindControllers()
    {
        leftController =
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        rightController =
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        // Controllers sometimes aren't ready immediately,
        // so reconnect if needed.
        if (!leftController.isValid || !rightController.isValid)
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
            audioSource.Pause();
        }

        // B = Twist
        if (rightB)
        {
            deformationMode = 2;
            audioSource.Pause();
        }

        // X = Audio
        if (leftX)
        {
            deformationMode = 3;

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
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
            ApplyAudioDeformation();
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
            Vector3 v = originalVertices[i];

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
            Vector3 v = originalVertices[i];

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

        // Right trigger twists one direction.
        // Left trigger twists the opposite direction.
        twistAmount +=
            (rightTrigger - leftTrigger)
            * twistSpeed
            * Time.deltaTime;

        for (int i = 0;
             i < originalVertices.Length;
             i++)
        {
            Vector3 v = originalVertices[i];

            float angle =
                v.y *
                twistAmount *
                twistStrength;

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

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
    // MODE 3 - AUDIO
    // =========================================================

    void ApplyAudioDeformation()
    {
        audioSource.GetSpectrumData(
            spectrum,
            0,
            FFTWindow.BlackmanHarris
        );

        float bass = 0f;

        for (int i = 0;
             i < bassBins;
             i++)
        {
            bass += spectrum[i];
        }

        bass /= bassBins;

        // Convert detected bass into 0-1
        float normalizedBass =
            Mathf.InverseLerp(
                bassMin,
                bassMax,
                bass
            );

        // Emphasize loud bass
        float emphasizedBass =
            Mathf.Pow(
                normalizedBass,
                bassPower
            );

        for (int i = 0;
             i < originalVertices.Length;
             i++)
        {
            Vector3 v =
                originalVertices[i];

            float variation =
                Mathf.Sin(
                    v.x * 5f +
                    v.y * 5f
                );

            Vector3 direction =
                v.normalized;

            v += direction
                 * emphasizedBass
                 * audioStrength
                 * variation;

            vertices[i] = v;
        }
    }
}