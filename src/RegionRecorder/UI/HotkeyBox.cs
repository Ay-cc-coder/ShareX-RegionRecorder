using System.ComponentModel;

namespace RegionRecorder;

/// <summary>点击后直接按下组合键即可录入的输入框。</summary>
internal sealed class HotkeyBox : TextBox
{
    private HotkeyDefinition hotkey = HotkeyDefinition.Default;

    public HotkeyBox()
    {
        ReadOnly = true;
        ShortcutsEnabled = false;
        Cursor = Cursors.Hand;
        TextAlign = HorizontalAlignment.Center;
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Color.White;
        TabStop = true;
        Text = hotkey.Display;
        Width = 180;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HotkeyDefinition Hotkey
    {
        get => hotkey;
        set
        {
            hotkey = value;
            Text = value.Display;
        }
    }

    protected override bool IsInputKey(Keys keyData)
    {
        // Tab / Esc 交给对话框处理，其余按键都用于录入组合键。
        Keys key = keyData & Keys.KeyCode;
        if (key is Keys.Tab or Keys.Escape) return false;
        return true;
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        // Tab / Esc 仍然交还给对话框，其余按键都在这里录入，避免 Enter 直接关窗。
        Keys key = keyData & Keys.KeyCode;
        if (key is Keys.Tab or Keys.Escape) return base.ProcessDialogKey(keyData);
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        if (e.KeyCode is Keys.Tab or Keys.Escape) return;

        // 只按下修饰键时先给个提示，等用户继续按主键。
        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
        {
            Text = "请继续按下主键…";
            return;
        }

        HotkeyDefinition candidate = HotkeyDefinition.FromKeys(e.KeyData);
        if (!candidate.IsValid)
        {
            Text = "需要包含 Ctrl / Alt / Win";
            return;
        }

        Hotkey = candidate;
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        BackColor = Color.FromArgb(0xEF, 0xF6, 0xFF);
        SelectAll();
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        BackColor = Color.White;
        Text = hotkey.Display;
    }
}
