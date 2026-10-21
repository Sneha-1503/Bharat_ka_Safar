// using UnityEngine;  
// using System.Collections; 

// public class CarController : MonoBehaviour { 

// 	// Car rigidbody COM
// 	public Transform centerOfMass;

// 	// Wheel motor for drive the car based on wheelJoint 2D
// 	JointMotor2D motorBack;  

// 	// Which wheel to drive with that?
// 	public WheelJoint2D motorWheel;

// 	// Store car speed
// 	public float speed ;   

// 	// Store is grounded based on the distance of the ground
// 	bool isGrounded;

// 	// How much distance of the ground means cars is grounded? answer=> less than 2.1f
// 	public float groundDistance = 2.1f ;

// 	// Motor power, Brake power and deceleration speed
// 	public float motorPower = 1400f,
// 	brakePower = -14f,
// 	 decelerationSpeed = 0.3f;

// 	// Car max speed
// 	public float maxSpeed = 14f;

// 	// inrenal usage
// 	float motorTemp;

// 	// Can rotate option. be true value when car is on the fly
// 	bool canRotate = false;

// 	// Rotate force on the  fly 
// 	public float RotateForce = 140f;

// 	[HideInInspector]public AudioSource EngineSoundS;

// 	public bool isMobile;

// 	public ParticleSystem wheelParticle;

// 	// Internal usage
// 	float powerTemp;
// 	ParticleSystem.EmissionModule em ,emSmoke;

// 	public Transform particlePosition;

// 	public bool useSmoke;
// 	public ParticleSystem smoke;
// 	public float smokeTargetSpeed = 17f;

// 	public float cameraDistance = 15f;

// 	// Audio integration variables
// 	private float baseEngineVolume = 1f;
// 	private bool engineSoundInitialized = false;

// 	void Awake()
// 	{
// 		Vector3 posCamera;
// 		posCamera = Camera.main.transform.position;
// 		Camera.main.transform.position = new Vector3 (posCamera.x, posCamera.y, - cameraDistance);
// 	}

// 	void Start () { 

// 		// Set car rigidbody's COM
// 		GetComponent<Rigidbody2D>().centerOfMass = centerOfMass.transform.localPosition;

// 		// Starting with WheelJoint2D motor
// 		motorBack = motorWheel.motor; 

// 		// Cast a ray to find isGrounded 
// 		StartCoroutine (RaycCast ());

// 		// Initialize engine sound
// 		EngineSoundS = GetComponent<AudioSource> ();
// 		if (EngineSoundS)
// 		{
// 		    EngineSoundS.playOnAwake = false;
// 		    EngineSoundS.loop = true;
// 		    EngineSoundS.pitch = minP; // Start at minimum pitch
// 		    EngineSoundS.Play(); // Keep it playing but at low volume/pitch
// 		}

// 		powerTemp = motorPower;

// 		em = wheelParticle.emission;
// 		em.enabled = false;

// 		if (smoke) {
// 			emSmoke = smoke.emission;
// 			emSmoke.enabled = false;
// 		}

// 		// Initialize engine volume with AudioManager
// 		StartCoroutine(InitializeEngineAudio());
// 	}  

// 	IEnumerator InitializeEngineAudio()
// 	{
// 		// Wait for AudioManager to be ready
// 		while (AudioManager.instance == null)
// 		{
// 			yield return null;
// 		}

// 		// Set initial engine volume from AudioManager
// 		if (EngineSoundS)
// 		{
// 			baseEngineVolume = AudioManager.instance.GetEngineVolume();
// 			EngineSoundS.volume = baseEngineVolume * AudioManager.instance.maxEngineVolume;
// 			engineSoundInitialized = true;
// 		}
// 	}

// 	float currentSpeed;    

// 	void FixedUpdate(){

// 		// speed limiter based on max speed limit value
// 		if (speed > maxSpeed)
// 			motorPower = 0;
// 		else
// 			motorPower = powerTemp;
		
// 		// Moving forward
// 		if (Input.GetAxis ("Horizontal") > 0 || HoriTemp > 0) {

// 			// Add force to car back wheel
// 			if(isGrounded)
// 				motorBack.motorSpeed = Mathf.Lerp (motorBack.motorSpeed, -motorPower, Time.deltaTime * 1.4f);

