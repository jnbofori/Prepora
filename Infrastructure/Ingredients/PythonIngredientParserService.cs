using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DTOs.Recipes;
using Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Python.Runtime;

namespace Infrastructure.Ingredients
{
  public class PythonIngredientParserService : IIngredientParserService, IDisposable
  {
    private static readonly object InitLock = new();
    private static bool _engineReady;
    private static string _scriptDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      PropertyNameCaseInsensitive = true,
      NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly IngredientParserSettings _settings;

    public PythonIngredientParserService(
      IOptions<IngredientParserSettings> settings,
      IHostApplicationLifetime lifetime)
    {
      _settings = settings.Value;
      _scriptDirectory = Path.Combine(AppContext.BaseDirectory, "Ingredients", "Python");
      EnsurePythonInitialized();

      lifetime.ApplicationStopping.Register(() =>
      {
        if (PythonEngine.IsInitialized)
          PythonEngine.Shutdown();
      });
    }

    public Task<ParseIngredientsResultDto> ParseAsync(
      IReadOnlyList<string> lines,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      // Console.WriteLine("Ensuring Python is initialized");
      // EnsurePythonInitialized();
      

      string responseJson;
      Console.WriteLine($"Is ready?!! {_engineReady}");

      using (Py.GIL())
      {
        Console.WriteLine("Importing sys");
        dynamic sys = Py.Import("sys");
        sys.path.insert(0, _scriptDirectory);

        Console.WriteLine("Importing ingredient_parser_bridge");
        dynamic module = Py.Import("ingredient_parser_bridge");
        responseJson = module.parse_ingredients_json(JsonSerializer.Serialize(lines)).ToString();
        Console.WriteLine("Response JSON: ", responseJson);
      }

      var payload = JsonSerializer.Deserialize<PythonParseResponse>(responseJson, JsonOptions)
        ?? new PythonParseResponse();

      var result = new ParseIngredientsResultDto();
      var sortOrder = 0;
      foreach (var item in payload.Ingredients ?? new List<PythonParsedIngredient>())
      {
        result.Ingredients.Add(new ParsedIngredientLineDto
        {
          SortOrder = sortOrder++,
          Name = item.Name,
          Quantity = item.Quantity,
          Unit = item.Unit,
          Size = item.Size,
          Note = item.Note,
          Branded = item.Branded
        });
      }

      foreach (var error in payload.Errors ?? new List<PythonParseError>())
        result.Warnings.Add($"Could not parse line {error.Index + 1} ({error.Line}): {error.Error}");

      return Task.FromResult(result);
    }

    public void Dispose()
    {
      // Shutdown is registered on application stopping.
    }

    private void EnsurePythonInitialized()
    {
      Console.WriteLine($"Is ready? {_engineReady}");
      if (_engineReady) return;

      lock (InitLock)
      {
        if (_engineReady) return;

        var pythonDll = ResolvePythonDll();
        if (!string.IsNullOrWhiteSpace(pythonDll))
          Runtime.PythonDLL = pythonDll;

        var pythonHome = ResolvePythonHome(pythonDll);
        if (!string.IsNullOrWhiteSpace(pythonHome))
          PythonEngine.PythonHome = pythonHome;

        PythonEngine.Initialize();
        // This is needed to allow multithreading in Python.NET
        PythonEngine.BeginAllowThreads();
        _engineReady = true;
      }
    }

    private string ResolvePythonDll()
    {
      if (!string.IsNullOrWhiteSpace(_settings.PythonDll))
        return _settings.PythonDll;

      var fromEnv = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
      if (!string.IsNullOrWhiteSpace(fromEnv))
        return fromEnv;

      if (OperatingSystem.IsWindows())
      {
        foreach (var version in new[] { "313", "312", "311", "310" })
        {
          var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Python", $"Python{version}", "python{version}.dll");
          if (File.Exists(path)) return path;
        }
      }
      else if (OperatingSystem.IsMacOS())
      {
        foreach (var version in new[] { "3.11", "3.13", "3.12", "3.10" })
        {
          var path = $"/Library/Frameworks/Python.framework/Versions/{version}/Python";
          if (File.Exists(path)) return path;
        }
      }
      else if (OperatingSystem.IsLinux())
      {
        foreach (var version in new[] { "3.13", "3.12", "3.11", "3.10" })
        {
          var path = $"/usr/lib/x86_64-linux-gnu/libpython{version}.so";
          if (File.Exists(path)) return path;
        }
      }

      throw new InvalidOperationException(
        "Python shared library not found. Install Python 3, pip install -r Infrastructure/Ingredients/Python/requirements.txt, "
        + "and set IngredientParser:PythonDll or PYTHONNET_PYDLL.");
    }

    private string ResolvePythonHome(string pythonDll)
    {
      if (!string.IsNullOrWhiteSpace(_settings.PythonHome))
        return _settings.PythonHome;

      var fromEnv = Environment.GetEnvironmentVariable("PYTHONHOME");
      if (!string.IsNullOrWhiteSpace(fromEnv))
        return fromEnv;

      if (string.IsNullOrWhiteSpace(pythonDll)) return null;

      var directory = Path.GetDirectoryName(pythonDll);
      if (directory == null) return null;

      if (OperatingSystem.IsMacOS() && directory.Contains("Versions"))
        return directory;

      return Directory.GetParent(directory)?.FullName ?? directory;
    }

    private class PythonParseResponse
    {
      public List<PythonParsedIngredient> Ingredients { get; set; } = new();
      public List<PythonParseError> Errors { get; set; } = new();
    }

    private class PythonParsedIngredient
    {
      public string Name { get; set; }
      public decimal? Quantity { get; set; }
      public string Unit { get; set; }
      public string Size { get; set; }
      public string Note { get; set; }
      public bool Branded { get; set; }
    }

    private class PythonParseError
    {
      public int Index { get; set; }
      public string Line { get; set; }
      public string Error { get; set; }
    }
  }
}
