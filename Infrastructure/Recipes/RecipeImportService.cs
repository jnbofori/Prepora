using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Application.DTOs.Recipes;
using Application.Interfaces;

namespace Infrastructure.Recipes
{
  public class RecipeImportService : IRecipeImportService
  {
    private const int MaxResponseBytes = 2_000_000;
    private readonly IHttpClientFactory _httpClientFactory;

    public RecipeImportService(IHttpClientFactory httpClientFactory)
    {
      _httpClientFactory = httpClientFactory;
    }

    public async Task<RecipeImportPreviewDto> ImportPreviewAsync(string url, CancellationToken cancellationToken = default)
    {
      var preview = new RecipeImportPreviewDto { Recipe = new RecipeUpsertDto() };

      if (string.IsNullOrWhiteSpace(url))
      {
        preview.Error = "URL is required";
        return preview;
      }

      if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
      {
        preview.Error = "Only absolute https URLs are allowed";
        return preview;
      }

      if (IsBlockedHost(uri))
      {
        preview.Error = "This host is not allowed";
        return preview;
      }

      var client = _httpClientFactory.CreateClient("RecipeImport");
      client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PreporaRecipeBot/1.0");

      string html;
      try
      {
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
          preview.Error = $"Could not fetch page: {(int)response.StatusCode}";
          return preview;
        }

        var len = response.Content.Headers.ContentLength;
        if (len.HasValue && len.Value > MaxResponseBytes)
        {
          preview.Error = "Response is too large";
          return preview;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var ms = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
          if (ms.Length + read > MaxResponseBytes)
          {
            preview.Error = "Response is too large";
            return preview;
          }
          await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        html = System.Text.Encoding.UTF8.GetString(ms.ToArray());
      }
      catch (Exception ex)
      {
        preview.Error = $"Fetch failed: {ex.Message}";
        return preview;
      }

      try
      {
        ParseJsonLdBlocks(html, preview);
      }
      catch (Exception ex)
      {
        preview.Warnings.Add($"Parse error: {ex.Message}");
      }

      if (!preview.Parsed)
      {
        if (string.IsNullOrEmpty(preview.Error))
          preview.Error = "No Recipe structured data (JSON-LD) found on this page";
        return preview;
      }

      return preview;
    }

    private static bool IsBlockedHost(Uri uri)
    {
      var host = uri.Host;
      if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        return true;
      if (IPAddress.TryParse(host, out var ip))
      {
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
          var bytes = ip.GetAddressBytes();
          if (bytes[0] == 10) return true;
          if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
          if (bytes[0] == 192 && bytes[1] == 168) return true;
          if (bytes[0] == 127) return true;
        }
      }

      return false;
    }