// 			// Wheel particles
// 			if (isGrounded) {
// 				if (speed < 4.3f) {
// 					wheelParticle.transform.position = particlePosition.position;
// 					em.enabled = true;
// 				} else
// 					em.enabled = false;
// 			}
// 			else
// 				em.enabled = false;
			
// 		}
// 		else 
// 		{// Moving backward
// 			if (Input.GetAxis ("Horizontal") < 0 || HoriTemp < 0) {
// 				if (speed < -maxSpeed) {
// 					if (isGrounded)
// 						motorBack.motorSpeed = Mathf.Lerp (motorBack.motorSpeed, 0, Time.deltaTime * 3f);
// 				} else {
// 					if (isGrounded)
// 						motorBack.motorSpeed = Mathf.Lerp (motorBack.motorSpeed, motorPower, Time.deltaTime * 1.4f);
// 				}
                                                                                      
// 			} else {// Releasing car throttle and brake
// 				if (isGrounded)
// 					motorBack.motorSpeed = Mathf.Lerp (motorBack.motorSpeed, 0, Time.deltaTime * decelerationSpeed);	
// 			}
// 		}

// 		// Update WheelJoint2D motor inputs
// 		motorWheel.motor = motorBack; 

// 		// Check for rotate on the fly
// 		if (canRotate)
// 		{
// 			Rotate ();
// 		}

// 		// Handle engine sound
// 		#if UNITY_EDITOR
// 		EngineSoundEditor ();  
// 		#else
// 		EngineSoundMobile (); 
// 		#endif

// 		if (!isMobile)
// 			HoriTemp = Input.GetAxis ("Horizontal");

// 		if (useSmoke) {
// 			if (Input.GetAxis ("Horizontal") > 0 || HoriTemp > 0) {
// 				if (speed < smokeTargetSpeed)
// 					emSmoke.enabled = true;
// 				else
// 					emSmoke.enabled = false;
// 			} else
// 				emSmoke.enabled = false;
// 		}
// 	}

// 	void LateUpdate()
// 	{
// 		// Get car speed
// 		speed = GetComponent<Rigidbody2D>().linearVelocity.magnitude;
// 		if (Input.GetAxis ("Horizontal") < 0 || HoriTemp < 0) 
// 			speed = -speed;

// 		// Update engine volume from AudioManager
// 		UpdateEngineVolume();
// 	}

// 	// Update engine volume based on AudioManager settings
// 	void UpdateEngineVolume()
// 	{
// 		if (AudioManager.instance && EngineSoundS && engineSoundInitialized)
// 		{
// 			float currentEngineVolume = AudioManager.instance.GetEngineVolume();
// 			EngineSoundS.volume = currentEngineVolume * AudioManager.instance.maxEngineVolume;
// 		}
// 	}

// 	// Rotate car on air based on speed
// 	void Rotate()
// 	{
// 		//based on player forward input(Like Hill Climb Racing game)
// 		if (Input.GetAxis ("Horizontal") > 0 || HoriTemp > 0) {
// 			GetComponent<Rigidbody2D> ().AddTorque (RotateForce);
// 		} else {
// 			if (Input.GetAxis ("Horizontal") < 0.0f || HoriTemp < 0)
// 				GetComponent<Rigidbody2D> ().AddTorque (-RotateForce);
// 		}
// 	}

// 	// Raycast body to find that car is on the ground or not
// 	IEnumerator RaycCast()
// 	{
// 		while (true) 
// 		{
// 			yield return new WaitForEndOfFrame();

// 			RaycastHit2D hit = Physics2D.Raycast (transform.position, -Vector2.up, 1000);
// 			float distance = Mathf.Abs(hit.point.y - transform.position.y);

// 			if (distance < groundDistance)
// 				isGrounded = true;
// 			else
// 				isGrounded = false;

// 			canRotate = !isGrounded;  	
// 		}
// 	}

// 	// Engine sound system
// 	public float Multiplyer = 3f;
// 	public float minP = 1f;
// 	public float maxP = 2.4f;
// 	float HoriTemp;

// 	public void EngineSoundMin ()
// 	{
// 		if (EngineSoundS && EngineSoundS.pitch > minP)
// 			EngineSoundS.pitch -= 1.4f * Time.deltaTime;
// 	}

