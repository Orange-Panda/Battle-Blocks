using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank : NetworkComponent
{
	public const int MaxHealth = 10;
	public const string InputMessage = "I";
	public const string StatusMessage = "S";
	public const string AttackMessage = "A";
	
	[SerializeField]
	private Renderer[] renderers;
	private new Rigidbody rigidbody;
	private Vector2 input;

	private float speed = 9;
	private float attackCooldown = 0;

	public int Score { get; private set; }
	public int Health { get; private set; } = 10;
	public static List<Tank> Tanks { get; private set; } = new List<Tank>();

	protected override void OnAwake()
	{
		base.OnAwake();
		rigidbody = GetComponent<Rigidbody>();
	}

	private void OnEnable()
	{
		NetworkCore.ActiveNetwork.NetworkTick += ActiveNetwork_NetworkTick;
		Tanks.Add(this);
	}

	private void OnDisable()
	{
		NetworkCore.ActiveNetwork.NetworkTick -= ActiveNetwork_NetworkTick;
		Tanks.Remove(this);
	}

	private void Update()
	{
		if (IsLocalPlayer)
		{
			attackCooldown = Mathf.MoveTowards(attackCooldown, 0, Time.deltaTime);

			if (attackCooldown <= 0 && Input.GetKey(KeyCode.Space))
			{
				attackCooldown = 0.5f;
				SendToServer(AttackMessage);
			}
		}
	}

	private void FixedUpdate()
	{
		if (IsServer)
		{
			rigidbody.MovePosition(rigidbody.position + transform.forward * Mathf.Clamp(input.y, -1, 1) * speed * Time.fixedDeltaTime);
			rigidbody.rotation = Quaternion.Euler(0, rigidbody.rotation.eulerAngles.y + Mathf.Round(Mathf.Clamp(input.x, -1, 1)) * 2, 0);
		}
	}

	protected override IEnumerator NetworkUpdate()
	{
		if (IsClient)
		{
			Destroy(rigidbody);
		}
		else if (IsServer)
		{
			ResetTank();
		}

		yield return null;

		foreach (Renderer renderer in renderers)
		{
			if (IsClient && IsLocalPlayer)
			{
				renderer.material.color = Color.green; 
			}
			else if (IsClient && !IsLocalPlayer)
			{
				renderer.material.color = Color.red;
			}
		}
	}

	public override void HandleMessage(string command, List<string> args)
	{
		//Parse input on the server
		if (IsServer && command.Equals(InputMessage) && args.Count >= 2 && float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y))
		{
			input = new Vector2(x, y);
		}

		//Create projectile on the server
		if (IsServer && command.Equals(AttackMessage) && NetworkItems.TryGetIndex("player_bullet", out int index))
		{
			NetworkCore.ActiveNetwork.CreateNetworkObject(index, Owner, rigidbody.position + new Vector3(0, 0.4f, 0), Quaternion.Euler(0, rigidbody.rotation.eulerAngles.y, 0));
		}

		//Parse status on client
		if (IsClient && command.Equals(StatusMessage) && args.Count >= 2 && int.TryParse(args[0], out int health) && int.TryParse(args[1], out int score))
		{
			Health = health;
			Score = score;
		}
	}

	private void ActiveNetwork_NetworkTick()
	{
		//Send input to the server
		if (IsLocalPlayer)
		{
			List<string> args = new List<string>()
			{
				Input.GetAxis("Horizontal").ToNetworkString(),
				Input.GetAxis("Vertical").ToNetworkString()
			}; 
			SendToServer(InputMessage, args);
		}
		
		//Send status to clients
		if (IsServer)
		{
			List<string> args = new List<string>()
			{
				Health.ToString(),
				Score.ToString()
			};
			SendToClient(StatusMessage, args);
		}
	}

	public void TakeDamage(int attacker, int value = 1)
	{
		if (IsServer)
		{
			Health = Mathf.Clamp(Health - value, 0, MaxHealth);

			if (Health <= 0)
			{
				GrantScore(5, attacker);
				ResetTank();
				Score -= 10;
			}
		}
	}

	public static void GrantScore(int value, int owner)
	{
		foreach (Tank tank in Tanks)
		{
			if (tank.Owner == owner)
			{
				tank.GrantScore(value);
			}
		}
	}

	public void GrantScore(int value)
	{
		if (IsServer)
		{
			Score += value;
		}
	}

	private void ResetTank()
	{
		Spawner spawner = FindObjectOfType<Spawner>();
		rigidbody.position = spawner.playerPoints[Random.Range(0, spawner.playerPoints.Length)] + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
		rigidbody.rotation = Quaternion.Euler(0, Random.Range(-180f, 180f), 0);
		Health = MaxHealth;
	}
}