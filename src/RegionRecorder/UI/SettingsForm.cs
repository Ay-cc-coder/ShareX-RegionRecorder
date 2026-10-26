using ShareX.ScreenRecordingLib;

namespace RegionRecorder;

/// <summary>设置窗口：保存位置、帧率、鼠标指针、系统声音、麦克风、倒计时。</summary>
internal sealed class SettingsForm : Form
{
    private readonly AppSettings settings;

    private readonly TextBox folderBox = new();
    private readonly ComboBox fpsBox = new();
    private readonly CheckBox cursorBox = new() { Text = "把鼠标指针录进画面" };
    private readonly CheckBox systemAudioBox = new() { Text = "录制电脑播放的声音" };
    private readonly CheckBox microphoneBox = new() { Text = "启用" };
    private readonly ComboBox microphoneList = new();
    private readonly NumericUpDown countdownBox = new();

    private readonly List<AudioCaptureDevice> microphones = new();

    public SettingsForm(AppSettings settings)
    {
        this.settings = settings;

        Text = "设置 · ShareX 区域录屏";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        AutoScaleDimensions = new SizeF(96f, 96f);
        ClientSize = new Size(604, 496);
        BackColor = Color.White;
        Font = Theme.Font(9.5f);

        BuildLayout();
        LoadValues();
    }

