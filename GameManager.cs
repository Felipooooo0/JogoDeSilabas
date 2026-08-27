using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum Difficulty { Facil, Medio, Dificil }

public struct WordData
{
    public string Word;
    public int Syllables;
    public Difficulty Level;

    public WordData(string word, int syllables, Difficulty level = Difficulty.Facil)
    {
        Word = word.ToUpper();
        Syllables = syllables;
        Level = level;
    }
}

public partial class GameManager : Control
{
    // --- Referências aos Nós da UI ---
    [Export] public Label WordLabel;
    [Export] public Container OptionsContainer;
    [Export] public Label TimerLabel;
    [Export] public Label LivesLabel;
    [Export] public Timer TurnTimer;
    [Export] public Label DescLabel;
    [Export] public Label FeedbackLabel;
    [Export] public Button RestartButton;
    [Export] public Button FullscreenButton;

    // --- Variáveis de Estado ---
    private List<WordData> _wordDatabase;
    private WordData _currentWordData;
    private int _lives = 3;
    private int _score = 0;
    private bool _canAnswer = false;
    private readonly Random _rand = new();

    public override void _Ready()
    {
        InitializeDatabase();
        ApplyVisualTheme();
        UpdateHud();

        if (RestartButton != null)
        {
            RestartButton.Hide();
            RestartButton.Pressed += () => GetTree().ReloadCurrentScene();
        }

        if (FullscreenButton != null)
        {
            FullscreenButton.Pressed += ToggleFullscreen;
        }

        TurnTimer.Timeout += OnTurnTimeout;
        StartNewTurn();
    }
    public override void _Process(double delta)
    {
        if (!TurnTimer.IsStopped() && TimerLabel != null)
        {
            TimerLabel.Text = $"Tempo: {Mathf.Ceil(TurnTimer.TimeLeft)}s";
        }
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.F11 || (key.Keycode == Key.Enter && key.AltPressed))
            {
                ToggleFullscreen();
            }
        }
    }
    public void ToggleFullscreen()
    {
        bool isFullscreen = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
        DisplayServer.WindowSetMode(isFullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
    }
    private Difficulty GetCurrentDifficulty()
    {
        if (_score >= 120) return Difficulty.Dificil;
        if (_score >= 50) return Difficulty.Medio;
        return Difficulty.Facil;
    }
    private void StartNewTurn()
    {
        if (FeedbackLabel != null) FeedbackLabel.Text = "";

        Difficulty currentDiff = GetCurrentDifficulty();
        TurnTimer.WaitTime = currentDiff switch
        {
            Difficulty.Dificil => 6.0,
            Difficulty.Medio => 8.0,
            _ => 10.0
        };

        var availableWords = _wordDatabase.Where(w => w.Level <= currentDiff).ToList();
        if (availableWords.Count == 0)
        {
            InitializeDatabase();
            availableWords = _wordDatabase.Where(w => w.Level <= currentDiff).ToList();
        }

        int randomIndex = _rand.Next(0, availableWords.Count);
        _currentWordData = availableWords[randomIndex];
        _wordDatabase.Remove(_currentWordData);

        WordLabel.Text = _currentWordData.Word;
        GenerateOptions();
        UpdateHud();

        _canAnswer = true;
        TurnTimer.Start();
    }
    private async void OnOptionSelected(string buttonText)
    {
        if (!_canAnswer) return;

        string numberOnly = buttonText.Split(' ')[0];
        if (int.TryParse(numberOnly, out int chosenSyllables))
        {
            if (chosenSyllables == _currentWordData.Syllables)
            {
                _canAnswer = false;
                TurnTimer.Stop();
                _score += 5;
                ShowFeedback("Acertou!", new Color("22C55E"));
                
                await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
                UpdateHud();
                StartNewTurn();
            }
            else
            {
                await HandleWrongAnswer($"Errou! Era {_currentWordData.Syllables} sílaba(s)");
            }
        }
    }
    private async void OnTurnTimeout()
    {
        if (!_canAnswer) return;
        await HandleWrongAnswer($"Tempo esgotado! Era {_currentWordData.Syllables} sílaba(s)");
    }
    private async System.Threading.Tasks.Task HandleWrongAnswer(string message)
    {
        _canAnswer = false;
        TurnTimer.Stop();
        ShowFeedback(message, new Color("EF4444"));
        
        await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
        TakeDamage();
    }
    private void ShowFeedback(string text, Color color)
    {
        if (FeedbackLabel == null) return;
        FeedbackLabel.Text = text;
        FeedbackLabel.AddThemeColorOverride("font_color", color);
    }
    private void TakeDamage()
    {
        _lives--;
        UpdateHud();

        if (_lives <= 0) GameOver();
        else StartNewTurn();
    }
    private void UpdateHud()
    {
        string hearts = string.Concat(Enumerable.Repeat("❤️", Math.Max(0, _lives)));
        if (LivesLabel != null) LivesLabel.Text = $"VIDAS {hearts}";

        Difficulty diff = GetCurrentDifficulty();
        string diffText = diff switch
        {
            Difficulty.Dificil => "NÍVEL: DIFÍCIL",
            Difficulty.Medio => "NÍVEL: MÉDIO",
            _ => "NÍVEL: FÁCIL"
        };

        if (DescLabel != null) DescLabel.Text = $"{diffText} | PONTOS: {_score}";
    }
    private void GameOver()
    {
        WordLabel.Text = $"FIM DE JOGO!\r\nPontos: {_score}";
        OptionsContainer.Hide();
        TimerLabel?.Hide();
        LivesLabel?.Hide();
        DescLabel?.Hide();
        FeedbackLabel?.Hide();
        RestartButton?.Show();
    }
    private void GenerateOptions()
    {
        var buttons = OptionsContainer.GetChildren().OfType<Button>().ToList();
        int correctAnswer = _currentWordData.Syllables;
        HashSet<int> options = new() { correctAnswer };

        while (options.Count < buttons.Count)
        {
            int offset = _rand.Next(-2, 3);
            int fakeOption = correctAnswer + offset;

            if (fakeOption >= 1 && fakeOption <= 10) options.Add(fakeOption);
            else options.Add(_rand.Next(1, 8));
        }

        List<int> shuffledOptions = options.OrderBy(_ => _rand.Next()).ToList();
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].Text = $"{shuffledOptions[i]}";
        }
    }
    private void ApplyVisualTheme()
    {
        RenderingServer.SetDefaultClearColor(Colors.White);

        if (GetNodeOrNull("CanvasLayer/HUD") is Panel hudPanel)
        {
            hudPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Colors.White });
        }

        if (WordLabel != null)
        {
            StyleBoxFlat wordCardStyle = new StyleBoxFlat
            {
                BgColor = Colors.White,
                BorderColor = new Color("E2E8F0"),
                ShadowSize = 12,
                ShadowOffset = new Vector2(0, 6),
                ShadowColor = new Color(0, 0, 0, 0.08f),
                ContentMarginLeft = 40, ContentMarginRight = 40,
                ContentMarginTop = 20, ContentMarginBottom = 20
            };
            wordCardStyle.SetCornerRadiusAll(24);
            wordCardStyle.SetBorderWidthAll(3);

            WordLabel.AddThemeStyleboxOverride("normal", wordCardStyle);
            WordLabel.AddThemeColorOverride("font_color", new Color("0F172A"));
            WordLabel.AddThemeFontSizeOverride("font_size", 56);
            WordLabel.HorizontalAlignment = HorizontalAlignment.Center;
            WordLabel.VerticalAlignment = VerticalAlignment.Center;
        }

        if (FeedbackLabel != null)
        {
            FeedbackLabel.AddThemeFontSizeOverride("font_size", 28);
            FeedbackLabel.HorizontalAlignment = HorizontalAlignment.Center;
        }

        if (LivesLabel != null) ApplyBadgeStyle(LivesLabel, new Color("FFE4E6"), new Color("E11D48"));
        if (TimerLabel != null) ApplyBadgeStyle(TimerLabel, new Color("E0F2FE"), new Color("0284C7"));

        if (DescLabel != null)
        {
            DescLabel.AddThemeColorOverride("font_color", new Color("64748B"));
            DescLabel.AddThemeFontSizeOverride("font_size", 20);
        }

        if (RestartButton != null)
        {
            ApplyButtonStyle(RestartButton, new Color("22C55E"), new Color("15803D"), new Vector2(280, 90), 32);
        }

        if (FullscreenButton != null)
        {
            StyleBoxFlat fsNormal = new StyleBoxFlat { BgColor = new Color("64748B"), BorderWidthBottom = 4, BorderColor = new Color("334155") };
            fsNormal.SetCornerRadiusAll(12);
            FullscreenButton.CustomMinimumSize = new Vector2(160, 50);
            FullscreenButton.AddThemeStyleboxOverride("normal", fsNormal);
            FullscreenButton.AddThemeFontSizeOverride("font_size", 18);
            FullscreenButton.AddThemeColorOverride("font_color", Colors.White);
        }

        Color[] baseColors = { new Color("FF9800"), new Color("AB47BC"), new Color("29B6F6"), new Color("66BB6A") };
        Color[] darkBorders = { new Color("E65100"), new Color("4A148C"), new Color("0277BD"), new Color("1B5E20") };

        int colorIndex = 0;
        foreach (Button button in OptionsContainer.GetChildren().OfType<Button>())
        {
            Color baseCol = baseColors[colorIndex % baseColors.Length];
            Color darkCol = darkBorders[colorIndex % darkBorders.Length];

            ApplyButtonStyle(button, baseCol, darkCol, new Vector2(280, 110), 44);
            
            // Garante que a ação de clique do botão é vinculada
            string capturedText = button.Text;
            button.Pressed += () => OnOptionSelected(button.Text);

            colorIndex++;
        }
    }
    private void ApplyBadgeStyle(Label label, Color bgColor, Color textColor)
    {
        StyleBoxFlat badge = new StyleBoxFlat
        {
            BgColor = bgColor,
            ContentMarginLeft = 20, ContentMarginRight = 20,
            ContentMarginTop = 8, ContentMarginBottom = 8
        };
        badge.SetCornerRadiusAll(30);
        label.AddThemeStyleboxOverride("normal", badge);
        label.AddThemeColorOverride("font_color", textColor);
        label.AddThemeFontSizeOverride("font_size", 22);
    }
    private void ApplyButtonStyle(Button button, Color baseCol, Color darkCol, Vector2 size, int fontSize)
    {
        StyleBoxFlat normalStyle = new StyleBoxFlat
        {
            BgColor = baseCol,
            BorderWidthBottom = 8,
            BorderColor = darkCol,
            ShadowSize = 4,
            ShadowOffset = new Vector2(0, 3),
            ShadowColor = new Color(0, 0, 0, 0.15f)
        };
        normalStyle.SetCornerRadiusAll(18);

        StyleBoxFlat hoverStyle = (StyleBoxFlat)normalStyle.Duplicate();
        hoverStyle.BgColor = baseCol.Lightened(0.12f);

        StyleBoxFlat pressedStyle = (StyleBoxFlat)normalStyle.Duplicate();
        pressedStyle.BorderWidthBottom = 2;
        pressedStyle.ShadowSize = 0;
        pressedStyle.ContentMarginTop = 6;

        button.CustomMinimumSize = size;
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);
        button.AddThemeStyleboxOverride("focus", normalStyle);
    }
    private void InitializeDatabase()
    {
        _wordDatabase = new List<WordData>
        
        {
            // FÁCIL
            new("Pão ", 1, Difficulty.Facil), new("Sol ☀", 1, Difficulty.Facil),
            new("Mãe ", 1, Difficulty.Facil), new("Pai ", 1, Difficulty.Facil),
            new("Flor ", 1, Difficulty.Facil), new("Trem ", 1, Difficulty.Facil),
            new("Luz ", 1, Difficulty.Facil), new("Cão ", 1, Difficulty.Facil),
            new("Casa ", 2, Difficulty.Facil), new("Gato ", 2, Difficulty.Facil),
            new("Mesa ", 2, Difficulty.Facil), new("Livro ", 2, Difficulty.Facil),
            new("Bola ", 2, Difficulty.Facil), new("Prato ", 2, Difficulty.Facil),
            new("Carro ", 2, Difficulty.Facil), new("Árvore ", 2, Difficulty.Facil),
            new("Porta ", 2, Difficulty.Facil), new("Peixe ", 2, Difficulty.Facil),
            new("Janela ", 3, Difficulty.Facil), new("Banana ", 3, Difficulty.Facil),
            new("Caderno ", 3, Difficulty.Facil), new("Escola ", 3, Difficulty.Facil),
            new("Manteiga ", 3, Difficulty.Facil), new("Caneta 🖊", 3, Difficulty.Facil),
            new("Relógio ", 3, Difficulty.Facil), new("Espelho ", 3, Difficulty.Facil),
            new("Girafa ", 3, Difficulty.Facil), new("Sapato ", 3, Difficulty.Facil),
            
            new("Cadeira ", 3, Difficulty.Facil),
            // MÉDIO
            new("Computador ", 4, Difficulty.Medio), new("Chocolate ", 4, Difficulty.Medio),
            new("Televisão ", 4, Difficulty.Medio), new("Geladeira ", 4, Difficulty.Medio),
            new("Tartaruga ", 4, Difficulty.Medio), new("Borboleta ", 4, Difficulty.Medio),
            new("Melancia ", 4, Difficulty.Medio), new("Bicicleta ", 4, Difficulty.Medio),
            new("Dinossauro ", 4, Difficulty.Medio), new("Jabuticaba ", 5, Difficulty.Medio),
            new("Matemática ", 5, Difficulty.Medio), new("Universidade ", 5, Difficulty.Medio),
            new("Especialidade ", 5, Difficulty.Medio), new("Arqueologia ", 5, Difficulty.Medio),
            
            new("Enciclopédia ", 5, Difficulty.Medio),
            // DIFÍCIL 
            new("Pneu ", 1, Difficulty.Dificil), new("Ritmo ", 2, Difficulty.Dificil),
            new("Apto ", 2, Difficulty.Dificil), new("Cacto ", 2, Difficulty.Dificil),
            new("Advogado ", 4, Difficulty.Dificil), new("Objeção ", 3, Difficulty.Dificil),
            new("Sublinhar ", 3, Difficulty.Dificil), new("Psicologia ", 5, Difficulty.Dificil),
            new("Gratuito ", 3, Difficulty.Dificil), new("Circuito ", 3, Difficulty.Dificil),
            new("Fluido ", 2, Difficulty.Dificil), new("Rubrica", 3, Difficulty.Dificil),
            new("Saúde ", 3, Difficulty.Dificil), new("Açaí", 3, Difficulty.Dificil),
            new("País ️", 2, Difficulty.Dificil), new("Responsabilidade ", 6, Difficulty.Dificil),
            new("Inconstitucional ", 6, Difficulty.Dificil), new("Biodiversidade ", 6, Difficulty.Dificil),
            new("Paralelepípedo ", 7, Difficulty.Dificil), new("Desproporcionalidade", 7, Difficulty.Dificil),
            new("Inconstitucionalissimamente ", 10, Difficulty.Dificil)
        };
    }
}