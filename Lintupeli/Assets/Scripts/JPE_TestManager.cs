using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class JPE_TestManager : MonoBehaviour
{
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
                float distCm = Vector3.Distance(startMarkerPosition, targetPosition) * 100f;
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

    void CalculateJPEDistance()
    {
        float distanceCm = Vector3.Distance(startMarkerPosition, endMarkerPosition) * 100f;
        trialResults.Add(distanceCm);

        if (resultText == null)
            return;

        string text = "Kierros " + currentTrial + "/" + totalTrials +
                      "\nJPE-virhe: " + distanceCm.ToString("F1") + " cm";

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

        if (instructions != null)
            instructions.text = firstInstruction;
    }

    public void RestartTest()
    {
        ClearMarkers();

        currentTrial = 0;
        trialResults.Clear();

        if (onRestartTest != null)
            onRestartTest.Invoke();

        if (instructions != null)
            instructions.text = firstInstruction;
    }

    public void ExitTest()
    {
        RestartTest();

        if (onExitTest != null)
            onExitTest.Invoke();
    }

    void UpdateInstructionText(int index)
    {
        if (instructions == null)
            return;

        switch (index)
        {
            case 0:
                instructions.text = firstInstruction;
                break;

            case 1:
                instructions.text = secondInstruction;
                break;

            case 2:
                instructions.text = thirdInstruction;
                break;

            default:
                instructions.text = "";
                break;
        }
    }

}
