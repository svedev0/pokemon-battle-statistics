namespace PokemonSearch.Models;

public class Effectiveness
{
	// Attack
	public List<string> QuadrupleDamageTo { get; set; } = [];
	public List<string> DoubleDamageTo { get; set; } = [];
	public List<string> NormalDamageTo { get; set; } = [];
	public List<string> HalfDamageTo { get; set; } = [];
	public List<string> QuarterDamageTo { get; set; } = [];
	public List<string> NoDamageTo { get; set; } = [];
	// Defence
	public List<string> QuadrupleDamageFrom { get; set; } = [];
	public List<string> DoubleDamageFrom { get; set; } = [];
	public List<string> NormalDamageFrom { get; set; } = [];
	public List<string> HalfDamageFrom { get; set; } = [];
	public List<string> QuarterDamageFrom { get; set; } = [];
	public List<string> NoDamageFrom { get; set; } = [];
}
