using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;

public class IntensityLevels : MonoBehaviour
{
#pragma warning disable 0108
	public KMAudio audio;
#pragma warning restore 0108
	public KMBombInfo bombInfo;
	public KMBombModule module;
	public KMColorblindMode colorblindMode;

	public KMSelectable button;
	public KMSelectable[] arrows;
	public GameObject[] intensityLeds;
	public Material onMaterial;
	public Material offMaterial;
	public Material[] colorMaterials;
	public TextMesh colorblindText;

	private Coroutine buttonAnimCoroutine;
#pragma warning disable 0414
	private Coroutine intensityAnimCoroutine;
#pragma warning restore 0414

	private bool isSolved; // souvenir

	private int intensityLevel;
	private int goalIntensity; // souvenir
	private int goalTime;

	private int x;
	private int y;
	private int z;
	private int a;
	private int multiplierA;
	private int multiplierB;
	private int digitalRoot;
	private int tempDigitalRoot;

	private string[] colors = {"Red", "Blue", "Green", "Yellow", "White", "Black", "Magenta", "Cyan"};
	private int[,] tableA = new int[8, 5]
	{
		{1, 2, 3, 4, 5},
		{6, 7, 8, 9, 10},
		{11, 12, 13, 14, 15},
		{16, 17, 18, 19, 20},
		{20, 19, 18, 17, 16},
		{15, 14, 13, 12, 11},
		{10, 9, 8, 7, 6},
		{5, 4, 3, 2, 1}
	};
	private int[,] tableB = new int[10, 10]
	{
		{50, 49, 48, 47, 46, 45, 44, 43, 42, 41},
		{40, 39, 38, 37, 36, 35, 34, 33, 32, 31},
		{30, 29, 28, 27, 26, 25, 24, 23, 22, 21},
		{20, 19, 18, 17, 16, 15, 14, 13, 12, 11},
		{10, 9, 8, 7, 6, 5, 4, 3, 2, 1},
		{1, 2, 3, 4, 5, 6, 7, 8, 9, 10},
		{11, 12, 13, 14, 15, 16, 17, 18, 19, 20},
		{21, 22, 23, 24, 25, 26, 27, 28, 29, 30},
		{31, 32, 33, 34, 35, 36, 37, 38, 39, 40},
		{41, 42, 43, 44, 45, 46, 47, 48, 49, 50}
	};

	private int colorIndex;

	static int moduleIdCounter = 1;
	int moduleId;

	void Start()
	{
		moduleId = moduleIdCounter++;

        button.OnInteract += delegate () { ButtonPress(); return false; };
		arrows[0].OnInteract += delegate () { ArrowPress(0); return false; };
		arrows[1].OnInteract += delegate () { ArrowPress(1); return false; };

		isSolved = false;
		intensityLevel = 0;

		colorIndex = Random.Range(0, 7);
		button.GetComponent<MeshRenderer>().material = colorMaterials[colorIndex];

		getSolution();

		if(colorblindMode.ColorblindModeActive == true)
		{
			enableColorblind();
		}
	}

	void ButtonPress()
	{
		button.AddInteractionPunch();
		audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, button.transform);

		if(buttonAnimCoroutine != null) {
			StopCoroutine(buttonAnimCoroutine);
		}
		buttonAnimCoroutine = StartCoroutine(buttonAnim());

