namespace DevelopersHub.ClashOfWhatecer 
{ 
    using UnityEngine;
    using UnityEngine.UI;
    using DevelopersHub.RealtimeNetworking.Client;

    public class UI_Settings : MonoBehaviour
    {

        [SerializeField] private GameObject _elements = null;
        [SerializeField] private Button _closeButton = null;
        [SerializeField] private Button _privacyButton = null;
        [SerializeField] private Button _logoutButton = null;
        [SerializeField] private Button _musicButton = null;
        [SerializeField] private Button _soundButton = null;
        [SerializeField] private Image _musicMute = null;
        [SerializeField] private Image _musicUnmute = null;
        [SerializeField] private Image _soundMute = null;
        [SerializeField] private Image _soundUnmute = null;

        private static UI_Settings _instance = null; public static UI_Settings instanse { get { return _instance; } }
        private bool _active = false; public bool isActive { get { return _active; } }
        private string email = "";

        private void Awake()
        {
            _instance = this;
            _elements.SetActive(false);
        }

        private void Start()
        {
            _closeButton.onClick.AddListener(Close);
            _logoutButton.onClick.AddListener(LogOut);
            _musicButton.onClick.AddListener(MusicMute);
            _soundButton.onClick.AddListener(SoundMute);
            _privacyButton.onClick.AddListener(PrivacyPolicyClicked);
        }

        private void PrivacyPolicyClicked()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            UI_PrivacyPolicy.instanse.Open();
        }


        public void Open()
        {              
            UpdateSoundButtons();

            _active = true;
            _elements.SetActive(true);
        }

        private void UpdateSoundButtons()
        {
            try
            {
                if (PlayerPrefs.HasKey("music_mute"))
                {
                    _musicMute.gameObject.SetActive(PlayerPrefs.GetInt("music_mute") == 1);
                    _musicUnmute.gameObject.SetActive(PlayerPrefs.GetInt("music_mute") != 1);
                }
                else
                {
                    _musicMute.gameObject.SetActive(false);
                    _musicUnmute.gameObject.SetActive(true);
                }
                if (PlayerPrefs.HasKey("sound_mute"))
                {
                    _soundMute.gameObject.SetActive(PlayerPrefs.GetInt("sound_mute") == 1);
                    _soundUnmute.gameObject.SetActive(PlayerPrefs.GetInt("sound_mute") != 1);
                }
                else
                {
                    _soundMute.gameObject.SetActive(false);
                    _soundUnmute.gameObject.SetActive(true);
                }
            }
            catch (System.Exception)
            {

            }
        }

        private void SoundMute()
        {
            try
            {
                int status = 0;
                if (PlayerPrefs.HasKey("sound_mute"))
                {
                    status = PlayerPrefs.GetInt("sound_mute");
                }
                if(status == 1)
                {
                    status = 0;
                }
                else
                {
                    status = 1;
                }
                PlayerPrefs.SetInt("sound_mute", status);
                SoundManager.instanse.soundMute = (status == 1);
            }
            catch (System.Exception)
            {

            }
            UpdateSoundButtons();
        }

        private void MusicMute()
        {
            try
            {
                int status = 0;
                if (PlayerPrefs.HasKey("music_mute"))
                {
                    status = PlayerPrefs.GetInt("music_mute");
                }
                if (status == 1)
                {
                    status = 0;
                }
                else
                {
                    status = 1;
                }
                PlayerPrefs.SetInt("music_mute", status);
                SoundManager.instanse.musicMute = (status == 1);
            }
            catch (System.Exception)
            {

            }
            UpdateSoundButtons();
        }

        public void Close()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);
            _active = false;
            _elements.SetActive(false);
        }

        public void EmailSendResponse(int response, int expiration)
        {
            Loading.Close();
            if (response == 1)
            {
                MessageBox.Open(2, 0.8f, true, MessageResponded,
                    new string[] { "Por favor, insira o código de confirmação. Você pode encontrá-lo na sua caixa de entrada ou na pasta de spam do seu e-mail." },
                    new string[] { "Confirmar", "Cancelar" }, null, new string[] { "" });
            }
            else if(response == 3)
            {
                MessageBox.Open(1, 0.8f, true, MessageResponded,
                    new string[] { "Este e-mail já está vinculado a outra conta." },
                    new string[] { "OK" });
            }
            else
            {
                MessageBox.Open(1, 0.8f, true, MessageResponded,
                    new string[] { "O código não é válido." },
                    new string[] { "OK" });
            }
        }

        public void EmailConfirmResponse(int response, string password)
        {
            if (response == 1)
            {
                Player.instanse.data.email = email;
                Open();
            }
            else if (response == 3)
            {
                MessageBox.Open(1, 0.8f, true, MessageResponded,
                    new string[] { "Este e-mail já está vinculado a outra conta." },
                    new string[] { "OK" });
            }
            else
            {
                MessageBox.Open(1, 0.8f, true, MessageResponded,
                    new string[] { "O código não é válido." },
                    new string[] { "OK" });
            }
        }

        private void MessageResponded(int layoutIndex, int buttonIndex)
        {
            if (layoutIndex == 1)
            {
                MessageBox.Close();
            }
            else if(layoutIndex == 2)
            {
                if (buttonIndex == 0)
                {
                    string code = MessageBox.GetInputValue(2, 0).Trim();
                    if (!string.IsNullOrEmpty(code))
                    {
                        MessageBox.Close();
                        Packet packet = new Packet();
                        packet.Write((int)Player.RequestsID.EMAILCONFIRM);
                        string device = SystemInfo.deviceUniqueIdentifier;
                        packet.Write(device);
                        packet.Write(email);
                        packet.Write(code);
                        Sender.TCP_Send(packet);
                    }
                }
                else
                {
                    MessageBox.Close();
                }
            }
            else if (layoutIndex == 3)
            {
                if (buttonIndex == 0)
                {
                    Packet packet = new Packet();
                    packet.Write((int)Player.RequestsID.LOGOUT);
                    string device = SystemInfo.deviceUniqueIdentifier;
                    packet.Write(device);
                    Sender.TCP_Send(packet);
                    PlayerPrefs.DeleteAll();
                    Player.RestartGame();
                }
                MessageBox.Close();
            }
            else if (layoutIndex == 4)
            {
                if (buttonIndex == 0)
                {
                    string str = MessageBox.GetInputValue(layoutIndex, 0).Trim();
                    if(str.Length > 20)
                    {
                        MessageBox.Open(1, 0.8f, true, MessageResponded,
                            new string[] { "O nome não pode ter mais de 20 caracteres." },
                            new string[] { "OK" });
                    }
                    else if (!string.IsNullOrEmpty(str) && Data.IsMessageGoodToSend(str))
                    {
                        MessageBox.Close();
                        Packet packet = new Packet();
                        packet.Write((int)Player.RequestsID.RENAME);
                        packet.Write(Data.EncodeString(str));
                        Sender.TCP_Send(packet);
                    }
                    else
                    {
                        MessageBox.Open(1, 0.8f, true, MessageResponded,
                            new string[] { "Este nome não é válido." },
                            new string[] { "OK" });
                    }
                }
                else
                {
                    MessageBox.Close();
                }
            }
        }

        private void LogOut()
        {
            SoundManager.instanse.PlaySound(SoundManager.instanse.buttonClickSound);

            MessageBox.Open(3, 0.8f, true, MessageResponded,
                new string[] { "Tem certeza que deseja sair? Se você não vinculou sua conta a um e-mail, todo o seu progresso será perdido." },
                new string[] { "Sair", "Cancelar" });
        }
    } 
}