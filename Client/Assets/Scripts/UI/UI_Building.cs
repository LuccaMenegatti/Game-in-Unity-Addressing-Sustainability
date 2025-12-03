namespace DevelopersHub.ClashOfWhatecer
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Building : MonoBehaviour
    {

        [SerializeField] private Data.BuildingID _id = Data.BuildingID.townhall; public Data.BuildingID id { set { _id = value; } }
        [SerializeField] private Button _button = null;
        [SerializeField] private Button _buttonInfo = null;
        [SerializeField] private Image _icon = null;
        [SerializeField] private Image _resourceIcon = null;
        [SerializeField] public TextMeshProUGUI _titleText = null;
        [SerializeField] public TextMeshProUGUI _resourceText = null;
        [SerializeField] public TextMeshProUGUI _timeText = null;
        [SerializeField] public TextMeshProUGUI _countText = null;

        private void Start()
        {
            _button.onClick.AddListener(Clicked);
            _buttonInfo.onClick.AddListener(Info);
        }

        private void Clicked()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            UI_Shop.instanse.PlaceBuilding(_id);
        }

        public void Initialize(bool haveWorker)
        {
            Data.ServerBuilding building = Player.instanse.GetServerBuilding(_id, 1);
            _titleText.text = Language.instanse.GetBuildingName(_id);
            if (Language.instanse.IsRTL && _titleText.horizontalAlignment == HorizontalAlignmentOptions.Left)
            {
                _titleText.horizontalAlignment = HorizontalAlignmentOptions.Right;
            }
            _titleText.ForceMeshUpdate(true);
            Sprite icon = AssetsBank.GetBuildingIcon(_id);
            if (icon != null)
            {
                _icon.sprite = icon;
            }

            if (building != null)
            {
                _button.interactable = haveWorker;
                int townHallLevel = 1;
                int count = 0;
                for (int i = 0; i < Player.instanse.data.buildings.Count; i++)
                {
                    if (Player.instanse.data.buildings[i].id == Data.BuildingID.townhall)
                    {
                        townHallLevel = Player.instanse.data.buildings[i].level;
                    }
                    if (Player.instanse.data.buildings[i].id == _id)
                    {
                        count++;
                    }
                }

                Data.BuildingCount limits = Data.GetBuildingLimits(townHallLevel, _id.ToString());

                if (limits != null)
                {
                    _countText.text = count.ToString() + "/" + limits.count.ToString();
                    if (count >= limits.count)
                    {
                        _button.interactable = false;
                    }
                }
                else
                {
                    _button.interactable = false;
                    _countText.text = "0/0";
                }

                _timeText.text = Tools.SecondsToTimeFormat(building.buildTime);

                _resourceText.text = building.requiredGold.ToString();
                _resourceIcon.sprite = AssetsBank.instanse.goldIcon;

                if (building.requiredGold <= Player.instanse.gold)
                {
                    _resourceText.color = Color.white;
                }
                else
                {
                    _resourceText.color = Color.red;
                    _button.interactable = false;
                }
            }
            else
            {
                _resourceText.color = Color.white;
                _timeText.text = "0";
                _resourceText.text = "0";
                _countText.text = "0/0";
                _resourceIcon.sprite = AssetsBank.instanse.goldIcon;
                _button.interactable = false;
            }
            _resourceText.ForceMeshUpdate(true);
            _timeText.ForceMeshUpdate(true);
            _countText.ForceMeshUpdate(true);
        }

        private void Info()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            UI_Info.instanse.OpenBuildingInfo(_id, 1);
        }

    }
}