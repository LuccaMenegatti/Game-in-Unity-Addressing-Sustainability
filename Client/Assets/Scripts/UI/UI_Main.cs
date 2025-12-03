namespace DevelopersHub.ClashOfWhatecer
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Main : MonoBehaviour
    {

        //[SerializeField] public TextMeshProUGUI logTest = null;
        [SerializeField] public GameObject _elements = null;
        [SerializeField] public TextMeshProUGUI _goldText = null;
        [SerializeField] public TextMeshProUGUI _elixirText = null;
        [SerializeField] public TextMeshProUGUI _usernameText = null;
        [SerializeField] public TextMeshProUGUI _xpText = null;
        [SerializeField] public TextMeshProUGUI _levelText = null;
        [SerializeField] public Image _goldBar = null;
        [SerializeField] public Image _elixirBar = null;
        [SerializeField] public Image _xpBar = null;
        [SerializeField] private Button _shopButton = null;
        [SerializeField] private Button _chatButton = null;
        [SerializeField] private Button _settingsButton = null;
        [SerializeField] private Button _rankingButton = null;
        [SerializeField] public BuildGrid _grid = null;
        [SerializeField] public Building[] _buildingPrefabs = null;

        [Header("Buttons")]
        public Transform buttonsParent = null;
        public UI_Button buttonCollectGold = null;
        public UI_Button buttonCollectElixir = null;
        public UI_Button buttonCollectDarkElixir = null;
        public UI_Bar barBuild = null;
        private static UI_Main _instance = null; public static UI_Main instanse { get { return _instance; } }

        private bool _active = true; public bool isActive { get { return _active; } }
        private int workers = 0;
        private int busyWorkers = 0; public bool haveAvalibaleBuilder { get { return busyWorkers < workers; } }

        /*
        public void Log(string text)
        {
            logTest.text = logTest.text + "\n" + text;
        }
        */

        private void Awake()
        {
            _instance = this;
            _elements.SetActive(true);
            _goldText.text = "";
            _elixirText.text = "";
            _usernameText.text = "";
            _xpText.text = "";
            _levelText.text = "";
            _goldBar.fillAmount = 0;
            _elixirBar.fillAmount = 0;
            _xpBar.fillAmount = 0;
        }

        private void Start()
        {
            _shopButton.onClick.AddListener(ShopButtonClicked);
            _chatButton.onClick.AddListener(ChatButtonClicked);
            _settingsButton.onClick.AddListener(SettingsButtonClicked);
            _rankingButton.onClick.AddListener(RankingButtonClicked);
            SoundManager.instanse.PlayMusic(SoundManager.instanse.mainMusic);
        }

        private void SettingsButtonClicked()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            UI_Settings.instanse.Open();
        }

        private void RankingButtonClicked()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            UI_PlayersRanking.instanse.Open();
        }

        private void ChatButtonClicked()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            UI_Chat.instanse.Open();
        }

        private void ShopButtonClicked()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            UI_Shop.instanse.SetStatus(true);
            SetStatus(false);
        }

        private void OnLeave()
        {
            UI_Build.instanse.Cancel();
        }

        public void SetStatus(bool status)
        {
            if (!status)
            {
                OnLeave();
            }
            else
            {
                if (SoundManager.instanse.musicSource.clip != SoundManager.instanse.mainMusic)
                {
                    SoundManager.instanse.PlayMusic(SoundManager.instanse.mainMusic);
                }
                Player.instanse.RushSyncRequest();
            }
            _active = status;
            _elements.SetActive(status);
        }

        public (Building, Data.ServerBuilding) GetBuildingPrefab(Data.BuildingID id)
        {
            Data.ServerBuilding server = Player.instanse.GetServerBuilding(id, 1);
            if (server != null)
            {
                for (int i = 0; i < _buildingPrefabs.Length; i++)
                {
                    if (_buildingPrefabs[i].id == id)
                    {
                        return (_buildingPrefabs[i], server);
                    }
                }
            }
            return (null, null);
        }

        public void DataSynced()
        {
            int _workers = 0;
            int _busyWorkers = 0;
            if (Player.instanse.data.buildings != null && Player.instanse.data.buildings.Count > 0)
            {
                for (int i = 0; i < Player.instanse.data.buildings.Count; i++)
                {
                    bool first = false;
                    if (Player.instanse.data.buildings[i].isConstructing && Player.instanse.data.buildings[i].buildTime > 0)
                    {
                        _busyWorkers += 1;
                    }
                    Building building = _grid.GetBuilding(Player.instanse.data.buildings[i].databaseID);
                    if (building != null)
                    {

                    }
                    else
                    {
                        building = _grid.GetBuilding(Player.instanse.data.buildings[i].id, Player.instanse.data.buildings[i].x, Player.instanse.data.buildings[i].y);
                        if (building != null)
                        {
                            _grid.RemoveUnidentifiedBuilding(building);
                            building.databaseID = Player.instanse.data.buildings[i].databaseID;
                            _grid.buildings.Add(building);
                        }
                        else
                        {
                            var prefab = GetBuildingPrefab(Player.instanse.data.buildings[i].id);
                            if (prefab.Item1)
                            {
                                building = Instantiate(prefab.Item1, Vector3.zero, Quaternion.identity);
                                building.rows = prefab.Item2.rows;
                                building.columns = prefab.Item2.columns;
                                building.databaseID = Player.instanse.data.buildings[i].databaseID;
                                building.lastChange = Player.instanse.lastUpdateSent.AddSeconds(-1);
                                first = true;
                                building.PlacedOnGrid(Player.instanse.data.buildings[i].x, Player.instanse.data.buildings[i].y);
                                if (building._baseArea)
                                {
                                    building._baseArea.gameObject.SetActive(false);
                                }
                                _grid.buildings.Add(building);
                            }
                            else
                            {
                                Debug.LogWarning("Building " + Player.instanse.data.buildings[i].id + " have no prefab.");
                                continue;
                            }
                        }
                    }

                    if (building.buildBar == null)
                    {
                        building.buildBar = Instantiate(barBuild, buttonsParent);
                        building.buildBar.gameObject.SetActive(false);
                    }

                    building.data = Player.instanse.data.buildings[i];
                    if (first)
                    {
                        building.lastChange = Player.instanse.lastUpdateSent.AddSeconds(-1);
                    }

                    switch (building.id)
                    {
                        case Data.BuildingID.goldmine:
                            if (building.collectButton == null)
                            {
                                building.collectButton = Instantiate(buttonCollectGold, buttonsParent);
                                building.collectButton.button.onClick.AddListener(building.Collect);
                                building.collectButton.gameObject.SetActive(false);
                            }
                            break;
                        case Data.BuildingID.elixirmine:
                            if (building.collectButton == null)
                            {
                                building.collectButton = Instantiate(buttonCollectElixir, buttonsParent);
                                building.collectButton.button.onClick.AddListener(building.Collect);
                                building.collectButton.gameObject.SetActive(false);
                            }
                            break;
                        case Data.BuildingID.darkelixirmine:
                            if (building.collectButton == null)
                            {
                                building.collectButton = Instantiate(buttonCollectDarkElixir, buttonsParent);
                                building.collectButton.button.onClick.AddListener(building.Collect);
                                building.collectButton.gameObject.SetActive(false);
                            }
                            break;
                        case Data.BuildingID.buildershut:
                            _workers += 1;
                            break;
                    }
                }
                _grid.RefreshBuildings();
            }
            if (Player.instanse.data.buildings != null)
            {
                for (int i = _grid.buildings.Count - 1; i >= 0; i--)
                {
                    bool found = false;
                    for (int j = 0; j < Player.instanse.data.buildings.Count; j++)
                    {
                        if (_grid.buildings[i].data.databaseID == Player.instanse.data.buildings[j].databaseID)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Destroy(_grid.buildings[i].gameObject);
                        _grid.buildings.RemoveAt(i);
                    }
                }
            }
            workers = _workers;
            busyWorkers = _busyWorkers;
        }
    }
}