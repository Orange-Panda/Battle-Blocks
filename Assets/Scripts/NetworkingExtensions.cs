using UnityEngine;

static class NetworkingExtensions
{
	public static string ToNetworkString(this float value)
	{
		return value.ToString("N2").TrimEnd('0').TrimEnd('.');
	}

	public static Vector3 Rounded(this Vector3 value)
	{
		value *= 100;
		value = new Vector3(Mathf.Round(value.x), Mathf.Round(value.y), Mathf.Round(value.z));
		return value / 100;
	}
}