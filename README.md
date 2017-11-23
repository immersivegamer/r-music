# r-music
Command line tool to download music from the subreddit /r/listentothis or other subreddits.

## Getting Started

### Prerequisites

What things you need to install?

```
Download .NET Framework 4.6.1 - https://www.microsoft.com/net/download/visual-studio-sdks
Download .NET Framework 3.5 (for YoutubeExtractor)- https://www.microsoft.com/net/download/visual-studio-sdks
```
If did not do a recursive clone, run:
```
git submodule update --init --recursive
```
OR, if that causes error
```
git submodule init
git submodule sync
git submodule foreach "(git checkout master; git pull)"
```
Nuget Packages
run (in base directory, and in YoutubeExtractor directory)
```
nuget restore
```

## Running the tests

Tests are written with Xunit. You can run these through your IDE.
