namespace Infrastructure.Nutrition
{
  /// <summary>
  /// Grams per one unit of measure for ingredient quantities when FDC portions are not used.
  /// Volumes use water-equivalent mass (~1 g/mL); US customary volumes use US definitions.
  /// </summary>
  internal static class IngredientGramConversions
  {
    internal static readonly Dictionary<string, decimal> ByUnit = new(StringComparer.OrdinalIgnoreCase)
    {
      ["g"] = 1,
      ["gram"] = 1,
      ["grams"] = 1,
      ["kg"] = 1000,
      ["kilogram"] = 1000,
      ["kilograms"] = 1000,
      ["mg"] = 0.001m,
      ["milligram"] = 0.001m,
      ["milligrams"] = 0.001m,
      ["oz"] = 28.3495m,
      ["ounce"] = 28.3495m,
      ["ounces"] = 28.3495m,
      ["lb"] = 453.592m,
      ["lbs"] = 453.592m,
      ["pound"] = 453.592m,
      ["pounds"] = 453.592m,

      // US volume (water-equivalent grams)
      ["tsp"] = 4.92892159375m,
      ["teaspoon"] = 4.92892159375m,
      ["tbsp"] = 14.78676478125m,
      ["tablespoon"] = 14.78676478125m,
      ["tbl"] = 14.78676478125m,
      ["tbs"] = 14.78676478125m,
      ["cup"] = 236.5882365m,
      ["c"] = 236.5882365m,
      ["floz"] = 29.5735295625m,
      ["fl oz"] = 29.5735295625m,
      ["fluid ounce"] = 29.5735295625m,
      ["pint"] = 473.176473m,
      ["pt"] = 473.176473m,
      ["quart"] = 946.352946m,
      ["qt"] = 946.352946m,
      ["gallon"] = 3785.411784m,
      ["gal"] = 3785.411784m,

      // Metric volume (water-equivalent)
      ["ml"] = 1,
      ["milliliter"] = 1,
      ["millilitre"] = 1,
      ["cc"] = 1,
      ["l"] = 1000,
      ["liter"] = 1000,
      ["litre"] = 1000,
      ["dl"] = 100,
      ["deciliter"] = 100,
      ["cl"] = 10,
      ["centiliter"] = 10,

      // Common recipe units (approximate)
      ["pinch"] = 0.31m,
      ["dash"] = 0.62m,
      ["stick"] = 113.398m,
      ["clove"] = 5m,
    };
  }
}
