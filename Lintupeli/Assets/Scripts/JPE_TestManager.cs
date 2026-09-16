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

    [Header("Direction Indicator")]
    public SpriteRenderer directionIndicatorRenderer;
    public Sprite horizontalIndicatorSprite;
    public Sprite verticalIndicatorSprite;

    [Header("Settings")]
    public float distanceFromHead = 0.9f;
    public int maxTargets = 3;
    public int totalTrials = 5;
    public float resultPanelMargin = 0.4f;

    private Vector3 wallOrigin;
    private Vector3 wallNormal;
    private Vector3 startMarkerPosition;
    private Vector3 extremeMarkerPosition;
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

            if (index == 1)
                extremeMarkerPosition = targetPosition;
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
            UpdateDirectionIndicator(false);

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

    // Right/up basis vectors for the wall plane, derived from wallNormal.
    void GetWallBasis(out Vector3 wallRight, out Vector3 wallUp)
    {
        Vector3 referenceUp = Mathf.Abs(Vector3.Dot(wallNormal, Vector3.up)) > 0.999f
            ? Vector3.forward
            : Vector3.up;

        wallRight = Vector3.Cross(referenceUp, wallNormal).normalized;
        wallUp = Vector3.Cross(wallNormal, wallRight).normalized;
    }

    // Returns the distance between two on-wall points along the currently
    // selected test axis only (horizontal = wall's right vector, vertical =
    // wall's up vector), instead of the full on-wall displacement.
    float GetAxisDistanceCm(Vector3 from, Vector3 to)
    {
        GetWallBasis(out Vector3 wallRight, out Vector3 wallUp);

        Vector3 displacement = to - from;
        Vector3 axis = direction == JPEDirection.Horizontal ? wallRight : wallUp;

        return Mathf.Abs(Vector3.Dot(displacement, axis)) * 100f;
    }

    // Places the result panel a fixed, short distance from the start marker
    // (so it always appears close to the user and is easy to find) on
    // whichever vertical side the extreme/return markers did NOT go toward
    // (so it still never overlaps them, even on a large vertical excursion).
    Vector3 GetResultPanelPosition()
    {
        GetWallBasis(out Vector3 wallRight, out Vector3 wallUp);

        float rightStart = Vector3.Dot(startMarkerPosition - wallOrigin, wallRight);
        float upStart = Vector3.Dot(startMarkerPosition - wallOrigin, wallUp);
        float upExtreme = Vector3.Dot(extremeMarkerPosition - wallOrigin, wallUp);
        float upEnd = Vector3.Dot(endMarkerPosition - wallOrigin, wallUp);

        bool markersWentDown = upExtreme < upStart && upEnd < upStart;
        float verticalSign = markersWentDown ? 1f : -1f;

        return wallOrigin + wallRight * rightStart + wallUp * (upStart + verticalSign * resultPanelMargin);
    }

    // "Vaaka" / "Pysty" — used in the result text and as the
    // instructions header so the active test direction is always visible.
    string GetDirectionLabel()
    {
        return direction == JPEDirection.Horizontal ? "Vaaka" : "Pysty";
    }

    // Shows the sprite matching the current test direction next to the
    // instructions text, in place of the old ASCII arrow hint.
    void UpdateDirectionIndicator(bool visible)
    {
        if (directionIndicatorRenderer == null)
            return;

        directionIndicatorRenderer.sprite =
            direction == JPEDirection.Horizontal ? horizontalIndicatorSprite : verticalIndicatorSprite;
        directionIndicatorRenderer.enabled = visible;
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

        currentButtonPanel = Instantiate(
            buttonPanelPrefab,
            GetResultPanelPosition(),
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
        extremeMarkerPosition = Vector3.zero;
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
                UpdateDirectionIndicator(false);
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

        instructions.text = GetDirectionLabel() + " testi\n\n" + filledBody;
        UpdateDirectionIndicator(true);
    }

}
