namespace DevelopersHub.ClashOfWhatecer
{
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Research : MonoBehaviour
    {

        [SerializeField] private GameObject _elements = null;
        [SerializeField] private Button _closeButton = null;
        [SerializeField] private RectTransform _listRoot = null;
        [SerializeField] private GridLayoutGroup _listLayout = null;
        private static UI_Research _instance = null; public static UI_Research instanse { get { return _instance; } }
        private bool _active = false; public bool isActive { get { return _active; } }

        private void Awake()
        {
            _instance = this;
            _elements.SetActive(false);
        }

        private void Start()
        {
            _closeButton.onClick.AddListener(Close);
            float space = Screen.height * 0.01f;
            float height = ((Screen.height * _listRoot.anchorMax.y) - (space * 4f)) / 3f;
            _listLayout.spacing = new Vector2(space, space);
            _listLayout.cellSize = new Vector2(height * 0.9f, height);
        }

        public void SetStatus(bool status)
        {
            if (status)
            {

            }
            _elements.SetActive(status);
            _active = status;
        }

        public void ResearchResponse(int response, Data.Research research)
        {
            if (isActive)
            {
                if (response == 1)
                {
                    if (research.type == Data.ResearchType.unit)
                    {
                    }
                    else if (research.type == Data.ResearchType.spell)
                    {
                    }
                }
                else if (response == 2)
                {

                }
                else
                {

                }
            }
        }

        private void Close()
        {
            SetStatus(false);
            UI_Main.instanse.SetStatus(true);
        }

    }
}