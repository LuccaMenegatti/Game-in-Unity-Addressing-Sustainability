namespace DevelopersHub.ClashOfWhatecer
{
    using DevelopersHub.RealtimeNetworking.Client;
    using System.Collections;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Score : MonoBehaviour
    {
        [Header("Conexões do Template")]
        [SerializeField] private Button buttonClose;
        [SerializeField] private GameObject _elements;

        [Header("Elementos de Pontuação")]
        [SerializeField] private TextMeshProUGUI textoPontosSustentaveis;
        [SerializeField] private TextMeshProUGUI textoDinheiro;

        [Header("Configurações da Animação")]
        [SerializeField] private float duracaoContagem = 1.5f;
        [SerializeField] private Color corDinheiroPositivo = Color.green;
        [SerializeField] private Color corDinheiroNegativo = Color.red;

        private const string PREFIXO_PONTOS = "Pontos sustentaveis: ";
        private const string PREFIXO_DINHEIRO = "Capital acumulado: ";

        private bool _active;

        void Start()
        {
            if (buttonClose != null)
            {
                buttonClose.onClick.AddListener(CloseAndLogoutDirect);
            }
        }

        public void ShowOnTimerEnd()
        {
            int pontosGanhos = Player.instanse.elixir;
            int dinheiroGanho = Player.instanse.gold;

            Show(pontosGanhos, dinheiroGanho);
        }

        public void Show(int pontos, int dinheiro)
        {
            textoPontosSustentaveis.text = PREFIXO_PONTOS + "0";
            textoDinheiro.text = ""; 

            _active = true;
            if (_elements != null) { _elements.SetActive(true); }
            gameObject.SetActive(true);

            StartCoroutine(AnimarSequenciaShow(pontos, dinheiro));
        }

        private IEnumerator AnimarSequenciaShow(int pontos, int dinheiro)
        {
            yield return StartCoroutine(ContarNumero(textoPontosSustentaveis, pontos, PREFIXO_PONTOS));

            yield return new WaitForSeconds(0.25f);

            string hexColor;
            if (dinheiro > 0)
            {
                hexColor = "#" + ColorUtility.ToHtmlStringRGB(corDinheiroPositivo);
            }
            else if (dinheiro < 0)
            {
                hexColor = "#" + ColorUtility.ToHtmlStringRGB(corDinheiroNegativo);
            }
            else
            {
                hexColor = "#FFFFFF"; 
            }

            string numeroTexto = dinheiro.ToString("$0; $0; $0");

            textoDinheiro.text = PREFIXO_DINHEIRO + "<color=" + hexColor + ">" + numeroTexto + "</color>";

            textoDinheiro.color = Color.white;
        }

        private IEnumerator ContarNumero(TextMeshProUGUI texto, int valorFinal, string prefixo)
        {
            float tempoPassado = 0f;
            int valorInicial = 0;

            while (tempoPassado < duracaoContagem)
            {
                tempoPassado += Time.deltaTime;
                float fracao = tempoPassado / duracaoContagem;

                int valorAtual = (int)Mathf.Lerp(valorInicial, valorFinal, fracao);

                texto.text = prefixo + valorAtual.ToString();

                yield return null;
            }

            texto.text = prefixo + valorFinal.ToString();
        }

        public void CloseAndLogoutDirect()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);

            Packet packet = new Packet();
            packet.Write((int)Player.RequestsID.LOGOUT);
            string device = SystemInfo.deviceUniqueIdentifier;
            packet.Write(device);
            Sender.TCP_Send(packet);
            PlayerPrefs.DeleteAll();
            Player.RestartGame();

            _active = false;
            if (_elements != null) { _elements.SetActive(false); }
            gameObject.SetActive(false);
        }
    }
}