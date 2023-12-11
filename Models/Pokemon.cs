using System.ComponentModel.DataAnnotations;

namespace PokemonSearch.Models;

public class Pokemon
{
	[Required]
	public string Name { get; set; } = string.Empty;
	public int Number { get; set; } = 0;
	public string Image { get; set; } = string.Empty;
	public List<string> Types { get; set; } = [];
	public List<string> AttackSuperEffective { get; set; } = [];
	public List<string> AttackNotVeryEffective { get; set; } = [];
	public List<string> AttackNoDamage { get; set; } = [];
	public List<string> DefenceImmune { get; set; } = [];
	public List<string> DefenceResistant { get; set; } = [];
	public List<string> DefenceNormal { get; set; } = [];
	public List<string> DefenceWeak { get; set; } = [];
}
