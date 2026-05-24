using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using GameSettingsManager = ZAUM.C4.GameSettings.Management.GameSettingsManager;
using LanguageSetting = ZAUM.C4.GameSettings.Visuals.Settings.LanguageSetting;
using LanguageIsoCode = ZAUM.FELD.Data.Localization.LanguageIsoCode;

namespace ZpLangToggle;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BasePlugin
{
    public const string PluginGuid = "zp.langtoggle";
    public const string PluginName = "Zero Parades Language Toggle";
    public const string PluginVersion = "1.0.0";

    internal static new ManualLogSource Log;
    internal static ConfigEntry<KeyCode> ToggleKey;
    internal static ConfigEntry<string> LanguageA;
    internal static ConfigEntry<string> LanguageB;
    internal static ConfigEntry<bool> ShowNotification;

    public override void Load()
    {
        Log = base.Log;

        ToggleKey = Config.Bind(
            "General", "ToggleKey", KeyCode.Semicolon,
            "Hotkey to toggle language. Use any Unity KeyCode name (Semicolon, BackQuote, F8, L, ...). Default matches Disco Elysium.");
        LanguageA = Config.Bind(
            "General", "LanguageA", "en",
            "First language to toggle to. Game ships with: en, zh_cn, de, ru, es_mx. Also accepts other ISO codes (zh_tw, en_us, fr, ja, ...) or a raw LanguageIsoCode integer (e.g. 33).");
        LanguageB = Config.Bind(
            "General", "LanguageB", "zh_cn",
            "Second language to toggle to. Same format as LanguageA.");
        ShowNotification = Config.Bind(
            "General", "ShowNotification", true,
            "Write a line to the BepInEx log every time the language switches.");

        ClassInjector.RegisterTypeInIl2Cpp<LangToggleBehaviour>();
        AddComponent<LangToggleBehaviour>();

        Log.LogInfo($"{PluginName} v{PluginVersion} loaded. Toggle key = {ToggleKey.Value}, A={LanguageA.Value}, B={LanguageB.Value}");
    }
}

public static class LangCodes
{
    // 5 languages shipped with Zero Parades + common regional variants.
    // Full ISO list lives in the LanguageIsoCode enum (~100 entries); this
    // map is a practical subset, and raw integers are also accepted via TryParse.
    private static readonly Dictionary<string, int> NameToInt = new(StringComparer.OrdinalIgnoreCase)
    {
        // Shipped with ZP 1.0
        { "en",     33 }, { "zh_cn",  25 }, { "de",     57 }, { "ru",     87 }, { "es_mx", 103 },
        // English variants
        { "en_us",  43 }, { "en_gb",  42 }, { "en_au",  34 }, { "en_ca",  36 },
        // Chinese variants
        { "zh",     23 }, { "zh_tw",  27 }, { "zh_hk",  24 }, { "zh_sg",  26 },
        // Other common locales (ZP doesn't ship translations for these but the codes are valid)
        { "fr",     51 }, { "it",     66 }, { "ja",     68 }, { "ko",     69 },
        { "pl",     80 }, { "pt",     82 }, { "pt_br",  81 }, { "nl",     32 },
    };

    public static bool TryParse(string input, out LanguageIsoCode result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var s = input.Trim();
        if (NameToInt.TryGetValue(s, out int value)) { result = (LanguageIsoCode)value; return true; }
        if (int.TryParse(s, out int rawInt)) { result = (LanguageIsoCode)rawInt; return true; }
        return false;
    }
}

public class LangToggleBehaviour : MonoBehaviour
{
    public LangToggleBehaviour(IntPtr ptr) : base(ptr) { }

    private static GameSettingsManager _cachedMgr;

    private void Update()
    {
        if (!Input.GetKeyDown(Plugin.ToggleKey.Value)) return;
        if (IsTypingInInputField()) return;
        TryToggleLanguage();
    }

    // Pressing the hotkey while an input field is focused would both insert
    // the character into the field and fire the toggle. Skip in that case.
    private static bool IsTypingInInputField()
    {
        var es = EventSystem.current;
        var sel = es?.currentSelectedGameObject;
        if (sel == null) return false;
        return sel.GetComponent<TMP_InputField>() != null
            || sel.GetComponent<InputField>() != null;
    }

    private static GameSettingsManager GetManager()
    {
        if (_cachedMgr != null) return _cachedMgr;
        _cachedMgr = UnityEngine.Object.FindObjectOfType<GameSettingsManager>();
        return _cachedMgr;
    }

    private static void TryToggleLanguage()
    {
        if (!LangCodes.TryParse(Plugin.LanguageA.Value, out var langA))
        {
            Plugin.Log.LogError($"Invalid LanguageA in config: '{Plugin.LanguageA.Value}'. See README for accepted values (e.g. en, zh_cn, de, ru, es_mx).");
            return;
        }
        if (!LangCodes.TryParse(Plugin.LanguageB.Value, out var langB))
        {
            Plugin.Log.LogError($"Invalid LanguageB in config: '{Plugin.LanguageB.Value}'. See README for accepted values.");
            return;
        }
        if ((int)langA == (int)langB)
        {
            Plugin.Log.LogWarning($"LanguageA and LanguageB resolve to the same language ({langA}); nothing to toggle.");
            return;
        }

        var mgr = GetManager();
        if (mgr == null)
        {
            Plugin.Log.LogWarning("GameSettingsManager not found in current scene; skipping toggle.");
            return;
        }

        LanguageIsoCode current;
        try
        {
            current = mgr.GetSettingValue<LanguageSetting, LanguageIsoCode>();
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"GetSettingValue<LanguageSetting,LanguageIsoCode> failed: {e}");
            // Exception may be caused by the manager being destroyed; drop the
            // cached reference so the next attempt re-resolves it.
            _cachedMgr = null;
            return;
        }

        var target = (int)current == (int)langA ? langB : langA;

        try
        {
            mgr.SetSettingValue<LanguageSetting, LanguageIsoCode>(target, true);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"SetSettingValue<LanguageSetting,LanguageIsoCode>({target}) failed: {e}");
            _cachedMgr = null;
            return;
        }

        if (Plugin.ShowNotification.Value)
            Plugin.Log.LogInfo($"Language toggled: {current} ({(int)current}) -> {target} ({(int)target})");
    }
}
