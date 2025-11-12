namespace DevelopersHub.ClashOfWhatecer
{
    using System;
    using UnityEngine;

    public class Language : MonoBehaviour
    {

        private static LanguageID defaultLanguage = LanguageID.english;
        private static Language _instance = null; public static Language instanse { get { Initialize(); return _instance; } }

        [Serializable] public class Translation
        {
            public LanguageID language;
            public string text;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            _instance = this;
        }

        private static void Initialize()
        {
            if(_instance != null) { return; }
            _instance = FindFirstObjectByType<Language>();
            if(_instance == null)
            {
                _instance = new GameObject("Language").AddComponent<Language>();
            }
            LanguageID stored = defaultLanguage;
            if (PlayerPrefs.HasKey("language"))
            {
                try
                {
                    stored = (LanguageID)PlayerPrefs.GetInt("language");
                }
                catch (Exception)
                {
                    stored = defaultLanguage;
                    PlayerPrefs.SetInt("language", (int)stored);
                }
            }
            else
            {
                PlayerPrefs.SetInt("language", (int)stored);
            }
            _instance.language = stored;
        }

        private LanguageID _language = LanguageID.persian; public LanguageID language { get { return _language; } set { SetLanguage(value); } }

        public enum LanguageID
        {
            english = 0, persian = 1, russian = 2, arabic = 3, spanish = 4, french =  5, italian = 6, german = 7
        }

        public bool IsRTL
        {
            get
            {
                if (_language == LanguageID.persian || _language == LanguageID.arabic)
                {
                    return true;
                }
                return false;
            }
        }

        private void SetLanguage(LanguageID id)
        {
            _language = id;
        }
        
        public string GetBuildingName(Data.BuildingID id, int level = 1)
        {
            switch (language)
            {
                case LanguageID.english:
                    switch (id)
                    {
                        case Data.BuildingID.townhall: return "Prefeitura";
                        case Data.BuildingID.goldmine: return "Usina de Adubo";
                        case Data.BuildingID.goldstorage: return "Ponto de Reciclagem";
                        case Data.BuildingID.elixirmine: return "Coletor de Água";
                        case Data.BuildingID.elixirstorage: return "Energia Eólica";                      
                        case Data.BuildingID.armycamp: return "Horta";                       
                        case Data.BuildingID.decoration: return "Decoration";
                        case Data.BuildingID.spellfactory: return "Centro Comunitário";
                        case Data.BuildingID.laboratory: return "Eco-Centro";
                        case Data.BuildingID.obstacle:
                            switch (level)
                            {
                                case 1: case 2: case 3: return "Arvore";
                                case 4: case 5: return "Pedra";
                                default: return "Obstacle";
                            }                
                    }
                    break;               
            }
            return "";
        }                     
    }
}