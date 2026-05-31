using System.Text.Json;
using System.Text.RegularExpressions;
using Application.DTOs.Nutrition;
using Application.Interfaces;
using Domain;
using Microsoft.Extensions.Options;

namespace Infrastructure.Nutrition
{
  public class UsdaNutritionService : INutritionService
  {
    private const int SearchPageSize = 10;
    private const int MaxDetailAttemptsPerSearch = 5;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
      var triedFdcIds = new HashSet<int>();
      var branded = ingredient.Branded;

      var expandedQuery = BuildIngredientSearchQuery(ingredient)?.Trim();
      if (!string.IsNullOrWhiteSpace(expandedQuery))
      {
        var candidates = await SearchRankedFoodsAsync(expandedQuery, branded, apiKey, cancellationToken);
        // Some fdcIds for food items are obsolete and won't be found with /food/{fdcId}
        // so we need to try to calculate from possible candidates
        var nutrition = await TryCalculateFromCandidatesAsync(
          ingredient, candidates, triedFdcIds, apiKey, cancellationToken);
        if (nutrition != null) return nutrition;
      }

      if (branded) return null;

      var unexpandedQuery = BuildSearchQueryUnexpanded(ingredient)?.Trim();
      if (!string.IsNullOrWhiteSpace(unexpandedQuery)
          && !string.Equals(unexpandedQuery, expandedQuery, StringComparison.OrdinalIgnoreCase))
      {
        var candidates = await SearchRankedFoodsAsync(unexpandedQuery, branded, apiKey, cancellationToken);
        var nutrition = await TryCalculateFromCandidatesAsync(
          ingredient, candidates, triedFdcIds, apiKey, cancellationToken);
        if (nutrition != null) return nutrition;
      }

      return null;
    }

    private async Task<IngredientNutrition> TryCalculateFromCandidatesAsync(
      RecipeIngredient ingredient,
      IReadOnlyList<FoodSearchItem> candidates,
      HashSet<int> triedFdcIds,
      string apiKey,
      CancellationToken cancellationToken)
    {
      if (candidates == null || candidates.Count == 0) return null;

      var attempts = 0;
      foreach (var candidate in candidates)
      {
        if (attempts >= MaxDetailAttemptsPerSearch) break;
        if (!triedFdcIds.Add(candidate.FdcId)) continue;
        attempts++;

        var details = await GetFoodDetailsAsync(candidate.FdcId, apiKey, cancellationToken);
        if (details == null) continue;

        var grams = ResolveGrams(ingredient, details);
        if (!grams.HasValue || grams <= 0) continue;

        if (ingredient.Branded || IsBrandedFood(details))
          return CalculateBrandedNutrition(details, grams.Value);

        var scale = grams.Value / 100m;
        return new IngredientNutrition
        {
          Calories = NonNegative(GetEnergyKcalPer100g(details) * scale),
          ProteinGrams = NonNegative(GetNutrientValue(details, "203", "Protein") * scale),
          CarbsGrams = NonNegative(GetNutrientValue(details, "205", "Carbohydrate, by difference") * scale),
          FatGrams = NonNegative(GetNutrientValue(details, "204", "Total lipid (fat)") * scale)
        };
      }

      return null;
    }

    private async Task<IReadOnlyList<FoodSearchItem>> SearchRankedFoodsAsync(
      string query,
      bool branded,
      string apiKey,
      CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(query)) return Array.Empty<FoodSearchItem>();

      var dataType = branded ? "Branded" : "Foundation,SR Legacy";
      var url =
        $"foods/search?query={Uri.EscapeDataString(query)}&pageSize={SearchPageSize}&dataType={Uri.EscapeDataString(dataType)}&api_key={Uri.EscapeDataString(apiKey)}";
      var response = await GetJsonAsync<FoodSearchResponse>(url, cancellationToken);

      var foods = response?.Foods;
      if (foods == null || foods.Count == 0) return Array.Empty<FoodSearchItem>();

