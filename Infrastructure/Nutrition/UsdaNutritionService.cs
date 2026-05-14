using System.Text.Json;
using Application.DTOs.Nutrition;
using Application.Interfaces;
using Domain;
using Microsoft.Extensions.Options;

namespace Infrastructure.Nutrition
{
  public class UsdaNutritionService : INutritionService
  {
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Dictionary<string, decimal> GramConversions = new(StringComparer.OrdinalIgnoreCase)
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
      ["pounds"] = 453.592m
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FoodDataCentralSettings _settings;

    public UsdaNutritionService(
      IHttpClientFactory httpClientFactory,
      IOptions<FoodDataCentralSettings> settings)
    {
      _httpClientFactory = httpClientFactory;
      _settings = settings.Value;
    }

    public async Task<RecipeNutritionResult> CalculateRecipeAsync(
      IEnumerable<RecipeIngredient> ingredients,
      decimal servings,
      CancellationToken cancellationToken = default)
    {
      var result = new RecipeNutritionResult();
      var apiKey = GetApiKey();

      if (string.IsNullOrWhiteSpace(apiKey))
      {
        result.Warnings.Add("FoodDataCentral API key is not configured");
        return result;
      }

      decimal calories = 0;
      decimal protein = 0;
      decimal carbs = 0;
      decimal fat = 0;
      var matchedAny = false;

      foreach (var ingredient in ingredients.OrderBy(i => i.SortOrder))
      {
        var line = new IngredientNutritionResult
        {
          SortOrder = ingredient.SortOrder,
          Name = ingredient.Name
        };

        if (string.IsNullOrWhiteSpace(ingredient.Name))
        {
          line.Warning = "Missing ingredient name";
          result.Warnings.Add($"Could not calculate nutrition for sort order {ingredient.SortOrder}: missing name");
          result.Ingredients.Add(line);
          continue;
        }

        try
        {
          var ingredientNutrition = await CalculateIngredientAsync(ingredient, apiKey, cancellationToken);
          if (ingredientNutrition == null)
          {
            line.Warning = "Could not resolve food or portion for this ingredient";
            result.Warnings.Add($"Could not calculate nutrition for {ingredient.Name}");
            result.Ingredients.Add(line);
            continue;
          }

          line.Calories = Round(ingredientNutrition.Calories);
          line.ProteinGrams = Round(ingredientNutrition.ProteinGrams);
          line.CarbsGrams = Round(ingredientNutrition.CarbsGrams);
          line.FatGrams = Round(ingredientNutrition.FatGrams);
          result.Ingredients.Add(line);

          calories += ingredientNutrition.Calories;
          protein += ingredientNutrition.ProteinGrams;
          carbs += ingredientNutrition.CarbsGrams;
          fat += ingredientNutrition.FatGrams;
          matchedAny = true;
        }
        catch (Exception ex)
        {
          line.Warning = ex.Message;
          result.Warnings.Add($"Could not calculate nutrition for {ingredient.Name}: {ex.Message}");
          result.Ingredients.Add(line);
        }
      }

      if (!matchedAny) return result;

      result.Calories = Round(calories);
      result.ProteinGrams = Round(protein);
      result.CarbsGrams = Round(carbs);
      result.FatGrams = Round(fat);
      result.CalculatedUtc = DateTime.UtcNow;

      if (servings > 0)
      {
        result.CaloriesPerServing = Round(calories / servings);
        result.ProteinGramsPerServing = Round(protein / servings);
        result.CarbsGramsPerServing = Round(carbs / servings);
        result.FatGramsPerServing = Round(fat / servings);
      }

      return result;
    }

    private async Task<IngredientNutrition> CalculateIngredientAsync(
      RecipeIngredient ingredient,
      string apiKey,
      CancellationToken cancellationToken)
    {
      var food = await SearchFoodAsync(BuildIngredientQuery(ingredient), apiKey, cancellationToken);
      if (food == null && !string.IsNullOrWhiteSpace(ingredient.Name))
        food = await SearchFoodAsync(ingredient.Name, apiKey, cancellationToken);
      if (food == null) return null;

      var details = await GetFoodDetailsAsync(food.FdcId, apiKey, cancellationToken);
      if (details == null) return null;

      var grams = ResolveGrams(ingredient, details);
      if (!grams.HasValue || grams <= 0) return null;

      var scale = grams.Value / 100m;
      return new IngredientNutrition
      {
        Calories = GetNutrientValue(details, "208", "Energy") * scale,
        ProteinGrams = GetNutrientValue(details, "203", "Protein") * scale,
        CarbsGrams = GetNutrientValue(details, "205", "Carbohydrate, by difference") * scale,
        FatGrams = GetNutrientValue(details, "204", "Total lipid (fat)") * scale
      };
    }

    private async Task<FoodSearchItem> SearchFoodAsync(string query, string apiKey, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(query)) return null;

      var url = $"foods/search?query={Uri.EscapeDataString(query)}&pageSize=1&api_key={Uri.EscapeDataString(apiKey)}";
      var response = await GetJsonAsync<FoodSearchResponse>(url, cancellationToken);
      return response?.Foods?.FirstOrDefault();
    }

