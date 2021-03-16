using TMPro;
using UnityEngine;

public class Scoreboard : MonoBehaviour
{
	private TextMeshProUGUI textMesh;

	private void Start()
	{
		textMesh = GetComponent<TextMeshProUGUI>();
	}

	private void Update()
	{
		string value = string.Empty;
		foreach (Tank tank in Tank.Tanks)
		{
			value += $"Tank {tank.Owner}: {tank.Score} PTS [{tank.Health} HP]\n";
		}
		textMesh.text = value;
	}
}