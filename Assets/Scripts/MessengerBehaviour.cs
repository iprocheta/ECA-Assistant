using UnityEngine;
using UnityEngine.UI;
//handle all events for tts>stt or stt>tts
public class MessengerBehaviour : Manager<MessengerBehaviour>
{
    private string finalMassageInput;
    private string finalMassageOutput;

    public bool isAvatartalking { get; set; }
    public InputField messageBox; // check if watson assistant and tts is working 
    
    
    public delegate void OnSpeechToTextComplete ();
    public static event OnSpeechToTextComplete onSTTCompleteEvent;

    public delegate void OnTextToSpeechComplete();
    public static event OnTextToSpeechComplete onTTSCompleteEvent;
    
    public string FinalMassageInput //store and retrieve stt (input)
    {
        get
        {
            if(finalMassageInput==null)
                return messageBox.text;
            return finalMassageInput;

        }
        set
        {
            finalMassageInput = value;
            messageBox.text = finalMassageInput;  //display
        }

    }
    
    public string FinalMassageOutput  //store and retrieve tts (reply)
    {
        get
        {
            return finalMassageOutput;
        }
        set
        {
            finalMassageOutput = value;
        }

    }
    public void STTCompleted()
    {
        onSTTCompleteEvent();
    }
    
    public void TTSCompleted()
    {
        onTTSCompleteEvent();
    }

    // Just for debug purpose
    public void SetMessage() //if I give text input
    {
        finalMassageInput = messageBox.text;
        STTCompleted();
    }
}

