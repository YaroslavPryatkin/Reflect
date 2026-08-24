using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmountOfDeathsTextController : MonoBehaviour
{
    private TextMeshProUGUI _amountOfDeathsText;

    private void Awake()
    {
        _amountOfDeathsText= GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        var myScene = gameObject.scene.name;
        var myGameSave = GameSavings.GetOrCreate(myScene, true);
        _amountOfDeathsText.text = "Deaths: " + myGameSave.AmountOfDeaths;
    }
}