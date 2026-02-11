using UnityEngine;
using TMPro;

public class JPE_TestManager : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;
    public TMP_Text instructionText;
    public TMP_Text resultText;
    public TMP_Text gripPushText;

    [Header("Settings")]
    public float distanceFromHead = 0.9f;

    private Vector3 neutralPoint;
    private Vector3 extremePoint;

    private bool neutralSet = false;

    private int gripPushCount = 0;

    private enum TestState
    {
        Inactive,
        WaitingForExtreme,
        WaitingForReturn
    }

    private TestState currentState = TestState.Inactive;

    void Update()
    {
        if (currentState == TestState.Inactive)
            return;

        bool leftGrip = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger);
        bool rightGrip = OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger);

        if (leftGrip || rightGrip)
        {
            HandleGrip();
            gripPushCount++;
            gripPushText.text = gripPushCount.ToString();
        }
    }

    // 🔹 Neutraali tallennetaan target-vaiheessa
    public void SaveNeutral(Vector3 headPosition, Vector3 forwardDirection)
    {
        forwardDirection.y = 0f;
        forwardDirection.Normalize();

        neutralPoint =
            headPosition + forwardDirection * distanceFromHead;

        neutralSet = true;

        if (instructionText != null)
            instructionText.text = "Neutraali tallennettu. Voit aloittaa testin.";
    }

    // 🔹 Käynnistetään testin aloitusnapista
    public void StartTest()
    {
        if (!neutralSet)
        {
            instructionText.text = "Aseta ensin neutraaliasento.";
            return;
        }

        resultText.text = "";

        instructionText.text =
            "Käännä pää ääriasentoon.\n\n" +
            "Paina Grip tallentaaksesi ääriasennon.\n\n"
            + neutralPoint.ToString();

        currentState = TestState.WaitingForExtreme;
    }

    void HandleGrip()
    {
        switch (currentState)
        {
            case TestState.WaitingForExtreme:
                SaveExtreme();
                instructionText.text =
                    "Palauta pää lähtöasentoon.\n\n" +
                    "Paina Grip mitataksesi virheen.\n\n" +
                    extremePoint.ToString();
                currentState = TestState.WaitingForReturn;
                break;

            case TestState.WaitingForReturn:
                MeasureReturn();
                currentState = TestState.Inactive;
                break;
        }
    }

    void SaveExtreme()
    {
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        extremePoint =
            headTransform.position + forward * distanceFromHead;
    }

    void MeasureReturn()
    {
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 currentPoint =
            headTransform.position + forward * distanceFromHead;

        float errorDistance =
            Vector3.Distance(neutralPoint, currentPoint);

        float angleDeg =
            Mathf.Atan(errorDistance / distanceFromHead) * Mathf.Rad2Deg;

        resultText.text =
            "JPE-virhe: " + angleDeg.ToString("F2") + "°";

        instructionText.text =
            "Testi valmis.\n\nVoit tehdä uuden mittauksen.";
    }
}
