using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using PokemonSearch.Models;
using System.Text.Json;

namespace PokemonSearch.Pages;

public class IndexModel : PageModel
{
	private readonly ILogger<IndexModel> _logger;

	public IndexModel(ILogger<IndexModel> logger)
	{
		_logger = logger;
	}

	public IActionResult OnGet()
	{
		return Page();
	}

	public async Task<IActionResult> OnPost(string name)
	{
		Pokemon pokemon = await GetPokemon(name);

		if (string.IsNullOrEmpty($"{pokemon.Number}"))
		{
			ViewData["result"] = "notfound";
			return Page();
		}

		Effectiveness effect = await GetEffectiveness(pokemon);

		if (pokemon.Number == 0)
		{
			ViewData["result"] = "error";
			return Page();
		}

		ViewData["result"] = "found";

		ViewData["name"] = ToTitleCase(name);
		ViewData["number"] = pokemon.Number;
		ViewData["image"] = pokemon.Image;
		ViewData["types"] = string.Join(";", pokemon.Types);

		ViewData["attack_x4"] = "";
		ViewData["attack_x2"] = string.Join(";", effect.DoubleDamageTo);
		ViewData["attack_x1"] = string.Join(";", effect.NormalDamageTo);
		ViewData["attack_x0.5"] = string.Join(";", effect.HalfDamageTo);
		ViewData["attack_x0.25"] = string.Join(";", effect.QuarterDamageTo);
		ViewData["attack_x0"] = string.Join(";", effect.NoDamageTo);

		ViewData["defence_x4"] = "";
		ViewData["defence_x2"] = string.Join(";", effect.DoubleDamageFrom);
		ViewData["defence_x1"] = string.Join(";", effect.NormalDamageFrom);
		ViewData["defence_x0.5"] = string.Join(";", effect.HalfDamageFrom);
		ViewData["defence_x0.25"] = string.Join(";", effect.QuarterDamageFrom);
		ViewData["defence_x0"] = string.Join(";", effect.NoDamageFrom);

		return Page();
	}

	private static readonly List<string> AllTypes = [
		"Normal", "Fire", "Water", "Electric", "Grass", "Ice",
		"Fighting", "Poison", "Ground", "Flying", "Psychic", "Bug",
		"Rock", "Ghost", "Dragon", "Dark", "Steel", "Fairy"
	];

	private static async Task<Pokemon> GetPokemon(string name)
	{
		string url = $"https://pokeapi.co/api/v2/pokemon/{name.ToLower()}";

		HttpRequestMessage request = new()
		{
			Method = HttpMethod.Get,
			RequestUri = new Uri(url),
		};

		HttpClient client = new();
		var response = client.SendAsync(request);
		var result = await response.Result.Content.ReadAsStringAsync();

		return ParsePokemon(result);
	}

	private static Pokemon ParsePokemon(string json)
	{
		JObject pokemonObject;
		try
		{
			pokemonObject = JObject.Parse(json);
		}
		catch
		{
			return new();
		}

		string name = "";

		var root = pokemonObject.Root;
		int number = root["id"]?.Value<int>() ?? 0;

		string image = root["sprites"]?["other"]?["official-artwork"]?["front_default"]?
			.Value<string>() ?? string.Empty;

		List<string> types = JArray.Parse(root["types"]?.ToString() ?? string.Empty)
			.Select(x => ToTitleCase(x["type"]?["name"]?.Value<string>() ?? string.Empty))
			.ToList();

		return new()
		{
			Name = name,
			Number = number,
			Image = image,
			Types = types,
		};
	}

	private static async Task<string> GetEffectivenessJson(string type)
	{
		string url = $"https://pokeapi.co/api/v2/type/{type}";

		HttpRequestMessage request = new()
		{
			Method = HttpMethod.Get,
			RequestUri = new Uri(url),
		};

		HttpClient client = new();
		var response = client.SendAsync(request);
		return await response.Result.Content.ReadAsStringAsync();
	}

	private static async Task<Effectiveness> GetEffectiveness(Pokemon pokemon)
	{
		List<string> types = pokemon.Types;

		if (types.Count == 1)
		{
			string type = pokemon.Types[0].ToLower();
			string json = await GetEffectivenessJson(type);
			return ParseEffectiveness(json);
		}
		else if (types.Count == 2)
		{
			string type1 = pokemon.Types[0].ToLower();
			string json1 = await GetEffectivenessJson(type1);
			var effectiveness1 = ParseEffectiveness(json1);

			string type2 = pokemon.Types[1].ToLower();
			string json2 = await GetEffectivenessJson(type2);
			var effectiveness2 = ParseEffectiveness(json2);

			return CalculateEffectiveness(effectiveness1, effectiveness2);
		}
		else
		{
			return new();
		}
	}

