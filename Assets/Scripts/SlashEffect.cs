using UnityEngine;

/// <summary>
/// Rastro do golpe do player: um arco que "varre" a área atingida e some em seguida.
/// Criado e reaproveitado pelo PlayerAttack (um só objeto, sem criar nada novo a cada golpe).
/// O formato acompanha o alcance (range) e a abertura (arcAngle) do ataque, então mostra
/// exatamente onde o golpe acerta.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SlashEffect : MonoBehaviour
{
    const int Segments = 20;

    Mesh mesh;
    Vector3[] vertices;
    Color[] colors;
    Color tint = Color.white;
    float sweepTime, fadeTime, timer;
    bool playing;

    /// <summary>Cria o objeto do efeito (chamado uma vez pelo PlayerAttack).</summary>
    public static SlashEffect Create(Material material)
    {
        var go = new GameObject("SlashEffect", typeof(MeshFilter), typeof(MeshRenderer), typeof(SlashEffect));
        var fx = go.GetComponent<SlashEffect>();
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material != null ? material : DefaultMaterial();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        go.SetActive(false);
        return fx;
    }

    void Awake()
    {
        mesh = new Mesh { name = "Slash" };
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        vertices = new Vector3[(Segments + 1) * 2];
        colors = new Color[vertices.Length];

        var tris = new int[Segments * 6];
        for (int i = 0; i < Segments; i++)
        {
            int v = i * 2, t = i * 6;
            tris[t] = v;     tris[t + 1] = v + 1; tris[t + 2] = v + 2;
            tris[t + 3] = v + 1; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = tris;
    }

    /// <summary>Mostra o golpe na posição/direção dadas.</summary>
    public void Play(Vector3 position, Quaternion rotation, float range, float arcAngle, Color color,
                     float sweepDuration, float fadeDuration)
    {
        transform.SetPositionAndRotation(position, rotation);
        tint = color;
        sweepTime = Mathf.Max(0.01f, sweepDuration);
        fadeTime = Mathf.Max(0.01f, fadeDuration);
        timer = 0f;
        playing = true;

        // arco no plano horizontal, da direita para a esquerda do player
        float inner = range * 0.35f;
        float half = arcAngle * 0.5f;
        for (int i = 0; i <= Segments; i++)
        {
            float a = Mathf.Lerp(half, -half, i / (float)Segments) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            vertices[i * 2] = dir * inner;
            vertices[i * 2 + 1] = dir * range;
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        gameObject.SetActive(true);
        UpdateColors();
    }

    void Update()
    {
        if (!playing) return;
        timer += Time.deltaTime;
        if (timer >= sweepTime + fadeTime)
        {
            playing = false;
            gameObject.SetActive(false);
            return;
        }
        UpdateColors();
    }

    void UpdateColors()
    {
        // varredura: a ponta avança pelo arco; depois tudo some
        float sweep = Mathf.Clamp01(timer / sweepTime);
        float fade = timer <= sweepTime ? 1f : 1f - (timer - sweepTime) / fadeTime;

        for (int i = 0; i <= Segments; i++)
        {
            float p = i / (float)Segments;
            // só aparece a parte já varrida; a ponta é mais forte, o rastro vai sumindo para trás
            float a = p > sweep ? 0f : Mathf.Lerp(0.25f, 1f, 1f - (sweep - p));
            a *= fade * tint.a;

            colors[i * 2] = new Color(tint.r, tint.g, tint.b, a * 0.2f);    // borda de dentro, mais fraca
            colors[i * 2 + 1] = new Color(tint.r, tint.g, tint.b, a);       // borda de fora
        }
        mesh.colors = colors;
    }

    // material transparente que usa as cores dos vértices
    static Material DefaultMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var mat = new Material(shader) { name = "Slash (auto)" };
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);   // Transparent
            mat.SetFloat("_Blend", 0f);     // Alpha
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);      // visível dos dois lados
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        return mat;
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }
}
