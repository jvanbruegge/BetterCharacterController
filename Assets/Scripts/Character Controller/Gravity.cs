using UnityEngine;

/**
 * A simple gravity indicator
 * When you have planet-like objects add this script to them
 * For a normal flat world add an empty game object as child of the player
 */
public class Gravity : MonoBehaviour
{
	[SerializeField]
	private float _gravity = 0.5f;

	public float gravity
	{
		get { return _gravity; }
	}
}