    private void BuildLayout()
    {
        Label title = new()
        {
            Text = "录制设置",
            Font = Theme.Font(17f, FontStyle.Bold),
            ForeColor = Theme.PageText,
            AutoSize = true,
            Location = new Point(28, 22)
        };

        Label subtitle = new()
        {
            Text = "更改会应用到下一次录制",
            ForeColor = Theme.PageMuted,
            AutoSize = true,
            Location = new Point(29, 58)
        };

        folderBox.Dock = DockStyle.Fill;
        folderBox.BorderStyle = BorderStyle.FixedSingle;
        folderBox.Margin = new Padding(0);

        Button browse = new()
        {
            Text = "浏览…",
            Dock = DockStyle.Right,
            Width = 78,
            FlatStyle = FlatStyle.System
        };
        browse.Click += (_, _) => BrowseFolder();

        TableLayoutPanel folderRow = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84f));
        folderRow.Controls.Add(folderBox, 0, 0);
        folderRow.Controls.Add(browse, 1, 0);

        fpsBox.DropDownStyle = ComboBoxStyle.DropDownList;
        fpsBox.Width = 96;
        foreach (int value in new[] { 24, 30, 60, 120 })
        {
            fpsBox.Items.Add(value);
        }

        FlowLayoutPanel fpsRow = new() { Dock = DockStyle.Fill, Margin = new Padding(0) };
        fpsRow.Controls.Add(fpsBox);
        fpsRow.Controls.Add(new Label { Text = "帧/秒", AutoSize = true, Margin = new Padding(8, 6, 0, 0) });

        cursorBox.AutoSize = true;
        cursorBox.ForeColor = Theme.PageText;

        systemAudioBox.AutoSize = true;
        systemAudioBox.ForeColor = Theme.PageText;

        microphoneBox.AutoSize = true;
        microphoneBox.ForeColor = Theme.PageText;
        microphoneBox.CheckedChanged += (_, _) => UpdateMicrophoneState();

        microphoneList.DropDownStyle = ComboBoxStyle.DropDownList;
        microphoneList.Width = 240;

        Button refresh = new()
        {
            Text = "刷新",
            Width = 64,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(8, 0, 0, 0)
        };
        refresh.Click += (_, _) => LoadMicrophones();

        FlowLayoutPanel microphoneRow = new() { Dock = DockStyle.Fill, Margin = new Padding(0) };
        microphoneRow.Controls.Add(microphoneBox);
        microphoneRow.Controls.Add(microphoneList);
        microphoneRow.Controls.Add(refresh);

        countdownBox.Minimum = 0;
        countdownBox.Maximum = 10;
        countdownBox.Width = 70;

        FlowLayoutPanel countdownRow = new() { Dock = DockStyle.Fill, Margin = new Padding(0) };
        countdownRow.Controls.Add(countdownBox);
        countdownRow.Controls.Add(new Label { Text = "秒倒计时（0 = 立即开始）", AutoSize = true, Margin = new Padding(8, 6, 0, 0) });

        TableLayoutPanel body = new()
        {
            Location = new Point(28, 96),
            Size = new Size(548, 286),
            ColumnCount = 2,
            RowCount = 6,
            BackColor = Color.White
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96f));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < 6; i++)
        {
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
        }

        AddRow(body, 0, "保存位置", folderRow);
        AddRow(body, 1, "帧率", fpsRow);
        AddRow(body, 2, "画面", cursorBox);
        AddRow(body, 3, "系统声音", systemAudioBox);
        AddRow(body, 4, "麦克风", microphoneRow);
        AddRow(body, 5, "开始前", countdownRow);

        Label shortcut = new()
        {
            Text = "快捷键 Ctrl + Shift + R：开始框选；录制中再按一次即停止。",
            ForeColor = Theme.PageMuted,
            AutoSize = true,
            Location = new Point(28, 396)
        };

        Button save = new()
        {
            Text = "保存",
            Size = new Size(104, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Accent,
            ForeColor = Color.White,
            Font = Theme.Font(9.5f, FontStyle.Bold),
            DialogResult = DialogResult.None
        };
        save.Click += (_, _) => Save();

        Button cancel = new()
        {
            Text = "取消",
            Size = new Size(88, 36),
            FlatStyle = FlatStyle.System,
            DialogResult = DialogResult.Cancel
        };

        save.Location = new Point(ClientSize.Width - 28 - save.Width - 8 - cancel.Width, ClientSize.Height - 28 - save.Height);
        cancel.Location = new Point(ClientSize.Width - 28 - cancel.Width, ClientSize.Height - 28 - save.Height);

        Controls.AddRange(new Control[] { title, subtitle, body, shortcut, save, cancel });
        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void AddRow(TableLayoutPanel table, int row, string label, Control control)
    {
        Label caption = new()
        {
            Text = label,
            ForeColor = Theme.PageMuted,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 0, 0)
        };

        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        table.Controls.Add(caption, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private void LoadValues()
    {
        folderBox.Text = settings.OutputFolder;

        int fpsIndex = fpsBox.Items.IndexOf(settings.FramesPerSecond);
        if (fpsIndex < 0)
        {
            fpsBox.Items.Add(settings.FramesPerSecond);
            fpsIndex = fpsBox.Items.Count - 1;
        }
        fpsBox.SelectedIndex = fpsIndex;

        cursorBox.Checked = settings.IncludeCursor;
        systemAudioBox.Checked = settings.CaptureSystemAudio;
        microphoneBox.Checked = settings.CaptureMicrophone;
        countdownBox.Value = Math.Clamp(settings.CountdownSeconds, 0, 10);

        LoadMicrophones();
        UpdateMicrophoneState();
    }

    private void LoadMicrophones()
    {
        microphones.Clear();
        microphoneList.Items.Clear();
        microphoneList.Items.Add("系统默认麦克风");

        try
        {
            foreach (AudioCaptureDevice device in AudioCaptureDevices.GetMicrophones())
            {
                microphones.Add(device);
                microphoneList.Items.Add(device.Name);
            }
        }
        catch (Exception)
        {
            // 没有麦克风或系统接口不可用时，只保留「系统默认」。
        }

        int index = 0;
        if (settings.MicrophoneDeviceId is { Length: > 0 } id)
        {
            int found = microphones.FindIndex(device => device.Id == id);
            if (found >= 0) index = found + 1;
        }

        microphoneList.SelectedIndex = Math.Min(index, microphoneList.Items.Count - 1);
    }

    private void UpdateMicrophoneState()
    {
        microphoneList.Enabled = microphoneBox.Checked && microphoneList.Items.Count > 0;
    }

    private void BrowseFolder()
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "选择录制文件的保存位置",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(folderBox.Text) ? folderBox.Text : settings.OutputFolder
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            folderBox.Text = dialog.SelectedPath;
        }
    }

    private void Save()
    {
        string folder = folderBox.Text.Trim();
        if (folder.Length == 0)
        {
            ShowWarning("请选择保存位置。");
            return;
        }

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            ShowWarning($"无法创建保存目录：\n{ex.Message}");
            return;
        }

        settings.OutputFolder = folder;
        settings.FramesPerSecond = fpsBox.SelectedItem is int fps ? fps : 30;
        settings.IncludeCursor = cursorBox.Checked;
        settings.CaptureSystemAudio = systemAudioBox.Checked;
        settings.CaptureMicrophone = microphoneBox.Checked;

        int micIndex = microphoneList.SelectedIndex;
        settings.MicrophoneDeviceId = micIndex > 0 && micIndex - 1 < microphones.Count
            ? microphones[micIndex - 1].Id
            : null;

        settings.CountdownSeconds = (int)countdownBox.Value;
        settings.Normalize();

        try
        {
            SettingsStore.Save(settings);
        }
        catch (Exception ex)
        {
            ShowWarning($"保存设置失败：\n{ex.Message}");
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowWarning(string message) => MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
