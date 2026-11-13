using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace ParadiseTycoon.Buildings
{
    public sealed class BuildingUpgradePanel : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text current;
        [SerializeField] TMP_Text next;
        [SerializeField] TMP_Text price;
        [SerializeField] TMP_Text feedback;
        [SerializeField] TMP_Text buyLabel;
        [SerializeField] UnityEngine.UI.Button buy;
        [SerializeField] UnityEngine.UI.Button close;
        [SerializeField] UnityEngine.UI.Button backdrop;

        static readonly CultureInfo Slovak = CultureInfo.GetCultureInfo("sk-SK");

        internal void Bind(Action buyAction, Action closeAction)
        {
            buy.onClick.AddListener(() => buyAction());
            close.onClick.AddListener(() => closeAction());
            backdrop.onClick.AddListener(() => closeAction());
        }

        internal void Show(ResortBuilding building, int coins, bool waveActive, bool confirming, string message)
        {
            var currentService = building.CurrentService;
            var nextService = building.GetLevel(building.Level + 1);
            bool built = currentService != null;
            bool hasUpgrade = built && nextService != null;
            int missing = hasUpgrade ? Mathf.Max(0, nextService.Cost - coins) : 0;

            title.text = building.DisplayName;
            current.text = built
                ? $"Úroveň {building.Level}  •  {Service(currentService)}"
                : "Stavenisko";
            next.text = hasUpgrade
                ? $"Úroveň {building.Level + 1}  •  {Service(nextService)}"
                : built ? "Maximálna úroveň" : "Vylepšenia budú dostupné po postavení budovy.";
            price.text = hasUpgrade
                ? $"Cena {nextService.Cost.ToString("N0", Slovak)} mincí   •   Máš {coins.ToString("N0", Slovak)}"
                : "";
            feedback.text = waveActive && hasUpgrade ? "Vylepšenia sú dostupné medzi vlnami."
                : missing > 0 ? $"Chýba {missing.ToString("N0", Slovak)} mincí."
                : confirming ? "Potvrď nákup vylepšenia."
                : message;
            buyLabel.text = confirming ? "Potvrdiť nákup" : "Vylepšiť";
            buy.gameObject.SetActive(hasUpgrade);
            buy.interactable = hasUpgrade && !waveActive && missing == 0;
            gameObject.SetActive(true);
        }

        static string Service(BuildingLevel level)
        {
            return $"Kapacita {level.Capacity}  •  {level.SecondsPerRequest.ToString("0.##", Slovak)} s / požiadavka";
        }
    }
}
