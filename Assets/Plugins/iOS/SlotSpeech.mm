#import <AVFoundation/AVFoundation.h>

static AVSpeechSynthesizer *SlotSpeechSynth;

extern "C" void SlotSpeechSpeak(const char *text)
{
    if (text == NULL)
    {
        return;
    }

    if (SlotSpeechSynth == nil)
    {
        SlotSpeechSynth = [[AVSpeechSynthesizer alloc] init];
    }

    NSString *line = [NSString stringWithUTF8String:text];
    AVSpeechUtterance *utterance = [AVSpeechUtterance speechUtteranceWithString:line];
    utterance.rate = AVSpeechUtteranceDefaultSpeechRate;
    [SlotSpeechSynth speakUtterance:utterance];
}
