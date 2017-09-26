using UnityEngine;
[RequireComponent(typeof(PlayerMotor))]
public class PlayerController : MonoBehaviour {


    [SerializeField]
    private float movement_Speed = 5f;

    private PlayerMotor motor;

	void Start () {
        motor = GetComponent<PlayerMotor>();
	}
	
	// Update is called once per frame
	void Update () {
        float _xMovement = Input.GetAxisRaw("Horizontal");
        float _Jump_Z = Input.GetAxisRaw("Vertical");//remember to use this input for jumping up 
        //or moving inside the map

        Vector3 _moveHorizontal = transform.right * _xMovement;
        Vector3 _moveVertical = transform.forward * _Jump_Z;

        Vector3 velocity = _moveHorizontal.normalized * movement_Speed;//make sure to see for how long is the 
        //key pressed as the velocity and acceleration should be applied according to the button presses

        motor.SendVelocity(velocity);
	}
}
