using Godot;

public partial class InicialSilabas : Control
{
	[Export] public Control MainMenuContainer;  // Arraste seu PanelContainer aqui
	[Export] public Control ThemeMenuContainer; // Arraste seu PanelContainer2 aqui

	[Export] public Button PlayButton;
	[Export] public Button ExitButton;

	[Export] public Button BtnGeral;
	[Export] public Button BtnAnimais;
	[Export] public Button BtnAlimentos;
	[Export] public Button BtnCiencia;
	[Export] public Button BtnVoltar;
	

	public override void _Ready()
	{
	
		if (MainMenuContainer != null)
			MainMenuContainer.Show();

		if (ThemeMenuContainer != null)
			ThemeMenuContainer.Hide();


		if (PlayButton != null)
			PlayButton.Pressed += OnPlayClicked;

		if (ExitButton != null)
			ExitButton.Pressed += () => GetTree().Quit();


		if (BtnGeral != null)
			BtnGeral.Pressed += () => StartGameWithCategory(WordCategory.Geral);

		if (BtnAnimais != null)
			BtnAnimais.Pressed += () => StartGameWithCategory(WordCategory.Animais);

		if (BtnAlimentos != null)
			BtnAlimentos.Pressed += () => StartGameWithCategory(WordCategory.Alimentos);

		if (BtnCiencia != null)
			BtnCiencia.Pressed += () => StartGameWithCategory(WordCategory.Ciencia);

		if (BtnVoltar != null)
			BtnVoltar.Pressed += OnBackClicked;
	}


	private void OnPlayClicked()
	{
		if (MainMenuContainer != null)
			MainMenuContainer.Hide();

		if (ThemeMenuContainer != null)
			ThemeMenuContainer.Show();
	}

	private void OnBackClicked()
	{
		if (ThemeMenuContainer != null)
			ThemeMenuContainer.Hide();

		if (MainMenuContainer != null)
			MainMenuContainer.Show();
	}


	
	private void StartGameWithCategory(WordCategory category)
	{
		GameManager.SelectedCategory = category;

		// Se a sua cena principal tiver outro nome (ex: Jogo.tscn), mude o texto abaixo!
		if (ResourceLoader.Exists("res://Main.tscn"))
		{
			GetTree().ChangeSceneToFile("res://Main.tscn");
		}
		else
		{
			GetTree().ChangeSceneToFile("res://Game.tscn");
		}
	}
}
