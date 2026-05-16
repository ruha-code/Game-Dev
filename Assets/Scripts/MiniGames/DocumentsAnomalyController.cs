using UnityEngine;
using TMPro;
using System.Collections;

public class DocumentsAnomalyController : MonoBehaviour
{
    [Header("References")]
    public TMP_Text titleText;
    public TMP_Text statusText;
    public TMP_Text trustText;
    public RectTransform documentPage;

    [Header("Settings")]
    public float minInterval = 6f;
    public float maxInterval = 12f;

    private void Start()
    {
        StartCoroutine(AnomalyRoutine());
    }

    private IEnumerator AnomalyRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            TriggerRandomAnomaly();
        }
    }

    private void TriggerRandomAnomaly()
    {
        int type = Random.Range(0, 5);
        switch (type)
        {
            case 0: StartCoroutine(TitleAnomaly()); break;
            case 1: StartCoroutine(StatusAnomaly()); break;
            case 2: StartCoroutine(ShiftAnomaly()); break;
            case 3: StartCoroutine(TrustAnomaly()); break;
            case 4: StartCoroutine(WhisperAnomaly()); break;
        }
    }

    private IEnumerator TitleAnomaly()
    {
        if (!titleText) yield break;
        string original = titleText.text;
        titleText.text = "LAB 7 CONTAINMENT REPORT";
        yield return new WaitForSeconds(0.3f);
        titleText.text = original;
    }

    private IEnumerator StatusAnomaly()
    {
        if (!statusText) yield break;
        string original = statusText.text;
        statusText.text = "You are not the first viewer.";
        yield return new WaitForSeconds(1.5f);
        statusText.text = original;
    }

    private IEnumerator ShiftAnomaly()
    {
        if (!documentPage) yield break;
        Vector3 originalPos = documentPage.localPosition;
        documentPage.localPosition += new Vector3(Random.Range(-10, 10), 0, 0);
        yield return new WaitForSeconds(0.1f);
        documentPage.localPosition = originalPos;
    }

    private IEnumerator TrustAnomaly()
    {
        if (!trustText) yield break;
        string original = trustText.text;
        trustText.text = "System Trust: watching";
        yield return new WaitForSeconds(0.5f);
        trustText.text = original;
    }

    private IEnumerator WhisperAnomaly()
    {
        if (AudioManager.Instance != null)
        {
            Debug.Log("[Anomaly] Faint whisper...");
        }
        yield return null;
    }
}
