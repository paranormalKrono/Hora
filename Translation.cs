using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Ink;

#if !UNITY_WEBGL
using System.IO;
#endif

public class Translation
{
    private const string UITranslations = "uiTranslation";
    private const string InkTranslations = "inkTranslations/";
    private const string NamesTranslations = "names";

    private const string filesFolder = "/Translations/";

    private const int _uiIDShift = 3;

    public List<string> _available { get; private set; }
    private string[] _ui;

    private string[] _uiIDs;
    private string[] _uiCurrent;

    private static int _currentTranslationID = 0;

    public Translation()
    {
        string[] lines = GetUITranslation();
        _ui = lines;

        string[] l0s = lines[0].Split(';');
        int count = lines.Length - 2; // End line is empty
        _available = new List<string>(l0s.Length - _uiIDShift);
        for (int i = _uiIDShift; i < l0s.Length; i++)
        {
            _available.Add(l0s[i]);
        }
        _uiIDs = new string[count];
        _uiCurrent = new string[count];
    }

    public void LoadUITranslation(string sid)
    {
        int id = _available.IndexOf(sid);
        if (id == -1 || _currentTranslationID == id) return;
        _currentTranslationID = id;

        string[] lines = _ui;
        for (int i = 1; i < lines.Length - 1; ++i)
        {
            string[] splitData = lines[i].Split(';');

            // 0 - ID, 1 - Source Text, 2 - Comment, 3,4,5,...
            _uiIDs[i - 1] = splitData[0];
            _uiCurrent[i - 1] = splitData[id + _uiIDShift];
        }
    }

    // IT'S HEAVY!!!
    public void TranslateUIToCurrent(VisualElement root)
    {
        foreach (var el in root.Children())
        {
            TranslateUIToCurrent(el);
        }
        for (int i = 0; i < _uiIDs.Length; ++i)
        {
            if (_uiIDs[i] == root.name)
            {
                if (root is Label)
                {
                    (root as Label).text = _uiCurrent[i];
                }
                else if (root is Button)
                {
                    (root as Button).text = _uiCurrent[i];
                }
                // else if (root is DropdownField)
                // {
                //     (root as DropdownField).= _uiTranslation[i];
                // }
                break;
            }
        }
    }

    private string[] GetUITranslation()
    {
#if UNITY_WEBGL
        string[] lines = (Resources.Load(UITranslations) as TextAsset).text.Split("\r\n");
#else
            string[] lines = File.ReadAllLines(Application.persistentDataPath + filesFolder + UITranslations);
#endif
        return lines;
    }

//    public string GetInk(GameInk.StoryFiles story)
//    {
//#if UNITY_WEBGL
//        string text = (Resources.Load(InkTranslations + _available[_currentTranslationID] + "/" + story.ToString()) as TextAsset).text;
//#else
//            string text = File.ReadAllText(Application.persistentDataPath + filesFolder + InkTranslations + _available[_currentTranslationID] + "/" + story.ToString());
//#endif
//        return text;
//    }

    public static string[] GetNames()
    {
#if UNITY_WEBGL
        string[] lines = (Resources.Load(NamesTranslations) as TextAsset).text.Split("\r\n");
#else
            string[] lines = File.ReadAllLines(Application.persistentDataPath + filesFolder + NamesTranslations);
#endif

        int curTranslationID = _currentTranslationID;
        int count = lines.Length - 1;
        string[] names = new string[count];
        for (int i = 1; i < count; ++i)
        {
            string[] splitData = lines[i].Split(';');

            // 0 - ID, 1,2,3,4,...
            names[i - 1] = splitData[curTranslationID + 1];
        }

        return names;
    }
}