using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba.ViewModels;

public class NewBoardViewModel : BaseViewModel
{
    private readonly GameDatabaseService _dbService;
    private readonly int? _editingGameId;
    private string? _editingGameOriginalName;

    private const int CategoryCount = 6;
    private const int ClueCount = 5;

    private readonly string[] _categoryNames = new string[CategoryCount];
    private readonly string[,] _questions = new string[CategoryCount, ClueCount];
    private readonly string[,] _answers = new string[CategoryCount, ClueCount];
    private readonly bool[,] _filled = new bool[CategoryCount, ClueCount];

    private int _filledCount;

    public NewBoardViewModel(GameDatabaseService dbService, int? editingGameId = null)
    {
        _dbService = dbService;
        _editingGameId = editingGameId;
    }

    public int FilledCount
    {
        get => _filledCount;
        private set
        {
            if (SetProperty(ref _filledCount, value))
                OnPropertyChanged(nameof(ProgressText));
        }
    }

    public string ProgressText => $"{FilledCount}/30 filled";

    public bool IsEditing => _editingGameId.HasValue;

    public string GetCategoryName(int column)
    {
        return _categoryNames[column] ?? string.Empty;
    }

    public void SetCategoryName(int column, string value)
    {
        _categoryNames[column] = value.Trim();
    }

    public string GetQuestion(int column, int row)
    {
        return _questions[column, row] ?? string.Empty;
    }

    public string GetAnswer(int column, int row)
    {
        return _answers[column, row] ?? string.Empty;
    }

    public bool IsFilled(int column, int row)
    {
        return _filled[column, row];
    }

    public void SaveClueInMemory(int column, int row, string question, string answer)
    {
        _questions[column, row] = question.Trim();
        _answers[column, row] = answer.Trim();

        if (!_filled[column, row])
        {
            _filled[column, row] = true;
            FilledCount++;
        }
    }

    public async Task LoadExistingBoardAsync()
    {
        if (!_editingGameId.HasValue)
            return;

        GameDb game = await _dbService.GetGameWithDetailsAsync(_editingGameId.Value);
        _editingGameOriginalName = game.Name;

        List<CategoryDb> categories = game.Categories
            .OrderBy(category => category.Id)
            .ToList();

        if (categories.Count != CategoryCount)
            throw new InvalidOperationException("This saved game does not contain exactly 6 categories.");

        FilledCount = 0;

        for (int column = 0; column < CategoryCount; column++)
        {
            CategoryDb category = categories[column];
            _categoryNames[column] = category.Name;

            List<ClueDb> clues = category.Clues
                .OrderBy(clue => clue.PointValue)
                .ToList();

            if (clues.Count != ClueCount)
                throw new InvalidOperationException($"Category {column + 1} does not contain exactly 5 clues.");

            for (int row = 0; row < ClueCount; row++)
            {
                ClueDb clue = clues[row];

                _questions[column, row] = clue.Question;
                _answers[column, row] = clue.Answer;
                _filled[column, row] = true;

                FilledCount++;
            }
        }
    }

    public async Task<int> SaveBoardAsync()
    {
        List<CustomCategoryInput> categories = BuildCategoriesFromBoard();

        if (_editingGameId.HasValue)
        {
            await _dbService.UpdatePlayerAuthoredGameAsync(
                _editingGameId.Value,
                _editingGameOriginalName ?? "Custom Game",
                categories,
                startingPointValue: 100,
                pointIncrement: 100);

            return _editingGameId.Value;
        }

        return await _dbService.BuildPlayerAuthoredGameAsync(
            "Custom Game",
            categories,
            startingPointValue: 100,
            pointIncrement: 100);
    }

    private List<CustomCategoryInput> BuildCategoriesFromBoard()
    {
        var categories = new List<CustomCategoryInput>();

        for (int column = 0; column < CategoryCount; column++)
        {
            string categoryName = _categoryNames[column]?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(categoryName))
                throw new InvalidOperationException("Please enter all 6 category names.");

            var clues = new List<(string Question, string Answer)>();

            for (int row = 0; row < ClueCount; row++)
            {
                if (!_filled[column, row])
                    throw new InvalidOperationException("Please complete all 30 questions and answers before saving.");

                clues.Add((
                    _questions[column, row],
                    _answers[column, row]
                ));
            }

            categories.Add(new CustomCategoryInput
            {
                CategoryName = categoryName,
                Clues = clues
            });
        }

        return categories;
    }
}