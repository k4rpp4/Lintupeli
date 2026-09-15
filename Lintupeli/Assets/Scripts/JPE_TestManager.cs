using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public enum JPEDirection
{
    Horizontal,
    Vertical
}

public class JPE_TestManager : MonoBehaviour
{
    [Header("Test Direction")]
    public JPEDirection direction = JPEDirection.Horizontal;

    [Header("References")]
    public Transform headTransform;
    public GameObject jpeTargetPrefab;
    public Transform jpeTargetsParent;
    public TMP_Text instructions;
    private TMP_Text resultText;
    public GameObject buttonPanelPrefab;
    private GameObject currentButtonPanel;

    [Header("Settings")]
    public float distanceFromHead = 0.9f;
    public int maxTargets = 3;
    public int totalTrials = 5;

    private Vector3 wallOrigin;
    private Vector3 wallNormal;
    private Vector3 startMarkerPosition;
    private Vector3 endMarkerPosition;

    private int index = 0;
    private int currentTrial = 0;
    private List<float> trialResults = new List<float>();

    public string firstInstruction;
    public string secondInstruction;
    public string thirdInstruction;

    [Header("External Exit Actions")]
    public UnityEvent onExitTest;
    public UnityEvent onRestartTest;

    private bool testComplete = false;
    private readonly string[] pointNames =
    {
        "Aloituspiste",
        "Ääripiste",
        "Lopetuspiste"
    };

