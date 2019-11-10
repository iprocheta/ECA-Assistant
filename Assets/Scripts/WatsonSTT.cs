using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using IBM.Watson.DeveloperCloud.DataTypes;
using IBM.Watson.DeveloperCloud.Services.Assistant.v1;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class WatsonSTT : WatsonCommon
{
    
    private SpeechToText service;
    
    private AudioClip recording = null;

    private string microphoneId;
    private int recordingBufferSize = 1;
    private int recordingHZ = 22050;

    private int recordingRoutine;

    private string _recognizeModel;

    public Text resultText;

    private string outputTextFinal = "";

    private string tempOutputText = "";

    private float timer = 0;
    private bool finalResultFound = false;

    public float t;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CreateService());
        
    }

    // Update is called once per frame
    void Update()
    {
        //print(outputTextFinal + " "+ tempOutputText);
        if (!MessengerBehaviour.Instance.isAvatartalking && outputTextFinal!="")
        {
            if(tempOutputText!=outputTextFinal)
            {
                tempOutputText = outputTextFinal;
                timer = 0;
            }
            else if (tempOutputText == outputTextFinal)
            {
                timer += Time.deltaTime;
                //Debug.Log("Timer ="+ timer);
            }

            if (timer > t && !finalResultFound)
            {
                
                timer = 0;
                Debug.Log(outputTextFinal);
                MessengerBehaviour.Instance.FinalMassageInput = outputTextFinal;
                Debug.Log(MessengerBehaviour.Instance.FinalMassageInput);
                MessengerBehaviour.Instance.STTCompleted();
                Debug.Log("Reached the Threshold. Talk now");
                outputTextFinal = "";
                finalResultFound = true;
                //StartCoroutine(WaitAndSay());

            }
        }
        
    }
    
    public bool Active
    {
        get { return service.IsListening; }
        set
        {
            if (value && !service.IsListening)
            {
                service.RecognizeModel = (string.IsNullOrEmpty(_recognizeModel) ? "en-US_BroadbandModel" : _recognizeModel);
                service.DetectSilence = true;
                service.EnableWordConfidence = true;
                service.EnableTimestamps = true;
                service.SilenceThreshold = 0.01f;
                service.MaxAlternatives = 1;
                service.EnableInterimResults = true;
                service.OnError = OnError;
                service.InactivityTimeout = -1;
                service.ProfanityFilter = false;
                service.SmartFormatting = true;
                service.SpeakerLabels = false;
                service.WordAlternativesThreshold = 0.5f;
                service.StartListening(OnRecognize, OnRecognizeSpeaker);
            }
            else if (!value && service.IsListening)
            {
                service.StopListening();
            }
        }
    }
    private IEnumerator CreateService() //connect server with provided credentials 
    {
        if (string.IsNullOrEmpty(iamAPIKey))
        {
            throw new WatsonException("Plesae provide IAM ApiKey for the service.");
        }

        TokenOptions tokenOptions = new TokenOptions()
        {
            IamApiKey = iamAPIKey
        };
        
        Credentials credentials = new Credentials(tokenOptions,serviceUrl);
        
        yield return new WaitUntil(credentials.HasIamTokenData);
        
        service = new SpeechToText(credentials);
        service.StreamMultipart = true;
        
        Active = true;
        StartRecording();
        
    }

    private void StartRecording()
    {       
        if (recordingRoutine == 0)
        {
            UnityObjectUtil.StartDestroyQueue();
            recordingRoutine = Runnable.Run(RecordingHandler());
        }
    }

    private void StopRecording()
    {
        if (recordingRoutine != 0)
        {
            Microphone.End(microphoneId);
            Runnable.Stop(recordingRoutine);
            recordingRoutine = 0;
            
        }
    }

    private IEnumerator RecordingHandler() //record voice input runtime
    {
        
        recording = Microphone.Start(microphoneId, true, recordingBufferSize, recordingHZ);
        yield return null;      // let recordingRoutine get set..

        if (recording == null)
        {
            StopRecording();
            yield break;
        }

        bool bFirstBlock = true;
        int midPoint = recording.samples / 2;
        float[] samples = null;
        AudioData record = null;
        
        while (recordingRoutine != 0 && recording != null) //microphone running and recording 
        {
            
            //stop recording when avatar is talking
            if (MessengerBehaviour.Instance.isAvatartalking) 
            {
                Active = false; 
            }
            else
            {
                Active = true;
            }

            bool doListen = false;

            int writePos = Microphone.GetPosition(microphoneId);//Get the position in samples of the recording.

            // if the write position is bigger than the recodings sample, that means we are not getting any recoding data
            if (writePos > recording.samples || !Microphone.IsRecording(microphoneId))
            {
                Debug.LogError("WatsonSTT.RecordingHandler()"+"Microphone disconnected.");

                StopRecording();
                yield break;
            }

            // if the recoding is now in the first block or in the midpoint
            if ((bFirstBlock && writePos >= midPoint) || (!bFirstBlock && writePos < midPoint))
            {
                // front block is recorded, make a RecordClip and pass it onto our callback.
                samples = new float[midPoint];
                recording.GetData(samples, bFirstBlock ? 0 : midPoint);// if in first block start getting data from 0, otherwise from mid

                // store the recoding in the AudioData and save the clip
                record = new AudioData();
				record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
                record.Clip = AudioClip.Create("Recording", midPoint, recording.channels, recordingHZ, false);
                record.Clip.SetData(samples, 0);
                
                service.OnListen(record);
               
                // toggle the firstblock boolean
                bFirstBlock = !bFirstBlock;
                
                
            }
            else
            {
                // calculate the number of samples remaining until we ready for a block of audio, 
                // and wait that amount of time it will take to record.
                int remaining = bFirstBlock ? (midPoint - writePos) : (recording.samples - writePos);
                float timeRemaining = (float)remaining / (float)recordingHZ;
//                Debug.Log(timeRemaining);
                
                yield return new WaitForSeconds(timeRemaining);
                
            }
            
        }

        
        yield break;
    }
    
    private void OnRecognize(SpeechRecognitionEvent result, Dictionary<string, object> customData) //stt convert
     {
        
        string lastSentence = "";
        double confidence = -1;
        float timeSinceLastUpdate = 0;

        float startTime = Time.time;
        if (result != null && result.results.Length > 0)
        {
            Debug.Log("Recognizing");
            foreach (var res in result.results) //browse Json for results and alternatives
            {
                foreach (var alt in res.alternatives)
                {
                    outputTextFinal = alt.transcript;
                    finalResultFound = false;
                    
                    string text = string.Format("{0} ({1}, {2:0.00})\n", alt.transcript, res.final ? "Final" : "Interim", alt.confidence); //for debug
                    resultText.text = text;                   
                    if (res.final) //final result
                    {
                        finalResultFound = true;
                        MessengerBehaviour.Instance.FinalMassageInput = alt.transcript;
                        Debug.Log(MessengerBehaviour.Instance.FinalMassageInput);
                        MessengerBehaviour.Instance.STTCompleted(); //trigger stt event
                        //StartCoroutine(WaitAndSay());
                        return;
                    }
                    
                }
                
            } 
        }
        
    }

    //Debug purpose (for future probably)
    private IEnumerator WaitAndSay()
    {
        yield return new WaitForSeconds(1);
        outputTextFinal = "";
        tempOutputText = "";
    }
    
    // Called when Recoding is Active
    private void OnRecognizeSpeaker(SpeakerRecognitionEvent result, Dictionary<string, object> customData)
    {
        if (result != null)
        {
            foreach (SpeakerLabelsResult labelResult in result.speaker_labels)
            {
                Debug.Log(string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence));
            }
        }
    }
    
    private void OnError(string error)
    {
        Active = false;

        Debug.Log("Error!" + error);
    }
    
}
