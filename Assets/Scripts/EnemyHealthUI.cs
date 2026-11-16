using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("Referências")]
    public EnemyHealth enemyHealth; 

    [Header("Configurações")]
    public float heartSize = 16f; 
    [Tooltip("Número máximo de corações exibidos para evitar bugs visuais")]
    public int maxHeartsLimit = 3; // Padrão seguro de 3 corações (6 de vida)

    [Header("Sprites dos Corações")]
    public Sprite fullHeart;
    public Sprite halfHeart;
    public Sprite emptyHeart;

    private List<Image> heartImages = new List<Image>();

    void Start()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        CreateHearts();
    }

    void Update()
    {
        if (enemyHealth != null)
        {
            UpdateHearts();
        }
    }

    void CreateHearts()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        heartImages.Clear();

        // Tenta ler a vida máxima do inimigo, mas limita no máximo a 'maxHeartsLimit' para não bugar
        float maxH = GetMaxHealth();
        int totalHearts = Mathf.Clamp(Mathf.CeilToInt(maxH / 2f), 1, maxHeartsLimit); 

        for (int i = 0; i < totalHearts; i++)
        {
            GameObject heartObj = new GameObject("Heart_" + i, typeof(RectTransform), typeof(Image));
            heartObj.transform.SetParent(transform, false);

            RectTransform rect = heartObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(heartSize, heartSize);
            rect.localScale = Vector3.one;

            Image heartImage = heartObj.GetComponent<Image>();
            heartImage.sprite = fullHeart;
            heartImage.preserveAspect = true;
            heartImages.Add(heartImage);
        }
    }

    void UpdateHearts()
    {
        if (enemyHealth == null) return;

        float currentHP = GetCurrentHealth();
        
        for (int i = 0; i < heartImages.Count; i++)
        {
            float heartValue = currentHP - (i * 2f); 

            if (heartValue >= 2f)
            {
                heartImages[i].sprite = fullHeart;
            }
            else if (heartValue > 0f)
            {
                heartImages[i].sprite = halfHeart;
            }
            else
            {
                heartImages[i].sprite = emptyHeart;
            }
        }
    }

    float GetMaxHealth()
    {
        if (enemyHealth == null) return 6f;
        
        System.Reflection.FieldInfo field = enemyHealth.GetType().GetField("maxHealth", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) return Mathf.Max(2f, (float)field.GetValue(enemyHealth));

        System.Reflection.PropertyInfo prop = enemyHealth.GetType().GetProperty("maxHealth");
        if (prop != null) return Mathf.Max(2f, (float)prop.GetValue(enemyHealth));

        return 6f; // Retorna 6 de vida por segurança (3 corações)
    }

    float GetCurrentHealth()
    {
        if (enemyHealth == null) return 6f;

        System.Reflection.FieldInfo field = enemyHealth.GetType().GetField("currentHealth", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) return (float)field.GetValue(enemyHealth);

        System.Reflection.PropertyInfo prop = enemyHealth.GetType().GetProperty("CurrentHealth");
        if (prop != null) return (float)prop.GetValue(enemyHealth);

        return 6f;
    }
}