      return foods
        .Select(f => (Item: f, Score: ScoreSearchCandidate(f, query, branded)))
        .OrderByDescending(x => x.Score)
        .Select(x => x.Item)
        .ToList();
    }

    private static decimal ScoreSearchCandidate(FoodSearchItem item, string searchQuery, bool brandedSearch)
    {
      var score = (decimal)(item.Score ?? 0);
      var desc = item.Description ?? string.Empty;
      var descLower = desc.ToLowerInvariant();
      var dataType = item.DataType ?? string.Empty;

      if (string.Equals(dataType, "Foundation", StringComparison.OrdinalIgnoreCase))
      {
        score += brandedSearch ? 0 : 80;
        if (!brandedSearch && item.PublishedDate.HasValue && item.PublishedDate.Value < DateTime.UtcNow.AddYears(-3))
          score -= 60;
      }
      else if (string.Equals(dataType, "SR Legacy", StringComparison.OrdinalIgnoreCase))
        score += brandedSearch ? 0 : 40;
      else if (string.Equals(dataType, "Branded", StringComparison.OrdinalIgnoreCase))
        score += brandedSearch ? 80 : 0;
      else if (string.Equals(dataType, "Survey (FNDDS)", StringComparison.OrdinalIgnoreCase))
        score += brandedSearch ? 0 : 10;

      foreach (var token in TokenizeForMatch(searchQuery))
      {
        if (token.Length < 2) continue;
        if (descLower.Contains(token)) score += 12;
      }

      if (searchQuery.Contains("raw", StringComparison.OrdinalIgnoreCase) && descLower.Contains("raw"))
        score += brandedSearch ? 0 : 15;
      if (searchQuery.Contains("uncooked", StringComparison.OrdinalIgnoreCase) && descLower.Contains("uncook"))
        score += brandedSearch ? 0 : 15;

      if (descLower.Contains("baby food") || descLower.Contains("babyfood")) score -= 200;
      if (descLower.Contains("candy") || descLower.Contains("snack bar")) score -= 120;
      if (descLower.Contains("fast food") || descLower.Contains("restaurant")) score -= 80;

      if (!brandedSearch
          && searchQuery.Contains("raw", StringComparison.OrdinalIgnoreCase)
          && (descLower.Contains("cooked") || descLower.Contains("roasted") || descLower.Contains("fried")))
        score -= 25;

      return score;
    }

    private static IEnumerable<string> TokenizeForMatch(string text)
    {
      return Regex.Split(text.ToLowerInvariant(), @"\W+")
        .Where(s => s.Length > 1)
        .Distinct();
    }

    private static string ExpandFoodName(string name)
    {
      if (string.IsNullOrWhiteSpace(name)) return name;
      var trimmed = name.Trim();
      var lower = trimmed.ToLowerInvariant();
      if (IngredientSearchExpansionMap.Entries.TryGetValue(lower, out var mapped)) return mapped;

      var firstWord = Regex.Split(lower, @"\W+").FirstOrDefault(s => s.Length > 0);
      if (firstWord != null && IngredientSearchExpansionMap.Entries.TryGetValue(firstWord, out mapped)) return mapped;

      if (lower.EndsWith(" raw", StringComparison.Ordinal) || lower.EndsWith(" uncooked", StringComparison.Ordinal))
        return lower;

      return $"{lower} raw";
    }

    private static string BuildSearchQueryUnexpanded(RecipeIngredient ingredient) =>
      string.IsNullOrWhiteSpace(ingredient.Name) ? null : ingredient.Name.Trim().ToLowerInvariant();

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

    private static string BuildIngredientSearchQuery(RecipeIngredient ingredient)
    {
      if (string.IsNullOrWhiteSpace(ingredient.Name)) return null;
      if (ingredient.Branded) return ingredient.Name.Trim();
      return ExpandFoodName(ingredient.Name);
    }

    private static decimal? ResolveGrams(RecipeIngredient ingredient, FoodDetails food)
    {
      if (!ingredient.Quantity.HasValue) return null;

      if (ingredient.Branded || IsBrandedFood(food))
      {
        var brandedGrams = ResolveBrandedGrams(ingredient, food);
        if (brandedGrams.HasValue) return brandedGrams;
        if (ingredient.Branded) return null;
      }

      var quantity = ingredient.Quantity.Value;
      var unit = ingredient.Unit?.Trim();
      var size = ingredient.Size?.Trim();

      if (!string.IsNullOrWhiteSpace(unit) && TryGetGramsPerUnit(unit, out var gramsPerUnit))
        return quantity * gramsPerUnit;

      var portion = FindBestPortion(food.FoodPortions, unit, size);
      if (portion?.GramWeight == null) return null;

      var portionAmount = portion.Amount.GetValueOrDefault(1);
      if (portionAmount <= 0) portionAmount = 1;

      return (quantity / portionAmount) * portion.GramWeight.Value;
    }

    private static bool IsBrandedFood(FoodDetails food) =>
      string.Equals(food.DataType, "Branded", StringComparison.OrdinalIgnoreCase)
      || string.Equals(food.FoodClass, "Branded", StringComparison.OrdinalIgnoreCase);

    private static decimal? ResolveBrandedGrams(RecipeIngredient ingredient, FoodDetails food)
    {
      var quantity = ingredient.Quantity!.Value;
      var unit = ingredient.Unit?.Trim();
      var canonicalUnit = string.IsNullOrWhiteSpace(unit) ? null : CanonicalizeUnit(unit);

      if (canonicalUnit is "can" or "package")
      {
        var packageGrams = ParsePackageWeightGrams(food.PackageWeight);
        if (packageGrams.HasValue) return quantity * packageGrams.Value;
      }

      if (canonicalUnit is "g")
        return quantity;

      if (food.ServingSize is > 0
          && string.Equals(food.ServingSizeUnit, "g", StringComparison.OrdinalIgnoreCase))
      {
        if (canonicalUnit is "serving")
          return quantity * food.ServingSize.Value;

        if (!string.IsNullOrWhiteSpace(unit)
            && TryGetGramsFromHouseholdServing(quantity, unit, food, out var householdGrams))
          return householdGrams;

        if (canonicalUnit is "oz")
          return quantity * 28.3495m;
      }

      if (canonicalUnit == null
          && food.ServingSize is > 0
          && string.Equals(food.ServingSizeUnit, "g", StringComparison.OrdinalIgnoreCase))
        return quantity * food.ServingSize.Value;

      if (!string.IsNullOrWhiteSpace(unit) && TryGetGramsPerUnit(unit, out var gramsPerUnit))
        return quantity * gramsPerUnit;

      return null;
    }

    private static bool TryGetGramsFromHouseholdServing(
      decimal quantity,
      string unit,
      FoodDetails food,
      out decimal grams)
    {
      grams = 0;
      if (!TryParseHouseholdServing(food.HouseholdServingFullText, out var householdAmount, out var householdUnit))
        return false;
      if (!UnitsAreEquivalent(unit, householdUnit)) return false;

      grams = (quantity / householdAmount) * food.ServingSize!.Value;
      return grams > 0;
    }

    private static bool TryParseHouseholdServing(string text, out decimal amount, out string unitToken)
    {
      amount = 0;
      unitToken = null;
      if (string.IsNullOrWhiteSpace(text)) return false;

      var match = Regex.Match(text.Trim(), @"^(\d+(?:\.\d+)?)\s*(.+)$");
      if (!match.Success) return false;
      if (!decimal.TryParse(match.Groups[1].Value, out amount) || amount <= 0) return false;

      unitToken = match.Groups[2].Value.Trim();
      return !string.IsNullOrWhiteSpace(unitToken);
    }

    private static bool UnitsAreEquivalent(string unitA, string unitB)
    {
      var canonicalA = CanonicalizeUnit(unitA);
      var canonicalB = CanonicalizeUnit(unitB);
      return canonicalA != null
        && canonicalB != null
        && string.Equals(canonicalA, canonicalB, StringComparison.OrdinalIgnoreCase);
    }

    private static string CanonicalizeUnit(string unit)
    {
      if (string.IsNullOrWhiteSpace(unit)) return null;

      var normalized = NormalizeUnit(unit);
      return normalized switch
      {
        "g" or "gram" => "g",
        "tbsp" or "tbs" or "tbl" or "tablespoon" => "tbsp",
        "tsp" or "teaspoon" => "tsp",
        "oz" or "ounce" or "onz" => "oz",
        "cup" or "c" => "cup",
        "floz" or "fluid ounce" => "floz",
        "ml" or "milliliter" or "millilitre" or "cc" => "ml",
        "l" or "liter" or "litre" => "l",
        "serving" or "portion" => "serving",
        "can" => "can",
        "package" or "pkg" => "package",
        _ => normalized
      };
    }

    private static decimal? ParsePackageWeightGrams(string packageWeight)
    {
      if (string.IsNullOrWhiteSpace(packageWeight)) return null;

      var gramMatches = Regex.Matches(packageWeight, @"(\d+(?:\.\d+)?)\s*g\b", RegexOptions.IgnoreCase);
      if (gramMatches.Count > 0
          && decimal.TryParse(gramMatches[gramMatches.Count - 1].Groups[1].Value, out var grams))
        return grams;

      var ozMatch = Regex.Match(packageWeight, @"(\d+(?:\.\d+)?)\s*oz\b", RegexOptions.IgnoreCase);
      if (ozMatch.Success && decimal.TryParse(ozMatch.Groups[1].Value, out var oz))
        return oz * 28.3495m;

      return null;
    }

    private static IngredientNutrition CalculateBrandedNutrition(FoodDetails food, decimal grams)
    {
      if (food.LabelNutrients != null
          && food.ServingSize is > 0
          && string.Equals(food.ServingSizeUnit, "g", StringComparison.OrdinalIgnoreCase))
      {
        var servings = grams / food.ServingSize.Value;
        return new IngredientNutrition
        {
          Calories = NonNegative((food.LabelNutrients.Calories?.Value ?? 0) * servings),
          ProteinGrams = NonNegative((food.LabelNutrients.Protein?.Value ?? 0) * servings),
          CarbsGrams = NonNegative((food.LabelNutrients.Carbohydrates?.Value ?? 0) * servings),
          FatGrams = NonNegative((food.LabelNutrients.Fat?.Value ?? 0) * servings)
        };
      }

      var scale = grams / 100m;
      return new IngredientNutrition
      {
        Calories = NonNegative(GetEnergyKcalPer100g(food) * scale),
        ProteinGrams = NonNegative(GetNutrientValue(food, "203", "Protein") * scale),
        CarbsGrams = NonNegative(GetNutrientValue(food, "205", "Carbohydrate, by difference") * scale),
        FatGrams = NonNegative(GetNutrientValue(food, "204", "Total lipid (fat)") * scale)
      };
    }

    private static FoodPortion FindBestPortion(IEnumerable<FoodPortion> portions, string unit, string size)
    {
      if (portions == null) return null;

      var hasUnit = !string.IsNullOrWhiteSpace(unit);
      var hasSize = !string.IsNullOrWhiteSpace(size);

      if (hasUnit && hasSize)
      {
        var combined = FindMatchingPortion(portions, unit, size);
        if (combined != null) return combined;
      }

      if (hasSize)
      {
        var bySize = FindMatchingPortionBySize(portions, size);
        if (bySize != null) return bySize;
      }

      if (hasUnit)
        return FindMatchingPortion(portions, unit);

      return null;
    }

    private static FoodPortion FindMatchingPortionBySize(IEnumerable<FoodPortion> portions, string size)
    {
      var normalizedSize = NormalizeUnit(size);
      return portions?
        .Where(p => !string.IsNullOrWhiteSpace(p.Modifier) && PortionModifierMatchesSize(p.Modifier, normalizedSize))
        .OrderByDescending(p => ScoreSizePortionMatch(p.Modifier, normalizedSize))
        .FirstOrDefault();
    }

    private static bool PortionModifierMatchesSize(string modifier, string normalizedSize)
    {
      var normalizedModifier = modifier.Trim().ToLowerInvariant();
      if (normalizedModifier == normalizedSize) return true;
      if (normalizedModifier.StartsWith(normalizedSize + " ", StringComparison.Ordinal)) return true;
      if (normalizedModifier.StartsWith(normalizedSize + "(", StringComparison.Ordinal)) return true;
      return false;
    }

    private static int ScoreSizePortionMatch(string modifier, string normalizedSize)
    {
      var normalizedModifier = modifier.Trim().ToLowerInvariant();
      if (normalizedModifier == normalizedSize) return 100;
      if (normalizedModifier.StartsWith(normalizedSize + " ", StringComparison.Ordinal)) return 80;
      if (normalizedModifier.StartsWith(normalizedSize + "(", StringComparison.Ordinal)) return 70;
      return 0;
    }

    private static FoodPortion FindMatchingPortion(IEnumerable<FoodPortion> portions, string unit, string size = null)
    {
      var normalizedUnit = NormalizeUnit(unit);
      var normalizedSize = string.IsNullOrWhiteSpace(size) ? null : NormalizeUnit(size);

      return portions?.FirstOrDefault(p =>
      {
        if (normalizedSize != null
            && !string.IsNullOrWhiteSpace(p.Modifier)
            && !PortionModifierMatchesSize(p.Modifier, normalizedSize))
          return false;

        return UnitMatches(p.MeasureUnit?.Name, normalizedUnit)
          || UnitMatches(p.MeasureUnit?.Abbreviation, normalizedUnit)
          || UnitMatches(p.Modifier, normalizedUnit)
          || UnitMatches(p.PortionDescription, normalizedUnit);
      });
    }

    private static bool TryGetGramsPerUnit(string unit, out decimal gramsPerUnit)
    {
      var trimmed = unit.Trim();
      if (IngredientGramConversions.ByUnit.TryGetValue(trimmed, out gramsPerUnit))
        return true;
      return IngredientGramConversions.ByUnit.TryGetValue(NormalizeUnit(trimmed), out gramsPerUnit);
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

    private static decimal GetEnergyKcalPer100g(FoodDetails food)
    {
      if (TryGetNutrientValueByNumber(food, "958", out var kcal)) return kcal;
      if (TryGetNutrientValueByNumber(food, "957", out kcal)) return kcal;
      return GetNutrientValue(food, "208", "Energy");
    }

    private static bool TryGetNutrientValueByNumber(FoodDetails food, string nutrientNumber, out decimal value)
    {
      value = 0;
      var nutrient = food.FoodNutrients?.FirstOrDefault(n =>
        string.Equals(n.NutrientNumber, nutrientNumber, StringComparison.OrdinalIgnoreCase)
        || string.Equals(n.Nutrient?.Number, nutrientNumber, StringComparison.OrdinalIgnoreCase));
      if (nutrient == null) return false;
      var amount = nutrient.Value ?? nutrient.Amount;
      if (!amount.HasValue) return false;
      value = amount.Value;
      return true;
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

    private static decimal NonNegative(decimal value) => value < 0 ? 0 : value;

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
      public string Description { get; set; }
      public string DataType { get; set; }
      public double? Score { get; set; }
      public DateTime? PublishedDate { get; set; }
    }

    private class FoodDetails
    {
      public string DataType { get; set; }
      public string FoodClass { get; set; }
      public decimal? ServingSize { get; set; }
      public string ServingSizeUnit { get; set; }
      public string PackageWeight { get; set; }
      public string HouseholdServingFullText { get; set; }
      public LabelNutrients LabelNutrients { get; set; }
      public List<FoodNutrient> FoodNutrients { get; set; } = new();
      public List<FoodPortion> FoodPortions { get; set; } = new();
    }

    private class LabelNutrients
    {
      public LabelNutrientValue Calories { get; set; }
      public LabelNutrientValue Protein { get; set; }
      public LabelNutrientValue Carbohydrates { get; set; }
      public LabelNutrientValue Fat { get; set; }
    }

    private class LabelNutrientValue
    {
      public decimal? Value { get; set; }
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
