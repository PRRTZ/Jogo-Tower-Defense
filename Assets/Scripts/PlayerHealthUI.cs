using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Referências")]
    public Health playerHealth;

    [Header("Configurações dos Corações")]
    public float heartSize = 32; // Use múltiplos de 16 (ex: 32 ou 48) para manter o pixel art nítido

    [Header("Sprites dos Corações")]
    public Sprite fullHeart;
    public Sprite halfHeart;
    public Sprite emptyHeart;

    private List<Image> heartImages = new List<Image>();

    void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Health>();
        }

        CreateHearts();
    }

    void Update()
    {
        if (playerHealth != null)
        {
            // a vida máxima mudou (upgrade na forja): refaz os corações
            if (HeartCount() != heartImages.Count) CreateHearts();

            UpdateHearts();
        }
    }

    int HeartCount()
    {
        float maxH = playerHealth.maxHealth;
        return Mathf.CeilToInt(maxH <= 10 ? maxH / 2f : maxH / 20f);
    }

    void CreateHearts()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        heartImages.Clear();

        if (playerHealth == null) return;
        int totalHearts = HeartCount();

        for (int i = 0; i < totalHearts; i++)
        {
            GameObject heartObj = new GameObject("Heart_" + i, typeof(RectTransform), typeof(Image));
            heartObj.transform.SetParent(transform, false);

            // Define o tamanho exato do coração sem distorção e com Scale em 1
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
        float currentHP = playerHealth.CurrentHealth;
        float maxH = playerHealth.maxHealth;
        float scaleFactor = maxH <= 10 ? 2f : 20f;

        for (int i = 0; i < heartImages.Count; i++)
        {
            float heartValue = currentHP - (i * scaleFactor);

            if (heartValue >= scaleFactor)
            {
                heartImages[i].sprite = fullHeart;
            }
            else if (heartValue > 0)
            {
                heartImages[i].sprite = halfHeart;
            }
            else
            {
                heartImages[i].sprite = emptyHeart;
            }
        }
    }
}
