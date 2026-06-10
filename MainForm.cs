using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GitPilot;

public class GithubAccount
{
    public string Alias { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public override string ToString() => string.IsNullOrWhiteSpace(Alias) ? UserName : Alias;
}

public class MainForm : Form
{
    private readonly string appDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GitPilot");

    private readonly string accountsFile;
    private readonly List<GithubAccount> accounts = new();

    private string selectedFolder = "";

    private Label lblFolder = new();
    private ComboBox cmbAccounts = new();
    private TextBox txtCommit = new();
    private TextBox txtRepoName = new();
    private TextBox txtRepoDescription = new();
    private CheckBox chkPrivate = new();
    private RichTextBox txtLog = new();
    private ListBox lstStatus = new();

    public MainForm()
    {
        accountsFile = Path.Combine(appDir, "accounts.json");
        Directory.CreateDirectory(appDir);

        BuildUi();
        LoadAccounts();
        RefreshAccountsUi();
    }

    private void BuildUi()
    {
        Text = "GitPilot - Controle de Commits";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(980, 650);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(12, 12, 18);
        Font = new Font("Segoe UI", 10);

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(18),
            BackColor = Color.FromArgb(12, 12, 18)
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 410));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(main);

        var left = PanelBox();
        var right = PanelBox();
        main.Controls.Add(left, 0, 0);
        main.Controls.Add(right, 1, 0);

