namespace DevelopersHub.ClashOfWhatecer
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_BuildingOptions : MonoBehaviour
    {

        [SerializeField] public GameObject _elements = null;

        private static UI_BuildingOptions _instance = null; public static UI_BuildingOptions instanse { get { return _instance; } }

        public RectTransform infoPanel = null;
        public RectTransform upgradePanel = null;
        public RectTransform instantPanel = null;
        public RectTransform trainPanel = null;
        public RectTransform clanPanel = null;
        public RectTransform spellPanel = null;
        public RectTransform researchPanel = null;
        public RectTransform removePanel = null;
        public RectTransform boostPanel = null;

        public Button infoButton = null;
        public Button upgradeButton = null;
        public Button instantButton = null;
        public Button trainButton = null;
        public Button clanButton = null;
        public Button spellButton = null;
        public Button researchButton = null;
        public Button removeButton = null;
        public Button boostButton = null;

        public TextMeshProUGUI instantCost = null;
        public TextMeshProUGUI removeCost = null;
        public TextMeshProUGUI boostCost = null;
        public Image removeCostIcon = null;
        [HideInInspector] public bool canDo = false;

        private void Awake()
        {
            _instance = this;
            _elements.SetActive(false);
        }

        public void SetStatus(bool status)
        {
            if (status && Building.selectedInstanse != null)
            {
                Data.BuildingID selectedID = Building.selectedInstanse.id;
                bool isConstructing = Building.selectedInstanse.data.isConstructing;
                infoPanel.gameObject.SetActive(UI_Main.instanse.isActive);

                if (selectedID == Data.BuildingID.tree && UI_Main.instanse.isActive && !isConstructing)
                {
                    removePanel.gameObject.SetActive(true);
                    int removeCostAmount = 50;
                    removeCostIcon.sprite = AssetsBank.instanse.elixirIcon;
                    removeCost.text = removeCostAmount.ToString();
                    removeCost.color = Color.white;
                    canDo = true;
                    removeCost.ForceMeshUpdate(true);
                }
                else
                {
                    removePanel.gameObject.SetActive(false);
                }

                upgradePanel.gameObject.SetActive(false);
                instantPanel.gameObject.SetActive(false);
                trainPanel.gameObject.SetActive(false);
                clanPanel.gameObject.SetActive(false);
                spellPanel.gameObject.SetActive(false);
                researchPanel.gameObject.SetActive(false);
                boostPanel.gameObject.SetActive(false);
            }

            _elements.SetActive(status);
        }
    }
}