using System;
using System.Collections.Generic;
using System.Text;

namespace jeo_ano_ba.Models;

// A player-authored category: a name plus up to 5 question/answer pairs.
// Used only as an in-memory carrier between the builder UI and the database
// service — point values are assigned automatically, never entered by the player.
public class CustomCategoryInput
{
    public string CategoryName { get; set; } = string.Empty;
    public List<(string Question, string Answer)> Clues { get; set; } = new();
}