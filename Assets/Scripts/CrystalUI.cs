using UnityEngine;
using TMPro;

public class CrystalUI : MonoBehaviour
{
    private TextMeshProUGUI text;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    void Update()
    {
        UpdateText();
    }

    void UpdateText()
    {
        text.text = "Кристаллы: " + PlayerController.crystalsCollected + "/" + PlayerController.totalCrystals;
    }
}