	private static Effectiveness ParseEffectiveness(string json)
	{
		JsonDocument typesObject = JsonDocument.Parse(json);
		JsonElement root = typesObject.RootElement;
		JsonElement damageRelations = root.GetProperty("damage_relations");

		List<string> doubleDamageTo = [.. damageRelations
			.GetProperty("double_damage_to").EnumerateArray()
			.Select(x => ToTitleCase(x.GetProperty("name").GetString() ?? "-"))];

		List<string> halfDamageTo = [.. damageRelations
			.GetProperty("half_damage_to").EnumerateArray()
			.Select(x => ToTitleCase(x.GetProperty("name").GetString() ?? "-"))];

		List<string> noDamageTo = [.. damageRelations
			.GetProperty("no_damage_to").EnumerateArray()
			.Select(x => ToTitleCase(x.GetProperty("name").GetString() ?? "-"))];

		List<string> doubleDamageFrom = [.. damageRelations
			.GetProperty("double_damage_from").EnumerateArray()
			.Select(x => ToTitleCase(x.GetProperty("name").GetString() ?? "-"))];

		List<string> halfDamageFrom = [.. damageRelations
			.GetProperty("half_damage_from").EnumerateArray()
			.Select(x => ToTitleCase(x.GetProperty("name").GetString() ?? "-"))];

		List<string> noDamageFrom = [.. damageRelations
			.GetProperty("no_damage_from").EnumerateArray()
			.Select(x => ToTitleCase(x.GetProperty("name").GetString() ?? "-"))];

		List<string> normalDamageTo = [.. AllTypes
			.Except(doubleDamageTo)
			.Except(halfDamageTo)
			.Except(noDamageTo)];

		List<string> normalDamageFrom = [.. AllTypes
			.Except(doubleDamageFrom)
			.Except(halfDamageFrom)
			.Except(noDamageFrom)];

		return new()
		{
			DoubleDamageTo = doubleDamageTo,
			NormalDamageTo = normalDamageTo,
			HalfDamageTo = halfDamageTo,
			NoDamageTo = noDamageTo,
			DoubleDamageFrom = doubleDamageFrom,
			NormalDamageFrom = normalDamageFrom,
			HalfDamageFrom = halfDamageFrom,
			NoDamageFrom = noDamageFrom,
		};
	}

