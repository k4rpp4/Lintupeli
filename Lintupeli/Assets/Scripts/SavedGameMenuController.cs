using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Button, interactable

public class SavedGameMenuController : MonoBehaviour
{
    [Header("References")]
    public FlockCheckpointController checkpointController;
    public CheckpointReviewDisplay checkpointReviewDisplay;

    [Header("Slot Buttons (6)")]
    public Button[] slotButtons; // assign SavedGameButton 0-5 in order

    [Header("UI Elements")]
    public TMP_Text pageText;
    public Button loadButton;
    public Button nextButton;
    public Button previousButton;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.85f, 0f);

    private const int SlotsPerPage = 6;
    private List<string> saveFiles = new List<string>();
    private int currentPage = 0;
    private int selectedSaveIndex = -1; // index into saveFiles, -1 = none

    void Start()
    {
        Refresh();
    }

    // Call on menu open and after any save/delete operation
    public void Refresh()
    {
        ScanSaveFiles();
        currentPage = 0;
        selectedSaveIndex = -1;
        UpdateDisplay();
        UpdateLoadButton();
    }

    // Reads all .json files from the persistent data path
    void ScanSaveFiles()
    {
        saveFiles.Clear();
        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.json");
        foreach (string file in files)
            saveFiles.Add(Path.GetFileName(file));
        saveFiles.Sort();
    }

    // Assign to each SavedGameButton with its fixed index value (0-5)
    public void OnSlotClicked(int slotIndex)
    {
        int saveIndex = currentPage * SlotsPerPage + slotIndex;
        if (saveIndex >= saveFiles.Count) return;

        selectedSaveIndex = saveIndex;
        UpdateSlotColors();
        UpdateLoadButton();
    }

    // Assign to NextButton
    public void NextPage()
    {
        currentPage++;
        UpdateDisplay();
    }

    // Assign to PreviousButton
    public void PreviousPage()
    {
        currentPage--;
        UpdateDisplay();
    }

    // Assign to LoadButton
    public void LoadSelected()
    {
        if (selectedSaveIndex < 0 || selectedSaveIndex >= saveFiles.Count) return;

        checkpointController.saveFileName = saveFiles[selectedSaveIndex];
        checkpointController.LoadCheckpoints();

        if (checkpointReviewDisplay != null && checkpointReviewDisplay.gameObject.activeSelf)
            checkpointReviewDisplay.SpawnMarkers();
    }

    void UpdateDisplay()
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)saveFiles.Count / SlotsPerPage));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        pageText.text = (currentPage + 1) + " / " + totalPages;

        int pageStart = currentPage * SlotsPerPage;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int saveIndex = pageStart + i;
            bool hasSave = saveIndex < saveFiles.Count;

            slotButtons[i].gameObject.SetActive(hasSave);

            if (hasSave)
            {
                TMP_Text label = slotButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = Path.GetFileNameWithoutExtension(saveFiles[saveIndex]);
            }
        }

        // hide nav buttons when there is no page to go to
        previousButton.gameObject.SetActive(currentPage > 0);
        nextButton.gameObject.SetActive(currentPage < totalPages - 1);

        UpdateSlotColors();
    }

    // Changes the TMP text color per slot — untouched by the Interaction SDK
    void UpdateSlotColors()
    {
        int pageStart = currentPage * SlotsPerPage;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (!slotButtons[i].gameObject.activeSelf) continue;

            TMP_Text label = slotButtons[i].GetComponentInChildren<TMP_Text>();
            if (label == null) continue;

            bool isSelected = (pageStart + i) == selectedSaveIndex;
            label.color = isSelected ? selectedColor : normalColor;
        }
    }

    // Keeps LoadButton non-interactable until a slot is selected
    void UpdateLoadButton()
    {
        loadButton.interactable = selectedSaveIndex >= 0;
    }
}
