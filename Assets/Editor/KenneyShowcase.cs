// Coloque em Assets/Editor/KenneyShowcase.cs
// Menu: Tools > Kenney > 1 - Vitrine da pasta selecionada
//       Tools > Kenney > 2 - Apagar todas as vitrines
//
// COMO USAR
//  1) File > New Scene (Basic URP) e NAO salve essa cena (assim o GameScene nao fica poluido).
//  2) No Project, clique na PASTA de um pack (a que tem os modelos FBX).
//  3) Tools > Kenney > 1 - Vitrine da pasta selecionada.
//  4) Tire print da Scene View (top-down, ja enquadrada). Se tiver muitos modelos, de zoom e tire varios prints.
//  5) Repita para os outros packs (as vitrines ficam empilhadas no eixo Z, sem sobrepor).
//
// O script escreve tambem Assets/kenney_relatorio_<pasta>.txt com tamanho (x,y,z) de cada modelo.
// Em pecas hexagonais planas aparecem numeros vermelhos 0-5 nas arestas (direcao das conexoes).

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KenneyShowcase
{
    const string ROOT_PREFIX = "KENNEY_VITRINE_";
    const float ROW_MAX_WIDTH = 80f;
    const float GAP = 1.0f;
    const float LABEL_LINE_H = 0.30f;

    // Se o pack tiver o mesmo modelo em varios formatos, usa o primeiro desta lista
    static readonly string[] EXT_PRIORITY = { ".fbx", ".glb", ".gltf", ".obj", ".dae", ".blend", ".prefab" };

    class Item
    {
        public string name;
        public string path;
        public GameObject go;
        public Bounds b;
        public float w, d, h;
        public GameObject label;
        public float labelW, labelH;
        public bool hex, flat;
    }

    [MenuItem("Tools/Kenney/1 - Vitrine da pasta selecionada")]
    public static void Build()
    {
        string folder = SelectedFolder();
        if (folder == null)
        {
            EditorUtility.DisplayDialog("Vitrine Kenney",
                "Selecione no Project a PASTA do pack (a que contem os modelos FBX) e rode de novo.", "OK");
            return;
        }

        // 1) coleta um modelo por nome, respeitando a prioridade de formato
        var best = new Dictionary<string, string>();
        foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new[] { folder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            int pr = ExtRank(p);
            if (pr < 0) continue;
            string key = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
            string old;
            if (best.TryGetValue(key, out old) && ExtRank(old) <= pr) continue;
            best[key] = p;
        }
        if (best.Count == 0)
        {
            EditorUtility.DisplayDialog("Vitrine Kenney",
                "Nenhum modelo encontrado em " + folder +
                "\nO Unity nao importa .glb sozinho: importe a versao FBX do pack.", "OK");
            return;
        }
        var keys = new List<string>(best.Keys);
        keys.Sort();

        string rootName = ROOT_PREFIX + Path.GetFileName(folder);
        DestroyRoots(rootName);
        float offsetZ = NextFreeZ();
        var root = new GameObject(rootName);
        var items = new List<Item>();

        // 2) instancia tudo na origem e mede
        for (int i = 0; i < keys.Count; i++)
        {
            string p = best[keys[i]];
            EditorUtility.DisplayProgressBar("Vitrine Kenney", p, (float)i / keys.Count);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (asset == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            if (go == null) continue;
            Bounds b;
            if (!GetBounds(go, out b)) { Object.DestroyImmediate(go); continue; }

            var it = new Item();
            it.name = Path.GetFileNameWithoutExtension(p);
            it.path = p;
            it.go = go;
            it.b = b;
            it.w = b.size.x;
            it.h = b.size.y;
            it.d = b.size.z;

            float ratio = it.d > 0.0001f ? it.w / it.d : 0f;
            bool pointy = Mathf.Abs(ratio - 0.8660f) < 0.04f;
            bool flat = Mathf.Abs(ratio - 1.1547f) < 0.04f;
            it.hex = (pointy || flat) && it.h < it.w * 0.9f;
            it.flat = flat;

            string txt = it.name + "\n" + F(it.w) + " x " + F(it.h) + " x " + F(it.d);
            it.label = MakeLabel(txt, new Color(0.1f, 0.1f, 0.1f), LABEL_LINE_H, out it.labelW, out it.labelH);
            items.Add(it);
        }
        EditorUtility.ClearProgressBar();

        // 3) agrupa em linhas
        var rows = new List<List<Item>>();
        var cur = new List<Item>();
        float x = 0f;
        foreach (var it in items)
        {
            float cell = Mathf.Max(it.w, it.labelW) + GAP;
            if (x + cell > ROW_MAX_WIDTH && cur.Count > 0)
            {
                rows.Add(cur);
                cur = new List<Item>();
                x = 0f;
            }
            cur.Add(it);
            x += cell;
        }
        if (cur.Count > 0) rows.Add(cur);

        // 4) posiciona
        float zc = 0f;
        float maxX = 0f;
        foreach (var row in rows)
        {
            float rowD = 0f, rowL = 0f;
            foreach (var it in row)
            {
                rowD = Mathf.Max(rowD, it.d);
                rowL = Mathf.Max(rowL, it.labelH);
            }
            float cx = 0f;
            foreach (var it in row)
            {
                float cell = Mathf.Max(it.w, it.labelW) + GAP;
                float mx = cx + cell * 0.5f;
                float mz = zc - rowD * 0.5f;

                Vector3 off = new Vector3(mx - it.b.center.x, -it.b.min.y, mz - it.b.center.z);
                it.go.transform.position += off;
                it.go.transform.SetParent(root.transform, true);

                it.label.transform.SetParent(root.transform, false);
                it.label.transform.localPosition = new Vector3(mx, 0.02f, zc - rowD - it.labelH * 0.5f - 0.15f);

                if (it.hex) AddEdgeNumbers(it, root.transform, mx, mz);
                cx += cell;
            }
            maxX = Mathf.Max(maxX, cx);
            zc -= rowD + rowL + GAP + 0.3f;
        }
        root.transform.position = new Vector3(0f, 0f, offsetZ);

        // 5) relatorio
        var sb = new StringBuilder();
        sb.AppendLine("PASTA: " + folder);
        sb.AppendLine("MODELOS: " + items.Count);
        sb.AppendLine("nome\tformato\tlargura(x)\taltura(y)\tprofundidade(z)\thex?\tcaminho");
        var hw = new List<float>(); var hd = new List<float>(); var hh = new List<float>();
        foreach (var it in items)
        {
            sb.AppendLine(it.name + "\t" + Path.GetExtension(it.path) + "\t" + F(it.w) + "\t" + F(it.h) + "\t" + F(it.d)
                + "\t" + (it.hex ? (it.flat ? "hex-flat-top" : "hex-pointy-top") : "-") + "\t" + it.path);
            if (it.hex) { hw.Add(it.w); hd.Add(it.d); hh.Add(it.h); }
        }
        if (hw.Count > 0)
        {
            hw.Sort(); hd.Sort(); hh.Sort();
            sb.AppendLine();
            sb.AppendLine("TILE HEX (mediana): largura " + F(hw[hw.Count / 2]) + "  profundidade " + F(hd[hd.Count / 2])
                + "  altura " + F(hh[hh.Count / 2]));
        }
        string rep = "Assets/kenney_relatorio_" + Path.GetFileName(folder) + ".txt";
        File.WriteAllText(rep, sb.ToString());
        AssetDatabase.Refresh();

        // 6) enquadra a Scene View de cima
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            Vector3 c = root.transform.position + new Vector3(maxX * 0.5f, 0f, zc * 0.5f);
            sv.orthographic = true;
            sv.LookAt(c, Quaternion.Euler(90f, 0f, 0f), Mathf.Max(maxX, -zc) * 0.55f);
        }
        Selection.activeGameObject = root;
        Debug.Log("Vitrine Kenney: " + items.Count + " modelos de " + folder + ". Relatorio: " + rep);
    }

    [MenuItem("Tools/Kenney/2 - Apagar todas as vitrines")]
    public static void ClearAll()
    {
        int n = 0;
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name.StartsWith(ROOT_PREFIX)) { Object.DestroyImmediate(go); n++; }
        }
        Debug.Log("Vitrines apagadas: " + n);
    }

    // ------------------------------------------------------------------ helpers

    static string F(float v)
    {
        return v.ToString("0.00", CultureInfo.InvariantCulture);
    }

    static string SelectedFolder()
    {
        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0) return null;
        string p = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
        if (string.IsNullOrEmpty(p)) return null;
        if (AssetDatabase.IsValidFolder(p)) return p;
        return Path.GetDirectoryName(p).Replace('\\', '/');
    }

    static int ExtRank(string path)
    {
        return System.Array.IndexOf(EXT_PRIORITY, Path.GetExtension(path).ToLowerInvariant());
    }

    static bool GetBounds(GameObject go, out Bounds b)
    {
        b = new Bounds();
        bool has = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return has;
    }

    static void DestroyRoots(string exactName)
    {
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == exactName) Object.DestroyImmediate(go);
        }
    }

    static float NextFreeZ()
    {
        float minZ = 0f;
        bool any = false;
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (!go.name.StartsWith(ROOT_PREFIX)) continue;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any || r.bounds.min.z < minZ) minZ = r.bounds.min.z;
                any = true;
            }
        }
        return any ? minZ - 20f : 0f;
    }

    // texto 3D deitado (le-se de cima, com +Z para cima da tela)
    static GameObject MakeLabel(string text, Color color, float lineH, out float w, out float h)
    {
        var g = new GameObject("label");
        var tm = g.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;

        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mr = g.GetComponent<MeshRenderer>();
        if (f != null)
        {
            tm.font = f;
            mr.sharedMaterial = f.material;
        }

        string[] ls = text.Split('\n');
        int lines = ls.Length;
        int maxChars = 0;
        foreach (string l in ls) maxChars = Mathf.Max(maxChars, l.Length);

        Vector3 s = mr.bounds.size;
        float oneLine = s.y > 0.0001f ? s.y / lines : 1f;
        float mw = s.x > 0.0001f ? s.x : maxChars * 0.55f * oneLine;
        float k = lineH / oneLine;

        g.transform.localScale = Vector3.one * k;
        g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        w = mw * k;
        h = oneLine * lines * k;
        return g;
    }

    // numeros vermelhos 0-5 nas arestas: 0 = +X (pointy-top) ou 30 graus (flat-top), crescendo de +X para +Z
    static void AddEdgeNumbers(Item it, Transform parent, float cx, float cz)
    {
        float a = (it.flat ? it.d : it.w) * 0.5f;   // apotema
        float r = a * 0.72f;
        float lh = Mathf.Min(0.35f, a * 0.35f);
        for (int k = 0; k < 6; k++)
        {
            float ang = (it.flat ? 30f : 0f) + 60f * k;
            float rad = ang * Mathf.Deg2Rad;
            float tw, th;
            var g = MakeLabel(k.ToString(), Color.red, lh, out tw, out th);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = new Vector3(cx + r * Mathf.Cos(rad), it.h + 0.05f, cz + r * Mathf.Sin(rad));
        }
    }
}