        var title = new Label
        {
            Text = "GitPilot",
            ForeColor = Color.FromArgb(255, 50, 95),
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 58
        };
        left.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Automatize commits, push e criação de repositórios.",
            ForeColor = Color.FromArgb(190, 170, 255),
            Dock = DockStyle.Top,
            Height = 38
        };
        left.Controls.Add(subtitle);
        subtitle.BringToFront();

        int y = 115;

        var btnFolder = ButtonPrimary("Selecionar pasta do projeto");
        btnFolder.Location = new Point(22, y);
        btnFolder.Click += (_, _) => SelectFolder();
        left.Controls.Add(btnFolder);

        y += 55;
        lblFolder = LabelValue("Nenhuma pasta selecionada");
        lblFolder.Location = new Point(22, y);
        lblFolder.Size = new Size(350, 45);
        left.Controls.Add(lblFolder);

        y += 65;
        left.Controls.Add(LabelTitle("Conta GitHub", 22, y));
        y += 30;
        cmbAccounts.Location = new Point(22, y);
        cmbAccounts.Size = new Size(350, 32);
        cmbAccounts.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbAccounts.BackColor = Color.FromArgb(30, 28, 42);
        cmbAccounts.ForeColor = Color.White;
        left.Controls.Add(cmbAccounts);

        y += 48;
        var btnAdd = ButtonSecondary("Adicionar conta");
        btnAdd.Location = new Point(22, y);
        btnAdd.Click += (_, _) => AddAccount();
        left.Controls.Add(btnAdd);

        var btnRemove = ButtonSecondary("Remover conta");
        btnRemove.Location = new Point(205, y);
        btnRemove.Click += (_, _) => RemoveAccount();
        left.Controls.Add(btnRemove);

        y += 62;
        left.Controls.Add(LabelTitle("Mensagem do commit", 22, y));
        y += 30;
        txtCommit.Location = new Point(22, y);
        txtCommit.Size = new Size(350, 35);
        txtCommit.BackColor = Color.FromArgb(30, 28, 42);
        txtCommit.ForeColor = Color.White;
        txtCommit.BorderStyle = BorderStyle.FixedSingle;
        txtCommit.PlaceholderText = "Ex: Implementa tela inicial";
        left.Controls.Add(txtCommit);

        y += 55;
        var btnCommit = ButtonPrimary("Commitar alterações");
        btnCommit.Location = new Point(22, y);
        btnCommit.Click += async (_, _) => await CommitAsync();
        left.Controls.Add(btnCommit);

        y += 50;
        var btnPush = ButtonPrimary("Enviar para GitHub");
        btnPush.Location = new Point(22, y);
        btnPush.Click += async (_, _) => await PushAsync();
        left.Controls.Add(btnPush);

        y += 62;
        var btnInit = ButtonSecondary("Inicializar Git");
        btnInit.Location = new Point(22, y);
        btnInit.Click += async (_, _) => await InitGitAsync();
        left.Controls.Add(btnInit);

        var btnStatus = ButtonSecondary("Atualizar status");
        btnStatus.Location = new Point(205, y);
        btnStatus.Click += async (_, _) => await StatusAsync();
        left.Controls.Add(btnStatus);

        var repoBox = PanelBox();
        repoBox.Dock = DockStyle.Top;
        repoBox.Height = 255;
        right.Controls.Add(repoBox);

        var repoTitle = new Label
        {
            Text = "Criar ou conectar repositório",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            Location = new Point(20, 18),
            Size = new Size(500, 32)
        };
        repoBox.Controls.Add(repoTitle);

        repoBox.Controls.Add(LabelTitle("Nome do repositório", 20, 65));
        txtRepoName.Location = new Point(20, 95);
        txtRepoName.Size = new Size(300, 32);
        txtRepoName.BackColor = Color.FromArgb(30, 28, 42);
        txtRepoName.ForeColor = Color.White;
        txtRepoName.PlaceholderText = "Ex: meu-projeto";
        repoBox.Controls.Add(txtRepoName);

        repoBox.Controls.Add(LabelTitle("Descrição", 340, 65));
        txtRepoDescription.Location = new Point(340, 95);
        txtRepoDescription.Size = new Size(350, 32);
        txtRepoDescription.BackColor = Color.FromArgb(30, 28, 42);
        txtRepoDescription.ForeColor = Color.White;
        txtRepoDescription.PlaceholderText = "Descrição opcional";
        repoBox.Controls.Add(txtRepoDescription);

        chkPrivate.Text = "Repositório privado";
        chkPrivate.ForeColor = Color.FromArgb(210, 210, 225);
        chkPrivate.Location = new Point(20, 140);
        chkPrivate.Size = new Size(250, 30);
        chkPrivate.BackColor = Color.Transparent;
        repoBox.Controls.Add(chkPrivate);

        var btnCreateRepo = ButtonPrimary("Criar repo no GitHub e conectar");
        btnCreateRepo.Location = new Point(20, 185);
        btnCreateRepo.Size = new Size(300, 40);
        btnCreateRepo.Click += async (_, _) => await CreateRepoAsync();
        repoBox.Controls.Add(btnCreateRepo);

        var btnConnect = ButtonSecondary("Conectar repo existente");
        btnConnect.Location = new Point(340, 185);
        btnConnect.Size = new Size(240, 40);
        btnConnect.Click += async (_, _) => await ConnectExistingRepoAsync();
        repoBox.Controls.Add(btnConnect);

        var statusTitle = new Label
        {
            Text = "Arquivos alterados",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            Location = new Point(20, 275),
            Size = new Size(400, 30)
        };
        right.Controls.Add(statusTitle);

        lstStatus.Location = new Point(20, 312);
        lstStatus.Size = new Size(690, 155);
        lstStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lstStatus.BackColor = Color.FromArgb(19, 18, 29);
        lstStatus.ForeColor = Color.FromArgb(230, 230, 240);
        lstStatus.BorderStyle = BorderStyle.FixedSingle;
        right.Controls.Add(lstStatus);

        var logTitle = new Label
        {
            Text = "Logs",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            Location = new Point(20, 485),
            Size = new Size(400, 30)
        };
        right.Controls.Add(logTitle);

        txtLog.Location = new Point(20, 522);
        txtLog.Size = new Size(690, 160);
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.BackColor = Color.FromArgb(5, 5, 10);
        txtLog.ForeColor = Color.FromArgb(210, 255, 220);
        txtLog.BorderStyle = BorderStyle.FixedSingle;
        txtLog.ReadOnly = true;
        right.Controls.Add(txtLog);
    }

    private Panel PanelBox()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 18, 30),
            Padding = new Padding(12),
            Margin = new Padding(8)
        };
    }

    private Label LabelTitle(string text, int x, int y) => new()
    {
        Text = text,
        ForeColor = Color.FromArgb(210, 200, 255),
        Location = new Point(x, y),
        Size = new Size(350, 25),
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
    };

    private Label LabelValue(string text) => new()
    {
        Text = text,
        ForeColor = Color.FromArgb(220, 220, 230),
        BackColor = Color.FromArgb(30, 28, 42),
        Padding = new Padding(8),
        AutoEllipsis = true
    };

    private Button ButtonPrimary(string text) => new()
    {
        Text = text,
        Size = new Size(350, 40),
        BackColor = Color.FromArgb(155, 35, 255),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
    };

    private Button ButtonSecondary(string text) => new()
    {
        Text = text,
        Size = new Size(167, 38),
        BackColor = Color.FromArgb(95, 25, 130),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
    };

    private void SelectFolder()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            selectedFolder = dialog.SelectedPath;
            lblFolder.Text = selectedFolder;
            _ = StatusAsync();
        }
    }

    private void LoadAccounts()
    {
        accounts.Clear();
        if (File.Exists(accountsFile))
        {
            var json = File.ReadAllText(accountsFile);
            var loaded = JsonSerializer.Deserialize<List<GithubAccount>>(json);
            if (loaded != null) accounts.AddRange(loaded);
        }
    }

    private void SaveAccounts()
    {
        var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(accountsFile, json);
    }

    private void RefreshAccountsUi()
    {
        cmbAccounts.Items.Clear();
        foreach (var acc in accounts) cmbAccounts.Items.Add(acc);
        if (cmbAccounts.Items.Count > 0) cmbAccounts.SelectedIndex = 0;
    }

    private void AddAccount()
    {
        using var form = new AccountForm();
        if (form.ShowDialog() == DialogResult.OK)
        {
            accounts.Add(form.Account);
            SaveAccounts();
            RefreshAccountsUi();
            Log("Conta adicionada.");
        }
    }

    private void RemoveAccount()
    {
        if (cmbAccounts.SelectedItem is not GithubAccount acc) return;
        accounts.Remove(acc);
        SaveAccounts();
        RefreshAccountsUi();
        Log("Conta removida.");
    }

    private GithubAccount? CurrentAccount()
    {
        if (cmbAccounts.SelectedItem is GithubAccount acc) return acc;
        MessageBox.Show("Adicione ou selecione uma conta GitHub.");
        return null;
    }

    private bool HasFolder()
    {
        if (!string.IsNullOrWhiteSpace(selectedFolder) && Directory.Exists(selectedFolder)) return true;
        MessageBox.Show("Selecione uma pasta de projeto primeiro.");
        return false;
    }

    private async Task InitGitAsync()
    {
        if (!HasFolder()) return;
        await RunGit("init");
        await StatusAsync();
    }

    private async Task StatusAsync()
    {
        if (!HasFolder()) return;
        var result = await RunGit("status --short", false);
        lstStatus.Items.Clear();
        if (string.IsNullOrWhiteSpace(result))
            lstStatus.Items.Add("Nenhuma alteração encontrada.");
        else
            foreach (var line in result.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                lstStatus.Items.Add(line.TrimEnd());
    }

    private async Task CommitAsync()
    {
        if (!HasFolder()) return;
        var acc = CurrentAccount();
        if (acc == null) return;

        var message = txtCommit.Text.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            MessageBox.Show("Digite a mensagem do commit.");
            return;
        }

        await RunGit($"config user.name \"{acc.UserName}\"");
        await RunGit($"config user.email \"{acc.Email}\"");
        await RunGit("add .");
        await RunGit($"commit -m \"{message.Replace("\"", "\\\"")}\"");
        await StatusAsync();
    }

    private async Task PushAsync()
    {
        if (!HasFolder()) return;
        await RunGit("branch -M main");
        await RunGit("push -u origin main");
        await StatusAsync();
    }

    private async Task CreateRepoAsync()
    {
        if (!HasFolder()) return;
        var acc = CurrentAccount();
        if (acc == null) return;

        if (string.IsNullOrWhiteSpace(acc.Token))
        {
            MessageBox.Show("Para criar repositório pelo app, adicione um token do GitHub na conta.");
            return;
        }

        var repo = txtRepoName.Text.Trim();
        if (string.IsNullOrWhiteSpace(repo))
        {
            MessageBox.Show("Digite o nome do repositório.");
            return;
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GitPilot");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acc.Token);

            var body = new
            {
                name = repo,
                description = txtRepoDescription.Text.Trim(),
                @private = chkPrivate.Checked,
                auto_init = false
            };

            var json = JsonSerializer.Serialize(body);
            var response = await client.PostAsync(
                "https://api.github.com/user/repos",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Log("Erro ao criar repo: " + content);
                MessageBox.Show("Erro ao criar repositório. Veja os logs.");
                return;
            }

            var remoteUrl = $"https://github.com/{acc.UserName}/{repo}.git";

            await RunGit("init");
            await RunGit("branch -M main");
            await RemoveOriginIfExists();
            await RunGit($"remote add origin {remoteUrl}");

            Log("Repositório criado e conectado: " + remoteUrl);
            MessageBox.Show("Repositório criado e conectado com sucesso!");
        }
        catch (Exception ex)
        {
            Log("Erro: " + ex.Message);
        }
    }

    private async Task ConnectExistingRepoAsync()
    {
        if (!HasFolder()) return;
        var repoUrl = Microsoft.VisualBasic.Interaction.InputBox(
            "Cole a URL HTTPS do repositório:",
            "Conectar repositório existente",
            "https://github.com/usuario/repositorio.git");

        if (string.IsNullOrWhiteSpace(repoUrl)) return;

        await RunGit("init");
        await RemoveOriginIfExists();
        await RunGit($"remote add origin {repoUrl}");
        Log("Repositório conectado: " + repoUrl);
    }

    private async Task RemoveOriginIfExists()
    {
        await RunGit("remote remove origin", false);
    }

    private async Task<string> RunGit(string args, bool showLog = true)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = selectedFolder,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return "";

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var text = (output + error).Trim();
            if (showLog || !string.IsNullOrWhiteSpace(text))
                Log("> git " + args + Environment.NewLine + text);

            return text;
        }
        catch (Exception ex)
        {
            Log("Erro executando Git: " + ex.Message);
            return "";
        }
    }

    private void Log(string text)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}{Environment.NewLine}");
        txtLog.ScrollToCaret();
    }
}