// 	public void EngineSoundMobile ()
// 	{
// 		if (!EngineSoundS || !engineSoundInitialized) return;

// 		// Always keep engine running, just adjust pitch based on input
// 		if (!EngineSoundS.isPlaying)
// 			EngineSoundS.Play();

// 		if (HoriTemp != 0) // When there's input
// 		{
// 		    if (speed < 40)
// 		    {
// 		        EngineSoundS.pitch = Mathf.Lerp(EngineSoundS.pitch, Mathf.Clamp(Mathf.Abs(HoriTemp) * Multiplyer, minP, maxP), Time.deltaTime * 5);
// 		    }
// 		    else
// 		    {
// 		        EngineSoundS.pitch = Mathf.Lerp(EngineSoundS.pitch, Mathf.Clamp(Mathf.Abs(HoriTemp) * Multiplyer, minP, maxP), Time.deltaTime * 5);
// 		    }
// 		}
// 		else // When no input - idle sound
// 		{
// 		    EngineSoundS.pitch = Mathf.Lerp(EngineSoundS.pitch, minP, Time.deltaTime * 3);
// 		}
// 	}

// 	public void EngineSoundEditor ()
// 	{
// 		if (!EngineSoundS || !engineSoundInitialized) return;

// 		// Always keep engine running, just adjust pitch based on input
// 		if (!EngineSoundS.isPlaying)
// 			EngineSoundS.Play();

// 		float horizontalInput = Input.GetAxis("Horizontal");

// 		if (horizontalInput != 0) // When there's input
// 		{
// 		    if (speed < 40)
// 		    {
// 		        EngineSoundS.pitch = Mathf.Lerp(EngineSoundS.pitch, Mathf.Clamp(Mathf.Abs(horizontalInput) * Multiplyer, minP, maxP), Time.deltaTime * 5);
// 		    }
// 		    else
// 		    {
// 		        EngineSoundS.pitch = Mathf.Lerp(EngineSoundS.pitch, Mathf.Clamp(Mathf.Abs(horizontalInput) * Multiplyer, minP, maxP), Time.deltaTime * 5);
// 		    }
// 		}
// 		else // When no input - idle sound
// 		{
// 		    EngineSoundS.pitch = Mathf.Lerp(EngineSoundS.pitch, minP, Time.deltaTime * 3);
// 		}
// 	}

// 	// Vehicle input system
// 	//this is public function for car Acceleration UI Button
// 	public void Acceleration ()
// 	{
// 		HoriTemp = 1f;
// 	}
// 	//this is public function for car Brake\Backward UI Button
// 	public void Brake ()
// 	{
// 		HoriTemp = -1f;
// 	}

// 	//this is for when player both release Brake or Acceleration button
// 	public void GasBrakeRelease ()
// 	{
// 		HoriTemp = 0;
// 	}
// }



using UnityEngine;  
using System.Collections; 

public class CarController : MonoBehaviour { 

	[Tooltip("Enabled by Vehicle Prefab Maker; preserves the existing pedal/HUD interface.")]
	public bool useBuilderPhysics;
	public float DriveInput => GetCurrentInput();

	// Car rigidbody COM
	public Transform centerOfMass;

	// Wheel motor for drive the car based on wheelJoint 2D
	JointMotor2D motorBack;  

	// Which wheel to drive with that?
	public WheelJoint2D motorWheel;

	// Store car speed
	public float speed ;   

	// Store is grounded based on the distance of the ground
	bool isGrounded;

	// How much distance of the ground means cars is grounded? answer=> less than 2.1f
	public float groundDistance = 2.1f ;

	// Motor power, Brake power and deceleration speed
	public float motorPower = 1400f,
	brakePower = -14f,
	 decelerationSpeed = 0.3f;

	// Car max speed
	public float maxSpeed = 14f;

	// inrenal usage
	float motorTemp;

	// Can rotate option. be true value when car is on the fly
	bool canRotate = false;

	// Rotate force on the  fly 
	public float RotateForce = 140f;

	[HideInInspector]public AudioSource EngineSoundS;

	public bool isMobile;

	public ParticleSystem wheelParticle;

