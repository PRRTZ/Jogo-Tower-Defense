// Coloque em: Assets/Editor/KenneyVilaBuilder.cs
// Menu: Tools > Kenney > 4 - Construir vilarejo (pode rodar de novo: apaga o KENNEY_VILA anterior)
// Chao e caminho: Hexagon Kit. Construcoes e adereços: Fantasy Town Kit. Ambientacao extra: Nature Kit.
// Ignora pastas com "KayKit", "Medieval_Hexagon", "Adventurers", "Quaternius".
// Relatorio: Assets/kenney_vila_relatorio.txt
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class KenneyVilaBuilder
{
    // tile do Hexagon Kit = 1.0 de largura; SCALE 3 = mesma proporcao do seu mapa anterior depois de escalar o grupo em 1.5
    const float SCALE = 3f;
    const float K = SCALE / 2f;                 // o plano foi gerado com tile = 2.0; K converte para o mundo
    // trecho do caminho/pasta do Fantasy Town Kit (se a sua pasta tiver outro nome, troque aqui)
    const string FTK = "fantasy";
    // as pecas de parede tem a "frente" (porta/janela) virada para +X. Se ficarem viradas para dentro, troque para true
    const bool WALL_FRONT_IS_MINUS_X = false;

    static readonly float ROW = Mathf.Sqrt(3f);
    static float PX(int q, int r) { return K * (2f * q + r); }
    static float PZ(int r) { return K * ROW * r; }

    static Dictionary<string, List<string>> paths;
    static Dictionary<string, string> norm;
    static Dictionary<string, GameObject> chosen;
    static HashSet<string> missing;
    static StringBuilder rep;
    static float gH, stoneH;                       // alturas medidas (sem escala)
    static Transform tGround, tSea, tPath, tBuild, tNature, tGame, tSpots, tWps;

    static readonly string[] EXT = { ".fbx", ".obj", ".glb", ".gltf", ".dae", ".blend", ".prefab" };
    static int Rank(string p) { return System.Array.IndexOf(EXT, Path.GetExtension(p).ToLowerInvariant()); }
    static string Norm(string n) { return n.ToLowerInvariant().Replace('_', '-'); }
    static float F(string s) { return float.Parse(s, CultureInfo.InvariantCulture); }
    static Transform Group(Transform parent, string name)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }
    static Transform Sub(Transform parent, string name)
    {
        var t = parent.Find(name);
        return t != null ? t : Group(parent, name);
    }

    // ---------------------------------------------------------------- assets
    static void LoadAssets()
    {
        paths = new Dictionary<string, List<string>>();
        foreach (string g in AssetDatabase.FindAssets("t:GameObject"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (!p.StartsWith("Assets/")) continue;
            string lp = p.ToLowerInvariant();
            if (lp.Contains("kaykit") || lp.Contains("medieval_hexagon") || lp.Contains("adventurers") || lp.Contains("quaternius")) continue;
            if (Rank(p) < 0) continue;
            string n = Path.GetFileNameWithoutExtension(p);
            List<string> l;
            if (!paths.TryGetValue(n, out l)) { l = new List<string>(); paths[n] = l; }
            l.Add(p);
        }
        foreach (var l in paths.Values) l.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
        norm = new Dictionary<string, string>();
        foreach (var n in paths.Keys.OrderBy(x => x)) { string k = Norm(n); if (!norm.ContainsKey(k)) norm[k] = n; }
        chosen = new Dictionary<string, GameObject>();
    }

    static Bounds Measure(GameObject asset)
    {
        var g = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        g.transform.position = Vector3.zero; g.transform.rotation = Quaternion.identity; g.transform.localScale = Vector3.one;
        var rs = g.GetComponentsInChildren<Renderer>();
        var b = new Bounds(Vector3.zero, Vector3.one);
        if (rs.Length > 0) { b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); }
        Object.DestroyImmediate(g);
        return b;
    }
    static bool ExpectHex(string n)
    {
        return n == "grass" || n == "dirt" || n == "sand" || n == "stone" || n == "water"
            || n.StartsWith("grass-") || n.StartsWith("stone-") || n.StartsWith("sand-") || n.StartsWith("water-") || n.StartsWith("dirt-");
    }
    static bool HexFit(Bounds b) { return Mathf.Abs(b.size.x - 1f) < 0.08f && Mathf.Abs(b.size.z - 1.1547f) < 0.08f; }

    // "ftk:nome" = peca do Fantasy Town Kit (varios packs tem "wall", "tree"... com o mesmo nome)
    static string Strip(string raw, out string hint)
    {
        if (raw.StartsWith("ftk:")) { hint = FTK; return raw.Substring(4); }
        hint = null;
        return raw;
    }

    static GameObject Get(string name, string hint)
    {
        string key = (hint ?? "") + "|" + name;
        GameObject go;
        if (chosen.TryGetValue(key, out go)) return go;
        string real = name;
        if (!paths.ContainsKey(real))
        {
            if (!norm.TryGetValue(Norm(name), out real))
            {
                chosen[key] = null;
                if (missing.Add(name)) Debug.LogWarning("[KenneyVila] Peca nao encontrada: " + name);
                return null;
            }
        }
        var list = paths[real];
        string pick = list[0];
        if (!string.IsNullOrEmpty(hint))
        {
            foreach (var p in list) if (p.ToLowerInvariant().Contains(hint)) { pick = p; break; }
        }
        else if (list.Count > 1 && ExpectHex(name))
        {
            foreach (var p in list)
            {
                var a = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (a != null && HexFit(Measure(a))) { pick = p; break; }
            }
        }
        go = AssetDatabase.LoadAssetAtPath<GameObject>(pick);
        chosen[key] = go;
        return go;
    }
    static GameObject Get(string name) { return Get(name, null); }

    // peca com pivo conhecido (tiles do Hexagon Kit e decalques de caminho)
    static GameObject Place(string name, float x, float y, float z, float yaw, Transform parent, float mult)
    {
        var a = Get(name);
        if (a == null) return null;
        var g = (GameObject)PrefabUtility.InstantiatePrefab(a);
        g.transform.SetParent(parent, false);
        g.transform.position = new Vector3(x, y, z);
        g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        g.transform.localScale = Vector3.one * (SCALE * mult);
        return g;
    }

    static bool WorldBounds(GameObject g, out Bounds b)
    {
        b = new Bounds();
        bool has = false;
        foreach (var r in g.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return has;
    }

    // peca de qualquer pack, SEM depender do pivo: o centro (x,z) da caixa da peca vai para (cx,cz) e a base fica em baseY
    static GameObject PlaceFit(string raw, float cx, float baseY, float cz, float yaw, Transform parent, float mult)
    {
        string hint;
        string name = Strip(raw, out hint);
        var a = Get(name, hint);
        if (a == null) return null;
        var g = (GameObject)PrefabUtility.InstantiatePrefab(a);
        g.transform.SetParent(parent, false);
        g.transform.position = Vector3.zero;
        g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        g.transform.localScale = Vector3.one * (SCALE * mult);
        Bounds b;
        if (WorldBounds(g, out b))
            g.transform.position += new Vector3(cx - b.center.x, baseY - b.min.y, cz - b.center.z);
        else
            g.transform.position = new Vector3(cx, baseY, cz);
        return g;
    }

    // topo de uma pilha de n tiles + 1
    static float TopY(int n) { return (n + 1) * gH * SCALE; }

    // ---------------------------------------------------------------- deteccao de conexoes do caminho
    // mascara de conexoes (bit k = borda k, na direcao k*60 graus a partir de +X para +Z) de um decalque hexagonal de largura 1.0
    static int DetectMask(string name)
    {
        var a = Get(name);
        if (a == null) return 0;
        var g = (GameObject)PrefabUtility.InstantiatePrefab(a);
        g.transform.position = Vector3.zero; g.transform.rotation = Quaternion.identity; g.transform.localScale = Vector3.one;
        int mask = 0;
        foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;
            var verts = mesh.vertices;
            var M = mf.transform.localToWorldMatrix;
            for (int i = 0; i < verts.Length; i++)
            {
                var w = M.MultiplyPoint3x4(verts[i]);
                for (int k = 0; k < 6; k++)
                {
                    float ang = k * 60f * Mathf.Deg2Rad;
                    float cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                    float dot = w.x * cs + w.z * sn;
                    float lat = Mathf.Abs(-w.x * sn + w.z * cs);
                    if (dot < 0.40f || lat > 0.26f) continue;
                    mask |= 1 << k;
                }
            }
        }
        Object.DestroyImmediate(g);
        return mask;
    }
    // peca girada em yaw = 60*r tem as conexoes nas direcoes (k - r)
    static int RotMask(int m, int r)
    {
        int o = 0;
        for (int k = 0; k < 6; k++) if ((m & (1 << k)) != 0) o |= 1 << (((k - r) % 6 + 6) % 6);
        return o;
    }
    static int Bits(int m) { int n = 0; for (int k = 0; k < 6; k++) if ((m & (1 << k)) != 0) n++; return n; }
    static string MaskStr(int m) { return System.Convert.ToString(m, 2).PadLeft(6, '0'); }

    static Dictionary<string, int> DetectSet(string prefix, int bits, params string[] exclude)
    {
        var d = new Dictionary<string, int>();
        foreach (var n in paths.Keys.OrderBy(x => x))
        {
            if (!n.StartsWith(prefix)) continue;
            if (exclude.Any(e => n.Contains(e))) continue;
            int m = DetectMask(n);
            rep.AppendLine(n + "  mascara=" + MaskStr(m) + "  bordas=" + Bits(m));
            if (Bits(m) == bits) d[n] = m;
        }
        return d;
    }
    static string FindTile(Dictionary<string, int> masks, int want, out int rot)
    {
        foreach (var kv in masks.OrderBy(x => x.Key.Length).ThenBy(x => x.Key))
            for (int r = 0; r < 6; r++)
                if (RotMask(kv.Value, r) == want) { rot = r; return kv.Key; }
        rot = 0; return null;
    }

    // ---------------------------------------------------------------- casas (pecas modulares do Fantasy Town Kit)
    // modulo = 1.0 do kit = SCALE no mundo. Frente (porta) da casa = +X local. W = profundidade, D = largura da fachada (em modulos)
    static readonly string[] ST_WIN = { "wall-window-glass", "wall-window-small", "wall-window-shutters", "wall-window-round" };
    static readonly string[] WD_WIN = { "wall-wood-window-glass", "wall-wood-window-small", "wall-wood-window-shutters", "wall-wood-window-round" };

    static void KindInfo(string k, int seed, out int W, out int D, out int floors, out string roof, out bool woodTop)
    {
        woodTop = false; roof = "roof-point"; floors = 1; W = 2; D = 2;
        switch (k)
        {
            case "casarao": W = 4; D = 4; floors = 2; roof = "roof-high-point"; woodTop = true; break;
            case "casa2": W = 2; D = 2; floors = 1; break;
            case "casa2b": W = 2; D = 2; floors = 2; woodTop = true; break;
            case "casa4": W = 2; D = 4; floors = 1; break;
            case "casa4b": W = 2; D = 4; floors = 2; woodTop = true; break;
            case "moinho": W = 2; D = 2; floors = 3; roof = "roof-high-point"; woodTop = true; break;
        }
        if (k.StartsWith("casa") && k != "casarao" && seed % 3 == 0) roof = "roof-high-point";   // alguns telhados vermelhos e altos
    }

    static void BuildHouse(string kind, float cxPlan, float czPlan, float yaw, int seed, Transform parent)
    {
        int W, D, floors; string roof; bool woodTop;
        KindInfo(kind, seed, out W, out D, out floors, out roof, out woodTop);
        var rnd = new System.Random(seed);
        float M = SCALE;
        float baseY = TopY(0);

        var root = new GameObject(kind + "_" + seed);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cxPlan * K, baseY, czPlan * K);
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Transform rt = root.transform;

        float flip = WALL_FRONT_IS_MINUS_X ? 180f : 0f;
        int doorIdx = Mathf.Clamp(D / 2 - (seed % 2), 0, D - 1);

        for (int f = 0; f < floors; f++)
        {
            bool stone = !(woodTop && f >= 1);
            string plain = stone ? "ftk:wall" : "ftk:wall-wood";
            string[] wins = stone ? ST_WIN : WD_WIN;
            float ly = f * M;

            // 4 lados: 0 = +X (frente), 1 = +Z, 2 = -X, 3 = -Z
            for (int side = 0; side < 4; side++)
            {
                int n = (side % 2 == 0) ? D : W;
                for (int i = 0; i < n; i++)
                {
                    float lx, lz, py;
                    if (side == 0) { lx = W * M * 0.5f; lz = (-D * 0.5f + i + 0.5f) * M; py = 0f; }
                    else if (side == 2) { lx = -W * M * 0.5f; lz = (-D * 0.5f + i + 0.5f) * M; py = 180f; }
                    else if (side == 1) { lz = D * M * 0.5f; lx = (-W * 0.5f + i + 0.5f) * M; py = 270f; }
                    else { lz = -D * M * 0.5f; lx = (-W * 0.5f + i + 0.5f) * M; py = 90f; }

                    string piece;
                    if (f == 0 && side == 0 && i == doorIdx) piece = stone ? "ftk:wall-door" : "ftk:wall-wood-door";
                    else if (f == 1 && side == 0 && i == doorIdx) piece = "ftk:" + wins[0];
                    else if (rnd.NextDouble() < 0.62) piece = "ftk:" + wins[rnd.Next(wins.Length)];
                    else piece = plain;

                    Vector3 w = rt.TransformPoint(new Vector3(lx, ly, lz));
                    PlaceFit(piece, w.x, w.y, w.z, yaw + py + flip, rt, 1f);
                }
            }
            // pilares nos 4 cantos
            string pil = stone ? "ftk:pillar-stone" : "ftk:pillar-wood";
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                {
                    Vector3 w = rt.TransformPoint(new Vector3(sx * W * M * 0.5f, ly, sz * D * M * 0.5f));
                    PlaceFit(pil, w.x, w.y, w.z, yaw, rt, 1f);
                }
        }

        // telhado: uma piramide por bloco de 2x2 modulos (nao depende da orientacao da peca)
        for (int bx = 0; bx < W / 2; bx++)
            for (int bz = 0; bz < D / 2; bz++)
            {
                float lx = (-W * 0.5f + 2 * bx + 1) * M;
                float lz = (-D * 0.5f + 2 * bz + 1) * M;
                Vector3 w = rt.TransformPoint(new Vector3(lx, floors * M, lz));
                PlaceFit("ftk:" + roof, w.x, w.y, w.z, yaw, rt, 2f);
            }

        // moinho: helices na lateral (+Z local)
        if (kind == "moinho")
        {
            float hc = (floors - 0.35f) * M;                   // altura do centro das helices
            float half = 3.11f * SCALE * 0.5f;                 // meia altura das helices (peca = 3.11 x 3.11)
            Vector3 w = rt.TransformPoint(new Vector3(0f, 0f, D * M * 0.5f + 0.8f));
            PlaceFit("ftk:windmill", w.x, w.y + hc - half, w.z, yaw + 90f, rt, 1f);
        }

        // colisao simples: uma caixa do tamanho da casa (bloqueia o player)
        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, floors * M * 0.5f, 0f);
        bc.size = new Vector3(W * M, floors * M, D * M);
    }

    // ---------------------------------------------------------------- construcao
    [MenuItem("Tools/Kenney/4 - Construir vilarejo")]
    public static void Build()
    {
        LoadAssets();
        missing = new HashSet<string>();
        rep = new StringBuilder();
        if (Get("grass") == null || Get("water") == null || Get("path-straight") == null)
        {
            Debug.LogError("[KenneyVila] Nao achei grass / water / path-straight (Hexagon Kit) em Assets. Importe a versao FBX do pack.");
            return;
        }
        var chk = Get("wall-door", FTK);
        if (chk == null || !AssetDatabase.GetAssetPath(chk).ToLowerInvariant().Contains(FTK))
        {
            Debug.LogError("[KenneyVila] Nao achei o Fantasy Town Kit. Coloque-o numa pasta com \"" + FTK + "\" no caminho (ex.: Assets/Packs/Kenney_FantasyTown) ou troque a constante FTK no topo do script.");
            return;
        }
        var old = GameObject.Find("KENNEY_VILA");
        if (old != null) Object.DestroyImmediate(old);

        gH = Measure(Get("grass")).size.y;
        stoneH = Get("stone") != null ? Measure(Get("stone")).size.y : gH;
        rep.AppendLine("altura grass=" + gH + " stone=" + stoneH);
        foreach (var n in new[] { "ftk:wall", "ftk:wall-door", "ftk:wall-window-glass", "ftk:wall-wood", "ftk:pillar-stone", "ftk:roof-point", "ftk:roof-high-point", "ftk:windmill", "ftk:fountain-round-detail", "ftk:lantern", "ftk:stall-red" })
        {
            string hint;
            string nm = Strip(n, out hint);
            var a = Get(nm, hint);
            if (a == null) { rep.AppendLine("FALTA: " + n); continue; }
            var b = Measure(a);
            rep.AppendLine(n + ": tamanho " + b.size.x.ToString("F2") + " x " + b.size.y.ToString("F2") + " x " + b.size.z.ToString("F2")
                + "  base em y=" + b.min.y.ToString("F2") + "  centro xz=(" + b.center.x.ToString("F2") + "," + b.center.z.ToString("F2") + ")  arquivo=" + AssetDatabase.GetAssetPath(a));
        }

        rep.AppendLine("--- CAMINHOS ---");
        var paths2 = DetectSet("path-", 2, "square", "start", "end", "intersection", "crossing");
        var paths1 = DetectSet("path-", 1);
        if (paths2.Count == 0) { paths2["path-straight"] = (1 << 3) | 1; paths2["path-corner"] = (1 << 4) | 1; rep.AppendLine("AVISO: deteccao de caminho falhou; usando mascaras padrao (confira o mapa)."); }
        string startDecal = paths1.ContainsKey("path-start") ? "path-start" : (paths1.Keys.FirstOrDefault() ?? "path-end");
        string endDecal = paths1.ContainsKey("path-square-end") ? "path-square-end" : (paths1.ContainsKey("path-end") ? "path-end" : startDecal);

        // hierarquia
        var root = new GameObject("KENNEY_VILA").transform;
        var tTerrain = Group(root, "01_Terreno");
        tGround = Group(tTerrain, "Grama"); tSea = Group(tTerrain, "Agua");
        tPath = Group(root, "02_Caminho");
        tBuild = Group(root, "03_Construcoes");
        tNature = Group(root, "04_Natureza");
        tGame = Group(root, "05_Gameplay");
        tSpots = Group(tGame, "TowerSpots"); tWps = Group(tGame, "Waypoints");

        float xmin = float.MaxValue, xmax = float.MinValue, zmin = float.MaxValue, zmax = float.MinValue;
        var wps = new List<Vector3>();
        var spotList = new List<int[]>();
        Vector3 playerPos = Vector3.zero;
        float groundY = gH * SCALE;

        foreach (var line in PLAN)
        {
            var f = line.Split(';');
            switch (f[0])
            {
                case "T":
                {
                    int q = int.Parse(f[1]), r = int.Parse(f[2]), n = int.Parse(f[3]);
                    string name = f[4]; float yaw = F(f[5]);
                    float x = PX(q, r), z = PZ(r);
                    bool isWater = name.StartsWith("water");
                    if (!isWater)
                    {
                        xmin = Mathf.Min(xmin, x); xmax = Mathf.Max(xmax, x); zmin = Mathf.Min(zmin, z); zmax = Mathf.Max(zmax, z);
                        for (int j = 0; j < n; j++) Place(Get("dirt") != null ? "dirt" : "grass", x, j * gH * SCALE, z, 0f, tGround, 1f);
                    }
                    Place(name, x, isWater ? 0f : n * gH * SCALE, z, yaw, isWater ? tSea : tGround, 1f);
                    break;
                }
                case "R":
                case "RS":
                case "RE":
                {
                    int q = int.Parse(f[1]), r = int.Parse(f[2]);
                    float x = PX(q, r), z = PZ(r);
                    int rot = 0; string tile;
                    if (f[0] == "R")
                    {
                        int inD = int.Parse(f[3]), outD = int.Parse(f[4]);
                        tile = FindTile(paths2, (1 << inD) | (1 << outD), out rot);
                        if (tile == null) { rep.AppendLine("SEM PECA de caminho " + inD + "-" + outD + " em " + q + "," + r); tile = "path-straight"; rot = 0; }
                    }
                    else
                    {
                        int e = int.Parse(f[3]);
                        tile = f[0] == "RS" ? startDecal : endDecal;
                        int m1 = DetectMask(tile);
                        for (int rr = 0; rr < 6; rr++) if (RotMask(m1, rr) == (1 << e)) { rot = rr; break; }
                    }
                    Place(tile, x, groundY + 0.005f, z, rot * 60f, tPath, 1f);
                    break;
                }
                case "S":
                    spotList.Add(new[] { int.Parse(f[1]), int.Parse(f[2]), int.Parse(f[3]) });
                    break;
                case "H":
                {
                    // H;tipo;x;z;giro;semente
                    string kind = f[1];
                    Transform par = Sub(tBuild, kind == "casarao" ? "Casarao" : (kind == "moinho" ? "Moinho_Base" : "Casas"));
                    BuildHouse(kind, F(f[2]), F(f[3]), F(f[4]), int.Parse(f[5]), par);
                    break;
                }
                case "O":
                {
                    // O;grupo;nome;x;z;pilha;giro;escala
                    string grp = f[1];
                    int n = int.Parse(f[5]);
                    Transform par;
                    if (grp == "Praca") par = Sub(tBuild, "Praca");
                    else par = Sub(tNature, grp == "Props" ? "Props" : "Arvores_Rochas");
                    PlaceFit(f[2], F(f[3]) * K, TopY(n), F(f[4]) * K, F(f[6]), par, F(f[7]));
                    break;
                }
                case "P":
                    wps.Add(new Vector3(F(f[1]) * K, groundY + 0.05f, F(f[2]) * K));
                    break;
                case "PS":
                    playerPos = new Vector3(F(f[1]) * K, groundY, F(f[2]) * K);
                    break;
            }
        }

        // pontos de torre: o tile de pedra ja esta no chao; aqui ficam so o objeto do ponto (com o seu script TowerSpot)
        foreach (var s in spotList)
        {
            var go = new GameObject("TowerSpot_" + s[0].ToString("00"));
            go.transform.SetParent(tSpots, false);
            go.transform.position = new Vector3(PX(s[1], s[2]), stoneH * SCALE, PZ(s[2]));
            go.AddComponent<TowerSpot>();
        }

        for (int i = 0; i < wps.Count; i++)
        {
            var w = new GameObject("WP_" + i.ToString("00"));
            w.transform.SetParent(tWps, false);
            w.transform.position = wps[i];
        }
        var sp = new GameObject("EnemySpawnPoint"); sp.transform.SetParent(tGame, false); sp.transform.position = wps[0];
        var sp2 = new GameObject("SpawnPoint"); sp2.transform.SetParent(tGame, false); sp2.transform.position = wps[0];
        var bp = new GameObject("BasePoint"); bp.transform.SetParent(tGame, false); bp.transform.position = wps[wps.Count - 1];
        var fl = new GameObject("FinishLine"); fl.transform.SetParent(tGame, false); fl.transform.position = wps[wps.Count - 1];
        var fc = fl.AddComponent<SphereCollider>(); fc.isTrigger = true; fc.radius = 2.4f;
        var ps = new GameObject("PlayerStart"); ps.transform.SetParent(tGame, false); ps.transform.position = playerPos;

        var wg = new GameObject("WalkableGround");
        wg.transform.SetParent(tGame, false);
        var bc = wg.AddComponent<BoxCollider>();
        bc.center = new Vector3((xmin + xmax) / 2f, groundY - 0.25f, (zmin + zmax) / 2f);
        bc.size = new Vector3(xmax - xmin + 2f, 0.5f, zmax - zmin + 2f);

        foreach (var t in new[] { tTerrain, tPath, tBuild, tNature })
            foreach (var tr in t.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticEditorFlags.BatchingStatic);

        if (missing.Count > 0) rep.AppendLine("PECAS NAO ENCONTRADAS: " + string.Join(", ", missing.ToArray()));
        File.WriteAllText("Assets/kenney_vila_relatorio.txt", rep.ToString());
        AssetDatabase.Refresh();

        Selection.activeGameObject = root.gameObject;
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.orthographic = false;
            sv.rotation = Quaternion.Euler(55f, 15f, 0f);
            sv.Frame(new Bounds(new Vector3((xmin + xmax) / 2f, 0f, (zmin + zmax) / 2f), new Vector3(xmax - xmin, 4f, zmax - zmin)), false);
        }
        Debug.Log("[KenneyVila] Vilarejo pronto: " + wps.Count + " waypoints, " + spotList.Count + " pontos de torre. Relatorio: Assets/kenney_vila_relatorio.txt");
    }

    // ---------------------------------------------------------------- plano (gerado e conferido por mim)
    // T = tile (q;r;pilha;nome;giro) | R/RS/RE = decalque de caminho | S = ponto de torre | H = casa (tipo;x;z;giro;semente)
    // O = objeto (grupo;nome;x;z;pilha;giro;escala) | P = waypoint | PS = inicio do jogador
    static readonly string[] PLAN = {
        "T;-9;9;0;water-rocks;0",
        "T;-9;10;0;water;0",
        "T;-9;11;0;water;0",
        "T;-9;12;0;water;0",
        "T;-9;13;0;water;0",
        "T;-8;8;0;water;0",
        "T;-8;9;0;water;0",
        "T;-8;10;0;water;0",
        "T;-8;11;0;water;0",
        "T;-8;12;0;water;0",
        "T;-8;13;0;water;0",
        "T;-8;14;0;water-rocks;0",
        "T;-8;15;0;water;0",
        "T;-8;16;0;water;0",
        "T;-8;17;0;water;0",
        "T;-8;18;0;water;0",
        "T;-8;19;0;water;0",
        "T;-7;7;0;water;0",
        "T;-7;8;0;water-rocks;0",
        "T;-7;9;2;stone-mountain;0",
        "T;-7;10;2;stone-mountain;0",
        "T;-7;11;0;sand-rocks;0",
        "T;-7;12;0;water;0",
        "T;-7;13;0;water;0",
        "T;-7;14;0;water;0",
        "T;-7;15;0;water;0",
        "T;-7;16;0;water;0",
        "T;-7;17;0;water;0",
        "T;-7;18;0;water;0",
        "T;-7;19;0;water;0",
        "T;-6;6;0;water;0",
        "T;-6;7;0;water;0",
        "T;-6;8;0;sand-rocks;0",
        "T;-6;9;0;grass;0",
        "T;-6;10;0;grass-forest;240",
        "T;-6;11;0;sand;0",
        "T;-6;12;0;sand;0",
        "T;-6;13;0;sand;0",
        "T;-6;14;0;sand;0",
        "T;-6;15;2;stone-hill;0",
        "T;-6;16;0;stone-hill;0",
        "T;-6;17;2;grass-hill;0",
        "T;-6;18;0;water;0",
        "T;-6;19;0;water;0",
        "T;-5;5;0;water-island;0",
        "T;-5;6;0;water;0",
        "T;-5;7;0;sand-rocks;0",
        "T;-5;8;0;grass;0",
        "T;-5;9;0;grass-forest;120",
        "T;-5;10;0;grass-forest;240",
        "T;-5;11;0;grass-forest;120",
        "T;-5;12;0;grass;0",
        "T;-5;13;0;grass;0",
        "T;-5;14;0;grass;0",
        "T;-5;15;0;grass;0",
        "T;-5;16;0;grass;0",
        "T;-5;17;0;stone-hill;0",
        "T;-5;18;0;water;0",
        "T;-5;19;0;water;0",
        "T;-5;20;0;water;0",
        "T;-4;5;0;water;0",
        "T;-4;6;0;water;0",
        "T;-4;7;2;stone-mountain;0",
        "T;-4;8;0;grass;0",
        "T;-4;9;0;grass;0",
        "T;-4;10;0;grass-forest;300",
        "T;-4;11;0;grass;0",
        "T;-4;12;0;grass;0",
        "T;-4;13;0;grass;0",
        "T;-4;14;0;grass;0",
        "T;-4;15;0;grass;0",
        "T;-4;16;0;grass;0",
        "T;-4;17;0;grass-hill;0",
        "T;-4;18;0;water-rocks;0",
        "T;-4;19;0;water;0",
        "T;-4;20;0;water;0",
        "T;-3;4;0;water;0",
        "T;-3;5;0;water;0",
        "T;-3;6;0;sand;0",
        "T;-3;7;0;grass;0",
        "T;-3;8;0;grass;0",
        "T;-3;9;0;grass-forest;60",
        "T;-3;10;0;grass;0",
        "T;-3;11;0;grass;0",
        "T;-3;12;0;grass;0",
        "T;-3;13;0;grass;0",
        "T;-3;14;0;grass;0",
        "T;-3;15;0;grass;0",
        "T;-3;16;0;grass;0",
        "T;-3;17;0;grass;0",
        "T;-3;18;0;stone-hill;0",
        "T;-3;19;0;water-rocks;0",
        "T;-3;20;0;water;0",
        "T;-2;4;0;water;0",
        "T;-2;5;0;water;0",
        "T;-2;6;2;stone-hill;0",
        "T;-2;7;0;grass;0",
        "T;-2;8;0;grass;0",
        "T;-2;9;0;grass;0",
        "T;-2;10;0;grass;0",
        "T;-2;11;0;grass;0",
        "T;-2;12;0;grass;0",
        "T;-2;13;0;grass;0",
        "T;-2;14;0;grass;0",
        "T;-2;15;0;grass;0",
        "T;-2;16;0;grass;0",
        "T;-2;17;0;grass;0",
        "T;-2;18;0;grass;0",
        "T;-2;19;0;water;0",
        "T;-2;20;0;water;0",
        "T;-2;21;0;water;0",
        "T;-1;3;0;water;0",
        "T;-1;4;0;water;0",
        "T;-1;5;2;grass-hill;0",
        "T;-1;6;0;grass;0",
        "T;-1;7;0;grass;0",
        "T;-1;8;0;grass;0",
        "T;-1;9;0;grass;0",
        "T;-1;10;0;grass;0",
        "T;-1;11;0;grass;0",
        "T;-1;12;0;grass;0",
        "T;-1;13;0;grass;0",
        "T;-1;14;0;grass;0",
        "T;-1;15;0;grass;0",
        "T;-1;16;0;grass;0",
        "T;-1;17;0;grass;0",
        "T;-1;18;0;grass;0",
        "T;-1;19;0;water;0",
        "T;-1;20;0;water;0",
        "T;-1;21;0;water;0",
        "T;0;3;0;water;0",
        "T;0;4;0;water;0",
        "T;0;5;2;grass-hill;0",
        "T;0;6;0;grass;0",
        "T;0;7;0;grass;0",
        "T;0;8;0;grass;0",
        "T;0;9;0;grass;0",
        "T;0;10;0;grass;0",
        "T;0;11;0;grass;0",
        "T;0;12;0;stone;0",
        "T;0;13;0;grass;0",
        "T;0;14;0;grass;0",
        "T;0;15;0;grass;0",
        "T;0;16;0;grass;0",
        "T;0;17;0;grass;0",
        "T;0;18;0;grass;0",
        "T;0;19;0;grass;0",
        "T;0;20;0;water;0",
        "T;0;21;0;water-rocks;0",
        "T;1;3;0;water;0",
        "T;1;4;0;water;0",
        "T;1;5;0;sand-rocks;0",
        "T;1;6;0;grass;0",
        "T;1;7;0;grass;0",
        "T;1;8;0;grass;0",
        "T;1;9;0;grass;0",
        "T;1;10;0;grass;0",
        "T;1;11;0;grass;0",
        "T;1;12;0;grass;0",
        "T;1;13;0;grass;0",
        "T;1;14;0;grass;0",
        "T;1;15;0;grass;0",
        "T;1;16;0;grass;0",
        "T;1;17;0;grass;0",
        "T;1;18;0;grass;0",
        "T;1;19;0;grass;0",
        "T;1;20;0;water-rocks;0",
        "T;1;21;0;water;0",
        "T;2;2;0;water;0",
        "T;2;3;0;water-rocks;0",
        "T;2;4;0;sand-rocks;0",
        "T;2;5;0;grass;0",
        "T;2;6;0;grass;0",
        "T;2;7;0;grass;0",
        "T;2;8;0;grass;0",
        "T;2;9;0;grass;0",
        "T;2;10;0;grass;0",
        "T;2;11;0;grass;0",
        "T;2;12;0;grass;0",
        "T;2;13;0;grass;0",
        "T;2;14;0;grass;0",
        "T;2;15;0;grass;0",
        "T;2;16;0;grass;0",
        "T;2;17;0;grass;0",
        "T;2;18;0;grass;0",
        "T;2;19;0;grass;0",
        "T;2;20;0;water;0",
        "T;2;21;0;water;0",
        "T;3;1;0;water;0",
        "T;3;2;0;water;0",
        "T;3;3;0;water;0",
        "T;3;4;0;grass;0",
        "T;3;5;0;grass;0",
        "T;3;6;0;grass;0",
        "T;3;7;0;grass;0",
        "T;3;8;0;grass;0",
        "T;3;9;0;grass;0",
        "T;3;10;0;grass;0",
        "T;3;11;0;grass;0",
        "T;3;12;0;grass;0",
        "T;3;13;0;grass;0",
        "T;3;14;0;grass;0",
        "T;3;15;0;grass;0",
        "T;3;16;0;grass;0",
        "T;3;17;0;grass;0",
        "T;3;18;0;grass;0",
        "T;3;19;0;grass;0",
        "T;3;20;0;water;0",
        "T;3;21;0;water;0",
        "T;4;0;0;water;0",
        "T;4;1;0;water;0",
        "T;4;2;0;grass;0",
        "T;4;3;0;grass;0",
        "T;4;4;0;grass;0",
        "T;4;5;0;grass;0",
        "T;4;6;0;grass;0",
        "T;4;7;0;grass;0",
        "T;4;8;0;grass;0",
        "T;4;9;0;grass;0",
        "T;4;10;0;grass;0",
        "T;4;11;0;grass;0",
        "T;4;12;0;grass;0",
        "T;4;13;0;grass;0",
        "T;4;14;0;grass;0",
        "T;4;15;0;grass;0",
        "T;4;16;0;grass;0",
        "T;4;17;0;stone;0",
        "T;4;18;0;grass;0",
        "T;4;19;0;grass;0",
        "T;4;20;0;water;0",
        "T;4;21;0;water;0",
        "T;5;-1;0;water;0",
        "T;5;0;0;water-rocks;0",
        "T;5;1;0;grass;0",
        "T;5;2;0;stone;0",
        "T;5;3;0;grass;0",
        "T;5;4;0;grass;0",
        "T;5;5;0;grass;0",
        "T;5;6;0;grass;0",
        "T;5;7;0;grass;0",
        "T;5;8;0;grass;0",
        "T;5;9;0;grass;0",
        "T;5;10;0;stone;0",
        "T;5;11;0;grass;0",
        "T;5;12;0;grass;0",
        "T;5;13;0;grass;0",
        "T;5;14;0;grass;0",
        "T;5;15;0;grass;0",
        "T;5;16;0;grass;0",
        "T;5;17;0;grass;0",
        "T;5;18;0;grass;0",
        "T;5;19;0;grass;0",
        "T;5;20;0;water;0",
        "T;5;21;0;water;0",
        "T;6;-1;0;water;0",
        "T;6;0;0;water;0",
        "T;6;1;0;grass;0",
        "T;6;2;0;grass;0",
        "T;6;3;0;grass;0",
        "T;6;4;0;grass;0",
        "T;6;5;0;grass;0",
        "T;6;6;0;grass;0",
        "T;6;7;0;grass;0",
        "T;6;8;0;grass;0",
        "T;6;9;0;grass;0",
        "T;6;10;0;grass;0",
        "T;6;11;0;grass;0",
        "T;6;12;0;grass;0",
        "T;6;13;0;grass;0",
        "T;6;14;0;grass;0",
        "T;6;15;0;grass;0",
        "T;6;16;0;grass;0",
        "T;6;17;0;grass;0",
        "T;6;18;0;grass;0",
        "T;6;19;0;grass;0",
        "T;6;20;0;water;0",
        "T;6;21;0;water;0",
        "T;7;-1;0;water;0",
        "T;7;0;0;water;0",
        "T;7;1;0;grass;0",
        "T;7;2;0;grass;0",
        "T;7;3;0;grass;0",
        "T;7;4;0;grass;0",
        "T;7;5;0;grass;0",
        "T;7;6;0;grass;0",
        "T;7;7;0;grass;0",
        "T;7;8;0;grass;0",
        "T;7;9;0;grass;0",
        "T;7;10;0;grass;0",
        "T;7;11;0;grass;0",
        "T;7;12;0;grass;0",
        "T;7;13;0;grass;0",
        "T;7;14;0;grass;0",
        "T;7;15;0;grass;0",
        "T;7;16;0;grass;0",
        "T;7;17;0;grass;0",
        "T;7;18;0;grass;0",
        "T;7;19;0;grass;0",
        "T;7;20;0;water;0",
        "T;7;21;0;water;0",
        "T;7;22;0;water-rocks;0",
        "T;8;-1;0;water;0",
        "T;8;0;0;water;0",
        "T;8;1;0;grass;0",
        "T;8;2;0;grass;0",
        "T;8;3;0;grass;0",
        "T;8;4;0;grass;0",
        "T;8;5;0;grass;0",
        "T;8;6;0;grass;0",
        "T;8;7;0;grass;0",
        "T;8;8;0;grass;0",
        "T;8;9;0;grass;0",
        "T;8;10;0;grass;0",
        "T;8;11;0;grass;0",
        "T;8;12;0;grass;0",
        "T;8;13;0;grass;0",
        "T;8;14;0;grass;0",
        "T;8;15;0;grass;0",
        "T;8;16;0;grass;0",
        "T;8;17;0;grass;0",
        "T;8;18;0;grass;0",
        "T;8;19;0;grass;0",
        "T;8;20;0;water-rocks;0",
        "T;8;21;0;water;0",
        "T;8;22;0;water;0",
        "T;9;-1;0;water;0",
        "T;9;0;0;water;0",
        "T;9;1;0;grass;0",
        "T;9;2;0;grass;0",
        "T;9;3;0;grass;0",
        "T;9;4;0;grass;0",
        "T;9;5;0;grass;0",
        "T;9;6;0;grass;0",
        "T;9;7;0;grass;0",
        "T;9;8;0;grass;0",
        "T;9;9;0;grass;0",
        "T;9;10;0;grass;0",
        "T;9;11;0;grass;0",
        "T;9;12;0;grass;0",
        "T;9;13;0;grass;0",
        "T;9;14;0;grass;0",
        "T;9;15;0;grass;0",
        "T;9;16;0;grass;0",
        "T;9;17;0;grass;0",
        "T;9;18;0;grass;0",
        "T;9;19;0;grass;0",
        "T;9;20;0;grass-hill;0",
        "T;9;21;0;water;0",
        "T;9;22;0;water;0",
        "T;10;-1;0;water;0",
        "T;10;0;0;water;0",
        "T;10;1;0;grass;0",
        "T;10;2;0;grass;0",
        "T;10;3;0;grass;0",
        "T;10;4;0;grass;0",
        "T;10;5;0;grass;0",
        "T;10;6;0;grass;0",
        "T;10;7;0;grass;0",
        "T;10;8;0;grass;0",
        "T;10;9;0;grass;0",
        "T;10;10;0;stone;0",
        "T;10;11;0;grass;0",
        "T;10;12;0;grass;0",
        "T;10;13;0;grass;0",
        "T;10;14;0;grass;0",
        "T;10;15;0;grass;0",
        "T;10;16;0;grass;0",
        "T;10;17;0;grass;0",
        "T;10;18;0;grass;0",
        "T;10;19;0;grass;0",
        "T;10;20;0;grass-hill;0",
        "T;10;21;0;water;0",
        "T;10;22;0;water-rocks;0",
        "T;11;-1;0;water;0",
        "T;11;0;0;water;0",
        "T;11;1;0;grass;0",
        "T;11;2;0;grass;0",
        "T;11;3;0;grass;0",
        "T;11;4;0;grass;0",
        "T;11;5;0;grass;0",
        "T;11;6;0;grass;0",
        "T;11;7;0;grass;0",
        "T;11;8;0;grass;0",
        "T;11;9;0;grass;0",
        "T;11;10;0;grass;0",
        "T;11;11;0;grass;0",
        "T;11;12;0;grass;0",
        "T;11;13;0;grass;0",
        "T;11;14;0;grass;0",
        "T;11;15;0;grass;0",
        "T;11;16;0;grass;0",
        "T;11;17;0;grass;0",
        "T;11;18;0;grass;0",
        "T;11;19;0;stone-hill;0",
        "T;11;20;0;water;0",
        "T;11;21;0;water;0",
        "T;12;-2;0;water;0",
        "T;12;-1;0;water;0",
        "T;12;0;0;grass;0",
        "T;12;1;0;grass;0",
        "T;12;2;0;stone;0",
        "T;12;3;0;grass;0",
        "T;12;4;0;grass;0",
        "T;12;5;0;grass;0",
        "T;12;6;0;grass;0",
        "T;12;7;0;grass;0",
        "T;12;8;0;grass;0",
        "T;12;9;0;grass;0",
        "T;12;10;0;grass;0",
        "T;12;11;0;grass;0",
        "T;12;12;0;grass;0",
        "T;12;13;0;grass;0",
        "T;12;14;0;grass;0",
        "T;12;15;0;grass;0",
        "T;12;16;0;grass;0",
        "T;12;17;0;grass;0",
        "T;12;18;0;grass;0",
        "T;12;19;0;stone-hill;0",
        "T;12;20;0;water;0",
        "T;12;21;0;water;0",
        "T;13;-2;0;water-island;0",
        "T;13;-1;0;water;0",
        "T;13;0;0;grass;0",
        "T;13;1;0;grass;0",
        "T;13;2;0;grass;0",
        "T;13;3;0;grass;0",
        "T;13;4;0;grass;0",
        "T;13;5;0;grass;0",
        "T;13;6;0;grass;0",
        "T;13;7;0;grass;0",
        "T;13;8;0;grass;0",
        "T;13;9;0;grass;0",
        "T;13;10;0;grass;0",
        "T;13;11;0;grass;0",
        "T;13;12;0;grass;0",
        "T;13;13;0;grass;0",
        "T;13;14;0;grass;0",
        "T;13;15;0;grass;0",
        "T;13;16;0;grass;0",
        "T;13;17;0;grass;0",
        "T;13;18;0;grass;0",
        "T;13;19;0;grass-hill;0",
        "T;13;20;0;water;0",
        "T;13;21;0;water;0",
        "T;14;-2;0;water;0",
        "T;14;-1;0;water;0",
        "T;14;0;0;grass;0",
        "T;14;1;0;grass;0",
        "T;14;2;0;grass;0",
        "T;14;3;0;grass;0",
        "T;14;4;0;grass;0",
        "T;14;5;0;grass;0",
        "T;14;6;0;grass;0",
        "T;14;7;0;grass;0",
        "T;14;8;0;grass;0",
        "T;14;9;0;grass;0",
        "T;14;10;0;grass;0",
        "T;14;11;0;grass;0",
        "T;14;12;0;grass;0",
        "T;14;13;0;grass;0",
        "T;14;14;0;grass;0",
        "T;14;15;0;grass;0",
        "T;14;16;0;grass;0",
        "T;14;17;0;grass;0",
        "T;14;18;0;grass;0",
        "T;14;19;0;stone-hill;0",
        "T;14;20;0;water;0",
        "T;14;21;0;water;0",
        "T;15;-2;0;water;0",
        "T;15;-1;0;water;0",
        "T;15;0;0;grass;0",
        "T;15;1;0;grass;0",
        "T;15;2;0;grass;0",
        "T;15;3;0;grass;0",
        "T;15;4;0;grass;0",
        "T;15;5;0;grass;0",
        "T;15;6;0;grass;0",
        "T;15;7;0;grass;0",
        "T;15;8;0;grass;0",
        "T;15;9;0;grass;0",
        "T;15;10;0;grass;0",
        "T;15;11;0;grass;0",
        "T;15;12;0;grass;0",
        "T;15;13;0;grass;0",
        "T;15;14;0;grass;0",
        "T;15;15;0;grass;0",
        "T;15;16;0;grass;0",
        "T;15;17;0;grass;0",
        "T;15;18;0;stone-hill;0",
        "T;15;19;0;water;0",
        "T;15;20;0;water-island;0",
        "T;16;-2;0;water;0",
        "T;16;-1;0;water-island;0",
        "T;16;0;0;water;0",
        "T;16;1;0;grass;0",
        "T;16;2;0;grass;0",
        "T;16;3;0;grass;0",
        "T;16;4;0;grass;0",
        "T;16;5;0;grass;0",
        "T;16;6;0;grass;0",
        "T;16;7;0;grass;0",
        "T;16;8;0;grass;0",
        "T;16;9;0;stone;0",
        "T;16;10;0;grass;0",
        "T;16;11;0;grass;0",
        "T;16;12;0;grass;0",
        "T;16;13;0;grass;0",
        "T;16;14;0;grass;0",
        "T;16;15;0;grass;0",
        "T;16;16;0;grass;0",
        "T;16;17;0;stone-hill;0",
        "T;16;18;0;water;0",
        "T;16;19;0;water;0",
        "T;17;-2;0;water;0",
        "T;17;-1;0;water;0",
        "T;17;0;0;water;0",
        "T;17;1;0;grass;0",
        "T;17;2;0;grass;0",
        "T;17;3;0;grass;0",
        "T;17;4;0;grass;0",
        "T;17;5;0;grass;0",
        "T;17;6;0;grass;0",
        "T;17;7;0;grass;0",
        "T;17;8;0;grass;0",
        "T;17;9;0;grass;0",
        "T;17;10;0;grass;0",
        "T;17;11;0;grass;0",
        "T;17;12;0;grass;0",
        "T;17;13;0;grass;0",
        "T;17;14;0;grass;0",
        "T;17;15;0;grass;0",
        "T;17;16;0;grass-hill;0",
        "T;17;17;0;water;0",
        "T;17;18;0;water;0",
        "T;18;-1;0;water;0",
        "T;18;0;0;water;0",
        "T;18;1;0;grass;0",
        "T;18;2;0;grass;0",
        "T;18;3;0;grass;0",
        "T;18;4;0;grass;0",
        "T;18;5;0;grass;0",
        "T;18;6;0;grass;0",
        "T;18;7;0;grass;0",
        "T;18;8;0;grass;0",
        "T;18;9;0;grass;0",
        "T;18;10;0;grass;0",
        "T;18;11;0;grass;0",
        "T;18;12;0;grass;0",
        "T;18;13;0;grass;0",
        "T;18;14;0;grass;0",
        "T;18;15;2;grass-hill;0",
        "T;18;16;0;water;0",
        "T;18;17;0;water;0",
        "T;19;-1;0;water;0",
        "T;19;0;0;water;0",
        "T;19;1;0;grass;0",
        "T;19;2;0;grass;0",
        "T;19;3;0;grass;0",
        "T;19;4;0;grass;0",
        "T;19;5;0;grass;0",
        "T;19;6;0;grass;0",
        "T;19;7;0;grass;0",
        "T;19;8;0;grass;0",
        "T;19;9;0;grass;0",
        "T;19;10;0;grass;0",
        "T;19;11;0;grass;0",
        "T;19;12;0;grass;0",
        "T;19;13;0;grass;0",
        "T;19;14;0;sand-rocks;0",
        "T;19;15;0;water;0",
        "T;19;16;0;water;0",
        "T;20;-1;0;water;0",
        "T;20;0;0;water;0",
        "T;20;1;0;grass;0",
        "T;20;2;0;grass;0",
        "T;20;3;0;grass;0",
        "T;20;4;0;grass;0",
        "T;20;5;0;grass;0",
        "T;20;6;0;grass;0",
        "T;20;7;0;grass;0",
        "T;20;8;0;grass;0",
        "T;20;9;0;grass;0",
        "T;20;10;0;grass;0",
        "T;20;11;0;grass;0",
        "T;20;12;0;grass;0",
        "T;20;13;2;stone-hill;0",
        "T;20;14;0;water;0",
        "T;20;15;0;water;0",
        "T;21;-1;0;water-island;0",
        "T;21;0;0;water;0",
        "T;21;1;0;grass;0",
        "T;21;2;0;grass;0",
        "T;21;3;0;grass;0",
        "T;21;4;0;grass;0",
        "T;21;5;0;grass;0",
        "T;21;6;0;grass;0",
        "T;21;7;0;grass;0",
        "T;21;8;0;grass;0",
        "T;21;9;0;grass;0",
        "T;21;10;0;grass;0",
        "T;21;11;0;grass;0",
        "T;21;12;0;grass;0",
        "T;21;13;2;stone-mountain;0",
        "T;21;14;0;water;0",
        "T;21;15;0;water;0",
        "T;22;-1;0;water;0",
        "T;22;0;0;water;0",
        "T;22;1;0;grass;0",
        "T;22;2;0;grass;0",
        "T;22;3;0;grass;0",
        "T;22;4;0;grass;0",
        "T;22;5;0;grass;0",
        "T;22;6;0;stone;0",
        "T;22;7;0;grass;0",
        "T;22;8;0;grass;0",
        "T;22;9;0;grass;0",
        "T;22;10;0;grass;0",
        "T;22;11;0;grass;0",
        "T;22;12;2;stone-mountain;0",
        "T;22;13;0;water;0",
        "T;22;14;0;water;0",
        "T;23;-1;0;water;0",
        "T;23;0;0;water-rocks;0",
        "T;23;1;0;grass;0",
        "T;23;2;0;grass;0",
        "T;23;3;0;grass;0",
        "T;23;4;0;grass;0",
        "T;23;5;0;grass;0",
        "T;23;6;0;grass;0",
        "T;23;7;0;grass;0",
        "T;23;8;0;grass;0",
        "T;23;9;0;grass;0",
        "T;23;10;0;grass;0",
        "T;23;11;0;grass;0",
        "T;23;12;2;grass-hill;0",
        "T;23;13;0;water;0",
        "T;23;14;0;water;0",
        "T;24;-1;0;water;0",
        "T;24;0;0;water;0",
        "T;24;1;0;water;0",
        "T;24;2;0;grass;0",
        "T;24;3;0;grass;0",
        "T;24;4;0;grass;0",
        "T;24;5;0;grass;0",
        "T;24;6;0;grass;0",
        "T;24;7;0;grass;0",
        "T;24;8;0;grass;0",
        "T;24;9;0;grass;0",
        "T;24;10;0;grass-forest;300",
        "T;24;11;2;stone-hill;0",
        "T;24;12;0;water;0",
        "T;24;13;0;water;0",
        "T;25;-1;0;water;0",
        "T;25;0;0;water;0",
        "T;25;1;0;water;0",
        "T;25;2;0;water;0",
        "T;25;3;0;grass;0",
        "T;25;4;0;grass;0",
        "T;25;5;0;grass;0",
        "T;25;6;0;grass;0",
        "T;25;7;0;grass;0",
        "T;25;8;0;grass;0",
        "T;25;9;0;grass-forest;180",
        "T;25;10;2;stone-mountain;0",
        "T;25;11;0;water;0",
        "T;25;12;0;water-rocks;0",
        "T;26;0;0;water;0",
        "T;26;1;0;water;0",
        "T;26;2;0;water-rocks;0",
        "T;26;3;0;grass;0",
        "T;26;4;0;grass;0",
        "T;26;5;0;grass;0",
        "T;26;6;0;grass;0",
        "T;26;7;0;grass;0",
        "T;26;8;0;grass;0",
        "T;26;9;2;stone-mountain;0",
        "T;26;10;0;water;0",
        "T;26;11;0;water;0",
        "T;27;1;0;water;0",
        "T;27;2;0;water-island;0",
        "T;27;3;0;sand-rocks;0",
        "T;27;4;0;sand;0",
        "T;27;5;0;grass;0",
        "T;27;6;0;sand;0",
        "T;27;7;2;stone-hill;0",
        "T;27;8;0;sand;0",
        "T;27;9;0;water;0",
        "T;27;10;0;water;0",
        "T;28;1;0;water;0",
        "T;28;2;0;water;0",
        "T;28;3;0;water;0",
        "T;28;4;0;sand-rocks;0",
        "T;28;5;0;sand;0",
        "T;28;6;0;water;0",
        "T;28;7;0;water;0",
        "T;28;8;0;water;0",
        "T;28;9;0;water;0",
        "T;29;1;0;water;0",
        "T;29;2;0;water;0",
        "T;29;3;0;water;0",
        "T;29;4;0;water;0",
        "T;29;5;0;water;0",
        "T;29;6;0;water;0",
        "T;29;7;0;water;0",
        "T;29;8;0;water;0",
        "T;30;2;0;water;0",
        "T;30;3;0;water;0",
        "T;30;4;0;water;0",
        "T;30;5;0;water;0",
        "RS;12;16;3",
        "R;11;16;0;2",
        "R;10;17;5;3",
        "R;9;17;0;3",
        "R;8;17;0;4",
        "R;8;16;1;3",
        "R;7;16;0;2",
        "R;6;17;5;3",
        "R;5;17;0;2",
        "R;4;18;5;3",
        "R;3;18;0;4",
        "R;3;17;1;3",
        "R;2;17;0;3",
        "R;1;17;0;4",
        "R;1;16;1;3",
        "R;0;16;0;4",
        "R;0;15;1;3",
        "R;-1;15;0;4",
        "R;-1;14;1;3",
        "R;-2;14;0;4",
        "R;-2;13;1;4",
        "R;-2;12;1;5",
        "R;-1;11;2;5",
        "R;0;10;2;0",
        "R;1;10;3;0",
        "R;2;10;3;0",
        "R;3;10;3;5",
        "R;4;9;2;0",
        "R;5;9;3;0",
        "R;6;9;3;1",
        "R;6;10;4;0",
        "R;7;10;3;0",
        "R;8;10;3;5",
        "R;9;9;2;0",
        "R;10;9;3;5",
        "R;11;8;2;0",
        "R;12;8;3;1",
        "R;12;9;4;0",
        "R;13;9;3;0",
        "R;14;9;3;5",
        "R;15;8;2;5",
        "R;16;7;2;0",
        "R;17;7;3;1",
        "R;17;8;4;0",
        "R;18;8;3;1",
        "R;18;9;4;0",
        "R;19;9;3;0",
        "R;20;9;3;5",
        "R;21;8;2;0",
        "R;22;8;3;5",
        "R;23;7;2;5",
        "R;24;6;2;4",
        "R;24;5;1;4",
        "R;24;4;1;3",
        "R;23;4;0;4",
        "R;23;3;1;3",
        "R;22;3;0;4",
        "R;22;2;1;3",
        "R;21;2;0;3",
        "R;20;2;0;3",
        "R;19;2;0;3",
        "R;18;2;0;3",
        "R;17;2;0;3",
        "R;16;2;0;3",
        "R;15;2;0;3",
        "R;14;2;0;4",
        "R;14;1;1;3",
        "R;13;1;0;3",
        "R;12;1;0;2",
        "R;11;2;5;3",
        "R;10;2;0;3",
        "R;9;2;0;3",
        "R;8;2;0;3",
        "R;7;2;0;3",
        "R;6;2;0;3",
        "RE;5;2;0",
        "S;1;0;12",
        "S;2;16;9",
        "S;3;22;6",
        "S;4;4;17",
        "S;5;5;10",
        "S;6;10;10",
        "S;7;12;2",
        "H;casarao;45.30;27.71;180;1",
        "H;moinho;8.70;3.46;0;2",
        "H;casa2b;22.71;9.80;270;749",
        "H;casa2;24.17;23.73;270;287",
        "H;casa2b;35.74;23.29;270;976",
        "H;casa2b;13.61;11.11;270;284",
        "H;casa2b;35.54;8.18;90;207",
        "O;Nature;ftk:tree;-2.60;15.66;0;64;0.75",
        "O;Nature;tree_thin;-3.60;15.99;0;128;0.80",
        "O;Nature;rock_smallF;1.73;24.38;0;184;0.90",
        "O;Props;flower_redB;-2.53;13.93;0;248;1.00",
        "O;Props;flower_yellowC;-2.65;13.82;0;217;1.00",
        "O;Props;flower_redA;-1.95;13.22;0;290;1.00",
        "O;Nature;tree_pineTallB;1.78;21.45;0;184;0.80",
        "O;Nature;ftk:tree;1.70;21.12;0;332;0.75",
        "O;Nature;tree_default_dark;4.66;25.86;0;307;0.80",
        "O;Nature;tree_thin;4.84;25.44;0;6;0.80",
        "O;Nature;ftk:tree-crooked;5.32;26.30;0;356;0.75",
        "O;Props;flower_redA;6.25;27.03;0;40;1.00",
        "O;Props;flower_redA;6.09;27.28;0;116;1.00",
        "O;Nature;tree_pineTallB;1.08;15.91;0;230;0.80",
        "O;Nature;tree_oak;1.19;15.12;0;191;0.80",
        "O;Nature;ftk:tree-crooked;0.54;15.43;0;254;0.75",
        "O;Nature;tree_pineRoundA;2.89;19.25;0;337;0.80",
        "O;Nature;tree_oak_dark;3.24;19.73;0;269;0.80",
        "O;Nature;ftk:tree;3.30;18.53;0;269;0.75",
        "O;Nature;tree_simple;6.72;25.90;0;190;0.80",
        "O;Props;plant_bushDetailed;6.71;25.51;0;303;0.90",
        "O;Nature;rock_smallF;7.61;27.48;0;126;0.90",
        "O;Props;plant_bushLarge;8.09;27.47;0;12;0.90",
        "O;Nature;tree_simple;0.64;12.27;0;246;0.80",
        "O;Props;plant_bushLarge;1.13;12.44;0;70;0.90",
        "O;Nature;ftk:tree-crooked;2.44;13.77;0;204;0.75",
        "O;Nature;tree_oak_dark;2.55;13.74;0;300;0.80",
        "O;Nature;tree_oak;1.57;14.06;0;260;0.80",
        "O;Nature;tree_detailed;3.47;17.32;0;95;0.80",
        "O;Nature;tree_default_dark;4.00;17.90;0;244;0.80",
        "O;Nature;tree_oak_dark;5.03;18.82;0;212;0.80",
        "O;Nature;tree_fat;5.38;19.02;0;208;0.80",
        "O;Nature;tree_oak_dark;4.60;18.68;0;148;0.80",
        "O;Props;flower_redB;7.88;24.56;0;357;0.90",
        "O;Props;flower_purpleC;3.26;12.10;0;199;1.00",
        "O;Props;flower_yellowB;2.87;12.74;0;110;1.00",
        "O;Props;flower_yellowB;3.56;11.69;0;34;1.00",
        "O;Props;flower_redB;2.59;12.55;0;101;1.00",
        "O;Nature;tree_fat;3.33;13.67;0;139;0.80",
        "O;Nature;ftk:tree;3.71;13.92;0;14;0.75",
        "O;Props;flower_purpleA;6.68;19.58;0;266;0.90",
        "O;Props;flower_yellowC;10.64;26.51;0;189;0.90",
        "O;Props;flower_redA;12.13;28.07;0;329;0.90",
        "O;Props;flower_purpleA;4.10;9.68;0;20;1.00",
        "O;Props;flower_redC;4.61;10.74;0;48;1.00",
        "O;Nature;tree_small;5.29;12.09;0;160;0.80",
        "O;Props;plant_flatTall;5.09;11.91;0;91;0.90",
        "O;Props;flower_purpleB;15.33;30.04;0;231;0.90",
        "O;Nature;ftk:tree;16.22;30.96;0;86;0.75",
        "O;Nature;tree_fat;15.56;31.20;0;287;0.80",
        "O;Nature;tree_pineSmallA;15.66;31.48;0;122;0.80",
        "O;Nature;tree_simple;6.67;12.08;0;197;0.80",
        "O;Props;plant_bushLarge;6.78;12.09;0;195;0.90",
        "O;Props;flower_purpleB;7.83;14.00;0;78;1.00",
        "O;Props;flower_purpleB;7.47;14.25;0;325;1.00",
        "O;Props;flower_yellowA;10.77;19.05;0;302;0.90",
        "O;Nature;ftk:tree;18.73;32.36;0;64;0.75",
        "O;Nature;tree_simple;19.54;32.86;0;357;0.80",
        "O;Nature;stone_largeB;8.38;10.28;0;114;0.90",
        "O;Props;plant_bushLarge;7.91;10.90;0;152;0.90",
        "O;Props;flower_purpleC;9.08;12.38;0;287;1.00",
        "O;Props;flower_purpleA;8.98;12.35;0;305;1.00",
        "O;Props;flower_redC;14.23;21.32;0;224;1.00",
        "O;Props;flower_purpleB;13.79;20.69;0;151;1.00",
        "O;Props;flower_yellowA;14.18;20.65;0;245;1.00",
        "O;Nature;ftk:tree-crooked;21.31;32.80;0;82;0.75",
        "O;Nature;tree_small;20.54;32.77;0;302;0.80",
        "O;Nature;tree_default_dark;9.52;9.07;0;53;0.80",
        "O;Props;plant_bushDetailed;9.27;8.59;0;113;0.90",
        "O;Props;flower_redB;12.80;15.15;0;92;0.90",
        "O;Nature;tree_oak_dark;16.20;20.80;0;64;0.80",
        "O;Props;plant_bushSmall;15.61;20.69;0;17;0.90",
        "O;Props;flower_redC;18.52;23.75;0;29;1.00",
        "O;Props;flower_purpleA;17.71;24.26;0;338;1.00",
        "O;Props;flower_yellowC;21.87;31.65;0;256;0.90",
        "O;Nature;ftk:tree-crooked;18.06;21.24;0;311;0.75",
        "O;Nature;tree_oak;17.77;21.34;0;212;0.80",
        "O;Nature;tree_pineTallB;18.56;20.93;0;38;0.80",
        "O;Props;flower_purpleB;22.33;27.93;0;23;0.90",
        "O;Props;flower_purpleB;25.30;32.73;0;160;0.90",
        "O;Props;flower_redA;18.30;16.75;0;68;0.90",
        "O;Props;flower_redC;14.80;5.02;0;4;0.90",
        "O;Nature;tree_pineRoundA;30.82;33.24;0;183;0.80",
        "O;Nature;tree_thin;31.19;32.28;0;260;0.80",
        "O;Nature;tree_detailed;30.42;32.83;0;45;0.80",
        "O;Nature;tree_thin;17.74;7.02;0;146;0.80",
        "O;Nature;tree_oak;18.28;6.82;0;265;0.80",
        "O;Props;flower_purpleB;21.49;13.66;0;338;0.90",
        "O;Props;flower_yellowB;24.20;14.01;0;17;1.00",
        "O;Props;flower_yellowB;24.22;13.64;0;64;1.00",
        "O;Props;flower_purpleA;23.78;13.72;0;71;1.00",
        "O;Props;flower_purpleC;23.29;13.66;0;224;1.00",
        "O;Nature;rock_smallB;27.81;21.37;0;14;0.90",
        "O;Props;plant_bushLarge;28.26;20.34;0;38;0.90",
        "O;Nature;rock_smallF;30.43;24.14;0;67;0.90",
        "O;Props;plant_flatTall;30.47;24.15;0;12;0.90",
        "O;Nature;tree_simple;34.67;33.51;0;262;0.80",
        "O;Nature;tree_oak_dark;35.56;32.51;0;205;0.80",
        "O;Props;flower_yellowA;19.42;1.53;0;246;0.90",
        "O;Props;flower_purpleB;25.67;14.30;0;163;0.90",
        "O;Props;flower_yellowB;28.18;16.66;0;353;0.90",
        "O;Nature;rock_smallA;29.94;21.49;0;13;0.90",
        "O;Props;plant_bushSmall;30.24;20.71;0;63;0.90",
        "O;Props;flower_yellowC;30.91;21.83;0;137;1.00",
        "O;Props;flower_redB;30.88;21.94;0;8;1.00",
        "O;Props;flower_purpleC;30.96;23.00;0;65;1.00",
        "O;Props;flower_yellowC;35.44;31.30;0;255;0.90",
        "O;Nature;tree_thin;37.38;32.82;0;38;0.80",
        "O;Nature;tree_small;37.51;33.16;0;11;0.80",
        "O;Nature;stone_largeB;27.30;12.50;0;256;0.90",
        "O;Props;plant_bushLarge;27.11;11.76;0;291;0.90",
        "O;Props;flower_purpleA;31.68;18.97;0;38;1.00",
        "O;Props;flower_redB;31.49;19.37;0;30;1.00",
        "O;Nature;tree_default_dark;39.41;32.47;0;315;0.80",
        "O;Nature;tree_simple;38.63;32.28;0;41;0.80",
        "O;Nature;tree_pineRoundA;39.25;33.05;0;304;0.80",
        "O;Props;flower_purpleA;28.66;11.85;0;31;0.90",
        "O;Props;flower_purpleB;31.02;16.18;0;138;0.90",
        "O;Nature;tree_pineTallB;33.13;18.50;0;319;0.80",
        "O;Nature;tree_pineTallB;33.19;19.24;0;160;0.80",
        "O;Nature;tree_pineTallB;32.66;18.64;0;342;0.80",
        "O;Props;flower_redB;24.09;-0.34;0;40;0.90",
        "O;Nature;tree_small;27.29;5.77;0;213;0.80",
        "O;Nature;tree_oak_dark;26.84;4.74;0;316;0.80",
        "O;Props;flower_purpleB;29.58;8.68;0;107;1.00",
        "O;Props;flower_yellowC;29.29;8.84;0;12;1.00",
        "O;Props;flower_redA;28.71;8.78;0;109;1.00",
        "O;Props;flower_purpleB;29.29;8.74;0;16;1.00",
        "O;Nature;ftk:rock-wide;29.61;9.77;0;152;0.90",
        "O;Props;plant_bushSmall;29.66;10.37;0;76;0.90",
        "O;Props;flower_yellowB;26.52;-0.04;0;289;0.90",
        "O;Props;flower_purpleC;28.71;5.37;0;196;0.90",
        "O;Nature;tree_detailed;30.69;8.79;0;359;0.80",
        "O;Props;plant_flatTall;30.89;9.31;0;130;0.90",
        "O;Props;flower_yellowA;33.17;12.26;0;216;0.90",
        "O;Props;flower_redA;35.79;17.69;0;92;0.90",
        "O;Props;mushroom_tanGroup;35.63;11.96;0;79;1.10",
        "O;Props;stump_round;34.58;12.08;0;316;0.90",
        "O;Props;flower_redA;35.56;13.44;0;77;0.90",
        "O;Props;flower_yellowC;38.22;17.11;0;323;0.90",
        "O;Nature;tree_pineTallB;40.37;21.40;0;354;0.80",
        "O;Nature;tree_pineTallB;40.22;20.34;0;136;0.80",
        "O;Nature;ftk:tree-crooked;40.17;21.14;0;80;0.75",
        "O;Props;flower_purpleB;38.49;15.81;0;314;0.90",
        "O;Nature;tree_pineSmallA;40.82;19.26;0;321;0.80",
        "O;Nature;tree_pineRoundA;40.57;19.37;0;225;0.80",
        "O;Nature;tree_simple;41.61;21.18;0;264;0.80",
        "O;Nature;tree_detailed;42.13;20.47;0;193;0.80",
        "O;Props;flower_purpleA;33.30;1.70;0;94;0.90",
        "O;Props;flower_purpleB;40.17;13.23;0;242;0.90",
        "O;Nature;ftk:tree;41.69;17.71;0;230;0.75",
        "O;Nature;tree_pineSmallA;42.37;17.38;0;341;0.80",
        "O;Nature;tree_oak_dark;43.70;19.06;0;128;0.80",
        "O;Nature;tree_default_dark;43.31;19.31;0;125;0.80",
        "O;Nature;tree_default_dark;42.64;19.26;0;260;0.80",
        "O;Nature;tree_default_dark;44.41;20.39;0;188;0.80",
        "O;Props;plant_bushLarge;44.27;20.84;0;230;0.90",
        "O;Props;flower_purpleB;40.19;11.06;0;176;0.90",
        "O;Props;flower_yellowA;44.38;16.90;0;108;0.90",
        "O;Nature;tree_thin;45.16;19.35;0;162;0.80",
        "O;Nature;tree_small;44.60;19.01;0;268;0.80",
        "O;Props;flower_purpleC;46.02;20.29;0;83;1.00",
        "O;Props;flower_purpleB;46.18;21.00;0;320;1.00",
        "O;Nature;rock_smallB;39.42;6.80;0;165;0.90",
        "O;Props;plant_bushDetailed;40.50;6.45;0;94;0.90",
        "O;Props;flower_redA;41.60;10.15;0;26;0.90",
        "O;Props;flower_purpleA;47.06;18.75;0;20;1.00",
        "O;Props;flower_redB;46.64;18.90;0;17;1.00",
        "O;Props;flower_redB;47.34;18.49;0;332;1.00",
        "O;Nature;ftk:rock-small;42.51;6.60;0;178;0.90",
        "O;Props;plant_bushLarge;42.37;7.47;0;261;0.90",
        "O;Props;mushroom_red;42.87;8.92;0;259;1.10",
        "O;Props;log;43.05;9.33;0;170;0.90",
        "O;Nature;tree_oak_dark;44.27;10.92;0;287;0.80",
        "O;Props;plant_bushLarge;44.12;9.89;0;53;0.90",
        "O;Props;flower_yellowA;44.42;12.36;0;206;0.90",
        "O;Props;flower_purpleB;45.78;14.06;0;336;0.90",
        "O;Props;flower_purpleA;47.58;17.65;0;306;0.90",
        "O;Nature;tree_default_dark;48.84;18.76;0;120;0.80",
        "O;Props;plant_flatTall;48.69;19.55;0;155;0.90",
        "O;Nature;rock_smallA;44.44;7.27;0;171;0.90",
        "O;Props;plant_flatTall;43.40;6.57;0;248;0.90",
        "O;Nature;rock_smallB;47.69;11.93;0;33;0.90",
        "O;Props;plant_flatTall;47.20;12.04;0;281;0.90",
        "O;Props;flower_purpleB;47.93;14.14;0;63;0.90",
        "O;Props;flower_purpleB;50.05;17.00;0;309;0.90",
        "O;Nature;rock_smallB;52.31;21.41;0;79;0.90",
        "O;Props;plant_bushSmall;52.00;21.50;0;275;0.90",
        "O;Props;flower_redA;45.10;5.38;0;280;0.90",
        "O;Props;flower_yellowA;46.27;6.54;0;256;0.90",
        "O;Props;flower_purpleC;49.41;12.31;0;78;0.90",
        "O;Props;flower_purpleA;51.68;17.11;0;82;1.00",
        "O;Props;flower_redA;52.04;17.09;0;262;1.00",
        "O;Props;flower_purpleB;51.85;17.17;0;15;1.00",
        "O;Props;flower_yellowB;52.01;16.78;0;354;1.00",
        "O;Nature;rock_smallB;54.32;20.70;0;237;0.90",
        "O;Props;plant_bushSmall;53.71;21.02;0;68;0.90",
        "O;Nature;tree_detailed;54.09;16.96;0;70;0.80",
        "O;Nature;tree_small;53.66;17.67;0;232;0.80",
        "O;Nature;tree_simple;54.46;19.37;0;304;0.80",
        "O;Nature;tree_pineSmallA;55.58;18.59;0;97;0.80",
        "O;Nature;tree_simple;55.23;18.95;0;301;0.80",
        "O;Props;flower_yellowA;46.81;1.57;0;285;0.90",
        "O;Props;flower_purpleA;52.48;9.95;0;103;0.90",
        "O;Nature;tree_pineRoundA;55.47;15.64;0;9;0.80",
        "O;Nature;tree_oak_dark;54.71;15.30;0;75;0.80",
        "O;Nature;tree_oak_dark;54.88;14.95;0;171;0.80",
        "O;Nature;tree_pineRoundA;57.28;19.28;0;14;0.80",
        "O;Nature;tree_pineTallB;57.33;18.52;0;55;0.80",
        "O;Props;flower_yellowC;51.32;4.92;0;189;0.90",
        "O;Nature;tree_oak;55.72;14.53;0;46;0.80",
        "O;Nature;tree_simple;56.39;13.38;0;15;0.80",
        "O;Nature;tree_thin;55.82;13.94;0;29;0.80",
        "O;Props;flower_purpleB;55.09;8.44;0;229;0.90",
        "O;Props;flower_yellowA;57.46;12.08;0;163;1.00",
        "O;Props;flower_yellowB;56.91;11.94;0;185;1.00",
        "O;Props;flower_purpleC;57.28;12.66;0;338;1.00",
        "O;Nature;ftk:tree;57.38;13.87;0;168;0.75",
        "O;Nature;tree_small;57.87;13.28;0;328;0.80",
        "O;Nature;ftk:tree-crooked;57.99;13.52;0;202;0.75",
        "O;Nature;tree_pineSmallA;56.25;6.77;0;341;0.80",
        "O;Nature;tree_oak_dark;55.57;7.17;0;219;0.80",
        "O;Nature;tree_pineSmallA;57.17;9.08;0;69;0.80",
        "O;Props;plant_bushSmall;57.55;8.95;0;342;0.90",
        "O;Nature;tree_thin;58.30;9.78;0;204;0.80",
        "O;Props;plant_flatTall;57.67;10.76;0;312;0.90",
        "O;Props;mushroom_red;59.41;11.88;0;76;1.10",
        "O;Props;stump_round;58.60;12.35;0;237;0.90",
        "O;Nature;tree_pineRoundA;60.26;13.74;0;352;0.80",
        "O;Nature;tree_fat;60.37;13.80;0;132;0.80",
        "O;Nature;tree_small;59.40;13.84;0;295;0.80",
        "O;Nature;rock_largeB;59.77;10.20;0;244;0.90",
        "O;Props;ftk:lantern;32.00;26.56;0;0;0.75",
        "O;Props;ftk:lantern;26.00;32.33;0;0;0.75",
        "O;Props;ftk:lantern;20.00;28.87;0;0;0.75",
        "O;Props;ftk:lantern;12.00;26.56;0;0;0.75",
        "O;Props;ftk:lantern;9.00;21.36;0;0;0.75",
        "O;Props;ftk:lantern;14.00;16.17;0;0;0.75",
        "O;Props;ftk:lantern;27.00;14.44;0;0;0.75",
        "O;Props;ftk:lantern;33.00;16.74;0;0;0.75",
        "O;Props;ftk:lantern;39.00;10.97;0;0;0.75",
        "O;Props;ftk:lantern;45.00;16.74;0;0;0.75",
        "O;Props;ftk:lantern;51.00;13.28;0;0;0.75",
        "O;Props;ftk:lantern;52.00;5.78;0;0;0.75",
        "O;Props;ftk:lantern;46.00;4.61;0;0;0.75",
        "O;Props;ftk:lantern;38.00;2.31;0;0;0.75",
        "O;Props;ftk:lantern;29.00;4.04;0;0;0.75",
        "O;Props;ftk:lantern;24.00;2.31;0;0;0.75",
        "O;Props;ftk:lantern;16.00;4.61;0;0;0.75",
        "O;Praca;ftk:fountain-round-detail;43.00;8.66;0;0;1.00",
        "O;Praca;ftk:cart;43.00;11.76;0;180;1.00",
        "O;Praca;ftk:lantern;45.05;10.71;0;0;0.75",
        "O;Praca;ftk:lantern;40.95;10.71;0;0;0.75",
        "O;Praca;ftk:lantern;40.95;6.61;0;0;0.75",
        "O;Praca;ftk:lantern;45.05;6.61;0;0;0.75",
        "O;Props;ftk:fence;5.50;1.46;0;90;1.00",
        "O;Props;ftk:fence;5.50;3.46;0;90;1.00",
        "O;Props;ftk:fence;5.50;5.46;0;90;1.00",
        "O;Props;ftk:hedge-large;8.70;6.66;0;0;1.00",
        "O;Props;ftk:hedge-large;8.70;0.26;0;0;1.00",
        "O;Props;ftk:lantern;11.30;5.06;0;0;0.75",
        "O;Props;ftk:fence;24.71;6.60;0;360;1.00",
        "O;Props;ftk:fence;22.71;6.60;0;360;1.00",
        "O;Props;ftk:fence;20.71;6.60;0;360;1.00",
        "O;Props;ftk:hedge-large;19.51;9.80;0;270;1.00",
        "O;Props;ftk:hedge-large;25.91;9.80;0;270;1.00",
        "O;Props;ftk:lantern;21.11;12.40;0;0;0.75",
        "O;Props;ftk:fence;26.17;20.53;0;360;1.00",
        "O;Props;ftk:fence;24.17;20.53;0;360;1.00",
        "O;Props;ftk:fence;22.17;20.53;0;360;1.00",
        "O;Props;ftk:hedge-large;20.97;23.73;0;270;1.00",
        "O;Props;ftk:hedge-large;27.37;23.73;0;270;1.00",
        "O;Props;ftk:lantern;22.57;26.33;0;0;0.75",
        "O;Props;ftk:fence;37.74;20.09;0;360;1.00",
        "O;Props;ftk:fence;35.74;20.09;0;360;1.00",
        "O;Props;ftk:fence;33.74;20.09;0;360;1.00",
        "O;Props;ftk:hedge-large;32.54;23.29;0;270;1.00",
        "O;Props;ftk:hedge-large;38.94;23.29;0;270;1.00",
        "O;Props;ftk:lantern;34.14;25.89;0;0;0.75",
        "O;Props;ftk:fence;15.61;7.91;0;360;1.00",
        "O;Props;ftk:fence;13.61;7.91;0;360;1.00",
        "O;Props;ftk:fence;11.61;7.91;0;360;1.00",
        "O;Props;ftk:hedge-large;10.41;11.11;0;270;1.00",
        "O;Props;ftk:hedge-large;16.81;11.11;0;270;1.00",
        "O;Props;ftk:lantern;12.01;13.71;0;0;0.75",
        "O;Props;ftk:fence;33.54;11.38;0;180;1.00",
        "O;Props;ftk:fence;35.54;11.38;0;180;1.00",
        "O;Props;ftk:fence;37.54;11.38;0;180;1.00",
        "O;Props;ftk:hedge-large;38.74;8.18;0;90;1.00",
        "O;Props;ftk:hedge-large;32.34;8.18;0;90;1.00",
        "O;Props;ftk:lantern;37.14;5.58;0;0;0.75",
        "O;Props;ftk:lantern;40.50;24.71;0;0;0.75",
        "O;Props;ftk:lantern;40.50;30.71;0;0;0.75",
        "O;Props;ftk:cart;39.00;34.11;0;90;1.00",
        "O;Props;ftk:wheel;39.30;36.01;0;0;1.00",
        "O;Props;ftk:lantern;11.40;1.76;0;0;0.75",
        "O;Props;ftk:lantern;11.40;5.16;0;0;0.75",
        "P;40.00;27.71",
        "P;38.00;27.71",
        "P;37.00;29.44",
        "P;35.00;29.44",
        "P;33.00;29.44",
        "P;32.00;27.71",
        "P;30.00;27.71",
        "P;29.00;29.44",
        "P;27.00;29.44",
        "P;26.00;31.18",
        "P;24.00;31.18",
        "P;23.00;29.44",
        "P;21.00;29.44",
        "P;19.00;29.44",
        "P;18.00;27.71",
        "P;16.00;27.71",
        "P;15.00;25.98",
        "P;13.00;25.98",
        "P;12.00;24.25",
        "P;10.00;24.25",
        "P;9.00;22.52",
        "P;8.00;20.78",
        "P;9.00;19.05",
        "P;10.00;17.32",
        "P;12.00;17.32",
        "P;14.00;17.32",
        "P;16.00;17.32",
        "P;17.00;15.59",
        "P;19.00;15.59",
        "P;21.00;15.59",
        "P;22.00;17.32",
        "P;24.00;17.32",
        "P;26.00;17.32",
        "P;27.00;15.59",
        "P;29.00;15.59",
        "P;30.00;13.86",
        "P;32.00;13.86",
        "P;33.00;15.59",
        "P;35.00;15.59",
        "P;37.00;15.59",
        "P;38.00;13.86",
        "P;39.00;12.12",
        "P;41.00;12.12",
        "P;42.00;13.86",
        "P;44.00;13.86",
        "P;45.00;15.59",
        "P;47.00;15.59",
        "P;49.00;15.59",
        "P;50.00;13.86",
        "P;52.00;13.86",
        "P;53.00;12.12",
        "P;54.00;10.39",
        "P;53.00;8.66",
        "P;52.00;6.93",
        "P;50.00;6.93",
        "P;49.00;5.20",
        "P;47.00;5.20",
        "P;46.00;3.46",
        "P;44.00;3.46",
        "P;42.00;3.46",
        "P;40.00;3.46",
        "P;38.00;3.46",
        "P;36.00;3.46",
        "P;34.00;3.46",
        "P;32.00;3.46",
        "P;30.00;3.46",
        "P;29.00;1.73",
        "P;27.00;1.73",
        "P;25.00;1.73",
        "P;24.00;3.46",
        "P;22.00;3.46",
        "P;20.00;3.46",
        "P;18.00;3.46",
        "P;16.00;3.46",
        "P;14.00;3.46",
        "P;12.00;3.46",
        "PS;29.00;19.05"
    };
}
#endif
