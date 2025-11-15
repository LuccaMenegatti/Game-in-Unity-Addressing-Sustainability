namespace DevelopersHub.ClashOfWhatecer
{
    using TMPro;
    using UnityEngine;

    public class UI_PlayerRank : MonoBehaviour
    {

        [SerializeField] private TextMeshProUGUI _nameText = null;
        [SerializeField] private TextMeshProUGUI _trophiesText = null;
        [SerializeField] private TextMeshProUGUI _rankText = null;
        [SerializeField] private TextMeshProUGUI _levelText = null;
        [SerializeField] private TextMeshProUGUI _goldText = null;
        [SerializeField] private TextMeshProUGUI _elixirText = null;

        private Data.PlayerRank _clan = null;

        public void Initialize(Data.PlayerRank player)
        {
            _clan = player;
            _levelText.text = player.level.ToString();
            _trophiesText.text = player.trophies.ToString();
            if (_goldText != null)
            {
                _goldText.text = player.gold.ToString();
                _goldText.ForceMeshUpdate(true);
            }
            if (_elixirText != null)
            {
                _elixirText.text = player.elixir.ToString();
                _elixirText.ForceMeshUpdate(true);
            }
            _rankText.text = player.rank.ToString();
            _nameText.text = Data.DecodeString(player.name);
            _levelText.ForceMeshUpdate(true);
            _trophiesText.ForceMeshUpdate(true);
            _nameText.ForceMeshUpdate(true);
            _rankText.ForceMeshUpdate(true);
        }
    }
}