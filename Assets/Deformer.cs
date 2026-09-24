using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(AudioSource))]
public class Deformer : MonoBehaviour
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
    public float audioStrength = 30;
    public int bassBins = 6;

    float[] spectrum = new float[64];

    AudioSource audioSource;

    // 1 = wave, 2 = twist, 3 = audio
    public int deformationMode = 1;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        originalVertices = mesh.vertices;
        vertices = new Vector3[originalVertices.Length];

        audioSource = GetComponent<AudioSource>();

        // Don't start playing immediately
        audioSource.playOnAwake = false;
        audioSource.Stop();
    }

    void Update()
    {
        // Switch modes
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            deformationMode = 1;
            audioSource.Pause();
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            deformationMode = 2;
            audioSource.Pause();
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            deformationMode = 3;

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

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

    void ApplyWave()
    {
        float[] waves = new float[originalVertices.Length];
        float averageWave = 0f;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];

            waves[i] =
                Mathf.Sin(v.x * frequency + Time.time * speed)
                * amplitude;

            averageWave += waves[i];
        }

        averageWave /= originalVertices.Length;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];

            v.y += waves[i] - averageWave;

            vertices[i] = v;
        }
    }

    void ApplyTwist()
    {
        if (Keyboard.current.eKey.isPressed)
            twistAmount += twistSpeed * Time.deltaTime;

        if (Keyboard.current.qKey.isPressed)
            twistAmount -= twistSpeed * Time.deltaTime;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];

            float angle = v.y * twistAmount * twistStrength;

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            float newX = v.x * cos - v.y * sin;
            float newY = v.x * sin + v.y * cos;

            v.x = newX;
            v.y = newY;

            vertices[i] = v;
        }
    }

    void ApplyAudioDeformation()
    {
        // Get frequency spectrum
        audioSource.GetSpectrumData(
            spectrum,
            0,
            FFTWindow.BlackmanHarris
        );

        // Average only low-frequency bins = bass
        float bass = 0f;

        for (int i = 0; i < bassBins; i++)
        {
            bass += spectrum[i];
        }

        bass /= bassBins;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];

            // Different parts deform differently
            float variation =
                Mathf.Sin(v.x * 5f + v.y * 5f);

            Vector3 direction = v.normalized;

            v += direction
                 * bass
                 * audioStrength
                 * variation;

            vertices[i] = v;
        }
    }
}