using TMPro;
using UnityEngine;

public class TextControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text text;

    [Header("Wave Settings")]
    [SerializeField] private float height = 10f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float characterDelay = 0.15f;

    private TMP_TextInfo textInfo;

    private Vector3[][] originalVertices;

    private void Awake()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();

        text.ForceMeshUpdate();

        textInfo = text.textInfo;

        SaveOriginalVertices();
    }

    private void SaveOriginalVertices()
    {
        originalVertices = new Vector3[textInfo.meshInfo.Length][];

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            originalVertices[i] =
                (Vector3[])textInfo.meshInfo[i].vertices.Clone();
        }
    }

    private void LateUpdate()
    {
        text.ForceMeshUpdate();

        textInfo = text.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] vertices =
                textInfo.meshInfo[materialIndex].vertices;

            // Restore original position
            Vector3[] original =
                originalVertices[materialIndex];

            // Wave timing for this character
            float time =
                Time.time * speed
                - i * characterDelay;

            // Smooth up/down movement
            float wave =
                Mathf.Sin(time);

            float offsetY =
                wave * height;

            Vector3 offset =
                new Vector3(0f, offsetY, 0f);

            vertices[vertexIndex + 0] =
                original[vertexIndex + 0] + offset;

            vertices[vertexIndex + 1] =
                original[vertexIndex + 1] + offset;

            vertices[vertexIndex + 2] =
                original[vertexIndex + 2] + offset;

            vertices[vertexIndex + 3] =
                original[vertexIndex + 3] + offset;
        }

        // Update meshes
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices =
                textInfo.meshInfo[i].vertices;

            textInfo.meshInfo[i].mesh.RecalculateBounds();

            text.UpdateGeometry(
                textInfo.meshInfo[i].mesh,
                i
            );
        }
    }
}