using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Android;
using Oculus.Voice;
using Meta.WitAi.Events;
using System.Text.RegularExpressions;
using System.Collections;

public class ContinuousVoiceListener : MonoBehaviour
{
    [Header("Voice Setup")]
    public AppVoiceExperience voiceExperience;

    [Header("Keyword Setup")]
    public string[] triggerKeywords;
    public UnityEvent[] triggerEvents;

    [SerializeField, TextArea(2, 6)]
    private string currentTranscription = "";

    private void Start()
    {
        StartCoroutine(StartVoiceExp());
        //IntializeVoiceExp();
    }

    IEnumerator StartVoiceExp()
    {
        // Wait for Unity and OVR audio to initialize
        yield return new WaitForSeconds(3f);

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(Permission.Microphone));
        }

        InitializeVoiceExp();
    }


    void InitializeVoiceExp()
    {
        voiceExperience.VoiceEvents.OnRequestCompleted.AddListener(ReactivateVoice);
        voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        voiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);

        voiceExperience.Activate();
    }

    void ReactivateVoice() => voiceExperience.Activate();

    private void OnPartialTranscription(string text)
    {
        currentTranscription = text;
        CheckForKeywords();
    }

    private void OnFullTranscription(string text)
    {
        currentTranscription = text;
        CheckForKeywords();

        // clear buffer but keep mic alive
        //currentTranscription = "";
    }

    private void CheckForKeywords()
    {
        // if (string.IsNullOrEmpty(currentTranscription)) return;

        string cleanText = currentTranscription.ToLower().Trim();

        for (int i = 0; i < triggerKeywords.Length; i++)
        {
            string keyword = triggerKeywords[i].ToLower().Trim();
            if (cleanText.Contains(keyword))
            {
                StartCoroutine(InvokeNextFrame(i));
                Debug.Log("TRIGGERED EVENT WORD: " + triggerKeywords[i]);
                currentTranscription = "";
                voiceExperience.VoiceEvents.OnPartialTranscription?.Invoke("");
                voiceExperience.VoiceEvents.OnFullTranscription?.Invoke("");
                return;
            }
        }
    }
    private IEnumerator InvokeNextFrame(int index)
    {
        yield return null; // Wait one frame for Meta thread cleanup
        triggerEvents?[index]?.Invoke();
        Debug.Log($"✅ TRIGGERED EVENT WORD: {triggerKeywords[index]}");
    }

    void RemoveListeners()
    {
        voiceExperience.VoiceEvents.OnRequestCompleted.RemoveListener(ReactivateVoice);
        voiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        voiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }
}