	// Internal usage
	float powerTemp;
	ParticleSystem.EmissionModule em ,emSmoke;

	public Transform particlePosition;

	public bool useSmoke;
	public ParticleSystem smoke;
	public float smokeTargetSpeed = 17f;

	public float cameraDistance = 15f;

	// Audio integration variables
	private float baseEngineVolume = 1f;
	private bool engineSoundInitialized = false;

	void Awake()
	{
		Vector3 posCamera;
		if (Camera.main == null) return;
		posCamera = Camera.main.transform.position;
		Camera.main.transform.position = new Vector3 (posCamera.x, posCamera.y, - cameraDistance);
	}

	void Start () { 

		// Set car rigidbody's COM
		if (centerOfMass != null) GetComponent<Rigidbody2D>().centerOfMass = centerOfMass.transform.localPosition;

		// Starting with WheelJoint2D motor
		motorBack = motorWheel.motor; 

		// Cast a ray to find isGrounded 
		if (!useBuilderPhysics) StartCoroutine (RayCast ());

		// Initialize engine sound
		EngineSoundS = GetComponent<AudioSource> ();
		if (EngineSoundS)
		{
		    EngineSoundS.playOnAwake = false;
		    EngineSoundS.loop = true;
		    EngineSoundS.pitch = minP; // Start at minimum pitch
		    EngineSoundS.Play(); // Keep it playing but at low volume/pitch
		}

		powerTemp = motorPower;
		if (useBuilderPhysics)
		{
			StartCoroutine(InitializeEngineAudio());
			return;
		}

		em = wheelParticle.emission;
		em.enabled = false;

		if (smoke) {
			emSmoke = smoke.emission;
			emSmoke.enabled = false;
		}

		// Initialize engine volume with AudioManager
		StartCoroutine(InitializeEngineAudio());
	}  

	IEnumerator InitializeEngineAudio()
	{
		// Wait for AudioManager to be ready
		while (AudioManager.instance == null)
		{
			yield return null;
		}

		// Set initial engine volume from AudioManager
		if (EngineSoundS)
		{
			baseEngineVolume = AudioManager.instance.GetEngineVolume();
			EngineSoundS.volume = baseEngineVolume * AudioManager.instance.maxEngineVolume;
			engineSoundInitialized = true;
		}
	}

	float currentSpeed;    

	void FixedUpdate(){
		if (useBuilderPhysics) { HandleEngineSound(); return; }

		// speed limiter based on max speed limit value
		if (speed > maxSpeed)
			motorPower = 0;
		else
			motorPower = powerTemp;
		
		// Get input from keyboard OR mobile UI buttons
		float currentInput = GetCurrentInput();
		
		// Moving forward
		if (currentInput > 0) {

			// Add force to car back wheel
			if(isGrounded)
				motorBack.motorSpeed = Mathf.Lerp (motorBack.motorSpeed, -motorPower, Time.deltaTime * 1.4f);

			// Wheel particles
			if (isGrounded) {
				if (speed < 4.3f) {
					wheelParticle.transform.position = particlePosition.position;
					em.enabled = true;
				} else
					em.enabled = false;
			}
			else
				em.enabled = false;
			
		}
		else 
		{// Moving backward
			if (currentInput < 0) {
				if (speed < -maxSpeed) {
					if (isGrounded)
						motorBack.motorSpeed = Mathf.Lerp (motorBack.motorSpeed, 0, Time.deltaTime * 3f);
				} else {
					if (isGrounded)
						motorBack.motorSpeed = Mathf.Lerp (motorBack.motorSpeed, motorPower, Time.deltaTime * 1.4f);
				}
                                                                                      
			} else {// Releasing car throttle and brake
				if (isGrounded)
					motorBack.motorSpeed = Mathf.Lerp (motorBack.motorSpeed, 0, Time.deltaTime * decelerationSpeed);	
			}
		}

		// Update WheelJoint2D motor inputs
		motorWheel.motor = motorBack; 

		// Check for rotate on the fly
		if (canRotate)
		{
			Rotate ();
		}

		// Handle engine sound - use unified method
		HandleEngineSound();

		if (useSmoke) {
			if (currentInput > 0) {
				if (speed < smokeTargetSpeed)
					emSmoke.enabled = true;
				else
					emSmoke.enabled = false;
			} else
				emSmoke.enabled = false;
		}
	}

