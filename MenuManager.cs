using Godot;

public partial class MenuManager : Control
{
    [Export] public Label TitleLabel;
    [Export] public Button PlayButton;
    [Export] public Button ExitButton;

    public override void _Ready()
    {
        // Define o fundo branco igual ao jogo
        RenderingServer.SetDefaultClearColor(Colors.White);

        // Aplica estilização e cores aos elementos
        ApplyVisualTheme();

        // Conecta as ações dos botões
        if (PlayButton != null)
        {
            PlayButton.Pressed -= OnPlayPressed;
            PlayButton.Pressed += OnPlayPressed;
        }

        if (ExitButton != null)
        {
            ExitButton.Pressed -= OnExitPressed;
            ExitButton.Pressed += OnExitPressed;
        }
    }

    private void OnPlayPressed()
    {
        // Tenta carregar Main.tscn (ou Game.tscn como alternativa)
        if (ResourceLoader.Exists("res://Main.tscn"))
        {
            GetTree().ChangeSceneToFile("res://Main.tscn");
        }
        else
        {
            GetTree().ChangeSceneToFile("res://Game.tscn");
        }
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }

    private void ApplyVisualTheme()
    {
        // Centraliza a tela principal
        SetAnchorsPreset(LayoutPreset.FullRect);

        // Título estilizado
        if (TitleLabel != null)
        {
            TitleLabel.Text = "JOGO DAS SÍLABAS";
            TitleLabel.AddThemeFontSizeOverride("font_size", 80);
            TitleLabel.AddThemeColorOverride("font_color", new Color("0F172A"));
            TitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        }

        // Botão JOGAR (Verde)
        if (PlayButton != null)
        {
            PlayButton.Text = "JOGAR";
            ApplyButtonStyle(PlayButton, new Color("22C55E"), new Color("15803D"), new Vector2(310, 100), 48);
        }

        // Botão SAIR (Vermelho)
        if (ExitButton != null)
        {
            ExitButton.Text = "SAIR";
            ApplyButtonStyle(ExitButton, new Color("EF4444"), new Color("991B1B"), new Vector2(310, 100), 48);
        }
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
}