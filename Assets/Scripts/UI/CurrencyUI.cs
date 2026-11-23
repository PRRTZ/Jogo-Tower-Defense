using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Mostra as moedas na tela e pisca em vermelho quando falta dinheiro para uma compra.
///
/// Com o ícone de moeda da UI do mapa: arraste o texto "UI/CoinUI/CoinAmount" em "Money Text".
/// O texto mantém a fonte, o tamanho e a cor definidos nele, e mostra só o número.
///
/// Sem nada em "Money Text": cria um texto próprio "Moedas: 0" no canto superior direito.
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    [Tooltip("Arraste aqui o CoinAmount da UI do mapa. Vazio = cria um texto próprio.")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Color noMoneyColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Cor do número")]
    [Tooltip("Ligado: pinta o número com a cor abaixo (útil em mapas escuros). Desligado: mantém a cor do texto.")]
    [SerializeField] private bool overrideColor = true;
    [SerializeField] private Color textColor = new Color(1f, 0.84f, 0.25f, 1f);

    [Header("Só para o texto próprio (Money Text vazio)")]
    [SerializeField] private string ownTextPrefix = "Moedas: ";
    [SerializeField] private Color ownTextColor = new Color(1f, 0.84f, 0.25f, 1f);
    [Tooltip("Distância do canto superior direito.")]
    [SerializeField] private Vector2 ownTextPosition = new Vector2(-50f, -90f);
    [SerializeField] private float ownTextFontSize = 24f;

    private string prefix = "";
    private Color normalColor;
    private Coroutine flash;

    private void Start()
    {
        if (moneyText == null)
        {
            CreateText();
            prefix = ownTextPrefix;
        }
        if (overrideColor) moneyText.color = textColor;
        normalColor = moneyText.color;

        var cm = CurrencyManager.Instance;
        if (cm == null)
        {
            Debug.LogError("CurrencyUI: não existe CurrencyManager na cena.");
            enabled = false;
            return;
        }

        cm.OnMoneyChanged += UpdateText;
        cm.OnPurchaseFailed += Flash;
        UpdateText(cm.Money);
    }

    private void OnDestroy()
    {
        var cm = CurrencyManager.Instance;
        if (cm == null) return;
        cm.OnMoneyChanged -= UpdateText;
        cm.OnPurchaseFailed -= Flash;
    }

    private void CreateText()
    {
        var canvas = UIFactory.CreateCanvas("UI_Moedas", 5);
        moneyText = UIFactory.CreateText(canvas.transform, "", ownTextFontSize, TextAlignmentOptions.Right);
        moneyText.fontStyle = FontStyles.Bold;
        moneyText.outlineWidth = 0.2f;
        moneyText.outlineColor = Color.black;
        moneyText.color = ownTextColor;

        var rt = moneyText.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = ownTextPosition;
        rt.sizeDelta = new Vector2(500f, 60f);
    }

    private void UpdateText(int money) => moneyText.text = UIFactory.RemoveDiacritics(prefix + money);

    private void Flash()
    {
        if (flash != null) StopCoroutine(flash);
        flash = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        const float duration = 0.5f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            moneyText.color = Color.Lerp(noMoneyColor, normalColor, t / duration);
            yield return null;
        }
        moneyText.color = normalColor;
        flash = null;
    }
}
