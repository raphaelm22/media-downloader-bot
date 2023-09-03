using System.Net.Http.Headers;

namespace MediaDownloaderBot.MessageReceivedHandlers.Youtube
{
    internal sealed class PlayerRequestModel
    {
        public static StringContent Create(string videoId) => new(
            $$"""
            {
                "context": {
                    "client": {
                        "hl": "en",
                        "clientName": "WEB",
                        "clientVersion": "2.20210721.00.00",
                        "clientFormFactor": "UNKNOWN_FORM_FACTOR",
                        "clientScreen": "WATCH"
                    },
                    "user": {
                        "lockedSafetyMode": false
                    },
                    "request": {
                        "useSsl": true,
                        "internalExperimentFlags": [],
                        "consistencyTokenJars": []
                    }
                },
                "videoId": "{{videoId}}",
                "playbackContext": {
                    "contentPlaybackContext": {
                        "vis": 0,
                        "splay": false,
                        "autoCaptionsDefaultOn": false,
                        "autonavState": "STATE_NONE",
                        "html5Preference": "HTML5_PREF_WANTS",
                        "lactMilliseconds": "-1"
                    }
                },
                "racyCheckOk": false,
                "contentCheckOk": false
            }
            """,
            new MediaTypeHeaderValue("application/json")
        );
    }
}