    private async Task<FoodDetails> GetFoodDetailsAsync(int fdcId, string apiKey, CancellationToken cancellationToken)
    {
      var url = $"food/{fdcId}?api_key={Uri.EscapeDataString(apiKey)}";
      return await GetJsonAsync<FoodDetails>(url, cancellationToken);
    }

    private async Task<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
      var client = _httpClientFactory.CreateClient("FoodDataCentral");
      using var response = await client.GetAsync(url, cancellationToken);
      if (!response.IsSuccessStatusCode) return default;

      await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
      return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    private static string BuildIngredientQuery(RecipeIngredient ingredient)
    {
      var parts = new List<string>();
      if (ingredient.Quantity.HasValue) parts.Add($"{ingredient.Quantity.Value:0.###}");
      if (!string.IsNullOrWhiteSpace(ingredient.Unit)) parts.Add(ingredient.Unit);
      parts.Add(ingredient.Name);
      return string.Join(" ", parts);
    }

    private static decimal? ResolveGrams(RecipeIngredient ingredient, FoodDetails food)
    {
      if (!ingredient.Quantity.HasValue) return null;
      var quantity = ingredient.Quantity.Value;
      var unit = ingredient.Unit?.Trim();

      if (string.IsNullOrWhiteSpace(unit)) return null;
      if (GramConversions.TryGetValue(unit, out var gramsPerUnit))
        return quantity * gramsPerUnit;

      var portion = FindMatchingPortion(food.FoodPortions, unit);
      if (portion?.GramWeight == null) return null;

      var portionAmount = portion.Amount.GetValueOrDefault(1);
      if (portionAmount <= 0) portionAmount = 1;

      return quantity / portionAmount * portion.GramWeight.Value;
    }

    private static FoodPortion FindMatchingPortion(IEnumerable<FoodPortion> portions, string unit)
    {
      var normalizedUnit = NormalizeUnit(unit);
      return portions?.FirstOrDefault(p =>
        UnitMatches(p.MeasureUnit?.Name, normalizedUnit)
        || UnitMatches(p.MeasureUnit?.Abbreviation, normalizedUnit)
        || UnitMatches(p.Modifier, normalizedUnit)
        || UnitMatches(p.PortionDescription, normalizedUnit));
    }

    private static bool UnitMatches(string value, string normalizedUnit)
    {
      if (string.IsNullOrWhiteSpace(value)) return false;
      return NormalizeUnit(value).Contains(normalizedUnit);
    }

    private static string NormalizeUnit(string unit)
    {
      var normalized = unit.Trim().Trim('.').ToLowerInvariant();
      return normalized.EndsWith("s") ? normalized[..^1] : normalized;
    }

    private static decimal GetNutrientValue(FoodDetails food, string nutrientNumber, string nutrientName)
    {
      var nutrient = food.FoodNutrients?.FirstOrDefault(n =>
        string.Equals(n.NutrientNumber, nutrientNumber, StringComparison.OrdinalIgnoreCase)
        || string.Equals(n.Nutrient?.Number, nutrientNumber, StringComparison.OrdinalIgnoreCase)
        || string.Equals(n.NutrientName, nutrientName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(n.Nutrient?.Name, nutrientName, StringComparison.OrdinalIgnoreCase));

      return nutrient?.Value ?? nutrient?.Amount ?? 0;
    }

    private string GetApiKey() =>
      string.IsNullOrWhiteSpace(_settings.ApiKey)
        ? Environment.GetEnvironmentVariable("FOODDATA_CENTRAL_API_KEY")
        : _settings.ApiKey;

    private static decimal Round(decimal value) => Math.Round(value, 2);

    private class IngredientNutrition
    {
      public decimal Calories { get; set; }
      public decimal ProteinGrams { get; set; }
      public decimal CarbsGrams { get; set; }
      public decimal FatGrams { get; set; }
    }

    private class FoodSearchResponse
    {
      public List<FoodSearchItem> Foods { get; set; } = new();
    }

    private class FoodSearchItem
    {
      public int FdcId { get; set; }
    }

    private class FoodDetails
    {
      public List<FoodNutrient> FoodNutrients { get; set; } = new();
      public List<FoodPortion> FoodPortions { get; set; } = new();
    }

    private class FoodNutrient
    {
      public string NutrientName { get; set; }
      public string NutrientNumber { get; set; }
      public decimal? Value { get; set; }
      public decimal? Amount { get; set; }
      public NutrientDefinition Nutrient { get; set; }
    }

    private class NutrientDefinition
    {
      public string Number { get; set; }
      public string Name { get; set; }
    }

    private class FoodPortion
    {
      public decimal? Amount { get; set; }
      public decimal? GramWeight { get; set; }
      public string Modifier { get; set; }
      public string PortionDescription { get; set; }
      public MeasureUnit MeasureUnit { get; set; }
    }

    private class MeasureUnit
    {
      public string Name { get; set; }
      public string Abbreviation { get; set; }
    }
  }
}