	private static Effectiveness CalculateEffectiveness(Effectiveness eff1, Effectiveness eff2)
	{
		/*
		Double (2)   * Double (2)   = x4
		Double (2)   * Normal (1)   = x2
		Double (2)   * Half   (0.5) = x1
		Double (2)   * No     (0)   = x0
		Normal (1)   * Double (2)   = x2
		Normal (1)   * Normal (1)   = x1
		Normal (1)   * Half   (0.5) = x0.5
		Normal (1)   * No     (0)   = x0
		Half   (0.5) * Double (2)   = x1
		Half   (0.5) * Normal (1)   = x0.5
		Half   (0.5) * Half   (0.5) = x0.25
		Half   (0.5) * No     (0)   = x0
		No     (0)   * Double (2)   = x0
		No     (0)   * Normal (1)   = x0
		No     (0)   * Half   (0.5) = x0
		No     (0)   * No     (0)   = x0
		*/

		// Attack
		List<(string type, double effectMultiplier)> effList1 = [];
		List<(string type, double effectMultiplier)> effList2 = [];
		List<(string type, double effectMultiplier)> combinedEffList = [];
		List<(string type, double effectMultiplier)> realEffList = [];

		effList1 = [.. effList1
			.Concat(eff1.DoubleDamageTo.Select(type => (type, 2.0)))
			.Concat(eff1.NormalDamageTo.Select(type => (type, 1.0)))
			.Concat(eff1.HalfDamageTo.Select(type => (type, 0.5)))
			.Concat(eff1.NoDamageTo.Select(type => (type, 0.0)))];

		effList2 = [.. effList2
			.Concat(eff2.DoubleDamageTo.Select(type => (type, 2.0)))
			.Concat(eff2.NormalDamageTo.Select(type => (type, 1.0)))
			.Concat(eff2.HalfDamageTo.Select(type => (type, 0.5)))
			.Concat(eff2.NoDamageTo.Select(type => (type, 0.0)))];

		combinedEffList = [.. effList1, .. effList2];

		foreach (var (type, effectMultiplier) in combinedEffList)
		{
			int typeEffCount = combinedEffList.Count(x => x.type == type);

			if (typeEffCount == 1)
			{
				realEffList.Add((type, effectMultiplier));
				continue;
			}

			var typeEff1 = combinedEffList.First(x => x.type == type);
			var typeEff2 = combinedEffList.Last(x => x.type == type);

			if (typeEff1.effectMultiplier == 0 ||
				typeEff2.effectMultiplier == 0)
			{
				realEffList.Add((type, 0));
				continue;
			}

			double realTypeEff =
				typeEff1.effectMultiplier * typeEff2.effectMultiplier;

			if (!realEffList.Select(x => x.type).Contains(type))
			{
				realEffList.Add((type, realTypeEff));
			}
		}

		List<string> quadrupleDamageTo = [.. realEffList
			.Where(x => x.effectMultiplier == 4)
			.Select(x => x.type)];
		List<string> doubleDamageTo = [.. realEffList
			.Where(x => x.effectMultiplier == 2)
			.Select(x => x.type)];
		List<string> normalDamageTo = [.. realEffList
			.Where(x => x.effectMultiplier == 1)
			.Select(x => x.type)];
		List<string> halfDamageTo = [.. realEffList
			.Where(x => x.effectMultiplier == 0.5)
			.Select(x => x.type)];
		List<string> quarterDamageTo = [.. realEffList
			.Where(x => x.effectMultiplier == 0.25)
			.Select(x => x.type)];
		List<string> noDamageTo = [.. realEffList
			.Where(x => x.effectMultiplier == 0)
			.Select(x => x.type)
			.Distinct()];

		// Defence
		effList1 = [];
		effList2 = [];
		combinedEffList = [];
		realEffList = [];

		effList1 = [.. effList1
			.Concat(eff1.DoubleDamageFrom.Select(type => (type, 2.0)))
			.Concat(eff1.NormalDamageFrom.Select(type => (type, 1.0)))
			.Concat(eff1.HalfDamageFrom.Select(type => (type, 0.5)))
			.Concat(eff1.NoDamageFrom.Select(type => (type, 0.0)))];

		effList2 = [.. effList2
			.Concat(eff2.DoubleDamageFrom.Select(type => (type, 2.0)))
			.Concat(eff2.NormalDamageFrom.Select(type => (type, 1.0)))
			.Concat(eff2.HalfDamageFrom.Select(type => (type, 0.5)))
			.Concat(eff2.NoDamageFrom.Select(type => (type, 0.0)))];

		combinedEffList = [.. effList1, .. effList2];

		foreach (var (type, effectMultiplier) in combinedEffList)
		{
			int typeEffCount = combinedEffList.Count(x => x.type == type);

			if (typeEffCount == 1)
			{
				realEffList.Add((type, effectMultiplier));
				continue;
			}

			var typeEff1 = combinedEffList.First(x => x.type == type);
			var typeEff2 = combinedEffList.Last(x => x.type == type);

			if (typeEff1.effectMultiplier == 0 ||
				typeEff2.effectMultiplier == 0)
			{
				realEffList.Add((type, 0));
				continue;
			}

			double realTypeEff =
				typeEff1.effectMultiplier * typeEff2.effectMultiplier;

			if (!realEffList.Select(x => x.type).Contains(type))
			{
				realEffList.Add((type, realTypeEff));
			}
		}

		List<string> quadrupleDamageFrom = [.. realEffList
			.Where(x => x.effectMultiplier == 4)
			.Select(x => x.type)];
		List<string> doubleDamageFrom = [.. realEffList
			.Where(x => x.effectMultiplier == 2)
			.Select(x => x.type)];
		List<string> normalDamageFrom = [.. realEffList
			.Where(x => x.effectMultiplier == 1)
			.Select(x => x.type)];
		List<string> halfDamageFrom = [.. realEffList
			.Where(x => x.effectMultiplier == 0.5)
			.Select(x => x.type)];
		List<string> quarterDamageFrom = [.. realEffList
			.Where(x => x.effectMultiplier == 0.25)
			.Select(x => x.type)];
		List<string> noDamageFrom = [.. realEffList
			.Where(x => x.effectMultiplier == 0)
			.Select(x => x.type)
			.Distinct()];

		return new()
		{
			QuadrupleDamageTo = quadrupleDamageTo,
			DoubleDamageTo = doubleDamageTo,
			NormalDamageTo = normalDamageTo,
			HalfDamageTo = halfDamageTo,
			QuarterDamageTo = quarterDamageTo,
			NoDamageTo = noDamageTo,
			QuadrupleDamageFrom = quadrupleDamageFrom,
			DoubleDamageFrom = doubleDamageFrom,
			NormalDamageFrom = normalDamageFrom,
			HalfDamageFrom = halfDamageFrom,
			QuarterDamageFrom = quarterDamageFrom,
			NoDamageFrom = noDamageFrom,
		};
	}

	private static string ToTitleCase(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		return char.ToUpper(input[0]) + input[1..];
	}
}
