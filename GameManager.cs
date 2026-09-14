using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// =========================================================================
// MANTIDO PARA NÃO QUEBRAR O InicialSilabas.cs
// =========================================================================
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

// =========================================================================
// ESTRUTURA PARA O JOGO DE CONVIVÊNCIA
// =========================================================================
public struct BehaviorData
{
    public string Title;
    public string ImagePath;
    public bool IsGood;

    public BehaviorData(string title, string imagePath, bool isGood)
    {
        Title = title;
        ImagePath = imagePath;
        IsGood = isGood;
    }
}

public partial class GameManager : Control
{
    public static WordCategory SelectedCategory = WordCategory.Geral;
      
    // =========================================================================
    // INTERFACE PRINCIPAL
    // =========================================================================
    [Export] public Label WordLabel;          
    [Export] public Container OptionsContainer; 
    [Export] public Label DescLabel;          
    [Export] public Label FeedbackLabel;
    [Export] public Button RestartButton;
    [Export] public Button MenuButton;
    [Export] public Container MarginContainer2; 
    [Export] public string MenuScenePath = "res://InicialBase.tscn";

    // =========================================================================
    // ELEMENTOS VISUAIS, ÍCONES E EFEITOS
    // =========================================================================
    [ExportGroup("Elementos do Jogo de Convivência")]
    [Export] public TextureRect ActionImage;  
    [Export] public ColorRect ScreenFlashRect; 
    [Export] public Texture2D VictoryTexture; // Arraste a foto de PARABÉNS aqui!
    
    [ExportGroup("Ícones dos Botões de Resposta")]
    [Export] public Texture2D GoodIcon; 
    [Export] public Texture2D BadIcon;  

    private List<BehaviorData> _behaviorDatabase;
    private readonly HashSet<BehaviorData> _usedBehaviors = new(); 
    private BehaviorData _currentBehavior;
    private int _score = 0;
    private int _correctAnswersCount = 0; // Conta quantas acertou no total
    private bool _canAnswer = false;
    private readonly Random _rand = new();
    private Tween _flashTween;

    public override void _Ready()
    {
       InitializeDatabase();

       MarginContainer2?.Hide();

       if (ScreenFlashRect != null)
       {
           ScreenFlashRect.MouseFilter = MouseFilterEnum.Ignore;
           ScreenFlashRect.Hide();
       }

       if (RestartButton != null)
       {
          RestartButton.Hide();
          RestartButton.Pressed += () => GetTree().ReloadCurrentScene();
       }

       if (MenuButton != null)
       {
          MenuButton.Hide();
          MenuButton.Pressed += OnMenuButtonPressed;
       }
       
       StartNewTurn();
    }

    private void OnMenuButtonPressed()
    {
       if (!string.IsNullOrEmpty(MenuScenePath))
       {
          GetTree().ChangeSceneToFile(MenuScenePath);
       }
    }

    private void StartNewTurn()
    {
       _canAnswer = true;
       if (FeedbackLabel != null) FeedbackLabel.Text = "";

       // Filtra apenas as atitudes que ainda não foram apresentadas
       var available = _behaviorDatabase.Where(b => !_usedBehaviors.Contains(b)).ToList();

       // Se já respondeu todas as perguntas, encerra o jogo
       if (available.Count == 0)
       {
          GameOver();
          return;
       }

       int randomIndex = _rand.Next(0, available.Count);
       _currentBehavior = available[randomIndex];
       _usedBehaviors.Add(_currentBehavior);

       if (WordLabel != null) WordLabel.Text = "Essa atitude é BOA ou RUIM?";
       if (DescLabel != null) DescLabel.Text = _currentBehavior.Title;

       if (ActionImage != null && !string.IsNullOrEmpty(_currentBehavior.ImagePath))
       {
           if (ResourceLoader.Exists(_currentBehavior.ImagePath))
               ActionImage.Texture = GD.Load<Texture2D>(_currentBehavior.ImagePath);
           else
               ActionImage.Texture = null;
       }

       GenerateOptions();
    }

    private async void OnOptionSelected(bool choseGood)
    {
       if (!_canAnswer) return;

       if (choseGood == _currentBehavior.IsGood)
       {
          _canAnswer = false;
          _score += 10;
          _correctAnswersCount++; // Incrementa os acertos
          
          ShowFeedback("Acertou!", new Color("22C55E")); 
          FlashScreen(new Color(0.0f, 0.9f, 0.2f, 0.65f)); // Brilho verde vivo
          
          await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
          StartNewTurn();
       }
       else
       {
          await HandleWrongAnswer("Errou! Essa atitude não é assim.");
       }
    }

