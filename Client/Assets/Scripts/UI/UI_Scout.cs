namespace DevelopersHub.ClashOfWhatecer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Scout : MonoBehaviour
    {

        [SerializeField] public GameObject _elements = null;
        [SerializeField] private Button _backButton = null;
        [SerializeField] public GameObject _panelReport = null;
        [SerializeField] public GameObject _panelScout = null;
        [SerializeField] public TextMeshProUGUI _playerNameText = null;
        [SerializeField] public TextMeshProUGUI _timerText = null;
        [SerializeField] public TextMeshProUGUI _percentageText = null;
        [SerializeField] private GameObject _damagePanel = null;
        [SerializeField] private GameObject _star1 = null;
        [SerializeField] private GameObject _star2 = null;
        [SerializeField] private GameObject _star3 = null;
        [SerializeField] private RectTransform healthBarGrid = null;
        [SerializeField] private Button _playButton = null;
        [SerializeField] private Button _pauseButton = null;
        [SerializeField] private Button _replayButton = null;

        private Player.Panel _backPanel = Player.Panel.main;
        private static UI_Scout _instance = null; public static UI_Scout instanse { get { return _instance; } }
        private bool _active = false; public bool isActive { get { return _active; } }
        private Data.Player _player = null;
        private Data.BattleType _type = Data.BattleType.normal;

        private void Awake()
        {
            _instance = this;
            _elements.SetActive(false);
        }

        private void Start()
        {
            _backButton.onClick.AddListener(Back);
            _playButton.onClick.AddListener(PlayReplyFirstTime);
            _pauseButton.onClick.AddListener(PauseReply);
            _replayButton.onClick.AddListener(ReplayReort);
        }

        public void Open(Data.Player player, Data.BattleType type, Data.BattleReport report)
        {
            _type = type;
            if (report != null)
            {
                _player = player;
                _report = report;
                _panelScout.SetActive(false);
                _panelReport.SetActive(true);
                Display();
            }
            else if (player != null)
            {
                _player = player;
                _panelScout.SetActive(true);
                _panelReport.SetActive(false);
                PlaceBuildings();
            }            
            else
            {
                _backPanel = Player.Panel.main;
            }
            if (UI_Main.instanse.isActive)
            {
                UI_Main.instanse.SetStatus(false);
            }
            _active = true;
            _elements.SetActive(true);
        }

        private void PlaceBuildings()
        {
            UI_Main.instanse._grid.Clear();
            for (int i = 0; i < _player.buildings.Count; i++)
            {
                if(_type == Data.BattleType.war && (_player.buildings[i].warX < 0 || _player.buildings[i].warY < 0))
                {
                    continue;
                }
                var prefab = UI_Main.instanse.GetBuildingPrefab(_player.buildings[i].id);
                if (prefab.Item1 != null)
                {
                    Building building = Instantiate(prefab.Item1, Vector3.zero, Quaternion.identity);
                    building.scout = true;
                    building.data = _player.buildings[i];
                    building.rows = prefab.Item2.rows;
                    building.columns = prefab.Item2.columns;
                    building.databaseID = _player.buildings[i].databaseID;
                    if (_type == Data.BattleType.war)
                    {
                        building.PlacedOnGrid(_player.buildings[i].warX, _player.buildings[i].warY, true, true);
                    }
                    else
                    {
                        building.PlacedOnGrid(_player.buildings[i].x, _player.buildings[i].y, true, true);
                    }
                    if (building._baseArea)
                    {
                        building._baseArea.gameObject.SetActive(false);
                    }
                    UI_Main.instanse._grid.buildings.Add(building);
                }
            }
            UI_Main.instanse._grid.RefreshBuildings();
        }

        private void PlayReply()
        {
            isStarted = true;
            baseTime.AddMilliseconds((DateTime.Now - pauseTime).TotalMilliseconds);
            Time.timeScale = 1f;
            _playButton.gameObject.SetActive(false);
            _pauseButton.gameObject.SetActive(true);
            _replayButton.gameObject.SetActive(false);
        }

        private void PlayReplyFirstTime()
        {
            baseTime = DateTime.Now;
            PlayReply();
        }

        private void ReplayReort()
        {
            Display();
            PlayReply();
        }

        private void PauseReply()
        {
            isStarted = false;
            pauseTime = DateTime.Now;
            Time.timeScale = 0f;
            _playButton.gameObject.SetActive(true);
            _pauseButton.gameObject.SetActive(false);
            _replayButton.gameObject.SetActive(false);
        }

        private void Back()
        {
            Close();
        }

        private void Close()
        {
            _active = false;
            isStarted = false;
            Time.timeScale = 1f;       
            UI_Main.instanse._grid.Clear();
            Player.instanse.SyncData(Player.instanse.data);
            if (_backPanel == Player.Panel.clan)
            {
                UI_Main.instanse.SetStatus(true);              
            }
            else
            {
                UI_Main.instanse.SetStatus(true);
            }
            _elements.SetActive(false);
        }

        private Data.BattleReport _report = null;
        private bool isStarted = false;
        private DateTime baseTime;
        private DateTime pauseTime;

        public bool Display()
        {
            _playerNameText.text = Data.DecodeString(_player.name);
            _damagePanel.SetActive(false);
            _star1.SetActive(false);
            _star2.SetActive(false);
            _star3.SetActive(false);
            _playButton.gameObject.SetActive(true);
            _pauseButton.gameObject.SetActive(false);
            _replayButton.gameObject.SetActive(false);

            int townhallLevel = 1;
            for (int i = 0; i < _report.buildings.Count; i++)
            {
                if (_report.buildings[i].id == Data.BuildingID.townhall)
                {
                    townhallLevel = _report.buildings[i].level;
                    //break;
                }
                _report.buildings[i].x -= Data.battleGridOffset;
                _report.buildings[i].y -= Data.battleGridOffset;
            }
            /*
            for (int i = 0; i < _report.frames.Count; i++)
            {
                for (int j = 0; j < _report.frames[i].units.Count; j++)
                {
                    _report.frames[i].units[j].x -= Data.battleGridOffset;
                    _report.frames[i].units[j].y -= Data.battleGridOffset;
                }
                for (int j = 0; j < _report.frames[i].spells.Count; j++)
                {
                    _report.frames[i].spells[j].x -= Data.battleGridOffset;
                    _report.frames[i].spells[j].y -= Data.battleGridOffset;
                }
            }
            */

            for (int i = 0; i < _report.buildings.Count; i++)
            {
                if (_report.buildings[i].x < 0 || _report.buildings[i].y < 0)
                {
                    continue;
                }

                _timerText.text = TimeSpan.FromSeconds(Data.battleDuration).ToString(@"mm\:ss");

                UI_Main.instanse._grid.Clear();

                if (_type == Data.BattleType.normal)
                {
                    int townHallLevel = 1;
                    for (i = 0; i < Player.instanse.data.buildings.Count; i++)
                    {
                        if (Player.instanse.data.buildings[i].id == Data.BuildingID.townhall)
                        {
                            townHallLevel = Player.instanse.data.buildings[i].level;
                        }
                    }
                }

                baseTime = DateTime.Now;

                _percentageText.text = Mathf.RoundToInt((float)(100f)).ToString() + "%";
                //UpdateLoots();

                //var trophies = Data.GetBattleTrophies(Player.instanse.data.trophies, player.trophies);
                //_winTrophiesText.text = trophies.Item1.ToString();
                //_looseTrophiesText.text = "-" + trophies.Item2.ToString();


                isStarted = false;

                return true;
            }

            return false;
        }     
    }
}