# Introduction

A Telegram Bot that takes a social media URL and download a video.

Social media supported:
1. Twitter
2. Instagram (stories requires account)


## Dependencies:

### 1. [FFmpeg](https://github.com/FFmpeg/FFmpeg "FFmpeg")

**FFmpeg** is used to download Twitters videos.

Install FFmpeg either put it on system path or configure the path on `appsettings.local.json` as below.
```json
"Twitter": {
    "FFmpegPath": "/usr/local/bin/ffmpeg"
  }
```

### 2. [Telegram bot](https://core.telegram.org/bots/tutorial "Telegram bot") account

You can create a Telegram bot using [BotFather](https://telegram.me/BotFather "BotFather") following this [tutorial](https://core.telegram.org/bots/tutorial "tutorial").

# Getting Started

Create `appsettings.local.json` file as below:

```json
{
  "Telegram": {
    "Token": "<type chatbot token>"
  }
}
```

**Optional:** Configure Instagram account to download stories videos

```json
"Instagram": {
    "Username": "<type username>",
	"Password": "<type password>"
  }
```

## Tip: Running on Raspberry OS

1. Install chromium:
```bash
sudo apt install chromium
```

2. Configure browser on `appsettings.local.json` file adding this lines:
```json
"PuppeteerBrowser": {
    "ExecutablePath": "/usr/bin/chromium",
    "Args": [
      "--no-sandbox",
      "--disable-gpu",
      "--disable-dev-shm-usage",
      "--disable-setuid-sandbox",
      "--no-startup-window"
    ]
}
```

