using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
#endif

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteShadow2D : MonoBehaviour
{
    public enum ShadowMode { Auto, Freeform }

    [Header("General Settings")]
    public ShadowMode mode = ShadowMode.Auto;
    [Range(0f, 1f)] public float opacity = 0.5f;
    public Color color = Color.black;
    [Range(-2f, 2f)] public float offsetX = 0.15f;
    [Range(-2f, 2f)] public float offsetY = -0.15f;

    [Header("Freeform Shape")]
    public List<Vector2> freeformPoints = new List<Vector2>();

    private SpriteRenderer source;
    private GameObject shadowObj;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

#if UNITY_EDITOR
    private bool initializedFromSprite = false;
#endif

    private void OnEnable()
    {
        source = GetComponent<SpriteRenderer>();
        CreateShadowObject();
        UpdateShadow();
    }

    private void LateUpdate()
    {
        if (mode == ShadowMode.Auto)
            UpdateShadow();
    }

    private void CreateShadowObject()
    {
        if (shadowObj == null)
        {
            shadowObj = new GameObject("ShadowMesh");
            shadowObj.transform.SetParent(transform);
            shadowObj.transform.localPosition = Vector3.zero;
            shadowObj.transform.localRotation = Quaternion.identity;

            meshFilter = shadowObj.AddComponent<MeshFilter>();
            meshRenderer = shadowObj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }
    }

    public void UpdateShadow()
    {
        if (!source) return;

        // ? ????? mesh ???????????????
        if (mode == ShadowMode.Auto)
        {
            shadowObj.SetActive(true);
            Mesh mesh = GenerateAutoMesh();
            meshFilter.sharedMesh = mesh;
        }
        else if (mode == ShadowMode.Freeform)
        {
#if UNITY_EDITOR
            if (!initializedFromSprite)
            {
                LoadSpriteOutline();
                initializedFromSprite = true;
            }
#endif
            Mesh mesh = GenerateFreeformMesh();
            meshFilter.sharedMesh = mesh;
        }

        // ? ??????????????????????????? sprite ???? ?????????????????? offset
        if (source.sprite != null)
        {
            Bounds b = source.sprite.bounds;
            Vector3 basePos = new Vector3(0, 0, 0); // ????????? sprite
            shadowObj.transform.localPosition = basePos + new Vector3(offsetX, offsetY, 0);
        }
        else
        {
            shadowObj.transform.localPosition = new Vector3(offsetX, offsetY, 0);
        }

        // ? ?????????????????????
        meshRenderer.sortingLayerID = source.sortingLayerID;
        meshRenderer.sortingOrder = source.sortingOrder - 1;
        meshRenderer.sharedMaterial.color = new Color(color.r, color.g, color.b, opacity);
    }

    private Mesh GenerateAutoMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, -0.5f, 0)
        };
        mesh.triangles = new int[] { 0, 1, 2, 2, 3, 0 };
        mesh.RecalculateNormals();
        return mesh;
    }

    private Mesh GenerateFreeformMesh()
    {
        if (freeformPoints == null || freeformPoints.Count < 3)
            return GenerateAutoMesh();

        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[freeformPoints.Count];
        for (int i = 0; i < freeformPoints.Count; i++)
            verts[i] = freeformPoints[i];

        int[] tris = new int[(freeformPoints.Count - 2) * 3];
        for (int i = 0; i < freeformPoints.Count - 2; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

#if UNITY_EDITOR
    public void LoadSpriteOutline()
    {
        freeformPoints.Clear();

        if (source.sprite == null) return;

        var sprite = source.sprite;
        var physicsShape = new List<Vector2>();
        int shapeCount = sprite.GetPhysicsShapeCount();

        if (shapeCount > 0)
        {
            sprite.GetPhysicsShape(0, physicsShape);
            foreach (var pt in physicsShape)
                freeformPoints.Add(pt);
        }
        else
        {
            Vector2 min = sprite.bounds.min;
            Vector2 max = sprite.bounds.max;
            freeformPoints.Add(new Vector2(min.x, max.y));
            freeformPoints.Add(new Vector2(max.x, max.y));
            freeformPoints.Add(new Vector2(max.x, min.y));
            freeformPoints.Add(new Vector2(min.x, min.y));
        }
    }
#endif

    private void OnDisable()
    {
        if (shadowObj != null)
            DestroyImmediate(shadowObj);
    }
}
