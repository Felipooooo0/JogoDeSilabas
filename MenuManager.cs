using Godot;

public partial class MenuManager : Control
{
    [Export] public Label TitleLabel;
    
   
    [Export] public Control MainMenuContainer;    
    [Export] public Control ThemeMenuContainer;   

   
    [Export] public Button PlayButton; 
    [Export] public Button ExitButton;

    
    [Export] public Button BtnGeral;
    [Export] public Button BtnAnimais;
    [Export] public Button BtnAlimentos;
    [Export] public Button BtnCiencia;
    [Export] public Button BtnVoltar; 

    public override void _Ready()
    {
        RenderingServer.SetDefaultClearColor(Colors.White);
        ApplyVisualTheme();

        // Garante que o menu principal começa visível e o de temas oculto ao abrir o jogo
        if (MainMenuContainer != null) MainMenuContainer.Show();
        if (ThemeMenuContainer != null) ThemeMenuContainer.Hide();

        // Configuração dos cliques dos botões principais
        if (PlayButton != null)
        {
            PlayButton.Pressed += OnPlayClicked;
        }

        if (ExitButton != null)
        {
            ExitButton.Pressed += () => GetTree().Quit();
        }

        // Configuração dos cliques dos botões de temas
        if (BtnGeral != null) BtnGeral.Pressed += () => StartGameWithCategory(WordCategory.Geral);
        if (BtnAnimais != null) BtnAnimais.Pressed += () => StartGameWithCategory(WordCategory.Animais);
        if (BtnAlimentos != null) BtnAlimentos.Pressed += () => StartGameWithCategory(WordCategory.Alimentos);
        if (BtnCiencia != null) BtnCiencia.Pressed += () => StartGameWithCategory(WordCategory.Ciencia);
        
        if (BtnVoltar != null)
        {
            BtnVoltar.Pressed += OnBackClicked;
        }
    }

    private void OnPlayClicked()
    {
        if (MainMenuContainer != null) 
        {
            MainMenuContainer.Hide();
        }

        if (ThemeMenuContainer != null) 
        {
            ThemeMenuContainer.Show();
        }
    }
    private void OnBackClicked()
    {
        if (ThemeMenuContainer != null) ThemeMenuContainer.Hide();
        if (MainMenuContainer != null) MainMenuContainer.Show();
    }

    private void StartGameWithCategory(WordCategory category)
    {
        GameManager.SelectedCategory = category;

        if (ResourceLoader.Exists("res://Main.tscn"))
        {
            GetTree().ChangeSceneToFile("res://Main.tscn");
        }
        else
        {
            GetTree().ChangeSceneToFile("res://Game.tscn");
        }
    }

    private void ApplyVisualTheme()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        if (TitleLabel != null)
        {
            TitleLabel.Text = "JOGO DAS SÍLABAS";
            TitleLabel.AddThemeFontSizeOverride("font_size", 70);
            TitleLabel.AddThemeColorOverride("font_color", new Color("0F172A"));
            TitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        }

        // Estiliza os botões principais
        if (PlayButton != null)
        {
            PlayButton.Text = "JOGAR";
            ApplyButtonStyle(PlayButton, new Color("22C55E"), new Color("15803D"), new Vector2(310, 90), 36);
        }

        if (ExitButton != null)
        {
            ExitButton.Text = "SAIR";
            ApplyButtonStyle(ExitButton, new Color("EF4444"), new Color("991B1B"), new Vector2(310, 80), 32);
        }

        // Estiliza os botões de temas sem emojis
        if (BtnGeral != null)
        {
            BtnGeral.Text = "Geral";
            ApplyButtonStyle(BtnGeral, new Color("64748B"), new Color("334155"), new Vector2(310, 70), 24);
        }

        if (BtnAnimais != null)
        {
            BtnAnimais.Text = "Animais";
            ApplyButtonStyle(BtnAnimais, new Color("FF9800"), new Color("E65100"), new Vector2(310, 70), 24);
        }

        if (BtnAlimentos != null)
        {
            BtnAlimentos.Text = "Alimentos";
            ApplyButtonStyle(BtnAlimentos, new Color("22C55E"), new Color("15803D"), new Vector2(310, 70), 24);
        }

        if (BtnCiencia != null)
        {
            BtnCiencia.Text = "Ciencia";
            ApplyButtonStyle(BtnCiencia, new Color("29B6F6"), new Color("0277BD"), new Vector2(310, 70), 24);
        }

        if (BtnVoltar != null)
        {
            BtnVoltar.Text = "Voltar";
            ApplyButtonStyle(BtnVoltar, new Color("94A3B8"), new Color("475569"), new Vector2(310, 60), 22);
        }
    }

    private void ApplyButtonStyle(Button button, Color baseCol, Color darkCol, Vector2 size, int fontSize)
    {
        StyleBoxFlat normalStyle = new StyleBoxFlat
        {
            BgColor = baseCol,
            BorderWidthBottom = 6,
            BorderColor = darkCol,
            ShadowSize = 4,
            ShadowOffset = new Vector2(0, 3),
            ShadowColor = new Color(0, 0, 0, 0.15f)
        };
        normalStyle.SetCornerRadiusAll(16);

        StyleBoxFlat hoverStyle = (StyleBoxFlat)normalStyle.Duplicate();
        hoverStyle.BgColor = baseCol.Lightened(0.12f);

        StyleBoxFlat pressedStyle = (StyleBoxFlat)normalStyle.Duplicate();
        pressedStyle.BorderWidthBottom = 2;
        pressedStyle.ShadowSize = 0;
        pressedStyle.ContentMarginTop = 4;

        button.CustomMinimumSize = size;
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);
        button.AddThemeStyleboxOverride("focus", normalStyle);
    }
}