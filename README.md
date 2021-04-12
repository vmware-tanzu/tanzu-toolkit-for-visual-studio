# tanzu-toolkit-for-visual-studio


### Table of Contents

- [Introduction](#introduction)
- [Disclaimer](#disclaimer)
- [Install](#install)
- [Demo Video](#demo-video)
- [Usage](#usage)
- [Contributing](#contributing)
- [Version Support](#version-support)



## Introduction

The `TanzuToolkitForVS` project provides a VSIX extension that allows Visual Studio users to interact with Tanzu Application Service Cloud Foundry) from their IDE.

## Disclaimer
- This product is still under development and, as such, may lack some features.
- Our team uses [this board in ZenHub](https://app.zenhub.com/workspaces/net-dev-x---visual-studio-extensions-604161e65a9f390012665e4d/board?repos=327998348) to track progress.
- All VSIX files for this extension are pre-release version and are not public supported.

## Install
- GitHub/Manual Download
  -  Download the .vsix file from the repo

## Demo Video + Screenshots

[[Demo video]![demo](https://user-images.githubusercontent.com/52456455/114413841-56d54700-9b7c-11eb-9baa-504a50bccb02.PNG)](https://user-images.githubusercontent.com/52456455/114176128-c2af7980-9908-11eb-831b-f2ac34bc3e61.mp4)

![demopic1](https://user-images.githubusercontent.com/52456455/114448940-2ef8da00-9ba2-11eb-885f-25815c8858ec.PNG)
![demopic2](https://user-images.githubusercontent.com/52456455/114448950-30c29d80-9ba2-11eb-964e-d1e3c2fe1423.PNG)
![demopic3](https://user-images.githubusercontent.com/52456455/114448957-328c6100-9ba2-11eb-938c-8a97119e27bc.PNG)


## Usage
- The Tanzu Cloud Explorer is located under the 'View' tab in VS. This window is where you can sign into your cloud instance and manage it.
- To deploy an app, right click on it and click 'Deploy to Tanzu application Service'. This will open up a new window, which allows you to choose the org and space for the app.
  - After clicking the 'Deploy' button, you can view the output in the 'Tanzu Output' window.
  - The newly deployed app will then show up in the Tanzu Cloud Explorer window.

## Contributing
(a ticket will be created for this) 


## Version Support
- Our extension currently supports v.2 of the Cloud Controller API, specifically between 2.128.0 - 2.149.0
- We are currently working on supporting V3 of the Cloud Controller API and later versions of V2

As of April 2021 we are still working on an MVP & are not yet advertising this tool publicly.
