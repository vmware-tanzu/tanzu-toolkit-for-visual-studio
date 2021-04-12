# tanzu-toolkit-for-visual-studio


### Table of Contents

- [Introduction](#introduction)
- [Disclaimer](#disclaimer)
- [Install](#install)
- [Usage](#usage)
- [Contributing](#contributing)
- [Version Support](#version-support)
- [Demo Video](#demo-video)
- [Project Management](#project-management)


## Introduction

The `TanzuToolkitForVS` project provides a VSIX extension that allows Visual Studio users to interact with Cloud Foundry from their IDE.

## Disclaimer
- This product is still under development and, as such, may lack some features.

## Install
- Github/Manual Download
  -  Download the .vsix file from the repo


## Usage
- The Tanzu Cloud Explorer is located under the 'View' tab in VS. This window is where you can sign into your cloud instance and manage it.
- To deploy an app, right click on it and click 'Deploy to Tanzu application Service'. This will open up a new window, which allows you to choose the org and space for the app.
  - After clicking the 'Deploy' button, you can view the output in the 'Tanzu Output' window.
  - The newly deployed app will then show up in the Tanzu Cloud Explorer window.

## Contributing
GitHub issues & pull requests are welcome! Please recognize that we are a small team with a limited capacity to engage heavily with open source contributions, so the best way to get new feature requests implemented is by contributing code. 


## Version Support
- Our extension will only work if you are running V2 of the Cloud Controller API, specifically between 2.128.0 - 2.149.0
- We are currently working on supporting V3 of the Cloud Controller API and later versions of V2

## Demo Video

[![Watch the video](![demo](https://user-images.githubusercontent.com/52456455/114413841-56d54700-9b7c-11eb-9baa-504a50bccb02.PNG)](https://user-images.githubusercontent.com/52456455/114176128-c2af7980-9908-11eb-831b-f2ac34bc3e61.mp4)

## Project Management

Our team uses [this board in ZenHub](https://app.zenhub.com/workspaces/net-dev-x---visual-studio-extensions-604161e65a9f390012665e4d/board?repos=327998348) to track progress.


As of April 2021 we are still working on an MVP & are not yet advertising this tool publicly.