    private static void ParseJsonLdBlocks(string html, RecipeImportPreviewDto preview)
    {
      var regex = new Regex(
        @"<script[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>([\s\S]*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

      foreach (Match m in regex.Matches(html))
      {
        var json = m.Groups[1].Value.Trim();
        if (string.IsNullOrEmpty(json)) continue;

        JsonElement root;
        try
        {
          using var doc = JsonDocument.Parse(json);
          root = doc.RootElement.Clone();
        }
        catch
        {
          preview.Warnings.Add("Skipped invalid JSON-LD block");
          continue;
        }

        TryExtractRecipe(root, preview);
        if (preview.Parsed) return;
      }
    }

    private static void TryExtractRecipe(JsonElement root, RecipeImportPreviewDto preview)
    {
      if (root.ValueKind == JsonValueKind.Array)
        foreach (var el in root.EnumerateArray())
          TryExtractRecipe(el, preview);

      if (preview.Parsed) return;

      if (root.ValueKind == JsonValueKind.Object)
      {
        if (root.TryGetProperty("@graph", out var graph))
        {
          if (graph.ValueKind == JsonValueKind.Array)
          {
            foreach (var el in graph.EnumerateArray())
            {
              TryExtractRecipe(el, preview);
              if (preview.Parsed) return;
            }
          }
          else
          {
            TryExtractRecipe(graph, preview);
            if (preview.Parsed) return;
          }
        }

        if (IsRecipeType(root))
        {
          MapRecipe(root, preview);
          return;
        }
      }
    }

    private static bool IsRecipeType(JsonElement obj)
    {
      if (!obj.TryGetProperty("@type", out var typeEl)) return false;
      if (typeEl.ValueKind == JsonValueKind.String)
        return typeEl.GetString() == "Recipe";
      if (typeEl.ValueKind == JsonValueKind.Array)
        return typeEl.EnumerateArray().Any(t => t.ValueKind == JsonValueKind.String && t.GetString() == "Recipe");
      return false;
    }

    private static void MapRecipe(JsonElement recipe, RecipeImportPreviewDto preview)
    {
      preview.Parsed = true;
      var dto = preview.Recipe;
      dto.Title = recipe.TryGetProperty("name", out var name) ? name.GetString()?.Trim() : null;
      dto.Description = recipe.TryGetProperty("description", out var desc) ? desc.GetString()?.Trim() : null;

      if (recipe.TryGetProperty("prepTime", out var prep))
        dto.PrepMinutes = ParseIsoDurationMinutes(prep.GetString());
      if (recipe.TryGetProperty("cookTime", out var cook))
        dto.CookMinutes = ParseIsoDurationMinutes(cook.GetString());
      if (recipe.TryGetProperty("totalTime", out var total))
        dto.TotalMinutes = ParseIsoDurationMinutes(total.GetString());

      if (recipe.TryGetProperty("recipeYield", out var yield))
        dto.Servings = ParseYield(yield);

      if (dto.Servings <= 0) dto.Servings = 1;

      var order = 0;
      if (recipe.TryGetProperty("recipeIngredient", out var ings))
      {
        foreach (var ing in EnumerateJsonArrayOrSingle(ings))
        {
          var text = ing.ValueKind == JsonValueKind.String ? ing.GetString() : ing.GetRawText();
          if (string.IsNullOrWhiteSpace(text)) continue;
          dto.Ingredients.Add(new RecipeUpsertIngredientDto
          {
            SortOrder = order++,
            Name = text.Trim(),
            Quantity = null,
            Unit = null,
            Note = null
          });
        }
      }

      order = 0;
      if (recipe.TryGetProperty("recipeInstructions", out var instr))
        MapInstructions(instr, dto, ref order);

      if (recipe.TryGetProperty("keywords", out var kw))
      {
        var s = kw.ValueKind == JsonValueKind.String ? kw.GetString() : null;
        if (!string.IsNullOrEmpty(s))
          foreach (var part in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            dto.Tags.Add(part);
      }

      if (string.IsNullOrWhiteSpace(dto.Title))
      {
        preview.Warnings.Add("Recipe name was missing; using placeholder");
        dto.Title = "Imported recipe";
      }
    }

    private static IEnumerable<JsonElement> EnumerateJsonArrayOrSingle(JsonElement el)
    {
      if (el.ValueKind == JsonValueKind.Array)
        foreach (var x in el.EnumerateArray()) yield return x;
      else
        yield return el;
    }

    private static void MapInstructions(JsonElement instr, RecipeUpsertDto dto, ref int order)
    {
      if (instr.ValueKind == JsonValueKind.String)
      {
        foreach (var line in instr.GetString().Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
          var t = line.Trim();
          if (t.Length == 0) continue;
          dto.Steps.Add(new RecipeUpsertStepDto { SortOrder = order++, Instruction = t });
        }
        return;
      }

      if (instr.ValueKind == JsonValueKind.Array)
      {
        foreach (var item in instr.EnumerateArray())
        {
          if (item.ValueKind == JsonValueKind.String)
          {
            dto.Steps.Add(new RecipeUpsertStepDto { SortOrder = order++, Instruction = item.GetString()?.Trim() });
            continue;
          }

          if (item.ValueKind == JsonValueKind.Object)
          {
            if (JsonLdTypeContains(item, "HowToStep") && item.TryGetProperty("text", out var text))
              dto.Steps.Add(new RecipeUpsertStepDto { SortOrder = order++, Instruction = text.GetString()?.Trim() });
            else if (JsonLdTypeContains(item, "HowToSection") && item.TryGetProperty("itemListElement", out var list))
              MapInstructions(list, dto, ref order);
            else if (item.TryGetProperty("itemListElement", out var list2))
              MapInstructions(list2, dto, ref order);
          }
        }
      }
    }

    private static int? ParseIsoDurationMinutes(string iso)
    {
      if (string.IsNullOrWhiteSpace(iso)) return null;
      try
      {
        var ts = XmlConvert.ToTimeSpan(iso);
        return (int)Math.Round(ts.TotalMinutes);
      }
      catch
      {
        return null;
      }
    }

    private static bool JsonLdTypeContains(JsonElement obj, string typeName)
    {
      if (!obj.TryGetProperty("@type", out var t)) return false;
      if (t.ValueKind == JsonValueKind.String)
        return string.Equals(t.GetString(), typeName, StringComparison.OrdinalIgnoreCase);
      if (t.ValueKind == JsonValueKind.Array)
        return t.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String &&
          string.Equals(x.GetString(), typeName, StringComparison.OrdinalIgnoreCase));
      return false;
    }

    private static decimal ParseYield(JsonElement yield)
    {
      if (yield.ValueKind == JsonValueKind.Number)
        return yield.GetDecimal();
      if (yield.ValueKind == JsonValueKind.String)
      {
        var s = yield.GetString();
        if (decimal.TryParse(s, out var d)) return d;
        var m = Regex.Match(s ?? "", @"(\d+(\.\d+)?)");
        if (m.Success && decimal.TryParse(m.Groups[1].Value, out d)) return d;
      }

      if (yield.ValueKind == JsonValueKind.Array && yield.GetArrayLength() > 0)
        return ParseYield(yield[0]);

      return 1;
    }
  }
}
