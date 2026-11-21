using UnityEngine;
using UnityEngine.InputSystem;

public class TowerBuilder : MonoBehaviour
{
    [Header("Torres disponíveis (arraste os prefabs Tower_*)")]
    public GameObject[] towerPrefabs;

    [Header("Interação")]
    public float interactRadius = 2.5f;     // distância do player ao local para poder usar E

    int selected;                            // torre escolhida (teclas 1..9)
    TowerSpot[] spots;
    Transform player;
    Health playerHealth;
    Animator playerAnimator;
    TowerSpot nearest;

    void Start()
    {
        spots = FindObjectsByType<TowerSpot>(FindObjectsSortMode.None);
        FindPlayer();
    }

    void FindPlayer()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc == null) return;
        player = pc.transform;
        playerHealth = pc.GetComponent<Health>();
        playerAnimator = pc.GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (player == null) { FindPlayer(); return; }
        if (playerHealth != null && playerHealth.IsDead) { nearest = null; return; }

        nearest = FindNearestSpot();

        var kb = Keyboard.current;
        if (kb == null) return;

        // teclas 1..9 escolhem qual torre construir
        for (int i = 0; i < towerPrefabs.Length && i < 9; i++)
            if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) selected = i;

        // E: constrói ou remove no local mais próximo
        if (nearest != null && kb.eKey.wasPressedThisFrame)
        {
            if (nearest.IsEmpty) Build(nearest);
            else Remove(nearest);
        }
    }

    TowerSpot FindNearestSpot()
    {
        TowerSpot best = null;
        float bestDist = interactRadius;
        foreach (var s in spots)
        {
            if (s == null) continue;
            Vector3 d = s.transform.position - player.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist <= bestDist) { best = s; bestDist = dist; }
        }
        return best;
    }

    void Build(TowerSpot spot)
    {
        if (towerPrefabs == null || towerPrefabs.Length == 0 || towerPrefabs[selected] == null) return;
        spot.tower = Instantiate(towerPrefabs[selected], spot.BuildPosition, spot.transform.rotation);
        PlayInteractAnimation();
    }

    void Remove(TowerSpot spot)
    {
        Destroy(spot.tower);
        spot.tower = null;
        PlayInteractAnimation();
    }

    void PlayInteractAnimation()
    {
        if (playerAnimator != null) playerAnimator.SetTrigger("Interact");
    }

    // aviso provisório na tela (depois vira a UI de opções)
    void OnGUI()
    {
        if (nearest == null || towerPrefabs == null || towerPrefabs.Length == 0) return;

        string msg = nearest.IsEmpty
            ? "[E] Construir " + towerPrefabs[selected].name + "     (teclas 1-" + towerPrefabs.Length + " trocam a torre)"
            : "[E] Remover torre";

        var style = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height - 120, Screen.width, 40), msg, style);
    }
}