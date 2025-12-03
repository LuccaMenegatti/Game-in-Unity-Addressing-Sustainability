namespace DevelopersHub.ClashOfWhatecer
{
    using DevelopersHub.RealtimeNetworking.Client;
    using System.Collections;
    using System.Linq;
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
        [SerializeField] private TextMeshProUGUI textFeedback;

        [Header("Configurações da Animação")]
        [SerializeField] private float duracaoContagem = 1.5f;
        [SerializeField] private float velocidadeDigitacao = 0.02f;
        [SerializeField] private Color corDinheiroPositivo = Color.green;
        [SerializeField] private Color corDinheiroNegativo = Color.red;

        private const string PREFIXO_PONTOS = "Pontos sustentáveis: ";
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

            int qtdArvores = 0;

            if (Player.instanse.data.buildings != null)
            {
                qtdArvores = Player.instanse.data.buildings.Count(b => b.id == Data.BuildingID.tree);
            }

            Show(pontosGanhos, dinheiroGanho, qtdArvores);
        }

        public void Show(int pontos, int dinheiro, int qtdArvores)
        {
            if (textoPontosSustentaveis) textoPontosSustentaveis.text = PREFIXO_PONTOS + "0";
            if (textoDinheiro) textoDinheiro.text = "";
            if (textFeedback) textFeedback.text = "";

            _active = true;
            if (_elements != null) { _elements.SetActive(true); }
            gameObject.SetActive(true);

            StartCoroutine(AnimarSequenciaShow(pontos, dinheiro, qtdArvores));
        }

        private IEnumerator AnimarSequenciaShow(int pontos, int dinheiro, int qtdArvores)
        {
            if (textoPontosSustentaveis)
                yield return StartCoroutine(ContarNumero(textoPontosSustentaveis, pontos, PREFIXO_PONTOS));

            yield return new WaitForSeconds(0.2f);

            if (textoDinheiro)
            {
                string hexColor;
                if (dinheiro > 0) hexColor = "#" + ColorUtility.ToHtmlStringRGB(corDinheiroPositivo);
                else if (dinheiro < 0) hexColor = "#" + ColorUtility.ToHtmlStringRGB(corDinheiroNegativo);
                else hexColor = "#FFFFFF";

                string numeroTexto = dinheiro.ToString("$0; $0; $0");
                textoDinheiro.text = PREFIXO_DINHEIRO + "<color=" + hexColor + ">" + numeroTexto + "</color>";
            }

            yield return new WaitForSeconds(0.3f);

            if (textFeedback)
            {
                string mensagemFinal = GerarFeedbackComposto(pontos, dinheiro, qtdArvores);
                yield return StartCoroutine(AnimarTextoDigitando(textFeedback, mensagemFinal));
            }
        }

        private string GerarFeedbackComposto(int pontos, int dinheiro, int arvores)
        {
            string msgPrincipal = "";

            int rico = 800;
            int pobre = 150;
            int ecoBom = 400;
            int ecoRuim = 150;

            if (dinheiro < 0)
                msgPrincipal = "<color=red>Colapso Financeiro:</color> Sem capital, não há cidade. A sustentabilidade exige uma economia funcional.";
            else if (dinheiro >= rico && pontos >= 600)
                msgPrincipal = "<color=green>Visão de Futuro:</color> Perfeito! Você alcançou a harmonia total entre lucro e natureza viva.";
            else if (dinheiro >= rico && pontos <= ecoRuim)
                msgPrincipal = "<color=yellow>Magnata Irresponsável:</color> Riqueza extrema às custas do ambiente. Sua cidade é rica, mas tóxica.";
            else if (dinheiro <= pobre && pontos >= ecoBom)
                msgPrincipal = "<color=yellow>Natureza Intacta, Cofre Vazio:</color> O ambiente agradece, mas a cidade estagnou economicamente.";
            else if (dinheiro > pobre && pontos > ecoRuim)
                msgPrincipal = "<color=#ADD8E6>Crescimento Sustentável:</color> Uma gestão consciente. Você está no caminho certo.";
            else
                msgPrincipal = "<color=grey>Gestão Ineficiente:</color> Resultados baixos. Repense sua estratégia de alocação de recursos.";

            msgPrincipal = "<b>" + msgPrincipal + "</b>";

            if (arvores <= 18)
            {
                msgPrincipal += "\n\n<b><color=yellow>Alerta de desmatamento:</color> A cobertura vegetal está crítica! Plante mais urgentemente.</b>";
            }

            return msgPrincipal;
        }

        private IEnumerator AnimarTextoDigitando(TextMeshProUGUI elementoTexto, string fraseCompleta)
        {
            elementoTexto.text = fraseCompleta;
            elementoTexto.maxVisibleCharacters = 0;
            elementoTexto.ForceMeshUpdate();

            int totalCaracteres = elementoTexto.textInfo.characterCount;

            for (int i = 0; i <= totalCaracteres; i++)
            {
                elementoTexto.maxVisibleCharacters = i;
                yield return new WaitForSeconds(velocidadeDigitacao);
            }
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
            if (SoundManager.instanse != null)
            {
                SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            }

            Packet packet = new Packet();
            packet.Write((int)Player.RequestsID.LOGOUT);
            packet.Write(SystemInfo.deviceUniqueIdentifier);

            Sender.TCP_Send(packet);

            PlayerPrefs.DeleteAll();
            Player.RestartGame();

            _active = false;
            if (_elements != null) { _elements.SetActive(false); }
            gameObject.SetActive(false);
        }
    }
}