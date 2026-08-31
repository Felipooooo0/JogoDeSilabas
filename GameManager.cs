using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum Difficulty { Facil, Medio, Dificil }
public enum WordCategory { Geral, Animais, Alimentos, Ciencia }
//srstst
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
	[Export] public Button MenuButton;
	[Export] public Container MarginContainer2; 
	[Export] public string MenuScenePath = "res://InicialBase.tscn"; // Caminho atualizado aqui
	
	public static WordCategory SelectedCategory = WordCategory.Geral;
	private List<WordData> _wordDatabase;
	private readonly HashSet<WordData> _usedWords = new(); 
	private WordData _currentWordData;
	private int _lives = 3;
	private int _score = 0;
	private bool _canAnswer = false;
	private readonly Random _rand = new();

	public override void _Ready()
	{
		InitializeDatabase();

		MarginContainer2?.Hide();

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

		if (TurnTimer != null)
		{
			TurnTimer.Timeout += OnTurnTimeout;
		}
		
		StartNewTurn();
	}

	public override void _Process(double delta)
	{
		if (TurnTimer != null && !TurnTimer.IsStopped() && TimerLabel != null)
		{
			TimerLabel.Text = $"Tempo: {Mathf.Ceil(TurnTimer.TimeLeft)}s";
		}
	}

	private void OnMenuButtonPressed()
	{
		if (!string.IsNullOrEmpty(MenuScenePath))
		{
			GetTree().ChangeSceneToFile(MenuScenePath);
		}
	}

	private Difficulty GetCurrentDifficulty()
	{
		if (_score >= 60) return Difficulty.Dificil;
		if (_score >= 30) return Difficulty.Medio;
		return Difficulty.Facil;
	}

	private void StartNewTurn()
	{
		_canAnswer = true;
		if (FeedbackLabel != null) FeedbackLabel.Text = "";

		Difficulty currentDiff = GetCurrentDifficulty();
		
		if (TurnTimer != null)
		{
			TurnTimer.WaitTime = currentDiff switch
			{
				Difficulty.Dificil => 30.0,
				Difficulty.Medio => 25.0,
				_ => 15.0
			};
		}
		
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

		if (availableWords.Count > 0)
		{
			int randomIndex = _rand.Next(0, availableWords.Count);
			_currentWordData = availableWords[randomIndex];
			_usedWords.Add(_currentWordData);

			if (WordLabel != null) WordLabel.Text = _currentWordData.Word;
			GenerateOptions();
		}
		
		UpdateHud();
		TurnTimer?.Start();
	}

	private async void OnOptionSelected(int chosenSyllables)
	{
		if (!_canAnswer) return;

		if (chosenSyllables == _currentWordData.Syllables)
		{
			_canAnswer = false;
			TurnTimer?.Stop();
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

	private async void OnTurnTimeout()
	{
		if (!_canAnswer) return;
		await HandleWrongAnswer($"Tempo esgotado! Era {_currentWordData.Syllables} sílaba(s)");
	}

	private async System.Threading.Tasks.Task HandleWrongAnswer(string message)
	{
		_canAnswer = false;
		TurnTimer?.Stop();
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
		if (WordLabel != null) WordLabel.Text = "FIM DE JOGO!\nPontos: " + _score;

		OptionsContainer?.Hide();
		TimerLabel?.Hide();
		LivesLabel?.Hide();
		DescLabel?.Hide();
		FeedbackLabel?.Hide();
		
		RestartButton?.Show();
		MenuButton?.Show();
		MarginContainer2?.Show(); 
	}

	private void GenerateOptions()
	{
		if (OptionsContainer == null) return;

		OptionsContainer.Show();

		List<Button> buttons = GetAllButtonsRecursive(OptionsContainer);

		if (buttons.Count == 0) return;

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
			int value = shuffledOptions[i];
			buttons[i].Text = $"{value}";

			var connections = buttons[i].GetSignalConnectionList("pressed");
			foreach (Godot.Collections.Dictionary connection in connections)
			{
				buttons[i].Disconnect("pressed", (Callable)connection["callable"]);
			}

			buttons[i].Pressed += () => OnOptionSelected(value);
		}
	}

	private List<Button> GetAllButtonsRecursive(Node parent)
	{
		List<Button> buttons = new();

		foreach (Node child in parent.GetChildren())
		{
			if (child is Button btn)
			{
				if (btn == RestartButton || btn == MenuButton) 
					continue;
				
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
			new("Zebra", 2, Difficulty.Facil, WordCategory.Animais),
			new("Girafa", 3, Difficulty.Facil, WordCategory.Animais),
			new("Elefante", 4, Difficulty.Facil, WordCategory.Animais),
			new("Cobra", 2, Difficulty.Facil, WordCategory.Animais),
			new("Tubarão", 3, Difficulty.Facil, WordCategory.Animais),
			new("Baleia", 3, Difficulty.Facil, WordCategory.Animais),
			new("Golfinho", 3, Difficulty.Facil, WordCategory.Animais),
			new("Papagaio", 4, Difficulty.Facil, WordCategory.Animais),
			new("Pinguim", 2, Difficulty.Facil, WordCategory.Animais),
			new("Abelha", 3, Difficulty.Facil, WordCategory.Animais),
			new("Formiga", 3, Difficulty.Facil, WordCategory.Animais),
			new("Aranha", 3, Difficulty.Facil, WordCategory.Animais),
			new("Mosquito", 3, Difficulty.Facil, WordCategory.Animais),
			new("Tartaruga", 4, Difficulty.Medio, WordCategory.Animais),
			new("Borboleta", 4, Difficulty.Medio, WordCategory.Animais),
			new("Canguru", 3, Difficulty.Medio, WordCategory.Animais),
			new("Hipopótamo", 5, Difficulty.Medio, WordCategory.Animais),
			new("Rinoceronte", 5, Difficulty.Medio, WordCategory.Animais),
			new("Crocodilo", 4, Difficulty.Medio, WordCategory.Animais),
			new("Flamingo", 3, Difficulty.Medio, WordCategory.Animais),
			new("Camaleão", 4, Difficulty.Medio, WordCategory.Animais),
			new("Ornitorrinco", 5, Difficulty.Medio, WordCategory.Animais),
			new("Escorpião", 4, Difficulty.Medio, WordCategory.Animais),
			new("Caranguejo", 4, Difficulty.Medio, WordCategory.Animais),
			new("Polvo", 2, Difficulty.Medio, WordCategory.Animais),
			new("Lagosta", 3, Difficulty.Medio, WordCategory.Animais),
			new("Javali", 3, Difficulty.Medio, WordCategory.Animais),
			new("Hiena", 3, Difficulty.Medio, WordCategory.Animais),
			new("Gorila", 3, Difficulty.Medio, WordCategory.Animais),
			new("Pantera", 3, Difficulty.Medio, WordCategory.Animais),
			new("Chacal", 2, Difficulty.Medio, WordCategory.Animais),
			new("Gazela", 3, Difficulty.Medio, WordCategory.Animais),
			new("Avestruz", 3, Difficulty.Medio, WordCategory.Animais),
			new("Orangotango", 5, Difficulty.Dificil, WordCategory.Animais),
			new("Tamanduá", 4, Difficulty.Dificil, WordCategory.Animais),
			new("Pangolim", 3, Difficulty.Dificil, WordCategory.Animais),
			new("Axolote", 4, Difficulty.Dificil, WordCategory.Animais),
			// alimentos
			new("Pão", 1, Difficulty.Facil, WordCategory.Alimentos),
			new("Arroz", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Feijão", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Leite", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Ovo", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Sal", 1, Difficulty.Facil, WordCategory.Alimentos),
			new("Mel", 1, Difficulty.Facil, WordCategory.Alimentos),
			new("Queijo", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Carne", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Peixe", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Frango", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Bolo", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Pizza", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Sopa", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Massa", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Batata", 3, Difficulty.Facil, WordCategory.Alimentos),
			new("Banana", 3, Difficulty.Facil, WordCategory.Alimentos),
			new("Maçã", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Uva", 2, Difficulty.Facil, WordCategory.Geral),
			new("Pera", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Manga", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Limão", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Coco", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Milho", 2, Difficulty.Facil, WordCategory.Alimentos),
			new("Manteiga", 3, Difficulty.Facil, WordCategory.Alimentos),
			new("Chocolate", 4, Difficulty.Medio, WordCategory.Alimentos),
			new("Melancia", 4, Difficulty.Medio, WordCategory.Alimentos),
			new("Morango", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Abacaxi", 4, Difficulty.Medio, WordCategory.Alimentos),
			new("Maracujá", 4, Difficulty.Medio, WordCategory.Alimentos),
			new("Tangerina", 4, Difficulty.Medio, WordCategory.Alimentos),
			new("Azeitona", 4, Difficulty.Medio, WordCategory.Alimentos),
			new("Pipoca", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Hambúrguer", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Sanduíche", 4, Difficulty.Medio, WordCategory.Alimentos),
			new("Macarrão", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Biscoito", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Sorvete", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Iogurte", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Gelatina", 4, Difficulty.Medio, WordCategory.Alimentos),
			new("Panqueca", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Brigadeiro", 4, Difficulty.Medio, WordCategory.Alimentos),
			new("Geleia", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Castanha", 3, Difficulty.Medio, WordCategory.Alimentos),
			new("Jabuticaba", 5, Difficulty.Medio, WordCategory.Alimentos),
			new("Açaí", 3, Difficulty.Dificil, WordCategory.Alimentos),
			new("Carambola", 4, Difficulty.Dificil, WordCategory.Alimentos),
			new("Alcachofra", 4, Difficulty.Dificil, WordCategory.Alimentos),
			new("Parmesão", 3, Difficulty.Dificil, WordCategory.Alimentos),
			// ciencia
			new("Flor", 1, Difficulty.Facil, WordCategory.Ciencia),
			new("Sol", 1, Difficulty.Facil, WordCategory.Ciencia),
			new("Lua", 2, Difficulty.Facil, WordCategory.Ciencia),
			new("Água", 2, Difficulty.Facil, WordCategory.Ciencia),
			new("Ar", 1, Difficulty.Facil, WordCategory.Ciencia),
			new("Fogo", 2, Difficulty.Facil, WordCategory.Ciencia),
			new("Terra", 2, Difficulty.Facil, WordCategory.Ciencia),
			new("Chuva", 2, Difficulty.Facil, WordCategory.Ciencia),
			new("Nuvem", 2, Difficulty.Facil, WordCategory.Ciencia),
			new("Gelo", 2, Difficulty.Facil, WordCategory.Ciencia),
			new("Planta", 2, Difficulty.Facil, WordCategory.Ciencia),
			new("Árvore", 3, Difficulty.Facil, WordCategory.Ciencia),
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
			new("Astronomia", 4, Difficulty.Medio, WordCategory.Ciencia),
			new("Biologia", 4, Difficulty.Medio, WordCategory.Ciencia),
			new("Química", 3, Difficulty.Medio, WordCategory.Ciencia),
			new("Física", 3, Difficulty.Medio, WordCategory.Ciencia),
			new("Geologia", 4, Difficulty.Medio, WordCategory.Ciencia),
			new("Ecologia", 4, Difficulty.Medio, WordCategory.Ciencia),
			new("Laboratório", 5, Difficulty.Medio, WordCategory.Ciencia),
			new("Microscópio", 4, Difficulty.Medio, WordCategory.Ciencia),
			new("Experimento", 5, Difficulty.Medio, WordCategory.Ciencia),
			new("Pesquisa", 3, Difficulty.Medio, WordCategory.Ciencia),
			new("Galáxia", 4, Difficulty.Medio, WordCategory.Ciencia),
			new("Vulcão", 2, Difficulty.Medio, WordCategory.Ciencia),
			new("Dinossauro", 4, Difficulty.Medio, WordCategory.Ciencia),
			new("Eletricidade", 5, Difficulty.Medio, WordCategory.Ciencia),
			new("Psicologia", 4, Difficulty.Dificil, WordCategory.Ciencia),
			new("Circuito", 3, Difficulty.Dificil, WordCategory.Ciencia),
			new("Biodiversidade", 6, Difficulty.Dificil, WordCategory.Ciencia),
			new("Fotossíntese", 5, Difficulty.Dificil, WordCategory.Ciencia),
			new("Termodinâmica", 5, Difficulty.Dificil, WordCategory.Ciencia),
			// geral
			new("Sol", 1, Difficulty.Facil, WordCategory.Geral),
			new("Mãe", 1, Difficulty.Facil, WordCategory.Geral),
			new("Pai", 1, Difficulty.Facil, WordCategory.Geral),
			new("Irmão", 2, Difficulty.Facil, WordCategory.Geral),
			new("Amigo", 3, Difficulty.Facil, WordCategory.Geral),
			new("Trem", 1, Difficulty.Facil, WordCategory.Geral),
			new("Luz", 1, Difficulty.Facil, WordCategory.Geral),
			new("Casa", 2, Difficulty.Facil, WordCategory.Geral),
			new("Mesa", 2, Difficulty.Facil, WordCategory.Geral),
			new("Bola", 2, Difficulty.Facil, WordCategory.Geral),
			new("Carro", 2, Difficulty.Facil, WordCategory.Geral),
			new("Porta", 2, Difficulty.Facil, WordCategory.Geral),
			new("Rua", 2, Difficulty.Facil, WordCategory.Geral),
			new("Praia", 2, Difficulty.Facil, WordCategory.Geral),
			new("Mar", 1, Difficulty.Facil, WordCategory.Geral),
			new("Rio", 2, Difficulty.Facil, WordCategory.Geral),
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
			new("Ventilador", 4, Difficulty.Medio, WordCategory.Geral),
			new("Elevador", 4, Difficulty.Medio, WordCategory.Geral),
			new("Computador", 4, Difficulty.Medio, WordCategory.Geral),
			new("Aeroporto", 4, Difficulty.Medio, WordCategory.Geral),
			new("Restaurante", 4, Difficulty.Medio, WordCategory.Geral),
			new("Biblioteca", 4, Difficulty.Medio, WordCategory.Geral),
			new("Hospital", 3, Difficulty.Medio, WordCategory.Geral),
			new("Mercado", 3, Difficulty.Medio, WordCategory.Geral),
			new("Shopping", 2, Difficulty.Medio, WordCategory.Geral),
			new("Cinema", 3, Difficulty.Medio, WordCategory.Geral),
			new("Teatro", 3, Difficulty.Medio, WordCategory.Geral),
			new("Futebol", 3, Difficulty.Medio, WordCategory.Geral),
			new("Música", 3, Difficulty.Medio, WordCategory.Geral),
			new("Viagem", 3, Difficulty.Medio, WordCategory.Geral),
			new("Férias", 2, Difficulty.Medio, WordCategory.Geral),
			new("Aniversário", 5, Difficulty.Medio, WordCategory.Geral),
			new("Especialidade", 6, Difficulty.Medio, WordCategory.Geral),
			new("Advogado", 4, Difficulty.Dificil, WordCategory.Geral),
			new("Objeção", 3, Difficulty.Dificil, WordCategory.Geral),
			new("Sublinhar", 3, Difficulty.Dificil, WordCategory.Geral),
			new("Gratuito", 3, Difficulty.Dificil, WordCategory.Geral),
			new("Rubrica", 3, Difficulty.Dificil, WordCategory.Geral),
			new("Responsabilidade", 7, Difficulty.Dificil, WordCategory.Geral),
			new("Inconstitucional", 6, Difficulty.Dificil, WordCategory.Geral)
		};
	}
}
