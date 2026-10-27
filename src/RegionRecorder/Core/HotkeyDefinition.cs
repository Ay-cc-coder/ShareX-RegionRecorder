namespace RegionRecorder;

/// <summary>全局快捷键：修饰键 + 主键，持久化为可读文本（如 "Ctrl + Shift + R"）。</summary>
internal readonly record struct HotkeyDefinition(uint Modifiers, uint Key)
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;

    /// <summary>必须有 Ctrl / Alt / Win 之一，避免把普通按键变成全局热键。</summary>
    private const uint RequiredModifiers = ModControl | ModAlt | ModWin;

    public const string DefaultText = "Ctrl + Shift + R";

    public static HotkeyDefinition Default => new(ModControl | ModShift, (uint)Keys.R);

    public bool IsValid => Key != 0 && (Modifiers & RequiredModifiers) != 0;

    public string Display => Format(this);

    public static string Format(HotkeyDefinition hotkey)
    {
        List<string> parts = new(4);
        if ((hotkey.Modifiers & ModControl) != 0) parts.Add("Ctrl");
        if ((hotkey.Modifiers & ModAlt) != 0) parts.Add("Alt");
        if ((hotkey.Modifiers & ModShift) != 0) parts.Add("Shift");
        if ((hotkey.Modifiers & ModWin) != 0) parts.Add("Win");
        parts.Add(KeyName((Keys)hotkey.Key));
        return string.Join(" + ", parts);
    }

    public static HotkeyDefinition FromKeys(Keys keyData)
    {
        Keys modifiers = keyData & Keys.Modifiers;
        Keys key = keyData & Keys.KeyCode;

        uint mods = 0;
        if ((modifiers & Keys.Control) != 0) mods |= ModControl;
        if ((modifiers & Keys.Alt) != 0) mods |= ModAlt;
        if ((modifiers & Keys.Shift) != 0) mods |= ModShift;

        return new HotkeyDefinition(mods, (uint)key);
    }

    public static bool TryParse(string? text, out HotkeyDefinition hotkey)
    {
        hotkey = Default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        uint modifiers = 0;
        Keys key = Keys.None;

        foreach (string part in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= ModControl; break;
                case "alt": modifiers |= ModAlt; break;
                case "shift": modifiers |= ModShift; break;
                case "win" or "windows": modifiers |= ModWin; break;
                default:
                    if (!TryParseKey(part, out key)) return false;
                    break;
            }
        }

        hotkey = new HotkeyDefinition(modifiers, (uint)key);
        return hotkey.IsValid;
    }

    private static bool TryParseKey(string text, out Keys key)
    {
        if (text.Length == 1)
        {
            char c = text[0];
            if (c is >= '0' and <= '9')
            {
                key = Keys.D0 + (c - '0');
                return true;
            }

            if (char.IsLetter(c))
            {
                key = (Keys)char.ToUpperInvariant(c);
                return true;
            }
        }

        return Enum.TryParse(text, true, out key) && key != Keys.None;
    }

    private static string KeyName(Keys key)
    {
        if (key is >= Keys.D0 and <= Keys.D9) return ((int)key - (int)Keys.D0).ToString();
        return key.ToString();
    }
}