public class AccountForm : Form
{
    public GithubAccount Account { get; private set; } = new();

    private TextBox txtAlias = new();
    private TextBox txtUser = new();
    private TextBox txtEmail = new();
    private TextBox txtToken = new();

    public AccountForm()
    {
        Text = "Adicionar Conta GitHub";
        Width = 480;
        Height = 380;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(12, 12, 18);
        Font = new Font("Segoe UI", 10);

        AddLabel("Apelido da conta", 25, 25);
        txtAlias = AddTextBox(25, 55, "Ex: Pessoal");

        AddLabel("Usuário do GitHub", 25, 95);
        txtUser = AddTextBox(25, 125, "Ex: etecmidia");

        AddLabel("Email do commit", 25, 165);
        txtEmail = AddTextBox(25, 195, "Ex: seuemail@gmail.com");

        AddLabel("Token GitHub opcional", 25, 235);
        txtToken = AddTextBox(25, 265, "Necessário para criar repo pelo app");
        txtToken.PasswordChar = '●';

        var btn = new Button
        {
            Text = "Salvar conta",
            Location = new Point(25, 305),
            Size = new Size(390, 38),
            BackColor = Color.FromArgb(155, 35, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btn.Click += (_, _) => Save();
        Controls.Add(btn);
    }

    private void AddLabel(string text, int x, int y)
    {
        Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(390, 24),
            ForeColor = Color.FromArgb(210, 200, 255),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        });
    }

    private TextBox AddTextBox(int x, int y, string placeholder)
    {
        var box = new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(390, 32),
            BackColor = Color.FromArgb(30, 28, 42),
            ForeColor = Color.White,
            PlaceholderText = placeholder,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(box);
        return box;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
        {
            MessageBox.Show("Usuário e email são obrigatórios.");
            return;
        }

        Account = new GithubAccount
        {
            Alias = txtAlias.Text.Trim(),
            UserName = txtUser.Text.Trim(),
            Email = txtEmail.Text.Trim(),
            Token = txtToken.Text.Trim()
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
