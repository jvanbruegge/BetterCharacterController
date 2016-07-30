using UnityEngine;

public class MovementController : MonoBehaviour
{
	[SerializeField]
	private float movementSpeed = 2f;

	private Vector3 movement;

	public Vector3 Movement
	{
		get { return movement; }
	}

	private void Update()
	{
		Vector3 movement = Vector3.zero;

		if(getForward())
		{
			movement += transform.forward;
		}
		else if(getBackward())
		{
			movement -= transform.forward;
		}

		if(getLeft())
		{
			movement -= transform.right;
		}
		else if(getRight())
		{
			movement += transform.right;
		}

		if(movement != Vector3.zero)
		{
			movement = movement.normalized;
		}

		this.movement = movement;
	}

	//++++++++++++++++ Override those methods in a subclass to plug in a custom input logic +++++++++++++++++++++++//
	protected virtual bool getForward()		{ return Input.GetKey(KeyCode.W); }
	protected virtual bool getBackward()	{ return Input.GetKey(KeyCode.S); }
	protected virtual bool getLeft()		{ return Input.GetKey(KeyCode.A); }
	protected virtual bool getRight()		{ return Input.GetKey(KeyCode.D); }
}