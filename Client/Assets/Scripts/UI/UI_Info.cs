namespace DevelopersHub.ClashOfWhatecer
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Info : MonoBehaviour
    {

        [SerializeField] private GameObject _elements = null;
        [SerializeField] private Button _closeButton = null;
        [SerializeField] public TextMeshProUGUI _titleText = null;
        [SerializeField] public TextMeshProUGUI _descriptionText = null;
        [SerializeField] public Image _icon = null;

        private static UI_Info _instance = null; public static UI_Info instanse { get { return _instance; } }
        private bool _active = false; public bool isActive { get { return _active; } }

        private void Awake()
        {
            _instance = this;
            _elements.SetActive(false);
        }

        private void Start()
        {
            _closeButton.onClick.AddListener(Close);
        }

        private void Close()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            _active = false;
            _elements.SetActive(false);
        }      

        public void OpenBuildingInfo(Data.BuildingID id, int level)
        {
            Sprite icon = AssetsBank.GetBuildingIcon(id, level);
            if (icon != null)
            {
                _icon.sprite = icon;
            }
            _titleText.text = Language.instanse.GetBuildingName(id, level);
            switch (Language.instanse.language)
            {               
                default:
                    _descriptionText.horizontalAlignment = HorizontalAlignmentOptions.Left;
                    switch (id)
                    {
                        case Data.BuildingID.townhall:
                            _descriptionText.text = "A Prefeitura é o coração da sua vila! É o principal centro de organização da sua comunidade.";
                            break;
                        case Data.BuildingID.goldmine: // Usina de Adubo
                            _descriptionText.text = "Esta usina transforma lixo orgânico (como cascas de frutas) em adubo, que deixa o solo forte para novas plantas. Isso reduz muito o lixo nos aterros!\n\nConstruir uma garante 100 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.goldstorage: // Ponto de Reciclagem
                            _descriptionText.text = "Aqui o lixo seco é separado (plástico, papel, vidro) e ganha uma nova vida! Isso evita que o lixo polua os rios e matas, transformando-o em coisas novas.\n\nConstruir um garante 150 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.elixirmine: // Coletor de Água
                            _descriptionText.text = "Esta construção é super inteligente! Ela guarda a água da chuva, que pode ser usada para molhar plantas e limpar calçadas, economizando nossa água potável.\n\nConstruir um garante 150 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.elixirstorage: // Energia Eólica
                            _descriptionText.text = "Este moinho usa a força do vento para criar energia elétrica! É uma energia 100% limpa, que não solta fumaça e não polui o ar que respiramos.\n\nConstruir um garante 200 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.armycamp: // Horta
                            _descriptionText.text = "Plantar nossa própria comida! A Horta nos dá alimentos frescos e saudáveis, sem veneno. Isso também diminui a poluição dos caminhões que trazem comida de longe.\n\nConstruir uma garante 200 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.spellfactory: // Centro Comunitário
                            _descriptionText.text = "A união faz a força! O Centro Comunitário é o ponto de encontro para organizar ações verdes, como mutirões de limpeza, feiras de troca e cuidar da vizinhança.\n\nConstruir um garante 300 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.laboratory: // Eco-Centro
                            _descriptionText.text = "Um laboratório para salvar o planeta! O Eco-Centro é onde novas ideias e tecnologias verdes são criadas, como jeitos melhores de reciclar e de limpar a natureza.\n\nConstruir um garante 400 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.obstacle:
                            switch (level)
                            {
                                case 1: _descriptionText.text = ""; break;
                                case 2: _descriptionText.text = ""; break;
                                case 3: _descriptionText.text = ""; break;
                                case 4: _descriptionText.text = ""; break;
                                case 5: _descriptionText.text = ""; break;
                            }
                            break;
                        default:
                            _descriptionText.text = "";
                            break;
                    }
                    break;
            }
            _active = true;
            transform.SetAsLastSibling();
            _elements.SetActive(true);
            _titleText.ForceMeshUpdate(true);
            _descriptionText.ForceMeshUpdate(true);
        }       
    }
}