        void Update()
    {
        if (testComplete)
            return;

        bool leftGrip = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger);
        bool rightGrip = OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger);

        if (leftGrip || rightGrip)
        {
            SpawnTargetIfAllowed();
        }
    }

    void SpawnTargetIfAllowed()
    {
        if (index >= maxTargets)
            return;

        SpawnTarget(index);
        index++;
        UpdateInstructionText(index);
    }


    void SpawnTarget(int index)
    {
        Vector3 forward = headTransform.forward.normalized;
        Vector3 targetPosition;

        if (index == 0)
        {
            wallNormal = forward;
            targetPosition = headTransform.position + forward * distanceFromHead;
            wallOrigin = targetPosition;
            startMarkerPosition = targetPosition;
        }
        else
        {
            targetPosition = ProjectOntoWall(forward);
        }

        Quaternion targetRotation = Quaternion.LookRotation(wallNormal);

        GameObject newTarget =
            Instantiate(jpeTargetPrefab, targetPosition, targetRotation, jpeTargetsParent);

        newTarget.name = pointNames[index];

        TMP_Text textComponent =
            newTarget.GetComponentInChildren<TMP_Text>();

        if (textComponent != null)
        {
            if (index == 0)
            {
                textComponent.text = pointNames[index];
            }
            else
            {
                float distCm = GetAxisDistanceCm(startMarkerPosition, targetPosition);
                textComponent.text = pointNames[index] + "\n" + distCm.ToString("F1") + " cm";
            }
        }

        JPETargetVisual visual =
            newTarget.GetComponent<JPETargetVisual>();

        if (visual != null)
            visual.SetVisible(false);

        if (index == 2)
        {
            endMarkerPosition = targetPosition;
            currentTrial++;

            RevealAllTargets();
            CalculateJPEDistance();

            instructions.text = "";

            testComplete = true;
        }
    }

    Vector3 ProjectOntoWall(Vector3 rayDir)
    {
        float denom = Vector3.Dot(rayDir, wallNormal);
        if (Mathf.Abs(denom) < 0.0001f)
            return wallOrigin;
        float t = Vector3.Dot(wallOrigin - headTransform.position, wallNormal) / denom;
        return headTransform.position + rayDir * t;
    }

    // Returns the distance between two on-wall points along the currently
    // selected test axis only (horizontal = wall's right vector, vertical =
    // wall's up vector), instead of the full on-wall displacement.
    float GetAxisDistanceCm(Vector3 from, Vector3 to)
    {
        Vector3 referenceUp = Mathf.Abs(Vector3.Dot(wallNormal, Vector3.up)) > 0.999f
            ? Vector3.forward
            : Vector3.up;

        Vector3 wallRight = Vector3.Cross(referenceUp, wallNormal).normalized;
        Vector3 wallUp = Vector3.Cross(wallNormal, wallRight).normalized;

        Vector3 displacement = to - from;
        Vector3 axis = direction == JPEDirection.Horizontal ? wallRight : wallUp;

        return Mathf.Abs(Vector3.Dot(displacement, axis)) * 100f;
    }

    // "Horisontaalinen" / "Vertikaalinen" — used in the result text and as the
    // instructions header so the active test direction is always visible.
    string GetDirectionLabel()
    {
        return direction == JPEDirection.Horizontal ? "Horisontaalinen" : "Vertikaalinen";
    }

    // ASCII arrow hint (the project's TMP font has no Unicode arrow glyphs),
    // shown next to the direction label so the direction is illustrated, not
    // just named.
    string GetDirectionArrow()
    {
        return direction == JPEDirection.Horizontal ? "<->" : "^v";
    }

    void CalculateJPEDistance()
    {
        float distanceCm = GetAxisDistanceCm(startMarkerPosition, endMarkerPosition);
        trialResults.Add(distanceCm);

        if (resultText == null)
            return;

        string text = "Kierros " + currentTrial + "/" + totalTrials +
                      "\n" + GetDirectionLabel() + " JPE-virhe: " + distanceCm.ToString("F1") + " cm";

        if (currentTrial >= totalTrials)
        {
            float sum = 0f;
            foreach (float r in trialResults) sum += r;
            float average = sum / trialResults.Count;
            text += "\n\nKeskiarvo: " + average.ToString("F1") + " cm";
        }

        resultText.text = text;
    }

    void RevealAllTargets()
    {
        Transform startPoint = null;

        foreach (Transform child in jpeTargetsParent)
        {
            JPETargetVisual visual =
                child.GetComponent<JPETargetVisual>();

            if (visual != null)
                visual.SetVisible(true);

            if (child.name == "Aloituspiste")
                startPoint = child;
        }

        if (startPoint != null)
            SpawnButtonPanel(startPoint);
    }

    void SpawnButtonPanel(Transform startPoint)
    {
        if (currentButtonPanel != null)
            Destroy(currentButtonPanel);

        Vector3 offset = new Vector3(0f, -0.5f, 0f);

        currentButtonPanel = Instantiate(
            buttonPanelPrefab,
            startPoint.position + offset,
            Quaternion.LookRotation(wallNormal)
        );

        currentButtonPanel.transform.SetParent(startPoint);

        // 🔹 GET RESULT TEXT FROM PANEL VIA SCRIPT
        JPETestMenuUI ui =
            currentButtonPanel.GetComponent<JPETestMenuUI>();

        if (ui != null)
            resultText = ui.resultText;

        // 🔹 Wire buttons
        Button[] buttons =
            currentButtonPanel.GetComponentsInChildren<Button>();

        foreach (Button btn in buttons)
        {
            btn.onClick.RemoveAllListeners();

            if (btn.name.Contains("Restart"))
            {
                if (currentTrial < totalTrials)
                {
                    btn.onClick.AddListener(NextTrial);
                    TMP_Text btnLabel = btn.GetComponentInChildren<TMP_Text>();
                    if (btnLabel != null) btnLabel.text = "Seuraava";
                }
                else
                {
                    btn.onClick.AddListener(RestartTest);
                }
            }

            if (btn.name.Contains("Exit"))
                btn.onClick.AddListener(ExitTest);
        }
    }




    void ClearMarkers()
    {
        for (int i = jpeTargetsParent.childCount - 1; i >= 0; i--)
            Destroy(jpeTargetsParent.GetChild(i).gameObject);

        if (currentButtonPanel != null)
            Destroy(currentButtonPanel);

        resultText = null;

        wallOrigin = Vector3.zero;
        wallNormal = Vector3.zero;
        startMarkerPosition = Vector3.zero;
        endMarkerPosition = Vector3.zero;

        testComplete = false;
        index = 0;
    }

    void NextTrial()
    {
        ClearMarkers();
        SetInstructionText(firstInstruction);
    }

    public void RestartTest()
    {
        ClearMarkers();

        currentTrial = 0;
        trialResults.Clear();

        if (onRestartTest != null)
            onRestartTest.Invoke();

        SetInstructionText(firstInstruction);
    }

    public void ExitTest()
    {
        RestartTest();

        if (onExitTest != null)
            onExitTest.Invoke();
    }

    // Wire these to the direction-selection buttons in the start menu.
    public void SetDirectionHorizontal()
    {
        direction = JPEDirection.Horizontal;
    }

    public void SetDirectionVertical()
    {
        direction = JPEDirection.Vertical;
    }

    void UpdateInstructionText(int index)
    {
        switch (index)
        {
            case 0:
                SetInstructionText(firstInstruction);
                break;

            case 1:
                SetInstructionText(secondInstruction);
                break;

            case 2:
                SetInstructionText(thirdInstruction);
                break;

            default:
                if (instructions != null)
                    instructions.text = "";
                break;
        }
    }

    // Shows the given instruction body with a direction header on top, and
    // fills in the {SUUNTA} token (if present) with a direction-specific
    // phrase, so the same Inspector-authored instruction text reads
    // differently for the horizontal vs. vertical test.
    void SetInstructionText(string body)
    {
        if (instructions == null)
            return;

        string directionPhrase = direction == JPEDirection.Horizontal ? "sivulle" : "ylös tai alas";
        string filledBody = body != null ? body.Replace("{SUUNTA}", directionPhrase) : "";

        instructions.text = GetDirectionLabel() + " testi " + GetDirectionArrow() + "\n\n" + filledBody;
    }

}