	// New method to get current input from either source
	float GetCurrentInput()
	{
		if (isMobile)
		{
			return HoriTemp; // Use mobile UI input
		}
		else
		{
			// For desktop/editor, combine both keyboard and UI input
			float keyboardInput = Input.GetAxis("Horizontal");
			return keyboardInput != 0 ? keyboardInput : HoriTemp;
		}
	}

	void LateUpdate()
	{
		// Get car speed
		if (useBuilderPhysics)
		{
			speed = GetComponent<Rigidbody2D>().linearVelocity.x;
			UpdateEngineVolume();
			return;
		}
		speed = GetComponent<Rigidbody2D>().linearVelocity.magnitude;
		float currentInput = GetCurrentInput();
		if (currentInput < 0) 
			speed = -speed;

		// Update engine volume from AudioManager
		UpdateEngineVolume();
	}

	// Update engine volume based on AudioManager settings
	void UpdateEngineVolume()
	{
		if (AudioManager.instance && EngineSoundS && engineSoundInitialized)
		{
			float currentEngineVolume = AudioManager.instance.GetEngineVolume();
			EngineSoundS.volume = currentEngineVolume * AudioManager.instance.maxEngineVolume;
		}
	}

	// Rotate car on air based on speed
	void Rotate()
	{
		float currentInput = GetCurrentInput();
		//based on player forward input(Like Hill Climb Racing game)
		if (currentInput > 0) {
			GetComponent<Rigidbody2D> ().AddTorque (RotateForce);
		} else {
			if (currentInput < 0.0f)
				GetComponent<Rigidbody2D> ().AddTorque (-RotateForce);
		}
	}

	// Raycast body to find that car is on the ground or not
	IEnumerator RayCast()
	{
		while (true) 
		{
			yield return new WaitForEndOfFrame();

			RaycastHit2D hit = Physics2D.Raycast (transform.position, -Vector2.up, 1000);
			float distance = Mathf.Abs(hit.point.y - transform.position.y);

			if (distance < groundDistance)
				isGrounded = true;
			else
				isGrounded = false;

			canRotate = !isGrounded;  	
		}
	}

	// Engine sound system
	public float Multiplyer = 3f;
	public float minP = 1f;
	public float maxP = 2.4f;
	float HoriTemp;

	public void EngineSoundMin ()
	{
		if (EngineSoundS && EngineSoundS.pitch > minP)
			EngineSoundS.pitch -= 1.4f * Time.deltaTime;
	}

	// Unified engine sound handling method
	void HandleEngineSound()
	{
		if (!EngineSoundS || !engineSoundInitialized) return;

		// Always keep engine running, just adjust pitch based on input
		if (!EngineSoundS.isPlaying)
			EngineSoundS.Play();

		float currentInput = GetCurrentInput();

		if (currentInput != 0) // When there's input
		{
		    if (speed < 40)
		    {
		        EngineSoundS.pitch = Mathf.Lerp(EngineSoundS.pitch, Mathf.Clamp(Mathf.Abs(currentInput) * Multiplyer, minP, maxP), Time.deltaTime * 5);
		    }
		    else
		    {
		        EngineSoundS.pitch = Mathf.Lerp(EngineSoundS.pitch, Mathf.Clamp(Mathf.Abs(currentInput) * Multiplyer, minP, maxP), Time.deltaTime * 5);
		    }
		}
		else // When no input - idle sound
		{
		    EngineSoundS.pitch = Mathf.Lerp(EngineSoundS.pitch, minP, Time.deltaTime * 3);
		}
	}

	// Keep the old methods for backward compatibility if needed
	public void EngineSoundMobile ()
	{
		HandleEngineSound();
	}

	public void EngineSoundEditor ()
	{
		HandleEngineSound();
	}

	// Vehicle input system
	//this is public function for car Acceleration UI Button
	public void Acceleration ()
	{
		HoriTemp = 1f;
	}
	//this is public function for car Brake\Backward UI Button
	public void Brake ()
	{
		HoriTemp = -1f;
	}

	//this is for when player both release Brake or Acceleration button
	public void GasBrakeRelease ()
	{
		HoriTemp = 0;
	}
}
