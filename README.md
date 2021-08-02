# tanzu-toolkit-for-visual-studio


### Table of Contents

- [Introduction](#introduction)
- [Disclaimer](#disclaimer)
- [Install](#install)
- [Demo Video](#demo-video)
- [Usage](#usage)
- [Version Support](#version-support)
- [Notes](#notes)



## Introduction

The `TanzuToolkit` solution provides a VSIX extension that allows Visual Studio users to interact with Tanzu Application Service from within their IDE.

## Disclaimer
- This product is still under development and, as such, may lack some features.
- Our team uses [this board in ZenHub](https://app.zenhub.com/workspaces/net-dev-x---visual-studio-extensions-604161e65a9f390012665e4d/board?repos=327998348) to track progress.
- All VSIX files for this extension are from the pre-release version and are not publicly supported.
- The extension currently doesn't support SSL validation when connecting to Tanzu Application Service.

## Install
- To install this extension, you will need Visual Studio version 16.0 or higher.
- You can find a pre-release under the `v0.0.2` tag in the ["Releases" section of this repository](https://github.com/vmware-tanzu/tanzu-toolkit-for-visual-studio/releases).
- Download the `Tanzu.Toolkit.VisualStudio.vsix` file & double-click to prompt an installation into your Visual Studio instance.
- To uninstall this extension from Visual Studio, visit the Visual Studio Extension Manager found under the `Extensions` menu.

## Demo Video

[[Demo video]![demo](https://user-images.githubusercontent.com/52456455/114413841-56d54700-9b7c-11eb-9baa-504a50bccb02.PNG)](https://user-images.githubusercontent.com/52456455/114176128-c2af7980-9908-11eb-831b-f2ac34bc3e61.mp4)

[[Demo video 0.0.2]![demo2](https://user-images.githubusercontent.com/52456455/127934814-eacd1e95-59ec-4ef7-99b4-e4a82d8fbfd2.PNG)
)](https://user-images.githubusercontent.com/52456455/127934840-1d261ddc-b1f2-4eef-95ac-e86f5fc91501.mp4)

## Screenshots

![demopic1](https://user-images.githubusercontent.com/52456455/114448940-2ef8da00-9ba2-11eb-885f-25815c8858ec.PNG)
![demopic2](https://user-images.githubusercontent.com/52456455/114448950-30c29d80-9ba2-11eb-964e-d1e3c2fe1423.PNG)
![demopic3](https://user-images.githubusercontent.com/52456455/114448957-328c6100-9ba2-11eb-938c-8a97119e27bc.PNG)


## Usage
- The Tanzu Cloud Explorer is located under the 'View' tab in VS. This window is where you can sign into your cloud instance and manage it.
- To deploy an app, right click on a project in the Solution Explorer window and click 'Deploy to Tanzu application Service'. This will open up a new window, which allows you to choose the org and space for the app.
  - After clicking the 'Deploy' button, you can view the output in the 'Tanzu Output' window (found under the 'View' menu).
  - The newly deployed app will show up in the Tanzu Cloud Explorer window after pressing the refresh button.

## Version Support
- Our extension currently supports both v.2 (≥ 2.128.0) and v.3 (≥ 3.63.0) of the Cloud Controller API. 
- To maximize the range of supported CC API versions, 2 copies of the CF CLI are bundled with this extension: CF CLI v6 & CF CLI v7:
  - If Tanzu Application Service is running CC API version 2 ... 
    - ... above 2.150.0, this extension uses CF CLI v7.
    - ... between 2.128.0 - 2.150.0, this VS extension uses CF CLI v6.
  - If Tanzu Application Service is running CC API version 3 ... 
    - ... above 3.85.0, this extension uses CF CLI v7.
    - ... between 3.63.0 - 3.85.0, this VS extension uses CF CLI v6.

As of July 2021 we are still working on an MVP & are not yet advertising this tool publicly.

## Notes
- This extension uses the CF CLI to perform certain operations on Tanzu Application Service. All CF CLI binaries & config files can be found in the installation directory for this Visual Studio Extension (by default, this extension is installed in `c:\users\<user>\appdata\local\microsoft\visualstudio\<vs-instance>\extensions\vmware\tanzu toolkit for visual studio\<vsix-version>`)