		if(isSolved == false) {
			if(intensityLevel == goalIntensity && (Mathf.Floor(bombInfo.GetTime()) % 10) == goalTime)
			{
				intensityAnimCoroutine = StartCoroutine(intensityAnim());
				isSolved = true;

				module.HandlePass();
				Debug.LogFormat("[Intensity Levels #{0}] Successfully disarmed the module!", moduleId);
			} else
			{
				module.HandleStrike();
				Debug.LogFormat("[Intensity Levels #{0}] Strike! Pressed button at intensity {1} and last digit {2}", moduleId, intensityLevel, bombInfo.GetFormattedTime().Last());
			}
		}
	}

	void ArrowPress(int arrow)
	{
		arrows[arrow].AddInteractionPunch();
		audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, arrows[arrow].transform);

		if(arrow == 0)
		{
			// up
			if(intensityLevel < 4) {
				intensityLevel++;
				intensityLeds[intensityLevel - 1].GetComponent<MeshRenderer>().material = onMaterial;
			}
		} else
		{
			// down
			if(intensityLevel > 0) {
				intensityLevel--;
				intensityLeds[intensityLevel].GetComponent<MeshRenderer>().material = offMaterial;
			}
		}
	}

	private IEnumerator buttonAnim(float duration = 0.075f)
	{
		float timer = 0f;
		while(timer < duration) {
			yield return null;
			timer += Time.deltaTime;

			button.transform.localPosition = Vector3.Lerp(new Vector3(0.0312f, 0.027f, 0f), new Vector3(0.0312f, 0.017f, 0f), timer/duration);
		}

		timer = 0f;
		while(timer < duration) {
			yield return null;
			timer += Time.deltaTime;

			button.transform.localPosition = Vector3.Lerp(new Vector3(0.0312f, 0.017f, 0f), new Vector3(0.0312f, 0.027f, 0f), timer/duration);
		}

		button.transform.localPosition = new Vector3(0.0312f, 0.027f, 0f);
	}

	private IEnumerator intensityAnim() {
		for(int i = 0; i < 4; i++) {
			intensityLevel = i;
			intensityLeds[intensityLevel].GetComponent<MeshRenderer>().material = offMaterial;
		}

		for(int i = 0; i < 4; i++) {
			yield return new WaitForSeconds(0.05f);
			intensityLevel = i;
			intensityLeds[intensityLevel].GetComponent<MeshRenderer>().material = onMaterial;
		}

		for(int i = 0; i < 4; i++) {
			yield return new WaitForSeconds(0.05f);
			intensityLevel = i;
			intensityLeds[intensityLevel].GetComponent<MeshRenderer>().material = colorMaterials[1];
		}
	}

	void getSolution() {
		// step 1
		x = (bombInfo.GetSerialNumberNumbers().First() * bombInfo.GetBatteryHolderCount()) + 1;
		y = (bombInfo.GetSerialNumberNumbers().Last() * bombInfo.GetPortCount()) + 1;
		z = x + y;
		if(bombInfo.GetBatteryCount() % 2 == 0) {
			z *= 2;
		} else {
			z *= 3;
		}
		z *= y;
		goalIntensity = z % 5;

		// step 2
		a = (goalIntensity + 1) * y;

		digitalRoot = ((z - 1) % 9) + 1;

		if(digitalRoot != 0) {
			a = (int)(a/digitalRoot);
		}

		multiplierA = tableA[colorIndex, goalIntensity];
		multiplierB = tableB[x % 10, y % 10];

		a *= multiplierA;
		a *= multiplierB;

		goalTime = (a + 1) % 10;

		// logging
		Debug.LogFormat("[Intensity Levels #{0}] Button color is {1}", moduleId, colors[colorIndex].ToLower());
		Debug.LogFormat("[Intensity Levels #{0}] x = {1}", moduleId, x);
		Debug.LogFormat("[Intensity Levels #{0}] y = {1}", moduleId, y);
		Debug.LogFormat("[Intensity Levels #{0}] z = {1}", moduleId, z);
		Debug.LogFormat("[Intensity Levels #{0}] Multiplier from Table A is {1}", moduleId, multiplierA);
		Debug.LogFormat("[Intensity Levels #{0}] Multiplier from Table B is {1}", moduleId, multiplierB);
		Debug.LogFormat("[Intensity Levels #{0}] Final a = {1}", moduleId, a);
		Debug.LogFormat("[Intensity Levels #{0}] Expected intensity {1}", moduleId, goalIntensity);
		Debug.LogFormat("[Intensity Levels #{0}] Expected press at last digit {1}", moduleId, goalTime);
	}

#pragma warning disable 0414
	private readonly string TwitchHelpMessage = "Cycle up or down using the commands \"!{0} up [amount]\" and \"!{0} down [amount]\" (default is 1 without the argument). Press the button using \"!{0} submit <digit>\", where <digit> is the last seconds digit of the timer you want to press the button on. Use \"!{0} colorblind\" to toggle between the colorblind mode.";
#pragma warning restore 0414

	private bool isColorblind = false;

	private IEnumerator ProcessTwitchCommand(string command) {
		command = command.ToLowerInvariant();

		if(command.StartsWith("up")) {
			string editedUp = command.Remove(0, 2);
			int amount = 1;
			if((editedUp.Length == 2) && (!(char.IsDigit(editedUp[1])) || ((editedUp[1] - '0') < 0))) {
				yield return "sendtochaterror!h Please input how many levels up you want to go, or input no argument if you want to go up by 1.";
			} else if(editedUp.Length == 2) {
				amount = editedUp[1] - '0';
			}
			yield return null;
			for(int i = 0; i < amount; i++) {
				yield return null;
				ArrowPress(0);
			}
		} else if(command.StartsWith("down")) {
			string editedDown = command.Remove(0, 4);
			int amount = 1;
			if((editedDown.Length == 2) && (!(char.IsDigit(editedDown[1])) || ((editedDown[1] - '0') < 0))) {
				yield return "sendtochaterror!h Please input how many levels down you want to go, or input no argument if you want to go down by 1.";
			} else if(editedDown.Length == 2) {
				amount = editedDown[1] - '0';
			}
			yield return null;
			for(int i = 0; i < amount; i++) {
				yield return null;
				ArrowPress(1);
			}
		} else if(command.StartsWith("submit")) {
			string timePress = command.Remove(0, 6);
			if((timePress.Length != 2) || !(char.IsDigit(timePress[1])) || ((timePress[1] - '0') < 0)) {
				yield return "sendtochaterror!h Please input the last seconds digit of the timer you want to press the button on.";
			} else {
				yield return null;
				timePress = timePress.Remove(0, 1);
				while((Mathf.Floor(bombInfo.GetTime()) % 10) != (timePress[0] - '0')) {
					yield return "trycancel";
				}
				ButtonPress();
			}
		} else if(command.StartsWith("colorblind")) {
			yield return null;
			if(isColorblind == false)
			{
				enableColorblind();
			} else
			{
				disableColorblind();
			}

			isColorblind = !isColorblind;
		}
	}

	private void enableColorblind()
	{
		if (colors[colorIndex] == "Black")
		{
			colorblindText.text = "K";
		} else
		{
            colorblindText.text = colors[colorIndex][0].ToString();
        }
	}

	private void disableColorblind()
	{
		colorblindText.text = "";
	}
}
