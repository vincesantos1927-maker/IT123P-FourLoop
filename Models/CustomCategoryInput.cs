using System;

namespace jeo_ano_ba.Models;

public class CustomCategoryInput
{
    public string CategoryName { get; set; } = string.Empty;

    public List<(string Question, string Answer)> Clues { get; set; } = new();
}