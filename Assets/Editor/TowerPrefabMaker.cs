using System.IO;
using UnityEditor;
using UnityEngine;

public static class TowerPrefabMaker
{
    // ---------- 1) Torre: selecione a base + a arma na Hierarchy ----------
    [MenuItem("Tools/Torres/1 - Criar prefab da torre (selecione base + arma)")]
    static void MakeTower()
    {
        GameObject baseGO = null, weaponGO = null;
        foreach (var g in Selection.gameObjects)
        {
            string n = g.name.ToLowerInvariant();
            if (n.Contains("weapon")) weaponGO = g;
            else if (n.Contains("base")) baseGO = g;
        }
        if (baseGO == null || weaponGO == null)
        {
            EditorUtility.DisplayDialog("Torres",
                "Selecione exatamente 2 objetos na Hierarchy: a base (nome com 'base') e a arma (nome com 'weapon').", "OK");
            return;
        }

        // nome do estilo: weapon-ballista -> Ballista
        string style = weaponGO.name.Replace("(Clone)", "").Trim();
        style = style.Replace("weapon-", "").Replace("Weapon-", "");
        string styleCap = char.ToUpperInvariant(style[0]) + style.Substring(1);
        string key = style.ToLowerInvariant();

        // raiz da torre na posição da base; base e arma viram filhas
        var root = new GameObject("Tower_" + styleCap);
        Undo.RegisterCreatedObjectUndo(root, "Criar torre");
        root.transform.position = baseGO.transform.position;
        Undo.SetTransformParent(baseGO.transform, root.transform, "Base na torre");
        Undo.SetTransformParent(weaponGO.transform, root.transform, "Arma na torre");

        var tower = root.AddComponent<Tower>();
        tower.weaponPivot = weaponGO.transform;

        // ponto de saída do projétil: frente e parte de cima da arma
        Bounds b = GetBounds(weaponGO);
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.position = b.center + weaponGO.transform.forward * b.extents.z + Vector3.up * (b.extents.y * 0.3f);
        muzzle.SetParent(weaponGO.transform, true);
        tower.muzzle = muzzle;

        // valores iniciais diferentes por estilo (ajuste no Inspector)
        if (key.Contains("ballista"))      { tower.range = 9f;  tower.cooldown = 1.2f; tower.damage = 30f; tower.projectileSpeed = 18f; }
        else if (key.Contains("turret"))   { tower.range = 6f;  tower.cooldown = 0.25f; tower.damage = 6f; tower.projectileSpeed = 22f; }
        else if (key.Contains("cannon"))   { tower.range = 8f;  tower.cooldown = 2f;   tower.damage = 40f; tower.splashRadius = 2f; tower.projectileSpeed = 14f; }
        else if (key.Contains("catapult")) { tower.range = 12f; tower.cooldown = 3f;   tower.damage = 50f; tower.splashRadius = 3f; tower.projectileSpeed = 10f; tower.arcHeight = 3f; }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Towers");
        string path = "Assets/Prefabs/Towers/Tower_" + styleCap + ".prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);

        Selection.activeGameObject = root;
        Debug.Log("Prefab criado: " + path + "  -> agora arraste o Projectile Prefab no componente Tower.");
    }

    // ---------- 2) Projétil: selecione o modelo da munição na cena ----------
    [MenuItem("Tools/Torres/2 - Criar prefab de projétil (selecione a munição)")]
    static void MakeProjectile()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Torres", "Selecione o modelo da munição na cena (ex.: uma flecha ou bala).", "OK");
            return;
        }

        var proj = Undo.AddComponent<Projectile>(go);                 // adiciona Rigidbody e SphereCollider junto
        var col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;

        Bounds b = GetBounds(go);
        float scale = Mathf.Max(0.0001f, go.transform.lossyScale.x);
        col.center = go.transform.InverseTransformPoint(b.center);
        col.radius = Mathf.Max(0.2f, b.extents.magnitude * 0.6f) / scale;   // um pouco generoso para acertar

        string baseName = go.name.Replace("(Clone)", "").Trim();
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Projectiles");
        string path = "Assets/Prefabs/Projectiles/Projectile_" + baseName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);

        Debug.Log("Prefab de projétil criado: " + path + "  (pode apagar o objeto da cena)");
    }

    // ---------- utilidades ----------
    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}