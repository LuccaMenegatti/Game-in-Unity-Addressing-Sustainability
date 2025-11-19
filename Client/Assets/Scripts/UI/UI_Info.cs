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
                        case Data.BuildingID.townhall: // Prefeitura
                            _descriptionText.text =
                                "A Prefeitura é o coração da sua vila! É o principal centro de organização e gestão da sua comunidade. Fortalecer " +
                                "este centro é essencial para coordenar todas as iniciativas sustentáveis, alinhando-se à construção de " +
                                "cidades e comunidades sustentáveis (ODS 11).";
                            break;
                        case Data.BuildingID.goldmine: // Usina de Compostagem
                            _descriptionText.text =
                                "Esta usina transforma lixo orgânico em adubo rico e natural. Você reduz drasticamente resíduos em aterros " +
                                "e contribui para a segurança alimentar (ODS 2) com solo mais fértil. Um exemplo de produção e consumo responsáveis (ODS 12).\n\n" +
                                "Construir uma garante 100 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.goldstorage: // Ponto de Reciclagem
                            _descriptionText.text =
                                "Aqui, o lixo seco é separado para ser reintroduzido na economia circular. Evita a poluição de rios e solos, " +
                                "diminuindo a necessidade de extrair novos recursos. Uma ação crucial para o consumo e produção sustentáveis (ODS 12).\n\n" +
                                "Construir um garante 150 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.elixirmine: // Coletor de Água de Chuva
                            _descriptionText.text =
                                "Esta construção armazena a água da chuva, que pode ser usada para irrigar e limpar calçadas, economizando nossa água potável. " +
                                "Sua implementação contribui para a gestão sustentável da água dentro de sua comunidade sustentável (ODS 11).\n\n" +
                                "Construir um garante 150 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.elixirstorage: // Aerogerador (Energia Eólica)
                            _descriptionText.text =
                                "Este moinho gera energia elétrica limpa e renovável a partir da força do vento. Evita a queima de combustíveis fósseis " +
                                "e a poluição do ar, sendo um pilar para a construção de uma cidade com energia sustentável (ODS 11).\n\n" +
                                "Construir um garante 200 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.armycamp: // Horta Comunitária
                            _descriptionText.text =
                                "Cultivar nossa própria comida na Horta oferece alimentos frescos e saudáveis, fortalecendo a segurança alimentar (ODS 2). " +
                                "Reduz a poluição do transporte de alimentos. Uma ação direta para o fome zero e produção sustentável (ODS 12).\n\n" +
                                "Construir uma garante 200 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.spellfactory: // Centro Comunitário
                            _descriptionText.text =
                                "O Centro Comunitário é o ponto de encontro para organizar e mobilizar ações verdes, como mutirões e feiras de troca. " +
                                "Fomenta a integração social e a participação cívica, essencial para uma comunidade sustentável e inclusiva (ODS 11).\n\n" +
                                "Construir um garante 300 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.laboratory: // Eco-Centro de Pesquisa
                            _descriptionText.text =
                                "Um laboratório para inovar em soluções verdes! O Eco-Centro foca na criação de novas tecnologias e práticas sustentáveis, " +
                                "como métodos aprimorados de reciclagem. Investir em pesquisa é fundamental para garantir padrões de consumo e produção sustentáveis " +
                                "no futuro (ODS 12).\n\n" +
                                "Construir um garante 400 Pontos Sustentáveis.";
                            break;
                        case Data.BuildingID.tree: // Árvore 
                            _descriptionText.text =
                                "Um símbolo de vida e responsabilidade ecológica. Cuidar do planeta faz parte da sua jornada!\n\n" +
                                "Ao Plantar: Você gasta Ouro, e ganha 50 Pontos Sustentáveis.\n\n" +
                                "Ao Remover: Você ganha 100 Ouro, mas perde 50 Pontos Sustentáveis.\n\n" +
                                "Se organize para pontuar mais! O equilíbrio entre construir e preservar é a chave.";
                            break;
                        case Data.BuildingID.obstacle:
                            switch (level)
                            {
                                case 1:
                                case 2:
                                case 3:
                                    _descriptionText.text =
                                        "Um símbolo de vida e responsabilidade ecológica. Cuidar do planeta faz parte da sua jornada!\n\n" +
                                        "Ao Plantar: Você gasta Ouro, e ganha 50 Pontos Sustentáveis.\n\n" +
                                        "Ao Remover: Você ganha 100 Ouro, mas perde 50 Pontos Sustentáveis.\n\n" +
                                        "Se organize para pontuar mais! O equilíbrio entre construir e preservar é a chave.";
                                    break;
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