    private async System.Threading.Tasks.Task HandleWrongAnswer(string message)
    {
       _canAnswer = false;
       ShowFeedback(message, new Color("EF4444")); 
       FlashScreen(new Color(1.0f, 0.0f, 0.1f, 0.65f)); // Brilho vermelho vivo
       
       await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
       StartNewTurn();
    }

    private void FlashScreen(Color flashColor)
    {
       if (ScreenFlashRect == null) return;

       ScreenFlashRect.Show();
       ScreenFlashRect.Color = flashColor;

       _flashTween?.Kill();
       _flashTween = CreateTween();
       _flashTween.TweenProperty(ScreenFlashRect, "color:a", 0.0f, 0.6f);
    }

    private void ShowFeedback(string text, Color color)
    {
       if (FeedbackLabel == null) return;
       FeedbackLabel.Text = text;
       FeedbackLabel.AddThemeColorOverride("font_color", color);
    }

    private void GameOver()
    {
       _canAnswer = false;
       int totalQuestions = _behaviorDatabase.Count;

       // Esconde os botões de resposta e mensagens temporárias
       OptionsContainer?.Hide();
       FeedbackLabel?.Hide();
       
       // Exibe o resultado de acertos
       if (WordLabel != null) 
           WordLabel.Text = $"FIM DE JOGO!\nVocê acertou {_correctAnswersCount} de {totalQuestions}!";

       // SE ACERTOU TUDO:
       if (_correctAnswersCount == totalQuestions)
       {
           if (DescLabel != null)
           {
               DescLabel.Text = "PARABÉNS! VOCÊ ACERTOU TUDO!";
               DescLabel.Show();
           }

           // Se configurou a imagem de vitória no Inspetor, ela é exibida
           if (ActionImage != null)
           {
               if (VictoryTexture != null)
               {
                   ActionImage.Texture = VictoryTexture;
                   ActionImage.Show();
               }
               else
               {
                   ActionImage.Hide();
               }
           }
       }
       else // SE ERROU PELO MENOS UMA:
       {
           if (DescLabel != null)
           {
               DescLabel.Text = "Tente novamente para acertar todas!";
               DescLabel.Show();
           }
           ActionImage?.Hide();
       }
       
       // Exibe botões de Reiniciar e Voltar ao Menu
       RestartButton?.Show();
       MenuButton?.Show();
       MarginContainer2?.Show(); 
    }

    private void GenerateOptions()
    {
       if (OptionsContainer == null) return;
       OptionsContainer.Show();

       List<Button> buttons = GetAllButtonsRecursive(OptionsContainer);
       if (buttons.Count < 2) return;

       foreach (var btn in buttons) btn.Hide();

       // Configura Botão 1 (BOM) com imagem
       ConfigureButtonIcon(buttons[0], GoodIcon, true);

       // Configura Botão 2 (RUIM) com imagem
       ConfigureButtonIcon(buttons[1], BadIcon, false);
    }

    private void ConfigureButtonIcon(Button button, Texture2D iconTexture, bool isGood)
    {
       button.Show();
       button.Text = "";  
       
       button.Icon = iconTexture;
       button.ExpandIcon = true; 
       button.IconAlignment = HorizontalAlignment.Center;
       
       button.CustomMinimumSize = new Vector2(250, 300); 

       ClearConnections(button);
       button.Pressed += () => OnOptionSelected(isGood);
    }

    private void ClearConnections(Button btn)
    {
        var connections = btn.GetSignalConnectionList("pressed");
        foreach (Godot.Collections.Dictionary connection in connections)
        {
            btn.Disconnect("pressed", (Callable)connection["callable"]);
        }
    }

    private List<Button> GetAllButtonsRecursive(Node parent)
    {
       List<Button> buttons = new();
       foreach (Node child in parent.GetChildren())
       {
          if (child is Button btn)
          {
             if (btn == RestartButton || btn == MenuButton) continue;
             buttons.Add(btn);
          }
          else if (child.GetChildCount() > 0)
          {
             buttons.AddRange(GetAllButtonsRecursive(child));
          }
       }
       return buttons;
    }

    private void InitializeDatabase()
    {
       _behaviorDatabase = new List<BehaviorData>
       {
           new("Dividir os brinquedos", "res://imagens/dividir_brinquedos.png", true),
           new("Empurrar o colega", "res://imagens/empurrar.png", false),
           new("Guardar o material", "res://imagens/guardar.png", true),
           new("Jogar lixo no chão", "res://imagens/lixo.png", false)
       };
    }
}