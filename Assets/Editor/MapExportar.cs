using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MapExporter
{
    [MenuItem("Tools/Mapa/Exportar objeto selecionado para texto")]
    static void Export()
    {
        var root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Mapa", "Selecione o grupo do mapa na Hierarchy (ex.: Map).", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("# raiz: " + root.name);
        sb.AppendLine("# caminho | modelo de origem | x y z (mundo) | rotY | escala xyz | componentes");
        Walk(root.transform, root.name, false, sb);

        // salva na pasta do projeto (fora de Assets, para não entrar no repositório sem querer)
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "mapa_exportado_" + root.name + ".txt"));
        File.WriteAllText(path, sb.ToString());
        EditorUtility.RevealInFinder(path);
        Debug.Log("Mapa exportado: " + path);
    }

    static void Walk(Transform t, string path, bool insidePrefab, StringBuilder sb)
    {
        var go = t.gameObject;
        bool isPrefabRoot = PrefabUtility.IsOutermostPrefabInstanceRoot(go);

        string src = "";
        if (isPrefabRoot)
        {
            var s = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
            if (s != null) src = AssetDatabase.GetAssetPath(s);
        }

        string comps = ComponentsText(go);

        // dentro de um modelo importado só interessa o que você adicionou (collider etc.)
        bool print = !insidePrefab || comps.Length > 0;
        if (print)
        {
            Vector3 p = t.position, sc = t.lossyScale;
            sb.AppendLine(path + " | " + src + " | " +
                F(p.x) + " " + F(p.y) + " " + F(p.z) + " | " +
                F(t.eulerAngles.y) + " | " +
                F(sc.x) + " " + F(sc.y) + " " + F(sc.z) + " | " + comps);
        }

        foreach (Transform child in t)
            Walk(child, path + "/" + child.name, insidePrefab || isPrefabRoot, sb);
    }

    // lista só componentes "de jogo" (ignora Transform, malha e renderer)
    static string ComponentsText(GameObject go)
    {
        var sb = new StringBuilder();
        foreach (var c in go.GetComponents<Component>())
        {
            if (c == null) continue;
            if (c is Transform || c is MeshFilter || c is MeshRenderer || c is SkinnedMeshRenderer) continue;

            if (sb.Length > 0) sb.Append(", ");
            sb.Append(c.GetType().Name);

            if (c is BoxCollider b)
                sb.Append("(centro " + V(b.center) + " tam " + V(b.size) + (b.isTrigger ? " trigger" : "") + ")");
            else if (c is SphereCollider sp)
                sb.Append("(centro " + V(sp.center) + " raio " + F(sp.radius) + (sp.isTrigger ? " trigger" : "") + ")");
        }
        return sb.ToString();
    }

    static string F(float v) { return v.ToString("F3", CultureInfo.InvariantCulture); }
    static string V(Vector3 v) { return F(v.x) + "," + F(v.y) + "," + F(v.z); }
}