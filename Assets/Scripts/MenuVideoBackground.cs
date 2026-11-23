using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Coloque no MESMO objeto do MainMenu. Troca o fundo do menu por um vídeo em loop (sem som).
public class MenuVideoBackground : MonoBehaviour
{
    [Header("Vídeo (arraste o MP4 / WebM aqui)")]
    public VideoClip clip;
    public float playbackSpeed = 1f;

    [Header("Aparência")]
    public bool hideFloatingDecor = true;          // esconde as bolinhas/imagens flutuantes por cima do vídeo
    [Range(0f, 0.9f)] public float dim = 0.15f;    // escurece o vídeo para o texto continuar legível

    VideoPlayer vp;
    RenderTexture rt;

    IEnumerator Start()
    {
        if (clip == null) yield break;

        // espera o MainMenu montar a interface
        GameObject canvas = null;
        for (int i = 0; i < 10 && canvas == null; i++)
        {
            yield return null;
            canvas = GameObject.Find("MenuCanvas");
        }
        if (canvas == null) { Debug.LogWarning("MenuVideoBackground: não achei o MenuCanvas (o MainMenu está na cena?)"); yield break; }

        Transform bg = canvas.transform.Find("Background");
        if (bg == null) { Debug.LogWarning("MenuVideoBackground: não achei o Background do menu"); yield break; }

        // esconde o que existia no fundo (imagem estática e decoração flutuante)
        foreach (Transform child in bg)
        {
            if (child.name == "BackgroundImage" || (hideFloatingDecor && child.name == "Floater"))
                child.gameObject.SetActive(false);
        }

        // imagem que mostra o vídeo, cobrindo a tela sem distorcer
        var go = new GameObject("VideoImage", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        go.transform.SetParent(bg, false);
        go.transform.SetAsFirstSibling();
        var raw = go.GetComponent<RawImage>();
        raw.raycastTarget = false;
        raw.color = new Color(1f - dim, 1f - dim, 1f - dim, 1f);      // escurece multiplicando a cor
        var fit = go.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = clip.width / (float)clip.height;

        // o vídeo é desenhado numa RenderTexture
        rt = new RenderTexture((int)clip.width, (int)clip.height, 0);
        raw.texture = rt;

        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = true;
        vp.skipOnDrop = true;
        vp.playbackSpeed = playbackSpeed;
        vp.audioOutputMode = VideoAudioOutputMode.None;               // a música do menu continua separada
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.clip = clip;

        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        vp.Play();
    }

    void OnDestroy()
    {
        if (rt != null) { rt.Release(); Destroy(rt); }
    }
}