using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Animates a TMP_Text by appending dots one-by-one (e.g. ".", "..", ...)
/// up to maxDots and looping.
/// 
/// Usage:
/// 1) Add this component to the same GameObject as a TextMeshProUGUI (TMP_Text)
///    or assign the text field manually.
/// 2) Optionally set prefix ("Loading"), maxDots (10), and interval.
/// </summary>
[DisallowMultipleComponent]
public sealed class DotsLoadingText : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text text;

    [Header("Format")]
    [SerializeField] private string prefix = "";
    [SerializeField, Min(1)] private int maxDots = 10;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float intervalSeconds = 0.15f;

    private Coroutine _routine;

    private void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();

        if (text == null)
        {
            Debug.LogWarning("[DotsLoadingText] No TMP_Text assigned/found. Disabling.");
            enabled = false;
            return;
        }

        _routine = StartCoroutine(Animate());
    }

    private void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private IEnumerator Animate()
    {
        int count = 0;
        while (true)
        {
            count = (count % maxDots) + 1; // 1..maxDots
            text.text = prefix + new string('.', count);
            yield return new WaitForSeconds(intervalSeconds);
        }
    }

    // Optional helpers if you want to control it from code
    public void SetPrefix(string value) => prefix = value ?? string.Empty;
    public void SetMaxDots(int value) => maxDots = Mathf.Max(1, value);
    public void SetInterval(float seconds) => intervalSeconds = Mathf.Max(0.01f, seconds);
}