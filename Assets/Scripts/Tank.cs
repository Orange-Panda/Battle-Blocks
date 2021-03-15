using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank : NetworkComponent
{
	public override void HandleMessage(string command, List<string> args)
	{
		
	}

	protected override IEnumerator NetworkUpdate()
	{
		throw new System.NotImplementedException();
	}
}