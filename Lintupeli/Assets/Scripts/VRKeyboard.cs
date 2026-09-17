using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built QWERTY + Finnish VR keyboard.
/// Supports lowercase, uppercase (⇧ one-shot toggle), and a numbers/symbols page (123).
/// Attach to the CanvasRoot inside RouteNamePanel. Set targetInputField in the Inspector.
/// </summary>
public class VRKeyboard : MonoBehaviour
{
    public InputField targetInputField;

    [Header("Key sizing (canvas units)")]
    public float keyWidth      = 62f;
    public float keyHeight     = 62f;
    public float keyGap        = 6f;
    public float bottomPadding = 12f;

    // ── Keyboard layout ───────────────────────────────────────────────────────

    private static readonly string[][] LetterRows =
    {
        new[] { "q","w","e","r","t","y","u","i","o","p","å" },
        new[] { "a","s","d","f","g","h","j","k","l","ö","ä" },
        new[] { "z","x","c","v","b","n","m","⌫" },
        new[] { "⇧", "VÄLI", "TYHJENNÄ", "123" },
    };

    private static readonly string[][] NumberRows =
    {
        new[] { "1","2","3","4","5","6","7","8","9","0" },
        new[] { "!","?",".",",","-","_","@","#","&","*" },
        new[] { "(",")","\u0022","'","+","=","/","\\","⌫" },
        new[] { "ABC", "VÄLI", "TYHJENNÄ" },
    };

    // ── State ─────────────────────────────────────────────────────────────────

    private bool        _caps;
    private GameObject  _letterPage;
    private GameObject  _numberPage;
    private Button      _shiftBtn;
    private Image       _shiftImg;

    private static readonly Color ColorKeyNormal   = new Color(0.22f, 0.22f, 0.28f, 1f);
    private static readonly Color ColorActionNormal = new Color(0.15f, 0.15f, 0.20f, 1f);
    private static readonly Color ColorHighlight   = new Color(0.38f, 0.38f, 0.50f, 1f);
    private static readonly Color ColorPressed     = new Color(0.12f, 0.50f, 0.85f, 1f);
    private static readonly Color ColorCapsActive  = new Color(0.12f, 0.50f, 0.85f, 1f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        ExpandCanvas();
        _letterPage = BuildPage("VRKBLetters", LetterRows, isLetterPage: true);
        _numberPage = BuildPage("VRKBNumbers", NumberRows, isLetterPage: false);
        ShowPage(letters: true);
    }

