using UnityEngine;

public class FilibusterModule : MonoBehaviour
{
	public TextMesh Display;
	public static float MicLoudness;
	public GameObject AnalogLevel;

	private float _timeElapsed;
	private float[] _samples;
	private int _sampleIndex;
	private AudioClip _clipRecord = new AudioClip();
	private const int SampleWindow = 128;

	void Start()
	{
		_samples = new float[SampleWindow];
		_sampleIndex = 0;
		_timeElapsed = Time.time;
	}

	//mic initialization
	void InitMic()
	{
		_clipRecord = Microphone.Start(null, true, 1, 44100);
	}

	void StopMicrophone()
	{
		Microphone.End(null);
	}

	//get data from microphone into audioclip
	float LevelMax()
	{
		float levelMax = 0;
		float[] waveData = new float[SampleWindow];
		int micPosition = Microphone.GetPosition(null) - (SampleWindow + 1); // null means the first microphone
		if (micPosition < 0) return 0;
		_clipRecord.GetData(waveData, micPosition);
		// Getting a peak on the last 128 samples
		for (int i = 0; i < SampleWindow; i++)
		{
			float wavePeak = waveData[i] * waveData[i];
			if (levelMax < wavePeak)
			{
				levelMax = wavePeak;
			}
		}
		return levelMax;
	}

	void Update()
	{
		// levelMax equals to the highest normalized value power 2, a small number because < 1
		// pass the value to a static var so we can access it from anywhere
		MicLoudness = LevelMax() * 100;

		_samples[_sampleIndex++] = MicLoudness;
		if (_sampleIndex == SampleWindow)
			_sampleIndex = 0;

		_timeElapsed += Time.deltaTime;

		if (_timeElapsed > 1f)
		{
			var sum = 0f;
			foreach (var sample in _samples)
			{
				sum += sample;
			}

			var average = sum / _samples.Length * 10;
			var zSize = average > 100 ? 100 : average;
			Display.text = zSize.ToString("0.0");
			AnalogLevel.transform.localScale = new Vector3(1, 1, zSize);

			_timeElapsed = 0f;
		}
	}

	bool _isInitialized;
	// start mic when scene starts
	void OnEnable()
	{
		InitMic();
		_isInitialized = true;
	}

	//stop mic when loading a new level or quit application
	void OnDisable()
	{
		StopMicrophone();
	}

	void OnDestroy()
	{
		StopMicrophone();
	}


	// make sure the mic gets started & stopped when application gets focused
	void OnApplicationFocus(bool focus)
	{
		if (focus)
		{
			//Debug.Log("Focus");

			if (!_isInitialized)
			{
				//Debug.Log("Init Mic");
				InitMic();
				_isInitialized = true;
			}
		}
		if (!focus)
		{
			//Debug.Log("Pause");
			StopMicrophone();
			//Debug.Log("Stop Mic");
			_isInitialized = false;

		}
	}
}
