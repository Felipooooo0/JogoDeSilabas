using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum Difficulty { Facil, Medio, Dificil }
public enum WordCategory { Geral, Animais, Alimentos, Ciencia }

public struct WordData
{
    public string Word;
    public int Syllables;
    public Difficulty Level;
    public WordCategory Category;

    public WordData(string word, int syllables, Difficulty level = Difficulty.Facil, WordCategory category = WordCategory.Geral)
    {
        Word = word.ToUpper();
        Syllables = syllables;
        Level = level;
        Category = category;
    }
}

public partial class GameManager : Control
{
    [Export] public Label WordLabel;
    [Export] public Container OptionsContainer;
    [Export] public Label TimerLabel;
    [Export] public Label LivesLabel;
    [Export] public Timer TurnTimer;
    [Export] public Label DescLabel;
    [Export] public Label FeedbackLabel;
    [Export] public Button RestartButton;
    [Export] public Button FullscreenButton;
    
    public static WordCategory SelectedCategory = WordCategory.Geral;
    private List<WordData> _wordDatabase;
    private readonly HashSet<WordData> _usedWords = new(); // <--- Histórico para evitar repetições
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
        if (_score >= 60) return Difficulty.Dificil;
        if (_score >= 30) return Difficulty.Medio;
        return Difficulty.Facil;
    }

    private void StartNewTurn()
    {
        if (FeedbackLabel != null) FeedbackLabel.Text = "";

        Difficulty currentDiff = GetCurrentDifficulty();
        TurnTimer.WaitTime = currentDiff switch
        {
            Difficulty.Dificil => 30.0,
            Difficulty.Medio => 25.0,
            _ => 15.0
        };
        
        var availableWords = _wordDatabase.Where(w => 
            w.Level <= currentDiff && 
            (SelectedCategory == WordCategory.Geral || w.Category == SelectedCategory) &&
            !_usedWords.Contains(w)
        ).ToList();

        
        if (availableWords.Count == 0)
        {
            _usedWords.Clear();
            availableWords = _wordDatabase.Where(w => 
                w.Level <= currentDiff && 
                (SelectedCategory == WordCategory.Geral || w.Category == SelectedCategory)
            ).ToList();
        }

        int randomIndex = _rand.Next(0, availableWords.Count);
        _currentWordData = availableWords[randomIndex];
        _usedWords.Add(_currentWordData);

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
           // animais
new("Cão", 1, Difficulty.Facil, WordCategory.Animais),
new("Gato", 2, Difficulty.Facil, WordCategory.Animais),
new("Peixe", 2, Difficulty.Facil, WordCategory.Animais),
new("Pato", 2, Difficulty.Facil, WordCategory.Animais),
new("Rato", 2, Difficulty.Facil, WordCategory.Animais),
new("Sapo", 2, Difficulty.Facil, WordCategory.Animais),
new("Vaca", 2, Difficulty.Facil, WordCategory.Animais),
new("Boi", 1, Difficulty.Facil, WordCategory.Animais),
new("Porco", 2, Difficulty.Facil, WordCategory.Animais),
new("Cavalo", 3, Difficulty.Facil, WordCategory.Animais),
new("Coelho", 3, Difficulty.Facil, WordCategory.Animais),
new("Galinha", 3, Difficulty.Facil, WordCategory.Animais),
new("Macaco", 3, Difficulty.Facil, WordCategory.Animais),
new("Leão", 2, Difficulty.Facil, WordCategory.Animais),
new("Tigre", 2, Difficulty.Facil, WordCategory.Animais),
new("Urso", 2, Difficulty.Facil, WordCategory.Animais),
new("Lobo", 2, Difficulty.Facil, WordCategory.Animais),
new("Zebra", 3, Difficulty.Facil, WordCategory.Animais),
new("Girafa", 3, Difficulty.Facil, WordCategory.Animais),
new("Elefante", 4, Difficulty.Facil, WordCategory.Animais),
new("Cobra", 2, Difficulty.Facil, WordCategory.Animais),
new("Tubarão", 3, Difficulty.Facil, WordCategory.Animais),
new("Baleia", 3, Difficulty.Facil, WordCategory.Animais),
new("Golfinho", 4, Difficulty.Facil, WordCategory.Animais),
new("Papagaio", 4, Difficulty.Facil, WordCategory.Animais),
new("Pinguim", 3, Difficulty.Facil, WordCategory.Animais),
new("Abelha", 3, Difficulty.Facil, WordCategory.Animais),
new("Formiga", 3, Difficulty.Facil, WordCategory.Animais),
new("Aranha", 3, Difficulty.Facil, WordCategory.Animais),
new("Mosquito", 4, Difficulty.Facil, WordCategory.Animais),
new("Tartaruga", 4, Difficulty.Medio, WordCategory.Animais),
new("Borboleta", 4, Difficulty.Medio, WordCategory.Animais),
new("Canguru", 4, Difficulty.Medio, WordCategory.Animais),
new("Hipopótamo", 5, Difficulty.Medio, WordCategory.Animais),
new("Rinoceronte", 5, Difficulty.Medio, WordCategory.Animais),
new("Crocodilo", 4, Difficulty.Medio, WordCategory.Animais),
new("Flamingo", 4, Difficulty.Medio, WordCategory.Animais),
new("Camaleão", 4, Difficulty.Medio, WordCategory.Animais),
new("Ornitorrinco", 6, Difficulty.Medio, WordCategory.Animais),
new("Escorpião", 4, Difficulty.Medio, WordCategory.Animais),
new("Caranguejo", 5, Difficulty.Medio, WordCategory.Animais),
new("Polvo", 3, Difficulty.Medio, WordCategory.Animais),
new("Lagosta", 4, Difficulty.Medio, WordCategory.Animais),
new("Javali", 3, Difficulty.Medio, WordCategory.Animais),
new("Hiena", 3, Difficulty.Medio, WordCategory.Animais),
new("Gorila", 3, Difficulty.Medio, WordCategory.Animais),
new("Pantera", 4, Difficulty.Medio, WordCategory.Animais),
new("Chacal", 3, Difficulty.Medio, WordCategory.Animais),
new("Gazela", 3, Difficulty.Medio, WordCategory.Animais),
new("Avestruz", 4, Difficulty.Medio, WordCategory.Animais),
new("Orangotango", 6, Difficulty.Dificil, WordCategory.Animais),
new("Tamanduá", 4, Difficulty.Dificil, WordCategory.Animais),
new("Pangolim", 4, Difficulty.Dificil, WordCategory.Animais),
new("Axolote", 4, Difficulty.Dificil, WordCategory.Animais),
// alimentos
new("Pão", 1, Difficulty.Facil, WordCategory.Alimentos),
new("Arroz", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Feijão", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Leite", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Ovo", 1, Difficulty.Facil, WordCategory.Alimentos),
new("Sal", 1, Difficulty.Facil, WordCategory.Alimentos),
new("Mel", 1, Difficulty.Facil, WordCategory.Alimentos),
new("Queijo", 3, Difficulty.Facil, WordCategory.Alimentos),
new("Carne", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Peixe", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Frango", 3, Difficulty.Facil, WordCategory.Alimentos),
new("Bolo", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Pizza", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Sopa", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Massa", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Batata", 3, Difficulty.Facil, WordCategory.Alimentos),
new("Banana", 3, Difficulty.Facil, WordCategory.Alimentos),
new("Maçã", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Uva", 1, Difficulty.Facil, WordCategory.Alimentos),
new("Pera", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Manga", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Limão", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Coco", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Milho", 2, Difficulty.Facil, WordCategory.Alimentos),
new("Manteiga", 3, Difficulty.Facil, WordCategory.Alimentos),
new("Chocolate", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Melancia", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Morango", 3, Difficulty.Medio, WordCategory.Alimentos),
new("Abacaxi", 3, Difficulty.Medio, WordCategory.Alimentos),
new("Maracujá", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Tangerina", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Azeitona", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Pipoca", 3, Difficulty.Medio, WordCategory.Alimentos),
new("Hambúrguer", 5, Difficulty.Medio, WordCategory.Alimentos),
new("Sanduíche", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Macarrão", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Biscoito", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Sorvete", 3, Difficulty.Medio, WordCategory.Alimentos),
new("Iogurte", 3, Difficulty.Medio, WordCategory.Alimentos),
new("Gelatina", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Panqueca", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Brigadeiro", 5, Difficulty.Medio, WordCategory.Alimentos),
new("Geleia", 3, Difficulty.Medio, WordCategory.Alimentos),
new("Castanha", 4, Difficulty.Medio, WordCategory.Alimentos),
new("Jabuticaba", 5, Difficulty.Medio, WordCategory.Alimentos),
new("Açaí", 3, Difficulty.Dificil, WordCategory.Alimentos),
new("Carambola", 4, Difficulty.Dificil, WordCategory.Alimentos),
new("Alcachofra", 5, Difficulty.Dificil, WordCategory.Alimentos),
new("Parmesão", 4, Difficulty.Dificil, WordCategory.Alimentos),
// ciencia
new("Flor", 1, Difficulty.Facil, WordCategory.Ciencia),
new("Sol", 1, Difficulty.Facil, WordCategory.Ciencia),
new("Lua", 1, Difficulty.Facil, WordCategory.Ciencia),
new("Água", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Ar", 1, Difficulty.Facil, WordCategory.Ciencia),
new("Fogo", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Terra", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Chuva", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Nuvem", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Gelo", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Planta", 3, Difficulty.Facil, WordCategory.Ciencia),
new("Árvore", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Folha", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Raiz", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Célula", 3, Difficulty.Facil, WordCategory.Ciencia),
new("Livro", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Caderno", 3, Difficulty.Facil, WordCategory.Ciencia),
new("Escola", 3, Difficulty.Facil, WordCategory.Ciencia),
new("Caneta", 3, Difficulty.Facil, WordCategory.Ciencia),
new("Lápis", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Mapa", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Planeta", 3, Difficulty.Facil, WordCategory.Ciencia),
new("Estrela", 3, Difficulty.Facil, WordCategory.Ciencia),
new("Mundo", 2, Difficulty.Facil, WordCategory.Ciencia),
new("Computador", 4, Difficulty.Medio, WordCategory.Ciencia),
new("Matemática", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Universidade", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Arqueologia", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Enciclopédia", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Astronomia", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Biologia", 4, Difficulty.Medio, WordCategory.Ciencia),
new("Química", 3, Difficulty.Medio, WordCategory.Ciencia),
new("Física", 3, Difficulty.Medio, WordCategory.Ciencia),
new("Geologia", 4, Difficulty.Medio, WordCategory.Ciencia),
new("Ecologia", 4, Difficulty.Medio, WordCategory.Ciencia),
new("Laboratório", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Microscópio", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Experimento", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Pesquisa", 3, Difficulty.Medio, WordCategory.Ciencia),
new("Planeta", 3, Difficulty.Medio, WordCategory.Ciencia),
new("Galáxia", 3, Difficulty.Medio, WordCategory.Ciencia),
new("Vulcão", 3, Difficulty.Medio, WordCategory.Ciencia),
new("Dinossauro", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Eletricidade", 5, Difficulty.Medio, WordCategory.Ciencia),
new("Psicologia", 5, Difficulty.Dificil, WordCategory.Ciencia),
new("Circuito", 3, Difficulty.Dificil, WordCategory.Ciencia),
new("Biodiversidade", 6, Difficulty.Dificil, WordCategory.Ciencia),
new("Fotossíntese", 6, Difficulty.Dificil, WordCategory.Ciencia),
new("Termodinâmica", 6, Difficulty.Dificil, WordCategory.Ciencia),
// geral
new("Sol", 1, Difficulty.Facil, WordCategory.Geral),
new("Mãe", 1, Difficulty.Facil, WordCategory.Geral),
new("Pai", 1, Difficulty.Facil, WordCategory.Geral),
new("Irmão", 2, Difficulty.Facil, WordCategory.Geral),
new("Amigo", 2, Difficulty.Facil, WordCategory.Geral),
new("Trem", 1, Difficulty.Facil, WordCategory.Geral),
new("Luz", 1, Difficulty.Facil, WordCategory.Geral),
new("Casa", 2, Difficulty.Facil, WordCategory.Geral),
new("Mesa", 2, Difficulty.Facil, WordCategory.Geral),
new("Bola", 2, Difficulty.Facil, WordCategory.Geral),
new("Carro", 2, Difficulty.Facil, WordCategory.Geral),
new("Porta", 2, Difficulty.Facil, WordCategory.Geral),
new("Rua", 1, Difficulty.Facil, WordCategory.Geral),
new("Praia", 2, Difficulty.Facil, WordCategory.Geral),
new("Mar", 1, Difficulty.Facil, WordCategory.Geral),
new("Rio", 1, Difficulty.Facil, WordCategory.Geral),
new("Céu", 1, Difficulty.Facil, WordCategory.Geral),
new("Chão", 1, Difficulty.Facil, WordCategory.Geral),
new("Janela", 3, Difficulty.Facil, WordCategory.Geral),
new("Relógio", 3, Difficulty.Facil, WordCategory.Geral),
new("Espelho", 3, Difficulty.Facil, WordCategory.Geral),
new("Sapato", 3, Difficulty.Facil, WordCategory.Geral),
new("Cadeira", 3, Difficulty.Facil, WordCategory.Geral),
new("Escola", 3, Difficulty.Facil, WordCategory.Geral),
new("Telefone", 4, Difficulty.Facil, WordCategory.Geral),
new("Caneta", 3, Difficulty.Facil, WordCategory.Geral),
new("Mochila", 3, Difficulty.Facil, WordCategory.Geral),
new("Livro", 2, Difficulty.Facil, WordCategory.Geral),
new("Papel", 2, Difficulty.Facil, WordCategory.Geral),
new("Foto", 2, Difficulty.Facil, WordCategory.Geral),
new("Televisão", 4, Difficulty.Medio, WordCategory.Geral),
new("Bicicleta", 4, Difficulty.Medio, WordCategory.Geral),
new("Geladeira", 4, Difficulty.Medio, WordCategory.Geral),
new("Ventilador", 5, Difficulty.Medio, WordCategory.Geral),
new("Elevador", 4, Difficulty.Medio, WordCategory.Geral),
new("Computador", 4, Difficulty.Medio, WordCategory.Geral),
new("Aeroporto", 4, Difficulty.Medio, WordCategory.Geral),
new("Restaurante", 5, Difficulty.Medio, WordCategory.Geral),
new("Biblioteca", 4, Difficulty.Medio, WordCategory.Geral),
new("Hospital", 4, Difficulty.Medio, WordCategory.Geral),
new("Mercado", 3, Difficulty.Medio, WordCategory.Geral),
new("Shopping", 3, Difficulty.Medio, WordCategory.Geral),
new("Cinema", 3, Difficulty.Medio, WordCategory.Geral),
new("Teatro", 3, Difficulty.Medio, WordCategory.Geral),
new("Futebol", 3, Difficulty.Medio, WordCategory.Geral),
new("Música", 3, Difficulty.Medio, WordCategory.Geral),
new("Viagem", 3, Difficulty.Medio, WordCategory.Geral),
new("Férias", 3, Difficulty.Medio, WordCategory.Geral),
new("Aniversário", 5, Difficulty.Medio, WordCategory.Geral),
new("Especialidade", 5, Difficulty.Medio, WordCategory.Geral),
new("Advogado", 4, Difficulty.Dificil, WordCategory.Geral),
new("Objeção", 3, Difficulty.Dificil, WordCategory.Geral),
new("Sublinhar", 3, Difficulty.Dificil, WordCategory.Geral),
new("Gratuito", 3, Difficulty.Dificil, WordCategory.Geral),
new("Rubrica", 3, Difficulty.Dificil, WordCategory.Geral),
new("Responsabilidade", 6, Difficulty.Dificil, WordCategory.Geral),
new("Inconstitucional", 6, Difficulty.Dificil, WordCategory.Geral)

        };
    }
}