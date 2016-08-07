using System.Collections;
using UnityEngine;

// Warning beep - http://freesound.org/people/Kodack/sounds/258193/

// Mic loudness code from http://forum.unity3d.com/threads/check-current-microphone-input-volume.133501/#post-2067251 (credit: atomtwist)
public class FilibusterModule : MonoBehaviour
{
	public static float MicLoudness;
	public TextMesh Display;
	public TextMesh ArmedDisplay;
	public GameObject AnalogLevel;
	public Material AnalogLevelMaterial;
	public AudioClip WarningBeep;
	public AudioClip StartingBeep;

	private float _timeElapsed;
	private float _failureTimeElapsed;
	private float _warningSoundTimeElapsed;
	private float[] _samples;
	private int _sampleIndex;
	private int _numConsecutiveFailures;
	private AudioClip _clipRecord = new AudioClip();
	private bool _isArmed;

	private const int SampleWindow = 60;
	private const float FailureThreshold = 25.0f;
	private static readonly Color ArmedColourOn = new Color(255, 0, 0, 1f);
	private static readonly Color ArmedColourOff = new Color(255, 0, 0, 0.2f);

	public void Start()
	{
		_samples = new float[SampleWindow];
		_sampleIndex = 0;
		_timeElapsed = 0;
		_failureTimeElapsed = 0;
		_warningSoundTimeElapsed = 0;
		_numConsecutiveFailures = 0;
		_isArmed = false;
		GetComponent<KMNeedyModule>().OnNeedyActivation = OnNeedyActivate;
		GetComponent<KMNeedyModule>().OnNeedyDeactivation = OnNeedyDeactivate;
		GetComponent<KMNeedyModule>().OnTimerExpired = DisarmModule;
	}

	//mic initialization
	private void InitMic()
	{
		_clipRecord = Microphone.Start(null, true, 1, 44100);
	}

	private void OnNeedyActivate()
	{
		GetComponent<KMAudio>().HandlePlaySoundAtTransform(StartingBeep.name, transform);
		StopAllCoroutines();
		StartCoroutine(ArmModule());
	}

	private void OnNeedyDeactivate()
	{
		StopAllCoroutines();
		DisarmModule();
	}

	IEnumerator ArmModule()
	{
		yield return new WaitForSeconds(5.0f);
		_isArmed = true;
		ArmedDisplay.color = ArmedColourOn;
	}

	private void StopMicrophone()
	{
		Microphone.End(null);
	}

	private void DisarmModule()
	{
		_numConsecutiveFailures = 0;
		_failureTimeElapsed = 0;
		_warningSoundTimeElapsed = 0;
		_isArmed = false;
		ArmedDisplay.color = ArmedColourOff;
	}

	//get data from microphone into audioclip
	private float LevelMax()
	{
		var levelMax = 0f;
		var waveData = new float[SampleWindow];
		var micPosition = Microphone.GetPosition(null) - (SampleWindow + 1); // null means the first microphone
		if (micPosition < 0) return 0;
		_clipRecord.GetData(waveData, micPosition);
		// Getting a peak on the last 128 samples
		for (var i = 0; i < SampleWindow; i++)
		{
			var wavePeak = waveData[i] * waveData[i];
			if (levelMax < wavePeak)
			{
				levelMax = wavePeak;
			}
		}
		return levelMax;
	}

	public void Update()
	{
		// levelMax equals to the highest normalized value power 2, a small number because < 1
		// pass the value to a static var so we can access it from anywhere
		MicLoudness = LevelMax() * 100;

		_samples[_sampleIndex++] = MicLoudness;
		if (_sampleIndex == SampleWindow)
			_sampleIndex = 0;

		_timeElapsed += Time.deltaTime;

		var micAverage = AverageLoudness();

		if (_timeElapsed > 0.1f)
		{
			UpdateDisplay(micAverage);
			_timeElapsed = 0;
		}

		if (_warningSoundTimeElapsed > 0.5f)
		{
			_warningSoundTimeElapsed = 0;
			UpdateWarningSound(micAverage);
		}

		UpdateBar(micAverage);

		if (_isArmed)
		{
			_warningSoundTimeElapsed += Time.deltaTime;
			_failureTimeElapsed += Time.deltaTime;

			if (_failureTimeElapsed > 1.0f)
			{
				CheckForFailure(micAverage);
				_failureTimeElapsed = 0f;
			}
		}
	}

	private void UpdateWarningSound(float micAverage)
	{
		if (micAverage < FailureThreshold)
		{
			GetComponent<KMAudio>().PlaySoundAtTransform(WarningBeep.name, transform);
		}
	}

	private void CheckForFailure(float micAverage)
	{
		if (micAverage < FailureThreshold)
		{
			_numConsecutiveFailures++;
			Debug.Log("Fail: " + _numConsecutiveFailures);
		}
		else
		{
			_numConsecutiveFailures = 0;
		}

		if (_numConsecutiveFailures > 2)
		{
			GetComponent<KMNeedyModule>().HandleStrike();
			DisarmModule();
		}
	}

	private float AverageLoudness()
	{
		var sum = 0f;
		foreach (var sample in _samples)
		{
			sum += sample;
		}

		var average = sum / _samples.Length * 10;
		var normalizedAverage = average > 100 ? 100 : average;

		return normalizedAverage;
	}

	private void UpdateDisplay(float average)
	{
		Display.text = average.ToString("0.0");
	}

	private void UpdateBar(float average)
	{
		if (average < FailureThreshold)
		{
			AnalogLevelMaterial.color = Color.red;
		}
		else if (average < 50f)
		{
			AnalogLevelMaterial.color = Color.yellow;
		}
		else
		{
			AnalogLevelMaterial.color = Color.green;
		}
		AnalogLevel.transform.localScale = new Vector3(1, 1, average);
	}

	private bool _isInitialized;
	// start mic when scene starts
	private void OnEnable()
	{
		InitMic();
		_isInitialized = true;
	}

	//stop mic when loading a new level or quit application
	public void OnDisable()
	{
		StopMicrophone();
		StopAllCoroutines();
	}

	public void OnDestroy()
	{
		StopMicrophone();
	}


	// make sure the mic gets started & stopped when application gets focused
	void OnApplicationFocus(bool focus)
	{
		if (focus)
		{
			if (!_isInitialized)
			{
				InitMic();
				_isInitialized = true;
			}
		}
		if (!focus)
		{
			StopMicrophone();
			_isInitialized = false;
		}
	}
}
