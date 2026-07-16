using System;

namespace jeo_ano_ba.Models;

public class CustomCategoryInput // used for making custom clues and categories (not yet saved)
{
    public string CategoryName { get; set; } = string.Empty;

    public List<(string Question, string Answer)> Clues { get; set; } = new();
}