using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BorderlessWindowUtility
{
    /// <summary>
    /// UI translations. The language is chosen in settings.ini (language=auto|en|ko);
    /// auto follows the Windows display language.
    /// </summary>
    internal static class I18n
    {
        // Shown in their own language so they can be found whatever the current language is
        public static readonly Dictionary<string, string> Languages = new Dictionary<string, string>
        {
            ["en"] = "English",
            ["ko"] = "한국어",
        };
        private const int LangKorean = 0x12;

        private static readonly Dictionary<string, Dictionary<string, string>> Strings =
            new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                // Tray
                ["status_paused"] = "Paused",
                ["status_watching_one"] = "Watching 1 profile",
                ["status_watching"] = "Watching {count} profiles",
                ["last_applied"] = "Last: {text}",
                ["menu_window_list"] = "Window list / add profile...",
                ["menu_profiles"] = "Edit profiles.ini",
                ["menu_log"] = "Open log",
                ["menu_align"] = "Alignment",
                ["menu_align_none"] = "No game running",
                ["menu_language"] = "Language",
                ["menu_language_auto"] = "Automatic (system language)",
                ["menu_startup"] = "Start with Windows",
                ["menu_pause"] = "Pause auto-apply",
                ["menu_exit"] = "Exit",
                // Window list
                ["window_list_title"] = "Window List - Borderless Window Utility",
                ["list_intro"] = "Select the game window, then add it as a profile. The game must be running in windowed mode.",
                ["col_process"] = "Process",
                ["col_title"] = "Window title",
                ["col_size"] = "Size",
                ["col_position"] = "Position",
                ["col_profile"] = "Profile",
                ["refresh"] = "Refresh",
                ["copy_process"] = "Copy process name",
                ["add_profile"] = "Add profile",
                ["field_name"] = "Name",
                ["field_process"] = "Process",
                ["field_title"] = "Title contains",
                ["field_title_hint"] = "optional; both must match when set",
                ["field_width"] = "Width",
                ["field_height"] = "Height",
                ["field_align"] = "Alignment",
                ["field_align_hint"] = "on the primary monitor",
                ["align_left"] = "Left",
                ["align_center"] = "Center",
                ["align_right"] = "Right",
                ["add_to_profiles"] = "Add to profiles.ini",
                ["open_profiles"] = "Open profiles.ini",
                ["close"] = "Close",
                ["error_name"] = "Enter a profile name.",
                ["error_target"] = "Enter a process name or a window title.",
                ["error_duplicate"] = "A profile named \"{name}\" already exists.",
                ["error_size_number"] = "Width and height must be numbers.",
                ["error_size_positive"] = "Width and height must be positive.",
                ["added"] = "Added profile \"{name}\".",
                ["added_hint"] = "The tray watcher picks it up automatically and applies it within a few seconds if the game is already running.",
            },
            ["ko"] = new Dictionary<string, string>
            {
                // Tray
                ["status_paused"] = "일시 정지됨",
                ["status_watching_one"] = "프로필 1개 감시 중",
                ["status_watching"] = "프로필 {count}개 감시 중",
                ["last_applied"] = "최근 적용: {text}",
                ["menu_window_list"] = "창 목록 / 프로필 추가...",
                ["menu_profiles"] = "profiles.ini 편집",
                ["menu_log"] = "로그 열기",
                ["menu_align"] = "배치",
                ["menu_align_none"] = "실행 중인 게임 없음",
                ["menu_language"] = "언어 (Language)",
                ["menu_language_auto"] = "자동 (시스템 언어)",
                ["menu_startup"] = "Windows 시작 시 자동 실행",
                ["menu_pause"] = "자동 적용 일시 정지",
                ["menu_exit"] = "종료",
                // Window list
                ["window_list_title"] = "창 목록 - Borderless Window Utility",
                ["list_intro"] = "게임 창을 선택한 뒤 프로필로 추가하세요. 게임은 창 모드로 실행 중이어야 합니다.",
                ["col_process"] = "프로세스",
                ["col_title"] = "창 제목",
                ["col_size"] = "크기",
                ["col_position"] = "위치",
                ["col_profile"] = "프로필",
                ["refresh"] = "새로 고침",
                ["copy_process"] = "프로세스 이름 복사",
                ["add_profile"] = "프로필 추가",
                ["field_name"] = "이름",
                ["field_process"] = "프로세스",
                ["field_title"] = "창 제목 포함",
                ["field_title_hint"] = "선택 사항, 입력하면 프로세스와 둘 다 일치해야 함",
                ["field_width"] = "너비",
                ["field_height"] = "높이",
                ["field_align"] = "배치",
                ["field_align_hint"] = "주 모니터 기준",
                ["align_left"] = "왼쪽",
                ["align_center"] = "가운데",
                ["align_right"] = "오른쪽",
                ["add_to_profiles"] = "profiles.ini에 추가",
                ["open_profiles"] = "profiles.ini 열기",
                ["close"] = "닫기",
                ["error_name"] = "프로필 이름을 입력하세요.",
                ["error_target"] = "프로세스 이름이나 창 제목을 입력하세요.",
                ["error_duplicate"] = "\"{name}\" 프로필이 이미 있습니다.",
                ["error_size_number"] = "너비와 높이는 숫자여야 합니다.",
                ["error_size_positive"] = "너비와 높이는 0보다 커야 합니다.",
                ["added"] = "\"{name}\" 프로필을 추가했습니다.",
                ["added_hint"] = "트레이 감시 프로그램이 자동으로 반영하며, 게임이 이미 실행 중이면 몇 초 안에 적용됩니다.",
            },
        };

        private static string language;

        public static string ReadSetting()
        {
            var settings = IniFile.Load(Paths.SettingsFile).Get("settings");
            string value = settings == null ? "auto" : IniFile.GetString(settings, "language", "auto").Trim().ToLowerInvariant();
            return Languages.ContainsKey(value) ? value : "auto";
        }

        public static void WriteSetting(string value)
        {
            File.WriteAllText(Paths.SettingsFile, $"[settings]\nlanguage = {value}\n\n", new UTF8Encoding(false));
            language = null;
        }

        public static string SystemLanguage() =>
            (Native.GetUserDefaultUILanguage() & 0x3FF) == LangKorean ? "ko" : "en";

        public static string Current
        {
            get
            {
                if (language == null)
                {
                    string setting = ReadSetting();
                    language = setting == "auto" ? SystemLanguage() : setting;
                }
                return language;
            }
        }

        /// <summary>Translated text; args are placeholder/value pairs, e.g. Tr("added", "name", "My Game").</summary>
        public static string Tr(string key, params string[] args)
        {
            if (!Strings[Current].TryGetValue(key, out string text))
                text = Strings["en"][key];
            for (int i = 0; i + 1 < args.Length; i += 2)
                text = text.Replace("{" + args[i] + "}", args[i + 1]);
            return text;
        }

        public static IEnumerable<string> AllTranslations(string key)
        {
            foreach (var strings in Strings.Values)
                yield return strings[key];
        }
    }
}