    void ExpandCanvas()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;
        float kbH = LetterRows.Length * keyHeight
                  + (LetterRows.Length - 1) * keyGap
                  + bottomPadding + 4f;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + kbH);
    }

    // ── Page builder ──────────────────────────────────────────────────────────

    GameObject BuildPage(string pageName, string[][] rows, bool isLetterPage)
    {
        var page = new GameObject(pageName);
        page.transform.SetParent(transform, false);

        var rt = page.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);

        float totalH = rows.Length * keyHeight + (rows.Length - 1) * keyGap;
        rt.sizeDelta        = new Vector2(800f, totalH);
        rt.anchoredPosition = new Vector2(0f, bottomPadding);

        for (int r = 0; r < rows.Length; r++)
        {
            float centreY  = totalH - keyHeight / 2f - r * (keyHeight + keyGap);
            bool isBottom  = r == rows.Length - 1;
            BuildRow(page.transform, rows[r], centreY, totalH, isBottom, isLetterPage);
        }

        return page;
    }

    void BuildRow(Transform page, string[] keys, float centreY, float pageH,
                  bool isBottomRow, bool isLetterPage)
    {
        float maxRowW = 11 * keyWidth + 10 * keyGap; // widest letter row

        float rowW = isBottomRow
            ? maxRowW
            : keys.Length * keyWidth + (keys.Length - 1) * keyGap;

        float startX = -rowW / 2f;

        for (int i = 0; i < keys.Length; i++)
        {
            string label = keys[i];
            float w      = isBottomRow ? BottomKeyWidth(label, keys, rowW) : keyWidth;

            var go = CreateKey(page, label, w, isBottomRow);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0f, 0.5f);
            rt.sizeDelta        = new Vector2(w, keyHeight);
            rt.anchoredPosition = new Vector2(startX, centreY - pageH / 2f);
            startX += w + keyGap;

            if (label == "⇧")
            {
                _shiftBtn = go.GetComponent<Button>();
                _shiftImg = go.GetComponent<Image>();
            }

            string captured = label;
            go.GetComponent<Button>().onClick.AddListener(() => OnKey(captured));
        }
    }

    float BottomKeyWidth(string label, string[] rowKeys, float rowW)
    {
        // VÄLI takes 4× weight, other keys 1×.
        int spaceCount = 0, normalCount = 0;
        foreach (var k in rowKeys)
            if (k == "VÄLI") spaceCount++; else normalCount++;

        float gapTotal  = (rowKeys.Length - 1) * keyGap;
        float available = rowW - gapTotal;
        float unit      = available / (normalCount + spaceCount * 4f);
        return label == "VÄLI" ? unit * 4f : unit;
    }

    GameObject CreateKey(Transform parent, string label, float w, bool isAction)
    {
        var go = new GameObject("Key_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(w, keyHeight);

        Color bg  = isAction ? ColorActionNormal : ColorKeyNormal;
        var img   = go.AddComponent<Image>();
        img.color = bg;

        var btn             = go.AddComponent<Button>();
        btn.targetGraphic   = img;
        var cb              = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = ColorHighlight;
        cb.pressedColor     = ColorPressed;
        btn.colors          = cb;

        // Text
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var trt       = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var txt       = textGo.AddComponent<Text>();
        txt.text      = KeyDisplayText(label);
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = label.Length > 3 ? 18 : (label.Length > 1 ? 22 : 28);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;

        return go;
    }

    string KeyDisplayText(string key)
    {
        if (key == "VÄLI")     return "välilyönti";
        if (key == "TYHJENNÄ") return "tyhjennä";
        if (key == "⇧")        return "⇧ isot";
        return key;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    void OnKey(string key)
    {
        if (targetInputField == null) return;

        if (key == "⇧")
        {
            _caps = !_caps;
            RefreshLetterLabels();
            ApplyShiftColor();
            return;
        }

        if (key == "123") { ShowPage(letters: false); return; }
        if (key == "ABC") { ShowPage(letters: true);  return; }

        if (key == "⌫")
        {
            if (targetInputField.text.Length > 0)
                targetInputField.text =
                    targetInputField.text.Substring(0, targetInputField.text.Length - 1);
            return;
        }

        if (key == "VÄLI")     { targetInputField.text += " "; return; }
        if (key == "TYHJENNÄ") { targetInputField.text = "";   return; }

        // Regular character — apply caps if on letter page.
        string ch = (_caps && key.Length == 1) ? key.ToUpper() : key;
        targetInputField.text += ch;

        // One-shot caps: disable after a single letter.
        if (_caps && key.Length == 1 && char.IsLetter(key[0]))
        {
            _caps = false;
            RefreshLetterLabels();
            ApplyShiftColor();
        }
    }

    void ShowPage(bool letters)
    {
        _letterPage.SetActive(letters);
        _numberPage.SetActive(!letters);
    }

    void RefreshLetterLabels()
    {
        // Keys are flat children of _letterPage — iterate directly.
        foreach (Transform child in _letterPage.transform)
        {
            // Only single-character letter keys (Key_q, Key_å, etc.)
            if (!child.name.StartsWith("Key_")) continue;
            string keyChar = child.name.Substring(4); // strip "Key_"
            if (keyChar.Length != 1) continue;
            if (!char.IsLetter(keyChar[0])) continue;

            var lbl = child.Find("Label");
            if (lbl == null) continue;
            var txt = lbl.GetComponent<Text>();
            if (txt == null) continue;
            txt.text = _caps ? keyChar.ToUpper() : keyChar.ToLower();
        }
    }

    void ApplyShiftColor()
    {
        if (_shiftImg == null || _shiftBtn == null) return;
        Color col = _caps ? ColorCapsActive : ColorActionNormal;
        _shiftImg.color = col;
        var cb = _shiftBtn.colors;
        cb.normalColor = col;
        _shiftBtn.colors = cb;
    }
}
