using jeo_ano_ba.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;


namespace jeo_ano_ba.Services
{
    public class CustomBoardDraftService
    {
        public const int CategoryCount = 6;
        public const int CluesPerCategory = 5;
        public const int TotalClues = CategoryCount * CluesPerCategory;
        public static readonly int[] PointValues = { 100, 200, 300, 400, 500 };
        
        public List<CustomCategoryInput> Categories { get; } = new();

        //empty board
        public CustomBoardDraftService() { Reset(); }
        //wipes the board 
        private void Reset()
        {
            Categories.Clear();
            for (int i = 0; i < CategoryCount; i++)
            {
                var category = new CustomCategoryInput
                {
                    CategoryName = string.Empty,

                };
                for (int j = 0; j < CluesPerCategory; j++)
                {
                    category.Clues.Add((string.Empty, string.Empty));

                }
                Categories.Add(category);
            }
        }
        public void RenameCategory(int categoryIndex, string name)
        {
            ValidCategory(categoryIndex);
            Categories[categoryIndex].CategoryName = name.Trim();
        }
        public void SaveClue(int categoryIndex, int clueIndex, string question, string answer)
        {
            ValidCategory(categoryIndex);
            ValidClue(clueIndex);
            Categories[categoryIndex].Clues[clueIndex] = (question.Trim(), answer.Trim());
        }
        public int GetFilledClueCount()
        {
            return Categories.Sum(category=>category.Clues.Count(clue => !string.IsNullOrWhiteSpace(clue.Item1) && !string.IsNullOrWhiteSpace(clue.Item2)));
        }
        public bool SaveOnly()
        {
            return Categories.Any(category =>!string.IsNullOrWhiteSpace(category.CategoryName) || category.Clues.Any(clue => !string.IsNullOrWhiteSpace(clue.Item1) || !string.IsNullOrWhiteSpace(clue.Item2)));
        }
        public bool SaveAndStart()
        {
            bool allCategoriesFilled = Categories.All(category => !string.IsNullOrWhiteSpace(category.CategoryName));
            return allCategoriesFilled && GetFilledClueCount() == TotalClues;
        }
        private static void ValidCategory(int index)
        {
            if (index < 0 || index >= CategoryCount)
                throw new ArgumentOutOfRangeException(nameof(index));
        }
        private static void ValidClue(int index)
        {
            if (index < 0 || index >= CluesPerCategory)
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
