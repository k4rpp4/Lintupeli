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

    private Vector3 startForward;
    private Vector3 extremeForward;
    private Vector3 endForward;

    private int index = 0;

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

        Vector3 targetPosition =
            headTransform.position + forward * distanceFromHead;

        Quaternion targetRotation =
            Quaternion.LookRotation(forward);

        GameObject newTarget =
            Instantiate(jpeTargetPrefab, targetPosition, targetRotation, jpeTargetsParent);

        newTarget.name = pointNames[index];

        TMP_Text textComponent =
            newTarget.GetComponentInChildren<TMP_Text>();

        if (textComponent != null)
        {
            textComponent.text =
                pointNames[index] + "\n\nDir:\n" +
                forward.ToString("F3");
        }

        // 🔹 FORCE INVISIBLE WHEN CREATED
        JPETargetVisual visual =
            newTarget.GetComponent<JPETargetVisual>();

        if (visual != null)
            visual.SetVisible(false);

        // Store direction
        if (index == 0)
            startForward = forward;

        if (index == 1)
            extremeForward = forward;

        if (index == 2)
        {
            endForward = forward;

            RevealAllTargets();
            CalculateJPEAngle();

            instructions.text = "";

            testComplete = true;
        }

    }


    void CalculateJPEAngle()
    {
        if (resultText == null)
            return;

        float angle = Vector3.Angle(startForward, endForward);

        resultText.text =
            "JPE-virhe: " + angle.ToString("F2") + "°";
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
            startPoint.rotation
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
                btn.onClick.AddListener(RestartTest);

            if (btn.name.Contains("Exit"))
                btn.onClick.AddListener(ExitTest);
        }
    }




    public void RestartTest()
    {
        instructions.text = firstInstruction;
        for (int i = jpeTargetsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(jpeTargetsParent.GetChild(i).gameObject);
        }

        if (currentButtonPanel != null)
            Destroy(currentButtonPanel);

        if (resultText != null)
            resultText.text = "";

        if (onRestartTest != null)
            onRestartTest.Invoke();

        startForward = Vector3.zero;
        extremeForward = Vector3.zero;
        endForward = Vector3.zero;

        testComplete = false;

        if (instructions != null)
            instructions.text = firstInstruction;
        index = 0;
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
